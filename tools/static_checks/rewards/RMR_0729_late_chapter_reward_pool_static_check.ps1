$ErrorActionPreference = 'Stop'

$script:StaticCheckScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$script:RepoRoot = $script:StaticCheckScriptDir
while ($script:RepoRoot -and -not (Test-Path (Join-Path $script:RepoRoot 'RogueLike Mod Reborn.csproj'))) {
    $script:RepoRoot = Split-Path -Parent $script:RepoRoot
}
if (-not $script:RepoRoot) {
    throw 'Could not locate repository root for static check.'
}
Set-Location $script:RepoRoot

$failures = New-Object System.Collections.Generic.List[string]

function Assert-ContainsIds {
    param(
        [string]$Label,
        [string[]]$Actual,
        [string[]]$Expected
    )
    foreach ($id in $Expected) {
        if ($Actual -notcontains $id) {
            $failures.Add("$Label missing $id")
        }
    }
}

function Assert-ExcludesIds {
    param(
        [string]$Label,
        [string[]]$Actual,
        [string[]]$Excluded
    )
    foreach ($id in $Excluded) {
        if ($Actual -contains $id) {
            $failures.Add("$Label must exclude $id")
        }
    }
}

[xml]$grade6DropXml = Get-Content -Raw -Encoding UTF8 '.\AddData\CardDropTable\CardDropTable_ch6.xml'
[xml]$grade7DropXml = Get-Content -Raw -Encoding UTF8 '.\AddData\CardDropTable\CardDropTable_ch7.xml'
[xml]$grade6EquipXml = Get-Content -Raw -Encoding UTF8 '.\SpecialStaticInfo\RewardPassiveInfos\EquipReward_ch6.xml'
[xml]$grade7EquipXml = Get-Content -Raw -Encoding UTF8 '.\SpecialStaticInfo\RewardPassiveInfos\EquipReward_ch7.xml'

$grade6Cards = @($grade6DropXml.SelectNodes('//DropTable/Card') | ForEach-Object { $_.InnerText })
$grade7Cards = @($grade7DropXml.SelectNodes('//DropTable/Card') | ForEach-Object { $_.InnerText })
$grade6Books = @($grade6EquipXml.SelectNodes('//RewardList') | ForEach-Object { $_.GetAttribute('ID') })
$grade7Books = @($grade7EquipXml.SelectNodes('//RewardList') | ForEach-Object { $_.GetAttribute('ID') })

Assert-ContainsIds 'Urban Star key-page pool' $grade6Books @('250035', '250036')
Assert-ContainsIds 'Urban Star Purple Tear combat-page pool' $grade6Cards @(
    '609001', '609002', '609003', '609004', '609005', '609006',
    '609007', '609008', '609009', '609010', '609011', '609012'
)
Assert-ContainsIds 'Urban Star Xiao combat-page pool' $grade6Cards @(
    '610002', '610003', '610004', '610005',
    '610008', '610009', '610010', '610011'
)
Assert-ExcludesIds 'Urban Star public combat-page pool' $grade6Cards @(
    '609013', '609020', '609021', '609022', '609023',
    '610001', '610006', '610007', '610012', '610013', '610021', '610022'
)

Assert-ContainsIds 'Impurity Hana key-page pool' $grade7Books @('260001', '260002', '260003', '260004')
Assert-ContainsIds 'Impurity Hana combat-page pool' $grade7Cards @(
    '701002', '701003', '701004', '701005', '701006', '701007',
    '701008', '701009', '701010', '701011', '701012'
)
Assert-ExcludesIds 'Impurity public combat-page pool' $grade7Cards @(
    '701001', '701021', '701022', '701023', '701024'
)

$shop = Get-Content -Raw -Encoding UTF8 '.\abcdcode_LOGLIKE_MOD\ShopBase.cs'
if ($shop -notmatch 'foreach\s*\(LorId cardId in dropTable\.cardIdList\)[\s\S]*?RewardingModel\.GetCardItemOriginAware\(cardId\)') {
    $failures.Add('shop combat-page pool must resolve @origin cards through GetCardItemOriginAware')
}

$unlocks = Get-Content -Raw -Encoding UTF8 '.\RMR_AbnormalityUnlocks.cs'
$blackSilenceVictory = [regex]::Match(
    $unlocks,
    'public static void RecordBlackSilenceVictoryUnlock\(\)(?<body>[\s\S]*?)public static void GrantDistortedEnsembleVictoryRewards'
).Groups['body'].Value
if ($blackSilenceVictory -notmatch 'RecordBlackSilenceStageClear\(\)[\s\S]*GrantBlackSilenceVictoryReward\(\)') {
    $failures.Add('Black Silence victory must immediately grant its key page after recording the clear')
}

$core = Get-Content -Raw -Encoding UTF8 '.\RMR_Core.cs'
if ($core -notmatch 'public static bool GrantBlackSilenceVictoryReward\(\)') {
    $failures.Add('RMRCore must expose the immediate Black Silence victory reward path')
}

if ($failures.Count -gt 0) {
    Write-Host "STATIC_FAILED=$($failures.Count)"
    foreach ($failure in $failures) {
        Write-Host "FAIL: $failure"
    }
    exit 1
}

Write-Host 'PASS: late-chapter key pages, combat pages, origin-aware shop lookup, and Black Silence victory reward are aligned.'
