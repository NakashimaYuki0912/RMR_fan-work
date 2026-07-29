// -----------------------------------------------------------------------------
// Shop system component: ShopBase
// Namespace/file: ruina-roguelike-reborn-main\abcdcode_LOGLIKE_MOD\ShopBase.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
using GameSave;
using LOR_DiceSystem;
using RogueLike_Mod_Reborn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace abcdcode_LOGLIKE_MOD
{

    /// <summary>Shop component: ShopBase</summary>

    public class ShopBase
    {
        public const int UpgradeCardBasePrice = 10;
        public const int UpgradeCardPriceStep = 2;

        /// <summary>Shop component: ShopSection</summary>

        public enum ShopSection
        {
            Passive,
            EquipPage,
            AbnormalityPage,
            EgoPage,
            CardUpgrade,
        }

        public static Dictionary<int, Vector2[]> CardShape;
        public List<ShopGoods> Goods;
        public Dictionary<string, GameObject> FrameObj = new Dictionary<string, GameObject>();

        public ShopBase()
        {
            if (ShopBase.CardShape != null)
                return;
            ShopBase.CardShape = new Dictionary<int, Vector2[]>();
            // Tightened X spacing so cards don't overlap each other or side panels.
            // Combat-page row (Y=365) sits lower than the old Y=425 strip to shrink the
            // empty gap above key-page / item goods. Horizontal gap ≈ 200px between centers.
            Vector2[] vector2Array1 = new Vector2[3]
            {
      new Vector2(280f, 365f),
      new Vector2(0.0f, 365f),
      new Vector2(-280f, 365f)
            };
            ShopBase.CardShape.Add(3, vector2Array1);
            Vector2[] vector2Array2 = new Vector2[4]
            {
      new Vector2(300f, 365f),
      new Vector2(100f, 365f),
      new Vector2(-100f, 365f),
      new Vector2(-300f, 365f)
            };
            ShopBase.CardShape.Add(4, vector2Array2);
            Vector2[] vector2Array3 = new Vector2[5]
            {
      new Vector2(350f, 365f),
      new Vector2(175f, 365f),
      new Vector2(0.0f, 365f),
      new Vector2(-175f, 365f),
      new Vector2(-350f, 365f)
            };
            ShopBase.CardShape.Add(5, vector2Array3);
            Vector2[] vector2Array4 = new Vector2[6]
            {
              new Vector2(350f, 365f),
              new Vector2(210f, 365f),
              new Vector2(70f, 365f),
              new Vector2(-70f, 365f),
              new Vector2(-210f, 365f),
              new Vector2(-350f, 365f)
            };
            ShopBase.CardShape.Add(6, vector2Array4);
            // 7–10 items: two rows, top row Y=410, bottom row Y=190
            Vector2[] vector2Array5 = new Vector2[7]
            {
      new Vector2(300f, 410f),
      new Vector2(100f, 410f),
      new Vector2(-100f, 410f),
      new Vector2(-300f, 410f),
      new Vector2(280f, 190f),
      new Vector2(0.0f, 190f),
      new Vector2(-280f, 190f)
            };
            ShopBase.CardShape.Add(7, vector2Array5);
            Vector2[] vector2Array6 = new Vector2[8]
            {
      new Vector2(300f, 410f),
      new Vector2(100f, 410f),
      new Vector2(-100f, 410f),
      new Vector2(-300f, 410f),
      new Vector2(300f, 190f),
      new Vector2(100f, 190f),
      new Vector2(-100f, 190f),
      new Vector2(-300f, 190f)
            };
            ShopBase.CardShape.Add(8, vector2Array6);
            Vector2[] vector2Array7 = new Vector2[9]
            {
      new Vector2(350f, 410f),
      new Vector2(175f, 410f),
      new Vector2(0.0f, 410f),
      new Vector2(-175f, 410f),
      new Vector2(-350f, 410f),
      new Vector2(300f, 190f),
      new Vector2(100f, 190f),
      new Vector2(-100f, 190f),
      new Vector2(-300f, 190f)
            };
            ShopBase.CardShape.Add(9, vector2Array7);
            Vector2[] vector2Array8 = new Vector2[10]
            {
      new Vector2(350f, 410f),
      new Vector2(175f, 410f),
      new Vector2(0.0f, 410f),
      new Vector2(-175f, 410f),
      new Vector2(-350f, 410f),
      new Vector2(350f, 190f),
      new Vector2(175f, 190f),
      new Vector2(0.0f, 190f),
      new Vector2(-175f, 190f),
      new Vector2(-350f, 190f)
            };
            ShopBase.CardShape.Add(10, vector2Array8);
        }
        #region --- Save / load ---


        public virtual SaveData GetSaveData()
        {
            SaveData saveData1 = new SaveData();
            SaveData data1 = new SaveData();
            for (int num = 0; num < 200; ++num)
            {
                if (this.FrameObj.ContainsKey("Goods_Card" + num.ToString()))
                {
                    if (!this.FrameObj["Goods_Card" + num.ToString()].activeSelf)
                        continue;
                    else
                    {
                        SaveData saveData2 = this.FrameObj["Goods_Card" + num.ToString()].GetComponent<ShopGoods>().GetSaveData();
                        saveData2.AddData("index", new SaveData(num));
                        data1.AddToList(saveData2);
                    }
                }
            }
            saveData1.AddData("Cards", data1);
            SaveData data2 = new SaveData();
            for (int num = 0; num < 200; ++num)
            {
                if (this.FrameObj.ContainsKey("Goods_Passive" + num.ToString()))
                {
                    if (!this.FrameObj["Goods_Passive" + num.ToString()].activeSelf)
                        continue;
                    else
                    {
                        SaveData saveData3 = this.FrameObj["Goods_Passive" + num.ToString()].GetComponent<ShopGoods>().GetSaveData();
                        saveData3.AddData("index", new SaveData(num));
                        data2.AddToList(saveData3);
                    }
                }
            }
            saveData1.AddData("Passives", data2);
            SaveData data3 = new SaveData();
            if (this.FrameObj.ContainsKey("Goods_Upgrade0") && this.FrameObj["Goods_Upgrade0"].activeSelf)
            {
                SaveData saveData4 = this.FrameObj["Goods_Upgrade0"].GetComponent<ShopGoods>().GetSaveData();
                saveData4.AddData("index", new SaveData(0));
                data3.AddToList(saveData4);
            }
            saveData1.AddData("Upgrades", data3);
            return saveData1;
        }

        public virtual void LoadFromSaveData(SaveData data)
        {
            foreach (SaveData data1 in data.GetData("Cards"))
                this.Shop_CardCreating(ItemXmlDataList.instance.GetCardItem(ExtensionUtils.LogLoadFromSaveData(data1.GetData("Id")), true), 1, new Vector2(0.0f, 0.0f), data1.GetInt("index")).LoadFromSaveData(data1);
            foreach (SaveData data2 in data.GetData("Passives"))
                this.Shop_PassiveCreating(Singleton<RewardPassivesList>.Instance.GetPassiveInfo(ExtensionUtils.LogLoadFromSaveData(data2.GetData("Id"))), new Vector2(0.0f, 0.0f), data2.GetInt("index")).LoadFromSaveData(data2);
            SaveData upgrades = data.GetData("Upgrades");
            if (upgrades != null)
            {
                foreach (SaveData upgradeData in upgrades)
                    this.Shop_CardUpgradeCreating(new Vector2(0.0f, 0.0f), upgradeData.GetInt("index"), upgradeData.GetInt("price")).LoadFromSaveData(upgradeData);
            }
        }
        #endregion

        #region --- Other helpers ---


        public static List<RewardPassiveInfo> GetPassiveInList(
          List<RewardPassiveInfo> passiveinfos,
          int count,
          ShopRewardType type)
        {
            List<RewardPassiveInfo> passiveInList = new List<RewardPassiveInfo>();
            if (passiveinfos == null || passiveinfos.Count == 0)
                return passiveInList;
            do
            {
                RewardPassiveInfo passiveinfo = passiveinfos[UnityEngine.Random.Range(0, passiveinfos.Count)];
                using (List<BattleUnitModel>.Enumerator enumerator = BattleObjectManager.instance.GetList(Faction.Player).GetEnumerator())
                {
                    BattleUnitModel current;
                    ShopPickUpModel pickUp;
                    do
                    {
                        if (enumerator.MoveNext())
                        {
                            current = enumerator.Current;
                            pickUp = LogLikeMod.FindPickUp(passiveinfo.script) as ShopPickUpModel;
                        }
                        else
                            goto label_7;
                    }
                    while (pickUp == null || !pickUp.IsCanPickUp(current.UnitData.unitData) || pickUp.GetShopType() != type || !pickUp.IsCanAddShop());
                }
                if (!passiveInList.Contains(passiveinfo))
                    passiveInList.Add(passiveinfo);
                label_7:
                passiveinfos.Remove(passiveinfo);
            }
            while (passiveInList.Count != count && passiveinfos.Count != 0);
            return passiveInList;
        }

        public Vector2 GetShopShape_Card(int num, int id)
        {
            Vector2[] row = ResolveCardShapeRow(num);
            int idx = Mathf.Clamp(id, 0, row.Length - 1);
            return row[idx];
        }

        public Vector2 GetShopShape_Passive(int num, int id)
        {
            Vector2[] row = ResolveCardShapeRow(num);
            int idx = Mathf.Clamp(id, 0, row.Length - 1);
            // Passive / item row sits below combat pages. Offset reduced with the combat
            // strip drop (was 450 with Y=425) so middle goods keep their screen Y.
            return row[idx] - new Vector2(0.0f, 390f);
        }

        /// <summary>
        /// CardShape only has keys 3–10. Shop may request passiveNum=2 when card count is 4.
        /// Fall back to nearest available layout instead of KeyNotFoundException.
        /// </summary>
        private static Vector2[] ResolveCardShapeRow(int num)
        {
            if (ShopBase.CardShape == null)
            {
                // Defensive: constructor always fills this, but keep a tiny fallback.
                return new[]
                {
                    new Vector2(280f, 365f),
                    new Vector2(0f, 365f),
                    new Vector2(-280f, 365f),
                };
            }
            if (ShopBase.CardShape.TryGetValue(num, out Vector2[] exact) && exact != null && exact.Length > 0)
                return exact;
            // Prefer larger key first so extra slots still have positions.
            for (int k = num; k <= 10; k++)
            {
                if (ShopBase.CardShape.TryGetValue(k, out Vector2[] up) && up != null && up.Length > 0)
                    return up;
            }
            for (int k = num; k >= 3; k--)
            {
                if (ShopBase.CardShape.TryGetValue(k, out Vector2[] down) && down != null && down.Length > 0)
                    return down;
            }
            // Last resort: synthesize evenly spaced row.
            int n = Math.Max(1, num);
            var synth = new Vector2[n];
            float step = n <= 1 ? 0f : 700f / (n - 1);
            for (int i = 0; i < n; i++)
                synth[i] = new Vector2(350f - i * step, 365f);
            return synth;
        }

        public Vector2 GetSupplementalSectionBasePosition(ShopSection section)
        {
            switch (section)
            {
                // Left column sits outside the combat-card row (±350) so hover previews
                // in the board center cannot cover equip / abnormality goods.
                case ShopSection.EquipPage:
                    return new Vector2(-700f, 280f);
                case ShopSection.AbnormalityPage:
                    return new Vector2(-700f, -160f);
                case ShopSection.EgoPage:
                    return new Vector2(700f, 280f);
                case ShopSection.CardUpgrade:
                    return new Vector2(700f, -200f);
                default:
                    return new Vector2(0.0f, -25f);
            }
        }

        public float GetSupplementalSectionStep(ShopSection section)
        {
            // Tighter vertical gap keeps both abno slots above the bottom UI strip.
            return section == ShopSection.CardUpgrade ? 0f : -210f;
        }

        public Vector2 GetSupplementalShopShape(ShopSection section, int id)
        {
            Vector2 basePos = this.GetSupplementalSectionBasePosition(section);
            return new Vector2(basePos.x, basePos.y + id * GetSupplementalSectionStep(section));
        }

        public static ShopBase FindShop(string script)
        {
            foreach (Assembly assem in LogLikeMod.GetAssemList())
            {
                foreach (System.Type type in LogLikeMod.GetLoadableTypes(assem))
                {
                    if (type.Name == "ShopModel_" + script.Trim())
                        return Activator.CreateInstance(type) as ShopBase;
                }
            }
            return new ShopBase();
        }
        #endregion

        #region --- Shop lifecycle ---


        public virtual void Init()
        {
            this.FrameObj = new Dictionary<string, GameObject>();
            this.CreateShop();
            LoguePlayDataSaver.SaveShop(this);
            Singleton<GlobalLogueEffectManager>.Instance.OnEnterShop(this);
        }
        #endregion

        #region --- Other helpers ---


        public virtual void RemoveShop()
        {
            if (this.FrameObj == null || this.FrameObj.Count <= 0)
                return;
            foreach (UnityEngine.Object @object in this.FrameObj.Values)
                UnityEngine.Object.Destroy(@object);
            this.FrameObj.Clear();
        }

        public static int CompareCardRarity(DiceCardXmlInfo x, DiceCardXmlInfo y)
        {
            if (x.Rarity > y.Rarity)
                return 1;
            return x.Rarity == y.Rarity ? 0 : -1;
        }

        public ShopGoods_Card Shop_CardCreating(DiceCardXmlInfo card, int num, Vector2 position, int id)
        {
            GameObject gameObject = new GameObject("");
            gameObject.transform.SetParent(this.FrameObj["ShopFrame"].transform);
            gameObject.transform.localScale = new Vector3(1f, 1f);
            ShopGoods_Card shopGoodsCard = gameObject.AddComponent<ShopGoods_Card>();
            shopGoodsCard.gameObject.transform.localPosition = (Vector3)position;
            shopGoodsCard.SetShop(this);
            int goods_count = num;
            shopGoodsCard.SetGoods(card, goods_count);
            this.Goods.Add((ShopGoods)shopGoodsCard);
            this.FrameObj.Add("Goods_Card" + id.ToString(), shopGoodsCard.gameObject);
            return shopGoodsCard;
        }

        public virtual void CreateShop_Card(int num = 5)
        {
            Singleton<CardDropValueList>.Instance.GetData(new LorId(LogLikeMod.ModId, 11001));
            CardDropValueXmlInfo data;
            switch (LogLikeMod.curchaptergrade)
            {
                case ChapterGrade.Grade1:
                    data = Singleton<CardDropValueList>.Instance.GetData(new LorId(LogLikeMod.ModId, 11001));
                    break;
                case ChapterGrade.Grade2:
                    data = Singleton<CardDropValueList>.Instance.GetData(new LorId(LogLikeMod.ModId, 12001));
                    break;
                case ChapterGrade.Grade3:
                    data = Singleton<CardDropValueList>.Instance.GetData(new LorId(LogLikeMod.ModId, 13001));
                    break;
                case ChapterGrade.Grade4:
                    data = Singleton<CardDropValueList>.Instance.GetData(new LorId(LogLikeMod.ModId, 14001));
                    break;
                case ChapterGrade.Grade5:
                    data = Singleton<CardDropValueList>.Instance.GetData(new LorId(LogLikeMod.ModId, 15001));
                    break;
                case ChapterGrade.Grade6:
                    data = Singleton<CardDropValueList>.Instance.GetData(new LorId(LogLikeMod.ModId, 16001));
                    break;
                case ChapterGrade.Grade7:
                default:
                    // Grade7 shop tries 17001 first, then falls back to the Grade6 pool.
                    data = Singleton<CardDropValueList>.Instance.GetData(new LorId(LogLikeMod.ModId, 17001))
                        ?? Singleton<CardDropValueList>.Instance.GetData(new LorId(LogLikeMod.ModId, 16001));
                    break;
            }
            // Fallbacks if chapter pool missing (e.g. drop-value files failed to load as .txt).
            if (data == null)
            {
                data = Singleton<CardDropValueList>.Instance.GetData(new LorId(LogLikeMod.ModId, 11001))
                    ?? Singleton<CardDropValueList>.Instance.GetData(new LorId(LogLikeMod.ModId, 12001))
                    ?? Singleton<CardDropValueList>.Instance.GetData(new LorId(LogLikeMod.ModId, 13001));
            }
            if (data == null)
            {
                Debug.LogError("[CreateShop_Card] No CardDropValue data for any chapter — combat pages cannot spawn. Check SpecialStaticInfo/DropValueXmlInfos (*.txt).");
                return;
            }
            Singleton<GlobalLogueEffectManager>.Instance.ChangeShopCardList(this, ref data);
            if (data == null)
            {
                Debug.LogError("[CreateShop_Card] ChangeShopCardList cleared drop data.");
                return;
            }
            // Build unowned card pool — skip cards the player already owns
            CardDropTableXmlInfo dropTable = Singleton<CardDropTableXmlList>.Instance.GetData(
                new LorId(data.workshopID, data.DropTableId));
            var allCards = new List<DiceCardXmlInfo>();
            if (dropTable != null)
            {
                foreach (LorId cardId in dropTable.cardIdList)
                {
                    DiceCardXmlInfo cardItem = RewardingModel.GetCardItemOriginAware(cardId);
                    if (cardItem != null)
                        allCards.Add(cardItem);
                }
            }
            else
            {
                Debug.LogWarning($"[CreateShop_Card] DropTable missing for {data.workshopID}:{data.DropTableId}");
            }
            var unowned = allCards.Where(c => !LogueBookModels.HasOwnedCombatPage(c.id)).ToList();
            Debug.Log($"[CreateShop_Card] All:{allCards.Count} Unowned:{unowned.Count} Need:{num}");

            List<DiceCardXmlInfo> selected = new List<DiceCardXmlInfo>();
            var pickedIds = new HashSet<LorId>();
            int targetCount = System.Math.Min(num, unowned.Count);

            for (int i = 0; i < targetCount; i++)
            {
                DiceCardXmlInfo card = RewardingModel.GetCardFromFilteredPool(unowned, data, pickedIds);
                if (card == null)
                    continue;

                Singleton<GlobalLogueEffectManager>.Instance.ChangeShopCard(ref card);

                // Post-ChangeShopCard filter: reject upgrades and already-owned cards
                if (card.id.packageId != null && card.id.packageId.Contains(LogCardUpgradeManager.UpgradeKeyword))
                {
                    Debug.Log($"[CreateShop_Card] Post-filter rejected upgrade: {card.id.packageId}:{card.id.id}");
                    continue;
                }
                if (LogueBookModels.HasOwnedCombatPage(card.id))
                {
                    Debug.Log($"[CreateShop_Card] Post-filter rejected owned: {card.id.packageId}:{card.id.id}");
                    continue;
                }
                // Deduplicate by normalized originalId within this shop
                string key = LogueBookModels.NormalizeCardKey(card.id);
                if (pickedIds.Any(id => LogueBookModels.NormalizeCardKey(id) == key))
                {
                    Debug.Log($"[CreateShop_Card] Post-filter rejected duplicate key: {card.id.packageId}:{card.id.id}");
                    continue;
                }

                pickedIds.Add(card.id);
                selected.Add(card);
            }

            Debug.Log($"[CreateShop_Card] Selected {selected.Count}/{num} cards for shop");
            selected.Sort(new Comparison<DiceCardXmlInfo>(ShopBase.CompareCardRarity));
            // Layout by actual selected count (not requested num) so CardShape keys always match.
            int layoutNum = selected.Count > 0 ? selected.Count : num;
            for (int index = 0; index < selected.Count; ++index)
                this.Shop_CardCreating(selected[index], this.ShopCardCount(selected[index].Rarity), this.GetShopShape_Card(layoutNum, index), index);
            Singleton<GlobalLogueEffectManager>.Instance.OnShopCardListCreate(this);
        }

        public ShopGoods_Passive Shop_PassiveCreating(
          RewardPassiveInfo passive,
          Vector2 position,
          int id)
        {
            GameObject gameObject = new GameObject("");
            gameObject.transform.SetParent(this.FrameObj["ShopFrame"].transform);
            gameObject.transform.localScale = new Vector3(1f, 1f);
            ShopGoods_Passive shopGoodsPassive = gameObject.AddComponent<ShopGoods_Passive>();
            shopGoodsPassive.gameObject.transform.localPosition = (Vector3)position;
            shopGoodsPassive.SetShop(this);
            RewardPassiveInfo goodinfo = passive;
            shopGoodsPassive.SetGoods(goodinfo);
            this.Goods.Add((ShopGoods)shopGoodsPassive);
            this.FrameObj.Add("Goods_Passive" + id.ToString(), shopGoodsPassive.gameObject);
            return shopGoodsPassive;
        }

        public int OnceCount(int num)
        {
            switch (num)
            {
                case 4:
                    return 2;
                case 5:
                    return 3;
                case 6:
                    return 4;
                case 7:
                    return 4;
                case 8:
                    return 5;
                case 9:
                    return 5;
                case 10:
                    return 6;
                default:
                    return num / 2;
            }
        }

        public virtual void CreateShop_passive(int num = 5)
        {
            List<RewardPassiveInfo> chapterData1 = Singleton<RewardPassivesList>.Instance.GetChapterData(ChapterGrade.GradeAll, PassiveRewardListType.Shop, new LorId(LogLikeMod.ModId, 90000));
            List<RewardPassiveInfo> rewardPassiveInfoList = new List<RewardPassiveInfo>();
            int count1 = this.OnceCount(num);
            int count2 = num - count1;
            rewardPassiveInfoList.AddRange(ShopBase.GetPassiveInList(chapterData1, count1, ShopRewardType.Once));
            List<RewardPassiveInfo> chapterData2 = Singleton<RewardPassivesList>.Instance.GetChapterData(ChapterGrade.GradeAll, PassiveRewardListType.Shop, new LorId(LogLikeMod.ModId, 90000));
            rewardPassiveInfoList.AddRange(ShopBase.GetPassiveInList(chapterData2, count2, ShopRewardType.Eternal));
            int layoutNum = rewardPassiveInfoList.Count > 0 ? rewardPassiveInfoList.Count : num;
            for (int index = 0; index < rewardPassiveInfoList.Count; ++index)
                this.Shop_PassiveCreating(rewardPassiveInfoList[index], this.GetShopShape_Passive(layoutNum, index), index);
        }

        public virtual int ShopCardCount(Rarity rare)
        {
            int num;
            switch (rare)
            {
                case Rarity.Common:
                    num = 5;
                    break;
                case Rarity.Uncommon:
                    num = 4;
                    break;
                case Rarity.Rare:
                    num = 3;
                    break;
                case Rarity.Unique:
                    num = 1;
                    break;
                default:
                    num = 1;
                    break;
            }
            return (int)((double)num + (double)num * (double)LogLikeMod.curchaptergrade / 5.0);
        }
        #endregion

        #region --- Goods / buy / sell ---


        public virtual int ShopGoodCount()
        {
            switch (LogLikeMod.curchaptergrade)
            {
                case ChapterGrade.Grade1:
                    return 4;
                case ChapterGrade.Grade2:
                    return 5;
                case ChapterGrade.Grade3:
                    return 6;
                case ChapterGrade.Grade4:
                    return 6;
                case ChapterGrade.Grade5:
                    return 7;
                case ChapterGrade.Grade6:
                    return 8;
                case ChapterGrade.Grade7:
                    return 8;
                default:
                    return 5;
            }
        }
        #endregion

        #region --- Other helpers ---


        public virtual void CreateShop()
        {
            this.RemoveShop();
            // Full-bleed shop board (stretch to parent UI so the green frame covers the stage).
            Image shopFrameImg = ModdingUtils.CreateImage(
                SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.transform,
                "ShopFrame", new Vector2(1f, 1f), new Vector2(0.0f, 0.0f));
            this.FrameObj.Add("ShopFrame", shopFrameImg.gameObject);
            RectTransform frameRect = shopFrameImg.GetComponent<RectTransform>();
            if (frameRect != null)
            {
                // Stretch across the battle UI summary area instead of a small centered square.
                frameRect.anchorMin = new Vector2(0.08f, 0.08f);
                frameRect.anchorMax = new Vector2(0.92f, 0.92f);
                frameRect.offsetMin = Vector2.zero;
                frameRect.offsetMax = Vector2.zero;
                frameRect.anchoredPosition = Vector2.zero;
                frameRect.localScale = Vector3.one;
            }
            // Keep aspect of the artwork via preserveAspect if Image supports it.
            try { shopFrameImg.preserveAspect = false; } catch { }
            Button button1 = ModdingUtils.CreateButton(SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.transform, "AbCardSelection_Skip", new Vector2(1f, 1f), new Vector2(-590f, -480f));
            button1.onClick.AddListener(new UnityAction(this.HideShop));
            this.FrameObj.Add("ShopHide", button1.gameObject);
            TextMeshProUGUI textTmp1 = ModdingUtils.CreateText_TMP(button1.transform, new Vector2(-30f, 0.0f), 40, new Vector2(0.0f, 0.0f), new Vector2(1f, 1f), new Vector2(0.0f, 0.0f), TextAlignmentOptions.Midline, LogLikeMod.DefFontColor, LogLikeMod.DefFont_TMP);
            textTmp1.text = TextDataModel.GetText("ui_ShopHide");
            textTmp1.transform.Rotate(0.0f, 0.0f, 2.5f);
            this.FrameObj.Add("ShopHideText", textTmp1.gameObject);
            this.Goods = new List<ShopGoods>();
            if (!LoguePlayDataSaver.LoadShop(this))
            {
                int num = this.ShopGoodCount();
                int passiveNum = Math.Min(4, Math.Max(2, num / 2));
                int equipNum = 2;
                int abnoNum = 2;
                int egoNum = 2;
                try
                {
                    this.CreateShop_Card(num);
                }
                catch (Exception ex)
                {
                    Debug.Log($"Shop Create error1 : Card{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                }
                try
                {
                    this.CreateShop_passive(passiveNum);
                }
                catch (Exception ex)
                {
                    Debug.Log($"Shop Create error2 : Passive{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                }
                try
                {
                    if (equipNum > 0)
                        this.CreateShop_EquipPages(equipNum, passiveNum);
                }
                catch (Exception ex)
                {
                    Debug.Log($"Shop Create error3 : EquipPage{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                }
                try
                {
                    if (abnoNum > 0)
                        this.CreateShop_AbnormalityPages(abnoNum, passiveNum + equipNum);
                }
                catch (Exception ex)
                {
                    Debug.Log($"Shop Create error4 : Abnormality{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                }
                try
                {
                    if (egoNum > 0)
                        this.CreateShop_EgoPages(egoNum, num);
                }
                catch (Exception ex)
                {
                    Debug.Log($"Shop Create error5 : EGO{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                }
                try
                {
                    this.CreateShop_CardUpgrade();
                }
                catch (Exception ex)
                {
                    Debug.Log($"Shop Create error6 : CardUpgrade{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                }
            }
            this.MoneyChecking();
            Button button2 = ModdingUtils.CreateButton(SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.transform, "AbCardSelection_Skip", new Vector2(1f, 1f), new Vector2(580f, -480f));
            TextMeshProUGUI textTmp2 = ModdingUtils.CreateText_TMP(button2.transform, new Vector2(-30f, 0.0f), 40, new Vector2(0.0f, 0.0f), new Vector2(1f, 1f), new Vector2(0.0f, 0.0f), TextAlignmentOptions.Midline, LogLikeMod.DefFontColor, LogLikeMod.DefFont_TMP);
            this.FrameObj.Add("LeaveButton", button2.gameObject);
            button2.onClick.AddListener(new UnityAction(this.LeaveShop));
            string text = TextDataModel.GetText("ui_ShopLeave");
            textTmp2.text = text;
            button2.transform.SetAsLastSibling();
        }
        #endregion

        #region --- Shop lifecycle ---


        public virtual void HideShop()
        {
            GameObject gameObject1 = this.FrameObj["ShopFrame"];
            GameObject gameObject2 = this.FrameObj["LeaveButton"];
            if (gameObject1.activeSelf)
            {
                this.FrameObj["ShopHideText"].GetComponent<TextMeshProUGUI>().text = TextDataModel.GetText("ui_ShopShow");
                gameObject1.SetActive(false);
                gameObject2.SetActive(false);
            }
            else
            {
                this.FrameObj["ShopHideText"].GetComponent<TextMeshProUGUI>().text = TextDataModel.GetText("ui_ShopHide");
                gameObject1.SetActive(true);
                gameObject2.SetActive(true);
                this.MoneyChecking();
            }
            if (LogLikeMod.rewards_InStage.Count <= 0)
                return;
            this.FrameObj["ShopHideText"].GetComponent<TextMeshProUGUI>().text = TextDataModel.GetText("ui_ShopShow");
            gameObject1.SetActive(false);
            gameObject2.SetActive(false);
        }
        #endregion

        #region --- Other helpers ---


        public virtual void LeaveShop()
        {
            this.RemoveShop();
            // Defeat current shop wave first. EnsureNextListIfNeeded skips rebuild while a wave is
            // still available (treats it as "already picked next stage"), so it must run after Defeat.
            Singleton<StageController>.Instance.GetStageModel().GetWave(Singleton<StageController>.Instance.CurrentWave).Defeat();
            // Sticky exit + strip residual immune merchant NPCs so after next-stage pick
            // EndBattlePhase does not "recover" into live combat vs the shop wave.
            try { RewardingModel.MarkNonCombatNodeExit("LeaveShop"); }
            catch (Exception ex) { Debug.LogWarning("[RMR] LeaveShop MarkNonCombatNodeExit: " + ex.Message); }
            try { RewardingModel.EnsureNextListIfNeeded(); }
            catch (Exception ex) { Debug.LogWarning("[RMR] LeaveShop EnsureNextListIfNeeded: " + ex.Message); }
            Singleton<StageController>.Instance.EndBattle();
            Singleton<ShopManager>.Instance.CloseShop();
            Singleton<GlobalLogueEffectManager>.Instance.OnLeaveShop(this);
        }

        public virtual void MoneyChecking()
        {
            if (this.Goods == null || this.Goods.Count == 0)
                return;
            this.Goods.FindAll(x => x.CheckEnoughMoney());
            LoguePlayDataSaver.SaveShop(this);
            LoguePlayDataSaver.SavePlayData_Menu();
        }

        /// <summary>
        /// Creates shop goods for role books (key pages) filtered by current chapter.
        /// </summary>
        public virtual void CreateShop_EquipPages(int num, int startIndex = 0)
        {
            if (num <= 0) return;
            // Get equip page rewards for the current chapter
            List<RewardPassiveInfo> equipPages = new List<RewardPassiveInfo>();
            var allCommon = Singleton<RewardPassivesList>.Instance.GetChapterData(
                LogLikeMod.curchaptergrade, PassiveRewardListType.CommonReward, new LorId(-1), true)
                ?? new List<RewardPassiveInfo>();
            // Also include next chapter's for variety
            if (LogLikeMod.curchaptergrade < ChapterGrade.Grade7)
            {
                var nextChapter = Singleton<RewardPassivesList>.Instance.GetChapterData(
                    LogLikeMod.curchaptergrade + 1, PassiveRewardListType.CommonReward, new LorId(-1), true);
                if (nextChapter != null)
                    allCommon.AddRange(nextChapter);
            }
            // Filter to EquipPage rewards only — reject ModNeeded / zero-stat stubs.
            foreach (var info in allCommon)
            {
                if (info == null || info.rewardtype != RewardType.EquipPage)
                    continue;
                BookXmlInfo book = RewardingModel.GetBookDataOriginAware(info.id);
                if (!RewardingModel.IsValidBookData(book))
                {
                    Debug.LogWarning($"[CreateShop_EquipPages] skip invalid book {info.workshopID}:{info.passiveid}");
                    continue;
                }
                if (book.Chapter > (int)LogLikeMod.curchaptergrade + 1)
                    continue;
                // Don't sell books the player already owns (unique pages)
                if (LogueBookModels.HasOwnedBookPage(info.id) || LogueBookModels.HasOwnedBookPage(book.id))
                    continue;
                equipPages.Add(info);
            }
            // Shuffle and pick
            ModdingUtils.SuffleList(equipPages);
            int created = 0;
            foreach (var info in equipPages)
            {
                if (created >= num) break;
                int slotIndex = startIndex + created;
                ShopGoods_Passive goods = this.Shop_PassiveCreating(info,
                    this.GetSupplementalShopShape(ShopSection.EquipPage, created), slotIndex);
                // Shop_PassiveCreating expects a valid script; for EquipPage rewards with no script,
                // ShopGoods_Passive.SetGoods will handle directEquipId
                created++;
            }
        }

        /// <summary>
        /// Creates shop goods for abnormality pages filtered by current tier.
        /// </summary>
        public virtual void CreateShop_AbnormalityPages(int num, int startIndex = 0)
        {
            if (num <= 0) return;
            List<RewardPassiveInfo> abnoPages = RMRAbnormalityUnlockManager.GetShopEligibleAbnormalityPages(LogLikeMod.curchaptergrade);
            // Shuffle and pick
            ModdingUtils.SuffleList(abnoPages);
            int created = 0;
            foreach (var info in abnoPages)
            {
                if (created >= num) break;
                int slotIndex = startIndex + created;
                ShopGoods_Passive goods = this.Shop_PassiveCreating(info,
                    this.GetSupplementalShopShape(ShopSection.AbnormalityPage, created), slotIndex);
                created++;
            }
        }

        public virtual void CreateShop_EgoPages(int num, int startIndex = 0)
        {
            if (num <= 0) return;
            List<DiceCardXmlInfo> egoPages = RMRAbnormalityUnlockManager.GetUnlockedRealizationEgoCardsForRewards(LogLikeMod.curchaptergrade);
            ModdingUtils.SuffleList(egoPages);
            int created = 0;
            foreach (DiceCardXmlInfo card in egoPages)
            {
                if (created >= num) break;
                if (card == null || LogueBookModels.HasOwnedCombatPage(card.id))
                    continue;
                int slotIndex = startIndex + created;
                this.Shop_CardCreating(card, 1, this.GetSupplementalShopShape(ShopSection.EgoPage, created), slotIndex);
                created++;
            }
        }

        public ShopGoods_CardUpgrade Shop_CardUpgradeCreating(Vector2 position, int id, int price)
        {
            GameObject gameObject = new GameObject("");
            gameObject.transform.SetParent(this.FrameObj["ShopFrame"].transform);
            gameObject.transform.localScale = new Vector3(1f, 1f);
            ShopGoods_CardUpgrade shopGoodsCardUpgrade = gameObject.AddComponent<ShopGoods_CardUpgrade>();
            shopGoodsCardUpgrade.gameObject.transform.localPosition = (Vector3)position;
            shopGoodsCardUpgrade.SetShop(this);
            shopGoodsCardUpgrade.SetGoods(price);
            this.Goods.Add((ShopGoods)shopGoodsCardUpgrade);
            this.FrameObj.Add("Goods_Upgrade" + id.ToString(), shopGoodsCardUpgrade.gameObject);
            return shopGoodsCardUpgrade;
        }

        public virtual void CreateShop_CardUpgrade()
        {
            int price = LogueBookModels.shopUpgradeCardPrice > 0 ? LogueBookModels.shopUpgradeCardPrice : UpgradeCardBasePrice;
            this.Shop_CardUpgradeCreating(this.GetSupplementalShopShape(ShopSection.CardUpgrade, 0), 0, price);
        }
        #endregion

        #region --- Goods / buy / sell ---


        public virtual void OnCardUpgradePurchased(ShopGoods_CardUpgrade goods)
        {
            if (goods == null)
                return;
            LogueBookModels.shopUpgradeCardPrice = goods.price + UpgradeCardPriceStep;
            goods.SetPrice(LogueBookModels.shopUpgradeCardPrice);
            this.MoneyChecking();
        }
        #endregion

    }
}
