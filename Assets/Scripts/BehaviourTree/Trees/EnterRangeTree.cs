using UnityEngine;

public class EnterRangeTree : BehaviourTree
{
    Blackboard blackboard;
    private EnemyController agent;
    private Vector3 targetPosition;
    private float preferredRange;
    
    public EnterRangeTree(Blackboard pBlackboard, float pPreferredRange, int pPriority = 0) : base("EnterRange", pPriority)
    {
        blackboard = pBlackboard;
        preferredRange = pPreferredRange;
        
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        
        setup();
    }

    private void setup()
    {
        Parallel _parallel = new("EnterRange/Parallel", 2, 1);
        Leaf _withinRange = new Leaf("EnterRange///RangeCheck", new ConditionStrategy(rangeCheck));
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
    }

    private bool rangeCheck()
    {
        blackboard.TryGetValue(CommonKeys.TargetPosition, out Vector3 targetPosition);
        
        return (targetPosition - agent.transform.position).magnitude < preferredRange;
    }
    
    private Vector3 getTargetDifVector()
    {
        blackboard.TryGetValue(CommonKeys.TargetPosition, out Vector3 targetPosition);
        
        Vector3 _agentPos = agent.transform.position;
        Vector3 _difVector = targetPosition - _agentPos;
        return _difVector;
    }
}
