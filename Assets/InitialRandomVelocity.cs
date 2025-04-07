using UnityEngine;

/// <summary>
/// Applies an initial random velocity to a Rigidbody2D component
/// when the script starts. Speed is configurable between a min and max range.
/// Optional random torque can also be applied.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))] // Ensures this GameObject has a Rigidbody2D
public class InitialRandomVelocity : MonoBehaviour
{
    [Header("Velocity Configuration")]

    [SerializeField]
    [Min(0f)] // Ensure speed isn't negative in the inspector
    [Tooltip("The minimum magnitude (speed) of the initial velocity.")]
    private float minSpeed = 2.0f;

    [SerializeField]
    [Min(0f)] // Ensure speed isn't negative in the inspector
    [Tooltip("The maximum magnitude (speed) of the initial velocity.")]
    private float maxSpeed = 5.0f;

    [Header("Rotation Configuration")]

    [SerializeField]
    [Tooltip("Whether to apply random torque (rotational force) to the object.")]
    private bool applyRandomTorque = false;

    [SerializeField]
    [Tooltip("The minimum torque to apply (can be negative for counterclockwise rotation).")]
    private float minTorque = -10.0f;

    [SerializeField]
    [Tooltip("The maximum torque to apply (can be positive for clockwise rotation).")]
    private float maxTorque = 10.0f;

    // Reference to the Rigidbody2D component
    private Rigidbody2D rb;

    // --- Unity Lifecycle Methods ---

    void Awake()
    {
        // Get the Rigidbody2D component attached to this GameObject
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError($"[{nameof(InitialRandomVelocity)}] Rigidbody2D component not found on {gameObject.name}. Disabling script.", this);
            enabled = false; // Disable this script component if RB is missing (though RequireComponent should prevent this)
        }
    }

    void Start()
    {
        // Apply the initial velocity logic when the object starts
        ApplyInitialVelocity();
    }

    // --- Core Logic ---

    /// <summary>
    /// Calculates and applies the initial random velocity.
    /// </summary>
    private void ApplyInitialVelocity()
    {
        // 1. Generate a random 2D direction.
        // Random.insideUnitCircle generates a random point within or on a circle of radius 1.
        // .normalized ensures the vector has a magnitude of 1 (it's just a direction).
        Vector2 randomDirection = Random.insideUnitCircle.normalized;

        // Handle the (very rare) case where Random.insideUnitCircle returns exactly Vector2.zero
        if (randomDirection == Vector2.zero)
        {
            randomDirection = Vector2.right; // Default to right direction
            Debug.LogWarning($"[{nameof(InitialRandomVelocity)}] Random direction was zero, defaulting to Vector2.right for {gameObject.name}.", this);
        }

        // 2. Generate a random speed between the min and max values.
        float randomSpeed = Random.Range(minSpeed, maxSpeed);

        // 3. Calculate the final velocity vector (Direction * Speed).
        Vector2 initialVelocity = randomDirection * randomSpeed;

        // 4. Apply the velocity to the Rigidbody2D.
        // This directly sets the object's velocity.
        rb.linearVelocity = initialVelocity;

        // 5. Apply random torque if enabled
        if (applyRandomTorque)
        {
            float randomTorque = Random.Range(minTorque, maxTorque);
            rb.angularVelocity = randomTorque;
        }
    }
}