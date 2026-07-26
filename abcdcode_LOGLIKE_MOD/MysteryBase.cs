// -----------------------------------------------------------------------------
// Library of Ruina mod script: MysteryBase
// Namespace/file: ruina-roguelike-reborn-main\abcdcode_LOGLIKE_MOD\MysteryBase.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
using GameSave;
using HarmonyLib;
using LOR_XML;
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RogueLike_Mod_Reborn;
namespace abcdcode_LOGLIKE_MOD
{

    /// <summary>MysteryBase</summary>

    public class MysteryBase
    {
        /// <summary>
        /// Utilized to determine metadata for picking abnormality pages.
        /// </summary>
        public static MysteryBase.MysteryAbnormalInfo curinfo;
        /// <summary>
        /// The animator object for MysteryBase fade-ins/outs.
        /// </summary>
        public AnimUpdater animator;
        /// <summary>
        /// A dictionary containing UI elements created for the event interface.<br></br>
        /// When making custom UI elements, please add them to this list.
        /// </summary>
        public Dictionary<string, GameObject> FrameObj;
        /// <summary>
        /// The event's respective XML info.
        /// </summary>
        public MysteryXmlInfo xmlinfo;
        public MysteryFrameInfo curFrame;
        public RogueMysteryXmlInfo loc;
        #region --- Save / load ---


        public virtual void LoadFromSaveData(SaveData savedata)
        {
            SaveData data = savedata.GetData("Frame");
            if (data == null)
                return;
            this.SwapFrame(data.GetIntSelf());
        }

        public virtual SaveData GetSaveData()
        {
            SaveData data = new SaveData();
            data.AddData("Frame", this.curFrame.FrameID);
            return data;
        }
        #endregion

        #region --- Other helpers ---


        public static void RemoveStageList(Predicate<LogueStageInfo> predicate, ChapterGrade grade)
        {
            LogueBookModels.RemainStageList[grade].Remove(LogueBookModels.RemainStageList[grade].Find(predicate));
            LogLikeMod.ResetNextStage();
        }

        public static void AddStageList(LogueStageInfo info, ChapterGrade grade)
        {
            LogueBookModels.RemainStageList[grade].Add(info);
            LogLikeMod.ResetNextStage();
        }

        public static void SetNextStageCustom(LorId stageid, StageType stagetype = StageType.Custom)
        {
            LogLikeMod.SetNextStage(stageid, stagetype, NextStageSetType.Custom);
            LogLikeMod.nextlist.Clear();
        }
        #endregion

        #region --- Save / load ---


