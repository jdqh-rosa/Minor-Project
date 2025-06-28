using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] BlackboardData blackboardData;

    private BehaviourTree tree;
    readonly EnemyBlackboard blackboard = new();
    //public TreeValuesManager ValuesManager;
    [SerializeField] private TreeValuesSO treeValues;
    public TreeValuesSO.TreeValuesRuntime TreeValues;
    private ComProtocol protocol;

    [SerializeField] private Character enemyCharacter;

    private float findRadius = 20;
    private float _movementSlerpFactor= 0.8f;
    private Vector3 previousDirection = Vector3.zero;

    private void Awake() {
        if (!enemyCharacter) enemyCharacter = GetComponent<Character>();

        blackboard.AddCharacterData(enemyCharacter.GetCharacterData());
        blackboard.AddWeaponData(enemyCharacter.Weapon.GetWeaponData());
        blackboard.SetKeyValue(CommonKeys.ComProtocol, protocol);
        blackboard.SetKeyValue(CommonKeys.ChosenPosition, enemyCharacter.transform.position);
        blackboard.SetKeyValue(CommonKeys.AgentSelf, this);
        blackboard.SetKeyValue(CommonKeys.FindRadius, findRadius);
        blackboard.SetKeyValue(CommonKeys.TeamSelf, enemyCharacter.GetCharacterInfo().Team);
        blackboard.SetKeyValue(CommonKeys.SelfHealth, enemyCharacter.GetCharacterInfo().Health);
        blackboard.SetKeyValue(CommonKeys.MaxHealth, enemyCharacter.GetCharacterInfo().MaxHealth);
        blackboardData?.SetValuesOnBlackboard(blackboard);

        //if (ValuesManager == null) {
        //    ValuesManager = new TreeValuesManager(blackboard, this);
        //}

        enemyCharacter.GetCharacterInfo().HealthChanged += AlterHealth;
        
        if (treeValues == null) {
            treeValues = ScriptableObject.CreateInstance<TreeValuesSO>();
        }
        TreeValues = new TreeValuesSO.TreeValuesRuntime(treeValues);

        
        tree = new BehaviourTree("Enemy");

        Repeater _repeater = new Repeater("BaseLogic");
        Parallel _parallel = new("BaseLogic/Parallel", 2);
        Parallel _actionParallel = new("BaseLogic/ActionParallel", 2);
        Leaf _distanceSelfFromWeapons = new("Combat/DistanceWeapon", new DistanceSelfFromWeaponsStrategy(blackboard));
        Leaf _moveToPosition = new("BaseLogic//MoveToPosition",
            new ActionStrategy(() => enemyCharacter.SetCharacterDirection(processDirections())));
        Leaf _positionWeapon = new Leaf("BaseLogic//AlignWeaponAngle",
            new ActionStrategy(() => enemyCharacter.RotateWeaponTowardsAngle(weaponAngle())));

        tree.AddChild(_repeater);
        _repeater.AddChild(_parallel);
        _parallel.AddChild(new CheckerTree(blackboard));
        _parallel.AddChild(new MessengerTree(blackboard));
        _parallel.AddChild(new DeciderTree(blackboard));
        _parallel.AddChild(_actionParallel);
        _actionParallel.AddChild(_distanceSelfFromWeapons);
        _actionParallel.AddChild(_moveToPosition);
        _actionParallel.AddChild(_positionWeapon);
        tree.Reset();

        AddBTDebugHUD();

        StartCoroutine(TreeTick());
    }

    private void Update() {
        //tree.Process();
    }

    IEnumerator TreeTick() {
        while (true) {
            tree.Process();
            yield return new WaitForSeconds(0.1f);
        }
    }

    private Vector2 moveDirection() {
        blackboard.TryGetValue(CommonKeys.ChosenPosition, out Vector3 _targetPosition);
        Vector3 moveDir = (_targetPosition - enemyCharacter.transform.position).normalized;
        return new Vector2(moveDir.x, moveDir.z);
    }

    private Vector3 movePosition() {
        blackboard.TryGetValue(CommonKeys.ChosenPosition, out Vector3 _targetPosition);
        return _targetPosition;
    }

    private Vector3 processDirections() {
        Vector3 _blendedDirection = blackboard.GetBlendedDirection();
        Vector3 _slerpedDir = Vector3.Slerp(previousDirection, _blendedDirection, _movementSlerpFactor);
        previousDirection = _blendedDirection;
        return _slerpedDir;
    }


    private float weaponAngle() {
        blackboard.TryGetValue(CommonKeys.ChosenWeaponAngle, out float _weaponAngle);
        return _weaponAngle;
    }

    public float GetWeaponRange() {
        return enemyCharacter.GetWeaponRange();
    }

    public float GetWeaponAngle() {
        return enemyCharacter.GetWeaponAngle();
    }

    public void InitiateAttackAction(ActionType pActionType, float pAttackAngle) {
        enemyCharacter.Attack(pActionType, pAttackAngle);
    }

    public void InitiateAttackAction(ActionInput pActionInput, float pAttackAngle) {
        enemyCharacter.Attack(pActionInput, pAttackAngle);
    }

    public void ChooseMovementAction(ActionType pActionType, Vector3 pDir) {
        enemyCharacter.SetCharacterDirection(pDir);
        enemyCharacter.Attack(pActionType, 0);
    }

    public void SendComMessage(EnemyController pRecipient, ComMessage pMessage) {
        pRecipient.ReceiveComMessage(pMessage);
    }

    public void ReceiveComMessage(ComMessage pMessage) {
        blackboard.TryGetValue(CommonKeys.MessageInbox, out List<ComMessage> _inbox);
        _inbox.Add(pMessage);
        blackboard.SetKeyValue(CommonKeys.MessageInbox, _inbox);
        //protocol.ReceiveComMessage(pMessage);
    }

    public void AlterHealth() {
        blackboard.SetKeyValue(CommonKeys.SelfHealth, enemyCharacter.GetCharacterInfo().Health);
    }

    public float GetCurrentHealth() {
        return enemyCharacter.GetCurrentHealth();
    }

    void OnDrawGizmos() {
        if (tree == null) return;
        DrawNodeGizmo(tree, Vector3.up * 2);
        DrawForces();
    }
    
    void DrawForces() {
        if (!Application.isPlaying) return;
        foreach (var force in blackboard.MovementForces) {
            Gizmos.color = force.Force>0 ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, transform.position + force.Direction * force.Force);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + blackboard.GetBlendedDirection() * 2);
        blackboard.ClearForces();
    }

    void DrawNodeGizmo(Node node, Vector3 pos) {
        Gizmos.color = node.IsActive ? Color.green : Color.gray;
        Gizmos.DrawSphere(pos, 0.1f);
        for (int i = 0; i < node.children.Count; i++) {
            Vector3 childPos = pos + new Vector3((i - node.children.Count / 2f) * 0.5f, 0, -0.5f);
            Gizmos.DrawLine(pos, childPos);
            DrawNodeGizmo(node.children[i], childPos);
        }
    }

    private void AddBTDebugHUD() {
        GetComponent<BTDebugHUD>().tree = tree;
    }

    private void OnDestroy() {
        enemyCharacter.GetCharacterInfo().HealthChanged -= AlterHealth;
    }
}