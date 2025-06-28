using UnityEngine;

public abstract class CombatState : BaseState<CombatSM>
{
    public ActionType actionType;
    protected float attackForce;
    protected float duration;
    protected float attackAngle;
    protected float attackRange;
    protected float interruptTime;
    public bool Interruptible;
    public bool HoldAction;
    protected float elapsedTime = 0f;
    protected float extendTime =0.1f;
    protected float retractTime =0.1f;
    
    CombatStateData stateData;

    public CombatState(string pName) : base(pName){}
    public virtual void Enter(CombatSM pStateMachine, float pAttackAngle)
    {
        base.Enter(pStateMachine);
        StateMachine.GetWeapon().CurrentState = WeaponState.Active;
        attackAngle = pAttackAngle;
        Interruptible = false;
    }

    public virtual void AddStateData(CombatStateData pData)
    {
        stateData = pData;
        attackForce = stateData.AttackForce;
        actionType = stateData.ActionType;
        duration = stateData.Duration;
        extendTime = stateData.ExtendTime;
        retractTime = stateData.RetractTime;
        actionType = stateData.ActionType;
        attackRange = stateData.AttackRange;
        interruptTime = stateData.InteruptTime;
        HoldAction = stateData.HoldAction;
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
    
}