        public static void LoadGetAbnomalityPanel(List<EmotionCardXmlInfo> cards, int level)
        {
            foreach (EmotionCardXmlInfo card in cards)
            {
                if (card == null || card.Script == null || card.Script.Count == 0)
                    continue;
                RewardPassiveInfo rewardInfo = RewardingModel.FindRewardInfo(card);
                if (rewardInfo != null)
                    RMRAbnormalityUnlockManager.EnsureVanillaEmotionPresentation(rewardInfo, card);
                PickUpModelBase pickUp = LogLikeMod.FindPickUp(card.Script[0]);
                PickUpModel_RMRVanillaEmotion.InjectResolvedDesc(card, pickUp);
            }
            UIGetAbnormalityPanel instance = UIGetAbnormalityPanel.instance;
            FieldInfo field1 = ModdingUtils.GetField("currentFloor", instance);
            GameObject fieldValue1 = ModdingUtils.GetFieldValue<GameObject>("ob_blackbgForKeterCompleterOpen", instance);
            FieldInfo field2 = ModdingUtils.GetField("currentSettinfCardCount", instance);
            FieldInfo field3 = ModdingUtils.GetField("sep", instance);
            FieldInfo field4 = ModdingUtils.GetField("isbinahhokmacompletecheck", instance);
            Image fieldValue2 = ModdingUtils.GetFieldValue<Image>("img_floorIcon", instance);
            TextMeshProUGUI fieldValue3 = ModdingUtils.GetFieldValue<TextMeshProUGUI>("txt_floorname", instance);
            TextMeshProUGUI fieldValue4 = ModdingUtils.GetFieldValue<TextMeshProUGUI>("txt_level", instance);
            GameObject fieldValue5 = ModdingUtils.GetFieldValue<GameObject>("controllerGuide", instance);
            GameObject fieldValue6 = ModdingUtils.GetFieldValue<GameObject>("AbnormalitiesRoot", instance);
            GameObject fieldValue7 = ModdingUtils.GetFieldValue<GameObject>("EgoCardsRoot", instance);
            TextMeshProUGUI fieldValue8 = ModdingUtils.GetFieldValue<TextMeshProUGUI>("txt_getabcardtxt", instance);
            TextMeshProUGUI fieldValue9 = ModdingUtils.GetFieldValue<TextMeshProUGUI>("txt_getegocardtxt", instance);
            List<UIEmotionPassiveCardInven> fieldValue10 = ModdingUtils.GetFieldValue<List<UIEmotionPassiveCardInven>>("AbnormalityList", instance);
            Animator fieldValue11 = ModdingUtils.GetFieldValue<Animator>("anim", instance);
            field1.SetValue(instance, null);
            fieldValue1.SetActive(false);
            field2.SetValue(instance, cards.Count);
            instance.Open();
            field3.SetValue(instance, SephirahType.None);
            field4.SetValue(instance, false);
            fieldValue2.sprite = UISpriteDataManager.instance.GetStoryIcon("Chapter1").icon;
            fieldValue3.text = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText("ui_LogueLikeFloor");
            switch (level)
            {
                case 1:
                    fieldValue4.text = "I";
                    break;
                case 2:
                    fieldValue4.text = "II";
                    break;
                case 3:
                    fieldValue4.text = "III";
                    break;
                case 4:
                    fieldValue4.text = "IV";
                    break;
                case 5:
                    fieldValue4.text = "V";
                    break;
                case 6:
                    fieldValue4.text = "VI";
                    break;
                default:
                    fieldValue4.text = "X";
                    break;
            }
            instance.SetColor(UIColorManager.Manager.GetSephirahGlowColor(SephirahType.None));
            fieldValue5.SetActive(false);
            fieldValue6.SetActive(true);
            fieldValue7.SetActive(false);
            fieldValue8.gameObject.SetActive(true);
            fieldValue9.gameObject.SetActive(false);
            instance.selectablePanel.ChildSelectable = instance.abpanelSelectable;
            UIControlManager.Instance.SelectSelectableForcely(instance.selectablePanel.ChildSelectable);
            List<EmotionCardXmlInfo> emotionCardXmlInfoList = cards;
            for (int index = 0; index < emotionCardXmlInfoList.Count; ++index)
            {
                if (index > fieldValue10.Count)
                    return;
                fieldValue10[index].Init(emotionCardXmlInfoList[index]);
            }
            foreach (UIEmotionPassiveCardInven passiveCardInven in fieldValue10)
                passiveCardInven.SetActiveDetail(false);
            instance.GetType().GetMethod("SetDefault", AccessTools.all).Invoke(instance, (object[])null);
            fieldValue11.SetTrigger("Reveal");
        }
        #endregion

        #region --- Mystery lifecycle ---


        public virtual void Init()
        {
            if (this.xmlinfo != null)
                this.loc = RogueMysteryXmlList.Instance.GetLocalizedMystery(this.xmlinfo.StageId);
            this.FrameObj = new Dictionary<string, GameObject>();
            this.SwapFrame(0);
            LoguePlayDataSaver.LoadMystery(this);
        }
        #endregion

        #region --- Other helpers ---


        public static MysteryBase FindMystery(string script)
        {
            foreach (Assembly assem in LogLikeMod.GetAssemList())
            {
                foreach (System.Type type in LogLikeMod.GetLoadableTypes(assem))
                {
                    if (type.Name == "MysteryModel_" + script.Trim())
                        return Activator.CreateInstance(type) as MysteryBase;
                }
            }
            return new MysteryBase();
        }

