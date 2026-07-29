// =============================================================================
// LogueBookModels - run inventory, party, stages, and permanent Compendium unlocks.
//
// Product term: Compendium (CN 图鉴 / KR 도감). C# names use Compendium*.
// Disk filename RMR_AtlasPermanentUnlocks is intentional (save compatibility).
// Some methods still contain "Atlas" in the identifier (e.g. SyncCurrentInventoryToPermanentAtlas).
// See docs/localization/CODE_TERMINOLOGY.md
// =============================================================================
using GameSave;
using HarmonyLib;
using LOR_DiceSystem;
using LOR_XML;
using RogueLike_Mod_Reborn;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using static CharacterSound;

namespace abcdcode_LOGLIKE_MOD
{

    /// <summary>
    /// Static run state for key pages, combat pages, party units, and permanent Compendium unlock sets.
    /// </summary>
    public class LogueBookModels
    {
        #region --- Run state fields (party / inventory / stages) ---

        public static BookXmlInfo BaseXmlInfo;
        public static BookEquipEffect BaseEquipStat;
        public static Dictionary<int, int> EmotionSelectDic;
        public static List<RewardPassiveInfo> selectedEmotion;
        public static List<RewardPassiveInfo> EmotionCardList;
        public static Dictionary<ChapterGrade, List<LogueStageInfo>> RemainStageList;
        public static List<BookModel> booklist;
        public static List<DiceCardItemModel> cardlist;
        public static List<UnitDataModel> playerModel;
        public static Dictionary<UnitDataModel, List<LorId>> playersPick;
        public static Dictionary<UnitDataModel, List<LorId>> playersperpassives;
        public static Dictionary<UnitDataModel, List<LogStatAdder>> playersstatadders;
        public static List<LorId> shopPick;
        public static int shopUpgradeCardPrice;
        public static List<UnitBattleDataModel> playerBattleModel;
        public static int nextinstanceid;
        public static HashSet<LorId> CompendiumUnlockedRoleBooks;
        public static HashSet<LorId> CompendiumUnlockedBattleCards;
        public static HashSet<LorId> CompendiumUnlockedAbnormalityPages;
        public static HashSet<LorId> CompendiumUnlockedEgoPages;
        // Compendium = CN 图鉴. Disk filename RMR_AtlasPermanentUnlocks kept for save compatibility.
        private const string PermanentCompendiumSaveName = "RMR_AtlasPermanentUnlocks";
        /// <summary>
        /// True after permanent Compendium has been loaded from disk (or intentionally cleared by reset).
        /// Prevents SavePermanentCompendiumData from writing empty in-memory sets over a real save
        /// when the hub Compendium is opened before CreatePlayer/run init.
        /// </summary>
        private static bool PermanentCompendiumHydrated;
        /// <summary>When true, SavePermanentCompendiumData skips disk-merge (used by full archive reset).</summary>
        private static bool SuppressPermanentAtlasMergeOnSave;
        public static Dictionary<UnitDataModel, LorId> Grade6SpecialBuiltInDeckSource = new Dictionary<UnitDataModel, LorId>();

        /// <summary>
        /// Degraded Binah combat page ids that have been upgraded to full (non-degraded) versions this run.
        /// Keys are degraded card numeric ids (607201-607205).
        /// </summary>
        public static HashSet<int> BinahUpgradedDegradedCardIds = new HashSet<int>();

        /// <summary>Vanilla mapping: degraded Binah pages → full Arbiter pages.</summary>
        private static readonly Dictionary<int, int> BinahDegradedToFullCardIds = new Dictionary<int, int>
        {
            { 607201, 706201 }, // Lock
            { 607202, 706202 }, // Fairy
            { 607203, 706203 }, // Shockwave
            { 607204, 706204 }, // Pillar
            { 607205, 706205 }, // Chain
        };

        public static BookModel LoadFromSaveData_BookModel(SaveData data)
        {
            LorId savedId = RMRCore.NormalizeLegacyBlueReverberationCorePageId(ExtensionUtils.LogLoadFromSaveData(data.GetData("id")));
            BookXmlInfo bookXml = RewardingModel.GetBookDataOriginAware(savedId) ?? Singleton<BookXmlList>.Instance.GetData(savedId);
            if (bookXml == null)
            {
                Debug.LogWarning($"[RMR Atlas] Missing saved core page {savedId}; falling back to starter page.");
                bookXml = LogueBookModels.BaseXmlInfo;
            }
            BookModel bookModel = new BookModel(bookXml);
            int index = data.GetInt("index");
            if (index != -1)
                bookModel.owner = LogueBookModels.playerModel[index];
            bookModel.instanceId = data.GetInt("instanceid");
            bookModel.originData.equipedPassiveBookInstanceId = data.GetInt("equippedbookid");
            bookModel.TryGainUniquePassive();
            return bookModel;
        }

        public static SaveData GetSaveData_BookModel(BookModel model)
        {
            SaveData data = new SaveData();
            data.AddData("id", model.ClassInfo.id.LogGetSaveData());
            if (model.owner != null)
            {
                int num = LogueBookModels.playerModel.IndexOf(model.owner);
                data.AddData("index", new SaveData(num));
            }
            else
                data.AddData("index", new SaveData(-1));
            data.AddData("instanceid", model.instanceId);
            data.AddData("equippedbookid", model.originData.equipedPassiveBookInstanceId);
            return data;
        }

        #endregion

        #region --- Permanent Compendium unlocks (disk: RMR_AtlasPermanentUnlocks) ---

        public static void EnsureCompendiumUnlocks()
        {
            if (LogueBookModels.CompendiumUnlockedRoleBooks == null)
                LogueBookModels.CompendiumUnlockedRoleBooks = new HashSet<LorId>();
            if (LogueBookModels.CompendiumUnlockedBattleCards == null)
                LogueBookModels.CompendiumUnlockedBattleCards = new HashSet<LorId>();
            if (LogueBookModels.CompendiumUnlockedAbnormalityPages == null)
                LogueBookModels.CompendiumUnlockedAbnormalityPages = new HashSet<LorId>();
            if (LogueBookModels.CompendiumUnlockedEgoPages == null)
                LogueBookModels.CompendiumUnlockedEgoPages = new HashSet<LorId>();
        }

        public static void RecordCompendiumRoleBook(LorId id)
        {
            if (id == LorId.None)
                return;
            BookXmlInfo bookXml = Singleton<BookXmlList>.Instance.GetData(id);
            if (!RMRCore.ShouldRecordRoleBookInPermanentCompendium(bookXml))
                return;
            LogueBookModels.EnsureCompendiumUnlocks();
            LogueBookModels.CompendiumUnlockedRoleBooks.Add(id);
        }

        public static void RecordCompendiumBattleCard(LorId id)
        {
            if (id == LorId.None)
                return;
            LogueBookModels.EnsureCompendiumUnlocks();
            LogueBookModels.CompendiumUnlockedBattleCards.Add(id);
        }

        public static void RecordCompendiumAbnormalityPage(LorId id)
        {
            if (id == LorId.None)
                return;
            if (RMRAbnormalityUnlockManager.IsNoAbnormalityFallback(id))
                return;
            RewardPassiveInfo info = Singleton<RewardPassivesList>.Instance.GetPassiveInfo(id);
            // Normal and realization-exclusive abnormality pages both go into the permanent atlas
            // so realization battles can offer abnormality selection from unlocked history.
            if (info == null || info.rewardtype != RewardType.Creature)
                return;
            LogueBookModels.EnsureCompendiumUnlocks();
            LogueBookModels.CompendiumUnlockedAbnormalityPages.Add(id);
        }

        public static void RecordCompendiumEgoPage(LorId id)
        {
            if (id == LorId.None || !RMRAbnormalityUnlockManager.IsRealizationEgoCard(id))
                return;
            LogueBookModels.EnsureCompendiumUnlocks();
            LogueBookModels.CompendiumUnlockedEgoPages.Add(id);
        }

        public static bool IsCompendiumRoleBookUnlocked(LorId id)
        {
            LogueBookModels.EnsureCompendiumUnlocks();
            return LogueBookModels.CompendiumUnlockedRoleBooks.Contains(id);
        }

        public static bool IsCompendiumBattleCardUnlocked(LorId id)
        {
            LogueBookModels.EnsureCompendiumUnlocks();
            return LogueBookModels.CompendiumUnlockedBattleCards.Contains(id);
        }

        public static bool IsCompendiumAbnormalityPageUnlocked(LorId id)
        {
            LogueBookModels.EnsureCompendiumUnlocks();
            return LogueBookModels.CompendiumUnlockedAbnormalityPages.Contains(id);
        }

        public static bool IsCompendiumEgoPageUnlocked(LorId id)
        {
            LogueBookModels.EnsureCompendiumUnlocks();
            return LogueBookModels.CompendiumUnlockedEgoPages.Contains(id);
        }

        public static void SyncCurrentInventoryToPermanentAtlas()
        {
            LogueBookModels.EnsureCompendiumUnlocks();
            if (LogueBookModels.cardlist != null)
            {
                foreach (DiceCardItemModel card in LogueBookModels.cardlist)
                    LogueBookModels.CompendiumUnlockedBattleCards.Add(card.GetID());
            }
            if (LogueBookModels.booklist != null)
            {
                foreach (BookModel book in LogueBookModels.booklist)
                {
                    if (book?.ClassInfo != null && RMRCore.ShouldRecordRoleBookInPermanentCompendium(book.ClassInfo))
                        LogueBookModels.CompendiumUnlockedRoleBooks.Add(book.ClassInfo.id);
                }
            }
            LogueBookModels.SyncPlayerLoadoutToPermanentAtlas();
            // Route-unlocked abnormality pages (normal + exclusive already picked) → permanent atlas
            foreach (LorId pageId in RMRAbnormalityUnlockManager.GetRouteUnlockedPageIds())
                LogueBookModels.RecordCompendiumAbnormalityPage(pageId);
            if (LogueBookModels.EmotionCardList != null)
            {
                foreach (RewardPassiveInfo info in LogueBookModels.EmotionCardList)
                {
                    if (info != null)
                        LogueBookModels.RecordCompendiumAbnormalityPage(info.id);
                }
            }
            PruneInvalidPermanentRoleBookAtlasUnlocks();
            PruneInvalidPermanentAbnormalityCompendiumUnlocks();
        }

        private static bool SyncPlayerLoadoutToPermanentAtlas()
        {
            bool changed = false;
            LogueBookModels.EnsureCompendiumUnlocks();
            IEnumerable<UnitBattleDataModel> models = LogueBookModels.playerBattleModel ?? Enumerable.Empty<UnitBattleDataModel>();
            foreach (UnitBattleDataModel model in models)
            {
                BookModel book = model?.unitData?.bookItem;
                if (book?.ClassInfo != null && RMRCore.ShouldRecordRoleBookInPermanentCompendium(book.ClassInfo))
                    changed |= LogueBookModels.CompendiumUnlockedRoleBooks.Add(book.ClassInfo.id);

                List<DiceCardXmlInfo> deck = book?.GetCardListFromCurrentDeck();
                if (deck == null)
                    continue;
                foreach (DiceCardXmlInfo card in deck)
                {
                    if (card != null && card.id != LorId.None)
                        changed |= LogueBookModels.CompendiumUnlockedBattleCards.Add(card.id);
                }
            }
            return changed;
        }

        private static void PruneInvalidPermanentRoleBookAtlasUnlocks()
        {
            LogueBookModels.EnsureCompendiumUnlocks();
            LogueBookModels.CompendiumUnlockedRoleBooks.RemoveWhere(id =>
            {
                BookXmlInfo bookXml = RewardingModel.GetBookDataOriginAware(id);
                return !RMRCore.ShouldRecordRoleBookInPermanentCompendium(bookXml);
            });
        }

        public static void PruneCorePageExclusiveBattleCardsFromInventoryAndAtlas()
        {
            LogueBookModels.EnsureCompendiumUnlocks();
            HashSet<LorId> exclusiveCards = LogueBookModels.GetCorePageExclusiveBattleCardIds();
            exclusiveCards.RemoveWhere(IsRedMistRewardBattleCard);
            exclusiveCards.RemoveWhere(IsBlueReverberationRewardBattleCard);
            if (exclusiveCards.Count == 0)
                return;
            if (LogueBookModels.cardlist != null)
                LogueBookModels.cardlist.RemoveAll(card => exclusiveCards.Contains(card.GetID()));
            LogueBookModels.CompendiumUnlockedBattleCards.RemoveWhere(id => exclusiveCards.Contains(id));
        }

        private static HashSet<LorId> GetCorePageExclusiveBattleCardIds()
        {
            HashSet<LorId> result = new HashSet<LorId>();
            List<LorId> roleBookIds = new List<LorId>();
            if (LogueBookModels.booklist != null)
                roleBookIds.AddRange(LogueBookModels.booklist.Where(book => book?.ClassInfo != null).Select(book => book.ClassInfo.id));
            if (LogueBookModels.CompendiumUnlockedRoleBooks != null)
                roleBookIds.AddRange(LogueBookModels.CompendiumUnlockedRoleBooks);
            foreach (LorId roleBookId in roleBookIds)
            {
                BookXmlInfo book = RewardingModel.GetBookDataOriginAware(roleBookId);
                if (book?.EquipEffect?.OnlyCard == null || book.EquipEffect.OnlyCard.Count == 0)
                    continue;
                foreach (int cardId in book.EquipEffect.OnlyCard)
                {
                    DiceCardXmlInfo card = ItemXmlDataList.instance.GetCardItem(new LorId(book.workshopID, cardId), true)
                        ?? ItemXmlDataList.instance.GetCardItem(cardId, true);
                    if (card != null)
                        result.Add(card.id);
                }
            }
            return result;
        }

        private static bool IsRedMistRewardBattleCard(LorId id)
        {
            return id.id >= 607003 && id.id <= 607007;
        }

        private static bool IsBlueReverberationRewardBattleCard(LorId id)
        {
            return id != null
                && (id.id == 704001
                    || (id.id >= 704011 && id.id <= 704014)
                    || id.id == 705010
                    || id.id == 705011);
        }

        public static void PruneInvalidPermanentAbnormalityCompendiumUnlocks()
        {
            LogueBookModels.EnsureCompendiumUnlocks();
            LogueBookModels.CompendiumUnlockedAbnormalityPages.RemoveWhere(id =>
            {
                if (RMRAbnormalityUnlockManager.IsNoAbnormalityFallback(id))
                    return true;
                RewardPassiveInfo info = Singleton<RewardPassivesList>.Instance.GetPassiveInfo(id);
                if (info == null || info.rewardtype != RewardType.Creature)
                    return true;
                // Normal pages stay once recorded.
                if (!RMRAbnormalityUnlockManager.IsRealizationExclusive(info))
                    return false;
                // Exclusive pages require the floor realization to be completed.
                // Use ResolveRealizationFloor (script + exclusive ID block) — script-only
                // left quietKid*/ID-only rows stuck as None and wiped valid atlas unlocks.
                SephirahType floor = RMRAbnormalityUnlockManager.ResolveRealizationFloor(info);
                return floor == SephirahType.None || !RMRAbnormalityUnlockManager.IsFloorRealizationCompleted(floor);
            });
            LogueBookModels.CompendiumUnlockedEgoPages.RemoveWhere(id =>
            {
                SephirahType floor = SephirahType.None;
                foreach (var kvp in RMRAbnormalityUnlockManager.RealizationEgoCardsByFloor)
                {
                    if (kvp.Value.Any(egoId => egoId == id))
                    {
                        floor = kvp.Key;
                        break;
                    }
                }
                return floor == SephirahType.None || !RMRAbnormalityUnlockManager.IsFloorRealizationCompleted(floor);
            });
        }

        public static void SavePermanentCompendiumUnlocks()
        {
            LogueBookModels.SyncCurrentInventoryToPermanentAtlas();
            LogueBookModels.SavePermanentCompendiumData();
            LoguePlayDataSaver.SavePlayData_Menu();
        }

        public static void SavePermanentCompendiumData()
        {
            try
            {
                LogueBookModels.EnsureCompendiumUnlocks();
                // Never overwrite disk with an unhydrated/empty session (hub open before CreatePlayer).
                if (!SuppressPermanentAtlasMergeOnSave && !PermanentCompendiumHydrated)
                    LogueBookModels.LoadPermanentCompendiumData();
                else if (!SuppressPermanentAtlasMergeOnSave)
                {
                    // Even when hydrated, re-merge disk so concurrent/other-session unlocks are not lost.
                    // LoadCompendiumUnlockSet only adds; it does not remove intentional progress.
                    try
                    {
                        SaveData disk = Singleton<LogueSaveManager>.Instance.LoadData(PermanentCompendiumSaveName);
                        if (disk != null)
                        {
                            LogueBookModels.LoadCompendiumUnlockSet(disk.GetData("atlasRoleBookUnlocks"), LogueBookModels.CompendiumUnlockedRoleBooks);
                            LogueBookModels.LoadCompendiumUnlockSet(disk.GetData("atlasBattleCardUnlocks"), LogueBookModels.CompendiumUnlockedBattleCards);
                            LogueBookModels.LoadCompendiumUnlockSet(disk.GetData("atlasAbnormalityPageUnlocks"), LogueBookModels.CompendiumUnlockedAbnormalityPages);
                            LogueBookModels.LoadCompendiumUnlockSet(disk.GetData("atlasEgoPageUnlocks"), LogueBookModels.CompendiumUnlockedEgoPages);
                        }
                    }
                    catch (Exception mergeEx)
                    {
                        Debug.LogWarning("[RMR Atlas] Pre-save disk merge skipped: " + mergeEx.Message);
                    }
                }

                LogueBookModels.PruneInvalidPermanentRoleBookAtlasUnlocks();
                LogueBookModels.PruneInvalidPermanentAbnormalityCompendiumUnlocks();
                SaveData data = new SaveData();
                data.AddData("atlasRoleBookUnlocks", LogueBookModels.SaveCompendiumUnlockSet(LogueBookModels.CompendiumUnlockedRoleBooks));
                data.AddData("atlasBattleCardUnlocks", LogueBookModels.SaveCompendiumUnlockSet(LogueBookModels.CompendiumUnlockedBattleCards));
                data.AddData("atlasAbnormalityPageUnlocks", LogueBookModels.SaveCompendiumUnlockSet(LogueBookModels.CompendiumUnlockedAbnormalityPages));
                data.AddData("atlasEgoPageUnlocks", LogueBookModels.SaveCompendiumUnlockSet(LogueBookModels.CompendiumUnlockedEgoPages));
                Singleton<LogueSaveManager>.Instance.SaveData(data, PermanentCompendiumSaveName);
                PermanentCompendiumHydrated = true;
                Debug.Log($"[RMR Atlas] Saved permanent atlas. RoleBooks={CompendiumUnlockedRoleBooks.Count}, BattleCards={CompendiumUnlockedBattleCards.Count}, Abno={CompendiumUnlockedAbnormalityPages.Count}, Ego={CompendiumUnlockedEgoPages.Count}.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[RMR Atlas] Failed to save permanent atlas data: " + e);
            }
        }

