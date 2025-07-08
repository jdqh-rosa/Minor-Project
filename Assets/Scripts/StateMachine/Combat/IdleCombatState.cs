using UnityEngine;

public class IdleCombatState : CombatState
{
    public IdleCombatState(string pName) : base(pName) {
        isInterruptible = true;
    }

    public override void Enter(CombatSM pStateMachine, float pAttackAngle) {
        StateMachine.GetWeapon().CurrentState = WeaponState.Reset;
    }
}
