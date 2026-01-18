using System;
using Unity.VisualScripting;
using UnityEngine;

public class AttackTargetTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    private EnemyController agent;
    
    public AttackTargetTree(EnemyBlackboard pBlackboard, EnemyController pAgent, Func<int> pDynamicPriority, int pFallback = 0) : base("Combat", pDynamicPriority, pFallback) {
        blackboard = pBlackboard;
        agent = pAgent;
        setup();
    }

    private void setup() {
        AddChild(new Leaf("AttackTarget/TargetCheck", new ConditionStrategy(() => HasValidTarget())));
        AddChild(new EnterRangeTree(blackboard, targetEnemy,agent.GetWeaponRange()-0.5f));
        AddChild(new Leaf("AttackTarget/WeaponAwareCombat", new WeaponAwareCombatStrategy(blackboard)));
        //AddChild(new Leaf("AttackTarget/DistanceWeapon", new DistanceSelfFromTargetWeaponStrategy(blackboard)));
        AddChild(new Leaf("AttackTarget/PointWeapon",  new ActionStrategy(()=> pointWeapon())));
        AddChild(new Leaf("AttackTarget/RangeCheck", new ConditionStrategy(()=>
        {
            if (!blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _targetEnemy) || !_targetEnemy) {
                Debug.Log($"no target found for {agent.name}");
                return false;
            }
            Vector3 _delta = _targetEnemy.transform.position - agent.transform.position;
            _delta.y = 0;
            Debug.Log($"{_delta.magnitude < agent.GetWeaponMaxRange()} weapon max range {agent.GetWeaponMaxRange()} at {_delta.magnitude}");
            return _delta.magnitude < agent.GetWeaponMaxRange();
        })));
        AddChild(new ChooseAttackTree(blackboard));
        AddChild(new AttackTree(blackboard, agent));
        //Selector attackOrParry = new Selector("AttackOrParry");
        //attackOrParry.AddChild(new Leaf("OffensiveParry", new OffensiveParryStrategy(blackboard)));
        //attackOrParry.AddChild(new AttackTree(blackboard, agent));
        //AddChild(attackOrParry);
    }

    private GameObject targetEnemy() {
        blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _enemy);
        return !_enemy ? null : _enemy;
    }
    
    void pointWeapon()
    {
        if(!blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _target) || !_target) return;
        
        Vector3 _difVector = _target.transform.position - agent.transform.position;
        blackboard.SetKeyValue(CommonKeys.ChosenWeaponAngle, RadialHelper.CartesianToPol(new Vector2(_difVector.x, _difVector.z)).y);
    }
    
    bool HasValidTarget()
    {
        return blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject enemy)&& enemy && enemy.TryGetComponent(out Character _);
    }
}
