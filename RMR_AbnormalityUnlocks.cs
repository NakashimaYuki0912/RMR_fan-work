// =============================================================================
// RMRAbnormalityUnlockManager - abnormality / E.G.O. progression for RMR.
//
// Isolation: realization exclusives (passive IDs 15370401-15370436 + script roots)
// never enter the normal reception abno pool until that floor is realized.
// Emotion tiers: vanilla EmotionLevel 1-2 -> I, 3-4 -> II, 5 -> III (team emotion gates).
// Route unlocks are per-run; permanent tiers / realizations persist on disk.
// =============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using abcdcode_LOGLIKE_MOD;
using GameSave;
using HarmonyLib;
using LOR_DiceSystem;
using LOR_XML;
using UnityEngine;

namespace RogueLike_Mod_Reborn
{
    /// <summary>
    /// Unlocks, pools, emotion tiers, and boss rewards for abnormality / E.G.O. pages.
    /// </summary>
    public static class RMRAbnormalityUnlockManager
    {
        #region --- Constants, floor tables, exclusive passive IDs ---

        public const int AbnormalityBattleRewardCount = 3;
        public const int MysteryRewardCount = 1;
        public const float UrbanLegendNormalAbnormalityRewardChance = 0.5f;
        public const float UrbanPlagueNormalAbnormalityRewardChance = 0.5f;

        public const int BlackForestMysteryId = 991001;
        public const int WellMysteryId = 991002;
        public const int HandsOfLightMysteryId = 991003;

        private const string ProgressSaveName = "RMR_AbnormalityProgress";
        private const string RealizationSaveName = "RMR_FloorRealizations";
        private const string RedMistVictorySaveName = "RMR_RedMistChallengeCleared";
        private const int NoAbnormalityFallbackBaseId = 15999000;
        private const int RedMistStageId = 60020;
        private const int RedMistCorePageId = 250022;
        private const int BlackSilenceStageId = 70020;
        private const int DistortedEnsembleStageId = 70021;
        private const int DistortedEnsembleLastStageId = 70021;

        private static readonly int[] RedMistBattlePageIds =
        {
            607003,
            607004,
            607005,
            607006,
            607007
        };

        private static readonly int[] BlueReverberationBattlePageIds =
        {
            704001,
            704011,
            704012,
            704013,
            704014,
            705010,
            705011
        };

        private static readonly List<LorId> RouteUnlockedPages = new List<LorId>();
        private static readonly HashSet<LorId> RouteUnlockedEgoPages = new HashSet<LorId>();
        private static readonly HashSet<int> PermanentlyUnlockedTiers = new HashSet<int>();
        private static readonly HashSet<SephirahType> CompletedRealizations = new HashSet<SephirahType>();
        /// <summary>script → vanilla EmotionLevel tier (1/2/3).</summary>
        private static readonly Dictionary<string, int> _vanillaEmotionTierByScript =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static bool _vanillaEmotionTiersLoaded;
        private static bool _staticEmotionTiersSeeded;
        private static bool BinahUnlockedForCurrentRoute;
        private static bool RedMistVictoryRewardsGrantedThisBattle;
        private static bool BlackSilenceClearRecordedThisBattle;
        private static bool BlueReverberationRewardsGrantedThisBattle;
        private static bool BattleClearRewardsEnqueuedThisBattle;

        // Floor -> all abnormality script roots on that floor
        // Sourced from vanilla EmotionCard_*.txt <Sephirah> tags
        public static readonly Dictionary<SephirahType, string[]> FloorAbnormalityScripts = new Dictionary<SephirahType, string[]>
        {
            { SephirahType.Malkuth, new[] { "ScorchedGirl", "HappyTeddyBear", "FairyCarnival", "QueenBee", "snowwhite" } },
            { SephirahType.Yesod, new[] { "ForsakenMurderer", "LittleHelper", "SingingMachine", "Butterfly", "freischutz" } },
            { SephirahType.Hod, new[] { "ShyLookToday", "RedShoes", "SpiderBud", "Laetitia", "blackswan" } },
            { SephirahType.Netzach, new[] { "UniverseZogak", "ChildofGalaxy", "Porccubus", "Alriune", "orchestra" } },
            { SephirahType.Tiphereth, new[] { "QueenOfHatred", "KnightOfDespair", "Greed", "Angry", "clownofnihil" } },
            { SephirahType.Gebura, new[] { "Redhood", "BigBadWolf", "Mountain", "Nosferatu", "nothing" } },
            { SephirahType.Chesed, new[] { "ScareCrow", "LumberJack", "House", "Ozma", "wizard" } },
            { SephirahType.Binah, new[] { "Bigbird", "SmallBird", "LongBird", "bossbird" } },
            { SephirahType.Hokma, new[] { "Bloodytree", "Clock", "BlueStar", "onebadmanygood", "plaguedoctor", "whitenight" } },
            { SephirahType.Keter, new[] { "BloodBath", "HeartofAspiration", "Pinocchio", "TheSnowQueen", "quietKid" } },
        };

        // Final-floor realization exclusives (CreatureInfo_PickTable GradeAll Unique Level 5–6).
        // Completing that floor opens these scripts to shop + post-battle abno 3-picks.
        // Roots must cover ALL numbered forms (freischutz1–3, not only freischutz3).
        // Do NOT list normal floor abnos (SingingMachine / Butterfly are regular Yesod pages).
        public static readonly Dictionary<SephirahType, string[]> RealizationRewardScriptsByFloor = new Dictionary<SephirahType, string[]>
        {
            { SephirahType.Malkuth, new[] { "snowwhite" } },           // snowwhite1–3
            { SephirahType.Yesod, new[] { "freischutz" } },             // freischutz1–3 (Der Freischütz final)
            { SephirahType.Hod, new[] { "blackswan" } },                // blackswan1–3
            { SephirahType.Netzach, new[] { "orchestra" } },            // orchestra1–3
            { SephirahType.Tiphereth, new[] { "clownofnihil" } },       // clownofnihil1–3
            { SephirahType.Gebura, new[] { "nothing" } },               // nothing1–3 (Nothing There)
            { SephirahType.Chesed, new[] { "wizard" } },                // wizard1–3
            { SephirahType.Binah, new[] { "bossbird" } },               // bossbird1–6 (Apocalypse Bird)
            { SephirahType.Hokma, new[] { "whitenight", "plaguedoctor", "onebadmanygood" } },
            { SephirahType.Keter, new[] { "quietKid" } },               // quietKidHammer / EyeShine / Guilty
        };

        /// <summary>
        /// RewardPassive IDs for GradeAll exclusive block (CreatureInfo_PickTable 15370401–15370436).
        /// Used as a hard isolation key so partial script-list mistakes cannot leak into normal drops.
        /// </summary>
        public const int RealizationExclusivePassiveIdMin = 15370401;
        public const int RealizationExclusivePassiveIdMax = 15370436;

        /// <summary>
        /// Exclusive passive ID → floor (mirrors CreatureInfo_PickTable GradeAll block order).
        /// Used when script matching fails so exclusives never slip into the normal pool ungated.
        /// </summary>
        private static SephirahType GetRealizationFloorForExclusivePassiveId(int passiveId)
        {
            if (passiveId < RealizationExclusivePassiveIdMin || passiveId > RealizationExclusivePassiveIdMax)
                return SephirahType.None;
            if (passiveId <= 15370403) return SephirahType.Malkuth;   // snowwhite1–3
            if (passiveId <= 15370406) return SephirahType.Yesod;     // freischutz1–3
            if (passiveId <= 15370409) return SephirahType.Hod;       // blackswan1–3
            if (passiveId <= 15370412) return SephirahType.Netzach;   // orchestra1–3
            if (passiveId <= 15370415) return SephirahType.Tiphereth; // clownofnihil1–3
            if (passiveId <= 15370418) return SephirahType.Gebura;    // nothing1–3
            if (passiveId <= 15370421) return SephirahType.Chesed;    // wizard1–3
            if (passiveId <= 15370427) return SephirahType.Binah;     // bossbird1–6
            if (passiveId <= 15370433) return SephirahType.Hokma;     // onebad/plague/whitenight
            return SephirahType.Keter;                                // quietKid*
        }

        // Boss clear rewards use the completed floor's full vanilla abnormality pool.
        // This is intentionally wider than RealizationRewardScriptsByFloor: Binah, for
        // example, must be able to roll Bigbird/SmallBird/LongBird pages after her
        // realization, not only the final bossbird entries.
        private static readonly Dictionary<SephirahType, string[]> BossRealizationRewardScriptsByFloor = FloorAbnormalityScripts;

        public static readonly HashSet<string> RealizationExclusiveScripts = new HashSet<string>(
            RealizationRewardScriptsByFloor.SelectMany(x => x.Value), StringComparer.OrdinalIgnoreCase);

        public static readonly Dictionary<SephirahType, LorId[]> RealizationEgoCardsByFloor = new Dictionary<SephirahType, LorId[]>
        {
            { SephirahType.Malkuth, new[] { new LorId(910001), new LorId(910002), new LorId(910003), new LorId(910004), new LorId(910005) } },
            { SephirahType.Yesod, new[] { new LorId(910011), new LorId(910012), new LorId(910013), new LorId(910014), new LorId(910015) } },
            { SephirahType.Hod, new[] { new LorId(910016), new LorId(910017), new LorId(910018), new LorId(910019), new LorId(910020) } },
            { SephirahType.Netzach, new[] { new LorId(910021), new LorId(910022), new LorId(910023), new LorId(910024), new LorId(910025) } },
            { SephirahType.Tiphereth, new[] { new LorId(910026), new LorId(910027), new LorId(910028), new LorId(910029), new LorId(910030) } },
            { SephirahType.Gebura, new[] { new LorId(910031), new LorId(910032), new LorId(910033), new LorId(910034), new LorId(910035) } },
            { SephirahType.Chesed, new[] { new LorId(910036), new LorId(910037), new LorId(910038), new LorId(910039), new LorId(910040) } },
            { SephirahType.Binah, new[] { new LorId(910041), new LorId(910042), new LorId(910043), new LorId(910044), new LorId(910045) } },
            { SephirahType.Hokma, new[] { new LorId(910046), new LorId(910047), new LorId(910048), new LorId(910049), new LorId(910050) } },
            { SephirahType.Keter, new[] { new LorId(910086), new LorId(910087), new LorId(910088), new LorId(910089), new LorId(910090) } },
        };
        private static readonly string[] SimpleRoots =
        {
            "ScorchedGirl", "HappyTeddyBear", "FairyCarnival", "QueenBee",
            "ForsakenMurderer", "LittleHelper", "SingingMachine", "Butterfly",
            "ShyLookToday", "RedShoes", "SpiderBud", "Laetitia",
            "UniverseZogak", "ChildofGalaxy", "Porccubus", "Alriune"
        };

        private static readonly string[] MediumRoots =
        {
            "QueenOfHatred", "KnightOfDespair", "Greed", "Angry",
            "Redhood", "BigBadWolf", "Mountain", "Nosferatu",
            "ScareCrow", "LumberJack", "House", "Ozma"
        };

        private static readonly string[] HardRoots =
        {
            "BloodBath", "HeartofAspiration", "Pinocchio", "TheSnowQueen",
            "Bigbird", "SmallBird", "LongBird",
            "Bloodytree", "Clock", "BlueStar"
        };

        #endregion

        #region --- Route / permanent progress (save-load) ---

        public static void StartNewRoute(ChapterGrade grade)
        {
            RouteUnlockedPages.Clear();
            RouteUnlockedEgoPages.Clear();
            BinahUnlockedForCurrentRoute = false;
            LoadPermanentProgress();
            LoadRealizationProgress();
            // Do NOT bulk-UnlockPage permanent-tier abnos into the mid-battle emotion pool.
            // Permanent tiers only gate shop/reward availability (IsRewardTierAvailableForChapter).
            // Mid-battle abno picks use pages actually obtained this route (rewards/shop/emotion).
        }

        public static void ResetArchiveProgress()
        {
            RouteUnlockedPages.Clear();
            RouteUnlockedEgoPages.Clear();
            BinahUnlockedForCurrentRoute = false;
            PermanentlyUnlockedTiers.Clear();
            DeleteSaveFile(ProgressSaveName);
        }

        public static void ResetAllPermanentProgress()
        {
            RouteUnlockedPages.Clear();
            RouteUnlockedEgoPages.Clear();
            BinahUnlockedForCurrentRoute = false;
            PermanentlyUnlockedTiers.Clear();
            CompletedRealizations.Clear();
            _realizationProgressLoaded = false;
            ResetRedMistChallengeBattleState();
            DeleteSaveFile(ProgressSaveName);
            DeleteSaveFile(RealizationSaveName);
            DeleteSaveFile(RedMistVictorySaveName);
            // Confirm disk wipe took effect (stale exclusives must not re-enter pools).
            LoadRealizationProgress(force: true);
            Debug.Log($"[RMRAbnormalityUnlockManager] ResetAllPermanentProgress: realization clears={CompletedRealizations.Count}");
        }

