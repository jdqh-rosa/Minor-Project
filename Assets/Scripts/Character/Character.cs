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

    private void Awake()
    {
        if (Weapon == null || Body == null)
        {
            Debug.LogError("Weapon or Body is not assigned.");
            return;
        }

        Weapon.Character = this;
        Body.Character = this;

        CombatSM = GetComponent<CombatSM>();
        if (!CombatSM)
        {
            CombatSM = gameObject.AddComponent<CombatSM>();
        }

        CombatSM.Character = this;
        var _idle = new IdleCombatState("Idle");
        CombatSM.AddState(_idle);
        CombatSM.AddState(new JabState("Jab"), data.JabState);
        CombatSM.AddState(new SwipeState("Swipe"), data.SwipeState);
        CombatSM.AddState(new ThrustState("Thrust"), data.ThrustState);
        CombatSM.AddState(new SwingState("Swing"), data.SwingState);
        CombatSM.InitialState = _idle;
    }

    private void Update()
    {
        isAttacking = CombatSM.GetCurrentState().actionType != ActionType.None;
    }

    public void CumulativeVelocity()
    {
        cumulVelocity = Body.Velocity + Weapon.Velocity;
    }

    public void SetLookDirection(Vector2 pLookDirection)
    {
        lookDirection = pLookDirection;
    }

    public void SetCharacterDirection(Vector3 pDir)
    {
        moveDirection = pDir;
    }

    public void SetCharacterPosition(Vector3 pPos)
    {
        movePosition = pPos;
        usePosition = true;
    }

    public void AddWeaponOrbital(float pAdditionalMomentum)
    {
        Weapon.OrbitalAccelerate(pAdditionalMomentum);
    }
    
    public void AddWeaponKnockback(float pAdditionalMomentum) {
        Weapon.OrbitalKnockback(pAdditionalMomentum);
    }

    public void RotateWeaponTowardsAngle(float pTargetAngle)
    {
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

    public void RotateWeaponWithForce(float pTargetAngle, float pForce)
    {
        float _angularDifference = Mathf.DeltaAngle(GetWeaponAngle(), pTargetAngle);
        if (Mathf.Abs(_angularDifference) < data.DeadZoneThreshold)
        {
            AddWeaponOrbital(0);
            return;
        }

        float _sign = (_angularDifference < 0) ? -1 : 1;
        AddWeaponOrbital(pForce * _sign);
    }

    void FixedUpdate()
    {
        bodyFunctions();
        weaponFunctions();

        CumulativeVelocity();
    }

    private void bodyFunctions()
    {
        if (usePosition)
        {
            moveToPoint(movePosition);
        }
        else
        {
            moveInDirection(moveDirection.normalized);
        }

        usePosition = false;
    }

    private void moveToPoint(Vector3 pPoint)
    {
        Vector3 diffVec = pPoint - transform.position;
        transform.position += Body.Move(diffVec.normalized) * Mathf.Min(diffVec.magnitude, Body.GetStepLength());
    }

    private void moveInDirection(Vector3 pDirection)
    {
        Vector3 _moveVelocity = Body.Move(pDirection);

        transform.position += _moveVelocity;
    }

    private void weaponFunctions()
    {
        Weapon.UpdatePosition();
    }

    public float GetWeaponAngle()
    {
        return Weapon.GetAngle();
    }

    public float GetWeaponRange()
    {
        return Weapon.GetRange();
    }

    public Vector3 GetWeaponPosition()
    {
        return Weapon.transform.position;
    }

    public float GetWeaponOrbital()
    {
        return Weapon.OrbitalVelocity;
    }

    public CharacterData GetData()
    {
        return data;
    }

    public bool IsAttacking()
    {
        return isAttacking;
    }

    public void Attack(ActionInput pAttackInput, float pTargetAngle)
    {
        CombatSM.Attack(pAttackInput, pTargetAngle, checkLinearAttack(pTargetAngle));
    }

    public void Attack(ActionType pAttackType, float pTargetAngle)
    {
        CombatSM.Attack(pAttackType, pTargetAngle);
    }

    private bool checkLinearAttack(float pTargetAngle)
    {
        return Mathf.Abs(Mathf.DeltaAngle(GetWeaponAngle(), pTargetAngle)) < data.LinearAttackZone;
    }

    public void Defend(ActionInput pAttackInput, float pTargetAngle)
    {
        ActionType _actionType = ActionType.None;
        if (pAttackInput == ActionInput.Press)
        {
            _actionType = ActionType.Parry;
        }
        else
        {
            _actionType = ActionType.Block;
        }

        Debug.Log($"Defend action type: {_actionType}");
    }

    public void CollisionDetected(Character pCharacterHit, bool pIsClash, float pMomentum)
    {
        if (pIsClash)
        {
            weaponHit(pCharacterHit, pMomentum);
        }
        else
        {
            bodyHit(pCharacterHit);
        }
    }

    private void weaponHit(Character pCharacterHit, float pMomentum)
    {
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

        float _m1 = Weapon.GetMass();
        float _m2 = pCharacterHit.Weapon.GetMass();

        float _newV1 = ((_m1 - _m2) / (_m1 + _m2)) * _v1 + ((2 * _m2) / (_m1 + _m2)) * _v2;
        float _newV2 = ((2 * _m1) / (_m1 + _m2)) * _v1 + ((_m2 - _m1) / (_m1 + _m2)) * _v2;

        Vector2 _newVelocity1 = cumulVelocity + (_newV1 - _v1) * _impactDirection;
        Vector2 _newVelocity2 = _otherMomentum + (_newV2 - _v2) * _impactDirection;

        _newVelocity1 *= data.CollisionElasticity;
        _newVelocity2 *= data.CollisionElasticity;

        Vector2 _relativePosition1 = (GetWeaponPosition() - pCharacterHit.GetWeaponPosition()).normalized;
        Vector2 _relativePosition2 = -_relativePosition1;


        float _sign1 = Mathf.Sign(Vector3.Cross(new Vector3(_relativePosition1.x, 0, _relativePosition1.y),
            new Vector3(_newVelocity1.x, 0, _newVelocity1.y)).y);
        float _sign2 = Mathf.Sign(Vector3.Cross(new Vector3(_relativePosition2.x, 0, _relativePosition2.y),
            new Vector3(_newVelocity2.x, 0, _newVelocity2.y)).y);

        float _angularFactor = 0.5f;
        float _angularMomentum1 = _sign1 * _newVelocity1.magnitude * _angularFactor;
        float _angularMomentum2 = _sign2 * _newVelocity2.magnitude * _angularFactor;

        AddWeaponKnockback(_angularMomentum1);
        pCharacterHit.AddWeaponKnockback(_angularMomentum2);
    }

    private void bodyHit(Character pCharacterHit)
    {
    }

    private Vector2 getHitMomentum()
    {
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
}