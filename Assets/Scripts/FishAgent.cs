// FishAgent.cs:
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(FishController))]
public class FishAgent : MonoBehaviour
{
    // --- Existing Headers and Parameters (Adjusted Defaults from previous step) ---
    [Header("Movement Settings")]
    public float movementVariation = 0.2f;
    public float movementRadius = 10f;
    public float boundaryForce = 1.5f;
    public float randomDirectionChangeSpeed = 0.3f;

    [Header("Perception Settings")]
    public float eyeSight = 7f;
    public float perceptionCheckInterval = 0.6f;
    public LayerMask perceptionLayers;

    [Header("Behavior Settings")]
    public List<string> scaredOfTags = new List<string>();
    public float scaredFleeForce = 1.8f;
    public List<string> attractedToTags = new List<string>();
    public float attractionForce = 0.8f;
    public string followTag;
    public float followForce = 1.2f;
    public float followDistance = 3.5f;

    [Header("Collision Avoidance")]
    public List<string> avoidCollisionWithTags = new List<string>();
    public float collisionAvoidanceDistance = 1.5f;
    public float collisionAvoidanceForce = 2.5f;

    [Header("Debug Visualization")]
    public bool showDebugGizmos = false;

    // --- Component references ---
    private FishController fishController;
    private Vector3 startPosition;

    // --- State tracking ---
    private Vector2 currentWanderDirection; // This needs to be accessible
    private float nextPerceptionCheckTime;
    private Transform currentFollowTarget;
    private List<Transform> currentThreats = new List<Transform>();
    private List<Transform> currentAttractions = new List<Transform>();
    private List<Transform> currentObstacles = new List<Transform>();
    private Vector2 randomMovementInfluence;
    private float nextRandomInfluenceTime;

    // --- Public Getter for Wander Direction ---
    /// <summary>
    /// Gets the current underlying wander direction of the fish.
    /// </summary>
    public Vector2 CurrentWanderDirection => currentWanderDirection;


    void Awake()
    {
        fishController = GetComponent<FishController>();
        startPosition = transform.position;
        currentWanderDirection = Random.insideUnitCircle.normalized;
        UpdateRandomMovementInfluence();
        fishController.SetAIControlled(true);
    }

    void Update()
    {
        if (Time.time >= nextPerceptionCheckTime)
        {
            PerceiveEnvironment();
            nextPerceptionCheckTime = Time.time + perceptionCheckInterval;
        }

        if (Time.time >= nextRandomInfluenceTime)
        {
            UpdateRandomMovementInfluence();
            nextRandomInfluenceTime = Time.time + Random.Range(2.0f, 5.0f);
        }

        // Smoothly update the base wander direction
        currentWanderDirection = Vector2.Lerp(currentWanderDirection, (currentWanderDirection + randomMovementInfluence).normalized, Time.deltaTime * randomDirectionChangeSpeed).normalized;

        // Calculate the desired high-level direction (flee, follow, attract, avoid, boundary nudge)
        // If none of these are strong, the fish controller will use the CurrentWanderDirection for idle movement.
        Vector2 primaryDesiredDirection = CalculatePrimaryDesiredDirection();
        fishController.SetMoveInput(primaryDesiredDirection); // Send the primary goal direction
    }

