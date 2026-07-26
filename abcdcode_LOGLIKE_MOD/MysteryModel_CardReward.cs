// -----------------------------------------------------------------------------
// Mystery / event node model: MysteryModel_CardReward
// Namespace/file: ruina-roguelike-reborn-main\abcdcode_LOGLIKE_MOD\MysteryModel_CardReward.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
using GameSave;
using LOR_DiceSystem;
using RogueLike_Mod_Reborn;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace abcdcode_LOGLIKE_MOD
{

    /// <summary>Mystery node model: MysteryModel_CardReward</summary>

    public class MysteryModel_CardReward : MysteryBase
    {
        public MysteryModel_CardReward.ChoiceResult result;
        public List<DiceCardXmlInfo> choicelist;
        public LorId curRewardid;
        public MysteryModel_CardReward.State curState = MysteryModel_CardReward.State.CardList;
        public static Dictionary<int, Vector2[]> ChoiceShape;
        #region --- Save / load ---


        public override void LoadFromSaveData(SaveData savedata)
        {
        }

        public static void AutoSave(DiceCardXmlInfo card) => LoguePlayDataSaver.SavePlayData_Menu();

        public static MysteryModel_CardReward PopupCardReward_AutoSave()
        {
            return MysteryModel_CardReward.PopupCardReward(new MysteryModel_CardReward.ChoiceResult(MysteryModel_CardReward.AutoSave));
        }
        #endregion

        #region --- Battle hooks ---


        public static MysteryModel_CardReward PopupCardReward(MysteryModel_CardReward.ChoiceResult result = null)
        {
            MysteryModel_CardReward mystery = new MysteryModel_CardReward();
            mystery.result = result;
            Singleton<MysteryManager>.Instance.AddInterrupt(mystery);
            return mystery;
        }

        public MysteryModel_CardReward()
        {
            if (MysteryModel_CardReward.ChoiceShape != null)
                return;
            MysteryModel_CardReward.ChoiceShape = new Dictionary<int, Vector2[]>();
            Vector2[] vector2Array1 = new Vector2[1]
            {
      new Vector2(0.0f, 100f)
            };
            MysteryModel_CardReward.ChoiceShape.Add(1, vector2Array1);
            Vector2[] vector2Array2 = new Vector2[2]
            {
      new Vector2(-200f, 100f),
      new Vector2(200f, 100f)
            };
            MysteryModel_CardReward.ChoiceShape.Add(2, vector2Array2);
            Vector2[] vector2Array3 = new Vector2[3]
            {
      new Vector2(-300f, 100f),
      new Vector2(0.0f, 100f),
      new Vector2(300f, 100f)
            };
            MysteryModel_CardReward.ChoiceShape.Add(3, vector2Array3);
            Vector2[] vector2Array4 = new Vector2[4]
            {
      new Vector2(-360f, 100f),
      new Vector2(-120f, 100f),
      new Vector2(120f, 100f),
      new Vector2(360f, 100f)
            };
            MysteryModel_CardReward.ChoiceShape.Add(4, vector2Array4);
            Vector2[] vector2Array5 = new Vector2[5]
            {
      new Vector2(-400f, 100f),
      new Vector2(-200f, 100f),
      new Vector2(0.0f, 100f),
      new Vector2(200f, 100f),
      new Vector2(400f, 100f)
            };
            MysteryModel_CardReward.ChoiceShape.Add(5, vector2Array5);
            Vector2[] vector2Array6 = new Vector2[6]
            {
      new Vector2(-300f, 250f),
      new Vector2(0.0f, 250f),
      new Vector2(300f, 250f),
      new Vector2(-300f, -50f),
      new Vector2(0.0f, -50f),
      new Vector2(300f, -50f)
            };
            MysteryModel_CardReward.ChoiceShape.Add(6, vector2Array6);
            Vector2[] vector2Array7 = new Vector2[7]
            {
      new Vector2(-360f, 250f),
      new Vector2(-120f, 250f),
      new Vector2(120f, 250f),
      new Vector2(360f, 250f),
      new Vector2(-300f, -50f),
      new Vector2(0.0f, -50f),
      new Vector2(300f, -50f)
            };
            MysteryModel_CardReward.ChoiceShape.Add(7, vector2Array7);
            Vector2[] vector2Array8 = new Vector2[8]
            {
      new Vector2(-360f, 250f),
      new Vector2(-120f, 250f),
      new Vector2(120f, 250f),
      new Vector2(360f, 250f),
      new Vector2(-360f, -50f),
      new Vector2(-120f, -50f),
      new Vector2(120f, -50f),
      new Vector2(360f, -50f)
            };
            MysteryModel_CardReward.ChoiceShape.Add(8, vector2Array8);
            Vector2[] vector2Array9 = new Vector2[9]
            {
      new Vector2(-400f, 250f),
      new Vector2(-200f, 250f),
      new Vector2(0.0f, 250f),
      new Vector2(200f, 250f),
      new Vector2(400f, 250f),
      new Vector2(-360f, -50f),
      new Vector2(-120f, -50f),
      new Vector2(120f, -50f),
      new Vector2(360f, -50f)
            };
            MysteryModel_CardReward.ChoiceShape.Add(9, vector2Array9);
            Vector2[] vector2Array10 = new Vector2[10]
            {
      new Vector2(-400f, 250f),
      new Vector2(-200f, 250f),
      new Vector2(0.0f, 250f),
      new Vector2(200f, 250f),
      new Vector2(400f, 250f),
      new Vector2(-400f, -50f),
      new Vector2(-200f, -50f),
      new Vector2(0.0f, -50f),
      new Vector2(200f, -50f),
      new Vector2(400f, -50f)
            };
            MysteryModel_CardReward.ChoiceShape.Add(10, vector2Array10);
        }
        #endregion

        #region --- Other helpers ---


        public override void SwapFrame(int id)
        {
            this.RemoveCurFrame();
            if (LogLikeMod.rewards.Count == 0 && this.curState == MysteryModel_CardReward.State.CardList)
            {
                Singleton<MysteryManager>.Instance.EndMystery((MysteryBase)this);
            }
            else
            {
                Image image = ModdingUtils.CreateImage(LogLikeMod.LogUIObjs[90].transform, "MysteryPanel_transparent", new Vector2(1f, 1f), new Vector2(0.0f, 0.0f));
                // 1920x1080 backdrop. It is decoration, but Image defaults raycastTarget to true, so
                // if anything ever paints it above the buttons on LogUIObjs[100] it also eats their
                // clicks. Defence in depth alongside the canvas-order restore in LogLikePatches.
                try { image.raycastTarget = false; } catch { /* ignore */ }
                this.FrameObj.Add("Frame", image.gameObject);
                TextMeshProUGUI textTmp1 = ModdingUtils.CreateText_TMP(image.transform, new Vector2(140f, -30f), 45, new Vector2(0.0f, 0.0f), new Vector2(1f, 1f), new Vector2(0.0f, 0.0f), TextAlignmentOptions.TopLeft, LogLikeMod.DefFontColor, LogLikeMod.DefFont_TMP);
                textTmp1.text = TextDataModel.GetText("BattleEnd_CardReward");
                this.FrameObj.Add("Title", textTmp1.gameObject);
                if (this.curState == MysteryModel_CardReward.State.CardList)
                {
                    Button button = ModdingUtils.CreateButton(LogLikeMod.LogUIObjs[100].transform, "MysteryButton_Enable", new Vector2(1f, 1f), new Vector2(590f, -480f), new Vector2(350f, 95f));
                    TextMeshProUGUI textTmp2 = ModdingUtils.CreateText_TMP(button.transform, new Vector2(-30f, 0.0f), 25, new Vector2(0.0f, 0.0f), new Vector2(1f, 1f), new Vector2(0.0f, 0.0f), TextAlignmentOptions.Midline, LogLikeMod.DefFontColor, LogLikeMod.DefFont_TMP);
                    this.FrameObj.Add("LeaveButton", button.gameObject);
                    button.onClick.AddListener(new UnityAction(this.LeaveReward));
                    textTmp2.text = TextDataModel.GetText("ui_selectskip");
                    textTmp2.transform.Rotate(0.0f, 0.0f, 2.5f);
                    Dictionary<LorId, int> dictionary = new Dictionary<LorId, int>();
                    foreach (DropBookXmlInfo reward in LogLikeMod.rewards)
                    {
                        if (dictionary.ContainsKey(reward.id))
                            dictionary[reward.id]++;
                        else
                            dictionary[reward.id] = 1;
                    }
                    int count = dictionary.Count;
                    int id1 = 0;
                    foreach (KeyValuePair<LorId, int> keyValuePair in dictionary)
                    {
                        KeyValuePair<LorId, int> pair = keyValuePair;
                        LogLikeMod.LogUIBookSlot logUiBookSlot = LogLikeMod.LogUIBookSlot.SlotCopying();
                        logUiBookSlot.transform.SetParent(image.transform);
                        logUiBookSlot.SetData_DropBook(pair.Key, pair.Value);
                        logUiBookSlot.transform.localScale = new Vector3(1.2f, 1.2f);
                        logUiBookSlot.transform.localPosition = (Vector3)this.GetChoiceShape(count, id1);
                        logUiBookSlot.gameObject.SetActive(true);
                        logUiBookSlot.selectable.SubmitEvent.RemoveAllListeners();
                        logUiBookSlot.selectable.SubmitEvent.AddListener((UnityAction<BaseEventData>)(e => this.OnClickDropCard(pair.Key)));
                        ++id1;
                    }
                }
                else
                {
                    Button button = ModdingUtils.CreateButton(LogLikeMod.LogUIObjs[100].transform, "MysteryButton_Enable", new Vector2(1f, 1f), new Vector2(590f, -480f), new Vector2(350f, 95f));
                    TextMeshProUGUI textTmp3 = ModdingUtils.CreateText_TMP(button.transform, new Vector2(-30f, 0.0f), 25, new Vector2(0.0f, 0.0f), new Vector2(1f, 1f), new Vector2(0.0f, 0.0f), TextAlignmentOptions.Midline, LogLikeMod.DefFontColor, LogLikeMod.DefFont_TMP);
                    this.FrameObj.Add("SkipButton", button.gameObject);
                    button.onClick.AddListener(new UnityAction(this.SkipReward));
                    textTmp3.text = TextDataModel.GetText("ui_selectskip");
                    textTmp3.transform.Rotate(0.0f, 0.0f, 2.5f);
                    List<DiceCardXmlInfo> diceCardXmlInfoList = RewardingModel.PickUpCards(Singleton<CardDropValueList>.Instance.GetData(this.curRewardid));
                    if (diceCardXmlInfoList == null || diceCardXmlInfoList.Count == 0)
                    {
                        this.HandleEmptyCardChoices();
                        return;
                    }
                    int count = diceCardXmlInfoList.Count;
                    this.choicelist = new List<DiceCardXmlInfo>();
                    for (int index = 0; index < count; ++index)
                    {
                        DiceCardXmlInfo info = diceCardXmlInfoList[index];
                        this.choicelist.Add(info);
                        LogLikeMod.UILogCardSlot CardSlot = LogLikeMod.UILogCardSlot.SlotCopyingByOrig();
                        CardSlot.transform.SetParent(image.transform);
                        CardSlot.transform.localScale = new Vector3(1.5f, 1.5f);
                        CardSlot.transform.localPosition = (Vector3)this.GetChoiceShape(count, index);
                        CardSlot.SetData(new DiceCardItemModel(info));
                        CardSlot.selectable.SubmitEvent.RemoveAllListeners();
                        CardSlot.selectable.SubmitEvent.AddListener((UnityAction<BaseEventData>)(e => this.OnClickCard(CardSlot)));
                        CardSlot.selectable.SelectEvent.RemoveAllListeners();
                        CardSlot.selectable.SelectEvent.AddListener((UnityAction<BaseEventData>)(e => this.OnPointerEnter(CardSlot)));
                        CardSlot.selectable.DeselectEvent.RemoveAllListeners();
                        CardSlot.selectable.DeselectEvent.AddListener((UnityAction<BaseEventData>)(e => this.OnPointerExit(CardSlot)));
                        CardSlot.txt_cardNumbers.text = "";
                    }
                }
            }
        }
        #endregion

        #region --- Battle hooks ---


        private void HandleEmptyCardChoices()
        {
            Debug.Log($"[RMR CardReward] Drop book produced no card choices: {this.curRewardid.packageId}:{this.curRewardid.id}");
            // An empty pool is a skip from the player's point of view — they were never offered a
            // choice. Upstream still drew the Skip button here, so the click counted; RMR's post
            // filter (owned / upgraded / duplicate originalId) empties the pool more and more as a
            // run goes on, and silently swallowing those made Pierre's meat-pie quest, which needs
            // five skips, impossible to finish. Effects like ConceptConverter lose their payout too.
            try
            {
                Singleton<GlobalLogueEffectManager>.Instance.OnSkipCardRewardChoose(
                    this.choicelist ?? new List<DiceCardXmlInfo>());
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR CardReward] empty-pool skip callback failed: " + ex.Message);
            }
            this.CompleteRewardFlow();
        }
        #endregion

        #region --- Other helpers ---


        private void CompleteRewardFlow()
        {
            if (LogLikeMod.UILogBattleDiceCardUI.Instance != null)
                LogLikeMod.UILogBattleDiceCardUI.Instance.gameObject.SetActive(false);
            this.RemoveCurFrame();
            this.curState = MysteryModel_CardReward.State.CardList;
            this.SwapFrame(0);
        }
        #endregion

        #region --- Battle hooks ---


        public void OnClickDropCard(LorId id)
        {
            LogLikeMod.rewards.Remove(LogLikeMod.rewards.Find((Predicate<DropBookXmlInfo>)(x => x.id == id)));
            this.curRewardid = id;
            this.curState = MysteryModel_CardReward.State.CardChoice;
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

        public void OnClickCard(LogLikeMod.UILogCardSlot CardSlot)
        {
            DiceCardXmlInfo card = CardSlot._cardModel.ClassInfo;
            Singleton<GlobalLogueEffectManager>.Instance.OnPickCardReward(this.choicelist, card);
            LogueBookModels.AddCard(card.id);
            this.curState = MysteryModel_CardReward.State.CardList;
            CardAddVfx.RunCardVfx(CardSlot);
            this.SwapFrame(0);
            if (this.result == null)
                return;
            this.result(card);
        }
        #endregion

        #region --- Other helpers ---


        public void SkipReward()
        {
            Singleton<GlobalLogueEffectManager>.Instance.OnSkipCardRewardChoose(this.choicelist);
            this.curState = MysteryModel_CardReward.State.CardList;
            this.SwapFrame(0);
        }

        public void LeaveReward()
        {
            LogLikeMod.rewards.Clear();
            Singleton<MysteryManager>.Instance.EndMystery((MysteryBase)this);
        }
        #endregion

        #region --- Getters / setters / checks ---


        public Vector2 GetChoiceShape(int num, int id) => MysteryModel_CardReward.ChoiceShape[num][id];

        /// <summary>enum State</summary>

        public enum State
        {
            CardList,
            CardChoice,
        }

        public delegate void ChoiceResult(DiceCardXmlInfo card);
        #endregion

    }
}
