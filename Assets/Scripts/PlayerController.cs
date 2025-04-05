// PlayerController.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // Make sure this is included

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

    private PlayerInputActions inputActions; // Your Input Actions asset class

    [Header("Visuals & Effects References")]
    public FishSquisher fishSquisher;
    public FishVisualController fishVisuals;
    public PlayerSoundController playerSoundController;

    // --- NEW REFERENCE ---
    [Header("Body Control")]
    [Tooltip("Reference to the BodyController managing mouth open/close visuals.")]
    public BodyController bodyController; // << ADD THIS REFERENCE

    [Header("Flutter drivers")]
    [SerializeField]
    private List<FlutterDriver> flutterDrivers = new List<FlutterDriver>();

    // Track the current mouth state
    private bool isMouthOpen = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inputActions = new PlayerInputActions(); // Initialize your Input Actions

        // --- Auto-find references if not set in Inspector ---
        if (fishVisuals == null) fishVisuals = GetComponentInChildren<FishVisualController>() ?? GetComponentInParent<FishVisualController>() ?? GetComponent<FishVisualController>();
        if (fishSquisher == null) fishSquisher = GetComponentInChildren<FishSquisher>() ?? GetComponentInParent<FishSquisher>() ?? GetComponent<FishSquisher>();
        if (playerSoundController == null) playerSoundController = GetComponentInChildren<PlayerSoundController>() ?? GetComponentInParent<PlayerSoundController>() ?? GetComponent<PlayerSoundController>();

        // --- >>> TRY TO FIND BODY CONTROLLER <<< ---
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

        // --- >>> CHECK BODY CONTROLLER <<< ---
        if (bodyController == null)
            Debug.LogWarning("PlayerController: BodyController reference not found. Mouth control will not work.", this);
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Move.performed += OnMovePerformed;
        inputActions.Player.Move.canceled += OnMoveCanceled;
        inputActions.Player.Dash.performed += OnDashPerformed;

        // --- >>> SUBSCRIBE TO MOUTH ACTIONS <<< ---
        // Use 'started' for press and 'canceled' for release for Button actions
        inputActions.Player.OpenMouth.started += OnMouthOpenStarted;
        inputActions.Player.OpenMouth.canceled += OnMouthOpenCanceled;
    }

    private void OnDisable()
    {
        // --- >>> UNSUBSCRIBE FROM MOUTH ACTIONS <<< ---
        inputActions.Player.OpenMouth.started -= OnMouthOpenStarted;
        inputActions.Player.OpenMouth.canceled -= OnMouthOpenCanceled;

        inputActions.Player.Disable();
        inputActions.Player.Move.performed -= OnMovePerformed;
        inputActions.Player.Move.canceled -= OnMoveCanceled;
        inputActions.Player.Dash.performed -= OnDashPerformed;

        if (rb != null) rb.linearVelocity = Vector2.zero;
        moveInput = Vector2.zero;
        isDashing = false;

        // --- Ensure mouth state is reset if disabled while open ---
        if (bodyController != null)
        {
            bodyController.SetMouthState(false); // Close mouth when player is disabled
        }
    }

    // --- Input Action Handlers ---

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }

    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        TryDash();
    }

    // --- >>> MOUTH CONTROL HANDLERS <<< ---
    private void OnMouthOpenStarted(InputAction.CallbackContext context)
    {
        if (bodyController != null)
        {
            bodyController.SetMouthState(true); // Open the mouth
            isMouthOpen = true;
        }
        else
        {
            Debug.LogWarning("Tried to open mouth, but BodyController reference is missing!", this);
        }
    }

    private void OnMouthOpenCanceled(InputAction.CallbackContext context)
    {
        if (bodyController != null)
        {
            bodyController.SetMouthState(false); // Close the mouth

            // Only trigger the squish animation if the mouth was previously open
            if (isMouthOpen && fishSquisher != null)
            {
                fishSquisher.TriggerSquish(FishSquisher.SquishActionType.Eat);
            }

            isMouthOpen = false;
        }
        else
        {
            // Warning already shown in Awake/Start, but can add context here if needed
            // Debug.LogWarning("Tried to close mouth, but BodyController reference is missing!", this);
        }
    }

    // --- Update / FixedUpdate / TryDash (Keep your existing logic here) ---

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

    private void TryDash()
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
}