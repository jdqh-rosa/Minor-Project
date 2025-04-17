using System.Collections.Generic;
using UnityEngine;

public class SequenceNode : BehaviourNode
{
    public SequenceNode(string pName) : base(pName){}
    List<BehaviourNode> children = new List<BehaviourNode>();
}
