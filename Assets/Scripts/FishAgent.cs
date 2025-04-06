// FishAgent.cs:
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(FishController))]
public class FishAgent : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Maximum strength of random movement variations")]
    public float movementVariation = 0.2f; // Further Reduced
    [Tooltip("Fish tries to stay within this distance from its spawn point")]
    public float movementRadius = 10f;
    [Tooltip("Strength of the desire to stay within movement radius")]
    public float boundaryForce = 1.5f; // Further Reduced
    [Tooltip("How quickly the fish changes its random wander direction (lower is slower/calmer)")]
    public float randomDirectionChangeSpeed = 0.3f; // Reduced for calmer turns

    [Header("Perception Settings")]
    [Tooltip("How far the fish can see other objects")]
    public float eyeSight = 7f; // Slightly reduced range
    [Tooltip("How often (in seconds) the fish checks for objects of interest")]
    public float perceptionCheckInterval = 0.6f; // Slightly less frequent checks due to slower speed
    [Tooltip("Layer mask for objects the fish can perceive")]
    public LayerMask perceptionLayers;

    [Header("Behavior Settings")]
    [Tooltip("Tags that scare the fish away")]
    public List<string> scaredOfTags = new List<string>();
    [Tooltip("Force multiplier when fleeing from scary objects")]
    public float scaredFleeForce = 1.8f; // Further Reduced

    [Tooltip("Tags the fish is attracted to")]
    public List<string> attractedToTags = new List<string>();
    [Tooltip("Force multiplier when moving toward attractive objects")]
    public float attractionForce = 0.8f; // Further Reduced

    [Tooltip("Tag the fish will follow if seen")]
    public string followTag;
    [Tooltip("Force multiplier when following target")]
    public float followForce = 1.2f; // Further Reduced
    [Tooltip("How close the fish tries to get to follow target")]
    public float followDistance = 3.5f; // Slightly increased distance

    [Header("Collision Avoidance")]
    [Tooltip("Tags to avoid colliding with")]
    public List<string> avoidCollisionWithTags = new List<string>();
    [Tooltip("Distance at which collision avoidance activates")]
    public float collisionAvoidanceDistance = 1.5f;
    [Tooltip("Force applied to avoid collisions")]
    public float collisionAvoidanceForce = 2.5f; // Keep avoidance relatively strong but lower than before

    [Header("Debug Visualization")]
    public bool showDebugGizmos = false;

    // --- Private fields remain the same ---
    private FishController fishController;
    private Vector3 startPosition;
    private Vector2 currentWanderDirection;
    private float nextPerceptionCheckTime;
    private Transform currentFollowTarget;
    private List<Transform> currentThreats = new List<Transform>();
    private List<Transform> currentAttractions = new List<Transform>();
    private List<Transform> currentObstacles = new List<Transform>();
    private Vector2 randomMovementInfluence;
    private float nextRandomInfluenceTime;


    // --- Awake, Update, PerceiveEnvironment, CalculateCollisionAvoidance, UpdateRandomMovementInfluence, OnDrawGizmos ---
    // No logical changes needed in these methods from the previous revision for this request.
    // The core logic for calculating desired direction remains, but the forces influencing it are weaker.

    void Awake()
    {
        fishController = GetComponent<FishController>();
        startPosition = transform.position;
        currentWanderDirection = Random.insideUnitCircle.normalized;
        UpdateRandomMovementInfluence();
        fishController.SetAIControlled(true);
    }

    void Start() { } // Keep Start empty or add initialization if needed

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
            nextRandomInfluenceTime = Time.time + Random.Range(2.0f, 5.0f); // Longer interval for influence changes
        }

        currentWanderDirection = Vector2.Lerp(currentWanderDirection, (currentWanderDirection + randomMovementInfluence).normalized, Time.deltaTime * randomDirectionChangeSpeed).normalized;

        Vector2 desiredDirection = CalculateDesiredDirection();
        fishController.SetMoveInput(desiredDirection);
    }


    Vector2 CalculateDesiredDirection()
    {
        Vector2 combinedForce = Vector2.zero;
        float totalWeight = 0f;

        // --- Calculate forces/influences ---

        // 1. Flee from threats
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
                totalWeight += scaredFleeForce;
            }
        }

        // 2. Follow target
        Vector2 followForceVec = Vector2.zero;
        if (totalWeight < scaredFleeForce * 0.5f && currentFollowTarget != null)
        {
            Vector2 dirToTarget = (Vector2)(currentFollowTarget.position - transform.position);
            float distance = dirToTarget.magnitude;
            if (distance > 0.01f)
            {
                float desiredSpeed = (distance > followDistance) ? 1.0f : Mathf.Clamp01(distance / followDistance);
                followForceVec = dirToTarget.normalized * desiredSpeed;
                combinedForce += followForceVec.normalized * followForce;
                totalWeight += followForce;
            }
        }

        // 3. Move toward attractions
        Vector2 attractForce = Vector2.zero;
        if (totalWeight < (scaredFleeForce + followForce) * 0.5f && currentAttractions.Count > 0)
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
                totalWeight += attractionForce;
            }
        }

        // 4. Default behavior: Wander within radius
        Vector2 wanderForce = currentWanderDirection;
        float distanceFromStart = Vector2.Distance(transform.position, startPosition);
        if (distanceFromStart > movementRadius)
        {
            Vector2 dirToStart = ((Vector2)startPosition - (Vector2)transform.position).normalized;
            float boundaryInfluence = Mathf.Clamp01((distanceFromStart - movementRadius) / (movementRadius * 0.5f));
            wanderForce = Vector2.Lerp(wanderForce, dirToStart, boundaryInfluence).normalized;
            combinedForce += wanderForce * boundaryForce;
            totalWeight += boundaryForce;
        }
        else if (distanceFromStart > movementRadius * 0.7f)
        {
            Vector2 dirToStart = ((Vector2)startPosition - (Vector2)transform.position).normalized;
            float boundaryInfluence = Mathf.Clamp01((distanceFromStart - movementRadius * 0.7f) / (movementRadius * 0.3f));
            wanderForce = Vector2.Lerp(wanderForce, dirToStart, boundaryInfluence * 0.5f).normalized;
            combinedForce += wanderForce * 1.0f; // Use a base weight of 1 for wander/boundary
            totalWeight += 1.0f;
        }
        else
        {
            if (totalWeight < 0.1f) // Only wander strongly if nothing else is happening
            {
                combinedForce += wanderForce * 1.0f; // Base wander weight
                totalWeight += 1.0f;
            }
        }


        // 5. Always apply collision avoidance
        Vector2 avoidanceForce = CalculateCollisionAvoidance();
        if (avoidanceForce.magnitude > 0.01f)
        {
            combinedForce += avoidanceForce * collisionAvoidanceForce;
            // Avoidance is additive and weighted by its force
        }


        // --- Combine and Finalize ---
        if (combinedForce.magnitude > 0.01f)
        {
            // Clamp magnitude to prevent excessive speed from combined forces, especially avoidance
            return Vector2.ClampMagnitude(combinedForce, 1.0f);
        }
        else if (wanderForce.magnitude > 0.01f && totalWeight < 0.1f) // Ensure wander doesn't fight boundary nudge
        {
            // If all else is zero, just wander (but check totalWeight to avoid conflicts)
            return wanderForce.normalized;
        }

        // Default fallback if truly idle (e.g., perfectly balanced forces)
        return Vector2.zero; // Return zero vector if no direction decided
    }

    void PerceiveEnvironment()
    {
        currentThreats.Clear();
        currentAttractions.Clear();
        currentFollowTarget = null;
        currentObstacles.Clear();

        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, eyeSight, perceptionLayers);
        Transform closestFollowTarget = null;
        float closestFollowDist = float.MaxValue;

        foreach (var collider in nearbyColliders)
        {
            if (collider.transform == transform) continue;

            GameObject targetObj = collider.gameObject;
            bool isThreat = false;
            bool isAttraction = false;
            // bool isFollowTarget = false; // Not needed here
            // bool isObstacle = false; // Not needed here

            foreach (var scaryTag in scaredOfTags)
            {
                if (!string.IsNullOrEmpty(scaryTag) && targetObj.CompareTag(scaryTag))
                {
                    currentThreats.Add(collider.transform);
                    isThreat = true;
                    break;
                }
            }

            if (!isThreat)
            {
                foreach (var attractiveTag in attractedToTags)
                {
                    if (!string.IsNullOrEmpty(attractiveTag) && targetObj.CompareTag(attractiveTag))
                    {
                        currentAttractions.Add(collider.transform);
                        isAttraction = true;
                        break;
                    }
                }
            }

            if (!isThreat && !isAttraction && !string.IsNullOrEmpty(followTag))
            {
                if (targetObj.CompareTag(followTag))
                {
                    float dist = Vector2.Distance(transform.position, collider.transform.position);
                    if (dist < closestFollowDist)
                    {
                        closestFollowDist = dist;
                        closestFollowTarget = collider.transform;
                        // isFollowTarget = true;
                    }
                }
            }

            foreach (var avoidTag in avoidCollisionWithTags)
            {
                if (!string.IsNullOrEmpty(avoidTag) && targetObj.CompareTag(avoidTag))
                {
                    float dist = Vector2.Distance(transform.position, collider.transform.position);
                    if (dist < collisionAvoidanceDistance * 1.2f)
                    {
                        currentObstacles.Add(collider.transform);
                        // isObstacle = true;
                        break;
                    }
                }
            }
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
        return avoidanceDirection; // Return raw avoidance vector
    }


    void UpdateRandomMovementInfluence()
    {
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

        Gizmos.color = Color.cyan;
        Vector2 desiredDir = CalculateDesiredDirection();
        if (Application.isPlaying) Gizmos.DrawRay(transform.position, desiredDir * 1.5f); // Draw desired direction from Agent

        if (Application.isPlaying && fishController != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, fishController.GetCurrentVelocity().normalized * 1.5f); // Draw current velocity
        }


        Vector2 avoidance = CalculateCollisionAvoidance();
        if (Application.isPlaying && avoidance.magnitude > 0.01f)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, avoidance.normalized * collisionAvoidanceForce * 0.5f); // Visualize scaled avoidance influence
        }
    }
}