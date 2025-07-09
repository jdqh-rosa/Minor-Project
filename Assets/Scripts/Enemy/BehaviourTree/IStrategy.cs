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
            if (msg.Type == type) return Node.NodeStatus.Success;
        }
        Debug.Log($"Check Message: {type} Failed");
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
            if (!_enemy || !_enemy.TryGetComponent(out Character _character)) continue;

            if (_character.IsAttacking()) {
                //todo: see if enemy attack makes contact
                Vector3 _diffVec = MiscHelper.DifferenceVector(agent.transform.position, _character.transform.position);
                Vector3 _diffFromWeaponVec = _character.transform.position + MiscHelper.Vec2ToVec3Pos(RadialHelper.PolarToCart(_character.GetWeaponAngle(), _character.GetWeaponRange())) - agent.transform.position;
                if (_diffVec.magnitude > _character.GetWeaponRange()) continue;
                if (_diffFromWeaponVec.magnitude > _character.GetWeaponRange()) continue;

                float _angle = RadialHelper.CartesianToPol(_diffVec.normalized).y;

                if (Mathf.Abs(_angle - _character.GetWeaponAngle()) > 180f) continue;

                _tryHitters.Add(_character);
            }
        }

        switch (_tryHitters.Count) {
            case 0:
                agent.TreeValues.CombatTactic.IsDefendSelfModified = false;
                blackboard.SetKeyValue(CommonKeys.ChosenAction, ActionType.None);
                break;
            case >= 2:
                agent.TreeValues.CombatTactic.IsDefendSelfModified = true;
                blackboard.SetKeyValue(CommonKeys.ChosenAction, ActionType.Dodge);
                blackboard.SetKeyValue(CommonKeys.TargetEnemy, FindClosestCharacter(_tryHitters).gameObject);
                break;
            default:
                agent.TreeValues.CombatTactic.IsDefendSelfModified = true;
                blackboard.SetKeyValue(CommonKeys.TargetEnemy, _tryHitters[0].gameObject);
                break;
        }

        return Node.NodeStatus.Success;
    }

    private Character FindClosestCharacter(List<Character> characters) {
        //stolen from getclosestcharacter
        Character closestCharacter = characters[0];
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

    public virtual Node.NodeStatus Process() {
        if (!avoidObject) return Node.NodeStatus.Failure;

        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController agent);
        Vector2 diffVec = MiscHelper.Vec2ToVec3Pos(avoidObject.transform.position - agent.transform.position);
        if (diffVec.magnitude > minDistance) return Node.NodeStatus.Success;
        blackboard.AddForce(diffVec, agent.TreeValues.Movement.AvoidObjectForce, "DistanceSelf_Object");
        return Node.NodeStatus.Success;
    }
}

public class DistanceSelfFromWeaponsStrategy : DistanceSelfFromObjectStrategy
{
    private EnemyBlackboard blackboard;

    public DistanceSelfFromWeaponsStrategy(EnemyBlackboard pBlackboard) : base(pBlackboard, null, 0.1f) {
        blackboard = pBlackboard;
    }

