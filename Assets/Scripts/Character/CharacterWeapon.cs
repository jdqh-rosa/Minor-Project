using System;
using System.Collections;
using UnityEngine;

public class CharacterWeapon : MonoBehaviour
{
    [SerializeField] private WeaponPart hilt;
    [SerializeField] private WeaponPart blade;
    [SerializeField] private WeaponPart tip;
    public Character Character;

    public Vector2 Velocity;
    public float Mass = 1f;
    public float WeaponDistance = 0f;

    public float OrbitalVelocity = 0f;
    public float ThrustVelocity = 0;
    private float swingDampingFactor = 0.98f;
    private float thrustDampingFactor = 0.98f;
    private float acceleration = 0.1f; // Acceleration factor for smooth control
    [SerializeField] private float maxOrbitalVelocity = 5f; // Limit max speed
    private float currentAngle = 0f;
    private float currentDistance = 0;
    [SerializeField] private float maxReach = 0.5f;

    [SerializeField] private float momentum = 0f;
    
    private WeaponState state;

    private void Start() {
        hilt.Weapon = this;
        blade.Weapon = this;
        tip.Weapon = this;

        WeaponDistance = Mathf.Max(0.01f, WeaponDistance);
        currentDistance = WeaponDistance;
    }

    private void Update() {
        switch (state) {
            case WeaponState.Idle:
                break;
            case WeaponState.Active:
                break;
            case WeaponState.Interruptable:
                break;
            case WeaponState.Reset: Retract();
                break;
        }
    }

    public void AddOrbitalVelocity(float addedMomentum) {
        OrbitalVelocity += addedMomentum * acceleration;
        OrbitalVelocity = Mathf.Clamp(OrbitalVelocity, -maxOrbitalVelocity, maxOrbitalVelocity);
    }

    public float OrbitalAccelerate(float pAcceleration) {
        if (currentDistance <= 0.001f) return OrbitalVelocity;

        float _angularAcceleration = Mathf.Clamp(pAcceleration / currentDistance, -maxOrbitalVelocity, maxOrbitalVelocity);
        
        OrbitalVelocity += _angularAcceleration;
        OrbitalVelocity = Mathf.Clamp(OrbitalVelocity, -maxOrbitalVelocity, maxOrbitalVelocity);

        if (Character.transform.name != "Player")
            Debug.Log($"Acceleration: {_angularAcceleration}, Velocity: {OrbitalVelocity}", this);

        return OrbitalVelocity;
    }

    public void ApplyThrust(float pTargetOffset, float pDuration) {
        StartCoroutine(ThrustRoutine(pTargetOffset, pDuration));
    }
    
    private IEnumerator ThrustRoutine(float targetOffset, float duration) {
        float elapsed = 0;
        float initialOffset = ThrustVelocity;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            ThrustVelocity = Mathf.Lerp(initialOffset, targetOffset, elapsed / duration);
            yield return null;
        }
        ThrustVelocity = targetOffset;
    }

    private void updateVelocity() {
        Velocity = CalculateOrbitalVelocity(OrbitalVelocity);
    }

    public Vector2 CalculateOrbitalVelocity(float pAngularDifference) {
        return new Vector2(
            -Mathf.Sin(currentAngle * Mathf.Deg2Rad),
            Mathf.Cos(currentAngle * Mathf.Deg2Rad)
        ) * (pAngularDifference * currentDistance);
    }

    public void UpdatePosition() {
        //float newAngle = Mathf.MoveTowardsAngle(currentAngle, currentAngle + angularDisplacement, RotationSpeed * Time.deltaTime);
        currentAngle = RadialHelper.NormalizeAngle(currentAngle + (OrbitalVelocity * Time.deltaTime));
        currentDistance = WeaponDistance + ThrustVelocity;
        currentDistance = Mathf.Clamp(currentDistance, WeaponDistance, maxReach);

        Vector2 orbitPos = RadialHelper.PolarToCart(currentAngle, currentDistance);
        
        transform.localPosition = new Vector3(orbitPos.x, transform.localPosition.y, orbitPos.y);
        transform.rotation = Quaternion.Euler(0, -currentAngle + 90, 0);

        OrbitalVelocity *= swingDampingFactor;
        ThrustVelocity *= thrustDampingFactor;
        
        if (Mathf.Abs(OrbitalVelocity) < 0.01f) OrbitalVelocity = 0;
        //if (Mathf.Abs(ThrustVelocity) < 0.01f) ThrustVelocity = 0;

        updateVelocity();
        
        momentum = VelocityToMomentum(OrbitalVelocity, currentDistance);
    }

    public float VelocityToMomentum(float pOrbitalVelocity, float pDistance) {
        return Mathf.Abs(Mass * pOrbitalVelocity * pDistance * pDistance);
    }

    public float MomentumToVelocity(float angularMomentum, float pDistance) {
        if (Mass <= 0 || pDistance <= 0) return 0; // Avoid division by zero
        return angularMomentum / (Mass * pDistance * pDistance);
    }

    private float retractClock=0;
    private void Retract() {
        if (currentDistance > WeaponDistance) {
            retractClock+=Time.deltaTime;
            currentDistance = Mathf.Lerp(currentDistance, WeaponDistance, retractClock);
        }
        else {
            retractClock = 0;
            currentDistance = WeaponDistance;
        }
    }

    public float GetAngle() {
        return currentAngle;
    }

    public void CollisionDetected(WeaponPart pPart, Character pCharacter, bool pIsClash, Vector3 pPointHit) {
        //get momentum from PoC 
        Vector3 _hitVector = pPointHit - Character.transform.position;
        Vector3 _weaponVector = pPart.transform.position - Character.transform.position;

        float _vectorDistance = Vector3.Dot(_weaponVector.normalized, _hitVector);
        float _pointMomentum = VelocityToMomentum(OrbitalVelocity, _vectorDistance);

        if (transform.parent.name == "Player") {
            //Debug.Log($"impact point: {pPointHit}, vector distance: {_vectorDistance}");
        }

        Character.CollisionDetected(pCharacter, pIsClash, _pointMomentum);
    }
}

public enum WeaponState
{
    Idle,
    Active,
    Interruptable,
    Reset
}