        public static void LoadPermanentCompendiumData()
        {
            try
            {
                LogueBookModels.EnsureCompendiumUnlocks();
                SaveData data = Singleton<LogueSaveManager>.Instance.LoadData(PermanentCompendiumSaveName);
                if (data == null)
                {
                    PermanentCompendiumHydrated = true;
                    Debug.Log("[RMR Atlas] No permanent atlas save on disk yet.");
                    return;
                }

                int beforeRoles = CompendiumUnlockedRoleBooks.Count;
                int beforeCards = CompendiumUnlockedBattleCards.Count;
                LogueBookModels.LoadCompendiumUnlockSet(data.GetData("atlasRoleBookUnlocks"), LogueBookModels.CompendiumUnlockedRoleBooks);
                LogueBookModels.LoadCompendiumUnlockSet(data.GetData("atlasBattleCardUnlocks"), LogueBookModels.CompendiumUnlockedBattleCards);
                LogueBookModels.LoadCompendiumUnlockSet(data.GetData("atlasAbnormalityPageUnlocks"), LogueBookModels.CompendiumUnlockedAbnormalityPages);
                LogueBookModels.LoadCompendiumUnlockSet(data.GetData("atlasEgoPageUnlocks"), LogueBookModels.CompendiumUnlockedEgoPages);
                LogueBookModels.PruneCorePageExclusiveBattleCardsFromInventoryAndAtlas();
                LogueBookModels.PruneInvalidPermanentAbnormalityCompendiumUnlocks();
                PermanentCompendiumHydrated = true;
                Debug.Log($"[RMR Atlas] Loaded permanent atlas. RoleBooks={CompendiumUnlockedRoleBooks.Count} (was {beforeRoles}), BattleCards={CompendiumUnlockedBattleCards.Count} (was {beforeCards}), Abno={CompendiumUnlockedAbnormalityPages.Count}, Ego={CompendiumUnlockedEgoPages.Count}.");
            }
            catch (Exception e)
            {
                PermanentCompendiumHydrated = true; // avoid infinite re-load loops; save will not wipe if merge runs
                Debug.LogWarning("[RMR Atlas] Failed to load permanent atlas data: " + e);
            }
        }

        public static void LoadPermanentCompendiumUnlocks()
        {
            LogueBookModels.LoadPermanentCompendiumData();
        }

        /// <summary>
        /// Call before intentionally wiping permanent atlas (reset archive).
        /// Prevents SavePermanentCompendiumData from re-merging disk contents back in.
        /// </summary>
        public static void BeginPermanentCompendiumReset()
        {
            SuppressPermanentAtlasMergeOnSave = true;
            PermanentCompendiumHydrated = true;
        }

        public static void EndPermanentCompendiumReset()
        {
            SuppressPermanentAtlasMergeOnSave = false;
        }

        private static SaveData SaveCompendiumUnlockSet(HashSet<LorId> ids)
        {
            SaveData data = new SaveData();
            if (ids == null)
                return data;
            foreach (LorId id in ids)
                data.AddToList(id.LogGetSaveData());
            return data;
        }

        private static void LoadCompendiumUnlockSet(SaveData data, HashSet<LorId> target)
        {
            if (data == null || target == null)
                return;
            foreach (SaveData savedId in data)
                target.Add(RMRCore.NormalizeLegacyBlueReverberationCorePageId(ExtensionUtils.LogLoadFromSaveData(savedId)));
        }

        #endregion

        #region --- Unit / book serialization helpers ---

        public static void LoadFromSaveData_UnitBattleDataModel(SaveData data, UnitBattleDataModel model)
        {
            BookXmlInfo classInfo = model.unitData.bookItem.ClassInfo;
            model.hp = (float)data.GetData("hp").GetIntSelf();
            model.isDead = data.GetData("isDead").GetIntSelf() == 1;
            classInfo._bookIcon = data.GetData("_bookIcon").GetStringSelf();
            classInfo.TextId = data.GetData("TextId").GetIntSelf();
            classInfo.Chapter = data.GetData("Chapter").GetIntSelf();
            classInfo.RangeType = (EquipRangeType)data.GetInt("RangeType");
            classInfo.CharacterSkin = new List<string>();
            foreach (SaveData saveData in data.GetData("CharacterSkin"))
                classInfo.CharacterSkin.Add(saveData.GetStringSelf());
            SaveData data1 = data.GetData("EquipEffect");
            classInfo.EquipEffect.AddedStartDraw = data1.GetInt("AddedStartDraw");
            classInfo.EquipEffect.Break = data1.GetInt("Break");
            classInfo.EquipEffect.HBResist = (AtkResist)data1.GetInt("HBResist");
            classInfo.EquipEffect.Hp = data1.GetInt("Hp");
            classInfo.EquipEffect.HpReduction = data1.GetInt("HpReduction");
            classInfo.EquipEffect.HResist = (AtkResist)data1.GetInt("HResist");
            classInfo.EquipEffect.MaxPlayPoint = data1.GetInt("MaxPlayPoint");
            foreach (SaveData data2 in data1.GetData("PassiveList"))
            {
                LorId lorId = ExtensionUtils.LogLoadFromSaveData(data2);
                if (lorId != new LorId(LogLikeMod.ModId, 1))
                    classInfo.EquipEffect.PassiveList.Add(lorId);
            }
            model.unitData.bookItem.instanceId = data.GetInt("instanceid");
            SaveData data3 = data.GetData("SucPassiveList");
            List<PassiveModel> fieldValue1 = LogLikeMod.GetFieldValue<List<PassiveModel>>(model.unitData.bookItem, "_activatedAllPassives");
            foreach (SaveData data4 in data3)
            {
                PassiveModel __instance = new PassiveModel(model.unitData.bookItem.instanceId);
                __instance.LoadFromSaveDataPassiveModel(data4);
                fieldValue1.Add(__instance);
            }
            classInfo.EquipEffect.PBResist = (AtkResist)data1.GetInt("PBResist");
            classInfo.EquipEffect.PResist = (AtkResist)data1.GetInt("PResist");
            classInfo.EquipEffect.SBResist = (AtkResist)data1.GetInt("SBResist");
            classInfo.EquipEffect.Speed = data1.GetInt("Speed");
            classInfo.EquipEffect.SpeedMin = data1.GetInt("SpeedMin");
            classInfo.EquipEffect.SResist = (AtkResist)data1.GetInt("SResist");
            classInfo.EquipEffect.StartPlayPoint = data1.GetInt("StartPlayPoint");
            model.unitData.bookItem.TryGainUniquePassive();
            model.unitData.bookItem.SetOriginalStat();
            model.unitData.bookItem.SetOriginalSpeed();
            model.unitData.bookItem.SetOriginalResists();
            model.unitData.bookItem.SetOriginalPlayPoint();
            model.unitData.bookItem.SetOriginalCharacterName();
            typeof(BookModel).GetField("_selectedOriginalSkin", AccessTools.all).SetValue(model.unitData.bookItem, classInfo.CharacterSkin[0]);
            typeof(BookModel).GetField("_characterSkin", AccessTools.all).SetValue(model.unitData.bookItem, classInfo.CharacterSkin[0]);
            SaveData data5 = data.GetData("_onlyCard");
            List<DiceCardXmlInfo> fieldValue2 = LogLikeMod.GetFieldValue<List<DiceCardXmlInfo>>(model.unitData.bookItem, "_onlyCards");
            if (data5.GetListCount() > 0)
            {
                foreach (SaveData data6 in data5)
                {
                    DiceCardXmlInfo cardItem = ItemXmlDataList.instance.GetCardItem(ExtensionUtils.LogLoadFromSaveData(data6));
                    fieldValue2.Add(cardItem);
                }
            }
            SaveData data7 = data.GetData("deck");
            int num1 = data7.GetListCount() > 0 ? 1 : 0;
            model.Log("INITIALIZING DECKS");
            if (num1 != 0)
            {
                foreach (SaveData data8 in data7)
                {
                    // A saved upgraded card uses a dynamic <LogUpgrade> package id. Rebuild it
                    // before adding it to the unit deck, just as the run inventory loader does;
                    // otherwise the inventory later contains the card but this deck entry was
                    // skipped while the dynamic XML definition did not yet exist.
                    LorId cardId = RestoreSavedCombatCardForLoad(ExtensionUtils.LogLoadFromSaveData(data8));
                    model.Log($"INITIALIZING DECK FOR UNIT : {cardId.packageId}, {cardId.id.ToString()}");
                    LogueBookModels.AddCard(cardId, 1, false);
                    int num2 = (int)model.unitData.AddCardFromInventory(cardId);
                }
            }
            SaveData data9 = data.GetData("equipedbooklist");  
            model.unitData.bookItem.originData.equipedBookIdListInPassive = new List<int>();
            foreach (SaveData saveData in data9)
                model.unitData.bookItem.originData.equipedBookIdListInPassive.Add(saveData.GetIntSelf());
            
            var data10 = data.GetData("customizedAppearance");
            if (data10 != null)
                model.unitData.customizeData.LoadFromSaveData(data10);

            var data11 = data.GetData("nuggetName");
            if (data11 != null)
                model.unitData.SetCustomName(data11.GetStringSelf());
            RestoreGrade6SpecialBuiltInDeckSource(model.unitData, data);
        }

        public static SaveData GetSaveData_UnitBattleDataModel(UnitBattleDataModel model)
        {
            BookXmlInfo classInfo = model.unitData.bookItem.ClassInfo;
            SaveData data1 = new SaveData();
            data1.AddData("hp", new SaveData((int)model.hp));
            data1.AddData("isDead", model.isDead ? 1 : 0);
            data1.AddData("_bookIcon", new SaveData(classInfo._bookIcon));
            data1.AddData("TextId", new SaveData(classInfo.TextId));
            data1.AddData("Chapter", new SaveData(classInfo.Chapter));
            data1.AddData("RangeType", new SaveData((int)classInfo.RangeType));
            SaveData data2 = new SaveData();
            foreach (string str in classInfo.CharacterSkin)
                data2.AddToList(new SaveData(str));
            data1.AddData("CharacterSkin", data2);
            SaveData data3 = new SaveData();
            data3.AddData("AddedStartDraw", new SaveData(classInfo.EquipEffect.AddedStartDraw));
            data3.AddData("Break", new SaveData(classInfo.EquipEffect.Break));
            data3.AddData("HBResist", new SaveData((int)classInfo.EquipEffect.HBResist));
            data3.AddData("Hp", new SaveData(classInfo.EquipEffect.Hp));
            data3.AddData("HpReduction", new SaveData(classInfo.EquipEffect.HpReduction));
            data3.AddData("HResist", new SaveData((int)classInfo.EquipEffect.HResist));
            data3.AddData("MaxPlayPoint", new SaveData(classInfo.EquipEffect.MaxPlayPoint));
            SaveData data4 = new SaveData();
            foreach (LorId passive in classInfo.EquipEffect.PassiveList)
            {
                if (passive != new LorId(LogLikeMod.ModId, 1))
                    data4.AddToList(passive.LogGetSaveData());
            }
            data3.AddData("PassiveList", data4);
            data3.AddData("PBResist", new SaveData((int)classInfo.EquipEffect.PBResist));
            data3.AddData("PResist", new SaveData((int)classInfo.EquipEffect.PResist));
            data3.AddData("SBResist", new SaveData((int)classInfo.EquipEffect.SBResist));
            data3.AddData("Speed", new SaveData(classInfo.EquipEffect.Speed));
            data3.AddData("SpeedMin", new SaveData(classInfo.EquipEffect.SpeedMin));
            data3.AddData("SResist", new SaveData((int)classInfo.EquipEffect.SResist));
            data3.AddData("StartPlayPoint", new SaveData(classInfo.EquipEffect.StartPlayPoint));
            data1.AddData("EquipEffect", data3);
            SaveData data5 = new SaveData();
            List<DiceCardXmlInfo> listFromCurrentDeck = model.unitData.bookItem.GetCardListFromCurrentDeck();
            if (listFromCurrentDeck.Count > 0)
            {
                foreach (DiceCardXmlInfo diceCardXmlInfo in listFromCurrentDeck)
                    data5.AddToList(diceCardXmlInfo.id.LogGetSaveData());
            }
            data1.AddData("deck", data5);
            data1.AddData("instanceid", new SaveData(model.unitData.bookItem.instanceId));
            SaveData data6 = new SaveData();
            foreach (int num in model.unitData.bookItem.originData.equipedBookIdListInPassive)
                data6.AddToList(new SaveData(num));
            data1.AddData("equipedbooklist", data6);
            SaveData data7 = new SaveData();
            foreach (PassiveModel __instance in model.unitData.bookItem.GetPassiveModelList().FindAll((Predicate<PassiveModel>)(x => x.originData.receivepassivebookId != model.unitData.bookItem.instanceId)))
            {
                SaveData dataPassiveModel = __instance.GetSaveDataPassiveModel();
                data7.AddToList(dataPassiveModel);
            }
            data1.AddData("SucPassiveList", data7);
            SaveData data8 = new SaveData();
            List<DiceCardXmlInfo> fieldValue = LogLikeMod.GetFieldValue<List<DiceCardXmlInfo>>(model.unitData.bookItem, "_onlyCards");
            if (fieldValue.Count > 0)
            {
                foreach (DiceCardXmlInfo diceCardXmlInfo in fieldValue)
                    data8.AddToList(diceCardXmlInfo.id.LogGetSaveData());
            }
            data1.AddData("_onlyCard", data8);
            var data9 = model.unitData.customizeData.GetSaveData();
            data1.AddData("customizedAppearance", data9);
            var data10 = model.unitData.name;
            data1.AddData("nuggetName", data10);
            EnsureGrade6SpecialBuiltInDeckSource();
            if (Grade6SpecialBuiltInDeckSource.TryGetValue(model.unitData, out LorId sourceId) && sourceId != LorId.None
                && !RMRCore.IsBlueReverberationCorePage(classInfo)
                && !IsBlueReverberationDeckSource(sourceId))
                data1.AddData("Grade6SpecialBuiltInDeckSource", Grade6SpecialBuiltInDeckSource[model.unitData].LogGetSaveData());
            return data1;
        }

        #endregion

        #region --- Full run save / load / chapter bootstrap ---

