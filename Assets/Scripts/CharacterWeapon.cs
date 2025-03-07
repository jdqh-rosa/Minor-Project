using System;
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

    private float orbitalVelocity = 0f;
    private Vector2 perpendicularVelocity;
    private float dampingFactor = 0.98f; // Smooths motion
    private float acceleration = 0.1f; // Acceleration factor for smooth control
    [SerializeField] private float maxOrbitalVelocity = 5f; // Limit max speed
    private float currentAngle = 0f;

    [SerializeField] private float momentum = 0f;

    private void Start() {
        hilt.Weapon = this;
        blade.Weapon = this;
        tip.Weapon = this;

        WeaponDistance = Mathf.Max(0.01f, WeaponDistance);
    }

    public void AddOrbitalVelocity(float addedMomentum) {
        orbitalVelocity += addedMomentum * acceleration;
        orbitalVelocity = Mathf.Clamp(orbitalVelocity, -maxOrbitalVelocity, maxOrbitalVelocity);
    }

    public float OrbitalAccelerate(float pAcceleration, float pTime) {
        if (WeaponDistance <= 0.001f) return orbitalVelocity;

        float _angularAcceleration =
            Mathf.Clamp(pAcceleration / WeaponDistance, -maxOrbitalVelocity, maxOrbitalVelocity);
        orbitalVelocity += _angularAcceleration;// * pTime;
        orbitalVelocity = Mathf.Clamp(orbitalVelocity, -maxOrbitalVelocity, maxOrbitalVelocity);

        if (Character.transform.name != "Player")
            Debug.Log($"Acceleration: {_angularAcceleration}, Velocity: {orbitalVelocity}", this);

        return orbitalVelocity;
    }

    public void AddPerpendicularVelocity(Vector2 pAddedMomentum) {
        perpendicularVelocity += pAddedMomentum * acceleration;
    }

    public void UpdateVelocity() {
        Velocity = CalculateVelocity(orbitalVelocity);
    }

    public Vector2 CalculateVelocity(float pAngularDifference) {
        return new Vector2(
            -Mathf.Sin(currentAngle * Mathf.Deg2Rad),
            Mathf.Cos(currentAngle * Mathf.Deg2Rad)
        ) * (pAngularDifference * WeaponDistance);
    }

    public void UpdatePosition() {
        //float newAngle = Mathf.MoveTowardsAngle(currentAngle, currentAngle + angularDisplacement, RotationSpeed * Time.deltaTime);
        currentAngle = RadialHelper.NormalizeAngle(currentAngle + (orbitalVelocity * Time.deltaTime));

        Vector2 orbitPos = RadialHelper.PolarToCart(currentAngle, WeaponDistance);

        transform.localPosition = new Vector3(orbitPos.x, transform.localPosition.y, orbitPos.y);
        transform.rotation = Quaternion.Euler(0, -currentAngle + 90, 0);

        orbitalVelocity *= dampingFactor;
        if (Mathf.Abs(orbitalVelocity) < 0.01f) orbitalVelocity = 0;

        UpdateVelocity();
        momentum = VelocityToMomentum(orbitalVelocity, WeaponDistance);
    }

    public float VelocityToMomentum(float pOrbitalVelocity, float pDistance) {
        return Mathf.Abs(Mass * pOrbitalVelocity * pDistance * pDistance);
    }

    public float MomentumToVelocity(float angularMomentum, float pDistance) {
        if (Mass <= 0 || pDistance <= 0) return 0; // Avoid division by zero
        return angularMomentum / (Mass * pDistance * pDistance);
    }

    public float GetAngle() {
        return currentAngle;
    }

    public void CollisionDetected(WeaponPart pPart, Character pCharacter, bool pIsClash, Vector3 pPointHit) {
        //get momentum from PoC 
        Vector3 _hitVector = pPointHit - Character.transform.position;
        Vector3 _weaponVector = pPart.transform.position - Character.transform.position;

        float _vectorDistance = Vector3.Dot(_weaponVector.normalized, _hitVector);
        float _pointMomentum = VelocityToMomentum(orbitalVelocity, _vectorDistance);

        if (transform.parent.name == "Player") {
            //Debug.Log($"impact point: {pPointHit}, vector distance: {_vectorDistance}");
        }

        Character.CollisionDetected(pCharacter, pIsClash, _pointMomentum);
    }
}