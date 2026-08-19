// -----------------------------------------------------------------------------
// Post-battle or shop pickup model: PickUpModel_RMRVanillaEmotion
// Namespace/file: ruina-roguelike-reborn-main\abcdcode_LOGLIKE_MOD\PickUpModel_RMRVanillaEmotion.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LOR_XML;
using RogueLike_Mod_Reborn;
using UnityEngine;

namespace abcdcode_LOGLIKE_MOD
{
    /// <summary>Pickup model: PickUpModel_RMRVanillaEmotion</summary>
    [HideFromItemCatalog]
    public class PickUpModel_RMRVanillaEmotion : PickUpModelBase
    {
        private readonly string _script;

        public PickUpModel_RMRVanillaEmotion(string script)
        {
            _script = script ?? string.Empty;
            LoadText();
        }
        #region --- Getters / setters / checks ---


        public static bool HasVanillaAbility(string script)
        {
            return FindAbilityType(script) != null;
        }
        #endregion

        #region --- UI show / hide / build ---


        public static PickUpModel_RMRVanillaEmotion TryCreate(string script)
        {
            return HasVanillaAbility(script) ? new PickUpModel_RMRVanillaEmotion(script) : null;
        }
        #endregion

        #region --- Getters / setters / checks ---


        public override bool IsCanPickUp(UnitDataModel target)
        {
            return target != null && !CheckDead(target);
        }
        #endregion

        #region --- Battle hooks ---


        public override void OnPickUp()
        {
            // Intentionally empty. Per-librarian apply lives in OnPickUp(BattleUnitModel).
            // StageLibraryFloorModel_OnPickPassiveCard always calls parameterless OnPickUp()
            // AFTER the SelectOne/All targeted applies. The previous "apply to every ally"
            // implementation stacked the same abnormality page twice on the chosen librarian
            // (two 巨目/小喙 icons; Small Beak injected two 0-cost combat pages into hand).
        }

        public override void OnPickUp(BattleUnitModel model)
        {
            ApplyTo(model);
        }
        #endregion

        #region --- Other helpers ---


        private void ApplyTo(BattleUnitModel model)
        {
            if (model == null || rewardinfo == null)
                return;

            EmotionCardAbilityBase ability = CreateAbility(_script);
            EmotionCardXmlInfo card = LogLikeMod.GetRegisteredPickUpXml(rewardinfo);
            if (ability == null || card == null)
            {
                Debug.LogError($"[RMRVanillaEmotion] Failed to apply vanilla emotion card: {_script}");
                return;
            }
            CreaturePickUpModel.ApplyEmotionCard(card, ability, model);
        }
        #endregion

        #region --- Save / load ---


        private void LoadText()
        {
            try
            {
                AbnormalityCard desc = ResolveAbnormalityDesc(_script);
                if (desc != null && !IsMissingDesc(desc))
                {
                    Name = string.IsNullOrEmpty(desc.cardName) ? _script : desc.cardName;
                    Desc = desc.abilityDesc ?? string.Empty;
                    FlaverText = desc.flavorText ?? string.Empty;
                    // Fill any still-empty field from mod CreaturePickUp_Table.
                    TryFillFromModTextTable();
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RMRVanillaEmotion] Failed to load text for {_script}: {ex.Message}");
            }
            // Fallback: mod localize table (PickUpCreature_{script}_Name/Desc/FlaverText).
            Name = _script;
            Desc = string.Empty;
            FlaverText = string.Empty;
            TryFillFromModTextTable();
        }
        #endregion

        #region --- Getters / setters / checks ---


        /// <summary>
        /// CreaturePickUp_Table.xml has full CN name/desc/flavor for reward scripts (UniverseZogak2 etc.).
        /// Used when vanilla Name-keyed lookup failed or only partially filled.
        /// </summary>
        private void TryFillFromModTextTable()
        {
            if (string.IsNullOrEmpty(_script))
                return;
            try
            {
                string name = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText(
                    "PickUpCreature_" + _script + "_Name");
                string desc = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText(
                    "PickUpCreature_" + _script + "_Desc");
                string flavor = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText(
                    "PickUpCreature_" + _script + "_FlaverText");
                if (IsMissingText(Name) && !IsMissingText(name)
                    && name.IndexOf("PickUpCreature_", StringComparison.Ordinal) < 0)
                    Name = name;
                if (IsMissingText(Desc) && !IsMissingText(desc)
                    && desc.IndexOf("PickUpCreature_", StringComparison.Ordinal) < 0)
                    Desc = desc;
                if (IsMissingText(FlaverText) && !IsMissingText(flavor)
                    && flavor.IndexOf("PickUpCreature_", StringComparison.Ordinal) < 0)
                    FlaverText = flavor;
            }
            catch { /* table missing is fine */ }
        }
        #endregion

        #region --- Other helpers ---


