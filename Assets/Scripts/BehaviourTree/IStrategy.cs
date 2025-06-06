using System;
using System.Collections.Generic;
using System.Net;
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

public class CalculatePositionStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    private Vector3 avoidPosition;

    public CalculatePositionStrategy(EnemyBlackboard pBlackboard) {
        blackboard = pBlackboard;
    }

    public Node.NodeStatus Process() {
        //todo: actual calculations?

        switch (blackboard.GetActiveTargetType()) {
            case TargetType.Enemy:
                break;
            case TargetType.Object:
                break;
            case TargetType.Ally:
                break;
            default:
                blackboard.TryGetValue(CommonKeys.TargetPosition, out Vector3 targetPos);
                blackboard.SetKeyValue(CommonKeys.ChosenPosition, targetPos);
                break;
        }


        return Node.NodeStatus.Success;
    }
}

public class CalculateWeaponAngleStrategy : IStrategy
{
    private Blackboard blackboard;
    private Vector3 avoidPosition;

    public CalculateWeaponAngleStrategy(Blackboard pBlackboard) {
        blackboard = pBlackboard;
    }

    public Node.NodeStatus Process() {
        blackboard.TryGetValue(CommonKeys.ChosenFaceAngle, out Vector3 targetPos);
        blackboard.SetKeyValue(CommonKeys.TargetPosition, targetPos);
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

    public new Node.NodeStatus Process() {
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

    public new Node.NodeStatus Process() {
        var status = base.Process();
        blackboard.SetKeyValue(CommonKeys.ActiveTarget, TargetType.Ally);
        return status;
    }
}

public class ChooseObjectStrategy : IStrategy
{
    private Blackboard blackboard;
    private BlackboardKey listKey;
    private BlackboardKey targetKey;

    public ChooseObjectStrategy(Blackboard pBlackboard, BlackboardKey pListKey, BlackboardKey ptargetKey) {
        blackboard = pBlackboard;
        listKey = pListKey;
        targetKey = ptargetKey;
    }

    public Node.NodeStatus Process() {
        if (!blackboard.TryGetValue(listKey, out List<GameObject> targets)) return Node.NodeStatus.Failure;
        //todo: use logic to choose most applicable target
        blackboard.SetValue(targetKey, targets[0]);
        blackboard.SetKeyValue(CommonKeys.TargetPosition, targets[0].transform.position);

        return Node.NodeStatus.Success;
    }
}

public class ContactAllyStrategy : IStrategy
{
    private Blackboard blackboard;
    private Vector3 avoidPosition;

    public ContactAllyStrategy(Blackboard pBlackboard) {
        blackboard = pBlackboard;
    }

    public Node.NodeStatus Process() {
        blackboard.TryGetValue(CommonKeys.TargetAlly, out GameObject ally);
        //todo: message ally
        return Node.NodeStatus.Success;
    }
}

public class ContactAlliesStrategy : IStrategy
{
    private Blackboard blackboard;
    private EnemyController agent;
    private ComMessage message;

    public ContactAlliesStrategy(Blackboard pBlackboard, ComMessage pMessage) {
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
    private Blackboard blackboard;
    private EnemyController agent;

    public DetectAttackStrategy(Blackboard pBlackboard) {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController _agent);
        agent = _agent;
    }

    public Node.NodeStatus Process() {
        blackboard.TryGetValue(CommonKeys.VisibleEnemies, out List<GameObject> enemies);

        List<Character> _tryHitters = new List<Character>();

        foreach (var _enemy in enemies) {
            if (!_enemy.TryGetComponent(out Character _character)) continue;

            if (_character.IsAttacking()) {
                //todo: see if enemy attack makes contact
                Vector3 _diffVec =
                    MiscHelper.DifferenceVector(agent.transform.position, _character.transform.position);

                if (_diffVec.magnitude > _character.GetWeaponRange()) continue;

                float _angle = RadialHelper.CartesianToPol(_diffVec.normalized).y;

                if (Mathf.Abs(_angle - _character.GetWeaponAngle()) > 180f) continue;

                _tryHitters.Add(_character);
            }
        }

        switch (_tryHitters.Count) {
            case 0:
                blackboard.SetKeyValue(CommonKeys.ChosenAction, ActionType.None);
                break;
            case >= 2:
                blackboard.SetKeyValue(CommonKeys.ChosenAction, ActionType.Dodge);
                blackboard.SetKeyValue(CommonKeys.TargetEnemy, FindClosestCharacter(_tryHitters).gameObject);
                break;
            default:
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

public class DistanceSelfStrategy : IStrategy
{
    private Blackboard blackboard;
    private Vector3 avoidPosition;

    public DistanceSelfStrategy(Blackboard pBlackboard, Vector3 pAvoidPosition) {
        blackboard = pBlackboard;
        avoidPosition = pAvoidPosition;
    }

    public Node.NodeStatus Process() {
        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController agent);
        Vector3 diffVec = avoidPosition - agent.transform.position;
        diffVec *= -1;
        blackboard.SetKeyValue(CommonKeys.TargetPosition, diffVec);
        return Node.NodeStatus.Success;
    }
}

public class DistanceSelfFromObjectStrategy : IStrategy
{
    private Blackboard blackboard;
    private GameObject avoidObject;
    private float minDistance;

    public DistanceSelfFromObjectStrategy(Blackboard pBlackboard, GameObject pAvoidObject, float pMinDistance) {
        blackboard = pBlackboard;
        avoidObject = pAvoidObject;
        minDistance = pMinDistance;
    }

    public Node.NodeStatus Process() {
        if (!avoidObject) return Node.NodeStatus.Failure;

        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController agent);
        Vector3 diffVec = avoidObject.transform.position - agent.transform.position;
        diffVec *= -1;
        blackboard.SetKeyValue(CommonKeys.TargetPosition, diffVec.normalized.normalized * minDistance);
        return Node.NodeStatus.Success;
    }
}

public class DodgeStrategy : IStrategy
{
    Blackboard blackboard;
    private EnemyController agent;

    public DodgeStrategy(Blackboard pBlackboard) {
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

public class ExecuteActionStrategy : IStrategy
{
    private Blackboard blackboard;

    public ExecuteActionStrategy(Blackboard pBlackboard) {
        blackboard = pBlackboard;
    }

    public Node.NodeStatus Process() {
        blackboard.TryGetValue(CommonKeys.ChosenAction, out Action action);
        //todo: process and use action

        return Node.NodeStatus.Success;
    }
}

public class ExecuteAttackStrategy : ExecuteActionStrategy
{
    private Blackboard blackboard;

    public ExecuteAttackStrategy(Blackboard pBlackboard) : base(pBlackboard) {
        blackboard = pBlackboard;
    }
}

public class FaceTargetStrategy : IStrategy
{
    private Blackboard blackboard;

    public FaceTargetStrategy(Blackboard pBlackboard) {
        blackboard = pBlackboard;
    }

    public Node.NodeStatus Process() {
        blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject target);
        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController agent);

        Vector3 diffVec = target.transform.position - agent.transform.position;

        blackboard.SetKeyValue(CommonKeys.ChosenFaceAngle, diffVec.normalized);

        return Node.NodeStatus.Success;
    }
}

public class FindAlliesStrategy : IStrategy
{
    FindCharactersStrategy findCharactersStrategy;

    public FindAlliesStrategy(Blackboard pBlackboard) {
        pBlackboard.TryGetValue(CommonKeys.TeamSelf, out CharacterTeam _team);
        BlackboardKey _keyList = pBlackboard.GetOrRegisterKey(CommonKeys.VisibleAllies);

        findCharactersStrategy = new(pBlackboard, _keyList, _team);
    }

    public Node.NodeStatus Process() {
        return findCharactersStrategy.Process();
    }
}

public class FindCharactersStrategy : IStrategy
{
    private Blackboard blackboard;
    private BlackboardKey listKey;
    private EnemyController agent;
    private float findRadius;
    private CharacterTeam team;
    private bool exclude;

    public FindCharactersStrategy(Blackboard pBlackboard, BlackboardKey pListKey,
        CharacterTeam pCharacterTeam = CharacterTeam.Any, bool pExcludeTeam = false) {
        blackboard = pBlackboard;
        listKey = pListKey;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        blackboard.TryGetValue(CommonKeys.FindRadius, out findRadius);
        //blackboard.TryGetValue(CommonKeys.TeamSelf, out team);
        team = pCharacterTeam;
        exclude = pExcludeTeam;
    }

    public Node.NodeStatus Process() {
        Collider[] _hitColliders = Physics.OverlapSphere(agent.transform.position, findRadius);
        List<GameObject> _targetsFound = new();

        foreach (var hitCollider in _hitColliders) {
            if (!hitCollider.gameObject.CompareTag("Character") ||
                hitCollider.gameObject == agent.transform.gameObject) continue;
            if (!hitCollider.gameObject.TryGetComponent(out Character _character)) continue;
            if (!exclude) {
                if (_character.GetData().CharacterTeam == team) {
                    _targetsFound.Add(hitCollider.gameObject);
                }

                continue;
            }

            if (team == CharacterTeam.Any || _character.GetData().CharacterTeam != team) {
                _targetsFound.Add(hitCollider.gameObject);
            }
        }

        blackboard.SetValue(listKey, _targetsFound);
        return _targetsFound.Count == 0 ? Node.NodeStatus.Failure : Node.NodeStatus.Success;
    }
}

public class FindEnemiesStrategy : IStrategy
{
    FindCharactersStrategy findCharactersStrategy;

    public FindEnemiesStrategy(Blackboard pBlackboard) {
        pBlackboard.TryGetValue(CommonKeys.TeamSelf, out CharacterTeam _team);
        BlackboardKey _keyList = pBlackboard.GetOrRegisterKey(CommonKeys.VisibleEnemies);

        findCharactersStrategy = new(pBlackboard, _keyList, _team, true);
    }

    public Node.NodeStatus Process() {
        return findCharactersStrategy.Process();
    }
}

public class FindObjectsStrategy : IStrategy
{
    private Blackboard blackboard;
    private BlackboardKey listKey;
    private Transform agentTransform;
    private float findRadius;
    private int findLayer;

    public FindObjectsStrategy(Blackboard pBlackboard, BlackboardKey pListKey,
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
    private Blackboard blackboard;
    private Vector3? lastFlankPosition;
    private float stuckTimer = 0f;
    private float maxStuckTime = 3f;

    public FlankStrategy(Blackboard pBlackboard) {
        blackboard = pBlackboard;
    }

    public Node.NodeStatus Process() {
        blackboard.TryGetValue(CommonKeys.FlankTarget, out GameObject _enemyTarget);
        if (_enemyTarget == null) return Node.NodeStatus.Failure;

        if (!blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController self)) return Node.NodeStatus.Failure;

        Vector3 enemyPosition = _enemyTarget.transform.position;

        if (!blackboard.TryGetValue(CommonKeys.FlankDirection, out Vector3 flankDirection)) {
            if (blackboard.TryGetValue(CommonKeys.FlankAlly, out GameObject ally)) {
                Vector3 allyPos = ally.transform.position;
                flankDirection = (enemyPosition - allyPos).normalized;
            }
            else {
                endFlank();
                return Node.NodeStatus.Failure;
            }
        }

        float flankDistance = 5f;
        Vector3 flankPosition = enemyPosition + flankDirection.normalized * flankDistance;


        if (lastFlankPosition.HasValue && Vector3.Distance(flankPosition, lastFlankPosition.Value) < 0.1f) {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= maxStuckTime) {
                return HandleFallback(self, enemyPosition);
            }
        }
        else {
            stuckTimer = 0f;
        }

        lastFlankPosition = flankPosition;
        blackboard.SetKeyValue(CommonKeys.TargetPosition, flankPosition);

        if (Vector3.Distance(self.transform.position, flankPosition) < 1f) {
            endFlank();
            return Node.NodeStatus.Success;
        }

        return Node.NodeStatus.Running;
    }

    private void endFlank() {
        blackboard.SetKeyValue<float?>(CommonKeys.FlankDirection, null);
        blackboard.SetKeyValue<GameObject>(CommonKeys.FlankTarget, null);
    }

    private Node.NodeStatus HandleFallback(EnemyController self, Vector3 enemyPosition) {
        Vector3 fallbackPosition = enemyPosition - self.transform.forward * 3f;
        blackboard.SetKeyValue(CommonKeys.TargetPosition, fallbackPosition);

        endFlank();

        return Node.NodeStatus.Failure;
    }
}

public class GetClosestAllyStrategy : GetClosestCharacterStrategy
{
    EnemyBlackboard blackboard;

    public GetClosestAllyStrategy(EnemyBlackboard pBlackboard) : base(pBlackboard,
        pBlackboard.GetOrRegisterKey(CommonKeys.TargetAlly), pBlackboard.GetOrRegisterKey(CommonKeys.VisibleAllies),
        pBlackboard.GetOrRegisterKey(CommonKeys.KnownAllies)) {
        blackboard = pBlackboard;
    }

    public new Node.NodeStatus Process() {
        var status = base.Process();
        blackboard.SetKeyValue(CommonKeys.ActiveTarget, TargetType.Ally);
        return status;
    }
}

public class GetClosestCharacterStrategy : IStrategy
{
    private Blackboard blackboard;
    private EnemyController agent;
    private BlackboardKey visibleKey;
    private BlackboardKey knownKey;
    private BlackboardKey targetKey;

    public GetClosestCharacterStrategy(Blackboard pBlackboard, BlackboardKey pTargetKey, BlackboardKey pVisibleKey,
        BlackboardKey pKnownKey = default) {
        blackboard = pBlackboard;
        visibleKey = pVisibleKey;
        knownKey = pKnownKey;
        targetKey = pTargetKey;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController _agent);
        agent = _agent;
    }

    public Node.NodeStatus Process() {
        if (blackboard.TryGetValue(visibleKey, out List<GameObject> characters)) {
            FindClosestCharacter(characters);
            return Node.NodeStatus.Success;
        }

        if (!blackboard.TryGetValue(knownKey, out List<GameObject> knownCharacters)) return Node.NodeStatus.Failure;

        FindClosestCharacter(knownCharacters);
        return Node.NodeStatus.Success;
    }

    private void FindClosestCharacter(List<GameObject> characters) {
        GameObject closestCharacter = null;
        float closestCharDistance = float.MaxValue;
        foreach (GameObject character in characters) {
            float charDistance = Mathf.Abs((agent.transform.position - character.transform.position).magnitude);
            if (!(charDistance < closestCharDistance)) continue;
            closestCharacter = character;
            closestCharDistance = charDistance;
        }

        blackboard.SetValue(targetKey, closestCharacter);
        blackboard.SetKeyValue(CommonKeys.TargetPosition, closestCharacter.transform.position);
    }
}

public class GetClosestEnemyStrategy : GetClosestCharacterStrategy
{
    public GetClosestEnemyStrategy(Blackboard pBlackboard) : base(pBlackboard,
        pBlackboard.GetOrRegisterKey(CommonKeys.TargetEnemy), pBlackboard.GetOrRegisterKey(CommonKeys.VisibleEnemies),
        pBlackboard.GetOrRegisterKey(CommonKeys.KnownEnemies)) { }
}

public class GroupUpStrategy : IStrategy
{
    private Blackboard blackboard;
    private float radius = 2.0f;
    private float arrivalThreshold = 1.0f;

