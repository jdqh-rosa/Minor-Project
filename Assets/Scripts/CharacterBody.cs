using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Composites;

public class CharacterBody : MonoBehaviour
{
    public Character Character;
    public Vector2 Velocity;
    private bool isStepping;
    private bool isWalking;

    [SerializeField] private float stepLength = 1f;
    [SerializeField] private float stepDuration = 0.3f;

    private Vector3 targetPosition;

    public void TurnToAngle(float angle) {
        transform.localRotation = Quaternion.Euler(0, angle, 0);
    }


    public void Move(Vector2 pDir) {
        if (pDir == Vector2.zero) return;
        
        if (isStepping) {
            targetPosition += new Vector3(pDir.x, 0, pDir.y)*stepLength;
            isWalking = true;
        }else
        {
            targetPosition = Character.transform.position + new Vector3(pDir.x, 0, pDir.y)*stepLength;
            StartCoroutine(step());
        }
    }

    private IEnumerator step()
    {
        isStepping = true;
        Vector3 _startPosition = Character.transform.position;
        float _elapsedTime = 0;

        while (_elapsedTime < stepDuration) {
            _elapsedTime += Time.deltaTime;
            float _t = _elapsedTime / stepDuration;
            float _easedT = EaseInOutQuad(_t);
            
            Character.transform.position = Vector3.Lerp(_startPosition, targetPosition, _easedT);
            yield return null;
        }
        
        Character.transform.position = targetPosition;
        isStepping = false;

        while (isWalking)
        {
            isWalking = false;
            StartCoroutine(step());
        }
    }

    private float EaseInOutQuad(float pT) {
        return (pT < 0.5f) ? (2 * pT * pT) : (1 - Mathf.Pow(-2 * pT+2, 2) / 2);
    }
}
