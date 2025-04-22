using System;
using System.Collections.Generic;
using Unity.VisualScripting;
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

public class FindTargetsStrategy : IStrategy
{
    private Blackboard blackboard;
    private Transform agentTransform;
    private float findRadius;
    private string findLayer;
    
    public FindTargetsStrategy(Blackboard pBlackboard, Transform pAgentTransform, float pFindRadius,
        string pFindLayer) {
        blackboard = pBlackboard;
        agentTransform = pAgentTransform;
        findRadius = pFindRadius;
        findLayer = pFindLayer;
    }

    public Node.NodeStatus Process() {
        Collider[] _hitColliders = Physics.OverlapSphere(agentTransform.position, findRadius);  
        List<GameObject> _targetsFound = new();

        foreach (var hitCollider in _hitColliders) {
            if (hitCollider.gameObject.layer == LayerMask.NameToLayer(findLayer)) {
                _targetsFound.Add(hitCollider.gameObject);
            }
        }

        BlackboardKey _targetsKey = blackboard.GetOrRegisterKey("Targets");
        blackboard.SetValue(_targetsKey, _targetsFound);

        Debug.Log($"Found {_targetsFound.Count} targets on Layer: {findLayer}");
        return _targetsFound.Count == 0 ? Node.NodeStatus.Failure : Node.NodeStatus.Success;
    }
}

public class ObtainTargetStrategy : IStrategy
{
    private Blackboard blackboard;

    public ObtainTargetStrategy(Blackboard pBlackboard) {
        blackboard = pBlackboard;
    }

    public Node.NodeStatus Process() {
        BlackboardKey _targetsKey = blackboard.GetOrRegisterKey("Targets");
        if (blackboard.TryGetValue(_targetsKey, out List<GameObject> targets)) {
            //todo: use logic to choose most applicable target
            BlackboardKey _target = blackboard.GetOrRegisterKey("ActiveTarget");
            blackboard.SetValue(_target, targets[0]);
            
            return Node.NodeStatus.Success;
        }
        return Node.NodeStatus.Failure;
    }
}