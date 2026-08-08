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

$rewarding = Read-Utf8 'abcdcode_LOGLIKE_MOD\RewardingModel.cs'
$patches = Read-Utf8 'abcdcode_Refactored\LogLikePatches.cs'

Assert-Match $rewarding `
    'BeginMidBattleChoiceInputGuard[\s\S]*?Time\.unscaledTime[\s\S]*?Input\.GetMouseButton\(\s*0\s*\)' `
    'Mid-battle E.G.O. completion must wait for both a short unscaled-time guard and primary-pointer release.'
Assert-Match $rewarding `
    'NoteMidBattleEgoPicked[\s\S]*?wasMidBattle[\s\S]*?BeginMidBattleChoiceInputGuard\(\)' `
    'Every completed or skipped mid-battle E.G.O. choice must arm the next-choice input guard.'
Assert-Match $rewarding `
    'EmotionChoice\(\)[\s\S]*?IsMidBattleChoiceInputGuardActive\(\)[\s\S]*?GetCurEmotion\(\)' `
    'The input guard must run before the next abnormality-page offer is generated.'
Assert-Match $rewarding `
    'ResetMidBattleEgoSelectionState\(\)[\s\S]*?ResetMidBattleChoiceInputGuard\(\)' `
    'Battle reset must clear the mid-battle choice input guard.'
Assert-Match $patches `
    'HideRewardSelectionImmediately[\s\S]*?cardSelectionGroup\.interactable\s*=\s*false[\s\S]*?egoSlotList[\s\S]*?SetActive\(false\)' `
    'Closing a mid-battle E.G.O. offer must disable interaction and deactivate its old card slots.'
Assert-Match $patches `
    'OnEmotionPagePicked\(card\)[\s\S]*?ArmMidBattleEgoAfterEmotionIfNeeded\(\)' `
    'Mid-battle E.G.O. must arm only after the abnormality page is committed.'
if ($rewarding -match 'public static void PickEmotion\([\s\S]*?ArmMidBattleEgoAfterEmotionIfNeeded\(\)[\s\S]*?ui_levelup\.Init') {
    throw 'PickEmotion must not arm mid-battle E.G.O. at offer-open time (races EmotionChoice and skips abno 4/5).'
}

Write-Output 'PASS: mid-battle E.G.O. completion cannot leak the same input into the next abnormality-page choice.'
