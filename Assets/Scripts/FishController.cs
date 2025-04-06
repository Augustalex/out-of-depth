// FishController.cs:
using UnityEngine;
using System.Collections.Generic; // Keep if using lists later

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(FishData))]
[RequireComponent(typeof(FishAgent))] // Add requirement for agent access
public class FishController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2.0f;
    public float acceleration = 3.0f;
    public float deceleration = 5.0f; // Works with Linear Drag
    [Tooltip("Target speed when idly wandering")]
    public float idleWanderSpeed = 0.75f; // NEW: Speed for calm floating
    public float dashSpeed = 6.0f;
    public float dashAcceleration = 10.0f;
    public float dashDuration = 0.35f;
    public float dashCooldown = 2.5f;

    [Header("Physics Rotation Correction")]
    [Tooltip("How strongly the fish tries to rotate back upright (torque force)")]
    public float physicsRotationCorrectionTorque = 5.0f; // NEW
    [Tooltip("Minimum angle difference (degrees) to trigger correction")]
    public float physicsRotationCorrectionThreshold = 2.0f; // NEW
    [Tooltip("Maximum torque applied to prevent excessive spinning")]
    public float maxCorrectionTorque = 10.0f; // NEW

    [Header("Stability Settings")]
    [Tooltip("Velocity magnitude below which the fish is considered stopped (used less now)")]
    public float stopThreshold = 0.05f; // Keep for potential full stop if needed later


    // --- Component References ---
    private Rigidbody2D rb;
    private FishData fishData;
    private FishAgent fishAgent; // Reference to the agent
    public FishVisualController fishVisuals;
    public PlayerSoundController playerSoundController;
    public FishSquisher fishSquisher;

    // --- Private State Variables ---
    private Vector2 moveInput;
    private Vector2 currentVelocityRef;
    private bool isDashing = false;
    private float dashEndTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        fishData = GetComponent<FishData>();
        fishAgent = GetComponent<FishAgent>(); // Get the agent component

        // --- CRITICAL Rigidbody Settings ---
        rb.gravityScale = 0;
        // **RECOMMENDATION:** Set Rigidbody2D Interpolate to 'Interpolate'.
        // **RECOMMENDATION:** Set Rigidbody2D Collision Detection to 'Continuous'.
        // **RECOMMENDATION:** Set Rigidbody2D Linear Drag to a higher value (e.g., 2.0 to 5.0).
        // **RECOMMENDATION:** Set Rigidbody2D Angular Drag (e.g., 1.0 to 5.0). This helps dampen rotation and works with the correction torque. Start with 2.0.

        // --- Auto-find & Error Checks ---
        if (fishVisuals == null) fishVisuals = GetComponentInChildren<FishVisualController>() ?? GetComponentInParent<FishVisualController>();
        // ... (other finds/checks)
        if (fishAgent == null) Debug.LogError("FishController requires a FishAgent component!", this);


        currentVelocityRef = Vector2.zero;
    }

    // --- SetMoveInput, SetAIControlled, ResetState, TryDash, ReleaseDash (No changes needed here) ---
    public void SetMoveInput(Vector2 input) { /* ... same ... */ }
    public void SetAIControlled(bool isAI) { /* ... same ... */ }
    public void ResetState() { /* ... same ... */ }
    public void TryDash() { /* ... same ... */ }
    public void ReleaseDash() { /* ... same ... */ }


    void Update()
    {
        if (isDashing && Time.time >= dashEndTime) { isDashing = false; }
        if (fishVisuals != null) { fishVisuals.UpdateVisuals(rb.linearVelocity); }
    }

    void FixedUpdate()
    {
        ApplySmoothedMovement();
        ApplyPhysicsRotationCorrection(); // Apply rotation correction every physics step
    }

    void ApplySmoothedMovement()
    {
        Vector2 targetDirection;
        float targetMaxSpeed;

        // Check if the Agent provided a primary movement direction
        if (moveInput.sqrMagnitude > 0.01f)
        {
            // Use the primary direction from the agent
            targetDirection = moveInput;
            targetMaxSpeed = isDashing ? dashSpeed : moveSpeed;
        }
        else
        {
            // No primary direction from agent, engage idle wander behavior
            targetDirection = fishAgent.CurrentWanderDirection; // Get wander direction
            targetMaxSpeed = idleWanderSpeed; // Use idle speed
        }

        // Ensure idle wander doesn't stop if wander direction is somehow zero
        if (targetDirection.sqrMagnitude < 0.01f && targetMaxSpeed == idleWanderSpeed)
        {
            // Fallback: if wander direction is zero, maybe pick a random direction or keep last velocity?
            // For now, let it target zero, drag should slow it down. Or use Vector2.right as default.
            targetDirection = Vector2.right; // Simple fallback if wander is zero
        }


        Vector2 targetVelocity = targetDirection * targetMaxSpeed;

        // Determine acceleration/deceleration for SmoothDamp
        float currentAccel = isDashing ? dashAcceleration : acceleration;
        // Use deceleration factor when target speed is lower than current speed, or when stopping/idling
        bool isDecelerating = targetVelocity.sqrMagnitude < rb.linearVelocity.sqrMagnitude || targetMaxSpeed <= idleWanderSpeed;
        float smoothTime = isDecelerating ? (1.0f / deceleration) : (1.0f / currentAccel);

        // Apply SmoothDamp towards the target velocity
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref currentVelocityRef, smoothTime, Mathf.Infinity, Time.fixedDeltaTime);

        // Optional: Remove or adjust the hard stop threshold if continuous idle movement is always desired
        // if (moveInput.sqrMagnitude < 0.01f && targetMaxSpeed <= idleWanderSpeed && rb.velocity.sqrMagnitude < stopThreshold * stopThreshold)
        // {
        //     rb.velocity = Vector2.zero; // This would stop the fish completely
        //     currentVelocityRef = Vector2.zero;
        // }
    }

    void ApplyPhysicsRotationCorrection()
    {
        // Get current rotation (Z-angle) from Rigidbody
        float currentAngle = rb.rotation;

        // Calculate the shortest difference to the target angle (0 degrees)
        float angleDifference = Mathf.DeltaAngle(currentAngle, 0f);

        // Only apply correction if the angle difference is significant
        if (Mathf.Abs(angleDifference) > physicsRotationCorrectionThreshold)
        {
            // Calculate desired torque: proportional to the angle difference
            // Negative sign because torque direction is opposite to angle difference needed for correction
            float targetTorque = -angleDifference * physicsRotationCorrectionTorque;

            // Clamp the torque to prevent excessive spinning
            targetTorque = Mathf.Clamp(targetTorque, -maxCorrectionTorque, maxCorrectionTorque);

            // Apply the torque impulse (adjust ForceMode if needed, Impulse is usually good for corrective nudges)
            rb.AddTorque(targetTorque * Time.fixedDeltaTime, ForceMode2D.Impulse);
        }
    }

    public Vector2 GetCurrentVelocity()
    {
        return rb != null ? rb.linearVelocity : Vector2.zero;
    }
}