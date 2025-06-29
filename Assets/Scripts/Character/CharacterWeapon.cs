using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterWeapon : MonoBehaviour
{
    public Character Character;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private WeaponPart hilt;
    [SerializeField] private WeaponPart blade;
    [SerializeField] private WeaponPart tip;
    [SerializeField] private WeaponData data;
    [SerializeField] private ConfigurableJoint weaponJoint;

    public Vector3 Velocity;

    private float acceleration = 0.1f;
    [SerializeField] private float angularFactor = 0.1f;
    [SerializeField] private float currentAngle = 0f;
    private float currentDistance = 0;

    public float OrbitalVelocity = 0f;
    public float ThrustVelocity = 0;
    public float KnockbackVelocity = 0;
    public float ThrustKnockback = 0;
    [SerializeField] private float momentum = 0f;

    private WeaponState state;

    public WeaponState CurrentState
    {
        get => state;
        set => state = value;
    }

    private Quaternion initialLocalRot;

    private void Start() {
        if (hilt != null) {
            hilt.Weapon = this;
            Physics.IgnoreCollision(Character.Body.GetComponent<Collider>(), hilt.GetComponent<Collider>());
            Debug.Log($"Ignoring: {Character.Body.transform.root.name} ↔ {hilt.transform.root.name}");
        }

        if (blade != null) {
            blade.Weapon = this;
            Physics.IgnoreCollision(Character.Body.GetComponent<Collider>(), blade.GetComponent<Collider>());
            Debug.Log($"Ignoring: {Character.Body.transform.root.name} ↔ {blade.transform.root.name}");
        }

        if (tip != null) tip.Weapon = this;
        if (blade != null && tip != null) {
            Physics.IgnoreCollision(blade.GetComponent<Collider>(), tip.GetComponent<Collider>());
            Debug.Log($"Ignoring: {blade.transform.root.name} ↔ {tip.transform.root.name}");
        }

        if (hilt != null && blade != null) {
            Physics.IgnoreCollision(hilt.GetComponent<Collider>(), blade.GetComponent<Collider>());
            Debug.Log($"Ignoring: {hilt.transform.root.name} ↔ {blade.transform.root.name}");
        }

        weaponJoint.connectedBody = Character.GetComponent<Rigidbody>();
        initialLocalRot = Quaternion.Inverse(weaponJoint.connectedBody.rotation) * transform.rotation;
        rb.ResetInertiaTensor();

        //data.MaxTurnVelocity = Character.GetCharacterData().MaxRotationSpeed;
        //data.WeaponDistance = Mathf.Max(0.01f, data.WeaponDistance);
        currentDistance = data.WeaponDistance;

        weaponJoint.axis = Character.transform.right;
        weaponJoint.secondaryAxis = Character.transform.up;
    }

    private void Update() {
        switch (state) {
            case WeaponState.Idle:
                break;
            case WeaponState.Active:
                break;
            case WeaponState.Reset:
                Retract();
                break;
        }
    }

    public void AddOrbitalVelocity(float pAddedMomentum) {
        OrbitalVelocity += pAddedMomentum * acceleration;
        OrbitalVelocity = Mathf.Clamp(OrbitalVelocity, -data.MaxOrbitalVelocity, data.MaxOrbitalVelocity);
    }

    public float OrbitalAccelerate(float pAcceleration) {
        if (currentDistance <= 0.001f) return OrbitalVelocity;

        float _angularAcceleration;
        if (state == WeaponState.Active) {
            _angularAcceleration = Mathf.Clamp(pAcceleration / currentDistance, -data.MaxOrbitalVelocity,
                data.MaxOrbitalVelocity);
        }
        else {
            _angularAcceleration =
                Mathf.Clamp(pAcceleration / currentDistance, -data.MaxTurnVelocity, data.MaxTurnVelocity);
        }

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
            _elapsed += Time.fixedDeltaTime;
            ThrustVelocity = Mathf.Lerp(_initialOffset, pTargetOffset, _elapsed / pDuration);
            yield return null;
        }

        ThrustVelocity = pTargetOffset;
    }

    public void LinearKnockback(float pKnockbackForce) {
        if (currentDistance <= 0.001f) return;

        //ThrustKnockback += pKnockbackForce;

        //return KnockbackVelocity;
    }

    private void updateVelocity() {
        Velocity = MiscHelper.Vec2ToVec3Pos(CalculateOrbitalVelocity(OrbitalVelocity));
        momentum = VelocityToMomentum(OrbitalVelocity, currentDistance);
    }

    private Vector2 CalculateOrbitalVelocity(float pAngularDifference) {
        return new Vector2(
            -Mathf.Sin(currentAngle * Mathf.Deg2Rad),
            Mathf.Cos(currentAngle * Mathf.Deg2Rad)
        ) * (pAngularDifference * currentDistance);
    }

    public void UpdatePosition() {
        currentAngle += OrbitalVelocity * Time.fixedDeltaTime;
        currentAngle += KnockbackVelocity;
        currentAngle = RadialHelper.NormalizeAngle(currentAngle);
        currentDistance = Mathf.Clamp(data.WeaponDistance + ThrustVelocity + ThrustKnockback, data.WeaponDistance, data.MaxReach);

        Vector2 _orbitPos = RadialHelper.PolarToCart(currentAngle, currentDistance);

        Vector3 radialX = Character.transform.right;
        Vector3 radialZ = Character.transform.forward;
        Vector3 worldOffset = (_orbitPos.x * radialX) + (_orbitPos.y * radialZ);
        Vector3 _worldOrbitPos = Character.transform.position + worldOffset;

        Vector3 radialWorld = (_worldOrbitPos - Character.transform.position);
        radialWorld.y = 0f;

        Transform _cb = weaponJoint.connectedBody.transform;
        Vector3 _jointLocalRadial = _cb.InverseTransformDirection(radialWorld);
        _jointLocalRadial = Vector3.ClampMagnitude(_jointLocalRadial, data.MaxReach);
        _jointLocalRadial.y = 0f;
        weaponJoint.targetPosition = _jointLocalRadial;


        Debug.DrawLine(Character.transform.position, _worldOrbitPos, Color.red);
        Debug.DrawLine(_worldOrbitPos, transform.position, Color.green);
        Debug.DrawRay(Character.transform.position, radialWorld.normalized * 2f, Color.cyan);
        Vector3 knockVec = Quaternion.Euler(0, currentAngle, 0) * Vector3.right * KnockbackVelocity;
        //Debug.DrawRay(transform.position, knockVec, Color.magenta, 0.1f);


        Quaternion _desiredRotation = Quaternion.Euler(0, -currentAngle + 90f, 0);
        Quaternion _worldToJointSpaceRotation = Quaternion.Inverse(weaponJoint.connectedBody.rotation);
        Quaternion _localDesiredRotation = _worldToJointSpaceRotation * _desiredRotation;
        //weaponJoint.targetRotation = Quaternion.Inverse(initialLocalRot) * _localDesiredRotation;
        weaponJoint.targetRotation = _localDesiredRotation;


        OrbitalVelocity *= data.SwingDampingFactor;
        ThrustVelocity *= data.ThrustDampingFactor;
        KnockbackVelocity *= data.SwingDampingFactor * 0.5f;
        //ThrustKnockback *= data.ThrustDampingFactor * 0.5f;
        if (Mathf.Abs(OrbitalVelocity) < 0.01f) OrbitalVelocity = 0;
        if (Mathf.Abs(KnockbackVelocity) < 0.001f) KnockbackVelocity = 0;
        if (Mathf.Abs(ThrustVelocity) < 0.001f) ThrustVelocity = 0;
        //if (Mathf.Abs(ThrustKnockback) < 0.01f) ThrustKnockback = 0;

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

    private float retractClock = 0;

    private void Retract() {
        if (currentDistance > data.WeaponDistance) {
            retractClock += Time.deltaTime;
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

    public float GetRange() {
        return tip.PartDistance;
    }

    public void CollisionDetected(WeaponPart pPart, Character pCharacterHit, bool pIsClash, Vector3 pPointHit, Vector3 pContactNormal) {
        //Vector3 vAttacker = pCharacterHit.Weapon.Velocity;
        //Vector3 vDefender = Velocity;

        Vector3 relVel3D = Velocity - pCharacterHit.Weapon.Velocity;
        Vector2 relVel2D = new Vector2(relVel3D.x, relVel3D.z);

        float mAttacker = pPart.Weapon.GetMass();
        Vector2 pMomentum = relVel2D * mAttacker;

        //pCharacterHit.CollisionDetected(Character, pIsClash, pMomentum, pPointHit);

        if (pIsClash) {
            weaponHit(pCharacterHit, pMomentum, pPointHit, pContactNormal);
        }
        else {
            bodyHit(pPart, pCharacterHit, pMomentum, pPointHit);
        }
    }

    private void weaponHit(Character pCharacterHit, Vector2 pMomentum, Vector3 pPointHit, Vector3 pContactNormal) {

        float _relAngVel = OrbitalVelocity - pCharacterHit.Weapon.OrbitalVelocity;
        if(Mathf.Abs(_relAngVel) < 0.01f) return;
        
        Vector3 _tangent = new Vector3(-Mathf.Sin(currentAngle * Mathf.Deg2Rad), 0, Mathf.Cos(currentAngle * Mathf.Deg2Rad));
        float _sign = Mathf.Sign(Vector3.Dot(_tangent, pContactNormal));

        //float _massFactor = GetMass();

        //float dKnockback = _relAngVel * _sign * _massFactor * angularFactor;
        float dKnockback = _relAngVel * _sign * angularFactor;
        KnockbackVelocity += dKnockback;
        KnockbackVelocity = Math.Clamp(KnockbackVelocity, -Character.GetCharacterData().MaxRotationSpeed, Character.GetCharacterData().MaxRotationSpeed);
        
        // float _myMass = GetMass();
        // float _otherMass = pCharacterHit.Weapon.GetMass();
        // float _invMassSum = (1f / _myMass) + (1f / _otherMass);
        // Vector3 _velocity = new Vector3(Velocity.x, 0, Velocity.z);
        // Vector3 _otherVelocity = new Vector3(pCharacterHit.Weapon.Velocity.x, 0, pCharacterHit.Weapon.Velocity.z);
        // Vector3 _relativeVelocity = _velocity - _otherVelocity;
        // _relativeVelocity.y = 0;
        // Vector3 _normal = _relativeVelocity.normalized;
        // if (_normal.sqrMagnitude < 1e-4f) return;
        // float _velAlongNormal = Vector3.Dot(_relativeVelocity, pContactNormal);
        // if (_velAlongNormal >= 0f) return;
        // float _bounciness = 0.8f;
        // float _impulseFactor = -(1f + _bounciness) * _velAlongNormal / _invMassSum;
        // Debug.Log($"J={_impulseFactor:F2}, velAlong={_velAlongNormal:F2}");
        // Vector3 _impulseVec = _normal * _impulseFactor;
        // Vector3 _leverArm = pPointHit - rb.worldCenterOfMass;
        // _leverArm.y = 0;
        // Vector3 _torque = Vector3.Cross(_leverArm, _impulseVec);
        // float _torqueY = _torque.y;
        // float _angularFactor = 1f;
        // float _dKnockback = _torqueY * _angularFactor;
        // if(Mathf.Abs(_dKnockback) < 0.01f) return;
        // KnockbackVelocity += _dKnockback;
        // KnockbackVelocity = Math.Clamp(KnockbackVelocity, -data.MaxOrbitalVelocity, data.MaxOrbitalVelocity);

        //float _totalMass = _myMass + _otherMass;
        //float _netMomentum = momentum - pCharacterHit.Weapon.momentum;
        //float _impulse = _netMomentum / _totalMass;
        //float _myImpulse = _impulse * _otherMass;
        //float _otherImpulse = _impulse * GetMass();
        //float _appliedImpulse = _netMomentum * (_otherMass / _totalMass);
        //float _relativeVelocity = momentum / GetMass() - pCharacterHit.Weapon.momentum / _otherMass;
        //Vector3 _r3 = pPointHit - rb.worldCenterOfMass;
        //_r3.y = 0;
        //Vector3 _F = new Vector3(pMomentum.x, 0f, pMomentum.y);
        //float torqueY = Vector3.Cross(_F, _r3).normalized.y;
        //float _angularFactor = 0.5f;
        //float _dKnockback = torqueY * _appliedImpulse * _angularFactor;
        //KnockbackVelocity += _dKnockback;

        // Vector3 _radialDir = (rb.position - Character.transform.position);
        // _radialDir.y = 0;
        // _radialDir.Normalize();
        // float _thrustImpulse = Vector3.Dot(_F, _radialDir);
        //float _radialFactor = 0.05f;
        // float _dThrust = _thrustImpulse * _radialFactor;
        // ThrustKnockback += _dThrust;

    }

    private void bodyHit(WeaponPart pPart, Character pCharacterHit, Vector2 pMomentum, Vector3 pPointHit) {
        float AverageHealth = 1000f;
        float HighestSpeed = 4000f;
        float DamageRatio = 0.5f;

        Debug.Log($"Velocity {pMomentum}, {pMomentum.magnitude}");
        Debug.Log($"Momentum {momentum}");
        
        float _strength = Mathf.Clamp01(momentum / HighestSpeed);

        pCharacterHit.TakeDamage(_strength * (AverageHealth * DamageRatio));
    }

    private void OnCollisionEnter(Collision collision) {
        foreach (var contact in collision.contacts) {
            GameObject thisObj = gameObject;
            GameObject otherObj = contact.otherCollider.transform.root.gameObject;

            // Skip if same root or already handled
            if (thisObj == otherObj || !CollisionTracker.TryRegisterCollision(thisObj, otherObj)) {
                Debug.Log($"[Collision Skipped] Already handled collision between {thisObj.name} and {otherObj.name}");
                return;
            }

            GameObject _partObject = contact.thisCollider.transform.gameObject;
            WeaponPart _part;

            if (contact.otherCollider.transform.root == transform.root) continue;

            
            switch (_partObject.GetComponent<WeaponPart>().PartType) {
                case WeaponPartType.Hilt:
                    _part = hilt;
                    break;
                case WeaponPartType.Main:
                    _part = blade;
                    break;
                case WeaponPartType.Tip:
                    _part = tip;
                    break;
                default:
                    continue;
            }

            var otherTrans = contact.otherCollider.transform;
            var otherChar = otherTrans.GetComponentInParent<Character>();
            if (otherChar == null) continue;

            bool isClash = otherTrans.GetComponent<WeaponPart>() != null;

            CollisionDetected(_part, otherChar, isClash, contact.point, contact.normal);
        }
    }

    public static class CollisionTracker
    {
        private static HashSet<(int, int)> handledPairs = new HashSet<(int, int)>();

        public static bool TryRegisterCollision(GameObject a, GameObject b) {
            int idA = a.GetInstanceID();
            int idB = b.GetInstanceID();
            var key = idA < idB ? (idA, idB) : (idB, idA);

            if (handledPairs.Contains(key)) {
                return false;
            }

            handledPairs.Add(key);
            return true;
        }

        public static void ClearFrameCollisions() {
            handledPairs.Clear();
        }
    }

    public WeaponData GetWeaponData() {
        return data;
    }

    public void SetWeaponCharacter(Character pCharacter) {
        Character = pCharacter;
        weaponJoint.connectedBody = pCharacter.GetComponent<Rigidbody>();
    }
}

public enum WeaponState
{
    Idle,
    Active,
    Reset
}