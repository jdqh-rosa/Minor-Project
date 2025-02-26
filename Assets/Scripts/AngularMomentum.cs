using System;
using UnityEngine;

public class AngularMomentum : MonoBehaviour
{
    [SerializeField] private float mass =1f;
    [SerializeField] private float radius =1f;
    [SerializeField] private float velocity;
    [SerializeField] private float angularDisplacement=0f;
    [SerializeField] private float currentAngle = 0f;
    [SerializeField] private float momentum = 0f;
    [SerializeField] private float dampingFactor =0.9f;
    
    public void CalculateAngularMomentum() {
        momentum = mass * velocity * radius;
    }
    
    public void CalculateAngularDisplacement(float pVelocity, float pTime) {
        angularDisplacement = (pVelocity / radius) * pTime;
        angularDisplacement *= Mathf.Rad2Deg;
        CalcVelocityFromAngularDisplacement(angularDisplacement, pTime);
    }
    
    public void CalcVelocityFromAngularDisplacement(float pAngle, float pTime) {
        velocity= (pAngle / pTime) * radius;
    }
    
    public float Accelerate(float acceleration, float time) {
        float _angularAcceleration = acceleration / radius;
        float _angularVelocity = velocity / radius;
        
        float _angle = _angularVelocity * time + 0.5f * _angularAcceleration * time * time;
        
        // Optional: Update angular velocity after acceleration
        float _newAngularVelocity = _angularVelocity + _angularAcceleration * time;
        velocity = _newAngularVelocity * radius; // Convert back to linear velocity
        
        angularDisplacement = _angle;
        
        return _angle; // Return the angle moved
    }

    public void SetOrbit() {
        //currentAngle = Mathf.MoveTowardsAngle(currentAngle, currentAngle + angularDisplacement, angularDisplacement*Time.deltaTime);
        currentAngle = RadialHelper.NormalizeAngle(currentAngle + angularDisplacement);
        
        Vector2 orbitPos = RadialHelper.PolarToCart(currentAngle, radius);
        
        transform.position = new Vector3(orbitPos.x, transform.position.y, orbitPos.y);
        transform.rotation = Quaternion.Euler(0, -currentAngle+90, 0);
        
        velocity *= dampingFactor;
        if (Mathf.Abs(velocity) < 0.01f) velocity = 0;
    }

    public float GetAngle() {
        return currentAngle;
    }
}
