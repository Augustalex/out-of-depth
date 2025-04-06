// FishAgent.cs:
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(FishController))]
public class FishAgent : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How much the wander direction randomly changes.")]
    public float movementVariation = 0.2f;
    [Tooltip("How quickly the wander direction changes.")]
    public float randomDirectionChangeSpeed = 0.3f;

    [Header("Perception Settings")]
    [Tooltip("How often the fish checks its surroundings (seconds).")]
    public float perceptionCheckInterval = 0.5f;
    [Tooltip("Which layers the fish can 'see' objects on.")]
    public LayerMask perceptionLayers; // Make sure this is set in the Inspector!

    [Header("Flee Behavior (Highest Priority)")]
    [Tooltip("Tags the fish will flee from.")]
    public List<string> scaredOfTags = new List<string>();
    [Tooltip("Range within which the fish detects threats.")]
    public float eyeSight = 7f;
    [Tooltip("Force multiplier for fleeing.")]
    public float scaredFleeForce = 2.0f;

    [Header("Attraction Behavior (Second Priority)")]
    [Tooltip("Tags the fish is attracted to.")]
    public List<string> attractedToTags = new List<string>();
    [Tooltip("Range within which the fish detects attractions.")]
    public float attractionRange = 5f; // New dedicated range
    [Tooltip("Force multiplier for attraction.")]
    public float attractionForce = 1.0f;

    [Header("Collision Avoidance")]
    [Tooltip("Tags the fish will try to avoid colliding with.")]
    public List<string> avoidCollisionWithTags = new List<string>();
    [Tooltip("Range within which the fish actively avoids collisions.")]
    public float collisionAvoidanceDistance = 1.5f;
    [Tooltip("Force multiplier for collision avoidance.")]
    public float collisionAvoidanceForce = 2.5f;

    [Header("Debug Visualization")]
    public bool showDebugGizmos = true; // Default to true for easier debugging

    // --- Component references ---
    private FishController fishController;

    // --- State tracking ---
    private Vector2 currentWanderDirection;
    private float nextPerceptionCheckTime;
    private List<Transform> currentThreats = new List<Transform>();
    private List<Transform> currentAttractions = new List<Transform>();
    private List<Transform> currentObstacles = new List<Transform>();
    private Vector2 randomMovementInfluence;
    private float nextRandomInfluenceTime;

    /// <summary>
    /// Gets the current underlying wander direction of the fish (used by FishController for idle movement).
    /// </summary>
    public Vector2 CurrentWanderDirection => currentWanderDirection;

    // --- Behavior State Enum for Clarity ---
    public enum FishBehaviorState { Wandering, Fleeing, Attracted }
    public FishBehaviorState CurrentState { get; private set; } = FishBehaviorState.Wandering;


    void Awake()
    {
        fishController = GetComponent<FishController>();
        currentWanderDirection = Random.insideUnitCircle.normalized;
        UpdateRandomMovementInfluence();
        fishController.SetAIControlled(true);

        if (perceptionLayers == 0) // Warn if layer mask isn't set
        {
            Debug.LogWarning($"FishAgent on {gameObject.name} has no Perception Layers set. It might not detect anything.", this);
        }
    }

    void Update()
    {
        // Periodic Perception
        if (Time.time >= nextPerceptionCheckTime)
        {
            PerceiveEnvironment();
            nextPerceptionCheckTime = Time.time + perceptionCheckInterval;
        }

        // Periodic Random Influence Update for Wandering
        if (Time.time >= nextRandomInfluenceTime)
        {
            UpdateRandomMovementInfluence();
            // Adjust wander influence update frequency (e.g., every 1 to 4 seconds)
            nextRandomInfluenceTime = Time.time + Random.Range(1.0f, 4.0f);
        }

        // Smoothly update the base wander direction
        currentWanderDirection = Vector2.Lerp(currentWanderDirection, (currentWanderDirection + randomMovementInfluence).normalized, Time.deltaTime * randomDirectionChangeSpeed).normalized;

        // Calculate the desired high-level direction based on priorities
        Vector2 primaryDesiredDirection = CalculatePriorityDirection();

        // Send the calculated direction (or Vector2.zero for wander) to the controller
        fishController.SetMoveInput(primaryDesiredDirection);
    }

    void PerceiveEnvironment()
    {
        currentThreats.Clear();
        currentAttractions.Clear();
        currentObstacles.Clear();

        // Determine the maximum range needed for the overlap check
        float maxDetectionRange = Mathf.Max(eyeSight, attractionRange, collisionAvoidanceDistance * 1.2f); // Added 1.2f buffer for avoidance detection

        // Perform a single overlap check using the largest required range and the layer mask
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, maxDetectionRange, perceptionLayers);

        foreach (var collider in nearbyColliders)
        {
            if (collider.transform == transform) continue; // Ignore self

            GameObject targetObj = collider.gameObject;
            Vector2 directionToTarget = collider.transform.position - transform.position;
            float distanceSqr = directionToTarget.sqrMagnitude; // Use squared distance for efficiency


            // Check for Attractions (within attractionRange) - Only if not already a threat
            foreach (var attractiveTag in attractedToTags)
            {
                if (!string.IsNullOrEmpty(attractiveTag) && targetObj.CompareTag(attractiveTag))
                {
                    if (distanceSqr < attractionRange * attractionRange)
                    {
                        currentAttractions.Add(collider.transform);
                        goto NextCollider; // Found purpose for this collider
                    }
                }
            }

            // Check for Threats (within eyeSight)
            foreach (var scaryTag in scaredOfTags)
            {
                if (!string.IsNullOrEmpty(scaryTag) && targetObj.CompareTag(scaryTag))
                {
                    if (distanceSqr < eyeSight * eyeSight)
                    {
                        currentThreats.Add(collider.transform);
                        goto NextCollider; // Found purpose for this collider, move to next one
                    }
                }
            }

            // Check for Collision Avoidance Obstacles (within collisionAvoidanceDistance)
            foreach (var avoidTag in avoidCollisionWithTags)
            {
                if (!string.IsNullOrEmpty(avoidTag) && targetObj.CompareTag(avoidTag))
                {
                    // Check slightly beyond the avoidance distance to anticipate
                    if (distanceSqr < (collisionAvoidanceDistance * 1.2f) * (collisionAvoidanceDistance * 1.2f))
                    {
                        currentObstacles.Add(collider.transform);
                        goto NextCollider; // Found purpose for this collider
                    }
                }
            }

        NextCollider:; // Label to jump to next collider in the loop
        }
    }


    Vector2 CalculatePriorityDirection()
    {
        Vector2 combinedForce = Vector2.zero;
        CurrentState = FishBehaviorState.Wandering; // Default state

        // --- Calculate Forces Based on Priority ---

        // 1. Flee from Threats (Highest Priority)
        Vector2 fleeForce = Vector2.zero;
        if (currentThreats.Count > 0)
        {
            foreach (var threat in currentThreats)
            {
                if (threat == null) continue;
                Vector2 dirFromThreat = ((Vector2)transform.position - (Vector2)threat.position);
                float distance = dirFromThreat.magnitude;
                if (distance > 0.01f) // Avoid division by zero
                {
                    // Weight force: stronger when closer
                    float weight = Mathf.Pow(1f - Mathf.Clamp01(distance / eyeSight), 2);
                    fleeForce += dirFromThreat.normalized * weight;
                }
            }
            if (fleeForce.magnitude > 0.01f)
            {
                combinedForce = fleeForce.normalized * scaredFleeForce;
                CurrentState = FishBehaviorState.Fleeing;
                // Fleeing overrides attraction, but avoidance is still added later
            }
        }

        // 2. Move toward Attractions (Second Priority - Only if NOT fleeing)
        Vector2 attractForce = Vector2.zero;
        if (CurrentState != FishBehaviorState.Fleeing && currentAttractions.Count > 0)
        {
            foreach (var attraction in currentAttractions)
            {
                if (attraction == null) continue;
                Vector2 dirToAttraction = ((Vector2)attraction.position - (Vector2)transform.position);
                float distance = dirToAttraction.magnitude;
                if (distance > 0.01f) // Avoid division by zero
                {
                    // Weight force: stronger when closer
                    float weight = 1f - Mathf.Clamp01(distance / attractionRange);
                    attractForce += dirToAttraction.normalized * weight;
                }
            }
            if (attractForce.magnitude > 0.01f)
            {
                combinedForce = attractForce.normalized * attractionForce;
                CurrentState = FishBehaviorState.Attracted;
                // Avoidance is still added later
            }
        }

        // 3. Collision Avoidance (Applied Additively)
        Vector2 avoidanceForce = CalculateCollisionAvoidance();
        if (avoidanceForce.magnitude > 0.01f)
        {
            // Add avoidance force. It blends with flee/attract or modifies wander.
            // If avoidance is very strong, it can dominate the final direction.
            combinedForce += avoidanceForce * collisionAvoidanceForce;
        }

        // --- Finalize ---
        if (combinedForce.magnitude > 0.01f)
        {
            // Return the combined direction if any force was significant
            return combinedForce.normalized;
        }

        // If no primary goal (Flee/Attract) and no significant avoidance,
        // return Vector2.zero. The FishController will use CurrentWanderDirection.
        return Vector2.zero;
    }


    Vector2 CalculateCollisionAvoidance()
    {
        Vector2 avoidanceDirection = Vector2.zero;
        int obstacleCount = 0;

        foreach (var obstacle in currentObstacles)
        {
            if (obstacle == null) continue;

            Vector2 dirToObstacle = (obstacle.position - transform.position);
            float distance = dirToObstacle.magnitude;

            // Check if within the actual avoidance distance (not the slightly larger perception buffer)
            if (distance < collisionAvoidanceDistance && distance > 0.01f)
            {
                obstacleCount++;
                // Calculate direction away from the obstacle
                Vector2 dirFromObstacle = -dirToObstacle.normalized;
                // Weight force: stronger when closer
                float avoidanceStrength = 1f - Mathf.Clamp01(distance / collisionAvoidanceDistance);
                // Add weighted force (squared for stronger effect up close)
                avoidanceDirection += dirFromObstacle * (avoidanceStrength * avoidanceStrength);
            }
        }

        if (obstacleCount > 0)
        {
            // Average the avoidance direction if multiple obstacles are close
            avoidanceDirection /= obstacleCount;
        }

        return avoidanceDirection.normalized; // Return normalized direction
    }


    void UpdateRandomMovementInfluence()
    {
        randomMovementInfluence = Random.insideUnitCircle * movementVariation;
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Draw Eyesight range (for fleeing)
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f); // Transparent Red
        Gizmos.DrawWireSphere(transform.position, eyeSight);

        // Draw Attraction range
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f); // Transparent Yellow
        Gizmos.DrawWireSphere(transform.position, attractionRange);

        // Draw Collision Avoidance range
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Transparent Orange
        Gizmos.DrawWireSphere(transform.position, collisionAvoidanceDistance);


        if (!Application.isPlaying) return;

        // --- Draw Runtime Directions ---
        // Draw wander direction
        Gizmos.color = Color.gray;
        Gizmos.DrawRay(transform.position, CurrentWanderDirection * 1.0f);

        // Draw primary goal direction (Result of Flee/Attract + Avoidance)
        Vector2 primaryDir = CalculatePriorityDirection(); // Re-calculate for gizmo drawing
        if (primaryDir.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.cyan; // Combined Goal
            Gizmos.DrawRay(transform.position, primaryDir * 1.5f);
        }

        // Draw actual velocity
        if (fishController != null)
        {
            Gizmos.color = Color.green; // Actual Movement
            Gizmos.DrawRay(transform.position, fishController.GetCurrentVelocity().normalized * 1.2f);
        }

        // Draw pure avoidance influence vector
        Vector2 avoidance = CalculateCollisionAvoidance();
        if (avoidance.magnitude > 0.01f)
        {
            Gizmos.color = Color.magenta; // Avoidance Vector
            Gizmos.DrawRay(transform.position, avoidance * collisionAvoidanceForce * 0.5f);
        }

        // --- Draw State Indicator ---
        float sphereRadius = 0.2f;
        Vector3 spherePos = transform.position + Vector3.up * 0.5f; // Position above the fish

        switch (CurrentState)
        {
            case FishBehaviorState.Fleeing:
                Gizmos.color = Color.red;
                break;
            case FishBehaviorState.Attracted:
                Gizmos.color = Color.yellow;
                break;
            case FishBehaviorState.Wandering:
            default:
                // Also indicate if actively avoiding even while wandering
                Gizmos.color = (avoidance.magnitude > 0.01f) ? Color.magenta : Color.blue;
                break;
        }
        Gizmos.DrawSphere(spherePos, sphereRadius);

    }
}