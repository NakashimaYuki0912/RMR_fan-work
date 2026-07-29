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

$patches = Read-Utf8 'abcdcode_Refactored\LogLikePatches.cs'
$realization = Read-Utf8 'RMR_RealizationManager.cs'
$restrictions = Read-Utf8 'RMR_PrepareRestrictions.cs'
$models = Read-Utf8 'abcdcode_LOGLIKE_MOD\LogueBookModels.cs'
$upgradeShop = Read-Utf8 'abcdcode_LOGLIKE_MOD\ShopGoods_CardUpgrade.cs'
$core = Read-Utf8 'RMR_Core.cs'

$coinHook = [regex]::Match(
    $patches,
    'public int BattleUnitEmotionDetail_CreateEmotionCoin\((?<body>[\s\S]*?)\n\s*\}'
).Groups['body'].Value
Assert-Match $coinHook `
    'InRealizationBattle[\s\S]*?CreateRealizationEmotionCoinsWithCurrentFloor\s*\(\s*orig\s*,\s*self\s*,\s*coinType\s*,\s*count\s*\)' `
    'Realization librarians must use the full vanilla coin path with a temporary current-floor binding.'
Assert-Match $coinHook `
    'InRealizationBattle[\s\S]*?faction\s*==\s*Faction\.Player[\s\S]*?CreateRealizationEmotionCoinsWithCurrentFloor[\s\S]*?return orig\(self,\s*coinType,\s*count\)' `
    'Realization current-floor binding must be limited to player librarians; enemies keep vanilla behavior.'
Assert-Match $patches `
    'CreateRealizationEmotionCoinsWithCurrentFloor[\s\S]*?_ownerSephirah[\s\S]*?try[\s\S]*?return orig\(self,\s*coinType,\s*count\)[\s\S]*?finally[\s\S]*?SetValue\(\s*unitData\s*,\s*originalFloor\s*\)' `
    'Realization emotion accumulation must restore the projected librarian owner floor after vanilla coin processing.'

Assert-Match $patches `
    'StageLibraryFloorModel_OnPickEgoCard[\s\S]*?\(\s*LogLikeMod\.CheckStage\(true\)\s*\|\|\s*RMRRealizationManager\.InRealizationBattle\s*\)[\s\S]*?GrantEgoIdsToBattleUnits' `
    'Realization E.G.O. picks must enter the RMR callback that grants the selected page to personalEgo.'

$passiveInventoryHook = [regex]::Match(
    $patches,
    'BookInventoryModel_GetBookList_PassiveEquip\((?<body>[\s\S]*?)\n\s*\}'
).Groups['body'].Value
Assert-Match $passiveInventoryHook `
    'IsRoguelikeBattleSettingContext\(\)' `
    'Passive attribution inventory must recognize vanilla realization-stage prepare.'
Assert-Match $passiveInventoryHook `
    'FilterEquipInventoryBooks\s*\(\s*LogueBookModels\.booklist\s*\)' `
    'Passive attribution must use the Compendium projection with floor chapter limits.'

$allBooksHook = [regex]::Match(
    $patches,
    'BookInventoryModel_GetBookListAll\((?<body>[\s\S]*?)\n\s*\}'
).Groups['body'].Value
Assert-Match $allBooksHook `
    'IsRoguelikeBattleSettingContext\(\)' `
    'Passive Apply must enumerate the realization Compendium books, not vanilla inventory.'

foreach ($lookupHookName in @(
    'BookInventoryModel_GetAllBookByInstanceId',
    'BookInventoryModel_GetBookByInstanceId')) {
    $lookupHook = [regex]::Match(
        $patches,
        [regex]::Escape($lookupHookName) + '\((?<body>[\s\S]*?)\n\s*\}'
    ).Groups['body'].Value
    Assert-Match $lookupHook `
        'IsRoguelikeBattleSettingContext\(\)' `
        "$lookupHookName must resolve projected realization book instance IDs."
    Assert-NoMatch $lookupHook `
        '\.Find\([^\r\n]+\)\.bookItem' `
        "$lookupHookName must not dereference a missing projected librarian."
}