    public GroupUpStrategy(Blackboard pBlackboard) {
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

        if (distance <= arrivalThreshold)
            return Node.NodeStatus.Success;

        blackboard.SetKeyValue(CommonKeys.TargetPosition, targetPosition);
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

public class MessageAllyStrategy : IStrategy
{
    private Blackboard blackboard;
    private GameObject ally;

    public MessageAllyStrategy(Blackboard pBlackboard, GameObject pAlly) {
        blackboard = pBlackboard;
        ally = pAlly;
    }

    public Node.NodeStatus Process() {
        //todo: message ally
        if (ally.TryGetComponent(out EnemyController _ally)) { }

        return Node.NodeStatus.Success;
    }
}

public class SetTargetAllyStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    private GetClosestAllyStrategy _getClosestAlly;
    private GameObject targetAlly;

    public SetTargetAllyStrategy(EnemyBlackboard pBlackboard, GameObject pAlly = null) {
        blackboard = pBlackboard;
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
            blackboard.SetKeyValue(CommonKeys.ActiveTarget, TargetType.Ally);
            blackboard.SetKeyValue(CommonKeys.TargetPosition, target.transform.position);
            return Node.NodeStatus.Success;
        }

        if (blackboard.TryGetValue(CommonKeys.LastAllyPosition, out Vector3 position)) {
            //todo: use last known ally position
            return Node.NodeStatus.Failure;
        }

        return Node.NodeStatus.Failure;
    }
}

public class MoveToPositionStrategy : IStrategy
{
    private Blackboard blackboard;
    private Vector3 destination;

