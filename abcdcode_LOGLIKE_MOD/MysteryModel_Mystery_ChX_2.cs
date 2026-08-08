// -----------------------------------------------------------------------------
// Mystery / event node model: MysteryModel_Mystery_ChX_2
// Namespace/file: ruina-roguelike-reborn-main\abcdcode_LOGLIKE_MOD\MysteryModel_Mystery_ChX_2.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
namespace abcdcode_LOGLIKE_MOD
{

    /// <summary>Mystery node model: MysteryModel_Mystery_ChX_2</summary>

    public class MysteryModel_Mystery_ChX_2 : MysteryBase
    {
        public override void OnClickChoice(int choiceid)
        {
            MysteryBase.SetNextStageCustom(new LorId(LogLikeMod.ModId, 855), StageType.Rest);
            base.OnClickChoice(choiceid);
        }
    }
}
