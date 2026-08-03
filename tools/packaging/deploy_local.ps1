# Deploy RMR for independent local testing.
# This never writes into Steam Workshop content and is safe from Steam unsubscribe/sync cleanup.
# Usage (from project root):
#   powershell -ExecutionPolicy Bypass -File .\tools\packaging\deploy_local.ps1 -Configuration Release
#   powershell -ExecutionPolicy Bypass -File .\tools\packaging\deploy_local.ps1 -Configuration Release -Clean

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$SkipBuild,
    [switch]$Clean,
    [string]$GameRoot = "E:\Steam\steamapps\common\Library Of Ruina"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptDir)
if (-not (Test-Path (Join-Path $projectRoot "RogueLike Mod Reborn.csproj"))) {
    throw "Project root not found from $scriptDir"
}

$modsRoot = Join-Path $GameRoot "LibraryOfRuina_Data\Mods"
if (-not (Test-Path $modsRoot)) {
    throw "Library Of Ruina local Mods directory not found: $modsRoot"
}

$localModName = "RMR_REBORN_LOCAL"
$localModRoot = Join-Path $modsRoot $localModName
$expectedRoot = Join-Path ([IO.Path]::GetFullPath($modsRoot).TrimEnd('\', '/')) $localModName
if ([IO.Path]::GetFullPath($localModRoot) -ne $expectedRoot) {
    throw "Refusing to deploy outside the dedicated local RMR directory: $localModRoot"
}
$dllsRoot = Join-Path $localModRoot "Assemblies\dlls"
$workshopSeed = "E:\Steam\steamapps\workshop\content\1256670\3743867841"

# Never replace DLLs while the game can have them loaded.
$game = Get-Process -Name "LibraryOfRuina" -ErrorAction SilentlyContinue
if ($game) {
    throw "LibraryOfRuina is running (PID $($game.Id)). Exit the game before deploy."
}

if ($Clean) {
    if (-not (Test-Path -LiteralPath $workshopSeed)) {
        throw "Clean local deployment requires the author Workshop package as the immutable resource seed: $workshopSeed"
    }
    if (Test-Path -LiteralPath $localModRoot) {
        Write-Host "Removing the existing dedicated local deployment..." -ForegroundColor Yellow
        Remove-Item -LiteralPath $localModRoot -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $localModRoot | Out-Null
    Write-Host "Seeding clean runtime resources from author Workshop item 3743867841..." -ForegroundColor Cyan
    & robocopy $workshopSeed $localModRoot /MIR /NFL /NDL /NJH /NJS /nc /ns /np `
        /XF *.bak *.old *.orig *.tmp *.pre_* *.pdb *~ `
        /XD _codex_backups .git | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "Clean resource seed failed (robocopy code $LASTEXITCODE)" }
    $global:LASTEXITCODE = 0
}

$msbuildCandidates = @(
    "D:\VisualStudio\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
)
$msb = $msbuildCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $msb -and -not $SkipBuild) {
    throw "MSBuild not found. Pass -SkipBuild only if the DLL is already built."
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

function Copy-Tree([string]$src, [string]$dst) {
    if (-not (Test-Path $src)) {
        Write-Host "  skip missing: $src" -ForegroundColor Yellow
        return
    }
    New-Item -ItemType Directory -Force -Path $dst | Out-Null
    # The target is the dedicated RMR_REBORN_LOCAL directory. Mirror only known runtime trees.
    & robocopy $src $dst /MIR /NFL /NDL /NJH /NJS /nc /ns /np `
        /XF *.bak *.pre_* *~ `
        /XD _codex_backups .git | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed $src -> $dst (code $LASTEXITCODE)" }
    $global:LASTEXITCODE = 0
}

Write-Host "Deploy target LOCAL TEST mod:" -ForegroundColor Cyan
Write-Host "  $localModRoot" -ForegroundColor DarkCyan
New-Item -ItemType Directory -Force -Path $dllsRoot | Out-Null

Write-Host "Copying DLL..." -ForegroundColor Cyan
Copy-Item $builtDll (Join-Path $dllsRoot "RogueLike Mod Reborn.dll") -Force

Write-Host "Syncing runtime data trees..." -ForegroundColor Cyan
$trees = @("AddData", "Localize", "SpecialStaticInfo", "StoryInfo", "ArtWork")
foreach ($tree in $trees) {
    Write-Host "  $tree"
    Copy-Tree (Join-Path $projectRoot $tree) (Join-Path $dllsRoot $tree)
}

$stageTemplate = Join-Path $scriptDir "StageModInfo.local.xml"
$stageTarget = Join-Path $localModRoot "StageModInfo.xml"
if (-not (Test-Path $stageTemplate)) {
    throw "Local StageModInfo template missing: $stageTemplate"
}
Copy-Item $stageTemplate $stageTarget -Force

$srcHash = (Get-FileHash $builtDll -Algorithm SHA256).Hash
$dstDll = Join-Path $dllsRoot "RogueLike Mod Reborn.dll"
$dstHash = (Get-FileHash $dstDll -Algorithm SHA256).Hash
if ($srcHash -ne $dstHash) {
    throw "DLL hash mismatch after local copy! src=$srcHash dst=$dstHash"
}

$pollution = @(Get-ChildItem -LiteralPath $localModRoot -Recurse -File | Where-Object {
    $_.Name -match '(?i)(\.bak|\.old|\.orig|\.tmp|~|\.pre_[^.]*)$' -or
    $_.FullName -match '(?i)\\_codex_backups\\|\\\.git\\' -or
    $_.Extension -eq '.pdb'
})
if ($pollution.Count -gt 0) {
    throw "Local deployment contains excluded backup/debug files: $($pollution.FullName -join '; ')"
}

$cecilPath = Join-Path $projectRoot "dependencies\Mono.Cecil.dll"
if (-not (Test-Path -LiteralPath $cecilPath)) {
    throw "Cannot verify deployed BuildTimestamp because Mono.Cecil is missing: $cecilPath"
}
[void][Reflection.Assembly]::LoadFrom($cecilPath)
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($dstDll)
try {
    $coreType = $assembly.MainModule.Types | Where-Object { $_.Name -eq "RMRCore" } | Select-Object -First 1
    $buildField = $coreType.Fields | Where-Object { $_.Name -eq "BuildTimestamp" } | Select-Object -First 1
    $build = [string]$buildField.Constant
}
finally {
    $assembly.Dispose()
}
if ([string]::IsNullOrWhiteSpace($build)) {
    throw "Could not read BuildTimestamp from deployed DLL: $dstDll"
}
Write-Host ""
Write-Host "Local deploy OK" -ForegroundColor Green
Write-Host "  List entry: [LOCAL TEST] RMR REBORN fan work"
Write-Host "  Path: $localModRoot"
Write-Host "  DLL SHA256: $dstHash"
Write-Host "  Build stamp (from DLL metadata): $build"
Write-Host "  IMPORTANT: if the Workshop RMR item is also listed, activate only one RMR entry at a time."
Write-Host "  Next: restart Library of Ruina and confirm Player.log Build: line matches."
