using System;
using UnityEngine;

public class CombatTree : BehaviourTree
{
    Blackboard blackboard;
    private EnemyController agent;
    private BlackboardKey agentKey;
    
    public CombatTree(Blackboard pBlackboard, int pPriority = 0) : base("Combat", pPriority)
    {
        blackboard = pBlackboard;
        
        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController agentValue);
        agent = agentValue;
        
        setup();
    }

    void setup()
    {
        Sequence _sequence = new Sequence("CombatBaseSeq");
        Leaf _findEnemiesAction = new Leaf("Combat/FindTargets", new FindEnemiesStrategy(blackboard));
        Leaf _obtainEnemy = new Leaf("Combat/ObtainTarget", new GetClosestEnemyStrategy(blackboard));
        Sequence _targetedSequence = new Sequence("Combat/TargetSeq");
        
        blackboard.TryGetValue(CommonKeys.SelfHealth, out int _charHealth);
        Leaf _healthCheck = new Leaf("Combat/TargetSeq/HealthCheck", new ConditionStrategy(() => _charHealth < 10));
        RandomSelector _randomSelector = new RandomSelector("Combat/TargetSeq/RandSel");
        Leaf _findAllies = new Leaf("Combat/TargetSeq/RandSel/FindAllies", new FindAlliesStrategy(blackboard));

        _sequence.AddChild(_findEnemiesAction);
        _sequence.AddChild(_obtainEnemy);
        _sequence.AddChild(_targetedSequence);
        _targetedSequence.AddChild(_healthCheck);
        _targetedSequence.AddChild(_randomSelector);
        _randomSelector.AddChild(new AttackTargetTree(blackboard, agent));
        //_randomSelector.AddChild(new DefendSelfTree(blackboard));
        _randomSelector.AddChild(_findAllies);

        AddChild(_sequence);
    }
}