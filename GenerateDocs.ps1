# -----------------------------------------------------------------------------
# PowerShell Script for Blazor Component Documentation
# -----------------------------------------------------------------------------
#
# Description:
# This script generates a markdown file documenting all PD* Blazor components
# in the specified project directory. For each component, it extracts public
# parameters from the C# code-behind file. If the documentation file has
# changed, it will prompt to commit the changes to git.
#
# How to run this script:
# 1. Open a PowerShell terminal in the root of your Blazor project
# 2. You may need to adjust the execution policy to run the script.
#    You can do this for the current session by running:
#    Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process
# 3. Run the script:
#    .\GenerateDocs.ps1
#
# Output:
# The script will create a file named ComponentDocumentation.md in the same
# directory. If the file has changed, you will be prompted to commit it.
#
# -----------------------------------------------------------------------------

# --- Configuration ---
# Automatically detect the project root (script location)
$projectRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($projectRoot)) {
    $projectRoot = Get-Location
}

Write-Host "Project Root: $projectRoot" -ForegroundColor Cyan

$componentPath = Join-Path $projectRoot "PanoramicData.Blazor"
$outputFile = Join-Path $projectRoot "ComponentDocumentation.md"

# Validate paths
if (-not (Test-Path $componentPath)) {
    Write-Host "Error: Component path not found: $componentPath" -ForegroundColor Red
    Write-Host "Please run this script from the project root directory." -ForegroundColor Yellow
    exit 1
}

# --- Script Body ---

# Create a temporary file to build the new documentation
$tempOutputFile = [System.IO.Path]::GetTempFileName()

# Add a title to the markdown file
Add-Content -Path $tempOutputFile -Value "# PanoramicData.Blazor Component Documentation"
Add-Content -Path $tempOutputFile -Value ""
Add-Content -Path $tempOutputFile -Value "This document provides an overview of the Blazor components in this project."
Add-Content -Path $tempOutputFile -Value ""
Add-Content -Path $tempOutputFile -Value "Generated on: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Add-Content -Path $tempOutputFile -Value ""

# Find all .razor files for the components
$razorFiles = Get-ChildItem -Path $componentPath -Filter "PD*.razor"

Write-Host "Found $($razorFiles.Count) components. Generating documentation..."

foreach ($razorFile in $razorFiles) {
    $componentName = $razorFile.BaseName
    $csFilePath = "$($razorFile.FullName).cs"

    Add-Content -Path $tempOutputFile -Value "## $componentName"
    Add-Content -Path $tempOutputFile -Value ""

    if (Test-Path $csFilePath) {
        $lines = Get-Content $csFilePath
        $paramObjects = @()

        for ($i = 0; $i -lt $lines.Length; $i++) {
            if ($lines[$i].Trim() -eq "[Parameter]") {
                # Found a parameter, now find the property definition
                for ($j = $i + 1; $j -lt $lines.Length; $j++) {
                    if ($lines[$j] -match "public\s+([^ ]+)\s+([^ ]+)\s*\{") {
                        $paramType = $Matches[1]
                        $paramName = $Matches[2]

                        # Look for summary comments above the [Parameter] attribute
                        $comment = ""
                        for ($k = $i - 1; $k -ge 0; $k--) {
                            $line = $lines[$k].Trim()
                            if ($line.StartsWith("/// <summary>")) {
                                # Found the start of the summary, now extract it
                                for ($l = $k + 1; $l -lt $i; $l++) {
                                    $summaryLine = $lines[$l].Trim()
                                    if ($summaryLine.StartsWith("/// </summary>")) {
                                        break
                                    }
                                    if ($summaryLine.StartsWith("///")) {
                                        $comment += ($summaryLine -replace "///", "").Trim() + " "
                                    }
                                }
                                break
                            }
                            if (!$line.StartsWith("///")) {
                                break
                            }
                        }
                        
                        $paramObjects += [pscustomobject]@{
                            Name = $paramName
                            Type = $paramType
                            Description = $comment.Trim()
                        }
                        $i = $j # Continue searching from after the property
                        break
                    }
                }
            }
        }

        if ($paramObjects.Length -gt 0) {
            Add-Content -Path $tempOutputFile -Value "**Parameters:**"
            Add-Content -Path $tempOutputFile -Value ""
            Add-Content -Path $tempOutputFile -Value "| Name | Type | Description |"
            Add-Content -Path $tempOutputFile -Value "|------|------|-------------|"
            foreach ($param in $paramObjects) {
                                Add-Content -Path $tempOutputFile -Value ("| `{0}` | `{1}` | {2} |" -f $param.Name, $param.Type, $param.Description)
            }
        } else {
            Add-Content -Path $tempOutputFile -Value "This component has no public parameters."
        }
    } else {
        Add-Content -Path $tempOutputFile -Value "No code-behind file found for this component."
    }

    Add-Content -Path $tempOutputFile -Value ""
    Add-Content -Path $tempOutputFile -Value "---"
    Add-Content -Path $tempOutputFile -Value ""
}

Write-Host "Documentation generation complete."

# --- Git Commit Logic ---

# Compare the new file with the existing file
$isDifferent = $true
if (Test-Path $outputFile) {
    $diff = Compare-Object -ReferenceObject (Get-Content $outputFile) -DifferenceObject (Get-Content $tempOutputFile)
    if ($null -eq $diff) {
        $isDifferent = $false
    }
}

if ($isDifferent) {
    # Replace the old file with the new one
    Move-Item -Path $tempOutputFile -Destination $outputFile -Force
    
    Write-Host "ComponentDocumentation.md has been updated."
    $confirmation = Read-Host "Do you want to commit the changes? (y/n)"

    if ($confirmation -eq 'y') {
        Write-Host "Staging and committing changes..."
        git add $outputFile
        git commit -m "docs: Update component documentation"
        Write-Host "Changes have been committed."
    } else {
        Write-Host "Changes were not committed."
    }
} else {
    # Clean up the temporary file
    Remove-Item -Path $tempOutputFile
    Write-Host "No changes detected in ComponentDocumentation.md."
}