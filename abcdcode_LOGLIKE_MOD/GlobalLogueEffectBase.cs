// -----------------------------------------------------------------------------
// Global run effect / inventory: GlobalLogueEffectBase
// Namespace/file: ruina-roguelike-reborn-main\abcdcode_LOGLIKE_MOD\GlobalLogueEffectBase.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
using GameSave;
using LOR_DiceSystem;
using RogueLike_Mod_Reborn;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


namespace abcdcode_LOGLIKE_MOD
{
    /// <summary>
    /// Base class for inventory items.<br></br>
    /// Do mind the class name is saved to disk- please use unique class names to avoid conflicts.
    /// </summary>
    [HideFromItemCatalog]
    public class GlobalLogueEffectBase : Savable
    {
        /// <value>
        /// Override this with the ID provided within the effect's respective localization XML.
        /// </value>
        public virtual string KeywordId { get; }

        /// <value>
        /// Override this with the filename of the effect's icon. Defaults to <see cref="KeywordId"/> if not provided.
        /// </value>
        public virtual string KeywordIconId { get; }
        #region --- Save / load ---


        /// <summary>
        /// Used for storing persistent information to save file.<br></br>
        /// It is recommended to start the method like so:
        /// <code>SaveData data = base.GetSaveData();</code><br></br>
        /// As base.GetSaveData contains the TypeName of the effect,<br></br>
        /// which is necessary for loading the effect from a save file.
        /// </summary>
        /// <returns>The <see cref="SaveData"/> to be stored in disk.</returns>
        public virtual SaveData GetSaveData()
        {
            SaveData saveData = new SaveData();
            saveData.AddData("TypeName", new SaveData(this.GetType().Name));
            return saveData;
        }
        #endregion

        #region --- UI show / hide / build ---


        public static GlobalLogueEffectBase CreateGlobalEffectBySave(SaveData save)
        {
            string stringSelf = save.GetData("TypeName").GetStringSelf();
            Debug.Log(("CGEBS tryfind : " + stringSelf));
            foreach (Assembly assem in LogLikeMod.GetAssemList())
            {
                foreach (System.Type type in LogLikeMod.GetLoadableTypes(assem))
                {
                    if (type.Name == stringSelf)
                    {
                        Debug.Log(("CGEBS find : " + stringSelf));
                        return Activator.CreateInstance(type) as GlobalLogueEffectBase;
                    }
                }
            }
            return (GlobalLogueEffectBase)null;
        }
        #endregion

        #region --- Save / load ---


        /// <summary>
        /// Used for loading persistent information from save file.<br></br>
        /// </summary>
        /// <param name="save">The SaveData for this effect that is being loaded.</param>
        public virtual void LoadFromSaveData(SaveData save)
        {
        }
        #endregion

        #region --- Other helpers ---


        /// <summary>
        /// Runs immediately when the effect is added to the player's inventory.
        /// </summary>
        public virtual void AddedNew()
        {
        }
        #endregion

        #region --- Getters / setters / checks ---


        /// <summary>
        /// Determines whether or not the item can stack.<br></br>
        /// <b>Does not prevent multiple copies of an item from being given.</b>
        /// </summary>
        public virtual bool CanDupliacte() => false;
        #endregion

        #region --- Other helpers ---


        /// <summary>
        /// Removes the effect from the inventory.
        /// </summary>
        public virtual void Destroy() => Singleton<GlobalLogueEffectManager>.Instance.RemoveEffect(this);
        #endregion

        #region --- Battle hooks ---


        /// <summary>
        /// Runs when the item is destroyed.
        /// </summary>
        public virtual void OnDestroy()
        {
        }

        /// <summary>
        /// Runs when a new assistant librarian is added.
        /// </summary>
        /// <param name="model">The assitant librarian in question.</param>
        public virtual void OnAddSubPlayer(UnitDataModel model)
        {
        }
        #endregion

        #region --- Other helpers ---


        /// <summary>
        /// Assigns a multiplier for crafting cost.
        /// </summary>
        /// <param name="effect">The CraftEffect to multiply the cost of.</param>
        public virtual float CraftCostMultiple(CraftEffect effect) => 1f;
        #endregion

        #region --- Battle hooks ---


        /// <summary>
        /// Runs whenever a combat page is added to the inventory.
        /// </summary>
        /// <param name="baseid">The ID of the combat page.</param>

