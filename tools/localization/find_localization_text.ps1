<#!
.SYNOPSIS
Finds a localization string, XML ID, or UI key in one RMR language folder.

.EXAMPLE
  .\tools\localization\find_localization_text.ps1 -Query 'ui_RMR_Hub_Atlas' -Language en
  .\tools\localization\find_localization_text.ps1 -Query '闪光之镜' -Language cn
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Query,

    [ValidateSet('cn', 'en', 'kr')]
    [string]$Language = 'en'
)

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
while ($root -and -not (Test-Path (Join-Path $root 'RogueLike Mod Reborn.csproj'))) {
    $root = Split-Path -Parent $root
}
if (-not $root) {
    throw "Could not locate repository root from $PSScriptRoot."
}

$languageRoot = Join-Path $root (Join-Path 'Localize' $Language)
if (-not (Test-Path -LiteralPath $languageRoot)) {
    throw "Language folder not found: $languageRoot"
}

$pattern = [regex]::Escape($Query)
$files = Get-ChildItem -LiteralPath $languageRoot -Recurse -File |
    Where-Object { $_.Extension -in '.xml', '.txt' }
$matches = Select-String -LiteralPath $files.FullName -Pattern $pattern -Encoding UTF8

if (-not $matches) {
    Write-Host "No match for '$Query' in Localize/$Language." -ForegroundColor Yellow
    exit 1
}

foreach ($match in $matches) {
    $relative = $match.Path.Substring($languageRoot.Length).TrimStart('\', '/')
    Write-Output ("Localize/{0}/{1}:{2}: {3}" -f $Language, $relative, $match.LineNumber, $match.Line.Trim())
}
