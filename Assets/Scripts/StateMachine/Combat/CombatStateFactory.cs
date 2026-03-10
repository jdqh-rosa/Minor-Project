using System;

public class CombatStateFactory
{
    public static CombatState Create(ActionType actionType) {
        return actionType switch
        {
            ActionType.Jab => new JabState(),
            ActionType.Thrust => new ThrustState(),
            ActionType.Swipe => new SwipeState(),
            ActionType.Swing => new SwingState(),
            ActionType.Stride => new StrideState(),
            ActionType.Dodge => new DodgeState(),
            ActionType.Heal => new HealState(),
            ActionType.Arrow => new ArrowState(),
            ActionType.StrongArrow => new StrongArrowState(),
            _ => throw new ArgumentOutOfRangeException(nameof(actionType), actionType, null)
        };
    }
}