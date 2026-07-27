# Prepare a clean upload tree for Steam Workshop item 3743867841.
# Does NOT upload by itself (Steam login required — see upload_workshop.ps1).
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File .\tools\packaging\prepare_workshop_upload.ps1

param(
    [string]$WorkshopContentId = "3743867841",
    [string]$SourceRoot = ""
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptDir)
if (-not (Test-Path (Join-Path $projectRoot "RogueLike Mod Reborn.csproj"))) {
    throw "Project root not found from $scriptDir"
}

if (-not $SourceRoot) {
    $SourceRoot = "E:\Steam\steamapps\workshop\content\1256670\$WorkshopContentId"
}
if (-not (Test-Path $SourceRoot)) {
    throw "Source workshop folder missing: $SourceRoot — run deploy_workshop.ps1 first."
}

$uploadRoot = "E:\Steam\steamapps\workshop\content\1256670\${WorkshopContentId}_upload"
if (Test-Path $uploadRoot) {
    Remove-Item -Recurse -Force $uploadRoot
}
New-Item -ItemType Directory -Force -Path $uploadRoot | Out-Null

Write-Host "Copying clean content:" -ForegroundColor Cyan
Write-Host "  from $SourceRoot"
Write-Host "  to   $uploadRoot"

# robocopy mirrors structure; exclude backups/dev noise.
# *.old matters: deploy_workshop.ps1 keeps the previous DLL as "RogueLike Mod Reborn.dll.old",
# and without this it shipped to subscribers as a second ~1.4 MB assembly in Assemblies\dlls.
& robocopy $SourceRoot $uploadRoot /E /NFL /NDL /NJH /NJS /nc /ns /np `
    /XF *.bak *.pre_* *~ *.pdb *.old `
    /XD _codex_backups DevNuggets .git | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy failed ($LASTEXITCODE)" }
$global:LASTEXITCODE = 0

# Ensure preview exists
$preview = Join-Path $uploadRoot "preview.jpg.png"
if (-not (Test-Path $preview)) {
    $alt = Get-ChildItem $uploadRoot -Filter "preview*" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($alt) {
        Copy-Item $alt.FullName $preview -Force
        Write-Host "  using preview: $($alt.FullName)" -ForegroundColor Yellow
    }
    else {
        Write-Host "WARNING: no preview image — Steam may reject or show blank cover." -ForegroundColor Yellow
    }
}

# Force PUBLIC Workshop StageModInfo (Exist=false) — never ship the [本地] local title to Steam.
# Local deploys use StageModInfo.local.xml; cloud subscribers must see StageModInfo.fanwork.xml.
$stageTemplate = Join-Path $scriptDir "StageModInfo.fanwork.xml"
if (Test-Path $stageTemplate) {
    Copy-Item $stageTemplate (Join-Path $uploadRoot "StageModInfo.xml") -Force
    Write-Host "  applied StageModInfo.fanwork.xml (Workshop title, Exist=false)" -ForegroundColor Green
    $smi = Get-Content (Join-Path $uploadRoot "StageModInfo.xml") -Raw -Encoding UTF8
    if ($smi -match '\[本地\]') { throw "Upload StageModInfo still has [本地] title — abort" }
    if ($smi -notmatch '\[Workshop\]') {
        Write-Host "  WARNING: public title missing [Workshop] tag" -ForegroundColor Yellow
    }
}
Get-ChildItem $uploadRoot -Recurse -Filter "StageModInfo.xml" -ErrorAction SilentlyContinue |
    Where-Object { $_.DirectoryName -ne $uploadRoot } |
    ForEach-Object {
        Remove-Item $_.FullName -Force
        Write-Host "  removed nested StageModInfo: $($_.FullName)" -ForegroundColor Yellow
    }
# Also remove already-disabled leftovers
Get-ChildItem $uploadRoot -Recurse -Filter "StageModInfo.xml.not_a_mod" -ErrorAction SilentlyContinue |
    ForEach-Object { Remove-Item $_.FullName -Force }

$sizeMb = [math]::Round(((Get-ChildItem $uploadRoot -Recurse -File | Measure-Object Length -Sum).Sum) / 1MB, 2)
Write-Host ""
Write-Host "Upload tree ready ($sizeMb MB): $uploadRoot" -ForegroundColor Green
Write-Host "Next: run upload_workshop.ps1 (requires your Steam login)." -ForegroundColor Cyan
