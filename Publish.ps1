param(
	# Skips waiting for the release run. The tag is still pushed, but nothing confirms a package
	# reached nuget.org — use it only if you are checking the run yourself.
	[switch]$SkipPublishVerification
)

# Ensure we are on the main branch
$branch = git rev-parse --abbrev-ref HEAD
if ($branch -ne 'main') {
	Write-Error "Not on main branch. Current branch: $branch"
	exit 1
}

# Ensure working tree is clean
$status = git status --porcelain
if ($status) {
	Write-Error "Working tree is not clean."
	exit 1
}

# Ensure we are up to date with origin
git fetch origin main --quiet
$behind = git rev-list --count HEAD..origin/main
if ($behind -gt 0) {
	Write-Error "Local branch is behind origin/main by $behind commit(s)."
	exit 1
}

# Checked before anything is pushed, because pushing the tag is the step that cannot be taken back.
# Without the GitHub CLI there is no way to confirm the release run succeeded, and an unverified
# publish is how repositories end up months behind their newest tag with nobody noticing.
if (-not $SkipPublishVerification) {
	$gh = Get-Command gh -ErrorAction SilentlyContinue
	if (-not $gh) {
		Write-Error "The GitHub CLI (gh) is required to verify that the package publishes. Install it from https://cli.github.com, or re-run with -SkipPublishVerification to publish without verification."
		exit 1
	}

	gh auth status 2>&1 | Out-Null
	if ($LASTEXITCODE -ne 0) {
		Write-Error "The GitHub CLI is not authenticated. Run 'gh auth login', or re-run with -SkipPublishVerification to publish without verification."
		exit 1
	}
}

# Get version from Nerdbank.GitVersioning via the project's MSBuild targets (the
# referenced NuGet package), so this does not depend on the global 'nbgv' CLI tool
# being installed or on PATH.
$packableProject = Get-ChildItem -Recurse -Filter *.csproj |
	Where-Object { $_.FullName -notmatch '[\\/]obj[\\/]' -and (Get-Content $_.FullName -Raw) -match 'Nerdbank\.GitVersioning' } |
	Select-Object -First 1
if (-not $packableProject) {
	Write-Error "Could not find a packable project referencing Nerdbank.GitVersioning."
	exit 1
}
$buildOutput = dotnet build $packableProject.FullName -t:GetBuildVersion --getProperty:NuGetPackageVersion -nologo -v:quiet -p:TreatWarningsAsErrors=false
if ($LASTEXITCODE -ne 0) {
	Write-Error "Failed to determine version from Nerdbank.GitVersioning.`n$buildOutput"
	exit 1
}
$version = ($buildOutput | Select-Object -Last 1).ToString().Trim()
Write-Host "Version: $version"

# Check if tag already exists
$existingTag = git tag -l $version
if ($existingTag) {
	Write-Error "Tag $version already exists."
	exit 1
}

# Create and push tag
git tag $version
git push origin $version
Write-Host "Tag $version pushed."

if ($SkipPublishVerification) {
	Write-Warning "Not waiting for the release run (-SkipPublishVerification). Nothing has confirmed that a package reached nuget.org."
	exit 0
}

# The repository the run belongs to, read from the remote rather than assumed.
$originUrl = git remote get-url origin
$repoFullName = ($originUrl -replace '^.*github\.com[:/]', '') -replace '\.git$', ''

Write-Host "Waiting for the release run for $version..."

# The run takes a few seconds to appear after the tag push.
$runId = $null
for ($attempt = 1; $attempt -le 12 -and -not $runId; $attempt++) {
	Start-Sleep -Seconds 5
	$runListJson = gh run list --repo $repoFullName --branch $version --limit 1 --json databaseId 2>$null
	if ($LASTEXITCODE -eq 0 -and $runListJson) {
		$runList = $runListJson | ConvertFrom-Json
		if ($runList.Count -gt 0) { $runId = $runList[0].databaseId }
	}
}

if (-not $runId) {
	Write-Error "Tag $version was pushed but no run appeared for it. Check https://github.com/$repoFullName/actions — the workflow may not trigger on tags."
	exit 1
}

Write-Host "Run: https://github.com/$repoFullName/actions/runs/$runId"
gh run watch $runId --repo $repoFullName --exit-status --interval 20
$runExitCode = $LASTEXITCODE

if ($runExitCode -ne 0) {
	Write-Host ""
	Write-Host "The release run did not succeed: https://github.com/$repoFullName/actions/runs/$runId" -ForegroundColor Red

	# A refused job — an exhausted Actions budget, for instance — fails before any step runs, so it
	# has no failed step to report. The check-run annotation is the only place the reason appears.
	$jobId = gh api "repos/$repoFullName/actions/runs/$runId/jobs" --jq '.jobs[0].id' 2>$null
	if ($LASTEXITCODE -eq 0 -and $jobId) {
		$annotation = gh api "repos/$repoFullName/check-runs/$jobId/annotations" --jq '.[0].message' 2>$null
		if ($LASTEXITCODE -eq 0 -and $annotation) {
			Write-Host "Reason: $annotation" -ForegroundColor Red
		}
	}

	Write-Host ""
	Write-Host "Tag $version is pushed but no package was published. Once the cause is fixed:" -ForegroundColor Yellow
	Write-Host "  gh run rerun $runId --repo $repoFullName --failed" -ForegroundColor Cyan
	exit 1
}

Write-Host "Package $version published." -ForegroundColor Green