        /// <summary>
        /// Vanilla AbnormalityCards.xml is keyed by EmotionCard.Name (e.g. SnowWhite_Vine),
        /// while reward scripts are snowwhite1. Looking up by script alone yields "Not found"
        /// and previously overwrote the good Name-keyed entry during GetReward injection.
        /// </summary>
        public static AbnormalityCard ResolveAbnormalityDesc(string script)
        {
            if (string.IsNullOrEmpty(script))
                return null;

            AbnormalityCardDescXmlList list = Singleton<AbnormalityCardDescXmlList>.Instance;
            if (list == null)
                return null;

            // 1) Prefer the vanilla EmotionCard.Name (SnowWhite_Vine / RedShoes_Kirakira / …).
            EmotionCardXmlInfo vanilla = RMRAbnormalityUnlockManager.FindVanillaEmotionCard(script);
            if (vanilla != null && !string.IsNullOrEmpty(vanilla.Name))
            {
                AbnormalityCard byName = list.GetAbnormalityCard(vanilla.Name);
                if (byName != null && !IsMissingDesc(byName))
                    return byName;
            }

            // 2) Registered pickup may already hold the resolved Name.
            if (LogLikeMod.PickUpXml_Dummy_Passive != null)
            {
                foreach (var kv in LogLikeMod.PickUpXml_Dummy_Passive)
                {
                    if (kv.Value == null)
                        continue;
                    EmotionCardXmlInfo registered = kv.Value.Find(c =>
                        c != null
                        && c.Script != null
                        && c.Script.Any(s => string.Equals(s, script, StringComparison.OrdinalIgnoreCase)));
                    if (registered != null && !string.IsNullOrEmpty(registered.Name)
                        && !string.Equals(registered.Name, script, StringComparison.OrdinalIgnoreCase))
                    {
                        AbnormalityCard byRegistered = list.GetAbnormalityCard(registered.Name);
                        if (byRegistered != null && !IsMissingDesc(byRegistered))
                            return byRegistered;
                    }
                }
            }

            // 3) Fall back to script key (works for cards whose Name == Script).
            AbnormalityCard byScript = list.GetAbnormalityCard(script);
            if (byScript != null && !IsMissingDesc(byScript))
                return byScript;

            return byScript;
        }
        #endregion

        #region --- Getters / setters / checks ---


        public static bool IsMissingDesc(AbnormalityCard desc)
        {
            if (desc == null)
                return true;
            return IsMissingText(desc.cardName)
                && IsMissingText(desc.abilityDesc)
                && IsMissingText(desc.flavorText);
        }

