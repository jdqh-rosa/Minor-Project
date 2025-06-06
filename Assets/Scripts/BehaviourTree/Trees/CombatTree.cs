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
        
        PrioritySelector _combatTacticSelector = new PrioritySelector("Combat/TargetSeq/CombatTacticSel");
        Leaf _distanceSelfFromWeapon = new("Combat/DistanceWeapon", new DistanceSelfFromObjectStrategy(blackboard, enemyWeapon(), _enemyWeaponRange));
        
        Sequence _flankSequence = new Sequence("Combat///FlankSeq");
        Leaf _flankCheck = new("Combat///FlankCheck", new ConditionStrategy(() =>
        {
            blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> visibleAllies);
            return visibleAllies.Count > 1;
        }));
        Leaf _flankTarget = new("Combat///FlankTarget", new FlankStrategy(blackboard));
        
        Sequence _surroundSequence = new Sequence("Combat//FlankSeq");
        Leaf _surroundCheck = new("Combat///FlankCheck", new ConditionStrategy(() =>
        {
            blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> visibleAllies);
            return visibleAllies.Count > 3;
        }));
        Leaf _surroundTarget = new("Combat///SurroundTarget", new SurroundTargetStrategy(blackboard));
        
        Sequence _fleeBranch = new Sequence("Combat//FleeBranch");
        Leaf _healthCheck = new Leaf("Combat/TargetSeq/HealthCheck", new ConditionStrategy(() => agentHealth() < 10));
        Leaf _retreat = new("Combat/FleeBranch/Retreat", new RetreatFromTargetStrategy(blackboard, targetEnemy()));
        
        AddChild(_baseCombatSequence);
        _baseCombatSequence.AddChild(_obtainEnemy);
        _baseCombatSequence.AddChild(_combatTacticSelector);
        _baseCombatSequence.AddChild(_distanceSelfFromWeapon);
        
        _combatTacticSelector.AddChild(_surroundSequence);
        _surroundSequence.AddChild(_surroundCheck);
        _surroundSequence.AddChild(_surroundTarget);
        
        _combatTacticSelector.AddChild(new AttackTargetTree(blackboard, agent));
        
        _combatTacticSelector.AddChild(new DefendSelfTree(blackboard, defensePriority()));
        
        _combatTacticSelector.AddChild(_fleeBranch);
        _fleeBranch.AddChild(_healthCheck);
        _fleeBranch.AddChild(_retreat);
        
        _combatTacticSelector.AddChild(_flankSequence);
        _flankSequence.AddChild(_flankCheck);
        _flankSequence.AddChild(_flankTarget);
    }
    
    private int defensePriority() {
        blackboard.TryGetValue(CommonKeys.ChosenAction, out ActionType _actionType);
        if (_actionType == ActionType.Parry || _actionType == ActionType.Dodge) return 3;
        return 0;
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

    private float agentHealth() {
        blackboard.TryGetValue(CommonKeys.SelfHealth, out float _agentHealth);
        return _agentHealth;
    }
}