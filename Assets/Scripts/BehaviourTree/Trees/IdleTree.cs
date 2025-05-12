using System.Collections.Generic;
using UnityEngine;

public class IdleTree : BehaviourTree
{
    Blackboard blackboard;
    private EnemyController agent;

    public IdleTree(Blackboard pBlackboard, int pPriority = 0) : base("Combat", pPriority)
    {
        blackboard = pBlackboard;

        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController _agentValue);
        agent = _agentValue;
        
        SetupIdleTree();
    }
    
    private BehaviourTree SetupIdleTree() {
        BehaviourTree _idleTree = new BehaviourTree("Idle");

        UntilFail _untilFail = new("Until Succes");
        Inverter _inverter = new Inverter("IdleBaseInv");
        Parallel _idleParallelNode = new Parallel("IdleBaseParallel", 2);
        
        Parallel _idleParallelAction = new Parallel("IdleParallelAction",2);
        Leaf _findEnemiesAction = new Leaf("IdleParallelAction/FindEnemies", new FindEnemiesStrategy(blackboard, agent.transform, 10f));
        Leaf _findAlliesAction = new Leaf("IdleParallelAction/FindAllies", new FindAlliesStrategy(blackboard, agent.transform, 10f));
        
        PrioritySelector _idleSelectorCheck = new PrioritySelector("_IdleSelectorCheck");
        blackboard.TryGetValue(CommonKeys.VisibleTargets, out List<GameObject> _targets);
        Leaf _targetsCheck = new("Idle//Selector/TargetsCheck", new ConditionStrategy(()=> _targets.Count > 0));
        blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> _allies);
        Leaf _alliesCheck = new("Idle//Selector/AlliesCheck", new ConditionStrategy(()=> _allies.Count > 0));
        //Leaf _patrolTimeCheck = new("Idle//Selector/AlliesCheck", new ConditionStrategy(()=> ));
        
        
        
        _idleTree.AddChild(_untilFail);
        _untilFail.AddChild(_inverter);
        _inverter.AddChild(_idleParallelNode);
        _idleParallelNode.AddChild(_idleParallelAction);
        _idleParallelNode.AddChild(_idleSelectorCheck);
        _idleParallelAction.AddChild(_findEnemiesAction);
        _idleParallelAction.AddChild(_findAlliesAction);
        _idleSelectorCheck.AddChild(_targetsCheck);
        _idleSelectorCheck.AddChild(_alliesCheck);
        //_IdleSelectorCheck.AddChild(_patrolTimeCheck);
        
        return _idleTree;
    }
}
