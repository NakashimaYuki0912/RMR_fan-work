# Deploy RMR build + data to Steam Workshop runtime folder.
# Usage (from repo / project root):
#   powershell -ExecutionPolicy Bypass -File .\tools\packaging\deploy_workshop.ps1
#   powershell -File .\tools\packaging\deploy_workshop.ps1 -Configuration Debug

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$SkipBuild,
    # Author's published Workshop content id (NOT the original CalmMagma item 3503523710).
    [string]$WorkshopContentId = "3743867841",
    # Optional seed tree when the target folder does not exist yet (e.g. first deploy of a new Workshop item).
    [string]$SeedFromWorkshopContentId = "3503523710"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptDir)
if (-not (Test-Path (Join-Path $projectRoot "RogueLike Mod Reborn.csproj"))) {
    throw "Project root not found from $scriptDir"
}

$workshopRoot = "E:\Steam\steamapps\workshop\content\1256670\$WorkshopContentId"
$dllsRoot = Join-Path $workshopRoot "Assemblies\dlls"
if (-not (Test-Path $dllsRoot)) {
    $seedRoot = "E:\Steam\steamapps\workshop\content\1256670\$SeedFromWorkshopContentId"
    if ($SeedFromWorkshopContentId -and (Test-Path $seedRoot)) {
        Write-Host "Workshop target missing; seeding $WorkshopContentId from $SeedFromWorkshopContentId ..." -ForegroundColor Yellow
        New-Item -ItemType Directory -Force -Path (Split-Path $workshopRoot -Parent) | Out-Null
        Copy-Item -Recurse -Force $seedRoot $workshopRoot
        # Drop seed-only author metadata that is not needed for a fresh local tree
        $codex = Join-Path $workshopRoot "Assemblies\_codex_backups"
        if (Test-Path $codex) { Remove-Item -Recurse -Force $codex }
    }
}
if (-not (Test-Path $dllsRoot)) {
    throw "Workshop dlls root missing: $dllsRoot (create/subscribe item $WorkshopContentId or pass -SeedFromWorkshopContentId)"
}
Write-Host "Deploy target Workshop content id: $WorkshopContentId" -ForegroundColor Cyan
Write-Host "  $workshopRoot" -ForegroundColor DarkCyan

# Abort if game is running (DLL locked / hot-load unsafe).
$game = Get-Process -Name "LibraryOfRuina" -ErrorAction SilentlyContinue
if ($game) {
    throw "LibraryOfRuina is running (PID $($game.Id)). Exit the game before deploy."
}

$msbuildCandidates = @(
    "D:\VisualStudio\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
)
$msb = $msbuildCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $msb -and -not $SkipBuild) {
    throw "MSBuild not found. Pass -SkipBuild if DLL is already built."
}

$csproj = Join-Path $projectRoot "RogueLike Mod Reborn.csproj"
if (-not $SkipBuild) {
    Write-Host "Building $Configuration ..." -ForegroundColor Cyan
    & $msb $csproj /p:Configuration=$Configuration /v:m /nologo
    if ($LASTEXITCODE -ne 0) { throw "Build failed ($LASTEXITCODE)" }
}

$builtDll = Join-Path $projectRoot "bin\$Configuration\RogueLike Mod Reborn.dll"
if (-not (Test-Path $builtDll)) {
    throw "Built DLL missing: $builtDll"
}

$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupDir = Join-Path $workshopRoot "Assemblies\_codex_backups\deploy_$stamp"
New-Item -ItemType Directory -Force -Path $backupDir | Out-Null

