// =============================================================================
// LogLikePatches / LogLikeRoutines - Harmony patches & UI coroutines for LOGLIKE.
// Covers: emotion UI, prepare/battle hooks, save interactions, inv panel glue.
// Related: RMR_Core.cs (RMR_Patches), LogLikeMod.cs (static state).
// Do not rename disk save keys (Lastest, RMR_AtlasPermanentUnlocks, etc.).
// =============================================================================
using BattleCharacterProfile;
using HarmonyLib;
using LOR_DiceSystem;
using LOR_XML;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Workshop;
using RogueLike_Mod_Reborn;


namespace abcdcode_LOGLIKE_MOD
{
    /// <summary>UI coroutines and helpers used by Harmony patches (emotion UI fade, etc.).</summary>
    public static class LogLikeRoutines
    {
        public static IEnumerator DisableRoutine(LevelUpUI self)
        {
            self.cardHidingGroup.alpha = 0.0f;
            float elapsed = 0.0f;
            while ((double)elapsed < 1.0)
            {
                elapsed += TimeManager.GetUIDeltaTime() * 2f;
                self.cardSelectionGroup.alpha = 1f - elapsed;
                yield return null;
            }
            self.SetRootCanvas(false);
            for (int i = 0; i < self.candidates.Length; ++i)
                self.candidates[i].gameObject.SetActive(false);
            for (int j = 0; j < self.egoSlotList.Length; ++j)
                self.egoSlotList[j].gameObject.SetActive(false);
        }

        public static void HideRewardSelectionImmediately(LevelUpUI self)
        {
            if (self == null)
                return;

            self.SetRootCanvas(false);
            if (LogLikeMod.skipPanel != null)
                LogLikeMod.skipPanel.gameObject.SetActive(false);
            if (LogLikeMod.StageRemainPanel != null)
                LogLikeMod.StageRemainPanel.gameObject.SetActive(false);
        }

        public static IEnumerator TranslateRoutine(bool hide, LevelUpUI self)
        {
            float translateDelay = (float)typeof(LevelUpUI).GetField("translateDelay", AccessTools.all).GetValue(self);
            float translateSpeed = (float)typeof(LevelUpUI).GetField("translateSpeed", AccessTools.all).GetValue(self);
            float elapsed = 0.0f;
            Vector2 v = new Vector2(-2027f, -213f);
            Vector2 vector2 = new Vector2(751f, 79f);
            if (hide)
            {
                self.showTranslator.anchoredPosition = Vector2.zero;
                self.cardSelectionGroup.interactable = false;
            }
            else
                self.showTranslator.anchoredPosition = v;
            yield return YieldCache.WaitForSeconds(translateDelay);
            if (hide)
            {
                while ((double)elapsed < 1.0)
                {
                    elapsed += Time.deltaTime * translateSpeed;
                    self.showTranslator.anchoredPosition = Vector2.Lerp(Vector2.zero, v, elapsed * elapsed);
                    yield return YieldCache.waitFrame;
                }
            }
            else
            {
                while ((double)elapsed < 1.0)
                {
                    elapsed += Time.deltaTime * translateSpeed;
                    self.showTranslator.anchoredPosition = Vector2.Lerp(v, Vector2.zero, elapsed);
                    yield return YieldCache.waitFrame;
                }
                self.cardSelectionGroup.interactable = true;
            }
        }

        /// <summary>
        /// Safely extracts the UITextDataLoader and frame Image from a cloned button,
        /// without relying on fragile child-index assumptions.
        /// Returns the loader (or null) and the frame (or null).
        /// </summary>
        public static void SafeGetButtonComponents(Button button, out UITextDataLoader loader, out Image frame)
        {
            loader = null;
            frame = null;
            if (button == null)
                return;
            loader = button.GetComponentInChildren<UITextDataLoader>(true);
            // Frame is typically the first Image child that isn't the button's own targetGraphic.
            var images = button.GetComponentsInChildren<Image>(true);
            if (images != null)
            {
                foreach (var img in images)
                {
                    if (img != null && img != button.targetGraphic)
                    {
                        frame = img;
                        break;
                    }
                }
            }
        }

        private static string GetRMRText(string key, string fallback)
        {
            try
            {
                string text = TextDataModel.GetText(key);
                if (!string.IsNullOrEmpty(text) && text != key)
                    return text;
            }
            catch
            {
            }
            string lang = TextDataModel.CurrentLanguage.ToString().ToLowerInvariant();
            if (key == "ui_Realization")
            {
                if (lang.Contains("en")) return "Realization";
                if (lang.Contains("kr")) return "\ud574\ubc29\uc804";
                if (lang.Contains("jp") || lang.Contains("ja")) return "\u89e3\u653e\u6226";
                return "\u89e3\u653e\u6218";
            }
            if (key == "ui_RealizationDesc")
            {
                if (lang.Contains("en")) return "Challenge a floor realization. Victory permanently unlocks that floor's abnormality and E.G.O pages in the atlas and reward pools.";
                if (lang.Contains("kr")) return "\uac01 \uce35\uc758 \ud574\ubc29\uc804\uc5d0 \ub3c4\uc804\ud558\uc138\uc694. \uc2b9\ub9ac \uc2dc \ud574\ub2f9 \uce35\uc758 \ud658\uc0c1\uccb4 \ud398\uc774\uc9c0\uc640 E.G.O \ud398\uc774\uc9c0\uac00 \ub3c4\uac10\uacfc \ubcf4\uc0c1 \ud480\uc5d0 \uc601\uad6c \ud574\uae08\ub429\ub2c8\ub2e4.";
                if (lang.Contains("jp") || lang.Contains("ja")) return "\u5404\u968e\u306e\u89e3\u653e\u6226\u306b\u6311\u6226\u3059\u308b\u3002\u52dd\u5229\u3059\u308b\u3068\u3001\u305d\u306e\u968e\u306e\u5e7b\u60f3\u4f53\u30da\u30fc\u30b8\u3068E.G.O\u30da\u30fc\u30b8\u304c\u56f3\u9451\u3068\u5831\u916c\u30d7\u30fc\u30eb\u306b\u6c38\u4e45\u89e3\u653e\u3055\u308c\u308b\u3002";
                return "\u6311\u6218\u5404\u5c42\u89e3\u653e\u6218\u3002\u80dc\u5229\u540e\uff0c\u8be5\u5c42\u5f02\u60f3\u4f53\u4e66\u9875\u4e0eE.G.O\u4e66\u9875\u4f1a\u6c38\u4e45\u89e3\u9501\u5230\u56fe\u9274\u548c\u5956\u52b1\u6c60\u3002";
            }
            return fallback;
        }

        private static TextMeshProUGUI GetOrCreateRealizationButtonLabel(Button button)
        {
            if (button == null)
                return null;
            Transform existing = button.transform.Find("RMR_RealizationLabel");
            TextMeshProUGUI label = existing == null ? null : existing.GetComponent<TextMeshProUGUI>();
            if (label == null)
            {
                GameObject go = new GameObject("RMR_RealizationLabel");
                go.transform.SetParent(button.transform, false);
                label = go.AddComponent<TextMeshProUGUI>();
            }
            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            label.transform.SetAsLastSibling();
            label.gameObject.SetActive(true);
            return label;
        }

        public static void ApplyRealizationButtonText(Button button)
        {
            TextMeshProUGUI label = GetOrCreateRealizationButtonLabel(button);
            if (label == null)
                return;
            label.text = GetRMRText("ui_Realization", "\u89e3\u653e\u6218");
            label.enableWordWrapping = false;
            label.fontSize = 32f;
            LogLikeMod.ApplyTmpFontPreservingSharpMaterial(label, LogLikeMod.DefFont_TMP);
            label.color = LogLikeMod.DefFontColor;
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Normal;
            label.raycastTarget = false;
        }

        public static void OnClickCraftTab(UIBattleSettingEditPanel __instance)
        {
            UISoundManager.instance.PlayEffectSound(UISoundType.Ui_Click);
            __instance.SetBUttonState((UIBattleSettingEditTap)4);
        }

        public static void OnClickCreatureTab(UIBattleSettingEditPanel __instance)
        {
            UISoundManager.instance.PlayEffectSound(UISoundType.Ui_Click);
            __instance.SetBUttonState((UIBattleSettingEditTap)3);
        }

        public static void OnClickCompendiumTab(UIBattleSettingEditPanel __instance)
        {
            // Atlas is no longer a prepare tab; ignore prepare-panel clicks.
            UISoundManager.instance.PlayEffectSound(UISoundType.Ui_Click);
            Debug.Log("[RMR] Atlas tab removed from prepare — open 图鉴 from start hub.");
        }

        public static void OnClickRealization(UIBattleSettingEditPanel __instance)
        {
            UISoundManager.instance.PlayEffectSound(UISoundType.Ui_Click);
            if (LogLikeRoutines.IsRoguelikeBattleSettingContext())
            {
                // Open the realization selection panel
                var panel = Singleton<LogRealizationPanel>.Instance;
                if (panel == null)
                {
                    GameObject go = new GameObject("LogRealizationPanel");
                    panel = go.AddComponent<LogRealizationPanel>();
                }
                panel.Show(__instance);
            }
        }

        public static void OnClickInventory(UIBattleSettingEditPanel __instance)
        {
            UISoundManager.instance.PlayEffectSound(UISoundType.Ui_Click);
            __instance.SetBUttonState((UIBattleSettingEditTap)2);
        }

        public static void SetBattleSettingCardPanelVisible(bool visible)
        {
            UIBattleSettingPanel panel = UI.UIController.Instance.GetUIPanel(UIPanelType.BattleSetting) as UIBattleSettingPanel;
            if (panel == null || panel.EditPanel == null)
                return;
            UISettingCardInvenPanel battleCardPanel = LogLikeMod.GetFieldValue<UISettingCardInvenPanel>(panel.EditPanel, "_battleCardPanel");
            if (battleCardPanel != null)
                battleCardPanel.SetActivePanel(visible);
        }

        public static bool IsRoguelikeBattleSettingContext()
        {
            // Realization prepare is intentional RMR context.
            if (RMRRealizationManager.IsRealizationPreparationActive)
                return true;
            // Full roguelike reception only — never true on vanilla library load.
            return LogLikeMod.CheckStage();
        }

        /// <summary>
        /// Force-unlock key page / combat page slots on the battle-setting librarian panel.
        /// Vanilla sets isBattlePageLock/isEquipPageLock when the floor was already used, or greys
        /// them while the matching edit tab is open; RMR prepare must stay editable.
        /// </summary>
        public static void ForceUnlockBattleSettingLoadoutSlots(UIBattleSettingLibrarianInfoPanel panel)
        {
            if (panel == null || !IsRoguelikeBattleSettingContext())
                return;
            Color uiColor = UIColorManager.Manager.GetUIColor(UIColor.Default);
            typeof(UIBattleSettingLibrarianInfoPanel).GetField("isBattlePageLock", AccessTools.all).SetValue(panel, false);
            typeof(UIBattleSettingLibrarianInfoPanel).GetField("isEquipPageLock", AccessTools.all).SetValue(panel, false);
            panel.SetBattlePageSlotColor(uiColor);
            panel.SetEquipPageSlotColor(uiColor);
        }

        /// <summary>
        /// Open equip / battle-card edit panel from librarian info, ignoring vanilla gates:
        /// - LibraryModel.GetEndContentState() == KeterCompleteOpen (post-game saves silent-block)
        /// - isBattlePageLock / isEquipPageLock (used-floor lock)
        /// Passive succession never hits those gates, which is why it still worked.
        /// </summary>
        public static bool TryOpenBattleSettingEditFromLibrarian(
          UIBattleSettingLibrarianInfoPanel panel,
          UIBattleSettingEditTap tap,
          BaseEventData eventData)
        {
            if (panel == null || !IsRoguelikeBattleSettingContext())
                return false;
            if (eventData != null && UIControlManager.GetInpuTypeOf(eventData) == InputType.RightClick)
                return true; // swallow right-click like vanilla, no open
            if (tap == UIBattleSettingEditTap.BattleCard)
            {
                UnitDataModel unit = LogLikeMod.GetFieldValue<UnitDataModel>(panel, "unitdata");
                if (unit == null)
                    return true;
            }
            ForceUnlockBattleSettingLoadoutSlots(panel);
            UISoundManager.instance.PlayEffectSound(UISoundType.Ui_Click);
            panel.GetSettingPanel().OnClickOpenEditPage(tap);
            return true;
        }

        public static void InitUIBattleSettingWaveSlot(
          UIBattleSettingWaveSlot slot,
          UIBattleSettingWaveList list)
        {
            FieldInfo field1 = slot.GetType().GetField("panel", AccessTools.all);
            FieldInfo field2 = slot.GetType().GetField("rect", AccessTools.all);
            FieldInfo field3 = slot.GetType().GetField("img_circle", AccessTools.all);
            FieldInfo field4 = slot.GetType().GetField("img_circleglow", AccessTools.all);
            FieldInfo field5 = slot.GetType().GetField("img_Icon", AccessTools.all);
            FieldInfo field6 = slot.GetType().GetField("img_IconGlow", AccessTools.all);
            FieldInfo field7 = slot.GetType().GetField("hsv_Icon", AccessTools.all);
            FieldInfo field8 = slot.GetType().GetField("hsv_IconGlow", AccessTools.all);
            FieldInfo field9 = slot.GetType().GetField("hsv_Circle", AccessTools.all);
            FieldInfo field10 = slot.GetType().GetField("hsv_CircleGlow", AccessTools.all);
            FieldInfo field11 = slot.GetType().GetField("txt_Alarm", AccessTools.all);
            FieldInfo field12 = slot.GetType().GetField("materialsetter_txtAlarm", AccessTools.all);
            FieldInfo field13 = slot.GetType().GetField("arrow", AccessTools.all);
            FieldInfo field14 = slot.GetType().GetField("defeatColor", AccessTools.all);
            FieldInfo field15 = slot.GetType().GetField("anim", AccessTools.all);
            FieldInfo field16 = slot.GetType().GetField("cg", AccessTools.all);
            field1.SetValue(slot, list);
            RectTransform transform = slot.transform as RectTransform;
            field2.SetValue(slot, transform);
            field3.SetValue(slot, slot.gameObject.transform.GetChild(1).GetChild(1).gameObject.GetComponent<Image>());
            field4.SetValue(slot, slot.gameObject.transform.GetChild(1).GetChild(0).gameObject.GetComponent<Image>());
            field5.SetValue(slot, slot.gameObject.transform.GetChild(1).GetChild(3).gameObject.GetComponent<Image>());
            field6.SetValue(slot, slot.gameObject.transform.GetChild(1).GetChild(2).gameObject.GetComponent<Image>());
            field7.SetValue(slot, slot.gameObject.transform.GetChild(1).GetChild(3).gameObject.GetComponent<_2dxFX_HSV>());
            field8.SetValue(slot, slot.gameObject.transform.GetChild(1).GetChild(2).gameObject.GetComponent<_2dxFX_HSV>());
            field9.SetValue(slot, slot.gameObject.transform.GetChild(1).GetChild(1).gameObject.GetComponent<_2dxFX_HSV>());
            field10.SetValue(slot, slot.gameObject.transform.GetChild(1).GetChild(0).gameObject.GetComponent<_2dxFX_HSV>());
            field11.SetValue(slot, slot.gameObject.transform.GetChild(2).gameObject.GetComponent<TextMeshProUGUI>());
            field12.SetValue(slot, slot.gameObject.transform.GetChild(2).gameObject.GetComponent<TextMeshProMaterialSetter>());
            field13.SetValue(slot, slot.gameObject.transform.GetChild(0).gameObject.GetComponent<Image>());
            Color color = new Color(0.454902f, 0.1098039f, 0.0f, 1f);
            field14.SetValue(slot, color);
            field15.SetValue(slot, slot.gameObject.GetComponent<Animator>());
            field16.SetValue(slot, slot.gameObject.GetComponent<CanvasGroup>());
            slot.transform.localPosition = (Vector3)new Vector2(120f, 0.0f);
            slot.gameObject.SetActive(false);
        }

        public static void InitUIBattleSettingWaveSlots(
          List<UIBattleSettingWaveSlot> slots,
          UIBattleSettingWaveList __instance)
        {
            float num = 5f / (float)slots.Count;
            for (int index = 0; index < slots.Count; ++index)
                slots[index].gameObject.transform.localScale = new Vector3(1f, 1f);
        }

        public static void ChangeEPCUTransform(EmotionPassiveCardUI __instance)
        {
            LevelUpUI uiLevelup = SingletonBehavior<BattleManagerUI>.Instance.ui_levelup;
            if ((double)uiLevelup.selectedEmotionCard.transform.localPosition.x > 0.0)
            {
                uiLevelup.selectedEmotionCard.transform.localPosition = new Vector3(-410f, -510f);
                uiLevelup.selectedEmotionCardBg.transform.localScale = new Vector3(1f, 1f, 1f);
            }
            else
            {
                uiLevelup.selectedEmotionCard.transform.localPosition = new Vector3(410f, -510f);
                uiLevelup.selectedEmotionCardBg.transform.localScale = new Vector3(-1f, 1f, 1f);
            }
        }
    }

    /// <summary>LOGLIKE type: LogLikeHooks</summary>

    public class LogLikeHooks
    {
        private static readonly FieldInfo UIBattleSettingPanelSephirahButtonsField = typeof(UIBattleSettingPanel).GetField("SephirahButtons", AccessTools.all);
        private static readonly FieldInfo BattleUnitEmotionDetailSelfField = typeof(BattleUnitEmotionDetail).GetField("_self", AccessTools.all);
        private static readonly MethodInfo BattleUnitEmotionDetailGetEmotionCoinAdderMethod = typeof(BattleUnitEmotionDetail).GetMethod("GetEmotionCoinAdder", AccessTools.all);
        private static readonly FieldInfo BattleUnitEmotionDetailEmotionCoinsField = typeof(BattleUnitEmotionDetail).GetField("_emotionCoins", AccessTools.all);
        private static readonly FieldInfo EmotionCoinCoinTypeField = typeof(EmotionCoin).GetField("_coinType", AccessTools.all);
        private static readonly FieldInfo BattleUnitCardsInHandUIRootObjField = typeof(BattleUnitCardsInHandUI).GetField("_rootObj", AccessTools.all);
        private static readonly FieldInfo BattleUnitCardsInHandUIToggleShowEgoField = typeof(BattleUnitCardsInHandUI).GetField("toggle_ShowEgo", AccessTools.all);
        private static readonly FieldInfo BattleUnitCardsInHandUIIsOverOnEgoToggleField = typeof(BattleUnitCardsInHandUI).GetField("isOverOnEgoToggle", AccessTools.all);
        private static readonly FieldInfo BattleUnitCardsInHandUISelectedUnitField = typeof(BattleUnitCardsInHandUI).GetField("_selectedUnit", AccessTools.all);
        private static readonly FieldInfo BattleUnitCardsInHandUIHoveredUnitField = typeof(BattleUnitCardsInHandUI).GetField("_hOveredUnit", AccessTools.all);
        private static readonly FieldInfo BattleUnitCardsInHandUIHandStateField = typeof(BattleUnitCardsInHandUI).GetField("_handState", AccessTools.all);
        private static readonly FieldInfo BattleUnitCardsInHandUISpToggleSpriteField = typeof(BattleUnitCardsInHandUI).GetField("sp_toggleSprite", AccessTools.all);
        private static readonly MethodInfo BattleUnitCardsInHandUISetEgoToggleStateMethod = typeof(BattleUnitCardsInHandUI).GetMethod("SetEgoToggleState", AccessTools.all);
        private static readonly MethodInfo BattleUnitCardsInHandUIUpdateCardListMethod = typeof(BattleUnitCardsInHandUI).GetMethod("UpdateCardList", AccessTools.all);
        private static readonly MethodInfo BattleUnitCardsInHandUIIsActivatedMethod = typeof(BattleUnitCardsInHandUI).GetMethod("IsActivated", AccessTools.all);

        /// <summary>
        /// Vanilla SetEgoToggleState indexes sp_toggleSprite[0..2] unconditionally.
        /// Early StartBattle / mystery receptions can have a short or null sprite array → ArgumentOutOfRange.
        /// </summary>
        private static bool CanSafelyCallSetEgoToggleState(BattleUnitCardsInHandUI self)
        {
            if (self == null || BattleUnitCardsInHandUISpToggleSpriteField == null)
                return false;
            try
            {
                var sprites = BattleUnitCardsInHandUISpToggleSpriteField.GetValue(self) as UnityEngine.Sprite[];
                return sprites != null && sprites.Length >= 3;
            }
            catch { return false; }
        }

        /// <summary>
        /// Apply hand / EGO-toggle state without relying on vanilla SetEgoToggleState sprite indexing.
        /// Prefer vanilla when sprites are ready; otherwise only set HandState + toggle flags.
        /// </summary>
        private static void ApplyHandEgoToggleSafe(
            BattleUnitCardsInHandUI self,
            BattleUnitCardsInHandUI.EgoToggleState state)
        {
            if (self == null)
                return;

            // Always pin combat hand state first for Hide/Off.
            try
            {
                if (state == BattleUnitCardsInHandUI.EgoToggleState.On)
                    BattleUnitCardsInHandUIHandStateField?.SetValue(self, BattleUnitCardsInHandUI.HandState.EgoCard);
                else
                    BattleUnitCardsInHandUIHandStateField?.SetValue(self, BattleUnitCardsInHandUI.HandState.BattleCard);
            }
            catch { /* ignore */ }

            Toggle toggle = null;
            try { toggle = BattleUnitCardsInHandUIToggleShowEgoField?.GetValue(self) as Toggle; } catch { toggle = null; }

            if (CanSafelyCallSetEgoToggleState(self) && BattleUnitCardsInHandUISetEgoToggleStateMethod != null)
            {
                try
                {
                    BattleUnitCardsInHandUISetEgoToggleStateMethod.Invoke(self, new object[] { state });
                    return;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[RMR] SetEgoToggleState invoke failed, using field fallback: " +
                        (ex.InnerException != null ? ex.InnerException.Message : ex.Message));
                }
            }

            // Field-only fallback (no sprite array access).
            try
            {
                if (toggle != null)
                {
                    try { toggle.SetIsOnWithoutNotify(state == BattleUnitCardsInHandUI.EgoToggleState.On); }
                    catch
                    {
                        try { toggle.isOn = state == BattleUnitCardsInHandUI.EgoToggleState.On; } catch { }
                    }
                    try { toggle.interactable = state == BattleUnitCardsInHandUI.EgoToggleState.On
                        || state == BattleUnitCardsInHandUI.EgoToggleState.Off; } catch { }
                    try
                    {
                        if (toggle.gameObject != null)
                            toggle.gameObject.SetActive(state != BattleUnitCardsInHandUI.EgoToggleState.Hide);
                    }
                    catch { /* ignore */ }
                }
            }
            catch { /* ignore */ }

            // Refresh card list only when UI is active; swallow OOR from empty formation.
            try
            {
                bool activated = false;
                if (BattleUnitCardsInHandUIIsActivatedMethod != null)
                    activated = (bool)BattleUnitCardsInHandUIIsActivatedMethod.Invoke(self, null);
                if (activated && BattleUnitCardsInHandUIUpdateCardListMethod != null)
                    BattleUnitCardsInHandUIUpdateCardListMethod.Invoke(self, null);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR] UpdateCardList fallback skipped: " +
                    (ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }

        private static void QueueDropBookReward(DropBookXmlInfo reward, LorId sourceId = null)
        {
            if (LogLikeMod.rewards == null)
                LogLikeMod.rewards = new List<DropBookXmlInfo>();
            LorId detectedId = sourceId ?? reward?.id;
            if (detectedId != null && (string.IsNullOrEmpty(detectedId.packageId) || detectedId.packageId == "@origin"))
            {
                LorId normalizedId = GetCurrentChapterDropValueRewardId();
                DropBookXmlInfo normalized = Singleton<DropBookXmlList>.Instance.GetData(normalizedId);
                if (normalized != null)
                    reward = normalized;
                else
                    Debug.LogWarning($"[RMR RewardFlow] Chapter drop book not found for vanilla enemy reward {detectedId}: {normalizedId}.");
            }
            if (reward == null)
                return;
            LogLikeMod.rewards.Add(reward);
        }

        private static LorId GetCurrentChapterDropValueRewardId()
        {
            int suffix = 1;
            if (LogLikeMod.curstagetype == StageType.Elite)
                suffix = 2;
            else if (LogLikeMod.curstagetype == StageType.Boss)
                suffix = 4;
            int chapterNumber = (int)LogLikeMod.curchaptergrade + 1;
            return new LorId(LogLikeMod.ModId, chapterNumber * 1000 + suffix);
        }

        private static void SkipCurrentRewardSelection(LevelUpUI self)
        {
            if (RewardingModel.rewardFlag == RewardingModel.RewardFlag.CardReward)
            {
                List<DiceCardXmlInfo> cardlist = new List<DiceCardXmlInfo>();
                if (self != null && self.egoSlotList != null)
                {
                    foreach (BattleDiceCardUI egoSlot in self.egoSlotList)
                    {
                        if (egoSlot != null && egoSlot.CardModel != null && egoSlot.CardModel.XmlData != null)
                            cardlist.Add(egoSlot.CardModel.XmlData);
                    }
                }
                Singleton<GlobalLogueEffectManager>.Instance.OnSkipCardRewardChoose(cardlist);
                if (LogLikeMod.rewards != null && LogLikeMod.rewards.Count > 0)
                    LogLikeMod.rewards.RemoveAt(0);
            }
            else if (RewardingModel.rewardFlag == RewardingModel.RewardFlag.EgoCardReward)
            {
                if (LogLikeMod.egoSelectionQueue != null && LogLikeMod.egoSelectionQueue.Count > 0)
                    LogLikeMod.egoSelectionQueue.RemoveAt(0);
                // Mid-battle EGO skip must resume combat — never enter post-battle StartPickReward / TryEndRun.
                if (RewardingModel.IsMidBattleEgoSelectionActive)
                {
                    try { RewardingModel.NoteMidBattleEgoPicked(LorId.None); } catch { }
                    try { RewardingModel.rewardFlag = RewardingModel.RewardFlag.EmtoionChoose; } catch { }
                    if (self != null)
                        self.SetRootCanvas(false);
                    try { RMRPrepareRestrictions.ForceHandUiToBattleCards(); } catch { }
                    Debug.Log("[RMR] Mid-battle EGO skipped — resume combat (no StartPickReward).");
                    return;
                }
            }
            else if (RewardingModel.rewardFlag == RewardingModel.RewardFlag.PassiveReward)
            {
                if (LogLikeMod.rewards_passive != null && LogLikeMod.rewards_passive.Count > 0)
                    LogLikeMod.rewards_passive.RemoveAt(0);
            }
            else
            {
                return;
            }

            if (self != null)
                self.SetRootCanvas(false);
            RewardingModel.StartPickReward();
        }

        /// <summary>
        /// Hook for hijacing Crrying Children's map manager.
        /// </summary>
        public void CryingChildMapManager_InitInvitationMap(Action<BattleSceneRoot, MapManager> orig, BattleSceneRoot self, MapManager m)
        {
            if (LogLikeMod.CheckStage(true) && m is CryingChildMapManager)
            {
                var newmap = m.gameObject.AddComponent<CryingChildrenLogKys>();
                UnityEngine.Object.Destroy(m);
                m = newmap;
            }
            orig(self, m);
        }

        /// <summary>
        /// Hook for fixing an annoying passive attribution bug where the game fails to find a passive.
        /// </summary>
        public void BookModel_ChangePassive(Action<BookModel, PassiveModel, PassiveModel> orig, BookModel self, PassiveModel currentBookPassive, PassiveModel changeBookPassive)
        {
            if (!LogLikeMod.CheckStage())
            {
                orig(self, currentBookPassive, changeBookPassive);
                return;
            }
            currentBookPassive.SuccessionPassiveForReserved(changeBookPassive);
        }

