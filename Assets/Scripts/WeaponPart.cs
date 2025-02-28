using UnityEngine;

public class WeaponPart : MonoBehaviour
{
    public WeaponPartType PartType;
    public ContactType ContactType;

    public void CollisionDetected(Character pCharacterHit, float pointOfContact, bool pIsClash) {
        
    }
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

