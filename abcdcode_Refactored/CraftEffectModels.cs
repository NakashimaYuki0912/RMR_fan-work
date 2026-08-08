// -----------------------------------------------------------------------------
// Refactored LOGLIKE/RMR logic: CraftEffectModels
// Namespace/file: ruina-roguelike-reborn-main\abcdcode_Refactored\CraftEffectModels.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
using UI;
using UnityEngine;


namespace abcdcode_LOGLIKE_MOD
{
    #region --- CraftExclusiveCardChapter5 ---

    /// <summary>CraftExclusiveCardChapter5</summary>
    public class CraftExclusiveCardChapter5 : CraftEffect
    {
        public override bool IsNormal()
        {
            return false;
        }

        public override Sprite GetCraftSprite() => LogLikeMod.ArtWorks["Chapter5Icon_ExCard"];

        public override string GetCraftName() => TextDataModel.GetText("CraftExCardChapter5Name");

        public override string GetCraftDesc() => TextDataModel.GetText("CraftExCardChapter5Desc");

        public override int GetCraftCost() => 10;

        public override bool CanCraft(int costresult)
        {
            if (CraftEffect.CanCraftExclusiveByChapter(ChapterGrade.Grade5) != null)
                return base.CanCraft(costresult);
            UIAlarmPopup.instance.SetAlarmText(TextDataModel.GetText("CraftEquipCant"));
            return false;
        }

        public override void Crafting()
        {
            base.Crafting();
            CraftEffect.CraftExclusiveCardByChapter(ChapterGrade.Grade5);
        }
    }
    #endregion

    #region --- CraftExclusiveCardChapter6 ---


    /// <summary>CraftExclusiveCardChapter6</summary>

    public class CraftExclusiveCardChapter6 : CraftEffect
    {
        public override bool IsNormal()
        {
            return false;
        }

        public override Sprite GetCraftSprite() => LogLikeMod.ArtWorks["Chapter6Icon_ExCard"];

        public override string GetCraftName() => TextDataModel.GetText("CraftExCardChapter6Name");

        public override string GetCraftDesc() => TextDataModel.GetText("CraftExCardChapter6Desc");

        public override int GetCraftCost() => 15;

        public override bool CanCraft(int costresult)
        {
            if (CraftEffect.CanCraftExclusiveByChapter(ChapterGrade.Grade6) != null)
                return base.CanCraft(costresult);
            UIAlarmPopup.instance.SetAlarmText(TextDataModel.GetText("CraftEquipCant"));
            return false;
        }

        public override void Crafting()
        {
            base.Crafting();
            CraftEffect.CraftExclusiveCardByChapter(ChapterGrade.Grade6);
            (UI.UIController.Instance.GetUIPanel(UIPanelType.BattleSetting) as UIBattleSettingPanel)._editPanel.BattleCardPanel.SetData();
        }
    }
    #endregion

    #region --- CraftExclusiveCardChapter7 ---


    /// <summary>CraftExclusiveCardChapter7</summary>

    public class CraftExclusiveCardChapter7 : CraftEffect
    {
        public override bool IsNormal()
        {
            return false;
        }

        public override Sprite GetCraftSprite() => LogLikeMod.ArtWorks["Chapter7Icon_ExCard"];

        public override string GetCraftName()
        {
            string text = TextDataModel.GetText("CraftExCardChapter7Name");
            if (string.IsNullOrEmpty(text) || text == "CraftExCardChapter7Name")
                return "Crafting Exclusive Combat Page - Impuritas Civitatis";
            return text;
        }

        public override string GetCraftDesc()
        {
            string text = TextDataModel.GetText("CraftExCardChapter7Desc");
            if (string.IsNullOrEmpty(text) || text == "CraftExCardChapter7Desc")
                return "Craft a random Impuritas Civitatis exclusive Combat Page (only exclusives from key pages in inventory).";
            return text;
        }

