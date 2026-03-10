using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyBlackboard : Blackboard
{
    public List<DirectionalForce> MovementForces = new();
    private void Init() {
        SetKeyValue(CommonKeys.MessageInbox, new List<ComMessage>());
        SetKeyValue(CommonKeys.VisibleAllies, new List<GameObject>());
        SetKeyValue(CommonKeys.VisibleTargets, new List<GameObject>());
        SetKeyValue(CommonKeys.DirectionalForces, new List<DirectionalForce>());
        SetKeyValue(CommonKeys.FlankFlag, false);
        SetKeyValue(CommonKeys.GroupUpFlag, false);
        SetKeyValue(CommonKeys.SurroundFlag, false);
        SetKeyValue(CommonKeys.RetreatFlag, false);
    }
    public void AddCharacterData(CharacterData pData)
    {
        SetKeyValue(CommonKeys.LinearAttackZone, pData.LinearAttackZone);
        SetKeyValue(CommonKeys.RotationSpeed, pData.RotationSpeed);
        SetKeyValue(CommonKeys.MaxRotationSpeed, pData.MaxRotationSpeed);
        
        Dictionary<ActionType, CombatStateData> _actionDictionary = new(){
            { ActionType.Stride, pData.StrideState},
            //{ ActionType.Dodge , pData.DodgeState },
        };
        SetKeyValue(CommonKeys.MovementActions, _actionDictionary);
        
        Init();
    }

    public void AddWeaponData(WeaponData pData) {
        
        Dictionary<ActionType, CombatStateData> _actionDictionary = new(){};
        foreach (AttackInputEntry attackInput in pData.AttackInputMap) {
            _actionDictionary.Add(attackInput.ActionData.ActionType, attackInput.ActionData);
        }
        
        SetKeyValue(CommonKeys.AttackActions, _actionDictionary);
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

    public float GetCurrentHealth() {
        TryGetValue(CommonKeys.AgentSelf, out EnemyController _agent);
        return _agent.GetCurrentHealth();
    }

    public bool CheckLowHealth() {
        TryGetValue(CommonKeys.MaxHealth, out float _maxHealth);
        TryGetValue(CommonKeys.AgentSelf, out EnemyController _agent);
        return GetCurrentHealth() <= _maxHealth * _agent.TreeValues.Health.LowHealthThreshold;
    }
    
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
            //UnityEngine.Debug.Log($"{force.Name}, {force.Direction}, {force.Force}");
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
    Error =0,
    AttackActions,
    AttackTolerance,
    MovementActions,
    ActiveTarget,
    AgentSelf,
    ChosenAction,
    ChosenAttack,
    ChosenFaceAngle,
    ChosenPosition,
    ChosenWeaponAngle,
    ComProtocol,
    DetectedAttack,
    DirectionalForces,
    FindRadius,
    FlankFlag,
    FlankAlly,
    FlankDirection,
    FlankTarget,
    GroupUpFlag,
    GroupUpAllies,
    GroupUpPosition,
    IncomingCoordination,
    KnownAllies,
    KnownEnemies,
    KnownTargets,
    LastAllyPosition,
    LastPatrolTime,
    LinearAttackZone,
    LowestHealthAlly,
    MaxHealth,
    MaxRotationSpeed,
    MessageInbox,
    PatrolFlag,
    PatrolCoolDown,
    PatrolPoints,
    PendingCoordination,
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
    UnitType,
    VisibleAllies,
    VisibleEnemies,
    VisibleTargets,
    VisibleWeapons,
}

public enum TargetType
{
    None,
    Ally,
    Enemy,
    Object,
    Position,
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