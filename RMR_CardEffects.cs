// =============================================================================
// RMR_CardEffects - DiceCardSelfAbility / dice ability scripts for RMR pages.
// Base: RMRCardSelfAbilityBase. Organized by urban grade regions where present.
// Gameplay scripts only; pool isolation lives in RMRAbnormalityUnlockManager.
// =============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using abcdcode_LOGLIKE_MOD;
using LOR_BattleUnit_UI;
using LOR_DiceSystem;
using LOR_XML;
using UnityEngine;

namespace RogueLike_Mod_Reborn
{
    /// <summary>
    /// A DiceCardSelfAbilityBase with some additional overrides that interact with RMR mechanics.
    /// </summary>
    public class RMRCardSelfAbilityBase : DiceCardSelfAbilityBase
    {
        public virtual void OnWaveStart(BattleDiceCardModel self, BattleUnitModel owner)
        {
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Starter_Evade</summary>

    public class DiceCardSelfAbility_RMR_Starter_Evade : DiceCardSelfAbilityBase
    {
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.allyCardDetail.DrawCards(1);
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Endurance, 1, owner);
        }

        // public static string Desc = "[On Use] Draw 1 page and gain 1 Endurance this Scene";
        public override string[] Keywords => new string[]{
        "DrawCard_Keyword", "Endurance_Keyword"
        };
    }
    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Starter_CoordinatedStrikes</summary>
    public class DiceCardSelfAbility_RMR_Starter_CoordinatedStrikes : DiceCardSelfAbilityBase
    {
        public override void OnUseCard()
        {
            base.OnUseCard();
            int pow = 0;
            foreach (var unit in BattleObjectManager.instance.GetAliveList())
            {
                foreach (var c in unit.cardSlotDetail.cardAry)
                {
                    if (c != null && c.target == card.target && c.card.GetID() == card.card.GetID())
                        pow++;
                }
            }
            --pow;
            card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus { power = pow });
        }

        // public static string Desc = "[On Use] Dice on this page gain +1 Power for each copy of this page being used against the same target";
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Gain2AhnOnKill</summary>

    public class DiceCardSelfAbility_RMR_Gain2AhnOnKill : DiceCardSelfAbilityBase
    {
        public override void OnEndBattle()
        {
            if (card.target.IsDead())
            {
                LogueBookModels.AddMoney(2);
            }
        }

        // public static string Desc = "[On Kill] Gain 2 Ahn";
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_ShivThrow</summary>

    public class DiceCardSelfAbility_RMR_ShivThrow : DiceCardSelfAbilityBase
    {
        /// <summary>ScrollAbility_RMR_Shiv</summary>
        public class ScrollAbility_RMR_Shiv : ScrollAbilityBase
        {
            public override void OnScrollDown(BattleUnitModel unit, BattleDiceCardModel self)
            {
                base.OnScrollDown(unit, self);
                if (self.GetCost() > 0)
                {
                    var card = self.XmlData;
                    if (self._originalXmlData == null)
                        self.CopySelf();
                    self.AddCost(-1);
                    List<DiceBehaviour> dicelist = card.DiceBehaviourList;
                    dicelist[0].Min = 1 + (2 * self.GetCost());
                    dicelist[0].Dice = 3 + (2 * self.GetCost());
                    card.DiceBehaviourList = dicelist;
                }
            }


            public override void OnScrollUp(BattleUnitModel unit, BattleDiceCardModel self)
            {
                base.OnScrollUp(unit, self);
                if (self.GetCost() < 5 && self.GetCost() + 1 <= unit.PlayPoint - unit.cardSlotDetail.ReservedPlayPoint)
                {
                    var card = self.XmlData;
                    if (self._originalXmlData == null)
                        self.CopySelf();
                    self.AddCost(1);
                    List<DiceBehaviour> dicelist = card.DiceBehaviourList;
                    dicelist[0].Min = 1 + (2 * self.GetCost());
                    dicelist[0].Dice = 3 + (2 * self.GetCost());
                    card.DiceBehaviourList = dicelist;
                }
            }
        }

        public override void OnAddToHand(BattleUnitModel owner)
        {
            base.OnAddToHand(owner);
            owner.AddScrollAbility<ScrollAbility_RMR_Shiv>(card.card);
            card.card.SetCurrentCost(card.card.GetOriginCost());
            card.card.ResetToOriginalData();
        }
        
        public override void OnUseInstance(BattleUnitModel unit, BattleDiceCardModel self, BattleUnitModel targetUnit)
        {
            base.OnUseInstance(unit, self, targetUnit);
            self.exhaust = true;
            BattleDiceBehavior battleDiceBehavior = self.CreateDiceCardBehaviorList()[0];
            var list = unit.allyCardDetail.GetHand();
            list.RemoveAll(x => x.GetID() == self.GetID());
            list.SortByCost();
            if (list[0] != null)
            {
                if (list[0]._originalXmlData == null)
                    list[0].CopySelf();
                List<DiceBehaviour> dicelist = list[0].XmlData.DiceBehaviourList;
                dicelist.Add(battleDiceBehavior.behaviourInCard);
                list[0].XmlData.DiceBehaviourList = dicelist;
                unit.bufListDetail.AddBufWithoutDuplication(new killshivsagaga());
            }
        }

        /// <summary>killshivsagaga</summary>

        public class killshivsagaga : BattleUnitBuf
        {
            public override void OnEndBattle(BattlePlayingCardDataInUnitModel curCard)
            {
                base.OnEndBattle(curCard);
                List<DiceBehaviour> dicelist = curCard.card.XmlData.DiceBehaviourList;
                dicelist.RemoveAll(x => x.Script == "RMR_Shivhidden");
                curCard.card.XmlData.DiceBehaviourList = dicelist;
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_Shivhidden</summary>

    public class DiceCardAbility_RMR_Shivhidden : DiceCardAbilityBase
    {
        
    }
    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Remote</summary>
    public class DiceCardSelfAbility_RMR_Remote : DiceCardSelfAbilityBase
    {
        public override void OnUseInstance(BattleUnitModel unit, BattleDiceCardModel self, BattleUnitModel targetUnit)
        {
            base.OnUseInstance(unit, self, targetUnit);
            self.exhaust = true;
            var list = targetUnit.cardSlotDetail.cardAry;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].card.CurCost > 0)
                {
                    targetUnit.cardSlotDetail.cardAry[i] = null;
                }
            }
            targetUnit.cardSlotDetail.SetPlayPoint(0);
            SingletonBehavior<BattleManagerUI>.Instance.ui_TargetArrow.UpdateTargetList();
            GlobalLogueEffectBase effect = GlobalLogueEffectManager.Instance.GetEffect<RMREffect_Remote>();
            if (effect != null)
            {
                effect.Destroy();
            }
            
        }
    }

    #region Canard
    public class DiceCardSelfAbility_RMR_Track : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Recover_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.RecoverHP(4);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_TrackUpgrade</summary>

    public class DiceCardSelfAbility_RMR_TrackUpgrade : DiceCardSelfAbilityBase
    {
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.RecoverHP(4);
        }

        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddBuf(new BattleUnitBuf_rmrtrackbuf());
        }
        public override string[] Keywords => new string[] { "Recover_Keyword" };
        /// <summary>BattleUnitBuf_rmrtrackbuf</summary>
        public class BattleUnitBuf_rmrtrackbuf : BattleUnitBuf
        {
            public override void BeforeRollDice(BattleDiceBehavior behavior)
            {
                base.BeforeRollDice(behavior);
                if (!behavior.abilityList.Contains(new DiceCardAbility_recoverHp1atk()))
                {
                    behavior.AddAbility(new DiceCardAbility_recoverHp1atk());
                }
            }

            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                this.Destroy();
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Chargeup</summary>

    public class DiceCardSelfAbility_RMR_Chargeup : DiceCardSelfAbilityBase
    {
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddBuf(new BattleUnitBuf_rmrchargeupbuf());
        }
        public override string[] Keywords => new string[] { "Paralysis_Keyword", "Energy_Keyword" };
        /// <summary>BattleUnitBuf_rmrchargeupbuf</summary>
        public class BattleUnitBuf_rmrchargeupbuf : BattleUnitBuf
        {
            public override void OnRoundStart()
            {
                base.OnRoundStart();
                _owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Paralysis, 1, _owner);
                _owner.cardSlotDetail.RecoverPlayPointByCard(1);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_ChargeupUpgrade</summary>

    public class DiceCardSelfAbility_RMR_ChargeupUpgrade : DiceCardSelfAbilityBase
    {
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.allyCardDetail.ExhaustACard(this.card.card);
            card.card.exhaust = true;
            owner.bufListDetail.AddBuf(new BattleUnitBuf_rmrchargeupbuf());
            owner.allyCardDetail.DrawCards(1);
        }
        public override string[] Keywords => new string[] { "DrawCard_Keyword","Paralysis_Keyword","Energy_Keyword" };
        /// <summary>BattleUnitBuf_rmrchargeupbuf</summary>
        public class BattleUnitBuf_rmrchargeupbuf : BattleUnitBuf
        {

            public override void OnRoundStart()
            {
                base.OnRoundStart();
                _owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Paralysis, 1, _owner);
                _owner.cardSlotDetail.RecoverPlayPointByCard(1);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_BackstreetsDash</summary>

    public class DiceCardSelfAbility_RMR_BackstreetsDash : DiceCardSelfAbilityBase
    {
        public override void OnStartParrying()
        {
            base.OnStartParrying();
            card.target?.currentDiceAction?.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
            {
                breakDmg = -5
            });
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_SkitterAway</summary>

    public class DiceCardSelfAbility_RMR_SkitterAway : DiceCardSelfAbilityBase
    {
        public override void OnApplyCard()
        {
            base.OnApplyCard();
            RMRUtilityExtensions.AddSpeedImmediately(owner, owner.cardOrder, +3);
        }

