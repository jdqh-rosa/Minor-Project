using System.Collections.Generic;
using UnityEngine;

public class MessengerTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    private EnemyController agent;
    private ComMessage message;
    public MessengerTree(EnemyBlackboard pBlackboard, int pPriority = 0) : base("MessageTree", pPriority) {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        setup();
    }

    private void setup() {
        Sequence _baseSequence = new ("MessageTreeBase");
        Leaf _alliesAvailable = new("MessageTree/AlliesAvailable", new ConditionStrategy(() => blackboard.AlliesAvailable()));
        PrioritySelector _messengerSelector = new("MessageTree/MessengerSelector");
        
        
        Sequence _flankSequence = new("MessageTree//Flank", ()=> agent.TreeValues.Messenger.FlankWeight + (agent.TreeValues.Messenger.IsFlankModified ? agent.TreeValues.Messenger.FlankMod : 0));
        Leaf _flankViable = new("MessageTree//Flank/ViableCheck", new ConditionStrategy(() =>
        {
            blackboard.TryGetValue(CommonKeys.FlankFlag, out bool flankFlag);
            blackboard.TryGetValue(CommonKeys.PendingCoordination, out ComMessage pending);
            return blackboard.EnemiesAvailable() && !flankFlag && pending==null;
        })); //todo: check if appropriate flag is off
        Leaf _flankCreate = new("MessageTree//Surround/SendMessage", new ActionStrategy(() =>
        {
            message = flankMessage();
        }));
        Leaf _flankMessage = new("MessageTree//Flank/SendMessage", new SendMessageToAllyStrategy(blackboard, ()=> targetAlly(),
            () => message));
        Leaf _flankModified = new("MessageTree//Flank/FlankModify", new ActionStrategy(()=> agent.TreeValues.CombatTactic.IsFlankModified = true));
        
        Sequence _groupUpSequence = new("MessageTree//GroupUp", ()=> agent.TreeValues.Messenger.GroupUpWeight + (agent.TreeValues.Messenger.IsGroupUpModified ? agent.TreeValues.Messenger.GroupUpMod : 0));
        Leaf _groupUpViable = new("MessageTree//GroupUp/ViableCheck", new ConditionStrategy(() =>
        {
            blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> visibleAllies);
            blackboard.TryGetValue(CommonKeys.GroupUpFlag, out bool groupUpFlag);
            blackboard.TryGetValue(CommonKeys.PendingCoordination, out ComMessage pending);

            int farAllies = 0;
            foreach (GameObject ally in visibleAllies) {
                if(!ally) continue;
                if((ally.transform.position - agent.transform.position).magnitude >= 15) farAllies++;
            }
            
            return farAllies >= 2 && !groupUpFlag && pending==null;
        }));
        Leaf _groupUpMessage = new("MessageTree//GroupUp/SendMessage", new SendMessageToAlliesStrategy(blackboard, groupUpMessage));
        Leaf _groupUpModified = new("MessageTree//Flank/FlankModify", new ActionStrategy(()=> agent.TreeValues.Decider.IsAssembleModified = true));
        
        Sequence _retreatSequence = new("MessageTree//Retreat", ()=> agent.TreeValues.Messenger.RetreatWeight  + (agent.TreeValues.Messenger.IsRetreatModified ? agent.TreeValues.Messenger.RetreatMod : 0));
        Leaf _retreatViable = new("MessageTree//Retreat/ViableCheck", new ConditionStrategy(() => blackboard.EnemiesAvailable() && blackboard.CheckLowHealth()));
        Leaf _retreatMessage = new("MessageTree//Retreat/SendMessage", new SendMessageToAlliesStrategy(blackboard, retreatMessage));
        Leaf _retreatModified = new("MessageTree//Flank/FlankModify", new ActionStrategy(()=> agent.TreeValues.CombatTactic.IsRetreatModified = true));
        
        Sequence _backUpSequence = new("MessageTree//BackUp", ()=> agent.TreeValues.Messenger.BackUpWeight + (agent.TreeValues.Messenger.IsBackUpModified ? agent.TreeValues.Messenger.BackUpMod : 0));
        Leaf _backUpViable = new("MessageTree//BackUp/ViableCheck", new ConditionStrategy(() => blackboard.EnemiesAvailable() && blackboard.CheckLowHealth()));
        Leaf _backUpMessage = new("MessageTree//BackUp/SendMessage", new SendMessageToAlliesStrategy(blackboard, backUpMessage));
        
        Sequence _surroundSequence = new("MessageTree//Surround", ()=> agent.TreeValues.Messenger.SurroundWeight + (agent.TreeValues.Messenger.IsSurroundModified ? agent.TreeValues.Messenger.SurroundMod : 0));
        Leaf _surroundViable = new("MessageTree//Surround/ViableCheck", new ConditionStrategy(() =>
        {
            blackboard.TryGetValue(CommonKeys.VisibleEnemies, out List<GameObject> _visibleEnemies);
            blackboard.TryGetValue(CommonKeys.SurroundFlag, out bool surroundFlag);
            blackboard.TryGetValue(CommonKeys.PendingCoordination, out ComMessage pending);
            return blackboard.AlliesAvailable() && _visibleEnemies.Count > 0 && _visibleEnemies.Count < 2 && pending == null;
        }));
        Leaf _surroundCreate = new("MessageTree//Surround/SendMessage", new ActionStrategy(() =>
        {
            message = surroundMessage();
        }));
        Leaf _surroundMessage = new("MessageTree//Surround/SendMessage", new SendMessageToAlliesStrategy(blackboard, message));
        Leaf _surroundModified = new("MessageTree//Flank/FlankModify", new ActionStrategy(()=> agent.TreeValues.CombatTactic.IsSurroundModified = true));

        Leaf _messageReset = new("MessageTree/MessageReset", new ActionStrategy(() => message = null));
        Leaf _pendingMessage = new("MessageTree/Pending", new ActionStrategy(() =>
        {
            if (message != null) {
                blackboard.SetKeyValue(CommonKeys.PendingCoordination, message);
            }
            message = null;
        }));
        
        
        AddChild(_baseSequence);
        _baseSequence.AddChild(_messengerSelector);
        _baseSequence.AddChild(_alliesAvailable);
        _baseSequence.AddChild(_messengerSelector);
        _baseSequence.AddChild(_pendingMessage);
        
        _messengerSelector.AddChild(_flankSequence);
        _messengerSelector.AddChild(_groupUpSequence);
        _messengerSelector.AddChild(_retreatSequence);
        _messengerSelector.AddChild(_backUpSequence);
        _messengerSelector.AddChild(_surroundSequence);
        
        _flankSequence.AddChild(_flankViable);
        _flankSequence.AddChild(_flankCreate);
        _flankSequence.AddChild(_flankMessage);
        //_flankSequence.AddChild(_flankModified);
        
        _groupUpSequence.AddChild(_groupUpViable);
        _groupUpSequence.AddChild(_groupUpMessage);
        //_groupUpSequence.AddChild(_groupUpModified);
        
        _retreatSequence.AddChild(_retreatViable);
        _retreatSequence.AddChild(_retreatMessage);
        //_retreatSequence.AddChild(_retreatModified);
        
        _backUpSequence.AddChild(_backUpViable);
        _backUpSequence.AddChild(_backUpMessage);
        
        _surroundSequence.AddChild(_surroundViable);
        _surroundSequence.AddChild(_surroundCreate);
        _surroundSequence.AddChild(_surroundMessage);
        //_surroundSequence.AddChild(_surroundModified);
    }

    private GameObject targetAlly() {
        if(!blackboard.TryGetValue(CommonKeys.TargetAlly, out GameObject ally) || !ally) {
            return null;
        }
        return ally;
    }
    private ComMessage flankMessage() {
        if (!blackboard.TryGetValue(CommonKeys.TargetAlly, out GameObject _ally) || !_ally) return null;
        if(!_ally.TryGetComponent(out EnemyController _allyAgent) || !_allyAgent) return null;
        if(!blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _enemy) || !_enemy) return null;
        
        Dictionary<MessageInfoType, object> _payload = new Dictionary<MessageInfoType, object>
        {
            { MessageInfoType.Enemy, _enemy },
            { MessageInfoType.Ally, agent },
            { MessageInfoType.DirectionVector, (_enemy.transform.position- agent.transform.position).normalized }
        };

        return new ComMessage(agent, _allyAgent, MessageType.Flank, _payload, Time.time);
    }

    private ComMessage groupUpMessage() {
        if (!blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> _allies) || _allies.Count <= 0) return null;
        List<GameObject> _allyList = new List<GameObject>();
        foreach (GameObject _ally in _allies) {
            if(!_ally || !_ally.TryGetComponent(out Character _allyChar) || !_allyChar) continue;
            if(_allyChar.GetCharacterInfo().UnitType == UnitType.Healer) continue;
            _allyList.Add(_ally); 
        }
        _allyList.Add(agent.gameObject);
        Dictionary<MessageInfoType, object> _payload = new Dictionary<MessageInfoType, object>
        {
            { MessageInfoType.Allies, _allyList },
            { MessageInfoType.Position, agent.transform.position },
        };

        return new ComMessage(agent, null, MessageType.GroupUp, _payload, Time.time);
    }

    private ComMessage retreatMessage() {
        if (!blackboard.TryGetValue(CommonKeys.TargetAlly, out GameObject _ally) || !_ally) return null;
        if(!_ally.TryGetComponent(out EnemyController _allyAgent) || !_allyAgent) return null;
        if(!blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _enemy) || !_enemy) return null;
        
        Dictionary<MessageInfoType, object> _payload = new Dictionary<MessageInfoType, object>
        {
            { MessageInfoType.Position, _enemy.transform.position },
            { MessageInfoType.Distance, agent.TreeValues.Miscellaneous.RetreatDistance }
        };

        return new ComMessage(agent, _allyAgent, MessageType.Retreat, _payload, Time.time);
    }
    private ComMessage backUpMessage() {
        if (!blackboard.TryGetValue(CommonKeys.TargetAlly, out GameObject _ally) || !_ally) return null;
        if(!_ally.TryGetComponent(out EnemyController _allyAgent) || !_allyAgent) return null;
        if(!blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _enemy) || !_enemy) return null;
        
        Dictionary<MessageInfoType, object> _payload = new Dictionary<MessageInfoType, object>
        {
            { MessageInfoType.Enemy, _enemy }
        };

        return new ComMessage(agent, _allyAgent, MessageType.RequestBackup, _payload, Time.time);
    }

    private ComMessage surroundMessage() {
        if (!blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> _allies) || _allies.Count <=0) return null;
        if(!blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _enemy) || !_enemy) return null;
        List<GameObject> _allyList = new List<GameObject>();
        foreach (GameObject _ally in _allies) {
            if(!_ally || !_ally.TryGetComponent(out Character _allyChar) || !_allyChar) continue;
            if(_allyChar.GetCharacterInfo().UnitType == UnitType.Healer) continue;
            _allyList.Add(_ally); 
        }
        _allyList.Add(agent.gameObject);
        
        float _faceAngle = RadialHelper.CartesianToPol((_enemy.transform.position - agent.transform.position).normalized).y;
        
        Dictionary<MessageInfoType, object> _payload = new Dictionary<MessageInfoType, object>
        {
            { MessageInfoType.Enemy, _enemy },
            { MessageInfoType.Allies, _allyList }, 
            { MessageInfoType.DirectionAngle, _faceAngle },
            { MessageInfoType.Distance, agent.TreeValues.Miscellaneous.SurroundDistance }
        };

        return new ComMessage(agent, null, MessageType.SurroundTarget, _payload, Time.time);
    }
}
