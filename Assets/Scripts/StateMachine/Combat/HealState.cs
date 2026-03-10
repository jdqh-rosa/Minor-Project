using UnityEngine;

public class HealState : JabState
{
    public HealState() : base() { }

    private GameObject healPrefab = AssetRegistry.Instance.healingOrbPrefab;

    public override void Enter(CombatSM pStateMachine, float pAttackAngle) {
        base.Enter(pStateMachine, pAttackAngle);
        StateMachine.GetWeapon().ApplyThrust(attackRange, extendTime);
        
        SpawnOrb();
    }

    public override void UpdateLogic(float delta) {
        if (elapsedTime < duration) {
            if (elapsedTime >= interruptTime) {
                isInterruptible = true;
            }
        }
        else {
            StateMachine.EndCurrentState();
        }

        elapsedTime += delta;
    }
    public override void UpdatePhysics(float delta) {
        //StateMachine.Character.RotateWeaponTowardsAngle(attackAngle);
    }

    public override void Exit() {
        base.Exit();
        StateMachine.GetWeapon().ApplyThrust(0, retractTime);
        elapsedTime = 0f;
        attackAngle = 0f;
        isInterruptible = true;
    }

    private void SpawnOrb() {
        Vector3 spawnPos = StateMachine.GetWeapon().GetTipPosition();
        float angle = StateMachine.GetWeapon().GetAngle();
        Vector3 direction = new Vector3(
            Mathf.Cos(angle * Mathf.Deg2Rad),
            0,
            Mathf.Sin(angle * Mathf.Deg2Rad)
        );
        var orb = Object.Instantiate(healPrefab, spawnPos + direction.normalized * 2 , Quaternion.Euler(0, angle, 0));
        orb.GetComponent<Projectile>().Setup(-attackForce, 10, direction);
    }
}
