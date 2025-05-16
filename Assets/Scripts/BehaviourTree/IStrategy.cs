using System;
using System.Collections.Generic;
using UnityEngine;

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

public class FindObjectsStrategy : IStrategy
{
    private Blackboard blackboard;
    private BlackboardKey listKey;
    private Transform agentTransform;
    private float findRadius;
    private int findLayer;
    
    public FindObjectsStrategy(Blackboard pBlackboard, BlackboardKey pListKey,  Transform pAgentTransform, float pFindRadius,
        int pFindLayer) {
        blackboard = pBlackboard;
        listKey = pListKey;
        agentTransform = pAgentTransform;
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

public class FindEnemiesStrategy : FindObjectsStrategy
{
    public FindEnemiesStrategy(Blackboard pBlackboard, Transform pAgentTransform, float pFindRadius) : base(pBlackboard, pBlackboard.GetOrRegisterKey(CommonKeys.VisibleEnemies), pAgentTransform, pFindRadius, LayerMask.NameToLayer("Character")) {
    }
}

public class FindAlliesStrategy : FindObjectsStrategy
{
    public FindAlliesStrategy(Blackboard pBlackboard, Transform pAgentTransform, float pFindRadius) : base(pBlackboard, pBlackboard.GetOrRegisterKey(CommonKeys.VisibleAllies), pAgentTransform, pFindRadius, LayerMask.NameToLayer("Character")) {
    }
}

public class ChooseObjectStrategy : IStrategy
{
    private Blackboard blackboard;
    private BlackboardKey listKey;
    private BlackboardKey targetKey;

    public ChooseObjectStrategy(Blackboard pBlackboard, BlackboardKey pListKey, BlackboardKey ptargetKey)
    {
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

public class ChooseEnemyStrategy : ChooseObjectStrategy
{
    public ChooseEnemyStrategy(Blackboard pBlackboard) : base (pBlackboard, pBlackboard.GetOrRegisterKey(CommonKeys.VisibleEnemies) , pBlackboard.GetOrRegisterKey(CommonKeys.TargetEnemy)) {}
}

public class ChooseAllyStrategy : ChooseObjectStrategy
{
    public ChooseAllyStrategy(Blackboard pBlackboard) : base (pBlackboard, pBlackboard.GetOrRegisterKey(CommonKeys.VisibleAllies) , pBlackboard.GetOrRegisterKey(CommonKeys.TargetAlly)) {}
}

public class MoveToPositionStrategy : IStrategy
{
    private Blackboard blackboard;
    private Vector3 destination;
    
    public MoveToPositionStrategy(Blackboard pBlackboard, Vector3 position)
    {
        blackboard = pBlackboard;
        destination = position;
    }
    
    public Node.NodeStatus Process()
    {
        blackboard.SetKeyValue(CommonKeys.TargetPosition, destination);
        return Node.NodeStatus.Success;
    }
}

public class CalculatePositionStrategy : IStrategy
{
    private Blackboard blackboard;
    private Vector3 avoidPosition;
    
    public CalculatePositionStrategy(Blackboard pBlackboard)
    {
        blackboard = pBlackboard;
    }
    public Node.NodeStatus Process()
    {
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
    
    public CalculateWeaponAngleStrategy(Blackboard pBlackboard)
    {
        blackboard = pBlackboard;
    }
    public Node.NodeStatus Process()
    {
        blackboard.TryGetValue(CommonKeys.ChosenFaceAngle, out Vector3 targetPos);
        blackboard.SetKeyValue(CommonKeys.ChosenPosition, targetPos);
        return Node.NodeStatus.Success;
    }
}

public class ContactAllyStrategy : IStrategy
{
    private Blackboard blackboard;
    private Vector3 avoidPosition;
    
    public ContactAllyStrategy(Blackboard pBlackboard)
    {
        blackboard = pBlackboard;
    }
    public Node.NodeStatus Process()
    {
        blackboard.TryGetValue(CommonKeys.TargetAlly, out GameObject ally);
        //todo: message ally
        return Node.NodeStatus.Success;
    }
}

public class ContactAlliesStrategy : IStrategy
{
    private Blackboard blackboard;
    
    public ContactAlliesStrategy(Blackboard pBlackboard)
    {
        blackboard = pBlackboard;
    }
    public Node.NodeStatus Process()
    {
        blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> allies);

        foreach (var ally in allies)
        {
            //todo: message ally
        }
        
        return Node.NodeStatus.Success;
    }
}

public class DetectAttackStrategy : IStrategy
{
    private Blackboard blackboard;
    
    public DetectAttackStrategy(Blackboard pBlackboard)
    {
        blackboard = pBlackboard;
    }
    public Node.NodeStatus Process()
    {
        blackboard.TryGetValue(CommonKeys.VisibleEnemies, out List<GameObject> enemies);
        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController _agent);

        foreach (var enemy in enemies)
        {
            if (!enemy.TryGetComponent(out Character _character)) continue;

            if (_character.IsAttacking())
            {
                //todo: see if enemy attack makes contact
                Vector3 _diffVec = MiscHelper.DifferenceVector(_agent.transform.position, _character.transform.position);

                if (_diffVec.magnitude > _character.GetWeaponRange()) continue;

                float _angle = RadialHelper.CartesianToPol(_diffVec.normalized).y;

                if (Mathf.Abs(_angle - _character.GetWeaponAngle()) > 180f) continue;
                
                //todo: deal with attack
            }
        }
        
        return Node.NodeStatus.Success;
    }
}

public class DistanceSelfStrategy : IStrategy
{
    private Blackboard blackboard;
    private Vector3 avoidPosition;
    
    public DistanceSelfStrategy(Blackboard pBlackboard, Vector3 pAvoidPosition)
    {
        blackboard = pBlackboard;
        avoidPosition = pAvoidPosition;
    }
    public Node.NodeStatus Process()
    {
        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController agent);
        Vector3 diffVec = avoidPosition - agent.transform.position;
        diffVec *= -1;
        blackboard.SetKeyValue(CommonKeys.ChosenPosition, diffVec);
        return Node.NodeStatus.Success;
    }
}

public class ExecuteActionStrategy : IStrategy
{
    private Blackboard blackboard;
    
    public ExecuteActionStrategy(Blackboard pBlackboard)
    {
        blackboard = pBlackboard;
    }
    public Node.NodeStatus Process()
    {
        blackboard.TryGetValue(CommonKeys.ChosenAction, out Action action);
        //todo: process and use action
        
        return Node.NodeStatus.Success;
    }
}

public class ExecuteAttackStrategy : ExecuteActionStrategy
{
    private Blackboard blackboard;
    
    public ExecuteAttackStrategy(Blackboard pBlackboard) : base(pBlackboard)
    {
        blackboard = pBlackboard;
    }
}

public class FaceTargetStrategy : IStrategy
{
    private Blackboard blackboard;
    
    public FaceTargetStrategy(Blackboard pBlackboard)
    {
        blackboard = pBlackboard;
    }
    public Node.NodeStatus Process()
    {
        blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject target);
        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController agent);

        Vector3 diffVec = target.transform.position - agent.transform.position;
        
        blackboard.SetKeyValue(CommonKeys.ChosenFaceAngle, diffVec.normalized);
        
        return Node.NodeStatus.Success;
    }
}
    
public class MessageAllyStrategy : IStrategy
{
    private Blackboard blackboard;
    private GameObject ally;
    
    public MessageAllyStrategy(Blackboard pBlackboard, GameObject pAlly)
    {
        blackboard = pBlackboard;
        ally = pAlly;
    }
    public Node.NodeStatus Process()
    {
        //todo: message ally
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