        public override int GetCraftCost() => 20;

        public override bool CanCraft(int costresult)
        {
            if (CraftEffect.CanCraftExclusiveByChapter(ChapterGrade.Grade7) != null)
                return base.CanCraft(costresult);
            UIAlarmPopup.instance.SetAlarmText(TextDataModel.GetText("CraftEquipCant"));
            return false;
        }

        public override void Crafting()
        {
            base.Crafting();
            CraftEffect.CraftExclusiveCardByChapter(ChapterGrade.Grade7);
            (UI.UIController.Instance.GetUIPanel(UIPanelType.BattleSetting) as UIBattleSettingPanel)._editPanel.BattleCardPanel.SetData();
        }
    }
    #endregion

    #region --- CraftEquipChapter1 ---


    /// <summary>CraftEquipChapter1</summary>

    public class CraftEquipChapter1 : CraftEffect
    {
        public override Sprite GetCraftSprite() => LogLikeMod.ArtWorks["Chapter1Icon"];

        public override string GetCraftName() => TextDataModel.GetText("CraftEquipChapter1Name");

        public override string GetCraftDesc() => TextDataModel.GetText("CraftEquipChapter1Desc");

        public override int GetCraftCost() => 10;

        public override bool CanCraft(int costresult)
        {
            if (CraftEffect.CheckCreaftEquipLimit(ChapterGrade.Grade1) != null)
                return base.CanCraft(costresult);
            UIAlarmPopup.instance.SetAlarmText(TextDataModel.GetText("CraftEquipCant"));
            return false;
        }

        public override void Crafting()
        {
            base.Crafting();
            CraftEffect.CraftEquipByChapter(ChapterGrade.Grade1);
        }
    }
    #endregion

    #region --- CraftEquipChapter2 ---


    /// <summary>CraftEquipChapter2</summary>

    public class CraftEquipChapter2 : CraftEffect
    {
        public override Sprite GetCraftSprite() => LogLikeMod.ArtWorks["Chapter2Icon"];

        public override string GetCraftName() => TextDataModel.GetText("CraftEquipChapter2Name");

        public override string GetCraftDesc() => TextDataModel.GetText("CraftEquipChapter2Desc");

        public override int GetCraftCost() => 14;

        public override bool CanCraft(int costresult)
        {
            if (CraftEffect.CheckCreaftEquipLimit(ChapterGrade.Grade2) != null)
                return base.CanCraft(costresult);
            UIAlarmPopup.instance.SetAlarmText(TextDataModel.GetText("CraftEquipCant"));
            return false;
        }

        public override void Crafting()
        {
            base.Crafting();
            CraftEffect.CraftEquipByChapter(ChapterGrade.Grade2);
        }
    }
    #endregion

    #region --- CraftEquipChapter3 ---


    /// <summary>CraftEquipChapter3</summary>

    public class CraftEquipChapter3 : CraftEffect
    {
        public override Sprite GetCraftSprite() => LogLikeMod.ArtWorks["Chapter3Icon"];

        public override string GetCraftName() => TextDataModel.GetText("CraftEquipChapter3Name");

        public override string GetCraftDesc() => TextDataModel.GetText("CraftEquipChapter3Desc");

        public override int GetCraftCost() => 18;

        public override bool CanCraft(int costresult)
        {
            if (CraftEffect.CheckCreaftEquipLimit(ChapterGrade.Grade3) != null)
                return base.CanCraft(costresult);
            UIAlarmPopup.instance.SetAlarmText(TextDataModel.GetText("CraftEquipCant"));
            return false;
        }

        public override void Crafting()
        {
            base.Crafting();
            CraftEffect.CraftEquipByChapter(ChapterGrade.Grade3);
        }
    }
    #endregion

    #region --- CraftEquipChapter4 ---


    /// <summary>CraftEquipChapter4</summary>

