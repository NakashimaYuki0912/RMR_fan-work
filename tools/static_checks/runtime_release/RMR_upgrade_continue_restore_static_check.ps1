# Regression guard: saved upgraded combat pages carry a dynamic <LogUpgrade> package id.
# After a fresh process starts, that XML entry does not exist until the upgrade manager rebuilds it.
# The continue loader must rebuild it before AddCard, otherwise AddCard silently drops the page.
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$root = Split-Path -Parent $root
$root = Split-Path -Parent $root
$source = Join-Path $root 'abcdcode_LOGLIKE_MOD\LogueBookModels.cs'
$text = Get-Content -LiteralPath $source -Raw -Encoding UTF8

if ($text -notmatch 'RestoreSavedCombatCardForLoad\s*\(') {
    throw 'Missing upgraded-card restore helper for Continue Run.'
}
if ($text -notmatch 'UpgradeMetadata\.UnpackPid\s*\(\s*savedId\.packageId') {
    throw 'Continue restore helper does not recognize saved <LogUpgrade> metadata.'
}
if ($text -notmatch 'GetUpgradeCard\s*\(\s*savedId\.GetOriginalId\(\)\s*,\s*metadata\.index\s*,\s*metadata\.count\s*\)') {
    throw 'Continue restore helper does not rebuild the exact saved upgrade level.'
}
if ($text -notmatch 'AddCard\s*\(\s*RestoreSavedCombatCardForLoad\s*\(') {
    throw 'Continue inventory loading does not restore dynamic upgraded IDs before AddCard.'
}

'RMR UPGRADED CARD CONTINUE RESTORE STATIC CHECK PASSED'
