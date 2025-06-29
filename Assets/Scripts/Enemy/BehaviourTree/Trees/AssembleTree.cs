public class AssembleTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    private EnemyController agent;
    
    public AssembleTree(EnemyBlackboard pBlackboard, int pPriority = 0) : base("Assemble", pPriority)
    {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        setup();
    }
    
    private void setup()
    {
        Parallel _baseParallel = new Parallel("AssembleBaseParallel", 2,1);
        
        Leaf _setTargetAlly = new ("Assemble//SetTargetAlly", new SetTargetAllyStrategy(blackboard));
        
        Sequence _groupBranch = new("Assemble//GroupBranch");
        Selector _selector = new("Assemble///GroupUp/MessageCheck");
        Leaf _groupMessageCheck = new("Assemble////GroupUp/GroupCheck", new CheckMessageStrategy(blackboard, MessageType.GroupUp));
        Leaf _modifyGroupMessageWeight = new("Assemble////GroupUp/MessageWeight", new ActionStrategy(()=> agent.TreeValues.Messenger.IsGroupUpModified=true));
        Leaf _groupUpAction = new("Assemble////GroupUp/GroupUpAction", new GroupUpStrategy(blackboard));
        
        AddChild(_baseParallel);
        _baseParallel.AddChild(_setTargetAlly);
        _baseParallel.AddChild(_groupBranch);
        
        _groupBranch.AddChild(_selector);
        _selector.AddChild(_groupMessageCheck);
        _selector.AddChild(_modifyGroupMessageWeight);
        _groupBranch.AddChild(_groupUpAction);
    }
}