        public virtual LorId InvenAddCardChange(LorId baseid) => baseid;
        #endregion

        #region --- Other helpers ---


        /// <summary>
        /// Runs right before a mid-Act reward is about to be triggered.<br></br>
        /// Can be used to alter rewards by modifying <see cref="LogLikeMod.rewards_InStage"/>.
        /// </summary>
        public virtual void RewardInStageInterrupt()
        {
        }

        /// <summary>
        /// Runs right before an end-of-Act reward is about to be triggered.<br></br>
        /// Can be used to alter rewards by modifying <see cref="LogLikeMod.rewards"/>.
        /// </summary>
        public virtual void RewardClearStageInterrupt()
        {
        }
        #endregion

        #region --- Battle hooks ---


        /// <summary>
        /// Intercepts combat page generation in shops.
        /// </summary>
        /// <param name="card">The combat page that was generated.<br></br>Can be modified directly (ref keyword).</param>
        public virtual void ChangeShopCard(ref DiceCardXmlInfo card)
        {
        }

        /// <summary>
        /// Directly intercepts combat page rewards.
        /// </summary>
        /// <param name="cardlist">The list of combat page XML infos that are to be rewarded.<br></br>Can be modified directly (ref keyword).</param>
        public virtual void ChangeCardReward(ref List<DiceCardXmlInfo> cardlist)
        {
        }
        #endregion

        #region --- Other helpers ---

        
        /// <summary>
        /// Adds an increment to the passive attribution capacity to all librarians.
        /// </summary>
        public virtual int ChangeSuccCostValue() => 0;
        #endregion

        #region --- Battle hooks ---


        /// <summary>
        /// Changes the final roll of a die.<br></br>
        /// Behaves same as vanilla.
        /// <param name="behavior">The current die being rolled.</param>
        /// <param name="diceResult">The final roll.<br></br>Can be modified directly (ref keyword).</param>
        /// </summary>
        public virtual void ChangeDiceResult(BattleDiceBehavior behavior, ref int diceResult)
        {
        }
        #endregion

        #region --- Other helpers ---


        /// <summary>
        /// Changes the list of action/RestPickUp choices when entering a resting room.
        /// </summary>
        /// <param name="currest">The current resting room MysteryBase object.</param>
        /// <param name="choices">The list of RestPickUps that are to be given as choices.<br></br>Can be modified directly (ref keyword).</param>
        public virtual void ChangeRestChoice(MysteryBase currest, ref List<RewardPassiveInfo> choices)
        {
        }
        #endregion

        #region --- Battle hooks ---


        /// <summary>
        /// Runs at the start of the round.<br></br>
        /// Behaves same as vanilla.
        /// </summary>
        /// <param name="stage">The currently active StageController.<br></br>Moreso a shorthand to avoid calling StageController.Instance a bunch of times.</param>
        public virtual void OnRoundStart(StageController stage)
        {
        }
        #endregion

        #region --- Other helpers ---


        /// <summary>
        /// Adds a multiplier to damage about to be dealt.<br></br>
        /// Behaves same as vanilla.
        /// </summary>
        /// <param name="model">The BattleUnitModel that is about to take said damage.</param>
        /// <param name="dmg">The raw amount of incoming damage with no other modifiers.</param>
        /// <param name="type">The type of damage that is being given.</param>
        /// <param name="keyword">The keyword of the buf that is dealing the damage, if any.</param>
        /// <returns></returns>
        public virtual float DmgFactor(
          BattleUnitModel model,
          int dmg,
          DamageType type = DamageType.ETC,
          KeywordBuf keyword = KeywordBuf.None)
        {
            return 1f;
        }
        #endregion

        #region --- Battle hooks ---


        /// <summary>
        /// Runs whenever an unit kills another unit.
        /// </summary>
        /// <param name="killer">Self-explanatory.</param>
        /// <param name="target">Self-explanatory.</param>
        public virtual void OnKillUnit(BattleUnitModel killer, BattleUnitModel target)
        {
        }

        /// <summary>
        /// Runs whenever an unit dies.
        /// </summary>
        /// <param name="unit">The unit that is going to die.</param>
        public virtual void OnDieUnit(BattleUnitModel unit)
        {
        }

