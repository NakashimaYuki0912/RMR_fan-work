param(
    [string]$ReferencePath = "E:\Library_of_Ruina_Abnormality_Pages_EN.txt"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$sourcePath = Join-Path $root "RMR_AbnormalityUnlocks.cs"
$source = Get-Content -Raw -Encoding UTF8 -LiteralPath $sourcePath

if (-not (Test-Path -LiteralPath $ReferencePath)) {
    throw "Authoritative abnormality-page reference not found: $ReferencePath"
}
$reference = Get-Content -Raw -Encoding UTF8 -LiteralPath $ReferencePath

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw $Message
    }
}

# User-supplied vanilla reference:
# team emotion 1-2 => Rhythm, 3-4 => Musical Addiction, 5 => Music.
$yesodStart = $reference.IndexOf("FLOOR OF TECHNOLOGICAL SCIENCES")
$yesodEnd = $reference.IndexOf("FLOOR OF LITERATURE", $yesodStart + 1)
Assert-True ($yesodStart -ge 0 -and $yesodEnd -gt $yesodStart) `
    "Could not isolate the Yesod section in the supplied reference."
$yesod = $reference.Substring($yesodStart, $yesodEnd - $yesodStart)
$tierOne = $yesod.IndexOf("TIER I ")
$tierTwo = $yesod.IndexOf("TIER II ", $tierOne + 1)
$tierThree = $yesod.IndexOf("TIER III ", $tierTwo + 1)
$rhythm = $yesod.IndexOf("Rhythm", $tierOne)
$addiction = $yesod.IndexOf("Musical Addiction", $tierTwo)
$music = $yesod.IndexOf("Music", $tierThree)
Assert-True ($tierOne -ge 0 -and $rhythm -gt $tierOne -and $rhythm -lt $tierTwo) `
    "Reference mismatch: Rhythm must be a Yesod Tier I page."
Assert-True ($tierTwo -gt $tierOne -and $addiction -gt $tierTwo -and $addiction -lt $tierThree) `
    "Reference mismatch: Musical Addiction must be a Yesod Tier II page."
Assert-True ($tierThree -gt $tierTwo -and $music -gt $tierThree) `
    "Reference mismatch: Music must be a Yesod Tier III page."

Assert-True ($source -match 'Put\("SingingMachine1",\s*3\)') `
    "Music/SingingMachine1 must be registered as tier III."
Assert-True ($source -match 'Put\("SingingMachine2",\s*1\)') `
    "Rhythm/SingingMachine2 must be registered as tier I."
Assert-True ($source -match 'Put\("SingingMachine3",\s*2\)') `
    "Musical Addiction/SingingMachine3 must be registered as tier II."

$candidateStart = $source.IndexOf("public static IEnumerable<string> GetVanillaScriptCandidates")
$candidateEnd = $source.IndexOf("private static bool VanillaScriptMatchesMod", $candidateStart)
Assert-True ($candidateStart -ge 0 -and $candidateEnd -gt $candidateStart) `
    "Could not isolate GetVanillaScriptCandidates."
$candidateBody = $source.Substring($candidateStart, $candidateEnd - $candidateStart)

# A numbered reward script is a distinct page. Falling back from SingingMachine2/3
# to the unnumbered root resolves both to the first vanilla card and pairs the
# wrong name/artwork with the correct reward.
Assert-True (-not ($candidateBody -match 'if\s*\(!string\.IsNullOrEmpty\(root\)\)\s*\{\s*Add\(root\.ToLowerInvariant\(\)\)')) `
    "Numbered abnormality scripts still fall back to the unnumbered root, which cross-pairs Yesod card names/artwork."

# Full vanilla tier audit. The supplied English list has display names, while the
# checked-in map records the corresponding 150 runtime Script -> EmotionLevel keys.
$mapPath = Join-Path $root "tools\_vanilla_emotion_level_map.txt"
$expected = @{}
Get-Content -Encoding UTF8 -LiteralPath $mapPath | ForEach-Object {
    if ($_ -match '^([^\s#]+)\s+([123])$') {
        $expected[$matches[1].ToLowerInvariant()] = [int]$matches[2]
    }
}
$seedStart = $source.IndexOf("private static void SeedStaticVanillaEmotionTiers")
$seedEnd = $source.IndexOf("private static int ReadEmotionLevelField", $seedStart)
Assert-True ($seedStart -ge 0 -and $seedEnd -gt $seedStart) `
    "Could not isolate static vanilla EmotionLevel seeds."
$seedBody = $source.Substring($seedStart, $seedEnd - $seedStart)
$actual = @{}
[regex]::Matches($seedBody, 'Put\("([^"]+)",\s*([123])\)') | ForEach-Object {
    $actual[$_.Groups[1].Value.ToLowerInvariant()] = [int]$_.Groups[2].Value
}
$conflicts = @($expected.Keys | Where-Object {
    $actual.ContainsKey($_) -and $actual[$_] -ne $expected[$_]
})
$missing = @($expected.Keys | Where-Object { -not $actual.ContainsKey($_) })
Assert-True ($expected.Count -eq 150) `
    "Expected the checked-in vanilla reference to contain 150 script tiers; found $($expected.Count)."
Assert-True ($conflicts.Count -eq 0) `
    "Static EmotionLevel seeds conflict with vanilla: $($conflicts -join ', ')."
Assert-True ($missing.Count -eq 0) `
    "Static EmotionLevel seeds omit vanilla scripts: $($missing -join ', ')."

Write-Output "PASS: Yesod presentation matches the supplied list; all 150 vanilla EmotionLevel scripts have 0 conflicts and 0 omissions."
