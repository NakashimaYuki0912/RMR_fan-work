$ErrorActionPreference = 'Stop'


$script:StaticCheckScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$script:RepoRoot = $script:StaticCheckScriptDir
while ($script:RepoRoot -and -not (Test-Path (Join-Path $script:RepoRoot 'RogueLike Mod Reborn.csproj'))) {
    $script:RepoRoot = Split-Path -Parent $script:RepoRoot
}
if (-not $script:RepoRoot) {
    throw 'Could not locate repository root for static check.'
}
Set-Location $script:RepoRoot
$root = $script:RepoRoot
function Read-Text([string]$relativePath) {
    return Get-Content -Raw -Encoding UTF8 (Join-Path $root $relativePath)
}

$core = Read-Text 'RMR_Core.cs'
$models = Read-Text 'abcdcode_LOGLIKE_MOD\LogueBookModels.cs'
$saver = Read-Text 'abcdcode_LOGLIKE_MOD\LoguePlayDataSaver.cs'
$patches = Read-Text 'abcdcode_Refactored\LogLikePatches.cs'
$unlocks = Read-Text 'RMR_AbnormalityUnlocks.cs'
$errors = @()

function Assert-Match([string]$label, [string]$text, [string]$pattern) {
    if ($text -notmatch $pattern) {
        $script:errors += $label
        Write-Host "[FAIL] $label" -ForegroundColor Red
    }
    else { Write-Host "[OK] $label" -ForegroundColor Green }
}

function Assert-NoMatch([string]$label, [string]$text, [string]$pattern) {
    if ($text -match $pattern) {
        $script:errors += $label
        Write-Host "[FAIL] $label" -ForegroundColor Red
    }
    else { Write-Host "[OK] $label" -ForegroundColor Green }
}

$grade6Grant = [regex]::Match(
    $core,
    'public static void EnsureGrade6SpecialCorePagesUnlocked\(\)(?<body>[\s\S]*?)public static bool IsBinahRedMistChallengeUnlocked'
).Groups['body'].Value
$temporaryGrant = [regex]::Match(
    $core,
    'public static void PrepareBinahForRedMistChallenge\(\)(?<body>[\s\S]*?)public static'
).Groups['body'].Value

Assert-Match 'Binah progression has an independent route-local flag' $unlocks 'BinahUnlockedForCurrentRoute'
Assert-Match 'Grade6 entry still grants Black Silence' $grade6Grant 'blackSilence\.id'
Assert-NoMatch 'Grade6 entry no longer grants Binah' $grade6Grant 'TryAddUniqueRoleBookToInventoryAndAtlas\s*\(\s*binah\.id'
Assert-Match 'Selecting a stage reapplies Binah challenge progression' $patches 'SetNextStage\s*\(\s*stage\.Id\s*,\s*stage\.type\s*\)\s*;[\s\S]*ApplyBinahRedMistProgressionState'
Assert-Match 'Temporary Binah access adds only to current booklist' $temporaryGrant 'EnsureRoleBookInCurrentBooklist\s*\(\s*binah\.id\s*\)'
Assert-NoMatch 'Temporary Binah access does not write permanent atlas' $temporaryGrant 'TryAddUniqueRoleBookToInventoryAndAtlas|RecordAtlasRoleBook'
Assert-Match 'Permanent Compendium gates Binah until Red Mist victory' $core 'ShouldRecordRoleBookInPermanentCompendium[\s\S]*IsRedMistChallengeVictoryRecorded'
Assert-Match 'Save loading reapplies Binah challenge progression state' $saver 'ApplyBinahRedMistProgressionState'
Assert-Match 'Red Mist victory unlocks Binah for the current route' $core 'UnlockBinahForCurrentRoute\s*\(\s*\)'
Assert-Match 'Red Mist victory records Binah in permanent Compendium' $core 'UnlockBinahAfterRedMistVictory[\s\S]*TryAddUniqueRoleBookToInventoryAndCompendium\s*\(\s*binah\.id\s*\)'
Assert-Match 'Binah remains recognized as a fixed-deck core page' $models 'bool isBinah'

if ($errors.Count -gt 0) {
    Write-Host "`nRMR 0620 Binah progression check failed: $($errors -join '; ')" -ForegroundColor Red
    exit 1
}

Write-Host "`nRMR 0620 Binah progression check passed." -ForegroundColor Green

