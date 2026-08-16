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

$manager = Read-Utf8 'RMR_RealizationManager.cs'

Assert-Match $manager 'public bool IsSephirah;' `
    'Route unit snapshot must preserve the original isSephirah value.'
Assert-Match $manager 'public SephirahType OwnerSephirah;' `
    'Route unit snapshot must preserve the original owner floor.'
Assert-Match $manager 'IsSephirah\s*=\s*unit\.isSephirah' `
    'ApplyCompendiumOnlyLoadout must snapshot isSephirah before temporary mutation.'
Assert-Match $manager 'OwnerSephirah\s*=\s*unit\.OwnerSephirah' `
    'ApplyCompendiumOnlyLoadout must snapshot OwnerSephirah before temporary mutation.'

$assign = [regex]::Match(
    $manager,
    '(?s)private static void AssignRealizationLibrarianIdentities\(.*?\n\s*\}(?=\n\s*private static)'
).Value
if ([string]::IsNullOrWhiteSpace($assign)) {
    throw 'AssignRealizationLibrarianIdentities helper is missing.'
}
Assert-Match $assign 'unit\.isSephirah\s*=\s*i\s*==\s*0' `
    'Exactly realization roster index 0 must be marked as the Sephirah librarian.'
Assert-Match $assign '_ownerSephirah' `
    'Realization librarian identity must bind the private owner floor field.'
Assert-Match $assign 'CurrentRealizationFloor' `
    'Realization librarian identity must bind to the selected realization floor.'

$apply = [regex]::Match(
    $manager,
    '(?s)private static bool ApplyCompendiumOnlyLoadout\(\).*?(?=\n\s*private static void RestoreRouteLoadout)'
).Value
if ([string]::IsNullOrWhiteSpace($apply)) {
    throw 'Could not extract ApplyCompendiumOnlyLoadout.'
}
Assert-Match $apply 'AssignRealizationLibrarianIdentities\(teamList\)' `
    'Temporary identities must be assigned before the realization stage is launched.'
if ($apply.IndexOf('CompendiumOnlyLoadoutActive = true', [StringComparison]::Ordinal) -gt
    $apply.IndexOf('AssignRealizationLibrarianIdentities(teamList)', [StringComparison]::Ordinal)) {
    throw 'The loadout must be marked active before identity assignment so failures can roll back.'
}
Assert-Match $apply 'Failed to assign realization librarian identities:[\s\S]*?RestoreRouteLoadout\(\)' `
    'Identity assignment failure must restore the route snapshot.'

$restore = [regex]::Match(
    $manager,
    '(?s)private static void RestoreRouteLoadout\(\).*?(?=\n\s*#endregion)'
).Value
if ([string]::IsNullOrWhiteSpace($restore)) {
    throw 'Could not extract RestoreRouteLoadout.'
}
Assert-Match $restore 'unit\.isSephirah\s*=\s*ds\.IsSephirah' `
    'Route restore must restore the original isSephirah value.'
Assert-Match $restore 'RestoreUnitOwnerSephirah\(unit,\s*ds\.OwnerSephirah\)' `
    'Route restore must restore the original owner floor.'

Assert-Match $manager '\[RMRRealizationManager\] Realization librarian identity:' `
    'A runtime identity probe is required for restarted-game verification.'

Write-Output 'PASS: RMR_0814_realization_sephirah_identity_static_check'
