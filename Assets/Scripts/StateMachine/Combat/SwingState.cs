using UnityEngine;

public class SwingState : CombatState
{
    private float attackForce = 100f;
    public SwingState(string pName) : base(pName) { }

    public override void Ready() {
        //Name = "Swing";
    }

    public override void Enter(CombatSM pStateMachine, float pAttackAngle) {
        base.Enter(pStateMachine, pAttackAngle);
        duration = 2f;
        actionType = ActionType.Swing;
        attackRange = 2f;
        elapsedTime = 0f;
        HoldAction = true;

        StateMachine.Weapon.ApplyThrust(attackRange, 0.5f);
    }

    public override void UpdateLogic(float delta) {
        //Debug.Log($"Swinging {elapsedTime}", StateMachine);
        if (elapsedTime < duration) {
            if (elapsedTime >= duration * 0.5f) {
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
        Debug.Log($"Swing attackAngle {attackAngle}", StateMachine);
        StateMachine.Character.RotateWeaponWithForce(attackAngle, attackForce);
    }

    public override void Exit() {
        base.Exit();
        StateMachine.Weapon.ApplyThrust(0, 0.1f);
        elapsedTime = 0f;
        attackAngle = 0f;
        Interruptible = true;
    }
}
