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
    Vector2 lookDirection;
    private Vector2 lastLookDirection = Vector2.zero; // Stores last used input direction
    private bool isUsingMouse = false;

    Vector2 moveDirection = Vector2.zero;
    
    [SerializeField] AngularStuff angularStuff;

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

    private void Start() {
    }

    void Update() {
        ProcessMoveInput();

        player.AddWeaponOrbital(10f);
        
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
            Vector2 mousePos = Mouse.current.position.ReadValue();
            float z = Camera.main.WorldToScreenPoint(player.transform.position).z;
            Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, z));
            worldMousePos.y = player.transform.position.y;

            Vector3 directionToMouse = worldMousePos - player.transform.position;
            directionToMouse.y = 0; 

            if (directionToMouse.sqrMagnitude > 0.1f) {
                //float targetAngle = Mathf.Atan2(directionToMouse.z, directionToMouse.x) * Mathf.Rad2Deg;
                //float currentAngle = player.GetWeaponAngle();
                //
                //Debug.DrawRay(player.transform.position, directionToMouse, Color.red);
                //
                //// Compute angular velocity needed to turn smoothly
                //float angularDifference = Mathf.DeltaAngle(currentAngle, targetAngle);
                //float angularVelocity = angularDifference * rotationSpeed * Time.deltaTime;
                //
                //Debug.Log($"TargetAngle: {targetAngle}, CurrentAngle: {currentAngle}");
                //
                //// Apply momentum
                //player.AddWeaponOrbital(angularVelocity);
                
                float targetAngle = Mathf.Atan2(directionToMouse.z, directionToMouse.x) * Mathf.Rad2Deg;
                float currentAngle = angularStuff.GetAngle();
                
                Debug.DrawRay(player.transform.position, directionToMouse, Color.red);
                
                // Compute angular velocity needed to turn smoothly
                float angularDifference = Mathf.DeltaAngle(currentAngle, targetAngle);
                float angularVelocity = angularDifference * rotationSpeed;
                
                Debug.Log($"TargetAngle: {targetAngle}, CurrentAngle: {currentAngle}");
                
                angularStuff.Accelerate(angularVelocity);
            }
        }
        else if (lastLookDirection.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(lastLookDirection.y, lastLookDirection.x) * Mathf.Rad2Deg;
            player.SetWeaponOrbit(angle);
        }
    }

    private void Attack(InputAction.CallbackContext context) {
        Debug.Log("Attack");
    }
}