using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class PlayerController : MonoBehaviour
{
    public GameInput PlayerControls;

    [SerializeField] private InputActionReference actionReference;

    private InputAction move;
    private InputAction look;
    private InputAction attack;
    private InputAction swing;

    [SerializeField] private Character player;

    [SerializeField] private float rotationSpeed = 10f;
    private Vector2 lastLookDirection = Vector2.zero; // Stores last used input direction
    private float targetAngle;
    private bool isUsingMouse = false;

    Vector2 moveDirection = Vector2.zero;
    
    bool isAttacking = false;

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
        attack.performed += context => attackInput(context);
        
        swing = PlayerControls.Player.Swing;
        swing.Enable();
        swing.performed += context => rotateWeapon();
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
            player.SetLookDirection(lastLookDirection.normalized);
        }
        else if (lastLookDirection.sqrMagnitude > 0.01f) {
            Vector2 smoothedDirection = Vector2.Lerp(lastLookDirection, gamepadInput, Time.deltaTime * rotationSpeed);
            targetAngle = RadialHelper.NormalizeAngle(RadialHelper.CartesianToPol(smoothedDirection).y);
            player.SetLookDirection(smoothedDirection.normalized);
        }
        
        player.RotateWeaponTowardsAngle(targetAngle);
    }

    private void rotateWeapon() {
        player.RotateWeaponTowardsAngle(targetAngle);
    }

    private void attackInput(InputAction.CallbackContext context) {
        if (context.interaction is HoldInteraction) {
            Debug.Log($"Hold Attack");
            player.Attack(ActionInput.Hold, targetAngle);
        }
        if (context.interaction is PressInteraction) {
            Debug.Log($"Press Attack");
            player.Attack(ActionInput.Press, targetAngle);
        }
    }
}