        public static SaveData SaveRouteUnlocks()
        {
            SaveData data = new SaveData(SaveDataType.Dictionary);
            SaveData pages = new SaveData();
            foreach (LorId id in RouteUnlockedPages)
                pages.AddToList(id.LogGetSaveData());
            data.AddData("Pages", pages);
            SaveData egoPages = new SaveData();
            foreach (LorId id in RouteUnlockedEgoPages)
                egoPages.AddToList(id.LogGetSaveData());
            data.AddData("EgoPages", egoPages);
            data.AddData("BinahUnlocked", BinahUnlockedForCurrentRoute ? 1 : 0);
            return data;
        }

        public static void LoadRouteUnlocks(SaveData data)
        {
            RouteUnlockedPages.Clear();
            RouteUnlockedEgoPages.Clear();
            BinahUnlockedForCurrentRoute = false;
            LoadPermanentProgress();
            LoadRealizationProgress();
            if (data == null)
            {
                // Empty route unlocks -- permanent tiers still gate shop rolls only.
                return;
            }
            SaveData pages = data.GetData("Pages");
            if (pages == null)
                pages = data;
            else
                BinahUnlockedForCurrentRoute = data.GetInt("BinahUnlocked") > 0;
            // Only restore pages that were saved as route unlocks (obtained this run).
            // Never re-inject GetPermanentStartingPages bulk unlocks.
            foreach (SaveData item in pages)
            {
                LorId id = ExtensionUtils.LogLoadFromSaveData(item);
                UnlockPage(id);
            }
            SaveData egoPages = data.GetData("EgoPages");
            if (egoPages != null)
            {
                foreach (SaveData item in egoPages)
                    UnlockEgoForCurrentRoute(ExtensionUtils.LogLoadFromSaveData(item));
            }
        }

        #endregion

        #region --- Emotion tier mapping (vanilla EmotionLevel -> I/II/III) ---

        public static List<RewardPassiveInfo> GetUnlockedEmotionCardsForBattle()
        {
            // Realization: permanent atlas pool (normal + exclusive pages already recorded).
            if (RMRRealizationManager.InRealizationBattle
                || RMRRealizationManager.IsRealizationPreparationActive)
            {
                LogueBookModels.EnsureCompendiumUnlocks();
                return LogueBookModels.CompendiumUnlockedAbnormalityPages
                    .Select(id => Singleton<RewardPassivesList>.Instance.GetPassiveInfo(id))
                    .Where(info => info != null && info.rewardtype == RewardType.Creature && !IsNoAbnormalityFallback(info.id))
                    .ToList();
            }

            // Normal RMR run: mid-battle emotion picks only from pages obtained this route
            // (battle rewards / shop / prior emotion picks) — not the entire catalog.
            // Realization exclusives still require floor clear (post-reset isolation).
            EnsureRealizationProgressLoaded();
            var list = RouteUnlockedPages
                .Select(id => Singleton<RewardPassivesList>.Instance.GetPassiveInfo(id))
                .Where(CanAppearInNormalReceptionAbnoPool)
                .ToList();
            Debug.Log($"[RMRAbnormalityUnlockManager] Mid-battle abno pool size={list.Count} (route-unlocked, exclusive gated).");
            return list;
        }

        /// <summary>
        /// Vanilla mapping (EmotionCard Xml &lt;EmotionLevel&gt; = page tier I/II/III):
        ///   Tier I   (EmotionLevel=1) -> team emotion 1-2
        ///   Tier II  (EmotionLevel=2) -> team emotion 3-4
        ///   Tier III (EmotionLevel=3) -> team emotion 5
        /// The digit in a script name is a page index, NOT the tier: butterfly2 is tier I
        /// while butterfly3 is tier III, and singingMachine (page 1) is tier III while
        /// singingMachine2 is tier I. Read the tier from EmotionLevel, never from the suffix.
        /// </summary>
        public static int GetRequiredAbnoTierForTeamEmotion(int teamEmotionLevel)
        {
            if (teamEmotionLevel <= 0)
                return 1;
            if (teamEmotionLevel <= 2)
                return 1;
            if (teamEmotionLevel <= 4)
                return 2;
            return 3;
        }

        /// <summary>
        /// Resolve vanilla abnormality tier (1/2/3) for a reward page via EmotionCardXmlList.
        /// Cached. Returns 0 if unknown.
        /// IMPORTANT: mod reward scripts (ShyLookToday1, SingingMachine1, …) must be aliased to
        /// vanilla EmotionCard Script values before lookup — otherwise every page falls through
        /// as tier 0 and incorrectly appears at team emotion 1–2 together.
        /// </summary>
        public static int GetVanillaAbnoTier(RewardPassiveInfo info)
        {
            if (info == null || string.IsNullOrEmpty(info.script))
                return 0;
            return GetVanillaAbnoTierForScript(info.script);
        }

