using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Composites;

public class CharacterBody : MonoBehaviour
{
    public Character Character;
    [SerializeField] private BodyData data;

    public Vector3 Velocity;
    private Vector3 movementInput;
    private bool isMovementSpecial=false;
    private bool isStepping;
    private bool isWalking;

    private Vector3 targetPosition;
    float elapsedTime = 0;

    public void TurnToAngle(float pAngle) {
        transform.localRotation = Quaternion.Euler(0, pAngle, 0);
    }

    public Vector3 Step(Vector3 pDir) {
        return isMovementSpecial ? Vector3.zero : Move(pDir, data.StepLength, data.StepDuration);
    }
    
    public Vector3 Stride(Vector3 pDir, float pStepLength, float pStepDuration, float pElapsedTime) {
        return Move(pDir, pStepLength, pStepDuration, pElapsedTime);
    }

    public Vector3 Dodge(Vector3 pDir, float pStepLength, float pStepDuration, float pElapsedTime) {
        return Move(pDir, pStepLength, pStepDuration, pElapsedTime);
    }
    
    private Vector3 Move(Vector3 pDir, float pStepLength, float pStepDuration, float pTime = -1) {
        
        if (pDir.sqrMagnitude < Mathf.Epsilon) {
            return Vector3.zero;
        }
        
        float _minFraction;
        if (!isStepping) {
            _minFraction = 0f;
            isStepping = true;
        }
        else {
            _minFraction = data.StepFraction;
        }
        
        elapsedTime += Time.deltaTime;
        if(pTime>=0) {
            elapsedTime = pTime;
        }
        
        float _t = elapsedTime / pStepDuration;
        
        if (_t >= 1) {
            isStepping = false;
            elapsedTime = 0;
            return Vector3.zero;
        }
        
        float _sineWave = Mathf.Sin(_t * Mathf.PI * 2);
        float _normalizedMagnitude = (_sineWave + 1) * 0.5f;
        float _dynamicMagnitude = Mathf.Lerp(_minFraction * pStepLength, pStepLength, _normalizedMagnitude);
        
        Velocity = pDir.normalized * _dynamicMagnitude;
        return Velocity;
    }
    
    public float GetStepLength()
    {
        return data.StepLength;
    }

    public void SetSpecialMovement(bool pSpecial) {
        isMovementSpecial = pSpecial;
    }
}