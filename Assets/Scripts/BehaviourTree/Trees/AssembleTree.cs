public class AssembleTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    
    public AssembleTree(EnemyBlackboard pBlackboard, int pPriority = 0) : base("Assemble", pPriority)
    {
        blackboard = pBlackboard;
        
        setup();
    }
    
    private void setup()
    {
        Parallel _baseParallel = new Parallel("AssembleBaseParallel", 2,1);
        
        Leaf _setTargetAlly = new ("Assemble//SetTargetAlly", new SetTargetAllyStrategy(blackboard));
        
        Sequence _groupBranch = new("Assemble//GroupBranch");
        Leaf _groupMessageCheck = new("Assemble///GroupUp/GroupCheck", new CheckMessageStrategy(blackboard, MessageType.GroupUp));
        Leaf _groupUpAction = new("Assemble///GroupUp/GroupUpAction", new GroupUpStrategy(blackboard));
        
        AddChild(_baseParallel);
        _baseParallel.AddChild(_setTargetAlly);
        _baseParallel.AddChild(_groupBranch);
        _baseParallel.AddChild(new EnterRangeTree(blackboard, 2f));
        
        _groupBranch.AddChild(_groupMessageCheck);
        _groupBranch.AddChild(_groupUpAction);
    }
}
