using UnityEngine;

public class StrideState : CombatState
{
    private Vector3 moveDir;
    private Vector3 moveVec;
    public StrideState() : base() { }

    public override void Enter(CombatSM pStateMachine, float pAttackAngle) {
        base.Enter(pStateMachine);
        isInterruptible = false;
        StateMachine.Character.Body.SetSpecialMovement(true);
        moveDir = StateMachine.Character.GetCharacterDirection();
    }

    public override void UpdateLogic(float delta) {
        elapsedTime += delta;
        if (elapsedTime < duration) {
            if (elapsedTime >= interruptTime) {
                isInterruptible = true;
            }
            moveVec = StateMachine.Character.Body.Stride(moveDir, attackRange, duration, elapsedTime);
        }
        else {
            StateMachine.EndCurrentState();
        }
    }

    public override void UpdatePhysics(float delta) {
        
        if(moveVec.sqrMagnitude < Mathf.Epsilon) return;
        
        StateMachine.Character.Move(moveVec);
    }

    public override void Exit() {
        base.Exit();
        elapsedTime = 0f;
        isInterruptible = true;
        moveDir = Vector3.zero;
        StateMachine.Character.Body.SetSpecialMovement(false);
    }
}
