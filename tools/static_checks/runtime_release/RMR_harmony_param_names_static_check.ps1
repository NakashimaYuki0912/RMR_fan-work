# Verify every Harmony patch's injected parameter names against the real vanilla signature.
#
# Harmony binds injected arguments BY NAME. One wrong name does not fail that single patch -- it
# makes CreateAndPatchAll abort the entire patch class with "IL Compile Error (unknown location)",
# so the mod dies at startup with no indication of which patch is at fault.
# (Hit 2026-07-27: UICharacterSlot.SetCharacter's parameter is "unitData", the patch said "unit".)
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File .\tools\static_checks\runtime_release\RMR_harmony_param_names_static_check.ps1

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = $scriptDir
while ($root -and -not (Test-Path (Join-Path $root 'RogueLike Mod Reborn.csproj'))) {
    $root = Split-Path -Parent $root
}
if (-not $root) { throw 'Could not locate repository root.' }

$cecil = Join-Path $root "dependencies\Mono.Cecil.dll"
$target = Join-Path $root "dependencies\Assembly-CSharp.dll"
if (-not (Test-Path $cecil) -or -not (Test-Path $target)) {
    Write-Host "Mono.Cecil or Assembly-CSharp missing - skipping (nothing to verify against)." -ForegroundColor Yellow
    exit 0
}
Add-Type -Path $cecil
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($target)

# vanilla type.method -> set of parameter names
$vanilla = @{}
foreach ($t in $asm.MainModule.Types) {
    foreach ($m in $t.Methods) {
        $key = "$($t.Name).$($m.Name)"
        if (-not $vanilla.ContainsKey($key)) { $vanilla[$key] = New-Object System.Collections.ArrayList }
        foreach ($p in $m.Parameters) { [void]$vanilla[$key].Add($p.Name) }
    }
}

# Harmony's own injected names are never real parameters.
$special = @('__instance','__result','__originalMethod','__args','__state','__runOriginal','__exception')

$failures = New-Object System.Collections.ArrayList
$checked = 0

foreach ($file in Get-ChildItem $root -Recurse -Filter *.cs |
        Where-Object { $_.FullName -notmatch '\\(_release_packages|bin|obj)\\' }) {
    $lines = Get-Content -LiteralPath $file.FullName -Encoding UTF8
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -notmatch 'HarmonyPatch\(\s*typeof\(\s*(?:[\w\.]+\.)?(\w+)\s*\)\s*,\s*(?:nameof\(\s*(?:[\w\.]+\.)?\w*\.?(\w+)\s*\)|"(\w+)")') { continue }
        $typeName = $Matches[1]
        $methodName = if ($Matches[2]) { $Matches[2] } else { $Matches[3] }

        # Find the patch method signature on the following lines.
        $sig = ""
        for ($j = $i + 1; $j -lt [Math]::Min($i + 8, $lines.Count); $j++) {
            $sig += " " + $lines[$j]
            if ($sig -match '\)\s*$' -or $lines[$j] -match '\)\s*$') { break }
        }
        if ($sig -notmatch '\(([^)]*)\)') { continue }
        $paramBlob = $Matches[1]
        if (-not $paramBlob.Trim()) { continue }

        $key = "$typeName.$methodName"
        if (-not $vanilla.ContainsKey($key)) { continue }   # not a vanilla type (RMR's own class)
        $known = $vanilla[$key]
        $checked++

        foreach ($p in ($paramBlob -split ',')) {
            $p = $p.Trim()
            if (-not $p) { continue }
            $name = ($p -split '\s+')[-1].TrimStart('@')
            if ($special -contains $name) { continue }
            if ($name -like '___*') { continue }            # private field injection
            if ($known -notcontains $name) {
                [void]$failures.Add("$($file.Name): patch on $key injects '$name' but the real parameters are: $(($known | Select-Object -Unique) -join ', ')")
            }
        }
    }
}

Write-Host "Checked $checked Harmony patch signature(s) against Assembly-CSharp."
if ($failures.Count -gt 0) {
    throw ("Harmony parameter-name mismatches (these abort the whole patch class at startup):`n  " + ($failures -join "`n  "))
}
"RMR HARMONY PARAM NAME STATIC CHECK PASSED"
