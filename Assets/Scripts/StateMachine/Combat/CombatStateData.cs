using UnityEngine;

[CreateAssetMenu(fileName = "New CombatState", menuName = "Character/CombatState")]
public class CombatStateData : ScriptableObject
{
    public ActionType ActionType;
    public float Duration;
    public float InteruptTime;
    public float AttackRange;
    public float IdealAttackAngle;
    public bool HoldAction;
}
