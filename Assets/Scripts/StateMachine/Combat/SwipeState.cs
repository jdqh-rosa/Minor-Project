using UnityEngine;

public class SwipeState : CombatState
{
    private float attackForce = 100f;
    public SwipeState(string pName) : base(pName) { }

    public override void Ready() {
        //Name = "Swipe";
    }

    public override void Enter(CombatSM pStateMachine, float pAttackAngle) {
        base.Enter(pStateMachine, pAttackAngle);
        duration = 0.1f;
        actionType = ActionType.Swipe;
        attackRange = 1.5f;
        elapsedTime = 0f;

        StateMachine.Weapon.ApplyThrust(attackRange, 0.5f);
    }

    public override void UpdateLogic(float delta) {
        Debug.Log($"Swiping {elapsedTime}", StateMachine);
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