        public static bool IsMissingText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return true;
            return string.Equals(text, "Not found", StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "Not Found", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Get or create a dictionary-backed AbnormalityCard for <paramref name="key"/>.
        /// GetAbnormalityCard may return a transient "Not found" stub that is intentionally
        /// not cached (to avoid poisoning real abno locales). Writers must upsert here so
        /// LevelUpUI / stage pick can later read the filled text.
        /// </summary>
        public static AbnormalityCard EnsureDescEntry(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;
            AbnormalityCardDescXmlList list = Singleton<AbnormalityCardDescXmlList>.Instance;
            if (list == null)
                return null;

            FieldInfo field = typeof(AbnormalityCardDescXmlList).GetField("_dictionary", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Dictionary<string, AbnormalityCard> dictionary = field != null
                ? field.GetValue(list) as Dictionary<string, AbnormalityCard>
                : null;
            if (dictionary == null)
                return null;

            if (dictionary.TryGetValue(key, out AbnormalityCard existing) && existing != null)
                return existing;

            AbnormalityCard created = new AbnormalityCard()
            {
                id = key,
                abnormalityName = string.Empty,
                cardName = string.Empty,
                abilityDesc = string.Empty,
                flavorText = string.Empty,
                dialogues = null
            };
            dictionary[key] = created;
            return created;
        }

        /// <summary>
        /// True when text is usable UI display (not missing, not the script id, not Hangul-only tofu risk).
        /// </summary>
        public static bool IsUsablePickUpDisplayText(string text, string script)
        {
            if (IsMissingText(text))
                return false;
            if (!string.IsNullOrEmpty(script)
                && string.Equals(text, script, StringComparison.OrdinalIgnoreCase))
                return false;
            try
            {
                if (RewardingModel.IsPoorDisplayNamePublic(text))
                    return false;
            }
            catch { /* ignore */ }
            return true;
        }
        #endregion

        #region --- Other helpers ---


        /// <summary>
        /// Inject resolved name/desc into the AbnormalityCard slot used by the pick UI.
        /// Never overwrites a good vanilla entry with "Not found".
        /// </summary>
        public static void InjectResolvedDesc(EmotionCardXmlInfo card, PickUpModelBase pickUp)
        {
            if (card == null)
                return;

            string script = card.Script != null && card.Script.Count > 0 ? card.Script[0] : null;

            // Prefer vanilla EmotionCard.Name as dictionary key (SnowWhite_Vine), not raw script
            // (ScorchedGirl1) — UI SetTexts looks up AbnormalityCardDesc by Name.
            // Uses FindVanillaEmotionCard (mod→vanilla script aliases + full list scan).
            try
            {
                if ((string.IsNullOrEmpty(card.Name)
                     || string.Equals(card.Name, script, StringComparison.OrdinalIgnoreCase))
                    && !string.IsNullOrEmpty(script))
                {
                    EmotionCardXmlInfo vanilla = RMRAbnormalityUnlockManager.FindVanillaEmotionCard(script);
                    if (vanilla != null)
                    {
                        if (!string.IsNullOrEmpty(vanilla.Name))
                            card.Name = vanilla.Name;
                        if (!string.IsNullOrEmpty(vanilla.Artwork))
                            card._artwork = vanilla.Artwork;
                        if (vanilla.Sephirah != SephirahType.None)
                            card.Sephirah = vanilla.Sephirah;
                        card.State = vanilla.State;
                    }
                }
            }
            catch { /* ignore presentation refresh */ }

            string key = !string.IsNullOrEmpty(card.Name) ? card.Name
                : script;
            if (string.IsNullOrEmpty(key))
                return;

            // Must be dictionary-backed — transient GetAbnormalityCard stubs are discarded.
            AbnormalityCard target = EnsureDescEntry(key);
            if (target == null)
                return;

            // Prefer PickUpModel text only when it is real localized content (custom PickUpModels).
            // Script-id fallbacks and Hangul leftovers must not overwrite vanilla Chinese.
            // Do NOT skip vanilla fill when abilityDesc is still missing — that left the pick UI
            // with a good title/flavor but 口口口 / empty mechanical description.
            if (pickUp != null && IsUsablePickUpDisplayText(pickUp.Name, script))
            {
                target.cardName = pickUp.Name;
                if (IsUsablePickUpDisplayText(pickUp.FlaverText, script))
                    target.flavorText = pickUp.FlaverText;
                if (IsUsablePickUpDisplayText(pickUp.Desc, script))
                    target.abilityDesc = pickUp.Desc;
            }

            // Fill any still-missing / unusable fields from vanilla Name-keyed AbnormalityCards.
            if (!IsUsablePickUpDisplayText(target.cardName, script)
                || IsMissingText(target.abilityDesc)
                || IsMissingText(target.flavorText)
                || !IsUsablePickUpDisplayText(target.abilityDesc, script)
                || !IsUsablePickUpDisplayText(target.flavorText, script))
            {
                AbnormalityCard resolved = ResolveAbnormalityDesc(script);
                if (resolved != null)
                {
                    if (!IsUsablePickUpDisplayText(target.cardName, script)
                        && IsUsablePickUpDisplayText(resolved.cardName, script))
                        target.cardName = resolved.cardName;
                    if ((!IsUsablePickUpDisplayText(target.flavorText, script)
                         || IsMissingText(target.flavorText))
                        && !IsMissingText(resolved.flavorText))
                        target.flavorText = resolved.flavorText;
                    if ((!IsUsablePickUpDisplayText(target.abilityDesc, script)
                         || IsMissingText(target.abilityDesc))
                        && !IsMissingText(resolved.abilityDesc))
                        target.abilityDesc = resolved.abilityDesc;
                    if (string.IsNullOrEmpty(target.abnormalityName)
                        && !string.IsNullOrEmpty(resolved.abnormalityName))
                        target.abnormalityName = resolved.abnormalityName;
                    if (target.dialogues == null && resolved.dialogues != null)
                        target.dialogues = resolved.dialogues;
                }
            }

            // Also stamp under the raw script key so any UI path that looks up by Script works.
            if (!string.IsNullOrEmpty(script)
                && !string.Equals(script, key, StringComparison.OrdinalIgnoreCase))
            {
                AbnormalityCard scriptSlot = EnsureDescEntry(script);
                if (scriptSlot != null)
                {
                    if (!IsUsablePickUpDisplayText(scriptSlot.cardName, script))
                        scriptSlot.cardName = target.cardName;
                    if (IsMissingText(scriptSlot.abilityDesc))
                        scriptSlot.abilityDesc = target.abilityDesc;
                    if (IsMissingText(scriptSlot.flavorText))
                        scriptSlot.flavorText = target.flavorText;
                }
            }
        }
        #endregion

        #region --- UI show / hide / build ---


        private static EmotionCardAbilityBase CreateAbility(string script)
        {
            Type type = FindAbilityType(script);
            return type == null ? null : Activator.CreateInstance(type) as EmotionCardAbilityBase;
        }
        #endregion

        #region --- Getters / setters / checks ---


        private static Type FindAbilityType(string script)
        {
            if (string.IsNullOrEmpty(script))
                return null;

            string typeName = "EmotionCardAbility_" + script.Trim();
            IEnumerable<Assembly> assemblies = LogLikeMod.GetAssemList()
                .Concat(new[] { typeof(EmotionCardAbilityBase).Assembly })
                .Distinct();
            foreach (Assembly assembly in assemblies)
            {
                foreach (Type type in LogLikeMod.GetLoadableTypes(assembly))
                {
                    if (string.Equals(type.Name, typeName, StringComparison.OrdinalIgnoreCase)
                        && typeof(EmotionCardAbilityBase).IsAssignableFrom(type))
                        return type;
                }
            }
            return null;
        }
        #endregion

    }
}
