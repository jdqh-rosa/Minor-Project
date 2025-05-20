using UnityEngine;

public class DefendSelfTree : BehaviourTree
{
    Blackboard blackboard;
    private EnemyController agent;
    
    public DefendSelfTree(Blackboard pBlackboard, int pPriority = 0) : base("DefendSelf", pPriority)
    {
        blackboard = pBlackboard;
        
        setup();
    }

    private void setup()
    {
        Sequence _sequence = new("DefendSelf/Sequence");
        Leaf _detectAttack = new("DefendSelf//DetectAttack", new DetectAttackStrategy(blackboard));
        PrioritySelector _defendSelector = new("DefendSelf//Selector");
        //Leaf _evadeAction = new("DefendSelf///Evade", );
        //Leaf _parryAction = new("DefendSelf///Parry", );
        //Leaf _blockAction = new("DefendSelf///Block", );
        
        AddChild(_sequence);
        _sequence.AddChild(_detectAttack);
        _sequence.AddChild(_defendSelector);
        //_defendSelector.AddChild(_evadeAction);
        //_defendSelector.AddChild(_parryAction);
        //_defendSelector.AddChild(_blockAction);
    }
}
