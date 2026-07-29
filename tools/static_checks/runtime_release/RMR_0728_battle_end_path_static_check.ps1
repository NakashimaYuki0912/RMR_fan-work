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

function Assert-Ordered([string]$Text, [string]$First, [string]$Second, [string]$Message) {
    $firstIndex = $Text.IndexOf($First, [StringComparison]::Ordinal)
    $secondIndex = $Text.IndexOf($Second, [StringComparison]::Ordinal)
    if ($firstIndex -lt 0 -or $secondIndex -lt 0 -or $firstIndex -ge $secondIndex) {
        throw $Message
    }
}

$purple = Read-Utf8 'abcdcode_LOGLIKE_MOD\PassiveAbility_250227Log.cs'
$patches = Read-Utf8 'abcdcode_Refactored\LogLikePatches.cs'
$rewarding = Read-Utf8 'abcdcode_LOGLIKE_MOD\RewardingModel.cs'
$realization = Read-Utf8 'RMR_RealizationManager.cs'
$mysterySweeper = Read-Utf8 'abcdcode_LOGLIKE_MOD\PassiveAbility_Mystery1_4_Sweeper.cs'
$mysteryThreeRounds = Read-Utf8 'abcdcode_LOGLIKE_MOD\PassiveAbility_Mystery4_3_1.cs'

# Purple Tear: never consume the one-time phase flag until a real destination exists.
# Otherwise a no-op/failed floor change makes the first form killable and the game
# reports a false victory instead of entering phase two.
Assert-Match $purple 'if \(availableFloorList\.Count == 0\)[\s\S]*?return;' `
    'Purple Tear transition must abort before committing state when no destination floor exists.'
Assert-Ordered $purple 'if (availableFloorList.Count == 0)' 'LogLikeMod.purpleexcept = true;' `
    'Purple Tear transition state must be committed only after destination validation.'
Assert-Match $purple 'catch \(Exception ex\)[\s\S]*?purpleexcept = false;[\s\S]*?param2 = 0;' `
    'Purple Tear transition must roll back its state if ChangeFloorForcely fails.'

# Central combat-end pipeline: a spurious EndBattle or EndBattlePhase while both
# factions are alive must be refused/recovered, and realization phases must not
# be recorded as a final clear.
Assert-Match $patches 'StageController_EndBattle[\s\S]*?IsLiveCombatBothSidesAlive\(\)[\s\S]*?Ignoring EndBattle while both sides still alive' `
    'StageController.EndBattle must refuse live-combat termination.'
Assert-Match $patches 'StageController_EndBattlePhase[\s\S]*?IsLiveCombatBothSidesAlive\(\)[\s\S]*?RoundStartPhase_System' `
    'EndBattlePhase must recover to combat while both factions are alive.'
Assert-Match $rewarding 'RewardClearStage[\s\S]*?IsLiveCombatBothSidesAlive\(\)[\s\S]*?return false;' `
    'RewardClearStage must refuse live-combat termination.'
Assert-Match $rewarding 'TryEndRunAfterAllRewards refused: combat is still live' `
    'Terminal reward cleanup must not end a still-live combat.'
Assert-Match $patches 'IsMidRealizationMultiPhase\(\)[\s\S]*?keep realization active' `
    'Realization EndBattle must preserve active multi-phase fights.'
Assert-Match $realization 'EnemyStageManager[\s\S]*?!mgr\.IsStageFinishable\(\)[\s\S]*?return true;' `
    'Realization phase detection must respect EnemyStageManager.IsStageFinishable.'

# Event combats intentionally end after their scripted timer, but they still need
# the sticky non-combat exit marker so the next-stage selection cannot leak back
# into the residual event wave.
Assert-Match $mysterySweeper 'MarkNonCombatNodeExit\("MysterySweeperTimer"\)' `
    'Sweeper timed mystery exit must mark the intentional non-combat end.'
Assert-Match $mysteryThreeRounds 'MarkNonCombatNodeExit\("MysteryThreeRoundTimer"\)' `
    'Three-round mystery exit must mark the intentional non-combat end.'

Write-Output 'PASS: battle-end paths distinguish real victory, phase transitions, and intentional event exits.'
