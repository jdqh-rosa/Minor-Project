using System;
using System.Collections.Generic;
using System.Net;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = System.Object;

public interface IStrategy
{
    Node.NodeStatus Process();
    void Reset() { }
}

public class ConditionStrategy : IStrategy
{
    readonly Func<bool> condition;

    public ConditionStrategy(Func<bool> condition) => this.condition = condition;

    public Node.NodeStatus Process() => condition() ? Node.NodeStatus.Success : Node.NodeStatus.Failure;
}

public class ActionStrategy : IStrategy
{
    readonly Action action;

    public ActionStrategy(Action action) => this.action = action;

    public Node.NodeStatus Process() {
        action();
        return Node.NodeStatus.Success;
    }
}

public class AlignAttackStrategy : IStrategy {
    private EnemyBlackboard blackboard;
    private EnemyController agent;
    public AlignAttackStrategy(EnemyBlackboard pBlackboard) {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController _agent);
        agent = _agent;
    }

    public Node.NodeStatus Process()
    {
        blackboard.TryGetValue(CommonKeys.RotationSpeed, out float rotationSpeed);
        blackboard.TryGetValue(CommonKeys.AttackTolerance, out float tolerance);
        blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject target);
        blackboard.TryGetValue(CommonKeys.ChosenAttack, out ActionType attackType);
        blackboard.TryGetValue(CommonKeys.AttackActions, out Dictionary<ActionType, CombatStateData> actions);
        
        //if (blackboard.AttackFeasible(target, attackType))
        //    return Node.NodeStatus.Success;

        Vector3 toTarget = target.transform.position - agent.transform.position;

        float desiredAngle =
            RadialHelper.CartesianToPol(new Vector2(toTarget.x, toTarget.z)).y;

        float maxAngle = actions[attackType].IdealAttackAngle;

        float clampedDesired =
            Mathf.Clamp(
                Mathf.DeltaAngle(agent.GetWeaponAngle(), desiredAngle),
                -maxAngle,
                maxAngle
            ) + agent.GetWeaponAngle();

        float newAngle =
            Mathf.MoveTowardsAngle(agent.GetWeaponAngle(), clampedDesired, rotationSpeed);

        blackboard.SetKeyValue(
            CommonKeys.ChosenWeaponAngle,
            RadialHelper.NormalizeAngle(newAngle)
        );

        Vector3 weaponTip =
            agent.transform.position +
            MiscHelper.Vec2ToVec3Pos(
                RadialHelper.PolarToCart(newAngle, agent.GetWeaponRange())
            );

        Vector3 positionalError = target.transform.position - weaponTip;

        if (positionalError.magnitude <= tolerance)
            return Node.NodeStatus.Success;

        blackboard.AddForce(
            positionalError.normalized,
            agent.TreeValues.Movement.AlignAttackForce,
            "AlignForAttack"
        );

        return Node.NodeStatus.Running;
    }

}

public class AngleWeaponAtTargetStrategy : IStrategy {
    private EnemyBlackboard blackboard;
    private EnemyController agent;
    private CommonKeys targetKey;
    private float targetAngle;
    public AngleWeaponAtTargetStrategy(EnemyBlackboard pBlackboard, CommonKeys pTargetKey) {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        targetKey = pTargetKey;
    }
    
    public AngleWeaponAtTargetStrategy(EnemyBlackboard pBlackboard, float pTargetAngle) {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        targetAngle = pTargetAngle;
    }

    public Node.NodeStatus Process() {
        float _targetAngle = GetTargetAngle();
        
        blackboard.SetKeyValue(CommonKeys.ChosenWeaponAngle, _targetAngle);
        
        return Node.NodeStatus.Success;
    }

    private float GetTargetAngle() {
        bool tryGet = blackboard.TryGetValue(targetKey, out GameObject target);
        bool isNull = target == null;
        bool useAngle = !tryGet || isNull;
        if(useAngle) return targetAngle;
        
        Vector2 diff = MiscHelper.Vec3ToVec2Pos(target.transform.position - agent.transform.position);
        return RadialHelper.CartesianToPol(diff).y;
    }
    
}

public class AvoidAndParryStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    private float avoidAngle = 20f;
    private float sidestepForce = 1f;

    public AvoidAndParryStrategy(EnemyBlackboard pBlackboard)
    {
        blackboard = pBlackboard;
    }

    public Node.NodeStatus Process()
    {
        if (!blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController _agent) || !_agent)
            return Node.NodeStatus.Failure;

        if (!blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _enemy) || !_enemy)
            return Node.NodeStatus.Failure;

        if (!_enemy.TryGetComponent(out Character _enemyChar)) 
            return Node.NodeStatus.Failure;

        if (!_enemyChar.Weapon || !_enemyChar.Weapon.gameObject)
            return Node.NodeStatus.Failure;

        bool _actionTaken = false;

        Vector2 _enemyToSelf = MiscHelper.Vec3ToVec2Pos(_agent.transform.position - _enemy.transform.position).normalized;
        float _selfAngle = RadialHelper.CartesianToPol(_enemyToSelf).y;
        float _weaponAngle = _enemyChar.GetWeaponAngle();

        float _delta = Mathf.Abs(Mathf.DeltaAngle(_selfAngle, _weaponAngle));
        if (_delta <= avoidAngle)
        {
            Vector2 _perpA = Vector2.Perpendicular(_enemyToSelf);
            Vector2 _escapeDir = Mathf.Abs(Mathf.DeltaAngle(_weaponAngle, RadialHelper.CartesianToPol(_perpA).y)) > Mathf.Abs(Mathf.DeltaAngle(_weaponAngle, RadialHelper.CartesianToPol(-_perpA).y)) ? _perpA : -_perpA;

            blackboard.AddForce(MiscHelper.Vec2ToVec3Pos(_escapeDir), sidestepForce * _agent.TreeValues.Movement.AvoidObjectForce, "Avoid_Weapon_Sidestep");
            _actionTaken = true;
        }

        Vector3 _attackTargetPos = _enemy.transform.position;
        Vector3 _diffVec = _attackTargetPos - _agent.transform.position;
        float _attackAngle = RadialHelper.CartesianToPol(new Vector2(_diffVec.x, _diffVec.z)).y;

        float _angleToWeapon = Mathf.DeltaAngle(_enemyChar.GetWeaponAngle(), _attackAngle);
        float _parryThreshold = _enemyChar.Weapon.OrbitalVelocity / 2f;

        if (Mathf.Abs(_angleToWeapon) <= _parryThreshold)
        {
            _agent.InitiateAttackAction(ActionType.Parry, _attackAngle);
            _actionTaken = true;
        }

        return _actionTaken ? Node.NodeStatus.Success : Node.NodeStatus.Failure;
    }
}

public class CheckMessageStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    private MessageType type;

    public CheckMessageStrategy(EnemyBlackboard pBlackboard, MessageType pType) {
        blackboard = pBlackboard;
        type = pType;
    }

    public Node.NodeStatus Process() {
        blackboard.TryGetValue(CommonKeys.MessageInbox, out List<ComMessage> _inbox);
        foreach (ComMessage msg in _inbox) {
            if (msg == null) continue;
            if (msg.Type == type) return Node.NodeStatus.Success;
        }
        //Debug.Log($"Check Message: {type} Failed");
        return Node.NodeStatus.Failure;
    }
}

//depr::See getclosestally
public class ChooseAllyStrategy : ChooseObjectStrategy
{
    private EnemyBlackboard blackboard;

    public ChooseAllyStrategy(EnemyBlackboard pBlackboard) : base(pBlackboard,
        pBlackboard.GetOrRegisterKey(CommonKeys.VisibleAllies), pBlackboard.GetOrRegisterKey(CommonKeys.TargetAlly)) {
        blackboard = pBlackboard;
    }

    public override Node.NodeStatus Process() {
        var status = base.Process();
        blackboard.SetKeyValue(CommonKeys.ActiveTarget, TargetType.Ally);
        return status;
    }
}

//depr::See getclosestenemy
public class ChooseEnemyStrategy : ChooseObjectStrategy
{
    EnemyBlackboard blackboard;

