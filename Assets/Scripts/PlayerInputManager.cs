using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public class PlayerInputManager : MonoBehaviour
{
    private PlayerInputActions inputActions;

    [Header("References")]
    [Tooltip("Reference to the PlayerController component")]
    [SerializeField] private PlayerController playerController;

    [Tooltip("Reference to the PlayerCameraController component")]
    [SerializeField] private PlayerCameraController cameraController;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Camera.Enable(); // Enable the Camera action map as well

        // Movement
        inputActions.Player.Move.performed += OnMovePerformed;
        inputActions.Player.Move.canceled += OnMoveCanceled;

        // Dash
        inputActions.Player.Dash.performed += OnDashPerformed;

        // Mouth Control
        inputActions.Player.OpenMouth.started += OnMouthOpenStarted;
        inputActions.Player.OpenMouth.canceled += OnMouthOpenCanceled;

        // Camera Zoom Control
        inputActions.Camera.Zoom.performed += OnZoomPerformed;
    }

    private void OnDisable()
    {
        // Camera Zoom Control
        inputActions.Camera.Zoom.performed -= OnZoomPerformed;

        // Mouth Control
        inputActions.Player.OpenMouth.started -= OnMouthOpenStarted;
        inputActions.Player.OpenMouth.canceled -= OnMouthOpenCanceled;

        // Dash
        inputActions.Player.Dash.performed -= OnDashPerformed;

        // Movement
        inputActions.Player.Move.performed -= OnMovePerformed;
        inputActions.Player.Move.canceled -= OnMoveCanceled;

        inputActions.Player.Disable();
        inputActions.Camera.Disable(); // Disable the Camera action map when disabling

        // Reset player state when input is disabled
        playerController.ResetState();
    }

    // --- Input Action Handlers ---
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        Vector2 moveInput = context.ReadValue<Vector2>();
        playerController.SetMoveInput(moveInput);
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        playerController.SetMoveInput(Vector2.zero);
    }

    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        playerController.TryDash();
    }

    private void OnMouthOpenStarted(InputAction.CallbackContext context)
    {
        playerController.SetMouthState(true);
    }

    private void OnMouthOpenCanceled(InputAction.CallbackContext context)
    {
        playerController.SetMouthState(false);
    }

    private void OnZoomPerformed(InputAction.CallbackContext context)
    {
        if (cameraController != null)
        {
            float scrollInput = context.ReadValue<float>();
            cameraController.HandleZoomInput(scrollInput);
        }
    }
}
