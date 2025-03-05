using System;
using UnityEngine;
using UnityEngine.Serialization;

public class Character : MonoBehaviour
{
    public Rigidbody RigidBody;
    public CharacterBody Body;
    public CharacterWeapon Weapon;
    
    public float MoveSpeed = 5f;
    Vector2 moveDirection = Vector2.zero;
    Vector2 lookDirection;

    private Vector2 cumulVelocity;

    public CollissionHandler collissionHandler;

    private void Start()
    {
        Weapon.Character = this;
        Body.Character = this;
    }

    public void CumulativeVelocity() {
        cumulVelocity = Body.Velocity + Weapon.Velocity;
    }
    
    public void SetBodyRotation(float angle) {
        Body.transform.rotation = Quaternion.Euler(0, angle, 0);
    }

    public void SetCharacterPosition(Vector2 pos) {
        moveDirection = pos;
    }

    public void SetWeaponOrbit(float pAngle) {
        //playerWeapon.SetOrbitPosition(weaponDistance, pAngle);
    }

    public void AddWeaponOrbital(float pAdditionalMomentum) {
        Weapon.OrbitalAccelerate(pAdditionalMomentum, Time.deltaTime);
    }

    public void AddWeaponPerpendicular(Vector2 pAdditionalMomentum) {
        Weapon.AddPerpendicularVelocity(pAdditionalMomentum);
    }
    
    public void RotateWeaponTowardsAngle(float targetAngle, float rotationSpeed) {
        // Get the current rotation of the weapon (in degrees)
        float currentAngle = Weapon.transform.rotation.eulerAngles.y;

        // Calculate the shortest way to rotate towards the target angle
        float angleDiff = Mathf.DeltaAngle(currentAngle, targetAngle);

        // Apply a rotation force (or torque) to rotate the weapon
        // Ensure the rotation speed is applied gradually
        float step = rotationSpeed * Time.deltaTime;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, step);

        // Update the weapon's rotation based on the calculated angle
        Weapon.transform.rotation = Quaternion.Euler(0, newAngle, 0);
    }

    
    void FixedUpdate()
    {
        bodyFunctions();
        weaponFunctions();
        
        CumulativeVelocity();
    }

    private void bodyFunctions()
    {
        Body.Move(moveDirection.normalized);
        //RigidBody.MovePosition(transform.position + new Vector3(moveDirection.x * MoveSpeed, 0, moveDirection.y * MoveSpeed));
    }
    
    private void weaponFunctions()
    {
        Weapon.UpdatePosition();
    }
    
    public float GetWeaponAngle() {
        return Weapon.GetAngle();
    }

    public Vector3 GetWeaponPosition() {
        return Weapon.transform.position;
    }

    public void CollisionDetected(Character pCharacterHit, bool pIsClash, float pMomentum) {
        if (pIsClash)
        {
            weaponHit(pCharacterHit, pMomentum);
        }
        else
        {
            bodyHit(pCharacterHit);
        }

    }

    private void weaponHit(Character pCharacterHit, float pMomentum)
    {
        Vector2 _otherMomentum = pCharacterHit.getHitMomentum();
        
        Vector2 _momentumDifference = cumulVelocity - _otherMomentum;

        Vector2 impactDirection = (cumulVelocity - _otherMomentum).normalized;

        float v1 = Vector2.Dot(cumulVelocity, impactDirection);
        float v2 = Vector2.Dot(_otherMomentum, impactDirection);

        float m1 = Weapon.Mass;
        float m2 = pCharacterHit.Weapon.Mass;

        float newV1 = ((m1 - m2) / (m1 + m2)) * v1 + ((2 * m2) / (m1 + m2)) * v2;
        float newV2 = ((2 * m1) / (m1 + m2)) * v1 + ((m2 - m1) / (m1 + m2)) * v2;

        Vector2 newVelocity1 = cumulVelocity + (newV1 - v1) * impactDirection;
        Vector2 newVelocity2 = _otherMomentum + (newV2 - v2) * impactDirection;
        
        AddWeaponOrbital(newVelocity1.magnitude);
        pCharacterHit.AddWeaponOrbital(newVelocity2.magnitude);
    }

    private void bodyHit(Character pCharacterHit) { }

    private Vector2 getHitMomentum() {
        CumulativeVelocity();
        return cumulVelocity;
    }

}
