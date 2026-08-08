// -----------------------------------------------------------------------------
// RogueLike Mod Reborn (RMR): RMR_GlobalEffects
// Namespace/file: ruina-roguelike-reborn-main\RMR_GlobalEffects.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using abcdcode_LOGLIKE_MOD;
using GameSave;
using HarmonyLib;
using LOR_DiceSystem;
using UI;
using UnityEngine;

namespace RogueLike_Mod_Reborn
{
    #region STARTER ITEMS
    public class RMREffect_IronHeart : GlobalLogueEffectBase
    {
        #region --- Effect types ---

        public override void OnStartBattleAfter()
        {
            if (LogLikeMod.curstagetype == abcdcode_LOGLIKE_MOD.StageType.Normal || LogLikeMod.curstagetype == abcdcode_LOGLIKE_MOD.StageType.Elite || LogLikeMod.curstagetype == abcdcode_LOGLIKE_MOD.StageType.Boss)
            {
                BattleUnitModel model = BattleObjectManager.instance.GetPatron();
                if (model == null)
                    return;
                if (LogueBookModels.playerBattleModel.Contains<UnitBattleDataModel>(model.UnitData))
                {
                    LogueBookModels.AddPlayerStat(LogueBookModels.playerBattleModel.Find((UnitBattleDataModel x) => x == model.UnitData), new LogStatAdder { maxhp = 1 });
                }
                model.RecoverHP(6);
            }
        }

        public static Rarity ItemRarity = Rarity.Common;

        public override string KeywordId => "RMR_IronHeart";

        public override string KeywordIconId => "RMR_IronHeart";
    }

    /// <summary>RMR type: RMREffect_HunterCloak</summary>

    public class RMREffect_HunterCloak : GlobalLogueEffectBase
    {
        public override void OnStartBattleAfter()
        {
            BattleUnitModel model = BattleObjectManager.instance.GetPatron();
            if (model == null)
                return;
            model.allyCardDetail.DrawCards(2);
        }

        public override void OnRoundStart(StageController stage)
        {
            BattleUnitModel model = BattleObjectManager.instance.GetPatron();
            if (model == null)
                return;
            model.allyCardDetail.AddNewCard(new LorId(RMRCore.packageId, -100), true);
        }

        public static Rarity ItemRarity = Rarity.Common;

        public override string KeywordId => "RMR_HunterCloak";

        public override string KeywordIconId => "RMR_HunterCloak";
    }

    /// <summary>RMR type: RMREffect_ViciousGlasses</summary>

    public class RMREffect_ViciousGlasses : GlobalLogueEffectBase
    {
        public override void OnRoundStart(StageController stage)
        {
            base.OnRoundStart(stage);
            var list = BattleObjectManager.instance.GetAliveList(Faction.Player);
            for (int i = 0; i < list.Count; i++)
            {
                list[i].bufListDetail.AddKeywordBufByEtc(RoguelikeBufs.CritChance, 10);
            }
        }

        public static Rarity ItemRarity = Rarity.Common;

        public override string KeywordId => "RMR_ViciousGlasses";

        public override string KeywordIconId => "RMR_ViciousGlasses";
    }

    /// <summary>RMR type: RMREffect_LightsGuidance</summary>

    public class RMREffect_LightsGuidance : GlobalLogueEffectBase
    {
        public override void OnRoundStart(StageController stage)
        {
            base.OnRoundStart(stage);
            var list = BattleObjectManager.instance.GetAliveList(Faction.Player);
            List<KeywordBuf> list2 = new List<KeywordBuf> { KeywordBuf.SlashPowerUp, KeywordBuf.PenetratePowerUp, KeywordBuf.HitPowerUp, KeywordBuf.DefensePowerUp };
            for (int i = 0; i < list.Count; i++)
            {
                list[i].bufListDetail.AddKeywordBufByEtc(RandomUtil.SelectOne<KeywordBuf>(list2), 1);
            }
        }

        public static Rarity ItemRarity = Rarity.Common;

        public override string KeywordId => "RMR_LightGuidance";

        public override string KeywordIconId => "RMR_LightGuidance";
    }

    /// <summary>RMR type: RMREffect_RoadlessCamelot</summary>

    public class RMREffect_RoadlessCamelot : GlobalLogueEffectBase
    {
        public override void OnRoundStart(StageController stage)
        {
            base.OnRoundStart(stage);
            var list = BattleObjectManager.instance.GetAliveList(Faction.Player);
            RandomUtil.SelectOne<BattleUnitModel>(list).bufListDetail.AddBuf(new CamelotBuf());
        }

        /// <summary>CamelotBuf</summary>

        public class CamelotBuf : BattleUnitBuf
        {
            public override string keywordId => "RMR_RoadlessCamelotBuf";
            public override string keywordIconId => "RMR_RoadlessCamelot";
            public override void OnUseCard(BattlePlayingCardDataInUnitModel card)
            {
                base.OnUseCard(card);
                foreach (BattleUnitModel guy in BattleObjectManager.instance.GetAliveList(_owner.faction).FindAll((BattleUnitModel x) => x != _owner))
                {
                    List<BattleDiceCardModel> list = guy.allyCardDetail.GetHand().FindAll((BattleDiceCardModel x) => x._xmlData.CheckCanUpgrade());
                    if (list.Count > 0)
                    {
                        BattleDiceCardModel cardy = RandomUtil.SelectOne(list);
                        cardy.exhaust = true;
                        cardy.owner.allyCardDetail.ExhaustCardInHand(cardy);
                        guy.allyCardDetail.AddNewCard(Singleton<LogCardUpgradeManager>.Instance.GetUpgradeCard(cardy.GetID()).id);
                    }
                }
            }

            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                this.Destroy();
            }
        }

        public static Rarity ItemRarity = Rarity.Common;

        // Name/Desc come from Localize/*/LogueEffectText via KeywordId (cn/en/kr).
        // Do not hardcode Chinese here — that bypasses localization.

        public override string KeywordId => "RMR_RoadlessCamelot";

