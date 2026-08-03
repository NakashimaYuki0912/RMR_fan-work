// -----------------------------------------------------------------------------
// RogueLike Mod Reborn (RMR): RMR_PrepareRestrictions
// Namespace/file: ruina-roguelike-reborn-main\RMR_PrepareRestrictions.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using abcdcode_LOGLIKE_MOD;
using HarmonyLib;
using LOR_DiceSystem;
using UI;
using UnityEngine;

namespace RogueLike_Mod_Reborn
{
    /// <summary>
    /// Deck / key-page filters for RMR prepare and Floor Realization (解放战).
    /// Vanilla EGO pages live in personalEgo / emotion EGO UI — never the combat deck inventory.
    /// </summary>
    public static class RMRPrepareRestrictions
    {
        /// <summary>
        /// Urban Nightmare = Chapter 5, Star of the City = 6, Impurity = 7 (same as BookXmlInfo.Chapter).
        /// </summary>
        public const int ChapterUrbanNightmare = 5;
        public const int ChapterUrbanStar = 6;
        public const int ChapterImpurity = 7;
        #region --- Other helpers ---


        public static bool IsEgoCombatPage(DiceCardXmlInfo card)
        {
            if (card == null)
                return false;
            try
            {
                if (card.IsEgo())
                    return true;
            }
            catch { /* IsEgo missing on some stubs */ }
            if (card.optionList == null)
                return false;
            return card.optionList.Contains(CardOption.EGO)
                || card.optionList.Contains(CardOption.EgoPersonal)
                || card.optionList.Contains(CardOption.EgoChange);
        }
        #endregion

        #region --- Restriction rules ---


        public static bool IsAllowedInCombatDeckInventory(DiceCardXmlInfo card)
        {
            if (card == null)
                return false;
            if (IsEgoCombatPage(card))
                return false;
            if (card.optionList != null && card.optionList.Contains(CardOption.NoInventory))
                return false;
            return true;
        }
        #endregion

        #region --- Other helpers ---


        /// <summary>
        /// 解放战出战层书页章节上限：
        /// 历史/科技/文学/艺术 → 都市之星(6)；其余解放战 → 杂质(7)。
        /// </summary>
        public static int GetRealizationMaxChapter(SephirahType floor)
        {
            switch (floor)
            {
                case SephirahType.Malkuth: // 历史
                case SephirahType.Yesod:   // 科技
                case SephirahType.Hod:     // 文学
                case SephirahType.Netzach: // 艺术
                    return ChapterUrbanStar;
                case SephirahType.Keter:   // 总类
                    return ChapterImpurity;
                default:
                    return ChapterImpurity;
            }
        }

        public static SephirahType GetPrepareFloor()
        {
            try
            {
                if (Singleton<StageController>.Instance != null)
                {
                    SephirahType f = Singleton<StageController>.Instance.CurrentFloor;
                    if (f != SephirahType.None && (int)f != 11)
                        return f;
                }
            }
            catch { /* ignore */ }
            try
            {
                if (UI.UIController.Instance != null)
                {
                    SephirahType f = UI.UIController.Instance.CurrentSephirah;
                    if (f != SephirahType.None && (int)f != 11)
                        return f;
                }
            }
            catch { /* ignore */ }
            return SephirahType.Malkuth;
        }

        public static bool IsRealizationPrepareContext()
        {
            return RMRRealizationManager.IsRealizationPreparationActive
                || RMRRealizationManager.InRealizationBattle
                || RMRRealizationManager.RealizationReceptionActive;
        }

        public static int GetContentChapter(BookXmlInfo book)
        {
            if (book == null)
                return 99;
            if (LogueBookModels.IsUrbanStarSpecialCorePage(book))
                return ChapterUrbanStar;
            int ch = book.Chapter;
            if (ch <= 0)
                ch = 1;
            return ch;
        }

        public static int GetContentChapter(DiceCardXmlInfo card)
        {
            if (card == null)
                return 99;
            // Argalia's RMR reward pages are granted together with his core page at
            // Urban Star progression even though the vanilla XML labels them Chapter 7.
            if (RMRCore.IsBlueReverberationBattlePage(card.id))
                return ChapterUrbanStar;
            int ch = card.Chapter;
            if (ch <= 0)
                ch = 1;
            return ch;
        }
        #endregion

        #region --- Restriction rules ---