        /// <summary>
        /// Hook for changing empty/partially empty decks giving out default pages.
        /// </summary>
        public List<DiceCardXmlInfo> UnitDataModel_GetDeckForBattle(Func<UnitDataModel, int, List<DiceCardXmlInfo>> orig, UnitDataModel self, int index)
        {
            try
            {
                if (LogueBookModels.TryGetGrade6SpecialBuiltInDeckCards(self, out List<DiceCardXmlInfo> builtInDeck))
                    return builtInDeck;

                bool useRogueDeck = (LogLikeMod.CheckStage(true) || RMRRealizationManager.InRealizationBattle)
                    && RMRCore.CurrentGamemode != null
                    && RMRCore.CurrentGamemode.ReplaceBaseDeck;
                if (!useRogueDeck)
                    return orig(self, index);

                // NEVER call self.GetCardList(index) directly — private method, MonoMod DynamicMethod
                // throws MethodAccessException and aborts StartBattle / CreateLibrarianUnit.
                List<DiceCardXmlInfo> list = InvokeUnitGetCardList(self, index);
                if (list == null)
                    return orig(self, index);

                int deckSize = self.GetDeckSize();
                int curSize = list.Count;
                if (self.bookItem != null
                    && self.bookItem.ClassInfo != null
                    && self.bookItem.ClassInfo.RangeType != EquipRangeType.Range
                    && curSize < deckSize)
                {
                    var deckData = DeckXmlList.Instance.GetData(RMRCore.CurrentGamemode.BaseDeckReplacement);
                    if (deckData?.cardIdList != null)
                    {
                        var defaultDeck = deckData.cardIdList;
                        for (int i = curSize; i < deckSize && i < defaultDeck.Count; i++)
                        {
                            DiceCardXmlInfo card = ItemXmlDataList.instance.GetCardItem(defaultDeck[i], false);
                            if (card != null)
                                list.Add(card);
                        }
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR] UnitDataModel_GetDeckForBattle fallback to vanilla: " + ex.Message);
                return orig(self, index);
            }
        }

        /// <summary>Reflect private UnitDataModel.GetCardList(int) for MonoMod-safe access.</summary>
        private static List<DiceCardXmlInfo> InvokeUnitGetCardList(UnitDataModel self, int index)
        {
            if (self == null)
                return null;
            try
            {
                MethodInfo m = AccessTools.Method(typeof(UnitDataModel), "GetCardList", new[] { typeof(int) });
                if (m == null)
                    return null;
                return m.Invoke(self, new object[] { index }) as List<DiceCardXmlInfo>
                    ?? new List<DiceCardXmlInfo>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR] InvokeUnitGetCardList failed: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Preserves the vanilla fixed-deck behavior for Black Silence and Binah while
        /// their source BookXmlInfo is projected onto a Roguelike unit.
        /// </summary>
        public bool BookModel_IsFixedDeck(Func<BookModel, bool> orig, BookModel self)
        {
            if (LogLikeRoutines.IsRoguelikeBattleSettingContext() && LogueBookModels.IsEditableBlueReverberationDeck(self))
                return false;
            if (LogueBookModels.HasGrade6SpecialBuiltInDeck(self))
                return true;
            return orig(self);
        }

        /// <summary>
        /// Displays the original vanilla fixed deck instead of the Roguelike starter deck.
        /// </summary>
        public List<DiceCardXmlInfo> BookModel_GetCardListFromCurrentDeck(
            Func<BookModel, List<DiceCardXmlInfo>> orig,
            BookModel self)
        {
            if (LogueBookModels.TryGetGrade6SpecialBuiltInDeckCards(self, out List<DiceCardXmlInfo> builtInDeck))
                return builtInDeck;
            return orig(self);
        }

        /// <summary>
        /// Hook for relocalizing abnormality pages.
        /// </summary>
        public AbnormalityCard AbnormalityCardDescXmlList_GetAbnormalityCard(
          Func<AbnormalityCardDescXmlList, string, AbnormalityCard> orig,
          AbnormalityCardDescXmlList self,
          string cardID)
        {
            if (!LogLikeMod.CheckStage())
                return orig(self, cardID);

            // Prefer vanilla loader first so real localization is never skipped.
            // The old path permanently cached "Not found" stubs, which then blocked
            // later loads and poisoned Name-keyed entries (SnowWhite_Vine etc.).
            try
            {
                AbnormalityCard fromVanilla = orig(self, cardID);
                if (fromVanilla != null
                    && !PickUpModel_RMRVanillaEmotion.IsMissingDesc(fromVanilla))
                    return fromVanilla;
            }
            catch
            {
                // Fall through to dictionary / stub path.
            }

            Dictionary<string, AbnormalityCard> dictionary = (Dictionary<string, AbnormalityCard>)typeof(AbnormalityCardDescXmlList).GetField("_dictionary", AccessTools.all).GetValue(self);
            if (dictionary != null && !string.IsNullOrEmpty(cardID) && dictionary.TryGetValue(cardID, out AbnormalityCard existing)
                && existing != null && !PickUpModel_RMRVanillaEmotion.IsMissingDesc(existing))
                return existing;

            // Transient stub — do NOT cache under cardID so a later language refresh can fill it.
            return new AbnormalityCard()
            {
                id = cardID,
                abnormalityName = "Not found",
                cardName = "Not found",
                abilityDesc = "Not found",
                flavorText = "Not found",
                dialogues = (List<AbnormalityCardDialog>)null
            };
        }

        /// <summary>
        /// Hook for closing the abno page UI.
        /// </summary>
        public void UIGetAbnormalityPanel_PointerClickButton(
          Action<UIGetAbnormalityPanel> orig,
          UIGetAbnormalityPanel self)
        {
            if (LogLikeMod.CheckStage() || LogLikeMod.CheckStage(true))
                self.Close();
            else
                orig(self);
        }

        /// <summary>
        /// Hook for pausing the battle scene whenever:<br></br>
        /// - <see cref="LogLikeMod.PauseBool"/> is true<br></br>
        /// - A mystery event is currently running<br></br>
        /// - An event interrupt is currently running<br></br>
        /// - A shop is currently running
        /// </summary>
        public void BattleSceneRoot_Update(Action<BattleSceneRoot> orig, BattleSceneRoot self)
        {
            if (LogLikeMod.CheckStage() && (Singleton<MysteryManager>.Instance.curMystery != null || Singleton<MysteryManager>.Instance.interruptMysterys != null && Singleton<MysteryManager>.Instance.interruptMysterys.Count > 0 || Singleton<ShopManager>.Instance.curshop != null || LogLikeMod.PauseBool))
                return;
            orig(self);
        }

        /// <summary>
        /// Hook for giving newly-generated units their adequate default deck replacements.
        /// </summary>
        public LorId BookXmlInfo_get_DeckId(Func<BookXmlInfo, LorId> orig, BookXmlInfo self)
        {
            var stage = Singleton<StageController>.Instance.GetStageModel();
            if (self.id.packageId == LogLikeMod.ModId // check for roguelike 
                && ((stage == null || !RoguelikeGamemodeController.Instance.isContinue) && self.id.id == -854) // check for wave start + main player
                || (LogLikeMod.AddPlayer && self.id.id >= -858 && self.id.id <= -855)) // if not above, then check if player is just being added
            {
                return RMRCore.CurrentGamemode.BaseDeckReplacement;
            }
            return orig(self);
        }

        /// <summary>
        /// Hook for sephirah floor buttons on battle prepare.
        /// RMR receptions now use the vanilla single-floor budget. The player may choose
        /// any initially available floor for its map/BGM, but after that floor is defeated
        /// vanilla must close the others and report GameOver.
        /// </summary>
        public void UIBattleSettingPanel_SetCurrentSephirahButton(
          Action<UIBattleSettingPanel> orig,
          UIBattleSettingPanel self)
        {
            orig(self);
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return;
            try
            {
                // Preserve vanilla button states. Only remove old RMR text labels.
                List<UISephirahButton> buttons = UIBattleSettingPanelSephirahButtonsField?.GetValue(self) as List<UISephirahButton>;
                if (buttons == null)
                    return;
                foreach (UISephirahButton btn in buttons)
                {
                    if (btn == null)
                        continue;
                    // Prepare UI: icons only — do not stamp RMR floor short names under sephirah buttons.
                    ClearSephirahFloorLabel(btn);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR] SetCurrentSephirahButton label cleanup failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Remove any RMR_FloorLabel under a sephirah button (prepare/编队 should not label floors).
        /// Also covers realization prepare which reuses UIBattleSettingPanel.
        /// </summary>
        private static void ClearSephirahFloorLabel(UISephirahButton btn)
        {
            if (btn == null)
                return;
            const string childName = "RMR_FloorLabel";
            try
            {
                Transform existing = btn.transform.Find(childName);
                if (existing != null)
                    UnityEngine.Object.Destroy(existing.gameObject);
            }
            catch { /* ignore */ }
        }

        /* UNUSED
        public void WorkshopSkinDataSetter_LateInit(
          Action<WorkshopSkinDataSetter> orig,
          WorkshopSkinDataSetter self)
        {
            orig(self);
        }
        */

        /// <summary>
        /// Hook for limiting emotion level gain depending on the chapter.
        /// </summary>
        public int BattleUnitEmotionDetail_CreateEmotionCoin(
          Func<BattleUnitEmotionDetail, EmotionCoinType, int, int> orig,
          BattleUnitEmotionDetail self,
          EmotionCoinType coinType,
          int count = 1)
        {
            // Realization librarians are temporary Compendium projections. Their saved
            // OwnerSephirah can differ from the floor currently being realized, while
            // vanilla CreateEmotionCoin returns 0 when StageModel.GetFloor(owner) is null.
            // Temporarily bind the projection to the active floor so the complete vanilla
            // coin path runs (passive callbacks, team E.G.O. cooldown, and IV -> V included).
            if (RMRRealizationManager.InRealizationBattle)
            {
                BattleUnitModel realizationUnit =
                    BattleUnitEmotionDetailSelfField.GetValue(self) as BattleUnitModel;
                if (realizationUnit != null && realizationUnit.faction == Faction.Player)
                    return CreateRealizationEmotionCoinsWithCurrentFloor(orig, self, coinType, count);
                return orig(self, coinType, count);
            }
            if (!LogLikeMod.CheckStage(true))
                return orig(self, coinType, count);
            return CreateRmrEmotionCoins(
                self,
                coinType,
                count,
                Math.Min((int)(LogLikeMod.curchaptergrade + 1), 5));
        }

        private static int CreateRealizationEmotionCoinsWithCurrentFloor(
            Func<BattleUnitEmotionDetail, EmotionCoinType, int, int> orig,
            BattleUnitEmotionDetail self,
            EmotionCoinType coinType,
            int count)
        {
            self.SetMaxEmotionLevel(5);
            BattleUnitModel battleUnitModel =
                BattleUnitEmotionDetailSelfField.GetValue(self) as BattleUnitModel;
            UnitDataModel unitData = battleUnitModel?.UnitData?.unitData;
            StageController controller = Singleton<StageController>.Instance;
            FieldInfo ownerSephirahField = AccessTools.Field(typeof(UnitDataModel), "_ownerSephirah");
            if (unitData == null || controller == null || ownerSephirahField == null)
                return CreateRmrEmotionCoins(self, coinType, count, 5);

            SephirahType originalFloor = (SephirahType)ownerSephirahField.GetValue(unitData);
            SephirahType currentFloor = controller.CurrentFloor;
            bool rebound = originalFloor != currentFloor;
            try
            {
                if (rebound)
                    ownerSephirahField.SetValue(unitData, currentFloor);
                return orig(self, coinType, count);
            }
            finally
            {
                if (rebound)
                    ownerSephirahField.SetValue(unitData, originalFloor);
            }
        }

        private static int CreateRmrEmotionCoins(
            BattleUnitEmotionDetail self,
            EmotionCoinType coinType,
            int count,
            int maximumEmotionLevel)
        {
            self.SetMaxEmotionLevel(maximumEmotionLevel);
            BattleUnitModel battleUnitModel = (BattleUnitModel)BattleUnitEmotionDetailSelfField.GetValue(self);
            if (battleUnitModel.faction == Faction.Player && battleUnitModel.UnitData.unitData.gender == Gender.Creature)
                return 0;
            count += (int)BattleUnitEmotionDetailGetEmotionCoinAdderMethod.Invoke(self, new object[1]
            {
       count
            });
            if (battleUnitModel.faction == Faction.Player)
                battleUnitModel.personalEgoDetail.AddEgoCoolTime(count);
            if (self.EmotionLevel >= self.MaximumEmotionLevel)
                return 0;
            List<EmotionCoin> emotionCoinList = (List<EmotionCoin>)BattleUnitEmotionDetailEmotionCoinsField.GetValue(self);
            for (int index = 0; index < count; ++index)
            {
                if (emotionCoinList.Count < self.MaximumCoinNumber)
                {
                    //if (!self.OnGetEmotionCoin(coinType))
                    //  ;
                    EmotionCoin uninitializedObject = (EmotionCoin)FormatterServices.GetUninitializedObject(typeof(EmotionCoin));
                    EmotionCoinCoinTypeField.SetValue(uninitializedObject, coinType);
                    emotionCoinList.Add(uninitializedObject);
                    self.totalEmotionCoins.Add(uninitializedObject);
                }
            }
            return count;
        }

        /// <summary>
        /// Hook for resetting the emotion level of all librarians and clearing out abno pages.
        /// </summary>
        public void StageController_EndBattlePhase(
          Action<StageController, float> orig,
          StageController self,
          float deltaTime)
        {
            // Realization battles have independent reward handling — skip Roguelike reward chain
            if (RMRRealizationManager.InRealizationBattle)
            {
                orig(self, deltaTime);
                return;
            }

            if (LogLikeMod.CheckStage(true))
            {
                if (LogLikeMod.purpleexcept)
                {
                    orig(self, deltaTime);
                    return;
                }
                // If EndBattlePhase was entered while both sides still fight (emotion-5 EGO glitch),
                // resume combat instead of hanging on RewardClearStage forever.
                try
                {
                    if (!RewardingModel.IsNonCombatNodeStage()
                        && RewardingModel.IsLiveCombatBothSidesAlive())
                    {
                        Debug.Log("[RMR] EndBattlePhase recovery: both sides alive → RoundStartPhase_System (abort reward end).");
                        try { RewardingModel.ClearSuppressSpuriousEndBattle(); } catch { }
                        try { LogLikeMod.EndBattle = false; } catch { }
                        try
                        {
                            LogLikeMod.SetStagePhase(self, StageController.StagePhase.RoundStartPhase_System);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning("[RMR] EndBattlePhase recovery SetStagePhase failed: " + ex.Message);
                        }
                        return;
                    }
                }
                catch { /* continue into normal reward path */ }

                RMRAbnormalityUnlockManager.GrantRedMistChallengeVictoryRewards();
                RMRAbnormalityUnlockManager.RecordBlackSilenceVictoryUnlock();
                RMRAbnormalityUnlockManager.GrantDistortedEnsembleVictoryRewards();
                if (!RewardingModel.RewardClearStage(self))
                    return;
                if (!LogLikeMod.EndBattle)
                {
                    if (LogLikeMod.curstagetype == StageType.Boss)
                        RMRAbnormalityUnlockManager.RecordPermanentClear(LogLikeMod.curchaptergrade);
                    self.EndBattle();
                    Singleton<GlobalLogueEffectManager>.Instance.OnEndBattle();
                    LogLikeMod.EndBattle = true;
                    foreach (BattleUnitModel battleUnitModel in BattleObjectManager.instance.GetList(Faction.Player))
                    {
                        battleUnitModel.emotionDetail.SetEmotionLevel(0);
                        battleUnitModel.emotionDetail.PassiveList.Clear();
                    }
                    // Between nodes: prune dead spine caches + optional GC (classic long-session lag).
                    try { RMRSessionHygiene.OnNodeTransition(heavy: true); } catch { /* ignore */ }
                }
            }
            orig(self, deltaTime);
        }

        /// <summary>
        /// Returns true for Grade4+ — the high chapters that receive an extra EquipPage reward.
        /// </summary>
        private static bool IsHighChapterExtraEquipRewardChapter(ChapterGrade grade)
        {
            return grade == ChapterGrade.Grade4
                || grade == ChapterGrade.Grade5
                || grade == ChapterGrade.Grade6
                || grade == ChapterGrade.Grade7;
        }

        /// <summary>
        /// Creates an extra EquipPage-only reward by filtering the current chapter's CommonReward pool.
        /// Returns null if no EquipPage entries exist in the pool.
        /// </summary>
        private static RewardInfo CreateExtraEquipPageReward(ChapterGrade grade)
        {
            var allRewards = Singleton<RewardPassivesList>.Instance.GetChapterData(grade, PassiveRewardListType.CommonReward, LorId.None);
            if (allRewards == null)
                return null;
            var equipPages = allRewards.Where(x => x != null && x.rewardtype == RewardType.EquipPage).ToList();
            if (equipPages.Count == 0)
                return null;
            return new RewardInfo { grade = grade, rewards = equipPages };
        }

        /// <summary>
        /// Adds an extra EquipPage reward to <see cref="LogLikeMod.rewards_passive"/> if the current chapter
        /// is Grade4/5/6 and the CommonReward pool contains EquipPage entries.
        /// Call this only after a battle reward (Common/Elite/Boss) has already been added.
        /// </summary>
        private static void TryAddExtraEquipPageReward()
        {
            if (!IsHighChapterExtraEquipRewardChapter(LogLikeMod.curchaptergrade))
                return;
            RewardInfo extraReward = CreateExtraEquipPageReward(LogLikeMod.curchaptergrade);
            if (extraReward != null)
                LogLikeMod.rewards_passive.Add(extraReward);
        }

        /// <summary>
        /// Hook for initializing stage rewards and enabling money UI.
        /// Also runs <see cref="GlobalLogueEffectManager.OnStartBattle"/> (and OnStartBattleAfter).
        /// </summary>
        private static void ResetBattleEgoSelectionState(StageController controller)
        {
            // Clear vanilla mid-battle EGO state — RMR drives emotion-3/4/5 EGO picks via
            // RewardingModel mid-battle queue (after abno), not egoSelectionPoint.
            Singleton<SpecialCardListModel>.Instance.Init();
            StageModel stage = controller?.GetStageModel();
            if (stage == null)
                return;
            foreach (StageLibraryFloorModel floor in stage.GetAvailableFloorList())
            {
                ModdingUtils.GetFieldValue<List<EmotionEgoXmlInfo>>("_selectedEgoList", floor)?.Clear();
                ModdingUtils.GetFieldValue<List<EmotionEgoXmlInfo>>("_selectableEgoList", floor)?.Clear();
                if (floor.team != null)
                    floor.team.egoSelectionPoint = 0;
            }
            try { RewardingModel.ResetMidBattleEgoSelectionState(); } catch { }
        }

        private static void PrepareRealizationCombatResources(StageController controller)
        {
            ResetBattleEgoSelectionState(controller);
            LogueBookModels.EnsureCompendiumUnlocks();
            LogueBookModels.selectedEmotion = new List<RewardPassiveInfo>();
            LogueBookModels.EmotionCardList = RMRAbnormalityUnlockManager.GetUnlockedEmotionCardsForBattle();
            LogLikeMod.curemotion = 0;
            LogLikeMod.egoSelectionQueue = new List<List<LorId>>();
            Debug.Log(
                $"[RMRRealizationManager] Prepared Compendium combat resources: " +
                $"abno={LogueBookModels.EmotionCardList?.Count ?? 0}, " +
                $"ego={LogueBookModels.CompendiumUnlockedEgoPages?.Count ?? 0}.");
        }

        private sealed class PurpleTransitionEmotionState
        {
            public int EmotionLevel;
            public int MaxEmotionLevel;
        }

        private static Dictionary<UnitDataModel, PurpleTransitionEmotionState> CapturePurpleTransitionEmotionState()
        {
            Dictionary<UnitDataModel, PurpleTransitionEmotionState> result = new Dictionary<UnitDataModel, PurpleTransitionEmotionState>();
            foreach (BattleUnitModel unit in BattleObjectManager.instance.GetList(Faction.Player))
            {
                if (unit?.UnitData?.unitData == null || unit.emotionDetail == null)
                    continue;
                result[unit.UnitData.unitData] = new PurpleTransitionEmotionState
                {
                    EmotionLevel = unit.emotionDetail.EmotionLevel,
                    MaxEmotionLevel = unit.emotionDetail.MaximumEmotionLevel
                };
            }
            return result;
        }

        private static void RestorePurpleTransitionEmotionState(Dictionary<UnitDataModel, PurpleTransitionEmotionState> states)
        {
            if (states == null || states.Count == 0)
                return;
            foreach (BattleUnitModel unit in BattleObjectManager.instance.GetList(Faction.Player))
            {
                if (unit?.UnitData?.unitData == null || unit.emotionDetail == null)
                    continue;
                if (!states.TryGetValue(unit.UnitData.unitData, out PurpleTransitionEmotionState state))
                    continue;
                int maxLevel = Math.Max(state.MaxEmotionLevel, Math.Min((int)(LogLikeMod.curchaptergrade + 1), 5));
                unit.emotionDetail.SetMaxEmotionLevel(maxLevel);
                unit.emotionDetail.SetEmotionLevel(Math.Min(state.EmotionLevel, maxLevel));
            }
        }

        public void StageController_StartBattle(Action<StageController> orig, StageController self)
        {
            RMRRealizationManager.EnterStartBattleHook();
            try
            {
                StageController_StartBattle_Inner(orig, self);
            }
            finally
            {
                RMRRealizationManager.ExitStartBattleHook();
            }
        }

        private void StageController_StartBattle_Inner(Action<StageController> orig, StageController self)
        {
            // If StartBattle runs before OnWaveStart (common), still complete realization launch
            // so we never deadlock: abort forever with reception=True and no Pending battle.
            if (RMRRealizationManager.RealizationReceptionActive
                && !RMRRealizationManager.PendingRealizationBattle
                && !RMRRealizationManager.InRealizationBattle
                && !RMRCore.IsPostInvitationLaunchConsumed)
            {
                try
                {
                    Debug.Log("[RMR] StartBattle: running HandlePostInvitationLaunch early (before OnWaveStart).");
                    RMRCore.HandlePostInvitationLaunch();
                }
                catch (Exception ex)
                {
                    Debug.LogError("[RMR] Early HandlePostInvitationLaunch failed: " + ex);
                }
            }

            // HandlePost may open realization prepare mid-hook. Abort THIS shell StartBattle
            // so we do not ActivatePending + orig immediately (skip team confirm → chaos).
            if (RMRRealizationManager.ConsumeAbortCurrentStartBattle())
            {
                LogLikeMod.PauseBool = false;
                Debug.Log("[RMR] StartBattle aborted after opening realization prepare — wait for player team confirm.");
                return;
            }

            // Only block dummy 854 while waiting for floor pick on prepare UI.
            // Do NOT block solely on RealizationReceptionActive — that prevented OnWaveStart
            // and left the player with no battle / instant "end".
            if (RMRRealizationManager.AwaitingRealizationFloorPick
                && !RMRRealizationManager.PendingRealizationBattle
                && !RMRRealizationManager.InRealizationBattle)
            {
                LogLikeMod.PauseBool = true;
                Debug.LogWarning("[RMR] StartBattle aborted — pick a realization floor first (no dummy combat).");
                // Do not OpenBattlePrepare under floor pick — overlay alone is enough.
                try { RMRRealizationLaunchHost.EnsureFloorPanelVisible(); } catch { }
                return;
            }

            // Player confirmed team on BattlePrepare: Pending is still true until here.
            // Activate InRealizationBattle, then run vanilla StartBattle for the boss stage.
            if (RMRRealizationManager.PendingRealizationBattle)
            {
                RMRRealizationManager.ActivatePendingRealization();
            }

            // Realization battles skip the normal Roguelike reward initialization
            if (RMRRealizationManager.InRealizationBattle)
            {
                // StartBattle can run again during vanilla multi-phase transitions.
                // Only reset the choice state before the first combat round; otherwise
                // selected pages and accumulated emotion would be lost between phases.
                if (!RMRRealizationManager.RealizationCombatLive)
                    PrepareRealizationCombatResources(self);
                orig(self);
                // Vanilla Floor Realization grants floor EGO into personalEgo and can leave the
                // hand UI stuck in EGO state — the roguelike post-orig cleanup below never runs
                // on this early-return path. Force the hand back to normal battle cards so the
                // player sees combat pages by default (EGO stays reachable via the toggle).
                try { RMRPrepareRestrictions.ForceHandUiToBattleCards(); } catch { }
                try { RMRPrepareRestrictions.HideHandUiUntilCombat(); } catch { }
                return;
            }

            // Purple Tear's half-HP teleport restarts the current floor battle without
            // ending the reception. Keep librarian emotion, selected abnormality pages,
            // EGO cards and reward queues intact until the forced transition completes.
            bool isPurpleTransition = LogLikeMod.CheckStage() && LogLikeMod.purpleexcept;
            if (isPurpleTransition)
            {
                Dictionary<UnitDataModel, PurpleTransitionEmotionState> emotionStates = CapturePurpleTransitionEmotionState();
                orig(self);
                RestorePurpleTransitionEmotionState(emotionStates);
                LogLikeMod.purpleexcept = false;
                return;
            }

            bool isRoguelikeBattle = LogLikeRoutines.IsRoguelikeBattleSettingContext();
            if (isRoguelikeBattle)
            {
                // Reset stale EndBattle flag at battle start. Without this, a later
                // party wipe returns "defeat" from RewardClearStage every frame but
                // skips the actual EndBattle call because the previous node left it true.
                LogLikeMod.EndBattle = false;
                ResetBattleEgoSelectionState(self);
                RewardingModel.ResetDropBookRewardNormalization();
                RewardingModel.ResetBattleVictoryConfirmation();
                RMRAbnormalityUnlockManager.ResetRedMistChallengeBattleState();
                LogLikeMod.BattleMoneyUI.Active();
                LogueBookModels.selectedEmotion = new List<RewardPassiveInfo>();
                LogueBookModels.EmotionCardList = RMRAbnormalityUnlockManager.GetUnlockedEmotionCardsForBattle();
                LogLikeMod.curemotion = 0;
                LogLikeMod.purpleexcept = false;
                LogLikeMod.rewards_InStage = new List<RewardInfo>();
                LogLikeMod.rewards = new List<DropBookXmlInfo>();
                LogLikeMod.rewards_lastKill = new List<DropBookXmlInfo>();
                LogLikeMod.rewards_passive = new List<RewardInfo>();
                LogLikeMod.rewardsMystery = new List<LorId>();
                LogLikeMod.egoSelectionQueue = new List<List<LorId>>();
                //if (!LogLikeMod.Debugging)
                //  ;
                LogLikeMod.PlayerEquipOrders = new List<EquipChangeOrder>();
                if (LogLikeMod.curstagetype == StageType.Normal)
                {
                    if (LogLikeMod.NormalRewardCool <= 0)
                    {
                        LogLikeMod.rewards_passive.Add(RewardInfo.GetCurChapterCommonReward(LogLikeMod.curchaptergrade));
                        LogLikeMod.NormalRewardCool = 0;
                        TryAddExtraEquipPageReward();
                    }
                    else
                        --LogLikeMod.NormalRewardCool;
                }
                if (LogLikeMod.curstagetype == StageType.Elite)
                {
                    RewardInfo eliteReward = RewardInfo.GetCurChapterEliteReward(LogLikeMod.curchaptergrade);
                    if (eliteReward != null)
                    {
                        LogLikeMod.rewards_passive.Add(eliteReward);
                        TryAddExtraEquipPageReward();
                    }
                }
                if (LogLikeMod.curstagetype == StageType.Creature)
                    LogLikeMod.NormalRewardCool = 0;
                if (LogLikeMod.curChStageStep != 0 && LogLikeMod.curstagetype == StageType.Boss && LogueBookModels.RemainStageList.ContainsKey(LogLikeMod.curchaptergrade + 1))
                {
                    RewardInfo chapterBossReward = RewardInfo.GetCurChapterBossReward(LogLikeMod.curchaptergrade);
                    if (chapterBossReward != null)
                    {
                        LogLikeMod.rewards_passive.Add(chapterBossReward);
                        TryAddExtraEquipPageReward();
                    }
                }
                RMRAbnormalityUnlockManager.SuppressRedMistChallengeGenericRewards();
                // ResetNextStage already builds Grade(N+1) options after a Boss (including Grade6→Grade7 杂质).
                // Do not re-clear here with a conflicting final-chapter rule.
                LogLikeMod.ResetNextStage();
                if (LogLikeMod.curstagetype == StageType.Boss
                    && (LogLikeMod.nextlist == null || LogLikeMod.nextlist.Count == 0)
                    && LogLikeMod.curchaptergrade < ChapterGrade.Grade7)
                {
                    ChapterGrade nextGrade = LogLikeMod.curchaptergrade + 1;
                    if (LogueBookModels.EnsureChapterRemainStages(nextGrade))
                    {
                        LogLikeMod.nextlist = LogueBookModels.GetNextList(nextGrade, true);
                        Debug.Log($"[RMR StartBattle] Boss nextlist was empty; rebuilt chapter {nextGrade} options={LogLikeMod.nextlist?.Count ?? 0}");
                    }
                }
                Singleton<GlobalLogueEffectManager>.Instance.OnStartBattle();
                Singleton<GlobalLogueEffectManager>.Instance.UpdateSprites();
            }
            else
            {
                LogLikeMod.ResetUIs();
            }
            if (isRoguelikeBattle)
                LogLikeMod.RunStartBattleWithCurrentNodeDefinition(self, () => orig(self));
            else
                orig(self);
            if (!isRoguelikeBattle)
                return;
            RewardingModel.ObserveLivingEnemyForVictoryConfirmation();
            Singleton<GlobalLogueEffectManager>.Instance.OnStartBattleAfter();
            // Do NOT dump all owned EGO into personalEgo at start — that forces the EGO hand UI and
            // blocks combat-page selection. Mid-battle picks grant only the chosen id.
            try { RMRPrepareRestrictions.ClearFloorEgoFromHandsAtBattleStart(); } catch { }
            // Hide hand during start/intro so a stray card ("协同突击" etc.) does not float on screen
            // before the player presses space. RoundStart / unit click re-opens via SetCardsObject.
            try { RMRPrepareRestrictions.HideHandUiUntilCombat(); } catch { }
        }

        /// <summary>
        /// Hook for initializing librarian units in roguelike.
        /// </summary>
        public void StageController_CreateLibrarianUnit(
          Action<StageController, SephirahType> orig,
          StageController self,
          SephirahType sephirah)
        {
            if (RMRRealizationManager.InRealizationBattle)
            {
                CreateRoguelikeLibrarianUnits(self, sephirah, false);
                return;
            }

            if (LogLikeMod.CheckStage(true))
            {
                CreateRoguelikeLibrarianUnits(self, sephirah, true);
            }
            else
                orig(self, sephirah);
        }

        private static void CreateRoguelikeLibrarianUnits(
            StageController self,
            SephirahType sephirah,
            bool applyLogueEffects)
        {
            BattleTeamModel battleTeamModel = (BattleTeamModel)typeof(StageController).GetField("_librarianTeam", AccessTools.all).GetValue(self);
            int num = 0;
            foreach (UnitBattleDataModel unitBattleData in LogueBookModels.playerBattleModel.FindAll(x => x.IsAddedBattle))
            {
                StageLibraryFloorModel floor = self.GetStageModel().GetFloor(sephirah);
                UnitDataModel unitData = unitBattleData.unitData;
                BattleUnitModel defaultUnit = BattleObjectManager.CreateDefaultUnit(Faction.Player);
                defaultUnit.index = num;
                defaultUnit.grade = unitData.grade;
                defaultUnit.formation = floor.GetFormationPosition(defaultUnit.index);
                defaultUnit.SetUnitData(unitBattleData);
                defaultUnit.OnCreated();
                battleTeamModel.AddUnit(defaultUnit);
                BattleObjectManager.instance.RegisterUnit(defaultUnit);
                defaultUnit.passiveDetail.OnUnitCreated();
                defaultUnit.SetDeadSceneBlock(true);
                ++num;
                if (applyLogueEffects)
                    Singleton<GlobalLogueEffectManager>.Instance.OnCreateLibrarian(defaultUnit);
            }
            if (applyLogueEffects)
                Singleton<GlobalLogueEffectManager>.Instance.OnCreateLibrarians();
        }

        /// <summary>
        /// Hook for prompting abno page choice screens.
        /// </summary>
        public bool StageController_RoundEndPhase_ChoiceEmotionCard(
          Func<StageController, bool> orig,
          StageController self)
        {
            // Realizations use the same tier-aware choice flow as RMR combat, backed by
            // the permanent Compendium pools prepared at StartBattle.
            if (!LogLikeMod.CheckStage(true) && !RMRRealizationManager.InRealizationBattle)
                return orig(self);
            foreach (BattleUnitModel alive in BattleObjectManager.instance.GetAliveList())
            {
                if (!alive.view.FormationReturned)
                    return false;
            }
            return RewardingModel.EmotionChoice();
        }

        /* UNUSED
        public void StageController_InitStageByInvitation(
          Action<StageController, StageClassInfo, List<LorId>> orig,
          StageController self,
          StageClassInfo stage,
          List<LorId> books = null)
        {
            orig(self, stage, books);
        }
        */

        /// <summary>
        /// Hook for transferring enemy book rewards into the reward pool at the end of the Act.
        /// </summary>
        public void StageController_OnEnemyDropBookForAdded(
          Action<StageController, DropBookDataForAddedReward> orig,
          StageController self,
          DropBookDataForAddedReward data)
        {
            if (LogLikeMod.CheckStage(true))
            {
                LogLikeMod.rewards_lastKill = new List<DropBookXmlInfo>();
                DropBookXmlInfo data1 = Singleton<DropBookXmlList>.Instance.GetData(data.id);
                QueueDropBookReward(data1, data.id);
                if (data1 != null)
                    LogLikeMod.rewards_lastKill.Add(data1);
            }
            else
                orig(self, data);
        }

        /// <summary>
        /// Hook for handling Roguelike acts actually ending.
        /// </summary>
        public void StageController_EndBattle(Action<StageController> orig, StageController self)
        {
            // Handle realization battle end ONLY after combat actually went live.
            // StartBattle→EndBattle transitions (before round 1) must not open the start hub.
            if (RMRRealizationManager.InRealizationBattle && self.Phase != StageController.StagePhase.EndBattle)
            {
                if (!RMRRealizationManager.ShouldHandleRealizationBattleEnd())
                {
                    Debug.Log("[RMRRealizationManager] Spurious EndBattle during realization transition — forwarding vanilla only.");
                    orig(self);
                    return;
                }

                // Multi-phase Angela/Roland: Corrosion etc. keep boss immortal until final form.
                // If EndBattle fires mid-phase (or during phase swap), do NOT clear floor / exit.
                if (RMRRealizationManager.IsMidRealizationMultiPhase())
                {
                    Debug.Log("[RMRRealizationManager] EndBattle during multi-phase transition — forward vanilla only, keep realization active.");
                    try { orig(self); }
                    catch (Exception ex) { Debug.LogWarning("[RMR] Mid-phase EndBattle orig: " + ex.Message); }
                    return;
                }

                bool victory = false;
                try
                {
                    victory = BattleObjectManager.instance.GetAliveListWithAvailable(Faction.Player).Count > 0
                        && BattleObjectManager.instance.GetAliveListWithAvailable(Faction.Enemy).Count == 0;
                }
                catch { }

                // Real stage clear of Floor Realization (all phases done, or defeat).
                RMRRealizationManager.OnRealizationBattleEnded(victory);
                try { orig(self); }
                catch (Exception ex) { Debug.LogWarning("[RMR] Realization EndBattle orig: " + ex.Message); }

                RMRRealizationManager.ClearRealizationFlag();
                RMRRealizationManager.ReturnToMainAfterRealization();
                RMRRealizationManager.EnsureExitBattleToLibrary();
                try { RMRSessionHygiene.OnNodeTransition(heavy: true); } catch { /* ignore */ }
                return;
            }

            if (LogLikeMod.CheckStage(true) && self.Phase != StageController.StagePhase.EndBattle)
            {
                // Spurious EndBattle while both factions still fight must not enter EndBattlePhase
                // (seen after emotion-5 abno+EGO: field clears → looks like instant enemy wipe).
                // Shop/mystery/rest and Purple Tear transition are excluded.
                try
                {
                    if (!LogLikeMod.purpleexcept
                        && !RewardingModel.IsNonCombatNodeStage()
                        && RewardingModel.IsLiveCombatBothSidesAlive())
                    {
                        Debug.Log("[RMR] Ignoring EndBattle while both sides still alive (live combat).");
                        return;
                    }
                }
                catch { /* fall through to normal reward EndBattle path */ }

                LogLikeMod.EndBattle = false;
                LogLikeMod.SetStagePhase(self, StageController.StagePhase.EndBattle);
            }
            else
                orig(self);
        }

        /// <summary>
        /// Hook for manipulating the UI for PickUpModel abno page popup screen.
        /// </summary>
        public IEnumerator LevelUpUI_OnSelectRoutine(Func<LevelUpUI, IEnumerator> orig, LevelUpUI self)
        {
            if (LogLikeMod.CheckStage())
            {
                FieldInfo _needUnitSelection = typeof(LevelUpUI).GetField("_needUnitSelection", AccessTools.all);
                FieldInfo _selectedCard = typeof(LevelUpUI).GetField("_selectedCard", AccessTools.all);
                self.cardSelectionGroup.interactable = false;
                yield return YieldCache.WaitForSeconds(0.1f);
                if ((bool)_needUnitSelection.GetValue(self))
                {
                    EmotionCardXmlInfo einfo = _selectedCard.GetValue(self) as EmotionCardXmlInfo;
                    LorId id = new LorId(LogLikeMod.GetPickUpXmlWorkShopId_Passive(einfo), einfo.id);
                    List<BattleUnitModel> aliveList = BattleObjectManager.instance.GetList(Faction.Player);
                    RewardPassiveInfo pinfo = Singleton<RewardPassivesList>.Instance.GetPassiveInfo(id);
                    PickUpModelBase pickupmodel = null;
                    if (pinfo != null)
                        pickupmodel = LogLikeMod.FindPickUp(pinfo.script);
                    if (aliveList != null)
                    {
                        if (pickupmodel != null)
                        {
                            if (pickupmodel.GetPickupTarget() != null)
                            {
                                aliveList = pickupmodel.GetPickupTarget();
                                goto label_11;
                            }
                            EmotionCardXmlInfo info = _selectedCard.GetValue(self) as EmotionCardXmlInfo;
                            pickupmodel.id = new LorId(LogLikeMod.GetPickUpXmlWorkShopId_Passive(info), info.id);
                            aliveList.RemoveAll(x => !pickupmodel.IsCanPickUp(x.UnitData.unitData));
                            info = null;
                        }
                        else
                            aliveList.RemoveAll(x => x.IsDead());
                    }
                label_11:
                    if (aliveList.Count > 0)
                    {
                        if (_selectedCard.GetValue(self) != null)
                            self.selectedEmotionCard.Init((EmotionCardXmlInfo)_selectedCard.GetValue(self), true);
                        if (Singleton<StageController>.Instance.AllyFormationDirection == Direction.LEFT)
                        {
                            self.selectedEmotionCard.transform.localPosition = new Vector3(410f, -510f);
                            self.selectedEmotionCardBg.transform.localScale = new Vector3(-1f, 1f, 1f);
                        }
                        else
                        {
                            self.selectedEmotionCard.transform.localPosition = new Vector3(-410f, -510f);
                            self.selectedEmotionCardBg.transform.localScale = new Vector3(1f, 1f, 1f);
                        }
                        List<UICustomSelectable_autofind> list = new List<UICustomSelectable_autofind>();
                        foreach (BattleUnitModel battleUnitModel in aliveList)
                        {
                            battleUnitModel.view.abCardSelector.Init(battleUnitModel, Singleton<StageController>.Instance.GetCurrentStageFloorModel().team.emotionLevel);
                            list.Add(battleUnitModel.view.abCardSelector.selectable);
                        }
                        foreach (UICustomSelectable_autofind uicustomSelectable_autofind in list)
                            uicustomSelectable_autofind.SetActivatedCharacterSelectables(list);
                        if (list.Count > 0)
                            BattleUIInputController.Instance.SelectSelectableForcely(list[0]);
                        self.OnSelectHide();
                        list = null;
                    }
                    else
                    {
                        self.StartCoroutine(LogLikeRoutines.DisableRoutine(self));
                        self.StartCoroutine(LogLikeRoutines.TranslateRoutine(true, self));
                    }
                    einfo = null;
                    id = null;
                    aliveList = null;
                    pinfo = null;
                }
                else
                {
                    self.StartCoroutine(LogLikeRoutines.DisableRoutine(self));
                    self.StartCoroutine(LogLikeRoutines.TranslateRoutine(true, self));
                }
            }
            else
                yield return orig(self);
        }

        /// <summary>
        /// Hook for manipulating the UI for PickUpModel abno page popup screen.
        /// </summary>
        public void LevelUpUI_SetEmotionPerDataUI(
          Action<LevelUpUI, float, float> orig,
          LevelUpUI self,
          float positivevalue,
          float negativevalue)
        {
            try
            {
                if (LogLikeMod.CheckStage(true))
                {
                    TextMeshProUGUI textMeshProUgui1 = (TextMeshProUGUI)typeof(LevelUpUI).GetField("txt_PositiveValueText", AccessTools.all).GetValue(self);
                    TextMeshProUGUI textMeshProUgui2 = (TextMeshProUGUI)typeof(LevelUpUI).GetField("txt_NegativeValueText", AccessTools.all).GetValue(self);
                    RectTransform rectTransform = (RectTransform)typeof(LevelUpUI).GetField("rect_BarContent", AccessTools.all).GetValue(self);
                    textMeshProUgui1.text = "0";
                    textMeshProUgui2.text = "0";
                    rectTransform.anchoredPosition = Vector2.zero;
                    rectTransform.anchoredPosition = new Vector2(0.0f, 0.0f);
                    return;
                }
            }
            catch
            {
                orig(self, positivevalue, negativevalue);
            }
            orig(self, positivevalue, negativevalue);
        }

        /// <summary>
        /// Hook for manipulating the UI for rewards. All of them.
        /// </summary>
        public void LevelUpUI_InitBase(
          Action<LevelUpUI, int, bool> orig,
          LevelUpUI self,
          int selectedCount,
          bool isEgo = false)
        {
            orig(self, selectedCount, isEgo);
            // Vanilla just filled the reward slots. Their names come from mod data that may still be
            // Chinese while the UI language is English, and the English face has no CJK glyphs, so
            // they draw as 口口口. Scope the repair to this panel: the scene-wide sweep costs a
            // FindObjectsOfType over every TMP in the scene and its 4s throttle almost always
            // expires between battles, so it ran in full right as the reward screen opened.
            try { LogLikeMod.RepairTmpFontsUnder(self.gameObject, "LevelUpUI.InitBase"); }
            catch { /* never block the reward UI */ }
            if (LogLikeMod.CheckStage(true))
            {
                TextMeshProUGUI textMeshProUgui1 = (TextMeshProUGUI)typeof(LevelUpUI).GetField("txt_SelectDesc", AccessTools.all).GetValue(self);
                TextMeshProUGUI textMeshProUgui2 = (TextMeshProUGUI)typeof(LevelUpUI).GetField("txt_BtnSelectDesc", AccessTools.all).GetValue(self);
                if (LogLikeMod.skipPanel == null)
                {
                    Button button = ModdingUtils.CreateButton(self.selectablePanel.transform.parent, "AbCardSelection_Skip", new Vector2(1f, 1f), new Vector2(650f, 385f));
                    LogLikeMod.skipPanel = button;
                    LogLikeMod.skipPanelText = ModdingUtils.CreateText_TMP(button.transform, new Vector2(-10f, 0.0f), (int)textMeshProUgui2.fontSize, new Vector2(0.0f, 0.0f), new Vector2(1f, 1f), new Vector2(0.0f, 0.0f), TextAlignmentOptions.Midline, textMeshProUgui2.color, textMeshProUgui2.font);
                }
                LogLikeMod.skipPanel.onClick.RemoveAllListeners();
                LogLikeMod.skipPanel.onClick.AddListener(() => SkipCurrentRewardSelection(self));
                if (LogLikeMod.StageRemainText == null)
                {
                    UILogCustomSelectable BackGround = ModdingUtils.CreateLogSelectable(self.selectablePanel.transform.parent, "AbCardSelection_Skip", new Vector2(1f, 1f), new Vector2(0.0f, 500f));
                    BackGround.SelectEvent = new UnityEventBasedata();
                    BackGround.SelectEvent.AddListener((UnityAction<BaseEventData>)(e =>
                    {
                        string name = TextDataModel.GetText("ui_stageremain") + LogueBookModels.RemainStageList[LogLikeMod.curchaptergrade].Count.ToString();
                        Dictionary<string, int> dictionary = new Dictionary<string, int>();
                        foreach (LogueStageInfo info in LogueBookModels.RemainStageList[LogLikeMod.curchaptergrade])
                        {
                            LogueBookModels.CreateStageDesc(info);
                            EmotionCardXmlInfo pickUpXml = LogLikeMod.GetRegisteredPickUpXml(info);
                            if (pickUpXml == null)
                                continue;
                            AbnormalityCard abnormalityCard = Singleton<AbnormalityCardDescXmlList>.Instance.GetAbnormalityCard(pickUpXml.Name);
                            if (abnormalityCard == null)
                                continue;
                            if (!dictionary.ContainsKey(abnormalityCard.cardName))
                                dictionary[abnormalityCard.cardName] = 1;
                            else
                                ++dictionary[abnormalityCard.cardName];
                        }
                        string desc = string.Empty;
                        foreach (KeyValuePair<string, int> keyValuePair in dictionary)
                            desc = desc + keyValuePair.Key + keyValuePair.Value.ToString() + Environment.NewLine;
                        SingletonBehavior<UIBattleOverlayManager>.Instance.EnableBufOverlay(name, desc, (Sprite)null, BackGround.gameObject);
                    }));
                    BackGround.DeselectEvent = new UnityEventBasedata();
                    BackGround.DeselectEvent.AddListener((UnityAction<BaseEventData>)(e => SingletonBehavior<UIBattleOverlayManager>.Instance.DisableOverlay()));
                    TextMeshProUGUI textTmp = ModdingUtils.CreateText_TMP(BackGround.transform, new Vector2(-10f, 0.0f), (int)textMeshProUgui2.fontSize, new Vector2(0.0f, 0.0f), new Vector2(1f, 1f), new Vector2(0.0f, 0.0f), TextAlignmentOptions.Midline, textMeshProUgui2.color, textMeshProUgui2.font);
                    LogLikeMod.StageRemainPanel = BackGround;
                    LogLikeMod.StageRemainText = textTmp;
                }
                int num;
                switch (RewardingModel.rewardFlag)
                {
                    case RewardingModel.RewardFlag.CardReward:
                        num = 1;
                        break;
                    case RewardingModel.RewardFlag.PassiveReward:
                        RewardPassiveInfo firstPassive = LogLikeMod.rewards_passive != null
                            && LogLikeMod.rewards_passive.Count > 0
                            && LogLikeMod.rewards_passive[0] != null
                            && LogLikeMod.rewards_passive[0].rewards != null
                            && LogLikeMod.rewards_passive[0].rewards.Count > 0
                            ? LogLikeMod.rewards_passive[0].rewards[0]
                            : null;
                        num = firstPassive != null && firstPassive.rewardtype != RewardType.Creature ? 1 : 0;
                        break;
                    default:
                        num = 0;
                        break;
                }
                if (num != 0)
                {
                    LogLikeMod.skipPanel.gameObject.SetActive(true);
                    LogLikeMod.skipPanelText.text = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("ui_selectskip");
                }
                else
                    LogLikeMod.skipPanel.gameObject.SetActive(false);
                if (RewardingModel.rewardFlag == RewardingModel.RewardFlag.NextStageChoose)
                {
                    LogLikeMod.StageRemainPanel.gameObject.SetActive(true);
                    TextMeshProUGUI stageRemainText1 = LogLikeMod.StageRemainText;
                    string text1 = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("ui_stageremain");
                    int count = LogueBookModels.RemainStageList.TryGetValue(LogLikeMod.curchaptergrade, out List<LogueStageInfo> curChapterList) && curChapterList != null
                        ? curChapterList.Count : 0;
                    string str1 = count.ToString();
                    string str2 = text1 + str1;
                    stageRemainText1.text = str2;
                    if (LogLikeMod.curstagetype == StageType.Boss && LogLikeMod.curchaptergrade < ChapterGrade.Grade7)
                    {
                        TextMeshProUGUI stageRemainText2 = LogLikeMod.StageRemainText;
                        string text2 = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("ui_stageremain");
                        if (LogueBookModels.RemainStageList.TryGetValue(LogLikeMod.curchaptergrade + 1, out List<LogueStageInfo> nextChapterList) && nextChapterList != null)
                            count = nextChapterList.Count;
                        else
                            count = 0;
                        string str3 = count.ToString();
                        string str4 = text2 + str3;
                        stageRemainText2.text = str4;
                    }
                    // C3: match title style (select desc font / color / size).
                    try
                    {
                        if (LogLikeMod.StageRemainText != null && textMeshProUgui1 != null)
                        {
                            if (LogLikeMod.DefFont_TMP != null)
                                LogLikeMod.ApplyTmpFontPreservingSharpMaterial(LogLikeMod.StageRemainText, LogLikeMod.DefFont_TMP);
                            else if (textMeshProUgui1.font != null)
                                LogLikeMod.ApplyTmpFontPreservingSharpMaterial(LogLikeMod.StageRemainText, textMeshProUgui1.font);
                            LogLikeMod.StageRemainText.fontSize = textMeshProUgui1.fontSize;
                            LogLikeMod.StageRemainText.color = textMeshProUgui1.color;
                            LogLikeMod.StageRemainText.alignment = TextAlignmentOptions.Center;
                        }
                    }
                    catch { /* ignore style sync */ }
                }
                else
                    LogLikeMod.StageRemainPanel.gameObject.SetActive(false);
                if (RewardingModel.rewardFlag == RewardingModel.RewardFlag.CardReward)
                {
                    textMeshProUgui1.text = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("BattleEnd_CardReward");
                    textMeshProUgui2.text = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("BattleEnd_CardReward");
                    if (!Singleton<TutorialManager>.Instance.IsSeeTuto("tutorial_BattlePage1_1"))
                        Singleton<TutorialManager>.Instance.LoadTuto("tutorial_BattlePage1_1");
                }
                if (RewardingModel.rewardFlag == RewardingModel.RewardFlag.EgoCardReward)
                {
                    string egoTitle = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("BattleEnd_EgoReward");
                    if (string.IsNullOrEmpty(egoTitle) || egoTitle.IndexOf("BattleEnd_EgoReward", StringComparison.Ordinal) >= 0)
                    {
                        string lang = "";
                        try { lang = TextDataModel.CurrentLanguage.ToString().ToLowerInvariant(); } catch { }
                        if (lang.Contains("kr") || lang.Contains("ko"))
                            egoTitle = "E.G.O. 책장 선택";
                        else if (lang.Contains("en"))
                            egoTitle = "Select an E.G.O. page";
                        else
                            egoTitle = "选择 E.G.O. 战斗书页";
                    }
                    textMeshProUgui1.text = egoTitle;
                    textMeshProUgui2.text = egoTitle;
                }
                if (RewardingModel.rewardFlag == RewardingModel.RewardFlag.PassiveReward)
                {
                    textMeshProUgui1.text = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("BattleEnd_PassiveReward");
                    textMeshProUgui2.text = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("BattleEnd_PassiveReward");
                    if (self.candidates != null && self.candidates.Length > 0 && self.candidates[0].Card != null)
                    {
                        var candidateCard = self.candidates[0].Card;
                        var passiveInfo = Singleton<RewardPassivesList>.Instance.GetPassiveInfo(new LorId(LogLikeMod.GetPickUpXmlWorkShopId_Passive(candidateCard), candidateCard.id));
                        if (passiveInfo != null)
                        {
                            if (!Singleton<TutorialManager>.Instance.IsSeeTuto("tutorial_EquipPage1_1new") && passiveInfo.rewardtype == RewardType.EquipPage)
                                Singleton<TutorialManager>.Instance.LoadTuto("tutorial_EquipPage1_1new");
                            if (!Singleton<TutorialManager>.Instance.IsSeeTuto("tutorial_EmotionPage1_1") && passiveInfo.rewardtype == RewardType.Creature)
                                Singleton<TutorialManager>.Instance.LoadTuto("tutorial_EmotionPage1_1");
                        }
                    }
                }
                if (RewardingModel.rewardFlag != RewardingModel.RewardFlag.NextStageChoose)
                    return;
                textMeshProUgui1.text = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("BattleEnd_NextStage");
                textMeshProUgui2.text = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("BattleEnd_NextStage");
            }
            else if (LogLikeMod.skipPanel != null)
                LogLikeMod.skipPanel.gameObject.SetActive(false);
        }

        /* UNUSED
        public bool RoundEndPhase_ReturnUnit(
          Func<StageController, float, bool> orig,
          StageController self,
          float deltaTime)
        {
            return orig(self, deltaTime);
        }
        */

        /// <summary>
        /// Hook for hijacking EGO page screen to give card rewards instead.
        /// </summary>
        public void StageLibraryFloorModel_OnPickEgoCard(
          Action<StageLibraryFloorModel, EmotionEgoXmlInfo> orig,
          StageLibraryFloorModel self,
          EmotionEgoXmlInfo egoCard)
        {
            if ((LogLikeMod.CheckStage(true) || RMRRealizationManager.InRealizationBattle)
                && RewardingModel.rewardFlag == RewardingModel.RewardFlag.EgoCardReward
                && LogLikeMod.egoSelectionQueue != null
                && LogLikeMod.egoSelectionQueue.Count > 0)
            {
                bool midBattle = RewardingModel.IsMidBattleEgoSelectionActive;
                List<DiceCardXmlInfo> cardlist = new List<DiceCardXmlInfo>();
                foreach (BattleDiceCardUI egoSlot in SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.egoSlotList)
                {
                    if (egoSlot?.CardModel?.XmlData != null)
                        cardlist.Add(egoSlot.CardModel.XmlData);
                }
                LorId pickId = egoCard != null ? egoCard.CardId : LorId.None;
                DiceCardXmlInfo pickCard = null;
                if (pickId != null && pickId != LorId.None)
                {
                    pickCard = ItemXmlDataList.instance.GetCardItem(pickId, true)
                        ?? ItemXmlDataList.instance.GetCardItem(pickId.id, true)
                        ?? ItemXmlDataList.instance.GetCardItem(new LorId(string.Empty, pickId.id), true);
                }
                if (!midBattle)
                    Singleton<GlobalLogueEffectManager>.Instance.OnPickCardReward(cardlist, pickCard);

                // Ownership / atlas (run permanent unlocks).
                if (pickCard != null && pickCard.id != null)
                    LogueBookModels.AddCard(pickCard.id);
                else if (pickId != null && pickId != LorId.None)
                    LogueBookModels.AddCard(pickId);
                RMRAbnormalityUnlockManager.UnlockEgoForCurrentRoute(pickId);
                LogueBookModels.RecordCompendiumEgoPage(pickId);
                try { RewardingModel.NoteMidBattleEgoPicked(pickId); } catch { }

                // Mid-battle: do NOT call vanilla OnPickEgoCard.
                // orig arms egoSelectionPoint and refreshes hand → sp_toggleSprite OOR / SetCardsObject
                // cascade that can push StageController into EndBattle while enemies still live.
                bool floorRegistered = false;
                try { floorRegistered = RMRAbnormalityUnlockManager.RegisterSelectedEgoOnCurrentFloor(pickId); } catch { }
                if (midBattle && pickId != null && pickId != LorId.None)
                {
                    try
                    {
                        SephirahType floor = self != null ? self.Sephirah : SephirahType.Keter;
                        Singleton<SpecialCardListModel>.Instance?.AddCard(pickId, floor);
                    }
                    catch { /* ignore */ }
                    try
                    {
                        if (self?.team != null)
                            self.team.egoSelectionPoint = 0;
                    }
                    catch { /* ignore */ }
                }

                // Grant ONLY the picked id during mid-battle — never re-flood all route EGOs.
                if (midBattle && pickId != null && pickId != LorId.None)
                {
                    try { RMRPrepareRestrictions.GrantEgoIdsToBattleUnits(new[] { pickId }); }
                    catch (Exception ex) { Debug.LogWarning("[RMR] GrantEgoIds after pick: " + ex.Message); }
                }
                if (LogLikeMod.egoSelectionQueue.Count > 0)
                    LogLikeMod.egoSelectionQueue.RemoveAt(0);

                // Mid-battle: leave rewardFlag / level-up UI; do not start post-battle StartPickReward.
                if (midBattle)
                {
                    try { RewardingModel.rewardFlag = RewardingModel.RewardFlag.EmtoionChoose; } catch { }
                    try
                    {
                        LevelUpUI levelup = SingletonBehavior<BattleManagerUI>.Instance?.ui_levelup;
                        if (levelup != null)
                            LogLikeRoutines.HideRewardSelectionImmediately(levelup);
                    }
                    catch { /* ignore */ }
                    // Field-only hand state; keep root hidden until ApplyLibrarianCardPhase.
                    try { RMRPrepareRestrictions.ForceHandUiToBattleCards(); } catch { }
                    try { RMRPrepareRestrictions.HideHandUiUntilCombat(); } catch { }
                    int enemyAlive = 0;
                    try
                    {
                        enemyAlive = BattleObjectManager.instance?.GetAliveListWithAvailable(Faction.Enemy)?.Count ?? 0;
                    }
                    catch { enemyAlive = -1; }
                    Debug.Log($"[RMR] Mid-battle EGO picked id={pickId?.packageId}:{pickId?.id} floorReg={floorRegistered} enemiesAlive={enemyAlive} (resume combat, no vanilla OnPickEgoCard).");
                }
                else
                {
                    // Post-battle EGO: close UI and advance remaining reward queue.
                    try
                    {
                        LevelUpUI levelup = SingletonBehavior<BattleManagerUI>.Instance?.ui_levelup;
                        if (levelup != null)
                            levelup.SetRootCanvas(false);
                    }
                    catch { /* ignore */ }
                    try { RewardingModel.StartPickReward(); }
                    catch (Exception ex) { Debug.LogWarning("[RMR] StartPickReward after post-battle EGO: " + ex.Message); }
                }
            }
            else
                orig(self, egoCard);
        }

        /// <summary>
        /// Hook for handling next stage choice and passive reward giving.
        /// </summary>
        public void StageLibraryFloorModel_OnPickPassiveCard(
          Action<StageLibraryFloorModel, EmotionCardXmlInfo, BattleUnitModel> orig,
          StageLibraryFloorModel self,
          EmotionCardXmlInfo card,
          BattleUnitModel target = null)
        {
            var stage = Singleton<StagesXmlList>.Instance.GetStageInfo(new LorId(LogLikeMod.GetPickUpXmlWorkShopId_Stage(card), card.id));
            if (RewardingModel.rewardFlag == RewardingModel.RewardFlag.NextStageChoose && stage != null)
            {
                if (stage.type == StageType.Creature)
                {
                    Debug.Log($"[RMR AbnoRoute] Creature stage selected. placeholder={stage.Id}, chapter={LogLikeMod.curchaptergrade}");
                    StageClassInfo abnormalityStage = RMRAbnormalityBattleRouter.PickStageForChapter(LogLikeMod.curchaptergrade);
                    if (abnormalityStage != null)
                    {
                        Debug.Log($"[RMR AbnoRoute] Routed to vanilla abnormality stage: {abnormalityStage.id}");
                        LogLikeMod.SetNextStage(abnormalityStage.id, StageType.Creature);
                        LogueBookModels.RemoveStageInlist(stage.Id, LogLikeMod.curchaptergrade);
                        return;
                    }
                    else
                    {
                        // No usable vanilla abnormality stage found.
                        // DO NOT fall through to the placeholder 99110x stage (which contains normal enemies).
                        // Instead, remove the placeholder and regenerate the next list.
                        Debug.LogError($"[RMR AbnoRoute] No vanilla abnormality stage for chapter {LogLikeMod.curchaptergrade}. Removing placeholder {stage.Id} from all chapters to prevent normal-battle routing.");
                        LogueBookModels.RemoveStageInlist(stage.Id);
                        LogLikeMod.nextlist = LogueBookModels.GetNextList(LogLikeMod.curchaptergrade);
                        // Pick a safe non-Creature fallback from the remain list so the game doesn't get stuck.
                        List<LogueStageInfo> remainList = LogueBookModels.RemainStageList[LogLikeMod.curchaptergrade];
                        LogueStageInfo fallbackStage = remainList.Find(s => s.type != StageType.Creature);
                        if (fallbackStage != null)
                        {
                            Debug.Log($"[RMR AbnoRoute] Fallback to non-Creature stage: {fallbackStage.Id} type={fallbackStage.type}");
                            LogLikeMod.SetNextStage(fallbackStage.Id, fallbackStage.type);
                            LogueBookModels.RemoveStageInlist(fallbackStage.Id, LogLikeMod.curchaptergrade);
                        }
                        else
                        {
                            Debug.LogError($"[RMR AbnoRoute] CRITICAL: No fallback stage available for chapter {LogLikeMod.curchaptergrade}!");
                        }
                        return;
                    }
                }
                LogLikeMod.SetNextStage(stage.Id, stage.type);
                RMRCore.ApplyBinahRedMistProgressionState();
            } else if (RewardingModel.rewardFlag == RewardingModel.RewardFlag.NextStageChoose && stage == null)
            {
                Debug.Log("NULL STAGE ERROR!!: " + card.Name + " --- " + card.id + " --- Generated PID: " + LogLikeMod.GetPickUpXmlWorkShopId_Stage(card) ?? "NULL");
            }
            else if (Singleton<RewardPassivesList>.Instance.GetPassiveInfo(new LorId(LogLikeMod.GetPickUpXmlWorkShopId_Passive(card), card.id)) != null)
            {
                RewardPassiveInfo pickedRewardInfo = Singleton<RewardPassivesList>.Instance.GetPassiveInfo(new LorId(LogLikeMod.GetPickUpXmlWorkShopId_Passive(card), card.id));
                if (pickedRewardInfo == null)
                {
                    Debug.LogWarning("[StageLibraryFloorModel_OnPickPassiveCard] pickedRewardInfo is null, cannot process reward.");
                    return;
                }
                if (pickedRewardInfo.rewardtype == RewardType.Creature && RewardingModel.rewardFlag == RewardingModel.RewardFlag.PassiveReward)
                {
                    RMRAbnormalityUnlockManager.OnEmotionPagePicked(card);
                    LogLikeMod.rewards_passive.RemoveAt(0);
                    return;
                }
                if (RewardingModel.rewardFlag == RewardingModel.RewardFlag.EmtoionChoose)
                {
                    RMRAbnormalityUnlockManager.OnEmotionPagePicked(card);
                    if (RMRAbnormalityUnlockManager.IsNoAbnormalityFallback(card))
                        return;
                }
                if (card.Script == null || card.Script.Count == 0)
                {
                    Debug.LogError("[StageLibraryFloorModel_OnPickPassiveCard] card.Script is null or empty, cannot resolve PickUp.");
                    return;
                }
                PickUpModelBase pickUp = LogLikeMod.FindPickUp(card.Script[0]);
                if (pickUp != null)
                {
                    pickUp.rewardinfo = pickedRewardInfo;
                    pickUp.id = new LorId(LogLikeMod.GetPickUpXmlWorkShopId_Passive(card), card.id);
                    if (card.TargetType == EmotionTargetType.All || card.TargetType == EmotionTargetType.AllIncludingEnemy)
                    {
                        foreach (BattleUnitModel model in BattleObjectManager.instance.GetList(Faction.Player))
                        {
                            pickUp.OnPickUp(model);
                            if (LogueBookModels.playersPick.ContainsKey(model.UnitData.unitData))
                                LogueBookModels.playersPick[model.UnitData.unitData].Add(new LorId(LogLikeMod.GetPickUpXmlWorkShopId_Passive(card), card.id));
                        }
                        if (card.TargetType == EmotionTargetType.AllIncludingEnemy)
                        {
                            foreach (BattleUnitModel model in BattleObjectManager.instance.GetList(Faction.Enemy))
                                pickUp.OnPickUp(model);
                        }
                    }
                    if (target != null)
                    {
                        pickUp.OnPickUp(target);
                        if (LogueBookModels.playersPick.ContainsKey(target.UnitData.unitData))
                            LogueBookModels.playersPick[target.UnitData.unitData].Add(new LorId(LogLikeMod.GetPickUpXmlWorkShopId_Passive(card), card.id));
                    }
                    pickUp.OnPickUp();
                    Singleton<LogueSaveManager>.Instance.AddToObtainCount(pickUp);
                }
                switch (RewardingModel.rewardFlag)
                {
                    case RewardingModel.RewardFlag.PassiveReward:
                        LogLikeMod.rewards_passive.RemoveAt(0);
                        break;
                    case RewardingModel.RewardFlag.RewardInStage:
                        LogLikeMod.rewards_InStage.RemoveAt(0);
                        if (Singleton<ShopManager>.Instance.curshop != null && LogLikeMod.rewards_InStage.Count == 0)
                            Singleton<ShopManager>.Instance.curshop.HideShop();
                        if (Singleton<MysteryManager>.Instance.curMystery == null || !(Singleton<MysteryManager>.Instance.curMystery is MysteryModel_Rest))
                            break;
                        (Singleton<MysteryManager>.Instance.curMystery as MysteryModel_Rest).HideRest();
                        break;
                }
            }
            else
                orig(self, card, target);
        }

        /// <summary>
        /// Hook for initializing inventory, abnormality and crafting screens.
        /// </summary>
        public void UIBattleSettingEditPanel_Open(
          Action<UIBattleSettingEditPanel, UIBattleSettingEditTap> orig,
          UIBattleSettingEditPanel self,
          UIBattleSettingEditTap state)
        {
            if (LogLikeRoutines.IsRoguelikeBattleSettingContext())
            {
                if (LogLikeMod.InvenBtn == null)
                {
                    Button fieldValue = LogLikeMod.GetFieldValue<Button>(self, "button_BattleCard");
                    LogLikeMod.InvenBtn = UnityEngine.Object.Instantiate<Button>(fieldValue, fieldValue.transform.parent);
                    LogLikeMod.InvenBtn.transform.localPosition = fieldValue.transform.localPosition + new Vector3(200f, 0.0f);
                    Button.ButtonClickedEvent buttonClickedEvent = new Button.ButtonClickedEvent();
                    buttonClickedEvent.AddListener((UnityAction)(() => LogLikeRoutines.OnClickInventory(self)));
                    LogLikeMod.InvenBtn.onClick = buttonClickedEvent;
                    LogLikeRoutines.SafeGetButtonComponents(LogLikeMod.InvenBtn, out UITextDataLoader component, out Image invenFrame);
                    if (component != null)
                    {
                        component.key = "ui_Inventory";
                        component.SetText();
                    }
                    LogLikeMod.InvenBtnFrame = invenFrame;
                    if (LogLikeMod.InvenBtnFrame != null)
                        LogLikeMod.InvenBtnFrame.enabled = false;
                }
                // Atlas tab removed from battle prepare — open from RMR start hub instead.
                // Keep CreatureBtn null/hidden; craft sits at former atlas slot (+400).
                if (LogLikeMod.CraftBtn == null)
                {
                    Button fieldValue = LogLikeMod.GetFieldValue<Button>(self, "button_BattleCard");
                    LogLikeMod.CraftBtn = UnityEngine.Object.Instantiate<Button>(fieldValue, fieldValue.transform.parent);
                    LogLikeMod.CraftBtn.transform.localPosition = fieldValue.transform.localPosition + new Vector3(400f, 0.0f);
                    Button.ButtonClickedEvent buttonClickedEvent = new Button.ButtonClickedEvent();
                    buttonClickedEvent.AddListener((UnityAction)(() => LogLikeRoutines.OnClickCraftTab(self)));
                    LogLikeMod.CraftBtn.onClick = buttonClickedEvent;
                    LogLikeRoutines.SafeGetButtonComponents(LogLikeMod.CraftBtn, out UITextDataLoader component, out Image craftFrame);
                    if (component != null)
                    {
                        component.key = "ui_CraftTab";
                        component.SetText();
                    }
                    LogLikeMod.CraftBtnFrame = craftFrame;
                    if (LogLikeMod.CraftBtnFrame != null)
                        LogLikeMod.CraftBtnFrame.enabled = false;
                }
                if (LogLikeMod.RealizationBtn == null)
                {
                    Button fieldValue = LogLikeMod.GetFieldValue<Button>(self, "button_BattleCard");
                    LogLikeMod.RealizationBtn = UnityEngine.Object.Instantiate<Button>(fieldValue, fieldValue.transform.parent);
                    LogLikeMod.RealizationBtn.transform.localPosition = fieldValue.transform.localPosition + new Vector3(800f, 0.0f);
                    Button.ButtonClickedEvent btnEvent = new Button.ButtonClickedEvent();
                    btnEvent.AddListener((UnityAction)(() => LogLikeRoutines.OnClickRealization(self)));
                    LogLikeMod.RealizationBtn.onClick = btnEvent;
                    LogLikeRoutines.SafeGetButtonComponents(LogLikeMod.RealizationBtn, out UITextDataLoader txtLoader, out Image realizationFrame);
                    if (txtLoader != null)
                    {
                        txtLoader.key = "ui_Realization";
                        txtLoader.SetText();
                    }
                    LogLikeRoutines.ApplyRealizationButtonText(LogLikeMod.RealizationBtn);
                    LogLikeMod.RealizationBtnFrame = realizationFrame;
                    if (LogLikeMod.RealizationBtnFrame != null)
                        LogLikeMod.RealizationBtnFrame.enabled = false;
                }
                LogLikeMod.InvenBtn.gameObject.SetActive(true);
                if (LogLikeMod.CreatureBtn != null)
                    LogLikeMod.CreatureBtn.gameObject.SetActive(false);
                if (LogLikeMod.CraftBtn != null)
                    LogLikeMod.CraftBtn.gameObject.SetActive(true);
                if (LogLikeMod.CompendiumBtn != null)
                    LogLikeMod.CompendiumBtn.gameObject.SetActive(false);
                LogLikeRoutines.ApplyRealizationButtonText(LogLikeMod.RealizationBtn);
                // Realization entry is only on the start hub — keep prepare-panel button hidden.
                if (LogLikeMod.RealizationBtn != null)
                    LogLikeMod.RealizationBtn.gameObject.SetActive(false);
                Singleton<GlobalLogueInventoryPanel>.Instance.SetActive(false);
                Singleton<LogCreatureTabPanel>.Instance.SetActive(false);
                Singleton<LogCraftPanel>.Instance.SetActive(false);
                try { Singleton<LogCompendiumPanel>.Instance.SetActive(false); } catch { }
                Image image = (Image)typeof(UIBattleSettingEditPanel).GetField("img_BlockBackGroundBg", AccessTools.all).GetValue(self);
                self.SetBUttonState(state);
                // Dimmer stays behind inventory content; postfix RaiseUiPanelCanvas lifts key/combat lists.
                // raycastTarget=true on a full-screen dimmer above books was blocking/obscuring slots.
                if (image != null)
                {
                    try { image.transform.SetAsFirstSibling(); } catch { }
                    image.raycastTarget = false;
                }
                self.SetActivePanel(true);
                // Z-order repair for key/combat inventory runs in
                // LogLikePatches.UIBattleSettingEditPanel_SetBUttonState_ZOrder (Harmony postfix).
            }
            else
            {
                if (LogLikeMod.InvenBtn != null)
                    LogLikeMod.InvenBtn.gameObject.SetActive(false);
                if (LogLikeMod.CreatureBtn != null)
                    LogLikeMod.CreatureBtn.gameObject.SetActive(false);
                if (LogLikeMod.CraftBtn != null)
                    LogLikeMod.CraftBtn.gameObject.SetActive(false);
                if (LogLikeMod.CompendiumBtn != null)
                    LogLikeMod.CompendiumBtn.gameObject.SetActive(false);
                if (LogLikeMod.RealizationBtn != null)
                    LogLikeMod.RealizationBtn.gameObject.SetActive(false);
                orig(self, state);
            }
        }

        /// <summary>
        /// Unlock BattleSetting combat-page / key-page slots for RMR prepare.
        /// Vanilla locks them after a floor act (IsUsedSephirah) and greys the slots.
        /// </summary>
        public void UIBattleSettingLibrarianInfoPanel_SetBattleCardSlotState(
          Action<UIBattleSettingLibrarianInfoPanel> orig,
          UIBattleSettingLibrarianInfoPanel self)
        {
            orig(self);
            LogLikeRoutines.ForceUnlockBattleSettingLoadoutSlots(self);
        }

        /// <summary>
        /// Unlock BattleSetting key-page slot for RMR prepare (see SetBattleCardSlotState).
        /// </summary>
        public void UIBattleSettingLibrarianInfoPanel_SetEquipPageSlotState(
          Action<UIBattleSettingLibrarianInfoPanel> orig,
          UIBattleSettingLibrarianInfoPanel self)
        {
            orig(self);
            LogLikeRoutines.ForceUnlockBattleSettingLoadoutSlots(self);
        }

        /* NOT IMPLEMENTED
        public void BattleDiceCardUI_ShowDetail(Action<BattleDiceCardUI> orig, BattleDiceCardUI self)
        {
            orig(self);
            if (!LogLikeMod.CheckStage(true))
                return;
            self.KeywordListUI.Activate();
        }
        */

        /// <summary>
        /// Hijacks and adjusts the ego card popup UI for miscellaneous purposes.
        /// </summary>
        public void BattleDiceCardUI_SetEgoCardForPopup(
          Action<BattleDiceCardUI, EmotionEgoXmlInfo> orig,
          BattleDiceCardUI self,
          EmotionEgoXmlInfo egoxmlinfo)
        {
            orig(self, egoxmlinfo);
            if (!LogLikeMod.CheckStage(true))
                return;
            foreach (GameObject gameObject in (GameObject[])typeof(BattleDiceCardUI).GetField("ob_NormalFrames", AccessTools.all).GetValue(self))
                gameObject.SetActive(true);
            foreach (GameObject gameObject in (GameObject[])typeof(BattleDiceCardUI).GetField("ob_EgoFrames", AccessTools.all).GetValue(self))
                gameObject.SetActive(false);
            DiceCardXmlInfo cardItem = ItemXmlDataList.instance.GetCardItem(egoxmlinfo.CardId);
            FieldInfo field1 = typeof(BattleDiceCardUI).GetField("colorFrame", AccessTools.all);
            FieldInfo field2 = typeof(BattleDiceCardUI).GetField("colorLineardodge", AccessTools.all);
            field1.SetValue(self, UIColorManager.Manager.GetCardRarityColor(cardItem.Rarity));
            field2.SetValue(self, UIColorManager.Manager.GetCardRarityLinearColor(cardItem.Rarity));
            typeof(BattleDiceCardUI).GetMethod("SetRangeIconHsv", AccessTools.all).Invoke(self, new object[1]
            {
       UIColorManager.Manager.CardRangeHsvValue[(int) cardItem.Rarity]
            });
            typeof(BattleDiceCardUI).GetMethod("SetFrameColor", AccessTools.all).Invoke(self, new object[1]
            {
      field1.GetValue( self)
            });
            self.SetLinearDodgeColor(true);
        }

        /// <summary>
        /// Sets card states for UI purposes.<br></br>
        /// Not entirely sure if this is necessary. Won't bother touching it though. -CalmMagma
        /// </summary>
        public void BattleUnitCardsInHandUI_SetCardsObject(
          Action<BattleUnitCardsInHandUI, BattleUnitModel, bool> orig,
          BattleUnitCardsInHandUI self,
          BattleUnitModel unitModel,
          bool isClicked = true)
        {
            // Vanilla SetCardsObject NREs in RMR receptions (hover unit / empty formation).
            // Keep Magma-style safe path for RMR, but still surface EGO toggle when personalEgo
            // or floor-selected EGO exists (after mid-battle EGO picks).
            // Default hand is ALWAYS battle pages when selecting a unit (isClicked): force toggle
            // off so flooded EGO / sticky toggle cannot trap the player on unplayable EGO-only hand.
            // NOTE: realization battles suppress CheckStage, but vanilla Floor Realization floods
            // personalEgo with floor EGO — the safe path MUST also run there or the hand
            // defaults to EGO pages instead of normal combat pages.
            if (!LogLikeMod.CheckStage(true) && !RMRRealizationManager.InRealizationBattle)
            {
                orig(self, unitModel, isClicked);
                return;
            }
            if (self == null || unitModel == null)
                return;
            try
            {
                GameObject gameObject = (GameObject)BattleUnitCardsInHandUIRootObjField.GetValue(self);
                Toggle toggle = (Toggle)BattleUnitCardsInHandUIToggleShowEgoField.GetValue(self);

                // Before card-select phase (intro "开始战斗" / idle enemies), never open the hand root.
                // Hover/select during StartBattle was re-activating a single floating combat page mid-screen.
                if (!RMRPrepareRestrictions.IsHandUiPhaseAllowed())
                {
                    BattleUnitCardsInHandUIIsOverOnEgoToggleField.SetValue(self, false);
                    try
                    {
                        if (toggle != null)
                        {
                            try { toggle.SetIsOnWithoutNotify(false); } catch { try { toggle.isOn = false; } catch { } }
                        }
                    }
                    catch { /* ignore */ }
                    try
                    {
                        BattleUnitCardsInHandUIHandStateField.SetValue(self, BattleUnitCardsInHandUI.HandState.BattleCard);
                    }
                    catch { /* ignore */ }
                    if (gameObject != null)
                        gameObject.SetActive(false);
                    return;
                }

                if (gameObject != null)
                    gameObject.SetActive(true);
                BattleUnitCardsInHandUIIsOverOnEgoToggleField.SetValue(self, false);
                if (isClicked)
                    BattleUnitCardsInHandUISelectedUnitField.SetValue(self, unitModel);
                else
                    BattleUnitCardsInHandUIHoveredUnitField.SetValue(self, unitModel);

                bool hasEgo = false;
                try
                {
                    hasEgo = unitModel.personalEgoDetail != null && unitModel.personalEgoDetail.ExistsCard();
                }
                catch { hasEgo = false; }
                try
                {
                    if (!hasEgo && Singleton<SpecialCardListModel>.Instance != null)
                        hasEgo = Singleton<SpecialCardListModel>.Instance.ExistEgoCardBySelected();
                }
                catch { /* ignore */ }

                // Selecting a unit → prefer combat pages. Player can still flip the purple EGO toggle.
                if (isClicked && toggle != null)
                {
                    try { toggle.isOn = false; } catch { /* ignore */ }
                }
                try
                {
                    BattleUnitCardsInHandUIHandStateField.SetValue(self, BattleUnitCardsInHandUI.HandState.BattleCard);
                }
                catch { /* ignore */ }

                BattleUnitCardsInHandUI.EgoToggleState egoToggleState = BattleUnitCardsInHandUI.EgoToggleState.Hide;
                if (hasEgo && toggle != null)
                {
                    // After force-off on click, Off; on hover preserve toggle if user left it on.
                    egoToggleState = toggle.isOn
                        ? BattleUnitCardsInHandUI.EgoToggleState.On
                        : BattleUnitCardsInHandUI.EgoToggleState.Off;
                    if (toggle.isOn)
                    {
                        try
                        {
                            BattleUnitCardsInHandUIHandStateField.SetValue(self, BattleUnitCardsInHandUI.HandState.EgoCard);
                        }
                        catch { /* ignore */ }
                    }
                }

                try
                {
                    if (!PlatformManager.Instance.AchievementUnlocked(AchievementEnum.ONCE_COPY)
                        && unitModel.allyCardDetail != null
                        && unitModel.allyCardDetail.Exsist6CardsInHand_andCopy())
                        PlatformManager.Instance.UnlockAchievement(AchievementEnum.ONCE_COPY);
                }
                catch { /* ignore */ }

                ApplyHandEgoToggleSafe(self, egoToggleState);
            }
            catch (Exception ex)
            {
                // Never let hand-UI setup abort the reception.
                Debug.LogWarning("[RMR] SetCardsObject safe path failed: " +
                    (ex.InnerException != null ? ex.InnerException.Message : ex.Message));
                try
                {
                    BattleUnitCardsInHandUIHandStateField?.SetValue(self, BattleUnitCardsInHandUI.HandState.BattleCard);
                }
                catch { /* ignore */ }
            }
        }

        /// <summary>
        /// Hook for de-equipping combat pages from the deck.
        /// </summary>
        public bool DeckModel_MoveCardToInventory(
          Func<DeckModel, LorId, bool> orig,
          DeckModel self,
          LorId cardId)
        {
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return orig(self, cardId);
            if (!((List<DiceCardXmlInfo>)typeof(DeckModel).GetField("_deck", AccessTools.all).GetValue(self)).Remove(ItemXmlDataList.instance.GetCardItem(cardId)))
                return false;
            LogueBookModels.AddCard(cardId, 1, false);
            return true;
        }

        /// <summary>
        /// Hook for equipping combat pages from the inventory.
        /// </summary>
        public CardEquipState DeckModel_AddCardFromInventory(
          Func<DeckModel, LorId, CardEquipState> orig,
          DeckModel self,
          LorId cardId)
        {
            CardEquipState cardEquipState;
            if (LogLikeRoutines.IsRoguelikeBattleSettingContext())
            {
                List<DiceCardXmlInfo> diceCardXmlInfoList = (List<DiceCardXmlInfo>)typeof(DeckModel).GetField("_deck", AccessTools.all).GetValue(self);
                if (diceCardXmlInfoList.Count >= 9)
                {
                    cardEquipState = CardEquipState.FullOfDeck;
                }
                else
                {
                    DiceCardXmlInfo card = ItemXmlDataList.instance.GetCardItem(cardId);
                    DiceCardSelfAbilityBase diceCardSelfAbility = Singleton<AssemblyManager>.Instance.CreateInstance_DiceCardSelfAbility(card.Script);
                    CardEquipState state;
                    if (diceCardSelfAbility != null && diceCardSelfAbility is LogDiceCardSelfAbility && !(diceCardSelfAbility as LogDiceCardSelfAbility).CanAddDeck(self, out state))
                        return state;
                    if (diceCardXmlInfoList.FindAll(x => x.id.GetOriginalId() == card.id.GetOriginalId()).Count >= card.Limit)
                        cardEquipState = CardEquipState.OverCardLimit;
                    else if (!LogueBookModels.RemoveCard(card.id))
                    {
                        cardEquipState = CardEquipState.LackOfCards;
                    }
                    else
                    {
                        Singleton<LibraryQuestManager>.Instance.OnEditCard();
                        diceCardXmlInfoList.Add(card);
                        cardEquipState = CardEquipState.Equippable;
                    }
                }
            }
            else
                cardEquipState = orig(self, cardId);
            return cardEquipState;
        }

        /// <summary>
        /// Hook for overriding combat page list with the roguelike's.
        /// </summary>
        public void UIInvenCardListScroll_SetData(
          Action<UIInvenCardListScroll, List<DiceCardItemModel>, UnitDataModel> orig,
          UIInvenCardListScroll self,
          List<DiceCardItemModel> cards,
          UnitDataModel unitData)
        {
            if (LogLikeRoutines.IsRoguelikeBattleSettingContext())
            {
                cards = LogueBookModels.GetCardListForInven();
                RMRPrepareRestrictions.NotifyInventoryEmptyIfNeeded(isBookInventory: false, cards?.Count ?? 0);
            }
            orig(self, cards, unitData);
        }

        /// <summary>
        /// Hook for equipping a combat page into an unit-<br></br>
        /// SPECIFICALLY, defers control of AddCardFromInventory to BookModel.
        /// </summary>
        public CardEquipState UnitDataModel_AddCardFromInventory(
          Func<UnitDataModel, LorId, CardEquipState> orig,
          UnitDataModel self,
          LorId cardId)
        {
            // A card id that resolves to nothing makes vanilla AddCardFromInventory dereference null.
            // Continue-load restores each librarian's deck through here from the Invitation screen,
            // where IsRoguelikeBattleSettingContext() is false, so the early-return below used to
            // hand the bad id straight to vanilla and the NRE aborted the entire save load
            // ("继续失败：存档未能完整加载"). One stale card must cost one card, not the save.
            try
            {
                if (cardId == null || ItemXmlDataList.instance.GetCardItem(cardId) == null)
                {
                    Debug.LogWarning($"[RMR] AddCardFromInventory: unresolvable card {cardId} skipped (deck entry dropped).");
                    return CardEquipState.ERROR;
                }
            }
            catch { /* resolution itself failed; fall through to the existing paths */ }

            // Restoring a save: vanilla checks the per-floor carry limit via
            //   LibraryModel.Instance.GetFloor(OwnerSephirah).GetEquipedUnitList_partial(cardId).Count
            // and the library floors do not exist yet when Continue runs from the Invitation screen,
            // so GetFloor returns null and every librarian's deck failed to restore on the first
            // attempt (a second Continue worked because the library was built by then).
            // The saved deck was already legal when it was written, so equip directly and skip a
            // check that cannot be evaluated at this point anyway.
            try
            {
                if (LogLikeMod.saveloading && self != null && self.bookItem != null)
                    return self.bookItem.AddCardFromInventoryToCurrentDeck(cardId);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR] AddCardFromInventory during save load failed: " + ex.Message);
                return CardEquipState.ERROR;
            }

            // Must stay false during vanilla library LoadPlayDataFromSaveFile / EquipBook.
            // Wrong true here re-enters BookModel hook and can crash the whole save load.
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
            {
                // Same null the FloorLimit path trips over, just outside a save load.
                if (self == null || self.bookItem == null || LibraryModel.Instance == null
                    || LibraryModel.Instance.GetFloor(self.OwnerSephirah) == null)
                {
                    Debug.LogWarning($"[RMR] AddCardFromInventory: library not ready for {cardId}; equipping without the floor-limit check.");
                    try
                    {
                        return self?.bookItem != null
                            ? self.bookItem.AddCardFromInventoryToCurrentDeck(cardId)
                            : CardEquipState.ERROR;
                    }
                    catch { return CardEquipState.ERROR; }
                }
                return orig(self, cardId);
            }
            try
            {
                if (self == null || self.bookItem == null)
                    return orig(self, cardId);
                return ItemXmlDataList.instance.GetCardItem(cardId) == null
                    ? CardEquipState.ERROR
                    : self.bookItem.AddCardFromInventoryToCurrentDeck(cardId);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR] UnitDataModel_AddCardFromInventory fallback to vanilla: " + ex.Message);
                return orig(self, cardId);
            }
        }

        /// <summary>
        /// Hook for equipping a combat page into an unit.
        /// </summary>
        public CardEquipState BookModel_AddCardFromInventory(
            Func<BookModel, LorId, CardEquipState> orig, 
            BookModel self, 
            LorId cardId)
        {
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return orig(self, cardId);
            try
            {
                bool editableBlue = LogueBookModels.IsEditableBlueReverberationDeck(self);
                if (self.IsFixedDeck() && !editableBlue)
                    return CardEquipState.ERROR;
                if (self.IsLockByBluePrimary() && !editableBlue)
                    return CardEquipState.ERROR;
                DiceCardXmlInfo cardXmlInfo = RewardingModel.GetCardItemOriginAware(cardId)
                    ?? ItemXmlDataList.instance.GetCardItem(cardId, false);
                if (cardXmlInfo == null)
                    return CardEquipState.ERROR;
                // EGO pages are not combat-deck cards (vanilla: personalEgo / emotion EGO UI only).
                if (RMRPrepareRestrictions.IsEgoCombatPage(cardXmlInfo)
                    || !RMRPrepareRestrictions.IsCardAllowedInCurrentPrepare(cardXmlInfo))
                    return CardEquipState.ERROR;
                if (cardXmlInfo.optionList.Contains(CardOption.OnlyPage))
                {
                    if (!self.GetOnlyCards().Exists((DiceCardXmlInfo x) => x.id.GetOriginalId() == cardXmlInfo.id.GetOriginalId()))
                        return CardEquipState.OnlyPageLimit;
                }
                else if (self.ClassInfo.RangeType == EquipRangeType.Melee)
                {
                    if (cardXmlInfo.Spec.Ranged == CardRange.Far)
                        return CardEquipState.FarTypeLimit;
                }
                else if (self.ClassInfo.RangeType == EquipRangeType.Range && cardXmlInfo.Spec.Ranged == CardRange.Near)
                {
                    return CardEquipState.NearTypeLimit;
                }

                // NEVER use self._deck — MonoMod DynamicMethod cannot access private BookModel._deck
                // (FieldAccessException) and aborts library/continue load (ReEquipDeck).
                DeckModel deck = LogLikeMod.GetFieldValue<DeckModel>(self, "_deck");
                if (deck == null)
                {
                    Debug.LogWarning("[RMR] BookModel_AddCardFromInventory: _deck null, vanilla fallback.");
                    return orig(self, cardId);
                }
                return deck.AddCardFromInventory(cardId);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR] BookModel_AddCardFromInventory fallback to vanilla: " + ex.Message);
                return orig(self, cardId);
            }
        }

        /// <summary>
        /// Hook for overriding keypage list with Roguelike's.
        /// </summary>
        public List<BookModel> BookInventoryModel_GetBookList_equip(
          Func<BookInventoryModel, List<BookModel>> orig,
          BookInventoryModel self)
        {
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return orig(self);
            List<BookModel> books = RMRPrepareRestrictions.FilterEquipInventoryBooks(LogueBookModels.booklist);
            RMRPrepareRestrictions.NotifyInventoryEmptyIfNeeded(isBookInventory: true, books?.Count ?? 0);
            return books;
        }

        /* DEPRECATED, MOVED TO RMRCORE.BOOKSTOADDTOINVENTORY
        public List<LorId> DropBookInventoryModel_GetBookList_invitationBookList(
          Func<DropBookInventoryModel, List<LorId>> orig,
          DropBookInventoryModel self)
        {
            List<LorId> invitationBookList = orig(self);

            if (LoguePlayDataSaver.CheckPlayerData())
                invitationBookList.Add(new LorId(LogLikeMod.ModId, -855));
            invitationBookList.Add(new LorId(LogLikeMod.ModId, -2854));
            invitationBookList.Add(new LorId(LogLikeMod.ModId, -3854));
            invitationBookList.Add(new LorId(LogLikeMod.ModId, -4854));
            invitationBookList.Add(new LorId(LogLikeMod.ModId, -5854));
            return invitationBookList;
        }
        */

        /// <summary>
        /// Hook to disable the card equip info button.
        /// </summary>
        public void UIInvenCardSlot_OnClickCardEquipInfoButton(
          Action<UIInvenCardSlot> orig,
          UIInvenCardSlot self)
        {
            if (LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return;
            orig(self);
        }

        /// <summary>
        /// Hook to make exclusive combat pages show up properly in roguelike's card inventory
        /// </summary>
        public void UIInvenCardScrollList_ApplyFilterAll(Action<UIInvenCardListScroll> orig, UIInvenCardListScroll self)
        {
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
            {
                orig(self);
                return;
            }
            self._currentCardListForFilter.Clear();
            List<DiceCardItemModel> cardsByDetailFilterUI = self.GetCardsByDetailFilterUI(self.GetCardBySearchFilterUI(self.GetCardsByCostFilterUI(self.GetCardsByGradeFilterUI(self._originCardList))));
            cardsByDetailFilterUI.Sort(new Comparison<DiceCardItemModel>(SortUtil.CardItemCompByCost));
            float y;
            if (self._unitdata != null)
            {
                Predicate<DiceCardItemModel> cond1 = (DiceCardItemModel x) => true;
                Predicate<DiceCardItemModel> cond2 = (DiceCardItemModel x) => true;
                switch (self._unitdata.bookItem.ClassInfo.RangeType)
                {
                    case EquipRangeType.Melee:
                        cond1 = ((DiceCardItemModel x) => x.GetSpec().Ranged == CardRange.Near);
                        break;
                    case EquipRangeType.Range:
                        cond1 = ((DiceCardItemModel x) => x.GetSpec().Ranged == CardRange.Far);
                        break;
                    case EquipRangeType.Hybrid:
                        cond1 = (DiceCardItemModel x) => true;
                        break;
                }
                List<DiceCardXmlInfo> onlyCards = self._unitdata.bookItem.GetOnlyCards();
                cond2 = (DiceCardItemModel x) => onlyCards.Exists((DiceCardXmlInfo z) => z.id.GetOriginalId() == x.GetID().GetOriginalId());
                foreach (DiceCardItemModel item in cardsByDetailFilterUI.FindAll((DiceCardItemModel x) => x.ClassInfo.optionList.Contains(CardOption.OnlyPage) && !cond2(x)))
                {
                    cardsByDetailFilterUI.Remove(item);
                }
                self._currentCardListForFilter.AddRange(cardsByDetailFilterUI.FindAll(delegate (DiceCardItemModel x)
                {
                    if (!x.ClassInfo.optionList.Contains(CardOption.OnlyPage))
                    {
                        return cond1(x);
                    }
                    return cond2(x);
                }));
                self._currentCardListForFilter.AddRange(cardsByDetailFilterUI.FindAll((DiceCardItemModel x) => !(x.ClassInfo.optionList.Contains(CardOption.OnlyPage) ? cond2(x) : cond1(x))));
            }
            else
            {
                self._currentCardListForFilter.AddRange(cardsByDetailFilterUI);
            }
            float x2 = (float)self.column * self.slotWidth;
            y = (float)(self.GetMaxRow() + self.row - 1) * self.slotHeight;
            self.scrollBar.SetScrollRectSize(x2, y);
            self.scrollBar.SetWindowPosition(0f, 0f);
            if (self.slotList != null && self.slotList.Count > 0)
                self.selectablePanel.ChildSelectable = self.slotList[0].selectable;
            self.SetCardsData(self.GetCurrentPageList());
            // A3: after cost/search filters, still empty → notify floor/EGO limits.
            if (self._currentCardListForFilter == null || self._currentCardListForFilter.Count == 0)
                RMRPrepareRestrictions.NotifyInventoryEmptyIfNeeded(false, 0);
        }

        /// <summary>
        /// Hook to manipulate card slot states to refer to the roguelike inventory.
        /// </summary>
        public void UIInvenCardSlot_SetSlotState(Action<UIInvenCardSlot> orig, UIInvenCardSlot self)
        {
            if (LogLikeRoutines.IsRoguelikeBattleSettingContext())
            {
                TextMeshProUGUI textMeshProUgui = (TextMeshProUGUI)typeof(UIInvenCardSlot).GetField("txt_deckLimit", AccessTools.all).GetValue(self);
                GameObject gameObject = (GameObject)typeof(UIInvenCardSlot).GetField("deckLimitRoot", AccessTools.all).GetValue(self);
                DiceCardItemModel _cardModel = (DiceCardItemModel)typeof(UIOriginCardSlot).GetField("_cardModel", AccessTools.all).GetValue(self);
                FieldInfo field = typeof(UIInvenCardSlot).GetField("slotState", AccessTools.all);
                field.SetValue(self, UIINVENCARD_STATE.None);
                if (_cardModel.num <= 0)
                    field.SetValue(self, UIINVENCARD_STATE.NumberZero);
                if (UI.UIController.Instance.CurrentUnit.GetDeckAll().FindAll(x => x.id.GetOriginalId() == _cardModel.GetID().GetOriginalId()).Count >= _cardModel.GetLimit())
                    field.SetValue(self, UIINVENCARD_STATE.LimitedDeck);
                UnitDataModel currentUnit = UI.UIController.Instance.CurrentUnit;
                if (currentUnit != null)
                {
                    BookModel bookItem = currentUnit.bookItem;
                    List<DiceCardXmlInfo> onlyCards = bookItem.GetOnlyCards();
                    if (_cardModel.ClassInfo.optionList.Contains(CardOption.OnlyPage))
                    {
                        if (!onlyCards.Exists(y => y.id.GetOriginalId() == _cardModel.GetID().GetOriginalId()))
                            field.SetValue(self, UIINVENCARD_STATE.OnlyPage);
                    }
                    else if (bookItem.ClassInfo.RangeType == EquipRangeType.Melee && _cardModel.GetSpec().Ranged == CardRange.Far)
                        field.SetValue(self, UIINVENCARD_STATE.RangeCard);
                    else if (bookItem.ClassInfo.RangeType == EquipRangeType.Range && _cardModel.GetSpec().Ranged == CardRange.Near)
                        field.SetValue(self, UIINVENCARD_STATE.MeleeCard);
                }
                gameObject.gameObject.SetActive((UIINVENCARD_STATE)field.GetValue(self) > UIINVENCARD_STATE.None);
                self.SetGrayScale((UIINVENCARD_STATE)field.GetValue(self) > UIINVENCARD_STATE.None);
                switch ((UIINVENCARD_STATE)field.GetValue(self))
                {
                    case UIINVENCARD_STATE.LimitedDeck:
                        textMeshProUgui.text = TextDataModel.GetText("ui_card_equipstate_overcardlimit");
                        break;
                    case UIINVENCARD_STATE.LimitedFloor:
                        textMeshProUgui.text = TextDataModel.GetText("ui_card_equipstate_overfloorlimit");
                        break;
                    case UIINVENCARD_STATE.NumberZero:
                        textMeshProUgui.text = TextDataModel.GetText("ui_card_equipstate_lackofcards");
                        break;
                    case UIINVENCARD_STATE.OnlyPage:
                        textMeshProUgui.text = TextDataModel.GetText("ui_card_equipstate_onlypagelimit");
                        break;
                    case UIINVENCARD_STATE.RangeCard:
                        textMeshProUgui.text = TextDataModel.GetText("ui_card_equipstate_fartypelimit");
                        break;
                    case UIINVENCARD_STATE.MeleeCard:
                        textMeshProUgui.text = TextDataModel.GetText("ui_card_equipstate_neartypelimit");
                        break;
                }
                self.RefreshNumbersData();
            }
            else
                orig(self);
        }

        /// <summary>
        /// Hook to initialize roguelike reception and gamemodes.
        /// </summary>
        public void UIInvitationRightMainPanel_ConfirmSendInvitation(
          Action<UIInvitationRightMainPanel> orig,
          UIInvitationRightMainPanel self)
        {
            StageClassInfo bookRecipe = self.GetBookRecipe();
            if (bookRecipe == null)
            {
                Debug.LogError("[RMR] ConfirmSendInvitation: GetBookRecipe() returned null — abort.");
                try { RMRRealizationManager.RollbackLaunchIntentKeepHub(); } catch { }
                return;
            }

            Singleton<StagesXmlList>.Instance.RestoreToDefault();
            Singleton<RewardPassivesList>.Instance.RestoreToDefault();
            Singleton<MysteryXmlList>.Instance.RestoreToDefault();
            Singleton<CardDropValueList>.Instance.RestoreToDefault();

            RMRCore.CurrentGamemode = null;

            LorId invitation = bookRecipe.id;
            bool succes = false;

            // Continue: accept both RMR packageId and LogLikeMod.ModId (button uses ModId).
            bool isContinueInvitation = invitation != null
                && invitation.id == -855
                && (invitation.packageId == RMRCore.packageId
                    || invitation.packageId == LogLikeMod.ModId
                    || string.IsNullOrEmpty(invitation.packageId));

            if (isContinueInvitation)
            {
                // Wipe any leftover realization-bootstrap flags so continue is pure roguelike.
                try { RMRRealizationManager.EndHubSessionToLibrary(); } catch { }
                RMRCore.ResetPostInvitationLaunchGate();

                bool loaded = RoguelikeGamemodeController.Instance.LoadGamemodeByStageRecipe(
                    new LorId(RMRCore.packageId, -855), true);
                if (!loaded || RMRCore.CurrentGamemode == null)
                {
                    Debug.LogError("[RMR] Continue failed: could not load saved gamemode (Lastest CurrentGamemode missing/invalid).");
                    UIAlarmPopup.instance?.SetAlarmText(
                        TextDataModel.GetText("ui_RMR_ContinueFailed")
                        ?? "Failed to continue Roguelike run. Save may be missing or outdated.");
                    return;
                }

                RMRCore.CurrentGamemode.FilterContent();
                RMRCore.CurrentGamemode.BeforeInitializeGamemode();
                bookRecipe.mapInfo.Clear();
                orig(self);
                bool loadOk = true;
                try
                {
                    LoguePlayDataSaver.LoadPlayData();
                }
                catch (Exception ex)
                {
                    loadOk = false;
                    Debug.LogError("[RMR] Continue LoadPlayData failed: " + ex);
                    try
                    {
                        UIAlarmPopup.instance?.SetAlarmText(
                            TextDataModel.GetText("ui_RMR_ContinueLoadFailed")
                            ?? "Continue failed: save data could not be fully loaded. Try New Run, or re-enter after a full restart.");
                    }
                    catch { }
                    // Do not leave a half-loaded reception fighting with missing cards/units.
                    try
                    {
                        RMRRealizationManager.EndHubSessionToLibrary();
                        StageController sc = Singleton<StageController>.Instance;
                        if (sc != null)
                            sc.GameOver(false, true);
                    }
                    catch (Exception goEx)
                    {
                        Debug.LogError("[RMR] Continue abort GameOver failed: " + goEx);
                    }
                    return;
                }
                try
                {
                    RMRCore.CurrentGamemode.AfterInitializeGamemode();
                }
                catch (Exception ex)
                {
                    Debug.LogError("[RMR] Continue AfterInitializeGamemode failed: " + ex);
                }
                if (loadOk)
                    this.Log("CONTINUED ROGUELIKE RUN! " + RMRCore.CurrentGamemode.SaveDataString);
                return;
            }
            else if (bookRecipe.id == new LorId(LogLikeMod.ModId, -2854))
            {
                RoguelikeGamemodeController.Instance.LoadGamemodeByStageRecipe(new LorId(RMRCore.packageId, -2854), false);
                RMRCore.CurrentGamemode.FilterContent();
                RMRCore.CurrentGamemode.BeforeInitializeGamemode();
                bookRecipe.mapInfo.Clear();
                orig(self);
                LoguePlayDataSaver.LoadChDebugData(ChapterGrade.Grade2);
                RMRCore.CurrentGamemode.AfterInitializeGamemode();
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter1());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter2());
                LoguePlayDataSaver.RemovePlayerData();
            }
            else if (bookRecipe.id == new LorId(LogLikeMod.ModId, -3854))
            {
                RoguelikeGamemodeController.Instance.LoadGamemodeByStageRecipe(new LorId(RMRCore.packageId, -3854), false);
                RMRCore.CurrentGamemode.FilterContent();
                RMRCore.CurrentGamemode.BeforeInitializeGamemode();
                bookRecipe.mapInfo.Clear();
                orig(self);
                LoguePlayDataSaver.LoadChDebugData(ChapterGrade.Grade3);
                RMRCore.CurrentGamemode.AfterInitializeGamemode();
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter1());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter2());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter3());
                LoguePlayDataSaver.RemovePlayerData();
            }
            else if (bookRecipe.id == new LorId(LogLikeMod.ModId, -4854))
            {
                RoguelikeGamemodeController.Instance.LoadGamemodeByStageRecipe(new LorId(RMRCore.packageId, -4854), false);
                RMRCore.CurrentGamemode.FilterContent();
                RMRCore.CurrentGamemode.BeforeInitializeGamemode();
                bookRecipe.mapInfo.Clear();
                orig(self);
                LoguePlayDataSaver.LoadChDebugData(ChapterGrade.Grade4);
                RMRCore.CurrentGamemode.AfterInitializeGamemode();
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter1());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter2());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter3());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter4());
                LoguePlayDataSaver.RemovePlayerData();
            }
            else if (bookRecipe.id == new LorId(LogLikeMod.ModId, -5854))
            {
                RoguelikeGamemodeController.Instance.LoadGamemodeByStageRecipe(new LorId(RMRCore.packageId, -5854), false);
                RMRCore.CurrentGamemode.FilterContent();
                RMRCore.CurrentGamemode.BeforeInitializeGamemode();
                bookRecipe.mapInfo.Clear();
                orig(self);
                LoguePlayDataSaver.LoadChDebugData(ChapterGrade.Grade5);
                RMRCore.CurrentGamemode.AfterInitializeGamemode();
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter1());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter2());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter3());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter4());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter5());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftExclusiveCardChapter5());
                LoguePlayDataSaver.RemovePlayerData();
            }
            else if (bookRecipe.id == new LorId(LogLikeMod.ModId, -6854))
            {
                RoguelikeGamemodeController.Instance.LoadGamemodeByStageRecipe(new LorId(RMRCore.packageId, -6854), false);
                RMRCore.CurrentGamemode.FilterContent();
                RMRCore.CurrentGamemode.BeforeInitializeGamemode();
                bookRecipe.mapInfo.Clear();
                orig(self);
                LoguePlayDataSaver.LoadChDebugData(ChapterGrade.Grade6);
                RMRCore.CurrentGamemode.AfterInitializeGamemode();
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter1());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter2());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter3());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter4());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter5());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftExclusiveCardChapter5());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftExclusiveCardChapter6());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter6());
                LoguePlayDataSaver.RemovePlayerData();
            }
            else if (bookRecipe.id == new LorId(LogLikeMod.ModId, -7854))
            {
                RoguelikeGamemodeController.Instance.LoadGamemodeByStageRecipe(new LorId(RMRCore.packageId, -7854), false);
                RMRCore.CurrentGamemode.FilterContent();
                RMRCore.CurrentGamemode.BeforeInitializeGamemode();
                bookRecipe.mapInfo.Clear();
                orig(self);
                LoguePlayDataSaver.LoadChDebugData(ChapterGrade.Grade7);
                RMRCore.CurrentGamemode.AfterInitializeGamemode();
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter1());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter2());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter3());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter4());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter5());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter6());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftExclusiveCardChapter5());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftExclusiveCardChapter6());
                Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new CraftEquipChapter7());
                LoguePlayDataSaver.RemovePlayerData();
            }

            try
            {
                succes = RoguelikeGamemodeController.Instance.LoadGamemodeByStageRecipe(invitation, false);
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
            if (RMRCore.CurrentGamemode == null)
                RMRCore.CurrentGamemode = new RoguelikeGamemode_RMR_Default();

            if (succes)
            {
                RMRCore.CurrentGamemode.FilterContent();
                RMRCore.CurrentGamemode.BeforeInitializeGamemode();
                bookRecipe.mapInfo.Clear();
                LogueBookModels.CreatePlayer();
                orig(self);
                LogueBookModels.CreatePlayerBattle();
                LoguePlayDataSaver.RemovePlayerData();
                RMRCore.CurrentGamemode.AfterInitializeGamemode();
                GlobalLogueEffectManager.Instance.AddEffects(new RMREffect_ExtendedFunctionalityEffect());
                this.Log("NEW RUN! " + RMRCore.CurrentGamemode.SaveDataString);
            }
            else
            {
                orig(self);
                this.Log("REGULAR RECEPTION!");
            }
        }

        /// <summary>
        /// Hook to force the character list panel to render unitData based on <see cref="LogueBookModels.playerBattleModel"/>.
        /// </summary>
        public void UILibrarianCharacterListPanel_SetLibrarianCharacterListPanel_Battle(
          Action<UILibrarianCharacterListPanel> orig,
          UILibrarianCharacterListPanel self)
        {
            if (LogLikeRoutines.IsRoguelikeBattleSettingContext())
            {
                UICharacterList uiCharacterList = (UICharacterList)typeof(UILibrarianCharacterListPanel).GetField("CharacterList", AccessTools.all).GetValue(self);
                List<UnitBattleDataModel> playerBattleModel = LogueBookModels.playerBattleModel;
                self.SetCharacterRenderer(playerBattleModel, false);
                uiCharacterList.InitUnitListFromBattleData(playerBattleModel);
                typeof(UILibrarianCharacterListPanel).GetMethod("UpdateFrameToSephirah", AccessTools.all).Invoke(self, new object[1]
                {
                     SephirahType.None
                });
            }
            else
                orig(self);
        }

        /// <summary>
        /// Hook to disable sephirot selection buttons.
        /// </summary>
        public void UILibrarianCharacterListPanel_InitSephirahSelectionButtonsInBattle(
          Action<UILibrarianCharacterListPanel, List<StageLibraryFloorModel>> orig,
          UILibrarianCharacterListPanel self,
          List<StageLibraryFloorModel> floors)
        {
            if (LogLikeRoutines.IsRoguelikeBattleSettingContext())
            {
                
                foreach (UISephirahSelectionButton sephirahSelectionButton in (List<UISephirahSelectionButton>)typeof(UILibrarianCharacterListPanel).GetField("SephirahSelectionButtons", AccessTools.all).GetValue(self))
                {
                    sephirahSelectionButton.Deactivate();
                    sephirahSelectionButton.SetLock();
                }
            }
            else
                orig(self, floors);
        }

    }

    [HarmonyPatch]
    public class LogLikePatches
    {
        #region PREFIXES 
        /// <summary>
        /// Prefix patch to hijack the abnormality page get card id method to support the reward dictionary.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(EmotionEgoXmlInfo), "CardId", MethodType.Getter)]
        public static bool EmotionEgoXmlInfo_get_CardId(EmotionEgoXmlInfo __instance, ref LorId __result)
        {
            if (LogLikeMod.RewardCardDic_Dummy == null)
                return true;
            foreach (KeyValuePair<string, List<EmotionEgoXmlInfo>> keyValuePair in LogLikeMod.RewardCardDic_Dummy)
            {
                if (keyValuePair.Value.Contains(__instance))
                {
                    __result = new LorId(keyValuePair.Key, __instance._CardId);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Important prefix patch to correctly get upgraded cards from the inventory/card dictionary.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ItemXmlDataList), nameof(ItemXmlDataList.GetCardItem), new Type[2] { typeof(LorId), typeof(bool) })]
        public static bool ItemXmlDataList_GetCardItem(
          ItemXmlDataList __instance,
          LorId id,
          bool errNull = false)
        {
            UpgradeMetadata metadata;
            if (LogLikeMod.CheckStage() && UpgradeMetadata.UnpackPid(id.packageId, out metadata))
                Singleton<LogCardUpgradeManager>.Instance.GetUpgradeCard(new LorId(metadata.actualPid, id.id), metadata.index, metadata.count);
            return true;
        }

        /// <summary>
        /// Routes selected string ids through RMR/LogLike localization
        /// (<c>Localize/{lang}/</c> via abcdcode_LOGLIKE_MOD_Extension.TextDataModel).
        /// Translators: add/edit text under Localize — see docs/localization/.
        /// Explicit prefixes always apply; other keys only while an RMR run/realization is active.
        /// </summary>
        /// <summary>
        /// Postfix, deliberately not a blocking prefix.
        ///
        /// This used to be a prefix returning false, which in Harmony skips the original method AND
        /// every later prefix. Once a run started, ShouldOverrideVanillaTextWithRmrText accepted
        /// nearly every key, so any other localization mod's patch on GetText was silently dropped --
        /// the most likely reason RMR + Localization Manager crashed during load rather than merely
        /// disagreeing about text.
        ///
        /// As a postfix, other mods still run, and a throw here costs only RMR's own override
        /// instead of breaking GetText for the whole game.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(TextDataModel), nameof(TextDataModel.GetText))]
        public static void TextDataModel_GetText(string id, ref string __result, params object[] args)
        {
            try
            {
                if (!ShouldOverrideVanillaTextWithRmrText(id))
                    return;

                string text = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText(id, args);
                if (string.IsNullOrEmpty(text))
                    return;
                __result = RewardingModel.SanitizeDisplayText(text);
            }
            catch (Exception ex)
            {
                // GetText is called from everywhere; never let it fail because of RMR.
                if (_loggedGetTextFailure)
                    return;
                _loggedGetTextFailure = true;
                Debug.LogWarning("[RMR Localize] TextDataModel.GetText override failed (logged once): " + ex.Message);
            }
        }

        private static bool _loggedGetTextFailure;

        /// <summary>Whether RMR may replace vanilla TextDataModel.GetText for this id.</summary>
        private static bool ShouldOverrideVanillaTextWithRmrText(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            if (IsExplicitRmrTextKey(id))
                return true;

            return LogLikeMod.CheckStage()
                || RMRRealizationManager.PendingRealizationBattle
                || RMRRealizationManager.InRealizationBattle;
        }

        /// <summary>
        /// Keys that always come from RMR Localize (hub, help, compendium, mysteries, …).
        /// Prefix list is the contract with Localize files — keep in sync when adding new id families.
        /// </summary>
        private static bool IsExplicitRmrTextKey(string id)
        {
            if (id == "ui_ExceptionWithLog")
                return true;

            return id.StartsWith("ui_RMR_", StringComparison.Ordinal)
                || id.StartsWith("RMR_", StringComparison.Ordinal)
                || id.StartsWith("LogueLike", StringComparison.Ordinal)
                || id.StartsWith("BossReward", StringComparison.Ordinal)
                || id.StartsWith("MysteryCh", StringComparison.Ordinal)
                || id.StartsWith("StartBoost", StringComparison.Ordinal)
                || id.StartsWith("MemberShip", StringComparison.Ordinal);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(StageController), nameof(StageController.RoundStartPhase_System))]
        public static bool StageController_RoundStartPhase_System(StageController __instance)
        {
            // First combat round of a realization fight — allow EndBattle cleanup afterward.
            // Also re-ensures multiphase passives every round (Angela/Roland E.G.O forms).
            if (RMRRealizationManager.InRealizationBattle)
                RMRRealizationManager.MarkRealizationCombatLive();

            // Mid-battle EGO suppress flag is only needed through the emotion RoundEnd → next round.
            // Non-combat node exit (shop/mystery leave) also ends once the next wave's RoundStart runs.
            try { RewardingModel.ClearSuppressSpuriousEndBattle(); } catch { /* ignore */ }
            try { RewardingModel.ClearNonCombatNodeExit(); } catch { /* ignore */ }
            try { RewardingModel.ObserveLivingEnemyForVictoryConfirmation(); } catch { /* ignore */ }

            if (!LogLikeMod.GetFieldValue<bool>(__instance, "_bCalledRoundStart_system") && LogLikeMod.CheckStage())
                Singleton<GlobalLogueEffectManager>.Instance.OnRoundStart(__instance);
            return true;
        }

        /// <summary>
        /// Vanilla multiphase bosses (PassiveAbility_105010 etc.) rely on IsImmortal to stay at 1 HP
        /// until OnRoundEndTheLast advances the phase. If immortality is missing, Die ends after form 1.
        /// Parameter names must match vanilla: Die(BattleUnitModel attacker, bool callEvent).
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(BattleUnitModel), nameof(BattleUnitModel.Die))]
        public static bool BattleUnitModel_Die_RealizationMultiphase(
            BattleUnitModel __instance,
            BattleUnitModel attacker,
            bool callEvent)
        {
            if (!RMRRealizationManager.ShouldBlockRealizationBossDeath(__instance))
                return true;
            try
            {
                if (__instance.hp < 1f)
                    __instance.SetHp(1);
                int bid = 0;
                try { if (__instance.Book != null) bid = __instance.Book.GetBookClassInfoId().id; } catch { }
                Debug.Log($"[RMRRealizationManager] Blocked Die on multiphase boss (hp clamped). book={bid}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMRRealizationManager] Multiphase Die block: " + ex.Message);
            }
            return false;
        }

        /*
        [HarmonyPrefix, HarmonyPatch(typeof(StageController), nameof(StageController.StartParrying))]
        public static bool StageController_StartParrying(
          StageController __instance,
          BattlePlayingCardDataInUnitModel cardA,
          BattlePlayingCardDataInUnitModel cardB)
        {
            if (cardA.owner.passiveDetail.HasPassive<PassiveAbility_ShopPassiveMook4>() && cardB is BattleKeepedCardDataInUnitModel)
            {
                LogLikeMod.SetStagePhase(__instance, StageController.StagePhase.ExecuteOneSideAction);
                cardA.owner.turnState = BattleUnitTurnState.DOING_ACTION;
                cardA.target.turnState = BattleUnitTurnState.DOING_ACTION;
                Singleton<BattleOneSidePlayManager>.Instance.StartOneSidePlay(cardA);
                return false;
            }
            if (!cardB.owner.passiveDetail.HasPassive<PassiveAbility_ShopPassiveMook4>() || !(cardA is BattleKeepedCardDataInUnitModel))
                return true;
            LogLikeMod.SetStagePhase(__instance, StageController.StagePhase.ExecuteOneSideAction);
            cardB.owner.turnState = BattleUnitTurnState.DOING_ACTION;
            cardB.target.turnState = BattleUnitTurnState.DOING_ACTION;
            Singleton<BattleOneSidePlayManager>.Instance.StartOneSidePlay(cardB);
            return false;
        }
        */

        /// <summary>
        /// Currently unused (?) patch to add support for modded story icons (strictly for Roguelike).
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(UISpriteDataManager), nameof(UISpriteDataManager.GetStoryIcon))]
        public static bool UISpriteDataManager_GetStoryIcon(
          string story,
          ref UIIconManager.IconSet __result)
        {
            if (!LogLikeMod.CheckStage() || !story.Contains("<LogLike>"))
                return true;
            string key = story.Remove(0, 9);
            Sprite artWork = LogLikeMod.ArtWorks[key];
            Sprite artWorkGlow = LogLikeMod.ArtWorks[key + "_glow"];
            if (artWork != null)
                __result = new UIIconManager.IconSet()
                {
                    icon = artWork,
                    iconGlow = artWorkGlow ?? artWork
                };
            return false;
        }

        /// <summary>
        /// Patch to hijack the BattleSetting screen to add the inventory, abnormality and crafting tabs.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(UIBattleSettingEditPanel), nameof(UIBattleSettingEditPanel.SetBUttonState))]
        public static bool UIBattleSettingEditPanel_SetBUttonState(
          UIBattleSettingEditPanel __instance,
          UIBattleSettingEditTap state)
        {
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
            {
                Singleton<GlobalLogueInventoryPanel>.Instance.SetActive(false);
                Singleton<LogCompendiumPanel>.Instance.SetActive(false);
                return true;
            }
            Button fieldValue1 = LogLikeMod.GetFieldValue<Button>(__instance, "button_EquipPage");
            Button fieldValue2 = LogLikeMod.GetFieldValue<Button>(__instance, "button_BattleCard");
            Image fieldValue3 = LogLikeMod.GetFieldValue<Image>(__instance, "img_equippageFrame");
            Image fieldValue4 = LogLikeMod.GetFieldValue<Image>(__instance, "img_battlecardFrame");
            UISettingEquipPageInvenPanel fieldValue5 = LogLikeMod.GetFieldValue<UISettingEquipPageInvenPanel>(__instance, "_equipPagePanel");
            UISettingCardInvenPanel fieldValue6 = LogLikeMod.GetFieldValue<UISettingCardInvenPanel>(__instance, "_battleCardPanel");
            RectTransform fieldValue7 = LogLikeMod.GetFieldValue<RectTransform>(__instance, "rect_LeftBg");
            switch (state)
            {
                case (UIBattleSettingEditTap)2:
                    fieldValue7.localPosition = (Vector3)new Vector2(0.0f, 0.0f);
                    ColorBlock colors1 = fieldValue1.colors;
                    ColorBlock colors2 = fieldValue2.colors;
                    colors1.normalColor = UIColorManager.Manager.GetUIColor(UIColor.Default);
                    colors2.normalColor = UIColorManager.Manager.GetUIColor(UIColor.Default);
                    fieldValue1.colors = colors1;
                    fieldValue2.colors = colors2;
                    fieldValue3.enabled = false;
                    fieldValue4.enabled = false;
                    fieldValue5.SetActivePanel(false);
                    fieldValue6.SetActivePanel(false);
                    if (LogLikeMod.InvenBtnFrame != null) LogLikeMod.InvenBtnFrame.enabled = true;
                    if (LogLikeMod.CreatureBtnFrame != null) LogLikeMod.CreatureBtnFrame.enabled = false;
                    if (LogLikeMod.CraftBtnFrame != null) LogLikeMod.CraftBtnFrame.enabled = false;
                    if (LogLikeMod.CompendiumBtnFrame != null) LogLikeMod.CompendiumBtnFrame.enabled = false;
                    Singleton<GlobalLogueInventoryPanel>.Instance.SetActive(true);
                    Singleton<LogCreatureTabPanel>.Instance.SetActive(false);
                    Singleton<LogCraftPanel>.Instance.SetActive(false);
                    try { Singleton<LogCompendiumPanel>.Instance.SetActive(false); } catch { }
                    return false;
                case (UIBattleSettingEditTap)3:
                    fieldValue7.localPosition = (Vector3)new Vector2(0.0f, 0.0f);
                    ColorBlock colors3 = fieldValue1.colors;
                    ColorBlock colors4 = fieldValue2.colors;
                    colors3.normalColor = UIColorManager.Manager.GetUIColor(UIColor.Default);
                    colors4.normalColor = UIColorManager.Manager.GetUIColor(UIColor.Default);
                    fieldValue1.colors = colors3;
                    fieldValue2.colors = colors4;
                    fieldValue3.enabled = false;
                    fieldValue4.enabled = false;
                    fieldValue5.SetActivePanel(false);
                    fieldValue6.SetActivePanel(false);
                    if (LogLikeMod.InvenBtnFrame != null) LogLikeMod.InvenBtnFrame.enabled = false;
                    if (LogLikeMod.CreatureBtnFrame != null) LogLikeMod.CreatureBtnFrame.enabled = true;
                    if (LogLikeMod.CraftBtnFrame != null) LogLikeMod.CraftBtnFrame.enabled = false;
                    if (LogLikeMod.CompendiumBtnFrame != null) LogLikeMod.CompendiumBtnFrame.enabled = false;
                    Singleton<GlobalLogueInventoryPanel>.Instance.SetActive(false);
                    Singleton<LogCreatureTabPanel>.Instance.SetActive(true);
                    Singleton<LogCraftPanel>.Instance.SetActive(false);
                    try { Singleton<LogCompendiumPanel>.Instance.SetActive(false); } catch { }
                    return false;
                case (UIBattleSettingEditTap)4:
                    fieldValue7.localPosition = (Vector3)new Vector2(0.0f, 0.0f);
                    ColorBlock colors5 = fieldValue1.colors;
                    ColorBlock colors6 = fieldValue2.colors;
                    colors5.normalColor = UIColorManager.Manager.GetUIColor(UIColor.Default);
                    colors6.normalColor = UIColorManager.Manager.GetUIColor(UIColor.Default);
                    fieldValue1.colors = colors5;
                    fieldValue2.colors = colors6;
                    fieldValue3.enabled = false;
                    fieldValue4.enabled = false;
                    fieldValue5.SetActivePanel(false);
                    fieldValue6.SetActivePanel(false);
                    if (LogLikeMod.InvenBtnFrame != null) LogLikeMod.InvenBtnFrame.enabled = false;
                    if (LogLikeMod.CreatureBtnFrame != null) LogLikeMod.CreatureBtnFrame.enabled = false;
                    if (LogLikeMod.CraftBtnFrame != null) LogLikeMod.CraftBtnFrame.enabled = true;
                    if (LogLikeMod.CompendiumBtnFrame != null) LogLikeMod.CompendiumBtnFrame.enabled = false;
                    Singleton<GlobalLogueInventoryPanel>.Instance.SetActive(false);
                    Singleton<LogCreatureTabPanel>.Instance.SetActive(false);
                    Singleton<LogCraftPanel>.Instance.SetActive(true);
                    try { Singleton<LogCompendiumPanel>.Instance.SetActive(false); } catch { }
                    return false;
                case (UIBattleSettingEditTap)5:
                    // Atlas moved to RMR start hub — never open from prepare tabs.
                    fieldValue7.localPosition = (Vector3)new Vector2(0.0f, 0.0f);
                    ColorBlock colors7 = fieldValue1.colors;
                    ColorBlock colors8 = fieldValue2.colors;
                    colors7.normalColor = UIColorManager.Manager.GetUIColor(UIColor.Default);
                    colors8.normalColor = UIColorManager.Manager.GetUIColor(UIColor.Default);
                    fieldValue1.colors = colors7;
                    fieldValue2.colors = colors8;
                    fieldValue3.enabled = false;
                    fieldValue4.enabled = false;
                    fieldValue5.SetActivePanel(false);
                    fieldValue6.SetActivePanel(false);
                    if (LogLikeMod.InvenBtnFrame != null) LogLikeMod.InvenBtnFrame.enabled = false;
                    if (LogLikeMod.CreatureBtnFrame != null) LogLikeMod.CreatureBtnFrame.enabled = false;
                    if (LogLikeMod.CraftBtnFrame != null) LogLikeMod.CraftBtnFrame.enabled = false;
                    if (LogLikeMod.CompendiumBtnFrame != null) LogLikeMod.CompendiumBtnFrame.enabled = false;
                    Singleton<GlobalLogueInventoryPanel>.Instance.SetActive(false);
                    Singleton<LogCreatureTabPanel>.Instance.SetActive(false);
                    Singleton<LogCraftPanel>.Instance.SetActive(false);
                    try { Singleton<LogCompendiumPanel>.Instance.SetActive(false); } catch { }
                    return false;
                default:
                    // EquipPage / BattleCard (vanilla handles open). Hide RMR overlays first so they
                    // cannot sit above the inventory list with a higher canvas sortingOrder.
                    Singleton<GlobalLogueInventoryPanel>.Instance.SetActive(false);
                    Singleton<LogCreatureTabPanel>.Instance.SetActive(false);
                    Singleton<LogCraftPanel>.Instance.SetActive(false);
                    try { Singleton<LogCompendiumPanel>.Instance.SetActive(false); } catch { }
                    if (LogLikeMod.InvenBtnFrame != null) LogLikeMod.InvenBtnFrame.enabled = false;
                    if (LogLikeMod.CreatureBtnFrame != null) LogLikeMod.CreatureBtnFrame.enabled = false;
                    if (LogLikeMod.CraftBtnFrame != null) LogLikeMod.CraftBtnFrame.enabled = false;
                    if (LogLikeMod.CompendiumBtnFrame != null) LogLikeMod.CompendiumBtnFrame.enabled = false;
                    try { CollapseGlobalEffectDrawerIfExpanded(); } catch { }
                    return true;
            }
        }

        /// <summary>
        /// After vanilla opens Equip/Battle inventory, force correct draw order:
        /// inventory panels (key pages + combat pages) above dimmer + RMR HUD clone.
        /// LogUIObjs[100] is a LevelUpUI clone with sortingOrder += 100 and was covering
        /// the center book/card lists so artwork looked buried under a dark layer.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(UIBattleSettingEditPanel), nameof(UIBattleSettingEditPanel.SetBUttonState))]
        public static void UIBattleSettingEditPanel_SetBUttonState_ZOrder(
          UIBattleSettingEditPanel __instance,
          UIBattleSettingEditTap state)
        {
            if (__instance == null || !LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return;
            // Leaving combat bookshelf → clear detailSlot canvas boost.
            if (state != UIBattleSettingEditTap.BattleCard)
            {
                try { RMRCombatCardDetailLayer.Restore(); } catch { /* ignore */ }
            }
            // Leaving key-page inventory → clear equip preview canvas boost.
            if (state != UIBattleSettingEditTap.EquipPage)
            {
                try { RMREquipPagePreviewLayer.Restore(); } catch { /* ignore */ }
            }
            // Any tab change first puts every forced canvas back; the call below re-applies only
            // what the new tab needs. Previously the equip raise and the HUD clamp both survived
            // the tab change and then the rest of the run.
            try { RestoreForcedCanvasOrders(); } catch { /* ignore */ }
            // Remember the tab so the popup handlers can re-apply after a vanilla popup closes.
            _lastPrepareEditPanel = __instance;
            _lastPrepareTab = state;
            if (state != UIBattleSettingEditTap.EquipPage && state != UIBattleSettingEditTap.BattleCard)
                return;
            try
            {
                RepairPrepareInventoryDrawOrder(__instance, state);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR UI] Equip inventory z-order repair failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Match vanilla combat hover: list slot highlights only; enlarged card is
        /// list.detailSlot (ShowDetailSlotByInventory). After vanilla places it, put
        /// detailSlot last among siblings (+ light Canvas boost on detail only).
        /// No list-card clone, no reparent off the prepare hierarchy.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(UIInvenCardListScroll), nameof(UIInvenCardListScroll.ShowDetailSlotByInventory))]
        public static void UIInvenCardListScroll_ShowDetailSlotByInventory_ElevateDetail(
            UIInvenCardListScroll __instance,
            UIOriginCardSlot slot)
        {
            if (__instance == null || !LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return;
            try
            {
                if (__instance.gameObject.GetComponent<RMRCombatCardHoverFollow>() == null)
                    __instance.gameObject.AddComponent<RMRCombatCardHoverFollow>();
                RMRCombatCardDetailLayer.ElevateDetailSlot(__instance);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR UI] detailSlot bring-to-front failed: " + ex.Message);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIInvenCardListScroll), nameof(UIInvenCardListScroll.HideDetailSlotByInventory))]
        public static void UIInvenCardListScroll_HideDetailSlotByInventory_RestoreDetail(
            UIInvenCardListScroll __instance)
        {
            try { RMRCombatCardDetailLayer.Restore(); }
            catch { /* ignore */ }
        }

        /// <summary>
        /// Key-page hover preview: same regression family as combat detailSlot.
        /// RMR raises EquipLeftPanel canvas (inventoryOrder+1), which buries the vanilla
        /// UIEquipPagePreviewPanel (it inherits edit-panel order 12). After vanilla shows
        /// the preview, put it last among siblings + temporary Canvas boost; restore on hide.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(UISettingEquipPageInvenPanel), nameof(UISettingEquipPageInvenPanel.ShowPreviewPanel))]
        public static void UISettingEquipPageInvenPanel_ShowPreviewPanel_ElevatePreview(
            UISettingEquipPageInvenPanel __instance)
        {
            if (__instance == null || !LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return;
            try { RMREquipPagePreviewLayer.ElevatePreviewPanel(__instance); }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR UI] equip preview bring-to-front failed: " + ex.Message);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UISettingEquipPageInvenPanel), nameof(UISettingEquipPageInvenPanel.HidePreviewPanel))]
        public static void UISettingEquipPageInvenPanel_HidePreviewPanel_RestorePreview(
            UISettingEquipPageInvenPanel __instance)
        {
            try { RMREquipPagePreviewLayer.Restore(); }
            catch { /* ignore */ }
        }

        /// <summary>
        /// Soften dimmer + RMR HUD for prepare inventory.
        /// Combat pages: do NOT RaiseUiPanelCanvas on the whole battle-card panel
        /// (that flattened sorting and let list slots cover vanilla detailSlot).
        /// Key-page equip panel may still raise above HUD.
        /// </summary>
        private static void RepairPrepareInventoryDrawOrder(
            UIBattleSettingEditPanel editPanel,
            UIBattleSettingEditTap state)
        {
            CollapseGlobalEffectDrawerIfExpanded();

            try { RMRRealizationLaunchHost.DestroyOverlayIfEmpty(); } catch { /* optional */ }

            Image blockBg = LogLikeMod.GetFieldValue<Image>(editPanel, "img_BlockBackGroundBg");
            if (blockBg != null)
            {
                try { blockBg.transform.SetAsFirstSibling(); } catch { }
                try { blockBg.raycastTarget = false; } catch { }
            }

            UISettingEquipPageInvenPanel equipPanel =
                LogLikeMod.GetFieldValue<UISettingEquipPageInvenPanel>(editPanel, "_equipPagePanel");
            UISettingCardInvenPanel cardPanel =
                LogLikeMod.GetFieldValue<UISettingCardInvenPanel>(editPanel, "_battleCardPanel");

            int hudOrder = GetRmrHudCanvasSortingOrder();
            int inventoryOrder = Math.Max(hudOrder + 20, 140);
            // Publish the top order so hover previews can boost above it instead of guessing 250.
            LogLikeMod.PrepareInventoryTopSortingOrder = inventoryOrder + 1;

            if (state == UIBattleSettingEditTap.EquipPage && equipPanel != null)
            {
                try
                {
                    equipPanel.transform.SetAsLastSibling();
                    if (equipPanel.EquipLeftPanel != null)
                        equipPanel.EquipLeftPanel.transform.SetAsLastSibling();
                }
                catch { }
                RaiseUiPanelCanvas(equipPanel.gameObject, inventoryOrder);
                try
                {
                    if (equipPanel.EquipLeftPanel != null)
                        RaiseUiPanelCanvas(equipPanel.EquipLeftPanel.gameObject, inventoryOrder + 1);
                }
                catch { }
            }

            if (state == UIBattleSettingEditTap.BattleCard && cardPanel != null)
            {
                // Sibling only — keep vanilla EditPanel.canvas sortingOrder=12 semantics so
                // detailSlot (later sibling) paints above list slots. Soften RMR HUD.
                try { cardPanel.transform.SetAsLastSibling(); } catch { }
                try { ClearForcedPanelCanvasRaise(cardPanel.gameObject); } catch { /* ignore */ }
                SoftenRmrHudCanvasForInventory(lowerSortingIfAbove: 50);
            }
            else
            {
                SoftenRmrHudCanvasForInventory(lowerSortingIfAbove: -1);
            }
        }

        /// <summary>
        /// Undo prior RaiseUiPanelCanvas on the battle-card panel so list/detail share
        /// the vanilla edit-panel canvas again (sibling order works for detailSlot).
        /// </summary>
        private static void ClearForcedPanelCanvasRaise(GameObject panelRoot)
        {
            if (panelRoot == null)
                return;
            Canvas c = panelRoot.GetComponent<Canvas>();
            if (c == null)
                return;
            // Only clear aggressive overrides we introduced (high order + override).
            if (c.overrideSorting && c.sortingOrder >= 100)
            {
                c.overrideSorting = false;
                // Leave component; disabling override returns to parent EditPanel order 12.
            }
        }

        private static int GetRmrHudCanvasSortingOrder()
        {
            try
            {
                if (LogLikeMod.LogUIObjs != null && LogLikeMod.LogUIObjs.dic != null
                    && LogLikeMod.LogUIObjs.dic.TryGetValue(100, out GameObject hudRoot)
                    && hudRoot != null)
                {
                    Canvas hudCanvas = hudRoot.GetComponent<Canvas>();
                    if (hudCanvas != null)
                        return hudCanvas.sortingOrder;
                }
            }
            catch { }
            return 100;
        }

        /// <param name="lowerSortingIfAbove">
        /// If &gt;= 0 and HUD canvas sortingOrder is above this, clamp it down so money/effects
        /// stay visible without burying the combat bookshelf (vanilla edit panel is order 12).
        /// Pass -1 to skip sorting clamp.
        /// </param>
        private static void SoftenRmrHudCanvasForInventory(int lowerSortingIfAbove = -1)
        {
            try
            {
                if (LogLikeMod.LogUIObjs == null || LogLikeMod.LogUIObjs.dic == null)
                    return;
                if (!LogLikeMod.LogUIObjs.dic.TryGetValue(100, out GameObject hudRoot) || hudRoot == null)
                    return;

                CanvasGroup cg = hudRoot.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }

                Canvas hudCanvas = hudRoot.GetComponent<Canvas>();
                if (hudCanvas != null && lowerSortingIfAbove >= 0 && hudCanvas.sortingOrder > lowerSortingIfAbove)
                {
                    // Keep slightly above vanilla edit panel (12) so money still shows, but
                    // well below detailSlot boost (250).
                    // Snapshot first: LogUIObjs[100] is cached for the whole run, so leaving it
                    // clamped here is what buried the post-battle reward Skip button (its panel
                    // lives on LogUIObjs[100], under the full-screen mask on LogUIObjs[90]) and
                    // made the Pierre meat-pie quest impossible to finish.
                    RememberCanvasOrder(hudCanvas);
                    hudCanvas.overrideSorting = true;
                    hudCanvas.sortingOrder = lowerSortingIfAbove;
                }

                foreach (Image img in hudRoot.GetComponentsInChildren<Image>(true))
                {
                    if (img == null)
                        continue;
                    string n = img.gameObject.name ?? "";
                    if (n.IndexOf("Money", StringComparison.OrdinalIgnoreCase) >= 0
                        || n.IndexOf("Effect", StringComparison.OrdinalIgnoreCase) >= 0
                        || n.IndexOf("Icon", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;
                    RectTransform rt = img.rectTransform;
                    if (rt == null)
                        continue;
                    bool fullStretch = rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one
                        && Mathf.Abs(rt.offsetMin.x) < 1f && Mathf.Abs(rt.offsetMin.y) < 1f
                        && Mathf.Abs(rt.offsetMax.x) < 1f && Mathf.Abs(rt.offsetMax.y) < 1f;
                    if (!fullStretch)
                        continue;
                    img.raycastTarget = false;
                    Color c = img.color;
                    if (c.a > 0.05f && (c.r + c.g + c.b) < 1.2f)
                    {
                        c.a = 0f;
                        img.color = c;
                    }
                }
            }
            catch { /* optional */ }
        }

        /// <summary>Pre-change state of a Canvas whose sorting RMR forced, so it can be put back.</summary>
        private sealed class CanvasSortingSnapshot
        {
            public Canvas Canvas;
            public bool Enabled;
            public bool OverrideSorting;
            public int SortingOrder;
        }

        /// <summary>
        /// Every canvas whose sorting the prepare screen currently forces.
        /// Both reported softlocks were one-way writes with no entry here: raising the key-page tab
        /// to 141 buried UIPassiveSuccessionPopup, and clamping the RMR HUD clone to 50 buried the
        /// reward screen's Skip button (LogUIObjs is cached for the whole run, so it never healed).
        /// Anything this file forces must be restorable.
        /// </summary>
        private static readonly List<CanvasSortingSnapshot> _forcedCanvasOrders = new List<CanvasSortingSnapshot>();

        private static void RememberCanvasOrder(Canvas canvas)
        {
            if (canvas == null)
                return;
            foreach (CanvasSortingSnapshot known in _forcedCanvasOrders)
            {
                // Keep the first snapshot: it holds the genuine pre-RMR values.
                if (ReferenceEquals(known.Canvas, canvas))
                    return;
            }
            _forcedCanvasOrders.Add(new CanvasSortingSnapshot
            {
                Canvas = canvas,
                Enabled = canvas.enabled,
                OverrideSorting = canvas.overrideSorting,
                SortingOrder = canvas.sortingOrder
            });
        }

        /// <summary>Put every forced canvas back exactly as it was. Safe to call repeatedly.</summary>
        private static void RestoreForcedCanvasOrders()
        {
            for (int i = _forcedCanvasOrders.Count - 1; i >= 0; i--)
            {
                CanvasSortingSnapshot snapshot = _forcedCanvasOrders[i];
                try
                {
                    if (snapshot.Canvas != null)
                    {
                        snapshot.Canvas.enabled = snapshot.Enabled;
                        snapshot.Canvas.overrideSorting = snapshot.OverrideSorting;
                        snapshot.Canvas.sortingOrder = snapshot.SortingOrder;
                    }
                }
                catch { /* destroyed canvas — nothing to restore */ }
            }
            _forcedCanvasOrders.Clear();
        }

        /// <summary>
        /// Ensure a panel has an override-sorted Canvas high enough to draw above RMR HUD clones.
        /// </summary>
        private static void RaiseUiPanelCanvas(GameObject panelRoot, int sortingOrder)
        {
            if (panelRoot == null)
                return;
            // Never fall back to GetComponentInParent. That reached the vanilla
            // UIBattleSettingEditPanel canvas (order 12) and pinned the entire key-page tab layer at
            // 141, which then painted over UIPassiveSuccessionPopup and its confirm dialogs.
            Canvas canvas = panelRoot.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = panelRoot.AddComponent<Canvas>();
                if (panelRoot.GetComponent<GraphicRaycaster>() == null)
                    panelRoot.AddComponent<GraphicRaycaster>();
            }
            RememberCanvasOrder(canvas);
            canvas.enabled = true;
            canvas.overrideSorting = true;
            if (canvas.sortingOrder < sortingOrder)
                canvas.sortingOrder = sortingOrder;
        }

        /// <summary>
        /// Expanded global-effect drawer uses BackGroundMask on LogUIObjs[100] (high sortingOrder).
        /// Collapse it when opening equip/battle inventory so the mask cannot dim/block book slots.
        /// </summary>
        private static void CollapseGlobalEffectDrawerIfExpanded()
        {
            try
            {
                GlobalLogueEffectManager mgr = Singleton<GlobalLogueEffectManager>.Instance;
                if (mgr == null)
                    return;
                if (mgr.IsOn && mgr.OnOffBtn != null)
                    mgr.OnOffBtn.OnClick();
                else if (mgr.BackGroundMask != null)
                    mgr.BackGroundMask.gameObject.SetActive(false);
                if (mgr.BackGroundMask != null)
                    mgr.BackGroundMask.gameObject.SetActive(false);
            }
            catch { /* ignore */ }
        }

        /// <summary>
        /// Prevents the reception from ending immediately upon clicking the back button. (I THINK)
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(UIBattleSettingPanel), nameof(UIBattleSettingPanel.OnClickBackButton))]
        public static bool UIBattleSettingPanel_OnClickBackButton()
        {
            // Close overlays first.
            if (RMRHelpHandbookPanel.Instance != null)
            {
                try
                {
                    if (RMRHelpHandbookPanel.Instance.IsVisible)
                    {
                        RMRHelpHandbookPanel.Instance.Hide();
                        return false;
                    }
                }
                catch { }
            }
            if (LogRealizationPanel.Instance != null && LogRealizationPanel.Instance.TryHandleBack())
                return false;
            if (RMRStartHubPanel.Instance != null && RMRStartHubPanel.Instance.IsVisible
                && RMRStartHubPanel.Instance.TryHandleBack())
                return false;

            // Floor pick on -853 shell (panel already closed): still must leave reception.
            if (RMRRealizationManager.AwaitingRealizationFloorPick
                && !RMRRealizationManager.PendingRealizationBattle
                && !RMRRealizationManager.InRealizationBattle)
            {
                RMRRealizationManager.CancelRealizationFloorPick();
                return false;
            }

            // Realization battle-prepare: back must CANCEL prep, never start the fight.
            if (RMRRealizationManager.PendingRealizationBattle
                || RMRRealizationManager.IsRealizationPreparationActive)
            {
                RMRRealizationManager.CancelRealizationPreparation();
                return false;
            }

            // Was a bare "return false" while the popup was open, which left no way out once the
            // key-page tab painted over its buttons. Close it instead of just swallowing the input.
            // (Also guards Instance: the old expression dereferenced it unconditionally.)
            try
            {
                if (LogLikeRoutines.IsRoguelikeBattleSettingContext()
                    && UIPassiveSuccessionPopup.Instance != null
                    && UIPassiveSuccessionPopup.Instance.isActiveAndEnabled)
                {
                    UIPassiveSuccessionPopup.Instance.CloseDefault();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR UI] passive-succession back handling failed: " + ex.Message);
            }
            return true;
        }

        /// <summary>Last prepare tab RMR applied draw-order fixes for, so they can be re-applied.</summary>
        private static UIBattleSettingEditPanel _lastPrepareEditPanel;
        private static UIBattleSettingEditTap _lastPrepareTab;

        [HarmonyPostfix, HarmonyPatch(typeof(UIPassiveSuccessionPopup), nameof(UIPassiveSuccessionPopup.Open))]
        public static void UIPassiveSuccessionPopup_Open()
        {
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return;
            LogLikeRoutines.SetBattleSettingCardPanelVisible(false);
            // The key-page tab was only ever hidden for the combat-card panel, so on the key-page
            // tab the raised inventory canvas painted straight over this popup — and with ESC and
            // the back button both suppressed, that was an unrecoverable softlock.
            try { RestoreForcedCanvasOrders(); } catch { /* ignore */ }
            // Same untranslated-passive tofu as the librarian info panel behind it.
            try
            {
                if (UIPassiveSuccessionPopup.Instance != null)
                    LogLikeMod.RepairTmpFontsUnder(UIPassiveSuccessionPopup.Instance.gameObject, "PassiveSuccessionPopup.Open");
            }
            catch { /* ignore */ }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIPassiveSuccessionPopup), nameof(UIPassiveSuccessionPopup.Close))]
        public static void UIPassiveSuccessionPopup_Close()
        {
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return;
            LogLikeRoutines.SetBattleSettingCardPanelVisible(true);
            ReapplyPrepareDrawOrderAfterPopup();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIPassiveSuccessionPopup), nameof(UIPassiveSuccessionPopup.CloseDefault))]
        public static void UIPassiveSuccessionPopup_CloseDefault()
        {
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return;
            LogLikeRoutines.SetBattleSettingCardPanelVisible(true);
            ReapplyPrepareDrawOrderAfterPopup();
        }

        /// <summary>
        /// Restore the inventory-above-HUD fix that <see cref="UIPassiveSuccessionPopup_Open"/> gave
        /// up, now that the popup is gone. Without this the RMR HUD clone covers the book list again.
        /// </summary>
        private static void ReapplyPrepareDrawOrderAfterPopup()
        {
            try
            {
                if (_lastPrepareEditPanel == null)
                    return;
                if (_lastPrepareTab != UIBattleSettingEditTap.EquipPage
                    && _lastPrepareTab != UIBattleSettingEditTap.BattleCard)
                    return;
                RepairPrepareInventoryDrawOrder(_lastPrepareEditPanel, _lastPrepareTab);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR UI] re-applying prepare draw order after popup failed: " + ex.Message);
            }
        }

        /*
        /// <summary>
        /// Patch for moving the positioning of enemy profiles.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(BattleUnitInfoManagerUI), nameof(BattleUnitInfoManagerUI.Initialize))]
        public static bool BattleUnitInfoManagerUI_Initialize(BattleUnitInfoManagerUI __instance)
        {
            if (__instance.enemyProfileArray.Length < 10)
            {
                List<Vector2> vector2List = new List<Vector2>()
                  {
                    new Vector2(121f, 410f),
                    new Vector2(121f, 480f),
                    new Vector2(121f, 550f),
                    new Vector2(121f, 620f),
                    new Vector2(121f, 690f)
                  };
                BattleCharacterProfileUI enemyProfile = __instance.enemyProfileArray[0];
                int index = 0;
                while (__instance.enemyProfileArray.Length < 10)
                {
                    __instance.enemyProfileArray = __instance.enemyProfileArray.AddToArray<BattleCharacterProfileUI>(LogLikeMod.LogBattleCharacterProfileUI.BattleUnitInfoManagerUI_Copying(enemyProfile));
                    __instance.enemyProfileArray[__instance.enemyProfileArray.Length - 1].gameObject.transform.localPosition = (Vector3)vector2List[index];
                    __instance.enemyProfileArray[__instance.enemyProfileArray.Length - 1].gameObject.SetActive(false);
                    ++index;
                }
            }
            return true;
        }
        */

        /// <summary>
        /// Patch for hijacking UI popups to show in the BattleSetting screen. (unsure)
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(UIPopupWindowManager), nameof(UIPopupWindowManager.Update))]
        public static bool UIPopupWindowManager_Update()
        {
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext()
                || UI.UIController.Instance.CurrentUIPhase != UIPhase.BattleSetting)
                return true;
            // Escape hatch. Vanilla Update only reads ESC, and suppressing it wholesale meant a
            // popup that ended up behind another layer could never be closed. Only let ESC through
            // while a popup is genuinely open, so it closes that popup instead of opening the
            // system menu.
            try
            {
                if (UIPassiveSuccessionPopup.Instance != null
                    && UIPassiveSuccessionPopup.Instance.isActiveAndEnabled)
                    return true;
            }
            catch { /* singleton not ready */ }
            return false;
        }


        [HarmonyPrefix, HarmonyPatch(typeof(UIPassiveSuccessionSlot), nameof(UIPassiveSuccessionSlot.SetDataModel))]
        public static bool UIPassiveSuccessionSlot_SetDataModel(
          UIPassiveSuccessionSlot __instance,
          PassiveModel passive,
          ref bool __result)
        {
            if (passive == null)
            {
                __result = false;
                return false;
            }
            if (passive.reservedData == null)
            {
                __result = false;
                return false;
            }
            if (!(passive.reservedData.currentpassive.id == new LorId(LogLikeMod.ModId, 1)))
                return true;
            __result = false;
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UICharacterListPanel), nameof(UICharacterListPanel.RefreshBattleUnitDataModel))]
        public static bool UICharacterListPanel_RefreshBattleUnitDataModel(
          UICharacterListPanel __instance,
          UnitDataModel data)
        {
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return true;
            __instance.Log("Refrash Character start");
            UnitDataModel targetUnit = ResolveRoguelikeUnitData(data);
            UnitBattleDataModel battledata = LogueBookModels.playerBattleModel?
                .Find(model => model?.unitData == targetUnit);
            if (battledata != null)
            {
                UICharacterSlot uiCharacterSlot = LogLikeMod.GetFieldValue<UICharacterList>(__instance, "CharacterList").slotList.Find(x => x.unitBattleData == battledata);
                if (uiCharacterSlot != null && uiCharacterSlot.unitBattleData != null)
                {
                    uiCharacterSlot.ReloadHpBattleSettingSlot();
                    __instance.Log("Refrash Character success");
                }
            }
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UnitDataModel), nameof(UnitDataModel.EquipBookForUI))]
        public static bool UnitDataModel_EquipBookForUI(
          UnitDataModel __instance,
          BookModel newBook,
          ref bool __result,
          bool isEnemySetting = false,
          bool force = false)
        {
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext() || isEnemySetting)
                return true;
            UnitDataModel targetUnit = ResolveRoguelikeUnitData(__instance);
            UnitBattleDataModel targetBattleModel = LogueBookModels.playerBattleModel?
                .Find(model => model?.unitData == targetUnit);
            if (targetUnit == null || targetBattleModel == null)
            {
                Debug.LogWarning("[RMR] EquipBookForUI could not resolve the selected realization librarian.");
                __result = false;
                return false;
            }
            int num = LogueBookModels.playerBattleModel.IndexOf(targetBattleModel);
            if (newBook != null)
            {
                targetUnit.Log("newBook not null");
                LogueBookModels.EquipNewPage(targetBattleModel, newBook.ClassInfo);
                LogueBookModels.RemoveEquip(targetUnit);
                newBook.SetOwner(targetUnit);
                __result = true;
                return false;
            }
            targetUnit.Log("newBook null");
            LogueBookModels.EquipNewPage(targetBattleModel, LogueBookModels.BaseXmlInfo);
            if (num != 0)
            {
                targetUnit.bookItem.ClassInfo.CharacterSkin[0] = "KetherLibrarian";
                typeof(BookModel).GetField("_selectedOriginalSkin", AccessTools.all).SetValue(targetUnit.bookItem, targetUnit.bookItem.ClassInfo.CharacterSkin[0]);
                typeof(BookModel).GetField("_characterSkin", AccessTools.all).SetValue(targetUnit.bookItem, targetUnit.bookItem.ClassInfo.CharacterSkin[0]);
            }
            LogueBookModels.RemoveEquip(targetUnit);
            __result = true;
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(BattleEmotionInfo_CenterBtn), nameof(BattleEmotionInfo_CenterBtn.OnPointerUp))]
        public static bool BattleEmotionInfo_CenterBtn_OnPointerUp()
        {
            return !LogLikeMod.CheckStage() || Singleton<MysteryManager>.Instance.curMystery == null && (Singleton<MysteryManager>.Instance.interruptMysterys == null || Singleton<MysteryManager>.Instance.interruptMysterys.Count <= 0) && Singleton<ShopManager>.Instance.curshop == null && !LogLikeMod.PauseBool;
        }

        /*
        [HarmonyPostfix, HarmonyPatch(typeof(BattleDiceCardUI), nameof(BattleDiceCardUI.GetClickableState))]
        public static void BattleDiceCardUI_GetClickableState(
          BattleDiceCardUI __instance,
          ref BattleDiceCardUI.ClickableState __result)
        {
            BattleUnitModel owner = __instance.CardModel.owner;
            if (owner == null || PassiveAbility_ShopPassiveMook9.HasPassive(owner) == null || __result != BattleDiceCardUI.ClickableState.NotEnoughCost)
                return;
            __result = BattleDiceCardUI.ClickableState.CanClick;
        } 
        */

        /// <summary>
        /// Harmony patch to handle Mook9
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(SpecialCardListModel), nameof(SpecialCardListModel.ReturnCardToHand))]
        public static bool SpecialCardListModel_ReturnCardToHand(
          SpecialCardListModel __instance,
          BattleUnitModel unit,
          BattleDiceCardModel appliedCard)
        {
            List<BattleDiceCardModel> fieldValue1 = ModdingUtils.GetFieldValue<List<BattleDiceCardModel>>("_cardInUse", __instance);
            List<BattleDiceCardModel> fieldValue2 = ModdingUtils.GetFieldValue<List<BattleDiceCardModel>>("_cardInReserved", __instance);
            List<BattleDiceCardModel> fieldValue3 = ModdingUtils.GetFieldValue<List<BattleDiceCardModel>>("_cardInHand", __instance);
            PassiveAbility_ShopPassiveMook9 shopPassiveMook9 = PassiveAbility_ShopPassiveMook9.HasPassive(unit);
            if (shopPassiveMook9 == null || !shopPassiveMook9.cards.ContainsKey(appliedCard))
                return true;
            unit.cardSlotDetail.ReserveCost(-(appliedCard.GetCost() - shopPassiveMook9.cards[appliedCard]));
            fieldValue1.Remove(appliedCard);
            fieldValue2.Remove(appliedCard);
            fieldValue3.Add(appliedCard);
            shopPassiveMook9.cards.Remove(appliedCard);
            return false;
        }

        /// <summary>
        /// Harmony patch to handle Mook9
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(BattlePersonalEgoCardDetail), nameof(BattlePersonalEgoCardDetail.ReturnCardToHand))]
        public static bool BattlePersonalEgoCardDetail_ReturnCardToHand(
          BattlePersonalEgoCardDetail __instance,
          BattleDiceCardModel appliedCard)
        {
            BattleUnitModel fieldValue1 = ModdingUtils.GetFieldValue<BattleUnitModel>("_self", __instance);
            List<BattleDiceCardModel> fieldValue2 = ModdingUtils.GetFieldValue<List<BattleDiceCardModel>>("_cardInUse", __instance);
            List<BattleDiceCardModel> fieldValue3 = ModdingUtils.GetFieldValue<List<BattleDiceCardModel>>("_cardInReserved", __instance);
            List<BattleDiceCardModel> fieldValue4 = ModdingUtils.GetFieldValue<List<BattleDiceCardModel>>("_cardInHand", __instance);
            PassiveAbility_ShopPassiveMook9 shopPassiveMook9 = PassiveAbility_ShopPassiveMook9.HasPassive(fieldValue1);
            if (shopPassiveMook9 == null || !shopPassiveMook9.cards.ContainsKey(appliedCard))
                return true;
            fieldValue1.cardSlotDetail.ReserveCost(-(appliedCard.GetCost() - shopPassiveMook9.cards[appliedCard]));
            fieldValue2.Remove(appliedCard);
            fieldValue3.Remove(appliedCard);
            fieldValue4.Add(appliedCard);
            shopPassiveMook9.cards.Remove(appliedCard);
            return false;
        }

        /// <summary>
        /// Harmony patch to handle Mook9
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(BattleAllyCardDetail), nameof(BattleAllyCardDetail.ReturnCardToHand))]
        public static bool BattleAllyCardDetail_ReturnCardToHand(
          BattleAllyCardDetail __instance,
          BattleDiceCardModel appliedCard)
        {
            BattleUnitModel fieldValue1 = ModdingUtils.GetFieldValue<BattleUnitModel>("_self", __instance);
            List<BattleDiceCardModel> fieldValue2 = ModdingUtils.GetFieldValue<List<BattleDiceCardModel>>("_cardInUse", __instance);
            List<BattleDiceCardModel> fieldValue3 = ModdingUtils.GetFieldValue<List<BattleDiceCardModel>>("_cardInReserved", __instance);
            List<BattleDiceCardModel> fieldValue4 = ModdingUtils.GetFieldValue<List<BattleDiceCardModel>>("_cardInHand", __instance);
            PassiveAbility_ShopPassiveMook9 shopPassiveMook9 = PassiveAbility_ShopPassiveMook9.HasPassive(fieldValue1);
            if (shopPassiveMook9 == null || !shopPassiveMook9.cards.ContainsKey(appliedCard))
                return true;
            fieldValue1.cardSlotDetail.ReserveCost(-(appliedCard.GetCost() - shopPassiveMook9.cards[appliedCard]));
            fieldValue2.Remove(appliedCard);
            fieldValue3.Remove(appliedCard);
            fieldValue4.Add(appliedCard);
            shopPassiveMook9.cards.Remove(appliedCard);
            return false;
        }

        /// <summary>
        /// Patch for handling Mook9
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(BattlePlayingCardSlotDetail), nameof(BattlePlayingCardSlotDetail.OnApplyCard))]
        public static bool BattlePlayingCardSlotDetail_OnApplyCard(
          BattlePlayingCardSlotDetail __instance,
          BattleDiceCardModel card,
          ref bool __result)
        {
            PassiveAbility_ShopPassiveMook9 shopPassiveMook9 = PassiveAbility_ShopPassiveMook9.HasPassive(ModdingUtils.GetFieldValue<BattleUnitModel>("_self", __instance));
            if (shopPassiveMook9 == null)
                return true;
            int cost = card.GetCost();
            if (__instance.ReservedPlayPoint + cost > __instance.GetMaxPlayPoint())
            {
                int num1 = __instance.GetMaxPlayPoint() - __instance.ReservedPlayPoint;
                int num2 = cost - num1;
                shopPassiveMook9.cards[card] = num2;
                __result = __instance.ReserveCost(num1);
                return false;
            }
            card.costSpended = false;
            __result = __instance.ReserveCost(cost);
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(BattleUnitBuf_burn), nameof(BattleUnitBuf_burn.OnRoundEnd))]
        public static bool BattleUnitBuf_burn_OnRoundEnd(BattleUnitBuf_burn __instance)
        {
            if (!LogLikeMod.CheckStage())
                return true;
            BattleUnitModel fieldValue = ModdingUtils.GetFieldValue<BattleUnitModel>("_owner", __instance);
            if (fieldValue.passiveDetail.PassiveList.Find(x => x is PassiveAbility_ShopPassiveStigma5) != null)
            {
                PassiveAbility_ShopPassiveStigma5 shopPassiveStigma5 = fieldValue.passiveDetail.PassiveList.Find(x => x is PassiveAbility_ShopPassiveStigma5) as PassiveAbility_ShopPassiveStigma5;
                if (shopPassiveStigma5.stack < __instance.stack)
                    shopPassiveStigma5.stack = __instance.stack;
            }
            if (Singleton<GlobalLogueEffectManager>.Instance.GetEffectList().Find(x => x is PickUpModel_ShopGoodStigma1.Stigma1Effect) == null)
                return true;
            if (!fieldValue.IsImmune(__instance.bufType))
            {
                int v = __instance.stack - fieldValue.bufListDetail.GetActivatedBufList().FindAll((Predicate<BattleUnitBuf>)(x => x is BattleUnitBuf_burnDown)).Count * 10;
                if (v < 0)
                    v = 0;
                fieldValue.TakeDamage(v, DamageType.Buf, keyword: __instance.bufType);
                __instance.GetType().GetMethod("PrintEffect", AccessTools.all).Invoke(__instance, (object[])null);
                if (fieldValue.bufListDetail.GetActivatedBuf(KeywordBuf.BurnBreak) != null)
                    fieldValue.TakeBreakDamage(v / 2, DamageType.Buf, keyword: __instance.bufType);
                if (fieldValue.faction == Faction.Enemy && fieldValue.IsDead())
                    Singleton<StageController>.Instance.GetStageModel().AddBurnKillCount();
            }
            __instance.stack = __instance.stack * 3 / 4;
            if (__instance.stack <= 0)
                __instance.Destroy();
            return false;
        }


        [HarmonyPrefix, HarmonyPatch(typeof(BattleUnitBufListDetail), nameof(BattleUnitBufListDetail.ChangeDiceResult))]
        public static void BattleUnitBufListDetail_ChangeDiceResult(
          BattleUnitBufListDetail __instance,
          BattleDiceBehavior behavior,
          ref int diceResult)
        {
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return;
            Singleton<GlobalLogueEffectManager>.Instance.ChangeDiceResult(behavior, ref diceResult);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(BattleUnitModel), nameof(BattleUnitModel.BeforeRollDice))]
        public static bool BattleUnitModel_BeforeRollDice(
          BattleUnitModel __instance,
          BattleDiceBehavior behavior)
        {
            // Outside RMR (or with zero global effects) skip singleton/list walk every dice.
            if (!LogLikeMod.CheckStage())
                return true;
            var mgr = Singleton<GlobalLogueEffectManager>.Instance;
            if (mgr == null || mgr.effects == null || mgr.effects.Count == 0)
                return true;
            mgr.BeforeRollDice(behavior);
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(BattlePlayingCardDataInUnitModel), nameof(BattlePlayingCardDataInUnitModel.OnUseCard))]
        public static bool BattlePlayingCardDataInUnitModel_OnUseCard(
          BattlePlayingCardDataInUnitModel __instance)
        {
            if (!LogLikeMod.CheckStage())
                return true;
            var mgr = Singleton<GlobalLogueEffectManager>.Instance;
            if (mgr == null || mgr.effects == null || mgr.effects.Count == 0)
                return true;
            mgr.OnUseCard(__instance);
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(BookPassiveInfo), "name", MethodType.Getter)]
        public static bool BookPassiveInfo_get_name(BookPassiveInfo __instance, ref string __result)
        {
            if (__instance?.passive == null)
                return true;
            // Origin-aware: mod package on vanilla passives used to miss CN PassiveDesc → empty/Hangul/tofu.
            // Reject Hangul-only hits so vanilla BookPassiveInfo can still try PassiveXmlInfo / origin.
            string name = RewardingModel.GetPassiveName(__instance.passive.id);
            if (string.IsNullOrEmpty(name) || IsPoorUiText(name))
                return true;
            __result = name;
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(BookPassiveInfo), "desc", MethodType.Getter)]
        public static bool BookPassiveInfo_get_desc(BookPassiveInfo __instance, ref string __result)
        {
            if (__instance?.passive == null)
                return true;
            string desc = RewardingModel.GetPassiveDesc(__instance.passive.id);
            if (string.IsNullOrEmpty(desc) || IsPoorUiText(desc))
                return true;
            __result = desc;
            return false;
        }

        /// <summary>
        /// BookModel.GetName → BookXmlInfo.Name returns InnerName for workshop books (often Hangul
        /// leftovers that render as 口口口 on CN TMP faces). Prefer origin-aware BookDesc whenever
        /// the current name is poor — including library view with RMR loaded (not only RMR runs).
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(BookModel), nameof(BookModel.GetName))]
        public static void BookModel_GetName(BookModel __instance, ref string __result)
        {
            if (__instance?.ClassInfo == null)
                return;
            // Always repair poor/Hangul/tofu names when RMR is loaded (library + prepare + run).
            if (!IsPoorUiText(__result))
                return;
            string fixedName = RewardingModel.GetLocalizedBookName(__instance.ClassInfo);
            if (!string.IsNullOrEmpty(fixedName) && !IsPoorUiText(fixedName))
            {
                __result = fixedName;
                return;
            }
            LogPoorBookNameOnce("GetName", __instance, __result, fixedName);
        }

        /// <summary>
        /// Character name under the equip portrait (口口口) — same Hangul/InnerName path as GetName.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(BookModel), nameof(BookModel.GetCharacterName))]
        public static void BookModel_GetCharacterName(BookModel __instance, ref string __result)
        {
            if (__instance?.ClassInfo == null)
                return;
            if (!IsPoorUiText(__result))
                return;
            string fixedName = RewardingModel.GetLocalizedBookName(__instance.ClassInfo);
            if (!string.IsNullOrEmpty(fixedName) && !IsPoorUiText(fixedName))
            {
                __result = fixedName;
                return;
            }
            LogPoorBookNameOnce("GetCharacterName", __instance, __result, fixedName);
        }

        private static readonly HashSet<string> _loggedPoorBookNames = new HashSet<string>();

        private static void LogPoorBookNameOnce(string where, BookModel book, string original, string attempted)
        {
            try
            {
                LorId id = book?.ClassInfo?.id;
                if (id == null || id == LorId.None)
                    return;
                string key = where + ":" + (id.packageId ?? "") + ":" + id.id;
                if (!_loggedPoorBookNames.Add(key))
                    return;
                string inner = book.ClassInfo.InnerName ?? "";
                string textId = book.ClassInfo.TextId.ToString();
                Debug.LogWarning(
                    $"[RMR Localize] Poor book name still unresolved ({where}) id={id.packageId}:{id.id} "
                    + $"TextId={textId} orig='{TruncateForLog(original)}' fixed='{TruncateForLog(attempted)}' "
                    + $"InnerName='{TruncateForLog(inner)}'");
            }
            catch { /* ignore */ }
        }

        private static string TruncateForLog(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            s = s.Replace("\n", "\\n").Replace("\r", "");
            return s.Length <= 48 ? s : s.Substring(0, 48) + "…";
        }

        private static bool IsPoorUiText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return true;
            if (RewardingModel.IsPoorDisplayNamePublic(text))
                return true;
            if (LooksLikeHangulOnly(text))
                return true;
            if (LooksLikeTofuBoxes(text))
                return true;
            return false;
        }

        private static bool LooksLikeTofuBoxes(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            int tofu = 0, total = 0;
            foreach (char ch in text)
            {
                if (char.IsWhiteSpace(ch))
                    continue;
                total++;
                if (ch == '\u53E3' || ch == '\u25A1' || ch == '\uFFFD' || ch == '\u2610')
                    tofu++;
            }
            return total > 0 && tofu * 2 >= total;
        }

        private static bool LooksLikeHangulOnly(string text)
        {
            if (string.IsNullOrEmpty(text))
                return true;
            int hangul = 0, han = 0;
            foreach (char ch in text)
            {
                if (ch >= 0xAC00 && ch <= 0xD7A3) hangul++;
                else if (ch >= 0x4E00 && ch <= 0x9FFF) han++;
            }
            return hangul > 0 && han == 0;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(BattleEmotionRewardInfoUI), nameof(BattleEmotionRewardInfoUI.SetData))]
        public static bool BattleEmotionRewardInfoUI_SetData(BattleEmotionRewardInfoUI __instance)
        {
            List<BattleEmotionRewardSlotUI> fieldValue = LogLikeMod.GetFieldValue<List<BattleEmotionRewardSlotUI>>(__instance, "slots");
            if (fieldValue.Count < 10)
            {
                while (fieldValue.Count < 10)
                {
                    BattleEmotionRewardSlotUI emotionRewardSlotUi = LogLikeMod.LogBattleEmotionRewardSlotUI.BattleEmotionRewardSlotUI_Copying(fieldValue[0]);
                    fieldValue.Add(emotionRewardSlotUi);
                }
            }
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(BattleEmotionCoinUI), nameof(BattleEmotionCoinUI.Init))]
        public static bool BattleEmotionCoinUI_Init(BattleEmotionCoinUI __instance)
        {
            if (__instance.enermy.Length < 10)
            {
                List<Vector2> vector2List = new List<Vector2>()
                  {
                    new Vector2(121f, 410f),
                    new Vector2(121f, 480f),
                    new Vector2(121f, 550f),
                    new Vector2(121f, 620f),
                    new Vector2(121f, 690f)
                  };
                int index = 0;
                while (__instance.enermy.Length < 10)
                {
                    BattleEmotionCoinUI.BattleEmotionCoinData battleEmotionCoinData = __instance.enermy[0];
                    battleEmotionCoinData.target = UnityEngine.Object.Instantiate<RectTransform>(battleEmotionCoinData.target, battleEmotionCoinData.target.parent);
                    battleEmotionCoinData.target.localPosition = (Vector3)vector2List[index];
                    __instance.enermy = __instance.enermy.AddToArray<BattleEmotionCoinUI.BattleEmotionCoinData>(battleEmotionCoinData);
                    ++index;
                }
            }
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIBattleSettingWaveList), nameof(UIBattleSettingWaveList.SetData))]
        public static bool UIBattleSettingWaveList_SetData(
          UIBattleSettingWaveList __instance,
          StageModel stage)
        {
            try
            {
                if (__instance.gameObject.GetComponent<ScrollRect>() == null)
                {
                    List<Transform> transformList = new List<Transform>();
                    Texture2D texture2D = new Texture2D(2, 2);
                    texture2D.LoadImage(File.ReadAllBytes(Application.dataPath + "/Managed/Image/Mask.png"));
                    Sprite sprite = Sprite.Create(texture2D, new Rect(0.0f, 0.0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.5f, 0.5f));
                    Image image = __instance.gameObject.AddComponent<Image>();
                    image.sprite = sprite;
                    image.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
                    (__instance.transform as RectTransform).sizeDelta = new Vector2(400f, (float)((double)stage.waveList.Count * 250.0 / 3.0));
                    ScrollRect scrollRect = __instance.gameObject.AddComponent<ScrollRect>();
                    scrollRect.scrollSensitivity = 15f;
                    scrollRect.content = __instance.transform as RectTransform;
                    scrollRect.horizontal = false;
                    scrollRect.movementType = ScrollRect.MovementType.Elastic;
                    scrollRect.elasticity = 0.1f;
                }
                if (stage.waveList.Count > __instance.waveSlots.Count)
                {
                    int num = stage.waveList.Count - __instance.waveSlots.Count;
                    for (int index = 0; index < num; ++index)
                    {
                        UIBattleSettingWaveSlot slot = UnityEngine.Object.Instantiate<UIBattleSettingWaveSlot>(__instance.waveSlots[0], __instance.waveSlots[0].transform.parent);
                        LogLikeRoutines.InitUIBattleSettingWaveSlot(slot, __instance);
                        List<UIBattleSettingWaveSlot> battleSettingWaveSlotList = new List<UIBattleSettingWaveSlot>();
                        battleSettingWaveSlotList.Add(slot);
                        battleSettingWaveSlotList.AddRange((IEnumerable<UIBattleSettingWaveSlot>)__instance.waveSlots);
                        __instance.waveSlots = battleSettingWaveSlotList;
                    }
                }
                if (stage.waveList.Count < __instance.waveSlots.Count)
                {
                    int num = __instance.waveSlots.Count - stage.waveList.Count;
                    for (int index = 0; index < num && __instance.waveSlots.Count != 5; ++index)
                    {
                        UIBattleSettingWaveSlot waveSlot = __instance.waveSlots[__instance.waveSlots.Count - 1];
                        __instance.waveSlots.Remove(waveSlot);
                        UnityEngine.Object.DestroyImmediate(waveSlot);
                    }
                }
                LogLikeRoutines.InitUIBattleSettingWaveSlots(__instance.waveSlots, __instance);
                foreach (Component waveSlot in __instance.waveSlots)
                    waveSlot.gameObject.SetActive(false);
                for (int index = 0; index < stage.waveList.Count; ++index)
                {
                    UIBattleSettingWaveSlot waveSlot = __instance.waveSlots[index];
                    waveSlot.SetData(stage.waveList[index]);
                    waveSlot.gameObject.SetActive(true);
                    if (stage.waveList[index].IsUnavailable())
                        waveSlot.SetDefeat();
                    if (index == stage.waveList.Count - 1)
                        waveSlot.ActivateArrow(false);
                }
                int index1 = Singleton<StageController>.Instance.CurrentWave - 1;
                if (index1 < 0 || __instance.waveSlots.Count <= index1)
                    Debug.LogError("Index Error");
                else
                    __instance.waveSlots[index1].SetHighlighted();
            }
            catch
            {
            }
          (__instance.transform as RectTransform).sizeDelta = new Vector2(400f, (float)((double)stage.waveList.Count * 250.0 / 3.0));
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(CharacterAppearance), nameof(CharacterAppearance.ChangeMotion))]
        public static bool CharacterAppearance_ChangeMotion_prefix(ActionDetail detail, ActionDetail __state)
        {
            __state = detail;
            return true;
        }

        // Patch to make upgraded combat pages consult their original PackageIds for combat page art
        [HarmonyPrefix, HarmonyPatch(typeof(CustomizingCardArtworkLoader), nameof(CustomizingCardArtworkLoader.GetSpecificArtworkSprite))]
        public static void CustomizingCardArtworkLoader_GetSpecificArtworkSprite(
          ref string id,
          string name)
        {
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return;
            if (id.Contains(LogCardUpgradeManager.UpgradeKeyword))
                id = UpgradeMetadata.UnpackPidUnsafe(id).actualPid;
        }

        #endregion

        #region POSTFIXES 

        [HarmonyPostfix, HarmonyPatch(typeof(BattleUnitBuf), nameof(BattleUnitBuf.Destroy))]
        public static void BattleUnitBuf_Destroy(BattleUnitBuf __instance)
        {
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return;
            BattleUnitModel fieldValue = ModdingUtils.GetFieldValue<BattleUnitModel>("_owner", __instance);
            if (!(__instance is BattleUnitBuf_burn) || fieldValue.passiveDetail.PassiveList.Find(x => x is PassiveAbility_ShopPassiveStigma5) == null)
                return;
            (fieldValue.passiveDetail.PassiveList.Find(x => x is PassiveAbility_ShopPassiveStigma5) as PassiveAbility_ShopPassiveStigma5).Recovering();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(BattleDiceCardUI), nameof(BattleDiceCardUI.GetClickableState))]
        public static void BattleDiceCardUI_GetClickableState(
          BattleDiceCardUI __instance,
          ref BattleDiceCardUI.ClickableState __result)
        {
            BattleUnitModel owner = __instance.CardModel.owner;
            if (owner == null || PassiveAbility_ShopPassiveMook9.HasPassive(owner) == null || __result != BattleDiceCardUI.ClickableState.NotEnoughCost)
                return;
            __result = BattleDiceCardUI.ClickableState.CanClick;
        }
        [HarmonyPostfix, HarmonyPatch(typeof(BattleUnitPassiveDetail), nameof(BattleUnitPassiveDetail.OnKill))]
        public static void BattleUnitPassiveDetail_OnKill(
          BattleUnitPassiveDetail __instance,
          BattleUnitModel target)
        {
            Singleton<GlobalLogueEffectManager>.Instance.OnKillUnit(ModdingUtils.GetFieldValue<BattleUnitModel>("_self", __instance), target);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(BattleUnitPassiveDetail), nameof(BattleUnitPassiveDetail.OnDie))]
        public static void BattleUnitPassiveDetail_OnDie(BattleUnitPassiveDetail __instance)
        {
            Singleton<GlobalLogueEffectManager>.Instance.OnDieUnit(ModdingUtils.GetFieldValue<BattleUnitModel>("_self", __instance));
        }

        /// <summary>
        /// Postfix patch for Union2's effect
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(BattleUnitBufListDetail), nameof(BattleUnitBufListDetail.CheckGift))]
        public static void BattleUnitBufListDetail_CheckGift(
          BattleUnitBufListDetail __instance,
          KeywordBuf bufType,
          int stack,
          BattleUnitModel actor)
        {
            if (bufType != KeywordBuf.Bleeding || actor.passiveDetail.PassiveList.Find(x => x is PassiveAbility_ShopPassiveUnion2) == null)
                return;
            ModdingUtils.GetFieldValue<BattleUnitModel>("_self", __instance).TakeDamage(stack);
        }

        /// <summary>
        /// Postfix patch to hook GlobalLogueEffectBase.DmgFactor
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(BattleUnitPassiveDetail), nameof(BattleUnitPassiveDetail.DmgFactor))]
        public static void BattleUnitPassiveDetail_DmgFactor(
          BattleUnitPassiveDetail __instance,
          ref float __result,
          int dmg,
          DamageType type = DamageType.ETC,
          KeywordBuf keyword = KeywordBuf.None)
        {
            __result = Singleton<GlobalLogueEffectManager>.Instance.DmgFactor(ModdingUtils.GetFieldValue<BattleUnitModel>("_self", __instance), dmg, type, keyword);
        }

        /// <summary>
        /// Postfix patch to set abnormality combat page arts
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(EmotionPassiveCardUI), nameof(EmotionPassiveCardUI.SetSprites))]
        public static void EmotionPassiveCardUI_SetSprites(EmotionPassiveCardUI __instance)
        {
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return;
            Image image = (Image)typeof(EmotionPassiveCardUI).GetField("_artwork", AccessTools.all).GetValue(__instance);
            EmotionCardXmlInfo emotionCardXmlInfo = (EmotionCardXmlInfo)typeof(EmotionPassiveCardUI).GetField("_card", AccessTools.all).GetValue(__instance);
            if (image == null || emotionCardXmlInfo == null)
                return;
            RewardPassiveInfo rewardInfo = RewardingModel.FindRewardInfo(emotionCardXmlInfo);
            if (rewardInfo != null && rewardInfo.rewardtype == RewardType.EquipPage)
            {
                BookXmlInfo book = RewardingModel.GetBookDataOriginAware(rewardInfo.id);
                if (book != null)
                {
                    Sprite thumb = new BookModel(book).GetThumbSprite();
                    if (thumb != null)
                    {
                        image.sprite = thumb;
                        return;
                    }
                }
            }
            if (LogLikeMod.ArtWorks.ContainsKey(emotionCardXmlInfo.Artwork))
                image.sprite = LogLikeMod.ArtWorks[emotionCardXmlInfo.Artwork];
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIOptionWindow), nameof(UIOptionWindow.Open))]
        public static void UIOptionWindow_Open(UIOptionWindow __instance)
        {
            if (LogLikeMod.DefFont == null)
            {
                LogLikeMod.DefFont = UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf");
                LogLikeMod.DefFontColor = UIColorManager.Manager.GetUIColor(UIColor.Default);
            }
            // Prefer language-aware TMP resolve (Noto CJK). Dropdown faces are often Latin-only
            // and would paint Chinese as tofu if forced into DefFont_TMP — never assign them.
            LogLikeMod.InvalidateTmpFontCache();
            LogLikeMod.EnsureLocalizedFonts("UIOptionWindow.Open", repairActiveUi: true);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIBattleSettingEditPanel), nameof(UIBattleSettingEditPanel.Close))]
        public static void UIBattleSettingEditPanel_Close()
        {
            // Unconditional: the forced orders belong to the prepare screen, and leaving them
            // applied is what leaked into battle and buried the post-battle reward Skip button.
            // Runs even outside a roguelike run so a stray raise can never survive the screen.
            try { RestoreForcedCanvasOrders(); } catch { /* ignore */ }
            _lastPrepareEditPanel = null;
            if (!LogLikeMod.CheckStage())
                return;
            LoguePlayDataSaver.SavePlayData_Menu();
        }

        /*
        [HarmonyPostfix, HarmonyPatch(typeof(BattleDiceCardUI), nameof(BattleDiceCardUI.SetCard))]
        public static void BattleDiceCardUI_SetCard(
          BattleDiceCardUI __instance,
          BattleDiceCardModel cardModel)
        {
            if (!LogLikeMod.CheckStage() || !LogLikeMod.ArtWorks.ContainsKey(cardModel.GetArtworkSrc()))
                return;
            __instance.img_artwork.sprite = LogLikeMod.ArtWorks[cardModel.GetArtworkSrc()];
        }
        */

        /*
        [HarmonyPostfix, HarmonyPatch(typeof(UIOriginCardSlot), nameof(UIOriginCardSlot.SetData))]
        public static void UIOriginCardSlot_SetData(
          UIOriginCardSlot __instance,
          DiceCardItemModel cardmodel)
        {
            if (!LogLikeMod.CheckStage())
                return;
            Image image = (Image)typeof(UIOriginCardSlot).GetField("img_Artwork", AccessTools.all).GetValue(__instance);
            bool flag = cardmodel.GetID().packageId == LogLikeMod.ModId;
            if (LogLikeMod.ArtWorks.ContainsKey(cardmodel.GetArtworkSrc()) & flag)
            {
                image.sprite = LogLikeMod.ArtWorks[cardmodel.GetArtworkSrc()];
            }
        }
        */

        [HarmonyPostfix, HarmonyPatch(typeof(EmotionPassiveCardUI), nameof(EmotionPassiveCardUI.Init))]
        public static void EmotionPassiveCardUI_Init(EmotionPassiveCardUI __instance)
        {
            if (!LogLikeMod.CheckStage())
                return;
            bool fieldValue = LogLikeMod.GetFieldValue<bool>(__instance, "_isForceOpen");
            if (LogLikeMod.ChangeEmotinCardBtn == null)
            {
                LogLikeMod.ChangeEmotinCardBtn = ModdingUtils.CreateButton(SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.selectedEmotionCardBg.transform.parent.parent, "AbCardSelection_Skip", new Vector2(1f, 1f), new Vector2(0.0f, -480f));
                LogLikeMod.ChangeEmotinCardBtn.onClick.AddListener(() => LogLikeRoutines.ChangeEPCUTransform(__instance));
                ModdingUtils.CreateText_TMP(LogLikeMod.ChangeEmotinCardBtn.transform, new Vector2(-30f, 0.0f), 40, new Vector2(0.0f, 0.0f), new Vector2(1f, 1f), new Vector2(0.0f, 0.0f), TextAlignmentOptions.Midline, LogLikeMod.DefFontColor, LogLikeMod.DefFont_TMP).text = TextDataModel.GetText("ui_EmotionPositionChange");
            }
            LogLikeMod.ChangeEmotinCardBtn.gameObject.SetActive(fieldValue);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(BookInventoryModel), nameof(BookInventoryModel.GetAllBookByInstanceId))]
        public static void BookInventoryModel_GetAllBookByInstanceId(
          ref BookModel __result,
          int bookInstanceId)
        {
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return;
            BookModel bookModel = FindRoguelikeBookByInstanceId(bookInstanceId);
            if (bookModel != null)
                __result = bookModel;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(BookInventoryModel), nameof(BookInventoryModel.GetBookByInstanceId))]
        public static void BookInventoryModel_GetBookByInstanceId(
          ref BookModel __result,
          int bookInstanceId)
        {
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return;
            BookModel bookModel = FindRoguelikeBookByInstanceId(bookInstanceId);
            if (bookModel != null)
                __result = bookModel;
        }

        private static BookModel FindRoguelikeBookByInstanceId(int bookInstanceId)
        {
            BookModel inventoryBook = LogueBookModels.booklist?
                .Find(book => book != null && book.instanceId == bookInstanceId);
            if (inventoryBook != null)
                return inventoryBook;
            UnitDataModel unit = LogueBookModels.playerModel?
                .Find(model => model?.bookItem != null && model.bookItem.instanceId == bookInstanceId);
            return unit?.bookItem;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(BookInventoryModel), nameof(BookInventoryModel.GetBookListAll))]
        public static void BookInventoryModel_GetBookListAll(ref List<BookModel> __result)
        {
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return;
            List<BookModel> bookModelList = new List<BookModel>();
            if (LogueBookModels.playerModel != null)
            {
                foreach (UnitDataModel unitDataModel in LogueBookModels.playerModel)
                {
                    if (unitDataModel?.defaultBook != null)
                        bookModelList.Add(unitDataModel.defaultBook);
                }
            }
            if (LogueBookModels.booklist != null)
                bookModelList.AddRange(LogueBookModels.booklist.Where(book => book != null));
            __result = bookModelList;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(BookInventoryModel), nameof(BookInventoryModel.GetBookList_PassiveEquip))]
        public static void BookInventoryModel_GetBookList_PassiveEquip(
          ref List<BookModel> __result,
          BookModel booktobeEquiped)
        {
            // A Floor Realization is a vanilla stage id, so CheckStage() is false even
            // though RMR has replaced booklist with the permanent Compendium projection.
            // Falling through here exposes the entire vanilla library as passive sources.
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return;
            List<BookModel> bookModelList1 = new List<BookModel>();
            List<BookModel> bookModelList2 =
                RMRPrepareRestrictions.FilterEquipInventoryBooks(LogueBookModels.booklist);
            foreach (BookModel bookModel in bookModelList2)
            {
                if (bookModel.owner == null && bookModel.GetPassiveInfoList().Count != 0)
                    bookModelList1.Add(bookModel);
            }
            __result = bookModelList1;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CharacterAppearance), nameof(CharacterAppearance.ChangeMotion))]
        public static void CharacterAppearance_ChangeMotion(
          CharacterAppearance __instance,
          ActionDetail detail,
          ActionDetail __state)
        {
            if (LogLikeMod.Temp)
            {
                for (int index = 0; index < SingletonBehavior<UICharacterRenderer>.Instance.cameraList.Count; ++index)
                {
                    Camera camera = SingletonBehavior<UICharacterRenderer>.Instance.cameraList[index];
                    camera.cullingMask = -1;
                    if (camera.gameObject.GetComponent("CameraRender") == null)
                        camera.gameObject.AddComponent<CameraRender>().index = index;
                }
                LogLikeMod.Temp = false;
            }
            WorkshopSkinDataSetter setter = __instance.GetComponent<WorkshopSkinDataSetter>();
            if (setter == null)
                return;
            WorkshopSkinData workshopSkinData = Singleton<CustomizingBookSkinLoader>.Instance.GetWorkshopBookSkinData(LogLikeMod.ModId).Find((Predicate<WorkshopSkinData>)(x => x.dic == setter.dic));
            if (workshopSkinData == null)
                return;
            string dataName = workshopSkinData.dataName;
            if (dataName == string.Empty)
                return;
            SpineStandingData spineStandingData = (SpineStandingData)null;
            if (LogLikeMod.spinedatas.TryGetValue(dataName, out spineStandingData))
            {
                if (spineStandingData.AnimDic.ContainsKey(__state))
                    detail = __state;
                if (spineStandingData.AnimDic.ContainsKey(detail))
                {
                    if (!LogLikeMod.spinemotions.ContainsKey(dataName))
                        LogLikeMod.spinemotions.Add(dataName, new Dictionary<ActionDetail, Dictionary<GameObject, SkeletonAnimation>>());
                    if (!LogLikeMod.spinemotions[dataName].ContainsKey(detail))
                        LogLikeMod.spinemotions[dataName].Add(detail, new Dictionary<GameObject, SkeletonAnimation>());
                    if (!LogLikeMod.spinemotions[dataName][detail].ContainsKey(__instance.gameObject))
                    {
                        SkeletonAnimation skeletonAnimation = SkeletonRenderer.NewSpineGameObject<SkeletonAnimation>(spineStandingData.asset);
                        __instance.AddChild(skeletonAnimation.gameObject);
                        skeletonAnimation.gameObject.transform.localScale = new Vector3(1f, 1f);
                        LogLikeMod.spinemotions[dataName][detail].Add(__instance.gameObject, skeletonAnimation);
                    }
                    LogLikeMod.spinemotions[dataName][detail][__instance.gameObject].gameObject.SetActive(true);
                    LogLikeMod.spinemotions[dataName][detail][__instance.gameObject].gameObject.transform.localPosition = new Vector3(0.0f, 0.0f);
                    LogLikeMod.spinemotions[dataName][detail][__instance.gameObject].gameObject.transform.localScale = spineStandingData.AnimScale[detail];
                    LogLikeMod.spinemotions[dataName][detail][__instance.gameObject].state.SetAnimation(0, spineStandingData.AnimDic[detail], spineStandingData.AnimLoop[detail]);
                    LogLikeMod.spinemotions[dataName][detail][__instance.gameObject].state.TimeScale = spineStandingData.AnimSpeed[detail];
                    if (UIPanel.Controller.GetUIPanel(UIPanelType.CharacterList).IsActivated || UIPanel.Controller.GetUIPanel(UIPanelType.CharacterList_Right).IsActivated)
                        LogLikeMod.spinemotions[dataName][detail][__instance.gameObject].gameObject.transform.localPosition = new Vector3(0.0f, 0.0f, 10f);
                    //else if (detail == ActionDetail.Standing)
                    //  ;
                }
            }
            if (!LogLikeMod.spinemotions.ContainsKey(dataName))
                return;
            foreach (KeyValuePair<ActionDetail, Dictionary<GameObject, SkeletonAnimation>> keyValuePair in LogLikeMod.spinemotions[dataName])
            {
                if (keyValuePair.Key != detail && keyValuePair.Value.ContainsKey(__instance.gameObject))
                    keyValuePair.Value[__instance.gameObject].gameObject.SetActive(false);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UILibrarianEquipInfoSlot), nameof(UILibrarianEquipInfoSlot.SetData))]
        public static void UILibrarianEquipInfoSlot_SetData(
          UILibrarianEquipInfoSlot __instance,
          BookPassiveInfo passive)
        {
            if (passive?.passive == null || __instance == null)
                return;
            // B3: hide money-placeholder passive ("当前拥有的眼") on equip info panel.
            if (RMRPrepareRestrictions.IsMoneyPlaceholderPassive(passive.passive.id))
            {
                if (__instance.txt_cost != null)
                    __instance.txt_cost.text = "";
                if (__instance.euipName != null)
                    __instance.euipName.text = "";
                if (__instance.euipDesc != null)
                    __instance.euipDesc.text = "";
                __instance.gameObject.SetActive(false);
                return;
            }
            __instance.gameObject.SetActive(true);

            // Force-rewrite equip passive name/desc after vanilla SetData.
            // Getter patches alone still leave Hangul/tofu when UI cached text or workshop
            // PassiveXmlInfo fields win the race over PassiveDescXmlList.
            try
            {
                LorId pid = passive.passive.id;
                string goodName = RewardingModel.GetPassiveName(pid);
                string goodDesc = RewardingModel.GetPassiveDesc(pid);

                if (!string.IsNullOrEmpty(goodName) && !IsPoorUiText(goodName))
                {
                    if (__instance.euipName != null)
                        __instance.euipName.text = goodName;
                    TrySetTmpField(__instance, "txt_euipName", goodName);
                }
                else
                {
                    LogPoorPassiveOnce("equipSlot.name", pid, __instance.euipName != null ? __instance.euipName.text : null, goodName);
                }

                if (!string.IsNullOrEmpty(goodDesc) && !IsPoorUiText(goodDesc))
                {
                    if (__instance.euipDesc != null)
                        __instance.euipDesc.text = goodDesc;
                    TrySetTmpField(__instance, "txt_euipDesc", goodDesc);
                }
                else
                {
                    LogPoorPassiveOnce("equipSlot.desc", pid, __instance.euipDesc != null ? __instance.euipDesc.text : null, goodDesc);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR Localize] UILibrarianEquipInfoSlot force rewrite failed: " + ex.Message);
            }
        }

        private static readonly HashSet<string> _loggedPoorPassives = new HashSet<string>();

        private static void LogPoorPassiveOnce(string where, LorId pid, string original, string attempted)
        {
            try
            {
                if (pid == null || pid == LorId.None)
                    return;
                string key = where + ":" + (pid.packageId ?? "") + ":" + pid.id;
                if (!_loggedPoorPassives.Add(key))
                    return;
                Debug.LogWarning(
                    $"[RMR Localize] Poor passive text ({where}) id={pid.packageId}:{pid.id} "
                    + $"ui='{TruncateForLog(original)}' resolved='{TruncateForLog(attempted)}'");
            }
            catch { /* ignore */ }
        }

        private static void TrySetTmpField(object target, string fieldName, string value)
        {
            if (target == null || string.IsNullOrEmpty(fieldName) || value == null)
                return;
            try
            {
                FieldInfo fi = AccessTools.Field(target.GetType(), fieldName);
                if (fi == null)
                    return;
                object obj = fi.GetValue(target);
                if (obj == null)
                    return;
                // TextMeshProUGUI / Text both expose .text
                PropertyInfo textProp = obj.GetType().GetProperty("text");
                if (textProp != null && textProp.CanWrite)
                    textProp.SetValue(obj, value, null);
            }
            catch { /* optional field */ }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(BattleUnitModel), nameof(BattleUnitModel.OnDie))]
        public static void BattleUnitModel_OnDie(BattleUnitModel __instance)
        {
            if (!LogLikeMod.CheckStage(true))
                return;
            if (__instance.faction == Faction.Enemy && __instance.UnitData.unitData.ExpDrop > 0)
            {
                int moneyDrop = __instance.UnitData.unitData.ExpDrop;
                if (LogLikeMod.curstagetype == StageType.Normal)
                {
                    float moneyScale = 1f;
                    switch (LogLikeMod.curchaptergrade)
                    {
                        case ChapterGrade.Grade1:
                        case ChapterGrade.Grade2:
                            moneyScale = 1.5f;
                            break;
                        case ChapterGrade.Grade3:
                            moneyScale = 0.9f;
                            break;
                        case ChapterGrade.Grade4:
                            moneyScale = 0.85f;
                            break;
                        case ChapterGrade.Grade5:
                            moneyScale = 0.8f;
                            break;
                        case ChapterGrade.Grade6:
                            moneyScale = 0.75f;
                            break;
                        case ChapterGrade.Grade7:
                            moneyScale = 0.7f;
                            break;
                    }
                    moneyDrop = Mathf.Max(1, Mathf.RoundToInt(moneyDrop * moneyScale));
                }
                PassiveAbility_MoneyCheck.AddMoney(moneyDrop);
            }
            else if (__instance.faction == Faction.Player && LogueBookModels.playerBattleModel.Contains(__instance.UnitData))
                LogueBookModels.playerBattleModel.Find(x => x == __instance.UnitData).isDead = true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(BookPassiveInfo), "desc", MethodType.Getter)]
        public static void BookPassiveInfo_get_desc_postfix(
          BookPassiveInfo __instance,
          ref string __result)
        {
            // B3: do not surface money counter as a fake passive description on equip UI.
            if (__instance?.passive != null
                && RMRPrepareRestrictions.IsMoneyPlaceholderPassive(__instance.passive.id))
            {
                __result = string.Empty;
                return;
            }
            // Vanilla / workshop path may still return Hangul after prefix let it through.
            if (__instance?.passive == null || !IsPoorUiText(__result))
                return;
            string desc = RewardingModel.GetPassiveDesc(__instance.passive.id);
            if (!string.IsNullOrEmpty(desc) && !IsPoorUiText(desc))
                __result = desc;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(BookPassiveInfo), "name", MethodType.Getter)]
        public static void BookPassiveInfo_get_name_postfix(
          BookPassiveInfo __instance,
          ref string __result)
        {
            if (__instance?.passive != null
                && RMRPrepareRestrictions.IsMoneyPlaceholderPassive(__instance.passive.id))
            {
                __result = string.Empty;
                return;
            }
            if (__instance?.passive == null || !IsPoorUiText(__result))
                return;
            string name = RewardingModel.GetPassiveName(__instance.passive.id);
            if (!string.IsNullOrEmpty(name) && !IsPoorUiText(name))
                __result = name;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(LocalizedTextLoader), nameof(LocalizedTextLoader.LoadOthers))]
        public static void LocalizedTextLoader_LoadOthers(string language)
        {
            ApplyVanillaLocalizeForLanguage(language, "LoadOthers");
        }

        /// <summary>Language of the last full vanilla-localize pass, so a missed pass can be caught up.</summary>
        private static string _lastVanillaLocalizeLanguage;

        #region --- Replaying panel population after a language change ---

        // Vanilla panels copy localized strings into their TMP labels at populate time, and a
        // language switch does not rebuild a panel that is already on screen.
        //
        // The probe settled this: at Invitation.OpenInit it logged table=True (the vanilla
        // abnormality table already held the NEW language) while the list on screen still showed the
        // OLD one -- and the hover preview, which reads that same table live, was correct. Same
        // table, two different languages on one screen. So the tables were never the problem for
        // those labels; the already-built UI was stale.
        //
        // Chasing more tables could not have fixed this. Remember what each panel was populated
        // with, and replay that call once the resync is done.

        private static UIAbnormalityPanel _lastAbnormalityPanel;
        private static LibraryFloorModel _lastAbnormalityPanelFloor;

        private static readonly List<KeyValuePair<UICharacterSlot, UnitDataModel>> _characterSlotData =
            new List<KeyValuePair<UICharacterSlot, UnitDataModel>>();

        [HarmonyPostfix, HarmonyPatch(typeof(UIAbnormalityPanel), nameof(UIAbnormalityPanel.SetData))]
        public static void UIAbnormalityPanel_SetData_Remember(UIAbnormalityPanel __instance, LibraryFloorModel floor)
        {
            _lastAbnormalityPanel = __instance;
            _lastAbnormalityPanelFloor = floor;
        }

        // Parameter name must be exactly "unitData": Harmony injects by name, and calling it "unit"
        // made CreateAndPatchAll abort the whole patch class with "IL Compile Error".
        [HarmonyPostfix, HarmonyPatch(typeof(UICharacterSlot), nameof(UICharacterSlot.SetCharacter))]
        public static void UICharacterSlot_SetCharacter_Remember(UICharacterSlot __instance, UnitDataModel unitData)
        {
            if (__instance == null)
                return;
            try
            {
                for (int i = _characterSlotData.Count - 1; i >= 0; i--)
                {
                    // Drop destroyed slots while we are here; this list must not grow forever.
                    if (_characterSlotData[i].Key == null)
                    {
                        _characterSlotData.RemoveAt(i);
                        continue;
                    }
                    if (ReferenceEquals(_characterSlotData[i].Key, __instance))
                        _characterSlotData.RemoveAt(i);
                }
                _characterSlotData.Add(new KeyValuePair<UICharacterSlot, UnitDataModel>(__instance, unitData));
            }
            catch { /* remembering is best-effort */ }
        }

        /// <summary>
        /// Re-populate panels that are still on screen, so their labels pick up the new language.
        /// Only touches live, active objects; a panel that is closed will repopulate itself anyway.
        /// </summary>
        private static void ReplayPanelPopulationForLanguage()
        {
            bool abnormalityPanelReplayed = false;
            try
            {
                if (_lastAbnormalityPanel != null
                    && _lastAbnormalityPanelFloor != null
                    && _lastAbnormalityPanel.gameObject != null
                    && _lastAbnormalityPanel.gameObject.activeInHierarchy)
                {
                    _lastAbnormalityPanel.SetData(_lastAbnormalityPanelFloor);
                    abnormalityPanelReplayed = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR Localize] abnormality panel replay failed: " + ex.Message);
            }

            int slots = 0;
            for (int i = _characterSlotData.Count - 1; i >= 0; i--)
            {
                KeyValuePair<UICharacterSlot, UnitDataModel> entry = _characterSlotData[i];
                try
                {
                    if (entry.Key == null)
                    {
                        _characterSlotData.RemoveAt(i);
                        continue;
                    }
                    if (entry.Value == null || !entry.Key.gameObject.activeInHierarchy)
                        continue;
                    entry.Key.SetCharacter(entry.Value);
                    slots++;
                }
                catch { /* one bad slot must not stop the rest */ }
            }
            Debug.Log($"[RMR Localize] Replayed panel population after localization refresh: "
                + $"abnormalityPanel={abnormalityPanelReplayed}, characterSlots={slots}.");
        }

        #endregion

        /// <summary>
        /// Re-stamp every vanilla text table RMR rewrites, for one language.
        /// Split out of the LoadOthers postfix because the game does not always run LoadOthers on a
        /// language change: switching cn-&gt;en did, en-&gt;cn did not. The abnormality/battle tables then
        /// stayed English under a Chinese UI, and Chinese text left under an English font rendered as
        /// tofu. <see cref="EnsureVanillaLocalizeMatchesLanguage"/> is the catch-up trigger.
        /// </summary>
        /// <summary>
        /// Guards against cross-mod recursion. This method calls back into eight vanilla loaders,
        /// which is exactly the set another localization mod patches; if its patch calls LoadOthers
        /// (or TextDataModel init) again, A->B->A recurses until the stack overflows -- a crash that
        /// leaves no managed stack trace in Player.log, only a silent exit during loading.
        /// The per-function Refreshing* flags cannot see that: each only protects itself.
        /// </summary>
        private static bool _insideVanillaLocalizePass;

        public static void ApplyVanillaLocalizeForLanguage(string language, string reason)
        {
            if (_insideVanillaLocalizePass)
            {
                Debug.LogWarning($"[RMR Localize] Re-entrant vanilla localize pass ignored (reason={reason}); "
                    + "another mod re-triggered localization from inside ours.");
                return;
            }
            _insideVanillaLocalizePass = true;
            try
            {
                ApplyVanillaLocalizeForLanguageCore(language, reason);
            }
            finally
            {
                _insideVanillaLocalizePass = false;
            }
        }

        private static void ApplyVanillaLocalizeForLanguageCore(string language, string reason)
        {
            // LoadTextData used to run unguarded here -- the only unprotected top-level call in the
            // whole chain. It is 200+ lines of reflection over every loaded mod assembly, so anything
            // it threw skipped all refreshes below AND propagated back into vanilla LoadOthers,
            // breaking localization init for the rest of the session.
            try { LogLikeMod.LoadTextData(language); }
            catch (Exception ex) { Debug.LogWarning($"[RMR Localize] LoadTextData failed ({reason}): " + ex.Message); }
            // Equip reward desc was registered before book/localize load — re-stamp CN names.
            try { RewardingModel.RefreshAllEquipRewardXmlData(); }
            catch (Exception ex) { Debug.LogWarning("[RMR Localize] equip reward refresh failed: " + ex.Message); }
            try { LogLikeMod.RefreshVanillaAbnormalityTextData(language, reason); }
            catch (Exception ex) { Debug.LogWarning("[RMR Localize] vanilla abno refresh failed: " + ex.Message); }
            // Combat page ability / keyword texts (shop hover EN mix-up).
            try { LogLikeMod.RefreshVanillaBattleLocalize(language, reason); }
            catch (Exception ex) { Debug.LogWarning("[RMR Localize] battle localize refresh failed: " + ex.Message); }
            // Key pages: reload vanilla Books/PassiveDesc then re-apply mod BookInfo/PassiveInfo.
            // Bare LoadPassiveDesc alone CLEARS mod keys → workshop passives fall back to Hangul XML.
            try { LogLikeMod.RefreshVanillaBookAndPassiveLocalize(language, reason); }
            catch (Exception ex) { Debug.LogWarning("[RMR Localize] book/passive reload failed: " + ex.Message); }
            // Binah Floor Realization phase captions (BossBirdText) must follow game language.
            // Without this, wrong package can leave Korean lines during a Chinese client session.
            try { LogLikeMod.ReloadVanillaBossBirdTextForLanguage(language, reason); }
            catch (Exception ex) { Debug.LogWarning("[RMR Localize] BossBirdText reload failed: " + ex.Message); }
            // Opening PV + librarian names: once per language (cached inside helpers).
            // Prefix the reason with "LoadOthers" so the per-language caches inside these helpers
            // treat a catch-up pass as authoritative. They whitelist LoadOthers/LoadTextData only,
            // so a resync triggered from Invitation.OpenInit or CallUIPhase was silently skipped and
            // librarian names stayed in the previous language.
            string forcedReason = reason != null && reason.StartsWith("LoadOthers", StringComparison.OrdinalIgnoreCase)
                ? reason
                : "LoadOthers/resync:" + reason;
            try { LogLikeMod.ReloadOpeningLyricsForLanguage(language, forcedReason); } catch { }
            try { LogLikeMod.ReloadLibrariansNamesForLanguage(language, forcedReason); } catch { }
            // Store canonical: vanilla passes several spellings for one language, and comparing a raw
            // parameter against a live lookup made the catch-up below think nothing had changed.
            string canonical = LogLikeMod.CanonicalizeLanguageTag(language);
            _lastVanillaLocalizeLanguage = canonical;

            // Tables are correct now, but panels already on screen still show the strings they were
            // built with. Rebuild them after every full localization pass, not only when the language
            // string changed: another localization loader can overwrite the tables in-place while the
            // selected language remains the same. In that case a table resync without this replay left
            // list labels in the previous language while hover text (which reads the table live) was new.
            try { ReplayPanelPopulationForLanguage(); }
            catch (Exception ex) { Debug.LogWarning("[RMR Localize] panel replay failed: " + ex.Message); }
        }

        /// <summary>
        /// Catch-up: if the live language no longer matches the last full pass, redo the pass.
        /// A cheap string compare when they already agree, so this is safe on hot UI entry points.
        /// </summary>
        public static void EnsureVanillaLocalizeMatchesLanguage(string reason)
        {
            try
            {
                // TextDataModel.CurrentLanguage tracks an in-session switch immediately;
                // ResolveInitialTextLanguage reads option.dat first and can lag until the game exits.
                string raw;
                try { raw = TextDataModel.CurrentLanguage.ToString(); }
                catch { raw = LogLikeMod.GetActiveTextLanguage(); }
                if (string.IsNullOrEmpty(raw))
                    return;
                string lang = LogLikeMod.CanonicalizeLanguageTag(raw);
                bool bookkeepingMatches = !string.IsNullOrEmpty(_lastVanillaLocalizeLanguage)
                    && string.Equals(lang, _lastVanillaLocalizeLanguage, StringComparison.OrdinalIgnoreCase);

                // Trust the table, not the bookkeeping. Logs showed match=True next to a MISMATCH
                // sample: RMR had recorded stamping the right language, yet the live table held the
                // other one because something reloaded it after our pass finished. Reading what is
                // actually in the table is the only trigger that catches that.
                bool? tableCorrect = LogLikeMod.IsAbnormalityTableInLanguage(lang);
                // The mod's own dictionary feeds compendium/reward page names, the vanilla table
                // feeds the hover detail. Both must be checked or one stays stale while the other
                // looks fine -- that is the "Chinese names, English description" split.
                bool? modTextCorrect = LogLikeMod.IsModTextDictInLanguage(lang);
                bool needsResync = !bookkeepingMatches || tableCorrect == false || modTextCorrect == false;

                Debug.Log($"[RMR Localize] lang check: live='{raw}'->'{lang}' lastStamped='{_lastVanillaLocalizeLanguage ?? "none"}' "
                    + $"bookkeeping={bookkeepingMatches} table={(tableCorrect.HasValue ? tableCorrect.Value.ToString() : "unknown")} "
                    + $"modText={(modTextCorrect.HasValue ? modTextCorrect.Value.ToString() : "unknown")} "
                    + $"resync={needsResync} reason={reason} | {LogLikeMod.SampleAbnormalityTextScript(lang)}");
                if (!needsResync)
                    return;
                ApplyVanillaLocalizeForLanguage(raw, reason);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR Localize] EnsureVanillaLocalizeMatchesLanguage: " + ex.Message);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(LocalizedTextLoader), nameof(LocalizedTextLoader.LoadOpeningLyrics))]
        public static void LocalizedTextLoader_LoadOpeningLyrics(string language)
        {
            try { LogLikeMod.ReloadOpeningLyricsForLanguage(language, "LoadOpeningLyrics"); } catch { }
        }

        /// <summary>
        /// Opening PV: ensure lyrics + CJK font once at Init/SetLanguage (not every subtitle frame).
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(Opening.GameOpeningController), nameof(Opening.GameOpeningController.SetLanguage))]
        public static void GameOpeningController_SetLanguage(Opening.GameOpeningController __instance, string language)
        {
            try
            {
                LogLikeMod.ReloadOpeningLyricsForLanguage(language, "Opening.SetLanguage");
                LogLikeMod.ApplyOpeningSubtitleFont(__instance);
            }
            catch { /* ignore */ }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Opening.GameOpeningController), nameof(Opening.GameOpeningController.Init))]
        public static void GameOpeningController_Init(Opening.GameOpeningController __instance)
        {
            try
            {
                string lang = null;
                try { lang = GlobalGameManager.Instance?.CurrentOption?.language; } catch { }
                if (string.IsNullOrEmpty(lang))
                    lang = TextDataModel.CurrentLanguage.ToString();
                LogLikeMod.ReloadOpeningLyricsForLanguage(lang, "Opening.Init");
                LogLikeMod.ApplyOpeningSubtitleFont(__instance);
            }
            catch { /* ignore */ }
        }

        /// <summary>
        /// Formation slot names: light path only (no full-scene font repair).
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(UICharacterSlot), nameof(UICharacterSlot.SetCharacter))]
        public static void UICharacterSlot_SetCharacter_LocalizeName(UICharacterSlot __instance)
        {
            if (__instance?.Name == null)
                return;
            try
            {
                TextMeshProUGUI nameTmp = __instance.Name;
                TMP_FontAsset font = LogLikeMod.DefFont_TMP;
                if (font != null && nameTmp.font != font)
                    LogLikeMod.ApplyTmpFontPreservingSharpMaterial(nameTmp, font);

                if (!IsPoorUiText(nameTmp.text))
                    return;
                UnitDataModel unit = null;
                try
                {
                    if (__instance.unitBattleData != null)
                        unit = __instance.unitBattleData.unitData;
                }
                catch { unit = null; }
                string fixedName = ResolveUnitDisplayName(unit);
                if (!string.IsNullOrEmpty(fixedName) && !IsPoorUiText(fixedName))
                    nameTmp.text = fixedName;
            }
            catch { /* ignore */ }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UnitDataModel), "get_name")]
        public static void UnitDataModel_get_name(UnitDataModel __instance, ref string __result)
        {
            if (__instance == null || !IsPoorUiText(__result))
                return;
            try
            {
                string fixedName = ResolveUnitDisplayName(__instance);
                if (!string.IsNullOrEmpty(fixedName) && !IsPoorUiText(fixedName))
                    __result = fixedName;
            }
            catch { /* ignore */ }
        }

        private static string ResolveUnitDisplayName(UnitDataModel unit)
        {
            if (unit == null)
                return string.Empty;
            try
            {
                // Prefer book character / key-page localized name.
                BookModel book = unit.bookItem ?? unit.CustomBookItem;
                if (book != null)
                {
                    string charName = null;
                    try { charName = book.GetCharacterName(); } catch { charName = null; }
                    if (!string.IsNullOrEmpty(charName) && !IsPoorUiText(charName))
                        return charName;
                    string bookName = null;
                    try { bookName = book.GetName(); } catch { bookName = null; }
                    if (!string.IsNullOrEmpty(bookName) && !IsPoorUiText(bookName))
                        return bookName;
                    if (book.ClassInfo != null)
                    {
                        string loc = RewardingModel.GetLocalizedBookName(book.ClassInfo);
                        if (!string.IsNullOrEmpty(loc) && !IsPoorUiText(loc))
                            return loc;
                    }
                }

                // nameID → LibrariansNameXmlList (must be language-correct after our reload).
                int nameId = -1;
                try
                {
                    FieldInfo fi = AccessTools.Field(typeof(UnitDataModel), "_nameID");
                    if (fi != null)
                        nameId = (int)fi.GetValue(unit);
                }
                catch { nameId = -1; }
                if (nameId >= 0 && Singleton<LibrariansNameXmlList>.Instance != null)
                {
                    string fromList = Singleton<LibrariansNameXmlList>.Instance.GetName(nameId);
                    if (!string.IsNullOrEmpty(fromList) && !IsPoorUiText(fromList))
                        return fromList;
                }

                // Sephirah default book → CharactersNameXmlList
                if (unit.isSephirah && unit.bookItem != null)
                {
                    try
                    {
                        LorId id = unit.bookItem.GetBookClassInfoId();
                        string seph = Singleton<CharactersNameXmlList>.Instance?.GetName(id);
                        if (!string.IsNullOrEmpty(seph) && !IsPoorUiText(seph))
                            return seph;
                    }
                    catch { /* ignore */ }
                }
            }
            catch { /* ignore */ }
            return string.Empty;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(LocalizedTextLoader), nameof(LocalizedTextLoader.LoadBossBirdText))]
        public static void LocalizedTextLoader_LoadBossBirdText(string language)
        {
            // Vanilla builds path Localize/{lang}/{lang}_Bossbird; BaseMod stores
            // Localize/{lang}/BossBirdText/BossBirdText.txt. Re-apply the real file after vanilla load.
            try { LogLikeMod.ReloadVanillaBossBirdTextForLanguage(language, "LoadBossBirdText"); }
            catch (Exception ex) { Debug.LogWarning("[RMR Localize] LoadBossBirdText postfix failed: " + ex.Message); }
        }

        /// <summary>
        /// Key-page detail panel story / ability body — force origin-aware CN + repair fonts.
        /// Covers library equip browse after RMR is loaded (vanilla content tofu).
        /// Also rewrites book/character name TMP fields when Hangul/tofu remains.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(UIBattleSettingLibrarianInfoPanel), "SetData")]
        public static void UIBattleSettingLibrarianInfoPanel_SetData_KeypageLocalize(UIBattleSettingLibrarianInfoPanel __instance)
        {
            try
            {
                // No-op heavy path: full EnsureLocalizedFonts on every SetData caused hitching.
            }
            catch { }

            if (__instance == null)
                return;
            try
            {
                BookModel book = null;
                foreach (string fieldName in new[] { "unitdata", "unitData", "_unitdata", "_unitData", "data" })
                {
                    FieldInfo fi = AccessTools.Field(__instance.GetType(), fieldName);
                    if (fi == null)
                        continue;
                    object val = fi.GetValue(__instance);
                    if (val is UnitDataModel udm)
                    {
                        book = udm.bookItem;
                        break;
                    }
                }
                if (book?.ClassInfo == null)
                    return;

                string goodName = RewardingModel.GetLocalizedBookName(book.ClassInfo);
                if (string.IsNullOrEmpty(goodName) || IsPoorUiText(goodName))
                {
                    LogPoorBookNameOnce("LibrarianInfo.SetData", book, book.GetName(), goodName);
                    return;
                }

                // Known vanilla/mod TMP labels for key page / character title under portrait.
                foreach (string fieldName in new[]
                {
                    "txt_bookName", "txt_Name", "bookName", "characterName",
                    "txt_characterName", "txt_equipedbook", "txt_enemyName"
                })
                {
                    try
                    {
                        FieldInfo fi = AccessTools.Field(__instance.GetType(), fieldName)
                            ?? AccessTools.Field(__instance.GetType().BaseType, fieldName);
                        if (fi == null)
                            continue;
                        object label = fi.GetValue(__instance);
                        if (label == null)
                            continue;
                        PropertyInfo textProp = label.GetType().GetProperty("text");
                        if (textProp == null || !textProp.CanRead || !textProp.CanWrite)
                            continue;
                        string cur = textProp.GetValue(label, null) as string;
                        if (IsPoorUiText(cur))
                            textProp.SetValue(label, goodName, null);
                    }
                    catch { /* optional field */ }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR Localize] LibrarianInfo keypage name rewrite failed: " + ex.Message);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UISettingInvenEquipPageSlot), nameof(UISettingInvenEquipPageSlot.SetOperatingPanel))]
        public static void UISettingInvenEquipPageSlot_SetOperatingPanel_Fonts(UISettingInvenEquipPageSlot __instance)
        {
            // Second postfix (equip button enable is another method) — fonts only.
            // Slot open is high-frequency; setter patch is enough (repair throttled / phase-driven).
            // intentionally empty — equip slot open is high-frequency
        }

        /// <summary>
        /// After selecting RMR / opening invitation, re-stamp book+passive CN and fix live TMP
        /// so library key pages and upcoming story/PV text are not Hangul tofu.
        /// </summary>
        public static void RefreshKeypageAndStoryLocalize(string reason)
        {
            try
            {
                // Full re-sync first when the language changed behind our back; this used to refresh
                // book/passive only, which is why abnormality text stayed in the previous language.
                EnsureVanillaLocalizeMatchesLanguage(reason);
                string lang = TextDataModel.CurrentLanguage.ToString();
                LogLikeMod.RefreshVanillaBookAndPassiveLocalize(lang, reason);
                LogLikeMod.EnsureLocalizedFonts(reason, repairActiveUi: true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR Localize] RefreshKeypageAndStoryLocalize: " + ex.Message);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIInvitationRightMainPanel), nameof(UIInvitationRightMainPanel.OpenInit))]
        public static void UIInvitationRightMainPanel_OpenInit(UIInvitationRightMainPanel __instance)
        {
            try { RefreshKeypageAndStoryLocalize("Invitation.OpenInit"); } catch { }
            if (LogLikeMod.LogOpenButton == null)
            {
                LogLikeMod.LogOpenButton = ModdingUtils.CreateLogSelectable(__instance.transform, "LogLikeModIcon", new Vector2(1f, 1f), new Vector2(-70f, 350f), new Vector2(100f, 100f));
                LogLikeMod.LogOpenButton.gameObject.AddComponent<FrameDummy>();
                Button.ButtonClickedEvent buttonClickedEvent = new Button.ButtonClickedEvent();
                buttonClickedEvent.AddListener(() =>
                {
                    // Mode select FIRST (normal / realization / help / exit), then invitation is sent.
                    try
                    {
                        SingletonBehavior<UIMainOverlayManager>.Instance.Close();
                    }
                    catch { }

                    Debug.Log("[RMR] LogOpenButton clicked.");
                    List<string> ExceptModNames;
                    if (LogLikeMod.CheckExceptionModExist(out ExceptModNames))
                    {
                        string text = TextDataModel.GetText("ui_ExceptionWithLog") + Environment.NewLine;
                        foreach (string str in ExceptModNames)
                            text = $"{text}-{str}{Environment.NewLine}";
                        UIAlarmPopup.instance.SetAlarmText(text);
                        return;
                    }

                    System.Action openHub = () =>
                    {
                        // ShowInternal -> PrepareNewHubSession already ForceClose + DestroyOverlay,
                        // then recreates overlay and builds the full-screen hub. Do not create
                        // overlay before that, or UI can attach to a destroyed transform.
                        RMRStartHubPanel.ShowOnInvitation(__instance);
                    };

                    try
                    {
                        if (LoguePlayDataSaver.CheckPlayerData())
                        {
                            // Confirm dialog must succeed with Yes/No + callback. If it fails,
                            // do NOT leave the player on a dead OK-only popup — open hub instead
                            // (user already clicked "new run"; overwrite risk is logged).
                            bool confirmShown = UIAlarmPopup.instance.SetChoiceAlarmText(
                                TextDataModel.GetText("ui_RMR_ConfirmStartNewRun"),
                                (bool yes) =>
                                {
                                    if (yes)
                                        openHub();
                                });
                            if (!confirmShown)
                            {
                                Debug.LogWarning("[RMR] Overwrite confirm UI failed; opening start hub directly after New Run click.");
                                openHub();
                            }
                        }
                        else
                        {
                            openHub();
                        }
                    }
                    catch (Exception clickEx)
                    {
                        Debug.LogError("[RMR] LogOpenButton handler failed, opening hub directly: " + clickEx);
                        try { openHub(); } catch (Exception hubEx) { Debug.LogError("[RMR] openHub failed: " + hubEx); }
                    }
                });
                LogLikeMod.LogOpenButton.onClick = buttonClickedEvent;
                LogLikeMod.LogOpenButton.SelectEvent = new UnityEventBasedata();
                LogLikeMod.LogOpenButton.SelectEvent.AddListener((BaseEventData e) =>
                {
                    SingletonBehavior<UIMainOverlayManager>.Instance.SetTooltip(
                        TextDataModel.GetText("ui_RMR_StartNewRun"),
                        TextDataModel.GetText("ui_RMR_StartNewRunDesc"), 
                        LogLikeMod.LogOpenButton.transform as RectTransform);
                });
                LogLikeMod.LogOpenButton.DeselectEvent = new UnityEventBasedata();
                LogLikeMod.LogOpenButton.DeselectEvent.AddListener((BaseEventData e) =>
                {
                    SingletonBehavior<UIMainOverlayManager>.Instance.Close();
                });
            }
            // Continue is offered on the RMR mode-select hub (after LogOpenButton), not as a
            // separate invitation-column icon. Hide any legacy button if it still exists.
            if (LogLikeMod.LogContinueButton != null)
                LogLikeMod.LogContinueButton.gameObject.SetActive(false);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIEmotionPassiveCardInven), nameof(UIEmotionPassiveCardInven.SetSprites))]
        public static void UIEmotionPassiveCardInven_SetSprites(
          UIEmotionPassiveCardInven __instance,
          MentalState state)
        {
            if (!LogLikeMod.CheckStage())
                return;
            LogLikeMod.GetFieldValue<Image>(__instance, "_artwork").sprite = LogLikeMod.ArtWorks[__instance.Card.Artwork];
        }


        /// <summary>
        /// Wire passive succession for RMR librarians, and force-unlock key/combat page slots.
        /// SetData only runs SetBattleCardSlotState/SetEquipPageSlotState when isSephirahPanel;
        /// re-apply unlocks here so loadout stays clickable after unit refresh.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(UIBattleSettingLibrarianInfoPanel), nameof(UIBattleSettingLibrarianInfoPanel.SetData))]
        public static void UIBattleSettingLibrarianInfoPanel_SetData(
          UIBattleSettingLibrarianInfoPanel __instance,
          UnitDataModel data)
        {
            if (__instance == null || data == null || !LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return;
            UnitDataModel targetUnit = ResolveRoguelikeUnitData(data);
            if (targetUnit == null)
                return;
            // Vanilla may hand this panel an equivalent realization UnitDataModel rather
            // than the exact object stored in playerModel. Context is the authoritative
            // guard; requiring reference identity made passive succession unclickable.
            __instance.PassiveListSelectable.SubmitEvent.RemoveAllListeners();
            // Passive names here come from mod data that may have no translation for the current
            // language, so they stay Chinese while the UI font is Latin-only and draw as 口口口
            // ("9口口口" in the Passive Attribution list). Repair this panel once it is populated;
            // the throttled scene-wide sweep does not cover a panel built this late.
            try { LogLikeMod.RepairTmpFontsUnder(__instance.gameObject, "LibrarianInfoPanel.SetData"); }
            catch { /* never block the prep screen */ }
            LogLikeRoutines.ForceUnlockBattleSettingLoadoutSlots(__instance);
            __instance.PassiveListSelectable.SubmitEvent.AddListener((UnityAction<BaseEventData>)(e => UIPassiveSuccessionPopup.Instance.SetData(targetUnit, (UIPassiveSuccessionPopup.ApplyEvent)(() =>
            {
                __instance.passiveSlotsPanel.SetStatsDataInEquipBook(targetUnit.bookItem);
                (UI.UIController.Instance.GetUIPanel(UIPanelType.BattleSetting) as UIBattleSettingPanel).EditPanel.EquipPagePanel.ChangeEquipBook(null);
                UIControlManager.Instance.SelectSelectableForcely(__instance.PassiveListSelectable);
                // Realization loadouts are temporary projections and must not overwrite
                // the route snapshot while the player assigns Compendium passives.
                if (!RMRRealizationManager.IsRealizationPreparationActive)
                    LoguePlayDataSaver.SavePlayData_Menu();
            }))));
        }

        private static UnitDataModel ResolveRoguelikeUnitData(UnitDataModel candidate)
        {
            if (candidate == null || LogueBookModels.playerModel == null)
                return candidate;
            UnitDataModel exact = LogueBookModels.playerModel
                .Find(unit => ReferenceEquals(unit, candidate));
            if (exact != null)
                return exact;
            if (candidate.bookItem == null)
                return candidate;
            List<UnitDataModel> matches = LogueBookModels.playerModel
                .FindAll(unit => unit?.bookItem != null
                    && unit.bookItem.instanceId == candidate.bookItem.instanceId);
            return matches.Count == 1 ? matches[0] : candidate;
        }

        /// <summary>
        /// Vanilla OnPointerClickBattlePageSlot silently returns when:
        ///   GetEndContentState() == KeterCompleteOpen (3) — permanent on most post-game saves
        ///   isBattlePageLock — used floor / edit tab already open
        /// Both produce "no feedback" clicks. Passive succession never checks these.
        /// RMR prepare must always open the battle-card edit panel.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(UIBattleSettingLibrarianInfoPanel), nameof(UIBattleSettingLibrarianInfoPanel.OnPointerClickBattlePageSlot))]
        public static bool UIBattleSettingLibrarianInfoPanel_OnPointerClickBattlePageSlot(
          UIBattleSettingLibrarianInfoPanel __instance,
          BaseEventData data)
        {
            return !LogLikeRoutines.TryOpenBattleSettingEditFromLibrarian(
                __instance, UIBattleSettingEditTap.BattleCard, data);
        }

        /// <summary>
        /// Same gates as battle-page click (GetEndContentState / isEquipPageLock). Force open equip edit.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(UIBattleSettingLibrarianInfoPanel), nameof(UIBattleSettingLibrarianInfoPanel.OnPointerClickEquipPage))]
        public static bool UIBattleSettingLibrarianInfoPanel_OnPointerClickEquipPage(
          UIBattleSettingLibrarianInfoPanel __instance,
          BaseEventData data)
        {
            return !LogLikeRoutines.TryOpenBattleSettingEditFromLibrarian(
                __instance, UIBattleSettingEditTap.EquipPage, data);
        }

        /// <summary>
        /// After vanilla SetOperatingPanel disables Equip for IsUsedSephirah / Blue-Reverb clear floors,
        /// re-enable the button so key pages can be equipped during RMR prepare.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(UISettingInvenEquipPageSlot), nameof(UISettingInvenEquipPageSlot.SetOperatingPanel))]
        public static void UISettingInvenEquipPageSlot_SetOperatingPanel(UISettingInvenEquipPageSlot __instance)
        {
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return;
            BookModel book = __instance.BookDataModel;
            if (book == null || !book.CanEquipBookByGivePassive())
                return;
            UICustomGraphicObject equipBtn = LogLikeMod.GetFieldValue<UICustomGraphicObject>(__instance, "button_Equip");
            UICustomGraphicObject emptyDeckBtn = LogLikeMod.GetFieldValue<UICustomGraphicObject>(__instance, "button_EmptyDeck");
            Image equipIcon = LogLikeMod.GetFieldValue<Image>(__instance, "img_equipbuttonIcon");
            TextMeshProUGUI equipTxt = LogLikeMod.GetFieldValue<TextMeshProUGUI>(__instance, "txt_equipButton");
            if (equipBtn == null)
                return;
            equipBtn.interactable = true;
            if (emptyDeckBtn != null)
                emptyDeckBtn.interactable = !book.IsEmptyDeckAll();
            if (equipIcon != null)
                equipIcon.color = Color.white;
            if (equipTxt != null)
            {
                string key = book.owner == null ? "ui_bookinventory_equipbook" : "ui_book_bookname_unequip";
                equipTxt.text = TextDataModel.GetText(key);
            }
        }

        /// <summary>
        /// Skip the IsUsedSephirah early-out inside OnClickEquipButton during RMR prepare so unequip works.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(UISettingInvenEquipPageSlot), nameof(UISettingInvenEquipPageSlot.OnClickEquipButton))]
        public static bool UISettingInvenEquipPageSlot_OnClickEquipButton(UISettingInvenEquipPageSlot __instance)
        {
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return true;
            BookModel book = __instance.BookDataModel;
            if (book == null || !book.CanEquipBookByGivePassive())
                return true;
            // Only intercept when vanilla would hard-return on used-sephirah owner.
            if (book.owner == null)
                return true;
            StageModel stage = Singleton<StageController>.Instance?.GetStageModel();
            if (stage == null || !stage.IsUsedSephirah(book.owner.OwnerSephirah))
                return true;

            UnitDataModel owner = book.owner;
            owner.EquipBookForUI(null);
            UISoundManager.instance.PlayEffectSound(UISoundType.Ui_Click);
            UISettingEquipPageInvenPanel panel = __instance.GetEquipInvenPanel();
            panel.EquipLeftPanel.EquipPageList.ReleaseSelectedSlot(book);
            owner.appearanceType = Gender.N;
            __instance.SetOperatingPanel();
            panel.isSaveCheck = false;
            SingletonBehavior<UICharacterRenderer>.Instance.ReloadCharacter(owner);
            panel.ChangeEquipBook(owner);
            if (__instance.selectable != null && __instance.selectable.interactable)
                UIControlManager.Instance.SelectSelectableForcely(__instance.selectable);
            return false;
        }

        /// <summary>
        /// Skip IsUsedSephirah early-out for EmptyDeck during RMR prepare.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(UISettingInvenEquipPageSlot), nameof(UISettingInvenEquipPageSlot.OnClickEmptyDeckButton))]
        public static bool UISettingInvenEquipPageSlot_OnClickEmptyDeckButton(UISettingInvenEquipPageSlot __instance)
        {
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return true;
            BookModel book = __instance.BookDataModel;
            if (book == null)
                return true;
            if (book.owner == null)
                return true;
            StageModel stage = Singleton<StageController>.Instance?.GetStageModel();
            if (stage == null || !stage.IsUsedSephirah(book.owner.OwnerSephirah))
                return true;

            book.EmptyDeckToInventoryAll();
            UISettingEquipPageInvenPanel panel = __instance.GetEquipInvenPanel();
            panel.isSaveCheck = false;
            panel.UpdateRightPanel();
            panel.ReleaseSelectedSlot();
            __instance.SetOperatingPanel();
            if (__instance.selectable != null && __instance.selectable.interactable)
                UIControlManager.Instance.SelectSelectableForcely(__instance.selectable);
            return false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIPassiveSuccessionPopup), nameof(UIPassiveSuccessionPopup.InitReservedData))]
        public static void UIPassiveSuccessionPopup_InitReservedData()
        {
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return;
            foreach (BookModel bookModel in LogueBookModels.booklist)
            {
                bookModel.InitReservedDataForPassiveSuccession();
                foreach (PassiveModel passiveModel in bookModel.GetPassiveModelList())
                    passiveModel.InitReservedData();
            }
            foreach (UnitDataModel unitDataModel in LogueBookModels.playerModel)
            {
                BookModel bookItem = unitDataModel.bookItem;
                bookItem.InitReservedDataForPassiveSuccession();
                foreach (PassiveModel passiveModel in bookItem.GetPassiveModelList())
                    passiveModel.InitReservedData();
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StageWaveModel), nameof(StageWaveModel.GetUnitBattleDataListByFormation))]
        public static void StageWaveModel_GetUnitBattleDataListByFormation(
          StageWaveModel __instance,
          ref List<UnitBattleDataModel> __result)
        {
            List<UnitBattleDataModel> unitBattleDataModelList = new List<UnitBattleDataModel>();
            for (int i = 0; i < 10; ++i)
            {
                int formationIndex = __instance.GetFormationIndex(i);
                if (formationIndex < __instance.UnitList.Count)
                    unitBattleDataModelList.Add(__instance.UnitList[formationIndex]);
            }
            __result = unitBattleDataModelList;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StageWaveModel), nameof(StageWaveModel.Init))]
        public static void StageWaveModel_Init(StageWaveModel __instance)
        {
            List<int> fieldValue = ModdingUtils.GetFieldValue<List<int>>("_formationIndex", __instance);
            fieldValue.Add(5);
            fieldValue.Add(6);
            fieldValue.Add(7);
            fieldValue.Add(8);
            fieldValue.Add(9);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIManualScreenPage), nameof(UIManualScreenPage.LoadContent))]
        public static void UIManualScreenPage_LoadContent(UIManualScreenPage __instance)
        {
            TutorialManager.TutoInfo logTuto = Singleton<TutorialManager>.Instance.FindLogTuto(__instance);
            if (logTuto == null)
                return;
            __instance.img_screenShot.sprite = LogLikeMod.ArtWorks[logTuto.ArtWork];
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIManualContentPanel), nameof(UIManualContentPanel.SetData))]
        public static void UIManualContentPanel_SetData(UIManualContentPanel __instance)
        {
            if (Singleton<TutorialManager>.Instance.Inited)
                return;
            Singleton<TutorialManager>.Instance.Init(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UI.UIController), nameof(UI.UIController.CallUIPhase), new Type[1] { typeof(UIPhase) })]
        public static void UIController_CallUIPhase(UIController __instance, UIPhase phase)
        {
            // Second catch-up point for a language switch the game did not route through
            // LoadOthers. No-op string compare unless the language actually changed.
            try { EnsureVanillaLocalizeMatchesLanguage("CallUIPhase." + phase); } catch { /* ignore */ }

            // Only heavy-repair on rare phase transitions (not every BattleSetting open).
            try
            {
                if (phase == UIPhase.Story || phase == UIPhase.Invitation)
                    LogLikeMod.EnsureLocalizedFonts("CallUIPhase." + phase, repairActiveUi: true);
                else if (phase == UIPhase.BattleSetting || phase == UIPhase.Sephirah)
                    LogLikeMod.EnsureLocalizedFonts("CallUIPhase." + phase, repairActiveUi: false);
            }
            catch { /* ignore */ }

            if (phase != UIPhase.BattleSetting || MysteryBase.curinfo == null)
                return;
            MysteryBase.LoadGetAbnomalityPanel(MysteryBase.curinfo.abnormal, MysteryBase.curinfo.level);
            MysteryBase.curinfo = (MysteryBase.MysteryAbnormalInfo)null;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UI.UIController), nameof(UI.UIController.Awake))]
        public static void UIController_Awake()
        {
            if (!(LogLikeMod.UILogCardSlot.Original == null))
                return;
            LogLikeMod.UILogCardSlot.Original = LogLikeMod.UILogCardSlot.SlotCopying();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(LevelUpUI), nameof(LevelUpUI.OnClickTargetUnit))]
        public static void LevelUpUI_OnClickTargetUnit()
        {
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return;
            foreach (BattleUnitModel battleUnitModel in (IEnumerable<BattleUnitModel>)BattleObjectManager.instance.GetList())
            {
                if (battleUnitModel.view.abCardSelector.isInitialized)
                    battleUnitModel.view.abCardSelector.TurnOffUI();
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StageModel), nameof(StageModel.GetFrontAvailableFloor))]
        public static void StageModel_GetFrontAvailableFloor(
          StageModel __instance,
          ref StageLibraryFloorModel __result)
        {
            // CRITICAL: The original LogLike patch forced null whenever ANY floor in floorList was
            // unavailable. Vanilla StageController.EndBattle treats
            //   GetFrontAvailableWave()==null || GetFrontAvailableFloor()==null
            // as full stage clear → BattleFinalEndUI → return to library (舞台落幕).
            // Roguelike is single-floor multi-wave: after rewards + SetNextStage adds the next wave,
            // EndBattle must continue the reception, not exit. Forcing floor=null ended the entire
            // run after every act (and wiped Lastest via ClearBattle RemovePlayerData).
            // Prefer the player's currently selected floor (Language/Gebura etc.) so multi-wave
            // continuation does not snap back to Malkuth (历史层) map/BGM.
            if (!LogLikeMod.CheckStage())
                return;
            try
            {
                SephirahType selected = Singleton<StageController>.Instance != null
                    ? Singleton<StageController>.Instance.CurrentFloor
                    : SephirahType.None;
                if (selected != SephirahType.None
                    && (int)selected != 11 /* ETC */
                    && __instance.floorList != null)
                {
                    StageLibraryFloorModel selectedFloor = __instance.floorList.Find(
                        f => f != null && f.Sephirah == selected && !f.IsUnavailable());
                    if (selectedFloor != null)
                    {
                        __result = selectedFloor;
                        return;
                    }
                }

                if (__result != null)
                    return;
                if (__instance.GetFrontAvailableWave() == null)
                    return;
                if (__instance.floorList == null)
                    return;
                StageLibraryFloorModel fallback = __instance.floorList.Find(
                    (Predicate<StageLibraryFloorModel>)(x => x != null && !x.IsUnavailable()));
                if (fallback == null && __instance.floorList.Count > 0)
                    fallback = __instance.floorList[0];
                if (fallback != null)
                {
                    __result = fallback;
                    Debug.Log($"[RMR] GetFrontAvailableFloor fallback → {fallback.Sephirah} (keep multi-wave reception alive).");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR] GetFrontAvailableFloor fallback failed: " + ex.Message);
            }
        }

        /// <summary>
        /// After StartBattle map init:
        /// - Special invitation maps (BlackSilence / ReverberationBand / Philip…) → do NOT override
        ///   (enemy ManagerScript + InitializeMap already drive those maps/BGM).
        /// - FloorOnly impurity (蓝残响各层) → sephirah map matching FloorOnly.
        /// - Normal RMR → player's selected sephirah (Language/Gebura etc.).
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(StageController), nameof(StageController.StartBattle))]
        public static void StageController_StartBattle_FloorMap(StageController __instance)
        {
            if (!LogLikeMod.CheckStage() || __instance == null)
                return;
            try
            {
                StageModel model = __instance.GetStageModel();
                StageClassInfo info = Singleton<StageClassInfoList>.Instance.GetData(LogLikeMod.curstageid)
                    ?? model?.ClassInfo;
                if (info == null)
                    return;

                // Vanilla StartBattle already initialized the selected CreatureMap while the
                // temporary node definition was active. Do not replace it with the floor map
                // after the RMR invitation shell has been restored.
                if (LogLikeMod.curstagetype == StageType.Creature)
                {
                    Debug.Log($"[RMR] StartBattle keep creature map for stage={LogLikeMod.curstageid}.");
                    return;
                }

                // Impurity bosses / story receptions with MapInfo: leave vanilla map pipeline alone.
                // Our previous ChangeToSephirahMap ran AFTER ManagerScript.OnWaveStart and wiped
                // ReverberationBand / BlackSilence invitation maps + BGM.
                if (LogLikeMod.StageHasSpecialInvitationMaps(info))
                {
                    Debug.Log($"[RMR] StartBattle keep special invitation maps for stage={info.id} maps={string.Join(",", info.mapInfo.ToArray())}");
                    return;
                }

                // Prefer FloorOnly (impurity Blue-Reverb primary seph fights) over player pick.
                SephirahType mapFloor = __instance.CurrentFloor;
                if (info.floorOnlyList != null && info.floorOnlyList.Count > 0
                    && info.floorOnlyList[0] != SephirahType.None)
                {
                    mapFloor = info.floorOnlyList[0];
                    if (__instance.CurrentFloor != mapFloor)
                        __instance.SetCurrentSephirah(mapFloor);
                }

                if (mapFloor == SephirahType.None || (int)mapFloor == 11 /* ETC */)
                    return;

                BattleSceneRoot root = SingletonBehavior<BattleSceneRoot>.Instance;
                if (root != null)
                {
                    root.ChangeToSephirahMap(mapFloor, false);
                    Debug.Log($"[RMR] StartBattle sephirah map/BGM floor={mapFloor} stage={info.id}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR] StartBattle floor map apply failed: " + ex.Message);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIColorManager), nameof(UIColorManager.GetSephirahColor))]
        public static void UIColorManager_GetSephirahColor(SephirahType sephirah, ref Color __result)
        {
            //if (!LogLikeMod.CheckStage() || sephirah != SephirahType.None)
            // ;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIColorManager), nameof(UIColorManager.GetSephirahGlowColor))]
        public static void UIColorManager_GetSephirahGlowColor(SephirahType sephirah, ref Color __result)
        {
            //if (!LogLikeMod.CheckStage() || sephirah != SephirahType.None)
            //  ;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UICharacterStatInfoPanel), nameof(UICharacterStatInfoPanel.SetData), new Type[1] { typeof(UnitDataModel) })]
        public static void UICharacterStatInfoPanel_SetData(
          UICharacterStatInfoPanel __instance,
          UnitDataModel data)
        {
            if (LogueBookModels.playerModel == null || !LogueBookModels.playerModel.Contains(data) || LogueBookModels.playersstatadders == null || !LogueBookModels.playersstatadders.TryGetValue(data, out var statAdders) || statAdders.Count <= 0)
                return;
            BookXmlInfo bookXmlInfo = LogueBookModels.CurPlayerEquipInfo(data);
            int hp = bookXmlInfo.EquipEffect.Hp;
            hp.Log("chp : " + hp.ToString());
            hp.Log("mhp : " + data.MaxHp.ToString());
            int num1;
            if (data.MaxHp > hp)
            {
                TextMeshProUGUI hpText = __instance.hpText;
                string str1 = hp.ToString();
                num1 = data.MaxHp - hp;
                string str2 = num1.ToString();
                string str3 = $"{str1} <color=#22FFE4>+ {str2}</color>";
                hpText.text = str3;
            }
            else if (data.MaxHp < hp)
            {
                TextMeshProUGUI hpText = __instance.hpText;
                string str4 = hp.ToString();
                num1 = hp - data.MaxHp;
                string str5 = num1.ToString();
                string str6 = $"{str4} <color=red>- {str5}</color>";
                hpText.text = str6;
            }
            int num2 = bookXmlInfo.EquipEffect.Break;
            if (data.Break > num2)
            {
                TextMeshProUGUI breakText = __instance.breakText;
                string str7 = num2.ToString();
                num1 = data.Break - num2;
                string str8 = num1.ToString();
                string str9 = $"{str7} <color=#22FFE4>+ {str8}</color>";
                breakText.text = str9;
            }
            else if (data.Break < num2)
            {
                TextMeshProUGUI breakText = __instance.breakText;
                string str10 = num2.ToString();
                num1 = num2 - data.Break;
                string str11 = num1.ToString();
                string str12 = $"{str10} <color=red>- {str11}</color>";
                breakText.text = str12;
            }
            AtkResist sresist1 = data.bookItem.equipeffect.SResist;
            AtkResist sresist2 = bookXmlInfo.EquipEffect.SResist;
            if (sresist1 > sresist2)
                __instance.resistSlash.text = $"<color=#22FFE4>{data.bookItem.GetResistHP_Text(BehaviourDetail.Slash)}</color>";
            else if (sresist1 < sresist2)
                __instance.resistSlash.text = $"<color=red>{data.bookItem.GetResistHP_Text(BehaviourDetail.Slash)}</color>";
            AtkResist presist1 = data.bookItem.equipeffect.PResist;
            AtkResist presist2 = bookXmlInfo.EquipEffect.PResist;
            if (presist1 > presist2)
                __instance.resistSlash.text = $"<color=#22FFE4>{data.bookItem.GetResistHP_Text(BehaviourDetail.Penetrate)}</color>";
            else if (presist1 < presist2)
                __instance.resistSlash.text = $"<color=red>{data.bookItem.GetResistHP_Text(BehaviourDetail.Penetrate)}</color>";
            AtkResist hresist1 = data.bookItem.equipeffect.HResist;
            AtkResist hresist2 = bookXmlInfo.EquipEffect.HResist;
            if (hresist1 > hresist2)
                __instance.resistSlash.text = $"<color=#22FFE4>{data.bookItem.GetResistHP_Text(BehaviourDetail.Hit)}</color>";
            else if (hresist1 < hresist2)
                __instance.resistSlash.text = $"<color=red>{data.bookItem.GetResistHP_Text(BehaviourDetail.Hit)}</color>";
            AtkResist sbResist1 = data.bookItem.equipeffect.SBResist;
            AtkResist sbResist2 = bookXmlInfo.EquipEffect.SBResist;
            if (sbResist1 > sbResist2)
                __instance.resistSlash.text = $"<color=#22FFE4>{data.bookItem.GetResistBP_Text(BehaviourDetail.Slash)}</color>";
            else if (sbResist1 < sbResist2)
                __instance.resistSlash.text = $"<color=red>{data.bookItem.GetResistBP_Text(BehaviourDetail.Slash)}</color>";
            AtkResist pbResist1 = data.bookItem.equipeffect.PBResist;
            AtkResist pbResist2 = bookXmlInfo.EquipEffect.PBResist;
            if (pbResist1 > pbResist2)
                __instance.resistSlash.text = $"<color=#22FFE4>{data.bookItem.GetResistBP_Text(BehaviourDetail.Penetrate)}</color>";
            else if (pbResist1 < pbResist2)
                __instance.resistSlash.text = $"<color=red>{data.bookItem.GetResistBP_Text(BehaviourDetail.Penetrate)}</color>";
            AtkResist hbResist1 = data.bookItem.equipeffect.HBResist;
            AtkResist hbResist2 = bookXmlInfo.EquipEffect.HBResist;
            if (hbResist1 > hbResist2)
                __instance.resistSlash.text = $"<color=#22FFE4>{data.bookItem.GetResistBP_Text(BehaviourDetail.Hit)}</color>";
            else if (hbResist1 < hbResist2)
                __instance.resistSlash.text = $"<color=red>{data.bookItem.GetResistBP_Text(BehaviourDetail.Hit)}</color>";
        }

        [HarmonyPostfix, HarmonyPatch(typeof(BookModel), nameof(BookModel.GetMaxPassiveCost))]
        public static void BookModel_GetMaxPassiveCost(ref int __result)
        {
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return;
            // Roguelike receptions keep base 6. Floor Realization (解放战) prepare/battle uses 12
            // so full RMR passive kits can equip before the boss.
            int baseCost = 6;
            if (RMRRealizationManager.IsRealizationPreparationActive
                || RMRRealizationManager.InRealizationBattle
                || RMRRealizationManager.RealizationReceptionActive)
                baseCost = 12;
            int bonus = 0;
            try
            {
                if (Singleton<GlobalLogueEffectManager>.Instance != null)
                    bonus = Singleton<GlobalLogueEffectManager>.Instance.ChangeSuccCostValue();
            }
            catch { /* ignore */ }
            int num = baseCost + bonus;
            if (num < 0)
                num = 0;
            __result = num;
        }

        /// <summary>
        /// skin name -> vanilla thumbnail sprite. GetThumbSprite runs once per rendered book slot,
        /// and the reward / compendium / prep screens draw dozens at a time; each miss used to do a
        /// linear Find over the entire BookXmlList plus a Resources.Load, which is what made those
        /// screens hitch. Sprites here are Resources-owned, so caching the reference is safe.
        /// </summary>
        private static readonly Dictionary<string, Sprite> _lorThumbSpriteBySkin =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);

        [HarmonyPostfix, HarmonyPatch(typeof(BookModel), nameof(BookModel.GetThumbSprite))]
        public static void BookModel_GetThumbSprite(BookModel __instance, ref Sprite __result)
        {
            if (!LogLikeMod.CheckStage() || __instance.ClassInfo.CharacterSkin == null)
                return;
            if (!__instance.ClassInfo.CharacterSkin.Any<string>())
                return;
            try
            {
                if (__instance.ClassInfo.skinType == "Lor")
                {
                    string skin = __instance.ClassInfo.CharacterSkin[0];
                    if (skin != null && _lorThumbSpriteBySkin.TryGetValue(skin, out Sprite cached))
                    {
                        if (cached != null)
                            __result = cached;
                        return;
                    }
                    BookXmlInfo bookXmlInfo = Singleton<BookXmlList>.Instance.GetList().Find((Predicate<BookXmlInfo>)(x => x.CharacterSkin[0] == __instance.ClassInfo.CharacterSkin[0] && !x.id.IsWorkshop()));
                    Sprite resolved = bookXmlInfo != null
                        ? UnityEngine.Resources.Load<Sprite>("Sprites/Books/Thumb/" + bookXmlInfo.id.id.ToString())
                        : null;
                    // Cache misses too: a skin with no vanilla book must not re-scan every frame.
                    if (skin != null)
                        _lorThumbSpriteBySkin[skin] = resolved;
                    if (resolved != null)
                        __result = resolved;
                }
                else
                {
                    if (!(__instance.ClassInfo.skinType == "CUSTOM") || !__instance.ClassInfo.CharacterSkin[0].Contains("<LogLike>"))
                        return;
                    string key = __instance.ClassInfo.CharacterSkin[0].Remove(0, 9);
                    if (!LogLikeMod.ArtWorks.ContainsKey(key))
                        return;
                    __result = LogLikeMod.ArtWorks[key];
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Failed to load thumbnail: " + ex);
                __result = UnityEngine.Resources.Load<Sprite>("Sprites/Books/Thumb/1");
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(StageController), nameof(StageController.GameOver))]
        public static void StageController_GameOver(ref bool iswin, ref bool isbackbutton)
        {
            if (!iswin
                && !RMRRealizationManager.RealizationCombatLive
                && !RMRRealizationManager.InRealizationBattle
                && LogLikeMod.CheckStage(true))
            {
                LoguePlayDataSaver.MarkRunDefeated("StageController.GameOver");
            }

            // Realization defeat goes through vanilla GameOver, NOT EndBattle, so the
            // EndBattle-hook cleanup (flags + route loadout restore) never ran. Stale
            // InRealizationBattle/CompendiumOnlyLoadoutActive then corrupts the next
            // realization AND normal play (CheckStage suppressed -> instant defeat).
            if (RMRRealizationManager.RealizationCombatLive || RMRRealizationManager.InRealizationBattle)
            {
                Debug.Log("[RMRRealizationManager] GameOver during live realization — running realization end cleanup (win=" + iswin + ").");
                try { RMRRealizationManager.OnRealizationBattleEnded(iswin); }
                catch (Exception ex) { Debug.LogWarning("[RMRRealizationManager] GameOver realization cleanup failed: " + ex.Message); }
                RMRRealizationManager.ClearRealizationFlag();
            }

            if (!RMRRealizationManager.ForceReturnAsDefeatPending)
                return;
            iswin = false;
            isbackbutton = true;
            RMRRealizationManager.ConsumeForceReturnAsDefeat();
            Debug.Log("[RMRRealizationManager] Routed realization completion through the vanilla defeat return flow.");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StageController), nameof(StageController.ActivateStartBattleEffectPhase))]
        public static void StageController_ActivateStartBattleEffectPhase(StageController __instance)
        {
            var list = ModdingUtils.GetFieldValue<List<BattlePlayingCardDataInUnitModel>>("_allCardList", __instance);
            var list2 = new List<BattlePlayingCardDataInUnitModel>();
            list2.AddRange(list); // prevent collection modified exception
            foreach (BattlePlayingCardDataInUnitModel card in list2)
                Singleton<GlobalLogueEffectManager>.Instance.OnStartBattle_AfterCardSet(card);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StageController), nameof(StageController.OnFixedUpdateLate))]
        public static void StageController_OnFixedUpdateLate()
        {
            // Hold pause while waiting for floor pick / realization prepare (do not clear each frame).
            if (RMRRealizationManager.AwaitingRealizationFloorPick
                || RMRRealizationManager.IsRealizationPreparationActive)
            {
                LogLikeMod.PauseBool = true;
                return;
            }

            if (LogLikeMod.CheckStage())
                LogLikeMod.PauseBool = RewardingModel.RewardInStage();
            else
                LogLikeMod.PauseBool = false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StageController), nameof(StageController.ClearBattle))]
        public static void StageController_ClearBattle(StageController __instance)
        {
            // Skip Roguelike clear-battle processing during realization battles
            if (RMRRealizationManager.InRealizationBattle)
                return;

            if (!LogLikeMod.CheckStage(true) || Environment.StackTrace.Contains("UIBgScreenChangeAnim"))
                return;
            if (!LogLikeMod.purpleexcept)
            {
                if (LogLikeMod.AddPlayer)
                {
                    try
                    {
                        LogueBookModels.AddSubPlayer();
                    }
                    catch (Exception addEx)
                    {
                        // Must not abort ClearBattle / EndBattlePhase — that freezes on black screen.
                        Debug.LogError("[RMR] AddSubPlayer during ClearBattle failed (continuing stage end): " + addEx);
                    }
                }
                if (LogLikeMod.RecoverPlayers)
                {
                    foreach (UnitBattleDataModel unitBattleDataModel in LogueBookModels.playerBattleModel)
                    {
                        try
                        {
                            unitBattleDataModel.isDead = false;
                            unitBattleDataModel.hp += (unitBattleDataModel.MaxHp - (int)unitBattleDataModel.hp) * 0.75f; // recover 75% of missing hp
                            unitBattleDataModel.Init();
                            unitBattleDataModel.emotionDetail.Reset();
                        }
                        catch (Exception recEx)
                        {
                            Debug.LogWarning("[RMR] RecoverPlayers unit failed: " + recEx.Message);
                        }
                    }
                }
            }
            LogLikeMod.AddPlayer = false;
            LogLikeMod.RecoverPlayers = false;
            if (!(LogLikeMod.curstageid != (LorId)null))
                return;
            LoguePlayDataSaver.SavePlayData();
            LoguePlayDataSaver.RemoveFlashData();
            StageModel stageModel = __instance.GetStageModel();
            // Do NOT wipe Lastest merely because this reception's waves are done.
            // RMR is multi-node: after each fight you still continue the journey (shop/mystery/next).
            // Wiping here made Continue vanish after every act; SavePlayData_Menu could not recreate
            // Lastest (it only updates an existing file). Only clear continue when the *run* ends.
            bool wavesEmpty = false;
            try { wavesEmpty = stageModel == null || stageModel.GetFrontAvailableWave() == null; } catch { wavesEmpty = true; }
            if (!wavesEmpty)
                return;

            bool partyWiped = false;
            try
            {
                partyWiped = BattleObjectManager.instance == null
                    || BattleObjectManager.instance.GetAliveListWithAvailable(Faction.Player).Count == 0;
            }
            catch { /* ignore */ }

            bool impurityRunComplete = LogLikeMod.curstagetype == StageType.Boss
                && LogLikeMod.curchaptergrade >= ChapterGrade.Grade7
                && (LogLikeMod.nextlist == null || LogLikeMod.nextlist.Count == 0);

            if (partyWiped || impurityRunComplete)
            {
                if (partyWiped)
                    LoguePlayDataSaver.MarkRunDefeated("StageController.ClearBattle party wipe");
                else
                    LoguePlayDataSaver.RemovePlayerData();
                Debug.Log($"[RMR] ClearBattle: wiped Lastest continue save (partyWiped={partyWiped}, impurityRunComplete={impurityRunComplete}).");
            }
            else
            {
                Debug.Log($"[RMR] ClearBattle: kept Lastest continue save (reception waves empty, journey continues). grade={LogLikeMod.curchaptergrade} type={LogLikeMod.curstagetype} nextlist={LogLikeMod.nextlist?.Count ?? 0}");
            }
        }

        /// <summary>
        /// A patch to be able to disable units when unchecked.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(UICharacterSlot), nameof(UICharacterSlot.SetNoToggleState))]
        public static void UICharacterSlot_SetToggleStateFalse(UICharacterSlot __instance)
        {
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return;
            var unit = LogueBookModels.playerBattleModel.Find(x => x.unitData == __instance.unitBattleData.unitData);
            if (unit != null)
                unit.IsAddedBattle = false;
        }

        /// <summary>
        /// A patch to be able to enable units when checked.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(UICharacterSlot), nameof(UICharacterSlot.SetYesToggleState))]
        public static void UICharacterSlot_SetToggleStateTrue(UICharacterSlot __instance)
        {
            if (!LogLikeRoutines.IsRoguelikeBattleSettingContext())
                return;
            var unit = LogueBookModels.playerBattleModel.Find(x => x.unitData == __instance.unitBattleData.unitData);
            if (unit != null)
                unit.IsAddedBattle = true;
        }

        #endregion

        #region FINALIZERS
        [HarmonyFinalizer, HarmonyPatch(typeof(BattleUnitEmotionDetail), nameof(BattleUnitEmotionDetail.Reset))]
        static Exception BattleUnitEmotionDetail_Reset(Exception __exception)
        {
            return null;
        }


        #endregion
    }
}
