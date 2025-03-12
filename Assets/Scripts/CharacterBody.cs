using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Composites;

public class CharacterBody : MonoBehaviour
{
    public Character Character;

    public Vector2 Velocity;
    private Vector2 movementInput;
    private bool isStepping;
    private bool isWalking;

    [SerializeField] private float stepLength = 1f;
    [SerializeField] private float stepDuration = 0.3f;
    private float lastStepVelocity = 0f;
    private float walkBottomFactor = 0.4f;

    private Vector3 targetPosition;

    public void TurnToAngle(float angle) {
        transform.localRotation = Quaternion.Euler(0, angle, 0);
    }

    public void Move(Vector2 pDir) {
        if (pDir.sqrMagnitude < Mathf.Epsilon) {
            movementInput = Vector2.zero;
            return;
        }

        movementInput = pDir.normalized * stepLength;

        if (!isStepping) {
            StartCoroutine(step());
        }
    }
    
    private IEnumerator step() {
        Debug.Log($"Started Step", this);
        isStepping = true;

        Vector3 _startPosition = Character.transform.position;
        targetPosition = _startPosition + new Vector3(movementInput.x, 0, movementInput.y);

        float _elapsedTime = 0;
        while (_elapsedTime < stepDuration) {
            _elapsedTime += Time.deltaTime;
            float _t = _elapsedTime / stepDuration;
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
        lastStepVelocity = 0;//Mathf.Max(lastStepVelocity, stepLength * walkBottomFactor);
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
}