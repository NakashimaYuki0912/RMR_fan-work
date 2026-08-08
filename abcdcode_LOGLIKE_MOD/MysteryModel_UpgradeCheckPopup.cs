// -----------------------------------------------------------------------------
// Mystery / event node model: MysteryModel_UpgradeCheckPopup
// Namespace/file: ruina-roguelike-reborn-main\abcdcode_LOGLIKE_MOD\MysteryModel_UpgradeCheckPopup.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
using GameSave;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;
using UI;
using RogueLike_Mod_Reborn;

namespace abcdcode_LOGLIKE_MOD
{

    /// <summary>Mystery node model: MysteryModel_UpgradeCheckPopup</summary>

    public class MysteryModel_UpgradeCheckPopup : MysteryBase
    {
        public LorId cardid;
        public MysteryModel_UpgradeCheckPopup.CheckResult okdele;
        public MysteryModel_UpgradeCheckPopup.CheckResult nodele;
        public LogLikeMod.UILogCardSlot slot;
        public UpgradeMetadata metadata;
        /// <summary>True when upgrading a degraded Binah page into its full vanilla counterpart.</summary>
        public bool binahSpecialUpgrade;
        #region --- Save / load ---


        public override void LoadFromSaveData(SaveData savedata)
        {
        }
        #endregion

        #region --- Other helpers ---


        public static MysteryModel_UpgradeCheckPopup PopupUpgradeCheck(
          LorId cardid,
          MysteryModel_UpgradeCheckPopup.CheckResult okdele = null,
          MysteryModel_UpgradeCheckPopup.CheckResult nodele = null)
        {
            MysteryModel_UpgradeCheckPopup mystery = new MysteryModel_UpgradeCheckPopup();
            mystery.okdele = okdele;
            mystery.nodele = nodele;
            mystery.cardid = cardid;
            Singleton<MysteryManager>.Instance.AddInterrupt(mystery);
            return mystery;
        }

