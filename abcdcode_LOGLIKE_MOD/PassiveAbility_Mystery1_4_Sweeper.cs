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

        public override string debugDesc => "3막 후 뒷골목의 밤이 끝남";

        public override void OnRoundStart()
        {
            base.OnRoundStart();
            ++this.round;
            if (this.round <= 3)
                return;
            try { RewardingModel.MarkNonCombatNodeExit("MysterySweeperTimer"); }
            catch (System.Exception ex) { UnityEngine.Debug.LogWarning("[RMR] MysterySweeperTimer exit marker: " + ex.Message); }
            Singleton<StageController>.Instance.GetStageModel().GetWave(Singleton<StageController>.Instance.CurrentWave).Defeat();
            Singleton<StageController>.Instance.EndBattle();
        }
    }
}
