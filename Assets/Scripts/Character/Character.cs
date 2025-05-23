using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class Character : MonoBehaviour
{
    public Rigidbody RigidBody;
    public CharacterBody Body;
    public CharacterWeapon Weapon;
    public CombatSM CombatSM;
    [SerializeField] private CharacterData data;

    Vector3 moveDirection = Vector3.zero;
    Vector3 movePosition = Vector3.zero;
    private bool usePosition = false;
    [SerializeField] Vector2 lookDirection;

    private Vector2 cumulVelocity;
    private bool isAttacking = false;

    private void Awake() {
        if (Weapon == null || Body == null) {
            Debug.LogError("Weapon or Body is not assigned.");
            return;
        }

        Weapon.Character = this;
        Body.Character = this;

        CombatSM = GetComponent<CombatSM>();
        if (!CombatSM) {
            CombatSM = gameObject.AddComponent<CombatSM>();
        }

        CombatSM.Character = this;
        var _idle = new IdleCombatState("Idle");
        CombatSM.AddState(_idle);
        CombatSM.AddState(new JabState("Jab"), data.JabState);
        CombatSM.AddState(new SwipeState("Swipe"), data.SwipeState);
        CombatSM.AddState(new ThrustState("Thrust"), data.ThrustState);
        CombatSM.AddState(new SwingState("Swing"), data.SwingState);
        CombatSM.AddState(new StrideState("Stride"), data.StrideState);
        CombatSM.AddState(new DodgeState("Dodge"), data.DodgeState);
        CombatSM.InitialState = _idle;
    }

    private void Update() {
        var actionType = CombatSM.GetCurrentState().actionType;
        isAttacking = actionType != ActionType.None && actionType != ActionType.Stride &&
                      actionType != ActionType.Dodge;
    }

    public void CumulativeVelocity() {
        cumulVelocity = Body.Velocity + Weapon.Velocity;
    }

    public void SetLookDirection(Vector2 pLookDirection) {
        lookDirection = pLookDirection;
    }

    public void SetCharacterDirection(Vector3 pDir) {
        moveDirection = pDir;
    }

    public Vector3 GetCharacterDirection() {
        return moveDirection;
    }

    public void SetCharacterPosition(Vector3 pPos) {
        movePosition = pPos;
        movePosition.y = transform.position.y;
        usePosition = true;
    }

    public void AddWeaponOrbital(float pAdditionalMomentum) {
        Weapon.OrbitalAccelerate(pAdditionalMomentum);
    }

    public void AddWeaponKnockback(float pAdditionalMomentum) {
        Weapon.OrbitalKnockback(pAdditionalMomentum);
    }

    public void RotateWeaponTowardsAngle(float pTargetAngle) {
        float _angularDifference = Mathf.DeltaAngle(GetWeaponAngle(), pTargetAngle);

        if (Mathf.Abs(_angularDifference) < data.DeadZoneThreshold) {
            AddWeaponOrbital(0);
            return;
        }

        float _currentAngularVelocity = GetWeaponOrbital();

        // --- PD Controller for angular motion ---
        // kp: proportional gain (affects how strongly the error is corrected)
        // kd: derivative gain (affects how strongly current angular velocity is damped)
        float _kp = data.RotationSpeed * data.DampingFactor;
        float _kd = data.VelocityDamping;
        // The control signal calculates the "torque" needed:
        float _controlSignal = _kp * _angularDifference - _kd * _currentAngularVelocity;

        float _newAngularVelocity = _currentAngularVelocity + _controlSignal * Time.deltaTime;
        _newAngularVelocity = Mathf.Clamp(_newAngularVelocity, -data.MaxRotationSpeed, data.MaxRotationSpeed);

        float addedMomentum = _newAngularVelocity - _currentAngularVelocity;
        AddWeaponOrbital(addedMomentum);
    }

    public void RotateWeaponWithForce(float pTargetAngle, float pForce) {
        float _angularDifference = Mathf.DeltaAngle(GetWeaponAngle(), pTargetAngle);
        if (Mathf.Abs(_angularDifference) < data.DeadZoneThreshold) {
            AddWeaponOrbital(0);
            return;
        }

        float _sign = (_angularDifference < 0) ? -1 : 1;
        AddWeaponOrbital(pForce * _sign);
    }

    void FixedUpdate() {
        bodyFunctions();
        weaponFunctions();

        CumulativeVelocity();
    }

    private void bodyFunctions() {
        if (usePosition) {
            moveToPoint(movePosition);
        }
        else {
            moveInDirection(moveDirection.normalized);
        }

        usePosition = false;
    }

    private void moveToPoint(Vector3 pPoint) {
        Vector3 diffVec = pPoint - transform.position;
        Vector3 _movementVec = Body.Step(diffVec.normalized) * Mathf.Min(diffVec.magnitude, Body.GetStepLength());
        Move(_movementVec);
    }

    private void moveInDirection(Vector3 pDirection) {
        Move(Body.Step(pDirection));
    }

    public void Move(Vector3 pMove) {
        transform.position += pMove;
    }

    private void weaponFunctions() {
        Weapon.UpdatePosition();
    }

    public float GetWeaponAngle() {
        return Weapon.GetAngle();
    }

    public float GetWeaponRange() {
        return Weapon.GetRange();
    }

    public Vector3 GetWeaponPosition() {
        return Weapon.transform.position;
    }

    public float GetWeaponOrbital() {
        return Weapon.OrbitalVelocity;
    }

    public CharacterData GetData() {
        return data;
    }

    public bool IsAttacking() {
        return isAttacking;
    }

    public void Attack(ActionInput pAttackInput, float pTargetAngle) {
        CombatSM.Attack(pAttackInput, pTargetAngle, checkLinearAttack(pTargetAngle));
    }

    public void Attack(ActionType pAttackType, float pTargetAngle) {
        CombatSM.Attack(pAttackType, pTargetAngle);
    }

    private bool checkLinearAttack(float pTargetAngle) {
        return Mathf.Abs(Mathf.DeltaAngle(GetWeaponAngle(), pTargetAngle)) < data.LinearAttackZone;
    }

    public void Defend(ActionInput pAttackInput, float pTargetAngle) {
        ActionType _actionType = ActionType.None;
        if (pAttackInput == ActionInput.Press) {
            _actionType = ActionType.Parry;
        }
        else {
            _actionType = ActionType.Block;
        }

        Debug.Log($"Defend action type: {_actionType}");
    }

    public void CollisionDetected(Character pCharacterHit, bool pIsClash, Vector2 pMomentum, Vector3 pPointHit) {
        if (pIsClash) {
            weaponHit(pCharacterHit, pMomentum, pPointHit);
        }
        else {
            bodyHit(pCharacterHit);
        }
    }

    private void weaponHit(Character pCharacterHit, Vector2 pMomentum, Vector3 pPointHit) {
        Vector2 _v1 = cumulVelocity;
        Vector2 _v2 = pMomentum;

        Vector2 _impactDirection;
        if (_v1.sqrMagnitude < 1e-4f) _impactDirection = _v2.normalized;
        else if (_v2.sqrMagnitude < 1e-4f) _impactDirection = _v1.normalized;
        else _impactDirection = (_v1 - _v2).normalized;

        float u1 = Vector2.Dot(_v1, _impactDirection);
        float u2 = Vector2.Dot(_v2, _impactDirection);
        
        float m1 = this.Weapon.GetMass();
        float m2 = pCharacterHit.Weapon.GetMass();
        float v1new = ((m1 - m2)/(m1 + m2))*u1 + (2*m2/(m1 + m2))*u2;
        float v2new = (2*m1/(m1 + m2))*u1 + ((m2 - m1)/(m1 + m2))*u2;

        _v1 += (v1new - u1)*_impactDirection;
        _v2 += (v2new - u2)*_impactDirection;
        
        _v1 *= data.CollisionElasticity;
        _v2 *= data.CollisionElasticity;

        Vector3 gripPos = GetWeaponPosition();
        Vector3 theirGripPos = pCharacterHit.GetWeaponPosition();

        Vector3 contactPos = pPointHit;
        Vector3 rA = contactPos - gripPos;
        Vector3 rB = contactPos - theirGripPos;

        Vector3 pA = new Vector3(_v1.x, 0f, _v1.y);
        Vector3 pB = new Vector3(_v2.x, 0f, _v2.y);
        
        float angularImpulse = Vector3.Cross(rA, pA).y;
        float angularImpulseB = Vector3.Cross(rB, pB).y;
        Debug.Log($"r={rA.magnitude:F2}, p={pA.magnitude:F2}, L={angularImpulse:F2}");
        Debug.Log($"r={rB.magnitude:F2}, p={pB.magnitude:F2}, L={angularImpulseB:F2}");

        float appliedAngular = angularImpulse * data.AngularFactor;
        float appliedAngularB = angularImpulseB * data.AngularFactor;

        Weapon.OrbitalKnockback(appliedAngular);
        pCharacterHit.Weapon.OrbitalKnockback(appliedAngularB);

        cumulVelocity = _v1;
        pCharacterHit.cumulVelocity = _v2;
    }

    private void bodyHit(Character pCharacterHit) { }

    private Vector2 getHitMomentum() {
        return cumulVelocity;
    }
}

public enum ActionInput
{
    Press,
    Hold
}

public enum ActionType
{
    None,
    Jab,
    Thrust,
    Swipe,
    Swing,
    Parry,
    Block,
    Stride,
    Dodge
}