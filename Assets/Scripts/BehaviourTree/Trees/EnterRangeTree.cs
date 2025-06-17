using UnityEngine;

public class EnterRangeTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    private EnemyController agent;
    private Vector3 targetPosition;
    private float preferredRange;
    
    public EnterRangeTree(EnemyBlackboard pBlackboard, float pPreferredRange, int pPriority = 0) : base("EnterRange", pPriority)
    {
        blackboard = pBlackboard;
        preferredRange = pPreferredRange;
        
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        
        setup();
    }

    private void setup()
    {
        Parallel _parallel = new("EnterRange/Parallel", 2);
        Leaf _withinRange = new Leaf("EnterRange///RangeCheck", new ConditionStrategy(()=> (GetTargetPosition() - agent.transform.position).magnitude < preferredRange));
        Leaf _prefPosition = new Leaf("EnterRange///PreferredPosition", new ActionStrategy(calcPrefPos));
        
        AddChild(_parallel);
        _parallel.AddChild(_withinRange);
        _parallel.AddChild(_prefPosition);
    }

    private void calcPrefPos()
    {
        Vector3 _difVector = getTargetDifVector();
        Vector3 _prefDif = _difVector - _difVector.normalized * (agent.GetWeaponRange() -0.1f);
        blackboard.SetKeyValue(CommonKeys.ChosenPosition, _prefDif + agent.transform.position);
        blackboard.AddForce(_prefDif, 1f, "EnterRange");
    }
    
    private Vector3 getTargetDifVector() {
        targetPosition = GetTargetPosition();
        Vector3 _agentPos = agent.transform.position;
        Vector3 _difVector = targetPosition - _agentPos;
        return _difVector;
    }

    private Vector3 GetTargetPosition() {
        switch (blackboard.GetActiveTargetType()) {
            case TargetType.Enemy:
                blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _targetEnemy);
                return _targetEnemy.transform.position;
            case TargetType.Object:
                blackboard.TryGetValue(CommonKeys.TargetObject, out GameObject _targetObject);
                return _targetObject.transform.position;
            case TargetType.Ally:
                blackboard.TryGetValue(CommonKeys.TargetAlly, out GameObject _targetAlly);
                return _targetAlly.transform.position;
            default:
                blackboard.TryGetValue(CommonKeys.TargetPosition, out Vector3 _targetPosition);
                return _targetPosition;
        }
    }
}
