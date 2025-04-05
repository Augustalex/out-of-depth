using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    public float dashImpulse = 10f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isDashing = false;
    private float dashTime;
    private float lastDashTime;

    private PlayerInputActions inputActions;

    [Header("Visuals & Effects References")]
    public FishSquisher fishSquisher; // Reference to the squish script
    public FishVisualController fishVisuals; // << ADD THIS REFERENCE

    private void Awake()
    {
        inputActions = new PlayerInputActions();

        // Try to find the visual controller automatically if not set in Inspector
        // Assumes it's on a child GameObject. If it's on the same object, use GetComponent.
        if (fishVisuals == null)
        {
            fishVisuals = GetComponentInChildren<FishVisualController>();
        }
        if (fishSquisher == null) // Also good to auto-find the squisher if possible
        {
            fishSquisher = GetComponentInChildren<FishSquisher>();
        }

        // Add error checks if references are crucial
        if (fishVisuals == null)
            Debug.LogError("PlayerController could not find FishVisualController!", this);
        if (fishSquisher == null)
            Debug.LogError("PlayerController could not find FishSquisher!", this);

    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        // Use lambda directly for simplicity if preferred
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero; // Ensure moveInput goes to zero
        inputActions.Player.Dash.performed += _ => TryDash();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
        // It's good practice to reset visuals/state on disable if needed
        if (fishVisuals != null)
        {
            // Optionally reset visuals when disabled
            // fishVisuals.ResetVisuals();
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // It's often better to handle visual updates in Update, after input is processed,
    // unless they directly depend on physics calculations done in FixedUpdate.
    // In this case, moveInput is updated by events, so Update or FixedUpdate works. Let's use Update.
    private void Update()
    {
        // --- Visual Updates ---
        // Check if the visual controller exists before calling it
        if (fishVisuals != null)
        {
            // Pass the current moveInput to the visual controller
            fishVisuals.UpdateVisuals(moveInput); // << CALL THE VISUAL UPDATE
        }

        // --- Dash Timer --- (Can stay in Update)
        if (isDashing && Time.time >= dashTime)
        {
            isDashing = false;
            // Optional: Maybe trigger a small "dash end" squish or effect here
        }
    }


    private void FixedUpdate()
    {
        // Physics calculations remain in FixedUpdate
        Vector2 targetVelocity;
        if (isDashing)
        {
            // Note: Using moveInput for dash direction might feel weird if input changes mid-dash.
            // Consider caching the dash direction when the dash starts.
            // For now, using current moveInput as per original script.
            targetVelocity = moveInput.normalized * dashSpeed; // Use normalized input for consistent dash speed
        }
        else
        {
            targetVelocity = moveInput * moveSpeed;
        }

        // Apply velocity directly for arcade-style movement
        rb.linearVelocity = targetVelocity;
    }

    private void TryDash()
    {
        // Ensure not already dashing and cooldown is met
        if (!isDashing && Time.time >= lastDashTime + dashCooldown && moveInput != Vector2.zero) // Don't dash if not moving
        {
            isDashing = true;
            dashTime = Time.time + dashDuration;
            lastDashTime = Time.time;

            // Trigger the Dash squish animation (ensure fishSquisher is assigned)
            if (fishSquisher != null)
            {
                fishSquisher.TriggerSquish(FishSquisher.SquishActionType.Dash);
            }
            else
            {
                Debug.LogWarning("Dash triggered, but FishSquisher reference is missing!", this);
            }

            // Maybe add a small impulse force here too, or rely purely on the FixedUpdate velocity change
            rb.AddForce(moveInput.normalized * dashImpulse, ForceMode2D.Impulse); // Example alternative
        }
    }
}