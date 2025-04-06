using UnityEngine;

public class FishVisualController : MonoBehaviour
{
    [Header("Tilting")]
    [Tooltip("Maximum angle in degrees the fish tilts up or down based on vertical movement.")]
    public float maxTiltAngle = 10f; // Reduced from 15f
    [Tooltip("How quickly the fish smoothly rotates towards the target tilt angle. Higher is faster.")]
    public float tiltSpeed = 5f; // Reduced from 10f

    [Header("Flipping")]
    [Tooltip("Check this if your sprite asset faces right by default. Uncheck if it faces left.")]
    public bool spriteFacesRightInitially = true;
    [Tooltip("Minimum horizontal input magnitude required to trigger a flip.")]
    public float flipThreshold = 0.25f; // Increased from 0.1f
    [Tooltip("Forces the fish to maintain its last facing direction when input is below this threshold.")]
    public float movementDeadZone = 0.15f; // Increased from 0.05f
    [Tooltip("Minimum time (seconds) to wait before allowing another flip. Prevents rapid flipping.")]
    public float flipCooldown = 0.5f;

    // Private references and state
    private Transform spriteTransform; // The transform of this GameObject (the sprite container)
    private float originalScaleX;      // Stores the magnitude of the initial X scale
    private bool isCurrentlyFacingRight = true; // Tracks the current visual facing direction
    private Vector2 lastSignificantMoveInput = Vector2.right; // Tracks the last significant movement
    private float lastFlipTime = -1f; // Tracks when we last flipped
    private float currentTiltVelocity = 0f; // Used for SmoothDamp

    void Awake()
    {
        // Cache the transform component this script is attached to
        spriteTransform = transform;

        // Store the absolute value of the initial X scale for flipping calculations
        originalScaleX = Mathf.Abs(spriteTransform.localScale.x);

        // Determine the initial facing direction based on the initial scale and the inspector setting
        isCurrentlyFacingRight = (spriteTransform.localScale.x > 0) == spriteFacesRightInitially;

        // Initialize last significant direction based on current facing
        lastSignificantMoveInput = isCurrentlyFacingRight ? Vector2.right : Vector2.left;
    }

    /// <summary>
    /// Updates the fish sprite's orientation (flip and tilt) based on movement input.
    /// Call this method from your PlayerController every frame input is processed.
    /// </summary>
    /// <param name="moveInput">The player's current movement input vector (typically ranges from -1 to 1 on each axis).</param>
    public void UpdateVisuals(Vector2 moveInput)
    {
        // If the input has significant magnitude, store it for future reference
        if (moveInput.magnitude > movementDeadZone)
        {
            // Only update horizontal component if it's significant
            if (Mathf.Abs(moveInput.x) > flipThreshold)
            {
                lastSignificantMoveInput.x = moveInput.x;
            }

            // Always update vertical component for tilting
            lastSignificantMoveInput.y = moveInput.y;
        }

        // Use filtered input for visual updates
        Vector2 visualInput = FilterInputForVisuals(moveInput);

        // Handle flipping and tilting in sequence
        HandleHorizontalFlip(visualInput.x);
        HandleVerticalTilt(visualInput.y);
    }

    /// <summary>
    /// Filters the raw input for visual purposes to prevent flickering when input is minimal
    /// </summary>
    private Vector2 FilterInputForVisuals(Vector2 rawInput)
    {
        // Hard zero-out any tiny inputs to prevent jitter
        if (Mathf.Abs(rawInput.x) < 0.01f) rawInput.x = 0;
        if (Mathf.Abs(rawInput.y) < 0.01f) rawInput.y = 0;

        // If the current input magnitude is very small, use the last significant direction
        // but only for horizontal movement (to prevent flipping issues)
        if (rawInput.magnitude < movementDeadZone)
        {
            // Keep vertical input as is (can be 0) to allow fish to level out when not moving vertically
            return new Vector2(
                Mathf.Abs(lastSignificantMoveInput.x) > flipThreshold ? lastSignificantMoveInput.x : 0,
                0 // Force vertical to zero when idle for proper leveling
            );
        }

        return rawInput;
    }

    /// <summary>
    /// Handles flipping the sprite horizontally based on horizontal input.
    /// </summary>
    private void HandleHorizontalFlip(float horizontalInput)
    {
        // Only consider flipping if cooldown has elapsed
        if (Time.time < lastFlipTime + flipCooldown)
            return;

        // Only flip if the input exceeds the threshold
        if (Mathf.Abs(horizontalInput) > flipThreshold)
        {
            bool shouldFaceRight = horizontalInput > 0;

            // Only flip if the desired direction is different from the current one
            if (shouldFaceRight != isCurrentlyFacingRight)
            {
                isCurrentlyFacingRight = shouldFaceRight;
                lastFlipTime = Time.time;

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
        // Calculate the base target tilt angle (-maxTiltAngle to +maxTiltAngle)
        float baseTargetTiltZ = verticalInput * maxTiltAngle;

        // If the sprite is currently flipped (facing left), invert the target tilt angle.
        // This makes "up" input visually tilt the nose up regardless of facing direction.
        float correctedTargetTiltZ = isCurrentlyFacingRight ? baseTargetTiltZ : -baseTargetTiltZ;

        // Get the current rotation in Euler angles
        float currentTiltZ = spriteTransform.localEulerAngles.z;

        // Normalize current rotation to -180 to 180 range
        if (currentTiltZ > 180f)
            currentTiltZ -= 360f;

        // Use SmoothDamp for even smoother rotation transitions
        float smoothTiltZ = Mathf.SmoothDamp(currentTiltZ, correctedTargetTiltZ,
                                            ref currentTiltVelocity, 1.0f / tiltSpeed);

        // Apply the new rotation, keeping X and Y rotation unchanged
        spriteTransform.localRotation = Quaternion.Euler(
            spriteTransform.localEulerAngles.x,
            spriteTransform.localEulerAngles.y,
            smoothTiltZ
        );
    }

    /// <summary>
    /// Optional: Resets the visuals to a default state (facing initial direction, no tilt).
    /// </summary>
    public void ResetVisuals()
    {
        // Reset tilt smoothly
        currentTiltVelocity = 0f;
        spriteTransform.localRotation = Quaternion.Euler(spriteTransform.localEulerAngles.x, spriteTransform.localEulerAngles.y, 0f);

        // Reset flip to initial direction
        isCurrentlyFacingRight = spriteFacesRightInitially;
        float initialScaleX = originalScaleX * (spriteFacesRightInitially ? 1f : -1f);
        spriteTransform.localScale = new Vector3(initialScaleX, spriteTransform.localScale.y, spriteTransform.localScale.z);
    }
}