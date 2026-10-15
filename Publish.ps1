param(
	# Skips waiting for the release run. The tag is still pushed, but nothing confirms a package
	# reached nuget.org; use it only if you are checking the run yourself.
	[switch]$SkipPublishVerification
)

$InformationPreference = 'Continue'

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

	# This only reads the stored login. The "status" subcommand makes an API call instead, so a GitHub
	# rate limit makes it report "The token in keyring is invalid" for a perfectly good login.
	gh auth token 2>&1 | Out-Null
	if ($LASTEXITCODE -ne 0) {
		Write-Error "The GitHub CLI has no stored login. Run 'gh auth login', or re-run with -SkipPublishVerification to publish without verification."
		exit 1
	}
}

# Get version from Nerdbank.GitVersioning via the project's MSBuild targets (the
# referenced NuGet package), so this does not depend on the global 'nbgv' CLI tool
# being installed or on PATH.
#
# Only projects git tracks in this checkout are considered. A recursive file search also walks
# git-ignored folders such as .claude/worktrees/, where each agent worktree holds a full checkout
# on its own branch: versioning one of those yields a version that is not a public release.
$packableProject = git ls-files '*.csproj' |
	Where-Object { (Test-Path $_) -and (Get-Content $_ -Raw) -match '<PackageReference\s+Include="Nerdbank\.GitVersioning"' } |
	Select-Object -First 1
if (-not $packableProject) {
	Write-Error "Could not find a tracked project referencing Nerdbank.GitVersioning."
	exit 1
}
$buildOutput = dotnet build $packableProject -t:GetBuildVersion --getProperty:NuGetPackageVersion -nologo -v:quiet -p:TreatWarningsAsErrors=false
if ($LASTEXITCODE -ne 0) {
	Write-Error "Failed to determine version from Nerdbank.GitVersioning.`n$buildOutput"
	exit 1
}
$version = ($buildOutput | Select-Object -Last 1).ToString().Trim()
Write-Information "Version: $version"

# On main the version must be a public release (1.2.3). Anything else, such as 1.2.3-gabcdef,
# would be tagged with a name that misses publicReleaseRefSpec and publish a prerelease.
if ($version -notmatch '^\d+\.\d+\.\d+$') {
	Write-Error "Version '$version' is not a public-release version (expected major.minor.patch). Not tagging."
	exit 1
}

# Check if tag already exists
$existingTag = git tag -l $version
if ($existingTag) {
	Write-Error "Tag $version already exists."
	exit 1
}

# Create and push tag. A push can fail transiently (a dropped connection, a DNS error), so it is
# retried, and the tag is never reported as pushed unless git says it was. If it still fails, the
# local tag is removed, so a re-run starts clean instead of stopping at "Tag already exists".
git tag $version
$pushed = $false
for ($attempt = 1; $attempt -le 3 -and -not $pushed; $attempt++) {
	git push origin $version
	if ($LASTEXITCODE -eq 0) {
		$pushed = $true
	}
	elseif ($attempt -lt 3) {
		Write-Warning "Pushing tag $version failed (attempt $attempt of 3); retrying."
		Start-Sleep -Seconds 5
	}
}
if (-not $pushed) {
	git tag -d $version | Out-Null
	Write-Error "Could not push tag $version to origin, so nothing was published. The local tag has been removed; re-run when the connection is back."
	exit 1
}
Write-Information "Tag $version pushed."

if ($SkipPublishVerification) {
	Write-Warning "Not waiting for the release run (-SkipPublishVerification). Nothing has confirmed that a package reached nuget.org."
	exit 0
}

# The repository the run belongs to, read from the remote rather than assumed.
$originUrl = git remote get-url origin
$repoFullName = ($originUrl -replace '^.*github\.com[:/]', '') -replace '\.git$', ''

Write-Information "Waiting for the release run for $version..."

# The run takes a few seconds to appear after the tag push.
$runId = $null
for ($attempt = 1; $attempt -le 12 -and -not $runId; $attempt++) {
	Start-Sleep -Seconds 5
	# A tag push can start several workflows; only the release workflow's run is the one to watch.
	$runListJson = gh run list --repo $repoFullName --workflow ci.yml --branch $version --limit 1 --json databaseId 2>$null
	if ($LASTEXITCODE -eq 0 -and $runListJson) {
		$runList = $runListJson | ConvertFrom-Json
		if ($runList.Count -gt 0) { $runId = $runList[0].databaseId }
	}
}

if (-not $runId) {
	Write-Error "Tag $version was pushed but no run appeared for it. Check https://github.com/$repoFullName/actions: the workflow may not trigger on tags, or GitHub may be rate-limiting the lookups."
	exit 1
}

Write-Information "Run: https://github.com/$repoFullName/actions/runs/$runId"
gh run watch $runId --repo $repoFullName --exit-status --interval 20
$runExitCode = $LASTEXITCODE

if ($runExitCode -ne 0) {
	Write-Information ""
	Write-Warning "The release run did not succeed: https://github.com/$repoFullName/actions/runs/$runId"

	# A refused job (an exhausted Actions budget, for instance) fails before any step runs, so it
	# has no failed step to report. The check-run annotation is the only place the reason appears.
	$jobId = gh api "repos/$repoFullName/actions/runs/$runId/jobs" --jq '.jobs[0].id' 2>$null
	if ($LASTEXITCODE -eq 0 -and $jobId) {
		$annotation = gh api "repos/$repoFullName/check-runs/$jobId/annotations" --jq '.[0].message' 2>$null
		if ($LASTEXITCODE -eq 0 -and $annotation) {
			Write-Warning "Reason: $annotation"
		}
	}

	Write-Information ""
	Write-Warning "Tag $version is pushed but no package was published. Once the cause is fixed:"
	Write-Information "  gh run rerun $runId --repo $repoFullName --failed"
	exit 1
}

Write-Information "Package $version published."