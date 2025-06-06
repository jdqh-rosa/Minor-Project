using UnityEngine;

public class CheckerTree : BehaviourTree
{
    EnemyBlackboard blackboard;
    public CheckerTree(EnemyBlackboard pBlackboard, int pPriority = 0) : base("CheckerTree", pPriority) {
        blackboard = pBlackboard;
        
        setup();
    }

    private void setup() {
        Parallel _baseParallel = new("Checker/Base", 1);
        
        Parallel _characterParallel = new("Checker/CharacterCheck", 1);
        
        Sequence _enemySequence = new("Checker//EnemyCheckerSeq");
        Leaf _findEnemies = new("Checker///FindEnemies", new FindEnemiesStrategy(blackboard));
        Leaf _enemiesAvailable = new("Checker//EnemiesAvailable", new ConditionStrategy(blackboard.EnemiesAvailable));
        
        Sequence _allySequence = new("Checker//AllyCheckerSeq");
        Leaf _findAllies = new("Checker///FindEnemies", new FindAlliesStrategy(blackboard));
        Leaf _alliesAvailable = new("Checker//EnemiesAvailable", new ConditionStrategy(blackboard.AlliesAvailable));
        
        Parallel _selfCheckParallel = new("Checker//SelfChecks", 1);
        
        //Leaf _patrolTimeCheck = new("Checker//PatrolTimeCheck", new ConditionStrategy());
        //Leaf _healthCheck = new("Checker//HealthCheck", new ConditionStrategy(()=> blackboard.GetHealth()< 10f ));
        Leaf _checkMessages = new("Checker//MessageCheck", new ProcessMessagesStrategy(blackboard, 5));
        Leaf _detectAttacks = new("Checker//DetectAttack", new DetectAttackStrategy(blackboard));
        
        AddChild(_baseParallel);
        _baseParallel.AddChild(_characterParallel);
        _baseParallel.AddChild(_selfCheckParallel);
        
        _characterParallel.AddChild(_enemySequence);
        _characterParallel.AddChild(_allySequence);
        
        _enemySequence.AddChild(_findEnemies);
        _enemySequence.AddChild(_enemiesAvailable);
        
        _allySequence.AddChild(_findAllies);
        _allySequence.AddChild(_alliesAvailable);
        
        //_selfCheckParallel.AddChild(_patrolTimeCheck);
        //_selfCheckParallel.AddChild(_healthCheck);
        _selfCheckParallel.AddChild(_checkMessages);
        _selfCheckParallel.AddChild(_detectAttacks);
        
        
    }
}
