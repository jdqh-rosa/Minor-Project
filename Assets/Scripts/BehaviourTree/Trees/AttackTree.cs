using System.Collections.Generic;
using UnityEngine;

public class AttackTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    private EnemyController agent;

    public AttackTree(EnemyBlackboard pBlackboard, EnemyController pAgent, int pPriority = 0) : base("DoAttack", pPriority)
    {
        blackboard = pBlackboard;
        agent = pAgent;
        
        setup();
    }

    private void setup()
    {
        Sequence _attackSequence = new("AttackSequence");
        Parallel _attackParallel = new("Attack//Parallel", 1);
        Leaf _atkRangeCheck = new("Attack//RangeCheck", new ConditionStrategy(()=> (getTarget().transform.position - agent.transform.position).magnitude < agent.GetWeaponRange()));
        Parallel _parallel = new("Attack///AttackParallel", 2);
        Leaf _executeAttack = new("ExecuteAttack", new ActionStrategy(()=>
        {
            blackboard.TryGetValue(CommonKeys.ChosenAttack, out ActionType _attackType);
            agent.InitiateAttackAction(_attackType, targetAngle());
        }));
        Leaf _angleCheck = new("DoAttack//AngleCheck", new ConditionStrategy(()=> { 
            float _deltaAngle = deltaAngle();
            float _attackAngle = idealAttackAngle();
            return _deltaAngle >= -_attackAngle && _deltaAngle <= _attackAngle ; }));
        RandomSelector _randomSelector = new("DoAttack//RandomSelector");
        Leaf _adjustAngle = new("DoAttack//RandSelector/AdjustAngle", new ActionStrategy(alignAttackAngle));
        Leaf _adjustPosition = new("DoAttack//RandSelector/AdjustPosition", new ActionStrategy(alignAttackPosition));

        AddChild(_attackSequence);
        _attackSequence.AddChild(_attackParallel);
        _attackSequence.AddChild(_executeAttack);
        _attackParallel.AddChild(_atkRangeCheck);
        _attackParallel.AddChild(_parallel);
        _parallel.AddChild(_angleCheck);
        _parallel.AddChild(_randomSelector);
        _randomSelector.AddChild(_adjustAngle);
        _randomSelector.AddChild(_adjustPosition);
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

    void alignAttackAngle()
    {
        //todo: get weapon attack angle and align with that
        
        float _attackAngle = agent.GetWeaponAngle();
        float _deltaAngle = deltaAngle();
        _attackAngle += (_deltaAngle >= 0) ? _deltaAngle : -_deltaAngle;
        
        blackboard.SetKeyValue(CommonKeys.ChosenWeaponAngle, RadialHelper.NormalizeAngle(_attackAngle));
    }

    float idealAttackAngle()
    {
        blackboard.TryGetValue(CommonKeys.ChosenAttack, out ActionType _attackType);
        blackboard.TryGetValue(CommonKeys.AttackActions, out Dictionary<ActionType, CombatStateData> _actions);
        float _attackAngle = _actions[_attackType].IdealAttackAngle;
        return _attackAngle;
    }

    void alignAttackPosition()
    {
        Vector3 _weaponTipPosition = MiscHelper.Vec2ToVec3Pos(RadialHelper.PolarToCart(agent.GetWeaponAngle(), agent.GetWeaponRange()));
        blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject target);

        Vector3 positionOffset = (target.transform.position - agent.transform.position) - _weaponTipPosition;
        
        blackboard.SetKeyValue(CommonKeys.TargetPosition, agent.transform.position + positionOffset);
        Vector2 dir = (positionOffset).normalized;
        blackboard.AddForce(dir, agent.TreeValues.Movement.AlignAttackForce, "Aligned_AttackPosition");
        
        blackboard.SetKeyValue(CommonKeys.ActiveTarget, TargetType.None);
    }
}