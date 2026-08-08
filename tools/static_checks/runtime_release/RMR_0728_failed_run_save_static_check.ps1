$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$rewarding = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $root "abcdcode_LOGLIKE_MOD\RewardingModel.cs")
$saver = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $root "abcdcode_LOGLIKE_MOD\LoguePlayDataSaver.cs")
$patches = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $root "abcdcode_Refactored\LogLikePatches.cs")

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw $Message
    }
}

$rewardStart = $rewarding.IndexOf("public static bool RewardClearStage")
$rewardEnd = $rewarding.IndexOf("#endregion", $rewardStart)
Assert-True ($rewardStart -ge 0 -and $rewardEnd -gt $rewardStart) `
    "Could not isolate RewardClearStage."
$rewardBody = $rewarding.Substring($rewardStart, $rewardEnd - $rewardStart)
Assert-True ($rewardBody.Contains("LoguePlayDataSaver.MarkRunDefeated")) `
    "Party wipe must invalidate Lastest before allowing the reception to end."

Assert-True ($saver.Contains("public static bool RunDefeatPending")) `
    "The save writer needs a defeat latch."
Assert-True ($saver -match 'ShouldRefuseSnapshotWrite[\s\S]*?RunDefeatPending[\s\S]*?return true;') `
    "A defeated run must refuse later autosaves that could recreate Lastest."
Assert-True ($saver.Contains("SavedPartyHasLivingMember")) `
    "Continue eligibility must inspect the saved party."
Assert-True ($saver -match 'CheckPlayerData[\s\S]*?!SavedPartyHasLivingMember[\s\S]*?RemoveData\("Lastest"\)[\s\S]*?return false;') `
    "An all-dead saved party must be deleted and hidden from Continue."

$gameOverStart = $patches.IndexOf("public static void StageController_GameOver")
$gameOverEnd = $patches.IndexOf("[HarmonyPostfix", $gameOverStart)
Assert-True ($gameOverStart -ge 0 -and $gameOverEnd -gt $gameOverStart) `
    "Could not isolate StageController_GameOver."
$gameOverBody = $patches.Substring($gameOverStart, $gameOverEnd - $gameOverStart)
$gameOverInvalidatesDefeat = $gameOverBody.Contains("!iswin") `
    -and $gameOverBody.Contains("!isbackbutton") `
    -and $gameOverBody.Contains("LogLikeMod.CheckStage(true)") `
    -and $gameOverBody.Contains("MarkRunDefeated")
Assert-True $gameOverInvalidatesDefeat `
    "Vanilla GameOver defeat must invalidate the roguelike run even if ClearBattle is skipped (and must ignore isbackbutton aborts)."
Assert-True ($gameOverBody.Contains("MarkRunAbortWithoutDefeat") -or $gameOverBody.Contains("isbackbutton")) `
    "ESC / return-to-title (isbackbutton) must preserve Lastest instead of MarkRunDefeated."

Write-Output "PASS: party wipe deletes Lastest, blocks rewrite, and cannot be continued."
