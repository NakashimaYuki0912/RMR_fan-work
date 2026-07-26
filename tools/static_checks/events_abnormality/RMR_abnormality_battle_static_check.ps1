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
    Get-Content -LiteralPath (Join-Path $root $relativePath) -Raw -Encoding UTF8
}

function Require-Contains($content, $pattern, $label) {
    if ($content -notmatch [regex]::Escape($pattern)) {
        throw "Missing ${label}: $pattern"
    }
}

function Require-NotContains($content, $pattern, $label) {
    if ($content -match [regex]::Escape($pattern)) {
        throw "Forbidden ${label}: $pattern"
    }
}

$router = Read-Text "RMR_AbnormalityBattleRouter.cs"
foreach ($pattern in @(
    "class RMRAbnormalityBattleRouter",
    "GetCandidateStageIds",
    "PickStageForChapter",
    # The router was reshaped from Low/Mid/HighTierStageIds into one pool filtered by librarian
    # count; these assertions were left behind and made the whole check throw before reaching
    # anything else. Assert the members that actually exist now.
    "RegularAbnormalityStageIds",
    "GetMaxLibrariansForChapter",
    "GetRequiredLibrarianCount",
    "201001",
    "202001",
    "203001",
    "204001",
    "205001",
    "206001",
    "207001",
    "210001",
    "208001",
    "209001"
)) {
    Require-Contains $router $pattern "abnormality battle router"
}

foreach ($finalStoryId in @(
    "201005",
    "202005",
    "203005",
    "204005",
    "205005",
    "206005",
    "207005",
    "208004",
    "209004",
    "210005",
    "210006",
    "210007",
    "210008",
    "210009"
)) {
    Require-NotContains $router $finalStoryId "final story abnormality battle id"
}

$patches = Read-Text "abcdcode_Refactored\LogLikePatches.cs"
Require-Contains $patches "RMRAbnormalityBattleRouter.PickStageForChapter" "creature stage routing hook"
Require-Contains $patches "stage.type == StageType.Creature" "creature stage branch"

$core = Read-Text "RogueLike Mod Reborn.csproj"
Require-Contains $core "RMR_AbnormalityBattleRouter.cs" "router compile include"

$earlyStages = @(
    "SpecialStaticInfo\StagesXmlInfos\Stage_ch1.xml",
    "SpecialStaticInfo\StagesXmlInfos\Stage_ch2.xml"
)
foreach ($stageFile in $earlyStages) {
    $content = Read-Text $stageFile
    Require-NotContains $content 'StageType="Creature"' "forbidden early-chapter creature card in $stageFile"
    Require-Contains $content 'StageType="Rest"' "early-chapter rest replacement in $stageFile"
}

$stages = @(
    "SpecialStaticInfo\StagesXmlInfos\Stage_ch3.xml",
    "SpecialStaticInfo\StagesXmlInfos\Stage_ch4.xml",
    "SpecialStaticInfo\StagesXmlInfos\Stage_ch5.xml",
    "SpecialStaticInfo\StagesXmlInfos\Stage_ch6.xml"
)
foreach ($stageFile in $stages) {
    $content = Read-Text $stageFile
    Require-Contains $content 'StageType="Creature"' "route creature card in $stageFile"
}

# Cross-validate RMR placeholder stage ids (99xxxx) referenced by the node maps against their
# definitions in AddData/StageInfo. e998173 deleted 991001-991003 / 991101-991107 while the node
# maps kept referencing them; the game then logged "INVALID STAGE REMOVED FROM STAGE LIST ON
# INITIALIZE" and silently dropped every creature and abnormality-mystery node, for every run.
# Only the 99xxxx range is checked — other ids in the node maps are vanilla stages with no
# definition in AddData, so a blanket check would be all false positives.
$definedStageIds = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($defFile in (Get-ChildItem -LiteralPath (Join-Path $root 'AddData\StageInfo') -Filter '*.xml' -Recurse)) {
    $defText = Get-Content -LiteralPath $defFile.FullName -Raw -Encoding UTF8
    foreach ($m in [regex]::Matches($defText, '<Stage\s+id="(\d+)"')) {
        [void]$definedStageIds.Add($m.Groups[1].Value)
    }
}

$missingRefs = @()
foreach ($mapFile in (Get-ChildItem -LiteralPath (Join-Path $root 'SpecialStaticInfo\StagesXmlInfos') -Filter 'Stage_ch*.xml')) {
    $mapText = Get-Content -LiteralPath $mapFile.FullName -Raw -Encoding UTF8
    foreach ($m in [regex]::Matches($mapText, 'ID="(99\d{4})"')) {
        $refId = $m.Groups[1].Value
        if (-not $definedStageIds.Contains($refId)) {
            $missingRefs += "$($mapFile.Name) references undefined stage $refId"
        }
    }
}
if ($missingRefs.Count -gt 0) {
    throw ("Node maps reference RMR stage ids with no <Stage id=...> definition in AddData/StageInfo:`n  " + ($missingRefs -join "`n  "))
}

"RMR ABNORMALITY BATTLE STATIC CHECK PASSED"

