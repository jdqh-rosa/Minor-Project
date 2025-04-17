using UnityEngine;

public class BehaviourNode : EnemyState
{
    public BehaviourNode(string pName) : base(pName){}
    public NodeStatus Status = NodeStatus.Inactive;


}

public enum NodeStatus
{
    Inactive,
    Success,
    Failure,
    Running
}
