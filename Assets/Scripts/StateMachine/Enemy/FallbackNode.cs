using System.Collections.Generic;
using UnityEngine;

public class FallbackNode : BehaviourNode
{
    public FallbackNode(string pName) : base(pName){}
    List<BehaviourNode> children = new List<BehaviourNode>();
    
}
