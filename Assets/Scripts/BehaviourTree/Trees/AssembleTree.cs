using System.Collections.Generic;
using UnityEngine;

public class AssembleTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    
    public AssembleTree(EnemyBlackboard pBlackboard, EnemyController pAgent, int pPriority = 0) : base("Assemble", pPriority)
    {
        blackboard = pBlackboard;
        
        setup();
    }

    private void setup()
    {
        
        Parallel _baseParallel = new Parallel("AssembleBaseParallel", 2,1);
        Sequence _sequence = new Sequence("Assemble/Sequence");
        Selector _selector = new Selector("Assemble//Selector");
        Selector _nearbyAllyBranch = new Selector("Assemble//NearbyAllySelector");
        Leaf _setTargetAlly = new ("Assemble//SetTargetAlly", new SetTargetAllyStrategy(blackboard));
        Leaf _allyCheck = new Leaf("Assemble//Selector/AllyCheck", new ConditionStrategy(()=>
        {
            blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> _allies);
            return _allies.Count > 0;
        }));
        Leaf _findAllies = new Leaf("Assemble//Selector/FindAllies", new FindAlliesStrategy(blackboard));
        Leaf _allyPositionCheck = new Leaf("Assemble//Sequence/AllyPositionCheck", new ConditionStrategy(() =>
        {
            blackboard.TryGetValue(CommonKeys.LastAllyPosition, out Vector3 _lastAllyPosition);
            return _lastAllyPosition != default;
        }));
        
        AddChild(_baseParallel);
        _baseParallel.AddChild(_sequence);
        _baseParallel.AddChild(new EnterRangeTree(blackboard, 2f));
        _sequence.AddChild(_selector);
        _sequence.AddChild(_setTargetAlly);
        _selector.AddChild(_nearbyAllyBranch);
        //_selector.AddChild(_allyPositionCheck); todo: implement this
        _nearbyAllyBranch.AddChild(_allyCheck);
        _nearbyAllyBranch.AddChild(_findAllies);
    }
}
