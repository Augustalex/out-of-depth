// FishVisualController.cs:
using UnityEngine;

public class FishVisualController : MonoBehaviour
{
    [Header("Tilting")]
    [Tooltip("Maximum angle in degrees the fish tilts up or down based on vertical velocity.")]
    public float maxTiltAngle = 8f; // Further Reduced for subtlety
    [Tooltip("How quickly the fish smoothly rotates towards the target tilt angle (higher is slower/smoother).")]
    public float tiltSmoothTime = 0.3f; // Increased for slower/gentler tilting

    [Header("Flipping")]
    [Tooltip("Check this if your sprite asset faces right by default.")]
    public bool spriteFacesRightInitially = true;
    [Tooltip("Minimum horizontal *velocity* required to trigger a potential flip.")]
    public float flipVelocityThreshold = 0.4f; // Adjusted slightly
    [Tooltip("Minimum *velocity* magnitude required for the fish to be considered 'moving' for orientation purposes.")]
    public float movementVelocityThreshold = 0.15f; // Adjusted slightly
    [Tooltip("Minimum time (seconds) to wait before allowing another flip.")]
    public float flipCooldown = 0.5f; // Slightly increased

    // --- Private fields remain the same ---
    private Transform spriteTransform;
    private float originalScaleX;
    private bool isCurrentlyFacingRight = true;
    private Vector2 lastFacingDirection = Vector2.right;
    private float lastFlipTime = -Mathf.Infinity;
    private float currentTiltVelocityRef = 0f; // Renamed for SmoothDamp ref clarity

    void Awake()
    {
        spriteTransform = transform;
        originalScaleX = Mathf.Abs(spriteTransform.localScale.x);
        isCurrentlyFacingRight = (spriteTransform.localScale.x > 0) == spriteFacesRightInitially;
        lastFacingDirection = isCurrentlyFacingRight ? Vector2.right : Vector2.left;
    }

    /// <summary>
    /// Updates the fish sprite's orientation based on its current velocity.
    /// Ensures fish returns to horizontal when vertical movement is low.
    /// </summary>
    public void UpdateVisuals(Vector2 currentVelocity)
    {
        float velocityMagnitude = currentVelocity.magnitude;

        // Determine the direction to use for visuals
        Vector2 visualDirection;

        if (velocityMagnitude > movementVelocityThreshold)
        {
            // Moving: Use current velocity direction.
            visualDirection = currentVelocity.normalized;

            // Update last *horizontal* facing direction memory if moving horizontally enough.
            if (Mathf.Abs(currentVelocity.x) > flipVelocityThreshold * 0.5f)
            {
                // Store the horizontal component direction bias
                lastFacingDirection.x = Mathf.Sign(currentVelocity.x);
            }
            // Store the vertical component for tilting
            lastFacingDirection.y = visualDirection.y; // Store normalized vertical component
        }
        else
        {
            // Slow or Stopped: Maintain last horizontal facing direction, force vertical direction to zero for leveling.
            visualDirection = new Vector2(lastFacingDirection.x, 0);
            // Keep lastFacingDirection.y as it was, but use 0 for visualDirection.y
        }

        // Handle flipping and tilting using the calculated visual direction
        HandleHorizontalFlip(visualDirection.x, currentVelocity.x);
        HandleVerticalTilt(visualDirection.y); // Pass the potentially zeroed vertical component
    }


    private void HandleHorizontalFlip(float horizontalDirection, float horizontalVelocity)
    {
        if (Time.time < lastFlipTime + flipCooldown) return;

        // Use absolute *actual* velocity for the flip threshold check
        if (Mathf.Abs(horizontalVelocity) > flipVelocityThreshold)
        {
            bool shouldFaceRight = horizontalDirection > 0.01f;
            bool shouldFaceLeft = horizontalDirection < -0.01f;

            if ((shouldFaceRight && !isCurrentlyFacingRight) || (shouldFaceLeft && isCurrentlyFacingRight))
            {
                isCurrentlyFacingRight = !isCurrentlyFacingRight;
                lastFlipTime = Time.time;

                float targetScaleX = originalScaleX * (isCurrentlyFacingRight ? 1f : -1f);
                if (!spriteFacesRightInitially) { targetScaleX *= -1f; }

                spriteTransform.localScale = new Vector3(targetScaleX, spriteTransform.localScale.y, spriteTransform.localScale.z);
            }
        }
    }


    private void HandleVerticalTilt(float verticalDirection)
    {
        // Calculate target tilt based on the vertical component of visualDirection.
        // If visualDirection.y is 0 (because fish is slow/stopped), targetTiltZ will be 0.
        float targetTiltZ = Mathf.Clamp(verticalDirection, -1f, 1f) * maxTiltAngle;

        if (!isCurrentlyFacingRight) { targetTiltZ *= -1f; } // Invert tilt if facing left

        float currentTiltZ = spriteTransform.localEulerAngles.z;

        // Smoothly damp towards the target tilt (which will be 0 if verticalDirection is 0)
        float smoothTiltZ = Mathf.SmoothDampAngle(currentTiltZ, targetTiltZ,
                                                 ref currentTiltVelocityRef, tiltSmoothTime, Mathf.Infinity, Time.deltaTime); // Use Time.deltaTime here for frame-rate independence

        spriteTransform.localRotation = Quaternion.Euler(
            spriteTransform.localEulerAngles.x,
            spriteTransform.localEulerAngles.y,
            smoothTiltZ
        );
    }

    // --- ResetVisuals remains the same ---
    public void ResetVisuals()
    {
        float currentTiltZ = spriteTransform.localEulerAngles.z;
        float smoothTiltZ = Mathf.SmoothDampAngle(currentTiltZ, 0f, ref currentTiltVelocityRef, tiltSmoothTime * 0.5f); // Faster reset
        spriteTransform.localRotation = Quaternion.Euler(spriteTransform.localEulerAngles.x, spriteTransform.localEulerAngles.y, smoothTiltZ);

        isCurrentlyFacingRight = spriteFacesRightInitially;
        float initialScaleX = originalScaleX * (isCurrentlyFacingRight ? 1f : -1f);
        if (!spriteFacesRightInitially) initialScaleX *= -1f;
        spriteTransform.localScale = new Vector3(initialScaleX, spriteTransform.localScale.y, spriteTransform.localScale.z);

        lastFacingDirection = isCurrentlyFacingRight ? Vector2.right : Vector2.left;
    }
}