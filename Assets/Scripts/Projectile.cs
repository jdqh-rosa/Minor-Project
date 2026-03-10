using System;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Vector3 _direction;
    protected float speed = 10f;
    protected float damage = 10f;
    
    private void Start() {
        Destroy(this.gameObject, 5f);
    }

    void Update() {
        transform.position += _direction * (Time.deltaTime * speed);
        //transform.Translate(transform.forward * (Time.deltaTime * speed), Space.World);
    }
    
    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("WeaponPart")) {
            Destroy(gameObject);
        }
        if (other.CompareTag("CharacterBody")) {
            other.GetComponent<CharacterBody>().Character.TakeDamage(damage);
            Destroy(gameObject);
        }
    }

    public void Setup(float pAmount, float pSpeed, Vector3 pDirection) {
        damage = pAmount;
        speed = pSpeed;
        _direction = pDirection.normalized;
        transform.rotation = Quaternion.LookRotation(_direction);
    }
}
