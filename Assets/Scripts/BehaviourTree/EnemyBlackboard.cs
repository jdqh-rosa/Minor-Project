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
            { ActionType.Swing, pData.SwingState }
        };
        
        
        SetKeyValue(CommonKeys.LinearAttackZone, pData.LinearAttackZone);
        SetKeyValue(CommonKeys.RotationSpeed, pData.RotationSpeed);
        SetKeyValue(CommonKeys.MaxRotationSpeed, pData.MaxRotationSpeed);
        SetKeyValue(CommonKeys.Actions, _actionDictionary);
        
    }
    
    
}

public enum CommonKeys
{
    Error,
    Actions,
    AgentSelf,
    ChosenAction,
    ChosenAttack,
    ChosenFaceAngle,
    ChosenPosition,
    ChosenTarget, //
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
    VisibleAllies,
    VisibleEnemies,
    VisibleTargets,
    WeaponReach,
    
    
}