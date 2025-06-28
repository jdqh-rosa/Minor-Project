using UnityEngine;

public class ChooseAttackTree : BehaviourTree
{
    Blackboard blackboard;
    private EnemyController agent;
    
    public ChooseAttackTree(Blackboard pBlackboard, int pPriority = 0) : base("ChooseAttack", pPriority)
    {
        blackboard = pBlackboard;
        blackboard.TryGetValue(CommonKeys.AgentSelf, out agent);
        setup();
    }

    private void setup()
    {
        PrioritySelector _baseSelector = new("AttackSelector");
        Sequence _stabSequence = new("StabSequence", ()=> agent.TreeValues.CombatAttack.StabWeight);
        Sequence _swingSequence = new("SwingSequence", ()=> agent.TreeValues.CombatAttack.SwingWeight);
        Leaf _stabCheck = new Leaf("ChooseAttack//StabCheck", new ConditionStrategy(stabCheck)); 
        RandomSelector _stabSelector = new("ChooseAttack///JabSelector");
        RandomSelector _swingSelector = new("ChooseAttack///SwingSelector");
        Leaf _weakStab = new("ChooseAttack////WeakStab", new ActionStrategy(() => chooseAttack(ActionType.Jab)), ()=> agent.TreeValues.CombatAttack.WeakStabWeight);
        Leaf _strongStab = new("ChooseAttack////StrongStab", new ActionStrategy(() => chooseAttack(ActionType.Thrust)), ()=> agent.TreeValues.CombatAttack.StrongStabWeight);
        Leaf _weakSwing = new("ChooseAttack////WeakSwing", new ActionStrategy(() => chooseAttack(ActionType.Swipe)), ()=> agent.TreeValues.CombatAttack.WeakSwingWeight);
        Leaf _strongSwing = new("ChooseAttack////StrongSwing", new ActionStrategy(() => chooseAttack(ActionType.Swing)), ()=> agent.TreeValues.CombatAttack.StrongSwingWeight);

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
        if (!_target) return false;
            
        float diffAngle = RadialHelper.CartesianToPol((_target.transform.position - _agent.transform.position).normalized).y;
        
        float deltaAngle = Mathf.DeltaAngle(diffAngle, _stabZone);

        return Mathf.Abs(deltaAngle) <= _stabZone;
    }
    
    private void chooseAttack(ActionType attackType)
    {
        blackboard.SetKeyValue(CommonKeys.ChosenAttack, attackType);
    }
}
