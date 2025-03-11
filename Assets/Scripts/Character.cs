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
    
    float elasticity = 0.8f;

    private Vector2 cumulVelocity;

    private void Start()
    {
        if (Weapon == null || Body == null) {
            Debug.LogError("Weapon or Body is not assigned.");
            return;
        }
        
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
        float currentAngle = Weapon.transform.rotation.eulerAngles.y;

        float angleDiff = Mathf.DeltaAngle(currentAngle, targetAngle);

        float step = rotationSpeed * Time.deltaTime;
        float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, step);

        Weapon.transform.rotation = Quaternion.Euler(0, newAngle, 0);
    }

    
    void FixedUpdate()
    {
        bodyFunctions();
        weaponFunctions();
        
        CumulativeVelocity();
    }

    private void bodyFunctions(){
        //Body.Move(moveDirection.normalized);
        //take movement from Body and apply it in here
        
        RigidBody.MovePosition(transform.position + new Vector3(moveDirection.x * MoveSpeed, 0, moveDirection.y * MoveSpeed));
    }
    
    private void weaponFunctions(){
        Weapon.UpdatePosition();
    }
    
    public float GetWeaponAngle() {
        return Weapon.GetAngle();
    }

    public Vector3 GetWeaponPosition() {
        return Weapon.transform.position;
    }

    public float GetWeaponOrbital() {
        return Weapon.OrbitalVelocity;
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

    private void weaponHit(Character pCharacterHit, float pMomentum){
        Vector2 _otherMomentum = pCharacterHit.getHitMomentum();
        
        Vector2 _impactDirection;
        if (cumulVelocity.magnitude < 0.01f)
        {
            _impactDirection = _otherMomentum.normalized;
        }
        else if (_otherMomentum.magnitude < 0.01f)
        {
            _impactDirection = cumulVelocity.normalized;
        }
        else
        {
            _impactDirection = (cumulVelocity - _otherMomentum).normalized;
        }

        float _v1 = Vector2.Dot(cumulVelocity, _impactDirection);
        float _v2 = Vector2.Dot(_otherMomentum, _impactDirection);

        float _m1 = Weapon.Mass;
        float _m2 = pCharacterHit.Weapon.Mass;

        float _newV1 = ((_m1 - _m2) / (_m1 + _m2)) * _v1 + ((2 * _m2) / (_m1 + _m2)) * _v2;
        float _newV2 = ((2 * _m1) / (_m1 + _m2)) * _v1 + ((_m2 - _m1) / (_m1 + _m2)) * _v2;

        Vector2 _newVelocity1 = cumulVelocity + (_newV1 - _v1) * _impactDirection;
        Vector2 _newVelocity2 = _otherMomentum + (_newV2 - _v2) * _impactDirection;
        
        _newVelocity1 *= elasticity;
        _newVelocity2 *= elasticity;
        
        Vector2 _relativePosition1 = (GetWeaponPosition() - pCharacterHit.GetWeaponPosition()).normalized;
        Vector2 _relativePosition2 = -_relativePosition1;


        float _sign1 = Mathf.Sign(Vector3.Cross(new Vector3(_relativePosition1.x, 0, _relativePosition1.y), new Vector3(_newVelocity1.x, 0, _newVelocity1.y)).y);
        float _sign2 = Mathf.Sign(Vector3.Cross(new Vector3(_relativePosition2.x, 0, _relativePosition2.y), new Vector3(_newVelocity2.x, 0, _newVelocity2.y)).y);
        
        float angularFactor = 0.5f;
        float _angularMomentum1 = _sign1 * _newVelocity1.magnitude *angularFactor;
        float _angularMomentum2 = _sign2 * _newVelocity2.magnitude *angularFactor;
        
        //Debug.Log($"Old Velocity1: {cumulVelocity}, Old Velocity2: {_otherMomentum}");
        //Debug.Log($"New Velocity1: {_newVelocity1}, New Velocity2: {_newVelocity2}");
        //Debug.Log($"Relative Position1: {_relativePosition1}, Sign1: {_sign1}");
        //Debug.Log($"Relative Position2: {_relativePosition2}, Sign2: {_sign2}");
        //Debug.Log($"Corrected Angular Momentum1: {_angularMomentum1*pMomentum}, Angular Momentum2: {_angularMomentum2*pMomentum}");
        
        AddWeaponOrbital(_angularMomentum1);
        pCharacterHit.AddWeaponOrbital(_angularMomentum2);
    }

    private void bodyHit(Character pCharacterHit) { }

    private Vector2 getHitMomentum() {
        return cumulVelocity;
    }

}
