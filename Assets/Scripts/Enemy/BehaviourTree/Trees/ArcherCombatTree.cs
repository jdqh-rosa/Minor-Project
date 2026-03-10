using System;
using System.Collections.Generic;
using UnityEngine;

public class ArcherCombatTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    private EnemyController agent;
    private BlackboardKey agentKey;
    
    public ArcherCombatTree(EnemyBlackboard pBlackboard, int pPriority = 0) : base("Combat", pPriority)
    {
        blackboard = pBlackboard;
        
        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController agentValue);
        agent = agentValue;
        
        setup();
    }

    void setup()
    {
        Sequence _baseCombatSequence = new Sequence("CombatBaseSeq");
        
        Leaf _obtainEnemy = new Leaf("Combat/ObtainTarget", new GetClosestEnemyStrategy(blackboard));

        Parallel _combatParallel = new("Combat//Parallel", 2, 1);
        Sequence _targetSequence = new Sequence("Combat//TargetSequence");
        Leaf _targetCheck = new Leaf("Combat/TargetCheck", new ConditionStrategy(() => targetEnemy()));
        PrioritySelector _combatTacticSelector = new PrioritySelector("Combat/TargetSeq/CombatTacticSel");
        //Leaf _distanceSelfFromWeapon = new("Combat/DistanceWeapon", new DistanceSelfFromObjectStrategy(blackboard, enemyWeapon(), _enemyWeaponRange));
        Leaf _weaponAware = new Leaf("Combat/WeaponAware", new WeaponAwareCombatStrategy(blackboard));
        Leaf _pointWeapon = new("Combat/PointWeapon", new ActionStrategy(()=> pointWeapon()));
        

        AddChild(_baseCombatSequence);
        _baseCombatSequence.AddChild(_obtainEnemy);
        _baseCombatSequence.AddChild(_combatParallel);
        
        _combatParallel.AddChild(_targetSequence);
        //_targetSequence.AddChild(_weaponAware);
        _targetSequence.AddChild(_targetCheck);
        _targetSequence.AddChild(_pointWeapon);
        _combatParallel.AddChild(_combatTacticSelector);
        _combatTacticSelector.AddChild(new SurroundTree(blackboard, ()=> agent.TreeValues.CombatTactic.SurroundWeight + (agent.TreeValues.CombatTactic.IsSurroundModified ? agent.TreeValues.CombatTactic.SurroundMod : 0)));
        _combatTacticSelector.AddChild(new ArcherTargetTree(blackboard, agent, ()=> agent.TreeValues.CombatTactic.AttackTargetWeight + (agent.TreeValues.CombatTactic.IsAttackTargetModified ? agent.TreeValues.CombatTactic.AttackTargetMod : 0)));
        _combatTacticSelector.AddChild(new FleeTree(blackboard, ()=> agent.TreeValues.CombatTactic.RetreatWeight + (agent.TreeValues.CombatTactic.IsRetreatModified ? agent.TreeValues.CombatTactic.RetreatMod + agent.TreeValues.Health.LowHealthWeight : 0)));
        _combatTacticSelector.AddChild(new FlankTree(blackboard, ()=> agent.TreeValues.CombatTactic.FlankWeight + (agent.TreeValues.CombatTactic.IsFlankModified ? agent.TreeValues.CombatTactic.FlankMod : 0)));
    }

    private GameObject targetEnemy() {
        blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _target);
        return _target;
    }
    
    void pointWeapon()
    {
        if(!blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _target) || !_target) return;
        Vector3 _agentPos = agent.transform.position;
        Vector3 _difVector = _target.transform.position - _agentPos;
        blackboard.SetKeyValue(CommonKeys.ChosenWeaponAngle, RadialHelper.CartesianToPol(new Vector2(_difVector.x, _difVector.z)).y);
    }
}