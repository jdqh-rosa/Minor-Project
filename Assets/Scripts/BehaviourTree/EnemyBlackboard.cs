using System.Collections.Generic;
using UnityEngine;

public class EnemyBlackboard : Blackboard
{
    private void Init() {
        SetKeyValue(CommonKeys.MessageInbox, new List<ComMessage>());
        SetKeyValue(CommonKeys.VisibleAllies, new List<GameObject>());
        SetKeyValue(CommonKeys.VisibleTargets, new List<GameObject>());
        SetKeyValue(CommonKeys.DirectionalForces, new List<DirectionalForce>());
        SetKeyValue(CommonKeys.FlankFlag, false);
        SetKeyValue(CommonKeys.GroupUpFlag, false);
        SetKeyValue(CommonKeys.SurroundFlag, false);
        SetKeyValue(CommonKeys.SurroundFlag, false);
    }
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
        SetKeyValue(CommonKeys.Actions, _actionDictionary);
        
        Init();
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
    
    public List<DirectionalForce> MovementForces = new();

    public void AddForce(Vector3 pDirection, float pStrength, string pName="")
    {
        MovementForces.Add(new DirectionalForce(pDirection.normalized, pStrength, pName));
    }

    public Vector3 GetBlendedDirection()
    {
        if (MovementForces.Count == 0) return Vector3.zero;

        Vector3 result = Vector3.zero;
        foreach (var force in MovementForces)
        {
            result += force.Direction * force.Force;
        }
        return result.normalized;
    }

    public void ClearForces()
    {
        MovementForces.Clear();
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
    FlankFlag,
    FlankAlly,
    FlankDirection,
    FlankTarget,
    GroupUpFlag,
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
    PatrolFlag,
    PatrolCoolDown,
    PatrolPoints,
    RotationSpeed,
    RetreatFlag,
    RetreatDistance,
    RetreatThreatPosition,
    SelfHealth,
    SurroundFlag,
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
    public string Name;
    public Vector3 Direction;
    public float Force;
    public DirectionalForce(Vector3 pDirection, float pForce, string pName="") {
        Direction = pDirection;
        Force = pForce;
        Name = pName;
    }
}