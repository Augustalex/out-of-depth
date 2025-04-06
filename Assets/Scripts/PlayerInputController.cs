using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(FishController))]
public class PlayerInputController : MonoBehaviour
{
    // References
    private FishController fishController;
    private PlayerInput playerInput;

    // Input state
    private Vector2 moveInput;
    private bool dashPressed;

    // Input action references
    private InputAction moveAction;
    private InputAction dashAction;

    private void Awake()
    {
        // Get references
        fishController = GetComponent<FishController>();
        playerInput = GetComponent<PlayerInput>();

        // Set up input actions
        if (playerInput != null)
        {
            moveAction = playerInput.actions["Move"];
            dashAction = playerInput.actions["Dash"];
        }
        else
        {
            Debug.LogWarning("PlayerInput component not found. Manual input binding required.", this);
        }
    }

    private void OnEnable()
    {
        // Subscribe to input events if using the new Input System
        if (dashAction != null)
        {
            dashAction.performed += OnDashPerformed;
            dashAction.canceled += OnDashCanceled;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from input events
        if (dashAction != null)
        {
            dashAction.performed -= OnDashPerformed;
            dashAction.canceled -= OnDashCanceled;
        }
    }

    private void Update()
    {
        // Read movement input
        if (moveAction != null)
        {
            moveInput = moveAction.ReadValue<Vector2>();
        }
        else
        {
            // Fallback to old input system if needed
            moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        }

        // Apply movement input to the fish controller
        fishController.SetMoveInput(moveInput);
    }

    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        fishController.TryDash();
    }

    private void OnDashCanceled(InputAction.CallbackContext context)
    {
        fishController.ReleaseDash();
    }
}