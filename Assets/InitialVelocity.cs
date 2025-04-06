using UnityEngine;

/// <summary>
/// Applies a configurable initial velocity to a Rigidbody2D component
/// when the script starts. Direction and speed are configurable.
/// Optional torque can also be applied.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))] // Ensures this GameObject has a Rigidbody2D
public class InitialVelocity : MonoBehaviour
{
    [Header("Velocity Configuration")]
    
    [SerializeField]
    [Tooltip("The direction of the initial velocity.")]
    private Vector2 direction = Vector2.right;
    
    [SerializeField]
    [Min(0f)] // Ensure speed isn't negative in the inspector
    [Tooltip("The magnitude (speed) of the initial velocity.")]
    private float speed = 5.0f;
    
    [Header("Rotation Configuration")]
    
    [SerializeField]
    [Tooltip("Whether to apply torque (rotational force) to the object.")]
    private bool applyTorque = false;
    
    [SerializeField]
    [Tooltip("The torque to apply (positive for clockwise, negative for counterclockwise rotation).")]
    private float torque = 0.0f;
    
    // Reference to the Rigidbody2D component
    private Rigidbody2D rb;
    
    // --- Unity Lifecycle Methods ---
    
    void Awake()
    {
        // Get the Rigidbody2D component attached to this GameObject
        rb = GetComponent<Rigidbody2D>();
        
        if (rb == null)
        {
            Debug.LogError($"[{nameof(InitialVelocity)}] Rigidbody2D component not found on {gameObject.name}. Disabling script.", this);
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
    /// Applies the configured initial velocity and optional torque.
    /// </summary>
    private void ApplyInitialVelocity()
    {
        // Normalize the direction vector if it's not already normalized
        Vector2 normalizedDirection = direction.magnitude > 0 ? direction.normalized : Vector2.right;
        
        // Calculate the final velocity vector (Direction * Speed)
        Vector2 initialVelocity = normalizedDirection * speed;
        
        // Apply the velocity to the Rigidbody2D
        rb.linearVelocity = initialVelocity;
        
        // Apply torque if enabled
        if (applyTorque)
        {
            rb.angularVelocity = torque;
        }
        
        // Optional: Log the applied velocity for debugging
        // Debug.Log($"[{nameof(InitialVelocity)}] Applied initial velocity {initialVelocity} (Speed: {speed}) to {gameObject.name}", this);
    }
    
    // Optional: Visualize the direction in the editor
    void OnDrawGizmosSelected()
    {
        if (direction != Vector2.zero)
        {
            Gizmos.color = Color.blue;
            Vector3 position = transform.position;
            Vector3 directionVector = new Vector3(direction.x, direction.y, 0).normalized;
            Gizmos.DrawLine(position, position + directionVector);
            
            // Draw arrow head
            Vector3 right = Quaternion.Euler(0, 0, 30) * -directionVector * 0.25f;
            Vector3 left = Quaternion.Euler(0, 0, -30) * -directionVector * 0.25f;
            Gizmos.DrawLine(position + directionVector, position + directionVector + right);
            Gizmos.DrawLine(position + directionVector, position + directionVector + left);
        }
    }
}
