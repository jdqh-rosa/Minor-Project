using System;
using UnityEngine;

public class WeaponPart : MonoBehaviour
{
    public WeaponPartType PartType;
    public ContactType ContactType;
    public CharacterWeapon Weapon;
    public float PartDistance;
}

public enum WeaponPartType
{
    None,
    Tip,
    Main,
    Hilt
}

public enum ContactType
{
    None,
    Slash,
    Poke,
    Chop,
    Blunt,
}