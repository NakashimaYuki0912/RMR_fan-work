$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = $scriptDir
while ($repoRoot -and -not (Test-Path (Join-Path $repoRoot 'RogueLike Mod Reborn.csproj'))) {
    $repoRoot = Split-Path -Parent $repoRoot
}
if (-not $repoRoot) {
    throw 'Could not locate repository root for static check.'
}
Set-Location $repoRoot

function Read-Utf8([string]$Path) {
    return Get-Content -LiteralPath $Path -Raw -Encoding UTF8
}

function Assert-Match([string]$Text, [string]$Pattern, [string]$Message) {
    if ($Text -notmatch $Pattern) {
        throw $Message
    }
}

function Assert-NotMatch([string]$Text, [string]$Pattern, [string]$Message) {
    if ($Text -match $Pattern) {
        throw $Message
    }
}

$saveMgr = Read-Utf8 'abcdcode_LOGLIKE_MOD\LogueSaveManager.cs'
$patches = Read-Utf8 'abcdcode_Refactored\LogLikePatches.cs'
$core = Read-Utf8 'RMR_Core.cs'
$emotion = Read-Utf8 'abcdcode_LOGLIKE_MOD\PickUpModel_RMRVanillaEmotion.cs'

Assert-Match $saveMgr `
    'File\.Replace\(|tempPath|\.tmp' `
    'LogueSaveManager.SaveData must write via temp then replace (never truncate destination first).'
Assert-Match $saveMgr `
    'catch \(Exception ex\)[\s\S]*?QuarantineCorrupt|Quarantining corrupt' `
    'LogueSaveManager.LoadData must catch deserialize failures and quarantine corrupt files.'
Assert-Match $saveMgr `
    'AddToObtainCount[\s\S]*?catch \(Exception ex\)' `
    'AddToObtainCount must swallow failures so combat UI can close.'
Assert-NotMatch $saveMgr `
    'using \(FileStream serializationStream = File\.Create\(\$"\{LogueSaveManager\.Saveroot\}/\{savename\}"\)\)' `
    'SaveData must not File.Create the destination path directly (truncates on crash).'

Assert-Match $patches `
    'else if \(target != null\)' `
    'OnPickPassiveCard must use else-if so All targets are not also applied as SelectOne.'
Assert-Match $patches `
    'OnPickPassiveCard apply failed \(UI must still close\)' `
    'OnPickPassiveCard must catch apply exceptions so LevelUpUI can finish.'
Assert-Match $patches `
    'EmotionTargetType\.SelectOne[\s\S]*?null target' `
    'SelectOne with null target must be logged and skipped, not applied to everyone.'

Assert-Match $emotion `
    'Intentionally empty|Per-librarian apply lives in OnPickUp\(BattleUnitModel\)' `
    'RMRVanillaEmotion parameterless OnPickUp must stay empty (no mass ally apply).'

Assert-NotMatch $core `
    'File\.Create\(LogueSaveManager\.Saveroot \+ "/RMR_ItemCatalog"\)' `
    'RMR_ItemCatalog bootstrap must not truncate via File.Create; use LogueSaveManager.SaveData.'
Assert-Match $core `
    'LoadData\("RMR_ItemCatalog"\);\s*\r?\n\s*if \(catalog == null\)' `
    'GetItemObtainCount/HasBeenObtained must null-check LoadData before GetInt.'

Write-Output 'PASS: save atomicity + emotion SelectOne softlock guards are present.'
