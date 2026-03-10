using System;
using System.Collections.Generic;
using UnityEngine;

public class SurroundTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    private EnemyController agent;
    
    public SurroundTree(EnemyBlackboard pBlackboard, Func<int> pDynamicPriority, int pFallback = 0) : base("Combat", pDynamicPriority, pFallback)
    {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        setup();
    }
    
    private void setup() {
        Leaf _surroundCheck = new("Combat///SurroundCheck", new ConditionStrategy(() =>
        {
            blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> visibleAllies);
            return visibleAllies.Count >= 2;
        }));
        Leaf _surroundTarget = new("Combat///SurroundTarget", new SurroundTargetStrategy(blackboard));
        
        AddChild(new Leaf("ShowTactic", new ActionStrategy(() => agent.GetComponent<TacticIndicator>().SetTactic(TacticType.Surround))));
        AddChild(_surroundCheck);
        AddChild(_surroundTarget);
    }
}