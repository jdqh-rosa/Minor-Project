using UnityEngine;

[CreateAssetMenu(fileName = "New Character", menuName = "Character/Character")]
public class CharacterData : ScriptableObject
{
    public float RotationSpeed = 10f;
    public float MaxRotationSpeed = 500f;
    public float DampingFactor = 0.2f;
    public float VelocityDamping = 0.9f;
    public float DeadZoneThreshold = 0.1f;
    public float LinearAttackZone = 30f;
    public float CollisionElasticity = 0.8f;
    public float AngularFactor = 0.9f;

    public CombatStateData JabState;
    public CombatStateData ThrustState;
    public CombatStateData SwipeState;
    public CombatStateData SwingState;
    public CombatStateData StrideState;
    public CombatStateData DodgeState;
    public CombatStateData ParryState;
    public CombatStateData BlockState;
}

public enum CharacterTeam
{
    Any,
    Player,
    Enemy,
    Neutral,
    Blembd
}
