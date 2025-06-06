using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] BlackboardData blackboardData;
    
    private BehaviourTree tree;
    readonly EnemyBlackboard blackboard = new();
    private ComProtocol protocol;

    [SerializeField] private Character enemyCharacter;

    private float findRadius = 10;

    private void Awake()
    {
        if (!enemyCharacter) enemyCharacter = GetComponent<Character>();
        
        blackboard.AddCharacterData(enemyCharacter.GetData());
        blackboardData.SetValuesOnBlackboard(blackboard);
        blackboard.SetKeyValue(CommonKeys.ComProtocol, protocol);        
        blackboard.SetKeyValue(CommonKeys.MessageInbox, new List<ComMessage>());
        blackboard.SetKeyValue(CommonKeys.ChosenPosition, enemyCharacter.transform.position);
        blackboard.SetKeyValue(CommonKeys.VisibleAllies, new List<GameObject>());
        blackboard.SetKeyValue(CommonKeys.VisibleTargets, new List<GameObject>());
        blackboard.SetKeyValue(CommonKeys.AgentSelf, this); 
        blackboard.SetKeyValue(CommonKeys.FindRadius, findRadius);
        blackboard.SetKeyValue(CommonKeys.DirectionalForces, new List<DirectionalForce>());
        
        tree = new BehaviourTree("Enemy");

        Repeater _repeater = new Repeater("BaseLogic");
        Parallel _parallel = new("BaseLogic/Parallel", 2);
        Parallel _actionParallel = new("BaseLogic/ActionParallel",2);
        Leaf _moveToPosition = new("BaseLogic//MoveToPosition", new ActionStrategy(() => enemyCharacter.SetCharacterPosition(movePosition())));
        Leaf _positionWeapon = new Leaf("BaseLogic//AlignWeaponAngle", new ActionStrategy(() => enemyCharacter.RotateWeaponTowardsAngle(weaponAngle())));
        
        
        tree.AddChild(_repeater);
        _repeater.AddChild(_parallel);
        _parallel.AddChild(new CheckerTree(blackboard));
        _parallel.AddChild(new MessengerTree(blackboard));
        _parallel.AddChild(new DeciderTree(blackboard));
        _parallel.AddChild(_actionParallel);
        _actionParallel.AddChild(_moveToPosition);
        _actionParallel.AddChild(_positionWeapon);
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

    private Vector2 processDirections() {
        if (!blackboard.TryGetValue(CommonKeys.DirectionalForces, out List<DirectionalForce> _forces)) return Vector2.zero;
        Vector2 direction = Vector2.zero;
        foreach (DirectionalForce dForce in _forces) {
            direction += (dForce.Direction).normalized * dForce.Force;
        }
        return direction.normalized;
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

    public void InitiateAttackAction(ActionType pActionType, float pAttackAngle)
    {
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