        public virtual void SwapFrame(int id)
        {
            this.RemoveCurFrame();
            if (this.xmlinfo == null)
            {
                Debug.LogError("[RMR Mystery] SwapFrame: xmlinfo is null.");
                return;
            }
            this.curFrame = this.xmlinfo.GetFrame(id);
            if (this.curFrame == null)
            {
                Debug.LogError("[RMR Mystery] SwapFrame: frame id=" + id + " missing.");
                return;
            }
            if (this.curFrame.choices == null)
                this.curFrame.choices = new List<MysteryChoiceInfo>();

            // LogUIObjs[90] is the mystery host; without it Init/SwapFrame NRE and normal play dies.
            if (LogLikeMod.LogUIObjs == null || !LogLikeMod.LogUIObjs.ContainsKey(90) || LogLikeMod.LogUIObjs[90] == null)
            {
                Debug.LogError("[RMR Mystery] SwapFrame: LogUIObjs[90] missing — cannot build mystery UI yet.");
                return;
            }

            Transform host = LogLikeMod.LogUIObjs[90].transform;
            try
            {
                string art = string.IsNullOrEmpty(this.curFrame.ArtWork) ? "MysteryPanel" : this.curFrame.ArtWork;
                Image frameArt = null;
                try
                {
                    frameArt = ModdingUtils.CreateImage(host, art, new Vector2(1f, 1f), new Vector2(-280f, 180f), new Vector2(1320f, 743f));
                }
                catch
                {
                    frameArt = ModdingUtils.CreateImage(host, "MysteryPanel", new Vector2(1f, 1f), new Vector2(-280f, 180f), new Vector2(1320f, 743f));
                }
                if (frameArt != null)
                    this.FrameObj.Add("FrameArt", frameArt.gameObject);

                Image image1 = ModdingUtils.CreateImage(host, "MysteryPanel", new Vector2(1f, 1f), new Vector2(0.0f, 0.0f));
                if (image1 == null)
                {
                    Debug.LogError("[RMR Mystery] SwapFrame: MysteryPanel image failed.");
                    return;
                }
                this.FrameObj.Add("Frame", image1.gameObject);
                Image image2 = ModdingUtils.CreateImage(image1.transform, "RMRMysteryTitleBG", new Vector2(1f, 1f), new Vector2(430f, 410f));
                if (image2 == null)
                    image2 = ModdingUtils.CreateImage(image1.transform, "MysteryPanel", new Vector2(1f, 1f), new Vector2(430f, 410f));
                this.FrameObj.Add("TitleBG", image2.gameObject);
                TextMeshProUGUI textTmp1 = ModdingUtils.CreateText_TMP(image2.transform, new Vector2(0.0f, 0.0f), 45, new Vector2(0.0f, 0.0f), new Vector2(1f, 1f), new Vector2(0.0f, 0.0f), TextAlignmentOptions.Midline, LogLikeMod.DefFontColor, LogLikeMod.DefFont_TMP);
                textTmp1.text = this.GetCurFrameTitle() ?? string.Empty;
                this.FrameObj.Add("Title", textTmp1.gameObject);

                int choiceAmount = this.curFrame.choices.Count;
                foreach (MysteryChoiceInfo choice in this.curFrame.choices)
                {
                    if (choice == null)
                        continue;
                    UILogCustomSelectable btn;
                    if (choiceAmount < 5)
                    {
                        btn = ModdingUtils.CreateLogSelectable(image2.transform, "MysteryButton_Enable", new Vector2(1f, 1f), new Vector2(100f, (float)(-200 - choice.ChoiceID * 200)));
                    }
                    else if (choiceAmount < 9)
                    {
                        btn = ModdingUtils.CreateLogSelectable(image2.transform, "MysteryButton_Enable", new Vector2(1f, 1f), new Vector2(-150f + (float)(choice.ChoiceID / 4 * 410), (float)(-200 + (choice.ChoiceID % 4) * -140)), new Vector2(401f, 114f));
                    }
                    else
                    {
                        btn = ModdingUtils.CreateLogSelectable(image2.transform, "MysteryButton_Enable", new Vector2(1f, 1f), new Vector2(-150f + (float)(choice.ChoiceID / 6 * 410), (float)(-200 + (choice.ChoiceID % 6) * -125)), new Vector2(401f, 114f));
                    }
                    if (btn == null)
                        continue;
                    Button.ButtonClickedEvent buttonClickedEvent = new Button.ButtonClickedEvent();
                    int cid = choice.ChoiceID;
                    buttonClickedEvent.AddListener((UnityAction)(() => this.OnClickChoiceCheckAlpha(btn.gameObject, cid)));
                    btn.onClick = buttonClickedEvent;
                    btn.SelectEvent = new UnityEventBasedata();
                    btn.SelectEvent.AddListener((UnityAction<BaseEventData>)(e => this.OnEnterChoice(cid)));
                    btn.DeselectEvent = new UnityEventBasedata();
                    btn.DeselectEvent.AddListener((UnityAction<BaseEventData>)(e => this.OnExitChoice(cid)));
                    this.FrameObj.Add("choice_btn" + choice.ChoiceID.ToString(), btn.gameObject);
                    TextMeshProUGUI textTmp2 = ModdingUtils.CreateText_TMP(btn.transform, new Vector2(20f, 0f), 32 - (choiceAmount >= 12 ? 8 : (choiceAmount - choiceAmount % 4)), new Vector2(0.05f, 0f), new Vector2(0.95f, 1f), new Vector2(0f, 0f), TextAlignmentOptions.Center, LogLikeMod.DefFontColor, LogLikeMod.DefFont_TMP);
                    string text = string.Empty;
                    try
                    {
                        if (loc != null)
                        {
                            var lf = loc.GetFrameById(this.curFrame.FrameID);
                            var lc = lf != null ? lf.GetChoiceById(choice.ChoiceID) : null;
                            text = lc != null && lc.Desc != null
                                ? lc.Desc.ReplaceColorShorthands()
                                : abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText(choice.desc).ReplaceColorShorthands();
                        }
                        else
                            text = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText(choice.desc).ReplaceColorShorthands();
                    }
                    catch
                    {
                        text = choice.desc ?? string.Empty;
                    }
                    textTmp2.text = text ?? string.Empty;
                    textTmp2.transform.Rotate(0.0f, 0.0f, 2.5f);
                    this.FrameObj.Add("Desc" + choice.ChoiceID.ToString(), textTmp2.gameObject);
                }
                this.CreateDia(Math.Max(0, choiceAmount - 1), image1.gameObject);
                if (this.animator == null)
                    this.animator = image1.gameObject.AddComponent<AnimUpdater>();
                this.animator.SetAnim(new MysteryAnimatorDefault(), this);
                try { LoguePlayDataSaver.SaveMystery(this); } catch { }
                try { LoguePlayDataSaver.SavePlayData_Menu(); } catch { }
            }
            catch (Exception ex)
            {
                Debug.LogError("[RMR Mystery] SwapFrame failed: " + ex);
            }
        }