    public ChooseEnemyStrategy(EnemyBlackboard pBlackboard) : base(pBlackboard,
        pBlackboard.GetOrRegisterKey(CommonKeys.VisibleEnemies), pBlackboard.GetOrRegisterKey(CommonKeys.TargetEnemy)) {
        blackboard = pBlackboard;
    }

    public override Node.NodeStatus Process() {
        var status = base.Process();
        blackboard.SetKeyValue(CommonKeys.ActiveTarget, TargetType.Ally);
        return status;
    }
}

public class ChooseObjectStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    private EnemyController agent;
    private BlackboardKey listKey;
    private BlackboardKey targetKey;

    public ChooseObjectStrategy(EnemyBlackboard pBlackboard, BlackboardKey pListKey, BlackboardKey ptargetKey) {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        listKey = pListKey;
        targetKey = ptargetKey;
    }

    public virtual Node.NodeStatus Process() {
        if (!blackboard.TryGetValue(listKey, out List<GameObject> targets)) return Node.NodeStatus.Failure;
        //todo: use logic to choose most applicable target
        blackboard.SetValue(targetKey, targets[0]);
        blackboard.SetKeyValue(CommonKeys.TargetPosition, targets[0].transform.position);
        blackboard.AddForce(targets[0].transform.position - agent.transform.position, agent.TreeValues.Movement.ChooseObjectForce, "Choose_TargetObject");

        return Node.NodeStatus.Success;
    }
}

public class ContactAlliesStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    private EnemyController agent;
    private ComMessage message;

    public ContactAlliesStrategy(EnemyBlackboard pBlackboard, ComMessage pMessage) {
        blackboard = pBlackboard;
        message = pMessage;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
    }

    public Node.NodeStatus Process() {
        blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> _allies);

        foreach (var _ally in _allies) {
            if (!_ally.TryGetComponent(out EnemyController _allyAgent)) continue;
            agent.SendComMessage(_allyAgent, message);
        }

        return Node.NodeStatus.Success;
    }
}

public class DetectAttackStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    private EnemyController agent;

    public DetectAttackStrategy(EnemyBlackboard pBlackboard) {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController _agent);
        agent = _agent;
    }

    public Node.NodeStatus Process() {
        blackboard.TryGetValue(CommonKeys.VisibleEnemies, out List<GameObject> enemies);

        List<Character> _tryHitters = new List<Character>();

        foreach (var _enemy in enemies) {
            if (!_enemy || !_enemy.TryGetComponent(out Character _enemyChar)) continue;
            if (!_enemyChar.IsAttacking()) continue;

            Vector2 _enemyPos2D = MiscHelper.Vec3ToVec2Pos(_enemyChar.transform.position);
            Vector2 _agentPos2D = MiscHelper.Vec3ToVec2Pos(agent.transform.position);

            float _weaponAngle = _enemyChar.GetWeaponAngle();
            float _orbitalVelocity = _enemyChar.Weapon.OrbitalVelocity; 
            float _weaponDistance = _enemyChar.Weapon.GetCurrentReach();

            float _sweepRadians = Mathf.Abs(_orbitalVelocity * Mathf.Deg2Rad * Time.fixedDeltaTime);
            float _attackWidth = _sweepRadians * _weaponDistance;

            Vector2 _enemyToAgent = (_agentPos2D - _enemyPos2D).normalized;
            float _lateralDist = Mathf.Abs(Vector2.Dot(_enemyToAgent, Vector2.Perpendicular(RadialHelper.PolarToCart(_weaponAngle, 1f))) * (_agentPos2D - _enemyPos2D).magnitude);

            if (_lateralDist <= _attackWidth && Vector2.Distance(_agentPos2D, _enemyPos2D) <= _weaponDistance) _tryHitters.Add(_enemyChar);
        }
        
        if (_tryHitters.Count == 0)
        {
            agent.TreeValues.CombatTactic.IsDefendSelfModified = false;
            blackboard.SetKeyValue(CommonKeys.ChosenAction, ActionType.None);
            return Node.NodeStatus.Failure;
        }
        
        Character _closest = FindClosestCharacter(_tryHitters);
        blackboard.SetKeyValue(CommonKeys.TargetEnemy, _closest.gameObject);
        agent.TreeValues.CombatTactic.IsDefendSelfModified = true;
        
        if (_tryHitters.Count >= 2)
        {
            blackboard.SetKeyValue(CommonKeys.ChosenAction, ActionType.Dodge);
        }

        return Node.NodeStatus.Success;
    }

    private Character FindClosestCharacter(List<Character> characters) {
        //stolen from getclosestcharacter
        Character closestCharacter = null;
        float closestCharDistance = float.MaxValue;
        foreach (Character character in characters) {
            float charDistance = Mathf.Abs((agent.transform.position - character.transform.position).magnitude);
            if (!(charDistance < closestCharDistance)) continue;
            closestCharacter = character;
            closestCharDistance = charDistance;
        }

        return closestCharacter;
    }
}

public class DistanceSelfFromObjectStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    protected GameObject avoidObject;
    protected float minDistance;

    public DistanceSelfFromObjectStrategy(EnemyBlackboard pBlackboard, GameObject pAvoidObject, float pMinDistance) {
        blackboard = pBlackboard;
        avoidObject = pAvoidObject;
        minDistance = pMinDistance;
    }

    public Node.NodeStatus Process() {
        if (!avoidObject) return Node.NodeStatus.Failure;
        if (minDistance <= 0) return Node.NodeStatus.Failure;

        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController agent);
        Vector2 diffVec = MiscHelper.Vec3ToVec2Pos(avoidObject.transform.position - agent.transform.position);
        if (diffVec.magnitude > minDistance) return Node.NodeStatus.Success;
        blackboard.AddForce(MiscHelper.Vec2ToVec3Pos(diffVec), agent.TreeValues.Movement.AvoidObjectForce, "DistanceSelf_Object");
        return Node.NodeStatus.Success;
    }
}

public class DistanceSelfFromTargetWeaponStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    private float avoidAngle = 20;
    public DistanceSelfFromTargetWeaponStrategy(EnemyBlackboard pBlackboard) {
        blackboard = pBlackboard;
    }

    public Node.NodeStatus Process() {
        if (!blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject enemy) || !enemy) return Node.NodeStatus.Failure;

        if (!enemy.TryGetComponent(out Character character)) return Node.NodeStatus.Failure;
        
        if (!character.Weapon || !character.Weapon.gameObject) return Node.NodeStatus.Failure;

        GameObject weapon = character.Weapon.gameObject;

        DistanceSelfFromObjectStrategy strategy = new DistanceSelfFromObjectStrategy(blackboard, weapon, character.GetWeaponRange());
        strategy.Process();

        AngleAway(enemy, character);
        
        return Node.NodeStatus.Success;
    }

    private void AngleAway(GameObject pEnemy, Character pECharacter) {
        if(!blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController _agent) || !_agent) return;
        Vector2 enemyToSelf = MiscHelper.Vec3ToVec2Pos(_agent.transform.position - pEnemy.transform.position).normalized;
        
        float weaponAngle = pECharacter.GetWeaponAngle();
        float selfAngle = RadialHelper.CartesianToPol(enemyToSelf).y;
        
        float delta = Mathf.Abs(Mathf.DeltaAngle(weaponAngle, selfAngle));
        if (delta > avoidAngle)
            return;

        Vector2 perpA = Vector2.Perpendicular(enemyToSelf);
        
        float deltaA = Mathf.Abs(Mathf.DeltaAngle(weaponAngle, RadialHelper.CartesianToPol(perpA).y));
        float deltaB = Mathf.Abs(Mathf.DeltaAngle(weaponAngle, RadialHelper.CartesianToPol(-perpA).y));
        
        Vector2 weaponForward = RadialHelper.PolarToCart(weaponAngle, 1f).normalized;
        float side = Mathf.Sign(Vector3.Cross(weaponForward, enemyToSelf).z);

        Vector2 escapeDir = side > 0
            ? Vector2.Perpendicular(enemyToSelf)
            : -Vector2.Perpendicular(enemyToSelf);
        //Vector2 escapeDir = deltaA > deltaB ? perpA : -perpA;
        
        if (Mathf.Abs(side) < 0.001f)
        {
            escapeDir = Vector2.Perpendicular(enemyToSelf);
        }
        
        blackboard.AddForce(MiscHelper.Vec2ToVec3Pos(escapeDir), _agent.TreeValues.Movement.AvoidObjectForce+2, "DistanceSelf_Weapon_Turn");
    }
}

