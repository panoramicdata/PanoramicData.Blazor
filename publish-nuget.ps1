$ErrorActionPreference = "Stop"

$releaseBranch = "main"
$projectPath = "PanoramicData.Blazor\PanoramicData.Blazor.csproj"
$dllForVersion = "PanoramicData.Blazor\bin\Release\net10.0\PanoramicData.Blazor.dll"

# This script will publish to nuget using the api key in nuget-api-key.txt in the solution root.
# The api key issued by nuget.org should ideally only have permissions to update a single package
# with new versions.

$apiKeyFilename = "nuget-api-key.txt";
if(-not (Test-Path($apiKeyFilename))){
Write-Error "$apiKeyFilename does not exist"
exit 1;
}
$apiKey = Get-Content $apiKeyFilename;

# Getting changes into release branch
Write-Host "Fetching latest commits..."
&git fetch

$branch= &git rev-parse --abbrev-ref HEAD
if ($branch -ne $releaseBranch) {
Write-Host "Not on $releaseBranch branch - merging current branch into $releaseBranch and releasing..."

try {
Write-Host "Checking out $releaseBranch..."
&git checkout $releaseBranch
if (-not $?) {throw "Error with git checkout"}

Write-Host "Pulling..."
&git pull
if (-not $?) {throw "Error with git pull"}

Write-Host "Merging $branch into $releaseBranch..."
&git merge $branch --no-edit
if (-not $?) {throw "Error with git merge"}

Write-Host "Pushing $releaseBranch..."
&git push
if (-not $?) {throw "Error with git push"}
}
catch
{
# If there was a problem and we were not on $releaseBranch then switch back
if ($branch -ne $releaseBranch) {
Write-Host "Switching back to $branch branch"
&git checkout $branch
}
exit 1;
}
}


try {

# Build and pack the component library
dotnet build $projectPath -c Release
dotnet pack $projectPath -c Release

$mostRecentPackage = Get-ChildItem PanoramicData.Blazor\bin\Release\*.nupkg | Sort-Object LastWriteTime | Select-Object -last 1
Write-Host "Publishing $mostRecentPackage..."
dotnet nuget push "$mostRecentPackage" --source https://api.nuget.org/v3/index.json --api-key $apiKey
if (-not $?) {throw "Error publishing NuGet package"}

$version = (Get-Command $dllForVersion).FileVersionInfo
$major = $version.FileMajorPart
$minor = $version.FileMinorPart
$build = $version.FileBuildPart
$versionString = @($major, $minor, $build) -join "."
Write-Host "Finished building version ${versionString}."

Write-Host "Creating tag '${versionString}'..."

# Create the tag
Write-Host "Adding tag..."
git tag -a "$versionString" -m "Tagging version ${versionString}"
if (-not $?) {
Write-Error "Tag creation failed..."
exit 1;
}

# Push the tag
Write-Host "Pushing tag to origin..."
git push origin "$versionString"
if (-not $?) {
Write-Error "Tag push failed..."
exit 1;
}
}
finally
{
# If we were not on $releaseBranch then switch back
if ($branch -ne $releaseBranch) {
Write-Host "Switching back to $branch branch"
&git checkout $branch
}
}