Assert-Match $patches `
    'ResolveRoguelikeUnitData[\s\S]*?bookItem\.instanceId' `
    'Equivalent realization UI units must resolve to the canonical RMR librarian by book instance ID.'
Assert-Match $patches `
    'UIBattleSettingLibrarianInfoPanel_SetData[\s\S]*?ResolveRoguelikeUnitData\s*\(\s*data\s*\)[\s\S]*?UIPassiveSuccessionPopup\.Instance\.SetData\(\s*targetUnit' `
    'Passive succession must edit the canonical RMR librarian, not a transient equivalent UI unit.'
Assert-Match $patches `
    'UnitDataModel_EquipBookForUI[\s\S]*?ResolveRoguelikeUnitData\s*\(\s*__instance\s*\)[\s\S]*?EquipNewPage\(\s*targetBattleModel' `
    'Core-page changes must target the canonical realization battle model.'

Assert-Match $restrictions `
    'IsUrbanStarSpecialCorePage\s*\(\s*book\s*\)[\s\S]*?return ChapterUrbanStar' `
    'Black Silence, Binah, Red Mist, and Argalia must be treated as Urban Star realization books.'
Assert-Match $models `
    'public static bool IsUrbanStarSpecialCorePage[\s\S]*?IsBlackSilenceCorePage[\s\S]*?IsBinahCorePage[\s\S]*?IsRedMistCorePage[\s\S]*?IsBlueReverberationCorePage' `
    'All four special Urban Star core pages must share one realization chapter classifier.'

Assert-Match $upgradeShop `
    'AddCard\s*\(\s*new LorId\(popup\.metadata\.unparsedPid,\s*cardid\.id\)\s*\)[\s\S]*?SavePermanentCompendiumData\(\)' `
    'A purchased normal-page upgrade must be persisted to the permanent Compendium immediately.'
Assert-Match $models `
    'RestoreCompendiumUpgradeDefinitions\(\)' `
    'Permanent upgraded combat-page definitions must be rehydrated before realization loadout creation.'
Assert-Match $realization `
    'RestoreCompendiumUpgradeDefinitions\(\)[\s\S]*?foreach \(LorId id in LogueBookModels\.CompendiumUnlockedBattleCards\)' `
    'Realization must rebuild upgraded page definitions before projecting Compendium battle cards.'
$modelRestoreOccurrences = [regex]::Matches($models, 'RestoreCompendiumUpgradeDefinitions\(\)').Count
$realizationRestoreOccurrences = [regex]::Matches($realization, 'RestoreCompendiumUpgradeDefinitions\(\)').Count
if ($modelRestoreOccurrences -ne 1 -or $realizationRestoreOccurrences -ne 1) {
    throw 'Upgraded-page definitions must be rebuilt only at the realization projection boundary.'
}

Assert-Match $core `
    'UnlockBinahAfterRedMistVictory[\s\S]*?TryAddUniqueRoleBookToInventoryAndCompendium\s*\(\s*binah\.id\s*\)' `
    'Red Mist victory must add Binah to the permanent Compendium as well as the current route.'
Assert-Match $core `
    'ShouldRecordRoleBookInPermanentCompendium[\s\S]*?IsRedMistChallengeVictoryRecorded' `
    'Binah must remain gated before Red Mist victory but become recordable afterward.'
Assert-NoMatch $core `
    'UnlockBinahAfterRedMistVictory[\s\S]*?SavePermanentCompendiumUnlocks\(\)[\s\S]*?ApplyBinahRedMistProgressionState' `
    'Binah victory must persist only the permanent Compendium here, without a broad route-save side effect.'
Assert-NoMatch $core `
    'ApplyBinahRedMistProgressionState[\s\S]*?SavePermanentCompendiumUnlocks\(\)[\s\S]*?NormalizeLegacyBlueReverberationCorePageId' `
    'Binah state replay must not trigger the broad route-save helper.'

Assert-Match $realization `
    'foreach \(LorId id in LogueBookModels\.CompendiumUnlockedRoleBooks\)' `
    'Realization core-page inventory must be projected from permanent Compendium ownership.'

Write-Output 'PASS: realization emotion, passive isolation, upgraded pages, and special Urban Star core-page contracts are present.'
