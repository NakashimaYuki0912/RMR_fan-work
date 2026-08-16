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

$models = Read-Utf8 'abcdcode_LOGLIKE_MOD\LogueBookModels.cs'
$patches = Read-Utf8 'abcdcode_Refactored\LogLikePatches.cs'
$rest = Read-Utf8 'abcdcode_LOGLIKE_MOD\PickUpModel_RestGood3.cs'
$shop = Read-Utf8 'abcdcode_LOGLIKE_MOD\PickUpModel_ShopGood29.cs'

Assert-Match $shop '8570029' 'Desiccant shop good must bind passive 8570029'
Assert-Match $shop '800003' 'Desiccant must unlock RestGood3 id 800003'
Assert-Match $rest 'ResistAllUp' 'RestGood3 must define ResistAllUp'
Assert-Match $models 'ReapplyAllPlayerStatAdders' 'Load/battle must reapply LogStatAdders'
Assert-Match $models 'ReapplyAllPlayerStatAdders after load' 'Continue load must reapply after playersstatadders restore'
Assert-Match $patches 'ReapplyAllPlayerStatAdders on create' 'Librarian create must reapply stat adders'

Write-Output 'PASS: RMR_0808_desiccant_statadder_reapply_static_check'
