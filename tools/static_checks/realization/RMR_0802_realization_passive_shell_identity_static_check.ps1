$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = $scriptDir
while ($repoRoot -and -not (Test-Path (Join-Path $repoRoot 'RogueLike Mod Reborn.csproj'))) {
    $repoRoot = Split-Path -Parent $repoRoot
}
if (-not $repoRoot) {
    throw 'Could not locate repository root for static check.'
}

function Read-Text([string]$relativePath) {
    return Get-Content -Raw -Encoding UTF8 (Join-Path $repoRoot $relativePath)
}

$errors = @()
function Assert-Match([string]$name, [string]$text, [string]$pattern) {
    if ($text -notmatch $pattern) {
        $script:errors += $name
        Write-Host "[FAIL] $name" -ForegroundColor Red
        return
    }
    Write-Host "[OK] $name" -ForegroundColor Green
}

function Assert-NoMatch([string]$name, [string]$text, [string]$pattern) {
    if ($text -match $pattern) {
        $script:errors += $name
        Write-Host "[FAIL] $name" -ForegroundColor Red
        return
    }
    Write-Host "[OK] $name" -ForegroundColor Green
}

$core = Read-Text 'RMR_Core.cs'
$models = Read-Text 'abcdcode_LOGLIKE_MOD\LogueBookModels.cs'
$patches = Read-Text 'abcdcode_Refactored\LogLikePatches.cs'
$prepare = Read-Text 'RMR_PrepareRestrictions.cs'
$realization = Read-Text 'RMR_RealizationManager.cs'

Assert-Match 'internal librarian shell classifier exists' $core `
    'IsInternalLibrarianShell\s*\(\s*LorId\s+id\s*\)'
Assert-Match 'permanent Compendium rejects internal librarian shells' $core `
    'ShouldRecordRoleBookInPermanentCompendium[\s\S]*?!IsInternalLibrarianShell\s*\(\s*page\.id\s*\)'
Assert-Match 'prepare inventory hides internal librarian shells' $prepare `
    'FilterRealizationCompendiumBooks[\s\S]*?IsInternalLibrarianShell\s*\(\s*book\.ClassInfo\.id\s*\)'

$fallbackMethod = [regex]::Match(
    $realization,
    'EnsureDefaultRealizationCompendiumUnlocks\(\)(?<body>[\s\S]*?)private static bool ApplyCompendiumOnlyLoadout').Groups['body'].Value
Assert-NoMatch 'realization fallback does not persist internal shell ids' $fallbackMethod `
    'CompendiumUnlockedRoleBooks\.Add'
Assert-Match 'realization uses local shell fallback only when no collectible core page exists' $realization `
    'atlasBooks\.Count\s*==\s*0[\s\S]*?IsInternalLibrarianShell'

Assert-Match 'player key-page XML is detached before mutation' $models `
    'DetachPlayerBookClassInfo[\s\S]*?SetXmlInfo\s*\(\s*CopyBookXmlInfo'
$equipBlock = [regex]::Match(
    $models,
    'EquipNewPage\(UnitBattleDataModel model, BookXmlInfo page, bool keepSuc = false\)(?<body>[\s\S]*?)public static void ApplyPlayerStat').Groups['body'].Value
Assert-Match 'core-page equip detaches the mutable player shell' $equipBlock `
    'DetachPlayerBookClassInfo\s*\(\s*unitData\s*\)'
Assert-Match 'book XML clone preserves RMR shell identity' $models `
    'CopyBookXmlInfo[\s\S]*?_id\s*=\s*original\._id[\s\S]*?workshopID\s*=\s*original\.workshopID'

$allBooksBlock = [regex]::Match(
    $patches,
    'BookInventoryModel_GetBookListAll\(ref List<BookModel> __result\)(?<body>[\s\S]*?)BookInventoryModel_GetBookList_PassiveEquip').Groups['body'].Value
Assert-Match 'passive ownership lookup includes equipped RMR librarian books' $allBooksBlock `
    'LogueBookModels\.playerModel[\s\S]*?unit\?\.bookItem'
Assert-Match 'passive ownership lookup still limits inventory to RMR projection' $allBooksBlock `
    'FilterRealizationCompendiumBooks\s*\(\s*LogueBookModels\.booklist\s*\)'
Assert-Match 'librarian info rendering resolves projected unit before vanilla SetData' $patches `
    'HarmonyPrefix[\s\S]*?UIBattleSettingLibrarianInfoPanel_SetData_Resolve[\s\S]*?ref UnitDataModel data[\s\S]*?ResolveRoguelikeUnitData'
Assert-Match 'route snapshot captures inherited passive models before realization mutation' $realization `
    'InheritedPassives[\s\S]*?GetPassiveModelList\(\)[\s\S]*?GetSaveDataPassiveModel'
Assert-Match 'route restore rebuilds inherited passive models after restoring the key page' $realization `
    'EquipNewPage\(unit, book\.ClassInfo, false\)[\s\S]*?LoadFromSaveDataPassiveModel\(savedPassive\)'
Assert-Match 'route snapshot restores passive donor book ownership' $realization `
    'RouteBookOwnerSnapshot[\s\S]*?owner\.Key\?\.SetOwner\(owner\.Value\)'

if ($errors.Count -gt 0) {
    Write-Host "`nRMR 0802 realization passive/shell identity check failed: $($errors -join '; ')" -ForegroundColor Red
    exit 1
}

Write-Host "`nRMR 0802 realization passive/shell identity check passed." -ForegroundColor Green
