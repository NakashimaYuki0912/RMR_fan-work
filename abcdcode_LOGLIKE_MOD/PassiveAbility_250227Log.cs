// -----------------------------------------------------------------------------
// Passive ability script: PassiveAbility_250227Log
// Namespace/file: ruina-roguelike-reborn-main\abcdcode_LOGLIKE_MOD\PassiveAbility_250227Log.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;


namespace abcdcode_LOGLIKE_MOD
{

    /// <summary>Passive ability: PassiveAbility_250227Log</summary>

    public class PassiveAbility_250227Log : PassiveAbilityBase
    {
        public int _teleportCondition = 350;
        public int _patternCount;
        public int _teleported;
        public int _stanceCooltime;
        public List<PurpleStance> _alreayUsed = new List<PurpleStance>();
        public PassiveAbility_250127 _stancePassive;
        public int _areaCoolTime = 1;
        public bool _teleportReady;
        public int _dmgReduction;
        #region --- Battle hooks ---


        public override int SpeedDiceNumAdder() => this._patternCount <= 3 ? 2 : 3;

        public int GetCurrentSpeedDiceNum()
        {
            return this.owner.Book.GetSpeedDiceRule(this.owner).Roll(this.owner).Count;
        }

        public override void OnWaveStart()
        {
            this._patternCount = this.owner.UnitData.floorBattleData.param1;
            this._teleported = this.owner.UnitData.floorBattleData.param2;
            this._areaCoolTime = this._teleported <= 0 ? 0 : 1;
            this._stancePassive = this.owner.passiveDetail.PassiveList.Find((Predicate<PassiveAbilityBase>)(x => x is PassiveAbility_250127)) as PassiveAbility_250127;
        }

        public override void OnRoundEnd()
        {
            this.owner.cardSlotDetail.RecoverPlayPoint(this.owner.cardSlotDetail.GetMaxPlayPoint());
        }

        public override void OnRoundStart()
        {
            if (this.owner.UnitData.floorBattleData.param2 > 0 || !this._teleportReady && (double)this.owner.hp > (double)this._teleportCondition)
                return;
            if (LogLikeMod.CheckStage(true))
            {
                EnterSecondPhaseInPlace();
                return;
            }
            List<StageLibraryFloorModel> availableFloorList = Singleton<StageController>.Instance.GetStageModel().GetAvailableFloorList();
            // Match the vanilla Purple Tear phase transition: move to a different,
            // supported floor. Keeping only CurrentFloor makes ChangeFloorForcely a
            // no-op and leaves the reception stuck between phases.
            availableFloorList.RemoveAll(x => x.Sephirah == SephirahType.Chesed);
            availableFloorList.RemoveAll(x => x.Sephirah == SephirahType.Hokma);
            availableFloorList.RemoveAll(x => x.Sephirah == Singleton<StageController>.Instance.CurrentFloor);
            if (availableFloorList.Count == 0)
            {
                UnityEngine.Debug.LogError("[RMR] Purple Tear phase transition deferred: no valid destination floor.");
                return;
            }

            SephirahType destination = RandomUtil.SelectOne(availableFloorList).Sephirah;
            // Commit the one-time phase state only after a real destination exists.
            // ChangeFloorForcely invokes battle lifecycle hooks synchronously, so the
            // purple exception flag must be visible for the duration of that call.
            LogLikeMod.purpleexcept = true;
            this.owner.UnitData.floorBattleData.param2 = 1;
            try
            {
                Singleton<StageController>.Instance.ChangeFloorForcely(destination, this.owner);
            }
            catch (Exception ex)
            {
                // A failed transition must remain retryable. Leaving param2=1 makes the
                // first form killable and turns the missing second phase into a false victory.
                LogLikeMod.purpleexcept = false;
                this.owner.UnitData.floorBattleData.param2 = 0;
                this._teleported = 0;
                UnityEngine.Debug.LogError("[RMR] Purple Tear phase transition failed and was rolled back: " + ex);
            }
        }

        private void EnterSecondPhaseInPlace()
        {
            // RMR deliberately has one reception floor. Consuming a second floor makes
            // party wipe wait for nonexistent librarians; calling ChangeFloorForcely on
            // the current floor is a no-op. Commit Purple Tear's phase-two state in place
            // and let OnRoundStartAfter rebuild stance/cards normally.
            LogLikeMod.purpleexcept = false;
            this.owner.UnitData.floorBattleData.param2 = 1;
            this._teleported = 1;
            this._areaCoolTime = 1;
            this._teleportReady = false;
            UnityEngine.Debug.Log("[RMR] Purple Tear entered phase two in place (single-floor reception).");
        }

        public override void OnRoundStartAfter()
        {
            --this._stanceCooltime;
            if (this._stanceCooltime <= 0)
                this.UpdateStance();
            this.SetCards();
            ++this._patternCount;
            this.owner.UnitData.floorBattleData.param1 = this._patternCount;
        }

