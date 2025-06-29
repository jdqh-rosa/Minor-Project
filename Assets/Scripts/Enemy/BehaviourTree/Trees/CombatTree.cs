using System;
using System.Collections.Generic;
using UnityEngine;

public class CombatTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    private EnemyController agent;
    private BlackboardKey agentKey;
    
    public CombatTree(EnemyBlackboard pBlackboard, int pPriority = 0) : base("Combat", pPriority)
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
        Leaf _targetCheck = new Leaf("Combat/TargetCheck", new ConditionStrategy(() => targetEnemy()));
        PrioritySelector _combatTacticSelector = new PrioritySelector("Combat/TargetSeq/CombatTacticSel");
        Leaf _distanceSelfFromWeapon = new("Combat/DistanceWeapon", new DistanceSelfFromObjectStrategy(blackboard, enemyWeapon(), _enemyWeaponRange));
        
        Sequence _flankSequence = new Sequence("Combat///FlankSeq", ()=> agent.TreeValues.CombatTactic.FlankWeight + (agent.TreeValues.CombatTactic.IsFlankModified ? agent.TreeValues.CombatTactic.FlankMod : 0));
        Leaf _flankCheck = new("Combat///FlankCheck", new ConditionStrategy(() =>
        {
            blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> visibleAllies);
            return visibleAllies.Count >= 1;
        }));
        Leaf _flankTarget = new("Combat///FlankTarget", new FlankStrategy(blackboard));
        
        Sequence _surroundSequence = new Sequence("Combat//SurroundSeq", ()=> agent.TreeValues.CombatTactic.SurroundWeight + (agent.TreeValues.CombatTactic.IsSurroundModified ? agent.TreeValues.CombatTactic.SurroundMod : 0));
        Leaf _surroundCheck = new("Combat///SurroundCheck", new ConditionStrategy(() =>
        {
            blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> visibleAllies);
            return visibleAllies.Count >= 2;
        }));
        Leaf _surroundTarget = new("Combat///SurroundTarget", new SurroundTargetStrategy(blackboard));
        
        Sequence _fleeBranch = new Sequence("Combat//FleeBranch", ()=> agent.TreeValues.CombatTactic.RetreatWeight + (agent.TreeValues.CombatTactic.IsRetreatModified ? agent.TreeValues.CombatTactic.RetreatMod + agent.TreeValues.Health.LowHealthWeight : 0));
        Leaf _healthCheck = new Leaf("Combat/TargetSeq/HealthCheck", new ConditionStrategy(() => blackboard.CheckLowHealth()));
        //PrioritySelector _retreatSelector = new("Combat//FleeBranch/Selector");
        Leaf _regroup = new("Combat/FleeBranch/Regroup", new GroupUpStrategy(blackboard), ()=> agent.TreeValues.CombatTactic.RetreatGroupWeight);
        Leaf _retreat = new("Combat/FleeBranch/Retreat", new RetreatFromTargetStrategy(blackboard, targetEnemy()), ()=> agent.TreeValues.CombatTactic.RetreatSelfWeight);
        
        AddChild(_baseCombatSequence);
        _baseCombatSequence.AddChild(_obtainEnemy);
        _baseCombatSequence.AddChild(_combatParallel);
        _baseCombatSequence.AddChild(_distanceSelfFromWeapon);
        
        _combatParallel.AddChild(_targetCheck);
        _combatParallel.AddChild(_combatTacticSelector);
        _combatTacticSelector.AddChild(_surroundSequence);
        _surroundSequence.AddChild(_surroundCheck);
        _surroundSequence.AddChild(_surroundTarget);
        
        _combatTacticSelector.AddChild(new AttackTargetTree(blackboard, agent, ()=> agent.TreeValues.CombatTactic.AttackTargetWeight + (agent.TreeValues.CombatTactic.IsAttackTargetModified ? agent.TreeValues.CombatTactic.AttackTargetMod : 0)));
        
        _combatTacticSelector.AddChild(new DefendSelfTree(blackboard, ()=> agent.TreeValues.CombatTactic.DefendSelfWeight + (agent.TreeValues.CombatTactic.IsDefendSelfModified ? agent.TreeValues.CombatTactic.DefendSelfMod : 0)));
        
        _combatTacticSelector.AddChild(_fleeBranch);
        _fleeBranch.AddChild(_healthCheck);
        _fleeBranch.AddChild(_retreat);
        
        _combatTacticSelector.AddChild(_flankSequence);
        _flankSequence.AddChild(_flankCheck);
        _flankSequence.AddChild(_flankTarget);
    }

    private float _enemyWeaponRange = 0;
    private GameObject enemyWeapon() {
        if(!blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _enemy)) return null;
        if(!_enemy.TryGetComponent(out Character _character)) return null;
        _enemyWeaponRange = _character.GetWeaponRange();
        return _character.Weapon.gameObject;
    }

    private GameObject targetEnemy() {
        blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _target);
        return _target;
    }
}