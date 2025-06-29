using System.Collections.Generic;
using UnityEngine;

public class DeciderTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    private EnemyController agent;
    public DeciderTree(EnemyBlackboard pBlackboard, int pPriority = 0) : base("DeciderTree", pPriority) {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        setup();
    }
    
    private void setup() {
        PrioritySelector _prioritySelector = new PrioritySelector("Decider//BranchSelector");
        
        Sequence _combatBranch = new("Base//CombatSequence", ()=> agent.TreeValues.Decider.CombatWeight);
        _combatBranch.AddChild(new Leaf("CheckCombat", new ConditionStrategy(() =>
        {
            blackboard.TryGetValue(CommonKeys.VisibleEnemies, out List<GameObject> targets);
            return targets is { Count: > 0 };
        })));
        _combatBranch.AddChild(new CombatTree(blackboard));
        
        Sequence _assembleBranch = new("Base//AssembleSequence", ()=> agent.TreeValues.Decider.AssembleWeight + (agent.TreeValues.Decider.IsAssembleModified ? agent.TreeValues.Decider.AssembleMod : 0));
        _assembleBranch.AddChild(new Leaf("CheckAssemble", new ConditionStrategy(() =>
        {
            blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> targets);
            return targets is { Count: > 0 };
        })));
        _assembleBranch.AddChild(new AssembleTree(blackboard));
        
        //todo: Add Patrol Branch
        
        AddChild(_prioritySelector);
        _prioritySelector.AddChild(_combatBranch);
        _prioritySelector.AddChild(_assembleBranch);
        _prioritySelector.AddChild(new IdleTree(blackboard, ()=> agent.TreeValues.Decider.IdleWeight));
    }
}