        public static SaveData CreateChSaveData(ChapterGrade grade)
        {
            LogueBookModels.CreatePlayer();
            LogueBookModels.CreatePlayerBattle();
            RMRAbnormalityUnlockManager.StartNewRoute(grade);
            int num = grade > ChapterGrade.Grade5 ? 4 : (int)grade;
            for (int index = 0; index < num; ++index)
                LogueBookModels.AddSubPlayer();
            SaveData data1 = new SaveData();
            LogueBookModels.AddingRemainStageList();
            SaveData data2 = new SaveData();
            foreach (KeyValuePair<ChapterGrade, List<LogueStageInfo>> remainStage in LogueBookModels.RemainStageList)
            {
                SaveData data3 = new SaveData();
                foreach (LogueStageInfo logueStageInfo in remainStage.Value)
                    data3.AddToList(logueStageInfo.Id.LogGetSaveData());
                data2.AddData(remainStage.Key.ToString(), data3);
            }
            data1.AddData("RemainStageList", data2);
            SaveData data4 = new SaveData();
            foreach (LorId cardId in Singleton<CardDropTableXmlList>.Instance.GetData(new LorId(LogLikeMod.ModId, -854001)).cardIdList)
                LogueBookModels.AddCard(cardId, 99, false);
            foreach (LorId cardId in Singleton<CardDropTableXmlList>.Instance.GetData(new LorId(LogLikeMod.ModId, -854101)).cardIdList)
                LogueBookModels.AddCard(cardId, 99, false);
            if (grade > ChapterGrade.Grade2)
            {
                foreach (LorId cardId in Singleton<CardDropTableXmlList>.Instance.GetData(new LorId(LogLikeMod.ModId, -854201)).cardIdList)
                    LogueBookModels.AddCard(cardId, 99, false);
            }
            if (grade > ChapterGrade.Grade3)
            {
                foreach (LorId cardId in Singleton<CardDropTableXmlList>.Instance.GetData(new LorId(LogLikeMod.ModId, -854301)).cardIdList)
                    LogueBookModels.AddCard(cardId, 99, false);
            }
            if (grade > ChapterGrade.Grade4)
            {
                foreach (LorId cardId in Singleton<CardDropTableXmlList>.Instance.GetData(new LorId(LogLikeMod.ModId, -854401)).cardIdList)
                    LogueBookModels.AddCard(cardId, 99, false);
            }
            foreach (DiceCardItemModel diceCardItemModel in LogueBookModels.cardlist)
            {
                SaveData data5 = new SaveData();
                data5.AddData("id", diceCardItemModel.ClassInfo.id.LogGetSaveData());
                data5.AddData("num", new SaveData(diceCardItemModel.num));
                data4.AddToList(data5);
            }
            data1.AddData("cardlist", data4);
            SaveData data6 = new SaveData();
            foreach (UnitBattleDataModel model in LogueBookModels.playerBattleModel)
                data6.AddToList(LogueBookModels.GetSaveData_UnitBattleDataModel(model));
            data1.AddData("playerBattleModel", data6);
            SaveData data7 = new SaveData();
            foreach (KeyValuePair<UnitDataModel, List<LorId>> keyValuePair in LogueBookModels.playersPick)
            {
                SaveData data8 = new SaveData();
                foreach (LorId id in keyValuePair.Value)
                    data8.AddToList(id.LogGetSaveData());
                data7.AddData(keyValuePair.Key.bookItem.BookId.id.ToString(), data8);
            }
            data1.AddData("playersPick", data7);
            SaveData data9 = new SaveData();
            foreach (KeyValuePair<UnitDataModel, List<LorId>> playersperpassive in LogueBookModels.playersperpassives)
            {
                SaveData data10 = new SaveData();
                foreach (LorId id in playersperpassive.Value)
                    data10.AddToList(id.LogGetSaveData());
                data9.AddData(playersperpassive.Key.bookItem.BookId.id.ToString(), data10);
            }
            data1.AddData("playersperpassives", data9);
            SaveData data11 = new SaveData();
            foreach (KeyValuePair<UnitDataModel, List<LogStatAdder>> playersstatadder in LogueBookModels.playersstatadders)
            {
                SaveData data12 = new SaveData();
                foreach (LogStatAdder logStatAdder in playersstatadder.Value)
                    data12.AddToList(logStatAdder.GetSaveData());
                data11.AddData(playersstatadder.Key.bookItem.BookId.id.ToString(), data12);
            }
            data1.AddData("playersstatadders", data11);
            SaveData data13 = new SaveData();
            foreach (LorId id in LogueBookModels.shopPick)
                data13.AddToList(id.LogGetSaveData());
            data1.AddData("shopPick", data13);
            SaveData data14 = new SaveData();
            foreach (RewardPassiveInfo rewardPassiveInfo in Singleton<RewardPassivesList>.Instance.GetChapterData((Predicate<RewardPassivesInfo>)(x => x.rewardtype == PassiveRewardListType.CommonReward)))
            {
                if ((ChapterGrade)Singleton<BookXmlList>.Instance.GetData(rewardPassiveInfo.id).Chapter <= grade + 1)
                {
                    for (int index = 0; index < PickUpModel_EquipDefault.EquipLimit(rewardPassiveInfo.passiverarity); ++index)
                    {
                        BookModel bookModel = new BookModel(Singleton<BookXmlList>.Instance.GetData(rewardPassiveInfo.id));
                        bookModel.instanceId = LogueBookModels.nextinstanceid++;
                        bookModel.TryGainUniquePassive();
                        LogueBookModels.booklist.Add(bookModel);
                        LogueBookModels.RecordCompendiumRoleBook(bookModel.ClassInfo.id);
                    }
                }
            }
            foreach (BookModel model in LogueBookModels.booklist)
            {
                SaveData saveDataBookModel = LogueBookModels.GetSaveData_BookModel(model);
                data14.AddToList(saveDataBookModel);
            }
            data1.AddData("booklist", data14);
            data1.AddData("RMRAbnormalityUnlocks", RMRAbnormalityUnlockManager.SaveRouteUnlocks());
            data1.AddData("atlasRoleBookUnlocks", LogueBookModels.SaveCompendiumUnlockSet(LogueBookModels.CompendiumUnlockedRoleBooks));
            data1.AddData("atlasBattleCardUnlocks", LogueBookModels.SaveCompendiumUnlockSet(LogueBookModels.CompendiumUnlockedBattleCards));
            data1.AddData("atlasAbnormalityPageUnlocks", LogueBookModels.SaveCompendiumUnlockSet(LogueBookModels.CompendiumUnlockedAbnormalityPages));
            data1.AddData("atlasEgoPageUnlocks", LogueBookModels.SaveCompendiumUnlockSet(LogueBookModels.CompendiumUnlockedEgoPages));
            data1.AddData("SubPlayerNum", new SaveData(LogueBookModels.playerModel.Count - 1));
            data1.AddData("nextinstanceid", LogueBookModels.nextinstanceid);
            return data1;
        }

        public static SaveData GetSaveData()
        {
            "".Log("LogueInven Save Start : " + DateTime.Now.ToString());
            LogueBookModels.SyncCurrentInventoryToPermanentAtlas();
            LogueBookModels.SavePermanentCompendiumData();
            SaveData data1 = new SaveData();
            SaveData data2 = new SaveData();
            foreach (KeyValuePair<int, int> keyValuePair in LogueBookModels.EmotionSelectDic)
                data2.AddData(keyValuePair.Key.ToString(), keyValuePair.Value);
            data1.AddData("emotionSelect", data2);
            SaveData data3 = new SaveData();
            foreach (KeyValuePair<ChapterGrade, List<LogueStageInfo>> remainStage in LogueBookModels.RemainStageList)
            {
                SaveData data4 = new SaveData();
                foreach (LogueStageInfo logueStageInfo in remainStage.Value)
                    data4.AddToList(logueStageInfo.Id.LogGetSaveData());
                data3.AddData(remainStage.Key.ToString(), data4);
            }
            data1.AddData("RemainStageList", data3);
            SaveData data5 = new SaveData();
            foreach (DiceCardItemModel diceCardItemModel in LogueBookModels.cardlist)
            {
                SaveData data6 = new SaveData();
                data6.AddData("id", diceCardItemModel.ClassInfo.id.LogGetSaveData());
                data6.AddData("num", new SaveData(diceCardItemModel.num));
                data5.AddToList(data6);
            }
            data1.AddData("cardlist", data5);
            SaveData data7 = new SaveData();
            foreach (UnitBattleDataModel model in LogueBookModels.playerBattleModel)
                data7.AddToList(LogueBookModels.GetSaveData_UnitBattleDataModel(model));
            data1.AddData("playerBattleModel", data7);
            SaveData data8 = new SaveData();
            foreach (KeyValuePair<UnitDataModel, List<LorId>> keyValuePair in LogueBookModels.playersPick)
            {
                SaveData data9 = new SaveData();
                foreach (LorId id in keyValuePair.Value)
                    data9.AddToList(id.LogGetSaveData());
                data8.AddData(keyValuePair.Key.bookItem.BookId.id.ToString(), data9);
            }
            data1.AddData("playersPick", data8);
            SaveData data10 = new SaveData();
            foreach (KeyValuePair<UnitDataModel, List<LorId>> playersperpassive in LogueBookModels.playersperpassives)
            {
                SaveData data11 = new SaveData();
                foreach (LorId id in playersperpassive.Value)
                    data11.AddToList(id.LogGetSaveData());
                data10.AddData(playersperpassive.Key.bookItem.BookId.id.ToString(), data11);
            }
            data1.AddData("playersperpassives", data10);
            SaveData data12 = new SaveData();
            foreach (KeyValuePair<UnitDataModel, List<LogStatAdder>> playersstatadder in LogueBookModels.playersstatadders)
            {
                SaveData data13 = new SaveData();
                foreach (LogStatAdder logStatAdder in playersstatadder.Value)
                    data13.AddToList(logStatAdder.GetSaveData());
                data12.AddData(playersstatadder.Key.bookItem.BookId.id.ToString(), data13);
            }
            data1.AddData("playersstatadders", data12);
            SaveData data14 = new SaveData();
            foreach (LorId id in LogueBookModels.shopPick)
                data14.AddToList(id.LogGetSaveData());
            data1.AddData("shopPick", data14);
            data1.AddData("shopUpgradeCardPrice", new SaveData(LogueBookModels.shopUpgradeCardPrice));
            SaveData binahUpgradeData = new SaveData();
            if (LogueBookModels.BinahUpgradedDegradedCardIds != null)
            {
                foreach (int upgradedId in LogueBookModels.BinahUpgradedDegradedCardIds)
                    binahUpgradeData.AddToList(new SaveData(upgradedId));
            }
            data1.AddData("binahUpgradedCards", binahUpgradeData);
            SaveData data15 = new SaveData();
            foreach (BookModel model in LogueBookModels.booklist)
            {
                SaveData saveDataBookModel = LogueBookModels.GetSaveData_BookModel(model);
                data15.AddToList(saveDataBookModel);
            }
            data1.AddData("booklist", data15);
            data1.AddData("RMRAbnormalityUnlocks", RMRAbnormalityUnlockManager.SaveRouteUnlocks());
            data1.AddData("atlasRoleBookUnlocks", LogueBookModels.SaveCompendiumUnlockSet(LogueBookModels.CompendiumUnlockedRoleBooks));
            data1.AddData("atlasBattleCardUnlocks", LogueBookModels.SaveCompendiumUnlockSet(LogueBookModels.CompendiumUnlockedBattleCards));
            data1.AddData("atlasAbnormalityPageUnlocks", LogueBookModels.SaveCompendiumUnlockSet(LogueBookModels.CompendiumUnlockedAbnormalityPages));
            data1.AddData("atlasEgoPageUnlocks", LogueBookModels.SaveCompendiumUnlockSet(LogueBookModels.CompendiumUnlockedEgoPages));
            data1.AddData("SubPlayerNum", new SaveData(LogueBookModels.playerModel.Count - 1));
            data1.AddData("nextinstanceid", LogueBookModels.nextinstanceid);
            "".Log("LogueInven Save End : " + DateTime.Now.ToString());
            return data1;
        }

        /// <summary>
        /// Picks, per-unit passives/stat adders, shop state, emotion selection and the remaining
        /// stage list. Separated from <see cref="LoadFromSaveData"/> so a failure here cannot skip
        /// the inventory load that follows it.
        /// </summary>
        private static void LoadOptionalRunState(SaveData save)
        {
            foreach (KeyValuePair<string, SaveData> keyValuePair in save.GetData("playersPick").GetDictionarySelf())
            {
                KeyValuePair<string, SaveData> dic = keyValuePair;
                UnitDataModel key = LogueBookModels.playerModel.Find((Predicate<UnitDataModel>)(x => x.bookItem.BookId.id.ToString() == dic.Key));
                foreach (SaveData data in dic.Value)
                    LogueBookModels.playersPick[key].Add(ExtensionUtils.LogLoadFromSaveData(data));
            }
            foreach (KeyValuePair<string, SaveData> keyValuePair in save.GetData("playersperpassives").GetDictionarySelf())
            {
                KeyValuePair<string, SaveData> dic = keyValuePair;
                UnitDataModel key = LogueBookModels.playerModel.Find((Predicate<UnitDataModel>)(x => x.bookItem.BookId.id.ToString() == dic.Key));
                foreach (SaveData data in dic.Value)
                    LogueBookModels.playersperpassives[key].Add(ExtensionUtils.LogLoadFromSaveData(data));
            }
            foreach (SaveData data in save.GetData("shopPick"))
                LogueBookModels.shopPick.Add(ExtensionUtils.LogLoadFromSaveData(data));
            SaveData upgradePriceData = save.GetData("shopUpgradeCardPrice");
            if (upgradePriceData != null)
                LogueBookModels.shopUpgradeCardPrice = upgradePriceData.GetIntSelf();
            LogueBookModels.BinahUpgradedDegradedCardIds = new HashSet<int>();
            SaveData binahUpgradeLoad = save.GetData("binahUpgradedCards");
            if (binahUpgradeLoad != null)
            {
                foreach (SaveData entry in binahUpgradeLoad)
                {
                    if (entry == null)
                        continue;
                    int upgradedId = entry.GetIntSelf();
                    if (upgradedId > 0)
                        LogueBookModels.BinahUpgradedDegradedCardIds.Add(upgradedId);
                }
            }
            // Units restore fixed decks before this flag is loaded; re-apply so full Binah pages stick.
            if (LogueBookModels.BinahUpgradedDegradedCardIds.Count > 0)
            {
                EnsureGrade6SpecialBuiltInDeckSource();
                foreach (KeyValuePair<UnitDataModel, LorId> kv in Grade6SpecialBuiltInDeckSource.ToList())
                {
                    if (kv.Key != null && kv.Value != null && kv.Value != LorId.None)
                        TryApplyGrade6SpecialBuiltInDeckToUnit(kv.Key, kv.Value);
                }
            }
            foreach (KeyValuePair<string, SaveData> keyValuePair in save.GetData("playersstatadders").GetDictionarySelf())
            {
                KeyValuePair<string, SaveData> dic = keyValuePair;
                UnitDataModel key = LogueBookModels.playerModel.Find(x => x.bookItem.BookId.id.ToString() == dic.Key);
                foreach (SaveData saveData in dic.Value)
                {
                    LogStatAdder statAdderBySave = LogStatAdder.CreateStatAdderBySave(saveData);
                    if (statAdderBySave != null)
                    {
                        statAdderBySave.LoadFromSaveData(saveData);
                        LogueBookModels.playersstatadders[key].Add(statAdderBySave);
                    }
                }
            }
            LogueBookModels.EmotionCardList = new List<RewardPassiveInfo>();
            try
            {
                SaveData data = save.GetData("emotionSelect");
                LogueBookModels.EmotionSelectDic[0] = data.GetInt("0");
                LogueBookModels.EmotionSelectDic[1] = data.GetInt("1");
                LogueBookModels.EmotionSelectDic[2] = data.GetInt("2");
                LogueBookModels.EmotionSelectDic[3] = data.GetInt("3");
            }
            catch
            {
            }
            LogueBookModels.RemainStageList = new Dictionary<ChapterGrade, List<LogueStageInfo>>();
            SaveData data1 = save.GetData("RemainStageList");
            foreach (ChapterGrade key in new List<ChapterGrade>()
            {
                ChapterGrade.Grade1,
                ChapterGrade.Grade2,
                ChapterGrade.Grade3,
                ChapterGrade.Grade4,
                ChapterGrade.Grade5,
                ChapterGrade.Grade6,
                ChapterGrade.Grade7
            })
            {
                if (data1.GetData(key.ToString()) != null)
                {
                    List<LogueStageInfo> logueStageInfoList = new List<LogueStageInfo>();
                    foreach (SaveData data2 in data1.GetData(key.ToString()))
                    {
                        LogueStageInfo stageInfo = Singleton<StagesXmlList>.Instance.GetStageInfo(ExtensionUtils.LogLoadFromSaveData(data2));
                        if (stageInfo != null)
                        {
                            if (!string.IsNullOrEmpty(stageInfo.script))
                            {
                                // FindPickUp returns null for a script whose PickUpModel_* type is
                                // gone (removed companion mod, renamed class). Keep the node.
                                PickUpModelBase pickUp = LogLikeMod.FindPickUp(stageInfo.script);
                                if (pickUp != null)
                                    pickUp.LoadFromSaveData(stageInfo);
                                else
                                    Debug.LogWarning($"[RMR] LoadOptionalRunState: no PickUpModel for script '{stageInfo.script}'; stage kept without pickup state.");
                            }
                            logueStageInfoList.Add(stageInfo);
                        }
                    }
                    LogueBookModels.RemainStageList.Add(key, logueStageInfoList);
                }
            }
        }

        public static void LoadFromSaveData(SaveData save)
        {
            LogueBookModels.CreatePlayer();
            LogueBookModels.CreatePlayerBattle();
            if (save.GetData("SubPlayerNum").GetIntSelf() > 0)
            {
                for (int index = 0; index < save.GetData("SubPlayerNum").GetIntSelf(); ++index)
                    LogueBookModels.AddSubPlayer();
            }
            int index1 = 0;
            foreach (SaveData data in save.GetData("playerBattleModel"))
            {
                data.Log("CURRENTLY INITIALIZING UNIT NUMBER: " + index1.ToString());
                // Per-unit guard: one librarian whose saved deck references content that no longer
                // resolves must not abort the whole load. Losing that unit's deck is recoverable in
                // the prep screen; losing the run is not.
                try
                {
                    if (index1 < LogueBookModels.playerBattleModel.Count)
                        LogueBookModels.LoadFromSaveData_UnitBattleDataModel(data, LogueBookModels.playerBattleModel[index1]);
                    else
                        Debug.LogWarning($"[RMR] LoadFromSaveData: save has more units than the current party ({index1}); entry skipped.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[RMR] LoadFromSaveData: unit {index1} failed to restore; continuing with the rest of the save. {ex}");
                }
                ++index1;
            }
            // Everything between the party and the inventory is optional run state. It used to run
            // unguarded, so one bad entry (a third-party assembly failing GetTypes, a missing pickup
            // script) aborted the whole method — and cardlist/booklist, which load LAST, stayed at the
            // empty lists CreatePlayer() had just reset them to. The run then looked like every key
            // page and combat page had been deleted. Losing some optional state is recoverable;
            // losing the inventory is not, so this must never take the inventory down with it.
            try
            {
                LoadOptionalRunState(save);
            }
            catch (Exception ex)
            {
                Debug.LogError("[RMR] LoadFromSaveData: optional run state failed to load; inventory load continues. " + ex);
            }
            LogueBookModels.EnsureRemainStageListIntegrity();
            foreach (SaveData saveData in save.GetData("cardlist"))
                LogueBookModels.AddCard(
                    RestoreSavedCombatCardForLoad(ExtensionUtils.LogLoadFromSaveData(saveData.GetData("id"))),
                    saveData.GetData("num").GetIntSelf(),
                    false);
            foreach (SaveData data3 in save.GetData("booklist"))
            {
                BookModel model = LogueBookModels.LoadFromSaveData_BookModel(data3);
                LogueBookModels.booklist.Add(model);
                LogueBookModels.RecordCompendiumRoleBook(model.ClassInfo.id);
            }
            LogueBookModels.LoadCompendiumUnlockSet(save.GetData("atlasRoleBookUnlocks"), LogueBookModels.CompendiumUnlockedRoleBooks);
            LogueBookModels.LoadCompendiumUnlockSet(save.GetData("atlasBattleCardUnlocks"), LogueBookModels.CompendiumUnlockedBattleCards);
            LogueBookModels.LoadCompendiumUnlockSet(save.GetData("atlasAbnormalityPageUnlocks"), LogueBookModels.CompendiumUnlockedAbnormalityPages);
            LogueBookModels.LoadCompendiumUnlockSet(save.GetData("atlasEgoPageUnlocks"), LogueBookModels.CompendiumUnlockedEgoPages);
            LogueBookModels.SyncCurrentInventoryToPermanentAtlas();
            PruneCorePageExclusiveBattleCardsFromInventoryAndAtlas();
            PruneInvalidPermanentAbnormalityCompendiumUnlocks();
            RMRAbnormalityUnlockManager.LoadRouteUnlocks(save.GetData("RMRAbnormalityUnlocks"));
            LogueBookModels.nextinstanceid = save.GetInt("nextinstanceid");
        }

