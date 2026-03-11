using System;
using System.Collections.Generic;
using UnityEngine;

public class AttackTargetTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    private EnemyController agent;
    private float attackRange;
    
    public AttackTargetTree(EnemyBlackboard pBlackboard, EnemyController pAgent, Func<int> pDynamicPriority, int pFallback = 0) : base("Combat", pDynamicPriority, pFallback) {
        blackboard = pBlackboard;
        agent = pAgent;

        attackRange = getAttackRange() + agent.GetWeaponRange();
        
        setup();
    }

    private void setup() {
        AddChild(new Leaf("ShowTactic", new ActionStrategy(() => agent.GetComponent<TacticIndicator>().SetTactic(TacticType.Attack))));
        AddChild(new Leaf("AttackTarget/TargetCheck", new ConditionStrategy(() => HasValidTarget())));
        AddChild(new EnterRangeTree(blackboard, targetEnemy,attackRange));
        //AddChild(new Leaf("AttackTarget/WeaponAwareCombat", new WeaponAwareCombatStrategy(blackboard)));
        //AddChild(new Leaf("AttackTarget/DistanceWeapon", new DistanceSelfFromTargetWeaponStrategy(blackboard)));
        AddChild(new Leaf("AttackTarget/PointWeapon",  new ActionStrategy(()=> pointWeapon())));
        AddChild(new Leaf("AttackTarget/RangeCheck", new ConditionStrategy(()=>
        {
            if (!blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _targetEnemy) || !_targetEnemy) {
                return false;
            }
            Vector3 _delta = _targetEnemy.transform.position - agent.transform.position;
            _delta.y = 0;
            return _delta.magnitude < agent.GetWeaponMaxRange();
        })));
        AddChild(new ChooseMeleeAttackTree(blackboard));
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
    
    private float getAttackRange() {
        List<AttackInputEntry> attackList = agent.gameObject.GetComponent<Character>().Weapon.GetWeaponData().AttackInputMap;
        
        float shortest = float.MaxValue;
        foreach (AttackInputEntry attack in attackList) {
            if (shortest > attack.ActionData.AttackRange) shortest = attack.ActionData.AttackRange;
        }
        return shortest;
    }
}
