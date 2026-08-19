$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = $scriptDir
while ($repoRoot -and -not (Test-Path (Join-Path $repoRoot 'RogueLike Mod Reborn.csproj'))) {
    $repoRoot = Split-Path -Parent $repoRoot
}
if (-not $repoRoot) { throw 'Could not locate repository root for static check.' }
Set-Location $repoRoot

function Read-Utf8([string]$Path) {
    return Get-Content -LiteralPath $Path -Raw -Encoding UTF8
}

function Assert-Match([string]$Text, [string]$Pattern, [string]$Message) {
    if ($Text -notmatch $Pattern) { throw $Message }
}

$panel = Read-Utf8 'abcdcode_LOGLIKE_MOD\LogRealizationPanel.cs'
$patches = Read-Utf8 'abcdcode_Refactored\LogLikePatches.cs'
$requiredKeys = @(
    'ui_RMR_RealizationTitle',
    'ui_RMR_RealizationSubtitle',
    'ui_RMR_RealizationDesc',
    'ui_RMR_RealizationProgress',
    'ui_RMR_RealizationReplay',
    'ui_RMR_RealizationChallenge',
    'ui_RMR_RealizationFooter',
    'ui_RMR_RealizationClose'
)

Assert-Match $panel `
    'abcdcode_LOGLIKE_MOD_Extension\.TextDataModel\.GetText\(key\)' `
    'Realization UI must read the RMR dictionary directly instead of making vanilla log every custom key as missing.'

foreach ($lang in @('cn', 'en', 'kr')) {
    [xml]$xml = Read-Utf8 (Join-Path (Join-Path 'Localize' $lang) 'UIs.txt')
    $ids = @($xml.localize.text | ForEach-Object { [string]$_.id })
    foreach ($key in $requiredKeys) {
        if ($ids -notcontains $key) {
            throw "Localize/$lang/UIs.txt is missing realization UI key: $key"
        }
    }
}

Assert-Match $patches `
    'HarmonyPatch\(typeof\(CreatureBattleDialogueLoader\), nameof\(CreatureBattleDialogueLoader\.GetDialogue\)\)' `
    'RMR realization librarians need a dialogue guard around CreatureBattleDialogueLoader.GetDialogue.'
Assert-Match $patches `
    'RMRRealizationManager\.CurrentRealizationFloor\s*!=\s*SephirahType\.Hokma' `
    'The dialogue guard must be limited to the RMR Hokma realization.'
Assert-Match $patches `
    'dlgType\s*==\s*DialogType\.START_BATTLE[\s\S]*?dlgType\s*==\s*DialogType\.DEATH\s*&&\s*isSephirah' `
    'Only WhiteNight start lines and the canonical Hokma death line may use vanilla dynamic keys.'
Assert-Match $patches `
    'Projected librarians have no generic BattleDialogModel[\s\S]*?__result\s*=\s*string\.Empty;[\s\S]*?return false;' `
    'Unsupported and projected-librarian death dialogue must be silenced before null BattleDialogModel or missing keys.'

Write-Output 'PASS: realization UI localization and Hokma dynamic-dialog guards are present.'
