using UnityEngine;

public class CheckerTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    private EnemyController agent;
    public CheckerTree(EnemyBlackboard pBlackboard, int pPriority = 0) : base("CheckerTree", pPriority) {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        setup();
    }

    private void setup() {
        Parallel _baseParallel = new("Checker/Base", 1);
        
        Parallel _characterParallel = new("Checker/CharacterCheck", 1);
        
        Sequence _enemySequence = new("Checker//EnemyCheckerSeq");
        Leaf _findEnemies = new("Checker///FindEnemies", new FindEnemiesStrategy(blackboard));
        Leaf _enemiesAvailable = new("Checker//EnemiesAvailable", new ConditionStrategy(() => blackboard.EnemiesAvailable()));
        Leaf _getClosestEnemy = new("Checker//GetClosestEnemy", new GetClosestEnemyStrategy(blackboard));
        
        Sequence _allySequence = new("Checker//AllyCheckerSeq");
        Leaf _findAllies = new("Checker///FindEnemies", new FindAlliesStrategy(blackboard));
        Leaf _alliesAvailable = new("Checker//AlliesAvailable", new ConditionStrategy(() => blackboard.AlliesAvailable()));
        Leaf _getClosestAlly = new("Checker//GetClosestAlly", new GetClosestAllyStrategy(blackboard));
        
        Parallel _selfCheckParallel = new("Checker//SelfChecks", 1);
        
        //Leaf _patrolTimeCheck = new("Checker//PatrolTimeCheck", new ConditionStrategy());
        Sequence _healthCheckSequence = new("Checker//SelfChecksSeq");
        Leaf _healthCheck = new("Checker//HealthCheck", new ConditionStrategy(()=> blackboard.CheckLowHealth()));
        Leaf _healthModify = new ("Checker//HealthModify", new ActionStrategy(()=> agent.TreeValues.CombatTactic.IsRetreatModified = true));
        
        Leaf _checkMessages = new("Checker//MessageCheck", new ProcessMessagesStrategy(blackboard, 5));
        Leaf _detectAttacks = new("Checker//DetectAttack", new DetectAttackStrategy(blackboard));
        
        AddChild(_baseParallel);
        _baseParallel.AddChild(_characterParallel);
        _baseParallel.AddChild(_selfCheckParallel);
        
        _characterParallel.AddChild(_enemySequence);
        _characterParallel.AddChild(_allySequence);
        
        _enemySequence.AddChild(_findEnemies);
        _enemySequence.AddChild(_enemiesAvailable);
        _enemySequence.AddChild(_getClosestEnemy);
        
        _allySequence.AddChild(_findAllies);
        _allySequence.AddChild(_alliesAvailable);
        _allySequence.AddChild(_getClosestAlly);
        
        //_selfCheckParallel.AddChild(_patrolTimeCheck);
        _selfCheckParallel.AddChild(_healthCheckSequence);
        _healthCheckSequence.AddChild(_healthCheck);
        _healthCheckSequence.AddChild(_healthModify);
        _selfCheckParallel.AddChild(_healthCheck);
        _selfCheckParallel.AddChild(_checkMessages);
        _selfCheckParallel.AddChild(_detectAttacks);
        
        
    }
}
