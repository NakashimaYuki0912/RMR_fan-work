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

$core = Read-Utf8 'RMR_Core.cs'
$emotion = Read-Utf8 'abcdcode_LOGLIKE_MOD\PickUpModel_RMRVanillaEmotion.cs'

Assert-Match $core `
    'GetCustomAttribute\(typeof\(HideFromItemCatalog\), false\) == null[\s\S]*?Activator\.CreateInstance\(pickups\[i\]\.AsType\(\)\)' `
    'The item catalog must check HideFromItemCatalog before reflection construction.'
Assert-Match $emotion `
    '\[HideFromItemCatalog\][\s\S]*?class PickUpModel_RMRVanillaEmotion' `
    'The parameterized vanilla-emotion adapter must be excluded from parameterless item-catalog construction.'

Write-Output 'PASS: parameterized vanilla-emotion adapter is excluded from item-catalog reflection construction.'
