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

# Get version from Nerdbank.GitVersioning
$versionJson = nbgv get-version -f json | ConvertFrom-Json
$version = $versionJson.NuGetPackageVersion
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
Write-Host "Tag $version pushed. CI will publish the package."