using System.Collections;
using UnityEngine;

public class JabState : CombatState
{
    public JabState(string pName) : base(pName) { }

    public override void Ready() {
        //Name = "Jab";
    }

    public override void Enter(CombatSM pStateMachine, float pAttackAngle) {
        base.Enter(pStateMachine, pAttackAngle);

        StateMachine.Weapon.ApplyThrust(attackRange, extendTime);
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

    private void ThrustAttack(bool pHoldAttack, float pTargetAngle) { }

    public override void UpdatePhysics(float delta) {
        StateMachine.Character.RotateWeaponTowardsAngle(attackAngle);
    }

    public override void Exit() {
        base.Exit();
        StateMachine.Weapon.ApplyThrust(0, retractTime);
        elapsedTime = 0f;
        attackAngle = 0f;
        Interruptible = true;
    }
}