        /// <summary>
        /// Rebuilds a dynamic upgraded combat-page definition before a Continue Run restores it.
        /// Upgrade IDs are persisted in the save, but their ItemXmlDataList entry exists only in
        /// memory and is absent after restarting the game. AddCard intentionally ignores an ID it
        /// cannot resolve, which previously made every saved upgraded page disappear on Continue.
        /// </summary>
        private static LorId RestoreSavedCombatCardForLoad(LorId savedId)
        {
            if (savedId == null || savedId == LorId.None)
                return savedId;
            if (!UpgradeMetadata.UnpackPid(savedId.packageId, out UpgradeMetadata metadata))
                return savedId;

            try
            {
                DiceCardXmlInfo restored = Singleton<LogCardUpgradeManager>.Instance.GetUpgradeCard(
                    savedId.GetOriginalId(), metadata.index, metadata.count);
                if (restored?.id != null)
                    return restored.id;
                Debug.LogError($"[RMR Save] Failed to rebuild saved upgraded combat page: {savedId.packageId}:{savedId.id}.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RMR Save] Failed to rebuild saved upgraded combat page {savedId.packageId}:{savedId.id}: {ex.Message}");
            }
            return savedId;
        }

        /// <summary>
        /// Permanent Compendium data can be loaded at the invitation hub without loading a
        /// route save first. Dynamic upgrade definitions only exist in ItemXmlDataList for
        /// the current process, so rebuild every recorded upgraded ID before a realization
        /// projects the Compendium into its temporary combat inventory.
        /// </summary>
        public static void RestoreCompendiumUpgradeDefinitions()
        {
            LogueBookModels.EnsureCompendiumUnlocks();
            if (LogueBookModels.CompendiumUnlockedBattleCards == null
                || LogueBookModels.CompendiumUnlockedBattleCards.Count == 0)
                return;

            var replacements = new List<KeyValuePair<LorId, LorId>>();
            int restoredCount = 0;
            foreach (LorId savedId in LogueBookModels.CompendiumUnlockedBattleCards.ToList())
            {
                if (savedId == null || savedId == LorId.None
                    || string.IsNullOrEmpty(savedId.packageId)
                    || !UpgradeMetadata.UnpackPid(savedId.packageId, out UpgradeMetadata metadata))
                    continue;
                try
                {
                    DiceCardXmlInfo restored = Singleton<LogCardUpgradeManager>.Instance.GetUpgradeCard(
                        savedId.GetOriginalId(), metadata.index, metadata.count);
                    if (restored?.id == null)
                    {
                        Debug.LogWarning($"[RMR Atlas] Failed to rebuild Compendium upgraded page {savedId}.");
                        continue;
                    }
                    restoredCount++;
                    if (restored.id != savedId)
                        replacements.Add(new KeyValuePair<LorId, LorId>(savedId, restored.id));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RMR Atlas] Failed to rebuild Compendium upgraded page {savedId}: {ex.Message}");
                }
            }

            foreach (KeyValuePair<LorId, LorId> replacement in replacements)
            {
                LogueBookModels.CompendiumUnlockedBattleCards.Remove(replacement.Key);
                LogueBookModels.CompendiumUnlockedBattleCards.Add(replacement.Value);
            }
            if (restoredCount > 0)
                Debug.Log($"[RMR Atlas] Rebuilt {restoredCount} upgraded combat page definition(s) from permanent Compendium.");
        }

        private static void EnsureRemainStageListIntegrity()
        {
            if (LogueBookModels.RemainStageList == null)
                LogueBookModels.RemainStageList = new Dictionary<ChapterGrade, List<LogueStageInfo>>();
            ChapterGrade[] grades =
            {
                ChapterGrade.Grade1,
                ChapterGrade.Grade2,
                ChapterGrade.Grade3,
                ChapterGrade.Grade4,
                ChapterGrade.Grade5,
                ChapterGrade.Grade6,
                ChapterGrade.Grade7
            };
            foreach (ChapterGrade grade in grades)
            {
                if (!LogueBookModels.RemainStageList.TryGetValue(grade, out List<LogueStageInfo> stages) || stages == null)
                {
                    LogueBookModels.RemainStageList[grade] = new List<LogueStageInfo>();
                    continue;
                }
                stages.RemoveAll(stage =>
                {
                    StageClassInfo stageClassInfo = stage == null ? null : StageClassInfoList.Instance.GetData(stage.Id);
                    return stageClassInfo == null || stageClassInfo.waveList == null || stageClassInfo.waveList.Count == 0;
                });
            }
        }

        public static void CreateStageDesc(LogueStageInfo info)
        {
            if (info == null)
                return;
            EmotionCardXmlInfo registeredPickUpXml = LogLikeMod.GetRegisteredPickUpXml(info);
            if (registeredPickUpXml == null || string.IsNullOrEmpty(registeredPickUpXml.Name))
                return;
            // Stage pick keys (workshopid_stageid) are not in vanilla AbnormalityCard XML.
            // GetAbnormalityCard returns a transient "Not found" stub that is NOT cached, so
            // mutating it is a no-op for the UI. Upsert a real dictionary entry instead.
            AbnormalityCard abnormalityCard = PickUpModel_RMRVanillaEmotion.EnsureDescEntry(registeredPickUpXml.Name);
            if (abnormalityCard == null)
                return;
            abnormalityCard.flavorText = "";
            if (info.type == StageType.Normal)
            {
                registeredPickUpXml._artwork = "Stage_Normal";
                abnormalityCard.cardName = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("Stage_Normal");
                abnormalityCard.abilityDesc = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("Stage_Normal_Desc");
            }
            if (info.type == StageType.Elite)
            {
                registeredPickUpXml._artwork = "Stage_Elite";
                abnormalityCard.cardName = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("Stage_Elite");
                abnormalityCard.abilityDesc = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("Stage_Elite_Desc");
            }
            if (info.type == StageType.Boss)
            {
                registeredPickUpXml._artwork = "Stage_Boss";
                abnormalityCard.cardName = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("Stage_Boss");
                abnormalityCard.abilityDesc = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("Stage_Boss_Desc");
            }
            if (info.type == StageType.Mystery)
            {
                registeredPickUpXml._artwork = "Stage_Mystery";
                abnormalityCard.cardName = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("Stage_Mystery");
                abnormalityCard.abilityDesc = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("Stage_Mystery_Desc");
            }
            if (info.type == StageType.Creature)
            {
                registeredPickUpXml._artwork = "异想体战斗";
                abnormalityCard.cardName = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("Stage_Creature");
                abnormalityCard.abilityDesc = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("Stage_Creature_Desc");
            }
            if (info.type == StageType.Shop)
            {
                registeredPickUpXml._artwork = "Stage_Shop";
                abnormalityCard.cardName = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("Stage_Shop");
                abnormalityCard.abilityDesc = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("Stage_Shop_Desc");
            }
            if (info.type == StageType.Rest)
            {
                registeredPickUpXml._artwork = "Stage_Rest";
                abnormalityCard.cardName = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("Stage_Rest");
                abnormalityCard.abilityDesc = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("Stage_Rest_Desc");
            }
            if (string.IsNullOrEmpty(info.script))
                return;
            PickUpModelBase pickUp = LogLikeMod.FindPickUp(info.script);
            if (pickUp == null)
                return;
            registeredPickUpXml._artwork = string.IsNullOrEmpty(pickUp.ArtWork) ? info.script : pickUp.ArtWork;
            abnormalityCard.cardName = pickUp.Name;
            abnormalityCard.abilityDesc = pickUp.Desc;
            abnormalityCard.flavorText = pickUp.FlaverText;
        }

        #endregion

        #region --- Inventory & reward helpers ---

        public static bool CheckIsCanAdd(RewardPassiveInfo passive)
        {
            if (passive.passivetype == RewardPassiveType.Nolimit)
                return true;
            PickUpModelBase pickUp = LogLikeMod.FindPickUp(LogLikeMod.GetRegisteredPickUpXml(passive).Script[0]);
            pickUp.id = passive.id;
            pickUp.rewardinfo = passive;
            if (pickUp.GetPickupTarget() != null)
            {
                foreach (BattleUnitModel battleUnitModel in pickUp.GetPickupTarget())
                {
                    if (pickUp.IsCanPickUp(battleUnitModel.UnitData.unitData))
                        return true;
                }
                return false;
            }
            foreach (UnitDataModel target in LogueBookModels.playerModel)
            {
                if (pickUp.IsCanPickUp(target))
                    return true;
            }
            return false;
        }

