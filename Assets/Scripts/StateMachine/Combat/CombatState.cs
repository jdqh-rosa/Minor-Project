using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New CombatState", menuName = "Character/CombatState")]
public class CombatState : BaseState<CombatSM>
{
    protected ActionType actionType;
    protected float attackForce;
    protected float duration;
    protected float attackAngle;
    protected float attackRange;
    protected float interruptTime;
    protected bool isInterruptible;
    protected bool isHoldAction;
    protected float elapsedTime = 0f;
    protected float extendTime =0.1f;
    protected float retractTime =0.1f;
    
    [SerializeField]CombatStateData stateData;

    protected CombatState(string pName) : base(pName){}
    protected CombatState() : base(){}

    public void Awake() {
        if (stateData != null) {
            AddStateData(stateData);
        }
    }

    public virtual void Enter(CombatSM pStateMachine, float pAttackAngle)
    {
        base.Enter(pStateMachine);
        StateMachine.GetWeapon().CurrentState = WeaponState.Active;
        attackAngle = pAttackAngle;
        isInterruptible = false;
    }

    public virtual void AddStateData(CombatStateData pData)
    {
        stateData = pData;
        Name = stateData.Name;
        attackForce = stateData.AttackForce;
        actionType = stateData.ActionType;
        duration = stateData.Duration;
        extendTime = stateData.ExtendTime;
        retractTime = stateData.RetractTime;
        attackRange = stateData.AttackRange;
        interruptTime = stateData.InteruptTime;
        isHoldAction = stateData.HoldAction;
    }

    public override void UpdateLogic(float delta)
    {
        base.UpdateLogic(delta);
    }

    public override void UpdatePhysics(float delta)
    {
        base.UpdatePhysics(delta);
    }

    public override void Exit()
    {
        base.Exit();
        StateMachine.GetWeapon().CurrentState = WeaponState.Reset;
    }

    public virtual void SetAttackAngle(float pAttackAngle) {
        
        attackAngle = pAttackAngle;
    }

    public bool IsInterruptible() {
        return isInterruptible;
    }

    public bool IsHoldAction() {
        return isHoldAction;
    }

    public ActionType GetActionType() {
        return actionType;
    }

}