        public static int GetVanillaAbnoTierForScript(string script)
        {
            if (string.IsNullOrEmpty(script))
                return 0;
            EnsureVanillaEmotionTiersLoaded();
            string key = script.Trim();
            if (TryLookupCachedAbnoTier(key, out int tier))
                return tier;

            // Mod reward scripts (PascalCase + digit) rarely match vanilla Script keys directly.
            // Reuse the same alias chain used for artwork/desc resolution.
            foreach (string candidate in GetVanillaScriptCandidates(key))
            {
                if (string.Equals(candidate, key, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (TryLookupCachedAbnoTier(candidate, out tier))
                {
                    _vanillaEmotionTierByScript[key] = tier;
                    return tier;
                }
            }

            // Live resolve: scan EmotionCardXmlList with full mod↔vanilla matching.
            try
            {
                EmotionCardXmlInfo card = FindVanillaEmotionCard(key);
                if (card != null)
                {
                    tier = ReadEmotionLevelField(card);
                    if (tier >= 1 && tier <= 3)
                    {
                        _vanillaEmotionTierByScript[key] = tier;
                        if (card.Script != null)
                        {
                            foreach (string s in card.Script)
                            {
                                if (!string.IsNullOrEmpty(s))
                                    _vanillaEmotionTierByScript[s] = tier;
                            }
                        }
                        return tier;
                    }
                }
            }
            catch { /* ignore */ }

            return 0;
        }

        private static bool TryLookupCachedAbnoTier(string script, out int tier)
        {
            tier = 0;
            if (string.IsNullOrEmpty(script))
                return false;
            if (_vanillaEmotionTierByScript.TryGetValue(script, out tier) && tier > 0)
                return true;
            return false;
        }

        private static void EnsureVanillaEmotionTiersLoaded()
        {
            // Static seed always available (even before EmotionCardXmlList is ready).
            if (!_staticEmotionTiersSeeded)
            {
                SeedStaticVanillaEmotionTiers();
                SeedModScriptAliasesIntoTierCache();
                _staticEmotionTiersSeeded = true;
            }

            if (_vanillaEmotionTiersLoaded)
                return;

            try
            {
                EmotionCardXmlList list = Singleton<EmotionCardXmlList>.Instance;
                if (list == null)
                    return; // retry later; static map still usable

                int loaded = LoadVanillaEmotionTiersFromXmlList(list);
                SeedModScriptAliasesIntoTierCache();

                // Only lock the live load once we actually read EmotionLevel values.
                // If the list is still empty (very early init), keep retrying.
                if (loaded > 0)
                {
                    _vanillaEmotionTiersLoaded = true;
                    Debug.Log($"[RMRAbnormalityUnlockManager] Loaded vanilla abno EmotionLevel tiers: liveEntries={loaded}, cacheKeys={_vanillaEmotionTierByScript.Count}.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMRAbnormalityUnlockManager] EnsureVanillaEmotionTiersLoaded: " + ex.Message);
            }
        }

        private static int LoadVanillaEmotionTiersFromXmlList(EmotionCardXmlList list)
        {
            int loaded = 0;
            if (list == null)
                return 0;

            // Prefer full _list scan (avoids Level vs EmotionLevel filter confusion).
            try
            {
                FieldInfo fi = AccessTools.Field(typeof(EmotionCardXmlList), "_list");
                var all = fi?.GetValue(list) as System.Collections.IList;
                if (all != null)
                {
                    foreach (object o in all)
                    {
                        EmotionCardXmlInfo card = o as EmotionCardXmlInfo;
                        loaded += RegisterEmotionCardTier(card);
                    }
                }
            }
            catch { /* fall through to floor API */ }

            // Also walk GetDataListByLevel (covers loaders that do not expose _list).
            foreach (SephirahType floor in FloorAbnormalityScripts.Keys)
            {
                for (int floorLevel = 1; floorLevel <= 6; floorLevel++)
                {
                    List<EmotionCardXmlInfo> cards = null;
                    try { cards = list.GetDataListByLevel(floor, floorLevel); }
                    catch { continue; }
                    if (cards == null)
                        continue;
                    foreach (EmotionCardXmlInfo card in cards)
                        loaded += RegisterEmotionCardTier(card);
                }
            }
            return loaded;
        }

        /// <summary>
        /// Record exact vanilla Script → EmotionLevel. Do NOT map digit-stripped roots:
        /// singingMachine is tier III while singingMachine2 is tier I; root collapse mixes tiers.
        /// </summary>
        private static int RegisterEmotionCardTier(EmotionCardXmlInfo card)
        {
            if (card?.Script == null)
                return 0;
            int tier = ReadEmotionLevelField(card);
            if (tier < 1 || tier > 3)
                return 0;
            int n = 0;
            foreach (string s in card.Script)
            {
                if (string.IsNullOrEmpty(s))
                    continue;
                _vanillaEmotionTierByScript[s] = tier;
                n++;
            }
            return n;
        }

        /// <summary>
        /// Copy resolved vanilla tiers onto mod reward script keys (SingingMachine1 → 3, etc.).
        /// </summary>
        private static void SeedModScriptAliasesIntoTierCache()
        {
            foreach (KeyValuePair<string, string> kvp in ModScriptToVanillaScript)
            {
                if (string.IsNullOrEmpty(kvp.Key) || string.IsNullOrEmpty(kvp.Value))
                    continue;
                if (_vanillaEmotionTierByScript.TryGetValue(kvp.Value, out int tier) && tier > 0)
                    _vanillaEmotionTierByScript[kvp.Key] = tier;
            }
        }

        /// <summary>
        /// Hardcoded vanilla EmotionLevel map (from EmotionCard_*.txt). Ensures tier filter
        /// works even if EmotionCardXmlList is not ready or Reflection misses EmotionLevel.
        /// Keys are vanilla Script values (case-insensitive dictionary).
        /// </summary>
        private static void SeedStaticVanillaEmotionTiers()
        {
            // script → EmotionLevel (page tier I/II/III)
            void Put(string script, int tier)
            {
                if (string.IsNullOrEmpty(script) || tier < 1 || tier > 3)
                    return;
                // Live/XML values win once loaded; static only fills gaps.
                if (_vanillaEmotionTierByScript.ContainsKey(script))
                    return;
                _vanillaEmotionTierByScript[script] = tier;
            }

            // Yesod (tech) — Music=III, Rhythm=I, Musical Addiction=II.
            // Source: vanilla EmotionCard_yesod plus Library_of_Ruina_Abnormality_Pages_EN.
            Put("murderer", 1); Put("murderer2", 2); Put("murderer3", 1);
            Put("helper", 2); Put("helper2", 1); Put("helper3", 2);
            Put("singingMachine", 3); Put("singingMachine2", 1); Put("singingMachine3", 2);
            Put("singingmachine", 3); Put("singingmachine2", 1); Put("singingmachine3", 2);
            // Mod reward scripts (direct keys — alias seed also fills these)
            Put("SingingMachine1", 3); Put("SingingMachine2", 1); Put("SingingMachine3", 2);
            Put("butterfly1", 2); Put("butterfly2", 1); Put("butterfly3", 3);
            Put("Butterfly3", 3);
            Put("freischutz1", 1); Put("freischutz2", 2); Put("freischutz3", 3);

            // Hod — shyLook / 今日的表情 = tier I
            Put("shyLook", 1); Put("shyLook2", 1); Put("shyLook3", 2);
            Put("shylook", 1); Put("shylook2", 1); Put("shylook3", 2);
            Put("ShyLookToday1", 1); Put("ShyLookToday2", 1); Put("ShyLookToday3", 2);
            Put("redshoes", 1); Put("redshoes2", 2); Put("redshoes3", 1);
            Put("spiderbud", 1); Put("spiderbud2", 2); Put("spiderbud3", 2);
            Put("latitia1", 1); Put("latitia2", 2); Put("latitia3", 2);
            Put("blackswan1", 3); Put("blackswan2", 3); Put("blackswan3", 3);

            // Malkuth
            Put("burnninggirl", 1); Put("burnninggirl2", 2); Put("burnninggirl3", 1);
            Put("teddy", 1); Put("teddy2", 1); Put("teddy3", 1);
            Put("fairy1", 1); Put("fairy2", 2); Put("fairy3", 2);
            Put("queenbee1", 2); Put("queenbee2", 2); Put("queenbee3", 3);
            Put("snowwhite1", 2); Put("snowwhite2", 3); Put("snowwhite3", 3);

            // Netzach
            Put("fragmentSpace", 1); Put("fragmentSpace2", 1); Put("fragmentSpace3", 2);
            Put("fragmentspace", 1); Put("fragmentspace2", 1); Put("fragmentspace3", 2);
            Put("galaxyChild", 1); Put("galaxyChild2", 2); Put("galaxyChild3", 2);
            Put("galaxychild", 1); Put("galaxychild2", 2); Put("galaxychild3", 2);
            Put("porccubus", 1); Put("porccubus2", 1); Put("porccubus3", 1);
            Put("alriune1", 2); Put("alriune2", 2); Put("alriune3", 3);
            Put("orchestra1", 2); Put("orchestra2", 3); Put("orchestra3", 3);

            // Keter
            Put("bloodbath", 2); Put("bloodbath2", 1); Put("bloodbath3", 1);
            Put("heart", 1); Put("heart_rush", 2); Put("doki", 1);
            Put("pinocchio1", 2); Put("pinocchio2", 1); Put("pinocchio3", 1);
            Put("snowqueen1", 2); Put("snowqueen2", 3); Put("snowqueen3", 3);
            Put("quietkidhammer", 2); Put("quietkideyeshine", 3); Put("quietkidguilty", 2);

            // Tiphereth
            Put("queenofhatred1", 1); Put("queenofhatred2", 1); Put("queenofhatred3", 1);
            Put("knightofdespair1", 1); Put("knightofdespair2", 1); Put("knightofdespair3", 1);
            Put("kingofgreed1", 2); Put("kingofgreed2", 2); Put("kingofgreed3", 2);
            Put("servantofwrath1", 2); Put("servantofwrath2", 2); Put("servantofwrath3", 3);
            Put("clownofnihil1", 2); Put("clownofnihil2", 3); Put("clownofnihil3", 3);

            // Gebura
            Put("redhood1", 1); Put("redhood2", 1); Put("redhood3", 2);
            Put("bigbadwolf1", 2); Put("bigbadwolf2", 1); Put("bigbadwolf3", 1);
            Put("danggocreature1", 2); Put("danggocreature2", 2); Put("danggocreature3", 2);
            Put("nosferatu1", 1); Put("nosferatu2", 2); Put("nosferatu3", 1);
            Put("nothing1", 3); Put("nothing2", 3); Put("nothing3", 3);

            // Chesed
            Put("scarecrow1", 1); Put("scarecrow2", 1); Put("scarecrow3", 1);
            Put("lumberjack1", 2); Put("lumberjack2", 1); Put("lumberjack3", 1);
            Put("waybackhome1", 1); Put("waybackhome2", 2); Put("waybackhome3", 2);
            Put("ozma1", 2); Put("ozma2", 2); Put("ozma3", 3);
            Put("wizard1", 2); Put("wizard2", 3); Put("wizard3", 3);

            // Binah
            Put("bigbird1", 1); Put("bigbird2", 1); Put("bigbird3", 2);
            Put("smallbird1", 2); Put("smallbird2", 2); Put("smallbird3", 1);
            Put("longbird1", 1); Put("longbird2", 2); Put("longbird3", 2);
            Put("bossbird1", 1); Put("bossbird2", 1); Put("bossbird3", 2);
            Put("bossbird4", 3); Put("bossbird5", 3); Put("bossbird6", 3);

            // Hokma
            Put("bloodytree1", 1); Put("bloodytree2", 1); Put("bloodytree3", 1);
            Put("silence1", 1); Put("silence2", 1); Put("silence3", 2);
            Put("bluestar1", 2); Put("bluestar2", 2); Put("bluestar3", 2);
            Put("onebadmanygood1", 2);
            Put("plaguedoctor1", 1);
            Put("whitenight1", 2); Put("whitenight2", 3); Put("whitenight3", 3); Put("whitenight4", 3);
        }

        private static int ReadEmotionLevelField(EmotionCardXmlInfo card)
        {
            if (card == null)
                return 0;
            try
            {
                // Public field on vanilla EmotionCardXmlInfo (also written by RegisterPickUpXml).
                int tier = card.EmotionLevel;
                if (tier >= 1 && tier <= 3)
                    return tier;
            }
            catch { /* ignore */ }
            try
            {
                FieldInfo fi = AccessTools.Field(typeof(EmotionCardXmlInfo), "EmotionLevel");
                if (fi != null)
                {
                    int tier = Convert.ToInt32(fi.GetValue(card));
                    if (tier >= 1 && tier <= 3)
                        return tier;
                }
                PropertyInfo pi = AccessTools.Property(typeof(EmotionCardXmlInfo), "EmotionLevel");
                if (pi != null)
                {
                    int tier = Convert.ToInt32(pi.GetValue(card, null));
                    if (tier >= 1 && tier <= 3)
                        return tier;
                }
            }
            catch { /* ignore */ }
            return 0;
        }

        /// <summary>
        /// True if this owned page may appear when team emotion level rises to <paramref name="teamEmotionLevel"/>.
        /// Vanilla: emo 1–2 → EmotionLevel 1 only; 3–4 → 2 only; 5 → 3 only. Never mix tiers.
        /// </summary>
        public static bool IsOwnedPageEligibleForTeamEmotion(RewardPassiveInfo info, int teamEmotionLevel)
        {
            if (info == null || IsNoAbnormalityFallback(info.id))
                return false;

            // Defense in depth: mid-battle pool is built with CanAppearInNormalReceptionAbnoPool,
            // but re-check so exclusives never show at emo 1–5 after a progress reset mid-save.
            // Realization battles intentionally use the permanent atlas (skip this gate).
            if (!RMRRealizationManager.InRealizationBattle
                && !RMRRealizationManager.IsRealizationPreparationActive
                && !CanAppearInNormalReceptionAbnoPool(info))
                return false;

            int requiredTier = GetRequiredAbnoTierForTeamEmotion(teamEmotionLevel);
            int pageTier = GetVanillaAbnoTier(info);
            if (pageTier <= 0)
            {
                // Unknown after alias + static + live resolve: exclude from ALL picks.
                // Dumping unknowns into tier I mixed SingingMachine1 (III) with ShyLookToday1 (I).
                Debug.LogWarning($"[RMRAbnormalityUnlockManager] Unknown abno EmotionLevel for script='{info.script}' id={info.id} — excluded from emotion pick (needTier={requiredTier}).");
                return false;
            }
            return pageTier == requiredTier;
        }

        /// <summary>
        /// Filter route-owned pages for a team emotion pick, then pick up to 3.
        /// </summary>
        public static List<RewardPassiveInfo> FilterOwnedPagesForTeamEmotion(
            IEnumerable<RewardPassiveInfo> ownedPool,
            int teamEmotionLevel,
            HashSet<string> alreadySelectedKeys)
        {
            int requiredTier = GetRequiredAbnoTierForTeamEmotion(teamEmotionLevel);
            var eligible = new List<RewardPassiveInfo>();
            if (ownedPool == null)
                return eligible;
            foreach (RewardPassiveInfo info in ownedPool)
            {
                if (info == null)
                    continue;
                // Must match RewardingModel.GetRewardPassiveKey: packageId may be null.
                string key = (info.id.packageId ?? "") + ":" + info.id.id;
                string keyAlt = info.id.packageId + ":" + info.id.id; // legacy null-concat form
                if (alreadySelectedKeys != null
                    && (alreadySelectedKeys.Contains(key) || alreadySelectedKeys.Contains(keyAlt)))
                    continue;
                if (!IsOwnedPageEligibleForTeamEmotion(info, teamEmotionLevel))
                    continue;
                eligible.Add(info);
            }
            Debug.Log($"[RMRAbnormalityUnlockManager] Emotion pick filter teamLv={teamEmotionLevel} needTier={requiredTier} eligible={eligible.Count} (owned pool filtered).");
            return eligible;
        }

        #endregion

        #region --- Battle clear / boss rewards (Red Mist, Black Silence, Ensemble) ---

        public static void EnqueueBattleClearRewards()
        {
            // Realization battles have their own reward path -- skip normal Roguelike reward chains
            if (RMRRealizationManager.InRealizationBattle)
                return;

            if (IsRedMistChallengeStage())
                return;

            if (LogLikeMod.curstagetype == abcdcode_LOGLIKE_MOD.StageType.Creature)
            {
                EnqueueRewardSelections(AbnormalityBattleRewardCount, LogLikeMod.curchaptergrade);
                return;
            }

            if (LogLikeMod.curstagetype == abcdcode_LOGLIKE_MOD.StageType.Normal)
            {
                if (ShouldEnqueueNormalAbnormalityReward(LogLikeMod.curchaptergrade))
                    EnqueueRewardSelections(1, LogLikeMod.curchaptergrade);
                return;
            }

            if (LogLikeMod.curstagetype == abcdcode_LOGLIKE_MOD.StageType.Elite)
                EnqueueRewardSelections(1, LogLikeMod.curchaptergrade);

            // Boss stages: add realization-tier abnormality + EGO rewards based on chapter grade
            if (LogLikeMod.curstagetype == abcdcode_LOGLIKE_MOD.StageType.Boss)
            {
                EnqueueBossRealizationTierRewards(LogLikeMod.curchaptergrade);
            }
        }

        public static bool TryEnqueueBattleClearRewardsAfterVictory()
        {
            if (BattleClearRewardsEnqueuedThisBattle)
                return true;
            if (!RewardingModel.HasConfirmedBattleVictory())
                return false;

            EnqueueBattleClearRewards();
            BattleClearRewardsEnqueuedThisBattle = true;
            Debug.Log(
                $"[RMRAbnormalityUnlockManager] Enqueued battle-clear rewards after confirmed victory: " +
                $"type={LogLikeMod.curstagetype}, grade={LogLikeMod.curchaptergrade}.");
            return true;
        }

        public static bool IsBinahUnlockedForCurrentRoute()
        {
            return BinahUnlockedForCurrentRoute;
        }

        public static void UnlockBinahForCurrentRoute()
        {
            BinahUnlockedForCurrentRoute = true;
        }

        public static void ResetRedMistChallengeBattleState()
        {
            RedMistVictoryRewardsGrantedThisBattle = false;
            BlackSilenceClearRecordedThisBattle = false;
            BlueReverberationRewardsGrantedThisBattle = false;
            BattleClearRewardsEnqueuedThisBattle = false;
        }

        private static bool IsRedMistChallengeStage()
        {
            return LogLikeMod.curstageid == new LorId(LogLikeMod.ModId, RedMistStageId);
        }

        private static bool IsBlackSilenceStage()
        {
            return LogLikeMod.curstageid == new LorId(LogLikeMod.ModId, BlackSilenceStageId);
        }

        private static bool IsDistortedEnsembleStage()
        {
            return LogLikeMod.curstageid != null
                && LogLikeMod.curstageid.packageId == LogLikeMod.ModId
                && LogLikeMod.curstageid.id >= DistortedEnsembleStageId
                && LogLikeMod.curstageid.id <= DistortedEnsembleLastStageId;
        }

        private static bool IsCurrentBattleVictory()
        {
            return BattleObjectManager.instance.GetAliveListWithAvailable(Faction.Player).Count > 0
                && BattleObjectManager.instance.GetAliveListWithAvailable(Faction.Enemy).Count == 0;
        }

        public static void SuppressRedMistChallengeGenericRewards()
        {
            if (!IsRedMistChallengeStage())
                return;
            LogLikeMod.rewards?.Clear();
            LogLikeMod.rewards_lastKill?.Clear();
            LogLikeMod.rewards_passive?.Clear();
            LogLikeMod.rewards_InStage?.Clear();
            LogLikeMod.rewardsMystery?.Clear();
            LogLikeMod.egoSelectionQueue?.Clear();
        }

        public static bool IsRedMistChallengeVictoryRecorded()
        {
            try
            {
                SaveData data = Singleton<LogueSaveManager>.Instance.LoadData(RedMistVictorySaveName);
                return data != null && data.GetInt("Cleared") > 0;
            }
            catch
            {
                return false;
            }
        }

        private static void RecordRedMistChallengeVictory()
        {
            try
            {
                SaveData data = new SaveData(SaveDataType.Dictionary);
                data.AddData("Cleared", new SaveData(1));
                Singleton<LogueSaveManager>.Instance.SaveData(data, RedMistVictorySaveName);
            }
            catch (Exception e)
            {
                Debug.LogError($"[RMRAbnormalityUnlockManager] Failed to save Red Mist challenge clear flag: {e.Message}");
            }
        }

        public static void PrunePrematureRedMistChallengeRewards()
        {
            if (IsRedMistChallengeVictoryRecorded())
                return;

            bool changed = false;
            LorId redMistBookId = new LorId(LogLikeMod.ModId, RedMistCorePageId);
            ResetPrematureRedMistLoadouts(redMistBookId);

            if (LogueBookModels.booklist != null)
                changed |= LogueBookModels.booklist.RemoveAll(book => book?.ClassInfo?.id == redMistBookId) > 0;
            if (LogueBookModels.cardlist != null)
            {
                HashSet<LorId> redMistCards = GetRedMistBattleCardLorIds();
                changed |= LogueBookModels.cardlist.RemoveAll(card => card != null && redMistCards.Contains(card.GetID())) > 0;
            }

            LogueBookModels.EnsureCompendiumUnlocks();
            changed |= LogueBookModels.CompendiumUnlockedRoleBooks.Remove(redMistBookId);
            foreach (LorId cardId in GetRedMistBattleCardLorIds())
                changed |= LogueBookModels.CompendiumUnlockedBattleCards.Remove(cardId);

            if (!changed)
                return;

            LogueBookModels.SavePermanentCompendiumUnlocks();
            Debug.Log("[RMRAbnormalityUnlockManager] Removed premature Red Mist core/battle pages; rewards require clearing the Red Mist challenge.");
        }

        private static HashSet<LorId> GetRedMistBattleCardLorIds()
        {
            return new HashSet<LorId>(RedMistBattlePageIds.Select(id => new LorId(LogLikeMod.ModId, id)));
        }

        private static void ResetPrematureRedMistLoadouts(LorId redMistBookId)
        {
            if (LogueBookModels.playerBattleModel == null)
                return;
            for (int index = 0; index < LogueBookModels.playerBattleModel.Count; index++)
            {
                UnitBattleDataModel battleModel = LogueBookModels.playerBattleModel[index];
                if (battleModel?.unitData?.bookItem?.ClassInfo?.id != redMistBookId)
                    continue;
                BookXmlInfo defaultPage = Singleton<BookXmlList>.Instance.GetData(
                    new LorId(LogLikeMod.ModId, -854 - index));
                if (defaultPage != null)
                    LogueBookModels.EquipNewPage(battleModel, defaultPage, false);
            }
        }

        public static void GrantRedMistChallengeVictoryRewards()
        {
            if (RedMistVictoryRewardsGrantedThisBattle || !IsRedMistChallengeStage())
                return;
            bool victory = BattleObjectManager.instance.GetAliveListWithAvailable(Faction.Player).Count > 0
                && BattleObjectManager.instance.GetAliveListWithAvailable(Faction.Enemy).Count == 0;
            if (!victory)
                return;

            RedMistVictoryRewardsGrantedThisBattle = true;
            RecordRedMistChallengeVictory();
            SuppressRedMistChallengeGenericRewards();
            LorId redMistBookId = new LorId(LogLikeMod.ModId, RedMistCorePageId);
            bool corePageAdded = LogueBookModels.TryAddUniqueRoleBookToInventoryAndCompendium(redMistBookId);
            foreach (int pageId in RedMistBattlePageIds)
                LogueBookModels.AddCard(new LorId(LogLikeMod.ModId, pageId), 1, false);
            RMRCore.UnlockBinahAfterRedMistVictory();

            Debug.Log($"[RMRAbnormalityUnlockManager] Red Mist challenge victory: core page {redMistBookId} " +
                      $"{(corePageAdded ? "added" : "already owned")}; battle pages " +
                      $"{string.Join(", ", RedMistBattlePageIds.Select(x => x.ToString()).ToArray())} unlocked.");
        }

        public static void RecordBlackSilenceVictoryUnlock()
        {
            if (BlackSilenceClearRecordedThisBattle || !IsBlackSilenceStage())
                return;
            if (!IsCurrentBattleVictory())
                return;

            BlackSilenceClearRecordedThisBattle = true;
            RMRCore.RecordBlackSilenceStageClear();
            Debug.Log("[RMRAbnormalityUnlockManager] Black Silence clear recorded; Black Silence core page will be granted on Urban Star entry.");
        }

        public static void GrantDistortedEnsembleVictoryRewards()
        {
            if (BlueReverberationRewardsGrantedThisBattle || !IsDistortedEnsembleStage())
                return;
            if (!IsCurrentBattleVictory())
                return;

            BlueReverberationRewardsGrantedThisBattle = true;
            RMRCore.RecordDistortedEnsembleStageClear();
            LorId blueBookId = RMRCore.GetBlueReverberationCorePageLorId();
            bool corePageAdded = RMRCore.PruneLegacyBlueReverberationCorePageUnlocks();
            corePageAdded |= LogueBookModels.TryAddUniqueRoleBookToInventoryAndCompendium(blueBookId);
            foreach (int pageId in BlueReverberationBattlePageIds)
                LogueBookModels.AddCard(new LorId(pageId), 1, false);
            LogueBookModels.SavePermanentCompendiumUnlocks();

            Debug.Log($"[RMRAbnormalityUnlockManager] Distorted Ensemble victory: core page {blueBookId} " +
                      $"{(corePageAdded ? "added" : "already owned")}; battle pages " +
                      $"{string.Join(", ", BlueReverberationBattlePageIds.Select(x => x.ToString()).ToArray())} unlocked; future Urban Star routes will grant these rewards automatically.");
        }

        /// <summary>
        /// After a BOSS clear: realization-exclusive abnormality 3-pick(s) and one E.G.O. 3-pick
        /// from floors the player has already completed in realization.
        /// Grade4–6 (都市恶疾/梦魇/之星): two exclusive abno 3-picks; other grades: one.
        /// Atlas unlock only means "eligible for pools", NOT "already owned this run".
        /// </summary>
        private static void EnqueueBossRealizationTierRewards(ChapterGrade grade)
        {
            LoadRealizationProgress();
            PruneUnselectedRealizationPagesFromRoute();
            HashSet<SephirahType> completedFloors = GetCompletedRealizationFloorsForBossTier(grade);
            if (completedFloors.Count == 0)
            {
                Debug.Log($"[RMRAbnormalityUnlockManager] BOSS realization rewards skipped: grade={grade}, no completed floors in tier {GetTierForChapter(grade)}");
                return;
            }

            // 1) Liberation-exclusive abnormality page(s) — 3-choose-1 each.
            // Urban Plague / Nightmare / Star (G4–G6): two picks; otherwise one.
            int exclusiveAbnoPicks = (grade == ChapterGrade.Grade4
                || grade == ChapterGrade.Grade5
                || grade == ChapterGrade.Grade6) ? 2 : 1;
            int exclusiveAbnoQueued = 0;
            for (int pick = 0; pick < exclusiveAbnoPicks; pick++)
            {
                List<RewardPassiveInfo> exclusiveAbno = RollExclusiveRealizationAbnormalityChoices(completedFloors, 3);
                if (exclusiveAbno != null && exclusiveAbno.Count > 0)
                {
                    LogLikeMod.rewards_passive.Add(new RewardInfo { grade = grade, rewards = exclusiveAbno });
                    exclusiveAbnoQueued++;
                    Debug.Log($"[RMRAbnormalityUnlockManager] BOSS exclusive abno 3-pick #{exclusiveAbnoQueued}/{exclusiveAbnoPicks} enqueued ({exclusiveAbno.Count} options) floors=[{string.Join(",", completedFloors)}]");
                }
                else
                {
                    Debug.LogWarning($"[RMRAbnormalityUnlockManager] BOSS exclusive abno 3-pick #{pick + 1}/{exclusiveAbnoPicks} empty for grade {grade} floors=[{string.Join(",", completedFloors)}]");
                    break;
                }
            }

            // 2) Liberation E.G.O. page — always a single 3-choose-1 (independent ego queue).
            bool egoQueued = EnqueueRealizationEgoSelection(completedFloors);
            Debug.Log($"[RMRAbnormalityUnlockManager] BOSS realization rewards grade={grade}: exclusiveAbnoPicks={exclusiveAbnoQueued}/{exclusiveAbnoPicks}, egoQueued={egoQueued}, floors=[{string.Join(",", completedFloors)}]");
        }

        private static HashSet<SephirahType> GetCompletedRealizationFloorsForBossTier(ChapterGrade grade)
        {
            HashSet<SephirahType> floors = GetFloorsForChapter(grade);
            floors.IntersectWith(CompletedRealizations);
            return floors;
        }

        /// <summary>
        /// True if this run already owns the E.G.O. (picked earlier or already in inventory).
        /// Atlas unlock alone is NOT ownership -- that only opens the reward/shop pool.
        /// </summary>
        #endregion

        #region --- E.G.O. unlocks & mid-battle ego choices ---

        public static bool IsEgoOwnedOnCurrentRoute(LorId id)
        {
            if (id == null || id == LorId.None)
                return true;
            if (IsEgoUnlockedForCurrentRoute(id))
                return true;
            try
            {
                if (LogueBookModels.cardlist != null
                    && LogueBookModels.cardlist.Any(c => c != null && c.GetID() != null
                        && c.GetID().id == id.id
                        && (string.IsNullOrEmpty(c.GetID().packageId) || string.IsNullOrEmpty(id.packageId)
                            || c.GetID().packageId == id.packageId)))
                    return true;
            }
            catch { /* ignore inventory probe failures */ }
            return false;
        }

        /// <summary>
        /// Realization-exclusive abnormality pages only (Snow White / Freisch眉tz final, etc.),
        /// from completed realization floors. Does not use the wide floor pool.
        /// </summary>
        private static List<RewardPassiveInfo> RollExclusiveRealizationAbnormalityChoices(HashSet<SephirahType> floors, int count)
        {
            var candidates = new List<RewardPassiveInfo>();
            foreach (var info in GetAllCreatureRewardPages())
            {
                if (info == null || info.rewardtype != RewardType.Creature)
                    continue;
                if (IsNoAbnormalityFallback(info.id))
                    continue;
                if (!IsRealizationExclusive(info))
                    continue;
                SephirahType floor = ResolveRealizationFloor(info);
                if (floor == SephirahType.None || !floors.Contains(floor))
                    continue;
                // Route ownership only -- atlas unlock is pool eligibility, not run ownership.
                if (RouteUnlockedPages.Exists(id => id == info.id))
                    continue;
                if (LogueBookModels.EmotionCardList != null && LogueBookModels.EmotionCardList.Any(x => x != null && x.id == info.id))
                    continue;
                if (LogLikeMod.rewards_passive != null
                    && LogLikeMod.rewards_passive.Any(reward => reward?.rewards != null && reward.rewards.Any(queued => queued != null && queued.id == info.id)))
                    continue;
                candidates.Add(info);
            }

            var result = new List<RewardPassiveInfo>();
            while (candidates.Count > 0 && result.Count < count)
            {
                int index = UnityEngine.Random.Range(0, candidates.Count);
                result.Add(candidates[index]);
                candidates.RemoveAt(index);
            }
            return result;
        }

        /// <summary>
        /// Enqueues one E.G.O. 3-choose-1 from completed realization floors.
        /// Returns true if a non-empty choice set was queued.
        /// </summary>
        private static bool EnqueueRealizationEgoSelection(HashSet<SephirahType> floors)
        {
            var candidates = new List<LorId>();
            foreach (var floor in floors)
            {
                if (!RealizationEgoCardsByFloor.TryGetValue(floor, out LorId[] egoIds) || egoIds == null)
                    continue;
                foreach (LorId id in egoIds)
                {
                    if (id == null || id == LorId.None)
                        continue;
                    if (IsEgoOwnedOnCurrentRoute(id))
                        continue;
                    // Must resolve a real card XML -- otherwise GetQueuedEgoRewards drops the entry and the whole 3-pick can vanish.
                    DiceCardXmlInfo card = ItemXmlDataList.instance.GetCardItem(id, true)
                        ?? ItemXmlDataList.instance.GetCardItem(id.id, true)
                        ?? ItemXmlDataList.instance.GetCardItem(new LorId(string.Empty, id.id), true);
                    if (card == null)
                        continue;
                    if (LogLikeMod.egoSelectionQueue != null
                        && LogLikeMod.egoSelectionQueue.Any(choice => choice != null && choice.Any(q => q != null && q.id == id.id)))
                        continue;
                    if (!candidates.Any(c => c != null && c.id == id.id))
                        candidates.Add(card.id != null ? card.id : id);
                }
            }

            if (candidates.Count == 0)
            {
                Debug.LogWarning($"[RMRAbnormalityUnlockManager] EnqueueRealizationEgoSelection: no unowned EGO in floors [{string.Join(",", floors)}]");
                return false;
            }

            int pickCount = Math.Min(3, candidates.Count);
            var choiceSet = new List<LorId>();
            for (int i = 0; i < pickCount; i++)
            {
                int index = UnityEngine.Random.Range(0, candidates.Count);
                choiceSet.Add(candidates[index]);
                candidates.RemoveAt(index);
            }

            if (LogLikeMod.egoSelectionQueue == null)
                LogLikeMod.egoSelectionQueue = new List<List<LorId>>();
            LogLikeMod.egoSelectionQueue.Add(choiceSet);

            Debug.Log($"[RMRAbnormalityUnlockManager] Enqueued EGO 3-pick: {choiceSet.Count} candidates (queue size {LogLikeMod.egoSelectionQueue.Count}) ids=[{string.Join(",", choiceSet.Select(x => x.id.ToString()).ToArray())}]");
            return true;
        }
        public static void EnqueueRewardSelections(int count)
        {
            EnqueueRewardSelections(count, LogLikeMod.curchaptergrade);
        }

        public static void EnqueueRewardSelections(int count, ChapterGrade grade)
        {
            for (int i = 0; i < count; i++)
            {
                List<RewardPassiveInfo> choices = RollRewardChoices(grade, 3);
                if (choices.Count > 0)
                    LogLikeMod.rewards_passive.Add(new RewardInfo { grade = grade, rewards = choices });
            }
        }

        public static bool OnEmotionPagePicked(EmotionCardXmlInfo card)
        {
            if (card == null)
                return false;
            RewardPassiveInfo info = GetPassiveInfoFromCard(card);
            if (info == null)
                return false;
            if (IsNoAbnormalityFallback(info.id))
                return true;
            if (info.rewardtype != RewardType.Creature)
                return false;
            if (RewardingModel.rewardFlag == RewardingModel.RewardFlag.PassiveReward)
            {
                UnlockPage(info.id);
                LogueBookModels.RecordCompendiumAbnormalityPage(info.id);
            }
            else if (RewardingModel.rewardFlag == RewardingModel.RewardFlag.EmtoionChoose && !LogueBookModels.selectedEmotion.Contains(info))
            {
                LogueBookModels.selectedEmotion.Add(info);
                // Persist into route pool so later battles this run can re-offer the page.
                UnlockPage(info.id);
                LogueBookModels.RecordCompendiumAbnormalityPage(info.id);
            }
            return true;
        }

        /// <summary>
        /// Resolve vanilla EmotionEgoXmlInfo for a combat-page id (for floor _selectedEgoList / EGO hand).
        /// </summary>
        public static EmotionEgoXmlInfo FindVanillaEmotionEgo(LorId cardId)
        {
            if (cardId == null || cardId == LorId.None || Singleton<EmotionEgoXmlList>.Instance == null)
                return null;
            foreach (var pair in RealizationEgoCardsByFloor)
            {
                List<EmotionEgoXmlInfo> floorEgos = null;
                try { floorEgos = Singleton<EmotionEgoXmlList>.Instance.GetDataList(pair.Key); }
                catch { continue; }
                if (floorEgos == null)
                    continue;
                foreach (EmotionEgoXmlInfo ego in floorEgos)
                {
                    if (ego == null)
                        continue;
                    try
                    {
                        if (ego.CardId != null && ego.CardId.id == cardId.id)
                            return ego;
                        if (pair.Value != null && pair.Value.Any(x => x != null && x.id == cardId.id)
                            && ego.CardId != null && ego.CardId.id == cardId.id)
                            return ego;
                    }
                    catch { /* ignore bad entry */ }
                }
            }
            // Fallback scan: any sephirah list
            try
            {
                foreach (SephirahType floor in System.Enum.GetValues(typeof(SephirahType)))
                {
                    if (floor == SephirahType.None)
                        continue;
                    List<EmotionEgoXmlInfo> list = Singleton<EmotionEgoXmlList>.Instance.GetDataList(floor);
                    if (list == null)
                        continue;
                    foreach (EmotionEgoXmlInfo ego in list)
                    {
                        if (ego?.CardId != null && ego.CardId.id == cardId.id)
                            return ego;
                    }
                }
            }
            catch { /* ignore */ }
            return null;
        }

        /// <summary>
        /// Register a mid-battle EGO pick onto the current floor's selected list so vanilla
        /// ExistEgoCardBySelected / hand toggle treats it like a floor E.G.O. selection.
        /// </summary>
        public static bool RegisterSelectedEgoOnCurrentFloor(LorId cardId)
        {
            if (cardId == null || cardId == LorId.None)
                return false;
            try
            {
                StageLibraryFloorModel floor = Singleton<StageController>.Instance?.GetCurrentStageFloorModel();
                if (floor == null)
                    return false;
                List<EmotionEgoXmlInfo> selected =
                    ModdingUtils.GetFieldValue<List<EmotionEgoXmlInfo>>("_selectedEgoList", floor);
                if (selected == null)
                {
                    selected = new List<EmotionEgoXmlInfo>();
                    try
                    {
                        FieldInfo fi = AccessTools.Field(typeof(StageLibraryFloorModel), "_selectedEgoList");
                        fi?.SetValue(floor, selected);
                    }
                    catch { return false; }
                }
                if (selected.Any(e => e?.CardId != null && e.CardId.id == cardId.id))
                    return true;

                EmotionEgoXmlInfo real = FindVanillaEmotionEgo(cardId);
                if (real == null)
                {
                    // Build a minimal entry so CardId resolves via our Harmony if needed.
                    real = LogLikeMod.AddEmotionEgoForReward(
                        ItemXmlDataList.instance?.GetCardItem(cardId, true)
                        ?? ItemXmlDataList.instance?.GetCardItem(cardId.id, true));
                }
                if (real == null)
                    return false;
                selected.Add(real);
                Debug.Log($"[RMR] Registered floor selected EGO id={cardId.id} (selected count={selected.Count}).");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR] RegisterSelectedEgoOnCurrentFloor: " + ex.Message);
                return false;
            }
        }

        public static void UnlockEgoForCurrentRoute(LorId id)
        {
            // Track all picked EGO for this run (not only realization ids) so personalEgo grant works.
            if (id != null && id != LorId.None)
                RouteUnlockedEgoPages.Add(id);
        }

        /// <summary>All EGO ids unlocked/picked on the current route.</summary>
        public static IEnumerable<LorId> EnumerateRouteUnlockedEgoPages()
        {
            return RouteUnlockedEgoPages;
        }

        public static bool IsEgoUnlockedForCurrentRoute(LorId id)
        {
            return RouteUnlockedEgoPages.Contains(id);
        }

        public static bool HasCompletedAnyRealization()
        {
            return CompletedRealizations != null && CompletedRealizations.Count > 0;
        }

        /// <summary>
        /// Pool of E.G.O. card ids available for mid-battle picks (emotion 3/4/5 after abno).
        /// Normal routes use only route-owned E.G.O.; realization battles use permanent
        /// Compendium ownership. Excludes ids already selected this reception.
        /// </summary>
        public static List<LorId> CollectMidBattleEgoCandidates(HashSet<int> alreadySelectedThisBattle)
        {
            var candidates = new List<LorId>();
            var seen = new HashSet<int>();
            bool usePermanentCompendium = RMRRealizationManager.InRealizationBattle
                || RMRRealizationManager.IsRealizationPreparationActive;
            if (usePermanentCompendium)
                LogueBookModels.EnsureCompendiumUnlocks();
            void Consider(LorId id)
            {
                if (id == null || id == LorId.None)
                    return;
                if (alreadySelectedThisBattle != null && alreadySelectedThisBattle.Contains(id.id))
                    return;
                bool isOwned = usePermanentCompendium
                    ? LogueBookModels.IsCompendiumEgoPageUnlocked(id)
                    : IsEgoOwnedOnCurrentRoute(id);
                if (!isOwned)
                    return;
                if (!seen.Add(id.id))
                    return;
                DiceCardXmlInfo card = ItemXmlDataList.instance?.GetCardItem(id, true)
                    ?? ItemXmlDataList.instance?.GetCardItem(id.id, true)
                    ?? ItemXmlDataList.instance?.GetCardItem(new LorId(string.Empty, id.id), true);
                if (card == null)
                    return;
                candidates.Add(card.id != null ? card.id : id);
            }

            IEnumerable<LorId> ownedEgoPages = usePermanentCompendium
                ? LogueBookModels.CompendiumUnlockedEgoPages
                : RouteUnlockedEgoPages;
            foreach (LorId id in ownedEgoPages)
                Consider(id);

            // Inventory copies (if any) that count as owned EGO.
            try
            {
                if (LogueBookModels.cardlist != null)
                {
                    foreach (DiceCardItemModel item in LogueBookModels.cardlist)
                    {
                        if (item?.ClassInfo == null)
                            continue;
                        if (!RMRPrepareRestrictions.IsEgoCombatPage(item.ClassInfo))
                            continue;
                        Consider(item.GetID() ?? item.ClassInfo.id);
                    }
                }
            }
            catch { /* ignore */ }

            string source = usePermanentCompendium ? "Compendium" : "route";
            Debug.Log($"[RMR] Mid-battle EGO candidates ({source}, owned-only) count={candidates.Count} ids=[{string.Join(",", candidates.Select(x => x.id.ToString()).ToArray())}]");
            return candidates;
        }

        /// <summary>
        /// Random 3-pick (or fewer) from mid-battle EGO pool.
        /// </summary>
        public static List<LorId> RollMidBattleEgoChoiceSet(HashSet<int> alreadySelectedThisBattle)
        {
            List<LorId> candidates = CollectMidBattleEgoCandidates(alreadySelectedThisBattle);
            if (candidates.Count == 0)
                return new List<LorId>();
            int pickCount = Math.Min(3, candidates.Count);
            var choiceSet = new List<LorId>();
            for (int i = 0; i < pickCount; i++)
            {
                int index = UnityEngine.Random.Range(0, candidates.Count);
                choiceSet.Add(candidates[index]);
                candidates.RemoveAt(index);
            }
            return choiceSet;
        }

        public static EmotionCardXmlInfo GetNoAbnormalityFallback(int level)
        {
            int id = NoAbnormalityFallbackBaseId + Math.Max(1, level);
            RewardPassiveInfo info = Singleton<RewardPassivesList>.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, id));
            return info == null ? null : LogLikeMod.GetRegisteredPickUpXml(info);
        }

