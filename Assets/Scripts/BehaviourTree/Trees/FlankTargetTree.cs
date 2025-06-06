using UnityEngine;

public class FlankTargetTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    
    public FlankTargetTree(EnemyBlackboard pBlackboard) : base("FlankTarget") {
        blackboard = pBlackboard;

        setup();
    }
    void setup() {
        Sequence _sequence = new("FlankTarget/Sequence");
        //Leaf _sendFlankMessage = new Leaf("Send Flank Message", new FlankStrategy(blackboard, ));
        
    }

    private void DetermineFlankDirection() {
        
    }

}
