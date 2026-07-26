// -----------------------------------------------------------------------------
// LOGLIKE core UI/data: LogCardUpgradeManager
// Namespace/file: ruina-roguelike-reborn-main\abcdcode_LOGLIKE_MOD\LogCardUpgradeManager.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
using LOR_DiceSystem;
using RogueLike_Mod_Reborn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;


namespace abcdcode_LOGLIKE_MOD
{

    /// <summary>LOGLIKE type: LogCardUpgradeManager</summary>

    public class LogCardUpgradeManager : Singleton<LogCardUpgradeManager>
    {
        public List<System.Type> UpgradeInfoCache;
        public static string UpgradeKeyword = "<LogUpgrade>";
        public Dictionary<LorId, Dictionary<int, UpgradeBase>> UpgradeInfoDic = new Dictionary<LorId, Dictionary<int, UpgradeBase>>();
        #region --- Getters / setters / checks ---


        public void SetInfoCache()
        {
            this.UpgradeInfoCache = new List<System.Type>();
            foreach (Assembly assem in LogLikeMod.GetAssemList())
            {
                foreach (System.Type type in LogLikeMod.GetLoadableTypes(assem))
                {
                    if (type.Name.Contains("UpgradeModel"))
                        this.UpgradeInfoCache.Add(type);
                }
            }
        }
        #endregion

        #region --- Battle hooks ---


        public DiceCardXmlInfo GetUpgradeCard(LorId cardid)
        {
            if (LogueBookModels.TryGetBinahFullUpgradeCard(cardid, out DiceCardXmlInfo binahFull))
                return binahFull;
            if (UpgradeMetadata.UnpackPid(cardid.packageId, out UpgradeMetadata metadata))
            {
                if (metadata.canStack)
                    return this.GetUpgradeCard(cardid.GetOriginalId(), metadata.index, metadata.count + 1);
                return ItemXmlDataList.instance.GetCardItem(cardid, false);
            }
            return GetUpgradeCard(cardid, 0, 1);
        }
        #endregion

        #region --- Save / load ---


        public void ReLoadCurAllUpgradeCard()
        {
            foreach (KeyValuePair<LorId, Dictionary<int, UpgradeBase>> keyValuePair1 in this.UpgradeInfoDic)
            {
                if (keyValuePair1.Value != null)
                {
                    foreach (KeyValuePair<int, UpgradeBase> keyValuePair2 in keyValuePair1.Value)
                        ItemXmlDataList.instance.LogAddModCard(keyValuePair2.Value.GetUpgradeInfo(keyValuePair2.Key, 1));
                }
            }
        }

        public Dictionary<int, DiceCardXmlInfo> GetAllUpgradesCard(LorId cardId, int count = 1)
        {
            Dictionary<int, DiceCardXmlInfo> dictionary = new Dictionary<int, DiceCardXmlInfo>();
            // Binah degraded → full is a discrete card-id swap, not a stacked dice upgrade.
            if (LogueBookModels.TryGetBinahFullUpgradeCard(cardId, out DiceCardXmlInfo binahFull))
            {
                dictionary[0] = binahFull;
                return dictionary;
            }
            if (UpgradeMetadata.UnpackPid(cardId.packageId, out UpgradeMetadata metadata))
            {
                if (metadata.canStack)
                {
                    DiceCardXmlInfo upgradeInfo = GetUpgradeCard(cardId.GetOriginalId(), metadata.index, metadata.count + count);
                    dictionary[metadata.index] = upgradeInfo;
                }
                return dictionary;
            }

            if (this.UpgradeInfoDic.TryGetValue(cardId, out Dictionary<int, UpgradeBase> upgrades))
            {
                foreach (KeyValuePair<int, UpgradeBase> pair in upgrades)
                {
                    DiceCardXmlInfo upgradeInfo = pair.Value.GetUpgradeInfo(pair.Key, count);
                    dictionary[pair.Key] = upgradeInfo;
                }
            }

            if (this.UpgradeInfoCache == null)
                this.SetInfoCache();
            foreach (System.Type type in this.UpgradeInfoCache.ToArray())
            {
                try
                {
                    if (Activator.CreateInstance(type) is UpgradeBase instance)
                    {
                        instance.Init();
                        instance.baseinfo = ItemXmlDataList.instance.GetCardItem(instance.baseid, true);

                        if (!this.UpgradeInfoDic.TryGetValue(instance.baseid, out Dictionary<int, UpgradeBase> _))
                        {
                            this.UpgradeInfoDic.Add(instance.baseid, new Dictionary<int, UpgradeBase>()
                            {
                                {
                                    instance.index,
                                    instance
                                }
                            });
                        }
                        else
                        {
                            this.UpgradeInfoDic[instance.baseid][instance.index] = instance;
                        }

                        instance.Log("Created Custom UpgradeInfo");
                        this.UpgradeInfoCache.Remove(type);
                        
                        if (instance.baseid == cardId)
                        {
                            DiceCardXmlInfo upgradeInfo = instance.GetUpgradeInfo(instance.index, count);
                            dictionary[instance.index] = upgradeInfo;
                        }
                    }
                }
                catch (Exception e)
                {
                    this.Log("Error Catched by Create " + type.Name + "\n" + e);
                }
            }

            foreach (var card in dictionary.Values)
            {
                ItemXmlDataList.instance.LogAddModCard(card);
            }

            return dictionary;
        }
        #endregion