        #endregion

        #region --- Permanent tier unlocks & no-abno fallback ---

        public static void RecordPermanentClear(ChapterGrade grade)
        {
            int tier = 0;
            if (grade == ChapterGrade.Grade4)
                tier = 1;
            else if (grade == ChapterGrade.Grade5)
                tier = 2;
            else if (grade >= ChapterGrade.Grade6)
                tier = 3;
            if (tier == 0)
                return;

            LoadPermanentProgress();
            if (PermanentlyUnlockedTiers.Add(tier))
                SavePermanentProgress();
        }

        public static bool IsNoAbnormalityFallback(EmotionCardXmlInfo card)
        {
            RewardPassiveInfo info = GetPassiveInfoFromCard(card);
            return info != null && IsNoAbnormalityFallback(info.id);
        }

        public static bool IsNoAbnormalityFallback(LorId id)
        {
            return id.packageId == LogLikeMod.ModId && id.id > NoAbnormalityFallbackBaseId && id.id <= NoAbnormalityFallbackBaseId + 6;
        }

        /// <summary>
        /// Check if a creature/abnormality reward page ID has been unlocked in the current route.
        /// </summary>
        public static bool IsPageUnlocked(LorId id)
        {
            return RouteUnlockedPages.Exists(x => x == id);
        }

