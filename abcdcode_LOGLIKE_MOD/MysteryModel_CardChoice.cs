// -----------------------------------------------------------------------------
// Mystery / event node model: MysteryModel_CardChoice
// Namespace/file: ruina-roguelike-reborn-main\abcdcode_LOGLIKE_MOD\MysteryModel_CardChoice.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
using GameSave;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace abcdcode_LOGLIKE_MOD
{

    /// <summary>Mystery node model: MysteryModel_CardChoice</summary>

    public class MysteryModel_CardChoice : MysteryBase
    {
        public bool frameinit = false;
        public static Vector2[] cardlocation = new Vector2[10]
        {
    new Vector2(-400f, 200f),
    new Vector2(-200f, 200f),
    new Vector2(0.0f, 200f),
    new Vector2(200f, 200f),
    new Vector2(400f, 200f),
    new Vector2(-400f, -100f),
    new Vector2(-200f, -100f),
    new Vector2(0.0f, -100f),
    new Vector2(200f, -100f),
    new Vector2(400f, -100f)
        };
        public List<LogLikeMod.UILogCardSlot> cardslots;
        public string titledesc;
        public int curindex;
        public List<DiceCardItemModel> cardlist;
        public MysteryModel_CardChoice.ChoiceResult resultdel;
        #region --- Save / load ---


        public override void LoadFromSaveData(SaveData savedata)
        {
        }
        #endregion

        #region --- Battle hooks ---


        public static MysteryModel_CardChoice PopupCardChoice(
          List<DiceCardItemModel> cardlist,
          MysteryModel_CardChoice.ChoiceResult dele,
          MysteryModel_CardChoice.ChoiceDescType desctype)
        {
            string desc = string.Empty;
            switch (desctype)
            {
                case MysteryModel_CardChoice.ChoiceDescType.ChooseDesc:
                    desc = TextDataModel.GetText("CardChoicePopUp_ChooseDesc");
                    break;
                case MysteryModel_CardChoice.ChoiceDescType.RemoveDesc:
                    desc = TextDataModel.GetText("CardChoicePopUp_RemoveDesc");
                    break;
                case MysteryModel_CardChoice.ChoiceDescType.TransformDesc:
                    desc = TextDataModel.GetText("CardChoicePopUp_TransformDesc");
                    break;
                case MysteryModel_CardChoice.ChoiceDescType.RewardDesc:
                    desc = TextDataModel.GetText("CardChoicePopUp_RewardDesc");
                    break;
                case MysteryModel_CardChoice.ChoiceDescType.UpgradeDesc:
                    desc = TextDataModel.GetText("CardChoicePopUp_UpgradeDesc");
                    break;
            }
            return MysteryModel_CardChoice.PopupCardChoice(cardlist, dele, desc);
        }

        public static MysteryModel_CardChoice PopupCardChoice(
          List<DiceCardItemModel> cardlist,
          MysteryModel_CardChoice.ChoiceResult dele,
          string desc)
        {
            cardlist = new List<DiceCardItemModel>(cardlist ?? new List<DiceCardItemModel>());
            cardlist.RemoveAll(x => x == null || x.num <= 0);
            if (cardlist.Count == 0)
            {
                Debug.LogWarning("[RMR] PopupCardChoice: empty card list — refusing interrupt to avoid softlock.");
                try
                {
                    UI.UIAlarmPopup.instance.SetAlarmText(TextDataModel.GetText("CardCheckPopUp_CannotUpgrade"));
                }
                catch { /* alarm optional */ }
                return null;
            }
            MysteryModel_CardChoice mystery = new MysteryModel_CardChoice();
            mystery.cardlist = cardlist;
            mystery.resultdel = dele;
            mystery.curindex = 0;
            mystery.titledesc = desc;
            Singleton<MysteryManager>.Instance.AddInterrupt(mystery);
            return mystery;
        }
        #endregion

        #region --- Other helpers ---


        public void FrameInit()
        {
            Image image = ModdingUtils.CreateImage(LogLikeMod.LogUIObjs[95].transform, "MysteryPanel_transparent", new Vector2(1f, 1f), new Vector2(0.0f, 0.0f));
            this.FrameObj.Add("Frame", image.gameObject);
            TextMeshProUGUI textTmp1 = ModdingUtils.CreateText_TMP(image.transform, new Vector2(140f, -100f), 45, new Vector2(0.0f, 0.0f), new Vector2(1f, 1f), new Vector2(0.0f, 0.0f), TextAlignmentOptions.TopLeft, LogLikeMod.DefFontColor, LogLikeMod.DefFont_TMP);
            textTmp1.text = this.titledesc;
            this.FrameObj.Add("Title", textTmp1.gameObject);
            TextMeshProUGUI textTmp2 = ModdingUtils.CreateText_TMP(image.transform, new Vector2(0.0f, -320f), 30, new Vector2(0.0f, 0.0f), new Vector2(1f, 1f), new Vector2(0.0f, 0.0f), TextAlignmentOptions.Midline, LogLikeMod.DefFontColor, LogLikeMod.DefFont_TMP);
            TextMeshProUGUI textMeshProUgui = textTmp2;
            int num = this.curindex + 1;
            string str1 = num.ToString();
            num = (this.cardlist.Count - 1) / 10 + 1;
            string str2 = num.ToString();
            string str3 = $"{str1} / {str2}";
            textMeshProUgui.text = str3;
            this.FrameObj.Add("CurCount", textTmp2.gameObject);
            Button button1 = ModdingUtils.CreateButton(image.transform, "MysteryArrow_LR", new Vector2(-1f, 1f), new Vector2(100f, -320f), new Vector2(40f, 40f));
            this.FrameObj.Add("NextArrow", button1.gameObject);
            Button.ButtonClickedEvent buttonClickedEvent1 = new Button.ButtonClickedEvent();
            buttonClickedEvent1.AddListener(new UnityAction(this.OnClickNext));
            button1.onClick = buttonClickedEvent1;
            Button button2 = ModdingUtils.CreateButton(image.transform, "MysteryArrow_LR", new Vector2(1f, 1f), new Vector2(-100f, -320f), new Vector2(40f, 40f));
            this.FrameObj.Add("PrevArrow", button2.gameObject);
            Button.ButtonClickedEvent buttonClickedEvent2 = new Button.ButtonClickedEvent();
            buttonClickedEvent2.AddListener(new UnityAction(this.OnClickPrev));
            button2.onClick = buttonClickedEvent2;
        }

        public override void SwapFrame(int id)
        {
            if (!this.frameinit)
            {
                this.FrameInit();
                this.frameinit = true;
            }
            GameObject gameObject = this.FrameObj["Frame"];
            this.FrameObj["NextArrow"].SetActive(this.cardlist.Count > this.curindex * 10 + 10);
            this.FrameObj["PrevArrow"].SetActive(this.curindex > 0);
            this.FrameObj["CurCount"].GetComponent<TextMeshProUGUI>().text = $"{(this.curindex + 1).ToString()} / {((this.cardlist.Count - 1) / 10 + 1).ToString()}";
            int index1 = 0;
            if (this.cardslots == null)
            {
                this.cardslots = new List<LogLikeMod.UILogCardSlot>();
                for (int index2 = 0; index2 < 10; ++index2)
                {
                    LogLikeMod.UILogCardSlot uiLogCardSlot = LogLikeMod.UILogCardSlot.SlotCopyingByOrig();
                    uiLogCardSlot.transform.SetParent(gameObject.transform);
                    uiLogCardSlot.transform.localScale = new Vector3(1.5f, 1.5f);
                    uiLogCardSlot.transform.localPosition = (Vector3)MysteryModel_CardChoice.cardlocation[index2];
                    this.cardslots.Add(uiLogCardSlot);
                }
            }
            for (; index1 < 10; ++index1)
            {
                if (this.cardlist.Count <= index1 + this.curindex * 10)
                {
                    this.cardslots[index1].gameObject.SetActive(false);
                }
                else
                {
                    DiceCardItemModel info = this.cardlist[index1 + this.curindex * 10];
                    LogLikeMod.UILogCardSlot CardSlot = this.cardslots[index1];
                    CardSlot.SetData(info);
                    // Upgrade is by card type/kind — hide stack counts (x99 etc). Other choices keep counts.
                    if (IsUpgradeChoiceTitle())
                        CardSlot.txt_cardNumbers.text = "";
                    else
                        CardSlot.txt_cardNumbers.text = "x" + info.num.ToString();
                    CardSlot.selectable.SubmitEvent.RemoveAllListeners();
                    CardSlot.selectable.SubmitEvent.AddListener((UnityAction<BaseEventData>)(e => this.OnClickCard(info)));
                    CardSlot.selectable.SelectEvent.RemoveAllListeners();
                    CardSlot.selectable.SelectEvent.AddListener((UnityAction<BaseEventData>)(e => this.OnPointerEnter(CardSlot)));
                    CardSlot.selectable.DeselectEvent.RemoveAllListeners();
                    CardSlot.selectable.DeselectEvent.AddListener((UnityAction<BaseEventData>)(e => this.OnPointerExit(CardSlot)));
                    CardSlot.gameObject.SetActive(true);
                }
            }
        }
        #endregion

        #region --- Battle hooks ---


        public void OnClickPrev()
        {
            --this.curindex;
            this.SwapFrame(0);
        }

        public void OnClickNext()
        {
            ++this.curindex;
            this.SwapFrame(0);
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

        public void OnPointerExit(LogLikeMod.UILogCardSlot CardSlot)
        {
            if (!(LogLikeMod.UILogBattleDiceCardUI.Instance != null))
                return;
            LogLikeMod.UILogBattleDiceCardUI.Instance.gameObject.SetActive(false);
        }

        public void OnClickCard(DiceCardItemModel card) => this.resultdel(this, card);
        #endregion

        #region --- Getters / setters / checks ---


        /// <summary>
        /// Upgrade popup shows one entry per card kind; stack numbers are misleading.
        /// Detect by title text (localized) or upgrade-desc key fallbacks.
        /// </summary>
        private bool IsUpgradeChoiceTitle()
        {
            if (string.IsNullOrEmpty(this.titledesc))
                return false;
            try
            {
                string upgrade = TextDataModel.GetText("CardChoicePopUp_UpgradeDesc");
                if (!string.IsNullOrEmpty(upgrade) && this.titledesc == upgrade)
                    return true;
            }
            catch { }
            // zh/en fallbacks if localization missing
            if (this.titledesc.Contains("\u5347\u7ea7") || this.titledesc.IndexOf("upgrade", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return false;
        }

        /// <summary>enum ChoiceDescType</summary>

        public enum ChoiceDescType
        {
            ChooseDesc,
            RemoveDesc,
            TransformDesc,
            RewardDesc,
            UpgradeDesc,
        }

        public delegate void ChoiceResult(MysteryModel_CardChoice mystery, DiceCardItemModel model);
        #endregion

    }
}