        public static List<DiceCardItemModel> GetCardListForInven()
        {
            // Vanilla EGO pages are personalEgo / emotion-EGO only — never deck inventory.
            try { RogueLike_Mod_Reborn.RMRPrepareRestrictions.StripEgoPagesFromPlayerDecks(); } catch { }

            var ids = new List<DiceCardXmlInfo>();
            if (RMRCore.CurrentGamemode != null && RMRCore.CurrentGamemode.ReplaceBaseDeck)
            {
                foreach (var card in DeckXmlList.Instance.GetData(RMRCore.CurrentGamemode.BaseDeckReplacement).cardIdList)
                {
                    if (ids.Count(x => x.id == card.id) == 0)
                        ids.Add(ItemXmlDataList.instance.GetCardItem(card, false));
                }
            }
            else ids.AddRange(new List<DiceCardXmlInfo>
            {
                ItemXmlDataList.instance.GetCardItem(1, false),
                ItemXmlDataList.instance.GetCardItem(2, false),
                ItemXmlDataList.instance.GetCardItem(3, false),
                ItemXmlDataList.instance.GetCardItem(4, false),
                ItemXmlDataList.instance.GetCardItem(5, false)
            });
            List<DiceCardItemModel> list3 = new List<DiceCardItemModel>();
            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] == null || !RogueLike_Mod_Reborn.RMRPrepareRestrictions.IsCardAllowedInCurrentPrepare(ids[i]))
                    continue;
                list3.Add(new DiceCardItemModel(ids[i])
                {
                    num = 99
                });
            }
            if (LogueBookModels.cardlist != null)
            {
                foreach (DiceCardItemModel card in LogueBookModels.cardlist)
                {
                    if (card?.ClassInfo == null)
                        continue;
                    if (!RogueLike_Mod_Reborn.RMRPrepareRestrictions.IsCardAllowedInCurrentPrepare(card.ClassInfo))
                        continue;
                    card.num = LogueBookModels.UNLOCKED_CARD_COUNT;
                    list3.Add(card);
                }
            }
            list3.Sort(new Comparison<DiceCardItemModel>(SortUtil.CardItemCompByCost));
            return list3;
        }

        public static List<DiceCardItemModel> GetCardList(bool RemoveBasic = false, bool Decks = false)
        {
            LogueBookModels.PurgeBaseCardsWhenUpgradeExists();
            List<DiceCardItemModel> list = LogueBookModels.cardlist ?? new List<DiceCardItemModel>();
            List<DiceCardItemModel> list2 = new List<DiceCardItemModel>();
            var ids = new List<DiceCardXmlInfo>();
            if (!RemoveBasic)
            {
                try
                {
                    if (RMRCore.CurrentGamemode != null && RMRCore.CurrentGamemode.ReplaceBaseDeck)
                    {
                        var deck = DeckXmlList.Instance?.GetData(RMRCore.CurrentGamemode.BaseDeckReplacement);
                        if (deck?.cardIdList != null)
                        {
                            foreach (var card in deck.cardIdList)
                            {
                                if (ids.Count(x => x != null && x.id == card.id) == 0)
                                    ids.Add(ItemXmlDataList.instance.GetCardItem(card, false));
                            }
                        }
                    }
                    else
                    {
                        ids.AddRange(new List<DiceCardXmlInfo>
                        {
                            ItemXmlDataList.instance.GetCardItem(1, false),
                            ItemXmlDataList.instance.GetCardItem(2, false),
                            ItemXmlDataList.instance.GetCardItem(3, false),
                            ItemXmlDataList.instance.GetCardItem(4, false),
                            ItemXmlDataList.instance.GetCardItem(5, false)
                        });
                    }
                }
                catch
                {
                    // Hub/atlas before gamemode ready — skip basic cards rather than NRE.
                }
                foreach (DiceCardXmlInfo xmlData in ids)
                {
                    if (xmlData == null)
                        continue;
                    list2.Add(new DiceCardItemModel(xmlData)
                    {
                        num = 99
                    });
                }
            }
            foreach (DiceCardItemModel diceCardItemModel in list)
            {
                if (diceCardItemModel?.ClassInfo == null)
                    continue;
                list2.Add(new DiceCardItemModel(diceCardItemModel.ClassInfo)
                {
                    num = LogueBookModels.UNLOCKED_CARD_COUNT
                });
            }
            if (Decks && LogueBookModels.playerModel != null)
            {
                foreach (UnitDataModel unitDataModel in LogueBookModels.playerModel)
                {
                    if (unitDataModel?.bookItem == null)
                        continue;
                    List<DiceCardXmlInfo> deckCards = unitDataModel.bookItem.GetCardListFromCurrentDeck();
                    if (deckCards == null)
                        continue;
                    using (List<DiceCardXmlInfo>.Enumerator enumerator4 = deckCards.GetEnumerator())
                    {
                        while (enumerator4.MoveNext())
                        {
                            DiceCardXmlInfo deckcard = enumerator4.Current;
                            if (deckcard == null)
                                continue;
                            DiceCardItemModel diceCardItemModel2 = list2.Find((DiceCardItemModel x) => x?.ClassInfo != null && x.ClassInfo.id == deckcard.id);
                            if (diceCardItemModel2 != null)
                            {
                                diceCardItemModel2.num++;
                            }
                        }
                    }
                }
            }
            list2.RemoveAll((DiceCardItemModel x) => x == null || x.num <= 0);
            list2.Sort(new Comparison<DiceCardItemModel>(SortUtil.CardItemCompByCost));
            return list2;
        }

        public static void AddBook(LorId id)
        {
            BookModel bookModel = new BookModel(Singleton<BookXmlList>.Instance.GetData(id));
            bookModel.instanceId = LogueBookModels.nextinstanceid++;
            bookModel.TryGainUniquePassive();
            LogueBookModels.booklist.Add(bookModel);
            LogueBookModels.RecordCompendiumRoleBook(id);
        }

        public static bool TryAddUniqueRoleBookToInventoryAndCompendium(LorId id)
        {
            id = RMRCore.NormalizeLegacyBlueReverberationCorePageId(id);
            if (id == LorId.None)
                return false;
            BookXmlInfo bookXml = RewardingModel.GetBookDataOriginAware(id);
            if (bookXml == null)
                return false;

            bool added = false;
            if (LogueBookModels.booklist == null)
                LogueBookModels.booklist = new List<BookModel>();
            if (!LogueBookModels.booklist.Any(book => book?.ClassInfo?.id == id))
            {
                BookModel bookModel = new BookModel(bookXml);
                bookModel.instanceId = LogueBookModels.nextinstanceid++;
                bookModel.TryGainUniquePassive();
                LogueBookModels.booklist.Add(bookModel);
                added = true;
            }

            LogueBookModels.EnsureCompendiumUnlocks();
            bool recordsPermanently = RMRCore.ShouldRecordRoleBookInPermanentCompendium(bookXml);
            if (recordsPermanently && !LogueBookModels.CompendiumUnlockedRoleBooks.Contains(id))
            {
                LogueBookModels.CompendiumUnlockedRoleBooks.Add(id);
                added = true;
            }
            return added;
        }

        public static bool HasGrade6SpecialBuiltInDeck(BookModel model)
        {
            return TryGetGrade6SpecialBuiltInDeckSource(model, out LorId _);
        }

        public static bool IsEditableBlueReverberationDeck(BookModel model)
        {
            return model != null && RMRCore.IsBlueReverberationCorePage(model.ClassInfo);
        }

        public static bool TryGetGrade6SpecialBuiltInDeckCards(UnitDataModel model, out List<DiceCardXmlInfo> builtInDeck)
        {
            if (model == null || !TryGetGrade6SpecialBuiltInDeckSource(model, out LorId sourceId))
            {
                builtInDeck = new List<DiceCardXmlInfo>();
                return false;
            }
            return TryLoadGrade6SpecialBuiltInDeckCards(sourceId, out builtInDeck);
        }

        public static bool TryGetGrade6SpecialBuiltInDeckCards(BookModel model, out List<DiceCardXmlInfo> builtInDeck)
        {
            if (!TryGetGrade6SpecialBuiltInDeckSource(model, out LorId sourceId))
            {
                builtInDeck = new List<DiceCardXmlInfo>();
                return false;
            }
            return TryLoadGrade6SpecialBuiltInDeckCards(sourceId, out builtInDeck);
        }

        private static void EnsureGrade6SpecialBuiltInDeckSource()
        {
            if (Grade6SpecialBuiltInDeckSource == null)
                Grade6SpecialBuiltInDeckSource = new Dictionary<UnitDataModel, LorId>();
        }

        private static bool TryGetGrade6SpecialBuiltInDeckSource(UnitDataModel model, out LorId sourceId)
        {
            sourceId = LorId.None;
            if (model == null)
                return false;
            EnsureGrade6SpecialBuiltInDeckSource();
            if (RMRCore.IsBlueReverberationCorePage(model.bookItem?.ClassInfo))
            {
                Grade6SpecialBuiltInDeckSource.Remove(model);
                return false;
            }
            if (Grade6SpecialBuiltInDeckSource.TryGetValue(model, out sourceId) && sourceId != LorId.None)
            {
                if (!IsBlueReverberationDeckSource(sourceId))
                    return true;
                Grade6SpecialBuiltInDeckSource.Remove(model);
                sourceId = LorId.None;
            }
            return TryResolveGrade6SpecialBuiltInDeckSource(model.bookItem?.ClassInfo, out sourceId);
        }

        private static bool TryGetGrade6SpecialBuiltInDeckSource(BookModel model, out LorId sourceId)
        {
            sourceId = LorId.None;
            if (model == null)
                return false;
            if (RMRCore.IsBlueReverberationCorePage(model.ClassInfo))
                return false;
            if (model.owner != null && TryGetGrade6SpecialBuiltInDeckSource(model.owner, out sourceId))
                return true;
            return TryResolveGrade6SpecialBuiltInDeckSource(model.ClassInfo, out sourceId);
        }

        private static bool TryResolveGrade6SpecialBuiltInDeckSource(BookXmlInfo page, out LorId sourceId)
        {
            sourceId = LorId.None;
            if (!IsGrade6SpecialBuiltInDeckPage(page))
                return false;
            if (TryGetKnownVanillaFixedDeckSource(page, out sourceId))
                return true;
            LorId deckId = page.DeckId;
            if (deckId != LorId.None && DeckXmlList.Instance.GetData(deckId) != null)
            {
                sourceId = deckId;
                return true;
            }
            if (HasLoadableBuiltInDeckOnlyCards(page))
            {
                sourceId = page.id;
                return true;
            }
            return false;
        }

        private static bool TryGetKnownVanillaFixedDeckSource(BookXmlInfo page, out LorId sourceId)
        {
            sourceId = LorId.None;
            int deckId = 0;
            if (RMRCore.IsBinahCorePage(page))
                deckId = 8;
            else if (IsBlackSilenceCorePage(page))
                deckId = 102;
            if (deckId == 0)
                return false;
            LorId fixedDeckId = new LorId(deckId);
            sourceId = fixedDeckId;
            return true;
        }

        private static bool TryLoadGrade6SpecialBuiltInDeckCards(LorId sourceId, out List<DiceCardXmlInfo> builtInDeck)
        {
            builtInDeck = new List<DiceCardXmlInfo>();
            DeckXmlInfo deck = DeckXmlList.Instance.GetData(sourceId);
            if (deck?.cardIdList != null && deck.cardIdList.Count > 0)
            {
                foreach (LorId cardId in deck.cardIdList)
                {
                    DiceCardXmlInfo card = ItemXmlDataList.instance.GetCardItem(cardId, true);
                    if (card != null)
                        builtInDeck.Add(card);
                }
                if (builtInDeck.Count > 0)
                {
                    ApplyBinahCardUpgradesInPlace(builtInDeck);
                    return true;
                }
            }

            if (TryLoadKnownVanillaFixedDeckCards(sourceId, out builtInDeck))
            {
                ApplyBinahCardUpgradesInPlace(builtInDeck);
                return true;
            }

            BookXmlInfo sourcePage = Singleton<BookXmlList>.Instance.GetData(sourceId);
            if (!IsGrade6SpecialBuiltInDeckPage(sourcePage))
                return false;
            if (!TryLoadBuiltInDeckFromOnlyCards(sourcePage, out builtInDeck))
                return false;
            ApplyBinahCardUpgradesInPlace(builtInDeck);
            return true;
        }

        public static bool IsBinahDegradedCardId(int cardId)
        {
            return BinahDegradedToFullCardIds.ContainsKey(cardId);
        }

        public static bool IsBinahDegradedUpgradeable(LorId cardId)
        {
            if (cardId == null || cardId == LorId.None)
                return false;
            int bare = cardId.GetOriginalId().id;
            return BinahDegradedToFullCardIds.ContainsKey(bare)
                && !BinahUpgradedDegradedCardIds.Contains(bare);
        }

        public static bool TryGetBinahFullUpgradeCard(LorId degradedId, out DiceCardXmlInfo fullCard)
        {
            fullCard = null;
            if (!IsBinahDegradedUpgradeable(degradedId))
                return false;
            int bare = degradedId.GetOriginalId().id;
            if (!BinahDegradedToFullCardIds.TryGetValue(bare, out int fullId))
                return false;
            fullCard = ItemXmlDataList.instance.GetCardItem(fullId, true)
                ?? ItemXmlDataList.instance.GetCardItem(new LorId(fullId), true);
            return fullCard != null;
        }

        /// <summary>
        /// Lists unique degraded Binah combat pages that can still be upgraded to full versions.
        /// Only offered when a player unit currently holds a Binah fixed deck.
        /// </summary>
        public static List<DiceCardItemModel> GetBinahDegradedUpgradeableCards()
        {
            var result = new List<DiceCardItemModel>();
            if (playerModel == null || playerModel.Count == 0)
                return result;
            if (!playerModel.Any(unit => unit?.bookItem?.ClassInfo != null
                    && (RMRCore.IsBinahCorePage(unit.bookItem.ClassInfo)
                        || (TryGetGrade6SpecialBuiltInDeckSource(unit, out LorId sourceId) && sourceId != null && sourceId.id == 8))))
                return result;

            foreach (KeyValuePair<int, int> pair in BinahDegradedToFullCardIds)
            {
                if (BinahUpgradedDegradedCardIds.Contains(pair.Key))
                    continue;
                DiceCardXmlInfo card = ItemXmlDataList.instance.GetCardItem(new LorId(LogLikeMod.ModId, pair.Key), true)
                    ?? ItemXmlDataList.instance.GetCardItem(pair.Key, true);
                if (card == null)
                    continue;
                result.Add(new DiceCardItemModel(card)
                {
                    num = 1
                });
            }
            return result;
        }

        /// <summary>
        /// Marks a degraded Binah page type as upgraded and refreshes fixed decks so all copies become full.
        /// </summary>
        public static bool TryApplyBinahDegradedCardUpgrade(LorId degradedId)
        {
            if (!IsBinahDegradedUpgradeable(degradedId))
                return false;
            int bare = degradedId.GetOriginalId().id;
            if (!BinahDegradedToFullCardIds.TryGetValue(bare, out int fullBare))
                return false;
            BinahUpgradedDegradedCardIds.Add(bare);
            EnsureGrade6SpecialBuiltInDeckSource();
            foreach (KeyValuePair<UnitDataModel, LorId> kv in Grade6SpecialBuiltInDeckSource.ToList())
            {
                if (kv.Key != null && kv.Value != null && kv.Value != LorId.None)
                    TryApplyGrade6SpecialBuiltInDeckToUnit(kv.Key, kv.Value);
            }
            Debug.Log($"[RMR] Binah degraded page upgraded: {bare} -> {fullBare}");
            return true;
        }

        private static void ApplyBinahCardUpgradesInPlace(List<DiceCardXmlInfo> deck)
        {
            if (deck == null || deck.Count == 0 || BinahUpgradedDegradedCardIds == null || BinahUpgradedDegradedCardIds.Count == 0)
                return;
            for (int i = 0; i < deck.Count; i++)
            {
                DiceCardXmlInfo card = deck[i];
                if (card?.id == null)
                    continue;
                int bare = card.id.GetOriginalId().id;
                if (!BinahUpgradedDegradedCardIds.Contains(bare))
                    continue;
                if (!BinahDegradedToFullCardIds.TryGetValue(bare, out int fullId))
                    continue;
                DiceCardXmlInfo full = ItemXmlDataList.instance.GetCardItem(fullId, true)
                    ?? ItemXmlDataList.instance.GetCardItem(new LorId(fullId), true);
                if (full != null)
                    deck[i] = full;
            }
        }

        private static bool TryLoadKnownVanillaFixedDeckCards(LorId sourceId, out List<DiceCardXmlInfo> builtInDeck)
        {
            builtInDeck = new List<DiceCardXmlInfo>();
            int[] cardIds = null;
            if (sourceId.id == 8)
                cardIds = new[] { 607201, 607202, 607202, 607202, 607203, 607204, 607204, 607205, 607205 };
            else if (sourceId.id == 102)
                cardIds = new[] { 702001, 702002, 702003, 702004, 702005, 702006, 702007, 702008, 702009 };
            if (cardIds == null)
                return false;

            foreach (int cardId in cardIds)
            {
                DiceCardXmlInfo card = ItemXmlDataList.instance.GetCardItem(new LorId(LogLikeMod.ModId, cardId), true)
                    ?? ItemXmlDataList.instance.GetCardItem(cardId, true);
                if (card != null)
                    builtInDeck.Add(card);
            }
            return builtInDeck.Count == cardIds.Length;
        }

        private static bool HasLoadableBuiltInDeckOnlyCards(BookXmlInfo page)
        {
            return TryLoadBuiltInDeckFromOnlyCards(page, out List<DiceCardXmlInfo> cards)
                && cards.Count > 0;
        }

        private static bool TryLoadBuiltInDeckFromOnlyCards(BookXmlInfo page, out List<DiceCardXmlInfo> builtInDeck)
        {
            builtInDeck = new List<DiceCardXmlInfo>();
            if (page?.EquipEffect?.OnlyCard == null || page.EquipEffect.OnlyCard.Count == 0)
                return false;

            foreach (int cardId in page.EquipEffect.OnlyCard)
            {
                DiceCardXmlInfo card = ItemXmlDataList.instance.GetCardItem(new LorId(page.workshopID, cardId), true)
                    ?? ItemXmlDataList.instance.GetCardItem(cardId, true);
                if (card != null)
                    builtInDeck.Add(card);
            }
            return builtInDeck.Count > 0;
        }

        private static bool IsGrade6SpecialBuiltInDeckPage(BookXmlInfo page)
        {
            if (page == null)
                return false;
            if (RMRCore.IsBlueReverberationCorePage(page))
                return false;
            bool isBinah = RMRCore.IsBinahCorePage(page);
            return IsDeckFixedBookCategory(page)
                || IsBlackSilenceCorePage(page)
                || isBinah;
        }

        /// <summary>
        /// These pages are unlocked by RMR's Urban Star progression even when their
        /// vanilla XML chapter is Impurity or their data comes from an RMR reward.
        /// Use this shared classifier for realization prepare filters.
        /// </summary>
        public static bool IsUrbanStarSpecialCorePage(BookXmlInfo page)
        {
            return IsBlackSilenceCorePage(page)
                || RMRCore.IsBinahCorePage(page)
                || IsRedMistCorePage(page)
                || RMRCore.IsBlueReverberationCorePage(page);
        }

        private static BookXmlInfo ResolveFreshEquipPage(BookXmlInfo page)
        {
            if (page == null || (!IsGrade6SpecialBuiltInDeckPage(page) && !RMRCore.IsBlueReverberationCorePage(page)))
                return page;
            return RewardingModel.GetBookDataOriginAware(page.id) ?? page;
        }

        private static bool IsBlueReverberationDeckSource(LorId sourceId)
        {
            if (sourceId == null || sourceId == LorId.None)
                return false;
            sourceId = RMRCore.NormalizeLegacyBlueReverberationCorePageId(sourceId);
            if (sourceId == RMRCore.GetBlueReverberationCorePageLorId())
                return true;
            return RMRCore.IsBlueReverberationCorePage(RewardingModel.GetBookDataOriginAware(sourceId));
        }

        private static bool IsDeckFixedBookCategory(BookXmlInfo page)
        {
            // Use reflection so builds without a public BookCategory.DeckFixed member still load.
            try
            {
                object category = AccessTools.Field(page.GetType(), "category")?.GetValue(page)
                    ?? AccessTools.Field(page.GetType(), "_category")?.GetValue(page)
                    ?? AccessTools.Property(page.GetType(), "Category")?.GetValue(page, null)
                    ?? AccessTools.Property(page.GetType(), "BookCategory")?.GetValue(page, null);
                return category != null
                    && category.ToString().IndexOf("DeckFixed", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsBlackSilenceCorePage(BookXmlInfo page)
        {
            return page != null
                && (page.TextId == 102
                    || (!string.IsNullOrEmpty(page.InnerName)
                        && page.InnerName.IndexOf("BlackSilence", StringComparison.OrdinalIgnoreCase) >= 0)
                    || (!string.IsNullOrEmpty(page._bookIcon)
                        && page._bookIcon.IndexOf("BlackSilence", StringComparison.OrdinalIgnoreCase) >= 0)
                    || (page.CharacterSkin != null
                        && page.CharacterSkin.Any(skin => !string.IsNullOrEmpty(skin)
                            && skin.IndexOf("BlackSilence", StringComparison.OrdinalIgnoreCase) >= 0)));
        }

        public static bool IsRedMistCorePage(BookXmlInfo page)
        {
            return page != null
                && (page.id.id == 250022
                    || page.TextId == 250022
                    || (!string.IsNullOrEmpty(page.InnerName)
                        && (page.InnerName.IndexOf("RedMist", StringComparison.OrdinalIgnoreCase) >= 0
                            || page.InnerName.IndexOf("Red Mist", StringComparison.OrdinalIgnoreCase) >= 0))
                    || (!string.IsNullOrEmpty(page._bookIcon)
                        && (page._bookIcon.IndexOf("RedMist", StringComparison.OrdinalIgnoreCase) >= 0
                            || page._bookIcon.IndexOf("TheRedMist", StringComparison.OrdinalIgnoreCase) >= 0))
                    || (page.CharacterSkin != null
                        && page.CharacterSkin.Any(skin => !string.IsNullOrEmpty(skin)
                            && (skin.IndexOf("RedMist", StringComparison.OrdinalIgnoreCase) >= 0
                                || skin.IndexOf("TheRedMist", StringComparison.OrdinalIgnoreCase) >= 0))));
        }

        private static void RestoreGrade6SpecialBuiltInDeckSource(UnitDataModel unitData, SaveData data)
        {
            if (unitData == null || data == null)
                return;
            EnsureGrade6SpecialBuiltInDeckSource();
            if (RMRCore.IsBlueReverberationCorePage(unitData.bookItem?.ClassInfo))
            {
                Grade6SpecialBuiltInDeckSource.Remove(unitData);
                return;
            }
            try
            {
                SaveData sourceData = data.GetData("Grade6SpecialBuiltInDeckSource");
                if (sourceData == null)
                    return;
                LorId sourceId = RMRCore.NormalizeLegacyBlueReverberationCorePageId(ExtensionUtils.LogLoadFromSaveData(sourceData));
                if (IsBlueReverberationDeckSource(sourceId))
                {
                    Grade6SpecialBuiltInDeckSource.Remove(unitData);
                    return;
                }
                if (sourceId != LorId.None)
                    TryApplyGrade6SpecialBuiltInDeckToUnit(unitData, sourceId);
            }
            catch
            {
            }
        }

        public static void AddUpgradeCard(LorId cardid, bool callInvenChangeEvent = true) =>
            LogueBookModels.AddCard(Singleton<LogCardUpgradeManager>.Instance.GetUpgradeCard(cardid).id, 1, callInvenChangeEvent);

        /// <summary>
        /// Removes base versions of cards from cardlist when an upgraded version exists.
        /// Ensures "once upgraded, always upgraded" — base cards are replaced by their upgraded counterparts.
        /// </summary>
        /// <summary>True when the id carries the upgrade marker in its packageId.</summary>
        private static bool IsUpgradedCardId(LorId id)
        {
            try
            {
                return id != null
                    && !string.IsNullOrEmpty(id.packageId)
                    && id.packageId.Contains(LogCardUpgradeManager.UpgradeKeyword);
            }
            catch { return true; }   // unsure -> keep the old always-purge behaviour
        }

        public static void PurgeBaseCardsWhenUpgradeExists()
        {
            // Invitation hub / pre-run atlas may call GetCardList before a run initializes cardlist.
            if (LogueBookModels.cardlist == null || LogueBookModels.cardlist.Count == 0)
                return;

            var upgradedIds = new HashSet<LorId>();
            // Collect all upgraded cards
            foreach (var card in LogueBookModels.cardlist)
            {
                if (card == null)
                    continue;
                try
                {
                    LorId id = card.GetID();
                    if (id == null || string.IsNullOrEmpty(id.packageId))
                        continue;
                    if (id.packageId.Contains(LogCardUpgradeManager.UpgradeKeyword))
                    {
                        LorId originalId = id.GetOriginalId();
                        if (originalId != id) // has a different base
                            upgradedIds.Add(originalId);
                    }
                }
                catch { /* skip bad card entry */ }
            }
            // Remove base versions that have upgraded counterparts
            if (upgradedIds.Count > 0)
                LogueBookModels.cardlist.RemoveAll(x => x != null && upgradedIds.Contains(x.GetID()));
        }

        /// <summary>
        /// Normalize a card ID to a stable key for ownership comparison.
        /// - Calls GetOriginalId() to strip &lt;LogUpgrade&gt; markers
        /// - Normalizes packageId: empty / "@origin" → "BaseGame"
        /// - Returns key like "BaseGame:204001"
        /// </summary>
        public static string NormalizeCardKey(LorId id)
        {
            if (id == null || id == LorId.None)
                return "None:0";
            LorId baseId = id.GetOriginalId();
            string pkg = baseId.packageId ?? "";
            if (string.IsNullOrEmpty(pkg) || pkg == "@origin")
                pkg = "BaseGame";
            return pkg + ":" + baseId.id.ToString();
        }

        /// <summary>
        /// Check if a combat page (battle card) has already been obtained.
        /// Uses normalized ID comparison — if player owns base OR upgraded version, returns true.
        /// </summary>
        public static bool HasOwnedCombatPage(LorId id)
        {
            if (LogueBookModels.cardlist == null) return false;
            string targetKey = NormalizeCardKey(id);
            return LogueBookModels.cardlist.Any(x => NormalizeCardKey(x.GetID()) == targetKey);
        }

        /// <summary>
        /// Check if a book/key page has already been obtained.
        /// Uses booklist.
        /// </summary>
        public static bool HasOwnedBookPage(LorId id)
        {
            if (LogueBookModels.booklist == null) return false;
            if (id == null || id == LorId.None) return false;
            string key = NormalizeCardKey(id);
            return LogueBookModels.booklist.Any(x =>
                x?.ClassInfo?.id == id || NormalizeCardKey(x?.ClassInfo?.id) == key);
        }

        /// <summary>
        /// Check if a RewardPassiveInfo has already been obtained.
        /// Handles different reward types appropriately:
        ///   EquipPage → check booklist (has the key page been acquired?)
        ///   Creature  → check EmotionCardList + RouteUnlockedPages
        ///   Other     → false (passives can be obtained multiple times)
        /// </summary>
        public static bool HasObtainedReward(RewardPassiveInfo info)
        {
            if (info == null) return false;
            if (info.rewardtype == RewardType.EquipPage)
                return HasOwnedBookPage(info.id);
            if (info.rewardtype == RewardType.Creature)
            {
                if (LogueBookModels.EmotionCardList != null && LogueBookModels.EmotionCardList.Any(x => x.id == info.id))
                    return true;
                return RMRAbnormalityUnlockManager.IsPageUnlocked(info.id);
            }
            return false;
        }

        // Unlock-based card inventory: once a card type is acquired, it is always available.
        // num defaults to 1 for caller compatibility, but internally we always use UNLOCKED_CARD_COUNT.
        public const int UNLOCKED_CARD_COUNT = 99;

        public static void AddCard(LorId cardId, int num = 1, bool callInvenChangeEvent = true)
        {
            if (callInvenChangeEvent)
                cardId = Singleton<GlobalLogueEffectManager>.Instance.InvenAddCardChange(cardId);
            if (num <= 0)
                return;
            DiceCardXmlInfo cardItem = ItemXmlDataList.instance.GetCardItem(cardId) ?? ItemXmlDataList.instance.GetCardItem(cardId, true);
            if (cardItem == null || cardItem.optionList.Contains(CardOption.NoInventory) || cardItem.optionList.Contains(CardOption.Basic))
                return;
            LogueBookModels.RecordCompendiumBattleCard(cardId);
            DiceCardItemModel diceCardItemModel = LogueBookModels.cardlist.Find(x => x.GetID() == cardId);
            if (diceCardItemModel != null)
            {
                // Card type already unlocked — ensure count stays at UNLOCKED_CARD_COUNT.
                // Nothing was inserted, so neither the sort nor the upgrade purge can change
                // anything; both used to run anyway on every single AddCard call.
                diceCardItemModel.num = LogueBookModels.UNLOCKED_CARD_COUNT;
                return;
            }

            LogueBookModels.cardlist.Add(new DiceCardItemModel(cardItem)
            {
                num = LogueBookModels.UNLOCKED_CARD_COUNT
            });
            LogueBookModels.cardlist.Sort(new Comparison<DiceCardItemModel>(SortUtil.CardItemCompByCost));
            // Only an upgraded card can orphan a base version, so skip the full-inventory sweep
            // for ordinary cards. This runs once per reward pick, per shop buy, per load entry.
            if (IsUpgradedCardId(cardId))
                LogueBookModels.PurgeBaseCardsWhenUpgradeExists();
        }

        // Permanently remove a card type from the unlocked inventory.
        // Used by effects that consume/destroy cards (e.g. StasisBlaze, StasisSpark, rest events).
        // The num parameter is kept for caller compatibility; in the unlock-based system,
        // any positive num simply removes the card type entirely.
        public static void DeleteCard(LorId cardId, int num = 1)
        {
            DiceCardItemModel diceCardItemModel = LogueBookModels.cardlist.Find((Predicate<DiceCardItemModel>)(x => x.GetID() == cardId));
            if (num <= 0)
            {
                Debug.LogError("num must not be zero");
                return;
            }
            if (diceCardItemModel == null)
            {
                Debug.LogError("There's no Card");
                return;
            }
            DiceCardXmlInfo cardItem = ItemXmlDataList.instance.GetCardItem(cardId);
            // Remove the card from all player decks first
            foreach (UnitDataModel unitDataModel in LogueBookModels.playerModel)
            {
                bool inventory;
                do
                {
                    inventory = unitDataModel.bookItem.MoveCardFromCurrentDeckToInventory(cardId);
                }
                while (inventory);
            }
            LogueBookModels.cardlist.Remove(diceCardItemModel);
        }

        // Check whether a card type is available in the unlocked inventory.
        // In the unlock-based system, this just verifies the card type is unlocked.
        // The num and actual count are no longer relevant — any unlocked card has UNLOCKED_CARD_COUNT copies.
        public static bool RemoveCard(LorId cardId, int num = 1)
        {
            DiceCardItemModel diceCardItemModel = LogueBookModels.cardlist.Find((Predicate<DiceCardItemModel>)(x => x.GetID() == cardId));
            if (num <= 0)
            {
                Debug.LogError("num must not be zero");
                return false;
            }
            DiceCardXmlInfo cardItem = ItemXmlDataList.instance.GetCardItem(cardId);
            if (cardItem != null && cardItem.optionList.Contains(CardOption.Basic))
                return true;
            if (diceCardItemModel == null)
            {
                Debug.LogError("not enough card");
                return false;
            }
            // In unlock-based system, presence in cardlist means the card type is available.
            // We no longer decrement num — the card stays unlocked.
            return true;
        }

        public static void RemoveEquip(UnitDataModel model)
        {
            LogueBookModels.booklist.Find((Predicate<BookModel>)(x => x.owner == model))?.SetOwner((UnitDataModel)null);
        }

        public static void ResetPlayerData(UnitDataModel player)
        {
            if (LogueBookModels.BaseEquipStat == null)
            {
                LogueBookModels.BaseEquipStat = LogueBookModels.CopyEquipEffect(player.bookItem.ClassInfo.EquipEffect);
                LogueBookModels.BaseEquipStat.PassiveList.Clear();
                LogueBookModels.BaseEquipStat.SResist = AtkResist.Vulnerable;
                LogueBookModels.BaseEquipStat.SBResist = AtkResist.Vulnerable;
                LogueBookModels.BaseEquipStat.PResist = AtkResist.Vulnerable;
                LogueBookModels.BaseEquipStat.PBResist = AtkResist.Vulnerable;
                LogueBookModels.BaseEquipStat.HResist = AtkResist.Vulnerable;
                LogueBookModels.BaseEquipStat.HBResist = AtkResist.Vulnerable;
                LogueBookModels.BaseEquipStat.Hp = 40;
                LogueBookModels.BaseEquipStat.Break = 20;
                LogueBookModels.BaseEquipStat.SpeedMin = 1;
                LogueBookModels.BaseEquipStat.Speed = 4;
            }
            if (LogueBookModels.BaseXmlInfo == null)
            {
                LogueBookModels.BaseXmlInfo = LogueBookModels.CopyBookXmlInfo(player.bookItem.ClassInfo);
                LogueBookModels.BaseXmlInfo.EquipEffect = LogueBookModels.BaseEquipStat;
            }
            player.bookItem.ClassInfo._bookIcon = "";
            player.bookItem.ClassInfo.Chapter = 1;
            player.bookItem.ClassInfo.RangeType = EquipRangeType.Melee;
            player.bookItem.ClassInfo.EquipEffect = LogueBookModels.CopyEquipEffect(LogueBookModels.BaseEquipStat);
            player.bookItem.ClassInfo.CharacterSkin = new List<string>()
            {
                "Roland"
            };
            player.bookItem.TryGainUniquePassive();
            player.bookItem.SetOriginalStat();
            player.bookItem.SetOriginalSpeed();
            player.bookItem.SetOriginalResists();
            player.bookItem.SetOriginalPlayPoint();
            player.bookItem.SetOriginalCharacterName();
            typeof(BookModel).GetField("_selectedOriginalSkin", AccessTools.all).SetValue(player.bookItem, player.bookItem.ClassInfo.CharacterSkin[0]);
            typeof(BookModel).GetField("_characterSkin", AccessTools.all).SetValue(player.bookItem, player.bookItem.ClassInfo.CharacterSkin[0]);
        }

        public static int GetMoney() => PassiveAbility_MoneyCheck.GetMoney();

        public static void AddMoney(int amount) => PassiveAbility_MoneyCheck.AddMoney(amount);

        public static void SubMoney(int amount) => PassiveAbility_MoneyCheck.SubMoney(amount);

        public static BookXmlInfo CurPlayerEquipInfo(UnitDataModel data)
        {
            BookModel bookModel = LogueBookModels.booklist.Find((Predicate<BookModel>)(x => x.owner == data));
            BookXmlInfo bookXmlInfo = LogueBookModels.BaseXmlInfo;
            if (bookModel != null)
                bookXmlInfo = bookModel.ClassInfo;
            return bookXmlInfo;
        }

        public static void CreatePlayer()
        {
            LogueBookModels.EmotionSelectDic = new Dictionary<int, int>();
            LogueBookModels.EmotionSelectDic[0] = 0;
            LogueBookModels.EmotionSelectDic[1] = 0;
            LogueBookModels.EmotionSelectDic[2] = 0;
            LogueBookModels.EmotionSelectDic[3] = 0;
            LogLikeMod.AddPlayer = false;
            LogLikeMod.RecoverPlayers = false;
            LogLikeMod.LoadTextData(TextDataModel.CurrentLanguage);
            LogLikeMod.CreateShopEquipPages();
            Singleton<LogCardUpgradeManager>.Instance.ReLoadCurAllUpgradeCard();
            LogueBookModels.nextinstanceid = 0;
            LogLikeMod.ResetUIs();
            LogLikeMod.curChStageStep = 0;
            LogueBookModels.EmotionCardList = new List<RewardPassiveInfo>();
            LogueBookModels.RemainStageList = new Dictionary<ChapterGrade, List<LogueStageInfo>>();
            LogueBookModels.AddingRemainStageList();
            LogueBookModels.cardlist = new List<DiceCardItemModel>();
            LogueBookModels.booklist = new List<BookModel>();
            // Fresh run session: clear in-memory atlas then rehydrate from permanent disk save.
            LogueBookModels.CompendiumUnlockedRoleBooks = new HashSet<LorId>();
            LogueBookModels.CompendiumUnlockedBattleCards = new HashSet<LorId>();
            LogueBookModels.CompendiumUnlockedAbnormalityPages = new HashSet<LorId>();
            LogueBookModels.CompendiumUnlockedEgoPages = new HashSet<LorId>();
            PermanentCompendiumHydrated = false;
            LogueBookModels.LoadPermanentCompendiumData();
            LogueBookModels.Grade6SpecialBuiltInDeckSource = new Dictionary<UnitDataModel, LorId>();
            LogueBookModels.BinahUpgradedDegradedCardIds = new HashSet<int>();
            LogueBookModels.playerModel = new List<UnitDataModel>();
            UnitDataModel player = new UnitDataModel(new LorId(LogLikeMod.ModId, -854));
            player.bookItem.instanceId = LogueBookModels.nextinstanceid++;
            LogueBookModels.ResetPlayerData(player);
            player.bookItem.ClassInfo.EquipEffect.PassiveList.Add(new LorId(LogLikeMod.ModId, 1));
            if (File.Exists(LogueSaveManager.Saveroot + "/dshka"))
            {
                SaveData data = Singleton<LogueSaveManager>.Instance.LoadData("dshka");
                player.SetCustomName(abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("LogueLike_PlayerNugget_Dshka"));
                player.customizeData.LoadFromSaveData(data);
                
            }
            else if (File.Exists(LogueSaveManager.Saveroot + "/Beta14"))
            {
                SaveData data = Singleton<LogueSaveManager>.Instance.LoadData("Beta14");
                player.SetCustomName(abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("LogueLike_PlayerNugget_Berbtha"));
                player.customizeData.LoadFromSaveData(data);
            }
            else
            {
                player.SetCustomName(abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("LogueLike_PlayerName"));
                var unit = LibraryModel.Instance.GetFloor(UI.UIController.Instance.CurrentSephirah).GetUnitDataList()[0].customizeData;
                player.customizeData.Copy(unit);
                player.customizeData.specialCustomID = unit.specialCustomID;
            }
            player.customizeData.SetCustomData(true);
            player.bookItem.TryGainUniquePassive();
            LogueBookModels.playerModel.Add(player);
            LogLikeMod.curstagetype = StageType.Start;
            LogLikeMod.curchaptergrade = ChapterGrade.Grade1;
            LogueBookModels.playersPick = new Dictionary<UnitDataModel, List<LorId>>();
            LogueBookModels.playersPick.Add(LogueBookModels.playerModel[0], new List<LorId>());
            LogueBookModels.playersperpassives = new Dictionary<UnitDataModel, List<LorId>>();
            LogueBookModels.playersperpassives.Add(LogueBookModels.playerModel[0], new List<LorId>());
            LogueBookModels.playersperpassives[LogueBookModels.playerModel[0]].Add(new LorId(LogLikeMod.ModId, 1));
            LogueBookModels.playersstatadders = new Dictionary<UnitDataModel, List<LogStatAdder>>();
            LogueBookModels.playersstatadders.Add(LogueBookModels.playerModel[0], new List<LogStatAdder>());
            LogueBookModels.shopPick = new List<LorId>();
            LogueBookModels.shopUpgradeCardPrice = ShopBase.UpgradeCardBasePrice;
            LogLikeMod.NormalRewardCool = 0;
            if (LogLikeMod.saveloading)
                return;
            PassiveAbility_MoneyCheck.SetMoney(5);
        }

        public static BookXmlInfo CopyBookXmlInfo(BookXmlInfo original)
        {
            return new BookXmlInfo()
            {
                _bookIcon = original._bookIcon,
                TextId = original.TextId,
                Chapter = original.Chapter,
                CharacterSkin = new List<string>((IEnumerable<string>)original.CharacterSkin),
                skinType = original.skinType,
                RangeType = original.RangeType,
                EquipEffect = LogueBookModels.CopyEquipEffect(original.EquipEffect)
            };
        }

        public static BookEquipEffect CopyEquipEffect(BookEquipEffect original)
        {
            return new BookEquipEffect()
            {
                AddedStartDraw = original.AddedStartDraw,
                Break = original.Break,
                CardList = new List<BookSoulCardInfo>((IEnumerable<BookSoulCardInfo>)original.CardList),
                DeadLine = original.DeadLine,
                HBResist = original.HBResist,
                Hp = original.Hp,
                HpReduction = original.HpReduction,
                HResist = original.HResist,
                MaxPlayPoint = original.MaxPlayPoint,
                OnlyCard = original.OnlyCard,
                PassiveCost = original.PassiveCost,
                PassiveList = new List<LorId>((IEnumerable<LorId>)original.PassiveList),
                PBResist = original.PBResist,
                PResist = original.PResist,
                SBResist = original.SBResist,
                Speed = original.Speed,
                SpeedDiceNum = original.SpeedDiceNum,
                SpeedMin = original.SpeedMin,
                SResist = original.SResist,
                StartPlayPoint = original.StartPlayPoint,
                _PassiveList = new List<LorIdXml>((IEnumerable<LorIdXml>)original._PassiveList)
            };
        }

        public static bool CanAddCardToCurrentDeck(LorId cardId, BookModel model)
        {
            if (model == null)
                return false;
            bool editableBlue = IsEditableBlueReverberationDeck(model);
            if (model.IsFixedDeck() && !editableBlue)
                return false;
            if (model.IsLockByBluePrimary() && !editableBlue)
                return false;
            DiceCardXmlInfo cardXmlInfo = RewardingModel.GetCardItemOriginAware(cardId);
            if (cardXmlInfo == null)
                return false;
            if (cardXmlInfo.optionList.Contains(CardOption.OnlyPage))
            {
                if (!model.GetOnlyCards().Exists(x => x.id.GetOriginalId() == cardXmlInfo.id.GetOriginalId()))
                    return false;
            }
            else if (model.ClassInfo.RangeType == EquipRangeType.Melee)
            {
                if (cardXmlInfo.Spec.Ranged == CardRange.Far)
                    return false;
            }
            else if (model.ClassInfo.RangeType == EquipRangeType.Range && cardXmlInfo.Spec.Ranged == CardRange.Near)
                return false;
            DiceCardSelfAbilityBase diceCardSelfAbility = Singleton<AssemblyManager>.Instance.CreateInstance_DiceCardSelfAbility(cardXmlInfo.Script);
            return diceCardSelfAbility == null || !(diceCardSelfAbility is LogDiceCardSelfAbility) || (diceCardSelfAbility as LogDiceCardSelfAbility).CanAddDeck(model.CopyCurrentDeck(), out CardEquipState _);
        }

        private static bool TrySetAndApplyGrade6SpecialBuiltInDeckSource(UnitDataModel unitData, BookXmlInfo page)
        {
            if (unitData == null)
                return false;
            EnsureGrade6SpecialBuiltInDeckSource();
            if (TryResolveGrade6SpecialBuiltInDeckSource(page, out LorId sourceId))
                return TryApplyGrade6SpecialBuiltInDeckToUnit(unitData, sourceId);
            Grade6SpecialBuiltInDeckSource.Remove(unitData);
            return false;
        }

        private static bool TryApplyGrade6SpecialBuiltInDeckToUnit(UnitDataModel unitData, LorId sourceId)
        {
            if (unitData?.bookItem == null || sourceId == LorId.None)
                return false;
            EnsureGrade6SpecialBuiltInDeckSource();
            if (!TryLoadGrade6SpecialBuiltInDeckCards(sourceId, out List<DiceCardXmlInfo> builtInDeck) || builtInDeck.Count == 0)
            {
                Grade6SpecialBuiltInDeckSource.Remove(unitData);
                return false;
            }

            Grade6SpecialBuiltInDeckSource[unitData] = sourceId;
            if (TryGetActualCurrentDeck(unitData.bookItem, out List<DiceCardXmlInfo> currentDeck))
            {
                foreach (DiceCardXmlInfo card in currentDeck.ToList())
                {
                    if (card != null)
                        AddCard(card.id, 1, false);
                }
                currentDeck.Clear();
                currentDeck.AddRange(builtInDeck);
            }

            Debug.Log($"[RMR] Applied fixed built-in deck source {sourceId} to unit book {unitData.bookItem.BookId}: {string.Join(",", builtInDeck.Select(card => card.id.ToString()).ToArray())}");
            return true;
        }

        private static bool TryGetActualCurrentDeck(BookModel book, out List<DiceCardXmlInfo> currentDeck)
        {
            currentDeck = null;
            if (book == null)
                return false;
            try
            {
                DeckModel deckModel = LogLikeMod.GetFieldValue<DeckModel>(book, "_deck");
                currentDeck = LogLikeMod.GetFieldValue<List<DiceCardXmlInfo>>(deckModel, "_deck");
                return currentDeck != null;
            }
            catch
            {
                return false;
            }
        }

        public static void EquipNewPage(UnitDataModel model, BookXmlInfo page, bool keepSuc = false)
        {
            UnitBattleDataModel model1 = LogueBookModels.playerBattleModel.Find((Predicate<UnitBattleDataModel>)(x => x.unitData == model));
            if (model1 == null)
                return;
            LogueBookModels.EquipNewPage(model1, page, keepSuc);
        }

        public static void EquipNewPage(UnitBattleDataModel model, BookXmlInfo page, bool keepSuc = false)
        {
            if (model == null || page == null)
                return;
            page = ResolveFreshEquipPage(page);
            UnitDataModel unitData = model.unitData;
            float num = model.hp / (float)model.MaxHp;
            double hpReductionMod = model.hp;
            foreach (PassiveAbilityBase passive in unitData.bookItem.CreatePassiveList())
            {
                hpReductionMod = (double)passive.GetStartHp((float)(int)model.hp);
            }
            if (hpReductionMod == model.hp) hpReductionMod = 1f;
            else hpReductionMod = model.hp / hpReductionMod;

            num.Log("cur hp percent : " + num.ToString());
            BookXmlInfo classInfo = unitData.bookItem.ClassInfo;
            classInfo._bookIcon = page._bookIcon;
            classInfo.Chapter = page.Chapter;
            classInfo.CharacterSkin = new List<string>(page.CharacterSkin);
            classInfo.skinType = page.skinType;
            classInfo.EquipEffect = LogueBookModels.CopyEquipEffect(page.EquipEffect);
            classInfo.EquipEffect.PassiveList = new List<LorId>(classInfo.EquipEffect.PassiveList);
            for (int index = 0; index < LogueBookModels.playersperpassives[unitData].Count; ++index)
                classInfo.EquipEffect.PassiveList.Insert(index, LogueBookModels.playersperpassives[unitData][index]);
            if (!keepSuc)
                unitData.bookItem.ReleaseAllEquipedPassiveBooks();
            model.unitData.bookItem.ClassInfo.RangeType = page.RangeType;
            model.unitData.bookItem.TryGainUniquePassive();
            model.unitData.bookItem.SetOriginalStat();
            model.unitData.bookItem.SetOriginalSpeed();
            model.unitData.bookItem.SetOriginalResists();
            model.unitData.bookItem.SetOriginalPlayPoint();
            model.unitData.bookItem.SetOriginalSpeedNum();
            if (LogueBookModels.playersstatadders[model.unitData].Count > 0)
                LogueBookModels.ApplyPlayerStat(model, LogueBookModels.playersstatadders[model.unitData]);
            model.unitData.bookItem.SetOriginalCharacterName();
            typeof(BookModel).GetField("_selectedOriginalSkin", AccessTools.all).SetValue(model.unitData.bookItem, classInfo.CharacterSkin[0]);
            typeof(BookModel).GetField("_characterSkin", AccessTools.all).SetValue(model.unitData.bookItem, classInfo.CharacterSkin[0]);
            model.hp = (float)Mathf.RoundToInt(num * (float)model.MaxHp);
            double startHp = model.hp;
            foreach (PassiveAbilityBase passive in model.unitData.bookItem.CreatePassiveList())
            {
                startHp = (double)passive.GetStartHp((float)(int)model.hp);
            }
            model.hp = (float)(startHp * hpReductionMod);
            if ((double)model.hp <= 0.0 && !model.isDead)
                model.hp = 1f;
            List<DiceCardXmlInfo> fieldValue = LogLikeMod.GetFieldValue<List<DiceCardXmlInfo>>(unitData.bookItem, "_onlyCards");
            fieldValue.Clear();
            foreach (int id in page.EquipEffect.OnlyCard)
            {
                DiceCardXmlInfo cardItem = ItemXmlDataList.instance.GetCardItem(new LorId(page.workshopID, id), true)
                    ?? ItemXmlDataList.instance.GetCardItem(id, true);
                if (cardItem == null)
                    "".Log("Error : No Limit Card Data");
                else
                    fieldValue.Add(cardItem);
            }
            bool appliedBuiltInDeck = TrySetAndApplyGrade6SpecialBuiltInDeckSource(unitData, page);
            PruneCorePageExclusiveBattleCardsFromInventoryAndAtlas();
            if (appliedBuiltInDeck)
                return;
            foreach (DiceCardXmlInfo diceCardXmlInfo in model.unitData.bookItem.GetCardListFromCurrentDeck())
            {
                if (!LogueBookModels.CanAddCardToCurrentDeck(diceCardXmlInfo.id, model.unitData.bookItem))
                {
                    diceCardXmlInfo.Log("ATTEMPTED TO UNEQUIP CARD");
                    model.unitData.bookItem.MoveCardFromCurrentDeckToInventory(diceCardXmlInfo.id);
                }
            }
        }

        public static void ApplyPlayerStat(UnitBattleDataModel model, List<LogStatAdder> adder)
        {
            int hp = LogueBookModels.StatAdderManager.GetHp(adder);
            int hpPercent = LogueBookModels.StatAdderManager.GetHpPercent(adder);
            int maxHp = (model.unitData.bookItem.HP + hp) * hpPercent / 100;
            if (maxHp < 1)
                maxHp = 1;

            int bp = LogueBookModels.StatAdderManager.GetBp(adder);
            int bpPercent = LogueBookModels.StatAdderManager.GetBpPercent(adder);
            int maxBp = (model.unitData.bookItem.Break + bp) * bpPercent / 100;
            if (maxBp < 1)
                maxBp = 1;

            model.unitData.bookItem.ClassInfo.EquipEffect.Hp = maxHp;
            model.unitData.bookItem.SetHp(maxHp);
            model.unitData.bookItem.ClassInfo.EquipEffect.Break = maxBp;
            model.unitData.bookItem.SetBp(maxBp);

            int speedDiceNum = model.unitData.bookItem.SpeedDiceNum + StatAdderManager.GetSpeedDiceNum(adder);
            model.unitData.bookItem.ClassInfo.EquipEffect.SpeedDiceNum = speedDiceNum;
            model.unitData.bookItem.SetSpeedDiceNum(speedDiceNum);

            int speedMax = model.unitData.bookItem.SpeedMax + StatAdderManager.GetSpeedMax(adder);
            model.unitData.bookItem.ClassInfo.EquipEffect.Speed = speedMax;
            model.unitData.bookItem.SetSpeedDiceMax(speedMax);

            int speedMin = model.unitData.bookItem.SpeedMin + StatAdderManager.GetSpeedMin(adder);
            model.unitData.bookItem.ClassInfo.EquipEffect.SpeedMin = speedMin;
            model.unitData.bookItem.SetSpeedDiceMin(speedMin);

            int startPlayPoint = model.unitData.bookItem.GetStartPlayPoint() + StatAdderManager.GetStartPlayPoint(adder);
            model.unitData.bookItem.ClassInfo.EquipEffect.StartPlayPoint = startPlayPoint;
            model.unitData.bookItem.SetStartPlayPoint(startPlayPoint);

            int maxPlayPoint = model.unitData.bookItem.ClassInfo.EquipEffect.MaxPlayPoint + StatAdderManager.GetMaxPlayPoint(adder);
            model.unitData.bookItem.ClassInfo.EquipEffect.MaxPlayPoint = maxPlayPoint;
            model.unitData.bookItem.SetMaxPlayPoint(maxPlayPoint);


            foreach (LogStatAdder logStatAdder in adder)
                model.unitData.bookItem.ClassInfo.RangeType = logStatAdder.GetRangeType(model.unitData.bookItem.ClassInfo.RangeType);
            foreach (LogStatAdder logStatAdder in adder)
            {
                AtkResist sresist = model.unitData.bookItem.equipeffect.SResist;
                model.unitData.bookItem.equipeffect.SResist = logStatAdder.GetResist(AtkResistType.SResist, sresist);
                AtkResist presist = model.unitData.bookItem.equipeffect.PResist;
                model.unitData.bookItem.equipeffect.PResist = logStatAdder.GetResist(AtkResistType.PResist, presist);
                AtkResist hresist = model.unitData.bookItem.equipeffect.HResist;
                model.unitData.bookItem.equipeffect.HResist = logStatAdder.GetResist(AtkResistType.HResist, hresist);
                AtkResist sbResist = model.unitData.bookItem.equipeffect.SBResist;
                model.unitData.bookItem.equipeffect.SBResist = logStatAdder.GetResist(AtkResistType.SBResist, sbResist);
                AtkResist pbResist = model.unitData.bookItem.equipeffect.PBResist;
                model.unitData.bookItem.equipeffect.PBResist = logStatAdder.GetResist(AtkResistType.PBResist, pbResist);
                AtkResist hbResist = model.unitData.bookItem.equipeffect.HBResist;
                model.unitData.bookItem.equipeffect.HBResist = logStatAdder.GetResist(AtkResistType.HBResist, hbResist);
            }
        }

        public static void AddPlayerStat(UnitDataModel model, LogStatAdder adder)
        {
            LogueBookModels.playersstatadders[model].Add(adder);
            BookModel bookModel = LogueBookModels.booklist.Find(x => x.owner == model);
            if (bookModel != null)
            {
                LogueBookModels.EquipNewPage(model, bookModel.ClassInfo, true);
            }
            else
            {
                LogueBookModels.EquipNewPage(model, LogueBookModels.BaseXmlInfo, true);
                if (LogueBookModels.playerModel[0] != model)
                {
                    model.bookItem.ClassInfo.CharacterSkin = new List<string>();
                    model.bookItem.ClassInfo.CharacterSkin.Add("KetherLibrarian");
                    typeof(BookModel).GetField("_selectedOriginalSkin", AccessTools.all).SetValue(model.bookItem, model.bookItem.ClassInfo.CharacterSkin[0]);
                    typeof(BookModel).GetField("_characterSkin", AccessTools.all).SetValue(model.bookItem, model.bookItem.ClassInfo.CharacterSkin[0]);
                }
            }
        }

        public static void AddPlayerStat(UnitBattleDataModel model, LogStatAdder adder)
        {
            LogueBookModels.AddPlayerStat(model.unitData, adder);
        }

        public static void AddSubPlayer()
        {
            if (LogueBookModels.playerModel.Count >= 5)
                return;
            UnitDataModel unitDataModel = new UnitDataModel(new LorId(LogLikeMod.ModId, -854 - LogueBookModels.playerModel.Count));
            unitDataModel.bookItem.instanceId = LogueBookModels.nextinstanceid++;
            unitDataModel.bookItem.ClassInfo.EquipEffect.PassiveList.Clear();
            try
            {
                var date = System.DateTime.Now;
                if (date.DayOfYear == 91 && date.Year % 4 > 0 || date.DayOfYear == 92 && date.Year % 4 == 0)
                {
                    SaveData beta14 = SaveManager.Instance.LoadData(LogLikeMod.path + "/DevNuggets/Beta14");
                    unitDataModel.customizeData.LoadFromSaveData(beta14);
                    unitDataModel.SetCustomName("Beta14");
                }
                else
                {
                    var floormodel = StageController.Instance.GetStageModel().GetFrontAvailableFloor();
                    UnitDataModel nugget = floormodel == null ? LibraryModel.Instance.GetFloor(SephirahType.Keter).GetUnitDataList()[LogueBookModels.playerModel.Count] : floormodel._floorModel.GetUnitDataList()[LogueBookModels.playerModel.Count];
                    unitDataModel.SetCustomName(nugget.name);
                    unitDataModel.customizeData.Copy(nugget.customizeData);
                }
            } catch (ArgumentOutOfRangeException e)
            {
                "".Log($"Player nugget {LogueBookModels.playerModel.Count} at currently selected floor does not exist: {e}");
                SaveData beta14 = SaveManager.Instance.LoadData(LogLikeMod.path + "/DevNuggets/Beta14");
                unitDataModel.customizeData.LoadFromSaveData(beta14);
                unitDataModel.SetCustomName(abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("LogueLike_PlayerName") + " " + LogueBookModels.playerModel.Count.ToString());
            } catch (Exception e)
            {
                Debug.LogWarning("OKAY REAL SHIT IS GOING ON IN SUBPLAYER ACTUALLY: " + e);
            }
            unitDataModel.customizeData.SetCustomData(true);
            LogueBookModels.ResetPlayerData(unitDataModel);
            unitDataModel.bookItem.ClassInfo.CharacterSkin = new List<string>()
            {
                "KetherLibrarian"
            };
            // BookModel skin fields are private at runtime (publicized only in the compile reference).
            // Direct assignment throws MonoMod FieldAccessException and freezes EndBattlePhase → black screen.
            string skinName = unitDataModel.bookItem.ClassInfo.CharacterSkin[0];
            try
            {
                typeof(BookModel).GetField("_selectedOriginalSkin", AccessTools.all)?.SetValue(unitDataModel.bookItem, skinName);
                typeof(BookModel).GetField("_characterSkin", AccessTools.all)?.SetValue(unitDataModel.bookItem, skinName);
            }
            catch (Exception skinEx)
            {
                Debug.LogWarning("[RMR] AddSubPlayer skin set failed (non-fatal): " + skinEx.Message);
            }
            LogueBookModels.playerModel.Add(unitDataModel);
            UnitBattleDataModel unitBattleDataModel = new UnitBattleDataModel(Singleton<StageController>.Instance.GetStageModel(), unitDataModel);
            LogueBookModels.playerBattleModel.Add(unitBattleDataModel);
            LogueBookModels.playersPick.Add(unitDataModel, new List<LorId>());
            LogueBookModels.playersperpassives.Add(unitDataModel, new List<LorId>());
            LogueBookModels.playersstatadders.Add(unitDataModel, new List<LogStatAdder>());
            Singleton<GlobalLogueEffectManager>.Instance.OnAddSubPlayer(unitDataModel);
        }

        /// <summary>
        /// Picks a number of encounters based on their types and the given <see cref="StageLimits"/>.
        /// </summary>
        /// <param name="stage">An empty list to be fed the new list of unique encounters.</param>
        /// <param name="allstage">A list containing all the encounters to pick from.</param>
        /// <param name="stageLimits">An object that limit how many of each type of encounter should appear in a chapter.</param>
        /// <param name="guaranteeChapterEvent">As to whether or not guarantee at least one event from that chapter.</param>
        public static void HandleLimitPícking(
          List<LogueStageInfo> stage,
          List<LogueStageInfo> allstage,
          StageLimits stageLimits, bool guaranteeChapterEvent = true)
        {
            int numBoss = stageLimits.Boss;
            int numNormal = stageLimits.Normal;
            int numShop = stageLimits.Shop;
            int numMystery = stageLimits.Mystery;
            int numElite = stageLimits.Elite;
            int numRest = stageLimits.Rest;
            int numCreature = stageLimits.Creature;
            if (guaranteeChapterEvent)
            {
                List<LogueStageInfo> mstage = StagesXmlList.Instance.GetChapterData(stageLimits.Chapter, true);
                ModdingUtils.SuffleList(mstage);
                stage.Add(mstage[0]);
                allstage.Remove(mstage[0]);
            }
            ModdingUtils.SuffleList(allstage);
            foreach (LogueStageInfo logueStageInfo in allstage)
            {
                if (logueStageInfo.type == StageType.Boss && numBoss > 0)
                {
                    stage.Add(logueStageInfo);
                    --numBoss;
                }
                else if (logueStageInfo.type == StageType.Normal && numNormal > 0)
                {
                    stage.Add(logueStageInfo);
                    --numNormal;
                }
                else if (logueStageInfo.type == StageType.Mystery && numMystery > 0)
                {
                    stage.Add(logueStageInfo);
                    --numMystery;
                }
                else if (logueStageInfo.type == StageType.Shop && numShop > 0)
                {
                    stage.Add(logueStageInfo);
                    --numShop;
                }
                else if (logueStageInfo.type == StageType.Elite && numElite > 0)
                {
                    stage.Add(logueStageInfo);
                    --numElite;
                }
                else if (logueStageInfo.type == StageType.Creature && numCreature > 0)
                {
                    stage.Add(logueStageInfo);
                    --numCreature;
                }
            }
            LogueStageInfo stageInfo = Singleton<StagesXmlList>.Instance.GetStageInfo(new LorId(LogLikeMod.ModId, 855));
            for (int index = 0; index < numRest; ++index)
                stage.Add(stageInfo);
        }

        public static List<LogueStageInfo> VanillaGamemodeReceptionList(ChapterGrade chapter)
        {
            List<LogueStageInfo> choiceReceptions = new List<LogueStageInfo>();
            List<LogueStageInfo> allReceptions = Singleton<StagesXmlList>.Instance.GetChapterDataVanilla(chapter);
            StageLimits stageLimits = new StageLimits
            {
                Normal = 0,
                Elite = 0,
                Mystery = 0,
                Shop = 0,
                Boss = 0,
                Rest = 0,
                Creature = 0,
                Chapter = ChapterGrade.DummyGrade
            };
            // Per floor: one fewer Normal reception, one Rest room (ID 855 via HandleLimitPicking).
            switch (chapter)
            {
                case ChapterGrade.Grade1:
                    stageLimits = new StageLimits
                    {
                        Normal = 2,
                        Elite = 0,
                        Mystery = 1,
                        Shop = 1,
                        Boss = 1,
                        Rest = 1,
                        Creature = 0,  // No abnormality battles in floor 1-2
                        Chapter = chapter
                    };
                    break;
                case ChapterGrade.Grade2:
                    stageLimits = new StageLimits
                    {
                        Normal = 4,
                        Elite = 0,
                        Mystery = 1,
                        Shop = 1,
                        Boss = 1,
                        Rest = 1,
                        Creature = 0,  // No abnormality battles in floor 1-2
                        Chapter = chapter
                    };
                    break;
                case ChapterGrade.Grade3:
                    stageLimits = new StageLimits
                    {
                        Normal = 4,
                        Elite = 0,
                        Mystery = 2,
                        Shop = 1,
                        Boss = 1,
                        Rest = 1,
                        Creature = 1,
                        Chapter = chapter
                    };
                    break;
                case ChapterGrade.Grade4:
                    stageLimits = new StageLimits
                    {
                        Normal = 4,
                        Elite = 0,
                        Mystery = 2,
                        Shop = 1,
                        Boss = 1,
                        Rest = 2, // +1 rest (都市恶疾)
                        Creature = 1,
                        Chapter = chapter
                    };
                    break;
                case ChapterGrade.Grade5:
                    stageLimits = new StageLimits
                    {
                        Normal = 5,
                        Elite = 0,
                        Mystery = 2,
                        Shop = 2,
                        Boss = 1,
                        Rest = 2, // +1 rest (都市梦魇)
                        Creature = 1,
                        Chapter = chapter
                    };
                    break;
                case ChapterGrade.Grade6:
                    stageLimits = new StageLimits
                    {
                        Normal = 4,
                        Elite = 1,
                        Mystery = 2,
                        Shop = 2,
                        Boss = 1,
                        Rest = 2, // +1 rest (都市之星)
                        Creature = 1,
                        Chapter = chapter
                    };
                    break;
                case ChapterGrade.Grade7:
                    stageLimits = new StageLimits
                    {
                        Normal = 5,
                        Elite = 0,
                        Mystery = 2,
                        Shop = 2,
                        Boss = 3,
                        Rest = 2, // +1 rest (杂质)
                        Creature = 1,
                        Chapter = chapter
                    };
                    break;
            }
            LogueBookModels.ApplyGrade6RedMistAppearanceRoll(allReceptions, ref stageLimits, chapter);
            LogueBookModels.HandleLimitPícking(choiceReceptions, allReceptions, stageLimits);
            if (chapter == ChapterGrade.Grade4)
                choiceReceptions.Add(Singleton<StagesXmlList>.Instance.GetStageInfo(new LorId(LogLikeMod.ModId, 80000)));
            // WORKSHOP CONTRACT EVENT
            return choiceReceptions;
        }

        private const int RedMistChallengeStageId = 60020;
        private const float RedMistChallengeAppearanceChance = 0.3f;

        private static void ApplyGrade6RedMistAppearanceRoll(List<LogueStageInfo> allReceptions, ref StageLimits stageLimits, ChapterGrade chapter)
        {
            if (chapter != ChapterGrade.Grade6 || allReceptions == null)
                return;
            LogueStageInfo redMist = allReceptions.Find(stage => stage != null
                && stage.Id == new LorId(LogLikeMod.ModId, RedMistChallengeStageId));
            if (redMist == null)
                return;
            if (UnityEngine.Random.value <= RedMistChallengeAppearanceChance)
                return;
            allReceptions.Remove(redMist);
            stageLimits.Elite = 0;
            stageLimits.Normal += 1;
            Debug.Log($"[RMR] Grade6 Red Mist challenge skipped by route roll; chance={RedMistChallengeAppearanceChance:0.##}. Replaced its elite slot with a normal node.");
        }

        public static void AddingRemainStageList()
        {
            for (int i = 0; i < 7; i++)
            {
                try
                {
                    var list = RMRCore.CurrentGamemode.InitializeChapterStageList((ChapterGrade)i);
                    var list2 = new List<LogueStageInfo>();
                    list2.AddRange(list);
                    "".Log($"Chapter {(i + 1).ToString()} StageList");
                    foreach (LogueStageInfo logueStageInfo in list2)
                    {
                        if (StageClassInfoList.Instance.GetData(logueStageInfo.Id) is var stage && (stage == null || stage.waveList == null || stage.waveList.Count == 0))
                        {
                            list.Remove(logueStageInfo);
                            "".Log($"!WARNING! -- INVALID STAGE REMOVED FROM STAGE LIST ON INITIALIZE");
                            "".Log($"{logueStageInfo.Id.packageId} _ {logueStageInfo.Id.id.ToString()}");
                        }
                        else
                            "".Log($"{logueStageInfo.Id.packageId} _ {logueStageInfo.Id.id.ToString()}");
                    }
                    if (list != null && list.Count > 0)
                    {
                        LogueBookModels.RemainStageList[(ChapterGrade)i] = new List<LogueStageInfo>();
                        LogueBookModels.RemainStageList[(ChapterGrade)i].AddRange(list);
                    }
                } catch (Exception e)
                {
                    Debug.Log("Error when initializing chapters: " + e);
                }
            }
        }

        /// <summary>
        /// Ensure RemainStageList[grade] exists and has at least one valid stage.
        /// If empty/missing, re-roll from the gamemode chapter table (e.g. Grade7 杂质 after 都市之星).
        /// Returns true when the chapter has selectable stages.
        /// </summary>
        public static bool EnsureChapterRemainStages(ChapterGrade grade)
        {
            try
            {
                if (LogueBookModels.RemainStageList == null)
                    LogueBookModels.RemainStageList = new Dictionary<ChapterGrade, List<LogueStageInfo>>();

                if (LogueBookModels.RemainStageList.TryGetValue(grade, out List<LogueStageInfo> existing)
                    && existing != null
                    && existing.Count > 0)
                {
                    existing.RemoveAll(stage =>
                    {
                        if (stage == null) return true;
                        StageClassInfo data = StageClassInfoList.Instance.GetData(stage.Id);
                        return data == null || data.waveList == null || data.waveList.Count == 0;
                    });
                    if (existing.Count > 0)
                        return true;
                }

                if (RMRCore.CurrentGamemode == null)
                {
                    Debug.LogWarning($"[RMR EnsureChapterRemainStages] no gamemode for {grade}");
                    return false;
                }

                List<LogueStageInfo> fresh = RMRCore.CurrentGamemode.InitializeChapterStageList(grade);
                List<LogueStageInfo> valid = new List<LogueStageInfo>();
                if (fresh != null)
                {
                    foreach (LogueStageInfo info in fresh)
                    {
                        if (info == null) continue;
                        StageClassInfo data = StageClassInfoList.Instance.GetData(info.Id);
                        if (data == null || data.waveList == null || data.waveList.Count == 0)
                        {
                            Debug.LogWarning($"[RMR EnsureChapterRemainStages] skip invalid {info.Id}");
                            continue;
                        }
                        valid.Add(info);
                    }
                }
                LogueBookModels.RemainStageList[grade] = valid;
                Debug.Log($"[RMR EnsureChapterRemainStages] grade={grade} stages={valid.Count}");
                return valid.Count > 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RMR EnsureChapterRemainStages] {grade}: {ex}");
                return false;
            }
        }

        public static List<EmotionCardXmlInfo> GetNextList(ChapterGrade grade, bool Stepone = false)
        {
            if (LogueBookModels.RemainStageList == null
                || !LogueBookModels.RemainStageList.TryGetValue(grade, out List<LogueStageInfo> remain)
                || remain == null
                || remain.Count == 0)
            {
                if (!LogueBookModels.EnsureChapterRemainStages(grade)
                    || !LogueBookModels.RemainStageList.TryGetValue(grade, out remain)
                    || remain == null
                    || remain.Count == 0)
                {
                    Debug.LogWarning($"[RMR GetNextList] no stages for {grade}");
                    return new List<EmotionCardXmlInfo>();
                }
            }

            List<LogueStageInfo> collection = new List<LogueStageInfo>(remain);
            if (collection.Count == 0)
                return new List<EmotionCardXmlInfo>();
            List<EmotionCardXmlInfo> nextList = new List<EmotionCardXmlInfo>();
            List<LogueStageInfo> logueStageInfoList = new List<LogueStageInfo>(collection);
            if (logueStageInfoList.FindAll(x => x.type == StageType.Normal).Count > 1 || logueStageInfoList.Count > 5)
            {
                LogueStageInfo boss = logueStageInfoList.Find(x => x.type == StageType.Boss);
                if (boss != null)
                    logueStageInfoList.Remove(boss);
            }
            // Safety: if filtering left nothing, fall back to full collection.
            if (logueStageInfoList.Count == 0)
                logueStageInfoList = new List<LogueStageInfo>(collection);

            int guard = 0;
            for (int index = 0; index < 3 && logueStageInfoList.Count > 0 && guard < 32; guard++)
            {
                LogueStageInfo info = logueStageInfoList[UnityEngine.Random.Range(0, logueStageInfoList.Count)];
                logueStageInfoList.Remove(info);
                if (info == null)
                    continue;
                EmotionCardXmlInfo thing = LogLikeMod.GetRegisteredPickUpXml(info);
                if (thing == null)
                {
                    // Lazy-register stage pickups so next-chapter options can still appear.
                    try { LogLikeMod.RegisterPickUpXml(info); }
                    catch { /* ignore */ }
                    thing = LogLikeMod.GetRegisteredPickUpXml(info);
                }
                if (thing != null)
                {
                    nextList.Add(thing);
                    try { LogueBookModels.CreateStageDesc(info); } catch { /* ignore */ }
                    index++;
                }
                else
                {
                    Debug.LogWarning($"[RMR GetNextList] NULL STAGE pickup: {info.Id?.packageId} --- {info.Id?.id}");
                }
            }
            return nextList;
        }

        public static List<EmotionCardXmlInfo> GetPassiveRewards(List<RewardPassiveInfo> list)
        {
            List<RewardPassiveInfo> rewardPassiveInfoList = new List<RewardPassiveInfo>();
            List<RewardPassiveInfo> rewards = new List<RewardPassiveInfo>();
            if (list != null) rewards.AddRange(list);

            // Filter out already-obtained rewards
            int totalBefore = rewards.Count;
            int removed = rewards.RemoveAll(x => HasObtainedReward(x));
            if (removed > 0)
                Debug.Log($"[GetPassiveRewards] Filtered {removed}/{totalBefore} obtained, {rewards.Count} remain");

            List<EmotionCardXmlInfo> passiveRewards = new List<EmotionCardXmlInfo>();
            do
            {
                RewardPassiveInfo reward = RewardingModel.GetReward(rewards);
                if (reward != null)
                {
                    if (LogueBookModels.CheckIsCanAdd(reward))
                        rewardPassiveInfoList.Add(reward);
                    rewards.Remove(reward);
                }
            }
            while (rewards.Count != 0 && rewardPassiveInfoList.Count != 3);
            Debug.Log($"[GetPassiveRewards] Final: {rewardPassiveInfoList.Count} passives selected");
            for (int index = 0; index < rewardPassiveInfoList.Count; ++index)
                passiveRewards.Add(LogLikeMod.GetRegisteredPickUpXml(rewardPassiveInfoList[index]));
            return passiveRewards;
        }

        public static List<EmotionCardXmlInfo> GetPassiveRewards_Inlist(List<RewardPassiveInfo> list)
        {
            return LogueBookModels.GetPassiveRewards(list);
        }

        public static void RemoveStageInlist(LorId id)
        {
            foreach (KeyValuePair<ChapterGrade, List<LogueStageInfo>> remainStage in LogueBookModels.RemainStageList)
                remainStage.Value.RemoveAll((Predicate<LogueStageInfo>)(x => x.Id == id));
        }

        public static void RemoveStageInlist(LorId id, ChapterGrade chapter)
        {
            LogueStageInfo logueStageInfo = LogueBookModels.RemainStageList[chapter].Find((Predicate<LogueStageInfo>)(x => x.Id == id));
            if (logueStageInfo == null)
                return;
            LogueBookModels.RemainStageList[chapter].Remove(logueStageInfo);
        }

        public static void CreatePlayerBattle()
        {
            LogueBookModels.playerBattleModel = new List<UnitBattleDataModel>();
            foreach (UnitDataModel data in LogueBookModels.playerModel)
            {
                UnitBattleDataModel unitBattleDataModel = new UnitBattleDataModel(Singleton<StageController>.Instance.GetStageModel(), data);
                LogueBookModels.playerBattleModel.Add(unitBattleDataModel);
            }
        }

        public static void AddUpgradeCard(LorId cardid, int index = 0, int count = 1, int num = 1, bool callInvenChangeEvent = true)
        {
            LogueBookModels.AddCard(Singleton<LogCardUpgradeManager>.Instance.GetUpgradeCard(cardid, index, count).id, num, callInvenChangeEvent);
        }

        public static int GetIndexOfUnit(BattleUnitModel model)
        {
            return playerModel.IndexOf(model.UnitData.unitData);
        }

        public static int GetIndexOfUnit(UnitBattleDataModel model)
        {
            return playerBattleModel.IndexOf(model);
        }

        /// <summary>StatAdderManager</summary>

        public class StatAdderManager
        {
            public static int GetHp(List<LogStatAdder> adder)
            {
                int hp = 0;
                foreach (LogStatAdder logStatAdder in adder)
                    hp += logStatAdder.maxhp;
                return hp;
            }

            public static int GetHpPercent(List<LogStatAdder> adder)
            {
                int hpPercent = 100;
                foreach (LogStatAdder logStatAdder in adder)
                    hpPercent += logStatAdder.maxhppercent;
                return hpPercent;
            }

            public static int GetBp(List<LogStatAdder> adder)
            {
                int bp = 0;
                foreach (LogStatAdder logStatAdder in adder)
                    bp += logStatAdder.maxbreak;
                return bp;
            }

            public static int GetBpPercent(List<LogStatAdder> adder)
            {
                int bpPercent = 100;
                foreach (LogStatAdder logStatAdder in adder)
                    bpPercent += logStatAdder.maxbreakpercent;
                return bpPercent;
            }

            public static int GetStartPlayPoint(List<LogStatAdder> adder)
            {
                int startplaypoint = 0;
                foreach (LogStatAdder logStatAdder in adder)
                    startplaypoint += logStatAdder.startplaypoint;
                return startplaypoint;
            }

            public static int GetMaxPlayPoint(List<LogStatAdder> adder)
            {
                int maxplaypoint = 0;
                foreach (LogStatAdder logStatAdder in adder)
                    maxplaypoint += logStatAdder.maxplaypoint;
                return maxplaypoint;
            }

            public static int GetSpeedDiceNum(List<LogStatAdder> adder)
            {
                int speeddicenum = 0;
                foreach (LogStatAdder logStatAdder in adder)
                    speeddicenum += logStatAdder.speeddicenum;
                return speeddicenum;
            }

            public static int GetSpeedMax(List<LogStatAdder> adder)
            {
                int speedmax = 0;
                foreach (LogStatAdder logStatAdder in adder)
                    speedmax += logStatAdder.speedmax;
                return speedmax;
            }

            public static int GetSpeedMin(List<LogStatAdder> adder)
            {
                int speedmin = 0;
                foreach (LogStatAdder logStatAdder in adder)
                    speedmin += logStatAdder.speedmin;
                return speedmin;
            }

        }

        /// <summary>enum StageFlag</summary>

        public enum StageFlag
        {
            None,
            OnlyNormal,
        }

        #endregion

        #region --- Stage limit struct ---

        /// <summary>Per-chapter caps for node types on the roguelike map.</summary>
        public struct StageLimits
        {
            public int Normal;
            public int Elite;
            public int Mystery;
            public int Shop;
            public int Boss;
            public int Rest;
            public int Creature;
            public ChapterGrade Chapter;
        }

        #endregion
    }
}
