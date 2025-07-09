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

    private void Start() {
        if (hilt != null) {
            hilt.Weapon = this;
            Physics.IgnoreCollision(Character.Body.GetComponent<Collider>(), hilt.GetComponent<Collider>());
            //Debug.Log($"Ignoring: {Character.Body.transform.root.name} ↔ {hilt.transform.root.name}");
        }

        if (blade != null) {
            blade.Weapon = this;
            Physics.IgnoreCollision(Character.Body.GetComponent<Collider>(), blade.GetComponent<Collider>());
            //Debug.Log($"Ignoring: {Character.Body.transform.root.name} ↔ {blade.transform.root.name}");
        }

        if (tip != null) tip.Weapon = this;
        if (blade != null && tip != null) {
            Physics.IgnoreCollision(blade.GetComponent<Collider>(), tip.GetComponent<Collider>());
            //Debug.Log($"Ignoring: {blade.transform.root.name} ↔ {tip.transform.root.name}");
        }

        if (hilt != null && blade != null) {
            Physics.IgnoreCollision(hilt.GetComponent<Collider>(), blade.GetComponent<Collider>());
            //Debug.Log($"Ignoring: {hilt.transform.root.name} ↔ {blade.transform.root.name}");
        }

        weaponJoint.connectedBody = Character.GetComponent<Rigidbody>();
        rb.ResetInertiaTensor();

        //data.MaxTurnVelocity = Character.GetCharacterData().MaxRotationSpeed;
        data.WeaponDistance = Mathf.Max(0.01f, data.WeaponDistance);
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

    public float OrbitalAccelerate(float pAcceleration) {
        if (currentDistance <= 0.001f) return OrbitalVelocity;

        float _angularAcceleration;
        if (state == WeaponState.Active) {
            _angularAcceleration = Mathf.Clamp(pAcceleration / currentDistance, -data.MaxOrbitalVelocity, data.MaxOrbitalVelocity);
        }
        else {
            _angularAcceleration = Mathf.Clamp(pAcceleration / currentDistance, -data.MaxTurnVelocity, data.MaxTurnVelocity);
        }

        OrbitalVelocity += _angularAcceleration;
        OrbitalVelocity = Mathf.Clamp(OrbitalVelocity, -data.MaxOrbitalVelocity, data.MaxOrbitalVelocity);

        return OrbitalVelocity;
    }

    public void ApplyThrust(float pTargetOffset, float pDuration) {
        StartCoroutine(thrustRoutine(pTargetOffset, pDuration));
    }

    private IEnumerator thrustRoutine(float pTargetOffset, float pDuration) {
        float _elapsed = 0;
        float _initialOffset = ThrustVelocity;
        while (_elapsed < pDuration) {
            _elapsed += Time.fixedDeltaTime;
            ThrustVelocity = Mathf.Lerp(_initialOffset, pTargetOffset, _elapsed / pDuration);
            yield return null;
        }

        ThrustVelocity = pTargetOffset;
    }

    public float LinearKnockback(float pKnockbackForce) {
        ThrustKnockback += pKnockbackForce;
        return KnockbackVelocity;
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
        if(!isActiveAndEnabled) return;
        currentAngle += OrbitalVelocity * Time.fixedDeltaTime;
        currentAngle += KnockbackVelocity;
        currentAngle = RadialHelper.NormalizeAngle(currentAngle);
        currentDistance = Mathf.Clamp(data.WeaponDistance + ThrustVelocity + ThrustKnockback, data.WeaponDistance, data.MaxReach);

        Vector2 _orbitPos = RadialHelper.PolarToCart(currentAngle, currentDistance);

        Vector3 radialX = Character.transform.right;
        Vector3 radialZ = Character.transform.forward;
        Vector3 worldOffset = (_orbitPos.x * radialX) + (_orbitPos.y * radialZ);
        Vector3 _worldOrbitPos = Character.transform.position + worldOffset;

        Vector3 _radialWorld = (_worldOrbitPos - Character.transform.position);
        _radialWorld.y = 0f;

        Transform _cb = weaponJoint.connectedBody.transform;
        Vector3 _jointLocalRadial = _cb.InverseTransformDirection(_radialWorld);
        _jointLocalRadial = Vector3.ClampMagnitude(_jointLocalRadial, data.MaxReach);
        _jointLocalRadial.y = 0f;
        weaponJoint.targetPosition = _jointLocalRadial;

        Debug.DrawLine(Character.transform.position, _worldOrbitPos, Color.red);
        Debug.DrawLine(_worldOrbitPos, transform.position, Color.green);
        Debug.DrawRay(Character.transform.position, _radialWorld.normalized * 2f, Color.cyan);
        Vector3 knockVec = Quaternion.Euler(0, currentAngle, 0) * Vector3.right * KnockbackVelocity;
        
        Quaternion _desiredRotation = Quaternion.Euler(0, -currentAngle + 90f, 0);
        Quaternion _worldToJointSpaceRotation = Quaternion.Inverse(weaponJoint.connectedBody.rotation);
        Quaternion _localDesiredRotation = _worldToJointSpaceRotation * _desiredRotation;
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

    public void CollisionDetected(WeaponPart pPart, Character pCharacterHit, bool pIsClash, Vector3 pContactNormal) {
        Vector3 _relVel3D = Velocity - pCharacterHit.Weapon.Velocity;
        Vector2 _relVel2D = MiscHelper.Vec3ToVec2Pos(_relVel3D);

        float _mAttacker = pPart.Weapon.GetMass();
        Vector2 _pMomentum = _relVel2D * _mAttacker;

        if (pIsClash) {
            weaponHit(pCharacterHit, pContactNormal);
        }
        else {
            bodyHit(pPart, pCharacterHit, _pMomentum);
        }
    }

    private void weaponHit(Character pCharacterHit, Vector3 pContactNormal) {

        float _relAngVel = OrbitalVelocity - pCharacterHit.Weapon.OrbitalVelocity;
        if(Mathf.Abs(_relAngVel) < 0.01f) return;
        
        Vector3 _tangent = new Vector3(-Mathf.Sin(currentAngle * Mathf.Deg2Rad), 0, Mathf.Cos(currentAngle * Mathf.Deg2Rad));
        float _sign = Mathf.Sign(Vector3.Dot(_tangent, pContactNormal));

        float _myMass = GetMass();
        float _otherMass = pCharacterHit.Weapon.GetMass();
        float _invMassSum = (1f / _myMass) + (1f / _otherMass);

        float _dKnockback = Mathf.Abs(_relAngVel * _invMassSum * angularFactor) * _sign;
        KnockbackVelocity += _dKnockback;
        //Debug.Log($"Knoockback: {_dKnockback}, _relAngVel: {_relAngVel}, _invMassSum: {_invMassSum}, Orbital: {OrbitalVelocity}, OtherOrbital: {pCharacterHit.Weapon.OrbitalVelocity}", this);
        KnockbackVelocity = Math.Clamp(KnockbackVelocity, -Character.GetCharacterData().MaxRotationSpeed, Character.GetCharacterData().MaxRotationSpeed);
    }

    private void bodyHit(WeaponPart pPart, Character pCharacterHit, Vector2 pMomentum) {
        float AverageHealth = 1000f;
        float HighestSpeed = 1000f;
        float DamageRatio = 0.5f;

        //Debug.Log($"Velocity {pMomentum}, {pMomentum.magnitude}");
        //Debug.Log($"Momentum {momentum}");
        
        float _strength = Mathf.Clamp01(OrbitalVelocity / HighestSpeed);

        float _damageMod = 0;
        switch (pPart.PartType) {
            case WeaponPartType.Hilt:
                _damageMod = data.HiltDamageFactor;
                break;
            case WeaponPartType.Main:
                _damageMod = data.MainDamageFactor;
                break;
            case WeaponPartType.Tip:
                _damageMod = data.TipDamageFactor;
                break;
            default:
                _damageMod = 1f;
                break;
        }
        switch (pPart.ContactType) {
            case ContactType.Blunt:
                _damageMod *= data.BluntDamageFactor;
                break;
            case ContactType.Chop:
                _damageMod *= data.ChopDamageFactor;
                break;
            case ContactType.Poke:
                _damageMod *= data.PokeDamageFactor;
                break;
            case ContactType.Slash:
                _damageMod *= data.SlashDamageFactor;
                break;
        }

        pCharacterHit.TakeDamage(_strength * (AverageHealth * DamageRatio) * _damageMod);
    }

    private void OnCollisionEnter(Collision collision) {
        foreach (var contact in collision.contacts) {
            GameObject _thisObj = gameObject;
            GameObject _otherObj = contact.otherCollider.transform.root.gameObject;
            _otherObj = contact.otherCollider.gameObject;

            if (_thisObj == _otherObj || !CollisionTracker.TryRegisterCollision(_thisObj, _otherObj)) {
                //Debug.Log($"[Collision Skipped] Already handled collision between {_thisObj.name} and {_otherObj.name}");
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

            var _otherTrans = contact.otherCollider.transform;
            var _otherChar = _otherTrans.GetComponentInParent<Character>();
            if (_otherChar == null) continue;

            bool isClash = _otherTrans.GetComponent<WeaponPart>() != null;

            CollisionDetected(_part, _otherChar, isClash, contact.normal);
        }
    }

    public static class CollisionTracker
    {
        private static HashSet<(int, int)> handledPairs = new HashSet<(int, int)>();

        public static bool TryRegisterCollision(GameObject pA, GameObject pB) {
            int _idA = pA.GetInstanceID();
            int _idB = pB.GetInstanceID();
            var key = _idA < _idB ? (_idA, _idB) : (_idB, _idA);

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