        public void SetCards()
        {
            this.owner.allyCardDetail.ExhaustAllCards();
            if (this.owner.UnitData.floorBattleData.param2 > 0)
            {
                if (this._areaCoolTime <= 0)
                {
                    this.AddNewCard(609013);
                    this._areaCoolTime = 2;
                }
                else
                    --this._areaCoolTime;
            }
            switch (this._stancePassive.CurrentStance)
            {
                case PurpleStance.Slash:
                    this.SetCards_slash();
                    break;
                case PurpleStance.Penetrate:
                    this.SetCards_penetrate();
                    break;
                case PurpleStance.Hit:
                    this.SetCards_hit();
                    break;
                case PurpleStance.Defense:
                    this.SetCards_defense();
                    break;
            }
            ++this._patternCount;
        }

        public void SetCards_slash()
        {
            this.AddNewCard(609001);
            this.AddNewCard(609002);
            this.AddNewCard(609003);
            this.AddNewCard(609001);
            this.AddNewCard(609002);
            this.AddNewCard(609003);
        }

        public void SetCards_penetrate()
        {
            this.AddNewCard(609004);
            this.AddNewCard(609005);
            this.AddNewCard(609006);
            this.AddNewCard(609004);
            this.AddNewCard(609005);
            this.AddNewCard(609006);
        }

        public void SetCards_hit()
        {
            this.AddNewCard(609007);
            this.AddNewCard(609008);
            this.AddNewCard(609009);
            this.AddNewCard(609007);
            this.AddNewCard(609008);
            this.AddNewCard(609009);
        }

        public void SetCards_defense()
        {
            this.AddNewCard(609010);
            this.AddNewCard(609011);
            this.AddNewCard(609012);
            this.AddNewCard(609010);
            this.AddNewCard(609011);
        }
        #endregion

        #region --- UI show / hide / build ---


        public void UpdateStance()
        {
            if (this._alreayUsed.Count >= 4)
                this._alreayUsed.Clear();
            int num = this.owner.UnitData.floorBattleData.param2;
            if (num > 0 && !this._alreayUsed.Contains(PurpleStance.Defense))
                this._alreayUsed.Add(PurpleStance.Defense);
            List<PurpleStance> list = new List<PurpleStance>()
            {
              PurpleStance.Slash,
              PurpleStance.Penetrate,
              PurpleStance.Hit,
              PurpleStance.Defense
            };
            foreach (PurpleStance purpleStance in this._alreayUsed)
                list.Remove(purpleStance);
            switch (RandomUtil.SelectOne<PurpleStance>(list))
            {
                case PurpleStance.Slash:
                    this._stancePassive.ChangeStance_slash();
                    this.owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.SlashPowerUp, 1);
                    this._stanceCooltime = num <= 0 ? 2 : 1;
                    this._alreayUsed.Add(PurpleStance.Slash);
                    break;
                case PurpleStance.Penetrate:
                    this._stancePassive.ChangeStance_penetrate();
                    this.owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.PenetratePowerUp, 1);
                    this._stanceCooltime = num <= 0 ? 2 : 1;
                    this._alreayUsed.Add(PurpleStance.Penetrate);
                    break;
                case PurpleStance.Hit:
                    this._stancePassive.ChangeStance_hit();
                    this.owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.HitPowerUp, 1);
                    this._stanceCooltime = num <= 0 ? 2 : 1;
                    this._alreayUsed.Add(PurpleStance.Hit);
                    break;
                case PurpleStance.Defense:
                    this._stancePassive.ChangeStance_defense();
                    this.owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.DefensePowerUp, 1);
                    this._stanceCooltime = 1;
                    this._alreayUsed.Add(PurpleStance.Defense);
                    break;
            }
        }
        #endregion

        #region --- Battle hooks ---


        public int AddNewCard(int id)
        {
            BattleDiceCardModel battleDiceCardModel = this.owner.allyCardDetail.AddTempCard(new LorId(LogLikeMod.ModId, id));
            return battleDiceCardModel != null ? battleDiceCardModel.GetOriginCost() : 1;
        }
        #endregion

        #region --- Other helpers ---


        public override bool BeforeTakeDamage(BattleUnitModel attacker, int dmg)
        {
            this._dmgReduction = 0;
            if (this.owner.UnitData.floorBattleData.param2 <= 0 && ((double)this.owner.hp <= (double)this._teleportCondition || (double)this.owner.hp - (double)dmg <= (double)this._teleportCondition))
            {
                this._dmgReduction = (int)((double)this._teleportCondition - ((double)this.owner.hp - (double)dmg));
                this._teleportReady = true;
            }
            return base.BeforeTakeDamage(attacker, dmg);
        }
        #endregion

        #region --- Battle hooks ---


        public override int GetDamageReductionAll()
        {
            int damageReductionAll;
            if (this.owner.UnitData.floorBattleData.param2 <= 0 && (double)this.owner.hp <= (double)this._teleportCondition)
            {
                damageReductionAll = 9999;
                this._teleportReady = true;
            }
            else
                damageReductionAll = this._dmgReduction;
            return damageReductionAll;
        }

        public override void OnBattleEnd_alive()
        {
            bool teleportReady = this._teleportReady;
        }
        #endregion

    }
}
