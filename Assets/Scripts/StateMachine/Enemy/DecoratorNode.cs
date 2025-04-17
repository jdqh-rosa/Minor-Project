using UnityEngine;

public class DecoratorNode : BehaviourNode
{
    public DecoratorNode(string pName) : base(pName){}
    BehaviourNode child = new BehaviourNode("DecoratorChild");
}