public class DistanceSelfFromWeaponsStrategy : IStrategy
{
    private EnemyBlackboard blackboard;

    public DistanceSelfFromWeaponsStrategy(EnemyBlackboard pBlackboard) {
        blackboard = pBlackboard;
    }

    public virtual Node.NodeStatus Process() {
        if(!blackboard.TryGetValue(CommonKeys.VisibleWeapons, out List<CharacterWeapon> _weapons) || _weapons == null) return Node.NodeStatus.Failure;
        
        foreach (CharacterWeapon _weapon in _weapons) {
            if(!_weapon || !_weapon.gameObject) continue;
            DistanceSelfFromObjectStrategy strategy = new DistanceSelfFromObjectStrategy(blackboard, _weapon.gameObject, _weapon.GetMaxReach());
            strategy.Process();
        }
        return Node.NodeStatus.Success;
    }
}

public class GetClosestAllyTypeStrategy : GetClosestAllyStrategy
{
    private EnemyBlackboard blackboard;
    private EnemyController agent;
    private UnitType unitType;

    public GetClosestAllyTypeStrategy(EnemyBlackboard pBlackboard, UnitType pUnitType) : base(pBlackboard) {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        unitType = pUnitType;
    }

    public virtual Node.NodeStatus Process() {
        base.Process();
        if(!blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> allies) || allies == null) return Node.NodeStatus.Failure;

        float closest = float.MaxValue;
        GameObject closestAlly = null;
        foreach (GameObject ally in allies) {
            if(!ally || !ally.TryGetComponent(out Character allyChar)) continue;
            if (allyChar.GetCharacterInfo().UnitType != unitType) continue;

            float allyDelta = (ally.transform.position - agent.transform.position).magnitude;

            if (!(allyDelta > closest)) continue;
            closest = allyDelta;
            closestAlly = ally;

        }

        if (!closestAlly) return Node.NodeStatus.Failure;
        blackboard.SetKeyValue(CommonKeys.TargetAlly, closestAlly);
        return Node.NodeStatus.Success;
    }
}

public class MovementActionStrategy : IStrategy
{
    EnemyBlackboard blackboard;
    private EnemyController agent;
    private Func<Vector2> moveDirectionProvider;
    private float moveRange;

    public MovementActionStrategy(EnemyBlackboard pBlackboard, Func<Vector2> pMoveDirectionProvider, float pMoveRange) {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        moveDirectionProvider = pMoveDirectionProvider;
        moveRange = pMoveRange;
    }

    public Node.NodeStatus Process() {
        
        if(!blackboard.TryGetValue(CommonKeys.MovementActions, out Dictionary<ActionType, CombatStateData> movementActions) || movementActions.Count == 0) return Node.NodeStatus.Failure;

        Vector2 moveDirection = moveDirectionProvider.Invoke();
        if (moveDirection == Vector2.zero) return Node.NodeStatus.Failure;
        
        ActionType _chosenAction = ActionType.None;
        float _furthestDistance = 0;
            
        foreach (var moveAction in movementActions) {
            float moveDistanceThisTick = moveAction.Value.AttackRange * (Time.deltaTime / moveAction.Value.Duration);

            if (moveDistanceThisTick > moveRange)
                continue;

            if (_chosenAction != ActionType.None && !(_furthestDistance < moveDistanceThisTick)) 
                continue;
            
            _chosenAction = moveAction.Key;
            _furthestDistance = moveDistanceThisTick;
            //Debug.Log($"moveAction: {moveAction.Key}, furthestDistance: {_furthestDistance}, moveRange: {moveRange}");
        }
        
        if (_chosenAction == ActionType.None) return Node.NodeStatus.Failure;
        
        agent.ChooseMovementAction(_chosenAction, moveDirection.normalized);
        return Node.NodeStatus.Success;
    }
}

public class DodgeStrategy : IStrategy
{
    EnemyBlackboard blackboard;
    private EnemyController agent;

    public DodgeStrategy(EnemyBlackboard pBlackboard) {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
    }

    public Node.NodeStatus Process() {
        blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject enemy);
        Vector3 _revDiffVec = agent.transform.position - enemy.transform.position;

        agent.ChooseMovementAction(ActionType.Dodge, _revDiffVec.normalized);
        return Node.NodeStatus.Success;
    }
}

public class FindAlliesStrategy : FindCharactersStrategy
{

    public FindAlliesStrategy(EnemyBlackboard pBlackboard) : base(pBlackboard, pBlackboard.GetOrRegisterKey(CommonKeys.VisibleAllies), CharacterTeam.TeamSelf) {
    }
}

public class FindCharactersStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    private BlackboardKey listKey;
    private EnemyController agent;
    private float findRadius;
    private CharacterTeam team;
    private bool exclude;

    protected FindCharactersStrategy(EnemyBlackboard pBlackboard, BlackboardKey pListKey, CharacterTeam pCharacterTeam = CharacterTeam.TeamSelf, bool pExcludeTeam = false) {
        blackboard = pBlackboard;
        listKey = pListKey;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        blackboard.TryGetValue(CommonKeys.FindRadius, out findRadius);
        //blackboard.TryGetValue(CommonKeys.TeamSelf, out team);
        if (pCharacterTeam == CharacterTeam.TeamSelf) {
            blackboard.TryGetValue(CommonKeys.TeamSelf, out team);
        }
        else {
            team = pCharacterTeam;
        }
        exclude = pExcludeTeam;
    }

    public virtual Node.NodeStatus Process() {
        Collider[] _hitColliders = Physics.OverlapSphere(agent.transform.position, findRadius);
        List<GameObject> _targetsFound = new();
        List<CharacterWeapon> _weaponsList = new();
        
        foreach (var hitCollider in _hitColliders) {
            GameObject _hitGO = hitCollider.gameObject;
            if (!_hitGO.CompareTag("Character") || _hitGO == agent.transform.gameObject) continue;
            if (!_hitGO.TryGetComponent(out Character _character)) continue;
            
            _weaponsList.Add(_character.Weapon);
            
            CharacterTeam _otherTeam = _character.GetCharacterInfo().Team;
            
            if (!exclude) {
                if (_otherTeam == team) {
                    _targetsFound.Add(_hitGO);
                }
                continue;
            }

            if (team == CharacterTeam.Any || (team != CharacterTeam.Neutral && _otherTeam != CharacterTeam.Neutral) && _otherTeam != team) {
                _targetsFound.Add(_hitGO);
            }
        }
        
        blackboard.SetKeyValue(CommonKeys.VisibleWeapons, _weaponsList);
        blackboard.SetValue(listKey, _targetsFound);
        return _targetsFound.Count == 0 ? Node.NodeStatus.Failure : Node.NodeStatus.Success;
    }
}

public class FindEnemiesStrategy : FindCharactersStrategy
{
    public FindEnemiesStrategy(EnemyBlackboard pBlackboard) : base(pBlackboard, pBlackboard.GetOrRegisterKey(CommonKeys.VisibleEnemies), CharacterTeam.TeamSelf, true) {
    }
}

public class FindObjectsStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    private BlackboardKey listKey;
    private Transform agentTransform;
    private float findRadius;
    private int findLayer;

    public FindObjectsStrategy(EnemyBlackboard pBlackboard, BlackboardKey pListKey,
        int pFindLayer) {
        blackboard = pBlackboard;
        listKey = pListKey;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController _agent);
        agentTransform = _agent.transform;
        blackboard.TryGetValue(CommonKeys.FindRadius, out findRadius);
        findLayer = pFindLayer;
    }

    public Node.NodeStatus Process() {
        Collider[] _hitColliders = Physics.OverlapSphere(agentTransform.position, findRadius, ~findLayer);
        List<GameObject> _targetsFound = new();

        foreach (var hitCollider in _hitColliders) {
            if (hitCollider.gameObject.layer == findLayer && hitCollider.gameObject != agentTransform.gameObject) {
                _targetsFound.Add(hitCollider.gameObject);
            }
        }

        blackboard.SetValue(listKey, _targetsFound);

        return _targetsFound.Count == 0 ? Node.NodeStatus.Failure : Node.NodeStatus.Success;
    }
}