    // Renamed to better reflect its purpose - calculates the *main* goal, excluding basic wander
    Vector2 CalculatePrimaryDesiredDirection()
    {
        Vector2 combinedForce = Vector2.zero;
        bool hasPrimaryGoal = false; // Flag if flee/follow/attract/boundary provides a direction

        // --- Calculate Primary Forces ---

        // 1. Flee from threats (Highest Priority)
        Vector2 fleeForce = Vector2.zero;
        if (currentThreats.Count > 0)
        {
            foreach (var threat in currentThreats)
            {
                if (threat == null) continue;
                Vector2 dirFromThreat = ((Vector2)transform.position - (Vector2)threat.position);
                float distance = dirFromThreat.magnitude;
                if (distance < eyeSight && distance > 0.01f)
                {
                    float weight = Mathf.Pow(1f - Mathf.Clamp01(distance / eyeSight), 2);
                    fleeForce += dirFromThreat.normalized * weight;
                }
            }
            if (fleeForce.magnitude > 0.01f)
            {
                combinedForce += fleeForce.normalized * scaredFleeForce;
                hasPrimaryGoal = true;
            }
        }

        // 2. Follow target (High Priority, but less than fleeing)
        Vector2 followForceVec = Vector2.zero;
        if (!hasPrimaryGoal && currentFollowTarget != null) // Only follow if not fleeing
        {
            Vector2 dirToTarget = (Vector2)(currentFollowTarget.position - transform.position);
            float distance = dirToTarget.magnitude;
            if (distance > 0.01f)
            {
                float desiredSpeedFactor = (distance > followDistance) ? 1.0f : Mathf.Clamp01(distance / followDistance); // Slow down when close
                followForceVec = dirToTarget.normalized * desiredSpeedFactor;
                combinedForce += followForceVec.normalized * followForce; // Use normalized direction scaled by force
                hasPrimaryGoal = true;
            }
        }

        // 3. Move toward attractions (Medium Priority)
        Vector2 attractForce = Vector2.zero;
        if (!hasPrimaryGoal && currentAttractions.Count > 0) // Only attract if not fleeing/following
        {
            foreach (var attraction in currentAttractions)
            {
                if (attraction == null) continue;
                Vector2 dirToAttraction = ((Vector2)attraction.position - (Vector2)transform.position);
                float distance = dirToAttraction.magnitude;
                if (distance < eyeSight && distance > 0.01f)
                {
                    float weight = 1f - Mathf.Clamp01(distance / eyeSight);
                    attractForce += dirToAttraction.normalized * weight;
                }
            }
            if (attractForce.magnitude > 0.01f)
            {
                combinedForce += attractForce.normalized * attractionForce;
                hasPrimaryGoal = true;
            }
        }

        // 4. Boundary Nudge (Considered a primary goal if active)
        float distanceFromStart = Vector2.Distance(transform.position, startPosition);
        if (distanceFromStart > movementRadius * 0.7f) // Start nudging back somewhat early
        {
            Vector2 dirToStart = ((Vector2)startPosition - (Vector2)transform.position).normalized;
            float boundaryInfluence = 0f;
            if (distanceFromStart > movementRadius) // Stronger force when outside
            {
                boundaryInfluence = Mathf.Clamp01((distanceFromStart - movementRadius) / (movementRadius * 0.5f));
                combinedForce += dirToStart * boundaryForce * boundaryInfluence; // Scaled by how far outside
                hasPrimaryGoal = true; // Definitely a primary goal if outside radius
            }
            else // Gentle nudge when approaching edge
            {
                boundaryInfluence = Mathf.Clamp01((distanceFromStart - movementRadius * 0.7f) / (movementRadius * 0.3f));
                // Add a weaker boundary force blended slightly with wander, only if no other goal exists yet
                if (!hasPrimaryGoal)
                {
                    Vector2 nudgeForce = Vector2.Lerp(currentWanderDirection, dirToStart, boundaryInfluence * 0.6f).normalized; // Gentle blend
                    combinedForce += nudgeForce * (boundaryForce * 0.5f); // Use half boundary force for the nudge inside
                    hasPrimaryGoal = true; // Nudging is now the primary goal
                }
            }
        }


        // 5. Always calculate collision avoidance
        Vector2 avoidanceForce = CalculateCollisionAvoidance();
        if (avoidanceForce.magnitude > 0.01f)
        {
            // Add avoidance force. If other forces exist, it blends. If avoidance is strong, it might dominate.
            combinedForce += avoidanceForce * collisionAvoidanceForce;
            // We don't set hasPrimaryGoal = true here, avoidance modifies the existing goal or wander.
        }


        // --- Finalize ---
        if (combinedForce.magnitude > 0.01f)
        {
            // If there's any calculated force (flee, follow, attract, boundary, avoidance) return its direction
            return combinedForce.normalized; // Normalize the final calculated direction
        }

        // If no primary goal was determined (and avoidance wasn't strong enough to create a vector),
        // return Vector2.zero. The FishController will then use CurrentWanderDirection for idle movement.
        return Vector2.zero;
    }

    // --- PerceiveEnvironment, CalculateCollisionAvoidance, UpdateRandomMovementInfluence, OnDrawGizmos ---
    // No changes needed in these from the previous step for these specific requests.
    // Calculation logic remains, but the output interpretation changes slightly based on hasPrimaryGoal.

