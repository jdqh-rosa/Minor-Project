using UnityEngine;

public abstract class CombatState : BaseState<CombatSM>
{
    public ActionType actionType;
    protected float duration;
    protected float attackAngle;
    protected float attackRange;
    protected float interruptTime;
    public bool Interruptible;
    public bool HoldAction;
    protected float elapsedTime = 0f;
    
    CombatStateData stateData;

    public CombatState(string pName) : base(pName){}
    public virtual void Enter(CombatSM pStateMachine, float pAttackAngle)
    {
        base.Enter(pStateMachine);
        attackAngle = pAttackAngle;
        Interruptible = false;
        
        Debug.Log($"Player Attack Jab Activated");
    }

    public virtual void AddStateData(CombatStateData pData)
    {
        stateData = pData;
        actionType = stateData.ActionType;
        duration = stateData.Duration;
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
    }

    public virtual void SetAttackAngle(float pAttackAngle) {
        
        attackAngle = pAttackAngle;
    }
    
}