        #region --- Battle hooks ---


        public DiceCardXmlInfo GetUpgradeCard(LorId cardid, int index = 0, int count = 1)
        {
            Dictionary<int, UpgradeBase> dictionary;
            if (this.UpgradeInfoDic.TryGetValue(cardid.GetOriginalId(), out dictionary) && dictionary.TryGetValue(index, out UpgradeBase upgrade))
            {
                DiceCardXmlInfo upgradeInfo = upgrade.GetUpgradeInfo(index, count);
                ItemXmlDataList.instance.LogAddModCard(upgradeInfo);
                return upgradeInfo;
            }
            DiceCardXmlInfo upgradeInfo1 = this.FindUpgradeInfo(cardid.GetOriginalId(), index, count).GetUpgradeInfo(index, count);
            ItemXmlDataList.instance.LogAddModCard(upgradeInfo1);
            return upgradeInfo1;
        }
        #endregion

        #region --- Getters / setters / checks ---


        public UpgradeBase FindUpgradeInfo(LorId cardid, int index = 0, int count = 1)
        {
            if (this.UpgradeInfoCache == null)
                this.SetInfoCache();
            if (this.UpgradeInfoCache.Count > 0)
            {
                foreach (System.Type type in this.UpgradeInfoCache.ToArray())
                {
                    try
                    {
                        if (Activator.CreateInstance(type) is UpgradeBase instance)
                        {
                            instance.Log("Created Custom UpgradeInfo");
                            instance.Init();
                            instance.baseinfo = ItemXmlDataList.instance.GetCardItem(instance.baseid, true);
                            if (instance.baseinfo == null)
                                this.Log($"Error! baseinfo NULL cardid : {cardid.packageId} _ {cardid.id.ToString()} _ {instance.index}");
                            if (this.UpgradeInfoDic.TryGetValue(instance.baseid, out Dictionary<int, UpgradeBase> _))
                                this.UpgradeInfoDic[instance.baseid][instance.index] = instance;
                            else
                                this.UpgradeInfoDic.Add(instance.baseid, new Dictionary<int, UpgradeBase>()
                                {
                                    {
                                        instance.index,
                                        instance
                                    }
                                });
                            this.UpgradeInfoCache.Remove(type);
                            if (instance.baseid == cardid && instance.index == index)
                            {
                                return instance;
                            }
                        }
                    }
                    catch
                    {
                        this.Log("Error Catched by Create " + type.Name);
                    }
                }
            }
            UpgradeBase upgradeInfo = new UpgradeBase();
            upgradeInfo.baseid = cardid;
            upgradeInfo.baseinfo = ItemXmlDataList.instance.GetCardItem(cardid, true);
            if (upgradeInfo.baseinfo == null)
            {
                this.Log("Error! baseinfo NULL cardid : " + cardid.packageId + " _ " + cardid.id.ToString());
            }
            if (!this.UpgradeInfoDic.TryGetValue(upgradeInfo.baseid, out Dictionary<int, UpgradeBase> _))
                this.UpgradeInfoDic.Add(upgradeInfo.baseid, new Dictionary<int, UpgradeBase>()
              {
                {
                  upgradeInfo.index,
                  upgradeInfo
                }
              });
            else
                this.UpgradeInfoDic[upgradeInfo.baseid][upgradeInfo.index] = upgradeInfo;
            return upgradeInfo;
        }
        #endregion

    }
}