        public virtual void CreateDia(int choicevalue, GameObject curFrame)
        {
            float y = 350f;
            Image image1 = ModdingUtils.CreateImage(curFrame.transform, "MaskSpace", new Vector2(1f, 1f), new Vector2(-440f, -360f), new Vector2(960f, y));
            image1.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            this.FrameObj.Add("Mask", image1.gameObject);
            TextMeshProUGUI textTmp = ModdingUtils.CreateText_TMP(image1.transform, new Vector2(0.0f, 0.0f), 30, new Vector2(0.0f, 0.0f), new Vector2(1f, 1f), new Vector2(0.0f, 0.0f), TextAlignmentOptions.TopLeft, LogLikeMod.DefFontColor, LogLikeMod.DefFont_TMP);
            textTmp.text = this.GetCurFrameDia();
            textTmp.preferredHeight.Log("preferredHeight: " + ((int)textTmp.preferredHeight).ToString());
            float maxDown = textTmp.preferredHeight - y;
            if ((double)maxDown > 0.0)
            {
                MysteryBase.TextUpDown textUpDown = textTmp.gameObject.AddComponent<MysteryBase.TextUpDown>();
                textUpDown.Init(textTmp.gameObject, 0.0f, maxDown);
                Image image2 = ModdingUtils.CreateImage(curFrame.transform, "MysteryArrow", new Vector2(1f, -1f), new Vector2(75f, -200f), new Vector2(40f, 40f));
                MysteryBase.TextUpDownButton textUpDownButton1 = image2.gameObject.AddComponent<MysteryBase.TextUpDownButton>();
                textUpDownButton1.targetGraphic = image2;
                textUpDownButton1.Init(new MysteryBase.TextUpDownButton.DownEvent(textUpDown.ScrollUp));
                this.FrameObj.Add("TextUpDown_Up", textUpDownButton1.gameObject);
                Image image3 = ModdingUtils.CreateImage(curFrame.transform, "MysteryArrow", new Vector2(1f, 1f), new Vector2(75f, -150f - y), new Vector2(40f, 40f));
                MysteryBase.TextUpDownButton textUpDownButton2 = image3.gameObject.AddComponent<MysteryBase.TextUpDownButton>();
                textUpDownButton2.targetGraphic = image3;
                textUpDownButton2.Init(new MysteryBase.TextUpDownButton.DownEvent(textUpDown.ScrollDown));
                this.FrameObj.Add("TextUpDown_Down", textUpDownButton2.gameObject);
            }
            this.FrameObj.Add("Dia", textTmp.gameObject);
        }
        #endregion

