using System.Collections.Generic;
using UnityEngine;

public class CombatSM : BaseStateMachine<CombatSM>
{
    public Character Character;
    private CharacterWeapon weapon;
    private CombatState inputState;
    private CombatState bufferedState;
    private float attackAngle;
    private float bufferTime = 1f;
    private float bufferClock;
    private CombatState currentCombatState;
    
#if UNITY_EDITOR
    public List<CombatState> EditorStates = new();
#endif
    
    private Dictionary<ActionType, string> actionMap = new();

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

    private void TransitionToState(CombatState pNewState, float pAttackAngle) {
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

    private void InputState(string pInput, float pAttackAngle = 0f) {
        inputState = (CombatState)GetState(pInput);
        if (inputState == null) Debug.Log($"Combat state not found: {pInput}");

    if (currentCombatState.IsHoldAction()) {
            currentCombatState.SetAttackAngle(pAttackAngle);
        }
    }

    private void HandleInput() {
        if (inputState == null && bufferedState == null) return;

        if (!currentCombatState.IsInterruptible()) {
            bufferedState = inputState;
            return;
        }
        
        inputState ??= bufferedState;

        TransitionToState(inputState, attackAngle);
        inputState = null;
        
        bufferClock += Time.deltaTime;
        if (bufferClock >= bufferTime) {
            bufferedState = null;
            bufferClock = 0f;
        }
    }
    
    public void Attack(ActionInput pAttackInput, float pTargetAngle, bool linear) {
        var entry = weapon.GetWeaponData().AttackInputMap.Find(e => e.Input == pAttackInput && e.Linear == linear);
        if (entry != null)
            Attack(entry.ActionData.ActionType, pTargetAngle);
    }

    public void Attack(ActionType pActionType, float pTargetAngle)
    {
        attackAngle = pTargetAngle;

        if (actionMap.TryGetValue(pActionType, out string stateName)) {
            InputState(stateName, pTargetAngle);
        }
    }
    
    public void AddState(CombatState newState, CombatStateData pData) {
        newState.AddStateData(pData);
        actionMap[pData.ActionType] = pData.Name;
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

    public void SetBufferTime(float pBufferTime) {
        bufferTime = pBufferTime;
    }
}