public class FlankStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    private EnemyController agent;
    private Vector3? lastFlankPosition;
    private float stuckTimer = 0f;
    private float maxStuckTime = 3f;

    public FlankStrategy(EnemyBlackboard pBlackboard) {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
    }

    public Node.NodeStatus Process() {
        blackboard.TryGetValue(CommonKeys.FlankTarget, out GameObject _enemyTarget);
        if (_enemyTarget == null) {
            endFlank();
            return Node.NodeStatus.Failure;
        }

        if (!agent) return Node.NodeStatus.Failure;

        Vector3 enemyPosition = _enemyTarget.transform.position;

        if (!blackboard.TryGetValue(CommonKeys.FlankAlly, out GameObject ally)) {
            endFlank();
            Debug.Log($"Flank Failed");
            return Node.NodeStatus.Failure;
        }

        if (!blackboard.TryGetValue(CommonKeys.FlankDirection, out Vector3 flankDirection)) {
            Vector3 allyPos = ally.transform.position;
            flankDirection = (enemyPosition - allyPos).normalized;
        }

        float flankDistance = 5f;
        Vector3 flankPosition = enemyPosition + flankDirection.normalized * flankDistance;


        if (lastFlankPosition.HasValue && Vector3.Distance(flankPosition, lastFlankPosition.Value) < 0.1f) {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= maxStuckTime) {
                return HandleFallback(agent, enemyPosition);
            }
        }
        else {
            stuckTimer = 0f;
        }

        lastFlankPosition = flankPosition;
        blackboard.SetKeyValue(CommonKeys.TargetPosition, flankPosition);
        blackboard.AddForce(flankPosition - agent.transform.position, agent.TreeValues.Movement.FlankForce, "Flank");

        if (Vector3.Distance(agent.transform.position, flankPosition) < 1f) {
            endFlank();
            return Node.NodeStatus.Success;
        }

        return Node.NodeStatus.Running;
    }

    private void endFlank() {
        blackboard.SetKeyValue<float?>(CommonKeys.FlankDirection, null);
        blackboard.SetKeyValue<GameObject>(CommonKeys.FlankTarget, null);
        agent.TreeValues.CombatTactic.IsFlankModified = false;
    }

    private Node.NodeStatus HandleFallback(EnemyController agent, Vector3 enemyPosition) {
        Vector3 fallbackPosition = enemyPosition - agent.transform.forward * 3f;
        blackboard.SetKeyValue(CommonKeys.TargetPosition, fallbackPosition);
        blackboard.AddForce(fallbackPosition - agent.transform.position, agent.TreeValues.Movement.FlankForce, "Flank_Fallback");
        
        endFlank();
        Debug.Log($"Flank Failed Fallback");
        return Node.NodeStatus.Failure;
    }
}

public class GetClosestAllyStrategy : GetClosestCharacterStrategy
{
    EnemyBlackboard blackboard;

    public GetClosestAllyStrategy(EnemyBlackboard pBlackboard) : base(pBlackboard, pBlackboard.GetOrRegisterKey(CommonKeys.TargetAlly), pBlackboard.GetOrRegisterKey(CommonKeys.VisibleAllies),
        pBlackboard.GetOrRegisterKey(CommonKeys.KnownAllies)) {
        blackboard = pBlackboard;
    }

    public override Node.NodeStatus Process() {
        var status = base.Process();
        blackboard.SetKeyValue(CommonKeys.ActiveTarget, TargetType.Ally);
        return status;
    }
}

public class GetClosestCharacterStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    private EnemyController agent;
    private BlackboardKey visibleKey;
    private BlackboardKey knownKey;
    private BlackboardKey targetKey;

    public GetClosestCharacterStrategy(EnemyBlackboard pBlackboard, BlackboardKey pTargetKey, BlackboardKey pVisibleKey, BlackboardKey pKnownKey = default) {
        blackboard = pBlackboard;
        visibleKey = pVisibleKey;
        knownKey = pKnownKey;
        targetKey = pTargetKey;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController _agent);
        agent = _agent;
    }

    public virtual Node.NodeStatus Process() {
        if (blackboard.TryGetValue(visibleKey, out List<GameObject> characters)) {
            FindClosestCharacter(characters);
            return Node.NodeStatus.Success;
        }

        if (!blackboard.TryGetValue(knownKey, out List<GameObject> knownCharacters)) return Node.NodeStatus.Failure;
        FindClosestCharacter(knownCharacters);
        
        return Node.NodeStatus.Success;
    }

    private void FindClosestCharacter(List<GameObject> characters) {
        if(characters.Count == 0) return;
        GameObject closestCharacter = null;
        float closestCharDistance = float.MaxValue;
        foreach (GameObject character in characters) {
            if(!character) continue;
            float charDistance = Mathf.Abs((agent.transform.position - character.transform.position).magnitude);
            if (!(charDistance < closestCharDistance)) continue;
            closestCharacter = character;
            closestCharDistance = charDistance;
        }
        if(closestCharacter == null) return;
        blackboard.SetValue(targetKey, closestCharacter);
        blackboard.SetKeyValue(CommonKeys.TargetPosition, closestCharacter.transform.position);
        //blackboard.AddForce(closestCharacter.transform.position, 0.1f, "Closest_Character");
    }
}

public class GetClosestEnemyStrategy : GetClosestCharacterStrategy
{
    private EnemyBlackboard blackboard;
    public GetClosestEnemyStrategy(EnemyBlackboard pBlackboard) : base(pBlackboard, pBlackboard.GetOrRegisterKey(CommonKeys.TargetEnemy), pBlackboard.GetOrRegisterKey(CommonKeys.VisibleEnemies), pBlackboard.GetOrRegisterKey(CommonKeys.KnownEnemies)) {
        blackboard = pBlackboard;
    }
    
    public override Node.NodeStatus Process() {
        var status = base.Process();
        blackboard.SetKeyValue(CommonKeys.ActiveTarget, TargetType.Enemy);
        return status;
    }
}

public class GetLowestAllyStrategy : IStrategy
{
    EnemyBlackboard blackboard;

    public GetLowestAllyStrategy(EnemyBlackboard pBlackboard) {
        blackboard = pBlackboard;
    }

    public Node.NodeStatus Process() {
        
        if(!blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> allies)) return Node.NodeStatus.Failure;

        GameObject _lowestAlly = null;
        float _lowestHealth = float.MaxValue;
        foreach (GameObject ally in allies) {
            if(!ally) continue;
            if (!ally.TryGetComponent(out Character _allyChar)) continue;
            if(_allyChar.GetCharacterInfo().Health > _lowestHealth) continue;
            _lowestAlly = ally;
        }
        
        blackboard.SetKeyValue(CommonKeys.LowestHealthAlly, _lowestAlly);
        return Node.NodeStatus.Success;
    }
}

