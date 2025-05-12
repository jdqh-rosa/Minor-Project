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
        RandomSelector _baseSelector = new("AttackSelector");
        RandomSelector _stabSelector = new("JabSelector");
        RandomSelector _swingSelector = new("SwingSelector");
        Leaf _weakStab = new("ChooseAttack////WeakStab", new ActionStrategy(() => chooseAttack(ActionType.Jab)));
        Leaf _strongStab = new("ChooseAttack////StrongStab", new ActionStrategy(() => chooseAttack(ActionType.Thrust)));
        Leaf _weakSwing = new("ChooseAttack////WeakSwing", new ActionStrategy(() => chooseAttack(ActionType.Swipe)));
        Leaf _strongSwing = new("ChooseAttack////StrongSwing", new ActionStrategy(() => chooseAttack(ActionType.Swing)));

        AddChild(_baseSelector);
        _baseSelector.AddChild(_stabSelector);
        _baseSelector.AddChild(_swingSelector);
        _stabSelector.AddChild(_weakStab);
        _stabSelector.AddChild(_strongStab);
        _swingSelector.AddChild(_weakSwing);
        _swingSelector.AddChild(_strongSwing);

    }
    
    private void chooseAttack(ActionType attackType)
    {
        blackboard.SetKeyValue(CommonKeys.ChosenAttack, attackType);
    }
}
