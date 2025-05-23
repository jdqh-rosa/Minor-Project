using UnityEngine;

public class SwingState : CombatState
{
    private float attackForce = 100f;
    public SwingState(string pName) : base(pName) { }

    public override void Ready() {
        //Name = "Swing";
        extendTime = 0.3f;
    }

    public override void Enter(CombatSM pStateMachine, float pAttackAngle) {
        base.Enter(pStateMachine, pAttackAngle);
        
        StateMachine.Weapon.ApplyThrust(attackRange, extendTime);
    }

    public override void UpdateLogic(float delta) {
        //Debug.Log($"Swinging {elapsedTime}", StateMachine);
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

    private void ThrustAttack(bool pHoldAttack, float pTargetAngle) { }

    public override void UpdatePhysics(float delta) {
        //Debug.Log($"Swing attackAngle {attackAngle}", StateMachine);
        StateMachine.Character.RotateWeaponWithForce(attackAngle, attackForce);
    }

    public override void Exit() {
        base.Exit();
        StateMachine.Weapon.ApplyThrust(0, retractTime);
        elapsedTime = 0f;
        attackAngle = 0f;
        Interruptible = true;
    }
}
