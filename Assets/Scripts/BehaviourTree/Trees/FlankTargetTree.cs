using UnityEngine;

public class FlankTargetTree : BehaviourTree
{
    Blackboard blackboard;
    
    public FlankTargetTree(Blackboard pBlackboard) : base("FlankTarget") {
        blackboard = pBlackboard;

        setup();
    }
    void setup() {
        
    }

    private void DetermineFlankDirection() {
        
    }

}
