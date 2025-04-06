// PlayerController.cs
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(FishData))]
public class FishController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3.5f; // Reduced from 5f for calmer movement
    public float dashSpeed = 10f; // Reduced from 15f
    public float dashDuration = 0.3f; // Increased from 0.2f for more deliberate dashes
    public float dashCooldown = 1.5f; // Increased from 1f
    public float dashImpulse = 8f; // Reduced from 10f

    [Header("Rotation Settings")]
    [Tooltip("How quickly the fish rotates to face movement direction")]
    public float rotationSpeed = 3f; // Reduced from 5f
    [Tooltip("How quickly the player returns to intended rotation")]
    public float rotationStabilitySpeed = 5f; // Reduced from 10f
    [Tooltip("The intended rotation angle in degrees (Z-axis)")]
    public float targetRotationAngle = 0f;
    [Tooltip("Minimum velocity required to apply rotation correction")]
    public float minVelocityForRotationCorrection = 0.3f; // Increased from 0.1f
    [Tooltip("Maximum tilt angle when moving up or down")]
    public float maxTiltAngle = 10f; // Reduced from 15f

    [Header("Stability Settings")]
    [Tooltip("Value below which velocities are considered zero for stability")]
    public float velocityDeadZone = 0.2f; // New stability setting
    [Tooltip("How strongly velocity is smoothed for visual updates")]
    public float visualSmoothingFactor = 0.8f; // New stability setting

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 smoothedVelocity = Vector2.zero; // For smoothing out visual updates
    private bool isDashing = false;
    private float dashEndTime;
    private float lastDashTime = -Mathf.Infinity;
    private bool isDashButtonHeld = false;

    [Header("Visuals & Effects References")]
    public FishSquisher fishSquisher;
    public FishVisualController fishVisuals;
    public PlayerSoundController playerSoundController;

    [Header("Flutter drivers")]
    [SerializeField]
    private List<FlutterDriver> flutterDrivers = new List<FlutterDriver>();

    [Header("Camera Control")]
    [SerializeField] private PlayerCameraController cameraController;

    private FishData fishData;

    // Flag to identify if this is controlled by an AI
    private bool isAIControlled = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        fishData = GetComponent<FishData>();

        // --- Auto-find references if not set in Inspector ---
        if (fishVisuals == null) fishVisuals = GetComponentInChildren<FishVisualController>() ?? GetComponentInParent<FishVisualController>() ?? GetComponent<FishVisualController>();
        if (fishSquisher == null) fishSquisher = GetComponentInChildren<FishSquisher>() ?? GetComponentInParent<FishSquisher>() ?? GetComponent<FishSquisher>();
        if (playerSoundController == null) playerSoundController = GetComponentInChildren<PlayerSoundController>() ?? GetComponentInParent<PlayerSoundController>() ?? GetComponent<PlayerSoundController>();

        // Find camera controller if not set
        if (cameraController == null)
        {
            cameraController = FindObjectOfType<PlayerCameraController>();
        }

        // Apply initial scale if FishData is available
        if (fishData != null)
        {
            transform.localScale = fishData.InitialScale;
        }

        // --- Error Checks ---
        if (fishVisuals == null) Debug.LogWarning("FishController: FishVisualController reference not found.", this);
        if (fishSquisher == null) Debug.LogWarning("FishController: FishSquisher reference not found.", this);
        if (playerSoundController == null) Debug.LogWarning("FishController: PlayerSoundController reference not found. Dash sounds will not play.", this);
        if (cameraController == null) Debug.LogWarning("FishController: PlayerCameraController reference not found. Velocity-based camera zoom will not work.", this);
        if (fishData == null) Debug.LogWarning("FishController: FishData reference not found. Initial scale will not be applied.", this);
    }

    // --- Public API methods for PlayerInputManager or AI systems ---

    /// <summary>
    /// Sets the direction the fish should move. Normalized internally.
    /// </summary>
    public void SetMoveInput(Vector2 input)
    {
        moveInput = input;

        // Update visuals based on movement input
        if (fishVisuals != null)
        {
            // Get current velocity and apply deadzone for stability
            Vector2 currentVelocity = rb.linearVelocity;

            // Apply velocity deadzone for more stability
            if (currentVelocity.magnitude < velocityDeadZone)
            {
                currentVelocity = Vector2.zero;
            }

            // If we have zero velocity, just use raw input for visuals
            if (currentVelocity.magnitude < 0.001f)
            {
                fishVisuals.UpdateVisuals(input);
                return;
            }

            // Smooth out velocity changes to prevent erratic visual updates
            smoothedVelocity = Vector2.Lerp(smoothedVelocity, currentVelocity, Time.deltaTime * (1.0f - visualSmoothingFactor));

            // Normalize direction vectors
            Vector2 velocityDirection = smoothedVelocity.normalized;

            // More weight to velocity at higher speeds, but input has priority at low speeds
            float velocityWeight = Mathf.Clamp01(smoothedVelocity.magnitude / moveSpeed);

            // When not actively moving (input close to zero), reduce influence even more
            if (input.magnitude < 0.1f)
            {
                velocityWeight *= 0.5f;
            }

            // Blend input with smoothed velocity direction for visual updates
            Vector2 blendedInput = Vector2.Lerp(input, velocityDirection, velocityWeight * 0.7f);

            // Apply to visuals
            fishVisuals.UpdateVisuals(blendedInput);
        }
    }

    /// <summary>
    /// Marks this controller as AI-controlled
    /// </summary>
    public void SetAIControlled(bool isAI)
    {
        isAIControlled = isAI;
    }

    public void ResetState()
    {
        if (rb != null) rb.linearVelocity = Vector2.zero;
        moveInput = Vector2.zero;
        isDashing = false;
    }

    public void TryDash()
    {
        if (!isDashing && Time.time >= lastDashTime + dashCooldown && moveInput != Vector2.zero)
        {
            isDashing = true;
            isDashButtonHeld = true;
            dashEndTime = Time.time + dashDuration;
            lastDashTime = Time.time;

            if (fishSquisher != null) fishSquisher.TriggerSquish(FishSquisher.SquishActionType.Dash);
            if (playerSoundController != null) playerSoundController.PlayDashSound();

            // Initial impulse to start the sprint
            Vector2 dashDirection = moveInput.normalized;
            rb.AddForce(dashDirection * dashImpulse, ForceMode2D.Impulse);
        }
    }

    public void ReleaseDash()
    {
        isDashButtonHeld = false;
    }

    private void Update()
    {
        // End dash if max duration is reached
        if (isDashing && Time.time >= dashEndTime)
        {
            isDashing = false;
            isDashButtonHeld = false;
        }

        // Update flutter drivers with velocity magnitude
        foreach (var driver in flutterDrivers)
        {
            driver.SetVelocity(rb.linearVelocity.magnitude);
        }
    }

    private void FixedUpdate()
    {
        ApplyMovement();
        ApplyRotationStability();
    }

    private void ApplyMovement()
    {
        // Calculate normalized move direction, with zero protection
        Vector2 currentMoveDirection = moveInput.magnitude > 0.01f ? moveInput.normalized : Vector2.zero;
        Vector2 targetVelocity = Vector2.zero;

        if (isDashing)
        {
            // If dash button is released or we've reached max duration, end the dash
            if (!isDashButtonHeld || Time.time >= dashEndTime)
            {
                isDashing = false;
                isDashButtonHeld = false;
                targetVelocity = currentMoveDirection * moveSpeed;
            }
            else
            {
                // Continue sprinting in the direction of movement input
                targetVelocity = currentMoveDirection * dashSpeed;
            }
        }
        else
        {
            // Only apply movement if input direction is significant
            if (currentMoveDirection.magnitude > 0.01f)
            {
                targetVelocity = currentMoveDirection * moveSpeed;
            }
            else
            {
                // When no input, gradually slow down rather than stopping instantly
                targetVelocity = rb.linearVelocity * 0.95f; // 5% velocity reduction per physics frame

                // If we're going very slow, just stop completely (prevents drift)
                if (targetVelocity.magnitude < velocityDeadZone * 0.5f)
                {
                    targetVelocity = Vector2.zero;
                }
            }
        }

        rb.linearVelocity = targetVelocity;
    }

    private void ApplyRotationStability()
    {
        // For AI-controlled fish, let the tilt be controlled by movement (FishVisualController handles this)
        if (isAIControlled)
            return;

        // Apply rotation stability when player is moving
        if (rb.linearVelocity.magnitude > minVelocityForRotationCorrection)
        {
            // Get current rotation and calculate the difference to target
            float currentRotation = transform.eulerAngles.z;

            // Normalize current rotation to -180 to 180 range for easier calculations
            if (currentRotation > 180f)
                currentRotation -= 360f;

            // Find shortest path to target rotation
            float angleDifference = Mathf.DeltaAngle(currentRotation, targetRotationAngle);

            // Apply rotation correction with smoothing
            if (Mathf.Abs(angleDifference) > 0.1f)
            {
                float newRotation = Mathf.LerpAngle(currentRotation, targetRotationAngle,
                    rotationStabilitySpeed * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0, 0, newRotation);
            }
        }
    }
}