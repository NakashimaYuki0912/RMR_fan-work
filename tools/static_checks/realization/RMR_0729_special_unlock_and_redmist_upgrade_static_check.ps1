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

$realization = Read-Utf8 'RMR_RealizationManager.cs'
$core = Read-Utf8 'RMR_Core.cs'
$upgrades = Read-Utf8 'abcdcode_Refactored\Upgrades.cs'
$effects = Read-Utf8 'RMR_CardEffects.cs'
$bookModels = Read-Utf8 'abcdcode_LOGLIKE_MOD\LogueBookModels.cs'
[xml]$exclusiveDropTables = Read-Utf8 'AddData\CardDropTable\CardDropTable_exclusives.xml'
[xml]$urbanStarDropTables = Read-Utf8 'AddData\CardDropTable\CardDropTable_ch6.xml'

$loadout = [regex]::Match(
    $realization,
    'private static bool ApplyCompendiumOnlyLoadout\(\)(?<body>[\s\S]*?)\n\s*private static'
).Groups['body'].Value
Assert-Match $loadout `
    'EnsureCompendiumUnlocks\(\)[\s\S]*?RMRCore\.GrantGrade6SpecialCorePagesIfNeeded\(\)[\s\S]*?foreach \(LorId id in LogueBookModels\.CompendiumUnlockedRoleBooks\)' `
    'Realization projection must migrate unlocked Binah and Blue Reverberation rewards before building the Compendium-only inventories.'

Assert-Match $core `
    'EnsureBlueReverberationRewardsForUrbanStar[\s\S]*?foreach \(int pageId in BlueReverberationBattlePageIds\)[\s\S]*?AddCard\(\s*cardId\s*,\s*1\s*,\s*false\s*\)' `
    'Blue Reverberation progression must record all Argalia battle pages in the permanent Compendium.'
foreach ($id in 704001, 704011, 704012, 704013, 704014, 705010, 705011) {
    Assert-Match $core `
        ([regex]::Escape($id.ToString())) `
        "Blue Reverberation battle-page set is missing $id."
}

$redMistUpgrade = [regex]::Match(
    $upgrades,
    'public class UpgradeModel_RedMist5[\s\S]*?\n\s*\}'
).Value
Assert-Match $redMistUpgrade `
    'SetSelfAbility\(\s*"RMR_kaliNormalAttackH"\s*\)' `
    'The upgraded Red Mist horizontal slash must use an upgrade-aware self ability.'

$redMistVerticalUpgrade = [regex]::Match(
    $upgrades,
    'public class UpgradeModel_RedMist3[\s\S]*?\n\s*\}'
).Value
Assert-Match $redMistVerticalUpgrade `
    'SetSelfAbility\(\s*"RMR_kaliNormalAttackJ"\s*\)' `
    'The upgraded Red Mist vertical slash must use an upgrade-aware self ability.'

$redMistThrustUpgrade = [regex]::Match(
    $upgrades,
    'public class UpgradeModel_RedMist4[\s\S]*?\n\s*\}'
).Value
Assert-Match $redMistThrustUpgrade `
    'SetSelfAbility\(\s*"RMR_kaliNormalAttackZ"\s*\)' `
    'The upgraded Red Mist greatsword thrust must use an upgrade-aware self ability.'

Assert-Match $effects `
    'DiceCardSelfAbility_RMR_kaliNormalAttackH[\s\S]*?_totalDamage\s*<\s*8' `
    'The upgraded Red Mist horizontal slash must require at least 8 damage.'
Assert-Match $effects `
    'DiceCardSelfAbility_RMR_kaliNormalAttackH[\s\S]*?usedOriginId\s*=[\s\S]*?GetOriginalId\(\)[\s\S]*?otherId\.GetOriginalId\(\)\s*==\s*usedOriginId[\s\S]*?AddCost\(\s*-1\s*\)[\s\S]*?RecoverPlayPointByCard\(\s*2\s*\)' `
    'The upgraded Red Mist horizontal slash must reduce other same-origin copies and recover 2 Light.'

foreach ($scriptName in 'J', 'Z') {
    Assert-Match $effects `
        "DiceCardSelfAbility_RMR_kaliNormalAttack$scriptName[\s\S]*?_totalDamage\s*<\s*8" `
        "The upgraded Red Mist $scriptName page must require at least 8 damage."
    Assert-Match $effects `
        "DiceCardSelfAbility_RMR_kaliNormalAttack$scriptName[\s\S]*?usedOriginId\s*=[\s\S]*?GetOriginalId\(\)[\s\S]*?otherId\.GetOriginalId\(\)\s*==\s*usedOriginId[\s\S]*?AddCost\(\s*-1\s*\)[\s\S]*?DrawCards\(\s*1\s*\)" `
        "The upgraded Red Mist $scriptName page must reduce other same-origin copies and draw 1 page."
}

$exclusiveTable = @($exclusiveDropTables.CardDropTableXmlRoot.DropTable) |
    Where-Object { [int]$_.ID -eq -999999 } |
    Select-Object -First 1
if ($null -eq $exclusiveTable -or 608004 -notin @($exclusiveTable.Card | ForEach-Object { [int]$_ })) {
    throw 'Mass Individual Disposal (608004) must remain declared in the obtainable exclusive-card drop table.'
}
$urbanStarTable = @($urbanStarDropTables.CardDropTableXmlRoot.DropTable) |
    Where-Object { [int]$_.ID -eq -854501 } |
    Select-Object -First 1
if ($null -eq $urbanStarTable -or 608004 -notin @($urbanStarTable.Card | ForEach-Object { [int]$_ })) {
    throw 'Mass Individual Disposal (608004) must be reachable from the Urban Star battle-page reward pool.'
}
Assert-Match $bookModels `
    'PruneCorePageExclusiveBattleCardsFromInventoryAndAtlas[\s\S]*?RemoveWhere\(\s*IsExplicitlyObtainableExclusiveBattleCard\s*\)' `
    'The exclusive-card pruning pass must preserve explicitly obtainable exclusive battle pages.'
Assert-Match $bookModels `
    'IsExplicitlyObtainableExclusiveBattleCard[\s\S]*?id\.id\s*==\s*608004' `
    'Mass Individual Disposal (608004) must be preserved in inventory and the permanent Compendium.'

Write-Output 'PASS: special-page migration, upgraded Red Mist effects, and obtainable Mass Individual Disposal retention are present.'