public class GroupUpStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    private EnemyController agent;
    private float radius = 10.0f;
    private float arrivalThreshold = 5.0f;

    public GroupUpStrategy(EnemyBlackboard pBlackboard) {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController _agent);
        agent = _agent;
    }

    public Node.NodeStatus Process() {
        if (!blackboard.TryGetValue(CommonKeys.GroupUpPosition, out Vector3 centerPosition)) {
            agent.TreeValues.Decider.IsAssembleModified = false;
            return Node.NodeStatus.Failure;
        }

        if (!blackboard.TryGetValue(CommonKeys.GroupUpAllies, out List<GameObject> allies)) {
            agent.TreeValues.Decider.IsAssembleModified = false;
            return Node.NodeStatus.Failure;
        }

        int index = allies.IndexOf(agent.gameObject);
        if (index == -1) {
            agent.TreeValues.Decider.IsAssembleModified = false;
            return Node.NodeStatus.Failure;
        }

        Vector3 targetPosition = GetGridPosition(centerPosition, index, allies.Count, radius);
        float distance = Vector3.Distance(agent.transform.position, targetPosition);

        if (distance <= arrivalThreshold) {
            agent.TreeValues.Decider.IsAssembleModified = false;
            return Node.NodeStatus.Success;
        }

        blackboard.SetKeyValue(CommonKeys.TargetPosition, targetPosition);
        blackboard.AddForce(targetPosition - agent.transform.position, agent.TreeValues.Movement.GroupUpForce, "GroupUp");
        return Node.NodeStatus.Running;
    }

    private Vector3 GetCirclePosition(Vector3 pCenter, int pIndex, int pTotal, float pRadius) {
        float _angle = 2 * Mathf.PI * pIndex / pTotal;
        Vector2 _pos = new Vector2(pCenter.x, pCenter.z) + RadialHelper.PolarToCart(_angle, pRadius);
        return new Vector3(_pos.x, pCenter.y, _pos.y);
    }

    private Vector3 GetGridPosition(Vector3 pCenter, int pIndex, int pTotal, float pSpacing) {
        int cols = Mathf.CeilToInt(Mathf.Sqrt(pTotal));
        int rows = Mathf.CeilToInt((float)pTotal / cols);

        int row = pIndex / cols;
        int col = pIndex % cols;

        float xOffset = (col - (cols - 1) / 2f) * pSpacing;
        float zOffset = (row - (rows - 1) / 2f) * pSpacing;

        return new Vector3(pCenter.x + xOffset, pCenter.y, pCenter.z + zOffset);
    }
}

public class LineOfSightStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    private EnemyController agent;
    private float attackRange = 0.5f;

    public LineOfSightStrategy(EnemyBlackboard pBlackboard, float pAttackRange) {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        attackRange = pAttackRange;
    }

    public Node.NodeStatus Process() {
        blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject target);
        if (target == null) return Node.NodeStatus.Failure;

        Vector3 weaponPosition = agent.gameObject.GetComponent<Character>().Weapon.GetTipPosition();

        Vector3 origin = agent.transform.position;
        Vector3 direction = (target.transform.position - origin).normalized;
        Vector3 weaponDirection = (target.transform.position - weaponPosition).normalized;
        direction.y = 0;
        weaponDirection.y = 0;
        float distance = Vector3.Distance(origin, target.transform.position);

        int mask = LayerMask.GetMask("Body");
        if (!Physics.Raycast(origin, direction, out RaycastHit bodyHit, attackRange, mask)) return Node.NodeStatus.Failure;
        //if (!Physics.Raycast(origin, weaponDirection, out RaycastHit weaponHit, attackRange, mask)) return false;

        Character hitChar = bodyHit.collider.GetComponentInParent<Character>();
        if (!hitChar) return Node.NodeStatus.Failure;
            
        //Character hitWeaponChar = weaponHit.collider.GetComponentInParent<Character>();
        //if (!hitWeaponChar) return false;

        if (!blackboard.TryGetValue(CommonKeys.TeamSelf, out CharacterTeam team)) return Node.NodeStatus.Failure;
        bool check = hitChar.GetCharacterInfo().Team != team;// && hitWeaponChar.GetCharacterInfo().Team != team;
        return Node.NodeStatus.Success;
    }
}

public class ModifyWeightStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    private EnemyController agent;
    private MessageType messageType;

    public ModifyWeightStrategy(EnemyBlackboard pBlackboard, MessageType pMessageType) {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        messageType = pMessageType;
    }

    public Node.NodeStatus Process() {
        switch (messageType) {
            case MessageType.Flank:
                agent.TreeValues.CombatTactic.IsFlankModified = true;
                break;
            case MessageType.GroupUp:
                agent.TreeValues.Decider.IsAssembleModified = true;
                break;
            case MessageType.Retreat:
                agent.TreeValues.CombatTactic.IsRetreatModified = true;
                break;
            case MessageType.SurroundTarget:
                agent.TreeValues.CombatTactic.IsSurroundModified = true;
                break;
        }
        
        return Node.NodeStatus.Success;
    }
}

public class SetTargetAllyStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    private EnemyController agent;
    private GetClosestAllyStrategy _getClosestAlly;
    private GameObject targetAlly;

    public SetTargetAllyStrategy(EnemyBlackboard pBlackboard, GameObject pAlly = null) {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        _getClosestAlly = new(blackboard);
        targetAlly = pAlly;
    }

    Node.NodeStatus IStrategy.Process() {
        if (targetAlly != null) {
            blackboard.SetKeyValue(CommonKeys.TargetAlly, targetAlly);
            blackboard.SetKeyValue(CommonKeys.ActiveTarget, TargetType.Ally);
            return Node.NodeStatus.Success;
        }

        if (_getClosestAlly.Process() == Node.NodeStatus.Success) {
            blackboard.TryGetValue(CommonKeys.TargetAlly, out GameObject target);
            if (!target) return Node.NodeStatus.Failure;
            blackboard.SetKeyValue(CommonKeys.ActiveTarget, TargetType.Ally);
            blackboard.SetKeyValue(CommonKeys.TargetPosition, target.transform.position);
            blackboard.AddForce(target.transform.position - agent.transform.position, agent.TreeValues.Movement.TargetAllyForce, "Target_Ally");
            return Node.NodeStatus.Success;
        }

        if (blackboard.TryGetValue(CommonKeys.LastAllyPosition, out Vector3 position)) {
            //todo: use last known ally position
            return Node.NodeStatus.Failure;
        }

        return Node.NodeStatus.Failure;
    }
}

public class OffensiveParryStrategy : IStrategy
{
    private EnemyBlackboard blackboard;

    private float parryThresholdAngle = 30f;

    public OffensiveParryStrategy(EnemyBlackboard pBlackboard)
    {
        blackboard = pBlackboard;
    }

    public Node.NodeStatus Process()
    {
        if (!blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _enemy) || !_enemy) return Node.NodeStatus.Failure;
        if (!blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController _agent) || !_agent) return Node.NodeStatus.Failure;
        if (!_enemy.TryGetComponent(out Character _enemyCharacter) || _enemyCharacter.Weapon == null) return Node.NodeStatus.Failure;

        Vector3 _agentPos = _agent.transform.position;
        Vector3 _enemyPos = _enemy.transform.position;
        Vector2 _agentToEnemy2D = MiscHelper.Vec3ToVec2Pos(_enemyPos - _agentPos).normalized;

        float _weaponAngle = _enemyCharacter.GetWeaponAngle();
        float _orbitalVelocity = _enemyCharacter.Weapon.OrbitalVelocity;
        float _weaponDistance = _enemyCharacter.Weapon.GetCurrentReach();
        float _sweepDegrees = Mathf.Abs(_orbitalVelocity * Time.fixedDeltaTime);

        float _agentAngle = RadialHelper.CartesianToPol(_agentToEnemy2D).y;
        float _deltaAngle = Mathf.DeltaAngle(_agentAngle, _weaponAngle);

        if (!(Mathf.Abs(_deltaAngle) <= _sweepDegrees + parryThresholdAngle)) return Node.NodeStatus.Failure;
        
        blackboard.SetKeyValue(CommonKeys.ChosenAttack, ActionType.Parry);
        blackboard.SetKeyValue(CommonKeys.TargetEnemy, _enemy);
        return Node.NodeStatus.Success;
    }
}

