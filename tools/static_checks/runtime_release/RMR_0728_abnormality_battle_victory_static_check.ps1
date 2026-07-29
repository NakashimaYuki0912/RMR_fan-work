$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$patchesPath = Join-Path $root "abcdcode_Refactored\LogLikePatches.cs"
$rewardingPath = Join-Path $root "abcdcode_LOGLIKE_MOD\RewardingModel.cs"
$abnormalityPath = Join-Path $root "RMR_AbnormalityUnlocks.cs"
$stagePath = Join-Path $root "abcdcode_LOGLIKE_MOD\LogLikeMod.cs"

$patches = Get-Content -Raw -Encoding UTF8 -LiteralPath $patchesPath
$rewarding = Get-Content -Raw -Encoding UTF8 -LiteralPath $rewardingPath
$abnormality = Get-Content -Raw -Encoding UTF8 -LiteralPath $abnormalityPath
$stage = Get-Content -Raw -Encoding UTF8 -LiteralPath $stagePath

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$startBattleStart = $patches.IndexOf("private void StageController_StartBattle_Inner")
$startBattleEnd = $patches.IndexOf("public void StageController_CreateLibrarianUnit", $startBattleStart)
Assert-True ($startBattleStart -ge 0 -and $startBattleEnd -gt $startBattleStart) `
    "Could not isolate StageController_StartBattle_Inner."
$startBattleBody = $patches.Substring($startBattleStart, $startBattleEnd - $startBattleStart)

Assert-True (-not $startBattleBody.Contains("EnqueueBattleClearRewards();")) `
    "Battle-clear abnormality rewards must not be enqueued during StartBattle."
Assert-True ($startBattleBody.Contains("ResetBattleVictoryConfirmation();")) `
    "StartBattle must reset the current-battle victory confirmation."
Assert-True ($startBattleBody.Contains("ObserveLivingEnemyForVictoryConfirmation();")) `
    "StartBattle must observe spawned enemies after vanilla initialization."
Assert-True ($startBattleBody.Contains("RunStartBattleWithCurrentNodeDefinition(self, () => orig(self));")) `
    "StartBattle must initialize through the selected node's temporary vanilla stage definition."

$rewardClearStart = $rewarding.IndexOf("public static bool RewardClearStage")
$rewardClearEnd = $rewarding.IndexOf("#endregion", $rewardClearStart)
Assert-True ($rewardClearStart -ge 0 -and $rewardClearEnd -gt $rewardClearStart) `
    "Could not isolate RewardClearStage."
$rewardClearBody = $rewarding.Substring($rewardClearStart, $rewardClearEnd - $rewardClearStart)

Assert-True ($rewardClearBody.Contains("HasConfirmedBattleVictory()")) `
    "Combat rewards must require a confirmed victory from the current battle."
Assert-True ($rewardClearBody.Contains("TryEnqueueBattleClearRewardsAfterVictory()")) `
    "Abnormality rewards must be enqueued only from the confirmed-victory path."
Assert-True ($abnormality.Contains("BattleClearRewardsEnqueuedThisBattle")) `
    "Battle-clear reward enqueueing must be idempotent within one battle."

Assert-True ($stage.Contains("public static void RunStartBattleWithCurrentNodeDefinition(")) `
    "The temporary StageController/StageModel definition helper is missing."
Assert-True ($stage.Contains("typeof(StageController).GetField(`"_stageType`", AccessTools.all)")) `
    "StageController private _stageType temporary definition is missing."
Assert-True ($stage.Contains("finally") -and $stage.Contains("stageTypeField.SetValue(controller, originalStageType)")) `
    "StageController private _stageType must be restored after vanilla StartBattle initialization."

Write-Output "PASS: abnormality rewards require this battle's victory and creature definitions are scoped to StartBattle initialization."
