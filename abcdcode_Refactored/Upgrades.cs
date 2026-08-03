// -----------------------------------------------------------------------------
// Refactored LOGLIKE/RMR logic: Upgrades
// Namespace/file: ruina-roguelike-reborn-main\abcdcode_Refactored\Upgrades.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
using LOR_DiceSystem;

namespace abcdcode_LOGLIKE_MOD
{
    #region --- UpgradeModel_awl1_1 ---

    /// <summary>Card upgrade model: UpgradeModel_awl1_1</summary>
    public class UpgradeModel_awl1_1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 0, 2);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetDice(2, 0, 2);
            this.baseid = new LorId(LogLikeMod.ModId, 612002);
        }
    }
    #endregion

    #region --- UpgradeModel_awl1 ---



    /// <summary>Card upgrade model: UpgradeModel_awl1</summary>


    public class UpgradeModel_awl1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 0, 2);
            this.baseid = new LorId(LogLikeMod.ModId, 612001);
        }
    }
    #endregion

    #region --- UpgradeModel_awl3 ---



    /// <summary>Card upgrade model: UpgradeModel_awl3</summary>


    public class UpgradeModel_awl3 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 0, 1);
            this.upgradeinfo.SetDice(1, 0, 1);
            this.upgradeinfo.SetDice(2, 0, 2);
            this.baseid = new LorId(LogLikeMod.ModId, 612003);
        }
    }
    #endregion

    #region --- UpgradeModel_awl4 ---



    /// <summary>Card upgrade model: UpgradeModel_awl4</summary>


    public class UpgradeModel_awl4 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetDice(2, 0, 2);
            this.upgradeinfo.SetAbility(2, "binding2atk");
            this.baseid = new LorId(LogLikeMod.ModId, 612004);
        }
    }
    #endregion

    #region --- UpgradeModel_awl5 ---



    /// <summary>Card upgrade model: UpgradeModel_awl5</summary>


    public class UpgradeModel_awl5 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 0, 3);
            this.upgradeinfo.SetAbility(1, "binding2pwLog");
            this.baseid = new LorId(LogLikeMod.ModId, 612005);
        }
    }
    #endregion

    #region --- UpgradeModel_awl6 ---



    /// <summary>Card upgrade model: UpgradeModel_awl6</summary>


    public class UpgradeModel_awl6 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetSelfAbility("awlofnight1Log");
            this.baseid = new LorId(LogLikeMod.ModId, 612006);
        }
    }
    #endregion

    #region --- UpgradeModel_bayyard1 ---



    /// <summary>Card upgrade model: UpgradeModel_bayyard1</summary>


    public class UpgradeModel_bayyard1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 0, 2);
            this.upgradeinfo.SetDice(1, 0, 2);
            this.baseid = new LorId(LogLikeMod.ModId, 616001);
        }
    }
    #endregion

    #region --- UpgradeModel_bayyard2_1 ---



    /// <summary>Card upgrade model: UpgradeModel_bayyard2_1</summary>


    public class UpgradeModel_bayyard2_1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 0, 2);
            this.upgradeinfo.SetDice(1, 0, 2);
            this.upgradeinfo.SetDice(2, 2, 0);
            this.upgradeinfo.SetSelfAbility("DontMoveDrawLog");
            this.baseid = new LorId(LogLikeMod.ModId, 616005);
        }
    }
    #endregion

    #region --- UpgradeModel_bayyard2 ---



    /// <summary>Card upgrade model: UpgradeModel_bayyard2</summary>


    public class UpgradeModel_bayyard2 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 0);
            this.upgradeinfo.SetDice(1, 0, 2);
            this.baseid = new LorId(LogLikeMod.ModId, 616002);
        }
    }
    #endregion

    #region --- UpgradeModel_bayyard3_1 ---



    /// <summary>Card upgrade model: UpgradeModel_bayyard3_1</summary>


    public class UpgradeModel_bayyard3_1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 0, 2);
            this.upgradeinfo.SetDice(1, 2, 0);
            this.upgradeinfo.SetDice(2, 0, 2);
            this.upgradeinfo.SetSelfAbility("DontMoveDrawLog");
            this.baseid = new LorId(LogLikeMod.ModId, 616004);
        }
    }
    #endregion

    #region --- UpgradeModel_bayyard3 ---



    /// <summary>Card upgrade model: UpgradeModel_bayyard3</summary>


    public class UpgradeModel_bayyard3 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 0);
            this.upgradeinfo.SetDice(1, 0, 2);
            this.baseid = new LorId(LogLikeMod.ModId, 616003);
        }
    }
    #endregion

    #region --- UpgradeModel_bayyard4_1 ---



    /// <summary>Card upgrade model: UpgradeModel_bayyard4_1</summary>


    public class UpgradeModel_bayyard4_1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 2);
            this.upgradeinfo.SetDice(1, 3, 6);
            this.baseid = new LorId(LogLikeMod.ModId, 616007);
        }
    }
    #endregion

    #region --- UpgradeModel_bayyard4 ---



    /// <summary>Card upgrade model: UpgradeModel_bayyard4</summary>


    public class UpgradeModel_bayyard4 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetSelfAbility("selfstunwithabilLog");
            this.baseid = new LorId(LogLikeMod.ModId, 616006);
        }
    }
    #endregion






    // Hard Rehearsal
    #region --- UpgradeModel_Bremen3 ---

    /// <summary>Card upgrade model: UpgradeModel_Bremen3</summary>
    public class UpgradeModel_Bremen3 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 1);
            this.upgradeinfo.SetDice(1, 1, 0);
            this.upgradeinfo.SetDice(2, 1, 0);
            this.baseid = new LorId(LogLikeMod.ModId, 407003);
        }
    }
    #endregion


    // Tendon Chords
    #region --- UpgradeModel_Bremen6 ---

    /// <summary>Card upgrade model: UpgradeModel_Bremen6</summary>
    public class UpgradeModel_Bremen6 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetCost(1);
            this.baseid = new LorId(LogLikeMod.ModId, 407006);
        }
    }
    #endregion

    #region --- UpgradeModel_Cane1 ---




    /// <summary>Card upgrade model: UpgradeModel_Cane1</summary>



    public class UpgradeModel_Cane1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 4);
            this.baseid = new LorId(LogLikeMod.ModId, 603001);
        }
    }
    #endregion

    #region --- UpgradeModel_Cane2 ---



    /// <summary>Card upgrade model: UpgradeModel_Cane2</summary>


    public class UpgradeModel_Cane2 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetSelfAbility("warpCharge7Log");
            this.baseid = new LorId(LogLikeMod.ModId, 603002);
        }
    }
    #endregion

    #region --- UpgradeModel_Cane3 ---



    /// <summary>Card upgrade model: UpgradeModel_Cane3</summary>


    public class UpgradeModel_Cane3 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 3);
            this.upgradeinfo.SetDice(1, 1, 0);
            this.upgradeinfo.SetDice(2, 1, 0);
            this.upgradeinfo.SetSelfAbility("warpCharge6");
            this.baseid = new LorId(LogLikeMod.ModId, 603003);
        }
    }
    #endregion

    #region --- UpgradeModel_Cane4 ---



    /// <summary>Card upgrade model: UpgradeModel_Cane4</summary>


    public class UpgradeModel_Cane4 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 0, 1);
            this.upgradeinfo.SetDice(1, 0, 1);
            this.upgradeinfo.SetDice(2, 0, 1);
            this.upgradeinfo.SetSelfAbility("badaCardRemakeLog");
            this.baseid = new LorId(LogLikeMod.ModId, 603004);
        }
    }
    #endregion

    #region --- UpgradeModel_Cane5 ---



    /// <summary>Card upgrade model: UpgradeModel_Cane5</summary>


    public class UpgradeModel_Cane5 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 0, 1);
            this.upgradeinfo.SetDice(1, 1, 0);
            this.upgradeinfo.SetDice(2, 0, 2);
            this.baseid = new LorId(LogLikeMod.ModId, 603005);
        }
    }
    #endregion

    #region --- UpgradeModel_Cane6 ---



    /// <summary>Card upgrade model: UpgradeModel_Cane6</summary>


    public class UpgradeModel_Cane6 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetDice(2, 0, 1);
            this.upgradeinfo.SetSelfAbility("warpCharge3");
            this.baseid = new LorId(LogLikeMod.ModId, 603006);
        }
    }
    #endregion

    #region --- UpgradeModel_Cane7 ---



    /// <summary>Card upgrade model: UpgradeModel_Cane7</summary>


    public class UpgradeModel_Cane7 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 2);
            this.upgradeinfo.SetDice(1, 1, 0);
            this.upgradeinfo.SetSelfAbility("chargeEnergyLog");
            this.baseid = new LorId(LogLikeMod.ModId, 603007);
        }
    }
    #endregion

    #region --- UpgradeModel_Cane8 ---



    /// <summary>Card upgrade model: UpgradeModel_Cane8</summary>


    public class UpgradeModel_Cane8 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 3);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetAbility(0, "breakChargeRemakeLog");
            this.baseid = new LorId(LogLikeMod.ModId, 603008);
        }
    }
    #endregion

    #region --- UpgradeModel_ch1_1 ---



    /// <summary>Card upgrade model: UpgradeModel_ch1_1</summary>


    public class UpgradeModel_ch1_1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 2);
            this.upgradeinfo.SetAbility(0, "RMR_RecycleOnClashLose");
            this.upgradeinfo.SetDice(1, 0, 2);
            this.baseid = new LorId(LogLikeMod.ModId, 100001);
        }
    }
    #endregion

    #region --- UpgradeModel_ch1_4 ---


    /// <summary>Card upgrade model: UpgradeModel_ch1_4</summary>

    public class UpgradeModel_ch1_4 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetSelfAbility("powerup2highspeed");
            this.upgradeinfo.SetDice(0, 0, 1);
            this.upgradeinfo.SetDice(1, 2, 0);
            this.baseid = new LorId(LogLikeMod.ModId, 100004);
        }
    }
    #endregion

    #region --- UpgradeModel_ch1_5 ---



    /// <summary>Card upgrade model: UpgradeModel_ch1_5</summary>


    public class UpgradeModel_ch1_5 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetCost(1);
            this.upgradeinfo.SetSelfAbility("quickness2");
            this.baseid = new LorId(LogLikeMod.ModId, 100005);
        }
    }
    #endregion

    #region --- UpgradeModel_ch2_1 ---



    /// <summary>Card upgrade model: UpgradeModel_ch2_1</summary>


    public class UpgradeModel_ch2_1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 3, 0);
            this.baseid = new LorId(LogLikeMod.ModId, 200001);
        }
    }
    #endregion

    #region --- UpgradeModel_ch2_4 ---



    /// <summary>Card upgrade model: UpgradeModel_ch2_4</summary>


    public class UpgradeModel_ch2_4 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.baseid = new LorId(LogLikeMod.ModId, 200004);
        }
    }
    #endregion

    #region --- UpgradeModel_Gear1 ---












    /// <summary>Card upgrade model: UpgradeModel_Gear1</summary>











    public class UpgradeModel_Gear1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 2);
            this.upgradeinfo.SetAbility(0, "RMR_Smoke4Atk");
            this.baseid = new LorId(LogLikeMod.ModId, 604001);
        }
    }
    #endregion

    #region --- UpgradeModel_Gear2 ---



    /// <summary>Card upgrade model: UpgradeModel_Gear2</summary>


    public class UpgradeModel_Gear2 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 0);
            this.upgradeinfo.SetDice(1, 0, 2);
            this.upgradeinfo.SetSelfAbility("RMR_Gain5Smoke");
            this.baseid = new LorId(LogLikeMod.ModId, 604002);
        }
    }
    #endregion

    #region --- UpgradeModel_Gear3 ---



    /// <summary>Card upgrade model: UpgradeModel_Gear3</summary>


    public class UpgradeModel_Gear3 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetSelfAbility("RMR_Gain3Smoke");
            this.upgradeinfo.SetAbility(0, "RMR_Smoke3Atk");
            this.baseid = new LorId(LogLikeMod.ModId, 604003);
        }
    }
    #endregion

    #region --- UpgradeModel_Gear4 ---



    /// <summary>Card upgrade model: UpgradeModel_Gear4</summary>


    public class UpgradeModel_Gear4 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 0, 2);
            this.upgradeinfo.SetDice(1, 1, 0);
            this.upgradeinfo.SetDice(2, 1, 0);
            this.upgradeinfo.SetSelfAbility("RMR_AssaultOrderUpgrade");
            this.baseid = new LorId(LogLikeMod.ModId, 604004);
        }
    }
    #endregion

    #region --- UpgradeModel_Gear5 ---



    /// <summary>Card upgrade model: UpgradeModel_Gear5</summary>


    public class UpgradeModel_Gear5 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetSelfAbility("RMR_GuidanceGearsUpgrade");
            this.baseid = new LorId(LogLikeMod.ModId, 604005);
        }
    }
    #endregion


    //Overpower
    #region --- UpgradeModel_Hook2 ---

    /// <summary>Card upgrade model: UpgradeModel_Hook2</summary>
    public class UpgradeModel_Hook2 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.baseid = new LorId(LogLikeMod.ModId, 104001);
        }
    }
    #endregion


    //Mutilate
    #region --- UpgradeModel_Hook4 ---

    /// <summary>Card upgrade model: UpgradeModel_Hook4</summary>
    public class UpgradeModel_Hook4 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.AddDice(1, 5, BehaviourDetail.Slash, "recoverHp1atk", BehaviourType.Atk);
            this.baseid = new LorId(LogLikeMod.ModId, 104006);
        }
    }
    #endregion

    #region --- UpgradeModel_IndexSecond1 ---



    /// <summary>Card upgrade model: UpgradeModel_IndexSecond1</summary>


    public class UpgradeModel_IndexSecond1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 3, 2);
            this.upgradeinfo.SetAbility(0, "gloriaDice");
            this.baseid = new LorId(LogLikeMod.ModId, 605001);
        }
    }
    #endregion

    #region --- UpgradeModel_IndexSecond10 ---



    /// <summary>Card upgrade model: UpgradeModel_IndexSecond10</summary>


    public class UpgradeModel_IndexSecond10 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 0, 1);
            this.upgradeinfo.AddDice(1, 4, BehaviourDetail.Hit, "", MotionDetail.H, BehaviourType.Atk, "IndexUnit_H", "");
            this.baseid = new LorId(LogLikeMod.ModId, 605010);
        }
    }
    #endregion

    #region --- UpgradeModel_IndexSecond2 ---



    /// <summary>Card upgrade model: UpgradeModel_IndexSecond2</summary>


    public class UpgradeModel_IndexSecond2 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 0);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetSelfAbility("estherCardLog");
            this.upgradeinfo.SetAbility(0, "estherDiceLog");
            this.baseid = new LorId(LogLikeMod.ModId, 605002);
        }
    }
    #endregion

    #region --- UpgradeModel_IndexSecond3 ---



    /// <summary>Card upgrade model: UpgradeModel_IndexSecond3</summary>


    public class UpgradeModel_IndexSecond3 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 3);
            this.upgradeinfo.SetAbility(0, "hubertDiceLog");
            this.baseid = new LorId(LogLikeMod.ModId, 605003);
        }
    }
    #endregion

    #region --- UpgradeModel_IndexSecond4 ---



    /// <summary>Card upgrade model: UpgradeModel_IndexSecond4</summary>


    public class UpgradeModel_IndexSecond4 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 0);
            this.upgradeinfo.SetDice(2, 0, 1);
            this.upgradeinfo.SetAbility(1, "powerUpDice6highlanderLog");
            this.baseid = new LorId(LogLikeMod.ModId, 605004);
        }
    }
    #endregion

    #region --- UpgradeModel_IndexSecond5 ---



    /// <summary>Card upgrade model: UpgradeModel_IndexSecond5</summary>


    public class UpgradeModel_IndexSecond5 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetAbility(0, "powerUp2targetHp50Log");
            this.upgradeinfo.SetAbility(1, "powerUp2targetHp50Log");
            this.baseid = new LorId(LogLikeMod.ModId, 605005);
        }
    }
    #endregion

    #region --- UpgradeModel_IndexSecond6 ---



    /// <summary>Card upgrade model: UpgradeModel_IndexSecond6</summary>


    public class UpgradeModel_IndexSecond6 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 0);
            this.upgradeinfo.SetDice(1, 1, 0);
            this.upgradeinfo.SetSelfAbility("cardPowerDown2targetHighlanderLog");
            this.baseid = new LorId(LogLikeMod.ModId, 605006);
        }
    }
    #endregion

    #region --- UpgradeModel_IndexSecond7 ---



    /// <summary>Card upgrade model: UpgradeModel_IndexSecond7</summary>


    public class UpgradeModel_IndexSecond7 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 2, 1);
            this.baseid = new LorId(LogLikeMod.ModId, 605007);
        }
    }
    #endregion

    #region --- UpgradeModel_IndexSecond8 ---



    /// <summary>Card upgrade model: UpgradeModel_IndexSecond8</summary>


    public class UpgradeModel_IndexSecond8 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetSelfAbility("indexRelease1Log");
            this.baseid = new LorId(LogLikeMod.ModId, 605008);
        }
    }
    #endregion

    #region --- UpgradeModel_IndexSecond9 ---



    /// <summary>Card upgrade model: UpgradeModel_IndexSecond9</summary>


    public class UpgradeModel_IndexSecond9 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetSelfAbility("indexRelease2Log");
            this.baseid = new LorId(LogLikeMod.ModId, 605009);
        }
    }
    #endregion

    #region --- UpgradeModel_Liu1_1 ---













    /// <summary>Card upgrade model: UpgradeModel_Liu1_1</summary>












    public class UpgradeModel_Liu1_1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 2, 0);
            this.baseid = new LorId(LogLikeMod.ModId, 606001);
        }
    }
    #endregion

    #region --- UpgradeModel_Liu1_2 ---



    /// <summary>Card upgrade model: UpgradeModel_Liu1_2</summary>


    public class UpgradeModel_Liu1_2 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 0);
            this.upgradeinfo.SetDice(1, 2, 2);
            this.upgradeinfo.SetAbility(1, "burn8atkLog");
            this.baseid = new LorId(LogLikeMod.ModId, 606002);
        }
    }
    #endregion

    #region --- UpgradeModel_Liu1_3 ---



    /// <summary>Card upgrade model: UpgradeModel_Liu1_3</summary>


    public class UpgradeModel_Liu1_3 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetCost(1);
            this.baseid = new LorId(LogLikeMod.ModId, 606003);
        }
    }
    #endregion

    #region --- UpgradeModel_Liu1_4 ---



    /// <summary>Card upgrade model: UpgradeModel_Liu1_4</summary>


    public class UpgradeModel_Liu1_4 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 0, 1);
            this.upgradeinfo.SetDice(1, 0, 1);
            this.upgradeinfo.SetDice(2, 1, 1);
            this.upgradeinfo.SetAbility(0, "burn1pwLog");
            this.baseid = new LorId(LogLikeMod.ModId, 606004);
        }
    }
    #endregion

    #region --- UpgradeModel_Liu1_5 ---



    /// <summary>Card upgrade model: UpgradeModel_Liu1_5</summary>


    public class UpgradeModel_Liu1_5 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 0, 1);
            this.upgradeinfo.SetAbility(1, "burn1atk");
            this.baseid = new LorId(LogLikeMod.ModId, 606005);
        }
    }
    #endregion

    #region --- UpgradeModel_Liu1_6 ---



    /// <summary>Card upgrade model: UpgradeModel_Liu1_6</summary>


    public class UpgradeModel_Liu1_6 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetAbility(0, "burn2atk");
            this.baseid = new LorId(LogLikeMod.ModId, 606006);
        }
    }
    #endregion

    #region --- UpgradeModel_Liu1_7 ---



    /// <summary>Card upgrade model: UpgradeModel_Liu1_7</summary>


    public class UpgradeModel_Liu1_7 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 0);
            this.upgradeinfo.SetAbility(0, "burn2atk");
            this.baseid = new LorId(LogLikeMod.ModId, 606007);
        }
    }
    #endregion

    #region --- UpgradeModel_Liu2_1 ---



    /// <summary>Card upgrade model: UpgradeModel_Liu2_1</summary>


    public class UpgradeModel_Liu2_1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 0, 2);
            this.upgradeinfo.SetDice(1, 1, 0);
            this.upgradeinfo.SetDice(2, 1, 0);
            this.upgradeinfo.SetDice(3, 0, 1);
            this.baseid = new LorId(LogLikeMod.ModId, 601001);
        }
    }
    #endregion

    #region --- UpgradeModel_Liu2_10 ---



    /// <summary>Card upgrade model: UpgradeModel_Liu2_10</summary>


    public class UpgradeModel_Liu2_10 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 2);
            this.upgradeinfo.SetDice(1, 0, 2);
            this.baseid = new LorId(LogLikeMod.ModId, 601010);
        }
    }
    #endregion

    #region --- UpgradeModel_Liu2_11 ---



    /// <summary>Card upgrade model: UpgradeModel_Liu2_11</summary>


    public class UpgradeModel_Liu2_11 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 2);
            this.baseid = new LorId(LogLikeMod.ModId, 601011);
        }
    }
    #endregion

    #region --- UpgradeModel_Liu2_12 ---



    /// <summary>Card upgrade model: UpgradeModel_Liu2_12</summary>


    public class UpgradeModel_Liu2_12 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetDice(2, 1, 1);
            this.baseid = new LorId(LogLikeMod.ModId, 601012);
        }
    }
    #endregion

    #region --- UpgradeModel_Liu2_13 ---



    /// <summary>Card upgrade model: UpgradeModel_Liu2_13</summary>


    public class UpgradeModel_Liu2_13 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 0);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetSelfAbility("gainPositive2Log");
            this.baseid = new LorId(LogLikeMod.ModId, 601013);
        }
    }
    #endregion

    #region --- UpgradeModel_Liu2_2 ---



    /// <summary>Card upgrade model: UpgradeModel_Liu2_2</summary>


    public class UpgradeModel_Liu2_2 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 0);
            this.upgradeinfo.SetDice(1, 1, 0);
            this.upgradeinfo.SetSelfAbility("gainPositive2Log");
            this.baseid = new LorId(LogLikeMod.ModId, 601002);
        }
    }
    #endregion

    #region --- UpgradeModel_Liu2_3 ---



    /// <summary>Card upgrade model: UpgradeModel_Liu2_3</summary>


    public class UpgradeModel_Liu2_3 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetCost(1);
            this.baseid = new LorId(LogLikeMod.ModId, 601003);
        }
    }
    #endregion

    #region --- UpgradeModel_Liu2_4 ---



    /// <summary>Card upgrade model: UpgradeModel_Liu2_4</summary>


    public class UpgradeModel_Liu2_4 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 2);
            this.upgradeinfo.SetDice(1, 0, 3);
            this.upgradeinfo.SetDice(2, 0, 0);
            this.baseid = new LorId(LogLikeMod.ModId, 601004);
        }
    }
    #endregion

    #region --- UpgradeModel_Liu2_5 ---



    /// <summary>Card upgrade model: UpgradeModel_Liu2_5</summary>


    public class UpgradeModel_Liu2_5 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 0, 2);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetDice(2, 1, 1);
            this.upgradeinfo.SetAbility(1, "quickness2atkLog");
            this.baseid = new LorId(LogLikeMod.ModId, 601005);
        }
    }
    #endregion

    #region --- UpgradeModel_Liu2_6 ---



    /// <summary>Card upgrade model: UpgradeModel_Liu2_6</summary>


    public class UpgradeModel_Liu2_6 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.baseid = new LorId(LogLikeMod.ModId, 601006);
        }
    }
    #endregion

    #region --- UpgradeModel_Liu2_7 ---



    /// <summary>Card upgrade model: UpgradeModel_Liu2_7</summary>


    public class UpgradeModel_Liu2_7 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 0);
            this.upgradeinfo.SetDice(1, 2, 2);
            this.upgradeinfo.SetAbility(0, "burn1atk");
            this.baseid = new LorId(LogLikeMod.ModId, 601007);
        }
    }
    #endregion

    #region --- UpgradeModel_Liu2_8_1 ---



    /// <summary>Card upgrade model: UpgradeModel_Liu2_8_1</summary>


    public class UpgradeModel_Liu2_8_1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 0);
            this.baseid = new LorId(LogLikeMod.ModId, 601014);
        }
    }
    #endregion

    #region --- UpgradeModel_Liu2_8 ---



    /// <summary>Card upgrade model: UpgradeModel_Liu2_8</summary>


    public class UpgradeModel_Liu2_8 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 0);
            this.upgradeinfo.SetSelfAbility("lowelonlyLog");
            this.baseid = new LorId(LogLikeMod.ModId, 601008);
        }
    }
    #endregion

    #region --- UpgradeModel_Liu2_9 ---



    /// <summary>Card upgrade model: UpgradeModel_Liu2_9</summary>


    public class UpgradeModel_Liu2_9 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 2);
            this.upgradeinfo.SetDice(2, 1, 0);
            this.upgradeinfo.SetAbility(1, "liuBurnDiceLog");
            this.baseid = new LorId(LogLikeMod.ModId, 601009);
        }
    }
    #endregion

    #region --- UpgradeModel_LogicSpecial ---



    /// <summary>Card upgrade model: UpgradeModel_LogicSpecial</summary>


    public class UpgradeModel_LogicSpecial : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(0, 2, 1);
            this.upgradeinfo.SetDice(0, 1, 1);
            this.baseid = new LorId(LogLikeMod.ModId, 706108);
        }
    }
    #endregion

    #region --- UpgradeModel_mirae1 ---



    /// <summary>Card upgrade model: UpgradeModel_mirae1</summary>


    public class UpgradeModel_mirae1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.baseid = new LorId(LogLikeMod.ModId, 614001);
        }
    }
    #endregion

    #region --- UpgradeModel_mirae2 ---



    /// <summary>Card upgrade model: UpgradeModel_mirae2</summary>


    public class UpgradeModel_mirae2 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetSelfAbility("plutoCardLog");
            this.baseid = new LorId(LogLikeMod.ModId, 614002);
        }
    }
    #endregion

    #region --- UpgradeModel_mirae3 ---



    /// <summary>Card upgrade model: UpgradeModel_mirae3</summary>


    public class UpgradeModel_mirae3 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 0, 2);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetDice(2, 1, 1);
            this.baseid = new LorId(LogLikeMod.ModId, 614003);
        }
    }
    #endregion

    #region --- UpgradeModel_mirae4 ---



    /// <summary>Card upgrade model: UpgradeModel_mirae4</summary>


    public class UpgradeModel_mirae4 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 0, 2);
            this.upgradeinfo.SetSelfAbility("discardAllEnergy1Log");
            this.baseid = new LorId(LogLikeMod.ModId, 614004);
        }
    }
    #endregion

    #region --- UpgradeModel_mirae5 ---



    /// <summary>Card upgrade model: UpgradeModel_mirae5</summary>


    public class UpgradeModel_mirae5 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(1, 0, 2);
            this.upgradeinfo.SetSelfAbility("handCountpowerUpLog");
            this.baseid = new LorId(LogLikeMod.ModId, 614005);
        }
    }
    #endregion

    #region --- UpgradeModel_mirae6 ---



    /// <summary>Card upgrade model: UpgradeModel_mirae6</summary>


    public class UpgradeModel_mirae6 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 2);
            this.upgradeinfo.SetAbility(0, "lifeInsuranceAreaAtkLog");
            this.baseid = new LorId(LogLikeMod.ModId, 614006);
        }
    }
    #endregion

    #region --- UpgradeModel_Molar5 ---









    /// <summary>Card upgrade model: UpgradeModel_Molar5</summary>








    public class UpgradeModel_Molar5 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 2);
            this.baseid = new LorId(LogLikeMod.ModId, 302002);
        }
    }
    #endregion

    #region --- UpgradeModel_Molar7 ---


    /// <summary>Card upgrade model: UpgradeModel_Molar7</summary>

    public class UpgradeModel_Molar7 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 0);
            this.upgradeinfo.SetDice(1, 1, 0);
            this.upgradeinfo.SetDice(2, 2, 1);
            this.baseid = new LorId(LogLikeMod.ModId, 302004);
        }
    }
    #endregion

    #region --- UpgradeModel_Pierr7 ---



    /// <summary>Card upgrade model: UpgradeModel_Pierr7</summary>


    public class UpgradeModel_Pierr7 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 0, 1);
            this.upgradeinfo.SetAbility(0, "paralysis1pw");
            this.baseid = new LorId(LogLikeMod.ModId, 201004);
        }
    }
    #endregion

    #region --- UpgradeModel_Purple1 ---



    

    /// <summary>Card upgrade model: UpgradeModel_Purple1</summary>


    

    public class UpgradeModel_Purple1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetCost(1);
            this.baseid = new LorId(LogLikeMod.ModId, 609001);
        }
    }
    #endregion

    #region --- UpgradeModel_Purple10 ---



    /// <summary>Card upgrade model: UpgradeModel_Purple10</summary>


    public class UpgradeModel_Purple10 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 0);
            this.upgradeinfo.SetDice(1, 1, 0);
            this.upgradeinfo.SetDice(2, 1, 1);
            this.upgradeinfo.AddDice(4, 8, BehaviourDetail.Guard, "", MotionDetail.G, BehaviourType.Standby, "ThePurpleTear", "");
            this.baseid = new LorId(LogLikeMod.ModId, 609010);
        }
    }
    #endregion

    #region --- UpgradeModel_Purple11 ---



    /// <summary>Card upgrade model: UpgradeModel_Purple11</summary>


    public class UpgradeModel_Purple11 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 0);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetAbility(1, "recoverBreak3atk");
            this.baseid = new LorId(LogLikeMod.ModId, 609011);
        }
    }
    #endregion

    #region --- UpgradeModel_Purple12 ---



    /// <summary>Card upgrade model: UpgradeModel_Purple12</summary>


    public class UpgradeModel_Purple12 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.AddDice(4, 9, BehaviourDetail.Slash, "", MotionDetail.H, BehaviourType.Atk, "ThePurpleTear_H", "");
            this.baseid = new LorId(LogLikeMod.ModId, 609012);
        }
    }
    #endregion

    #region --- UpgradeModel_Purple13 ---



    /// <summary>Card upgrade model: UpgradeModel_Purple13</summary>


    public class UpgradeModel_Purple13 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetDice(2, 1, 1);
            this.upgradeinfo.SetAbility(0, "purpleAreaDice1Log");
            this.upgradeinfo.SetAbility(1, "purpleAreaDice2Log");
            this.upgradeinfo.SetAbility(2, "purpleAreaDice3Log");
            this.baseid = new LorId(LogLikeMod.ModId, 609013);
        }
    }
    #endregion

    #region --- UpgradeModel_Purple2 ---



    /// <summary>Card upgrade model: UpgradeModel_Purple2</summary>


    public class UpgradeModel_Purple2 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 0, 5);
            this.upgradeinfo.SetDice(1, 1, 0);
            this.baseid = new LorId(LogLikeMod.ModId, 609002);
        }
    }
    #endregion

    #region --- UpgradeModel_Purple3 ---



    /// <summary>Card upgrade model: UpgradeModel_Purple3</summary>


    public class UpgradeModel_Purple3 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetDice(2, 0, 2);
            this.baseid = new LorId(LogLikeMod.ModId, 609003);
        }
    }
    #endregion

    #region --- UpgradeModel_Purple4 ---



    /// <summary>Card upgrade model: UpgradeModel_Purple4</summary>


    public class UpgradeModel_Purple4 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 0, 2);
            this.upgradeinfo.SetDice(1, 2, 0);
            this.upgradeinfo.SetAbility(0, "bleeding1atk");
            this.baseid = new LorId(LogLikeMod.ModId, 609004);
        }
    }
    #endregion

    #region --- UpgradeModel_Purple5 ---



    /// <summary>Card upgrade model: UpgradeModel_Purple5</summary>


    public class UpgradeModel_Purple5 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 0, 1);
            this.upgradeinfo.SetDice(1, 0, 1);
            this.upgradeinfo.SetDice(2, 1, 0);
            this.upgradeinfo.SetDice(3, 1, 1);
            this.baseid = new LorId(LogLikeMod.ModId, 609005);
        }
    }
    #endregion

    #region --- UpgradeModel_Purple6 ---



    /// <summary>Card upgrade model: UpgradeModel_Purple6</summary>


    public class UpgradeModel_Purple6 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 0, 2);
            this.upgradeinfo.SetDice(1, 0, 2);
            this.upgradeinfo.SetDice(2, 2, 0);
            this.baseid = new LorId(LogLikeMod.ModId, 609006);
        }
    }
    #endregion

    #region --- UpgradeModel_Purple7 ---



    /// <summary>Card upgrade model: UpgradeModel_Purple7</summary>


    public class UpgradeModel_Purple7 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 2);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetAbility(0, "recoverBreak5atk");
            this.baseid = new LorId(LogLikeMod.ModId, 609007);
        }
    }
    #endregion

    #region --- UpgradeModel_Purple8 ---



    /// <summary>Card upgrade model: UpgradeModel_Purple8</summary>


    public class UpgradeModel_Purple8 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetDice(2, 1, 1);
            this.upgradeinfo.SetAbility(1, "recoverBreak5atk");
            this.baseid = new LorId(LogLikeMod.ModId, 609008);
        }
    }
    #endregion

    #region --- UpgradeModel_Purple9 ---



    /// <summary>Card upgrade model: UpgradeModel_Purple9</summary>


    public class UpgradeModel_Purple9 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 0);
            this.upgradeinfo.SetDice(1, 1, 0);
            this.upgradeinfo.SetDice(2, 1, 0);
            this.upgradeinfo.SetDice(3, 1, 1);
            this.upgradeinfo.SetAbility(0, "powerDownNext3pwTargetLog");
            this.upgradeinfo.SetAbility(1, "powerDownNext3pwTargetLog");
            this.upgradeinfo.SetAbility(2, "powerDownNext3pwTargetLog");
            this.baseid = new LorId(LogLikeMod.ModId, 609009);
        }
    }
    #endregion

    #region --- UpgradeModel_Rats1 ---



    /// <summary>Card upgrade model: UpgradeModel_Rats1</summary>


    public class UpgradeModel_Rats1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetCost(1);
            this.upgradeinfo.SetAbility(1, "bleeding1atk");
            this.baseid = new LorId(LogLikeMod.ModId, 101004);
        }
    }
    #endregion

    #region --- UpgradeModel_Rats2 ---



    /// <summary>Card upgrade model: UpgradeModel_Rats2</summary>


    public class UpgradeModel_Rats2 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetSelfAbility("rhinoCard");
            this.upgradeinfo.SetAbility(1, "bleeding2atk");
            this.upgradeinfo.SetDice(0, 1, 0);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetDice(2, 2, 0);
            this.baseid = new LorId(LogLikeMod.ModId, 101002);
        }
    }
    #endregion

    #region --- UpgradeModel_Rats3 ---



    /// <summary>Card upgrade model: UpgradeModel_Rats3</summary>


    public class UpgradeModel_Rats3 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetAbility(0, "bleeding2atk");
            this.baseid = new LorId(LogLikeMod.ModId, 101003);
        }
    }
    #endregion

    #region --- UpgradeModel_Rcorp1_1 ---



    /// <summary>Card upgrade model: UpgradeModel_Rcorp1_1</summary>


    public class UpgradeModel_Rcorp1_1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetCost(1);
            this.baseid = new LorId(LogLikeMod.ModId, 608017);
        }
    }
    #endregion

    #region --- UpgradeModel_Rcorp1 ---



    /// <summary>Card upgrade model: UpgradeModel_Rcorp1</summary>


    public class UpgradeModel_Rcorp1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetDice(2, 1, 0);
            this.baseid = new LorId(LogLikeMod.ModId, 608001);
        }
    }
    #endregion

    #region --- UpgradeModel_Rcorp10 ---



    /// <summary>Card upgrade model: UpgradeModel_Rcorp10</summary>


    public class UpgradeModel_Rcorp10 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 0);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetDice(2, 1, 1);
            this.upgradeinfo.SetSelfAbility("deerCardLog");
            this.upgradeinfo.SetAbility(2, "deerDiceLog");
            this.baseid = new LorId(LogLikeMod.ModId, 608010);
        }
    }
    #endregion

    #region --- UpgradeModel_Rcorp11 ---



    /// <summary>Card upgrade model: UpgradeModel_Rcorp11</summary>


    public class UpgradeModel_Rcorp11 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 2, 2);
            this.upgradeinfo.SetSelfAbility("warpCharge6");
            this.baseid = new LorId(LogLikeMod.ModId, 608011);
        }
    }
    #endregion

    #region --- UpgradeModel_Rcorp12 ---



    /// <summary>Card upgrade model: UpgradeModel_Rcorp12</summary>


    public class UpgradeModel_Rcorp12 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetCost(1);
            this.upgradeinfo.SetDice(0, 1, 0);
            this.upgradeinfo.SetDice(1, 1, 0);
            this.upgradeinfo.SetDice(2, 1, 0);
            this.baseid = new LorId(LogLikeMod.ModId, 608012);
        }
    }
    #endregion

    #region --- UpgradeModel_Rcorp13 ---



    /// <summary>Card upgrade model: UpgradeModel_Rcorp13</summary>


    public class UpgradeModel_Rcorp13 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetDice(2, 2, 0);
            this.upgradeinfo.SetSelfAbility("warpCharge5");
            this.baseid = new LorId(LogLikeMod.ModId, 608013);
        }
    }
    #endregion

    #region --- UpgradeModel_Rcorp14 ---



    /// <summary>Card upgrade model: UpgradeModel_Rcorp14</summary>


    public class UpgradeModel_Rcorp14 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetSelfAbility("energyAndChargeLog");
            this.baseid = new LorId(LogLikeMod.ModId, 608014);
        }
    }
    #endregion

    #region --- UpgradeModel_Rcorp15 ---



    /// <summary>Card upgrade model: UpgradeModel_Rcorp15</summary>


    public class UpgradeModel_Rcorp15 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 0);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetDice(2, 1, 1);
            this.upgradeinfo.SetSelfAbility("warpCharge9Log");
            this.baseid = new LorId(LogLikeMod.ModId, 608015);
        }
    }
    #endregion

    #region --- UpgradeModel_Rcorp16 ---



    /// <summary>Card upgrade model: UpgradeModel_Rcorp16</summary>


    public class UpgradeModel_Rcorp16 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetCost(1);
            this.baseid = new LorId(LogLikeMod.ModId, 608016);
        }
    }
    #endregion

    #region --- UpgradeModel_Rcorp2 ---



    /// <summary>Card upgrade model: UpgradeModel_Rcorp2</summary>


    public class UpgradeModel_Rcorp2 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 0, 1);
            this.upgradeinfo.SetDice(1, 0, 1);
            this.upgradeinfo.SetDice(2, 1, 0);
            this.upgradeinfo.SetAbility(0, "rudolphDice");
            this.upgradeinfo.SetAbility(1, "rudolphDice");
            this.upgradeinfo.SetAbility(2, "rudolphDice");
            this.baseid = new LorId(LogLikeMod.ModId, 608002);
        }
    }
    #endregion

    #region --- UpgradeModel_Rcorp3 ---



    /// <summary>Card upgrade model: UpgradeModel_Rcorp3</summary>


    public class UpgradeModel_Rcorp3 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 5);
            this.baseid = new LorId(LogLikeMod.ModId, 608003);
        }
    }
    #endregion

    #region --- UpgradeModel_Rcorp4 ---



    /// <summary>Card upgrade model: UpgradeModel_Rcorp4</summary>


    public class UpgradeModel_Rcorp4 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 5, 0);
            this.baseid = new LorId(LogLikeMod.ModId, 608004);
        }
    }
    #endregion

    #region --- UpgradeModel_Rcorp5 ---



    /// <summary>Card upgrade model: UpgradeModel_Rcorp5</summary>


    public class UpgradeModel_Rcorp5 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 2);
            this.upgradeinfo.SetSelfAbility("deerCardLog");
            this.upgradeinfo.SetAbility(0, "deerDiceLog");
            this.baseid = new LorId(LogLikeMod.ModId, 608005);
        }
    }
    #endregion

    #region --- UpgradeModel_Rcorp6 ---



    /// <summary>Card upgrade model: UpgradeModel_Rcorp6</summary>


    public class UpgradeModel_Rcorp6 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetCost(1);
            this.baseid = new LorId(LogLikeMod.ModId, 608006);
        }
    }
    #endregion

    #region --- UpgradeModel_Rcorp7 ---



    /// <summary>Card upgrade model: UpgradeModel_Rcorp7</summary>


    public class UpgradeModel_Rcorp7 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetDice(2, 1, 1);
            this.upgradeinfo.SetSelfAbility("powerUp1speed5exhaustLog");
            this.baseid = new LorId(LogLikeMod.ModId, 608007);
        }
    }
    #endregion

    #region --- UpgradeModel_Rcorp8 ---



    /// <summary>Card upgrade model: UpgradeModel_Rcorp8</summary>


    public class UpgradeModel_Rcorp8 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 2);
            this.upgradeinfo.SetSelfAbility("powerUp3speed5exhaustLog");
            this.baseid = new LorId(LogLikeMod.ModId, 608008);
        }
    }
    #endregion

    #region --- UpgradeModel_Rcorp9 ---



    /// <summary>Card upgrade model: UpgradeModel_Rcorp9</summary>


    public class UpgradeModel_Rcorp9 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 2);
            this.upgradeinfo.SetDice(1, 0, 1);
            this.upgradeinfo.SetDice(2, 1, 1);
            this.upgradeinfo.SetSelfAbility("otherAlly3strength1thisRoundLog");
            this.baseid = new LorId(LogLikeMod.ModId, 608009);
        }
    }
    #endregion

    #region --- UpgradeModel_RedMist1 ---



    /// <summary>Card upgrade model: UpgradeModel_RedMist1</summary>


    public class UpgradeModel_RedMist1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 8);
            this.baseid = new LorId(LogLikeMod.ModId, 607001);
        }
    }
    #endregion

    #region --- UpgradeModel_RedMist2 ---



    /// <summary>Card upgrade model: UpgradeModel_RedMist2</summary>


    public class UpgradeModel_RedMist2 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 5, 6);
            this.baseid = new LorId(LogLikeMod.ModId, 607002);
        }
    }
    #endregion

    #region --- UpgradeModel_RedMist3 ---



    /// <summary>Card upgrade model: UpgradeModel_RedMist3</summary>


    public class UpgradeModel_RedMist3 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 2);
            this.upgradeinfo.SetDice(1, 1, 2);
            this.upgradeinfo.SetSelfAbility("RMR_kaliNormalAttackJ");
            this.baseid = new LorId(LogLikeMod.ModId, 607003);
        }
    }
    #endregion

    #region --- UpgradeModel_RedMist4 ---



    /// <summary>Card upgrade model: UpgradeModel_RedMist4</summary>


    public class UpgradeModel_RedMist4 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetDice(2, 1, 1);
            this.upgradeinfo.SetSelfAbility("RMR_kaliNormalAttackZ");
            this.baseid = new LorId(LogLikeMod.ModId, 607004);
        }
    }
    #endregion

    #region --- UpgradeModel_RedMist5 ---



    /// <summary>Card upgrade model: UpgradeModel_RedMist5</summary>


    public class UpgradeModel_RedMist5 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 2);
            this.upgradeinfo.SetDice(1, 1, 2);
            this.upgradeinfo.SetSelfAbility("RMR_kaliNormalAttackH");
            this.baseid = new LorId(LogLikeMod.ModId, 607005);
        }
    }
    #endregion

    #region --- UpgradeModel_RedMist6 ---



    /// <summary>Card upgrade model: UpgradeModel_RedMist6</summary>


    public class UpgradeModel_RedMist6 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 2);
            this.baseid = new LorId(LogLikeMod.ModId, 607006);
        }
    }
    #endregion

    #region --- UpgradeModel_RedMist7 ---



    /// <summary>Card upgrade model: UpgradeModel_RedMist7</summary>


    public class UpgradeModel_RedMist7 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 6);
            this.baseid = new LorId(LogLikeMod.ModId, 607007);
        }
    }
    #endregion

    #region --- UpgradeModel_Shyao1 ---











    /// <summary>Card upgrade model: UpgradeModel_Shyao1</summary>










    public class UpgradeModel_Shyao1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 2);
            this.baseid = new LorId(LogLikeMod.ModId, 610001);
        }
    }
    #endregion

    #region --- UpgradeModel_Shyao10 ---



    /// <summary>Card upgrade model: UpgradeModel_Shyao10</summary>


    public class UpgradeModel_Shyao10 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 0, 1);
            this.upgradeinfo.SetDice(2, 1, 0);
            this.upgradeinfo.SetDice(3, 1, 0);
            this.baseid = new LorId(LogLikeMod.ModId, 610010);
        }
    }
    #endregion

    #region --- UpgradeModel_Shyao11 ---



    /// <summary>Card upgrade model: UpgradeModel_Shyao11</summary>


    public class UpgradeModel_Shyao11 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 0);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetDice(2, 0, 2);
            this.upgradeinfo.SetSelfAbility("allyBufLog");
            this.baseid = new LorId(LogLikeMod.ModId, 610011);
        }
    }
    #endregion

    #region --- UpgradeModel_Shyao2 ---



    /// <summary>Card upgrade model: UpgradeModel_Shyao2</summary>


    public class UpgradeModel_Shyao2 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetDice(2, 2, 0);
            this.baseid = new LorId(LogLikeMod.ModId, 610002);
        }
    }
    #endregion

    #region --- UpgradeModel_Shyao3 ---



    /// <summary>Card upgrade model: UpgradeModel_Shyao3</summary>


    public class UpgradeModel_Shyao3 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 0);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetAbility(0, "burn1pwLog");
            this.baseid = new LorId(LogLikeMod.ModId, 610003);
        }
    }
    #endregion

    #region --- UpgradeModel_Shyao4 ---



    /// <summary>Card upgrade model: UpgradeModel_Shyao4</summary>


    public class UpgradeModel_Shyao4 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 2);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.baseid = new LorId(LogLikeMod.ModId, 610004);
        }
    }
    #endregion

    #region --- UpgradeModel_Shyao5 ---



    /// <summary>Card upgrade model: UpgradeModel_Shyao5</summary>


    public class UpgradeModel_Shyao5 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.baseid = new LorId(LogLikeMod.ModId, 610005);
        }
    }
    #endregion

    #region --- UpgradeModel_Shyao6 ---



    /// <summary>Card upgrade model: UpgradeModel_Shyao6</summary>


    public class UpgradeModel_Shyao6 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 3, 5);
            this.baseid = new LorId(LogLikeMod.ModId, 610006);
        }
    }
    #endregion

    #region --- UpgradeModel_Shyao8 ---



    /// <summary>Card upgrade model: UpgradeModel_Shyao8</summary>


    public class UpgradeModel_Shyao8 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 0);
            this.upgradeinfo.SetDice(1, 1, 0);
            this.upgradeinfo.SetDice(2, 1, 0);
            this.upgradeinfo.SetSelfAbility("shyaoSelfBurnLog");
            this.baseid = new LorId(LogLikeMod.ModId, 610008);
        }
    }
    #endregion

    #region --- UpgradeModel_Shyao9 ---



    /// <summary>Card upgrade model: UpgradeModel_Shyao9</summary>


    public class UpgradeModel_Shyao9 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 5, 0);
            this.baseid = new LorId(LogLikeMod.ModId, 610009);
        }
    }
    #endregion

    #region --- UpgradeModel_StigmaBurn ---



    


    /// <summary>Card upgrade model: UpgradeModel_StigmaBurn</summary>


    


    public class UpgradeModel_StigmaBurn : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetSelfAbility("Stigma8_0Up");
            this.baseid = new LorId(LogLikeMod.ModId, 8582008);
        }
    }
    #endregion

    #region --- UpgradeModel_Stray6 ---






    /// <summary>Card upgrade model: UpgradeModel_Stray6</summary>





    public class UpgradeModel_Stray6 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 2);
            this.baseid = new LorId(LogLikeMod.ModId, 303009);
        }
    }
    #endregion

    #region --- UpgradeModel_Stray7 ---



    /// <summary>Card upgrade model: UpgradeModel_Stray7</summary>


    public class UpgradeModel_Stray7 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 0);
            this.upgradeinfo.SetDice(1, 0, 1);
            this.upgradeinfo.SetDice(2, 1, 1);
            this.upgradeinfo.SetDice(3, 1, 0);
            this.upgradeinfo.SetCost(1);
            this.baseid = new LorId(LogLikeMod.ModId, 303002);
        }
    }
    #endregion

    #region --- UpgradeModel_Stray8 ---



    /// <summary>Card upgrade model: UpgradeModel_Stray8</summary>


    public class UpgradeModel_Stray8 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 0);
            this.upgradeinfo.SetDice(1, 2, 0);
            this.upgradeinfo.SetDice(2, 2, 0);
            this.baseid = new LorId(LogLikeMod.ModId, 303001);
        }
    }
    #endregion

    #region --- UpgradeModel_Stray9 ---



    /// <summary>Card upgrade model: UpgradeModel_Stray9</summary>


    public class UpgradeModel_Stray9 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 0);
            this.upgradeinfo.SetDice(1, 0, 2);
            this.upgradeinfo.SetDice(2, 1, 1);
            this.baseid = new LorId(LogLikeMod.ModId, 303007);
        }
    }
    #endregion

    #region --- UpgradeModel_Street1 ---





    /// <summary>Card upgrade model: UpgradeModel_Street1</summary>




    public class UpgradeModel_Street1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetCost(1);
            this.baseid = new LorId(LogLikeMod.ModId, 202006);
        }
    }
    #endregion

    #region --- UpgradeModel_Street3 ---





    /// <summary>Card upgrade model: UpgradeModel_Street3</summary>




    public class UpgradeModel_Street3 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 0);
            this.upgradeinfo.SetDice(1, 1, 0);
            this.upgradeinfo.SetDice(2, 1, 0);
            this.baseid = new LorId(LogLikeMod.ModId, 202005);
        }
    }
    #endregion

    #region --- UpgradeModel_Street8 ---



    /// <summary>Card upgrade model: UpgradeModel_Street8</summary>


    public class UpgradeModel_Street8 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(1, 3, 0);
            this.baseid = new LorId(LogLikeMod.ModId, 202004);
        }
    }
    #endregion

    #region --- UpgradeModel_Thumb1 ---



   


    

    /// <summary>Card upgrade model: UpgradeModel_Thumb1</summary>


   


    

    public class UpgradeModel_Thumb1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetSelfAbility("AddBullet1AndDrawLog");
            this.baseid = new LorId(LogLikeMod.ModId, 602001);
        }
    }
    #endregion

    #region --- UpgradeModel_Thumb10 ---



    /// <summary>Card upgrade model: UpgradeModel_Thumb10</summary>


    public class UpgradeModel_Thumb10 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 2);
            this.upgradeinfo.SetDice(1, 1, 0);
            this.upgradeinfo.SetDice(2, 1, 0);
            this.upgradeinfo.SetDice(3, 0, 1);
            this.baseid = new LorId(LogLikeMod.ModId, 602010);
        }
    }
    #endregion

    #region --- UpgradeModel_Thumb11 ---



    /// <summary>Card upgrade model: UpgradeModel_Thumb11</summary>


    public class UpgradeModel_Thumb11 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetDice(2, 0, 1);
            this.upgradeinfo.SetSelfAbility("dmgUp3AllLog");
            this.baseid = new LorId(LogLikeMod.ModId, 602011);
        }
    }
    #endregion

    #region --- UpgradeModel_Thumb12 ---



    /// <summary>Card upgrade model: UpgradeModel_Thumb12</summary>


    public class UpgradeModel_Thumb12 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 0, 2);
            this.upgradeinfo.SetAbility(0, "bleeding3atk");
            this.baseid = new LorId(LogLikeMod.ModId, 602012);
        }
    }
    #endregion

    #region --- UpgradeModel_Thumb2 ---



    /// <summary>Card upgrade model: UpgradeModel_Thumb2</summary>


    public class UpgradeModel_Thumb2 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 0);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetDice(2, 1, 1);
            this.upgradeinfo.SetSelfAbility("AddBullet2Log");
            this.baseid = new LorId(LogLikeMod.ModId, 602002);
        }
    }
    #endregion

    #region --- UpgradeModel_Thumb20 ---



    /// <summary>Card upgrade model: UpgradeModel_Thumb20</summary>


    public class UpgradeModel_Thumb20 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 2);
            this.upgradeinfo.SetSelfAbility("thumbBullet1Log");
            this.baseid = new LorId(LogLikeMod.ModId, 602020);
        }
    }
    #endregion

    #region --- UpgradeModel_Thumb21 ---



    /// <summary>Card upgrade model: UpgradeModel_Thumb21</summary>


    public class UpgradeModel_Thumb21 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 2);
            this.upgradeinfo.SetSelfAbility("thumbBullet2Log");
            this.baseid = new LorId(LogLikeMod.ModId, 602021);
        }
    }
    #endregion

    #region --- UpgradeModel_Thumb22 ---



    /// <summary>Card upgrade model: UpgradeModel_Thumb22</summary>


    public class UpgradeModel_Thumb22 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 2);
            this.upgradeinfo.SetSelfAbility("thumbBullet3Log");
            this.baseid = new LorId(LogLikeMod.ModId, 602022);
        }
    }
    #endregion

    #region --- UpgradeModel_Thumb3 ---



    /// <summary>Card upgrade model: UpgradeModel_Thumb3</summary>


    public class UpgradeModel_Thumb3 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.baseid = new LorId(LogLikeMod.ModId, 602003);
        }
    }
    #endregion

    #region --- UpgradeModel_Thumb4 ---



    /// <summary>Card upgrade model: UpgradeModel_Thumb4</summary>


    public class UpgradeModel_Thumb4 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 2);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.baseid = new LorId(LogLikeMod.ModId, 602004);
        }
    }
    #endregion

    #region --- UpgradeModel_Thumb6 ---



    /// <summary>Card upgrade model: UpgradeModel_Thumb6</summary>


    public class UpgradeModel_Thumb6 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 2);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.baseid = new LorId(LogLikeMod.ModId, 602006);
        }
    }
    #endregion

    #region --- UpgradeModel_Thumb7 ---



    /// <summary>Card upgrade model: UpgradeModel_Thumb7</summary>


    public class UpgradeModel_Thumb7 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 2, 0);
            this.upgradeinfo.SetDice(2, 1, 1);
            this.baseid = new LorId(LogLikeMod.ModId, 602007);
        }
    }
    #endregion

    #region --- UpgradeModel_Thumb8 ---



    /// <summary>Card upgrade model: UpgradeModel_Thumb8</summary>


    public class UpgradeModel_Thumb8 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetDice(2, 2, 0);
            this.baseid = new LorId(LogLikeMod.ModId, 602008);
        }
    }
    #endregion

    #region --- UpgradeModel_Thumb9 ---



    /// <summary>Card upgrade model: UpgradeModel_Thumb9</summary>


    public class UpgradeModel_Thumb9 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 0, 2);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetDice(2, 1, 1);
            this.baseid = new LorId(LogLikeMod.ModId, 602009);
        }
    }
    #endregion

    #region --- UpgradeModel_usett1 ---



    /// <summary>Card upgrade model: UpgradeModel_usett1</summary>


    public class UpgradeModel_usett1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 2);
            this.upgradeinfo.SetDice(1, 2, 0);
            this.baseid = new LorId(LogLikeMod.ModId, 613001);
        }
    }
    #endregion

    #region --- UpgradeModel_usett2 ---



    /// <summary>Card upgrade model: UpgradeModel_usett2</summary>


    public class UpgradeModel_usett2 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 2, 0);
            this.baseid = new LorId(LogLikeMod.ModId, 613002);
        }
    }
    #endregion

    #region --- UpgradeModel_usett3 ---



    /// <summary>Card upgrade model: UpgradeModel_usett3</summary>


    public class UpgradeModel_usett3 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 0);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.baseid = new LorId(LogLikeMod.ModId, 613003);
        }
    }
    #endregion

    #region --- UpgradeModel_usett4 ---



    /// <summary>Card upgrade model: UpgradeModel_usett4</summary>


    public class UpgradeModel_usett4 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 2, 0);
            this.upgradeinfo.SetDice(2, 1, 1);
            this.baseid = new LorId(LogLikeMod.ModId, 613004);
        }
    }
    #endregion

    #region --- UpgradeModel_usett5 ---



    /// <summary>Card upgrade model: UpgradeModel_usett5</summary>


    public class UpgradeModel_usett5 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 2, 0);
            this.upgradeinfo.SetDice(2, 1, 1);
            this.baseid = new LorId(LogLikeMod.ModId, 613005);
        }
    }
    #endregion

    #region --- UpgradeModel_usett6 ---



    /// <summary>Card upgrade model: UpgradeModel_usett6</summary>


    public class UpgradeModel_usett6 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 2, 0);
            this.upgradeinfo.SetDice(2, 2, 1);
            this.upgradeinfo.SetAbility(2, "usett_eyeLog");
            this.baseid = new LorId(LogLikeMod.ModId, 613006);
        }
    }
    #endregion



   


    // Feather Shield
    #region --- UpgradeModel_Wedge10 ---

    /// <summary>Card upgrade model: UpgradeModel_Wedge10</summary>
    public class UpgradeModel_Wedge10 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetCost(1);
            this.baseid = new LorId(LogLikeMod.ModId, 408013);
        }
    }
    #endregion

    // Sparking Spear
    #region --- UpgradeModel_Wedge2 ---

    /// <summary>Card upgrade model: UpgradeModel_Wedge2</summary>
    public class UpgradeModel_Wedge2 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetCost(1);
            this.baseid = new LorId(LogLikeMod.ModId, 408002);
        }
    }
    #endregion


    // Searing Sword
    #region --- UpgradeModel_Wedge8 ---

    /// <summary>Card upgrade model: UpgradeModel_Wedge8</summary>
    public class UpgradeModel_Wedge8 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetCost(1);
            this.baseid = new LorId(LogLikeMod.ModId, 408012);
        }
    }
    #endregion

    #region --- UpgradeModel_work1_1 ---


    /// <summary>Card upgrade model: UpgradeModel_work1_1</summary>

    public class UpgradeModel_work1_1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetSelfAbility("RMR_SmokeSmashUpgrade");
            this.upgradeinfo.SetAbility(1, "RMR_Smoke3Atk");
            this.baseid = new LorId(LogLikeMod.ModId, 615003);
        }
    }
    #endregion

    #region --- UpgradeModel_work1 ---


    /// <summary>Card upgrade model: UpgradeModel_work1</summary>

    public class UpgradeModel_work1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetSelfAbility("RMR_Gain4Smoke");
            this.baseid = new LorId(LogLikeMod.ModId, 615001);
        }
    }
    #endregion

    #region --- UpgradeModel_work2 ---


    /// <summary>Card upgrade model: UpgradeModel_work2</summary>

    public class UpgradeModel_work2 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetSelfAbility("RMR_Gain4Smoke");
            this.baseid = new LorId(LogLikeMod.ModId, 615002);
        }
    }
    #endregion

    #region --- UpgradeModel_work3 ---


    /// <summary>Card upgrade model: UpgradeModel_work3</summary>

    public class UpgradeModel_work3 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetDice(2, 1, 1);
            this.upgradeinfo.SetSelfAbility("RMR_Gain3Smoke");
            this.upgradeinfo.SetAbility(1, "RMR_Smoke4Atk");
            this.baseid = new LorId(LogLikeMod.ModId, 615004);
        }
    }
    #endregion

    #region --- UpgradeModel_work4 ---


    /// <summary>Card upgrade model: UpgradeModel_work4</summary>

    public class UpgradeModel_work4 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 2);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetDice(2, 2, 2);
            this.baseid = new LorId(LogLikeMod.ModId, 615006);
        }
    }
    #endregion

    #region --- UpgradeModel_work5 ---


    /// <summary>Card upgrade model: UpgradeModel_work5</summary>

    public class UpgradeModel_work5 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetCost(1);
            this.baseid = new LorId(LogLikeMod.ModId, 615005);
        }
    }
    #endregion

    // Mend Weapon
    #region --- UpgradeModel_workshopfixer1 ---

    /// <summary>Card upgrade model: UpgradeModel_workshopfixer1</summary>
    public class UpgradeModel_workshopfixer1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetCost(1);
            this.baseid = new LorId(LogLikeMod.ModId, 410001);
        }
    }
    #endregion

    #region --- UpgradeModel_Yan1 ---


    /// <summary>Card upgrade model: UpgradeModel_Yan1</summary>

    public class UpgradeModel_Yan1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 2);
            this.upgradeinfo.SetAbility(0, "yanAreaDiceLog");
            this.baseid = new LorId(LogLikeMod.ModId, 611001);
        }
    }
    #endregion

    #region --- UpgradeModel_Yan2 ---


    /// <summary>Card upgrade model: UpgradeModel_Yan2</summary>

    public class UpgradeModel_Yan2 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 0, 2);
            this.upgradeinfo.SetDice(2, 0, 1);
            this.upgradeinfo.SetAbility(1, "yanOnly1DiceLog");
            this.baseid = new LorId(LogLikeMod.ModId, 611002);
        }
    }
    #endregion

    #region --- UpgradeModel_Yan3 ---


    /// <summary>Card upgrade model: UpgradeModel_Yan3</summary>

    public class UpgradeModel_Yan3 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 2, 2);
            this.baseid = new LorId(LogLikeMod.ModId, 611003);
        }
    }
    #endregion

    #region --- UpgradeModel_Yan4 ---


    /// <summary>Card upgrade model: UpgradeModel_Yan4</summary>

    public class UpgradeModel_Yan4 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetSelfAbility("yanInstantEnergyLog");
            this.baseid = new LorId(LogLikeMod.ModId, 611004);
        }
    }
    #endregion

    #region --- UpgradeModel_Yan5 ---


    /// <summary>Card upgrade model: UpgradeModel_Yan5</summary>

    public class UpgradeModel_Yan5 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetDice(2, 1, 1);
            this.upgradeinfo.SetAbility(2, "yanOnly1Dice");
            this.baseid = new LorId(LogLikeMod.ModId, 611005);
        }
    }
    #endregion

    #region --- UpgradeModel_Yan6 ---


    /// <summary>Card upgrade model: UpgradeModel_Yan6</summary>

    public class UpgradeModel_Yan6 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.upgradeinfo.SetDice(2, 1, 0);
            this.baseid = new LorId(LogLikeMod.ModId, 611006);
        }
    }
    #endregion

    #region --- UpgradeModel_Yan7 ---


    /// <summary>Card upgrade model: UpgradeModel_Yan7</summary>

    public class UpgradeModel_Yan7 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetCost(1);
            this.baseid = new LorId(LogLikeMod.ModId, 611007);
        }
    }
    #endregion

    #region --- UpgradeModel_Yun1 ---


    /// <summary>Card upgrade model: UpgradeModel_Yun1</summary>

    public class UpgradeModel_Yun1 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 3, 0);
            this.baseid = new LorId(LogLikeMod.ModId, 102005);
        }
    }
    #endregion

    #region --- UpgradeModel_Yun2 ---


    /// <summary>Card upgrade model: UpgradeModel_Yun2</summary>

    public class UpgradeModel_Yun2 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetSelfAbility("protection2friendLog");
            this.upgradeinfo.SetDice(0, 0, 0);
            this.upgradeinfo.SetDice(1, 0, 0);
            this.upgradeinfo.SetDice(2, 1, 1);
            this.baseid = new LorId(LogLikeMod.ModId, 102001);
        }
    }
    #endregion

    #region --- UpgradeModel_Yun3 ---


    /// <summary>Card upgrade model: UpgradeModel_Yun3</summary>

    public class UpgradeModel_Yun3 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetCost(1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.baseid = new LorId(LogLikeMod.ModId, 102006);
        }
    }
    #endregion

    #region --- UpgradeModel_Zwei10 ---


    /// <summary>Card upgrade model: UpgradeModel_Zwei10</summary>

    public class UpgradeModel_Zwei10 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 2, 1);
            this.upgradeinfo.SetDice(1, 1, 1);
            this.baseid = new LorId(LogLikeMod.ModId, 301008);
        }
    }
    #endregion

    #region --- UpgradeModel_Zwei9 ---


    /// <summary>Card upgrade model: UpgradeModel_Zwei9</summary>

    public class UpgradeModel_Zwei9 : UpgradeBase
    {
        public override void Init()
        {
            this.upgradeinfo = new UpgradeBase.UpgradeInfo();
            this.upgradeinfo.SetDice(0, 1, 1);
            this.upgradeinfo.SetDice(1, 0, 1);
            this.upgradeinfo.SetDice(2, 0, 2);
            this.baseid = new LorId(LogLikeMod.ModId, 301005);
        }
    }
    #endregion

}
