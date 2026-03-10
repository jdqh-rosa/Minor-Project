using System.Collections.Generic;
using UnityEngine;

public class HealerCombatTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    private EnemyController agent;
    
    public HealerCombatTree(EnemyBlackboard pBlackboard, int pPriority = 0) : base("HealerCombat", pPriority)
    {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        setup();
    }
    
    private void setup()
    {
       Sequence _baseCombatSequence = new Sequence("CombatBaseSeq");
       Leaf _obtainAlly = new Leaf("Combat/ObtainTarget", new GetLowestAllyStrategy(blackboard));

        Parallel _combatParallel = new("Combat//Parallel", 1, 1);
        Sequence _targetSequence = new Sequence("Combat//TargetSequence");
        Leaf _targetCheck = new Leaf("Combat/TargetCheck", new ConditionStrategy(() => targetAlly()));
        PrioritySelector _combatTacticSelector = new PrioritySelector("Combat/TargetSeq/CombatTacticSel");
        //Leaf _distanceSelfFromWeapon = new("Combat/DistanceWeapon", new DistanceSelfFromObjectStrategy(blackboard, enemyWeapon(), _enemyWeaponRange));
        Leaf _weaponAware = new Leaf("Combat/WeaponAware", new WeaponAwareCombatStrategy(blackboard));
        Leaf _pointWeapon = new("Combat/PointWeapon", new AngleWeaponAtTargetStrategy(blackboard, CommonKeys.LowestHealthAlly));
        

        AddChild(_baseCombatSequence);
        _baseCombatSequence.AddChild(_obtainAlly);
        _baseCombatSequence.AddChild(_combatParallel);
        
        _combatParallel.AddChild(_targetSequence);
        //_targetSequence.AddChild(_weaponAware);
        _targetSequence.AddChild(_targetCheck);
        _targetSequence.AddChild(_pointWeapon);
        _combatParallel.AddChild(_combatTacticSelector);
        _combatTacticSelector.AddChild(new HealTargetTree(blackboard, agent, ()=> agent.TreeValues.CombatTactic.AttackTargetWeight + (agent.TreeValues.CombatTactic.IsAttackTargetModified ? agent.TreeValues.CombatTactic.AttackTargetMod : 0)));
        _combatTacticSelector.AddChild(new DefendSelfTree(blackboard, ()=> agent.TreeValues.CombatTactic.DefendSelfWeight + (agent.TreeValues.CombatTactic.IsDefendSelfModified ? agent.TreeValues.CombatTactic.DefendSelfMod : 0)));
        _combatTacticSelector.AddChild(new FleeTree(blackboard, ()=> agent.TreeValues.CombatTactic.RetreatWeight + (agent.TreeValues.CombatTactic.IsRetreatModified ? agent.TreeValues.CombatTactic.RetreatMod + agent.TreeValues.Health.LowHealthWeight : 0)));
    }
        
    private GameObject targetAlly() {
        blackboard.TryGetValue(CommonKeys.LowestHealthAlly, out GameObject _target);
        return _target;
    }
}