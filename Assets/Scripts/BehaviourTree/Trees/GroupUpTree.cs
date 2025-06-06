using System.Collections.Generic;
using UnityEngine;

public class GroupUpTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    private EnemyController agent;
    private bool sender=false;
    
    public GroupUpTree(EnemyBlackboard pBlackboard) : base("GroupUp"){
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        
        setup();
    }
    
    void setup() {

        Sequence _sequence = new("GroupUp/Sequence");
        Leaf _findAllies = new("GroupUp/FindAllies", new FindAlliesStrategy(blackboard));
        Leaf _contactAllies = new("GroupUp/ContactAllies", new ContactAlliesStrategy(blackboard, GroupUpMessage()));
        Leaf _groupUpAction = new("", new GroupUpStrategy(blackboard));
        EnterRangeTree _enterRange = new(blackboard, 2f);

        AddChild(_sequence);
        _sequence.AddChild(_findAllies);
        _sequence.AddChild(_contactAllies);
        _sequence.AddChild(_groupUpAction);
        _sequence.AddChild(_enterRange);
    }

    private ComMessage GroupUpMessage() {
        Dictionary<MessageInfoType, object> _messagePayload = new();
        blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> _visibleAllies);
        
        Vector3 _groupUpPosition = agent.transform.position; //todo: set better goup up position
        _messagePayload.Add(MessageInfoType.Allies, _visibleAllies);
        _messagePayload.Add(MessageInfoType.Position, _groupUpPosition);
        
        return new ComMessage(agent, null ,MessageType.GroupUp, _messagePayload, Time.time);
    }
}
