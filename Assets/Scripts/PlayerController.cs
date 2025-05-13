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
    private Vector2 lastLookDirection = Vector2.zero;
    private float targetAngle;
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
        player.SetCharacterDirection(MiscHelper.Vec2ToVec3Pos(moveDirection));
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
            Vector2 _mousePos = Mouse.current.position.ReadValue();
            float _z = Camera.main.WorldToScreenPoint(player.transform.position).z;
            Vector3 _worldMousePos = Camera.main.ScreenToWorldPoint(new Vector3(_mousePos.x, _mousePos.y, _z));
            _worldMousePos.y = player.transform.position.y;

            Vector3 _directionToMouse = _worldMousePos - player.transform.position;
            _directionToMouse.y = 0;

            if (_directionToMouse.sqrMagnitude > 0.1f) {
                lastLookDirection = new Vector2(_directionToMouse.x, _directionToMouse.z);
                targetAngle = RadialHelper.NormalizeAngle(RadialHelper.CartesianToPol(lastLookDirection).y);
            }
            player.SetLookDirection(lastLookDirection.normalized);
        }
        else if (lastLookDirection.sqrMagnitude > 0.01f) {
            Vector2 _smoothedDirection = Vector2.Lerp(lastLookDirection, gamepadInput, Time.deltaTime * rotationSpeed);
            targetAngle = RadialHelper.NormalizeAngle(RadialHelper.CartesianToPol(_smoothedDirection).y);
            player.SetLookDirection(_smoothedDirection.normalized);
        }
        
        player.RotateWeaponTowardsAngle(targetAngle);
    }

    private void rotateWeapon()
    {
        if (player.IsAttacking()) return;
        
        
        player.RotateWeaponTowardsAngle(targetAngle);
    }

    private void attackInput(InputAction.CallbackContext pContext) {
        if (pContext.interaction is HoldInteraction) {
            Debug.Log($"Hold Attack");
            player.Attack(ActionInput.Hold, targetAngle);
        }
        if (pContext.interaction is PressInteraction) {
            Debug.Log($"Press Attack");
            player.Attack(ActionInput.Press, targetAngle);
        }
    }
}