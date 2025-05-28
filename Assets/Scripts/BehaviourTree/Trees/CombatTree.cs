using System;
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
        Sequence _sequence = new Sequence("CombatBaseSeq");
        Leaf _findEnemiesAction = new Leaf("Combat/FindTargets", new FindEnemiesStrategy(blackboard));
        Leaf _obtainEnemy = new Leaf("Combat/ObtainTarget", new GetClosestEnemyStrategy(blackboard));
        Sequence _targetedSequence = new Sequence("Combat/TargetSeq");
        
        blackboard.TryGetValue(CommonKeys.SelfHealth, out int _charHealth);
        Leaf _healthCheck = new Leaf("Combat/TargetSeq/HealthCheck", new ConditionStrategy(() => _charHealth < 10));
        Leaf _detectAttack = new("Combat/DetectAttack", new DetectAttackStrategy(blackboard));
        PrioritySelector _prioritySelector = new PrioritySelector("Combat/TargetSeq/RandSel");
        Leaf _distanceSelfFromWeapon = new("Combat/DistanceWeapon", new DistanceSelfFromObjectStrategy(blackboard, enemyWeapon(), _enemyWeaponRange));
        Leaf _findAllies = new Leaf("Combat/TargetSeq/RandSel/FindAllies", new FindAlliesStrategy(blackboard));

        _sequence.AddChild(_findEnemiesAction);
        _sequence.AddChild(_obtainEnemy);
        _sequence.AddChild(_targetedSequence);
        _targetedSequence.AddChild(_healthCheck);
        _targetedSequence.AddChild(_detectAttack);
        _targetedSequence.AddChild(_prioritySelector);
        _targetedSequence.AddChild(_distanceSelfFromWeapon);
        _prioritySelector.AddChild(new AttackTargetTree(blackboard, agent));
        _prioritySelector.AddChild(new DefendSelfTree(blackboard, defensePriority()));
        _prioritySelector.AddChild(_findAllies);

        AddChild(_sequence);
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
}