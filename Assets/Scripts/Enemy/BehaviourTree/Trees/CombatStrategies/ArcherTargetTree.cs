using System;
using System.Collections.Generic;
using Unity.VisualScripting;
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
        AddChild(new Leaf("ArcherAttack/LOSCheck", new ConditionStrategy(() =>
        {
            blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject target);
            if (target == null) return false;

            Vector3 weaponPosition = agent.gameObject.GetComponent<Character>().Weapon.GetTipPosition();

            Vector3 origin = agent.transform.position;
            Vector3 direction = (target.transform.position - origin).normalized;
            Vector3 weaponDirection = (target.transform.position - weaponPosition).normalized;
            direction.y = 0;
            weaponDirection.y = 0;
            float distance = Vector3.Distance(origin, target.transform.position);

            int mask = LayerMask.GetMask("Body");
            DrawLines.DrawLine(agent.transform.position, agent.transform.position + direction * attackRange,Color.magenta);
            if (!Physics.Raycast(origin, direction, out RaycastHit bodyHit, attackRange, mask)) return false;
            //DrawLines.DrawLine(agent.transform.position, agent.transform.position + weaponDirection * attackRange, Color.black);
            //if (!Physics.Raycast(origin, weaponDirection, out RaycastHit weaponHit, attackRange, mask)) return false;

            Character hitChar = bodyHit.collider.GetComponentInParent<Character>();
            if (!hitChar) return false;
            
            //Character hitWeaponChar = weaponHit.collider.GetComponentInParent<Character>();
            //if (!hitWeaponChar) return false;

            if (!blackboard.TryGetValue(CommonKeys.TeamSelf, out CharacterTeam team)) return false;
            return hitChar.GetCharacterInfo().Team != team;// && hitWeaponChar.GetCharacterInfo().Team != team;
        })));
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
