using UnityEngine;

public class CollissionHandler : MonoBehaviour
{
    Character character;
    Character otherCharacter;

    private CharacterWeapon characterWeapon;
    private CharacterWeapon otherWeapon;

    public void ResolveCollision(Character pCharacter, Character pOtherCharacter)
    {
        character = pCharacter;
        otherCharacter = pOtherCharacter;
        characterWeapon = character.Weapon;
        otherWeapon = otherCharacter.Weapon;
        
        
    }
}
