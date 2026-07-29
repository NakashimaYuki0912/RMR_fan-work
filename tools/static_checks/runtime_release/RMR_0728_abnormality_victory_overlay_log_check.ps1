param(
    [string]$PlayerLog = "$env:USERPROFILE\AppData\LocalLow\Project Moon\LibraryOfRuina\Player.log"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $PlayerLog)) {
    throw "Player.log not found: $PlayerLog"
}

$lines = Get-Content -LiteralPath $PlayerLog
$startBattle = -1
$pickupAfterStart = -1
$victoryAfterStart = -1

for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match "\[RMR\] StartBattle keep special invitation maps") {
        $startBattle = $i
        $pickupAfterStart = -1
        $victoryAfterStart = -1
        continue
    }

    if ($startBattle -lt 0) {
        continue
    }

    if ($lines[$i] -match "\[RMR RewardClearStage\].*type=Creature") {
        $victoryAfterStart = $i
    }

    if ($pickupAfterStart -lt 0 -and $lines[$i] -match "PickUpCreature_.+_(Name|Desc|FlaverText)") {
        $pickupAfterStart = $i
    }
}

if ($startBattle -lt 0) {
    throw "No RMR special-invitation StartBattle marker was found."
}

if ($pickupAfterStart -lt 0) {
    throw "No abnormality reward UI marker was found after the last StartBattle marker."
}

if ($victoryAfterStart -lt 0 -or $victoryAfterStart -gt $pickupAfterStart) {
    Write-Error (
        "BUG: abnormality reward UI opened before this battle produced a Creature victory. " +
        "StartBattle line={0}, first PickUpCreature line={1}, Creature victory line={2}." -f
        ($startBattle + 1),
        ($pickupAfterStart + 1),
        $(if ($victoryAfterStart -lt 0) { "none" } else { $victoryAfterStart + 1 })
    )
}

Write-Output (
    "PASS: abnormality reward UI followed the current battle's Creature victory " +
    "(StartBattle line={0}, victory line={1}, reward line={2})." -f
    ($startBattle + 1),
    ($victoryAfterStart + 1),
    ($pickupAfterStart + 1)
)
