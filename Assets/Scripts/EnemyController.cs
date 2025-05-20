using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] BlackboardData blackboardData;
    
    private BehaviourTree tree;
    readonly EnemyBlackboard blackboard = new();

    [SerializeField] private Character enemyCharacter;

    private float findRadius = 10;

    private void Awake()
    {
        if (!enemyCharacter) enemyCharacter = GetComponent<Character>();
        
        blackboard.AddCharacterData(enemyCharacter.GetData());
        blackboardData.SetValuesOnBlackboard(blackboard);
        blackboard.SetKeyValue(CommonKeys.ChosenPosition, enemyCharacter.transform.position);
        blackboard.SetKeyValue(CommonKeys.VisibleAllies, new List<GameObject>());
        blackboard.SetKeyValue(CommonKeys.VisibleTargets, new List<GameObject>());
        blackboard.SetKeyValue(CommonKeys.AgentSelf, this); 
        blackboard.SetKeyValue(CommonKeys.FindRadius, findRadius);
        
        tree = new BehaviourTree("Enemy");

        Repeater _repeater = new Repeater("BaseLogic");
        Parallel _parallel = new("BaseLogic/Parallel", 2);
        PrioritySelector _prioritySelector = new PrioritySelector("BaseLogic//BranchSelector");
        Parallel _actionParallel = new("BaseLogic/ActionParallel",2);
        Leaf _moveToPosition = new("BaseLogic//MoveToPosition", new ActionStrategy(() => enemyCharacter.SetCharacterPosition(movePosition())));
        Leaf _positionWeapon = new Leaf("BaseLogic//AlignWeaponAngle", new ActionStrategy(() => enemyCharacter.RotateWeaponTowardsAngle(weaponAngle())));
        
        _repeater.AddChild(_parallel);
        _parallel.AddChild(_prioritySelector);
        _parallel.AddChild(_actionParallel);
        _actionParallel.AddChild(_moveToPosition);
        _actionParallel.AddChild(_positionWeapon);

        Sequence _combatBranch = new("Base//CombatSequence");
        _combatBranch.AddChild(new Leaf("CheckCombat", new ConditionStrategy(() =>
        {
            blackboard.TryGetValue(CommonKeys.VisibleEnemies, out List<GameObject> targets);
            return targets is { Count: > 0 };
        })));
        _combatBranch.AddChild(new CombatTree(blackboard));
        
        Sequence _assembleBranch = new("Base//AssembleSequence");
        _assembleBranch.AddChild(new Leaf("CheckAssemble", new ConditionStrategy(() =>
        {
            blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> targets);
            return targets is { Count: > 0 };
        })));
        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController agent);

        _assembleBranch.AddChild(new AssembleTree(blackboard, agent));
        
        _prioritySelector.AddChild(_combatBranch);
        _prioritySelector.AddChild(_assembleBranch);
        _prioritySelector.AddChild(new IdleTree(blackboard));
        
        tree.AddChild(_repeater);
        tree.Reset();
        
        AddBTDebugHUD();
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
    
    void OnDrawGizmos() {
        if (tree == null) return;
        DrawNodeGizmo(tree, Vector3.up * 2);
    }

    void DrawNodeGizmo(Node node, Vector3 pos) {
        Gizmos.color = node.IsActive ? Color.green : Color.gray;
        Gizmos.DrawSphere(pos, 0.1f);
        for (int i = 0; i < node.children.Count; i++) {
            Vector3 childPos = pos + new Vector3((i - node.children.Count/2f)*0.5f, -0.5f, 0);
            Gizmos.DrawLine(pos, childPos);
            DrawNodeGizmo(node.children[i], childPos);
        }
    }

    private void AddBTDebugHUD() {
        GetComponent<BTDebugHUD>().tree = tree;
    }

}
