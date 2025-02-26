using UnityEngine;

public class CharacterWeapon : MonoBehaviour
{
    [SerializeField] private GameObject hilt;
    [SerializeField] private GameObject blade;
    [SerializeField] private GameObject tip;

    public Vector2 Velocity;
    public float WeaponDistance = 1f;
    
    private float orbitalVelocity=0f;
    private Vector2 perpendicularVelocity;
    private float dampingFactor = 0.98f; // Smooths motion
    private float acceleration = 0.1f; // Acceleration factor for smooth control
    [SerializeField] private float maxOrbitalVelocity = 5f; // Limit max speed
    private float currentAngle = 0f;
    
    [SerializeField] private float mass =1f;
    [SerializeField] private float momentum = 0f;
    
    public void AddOrbitalVelocity(float addedMomentum) {
        orbitalVelocity += addedMomentum * acceleration;
        orbitalVelocity = Mathf.Clamp(orbitalVelocity, -maxOrbitalVelocity, maxOrbitalVelocity);
    }
    
    public float OrbitalAccelerate(float pAcceleration, float pTime) {
        float _angularAcceleration = pAcceleration / WeaponDistance;
        orbitalVelocity += _angularAcceleration * pTime;

        return orbitalVelocity;
    }
    
    public void AddPerpendicularVelocity(Vector2 addedMomentum) {
        perpendicularVelocity += addedMomentum * acceleration;
    }
    
    public void UpdateVelocity() {
        Velocity = new Vector2(
            -Mathf.Sin(currentAngle * Mathf.Deg2Rad),
            Mathf.Cos(currentAngle * Mathf.Deg2Rad)
        ) * (orbitalVelocity * WeaponDistance);
    }
    
    public void UpdatePosition() {
        
        //float newAngle = Mathf.MoveTowardsAngle(currentAngle, currentAngle + angularDisplacement, RotationSpeed * Time.deltaTime);
        currentAngle = RadialHelper.NormalizeAngle(currentAngle + (orbitalVelocity * Time.deltaTime));
        
        Vector2 orbitPos = RadialHelper.PolarToCart(currentAngle, WeaponDistance);
        
        transform.localPosition = new Vector3(orbitPos.x, transform.localPosition.y, orbitPos.y);
        transform.rotation = Quaternion.Euler(0, -currentAngle+90, 0);
        
        orbitalVelocity *= dampingFactor;
        if (Mathf.Abs(orbitalVelocity) < 0.01f) orbitalVelocity = 0;
        
        UpdateVelocity();
        CalculateAngularMomentum();
    }
    
    public void CalculateAngularMomentum() {
        momentum = Mathf.Abs(mass * orbitalVelocity * WeaponDistance);
    }
    
    public float GetAngle() {
        return currentAngle;
    }
}