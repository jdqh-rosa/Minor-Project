using System.Collections.Generic;
using System.Timers;
using UnityEngine;

public class ParallelNode : BehaviourNode
{
    public ParallelNode(string pName) : base(pName){}
    List<BehaviourNode> children = new List<BehaviourNode>();


    void ProcessChildren() {
        foreach (BehaviourNode node in children) {
            node.UpdateLogic(Time.deltaTime);
        }
    }
}
