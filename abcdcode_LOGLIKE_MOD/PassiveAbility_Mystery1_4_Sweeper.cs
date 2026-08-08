// -----------------------------------------------------------------------------
// Passive ability script: PassiveAbility_Mystery1_4_Sweeper
// Namespace/file: ruina-roguelike-reborn-main\abcdcode_LOGLIKE_MOD\PassiveAbility_Mystery1_4_Sweeper.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
namespace abcdcode_LOGLIKE_MOD
{

    /// <summary>Passive ability: PassiveAbility_Mystery1_4_Sweeper</summary>

    public class PassiveAbility_Mystery1_4_Sweeper : PassiveAbilityBase
    {
        public int round;
        private bool _ended;

        public override string debugDesc => "3막 후 뒷골목의 밤이 끝남";

        public override void OnWaveStart()
        {
            base.OnWaveStart();
            this.round = 0;
            this._ended = false;
        }

        public override void OnRoundStart()
        {
            base.OnRoundStart();
            if (this._ended)
                return;
            ++this.round;
            if (this.round <= 3)
                return;
            this._ended = true;
            // Timed Normal combat win — clear enemies for victory confirmation, but do NOT
            // MarkNonCombatNodeExit (that sticky flag skips battle rewards and can race a
            // second EndBattle into vanilla FinalEnd / 舞台落幕 before next-stage pick).
            try { RewardingModel.MarkForcedTimedCombatVictory("MysterySweeperTimer"); }
            catch (System.Exception ex) { UnityEngine.Debug.LogWarning("[RMR] MysterySweeperTimer victory marker: " + ex.Message); }
            Singleton<StageController>.Instance.GetStageModel().GetWave(Singleton<StageController>.Instance.CurrentWave).Defeat();
            StageController sc = Singleton<StageController>.Instance;
            // Enemy Die() may already have entered EndBattlePhase; a second EndBattle would
            // hit Phase==EndBattle → orig → FinalEnd with no next wave yet.
            if (sc != null && sc.Phase != StageController.StagePhase.EndBattle)
                sc.EndBattle();
        }
    }
}
