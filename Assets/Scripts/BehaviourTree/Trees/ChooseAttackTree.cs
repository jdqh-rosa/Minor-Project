using UnityEngine;

public class ChooseAttackTree : BehaviourTree
{
    Blackboard blackboard;
    
    public ChooseAttackTree(Blackboard pBlackboard, int pPriority = 0) : base("ChooseAttack", pPriority)
    {
        blackboard = pBlackboard;

        setup();
    }

    private void setup()
    {
        Selector _baseSelector = new("AttackSelector");
        Sequence _stabSequence = new("StabSequence");
        Sequence _swingSequence = new("SwingSequence");
        Leaf _stabCheck = new Leaf("ChooseAttack//StabCheck", new ConditionStrategy(stabCheck)); 
        RandomSelector _stabSelector = new("ChooseAttack///JabSelector");
        RandomSelector _swingSelector = new("ChooseAttack///SwingSelector");
        Leaf _weakStab = new("ChooseAttack////WeakStab", new ActionStrategy(() => chooseAttack(ActionType.Jab)));
        Leaf _strongStab = new("ChooseAttack////StrongStab", new ActionStrategy(() => chooseAttack(ActionType.Thrust)));
        Leaf _weakSwing = new("ChooseAttack////WeakSwing", new ActionStrategy(() => chooseAttack(ActionType.Swipe)));
        Leaf _strongSwing = new("ChooseAttack////StrongSwing", new ActionStrategy(() => chooseAttack(ActionType.Swing)));

        AddChild(_baseSelector);
        _baseSelector.AddChild(_stabSequence);
        _baseSelector.AddChild(_swingSequence);
        _stabSequence.AddChild(_stabCheck);
        _stabSequence.AddChild(_stabSelector);
        _stabSelector.AddChild(_weakStab);
        _stabSelector.AddChild(_strongStab);
        _swingSequence.AddChild(_swingSelector);
        _swingSelector.AddChild(_weakSwing);
        _swingSelector.AddChild(_strongSwing);

    }

    private bool stabCheck()
    {
        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController _agent);
        blackboard.TryGetValue(CommonKeys.LinearAttackZone, out float _stabZone);
        blackboard.TryGetValue(CommonKeys.TargetEnemy, out GameObject _target);

        float diffAngle = RadialHelper.CartesianToPol((_target.transform.position - _agent.transform.position).normalized).y;
        
        float deltaAngle = Mathf.DeltaAngle(diffAngle, _stabZone);

        return Mathf.Abs(deltaAngle) <= _stabZone;
    }
    
    private void chooseAttack(ActionType attackType)
    {
        blackboard.SetKeyValue(CommonKeys.ChosenAttack, attackType);
    }
}
