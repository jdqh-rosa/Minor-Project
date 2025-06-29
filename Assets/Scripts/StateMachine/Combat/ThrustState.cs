using UnityEngine;

public class ThrustState : CombatState
{
    
    public ThrustState(string pName) : base(pName) { }

    public override void Enter(CombatSM pStateMachine, float pAttackAngle) {
        base.Enter(pStateMachine, pAttackAngle);
        
        StateMachine.GetWeapon().ApplyThrust(attackRange, extendTime);
    }

    public override void UpdateLogic(float delta) {
        if (elapsedTime < duration) {
            if (elapsedTime >= interruptTime) {
                Interruptible = true;
            }
        }
        else {
            StateMachine.EndCurrentState();
        }

        elapsedTime += delta;
    }
    public override void UpdatePhysics(float delta) {
        StateMachine.Character.RotateWeaponTowardsAngle(attackAngle);
    }

    public override void Exit() {
        base.Exit();
        StateMachine.GetWeapon().ApplyThrust(0, retractTime);
        elapsedTime = 0f;
        attackAngle = 0f;
        Interruptible = true;
    }
}
