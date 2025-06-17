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
    [SerializeField] private ConfigurableJoint weaponJoint;

    public Vector3 Velocity;

    private float acceleration = 0.1f; // Acceleration factor for smooth control
    [SerializeField] private float currentAngle = 0f;
    private float currentDistance = 0;

    public float OrbitalVelocity = 0f;
    public float ThrustVelocity = 0;
    public float KnockbackVelocity = 0;
    [SerializeField] private float momentum = 0f;

    private WeaponState state;

    private Quaternion initialLocalRot;

    private void Start() {
        hilt.Weapon = this;
        blade.Weapon = this;
        tip.Weapon = this;
        initialLocalRot = Quaternion.Inverse(weaponJoint.connectedBody.rotation) * transform.rotation;
        rb.ResetInertiaTensor();

        data.WeaponDistance = Mathf.Max(0.01f, data.WeaponDistance);
        currentDistance = data.WeaponDistance;

        Physics.IgnoreCollision(Character.Body.GetComponent<Collider>(), hilt.GetComponent<Collider>());
        Physics.IgnoreCollision(Character.Body.GetComponent<Collider>(), blade.GetComponent<Collider>());
        Physics.IgnoreCollision(hilt.GetComponent<Collider>(), blade.GetComponent<Collider>());
        Physics.IgnoreCollision(blade.GetComponent<Collider>(), tip.GetComponent<Collider>());

        Debug.Log($"Ignoring: {Character.Body.transform.root.name} ↔ {hilt.transform.root.name}");
        Debug.Log($"Ignoring: {Character.Body.transform.root.name} ↔ {blade.transform.root.name}");
        Debug.Log($"Ignoring: {hilt.transform.root.name} ↔ {blade.transform.root.name}");
        Debug.Log($"Ignoring: {blade.transform.root.name} ↔ {tip.transform.root.name}");

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

        float _angularAcceleration = Mathf.Clamp(pAcceleration / currentDistance, -data.MaxOrbitalVelocity,
            data.MaxOrbitalVelocity);

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
        currentAngle = RadialHelper.NormalizeAngle(currentAngle + (_adjustedVelocity * Time.fixedDeltaTime));
        currentDistance = Mathf.Clamp(data.WeaponDistance + ThrustVelocity, data.WeaponDistance, data.MaxReach);

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
        

        Quaternion _desiredRotation = Quaternion.Euler(0, -currentAngle + 90f, 0);
        Quaternion _worldToJointSpaceRotation = Quaternion.Inverse(weaponJoint.connectedBody.rotation);
        Quaternion _localDesiredRotation = _worldToJointSpaceRotation * _desiredRotation;
        //weaponJoint.targetRotation = Quaternion.Inverse(initialLocalRot) * _localDesiredRotation;
        weaponJoint.targetRotation = _localDesiredRotation;


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
        return data.MaxReach + tip.PartDistance;
    }

    public void CollisionDetected(WeaponPart pPart, Character pCharacterHit, bool pIsClash, Vector3 pPointHit) {
        Vector3 vAttacker = pCharacterHit.Weapon.Velocity;
        Vector3 vDefender = Velocity;

        Vector3 relVel3D = vAttacker - vDefender;
        Vector2 relVel2D = new Vector2(relVel3D.x, relVel3D.z);

        float mAttacker = pPart.Weapon.GetMass();
        Vector2 pMomentum = relVel2D * mAttacker;

        pCharacterHit.CollisionDetected(Character, pIsClash, pMomentum, pPointHit);
    }

    private void OnCollisionEnter(Collision collision) {
        foreach (var contact in collision.contacts) {
            GameObject _part = contact.thisCollider.transform.gameObject;
            WeaponPart _partPart;

            if (contact.otherCollider.transform.root == transform.root) continue;

            if (_part == hilt.gameObject) {
                _partPart = hilt;
            }
            else if (_part == blade.transform.gameObject) {
                _partPart = blade;
            }
            else if (_part == tip.gameObject) {
                _partPart = tip;
            }
            else {
                continue;
            }

            var otherTrans = contact.otherCollider.transform;
            var otherChar = otherTrans.GetComponentInParent<Character>();
            if (otherChar == null) continue;

            bool isClash = otherTrans.GetComponent<WeaponPart>() != null;

            CollisionDetected(_partPart, otherChar, isClash, contact.point);
        }
    }


    private void setupJoint() { }
}

public enum WeaponState
{
    Idle,
    Active,
    Reset
}