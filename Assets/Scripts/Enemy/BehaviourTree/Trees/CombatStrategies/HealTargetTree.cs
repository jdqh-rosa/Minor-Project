using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HealTargetTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    private EnemyController agent;
    private float attackRange = 0.5f;
    
    public HealTargetTree(EnemyBlackboard pBlackboard, EnemyController pAgent, Func<int> pDynamicPriority, int pFallback = 0) : base("Combat", pDynamicPriority, pFallback) {
        blackboard = pBlackboard;
        agent = pAgent;
        setup();
    }

    private void setup() {

        attackRange = getAttackRange();
        AddChild(new Leaf("ShowTactic", new ActionStrategy(() => agent.GetComponent<TacticIndicator>().SetTactic(TacticType.Flee))));
        AddChild(new Leaf("AttackTarget/TargetCheck", new ConditionStrategy(() => HasValidTarget())));
        AddChild(new EnterRangeTree(blackboard, targetAlly, attackRange));
        AddChild(new Leaf("AttackTarget/WeaponAwareCombat", new WeaponAwareCombatStrategy(blackboard)));
        //AddChild(new Leaf("AttackTarget/DistanceWeapon", new DistanceSelfFromTargetWeaponStrategy(blackboard)));
        AddChild(new Leaf("AttackTarget/PointWeapon",  new AngleWeaponAtTargetStrategy(blackboard, CommonKeys.LowestHealthAlly)));
        AddChild(new Leaf("AttackTarget/RangeCheck", new ConditionStrategy(()=>
        {
            if (!blackboard.TryGetValue(CommonKeys.LowestHealthAlly, out GameObject _targetAlly) || !_targetAlly) {
                Debug.Log($"no target found for {agent.name}");
                return false;
            }
            Vector3 _delta = _targetAlly.transform.position - agent.transform.position;
            _delta.y = 0;
            return _delta.magnitude < attackRange;
        })));
        AddChild(new Leaf("ExecuteHeal", new ActionStrategy(()=>
        {
            blackboard.TryGetValue(CommonKeys.ChosenWeaponAngle, out float targetAngle);
            agent.InitiateAttackAction(ActionType.Heal, targetAngle);
        })));
    }

    private GameObject targetAlly() {
        blackboard.TryGetValue(CommonKeys.LowestHealthAlly, out GameObject _enemy);
        return !_enemy ? null : _enemy;
    }

    private float getAttackRange() {
        List<AttackInputEntry> attackList = agent.gameObject.GetComponent<Character>().Weapon.GetWeaponData().AttackInputMap;
        
        float shortest = float.MaxValue;
        foreach (AttackInputEntry attack in attackList) {
            if (shortest > attack.ActionData.AttackRange) shortest = attack.ActionData.AttackRange;
        }
        return shortest;
    }
    
    bool HasValidTarget()
    {
        return blackboard.TryGetValue(CommonKeys.LowestHealthAlly, out GameObject ally)&& ally && ally.TryGetComponent(out Character _);
    }
}