function Backup-IfExists([string]$path) {
    if (Test-Path $path) {
        $rel = $path.Substring($dllsRoot.Length).TrimStart('\', '/')
        $dest = Join-Path $backupDir $rel
        $parent = Split-Path $dest -Parent
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
        Copy-Item $path $dest -Force
    }
}

function Copy-Tree([string]$src, [string]$dst) {
    if (-not (Test-Path $src)) {
        Write-Host "  skip missing: $src" -ForegroundColor Yellow
        return
    }
    New-Item -ItemType Directory -Force -Path $dst | Out-Null
    # robocopy: mirror content of folder, exclude bak/pre backups
    & robocopy $src $dst /E /NFL /NDL /NJH /NJS /nc /ns /np `
        /XF *.bak *.pre_* *~ `
        /XD _codex_backups .git | Out-Null
    # robocopy exit 0-7 are success
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed $src -> $dst (code $LASTEXITCODE)" }
    $global:LASTEXITCODE = 0
}

Write-Host "Backing up previous runtime DLL..." -ForegroundColor Cyan
Backup-IfExists (Join-Path $dllsRoot "RogueLike Mod Reborn.dll")

Write-Host "Copying DLL..." -ForegroundColor Cyan
Copy-Item $builtDll (Join-Path $dllsRoot "RogueLike Mod Reborn.dll") -Force

Write-Host "Syncing data trees (AddData / Localize / SpecialStaticInfo / StoryInfo / ArtWork)..." -ForegroundColor Cyan
$trees = @(
    @{ Src = "AddData"; Dst = "AddData" },
    @{ Src = "Localize"; Dst = "Localize" },
    @{ Src = "SpecialStaticInfo"; Dst = "SpecialStaticInfo" },
    @{ Src = "StoryInfo"; Dst = "StoryInfo" },
    @{ Src = "ArtWork"; Dst = "ArtWork" }
)
foreach ($t in $trees) {
    $src = Join-Path $projectRoot $t.Src
    $dst = Join-Path $dllsRoot $t.Dst
    Write-Host "  $($t.Src) -> $($t.Dst)"
    Copy-Tree $src $dst
}

Write-Host "Quarantining leftover .bak / .pre_* under dlls (not deleted from backups)..." -ForegroundColor Cyan
$quarantine = Join-Path $backupDir "quarantined_bak"
New-Item -ItemType Directory -Force -Path $quarantine | Out-Null
$moved = 0
Get-ChildItem $dllsRoot -Recurse -File -ErrorAction SilentlyContinue | Where-Object {
    $_.Name -match '\.bak$' -or $_.Name -match '\.pre_' -or $_.Name -like '*~'
} | ForEach-Object {
    $rel = $_.FullName.Substring($dllsRoot.Length).TrimStart('\', '/')
    $dest = Join-Path $quarantine $rel
    $parent = Split-Path $dest -Parent
    New-Item -ItemType Directory -Force -Path $parent | Out-Null
    Move-Item $_.FullName $dest -Force
    $moved++
}
Write-Host "  moved $moved backup-style files to $quarantine"

# --- StageModInfo hygiene (fan work / forked items) ---
# RMR must keep InvitationFile.* Exist="false". Exist="true" + empty Data stubs
# makes LoR open a broken vanilla invitation instead of the DLL hub.
# Local deployment uses StageModInfo.local.xml in deploy_local.ps1.
# This Workshop deployment uses StageModInfo.fanwork.xml so the subscribed item stays distinct.
Write-Host "Fixing StageModInfo (DLL-only invitation, Workshop title)..." -ForegroundColor Cyan
$stageLocal = Join-Path $scriptDir "StageModInfo.local.xml"
$stageFanwork = Join-Path $scriptDir "StageModInfo.fanwork.xml"
$stageTarget = Join-Path $workshopRoot "StageModInfo.xml"
$stageTemplate = $stageFanwork
if (Test-Path $stageTemplate) {
    Copy-Item $stageTemplate $stageTarget -Force
    # Stamp build time into Description so local vs older deploys are easier to tell apart.
    try {
        $stampLine = Get-Date -Format "yyyy-MM-dd HH:mm"
        $xml = Get-Content $stageTarget -Raw -Encoding UTF8
        if ($xml -match '<Description>(.*?)</Description>') {
            $desc = $Matches[1]
            if ($desc -notmatch 'deployed=') {
                $desc = "$desc | deployed=$stampLine id=$WorkshopContentId"
            }
            else {
                $desc = $desc -replace 'deployed=[^|<\s]+', "deployed=$stampLine"
            }
            $xml = $xml -replace '<Description>.*?</Description>', "<Description>$desc</Description>"
            $utf8 = New-Object System.Text.UTF8Encoding $false
            [IO.File]::WriteAllText($stageTarget, $xml, $utf8)
        }
        Write-Host "  StageModInfo from: $(Split-Path $stageTemplate -Leaf) (Workshop listing title)" -ForegroundColor Green
    }
    catch {
        Write-Host "  WARNING: could not stamp StageModInfo description: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}
else {
    Write-Host "  WARNING: StageModInfo.fanwork.xml missing; skipping StageModInfo fix." -ForegroundColor Yellow
}
# Nested StageModInfo under "mod infos" are author packaging notes — do not expose as mods.
Get-ChildItem $workshopRoot -Recurse -Filter "StageModInfo.xml" -ErrorAction SilentlyContinue |
    Where-Object { $_.DirectoryName -ne $workshopRoot } |
    ForEach-Object {
        $disabled = $_.FullName + ".not_a_mod"
        if (-not (Test-Path $disabled)) {
            Move-Item $_.FullName $disabled -Force
            Write-Host "  disabled nested: $($_.FullName)" -ForegroundColor Yellow
        }
        else {
            Remove-Item $_.FullName -Force
            Write-Host "  removed nested (already disabled): $($_.FullName)" -ForegroundColor Yellow
        }
    }

$srcHash = (Get-FileHash $builtDll -Algorithm SHA256).Hash
$dstHash = (Get-FileHash (Join-Path $dllsRoot "RogueLike Mod Reborn.dll") -Algorithm SHA256).Hash
if ($srcHash -ne $dstHash) {
    throw "DLL hash mismatch after copy! src=$srcHash dst=$dstHash"
}

$bytes = [IO.File]::ReadAllBytes((Join-Path $dllsRoot "RogueLike Mod Reborn.dll"))
$u = [Text.Encoding]::Unicode.GetString($bytes)
$build = [regex]::Match($u, "2026-\d{2}-\d{2}T[\w\+\-\.]+")
Write-Host ""
Write-Host "Deploy OK" -ForegroundColor Green
Write-Host "  Workshop content id: $WorkshopContentId"
Write-Host "  Path: $workshopRoot"
Write-Host "  DLL SHA256: $dstHash"
Write-Host "  Build stamp (from DLL strings): $($build.Value)"
Write-Host "  StageModInfo: $stageTarget"
Write-Host "  Backup: $backupDir"
Write-Host "  Next: fully restart Library of Ruina; confirm Player.log Build: line matches."
Write-Host "  NOTE: Steam may re-download and OVERWRITE this folder until you upload to Workshop cloud." -ForegroundColor Yellow
