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
        
        var _hitChar = pOther.collider.GetComponentInParent<Character>();
        if (_hitChar == null) return;

        bool isClash = _hitChar.CompareTag("WeaponPart");
        Vector3 contactPoint = transform.position; // approximate tip, or do a raycast
        Weapon.CollisionDetected(this, _hitChar, isClash, contactPoint);
        
        // Vector3 _contact = pOther.GetContact(0).point;
        // var _collisionTag = pOther.gameObject.tag;
        //
        // switch (_collisionTag)
        // {
        //     case "WeaponPart":
        //         WeaponPart _component;
        //         if (pOther.gameObject.TryGetComponent(out _component))
        //         {
        //             CollisionDetected(_component.Weapon.Character, true, _contact);
        //         } break;
        //     case "CharacterBody":
        //         Character _characterHit;
        //         if (pOther.gameObject.TryGetComponent(out _characterHit))
        //         {
        //             CollisionDetected(_characterHit, false, _contact);
        //         } break;
        // }
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