        public override void OnReleaseCard()
        {
            base.OnReleaseCard();
            RMRUtilityExtensions.AddSpeedImmediately(owner, owner.cardOrder, -3);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_SkitterAwayUpgrade</summary>

    public class DiceCardSelfAbility_RMR_SkitterAwayUpgrade : DiceCardSelfAbilityBase
    {
        public override void OnApplyCard()
        {
            base.OnApplyCard();
            RMRUtilityExtensions.AddSpeedImmediately(owner, owner.cardOrder, +5);
        }

        public override void OnReleaseCard()
        {
            base.OnReleaseCard();
            RMRUtilityExtensions.AddSpeedImmediately(owner, owner.cardOrder, -5);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_EndureUpgrade</summary>

    public class DiceCardSelfAbility_RMR_EndureUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Protection_Keyword", "BreakProtection_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Protection, 1, owner);
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.BreakProtection, 1, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_DriedupUpgrade</summary>

    public class DiceCardSelfAbility_RMR_DriedupUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Strength_Keyword", "Quickness_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Strength, 2, owner);
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Quickness, 2, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_ChopItOff</summary>

    public class DiceCardSelfAbility_RMR_ChopItOff : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Paralysis_Keyword" };
        public override void BeforeRollDice(BattleDiceBehavior behavior)
        {
            base.BeforeRollDice(behavior);
            int stack = behavior.card.target.bufListDetail.GetKewordBufStack(KeywordBuf.Paralysis);
            if (stack > 3)
            {
                stack = 3;
            }
            behavior.ApplyDiceStatBonus(new DiceStatBonus
            {
                power = stack
            });
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_GoinFirst</summary>

    public class DiceCardSelfAbility_RMR_GoinFirst : DiceCardSelfAbilityBase
    {
        public override void OnUseCard()
        {
            base.OnUseCard();
            if (owner.cardSlotDetail.cardQueue.Peek() == card)
            {
                card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                {
                    min = 2
                });
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_StruggleUpgrade</summary>

    public class DiceCardSelfAbility_RMR_StruggleUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Protection_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Protection, 2, owner);
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Protection, 2, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_YouOnlyLiveOnce</summary>

    public class DiceCardSelfAbility_RMR_YouOnlyLiveOnce : DiceCardSelfAbilityBase
    {
        public override void OnStartParrying()
        {
            base.OnStartParrying();
            card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
            {
                dmgRate = 50,
                breakRate = 50
            });
            card.target?.currentDiceAction?.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
            {
                dmgRate = 50,
                breakRate = 50
            });
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_GutHarvesting</summary>

    public class DiceCardSelfAbility_RMR_GutHarvesting : DiceCardSelfAbilityBase
    {

        public override void OnEndBattle()
        {
            base.OnEndBattle();
            if (card.target.IsDead())
            {
                LogueBookModels.AddMoney(4);
            }
            this.card.card.XmlData.Script = "";
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_GutHarvestingUpgrade</summary>

    public class DiceCardSelfAbility_RMR_GutHarvestingUpgrade : DiceCardSelfAbilityBase
    {

        public override void OnEndBattle()
        {
            base.OnEndBattle();
            if (card.target.IsDead())
            {
                switch (card.target.Book.Rarity)
                {
                    case Rarity.Common:
                        LogueBookModels.AddMoney(4);
                        break;
                    case Rarity.Uncommon:
                        LogueBookModels.AddMoney(7);
                        break;
                    case Rarity.Rare:
                        LogueBookModels.AddMoney(10);
                        break;
                    case Rarity.Unique:
                        LogueBookModels.AddMoney(15);
                        break;
                    case Rarity.Special:
                        LogueBookModels.AddMoney(15);
                        break;
                    default:
                        LogueBookModels.AddMoney(4);
                        break;
                }
                
            }
            this.card.card.XmlData.Script = "";
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_break3atkcard</summary>

    public class DiceCardSelfAbility_RMR_break3atkcard : DiceCardSelfAbilityBase
    {
        public override void OnSucceedAttack()
        {
            base.OnSucceedAttack();
            card.target.breakDetail.TakeBreakDamage(3, DamageType.Card_Ability);
        }
    }

    #endregion

    #region Urban Myth

    public class DiceCardSelfAbility_RMR_GatherIntelUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "DrawCard_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            List<BattleDiceCardModel> drawpile = owner.allyCardDetail.GetDeck();
            if (drawpile.Count > 0)
            {
                 owner.allyCardDetail.DrawCardsAllSpecific(drawpile.SortReturn((x, y) => x.GetRarity() - y.GetRarity()).Last().GetID());
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Appetite</summary>

    public class DiceCardSelfAbility_RMR_Appetite : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Bleeding_Keyword", "Recover_Keyword" };
        public override void OnSucceedAttack()
        {
            base.OnSucceedAttack();
            int stack = card.target.bufListDetail.GetKewordBufStack(KeywordBuf.Bleeding);
            if (stack > 15)
            {
                stack = 15;
            }
            owner.RecoverHP(stack);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_QuickAttack</summary>

    public class DiceCardSelfAbility_RMR_QuickAttack : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Quickness_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Quickness, 2, owner);
            if (owner.speedDiceResult[card.slotOrder].value > card.target.speedDiceResult[card.targetSlotOrder].value)
            {
                card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                {
                    power = 2
                });
            }
        }
    }
    

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_bleed2atkcard</summary>
    

    public class DiceCardSelfAbility_RMR_bleed2atkcard : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Bleeding_Keyword" };
        public override void OnSucceedAttack()
        {
            base.OnSucceedAttack();
            card.target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Bleeding, 2, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_PreparedMindLulu</summary>

    public class DiceCardSelfAbility_RMR_PreparedMindLulu : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Burn_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddBuf(new BattleUnitBuf_burnPlus());
        }

        /// <summary>BattleUnitBuf_burnPlus</summary>

        public class BattleUnitBuf_burnPlus : BattleUnitBuf
        {
            public override int OnGiveKeywordBufByCard(BattleUnitBuf cardBuf, int stack, BattleUnitModel target)
            {
                if (cardBuf.bufType == KeywordBuf.Burn)
                {
                    return 1;
                }
                return 0;
            }

            public override void OnRoundEnd()
            {
                Destroy();
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_PreparedMindLuluUpgrade</summary>

    public class DiceCardSelfAbility_RMR_PreparedMindLuluUpgrade : DiceCardSelfAbilityBase
    {
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddBuf(new BattleUnitBuf_rmrpreparedmindbuf());
        }
        public override string[] Keywords => new string[] { "Burn_Keyword","Endurance_Keyword", };

        /// <summary>BattleUnitBuf_rmrpreparedmindbuf</summary>

        public class BattleUnitBuf_rmrpreparedmindbuf : BattleUnitBuf
        {
            public override int OnGiveKeywordBufByCard(BattleUnitBuf cardBuf, int stack, BattleUnitModel target)
            {
                if (cardBuf.bufType == KeywordBuf.Burn)
                {
                    _owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Endurance, 1, _owner);
                    return 1;
                }
                return 0;
            }


            public override void OnRoundEnd()
            {
                Destroy();
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Endurance1start</summary>

    public class DiceCardSelfAbility_RMR_Endurance1start : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Endurance_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Endurance, 1, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Endurance2start</summary>

    public class DiceCardSelfAbility_RMR_Endurance2start : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Endurance_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Endurance, 2, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Multiblock</summary>

    public class DiceCardSelfAbility_RMR_Multiblock : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Protection_Keyword", "BreakProtection_Keyword", "RMR_Shield_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Protection, 3, owner);
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.BreakProtection, 3, owner);
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRShield, 5, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_MultiblockUpgrade</summary>

    public class DiceCardSelfAbility_RMR_MultiblockUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Protection_Keyword", "BreakProtection_Keyword", "RMR_Shield_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Protection, 4, owner);
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.BreakProtection, 4, owner);
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRShield, 10, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_FleetFootsteps</summary>

    public class DiceCardSelfAbility_RMR_FleetFootsteps : DiceCardSelfAbilityBase
    {
        public override void OnApplyCard()
        {
            base.OnApplyCard();
            RMRUtilityExtensions.AddSpeedImmediately(owner, owner.cardOrder, +2);
        }

        public override void OnReleaseCard()
        {
            base.OnReleaseCard();
            RMRUtilityExtensions.AddSpeedImmediately(owner, owner.cardOrder, -2);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Deflect</summary>

    public class DiceCardSelfAbility_RMR_Deflect : DiceCardSelfAbilityBase
    {
        public override void OnWinParryingAtk()
        {
            base.OnWinParryingAtk();
            card?.target?.currentDiceAction?.ApplyDiceStatBonus(DiceMatch.NextDice, new DiceStatBonus
            {
                power = -1
            });
        }
        public override void OnWinParryingDef()
        {
            base.OnWinParryingDef();
            card?.target?.currentDiceAction?.ApplyDiceStatBonus(DiceMatch.NextDice, new DiceStatBonus
            {
                power = -1
            });
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_IngredientHunt</summary>

    public class DiceCardSelfAbility_RMR_IngredientHunt : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Bleeding_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            if (card.target.bufListDetail.GetActivatedBuf(KeywordBuf.Bleeding) != null)
            {
                card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                {
                    power = 1
                });
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Multihit</summary>

    public class DiceCardSelfAbility_RMR_Multihit : DiceCardSelfAbilityBase
    {
        public override void OnUseCard()
        {
            base.OnUseCard();
            card.ForeachQueue(DiceMatch.AllDice, behavior => behavior.AddAbility(new DiceCardAbility_RMR_Multhitdie()));
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_Multhitdie</summary>

    public class DiceCardAbility_RMR_Multhitdie : DiceCardAbilityBase
    {
        bool trigger;
        public override void OnSucceedAttack()
        {
            base.OnSucceedAttack();
            if (!trigger)
            {
                if (RandomUtil.valueForProb <= 0.33)
                {
                    base.ActivateBonusAttackDice();
                    trigger = true;
                }
               

            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_NonstopAssault</summary>

    public class DiceCardSelfAbility_RMR_NonstopAssault : DiceCardSelfAbilityBase
    {
        /// <summary>BattleUnitBuf_rmrnonstopbuf</summary>
        public class BattleUnitBuf_rmrnonstopbuf : BattleUnitBuf
        {
            public BattleDiceCardModel card;

            public BattleUnitBuf_rmrnonstopbuf(BattleDiceCardModel card)
            {
                this.card = card;
            }

            public override void OnRoundEndTheLast()
            {
                _owner.allyCardDetail.DrawCardsAllSpecific(card.GetID(), card);
                Destroy();
            }
        }

        public override string[] Keywords => new string[1] { "DrawCard_Keyword" };

        public override void OnUseCard()
        {
            base.owner.bufListDetail.AddBuf(new BattleUnitBuf_rmrnonstopbuf(card.card));
        }


    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_NonstopAssaultUpgrade</summary>

    public class DiceCardSelfAbility_RMR_NonstopAssaultUpgrade : DiceCardSelfAbilityBase
    {
        /// <summary>BattleUnitBuf_rmrnonstopbuf</summary>
        public class BattleUnitBuf_rmrnonstopbuf : BattleUnitBuf
        {
            public BattleDiceCardModel card;

            public BattleUnitBuf_rmrnonstopbuf(BattleDiceCardModel card)
            {
                this.card = card;
            }

            public override void OnRoundEndTheLast()
            {
                _owner.allyCardDetail.DrawCardsAllSpecific(card.GetID(), card);
                Destroy();
            }
        }

        public override string[] Keywords => new string[1] { "DrawCard_Keyword" };

        public override void OnUseCard()
        {
            base.owner.cardSlotDetail.RecoverPlayPointByCard(1);
            base.owner.bufListDetail.AddBuf(new BattleUnitBuf_rmrnonstopbuf(card.card));
        }


    }

    #endregion

    #region Urban Legend
    public class DiceCardAbility_RMR_LawOrder : DiceCardAbilityBase
    {
        public override void OnWinParrying()
        {
            base.OnWinParrying();
             DiceCardXmlInfo cardItem = ItemXmlDataList.instance.GetCardItem(new LorId(LogLikeMod.ModId, 390001));
            List <BattleDiceBehavior> list = new List<BattleDiceBehavior>();
            int num = 0;
            foreach (DiceBehaviour diceBehaviour in cardItem.DiceBehaviourList)
            {
                BattleDiceBehavior battleDiceBehavior = new BattleDiceBehavior();
                battleDiceBehavior.behaviourInCard = diceBehaviour.Copy();
                battleDiceBehavior.SetIndex(num++);
                list.Add(battleDiceBehavior);
            }
            owner.cardSlotDetail.keepCard.AddBehaviours(cardItem, list); 
      /*      BattleDiceBehavior die = new BattleDiceBehavior
            {
                behaviourInCard = new DiceBehaviour
                {
                    Min = 3,
                    Dice = 8,
                    Detail = BehaviourDetail.Guard,
                    Type = BehaviourType.Def,
                    MotionDetail = MotionDetail.G,
                    MotionDetailDefault = MotionDetail.N,
                    Script = ""
                }
            };
            owner.cardSlotDetail.keepCard.AddBehaviourForOnlyDefense(this.card.card, die); */
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_LawOrderUpgrade</summary>

    public class DiceCardAbility_RMR_LawOrderUpgrade : DiceCardAbilityBase
    {
        public override void OnWinParrying()
        {
            base.OnWinParrying();
              DiceCardXmlInfo cardItem = ItemXmlDataList.instance.GetCardItem(new LorId(LogLikeMod.ModId, 390002));
              List<BattleDiceBehavior> list = new List<BattleDiceBehavior>();
              int num = 0;
              foreach (DiceBehaviour diceBehaviour in cardItem.DiceBehaviourList)
              {
                  BattleDiceBehavior battleDiceBehavior = new BattleDiceBehavior();
                  battleDiceBehavior.behaviourInCard = diceBehaviour.Copy();
                  battleDiceBehavior.SetIndex(num++);
                  list.Add(battleDiceBehavior);
              }
              owner.cardSlotDetail.keepCard.AddBehaviours(cardItem, list); 
        /*    BattleDiceBehavior die = new BattleDiceBehavior
            {
                behaviourInCard = new DiceBehaviour
                {
                    Min = 3,
                    Dice = 8,
                    Detail = BehaviourDetail.Guard,
                    Type = BehaviourType.Def,
                    MotionDetail = MotionDetail.G,
                    MotionDetailDefault = MotionDetail.N,
                    Script = ""
                }
            };
            owner.cardSlotDetail.keepCard.AddBehaviourForOnlyDefense(this.card.card, die);
            owner.cardSlotDetail.keepCard.AddBehaviourForOnlyDefense(this.card.card, die); */
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Diversion</summary>

    public class DiceCardSelfAbility_RMR_Diversion : DiceCardSelfAbilityBase
    {
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            if (this.card.targetSlotOrder < this.card.target.cardSlotDetail.cardAry.Count)
            {
                BattlePlayingCardDataInUnitModel fish = this.card.target.cardSlotDetail.cardAry[this.card.targetSlotOrder];
                if (fish != null)
                {
                    if (fish.target != this.owner && fish.CanIRedirectPls(this.owner))
                    {
                        fish.target = this.owner;
                        fish.targetSlotOrder = this.card.slotOrder;
                    }

                }
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_SharpSwipe</summary>

    public class DiceCardSelfAbility_RMR_SharpSwipe : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Bleeding_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddBuf(new agagagagbleeeeed());
        }

        /// <summary>agagagagbleeeeed</summary>

        public class agagagagbleeeeed : BattleUnitBuf
        {
            public override void BeforeRollDice(BattleDiceBehavior behavior)
            {
                base.BeforeRollDice(behavior);
                behavior.AddAbility(new DiceCardAbility_bleeding1atk());
            }
            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                this.Destroy();
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Standoff</summary>

    public class DiceCardSelfAbility_RMR_Standoff : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Shield_Keyword", "RMR_StaggerShield_Keyword" };
        public override void OnStartParrying()
        {
            base.OnStartParrying();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRShield, 5, owner);
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRStaggerShield, 5, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_StandoffUpgrade</summary>

    public class DiceCardSelfAbility_RMR_StandoffUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Shield_Keyword", "RMR_StaggerShield_Keyword" };
        public override void OnStartParrying()
        {
            base.OnStartParrying();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRShield, 8, owner);
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRStaggerShield, 8, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Avert</summary>

    public class DiceCardSelfAbility_RMR_Avert : DiceCardSelfAbilityBase
    {
        public override void OnStartParrying()
        {
            base.OnStartParrying();
            if (card?.target?.currentDiceAction?.earlyTarget?.faction == owner.faction && card?.target?.currentDiceAction?.earlyTarget != owner)
            {
                card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                {
                    power = 2
                });
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_StartinLightly</summary>

    public class DiceCardSelfAbility_RMR_StartinLightly : DiceCardSelfAbilityBase
    {
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.HitPowerUp, 1, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_GuardianUpgrade</summary>

    public class DiceCardSelfAbility_RMR_GuardianUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Endurance_Keyword", "Quickness_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            foreach (BattleUnitModel amog in BattleObjectManager.instance.GetAliveList(owner.faction))
            {
                amog.bufListDetail.AddKeywordBufByCard(KeywordBuf.Endurance, 1, owner);
                amog.bufListDetail.AddKeywordBufByCard(KeywordBuf.Quickness, 1, owner);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_GambleUpgrade</summary>

    public class DiceCardSelfAbility_RMR_GambleUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_CriticalStrike_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            var x = owner.allyCardDetail.DiscardACardLowest();
            if (x != null && x.GetCost() > 0)
            {
                owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.CritChance, x.GetCost()*2, owner);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_BackAttack</summary>

    public class DiceCardSelfAbility_RMR_BackAttack : DiceCardSelfAbilityBase
    {
        public override void OnUseCard()
        {
            base.OnUseCard();
            foreach (var unit in BattleObjectManager.instance.GetAliveList())
            {
                foreach (var c in unit.cardSlotDetail.cardAry)
                {
                    if (c != null && c.target == card.target && c.card.GetCost() >= 3 && unit.faction == owner.faction)
                    {
                        card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus { power = 1 });
                    }
                }
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_SliceUpgrade</summary>

    public class DiceCardSelfAbility_RMR_SliceUpgrade : DiceCardSelfAbilityBase
    {
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.allyCardDetail.DiscardACardLowest();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.SlashPowerUp, 1, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_StayClamUpgrade</summary>

    public class DiceCardSelfAbility_RMR_StayClamUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "DrawCard_Keyword", "RMR_StaggerShield_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.allyCardDetail.DrawCards(1);
        }

        public override void OnDiscard(BattleUnitModel unit, BattleDiceCardModel self)
        {
            base.OnDiscard(unit, self);
            unit.allyCardDetail.DrawCards(1);
            unit.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRStaggerShield, 3, unit);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_FishOnslaughtUpgrade</summary>

    public class DiceCardSelfAbility_RMR_FishOnslaughtUpgrade : DiceCardSelfAbilityBase
    {
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.allyCardDetail.DiscardACardLowest();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.DmgUp, 1, owner);
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_recover5breakpw</summary>

    public class DiceCardAbility_RMR_recover5breakpw : DiceCardAbilityBase
    {
        public override void OnWinParrying()
        {
            base.OnWinParrying();
            owner.breakDetail.RecoverBreak(5);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_SearingBlow</summary>

    public class DiceCardSelfAbility_RMR_SearingBlow : DiceCardSelfAbilityBase
    {
        // empty for description
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Outburst</summary>

    public class DiceCardSelfAbility_RMR_Outburst : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Energy_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.allyCardDetail.DiscardACardLowest();
            owner.cardSlotDetail.RecoverPlayPointByCard(3);
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_allydraw1pw</summary>

    public class DiceCardAbility_RMR_allydraw1pw : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "DrawCard_Keyword" };
        public override void OnWinParrying()
        {
            base.OnWinParrying();
            List<BattleUnitModel> gooners = BattleObjectManager.instance.GetAliveList(owner.faction).FindAll((BattleUnitModel x) => x != owner);
            if (gooners.Count > 0)
            {
                RandomUtil.SelectOne<BattleUnitModel>(gooners).allyCardDetail.DrawCards(1);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_break2atk</summary>

    public class DiceCardSelfAbility_RMR_break2atk : DiceCardSelfAbilityBase
    {
        public override void OnSucceedAttack()
        {
            base.OnSucceedAttack();
            card.target.breakDetail.TakeBreakDamage(2, DamageType.Card_Ability, owner);
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_paralysisbind1atk</summary>

    public class DiceCardAbility_RMR_paralysisbind1atk : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "Paralysis_Keyword", "Binding_Keyword" };
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Paralysis, 1, owner);
            target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Binding, 1, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_CutIn</summary>

    public class DiceCardSelfAbility_RMR_CutIn : DiceCardSelfAbilityBase
    {
        public override void OnUseCard()
        {
            base.OnUseCard();
            if (card.target.cardSlotDetail.cardQueue.Count > 0)
            {
                card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                {
                    min = StageController.Instance._allCardList.Count(x => x.owner == card.target)
                });
            }
        }
    }
    
    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_MeatJamUpgrade</summary>
    
    public class DiceCardSelfAbility_RMR_MeatJamUpgrade : DiceCardSelfAbilityBase
    {
        public override void OnUseInstance(BattleUnitModel unit, BattleDiceCardModel self, BattleUnitModel targetUnit)
        {
            base.OnUseInstance(unit, self, targetUnit);
            unit.RecoverHP(30);
            BookModel bookItem = unit.UnitData.unitData.bookItem;
            if (!bookItem.GetDeckAll_nocopy()[bookItem.GetCurrentDeckIndex()].MoveCardToInventory(self.GetID()))
              return;
            LogueBookModels.DeleteCard(self.GetID());
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_5shield</summary>

    public class DiceCardSelfAbility_RMR_5shield : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Shield_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRShield, 5, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_BladeWhirl</summary>

    public class DiceCardSelfAbility_RMR_BladeWhirl : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Endurance_Keyword", "DrawCard_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            if (owner.bufListDetail.GetActivatedBuf(KeywordBuf.Endurance) != null)
            {
                owner.allyCardDetail.DrawCards(1);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_BladeWhirlUpgrade</summary>

    public class DiceCardSelfAbility_RMR_BladeWhirlUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Endurance_Keyword", "DrawCard_Keyword", "Energy_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            if (owner.bufListDetail.GetActivatedBuf(KeywordBuf.Endurance) != null)
            {
                owner.allyCardDetail.DrawCards(1);
                owner.cardSlotDetail.RecoverPlayPointByCard(1);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_HandlingWorkUpgrade</summary>

    public class DiceCardSelfAbility_RMR_HandlingWorkUpgrade : DiceCardSelfAbilityBase
    {
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddBuf(new googoogaagaaresistances());
        }

        /// <summary>googoogaagaaresistances</summary>

        public class googoogaagaaresistances : BattleUnitBuf
        {
            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                this.Destroy();
            }
            public override AtkResist GetResistBP(AtkResist origin, BehaviourDetail detail)
            {
                if (origin == AtkResist.Vulnerable || origin == AtkResist.Weak)
                {
                    return AtkResist.Normal;
                }
                return base.GetResistBP(origin, detail);
            }
            public override AtkResist GetResistHP(AtkResist origin, BehaviourDetail detail)
            {
                if (origin == AtkResist.Vulnerable || origin == AtkResist.Weak)
                {
                    return AtkResist.Normal;
                }
                return base.GetResistHP(origin, detail);
            }
        }
    }

    #endregion

    #region Urban Plague
    public class DiceCardSelfAbility_RMR_TasteChain : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Bleeding_Keyword", "Energy_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            foreach (BattleUnitModel battleUnitModel in BattleObjectManager.instance.GetAliveList(base.owner.faction))
            {
                if (battleUnitModel != base.owner)
                {
                    battleUnitModel.bufListDetail.AddBuf(new BattleUnitBuf_luxunitybuf());
                }
            }
        }

        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.cardSlotDetail.RecoverPlayPointByCard(1);
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Bleeding, 1, owner);
        }
        /// <summary>BattleUnitBuf_luxunitybuf</summary>
        public class BattleUnitBuf_luxunitybuf : BattleUnitBuf
        {
            public override void OnUseCard(BattlePlayingCardDataInUnitModel card)
            {
                base.OnUseCard(card);
                if (card.cardAbility != null)
                {
                    if (card.cardAbility.IsUniteCard)
                    {
                        _owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Bleeding, 1, _owner);
                    }
                }
            }
            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                this.Destroy();
            }
        }

        /* skeleton for unity effects
         
        public override bool IsUniteCard => true;
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            foreach (BattleUnitModel battleUnitModel in BattleObjectManager.instance.GetAliveList(base.owner.faction))
            {
                if (battleUnitModel != base.owner && !battleUnitModel.bufListDetail.HasBuf<BattleUnitBuf_luxunitybuf>())
                {
                    battleUnitModel.bufListDetail.AddBuf(new BattleUnitBuf_luxunitybuf());
                }
            }
        }
        /// <summary>BattleUnitBuf_luxunitybuf</summary>
        public class BattleUnitBuf_luxunitybuf : BattleUnitBuf
        {
            public override void OnUseCard(BattlePlayingCardDataInUnitModel card)
            {
                base.OnUseCard(card);
                if (card.cardAbility != null)
                {
                    if (card.cardAbility.IsUniteCard)
                    {
                        
                    }
                }
            }
            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                this.Destroy();
            }
        } */
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_LowNight</summary>

    public class DiceCardSelfAbility_RMR_LowNight : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Strength_Keyword" };
        public override void OnDiscard(BattleUnitModel unit, BattleDiceCardModel self)
        {
            base.OnDiscard(unit, self);
            if (unit.hp > unit.MaxHp/2)
            {
                unit.TakeDamage(4, DamageType.Card_Ability);
                unit.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Strength, 1, owner);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_LowNightUpgrade</summary>

    public class DiceCardSelfAbility_RMR_LowNightUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Strength_Keyword", "RMR_Shield_Keyword" };
        public override void OnDiscard(BattleUnitModel unit, BattleDiceCardModel self)
        {
            base.OnDiscard(unit, self);
            if (unit.hp > unit.MaxHp / 2)
            {
                unit.TakeDamage(4, DamageType.Card_Ability);
                unit.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Strength, 1, unit);
            }
            else if (unit.hp < unit.MaxHp / 2)
            {
                unit.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRShield, 4, unit);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_RulesBackstreets</summary>

    public class DiceCardSelfAbility_RMR_RulesBackstreets : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Bleeding_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            if (card.target != null && card.target.bufListDetail.GetKewordBufStack(KeywordBuf.Bleeding) >= 4)
            {
                card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                {
                    min = 2
                });
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_bleed2draw1atk</summary>

    public class DiceCardAbility_RMR_bleed2draw1atk : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "Bleeding_Keyword", "DrawCard_Keyword" };
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Bleeding, 2, owner);
            owner.allyCardDetail.DrawCards(1);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_HandleRequest</summary>

    public class DiceCardSelfAbility_RMR_HandleRequest : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Burn_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddBuf(new BattleUnitBuf_burnPlus());
        }

        /// <summary>BattleUnitBuf_burnPlus</summary>

        public class BattleUnitBuf_burnPlus : BattleUnitBuf
        {
            public override int OnGiveKeywordBufByCard(BattleUnitBuf cardBuf, int stack, BattleUnitModel target)
            {
                if (cardBuf.bufType == KeywordBuf.Burn)
                {
                    return 1;
                }
                return 0;
            }

            public override void OnRoundEnd()
            {
                Destroy();
            }
        }

    }

    /// <summary>Dice ability: DiceCardAbility_RMR_nodeflectdamage</summary>

    public class DiceCardAbility_RMR_nodeflectdamage : DiceCardAbilityBase
    {
        public override void BeforeRollDice()
        {
            base.BeforeRollDice();
            if (behavior.TargetDice != null)
            {
                behavior.TargetDice.ApplyDiceStatBonus(new DiceStatBonus
                {
                    guardBreakAdder = -9999
                });
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_boostdiceminvalue2pl</summary>

    public class DiceCardAbility_RMR_boostdiceminvalue2pl : DiceCardAbilityBase
    {
        public override void OnLoseParrying()
        {
            base.OnLoseParrying();
            card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
            {
                min = 2
            });
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_PrescriptOrder</summary>

    public class DiceCardSelfAbility_RMR_PrescriptOrder : DiceCardSelfAbility_RMR_Zeal
    {
        public override string[] Keywords => new string[] { "DrawCard_Keyword" };

        public override void OnUseCard()
        {
            base.OnUseCard();
            if (owner.allyCardDetail.IsHighlander())
            {
                owner.allyCardDetail.DrawCards(1);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_PrescriptOrderUpgrade</summary>

    public class DiceCardSelfAbility_RMR_PrescriptOrderUpgrade : DiceCardSelfAbility_RMR_Zeal
    {
        public override string[] Keywords => new string[] {  "DrawCard_Keyword", "RMR_StaggerShield_Keyword" };

        public override void OnUseCard()
        {
            base.OnUseCard();
            if (owner.allyCardDetail.IsHighlander())
            {
                owner.allyCardDetail.DrawCards(1);
            }
            else
            {
                owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRStaggerShield, owner.allyCardDetail.GetHand().Count, owner);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_TargetSpotted</summary>

    public class DiceCardSelfAbility_RMR_TargetSpotted : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_CriticalStrike_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.CritChance, 5, owner);
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_binding3crit</summary>

    public class DiceCardAbility_RMR_binding3crit : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "Binding_Keyword" };
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            if (owner.isCrit())
            {
                target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Binding, 3, owner);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_UnavoidableGazeUpgrade</summary>

    public class DiceCardSelfAbility_RMR_UnavoidableGazeUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Paralysis_Keyword" };
        public override void BeforeRollDice(BattleDiceBehavior behavior)
        {
            base.BeforeRollDice(behavior);
            int stack = behavior.card.target.bufListDetail.GetKewordBufStack(KeywordBuf.Paralysis);
            if (stack > 5)
            {
                stack = 5;
            }
            behavior.ApplyDiceStatBonus(new DiceStatBonus
            {
                power = stack
            });
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_YoureSoft</summary>

    public class DiceCardSelfAbility_RMR_YoureSoft : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Bleeding_Keyword", "RMR_BleedProtection_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Bleeding, 2, owner);
        }

        public override bool IsUniteCard => true;
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            foreach (BattleUnitModel battleUnitModel in BattleObjectManager.instance.GetAliveList(base.owner.faction))
            {
                if (battleUnitModel != base.owner)
                {
                    battleUnitModel.bufListDetail.AddBuf(new BattleUnitBuf_luxunitybuf());
                }
            }
        }
        /// <summary>BattleUnitBuf_luxunitybuf</summary>
        public class BattleUnitBuf_luxunitybuf : BattleUnitBuf
        {
            public override void OnUseCard(BattlePlayingCardDataInUnitModel card)
            {
                base.OnUseCard(card);
                if (card.cardAbility != null)
                {
                    if (card.cardAbility.IsUniteCard)
                    {
                        _owner.bufListDetail.AddKeywordBufByCard(RoguelikeBufs.BleedProtection, 1, _owner);
                    }
                }
            }
            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                this.Destroy();
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Relay</summary>

    public class DiceCardSelfAbility_RMR_Relay : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "DrawCard_Keyword" };
        public override void OnDiscard(BattleUnitModel unit, BattleDiceCardModel self)
        {
            base.OnDiscard(unit, self);
            RandomUtil.SelectOne<BattleUnitModel>(BattleObjectManager.instance.GetAliveList(unit.faction).FindAll((BattleUnitModel x) => x != unit)).allyCardDetail.DrawCards(1);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_RelayUpgrade</summary>

    public class DiceCardSelfAbility_RMR_RelayUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "DrawCard_Keyword" };
        public override void OnDiscard(BattleUnitModel unit, BattleDiceCardModel self)
        {
            base.OnDiscard(unit, self);
            RandomUtil.SelectOne<BattleUnitModel>(BattleObjectManager.instance.GetAliveList(unit.faction).FindAll((BattleUnitModel x) => x != unit)).allyCardDetail.DrawCards(1);
        }

        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.allyCardDetail.DrawCards(1);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_DarkCloudUpgrade</summary>

    public class DiceCardSelfAbility_RMR_DarkCloudUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Bleeding_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddBuf(new guggugubleeeeedd());
        }

        /// <summary>guggugubleeeeedd</summary>

        public class guggugubleeeeedd : BattleUnitBuf
        {
            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                this.Destroy();
            }
            public override void BeforeRollDice(BattleDiceBehavior behavior)
            {
                base.BeforeRollDice(behavior);
                if (!behavior.abilityList.Contains(new DiceCardAbility_RMR_bleeding1atk2lessbreakdmg()))
                {
                    behavior.AddAbility(new DiceCardAbility_RMR_bleeding1atk2lessbreakdmg());
                }
            }
        }

    }

    /// <summary>Dice ability: DiceCardAbility_RMR_bleeding1atk2lessbreakdmg</summary>

    public class DiceCardAbility_RMR_bleeding1atk2lessbreakdmg : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "Bleeding_Keyword" };
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Bleeding, 1, owner);
        }
        public override void BeforeGiveDamage()
        {
            base.BeforeGiveDamage();
            behavior.ApplyDiceStatBonus(new DiceStatBonus
            {
                breakDmg = -2
            });
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_UpbeatPerformance</summary>

    public class DiceCardSelfAbility_RMR_UpbeatPerformance : DiceCardSelfAbilityBase
    {
        public override void OnWinParryingAtk()
        {
            base.OnWinParryingAtk();
            Effect();
        }

        public override void OnWinParryingDef()
        {
            base.OnWinParryingDef();
            Effect();
        }

        private void Effect()
        {
            foreach (BattleUnitModel amog in BattleObjectManager.instance.GetAliveList(owner.faction))
            {
                int count = amog.emotionDetail.CreateEmotionCoin(EmotionCoinType.Positive);
                SingletonBehavior<BattleManagerUI>.Instance.ui_battleEmotionCoinUI.OnAcquireCoin(amog, EmotionCoinType.Positive, count);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_ReturnFireUpgrade</summary>

    public class DiceCardSelfAbility_RMR_ReturnFireUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_CriticalStrike_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.CritChance, 8, owner);
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_1weakcrit</summary>

    public class DiceCardAbility_RMR_1weakcrit : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "Weak_Keyword" };
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            if (owner.isCrit())
            {
                target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Weak, 1, owner);
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_2weakcrit</summary>

    public class DiceCardAbility_RMR_2weakcrit : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "Weak_Keyword" };
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            if (owner.isCrit())
            {
                target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Weak, 2, owner);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_FrontalDodge</summary>

    public class DiceCardSelfAbility_RMR_FrontalDodge : DiceCardSelfAbilityBase
    {
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.breakDetail.RecoverBreak(8);
        }
        public override bool IsUniteCard => true;
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_FlankAttack</summary>

    public class DiceCardSelfAbility_RMR_FlankAttack : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Bleeding_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Bleeding, 1, owner);
        }
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            foreach (BattleUnitModel battleUnitModel in BattleObjectManager.instance.GetAliveList(base.owner.faction))
            {
                if (!battleUnitModel.bufListDetail.HasBuf<BattleUnitBuf_luxunitybuf>())
                {
                    battleUnitModel.bufListDetail.AddBuf(new BattleUnitBuf_luxunitybuf());
                }
            }
        }
        /// <summary>BattleUnitBuf_luxunitybuf</summary>
        public class BattleUnitBuf_luxunitybuf : BattleUnitBuf
        {
            public override void OnUseCard(BattlePlayingCardDataInUnitModel card)
            {
                base.OnUseCard(card);
                if (card.cardAbility != null)
                {
                    if (card.cardAbility.IsUniteCard)
                    {
                        card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                        {
                            power = 1
                        });
                    }
                }
            }
            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                this.Destroy();
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_FlankAttackUpgrade</summary>

    public class DiceCardSelfAbility_RMR_FlankAttackUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Bleeding_Keyword" };
        public override bool IsUniteCard => true;
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Bleeding, 1, owner);
        }
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            foreach (BattleUnitModel battleUnitModel in BattleObjectManager.instance.GetAliveList(base.owner.faction))
            {
                if (!battleUnitModel.bufListDetail.HasBuf<DiceCardSelfAbility_RMR_FlankAttack.BattleUnitBuf_luxunitybuf>())
                {
                    battleUnitModel.bufListDetail.AddBuf(new DiceCardSelfAbility_RMR_FlankAttack.BattleUnitBuf_luxunitybuf());
                }
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_RedNotes</summary>

    public class DiceCardSelfAbility_RMR_RedNotes : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Strength_Keyword", "Endurance_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            List<BattleUnitModel> goobers = BattleObjectManager.instance.GetAliveList(owner.faction).FindAll((BattleUnitModel x) => x.bufListDetail.GetActivatedBuf(KeywordBuf.Strength) == null);
            if (goobers.Count > 0)
            {
                BattleUnitModel goober = RandomUtil.SelectOne<BattleUnitModel>(goobers);
                goober.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Strength, 1, owner);
                goober.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Endurance, 1, owner);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_RedNotesUpgrade</summary>

    public class DiceCardSelfAbility_RMR_RedNotesUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Strength_Keyword", "Endurance_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            for (int i = 0; i < 2; i++)
            {
                List<BattleUnitModel> goobers = BattleObjectManager.instance.GetAliveList(owner.faction).FindAll((BattleUnitModel x) => x.bufListDetail.GetActivatedBuf(KeywordBuf.Strength) == null);
                if (goobers.Count > 0)
                {
                    BattleUnitModel goober = RandomUtil.SelectOne<BattleUnitModel>(goobers);
                    goober.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Strength, 1, owner);
                    goober.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Endurance, 1, owner);
                }
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_WillBeTasty</summary>

    public class DiceCardSelfAbility_RMR_WillBeTasty : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Recover_Keyword"};
        public override void OnUseCard()
        {
            base.OnUseCard();
            if (card.target.IsBreakLifeZero())
            {
                owner.RecoverHP(4);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_IHATECQC</summary>

    public class DiceCardSelfAbility_RMR_IHATECQC : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Protection_Keyword", "BreakProtection_Keyword", "RMR_CriticalStrike_Keyword" };
        public override void OnLoseParrying()
        {
            base.OnLoseParrying();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Protection, 1, owner);
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.BreakProtection, 1, owner);
            owner.bufListDetail.AddKeywordBufByCard(RoguelikeBufs.CritChance, 3, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_IHATECQCUpgrade</summary>

    public class DiceCardSelfAbility_RMR_IHATECQCUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Protection_Keyword", "BreakProtection_Keyword", "RMR_CriticalStrike_Keyword" };
        public override void OnLoseParrying()
        {
            base.OnLoseParrying();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Protection, 1, owner);
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.BreakProtection, 1, owner);
            owner.bufListDetail.AddKeywordBufByCard(RoguelikeBufs.CritChance, 5, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_TakeTheShot</summary>

    public class DiceCardSelfAbility_RMR_TakeTheShot : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "DrawCard_Keyword", "Energy_Keyword" };
        public override void OnSucceedAttack()
        {
            base.OnSucceedAttack();
            if (owner.isCrit())
            {
                owner.allyCardDetail.DrawCards(1);
                owner.cardSlotDetail.RecoverPlayPointByCard(1);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_TakeTheShotUpgrade</summary>

    public class DiceCardSelfAbility_RMR_TakeTheShotUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "DrawCard_Keyword", "Energy_Keyword", "RMR_CriticalStrike_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.CritChance, 5, owner);
        }
        public override void OnSucceedAttack()
        {
            base.OnSucceedAttack();
            if (owner.isCrit())
            {
                owner.allyCardDetail.DrawCards(1);
                owner.cardSlotDetail.RecoverPlayPointByCard(1);
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_5breakdmgcrit</summary>

    public class DiceCardAbility_RMR_5breakdmgcrit : DiceCardAbilityBase
    {
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            if (owner.isCrit())
            {
                target.breakDetail.TakeBreakDamage(5, DamageType.Card_Ability, owner);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_OpportunitySpotted</summary>

    public class DiceCardSelfAbility_RMR_OpportunitySpotted : DiceCardSelfAbilityBase
    {
        public override void OnUseCard()
        {
            base.OnUseCard();
            int count = 0;
            foreach (BattleUnitBuf buf in card.target.bufListDetail.GetActivatedBufList())
            {
                if (buf.positiveType == BufPositiveType.Negative)
                {
                    count++;
                }
            }
            if (count > 3)
            {
                count = 3;
            }
            card.ApplyDiceStatBonus(DiceMatch.AllAttackDice, new DiceStatBonus
            {
                max = count
            });
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_YoureHindranceUpgrade</summary>

    public class DiceCardSelfAbility_RMR_YoureHindranceUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Disarm_Keyword" };
        public override void OnSucceedAttack()
        {
            base.OnSucceedAttack();
            card.target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Disarm, 1, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_CleanUp</summary>

    public class DiceCardSelfAbility_RMR_CleanUp : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Energy_Keyword", "DrawCard_Keyword", "Bleeding_Keyword" };

        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.cardSlotDetail.RecoverPlayPointByCard(2);
            if (card.target.bufListDetail.GetKewordBufStack(KeywordBuf.Bleeding) >= 4)
            {
                owner.allyCardDetail.DrawCards(1);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_CumulusWall</summary>

    public class DiceCardSelfAbility_RMR_CumulusWall : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Shield_Keyword", "RMR_StaggerShield_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRShield, 5, owner);
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRStaggerShield, 5, owner);
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_CumulusWallDie</summary>

    public class DiceCardAbility_RMR_CumulusWallDie : DiceCardAbilityBase
    {
        public override void OnWinParrying()
        {
            base.OnWinParrying();
            if (behavior.TargetDice?.Type == BehaviourType.Atk)
            {
                behavior.TargetDice.isBonusAttack = true;
                base.ActivateBonusAttackDice();
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_CumulusWallUpgrade</summary>

    public class DiceCardSelfAbility_RMR_CumulusWallUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Shield_Keyword", "RMR_StaggerShield_Keyword", "Bleeding_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRShield, 5, owner);
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRStaggerShield, 5, owner);
        }

        public override void OnUseCard()
        {
            base.OnUseCard();
            if (card.target.bufListDetail.GetKewordBufStack(KeywordBuf.Bleeding) >= 4)
            {
                owner.bufListDetail.AddKeywordBufByCard(RoguelikeBufs.RMRShield, 5, owner);
                owner.bufListDetail.AddKeywordBufByCard(RoguelikeBufs.RMRStaggerShield, 5, owner);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_ShrineToMusic</summary>

    public class DiceCardSelfAbility_RMR_ShrineToMusic : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Weak_Keyword", "Disarm_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            List<BattleUnitModel> goobers = BattleObjectManager.instance.GetAliveList_opponent(owner.faction).FindAll((BattleUnitModel x) => x.bufListDetail.GetActivatedBuf(KeywordBuf.Weak) == null);
            if (goobers.Count > 0)
            {
                BattleUnitModel goober = RandomUtil.SelectOne<BattleUnitModel>(goobers);
                goober.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Weak, 1, owner);
                goober.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Disarm, 1, owner);
            }
        }
    }
    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_ShrineToMusicUpgrade</summary>
    public class DiceCardSelfAbility_RMR_ShrineToMusicUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Weak_Keyword", "Disarm_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            for (int i = 0; i < 2; i++)
            {
                List<BattleUnitModel> goobers = BattleObjectManager.instance.GetAliveList_opponent(owner.faction).FindAll((BattleUnitModel x) => x.bufListDetail.GetActivatedBuf(KeywordBuf.Weak) == null);
                if (goobers.Count > 0)
                {
                    BattleUnitModel goober = RandomUtil.SelectOne<BattleUnitModel>(goobers);
                    goober.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Weak, 1, owner);
                    goober.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Disarm, 1, owner);
                }
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Headshot</summary>

    public class DiceCardSelfAbility_RMR_Headshot : DiceCardSelfAbilityBase
    {
        public override void OnSucceedAttack()
        {
            base.OnSucceedAttack();
            if (owner.isCrit())
            {
                card.target?.currentDiceAction?.DestroyDice(DiceMatch.NextDice, DiceUITiming.Start);
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_dmgupquickness1ally2atk</summary>

    public class DiceCardAbility_RMR_dmgupquickness1ally2atk : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "Quickness_Keyword" };
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            foreach (BattleUnitModel item in BattleObjectManager.instance.GetAliveList_random(base.owner.faction, 2))
            {
                item.bufListDetail.AddKeywordBufByCard(KeywordBuf.DmgUp, 1, base.owner);
                item.bufListDetail.AddKeywordBufByCard(KeywordBuf.Quickness, 1, base.owner);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_SpearedSweep</summary>

    public class DiceCardSelfAbility_RMR_SpearedSweep : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Disarm_Keyword" };
        public override void OnStartParrying()
        {
            base.OnStartParrying();
            card.target.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Disarm, 2, owner);
        }

        public override void OnEndBattle()
        {
            base.OnEndBattle();
            card.target.bufListDetail.RemoveBufAll(KeywordBuf.Disarm);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_SpearedSweepUpgrade</summary>

    public class DiceCardSelfAbility_RMR_SpearedSweepUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Disarm_Keyword" };
        public override void OnSucceedAttack()
        {
            base.OnSucceedAttack();
            card.target.TakeDamage(1, DamageType.Card_Ability, owner);
        }
        public override void OnStartParrying()
        {
            base.OnStartParrying();
            card.target.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Disarm, 2, owner);
        }

        public override void OnEndBattle()
        {
            base.OnEndBattle();
            card.target.bufListDetail.RemoveBufAll(KeywordBuf.Disarm);
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_SpearSweepDie</summary>

    public class DiceCardAbility_RMR_SpearSweepDie : DiceCardAbilityBase
    {
        public override void OnWinParrying()
        {
            base.OnWinParrying();
            BattleDiceBehavior die = new BattleDiceBehavior
            {
                behaviourInCard = new DiceBehaviour
                {
                    Min = 3,
                    Dice = 5,
                    Detail = BehaviourDetail.Penetrate,
                    Type = BehaviourType.Atk,
                    MotionDetail = MotionDetail.Z,
                    MotionDetailDefault = MotionDetail.N,
                    Script = ""
                }
            };
            behavior.card.AddDice(die);
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_SpearSweepDieUpgrade</summary>

    public class DiceCardAbility_RMR_SpearSweepDieUpgrade : DiceCardAbilityBase
    {
        public override void OnWinParrying()
        {
            base.OnWinParrying();
            BattleDiceBehavior die = new BattleDiceBehavior
            {
                behaviourInCard = new DiceBehaviour
                {
                    Min = 4,
                    Dice = 5,
                    Detail = BehaviourDetail.Penetrate,
                    Type = BehaviourType.Atk,
                    MotionDetail = MotionDetail.Z,
                    MotionDetailDefault = MotionDetail.N,
                    Script = ""
                }
            };
            behavior.card.AddDice(die);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_FullStopToLife</summary>

    public class DiceCardSelfAbility_RMR_FullStopToLife : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_CriticalStrike_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            List<BattleUnitModel> goobers = BattleObjectManager.instance.GetAliveList(owner.faction).FindAll((BattleUnitModel x) => x != owner);
            if (goobers.Count > 0)
            {
                RandomUtil.SelectOne<BattleUnitModel>(goobers).bufListDetail.AddKeywordBufByCard(RoguelikeBufs.CritChance, 4, owner);
            }
            owner.bufListDetail.AddKeywordBufByCard(RoguelikeBufs.CritChance, 4, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_FullStopToLifeUpgrade1</summary>

    public class DiceCardSelfAbility_RMR_FullStopToLifeUpgrade1 : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_CriticalStrike_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            List<BattleUnitModel> goobers = BattleObjectManager.instance.GetAliveList(owner.faction).FindAll((BattleUnitModel x) => x != owner);
            if (goobers.Count > 0)
            {
                RandomUtil.SelectOne<BattleUnitModel>(goobers).bufListDetail.AddKeywordBufByCard(RoguelikeBufs.CritChance, 8, owner);
            }
            owner.bufListDetail.AddKeywordBufByCard(RoguelikeBufs.CritChance, 8, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_FullStopToLifeUpgrade2</summary>

    public class DiceCardSelfAbility_RMR_FullStopToLifeUpgrade2 : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_CriticalStrike_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.CritChance, 5, owner);
            owner.bufListDetail.AddKeywordBufByCard(RoguelikeBufs.CritChance, 8, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_ElectricShock</summary>

    public class DiceCardSelfAbility_RMR_ElectricShock : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Paralysis_Keyword" };
        public override void OnEndBattle()
        {
            base.OnEndBattle();
            BattleUnitBuf para = card.target.bufListDetail.GetActivatedBuf(KeywordBuf.Paralysis);
            if (para != null)
            {
                card.target.breakDetail.TakeBreakDamage(para.stack * 2, DamageType.Card_Ability, owner);
                para.Destroy();
                card.target.bufListDetail.RemoveBuf(para);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_ElectricShockUpgrade</summary>

    public class DiceCardSelfAbility_RMR_ElectricShockUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Paralysis_Keyword" };
        public override void OnEndBattle()
        {
            base.OnEndBattle();
            BattleUnitBuf para = card.target.bufListDetail.GetActivatedBuf(KeywordBuf.Paralysis);
            if (para != null)
            {
                owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.WarpCharge, para.stack * 2, owner);
                para.Destroy();
                card.target.bufListDetail.RemoveBuf(para);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_HeresMyChance</summary>

    public class DiceCardSelfAbility_RMR_HeresMyChance : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Bleeding_Keyword" };
        public override bool IsUniteCard => true;
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Bleeding, 3, owner);
            card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
            {
                power = 1
            });
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Whet</summary>

    public class DiceCardSelfAbility_RMR_Whet : DiceCardSelfAbilityBase
    {
        public override void OnEndBattle()
        {
            base.OnEndBattle();
            owner.bufListDetail.AddBuf(new googoogaagaapower());
        }

        /// <summary>googoogaagaapower</summary>

        public class googoogaagaapower : BattleUnitBuf
        {
            public override void OnUseCard(BattlePlayingCardDataInUnitModel card)
            {
                base.OnUseCard(card);
                card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                {
                    power = 1
                });
                this.Destroy();
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_Detonate</summary>

    public class DiceCardAbility_RMR_Detonate : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "Burn_Keyword" };
        public override void BeforeGiveDamage()
        {
            base.BeforeGiveDamage();
            behavior.ApplyDiceStatBonus(new DiceStatBonus
            {
                dmgRate = -9999
            });
        }
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, behavior.DiceResultValue, owner);
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_SkyCut</summary>

    public class DiceCardAbility_RMR_SkyCut : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "Bleeding_Keyword" };
        public override void BeforeGiveDamage()
        {
            base.BeforeGiveDamage();
            behavior.ApplyDiceStatBonus(new DiceStatBonus
            {
                breakRate = -9999
            });
        }
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Bleeding, Mathf.Max(behavior.DiceResultValue-behavior.GetDiceMin(), 1), owner);
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_add5powerpl</summary>

    public class DiceCardAbility_RMR_add5powerpl : DiceCardAbilityBase
    {
        public override void OnLoseParrying()
        {
            base.OnLoseParrying();
            card.ApplyDiceStatBonus(DiceMatch.NextDice, new DiceStatBonus
            {
                power = 5
            });
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_SearingSword</summary>

    public class DiceCardSelfAbility_RMR_SearingSword : DiceCardSelfAbilityBase
    {
        public override void BeforeGiveDamage(BattleDiceBehavior behavior)
        {
            base.BeforeGiveDamage(behavior);
            behavior.ApplyDiceStatBonus(new DiceStatBonus
            {
                dmgRate = -50
            });
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_HeadtoHead</summary>

    public class DiceCardSelfAbility_RMR_HeadtoHead : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Bleeding_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Bleeding, 2, owner);
        }
        public override bool IsUniteCard => true;
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            foreach (BattleUnitModel battleUnitModel in BattleObjectManager.instance.GetAliveList(base.owner.faction))
            {
                if (battleUnitModel != base.owner && !battleUnitModel.bufListDetail.HasBuf<BattleUnitBuf_luxunitybuf>())
                {
                    battleUnitModel.bufListDetail.AddBuf(new BattleUnitBuf_luxunitybuf());
                }
            }
        }
        /// <summary>BattleUnitBuf_luxunitybuf</summary>
        public class BattleUnitBuf_luxunitybuf : BattleUnitBuf
        {
            public override void OnStartParrying(BattlePlayingCardDataInUnitModel card)
            {
                base.OnStartParrying(card);
                if (card.cardAbility != null)
                {
                    if (card.cardAbility.IsUniteCard)
                    {
                        int amount = _owner.bufListDetail.GetKewordBufStack(KeywordBuf.Bleeding);
                        card.target.currentDiceAction?.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                        {
                            dmg = -amount,
                            breakDmg = -amount 
                        });
                    }
                }
            }

            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                this.Destroy();
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_HeadtoHeadUpgrade</summary>

    public class DiceCardSelfAbility_RMR_HeadtoHeadUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Bleeding_Keyword" };
        public override bool IsUniteCard => true;
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            foreach (BattleUnitModel battleUnitModel in BattleObjectManager.instance.GetAliveList(base.owner.faction))
            {
                if (battleUnitModel != base.owner && !battleUnitModel.bufListDetail.HasBuf<DiceCardSelfAbility_RMR_HeadtoHead.BattleUnitBuf_luxunitybuf>())
                {
                    battleUnitModel.bufListDetail.AddBuf(new DiceCardSelfAbility_RMR_HeadtoHead.BattleUnitBuf_luxunitybuf());
                }
            }
        }

        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Bleeding, 2, owner);
        }

        public override void OnStartParrying()
        {
            base.OnStartParrying();
            owner.allyCardDetail.DrawCards(1);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_SunsetBlade</summary>

    public class DiceCardSelfAbility_RMR_SunsetBlade : DiceCardSelfAbilityBase
    {
        public override void OnStartParrying()
        {
            base.OnStartParrying();
            card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
            {
                min = 2,
                dmg = -3
            });
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_StructuralAnalysis</summary>

    public class DiceCardSelfAbility_RMR_StructuralAnalysis : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Paralysis_Keyword" };
        public override void OnUseCard()
        {
            if (card.target == null)
            {
                return;
            }
            List<BehaviourDetail> list = new List<BehaviourDetail>();
            int resistValue = GetResistValue(BehaviourDetail.Slash);
            int resistValue2 = GetResistValue(BehaviourDetail.Penetrate);
            int resistValue3 = GetResistValue(BehaviourDetail.Hit);
            int num = resistValue;
            if (resistValue2 > num)
            {
                num = resistValue2;
            }
            if (resistValue3 > num)
            {
                num = resistValue3;
            }
            if (num == resistValue)
            {
                list.Add(BehaviourDetail.Slash);
            }
            if (num == resistValue2)
            {
                list.Add(BehaviourDetail.Penetrate);
            }
            if (num == resistValue3)
            {
                list.Add(BehaviourDetail.Hit);
            }
            BehaviourDetail detail = RandomUtil.SelectOne(list);
            foreach (BattleDiceBehavior diceBehavior in card.GetDiceBehaviorList())
            {
                if (IsAttackDice(diceBehavior.behaviourInCard.Detail))
                {
                    diceBehavior.behaviourInCard = diceBehavior.behaviourInCard.Copy();
                    diceBehavior.behaviourInCard.Detail = detail;
                }
            }
        }

        private int GetResistValue(BehaviourDetail detail)
        {
            return Mathf.RoundToInt((0f + BookModel.GetResistRate(card.target.GetResistHP(detail)) + BookModel.GetResistRate(card.target.GetResistBP(detail))) * 10f);
        }
        public override void OnSucceedAttack(BattleDiceBehavior behavior)
        {
            base.OnSucceedAttack(behavior);
            if (behavior.Detail == BehaviourDetail.Penetrate)
            {
                card.target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Paralysis, 1, owner);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Feast</summary>

    public class DiceCardSelfAbility_RMR_Feast : DiceCardSelfAbilityBase
    {
        public override void OnEndBattle()
        {
            base.OnEndBattle();
            if (card.target.IsDead())
            {
                owner.allyCardDetail.ExhaustACardAnywhere(card.card);
                card.card.exhaust = true;
                if (owner.faction == Faction.Player && LogueBookModels.playerBattleModel.Contains<UnitBattleDataModel>(owner.UnitData))
                {
                    LogueBookModels.AddPlayerStat(LogueBookModels.playerBattleModel.Find((UnitBattleDataModel x) => x == owner.UnitData), new LogStatAdder { maxhp = 2 });
                }
                else
                {
                    owner.bufListDetail.AddBuf(new maxhphehehehe());
                }
                // add max hp increase
            }
        }

        /// <summary>maxhphehehehe</summary>

        public class maxhphehehehe : BattleUnitBuf
        {
            public override StatBonus GetStatBonus()
            {
                return new StatBonus
                {
                    hpAdder = 2
                };
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_FeastUpgrade</summary>

    public class DiceCardSelfAbility_RMR_FeastUpgrade : DiceCardSelfAbilityBase
    {
        public override void OnEndBattle()
        {
            base.OnEndBattle();
            if (card.target.IsDead())
            {
                owner.allyCardDetail.ExhaustACardAnywhere(card.card);
                card.card.exhaust = true;
                // add max hp increase
                if (owner.faction == Faction.Player && LogueBookModels.playerBattleModel.Contains<UnitBattleDataModel>(owner.UnitData))
                {
                    LogueBookModels.AddPlayerStat(LogueBookModels.playerBattleModel.Find((UnitBattleDataModel x) => x == owner.UnitData), new LogStatAdder { maxhp = 3 });
                }
                else
                {
                    owner.bufListDetail.AddBuf(new maxhphehehehe());
                }
            }
        }

        /// <summary>maxhphehehehe</summary>

        public class maxhphehehehe : BattleUnitBuf
        {
            public override StatBonus GetStatBonus()
            {
                return new StatBonus
                {
                    hpAdder = 3
                };
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Eject</summary>

    public class DiceCardSelfAbility_RMR_Eject : DiceCardSelfAbilityBase
    {
        public override void OnStartParrying()
        {
            base.OnStartParrying();
            card.target.currentDiceAction?.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
            {
                dmg = -2,
                breakDmg = -2
            });
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_EjectUpgrade</summary>

    public class DiceCardSelfAbility_RMR_EjectUpgrade : DiceCardSelfAbilityBase
    {
        public override void OnStartParrying()
        {
            base.OnStartParrying();
            card.target.currentDiceAction?.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
            {
                dmg = -4,
                breakDmg = -4
            });
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_NotAnotherStep</summary>

    public class DiceCardSelfAbility_RMR_NotAnotherStep : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_CriticalStrike_Keyword" };
        public override void OnStartParrying()
        {
            base.OnStartParrying();
            if (owner.speedDiceResult[card.slotOrder].value < card.target.speedDiceResult[card.targetSlotOrder].value)
            {
                owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.CritChance, 10, owner);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_NotAnotherStepUpgrade</summary>

    public class DiceCardSelfAbility_RMR_NotAnotherStepUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_CriticalStrike_Keyword" };
        public override void OnStartParrying()
        {
            base.OnStartParrying();
            if (owner.speedDiceResult[card.slotOrder].value < card.target.speedDiceResult[card.targetSlotOrder].value)
            {
                owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.CritChance, 15, owner);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_SharpenedBladev2</summary>

    public class DiceCardSelfAbility_RMR_SharpenedBladev2 : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Bleeding_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.DmgUp, 2, owner);
            owner.bufListDetail.AddBuf(new hehehehebleeeedILVOVEVEBLEEEED());
        }

        /// <summary>hehehehebleeeedILVOVEVEBLEEEED</summary>

        public class hehehehebleeeedILVOVEVEBLEEEED : BattleUnitBuf
        {
            public override void BeforeRollDice(BattleDiceBehavior behavior)
            {
                base.BeforeRollDice(behavior);
                if (behavior.Detail == BehaviourDetail.Slash)
                {
                    if (!behavior.abilityList.Contains(new heheheedmgbleed()))
                    {
                        behavior.AddAbility(new heheheedmgbleed());
                    }
                }
            }

            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                this.Destroy();
            }

            /// <summary>heheheedmgbleed</summary>

            public class heheheedmgbleed : DiceCardAbilityBase
            {
                public override void OnSucceedAttack(BattleUnitModel target)
                {
                    base.OnSucceedAttack(target);
                    target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Bleeding, owner.bufListDetail.GetKewordBufStack(KeywordBuf.DmgUp), owner);
                }
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_SharpenedBlade</summary>

    public class DiceCardSelfAbility_RMR_SharpenedBlade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Bleeding_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddBuf(new hehehehebleeeedILVOVEVEBLEEEED());
        }

        /// <summary>hehehehebleeeedILVOVEVEBLEEEED</summary>

        public class hehehehebleeeedILVOVEVEBLEEEED : BattleUnitBuf
        {
            public override int OnGiveKeywordBufByCard(BattleUnitBuf cardBuf, int stack, BattleUnitModel target)
            {
                if (cardBuf.bufType == KeywordBuf.Bleeding)
                {
                    return 1;
                }
                return base.OnGiveKeywordBufByCard(cardBuf, stack, target);
            }
            public override void BeforeGiveDamage(BattleDiceBehavior behavior)
            {
                base.BeforeGiveDamage(behavior);
                behavior.ApplyDiceStatBonus(new DiceStatBonus
                {
                    dmg = -2,
                    breakDmg = -2
                });
            }

            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                this.Destroy();
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_SharpenedBladeUpgrade</summary>

    public class DiceCardSelfAbility_RMR_SharpenedBladeUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Bleeding_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddBuf(new hehehehebleeeedILVOVEVEBLEEEED());
        }

        /// <summary>hehehehebleeeedILVOVEVEBLEEEED</summary>

        public class hehehehebleeeedILVOVEVEBLEEEED : BattleUnitBuf
        {
            public override int OnGiveKeywordBufByCard(BattleUnitBuf cardBuf, int stack, BattleUnitModel target)
            {
                if (cardBuf.bufType == KeywordBuf.Bleeding)
                {
                    return 2;
                }
                return base.OnGiveKeywordBufByCard(cardBuf, stack, target);
            }
            public override void BeforeGiveDamage(BattleDiceBehavior behavior)
            {
                base.BeforeGiveDamage(behavior);
                behavior.ApplyDiceStatBonus(new DiceStatBonus
                {
                    dmg = -4,
                    breakDmg = -4
                });
            }

            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                this.Destroy();
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_SilentMist</summary>

    public class DiceCardSelfAbility_RMR_SilentMist : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Protection_Keyword", "BreakProtection_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Protection, 3, owner);
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.BreakProtection, 3, owner);
            
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_ButterflySlash</summary>

    public class DiceCardSelfAbility_RMR_ButterflySlash : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Burn_Keyword" };
        public override void BeforeGiveDamage(BattleDiceBehavior behavior)
        {
            base.BeforeGiveDamage(behavior);
            behavior.ApplyDiceStatBonus(new DiceStatBonus
            {
                dmg = -4
            });
        }
        public override void OnWinParryingAtk()
        {
            base.OnWinParryingAtk();
            card.target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 1, owner);
        }

        public override void OnWinParryingDef()
        {
            base.OnWinParryingDef();
            card.target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 1, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_IndiscriminateShots</summary>

    public class DiceCardSelfAbility_RMR_IndiscriminateShots : DiceCardSelfAbilityBase
    {
        public override void OnUseCard()
        {
            base.OnUseCard();
            foreach (BattleDiceBehavior die in card.GetDiceBehaviorList())
            {
                PlayCopy(die);
            }
            this.card.DestroyDice(DiceMatch.AllDice, DiceUITiming.Start);
            this.card.DestroyPlayingCard();
        }

        public override void OnStartBattleAfterCreateBehaviour()
        {
            base.OnStartBattleAfterCreateBehaviour();
            this.card.DestroyDice(DiceMatch.AllDice, DiceUITiming.Start);
        }

        private void PlayCopy(BattleDiceBehavior die)
        {
            BattleUnitModel randomtarget = RandomUtil.SelectOne<BattleUnitModel>(BattleObjectManager.instance.GetAliveList_opponent(owner.faction).FindAll((BattleUnitModel x) => x.IsTargetable(owner)));
            int targetslot = UnityEngine.Random.Range(0, randomtarget.speedDiceResult.Count - 1);
            BattleDiceCardModel battleDiceCardModel = BattleDiceCardModel.CreatePlayingCard(ItemXmlDataList.instance.GetCardItem(card.card.GetID(), false));
            BattlePlayingCardDataInUnitModel battlePlayingCardDataInUnitModel = new BattlePlayingCardDataInUnitModel();
            battlePlayingCardDataInUnitModel.owner = owner;
            battlePlayingCardDataInUnitModel.card = battleDiceCardModel;
            battlePlayingCardDataInUnitModel.target = randomtarget;
            battlePlayingCardDataInUnitModel.earlyTarget = randomtarget;
            battlePlayingCardDataInUnitModel.earlyTargetOrder = targetslot;

            battlePlayingCardDataInUnitModel.targetSlotOrder = targetslot;
            battlePlayingCardDataInUnitModel.speedDiceResultValue = owner.GetSpeed(0);
            battlePlayingCardDataInUnitModel.slotOrder = 0;

            battlePlayingCardDataInUnitModel.ResetCardQueueWithoutStandby();
            battlePlayingCardDataInUnitModel.cardBehaviorQueue.Clear();
            BattleDiceBehavior diceBehavior = RMRUtilityExtensions.CreateBattleDiceBehavior(die.behaviourInCard, null);
            die.BetterCopyAbilityAndStat(diceBehavior);
            battlePlayingCardDataInUnitModel.AddDice(diceBehavior);

            StageController.Instance.GetAllCards().Insert(die.Index, battlePlayingCardDataInUnitModel);
            // StageController.Instance.AddAllCardListInBattle(battlePlayingCardDataInUnitModel, this.card.target);
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_IndiscriminateShotsDie1</summary>

    public class DiceCardAbility_RMR_IndiscriminateShotsDie1 : DiceCardAbilityBase
    {
        public override void OnSucceedAttack()
        {
            base.OnSucceedAttack();
            if (owner.isCrit())
            {
                owner.bufListDetail.AddBuf(new gagagapowerrINFINTIPOWEREWRR());
            }
        }

        /// <summary>gagagapowerrINFINTIPOWEREWRR</summary>

        public class gagagapowerrINFINTIPOWEREWRR : BattleUnitBuf
        {
            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                this.Destroy();
            }
            public override void OnRollDice(BattleDiceBehavior behavior)
            {
                base.OnRollDice(behavior);
                if (behavior.abilityList.Exists((DiceCardAbilityBase x) => x is DiceCardAbility_RMR_IndiscriminateShotsDie2))
                {
                    behavior.ApplyDiceStatBonus(new DiceStatBonus
                    {
                        power = 2
                    });
                    this.Destroy();
                }
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_IndiscriminateShotsDie1Upgrade</summary>

    public class DiceCardAbility_RMR_IndiscriminateShotsDie1Upgrade : DiceCardAbilityBase
    {
        public override void OnSucceedAttack()
        {
            base.OnSucceedAttack();
            if (owner.isCrit())
            {
                owner.bufListDetail.AddBuf(new gagagapowerrINFINTIPOWEREWRR());
            }
        }

        /// <summary>gagagapowerrINFINTIPOWEREWRR</summary>

        public class gagagapowerrINFINTIPOWEREWRR : BattleUnitBuf
        {
            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                this.Destroy();
            }
            public override void OnRollDice(BattleDiceBehavior behavior)
            {
                base.OnRollDice(behavior);
                if (behavior.abilityList.Exists((DiceCardAbilityBase x) => x is DiceCardAbility_RMR_IndiscriminateShotsDie2))
                {
                    behavior.ApplyDiceStatBonus(new DiceStatBonus
                    {
                        power = 4
                    });
                    this.Destroy();
                }
                
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_IndiscriminateShotsDie2</summary>

    public class DiceCardAbility_RMR_IndiscriminateShotsDie2 : DiceCardAbilityBase
    {
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            target.TakeDamage(behavior.DiceResultValue, DamageType.Card_Ability, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_BeyondShadow</summary>

    public class DiceCardSelfAbility_RMR_BeyondShadow : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_CriticalStrike_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            if (owner.bufListDetail.GetKewordBufStack(RoguelikeBufs.CritChance)/100 > RandomUtil.valueForProb)
            {
                card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                {
                    power = 2
                });
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_2bleedcrit</summary>

    public class DiceCardAbility_RMR_2bleedcrit : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "Bleeding_Keyword" };
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            if (owner.isCrit())
            {
                target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Bleeding, 2, owner);
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_3bleedcrit</summary>

    public class DiceCardAbility_RMR_3bleedcrit : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "Bleeding_Keyword" };
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            if (owner.isCrit())
            {
                target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Bleeding, 3, owner);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_ScatteringSlash</summary>

    public class DiceCardSelfAbility_RMR_ScatteringSlash : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Shield_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRShield, 12, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_ScatteringSlashUpgrade</summary>

    public class DiceCardSelfAbility_RMR_ScatteringSlashUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Shield_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRShield, 16, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_UnforgettableMelody</summary>

    public class DiceCardSelfAbility_RMR_UnforgettableMelody : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Strength_Keyword", "DrawCard_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            foreach (BattleUnitModel goofy in BattleObjectManager.instance.GetAliveList(owner.faction))
            {
                if (goofy.bufListDetail.GetActivatedBuf(KeywordBuf.Strength) != null)
                {
                    goofy.allyCardDetail.DrawCards(1);
                }
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_UnforgettableMelodyUpgrade</summary>

    public class DiceCardSelfAbility_RMR_UnforgettableMelodyUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Strength_Keyword", "DrawCard_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Strength, 1, owner);
            foreach (BattleUnitModel goofy in BattleObjectManager.instance.GetAliveList(owner.faction))
            {
                if (goofy.bufListDetail.GetActivatedBuf(KeywordBuf.Strength) != null)
                {
                    goofy.allyCardDetail.DrawCards(1);
                }
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_FaintMemories</summary>

    public class DiceCardSelfAbility_RMR_FaintMemories : DiceCardSelfAbilityBase
    {
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.breakDetail.TakeBreakDamage(7, DamageType.Card_Ability);
            card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
            {
                power = 2
            });
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_FaintMemoriesUpgrade</summary>

    public class DiceCardSelfAbility_RMR_FaintMemoriesUpgrade : DiceCardSelfAbilityBase
    {
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.breakDetail.TakeBreakDamage(9, DamageType.Card_Ability);
            card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
            {
                power = 3
            });
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_KillMotherfuckerOfChoice</summary>

    public class DiceCardAbility_RMR_KillMotherfuckerOfChoice : DiceCardAbilityBase
    {
        public override void BeforeGiveDamage()
        {
            base.BeforeGiveDamage();
            if (owner.isCrit())
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus
                {
                    dmgRate = 50,
                    breakRate = 50
                }); ;
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_KillMotherfuckerOfChoiceUpgrade</summary>

    public class DiceCardAbility_RMR_KillMotherfuckerOfChoiceUpgrade : DiceCardAbilityBase
    {
        public override void BeforeGiveDamage()
        {
            base.BeforeGiveDamage();
            if (owner.isCrit())
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus
                {
                    dmgRate = 75,
                    breakRate = 75
                }); ;
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_BindingArmsUpgrade</summary>

    public class DiceCardSelfAbility_RMR_BindingArmsUpgrade : DiceCardSelfAbilityBase
    {
        public override void OnUseCard()
        {
            base.OnUseCard();
            List<BattleDiceCardBuf> list = card.card.GetBufList().FindAll((BattleDiceCardBuf x) => x is CostDownSelfBuf);
            if (list.Count < 3)
            {
                card.card.AddBuf(new CostDownSelfBuf());
            }
        }

        /// <summary>CostDownSelfBuf</summary>

        public class CostDownSelfBuf : BattleDiceCardBuf
        {
            public override int GetCost(int oldCost)
            {
                return oldCost - 1;
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_HeavyPeaks</summary>

    public class DiceCardSelfAbility_RMR_HeavyPeaks : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Strength_Keyword", "Endurance_Keyword" };
        public override void OnEndBattle()
        {
            base.OnEndBattle();
            if (owner.emotionDetail._emotionCoins.Count >= owner.emotionDetail.MaximumCoinNumber)
            {
                owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Strength, 3, owner);
                owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Endurance, 3, owner);
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_nodamage</summary>

    public class DiceCardAbility_RMR_nodamage : DiceCardAbilityBase
    {
        public override void BeforeGiveDamage()
        {
            base.BeforeGiveDamage();
            behavior.ApplyDiceStatBonus(new DiceStatBonus
            {
                dmgRate = -9999,
                breakRate = -9999
            });
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Tailoring</summary>

    public class DiceCardSelfAbility_RMR_Tailoring : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "DrawCard_Keyword" };
        /// <summary>BattleDiceCardBuf_costDownCard</summary>
        public class BattleDiceCardBuf_costDownCard : BattleDiceCardBuf
        {
            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                this.Destroy();
            }

            public override int GetCost(int oldCost)
            {
                return oldCost - 1;
            }
        }
        public override void OnEnterCardPhase(BattleUnitModel unit, BattleDiceCardModel self)
        {
            base.OnEnterCardPhase(unit, self);
            if (unit.allyCardDetail.GetHand().Exists((BattleDiceCardModel x) => x.GetID() == new LorId(LogLikeMod.ModId, 390001)))
            {
                self.AddBufWithoutDuplication(new BattleDiceCardBuf_costDownCard());
            }
        }

        public override void OnUseCard()
        {
            base.OnUseCard();
            if (!owner.allyCardDetail.GetHand().Exists((BattleDiceCardModel x) => x.GetID() == new LorId(LogLikeMod.ModId, 390001)))
            {
                owner.allyCardDetail.DrawCards(2);
                card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                {
                    dmg = -4,
                    breakDmg = -4
                });
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_TailoringUpgrade</summary>

    public class DiceCardSelfAbility_RMR_TailoringUpgrade : DiceCardSelfAbilityBase
    {
        /// <summary>BattleDiceCardBuf_costDownCard</summary>
        public class BattleDiceCardBuf_costDownCard : BattleDiceCardBuf
        {
            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                this.Destroy();
            }

            public override int GetCost(int oldCost)
            {
                return oldCost - 1;
            }
        }
        public override void OnEnterCardPhase(BattleUnitModel unit, BattleDiceCardModel self)
        {
            base.OnEnterCardPhase(unit, self);
            if (unit.allyCardDetail.GetHand().Exists((BattleDiceCardModel x) => x.GetID() == new LorId(LogLikeMod.ModId, 390001)))
            {
                self.AddBufWithoutDuplication(new BattleDiceCardBuf_costDownCard());
            }
        }

        public override void OnEndBattle()
        {
            base.OnEndBattle();
            if (card.target.IsDead())
            {
                foreach (var book in LogLikeMod.rewards_lastKill)
                {
                    if (LogLikeMod.rewards.Remove(book))
                    {
                        ChapterGrade chapter = (ChapterGrade)(book.chapter - 1);
                        Rarity rarity = book.id.packageId == RMRCore.packageId ? (Rarity)(int.Parse(book.id.id.ToString().Last().ToString()) - 1) : Rarity.Unique;
                        LogLikeMod.rewards.Add(RMRCore.CurrentGamemode.GetCommonEnemyDropBook(chapter, rarity));
                    }
                }
            }
        }
    }


    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_FascinatingFabric</summary>


    public class DiceCardSelfAbility_RMR_FascinatingFabric : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Protection_Keyword", "BreakProtection_Keyword" };
        public override bool BeforeAddToHand(BattleUnitModel unit, BattleDiceCardModel self)
        {
            if (unit.allyCardDetail.GetAllDeck().FindAll((BattleDiceCardModel x) => x.GetID() == self.GetID()).Count >= 3)
            {
                return false;
            }
            return true;
        }
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Protection, 1, owner);
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.BreakProtection, 1, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_FascinatingFabricUpgrade</summary>

    public class DiceCardSelfAbility_RMR_FascinatingFabricUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Protection_Keyword", "BreakProtection_Keyword" };
        public override bool BeforeAddToHand(BattleUnitModel unit, BattleDiceCardModel self)
        {
            if (unit.allyCardDetail.GetAllDeck().FindAll((BattleDiceCardModel x) => x.GetID() == self.GetID()).Count >= 3)
            {
                return false;
            }
            return true;
        }
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Protection, 2, owner);
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.BreakProtection, 2, owner);
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_CrackOfDawnDie</summary>

    public class DiceCardAbility_RMR_CrackOfDawnDie : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "Burn_Keyword" };
        public override void BeforeGiveDamage()
        {
            base.BeforeGiveDamage();
            behavior.ApplyDiceStatBonus(new DiceStatBonus
            {
                dmgRate = -9999
            });
        }
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 9, owner);
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_CrackOfDawnDieUpgrade</summary>

    public class DiceCardAbility_RMR_CrackOfDawnDieUpgrade : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "Burn_Keyword" };
        public override void BeforeGiveDamage()
        {
            base.BeforeGiveDamage();
            behavior.ApplyDiceStatBonus(new DiceStatBonus
            {
                dmgRate = -9999
            });
        }
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 11, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_NowDie</summary>

    public class DiceCardSelfAbility_RMR_NowDie : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Bleeding_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Bleeding, 5, owner);
        }
        public override bool IsUniteCard => true;
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            foreach (BattleUnitModel battleUnitModel in BattleObjectManager.instance.GetAliveList(base.owner.faction))
            {
                if (battleUnitModel != base.owner && !battleUnitModel.bufListDetail.HasBuf<BattleUnitBuf_luxunitybuf>())
                {
                    battleUnitModel.bufListDetail.AddBuf(new BattleUnitBuf_luxunitybuf());
                }
            }
        }
        /// <summary>BattleUnitBuf_luxunitybuf</summary>
        public class BattleUnitBuf_luxunitybuf : BattleUnitBuf
        {
            public override void OnUseCard(BattlePlayingCardDataInUnitModel card)
            {
                base.OnUseCard(card);
                if (card.cardAbility != null)
                {
                    if (card.cardAbility.IsUniteCard)
                    {
                        _owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.DmgUp, _owner.bufListDetail.GetKewordBufStack(KeywordBuf.Bleeding) / 3, _owner);
                    }
                }
            }
            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                this.Destroy();
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Mendweapon</summary>

    public class DiceCardSelfAbility_RMR_Mendweapon : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Energy_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.cardSlotDetail.RecoverPlayPointByCard(1);
        }
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.PenetratePowerUp, 1, owner);
        }
    }
    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Refine</summary>
    public class DiceCardSelfAbility_RMR_Refine : DiceCardSelfAbilityBase
    {
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.PierceClashPower, 2, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Sakura</summary>

    public class DiceCardSelfAbility_RMR_Sakura : DiceCardSelfAbilityBase
    {
        public override void OnUseCard()
        {
            base.OnUseCard();
            int count = owner.allyCardDetail.GetHand().Count;
            if (count > 0)
            {
                for (int i = 0; i < count && i < 4; i++)
                {
                    owner.allyCardDetail.DisCardACardRandom();
                    card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                    {
                        min = 1
                    });
                }
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_SakuraUpgrade</summary>

    public class DiceCardSelfAbility_RMR_SakuraUpgrade : DiceCardSelfAbilityBase
    {
        public override void OnUseCard()
        {
            base.OnUseCard();
            int count = owner.allyCardDetail.GetHand().Count;
            if (count > 0)
            {
                for (int i = 0; i < count && i < 4; i++)
                {
                    owner.allyCardDetail.DisCardACardRandom();
                    card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                    {
                        min = 1
                    });
                    if (i == 3)
                    {
                        owner.bufListDetail.AddBuf(new googoogaagaaresistances());
                    }
                }
            }
        }
     

        /// <summary>googoogaagaaresistances</summary>
     

        public class googoogaagaaresistances : BattleUnitBuf
        {
            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                this.Destroy();
            }
            public override AtkResist GetResistBP(AtkResist origin, BehaviourDetail detail)
            {
                if (origin == AtkResist.Vulnerable || origin == AtkResist.Weak)
                {
                    return AtkResist.Endure;
                }
                return base.GetResistBP(origin, detail);
            }
            public override AtkResist GetResistHP(AtkResist origin, BehaviourDetail detail)
            {
                if (origin == AtkResist.Vulnerable || origin == AtkResist.Weak)
                {
                    return AtkResist.Endure;
                }
                return base.GetResistHP(origin, detail);
            }
        }
    
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_InkOver</summary>

    public class DiceCardSelfAbility_RMR_InkOver : DiceCardSelfAbilityBase
    {
        public override void OnRoundStart_inHand(BattleUnitModel unit, BattleDiceCardModel self)
        {
            base.OnRoundStart_inHand(unit, self);
            foreach (BattleUnitModel amog in BattleObjectManager.instance.GetAliveList_opponent(unit.faction))
            {
                if (amog.bufListDetail.GetKewordBufStack(KeywordBuf.Bleeding) >= 10)
                {
                    self.AddBuf(new costreduction());
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


        public override void OnSucceedAttack(BattleDiceBehavior behavior)
        {
            base.OnSucceedAttack(behavior);
            behavior.card?.target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Bleeding, 1, owner);
        }
    }
    /// <summary>Dice ability: DiceCardAbility_RMR_InkOverDie</summary>
    public class DiceCardAbility_RMR_InkOverDie : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "Bleeding_Keyword", "DrawCard_Keyword" };
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            if (target.bufListDetail.GetKewordBufStack(KeywordBuf.Bleeding) >= 4)
            {
                int count = target.bufListDetail.GetKewordBufStack(KeywordBuf.Bleeding) / 4;
                if (count > 2)
                {
                    count = 2;
                }
                owner.allyCardDetail.DrawCards(count);
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_InkOverDieUpgrade</summary>

    public class DiceCardAbility_RMR_InkOverDieUpgrade : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "Bleeding_Keyword", "DrawCard_Keyword" };
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            if (target.bufListDetail.GetKewordBufStack(KeywordBuf.Bleeding) >= 3)
            {
                int count = target.bufListDetail.GetKewordBufStack(KeywordBuf.Bleeding) / 3;
                if (count > 3)
                {
                    count = 3;
                }
                owner.allyCardDetail.DrawCards(count);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_energy2draw1</summary>

    public class DiceCardSelfAbility_RMR_energy2draw1 : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Energy_Keyword", "DrawCard_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.cardSlotDetail.RecoverPlayPointByCard(2);
            owner.allyCardDetail.DrawCards(1);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Observe</summary>

    public class DiceCardSelfAbility_RMR_Observe : DiceCardSelfAbilityBase
    {
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddBuf(new BattleUnitBuf_rmrobservebuf());
        }
        public override string[] Keywords => new string[] { "Strength_Keyword" };
        /// <summary>BattleUnitBuf_rmrobservebuf</summary>
        public class BattleUnitBuf_rmrobservebuf : BattleUnitBuf
        {
            public override void OnRoundStart()
            {
                base.OnRoundStart();
                _owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Strength, 1, _owner);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_ObserveUpgrade</summary>

    public class DiceCardSelfAbility_RMR_ObserveUpgrade : DiceCardSelfAbilityBase
    {
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Strength, 2, owner);
            owner.bufListDetail.AddBuf(new BattleUnitBuf_rmrobservebuf());
        }
        public override string[] Keywords => new string[] { "Strength_Keyword" };
        /// <summary>BattleUnitBuf_rmrobservebuf</summary>
        public class BattleUnitBuf_rmrobservebuf : BattleUnitBuf
        {
            public override void OnRoundStart()
            {
                base.OnRoundStart();
                _owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Strength, 1, _owner);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_WrathOfTormentUpgrade</summary>

    public class DiceCardSelfAbility_RMR_WrathOfTormentUpgrade : DiceCardSelfAbilityBase
    {
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.TakeDamage(9, DamageType.Card_Ability);
            card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
            {
                power = 3
            });
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_bleed1atkcard</summary>

    public class DiceCardSelfAbility_RMR_bleed1atkcard : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Bleeding_Keyword" };
        public override void OnSucceedAttack()
        {
            base.OnSucceedAttack();
            card.target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Bleeding, 1, owner);
        }
    }

    #endregion

    #region Urban Nightmare

    public class DiceCardSelfAbility_RMR_Engagement : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Shield_Keyword", "RMR_StaggerShield_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRShield, 6, owner);
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRStaggerShield, 6, owner);
        }
        public override void OnUseCard()
        {
            base.OnUseCard();
            //    this.owner.allyCardDetail.AddNewCard(Singleton<LogCardUpgradeManager>.Instance.GetUpgradeCard(new LorId(LogLikeMod.ModId, 511005)).id);
            owner.allyCardDetail.AddNewCard(new LorId(LogLikeMod.ModId, 511005));
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_EngagementUpgrade</summary>

    public class DiceCardSelfAbility_RMR_EngagementUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Shield_Keyword", "RMR_StaggerShield_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRShield, 8, owner);
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRStaggerShield, 8, owner);
        }
        public override void OnUseCard()
        {
            base.OnUseCard();
            this.owner.allyCardDetail.AddNewCard(Singleton<LogCardUpgradeManager>.Instance.GetUpgradeCard(new LorId(LogLikeMod.ModId, 511005)).id);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_EnGarde</summary>

    public class DiceCardSelfAbility_RMR_EnGarde : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_ClashPower_Keyword", "Strength_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddKeywordBufByCard(RoguelikeBufs.ClashPower, 1, owner);
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.ClashPower, 1, owner);
        }

        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Strength, 1, owner);
            card?.target?.bufListDetail.AddKeywordBufByCard(KeywordBuf.Strength, 1, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_EnGardeUpgrade</summary>

    public class DiceCardSelfAbility_RMR_EnGardeUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_ClashPower_Keyword", "Strength_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddKeywordBufByCard(RoguelikeBufs.ClashPower, 1, owner);
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.ClashPower, 1, owner);
        }

        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Strength, 2, owner);
            card?.target?.bufListDetail.AddKeywordBufByCard(KeywordBuf.Strength, 2, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_OvercomeCrisis</summary>

    public class DiceCardSelfAbility_RMR_OvercomeCrisis : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "DrawCard_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.allyCardDetail.DrawCards(1);
            List<BattleUnitModel> list = BattleObjectManager.instance.GetAliveList(owner.faction).FindAll((BattleUnitModel x) => x != owner && x.cardSlotDetail.cardQueue.Any((BattlePlayingCardDataInUnitModel card) => card.cardAbility != null && card.cardAbility.IsUniteCard));
            if (list.Count > 0)
            {
                RandomUtil.SelectOne<BattleUnitModel>(list).allyCardDetail.DrawCards(1);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_OvercomeCrisisUpgrade</summary>

    public class DiceCardSelfAbility_RMR_OvercomeCrisisUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "DrawCard_Keyword" };
        public override bool IsUniteCard => true;
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.allyCardDetail.DrawCards(1);
            List<BattleUnitModel> list = BattleObjectManager.instance.GetAliveList(owner.faction).FindAll((BattleUnitModel x) => x != owner && x.cardSlotDetail.cardQueue.Any((BattlePlayingCardDataInUnitModel card) => card.cardAbility != null && card.cardAbility.IsUniteCard));
            if (list.Count > 0)
            {
                RandomUtil.SelectOne<BattleUnitModel>(list).allyCardDetail.DrawCards(1);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_SmokingPipe</summary>

    public class DiceCardSelfAbility_RMR_SmokingPipe : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Smoke_Keyword", "Energy_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRSmoke, 3, owner);
            owner.cardSlotDetail.RecoverPlayPointByCard(1);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_SmokingPipeUpgrade</summary>

    public class DiceCardSelfAbility_RMR_SmokingPipeUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Smoke_Keyword", "Energy_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRSmoke, 4, owner);
            owner.cardSlotDetail.RecoverPlayPointByCard(1);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Juggling</summary>

    public class DiceCardSelfAbility_RMR_Juggling : DiceCardSelfAbilityBase
    {
        public override void OnEnterCardPhase(BattleUnitModel unit, BattleDiceCardModel self)
        {
            base.OnEnterCardPhase(unit, self);
            var card = ItemXmlDataList.instance.GetCardItem(self.GetID());
            card.Script = "";
            unit.allyCardDetail.AddNewCardToDeck(card.id);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Faith</summary>

    public class DiceCardSelfAbility_RMR_Faith : DiceCardSelfAbility_RMR_Zeal
    {
        public override string[] Keywords => new string[] { "Energy_Keyword", "OnlyOne_Keyword" };


        public override void OnUseCard()
        {
            base.OnUseCard();
            if (owner.allyCardDetail.IsHighlander())
            {
                owner.cardSlotDetail.RecoverPlayPointByCard(1);
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_FaithDie</summary>

    public class DiceCardAbility_RMR_FaithDie : DiceCardAbilityBase
    {
        public override void BeforeRollDice()
        {
            base.BeforeRollDice();
            int higheststack = 0;
            BehaviourDetail detail = BehaviourDetail.Slash;
            if (higheststack < owner.bufListDetail.GetKewordBufStack(KeywordBuf.SlashPowerUp))
            {
                higheststack = owner.bufListDetail.GetKewordBufStack(KeywordBuf.SlashPowerUp);
                detail = BehaviourDetail.Slash;
            }
            if (higheststack < owner.bufListDetail.GetKewordBufStack(KeywordBuf.PenetratePowerUp))
            {
                higheststack = owner.bufListDetail.GetKewordBufStack(KeywordBuf.PenetratePowerUp);
                detail = BehaviourDetail.Penetrate;
            }
            if (higheststack < owner.bufListDetail.GetKewordBufStack(KeywordBuf.HitPowerUp))
            {
                higheststack = owner.bufListDetail.GetKewordBufStack(KeywordBuf.HitPowerUp);
                detail = BehaviourDetail.Hit;
            }
            if (higheststack > 0)
            {
                behavior.behaviourInCard = behavior.behaviourInCard.Copy();
                if (behavior.Type == BehaviourType.Def)
                {
                    behavior.behaviourInCard.Type = BehaviourType.Atk;
                    behavior.behaviourInCard.Detail = detail;
                }
                else
                {
                    behavior.behaviourInCard.Detail = detail;
                }
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Leap</summary>

    public class DiceCardSelfAbility_RMR_Leap : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Quickness_Keyword", "WarpCharge", "DrawCard_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Quickness, 1, owner);
            BattleUnitBuf_warpCharge charge = owner.bufListDetail.GetActivatedBuf(KeywordBuf.WarpCharge) as BattleUnitBuf_warpCharge;
            if (charge != null && charge.UseStack(3, true))
            {
                owner.allyCardDetail.DrawCards(3);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_LeapUpgrade</summary>

    public class DiceCardSelfAbility_RMR_LeapUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Quickness_Keyword", "WarpCharge", "DrawCard_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Quickness, 2, owner);
            BattleUnitBuf_warpCharge charge = owner.bufListDetail.GetActivatedBuf(KeywordBuf.WarpCharge) as BattleUnitBuf_warpCharge;
            if (charge != null && charge.UseStack(3, true))
            {
                owner.allyCardDetail.DrawCards(3);
            }
        }
    }


    // reference this whenever you want to make an adapt ability, it has to have the same prefix too ie:
    // DiceCardSelfAbility_RMR_AdaptPage_cardabilityexample : DiceCardSelfAbility_RMR_AdaptPage
    // DiceCardAbility_RMR_AdaptDice_diceabilityexample : DiceCardAbility_RMR_AdaptDice
    /// <summary>ScrollAbility_RMR_AdaptScroll</summary>
    public class ScrollAbility_RMR_AdaptScroll : ScrollAbilityBase
    {
        public override void OnScrollUp(BattleUnitModel unit, BattleDiceCardModel self)
        {
            base.OnScrollUp(unit, self);
            DiceCardSelfAbilityBase Default = self.CreateDiceCardSelfAbilityScript();

            DiceCardXmlInfo diceCardXmlInfo = self.XmlData.Copy(true);
            if (!diceCardXmlInfo.Script.Contains("RMR_AdaptPage"))
            {
                return;
            }
            if (!diceCardXmlInfo.DiceBehaviourList.Exists(x => x.Script.Contains("RMR_AdaptDice")))
            {
                return;
            }
            List<DiceWithIndex> diceList = new List<DiceWithIndex>();
            DiceBehaviour adaptDice = diceCardXmlInfo.DiceBehaviourList.Find(x => x.Script.Contains("RMR_AdaptDice"));
            DiceWithIndex adaptDiceWithIndex = new DiceWithIndex { dice = adaptDice.Copy(), index = diceCardXmlInfo.DiceBehaviourList.IndexOf(adaptDice) };
            foreach (DiceBehaviour item in diceCardXmlInfo.DiceBehaviourList)
            {
                diceList.Add(new DiceWithIndex { dice = item.Copy(), index = diceCardXmlInfo.DiceBehaviourList.IndexOf(item) });
            }


            adaptDiceWithIndex = diceList.Find(x => x.index == adaptDiceWithIndex.index);
            adaptDiceWithIndex.index--;
            if (adaptDiceWithIndex.index < 0)
            {
                adaptDiceWithIndex.index = diceList.Count - 1;
                foreach (DiceWithIndex item in diceList.FindAll(x => x != adaptDiceWithIndex))
                {
                    item.index--;
                }
            }
            else
            {
                foreach (DiceWithIndex item in diceList.FindAll(x => x != adaptDiceWithIndex && x.index == adaptDiceWithIndex.index))
                {
                    item.index++;
                }
            }

            diceCardXmlInfo.DiceBehaviourList.Clear();
            diceList.Sort((DiceWithIndex x, DiceWithIndex y) => x.index - y.index);
            int num = diceList.Count;
            for (int i = 0; i < num; i++)
            {
                diceCardXmlInfo.DiceBehaviourList.Add(diceList[0].dice);
                diceList.RemoveAt(0);
            }
            self._xmlData = diceCardXmlInfo;

            DiceCardSelfAbilityBase diceCardSelfAbilityBase = self.CreateDiceCardSelfAbilityScript();
            if (diceCardSelfAbilityBase != null && diceCardSelfAbilityBase != Default)
            {
                diceCardSelfAbilityBase.OnEnterCardPhase(unit, self);
            }
        }

        public override void OnScrollDown(BattleUnitModel unit, BattleDiceCardModel self)
        {
            base.OnScrollDown(unit, self);
            DiceCardSelfAbilityBase Default = self.CreateDiceCardSelfAbilityScript();

            DiceCardXmlInfo diceCardXmlInfo = self.XmlData.Copy(true);
            if (!diceCardXmlInfo.Script.Contains("RMR_AdaptPage"))
            {
                return;
            }
            if (!diceCardXmlInfo.DiceBehaviourList.Exists(x => x.Script.Contains("RMR_AdaptDice")))
            {
                return;
            }
            List<DiceWithIndex> diceList = new List<DiceWithIndex>();
            DiceBehaviour adaptDice = diceCardXmlInfo.DiceBehaviourList.Find(x => x.Script.Contains("RMR_AdaptDice"));
            DiceWithIndex adaptDiceWithIndex = new DiceWithIndex { dice = adaptDice.Copy(), index = diceCardXmlInfo.DiceBehaviourList.IndexOf(adaptDice) };
            foreach (DiceBehaviour item in diceCardXmlInfo.DiceBehaviourList)
            {
                diceList.Add(new DiceWithIndex { dice = item.Copy(), index = diceCardXmlInfo.DiceBehaviourList.IndexOf(item) });
            }


            adaptDiceWithIndex = diceList.Find(x => x.index == adaptDiceWithIndex.index);
            adaptDiceWithIndex.index++;
            if (adaptDiceWithIndex.index > diceList.Count - 1)
            {
                adaptDiceWithIndex.index = 0;
                foreach (DiceWithIndex item in diceList.FindAll(x => x != adaptDiceWithIndex))
                {
                    item.index++;
                }
            }
            else
            {
                foreach (DiceWithIndex item in diceList.FindAll(x => x != adaptDiceWithIndex && x.index == adaptDiceWithIndex.index))
                {
                    item.index--;
                }
            }

            diceCardXmlInfo.DiceBehaviourList.Clear();
            diceList.Sort((DiceWithIndex x, DiceWithIndex y) => x.index - y.index);
            int num = diceList.Count;
            for (int i = 0; i < num; i++)
            {
                diceCardXmlInfo.DiceBehaviourList.Add(diceList[0].dice);
                diceList.RemoveAt(0);
            }
            self._xmlData = diceCardXmlInfo;

            DiceCardSelfAbilityBase diceCardSelfAbilityBase = self.CreateDiceCardSelfAbilityScript();
            if (diceCardSelfAbilityBase != null && diceCardSelfAbilityBase != Default)
            {
                diceCardSelfAbilityBase.OnEnterCardPhase(unit, self);
            }
        }

        /// <summary>DiceWithIndex</summary>

        public class DiceWithIndex
        {
            public DiceBehaviour dice { get; set; }
            public int index { get; set; }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_AdaptPage</summary>

    public class DiceCardSelfAbility_RMR_AdaptPage : DiceCardSelfAbilityBase
    {
        public override string[] Keywords
        {
            get
            {
                return new string[]
                {
                "RMR_Adapt_Keyword"
                };
            }
        }
        public override void OnEnterCardPhase(BattleUnitModel unit, BattleDiceCardModel self)
        {
            base.OnEnterCardPhase(unit, self);
            unit.AddScrollAbility(self, new ScrollAbility_RMR_AdaptScroll());
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_AdaptDice</summary>

    public class DiceCardAbility_RMR_AdaptDice : DiceCardAbilityBase
    {
        public override string[] Keywords
        {
            get
            {
                return new string[]
                {
                "RMR_Adapt_Keyword"
                };
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_AdaptDice_Riposte</summary>

    public class DiceCardAbility_RMR_AdaptDice_Riposte : DiceCardAbility_RMR_AdaptDice
    {
        public override void OnWinParrying()
        {
            base.OnWinParrying();
            card.target?.currentDiceAction?.AddDiceAdder(DiceMatch.NextDice, -2);
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_AdaptDice_RiposteUpgrade</summary>

    public class DiceCardAbility_RMR_AdaptDice_RiposteUpgrade : DiceCardAbility_RMR_AdaptDice
    {
        public override void OnWinParrying()
        {
            base.OnWinParrying();
            card.target?.currentDiceAction?.AddDiceAdder(DiceMatch.NextDice, -3);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_DesperateStruggle</summary>

    public class DiceCardSelfAbility_RMR_DesperateStruggle : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_ClashPower_Keyword" };
        public override bool IsUniteCard => true;

        public override void OnStartBattle()
        {
            base.OnStartBattle();
            foreach (BattleUnitModel battleUnitModel in BattleObjectManager.instance.GetAliveList(base.owner.faction))
            {
                if (battleUnitModel != base.owner && !battleUnitModel.bufListDetail.HasBuf<BattleUnitBuf_luxunitybuf>())
                {
                    battleUnitModel.bufListDetail.AddBuf(new BattleUnitBuf_luxunitybuf());
                }
            }
        }
        /// <summary>BattleUnitBuf_luxunitybuf</summary>
        public class BattleUnitBuf_luxunitybuf : BattleUnitBuf
        {
            bool trigger;
            public override void OnUseCard(BattlePlayingCardDataInUnitModel card)
            {
                base.OnUseCard(card);
                if (card.cardAbility != null)
                {
                    if (card.cardAbility.IsUniteCard && !trigger)
                    {
                        trigger = true;
                        _owner.TakeDamage(5, DamageType.Card_Ability);
                        _owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.ClashPower, 2, _owner);
                    }
                }
            }
            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                this.Destroy();
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_HiddenBlade</summary>

    public class DiceCardSelfAbility_RMR_HiddenBlade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Smoke_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            BattleUnitBuf_RMR_Smoke smoke = owner.bufListDetail.GetActivatedBuf(RoguelikeBufs.RMRSmoke) as BattleUnitBuf_RMR_Smoke;
            if (smoke != null && smoke.Spend(3, true))
            {
                card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                {
                    power = 2
                });
                owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.DmgUp, 2, owner);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_HiddenBladeUpgrade</summary>

    public class DiceCardSelfAbility_RMR_HiddenBladeUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Smoke_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            BattleUnitBuf_RMR_Smoke smoke = owner.bufListDetail.GetActivatedBuf(RoguelikeBufs.RMRSmoke) as BattleUnitBuf_RMR_Smoke;
            if (smoke != null && smoke.Spend(2, true))
            {
                card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                {
                    power = 2
                });
                owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.DmgUp, 2, owner);
            }
        }
    }


    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_FlyingSword</summary>


    public class DiceCardSelfAbility_RMR_FlyingSword : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Quickness_Keyword" };
        public override bool IsUniteCard => true;

        public override void OnStartBattle()
        {
            base.OnStartBattle();
            foreach (BattleUnitModel battleUnitModel in BattleObjectManager.instance.GetAliveList(base.owner.faction))
            {
                if (battleUnitModel != base.owner && !battleUnitModel.bufListDetail.HasBuf<BattleUnitBuf_luxunitybuf>())
                {
                    battleUnitModel.bufListDetail.AddBuf(new BattleUnitBuf_luxunitybuf());
                }
            }
        }
        /// <summary>BattleUnitBuf_luxunitybuf</summary>
        public class BattleUnitBuf_luxunitybuf : BattleUnitBuf
        {
            public override void OnUseCard(BattlePlayingCardDataInUnitModel card)
            {
                base.OnUseCard(card);
                if (card.cardAbility != null)
                {
                    if (card.cardAbility.IsUniteCard)
                    {
                        _owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Quickness, 1, _owner);
                    }
                }
            }
            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                this.Destroy();
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_FlyingSwordUpgrade</summary>

    public class DiceCardSelfAbility_RMR_FlyingSwordUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Quickness_Keyword" };
        public override bool IsUniteCard => true;
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Quickness, 2, owner);
        }
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            foreach (BattleUnitModel battleUnitModel in BattleObjectManager.instance.GetAliveList(base.owner.faction))
            {
                if (battleUnitModel != base.owner && !battleUnitModel.bufListDetail.HasBuf<DiceCardSelfAbility_RMR_FlyingSword.BattleUnitBuf_luxunitybuf>())
                {
                    battleUnitModel.bufListDetail.AddBuf(new DiceCardSelfAbility_RMR_FlyingSword.BattleUnitBuf_luxunitybuf());
                }
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_HastePower1</summary>

    public class DiceCardAbility_RMR_HastePower1 : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "Quickness_Keyword" };
        public override void BeforeRollDice()
        {
            base.BeforeRollDice();
            if (owner.bufListDetail.GetActivatedBuf(KeywordBuf.Quickness) != null)
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus
                {
                    power = 1
                });
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_HastePower2</summary>

    public class DiceCardAbility_RMR_HastePower2 : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "Quickness_Keyword" };
        public override void BeforeRollDice()
        {
            base.BeforeRollDice();
            if (owner.bufListDetail.GetActivatedBuf(KeywordBuf.Quickness) != null)
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus
                {
                    power = 2
                });
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Gigigi</summary>

    public class DiceCardSelfAbility_RMR_Gigigi : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Energy_Keyword", "DrawCard_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.cardSlotDetail.RecoverPlayPointByCard(1);
            owner.allyCardDetail.DrawCards(1);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_InhaleSmoke</summary>

    public class DiceCardSelfAbility_RMR_InhaleSmoke : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Smoke_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRSmoke, 4, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_InhaleSmokeUpgrade</summary>

    public class DiceCardSelfAbility_RMR_InhaleSmokeUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Smoke_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRSmoke, 6, owner);
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_WildCard</summary>

    public class DiceCardAbility_RMR_WildCard : DiceCardAbilityBase
    {
        public override void OnSucceedAttack()
        {
            base.OnSucceedAttack();
            foreach (BattleUnitModel amog in BattleObjectManager.instance.GetAliveList(owner.faction))
            {
                amog.breakDetail.RecoverBreak(5);
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_allydraw1atk</summary>

    public class DiceCardAbility_RMR_allydraw1atk : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "DrawCard_Keyword" };
        public override void OnSucceedAttack()
        {
            base.OnSucceedAttack();
            List<BattleUnitModel> gooners = BattleObjectManager.instance.GetAliveList(owner.faction).FindAll((BattleUnitModel x) => x != owner);
            if (gooners.Count > 0)
            {
                RandomUtil.SelectOne<BattleUnitModel>(gooners).allyCardDetail.DrawCards(1);
            }
        }
    }


    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Acupuncture</summary>


    public class DiceCardSelfAbility_RMR_Acupuncture : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_CriticalStrike_Keyword", "RMR_SlashClashPower_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.CritChance, 4, owner);
            if (owner.bufListDetail.GetKewordBufStack(RoguelikeBufs.CritChance) >= 15)
            {
                owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.SlashClashPower, 1, owner);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_ProselyteBlade</summary>

    public class DiceCardSelfAbility_RMR_ProselyteBlade : DiceCardSelfAbility_RMR_Zeal
    {
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddBuf(new gagaproselytebuf());
            owner.bufListDetail.AddBuf(new losepowerusmoothbrain());
        }

        /// <summary>gagaproselytebuf</summary>

        public class gagaproselytebuf : BattleUnitBuf
        {
            public override void OnRoundStart()
            {
                base.OnRoundStart();
                List<KeywordBuf> list = new List<KeywordBuf> { KeywordBuf.SlashPowerUp, KeywordBuf.PenetratePowerUp, KeywordBuf.HitPowerUp, KeywordBuf.DefensePowerUp };
                _owner.bufListDetail.AddKeywordBufThisRoundByCard(RandomUtil.SelectOne<KeywordBuf>(list), 1, _owner);
            }
        }

        /// <summary>losepowerusmoothbrain</summary>

        public class losepowerusmoothbrain : BattleUnitBuf
        {
            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                this.Destroy();
            }
            public override void BeforeRollDice(BattleDiceBehavior behavior)
            {
                base.BeforeRollDice(behavior);
                behavior.ApplyDiceStatBonus(new DiceStatBonus
                {
                    power = -4
                });
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_ProselyteBladeUpgrade</summary>

    public class DiceCardSelfAbility_RMR_ProselyteBladeUpgrade : DiceCardSelfAbilityBase
    {
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddBuf(new gagaproselytebuf());
            owner.bufListDetail.AddBuf(new losepowerusmoothbrain());
        }

        /// <summary>gagaproselytebuf</summary>

        public class gagaproselytebuf : BattleUnitBuf
        {
            public override void OnRoundStart()
            {
                base.OnRoundStart();
                List<KeywordBuf> list = new List<KeywordBuf> { KeywordBuf.SlashPowerUp, KeywordBuf.PenetratePowerUp, KeywordBuf.HitPowerUp, KeywordBuf.DefensePowerUp };
                _owner.bufListDetail.AddKeywordBufThisRoundByCard(RandomUtil.SelectOne<KeywordBuf>(list), 1, _owner);
            }
        }

        /// <summary>losepowerusmoothbrain</summary>

        public class losepowerusmoothbrain : BattleUnitBuf
        {
            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                this.Destroy();
            }
            public override void BeforeRollDice(BattleDiceBehavior behavior)
            {
                base.BeforeRollDice(behavior);
                behavior.ApplyDiceStatBonus(new DiceStatBonus
                {
                    power = -2
                });
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_SingletonPower2</summary>

    public class DiceCardAbility_RMR_SingletonPower2 : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "OnlyOne_Keyword" };
        public override void BeforeRollDice()
        {
            base.BeforeRollDice();
            if (owner.allyCardDetail.IsHighlander())
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus
                {
                    power = 2
                });
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_SenseQuarry</summary>

    public class DiceCardSelfAbility_RMR_SenseQuarry : DiceCardSelfAbility_RMR_Zeal
    {
        public override string[] Keywords => new string[] { "OnlyOne_Keyword" };

        public override void OnUseCard()
        {
            base.OnUseCard();
            if (owner.allyCardDetail.IsHighlander())
            {
                int amount = 0;
                foreach (BattleUnitBuf buf in card?.target?.bufListDetail.GetActivatedBufList())
                {
                    if (buf.positiveType == BufPositiveType.Negative)
                    {
                        amount++;
                    }
                }
                if (amount > 0)
                {
                    card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                    {
                        power = amount
                    });
                }
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_FlecheCounter</summary>

    public class DiceCardAbility_RMR_FlecheCounter : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "Strength_Keyword", "Endurance_Keyword" };
        public override void BeforeRollDice()
        {
            base.BeforeRollDice();
            if (behavior.card?.target?.bufListDetail.GetActivatedBuf(KeywordBuf.Strength) != null || behavior.card?.target?.bufListDetail.GetActivatedBuf(KeywordBuf.Endurance) != null)
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus
                {
                    power = 1
                });
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_FierceCharge</summary>

    public class DiceCardSelfAbility_RMR_FierceCharge : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Burn_Keyword" };
        public override void OnSucceedAttack()
        {
            base.OnSucceedAttack();
            card?.target?.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Burn, 5, owner);
        }

        public override void OnApplyCard()
        {
            base.OnApplyCard();
            RMRUtilityExtensions.AddSpeedImmediately(owner, owner.cardOrder, +6);
        }

        public override void OnReleaseCard()
        {
            base.OnReleaseCard();
            RMRUtilityExtensions.AddSpeedImmediately(owner, owner.cardOrder, -6);
        }

    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_FierceChargeUpgrade</summary>

    public class DiceCardSelfAbility_RMR_FierceChargeUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Burn_Keyword" };
        public override void OnSucceedAttack()
        {
            base.OnSucceedAttack();
            card?.target?.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Burn, 5, owner);
        }

        public override void OnWinParryingAtk()
        {
            base.OnWinParryingAtk();
            card?.target?.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Burn, 5, owner);
        }

        public override void OnWinParryingDef()
        {
            base.OnWinParryingDef();
            card?.target?.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Burn, 5, owner);
        }

        public override void OnApplyCard()
        {
            base.OnApplyCard();
            RMRUtilityExtensions.AddSpeedImmediately(owner, owner.cardOrder, +6);
        }

        public override void OnReleaseCard()
        {
            base.OnReleaseCard();
            RMRUtilityExtensions.AddSpeedImmediately(owner, owner.cardOrder, -6);
        }

    }

    /// <summary>Dice ability: DiceCardAbility_RMR_FierceChargeDie</summary>

    public class DiceCardAbility_RMR_FierceChargeDie : DiceCardAbilityBase
    {
        public override void BeforeGiveDamage()
        {
            base.BeforeGiveDamage();
            behavior.ApplyDiceStatBonus(new DiceStatBonus
            {
                dmgRate = -9999
            });
        }
        public override void OnWinParrying()
        {
            base.OnWinParrying();
            List<BattleUnitModel> aliveList = BattleObjectManager.instance.GetAliveList_opponent(owner.faction);
            if (aliveList.Count > 0)
            {
                BattleUnitModel target = RandomUtil.SelectOne(aliveList);
                Singleton<StageController>.Instance.AddAllCardListInBattle(card, target);
            }
        }
    }



    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_ExtremeEdge</summary>



    public class DiceCardSelfAbility_RMR_ExtremeEdge : DiceCardSelfAbilityBase
    {
        public override bool IsUniteCard => true;

        public override void OnStartBattle()
        {
            base.OnStartBattle();
            foreach (BattleUnitModel battleUnitModel in BattleObjectManager.instance.GetAliveList(base.owner.faction))
            {
                if (battleUnitModel != base.owner && !battleUnitModel.bufListDetail.HasBuf<BattleUnitBuf_luxunitybuf>())
                {
                    battleUnitModel.bufListDetail.AddBuf(new BattleUnitBuf_luxunitybuf());
                }
            }
        }
        /// <summary>BattleUnitBuf_luxunitybuf</summary>
        public class BattleUnitBuf_luxunitybuf : BattleUnitBuf
        {
            public override void OnUseCard(BattlePlayingCardDataInUnitModel card)
            {
                base.OnUseCard(card);
                if (card.cardAbility != null)
                {
                    if (card.cardAbility.IsUniteCard)
                    {
                        _owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Resistance, 1, _owner);
                    }
                }
            }
            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                this.Destroy();
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_YieldMyFlesh</summary>

    public class DiceCardAbility_RMR_YieldMyFlesh : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "onlypage_kim", "RMR_Shield_Keyword", "RMR_StaggerShield_Keyword" };
        public override void OnLoseParrying()
        {
            base.OnLoseParrying();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRShield, 15, owner);
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRStaggerShield, 15, owner);
            owner.allyCardDetail.AddNewCard(new LorId(LogLikeMod.ModId, 512006));
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_YieldMyFleshUpgrade</summary>

    public class DiceCardAbility_RMR_YieldMyFleshUpgrade : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "onlypage_kim", "RMR_Shield_Keyword", "RMR_StaggerShield_Keyword" };
        public override void OnLoseParrying()
        {
            base.OnLoseParrying();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRShield, 20, owner);
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRStaggerShield, 20, owner);
            this.owner.allyCardDetail.AddNewCard(Singleton<LogCardUpgradeManager>.Instance.GetUpgradeCard(new LorId(LogLikeMod.ModId, 512006)).id);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_ToWherePrescript</summary>

    public class DiceCardSelfAbility_RMR_ToWherePrescript : DiceCardSelfAbility_RMR_Zeal
    {
        public override string[] Keywords => new string[] { "OnlyOne_Keyword" };

        public override void OnUseCard()
        {
            base.OnUseCard();
            if (owner.allyCardDetail.IsHighlander())
            {
                card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                {
                    power = 1
                });
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_ToWherePrescriptUpgrade</summary>

    public class DiceCardSelfAbility_RMR_ToWherePrescriptUpgrade : DiceCardSelfAbility_RMR_Zeal
    {
        public override string[] Keywords => new string[] { "OnlyOne_Keyword", "Vulnerable_Keyword" };

        public override void OnUseCard()
        {
            base.OnUseCard();
            if (owner.allyCardDetail.IsHighlander())
            {
                card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                {
                    power = 2
                });
            }
        }

        public override void OnSucceedAttack()
        {
            base.OnSucceedAttack();
            card?.target?.bufListDetail.AddKeywordBufByCard(KeywordBuf.Vulnerable, 1, owner);
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_PrescriptDie</summary>

    public class DiceCardAbility_RMR_PrescriptDie : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "OnlyOne_Keyword"};
        public override void OnWinParrying()
        {
            base.OnWinParrying();
            if (owner.allyCardDetail.IsHighlander())
            {
                card?.DestroyDice(DiceMatch.NextDice, DiceUITiming.Start);
                behavior.card?.target?.currentDiceAction?.DestroyDice(DiceMatch.NextDice, DiceUITiming.Start);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Zeal</summary>

    public class DiceCardSelfAbility_RMR_Zeal : RMRCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Zeal_Keyword" };

        public bool zealtrigger;
        /*
        public void OnWaveStart_RogueLike(BattleDiceCardModel self, BattleUnitModel owner)
        {
            if (!zealtrigger)
            {
                zealtrigger = true;
                foreach (BattleUnitModel impostor in BattleObjectManager.instance.GetAliveList(owner.faction).FindAll((BattleUnitModel x) => x != owner))
                {
                    var card = impostor.allyCardDetail.AddNewCardToDeck(self.GetID());
                    if (card != null)
                    {
                        DiceCardSelfAbility_RMR_Zeal ability = card._script as DiceCardSelfAbility_RMR_Zeal;
                        ability.zealtrigger = true;
                    }
                }
            }
        }
        */
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_Execute</summary>

    public class DiceCardAbility_RMR_Execute : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "OnlyOne_Keyword" };
        public override void BeforeRollDice()
        {
            base.BeforeRollDice();
            int count = 0;
            foreach (BattleDiceCardModel card in owner.allyCardDetail.GetDeck())
            {
                if (RMRUtilityExtensions.CheckForKeyword(card, "OnlyOne_Keyword"))
                {
                    count++;
                }
            }
            if (count > 0)
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus
                {
                    power = 1+(count/2)
                });
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_ExecuteUpgrade</summary>

    public class DiceCardAbility_RMR_ExecuteUpgrade : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "OnlyOne_Keyword" };
        public override void BeforeRollDice()
        {
            base.BeforeRollDice();
            int count = 0;
            foreach (BattleDiceCardModel card in owner.allyCardDetail.GetDeck())
            {
                if (RMRUtilityExtensions.CheckForKeyword(card, "OnlyOne_Keyword"))
                {
                    count++;
                }
            }
            if (count > 0)
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus
                {
                    power = 3 + (count / 2)
                });
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_FleshFillet</summary>

    public class DiceCardSelfAbility_RMR_FleshFillet : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Smoke_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            BattleUnitBuf_RMR_Smoke smoke = owner.bufListDetail.GetActivatedBuf(RoguelikeBufs.RMRSmoke) as BattleUnitBuf_RMR_Smoke;
            if (smoke != null && smoke.Spend(2, true))
            {
                card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                {
                    power = 2
                });
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_FleshFilletDie</summary>

    public class DiceCardAbility_RMR_FleshFilletDie : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Smoke_Keyword", "Bleeding_Keyword" };
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Bleeding, target.bufListDetail.GetKewordBufStack(RoguelikeBufs.RMRSmoke), owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Slay</summary>

    public class DiceCardSelfAbility_RMR_Slay : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Strength_Keyword" };
        public override void OnSucceedAttack()
        {
            base.OnSucceedAttack();
            if (owner.isCrit())
            {
                owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Strength, 1, owner);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_sweeperpage</summary>

    public class DiceCardSelfAbility_RMR_sweeperpage : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Persistence_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRPersistence, 4, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_sweeperpageUpgrade</summary>

    public class DiceCardSelfAbility_RMR_sweeperpageUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Persistence_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRPersistence, 6, owner);
        }
    }

    /// <summary>AI card priority: DiceCardPriority_RMR_sweeper</summary>

    public class DiceCardPriority_RMR_sweeper : DiceCardPriorityBase
    {
        public override int GetPriorityBonus(BattleUnitModel owner)
        {
            if (owner.bufListDetail.GetActivatedBuf(RoguelikeBufs.RMRPersistence) != null)
            {
                return -3;
            }
            return 2;
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_RipSpace</summary>

    public class DiceCardSelfAbility_RMR_RipSpace : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "onlypage_warp_Keyword", "WarpCharge" };
        public int chargestack;
        public override void OnUseCard()
        {
            base.OnUseCard();
            BattleUnitBuf_warpCharge charge = owner.bufListDetail.GetActivatedBuf(KeywordBuf.WarpCharge) as BattleUnitBuf_warpCharge;
            if (charge != null)
            {
                chargestack = charge.stack;
                if (charge.UseStack(charge.stack, true))
                {
                    if (RandomUtil.valueForProb < (float)chargestack * 0.1f)
                    {
                        card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                        {
                            min = 8,
                            max = 8
                        });
                    }
                    else
                    {
                        chargestack = 0;
                        owner.TakeDamage(20, DamageType.Card_Ability);
                    }

                }
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_RipSpaceUpgrade</summary>

    public class DiceCardSelfAbility_RMR_RipSpaceUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "onlypage_warp_Keyword", "WarpCharge" };
        public int chargestack;
        public override void OnUseCard()
        {
            base.OnUseCard();
            BattleUnitBuf_warpCharge charge = owner.bufListDetail.GetActivatedBuf(KeywordBuf.WarpCharge) as BattleUnitBuf_warpCharge;
            if (charge != null)
            {
                chargestack = charge.stack;
                if (charge.UseStack(charge.stack, true))
                {
                    if (RandomUtil.valueForProb < (float)chargestack * 0.12f)
                    {
                        card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                        {
                            min = 8,
                            max = 8
                        });
                    }
                    else
                    {
                        chargestack = 0;
                        owner.TakeDamage(20, DamageType.Card_Ability);
                    }

                }
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_RipSpaceDie</summary>

    public class DiceCardAbility_RMR_RipSpaceDie : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[1] { "WarpCharge" };

        public override void BeforeRollDice()
        {
            DiceCardSelfAbility_RMR_RipSpace diceCardSelfAbility_warpSkill = base.card?.cardAbility as DiceCardSelfAbility_RMR_RipSpace;
            if (diceCardSelfAbility_warpSkill == null)
            {
                return;
            }
            if (diceCardSelfAbility_warpSkill.chargestack >= 10)
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus
                {
                    power = 2
                });
            }
            if (diceCardSelfAbility_warpSkill.chargestack < 5)
            {
                return;
            }
            base.owner.battleCardResultLog?.SetSucceedAtkEvent(delegate
            {
                FilterUtil.ShowWarpFilter();
                CameraFilterPack_FX_EarthQuake cameraFilterPack_FX_EarthQuake = SingletonBehavior<BattleCamManager>.Instance?.EffectCam.gameObject.AddComponent<CameraFilterPack_FX_EarthQuake>() ?? null;
                if (cameraFilterPack_FX_EarthQuake != null)
                {
                    cameraFilterPack_FX_EarthQuake.StartCoroutine(EarthQuakeRoutine_warp(cameraFilterPack_FX_EarthQuake));
                    AutoScriptDestruct autoScriptDestruct = SingletonBehavior<BattleCamManager>.Instance?.EffectCam.gameObject.AddComponent<AutoScriptDestruct>() ?? null;
                    if (autoScriptDestruct != null)
                    {
                        autoScriptDestruct.targetScript = cameraFilterPack_FX_EarthQuake;
                        autoScriptDestruct.time = 0.5f;
                    }
                }
            });

        }

        private IEnumerator EarthQuakeRoutine_warp(CameraFilterPack_FX_EarthQuake r)
        {
            float e = 0f;
            while (e < 1f)
            {
                e += Time.deltaTime * 2f;
                r.Speed = 30f * (1f - e);
                r.X = 0.02f * (1f - e);
                r.Y = 0.02f * (1f - e);
                yield return null;
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_RipSpaceDieUpgrade</summary>

    public class DiceCardAbility_RMR_RipSpaceDieUpgrade : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[1] { "WarpCharge" };

        public override void BeforeRollDice()
        {
            DiceCardSelfAbility_RMR_RipSpaceUpgrade diceCardSelfAbility_warpSkill = base.card?.cardAbility as DiceCardSelfAbility_RMR_RipSpaceUpgrade;
            if (diceCardSelfAbility_warpSkill == null)
            {
                return;
            }
            if (diceCardSelfAbility_warpSkill.chargestack >= 10)
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus
                {
                    power = 4
                });
            }
            if (diceCardSelfAbility_warpSkill.chargestack < 5)
            {
                return;
            }
            base.owner.battleCardResultLog?.SetSucceedAtkEvent(delegate
            {
                FilterUtil.ShowWarpFilter();
                CameraFilterPack_FX_EarthQuake cameraFilterPack_FX_EarthQuake = SingletonBehavior<BattleCamManager>.Instance?.EffectCam.gameObject.AddComponent<CameraFilterPack_FX_EarthQuake>() ?? null;
                if (cameraFilterPack_FX_EarthQuake != null)
                {
                    cameraFilterPack_FX_EarthQuake.StartCoroutine(EarthQuakeRoutine_warp(cameraFilterPack_FX_EarthQuake));
                    AutoScriptDestruct autoScriptDestruct = SingletonBehavior<BattleCamManager>.Instance?.EffectCam.gameObject.AddComponent<AutoScriptDestruct>() ?? null;
                    if (autoScriptDestruct != null)
                    {
                        autoScriptDestruct.targetScript = cameraFilterPack_FX_EarthQuake;
                        autoScriptDestruct.time = 0.5f;
                    }
                }
            });

        }

        private IEnumerator EarthQuakeRoutine_warp(CameraFilterPack_FX_EarthQuake r)
        {
            float e = 0f;
            while (e < 1f)
            {
                e += Time.deltaTime * 2f;
                r.Speed = 30f * (1f - e);
                r.X = 0.02f * (1f - e);
                r.Y = 0.02f * (1f - e);
                yield return null;
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_EndlessBattle</summary>

    public class DiceCardSelfAbility_RMR_EndlessBattle : DiceCardSelfAbilityBase
    {
        public override bool IsUniteCard => true;

        public override void OnStartParrying()
        {
            base.OnStartParrying();
            if (owner.hp <= (float)owner.MaxHp/4)
            {
                card.ignorePower = true;
                card.target.currentDiceAction.ignorePower = true;
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_LetShowBegin</summary>

    public class DiceCardSelfAbility_RMR_LetShowBegin : DiceCardSelfAbilityBase
    {
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddBuf(new showbuf());
        }

        /// <summary>showbuf</summary>

        public class showbuf : BattleUnitBuf  
        {
            public override void OnRoundEnd()
            {
                base.OnRoundEnd();
                this.Destroy();
            }
            public override void BeforeRollDice(BattleDiceBehavior behavior)
            {
                base.BeforeRollDice(behavior);
                if (behavior.Type == BehaviourType.Standby)
                {
                    behavior.ApplyDiceStatBonus(new DiceStatBonus
                    {
                        power = 2
                    });
                }
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_TraceFumes</summary>

    public class DiceCardSelfAbility_RMR_TraceFumes : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Smoke_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            if (card.target.bufListDetail.GetActivatedBuf(RoguelikeBufs.RMRSmoke) == null)
            {
                card.card.AddBuf(new reducecost());
            }
        }

        /// <summary>reducecost</summary>

        public class reducecost : BattleDiceCardBuf
        {
            public override int GetCost(int oldCost)
            {
                return oldCost - 1;
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_UndertakePrescript</summary>

    public class DiceCardSelfAbility_RMR_UndertakePrescript : DiceCardSelfAbility_RMR_Zeal
    {
        public override string[] Keywords => new string[] { "OnlyOne_Keyword" };

        public override void OnUseCard()
        {
            base.OnUseCard();
            if (!owner.allyCardDetail.IsHighlander())
            {
                card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                {
                    power = -2
                });
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_FlashingStrike</summary>

    public class DiceCardSelfAbility_RMR_FlashingStrike : DiceCardSelfAbilityBase
    {
        public override bool IsUniteCard => true;

        public override void OnUseCard()
        {
            base.OnUseCard();
            if (owner.speedDiceResult[card.slotOrder].value >= 6)
            {
                card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                {
                    power = 2
                });
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_FlashingStrikeUpgrade</summary>

    public class DiceCardSelfAbility_RMR_FlashingStrikeUpgrade : DiceCardSelfAbilityBase
    {
        public override bool IsUniteCard => true;

        public override void OnUseCard()
        {
            base.OnUseCard();
            if (owner.speedDiceResult[card.slotOrder].value >= 5)
            {
                card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                {
                    power = 2
                });
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_ForTheFamily</summary>

    public class DiceCardSelfAbility_RMR_ForTheFamily : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Protection_Keyword", "BreakProtection_Keyword", "RMR_Persistence_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            foreach (BattleUnitModel item in BattleObjectManager.instance.GetAliveList_random(base.owner.faction, 2))
            {
                item.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Protection, 1, base.owner);
                item.bufListDetail.AddKeywordBufByCard(KeywordBuf.Protection, 1, base.owner);
            }
        }

        public override void OnUseCard()
        {
            base.OnUseCard();
            foreach (BattleUnitModel ally in BattleObjectManager.instance.GetAliveList(owner.faction))
            {
                if (ally.bufListDetail.GetActivatedBuf(RoguelikeBufs.RMRPersistence) != null)
                {
                    ally.RecoverHP(3);
                    ally.breakDetail.RecoverBreak(3);
                }
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_ForTheFamilyUpgrade</summary>

    public class DiceCardSelfAbility_RMR_ForTheFamilyUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Protection_Keyword", "BreakProtection_Keyword", "RMR_Persistence_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            foreach (BattleUnitModel item in BattleObjectManager.instance.GetAliveList_random(base.owner.faction, 2))
            {
                item.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Protection, 1, base.owner);
                item.bufListDetail.AddKeywordBufByCard(KeywordBuf.Protection, 1, base.owner);
            }
        }

        public override void OnUseCard()
        {
            base.OnUseCard();
            foreach (BattleUnitModel ally in BattleObjectManager.instance.GetAliveList(owner.faction))
            {
                if (ally.bufListDetail.GetActivatedBuf(RoguelikeBufs.RMRPersistence) != null)
                {
                    ally.bufListDetail.AddKeywordBufByCard(KeywordBuf.Protection, 1, owner);
                    ally.RecoverHP(4);
                    ally.breakDetail.RecoverBreak(4);
                }
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_TrashDisposalDie</summary>

    public class DiceCardAbility_RMR_TrashDisposalDie : DiceCardAbilityBase
    {
        int count;
        public override void OnSucceedAttack()
        {
            base.OnSucceedAttack();
            if (count == 0)
            {
                BehaviourAction_sweeperOnly.movable = true;
            }
            if (behavior.DiceVanillaValue != behavior.GetDiceMin() && count < 6)
            {
                count++;
                base.ActivateBonusAttackDice();
            }
        }
    }


    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_BoundaryOfDeath</summary>


    public class DiceCardSelfAbility_RMR_BoundaryOfDeath : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "onlypage_yujin_Keyword", "RMR_Luck_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            if (owner.hp <= (float)owner.MaxHp / 5)
            {
                owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRLuck, 1, owner);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_BlazingStrike</summary>

    public class DiceCardSelfAbility_RMR_BlazingStrike : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "onlypage_philip_Keyword", "Burn_Keyword" };
        public override bool OnChooseCard(BattleUnitModel owner)
        {
            if (owner.emotionDetail.EmotionLevel >= 3)
            {
                return true;
            }
            return false;
        }
        public override void OnUseCard()
        {
            base.OnUseCard();
            int amount = card.target.bufListDetail.GetKewordBufStack(KeywordBuf.Burn);
            if (amount > 30)
            {
                amount = 30;
            }
            card.target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, amount, owner);
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_BlazingStrikeDie</summary>

    public class DiceCardAbility_RMR_BlazingStrikeDie : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "Burn_Keyword" };
        public override void BeforeGiveDamage()
        {
            base.BeforeGiveDamage();
            behavior.ApplyDiceStatBonus(new DiceStatBonus
            {
                dmgRate = -9999,
                breakRate = -9999
            });
        }
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 10, owner);
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_BlazingStrikeDieUpgrade</summary>

    public class DiceCardAbility_RMR_BlazingStrikeDieUpgrade : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "Burn_Keyword" };
        public override void BeforeGiveDamage()
        {
            base.BeforeGiveDamage();
            behavior.ApplyDiceStatBonus(new DiceStatBonus
            {
                dmgRate = -9999,
                breakRate = -9999
            });
        }
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Burn, 20, owner);
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_ForcefulGesture</summary>

    public class DiceCardAbility_RMR_ForcefulGesture : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "Weak_Keyword", "Disarm_Keyword", "Binding_Keyword", "Vulnerable_Keyword", "Vulnerable_break"};
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            List<KeywordBuf> list = new List<KeywordBuf> { KeywordBuf.Weak, KeywordBuf.Disarm, KeywordBuf.Binding, KeywordBuf.Vulnerable, KeywordBuf.Vulnerable_break };
            target.bufListDetail.AddKeywordBufByCard(RandomUtil.SelectOne<KeywordBuf>(list), 1, owner);
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_ForcefulGestureUpgrade</summary>

    public class DiceCardAbility_RMR_ForcefulGestureUpgrade : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "Weak_Keyword", "Disarm_Keyword", "Binding_Keyword", "Vulnerable_Keyword", "Vulnerable_break" };
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            List<KeywordBuf> list = new List<KeywordBuf> { KeywordBuf.Weak, KeywordBuf.Disarm, KeywordBuf.Binding, KeywordBuf.Vulnerable, KeywordBuf.Vulnerable_break };
            KeywordBuf buf = RandomUtil.SelectOne<KeywordBuf>(list);
            list.Remove(buf);
            target.bufListDetail.AddKeywordBufByCard(buf, 1, owner);
            target.bufListDetail.AddKeywordBufByCard(RandomUtil.SelectOne<KeywordBuf>(list), 1, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Ripple</summary>

    public class DiceCardSelfAbility_RMR_Ripple : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "WarpCharge" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            for (int i = 0; i < 2; i++)
            {
                BattleUnitBuf_warpCharge charge = owner.bufListDetail.GetActivatedBuf(KeywordBuf.WarpCharge) as BattleUnitBuf_warpCharge;
                if (charge != null && charge.UseStack(2, true))
                {
                    card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                    {
                        power = 2
                    });
                }
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_RippleDie</summary>

    public class DiceCardAbility_RMR_RippleDie : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "WarpCharge"};
        public override void OnLoseParrying()
        {
            base.OnLoseParrying();
            BattleUnitBuf_warpCharge charge = owner.bufListDetail.GetActivatedBuf(KeywordBuf.WarpCharge) as BattleUnitBuf_warpCharge;
            if (charge != null && charge.UseStack(1, true))
            {
                base.ActivateBonusAttackDice();
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Overthrow</summary>

    public class DiceCardSelfAbility_RMR_Overthrow : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_SlashClashPower_Keyword", "RMR_CriticalStrike_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.SlashClashPower, 3, owner);
        }

        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufByCard(RoguelikeBufs.CritChance, 7, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_OverthrowUpgrade</summary>

    public class DiceCardSelfAbility_RMR_OverthrowUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_SlashClashPower_Keyword", "RMR_CriticalStrike_Keyword" };
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.SlashClashPower, 3, owner);
        }

        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufByCard(RoguelikeBufs.CritChance, 5, owner);
            owner.bufListDetail.AddKeywordBufByCard(RoguelikeBufs.CritChance, 10, owner);
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_OverthrowDie</summary>

    public class DiceCardAbility_RMR_OverthrowDie : DiceCardAbilityBase
    {
        public override void OnSucceedAttack()
        {
            base.OnSucceedAttack();
            if (owner.isCrit())
            {
                card.card.AddBuf(new costreduction());
            }
        }
        /// <summary>costreduction</summary>
        public class costreduction : BattleDiceCardBuf
        {
            public override int GetCost(int oldCost)
            {
                return oldCost - 1;
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_WillOfThePrescript</summary>

    public class DiceCardSelfAbility_RMR_WillOfThePrescript : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "OnlyOne_Keyword", "RMR_Zeal_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            int count = 0;
            foreach (BattleDiceCardModel card in owner.allyCardDetail.GetDeck())
            {
                if (RMRUtilityExtensions.CheckForKeyword(card, "OnlyOne_Keyword"))
                {
                    count++;
                }
            }
            if (count > 0)
            {
                owner.allyCardDetail.DrawCards(count);
            }
        }



        // check the code on this again cuz its probably fucked
        public override void OnStartParrying()
        {
            base.OnStartParrying();
            List<BattleDiceCardModel> list = new List<BattleDiceCardModel>();
            List<DiceCardAbilityBase> list2 = new List<DiceCardAbilityBase>();
            foreach (BattleDiceCardModel card in owner.allyCardDetail.GetDeck())
            {
                if (RMRUtilityExtensions.CheckForKeyword(card, "RMR_Zeal_Keyword"))
                {
                    list.Add(card);
                }
            }
            if (list.Count > 0)
            {
                foreach (BattleDiceCardModel card in list)
                {
                    foreach (BattleDiceBehavior dies in card.CreateDiceCardBehaviorList())
                    {
                        if (dies.abilityList.Count > 0)
                        {
                            list2.AddRange(dies.abilityList);
                        }
                    }
                }
            }
            if (list2.Count > 0)
            {
                foreach (BattleDiceBehavior die in card.cardBehaviorQueue)
                {
                    die.AddAbility(RandomUtil.SelectOne(list2));
                }
            }
         
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Moulinet</summary>

    public class DiceCardSelfAbility_RMR_Moulinet : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Vulnerable_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            if (card.target.bufListDetail.GetActivatedBuf(KeywordBuf.Vulnerable) != null)
            {
                card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                {
                    power = 1
                });
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Overcharge</summary>

    public class DiceCardSelfAbility_RMR_Overcharge : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "WarpCharge" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.WarpCharge, 5, owner);
            owner.bufListDetail.AddReadyBuf(new BattleUnitBuf_sealTemp());
            if (owner.bufListDetail.GetKewordBufStack(KeywordBuf.WarpCharge) < 10)
            {
                owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.WarpCharge, 5, owner);
                owner.bufListDetail.AddReadyBuf(new BattleUnitBuf_sealTemp());
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_OverchargeUpgrade1</summary>

    public class DiceCardSelfAbility_RMR_OverchargeUpgrade1 : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "WarpCharge" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.WarpCharge, 7, owner);
            owner.bufListDetail.AddReadyBuf(new BattleUnitBuf_sealTemp());
            if (owner.bufListDetail.GetKewordBufStack(KeywordBuf.WarpCharge) < 10)
            {
                owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.WarpCharge, 7, owner);
                owner.bufListDetail.AddReadyBuf(new BattleUnitBuf_sealTemp());
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_OverchargeUpgrade2</summary>

    public class DiceCardSelfAbility_RMR_OverchargeUpgrade2 : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "WarpCharge" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.WarpCharge, 11, owner);
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Stun, 1, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_LossOfSensesUpgrade</summary>

    public class DiceCardSelfAbility_RMR_LossOfSensesUpgrade : DiceCardSelfAbilityBase
    {
        public override void OnWinParryingAtk()
        {
            base.OnWinParryingAtk();
            card.target?.currentDiceAction?.AddDiceAdder(DiceMatch.NextDice, -2);
        }

        public override void OnWinParryingDef()
        {
            base.OnWinParryingDef();
            card.target?.currentDiceAction?.AddDiceAdder(DiceMatch.NextDice, -2);
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_LossOfSensesDie</summary>

    public class DiceCardAbility_RMR_LossOfSensesDie : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Smoke_Keyword" };
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            BattleUnitBuf_RMR_Smoke smoke = target.bufListDetail.GetActivatedBuf(KeywordBuf.Smoke) as BattleUnitBuf_RMR_Smoke;
            if (smoke != null && smoke.stack >= 6)
            {
                smoke.stack -= 5;
                if (smoke.stack <= 0)
                {
                    smoke.Destroy();
                }
                List<BattleDiceCardModel> hand = card.target.allyCardDetail.GetDeck();
                int highest = 0;
                foreach (BattleDiceCardModel item in hand)
                {
                    int cost = item.GetCost();
                    if (highest < cost)
                    {
                        highest = cost;
                    }
                }
                List<BattleDiceCardModel> list = hand.FindAll((BattleDiceCardModel x) => x.GetCost() == highest);
                if (list.Count > 0)
                {
                    RandomUtil.SelectOne(list).AddBuf(new guhhuhhuhbuhhuh());
                }
            }

        }

        /// <summary>guhhuhhuhbuhhuh</summary>

        public class guhhuhhuhbuhhuh : BattleDiceCardBuf
        {
            public override void OnUseCard(BattleUnitModel owner)
            {
                base.OnUseCard(owner);
                this.Destroy();
            }
            public override int GetCost(int oldCost)
            {
                return oldCost + 1;
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_LoSDraw2</summary>

    public class DiceCardAbility_RMR_LoSDraw2 : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Smoke_Keyword", "DrawCard_Keyword" };
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            BattleUnitBuf_RMR_Smoke smoke = target.bufListDetail.GetActivatedBuf(KeywordBuf.Smoke) as BattleUnitBuf_RMR_Smoke;
            if (smoke != null && smoke.stack >= 3)
            {
                owner.allyCardDetail.DrawCards(2);
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_AutomatedMovement</summary>

    public class DiceCardSelfAbility_RMR_AutomatedMovement : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "Quickness_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Quickness, 2, owner);
        }

        public override void OnApplyCard()
        {
            base.OnApplyCard();
            RMRUtilityExtensions.AddSpeedImmediately(owner, owner.cardOrder, +2);
        }

        public override void OnReleaseCard()
        {
            base.OnReleaseCard();
            RMRUtilityExtensions.AddSpeedImmediately(owner, owner.cardOrder, -2);
        }

    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_RepressedFlesh</summary>

    public class DiceCardSelfAbility_RMR_RepressedFlesh : DiceCardSelfAbilityBase
    {
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddBuf(new bluntingiambluntingoooh());
        }
        /// <summary>bluntingiambluntingoooh</summary>
        public class bluntingiambluntingoooh : BattleUnitBuf
        {
            public override void OnSuccessAttack(BattleDiceBehavior behavior)
            {
                base.OnSuccessAttack(behavior);
                if (behavior.Detail == BehaviourDetail.Hit)
                {
                    behavior.card?.target?.breakDetail.TakeBreakDamage(1, DamageType.Card_Ability);
                }
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_RepressedFleshUpgrade</summary>

    public class DiceCardSelfAbility_RMR_RepressedFleshUpgrade : DiceCardSelfAbilityBase
    {
        public override void OnStartBattle()
        {
            base.OnStartBattle();
            owner.bufListDetail.AddBuf(new bluntingiambluntingoooh());
        }
        /// <summary>bluntingiambluntingoooh</summary>
        public class bluntingiambluntingoooh : BattleUnitBuf
        {
            public override void OnSuccessAttack(BattleDiceBehavior behavior)
            {
                base.OnSuccessAttack(behavior);
                if (behavior.Detail == BehaviourDetail.Hit)
                {
                    behavior.card?.target?.breakDetail.TakeBreakDamage(2, DamageType.Card_Ability);
                }
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_CoolerRepressed</summary>

    public class DiceCardAbility_RMR_CoolerRepressed : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "DrawCard_Keyword" };
        public override void BeforeRollDice()
        {
            if (behavior.TargetDice != null)
            {
                BattleDiceBehavior targetDice = behavior.TargetDice;
                if (IsDefenseDice(targetDice.Detail))
                {
                    targetDice.ApplyDiceStatBonus(new DiceStatBonus
                    {
                        power = -3
                    });
                }
            }
        }
        public override void OnWinParrying()
        {
            base.OnWinParrying();
            if (IsDefenseDice(behavior.TargetDice.Detail))
            {
                owner.allyCardDetail.DrawCards(1);
            }
        }
    }





    #endregion


    #region Star of the City

    // while I am not handling sotc yet, I have to adjust the SotC smoke pages

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Gain2Smoke</summary>

    public class DiceCardSelfAbility_RMR_Gain2Smoke : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Smoke_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRSmoke, 2, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Gain3Smoke</summary>

    public class DiceCardSelfAbility_RMR_Gain3Smoke : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Smoke_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRSmoke, 3, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Gain4Smoke</summary>

    public class DiceCardSelfAbility_RMR_Gain4Smoke : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Smoke_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRSmoke, 4, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Gain5Smoke</summary>

    public class DiceCardSelfAbility_RMR_Gain5Smoke : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Smoke_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRSmoke, 5, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_GuidanceGears</summary>

    public class DiceCardSelfAbility_RMR_GuidanceGears : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Smoke_Keyword", "Energy_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRSmoke, 2, owner);
            owner.cardSlotDetail.RecoverPlayPointByCard(2);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_GuidanceGearsUpgrade</summary>

    public class DiceCardSelfAbility_RMR_GuidanceGearsUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Smoke_Keyword", "Energy_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRSmoke, 3, owner);
            owner.cardSlotDetail.RecoverPlayPointByCard(2);
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_SmokeBlowDie</summary>

    public class DiceCardAbility_RMR_SmokeBlowDie : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Smoke_Keyword", "Energy_Keyword" };
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            target.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRSmoke, 3, owner);
            owner.cardSlotDetail.RecoverPlayPointByCard(1);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_SmokeSmash</summary>

    public class DiceCardSelfAbility_RMR_SmokeSmash : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Smoke_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            if (owner.bufListDetail.GetKewordBufStack(RoguelikeBufs.RMRSmoke) >= 9)
            {
                card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                {
                    power = 2
                });
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_SmokeSmashUpgrade</summary>

    public class DiceCardSelfAbility_RMR_SmokeSmashUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Smoke_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            if (owner.bufListDetail.GetKewordBufStack(RoguelikeBufs.RMRSmoke) >= 8)
            {
                card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                {
                    power = 2
                });
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_AssaultOrder</summary>

    public class DiceCardSelfAbility_RMR_AssaultOrder : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Smoke_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            if (card.target.bufListDetail.GetKewordBufStack(RoguelikeBufs.RMRSmoke) >= 2)
            {
                card.ApplyDiceStatBonus(DiceMatch.AllAttackDice, new DiceStatBonus
                {
                    power = 1
                });
            }
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_AssaultOrderUpgrade</summary>

    public class DiceCardSelfAbility_RMR_AssaultOrderUpgrade : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Smoke_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            if (card.target.bufListDetail.GetKewordBufStack(RoguelikeBufs.RMRSmoke) >= 2)
            {
                card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                {
                    power = 1
                });
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_SmokePrickDie</summary>

    public class DiceCardAbility_RMR_SmokePrickDie : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Smoke_Keyword" };
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            target.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRSmoke, 2, owner);
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRSmoke, 1, owner);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Vapour</summary>

    public class DiceCardSelfAbility_RMR_Vapour : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Smoke_Keyword", "DrawCard_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRSmoke, 4, owner);
            owner.allyCardDetail.DrawCards(1);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_ExhaleSmoke</summary>

    public class DiceCardSelfAbility_RMR_ExhaleSmoke : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "onlypage_cor", "RMR_Smoke_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRSmoke, 8, owner);
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_ExhaleSmokeDie</summary>

    public class DiceCardAbility_RMR_ExhaleSmokeDie : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_Smoke_Keyword", "Paralysis_Keyword" };
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            target.bufListDetail.AddKeywordBufThisRoundByCard(RoguelikeBufs.RMRSmoke, 3, owner);
            if (target.bufListDetail.GetKewordBufStack(RoguelikeBufs.RMRSmoke) >= 5)
            {
                target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Paralysis, 3, owner);
            }
        }
    }

    #endregion


    #region Fixes and Edits

    public class DiceCardSelfAbility_fireIlsumLogEdit : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[1] { "DrawCard_Keyword" };

        public override void OnUseCard()
        {
            List<BattleDiceCardModel> list = base.owner.allyCardDetail.GetHand().FindAll(x => x.GetID().GetOriginalId() == card.card.GetID().GetOriginalId());
            if (list.Count <= 0)
            {
                return;
            }
            BattleDiceCardModel battleDiceCardModel = RandomUtil.SelectOne(list);
            foreach (BattleDiceBehavior item in battleDiceCardModel.CreateDiceCardBehaviorList())
            {
                if (item.Type == BehaviourType.Atk)
                {
                    card.AddDice(item);
                }
            }
            base.owner.allyCardDetail.DiscardACardByAbility(battleDiceCardModel);
            base.owner.allyCardDetail.DrawCards(1);
        }
    }


    /// <summary>Card self-ability: DiceCardSelfAbility_costdownLogEdit</summary>


    public class DiceCardSelfAbility_costdownLogEdit : DiceCardSelfAbilityBase
    {
        /// <summary>BattleDiceCardBuf_costDownCard</summary>
        public class BattleDiceCardBuf_costDownCard : BattleDiceCardBuf
        {
            private int _count;

            public override DiceCardBufType bufType => DiceCardBufType.CostDownR;

            public BattleDiceCardBuf_costDownCard(int count)
            {
                _count = count;
            }

            public override int GetCost(int oldCost)
            {
                return oldCost - _count;
            }
        }

        public override void OnEnterCardPhase(BattleUnitModel unit, BattleDiceCardModel self)
        {
            List<BattleDiceCardModel> list = unit.allyCardDetail.GetHand().FindAll((BattleDiceCardModel x) => x != self && x.GetID().GetOriginalId() == card.card.GetID().GetOriginalId());
            self.AddBufWithoutDuplication(new BattleDiceCardBuf_costDownCard(list.Count));
        }
    }

    #endregion


    #region Enemy Effects

    public class DiceCardSelfAbility_RMR_PrescriptLoophole : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "IndexReleaseCard3_Keyword" };
        public override void OnUseCard()
        {
            base.OnUseCard();
            owner.allyCardDetail.AddNewCard(new LorId(LogLikeMod.ModId, 605010));
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Skewer</summary>

    public class DiceCardSelfAbility_RMR_Skewer : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "OnlyOne_Keyword","Vulnerable_Keyword", "Vulnerable_break" };

        public override void OnUseCard()
        {
            base.OnUseCard();
            if (owner.allyCardDetail.IsHighlander())
            {
                card.target.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Vulnerable, 1, owner);
                card.target.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Vulnerable_break, 1, owner);
            }
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_SkewerDie</summary>

    public class DiceCardAbility_RMR_SkewerDie : DiceCardAbilityBase
    {
        public override string[] Keywords => new string[] { "OnlyOne_Keyword" };
        public override void OnWinParrying()
        {
            base.OnWinParrying();
            if (owner.allyCardDetail.IsHighlander())
            {
                base.card?.target?.currentDiceAction?.DestroyDice(DiceMatch.NextDice);
            }
        }
    }

    /// <summary>
    /// Upgrade-aware version of the Red Mist vertical slash.
    /// Vanilla compares other copies against the bare 607003 LorId, which excludes
    /// upgraded copies whose packageId stores RMR upgrade metadata.
    /// </summary>
    public class DiceCardSelfAbility_RMR_kaliNormalAttackJ : DiceCardSelfAbilityBase
    {
        private int _totalDamage;

        public override string[] Keywords => new string[] { "DrawCard_Keyword" };

        public override void OnUseCard()
        {
            _totalDamage = 0;
        }

        public override void AfterGiveDamage(int damage, BattleUnitModel target)
        {
            _totalDamage += damage;
        }

        public override void OnEndBattle()
        {
            if (_totalDamage < 8 || owner?.allyCardDetail == null || card?.card == null)
                return;

            LorId usedOriginId = card.card.GetID()?.GetOriginalId();
            if (usedOriginId == null || usedOriginId == LorId.None)
                return;

            foreach (BattleDiceCardModel other in owner.allyCardDetail.GetAllDeck())
            {
                if (other == null || other == card.card)
                    continue;
                LorId otherId = other.GetID();
                if (otherId != null && otherId.GetOriginalId() == usedOriginId)
                    other.AddCost(-1);
            }
            owner.allyCardDetail.DrawCards(1);
        }
    }

    /// <summary>
    /// Upgrade-aware version of the Red Mist greatsword thrust.
    /// Vanilla compares other copies against the bare 607004 LorId, which excludes
    /// upgraded copies whose packageId stores RMR upgrade metadata.
    /// </summary>
    public class DiceCardSelfAbility_RMR_kaliNormalAttackZ : DiceCardSelfAbilityBase
    {
        private int _totalDamage;

        public override string[] Keywords => new string[] { "DrawCard_Keyword" };

        public override void OnUseCard()
        {
            _totalDamage = 0;
        }

        public override void AfterGiveDamage(int damage, BattleUnitModel target)
        {
            _totalDamage += damage;
        }

        public override void OnEndBattle()
        {
            if (_totalDamage < 8 || owner?.allyCardDetail == null || card?.card == null)
                return;

            LorId usedOriginId = card.card.GetID()?.GetOriginalId();
            if (usedOriginId == null || usedOriginId == LorId.None)
                return;

            foreach (BattleDiceCardModel other in owner.allyCardDetail.GetAllDeck())
            {
                if (other == null || other == card.card)
                    continue;
                LorId otherId = other.GetID();
                if (otherId != null && otherId.GetOriginalId() == usedOriginId)
                    other.AddCost(-1);
            }
            owner.allyCardDetail.DrawCards(1);
        }
    }

    /// <summary>
    /// Upgrade-aware version of the Red Mist horizontal slash.
    /// Vanilla compares other copies against the bare 607005 LorId, which excludes
    /// upgraded copies whose packageId stores RMR upgrade metadata.
    /// </summary>
    public class DiceCardSelfAbility_RMR_kaliNormalAttackH : DiceCardSelfAbilityBase
    {
        private int _totalDamage;

        public override string[] Keywords => new string[] { "Energy_Keyword" };

        public override void OnUseCard()
        {
            _totalDamage = 0;
        }

        public override void AfterGiveDamage(int damage, BattleUnitModel target)
        {
            _totalDamage += damage;
        }

        public override void OnEndBattle()
        {
            if (_totalDamage < 8 || owner?.allyCardDetail == null || card?.card == null)
                return;

            LorId usedOriginId = card.card.GetID()?.GetOriginalId();
            if (usedOriginId == null || usedOriginId == LorId.None)
                return;

            foreach (BattleDiceCardModel other in owner.allyCardDetail.GetAllDeck())
            {
                if (other == null || other == card.card)
                    continue;
                LorId otherId = other.GetID();
                if (otherId != null && otherId.GetOriginalId() == usedOriginId)
                    other.AddCost(-1);
            }
            owner.cardSlotDetail.RecoverPlayPointByCard(2);
        }
    }

    /// <summary>Card self-ability: DiceCardSelfAbility_RMR_Amputation</summary>

    public class DiceCardSelfAbility_RMR_Amputation : DiceCardSelfAbilityBase
    {
        public override string[] Keywords => new string[] { "RMR_onlypage_copley" };
        public override bool OnChooseCard(BattleUnitModel owner)
        {
            if (owner.bufListDetail.GetActivatedBuf(KeywordBuf.IndexRelease) == null)
            {
                return false;
            }
            return true;
        }


    }

    /// <summary>Dice ability: DiceCardAbility_RMR_Amputationfirstdie</summary>

    public class DiceCardAbility_RMR_Amputationfirstdie : DiceCardAbilityBase
    {
        public override void OnWinParrying()
        {
            base.OnWinParrying();
            base.card?.target?.currentDiceAction?.DestroyDice(DiceMatch.AllDice);
        }

        public override void OnLoseParrying()
        {
            base.OnLoseParrying();
            base.card.DestroyDice(DiceMatch.AllDice);
        }
    }

    /// <summary>Dice ability: DiceCardAbility_RMR_Amputationdie</summary>

    public class DiceCardAbility_RMR_Amputationdie : DiceCardAbilityBase
    {
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            base.OnSucceedAttack(target);
            if (owner.faction == Faction.Enemy)
            {
                target.TakeDamage(target.MaxHp / 4, DamageType.Card_Ability, owner);
                target.bufListDetail.AddBuf(new amputationbuf());
            }
            else
            {
                target.TakeDamage(target.MaxHp / 4, DamageType.Card_Ability, owner);
                LogueBookModels.AddPlayerStat(target.UnitData, new LogStatAdder()
                {
                    maxhppercent = -25
                });
            }
        }

        /// <summary>amputationbuf</summary>

        public class amputationbuf : BattleUnitBuf
        {
            public override StatBonus GetStatBonus()
            {
                return new StatBonus
                {
                    hpRate = -25
                };
            }
        }
    }

    #endregion
}
