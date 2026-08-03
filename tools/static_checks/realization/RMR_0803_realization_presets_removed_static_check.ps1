$ErrorActionPreference = 'Stop'

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$errors = [System.Collections.Generic.List[string]]::new()

function Read-Utf8([string]$relativePath) {
    return [System.IO.File]::ReadAllText(
        (Join-Path $repo $relativePath),
        [System.Text.Encoding]::UTF8)
}

function Assert-NotMatch([string]$label, [string]$text, [string]$pattern) {
    if ($text -match $pattern) {
        $errors.Add($label)
        Write-Host "[FAIL] $label" -ForegroundColor Red
    }
    else {
        Write-Host "[OK] $label" -ForegroundColor Green
    }
}

function Assert-Match([string]$label, [string]$text, [string]$pattern) {
    if ($text -notmatch $pattern) {
        $errors.Add($label)
        Write-Host "[FAIL] $label" -ForegroundColor Red
    }
    else {
        Write-Host "[OK] $label" -ForegroundColor Green
    }
}

$manager = Read-Utf8 'RMR_RealizationManager.cs'
$patches = Read-Utf8 'abcdcode_Refactored\LogLikePatches.cs'
$launchHost = Read-Utf8 'RMR_RealizationLaunchHost.cs'
$project = Read-Utf8 'RogueLike Mod Reborn.csproj'
$cn = Read-Utf8 'Localize\cn\UIs.txt'
$en = Read-Utf8 'Localize\en\UIs.txt'
$kr = Read-Utf8 'Localize\kr\UIs.txt'
$oldAdr = Read-Utf8 'docs\decisions\ADR-002-realization-named-loadout-presets.md'
$newAdr = Read-Utf8 'docs\decisions\ADR-003-remove-realization-loadout-presets.md'

Assert-NotMatch 'preset save and public APIs are absent from the realization manager' $manager `
    'RealizationPreset|SavedRealization|RMR_RealizationPresets'
Assert-NotMatch 'prepare screen no longer mounts a preset panel' $patches `
    'RMRRealizationPresetPanel'
Assert-NotMatch 'shared overlay no longer retains a preset panel' $launchHost `
    'RMRRealizationPresetPanel'
Assert-NotMatch 'Release project no longer compiles the preset panel' $project `
    'RMR_RealizationPresetPanel\.cs'

$panelPath = Join-Path $repo 'RMR_RealizationPresetPanel.cs'
if (Test-Path -LiteralPath $panelPath) {
    $errors.Add('preset panel source file is removed')
    Write-Host '[FAIL] preset panel source file is removed' -ForegroundColor Red
}
else {
    Write-Host '[OK] preset panel source file is removed' -ForegroundColor Green
}

Assert-NotMatch 'Chinese preset localization is removed' $cn 'ui_RMR_Preset_'
Assert-NotMatch 'English preset localization is removed' $en 'ui_RMR_Preset_'
Assert-NotMatch 'Korean preset localization is removed' $kr 'ui_RMR_Preset_'
Assert-Match 'ADR-002 records that the old decision was superseded' $oldAdr `
    'Superseded by \[ADR-003\]'
Assert-Match 'ADR-003 records the explicit full-feature withdrawal' $newAdr `
    'Remove realization loadout presets from the product'

if ($errors.Count -gt 0) {
    throw "RMR 0803 realization preset removal check failed: $($errors -join '; ')"
}

Write-Host 'RMR 0803 realization preset removal check passed.' -ForegroundColor Green
