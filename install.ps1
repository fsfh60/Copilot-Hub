#!/usr/bin/env pwsh
# CopilotHub Installer for Windows
# Usage: irm https://raw.githubusercontent.com/fsfh60/Copilot-Hub/main/install.ps1 | iex

$ErrorActionPreference = "Stop"
$repo = "fsfh60/Copilot-Hub"
$installDir = "$env:LOCALAPPDATA\CopilotHub"

Write-Host ""
Write-Host "  ╔══════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "  ║       CopilotHub Installer           ║" -ForegroundColor Cyan
Write-Host "  ╚══════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Detect architecture
$arch = if ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq "Arm64") { "arm64" } else { "x64" }
Write-Host "  Detected architecture: windows-$arch" -ForegroundColor Gray

# Get latest release
Write-Host "  Fetching latest release..." -ForegroundColor Gray
try {
    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases/latest" -Headers @{ Accept = "application/vnd.github.v3+json" }
    $tag = $release.tag_name
    Write-Host "  Latest version: $tag" -ForegroundColor Green
} catch {
    Write-Host "  ERROR: Could not fetch latest release. Check your internet connection." -ForegroundColor Red
    exit 1
}

# Find the right asset
if ($arch -eq "x64") {
    $assetName = "CopilotHub.exe"
} else {
    $assetName = "CopilotHub-$tag-windows-arm64.exe"
}

$asset = $release.assets | Where-Object { $_.name -eq $assetName }
if (-not $asset) {
    Write-Host "  ERROR: Could not find asset '$assetName' in release $tag" -ForegroundColor Red
    exit 1
}

$downloadUrl = $asset.browser_download_url

# Download checksum
Write-Host "  Downloading checksums..." -ForegroundColor Gray
try {
    $checksums = Invoke-RestMethod -Uri "https://github.com/$repo/releases/download/$tag/checksums-sha256.txt"
} catch {
    $checksums = $null
    Write-Host "  Warning: Could not download checksums, skipping verification" -ForegroundColor Yellow
}

# Create install directory
if (!(Test-Path $installDir)) {
    New-Item -ItemType Directory -Path $installDir -Force | Out-Null
}

$exePath = Join-Path $installDir "CopilotHub.exe"

# Download
Write-Host "  Downloading $assetName..." -ForegroundColor Gray
Invoke-WebRequest -Uri $downloadUrl -OutFile $exePath -UseBasicParsing

# Verify checksum
if ($checksums) {
    $expectedHash = ($checksums -split "`n" | Where-Object { $_ -match $assetName } | ForEach-Object { ($_ -split "\s+")[0] }).Trim()
    if ($expectedHash) {
        $actualHash = (Get-FileHash $exePath -Algorithm SHA256).Hash.ToLower()
        if ($actualHash -eq $expectedHash) {
            Write-Host "  ✓ SHA256 checksum verified" -ForegroundColor Green
        } else {
            Write-Host "  ✗ SHA256 mismatch! Expected: $expectedHash Got: $actualHash" -ForegroundColor Red
            Remove-Item $exePath -Force
            exit 1
        }
    }
}

# Add to PATH if not already there
$userPath = [Environment]::GetEnvironmentVariable("Path", "User")
if ($userPath -notlike "*$installDir*") {
    Write-Host "  Adding to PATH..." -ForegroundColor Gray
    [Environment]::SetEnvironmentVariable("Path", "$userPath;$installDir", "User")
    $env:Path = "$env:Path;$installDir"
}

# Create desktop shortcut
$desktopPath = [Environment]::GetFolderPath("Desktop")
$shortcutPath = Join-Path $desktopPath "CopilotHub.lnk"
try {
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $exePath
    $shortcut.WorkingDirectory = $installDir
    $shortcut.Description = "CopilotHub - Multi-session Copilot Manager"
    $shortcut.Save()
    Write-Host "  ✓ Desktop shortcut created" -ForegroundColor Green
} catch {
    Write-Host "  Warning: Could not create desktop shortcut" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "  ✓ CopilotHub $tag installed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "  Location : $exePath" -ForegroundColor White
Write-Host "  Run      : CopilotHub" -ForegroundColor White
Write-Host "  Or       : Double-click the desktop shortcut" -ForegroundColor White
Write-Host ""
