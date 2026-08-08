// -----------------------------------------------------------------------------
// Shop system component: ShopGoods_CardUpgrade
// Namespace/file: ruina-roguelike-reborn-main\abcdcode_LOGLIKE_MOD\ShopGoods_CardUpgrade.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
using GameSave;
using LOR_DiceSystem;
using RogueLike_Mod_Reborn;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace abcdcode_LOGLIKE_MOD
{
    /// <summary>Shop component: ShopGoods_CardUpgrade</summary>
    public class ShopGoods_CardUpgrade : ShopGoods
    {
        public UILogCustomSelectable customSelectable;
        public TextMeshProUGUI NameText;
        public TextMeshProUGUI IconText;
        #region --- Battle hooks ---


        public static List<DiceCardItemModel> GetUpgradeableCards()
        {
            List<DiceCardItemModel> cards = LogueBookModels.GetCardList(true, true);
            if (cards == null)
                cards = new List<DiceCardItemModel>();
            // One entry per card kind (upgrade is by type, not by stack count).
            var seen = new HashSet<string>();
            var result = new List<DiceCardItemModel>();
            foreach (DiceCardItemModel x in cards
                .Where(c => c != null && c.ClassInfo != null && c.ClassInfo.CheckCanUpgrade())
                .Where(c => Singleton<LogCardUpgradeManager>.Instance.GetAllUpgradesCard(c.GetID()).Count > 0))
            {
                LorId id = x.GetID();
                string key = id == null ? x.GetHashCode().ToString()
                    : ((id.packageId ?? "") + ":" + id.id);
                if (!seen.Add(key))
                    continue;
                result.Add(x);
            }
            // Binah fixed-deck degraded pages are NoInventory and never enter cardlist;
            // surface them explicitly so rest/shop can upgrade to full Arbiter versions.
            foreach (DiceCardItemModel binahCard in LogueBookModels.GetBinahDegradedUpgradeableCards())
            {
                LorId id = binahCard?.GetID();
                if (id == null)
                    continue;
                string key = (id.packageId ?? "") + ":" + id.id;
                if (!seen.Add(key))
                    continue;
                result.Add(binahCard);
            }
            return result;
        }

        public static bool HasUpgradeableCards()
        {
            return GetUpgradeableCards().Count > 0;
        }
        #endregion

        #region --- Getters / setters / checks ---


        public void SetGoods(int price)
        {
            Sprite sprite = null;
            if (LogLikeMod.ArtWorks.ContainsKey("Shop_CardUpgrade_Icon"))
                sprite = LogLikeMod.ArtWorks["Shop_CardUpgrade_Icon"];
            else if (LogLikeMod.ArtWorks.ContainsKey("Stage_Rest"))
                sprite = LogLikeMod.ArtWorks["Stage_Rest"];
            if (sprite != null)
                this.customSelectable = ModdingUtils.CreateLogSelectable(this.transform, sprite, new Vector2(1f, 1f), new Vector2(0.0f, 0.0f), new Vector2(165f, 220f));
            else
                this.customSelectable = ModdingUtils.CreateLogSelectable(this.transform, "Stage_Rest", new Vector2(1f, 1f), new Vector2(0.0f, 0.0f), new Vector2(165f, 220f));
            Button.ButtonClickedEvent buttonClickedEvent = new Button.ButtonClickedEvent();
            buttonClickedEvent.AddListener(new UnityAction(this.OnClickGoods));
            this.customSelectable.onClick = buttonClickedEvent;
            this.customSelectable.SelectEvent = new UnityEventBasedata();
            this.customSelectable.SelectEvent.AddListener((UnityAction<BaseEventData>)(e => this.OnPointerEnter()));
            this.customSelectable.DeselectEvent = new UnityEventBasedata();
            this.customSelectable.DeselectEvent.AddListener((UnityAction<BaseEventData>)(e => this.OnPointerExit()));
            Image image = ModdingUtils.CreateImage(this.transform, "MoneyIcon", new Vector2(1f, 1f), new Vector2(-10f, -130f));
            this.moneyicon = image;
            this.Money = ModdingUtils.CreateText_TMP(image.transform, new Vector2(40f, 0.0f), 25, new Vector2(0.5f, 0.0f), new Vector2(1f, 1f), new Vector2(0.0f, 0.0f), TextAlignmentOptions.MidlineLeft, LogLikeMod.DefFontColor, LogLikeMod.DefFont_TMP);
            this.SetPrice(price);
        }

        public void SetPrice(int newPrice)
        {
            this.price = newPrice;
            if (this.Money != null)
                this.Money.text = this.price.ToString();
        }
        #endregion

        #region --- Save / load ---


        public override void LoadFromSaveData(SaveData data)
        {
            base.LoadFromSaveData(data);
            this.SetPrice(this.price);
        }
        #endregion

        #region --- Getters / setters / checks ---


        public override bool CanPurchase()
        {
            return HasUpgradeableCards() && base.CanPurchase();
        }
        #endregion

        #region --- Battle hooks ---


        public override bool CheckEnoughMoney()
        {
            bool enough = this.CanPurchase();
            if (this.Money != null)
                this.Money.color = enough ? LogLikeMod.DefFontColor : Color.red;
            return enough;
        }

        public void OnClickGoods()
        {
            if (!this.CanPurchase())
            {
                UIAlarmPopup.instance.SetAlarmText(TextDataModel.GetText("CardCheckPopUp_CannotUpgrade"));
                return;
            }
            MysteryModel_CardChoice.PopupCardChoice(GetUpgradeableCards(), new MysteryModel_CardChoice.ChoiceResult(this.OnChoiceCard), MysteryModel_CardChoice.ChoiceDescType.UpgradeDesc);
        }

        public void OnChoiceCard(MysteryModel_CardChoice mystery, DiceCardItemModel model)
        {
            if (model == null)
                return;
            MysteryModel_UpgradeCheckPopup.PopupUpgradeCheck(model.GetID(), my => this.ApplyUpgrade(mystery, my, model.GetID()));
        }
        #endregion

        #region --- Other helpers ---


        public void ApplyUpgrade(MysteryModel_CardChoice mystery, MysteryModel_UpgradeCheckPopup popup, LorId cardid)
        {
            try
            {
                if (!this.CanPurchase() || popup == null)
                    return;

                if (popup.binahSpecialUpgrade || LogueBookModels.IsBinahDegradedUpgradeable(cardid))
                {
                    if (!LogueBookModels.TryApplyBinahDegradedCardUpgrade(cardid))
                        return;
                    PassiveAbility_MoneyCheck.SubMoney(this.price);
                    UISoundManager.instance.PlayEffectSound(UISoundType.Card_Apply);
                    if (popup.slot != null)
                        CardAddVfx.RunCardVfx(popup.slot);
                    if (this.parent != null)
                        this.parent.OnCardUpgradePurchased(this);
                    if (!HasUpgradeableCards())
                        this.gameObject.SetActive(false);
                    return;
                }

                if (popup.metadata == null || string.IsNullOrEmpty(popup.metadata.unparsedPid))
                {
                    UIAlarmPopup.instance.SetAlarmText(TextDataModel.GetText("CardCheckPopUp_CannotUpgrade"));
                    return;
                }

                LorId upgradedId = new LorId(popup.metadata.unparsedPid, cardid.id);
                LogueBookModels.ReplaceUnlockedCardWithUpgrade(cardid, upgradedId);
                LogueBookModels.SavePermanentCompendiumData();
                try { LoguePlayDataSaver.SavePlayData_Menu(); }
                catch (Exception ex) { Debug.LogWarning("[RMR] Save after card upgrade: " + ex.Message); }
                PassiveAbility_MoneyCheck.SubMoney(this.price);
                UISoundManager.instance.PlayEffectSound(UISoundType.Card_Apply);
                if (popup.slot != null)
                    CardAddVfx.RunCardVfx(popup.slot);
                if (this.parent != null)
                    this.parent.OnCardUpgradePurchased(this);
                if (!HasUpgradeableCards())
                    this.gameObject.SetActive(false);
            }
            finally
            {
                // Always restore shop UI — failed upgrades used to leave CardChoice + popup stuck.
                try { Singleton<MysteryManager>.Instance.EndMystery(mystery); } catch { /* best-effort */ }
                try { Singleton<MysteryManager>.Instance.EndMystery(popup); } catch { /* best-effort */ }
            }
        }
        #endregion

        #region --- Save / load ---


        public override SaveData GetSaveData()
        {
            return base.GetSaveData();
        }
        #endregion

        #region --- Battle hooks ---


        public void OnPointerEnter()
        {
            var overlay = SingletonBehavior<UIMainOverlayManager>.Instance;
            if (overlay == null)
                return;
            overlay.SetTooltip(TextDataModel.GetText("Shop_CardUpgrade_Name"), TextDataModel.GetText("Shop_CardUpgrade_Desc"), this.gameObject.transform as RectTransform, Rarity.Rare, UIToolTipPanelType.OnlyContent);
            // tooltipPositionPivot is private — access via reflection (FieldAccessException otherwise).
            try
            {
                var pivot = HarmonyLib.AccessTools.Field(typeof(UIMainOverlayManager), "tooltipPositionPivot")
                    ?.GetValue(overlay) as RectTransform;
                if (pivot != null)
                {
                    var curPos = pivot.anchoredPosition;
                    pivot.anchoredPosition = new Vector2(curPos.x, curPos.y + 100f);
                }
            }
            catch { }
        }

        public void OnPointerExit()
        {
            SingletonBehavior<UIMainOverlayManager>.Instance.Close();
        }
        #endregion

    }
}


