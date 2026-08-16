# RogueLike Mod Reborn - Pack Script (v2: preserve Workshop structure 1:1)
# Usage: powershell -File .\tools\packaging\pack_mod.ps1
# Output: RougelikeModReborn_YYYYMMDD_HHmmss.zip under _release_packages\archives

param(
    # Author's published Workshop content id (NOT the original CalmMagma item 3503523710).
    [string]$WorkshopContentId = "3743867841"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
while ($projectRoot -and -not (Test-Path (Join-Path $projectRoot "RogueLike Mod Reborn.csproj"))) {
    $projectRoot = Split-Path -Parent $projectRoot
}
if (-not $projectRoot) {
    throw "Could not locate repository root for packaging."
}

$workshop = "E:\Steam\steamapps\workshop\content\1256670\$WorkshopContentId"
if (-not (Test-Path $workshop)) {
    Write-Host "ERROR: Workshop not found: $workshop" -ForegroundColor Red
    Write-Host "Hint: run deploy_workshop.ps1 first (it seeds from 3503523710 if needed)." -ForegroundColor Yellow
    exit 1
}
Write-Host "Packing Workshop content id: $WorkshopContentId" -ForegroundColor Cyan

$tmpDir = "$projectRoot\_release_packages\RougelikeModReborn"
$archiveDir = "$projectRoot\_release_packages\archives"
New-Item -ItemType Directory -Force -Path $archiveDir | Out-Null

# Clean + copy everything 1:1
if (Test-Path $tmpDir) { Remove-Item -Recurse -Force $tmpDir }
Copy-Item -Recurse $workshop $tmpDir
Write-Host "Copied Workshop -> temp"

$tmpAsm = "$tmpDir\Assemblies"
$tmpDlls = "$tmpAsm\dlls"

# ====== Clean dev files ======
Write-Host "Cleaning dev files..."
$removed = 0
Get-ChildItem -Recurse $tmpDir -Filter "*.bak" -ErrorAction SilentlyContinue | ForEach-Object { Remove-Item $_.FullName -Force; $removed++ }
Write-Host "  Removed $removed .bak files"

$removed = 0
Get-ChildItem -Recurse $tmpDir -Filter "*.pre_*" -ErrorAction SilentlyContinue | ForEach-Object { Remove-Item $_.FullName -Force; $removed++ }
Write-Host "  Removed $removed .pre_* files"

if (Test-Path "$tmpDlls\DevNuggets") {
    Remove-Item -Recurse -Force "$tmpDlls\DevNuggets"
    Write-Host "  Removed DevNuggets\"
}
if (Test-Path "$tmpAsm\_codex_backups") {
    Remove-Item -Recurse -Force "$tmpAsm\_codex_backups"
    Write-Host "  Removed _codex_backups\"
}

# Remove Workshop metadata (not needed for Mods)
@("$tmpDir\desc.txt", "$tmpDir\preview.jpg.png", "$tmpDir\old changelogs.txt", "$tmpDir\mod infos") | ForEach-Object {
    if (Test-Path $_) { Remove-Item -Recurse -Force $_ }
}

# ====== Fix FormationInfo ======
Write-Host "Fixing FormationInfo..."
$fiDir = "$tmpDlls\AddData\FormationInfo"
if (-not (Test-Path $fiDir)) {
    New-Item -ItemType Directory -Force -Path $fiDir | Out-Null
    Copy-Item "$tmpDlls\AddData\FormationInfo.txt" "$fiDir\FormationInfo.txt" -Force
    Write-Host "  Created FormationInfo\FormationInfo.txt"
}

# ====== Zip ======
Write-Host "Creating zip..."
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$zipName = "RougelikeModReborn_v$timestamp.zip"
$zipPath = Join-Path $archiveDir $zipName
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

# Compress-Archive Write-Progress can throw IndexOutOfRange on large trees; use ZipFile.
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory(
    $tmpDir,
    $zipPath,
    [System.IO.Compression.CompressionLevel]::Optimal,
    $false)
Remove-Item -Recurse -Force $tmpDir

$zipSize = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)
Write-Host ""
Write-Host "Done: $zipName ($zipSize MB)"
