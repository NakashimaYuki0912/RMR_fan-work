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

$stagePath = 'AddData\StageInfo\StageInfo_ch1.xml'
[xml]$stageXml = Read-Utf8 $stagePath
$rmrStageIds = @('-853', '-854', '-855', '-2854', '-3854', '-4854', '-5854', '-6854', '-7854')
foreach ($stageId in $rmrStageIds) {
    $stage = $stageXml.SelectSingleNode("/StageXmlRoot/Stage[@id='$stageId']")
    if ($null -eq $stage) {
        throw "RMR stage $stageId is missing from $stagePath."
    }
    if ([int]$stage.FloorNum -ne 1) {
        throw "RMR stage $stageId must have FloorNum=1; actual=$($stage.FloorNum)."
    }
}

$patches = Read-Utf8 'abcdcode_Refactored\LogLikePatches.cs'
$purple = Read-Utf8 'abcdcode_LOGLIKE_MOD\PassiveAbility_250227Log.cs'

# Music follows the initially selected floor, but reception availability remains
# vanilla single-floor state. Never reopen all floor buttons or stamp a fake "1".
Assert-NoMatch $patches 'btn\.SetButtonState\(UISephirahButton\.ButtonState\.Open\)' `
    'Battle prepare must not force all sephirah buttons open.'
Assert-NoMatch $patches 'ui_battlesetting_possiblefloor"\) \+ " 1"' `
    'Available-floor text must use the vanilla live 1/0 count.'

# Every new combat must clear the previous node's completion latch. The captured
# failure log repeated "no living players" because EndBattle was still true.
Assert-Match $patches 'Reset stale EndBattle flag at battle start[\s\S]*?LogLikeMod\.EndBattle = false;' `
    'StartBattle must reset the stale EndBattle completion latch.'

# RMR has one usable floor. Purple Tear must enter phase two in place instead of
# consuming another reception floor or waiting forever for a destination.
Assert-Match $purple 'LogLikeMod\.CheckStage\(true\)[\s\S]*?EnterSecondPhaseInPlace\(\);[\s\S]*?return;' `
    'Purple Tear must use an in-place second phase in single-floor RMR combat.'
Assert-Match $purple 'EnterSecondPhaseInPlace[\s\S]*?param2 = 1;[\s\S]*?_teleported = 1;[\s\S]*?_teleportReady = false;' `
    'Purple Tear in-place transition must commit all phase-two state.'

Write-Output 'PASS: RMR reception uses one floor, defeat can terminate, and Purple Tear phase two is single-floor compatible.'
