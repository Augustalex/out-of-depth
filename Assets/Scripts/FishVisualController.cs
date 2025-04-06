// FishVisualController.cs:
using UnityEngine;

public class FishVisualController : MonoBehaviour
{
    [Header("Tilting")]
    [Tooltip("Maximum angle in degrees the fish tilts up or down based on vertical velocity.")]
    public float maxTiltAngle = 12f; // Slightly adjusted, tune as needed
    [Tooltip("How quickly the fish smoothly rotates towards the target tilt angle (lower is slower/smoother).")]
    public float tiltSmoothTime = 0.2f; // Changed to smooth time for SmoothDampAngle

    [Header("Flipping")]
    [Tooltip("Check this if your sprite asset faces right by default.")]
    public bool spriteFacesRightInitially = true;
    [Tooltip("Minimum horizontal *velocity* required to trigger a potential flip.")]
    public float flipVelocityThreshold = 0.5f; // Based on velocity now, increased threshold
    [Tooltip("Minimum *velocity* magnitude required for the fish to be considered 'moving' for orientation purposes.")]
    public float movementVelocityThreshold = 0.2f; // Increased
    [Tooltip("Minimum time (seconds) to wait before allowing another flip.")]
    public float flipCooldown = 0.4f; // Slightly increased

    // Private references and state
    private Transform spriteTransform;
    private float originalScaleX;
    private bool isCurrentlyFacingRight = true;
    private Vector2 lastFacingDirection = Vector2.right; // Tracks direction, not input
    private float lastFlipTime = -Mathf.Infinity;
    private float currentTiltVelocity = 0f; // Used for SmoothDampAngle

    void Awake()
    {
        spriteTransform = transform;
        originalScaleX = Mathf.Abs(spriteTransform.localScale.x);
        isCurrentlyFacingRight = (spriteTransform.localScale.x > 0) == spriteFacesRightInitially;
        lastFacingDirection = isCurrentlyFacingRight ? Vector2.right : Vector2.left;
    }

    /// <summary>
    /// Updates the fish sprite's orientation based on its current velocity.
    /// </summary>
    /// <param name="currentVelocity">The fish's current Rigidbody2D velocity.</param>
    public void UpdateVisuals(Vector2 currentVelocity)
    {
        float velocityMagnitude = currentVelocity.magnitude;

        // Determine the direction to use for visuals
        Vector2 visualDirection;
        if (velocityMagnitude > movementVelocityThreshold)
        {
            // If moving significantly, use current velocity direction
            visualDirection = currentVelocity.normalized;
            // Update last facing direction only if horizontal velocity is significant
            if (Mathf.Abs(currentVelocity.x) > flipVelocityThreshold * 0.5f) // Use a slightly lower threshold to update facing direction memory
            {
                lastFacingDirection = currentVelocity.normalized;
            }
        }
        else
        {
            // If moving very slowly or stopped, maintain last known facing direction for flip, but level out tilt
            visualDirection = new Vector2(lastFacingDirection.x, 0); // Use last horizontal, zero vertical for leveling
        }

        // Handle flipping and tilting
        HandleHorizontalFlip(visualDirection.x, currentVelocity.x); // Pass both ideal and actual horizontal velocity
        HandleVerticalTilt(visualDirection.y);
    }


    /// <summary>
    /// Handles flipping the sprite horizontally based on horizontal direction/velocity.
    /// </summary>
    /// <param name="horizontalDirection">Normalized horizontal direction derived from velocity or last known direction.</param>
    /// <param name="horizontalVelocity">Actual current horizontal velocity.</param>
    private void HandleHorizontalFlip(float horizontalDirection, float horizontalVelocity)
    {
        // Check cooldown
        if (Time.time < lastFlipTime + flipCooldown)
            return;

        // Check if absolute *actual* velocity is enough to warrant considering a flip
        if (Mathf.Abs(horizontalVelocity) > flipVelocityThreshold)
        {
            // Determine desired facing direction based on the visual direction
            bool shouldFaceRight = horizontalDirection > 0.01f; // Use small tolerance
            bool shouldFaceLeft = horizontalDirection < -0.01f;

            // Only flip if the desired direction is clear and different from the current one
            if ((shouldFaceRight && !isCurrentlyFacingRight) || (shouldFaceLeft && isCurrentlyFacingRight))
            {
                isCurrentlyFacingRight = !isCurrentlyFacingRight; // Flip the state
                lastFlipTime = Time.time;

                float targetScaleX = originalScaleX * (isCurrentlyFacingRight ? 1f : -1f);
                if (!spriteFacesRightInitially)
                {
                    targetScaleX *= -1f;
                }

                // Apply the new scale
                // Consider Lerping scale for smoother flip? For now, instant flip.
                spriteTransform.localScale = new Vector3(targetScaleX, spriteTransform.localScale.y, spriteTransform.localScale.z);
            }
        }
        // If velocity is below threshold, maintain current facing direction.
    }

    /// <summary>
    /// Handles tilting the sprite up or down based on vertical direction.
    /// </summary>
    private void HandleVerticalTilt(float verticalDirection)
    {
        // Calculate target tilt based on vertical direction component
        // Clamp verticalDirection just in case magnitude was slightly > 1
        float clampedVertical = Mathf.Clamp(verticalDirection, -1f, 1f);
        float targetTiltZ = clampedVertical * maxTiltAngle;

        // If facing left, invert the target tilt angle for intuitive control
        if (!isCurrentlyFacingRight)
        {
            targetTiltZ *= -1f;
        }

        // Get current Z rotation (ensure it's handled correctly)
        float currentTiltZ = spriteTransform.localEulerAngles.z;

        // Use SmoothDampAngle for smooth rotation towards the target tilt
        // It correctly handles angle wrapping (e.g., from 359 to 1 degree)
        float smoothTiltZ = Mathf.SmoothDampAngle(currentTiltZ, targetTiltZ,
                                                 ref currentTiltVelocity, tiltSmoothTime);

        // Apply the smoothed rotation
        spriteTransform.localRotation = Quaternion.Euler(
            spriteTransform.localEulerAngles.x, // Keep original X rotation
            spriteTransform.localEulerAngles.y, // Keep original Y rotation
            smoothTiltZ
        );
    }

    public void ResetVisuals()
    {
        // Reset tilt smoothly towards 0
        float currentTiltZ = spriteTransform.localEulerAngles.z;
        float smoothTiltZ = Mathf.SmoothDampAngle(currentTiltZ, 0f, ref currentTiltVelocity, tiltSmoothTime * 0.5f); // Faster reset
        spriteTransform.localRotation = Quaternion.Euler(spriteTransform.localEulerAngles.x, spriteTransform.localEulerAngles.y, smoothTiltZ);

        // Reset flip to initial direction instantly
        isCurrentlyFacingRight = spriteFacesRightInitially;
        float initialScaleX = originalScaleX * (isCurrentlyFacingRight ? 1f : -1f);
        if (!spriteFacesRightInitially) initialScaleX *= -1f;
        spriteTransform.localScale = new Vector3(initialScaleX, spriteTransform.localScale.y, spriteTransform.localScale.z);

        lastFacingDirection = isCurrentlyFacingRight ? Vector2.right : Vector2.left;
    }
}