    public MoveToPositionStrategy(Blackboard pBlackboard, Vector3 position) {
        blackboard = pBlackboard;
        destination = position;
    }

    public Node.NodeStatus Process() {
        blackboard.SetKeyValue(CommonKeys.ChosenPosition, destination);
        return Node.NodeStatus.Success;
    }
}

public class ProcessMessagesStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    private ComProtocol protocol;
    private int bandwidth;

    public ProcessMessagesStrategy(EnemyBlackboard pBlackboard, int pBandwidth) {
        blackboard = pBlackboard;
        bandwidth = pBandwidth;
        blackboard.TryGetValue(CommonKeys.ComProtocol, out protocol);
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
        GameObject _ally = (GameObject)message.Payload[MessageInfoType.Ally];
        Vector2 _flankDirection = (Vector2)message.Payload[MessageInfoType.Direction];

        Vector3 flankDir = new Vector3(_flankDirection.x, 0, _flankDirection.y);
        blackboard.SetKeyValue(CommonKeys.FlankDirection, flankDir);
        blackboard.SetKeyValue(CommonKeys.FlankAlly, _ally);
        blackboard.SetKeyValue(CommonKeys.FlankTarget, _enemyTarget);
    }

    private void ProcessGroupUp(ComMessage message) {
        List<GameObject> _groupUpAllies = (List<GameObject>)message.Payload[MessageInfoType.Allies];
        Vector3 _groupUpPosition = (Vector3)message.Payload[MessageInfoType.Position];

        blackboard.SetKeyValue(CommonKeys.GroupUpAllies, _groupUpAllies);
        blackboard.SetKeyValue(CommonKeys.GroupUpPosition, _groupUpPosition);
    }

