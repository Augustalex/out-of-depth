// PlayerController.cs
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    public float dashImpulse = 10f;

    [Header("Rotation Stability")]
    [Tooltip("How quickly the player returns to intended rotation")]
    public float rotationStabilitySpeed = 10f;
    [Tooltip("The intended rotation angle in degrees (Z-axis)")]
    public float targetRotationAngle = 0f;
    [Tooltip("Minimum velocity required to apply rotation correction")]
    public float minVelocityForRotationCorrection = 0.1f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isDashing = false;
    private float dashEndTime;
    private float lastDashTime = -Mathf.Infinity;
    private bool isDashButtonHeld = false; // New variable to track if dash button is being held

    [Header("Visuals & Effects References")]
    public FishSquisher fishSquisher;
    public FishVisualController fishVisuals;
    public PlayerSoundController playerSoundController;

    [Header("Body Control")]
    [Tooltip("Reference to the BodyController managing mouth open/close visuals.")]
    public BodyController bodyController;

    [Header("Flutter drivers")]
    [SerializeField]
    private List<FlutterDriver> flutterDrivers = new List<FlutterDriver>();

    [Header("Camera Control")]
    [SerializeField] private PlayerCameraController cameraController;

    // Track the current mouth state
    private bool isMouthOpen = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // --- Auto-find references if not set in Inspector ---
        if (fishVisuals == null) fishVisuals = GetComponentInChildren<FishVisualController>() ?? GetComponentInParent<FishVisualController>() ?? GetComponent<FishVisualController>();
        if (fishSquisher == null) fishSquisher = GetComponentInChildren<FishSquisher>() ?? GetComponentInParent<FishSquisher>() ?? GetComponent<FishSquisher>();
        if (playerSoundController == null) playerSoundController = GetComponentInChildren<PlayerSoundController>() ?? GetComponentInParent<PlayerSoundController>() ?? GetComponent<PlayerSoundController>();

        if (bodyController == null)
        {
            bodyController = GetComponentInChildren<BodyController>(); // Often on a child object
            if (bodyController == null) // Or maybe on the same object?
                bodyController = GetComponent<BodyController>();
            if (bodyController == null) // Or parent? Less likely but possible
                bodyController = GetComponentInParent<BodyController>();
        }

        // Find camera controller if not set
        if (cameraController == null)
        {
            cameraController = FindObjectOfType<PlayerCameraController>();
        }

        // --- Error Checks ---
        if (fishVisuals == null) Debug.LogWarning("PlayerController: FishVisualController reference not found.", this);
        if (fishSquisher == null) Debug.LogWarning("PlayerController: FishSquisher reference not found.", this);
        if (playerSoundController == null) Debug.LogWarning("PlayerController: PlayerSoundController reference not found. Dash sounds will not play.", this);
        if (cameraController == null) Debug.LogWarning("PlayerController: PlayerCameraController reference not found. Velocity-based camera zoom will not work.", this);

        if (bodyController == null)
            Debug.LogWarning("PlayerController: BodyController reference not found. Mouth control will not work.", this);
    }

    // --- Public API methods for PlayerInputManager ---

    public void SetMoveInput(Vector2 input)
    {
        moveInput = input;
    }

    public void SetMouthState(bool isOpen)
    {
        if (bodyController != null)
        {
            bodyController.SetMouthState(isOpen);

            // If we're closing the mouth and it was previously open, trigger squish
            if (!isOpen && isMouthOpen)
            {
                if (fishSquisher != null)
                {
                    fishSquisher.TriggerSquish(FishSquisher.SquishActionType.Eat);
                }

                // Play eat sound when mouth closes
                if (playerSoundController != null)
                {
                    playerSoundController.PlayEatSound();
                }
            }

            isMouthOpen = isOpen;
        }
        else if (isOpen) // Only log warning when trying to open mouth
        {
            Debug.LogWarning("Tried to change mouth state, but BodyController reference is missing!", this);
        }
    }

    public void ResetState()
    {
        if (rb != null) rb.linearVelocity = Vector2.zero;
        moveInput = Vector2.zero;
        isDashing = false;

        // Ensure mouth state is reset
        if (bodyController != null)
        {
            bodyController.SetMouthState(false); // Close mouth
            isMouthOpen = false;
        }
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

    // New method to handle releasing the dash button
    public void ReleaseDash()
    {
        isDashButtonHeld = false;
    }

    private void Update()
    {
        if (fishVisuals != null)
        {
            fishVisuals.UpdateVisuals(moveInput);
        }

        // End dash if max duration is reached
        if (isDashing && Time.time >= dashEndTime)
        {
            isDashing = false;
            isDashButtonHeld = false;
        }

        foreach (var driver in flutterDrivers)
        {
            driver.SetVelocity(rb.linearVelocity.magnitude);
        }

        // The camera controller now directly accesses the Rigidbody2D
        // No need to update it with velocity information


        Vector2 currentMoveDirection = moveInput.normalized;
        Vector2 targetVelocity;

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
            targetVelocity = currentMoveDirection * moveSpeed;
        }

        rb.linearVelocity = targetVelocity;

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