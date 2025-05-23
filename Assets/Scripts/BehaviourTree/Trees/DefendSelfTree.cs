using UnityEngine;

public class DefendSelfTree : BehaviourTree
{
    Blackboard blackboard;
    private EnemyController agent;
    
    public DefendSelfTree(Blackboard pBlackboard, int pPriority = 0) : base("DefendSelf", pPriority)
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
        
        AddChild(_sequence);
        _sequence.AddChild(_detectAttack);
        _sequence.AddChild(_defendSelector);
        _defendSelector.AddChild(_evadeAction);
        _defendSelector.AddChild(_interceptAction);
        //_defendSelector.AddChild(_blockAction);
    }
}
