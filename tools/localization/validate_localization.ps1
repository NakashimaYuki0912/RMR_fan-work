<#!
.SYNOPSIS
Checks translator-owned localization files against the Chinese reference tree.

.DESCRIPTION
Validates that English/Korean retain the reference files, that every localization
file is parseable XML, and that UIs.txt key IDs remain aligned. Extra target-language
files are reported as warnings because Korean currently has legacy additions.
#>
[CmdletBinding()]
param(
    [ValidateSet('cn', 'en', 'kr')]
    [string]$ReferenceLanguage = 'cn',

    [ValidateSet('cn', 'en', 'kr')]
    [string[]]$TargetLanguages = @('en', 'kr')
)

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
while ($root -and -not (Test-Path (Join-Path $root 'RogueLike Mod Reborn.csproj'))) {
    $root = Split-Path -Parent $root
}
if (-not $root) {
    throw "Could not locate repository root from $PSScriptRoot."
}

function GetRelativeFiles([string]$language) {
    $languageRoot = Join-Path $root (Join-Path 'Localize' $language)
    if (-not (Test-Path -LiteralPath $languageRoot)) {
        throw "Language folder not found: $languageRoot"
    }
    return @(Get-ChildItem -LiteralPath $languageRoot -Recurse -File |
        Where-Object { $_.Extension -in '.xml', '.txt' } |
        ForEach-Object { $_.FullName.Substring($languageRoot.Length).TrimStart('\', '/').Replace('\', '/') } |
        Sort-Object)
}

function GetUiKeys([string]$language) {
    $path = Join-Path $root (Join-Path (Join-Path 'Localize' $language) 'UIs.txt')
    [xml]$xml = Get-Content -LiteralPath $path -Raw -Encoding UTF8
    return @($xml.SelectNodes('/localize/text') | ForEach-Object { $_.GetAttribute('id') } | Sort-Object -Unique)
}

function AssertXmlFiles([string]$language) {
    $languageRoot = Join-Path $root (Join-Path 'Localize' $language)
    $invalid = New-Object System.Collections.Generic.List[string]
    Get-ChildItem -LiteralPath $languageRoot -Recurse -File |
        Where-Object { $_.Extension -in '.xml', '.txt' } |
        ForEach-Object {
            try { [xml](Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8) | Out-Null }
            catch { $invalid.Add($_.FullName.Substring($languageRoot.Length).TrimStart('\', '/')) }
        }
    if ($invalid.Count -gt 0) {
        throw "Localize/$language contains invalid XML: $($invalid -join ', ')"
    }
}

$referenceFiles = GetRelativeFiles $ReferenceLanguage
$referenceKeys = GetUiKeys $ReferenceLanguage
AssertXmlFiles $ReferenceLanguage

foreach ($language in $TargetLanguages | Select-Object -Unique) {
    if ($language -eq $ReferenceLanguage) { continue }
    $targetFiles = GetRelativeFiles $language
    AssertXmlFiles $language

    $missingFiles = @($referenceFiles | Where-Object { $_ -notin $targetFiles })
    if ($missingFiles.Count -gt 0) {
        throw "Localize/$language is missing reference files: $($missingFiles -join ', ')"
    }
    $extraFiles = @($targetFiles | Where-Object { $_ -notin $referenceFiles })
    if ($extraFiles.Count -gt 0) {
        Write-Host "WARNING Localize/$language has legacy/additional files: $($extraFiles -join ', ')" -ForegroundColor Yellow
    }

    $targetKeys = GetUiKeys $language
    $missingKeys = @($referenceKeys | Where-Object { $_ -notin $targetKeys })
    $extraKeys = @($targetKeys | Where-Object { $_ -notin $referenceKeys })
    if ($missingKeys.Count -gt 0 -or $extraKeys.Count -gt 0) {
        throw "UIs.txt key mismatch for $language. Missing=$($missingKeys -join ', '); Extra=$($extraKeys -join ', ')"
    }
    Write-Host "Localize/${language}: $($targetFiles.Count) files, $($targetKeys.Count) UI keys - OK"
}

Write-Host 'Localization validation passed.' -ForegroundColor Green
