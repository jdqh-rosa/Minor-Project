using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Diagnostics;

public class Node : ScriptableObject
{
    public enum NodeStatus
    {
        Success,
        Failure,
        Running
    }

    public readonly string name;
    public readonly int priority;

    public readonly List<Node> children;
    protected int currentChild;

    public Node(string name = "Node", int pPriotity = 0) {
        this.name = name;
        priority = pPriotity;
    }

    public void AddChild(Node child) => children.Add(child);

    public virtual NodeStatus Process() => children[currentChild].Process();

    public virtual void Reset() {
        currentChild = 0;
        foreach (var child in children) {
            child.Reset();
        }
    }
}

public class Inverter : Node
{
    public Inverter(string pName = "Inverter") : base(pName) { }

    public override NodeStatus Process() {
        switch (children[0].Process()) {
            case NodeStatus.Running:
                return NodeStatus.Running;
            case NodeStatus.Failure:
                return NodeStatus.Success;
            default:
                return NodeStatus.Failure;
        }
    }
}

public class UntilFail : Node
{
    public UntilFail(string pName = "UntilFail") : base(pName) { }
    
    public override NodeStatus Process() {
        if (children[0].Process() == NodeStatus.Failure) {
            Reset();
            return NodeStatus.Failure;
        }
        
        return NodeStatus.Running;
    }
}

public class Repeater : Node //0 goes on forever
{
    private int repetitions;
    int repeats=0;

    public Repeater(string pName = "Repeater", int pRepetitions = 0) : base(pName) {
        repetitions = pRepetitions;
    }

    public override NodeStatus Process() {
        if (repetitions == 0 || (repetitions != 0 && repeats < repetitions)) {
            base.Process();
            return NodeStatus.Running;
        }

        return repeats != repetitions ? NodeStatus.Failure : NodeStatus.Success;
    }

    public override void Reset() {
        base.Reset();
        repeats = 0;
    }
}
public class Selector : Node
{
    public Selector(string name, int pPriority=0) : base(name, pPriority) { }

    public override NodeStatus Process() {
        if (currentChild < children.Count) {
            switch (children[currentChild].Process()) {
                case NodeStatus.Running:
                    return NodeStatus.Running;
                case NodeStatus.Success:
                    Reset();
                    return NodeStatus.Success;
                default:
                    currentChild++;
                    return NodeStatus.Running;
            }
        }

        Reset();
        return NodeStatus.Failure;
    }
}

public class PrioritySelector : Selector
{
    List<Node> sortedChildren;
    List<Node> SortedChildren => sortedChildren ??= SortChildren();

    protected virtual List<Node> SortChildren() => children.OrderByDescending(child => child.priority).ToList();

    public PrioritySelector(string name = "PrioritySelector", int pPriority=0) : base(name, pPriority) { }

    public override void Reset() {
        base.Reset();
        sortedChildren = null;
    }

    public override NodeStatus Process() {
        foreach (var child in SortedChildren) {
            switch (children[currentChild].Process()) {
                case NodeStatus.Running:
                    return NodeStatus.Running;
                case NodeStatus.Success:
                    Reset();
                    return NodeStatus.Success;
                default:
                    continue;
            }
        }

        return NodeStatus.Failure;
    }
}

public class RandomSelector : PrioritySelector
{
    protected override List<Node> SortChildren() => children.OrderBy(i => Guid.NewGuid()).ToList(); //idk about this check fisher yates icof
    
    public RandomSelector(string name) : base(name) { }
}

public class Sequence : Node
{
    public Sequence(string pName = "Sequence", int pPriority=0) : base(pName, pPriority) { }

    public override NodeStatus Process() {
        if (currentChild < children.Count) {
            switch (children[currentChild].Process()) {
                case NodeStatus.Running:
                    return NodeStatus.Running;
                case NodeStatus.Failure:
                    Reset();
                    return NodeStatus.Failure;
                default:
                    currentChild++;
                    return currentChild == children.Count ? NodeStatus.Success : NodeStatus.Running;
            }
        }

        Reset();
        return NodeStatus.Success;
    }
}

public class Leaf : Node
{
    readonly IStrategy strategy;

    public Leaf(string pName, IStrategy pStrategy, int pPriority=0) : base(pName, pPriority) {
        strategy = pStrategy;
    }
}

public class BehaviourTree : Node
{
    public BehaviourTree(string pName) : base(pName) { }

    public override NodeStatus Process() {
        while (currentChild < children.Count) {
            var status = children[currentChild].Process();
            if (status != NodeStatus.Running) {
                return status;
            }

            currentChild++;
        }

        return NodeStatus.Success;
    }
}


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