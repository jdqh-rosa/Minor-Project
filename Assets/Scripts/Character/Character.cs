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
    [SerializeField] private UnitData unitData;
    [SerializeField] private CharacterData characterData;
    [SerializeField] private HealthBar healthBar;
    
    private CharacterInfo charInfo;

    Vector3 moveDirection = Vector3.zero;
    Vector3 movePosition = Vector3.zero;
    private bool usePosition = false;
    [SerializeField] Vector2 lookDirection;

    private Vector2 cumulVelocity;
    private bool isAttacking = false;
    
    private bool hitInvulnerable = false;
    private float hitClock = 0f;

    private void Awake() {
        if (Weapon == null || Body == null) {
            Debug.LogError("Weapon or Body is not assigned.");
            return;
        }

        charInfo = new CharacterInfo(unitData);
        
        Weapon.Character = this;
        Body.Character = this;

        CombatSM = GetComponent<CombatSM>();
        if (!CombatSM) {
            CombatSM = gameObject.AddComponent<CombatSM>();
        }

        CombatSM.Character = this;
        CombatSM.SetBufferTime(characterData.CombatBufferTime);
        CombatSM.SetWeapon(Weapon);
        var _idle = new IdleCombatState("Idle");
        CombatSM.AddState(_idle);
        CombatSM.AddState(new JabState("Jab"), Weapon.GetWeaponData().JabState);
        CombatSM.AddState(new SwipeState("Swipe"), Weapon.GetWeaponData().SwipeState);
        CombatSM.AddState(new ThrustState("Thrust"), Weapon.GetWeaponData().ThrustState);
        CombatSM.AddState(new SwingState("Swing"), Weapon.GetWeaponData().SwingState);
        CombatSM.AddState(new StrideState("Stride"), characterData.StrideState);
        CombatSM.AddState(new DodgeState("Dodge"), characterData.DodgeState);
        CombatSM.InitialState = _idle;
        
        healthBar.ChangeHealth(charInfo.Health/charInfo.MaxHealth);
    }

    private void Update() {
        if (!Weapon.Character) {
            Weapon.SetWeaponCharacter(this);
        }
        
        if (hitInvulnerable) {
            hitClock += Time.deltaTime;
            if (hitClock > characterData.HitInvulerabilityTime) {
                hitInvulnerable = false;
                hitClock = 0f;
            }
        }
        
        var actionType = CombatSM.GetCurrentState().actionType;
        isAttacking = actionType != ActionType.None && actionType != ActionType.Stride && actionType != ActionType.Dodge;
        
        if (gameObject.name == "Dummy") {
            var _player = GameObject.Find("Player");
            if(!_player) return;
            float _playerAngle = RadialHelper.CartesianToPol(MiscHelper.Vec3ToVec2Pos(_player.transform.position - transform.position)).y;
            RotateWeaponTowardsAngle(_playerAngle);
        }
    }
    
    void FixedUpdate() {
        CharacterWeapon.CollisionTracker.ClearFrameCollisions();
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
        RigidBody.Move(transform.position += pMove, transform.rotation);
        //transform.position += pMove; //todo: use rigidbody for movement
    }
    
    private void weaponFunctions() {
        Weapon.UpdatePosition();
    }

    private void CumulativeVelocity() {
        cumulVelocity = Body.Velocity + Weapon.Velocity;
    }
    
    public void AddWeaponOrbital(float pAdditionalMomentum) {
        Weapon.OrbitalAccelerate(pAdditionalMomentum);
    }

    public void RotateWeaponTowardsAngle(float pTargetAngle) {
        float _angularDifference = Mathf.DeltaAngle(GetWeaponAngle(), pTargetAngle);

        if (Mathf.Abs(_angularDifference) < characterData.DeadZoneThreshold) {
            AddWeaponOrbital(0);
            return;
        }

        float _currentAngularVelocity = GetWeaponOrbital();

        float _kp = characterData.RotationSpeed * characterData.DampingFactor;
        float _kd = characterData.VelocityDamping;
        float _controlSignal = _kp * _angularDifference - _kd * _currentAngularVelocity;

        float _newAngularVelocity = _currentAngularVelocity + _controlSignal * Time.deltaTime;
        _newAngularVelocity = Mathf.Clamp(_newAngularVelocity, -characterData.MaxRotationSpeed, characterData.MaxRotationSpeed);
        
        float addedMomentum = _newAngularVelocity - _currentAngularVelocity;
        AddWeaponOrbital(addedMomentum);
    }

    public void RotateWeaponWithForce(float pTargetAngle, float pForce) {
        float _angularDifference = Mathf.DeltaAngle(GetWeaponAngle(), pTargetAngle);
        if (Mathf.Abs(_angularDifference) < characterData.DeadZoneThreshold) {
            AddWeaponOrbital(0);
            return;
        }

        float _sign = (_angularDifference < 0) ? -1 : 1;
        AddWeaponOrbital(pForce * _sign);
    }
    
    public void Attack(ActionInput pAttackInput, float pTargetAngle) {
        CombatSM.Attack(pAttackInput, pTargetAngle, checkLinearAttack(pTargetAngle));
    }

    public void Attack(ActionType pAttackType, float pTargetAngle) {
        CombatSM.Attack(pAttackType, pTargetAngle);
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
        // Vector2 _v1 = cumulVelocity;
        // Vector2 _v2 = pMomentum;
        //
        // Vector2 _impactDirection;
        // if (_v1.sqrMagnitude < 1e-4f) _impactDirection = _v2.normalized;
        // else if (_v2.sqrMagnitude < 1e-4f) _impactDirection = _v1.normalized;
        // else _impactDirection = (_v1 - _v2).normalized;
        //
        // float u1 = Vector2.Dot(_v1, _impactDirection);
        // float u2 = Vector2.Dot(_v2, _impactDirection);
        //
        // float m1 = this.Weapon.GetMass();
        // float m2 = pCharacterHit.Weapon.GetMass();
        // float v1new = ((m1 - m2)/(m1 + m2))*u1 + (2*m2/(m1 + m2))*u2;
        // float v2new = (2*m1/(m1 + m2))*u1 + ((m2 - m1)/(m1 + m2))*u2;
        //
        // _v1 += (v1new - u1)*_impactDirection;
        // _v2 += (v2new - u2)*_impactDirection;
        //
        // _v1 *= characterData.CollisionElasticity;
        // _v2 *= characterData.CollisionElasticity;
        //
        // Vector3 gripPos = GetWeaponPosition();
        // Vector3 theirGripPos = pCharacterHit.GetWeaponPosition();
        //
        // Vector3 contactPos = pPointHit;
        // Vector3 rA = contactPos - gripPos;
        // Vector3 rB = contactPos - theirGripPos;
        //
        // Vector3 pA = new Vector3(_v1.x, 0f, _v1.y);
        // Vector3 pB = new Vector3(_v2.x, 0f, _v2.y);
        //
        // float angularImpulse = Vector3.Cross(rA, pA).y;
        // float angularImpulseB = Vector3.Cross(rB, pB).y;
        // Debug.Log($"r={rA.magnitude:F2}, p={pA.magnitude:F2}, L={angularImpulse:F2}");
        // Debug.Log($"r={rB.magnitude:F2}, p={pB.magnitude:F2}, L={angularImpulseB:F2}");
        //
        // float appliedAngular = Mathf.Clamp(angularImpulse * characterData.AngularFactor, -characterData.MaxRotationSpeed, characterData.MaxRotationSpeed);
        // float appliedAngularB = Mathf.Clamp(angularImpulseB * pCharacterHit.characterData.AngularFactor, -pCharacterHit.characterData.MaxRotationSpeed, pCharacterHit.characterData.MaxRotationSpeed);
        //
        // Weapon.OrbitalKnockback(appliedAngular);
        // pCharacterHit.Weapon.OrbitalKnockback(appliedAngularB);
        //
        // cumulVelocity = _v1;
        // pCharacterHit.cumulVelocity = _v2;
         
        
        Vector3 gripPos = GetWeaponPosition();
        Vector3 contactPoint = pPointHit;
        Vector3 r = contactPoint - gripPos;
        
        Vector3 relativeVelocity = pCharacterHit.Weapon.Velocity - Weapon.Velocity;
        Vector3 force = relativeVelocity * Weapon.GetMass();
        
        float torqueY = Vector3.Cross(r, force).y; // Only Y-axis torque matters in a flat orbital system
        float torqueFactor = 1f;
        float thrustFactor = 1f;
        
        float alongBlade = Vector3.Dot(r.normalized, Weapon.transform.forward);
        if (alongBlade > 0.8f) { // near tip
            torqueFactor *= 1.5f; // tip hits swing harder
        } else if (alongBlade < 0.2f) {
            torqueFactor *= 0.5f; // hilt hits swing softly
        }
        
        float thrustForce = Vector3.Dot(relativeVelocity, r.normalized);
        Weapon.LinearKnockback(thrustForce * thrustFactor);
        Weapon.OrbitalKnockback(Mathf.Clamp(torqueY * torqueFactor, -characterData.MaxRotationSpeed, characterData.MaxRotationSpeed));
        Debug.Log($"collision force: {torqueY * torqueFactor}", this);
    }

    private void bodyHit(Character pCharacterHit) { }

    private bool checkLinearAttack(float pTargetAngle) {
        return Mathf.Abs(Mathf.DeltaAngle(GetWeaponAngle(), pTargetAngle)) < characterData.LinearAttackZone;
    }
    
    public void SetLookDirection(Vector2 pLookDirection) {
        lookDirection = pLookDirection;
        CombatSM.SetAttackAngle(RadialHelper.CartesianToPol(lookDirection).y);
    }
    
    public void SetCharacterPosition(Vector3 pPos) {
        movePosition = pPos;
        movePosition.y = transform.position.y;
        usePosition = true;
    }

    public void SetCharacterDirection(Vector3 pDir) {
        pDir.y= 0;
        moveDirection = pDir;
    }

    public Vector3 GetCharacterDirection() {
        return moveDirection;
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

    public CharacterData GetCharacterData() {
        return characterData;
    }

    public CharacterInfo GetCharacterInfo() {
        if (charInfo == null) {
            Debug.LogWarning($"charInfo was null on {gameObject.name}, recreating from unitData...", this);
            charInfo = new CharacterInfo(unitData);
        }

        return charInfo;
    }

    public float GetCurrentHealth() {
        return GetCharacterInfo().Health;
    }

    public bool IsAttacking() {
        return isAttacking;
    }

    private Vector2 getHitMomentum() {
        return cumulVelocity;
    }
    
    public void TakeDamage(float pDamage) {
        if(hitInvulnerable) return;
        charInfo.TakeDamage(pDamage);
        healthBar.ChangeHealth(charInfo.Health/charInfo.MaxHealth);
        hitInvulnerable = true;
        if (charInfo.Health <= 0) {
            Destroy(this.gameObject);
        }
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