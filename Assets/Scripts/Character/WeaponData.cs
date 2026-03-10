using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Character/Weapon")]
public class WeaponData : ScriptableObject
{
    public float WeaponLength = 0f;
    public float Mass = 1;
    public float WeaponDistance = 0.1f;
    public float MaxTurnVelocity = 400f;
    public float MaxOrbitalVelocity = 700f;
    public float MaxReach = 1f;
    public float SwingDampingFactor = 0.98f;
    public float ThrustDampingFactor = 0.98f;
    public float KnockbackFactor = 0.9f;
    public float KnockbackScaleFactor = 0.02f;

    [Header("Part Damage Modifier")] 
    public float TipDamageFactor = 1.3f;
    public float MainDamageFactor = 1f;
    public float HiltDamageFactor = 0.5f;
    
    [Header("Contact Damage Modifier")] 
    public float BluntDamageFactor = 0.8f;
    public float ChopDamageFactor = 1.3f;
    public float PokeDamageFactor = 1.1f;
    public float SlashDamageFactor = 1.2f;
    
    [Header("Effect")]
    public GameObject ClashEffectPrefab;
    public GameObject ImpactEffectPrefab;
    
    [Header("Combat States")]
    public List<AttackInputEntry> AttackInputMap;
}

[Serializable]
public class AttackInputEntry
{
    public ActionInput Input;
    public bool Linear;
    public CombatStateData ActionData;
}