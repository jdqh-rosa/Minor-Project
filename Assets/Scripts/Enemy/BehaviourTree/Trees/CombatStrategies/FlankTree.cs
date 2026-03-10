using System;
using System.Collections.Generic;
using UnityEngine;

public class FlankTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    private EnemyController agent;
    
    public FlankTree(EnemyBlackboard pBlackboard, Func<int> pDynamicPriority, int pFallback = 0) : base("Combat", pDynamicPriority, pFallback)
    {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        setup();
    }
    
    private void setup() {
        Leaf _flankCheck = new("Combat///FlankCheck", new ConditionStrategy(() =>
        {
            blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> visibleAllies);
            return visibleAllies.Count >= 1;
        }));
        Leaf _flankTarget = new("Combat///FlankTarget", new FlankStrategy(blackboard));
        
        
        AddChild(new Leaf("ShowTactic", new ActionStrategy(() => agent.GetComponent<TacticIndicator>().SetTactic(TacticType.Flank))));
        AddChild(_flankCheck);
        AddChild(_flankTarget);
    }
}

