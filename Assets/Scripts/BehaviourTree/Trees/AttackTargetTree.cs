using UnityEngine;

public class AttackTargetTree : BehaviourTree
{
    Blackboard blackboard;
    private EnemyController agent;
    
    public AttackTargetTree(Blackboard pBlackboard, EnemyController pAgent, int pPriority = 0) : base("AttackTarget", pPriority)
    {
        blackboard = pBlackboard;
        agent = pAgent;
        
        setup();
    }

    private void setup()
    {
        AddChild(new EnterRangeTree(blackboard, agent.GetWeaponRange()));
        AddChild(new ChooseAttackTree(blackboard));
        AddChild(new AttackTree(blackboard, agent));
    }
}
