using UnityEngine;

public class FishMovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Normal movement speed when actively moving to a target")]
    public float normalSpeed = 2.0f;
    [Tooltip("Slower speed when idly wandering")]
    public float idleSpeed = 0.75f;
    [Tooltip("Maximum speed during a dash")]
    public float dashSpeed = 6.0f;
    [Tooltip("How quickly the fish accelerates to target speed")]
    public float acceleration = 3.0f;
    [Tooltip("How quickly the fish slows down")]
    public float deceleration = 5.0f;
    [Tooltip("Distance threshold to consider target reached")]
    public float reachTargetThreshold = 0.2f;

    [Header("Dash Settings")]
    public float dashDuration = 0.35f;
    public float dashCooldown = 2.5f;

    [Header("Rotation Settings")]
    [Tooltip("How strongly the fish tries to rotate back upright (torque force)")]
    public float physicsRotationCorrectionTorque = 5.0f;
    [Tooltip("Minimum angle difference (degrees) to trigger correction")]
    public float physicsRotationCorrectionThreshold = 2.0f;
    [Tooltip("Maximum torque applied to prevent excessive spinning")]
    public float maxCorrectionTorque = 10.0f;

    // Movement state
    public enum MovementState
    {
        Idle,
        MovingToPosition,
        FollowingTarget,
        Dashing
    }

    // Component references
    [Header("Component References")]
    [Tooltip("Rigidbody2D component for physics interactions")]
    [SerializeField]
    private Rigidbody2D rb;

    // State tracking
    private MovementState currentState = MovementState.Idle;
    private Vector2 targetPosition;
    private Transform targetTransform;
    private float currentTargetSpeed;
    private Vector2 currentVelocityRef; // For smooth dampening

    // Dash state
    private bool isDashing = false;
    private float dashEndTime;
    private float nextDashAvailableTime;

    // Wandering state
    private Vector2 wanderDirection = Vector2.right;
    private float wanderDirectionChangeInterval = 2.0f;
    private float nextWanderDirectionChange;

    // Events
    public System.Action OnTargetReached;
    public System.Action OnDashStart;
    public System.Action OnDashEnd;

    void Awake()
    {
        rb.gravityScale = 0;
        currentTargetSpeed = idleSpeed;
        UpdateWanderDirection();
    }

    void FixedUpdate()
    {
        UpdateMovementState();
        ApplyMovement();
        ApplyRotationCorrection();
    }

    private void UpdateMovementState()
    {
        // Check if dash is ending
        if (isDashing && Time.time >= dashEndTime)
        {
            EndDash();
        }

        // Update wander direction periodically when idle
        if (currentState == MovementState.Idle && Time.time >= nextWanderDirectionChange)
        {
            UpdateWanderDirection();
        }

        // Check if we've reached the target position
        if (currentState == MovementState.MovingToPosition)
        {
            if (Vector2.Distance(rb.position, targetPosition) <= reachTargetThreshold)
            {
                // Target reached
                OnTargetReached?.Invoke();
                SetIdle();
            }
        }
    }

    private void UpdateWanderDirection()
    {
        float randomAngle = Random.Range(0f, 360f);
        wanderDirection = new Vector2(
            Mathf.Cos(randomAngle * Mathf.Deg2Rad),
            Mathf.Sin(randomAngle * Mathf.Deg2Rad)
        ).normalized;

        nextWanderDirectionChange = Time.time +
            Random.Range(wanderDirectionChangeInterval * 0.5f, wanderDirectionChangeInterval * 1.5f);
    }

    private void ApplyMovement()
    {
        Vector2 moveDirection;
        float targetSpeed;

        switch (currentState)
        {
            case MovementState.Dashing:
                moveDirection = rb.linearVelocity.normalized;
                if (moveDirection.sqrMagnitude < 0.1f) moveDirection = transform.right; // Fallback direction
                targetSpeed = dashSpeed;
                break;

            case MovementState.MovingToPosition:
                moveDirection = ((Vector2)targetPosition - rb.position).normalized;
                targetSpeed = currentTargetSpeed;
                break;

            case MovementState.FollowingTarget:
                if (targetTransform != null)
                {
                    moveDirection = ((Vector2)targetTransform.position - rb.position).normalized;
                    targetSpeed = currentTargetSpeed;
                }
                else
                {
                    // Target was destroyed, go to idle
                    currentState = MovementState.Idle;
                    moveDirection = wanderDirection;
                    targetSpeed = idleSpeed;
                }
                break;

            case MovementState.Idle:
            default:
                moveDirection = wanderDirection;
                targetSpeed = idleSpeed;
                break;
        }

        Vector2 targetVelocity = moveDirection * targetSpeed;

        // Determine if accelerating or decelerating
        float currentAccel = isDashing ? acceleration * 2 : acceleration;
        bool isDecelerating = targetVelocity.sqrMagnitude < rb.linearVelocity.sqrMagnitude;
        float smoothTime = isDecelerating ? (1.0f / deceleration) : (1.0f / currentAccel);

        // Apply smoothed movement
        rb.linearVelocity = Vector2.SmoothDamp(
            rb.linearVelocity,
            targetVelocity,
            ref currentVelocityRef,
            smoothTime,
            Mathf.Infinity,
            Time.fixedDeltaTime
        );
    }

    private void ApplyRotationCorrection()
    {
        float currentAngle = rb.rotation;
        float angleDifference = Mathf.DeltaAngle(currentAngle, 0f);

        if (Mathf.Abs(angleDifference) > physicsRotationCorrectionThreshold)
        {
            float targetTorque = -angleDifference * physicsRotationCorrectionTorque;
            targetTorque = Mathf.Clamp(targetTorque, -maxCorrectionTorque, maxCorrectionTorque);
            rb.AddTorque(targetTorque * Time.fixedDeltaTime, ForceMode2D.Impulse);
        }
    }

    // Public methods for controlling the fish movement

    /// <summary>
    /// Sets a target position for the fish to move toward
    /// </summary>
    public void SetTargetPosition(Vector2 position)
    {
        targetPosition = position;
        targetTransform = null;
        currentState = MovementState.MovingToPosition;
        currentTargetSpeed = normalSpeed;
    }

    /// <summary>
    /// Sets a transform for the fish to follow
    /// </summary>
    public void SetTargetTransform(Transform target)
    {
        targetTransform = target;
        currentState = MovementState.FollowingTarget;
        currentTargetSpeed = normalSpeed;
    }

    /// <summary>
    /// Sets the fish to idle mode with random wandering
    /// </summary>
    public void SetIdle()
    {
        targetTransform = null;
        targetPosition = Vector2.zero;
        currentState = MovementState.Idle;
        currentTargetSpeed = idleSpeed;
    }

    /// <summary>
    /// Sets the target speed for movement
    /// </summary>
    public void SetTargetSpeed(float speed)
    {
        currentTargetSpeed = speed;
    }

    /// <summary>
    /// Makes the fish dash in its current direction
    /// </summary>
    public bool TryDash()
    {
        if (!isDashing && Time.time >= nextDashAvailableTime)
        {
            isDashing = true;
            dashEndTime = Time.time + dashDuration;
            nextDashAvailableTime = Time.time + dashCooldown;
            currentState = MovementState.Dashing;
            OnDashStart?.Invoke();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Force-ends the current dash if one is active
    /// </summary>
    public void CancelDash()
    {
        if (isDashing)
        {
            EndDash();
        }
    }

    private void EndDash()
    {
        isDashing = false;
        OnDashEnd?.Invoke();

        // Return to previous state
        if (targetTransform != null)
            currentState = MovementState.FollowingTarget;
        else if (targetPosition != Vector2.zero)
            currentState = MovementState.MovingToPosition;
        else
            currentState = MovementState.Idle;
    }

    /// <summary>
    /// Gets the current velocity of the fish
    /// </summary>
    public Vector2 GetCurrentVelocity()
    {
        return rb != null ? rb.linearVelocity : Vector2.zero;
    }

    /// <summary>
    /// Gets the current movement state
    /// </summary>
    public MovementState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// Gets current wander direction (useful for visual orientation)
    /// </summary>
    public Vector2 GetWanderDirection()
    {
        return wanderDirection;
    }

    /// <summary>
    /// Checks if the fish has approximately reached its target position
    /// </summary>
    public bool HasReachedTargetPosition()
    {
        if (currentState != MovementState.MovingToPosition) return false;
        return Vector2.Distance(rb.position, targetPosition) <= reachTargetThreshold;
    }

    /// <summary>
    /// Checks if a dash is currently available
    /// </summary>
    public bool IsDashAvailable()
    {
        return !isDashing && Time.time >= nextDashAvailableTime;
    }

    /// <summary>
    /// Gets the cooldown remaining before dash is available again
    /// </summary>
    public float GetDashCooldownRemaining()
    {
        if (IsDashAvailable()) return 0f;
        return nextDashAvailableTime - Time.time;
    }
}
