using System;
using System.Collections.Generic;
using UnityEngine;

public class IdleTree : BehaviourTree
{
    EnemyBlackboard blackboard;

    public IdleTree(EnemyBlackboard pBlackboard, int pPriority = 0) : base("Combat", pPriority)
    {
        blackboard = pBlackboard;

        //SetupIdleTree();
    }
    public IdleTree(EnemyBlackboard pBlackboard, Func<int> pDynamicPriority, int pFallback = 0) : base("Combat", pDynamicPriority, pFallback)
    {
        blackboard = pBlackboard;

        //SetupIdleTree();
    }
    
    private void SetupIdleTree() {
        Parallel _idleBaseParallel = new Parallel("IdleBaseParallel", 1);
        Parallel _idleParallelAction = new Parallel("IdleParallelAction",1);
        Leaf _findEnemiesAction = new Leaf("IdleParallelAction/FindEnemies", new FindEnemiesStrategy(blackboard));
        Leaf _findAlliesAction = new Leaf("IdleParallelAction/FindAllies", new FindAlliesStrategy(blackboard));
        
        PrioritySelector _idleSelectorCheck = new PrioritySelector("_IdleSelectorCheck");
        blackboard.TryGetValue(CommonKeys.VisibleTargets, out List<GameObject> _targets);
        Leaf _targetsCheck = new("Idle//Selector/TargetsCheck", new ConditionStrategy(()=> _targets.Count > 0));
        blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> _allies);
        Leaf _alliesCheck = new("Idle//Selector/AlliesCheck", new ConditionStrategy(()=> _allies.Count > 0));
        //Leaf _patrolTimeCheck = new("Idle//Selector/PatrolCheck", new ConditionStrategy(()=> blackboard.TimeSinceLastPatrol() < blackboard.PatrolCooldown()));
        
        AddChild(_idleBaseParallel);
        _idleBaseParallel.AddChild(_idleParallelAction);
        _idleBaseParallel.AddChild(_idleSelectorCheck);
        _idleParallelAction.AddChild(_findEnemiesAction);
        _idleParallelAction.AddChild(_findAlliesAction);
        _idleSelectorCheck.AddChild(_targetsCheck);
        _idleSelectorCheck.AddChild(_alliesCheck);
        //_IdleSelectorCheck.AddChild(_patrolTimeCheck);
    }
}
