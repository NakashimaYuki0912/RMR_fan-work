$ErrorActionPreference = "Stop"


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
function Read-Text($relativePath) {
    [System.IO.File]::ReadAllText((Join-Path $root $relativePath), [System.Text.Encoding]::UTF8)
}

function Read-Xml($relativePath) {
    [xml](Read-Text $relativePath)
}

function Require-Contains($content, $pattern, $label) {
    if ($content -notmatch [regex]::Escape($pattern)) {
        throw "Missing ${label}: $pattern"
    }
}

function Require-True($condition, $message) {
    if (-not $condition) {
        throw $message
    }
}

$logLikeMod = Read-Text "abcdcode_LOGLIKE_MOD\LogLikeMod.cs"
foreach ($pattern in @(
    "[RMR SetNextStage] Stage not found",
    "[RMR SetNextStage] Stage has no wave data",
    "RunStartBattleWithCurrentNodeDefinition",
    "originalMapInfo",
    "shellInfo.mapInfo = originalMapInfo"
)) {
    Require-Contains $logLikeMod $pattern "SetNextStage runtime-end guard"
}

foreach ($locale in @("cn", "en", "kr")) {
    $ui = Read-Text "Localize\$locale\UIs.txt"
    Require-Contains $ui 'id="ui_Realization"' "$locale realization localization"
    [xml]$ui | Out-Null
}

$stageCh7 = Read-Xml "SpecialStaticInfo\StagesXmlInfos\Stage_ch7.xml"
$stageInfoCh7 = Read-Xml "AddData\StageInfo\StageInfo_ch7.xml"
$stageInfoCh7Event = Read-Xml "AddData\StageInfo\StageInfo_ch7event.xml"
$dropValuesCh7 = Read-Xml "SpecialStaticInfo\DropValueXmlInfos\values_ch7.txt"
$cardDropTableCh7 = Read-Xml "AddData\CardDropTable\CardDropTable_ch7.xml"
$dropBookCh7 = Read-Xml "AddData\DropBook\Dropbook_ch7.xml"
$equipRewardCh7 = Read-Xml "SpecialStaticInfo\RewardPassiveInfos\EquipReward_ch7.xml"
$mysteryCh7 = Read-Xml "SpecialStaticInfo\MysteryXmlInfos\ch7_mysterys.xml"
$bookModels = Read-Text "abcdcode_LOGLIKE_MOD\LogueBookModels.cs"
$rewarding = Read-Text "abcdcode_LOGLIKE_MOD\RewardingModel.cs"
$mysteryCh1Script = Read-Text "abcdcode_LOGLIKE_MOD\MysteryModel_Mystery_Ch1_1.cs"
$mysteryChX3Script = Read-Text "abcdcode_LOGLIKE_MOD\MysteryModel_Mystery_ChX_3.cs"
$mysteryChX4Script = Read-Text "abcdcode_LOGLIKE_MOD\MysteryModel_Mystery_ChX_4.cs"
$abnormalityUnlocks = Read-Text "RMR_AbnormalityUnlocks.cs"
$realizationManager = Read-Text "RMR_RealizationManager.cs"

$routeStages = @($stageCh7.StagesXmlRoot.ChapterList.StageList)
Require-True (@($routeStages | Where-Object { $_.GetAttribute("StageType") -eq "Normal" }).Count -eq 10) "Grade7 must route the ten vanilla Reverberation Ensemble member battles as normal nodes."
Require-True (@($routeStages | Where-Object { $_.GetAttribute("StageType") -eq "Boss" }).Count -eq 2) "Grade7 must have exactly two full boss-flow candidates."
Require-True (@($routeStages | Where-Object { $_.GetAttribute("StageType") -eq "Creature" }).Count -eq 1) "Grade7 must have one abnormality battle node."
foreach ($normalId in @("70001", "70002", "70003", "70004", "70005", "70006", "70007", "70008", "70009", "70010")) {
    Require-True ($routeStages | Where-Object { $_.GetAttribute("ID") -eq $normalId -and $_.GetAttribute("StageType") -eq "Normal" }) "Grade7 route must include Reverberation Ensemble normal stage $normalId."
}
foreach ($bossId in @("70020", "70021")) {
    Require-True ($routeStages | Where-Object { $_.GetAttribute("ID") -eq $bossId -and $_.GetAttribute("StageType") -eq "Boss" }) "Grade7 route must include Impuritas boss candidate $bossId."
}
foreach ($splitBossId in @("70008", "70009", "70010")) {
    Require-True (-not ($routeStages | Where-Object { $_.GetAttribute("ID") -eq $splitBossId -and $_.GetAttribute("StageType") -eq "Boss" })) "Grade7 route must not use split Reverberation Ensemble layer $splitBossId as a boss."
}