        public static void UnlockShopAbnormalityPage(RewardPassiveInfo info)
        {
            if (info == null || info.rewardtype != RewardType.Creature)
                return;
            UnlockPage(info.id);
            LogueBookModels.RecordCompendiumAbnormalityPage(info.id);
        }

        /// <summary>
        /// Returns a copy of the current route's unlocked abnormality page IDs.
        /// Used by atlas sync to record pages obtained during the run into the permanent atlas.
        /// </summary>
        public static List<LorId> GetRouteUnlockedPageIds()
        {
            return new List<LorId>(RouteUnlockedPages);
        }

        /// <summary>
        /// Returns the floor (SephirahType) that a script root belongs to, or Keter if unknown.
        /// </summary>
        #endregion

        #region --- Pool isolation: realization exclusives vs normal reception ---

        public static SephirahType GetFloorForScript(string script)
        {
            string root = GetRootScript(script);
            if (string.IsNullOrEmpty(root)
                || root.IndexOf("RMRNoAbnormality", StringComparison.OrdinalIgnoreCase) >= 0
                || root.IndexOf("BossReward", StringComparison.OrdinalIgnoreCase) >= 0
                || root.IndexOf("Equip", StringComparison.OrdinalIgnoreCase) >= 0)
                return SephirahType.None;
            foreach (var kvp in FloorAbnormalityScripts)
            {
                foreach (string floorRoot in kvp.Value)
                {
                    if (ScriptMatchesRoot(root, floorRoot))
                        return kvp.Key;
                }
            }
            // Non-abno scripts (shop/boss reward wrappers) — silent None, no spam.
            return SephirahType.None;
        }

