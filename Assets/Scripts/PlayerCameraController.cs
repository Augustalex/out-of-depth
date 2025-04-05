using UnityEngine;
using UnityEngine.InputSystem; // Required for InputValue
using Unity.Cinemachine;       // Required for CinemachineCamera

public class PlayerCameraController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Cinemachine Camera component to control.")]
    [SerializeField] private CinemachineCamera virtualCamera;

    [Tooltip("Reference to the player's Rigidbody2D component.")]
    [SerializeField] private Rigidbody2D playerRigidbody;

    [Header("Zoom Settings")]
    [Tooltip("Minimum Orthographic Size (closest zoom).")]
    [SerializeField] private float minOrthographicSize = 3f;

    [Tooltip("Maximum Orthographic Size (farthest zoom).")]
    [SerializeField] private float maxOrthographicSize = 10f;

    [Tooltip("How sensitive the zoom is to scroll input. Higher value = faster zoom.")]
    [SerializeField] private float zoomStep = 0.5f; // How much target size changes per scroll 'tick'

    [Tooltip("How quickly the camera smooths to the target zoom level. Smaller values are faster.")]
    [SerializeField] private float zoomSmoothTime = 0.15f;

    [Header("Velocity-Based Zoom")]
    [Tooltip("How much the camera zooms out based on player velocity")]
    [SerializeField] private float velocityZoomFactor = 0.2f;

    [Tooltip("Maximum additional zoom from velocity")]
    [SerializeField] private float maxVelocityZoom = 3f;

    [Tooltip("Base orthographic size when player is not moving")]
    [SerializeField] private float baseOrthographicSize = 5f;

    [Tooltip("How quickly the camera adjusts to velocity changes")]
    [SerializeField] private float velocityZoomSmoothTime = 0.5f;

    private float currentTargetOrthographicSize;
    private float zoomVelocity = 0f; // Needed for SmoothDamp
    private float manualZoomOffset = 0f; // Tracks manual zoom adjustments
    private float currentVelocityZoom = 0f; // Current velocity-based zoom
    private float velocityZoomVelocity = 0f; // For smoothing velocity zoom

    void Awake()
    {
        // Basic validation
        if (virtualCamera == null)
        {
            Debug.LogError("PlayerCameraController: CinemachineCamera reference is not set!", this);
            enabled = false; // Disable script if VCam isn't set
            return;
        }

        // Check if the camera is orthographic using the 'Lens' property
        if (!virtualCamera.Lens.Orthographic)
        {
            Debug.LogWarning("PlayerCameraController: Attached CinemachineCamera is not set to Orthographic projection.", this);
        }

        // Initialize the target size to the camera's starting size
        // Use the 'Lens' property
        currentTargetOrthographicSize = virtualCamera.Lens.OrthographicSize;

        // Find the player's Rigidbody2D if not set
        if (playerRigidbody == null)
        {
            FindPlayer();
        }
    }

    void FindPlayer()
    {
        // Try to find player through tag first
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        // If that fails, look for PlayerController component
        if (player == null)
        {
            PlayerController playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
            {
                player = playerController.gameObject;
            }
        }

        // Set the Rigidbody2D if we found the player
        if (player != null)
        {
            playerRigidbody = player.GetComponent<Rigidbody2D>();
            if (playerRigidbody == null)
            {
                Debug.LogWarning("PlayerCameraController: Player found but has no Rigidbody2D component.", this);
            }
        }
        else
        {
            Debug.LogWarning("PlayerCameraController: Could not find player. Velocity-based zoom will not work.", this);
        }
    }

    public void SetPlayer(GameObject player)
    {
        if (player != null)
        {
            playerRigidbody = player.GetComponent<Rigidbody2D>();
            if (playerRigidbody == null)
            {
                Debug.LogWarning("PlayerCameraController: Provided player has no Rigidbody2D component.", this);
            }
        }
    }

    void Update()
    {
        // Get player velocity directly from the Rigidbody2D
        float playerVelocity = 0f;
        if (playerRigidbody != null)
        {
            playerVelocity = playerRigidbody.linearVelocity.magnitude;
        }

        // Calculate target velocity-based zoom amount
        float targetVelocityZoom = Mathf.Clamp(playerVelocity * velocityZoomFactor, 0f, maxVelocityZoom);

        // Smoothly interpolate to the target velocity zoom
        currentVelocityZoom = Mathf.SmoothDamp(
            currentVelocityZoom,
            targetVelocityZoom,
            ref velocityZoomVelocity,
            velocityZoomSmoothTime
        );

        // Calculate final target size by combining base size, velocity zoom, and manual zoom offset
        currentTargetOrthographicSize = baseOrthographicSize + currentVelocityZoom + manualZoomOffset;

        // Ensure we respect min/max zoom limits
        currentTargetOrthographicSize = Mathf.Clamp(currentTargetOrthographicSize, minOrthographicSize, maxOrthographicSize);

        // Check current size using the 'Lens' property
        if (Mathf.Abs(virtualCamera.Lens.OrthographicSize - currentTargetOrthographicSize) > 0.01f)
        {
            // Apply smoothing using the 'Lens' property
            virtualCamera.Lens.OrthographicSize = Mathf.SmoothDamp(
                virtualCamera.Lens.OrthographicSize, // Read current size via Lens
                currentTargetOrthographicSize,
                ref zoomVelocity, // SmoothDamp manages this velocity value
                zoomSmoothTime
            );
        }
        else
        {
            // Snap to target if very close, using the 'Lens' property
            virtualCamera.Lens.OrthographicSize = currentTargetOrthographicSize;
            zoomVelocity = 0f; // Reset velocity when target is reached
        }
    }

    // Public method for external input handling by PlayerInputManager
    public void HandleZoomInput(float scrollInput)
    {
        Debug.Log($"Zoom input received: {scrollInput}");

        // The scroll wheel often gives large values (e.g., +/- 120). Normalize it slightly.
        // We only care about the direction (positive or negative).
        float scrollDirection = Mathf.Sign(scrollInput);

        // Adjust the manual zoom offset
        // Subtract because scrolling UP (positive value usually) should zoom IN (decrease orthographic size)
        manualZoomOffset -= scrollDirection * zoomStep;

        // Calculate what the final size would be with this manual zoom
        float potentialSize = baseOrthographicSize + currentVelocityZoom + manualZoomOffset;

        // If this would exceed limits, adjust the manual zoom offset to respect the limits
        if (potentialSize < minOrthographicSize)
        {
            manualZoomOffset = minOrthographicSize - (baseOrthographicSize + currentVelocityZoom);
        }
        else if (potentialSize > maxOrthographicSize)
        {
            manualZoomOffset = maxOrthographicSize - (baseOrthographicSize + currentVelocityZoom);
        }

        Debug.Log($"New manual zoom offset: {manualZoomOffset}, Target size: {potentialSize}");
    }

    // Legacy method kept for compatibility if using PlayerInput component with Send/Broadcast Messages
    // Can be removed if exclusively using the PlayerInputManager approach
    public void OnZoom(InputValue value)
    {
        HandleZoomInput(value.Get<float>());
    }
}