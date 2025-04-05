using System.Collections.Generic;
using UnityEngine;

// You might need this if PlayerInputActions is in a specific namespace
// using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))] // Good practice to require Rigidbody2D
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    public float dashImpulse = 10f; // Keep this if you like the impulse feel

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isDashing = false;
    private float dashEndTime; // Renamed for clarity
    private float lastDashTime = -Mathf.Infinity; // Initialize to allow dashing immediately

    private PlayerInputActions inputActions;

    [Header("Visuals & Effects References")]
    public FishSquisher fishSquisher; // Reference to the squish script
    public FishVisualController fishVisuals; // Reference to the visual controller

    [Header("Flutter drivers")]
    [SerializeField] // Expose in Inspector but keep private
    private List<FlutterDriver> flutterDrivers = new List<FlutterDriver>();


    [Header("Sound References")] // << NEW SECTION
    public PlayerSoundController playerSoundController; // << ADD THIS REFERENCE

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>(); // Get Rigidbody early
        inputActions = new PlayerInputActions();

        // --- Auto-find references if not set in Inspector (Good Practice) ---
        if (fishVisuals == null)
        {
            fishVisuals = GetComponentInChildren<FishVisualController>();
            if (fishVisuals == null) // Still null? Maybe it's on the parent or same object
                fishVisuals = GetComponentInParent<FishVisualController>() ?? GetComponent<FishVisualController>();
        }
        if (fishSquisher == null)
        {
            fishSquisher = GetComponentInChildren<FishSquisher>();
            if (fishSquisher == null)
                fishSquisher = GetComponentInParent<FishSquisher>() ?? GetComponent<FishSquisher>();
        }
        if (playerSoundController == null) // << TRY TO FIND SOUND CONTROLLER
        {
            playerSoundController = GetComponentInChildren<PlayerSoundController>();
            if (playerSoundController == null)
                playerSoundController = GetComponentInParent<PlayerSoundController>() ?? GetComponent<PlayerSoundController>();
        }

        // --- Error Checks ---
        if (fishVisuals == null)
            Debug.LogWarning("PlayerController: FishVisualController reference not found.", this);
        if (fishSquisher == null)
            Debug.LogWarning("PlayerController: FishSquisher reference not found.", this);
        if (playerSoundController == null) // << CHECK SOUND CONTROLLER
            Debug.LogWarning("PlayerController: PlayerSoundController reference not found. Dash sounds will not play.", this);
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Move.performed += OnMovePerformed;
        inputActions.Player.Move.canceled += OnMoveCanceled;
        inputActions.Player.Dash.performed += OnDashPerformed;
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
        inputActions.Player.Move.performed -= OnMovePerformed;
        inputActions.Player.Move.canceled -= OnMoveCanceled;
        inputActions.Player.Dash.performed -= OnDashPerformed;

        // Reset velocity on disable to prevent drifting if the game is paused/unpaused
        if (rb != null) rb.linearVelocity = Vector2.zero;
        moveInput = Vector2.zero; // Reset input state
        isDashing = false; // Reset dash state
    }

    // Using named methods for input for better readability and easier removal in OnDisable
    private void OnMovePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }

    private void OnDashPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        TryDash();
    }


    private void Update()
    {
        // --- Visual Updates ---
        if (fishVisuals != null)
        {
            fishVisuals.UpdateVisuals(moveInput);
        }

        // --- Dash Timer ---
        if (isDashing && Time.time >= dashEndTime)
        {
            isDashing = false;
            // You might want to reset velocity smoothly here instead of abruptly stopping
            // For now, FixedUpdate will handle setting velocity based on moveInput
        }

        foreach (var driver in flutterDrivers)
        {
            driver.SetVelocity(rb.linearVelocity.magnitude); // Pass the current speed to the flutter drivers
        }
    }


    private void FixedUpdate()
    {
        // Store the direction we intend to move/dash in
        Vector2 currentMoveDirection = moveInput.normalized; // Normalize for consistent direction

        Vector2 targetVelocity;
        if (isDashing)
        {
            // Maintain dash speed in the *original* dash direction (usually feels better)
            // If you want the dash to follow input changes, use currentMoveDirection here.
            // Assuming we want to dash in the direction we started moving:
            // We already applied impulse in TryDash, now mainly rely on velocity override.
            // Note: The impulse adds an initial burst, velocity maintains speed.
            // Let's get the direction from the *current* velocity if dashing, or input if starting
            Vector2 dashDirection = rb.linearVelocity.normalized; // Use current direction of movement
            if (dashDirection == Vector2.zero) dashDirection = currentMoveDirection; // Fallback if somehow stopped mid-dash

            targetVelocity = dashDirection * dashSpeed;
        }
        else
        {
            // Normal movement
            targetVelocity = currentMoveDirection * moveSpeed;
        }

        // Apply the calculated velocity
        rb.linearVelocity = targetVelocity;
    }

    private void TryDash()
    {
        // Check cooldown, if currently dashing, and if there's movement input
        if (!isDashing && Time.time >= lastDashTime + dashCooldown && moveInput != Vector2.zero)
        {
            isDashing = true;
            dashEndTime = Time.time + dashDuration;
            lastDashTime = Time.time;

            // Store the direction *at the start* of the dash
            Vector2 dashDirection = moveInput.normalized;

            // Apply effects and sound *before* physics changes if possible
            // Trigger the Dash squish animation
            if (fishSquisher != null)
            {
                fishSquisher.TriggerSquish(FishSquisher.SquishActionType.Dash);
            }

            // --- >>> Play the Dash Sound <<< ---
            if (playerSoundController != null)
            {
                playerSoundController.PlayDashSound(); // Call the sound controller!
            }
            else
            {
                // Warning already shown in Awake, but can add another here if preferred
                // Debug.LogWarning("Dash triggered, but PlayerSoundController reference is missing!", this);
            }

            // --- Physics ---
            // Option 1: Pure velocity change (handled in FixedUpdate)
            // rb.velocity = dashDirection * dashSpeed; // Set velocity directly (less 'punchy')

            // Option 2: Impulse + Velocity change (more 'punchy')
            // Reset velocity slightly before impulse to make impulse more pronounced if needed
            // rb.velocity = Vector2.zero; // Optional reset
            rb.AddForce(dashDirection * dashImpulse, ForceMode2D.Impulse);

            // Ensure FixedUpdate uses the correct speed right away if needed (can sometimes help responsiveness)
            rb.linearVelocity = dashDirection * dashSpeed; // Set this if impulse isn't enough or feels inconsistent

        }
    }
}