using System.Collections.Generic;
using UnityEngine;

public class MessengerTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    private EnemyController agent;
    public MessengerTree(EnemyBlackboard pBlackboard, int pPriority = 0) : base("MessageTree", pPriority) {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        setup();
    }

    private void setup() {
        Sequence _baseSequence = new ("MessageTreeBase");
        Leaf _alliesAvailable = new("MessageTree/AlliesAvailable", new ConditionStrategy(() => blackboard.AlliesAvailable()));
        PrioritySelector _messengerSelector = new("MessageTree/MessengerSelector");
        
        
        Sequence _flankSequence = new("MessageTree//Flank", ()=> agent.TreeValues.Messenger.FlankWeight);
        Leaf _flankViable = new("MessageTree//Flank/ViableCheck", new ConditionStrategy(() =>
        {
            blackboard.TryGetValue(CommonKeys.FlankFlag, out bool flankFlag);
            return blackboard.EnemiesAvailable() && !flankFlag;
        })); //todo: check if appropriate flag is off
        Leaf _flankMessage = new("MessageTree//Flank/SendMessage", new SendMessageToAllyStrategy(blackboard, ()=> targetAlly(), ()=> flankMessage()));
        
        Sequence _groupUpSequence = new("MessageTree//GroupUp", ()=> agent.TreeValues.Messenger.GroupUpWeight);
        Leaf _groupUpViable = new("MessageTree//GroupUp/ViableCheck", new ConditionStrategy(() =>
        {
            blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> visibleAllies);
            blackboard.TryGetValue(CommonKeys.GroupUpFlag, out bool groupUpFlag);
            return visibleAllies.Count > 2 && !groupUpFlag;
        }));
        Leaf _groupUpMessage = new("MessageTree//GroupUp/SendMessage", new SendMessageToAlliesStrategy(blackboard, groupUpMessage));
        
        Sequence _retreatSequence = new("MessageTree//Retreat", ()=> agent.TreeValues.Messenger.RetreatWeight);
        Leaf _retreatViable = new("MessageTree//Retreat/ViableCheck", new ConditionStrategy(() => blackboard.GetHealth() > agent.TreeValues.Health.LowHealthThreshold));
        Leaf _retreatMessage = new("MessageTree//Retreat/SendMessage", new SendMessageToAlliesStrategy(blackboard, retreatMessage));
        
        Sequence _backUpSequence = new("MessageTree//BackUp", ()=> agent.TreeValues.Messenger.BackUpWeight);
        Leaf _backUpViable = new("MessageTree//BackUp/ViableCheck", new ConditionStrategy(() => blackboard.EnemiesAvailable() && blackboard.GetHealth() > agent.TreeValues.Health.LowHealthThreshold));
        Leaf _backUpMessage = new("MessageTree//BackUp/SendMessage", new SendMessageToAlliesStrategy(blackboard, backUpMessage));
        
        Sequence _surroundSequence = new("MessageTree//Surround", ()=> agent.TreeValues.Messenger.SurroundWeight);
        Leaf _surroundViable = new("MessageTree//Surround/ViableCheck", new ConditionStrategy(() =>
        {
            blackboard.TryGetValue(CommonKeys.VisibleEnemies, out List<GameObject> _visibleEnemies);
            blackboard.TryGetValue(CommonKeys.SurroundFlag, out bool surroundFlag);
            return blackboard.AlliesAvailable() && _visibleEnemies.Count > 0 && _visibleEnemies.Count < 2 && !surroundFlag;
        }));
        Leaf _surroundMessage = new("MessageTree//Surround/SendMessage", new SendMessageToAlliesStrategy(blackboard, surroundMessage));
        
        
        AddChild(_baseSequence);
        _baseSequence.AddChild(_alliesAvailable);
        _baseSequence.AddChild(_messengerSelector);
        
        _messengerSelector.AddChild(_flankSequence);
        _messengerSelector.AddChild(_groupUpSequence);
        _messengerSelector.AddChild(_retreatSequence);
        _messengerSelector.AddChild(_backUpSequence);
        _messengerSelector.AddChild(_surroundSequence);
        
        _flankSequence.AddChild(_flankViable);
        _flankSequence.AddChild(_flankMessage);
        
        _groupUpSequence.AddChild(_groupUpViable);
        _groupUpSequence.AddChild(_groupUpMessage);
        
        _retreatSequence.AddChild(_retreatViable);
        _retreatSequence.AddChild(_retreatMessage);
        
        _backUpSequence.AddChild(_backUpViable);
        _backUpSequence.AddChild(_backUpMessage);
        
        _surroundSequence.AddChild(_surroundViable);
        _surroundSequence.AddChild(_surroundMessage);
    }

    private GameObject targetAlly() {
        blackboard.TryGetValue(CommonKeys.TargetAlly, out GameObject ally);
        return ally;
    }
    private ComMessage flankMessage() {
        if (!blackboard.TryGetValue(CommonKeys.TargetAlly, out GameObject _ally)) return null;
        if(!_ally.TryGetComponent(out EnemyController _allyAgent)) return null;
        if(!blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _enemy)) return null;
        
        Dictionary<MessageInfoType, object> _payload = new Dictionary<MessageInfoType, object>
        {
            { MessageInfoType.Enemy, _enemy },
            { MessageInfoType.Ally, agent },
            { MessageInfoType.Direction, (_enemy.transform.position- agent.transform.position).normalized }
        };

        return new ComMessage(agent, _allyAgent, MessageType.Flank, _payload, Time.time);
    }

    private ComMessage groupUpMessage() {
        if (!blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> _allies)) return null;
        
        Dictionary<MessageInfoType, object> _payload = new Dictionary<MessageInfoType, object>
        {
            { MessageInfoType.Allies, _allies },
            { MessageInfoType.Position, agent.transform.position },
        };

        return new ComMessage(agent, null, MessageType.GroupUp, _payload, Time.time);
    }

    private ComMessage retreatMessage() {
        if (!blackboard.TryGetValue(CommonKeys.TargetAlly, out GameObject _ally)) return null;
        if(!_ally.TryGetComponent(out EnemyController _allyAgent)) return null;
        if(!blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _enemy)) return null;
        
        Dictionary<MessageInfoType, object> _payload = new Dictionary<MessageInfoType, object>
        {
            { MessageInfoType.Position, _enemy.transform.position },
            { MessageInfoType.Distance, 10f }
        };

        return new ComMessage(agent, _allyAgent, MessageType.Retreat, _payload, Time.time);
    }
    private ComMessage backUpMessage() {
        if (!blackboard.TryGetValue(CommonKeys.TargetAlly, out GameObject _ally)) return null;
        if(!_ally.TryGetComponent(out EnemyController _allyAgent)) return null;
        if(!blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _enemy)) return null;
        
        Dictionary<MessageInfoType, object> _payload = new Dictionary<MessageInfoType, object>
        {
            { MessageInfoType.Enemy, _enemy }
        };

        return new ComMessage(agent, _allyAgent, MessageType.RequestBackup, _payload, Time.time);
    }

    private ComMessage surroundMessage() {
        if (!blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> _allies)) return null;
        if(!blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _enemy)) return null;
        
        Dictionary<MessageInfoType, object> _payload = new Dictionary<MessageInfoType, object>
        {
            { MessageInfoType.Enemy, _enemy },
            { MessageInfoType.Allies, _allies }, 
            { MessageInfoType.Direction, 270f }, //surroundAngle
            { MessageInfoType.Distance, 5f } //surroundRadius
        };

        return new ComMessage(agent, null, MessageType.SurroundTarget, _payload, Time.time);
    }
}
