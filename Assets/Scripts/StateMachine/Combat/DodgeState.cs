using UnityEngine;

public class DodgeState : CombatState
{
    private Vector3 moveDir;
    private Vector3 moveVec;
    public DodgeState(string pName) : base(pName) { }

    public override void Enter(CombatSM pStateMachine, float pAttackAngle) {
        base.Enter(pStateMachine);
        Interruptible = false;
        StateMachine.Character.Body.SetSpecialMovement(false);
        moveDir = StateMachine.Character.GetCharacterDirection();
    }

    public override void UpdateLogic(float delta) {
        elapsedTime += delta;
        if (elapsedTime < duration) {
            if (elapsedTime >= interruptTime) {
                Interruptible = true;
            }
            moveVec = StateMachine.Character.Body.Dodge(moveDir,attackRange, duration, elapsedTime);
        }
        else {
            StateMachine.EndCurrentState();
        }
        
    }

    public override void UpdatePhysics(float delta) {
        StateMachine.Character.Move(moveVec);
    }

    public override void Exit() {
        base.Exit();
        elapsedTime = 0f;
        Interruptible = true;
        moveDir = Vector3.zero;
        StateMachine.Character.Body.SetSpecialMovement(false);
    }
}