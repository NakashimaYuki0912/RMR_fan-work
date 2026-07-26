// -----------------------------------------------------------------------------
// Library of Ruina mod script: RewardingModel
// Namespace/file: ruina-roguelike-reborn-main\abcdcode_LOGLIKE_MOD\RewardingModel.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
using LOR_DiceSystem;
using LOR_XML;
using RogueLike_Mod_Reborn;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace abcdcode_LOGLIKE_MOD
{

    /// <summary>RewardingModel</summary>

    public class RewardingModel
    {
        public static RewardingModel.RewardFlag rewardFlag;
        private const double BattleCardRewardRetentionRate = 0.7;
        private const double NormalBattleCardRewardRetentionRate = 0.49;
        private static readonly HashSet<LorId> NormalizedDropBookRewardIds = new HashSet<LorId>();
        private static bool BossFallbackRewardCheckedThisBattle;
        private static readonly Dictionary<int, string> KnownBookNameOverrides = new Dictionary<int, string>
        {
            { 260001, "Hana\u534f\u4f1a3\u79d1\u6536\u5c3e\u4eba\u4e4b\u9875" },
            { 260002, "\u7f8e\u91cc\u5948\u4e4b\u9875" },
            { 260003, "\u54c8\u7f57\u5fb7\u4e4b\u9875" },
            { 260004, "\u5965\u5229\u7ef4\u8036\u4e4b\u9875" },
            { 250013, "\u963f\u5c14\u52a0\u5229\u4e9a\u4e4b\u9875" },
            { 1301011, "\u83f2\u5229\u666e\u4e4b\u9875" },
            { 1302011, "\u827e\u7433\u4e4b\u9875" },
            { 1303011, "\u683c\u857e\u5854\u4e4b\u9875" },
            { 1304011, "\u4e0d\u6765\u6885\u4e4b\u9875" },
            { 1305011, "\u5965\u65af\u74e6\u5c14\u5fb7\u4e4b\u9875" },
            { 1306011, "\u5854\u5c3c\u5a05\u4e4b\u9875" },
            { 1307011, "\u5728\u5baa\u4e4b\u9875" },
            { 1308011, "\u4f0a\u83b2\u5a1c\u4e4b\u9875" },
            { 1309011, "\u666e\u9c81\u6258\u4e4b\u9875" },
        };
        private static readonly Dictionary<string, string> KnownTextReplacements = new Dictionary<string, string>
        {
            { "\ud558\ub098\u534f\u4f1a", "Hana\u534f\u4f1a" },
            { "\ud558\ub098 \ud574\uacb0\uc0ac", "Hana\u534f\u4f1a\u6536\u5c3e\u4eba" },
            { "\ud558\ub098", "Hana" },
            { "\uacf5\uba85 \uc870\uc808", "\u5171\u632f\u8c03\u8282" },
            { "\uc544\ub974\uac08\ub9ac\uc544 \ub09c\ubb34 \uad11\uc5ed", "Crescendo" },
            { "\uc544\ub974\uac08\ub9ac\uc544 \ub300\ub2e8\uc6d0", "\u843d\u5e55\u7ec8\u66f2" },
            { "\uc544\ub974\uac08\ub9ac\uc544", "\u963f\u5c14\u52a0\u5229\u4e9a" },
        };
        #region --- Reward generation ---


        public static void ResetDropBookRewardNormalization()
        {
            NormalizedDropBookRewardIds.Clear();
            BossFallbackRewardCheckedThisBattle = false;
        }
        #endregion

        #region --- Book / card data ---


        public static string GetChapterText(int grade)
        {
            switch (grade)
            {
                case 1:
                    return TextDataModel.GetText("ui_maintitle_citystate_1");
                case 2:
                    return TextDataModel.GetText("ui_maintitle_citystate_2");
                case 3:
                    return TextDataModel.GetText("ui_maintitle_citystate_3");
                case 4:
                    return TextDataModel.GetText("ui_maintitle_citystate_4");
                case 5:
                    return TextDataModel.GetText("ui_maintitle_citystate_5");
                case 6:
                    return TextDataModel.GetText("ui_maintitle_citystate_6");
                case 7:
                    return TextDataModel.GetText("ui_maintitle_citystate_7");
                default:
                    return "Not Found";
            }
        }

        public static string GetResistText(AtkResist resist)
        {
            switch (resist)
            {
                case AtkResist.Weak:
                    return TextDataModel.GetText("ui_resistance_weak");
                case AtkResist.Vulnerable:
                    return TextDataModel.GetText("ui_resistance_vulnerable");
                case AtkResist.Normal:
                    return TextDataModel.GetText("ui_resistance_normal");
                case AtkResist.Endure:
                    return TextDataModel.GetText("ui_resistance_endure");
                case AtkResist.Resist:
                    return TextDataModel.GetText("ui_resistance_resist");
                case AtkResist.Immune:
                    return TextDataModel.GetText("ui_resistance_immune");
                default:
                    return "???";
            }
        }

        public static string GetAblilityText(BookXmlInfo bookinfo)
        {
            if (bookinfo?.EquipEffect == null)
                return string.Empty;
            string str = $"{$"{$"{$"{$"{$"{$"{string.Empty}{TextDataModel.GetText("ui_ability_hp")}: {bookinfo.EquipEffect.Hp.ToString()}{Environment.NewLine}"}{TextDataModel.GetText("ui_ability_break")}: {bookinfo.EquipEffect.Break.ToString()}{Environment.NewLine}"}{abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("Equip_Light")}: {bookinfo.EquipEffect.StartPlayPoint.ToString()}{Environment.NewLine}"}{abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("Equip_SpeedArea")}: {bookinfo.EquipEffect.SpeedMin.ToString()}~{bookinfo.EquipEffect.Speed.ToString()}{Environment.NewLine}"}{abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("Equip_Slash")}: {RewardingModel.GetResistText(bookinfo.EquipEffect.SResist)}/{RewardingModel.GetResistText(bookinfo.EquipEffect.SBResist)}{Environment.NewLine}"}{abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("Equip_Penetrate")}: {RewardingModel.GetResistText(bookinfo.EquipEffect.PResist)}/{RewardingModel.GetResistText(bookinfo.EquipEffect.PBResist)}{Environment.NewLine}"}{abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("Equip_Hit")}: {RewardingModel.GetResistText(bookinfo.EquipEffect.HResist)}/{RewardingModel.GetResistText(bookinfo.EquipEffect.HBResist)}{Environment.NewLine}";
            // One passive per line with full desc (not a Hangul/empty comma list).
            var passiveLines = new List<string>();
            if (bookinfo.EquipEffect.PassiveList != null)
            {
                for (int index = 0; index < bookinfo.EquipEffect.PassiveList.Count; ++index)
                {
                    LorId pid = bookinfo.EquipEffect.PassiveList[index];
                    string passiveName = RewardingModel.GetPassiveName(pid);
                    if (string.IsNullOrEmpty(passiveName) || IsPoorDisplayName(passiveName))
                        continue;
                    string passiveDesc = RewardingModel.GetPassiveDesc(pid);
                    if (!string.IsNullOrEmpty(passiveDesc) && !IsPoorDisplayName(passiveDesc))
                        passiveLines.Add($"· {passiveName}：{passiveDesc}");
                    else
                        passiveLines.Add($"· {passiveName}");
                }
            }
            string passiveBlock = passiveLines.Count > 0
                ? string.Join(Environment.NewLine, passiveLines)
                : string.Empty;
            string header = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("Equip_Passive");
            // Reward / equip-page pick UI: stats + passives only.
            // Do NOT append vanilla Books.txt story paragraphs (library "书籍故事") —
            // players pick key pages for combat stats, not lore dump.
            string body = string.IsNullOrEmpty(passiveBlock)
                ? $"{str}{header}:"
                : $"{str}{header}:{Environment.NewLine}{passiveBlock}";
            return SanitizeDisplayText(body);
        }

        /// <summary>
        /// Origin-aware book story paragraphs from BookDescXmlList.GetBookText.
        /// Kept for atlas/detail callers that explicitly want lore; reward ability text must not use this.
        /// </summary>
        public static string GetLocalizedBookStory(BookXmlInfo book)
        {
            if (book == null)
                return string.Empty;
            BookDescXmlList list = Singleton<BookDescXmlList>.Instance;
            if (list == null)
                return string.Empty;

            List<string> paragraphs = null;
            void Try(LorId id)
            {
                if (paragraphs != null && paragraphs.Count > 0)
                    return;
                if (id == null || id == LorId.None)
                    return;
                try { paragraphs = list.GetBookText(id); } catch { /* ignore */ }
            }

            if (book.id != null)
            {
                foreach (LorId candidate in GetOriginAwareIds(book.id))
                    Try(candidate);
                Try(new LorId(book.id.id));
            }
            if (book.TextId > 0)
            {
                Try(new LorId(book.TextId));
                if (book.id != null && !IsOriginPackage(book.id.packageId))
                    Try(new LorId(book.id.packageId, book.TextId));
            }

            if (paragraphs == null || paragraphs.Count == 0)
                return string.Empty;
            // Prefer non-Hangul paragraphs for CN display.
            var lines = new List<string>();
            foreach (string p in paragraphs)
            {
                if (string.IsNullOrEmpty(p) || IsPoorDisplayName(p))
                    continue;
                lines.Add(p.Trim());
            }
            return lines.Count == 0 ? string.Empty : string.Join(Environment.NewLine, lines);
        }
        #endregion

        #region --- Other helpers ---


        public static string SanitizeDisplayText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            // Fast path: most strings need no rewrite — avoid walking the table every UI refresh.
            bool any = false;
            foreach (KeyValuePair<string, string> replacement in KnownTextReplacements)
            {
                if (text.IndexOf(replacement.Key, StringComparison.Ordinal) >= 0)
                {
                    any = true;
                    break;
                }
            }
            if (!any)
                return text;
            foreach (KeyValuePair<string, string> replacement in KnownTextReplacements)
                text = text.Replace(replacement.Key, replacement.Value);
            return text;
        }

        // ---- Session caches (passive/book localize is called on every equip/hover refresh) ----
        private static string _localizeCacheLanguage;
        private static readonly Dictionary<string, string> PassiveNameCache = new Dictionary<string, string>(256);
        private static readonly Dictionary<string, string> PassiveDescCache = new Dictionary<string, string>(256);
        private static readonly Dictionary<string, string> BookNameCache = new Dictionary<string, string>(256);
        private const int LocalizeCacheSoftCap = 4096;

        private static void EnsureLocalizeCacheLanguage()
        {
            string lang = null;
            try { lang = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.CurrentLanguage; } catch { /* ignore */ }
            if (string.IsNullOrEmpty(lang))
            {
                try { lang = global::TextDataModel.CurrentLanguage; } catch { /* ignore */ }
            }
            if (lang == null)
                lang = string.Empty;
            if (_localizeCacheLanguage == lang)
                return;
            _localizeCacheLanguage = lang;
            PassiveNameCache.Clear();
            PassiveDescCache.Clear();
            BookNameCache.Clear();
        }

        /// <summary>Called by session hygiene when sets grow or language may have changed.</summary>
        public static void ClearLocalizeCachesIfStale()
        {
            EnsureLocalizeCacheLanguage();
            if (PassiveNameCache.Count > LocalizeCacheSoftCap
                || PassiveDescCache.Count > LocalizeCacheSoftCap
                || BookNameCache.Count > LocalizeCacheSoftCap)
            {
                PassiveNameCache.Clear();
                PassiveDescCache.Clear();
                BookNameCache.Clear();
            }
        }

        public static void ClearLocalizeCaches()
        {
            PassiveNameCache.Clear();
            PassiveDescCache.Clear();
            BookNameCache.Clear();
            _localizeCacheLanguage = null;
        }

        private static string LorIdCacheKey(LorId id)
        {
            if (id == null)
                return string.Empty;
            return (id.packageId ?? string.Empty) + ":" + id.id;
        }
        #endregion

        #region --- Book / card data ---


        private static bool TryGetKnownBookName(BookXmlInfo book, out string name)
        {
            name = null;
            if (book == null)
                return false;
            if (book.id != null && KnownBookNameOverrides.TryGetValue(book.id.id, out name))
                return true;
            return book.TextId > 0 && KnownBookNameOverrides.TryGetValue(book.TextId, out name);
        }
        #endregion

        #region --- Other helpers ---


        private static bool IsOriginPackage(string packageId)
        {
            return string.IsNullOrEmpty(packageId) || packageId == "@origin";
        }
        #endregion

        #region --- Book / card data ---


        private static List<LorId> GetOriginAwareIds(LorId id)
        {
            var result = new List<LorId>();
            if (id == null || id == LorId.None)
                return result;
            result.Add(id);
            // Always try bare / empty-package variants. BookXmlList.GetData never returns null —
            // missing entries become isError "ModNeeded" stubs — so callers must walk candidates.
            LorId bareId = new LorId(id.id);
            if (!result.Contains(bareId))
                result.Add(bareId);
            LorId emptyPackageId = new LorId(string.Empty, id.id);
            if (!result.Contains(emptyPackageId))
                result.Add(emptyPackageId);
            if (!IsOriginPackage(id.packageId))
            {
                LorId originTagged = new LorId("@origin", id.id);
                if (!result.Contains(originTagged))
                    result.Add(originTagged);
            }
            return result;
        }

        /// <summary>
        /// BookXmlList.GetData fabricates a zero-stat stub (InnerName="ModNeeded", isError=true)
        /// when the LorId is missing. Treat those as missing data.
        /// </summary>
        public static bool IsValidBookData(BookXmlInfo book)
        {
            if (book == null || book.isError)
                return false;
            if (string.Equals(book.InnerName, "ModNeeded", StringComparison.OrdinalIgnoreCase))
                return false;
            // Real key pages always have HP. Default EquipEffect also uses StartPlayPoint=3 with Hp=0.
            if (book.EquipEffect == null || book.EquipEffect.Hp <= 0)
                return false;
            return true;
        }

        public static bool IsValidCardData(DiceCardXmlInfo card)
        {
            return card != null && !card.isError;
        }

        public static BookXmlInfo GetBookDataOriginAware(LorId id)
        {
            if (id == null || id == LorId.None)
                return null;

            BookXmlList list = Singleton<BookXmlList>.Instance;
            if (list == null)
                return null;

            foreach (LorId candidate in GetOriginAwareIds(id))
            {
                // Prefer dictionary lookup; reject ModNeeded stubs so later candidates can win.
                BookXmlInfo book = list.GetData(candidate, false);
                if (IsValidBookData(book))
                    return book;
                if (IsOriginPackage(candidate.packageId))
                {
                    book = list.GetData(candidate.id);
                    if (IsValidBookData(book))
                        return book;
                }
            }

            // Last resort: scan full list by numeric id (package mismatch / workshop merge).
            try
            {
                List<BookXmlInfo> all = list.GetList();
                if (all != null)
                {
                    BookXmlInfo match = all.Find(b =>
                        IsValidBookData(b)
                        && b.id != null
                        && b.id.id == id.id
                        && (IsOriginPackage(id.packageId) || IsOriginPackage(b.id.packageId) || b.id.packageId == id.packageId));
                    if (match != null)
                        return match;
                    // If caller asked for @origin, any valid vanilla-or-workshop page with that id is fine.
                    if (IsOriginPackage(id.packageId))
                    {
                        match = all.Find(b => IsValidBookData(b) && b.id != null && b.id.id == id.id);
                        if (match != null)
                            return match;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR] GetBookDataOriginAware scan failed: " + ex.Message);
            }
            return null;
        }

        public static DiceCardXmlInfo GetCardItemOriginAware(LorId id)
        {
            if (id == null || id == LorId.None || ItemXmlDataList.instance == null)
                return null;

            foreach (LorId candidate in GetOriginAwareIds(id))
            {
                // error:false fabricates a dummy 1-4 dice card — use true so missing → null.
                DiceCardXmlInfo card = ItemXmlDataList.instance.GetCardItem(candidate, true);
                if (IsValidCardData(card))
                    return card;
                if (IsOriginPackage(candidate.packageId))
                {
                    card = ItemXmlDataList.instance.GetCardItem(candidate.id, true);
                    if (IsValidCardData(card))
                        return card;
                }
            }
            return null;
        }

        public static string GetLocalizedCardName(DiceCardXmlInfo card)
        {
            if (card == null)
                return string.Empty;

            // Upgrade cards are mod-registered with workshopName = "{baseWorkshopName}+".
            // Prefer vanilla TextId / original id localization first, then append "+".
            bool isUpgrade = false;
            try
            {
                string wid = card.workshopID ?? "";
                isUpgrade = wid.IndexOf(LogCardUpgradeManager.UpgradeKeyword, StringComparison.Ordinal) >= 0
                    || (!string.IsNullOrEmpty(card.workshopName) && card.workshopName.EndsWith("+", StringComparison.Ordinal));
            }
            catch { /* ignore */ }

            // 1) TextId (upgrade copies preserve base TextId)
            if (card.TextId != null && card.TextId != LorId.None)
            {
                string name = Singleton<BattleCardDescXmlList>.Instance.GetCardName(card.TextId);
                if (!string.IsNullOrEmpty(name) && !string.Equals(name, "Not Found", StringComparison.OrdinalIgnoreCase) && !IsPoorDisplayName(name))
                    return SanitizeDisplayText(isUpgrade && !name.EndsWith("+") ? name + "+" : name);
                name = Singleton<BattleCardDescXmlList>.Instance.GetCardName(new LorId(card.TextId.id));
                if (!string.IsNullOrEmpty(name) && !string.Equals(name, "Not Found", StringComparison.OrdinalIgnoreCase) && !IsPoorDisplayName(name))
                    return SanitizeDisplayText(isUpgrade && !name.EndsWith("+") ? name + "+" : name);
            }

            // 2) Origin-aware id walk (and original id for upgrades)
            foreach (LorId candidate in GetOriginAwareIds(card.id))
            {
                string name = Singleton<BattleCardDescXmlList>.Instance.GetCardName(candidate);
                if (!string.IsNullOrEmpty(name) && !string.Equals(name, "Not Found", StringComparison.OrdinalIgnoreCase) && !IsPoorDisplayName(name))
                    return SanitizeDisplayText(isUpgrade && !name.EndsWith("+") ? name + "+" : name);
                if (IsOriginPackage(candidate.packageId))
                {
                    name = Singleton<BattleCardDescXmlList>.Instance.GetCardName(new LorId(candidate.id));
                    if (!string.IsNullOrEmpty(name) && !string.Equals(name, "Not Found", StringComparison.OrdinalIgnoreCase) && !IsPoorDisplayName(name))
                        return SanitizeDisplayText(isUpgrade && !name.EndsWith("+") ? name + "+" : name);
                }
            }
            try
            {
                LorId orig = card.id != null ? card.id.GetOriginalId() : null;
                if (orig != null && orig != card.id)
                {
                    string name = Singleton<BattleCardDescXmlList>.Instance.GetCardName(orig);
                    if (!string.IsNullOrEmpty(name) && !string.Equals(name, "Not Found", StringComparison.OrdinalIgnoreCase) && !IsPoorDisplayName(name))
                        return SanitizeDisplayText(isUpgrade && !name.EndsWith("+") ? name + "+" : name);
                    name = Singleton<BattleCardDescXmlList>.Instance.GetCardName(new LorId(orig.id));
                    if (!string.IsNullOrEmpty(name) && !string.Equals(name, "Not Found", StringComparison.OrdinalIgnoreCase) && !IsPoorDisplayName(name))
                        return SanitizeDisplayText(isUpgrade && !name.EndsWith("+") ? name + "+" : name);
                }
            }
            catch { /* ignore */ }

            // 3) Only then workshopName — but strip trailing + and re-localize base if possible
            string ws = card.workshopName ?? "";
            if (!string.IsNullOrEmpty(ws))
            {
                string baseWs = ws.TrimEnd('+');
                if (!string.IsNullOrEmpty(baseWs) && !IsPoorDisplayName(baseWs)
                    && baseWs.IndexOf('\u25A1') < 0 && baseWs.IndexOf('\uFFFD') < 0)
                {
                    // Prefer not to show raw English workshop ids when we already failed localization;
                    // still better than empty.
                    string cleaned = SanitizeDisplayText(baseWs);
                    if (isUpgrade && !cleaned.EndsWith("+", StringComparison.Ordinal))
                        cleaned += "+";
                    // Reject pure non-CJK garbage for CN if entire string is replacement boxes
                    if (!LooksLikeTofu(cleaned))
                        return cleaned;
                }
            }

            string fallback = SanitizeDisplayText(card.Name ?? string.Empty);
            if (isUpgrade && !string.IsNullOrEmpty(fallback) && !fallback.EndsWith("+", StringComparison.Ordinal))
                fallback += "+";
            return fallback;
        }
        #endregion

        #region --- Other helpers ---


        private static bool LooksLikeTofu(string text)
        {
            if (string.IsNullOrEmpty(text))
                return true;
            int bad = 0, good = 0;
            foreach (char c in text)
            {
                if (c == '\u25A1' || c == '\uFFFD' || c == '\u25A0' || c == '?')
                    bad++;
                else if (!char.IsWhiteSpace(c) && c != '+')
                    good++;
            }
            return good == 0 && bad > 0;
        }
        #endregion

        #region --- Book / card data ---


        public static string GetLocalizedCardAbilityDesc(DiceCardXmlInfo card)
        {
            if (card == null)
                return string.Empty;
            foreach (LorId candidate in GetOriginAwareIds(card.id))
            {
                string text = Singleton<BattleCardDescXmlList>.Instance.GetAbilityDesc(candidate);
                if (!string.IsNullOrEmpty(text))
                    return SanitizeDisplayText(text);
                if (IsOriginPackage(candidate.packageId))
                {
                    text = Singleton<BattleCardDescXmlList>.Instance.GetAbilityDesc(new LorId(candidate.id));
                    if (!string.IsNullOrEmpty(text))
                        return SanitizeDisplayText(text);
                }
            }
            List<string> abilityDesc = Singleton<BattleCardAbilityDescXmlList>.Instance.GetAbilityDesc(card);
            if (abilityDesc != null && abilityDesc.Count > 0)
                return SanitizeDisplayText(string.Join("\n", abilityDesc));
            return string.Empty;
        }

        public static string GetLocalizedBookName(BookXmlInfo book)
        {
            if (book == null)
                return string.Empty;

            EnsureLocalizeCacheLanguage();
            string cacheKey = null;
            if (book.id != null)
            {
                cacheKey = LorIdCacheKey(book.id) + "#" + book.TextId;
                if (BookNameCache.TryGetValue(cacheKey, out string cached))
                    return cached;
            }

            if (TryGetKnownBookName(book, out string knownName) && !IsPoorDisplayName(knownName))
            {
                if (cacheKey != null)
                    BookNameCache[cacheKey] = knownName;
                return knownName;
            }

            List<string> candidates = new List<string>();
            void Consider(string name)
            {
                if (!string.IsNullOrEmpty(name)
                    && !string.Equals(name, "ModNeeded", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(name, "Not Found", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(name, "Not found", StringComparison.OrdinalIgnoreCase))
                    candidates.Add(name);
            }

            if (book.id != null)
            {
                foreach (LorId candidate in GetOriginAwareIds(book.id))
                {
                    Consider(Singleton<BookDescXmlList>.Instance.GetBookName(candidate));
                    if (IsOriginPackage(candidate.packageId))
                        Consider(Singleton<BookDescXmlList>.Instance.GetBookName(new LorId(candidate.id)));
                }
                // Bare numeric id (origin dictionary) — shared vanilla keypages.
                Consider(Singleton<BookDescXmlList>.Instance.GetBookName(new LorId(book.id.id)));
            }

            // TextId is often the real Books.txt key (e.g. book 211003 → TextId 23).
            if (book.TextId > 0)
            {
                Consider(Singleton<BookDescXmlList>.Instance.GetBookName(new LorId(book.TextId)));
                if (book.id != null && !IsOriginPackage(book.id.packageId))
                    Consider(Singleton<BookDescXmlList>.Instance.GetBookName(new LorId(book.id.packageId, book.TextId)));
            }

            if (!string.IsNullOrEmpty(book.InnerName)
                && !string.Equals(book.InnerName, "ModNeeded", StringComparison.OrdinalIgnoreCase)
                && !book.isError)
                Consider(book.InnerName);

            string best = PickBestDisplayName(candidates);
            string result;
            if (!string.IsNullOrEmpty(best))
                result = SanitizeDisplayText(best);
            else if (string.Equals(book.InnerName, "ModNeeded", StringComparison.OrdinalIgnoreCase) || book.isError)
                result = string.Empty;
            else
                result = SanitizeDisplayText(book.InnerName ?? string.Empty);

            if (cacheKey != null)
                BookNameCache[cacheKey] = result;
            return result;
        }

        public static string GetPassiveName(LorId passiveId)
        {
            if (passiveId == null || passiveId == LorId.None)
                return string.Empty;

            // Placeholder / garbage passives (e.g. 1000000000) have no desc — hide them.
            if (passiveId.id <= 0 || passiveId.id >= 1000000000)
                return string.Empty;

            EnsureLocalizeCacheLanguage();
            string cacheKey = LorIdCacheKey(passiveId);
            if (PassiveNameCache.TryGetValue(cacheKey, out string cached))
                return cached;

            List<string> candidates = new List<string>();
            void Consider(string name)
            {
                if (!string.IsNullOrEmpty(name))
                    candidates.Add(name);
            }

            PassiveDescXmlList list = Singleton<PassiveDescXmlList>.Instance;
            foreach (LorId candidate in GetOriginAwareIds(passiveId))
            {
                if (list != null)
                {
                    Consider(list.GetName(candidate));
                    if (IsOriginPackage(candidate.packageId))
                        Consider(list.GetName(candidate.id));
                }
                // Workshop BookPassiveInfo reads PassiveXmlInfo fields — stamp path lives there.
                try
                {
                    PassiveXmlInfo pxi = Singleton<PassiveXmlList>.Instance?.GetData(candidate)
                        ?? Singleton<PassiveXmlList>.Instance?.GetData(new LorId(candidate.id));
                    if (pxi != null && !string.IsNullOrEmpty(pxi.name))
                        Consider(pxi.name);
                }
                catch { /* ignore */ }
            }
            // Always try bare origin id — mod books often wrap vanilla passives with Pid=@origin
            // but InitializeLorIds can still leave a workshop package on some entries.
            if (list != null)
            {
                Consider(list.GetName(passiveId.id));
                Consider(list.GetName(new LorId(passiveId.id)));
            }
            try
            {
                PassiveXmlInfo bare = Singleton<PassiveXmlList>.Instance?.GetData(new LorId(passiveId.id));
                if (bare != null && !string.IsNullOrEmpty(bare.name))
                    Consider(bare.name);
            }
            catch { /* ignore */ }

            string best = PickBestDisplayName(candidates);
            string result = !string.IsNullOrEmpty(best) ? SanitizeDisplayText(best) : string.Empty;
            PassiveNameCache[cacheKey] = result;
            return result;
        }

        /// <summary>
        /// Origin-aware passive description (mirrors GetPassiveName).
        /// Mod package ids often hide the vanilla CN PassiveDesc entry.
        /// </summary>
        public static string GetPassiveDesc(LorId passiveId)
        {
            if (passiveId == null || passiveId == LorId.None)
                return string.Empty;
            if (passiveId.id <= 0 || passiveId.id >= 1000000000)
                return string.Empty;

            EnsureLocalizeCacheLanguage();
            string cacheKey = LorIdCacheKey(passiveId);
            if (PassiveDescCache.TryGetValue(cacheKey, out string cached))
                return cached;

            List<string> candidates = new List<string>();
            void Consider(string desc)
            {
                if (!string.IsNullOrEmpty(desc))
                    candidates.Add(desc);
            }

            PassiveDescXmlList list = Singleton<PassiveDescXmlList>.Instance;
            if (list == null)
                return string.Empty;

            foreach (LorId candidate in GetOriginAwareIds(passiveId))
            {
                Consider(list.GetDesc(candidate));
                if (IsOriginPackage(candidate.packageId))
                    Consider(list.GetDesc(candidate.id));
                try
                {
                    PassiveXmlInfo pxi = Singleton<PassiveXmlList>.Instance?.GetData(candidate)
                        ?? Singleton<PassiveXmlList>.Instance?.GetData(new LorId(candidate.id));
                    if (pxi != null && !string.IsNullOrEmpty(pxi.desc))
                        Consider(pxi.desc);
                }
                catch { /* ignore */ }
            }
            Consider(list.GetDesc(passiveId.id));
            Consider(list.GetDesc(new LorId(passiveId.id)));
            try
            {
                PassiveXmlInfo bare = Singleton<PassiveXmlList>.Instance?.GetData(new LorId(passiveId.id));
                if (bare != null && !string.IsNullOrEmpty(bare.desc))
                    Consider(bare.desc);
            }
            catch { /* ignore */ }

            string best = PickBestDisplayName(candidates);
            string result = !string.IsNullOrEmpty(best) ? SanitizeDisplayText(best) : string.Empty;
            PassiveDescCache[cacheKey] = result;
            return result;
        }
        #endregion

        #region --- Reward generation ---


        /// <summary>
        /// Prefer CJK / Latin display names over Hangul leftovers that render as tofu
        /// when the active TMP face is a Chinese-only CJK font.
        /// </summary>
        private static string PickBestDisplayName(List<string> candidates)
        {
            if (candidates == null || candidates.Count == 0)
                return null;
            string best = null;
            int bestScore = int.MinValue;
            foreach (string c in candidates)
            {
                if (string.IsNullOrEmpty(c))
                    continue;
                int score = ScoreDisplayName(c);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }
            return best;
        }
        #endregion

        #region --- Other helpers ---


        private static int ScoreDisplayName(string text)
        {
            if (string.IsNullOrEmpty(text))
                return int.MinValue;
            int score = 0;
            int hangul = 0;
            int han = 0;
            int tofu = 0;
            int other = 0;
            foreach (char ch in text)
            {
                if (ch >= 0xAC00 && ch <= 0xD7A3)
                    hangul++;
                else if (ch >= 0x4E00 && ch <= 0x9FFF)
                {
                    // 口 (U+53E3) often appears as repeated tofu placeholders when glyphs fail.
                    if (ch == '\u53E3' || ch == '\u25A1' || ch == '\uFFFD' || ch == '?')
                        tofu++;
                    else
                        han++;
                }
                else if (ch == '\u25A1' || ch == '\uFFFD' || ch == '\u2610')
                    tofu++;
                else if (!char.IsWhiteSpace(ch))
                    other++;
            }
            // Strongly prefer Han (Chinese) names over Hangul (Korean tofu risk).
            score += han * 8;
            score += other;
            score -= hangul * 12;
            score -= tofu * 20;
            if (hangul > 0 && han == 0)
                score -= 50;
            // Mostly tofu boxes → unusable
            if (tofu > 0 && tofu >= Math.Max(1, (han + hangul + other) / 2))
                score -= 100;
            return score;
        }

        public static bool IsPoorDisplayNamePublic(string text) => IsPoorDisplayName(text);

        private static bool IsPoorDisplayName(string text)
        {
            if (string.IsNullOrEmpty(text))
                return true;
            return ScoreDisplayName(text) < 0;
        }
        #endregion

        #region --- Reward generation ---


        public static RewardPassiveInfo FindRewardInfo(EmotionCardXmlInfo card)
        {
            if (card == null || Singleton<RewardPassivesList>.Instance?.infos == null)
                return null;
            foreach (RewardPassivesInfo infoGroup in Singleton<RewardPassivesList>.Instance.infos)
            {
                if (infoGroup?.RewardPassiveList == null)
                    continue;
                foreach (RewardPassiveInfo info in infoGroup.RewardPassiveList)
                {
                    if (info == null || info.passiveid != card.id)
                        continue;
                    EmotionCardXmlInfo registered = LogLikeMod.GetRegisteredPickUpXml(info);
                    if (registered == card)
                        return info;
                }
            }
            return null;
        }
        #endregion

        #region --- Book / card data ---


        public static string GetRaritytext(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Common:
                    return TextDataModel.GetText("ui_rarity_common");
                case Rarity.Uncommon:
                    return TextDataModel.GetText("ui_rarity_uncommon");
                case Rarity.Rare:
                    return TextDataModel.GetText("ui_rarity_rare");
                case Rarity.Unique:
                    return TextDataModel.GetText("ui_rarity_unique");
                default:
                    return "Not Found";
            }
        }
        #endregion

        #region --- Reward generation ---


        public static void CreateEquipRewardXmlData(RewardPassiveInfo info)
        {
            if (info == null)
                return;
            BookXmlInfo data = RewardingModel.GetBookDataOriginAware(info.id);
            EmotionCardXmlInfo pickUpXml = LogLikeMod.GetRegisteredPickUpXml(info);
            if (data == null || pickUpXml == null)
                return;
            // Must be dictionary-backed (GetAbnormalityCard may return a transient "Not found" stub).
            AbnormalityCard abnormalityCard = PickUpModel_RMRVanillaEmotion.EnsureDescEntry(pickUpXml.Name);
            if (abnormalityCard == null)
                return;
            string bookName = RewardingModel.GetLocalizedBookName(data);
            // Avoid permanently stamping Hangul InnerName / empty into the pick UI cache.
            if (!string.IsNullOrEmpty(bookName) && !IsPoorDisplayName(bookName))
                abnormalityCard.cardName = bookName;
            else if (PickUpModel_RMRVanillaEmotion.IsMissingText(abnormalityCard.cardName)
                     || IsPoorDisplayName(abnormalityCard.cardName))
                abnormalityCard.cardName = bookName ?? string.Empty;
            abnormalityCard.flavorText = $"{RewardingModel.GetChapterText(data.Chapter)}, {RewardingModel.GetRaritytext(data.Rarity)}";
            abnormalityCard.abilityDesc = RewardingModel.GetAblilityText(data);
            if (!string.IsNullOrEmpty(data._bookIcon))
                pickUpXml._artwork = data._bookIcon;
        }

        /// <summary>
        /// Re-inject equip reward names/descs after book localization loads (or language changes).
        /// RegisterPickUpXml runs before LoadTextData/LoadEquipPages, so first-pass injection
        /// often captures Hangul InnerName / empty BookDesc results.
        /// </summary>
        public static void RefreshAllEquipRewardXmlData()
        {
            try
            {
                if (Singleton<RewardPassivesList>.Instance?.infos == null)
                    return;
                int count = 0;
                foreach (RewardPassivesInfo group in Singleton<RewardPassivesList>.Instance.infos)
                {
                    if (group?.RewardPassiveList == null)
                        continue;
                    foreach (RewardPassiveInfo info in group.RewardPassiveList)
                    {
                        if (info == null || info.rewardtype != RewardType.EquipPage)
                            continue;
                        CreateEquipRewardXmlData(info);
                        count++;
                    }
                }
                Debug.Log($"[RMR Localize] Refreshed equip reward display text for {count} entries.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR Localize] RefreshAllEquipRewardXmlData failed: " + ex.Message);
            }
        }

        public static RewardPassiveInfo GetReward(List<RewardPassiveInfo> rewards)
        {
            List<RewardPassiveInfo> rewardPassiveInfoList1 = new List<RewardPassiveInfo>();
            List<RewardPassiveInfo> rewardPassiveInfoList2 = new List<RewardPassiveInfo>();
            List<RewardPassiveInfo> rewardPassiveInfoList3 = new List<RewardPassiveInfo>();
            List<RewardPassiveInfo> rewardPassiveInfoList4 = new List<RewardPassiveInfo>();
            if (rewards == null || rewards.Count == 0)
                return (RewardPassiveInfo)null;
            foreach (RewardPassiveInfo reward in rewards)
            {
                switch (reward.passiverarity)
                {
                    case Rarity.Common:
                        rewardPassiveInfoList1.Add(reward);
                        break;
                    case Rarity.Uncommon:
                        rewardPassiveInfoList2.Add(reward);
                        break;
                    case Rarity.Rare:
                        rewardPassiveInfoList3.Add(reward);
                        break;
                    case Rarity.Unique:
                        rewardPassiveInfoList4.Add(reward);
                        break;
                }
            }
            List<RewardPassiveInfo> rewardPassiveInfoList5 = (List<RewardPassiveInfo>)null;
            List<Rarity> rarityList = new List<Rarity>();
            if (rewardPassiveInfoList1.Count > 0)
            {
                for (int index = 0; index < 50; ++index)
                    rarityList.Add(Rarity.Common);
            }
            if (rewardPassiveInfoList2.Count > 0)
            {
                for (int index = 0; index < 30; ++index)
                    rarityList.Add(Rarity.Uncommon);
            }
            if (rewardPassiveInfoList3.Count > 0)
            {
                for (int index = 0; index < 13; ++index)
                    rarityList.Add(Rarity.Rare);
            }
            if (rewardPassiveInfoList4.Count > 0)
            {
                for (int index = 0; index < 7; ++index)
                    rarityList.Add(Rarity.Unique);
            }
            Rarity rarity = rarityList[UnityEngine.Random.Range(0, rarityList.Count)];
            if (rarity == Rarity.Common)
                rewardPassiveInfoList5 = rewardPassiveInfoList1;
            if (rarity == Rarity.Uncommon)
                rewardPassiveInfoList5 = rewardPassiveInfoList2;
            if (rarity == Rarity.Rare)
                rewardPassiveInfoList5 = rewardPassiveInfoList3;
            if (rarity == Rarity.Unique)
                rewardPassiveInfoList5 = rewardPassiveInfoList4;
            if (rewardPassiveInfoList5 == null || rewardPassiveInfoList5.Count == 0)
                return (RewardPassiveInfo)null;
            RewardPassiveInfo info = rewardPassiveInfoList5[UnityEngine.Random.Range(0, rewardPassiveInfoList5.Count)];
            if (info.rewardtype == RewardType.EquipPage)
            {
                RewardingModel.CreateEquipRewardXmlData(info);
                EmotionCardXmlInfo equipXml = LogLikeMod.GetRegisteredPickUpXml(info);
                if (equipXml != null)
                    equipXml.TargetType = EmotionTargetType.All;
            }
            else if (info.script != string.Empty)
            {
                // Inject under the registered Name key (vanilla Name like SnowWhite_Vine after
                // ApplyVanillaEmotionPresentation). Never stamp "Not found" over good vanilla text.
                EmotionCardXmlInfo registeredPickUpXml = LogLikeMod.GetRegisteredPickUpXml(info);
                // Creature only — EquipPage presentation is handled by CreateEquipRewardXmlData.
                if (registeredPickUpXml != null && info.rewardtype == RewardType.Creature)
                    RMRAbnormalityUnlockManager.EnsureVanillaEmotionPresentation(info, registeredPickUpXml);
                PickUpModelBase pickUp = LogLikeMod.FindPickUp(info.script);
                if (registeredPickUpXml != null)
                    PickUpModel_RMRVanillaEmotion.InjectResolvedDesc(registeredPickUpXml, pickUp);
                else
                {
                    AbnormalityCard abnormalityCard = Singleton<AbnormalityCardDescXmlList>.Instance.GetAbnormalityCard(info.script);
                    if (abnormalityCard != null && pickUp != null
                        && PickUpModel_RMRVanillaEmotion.IsUsablePickUpDisplayText(pickUp.Name, info.script))
                    {
                        abnormalityCard.cardName = pickUp.Name;
                        abnormalityCard.flavorText = pickUp.FlaverText;
                        abnormalityCard.abilityDesc = pickUp.Desc;
                    }
                }
            }
            return info;
        }
        #endregion

        #region --- Book / card data ---


        public static DiceCardXmlInfo GetCard(CardDropValueXmlInfo info)
        {
            if (info == null)
            {
                Debug.Log("info is null");
                return (DiceCardXmlInfo)null;
            }
            CardDropTableXmlInfo data = Singleton<CardDropTableXmlList>.Instance.GetData(new LorId(info.workshopID, info.DropTableId));
            List<DiceCardXmlInfo> diceCardXmlInfoList1 = new List<DiceCardXmlInfo>();
            List<DiceCardXmlInfo> diceCardXmlInfoList2 = new List<DiceCardXmlInfo>();
            List<DiceCardXmlInfo> diceCardXmlInfoList3 = new List<DiceCardXmlInfo>();
            List<DiceCardXmlInfo> diceCardXmlInfoList4 = new List<DiceCardXmlInfo>();
            if (data == null || data.cardIdList.Count == 0)
                return (DiceCardXmlInfo)null;
            foreach (LorId cardId in data.cardIdList)
            {
                DiceCardXmlInfo cardItem = RewardingModel.GetCardItemOriginAware(cardId);
                if (cardItem == null)
                    cardItem.Log($"PickUpCardNull : {cardId.packageId} _ {cardId.id.ToString()}");
                else if (cardItem != null)
                {
                    switch (cardItem.Rarity)
                    {
                        case Rarity.Common:
                            diceCardXmlInfoList1.Add(cardItem);
                            break;
                        case Rarity.Uncommon:
                            diceCardXmlInfoList2.Add(cardItem);
                            break;
                        case Rarity.Rare:
                            diceCardXmlInfoList3.Add(cardItem);
                            break;
                        case Rarity.Unique:
                            diceCardXmlInfoList4.Add(cardItem);
                            break;
                    }
                }
            }
            List<DiceCardXmlInfo> diceCardXmlInfoList5 = (List<DiceCardXmlInfo>)null;
            List<Rarity> rarityList = new List<Rarity>();
            if (diceCardXmlInfoList1.Count > 0)
            {
                for (int index = 0; index < info.CommonValue; ++index)
                    rarityList.Add(Rarity.Common);
            }
            if (diceCardXmlInfoList2.Count > 0)
            {
                for (int index = 0; index < info.UncommonValue; ++index)
                    rarityList.Add(Rarity.Uncommon);
            }
            if (diceCardXmlInfoList3.Count > 0)
            {
                for (int index = 0; index < info.RareValue; ++index)
                    rarityList.Add(Rarity.Rare);
            }
            if (diceCardXmlInfoList4.Count > 0)
            {
                for (int index = 0; index < info.UniqueValue; ++index)
                    rarityList.Add(Rarity.Unique);
            }
            if (rarityList.Count == 0)
                return null;
            Rarity rarity = rarityList[UnityEngine.Random.Range(0, rarityList.Count)];
            if (rarity == Rarity.Common)
                diceCardXmlInfoList5 = diceCardXmlInfoList1;
            if (rarity == Rarity.Uncommon)
                diceCardXmlInfoList5 = diceCardXmlInfoList2;
            if (rarity == Rarity.Rare)
                diceCardXmlInfoList5 = diceCardXmlInfoList3;
            if (rarity == Rarity.Unique)
                diceCardXmlInfoList5 = diceCardXmlInfoList4;
            return diceCardXmlInfoList5 == null || diceCardXmlInfoList5.Count == 0 ? (DiceCardXmlInfo)null : diceCardXmlInfoList5[UnityEngine.Random.Range(0, diceCardXmlInfoList5.Count)];
        }

        /// <summary>
        /// Pick one card from a pre-filtered pool using the same weighted-rarity logic as <see cref="GetCard"/>.
        /// Returns null when the pool is empty or all candidates are excluded.
        /// </summary>
        public static DiceCardXmlInfo GetCardFromFilteredPool(
            List<DiceCardXmlInfo> pool,
            CardDropValueXmlInfo info,
            HashSet<LorId> excludeIds)
        {
            // Separate by rarity, excluding already-picked IDs
            var byRarity = new Dictionary<Rarity, List<DiceCardXmlInfo>>();
            foreach (var card in pool)
            {
                if (excludeIds.Contains(card.id))
                    continue;
                if (!byRarity.ContainsKey(card.Rarity))
                    byRarity[card.Rarity] = new List<DiceCardXmlInfo>();
                byRarity[card.Rarity].Add(card);
            }

            // Build weighted rarity list (same weights as GetCard)
            var rarityList = new List<Rarity>();
            if (byRarity.ContainsKey(Rarity.Common))
                for (int i = 0; i < info.CommonValue; i++) rarityList.Add(Rarity.Common);
            if (byRarity.ContainsKey(Rarity.Uncommon))
                for (int i = 0; i < info.UncommonValue; i++) rarityList.Add(Rarity.Uncommon);
            if (byRarity.ContainsKey(Rarity.Rare))
                for (int i = 0; i < info.RareValue; i++) rarityList.Add(Rarity.Rare);
            if (byRarity.ContainsKey(Rarity.Unique))
                for (int i = 0; i < info.UniqueValue; i++) rarityList.Add(Rarity.Unique);

            if (rarityList.Count == 0)
                return null;

            Rarity rarity = rarityList[UnityEngine.Random.Range(0, rarityList.Count)];
            if (!byRarity.ContainsKey(rarity) || byRarity[rarity].Count == 0)
                return null;

            var rarityPool = byRarity[rarity];
            return rarityPool[UnityEngine.Random.Range(0, rarityPool.Count)];
        }
        #endregion

        #region --- Reward generation ---


        public static List<DiceCardXmlInfo> PickUpCards(CardDropValueXmlInfo info)
        {
            List<DiceCardXmlInfo> result = new List<DiceCardXmlInfo>();
            if (info == null)
            {
                Debug.Log("[PickUpCards] Drop value is null");
                return result;
            }

            // Get the full drop table and build the unowned card pool
            CardDropTableXmlInfo dropTable = Singleton<CardDropTableXmlList>.Instance.GetData(
                new LorId(info.workshopID, info.DropTableId));

            if (dropTable == null || dropTable.cardIdList.Count == 0)
            {
                Debug.Log("[PickUpCards] Drop table is empty");
                return result;
            }

            var allCards = new List<DiceCardXmlInfo>();
            foreach (LorId cardId in dropTable.cardIdList)
            {
                DiceCardXmlInfo cardItem = RewardingModel.GetCardItemOriginAware(cardId);
                if (cardItem != null)
                    allCards.Add(cardItem);
            }

            int totalInPool = allCards.Count;

            // Filter: exclude already-owned cards
            var unowned = allCards.Where(c => !LogueBookModels.HasOwnedCombatPage(c.id)).ToList();
            int ownedCount = totalInPool - unowned.Count;

            // Filter: exclude cards that are already upgraded versions
            // (upgraded cards should only come from rest upgrades, not from reward pools)
            int upgradeCount = unowned.RemoveAll(c =>
                c.id.packageId != null && c.id.packageId.Contains(LogCardUpgradeManager.UpgradeKeyword));
            int filteredUpgrade = upgradeCount;

            Debug.Log($"[PickUpCards] Pool total:{totalInPool} owned:{ownedCount} upgradeFiltered:{filteredUpgrade} available:{unowned.Count}");

            // Build the 3-choice list, deduplicating within the selection
            var pickedIds = new HashSet<LorId>();
            int targetCount = Math.Min(3, unowned.Count);

            for (int i = 0; i < targetCount; i++)
            {
                DiceCardXmlInfo card = GetCardFromFilteredPool(unowned, info, pickedIds);
                if (card != null)
                {
                    pickedIds.Add(card.id);
                    result.Add(card);
                    Debug.Log($"[PickUpCards] #{i}: {card.id.packageId}:{card.id.id} ({card.Name})");
                }
            }

            if (result.Count < 3)
                Debug.Log($"[PickUpCards] Only {result.Count}/3 cards available (all owned or pool exhausted)");

            Singleton<GlobalLogueEffectManager>.Instance.ChangeCardReward(ref result);

            // Second filter: after global effects may have injected upgrades or already-owned cards,
            // remove owned cards, upgrade versions, and duplicates by normalized originalId.
            var seenKeys = new HashSet<string>();
            for (int i = result.Count - 1; i >= 0; i--)
            {
                var card = result[i];
                string key = LogueBookModels.NormalizeCardKey(card.id);

                // Remove if already owned (global effect may have swapped in an owned card)
                if (LogueBookModels.HasOwnedCombatPage(card.id))
                {
                    Debug.Log($"[PickUpCards] Post-filter removed owned: {card.id.packageId}:{card.id.id}");
                    result.RemoveAt(i);
                    continue;
                }

                // Remove upgrade versions (upgrades only via rest, not reward pools)
                if (card.id.packageId != null && card.id.packageId.Contains(LogCardUpgradeManager.UpgradeKeyword))
                {
                    Debug.Log($"[PickUpCards] Post-filter removed upgrade: {card.id.packageId}:{card.id.id}");
                    result.RemoveAt(i);
                    continue;
                }

                // Deduplicate by normalized originalId (within the 3-choice set)
                if (!seenKeys.Add(key))
                {
                    Debug.Log($"[PickUpCards] Post-filter removed duplicate originalId: {card.id.packageId}:{card.id.id}");
                    result.RemoveAt(i);
                }
            }

            Debug.Log($"[PickUpCards] After post-filter: {result.Count} cards remain");
            return result;
        }

        public static bool RewardInStage()
        {
            Singleton<GlobalLogueEffectManager>.Instance.RewardInStageInterrupt();
            if (SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.IsEnabled)
                return true;
            if (LogLikeMod.rewards_InStage.Count == 0)
                return false;
            if (BattleObjectManager.instance.GetAliveListWithAvailable(Faction.Player).Count == 0)
            {
                SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.SetRootCanvas(false);
                return false;
            }
            SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.SetRootCanvas(true);
            RewardingModel.StartPickReward_InStage();
            return true;
        }

        public static void StartPickReward_InStage()
        {
            while (LogLikeMod.rewards_InStage.Count > 0)
            {
                List<EmotionCardXmlInfo> passiveRewardsInlist = LogueBookModels.GetPassiveRewards_Inlist(LogLikeMod.rewards_InStage[0].rewards);
                if (passiveRewardsInlist == null || passiveRewardsInlist.Count == 0)
                {
                    Debug.Log("[StartPickReward_InStage] Empty reward, skipping to next");
                    LogLikeMod.rewards_InStage.RemoveAt(0);
                    continue;
                }
                RewardingModel.rewardFlag = RewardingModel.RewardFlag.RewardInStage;
                SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.Init(1, passiveRewardsInlist);
                return;
            }
        }

        public static void StartPickReward()
        {
            var list = new List<RewardInfo>(LogLikeMod.rewards_passive); // NULL SAFETY CHECKING
            foreach (var passive in list)
            {
                if (passive.rewards == null)
                    LogLikeMod.rewards_passive.Remove(passive);
            }
            EnsureBossBattleCardReward();
            NormalizeDropBookRewards();
            if (HasQueuedEgoSelections())
            {
                List<EmotionEgoXmlInfo> egoRewards = GetQueuedEgoRewards();
                if (egoRewards == null || egoRewards.Count == 0)
                {
                    LogLikeMod.egoSelectionQueue.RemoveAt(0);
                    StartPickReward();
                    return;
                }
                RewardingModel.rewardFlag = RewardingModel.RewardFlag.EgoCardReward;
                SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.InitEgo(1, egoRewards);
            }
            else if (LogLikeMod.rewardsMystery.Count > 0)
            {
                LorId mysteryid = LogLikeMod.rewardsMystery[0];
                LogLikeMod.rewardsMystery.RemoveAt(0);
                Singleton<MysteryManager>.Instance.StartMystery(mysteryid);
                SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.SetRootCanvas(false);
            }
            else
            {
                int rewardCount = LogLikeMod.rewards != null ? LogLikeMod.rewards.FindAll(x => x != null).Count : 0;
                int passiveCount = LogLikeMod.rewards_passive != null ? LogLikeMod.rewards_passive.FindAll(x => x != null).Count : 0;
                int nextCount = LogLikeMod.nextlist != null ? LogLikeMod.nextlist.FindAll(x => x != null).Count : 0;
                if (rewardCount == 0 && passiveCount == 0 && nextCount == 0 && !HasQueuedEgoSelections() && !HasQueuedMysteryRewards())
                {
                    // Final-chapter boss (杂质) or any terminal clear: no next-stage UI — finish battle.
                    TryEndRunAfterAllRewards();
                    return;
                }
                if (LogLikeMod.rewards.Count > 0)
                {
                    Singleton<MysteryManager>.Instance.StartMystery(new LorId(LogLikeMod.ModId, -4));
                    // start Combat Page card reward event (MysteryModel_CardReward)
                    SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.SetRootCanvas(false);
                }
                else if (LogLikeMod.rewards_passive.Count > 0)
                {
                    List<EmotionCardXmlInfo> passiveRewardsInlist = LogueBookModels.GetPassiveRewards_Inlist(LogLikeMod.rewards_passive.FindAll(x => x !=null)[0].rewards);
                    if (passiveRewardsInlist == null || passiveRewardsInlist.Count == 0)
                    {
                        Debug.Log("[StartPickReward] Empty passive reward, skipping to next");
                        LogLikeMod.rewards_passive.RemoveAt(0);
                        StartPickReward();
                        return;
                    }
                    RewardingModel.rewardFlag = RewardingModel.RewardFlag.PassiveReward;
                    SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.Init(1, passiveRewardsInlist);
                }
                else
                {
                    if (LogLikeMod.nextlist == null || LogLikeMod.nextlist.FindAll(x => x != null).Count <= 0)
                    {
                        TryEndRunAfterAllRewards();
                        return;
                    }
                    List<EmotionCardXmlInfo> nextlist = LogLikeMod.nextlist;
                    RewardingModel.rewardFlag = RewardingModel.RewardFlag.NextStageChoose;
                    SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.Init(1, nextlist);
                }
            }
        }

        /// <summary>
        /// When every reward queue is empty and there is no next-stage pick (esp. Grade7 boss),
        /// close reward UI and EndBattle so the run can finish instead of hanging or reopening picks.
        /// </summary>
        public static void TryEndRunAfterAllRewards()
        {
            try
            {
                if (LogLikeMod.EndBattle)
                    return;
                StageController sc = Singleton<StageController>.Instance;
                if (sc == null)
                    return;
                // Still have a queued wave → another fight in this reception, not run end.
                try
                {
                    StageModel stageModel = sc.GetStageModel();
                    if (stageModel != null && stageModel.GetFrontAvailableWave() != null)
                        return;
                }
                catch { /* ignore */ }

                if (SingletonBehavior<BattleManagerUI>.Instance?.ui_levelup != null)
                    SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.SetRootCanvas(false);
                sc.EndBattle();
                Singleton<GlobalLogueEffectManager>.Instance.OnEndBattle();
                LogLikeMod.EndBattle = true;
                Debug.Log($"[RMR] TryEndRunAfterAllRewards EndBattle grade={LogLikeMod.curchaptergrade} type={LogLikeMod.curstagetype}.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR] TryEndRunAfterAllRewards failed: " + ex.Message);
            }
        }

        public static void CompleteInterruptReward()
        {
            MysteryBase mystery = Singleton<MysteryManager>.Instance.curMystery;
            if (mystery != null && mystery.GetType().Name.IndexOf("Reward", StringComparison.OrdinalIgnoreCase) >= 0)
                Singleton<MysteryManager>.Instance.EndMystery(mystery);
            StartPickReward();
        }

        private static void NormalizeDropBookRewards()
        {
            if (LogLikeMod.rewards == null || LogLikeMod.rewards.Count == 0)
                return;
            foreach (var group in LogLikeMod.rewards.FindAll(reward => reward != null && reward.id != null)
                         .GroupBy(reward => reward.id).ToList())
            {
                if (NormalizedDropBookRewardIds.Contains(group.Key))
                    continue;
                NormalizedDropBookRewardIds.Add(group.Key);
                int count = group.Count();
                if (count <= 1)
                    continue;
                int keepCount = Math.Max(1, (int)Math.Ceiling(count * GetBattleCardRewardRetentionRate()));
                keepCount = Math.Min(keepCount, GetDropBookRewardSelectionCap());
                int removeCount = count - keepCount;
                for (int i = LogLikeMod.rewards.Count - 1; i >= 0 && removeCount > 0; i--)
                {
                    DropBookXmlInfo reward = LogLikeMod.rewards[i];
                    if (reward == null || reward.id != group.Key)
                        continue;
                    LogLikeMod.rewards.RemoveAt(i);
                    removeCount--;
                }
            }
        }

        private static double GetBattleCardRewardRetentionRate()
        {
            return LogLikeMod.curstagetype == StageType.Normal
                ? NormalBattleCardRewardRetentionRate
                : BattleCardRewardRetentionRate;
        }

        private static int GetDropBookRewardSelectionCap()
        {
            if (LogLikeMod.curstagetype != StageType.Normal)
                return int.MaxValue;
            if (LogLikeMod.curchaptergrade >= ChapterGrade.Grade6)
                return 2;
            if (LogLikeMod.curchaptergrade >= ChapterGrade.Grade4)
                return 3;
            return int.MaxValue;
        }

        /// <summary>
        /// Previously injected a single Art-grade combat-page drop book when boss rewards were empty.
        /// Disabled: player request — no free art combat page on boss (pre/post reward path).
        /// </summary>
        private static void EnsureBossBattleCardReward()
        {
            if (LogLikeMod.curstagetype != StageType.Boss)
                return;
            if (BossFallbackRewardCheckedThisBattle)
                return;
            BossFallbackRewardCheckedThisBattle = true;
            // Do not add fallback drop books for boss stages.
        }
        #endregion

        #region --- Other helpers ---


        public static bool HasQueuedEgoSelections()
        {
            return LogLikeMod.egoSelectionQueue != null
                && LogLikeMod.egoSelectionQueue.Any(choice => choice != null && choice.Count > 0);
        }

        // --- Mid-battle E.G.O. selection (vanilla: after abno at team emotion 3 / 4 / 5) ---
        private static int _pendingMidBattleEgoEmotionLevel;
        private static readonly HashSet<int> _midBattleEgoDoneAtLevel = new HashSet<int>();
        private static readonly HashSet<int> _midBattleSelectedEgoCardIds = new HashSet<int>();
        /// <summary>True while LevelUpUI is showing a mid-battle EGO 3-pick (not post-battle queue).</summary>
        public static bool IsMidBattleEgoSelectionActive { get; private set; }
        /// <summary>
        /// Blocks spurious EndBattle → EndBattlePhase while emotion/EGO UI just finished and combat
        /// is still live (both factions alive). Cleared on next RoundStart / battle reset.
        /// </summary>
        public static bool SuppressSpuriousEndBattleWhileCombatLive { get; private set; }

        public static void ResetMidBattleEgoSelectionState()
        {
            _pendingMidBattleEgoEmotionLevel = 0;
            _midBattleEgoDoneAtLevel.Clear();
            _midBattleSelectedEgoCardIds.Clear();
            IsMidBattleEgoSelectionActive = false;
            SuppressSpuriousEndBattleWhileCombatLive = false;
            NonCombatNodeExitPending = false;
        }
        #endregion

        #region --- Reward generation ---


        public static void NoteMidBattleEgoPicked(LorId id)
        {
            if (id != null && id != LorId.None)
                _midBattleSelectedEgoCardIds.Add(id.id);
            bool wasMidBattle = IsMidBattleEgoSelectionActive;
            IsMidBattleEgoSelectionActive = false;
            // Keep combat alive after emotion-5 EGO: do not let reward EndBattle hijack RoundEnd.
            if (wasMidBattle)
                SuppressSpuriousEndBattleWhileCombatLive = true;
        }
        #endregion

        #region --- Other helpers ---


        public static void ClearSuppressSpuriousEndBattle()
        {
            SuppressSpuriousEndBattleWhileCombatLive = false;
        }

        /// <summary>
        /// Sticky flag set when leaving shop / finishing mystery-rest (Defeat + EndBattle).
        /// Survives CloseShop / EndMystery and next-stage pick (SetNextStage overwrites
        /// curstagetype), so EndBattlePhase "both sides alive" recovery cannot resume
        /// combat against residual immune merchant/mystery NPCs. Cleared on next RoundStart.
        /// </summary>
        public static bool NonCombatNodeExitPending { get; private set; }

        /// <summary>
        /// Mark intentional non-combat node exit and strip residual enemy NPCs so
        /// IsLiveCombatBothSidesAlive is false for the remainder of EndBattlePhase.
        /// </summary>
        public static void MarkNonCombatNodeExit(string reason = null)
        {
            NonCombatNodeExitPending = true;
            try
            {
                if (BattleObjectManager.instance != null)
                {
                    List<BattleUnitModel> enemies = BattleObjectManager.instance.GetList(Faction.Enemy);
                    if (enemies != null)
                    {
                        // Copy: Die may mutate the live unit list.
                        foreach (BattleUnitModel unit in new List<BattleUnitModel>(enemies))
                        {
                            if (unit == null || unit.IsDead())
                                continue;
                            try { unit.Die(); }
                            catch { /* ignore immortal / already-dead */ }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR] MarkNonCombatNodeExit clear enemies failed: " + ex.Message);
            }
            if (!string.IsNullOrEmpty(reason))
                Debug.Log("[RMR] NonCombatNodeExit pending: " + reason);
        }

        public static void ClearNonCombatNodeExit()
        {
            NonCombatNodeExitPending = false;
        }

        /// <summary>True if a mid-battle EGO offer is pending (armed, not yet opened).</summary>
        public static bool HasPendingMidBattleEgo()
        {
            return _pendingMidBattleEgoEmotionLevel >= 3;
        }

        /// <summary>
        /// Live combat: both factions still have available units. Used to refuse EndBattle
        /// (reward phase) so mid-battle EGO pick cannot wipe enemies / end the reception.
        /// </summary>
        public static bool IsLiveCombatBothSidesAlive()
        {
            try
            {
                if (BattleObjectManager.instance == null)
                    return false;
                int players = BattleObjectManager.instance.GetAliveListWithAvailable(Faction.Player).Count;
                int enemies = BattleObjectManager.instance.GetAliveListWithAvailable(Faction.Enemy).Count;
                return players > 0 && enemies > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Shop / mystery / rest are non-combat nodes that may call EndBattle while "units" exist.
        /// Combat stages must not enter reward EndBattle while both sides are still alive.
        /// Also true while NonCombatNodeExitPending — after next-stage pick curstagetype is no
        /// longer Shop/Mystery but residual NPCs may still be on the field until the wave swaps.
        /// </summary>
        public static bool IsNonCombatNodeStage()
        {
            if (NonCombatNodeExitPending)
                return true;
            try
            {
                if (Singleton<ShopManager>.Instance?.curshop != null)
                    return true;
                if (Singleton<MysteryManager>.Instance?.curMystery != null)
                    return true;
            }
            catch { /* ignore */ }
            return LogLikeMod.curstagetype == StageType.Shop
                || LogLikeMod.curstagetype == StageType.Mystery
                || LogLikeMod.curstagetype == StageType.Rest
                || LogLikeMod.curstagetype == StageType.Reward;
        }

        /// <summary>
        /// After an abnormality pick at emotion 3/4/5, arm a mid-battle EGO offer (opened next
        /// EmotionChoice once LevelUpUI closes).
        /// </summary>
        public static void ArmMidBattleEgoAfterEmotionIfNeeded()
        {
            int lv = LogLikeMod.curemotion;
            if (lv < 3 || lv > 5)
                return;
            if (_midBattleEgoDoneAtLevel.Contains(lv))
                return;
            if (!RogueLike_Mod_Reborn.RMRAbnormalityUnlockManager.HasCompletedAnyRealization()
                && (LogLikeMod.egoSelectionQueue == null || LogLikeMod.egoSelectionQueue.Count == 0))
            {
                // Still arm if route has any unlocked EGO (shop/reward) so mid-battle pick can run.
                bool anyRouteEgo = false;
                try
                {
                    anyRouteEgo = RogueLike_Mod_Reborn.RMRAbnormalityUnlockManager
                        .EnumerateRouteUnlockedEgoPages()?.Any() == true;
                }
                catch { anyRouteEgo = false; }
                if (!anyRouteEgo)
                    return;
            }
            _pendingMidBattleEgoEmotionLevel = lv;
            Debug.Log($"[RMR] Armed mid-battle EGO selection after emotion abno pick level={lv}.");
        }

        /// <summary>
        /// Open mid-battle EGO 3-pick if armed and LevelUpUI is free. Returns true if UI was opened
        /// (caller should keep RoundEnd emotion phase waiting).
        /// </summary>
        public static bool TryOpenPendingMidBattleEgoSelection()
        {
            if (_pendingMidBattleEgoEmotionLevel < 3)
                return false;
            try
            {
                if (SingletonBehavior<BattleManagerUI>.Instance?.ui_levelup != null
                    && SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.IsEnabled)
                    return false; // wait for abno UI to finish closing
            }
            catch { /* continue */ }

            int lv = _pendingMidBattleEgoEmotionLevel;
            _pendingMidBattleEgoEmotionLevel = 0;
            if (_midBattleEgoDoneAtLevel.Contains(lv))
                return false;

            List<LorId> choiceIds = RogueLike_Mod_Reborn.RMRAbnormalityUnlockManager
                .RollMidBattleEgoChoiceSet(_midBattleSelectedEgoCardIds);
            if (choiceIds == null || choiceIds.Count == 0)
            {
                _midBattleEgoDoneAtLevel.Add(lv);
                Debug.Log($"[RMR] Mid-battle EGO at emotion {lv}: empty pool — skip.");
                return false;
            }

            List<EmotionEgoXmlInfo> offers = new List<EmotionEgoXmlInfo>();
            foreach (LorId id in choiceIds)
            {
                DiceCardXmlInfo card = GetCardItemOriginAware(id)
                    ?? ItemXmlDataList.instance?.GetCardItem(id, true)
                    ?? ItemXmlDataList.instance?.GetCardItem(id.id, true);
                if (card == null)
                    continue;
                EmotionEgoXmlInfo ego = LogLikeMod.AddEmotionEgoForReward(card);
                if (ego != null)
                    offers.Add(ego);
            }
            if (offers.Count == 0)
            {
                _midBattleEgoDoneAtLevel.Add(lv);
                Debug.LogWarning($"[RMR] Mid-battle EGO at emotion {lv}: offers failed to build.");
                return false;
            }

            if (LogLikeMod.egoSelectionQueue == null)
                LogLikeMod.egoSelectionQueue = new List<List<LorId>>();
            // Front of queue so OnPickEgoCard / skip dequeue the mid-battle offer first.
            LogLikeMod.egoSelectionQueue.Insert(0, choiceIds);

            _midBattleEgoDoneAtLevel.Add(lv);
            IsMidBattleEgoSelectionActive = true;
            rewardFlag = RewardFlag.EgoCardReward;
            try
            {
                SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.SetRootCanvas(true);
                SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.InitEgo(1, offers);
            }
            catch (Exception ex)
            {
                IsMidBattleEgoSelectionActive = false;
                if (LogLikeMod.egoSelectionQueue.Count > 0)
                    LogLikeMod.egoSelectionQueue.RemoveAt(0);
                Debug.LogWarning("[RMR] Mid-battle InitEgo failed: " + ex.Message);
                return false;
            }

            int enemyAlive = 0;
            int playerAlive = 0;
            try
            {
                enemyAlive = BattleObjectManager.instance?.GetAliveListWithAvailable(Faction.Enemy)?.Count ?? 0;
                playerAlive = BattleObjectManager.instance?.GetAliveListWithAvailable(Faction.Player)?.Count ?? 0;
            }
            catch { /* ignore */ }
            Debug.Log($"[RMR] Mid-battle EGO selection opened at emotion {lv}, offers={offers.Count} ids=[{string.Join(",", choiceIds.Select(x => x.id.ToString()).ToArray())}] enemies={enemyAlive} players={playerAlive}");
            return true;
        }
        #endregion

        #region --- Reward generation ---


        public static bool HasQueuedMysteryRewards()
        {
            return LogLikeMod.rewardsMystery != null && LogLikeMod.rewardsMystery.Count > 0;
        }

        private static List<EmotionEgoXmlInfo> GetQueuedEgoRewards()
        {
            if (!HasQueuedEgoSelections())
                return new List<EmotionEgoXmlInfo>();
            List<LorId> ids = LogLikeMod.egoSelectionQueue[0];
            List<EmotionEgoXmlInfo> result = new List<EmotionEgoXmlInfo>();
            foreach (LorId id in ids)
            {
                // Atlas unlock = pool eligibility only. Skip only if this run already owns the EGO.
                if (RMRAbnormalityUnlockManager.IsEgoOwnedOnCurrentRoute(id))
                    continue;
                DiceCardXmlInfo card = ItemXmlDataList.instance.GetCardItem(id, true)
                    ?? ItemXmlDataList.instance.GetCardItem(id.id, true)
                    ?? ItemXmlDataList.instance.GetCardItem(new LorId(string.Empty, id.id), true);
                if (card == null)
                {
                    Debug.LogWarning($"[GetQueuedEgoRewards] card missing for EGO id={id}");
                    continue;
                }
                EmotionEgoXmlInfo ego = LogLikeMod.AddEmotionEgoForReward(card);
                if (ego != null)
                    result.Add(ego);
            }
            return result;
        }

        public static void PickEmotion(List<EmotionCardXmlInfo> emotions)
        {
            RewardingModel.rewardFlag = RewardingModel.RewardFlag.EmtoionChoose;
            // Vanilla: at team emotion 3/4/5, after abno pick comes floor E.G.O. selection.
            ArmMidBattleEgoAfterEmotionIfNeeded();
            // Font resolve only -- no scene sweep. This runs BEFORE ui_levelup.Init, so a sweep here
            // would scan the previous screen's TMP and still miss the slots about to be built. The
            // LevelUpUI.InitBase postfix repairs that panel after Init, scoped to the panel itself.
            try { LogLikeMod.EnsureLocalizedFonts("PickEmotion", repairActiveUi: false); } catch { }
            SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.Init(emotions.Count, emotions);
        }

        public static bool RewardClearStage(StageController __instance)
        {
            if (LogLikeMod.purpleexcept)
                return true;
            Singleton<GlobalLogueEffectManager>.Instance.RewardClearStageInterrupt();
            if (Singleton<MysteryManager>.Instance.curMystery != null || SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.IsEnabled)
                return false;
            // Mid-battle abno/EGO must never open post-battle rewards or allow EndBattle while fight continues.
            if (IsMidBattleEgoSelectionActive || HasPendingMidBattleEgo())
                return false;
            if (SuppressSpuriousEndBattleWhileCombatLive && IsLiveCombatBothSidesAlive() && !IsNonCombatNodeStage())
            {
                if (!LogLikeMod.EndBattle)
                    Debug.Log("[RMR RewardClearStage] refuse EndBattle — mid-battle EGO just resolved and both sides still alive.");
                return false;
            }
            if (IsLiveCombatBothSidesAlive() && !IsNonCombatNodeStage())
            {
                // Spurious EndBattlePhase while combat is still live (seen after emotion-5 EGO pick).
                if (!LogLikeMod.EndBattle)
                    Debug.Log("[RMR RewardClearStage] refuse EndBattle — both factions still have living units (live combat).");
                return false;
            }
            EnsureBossBattleCardReward();
            // Total party wipe = defeat. Do NOT open reward/next-stage UI just because nextlist was
            // pre-filled at StartBattle — that would let a lost fight continue the run.
            if (BattleObjectManager.instance.GetAliveListWithAvailable(Faction.Player).Count == 0)
            {
                SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.SetRootCanvas(false);
                Debug.Log("[RMR RewardClearStage] no living players → defeat / end reception.");
                return true;
            }
            EnsureNextListIfNeeded();
            int rewardCount = LogLikeMod.rewards != null ? LogLikeMod.rewards.FindAll(x => x != null).Count : 0;
            int passiveCount = LogLikeMod.rewards_passive != null ? LogLikeMod.rewards_passive.FindAll(x => x != null).Count : 0;
            int nextCount = LogLikeMod.nextlist != null ? LogLikeMod.nextlist.FindAll(x => x != null).Count : 0;
            if (rewardCount == 0 && passiveCount == 0 && nextCount == 0 && !HasQueuedEgoSelections() && !HasQueuedMysteryRewards())
            {
                // EndBattlePhase calls this every frame — log once so Player.log is not flooded.
                if (!LogLikeMod.EndBattle)
                {
                    Debug.Log($"[RMR RewardClearStage] queues empty → allow EndBattle (grade={LogLikeMod.curchaptergrade}, type={LogLikeMod.curstagetype}, step={LogLikeMod.curChStageStep}).");
                }
                return true;
            }
            if (Singleton<MysteryManager>.Instance.curMystery == null)
                SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.SetRootCanvas(true);
            if (RogueLike_Mod_Reborn.RMRCore.provideAdditionalLogging)
            {
                Debug.Log("REWARDS");
                if (LogLikeMod.rewards != null)
                {
                    foreach (var reward in LogLikeMod.rewards)
                    {
                        if (reward == null)
                            Debug.Log("NULL REWARD!!");
                        else
                            Debug.Log(reward.id.packageId + " --- " + reward.id.id.ToString());
                    }
                }
                Debug.Log("REWARDS PASSIVE");
                if (LogLikeMod.rewards_passive != null)
                {
                    for (int i = 0; i < LogLikeMod.rewards_passive.Count; i++)
                    {
                        if (LogLikeMod.rewards_passive[i] == null || LogLikeMod.rewards_passive[i].rewards == null)
                            Debug.Log("NULL REWARD LIST!!");
                        else
                        {
                            Debug.Log($"REWARDS PASSIVE LIST {i}");
                            foreach (var reward in LogLikeMod.rewards_passive[i].rewards)
                            {
                                if (reward == null)
                                    Debug.Log("NULL REWARD LIST!!");
                                else
                                    Debug.Log(reward.id.packageId + " --- " + reward.id.id.ToString());
                            }
                        }
                    }
                }
                Debug.Log("NEXTLIST");
                if (LogLikeMod.nextlist != null)
                {
                    foreach (var reward in LogLikeMod.nextlist)
                    {
                        if (reward == null)
                            Debug.Log("NULL REWARD!!");
                        else
                            Debug.Log(reward.Name + " --- " + reward.id.ToString());
                    }
                }
            }
            RewardingModel.StartPickReward();
            return false;
        }
        #endregion

        #region --- Other helpers ---


        /// <summary>
        /// If next-stage options were cleared (shop leave / bad save) but the chapter still has
        /// remaining nodes AND no next wave is already queued, rebuild nextlist so the run does not
        /// FinalEnd with no choices.
        /// Do NOT rebuild after the player already picked a stage (SetNextStage adds waves first).
        /// Do NOT rebuild after Impurity (Grade7) boss — that is the end of the run even if
        /// RemainStageList still has leftover nodes.
        /// </summary>
        public static void EnsureNextListIfNeeded()
        {
            try
            {
                if (LogLikeMod.nextlist == null)
                    LogLikeMod.nextlist = new List<EmotionCardXmlInfo>();
                if (LogLikeMod.nextlist.FindAll(x => x != null).Count > 0)
                    return;

                // Impurity boss is the final fight. ResetNextStage already cleared nextlist;
                // rebuilding from remaining Grade7 nodes would incorrectly continue the run
                // (seen after Distorted Ensemble / 扭曲乐团 with "剩余接待: 8").
                if (LogLikeMod.curstagetype == StageType.Boss
                    && LogLikeMod.curchaptergrade >= ChapterGrade.Grade7)
                {
                    LogLikeMod.nextlist.Clear();
                    Debug.Log("[RMR] EnsureNextListIfNeeded: Grade7 boss is final — nextlist stays empty (run should end after rewards).");
                    return;
                }

                // Player already chose next stage → waves are queued; empty nextlist is intentional.
                try
                {
                    StageModel stageModel = Singleton<StageController>.Instance?.GetStageModel();
                    if (stageModel != null && stageModel.GetFrontAvailableWave() != null)
                        return;
                }
                catch { /* ignore */ }

                if (LogueBookModels.RemainStageList == null)
                    return;
                ChapterGrade grade = LogLikeMod.curchaptergrade;
                // After Urban Star (Grade6) boss, options come from Grade7 (杂质).
                if (LogLikeMod.curstagetype == StageType.Boss
                    && LogLikeMod.curchaptergrade < ChapterGrade.Grade7)
                {
                    grade = LogLikeMod.curchaptergrade + 1;
                }
                if (!LogueBookModels.EnsureChapterRemainStages(grade))
                    return;
                bool stepOne = LogLikeMod.curstagetype == StageType.Start
                    || LogLikeMod.curstagetype == StageType.Boss;
                LogLikeMod.nextlist = LogueBookModels.GetNextList(grade, stepOne);
                Debug.Log($"[RMR] EnsureNextListIfNeeded rebuilt nextlist count={LogLikeMod.nextlist?.Count ?? 0} grade={grade} type={LogLikeMod.curstagetype}.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR] EnsureNextListIfNeeded failed: " + ex.Message);
            }
        }
        #endregion

        #region --- Book / card data ---


        public static List<EmotionCardXmlInfo> GetCurEmotion()
        {
            List<EmotionCardXmlInfo> infos = new List<EmotionCardXmlInfo>();
            int num1 = LogLikeMod.curemotion + 1;
            int num2 = 0;
            int num3 = 0;
            foreach (UnitBattleDataModel unitBattleDataModel in LogueBookModels.playerBattleModel)
            {
                if (!unitBattleDataModel.isDead)
                {
                    ++num2;
                    num3 += unitBattleDataModel.emotionDetail.EmotionLevel;
                }
            }
            if (num2 == 0 || num3 / num2 < num1)
                return null;
            ++LogLikeMod.curemotion;
            int teamEmotionLevel = LogLikeMod.curemotion; // 1..5 after increment
            int needTier = RMRAbnormalityUnlockManager.GetRequiredAbnoTierForTeamEmotion(teamEmotionLevel);

            if (LogueBookModels.EmotionCardList == null || LogueBookModels.EmotionCardList.Count == 0)
            {
                Debug.Log($"[RMR] GetCurEmotion teamLv={teamEmotionLevel} tier={needTier}: empty owned pool → fallback.");
                EmotionCardXmlInfo fallback = RMRAbnormalityUnlockManager.GetNoAbnormalityFallback(teamEmotionLevel);
                if (fallback != null)
                    infos.Add(fallback);
                return infos;
            }

            // Already-selected this reception (cannot pick the same page twice).
            HashSet<string> selectedEmotionIds = new HashSet<string>();
            if (LogueBookModels.selectedEmotion != null)
            {
                foreach (RewardPassiveInfo selected in LogueBookModels.selectedEmotion)
                {
                    if (selected != null)
                        selectedEmotionIds.Add(RewardingModel.GetRewardPassiveKey(selected));
                }
            }

            // Vanilla: tier I at emo 1–2, tier II at 3–4, tier III at 5.
            // Pool = route-owned pages only (set at battle start), filtered by vanilla EmotionLevel.
            List<RewardPassiveInfo> eligible = RMRAbnormalityUnlockManager.FilterOwnedPagesForTeamEmotion(
                LogueBookModels.EmotionCardList,
                teamEmotionLevel,
                selectedEmotionIds);

            HashSet<string> emotionCardKeys = new HashSet<string>();
            foreach (RewardPassiveInfo info in eligible)
            {
                EmotionCardXmlInfo card = LogLikeMod.GetRegisteredPickUpXml(info);
                if (card == null)
                    continue;
                string key = RewardingModel.GetEmotionCardKey(card);
                if (emotionCardKeys.Add(key))
                    infos.Add(card);
            }

            // Random 3-pick from eligible owned pages.
            while (infos.Count > 3)
            {
                int index = UnityEngine.Random.Range(0, infos.Count);
                infos.RemoveAt(index);
            }

            foreach (EmotionCardXmlInfo emotionCardXmlInfo in infos)
            {
                // Lazy re-apply Name/artwork if RegisterPickUpXml ran before EmotionCardXmlList was ready
                // (or before mod→vanilla script aliases could resolve). UI SetTexts keys by Name.
                RewardPassiveInfo offerInfo = RewardingModel.FindRewardInfo(emotionCardXmlInfo);
                if (offerInfo != null)
                    RMRAbnormalityUnlockManager.EnsureVanillaEmotionPresentation(offerInfo, emotionCardXmlInfo);

                PickUpModelBase pickUp = (emotionCardXmlInfo.Script != null && emotionCardXmlInfo.Script.Count > 0)
                    ? LogLikeMod.FindPickUp(emotionCardXmlInfo.Script[0])
                    : null;
                PickUpModel_RMRVanillaEmotion.InjectResolvedDesc(emotionCardXmlInfo, pickUp);
            }

            if (infos.Count == 0)
            {
                Debug.Log($"[RMR] GetCurEmotion teamLv={teamEmotionLevel} tier={needTier}: no eligible owned page → fallback.");
                EmotionCardXmlInfo fallback = RMRAbnormalityUnlockManager.GetNoAbnormalityFallback(teamEmotionLevel);
                if (fallback != null)
                    infos.Add(fallback);
            }
            else
            {
                Debug.Log($"[RMR] GetCurEmotion teamLv={teamEmotionLevel} tier={needTier}: offering {infos.Count} owned abno page(s).");
            }
            return infos;
        }
        #endregion

        #region --- Other helpers ---


        public static bool EmotionChoice()
        {
            if (SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.IsEnabled)
                return false;

            // After abno UI closes at emotion 3/4/5, open E.G.O. 3-pick before resuming combat.
            if (TryOpenPendingMidBattleEgoSelection())
                return false;

            List<EmotionCardXmlInfo> curEmotion = RewardingModel.GetCurEmotion();
            if (curEmotion == null || curEmotion.Count == 0)
                return true;
            if (BattleObjectManager.instance.GetAliveListWithAvailable(Faction.Player).Count == 0)
            {
                SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.SetRootCanvas(false);
                return true;
            }
            SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.SetRootCanvas(true);
            RewardingModel.PickEmotion(curEmotion);
            return false;
        }

        /// <summary>enum RewardFlag</summary>

        public enum RewardFlag
        {
            CardReward,
            PassiveReward,
            NextStageChoose,
            EmtoionChoose,
            RewardInStage,
            EgoCardReward,
        }
        #endregion

        #region --- Reward generation ---


        private static string GetRewardPassiveKey(RewardPassiveInfo info)
        {
            if (info == null || info.id == null)
                return string.Empty;
            // Null-safe: matches FilterOwnedPagesForTeamEmotion key format.
            return (info.id.packageId ?? "") + ":" + info.id.id.ToString();
        }
        #endregion

        #region --- Book / card data ---


        private static string GetEmotionCardKey(EmotionCardXmlInfo info)
        {
            if (info == null)
                return string.Empty;
            string script = string.Empty;
            if (info.Script != null && info.Script.Count > 0)
                script = info.Script[0];
            string packageId = LogLikeMod.GetPickUpXmlWorkShopId_Passive(info) ?? string.Empty;
            return packageId + ":" + info.id.ToString() + ":" + script;
        }
        #endregion

    }
}