        public override void SwapFrame(int id)
        {
            Image image = ModdingUtils.CreateImage(LogLikeMod.LogUIObjs[97].transform, "MysteryPanel_transparent", new Vector2(1f, 1f), new Vector2(0.0f, 0.0f));
            this.FrameObj.Add("Frame", image.gameObject);
            Button button1 = ModdingUtils.CreateButton(image.transform, "MysteryButton_Enable", new Vector2(1f, 1f), new Vector2(-150f, -380f), new Vector2(300f, 70f));
            this.FrameObj.Add("Ok", button1.gameObject);
            Button.ButtonClickedEvent buttonClickedEvent1 = new Button.ButtonClickedEvent();
            buttonClickedEvent1.AddListener(new UnityAction(this.OnClickOk));
            button1.onClick = buttonClickedEvent1;
            Button button2 = ModdingUtils.CreateButton(image.transform, "MysteryButton_Enable", new Vector2(1f, 1f), new Vector2(150f, -380f), new Vector2(300f, 70f));
            this.FrameObj.Add("No", button2.gameObject);
            Button.ButtonClickedEvent buttonClickedEvent2 = new Button.ButtonClickedEvent();
            buttonClickedEvent2.AddListener(new UnityAction(this.OnClickNo));
            button2.onClick = buttonClickedEvent2;
            TextMeshProUGUI textTmp1 = ModdingUtils.CreateText_TMP(button1.transform, new Vector2(0.0f, 0.0f), 30, new Vector2(0.0f, 0.0f), new Vector2(1f, 1f), new Vector2(0.0f, 0.0f), TextAlignmentOptions.Midline, LogLikeMod.DefFontColor, LogLikeMod.DefFont_TMP);
            textTmp1.text = TextDataModel.GetText("CardCheckPopUp_UpgradeOk");
            textTmp1.transform.Rotate(0.0f, 0.0f, 2.5f);
            this.FrameObj.Add("OkText", textTmp1.gameObject);
            TextMeshProUGUI textTmp2 = ModdingUtils.CreateText_TMP(button2.transform, new Vector2(0.0f, 0.0f), 30, new Vector2(0.0f, 0.0f), new Vector2(1f, 1f), new Vector2(0.0f, 0.0f), TextAlignmentOptions.Midline, LogLikeMod.DefFontColor, LogLikeMod.DefFont_TMP);
            textTmp2.text = TextDataModel.GetText("CardCheckPopUp_UpgradeNo");
            textTmp2.transform.Rotate(0.0f, 0.0f, 2.5f);
            this.FrameObj.Add("NoText", textTmp2.gameObject);
            LogLikeMod.UILogCardSlot curcard = LogLikeMod.UILogCardSlot.SlotCopyingByOrig();
            this.metadata = null;
            this.slot = null;
            this.binahSpecialUpgrade = LogueBookModels.IsBinahDegradedUpgradeable(this.cardid);

            var upgradeList = Singleton<LogCardUpgradeManager>.Instance.GetAllUpgradesCard(this.cardid, 1).Values.ToList();
            if (!this.binahSpecialUpgrade)
                upgradeList.Sort((x, y) => UpgradeMetadata.UnpackPidUnsafe(x.id.packageId).index > UpgradeMetadata.UnpackPidUnsafe(y.id.packageId).index ? 1 : 0);

            // No upgrade path: keep Cancel enabled, disable OK so the shop cannot softlock.
            if (upgradeList.Count == 0 && !this.binahSpecialUpgrade)
            {
                button1.interactable = false;
                try
                {
                    UIAlarmPopup.instance.SetAlarmText(TextDataModel.GetText("CardCheckPopUp_CannotUpgrade"));
                }
                catch { /* alarm optional */ }
            }

            curcard.transform.SetParent(image.transform);
            curcard.transform.localScale = new Vector3(1.5f, 1.5f);
            curcard.transform.localPosition = new Vector3(upgradeList.Count > 1 ? 0f : -250f, upgradeList.Count > 1 ? 270f : 0.0f);
            DiceCardItemModel info = new DiceCardItemModel(ItemXmlDataList.instance.GetCardItem(this.cardid));
            curcard.SetData(info);
            curcard.txt_cardNumbers.text = "";
            curcard.selectable.SubmitEvent.RemoveAllListeners();
            curcard.selectable.SubmitEvent.AddListener(e => this.OnClickCard(info));
            curcard.selectable.SelectEvent.RemoveAllListeners();
            curcard.selectable.SelectEvent.AddListener(e => this.OnPointerEnter(curcard));
            curcard.selectable.DeselectEvent.RemoveAllListeners();
            curcard.selectable.DeselectEvent.AddListener(e => this.OnPointerExit(curcard));
            curcard.gameObject.SetActive(true);
            
            if (upgradeList.Count == 1)
            {
                var upgrade = upgradeList[0];
                LogLikeMod.UILogCardSlot resultcard = LogLikeMod.UILogCardSlot.SlotCopyingByOrig();
                resultcard.transform.SetParent(image.transform);
                resultcard.transform.localScale = new Vector3(1.5f, 1.5f);
                resultcard.transform.localPosition = new Vector3(250f, 0.0f);
                info = new DiceCardItemModel(upgrade);
                resultcard.SetData(info);
                resultcard.txt_cardNumbers.text = "";
                resultcard.selectable.SubmitEvent.RemoveAllListeners();
                resultcard.selectable.SubmitEvent.AddListener(e => this.OnClickCard(info));
                resultcard.selectable.SelectEvent.RemoveAllListeners();
                resultcard.selectable.SelectEvent.AddListener(e => this.OnPointerEnter(resultcard));
                resultcard.selectable.DeselectEvent.RemoveAllListeners();
                resultcard.selectable.DeselectEvent.AddListener(e => this.OnPointerExit(resultcard));
                resultcard.gameObject.SetActive(true);
                this.slot = resultcard;
                if (!this.binahSpecialUpgrade)
                    UpgradeMetadata.UnpackPid(upgrade.id.packageId, out this.metadata);
            }
            else
            {
                bool even = upgradeList.Count % 2 == 0;
                for (int i = 0; i < upgradeList.Count; i++)
                {
                    var upgrade = upgradeList[i];
                    LogLikeMod.UILogCardSlot resultcard = LogLikeMod.UILogCardSlot.SlotCopyingByOrig();
                    resultcard.name = "CardUpgradeMult_" + i.ToString();
                    resultcard.transform.SetParent(image.transform);
                    resultcard.transform.localScale = new Vector3(1.5f, 1.5f);
                    if (even)
                        resultcard.transform.localPosition = new Vector3(125f * (i / 2 + 1) * (i % 2 == 1 ? 1f : -1f), -50.0f);
                    else
                        resultcard.transform.localPosition = new Vector3(250f * (i / 2 + i % 2) * (i % 2 == 0 ? 1f : -1f), -50.0f);
                    info = new DiceCardItemModel(upgrade);
                    resultcard.SetData(info);
                    var selectedImage = ModdingUtils.CreateImage(resultcard.transform.parent, LogLikeMod.ArtWorks["RogueLikeRebornIconAlt"], new Vector2(1f, 1f), new Vector2(resultcard.transform.localPosition.x - 30f, 100), new Vector2(75f, 75f));
                    selectedImage.transform.Rotate(0f, 0f, 6f);
                    this.FrameObj.Add("selectedImage_" + i.ToString(), selectedImage.gameObject);
                    if (i == 0)
                    {
                        UpgradeMetadata.UnpackPid(upgrade.id.packageId, out this.metadata);
                        this.slot = resultcard;
                    }
                    else selectedImage.gameObject.SetActive(false);
                    resultcard.txt_cardNumbers.text = "";
                    resultcard.selectable.SubmitEvent.RemoveAllListeners();
                    resultcard.selectable.SubmitEvent.AddListener((UnityAction<BaseEventData>)(e => this.OnClickCardMultChoice(resultcard)));
                    resultcard.selectable.SelectEvent.RemoveAllListeners();
                    resultcard.selectable.SelectEvent.AddListener((UnityAction<BaseEventData>)(e => this.OnPointerEnterMultChoice(resultcard)));
                    resultcard.selectable.DeselectEvent.RemoveAllListeners();
                    resultcard.selectable.DeselectEvent.AddListener((UnityAction<BaseEventData>)(e => this.OnPointerExit(resultcard)));
                    resultcard.gameObject.SetActive(true);
                }
            }
        }

