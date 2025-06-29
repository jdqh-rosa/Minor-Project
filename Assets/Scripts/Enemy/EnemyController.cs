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
    [SerializeField] private float tickWait=0.1f;
    [SerializeField] private TreeValuesSO treeValues;
    public TreeValuesSO.TreeValuesRuntime TreeValues;

    [SerializeField] private Character enemyCharacter;

    [SerializeField] private float movementSlerpFactor= 0.8f;
    private Vector3 previousDirection = Vector3.zero;

    private void Awake() {
        if (!enemyCharacter) enemyCharacter = GetComponent<Character>();

        blackboardData?.SetValuesOnBlackboard(blackboard);
        blackboard.AddCharacterData(enemyCharacter.GetCharacterData());
        blackboard.AddWeaponData(enemyCharacter.Weapon.GetWeaponData());
        blackboard.SetKeyValue(CommonKeys.ChosenPosition, enemyCharacter.transform.position);
        blackboard.SetKeyValue(CommonKeys.AgentSelf, this);
        blackboard.SetKeyValue(CommonKeys.FindRadius, treeValues.Miscellaneous.FindRange);
        blackboard.SetKeyValue(CommonKeys.TeamSelf, enemyCharacter.GetCharacterInfo().Team);
        blackboard.SetKeyValue(CommonKeys.SelfHealth, enemyCharacter.GetCharacterInfo().Health);
        blackboard.SetKeyValue(CommonKeys.MaxHealth, enemyCharacter.GetCharacterInfo().MaxHealth);

        //if (ValuesManager == null) {
        //    ValuesManager = new TreeValuesManager(blackboard, this);
        //}

        enemyCharacter.GetCharacterInfo().HealthChanged += alterHealth;
        
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

        StartCoroutine(TreeTick());
    }

    private void Update() {
        //tree.Process();
    }

    IEnumerator TreeTick() {
        while (true) {
            blackboard.ClearForces();
            tree.Process();
            yield return new WaitForSeconds(tickWait);
        }
    }

    private Vector3 processDirections() {
        Vector3 _blendedDirection = blackboard.GetBlendedDirection();
        Vector3 _slerpedDir = Vector3.Slerp(previousDirection, _blendedDirection, movementSlerpFactor);
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
        pRecipient.receiveComMessage(pMessage);
    }

    private void receiveComMessage(ComMessage pMessage) {
        blackboard.TryGetValue(CommonKeys.MessageInbox, out List<ComMessage> _inbox);
        _inbox.Add(pMessage);
        blackboard.SetKeyValue(CommonKeys.MessageInbox, _inbox);
    }

    private void alterHealth() {
        blackboard.SetKeyValue(CommonKeys.SelfHealth, enemyCharacter.GetCharacterInfo().Health);
    }

    public float GetCurrentHealth() {
        return enemyCharacter.GetCurrentHealth();
    }

    void OnDrawGizmos() {
        if (tree == null) return;
        drawNodeGizmo(tree, enemyCharacter.transform.position);
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
    }

    private void drawNodeGizmo(Node pNode, Vector3 pPos) {
        Gizmos.color = pNode.IsActive ? Color.green : Color.gray;
        Gizmos.DrawSphere(pPos, 0.1f);
        for (int i = 0; i < pNode.children.Count; i++) {
            Vector3 childPos = pPos + new Vector3((i - pNode.children.Count / 2f) * 0.5f, 0, -0.5f);
            Gizmos.DrawLine(pPos, childPos);
            drawNodeGizmo(pNode.children[i], childPos);
        }
    }

    private void OnDestroy() {
        enemyCharacter.GetCharacterInfo().HealthChanged -= alterHealth;
    }
}