$stageInfoIds = @($stageInfoCh7.StageXmlRoot.Stage | ForEach-Object { [string]$_.id })
foreach ($routeStage in $routeStages | Where-Object { $_.GetAttribute("StageType") -in @("Normal", "Boss") }) {
    $routeStageId = $routeStage.GetAttribute("ID")
    Require-True ($stageInfoIds -contains $routeStageId) "Routed Grade7 battle stage $routeStageId has no StageInfo entry."
    $stageInfo = $stageInfoCh7.StageXmlRoot.Stage | Where-Object { [string]$_.id -eq $routeStageId } | Select-Object -First 1
    Require-True (@($stageInfo.Wave).Count -gt 0) "Routed Grade7 battle stage $routeStageId has no waves."
    Require-True ((@($stageInfo.Wave.Unit) | Where-Object { $_ }).Count -gt 0) "Routed Grade7 battle stage $routeStageId has no enemy units."
}

$stage70001 = $stageInfoCh7.StageXmlRoot.Stage | Where-Object { [string]$_.id -eq "70001" } | Select-Object -First 1
$stage70010 = $stageInfoCh7.StageXmlRoot.Stage | Where-Object { [string]$_.id -eq "70010" } | Select-Object -First 1
$stage70020 = $stageInfoCh7.StageXmlRoot.Stage | Where-Object { [string]$_.id -eq "70020" } | Select-Object -First 1
$stage70021 = $stageInfoCh7.StageXmlRoot.Stage | Where-Object { [string]$_.id -eq "70021" } | Select-Object -First 1
Require-True (@($stage70001.Wave.Unit) -contains "1301011") "Grade7 normal stage 70001 must be Philip's Reverberation Ensemble battle."
Require-True (@($stage70010.Wave.Unit) -contains "1310011") "Grade7 normal stage 70010 must be Argalia's Reverberation Ensemble battle."
Require-True (($stage70020.Wave.ManagerScript | Select-Object -First 1) -eq "BlackSilence") "Grade7 boss 70020 must use the full Black Silence manager."
Require-True (@($stage70021.Wave).Count -eq 3) "Grade7 boss 70021 must keep the full three-wave Distorted Reverberation Ensemble flow."
Require-True (($stage70021.Wave | Where-Object { $_.ManagerScript -eq "TwistedReverberationBand_Middle" }) -ne $null) "Grade7 boss 70021 must include the middle-layer manager script."
Require-Contains $logLikeMod "for (int i = 0; i < data.waveList.Count; i++)" "SetNextStage must enqueue all waves instead of only wave 0."
Require-Contains $logLikeMod "wave={i + 1}/{data.waveList.Count}" "SetNextStage must log multi-wave impurity routing."
Require-Contains $logLikeMod "waveListField?.SetValue(stageModel, stageWaveModelList)" "SetNextStage must recover a missing private wave list instead of null-refing."
Require-True ($logLikeMod -notmatch "stageModel\.ClassInfo\.mapInfo\s*=") "SetNextStage must not persist selected mapInfo into the shared reception shell."
Require-Contains $logLikeMod "StageClassInfo shellInfo = model?.ClassInfo" "Temporary StartBattle definition must guard a missing StageModel.ClassInfo."

$event70011 = $stageInfoCh7Event.StageXmlRoot.Stage | Where-Object { [string]$_.id -eq "70011" } | Select-Object -First 1
$event70012 = $stageInfoCh7Event.StageXmlRoot.Stage | Where-Object { [string]$_.id -eq "70012" } | Select-Object -First 1
Require-True (($event70011.Wave.Unit | Select-Object -First 1) -eq "28370003") "Grade7 mystery shell 70011 must keep the mod event enemy."
Require-True (($event70012.Wave.Unit | Select-Object -First 1) -eq "28370004") "Grade7 mystery shell 70012 must keep the mod event enemy."
Require-Contains $logLikeMod "IsVanillaImpurityBattleStage" "Impuritas vanilla package restoration must be explicitly limited to vanilla battle stages."
Require-Contains $logLikeMod "stageId >= 70001 && stageId <= 70010" "Impuritas vanilla package restoration must include the ten original Ensemble member battles."
Require-Contains $logLikeMod "stageId == 70020" "Impuritas vanilla package restoration must include the Black Silence boss."
Require-Contains $logLikeMod "stageId == 70021" "Impuritas vanilla package restoration must include the Distorted Ensemble boss."
Require-True ($logLikeMod -notmatch "stageClassInfo\.id\.id >= 70001\s*&&\s*stageClassInfo\.id\.id <= 70021") "Impuritas package restoration must not strip the mod package from 70011/70012 event shell enemies."

