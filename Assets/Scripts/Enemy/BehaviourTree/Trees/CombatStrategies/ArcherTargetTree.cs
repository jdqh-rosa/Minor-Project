using System;
using System.Collections.Generic;
using UnityEngine;

public class ArcherTargetTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    private EnemyController agent;
    private float attackRange;
    
    public ArcherTargetTree(EnemyBlackboard pBlackboard, EnemyController pAgent, Func<int> pDynamicPriority, int pFallback = 0) : base("Combat", pDynamicPriority, pFallback) {
        blackboard = pBlackboard;
        agent = pAgent;

        attackRange = getAttackRange() + agent.GetWeaponRange();
        
        setup();
    }

    private void setup() {
        
        
        AddChild(new Leaf("ShowTactic", new ActionStrategy(() => agent.GetComponent<TacticIndicator>().SetTactic(TacticType.Attack))));
        AddChild(new Leaf("AttackTarget/TargetCheck", new ConditionStrategy(() => HasValidTarget())));
        AddChild(new EnterRangeTree(blackboard, targetEnemy,attackRange));
        AddChild(new Leaf("Combat/PointWeapon", new AngleWeaponAtTargetStrategy(blackboard, CommonKeys.TargetEnemy)));
        AddChild(new Leaf("AttackTarget/RangeCheck", new ConditionStrategy(()=>
        {
            if (!blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _targetEnemy) || !_targetEnemy) {
                return false;
            }
            Vector3 _delta = _targetEnemy.transform.position - agent.transform.position;
            _delta.y = 0;
            return _delta.magnitude < attackRange;
        })));
        //AddChild(new Leaf("ArcherLOS", new LineOfSightStrategy(blackboard, attackRange)));
        RandomSelector randomSelector = new RandomSelector("ArcherAttackSelect");
        randomSelector.AddChild(new Leaf("ArcherWeakArrow", new ActionStrategy(() => chooseAttack(ActionType.Arrow)), ()=> agent.TreeValues.CombatAttack.WeakStabWeight));
        randomSelector.AddChild(new Leaf("ArcherStrongArrow", new ActionStrategy(() => chooseAttack(ActionType.StrongArrow)), ()=> agent.TreeValues.CombatAttack.StrongStabWeight));
        AddChild(randomSelector);
        AddChild(new AttackTree(blackboard, agent));
    }
    
    private void chooseAttack(ActionType attackType)
    {
        blackboard.SetKeyValue(CommonKeys.ChosenAttack, attackType);
    }

    private GameObject targetEnemy() {
        blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _enemy);
        return !_enemy ? null : _enemy;
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