        #region --- Choices / results ---


        public void OnClickChoiceCheckAlpha(GameObject btn, int i)
        {
            if ((double)btn.GetComponent<Image>().color.a < 0.75 || btn.GetComponent<Image>().sprite == LogLikeMod.ArtWorks["disabledButton"])
                return;
            this.OnClickChoice(i);
        }
        #endregion

        #region --- Mystery lifecycle ---


        public void ShowDetailCard(int choiceid, LorId cardid)
        {
            GameObject gameObject = this.FrameObj["choice_btn" + choiceid.ToString()];
            LogLikeMod.UILogBattleDiceCardUI instance = LogLikeMod.UILogBattleDiceCardUI.Instance;
            instance.gameObject.SetActive(true);
            instance.SetCard(BattleDiceCardModel.CreatePlayingCard(ItemXmlDataList.instance.GetCardItem(cardid)));
            instance.transform.SetParent(gameObject.transform.parent);
            instance.transform.localPosition = new Vector3(140f, (float)(choiceid * 90 - 120));
            instance.transform.localScale = new Vector3(0.2f, 0.2f);
        }
        #endregion

        #region --- Other helpers ---


        public void HideDetailCard()
        {
            LogLikeMod.UILogBattleDiceCardUI.Instance.gameObject.SetActive(false);
        }
        #endregion

        #region --- Choices / results ---


        public virtual void OnEnterChoice(int choiceid)
        {
            if (!(this.FrameObj["choice_btn" + choiceid].GetComponent<Image>().sprite != LogLikeMod.ArtWorks["disabledButton"]))
                return;
            this.FrameObj["choice_btn" + choiceid].GetComponent<Image>().sprite = LogLikeMod.ArtWorks["MysteryButton_Hover"];
        }

        public virtual void OnExitChoice(int choiceid)
        {
            if (!(this.FrameObj["choice_btn" + choiceid].GetComponent<Image>().sprite != LogLikeMod.ArtWorks["disabledButton"]))
                return;
            this.FrameObj["choice_btn" + choiceid].GetComponent<Image>().sprite = LogLikeMod.ArtWorks["MysteryButton_Enable"];
        }

