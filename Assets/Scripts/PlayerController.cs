using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isDashing = false;
    private float dashTime;
    private float lastDashTime;

    private PlayerInputActions inputActions;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += _ => moveInput = Vector2.zero;
        inputActions.Player.Dash.performed += _ => TryDash();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            rb.linearVelocity = moveInput * dashSpeed;
            if (Time.time >= dashTime)
                isDashing = false;
        }
        else
        {
            rb.linearVelocity = moveInput * moveSpeed;
        }
    }

    private void TryDash()
    {
        if (Time.time >= lastDashTime + dashCooldown)
        {
            isDashing = true;
            dashTime = Time.time + dashDuration;
            lastDashTime = Time.time;
        }
    }
}