        public static bool IsBookAllowedInCurrentPrepare(BookXmlInfo book)
        {
            if (book == null || !RewardingModel.IsValidBookData(book))
                return false;
            if (!IsRealizationPrepareContext())
                return true;
            int max = GetRealizationMaxChapter(GetPrepareFloor());
            return GetContentChapter(book) <= max;
        }

        public static bool IsCardAllowedInCurrentPrepare(DiceCardXmlInfo card)
        {
            if (!IsAllowedInCombatDeckInventory(card))
                return false;
            if (!IsRealizationPrepareContext())
                return true;
            int max = GetRealizationMaxChapter(GetPrepareFloor());
            return GetContentChapter(card) <= max;
        }

        public static List<DiceCardItemModel> FilterCombatInventoryCards(List<DiceCardItemModel> source)
        {
            if (source == null)
                return new List<DiceCardItemModel>();
            var result = new List<DiceCardItemModel>();
            foreach (DiceCardItemModel item in source)
            {
                if (item?.ClassInfo == null)
                    continue;
                if (!IsCardAllowedInCurrentPrepare(item.ClassInfo))
                    continue;
                result.Add(item);
            }
            return result;
        }

        public static List<BookModel> FilterEquipInventoryBooks(List<BookModel> source)
        {
            return FilterRealizationCompendiumBooks(source);
        }

        /// <summary>
        /// The caller supplies LogueBookModels.booklist. During Realization that list
        /// is the temporary projection built exclusively from permanent Compendium
        /// unlocks; outside Realization it is the current RMR route inventory.
        /// Never merge BookInventoryModel's vanilla result here: doing so exposes
        /// unearned pages such as Pluto and Jae-heon. The selected floor's chapter
        /// cap is then applied without changing the authoritative RMR source.
        /// </summary>
        public static List<BookModel> FilterRealizationCompendiumBooks(List<BookModel> source)
        {
            if (source == null)
                return new List<BookModel>();
            var result = new List<BookModel>();
            foreach (BookModel book in source)
            {
                if (book?.ClassInfo == null)
                    continue;
                // RMR librarians keep a private -854..-858 shell whose XML is mutated to
                // represent the equipped collectible page. It is an implementation detail,
                // never another inventory item the player can equip.
                if (RMRCore.IsInternalLibrarianShell(book.ClassInfo.id))
                    continue;
                if (!IsBookAllowedInCurrentPrepare(book.ClassInfo))
                    continue;
                result.Add(book);
            }
            return result;
        }
        #endregion

        #region --- Other helpers ---


