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

    private Vector3 moveDirection = Vector3.zero;
    private Vector3 movePosition = Vector3.zero;
    private bool usePosition = false;
    [SerializeField] private Vector2 lookDirection;

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

        healthBar.ChangeHealth(charInfo.Health / charInfo.MaxHealth);
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

        var _actionType = CombatSM.GetCurrentState().actionType;
        isAttacking = _actionType != ActionType.None && _actionType != ActionType.Stride &&
                      _actionType != ActionType.Dodge;

        if (gameObject.name == "Dummy") {
            var _player = GameObject.Find("Player");
            if (!_player) return;
            float _playerAngle = RadialHelper
                .CartesianToPol(MiscHelper.Vec3ToVec2Pos(_player.transform.position - transform.position)).y;
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
        Vector3 _diffVec = pPoint - transform.position;
        Vector3 _movementVec = Body.Step(_diffVec.normalized) * Mathf.Min(_diffVec.magnitude, Body.GetStepLength());
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

    private void addWeaponOrbital(float pAdditionalMomentum) {
        Weapon.OrbitalAccelerate(pAdditionalMomentum);
    }

    public void RotateWeaponTowardsAngle(float pTargetAngle) {
        float _angularDifference = Mathf.DeltaAngle(GetWeaponAngle(), pTargetAngle);

        if (Mathf.Abs(_angularDifference) < characterData.DeadZoneThreshold) {
            addWeaponOrbital(0);
            return;
        }

        float _currentAngularVelocity = getWeaponOrbital();

        float _kp = characterData.RotationSpeed * characterData.DampingFactor;
        float _kd = characterData.VelocityDamping;
        float _controlSignal = _kp * _angularDifference - _kd * _currentAngularVelocity;

        float _newAngularVelocity = _currentAngularVelocity + _controlSignal * Time.deltaTime;
        _newAngularVelocity = Mathf.Clamp(_newAngularVelocity, -characterData.MaxRotationSpeed, characterData.MaxRotationSpeed);

        float _addedMomentum = _newAngularVelocity - _currentAngularVelocity;
        addWeaponOrbital(_addedMomentum);
    }

    public void RotateWeaponWithForce(float pTargetAngle, float pForce) {
        float _angularDifference = Mathf.DeltaAngle(GetWeaponAngle(), pTargetAngle);
        if (Mathf.Abs(_angularDifference) < characterData.DeadZoneThreshold) {
            addWeaponOrbital(0);
            return;
        }

        float _sign = (_angularDifference < 0) ? -1 : 1;
        addWeaponOrbital(pForce * _sign);
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
        pDir.y = 0;
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

    private float getWeaponOrbital() {
        return Weapon.OrbitalVelocity;
    }

    public CharacterData GetCharacterData() {
        return characterData;
    }

    public CharacterInfo GetCharacterInfo() {
        charInfo ??= new CharacterInfo(unitData);
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
        if (hitInvulnerable) return;
        charInfo.TakeDamage(pDamage);
        healthBar.ChangeHealth(charInfo.Health / charInfo.MaxHealth);
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