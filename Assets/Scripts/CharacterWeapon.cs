using UnityEngine;

public class CharacterWeapon : MonoBehaviour
{
    [SerializeField] private GameObject hilt;
    [SerializeField] private GameObject blade;
    [SerializeField] private GameObject tip;

    public Vector2 Velocity;
    public float RotationSpeed = 1f;
    public float WeaponDistance = 1f;
    
    private Vector2 orbitalVelocity;
    private Vector2 perpendicularVelocity;
    private float currentAngularVelocity = 0f;
    private float dampingFactor = 0.98f; // Smooths motion
    private float acceleration = 0.1f; // Acceleration factor for smooth control
    private float maxAngularVelocity = 5f; // Limit max speed
    private float currentAngle = 0f;
    
    public void AddOrbitalVelocity(float addedMomentum) {
        currentAngularVelocity += addedMomentum * acceleration;
        currentAngularVelocity = Mathf.Clamp(currentAngularVelocity, -maxAngularVelocity, maxAngularVelocity);
        currentAngle += currentAngularVelocity;
    }
    
    public void AddPerpendicularVelocity(Vector2 addedMomentum) {
        perpendicularVelocity += addedMomentum * acceleration;
    }
    
    public void UpdateVelocity() {
        // Compute new velocity from angular velocity
        Velocity = new Vector2(
            -Mathf.Sin(transform.eulerAngles.y * Mathf.Deg2Rad),
            Mathf.Cos(transform.eulerAngles.y * Mathf.Deg2Rad)
        ) * (currentAngularVelocity * WeaponDistance);
    }
    
    public void UpdatePosition() {
        // Compute new angle based on velocity
        float newAngle = transform.localEulerAngles.y + (currentAngularVelocity * Time.deltaTime);
        Debug.Log($"New Weapon angle: {newAngle}");

        // Convert angle to position
        Vector2 orbitPos = RadialHelper.PolarToCart(newAngle, WeaponDistance);
        transform.localPosition = new Vector3(orbitPos.x, transform.localPosition.y, orbitPos.y);
        
        // Rotate to face away from character
        transform.rotation = Quaternion.Euler(0, -newAngle, 0);
        
        // Apply frictional damping
        currentAngularVelocity *= dampingFactor;
        if (Mathf.Abs(currentAngularVelocity) < 0.01f) currentAngularVelocity = 0;
    }

    public float GetAngle() {
        return transform.eulerAngles.y;
    }

    public void CalculateAngularVelocity() {
        
    }
}