        /// <summary>
        /// Runs at combat start for every combat page being used.<br></br>
        /// More specifically, <see cref="StageController._allCardList"/>.<br></br>
        /// Other than that, behaves same as vanilla.
        /// </summary>
        public virtual void OnStartBattle(BattlePlayingCardDataInUnitModel card)
        {
        }

        /// <summary>
        /// Runs *before* a die is rolled.<br></br>
        /// Behaves same as vanilla.
        /// </summary>
        public virtual void BeforeRollDice(BattleDiceBehavior behavior)
        {
        }

        /// <summary>
        /// Runs whenever any unit uses a combat page. (On Use)<br></br>
        /// Behaves same as vanilla.
        /// </summary>
        public virtual void OnUseCard(BattlePlayingCardDataInUnitModel cardmodel)
        {
        }

        /// <summary>
        /// Directly alters the list of combat pages made available in a shop.
        /// </summary>
        /// <param name="shop">The currently open shop object.</param>
        /// <param name="list">An object containing the drop table ID and drop rates for different rarities.<br></br>
        /// Can be modified directly (ref keyword).</param>
        public virtual void ChangeShopCardList(ShopBase shop, ref CardDropValueXmlInfo list)
        {
        }
        #endregion

        #region --- UI show / hide / build ---


        /// <summary>
        /// Runs after combat pages have been generated in a shop.
        /// </summary>
        /// <param name="shop">The currently open shop object.</param>
        public virtual void OnShopCardListCreate(ShopBase shop)
        {
        }
        #endregion

        #region --- Battle hooks ---


        /// <summary>
        /// Runs when a combat page reward has been chosen by the player.
        /// </summary>
        /// <param name="cardlist">The list of combat pages that were available to choose.</param>
        /// <param name="pick">The combat page that was picked by the player.</param>
        public virtual void OnPickCardReward(List<DiceCardXmlInfo> cardlist, DiceCardXmlInfo pick)
        {
        }

        /// <summary>
        /// Runs whenever the player skips a combat page reward.
        /// </summary>
        /// <param name="cardlist">The list of combat pages that were available to choose.</param>
        public virtual void OnSkipCardRewardChoose(List<DiceCardXmlInfo> cardlist)
        {
        }
        #endregion

        #region --- Getters / setters / checks ---


        /// <summary>
        /// Determines if an item can be bought from a shop, regardless of price.<br></br>
        /// Default to <see langword="true"/>.
        /// </summary>
        /// <param name="shop">The currently open shop object.</param>
        /// <param name="goods">The item whose purchasability is going to be determined.</param>
        /// <returns>Whether or not the item can be bought. (true for yes, false for no)</returns>
        public virtual bool CanShopPurchase(ShopBase shop, ShopGoods goods) => true;
        #endregion

        #region --- Battle hooks ---


        /// <summary>
        /// Runs whenever the player enters a shop.
        /// </summary>
        /// <param name="shop">The currently open shop object.</param>
        public virtual void OnEnterShop(ShopBase shop)
        {
        }

        /// <summary>
        /// Runs whenever the playe leaves a shop.
        /// </summary>
        /// <param name="shop">The currently open shop object.</param>
        public virtual void OnLeaveShop(ShopBase shop)
        {
        }

        /// <summary>
        /// Runs at the end of the Act. (not Scene/Turn!)<br></br>
        /// Behaves same as vanilla.
        /// </summary>
        public virtual void OnEndBattle()
        {
        }

        /// <summary>
        /// Runs at the start of the Act.<br></br>
        /// WARNING! This runs BEFORE the initialization process for fights!<br></br>
        /// If you mean to run something OnWaveStart, use <see cref="OnStartBattleAfter"/> instead.<br></br>
        /// </summary>
        public virtual void OnStartBattle()
        {
        }

        /// <summary>
        /// Runs at the start of the Act.<br></br>
        /// Behaves same as vanilla's OnStartBattle methods.
        /// </summary>
        public virtual void OnStartBattleAfter()
        {
        }
        #endregion

        #region --- UI show / hide / build ---


        /// <summary>
        /// Runs whenever a librarian's <see cref="BattleUnitModel"/> is created for battle.<br></br>
        /// More specifically, <see cref="LogLikeHooks.StageController_CreateLibrarianUnit"/>.
        /// </summary>
        /// <param name="model">The unit that was just created.</param>
        public virtual void OnCreateLibrarian(BattleUnitModel model)
        {
        }