    void PerceiveEnvironment()
    {
        currentThreats.Clear();
        currentAttractions.Clear();
        currentFollowTarget = null;
        currentObstacles.Clear();
        // Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, eyeSight, perceptionLayers);
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, eyeSight);
        Transform closestFollowTarget = null;
        float closestFollowDist = float.MaxValue;

        foreach (var collider in nearbyColliders)
        {
            if (collider.transform == transform) continue;
            GameObject targetObj = collider.gameObject;
            bool isThreat = false; bool isAttraction = false;

            foreach (var scaryTag in scaredOfTags) { if (!string.IsNullOrEmpty(scaryTag) && targetObj.CompareTag(scaryTag)) { currentThreats.Add(collider.transform); isThreat = true; break; } }
            if (!isThreat) { foreach (var attractiveTag in attractedToTags) { if (!string.IsNullOrEmpty(attractiveTag) && targetObj.CompareTag(attractiveTag)) { currentAttractions.Add(collider.transform); isAttraction = true; break; } } }
            if (!isThreat && !isAttraction && !string.IsNullOrEmpty(followTag)) { if (targetObj.CompareTag(followTag)) { float dist = Vector2.Distance(transform.position, collider.transform.position); if (dist < closestFollowDist) { closestFollowDist = dist; closestFollowTarget = collider.transform; } } }
            foreach (var avoidTag in avoidCollisionWithTags) { if (!string.IsNullOrEmpty(avoidTag) && targetObj.CompareTag(avoidTag)) { float dist = Vector2.Distance(transform.position, collider.transform.position); if (dist < collisionAvoidanceDistance * 1.2f) { currentObstacles.Add(collider.transform); break; } } }
        }
        currentFollowTarget = closestFollowTarget;
    }

    Vector2 CalculateCollisionAvoidance()
    {
        Vector2 avoidanceDirection = Vector2.zero;
        foreach (var obstacle in currentObstacles)
        {
            if (obstacle == null) continue;
            Vector2 dirToObstacle = (obstacle.position - transform.position);
            float distance = dirToObstacle.magnitude;
            if (distance < collisionAvoidanceDistance && distance > 0.01f)
            {
                Vector2 dirFromObstacle = -dirToObstacle.normalized;
                float avoidanceStrength = 1f - Mathf.Clamp01(distance / collisionAvoidanceDistance);
                avoidanceDirection += dirFromObstacle * avoidanceStrength * avoidanceStrength;
            }
        }
        return avoidanceDirection;
    }

    void UpdateRandomMovementInfluence() { randomMovementInfluence = Random.insideUnitCircle * movementVariation; }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Draw movement radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(startPosition, movementRadius);

        // Draw eyesight range
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, eyeSight);

        // Draw collision avoidance range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, collisionAvoidanceDistance);

        if (!Application.isPlaying) return;

        // Draw wander direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, CurrentWanderDirection * 1.2f);

        // Draw primary goal direction
        Vector2 primaryDir = CalculatePrimaryDesiredDirection();
        if (primaryDir.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, primaryDir * 1.5f);
        }

        // Draw current velocity
        if (fishController != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, fishController.GetCurrentVelocity().normalized * 1.5f);
        }

        // Draw avoidance influence
        Vector2 avoidance = CalculateCollisionAvoidance();
        if (avoidance.magnitude > 0.01f)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, avoidance.normalized * collisionAvoidanceForce * 0.5f);
        }

        // Visualize current mode
        if (currentThreats.Count > 0)
        {
            Gizmos.color = Color.red; // Fleeing mode
            Gizmos.DrawSphere(transform.position + Vector3.up * 0.5f, 0.2f);
        }
        else if (currentFollowTarget != null)
        {
            Gizmos.color = Color.green; // Following mode
            Gizmos.DrawSphere(transform.position + Vector3.up * 0.5f, 0.2f);
        }
        else if (currentAttractions.Count > 0)
        {
            Gizmos.color = Color.yellow; // Attracted mode
            Gizmos.DrawSphere(transform.position + Vector3.up * 0.5f, 0.2f);
        }
        else
        {
            Gizmos.color = Color.gray; // Idle mode
            Gizmos.DrawSphere(transform.position + Vector3.up * 0.5f, 0.2f);
        }
    }
}