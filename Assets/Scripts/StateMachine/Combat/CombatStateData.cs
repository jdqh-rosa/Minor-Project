using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New CombatStateData", menuName = "Character/CombatStateData")]
[Serializable]
public class CombatStateData : ScriptableObject
{
    public string Name;
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
