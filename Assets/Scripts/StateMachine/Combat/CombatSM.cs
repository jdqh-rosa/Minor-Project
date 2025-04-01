using System.Collections.Generic;
using Unity.VisualScripting;
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

    public new CombatState GetCurrentState() {
        return (CombatState)base.GetCurrentState();
    }

    public override void EndCurrentState() {
        //base.EndCurrentState();
        TransitionToState(InitialState.Name);
    }
}