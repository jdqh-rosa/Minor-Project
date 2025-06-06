using UnityEngine;

public class MessengerTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    public MessengerTree(EnemyBlackboard pBlackboard, int pPriority = 0) : base("MessageTree", pPriority) {
        blackboard = pBlackboard;
        
        setup();
    }

    private void setup() {
        Sequence _baseSequence = new ("MessageTreeBase");
        Leaf _alliesAvailable = new("MessageTree/AlliesAvailable", new ConditionStrategy(blackboard.AlliesAvailable));
        PrioritySelector _messengerSelector = new("MessageTree/MessengerSelector");
        
        
        Sequence _flankSequence = new("MessageTree//Flank");
        //Leaf _flankViable = new("MessageTree//Flank/ViableCheck", new ConditionStrategy());
        //Leaf _flankMessage = new("MessageTree//Flank/SendMessage", new SendMessageToAllyStrategy());
        
        Sequence _groupUpSequence = new("MessageTree//GroupUp");
        //Leaf _groupUpViable = new("MessageTree//GroupUp/ViableCheck", new ConditionStrategy());
        //Leaf _groupUpMessage = new("MessageTree//GroupUp/SendMessage", new SendMessageToAllyStrategy());
        
        Sequence _retreatSequence = new("MessageTree//Retreat");
        //Leaf _retreatViable = new("MessageTree//Retreat/ViableCheck", new ConditionStrategy());
        //Leaf _retreatMessage = new("MessageTree//Retreat/SendMessage", new SendMessageToAllyStrategy());
        
        Sequence _backUpSequence = new("MessageTree//BackUp");
        //Leaf _backUpViable = new("MessageTree//BackUp/ViableCheck", new ConditionStrategy());
        //Leaf _backUpMessage = new("MessageTree//BackUp/SendMessage", new SendMessageToAllyStrategy());
        
        Sequence _surroundSequence = new("MessageTree//Surround");
        //Leaf _surroundViable = new("MessageTree//Surround/ViableCheck", new ConditionStrategy());
        //Leaf _surroundMessage = new("MessageTree//Surround/SendMessage", new SendMessageToAllyStrategy());
        
        
        AddChild(_baseSequence);
        _baseSequence.AddChild(_alliesAvailable);
        _baseSequence.AddChild(_messengerSelector);
        
        _messengerSelector.AddChild(_flankSequence);
        _messengerSelector.AddChild(_groupUpSequence);
        _messengerSelector.AddChild(_retreatSequence);
        _messengerSelector.AddChild(_backUpSequence);
        _messengerSelector.AddChild(_surroundSequence);
        
        //_flankSequence.AddChild(_flankViable);
        //_flankSequence.AddChild(_flankMessage);
        
        // _groupUpSequence.AddChild(_groupUpViable);
        // _groupUpSequence.AddChild(_groupUpMessage);
        
        // _retreatSequence.AddChild(_retreatViable);
        // _retreatSequence.AddChild(_retreatMessage);
        
        // _backUpSequence.AddChild(_backUpViable);
        // _backUpSequence.AddChild(_backUpMessage);
        
        // _surroundSequence.AddChild(_surroundViable);
        // _surroundSequence.AddChild(_surroundMessage);
    }
}