        public virtual void OnClickChoice(int choiceid)
        {
            int next = this.curFrame.Getchoice(choiceid).next;
            if (next <= -1)
            {
                if (this.curFrame.Getchoice(choiceid).next == -1 && Singleton<StageController>.Instance.Phase != StageController.StagePhase.EndBattle)
                {
                    Singleton<StageController>.Instance.GetStageModel().GetWave(Singleton<StageController>.Instance.CurrentWave).Defeat();
                    // Sticky exit + strip residual immune mystery NPCs so next-stage pick cannot
                    // resume combat against the mystery wave after SetNextStage flips curstagetype.
                    try { RewardingModel.MarkNonCombatNodeExit("MysteryEnd"); }
                    catch (Exception ex) { Debug.LogWarning("[RMR] MysteryEnd MarkNonCombatNodeExit: " + ex.Message); }
                    Singleton<StageController>.Instance.EndBattle();
                }
                Singleton<MysteryManager>.Instance.EndMystery(this);
            }
            else
                this.SwapFrame(next);
        }
        #endregion

        #region --- Other helpers ---


        public virtual string GetCurFrameTitle()
        {
            string text = string.Empty;
            try
            {
                if (loc != null)
                    text = loc.Title;
                else
                    text = abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText(this.xmlinfo.Title, Array.Empty<object>());
            }
            catch (Exception e) { Debug.Log("Failed to localize frame title: " + e); }
            return text.ReplaceColorShorthands();
        }

        public virtual string GetCurFrameDia()
        {
            string curFrameDia = string.Empty;
            try
            {
                if (loc != null)
                {
                    curFrameDia = loc.GetFrameById(this.curFrame.FrameID).Dialogs;
                }
                else if (this.curFrame.Dialog.Count > 0)
                { 
                    foreach (string id in this.curFrame.Dialog)
                        curFrameDia = curFrameDia + abcdcode_LOGLIKE_MOD_Extension.TextDataModel.GetText(id) + Environment.NewLine;
                }
                
            }
            catch (Exception e) { Debug.Log("Failed to localize frame dialog: " + e); }
            return curFrameDia.ReplaceColorShorthands();
        }
        #endregion

        #region --- Mystery lifecycle ---


        public virtual void EndMystery() => this.RemoveCurFrame();
        #endregion

        #region --- Other helpers ---


        public void RemoveCurFrame()
        {
            if (this.FrameObj == null || this.FrameObj.Count <= 0)
                return;
            foreach (GameObject gameObject in this.FrameObj.Values)
            {
                try
                {
                    if (gameObject != null)
                        UnityEngine.Object.Destroy(gameObject);
                }
                catch
                {
                }
            }
            this.animator = (AnimUpdater)null;
            this.FrameObj.Clear();
        }

        public void DisableButton(int button)
        {
            string btn = button.ToString();
            this.FrameObj["choice_btn" + btn].GetComponent<Image>().sprite = LogLikeMod.ArtWorks["disabledButton"];
            this.FrameObj["Desc" + btn].GetComponent<TextMeshProUGUI>().text = TextDataModel.GetText("Mystery_RequireCondition") + this.FrameObj["Desc" + btn].GetComponent<TextMeshProUGUI>().text;
        }

        public void ReformatButton(int button, params object[] parameters)
        {
            string btn = button.ToString();
            var tmpro = this.FrameObj["Desc" + btn].GetComponent<TextMeshProUGUI>();
            tmpro.text = string.Format(tmpro.text, parameters);
        }

        public void ReformatDialog(params object[] parameters)
        {
            var tmpro = this.FrameObj["Dia"].GetComponent<TextMeshProUGUI>();
            tmpro.text = string.Format(tmpro.text, parameters);
        }
        #endregion

        #region --- Mystery lifecycle ---


        public void ShowOverlayOverButton(BattleUnitBuf buf, int button)
        {
            SingletonBehavior<UIBattleOverlayManager>.Instance.EnableBufOverlay(buf.bufActivatedName, buf.bufActivatedText, buf.GetBufIcon(), this.FrameObj["choice_btn" + button.ToString()]);
        }

