using System.Collections.Generic;
using UnityEngine;

public class AttackTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    private EnemyController agent;
    private float attackRange;

    public AttackTree(EnemyBlackboard pBlackboard, EnemyController pAgent, int pPriority = 0) : base("DoAttack", pPriority)
    {
        blackboard = pBlackboard;
        agent = pAgent;
        attackRange = getAttackRange() + agent.GetWeaponRange();
        
        setup();
    }

    private void setup()
    {
        Sequence _attackSequence = new("AttackSequence");
        Leaf _atkRangeCheck = new("Attack//RangeCheck", new ConditionStrategy(()=>
        {
            Vector3 delta = getTarget().transform.position - agent.transform.position;
            delta.y = 0;
            return delta.magnitude < attackRange;
        }));
        Sequence _sequence = new("Attack///AttackSequence", 2);
        Leaf _executeAttack = new("ExecuteAttack", new ActionStrategy(()=>
        {
            Debug.Log($"execute attack");

            blackboard.TryGetValue(CommonKeys.ChosenAttack, out ActionType _attackType);
            agent.InitiateAttackAction(_attackType, targetAngle());
        }));
        Leaf _angleCheck = new("DoAttack//AngleCheck", new ConditionStrategy(()=> { 
            float _deltaAngle = deltaAngle();
            float _attackAngle = idealAttackAngle();
            return _deltaAngle >= -_attackAngle && _deltaAngle <= _attackAngle ; }));
        //RandomSelector _randomSelector = new("DoAttack//RandomSelector");
        Selector _alignSelector = new("DoAttack//AlignSelector");
        Leaf _adjustAngle = new("DoAttack//RandSelector/AdjustAngle", new ActionStrategy(() =>
        {
            float _currAngle = agent.GetWeaponAngle();
            float _deltaAngle = deltaAngle();
            _currAngle += _deltaAngle;
            blackboard.SetKeyValue(CommonKeys.ChosenWeaponAngle, RadialHelper.NormalizeAngle(_currAngle));
        }));
        Leaf _adjustPosition = new("DoAttack//RandSelector/AdjustPosition", new ActionStrategy(() =>
        {
            Vector3 _weaponTipPosition = MiscHelper.Vec2ToVec3Pos(RadialHelper.PolarToCart(agent.GetWeaponAngle(), attackRange));
            blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject target);
            Vector3 positionOffset = (target.transform.position - agent.transform.position) - _weaponTipPosition;
            blackboard.SetKeyValue(CommonKeys.TargetPosition, agent.transform.position + positionOffset);
            Vector2 dir = (positionOffset).normalized;
            blackboard.AddForce(dir, agent.TreeValues.Movement.AlignAttackForce, "Aligned_AttackPosition");
            blackboard.SetKeyValue(CommonKeys.ActiveTarget, TargetType.None);
        }));
        Selector _attackSelector = new("Attack/AttackSelector");
        Leaf _offensiveParry = new Leaf( "Attack/OffensiveParry", new OffensiveParryStrategy(blackboard));

        AddChild(_attackSequence);
        _attackSequence.AddChild(_atkRangeCheck);
        _attackSequence.AddChild(_alignSelector);
        _attackSequence.AddChild(_attackSelector);
        _attackSelector.AddChild(_offensiveParry);
        _attackSelector.AddChild(_executeAttack);
        _alignSelector.AddChild(_angleCheck);
        _alignSelector.AddChild(_sequence);
        _sequence.AddChild(_adjustAngle);
        _sequence.AddChild(_adjustPosition);
    }
    
    float deltaAngle()
    {
        float _weaponAngle = agent.GetWeaponAngle();
        float _targetAngle = targetAngle();
        return Mathf.DeltaAngle(_weaponAngle, _targetAngle);
    }
    
    float targetAngle()
    {
        Vector3 _targetAngleVector = getTargetDifVector();
        return RadialHelper.CartesianToPol(new Vector2(_targetAngleVector.x, _targetAngleVector.z)).y;
    }
    
    private Vector3 getTargetDifVector()
    {
        Vector3 _agentPos = agent.transform.position;
        Vector3 _difVector = getTarget().transform.position - _agentPos;
        return _difVector;
    }
    
    GameObject getTarget()
    {
        blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _target);
        return _target;
    }
    
    float idealAttackAngle()
    {
        blackboard.TryGetValue(CommonKeys.ChosenAttack, out ActionType _attackType);
        blackboard.TryGetValue(CommonKeys.AttackActions, out Dictionary<ActionType, CombatStateData> _actions);
        float _attackAngle = _actions[_attackType].IdealAttackAngle;
        return _attackAngle;
    }
    
    private float getAttackRange() {
        List<AttackInputEntry> attackList = agent.gameObject.GetComponent<Character>().Weapon.GetWeaponData().AttackInputMap;
        
        float shortest = float.MaxValue;
        foreach (AttackInputEntry attack in attackList) {
            if (shortest > attack.ActionData.AttackRange) shortest = attack.ActionData.AttackRange;
        }
        return shortest;
    }
}