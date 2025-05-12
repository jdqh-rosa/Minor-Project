using UnityEngine;

public class ThrustState : CombatState
{
    public ThrustState(string pName) : base(pName) { }

    public override void Ready() {
        //Name = "Thrust";
    }

    public override void Enter(CombatSM pStateMachine, float pAttackAngle) {
        base.Enter(pStateMachine, pAttackAngle);
        duration = 1.5f;
        actionType = ActionType.Thrust;
        attackRange = 3f;
        elapsedTime = 0f;
        HoldAction = true;

        StateMachine.Weapon.ApplyThrust(attackRange, 0.1f);
    }

    public override void UpdateLogic(float delta) {
        if (elapsedTime < duration) {
            if (elapsedTime >= duration * 0.8f) {
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
        StateMachine.Character.RotateWeaponTowardsAngle(attackAngle);
    }

    public override void Exit() {
        base.Exit();
        StateMachine.Weapon.ApplyThrust(0, 0.2f);
        elapsedTime = 0f;
        attackAngle = 0f;
        Interruptible = true;
    }
}
