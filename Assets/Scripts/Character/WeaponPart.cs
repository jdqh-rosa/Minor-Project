using System;
using UnityEngine;

public class WeaponPart : MonoBehaviour
{
    public WeaponPartType PartType;
    public ContactType ContactType;
    public CharacterWeapon Weapon;
    public float PartDistance;

    public void CollisionDetected(Character pCharacterHit, bool pIsClash, Vector3 pPointOfContact)
    {
        Debug.Log("WeaponPart::CollisionDetected", this);
        Weapon.CollisionDetected(this, pCharacterHit, pIsClash, pPointOfContact);
    }

    private void OnCollisionEnter(Collision pOther)
    {
        if (pOther.transform.parent == transform.parent) return;
        
        Vector3 _contact = pOther.GetContact(0).point;
        var _collisionTag = pOther.gameObject.tag;
        
        switch (_collisionTag)
        {
            case "WeaponPart":
                WeaponPart _component;
                if (pOther.gameObject.TryGetComponent(out _component))
                {
                    
                    CollisionDetected(_component.Weapon.Character, true, _contact);
                } break;
            case "CharacterBody":
                Character _characterHit;
                if (pOther.gameObject.TryGetComponent(out _characterHit))
                {
                    CollisionDetected(_characterHit, false, _contact);
                } break;
        }
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