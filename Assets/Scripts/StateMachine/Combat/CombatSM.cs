using System.Collections.Generic;
using UnityEngine;

public class CombatSM : BaseStateMachine<CombatSM>
{
    public Character Character;
    public CharacterWeapon Weapon;
    private CombatState inputState;
    private float attackAngle;
    protected CombatState currentCombatState;

    protected override void Start() {
        base.Start();
        currentCombatState = currentState as CombatState;
    }

    protected override void Update() {
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

    public void InputState(string pInput, float pAttackAngle) {
        inputState = (CombatState)GetState(pInput);
        attackAngle = pAttackAngle;
        
        if (currentCombatState.HoldAction) {
            currentCombatState.SetAttackAngle(attackAngle);
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
        }
    }

    public new CombatState GetCurrentState() {
        return (CombatState)base.GetCurrentState();
    }

    public override void EndCurrentState() {
        TransitionToState(InitialState.Name);
    }
}