using UnityEngine;

public class IdleCombatState : CombatState
{
    public IdleCombatState(string pName) : base(pName) {
        Interruptible = true;
    }

    public override void UpdateLogic(float delta) {
        if (StateMachine.transform.gameObject.name == "Player") {
            //Debug.Log("IdleCombatState::UpdateLogic", this);
        }
    }
}
