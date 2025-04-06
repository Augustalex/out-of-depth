// FishAgent.cs:
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(FishMovementController))]
public class FishAgent : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How far ahead to place target positions")]
    public float targetPositionDistance = 3.0f;
    [Tooltip("How often the agent updates its decision (seconds)")]
    public float decisionUpdateInterval = 0.2f;

    [Header("Perception Settings")]
    [Tooltip("How often the fish checks its surroundings (seconds).")]
    public float perceptionCheckInterval = 0.5f;
    [Tooltip("Which layers the fish can 'see' objects on.")]
    public LayerMask perceptionLayers;

    [Header("Flee Behavior (Highest Priority)")]
    [Tooltip("Tags the fish will flee from.")]
    public List<string> scaredOfTags = new List<string>();
    [Tooltip("Range within which the fish detects threats.")]
    public float eyeSight = 7f;
    [Tooltip("Speed multiplier when fleeing")]
    public float fleeSpeedMultiplier = 1.5f;

    [Header("Attraction Behavior (Second Priority)")]
    [Tooltip("Tags the fish is attracted to.")]
    public List<string> attractedToTags = new List<string>();
    [Tooltip("Range within which the fish detects attractions.")]
    public float attractionRange = 5f;
    [Tooltip("Speed multiplier when approaching attractions")]
    public float attractionSpeedMultiplier = 1.2f;

    [Header("Collision Avoidance")]
    [Tooltip("Tags the fish will try to avoid colliding with.")]
    public List<string> avoidCollisionWithTags = new List<string>();
    [Tooltip("Range within which the fish actively avoids collisions.")]
    public float collisionAvoidanceDistance = 1.5f;
    [Tooltip("Distance to move away when avoiding collisions")]
    public float collisionAvoidanceOffset = 1.0f;

    [Header("Dash Behavior")]
    [Tooltip("Whether the fish should dash when fleeing")]
    public bool dashWhenFleeing = true;
    [Tooltip("Minimum distance to threat before considering dash")]
    public float minDistanceToDash = 3.0f;

    [Header("Debug Visualization")]
    public bool showDebugGizmos = true;

    // --- Component references ---
    private FishMovementController movement;

    // --- State tracking ---
    private float nextPerceptionCheckTime;
    private float nextDecisionTime;
    private List<Transform> currentThreats = new List<Transform>();
    private List<Transform> currentAttractions = new List<Transform>();
    private List<Transform> currentObstacles = new List<Transform>();

    // --- Behavior State Enum for Clarity ---
    public enum FishBehaviorState { Wandering, Fleeing, Attracted, AvoidingCollision }
    public FishBehaviorState CurrentState { get; private set; } = FishBehaviorState.Wandering;

    // Target tracking
    private Vector2 currentTargetPosition;
    private Transform currentTargetTransform;
    private bool hasActiveTarget = false;

    void Awake()
    {
        movement = GetComponent<FishMovementController>();

        if (perceptionLayers == 0)
        {
            Debug.LogWarning($"FishAgent on {gameObject.name} has no Perception Layers set. It might not detect anything.", this);
        }

        // Subscribe to movement events
        movement.OnTargetReached += HandleTargetReached;
        movement.OnDashEnd += HandleDashEnd;
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (movement != null)
        {
            movement.OnTargetReached -= HandleTargetReached;
            movement.OnDashEnd -= HandleDashEnd;
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

        // Periodic Decision Making
        if (Time.time >= nextDecisionTime)
        {
            MakeMovementDecision();
            nextDecisionTime = Time.time + decisionUpdateInterval;
        }
    }

    void PerceiveEnvironment()
    {
        currentThreats.Clear();
        currentAttractions.Clear();
        currentObstacles.Clear();

        // Determine the maximum range needed for the overlap check
        float maxDetectionRange = Mathf.Max(eyeSight, attractionRange, collisionAvoidanceDistance * 1.2f);

        // Perform a single overlap check using the largest required range and the layer mask
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, maxDetectionRange, perceptionLayers);

        foreach (var collider in nearbyColliders)
        {
            if (collider.transform == transform) continue; // Ignore self

            GameObject targetObj = collider.gameObject;
            Vector2 directionToTarget = collider.transform.position - transform.position;
            float distanceSqr = directionToTarget.sqrMagnitude;

            // Check for Threats (within eyeSight) - Highest Priority
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

            // Check for Attractions (within attractionRange)
            foreach (var attractiveTag in attractedToTags)
            {
                if (!string.IsNullOrEmpty(attractiveTag) && targetObj.CompareTag(attractiveTag))
                {
                    if (distanceSqr < attractionRange * attractionRange)
                    {
                        currentAttractions.Add(collider.transform);
                        goto NextCollider;
                    }
                }
            }

            // Check for Collision Avoidance Obstacles
            foreach (var avoidTag in avoidCollisionWithTags)
            {
                if (!string.IsNullOrEmpty(avoidTag) && targetObj.CompareTag(avoidTag))
                {
                    if (distanceSqr < (collisionAvoidanceDistance * 1.2f) * (collisionAvoidanceDistance * 1.2f))
                    {
                        currentObstacles.Add(collider.transform);
                        goto NextCollider;
                    }
                }
            }

        NextCollider:;
        }
    }

    void MakeMovementDecision()
    {
        // Reset target tracking for this decision cycle
        hasActiveTarget = false;

        // --- Priority-based decision making ---

        // 1. Check collision avoidance (immediate physical safety)
        if (HandleCollisionAvoidance())
        {
            CurrentState = FishBehaviorState.AvoidingCollision;
            return; // Decision made
        }

        // 2. Check for threats (Highest priority behavior)
        if (HandleFleeThreats())
        {
            CurrentState = FishBehaviorState.Fleeing;
            return; // Decision made
        }

        // 3. Check for attractions (Second priority)
        if (HandleAttractions())
        {
            CurrentState = FishBehaviorState.Attracted;
            return; // Decision made
        }

        // 4. Default: Wander (idle movement handled by the movement controller)
        if (movement.GetCurrentState() != FishMovementController.MovementState.Idle)
        {
            movement.SetIdle();
        }

        CurrentState = FishBehaviorState.Wandering;
    }

    bool HandleCollisionAvoidance()
    {
        if (currentObstacles.Count == 0) return false;

        Vector2 avoidanceDirection = Vector2.zero;
        int validObstacles = 0;

        foreach (var obstacle in currentObstacles)
        {
            if (obstacle == null) continue;

            Vector2 dirToObstacle = (Vector2)(obstacle.position - transform.position);
            float distance = dirToObstacle.magnitude;

            if (distance < collisionAvoidanceDistance && distance > 0.01f)
            {
                validObstacles++;
                // Direction away from obstacle with weight based on proximity
                float weight = 1f - Mathf.Clamp01(distance / collisionAvoidanceDistance);
                avoidanceDirection += -dirToObstacle.normalized * (weight * weight);
            }
        }

        if (validObstacles > 0)
        {
            avoidanceDirection /= validObstacles; // Average

            // Set target position in the avoidance direction
            Vector2 targetPosition = (Vector2)transform.position +
                avoidanceDirection.normalized * collisionAvoidanceOffset;

            // Set a short-term avoidance target
            movement.SetTargetPosition(targetPosition);
            movement.SetTargetSpeed(movement.normalSpeed * 1.1f); // Slightly increased speed

            hasActiveTarget = true;
            currentTargetPosition = targetPosition;
            currentTargetTransform = null;

            return true;
        }

        return false;
    }

    bool HandleFleeThreats()
    {
        if (currentThreats.Count == 0) return false;

        // Find the closest threat
        Transform closestThreat = null;
        float closestDistance = float.MaxValue;

        foreach (var threat in currentThreats)
        {
            if (threat == null) continue;

            float distance = Vector2.Distance(transform.position, threat.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestThreat = threat;
            }
        }

        if (closestThreat != null)
        {
            // Calculate flee direction (away from threat)
            Vector2 fleeDirection = ((Vector2)transform.position - (Vector2)closestThreat.position).normalized;

            // Set target position in the flee direction
            Vector2 targetPosition = (Vector2)transform.position +
                fleeDirection * targetPositionDistance * 1.5f;

            // Try to dash if very close to the threat
            if (dashWhenFleeing && closestDistance < minDistanceToDash && movement.IsDashAvailable())
            {
                // Point in flee direction first
                movement.SetTargetPosition(targetPosition);

                // Then initiate dash
                movement.TryDash();
            }
            else
            {
                // Normal flee
                movement.SetTargetPosition(targetPosition);
                movement.SetTargetSpeed(movement.normalSpeed * fleeSpeedMultiplier);
            }

            hasActiveTarget = true;
            currentTargetPosition = targetPosition;
            currentTargetTransform = null;

            return true;
        }

        return false;
    }

    bool HandleAttractions()
    {
        if (currentAttractions.Count == 0) return false;

        // Find the closest attraction
        Transform bestAttraction = null;
        float closestDistance = float.MaxValue;

        foreach (var attraction in currentAttractions)
        {
            if (attraction == null) continue;

            float distance = Vector2.Distance(transform.position, attraction.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestAttraction = attraction;
            }
        }

        if (bestAttraction != null)
        {
            // Follow the attraction directly
            movement.SetTargetTransform(bestAttraction);
            movement.SetTargetSpeed(movement.normalSpeed * attractionSpeedMultiplier);

            hasActiveTarget = true;
            currentTargetTransform = bestAttraction;
            currentTargetPosition = bestAttraction.position;

            return true;
        }

        return false;
    }

    // Event handlers
    private void HandleTargetReached()
    {
        // If we reached a target during collision avoidance or fleeing,
        // immediately make a new decision
        if (CurrentState == FishBehaviorState.AvoidingCollision ||
            CurrentState == FishBehaviorState.Fleeing)
        {
            nextDecisionTime = Time.time; // Decide immediately
        }
    }

    private void HandleDashEnd()
    {
        // After a dash completes, immediately make a new decision
        nextDecisionTime = Time.time;
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Draw perception ranges
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f); // Transparent Red - Threats
        Gizmos.DrawWireSphere(transform.position, eyeSight);

        Gizmos.color = new Color(1f, 1f, 0f, 0.2f); // Transparent Yellow - Attractions
        Gizmos.DrawWireSphere(transform.position, attractionRange);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Transparent Orange - Collision Avoidance
        Gizmos.DrawWireSphere(transform.position, collisionAvoidanceDistance);

        if (!Application.isPlaying || movement == null) return;

        // Draw current target if we have one
        if (hasActiveTarget)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(currentTargetPosition, 0.2f);

            if (currentTargetTransform != null)
            {
                // Draw line to moving target
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, currentTargetTransform.position);
            }
            else
            {
                // Draw line to position target
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, currentTargetPosition);
            }
        }

        // Draw wander direction when idling
        if (CurrentState == FishBehaviorState.Wandering)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawRay(transform.position, movement.GetWanderDirection() * 1.5f);
        }

        // Draw current movement velocity
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, movement.GetCurrentVelocity().normalized * 1.2f);

        // Draw state indicator
        float sphereRadius = 0.2f;
        Vector3 spherePos = transform.position + Vector3.up * 0.5f;

        switch (CurrentState)
        {
            case FishBehaviorState.Fleeing:
                Gizmos.color = Color.red;
                break;
            case FishBehaviorState.Attracted:
                Gizmos.color = Color.yellow;
                break;
            case FishBehaviorState.AvoidingCollision:
                Gizmos.color = Color.magenta;
                break;
            case FishBehaviorState.Wandering:
            default:
                Gizmos.color = Color.blue;
                break;
        }
        Gizmos.DrawSphere(spherePos, sphereRadius);
    }
}