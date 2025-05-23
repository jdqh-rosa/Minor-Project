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

public class CalculatePositionStrategy : IStrategy
{
    private Blackboard blackboard;
    private Vector3 avoidPosition;

    public CalculatePositionStrategy(Blackboard pBlackboard) {
        blackboard = pBlackboard;
    }

    public Node.NodeStatus Process() {
        //todo: actual calculations?
        blackboard.TryGetValue(CommonKeys.TargetPosition, out Vector3 targetPos);
        blackboard.SetKeyValue(CommonKeys.ChosenPosition, targetPos);
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

//depr::See getclosestally
public class ChooseAllyStrategy : ChooseObjectStrategy
{
    public ChooseAllyStrategy(Blackboard pBlackboard) : base(pBlackboard,
        pBlackboard.GetOrRegisterKey(CommonKeys.VisibleAllies), pBlackboard.GetOrRegisterKey(CommonKeys.TargetAlly)) { }
}

//depr::See getclosestenemy
public class ChooseEnemyStrategy : ChooseObjectStrategy
{
    public ChooseEnemyStrategy(Blackboard pBlackboard) : base(pBlackboard,
        pBlackboard.GetOrRegisterKey(CommonKeys.VisibleEnemies),
        pBlackboard.GetOrRegisterKey(CommonKeys.TargetEnemy)) { }
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

    public ContactAlliesStrategy(Blackboard pBlackboard) {
        blackboard = pBlackboard;
    }

    public Node.NodeStatus Process() {
        blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> allies);

        foreach (var ally in allies) {
            //todo: message ally
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
    
    private Character FindClosestCharacter(List<Character> characters) { //stolen from getclosestcharacter
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
        if(!avoidObject) return Node.NodeStatus.Failure;
        
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
            if (!hitCollider.gameObject.CompareTag("Character") || hitCollider.gameObject == agent.transform.gameObject) continue;
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

public class GetClosestAllyStrategy : GetClosestCharacterStrategy
{
    public GetClosestAllyStrategy(Blackboard pBlackboard) : base(pBlackboard, pBlackboard.GetOrRegisterKey(CommonKeys.TargetAlly), pBlackboard.GetOrRegisterKey(CommonKeys.VisibleAllies), pBlackboard.GetOrRegisterKey(CommonKeys.KnownAllies)) { }
}
public class GetClosestCharacterStrategy : IStrategy
{
    private Blackboard blackboard;
    private EnemyController agent;
    private BlackboardKey visibleKey;
    private BlackboardKey knownKey;
    private BlackboardKey targetKey;

    public GetClosestCharacterStrategy(Blackboard pBlackboard, BlackboardKey pTargetKey, BlackboardKey pVisibleKey, BlackboardKey pKnownKey=default) {
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
        GameObject closestCharacter = characters[0];
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
    public GetClosestEnemyStrategy(Blackboard pBlackboard) : base(pBlackboard, pBlackboard.GetOrRegisterKey(CommonKeys.TargetEnemy), pBlackboard.GetOrRegisterKey(CommonKeys.VisibleEnemies), pBlackboard.GetOrRegisterKey(CommonKeys.KnownEnemies)) { }
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
        return Node.NodeStatus.Success;
    }
}

public class SetTargetAllyStrategy : IStrategy
{
    private Blackboard blackboard;
    private GetClosestAllyStrategy _getClosestAlly;

    public SetTargetAllyStrategy(Blackboard pBlackboard) {
        blackboard = pBlackboard;
        _getClosestAlly = new(blackboard);
    }

    Node.NodeStatus IStrategy.Process() {
        if (_getClosestAlly.Process() == Node.NodeStatus.Success) {
            blackboard.TryGetValue(CommonKeys.TargetAlly, out GameObject target);
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

public class StrikeParry : IStrategy
{
    private Blackboard blackboard;
    
    public StrikeParry(Blackboard pBlackboard)
    {
        blackboard = pBlackboard;
    }
    public Node.NodeStatus Process()
    {
        blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _target);
        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController _agent);
        
        if(!_target || !_target.TryGetComponent(out Character _character)) return Node.NodeStatus.Failure;

        Vector3 diffVec = _character.Weapon.transform.position - _agent.transform.position;
        
        _agent.InitiateAttackAction(ActionInput.Press, RadialHelper.CartesianToPol(diffVec.normalized).y);
        
        return Node.NodeStatus.Success;
    }
}

// public class Strategy : IStrategy
// {
//     private Blackboard blackboard;
//     
//     public Strategy(Blackboard pBlackboard)
//     {
//         blackboard = pBlackboard;
//     }
//     public Node.NodeStatus Process()
//     {
//         
//         return Node.NodeStatus.Success;
//     }
// }