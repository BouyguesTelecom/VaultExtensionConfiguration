<#
.SYNOPSIS
    Creates and pushes a version tag based on git commit count.

.DESCRIPTION
    This script calculates the patch version by counting commits since the
    current major.minor version was introduced in version.json, creates a
    git tag, and optionally pushes it to the remote repository.

.PARAMETER Push
    If specified, pushes the tag to the remote repository.

.PARAMETER DryRun
    If specified, shows what would be done without actually creating or pushing the tag.

.EXAMPLE
    ./tools/version.ps1
    Creates the tag locally without pushing.

.EXAMPLE
    ./tools/version.ps1 -Push
    Creates the tag and pushes it to origin.

.EXAMPLE
    ./tools/version.ps1 -DryRun
    Shows what version tag would be created without doing anything.
#>

[CmdletBinding()]
param(
    [switch]$Push,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')

Push-Location $RepoRoot
try {
    # Verify we're in a git repository
    $gitRoot = git rev-parse --show-toplevel 2>$null
    if (-not $gitRoot) {
        Write-Error "Not in a git repository"
        exit 1
    }

    # Check for uncommitted changes
    $gitStatus = git status --porcelain
    if ($gitStatus) {
        Write-Warning "You have uncommitted changes. Consider committing them first."
        if (-not $DryRun) {
            $continue = Read-Host "Continue anyway? (y/N)"
            if ($continue -ne 'y') {
                Write-Host "Aborted." -ForegroundColor Yellow
                exit 0
            }
        }
    }

    # Read version.json
    $versionJsonPath = Join-Path $RepoRoot 'version.json'
    if (-not (Test-Path $versionJsonPath)) {
        Write-Error "version.json not found at $versionJsonPath"
        exit 1
    }

    $versionConfig = Get-Content $versionJsonPath -Raw | ConvertFrom-Json
    $baseVersion = $versionConfig.version  # e.g., "0.1-beta" or "1.0"

    # Extract major.minor and prerelease tag
    if ($baseVersion -match '^(\d+\.\d+)(-(.+))?$') {
        $majorMinor = $Matches[1]          # e.g., "0.1"
        $prereleaseTag = $Matches[3]       # e.g., "beta" or $null
    } else {
        Write-Error "Invalid version format in version.json: $baseVersion"
        exit 1
    }

    Write-Host "Calculating version..." -ForegroundColor Cyan
    Write-Host "  Base version: $majorMinor" -ForegroundColor Gray
    if ($prereleaseTag) {
        Write-Host "  Prerelease:   $prereleaseTag" -ForegroundColor Gray
    }

    # Find the first commit where this major.minor version was introduced
    # by looking at version.json history (oldest first)
    $versionCommits = @(git log --oneline --reverse -- version.json | ForEach-Object {
        ($_ -split ' ')[0]
    })

    $firstVersionCommit = $null
    foreach ($commit in $versionCommits) {
        try {
            $commitVersionJson = git show "${commit}:version.json" 2>$null | ConvertFrom-Json
            $commitVersion = $commitVersionJson.version

            # Check if this commit has the same major.minor
            if ($commitVersion -match "^$([regex]::Escape($majorMinor))") {
                $firstVersionCommit = $commit
                break  # Found the first commit with this version
            }
        } catch {
            # Can't parse, skip
            continue
        }
    }

    # Count commits from first version commit to HEAD
    if ($firstVersionCommit) {
        Write-Host "  First commit with $majorMinor`: $firstVersionCommit" -ForegroundColor Gray
        $patchVersion = [int](git rev-list --count "${firstVersionCommit}~1..HEAD" 2>$null)
        if ($LASTEXITCODE -ne 0 -or $patchVersion -eq 0) {
            # If firstVersionCommit is the root commit or error
            $patchVersion = [int](git rev-list --count "${firstVersionCommit}..HEAD") + 1
        }
    } else {
        # No version found, count all commits
        $patchVersion = [int](git rev-list --count HEAD)
    }

    # Build version strings
    $version = "$majorMinor.$patchVersion"
    $gitCommitId = git rev-parse HEAD
    $gitCommitIdShort = git rev-parse --short HEAD

    if ($prereleaseTag) {
        $semVer = "$version-$prereleaseTag"
        $tagName = "v$version"
    } else {
        $semVer = $version
        $tagName = "v$version"
    }

    Write-Host ""
    Write-Host "Version Information:" -ForegroundColor Green
    Write-Host "  Version:        $version"
    Write-Host "  SemVer:         $semVer"
    Write-Host "  Tag to create:  $tagName"
    Write-Host "  Commit:         $gitCommitIdShort ($gitCommitId)"
    Write-Host "  Patch number:   $patchVersion (commits since $majorMinor was introduced)"
    Write-Host ""

    if ($DryRun) {
        Write-Host "[DRY RUN] Would create tag: $tagName" -ForegroundColor Yellow
        if ($Push) {
            Write-Host "[DRY RUN] Would push tag to origin" -ForegroundColor Yellow
        }
        exit 0
    }

    # Check if tag already exists
    $existingTag = git tag -l $tagName
    if ($existingTag) {
        Write-Error "Tag '$tagName' already exists. Use a different version or delete the existing tag."
        exit 1
    }

    # Create the tag (lightweight tag for compatibility with release workflow)
    Write-Host "Creating tag '$tagName'..." -ForegroundColor Cyan
    git tag $tagName

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to create tag"
        exit 1
    }
    Write-Host "Tag '$tagName' created successfully." -ForegroundColor Green

    # Push if requested
    if ($Push) {
        Write-Host "Pushing tag to origin..." -ForegroundColor Cyan
        git push origin $tagName

        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to push tag"
            exit 1
        }
        Write-Host "Tag '$tagName' pushed successfully." -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "Tag created locally. To push it, run:" -ForegroundColor Yellow
        Write-Host "  git push origin $tagName" -ForegroundColor White
        Write-Host "Or run this script with -Push parameter." -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "Done!" -ForegroundColor Green

} finally {
    Pop-Location
}