        /// <summary>
        /// True if this is a final-floor realization exclusive abno page.
        /// Gates: GradeAll exclusive ID block, Unique Level≥5 non-fallback, or exclusive script root.
        /// </summary>
        public static bool IsRealizationExclusive(RewardPassiveInfo info)
        {
            if (info == null || info.rewardtype != RewardType.Creature)
                return false;
            if (IsNoAbnormalityFallback(info.id))
                return false;

            // Hard ID range from CreatureInfo_PickTable GradeAll exclusive block.
            if (info.passiveid >= RealizationExclusivePassiveIdMin
                && info.passiveid <= RealizationExclusivePassiveIdMax)
                return true;

            // XML Level 5–6 Unique final forms (same block; belt-and-suspenders if IDs differ by package).
            if (info.level >= 5
                && info.passiverarity == Rarity.Unique
                && ResolveRealizationFloor(info) != SephirahType.None)
                return true;

            return !string.IsNullOrEmpty(info.script)
                && GetRealizationFloorForScript(info.script) != SephirahType.None;
        }

        /// <summary>
        /// Resolve which floor's final realization gates this exclusive page (script and/or ID).
        /// </summary>
        public static SephirahType ResolveRealizationFloor(RewardPassiveInfo info)
        {
            if (info == null)
                return SephirahType.None;
            SephirahType byScript = GetRealizationFloorForScript(info.script);
            if (byScript != SephirahType.None)
                return byScript;
            return GetRealizationFloorForExclusivePassiveId(info.passiveid);
        }

        /// <summary>
        /// Single gate for ALL normal-reception abno generation paths
        /// (post-battle 3-pick, shop, mystery abno enqueue, mid-battle owned pool, GetCurChapterCreature).
        /// Exclusive pages require the matching floor realization clear; others always pass.
        /// </summary>
        public static bool CanAppearInNormalReceptionAbnoPool(RewardPassiveInfo info)
        {
            if (info == null || info.rewardtype != RewardType.Creature)
                return false;
            if (IsNoAbnormalityFallback(info.id))
                return false;
            return IsRealizationRewardAvailable(info);
        }

        /// <summary>
        /// Returns true if the player has completed the realization battle for the given floor.
        /// </summary>
        public static bool IsFloorRealizationCompleted(SephirahType floor)
        {
            EnsureRealizationProgressLoaded();
            return floor != SephirahType.None && CompletedRealizations.Contains(floor);
        }

        public static void RefreshRealizationProgress()
        {
            LoadRealizationProgress(force: true);
        }

        /// <summary>
        /// Exclusive pages enter the normal/shop/post-battle drop pool only after their floor is cleared.
        /// Non-exclusive pages always pass.
        /// </summary>
        public static bool IsRealizationRewardAvailable(RewardPassiveInfo info)
        {
            if (info == null)
                return false;
            if (!IsRealizationExclusive(info))
                return true;
            EnsureRealizationProgressLoaded();
            SephirahType floor = ResolveRealizationFloor(info);
            if (floor == SephirahType.None)
            {
                // Exclusive by ID/level but unknown floor map → keep locked.
                return false;
            }
            return CompletedRealizations.Contains(floor);
        }

        public static SephirahType GetRealizationFloorForScript(string script)
        {
            if (string.IsNullOrEmpty(script))
                return SephirahType.None;
            foreach (var kvp in RealizationRewardScriptsByFloor)
            {
                foreach (string configuredScript in kvp.Value)
                {
                    if (ScriptMatchesRealizationEntry(script, configuredScript))
                        return kvp.Key;
                }
            }
            return SephirahType.None;
        }

        private static SephirahType GetBossRealizationRewardFloorForScript(string script)
        {
            foreach (var kvp in BossRealizationRewardScriptsByFloor)
            {
                foreach (string configuredScript in kvp.Value)
                {
                    if (ScriptMatchesRealizationEntry(script, configuredScript))
                        return kvp.Key;
                }
            }
            return SephirahType.None;
        }

        public static bool IsRealizationEgoCard(LorId id)
        {
            if (id == null || id == LorId.None)
                return false;
            foreach (var kvp in RealizationEgoCardsByFloor)
            {
                if (kvp.Value == null)
                    continue;
                // Compare by numeric id — packageId may be empty vs null across sources.
                if (kvp.Value.Any(x => x != null && x.id == id.id))
                    return true;
            }
            return false;
        }

        public static bool IsRealizationEgoCardUnlocked(LorId id)
        {
            foreach (var kvp in RealizationEgoCardsByFloor)
            {
                if (kvp.Value.Any(x => x == id))
                    return IsFloorRealizationCompleted(kvp.Key);
            }
            return false;
        }

        public static List<DiceCardXmlInfo> GetUnlockedRealizationEgoCardsForRewards(ChapterGrade grade)
        {
            int tier = GetTierForChapter(grade);
            List<DiceCardXmlInfo> result = new List<DiceCardXmlInfo>();
            foreach (var kvp in RealizationEgoCardsByFloor)
            {
                if (GetTierForFloor(kvp.Key) != tier || !IsFloorRealizationCompleted(kvp.Key))
                    continue;
                foreach (LorId id in kvp.Value)
                {
                    if (IsEgoUnlockedForCurrentRoute(id) || LogueBookModels.IsCompendiumEgoPageUnlocked(id))
                        continue;
                    DiceCardXmlInfo card = ItemXmlDataList.instance.GetCardItem(id, true);
                    if (card != null && !result.Any(x => x.id == card.id))
                        result.Add(card);
                }
            }
            return result;
        }

        public static List<EmotionEgoXmlInfo> GetUnlockedEgoChoicesForBattle(StageLibraryFloorModel floor, int count = 3)
        {
            var selected = floor == null
                ? new List<EmotionEgoXmlInfo>()
                : ModdingUtils.GetFieldValue<List<EmotionEgoXmlInfo>>("_selectedEgoList", floor) ?? new List<EmotionEgoXmlInfo>();
            HashSet<LorId> selectedIds = new HashSet<LorId>(selected.Where(x => x != null).Select(x => x.CardId));
            List<EmotionEgoXmlInfo> candidates = new List<EmotionEgoXmlInfo>();
            bool usePermanentAtlas = RMRRealizationManager.InRealizationBattle
                || RMRRealizationManager.IsRealizationPreparationActive;
            if (usePermanentAtlas)
                LogueBookModels.EnsureCompendiumUnlocks();

            foreach (var pair in RealizationEgoCardsByFloor)
            {
                List<EmotionEgoXmlInfo> floorEgos = Singleton<EmotionEgoXmlList>.Instance.GetDataList(pair.Key);
                if (floorEgos == null)
                    continue;
                foreach (EmotionEgoXmlInfo ego in floorEgos)
                {
                    if (ego == null || !pair.Value.Contains(ego.CardId))
                        continue;
                    bool unlocked = usePermanentAtlas
                        ? LogueBookModels.IsCompendiumEgoPageUnlocked(ego.CardId)
                        : RouteUnlockedEgoPages.Contains(ego.CardId);
                    if (!unlocked || selectedIds.Contains(ego.CardId))
                        continue;
                    if (!candidates.Any(x => x.CardId == ego.CardId))
                        candidates.Add(ego);
                }
            }

            List<EmotionEgoXmlInfo> result = new List<EmotionEgoXmlInfo>();
            while (candidates.Count > 0 && result.Count < count)
            {
                int index = UnityEngine.Random.Range(0, candidates.Count);
                result.Add(candidates[index]);
                candidates.RemoveAt(index);
            }
            return result;
        }

        /// <summary>
        /// Mod reward scripts (PascalCase LogLike names) often differ from vanilla EmotionCard
        /// Script fields (e.g. ScorchedGirl1 → burnninggirl). Without this map,
        /// FindVanillaEmotionCard permanently fails and Name stays as the script key so
        /// EmotionPassiveCardUI.SetTexts looks up the wrong AbnormalityCardDesc → 口口口 / Not found.
        /// </summary>
        private static readonly Dictionary<string, string> ModScriptToVanillaScript =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Scorched Girl
                { "ScorchedGirl1", "burnninggirl" },
                { "ScorchedGirl2", "burnninggirl2" },
                { "ScorchedGirl3", "burnninggirl3" },
                // Bloodbath
                { "BloodBath1", "bloodbath" },
                { "BloodBath2", "bloodbath2" },
                { "BloodBath3", "bloodbath3" },
                // Happy Teddy
                { "HappyTeddyBear1", "teddy" },
                { "HappyTeddyBear2", "teddy2" },
                { "HappyTeddyBear3", "teddy3" },
                // Forsaken Murderer
                { "ForsakenMurderer1", "murderer" },
                { "ForsakenMurderer2", "murderer2" },
                { "ForsakenMurderer3", "murderer3" },
                // Today's Shy Look
                { "ShyLookToday1", "shylook" },
                { "ShyLookToday2", "shylook2" },
                { "ShyLookToday3", "shylook3" },
                // Fragment of the Universe
                { "UniverseZogak1", "fragmentspace" },
                { "UniverseZogak2", "fragmentspace2" },
                { "UniverseZogak3", "fragmentspace3" },
                // Fairy Festival
                { "FairyCarnival1", "fairy1" },
                { "FairyCarnival2", "fairy2" },
                { "FairyCarnival3", "fairy3" },
                // Child of the Galaxy
                { "ChildofGalaxy1", "galaxychild" },
                { "ChildofGalaxy2", "galaxychild2" },
                { "ChildofGalaxy3", "galaxychild3" },
                // Heart of Aspiration (3rd page is vanilla "doki", not heart*)
                { "HeartofAspiration1", "heart" },
                { "HeartofAspiration2", "heart_rush" },
                { "HeartofAspiration3", "doki" },
                // Little Helper
                { "LittleHelper1", "helper" },
                { "LittleHelper2", "helper2" },
                { "LittleHelper3", "helper3" },
                // Red Shoes
                { "RedShoes1", "redshoes" },
                { "RedShoes2", "redshoes2" },
                { "RedShoes3", "redshoes3" },
                // Laetitia (vanilla spelling latitia)
                { "Laetitia1", "latitia1" },
                { "Laetitia2", "latitia2" },
                { "Laetitia3", "latitia3" },
                // King of Greed
                { "Greed1", "kingofgreed1" },
                { "Greed2", "kingofgreed2" },
                { "Greed3", "kingofgreed3" },
                // Servant of Wrath
                { "Angry1", "servantofwrath1" },
                { "Angry2", "servantofwrath2" },
                { "Angry3", "servantofwrath3" },
                // The Road Home / House
                { "House1", "waybackhome1" },
                { "House2", "waybackhome2" },
                { "House3", "waybackhome3" },
                // Spider Bud
                { "SpiderBud1", "spiderbud" },
                { "SpiderBud2", "spiderbud2" },
                { "SpiderBud3", "spiderbud3" },
                // Singing Machine
                { "SingingMachine1", "singingmachine" },
                { "SingingMachine2", "singingmachine2" },
                { "SingingMachine3", "singingmachine3" },
                // Porccubus
                { "Porccubus1", "porccubus" },
                { "Porccubus2", "porccubus2" },
                { "Porccubus3", "porccubus3" },
                // Snow Queen
                { "TheSnowQueen1", "snowqueen1" },
                { "TheSnowQueen2", "snowqueen2" },
                { "TheSnowQueen3", "snowqueen3" },
                // Mountain of Smiling Bodies (vanilla script root is danggocreature)
                { "Mountain1", "danggocreature1" },
                { "Mountain2", "danggocreature2" },
                { "Mountain3", "danggocreature3" },
                // Time / Clock (vanilla script root is silence)
                { "Clock1", "silence1" },
                { "Clock2", "silence2" },
                { "Clock3", "silence3" },
                // Quiet Kid (case only, listed for clarity)
                { "quietKidHammer", "quietkidhammer" },
                { "quietKidEyeShine", "quietkideyeshine" },
                { "quietKidGuilty", "quietkidguilty" },
            };

