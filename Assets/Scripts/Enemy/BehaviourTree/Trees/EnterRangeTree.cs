using UnityEngine;
using System;
using Unity.Burst.Intrinsics;

public class EnterRangeTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    private EnemyController agent;
    private Func<GameObject> targetObject;
    private Vector3 targetPosition;
    private float preferredRange;
    
    public EnterRangeTree(EnemyBlackboard pBlackboard, Func<GameObject> pTargetObject, float pPreferredRange, int pPriority = 0) : base("EnterRange", pPriority)
    {
        blackboard = pBlackboard;
        preferredRange = pPreferredRange;
        targetObject = pTargetObject;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        
        setup();
    }

    private void setup()
    {
        Leaf _targetCheck = new("EnterRange///TargetCheck", new ConditionStrategy(targetCheck));
        Selector _rangeSelector  = new("EnterRange///RangeSelector", 1);
        Leaf _withinRange = new Leaf("EnterRange////RangeCheck", new ConditionStrategy(()=>
        {
            Vector3 delta = GetTargetPosition() - agent.transform.position;
            delta.y = 0;
            if (delta.magnitude < preferredRange) {
                //Debug.Log($"Delta is {delta.magnitude}");
            }
            return delta.magnitude < preferredRange;
        }));
        Leaf _movementAction = new("EnterRange////MovementAction", new MovementActionStrategy(blackboard,()=> getTargetDifVector(), preferredRange));
        Leaf _prefPosition = new Leaf("EnterRange//PreferredPosition", new ActionStrategy(calcPrefPos));
        
        AddChild(_targetCheck);
        AddChild(_rangeSelector);
        _rangeSelector.AddChild(_withinRange);
        _rangeSelector.AddChild(_movementAction);
        AddChild(_prefPosition);
    }

    private void calcPrefPos()
    {
        Vector3 _difVector = getTargetDifVector();
        if (_difVector.magnitude < preferredRange) return;
        Vector3 _prefDif = _difVector - _difVector.normalized * agent.GetWeaponRange();
        blackboard.SetKeyValue(CommonKeys.ChosenPosition, _prefDif + agent.transform.position);
        blackboard.AddForce(_prefDif, agent.TreeValues.Movement.EnterRangeForce, "EnterRange");
    }
    
    private Vector3 getTargetDifVector() {
        //targetPosition = GetTargetPosition();
        GameObject _target = targetObject.Invoke();
        if(!_target) return Vector3.zero;
        targetPosition = _target.transform.position;
        Vector3 _agentPos = agent.transform.position;
        Vector3 _difVector = targetPosition - _agentPos;
        _difVector.y = 0;
        return _difVector;
    }

    private bool targetCheck() {
        switch (blackboard.GetActiveTargetType()) {
            case TargetType.Enemy:
                return blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _targetEnemy) && _targetEnemy;
            case TargetType.Object:
                return blackboard.TryGetValue(CommonKeys.TargetObject, out GameObject _targetObject) && _targetObject;
            case TargetType.Ally:
                return blackboard.TryGetValue(CommonKeys.TargetAlly, out GameObject _targetAlly) && _targetAlly;
            default:
                return blackboard.TryGetValue(CommonKeys.TargetPosition, out Vector3 _targetPosition) && _targetPosition != Vector3.zero;
        }
    }

    private Vector3 GetTargetPosition() {
        switch (blackboard.GetActiveTargetType()) {
            case TargetType.Enemy:
                if(blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _targetEnemy) && _targetEnemy) return _targetEnemy.transform.position;
                break;
            case TargetType.Object:
                if(blackboard.TryGetValue(CommonKeys.TargetObject, out GameObject _targetObject) && _targetObject) return _targetObject.transform.position;
                break;
            case TargetType.Ally:
                if(blackboard.TryGetValue(CommonKeys.TargetAlly, out GameObject _targetAlly) && _targetAlly) return _targetAlly.transform.position;
                break;
            default:
                if(blackboard.TryGetValue(CommonKeys.TargetPosition, out Vector3 _targetPosition) && _targetPosition != Vector3.zero) return _targetPosition;
                break;
        }
        return agent.transform.position;
    }
}