public class ProcessMessagesStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    private EnemyController agent;
    private int bandwidth;

    public ProcessMessagesStrategy(EnemyBlackboard pBlackboard, int pBandwidth) {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        bandwidth = pBandwidth;
    }

    public Node.NodeStatus Process() {
        processInbox();
        return Node.NodeStatus.Success;
    }

    private void processInbox() {
        blackboard.TryGetValue(CommonKeys.MessageInbox, out List<ComMessage> _messageInbox);

        int _count = Math.Min(_messageInbox.Count, bandwidth);
        List<ComMessage> _messagesToProcess = _messageInbox.GetRange(0, _count);

        foreach (ComMessage message in _messagesToProcess) {
            if(message == null) continue;
            processMessage(message);
        }

        _messageInbox.RemoveRange(0, _count);
        blackboard.SetKeyValue(CommonKeys.MessageInbox, _messageInbox);
    }

    private void processMessage(ComMessage message) {
        switch (message.Type) {
            case MessageType.Communicate:

                break;
            case MessageType.EnemySpotted:
                ProcessEnemySpotted(message);
                break;
            case MessageType.Flank:
                ProcessFlank(message);
                break;
            case MessageType.GroupUp:
                ProcessGroupUp(message);
                break;
            case MessageType.Retreat:
                ProcessRetreat(message);
                break;
            case MessageType.RequestBackup:
                ProcessRequestBackup(message);
                break;
            case MessageType.SurroundTarget:
                ProcessSurroundTarget(message);
                break;
            case MessageType.Confirm:
                ProcessConfirm(message);
                break;
            case MessageType.Decline:
                ProcessDecline(message);
                break;
        }
    }
    
    private void ProcessConfirm(ComMessage message) {
        if(!blackboard.TryGetValue(CommonKeys.PendingCoordination, out ComMessage _pending)) return;
        switch (_pending.Type) {
            case MessageType.Flank:
                ConfirmFlank(message, _pending);
                break;
            case MessageType.SurroundTarget:
                ConfirmSurround(message, _pending);
                break;
        }
    }

    private void ConfirmFlank(ComMessage message, ComMessage _pending) {
        GameObject _enemyTarget = (GameObject)_pending.Payload[MessageInfoType.Enemy];
        Vector3 _flankDirection = (Vector3)_pending.Payload[MessageInfoType.DirectionVector];
        blackboard.SetKeyValue(CommonKeys.FlankDirection, _flankDirection);
        blackboard.SetKeyValue(CommonKeys.FlankAlly, message.Sender);
        blackboard.SetKeyValue(CommonKeys.FlankTarget, _enemyTarget);
        agent.TreeValues.CombatTactic.IsFlankModified = true;
    }
    
    private void ConfirmSurround(ComMessage message, ComMessage _pending) {
        GameObject _enemyTarget = (GameObject)message.Payload[MessageInfoType.Enemy];
        float _surroundAngle = (float)message.Payload[MessageInfoType.DirectionAngle];
        float _surroundRadius = (float)message.Payload[MessageInfoType.Distance];
        
        blackboard.TryGetValue(CommonKeys.SurroundAllies, out List<GameObject> _allies);
        _allies.Add(_pending.Sender.gameObject);

        blackboard.SetKeyValue(CommonKeys.SurroundTarget, _enemyTarget);
        blackboard.SetKeyValue(CommonKeys.SurroundAllies, _allies);
        blackboard.SetKeyValue(CommonKeys.SurroundRadius, _surroundRadius);
        blackboard.SetKeyValue(CommonKeys.SurroundDirection, _surroundAngle);
        agent.TreeValues.CombatTactic.IsSurroundModified = true;
    }
    
    private void ProcessDecline(ComMessage message) {
        if(!blackboard.TryGetValue(CommonKeys.PendingCoordination, out ComMessage _pending)) return;
        switch (_pending.Type) {
            case MessageType.Flank:
                blackboard.SetKeyValue(CommonKeys.PendingCoordination, (ComMessage)null);
                break;
        }
        
    }

    private void ProcessEnemySpotted(ComMessage message) {
        GameObject _enemyTarget = (GameObject)message.Payload[MessageInfoType.Enemy];
        //todo: check if already aware of enemy, if not become aware
        blackboard.TryGetValue(CommonKeys.VisibleEnemies, out List<GameObject> _visibleEnemies);
        if (_visibleEnemies.Contains(_enemyTarget)) return;
        _visibleEnemies.Add(_enemyTarget);
        blackboard.SetKeyValue(CommonKeys.VisibleEnemies, _visibleEnemies);
    }

    private void ProcessFlank(ComMessage message) {
        if (blackboard.TryGetValue(CommonKeys.IncomingCoordination, out ComMessage pending)) {
            agent.SendComMessage(message.Sender, new ComMessage(agent, message.Sender, MessageType.Decline, null, Time.time));
        }
        
        GameObject _enemyTarget = (GameObject)message.Payload[MessageInfoType.Enemy];
        EnemyController _ally = (EnemyController)message.Payload[MessageInfoType.Ally];
        Vector3 _flankDirection = (Vector3)message.Payload[MessageInfoType.DirectionVector];

        blackboard.SetKeyValue(CommonKeys.FlankDirection, _flankDirection);
        blackboard.SetKeyValue(CommonKeys.FlankAlly, _ally);
        blackboard.SetKeyValue(CommonKeys.FlankTarget, _enemyTarget);
        agent.TreeValues.CombatTactic.IsFlankModified = true;
        
        agent.SendComMessage(message.Sender, new ComMessage(agent, message.Sender, MessageType.Confirm, null, Time.time));
        blackboard.SetKeyValue(CommonKeys.IncomingCoordination, message);
    }

    private void ProcessGroupUp(ComMessage message) {
        List<GameObject> _groupUpAllies = (List<GameObject>)message.Payload[MessageInfoType.Allies];
        Vector3 _groupUpPosition = (Vector3)message.Payload[MessageInfoType.Position];

        blackboard.SetKeyValue(CommonKeys.GroupUpAllies, _groupUpAllies);
        blackboard.SetKeyValue(CommonKeys.GroupUpPosition, _groupUpPosition);
        agent.TreeValues.Decider.IsAssembleModified = true;
    }

    private void ProcessRetreat(ComMessage message) {
        Vector3 _position = (Vector3)message.Payload[MessageInfoType.Position];
        float _retreatDistance = (float)message.Payload[MessageInfoType.Distance];

        blackboard.SetKeyValue(CommonKeys.RetreatThreatPosition, _position);
        blackboard.SetKeyValue(CommonKeys.RetreatDistance, _retreatDistance);
        agent.TreeValues.CombatTactic.IsRetreatModified = true;
    }

    private void ProcessRequestBackup(ComMessage message) {
        GameObject _enemyTarget = (GameObject)message.Payload[MessageInfoType.Enemy];
        blackboard.SetKeyValue(CommonKeys.TargetEnemy, _enemyTarget);
    }

    private void ProcessSurroundTarget(ComMessage message) {
        GameObject _enemyTarget = (GameObject)message.Payload[MessageInfoType.Enemy];
        List<GameObject> _allies = (List<GameObject>)message.Payload[MessageInfoType.Allies];
        float _surroundAngle = (float)message.Payload[MessageInfoType.DirectionAngle];
        float _surroundRadius = (float)message.Payload[MessageInfoType.Distance];

        blackboard.SetKeyValue(CommonKeys.SurroundTarget, _enemyTarget);
        blackboard.SetKeyValue(CommonKeys.SurroundAllies, _allies);
        blackboard.SetKeyValue(CommonKeys.SurroundRadius, _surroundRadius);
        blackboard.SetKeyValue(CommonKeys.SurroundDirection, _surroundAngle);
        agent.TreeValues.CombatTactic.IsSurroundModified = true;
    }
}

public class RespondToMessageStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    private MessageType type;

    public RespondToMessageStrategy(EnemyBlackboard pBlackboard, MessageType pType) {
        blackboard = pBlackboard;
        type = pType;
    }

    public Node.NodeStatus Process() {
        blackboard.TryGetValue(CommonKeys.MessageInbox, out List<ComMessage> _inbox);
        var message = _inbox.Find(x => x.Type == type);
        if (message is null) return Node.NodeStatus.Failure;


        return Node.NodeStatus.Success;
    }
}

