$ErrorActionPreference = "Stop"

$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..\..")).Path
$abnormalitySource = Get-Content -LiteralPath (Join-Path $repo "RMR_AbnormalityUnlocks.cs") -Raw -Encoding UTF8
$rewardingSource = Get-Content -LiteralPath (Join-Path $repo "abcdcode_LOGLIKE_MOD\RewardingModel.cs") -Raw -Encoding UTF8
$failures = New-Object System.Collections.Generic.List[string]

function Assert-Match([string]$label, [string]$text, [string]$pattern) {
    if ($text -notmatch $pattern) {
        $failures.Add($label)
    }
}

# Heart of Aspiration public reward scripts must match the actual vanilla effect,
# Name and EmotionLevel entries, not the numbered suffix.
Assert-Match "HeartofAspiration1 must resolve to doki (Pulsation)" $abnormalitySource `
    '\{\s*"HeartofAspiration1"\s*,\s*"doki"\s*\}'
Assert-Match "HeartofAspiration2 must resolve to heart (Urging)" $abnormalitySource `
    '\{\s*"HeartofAspiration2"\s*,\s*"heart"\s*\}'
Assert-Match "HeartofAspiration3 must resolve to heart_rush (Fervent Beats)" $abnormalitySource `
    '\{\s*"HeartofAspiration3"\s*,\s*"heart_rush"\s*\}'
Assert-Match "ShyLookToday1 must resolve to shylook (Today's Expression)" $abnormalitySource `
    '\{\s*"ShyLookToday1"\s*,\s*"shylook"\s*\}'
Assert-Match "ShyLookToday2 must resolve to shylook3 (Social Distancing)" $abnormalitySource `
    '\{\s*"ShyLookToday2"\s*,\s*"shylook3"\s*\}'
Assert-Match "ShyLookToday3 must resolve to shylook2 (Shyness)" $abnormalitySource `
    '\{\s*"ShyLookToday3"\s*,\s*"shylook2"\s*\}'
Assert-Match "ShyLookToday2 static tier must be II" $abnormalitySource `
    'Put\("ShyLookToday2",\s*2\)'
Assert-Match "ShyLookToday3 static tier must be I" $abnormalitySource `
    'Put\("ShyLookToday3",\s*1\)'

[xml]$creatureCn = Get-Content -LiteralPath (Join-Path $repo "Localize\cn\CreaturePickUp_Table.xml") -Raw -Encoding UTF8
$creatureText = @{}
foreach ($node in $creatureCn.SelectNodes("//text")) {
    $creatureText[$node.GetAttribute("id")] = $node.InnerText
}
$pulseName = -join @([char]0x8109, [char]0x52A8)
$urgingName = -join @([char]0x6E34, [char]0x671B)
$ferventBeatsName = -join @([char]0x5267, [char]0x70C8, [char]0x640F, [char]0x52A8)
$expectedHeartNames = @{
    "PickUpCreature_HeartofAspiration1_Name" = $pulseName
    "PickUpCreature_HeartofAspiration2_Name" = $urgingName
    "PickUpCreature_HeartofAspiration3_Name" = $ferventBeatsName
}
foreach ($entry in $expectedHeartNames.GetEnumerator()) {
    if ($creatureText[$entry.Key] -ne $entry.Value) {
        $failures.Add("$($entry.Key) must be '$($entry.Value)'")
    }
}

# Reproduce the Myo/Rhino collision from the actual data:
# Myo book 250024 deliberately uses localization TextId 250030, while bare
# localization ID 250024 belongs to the Rhino page.
[xml]$equipCh6 = Get-Content -LiteralPath (Join-Path $repo "AddData\EquipPage\EquipPage_Librarian_ch6.xml") -Raw -Encoding UTF8
$myo = $equipCh6.SelectSingleNode("//Book[@ID='250024']")
if ($null -eq $myo -or [string]$myo.TextId -ne "250030") {
    $failures.Add("Myo book 250024 must retain TextId 250030")
}

function Read-BookNames([string]$language) {
    $names = @{}
    $folder = Join-Path $repo "Localize\$language\BookInfo"
    foreach ($file in Get-ChildItem -LiteralPath $folder -File) {
        if ($file.Extension -notin @(".txt", ".xml")) {
            continue
        }
        try {
            [xml]$xml = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
            foreach ($node in $xml.SelectNodes("//BookDesc")) {
                $id = $node.GetAttribute("BookID")
                if (-not [string]::IsNullOrEmpty($id) -and -not $names.ContainsKey($id)) {
                    $names[$id] = [string]$node.BookName
                }
            }
        }
        catch {
            $failures.Add("failed to parse $($file.FullName): $($_.Exception.Message)")
        }
    }
    return $names
}

