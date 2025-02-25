using System;
using UnityEngine;

public class AngularStuff : MonoBehaviour
{
    AngularMomentum angularMomentum;

    private void Start() {
        angularMomentum = GetComponent<AngularMomentum>();
    }

    private void Update() {
        //angularMomentum.CalculateAngularDisplacement(velocity, Time.deltaTime);
        //Accelerate(acceleration);
    }

    public void Accelerate(float pAcceleration) {
        angularMomentum.Accelerate(pAcceleration, Time.deltaTime);
        angularMomentum.CalculateAngularMomentum();
        angularMomentum.SetOrbit();
    }

    public float GetAngle() {
        return angularMomentum.GetAngle();
    }
}
