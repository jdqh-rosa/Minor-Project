using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public GameInput PlayerControls;

    private InputAction move;
    private InputAction look;
    private InputAction attack;

    [SerializeField] private Character player;

    [SerializeField] private float rotationSpeed = 10f;
    private Vector2 lastLookDirection = Vector2.zero; // Stores last used input direction
    private float targetAngle;
    private bool isUsingMouse = false;

    Vector2 moveDirection = Vector2.zero;

    [SerializeField] float maxRotationSpeed = 180f;
    [SerializeField] float dampingFactor = 0.2f;
    [SerializeField] float velocityDamping = 0.9f;
    [SerializeField] float deadZoneThreshold = 5f;

    private void Awake() {
        PlayerControls = new GameInput();
    }

    private void OnEnable() {
        move = PlayerControls.Player.Move;
        move.Enable();

        look = PlayerControls.Player.Look;
        look.Enable();

        attack = PlayerControls.Player.Attack;
        attack.Enable();
        attack.performed += attackInput;
    }

    private void OnDisable() {
        move.Disable();
        look.Disable();
        attack.Disable();
    }

    void Update() {
        processMoveInput();

        processLookInput();
    }

    private void processMoveInput() {
        moveDirection = move.ReadValue<Vector2>();
        player.SetCharacterPosition(moveDirection);
    }

    private void processLookInput() {
        Vector2 mouseInput = Mouse.current.position.ReadValue();
        Vector2 gamepadInput = look.ReadValue<Vector2>();

        var activeControl = look.activeControl;
        var activeDevice = activeControl?.device;
        if (activeDevice != null) {
            switch (activeDevice) {
                case Gamepad:
                    lastLookDirection = gamepadInput;
                    isUsingMouse = false;
                    break;
                case Mouse:
                    lastLookDirection = mouseInput;
                    isUsingMouse = true;
                    break;
            }
        }

        float currentAngle = player.GetWeaponAngle();

        if (isUsingMouse) {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            float z = Camera.main.WorldToScreenPoint(player.transform.position).z;
            Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, z));
            worldMousePos.y = player.transform.position.y;

            Vector3 directionToMouse = worldMousePos - player.transform.position;
            directionToMouse.y = 0;

            if (directionToMouse.sqrMagnitude > 0.1f) {
                lastLookDirection = new Vector2(directionToMouse.x, directionToMouse.z);
                targetAngle = RadialHelper.NormalizeAngle(RadialHelper.CartesianToPol(lastLookDirection).y);
            }
        }
        else if (lastLookDirection.sqrMagnitude > 0.01f) {
            Vector2 smoothedDirection = Vector2.Lerp(lastLookDirection, gamepadInput, Time.deltaTime * rotationSpeed);
            targetAngle = RadialHelper.NormalizeAngle(RadialHelper.CartesianToPol(smoothedDirection).y);
        }

        
        float angularDifference = Mathf.DeltaAngle(currentAngle, targetAngle);

        
        if (Mathf.Abs(angularDifference) < deadZoneThreshold) {
            player.AddWeaponOrbital(0);
            return;
        }

        
        float currentAngularVelocity = player.GetWeaponOrbital();

        // --- PD Controller for angular motion ---
        // kp: proportional gain (affects how strongly the error is corrected)
        // kd: derivative gain (affects how strongly current angular velocity is damped)
        float kp = rotationSpeed * dampingFactor;
        float kd = velocityDamping;
        // The control signal calculates the "torque" needed:
        float controlSignal = kp * angularDifference - kd * currentAngularVelocity;

        
        float newAngularVelocity = currentAngularVelocity + controlSignal * Time.deltaTime;
        newAngularVelocity = Mathf.Clamp(newAngularVelocity, -maxRotationSpeed, maxRotationSpeed);

        
        float addedMomentum = newAngularVelocity - currentAngularVelocity;
        player.AddWeaponOrbital(addedMomentum);

        
        Vector2 targetDirection = RadialHelper.PolarToCart(targetAngle, 1);
        Vector3 worldTargetDirection = new Vector3(targetDirection.x, 0, targetDirection.y).normalized;
        Debug.DrawRay(player.transform.position, worldTargetDirection * 2, Color.blue);

        Vector3 lastLookDir = new Vector3(lastLookDirection.x, 0, lastLookDirection.y).normalized;
        Debug.DrawRay(player.GetWeaponPosition(), lastLookDir * 2, Color.red);
    }

    private void attackInput(InputAction.CallbackContext context) {
        processLookInput();
    }
}