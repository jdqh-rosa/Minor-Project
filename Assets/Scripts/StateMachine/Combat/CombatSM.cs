using System.Collections.Generic;
using UnityEngine;

public class CombatSM : BaseStateMachine<CombatSM>
{
    public Character Character;
    private CharacterWeapon weapon;
    private CombatState inputState;
    private float attackAngle;
    protected CombatState currentCombatState;

    protected override void Start() {
        base.Start();
        currentCombatState = currentState as CombatState;
    }

    protected override void Update() {
        if (weapon != Character.Weapon) {
            SetWeapon(Character.Weapon);
        }
        
        HandleInput();
        base.Update();
    }

    protected override void FixedUpdate() {
        base.FixedUpdate();
    }

    public void TransitionToState(CombatState pNewState, float pAttackAngle) {
        if (!states.ContainsKey(pNewState.Name)) return;
        currentState?.Exit();

        currentState = (CombatState)states[pNewState.Name];
        currentCombatState = (CombatState)currentState;
        pNewState?.Enter(this, pAttackAngle);
    }

    public void TransitionToState(string newState, float pAttackAngle) {
        if (!states.ContainsKey(newState)) return;
        currentCombatState?.Exit();

        currentCombatState = (CombatState)states[newState];
        var curTemp = currentCombatState;
        curTemp?.Enter(this, pAttackAngle);
    }

    public void InputState(CombatState pInput, float pAttackAngle) {
        inputState = pInput;
        attackAngle = pAttackAngle;
    }

    public void InputState(string pInput, float pAttackAngle=0f) {
        inputState = (CombatState)GetState(pInput);
        
        //SetAttackAngle(pAttackAngle);
        if (currentCombatState.HoldAction) {
            currentCombatState.SetAttackAngle(pAttackAngle);
        }
    }

    private void HandleInput() {
        if (inputState == null) return;

        if (!currentCombatState.Interruptible) return;

        TransitionToState(inputState, attackAngle);
        inputState = null;
    }
    
    public void Attack(ActionInput pAttackInput, float pTargetAngle, bool linearAttack) {
        ActionType _actionType = ActionType.None;
        if (pAttackInput == ActionInput.Press) {
            _actionType = linearAttack ? ActionType.Jab : ActionType.Swipe;
        }

        if (pAttackInput == ActionInput.Hold) {
            _actionType = linearAttack ? ActionType.Thrust : ActionType.Swing;
        }

        Debug.Log($"Attack Action Type: {_actionType}");

        Attack(_actionType, pTargetAngle);
    }

    public void Attack(ActionType pActionType, float pTargetAngle)
    {
        attackAngle = pTargetAngle;
        switch (pActionType) {
            case ActionType.Jab:
                InputState("Jab", pTargetAngle);
                break;
            case ActionType.Thrust:
                InputState("Thrust", pTargetAngle);
                break;
            case ActionType.Swipe:
                InputState("Swipe", pTargetAngle);
                break;
            case ActionType.Swing:
                InputState("Swing", pTargetAngle);
                break;
            case ActionType.Stride:
                InputState("Stride");
                break;
            case ActionType.Dodge:
                InputState("Dodge");
                break;
        }
    }
    
    public void AddState(CombatState newState, CombatStateData pData) {
        newState.AddStateData(pData);
        AddState(newState);
    }

    public new CombatState GetCurrentState() {
        return (CombatState)base.GetCurrentState();
    }

    public override void EndCurrentState() {
        TransitionToState(InitialState.Name);
    }

    public void SetAttackAngle(float pAngle) {
        attackAngle = pAngle;
        currentCombatState.SetAttackAngle(attackAngle);
    }

    public void SetWeapon(CharacterWeapon pWeapon) {
        weapon = pWeapon;
    }
    public CharacterWeapon GetWeapon() {
        return weapon;
    }
}