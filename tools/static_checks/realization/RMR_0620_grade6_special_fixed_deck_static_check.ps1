$ErrorActionPreference = 'Stop'


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
$models = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'abcdcode_LOGLIKE_MOD\LogueBookModels.cs')
$patches = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'abcdcode_Refactored\LogLikePatches.cs')
$mod = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'abcdcode_LOGLIKE_MOD\LogLikeMod.cs')

$errors = @()

function Assert-Match([string]$name, [string]$text, [string]$pattern) {
    if ($text -notmatch $pattern) {
        $script:errors += $name
        Write-Host "[FAIL] $name" -ForegroundColor Red
    }
    else {
        Write-Host "[OK] $name" -ForegroundColor Green
    }
}

function Assert-NoMatch([string]$name, [string]$text, [string]$pattern) {
    if ($text -match $pattern) {
        $script:errors += $name
        Write-Host "[FAIL] $name" -ForegroundColor Red
    }
    else {
        Write-Host "[OK] $name" -ForegroundColor Green
    }
}

Assert-Match 'special fixed-deck recognition requires DeckFixed' $models 'DeckFixed'
Assert-Match 'Black Silence is identified explicitly' $models 'BlackSilence'
Assert-Match 'Binah is identified explicitly' $models 'Binah'
Assert-Match 'Binah fixed deck falls back to vanilla deck 8' $models 'deckId\s*=\s*8'
Assert-Match 'Black Silence fixed deck falls back to vanilla deck 102' $models 'deckId\s*=\s*102'
Assert-Match 'Binah degraded-to-full upgrade map exists' $models '607201,\s*706201'
Assert-Match 'Binah upgrade applies to fixed deck loads' $models 'ApplyBinahCardUpgradesInPlace'
Assert-Match 'Binah upgrade state is saved' $models 'binahUpgradedCards'
$fixedDeckBody = [regex]::Match($models, 'IsGrade6SpecialBuiltInDeckPage\(BookXmlInfo page\)(?<body>[\s\S]*?)public static bool IsUrbanStarSpecialCorePage').Groups['body'].Value
Assert-NoMatch 'Red Mist is not treated as a fixed built-in deck page' $fixedDeckBody 'IsRedMistCorePage'
Assert-Match 'Blue Reverberation is explicitly excluded from fixed-deck pages' $fixedDeckBody 'IsBlueReverberationCorePage\(page\)[\s\S]*return false'
Assert-Match 'Blue Reverberation editable deck helper exists' $models 'IsEditableBlueReverberationDeck'
Assert-Match 'stale Blue Reverberation fixed deck source is cleared' $models 'RMRCore\.IsBlueReverberationCorePage\(model\.bookItem\?\.ClassInfo\)[\s\S]*Grade6SpecialBuiltInDeckSource\.Remove\(model\)'
Assert-Match 'legacy Blue Reverberation fixed deck source is normalized' $models 'NormalizeLegacyBlueReverberationCorePageId\(sourceId\)'
Assert-Match 'Blue Reverberation can bypass vanilla blue primary deck lock in RMR' $models 'IsLockByBluePrimary\(\)\s*&&\s*!editableBlue'
Assert-Match 'special deck source is tracked per unit' $models 'Dictionary<UnitDataModel,\s*LorId>.*BuiltInDeck'
Assert-Match 'special deck is loaded from source page id' $models 'DeckXmlList.*GetData\(sourceId\)'
Assert-Match 'special deck can fall back to source page OnlyCard list' $models 'TryLoadBuiltInDeckFromOnlyCards'
Assert-Match 'missing DeckId stores the source page id' $models 'sourceId\s*=\s*page\.id'
Assert-Match 'special deck source is saved' $models 'Grade6SpecialBuiltInDeckSource.*LogGetSaveData'
Assert-Match 'special deck source is restored' $models 'Grade6SpecialBuiltInDeckSource'
$resolveBody = [regex]::Match($models, 'TryResolveGrade6SpecialBuiltInDeckSource\(BookXmlInfo page, out LorId sourceId\)(?<body>[\s\S]*?)private static bool TryGetKnownVanillaFixedDeckSource').Groups['body'].Value
if ($resolveBody.IndexOf('TryGetKnownVanillaFixedDeckSource') -lt 0 -or $resolveBody.IndexOf('TryGetKnownVanillaFixedDeckSource') -gt $resolveBody.IndexOf('LorId deckId = page.DeckId')) {
    $errors += 'known vanilla fixed-deck fallback must run before hooked DeckId'
    Write-Host '[FAIL] known vanilla fixed-deck fallback must run before hooked DeckId' -ForegroundColor Red
}
else {
    Write-Host '[OK] known vanilla fixed-deck fallback runs before hooked DeckId' -ForegroundColor Green
}
Assert-Match 'special fixed deck is applied when equipping the core page' $models 'TrySetAndApplyGrade6SpecialBuiltInDeckSource'
Assert-Match 'special fixed deck is written to the real DeckModel deck' $models 'TryApplyGrade6SpecialBuiltInDeckToUnit[\s\S]*TryGetActualCurrentDeck[\s\S]*currentDeck\.Clear\(\)[\s\S]*currentDeck\.AddRange\(builtInDeck\)'
Assert-Match 'special fixed deck restore reapplies the real deck' $models 'RestoreGrade6SpecialBuiltInDeckSource[\s\S]*TryApplyGrade6SpecialBuiltInDeckToUnit\(unitData,\s*sourceId\)'
Assert-Match 'special fixed deck skips normal deck pruning after equip' $models 'if\s*\(appliedBuiltInDeck\)\s*return;'
Assert-Match 'battle deck hook reads special fixed deck first' $patches 'TryGetGrade6SpecialBuiltInDeckCards'
Assert-Match 'BookModel IsFixedDeck hook exists' $patches 'BookModel_IsFixedDeck'
Assert-Match 'BookModel deck display hook exists' $patches 'BookModel_GetCardListFromCurrentDeck'
$battleDeckHookFirstBranch = [regex]::Match($patches, 'UnitDataModel_GetDeckForBattle[\s\S]*?return builtInDeck;').Value
Assert-NoMatch 'battle deck hook is not gated by battle state' $battleDeckHookFirstBranch 'CheckStage|PendingRealizationBattle|InRealizationBattle'
Assert-NoMatch 'IsFixedDeck hook is not gated by battle state' ([regex]::Match($patches, 'BookModel_IsFixedDeck[\s\S]*?return orig\(self\);').Value) 'CheckStage|PendingRealizationBattle|InRealizationBattle'
Assert-NoMatch 'deck display hook is not gated by battle state' ([regex]::Match($patches, 'BookModel_GetCardListFromCurrentDeck[\s\S]*?return orig\(self\);').Value) 'CheckStage|PendingRealizationBattle|InRealizationBattle'
Assert-Match 'IsFixedDeck hook lets editable Blue Reverberation stay editable' $patches 'IsEditableBlueReverberationDeck\(self\)[\s\S]*return false'
Assert-Match 'IsFixedDeck hook is registered' $mod 'CreateHook\(typeof\(BookModel\),\s*"IsFixedDeck"'
Assert-Match 'deck display hook is registered' $mod 'CreateHook\(typeof\(BookModel\),\s*"GetCardListFromCurrentDeck"'
Assert-NoMatch 'OnlyCard is not treated as a generic fixed deck' $models 'OnlyCard list defines the book''s exclusive deck'

if ($errors.Count -gt 0) {
    Write-Host "`nRMR 0620 special fixed-deck check failed: $($errors -join '; ')" -ForegroundColor Red
    exit 1
}

Write-Host "`nRMR 0620 special fixed-deck check passed." -ForegroundColor Green