foreach ($dropValueId in @("7001", "7002", "7004", "17001")) {
    Require-True ($dropValuesCh7.CardDropValueXmlRoot.DropValue | Where-Object { $_.ID -eq $dropValueId }) "Grade7 drop value $dropValueId missing."
}
Require-True ($cardDropTableCh7.CardDropTableXmlRoot.DropTable | Where-Object { $_.ID -eq "-854601" }) "Grade7 card drop table -854601 missing."
$grade7Cards = @($cardDropTableCh7.CardDropTableXmlRoot.DropTable.Card | ForEach-Object { $_.InnerText })
Require-True ($grade7Cards -contains "704001") "Grade7 card drop table must include Argalia/Reverberation Ensemble combat pages."
Require-True ($grade7Cards -contains "704018") "Grade7 card drop table must include Bremen/Reverberation Ensemble combat pages."
Require-True (-not ($grade7Cards -contains "701001")) "Grade7 card drop table must not be Hana-only after routing Reverberation Ensemble normal stages."
foreach ($bookId in @("7001", "7002", "7003", "7004")) {
    Require-True ($dropBookCh7.BookUseXmlRoot.BookUse | Where-Object { $_.ID -eq $bookId }) "Grade7 drop book $bookId missing."
}
Require-True ($equipRewardCh7.RewardPassivesRoot.ChapterList.Chapter -eq "Grade7") "Grade7 equip reward chapter is missing."
$grade7EquipIds = @($equipRewardCh7.RewardPassivesRoot.ChapterList.RewardList | ForEach-Object { [string]$_.ID })
foreach ($equipId in @("260001", "260002", "260003", "260004")) {
    Require-True ($grade7EquipIds -contains $equipId) "Grade7 equip rewards must include verified vanilla impurity core page $equipId."
}
foreach ($equipId in @("260005", "260006", "260007", "260008", "260009", "260010", "260011", "260012", "260013", "260014")) {
    Require-True (-not ($grade7EquipIds -contains $equipId)) "Grade7 equip rewards must not include Mod Needed candidate $equipId."
}
foreach ($mysteryId in @("70011", "70012")) {
    Require-True ($mysteryCh7.MysteryXmlRoot.Mystery | Where-Object { $_.ID -eq $mysteryId }) "Grade7 mystery $mysteryId must have real MysteryXmlInfo so it is not an empty event."
}
Require-Contains $logLikeMod "RewardingModel.CreateEquipRewardXmlData(info)" "EquipPage reward registration must create core page title/description data."
Require-Contains $logLikeMod "PickUpXml_Dummy_Passive.TryGetValue(packageId" "Reward passive lookup must tolerate package/workshop key differences."
Require-Contains $rewarding "pickUpXml._artwork = data._bookIcon" "EquipPage reward presentation should use book artwork when available."
Require-Contains $bookModels "GetCardItem(cardId) ?? ItemXmlDataList.instance.GetCardItem(cardId, true)" "Permanent battle card unlocks must fall back to package-aware lookup."
Require-Contains $abnormalityUnlocks "LogueBookModels.SavePermanentAtlasUnlocks();" "Distorted Ensemble victory must persist Blue Reverberation battle pages immediately."
Require-Contains $mysteryCh1Script "QueuePlainDropBooks(LogLikeMod.curchaptergrade, 2)" "Current-chapter combat page mystery reward must use the current route chapter."
Require-Contains $mysteryCh1Script "LogLikeMod.curchaptergrade < ChapterGrade.Grade7" "Next-chapter combat page mystery reward must cap at Grade7 instead of hardcoding Grade2."
Require-True ($mysteryCh1Script -notmatch "new LorId\(LogLikeMod\.ModId, 1001\)") "Current-chapter combat page mystery reward must not hardcode Canard drop book 1001."
Require-True ($mysteryCh1Script -notmatch "new LorId\(LogLikeMod\.ModId, 2001\)") "Next-chapter combat page mystery reward must not hardcode Urban Myth drop book 2001."
Require-Contains $mysteryChX3Script "new int[7]" "Chapter-wide mystery reward event must have a Grade7 reward tier."
Require-Contains $mysteryChX3Script "GetChapterRewardIndex()" "Chapter-wide mystery reward event must clamp chapter reward indexes."
Require-Contains $mysteryChX4Script "this.droplist = new List<LorId>()" "One-time shop mystery event must initialize its droplist."
Require-Contains $mysteryChX4Script "all.Count == 0" "One-time shop mystery event must handle empty shop reward candidates."

Require-Contains $realizationManager "EnsureDefaultRealizationAtlasUnlocks" "realization starter atlas fallback"
Require-True ($realizationManager -notmatch "atlasBooks\.AddRange\(RouteBookSnapshot\)") "Realization atlas-only loadout must not fall back to all route core pages."
Require-True ($realizationManager -notmatch "atlasCards\.AddRange\(RouteCardSnapshot\)") "Realization atlas-only loadout must not fall back to all route combat pages."
Require-True ($realizationManager -notmatch "SavePermanentAtlasUnlocks\(\)") "Realization starter fallback must not sync current route inventory into the permanent atlas."
Require-Contains $realizationManager "SavePermanentAtlasData()" "realization starter fallback atlas-only persistence"

"RMR 0628 RUNTIME END AND CH7 STATIC CHECK PASSED"

