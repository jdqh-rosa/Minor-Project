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

    public float MoveSpeed = 5f;
    Vector2 moveDirection = Vector2.zero;
    [SerializeField] Vector2 lookDirection;

    float elasticity = 0.8f;

    private Vector2 cumulVelocity;

    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] float maxRotationSpeed = 180f;
    [SerializeField] float dampingFactor = 0.2f;
    [SerializeField] float velocityDamping = 0.9f;
    [SerializeField] float deadZoneThreshold = 5f;

    private bool isAttacking = false;
    private ActionType actionType = ActionType.None;
    [SerializeField] float linearAttackZone = 30f;

    [SerializeField] private float shortAttackDuration = 0.5f;

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
        CombatSM.AddState(new JabState("Jab"));
        CombatSM.AddState(new SwipeState("Swipe"));
        CombatSM.AddState(new ThrustState("Thrust"));
        CombatSM.AddState(new SwingState("Swing"));
        CombatSM.InitialState = _idle;

    }

    private void Update() {
        if (CombatSM.GetCurrentState().actionType != ActionType.None) {
            isAttacking = true;
        }
        else {
            isAttacking = false;
        }
    }

    public void CumulativeVelocity() {
        cumulVelocity = Body.Velocity + Weapon.Velocity;
    }

    public void SetBodyRotation(float angle) {
        Body.transform.rotation = Quaternion.Euler(0, angle, 0);
    }

    public void SetLookDirection(Vector2 pLookDirection) {
        lookDirection = pLookDirection;
    }

    public void SetCharacterPosition(Vector2 pPos) {
        moveDirection = pPos;
    }

    public void AddWeaponOrbital(float pAdditionalMomentum) {
        Weapon.OrbitalAccelerate(pAdditionalMomentum);
    }

    //depr?
    public void RotateWeaponTowardsAngle(float targetAngle, float rotationSpeed) {
        float currentAngle = Weapon.transform.rotation.eulerAngles.y;

        float angleDiff = Mathf.DeltaAngle(currentAngle, targetAngle);

        float step = rotationSpeed * Time.deltaTime;
        float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, step);

        Weapon.transform.rotation = Quaternion.Euler(0, newAngle, 0);
    }

    public void RotateWeaponTowardsAngle(float pTargetAngle) {
        if (!isAttacking) {
            float angularDifference = Mathf.DeltaAngle(GetWeaponAngle(), pTargetAngle);

            if (Mathf.Abs(angularDifference) < deadZoneThreshold) {
                AddWeaponOrbital(0);
                return;
            }

            float currentAngularVelocity = GetWeaponOrbital();

            // --- PD Controller for angular motion ---
            // kp: proportional gain (affects how strongly the error is corrected)
            // kd: derivative gain (affects how strongly current angular velocity is damped)
            float kp = rotationSpeed * dampingFactor;
            float kd = velocityDamping;
            // The control signal calculates the "torque" needed:
            float controlSignal = kp * angularDifference - kd * currentAngularVelocity;

            float newAngularVelocity = currentAngularVelocity + controlSignal * Time.deltaTime;
            newAngularVelocity = Mathf.Clamp(newAngularVelocity, -maxRotationSpeed, maxRotationSpeed);

            float addedMomentum = newAngularVelocity - currentAngularVelocity;
            AddWeaponOrbital(addedMomentum);
        }
    }

    public void RotateWeaponWithForce(float pTargetAngle, float pForce) {
        float angularDifference = Mathf.DeltaAngle(GetWeaponAngle(), pTargetAngle);

        if (Mathf.Abs(angularDifference) < deadZoneThreshold) {
            AddWeaponOrbital(0);
            return;
        }


        float sign = (angularDifference < 0) ? -1 : 1;

        AddWeaponOrbital(pForce * sign);
    }

    void FixedUpdate() {
        bodyFunctions();
        weaponFunctions();

        CumulativeVelocity();
    }

    private void bodyFunctions() {
        //Body.Move(moveDirection.normalized);
        //take movement from Body and apply it in here

        RigidBody.MovePosition(transform.position +
                               new Vector3(moveDirection.x * MoveSpeed, 0, moveDirection.y * MoveSpeed));
    }

    private void weaponFunctions() {
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
        if (pIsClash) {
            weaponHit(pCharacterHit, pMomentum);
        }
        else {
            bodyHit(pCharacterHit);
        }
    }

    private void weaponHit(Character pCharacterHit, float pMomentum) {
        Vector2 _otherMomentum = pCharacterHit.getHitMomentum();

        Vector2 _impactDirection;
        if (cumulVelocity.magnitude < 0.01f) {
            _impactDirection = _otherMomentum.normalized;
        }
        else if (_otherMomentum.magnitude < 0.01f) {
            _impactDirection = cumulVelocity.normalized;
        }
        else {
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


        float _sign1 = Mathf.Sign(Vector3.Cross(new Vector3(_relativePosition1.x, 0, _relativePosition1.y),
            new Vector3(_newVelocity1.x, 0, _newVelocity1.y)).y);
        float _sign2 = Mathf.Sign(Vector3.Cross(new Vector3(_relativePosition2.x, 0, _relativePosition2.y),
            new Vector3(_newVelocity2.x, 0, _newVelocity2.y)).y);

        float angularFactor = 0.5f;
        float _angularMomentum1 = _sign1 * _newVelocity1.magnitude * angularFactor;
        float _angularMomentum2 = _sign2 * _newVelocity2.magnitude * angularFactor;

        AddWeaponOrbital(_angularMomentum1);
        pCharacterHit.AddWeaponOrbital(_angularMomentum2);
    }

    private void bodyHit(Character pCharacterHit) { }

    private Vector2 getHitMomentum() {
        return cumulVelocity;
    }

    public void Attack(ActionInput pAttackInput, float pTargetAngle) {

        if (pAttackInput == ActionInput.Press) {
            actionType = checkLinearAttack(pTargetAngle) ? ActionType.Jab : ActionType.Swipe;
        }

        if (pAttackInput == ActionInput.Hold) {
            actionType = checkLinearAttack(pTargetAngle) ? ActionType.Thrust : ActionType.Swing;
        }

        Debug.Log($"Attack Action Type: {actionType}");

        switch (actionType) {
            case ActionType.Jab:
                CombatSM.InputState("Jab", pTargetAngle);
                break;
            case ActionType.Thrust:
                CombatSM.InputState("Thrust", pTargetAngle);
                break;
            case ActionType.Swipe:
                CombatSM.InputState("Swipe", pTargetAngle);
                break;
            case ActionType.Swing:
                CombatSM.InputState("Swing", pTargetAngle);
                break;
        }
    }

    private bool checkLinearAttack(float pTargetAngle) {
        return Mathf.Abs(Mathf.DeltaAngle(GetWeaponAngle(), pTargetAngle)) < linearAttackZone;
    }

    public void Defend(ActionInput pAttackInput, float pTargetAngle) {
        if (pAttackInput == ActionInput.Press) {
            actionType = ActionType.Parry;
        }
        else {
            actionType = ActionType.Block;
        }

        Debug.Log($"Defend action type: {actionType}");
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
}