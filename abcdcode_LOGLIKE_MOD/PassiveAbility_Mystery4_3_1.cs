// -----------------------------------------------------------------------------
// Passive ability script: PassiveAbility_Mystery4_3_1
// Namespace/file: ruina-roguelike-reborn-main\abcdcode_LOGLIKE_MOD\PassiveAbility_Mystery4_3_1.cs
// English comments/regions for maintainability. Do not rename disk save keys.
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;

 
namespace abcdcode_LOGLIKE_MOD {

/// <summary>Passive ability: PassiveAbility_Mystery4_3_1</summary>

public class PassiveAbility_Mystery4_3_1 : PassiveAbilityBase
{
  public static bool Ended;
  public int round;
  public bool Used;

  public override string debugDesc => "일방 공격을 받지 않음. 내 주사위를 모두 소모하면 강제 합 종료. 3막 뒤에 전투가 끝남";

  public override void OnWaveStart()
  {
    base.OnWaveStart();
    PassiveAbility_Mystery4_3_1.Ended = false;
  }

  public override void OnRoundStart()
  {
    base.OnRoundStart();
    ++this.round;
    if (this.round <= 3 || PassiveAbility_Mystery4_3_1.Ended)
      return;
    try { RewardingModel.MarkNonCombatNodeExit("MysteryThreeRoundTimer"); }
    catch (Exception ex) { UnityEngine.Debug.LogWarning("[RMR] MysteryThreeRoundTimer exit marker: " + ex.Message); }
    Singleton<StageController>.Instance.GetStageModel().GetWave(Singleton<StageController>.Instance.CurrentWave).Defeat();
    Singleton<StageController>.Instance.EndBattle();
    PassiveAbility_Mystery4_3_1.Ended = true;
  }

  public override void OnUseCard(BattlePlayingCardDataInUnitModel curCard)
  {
    base.OnUseCard(curCard);
    if (curCard.target.currentDiceAction == null)
      return;
    List<BattleDiceBehavior> diceBehaviorList = curCard.GetDiceBehaviorList();
    diceBehaviorList.RemoveAll((Predicate<BattleDiceBehavior>) (x => x.Type != 0));
    if (diceBehaviorList.Count <= 0)
      return;
    diceBehaviorList[diceBehaviorList.Count - 1].AddAbility((DiceCardAbilityBase) new PassiveAbility_Mystery4_3_1.EndParryingAbility());
  }

  public override void OnStartTargetedOneSide(BattlePlayingCardDataInUnitModel attackerCard)
  {
    base.OnStartTargetedOneSide(attackerCard);
    Queue<BattleDiceBehavior> battleDiceBehaviorQueue = new Queue<BattleDiceBehavior>((IEnumerable<BattleDiceBehavior>) attackerCard.cardBehaviorQueue);
    List<BattleDiceBehavior> behaviourList = new List<BattleDiceBehavior>();
    while (battleDiceBehaviorQueue.Count > 0)
    {
      BattleDiceBehavior battleDiceBehavior = battleDiceBehaviorQueue.Dequeue();
      behaviourList.Add(battleDiceBehavior);
    }
    attackerCard.owner.cardSlotDetail.keepCard.AddBehaviours(attackerCard.card, behaviourList);
    attackerCard.cardBehaviorQueue.Clear();
  }

  /// <summary>EndParryingAbility</summary>

  public class EndParryingAbility : DiceCardAbilityBase
  {
    public override void AfterAction()
    {
      base.AfterAction();
      BattleUnitModel target = this.owner.currentDiceAction?.target;
      if (target == null)
        return;
      Queue<BattleDiceBehavior> battleDiceBehaviorQueue = new Queue<BattleDiceBehavior>((IEnumerable<BattleDiceBehavior>) target.currentDiceAction.cardBehaviorQueue);
      List<BattleDiceBehavior> behaviourList = new List<BattleDiceBehavior>();
      while (battleDiceBehaviorQueue.Count > 0)
      {
        BattleDiceBehavior battleDiceBehavior = battleDiceBehaviorQueue.Dequeue();
        behaviourList.Add(battleDiceBehavior);
      }
      target.cardSlotDetail.keepCard.AddBehaviours(target.currentDiceAction.card, behaviourList);
      target.currentDiceAction.cardBehaviorQueue.Clear();
    }
  }
}
}
