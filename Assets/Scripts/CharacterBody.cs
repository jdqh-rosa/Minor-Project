using System.Collections;
using UnityEngine;

public class CharacterBody : MonoBehaviour
{
    public Character Character;
    public Vector2 Velocity;
    private bool isStepping;
    private bool isWalking;

    [SerializeField] private float moveSpeed =10f;
    private float stepLength = 1;

    public void TurnToAngle(float angle) {
        transform.localRotation = Quaternion.Euler(0, angle, 0);
    }

    public void Move(Vector2 dir)
    {
        if (isStepping)
        {
            isWalking = true;
            return;
        }
        
        if (isStepping || !isStepping && isWalking)
        {
            StartCoroutine(Step());
        }
    }

    IEnumerator Step()
    {
        isStepping = true;

        while (false)
        {
            yield return new WaitForSeconds(0.1f);
        }

        isStepping = false;
    }
}
