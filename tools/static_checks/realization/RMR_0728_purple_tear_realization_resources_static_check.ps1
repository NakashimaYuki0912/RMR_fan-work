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

function Assert-NoMatch([string]$Text, [string]$Pattern, [string]$Message) {
    if ($Text -match $Pattern) {
        throw $Message
    }
}

$purple = Read-Utf8 'abcdcode_LOGLIKE_MOD\PassiveAbility_250227Log.cs'
$patches = Read-Utf8 'abcdcode_Refactored\LogLikePatches.cs'
$abno = Read-Utf8 'RMR_AbnormalityUnlocks.cs'

# Vanilla PassiveAbility_250227 removes Chesed, Hokma, and the current floor before
# selecting the forced-teleport target. Keeping only the current floor is a no-op
# ChangeFloorForcely call and leaves the half-HP transition waiting forever.
Assert-Match $purple 'RemoveAll\(x => x\.Sephirah == SephirahType\.Chesed\)' `
    'Purple Tear transition must exclude Chesed like vanilla.'
Assert-Match $purple 'RemoveAll\(x => x\.Sephirah == SephirahType\.Hokma\)' `
    'Purple Tear transition must exclude Hokma like vanilla.'
Assert-Match $purple 'RemoveAll\(x => x\.Sephirah == Singleton<StageController>\.Instance\.CurrentFloor\)' `
    'Purple Tear transition must exclude the current floor.'
Assert-NoMatch $purple 'RemoveAll\(x => x\.Sephirah != Singleton<StageController>\.Instance\.CurrentFloor\)' `
    'Purple Tear transition must not keep only the current floor.'

# Realization librarians are RMR-projected units. Preserve vanilla emotion-coin
# semantics, but explicitly give those projected units the normal realization cap.
Assert-Match $patches 'InRealizationBattle\)\s*\{\s*self\.SetMaxEmotionLevel\(5\);\s*return orig\(self, coinType, count\);' `
    'Realization combat must gain vanilla emotion coins with max emotion level 5.'

# Realization must use the permanent Compendium resource pools, not the current route.
Assert-Match $patches 'if \(!RMRRealizationManager\.RealizationCombatLive\)\s*PrepareRealizationCombatResources\(self\)' `
    'Realization StartBattle must initialize Compendium abnormality/E.G.O. resources.'
Assert-Match $patches '!LogLikeMod\.CheckStage\(true\) && !RMRRealizationManager\.InRealizationBattle[\s\S]*?return RewardingModel\.EmotionChoice\(\);' `
    'Realization RoundEnd must use the Compendium-backed abnormality/E.G.O. choice flow.'
Assert-Match $abno 'usePermanentCompendium[\s\S]*?CompendiumUnlockedEgoPages' `
    'Realization mid-battle E.G.O. choices must come from permanent Compendium ownership.'

# Passive attribution must remain wired in realization prepare even when the vanilla
# UI supplies an equivalent UnitDataModel instance rather than the exact list object.
Assert-Match $patches 'UIBattleSettingLibrarianInfoPanel_SetData[\s\S]*?IsRoguelikeBattleSettingContext\(\)[\s\S]*?RemoveAllListeners\(\)' `
    'Passive attribution listeners must be rewired only after confirming RMR/realization context.'
Assert-NoMatch $patches 'IsRoguelikeBattleSettingContext\(\) \|\| !LogueBookModels\.playerModel\.Contains\(data\)' `
    'Passive attribution must not require reference identity with playerModel.'
Assert-Match $patches 'if \(!RMRRealizationManager\.IsRealizationPreparationActive\)\s*LoguePlayDataSaver\.SavePlayData_Menu\(\);' `
    'Realization passive edits must not overwrite the normal route snapshot.'

Write-Output 'PASS: Purple Tear phase transition and realization Compendium resource contracts are present.'
