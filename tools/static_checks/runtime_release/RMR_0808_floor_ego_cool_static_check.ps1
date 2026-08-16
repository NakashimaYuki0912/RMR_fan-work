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

$patches = Read-Utf8 'abcdcode_Refactored\LogLikePatches.cs'

Assert-Match $patches 'AddFloorEgoCoolTimeForRmr' 'Floor EGO cool helper must exist'
Assert-Match $patches 'EnsureFloorEgoCoolTimeProgressAfterVanilla' 'Post-vanilla cool ensure must exist'
Assert-Match $patches 'CanUsingEgo\(\)' 'Post-vanilla path must skip when CanUsingEgo already ran'
Assert-Match $patches 'emotionLevel < 3' 'Must keep emotion-level >= 3 gate'
Assert-Match $patches 'Instance\?\.AddEgoCoolTime\(count\)' 'Must call SpecialCardListModel.AddEgoCoolTime'
Assert-Match $patches 'EnsureFloorEgoCoolTimeProgressAfterVanilla\(count\);' 'Realization coin path must ensure after orig'

# Extract CreateRmrEmotionCoins body and require both cool paths.
$m = [regex]::Match($patches, 'private static int CreateRmrEmotionCoins\([\s\S]*?\n        \}')
if (-not $m.Success) { throw 'Could not locate CreateRmrEmotionCoins method body' }
$body = $m.Value
if ($body -notmatch 'personalEgoDetail\.AddEgoCoolTime\(count\)') { throw 'CreateRmrEmotionCoins must refill personalEgo cool' }
if ($body -notmatch 'AddFloorEgoCoolTimeForRmr\(count\)') { throw 'CreateRmrEmotionCoins must drive floor EGO cool' }

Write-Output 'PASS: RMR_0808_floor_ego_cool_static_check'
