using UnityEngine;

public class AttackTree : BehaviourTree
{
    Blackboard blackboard;
    private EnemyController agent;

    public AttackTree(Blackboard pBlackboard, EnemyController pAgent, int pPriority = 0) : base("DoAttack", pPriority)
    {
        blackboard = pBlackboard;
        agent = pAgent;
        
        setup();
    }

    private void setup()
    {
        Sequence _attackSequence = new("AttackSequence");
        Parallel _attackParallel = new("Attack//Parallel", 1);
        Leaf _atkRangeCheck = new("Attack//RangeCheck", new ConditionStrategy(attackRangeCheck));
        Parallel _parallel = new("Attack///AttackParallel", 2);
        Leaf _executeAttack = new("ExecuteAttack", new ActionStrategy(attack));
        Leaf _angleCheck = new("DoAttack//AngleCheck", new ConditionStrategy(attackAngleCheck));
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
    
    bool attackRangeCheck()
    {
        return (getTarget().transform.position - agent.transform.position).magnitude < agent.GetWeaponRange();
    }

    bool attackAngleCheck()
    {
        float weaponAngle = agent.GetWeaponAngle();
        float targetAngle = this.targetAngle();
        float deltaAngle = Mathf.DeltaAngle(weaponAngle, targetAngle);
        return deltaAngle is >= -90 and <= 90 ;
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

    void attack()
    {
        blackboard.TryGetValue(CommonKeys.ChosenAttack, out ActionType _attackType);
        
        agent.ChooseAttack(_attackType, targetAngle());
    }

    void alignAttackAngle()
    {
        //todo: get weapon attack angle and align with that
        blackboard.SetKeyValue(CommonKeys.ChosenWeaponAngle, targetAngle());
    }

    void alignAttackPosition()
    {
        //todo: get weapon attack angle and align with that

        Vector3 _weaponTipPosition = MiscHelper.Vec2ToVec3Pos(RadialHelper.PolarToCart(agent.GetWeaponAngle(), agent.GetWeaponRange()));
        blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject target);

        Vector3 positionOffset = target.transform.position - _weaponTipPosition;
        
        blackboard.SetKeyValue(CommonKeys.ChosenPosition, agent.transform.position + positionOffset);
    }
}