    public class CraftEquipChapter4 : CraftEffect
    {
        public override Sprite GetCraftSprite() => LogLikeMod.ArtWorks["Chapter4Icon"];

        public override string GetCraftName() => TextDataModel.GetText("CraftEquipChapter4Name");

        public override string GetCraftDesc() => TextDataModel.GetText("CraftEquipChapter4Desc");

        public override int GetCraftCost() => 22;

        public override bool CanCraft(int costresult)
        {
            if (CraftEffect.CheckCreaftEquipLimit(ChapterGrade.Grade4) != null)
                return base.CanCraft(costresult);
            UIAlarmPopup.instance.SetAlarmText(TextDataModel.GetText("CraftEquipCant"));
            return false;
        }

        public override void Crafting()
        {
            base.Crafting();
            CraftEffect.CraftEquipByChapter(ChapterGrade.Grade4);
        }
    }
    #endregion

    #region --- CraftEquipChapter5 ---


    /// <summary>CraftEquipChapter5</summary>

    public class CraftEquipChapter5 : CraftEffect
    {
        public override Sprite GetCraftSprite() => LogLikeMod.ArtWorks["Chapter5Icon"];

        public override string GetCraftName() => TextDataModel.GetText("CraftEquipChapter5Name");

        public override string GetCraftDesc() => TextDataModel.GetText("CraftEquipChapter5Desc");

        public override int GetCraftCost() => 26;

        public override bool CanCraft(int costresult)
        {
            if (CraftEffect.CheckCreaftEquipLimit(ChapterGrade.Grade5) != null)
                return base.CanCraft(costresult);
            UIAlarmPopup.instance.SetAlarmText(TextDataModel.GetText("CraftEquipCant"));
            return false;
        }

        public override void Crafting()
        {
            base.Crafting();
            CraftEffect.CraftEquipByChapter(ChapterGrade.Grade5);
        }
    }
    #endregion

    #region --- CraftEquipChapter6 ---


    /// <summary>CraftEquipChapter6</summary>

    public class CraftEquipChapter6 : CraftEffect
    {
        public override Sprite GetCraftSprite() => LogLikeMod.ArtWorks["Chapter6Icon"];

        public override string GetCraftName() => TextDataModel.GetText("CraftEquipChapter6Name");

        public override string GetCraftDesc() => TextDataModel.GetText("CraftEquipChapter6Desc");

        public override int GetCraftCost() => 30;

        public override bool CanCraft(int costresult)
        {
            if (CraftEffect.CheckCreaftEquipLimit(ChapterGrade.Grade6) != null)
                return base.CanCraft(costresult);
            UIAlarmPopup.instance.SetAlarmText(TextDataModel.GetText("CraftEquipCant"));
            return false;
        }

        public override void Crafting()
        {
            base.Crafting();
            CraftEffect.CraftEquipByChapter(ChapterGrade.Grade6);
        }
    }
    #endregion

    #region --- CraftEquipChapter7 ---


    /// <summary>CraftEquipChapter7</summary>

    public class CraftEquipChapter7 : CraftEffect
    {
        public override Sprite GetCraftSprite() => LogLikeMod.ArtWorks["Chapter7Icon"];

        public override string GetCraftName() => TextDataModel.GetText("CraftEquipChapter7Name");

        public override string GetCraftDesc() => TextDataModel.GetText("CraftEquipChapter7Desc");

        public override int GetCraftCost() => 35;

        public override bool CanCraft(int costresult)
        {
            bool flag;
            if (CraftEffect.CheckCreaftEquipLimit(ChapterGrade.Grade7) == null)
            {
                UIAlarmPopup.instance.SetAlarmText(TextDataModel.GetText("CraftEquipCant"));
                flag = false;
            }
            else
                flag = base.CanCraft(costresult);
            return flag;
        }

        public override void Crafting()
        {
            base.Crafting();
            CraftEffect.CraftEquipByChapter(ChapterGrade.Grade7);
        }
    }
    #endregion


}
