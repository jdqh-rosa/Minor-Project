using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

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

    public void AddOrbitalVelocity(float pAddedMomentum) {
        float angularVelocity = pAddedMomentum / WeaponDistance;  // Angular velocity based on momentum and distance
        Vector2 direction = new Vector2(-Mathf.Sin(transform.eulerAngles.y * Mathf.Deg2Rad), Mathf.Cos(transform.eulerAngles.y * Mathf.Deg2Rad)); 
        orbitalVelocity = direction * angularVelocity * WeaponDistance;
    }
    
    public void PreAddOrbitalVelocity(float pAddedMomentum) {
        float _angularVelocity = RotationSpeed;
        float _tangentialVelocity = _angularVelocity * WeaponDistance;
        
        Vector2 direction = new Vector2(-Mathf.Sin(transform.eulerAngles.y), Mathf.Cos(transform.eulerAngles.y));
        
        orbitalVelocity = direction.normalized * (_tangentialVelocity+pAddedMomentum);
    }

    public void AddPerpendicularVelocity(Vector2 pAddedMomentum) {
        perpendicularVelocity = pAddedMomentum;
    }

    public void UpdateVelocity() {
        Velocity = orbitalVelocity + perpendicularVelocity;
    }

    public void UpdatePositionWithVelocity() {
        float _velocityAngle = Mathf.Atan2(Velocity.y, Velocity.x) * Mathf.Rad2Deg;
        
        SetOrbitPosition(WeaponDistance, _velocityAngle);
    }
    
    public void SetOrbitPosition(float pDistance, float pAngle) {
        Vector2 _orbitPos = RadialHelper.PolarToCart(pAngle, pDistance);
        transform.localPosition = new Vector3(_orbitPos.x, transform.localPosition.y, _orbitPos.y);
        transform.rotation = Quaternion.Euler(0, -pAngle, 0);
    }
}