using System;
using System.Collections.Generic;
using System.Linq;

public class Node
{
    public enum NodeStatus
    {
        Success,
        Failure,
        Running
    }

    public readonly string Name;
    public readonly int Priority;

    public readonly List<Node> children = new();
    public Node Parent;
    public string Path;
    protected int currentChild;
    public bool IsActive;
    private NodeStatus status;

    public Node(string pName = "Node", int pPriority = 0) {
        Name = pName;
        Priority = pPriority;
    }

    public virtual void AddChild(Node child) {
        child.Parent = this;
        child.Path = $"{Path} > {child.Name}";
        children.Add(child);
    } 

    public virtual NodeStatus Process()
    {
        IsActive = true;
        status = children[currentChild].Process();
        IsActive = false;
        return status;
    }

    public virtual void Reset() {
        currentChild = 0;
        foreach (var child in children) {
            child.Reset();
        }
        IsActive = false;
    }

    public int GetCurrentChildIndex() {
        return currentChild;
    }

    public NodeStatus GetStatus() {
        return status;
    }
}

public class DecoratorNode : Node
{
    public DecoratorNode(string pName = "Decorator") : base(pName) { }
    
    public override void AddChild(Node child) {
        child.Parent = this;
        child.Path = $"{Path} > {child.Name}";
        if (children.Count > 0) {
            children[0] = child;
        }
        else {
            children.Add(child);
        }
    }
}

public class Inverter : DecoratorNode
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

public class UntilFail : DecoratorNode
{
    public UntilFail(string pName = "UntilFail") : base(pName) { }

    public override NodeStatus Process() {
        if (children[0].Process() == NodeStatus.Failure) {
            Reset();
            return NodeStatus.Success;
        }
        IsActive = true;
        return NodeStatus.Running;
    }
}

public class Parallel : Node
{
    private int succesThreshold;
    private int failureThreshold;
    public Parallel(string name, int pSuccesThreshold, int pFailureThreshold =0, int pPriority = 0) : base(name, pPriority) {
        succesThreshold = pSuccesThreshold;
        failureThreshold = pFailureThreshold;
    }

    public override NodeStatus Process() {
        int succesCount = 0;
        int failureCount = 0;
        if (failureThreshold == 0) failureThreshold = children.Count;
        foreach (Node child in children) {
            var status = child.Process();
            if (status == NodeStatus.Success) {
                succesCount++;
            }else if (status == NodeStatus.Failure) {
                failureCount++;
            }
        }

        if (succesCount >= succesThreshold) return NodeStatus.Success;
        IsActive = true;
        return failureCount >= failureThreshold ? NodeStatus.Failure : NodeStatus.Running;
    }
}

public class Repeater : DecoratorNode //0 goes on forever
{
    private int repetitions;
    int repeats = 0;

    public Repeater(string pName = "Repeater", int pRepetitions = 0) : base(pName) {
        repetitions = pRepetitions;
    }

    public override NodeStatus Process() {
        if (repetitions == 0 || (repetitions != 0 && repeats < repetitions)) {
            base.Process();
            IsActive = true;
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
    public Selector(string name, int pPriority = 0) : base(name, pPriority) { }

    public override NodeStatus Process() {
        if (currentChild < children.Count) {
            switch (children[currentChild].Process()) {
                case NodeStatus.Running:
                    IsActive = true;
                    return NodeStatus.Running;
                case NodeStatus.Success:
                    Reset();
                    return NodeStatus.Success;
                default:
                    currentChild++;
                    IsActive = true;
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

    protected virtual List<Node> SortChildren() => children.OrderByDescending(child => child.Priority).ToList();

    public PrioritySelector(string name = "PrioritySelector", int pPriority = 0) : base(name, pPriority) { }

    public override void Reset() {
        base.Reset();
        sortedChildren = null;
    }

    public override NodeStatus Process() {
        IsActive = true;
        foreach (var child in SortedChildren) {
            switch (child.Process()) {
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
    protected override List<Node> SortChildren() =>
        children.OrderBy(i => Guid.NewGuid()).ToList(); //idk about this check fisher yates icof

    public RandomSelector(string name) : base(name) { }
}

public class Sequence : Node
{
    public Sequence(string pName = "Sequence", int pPriority = 0) : base(pName, pPriority) { }

    public override NodeStatus Process() {
        IsActive = true;
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
    public Leaf(string pName, IStrategy pStrategy, int pPriority = 0) : base(pName, pPriority) {
        strategy = pStrategy;
    }
    public override NodeStatus Process() => strategy.Process();
    public override void Reset() => strategy.Reset();
}

public class BehaviourTree : Node
{
    public BehaviourTree(string pName, int pPriority =0) : base(pName, pPriority) { }

    public override NodeStatus Process() {
        IsActive = true;
        foreach (Node child in children) {
            var status = child.Process();
            if (status != NodeStatus.Success) {
                //Debug.Log($"{Name} : {children[currentChild].Path}=>{status}");
                return status;
            }
        }
        // while (currentChild < children.Count) {
        //     var status = children[currentChild].Process();
        //     if (status != NodeStatus.Success) {
        //         //Debug.Log($"{Name} : {children[currentChild].Path}=>{status}");
        //         return status;
        //     }
        //     currentChild++;
        // }
        //Debug.Log($"{Name} =>{NodeStatus.Success}");
        return NodeStatus.Success;
    }
}