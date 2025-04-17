using System;
using System.Collections;
using UnityEngine;

public class CharacterWeapon : MonoBehaviour
{
    public Character Character;
    [SerializeField] private WeaponPart hilt;
    [SerializeField] private WeaponPart blade;
    [SerializeField] private WeaponPart tip;
    [SerializeField] private WeaponData data;

    public Vector2 Velocity;
    
    private float acceleration = 0.1f; // Acceleration factor for smooth control
    private float currentAngle = 0f;
    private float currentDistance = 0;
    
    public float OrbitalVelocity = 0f;
    public float ThrustVelocity = 0;
    [SerializeField] private float momentum = 0f;
    
    private WeaponState state;

    private void Start() {
        hilt.Weapon = this;
        blade.Weapon = this;
        tip.Weapon = this;

        data.WeaponDistance = Mathf.Max(0.01f, data.WeaponDistance);
        currentDistance = data.WeaponDistance;
    }

    private void Update() {
        switch (state) {
            case WeaponState.Idle:
                break;
            case WeaponState.Active:
                break;
            case WeaponState.Reset: Retract();
                break;
        }
    }

    public void AddOrbitalVelocity(float pAddedMomentum) {
        OrbitalVelocity += pAddedMomentum * acceleration;
        OrbitalVelocity = Mathf.Clamp(OrbitalVelocity, -data.MaxOrbitalVelocity, data.MaxOrbitalVelocity);
    }

    public float OrbitalAccelerate(float pAcceleration) {
        if (currentDistance <= 0.001f) return OrbitalVelocity;

        float _angularAcceleration = Mathf.Clamp(pAcceleration / currentDistance, -data.MaxOrbitalVelocity, data.MaxOrbitalVelocity);
        
        OrbitalVelocity += _angularAcceleration;
        OrbitalVelocity = Mathf.Clamp(OrbitalVelocity, -data.MaxOrbitalVelocity, data.MaxOrbitalVelocity);

        if (Character.transform.name != "Player")
            Debug.Log($"Acceleration: {_angularAcceleration}, Velocity: {OrbitalVelocity}", this);

        return OrbitalVelocity;
    }

    public void ApplyThrust(float pTargetOffset, float pDuration) {
        StartCoroutine(ThrustRoutine(pTargetOffset, pDuration));
    }
    
    private IEnumerator ThrustRoutine(float pTargetOffset, float pDuration) {
        float _elapsed = 0;
        float _initialOffset = ThrustVelocity;
        while (_elapsed < pDuration) {
            _elapsed += Time.deltaTime;
            ThrustVelocity = Mathf.Lerp(_initialOffset, pTargetOffset, _elapsed / pDuration);
            yield return null;
        }
        ThrustVelocity = pTargetOffset;
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
        currentAngle = RadialHelper.NormalizeAngle(currentAngle + (OrbitalVelocity * Time.deltaTime));
        currentDistance = data.WeaponDistance + ThrustVelocity;
        currentDistance = Mathf.Clamp(currentDistance, data.WeaponDistance, data.MaxReach);

        Vector2 _orbitPos = RadialHelper.PolarToCart(currentAngle, currentDistance);
        
        transform.localPosition = new Vector3(_orbitPos.x, transform.localPosition.y, _orbitPos.y);
        transform.rotation = Quaternion.Euler(0, -currentAngle + 90, 0);

        OrbitalVelocity *= data.SwingDampingFactor;
        ThrustVelocity *= data.ThrustDampingFactor;
        
        if (Mathf.Abs(OrbitalVelocity) < 0.01f) OrbitalVelocity = 0;
        //if (Mathf.Abs(ThrustVelocity) < 0.01f) ThrustVelocity = 0;

        updateVelocity();
        
        momentum = VelocityToMomentum(OrbitalVelocity, currentDistance);
    }

    public float VelocityToMomentum(float pOrbitalVelocity, float pDistance) {
        return Mathf.Abs(data.Mass * pOrbitalVelocity * pDistance * pDistance);
    }

    public float MomentumToVelocity(float pAngularMomentum, float pDistance) {
        if (data.Mass <= 0 || pDistance <= 0) return 0;
        return pAngularMomentum / (data.Mass * pDistance * pDistance);
    }

    private float retractClock=0;
    private void Retract() {
        if (currentDistance > data.WeaponDistance) {
            retractClock+=Time.deltaTime;
            currentDistance = Mathf.Lerp(currentDistance, data.WeaponDistance, retractClock);
        }
        else {
            retractClock = 0;
            currentDistance = data.WeaponDistance;
        }
    }

    public float GetAngle() {
        return currentAngle;
    }

    public float GetMass() {
        return data.Mass;
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
    Reset
}