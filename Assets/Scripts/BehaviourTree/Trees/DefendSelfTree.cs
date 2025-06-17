using System;
using UnityEngine;

public class DefendSelfTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    private EnemyController agent;
    
    public DefendSelfTree(EnemyBlackboard pBlackboard, int pPriority = 0) : base("DefendSelf", pPriority)
    {
        blackboard = pBlackboard;
        setup();
    }
    public DefendSelfTree(EnemyBlackboard pBlackboard, Func<int> pDynamicPriority, int pFallback = 0) : base("Combat", pDynamicPriority, pFallback)
    {
        blackboard = pBlackboard;
        setup();
    }

    private void setup()
    {
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        Sequence _sequence = new("DefendSelf/Sequence");
        Leaf _detectAttack = new("DefendSelf//DetectAttack", new DetectAttackStrategy(blackboard));
        PrioritySelector _defendSelector = new("DefendSelf//Selector");
        Leaf _evadeAction = new("DefendSelf///Evade", new DodgeStrategy(blackboard), ()=> agent.TreeValues.Defense.EvadeWeight);
        Leaf _interceptAction = new("DefendSelf///StrikeParry", new StrikeParry(blackboard), ()=> agent.TreeValues.Defense.ParryWeight);
        //Leaf _blockAction = new("DefendSelf///Block",  , ()=> agent.TreeValues.Defense.BlockWeight);
        Leaf _retreatAction = new("", new RetreatFromPositionStrategy(blackboard), ()=> agent.TreeValues.Defense.RetreatWeight);
        
        AddChild(_sequence);
        _sequence.AddChild(_detectAttack);
        _sequence.AddChild(_defendSelector);
        _defendSelector.AddChild(_evadeAction);
        _defendSelector.AddChild(_interceptAction);
        //_defendSelector.AddChild(_blockAction);
        _defendSelector.AddChild(_retreatAction);
    }
}