    private void ProcessRetreat(ComMessage message) {
        Vector3 _position = (Vector3)message.Payload[MessageInfoType.Position];
        float _retreatDistance = (float)message.Payload[MessageInfoType.Distance];

        blackboard.SetKeyValue(CommonKeys.RetreatThreatPosition, _position);
        blackboard.SetKeyValue(CommonKeys.RetreatDistance, _retreatDistance);
    }

    private void ProcessRequestBackup(ComMessage message) {
        GameObject _enemyTarget = (GameObject)message.Payload[MessageInfoType.Enemy];
        blackboard.SetKeyValue(CommonKeys.TargetEnemy, _enemyTarget);
        //todo: influence to attack
    }

    private void ProcessSurroundTarget(ComMessage message) {
        GameObject _enemyTarget = (GameObject)message.Payload[MessageInfoType.Enemy];
        List<GameObject> _allies = (List<GameObject>)message.Payload[MessageInfoType.Allies];
        Vector2 _surroundAngle = (Vector2)message.Payload[MessageInfoType.Direction];
        float _surroundRadius = (float)message.Payload[MessageInfoType.Distance];

        blackboard.SetKeyValue(CommonKeys.SurroundTarget, _enemyTarget);
        blackboard.SetKeyValue(CommonKeys.SurroundAllies, _allies);
        blackboard.SetKeyValue(CommonKeys.SurroundRadius, _surroundRadius);
        blackboard.SetKeyValue(CommonKeys.SurroundDirection, _surroundAngle);
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

    public RetreatFromTargetStrategy(EnemyBlackboard pBlackboard, GameObject pTarget, float pRetreatDistance = 5.0f) : base(pBlackboard, pRetreatDistance) {
        blackboard = pBlackboard;
        target = pTarget;
    }

    public new Node.NodeStatus Process () {
        blackboard.SetKeyValue(CommonKeys.RetreatThreatPosition, target.transform.position);
        return base.Process();
    }
}

public class RetreatFromPositionStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    private float retreatDistance;

