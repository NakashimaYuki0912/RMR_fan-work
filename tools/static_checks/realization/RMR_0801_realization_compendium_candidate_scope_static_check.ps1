$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$core = [IO.File]::ReadAllText((Join-Path $repoRoot "RMR_Core.cs"), [Text.Encoding]::UTF8)
$prepare = [IO.File]::ReadAllText((Join-Path $repoRoot "RMR_PrepareRestrictions.cs"), [Text.Encoding]::UTF8)
$books = [IO.File]::ReadAllText((Join-Path $repoRoot "abcdcode_LOGLIKE_MOD\LogueBookModels.cs"), [Text.Encoding]::UTF8)
$patches = [IO.File]::ReadAllText((Join-Path $repoRoot "abcdcode_Refactored\LogLikePatches.cs"), [Text.Encoding]::UTF8)

function Require-Match([string]$text, [string]$pattern, [string]$message) {
    if ($text -notmatch $pattern) {
        throw $message
    }
}

Require-Match $core `
    'public\s+static\s+bool\s+IsBlueReverberationBattlePage\s*\(\s*LorId\s+id\s*\)' `
    'Argalia battle-page identity must have one shared RMRCore classifier.'
Require-Match $prepare `
    'GetContentChapter\s*\(\s*DiceCardXmlInfo\s+card\s*\)[\s\S]*?IsBlueReverberationBattlePage\s*\(\s*card\.id\s*\)[\s\S]*?return\s+ChapterUrbanStar' `
    'Argalia reward pages must use the same Urban Star realization tier as the Argalia core page.'
Require-Match $books `
    'IsBlueReverberationRewardBattleCard\s*\(\s*LorId\s+id\s*\)[\s\S]*?return\s+RMRCore\.IsBlueReverberationBattlePage\s*\(\s*id\s*\)' `
    'Exclusive-card pruning and realization chapter filtering must share Argalia page identity.'

Require-Match $prepare `
    'GetRealizationMaxChapter\s*\(\s*SephirahType\s+floor\s*\)[\s\S]*?case\s+SephirahType\.Malkuth[\s\S]*?case\s+SephirahType\.Yesod[\s\S]*?case\s+SephirahType\.Hod[\s\S]*?case\s+SephirahType\.Netzach[\s\S]*?return\s+ChapterUrbanStar\s*;[\s\S]*?default\s*:[\s\S]*?return\s+ChapterImpurity\s*;' `
    'The first four realizations must cap at Urban Star; later realizations must allow Impurity.'
Require-Match $prepare `
    'IsBookAllowedInCurrentPrepare\s*\(\s*BookXmlInfo\s+book\s*\)[\s\S]*?GetRealizationMaxChapter\s*\(\s*GetPrepareFloor\s*\(\s*\)\s*\)[\s\S]*?GetContentChapter\s*\(\s*book\s*\)\s*<=\s*max' `
    'Realization key pages must enforce the floor chapter cap.'
Require-Match $prepare `
    'FilterRealizationCompendiumBooks\s*\(\s*List<BookModel>\s+source\s*\)[\s\S]*?IsBookAllowedInCurrentPrepare\s*\(\s*book\.ClassInfo\s*\)' `
    'Realization key-page candidates must combine the RMR inventory source with the floor chapter cap.'

$equipBooksMatch = [regex]::Match(
    $patches,
    'BookInventoryModel_GetBookList_equip\s*\([\s\S]*?\n\s*\}')
if (-not $equipBooksMatch.Success) {
    throw 'Could not isolate the realization GetBookList_equip hook.'
}
Require-Match $equipBooksMatch.Value `
    'FilterRealizationCompendiumBooks\s*\(\s*LogueBookModels\.booklist\s*\)' `
    'The equip-page editor must use the RMR inventory / Compendium projection.'
if ($equipBooksMatch.Value -match 'FilterEquipInventoryBooks|GetRealizationMaxChapter|IsBookAllowedInCurrentPrepare') {
    throw 'The realization equip-page editor must not reject unlocked Chapter 7 key pages by chapter.'
}

$allBooksMatch = [regex]::Match(
    $patches,
    'BookInventoryModel_GetBookListAll\s*\([\s\S]*?\n\s*\}[\s\S]*?\n\s*\[HarmonyPostfix,\s*HarmonyPatch\(typeof\(BookInventoryModel\),\s*nameof\(BookInventoryModel\.GetBookList_PassiveEquip\)\)\]')
if (-not $allBooksMatch.Success) {
    throw 'Could not isolate the realization GetBookListAll hook.'
}
Require-Match $allBooksMatch.Value `
    'FilterRealizationCompendiumBooks\s*\(\s*LogueBookModels\.booklist\s*\)' `
    'Every realization core-page editor must use the RMR inventory / Compendium projection.'
if ($allBooksMatch.Value -match '\.defaultBook') {
    throw 'Realization GetBookListAll must not mix vanilla/default librarian books into the Compendium projection.'
}
if ($allBooksMatch.Value -match 'FilterEquipInventoryBooks|GetRealizationMaxChapter|IsBookAllowedInCurrentPrepare') {
    throw 'Realization core/passive editors must not reject unlocked Chapter 7 key pages by chapter.'
}

$passiveBooksMatch = [regex]::Match(
    $patches,
    'BookInventoryModel_GetBookList_PassiveEquip\s*\([\s\S]*?\n\s*\}')
if (-not $passiveBooksMatch.Success) {
    throw 'Could not isolate the realization GetBookList_PassiveEquip hook.'
}
Require-Match $passiveBooksMatch.Value `
    'FilterRealizationCompendiumBooks\s*\(\s*LogueBookModels\.booklist\s*\)' `
    'The passive editor must use the RMR inventory / Compendium projection.'
if ($passiveBooksMatch.Value -match 'FilterEquipInventoryBooks|GetRealizationMaxChapter|IsBookAllowedInCurrentPrepare') {
    throw 'The realization passive editor must not reject unlocked Chapter 7 key pages by chapter.'
}

Require-Match $patches `
    'UIInvenCardSlot_SetSlotState[\s\S]*?UnitDataModel\s+currentUnit\s*=\s*UI\.UIController\.Instance\?\.CurrentUnit;[\s\S]*?currentUnit\?\.bookItem\?\.ClassInfo\s*==\s*null[\s\S]*?orig\(self\);[\s\S]*?return;[\s\S]*?currentUnit\.GetDeckAll\(\)' `
    'Card-slot state must guard a missing CurrentUnit before calling GetDeckAll during list refresh.'

Write-Output 'PASS: realization core/card candidate scope and card-slot refresh guards are aligned.'
