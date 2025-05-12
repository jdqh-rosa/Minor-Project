using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] BlackboardData blackboardData;
    
    private BehaviourTree tree;
    readonly Blackboard blackboard = new();
    

    [SerializeField] private Character enemyCharacter;

    private float findRadius = 10;

    private void Awake()
    {

        if (!enemyCharacter) enemyCharacter = GetComponent<Character>();
        
        blackboardData.SetValuesOnBlackboard(blackboard);
        
        blackboard.SetKeyValue(CommonKeys.ChosenPosition, enemyCharacter.transform.position);
        blackboard.SetKeyValue(CommonKeys.VisibleAllies, new List<GameObject>());
        blackboard.SetKeyValue(CommonKeys.VisibleTargets, new List<GameObject>());
        blackboard.SetKeyValue(CommonKeys.AgentSelf, this); 
        blackboard.SetKeyValue(CommonKeys.FindRadius, findRadius);
        
        tree = new BehaviourTree("Enemy");

        Repeater _repeater = new Repeater("BaseLogic");
        Parallel _parallel = new("BaseLogic/Parallel", 2);
        Parallel _actionParallel = new("BaseLogic/ActionParallel",2);
        PrioritySelector _prioritySelector = new PrioritySelector("BaseLogic//Selector");
        Leaf _moveToPosition = new("BaseLogic//MoveToPosition", new ActionStrategy(() => enemyCharacter.SetCharacterPosition(movePosition())));
        Leaf _positionWeapon = new Leaf("BaseLogic//AlignWeaponAngle", new ActionStrategy(() => enemyCharacter.RotateWeaponTowardsAngle(weaponAngle())));
        
        _repeater.AddChild(_parallel);
        _parallel.AddChild(_prioritySelector);
        _parallel.AddChild(_actionParallel);
        _actionParallel.AddChild(_moveToPosition);
        _actionParallel.AddChild(_positionWeapon);
        _prioritySelector.AddChild(new CombatTree(blackboard, 2));
        _prioritySelector.AddChild(new IdleTree(blackboard));
        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController agent);
        _prioritySelector.AddChild(new AssembleTree(blackboard, agent));
        
        tree.AddChild(_repeater);
        tree.Reset();
    }

    private void Update() {
        tree.Process();
    }

    private Vector2 moveDirection()
    {
        blackboard.TryGetValue(CommonKeys.ChosenPosition, out Vector3 _targetPosition);
        Vector3 moveDir = (_targetPosition - enemyCharacter.transform.position).normalized;
        return new Vector2(moveDir.x, moveDir.z);
    }
    
    private Vector3 movePosition()
    {
        blackboard.TryGetValue(CommonKeys.ChosenPosition, out Vector3 _targetPosition);
        return _targetPosition;
    }


    private float weaponAngle()
    {
        blackboard.TryGetValue(CommonKeys.ChosenWeaponAngle, out float _weaponAngle);
        return _weaponAngle;
    }

    public float GetWeaponRange()
    {
        return enemyCharacter.GetWeaponRange();
    }

    public float GetWeaponAngle()
    {
        return enemyCharacter.GetWeaponAngle();
    }

    public void ChooseAttack(ActionType pActionType, float pAttackAngle)
    {
        enemyCharacter.Attack(pActionType, pAttackAngle);
    }

}