        /// <summary>
        /// Candidate vanilla Script values to try when resolving a mod reward script.
        /// </summary>
        public static IEnumerable<string> GetVanillaScriptCandidates(string script)
        {
            if (string.IsNullOrEmpty(script))
                yield break;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<string>();
            void Add(string s)
            {
                if (string.IsNullOrEmpty(s) || !seen.Add(s))
                    return;
                list.Add(s);
            }

            Add(script);
            if (ModScriptToVanillaScript.TryGetValue(script, out string mapped))
                Add(mapped);

            string root = GetRootScript(script);
            string digits = script.Length > root.Length ? script.Substring(root.Length) : string.Empty;
            if (ModScriptToVanillaScript.TryGetValue(root + "1", out string mappedFirst)
                && (string.IsNullOrEmpty(digits) || digits == "1"))
                Add(mappedFirst);
            if (!string.IsNullOrEmpty(digits) && ModScriptToVanillaScript.TryGetValue(root + digits, out string mappedN))
                Add(mappedN);

            // Case-only variants (QueenBee1 ↔ queenbee1).
            Add(script.ToLowerInvariant());
            if (!string.IsNullOrEmpty(root))
            {
                // A numbered script is a distinct abnormality page. Falling back from
                // SingingMachine2/3 to the unnumbered root resolves both to Music and
                // cross-pairs its artwork with Rhythm/Musical Addiction.
                if (string.IsNullOrEmpty(digits))
                    Add(root.ToLowerInvariant());
                if (!string.IsNullOrEmpty(digits))
                    Add(root.ToLowerInvariant() + digits);
            }

            foreach (string s in list)
                yield return s;
        }

