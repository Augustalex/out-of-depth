using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public class HappyAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Duration of the entire happy animation sequence in seconds")]
    public float animationDuration = 2.0f; // Total time for all circles

    [Tooltip("Radius of the circular path")]
    public float circleRadius = 1.0f;

    [Tooltip("Number of complete circles to perform during the animation")]
    public int numberOfCircles = 2;

    [Tooltip("Speed multiplier affects how fast the circles are completed within the duration")]
    public float speedMultiplier = 1.0f; // Can speed up/slow down the rate

    [Header("Animation Options")]
    [Tooltip("Whether to play the animation automatically on start")]
    public bool playOnStart = false;

    [Tooltip("Ensures the object returns exactly to its starting position and rotation")]
    public bool returnToStartPosition = true; // Keep this true for the requirement

    [Header("Events")]
    [Tooltip("Event triggered when animation completes")]
    public UnityEvent onAnimationComplete;

    // Private variables
    private List<Rigidbody2D> disabledRigidbodies = new List<Rigidbody2D>();
    private Vector3 initialPosition; // Store the exact start position
    private Quaternion initialRotation; // Store the exact start rotation
    private Coroutine animationCoroutine;
    private bool isAnimating = false;

    private void Start()
    {
        if (playOnStart)
        {
            PlayAnimation();
        }
    }

    /// <summary>
    /// Starts the happy animation sequence
    /// </summary>
    public void PlayAnimation()
    {
        if (isAnimating)
        {
            Debug.LogWarning("Animation is already playing.");
            return; // Don't start if already playing
        }

        // Start the animation coroutine
        animationCoroutine = StartCoroutine(AnimationSequence());
    }

    /// <summary>
    /// Stops the animation prematurely and restores state
    /// </summary>
    public void StopAnimation()
    {
        if (!isAnimating) return;

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        RestoreRigidbodies(); // Restore physics state regardless

        // If stopped early, decide whether to snap back or stay put
        // Current implementation snaps back if returnToStartPosition is true
        if (returnToStartPosition && initialPosition != null) // Check if initial state was stored
        {
            transform.position = initialPosition;
            transform.rotation = initialRotation;
        }

        isAnimating = false;
        // Note: onAnimationComplete usually fires only on natural completion.
        // You might want a separate event for forced stops.
    }

    private IEnumerator AnimationSequence()
    {
        isAnimating = true;
        initialPosition = transform.position; // Record start state precisely
        initialRotation = transform.rotation;

        // Disable physics interference
        DisableRigidbodies();

        float elapsedTime = 0f;
        float totalAngleToCover = numberOfCircles * 360f;

        // Main animation loop
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime * speedMultiplier;

            // Calculate progress (0 to 1) respecting the duration
            // Use Clamp01 to prevent overshoot if speedMultiplier is high or frame rate drops
            float normalizedTime = Mathf.Clamp01(elapsedTime / animationDuration);

            // Use easing for smoother start/end (optional, but often looks better)
            float easedTime = EaseInOutQuad(normalizedTime);

            // Calculate the current angle based on eased progress
            float currentAngle = easedTime * totalAngleToCover;
            float radians = currentAngle * Mathf.Deg2Rad;

            // --- Position Calculation ---
            // Calculate offset from the starting position (center of the circle)
            // Using Cos for X and Sin for Y makes it start moving roughly to the right
            // Adjust Sin/Cos or add offsets (e.g., +90 degrees) if you want a different start direction
            float xOffset = Mathf.Cos(radians) * circleRadius;
            float yOffset = Mathf.Sin(radians) * circleRadius;

            // Apply the offset to the initial position
            transform.position = initialPosition + new Vector3(xOffset, yOffset, 0);

            // --- Rotation Calculation (Face direction of movement) ---
            // Calculate a point slightly ahead in time to find the direction
            float lookAheadTime = Mathf.Clamp01((elapsedTime + Time.deltaTime) / animationDuration); // Look ahead one frame
            float lookAheadEased = EaseInOutQuad(lookAheadTime);
            float lookAheadAngle = lookAheadEased * totalAngleToCover;
            float lookAheadRadians = lookAheadAngle * Mathf.Deg2Rad;

            float nextXOffset = Mathf.Cos(lookAheadRadians) * circleRadius;
            float nextYOffset = Mathf.Sin(lookAheadRadians) * circleRadius;

            // Vector pointing from current position to next position
            Vector3 moveDirection = new Vector3(nextXOffset - xOffset, nextYOffset - yOffset, 0);

            // Only update rotation if there is movement direction
            if (moveDirection.sqrMagnitude > 0.0001f) // Check square magnitude for efficiency
            {
                // Calculate angle from direction vector (Atan2 handles quadrants correctly)
                // Subtract 90 degrees if your sprite's "forward" is its Y+ axis
                float targetAngleZ = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg - 90f;
                // Smoothly rotate towards the target angle (optional, Slerp provides smoother visual rotation)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, targetAngleZ), Time.deltaTime * 10f); // Adjust 10f for rotation speed
                // Or snap instantly: transform.rotation = Quaternion.Euler(0, 0, targetAngleZ);
            }
            else if (elapsedTime == 0) // Set initial rotation based on first movement step if not already moving
            {
                // This helps if the object starts stationary before moving
                float targetAngleZ = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0, 0, targetAngleZ);
            }


            yield return null; // Wait for the next frame
        }

        // --- Animation Finished ---

        // Restore physics state
        RestoreRigidbodies();

        // Explicitly reset to the initial state if required
        if (returnToStartPosition)
        {
            transform.position = initialPosition;
            transform.rotation = initialRotation;
        }
        else
        {
            // Optional: Ensure it ends exactly at the calculated final frame position/rotation
            // This might be useful if !returnToStartPosition
            float finalRadians = (totalAngleToCover) * Mathf.Deg2Rad;
            float finalX = Mathf.Cos(finalRadians) * circleRadius;
            float finalY = Mathf.Sin(finalRadians) * circleRadius;
            transform.position = initialPosition + new Vector3(finalX, finalY, 0);
            // Could calculate final rotation here too if needed
        }

        isAnimating = false;

        // Invoke the completion event
        if (onAnimationComplete != null)
        {
            onAnimationComplete.Invoke();
        }
    }

    // Smooth easing function
    private float EaseInOutQuad(float t)
    {
        // Clamps t between 0 and 1
        t = Mathf.Clamp01(t);
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }

    // Disable/Restore Rigidbody functions remain the same as your original script
    private void DisableRigidbodies()
    {
        disabledRigidbodies.Clear();
        Rigidbody2D[] rigidbodies = GetComponentsInChildren<Rigidbody2D>(true);
        foreach (Rigidbody2D rb in rigidbodies)
        {
            if (rb != null && rb.simulated)
            {
                disabledRigidbodies.Add(rb);
                rb.linearVelocity = Vector2.zero; // Use velocity for Rigidbody2D
                rb.angularVelocity = 0f;
                rb.simulated = false; // Set simulated to false
            }
        }
    }

    private void RestoreRigidbodies()
    {
        foreach (Rigidbody2D rb in disabledRigidbodies)
        {
            if (rb != null)
            {
                rb.simulated = true; // Re-enable simulation
            }
        }
        disabledRigidbodies.Clear();
    }

    // Ensure the coroutine is stopped if the object is destroyed
    private void OnDestroy()
    {
        if (isAnimating && animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            // Optional: Decide if rigidbodies should be restored on destroy
            // RestoreRigidbodies();
        }
    }
}