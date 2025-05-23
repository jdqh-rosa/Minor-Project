using System;
using System.Collections;
using UnityEngine;

public class CharacterWeapon : MonoBehaviour
{
    public Character Character;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private WeaponPart hilt;
    [SerializeField] private WeaponPart blade;
    [SerializeField] private WeaponPart tip;
    [SerializeField] private WeaponData data;

    public Vector3 Velocity;
    
    private float acceleration = 0.1f; // Acceleration factor for smooth control
    [SerializeField] private float currentAngle = 0f;
    private float currentDistance = 0;
    
    public float OrbitalVelocity = 0f;
    public float ThrustVelocity = 0;
    public float KnockbackVelocity = 0;
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

        return OrbitalVelocity;
    }
    
    public float OrbitalKnockback(float pKnockbackForce) {
        if (currentDistance <= 0.001f) return OrbitalVelocity;

        KnockbackVelocity += pKnockbackForce;

        return KnockbackVelocity;
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
        Velocity = MiscHelper.Vec2ToVec3Pos(CalculateOrbitalVelocity(OrbitalVelocity));
        momentum = VelocityToMomentum(OrbitalVelocity, currentDistance);
    }

    public Vector2 CalculateOrbitalVelocity(float pAngularDifference) {
        return new Vector2(
            -Mathf.Sin(currentAngle * Mathf.Deg2Rad),
            Mathf.Cos(currentAngle * Mathf.Deg2Rad)
        ) * (pAngularDifference * currentDistance);
    }

    public void UpdatePosition() {
        float _adjustedVelocity = OrbitalVelocity + KnockbackVelocity;
        currentAngle = RadialHelper.NormalizeAngle(currentAngle + (_adjustedVelocity * Time.deltaTime));
        currentDistance = data.WeaponDistance + ThrustVelocity;
        currentDistance = Mathf.Clamp(currentDistance, data.WeaponDistance, data.MaxReach);

        Vector2 _orbitPos = RadialHelper.PolarToCart(currentAngle, currentDistance);
        
        //Vector3 parentWorldPos = transform.parent.position;
        //Vector3 targetWorldPos = parentWorldPos + new Vector3(_orbitPos.x, transform.localPosition.y, _orbitPos.y);
        //rb.MovePosition(targetWorldPos);
        //rb.MoveRotation(Quaternion.Euler(0f, -currentAngle + 90f, 0f));
        
        transform.localPosition = new Vector3(_orbitPos.x, transform.localPosition.y, _orbitPos.y);
        transform.rotation = Quaternion.Euler(0, -currentAngle + 90, 0);

        OrbitalVelocity *= data.SwingDampingFactor;
        ThrustVelocity *= data.ThrustDampingFactor;
        KnockbackVelocity *= data.SwingDampingFactor;
        
        if (Mathf.Abs(OrbitalVelocity) < 0.01f) OrbitalVelocity = 0;
        if (Mathf.Abs(KnockbackVelocity) < 0.01f) KnockbackVelocity = 0;
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

    public float GetRange()
    {
        return  data.MaxReach + tip.PartDistance;
    }

    public void CollisionDetected(WeaponPart pPart, Character pCharacterHit, bool pIsClash, Vector3 pPointHit) {
        
        Vector3 vAttacker = pCharacterHit.Weapon.Velocity;
        Vector3 vDefender = Velocity; 
        
        Vector3 relVel3D = vAttacker - vDefender;
        Vector2 relVel2D = new Vector2(relVel3D.x, relVel3D.z);
        
        float mAttacker = pPart.Weapon.GetMass();
        Vector2 pMomentum   = relVel2D * mAttacker;
        
        pCharacterHit.CollisionDetected(Character, pIsClash, pMomentum, pPointHit);
    }
    
    // private void OnCollisionEnter(Collision collision)
    // {
    //     foreach (var contact in collision.contacts) {
    //         GameObject _part = contact.thisCollider.transform.gameObject;
    //         WeaponPart _partPart;
    //         if (_part == hilt.gameObject) {
    //             _partPart = hilt;
    //         }
    //         else if (_part == blade.transform.gameObject) {
    //             _partPart = blade;
    //         }
    //         else if (_part == tip.gameObject) {
    //             _partPart = tip;
    //         }
    //         else {
    //             return;
    //         }
    //         
    //         var otherTrans = contact.otherCollider.transform;
    //         var otherChar  = otherTrans.GetComponentInParent<Character>();
    //         if (otherChar == null) continue;
    //
    //         bool isClash = otherTrans.CompareTag("WeaponPart");
    //
    //         CollisionDetected(_partPart, otherChar, isClash, contact.point);
    //     }
    // }
    
}

public enum WeaponState
{
    Idle,
    Active,
    Reset
}