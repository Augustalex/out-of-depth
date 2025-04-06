// FishAgent.cs:
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(FishController))]
public class FishAgent : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Maximum strength of random movement variations")]
    public float movementVariation = 0.3f; // Reduced
    [Tooltip("Fish tries to stay within this distance from its spawn point")]
    public float movementRadius = 10f;
    [Tooltip("Strength of the desire to stay within movement radius")]
    public float boundaryForce = 2f; // Reduced
    [Tooltip("How quickly the fish changes its random wander direction")]
    public float randomDirectionChangeSpeed = 0.5f; // Added for smoother random turns

    [Header("Perception Settings")]
    [Tooltip("How far the fish can see other objects")]
    public float eyeSight = 8f;
    [Tooltip("How often (in seconds) the fish checks for objects of interest")]
    public float perceptionCheckInterval = 0.5f; // Slightly more frequent checks might be needed with smoother movement
    [Tooltip("Layer mask for objects the fish can perceive")]
    public LayerMask perceptionLayers;

    [Header("Behavior Settings")]
    [Tooltip("Tags that scare the fish away")]
    public List<string> scaredOfTags = new List<string>();
    [Tooltip("Force multiplier when fleeing from scary objects")]
    public float scaredFleeForce = 2.0f; // Reduced

    [Tooltip("Tags the fish is attracted to")]
    public List<string> attractedToTags = new List<string>();
    [Tooltip("Force multiplier when moving toward attractive objects")]
    public float attractionForce = 1.0f; // Reduced

    [Tooltip("Tag the fish will follow if seen")]
    public string followTag;
    [Tooltip("Force multiplier when following target")]
    public float followForce = 1.5f; // Reduced
    [Tooltip("How close the fish tries to get to follow target")]
    public float followDistance = 3f;

    [Header("Collision Avoidance")]
    [Tooltip("Tags to avoid colliding with")]
    public List<string> avoidCollisionWithTags = new List<string>();
    [Tooltip("Distance at which collision avoidance activates")]
    public float collisionAvoidanceDistance = 1.5f; // Slightly reduced to react sooner if needed
    [Tooltip("Force applied to avoid collisions")]
    public float collisionAvoidanceForce = 3.0f; // Can be slightly higher priority

    [Header("Debug Visualization")]
    public bool showDebugGizmos = false;

    // Component references
    private FishController fishController;
    private Vector3 startPosition;

    // State tracking
    private Vector2 currentWanderDirection; // Renamed for clarity
    private float nextPerceptionCheckTime;
    private Transform currentFollowTarget;
    private List<Transform> currentThreats = new List<Transform>();
    private List<Transform> currentAttractions = new List<Transform>();
    private List<Transform> currentObstacles = new List<Transform>();

    // Movement state
    private Vector2 randomMovementInfluence;
    private float nextRandomInfluenceTime; // Renamed for clarity

    void Awake()
    {
        fishController = GetComponent<FishController>();
        startPosition = transform.position;

        // Initialize with a random movement direction
        currentWanderDirection = Random.insideUnitCircle.normalized;
        UpdateRandomMovementInfluence(); // Calculate initial influence

        // Tell the FishController this is AI-controlled
        fishController.SetAIControlled(true);
    }

    void Update()
    {
        // Check for perception updates on interval
        if (Time.time >= nextPerceptionCheckTime)
        {
            PerceiveEnvironment();
            nextPerceptionCheckTime = Time.time + perceptionCheckInterval;
        }

        // Update random movement influence periodically
        if (Time.time >= nextRandomInfluenceTime)
        {
            UpdateRandomMovementInfluence();
            nextRandomInfluenceTime = Time.time + Random.Range(1.5f, 4f); // Longer interval for influence changes
        }

        // Smoothly update the base wander direction towards the random influence
        currentWanderDirection = Vector2.Lerp(currentWanderDirection, (currentWanderDirection + randomMovementInfluence).normalized, Time.deltaTime * randomDirectionChangeSpeed).normalized;

        // Calculate all movement influences
        Vector2 desiredDirection = CalculateDesiredDirection();

        // Apply movement direction to the fish controller
        // The controller will handle smoothing the actual velocity
        fishController.SetMoveInput(desiredDirection);
    }

    // Renamed from CalculateMovement to better reflect its purpose
    Vector2 CalculateDesiredDirection()
    {
        Vector2 combinedForce = Vector2.zero;
        float totalWeight = 0f; // Use weighting instead of strict priorities for smoother blending

        // --- Calculate forces/influences ---

        // 1. Flee from threats (Highest Priority)
        Vector2 fleeForce = Vector2.zero;
        if (currentThreats.Count > 0)
        {
            foreach (var threat in currentThreats)
            {
                if (threat == null) continue;
                Vector2 dirFromThreat = ((Vector2)transform.position - (Vector2)threat.position);
                float distance = dirFromThreat.magnitude;
                if (distance < eyeSight && distance > 0.01f) // Avoid division by zero
                {
                    // Stronger avoidance when closer
                    float weight = Mathf.Pow(1f - Mathf.Clamp01(distance / eyeSight), 2);
                    fleeForce += dirFromThreat.normalized * weight;
                }
            }
            if (fleeForce.magnitude > 0.01f)
            {
                combinedForce += fleeForce.normalized * scaredFleeForce;
                totalWeight += scaredFleeForce; // Add weight
            }
        }

        // 2. Follow target (High Priority, but less than fleeing)
        Vector2 followForceVec = Vector2.zero;
        if (totalWeight < scaredFleeForce * 0.5f && currentFollowTarget != null) // Only follow if not strongly fleeing
        {
            Vector2 dirToTarget = (Vector2)(currentFollowTarget.position - transform.position);
            float distance = dirToTarget.magnitude;

            if (distance > 0.01f)
            {
                // Move towards target if far, slow down when close
                float desiredSpeed = (distance > followDistance) ? 1.0f : Mathf.Clamp01(distance / followDistance);
                followForceVec = dirToTarget.normalized * desiredSpeed;
                combinedForce += followForceVec.normalized * followForce;
                totalWeight += followForce; // Add weight
            }
        }

        // 3. Move toward attractions (Medium Priority)
        Vector2 attractForce = Vector2.zero;
        if (totalWeight < (scaredFleeForce + followForce) * 0.5f && currentAttractions.Count > 0) // Only attract if not strongly fleeing/following
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
                totalWeight += attractionForce; // Add weight
            }
        }

        // 4. Default behavior: Wander within radius (Low Priority)
        Vector2 wanderForce = currentWanderDirection; // Base wander direction
        float distanceFromStart = Vector2.Distance(transform.position, startPosition);
        if (distanceFromStart > movementRadius) // Stronger boundary push when outside
        {
            Vector2 dirToStart = ((Vector2)startPosition - (Vector2)transform.position).normalized;
            // Blend wander with return-to-center force
            float boundaryInfluence = Mathf.Clamp01((distanceFromStart - movementRadius) / (movementRadius * 0.5f)); // Start returning strongly past radius
            wanderForce = Vector2.Lerp(wanderForce, dirToStart, boundaryInfluence).normalized;
            combinedForce += wanderForce * boundaryForce; // Boundary force has its own weight here
            totalWeight += boundaryForce;
        }
        else if (distanceFromStart > movementRadius * 0.7f) // Gentle nudge back before hitting boundary
        {
            Vector2 dirToStart = ((Vector2)startPosition - (Vector2)transform.position).normalized;
            float boundaryInfluence = Mathf.Clamp01((distanceFromStart - movementRadius * 0.7f) / (movementRadius * 0.3f));
            wanderForce = Vector2.Lerp(wanderForce, dirToStart, boundaryInfluence * 0.5f).normalized; // Less aggressive nudge
            combinedForce += wanderForce * 1.0f; // Add basic wander weight
            totalWeight += 1.0f;
        }
        else
        {
            // Just apply base wander weight if no other strong forces are active
            if (totalWeight < 0.1f)
            {
                combinedForce += wanderForce * 1.0f;
                totalWeight += 1.0f;
            }
        }


        // 5. Always apply collision avoidance (Very High Priority, additive)
        Vector2 avoidanceForce = CalculateCollisionAvoidance();
        if (avoidanceForce.magnitude > 0.01f)
        {
            // Add avoidance scaled by its force multiplier. We don't normalize the *entire* sum
            // afterward if avoidance is active, letting it strongly steer away.
            combinedForce += avoidanceForce * collisionAvoidanceForce;
            // We don't add to totalWeight here, treating avoidance as a separate corrective force
        }


        // --- Combine and Finalize ---
        if (combinedForce.magnitude > 0.01f)
        {
            // If avoidance is active, let it dominate more
            if (avoidanceForce.magnitude > 0.01f)
            {
                // Maybe clamp the magnitude instead of pure normalization?
                return Vector2.ClampMagnitude(combinedForce, 1.0f); // Clamp magnitude to prevent extreme speeds from combined forces
            }
            else
            {
                // Otherwise normalize the blended direction vector
                return combinedForce.normalized;
            }
        }
        else if (wanderForce.magnitude > 0.01f)
        {
            // If all else is zero, just wander
            return wanderForce.normalized;
        }

        // Default fallback (should rarely happen)
        return Vector2.right; // Or currentWanderDirection
    }


    void PerceiveEnvironment()
    {
        // Clear previous perception results
        currentThreats.Clear();
        currentAttractions.Clear();
        currentFollowTarget = null;
        currentObstacles.Clear(); // Clear obstacles too

        // Detect all colliders within eyesight
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, eyeSight, perceptionLayers);
        Transform closestFollowTarget = null;
        float closestFollowDist = float.MaxValue;

        foreach (var collider in nearbyColliders)
        {
            // Skip self
            if (collider.transform == transform) continue;

            GameObject targetObj = collider.gameObject;
            bool isThreat = false;
            bool isAttraction = false;
            bool isFollowTarget = false;
            bool isObstacle = false; // Track obstacle separately

            // Check Threat Tags
            foreach (var scaryTag in scaredOfTags)
            {
                if (!string.IsNullOrEmpty(scaryTag) && targetObj.CompareTag(scaryTag))
                {
                    currentThreats.Add(collider.transform);
                    isThreat = true;
                    break; // Processed as threat
                }
            }

            // Check Attraction Tags (only if not a threat)
            if (!isThreat)
            {
                foreach (var attractiveTag in attractedToTags)
                {
                    if (!string.IsNullOrEmpty(attractiveTag) && targetObj.CompareTag(attractiveTag))
                    {
                        currentAttractions.Add(collider.transform);
                        isAttraction = true;
                        break; // Processed as attraction
                    }
                }
            }

            // Check Follow Tag (only if not threat or attraction)
            if (!isThreat && !isAttraction && !string.IsNullOrEmpty(followTag))
            {
                if (targetObj.CompareTag(followTag))
                {
                    float dist = Vector2.Distance(transform.position, collider.transform.position);
                    if (dist < closestFollowDist) // Find the closest potential follow target
                    {
                        closestFollowDist = dist;
                        closestFollowTarget = collider.transform;
                        isFollowTarget = true; // Mark as potential follow target
                    }
                }
            }

            // Check Collision Avoidance Tags (can be anything, including threats/attractions)
            foreach (var avoidTag in avoidCollisionWithTags)
            {
                if (!string.IsNullOrEmpty(avoidTag) && targetObj.CompareTag(avoidTag))
                {
                    float dist = Vector2.Distance(transform.position, collider.transform.position);
                    // Only add if within avoidance distance for efficiency
                    if (dist < collisionAvoidanceDistance * 1.2f) // Check slightly beyond radius
                    {
                        currentObstacles.Add(collider.transform);
                        isObstacle = true;
                        break; // Processed as obstacle for avoidance
                    }
                }
            }
        }

        // Set the single closest follow target after checking all colliders
        currentFollowTarget = closestFollowTarget;
    }

    // Calculates only the avoidance vector, scaling happens in CalculateDesiredDirection
    Vector2 CalculateCollisionAvoidance()
    {
        Vector2 avoidanceDirection = Vector2.zero;

        foreach (var obstacle in currentObstacles)
        {
            if (obstacle == null) continue;

            Vector2 dirToObstacle = (obstacle.position - transform.position);
            float distance = dirToObstacle.magnitude;

            // Only avoid if within collision avoidance distance
            if (distance < collisionAvoidanceDistance && distance > 0.01f)
            {
                // Calculate direction pointing away from the obstacle center
                Vector2 dirFromObstacle = -dirToObstacle.normalized;

                // Stronger avoidance force the closer the obstacle is
                float avoidanceStrength = 1f - Mathf.Clamp01(distance / collisionAvoidanceDistance);
                avoidanceDirection += dirFromObstacle * avoidanceStrength * avoidanceStrength; // Square strength for more urgent avoidance when very close
            }
        }

        // Return the combined avoidance vector (normalization and scaling happen later)
        return avoidanceDirection;
    }

    void UpdateRandomMovementInfluence()
    {
        // Creates a random vector offset
        randomMovementInfluence = Random.insideUnitCircle * movementVariation;
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos || fishController == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(startPosition, movementRadius);

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, eyeSight);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, collisionAvoidanceDistance);

        // Draw desired direction from Agent
        Gizmos.color = Color.cyan;
        Vector2 desiredDir = CalculateDesiredDirection();
        Gizmos.DrawRay(transform.position, desiredDir * 2f);

        // Draw current velocity from Controller (more accurate representation of actual movement)
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, fishController.GetCurrentVelocity().normalized * 1.5f);

        // Draw avoidance vector if active
        Vector2 avoidance = CalculateCollisionAvoidance();
        if (avoidance.magnitude > 0.01f)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, avoidance.normalized * collisionAvoidanceForce);
        }
    }
}