public class RetreatFromEnemiesStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    protected EnemyController agent;
    private float retreatDistance;

    public RetreatFromEnemiesStrategy(EnemyBlackboard pBlackboard, float pRetreatDistance = 5.0f) {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        retreatDistance = pRetreatDistance;
    }

    public Node.NodeStatus Process () {
        if (!agent) return Node.NodeStatus.Failure;
        if (!blackboard.TryGetValue(CommonKeys.RetreatDistance, out float _retreatDistance))
            _retreatDistance = retreatDistance;
        if (!blackboard.TryGetValue(CommonKeys.VisibleEnemies, out List<GameObject> _enemies)) return Node.NodeStatus.Success;

        Vector3 currentPos = agent.transform.position;
        Vector3 awayDirection = Vector3.zero;
        float _closestEnemy = float.MaxValue;
        
        foreach (GameObject enemy in _enemies) {
           awayDirection += (currentPos - enemy.transform.position).normalized;
           float _distance = (enemy.transform.position - agent.transform.position).magnitude;
           if (_closestEnemy > _distance) _closestEnemy = _distance;
        }
        
        if (awayDirection == Vector3.zero) awayDirection = UnityEngine.Random.insideUnitSphere.normalized;

        Vector3 retreatTarget = currentPos + awayDirection * _retreatDistance;

        if (_closestEnemy >= _retreatDistance) {
            endRetreat();
            return Node.NodeStatus.Success;
        }

        blackboard.SetKeyValue(CommonKeys.TargetPosition, retreatTarget);
        blackboard.AddForce(awayDirection, agent.TreeValues.Movement.RetreatForce, "Retreat_Enemies");
        return Node.NodeStatus.Running;
    }

    protected void endRetreat() {
        agent.TreeValues.Messenger.IsRetreatModified = false;
        agent.TreeValues.CombatTactic.IsRetreatModified = false;
        blackboard.SetKeyValue<float?>(CommonKeys.RetreatDistance, null);
        blackboard.SetKeyValue<Vector3?>(CommonKeys.RetreatThreatPosition, null);
    }
}

public class RetreatFromTargetStrategy : RetreatFromPositionStrategy
{
    private EnemyBlackboard blackboard;
    private GameObject target;
    private Func<GameObject> targetMethod;

    public RetreatFromTargetStrategy(EnemyBlackboard pBlackboard, GameObject pTarget, float pRetreatDistance = 5.0f) : base(pBlackboard, pRetreatDistance) {
        blackboard = pBlackboard;
        target = pTarget;
    }
    
    public RetreatFromTargetStrategy(EnemyBlackboard pBlackboard, Func<GameObject> pTarget, float pRetreatDistance = 5.0f) : base(pBlackboard, pRetreatDistance) {
        blackboard = pBlackboard;
        targetMethod = pTarget;
    }

    public override Node.NodeStatus Process () {
        agent.TreeValues.Messenger.IsRetreatModified = true;
        if (!target && targetMethod == null) return Node.NodeStatus.Failure;
        target ??= targetMethod();
        blackboard.SetKeyValue(CommonKeys.RetreatThreatPosition, target.transform.position);
        return base.Process();
    }

    protected override void endRetreat() {
        agent.TreeValues.Messenger.IsRetreatModified = false;
        agent.TreeValues.CombatTactic.IsRetreatModified = false;
        base.endRetreat();
    }
}

public class RetreatFromPositionStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    protected EnemyController agent;
    private float retreatDistance;

    public RetreatFromPositionStrategy(EnemyBlackboard pBlackboard, float pRetreatDistance = 5.0f) {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        retreatDistance = pRetreatDistance;
    }

    public virtual Node.NodeStatus Process() {
        if (!agent) return Node.NodeStatus.Failure;
        if (!blackboard.TryGetValue(CommonKeys.RetreatThreatPosition, out Vector3 _threatPos))
            return Node.NodeStatus.Failure;
        if (!blackboard.TryGetValue(CommonKeys.RetreatDistance, out float _retreatDistance))
            _retreatDistance = retreatDistance;

        Vector3 currentPos = agent.transform.position;
        Vector3 awayDirection = (currentPos - _threatPos).normalized;

        if (awayDirection == Vector3.zero) awayDirection = UnityEngine.Random.insideUnitSphere.normalized;

        Vector3 retreatTarget = currentPos + awayDirection * _retreatDistance;

        float distanceFromThreat = Vector3.Distance(currentPos, _threatPos);
        if (distanceFromThreat >= _retreatDistance) {
            endRetreat();
            return Node.NodeStatus.Success;
        }

        blackboard.SetKeyValue(CommonKeys.TargetPosition, retreatTarget);
        blackboard.AddForce(_threatPos - agent.transform.position, agent.TreeValues.Movement.RetreatForce, "Retreat_Position");
        return Node.NodeStatus.Running;
    }

    protected virtual void endRetreat() {
        blackboard.SetKeyValue<float?>(CommonKeys.RetreatDistance, null);
        blackboard.SetKeyValue<Vector3?>(CommonKeys.RetreatThreatPosition, null);
    }
}

public class SendMessageToAllyStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    private ComMessage message;
    protected Func<ComMessage> messageMethod;
    protected GameObject recipient;
    protected Func<GameObject> recipientMethod;

    public SendMessageToAllyStrategy(EnemyBlackboard pBlackboard, GameObject pRecipient, ComMessage _message) {
        blackboard = pBlackboard;
        recipient = pRecipient;
        message = _message;
    }
    
    public SendMessageToAllyStrategy(EnemyBlackboard pBlackboard, Func<GameObject> pRecipient, Func<ComMessage> _message) {
        blackboard = pBlackboard;
        recipientMethod = pRecipient;
        messageMethod = _message;
    }

    public virtual Node.NodeStatus Process() {
        if(!blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController _agent) || !_agent) return Node.NodeStatus.Failure;
        if (recipient == null && recipientMethod == null) return Node.NodeStatus.Failure;
        recipient ??= recipientMethod();
        if(message == null && messageMethod == null){ return Node.NodeStatus.Failure; }
        message ??= messageMethod();
        if (message == null || !recipient || !recipient.TryGetComponent(out EnemyController _allyAgent) || !_allyAgent) return Node.NodeStatus.Failure;

        if((message.Type is MessageType.Flank or MessageType.SurroundTarget or MessageType.RequestBackup) && !_allyAgent.CanExecute(message.Type) ){ //&& _allyAgent.GetCharInfo().UnitType == UnitType.Healer){
            return Node.NodeStatus.Failure;
        }
        
        _agent.SendComMessage(_allyAgent, message);

        return Node.NodeStatus.Success;
    }

    public void Reset() {
    message = null;
    //messageMethod = null;
    recipient = null;
    //recipientMethod = null;
    }
}

public class SendMessageToAlliesStrategy : SendMessageToAllyStrategy
{
    private EnemyBlackboard blackboard;

    public SendMessageToAlliesStrategy(EnemyBlackboard pBlackboard, ComMessage _message) : base(pBlackboard, null, _message) {
        blackboard = pBlackboard;
    }
    public SendMessageToAlliesStrategy(EnemyBlackboard pBlackboard, Func<ComMessage> _message) : base(pBlackboard, null, _message) {
        blackboard = pBlackboard;
    }

    public override Node.NodeStatus Process() {
        if(!blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> _allies) || _allies.Count<=0) return Node.NodeStatus.Failure;
        if(!blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController _agent) || !_agent) return Node.NodeStatus.Failure;
        
        foreach (var _ally in _allies) {
            recipient = _ally;
            base.Process();
        }

        recipient = _agent.gameObject;
        base.Process();
        
        return Node.NodeStatus.Success;
    }
}

public class StrikeParry : IStrategy
{
    private EnemyBlackboard blackboard;

    public StrikeParry(EnemyBlackboard pBlackboard) {
        blackboard = pBlackboard;
    }