        private static bool VanillaScriptMatchesMod(string vanillaScript, string modScript)
        {
            if (string.IsNullOrEmpty(vanillaScript) || string.IsNullOrEmpty(modScript))
                return false;
            if (ScriptMatchesRealizationEntry(vanillaScript, modScript))
                return true;
            foreach (string candidate in GetVanillaScriptCandidates(modScript))
            {
                if (string.Equals(vanillaScript, candidate, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Resolve the vanilla EmotionCardXmlInfo for a reward script (e.g. snowwhite1 →
        /// Name "SnowWhite_Vine", ScorchedGirl1 → burnninggirl Name). Vanilla AbnormalityCards
        /// localization is keyed by Name, not by Script, so callers that need desc text must use this Name.
        /// </summary>
        public static EmotionCardXmlInfo FindVanillaEmotionCard(string script)
        {
            if (string.IsNullOrEmpty(script))
                return null;
            SephirahType floor = GetRealizationFloorForScript(script);
            if (floor == SephirahType.None)
                floor = GetFloorForScript(script);
            if (floor != SephirahType.None)
            {
                EmotionCardXmlInfo onFloor = FindVanillaEmotionCardOnFloor(floor, script);
                if (onFloor != null)
                    return onFloor;
            }
            // Scan all mapped floors (script roots not in the floor map / wrong Level filter).
            foreach (SephirahType candidateFloor in FloorAbnormalityScripts.Keys)
            {
                if (candidateFloor == floor)
                    continue;
                EmotionCardXmlInfo found = FindVanillaEmotionCardOnFloor(candidateFloor, script);
                if (found != null)
                    return found;
            }
            // Last resort: raw _list scan (works once EmotionCardXmlList is fully loaded).
            return FindVanillaEmotionCardScanAll(script);
        }

        private static EmotionCardXmlInfo FindVanillaEmotionCardOnFloor(SephirahType floor, string script)
        {
            EmotionCardXmlList list = Singleton<EmotionCardXmlList>.Instance;
            if (list == null || string.IsNullOrEmpty(script))
                return null;

            for (int level = 1; level <= 6; level++)
            {
                List<EmotionCardXmlInfo> cards = null;
                try { cards = list.GetDataListByLevel(floor, level); } catch { continue; }
                if (cards == null)
                    continue;
                EmotionCardXmlInfo match = cards.FirstOrDefault(card =>
                    card != null
                    && card.Script != null
                    && card.Script.Any(s => VanillaScriptMatchesMod(s, script)));
                if (match != null)
                    return match;
            }
            return null;
        }

        /// <summary>
        /// Full _list scan when floor/level filters miss (early init, or Level field mismatch).
        /// </summary>
        private static EmotionCardXmlInfo FindVanillaEmotionCardScanAll(string script)
        {
            if (string.IsNullOrEmpty(script))
                return null;
            try
            {
                EmotionCardXmlList list = Singleton<EmotionCardXmlList>.Instance;
                if (list == null)
                    return null;
                FieldInfo fi = AccessTools.Field(typeof(EmotionCardXmlList), "_list");
                var all = fi?.GetValue(list) as System.Collections.IList;
                if (all == null)
                    return null;
                foreach (object o in all)
                {
                    EmotionCardXmlInfo card = o as EmotionCardXmlInfo;
                    if (card?.Script == null)
                        continue;
                    if (card.Script.Any(s => VanillaScriptMatchesMod(s, script)))
                        return card;
                }
            }
            catch { /* ignore */ }
            return null;
        }

        /// <summary>
        /// True when the virtual card still uses the registration default Name (= script),
        /// so EmotionPassiveCardUI would look up the wrong AbnormalityCardDesc key.
        /// </summary>
        public static bool NeedsVanillaEmotionPresentation(RewardPassiveInfo info, EmotionCardXmlInfo virtualCard)
        {
            if (info == null || virtualCard == null || info.rewardtype != RewardType.Creature)
                return false;
            if (string.IsNullOrEmpty(info.script))
                return false;
            // Fallback / non-vanilla pages keep script Name intentionally.
            if (string.Equals(info.script, "RMRNoAbnormality", StringComparison.OrdinalIgnoreCase))
                return false;
            if (string.IsNullOrEmpty(virtualCard.Name))
                return true;
            if (string.Equals(virtualCard.Name, info.script, StringComparison.OrdinalIgnoreCase))
                return true;
            if (virtualCard.Script != null && virtualCard.Script.Count > 0
                && string.Equals(virtualCard.Name, virtualCard.Script[0], StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        /// <summary>
        /// Lazy re-apply: RegisterPickUpXml often runs before EmotionCardXmlList is populated,
        /// so ApplyVanillaEmotionPresentation no-ops at init. Call again at emotion-pick / reward time.
        /// </summary>
        public static bool EnsureVanillaEmotionPresentation(RewardPassiveInfo info, EmotionCardXmlInfo virtualCard)
        {
            if (!NeedsVanillaEmotionPresentation(info, virtualCard))
                return false;
            string nameBefore = virtualCard.Name;
            ApplyVanillaEmotionPresentation(info, virtualCard);
            return !string.Equals(nameBefore, virtualCard.Name, StringComparison.Ordinal);
        }

        /// <summary>
        /// Re-apply vanilla Name/artwork for every registered Creature reward whose presentation
        /// was missing at init. Safe to call after EmotionCardXmlList / locale load.
        /// Does not touch EquipPage rewards.
        /// </summary>
        public static void RefreshAllCreatureEmotionPresentation()
        {
            try
            {
                if (LogLikeMod.PickUpXml_Dummy_Passive == null
                    || Singleton<RewardPassivesList>.Instance?.infos == null)
                    return;
                int fixedCount = 0;
                foreach (RewardPassivesInfo group in Singleton<RewardPassivesList>.Instance.infos)
                {
                    if (group?.RewardPassiveList == null)
                        continue;
                    foreach (RewardPassiveInfo info in group.RewardPassiveList)
                    {
                        if (info == null || info.rewardtype != RewardType.Creature)
                            continue;
                        EmotionCardXmlInfo card = LogLikeMod.GetRegisteredPickUpXml(info);
                        if (card == null)
                            continue;
                        if (EnsureVanillaEmotionPresentation(info, card))
                            fixedCount++;
                    }
                }
                if (fixedCount > 0)
                    Debug.Log($"[RMRAbnormalityUnlockManager] Lazy-applied vanilla emotion presentation for {fixedCount} creature page(s).");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMRAbnormalityUnlockManager] RefreshAllCreatureEmotionPresentation failed: " + ex.Message);
            }
        }

        public static void ApplyVanillaEmotionPresentation(RewardPassiveInfo info, EmotionCardXmlInfo virtualCard)
        {
            if (info == null || virtualCard == null || info.rewardtype != RewardType.Creature)
                return;

            EmotionCardXmlInfo vanillaCard = FindVanillaEmotionCard(info.script)
                ?? FindVanillaEmotionCardScanAll(info.script);
            if (vanillaCard == null)
            {
                // Only warn once per script to avoid init spam when EmotionCardXmlList is still empty.
                // Mod fallback page has no vanilla entry — silence spam.
                if (info.script != null
                    && info.script.IndexOf("RMRNoAbnormality", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                if (_presentationMissLogged == null)
                    _presentationMissLogged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (_presentationMissLogged.Add(info.script ?? ""))
                    Debug.LogWarning($"[RMRAbnormalityUnlockManager] Vanilla emotion presentation not found for {info.script}.");
                return;
            }

            // Critical: UI EmotionPassiveCardUI.SetTexts looks up AbnormalityCardDesc by Name.
            // Vanilla localization IDs are fancy names (SnowWhite_Vine), not scripts (snowwhite1).
            virtualCard.Name = vanillaCard.Name;
            virtualCard._artwork = vanillaCard.Artwork;
            virtualCard.Sephirah = vanillaCard.Sephirah;
            virtualCard.State = vanillaCard.State;
        }

        private static HashSet<string> _presentationMissLogged;
        /// <summary>
        /// Mark a floor realization as completed (first clear only). Unlocks the floor's
        /// realization reward pool and records exclusive abnormality + E.G.O. pages into
        /// the permanent atlas. Does not grant pages to the current route inventory.
        /// </summary>
        public static void CompleteFloorRealization(SephirahType floor)
        {
            EnsureRealizationProgressLoaded();
            if (!CompletedRealizations.Add(floor))
                return;

            SaveRealizationProgress();
            _realizationProgressLoaded = true;

            foreach (RewardPassiveInfo info in GetAllCreatureRewardPages())
            {
                if (info == null || !IsRealizationExclusive(info))
                    continue;
                if (ResolveRealizationFloor(info) != floor)
                    continue;
                LogueBookModels.RecordCompendiumAbnormalityPage(info.id);
            }

            if (RealizationEgoCardsByFloor.TryGetValue(floor, out LorId[] egos) && egos != null)
            {
                foreach (LorId egoId in egos)
                    LogueBookModels.RecordCompendiumEgoPage(egoId);
            }

            LogueBookModels.SavePermanentCompendiumData();
            Debug.Log($"[RMRAbnormalityUnlockManager] Floor realization first clear recorded to atlas: {floor}");
        }

        /// <summary>
        /// Returns the set of floors whose pages are available for the given chapter grade.
        /// Grade 1-3 -> Malkuth/Yesod/Hod/Netzach (early floors)
        /// Grade 4-5 -> Tiphereth/Gebura/Chesed (mid floors)
        /// Grade 6-7 -> Binah/Hokma/Keter (late floors)
        /// </summary>
        public static HashSet<SephirahType> GetFloorsForChapter(ChapterGrade grade)
        {
            if (grade <= ChapterGrade.Grade3)
                return new HashSet<SephirahType> { SephirahType.Malkuth, SephirahType.Yesod, SephirahType.Hod, SephirahType.Netzach };
            if (grade <= ChapterGrade.Grade5)
                return new HashSet<SephirahType> { SephirahType.Tiphereth, SephirahType.Gebura, SephirahType.Chesed };
            return new HashSet<SephirahType> { SephirahType.Binah, SephirahType.Hokma, SephirahType.Keter };
        }

        private static void UnlockPage(LorId id)
        {
            if (id == LorId.None || IsNoAbnormalityFallback(id))
                return;
            if (!RouteUnlockedPages.Exists(x => x == id))
                RouteUnlockedPages.Add(id);
        }

        private static List<RewardPassiveInfo> RollRewardChoices(ChapterGrade grade, int count)
        {
            List<RewardPassiveInfo> candidates = GetRewardCandidates(grade);
            List<RewardPassiveInfo> result = new List<RewardPassiveInfo>();
            while (candidates.Count > 0 && result.Count < count)
            {
                int index = UnityEngine.Random.Range(0, candidates.Count);
                result.Add(candidates[index]);
                candidates.RemoveAt(index);
            }
            return result;
        }

        private static List<RewardPassiveInfo> GetRewardCandidates(ChapterGrade grade)
        {
            EnsureRealizationProgressLoaded();
            var candidates = GetAllCreatureRewardPages()
                .Where(CanAppearInNormalReceptionAbnoPool)
                // Final-floor exclusives never enter the normal 3-pick until that floor is cleared
                // (also enforced inside CanAppearInNormalReceptionAbnoPool / GetTierForRewardPage).
                .Where(info => IsRewardTierAvailableForChapter(GetTierForRewardPage(info), grade))
                .Where(info => !RouteUnlockedPages.Exists(id => id == info.id))
                .Where(info => LogueBookModels.EmotionCardList == null || !LogueBookModels.EmotionCardList.Any(x => x.id == info.id))
                .ToList();

            // Diagnostics: never silently include exclusives when the floor is incomplete.
            int lockedExclusiveSkipped = GetAllCreatureRewardPages().Count(info =>
                info != null && IsRealizationExclusive(info) && !IsRealizationRewardAvailable(info));
            Debug.Log($"[RMRAbnormalityUnlockManager] Normal abno pool grade={grade} candidates={candidates.Count} lockedExclusivesExcluded={lockedExclusiveSkipped} clears=[{string.Join(",", CompletedRealizations)}]");
            return candidates;
        }

        private static IEnumerable<RewardPassiveInfo> GetPermanentStartingPages()
        {
            foreach (int tier in PermanentlyUnlockedTiers)
            {
                foreach (RewardPassiveInfo info in GetAllCreatureRewardPages().Where(x => GetTierForScript(x.script) == tier && x.level >= 4 && !IsRealizationExclusive(x)))
                    yield return info;
            }
        }

        public static void PruneUnselectedRealizationPagesFromRoute()
        {
            Predicate<LorId> shouldRemove = id =>
            {
                RewardPassiveInfo info = Singleton<RewardPassivesList>.Instance.GetPassiveInfo(id);
                return info != null
                    && IsRealizationExclusive(info)
                    && !LogueBookModels.IsCompendiumAbnormalityPageUnlocked(id);
            };
            RouteUnlockedPages.RemoveAll(shouldRemove);
            if (LogueBookModels.EmotionCardList != null)
                LogueBookModels.EmotionCardList.RemoveAll(info => info != null && shouldRemove(info.id));
        }

        public static List<RewardPassiveInfo> GetShopEligibleAbnormalityPages(ChapterGrade grade)
        {
            EnsureRealizationProgressLoaded();
            return GetAllCreatureRewardPages()
                .Where(CanAppearInNormalReceptionAbnoPool)
                .Where(info => IsRewardTierAvailableForChapter(GetTierForRewardPage(info), grade))
                .Where(info => LogueBookModels.EmotionCardList == null || !LogueBookModels.EmotionCardList.Any(x => x.id == info.id))
                .Where(info => !RouteUnlockedPages.Exists(id => id == info.id))
                .ToList();
        }

        private static List<RewardPassiveInfo> GetAllCreatureRewardPages()
        {
            List<RewardPassiveInfo> result = new List<RewardPassiveInfo>();
            foreach (RewardPassivesInfo info in Singleton<RewardPassivesList>.Instance.infos.Where(x => x.rewardtype == PassiveRewardListType.Custom))
            {
                if (info.RewardPassiveList == null)
                    continue;
                result.AddRange(info.RewardPassiveList.Where(x => x.rewardtype == RewardType.Creature));
            }
            return result;
        }

        private static RewardPassiveInfo GetPassiveInfoFromCard(EmotionCardXmlInfo card)
        {
            if (card == null)
                return null;
            return Singleton<RewardPassivesList>.Instance.GetPassiveInfo(new LorId(LogLikeMod.GetPickUpXmlWorkShopId_Passive(card), card.id));
        }

        public static int GetRewardTierForCurrentChapter()
        {
            return GetTierForChapter(LogLikeMod.curchaptergrade);
        }


        public static bool IsRewardTierAvailableForChapter(int rewardTier, ChapterGrade grade)
        {
            int currentTier = GetTierForChapter(grade);
            return rewardTier > 0 && rewardTier <= currentTier;
        }

        public static float GetNormalAbnormalityRewardChance(ChapterGrade grade)
        {
            if (grade == ChapterGrade.Grade3)
                return UrbanLegendNormalAbnormalityRewardChance;
            if (grade == ChapterGrade.Grade4)
                return UrbanPlagueNormalAbnormalityRewardChance;
            return 1f;
        }

        public static bool ShouldEnqueueNormalAbnormalityReward(ChapterGrade grade)
        {
            float chance = GetNormalAbnormalityRewardChance(grade);
            return chance >= 1f || UnityEngine.Random.value < chance;
        }
        public static int GetTierForChapter(ChapterGrade grade)
        {
            if (grade <= ChapterGrade.Grade3)
                return 1;
            if (grade <= ChapterGrade.Grade5)
                return 2;
            return 3;
        }

        public static int GetTierForFloor(SephirahType floor)
        {
            if (floor == SephirahType.Malkuth || floor == SephirahType.Yesod || floor == SephirahType.Hod || floor == SephirahType.Netzach)
                return 1;
            if (floor == SephirahType.Tiphereth || floor == SephirahType.Gebura || floor == SephirahType.Chesed)
                return 2;
            if (floor == SephirahType.Binah || floor == SephirahType.Hokma || floor == SephirahType.Keter)
                return 3;
            return 0;
        }

        public static int GetTierForScript(string script)
        {
            // Locked realization exclusives are tier 0 so any tier-only gate also rejects them.
            EnsureRealizationProgressLoaded();
            SephirahType exclusiveFloor = GetRealizationFloorForScript(script);
            if (exclusiveFloor != SephirahType.None && !IsFloorRealizationCompleted(exclusiveFloor))
                return 0;

            SephirahType floor = GetFloorForScript(script);
            if (floor == SephirahType.Malkuth || floor == SephirahType.Yesod || floor == SephirahType.Hod || floor == SephirahType.Netzach)
                return 1;
            if (floor == SephirahType.Tiphereth || floor == SephirahType.Gebura || floor == SephirahType.Chesed)
                return 2;
            if (floor == SephirahType.Binah || floor == SephirahType.Hokma || floor == SephirahType.Keter)
                return 3;
            return 0;
        }

        /// <summary>
        /// Chapter-pool tier for a full reward row (script + exclusive ID). Prefer this over script-only
        /// when gating shop/post-battle candidates so ID-range exclusives cannot slip through.
        /// </summary>
        public static int GetTierForRewardPage(RewardPassiveInfo info)
        {
            if (info == null)
                return 0;
            if (IsRealizationExclusive(info) && !IsRealizationRewardAvailable(info))
                return 0;
            int byScript = GetTierForScript(info.script);
            if (byScript > 0)
                return byScript;
            // Exclusive with floor from ID map but weak script → use floor chapter tier.
            SephirahType floor = ResolveRealizationFloor(info);
            return floor != SephirahType.None ? GetTierForFloor(floor) : 0;
        }

        private static bool ScriptMatchesRoot(string scriptRoot, string floorRoot)
        {
            if (string.IsNullOrEmpty(scriptRoot) || string.IsNullOrEmpty(floorRoot))
                return false;
            return string.Equals(scriptRoot, floorRoot, StringComparison.OrdinalIgnoreCase)
                   || scriptRoot.StartsWith(floorRoot, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ScriptMatchesRealizationEntry(string script, string configuredScript)
        {
            if (string.IsNullOrEmpty(script) || string.IsNullOrEmpty(configuredScript))
                return false;
            if (string.Equals(script, configuredScript, StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(GetRootScript(script), configuredScript, StringComparison.OrdinalIgnoreCase))
                return true;
            // Prefix match only when the remainder is empty, pure digits (freischutz3),
            // or quietKid* letter suffixes (quietKidHammer / EyeShine / Guilty).
            // Avoid treating arbitrary "wizardFoo" as the Chesed exclusive root.
            if (!script.StartsWith(configuredScript, StringComparison.OrdinalIgnoreCase))
                return false;
            string rest = script.Substring(configuredScript.Length);
            if (rest.Length == 0)
                return true;
            bool allDigits = true;
            for (int i = 0; i < rest.Length; i++)
            {
                if (!char.IsDigit(rest[i]))
                {
                    allDigits = false;
                    break;
                }
            }
            if (allDigits)
                return true;
            if (string.Equals(configuredScript, "quietKid", StringComparison.OrdinalIgnoreCase)
                && rest.Length > 0
                && char.IsLetter(rest[0]))
                return true;
            return false;
        }

        private static string GetRootScript(string script)
        {
            if (string.IsNullOrEmpty(script))
                return string.Empty;
            int index = script.Length - 1;
            while (index >= 0 && char.IsDigit(script[index]))
                index--;
            return script.Substring(0, index + 1);
        }

        private static void LoadPermanentProgress()
        {
            PermanentlyUnlockedTiers.Clear();
            SaveData data = Singleton<LogueSaveManager>.Instance.LoadData(ProgressSaveName);
            if (data == null)
                return;
            for (int tier = 1; tier <= 3; tier++)
            {
                if (data.GetInt("Tier" + tier) > 0)
                    PermanentlyUnlockedTiers.Add(tier);
            }
        }

        private static void SavePermanentProgress()
        {
            SaveData data = new SaveData(SaveDataType.Dictionary);
            for (int tier = 1; tier <= 3; tier++)
                data.AddData("Tier" + tier, PermanentlyUnlockedTiers.Contains(tier) ? 1 : 0);
            Singleton<LogueSaveManager>.Instance.SaveData(data, ProgressSaveName);
        }

        private static bool _realizationProgressLoaded;

        private static void EnsureRealizationProgressLoaded()
        {
            if (!_realizationProgressLoaded)
                LoadRealizationProgress(force: false);
        }

        private static void LoadRealizationProgress(bool force = true)
        {
            if (_realizationProgressLoaded && !force)
                return;
            CompletedRealizations.Clear();
            SaveData data = null;
            try { data = Singleton<LogueSaveManager>.Instance.LoadData(RealizationSaveName); }
            catch { data = null; }
            if (data != null)
            {
                foreach (SephirahType floor in Enum.GetValues(typeof(SephirahType)))
                {
                    if (floor == SephirahType.None)
                        continue;
                    try
                    {
                        if (data.GetData(floor.ToString()) != null && data.GetInt(floor.ToString()) > 0)
                            CompletedRealizations.Add(floor);
                    }
                    catch { /* ignore bad enum keys */ }
                }
            }
            _realizationProgressLoaded = true;
        }

        private static void SaveRealizationProgress()
        {
            SaveData data = new SaveData(SaveDataType.Dictionary);
            foreach (SephirahType floor in Enum.GetValues(typeof(SephirahType)))
            {
                if (floor == SephirahType.None)
                    continue;
                data.AddData(floor.ToString(), CompletedRealizations.Contains(floor) ? 1 : 0);
            }
            Singleton<LogueSaveManager>.Instance.SaveData(data, RealizationSaveName);
            _realizationProgressLoaded = true;
        }

        private static void DeleteSaveFile(string saveName)
        {
            try
            {
                string path = Path.Combine(LogueSaveManager.Saveroot, saveName);
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }

        #endregion
    }

    /// <summary>
    /// Placeholder pickup when no unlocked abnormality is available at the current emotion tier.
    /// </summary>
    public class PickUpModel_RMRNoAbnormality : CreaturePickUpModel
    {
        public PickUpModel_RMRNoAbnormality()
        {
            // Language-aware strings (fallback to EN if not Chinese).
            string lang = "";
            try { lang = TextDataModel.CurrentLanguage.ToString().ToLowerInvariant(); } catch { }
            bool zh = lang.Contains("cn") || lang.Contains("ch") || lang.Contains("zh");
            bool kr = lang.Contains("kr") || lang.Contains("ko");
            if (zh)
            {
                this.Name = "\u65e0"; // 无
                this.Desc = "\u5f53\u524d\u60c5\u611f\u7b49\u7ea7\u6ca1\u6709\u5df2\u89e3\u9501\u7684\u5f02\u60f3\u4f53\u4e66\u9875\u3002";
                this.FlaverText = "\u8fd9\u4e00\u6b21\uff0c\u5149\u6ca1\u6709\u56de\u5e94\u3002";
            }
            else if (kr)
            {
                this.Name = "\uc5c6\uc74c"; // 없음
                this.Desc = "\ud604\uc7ac \uac10\uc815 \ub4f1\uae09\uc5d0\uc11c \ud574\uae08\ub41c \ud658\uc0c1\uccb4 \ud398\uc774\uc9c0\uac00 \uc5c6\uc2b5\ub2c8\ub2e4.";
                this.FlaverText = "\uc774\ubc88\uc5d0\ub294 \ube5b\uc774 \uc751\ub2f5\ud558\uc9c0 \uc54a\uc558\uc2b5\ub2c8\ub2e4.";
            }
            else
            {
                this.Name = "None";
                this.Desc = "No unlocked abnormality pages are available at this Emotion level.";
                this.FlaverText = "This time, the Light does not answer.";
            }
            this.ArtWork = "Stage_Rest";
        }

        public override void OnPickUp()
        {
        }

        public override void OnPickUp(BattleUnitModel model)
        {
        }
    }
}
