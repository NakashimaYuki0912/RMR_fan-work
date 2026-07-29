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
$requiredFiles = @(
  "RMR_AbnormalityUnlocks.cs",
  "RMR_MysteryEvents.cs",
  "abcdcode_Refactored\LogLikePatches.cs",
  "abcdcode_LOGLIKE_MOD\RewardingModel.cs",
  "abcdcode_LOGLIKE_MOD\LogueBookModels.cs",
  "Localize\cn\UIs.txt",
  "SpecialStaticInfo\MysteryXmlInfos\chAll_mysterys.xml"
)

foreach ($file in $requiredFiles) {
  if (!(Test-Path (Join-Path $root $file))) {
    throw "Missing required file: $file"
  }
}

$unlock = Get-Content (Join-Path $root "RMR_AbnormalityUnlocks.cs") -Raw
foreach ($pattern in @(
  "class RMRAbnormalityUnlockManager",
  "AbnormalityBattleRewardCount",
  "MysteryRewardCount",
  "GetUnlockedEmotionCardsForBattle",
  "EnqueueRewardSelections",
  "TryEnqueueBattleClearRewardsAfterVictory",
  "RecordPermanentClear",
  "ResetArchiveProgress",
  "GetNoAbnormalityFallback"
)) {
  if ($unlock -notmatch [regex]::Escape($pattern)) {
    throw "Unlock manager missing $pattern"
  }
}

foreach ($pattern in @(
  '"ScorchedGirl", "HappyTeddyBear", "FairyCarnival", "QueenBee"',
  '"ForsakenMurderer", "LittleHelper", "SingingMachine", "Butterfly"',
  '"ShyLookToday", "RedShoes", "SpiderBud", "Laetitia"',
  '"UniverseZogak", "ChildofGalaxy", "Porccubus", "Alriune"',
  '"QueenOfHatred", "KnightOfDespair", "Greed", "Angry"',
  '"Redhood", "BigBadWolf", "Mountain", "Nosferatu"',
  '"ScareCrow", "LumberJack", "House", "Ozma"',
  '"BloodBath", "HeartofAspiration", "Pinocchio", "TheSnowQueen"',
  '"Bigbird", "SmallBird", "LongBird"',
  '"Bloodytree", "Clock", "BlueStar"'
)) {
  if ($unlock -notmatch [regex]::Escape($pattern)) {
    throw "Abnormality tier grouping missing or reordered: $pattern"
  }
}

$simpleBlock = [regex]::Match($unlock, 'SimpleRoots\s*=\s*\{(?<body>.*?)\};', 'Singleline').Groups['body'].Value
foreach ($forbidden in @("Redhood", "BigBadWolf", "Mountain", "Nosferatu", "BloodBath", "Pinocchio", "TheSnowQueen", "Bloodytree", "Clock", "BlueStar")) {
  if ($simpleBlock -match [regex]::Escape($forbidden)) {
    throw "Simple tier incorrectly contains later-floor abnormality root: $forbidden"
  }
}

$patches = Get-Content (Join-Path $root "abcdcode_Refactored\LogLikePatches.cs") -Raw
foreach ($pattern in @(
  "RMRAbnormalityUnlockManager.GetUnlockedEmotionCardsForBattle",
  "RewardingModel.ResetBattleVictoryConfirmation",
  "RMRAbnormalityUnlockManager.RecordPermanentClear",
  "RMRAbnormalityUnlockManager.OnEmotionPagePicked",
  "StageType.Creature"
)) {
  if ($patches -notmatch [regex]::Escape($pattern)) {
    throw "Patch missing $pattern"
  }
}

$rewarding = Get-Content (Join-Path $root "abcdcode_LOGLIKE_MOD\RewardingModel.cs") -Raw
foreach ($pattern in @(
  "RMRAbnormalityUnlockManager.GetNoAbnormalityFallback",
  "RMRAbnormalityUnlockManager.TryEnqueueBattleClearRewardsAfterVictory"
)) {
  if ($rewarding -notmatch [regex]::Escape($pattern)) {
    throw "RewardingModel missing $pattern"
  }
}

$logue = Get-Content (Join-Path $root "abcdcode_LOGLIKE_MOD\LogueBookModels.cs") -Raw
foreach ($pattern in @(
  "RMRAbnormalityUnlockManager.StartNewRoute",
  "RMRAbnormalityUnlockManager.SaveRouteUnlocks",
  "RMRAbnormalityUnlockManager.LoadRouteUnlocks"
)) {
  if ($logue -notmatch [regex]::Escape($pattern)) {
    throw "LogueBookModels missing $pattern"
  }
}

$core = Get-Content (Join-Path $root "RMR_Core.cs") -Raw
foreach ($pattern in @(
  "RMRAbnormalityUnlockManager.ResetArchiveProgress"
)) {
  if ($core -notmatch [regex]::Escape($pattern)) {
    throw "Core missing $pattern"
  }
}

$events = Get-Content (Join-Path $root "RMR_MysteryEvents.cs") -Raw
foreach ($pattern in @(
  "MysteryModel_RMR_AbnoBlackForest",
  "MysteryModel_RMR_AbnoWell",
  "MysteryModel_RMR_AbnoHandsOfLight",
  "RMRAbnormalityUnlockManager.EnqueueRewardSelections"
)) {
  if ($events -notmatch [regex]::Escape($pattern)) {
    throw "Mystery events missing $pattern"
  }
}

$ui = Get-Content (Join-Path $root "Localize\cn\UIs.txt") -Raw
foreach ($pattern in @(
  "Stage_Creature",
  "Stage_Creature_Desc"
)) {
  if ($ui -notmatch [regex]::Escape($pattern)) {
    throw "Missing UI localize $pattern"
  }
}

$mysteryXml = Get-Content (Join-Path $root "SpecialStaticInfo\MysteryXmlInfos\chAll_mysterys.xml") -Raw
foreach ($pattern in @(
  "RMR_AbnoBlackForest",
  "RMR_AbnoWell",
  "RMR_AbnoHandsOfLight",
  "RMR_AbnoEvent_BlackForest",
  "RMR_AbnoEvent_Well",
  "RMR_AbnoEvent_HandsOfLight"
)) {
  if ($mysteryXml -notmatch [regex]::Escape($pattern)) {
    throw "Mystery XML missing $pattern"
  }
}

$earlyStageFiles = @("Stage_ch1.xml", "Stage_ch2.xml")
foreach ($stageFile in $earlyStageFiles) {
  $stageText = Get-Content (Join-Path $root "SpecialStaticInfo\StagesXmlInfos\$stageFile") -Raw
  if ($stageText -match 'StageType="Creature"') {
    throw "$stageFile should not contain Creature stage before chapter 3"
  }
  if ($stageText -notmatch 'StageType="Rest"') {
    throw "$stageFile missing Rest replacement stage"
  }
}

$creatureStageFiles = @("Stage_ch3.xml", "Stage_ch4.xml", "Stage_ch5.xml", "Stage_ch6.xml")
foreach ($stageFile in $creatureStageFiles) {
  $stageText = Get-Content (Join-Path $root "SpecialStaticInfo\StagesXmlInfos\$stageFile") -Raw
  if ($stageText -notmatch 'StageType="Creature"') {
    throw "$stageFile missing Creature stage"
  }
}
"ABNORMALITY UNLOCK STATIC CHECK PASSED"