    public Node.NodeStatus Process() {
        if(!blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _target) || !_target) return Node.NodeStatus.Failure;
        if(!blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController _agent) || !_agent) return Node.NodeStatus.Failure;
        if (!_target || !_target.TryGetComponent(out Character _enemyCharacter)) return Node.NodeStatus.Failure;
        if (!_enemyCharacter.Weapon) return Node.NodeStatus.Failure;
        
        Vector2 _agentPos2D = MiscHelper.Vec3ToVec2Pos(_agent.transform.position);
        Vector2 _enemyPos2D = MiscHelper.Vec3ToVec2Pos(_target.transform.position);
        Vector2 _enemyToSelf = (_agentPos2D - _enemyPos2D).normalized;

        float _weaponAngle = _enemyCharacter.GetWeaponAngle();
        float _orbitalVelocity = _enemyCharacter.Weapon.OrbitalVelocity;
        float _weaponDistance = _enemyCharacter.GetWeaponCurrentRange();

        float _sweepDegrees = Mathf.Abs(_orbitalVelocity * Time.fixedDeltaTime);
        float _sweepWidth = _sweepDegrees * _weaponDistance;

        Vector2 _weaponDir = RadialHelper.PolarToCart(_weaponAngle, 1f).normalized;
        float _lateralDist = Mathf.Abs(Vector2.Dot(_enemyToSelf, Vector2.Perpendicular(_weaponDir)) * (_enemyPos2D - _agentPos2D).magnitude);

        if (_lateralDist > _sweepWidth)
            return Node.NodeStatus.Failure;

        Vector2 _perpA = Vector2.Perpendicular(_enemyToSelf);
        Vector2 _escapeDir = Vector2.Dot(_perpA, _agent.transform.right) > 0 ? _perpA : -_perpA;

        blackboard.AddForce(MiscHelper.Vec2ToVec3Pos(_escapeDir), _agent.TreeValues.Movement.AvoidObjectForce, "DistanceSelf_Weapon_Turn");
        return Node.NodeStatus.Success;
    }
}

public class SurroundTargetStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    private EnemyController agent;
    private float arrivalThreshold = 1f;

    public SurroundTargetStrategy(EnemyBlackboard pBlackboard) {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
    }

    public Node.NodeStatus Process() {
        if (!blackboard.TryGetValue(CommonKeys.SurroundTarget, out GameObject target)) {
            endSurround();
            return Node.NodeStatus.Failure;
        }

        if (!blackboard.TryGetValue(CommonKeys.SurroundAllies, out List<GameObject> allies)) {
            endSurround();
            return Node.NodeStatus.Failure;
        }
        if (!blackboard.TryGetValue(CommonKeys.SurroundRadius, out float radius)) radius = 5f;
        if (!agent) return Node.NodeStatus.Failure;
        if (!blackboard.TryGetValue(CommonKeys.SurroundDirection, out Vector3 direction)) direction = Vector3.forward;

        foreach (GameObject ally in allies) {
            if (!ally) {
                endSurround();
                return Node.NodeStatus.Failure;
            }
        }
        
        int index = allies.IndexOf(agent.gameObject);
        if (index == -1) {
            endSurround();
            return Node.NodeStatus.Failure;
        }

        Vector3 targetPos = target.transform.position;
        Vector3 forward = direction.normalized;

        Vector3 desiredPosition = GetCirclePosition(targetPos, index, allies.Count, radius);
        Vector3 directionToPosition = desiredPosition - agent.transform.position;
        float distance = directionToPosition.magnitude;

        if (distance <= arrivalThreshold) {
            endSurround();
            return Node.NodeStatus.Success;
        }

        blackboard.SetKeyValue(CommonKeys.TargetPosition, desiredPosition);
        blackboard.AddForce(directionToPosition, agent.TreeValues.Movement.SurroundForce, "Surround_Target");
        return Node.NodeStatus.Running;
    }

    private Vector3 GetSectorPosition(Vector3 center, int index, int total, float radius, float angleDegrees,
        Vector3 facing) {
        float halfAngleRad = Mathf.Deg2Rad * (angleDegrees / 2);
        float startAngle = -halfAngleRad;
        float angleStep = angleDegrees / (total - 1);
        float angle = startAngle + Mathf.Deg2Rad * angleStep * index;

        Quaternion rotation = Quaternion.AngleAxis(Mathf.Rad2Deg * angle, Vector3.up);
        Vector3 direction = rotation * facing.normalized;
        return center + direction * radius;
    }

    private Vector3 GetCirclePosition(Vector3 pCenter, int pIndex, int pTotal, float pRadius) {
        float _angle = 2 * Mathf.PI * ((float)pIndex / pTotal) *Mathf.Rad2Deg;
        Vector2 _pos = new Vector2(pCenter.x, pCenter.z) + RadialHelper.PolarToCart(_angle, pRadius);
        return new Vector3(_pos.x, pCenter.y, _pos.y);
    }

    private void endSurround() {
        agent.TreeValues.CombatTactic.IsSurroundModified = false;
    }
}

public class WeaponAwareCombatStrategy : IStrategy
{
    private EnemyBlackboard blackboard;

    private float baseAvoidAngle = 20f;
    private float sidestepForceMultiplier = 1f;

    public WeaponAwareCombatStrategy(EnemyBlackboard pBlackboard)
    {
        blackboard = pBlackboard;
    }

    public Node.NodeStatus Process()
    {
        if (!blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController agent) || !agent) return Node.NodeStatus.Failure;
        if (!blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject enemy) || !enemy) return Node.NodeStatus.Success;
        if (!enemy.TryGetComponent(out Character enemyChar) || !enemyChar.Weapon) return Node.NodeStatus.Failure;

        CharacterWeapon weapon = enemyChar.Weapon;

        bool actionTaken = false;

        Vector2 enemyToSelf = MiscHelper.Vec3ToVec2Pos(agent.transform.position - enemy.transform.position).normalized;
        float selfAngle = RadialHelper.CartesianToPol(enemyToSelf).y;

        float weaponAngle = enemyChar.GetWeaponAngle();
        float weaponAngularVelocity = Mathf.Abs(weapon.OrbitalVelocity * Mathf.Rad2Deg);

        float dynamicAvoidAngle = Mathf.Max(baseAvoidAngle, weaponAngularVelocity);
        float delta = Mathf.Abs(Mathf.DeltaAngle(selfAngle, weaponAngle));

        if (delta <= dynamicAvoidAngle)
        {
            
            Vector2 _perpA = Vector2.Perpendicular(enemyToSelf);
            float perpAngle = RadialHelper.CartesianToPol(_perpA).y;
            float negPerpAngle = RadialHelper.CartesianToPol(-_perpA).y;
            Vector2 _escapeDir = Mathf.Abs(Mathf.DeltaAngle(weaponAngle, perpAngle)) > Mathf.Abs(Mathf.DeltaAngle(weaponAngle, negPerpAngle)) ? _perpA : -_perpA;

            blackboard.AddForce(MiscHelper.Vec2ToVec3Pos(_escapeDir), sidestepForceMultiplier * agent.TreeValues.Movement.AvoidObjectForce, "Avoid_Weapon_Sidestep");
            actionTaken = true;
        }

        Vector3 attackTargetPos = enemy.transform.position;
        Vector3 diffVec = attackTargetPos - agent.transform.position;
        float attackAngle = RadialHelper.CartesianToPol(new Vector2(diffVec.x, diffVec.z)).y;

        float weaponAttackWidth = Mathf.Abs(weapon.OrbitalVelocity) * Mathf.Rad2Deg * Time.fixedDeltaTime;
        weaponAttackWidth = Mathf.Max(weaponAttackWidth, baseAvoidAngle);

        float angleToWeapon = Mathf.DeltaAngle(weaponAngle, attackAngle);

        if (Mathf.Abs(angleToWeapon) <= weaponAttackWidth / 2f)
        {
            agent.InitiateAttackAction(ActionType.Parry, weaponAngle);
            actionTaken = true;
        }

        if (!actionTaken && weaponAngularVelocity > 50f && delta < 90f)
        {
            blackboard.SetKeyValue(CommonKeys.ChosenAction, ActionType.Dodge);
            blackboard.SetKeyValue(CommonKeys.TargetEnemy, enemy);
            actionTaken = true;
        }

        return actionTaken ? Node.NodeStatus.Success : Node.NodeStatus.Failure;
    }
}

// public class Strategy : IStrategy
// {
//     private EnemyBlackboard blackboard;
//     
//     public Strategy(EnemyBlackboard pBlackboard)
//     {
//         blackboard = pBlackboard;
//     }
//     public Node.NodeStatus Process()
//     {
//         
//         return Node.NodeStatus.Success;
//     }
// }