        /// <summary>
        /// Remove illegal EGO pages already stuffed into decks (atlas/EGO reward path mistakes).
        /// </summary>
        public static void StripEgoPagesFromPlayerDecks()
        {
            if (LogueBookModels.playerModel == null)
                return;
            foreach (UnitDataModel unit in LogueBookModels.playerModel)
            {
                if (unit?.bookItem == null)
                    continue;
                try
                {
                    List<DiceCardXmlInfo> deck = unit.bookItem.GetCardListFromCurrentDeck();
                    if (deck == null)
                        continue;
                    var toRemove = deck.Where(c => IsEgoCombatPage(c)).Select(c => c.id).Distinct().ToList();
                    foreach (LorId id in toRemove)
                    {
                        try { unit.bookItem.MoveCardFromCurrentDeckToInventory(id); }
                        catch { /* ignore */ }
                    }
                    if (toRemove.Count > 0)
                        Debug.Log($"[RMR] Stripped {toRemove.Count} EGO page id(s) from deck of {unit.name}.");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[RMR] StripEgoPagesFromPlayerDecks: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Collect EGO ids owned this run (route unlock + combat inventory entries).
        /// </summary>
        public static List<LorId> GetOwnedEgoIdsForBattle()
        {
            var ids = new List<LorId>();
            var seen = new HashSet<string>();
            void TryAdd(LorId id)
            {
                if (id == null || id == LorId.None)
                    return;
                string key = (id.packageId ?? "") + ":" + id.id;
                if (!seen.Add(key))
                    return;
                DiceCardXmlInfo card = null;
                try
                {
                    card = ItemXmlDataList.instance.GetCardItem(id, true)
                        ?? ItemXmlDataList.instance.GetCardItem(id.id, true);
                }
                catch { }
                if (card != null && !IsEgoCombatPage(card))
                    return;
                // If XML missing still allow route-tracked ids through.
                if (card == null && !RMRAbnormalityUnlockManager.IsEgoUnlockedForCurrentRoute(id)
                    && !RMRAbnormalityUnlockManager.IsEgoOwnedOnCurrentRoute(id))
                    return;
                ids.Add(id);
            }

            try
            {
                // Route-picked realization EGO.
                // IsEgoUnlockedForCurrentRoute is checked via ownership helper + inventory scan below.
            }
            catch { }

            try
            {
                if (LogueBookModels.cardlist != null)
                {
                    foreach (DiceCardItemModel item in LogueBookModels.cardlist)
                    {
                        if (item?.ClassInfo == null)
                            continue;
                        if (IsEgoCombatPage(item.ClassInfo))
                            TryAdd(item.GetID() ?? item.ClassInfo.id);
                    }
                }
            }
            catch { }

            // Route-unlocked ids even if stripped from decks / not shown in inventory.
            try
            {
                foreach (LorId id in RMRAbnormalityUnlockManager.EnumerateRouteUnlockedEgoPages())
                    TryAdd(id);
            }
            catch { }

            return ids;
        }

        /// <summary>
        /// Grant specific EGO ids into living players' personalEgo hands (mid-battle picks).
        /// Do NOT dump every route-owned EGO at battle start — that forces EGO hand on and
        /// blocks normal combat-page selection.
        /// </summary>
        public static void GrantEgoIdsToBattleUnits(IEnumerable<LorId> egoIds)
        {
            if (egoIds == null)
                return;
            var list = egoIds.Where(id => id != null && id != LorId.None).ToList();
            if (list.Count == 0)
                return;

            int granted = 0;
            try
            {
                List<BattleUnitModel> players = BattleObjectManager.instance?.GetList(Faction.Player);
                if (players == null)
                    return;
                foreach (BattleUnitModel unit in players)
                {
                    if (unit == null || unit.IsDead() || unit.personalEgoDetail == null)
                        continue;
                    foreach (LorId id in list)
                    {
                        try
                        {
                            bool has = false;
                            try
                            {
                                var cards = unit.personalEgoDetail.GetHand();
                                if (cards != null)
                                    has = cards.Any(c => c != null && c.GetID() != null
                                        && c.GetID().id == id.id
                                        && (string.IsNullOrEmpty(c.GetID().packageId) || string.IsNullOrEmpty(id.packageId)
                                            || c.GetID().packageId == id.packageId));
                            }
                            catch { has = false; }
                            if (!has)
                            {
                                unit.personalEgoDetail.AddCard(id);
                                granted++;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[RMR] GrantEgo AddCard failed id={id}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR] GrantEgoIdsToBattleUnits: " + ex.Message);
            }

            if (granted > 0)
                Debug.Log($"[RMR] Granted EGO to personalEgo ({granted} add ops, {list.Count} unique ids).");
        }

        /// <summary>
        /// Legacy entry: only grants EGOs that were selected mid-battle this reception.
        /// Full route inventory is no longer auto-injected at StartBattle.
        /// </summary>
        public static void GrantOwnedEgoToBattleUnits()
        {
            // No-op for bulk grant. Mid-battle picks call GrantEgoIdsToBattleUnits with the pick.
            // Kept so existing call sites compile without re-flooding hands.
        }

        /// <summary>
        /// Clear floor/realization EGO from personalEgo at battle start so hand defaults to combat pages.
        /// Key-page personal EGOs (EgoPersonal) are left alone.
        /// Mid-battle picks re-add only the chosen id via <see cref="GrantEgoIdsToBattleUnits"/>.
        /// </summary>
        public static void ClearFloorEgoFromHandsAtBattleStart()
        {
            try
            {
                List<BattleUnitModel> players = BattleObjectManager.instance?.GetList(Faction.Player);
                if (players == null)
                    return;
                foreach (BattleUnitModel unit in players)
                {
                    if (unit?.personalEgoDetail == null)
                        continue;
                    try
                    {
                        var hand = unit.personalEgoDetail.GetHand();
                        if (hand == null || hand.Count == 0)
                            continue;
                        var toRemove = new List<BattleDiceCardModel>();
                        foreach (BattleDiceCardModel c in hand)
                        {
                            if (c == null)
                                continue;
                            LorId id = c.GetID();
                            DiceCardXmlInfo xml = null;
                            try
                            {
                                xml = c.XmlData
                                    ?? (id != null ? ItemXmlDataList.instance?.GetCardItem(id, true) : null)
                                    ?? (id != null ? ItemXmlDataList.instance?.GetCardItem(id.id, true) : null);
                            }
                            catch { xml = null; }

                            // Keep true key-page personal EGOs only.
                            if (xml != null && xml.optionList != null
                                && xml.optionList.Contains(CardOption.EgoPersonal)
                                && !xml.optionList.Contains(CardOption.EGO))
                                continue;

                            // Drop floor realization EGO / any non-personal EGO combat page.
                            bool isFloorEgo = id != null && RMRAbnormalityUnlockManager.IsRealizationEgoCard(id);
                            bool isRouteEgo = id != null && RMRAbnormalityUnlockManager.IsEgoUnlockedForCurrentRoute(id);
                            bool isEgoPage = xml != null && IsEgoCombatPage(xml);
                            if (isFloorEgo || isRouteEgo || isEgoPage)
                                toRemove.Add(c);
                        }
                        foreach (BattleDiceCardModel c in toRemove)
                        {
                            try { hand.Remove(c); } catch { }
                            try { unit.personalEgoDetail.RemoveCard(c.GetID()); } catch { }
                        }
                        if (toRemove.Count > 0)
                            Debug.Log($"[RMR] Cleared {toRemove.Count} floor EGO from personalEgo hand at battle start ({unit.UnitData?.unitData?.name}).");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("[RMR] ClearFloorEgoFromHands: " + ex.Message);
                    }
                }
            }
            catch { /* ignore */ }
        }

        /// <summary>
        /// Force open hand UI onto combat pages (toggle off, HandState.BattleCard).
        /// Field-only — does NOT re-enter SetCardsObject / SetEgoToggleState (those can OOR
        /// on sp_toggleSprite during early StartBattle / mystery receptions).
        /// Mid-battle EGO pick and unit click use the safer SetCardsObject path separately.
        /// </summary>
        public static void ForceHandUiToBattleCards()
        {
            try
            {
                var handUi = SingletonBehavior<BattleManagerUI>.Instance?.ui_unitCardsInHand;
                if (handUi == null)
                    return;

                try
                {
                    var toggleField = AccessTools.Field(typeof(BattleUnitCardsInHandUI), "toggle_ShowEgo");
                    var toggle = toggleField?.GetValue(handUi) as UnityEngine.UI.Toggle;
                    if (toggle != null)
                    {
                        try { toggle.SetIsOnWithoutNotify(false); }
                        catch
                        {
                            try { toggle.isOn = false; } catch { /* ignore */ }
                        }
                    }
                }
                catch { /* ignore */ }

                try
                {
                    var handStateField = AccessTools.Field(typeof(BattleUnitCardsInHandUI), "_handState");
                    handStateField?.SetValue(handUi, BattleUnitCardsInHandUI.HandState.BattleCard);
                }
                catch { /* ignore */ }

                Debug.Log("[RMR] ForceHandUiToBattleCards: HandState=BattleCard toggle forced off (field-only).");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR] ForceHandUiToBattleCards: " + ex.Message);
            }
        }
        #endregion

        #region --- Restriction rules ---


        /// <summary>
        /// True when hand UI is allowed to open (card-select phase only).
        /// Prevents a floating combat page under "开始战斗" before the player presses space.
        /// </summary>
        public static bool IsHandUiPhaseAllowed()
        {
            try
            {
                StageController sc = Singleton<StageController>.Instance;
                if (sc == null)
                    return false;
                return sc.Phase == StageController.StagePhase.ApplyLibrarianCardPhase;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region --- Other helpers ---


        /// <summary>
        /// Hide the hand root during battle intro (before space / RoundStart) so a single combat
        /// page does not float mid-screen while units still idle under "开始战斗".
        /// Also clears selected/hovered unit so hover cannot immediately re-open SetCardsObject with stale slots.
        /// </summary>
        public static void HideHandUiUntilCombat()
        {
            try
            {
                var handUi = SingletonBehavior<BattleManagerUI>.Instance?.ui_unitCardsInHand;
                if (handUi == null)
                    return;

                try
                {
                    var toggleField = AccessTools.Field(typeof(BattleUnitCardsInHandUI), "toggle_ShowEgo");
                    var toggle = toggleField?.GetValue(handUi) as UnityEngine.UI.Toggle;
                    if (toggle != null)
                    {
                        try { toggle.SetIsOnWithoutNotify(false); }
                        catch
                        {
                            try { toggle.isOn = false; } catch { /* ignore */ }
                        }
                    }
                }
                catch { /* ignore */ }

                try
                {
                    AccessTools.Field(typeof(BattleUnitCardsInHandUI), "_handState")
                        ?.SetValue(handUi, BattleUnitCardsInHandUI.HandState.BattleCard);
                }
                catch { /* ignore */ }

                try
                {
                    AccessTools.Field(typeof(BattleUnitCardsInHandUI), "_selectedUnit")?.SetValue(handUi, null);
                    AccessTools.Field(typeof(BattleUnitCardsInHandUI), "_hOveredUnit")?.SetValue(handUi, null);
                }
                catch { /* ignore */ }

                var rootField = AccessTools.Field(typeof(BattleUnitCardsInHandUI), "_rootObj");
                var root = rootField?.GetValue(handUi) as GameObject;
                if (root != null)
                    root.SetActive(false);

                // Leftover LevelUpUI ego/combat-looking slots from prior reward/EGO pick can float mid-screen.
                try
                {
                    LevelUpUI levelup = SingletonBehavior<BattleManagerUI>.Instance?.ui_levelup;
                    if (levelup != null)
                    {
                        try { levelup.SetRootCanvas(false); } catch { /* ignore */ }
                        try
                        {
                            if (levelup.egoSlotList != null)
                            {
                                for (int i = 0; i < levelup.egoSlotList.Length; i++)
                                {
                                    if (levelup.egoSlotList[i] != null && levelup.egoSlotList[i].gameObject != null)
                                        levelup.egoSlotList[i].gameObject.SetActive(false);
                                }
                            }
                        }
                        catch { /* ignore */ }
                        try
                        {
                            if (levelup.candidates != null)
                            {
                                for (int i = 0; i < levelup.candidates.Length; i++)
                                {
                                    if (levelup.candidates[i] != null && levelup.candidates[i].gameObject != null)
                                        levelup.candidates[i].gameObject.SetActive(false);
                                }
                            }
                        }
                        catch { /* ignore */ }
                    }
                }
                catch { /* ignore */ }

                Debug.Log("[RMR] HideHandUiUntilCombat: hand root deactivated for intro.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR] HideHandUiUntilCombat: " + ex.Message);
            }
        }

        public static bool IsMoneyPlaceholderPassive(LorId id)
        {
            return id != null && id.id == 1
                && (id.packageId == LogLikeMod.ModId
                    || id.packageId == RMRCore.packageId
                    || string.IsNullOrEmpty(id.packageId));
        }

        public static string GetFloorShortName(SephirahType floor)
        {
            string key;
            switch (floor)
            {
                case SephirahType.Malkuth: key = "ui_RMR_Floor_Malkuth"; break;
                case SephirahType.Yesod: key = "ui_RMR_Floor_Yesod"; break;
                case SephirahType.Hod: key = "ui_RMR_Floor_Hod"; break;
                case SephirahType.Netzach: key = "ui_RMR_Floor_Netzach"; break;
                case SephirahType.Tiphereth: key = "ui_RMR_Floor_Tiphereth"; break;
                case SephirahType.Gebura: key = "ui_RMR_Floor_Gebura"; break;
                case SephirahType.Chesed: key = "ui_RMR_Floor_Chesed"; break;
                case SephirahType.Binah: key = "ui_RMR_Floor_Binah"; break;
                case SephirahType.Hokma: key = "ui_RMR_Floor_Hokma"; break;
                case SephirahType.Keter: key = "ui_RMR_Floor_Keter"; break;
                default: return string.Empty;
            }
            try
            {
                string t = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText(key);
                if (!string.IsNullOrEmpty(t) && t != key)
                    return t;
            }
            catch { /* fall through */ }
            return floor.ToString();
        }

        /// <summary>
        /// Former empty-inventory toast (e.g. "当前层限制：没有可显示的核心书页…") — removed as useless noise.
        /// Kept as silent no-op so existing call sites need no change.
        /// </summary>
        public static void NotifyInventoryEmptyIfNeeded(bool isBookInventory, int count)
        {
            // Silent: no modal / alarm when equip (core) or combat page lists are empty after floor filters.
        }
        #endregion

    }
}
