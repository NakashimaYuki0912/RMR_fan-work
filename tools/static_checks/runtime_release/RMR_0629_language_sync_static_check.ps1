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
$root = $script:RepoRoot
function ReadText($rel) { Get-Content -LiteralPath (Join-Path $root $rel) -Raw -Encoding UTF8 }
function AssertContains($name, $text, $needle) { if (-not $text.Contains($needle)) { throw "$name missing: $needle" } }
function AssertNotContains($name, $text, $needle) { if ($text.Contains($needle)) { throw "$name must not contain: $needle" } }

$logLike = ReadText 'abcdcode_LOGLIKE_MOD\LogLikeMod.cs'
$patches = ReadText 'abcdcode_Refactored\LogLikePatches.cs'

AssertContains 'extension text dictionary cleared before localized reload' $logLike 'TextDataModel.textDic.Clear()'
AssertContains 'extension text model marked loaded to prevent stale vanilla lazy-load' $logLike 'TextDataModel._isLoaded = true'
AssertContains 'game language separated from mod localization fallback' $logLike 'ResolveModLocalizeLanguage(language)'
$resolver = [regex]::Match($logLike, 'private static string ResolveInitialTextLanguage\(\)\s*\{(?<body>[\s\S]*?)\n\s*\}\s*\n\s*private static string NormalizeTextLanguage')
if (-not $resolver.Success) { throw 'Could not isolate ResolveInitialTextLanguage.' }
$resolverBody = $resolver.Groups['body'].Value
AssertContains 'runtime localized text loader is preferred over stale option.dat after an in-session switch' $resolverBody 'LocalizedTextLoader loader = Singleton<LocalizedTextLoader>.Instance;'
if ($resolverBody.IndexOf('LocalizedTextLoader loader = Singleton<LocalizedTextLoader>.Instance;') -gt $resolverBody.IndexOf('string optionLanguage = TryReadLanguageFromOptionFile();')) {
    throw 'Runtime language resolver still prefers option.dat over the active LocalizedTextLoader language.'
}
AssertContains 'explicit language reload is normalized before configuration fallback' $logLike 'language = CanonicalizeTextLanguage(language);'
AssertContains 'traditional chinese falls back to simplified mod localize' $logLike 'requested == "trcn"'
AssertContains 'japanese can fall back without changing game language' $logLike 'modLocalizeLanguage'
AssertContains 'satellite effect text uses mod localize fallback language' $logLike 'LoadSatelliteBattleTexts(localizeLanguage)'
AssertContains 'satellite dialog uses mod localize fallback language' $logLike 'LoadSatelliteBattleDialog(localizeLanguage)'
AssertContains 'mystery localization uses mod localize fallback language' $logLike 'RogueMysteryXmlList.Instance.Init(localizeLanguage)'
AssertContains 'startup refreshes vanilla abnormality card descriptions for selected language' $logLike 'RefreshVanillaAbnormalityTextData(initialLanguage'
AssertContains 'vanilla abnormality refresh reloads card descriptions' $logLike 'LoadAbnormalityCardDescriptions(language)'
AssertContains 'vanilla abnormality refresh reloads ability descriptions' $logLike 'LoadAbnormalityAbilityDescription(language)'
AssertContains 'a full localization refresh always rebuilds already-open localized panels' $patches 'ReplayPanelPopulationForLanguage();'
AssertNotContains 'panel replay must not be limited to a language-string change' $patches 'if (languageActuallyChanged)'
AssertContains 'japanese font probe checks kana not only kanji' $logLike 'return "\u65e5\u3042\u30a2\u6f22";'
AssertContains 'simplified chinese font probe checks common simplified glyphs' $logLike 'return "\u56fe\u6c49\u8bed\u6d4b\u8bd5";'
AssertContains 'traditional chinese font probe checks common traditional glyphs' $logLike 'return "\u5716\u6f22\u8a9e\u6e2c\u8a66";'
AssertContains 'korean font probe remains explicit' $logLike 'return "\ud55c\uae00\ub3c4";'
AssertNotContains 'RMR must not force preferred TMP font over vanilla UI' $logLike 'ResolvePreferredLocalizedTmpFont'
AssertContains 'vanilla TextDataModel override is gated before reading mod text' $patches 'ShouldOverrideVanillaTextWithRmrText(id)'
AssertContains 'generic mod text only overrides while RMR is active' $patches 'LogLikeMod.CheckStage()'
AssertContains 'main menu RMR text remains available outside RMR stage' $patches 'id.StartsWith("ui_RMR_", StringComparison.Ordinal)'
AssertContains 'vanilla TextDataModel only overridden by non-empty mod dictionary values' $patches 'if (string.IsNullOrEmpty(text))'
AssertNotContains 'RMR must not recursively replace TMP fonts in vanilla card UI' $patches 'ApplyRmrTmpFont'
AssertNotContains 'RMR must not add font-only BattleDiceCardUI postfix' $patches 'BattleDiceCardUI_SetCard_RmrFont'
AssertNotContains 'RMR must not add font-only UIOriginCardSlot postfix' $patches 'UIOriginCardSlot_SetData_RmrFont'
AssertNotContains 'RMR must not add font-only dice behaviour postfix' $patches 'SetBehaviourInfo_RmrFont'

foreach ($file in @('Localize\cn\UIs.txt', 'Localize\en\UIs.txt', 'Localize\kr\UIs.txt')) {
    [xml](Get-Content -Raw -Encoding UTF8 (Join-Path $root $file)) | Out-Null
}

Write-Host 'RMR language sync static check passed.'

