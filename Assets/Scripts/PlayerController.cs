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
    float targetAngle;
    private bool isUsingMouse = false;

    Vector2 moveDirection = Vector2.zero;

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
        attack.performed += Attack;
    }

    private void OnDisable() {
        move.Disable();
        look.Disable();
        attack.Disable();
    }

    void Update() {
        ProcessMoveInput();

        ProcessLookInput();
    }

    void ProcessMoveInput() {
        moveDirection = move.ReadValue<Vector2>();
        player.SetCharacterPosition(moveDirection);
    }

    void ProcessLookInput() {
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
        else if (lastLookDirection.sqrMagnitude > 0.01f)
        {
            targetAngle = RadialHelper.NormalizeAngle(RadialHelper.CartesianToPol(lastLookDirection).y);
        }
        
        float angularDifference = Mathf.DeltaAngle(currentAngle, targetAngle);
        float maxRotationSpeed = 180f;
        float angularVelocity = 0;
        float dampingFactor = 0.2f;
        float velocityDamping = 0.9f;
        float angularAcceleration = angularDifference * rotationSpeed * dampingFactor;
        angularVelocity = (angularVelocity + angularAcceleration) * velocityDamping;
        
        Debug.Log($"currentAngle: {currentAngle}, targetAngle: {targetAngle}");
        
        player.AddWeaponOrbital(Mathf.Clamp(angularVelocity, -maxRotationSpeed, maxRotationSpeed));

        {
            Vector2 perpendicularDirection = new Vector2(-lastLookDirection.y, lastLookDirection.x).normalized;
            Vector2 angularDirection = perpendicularDirection * angularVelocity;
            Vector3 worldAngularDirection = new Vector3(angularDirection.x, 0, angularDirection.y).normalized;
            Debug.DrawRay(player.transform.position, worldAngularDirection * Mathf.Abs(angularVelocity), Color.green);

            Vector2 targetDirection = RadialHelper.PolarToCart(targetAngle, 1);
            Vector3 worldTargetDirection = new Vector3(targetDirection.x, 0, targetDirection.y).normalized;
            Debug.DrawRay(player.transform.position, worldTargetDirection * 2, Color.blue);

            Vector3 lastLookDir = new Vector3(lastLookDirection.x, 0, lastLookDirection.y).normalized;
            Debug.DrawRay(player.GetWeaponPosition(), lastLookDir * 2, Color.red);
        }

    }

    private void Attack(InputAction.CallbackContext context) {
        ProcessLookInput();
    }
}