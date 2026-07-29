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
$bs = [char]92
$root = $script:RepoRoot
function Read-Text($rel) { Get-Content (Join-Path $root $rel) -Raw -Encoding UTF8 }
function AssertContains($label, $text, $needle) {
    if ($text -cnotlike "*$needle*") { throw "$label missing: $needle" }
}
function AssertNotContains($label, $text, $needle) {
    if ($text -clike "*$needle*") { throw "$label forbidden: $needle" }
}

$core = Read-Text 'RMR_Core.cs'
$books = Read-Text ('abcdcode_LOGLIKE_MOD' + $bs + 'LogueBookModels.cs')
$unlocks = Read-Text 'RMR_AbnormalityUnlocks.cs'
$restrictions = Read-Text 'RMR_PrepareRestrictions.cs'

# 1. Grade6 special core page grant helper exists
AssertContains 'RMR_Core has GrantGrade6SpecialCorePagesIfNeeded method' $core 'GrantGrade6SpecialCorePagesIfNeeded'

# 2. Only triggered at ChapterGrade.Grade6 / entering Urban Star
AssertContains 'GrantGrade6SpecialCorePagesIfNeeded called in Grade6 switch case' $core 'case ChapterGrade.Grade6:'
AssertContains 'GrantGrade6SpecialCorePagesIfNeeded called in DebugCh6 AfterInitializeGamemode' $core 'RMRCore.GrantGrade6SpecialCorePagesIfNeeded()'

# 3. Logic contains Binah and BlackSilence/Black Silence resolution
AssertContains 'Black Silence TextId=102 lookup' $core 'TextId == 102'
AssertContains 'BlackSilence !IsWorkshop filter' $core '!x.id.IsWorkshop()'
AssertContains 'Binah resolution by CharacterSkin' $core 'CharacterSkin'
AssertContains 'Binah resolution exists' $core 'Binah'

# 4. Uses BookXmlList.GetList() to resolve original pages, not AddData/EquipPage XML
AssertContains 'Uses BookXmlList.GetList()' $core 'BookXmlList>.Instance.GetList()'
AssertNotContains 'Must not modify AddData/EquipPage XML to forge pages' $core 'AddData/EquipPage'

# 5. Uses the current permanent Compendium helper and preserves the current-route copy.
AssertContains 'TryAddUniqueRoleBookToInventoryAndCompendium used' $core 'TryAddUniqueRoleBookToInventoryAndCompendium'
AssertContains 'TryAddUniqueRoleBookToInventoryAndCompendium checks Compendium duplicate' $books 'CompendiumUnlockedRoleBooks.Contains(id)'
AssertContains 'TryAddUniqueRoleBookToInventoryAndCompendium checks booklist duplicate' $books 'booklist.Any'
AssertContains 'TryAddUniqueRoleBookToInventoryAndCompendium validates BookXmlInfo' $books 'GetBookDataOriginAware(id)'

# 6. Red Mist victory records both Gebura and Binah; Argalia resolves to the player page.
AssertContains 'Red Mist victory records Gebura core page' $unlocks 'TryAddUniqueRoleBookToInventoryAndCompendium(redMistBookId)'
AssertContains 'Red Mist victory records Binah core page' $core 'TryAddUniqueRoleBookToInventoryAndCompendium(binah.id)'
AssertContains 'Argalia player core page id is explicit' $core 'BlueReverberationPlayerCorePageId = 260005'
AssertContains 'Urban Star special core page classifier exists' $books 'IsUrbanStarSpecialCorePage'
AssertContains 'Realization chapter filter uses special core classifier' $restrictions 'IsUrbanStarSpecialCorePage(book)'

# 7. One-time flag / dedup to prevent duplicate grants
AssertContains 'Permanent save flag constant exists' $core 'Grade6SpecialCorePagesGrantedSaveName'
AssertContains 'One-time flag check before grant' $core 'RMR_Grade6SpecialCorePagesGranted'

# 8. Does not reference _release_packages
AssertNotContains 'RMR_Core must not reference _release_packages' $core '_release_packages'
AssertNotContains 'LogueBookModels must not reference _release_packages' $books '_release_packages'

Write-Host 'RMR 0616 grade6 special core pages static check passed — all constraints verified.'

