using System.Collections.Generic;
using UnityEngine;

public class AssembleTree : BehaviourTree
{
    Blackboard blackboard;
    private EnemyController agent;
    
    public AssembleTree(Blackboard pBlackboard, EnemyController pAgent, int pPriority = 0) : base("Assemble", pPriority)
    {
        blackboard = pBlackboard;
        agent = pAgent;
        
        setup();
    }

    private void setup()
    {
        
        Parallel _parallel = new Parallel("AssembleParallel", 2);
        
        Selector _selector = new Selector("Assemble//Selector");
        Sequence _sequence = new Sequence("Assemble//Sequence");
        blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> _allies);
        Leaf _allyCheck = new Leaf("Assemble//Selector/AllyCheck", new ConditionStrategy(()=> _allies.Count > 0 ));
        Leaf _findAllies = new Leaf("Assemble//Selector/FindAllies", new FindAlliesStrategy(blackboard, agent.transform, findRadius()));
        Leaf _allyPositionCheck = new Leaf("Assemble//Sequence/AllyPositionCheck", new ConditionStrategy(() => lastAlly()));
        
        
        _parallel.AddChild(_selector);
        _parallel.AddChild(_sequence);
        _selector.AddChild(_allyCheck);
        _selector.AddChild(_findAllies);
        _sequence.AddChild(_allyPositionCheck);
        _sequence.AddChild(new EnterRangeTree(blackboard, 2f));
        
        AddChild(_parallel);
    }

    private float findRadius()
    {
        blackboard.TryGetValue(CommonKeys.FindRadius, out float _findRadius);
        return _findRadius;
    }

    private bool lastAlly()
    {
        blackboard.TryGetValue(CommonKeys.LastAllyPosition, out Vector3 _lastAllyPosition);
        return _lastAllyPosition != default;
    }
}
