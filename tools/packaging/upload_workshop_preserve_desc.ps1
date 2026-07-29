# Upload Workshop item 3743867841 while PRESERVING the live page description/title.
# - Scrapes current description from Steam before upload
# - Writes it back into the VDF (so steamcmd does not replace with a short changenote)
# - Only updates content + changenote (and optional preview)
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File .\tools\packaging\upload_workshop_preserve_desc.ps1
#   powershell -File .\tools\packaging\upload_workshop_preserve_desc.ps1 -SteamUser gffnj3 -SkipPrepare

param(
    [string]$WorkshopContentId = "3743867841",
    [string]$SteamUser = "gffnj3",
    [string]$ChangeNote = "",
    [switch]$SkipPrepare,
    [switch]$SkipPreview  # if set, reuse existing preview path from prior VDF (still required by steamcmd)
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent (Split-Path -Parent $scriptDir)

function Escape-VdfString([string]$s) {
    if ($null -eq $s) { return "" }
    # Literal replacements only (avoid PowerShell -replace regex backslash pitfalls).
    $s = $s.Replace('\', '\\')
    $s = $s.Replace('"', '\"')
    $s = $s.Replace("`r`n", '\n').Replace("`n", '\n').Replace("`r", '\n')
    $s = $s.Replace("`t", '\t')
    return $s
}

function Convert-SteamDescHtml([string]$htmlDesc) {
    $s = $htmlDesc
    $s = [regex]::Replace($s, '<div class="bb_h1">(.*?)</div>', '[h1]$1[/h1]', 'Singleline')
    $s = [regex]::Replace($s, '<div class="bb_h2">(.*?)</div>', '[h2]$1[/h2]', 'Singleline')
    $s = [regex]::Replace($s, '<div class="bb_h3">(.*?)</div>', '[h3]$1[/h3]', 'Singleline')
    $s = [regex]::Replace($s, '<ul class="bb_ul">(.*?)</ul>', {
        param($m)
        $inner = $m.Groups[1].Value
        $items = [regex]::Matches($inner, '<li>(.*?)</li>', 'Singleline') | ForEach-Object { '[*]' + $_.Groups[1].Value }
        "[list]`n" + ($items -join "`n") + "`n[/list]"
    }, 'Singleline')
    $s = $s -replace '<br\s*/?>', "`n"
    $s = $s -replace '<i>', '[i]' -replace '</i>', '[/i]'
    $s = $s -replace '<b>', '[b]' -replace '</b>', '[/b]'
    $s = $s -replace '<u>', '[u]' -replace '</u>', '[/u]'
    $s = [regex]::Replace($s, '<a [^>]*href="([^"]+)"[^>]*>(.*?)</a>', '[url=$1]$2[/url]', 'Singleline')
    $s = [System.Net.WebUtility]::HtmlDecode($s)
    $s = [regex]::Replace($s, '<[^>]+>', '')
    $s = $s -replace "`r`n", "`n" -replace "`r", "`n"
    while ($s -match "`n`n`n") { $s = $s -replace "`n`n`n", "`n`n" }
    $s = $s.Trim()
    foreach ($noise in @("Share to your Steam activity feed", "You need to sign in or create an account")) {
        $cut = $s.IndexOf($noise)
        if ($cut -ge 0) { $s = $s.Substring(0, $cut).Trim() }
    }
    # The description block on the page is followed by the comment-thread bootstrap script. Its
    # nested escaped quotes and braces are valid text but NOT valid KeyValues, and steamcmd dies with
    # "RecursiveLoadFromBuffer: got } in key ... Failed to parse build config file".
    # Cut at the first script marker, then fall back to the last legitimate BBCode close tag.
    foreach ($marker in @('$J(', 'InitializeCommentThread', 'g_rgCommentThreadInfo', '"contributors"', '\"contributors\"')) {
        $cut = $s.IndexOf($marker)
        if ($cut -ge 0) { $s = $s.Substring(0, $cut).Trim() }
    }
    $lastClose = -1
    foreach ($tag in @('[/list]', '[/url]', '[/b]', '[/i]', '[/h2]', '[/h1]')) {
        $idx = $s.LastIndexOf($tag)
        if ($idx -ge 0 -and ($idx + $tag.Length) -gt $lastClose) { $lastClose = $idx + $tag.Length }
    }
    if ($lastClose -gt 80) { $s = $s.Substring(0, $lastClose) }
    # unwrap steam linkfilter
    $s = [regex]::Replace($s, '\[url=https://steamcommunity\.com/linkfilter/\?u=([^\]]+)\]', {
        param($m)
        $u = [Uri]::UnescapeDataString($m.Groups[1].Value)
        "[url=$u]"
    })
    $s = $s -replace '\[/url\]\[discord\.com\]', '[/url]'
    $s = $s -replace '\[/url\]\[github\.com\]', '[/url]'
    return $s.Trim()
}

function Get-LiveWorkshopMeta([string]$id) {
    $url = "https://steamcommunity.com/sharedfiles/filedetails/?id=$id"
    Write-Host "Fetching live page: $url" -ForegroundColor Cyan
    $html = (Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 45).Content
    $title = "RMR REBORN fan work [Workshop]"
    if ($html -match 'class="workshopItemTitle"[^>]*>([^<]+)<') {
        $title = [System.Net.WebUtility]::HtmlDecode($Matches[1].Trim())
    }
    if ($html -notmatch '(?s)id="highlightContent"[^>]*>([\s\S]*?)</div>\s*<div') {
        throw "Could not find workshop description block on page (login wall?)."
    }
    $desc = Convert-SteamDescHtml $Matches[1]
    if ($desc.Length -lt 80) {
        throw "Scraped description too short ($($desc.Length) chars) — abort to avoid wiping page."
    }
    return @{ Title = $title; Description = $desc }
}

# --- prepare content ---
if (-not $SkipPrepare) {
    & powershell -ExecutionPolicy Bypass -File (Join-Path $scriptDir "prepare_workshop_upload.ps1") -WorkshopContentId $WorkshopContentId
}

$uploadRoot = "E:\Steam\steamapps\workshop\content\1256670\${WorkshopContentId}_upload"
$backupUpload = "E:\Steam\steamapps\workshop\content\1256670_BACKUPS\${WorkshopContentId}_upload"
if (-not (Test-Path (Join-Path $uploadRoot "Assemblies\dlls\RogueLike Mod Reborn.dll")) `
    -and -not (Test-Path (Join-Path $uploadRoot "Assemblies\RogueLike Mod Reborn.dll"))) {
    # Some trees nest under Assemblies\dlls
    if (-not (Test-Path $uploadRoot)) {
        throw "Upload tree missing: $uploadRoot"
    }
}

# Mirror to BACKUPS path used by historical VDF
if (Test-Path -LiteralPath $backupUpload) { Remove-Item -LiteralPath $backupUpload -Recurse -Force }
New-Item -ItemType Directory -Force -Path (Split-Path $backupUpload -Parent) | Out-Null
# Use named literal paths: the upload source lives on a drive-qualified path and
# positional provider binding has previously misparsed its drive prefix on this machine.
Copy-Item -LiteralPath $uploadRoot -Destination $backupUpload -Recurse -Force

$dllCandidates = @(
    (Join-Path $backupUpload "Assemblies\dlls\RogueLike Mod Reborn.dll"),
    (Join-Path $backupUpload "Assemblies\RogueLike Mod Reborn.dll")
) | Where-Object { Test-Path $_ }
if (-not $dllCandidates) { throw "DLL missing in upload tree" }
$dllHash = (Get-FileHash @($dllCandidates)[0] -Algorithm SHA256).Hash

$preview = Get-ChildItem -LiteralPath $backupUpload -Filter "preview*" -File -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $preview) {
    $preview = Get-ChildItem -LiteralPath $backupUpload -Filter "preview*" -Recurse -File -ErrorAction SilentlyContinue | Select-Object -First 1
}
if (-not $preview) { throw "preview image missing under $backupUpload" }

$meta = Get-LiveWorkshopMeta $WorkshopContentId
$preservePath = Join-Path $scriptDir "_preserved_description_$WorkshopContentId.txt"
[IO.File]::WriteAllText($preservePath, $meta.Description, [Text.UTF8Encoding]::new($false))
Write-Host "Preserved title: $($meta.Title)" -ForegroundColor Green
Write-Host "Preserved description length: $($meta.Description.Length) -> $preservePath" -ForegroundColor Green

if (-not $ChangeNote) {
    $ChangeNote = @"
[h2]2026-07-15 Update[/h2]
[list]
[*][b]Abno pool isolation[/b]: exclusives gated by floor clear (ID+script); safer script match; atlas prune uses ResolveRealizationFloor
[*][b]Emotion tiers[/b]: mid-battle picks re-check exclusive gate after progress reset
[*][b]Chapter pools[/b]: GetTierForRewardPage for shop/post-battle/GetCurChapterCreature
[*][b]EN localization[/b]: Atlas → Compendium (CN 图鉴 / KR 도감 unchanged)
[/list]

[h2]2026-07-15 更新[/h2]
[list]
[*][b]异想体隔离[/b]：专属页按解放门控（ID+脚本）；图鉴修剪用 ResolveRealizationFloor
[*][b]情感分层[/b]：局中再校验专属门禁
[*][b]章节池[/b]：商店/战后/章节池用 GetTierForRewardPage
[*][b]英文[/b]：图鉴显示 Compendium（中文图鉴/韩文도감不变）
[/list]

DLL SHA: $dllHash
"@.Trim()
}

$vdfPath = Join-Path $scriptDir "workshop_item_${WorkshopContentId}.vdf"
$descEsc = Escape-VdfString $meta.Description
$titleEsc = Escape-VdfString $meta.Title
$noteEsc = Escape-VdfString $ChangeNote
$contentEsc = Escape-VdfString $backupUpload
$previewEsc = Escape-VdfString $preview.FullName

$vdf = @"
"workshopitem"
{
	"appid"		"1256670"
	"publishedfileid"		"$WorkshopContentId"
	"contentfolder"		"$contentEsc"
	"previewfile"		"$previewEsc"
	"visibility"		"0"
	"title"		"$titleEsc"
	"description"		"$descEsc"
	"changenote"		"$noteEsc"
}
"@
[IO.File]::WriteAllText($vdfPath, $vdf, [Text.UTF8Encoding]::new($false))
Write-Host "VDF written (description preserved from live page): $vdfPath" -ForegroundColor Cyan

# Parse the VDF the way steamcmd's KeyValues reader does BEFORE spending a login round-trip on it.
# A scraped description that smuggles in page JavaScript produces a file that looks fine but makes
# steamcmd abort with "Failed to parse build config file" after it has already logged in.
function Assert-VdfParses([string]$path) {
    $text = [IO.File]::ReadAllText($path)
    # Steam's KeyValues reader does NOT honour \" inside a quoted string the way JSON does: a
    # backslash is literal and the next '"' ends the value. A scraped description carrying page
    # JSON (\"contributors\":[...]) therefore terminates early, and everything after it is read as
    # keys/braces -- which is exactly the "got } in key" abort. Mirror that rule here, or this check
    # happily passes files steamcmd rejects.
    $i = 0; $n = $text.Length; $tokens = New-Object System.Collections.ArrayList
    while ($i -lt $n) {
        $c = $text[$i]
        if ($c -eq ' ' -or $c -eq "`t" -or $c -eq "`r" -or $c -eq "`n") { $i++; continue }
        if ($c -eq '"') {
            $i++; $start = $i
            while ($i -lt $n -and $text[$i] -ne '"') { $i++ }
            if ($i -ge $n) { throw "VDF parse check failed: unterminated string starting at offset $start" }
            $i++
            [void]$tokens.Add('STR'); continue
        }
        if ($c -eq '{' -or $c -eq '}') { [void]$tokens.Add($c.ToString()); $i++; continue }
        $ctxStart = [Math]::Max(0, $i - 60)
        throw "VDF parse check failed: stray '$c' at offset $i -- a value likely ended early on an unescaped quote. Context: $($text.Substring($ctxStart, [Math]::Min(120, $n - $ctxStart)))"
    }
    if ($tokens.Count -lt 4) { throw "VDF parse check failed: too few tokens" }
    if ($tokens[0] -ne 'STR' -or $tokens[1] -ne '{' -or $tokens[$tokens.Count-1] -ne '}') {
        throw "VDF parse check failed: unexpected top-level shape (a value ended early and swallowed the structure)"
    }
    $body = $tokens.Count - 3
    if ($body % 2 -ne 0) { throw "VDF parse check failed: odd key/value count (a string is unterminated)" }
    Write-Host "  VDF parse check OK ($($body/2) key/value pairs)" -ForegroundColor Green
}
Assert-VdfParses $vdfPath

$steamcmd = @("E:\Steam\steamcmd\steamcmd.exe", "E:\Steam\steamcmd.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $steamcmd) { throw "steamcmd not found" }

Write-Host "Uploading content only — description is the LIVE scraped text, not a short log." -ForegroundColor Yellow
& $steamcmd +login $SteamUser +workshop_build_item $vdfPath +quit
$code = $LASTEXITCODE
Write-Host "steamcmd exit: $code"
if ($code -eq 0) {
    Write-Host "OK. Refresh https://steamcommunity.com/sharedfiles/filedetails/?id=$WorkshopContentId" -ForegroundColor Green
    Write-Host "Verify description still starts with your About section." -ForegroundColor Green
}
exit $code
