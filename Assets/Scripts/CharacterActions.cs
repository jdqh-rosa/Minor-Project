using System;
using System.Collections.Generic;
using UnityEngine;

public struct CharacterActions
{
    Blackboard blackboard;
    
    public void CalculatePreferredPosition()
    {
        //todo: actual calculations?
        blackboard.TryGetValue(CommonKeys.TargetPosition, out Vector3 targetPos);
        blackboard.SetKeyValue(CommonKeys.ChosenPosition, targetPos);
    }
    
    public void CalculatePreferredWeaponAngle()
    {
        blackboard.TryGetValue(CommonKeys.ChosenFaceAngle, out Vector3 targetPos);
        blackboard.SetKeyValue(CommonKeys.ChosenPosition, targetPos);
    }

    public void ChooseAlly()
    {
        blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> allies);
    }

    public void ChooseTarget()
    {
        
    }

    public void ContactAlly(GameObject pAlly)
    {
        
    }

    public void ContactAllies()
    {
        blackboard.TryGetValue(CommonKeys.VisibleAllies, out List<GameObject> allies);
        foreach (var ally in allies)
        {
            ContactAlly(ally);
        }
        
    }

    public void DetectAttack()
    {
        
    }

    public void DistanceSelf(Vector3 pAvoidPosition)
    {
        blackboard.TryGetValue(CommonKeys.AgentSelf, out EnemyController agent);
        Vector3 diffVec = pAvoidPosition - agent.transform.position;
        diffVec *= -1;
        blackboard.SetKeyValue(CommonKeys.ChosenPosition, diffVec);
    }

    public void ExecuteAction()
    {
        
    }

    public void ExecuteAttack()
    {
        
    }

    public void FaceTarget()
    {
        
    }

    public void FindAllies()
    {
        
    }

    public void FindTargets()
    {
        
    }
    
    public void MessageAlly(){}
    
    public void ObtainTarget(){}

    public void SetDesiredFaceAngle(float pAngle)
    {
        blackboard.SetKeyValue(CommonKeys.ChosenFaceAngle, pAngle);
    }

    public void SetDesiredPosition(Vector3 pPoint)
    {
        blackboard.SetKeyValue(CommonKeys.ChosenPosition, pPoint);
    }

    public void SetDesiredWeaponAngle(float pAngle)
    {
        blackboard.SetKeyValue(CommonKeys.ChosenWeaponAngle, pAngle);
    }

    public void SetTarget(GameObject pTarget)
    {
        blackboard.SetKeyValue(CommonKeys.ChosenTarget, pTarget);
    }

    public void SetTargets(List<GameObject> pTargets)
    {
        blackboard.SetKeyValue(CommonKeys.VisibleEnemies, pTargets);
    }
    
}
