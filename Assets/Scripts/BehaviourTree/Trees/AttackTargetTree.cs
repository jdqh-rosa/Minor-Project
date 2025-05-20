using UnityEngine;

public class AttackTargetTree : BehaviourTree
{
    Blackboard blackboard;
    private EnemyController agent;
    
    public AttackTargetTree(Blackboard pBlackboard, EnemyController pAgent, int pPriority = 0) : base("AttackTarget", pPriority)
    {
        blackboard = pBlackboard;
        agent = pAgent;
        
        setup();
    }

    private void setup()
    {
        Parallel _parallel = new("AttackTarget/Parallel", 1);
        _parallel.AddChild(new EnterRangeTree(blackboard, agent.GetWeaponRange()));
        //_parallel.AddChild(new Leaf("AttackTarget//PointWeapon",  new ActionStrategy(pointWeapon)));
        AddChild(_parallel);
        AddChild(new ChooseAttackTree(blackboard));
        AddChild(new AttackTree(blackboard, agent));
    }
    
    void pointWeapon()
    {
        blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _target);
        Vector3 _agentPos = agent.transform.position;
        Vector3 _difVector = _target.transform.position - _agentPos;
        blackboard.SetKeyValue(CommonKeys.ChosenWeaponAngle, RadialHelper.CartesianToPol(new Vector2(_difVector.x, _difVector.z)).y);
    }
}