        public void DefaultOk()
        {
        }

        public void DefaultNo()
        {
            UISoundManager.instance.PlayEffectSound(UISoundType.Ui_Cancel);
            Singleton<MysteryManager>.Instance.EndMystery(this);
        }
        #endregion

        #region --- Battle hooks ---


        public void OnClickOk()
        {
            if (!this.binahSpecialUpgrade && (this.metadata == null || this.slot == null))
            {
                try
                {
                    UIAlarmPopup.instance.SetAlarmText(TextDataModel.GetText("CardCheckPopUp_CannotUpgrade"));
                }
                catch { /* alarm optional */ }
                this.DefaultNo();
                return;
            }
            if (this.slot != null)
                CardAddVfx.RunCardVfx(this.slot);
            if (this.okdele == null)
                this.DefaultOk();
            else
                this.okdele(this);
        }

        public void OnClickNo()
        {
            if (this.nodele == null)
                this.DefaultNo();
            else
                this.nodele(this);
        }

        public void OnPointerEnter(LogLikeMod.UILogCardSlot CardSlot)
        {
            LogLikeMod.UILogBattleDiceCardUI instance = LogLikeMod.UILogBattleDiceCardUI.Instance;
            instance.transform.SetParent(CardSlot.transform.parent);
            instance.gameObject.SetActive(true);
            instance.SetCard(BattleDiceCardModel.CreatePlayingCard(CardSlot._cardModel.ClassInfo));
            instance.transform.localPosition = CardSlot.transform.localPosition + ((double)CardSlot.transform.localPosition.x > 0.0 ? new Vector3(-270f, -150f) : new Vector3(270f, -150f));
            instance.transform.localScale = new Vector3(0.25f, 0.25f);
            instance.gameObject.layer = LayerMask.NameToLayer("UI");
        }

        public void OnPointerEnterMultChoice(LogLikeMod.UILogCardSlot CardSlot)
        {
            LogLikeMod.UILogBattleDiceCardUI instance = LogLikeMod.UILogBattleDiceCardUI.Instance;
            instance.transform.SetParent(CardSlot.transform.parent);
            instance.gameObject.SetActive(true);
            instance.SetCard(BattleDiceCardModel.CreatePlayingCard(CardSlot._cardModel.ClassInfo));
            instance.transform.localPosition = CardSlot.transform.localPosition + ((double)CardSlot.transform.localPosition.x > 0.0 ? new Vector3(-270f, -150f) : new Vector3(270f, -150f));
            instance.transform.localScale = new Vector3(0.25f, 0.25f);
            instance.gameObject.layer = LayerMask.NameToLayer("UI");
        }

        public void OnPointerExit(LogLikeMod.UILogCardSlot CardSlot)
        {
            if (LogLikeMod.UILogBattleDiceCardUI.Instance == null)
                return;
            LogLikeMod.UILogBattleDiceCardUI.Instance.gameObject.SetActive(false);
        }
        public void OnClickCardMultChoice(LogLikeMod.UILogCardSlot CardSlot)
        {
            string cardNum = CardSlot.gameObject.name.Substring(16);
            foreach (var obj in this.FrameObj)
            {
                if (obj.Key.StartsWith("selectedImage_"))
                {
                    if (obj.Key.Substring(14) == cardNum)
                        obj.Value.gameObject.SetActive(true);
                    else
                        obj.Value.gameObject.SetActive(false);
                }
            }
            UISoundManager.instance.PlayEffectSound(UISoundType.Card_Apply);
            UpgradeMetadata.UnpackPid(CardSlot._cardModel.ClassInfo.id.packageId, out this.metadata);
            this.slot = CardSlot;
        }

        public void OnClickCard(DiceCardItemModel card)
        {

        }

        public delegate void CheckResult(MysteryModel_UpgradeCheckPopup mystery);
        #endregion

    }
}
