using System.Collections.Generic;
using UnityEngine;

public class EnemyBlackboard : Blackboard
{

    public void AddCharacterData(CharacterData pData)
    {
        Dictionary<ActionType, CombatStateData> _actionDictionary = new(){
            { ActionType.Jab, pData.JabState },
            { ActionType.Thrust, pData.ThrustState },
            { ActionType.Swipe, pData.SwipeState },
            { ActionType.Swing, pData.SwingState },
            { ActionType.Stride, pData.StrideState},
            { ActionType.Dodge , pData.DodgeState },
        };
        
        
        SetKeyValue(CommonKeys.LinearAttackZone, pData.LinearAttackZone);
        SetKeyValue(CommonKeys.RotationSpeed, pData.RotationSpeed);
        SetKeyValue(CommonKeys.MaxRotationSpeed, pData.MaxRotationSpeed);
        SetKeyValue(CommonKeys.TeamSelf, pData.CharacterTeam);
        SetKeyValue(CommonKeys.Actions, _actionDictionary);
        
    }

    public TargetType GetActiveTargetType() {
        TryGetValue(CommonKeys.ActiveTarget, out TargetType result);
        return result;
    }
}

public enum CommonKeys
{
    Error,
    Actions,
    ActiveTarget,
    AgentSelf,
    ChosenAction,
    ChosenAttack,
    ChosenFaceAngle,
    ChosenPosition,
    ChosenWeaponAngle,
    DetectedAttack,
    FindRadius,
    KnownAllies,
    KnownEnemies,
    KnownTargets,
    LastAllyPosition,
    LinearAttackZone,
    PatrolCoolDown,
    PatrolPoints,
    RotationSpeed,
    MaxRotationSpeed,
    SelfHealth,
    TargetAlly,
    TargetEnemy,
    TargetObject,
    TargetPosition,
    TeamSelf,
    VisibleAllies,
    VisibleEnemies,
    VisibleTargets,
    WeaponReach,
    
}

public enum TargetType
{
    None,
    Ally,
    Enemy,
    Object,
}