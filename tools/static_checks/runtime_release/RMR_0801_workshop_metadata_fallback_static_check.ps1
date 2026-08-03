$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$uploaderPath = Join-Path $repoRoot "tools\packaging\upload_workshop_preserve_desc.ps1"
$source = [IO.File]::ReadAllText($uploaderPath, [Text.Encoding]::UTF8)

function Assert-Contains([string]$pattern, [string]$message) {
    if ($source -notmatch $pattern) {
        throw $message
    }
}

Assert-Contains '\[switch\]\$UsePreservedMeta' `
    'Uploader must support an explicit saved-metadata path when Steam Community is rate-limited.'
Assert-Contains '\[switch\]\$DryRun' `
    'Uploader must support a non-uploading VDF generation test.'
Assert-Contains 'function\s+Get-PreservedWorkshopMeta' `
    'Uploader must load the previously preserved title and description.'
Assert-Contains 'Get-LiveWorkshopMeta[\s\S]*?catch[\s\S]*?Get-PreservedWorkshopMeta' `
    'A live metadata fetch failure must fall back to the preserved snapshot.'
Assert-Contains 'workshop_changenote_\$\{WorkshopContentId\}\.txt[\s\S]*?ReadAllText\(\$defaultChangeNotePath,\s*\$strictUtf8\)' `
    'The default bilingual changenote must be read explicitly as UTF-8 under Windows PowerShell 5.1.'
Assert-Contains 'if\s*\(\$DryRun\)[\s\S]*?exit\s+0[\s\S]*?steamcmd' `
    'Dry-run must stop successfully before SteamCMD starts.'
Assert-Contains 'VDF value contains an unsupported double quote' `
    'Uploader must reject double quotes that Steam KeyValues cannot escape.'

Write-Output 'PASS: Workshop uploader can safely bypass or recover from Steam Community metadata rate limiting.'
