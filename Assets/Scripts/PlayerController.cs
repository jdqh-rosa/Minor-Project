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
    Vector2 lookDirection;
    private Vector2 lastLookDirection = Vector2.zero; // Stores last used input direction
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
        if (activeDevice == null) return;
        
        if (activeDevice is Gamepad) // Gamepad stick movement
        {
            lastLookDirection = gamepadInput;
            isUsingMouse = false;
        }
        else if (activeDevice is Mouse) {
            lastLookDirection = mouseInput;
            isUsingMouse = true;
        }

        if (isUsingMouse) {
            Vector2 screenPos = Mouse.current.position.ReadValue();

            float z = Camera.main.WorldToScreenPoint(player.transform.position).z;

            Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, z));
    
            worldMousePos.y = player.transform.position.y;

            Vector3 directionToMouse = worldMousePos - player.transform.position;
            directionToMouse.y = 0;
            
            if (directionToMouse.sqrMagnitude > 0.1f) {
                //float angle = Mathf.Atan2(directionToMouse.z, directionToMouse.x) * Mathf.Rad2Deg;
                //player.SetWeaponOrbit(angle);
                
                float _targetAngle = Mathf.Atan2(directionToMouse.z, directionToMouse.x) * Mathf.Rad2Deg;

                // Apply torque/force to rotate the weapon towards the target angle
                player.RotateWeaponTowardsAngle(_targetAngle, rotationSpeed);
            }
        }
        else if (lastLookDirection.sqrMagnitude > 0.01f) // Ignore very small inputs
        {
            float angle = Mathf.Atan2(lastLookDirection.y, lastLookDirection.x) * Mathf.Rad2Deg;
            player.SetWeaponOrbit(angle);
        }
    }

    private void Attack(InputAction.CallbackContext context) {
        Debug.Log("Attack");
    }
}