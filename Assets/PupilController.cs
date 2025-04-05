using UnityEngine;

#if UNITY_EDITOR
using UnityEditor; // Put the UnityEditor using statement inside the block
#endif

public class PupilController : MonoBehaviour
{
    [Header("Target References")]
    [Tooltip("The Rigidbody2D of the object whose movement the eye should follow.")]
    public Rigidbody2D targetRigidbody;

    [Tooltip("The central transform of the eyeball. If null, assumes this GameObject's parent.")]
    public Transform eyeCenter;

    [Header("Movement Settings")]
    [Tooltip("The minimum speed the target needs to reach to be considered 'moving'.")]
    public float movementThreshold = 0.1f;

    [Tooltip("How quickly the pupil moves towards its target position.")]
    public float lookSpeed = 10f;

    [Header("Look Boundaries")]
    [Tooltip("The maximum distance the pupil can move from the eye center (X and Y radius).")]
    public Vector2 lookRadius = new Vector2(0.3f, 0.3f);

    [Header("Idle Behavior")]
    [Tooltip("Minimum time the pupil stays looking in one random direction when idle.")]
    public float minIdleLookTime = 0.5f;
    [Tooltip("Maximum time the pupil stays looking in one random direction when idle.")]
    public float maxIdleLookTime = 1.5f;

    // Internal State Variables
    private Vector2 currentLookTargetLocal; // Target position relative to eye center
    private float idleTimer;
    private float currentIdleDuration;
    private bool wasMovingLastFrame = false; // To detect transition to idle

    void Start()
    {
        // If eyeCenter is not explicitly assigned, assume the parent transform is the center
        if (eyeCenter == null)
        {
            if (transform.parent != null)
            {
                eyeCenter = transform.parent;
            }
            else
            {
                Debug.LogError("PupilController requires either an 'eyeCenter' Transform assigned or to be parented to the eye center GameObject.", this);
                enabled = false; // Disable script if no center is found
                return;
            }
        }

        // Ensure the pupil starts at the center or its initial target
        currentLookTargetLocal = Vector2.zero;
        transform.localPosition = Vector3.zero; // Start pupil at the local center
        SetupNewIdleTarget();
    }

    void Update()
    {
        if (targetRigidbody == null || eyeCenter == null)
        {
            // Don't do anything if references are missing
            return;
        }

        // --- Determine Target Look Position ---
        Vector2 targetVelocity = targetRigidbody.linearVelocity;
        bool isMoving = targetVelocity.magnitude > movementThreshold;

        if (isMoving)
        {
            // Target is moving: Look in the direction of movement
            Vector2 lookDirection = targetVelocity.normalized;
            // Scale direction by elliptical radius
            currentLookTargetLocal = new Vector2(lookDirection.x * lookRadius.x, lookDirection.y * lookRadius.y);
            wasMovingLastFrame = true;
        }
        else
        {
            // Target is idle
            if (wasMovingLastFrame)
            {
                // Just stopped moving, reset idle timer immediately to pick a new direction
                SetupNewIdleTarget();
                wasMovingLastFrame = false;
            }

            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
            {
                SetupNewIdleTarget();
            }
            // Keep currentLookTargetLocal as is until timer runs out
        }

        // --- Update Pupil Position ---
        // Smoothly move the pupil towards the target local position
        // Using localPosition assumes this pupil GameObject is a child of the eyeCenter
        transform.localPosition = Vector3.Lerp(transform.localPosition, (Vector3)currentLookTargetLocal, Time.deltaTime * lookSpeed);
    }

    void SetupNewIdleTarget()
    {
        // Pick a random direction within a unit circle
        Vector2 randomDirection = Random.insideUnitCircle;
        // Scale the random direction by the elliptical radius
        currentLookTargetLocal = new Vector2(randomDirection.x * lookRadius.x, randomDirection.y * lookRadius.y);

        // Set a new random duration for how long to look in this direction
        currentIdleDuration = Random.Range(minIdleLookTime, maxIdleLookTime);
        idleTimer = currentIdleDuration;
    }


    // --- Editor Visualization ---
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Use the assigned eyeCenter, or default to parent if available during editing
        Transform center = eyeCenter != null ? eyeCenter : transform.parent;

        if (center != null)
        {
            // Store current Gizmo matrix
            Matrix4x4 originalMatrix = Handles.matrix;
            // Set matrix to follow the eye center's transform
            Handles.matrix = center.localToWorldMatrix;

            // Draw the elliptical boundary relative to the eye center
            Handles.color = Color.cyan;
            // Handles.DrawWireEllipse needs the position/rotation handled by the matrix,
            // the normal (Vector3.forward for 2D XY), and the radii (Vector2).
            // We pass Vector3.zero as position because the matrix is already set to the center.
            Handles.DrawWireArc(Vector3.zero, Vector3.forward, Vector3.right, 360f, lookRadius.x, 1f);
            // Restore original Gizmo matrix
            Handles.matrix = originalMatrix;

            // Optional: Draw a line to the current target look position
            Gizmos.color = Color.yellow;
            Vector3 worldTarget = center.TransformPoint((Vector3)currentLookTargetLocal);
            Gizmos.DrawLine(center.position, worldTarget);
        }
    }

    // Helper for drawing ellipse if Handles are not preferred (less smooth)
    void DrawGizmosEllipse(Vector3 center, Vector2 radius, int segments = 30)
    {
        Gizmos.color = Color.cyan;
        Vector3 startPoint = center + new Vector3(radius.x, 0, 0);
        Vector3 lastPoint = startPoint;

        for (int i = 1; i <= segments; i++)
        {
            float angle = i / (float)segments * 360f * Mathf.Deg2Rad;
            Vector3 nextPointRelative = new Vector3(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y, 0);
            Vector3 nextPoint = center + nextPointRelative;
            Gizmos.DrawLine(lastPoint, nextPoint);
            lastPoint = nextPoint;
        }
    }

#endif // UNITY_EDITOR
}