// FishController.cs:
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(FishData))]
public class FishController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3.0f; // Reduced
    public float acceleration = 5.0f; // How quickly the fish reaches moveSpeed
    public float deceleration = 8.0f; // How quickly the fish stops (higher is faster)
    public float dashSpeed = 8.0f;    // Reduced
    public float dashAcceleration = 15.0f; // How quickly it reaches dash speed
    public float dashDuration = 0.35f; // Slightly longer
    public float dashCooldown = 2.0f;  // Increased
    // Removed dashImpulse, acceleration handles the speed gain now

    // Removed Rotation Settings - Handled by FishVisualController now

    [Header("Stability Settings")]
    [Tooltip("Velocity magnitude below which the fish is considered stopped")]
    public float stopThreshold = 0.1f; // Renamed from velocityDeadZone for clarity

    // Removed visualSmoothingFactor - Visuals use velocity directly

    private Rigidbody2D rb;
    private Vector2 moveInput; // The desired direction of movement (normalized)
    private Vector2 currentVelocity; // Keep track of velocity for SmoothDamp
    private bool isDashing = false;
    private float dashEndTime;
    private float lastDashTime = -Mathf.Infinity;

    // Removed isDashButtonHeld - Dash logic simplified for AI/potential player use
    // Removed smoothedVelocity - Using rb.velocity directly for visual input

    [Header("Visuals & Effects References")]
    public FishVisualController fishVisuals;
    public PlayerSoundController playerSoundController; // Keep for dash sounds etc.
    public FishSquisher fishSquisher; // Keep for effects

    // Removed flutter drivers references - Add back if needed, logic remains the same

    [Header("Camera Control")]
    [SerializeField] private PlayerCameraController cameraController; // Keep if camera reacts to fish

    private FishData fishData;
    private bool isAIControlled = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        fishData = GetComponent<FishData>();

        // Ensure Rigidbody settings are suitable
        rb.gravityScale = 0; // Essential for side-view/top-down fish
        // **RECOMMENDATION:** Set Rigidbody2D Interpolate to 'Interpolate' for smoother visuals.
        // **RECOMMENDATION:** Set Rigidbody2D Collision Detection to 'Continuous' if fish move fast.
        // **RECOMMENDATION:** Add some Linear Drag (e.g., 1.0 to 3.0) to the Rigidbody2D. This helps with the deceleration logic.

        // --- Auto-find references ---
        if (fishVisuals == null) fishVisuals = GetComponentInChildren<FishVisualController>() ?? GetComponentInParent<FishVisualController>();
        if (fishSquisher == null) fishSquisher = GetComponentInChildren<FishSquisher>() ?? GetComponentInParent<FishSquisher>();
        if (playerSoundController == null) playerSoundController = GetComponentInChildren<PlayerSoundController>() ?? GetComponentInParent<PlayerSoundController>();
        if (cameraController == null) cameraController = FindObjectOfType<PlayerCameraController>();

        if (fishData != null) transform.localScale = fishData.InitialScale;

        // --- Error Checks ---
        if (fishVisuals == null) Debug.LogWarning("FishController: FishVisualController reference not found.", this);
        // Add other checks as needed

        // Initial velocity state
        currentVelocity = rb.linearVelocity;
    }


    public void SetMoveInput(Vector2 input)
    {
        // Store the desired movement direction (normalize to prevent speed hacks)
        if (input.sqrMagnitude > 0.01f)
        {
            moveInput = input.normalized;
        }
        else
        {
            moveInput = Vector2.zero;
        }
    }

    public void SetAIControlled(bool isAI)
    {
        isAIControlled = isAI;
    }

    public void ResetState()
    {
        rb.linearVelocity = Vector2.zero;
        currentVelocity = Vector2.zero;
        moveInput = Vector2.zero;
        isDashing = false;
        // Reset visuals?
        if (fishVisuals != null) fishVisuals.ResetVisuals();
    }

    public void TryDash()
    {
        // Allow dash only if moving and cooldown is ready
        if (!isDashing && Time.time >= lastDashTime + dashCooldown && moveInput.sqrMagnitude > 0.1f)
        {
            isDashing = true;
            dashEndTime = Time.time + dashDuration;
            lastDashTime = Time.time;

            if (fishSquisher != null) fishSquisher.TriggerSquish(FishSquisher.SquishActionType.Dash);
            if (playerSoundController != null) playerSoundController.PlayDashSound();

            // No impulse needed, acceleration handles the burst
        }
    }

    // Optional: Call if dash input is released early (for player control)
    public void ReleaseDash()
    {
        if (isDashing)
        {
            // End dash prematurely if needed
            dashEndTime = Time.time; // End it now
            isDashing = false;
        }
    }

    void Update()
    {
        // Update dash state based on time
        if (isDashing && Time.time >= dashEndTime)
        {
            isDashing = false;
        }

        // Send current velocity to visual controller
        if (fishVisuals != null)
        {
            fishVisuals.UpdateVisuals(rb.linearVelocity); // Use actual Rigidbody velocity
        }

        // Update Camera Controller if it exists
        if (cameraController != null)
        {
            // Example: cameraController.SetTargetVelocity(rb.velocity.magnitude);
        }
    }

    void FixedUpdate()
    {
        ApplySmoothedMovement();
    }

    void ApplySmoothedMovement()
    {
        float currentMaxSpeed = moveSpeed;
        float currentAcceleration = acceleration;

        // Determine target speed and acceleration based on state
        if (isDashing)
        {
            currentMaxSpeed = dashSpeed;
            currentAcceleration = dashAcceleration;
        }

        Vector2 targetVelocity;

        // If there's movement input, accelerate towards it
        if (moveInput.sqrMagnitude > 0.01f)
        {
            targetVelocity = moveInput * currentMaxSpeed;
            // Accelerate towards target velocity
            rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref currentVelocity, 1.0f / currentAcceleration, Mathf.Infinity, Time.fixedDeltaTime);

        }
        // If no input, decelerate smoothly to a stop
        else
        {
            targetVelocity = Vector2.zero;
            // Decelerate towards zero velocity
            // Using a slightly different approach for deceleration for potentially snappier stops
            rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref currentVelocity, 1.0f / deceleration, Mathf.Infinity, Time.fixedDeltaTime);

            // If velocity is very low, clamp to zero to prevent drifting
            if (rb.linearVelocity.sqrMagnitude < stopThreshold * stopThreshold)
            {
                rb.linearVelocity = Vector2.zero;
                currentVelocity = Vector2.zero; // Reset SmoothDamp velocity tracker
            }
        }
    }

    // Helper to get current velocity for the Agent's Gizmos
    public Vector2 GetCurrentVelocity()
    {
        return rb ? rb.linearVelocity : Vector2.zero;
    }

    // Removed ApplyRotationStability - Handled by Visual Controller
}