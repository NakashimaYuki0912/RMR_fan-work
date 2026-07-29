$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$stagePath = Join-Path $root "abcdcode_LOGLIKE_MOD\LogLikeMod.cs"
$patchesPath = Join-Path $root "abcdcode_Refactored\LogLikePatches.cs"
$stage = Get-Content -Raw -Encoding UTF8 -LiteralPath $stagePath
$patches = Get-Content -Raw -Encoding UTF8 -LiteralPath $patchesPath

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw $Message
    }
}

$setStart = $stage.IndexOf("public static void SetNextStage")
$setEnd = $stage.IndexOf("public static void RunStartBattleWithCurrentNodeDefinition", $setStart)
Assert-True ($setStart -ge 0 -and $setEnd -gt $setStart) `
    "Could not isolate SetNextStage/current-node scope helper."
$setBody = $stage.Substring($setStart, $setEnd - $setStart)
Assert-True (-not $setBody.Contains("stageModel.ClassInfo.mapInfo =")) `
    "SetNextStage must not replace shared StageClassInfo.mapInfo."
Assert-True (-not $setBody.Contains("stageModel.ClassInfo.mapInfo.Clear()")) `
    "SetNextStage must not clear shared StageClassInfo.mapInfo."

$scopeStart = $stage.IndexOf("public static void RunStartBattleWithCurrentNodeDefinition")
$scopeEnd = $stage.IndexOf("public static void TryApplyStageFloorOnly", $scopeStart)
Assert-True ($scopeStart -ge 0 -and $scopeEnd -gt $scopeStart) `
    "Temporary StartBattle stage-definition scope is missing."
$scopeBody = $stage.Substring($scopeStart, $scopeEnd - $scopeStart)
Assert-True ($scopeBody.Contains("try") -and $scopeBody.Contains("finally")) `
    "Temporary stage definition must restore state in finally."
Assert-True ($scopeBody.Contains("originalStageType") -and $scopeBody.Contains("originalMapInfo")) `
    "Temporary stage definition must preserve controller type and shared mapInfo."
Assert-True ($scopeBody.Contains("stageTypeField.SetValue(controller, originalStageType)")) `
    "StageController._stageType must be restored after StartBattle initialization."
Assert-True ($scopeBody.Contains("shellInfo.mapInfo = originalMapInfo")) `
    "Shared StageClassInfo.mapInfo must be restored after StartBattle initialization."

Assert-True ($patches.Contains("RunStartBattleWithCurrentNodeDefinition(self, () => orig(self))")) `
    "The StartBattle hook must execute vanilla initialization inside the temporary definition scope."
Assert-True ($patches -match 'curstagetype\s*==\s*StageType\.Creature[\s\S]{0,500}keep creature map') `
    "The StartBattle map postfix must preserve the creature map after the temporary scope."

Write-Output "PASS: selected node stage type/map are temporary StartBattle inputs and cannot pollute another run."
