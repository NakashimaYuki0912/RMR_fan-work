$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = $scriptDir
while ($repoRoot -and -not (Test-Path (Join-Path $repoRoot 'RogueLike Mod Reborn.csproj'))) {
    $repoRoot = Split-Path -Parent $repoRoot
}
if (-not $repoRoot) { throw 'Could not locate repository root for static check.' }
Set-Location $repoRoot

function Read-Utf8([string]$Path) { return Get-Content -LiteralPath $Path -Raw -Encoding UTF8 }
function Assert-Match([string]$Text, [string]$Pattern, [string]$Message) {
    if ($Text -notmatch $Pattern) { throw $Message }
}
function Assert-NoMatch([string]$Text, [string]$Pattern, [string]$Message) {
    if ($Text -match $Pattern) { throw $Message }
}

$mgr = Read-Utf8 'RMR_RealizationManager.cs'
$patches = Read-Utf8 'abcdcode_Refactored\LogLikePatches.cs'
$saver = Read-Utf8 'abcdcode_LOGLIKE_MOD\LoguePlayDataSaver.cs'

Assert-Match $mgr 'PassiveAbility_105010' 'Die allowlist must include Angela Malkuth 105010'
Assert-Match $mgr 'PassiveAbility_205010' 'Die allowlist must include Angela Yesod 205010'
Assert-Match $mgr 'IsHokmaApostleUnit|PassiveAbility_905500' 'Must never-block Hokma apostle 905500'
Assert-Match $mgr 'return n == "PassiveAbility_105010" \|\| n == "PassiveAbility_205010"' `
    'IsMultiphasePassiveType must be allowlist-only (105010/205010)'

# Broad heuristic that matched Despair 505211 must be gone from IsMultiphasePassiveType body.
$multiFn = [regex]::Match(
    $mgr,
    '(?s)private static bool IsMultiphasePassiveType\(Type t\)\s*\{(?<body>.*?)(?=\n\s*private static IEnumerable<object> EnumeratePassives)'
).Groups['body'].Value
if ([string]::IsNullOrWhiteSpace($multiFn)) { throw 'Could not extract IsMultiphasePassiveType body' }
Assert-NoMatch $multiFn 'AccessTools\.Field\(t,\s*"_currentPhase"\)' `
    'IsMultiphasePassiveType must not treat any _currentPhase as multiphase'
Assert-NoMatch $multiFn 'PassiveAbility_505010|PassiveAbility_905010' `
    'Dead Angela hardcodes 505010/905010 must not remain in allowlist'

Assert-Match $patches '\[RMR DieProbe\]' 'Die probe log required'
Assert-Match $patches 'IsImpurityScriptedBossEndBattleAllowed|Allowing EndBattle for impurity/scripted boss' `
    'Impurity Ensemble/BlackSilence EndBattle allow required'
Assert-Match $saver '\[RMR SaveLifecycle\]' 'SaveLifecycle probe required'

Write-Output 'PASS: RMR_0808_multiphase_die_allowlist_static_check'