        public override string KeywordIconId => "RMR_RoadlessCamelot";
    }

    /// <summary>RMR type: RMREffect_StrangeOrb</summary>

    public class RMREffect_StrangeOrb : GlobalLogueEffectBase
    {
        public override void OnStartBattleAfter()
        {
            StageController.Instance.AddModdedUnit(Faction.Player, new LorId(RMRCore.packageId, -100), -1, 170, new XmlVector2 { x = 20, y = 0 });
            UnitUtil.RefreshCombatUI();
        }

        public static Rarity ItemRarity = Rarity.Common;

        public override string KeywordId
        {
            get
            {
                if (LogLikeMod.itemCatalogActive)
                {
                    return "RMR_StrangeOrb";
                }
                switch (LogLikeMod.curchaptergrade)
                {
                    case ChapterGrade.Grade1:
                        return "RMR_StrangeOrb";
                    case ChapterGrade.Grade2:
                        return "RMR_StrangeOrbGrade2";
                    case ChapterGrade.Grade3:
                        return "RMR_StrangeOrbGrade3";
                    case ChapterGrade.Grade4:
                        return "RMR_StrangeOrbGrade4";
                    case ChapterGrade.Grade5:
                        return "RMR_StrangeOrbGrade5";
                    case ChapterGrade.Grade6:
                        return "RMR_StrangeOrbGrade6";
                    default:
                        return "RMR_StrangeOrb";
                }
            }
        }

        public override string KeywordIconId => "RMR_StrangeOrb";
        /// <summary>Passive ability: PassiveAbility_RMR_StrangeOrbPassive</summary>
        public class PassiveAbility_RMR_StrangeOrbPassive : PassiveAbilityBase
        {
            public override bool isTargetable => false;

            public override void OnRoundStart()
            {
                base.OnRoundStart();
                var alivelist = BattleObjectManager.instance.GetAliveList(owner.faction).FindAll((BattleUnitModel x) => !x.IsDead() && x != owner);
                if (alivelist.Count > 0)
                {
                    var list = BattleObjectManager.instance.GetAliveList(owner.faction).Shuffle();
                    switch (LogLikeMod.curchaptergrade)
                    {
                        case ChapterGrade.Grade1:
                            Effects(1, list);
                            break;
                        case ChapterGrade.Grade2:
                            Effects(1, list);
                            Effects(2, list);
                            break;
                        case ChapterGrade.Grade3:
                            Effects(1, list);
                            Effects(2, list);
                            Effects(3, list);
                            break;
                        case ChapterGrade.Grade4:
                            Effects(1, list);
                            Effects(2, list);
                            Effects(3, list);
                            Effects(4, list);
                            break;
                        case ChapterGrade.Grade5:
                            Effects(1, list);
                            Effects(2, list);
                            Effects(3, list);
                            Effects(4, list);
                            Effects(5, list);
                            break;
                        case ChapterGrade.Grade6:
                            Effects(1, list);
                            Effects(2, list);
                            Effects(3, list);
                            Effects(4, list);
                            Effects(5, list);
                            Effects(6, list);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    owner.Die();
                }




            }

            public override bool IsImmuneBreakDmg(DamageType type)
            {
                return true;
            }

            public override AtkResist GetResistHP(AtkResist origin, BehaviourDetail detail)
            {
                return (AtkResist)Math.Min((int)LogLikeMod.curchaptergrade + 1, 4);
            }

            public override void OnDie()
            {
                owner.view.deadEvent = RMRUtilityExtensions.ExplodeOnDeath;
                base.OnDie();
            }

            private void Effects(int effect, List<BattleUnitModel> list)
            {
                switch (effect)
                {
                    case 1:
                        owner.allyCardDetail.AddNewCard(new LorId(RMRCore.packageId, -104), true);
                        break;

                    case 2:
                        foreach (BattleUnitModel guy in list)
                        {
                            guy.bufListDetail.AddKeywordBufThisRoundByEtc(RoguelikeBufs.RMRShield, 3);
                        }
                        if (Singleton<StageController>.Instance.RoundTurn % 2 == 0)
                        {
                            foreach (BattleUnitModel guy in list)
                            {
                                guy.breakDetail.RecoverBreak(3);
                            }
                        }
                        break;

                    case 3:
                        if (Singleton<StageController>.Instance.RoundTurn == 3)
                        {
                            foreach (BattleUnitModel guy in list)
                            {
                                guy.cardSlotDetail.RecoverPlayPoint(1);
                            }
                        }
                        break;

                    case 4:
                        int random = RandomUtil.Range(1, 3);
                        switch (random)
                        {
                            case 1:
                                foreach (BattleUnitModel guy in list)
                                {
                                    guy.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.DmgUp, 1);
                                }
                                break;

                            case 2:
                                foreach (BattleUnitModel guy in list)
                                {
                                    guy.bufListDetail.AddKeywordBufThisRoundByEtc(RoguelikeBufs.BleedProtection, 1);
                                }
                                break;

                            case 3:
                                foreach (BattleUnitModel guy in list)
                                {
                                    guy.bufListDetail.AddKeywordBufThisRoundByEtc(RoguelikeBufs.BurnProtection, 3);
                                }
                                break;
                        }
                        break;

                    case 5:
                        if (Singleton<StageController>.Instance.RoundTurn == 4)
                        {
                            foreach (BattleUnitModel guy in list)
                            {
                                guy.allyCardDetail.DrawCards(1);
                            }
                        }
                        if (Singleton<StageController>.Instance.RoundTurn >= 4)
                        {
                            RandomUtil.SelectOne<BattleUnitModel>(list.FindAll((BattleUnitModel x) => x != owner)).allyCardDetail.DrawCards(1);
                        }
                        break;

                    case 6:
                        var handlist = new List<BattleDiceCardModel>();
                        foreach (BattleDiceCardModel card in RandomUtil.SelectOne<BattleUnitModel>(list.FindAll((BattleUnitModel x) => x != owner)).allyCardDetail.GetHand())
                        {
                            if (card.GetCost() > 0)
                            {
                                handlist.Add(card);
                            }
                        }
                        if (handlist.Count > 0)
                        {
                            RandomUtil.SelectOne<BattleDiceCardModel>(handlist).AddBuf(new cardcostreductionorb());
                        }
                        break;

                    default:
                        break;
                }
            }

            /// <summary>cardcostreductionorb</summary>

            public class cardcostreductionorb : BattleDiceCardBuf
            {
                public override int GetCost(int oldCost)
                {
                    return oldCost - 1;
                }

                public override void OnUseCard(BattleUnitModel owner)
                {
                    base.OnUseCard(owner);
                    this.Destroy();
                }
            }
        }

    }
    #endregion

    /// <summary>RMR type: RMREffect_CorrodedChains</summary>

    public class RMREffect_CorrodedChains : GlobalLogueEffectBase
    {
        public static Rarity ItemRarity = Rarity.Uncommon;
        public override string KeywordId => "RMR_CorrodedChains";
        public override string KeywordIconId => "RMR_CorrodedChains";
        public override void OnStartBattleAfter()
        {
            var list = BattleObjectManager.instance.GetAliveList(Faction.Player);
            for (int i = 0; i < list.Count; i++)
            {
                list[i].bufListDetail.AddBuf(new RMR_EffectBuf_CorrodedChains());
            }
        }
        /// <summary>RMR type: RMR_EffectBuf_CorrodedChains</summary>
        public class RMR_EffectBuf_CorrodedChains : BattleUnitBuf
        {
            public override void OnSuccessAttack(BattleDiceBehavior behavior)
            {
                base.OnSuccessAttack(behavior);
                int bleedStacks = this._owner.bufListDetail.GetKewordBufStack(KeywordBuf.Bleeding);
                if (bleedStacks > 0)
                {
                    bleedStacks /= 2;
                    BattleUnitModel target = behavior.card.target;
                    target?.bufListDetail?.AddKeywordBufByEtc(KeywordBuf.Bleeding, bleedStacks, _owner);
                }
            }

        }

    }

    /// <summary>RMR type: RMREffect_BleedingSpleen</summary>

    public class RMREffect_BleedingSpleen : GlobalLogueEffectBase
    {
        public static Rarity ItemRarity = Rarity.Rare;
        public override string KeywordId => "RMR_BleedingSpleen";
        public override string KeywordIconId => "RMR_BleedingSpleen";
        public override void OnRoundStart(StageController stage)
        {
            base.OnRoundStart(stage);
            var list = BattleObjectManager.instance.GetAliveList(Faction.Player);
            for (int i = 0; i < list.Count; i++)
            {
                list[i].bufListDetail.AddKeywordBufByEtc(RoguelikeBufs.CritChance, 5);
            }
        }
        public override void OnCrit(BattleUnitModel critter, BattleUnitModel target)
        {
            base.OnCrit(critter, target);
            if (critter.faction == Faction.Player)
                target?.bufListDetail?.AddKeywordBufByEtc(KeywordBuf.Bleeding, 2, critter);
        }
        public override void OnKillUnit(BattleUnitModel killer, BattleUnitModel target)
        {
            base.OnKillUnit(killer, target);
            int BleedToTransfer = target.bufListDetail.GetKewordBufAllStack(KeywordBuf.Bleeding);
            if (BleedToTransfer < 10) return;
            BleedToTransfer /= 2;
            List<BattleUnitModel> EnemyList = BattleObjectManager.instance.GetAliveList(target.faction);
            foreach (var unit in EnemyList)
            {
                if (unit != target)
                {
                    unit?.TakeDamage(BleedToTransfer, DamageType.ETC, killer);
                }
            }
        }
    }

    /// <summary>RMR type: RMREffect_HarvesterScythe</summary>

    public class RMREffect_HarvesterScythe : GlobalLogueEffectBase
    {
        public static Rarity ItemRarity = Rarity.Uncommon;
        public override string KeywordId => "RMR_HarvestScythe";
        public override string KeywordIconId => "RMR_HarvestScythe";
        public override void OnRoundStart(StageController stage)
        {
            base.OnRoundStart(stage);
            var list = BattleObjectManager.instance.GetAliveList(Faction.Player);
            for (int i = 0; i < list.Count; i++)
            {
                list[i].bufListDetail.AddKeywordBufByEtc(RoguelikeBufs.CritChance, 5);
            }
        }
        public override void OnCrit(BattleUnitModel critter, BattleUnitModel target)
        {
            base.OnCrit(critter, target);
            critter?.RecoverHP(5);
        }
    }

    /// <summary>RMR type: RMREffect_Remote</summary>

    public class RMREffect_Remote : GlobalLogueEffectBase
    {
        public static Rarity ItemRarity = Rarity.Uncommon;
        public override string KeywordId => "RMR_Remote";
        public override string KeywordIconId => "RMR_Remote";
        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            if (LogLikeMod.curstagetype == abcdcode_LOGLIKE_MOD.StageType.Normal || LogLikeMod.curstagetype == abcdcode_LOGLIKE_MOD.StageType.Elite)
            {
                BattleUnitModel model = BattleObjectManager.instance.GetPatron();
                if (model != null)
                {
                    model.allyCardDetail.AddNewCard(new LorId(RMRCore.packageId, -103));
                }
            }
        }
    }

    /// <summary>RMR type: RMREffect_BigBrotherChains</summary>

    public class RMREffect_BigBrotherChains : GlobalLogueEffectBase
    {
        int powerUp = 0;
        public override void OnUseCard(BattlePlayingCardDataInUnitModel card)
        {
            if (card.owner.faction == Faction.Enemy) return;
            base.OnUseCard(card);
            powerUp = 0;
            var list = card.owner.allyCardDetail.GetHand();

            foreach (BattleDiceCardModel c in list)
            {
                if (c != null && c.GetID() == card.card.GetID())
                {
                    powerUp++; // if card is the same, add power
                }
            }
            card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
            {
                power = powerUp
            });
            // do this here because for some reason we can't apply it on rolling the dice, which is probably something to do with order of operation
        }
        /*public override void BeforeRollDice(BattleDiceBehavior behavior)
        {
            if (behavior.card.owner.faction == Faction.Enemy) return;
            base.BeforeRollDice(behavior);
            behavior.ApplyDiceStatBonus(new DiceStatBonus
            {
                power = powerUp
            });
        }*/
        // omitted until we know for sure the other solution works

        public static Rarity ItemRarity = Rarity.Rare;
        public override string KeywordIconId => "RMR_BigBrothersChains";
        public override string KeywordId => "RMR_BigBrothersChains";
    }

    // NEEDS LOCALIZATION, OBTAINMENT METHOD AND TESTING
    /// <summary>RMR type: RMREffect_ZeroCounterplay</summary>
    public class RMREffect_ZeroCounterplay : GlobalLogueEffectBase
    {
        bool ZCActive = false;
        int diceTicks = 3;

        public static Rarity ItemRarity = Rarity.Rare;

        public override void OnRoundStart(StageController stage)
        {
            base.OnRoundStart(stage);
            ZCActive = true;
            diceTicks = 3;
        }
        public override void BeforeRollDice(BattleDiceBehavior behavior)
        {
            base.BeforeRollDice(behavior);
            if (ZCActive && behavior.TargetDice != null && behavior.TargetDice.card.GetDiceBehaviorList().Count() > 0)
            {
                var list = behavior.TargetDice.card.owner.cardSlotDetail.keepCard;
                if (list != null && behavior.TargetDice.card == list)
                {
                    foreach (var d in list.cardBehaviorQueue)
                    {
                        if (diceTicks > 0)
                        {
                            d.DestroyDice(DiceUITiming.AttackAfter);
                            diceTicks--;
                        }
                    }
                    ZCActive = false;
                }
            }
            // this has yet to be tested
        }
    }

    /// <summary>RMR type: RMREffect_Crowbar</summary>

    public class RMREffect_Crowbar : GlobalLogueEffectBase
    {
        /// <summary>CrowbarDamageBuf</summary>
        public class CrowbarDamageBuf : BattleUnitBuf
        {
            public override void BeforeRollDice(BattleDiceBehavior behavior)
            {
                base.BeforeRollDice(behavior);
                var enemy = behavior.card.target;
                // this is how you select the enemy 
                if (enemy == null) return;
                if (enemy.hp / (float)enemy.MaxHp >= 0.9) // float thingy means 0.9 = 90%
                {
                    behavior.ApplyDiceStatBonus(new DiceStatBonus { dmgRate = 50, breakRate = 50 });
                    //applying extra damage to the funny die
                }
            }
        }

        //hidden buf because pm are terrible people
        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            foreach (var unit in BattleObjectManager.instance.GetAliveList(Faction.Player))
            {
                unit.bufListDetail.AddBuf(new CrowbarDamageBuf());
            }
        }

        public static Rarity ItemRarity = Rarity.Uncommon;

        public override string KeywordId => "RMR_Crowbar";

        public override string KeywordIconId => "RMR_Crowbar";
    }


    /*
    /// <summary>RMR type: RMREffect_ScrapScavenge</summary>
    public class RMREffect_ScrapScavenge : GlobalLogueEffectBase
    {
        public static Rarity ItemRarity = Rarity.Rare;
        public override string KeywordId => "RMR_ScrapScavenge";

        public override string KeywordIconId => "RMR_ScrapScavenge";


       
        // not sure if there is a method for this as of the moment

    } */

    /// <summary>RMR type: RMREffect_WelltunedWeapons</summary>

    public class RMREffect_WelltunedWeapons : GlobalLogueEffectBase
    {
        public static Rarity ItemRarity = Rarity.Uncommon;
        public override string KeywordId => "RMR_WelltunedWeapons";

        public override string KeywordIconId => "RMR_WelltunedWeapons";

        public override void AddedNew()
        {
            base.AddedNew();
            stack = 0;
            foreach (DiceCardItemModel card in LogueBookModels.cardlist.FindAll(x => x.GetRarity() == Rarity.Uncommon && x.ClassInfo.CheckCanUpgrade()))
            {
                LogueBookModels.AddUpgradeCard(card.GetID(), true);
                LogueBookModels.RemoveCard(card.GetID());
            }
        }

        int stack;

        public override int GetStack()
        {
            return stack;
        }

        /// <summary>imtuningmyweaponslikeatuner</summary>

        public class imtuningmyweaponslikeatuner : BattleUnitBuf
        {
            public override void OnUseCard(BattlePlayingCardDataInUnitModel card)
            {
                base.OnUseCard(card);
                if (card.card.GetRarity() == Rarity.Uncommon)
                {
                    stack++;
                    RMREffect_WelltunedWeapons weapons = Singleton<GlobalLogueEffectManager>.Instance.GetEffect<RMREffect_WelltunedWeapons>();
                    if (weapons != null)
                    {
                        weapons.stack++;
                        if (weapons.stack >= 9)
                        {
                            weapons.stack = 0;
                            _owner.cardSlotDetail.RecoverPlayPoint(2);
                            _owner.bufListDetail.AddBuf(new infinitelightyessirsirsir());
                        }
                        Singleton<GlobalLogueEffectManager>.Instance.UpdateSprites();
                    }
                }
            }

            /// <summary>infinitelightyessirsirsir</summary>

            public class infinitelightyessirsirsir : BattleUnitBuf
            {
                public override void Init(BattleUnitModel owner)
                {
                    base.Init(owner);
                    owner.view.costUI.AddMaxLight(1);
                }
                public override int MaxPlayPointAdder()
                {
                    return 1;
                }
            }
        }
        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            foreach (var unit in BattleObjectManager.instance.GetAliveList(Faction.Player)) 
                unit.bufListDetail.AddBuf(new imtuningmyweaponslikeatuner());
        }
    }

    /// <summary>RMR type: RMREffect_GamblerEye</summary>

    public class RMREffect_GamblerEye : GlobalLogueEffectBase
    {
        public static Rarity ItemRarity = Rarity.Unique;
        public override string KeywordId => "RMR_GamblerEye";

        public override string KeywordIconId => "RMR_GamblerEye";

        public override void AddedNew()
        {
            base.AddedNew();
            stack = 0;
        }

        int stack;

        public override int GetStack()
        {
            return stack;
        }

        /// <summary>letsgogambling</summary>

        public class letsgogambling : BattleUnitBuf
        {
            public override void OnUseCard(BattlePlayingCardDataInUnitModel card)
            {
                base.OnUseCard(card);
                if (card.card.GetRarity() == Rarity.Unique)
                {
                    stack++;
                    RMREffect_GamblerEye gambler = Singleton<GlobalLogueEffectManager>.Instance.GetEffect<RMREffect_GamblerEye>();
                    if (gambler != null)
                    {
                        gambler.stack++;
                    }
                    if (gambler.stack >= 7)
                    {
                        for (int i = 1; i < 7; i++)
                        {
                            Effect(i);
                        }
                        // maybe play a sound here if you feel like it
                        gambler.stack = 0;
                    }
                    else
                    {
                        for (int i = 1; i < 7; i++)
                        {
                            if (RandomUtil.valueForProb <= 0.07f)
                            {
                                Effect(i);
                            }

                        }
                    }
                    Singleton<GlobalLogueEffectManager>.Instance.UpdateSprites();
                }
            }

            private void Effect(int effect)
            {
                switch (effect)
                {
                    case 1:
                        _owner.bufListDetail.AddKeywordBufThisRoundByEtc(RoguelikeBufs.RMRLuck, 2, _owner);
                        break;
                    case 2:
                        _owner.bufListDetail.AddKeywordBufThisRoundByEtc(RoguelikeBufs.CritChance, 5, _owner);
                        break;
                    case 3:
                        _owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Quickness, 2, _owner);
                        break;
                    case 4:
                        _owner.bufListDetail.AddKeywordBufThisRoundByEtc(RoguelikeBufs.RMRStaggerShield, 10, _owner);
                        break;
                    case 5:
                        _owner.cardSlotDetail.RecoverPlayPoint(1);
                        break;
                    case 6:
                        _owner.allyCardDetail.DrawCards(1);
                        break;
                    default:

                        break;
                }
            }
        }
        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            foreach (var unit in BattleObjectManager.instance.GetAliveList(Faction.Player)) unit.bufListDetail.AddBuf(new letsgogambling());

        }
    }

    /// <summary>RMR type: RMREffect_OrdinaryClothes</summary>

    public class RMREffect_OrdinaryClothes : GlobalLogueEffectBase
    {
        //hidden passive because pm are terrible people
        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            foreach (var unit in BattleObjectManager.instance.GetAliveList(Faction.Player))
            {
                unit.passiveDetail.AddPassive(new PassiveAbility_RMR_clotheshiddenpassive());
            }
        }

        /// <summary>Passive ability: PassiveAbility_RMR_clotheshiddenpassive</summary>

        public class PassiveAbility_RMR_clotheshiddenpassive : PassiveAbilityBase
        {
            public override bool DontChangeResistByBreak()
            {
                return true;
            }

            public override void OnBreakState()
            {
                owner?.bufListDetail.AddBufWithoutDuplication(new BattleUnitBuf_luxCouragebuf());
            }

            /// <summary>BattleUnitBuf_luxCouragebuf</summary>

            public class BattleUnitBuf_luxCouragebuf : BattleUnitBuf
            {
                public override AtkResist GetResistHP(AtkResist origin, BehaviourDetail detail)
                {
                    return AtkResist.Vulnerable;
                }
                public override void OnRoundStart()
                {
                    if (!_owner.IsBreakLifeZero())
                    {
                        this.Destroy();
                    }
                }
            }
        }

        public static Rarity ItemRarity = Rarity.Uncommon;

        public override string KeywordId => "RMR_OrdinaryClothes";

        public override string KeywordIconId => "RMR_OrdinaryClothes";
    }


    /// <summary>RMR type: RMREffect_Prescript</summary>


    public class RMREffect_Prescript : OnceEffect
    {
        public bool disable = false;

        public override void Use()
        {

        }

        public override void OnClick()
        {

        }

        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            if (!disable)
            {
                for (int i = 0; i < this.stack; i++)
                {
                    List<BattleUnitModel> list = BattleObjectManager.instance.GetAliveList(Faction.Player).FindAll((BattleUnitModel x) => x.bufListDetail.GetActivatedBufList().Find((BattleUnitBuf y) => y is BattleUnitBuf_RMRPrescriptbuf) == null);
                    if (list.Count > 0)
                    {
                        RandomUtil.SelectOne<BattleUnitModel>(list).bufListDetail.AddBuf(new BattleUnitBuf_RMRPrescriptbuf());
                    }
                }
            }
        }

        /// <summary>BattleUnitBuf_RMRPrescriptbuf</summary>

        public class BattleUnitBuf_RMRPrescriptbuf : BattleUnitBuf
        {
            public override string keywordId => "RMR_Prescriptbuf";
            public override string keywordIconId => "RMR_Prescriptbuf";

            BattleUnitModel target;
            int count;

            public override void OnUseCard(BattlePlayingCardDataInUnitModel card)
            {
                base.OnUseCard(card);
                if (target != null)
                {
                    if (target != card.target)
                    {
                        target = card.target;
                        count = 0;
                        card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                        {
                            dmgRate = -50,
                            breakRate = -50
                        });
                    }
                    else if (target == card.target)
                    {
                        count++;
                        DmgIncrease(card);
                    }
                }
                else
                {
                    count++;
                    target = card.target;
                    DmgIncrease(card);
                }
            }

            private void DmgIncrease(BattlePlayingCardDataInUnitModel card)
            {
                if (count > 3)
                {
                    count = 3;
                }
                card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                {
                    dmgRate = 10 * count,
                    breakRate = 10 * count
                });
            }
        }
        public static Rarity ItemRarity = Rarity.Special;

        public override Sprite GetSprite()
        {
            return disable ? null : base.GetSprite();
        }

        public override string KeywordId => "RMR_Prescript";

        public override string KeywordIconId => "RMR_Prescript";
    }

    /// <summary>RMR type: RMREffect_IronMountain</summary>

    public class RMREffect_IronMountain : GlobalLogueEffectBase
    {
        public static Rarity ItemRarity = Rarity.Uncommon;
        public override string KeywordId => "RMR_IronMountain";

        public override string KeywordIconId => "RMR_IronMountain";

        /// <summary>InflictMPregOnClashWin</summary>

        public class InflictMPregOnClashWin : BattleUnitBuf
        {
            public override void OnWinParrying(BattleDiceBehavior behavior)
            {
                base.OnWinParrying(behavior);
                behavior.card.target?.bufListDetail?.AddKeywordBufByEtc(KeywordBuf.Burn, 1, _owner);
                // question marks are null checking!!
            }
        }
        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            foreach (var unit in BattleObjectManager.instance.GetAliveList(Faction.Player)) unit.bufListDetail.AddBuf(new InflictMPregOnClashWin());

        }
    }

    /// <summary>RMR type: RMREffect_DragonFist</summary>

    public class RMREffect_DragonFist : GlobalLogueEffectBase
    {
        public override string KeywordId => "RMR_DragonFist";
        public override string KeywordIconId => "RMR_DragonFist";

        public static Rarity ItemRarity = Rarity.Uncommon;

        /// <summary>InflictMPregOnHit</summary>

        public class InflictMPregOnHit : BattleUnitBuf
        {
            public override void OnSuccessAttack(BattleDiceBehavior behavior)
            {
                base.OnSuccessAttack(behavior);
                behavior.card.target?.bufListDetail?.AddKeywordBufByEtc(KeywordBuf.Burn, 1, _owner);
                // question marks are null checking!!
            }
        }

        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            foreach (var unit in BattleObjectManager.instance.GetAliveList(Faction.Player)) unit.bufListDetail.AddBuf(new InflictMPregOnHit());
        }
    }


    /// <summary>RMR type: RMREffect_StasisBlaze</summary>


    public class RMREffect_StasisBlaze : GlobalLogueEffectBase
    {
        public static bool IsRandom = true;
        public override string KeywordId => "RMR_StasisBlaze";
        public override string KeywordIconId => "RMR_StasisBlaze";

        public static Rarity ItemRarity = Rarity.Rare;

        LorId card;

        public override void AddedNew()
        {
            base.AddedNew();
            List<DiceCardItemModel> cards = LogueBookModels.GetCardList(false, true);
            if (cards == null || cards.Count == 0 || !cards.Exists(c => c != null && c.num > 0))
            {
                Debug.LogWarning("[RMR] StasisBlaze: no sealable combat pages; effect kept without a sealed card.");
                return;
            }
            MysteryModel_CardChoice.PopupCardChoice(cards, new MysteryModel_CardChoice.ChoiceResult(Effect), MysteryModel_CardChoice.ChoiceDescType.ChooseDesc);
            
        }

        public override SaveData GetSaveData()
        {
            SaveData data = base.GetSaveData();
            data.AddData("RMRStatisBlaze", card.LogGetSaveData());
            return data; 
        }

        public override void LoadFromSaveData(SaveData save)
        {
            base.LoadFromSaveData(save);
            this.card = ExtensionUtils.LogLoadFromSaveData(save.GetData("RMRStatisBlaze"));
        }

        private void Effect(MysteryModel_CardChoice mystery, DiceCardItemModel model)
        {
            card = model.GetID();
            LogueBookModels.DeleteCard(model.GetID());
            UISoundManager.instance.PlayEffectSound(UISoundType.Card_Apply);
            Singleton<MysteryManager>.Instance.EndMystery(mystery);
        }
        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            foreach (var unit in BattleObjectManager.instance.GetAliveList(Faction.Player))
            {
                var card = unit.allyCardDetail.AddNewCard(this.card);
                foreach (DiceBehaviour die in card.XmlData.DiceBehaviourList)
                {
                    die.Min += 2;
                    die.Dice += 1;
                }
            }

        }
    }

    /// <summary>RMR type: RMREffect_StasisSpark</summary>

    public class RMREffect_StasisSpark : GlobalLogueEffectBase
    {
        public static bool IsRandom = true;
        public override string KeywordId => "RMR_StasisSpark";
        public override string KeywordIconId => "RMR_StasisSpark";

        public static Rarity ItemRarity = Rarity.Rare;

        LorId card;

        public override void AddedNew()
        {
            base.AddedNew();
            List<DiceCardItemModel> cards = LogueBookModels.GetCardList(false, true);
            if (cards == null || cards.Count == 0 || !cards.Exists(c => c != null && c.num > 0))
            {
                Debug.LogWarning("[RMR] StasisSpark: no sealable combat pages; effect kept without a sealed card.");
                return;
            }
            MysteryModel_CardChoice.PopupCardChoice(cards, new MysteryModel_CardChoice.ChoiceResult(Effect), MysteryModel_CardChoice.ChoiceDescType.ChooseDesc);

        }

        public override SaveData GetSaveData()
        {
            SaveData data = base.GetSaveData();
            data.AddData("RMRStatisSpark", card.LogGetSaveData());
            return data;
        }

        public override void LoadFromSaveData(SaveData save)
        {
            base.LoadFromSaveData(save);
            this.card = ExtensionUtils.LogLoadFromSaveData(save.GetData("RMRStatisSpark"));
        }

        private void Effect(MysteryModel_CardChoice mystery, DiceCardItemModel model)
        {
            card = model.GetID();
            LogueBookModels.DeleteCard(model.GetID());
            UISoundManager.instance.PlayEffectSound(UISoundType.Card_Apply);
            Singleton<MysteryManager>.Instance.EndMystery(mystery);
        }
        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            foreach (var unit in BattleObjectManager.instance.GetAliveList(Faction.Player))
            {
                var card = unit.allyCardDetail.AddNewCard(this.card);
                card.AddBuf(new Sparkbuf());
            }

        }

        /// <summary>Sparkbuf</summary>

        public class Sparkbuf : BattleDiceCardBuf
        {
            public override void OnUseCard(BattleUnitModel owner)
            {
                base.OnUseCard(owner);
                owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Protection, 3, owner);
                owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.BreakProtection, 3, owner);
            }
        }
    }

    /// <summary>RMR type: RMREffect_StasisLight</summary>

    public class RMREffect_StasisLight : GlobalLogueEffectBase
    {
        public static bool IsRandom = true;
        public override string KeywordId => "RMR_StasisLight";
        public override string KeywordIconId => "RMR_StasisLight";

        public static Rarity ItemRarity = Rarity.Rare;

        LorId card;

        public override void AddedNew()
        {
            base.AddedNew();
            List<DiceCardItemModel> cards = LogueBookModels.GetCardList(false, true);
            if (cards == null || cards.Count == 0 || !cards.Exists(c => c != null && c.num > 0))
            {
                Debug.LogWarning("[RMR] StasisLight: no sealable combat pages; effect kept without a sealed card.");
                return;
            }
            MysteryModel_CardChoice.PopupCardChoice(cards, new MysteryModel_CardChoice.ChoiceResult(Effect), MysteryModel_CardChoice.ChoiceDescType.ChooseDesc);

        }

        public override void LoadFromSaveData(SaveData save)
        {
            base.LoadFromSaveData(save);
            this.card = ExtensionUtils.LogLoadFromSaveData(save.GetData("RMRStatisLight"));
        }

        private void Effect(MysteryModel_CardChoice mystery, DiceCardItemModel model)
        {
            card = model.GetID();
            LogueBookModels.DeleteCard(model.GetID());
            UISoundManager.instance.PlayEffectSound(UISoundType.Card_Apply);
            Singleton<MysteryManager>.Instance.EndMystery(mystery);
        }
        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            foreach (var unit in BattleObjectManager.instance.GetAliveList(Faction.Player))
            {
                var card = unit.allyCardDetail.AddNewCard(this.card);
                card.AddBuf(new Lightbuf());
            }

        }

        /// <summary>Lightbuf</summary>

        public class Lightbuf : BattleDiceCardBuf
        {
            public override void OnUseCard(BattleUnitModel owner)
            {
                base.OnUseCard(owner);
                owner.cardSlotDetail.RecoverPlayPointByCard(1);
                owner.allyCardDetail.DrawCards(1);
            }

            public override int GetCost(int oldCost)
            {
                return oldCost - 1;
            }
        }
    }

    /// <summary>RMR type: RMREffect_Duplicator</summary>

    public class RMREffect_Duplicator : GlobalLogueEffectBase
    {
        public static bool IsRandom = true;
        public override string KeywordId => "RMR_Duplicator";
        public override string KeywordIconId => "RMR_Duplicator";

        public static Rarity ItemRarity = Rarity.Rare;

        /// <summary>Dupe</summary>

        public class Dupe : BattleUnitBuf
        {
            List<BattlePlayingCardDataInUnitModel> cards;
            public override void OnRoundStart()
            {
                base.OnRoundStart();
                cards.Clear();
            }
            public override void OnUseCard(BattlePlayingCardDataInUnitModel card)
            {
                base.OnUseCard(card);
                if (!cards.Contains(card))
                {
                    cards.Add(card);
                }
            }

            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                foreach (BattlePlayingCardDataInUnitModel card in cards)
                {
                    List<BattleUnitModel> list = new List<BattleUnitModel>();
                    foreach (BattleUnitModel goober in BattleObjectManager.instance.GetAliveList(_owner.faction))
                    {
                        Dupe buf = goober.bufListDetail.GetActivatedBufList().Find((BattleUnitBuf x) => x is Dupe) as Dupe;
                        if (buf != null)
                        {
                            if (buf.cards.Contains(card))
                            {
                                
                                buf.cards.Remove(card);
                                list.Add(goober);
                            }
                        }
                    }
                    if (list.Count > 0)
                    {
                        cards.Remove(card);
                        _owner.allyCardDetail.AddNewCard(card.card.GetID());
                        foreach (BattleUnitModel guy in list)
                        {
                            guy.allyCardDetail.AddNewCard(card.card.GetID());
                        }
                    }
                }

            }
        }



        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            foreach (var unit in BattleObjectManager.instance.GetAliveList(Faction.Player)) unit.bufListDetail.AddBuf(new Dupe());
        }
    }

    /// <summary>RMR type: RMREffect_ZweiSwordstyle</summary>

    public class RMREffect_ZweiSwordstyle : GlobalLogueEffectBase
    {
        public static bool IsRandom = true;
        public override string KeywordId => "RMR_ZweiSwordstyle";
        public override string KeywordIconId => "RMR_ZweiSwordstyle";

        public static Rarity ItemRarity = Rarity.Uncommon;

        /// <summary>ZweiBuf</summary>

        public class ZweiBuf : BattleUnitBuf
        {
            public override void OnWinParrying(BattleDiceBehavior behavior)
            {
                base.OnWinParrying(behavior);
                if (behavior.Detail == BehaviourDetail.Slash)
                {
                    _owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Endurance, 1, _owner);
                }
            }
        }

        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            foreach (var unit in BattleObjectManager.instance.GetAliveList(Faction.Player)) unit.bufListDetail.AddBuf(new ZweiBuf());
        }
    }

    /// <summary>RMR type: RMREffect_ZweiBlock</summary>

    public class RMREffect_ZweiBlock : GlobalLogueEffectBase
    {
        public override string KeywordId => "RMR_ZweiBlock";
        public override string KeywordIconId => "RMR_ZweiBlock";

        public static Rarity ItemRarity = Rarity.Uncommon;
        /// <summary>ZweiBuf</summary>
        public class ZweiBuf : BattleUnitBuf
        {
            public override void OnWinParrying(BattleDiceBehavior behavior)
            {
                base.OnWinParrying(behavior);
                if (IsAttackDice(behavior.TargetDice.Detail))
                {
                    if (behavior.Detail == BehaviourDetail.Guard)
                    {
                        _owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Strength, 1, _owner);
                    }
                }
            }
        }

        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            foreach (var unit in BattleObjectManager.instance.GetAliveList(Faction.Player)) unit.bufListDetail.AddBuf(new ZweiBuf());
        }
    }

    /// <summary>RMR type: RMREffect_RuinedDummy</summary>

    public class RMREffect_RuinedDummy : GlobalLogueEffectBase
    {
        public static bool IsRandom = true;
        public override string KeywordId => "RMR_RuinedDummy";
        public override string KeywordIconId => "RMR_RuinedDummy";

        public static Rarity ItemRarity = Rarity.Rare;

        /// <summary>ItemBuf</summary>

        public class ItemBuf : BattleUnitBuf
        {
            public override void OnUseCard(BattlePlayingCardDataInUnitModel card)
            {
                base.OnUseCard(card);
                if (card.card.XmlData?.Script == "")
                {
                    _owner.cardSlotDetail.RecoverPlayPoint(1);
                }
            }

            public override void BeforeRollDice(BattleDiceBehavior behavior)
            {
                base.BeforeRollDice(behavior);
                if (behavior.abilityList.Count == 0 || behavior.behaviourInCard.Script == "")
                {
                    behavior.ApplyDiceStatBonus(new DiceStatBonus
                    {
                        power = 1
                    });
                }
            }
        }

        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            foreach (var unit in BattleObjectManager.instance.GetAliveList(Faction.Player)) unit.bufListDetail.AddBuf(new ItemBuf());
        }
    }

    /// <summary>RMR type: RMREffect_FlickerSwitch</summary>

    public class RMREffect_FlickerSwitch : GlobalLogueEffectBase
    {
        public static bool IsRandom = true;
        public override string KeywordId => "RMR_FlickerSwitch";
        public override string KeywordIconId => "RMR_FlickerSwitch";

        public static Rarity ItemRarity = Rarity.Unique;

        /// <summary>ItemBuf</summary>

        public class ItemBuf : BattleUnitBuf
        {
            public override void OnUseCard(BattlePlayingCardDataInUnitModel card)
            {
                base.OnUseCard(card);
                if (card.card.IsExhaustOnUse()) // this is not foolproof so it would be best if we had a proper method for exhaust triggers - maybe make our own method for exhausting pages and have everything call from that?
                {
                    List<BattleDiceCardModel> list = _owner.allyCardDetail.GetHand().FindAll((BattleDiceCardModel x) => x.GetCost() > 0);
                    if (list.Count > 0)
                    {
                        RandomUtil.SelectOne<BattleDiceCardModel>(list).AddBuf(new costreduction());
                    }
                }
            }
        }

        /// <summary>costreduction</summary>

        public class costreduction : BattleDiceCardBuf
        {
            public override void OnUseCard(BattleUnitModel owner)
            {
                base.OnUseCard(owner);
                this.Destroy();
            }
            public override int GetCost(int oldCost)
            {
                return oldCost - 2;
            }
        }

        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            foreach (var unit in BattleObjectManager.instance.GetAliveList(Faction.Player)) unit.bufListDetail.AddBuf(new ItemBuf());
        }
    }

    /// <summary>RMR type: RMREffect_TCorpRestore</summary>

    public class RMREffect_TCorpRestore : GlobalLogueEffectBase
    {
        public static bool IsRandom = true;
        public override string KeywordId => "RMR_TCorpRestore";
        public override string KeywordIconId => "RMR_TCorpRestore";

        public static Rarity ItemRarity = Rarity.Rare;

        /// <summary>TCorpBuf</summary>

        public class TCorpBuf : BattleUnitBuf
        {
            int dmgtaken;
            bool trigger;
            public override void OnLoseHp(int dmg)
            {
                base.OnLoseHp(dmg);
                dmgtaken += dmg;
                if (dmgtaken >= 15 && !trigger)
                {
                    trigger = true;
                    foreach (BattleUnitModel amog in BattleObjectManager.instance.GetAliveList(_owner.faction))
                    {
                        BattleUnitBuf buf = amog.bufListDetail.GetActivatedBufList().Find((BattleUnitBuf x) => x is RestorationBuf);
                        if (buf != null)
                        {
                            buf.Destroy();
                        }
                    }
                    _owner.bufListDetail.AddBuf(new RestorationBuf(10));
                }
            }
        }

        /// <summary>RestorationBuf</summary>

        public class RestorationBuf : BattleUnitBuf
        {
            public override string keywordId => "RMR_Restoration";
            public override string keywordIconId => "RMR_Restoration";

            public override string bufActivatedText
            {
                get
                {
                    return Singleton<BattleEffectTextsXmlList>.Instance.GetEffectTextDesc(this.keywordId.ToString(), this.stack, this.scenes);
                }
            }

            int scenes;

            public override void Init(BattleUnitModel owner)
            {
                base.Init(owner);
                scenes = 3;
            }
            public RestorationBuf(int stack)
            {
                this.stack = stack;
            }

            public override void OnLoseHp(int dmg)
            {
                base.OnLoseHp(dmg);
                this.stack += dmg / 2;
            }

            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                scenes--;
                if (scenes <= 0)
                {
                    _owner.RecoverHP(this.stack);
                    this.Destroy();
                }
            }
        }

        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            foreach (var unit in BattleObjectManager.instance.GetAliveList(Faction.Player)) unit.bufListDetail.AddBuf(new TCorpBuf());
        }
    }

    /// <summary>RMR type: RMREffect_Timepiece</summary>

    public class RMREffect_Timepiece : GlobalLogueEffectBase
    {
        public static bool IsRandom = true;
        public override string KeywordId => "RMR_Timepiece";
        public override string KeywordIconId => "RMR_Timepiece";

        public static Rarity ItemRarity = Rarity.Unique;
        public override void OnRoundStart(StageController stage) // change this to onroundend when it becomes accessible
        {
            base.OnRoundStart(stage);
            if (StageController.Instance.RoundTurn == 7)
            {
                foreach (BattleUnitModel goober in BattleObjectManager.instance.GetAliveList(Faction.Player))
                {
                    goober.allyCardDetail.DrawCards(3);
                    goober.cardSlotDetail.RecoverPlayPoint(goober.cardSlotDetail.GetMaxPlayPoint());
                    if (goober.emotionDetail.EmotionLevel < 5)
                    {
                        goober.emotionDetail.LevelUp_Forcely(1);
                    }
                }
            }
        }
    }

    /// <summary>RMR type: RMREffect_Jokercard</summary>

    public class RMREffect_Jokercard : GlobalLogueEffectBase
    {
        public static bool IsRandom = true;
        public override string KeywordId => "RMR_Jokercard";
        public override string KeywordIconId => "RMR_Jokercard";

        public static Rarity ItemRarity = Rarity.Unique;
        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            foreach (var unit in BattleObjectManager.instance.GetAliveList(Faction.Player)) unit.allyCardDetail.AddNewCard(new LorId(RMRCore.packageId, -105), true);
        }
    }


    /// <summary>RMR type: RMREffect_MoonstoneGyro</summary>


    public class RMREffect_MoonstoneGyro : GlobalLogueEffectBase
    {
        public static bool IsRandom = true;
        public override string KeywordId => "RMR_MoonstoneGyro";
        public override string KeywordIconId => "RMR_MoonstoneGyro";

        public static Rarity ItemRarity = Rarity.Uncommon;

        /// <summary>Passive ability: PassiveAbility_RMR_moonstonegyro</summary>

        public class PassiveAbility_RMR_moonstonegyro : PassiveAbilityBase
        {
            public override void OnDiscardByAbility(List<BattleDiceCardModel> cards)
            {
                base.OnDiscardByAbility(cards);
                foreach (BattleDiceCardModel card in cards)
                {
                    owner.breakDetail.RecoverBreak(card.GetCost() + 2);
                }
            }
        }


        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            foreach (var unit in BattleObjectManager.instance.GetAliveList(Faction.Player)) unit.passiveDetail.AddPassive(new PassiveAbility_RMR_moonstonegyro());
        }
    }

    /// <summary>RMR type: RMREffect_Duffelbag</summary>

    public class RMREffect_Duffelbag : GlobalLogueEffectBase
    {
        public static bool IsRandom = true;
        public override string KeywordId => "RMR_Duffelbag";
        public override string KeywordIconId => "RMR_Duffelbag";

        public static Rarity ItemRarity = Rarity.Rare;

        public override void AddedNew()
        {
            base.AddedNew();
            LogueBookModels.AddMoney(150);
        }
    }

    /// <summary>RMR type: RMREffect_AvalonStimpack</summary>

    public class RMREffect_AvalonStimpack : GlobalLogueEffectBase
    {
        public static bool IsRandom = true;
        public override string KeywordId => "RMR_AvalonStimpack";
        public override string KeywordIconId => "RMR_AvalonStimpack";

        public static Rarity ItemRarity = Rarity.Unique;

        /// <summary>Passive ability: PassiveAbility_RMR_avalon</summary>

        public class PassiveAbility_RMR_avalon : PassiveAbilityBase
        {
            int triggers;
            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                triggers = 0;
            }
            public override int OnAddKeywordBufByCard(BattleUnitBuf buf, int stack)
            {
                if (buf.positiveType == BufPositiveType.Negative && triggers < 3)
                {
                    triggers++;
                    owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Resistance, 1, owner);
                }
                return base.OnAddKeywordBufByCard(buf, stack);
            }
        }

        public override void OnRoundStart(StageController stage)
        {
            base.OnRoundStart(stage);
            foreach(BattleUnitModel goober in BattleObjectManager.instance.GetAliveList(Faction.Player))
            {
                List<BattleUnitBuf> debuffs = goober.bufListDetail.GetActivatedBufList().FindAll((BattleUnitBuf x) => x.positiveType == BufPositiveType.Negative);
                if (debuffs.Count > 0)
                {
                    BattleUnitBuf buf = RandomUtil.SelectOne<BattleUnitBuf>(debuffs);
                    BattleUnitBuf resilience = goober.bufListDetail.GetActivatedBuf(KeywordBuf.Resistance);
                    if (resilience != null)
                    {
                        int stack = resilience.stack;
                        if (stack < buf.stack)
                        {
                            buf.stack -= stack;
                            resilience.Destroy();
                        }
                        else
                        {
                            buf.stack -= stack;
                            resilience.stack -= stack;
                        }
                    }
                }
            }
        }

        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            foreach (var unit in BattleObjectManager.instance.GetAliveList(Faction.Player)) unit.passiveDetail.AddPassive(new PassiveAbility_RMR_avalon());
        }
    }

    /// <summary>RMR type: RMREffect_MourningVeil</summary>

    public class RMREffect_MourningVeil : GlobalLogueEffectBase
    {
        public static bool IsRandom = true;
        public override string KeywordId => "RMR_MourningVeil";
        public override string KeywordIconId => "RMR_MourningVeil";

        public static Rarity ItemRarity = Rarity.Common;



        public override void OnDieUnit(BattleUnitModel unit)
        {
            base.OnDieUnit(unit);
            if (unit.faction == Faction.Player)
            {
                BattleUnitModel guy = RandomUtil.SelectOne<BattleUnitModel>(BattleObjectManager.instance.GetAliveList(Faction.Player));
                guy.RecoverHP((int)(unit.MaxHp * .3));
                guy.breakDetail.RecoverBreak((int)(unit.breakDetail.GetDefaultBreakGauge() * .3));
            }
        }


    }

    /// <summary>RMR type: RMREffect_TrainingWheels</summary>

    public class RMREffect_TrainingWheels : GlobalLogueEffectBase
    {
        public override string KeywordId => "RMR_TrainingWheels";
        public override string KeywordIconId => "RMR_TrainingWheels";

        public static Rarity ItemRarity = Rarity.Common;


        /// <summary>ItemBuf</summary>


        public class ItemBuf : BattleUnitBuf
        {
            public override void OnRollSpeedDice()
            {
                base.OnRollSpeedDice();
                int slowest = 999;
                foreach (BattleUnitModel guy in BattleObjectManager.instance.GetAliveList(Faction.Player))
                {
                    foreach (SpeedDice die in guy.speedDiceResult)
                    {
                        if (slowest > die.value)
                        {
                            slowest = die.value;
                        }
                    }
                }
                foreach (BattleUnitModel guy in BattleObjectManager.instance.GetAliveList(Faction.Player))
                {
                    foreach (SpeedDice die in guy.speedDiceResult)
                    {
                        if (die.value == slowest)
                        {
                            die.value += 3;
                        }
                    }
                    guy.speedDiceResult.Sort(delegate (SpeedDice d1, SpeedDice d2)
                    {
                        if (d1.breaked && d2.breaked)
                        {
                            if (d1.value > d2.value)
                            {
                                return -1;
                            }
                            if (d1.value < d2.value)
                            {
                                return 1;
                            }
                            return 0;
                        }
                        if (d1.breaked && !d2.breaked)
                        {
                            return -1;
                        }
                        if (!d1.breaked && d2.breaked)
                        {
                            return 1;
                        }
                        if (d1.value > d2.value)
                        {
                            return -1;
                        }
                        return (d1.value < d2.value) ? 1 : 0;
                    });
                }
                this.Destroy();
            }

        }

        public override void OnRoundStart(StageController stage)
        {
            base.OnRoundStart(stage);
            RandomUtil.SelectOne<BattleUnitModel>(BattleObjectManager.instance.GetAliveList(Faction.Player)).bufListDetail.AddBuf(new ItemBuf());
        }

    }

    /// <summary>RMR type: RMREffect_BudgetToolkit</summary>

    public class RMREffect_BudgetToolkit : GlobalLogueEffectBase
    {
        public static bool IsRandom = true;
        public override string KeywordId => "RMR_BudgetToolkit";
        public override string KeywordIconId => "RMR_BudgetToolkit";

        public static Rarity ItemRarity = Rarity.Common;

        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            foreach (var unit in BattleObjectManager.instance.GetAliveList(Faction.Player))
            {
                    List<BattleDiceCardModel> list = unit.allyCardDetail.GetDeck().FindAll((BattleDiceCardModel x) => x._xmlData.CheckCanUpgrade());
                    if (list.Count > 0)
                    {
                        BattleDiceCardModel cardy = RandomUtil.SelectOne(list);
                        cardy.exhaust = true;
                        cardy.owner.allyCardDetail.ExhaustCardInHand(cardy);
                        unit.allyCardDetail.AddNewCardToDeck(Singleton<LogCardUpgradeManager>.Instance.GetUpgradeCard(cardy.GetID()).id);
                    }
                
            }
        }
    }

    /// <summary>RMR type: RMREffect_Satchel</summary>

    public class RMREffect_Satchel : GlobalLogueEffectBase
    {
        public static bool IsRandom = true;
        public override string KeywordId => "RMR_Satchel";
        public override string KeywordIconId => "RMR_Satchel";

        public static Rarity ItemRarity = Rarity.Common;

        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            foreach (var unit in BattleObjectManager.instance.GetAliveList(Faction.Player))
            {
                unit.allyCardDetail.SetMaxHand(unit.allyCardDetail._maxHand + 1);
                unit.allyCardDetail.SetMaxDrawHand(unit.allyCardDetail._maxDrawHand + 1);
                unit.allyCardDetail.DrawCards(1);
            }
        }
    }

    /// <summary>RMR type: RMREffect_SentinelBracers</summary>

    public class RMREffect_SentinelBracers : GlobalLogueEffectBase
    {
        public static bool IsRandom = true;
        public override string KeywordId => "RMR_SentinelBracers";
        public override string KeywordIconId => "RMR_SentinelBracers";

        public static Rarity ItemRarity = Rarity.Common;

        /// <summary>Passive ability: PassiveAbility_RMR_bracers</summary>

        public class PassiveAbility_RMR_bracers : PassiveAbilityBase
        {
            bool trigger;
            public override void OnStartBattle()
            {
                base.OnStartBattle();
                if (!trigger)
                {
                    if (owner.cardSlotDetail.cardQueue.Count == 0)
                    {
                        Effect();
                    }
                    else
                    {
                        bool offense = false;
                        foreach (BattlePlayingCardDataInUnitModel card in owner.cardSlotDetail.cardQueue)
                        {
                            foreach (BattleDiceBehavior die in card.GetDiceBehaviorList())
                            {
                                if (IsAttackDice(die.Detail))
                                {
                                    offense = true;
                                }
                            }
                        }
                        if (!offense)
                        {
                            Effect();
                        }
                    }
                }
            }

            private void Effect()
            {
                owner.bufListDetail.AddKeywordBufThisRoundByEtc(RoguelikeBufs.RMRShield, 8, owner);
                owner.bufListDetail.AddKeywordBufThisRoundByEtc(RoguelikeBufs.RMRStaggerShield, 8, owner);
            }

            public override void OnRoundStart()
            {
                base.OnRoundStart();
                trigger = false;
            }
        }
        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            foreach (var unit in BattleObjectManager.instance.GetAliveList(Faction.Player)) unit.passiveDetail.AddPassive(new PassiveAbility_RMR_bracers());
        }
    }

    /// <summary>RMR type: RMREffect_ConceptConverter</summary>

    public class RMREffect_ConceptConverter : GlobalLogueEffectBase
    {
        public static bool IsRandom = true;
        public override string KeywordId => "RMR_ConceptConverter";
        public override string KeywordIconId => "RMR_ConceptConverter";

        public static Rarity ItemRarity = Rarity.Common;

        
        public override void OnSkipCardRewardChoose(List<DiceCardXmlInfo> cardlist)
        {
            base.OnSkipCardRewardChoose(cardlist);
            LogueBookModels.AddMoney(2);
        }
    }


    /// <summary>RMR type: RMREffect_ColorMixer</summary>


    public class RMREffect_ColorMixer : GlobalLogueEffectBase
    {
        public static bool IsRandom = true;
        public override string KeywordId => "RMR_ColorMixer";
        public override string KeywordIconId => "RMR_ColorMixer";

        public static Rarity ItemRarity = Rarity.Common;

        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            foreach (var unit in BattleObjectManager.instance.GetAliveList(Faction.Player))
            {
                bool common = false;
                bool uncommon = false;
                bool rare = false;
                bool unique = false;
                foreach (BattleDiceCardModel card in unit.allyCardDetail.GetAllDeck())
                {
                    switch (card.GetRarity())
                    {
                        case Rarity.Common:
                            common = true;
                            break;
                        case Rarity.Uncommon:
                            uncommon = true;
                            break;
                        case Rarity.Rare:
                            rare = true;
                            break;
                        case Rarity.Unique:
                            unique = true;
                            break;
                        default:
                            break;
                    }
                }
                if (common && uncommon && rare && unique)
                {
                    unit.allyCardDetail.DrawCards(1);
                    List<BattleDiceCardModel> hand = unit.allyCardDetail.GetHand().FindAll((BattleDiceCardModel x) => x.GetCost() > 0);
                    if (hand.Count > 0)
                    {
                        hand.SortReturn((x, y) => x.GetRarity() - y.GetRarity()).Last().AddBuf(new costreduction());
                    }
                }
            }
        }

        /// <summary>costreduction</summary>

        public class costreduction : BattleDiceCardBuf
        {
            public override void OnUseCard(BattleUnitModel owner)
            {
                base.OnUseCard(owner);
                this.Destroy();
            }
            public override int GetCost(int oldCost)
            {
                return oldCost - 1;
            }
        }
    }

    /// <summary>RMR type: RMREffect_WhiteCotton</summary>

    public class RMREffect_WhiteCotton : GlobalLogueEffectBase
    {
        public static bool IsRandom = true;
        public override string KeywordId => "RMR_WhiteCotton";
        public override string KeywordIconId => "RMR_WhiteCotton";

        public static Rarity ItemRarity = Rarity.Rare;

        bool isactive;

        public override void OnClick()
        {
            base.OnClick();
            isactive = !isactive;
            foreach (var unit in BattleObjectManager.instance.GetAliveList())
            {
                PassiveAbility_RMR_hehehehastagger gossypassive = unit.passiveDetail.PassiveList.Find((PassiveAbilityBase x) => x is PassiveAbility_RMR_hehehehastagger) as PassiveAbility_RMR_hehehehastagger;
                if (gossypassive != null)
                {
                    gossypassive.active = isactive;
                }
            }
            GlobalLogueEffectManager.Instance.UpdateSprites();
            if (isactive)
            {
                UISoundManager.instance.PlayEffectSound(UISoundType.Card_Apply);
            }
            else
            {
                UISoundManager.instance.PlayEffectSound(UISoundType.Ui_Cancel);
            }
            
        }
        public override void OnRoundStart(StageController stage)
        {
            base.OnRoundStart(stage);
            foreach (var unit in BattleObjectManager.instance.GetAliveList(Faction.Player))
            {
                unit.bufListDetail.AddKeywordBufThisRoundByEtc(RoguelikeBufs.BleedProtection, 1, unit);
            }
        }
        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            foreach (var unit in BattleObjectManager.instance.GetAliveList()) unit.passiveDetail.AddPassive(new PassiveAbility_RMR_hehehehastagger());
        }

        /// <summary>Passive ability: PassiveAbility_RMR_hehehehastagger</summary>

        public class PassiveAbility_RMR_hehehehastagger : PassiveAbilityBase
        {
            public bool active;

            public override void Init(BattleUnitModel self)
            {
                base.Init(self);
                RMREffect_WhiteCotton gossy = Singleton<GlobalLogueEffectManager>.Instance.GetEffectList().Find(x => x is RMREffect_WhiteCotton) as RMREffect_WhiteCotton;
                if (gossy != null)
                {
                    active = gossy.isactive;
                }
            }
            public override bool OnBreakGageZero()
            {
                if (active)
                {
                    owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Bleeding, owner.breakDetail.GetDefaultBreakGauge() / 6, owner);
                    owner.breakDetail.ResetGauge();
                    return true;
                }
                return base.OnBreakGageZero();
            }
        }
    }


    /// <summary>RMR type: RMREffect_ExposeWeakness</summary>


    public class RMREffect_ExposeWeakness : GlobalLogueEffectBase
    {
        public static bool IsRandom = true;
        public override string KeywordId => "RMR_ExposeWeakness";
        public override string KeywordIconId => "RMR_ExposeWeakness";

        public static Rarity ItemRarity = Rarity.Rare;

        public override void OnRoundStart(StageController stage)
        {
            base.OnRoundStart(stage);
            List<BattleUnitModel> list = BattleObjectManager.instance.GetAliveList(Faction.Player);
            int highesthp = 0;
            if (list.Count > 0)
            {
                foreach (BattleUnitModel guy in list)
                {
                    if (guy.hp > highesthp)
                    {
                        highesthp = (int)guy.hp;
                    }
                }
            }
            List<BattleUnitModel> list2 = new List<BattleUnitModel>();
            foreach (BattleUnitModel goober in list)
            {
                if (goober.hp == highesthp)
                {
                    list2.Add(goober);
                }
            }
            if (list2.Count > 0)
            {
                RandomUtil.SelectOne<BattleUnitModel>(list2).bufListDetail.AddBuf(new EnGardeBuf());
            }
        }

        /// <summary>EnGardeBuf</summary>

        public class EnGardeBuf : BattleUnitBuf
        {
            public override string keywordId => "RMR_Engarde";
            public override string keywordIconId => "RMR_Engarde";

            BattleUnitModel target;

            public override void OnWinParrying(BattleDiceBehavior behavior)
            {
                base.OnWinParrying(behavior);
                if (target == behavior.card.target)
                {
                    this.stack++;
                    if (this.stack >= 3)
                    {
                        target.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Vulnerable, 1, _owner);
                        target.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Vulnerable_break, 1, _owner);
                        this.stack -= 3;
                    }
                }
                else
                {
                    target = behavior.card.target;
                    this.stack = 1;
                }
            }
        }
    }

    /// <summary>RMR type: RMREffect_FindingWeakness</summary>

    public class RMREffect_FindingWeakness : GlobalLogueEffectBase
    {
        public static bool IsRandom = true;
        public override string KeywordId => "RMR_FindingWeakness";
        public override string KeywordIconId => "RMR_FindingWeakness";

        public static Rarity ItemRarity = Rarity.Common;

        /// <summary>ItemBuf</summary>

        public class ItemBuf : BattleUnitBuf
        {
            public override void OnWinParrying(BattleDiceBehavior behavior)
            {
                base.OnWinParrying(behavior);
                if (behavior.card?.target?.bufListDetail.GetActivatedBuf(KeywordBuf.Strength) != null)
                {
                    behavior.card.target.TakeDamage(2, DamageType.ETC);
                }
                if (behavior.card?.target?.bufListDetail.GetActivatedBuf(KeywordBuf.Endurance) != null)
                {
                    behavior.card.target.breakDetail.TakeBreakDamage(2, DamageType.ETC);
                }
            }
        }

        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            foreach (var unit in BattleObjectManager.instance.GetAliveList()) unit.bufListDetail.AddBuf(new ItemBuf());
        }
    }



    /// <summary>
    /// Hidden effect that is added on gamemode initialization<br></br>
    /// Basekit 20% chance to find upgraded cards
    /// </summary>
    [HideFromItemCatalog]
    public class RMREffect_HiddenUpgradeChanceEffect : GlobalLogueEffectBase
    {
        public override void ChangeShopCard(ref DiceCardXmlInfo card)
        {
            base.ChangeShopCard(ref card);
            if (card.CheckCanUpgrade())
            {
                if (UnityEngine.Random.value < 0.20f)
                {
                    card = Singleton<LogCardUpgradeManager>.Instance.GetUpgradeCard(card.id);
                }
            }
        }

        public override void ChangeCardReward(ref List<DiceCardXmlInfo> cardlist)
        {
            List<DiceCardXmlInfo> list = new List<DiceCardXmlInfo>();
            foreach (DiceCardXmlInfo diceCardXmlInfo in cardlist)
            {
                if (!diceCardXmlInfo.CheckCanUpgrade())
                {
                    list.Add(diceCardXmlInfo);
                }
                else
                {
                    if (UnityEngine.Random.value < 0.20f)
                    {
                        list.Add(Singleton<LogCardUpgradeManager>.Instance.GetUpgradeCard(diceCardXmlInfo.id));
                    }
                    else
                    {
                        list.Add(diceCardXmlInfo);
                    }
                }
            }
            cardlist = list;
        }
    }

    /// <summary>
    /// Hidden effect that is added on base gamemode initialization to allow for effects like Zeal.
    /// </summary>
    [HideFromItemCatalog]
    public class RMREffect_ExtendedFunctionalityEffect : GlobalLogueEffectBase
    {
        /// <summary>
        /// Executes the "OnWaveStart" override for RMRCardSelfAbilityBase.
        /// </summary>
        public override void OnStartBattleAfter()
        {
            base.OnStartBattleAfter();
            try
            {
                if (BattleObjectManager.instance == null)
                    return;
                var scriptField = AccessTools.Field(typeof(BattleDiceCardModel), "_script");
                foreach (var unit in BattleObjectManager.instance.GetAliveList())
                {
                    if (unit?.allyCardDetail == null)
                        continue;
                    List<BattleDiceCardModel> deck = null;
                    try { deck = unit.allyCardDetail.GetDeck(); } catch { continue; }
                    if (deck == null)
                        continue;
                    foreach (var page in deck)
                    {
                        if (page == null)
                            continue;
                        object scriptObj = null;
                        try
                        {
                            scriptObj = scriptField != null ? scriptField.GetValue(page) : null;
                        }
                        catch
                        {
                            continue;
                        }
                        if (scriptObj is RMRCardSelfAbilityBase rmr)
                        {
                            try { rmr.OnWaveStart(page, unit); }
                            catch (Exception ex)
                            {
                                Debug.LogWarning("[RMR] OnWaveStart card script failed: " + ex.Message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RMR] RMREffect_ExtendedFunctionalityEffect.OnStartBattleAfter: " + ex.Message);
            }
        }
        #endregion

    }
}