$cnBookNames = Read-BookNames "cn"
$rhinoPageName = -join @([char]0x7280, [char]0x725B, [char]0x4E4B, [char]0x9875)
$myoPageName = -join @([char]0x7F2A, [char]0x4E4B, [char]0x9875)
if ($cnBookNames["250024"] -ne $rhinoPageName) {
    $failures.Add("CN localization ID 250024 must reproduce the Rhino collision")
}
if ($cnBookNames["250030"] -ne $myoPageName) {
    $failures.Add("CN localization TextId 250030 must resolve to Myo")
}

# Every player key page whose numeric ID collides with another localized name
# must have a valid TextId name in all shipped languages.
$collisionCount = 0
foreach ($language in @("cn", "en", "kr")) {
    $bookNames = Read-BookNames $language
    foreach ($file in Get-ChildItem -LiteralPath (Join-Path $repo "AddData\EquipPage") -Filter "*.xml" -File) {
        if ($file.Name -match "enemy") {
            continue
        }
        [xml]$xml = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
        foreach ($book in $xml.SelectNodes("//Book")) {
            $id = $book.GetAttribute("ID")
            $textId = [string]$book.TextId
            if ($bookNames.ContainsKey($id) -and $bookNames.ContainsKey($textId) -and $bookNames[$id] -ne $bookNames[$textId]) {
                $collisionCount++
                if ([string]::IsNullOrWhiteSpace($bookNames[$textId])) {
                    $failures.Add("$language book $id has an empty TextId $textId localization")
                }
            }
        }
    }
}
if ($collisionCount -lt 8) {
    $failures.Add("expected at least 8 player key-page ID/TextId collision cases, found $collisionCount")
}

# The display resolver must try the canonical TextId group before the numeric
# book-ID group; otherwise Chinese scoring selects longer but unrelated names
# such as Rhino over Myo.
$nameMethodStart = $rewardingSource.IndexOf("public static string GetLocalizedBookName(BookXmlInfo book)", [StringComparison]::Ordinal)
$nameMethodEnd = $rewardingSource.IndexOf("public static string GetPassiveName", $nameMethodStart, [StringComparison]::Ordinal)
if ($nameMethodStart -lt 0 -or $nameMethodEnd -le $nameMethodStart) {
    $failures.Add("cannot locate GetLocalizedBookName body")
}
else {
    $nameMethod = $rewardingSource.Substring($nameMethodStart, $nameMethodEnd - $nameMethodStart)
    $textIdIndex = $nameMethod.IndexOf("if (book.TextId > 0)", [StringComparison]::Ordinal)
    $bookIdIndex = $nameMethod.IndexOf("foreach (LorId candidate in GetOriginAwareIds(book.id))", [StringComparison]::Ordinal)
    if ($textIdIndex -lt 0 -or $bookIdIndex -lt 0 -or $textIdIndex -gt $bookIdIndex) {
        $failures.Add("GetLocalizedBookName must resolve TextId before numeric book ID")
    }
    elseif ($nameMethod.Substring($textIdIndex, $bookIdIndex - $textIdIndex) -notmatch 'return\s+textIdResult\s*;') {
        $failures.Add("GetLocalizedBookName must return a usable TextId name before considering numeric book-ID names")
    }
}

$knownStart = $rewardingSource.IndexOf("private static bool TryGetKnownBookName", [StringComparison]::Ordinal)
$knownEnd = $rewardingSource.IndexOf("private static bool IsOriginPackage", $knownStart, [StringComparison]::Ordinal)
if ($knownStart -lt 0 -or $knownEnd -le $knownStart) {
    $failures.Add("cannot locate TryGetKnownBookName body")
}
else {
    $knownMethod = $rewardingSource.Substring($knownStart, $knownEnd - $knownStart)
    $textIdIndex = $knownMethod.IndexOf("book.TextId", [StringComparison]::Ordinal)
    $bookIdIndex = $knownMethod.IndexOf("book.id.id", [StringComparison]::Ordinal)
    if ($textIdIndex -lt 0 -or $bookIdIndex -lt 0 -or $textIdIndex -gt $bookIdIndex) {
        $failures.Add("TryGetKnownBookName must prefer TextId overrides before numeric book-ID overrides")
    }
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) {
        Write-Host "FAIL: $failure" -ForegroundColor Red
    }
    Write-Host "STATIC_FAILED=$($failures.Count)"
    exit 1
}

Write-Host "PASS: Heart of Aspiration presentation and key-page TextId identity are aligned." -ForegroundColor Green
Write-Host "KEYPAGE_LOCALIZATION_COLLISIONS_AUDITED=$collisionCount"