    public RetreatFromPositionStrategy(EnemyBlackboard pBlackboard, float pRetreatDistance = 5.0f) {
        blackboard = pBlackboard;
        retreatDistance = pRetreatDistance;
    }

    public Node.NodeStatus Process() {
        if (!blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController _agent)) return Node.NodeStatus.Failure;
        if (!blackboard.TryGetValue(CommonKeys.RetreatThreatPosition, out Vector3 _threatPos))
            return Node.NodeStatus.Failure;
        if (!blackboard.TryGetValue(CommonKeys.RetreatDistance, out float _retreatDistance))
            _retreatDistance = retreatDistance;

        Vector3 currentPos = _agent.transform.position;
        Vector3 awayDirection = (currentPos - _threatPos).normalized;

        if (awayDirection == Vector3.zero) awayDirection = UnityEngine.Random.insideUnitSphere.normalized;

        Vector3 retreatTarget = currentPos + awayDirection * _retreatDistance;

        float distanceFromThreat = Vector3.Distance(currentPos, _threatPos);
        if (distanceFromThreat >= _retreatDistance) {
            endRetreat();
            return Node.NodeStatus.Success;
        }

        blackboard.SetKeyValue(CommonKeys.TargetPosition, retreatTarget);
        return Node.NodeStatus.Running;
    }

    private void endRetreat() {
        blackboard.SetKeyValue<float?>(CommonKeys.RetreatDistance, null);
        blackboard.SetKeyValue<Vector3?>(CommonKeys.RetreatThreatPosition, null);
    }
}

