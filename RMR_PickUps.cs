// =============================================================================
// RMR_PickUps - shop & stage pickup models unique to RMR.
// Regions: SHOP PICK UPS / STAGE PICK UPS. Each type grants a global effect or
// inventory reward when selected in shop or after battle.
// =============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using abcdcode_LOGLIKE_MOD;
using GameSave;

namespace RogueLike_Mod_Reborn
{
    #region --- Shop pickups ---
    [HideFromItemCatalog]
    public class PickUpModel_RMR_BigBrothersChains : ShopPickUpModel
    {
        public PickUpModel_RMR_BigBrothersChains() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90047);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90047));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_BigBrotherChains());
        }
        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }
        public override string KeywordIconId => "RMR_BigBrothersChains";

        public override string KeywordId => "RMR_BigBrothersChains";

    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_Crowbar : ShopPickUpModel
    {
        public PickUpModel_RMR_Crowbar() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90048);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90048));            
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_Crowbar());
        }
        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }

        public override string KeywordIconId => "RMR_Crowbar";

        public override string KeywordId => "RMR_Crowbar";

    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_HarvestScythe : ShopPickUpModel
    {
        public PickUpModel_RMR_HarvestScythe() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90049);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90049));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_HarvesterScythe());
        }

        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }

        public override string KeywordIconId => "RMR_HarvestScythe";

        public override string KeywordId => "RMR_HarvestScythe";

    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_Remote : ShopPickUpModel
    {
        public PickUpModel_RMR_Remote() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90050);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90050));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_Remote());
        }

        public override string KeywordIconId => "RMR_Remote";

        public override string KeywordId => "RMR_Remote";

    }


    [HideFromItemCatalog]
    public class PickUpModel_RMR_BleedingSpleen : ShopPickUpModel
    {
        public PickUpModel_RMR_BleedingSpleen() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90051);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90051));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_BleedingSpleen());
        }

        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }

        public override string KeywordIconId => "RMR_BleedingSpleen";

        public override string KeywordId => "RMR_BleedingSpleen";

    }


    [HideFromItemCatalog]
    public class PickUpModel_RMR_CorrodedChains : ShopPickUpModel
    {
        public PickUpModel_RMR_CorrodedChains() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90052);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90052));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_CorrodedChains());
        }

        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }

        public override string KeywordIconId => "RMR_CorrodedChains";

        public override string KeywordId => "RMR_CorrodedChains";

    }


    /// <summary>Pickup model: PickUpModel_RMR_Polyhedra</summary>


    public class PickUpModel_RMR_Polyhedra : ShopPickUpModel
    {
        public PickUpModel_RMR_Polyhedra() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90069);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90069));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            AddPassiveReward(this.id);
        }

        public override bool IsCanAddShop()
        {
            return LogueBookModels.shopPick.Contains(this.id);
        }

        public override void OnPickUp(BattleUnitModel model)
        {
            base.OnPickUp(model);
            LogueBookModels.AddPlayerStat(model.UnitData, new LogStatAdder()
            {
                maxhp = 1,
                maxbreak = 1,
                speedmax = 1,
                speedmin = 1,
                maxplaypoint = 1,
                startplaypoint = 1
            });
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_Hidden_Polyhedra() { unitIndex = LogueBookModels.GetIndexOfUnit(model) });
        }

        public override string KeywordIconId => "RMRPickUp_Polyhedra";

        public override string KeywordId => "RMRPickUp_Polyhedra";

        [HideFromItemCatalog]
        public class RMREffect_Hidden_Polyhedra : GlobalLogueEffectBase
        {
            public int unitIndex;

            public override void BeforeRollDice(BattleDiceBehavior behavior)
            {
                base.BeforeRollDice(behavior);
                if (behavior.owner.UnitData.unitData == LogueBookModels.playerModel[unitIndex])
                {
                    behavior.ApplyDiceStatBonus(new DiceStatBonus
                    {
                        dmg = 1,
                        breakDmg = 1,
                        guardBreakAdder = 1,
                        min = 1,
                        max = 1
                    });
                }
            }

            public override SaveData GetSaveData()
            {
                SaveData data = base.GetSaveData();
                data.AddData("savedUnit", unitIndex);
                return data;
            }

            public override void LoadFromSaveData(SaveData save)
            {
                base.LoadFromSaveData(save);
                this.unitIndex = save.GetInt("savedUnit");
            }
        }
    }


    [HideFromItemCatalog]
    public class PickUpModel_RMR_WelltunedWeapons : ShopPickUpModel
    {
        public PickUpModel_RMR_WelltunedWeapons() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90053);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90053));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_WelltunedWeapons());
        }

        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }
        public override Rarity ItemRarity => Rarity.Uncommon;
        public override string KeywordIconId => "RMR_WelltunedWeapons";

        public override string KeywordId => "RMR_WelltunedWeapons";

    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_GamblerEye : ShopPickUpModel
    {
        public PickUpModel_RMR_GamblerEye() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90054);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90054));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_GamblerEye());
        }

        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }

        public override Rarity ItemRarity => Rarity.Unique;
        public override string KeywordIconId => "RMR_GamblerEye";

        public override string KeywordId => "RMR_GamblerEye";

    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_OrdinaryClothes : ShopPickUpModel
    {
        public PickUpModel_RMR_OrdinaryClothes() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90055);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90055));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_OrdinaryClothes());
        }

        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }

        public override Rarity ItemRarity => Rarity.Uncommon;
        public override string KeywordIconId => "RMR_OrdinaryClothes";

        public override string KeywordId => "RMR_OrdinaryClothes";

    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_StasisBlaze : ShopPickUpModel
    {
        public PickUpModel_RMR_StasisBlaze() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90056);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90056));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            var effect = new RMREffect_StasisBlaze();
            var mgr = Singleton<GlobalLogueEffectManager>.Instance;
            bool firstAcquire = mgr.GetEffect<RMREffect_StasisBlaze>() == null;
            mgr.AddEffects(effect);
            // AddEffects only calls AddedNew on duplicate stacks; first buy must open the seal UI.
            if (firstAcquire && !mgr.isLoadingSave)
                effect.AddedNew();
        }

        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }

        public override Rarity ItemRarity => Rarity.Rare;
        public override string KeywordIconId => "RMR_StasisBlaze";

        public override string KeywordId => "RMR_StasisBlaze";

    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_StasisSpark : ShopPickUpModel
    {
        public PickUpModel_RMR_StasisSpark() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90057);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90057));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            var effect = new RMREffect_StasisSpark();
            var mgr = Singleton<GlobalLogueEffectManager>.Instance;
            bool firstAcquire = mgr.GetEffect<RMREffect_StasisSpark>() == null;
            mgr.AddEffects(effect);
            // AddEffects only calls AddedNew on duplicate stacks; first buy must open the seal UI.
            if (firstAcquire && !mgr.isLoadingSave)
                effect.AddedNew();
        }

        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }

        public override Rarity ItemRarity => Rarity.Rare;
        public override string KeywordIconId => "RMR_StasisSpark";

        public override string KeywordId => "RMR_StasisSpark";

    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_StasisLight : ShopPickUpModel
    {
        public PickUpModel_RMR_StasisLight() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90058);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90058));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            var effect = new RMREffect_StasisLight();
            var mgr = Singleton<GlobalLogueEffectManager>.Instance;
            bool firstAcquire = mgr.GetEffect<RMREffect_StasisLight>() == null;
            mgr.AddEffects(effect);
            // AddEffects only calls AddedNew on duplicate stacks; first buy must open the seal UI.
            if (firstAcquire && !mgr.isLoadingSave)
                effect.AddedNew();
        }

        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }

       
        public override Rarity ItemRarity => Rarity.Rare;
        public override string KeywordIconId => "RMR_StasisLight";

        public override string KeywordId => "RMR_StasisLight";

    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_Duplicator : ShopPickUpModel
    {
        public PickUpModel_RMR_Duplicator() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90059);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90059));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_Duplicator());
        }

        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }


        public override Rarity ItemRarity => Rarity.Rare;
        public override string KeywordIconId => "RMR_Duplicator";

        public override string KeywordId => "RMR_Duplicator";

    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_ZweiSwordstyle : ShopPickUpModel
    {
        public PickUpModel_RMR_ZweiSwordstyle() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90060);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90060));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_ZweiSwordstyle());
        }
        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }
        public override Rarity ItemRarity => Rarity.Uncommon;
        public override string KeywordIconId => "RMR_ZweiSwordstyle";
        public override string KeywordId => "RMR_ZweiSwordstyle";
    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_ZweiBlock : ShopPickUpModel
    {
        public PickUpModel_RMR_ZweiBlock() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90061);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90061));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_ZweiBlock());
        }
        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }
        public override Rarity ItemRarity => Rarity.Uncommon;
        public override string KeywordIconId => "RMR_ZweiBlock";
        public override string KeywordId => "RMR_ZweiBlock";
    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_RuinedDummy : ShopPickUpModel
    {
        public PickUpModel_RMR_RuinedDummy() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90062);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90062));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_RuinedDummy());
        }
        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }
        public override Rarity ItemRarity => Rarity.Rare;
        public override string KeywordIconId => "RMR_RuinedDummy";
        public override string KeywordId => "RMR_RuinedDummy";
    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_FlickerSwitch : ShopPickUpModel
    {
        public PickUpModel_RMR_FlickerSwitch() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90063);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90063));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_FlickerSwitch());
        }
        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }
        public override Rarity ItemRarity => Rarity.Unique;
        public override string KeywordIconId => "RMR_FlickerSwitch";
        public override string KeywordId => "RMR_FlickerSwitch";
    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_TCorpRestore : ShopPickUpModel
    {
        public PickUpModel_RMR_TCorpRestore() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90064);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90064));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_TCorpRestore());
        }
        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }
        public override Rarity ItemRarity => Rarity.Rare;
        public override string KeywordIconId => "RMR_TCorpRestore";
        public override string KeywordId => "RMR_TCorpRestore";
    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_Timepiece : ShopPickUpModel
    {
        public PickUpModel_RMR_Timepiece() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90065);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90065));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_TCorpRestore());
        }
        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }
        public override Rarity ItemRarity => Rarity.Unique;
        public override string KeywordIconId => "RMR_Timepiece";
        public override string KeywordId => "RMR_Timepiece";
    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_Jokercard : ShopPickUpModel
    {
        public PickUpModel_RMR_Jokercard() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90066);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90066));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_Jokercard());
        }
        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }
        public override Rarity ItemRarity => Rarity.Unique;
        public override string KeywordIconId => "RMR_Jokercard";
        public override string KeywordId => "RMR_Jokercard";
    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_MoonstoneGyro : ShopPickUpModel
    {
        public PickUpModel_RMR_MoonstoneGyro() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90067);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90067));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_MoonstoneGyro());
        }
        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }
        public override Rarity ItemRarity => Rarity.Uncommon;
        public override string KeywordIconId => "RMR_MoonstoneGyro";
        public override string KeywordId => "RMR_MoonstoneGyro";
    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_AvalonStimpack : ShopPickUpModel
    {
        public PickUpModel_RMR_AvalonStimpack() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90068);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90068));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_AvalonStimpack());
        }
        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }
        public override Rarity ItemRarity => Rarity.Unique;
        public override string KeywordIconId => "RMR_AvalonStimpack";
        public override string KeywordId => "RMR_AvalonStimpack";
    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_MourningVeil : ShopPickUpModel
    {
        public PickUpModel_RMR_MourningVeil() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90070);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90070));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_MourningVeil());
        }
        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }
        public override Rarity ItemRarity => Rarity.Common;
        public override string KeywordIconId => "RMR_MourningVeil";
        public override string KeywordId => "RMR_MourningVeil";
    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_TrainingWheel : ShopPickUpModel
    {
        public PickUpModel_RMR_TrainingWheel() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90071);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90071));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_TrainingWheels());
        }
        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }
        public override Rarity ItemRarity => Rarity.Common;
        public override string KeywordIconId => "RMR_TrainingWheels";
        public override string KeywordId => "RMR_TrainingWheels";
    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_BudgetToolkit : ShopPickUpModel
    {
        public PickUpModel_RMR_BudgetToolkit() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90072);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90072));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_BudgetToolkit());
        }
        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }
        public override Rarity ItemRarity => Rarity.Common;
        public override string KeywordIconId => "RMR_BudgetToolkit";
        public override string KeywordId => "RMR_BudgetToolkit";
    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_Satchel : ShopPickUpModel
    {
        public PickUpModel_RMR_Satchel() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90073);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90073));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_Satchel());
        }
        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }
        public override Rarity ItemRarity => Rarity.Common;
        public override string KeywordIconId => "RMR_Satchel";
        public override string KeywordId => "RMR_Satchel";
    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_SentinelBracers : ShopPickUpModel
    {
        public PickUpModel_RMR_SentinelBracers() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90074);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90074));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_SentinelBracers());
        }
        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }
        public override Rarity ItemRarity => Rarity.Common;
        public override string KeywordIconId => "RMR_SentinelBracers";
        public override string KeywordId => "RMR_SentinelBracers";
    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_ConceptConverter : ShopPickUpModel
    {
        public PickUpModel_RMR_ConceptConverter() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90075);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90075));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_ConceptConverter());
        }
        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }
        public override Rarity ItemRarity => Rarity.Common;
        public override string KeywordIconId => "RMR_ConceptConverter";
        public override string KeywordId => "RMR_ConceptConverter";
    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_ColorMixer : ShopPickUpModel
    {
        public PickUpModel_RMR_ColorMixer() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90076);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90076));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_ColorMixer());
        }
        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }
        public override Rarity ItemRarity => Rarity.Common;
        public override string KeywordIconId => "RMR_ColorMixer";
        public override string KeywordId => "RMR_ColorMixer";
    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_WhiteCotton : ShopPickUpModel
    {
        public PickUpModel_RMR_WhiteCotton() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90077);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90077));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_WhiteCotton());
        }
        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }
        public override Rarity ItemRarity => Rarity.Rare;
        public override string KeywordIconId => "RMR_WhiteCotton";
        public override string KeywordId => "RMR_WhiteCotton";
    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_ExposeWeakness : ShopPickUpModel
    {
        public PickUpModel_RMR_ExposeWeakness() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90078);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90078));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_ExposeWeakness());
        }
        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }
        public override Rarity ItemRarity => Rarity.Rare;
        public override string KeywordIconId => "RMR_ExposeWeakness";
        public override string KeywordId => "RMR_ExposeWeakness";
    }

    [HideFromItemCatalog]
    public class PickUpModel_RMR_FindingWeakness : ShopPickUpModel
    {
        public PickUpModel_RMR_FindingWeakness() : base()
        {
            this.id = new LorId(LogLikeMod.ModId, 90079);
            this.rewardinfo = RewardPassivesList.Instance.GetPassiveInfo(new LorId(LogLikeMod.ModId, 90079));
        }
        public override void OnPickUpShop(ShopGoods good)
        {
            Singleton<GlobalLogueEffectManager>.Instance.AddEffects(new RMREffect_FindingWeakness());
        }
        public override bool IsCanAddShop()
        {
            return !LogueBookModels.shopPick.Contains(this.id);
        }
        public override Rarity ItemRarity => Rarity.Common;
        public override string KeywordIconId => "RMR_FindingWeakness";
        public override string KeywordId => "RMR_FindingWeakness";
    }


    #endregion

    #region --- Stage pickups ---
    [HideFromItemCatalog]
    public class PickUpModel_RMR_CopleyConsequences : PickUpModelBase
    {
        public override void LoadFromSaveData(LogueStageInfo stage) => stage.type = abcdcode_LOGLIKE_MOD.StageType.Mystery;

        public PickUpModel_RMR_CopleyConsequences()
        {
            this.Name = TextDataModel.GetText("Stage_Mystery");
            this.Desc = TextDataModel.GetText("Stage_Mystery_Desc");
            this.FlaverText = "";
            this.ArtWork = "Stage_Mystery";
        }
    }
    #endregion
}
