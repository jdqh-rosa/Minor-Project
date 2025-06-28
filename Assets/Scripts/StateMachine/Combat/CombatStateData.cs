using UnityEngine;

[CreateAssetMenu(fileName = "New CombatState", menuName = "Character/CombatState")]
public class CombatStateData : ScriptableObject
{
    public ActionType ActionType;
    public float AttackForce;
    public float Duration;
    public float ExtendTime;
    public float RetractTime;
    public float InteruptTime;
    public float AttackRange;
    public float IdealAttackAngle;
    public bool HoldAction;
}
