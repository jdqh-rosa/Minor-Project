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
    private bool isStepping;
    private bool isWalking;

    private float lastStepVelocity = 0f;

    private Vector3 targetPosition;

    public void TurnToAngle(float angle) {
        transform.localRotation = Quaternion.Euler(0, angle, 0);
    }

    float elapsedTime = 0;
    public Vector3 Move(Vector3 pDir) {
        if (pDir.sqrMagnitude < Mathf.Epsilon) {
            movementInput = Vector3.zero;
            return movementInput;
        }

        movementInput = pDir.normalized * data.StepLength;

        float _minFraction;
        
        if (!isStepping) {
            _minFraction = 0f;
            isStepping = true;
        }
        else {
            _minFraction = data.StepFraction;
        }
        
        elapsedTime += Time.deltaTime;
        float _t = elapsedTime / data.StepDuration;
        
        if (_t >= 1) {
            isStepping = false;
            elapsedTime = 0;
            return Vector3.zero;
        }
        
        float _sineWave = Mathf.Sin(_t * Mathf.PI * 2);
        float _normalizedMagnitude = (_sineWave + 1) * 0.5f;
        float _dynamicMagnitude = Mathf.Lerp(_minFraction * data.StepLength, data.StepLength, _normalizedMagnitude);
        
        Velocity = pDir.normalized * _dynamicMagnitude;
        
        return Velocity;
    }
    
    private IEnumerator step() {
        Debug.Log($"Started Step", this);
        isStepping = true;

        Vector3 _startPosition = Character.transform.position;
        targetPosition = _startPosition + new Vector3(movementInput.x, 0, movementInput.y);

        float _elapsedTime = 0;
        while (_elapsedTime < data.StepDuration) {
            _elapsedTime += Time.deltaTime;
            float _t = _elapsedTime / data.StepDuration;
            float _baseEasing = EaseInOutStep(_t);
            _baseEasing = Mathf.Max(_baseEasing, lastStepVelocity);
            float _easedT = _baseEasing;
            //_easedT = Mathf.Lerp(lastStepVelocity, 1f, _baseEasing);
            
            Character.transform.position = Vector3.Lerp(_startPosition, targetPosition, _easedT);
            Velocity = new Vector2(Character.transform.position.x, Character.transform.position.z) - new Vector2(_startPosition.x, _startPosition.z);
            
            Debug.Log($"elapsedTime: {_elapsedTime}, T: {_t}, E: {_easedT}, Velocity: {Velocity}");
            
            yield return null;
        }

        Character.transform.position = targetPosition;
        Debug.Log($"lastStepVelocity: {lastStepVelocity}");
        lastStepVelocity = 0;//Mathf.Max(lastStepVelocity, stepLength * data.StepFraction);
        Debug.Log($"lastStepVelocity: {lastStepVelocity}");

        if (movementInput.sqrMagnitude > Mathf.Epsilon) {
            isStepping = false;
        }
        else {
            isStepping = false;
            lastStepVelocity = 0;
            Velocity = Vector2.zero;
        }
    }

    private float EaseInOutStep(float pT) {
        return (pT < 0.5f) ? (2 * pT * pT) : (1 - Mathf.Pow(-2 * pT + 2, 2) / 2);
    }

    public float GetStepLength()
    {
        return data.StepLength;
    }
}