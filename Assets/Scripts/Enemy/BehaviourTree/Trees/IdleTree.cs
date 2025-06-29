using System;
using System.Collections.Generic;
using UnityEngine;

public class IdleTree : BehaviourTree
{
    EnemyBlackboard blackboard;

    public IdleTree(EnemyBlackboard pBlackboard, int pPriority = 0) : base("Combat", pPriority)
    {
        blackboard = pBlackboard;
    }
    public IdleTree(EnemyBlackboard pBlackboard, Func<int> pDynamicPriority, int pFallback = 0) : base("Combat", pDynamicPriority, pFallback)
    {
        blackboard = pBlackboard;
    }
}
