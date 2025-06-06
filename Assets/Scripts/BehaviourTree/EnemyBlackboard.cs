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

    public float TimeSinceLastPatrol() {
        TryGetValue(CommonKeys.LastPatrolTime, out float result);
        return result;
    }

    public float PatrolCooldown() {
        TryGetValue(CommonKeys.PatrolCoolDown, out float result);
        return result;
    }

    public bool AlliesAvailable() {
        TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> _allies);
        return _allies.Count > 0;
    }
    
    public bool EnemiesAvailable() {
        TryGetValue(CommonKeys.VisibleEnemies, out List<GameObject> _enemies);
        return _enemies.Count > 0;
    }

    public float GetHealth() {
        TryGetValue(CommonKeys.SelfHealth, out float result);
        return result;
    }

    public void AddDirectionalForce(DirectionalForce pForce) {
        TryGetValue(CommonKeys.DirectionalForces, out List<DirectionalForce> _forces);
        _forces.Add(pForce);
        SetKeyValue(CommonKeys.DirectionalForces, _forces);
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
    ComProtocol,
    DetectedAttack,
    FindRadius,
    FlankAlly,
    FlankDirection,
    FlankTarget,
    GroupUpAllies,
    GroupUpPosition,
    KnownAllies,
    KnownEnemies,
    KnownTargets,
    LastAllyPosition,
    LastPatrolTime,
    LinearAttackZone,
    MaxRotationSpeed,
    MessageInbox,
    PatrolCoolDown,
    PatrolPoints,
    RotationSpeed,
    RetreatDistance,
    RetreatThreatPosition,
    SelfHealth,
    SurroundAllies,
    SurroundDirection,
    SurroundRadius,
    SurroundTarget,
    TargetAlly,
    TargetEnemy,
    TargetObject,
    TargetPosition,
    TeamSelf,
    VisibleAllies,
    VisibleEnemies,
    VisibleTargets,
    WeaponReach,
    
    DirectionalForces,
}

public enum TargetType
{
    None,
    Ally,
    Enemy,
    Object,
}

public struct DirectionalForce
{
    public Vector2 Direction;
    public float Force;
    public DirectionalForce(Vector2 pDirection, float pForce) {
        Direction = pDirection;
        Force = pForce;
    }
}