    public override Node.NodeStatus Process() {
        blackboard.TryGetValue(CommonKeys.VisibleWeapons, out List<CharacterWeapon> _weapons);
        
        foreach (CharacterWeapon _weapon in _weapons) {
            if(!_weapon) continue;
            avoidObject = _weapon.gameObject;
            minDistance = _weapon.GetRange();
            base.Process();
        }
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
        if (_enemyTarget == null) return Node.NodeStatus.Failure;

        if (!agent) return Node.NodeStatus.Failure;

        Vector3 enemyPosition = _enemyTarget.transform.position;

        if (!blackboard.TryGetValue(CommonKeys.FlankDirection, out Vector3 flankDirection)) {
            if (blackboard.TryGetValue(CommonKeys.FlankAlly, out GameObject ally)) {
                Vector3 allyPos = ally.transform.position;
                flankDirection = (enemyPosition - allyPos).normalized;
            }
            else {
                endFlank();
                Debug.Log($"Flank Failed");
                return Node.NodeStatus.Failure;
            }
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
        //blackboard.AddForce(closestCharacter.transform.position, 1f, "Closest_Character");
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

public class GroupUpStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    private float radius = 2.0f;
    private float arrivalThreshold = 1.0f;

    public GroupUpStrategy(EnemyBlackboard pBlackboard) {
        blackboard = pBlackboard;
    }

    public Node.NodeStatus Process() {
        if (!blackboard.TryGetValue(CommonKeys.GroupUpPosition, out Vector3 centerPosition))
            return Node.NodeStatus.Failure;
        if (!blackboard.TryGetValue(CommonKeys.GroupUpAllies, out List<GameObject> allies))
            return Node.NodeStatus.Failure;
        if (!blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController agent)) return Node.NodeStatus.Failure;

        int index = allies.IndexOf(agent.gameObject);
        if (index == -1) return Node.NodeStatus.Failure;

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
        GameObject _enemyTarget = (GameObject)message.Payload[MessageInfoType.Enemy];
        EnemyController _ally = (EnemyController)message.Payload[MessageInfoType.Ally];
        Vector3 _flankDirection = (Vector3)message.Payload[MessageInfoType.DirectionVector];

        blackboard.SetKeyValue(CommonKeys.FlankDirection, _flankDirection);
        blackboard.SetKeyValue(CommonKeys.FlankAlly, _ally);
        blackboard.SetKeyValue(CommonKeys.FlankTarget, _enemyTarget);
        agent.TreeValues.CombatTactic.IsFlankModified = true;
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
        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController _agent);
        if (recipient == null && recipientMethod == null) return Node.NodeStatus.Failure;
        recipient ??= recipientMethod();
        if(message == null && messageMethod == null){ return Node.NodeStatus.Failure; }
        message ??= messageMethod();
        if (recipient.TryGetComponent(out EnemyController _allyAgent)) {
            _agent.SendComMessage(_allyAgent, message);
            return Node.NodeStatus.Success;
        }
        return Node.NodeStatus.Failure;
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
        blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> _allies);
        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController _agent);
        
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
        blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _target);
        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController _agent);

        if (!_target || !_target.TryGetComponent(out Character _character)) return Node.NodeStatus.Failure;

        Vector3 diffVec = _character.Weapon.transform.position - _agent.transform.position;

        _agent.InitiateAttackAction(ActionInput.Press, RadialHelper.CartesianToPol(diffVec.normalized).y);

        return Node.NodeStatus.Success;
    }
}

public class SurroundTargetStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    private EnemyController agent;
    private float arrivalThreshold = 0.5f;
    private float sectorAngle = 170;

    public SurroundTargetStrategy(EnemyBlackboard pBlackboard) {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
    }

    public Node.NodeStatus Process() {
        if (!blackboard.TryGetValue(CommonKeys.SurroundTarget, out GameObject target)) return Node.NodeStatus.Failure;
        if (!blackboard.TryGetValue(CommonKeys.SurroundAllies, out List<GameObject> allies))
            return Node.NodeStatus.Failure;
        if (!blackboard.TryGetValue(CommonKeys.SurroundRadius, out float radius)) radius = 5f;
        if (!agent) return Node.NodeStatus.Failure;
        if (!blackboard.TryGetValue(CommonKeys.SurroundDirection, out Vector3 direction)) direction = Vector3.forward;

        int index = allies.IndexOf(agent.gameObject);
        if (index == -1) return Node.NodeStatus.Failure;

        Vector3 targetPos = target.transform.position;
        Vector3 forward = direction.normalized;

        Vector3 desiredPosition = GetSectorPosition(targetPos, index, allies.Count, radius, sectorAngle, forward);
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
        float _angle = 2 * Mathf.PI * pIndex / pTotal;
        Vector2 _pos = new Vector2(pCenter.x, pCenter.z) + RadialHelper.PolarToCart(_angle, pRadius);
        return new Vector3(_pos.x, pCenter.y, _pos.y);
    }

    private void endSurround() {
        agent.TreeValues.CombatTactic.IsSurroundModified = false;
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