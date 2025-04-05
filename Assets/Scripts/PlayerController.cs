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

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isDashing = false;
    private float dashEndTime;
    private float lastDashTime = -Mathf.Infinity;

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

        // --- Error Checks ---
        if (fishVisuals == null) Debug.LogWarning("PlayerController: FishVisualController reference not found.", this);
        if (fishSquisher == null) Debug.LogWarning("PlayerController: FishSquisher reference not found.", this);
        if (playerSoundController == null) Debug.LogWarning("PlayerController: PlayerSoundController reference not found. Dash sounds will not play.", this);

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
            if (!isOpen && isMouthOpen && fishSquisher != null)
            {
                fishSquisher.TriggerSquish(FishSquisher.SquishActionType.Eat);
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
            dashEndTime = Time.time + dashDuration;
            lastDashTime = Time.time;
            Vector2 dashDirection = moveInput.normalized;

            if (fishSquisher != null) fishSquisher.TriggerSquish(FishSquisher.SquishActionType.Dash);
            if (playerSoundController != null) playerSoundController.PlayDashSound();

            rb.AddForce(dashDirection * dashImpulse, ForceMode2D.Impulse);
            rb.linearVelocity = dashDirection * dashSpeed;
        }
    }

    private void Update()
    {
        if (fishVisuals != null)
        {
            fishVisuals.UpdateVisuals(moveInput);
        }

        if (isDashing && Time.time >= dashEndTime)
        {
            isDashing = false;
        }

        foreach (var driver in flutterDrivers)
        {
            driver.SetVelocity(rb.linearVelocity.magnitude);
        }
    }

    private void FixedUpdate()
    {
        Vector2 currentMoveDirection = moveInput.normalized;
        Vector2 targetVelocity;

        if (isDashing)
        {
            Vector2 dashDirection = rb.linearVelocity.normalized;
            if (dashDirection == Vector2.zero) dashDirection = currentMoveDirection;
            targetVelocity = dashDirection * dashSpeed;
        }
        else
        {
            targetVelocity = currentMoveDirection * moveSpeed;
        }

        rb.linearVelocity = targetVelocity;
    }
}