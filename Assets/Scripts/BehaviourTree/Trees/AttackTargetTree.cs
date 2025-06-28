using System;
using UnityEngine;

public class AttackTargetTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    private EnemyController agent;
    
    public AttackTargetTree(EnemyBlackboard pBlackboard, EnemyController pAgent, int pPriority = 0) : base("AttackTarget", pPriority)
    {
        blackboard = pBlackboard;
        agent = pAgent;
        
        setup();
    }

    public AttackTargetTree(EnemyBlackboard pBlackboard, EnemyController pAgent, Func<int> pDynamicPriority, int pFallback = 0) : base("Combat", pDynamicPriority, pFallback) {
        blackboard = pBlackboard;
        agent = pAgent;
        setup();
    }

    private void setup()
    {
        Parallel _parallel = new("AttackTarget/Parallel", 1);
        _parallel.AddChild(new Leaf("AttackTarget/Parallel/TargetCheck", new ConditionStrategy(() => targetEnemy())));
        _parallel.AddChild(new EnterRangeTree(blackboard, targetEnemy,agent.GetWeaponRange() -0.2f));
        _parallel.AddChild(new Leaf("AttackTarget//PointWeapon",  new ActionStrategy(pointWeapon)));
        AddChild(_parallel);
        AddChild(new ChooseAttackTree(blackboard));
        AddChild(new AttackTree(blackboard, agent));
    }

    private GameObject targetEnemy() {
        blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _enemy);
        return _enemy;
    }
    
    void pointWeapon()
    {
        blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _target);
        Vector3 _agentPos = agent.transform.position;
        Vector3 _difVector = _target.transform.position - _agentPos;
        blackboard.SetKeyValue(CommonKeys.ChosenWeaponAngle, RadialHelper.CartesianToPol(new Vector2(_difVector.x, _difVector.z)).y);
    }
}
