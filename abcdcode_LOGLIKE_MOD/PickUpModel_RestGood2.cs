// -----------------------------------------------------------------------------
// Post-battle or shop pickup model: PickUpModel_RestGood2
// Namespace/file: ruina-roguelike-reborn-main\abcdcode_LOGLIKE_MOD\PickUpModel_RestGood2.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
using System;
using System.Reflection;
using UI;


namespace abcdcode_LOGLIKE_MOD
{

    /// <summary>Pickup model: PickUpModel_RestGood2</summary>

    public class PickUpModel_RestGood2 : RestPickUp
    {
        public PickUpModel_RestGood2()
        {
            this.basepassive = Singleton<PassiveXmlList>.Instance.GetData(new LorId(LogLikeMod.ModId, 800002));
            this.Name = Singleton<PassiveDescXmlList>.Instance.GetName(this.basepassive.id);
            this.Desc = Singleton<PassiveDescXmlList>.Instance.GetDesc(this.basepassive.id);
            this.id = new LorId(LogLikeMod.ModId, 800002);
            this.type = RestPickUp.RestPickUpType.Main;
        }

        public override void OnPickUp(BattleUnitModel model)
        {
            base.OnPickUp(model);
            LogStatAdder adder = new LogStatAdder()
            {
                maxbreakpercent = 5,
                maxhppercent = 5
            };
            LogueBookModels.AddPlayerStat(model.UnitData, adder);
        }

        public override bool CheckCondition()
        {
            // Share shop filter so Binah fixed-deck degraded pages are included.
            bool condition = ShopGoods_CardUpgrade.HasUpgradeableCards();
            if (!condition)
                UI.UIAlarmPopup.instance.SetAlarmText(TextDataModel.GetText("CardCheckPopUp_CannotUpgrade"));
            return condition;
        }

        public override void OnChoice(RestGood good)
        {
            MysteryModel_CardChoice.PopupCardChoice(
                ShopGoods_CardUpgrade.GetUpgradeableCards(),
                new MysteryModel_CardChoice.ChoiceResult(this.UpgradeCard),
                MysteryModel_CardChoice.ChoiceDescType.UpgradeDesc);
        }

        public void UpgradeCard(MysteryModel_CardChoice mystery, DiceCardItemModel model)
        {
            MysteryModel_UpgradeCheckPopup.PopupUpgradeCheck(
                model.GetID(), 
                my => this.UpgradedCard(mystery, my, model.GetID())
            );
        }

        public void UpgradedCard(
          MysteryModel_CardChoice mystery,
          MysteryModel_UpgradeCheckPopup mystery2,
          LorId cardid)
        {
            try
            {
                if (mystery2 != null && (mystery2.binahSpecialUpgrade || LogueBookModels.IsBinahDegradedUpgradeable(cardid)))
                {
                    if (!LogueBookModels.TryApplyBinahDegradedCardUpgrade(cardid))
                        return;
                }
                else
                {
                    if (mystery2?.metadata == null || string.IsNullOrEmpty(mystery2.metadata.unparsedPid))
                    {
                        UI.UIAlarmPopup.instance.SetAlarmText(TextDataModel.GetText("CardCheckPopUp_CannotUpgrade"));
                        return;
                    }
                    LogueBookModels.ReplaceUnlockedCardWithUpgrade(
                        cardid,
                        new LorId(mystery2.metadata.unparsedPid, cardid.id));
                    try { LogueBookModels.SavePermanentCompendiumData(); } catch { /* atlas */ }
                    try { LoguePlayDataSaver.SavePlayData_Menu(); } catch { /* continue snapshot */ }
                }
                UISoundManager.instance.PlayEffectSound(UISoundType.Card_Apply);
            }
            finally
            {
                try { Singleton<MysteryManager>.Instance.EndMystery(mystery); } catch { /* best-effort */ }
                try { Singleton<MysteryManager>.Instance.EndMystery(mystery2); } catch { /* best-effort */ }
            }
        }
    }
}
