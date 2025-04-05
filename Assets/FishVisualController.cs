using UnityEngine;

public class FishVisualController : MonoBehaviour
{
    [Header("Tilting")]
    [Tooltip("Maximum angle in degrees the fish tilts up or down based on vertical movement.")]
    public float maxTiltAngle = 15f;
    [Tooltip("How quickly the fish smoothly rotates towards the target tilt angle. Higher is faster.")]
    public float tiltSpeed = 10f;

    [Header("Flipping")]
    [Tooltip("Check this if your sprite asset faces right by default. Uncheck if it faces left.")]
    public bool spriteFacesRightInitially = true;
    [Tooltip("Minimum horizontal input magnitude required to trigger a flip.")]
    public float flipThreshold = 0.1f;

    // Private references and state
    private Transform spriteTransform; // The transform of this GameObject (the sprite container)
    private float originalScaleX;      // Stores the magnitude of the initial X scale
    private bool isCurrentlyFacingRight = true; // Tracks the current visual facing direction

    void Awake()
    {
        // Cache the transform component this script is attached to
        spriteTransform = transform;

        // Store the absolute value of the initial X scale for flipping calculations
        originalScaleX = Mathf.Abs(spriteTransform.localScale.x);

        // Determine the initial facing direction based on the initial scale and the inspector setting
        isCurrentlyFacingRight = (spriteTransform.localScale.x > 0) == spriteFacesRightInitially;
    }

    /// <summary>
    /// Updates the fish sprite's orientation (flip and tilt) based on movement input.
    /// Call this method from your PlayerController every frame input is processed.
    /// </summary>
    /// <param name="moveInput">The player's current movement input vector (typically ranges from -1 to 1 on each axis).</param>
    public void UpdateVisuals(Vector2 moveInput)
    {
        HandleHorizontalFlip(moveInput.x);
        HandleVerticalTilt(moveInput.y);
    }

    /// <summary>
    /// Handles flipping the sprite horizontally based on horizontal input.
    /// </summary>
    private void HandleHorizontalFlip(float horizontalInput)
    {
        // Determine the desired facing direction based on input, only if input exceeds threshold
        if (Mathf.Abs(horizontalInput) > flipThreshold)
        {
            bool shouldFaceRight = horizontalInput > 0;

            // Only flip if the desired direction is different from the current one
            if (shouldFaceRight != isCurrentlyFacingRight)
            {
                isCurrentlyFacingRight = shouldFaceRight;

                // Calculate the target X scale:
                // - Start with the original magnitude.
                // - Multiply by 1 if facing right, -1 if facing left.
                // - If the sprite initially faces left, invert the result.
                float targetScaleX = originalScaleX * (isCurrentlyFacingRight ? 1f : -1f);
                if (!spriteFacesRightInitially)
                {
                    targetScaleX *= -1f; // Invert scale if base sprite faces left
                }

                // Apply the new scale immediately
                spriteTransform.localScale = new Vector3(targetScaleX, spriteTransform.localScale.y, spriteTransform.localScale.z);
            }
        }
        // If horizontalInput is below the threshold, we don't change the flip state, maintaining the last direction.
    }

    /// <summary>
    /// Handles tilting the sprite up or down based on vertical input.
    /// </summary>
    private void HandleVerticalTilt(float verticalInput)
    {
        // Calculate the target tilt angle (-maxTiltAngle to +maxTiltAngle)
        float targetTiltZ = verticalInput * maxTiltAngle;

        // Get the current rotation in Euler angles (local space)
        Vector3 currentLocalEuler = spriteTransform.localEulerAngles;

        // Smoothly interpolate the current Z angle towards the target Z angle
        // LerpAngle handles wrapping correctly (e.g., from 350 degrees to 10 degrees)
        float smoothTiltZ = Mathf.LerpAngle(currentLocalEuler.z, targetTiltZ, Time.deltaTime * tiltSpeed);

        // Apply the new rotation, keeping X and Y rotation unchanged (relative to parent)
        spriteTransform.localRotation = Quaternion.Euler(currentLocalEuler.x, currentLocalEuler.y, smoothTiltZ);
    }

    /// <summary>
    /// Optional: Resets the visuals to a default state (facing initial direction, no tilt).
    /// </summary>
    public void ResetVisuals()
    {
        // Reset tilt smoothly or instantly
        spriteTransform.localRotation = Quaternion.Euler(spriteTransform.localEulerAngles.x, spriteTransform.localEulerAngles.y, 0f);

        // Reset flip to initial direction
        isCurrentlyFacingRight = spriteFacesRightInitially;
        float initialScaleX = originalScaleX * (spriteFacesRightInitially ? 1f : -1f);
        spriteTransform.localScale = new Vector3(initialScaleX, spriteTransform.localScale.y, spriteTransform.localScale.z);
    }
}