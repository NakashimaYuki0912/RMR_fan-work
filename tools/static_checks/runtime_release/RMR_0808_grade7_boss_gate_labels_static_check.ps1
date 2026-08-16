$ErrorActionPreference = "Stop"

# Wave 3 (#9 #10 #11): Impuritas dual-boss pool must stay gated behind a few Normal
# steps and each boss must render a distinct, localized label instead of the
# generic "Stage_Boss" text. Neither 70020 (Black Silence) nor 70021 (Distorted
# Ensemble) may be removed from the chapter pool.

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
    [System.IO.File]::ReadAllText((Join-Path $root $relativePath), [System.Text.Encoding]::UTF8)
}

function Read-Xml($relativePath) {
    [xml](Read-Text $relativePath)
}

function Require-Contains($content, $pattern, $label) {
    if ($content -notmatch [regex]::Escape($pattern)) {
        throw "Missing ${label}: $pattern"
    }
}

function Require-True($condition, $message) {
    if (-not $condition) {
        throw $message
    }
}

$bookModels = Read-Text "abcdcode_LOGLIKE_MOD\LogueBookModels.cs"

# 1) GetNextList must gate ALL Boss-type Grade7 stages behind curChStageStep, not just one.
Require-Contains $bookModels "grade >= ChapterGrade.Grade7 && LogLikeMod.curChStageStep < 3" "Impuritas boss step-gate condition"
Require-Contains $bookModels "logueStageInfoList.RemoveAll(x => x != null && x.type == StageType.Boss)" "Impuritas boss step-gate must remove ALL boss candidates, not just the first"

# 2) Pre-Impuritas single-boss chapters keep their existing (single-boss) filter untouched.
Require-Contains $bookModels "grade < ChapterGrade.Grade7" "pre-Impuritas boss-hide guard must stay scoped to Grade1-6"

# 3) CreateStageDesc must render distinct names/descriptions for 70020 vs 70021 instead of
#    always falling back to the generic "Stage_Boss" text.
Require-Contains $bookModels "sid == 70020" "Black Silence stage-id branch in CreateStageDesc"
Require-Contains $bookModels "sid == 70021" "Distorted Ensemble stage-id branch in CreateStageDesc"
Require-Contains $bookModels "Stage_Boss_BlackSilence" "Black Silence localization key lookup"
Require-Contains $bookModels "Stage_Boss_DistortedEnsemble" "Distorted Ensemble localization key lookup"

# 4) Localization keys must exist (not just be referenced) in all three shipped languages.
foreach ($locale in @("cn", "en", "kr")) {
    $ui = Read-Text "Localize\$locale\UIs.txt"
    Require-Contains $ui 'id="Stage_Boss_BlackSilence"' "$locale Black Silence boss label"
    Require-Contains $ui 'id="Stage_Boss_DistortedEnsemble"' "$locale Distorted Ensemble boss label"
    Require-Contains $ui 'id="Stage_Boss_BlackSilence_Desc"' "$locale Black Silence boss description"
    Require-Contains $ui 'id="Stage_Boss_DistortedEnsemble_Desc"' "$locale Distorted Ensemble boss description"
    [xml]$ui | Out-Null
}

# 5) Neither boss may ever be dropped from the Grade7 chapter pool.
$stageCh7 = Read-Xml "SpecialStaticInfo\StagesXmlInfos\Stage_ch7.xml"
$routeStages = @($stageCh7.StagesXmlRoot.ChapterList.StageList)
foreach ($bossId in @("70020", "70021")) {
    Require-True (
        $routeStages | Where-Object { $_.GetAttribute("ID") -eq $bossId -and $_.GetAttribute("StageType") -eq "Boss" }
    ) "Grade7 route must keep boss candidate $bossId (must not remove either boss)."
}

# 6) The Head / Olivier / Hana gap must stay documented rather than silently missing.
$knownBugs = Read-Text "docs\agent-handbook\04-known-bugs-and-fixes.md"
Require-Contains $knownBugs "The Head" "Head fight documented as an intended gap"
Require-Contains $knownBugs "Olivier / Hana" "Olivier/Hana gap documentation"

"RMR 0808 GRADE7 BOSS GATE AND LABELS STATIC CHECK PASSED"
