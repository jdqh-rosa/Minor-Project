using UnityEngine;

public class Character : MonoBehaviour
{
    public Rigidbody RigidBody;
    [SerializeField] private CharacterBody playerBody;
    
    [SerializeField] private CharacterWeapon playerWeapon;
    
    public float MoveSpeed = 5f;
    Vector2 moveDirection = Vector2.zero;
    Vector2 lookDirection;

    private Vector2 cumulVelocity;

    public void CumulativeVelocity() {
        cumulVelocity = playerBody.Velocity + playerWeapon.Velocity;
    }
    
    public void SetBodyRotation(float angle) {
        playerBody.transform.rotation = Quaternion.Euler(0, angle, 0);
    }

    public void SetCharacterPosition(Vector2 pos) {
        moveDirection = pos;
    }

    public void SetWeaponOrbit(float pAngle) {
        //playerWeapon.SetOrbitPosition(weaponDistance, pAngle);
    }

    public void AddWeaponOrbital(float pAdditionalMomentum) {
        playerWeapon.OrbitalAccelerate(pAdditionalMomentum, Time.deltaTime);
    }

    public void AddWeaponPerpendicular(Vector2 pAdditionalMomentum) {
        playerWeapon.AddPerpendicularVelocity(pAdditionalMomentum);
    }
    
    public void RotateWeaponTowardsAngle(float targetAngle, float rotationSpeed) {
        // Get the current rotation of the weapon (in degrees)
        float currentAngle = playerWeapon.transform.rotation.eulerAngles.y;

        // Calculate the shortest way to rotate towards the target angle
        float angleDiff = Mathf.DeltaAngle(currentAngle, targetAngle);

        // Apply a rotation force (or torque) to rotate the weapon
        // Ensure the rotation speed is applied gradually
        float step = rotationSpeed * Time.deltaTime;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, step);

        // Update the weapon's rotation based on the calculated angle
        playerWeapon.transform.rotation = Quaternion.Euler(0, newAngle, 0);
    }

    
    void FixedUpdate()
    {
        RigidBody.linearVelocity = new Vector3(moveDirection.x * MoveSpeed, 0, moveDirection.y * MoveSpeed);
        
        //playerWeapon.UpdateVelocity();
        
        playerWeapon.UpdatePosition();
    }


    public float GetWeaponAngle() {
        return playerWeapon.GetAngle();
    }

    public Vector3 GetWeaponPosition() {
        return playerWeapon.transform.position;
    }
    
}
