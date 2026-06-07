Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Ensure we are on the main branch
$branch = (& git rev-parse --abbrev-ref HEAD).Trim()
if ($LASTEXITCODE -ne 0) {
	throw "Failed to determine current git branch."
}

if ($branch -ne 'main') {
	throw "Not on main branch. Current branch: $branch"
}

# Ensure working tree is clean
$status = (& git status --porcelain).Trim()
if ($LASTEXITCODE -ne 0) {
	throw "Failed to check git working tree status."
}

if (-not [string]::IsNullOrWhiteSpace($status)) {
	throw "Working tree is not clean."
}

# Ensure we are up to date with origin
& git fetch origin main --quiet
if ($LASTEXITCODE -ne 0) {
	throw "Failed to fetch origin/main."
}

$behindText = (& git rev-list --count HEAD..origin/main).Trim()
if ($LASTEXITCODE -ne 0) {
	throw "Failed to compare local branch with origin/main."
}

$behind = 0
if (-not [int]::TryParse($behindText, [ref]$behind)) {
	throw "Unable to parse behind count: '$behindText'."
}

if ($behind -gt 0) {
	throw "Local branch is behind origin/main by $behind commit(s)."
}

# Get version from Nerdbank.GitVersioning
$versionJsonText = $null
if (Get-Command nbgv -ErrorAction SilentlyContinue) {
	$versionJsonText = (& nbgv get-version -f json)
}
else {
	$versionJsonText = (& dotnet nbgv get-version -f json)
}

if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($versionJsonText)) {
	throw "Failed to get version from Nerdbank.GitVersioning. Ensure 'nbgv' or 'dotnet nbgv' is available."
}

$versionJson = $versionJsonText | ConvertFrom-Json
$version = "$($versionJson.NuGetPackageVersion)".Trim()
if ([string]::IsNullOrWhiteSpace($version)) {
	throw "NuGetPackageVersion was empty."
}

Write-Host "Version: $version"

# Check if tag already exists
$existingTag = (& git tag -l $version).Trim()
if ($LASTEXITCODE -ne 0) {
	throw "Failed to query existing tags."
}

if (-not [string]::IsNullOrWhiteSpace($existingTag)) {
	throw "Tag $version already exists."
}

# Create and push tag
& git tag $version
if ($LASTEXITCODE -ne 0) {
	throw "Failed to create tag '$version'."
}

& git push origin $version
if ($LASTEXITCODE -ne 0) {
	throw "Failed to push tag '$version' to origin."
}

Write-Host "Tag $version pushed. CI will publish the package."