        /// <summary>
        /// Runs *after* all librarians' BattleUnitModels have been created for battle.
        /// </summary>
        public virtual void OnCreateLibrarians()
        {
        }
        #endregion

        #region --- Battle hooks ---


        /// <summary>
        /// Runs when you click on the item. With your cursor.
        /// </summary>
        public virtual void OnClick()
        {
        }
        #endregion

        #region --- Getters / setters / checks ---


        /// <summary>
        /// Used to get the item's sprite.<br></br>
        /// If you wish to dynamically modify the sprite, it is recommended to instead do<br></br>
        /// a conditional overide for <see cref="KeywordIconId"/>, and then<br></br>
        /// run <see cref="GlobalLogueEffectManager.UpdateSprites"/> when you expect the sprite to change.
        /// </summary>
        public virtual Sprite GetSprite()
        {
            Sprite sprite = null;
            string id;
            try
            {
                id = RMRCore.ClassIds[this.GetType().Assembly.FullName];
            }
            catch
            {
                return null;
            }
            if (!string.IsNullOrEmpty(id))
            {
                try
                {
                    if (id == RMRCore.packageId)
                        sprite = LogLikeMod.ArtWorks[KeywordIconId ?? KeywordId];
                    else
                        sprite = LogLikeMod.ModdedArtWorks[(id, KeywordIconId ?? KeywordId)];
                }
                catch
                {
                    return null;
                }
            }
            return sprite;
        }

        /// <summary>
        /// Used to get the item's name.<br></br>
        /// Overriding is not recommended. Refer to <see cref="KeywordId"/> instead.
        /// </summary>
        public virtual string GetEffectName()
        {
            LogueEffectXmlInfo info = null;
            if (!string.IsNullOrEmpty(this.KeywordId))
            {
                try
                {
                    info = LogueEffectXmlList.Instance.GetEffectInfo(KeywordId, RMRCore.ClassIds[this.GetType().Assembly.FullName], this.GetStack());
                }
                catch
                {
                    info = null;
                }
            }
            return info == null ? "" : info.Name;
        }

        /// <summary>
        /// Used to get the item's description.<br></br>
        /// Overriding is not recommended. Refer to <see cref="KeywordId"/> instead.
        /// </summary>
        public virtual string GetEffectDesc()
        {
            LogueEffectXmlInfo info = null;
            if (!string.IsNullOrEmpty(this.KeywordId))
            {
                try
                {
                    info = LogueEffectXmlList.Instance.GetEffectInfo(KeywordId, RMRCore.ClassIds[this.GetType().Assembly.FullName], this.GetStack());
                }
                catch
                {
                    info = null;
                }
            }
            return info == null ? "" : info.Desc + "\n\n" + info.FlavorText;
        }

        /// <summary>
        /// Used to get the item's credenza entry in the Item Catalog.<br></br>
        /// Overriding is not recommended. Refer to <see cref="KeywordId"/> instead.
        /// </summary>
        public virtual string GetCredenzaEntry()
        {
            LogueEffectXmlInfo info = null;
            if (!string.IsNullOrEmpty(this.KeywordId))
            {
                try
                {
                    info = LogueEffectXmlList.Instance.GetEffectInfo(KeywordId, RMRCore.ClassIds[this.GetType().Assembly.FullName], this.GetStack());
                }
                catch
                {
                    info = null;
                }
            }
            return info == null ? TextDataModel.GetText("ui_RMR_ItemNoEntry_Credenza") : info.CatalogDesc;
        }

        /// <summary>
        /// Returns the number to be shown on the item stack counter.<br></br>
        /// Defaults to -1, meaning no stack number is shown.
        /// </summary>
        public virtual int GetStack() => -1;
        #endregion

        #region --- Battle hooks ---


        /// <summary>
        /// [RMR] Runs whenever <see cref="BattleUnitBuf_RMR_CritChance"/> crits successfully.
        /// </summary>
        /// <param name="critter">The unit that is dealing the critical strike.</param>
        /// <param name="target">The unit that is receiving the critical strike.</param>
        public virtual void OnCrit(BattleUnitModel critter, BattleUnitModel target)
        {
        }

        /// <summary>
        /// [RMR] Runs immediately after <see cref="RoguelikeGamemodeBase.OnClearBossWave"/>.
        /// </summary>
        public virtual void AfterClearBossWave()
        {

        }
        #endregion

    }
}
