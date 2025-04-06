// FishController.cs:
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(FishData))]
public class FishController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Target cruising speed")]
    public float moveSpeed = 2.0f; // Reduced
    [Tooltip("How quickly the fish reaches moveSpeed (lower is slower)")]
    public float acceleration = 3.0f; // Reduced
    [Tooltip("How quickly the fish stops (higher values + drag work together)")]
    public float deceleration = 5.0f; // Reduced, rely more on drag
    [Tooltip("Target speed during dash")]
    public float dashSpeed = 6.0f;    // Reduced
    [Tooltip("How quickly it reaches dash speed")]
    public float dashAcceleration = 10.0f; // Reduced
    public float dashDuration = 0.35f;
    public float dashCooldown = 2.5f;  // Slightly Increased

    [Header("Stability Settings")]
    [Tooltip("Velocity magnitude below which the fish is considered stopped")]
    public float stopThreshold = 0.05f; // Reduced threshold for stopping

    // --- Private fields remain the same ---
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 currentVelocityRef; // Renamed for SmoothDamp ref clarity
    private bool isDashing = false;
    private float dashEndTime;
    private float lastDashTime = -Mathf.Infinity;

    [Header("Visuals & Effects References")]
    public FishVisualController fishVisuals;
    public PlayerSoundController playerSoundController;
    public FishSquisher fishSquisher;

    [Header("Camera Control")]
    [SerializeField] private PlayerCameraController cameraController;

    private FishData fishData;
    private bool isAIControlled = false;


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        fishData = GetComponent<FishData>();

        // --- CRITICAL Rigidbody Settings ---
        rb.gravityScale = 0;
        // **RECOMMENDATION:** Set Rigidbody2D Interpolate to 'Interpolate'.
        // **RECOMMENDATION:** Set Rigidbody2D Collision Detection to 'Continuous'.
        // **RECOMMENDATION:** Set Rigidbody2D Linear Drag to a higher value (e.g., 2.0 to 5.0). This is KEY for calmer deceleration. Adjust as needed!

        // --- Auto-find references ---
        if (fishVisuals == null) fishVisuals = GetComponentInChildren<FishVisualController>() ?? GetComponentInParent<FishVisualController>();
        if (fishSquisher == null) fishSquisher = GetComponentInChildren<FishSquisher>() ?? GetComponentInParent<FishSquisher>();
        if (playerSoundController == null) playerSoundController = GetComponentInChildren<PlayerSoundController>() ?? GetComponentInParent<PlayerSoundController>();
        if (cameraController == null) cameraController = FindObjectOfType<PlayerCameraController>();

        if (fishData != null) transform.localScale = fishData.InitialScale;

        // Initialize SmoothDamp reference velocity
        currentVelocityRef = Vector2.zero;
    }

    // --- SetMoveInput, SetAIControlled, ResetState, TryDash, ReleaseDash remain the same ---
    public void SetMoveInput(Vector2 input)
    {
        if (input.sqrMagnitude > 0.01f) { moveInput = input.normalized; }
        else { moveInput = Vector2.zero; }
    }
    public void SetAIControlled(bool isAI) { isAIControlled = isAI; }
    public void ResetState()
    {
        rb.linearVelocity = Vector2.zero;
        currentVelocityRef = Vector2.zero;
        moveInput = Vector2.zero;
        isDashing = false;
        if (fishVisuals != null) fishVisuals.ResetVisuals();
    }
    public void TryDash()
    {
        if (!isDashing && Time.time >= lastDashTime + dashCooldown && moveInput.sqrMagnitude > 0.1f)
        {
            isDashing = true;
            dashEndTime = Time.time + dashDuration;
            lastDashTime = Time.time;
            if (fishSquisher != null) fishSquisher.TriggerSquish(FishSquisher.SquishActionType.Dash);
            if (playerSoundController != null) playerSoundController.PlayDashSound();
        }
    }
    public void ReleaseDash()
    {
        if (isDashing) { dashEndTime = Time.time; isDashing = false; }
    }


    void Update()
    {
        if (isDashing && Time.time >= dashEndTime)
        {
            isDashing = false;
        }

        if (fishVisuals != null)
        {
            // Pass velocity for visual updates (tilting/flipping)
            fishVisuals.UpdateVisuals(rb.linearVelocity);
        }

        if (cameraController != null)
        {
            // cameraController.SetTargetVelocity(rb.velocity.magnitude);
        }
    }

    void FixedUpdate()
    {
        ApplySmoothedMovement();
    }

    void ApplySmoothedMovement()
    {
        float targetMaxSpeed = isDashing ? dashSpeed : moveSpeed;
        float currentAccel = isDashing ? dashAcceleration : acceleration;

        Vector2 targetVelocity;

        // Target velocity based on input direction and max speed
        targetVelocity = moveInput * targetMaxSpeed;

        // Use SmoothDamp to approach the target velocity
        // Acceleration is controlled by SmoothDamp's smoothTime (calculated as 1/acceleration)
        // Deceleration is primarily handled by Rigidbody's Linear Drag, but SmoothDamp helps guide it to zero when input stops.
        float smoothTime = (moveInput.sqrMagnitude > 0.01f) ? (1.0f / currentAccel) : (1.0f / deceleration);

        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref currentVelocityRef, smoothTime, Mathf.Infinity, Time.fixedDeltaTime);

        // Force stop if velocity is very low and no input (works with drag)
        if (moveInput.sqrMagnitude < 0.01f && rb.linearVelocity.sqrMagnitude < stopThreshold * stopThreshold)
        {
            rb.linearVelocity = Vector2.zero;
            currentVelocityRef = Vector2.zero; // Reset SmoothDamp ref
        }
    }

    public Vector2 GetCurrentVelocity()
    {
        // Use rb.velocity if available, otherwise return zero. Check rb exists.
        return rb != null ? rb.linearVelocity : Vector2.zero;
    }
}