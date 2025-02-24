using UnityEngine;

public class CharacterBody : MonoBehaviour
{
    public Vector2 Velocity;

    public void TurnToAngle(float angle) {
        transform.localRotation = Quaternion.Euler(0, angle, 0);
    }
}
