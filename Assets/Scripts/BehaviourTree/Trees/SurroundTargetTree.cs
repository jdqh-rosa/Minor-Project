using UnityEngine;

public class SurroundTargetTree : BehaviourTree
{
    Blackboard blackboard;
    
    public SurroundTargetTree(Blackboard pBlackboard) : base("SurroundTarget") {
        blackboard = pBlackboard;
        
        setup();
    }

    void setup() {
        Sequence _sequence = new Sequence("SurroundTarget/Sequence");
        //todo: Find visible allies
        //todo: Communicate with allies //SurroundStrategy
        //todo: move into position
    }

    private void DetermineDirection() {
        
    }
}