        public void ShowOverlayOverButton(GlobalLogueEffectBase item, int button)
        {
            SingletonBehavior<UIMainOverlayManager>.Instance.SetTooltip(item.GetEffectName(), item.GetEffectDesc() + "\n"
                        + "<color=#" + ColorUtility.ToHtmlStringRGB(UIColorManager.Manager.GetEquipRarityColor(item.GetRarity())) + ">" + item.GetRarity().ToString() + "</color>", this.FrameObj["choice_btn" + button.ToString()].transform as RectTransform, item.GetRarity(), UIToolTipPanelType.OnlyContent);
        }

        public void ShowOverlayOverButton(PickUpModelBase item, int button)
        {
            SingletonBehavior<UIMainOverlayManager>.Instance.SetTooltip(item.Name, item.Desc + "\n"
                        + "<color=#" + ColorUtility.ToHtmlStringRGB(UIColorManager.Manager.GetEquipRarityColor(item.GetRarity())) + ">" + item.GetRarity().ToString() + "</color>", this.FrameObj["choice_btn" + button.ToString()].transform as RectTransform, item.GetRarity(), UIToolTipPanelType.OnlyContent);
        }

        public void CloseOverlayOverButton()
        {
            if (SingletonBehavior<UIBattleOverlayManager>.Instance.isActiveAndEnabled)
                SingletonBehavior<UIBattleOverlayManager>.Instance.DisableOverlay();
            if (SingletonBehavior<UIMainOverlayManager>.Instance.IsOpened())
                SingletonBehavior<UIMainOverlayManager>.Instance.Close();
        }

        /// <summary>TextUpDownButton</summary>

        public class TextUpDownButton : Selectable
        {
            public MysteryBase.TextUpDownButton.DownEvent down;

            public void Init(MysteryBase.TextUpDownButton.DownEvent down) => this.down = down;

            public override void OnPointerDown(PointerEventData eventData)
            {
                base.OnPointerDown(eventData);
                if (this.down == null)
                    return;
                this.down();
            }

            public void Update()
            {
                if (!this.IsPressed())
                    return;
                this.down();
            }

            public delegate void DownEvent();
        }

        /// <summary>TextUpDown</summary>

        public class TextUpDown : MonoBehaviour
        {
            public GameObject target;
            public float maxUp;
            public float maxDown;

            public void Init(GameObject target, float maxUp, float maxDown)
            {
                this.target = target;
                this.maxUp = maxUp;
                this.maxDown = maxDown;
            }

            public void ScrollUp()
            {
                if ((double)this.target.transform.localPosition.y > (double)this.maxUp)
                    this.target.transform.localPosition -= new Vector3(0.0f, 7.5f);
                this.CheckUpDownFit();
            }

            public void ScrollDown()
            {
                if ((double)this.target.transform.localPosition.y < (double)this.maxDown)
                    this.target.transform.localPosition += new Vector3(0.0f, 7.5f);
                this.CheckUpDownFit();
            }

            public void CheckUpDownFit()
            {
                if ((double)this.target.transform.localPosition.y < (double)this.maxUp)
                    this.target.transform.localPosition = new Vector3(this.target.transform.localPosition.x, this.maxUp);
                if ((double)this.target.transform.localPosition.y <= (double)this.maxDown)
                    return;
                this.target.transform.localPosition = new Vector3(this.target.transform.localPosition.x, this.maxDown);
            }

            public void Update()
            {
                float y = Input.mouseScrollDelta.y;
                if (Input.GetKey(KeyCode.UpArrow) || (double)y > 0.0)
                    this.ScrollUp();
                if (!Input.GetKey(KeyCode.DownArrow) && (double)y >= 0.0)
                    return;
                this.ScrollDown();
            }
        }

        /// <summary>MysteryAbnormalInfo</summary>

        public class MysteryAbnormalInfo
        {
            public List<EmotionCardXmlInfo> abnormal;
            public int level;
        }
        #endregion

    }
}