public class SendMessageToAllyStrategy : IStrategy
{
    private EnemyBlackboard blackboard;
    private ComMessage message;
    protected GameObject recipient;

    public SendMessageToAllyStrategy(EnemyBlackboard pBlackboard, GameObject pRecipient, ComMessage _message) {
        blackboard = pBlackboard;
        recipient = pRecipient;
        message = _message;
    }

    public Node.NodeStatus Process() {
        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController _agent);
        if (recipient.TryGetComponent<EnemyController>(out EnemyController _allyAgent)) {
            _agent.SendComMessage(_allyAgent, message);
            return Node.NodeStatus.Success;
        }
        return Node.NodeStatus.Failure;
    }
}

public class SendMessageToAlliesStrategy : SendMessageToAllyStrategy
{
    private EnemyBlackboard blackboard;

    public SendMessageToAlliesStrategy(EnemyBlackboard pBlackboard, ComMessage _message) : base(pBlackboard, null, _message) {
        blackboard = pBlackboard;
    }

    public new Node.NodeStatus Process() {
        blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> _allies);
        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController _agent);
        foreach (var _ally in _allies) {
            recipient = _ally;
            base.Process();
        }

        return Node.NodeStatus.Success;
    }
}

public class StrikeParry : IStrategy
{
    private Blackboard blackboard;

    public StrikeParry(Blackboard pBlackboard) {
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
    private float arrivalThreshold = 0.5f;
    private float sectorAngle = 170;
    private float directionAngle = 90;

    public SurroundTargetStrategy(EnemyBlackboard pBlackboard) {
        blackboard = pBlackboard;
    }

    public Node.NodeStatus Process() {
        if (!blackboard.TryGetValue(CommonKeys.SurroundTarget, out GameObject target)) return Node.NodeStatus.Failure;
        if (!blackboard.TryGetValue(CommonKeys.SurroundAllies, out List<GameObject> allies))
            return Node.NodeStatus.Failure;
        if (!blackboard.TryGetValue(CommonKeys.SurroundRadius, out float radius)) radius = 5f;
        if (!blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController self)) return Node.NodeStatus.Failure;
        if (!blackboard.TryGetValue(CommonKeys.SurroundDirection, out Vector3 direction)) direction = Vector3.forward;

        int index = allies.IndexOf(self.gameObject);
        if (index == -1) return Node.NodeStatus.Failure;

        Vector3 targetPos = target.transform.position;
        Vector3 forward = direction.normalized;

        Vector3 desiredPosition = GetSectorPosition(targetPos, index, allies.Count, radius, sectorAngle, forward);
        float distance = Vector3.Distance(self.transform.position, desiredPosition);

        if (distance <= arrivalThreshold)
            return Node.NodeStatus.Success;

        blackboard.SetKeyValue(CommonKeys.TargetPosition, desiredPosition);
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