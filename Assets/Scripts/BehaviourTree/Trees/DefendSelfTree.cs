using UnityEngine;

public class DefendSelfTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    private EnemyController agent;
    
    public DefendSelfTree(EnemyBlackboard pBlackboard, int pPriority = 0) : base("DefendSelf", pPriority)
    {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        setup();
    }

    private void setup()
    {
        Sequence _sequence = new("DefendSelf/Sequence");
        Leaf _detectAttack = new("DefendSelf//DetectAttack", new DetectAttackStrategy(blackboard));
        PrioritySelector _defendSelector = new("DefendSelf//Selector");
        Leaf _evadeAction = new("DefendSelf///Evade", new DodgeStrategy(blackboard));
        Leaf _interceptAction = new("DefendSelf///StrikeParry", new StrikeParry(blackboard));
        //Leaf _blockAction = new("DefendSelf///Block", );
        Leaf _retreatAction = new("", new RetreatFromPositionStrategy(blackboard));
        
        AddChild(_sequence);
        _sequence.AddChild(_detectAttack);
        _sequence.AddChild(_defendSelector);
        _defendSelector.AddChild(_evadeAction);
        _defendSelector.AddChild(_interceptAction);
        //_defendSelector.AddChild(_blockAction);
        _defendSelector.AddChild(_retreatAction);
    }
}
