using UnityEngine;
using UnityEngine.InputSystem; // Required for InputValue
using Unity.Cinemachine;       // Required for CinemachineCamera

public class PlayerCameraController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Cinemachine Camera component to control.")]
    [SerializeField] private CinemachineCamera virtualCamera;

    [Header("Zoom Settings")]
    [Tooltip("Minimum Orthographic Size (closest zoom).")]
    [SerializeField] private float minOrthographicSize = 3f;

    [Tooltip("Maximum Orthographic Size (farthest zoom).")]
    [SerializeField] private float maxOrthographicSize = 10f;

    [Tooltip("How sensitive the zoom is to scroll input. Higher value = faster zoom.")]
    [SerializeField] private float zoomStep = 0.5f; // How much target size changes per scroll 'tick'

    [Tooltip("How quickly the camera smooths to the target zoom level. Smaller values are faster.")]
    [SerializeField] private float zoomSmoothTime = 0.15f;

    private float currentTargetOrthographicSize;
    private float zoomVelocity = 0f; // Needed for SmoothDamp

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
    }

    void Update()
    {
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
        // The scroll wheel often gives large values (e.g., +/- 120). Normalize it slightly.
        // We only care about the direction (positive or negative).
        float scrollDirection = Mathf.Sign(scrollInput);

        // Adjust the target orthographic size
        // Subtract because scrolling UP (positive value usually) should zoom IN (decrease orthographic size)
        currentTargetOrthographicSize -= scrollDirection * zoomStep;

        // Clamp the target size within the defined min/max range
        currentTargetOrthographicSize = Mathf.Clamp(currentTargetOrthographicSize, minOrthographicSize, maxOrthographicSize);
    }

    // Legacy method kept for compatibility if using PlayerInput component with Send/Broadcast Messages
    // Can be removed if exclusively using the PlayerInputManager approach
    public void OnZoom(InputValue value)
    {
        HandleZoomInput(value.Get<float>());
    }
}