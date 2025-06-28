using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Character/Weapon")]
public class WeaponData : ScriptableObject
{
    public float Mass = 1;
    public float WeaponDistance = 0.1f;
    public float MaxTurnVelocity = 400f;
    public float MaxOrbitalVelocity = 700f;
    public float MaxReach = 1f;
    public float SwingDampingFactor = 0.98f;
    public float ThrustDampingFactor = 0.98f;
    
    [Header("Combat States")]
    public CombatStateData JabState;
    public CombatStateData ThrustState;
    public CombatStateData SwipeState;
    public CombatStateData SwingState;
    public CombatStateData BlockState;
}