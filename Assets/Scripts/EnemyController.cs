using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] BlackboardData blackboardData;
    
    private BehaviourTree tree;
    readonly Blackboard blackboard = new Blackboard();

    private void Awake() {
        
        blackboardData.SetValuesOnBlackboard(blackboard);
        
        BlackboardKey allies = blackboard.GetOrRegisterKey("Allies");
        blackboard.SetValue(allies, new List<GameObject>());
        BlackboardKey targets = blackboard.GetOrRegisterKey("Targets");
        blackboard.SetValue(targets, new List<GameObject>());
        
        tree = new BehaviourTree("Enemy");

        Repeater _repeater = new Repeater("BaseLogic");
        PrioritySelector _prioritySelector = new PrioritySelector("BaseLogic");
        _repeater.AddChild(_prioritySelector);
        
        
        _prioritySelector.AddChild(SetupIdleTree());
        _prioritySelector.AddChild(SetupCombatTree());
        
        tree.AddChild(_repeater);
        tree.Reset();
    }

    private void Update() {
        tree.Process();
    }


    private BehaviourTree SetupIdleTree() {
        BehaviourTree _idleTree = new BehaviourTree("Idle");
        
        Inverter _inverter = new Inverter("IdleBaseInv");
        Parallel _idleParallelNode = new Parallel("IdleBaseParallel", 2);
        
        Parallel _idleParallelAction = new Parallel("IdleParallelAction",2);
        Leaf _findTargetsAction = new Leaf("IdleParallelAction/FindTargets", new FindTargetsStrategy(blackboard, this.transform, 10f, "Character"));
        Leaf _findAlliesAction = new Leaf("IdleParallelAction/FindAllies", new FindTargetsStrategy(blackboard, this.transform, 10f, "Enemy"));
        _idleParallelNode.AddChild(_findTargetsAction);
        _idleParallelNode.AddChild(_findAlliesAction);
        
        PrioritySelector _IdleParallelCheck = new PrioritySelector("IdleParallelCheck");
        
        
        
        _idleParallelNode.AddChild(_idleParallelAction);
        _idleParallelNode.AddChild(_IdleParallelCheck);
        _inverter.AddChild(_idleParallelNode);
        
        _idleTree.AddChild(_inverter);
        
        return _idleTree;
    }

    private BehaviourTree SetupCombatTree() {
        BehaviourTree _combatTree = new BehaviourTree("Combat");
        Repeater _repeater = new Repeater("CombatBase");
        Sequence _sequence = new Sequence("CombatBaseSeq");
        Leaf _findTargetsAction = new Leaf("Combat/FindTargets", new FindTargetsStrategy(blackboard, this.transform, 10f, "Character"));
        Leaf _obtainTarget = new Leaf("Combat/ObtainTarget", new ObtainTargetStrategy(blackboard));
        Sequence _targetedSequence = new Sequence("Combat/TargetSeq");
        //Leaf _healthCheck = new Leaf("Combat/TargetSeq/HealthCheck", new ConditionStrategy(blackboard.TryGetValue()));
        RandomSelector _randomSelector = new RandomSelector("Combat/TargetSeq/RandSel");
        //BehaviourTree _attackTarget = new BehaviourTree("Combat/TargetSeq/RandSel/AtkTarget");
        //BehaviourTree _defendSelf = new BehaviourTree("Combat/TargetSeq/RandSel/DefTarget");
        Leaf _findAllies = new Leaf("Combat/TargetSeq/RandSel/FindAllies", new FindTargetsStrategy(blackboard, this.transform, 10f, "Enemy"));
        
        _repeater.AddChild(_sequence);
        _sequence.AddChild(_findTargetsAction);
        _sequence.AddChild(_obtainTarget);
        _sequence.AddChild(_targetedSequence);
        //_targetedSequence.AddChild(_healthCheck);
        _targetedSequence.AddChild(_randomSelector);
        //_randomSelector.AddChild(_attackTarget);
        //_randomSelector.AddChild(_defendSelf);
        _randomSelector.AddChild(_findAllies);
        
        _combatTree.AddChild(_repeater);
        return _combatTree;
    }

    private BehaviourTree SetupAssembleTree() {
        BehaviourTree _assembleTree = new BehaviourTree("Assemble");
        
        Parallel _parallel = new Parallel("AssembleParallel", 2);
        
        Selector _selector = new Selector("Assemble//Selector");
        Sequence _sequence = new Sequence("Assemble//Sequence");
        //Leaf _allyCheck = new Leaf("Assemble//Selector/AllyCheck", new ConditionStrategy());
        Leaf _findAllies = new Leaf("Assemble//Selector/FindAllies", new FindTargetsStrategy(blackboard, this.transform, 10f, "Enemy"));
        //Leaf _allyPositionCheck = new Leaf("Assemble//Sequence/AllyPositionCheck", new ConditionStrategy());
        //BehaviourTree _enterAllyRange = new BehaviourTree("Assemble//Sequence/EnterRange", lastAllyPosition);
        
        
        _parallel.AddChild(_selector);
        _parallel.AddChild(_sequence);
        //_selector.AddChild(_allyCheck);
        _selector.AddChild(_findAllies);
        //_sequence.AddChild(_allyPositionCheck);
        //_sequence.AddChild(_enterAllyRange);
        
        _assembleTree.AddChild(_parallel);
        return _assembleTree;
    }

}
