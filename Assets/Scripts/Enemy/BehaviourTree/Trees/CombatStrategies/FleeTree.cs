using System;
using System.Collections.Generic;
using UnityEngine;

public class FleeTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    private EnemyController agent;
    
    public FleeTree(EnemyBlackboard pBlackboard, Func<int> pDynamicPriority, int pFallback = 0) : base("Combat", pDynamicPriority, pFallback)
    {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        setup();
    }
    
    private void setup() {
        Leaf _healthCheck = new Leaf("Combat/TargetSeq/HealthCheck", new ConditionStrategy(() => blackboard.CheckLowHealth()));
        //PrioritySelector _retreatSelector = new("Combat//FleeBranch/Selector");
        Leaf _regroup = new("Combat/FleeBranch/Regroup", new GroupUpStrategy(blackboard), ()=> agent.TreeValues.CombatTactic.RetreatGroupWeight);
        Parallel _retreatBranch = new("Combat/FleeBranch/Retreat", 2, 1);
        Sequence _retreatHealer = new("Combat/FleeBranch/RetreatHealer");
        Leaf _getClosestHealer = new ("Combat/FleeBranch/RetreatHealer", new GetClosestAllyTypeStrategy(blackboard, UnitType.Healer));
        EnterRangeTree _goToHealer = new EnterRangeTree(blackboard, targetHealer, 5);
        Leaf _retreat = new("Combat/FleeBranch/Retreat", new RetreatFromEnemiesStrategy(blackboard, 20f), ()=> agent.TreeValues.CombatTactic.RetreatSelfWeight);
        
        AddChild(new Leaf("ShowTactic", new ActionStrategy(() => agent.GetComponent<TacticIndicator>().SetTactic(TacticType.Flee))));
        AddChild(_healthCheck);
        AddChild(_retreatBranch);
        _retreatBranch.AddChild(_retreat);
        _retreatBranch.AddChild(_retreatHealer);
        _retreatHealer.AddChild(_getClosestHealer);
        _retreatHealer.AddChild(_goToHealer);
    }

    private GameObject targetHealer() {
        blackboard.TryGetValue(CommonKeys.TargetAlly, out GameObject target);
        return target;
    }
}