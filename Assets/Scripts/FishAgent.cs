using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(FishController))]
public class FishAgent : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Maximum strength of random movement variations")]
    public float movementVariation = 0.5f;
    [Tooltip("Fish tries to stay within this distance from its spawn point")]
    public float movementRadius = 10f;
    [Tooltip("Strength of the desire to stay within movement radius")]
    public float boundaryForce = 5f;

    [Header("Perception Settings")]
    [Tooltip("How far the fish can see other objects")]
    public float eyeSight = 8f;
    [Tooltip("How often (in seconds) the fish checks for objects of interest")]
    public float perceptionCheckInterval = 1f;
    [Tooltip("Layer mask for objects the fish can perceive")]
    public LayerMask perceptionLayers;

    [Header("Behavior Settings")]
    [Tooltip("Tags that scare the fish away")]
    public List<string> scaredOfTags = new List<string>();
    [Tooltip("Force multiplier when fleeing from scary objects")]
    public float scaredFleeForce = 3f;

    [Tooltip("Tags the fish is attracted to")]
    public List<string> attractedToTags = new List<string>();
    [Tooltip("Force multiplier when moving toward attractive objects")]
    public float attractionForce = 1.5f;

    [Tooltip("Tag the fish will follow if seen")]
    public string followTag;
    [Tooltip("Force multiplier when following target")]
    public float followForce = 2f;
    [Tooltip("How close the fish tries to get to follow target")]
    public float followDistance = 3f;

    [Header("Collision Avoidance")]
    [Tooltip("Tags to avoid colliding with")]
    public List<string> avoidCollisionWithTags = new List<string>();
    [Tooltip("Distance at which collision avoidance activates")]
    public float collisionAvoidanceDistance = 2f;
    [Tooltip("Force applied to avoid collisions")]
    public float collisionAvoidanceForce = 2f;

    [Header("Debug Visualization")]
    public bool showDebugGizmos = false;

    // Component references
    private FishController fishController;
    private Vector3 startPosition;

    // State tracking
    private Vector2 currentMovementDirection;
    private float nextPerceptionCheckTime;
    private Transform currentFollowTarget;
    private List<Transform> currentThreats = new List<Transform>();
    private List<Transform> currentAttractions = new List<Transform>();
    private List<Transform> currentObstacles = new List<Transform>();

    // Movement state
    private Vector2 randomMovementInfluence;
    private float nextRandomMovementTime;

    private void Awake()
    {
        fishController = GetComponent<FishController>();
        startPosition = transform.position;

        // Initialize with a random movement direction
        currentMovementDirection = Random.insideUnitCircle.normalized;
        UpdateRandomMovementInfluence();

        // Tell the FishController this is AI-controlled
        fishController.SetAIControlled(true);
    }

    private void Start()
    {
        // No special initialization needed for tag-based system
    }

    private void Update()
    {
        // Check for perception updates on interval
        if (Time.time >= nextPerceptionCheckTime)
        {
            PerceiveEnvironment();
            nextPerceptionCheckTime = Time.time + perceptionCheckInterval;
        }

        // Update random movement influence
        if (Time.time >= nextRandomMovementTime)
        {
            UpdateRandomMovementInfluence();
            nextRandomMovementTime = Time.time + Random.Range(1f, 3f);
        }

        // Calculate all movement influences
        Vector2 movement = CalculateMovement();

        // Apply movement direction to the fish controller
        fishController.SetMoveInput(movement);
    }

    private Vector2 CalculateMovement()
    {
        Vector2 finalDirection = Vector2.zero;
        bool hasHighPriorityBehavior = false;

        // High priority: Flee from threats
        if (currentThreats.Count > 0)
        {
            hasHighPriorityBehavior = true;
            Vector2 fleeDirection = Vector2.zero;

            foreach (var threat in currentThreats)
            {
                if (threat == null) continue;

                Vector2 dirFromThreat = (Vector2)(transform.position - threat.position).normalized;
                float distance = Vector2.Distance(transform.position, threat.position);
                float influenceStrength = 1f - Mathf.Clamp01(distance / eyeSight);

                fleeDirection += dirFromThreat * influenceStrength;
            }

            if (fleeDirection.magnitude > 0.1f)
            {
                finalDirection += fleeDirection.normalized * scaredFleeForce;
            }
        }

        // Medium priority: Follow target
        if (!hasHighPriorityBehavior && currentFollowTarget != null)
        {
            hasHighPriorityBehavior = true;
            Vector2 dirToTarget = (Vector2)(currentFollowTarget.position - transform.position);
            float distance = dirToTarget.magnitude;

            // Only move towards target if we're farther than follow distance
            if (distance > followDistance)
            {
                finalDirection += dirToTarget.normalized * followForce;
            }
            else
            {
                // Maintain position at follow distance
                finalDirection += (dirToTarget.normalized * (distance / followDistance - 1f));
            }
        }

        // Lower priority: Move toward attractions
        if (!hasHighPriorityBehavior && currentAttractions.Count > 0)
        {
            Vector2 attractDirection = Vector2.zero;

            foreach (var attraction in currentAttractions)
            {
                if (attraction == null) continue;

                Vector2 dirToAttraction = (Vector2)(attraction.position - transform.position).normalized;
                float distance = Vector2.Distance(transform.position, attraction.position);
                float influenceStrength = 1f - Mathf.Clamp01(distance / eyeSight);

                attractDirection += dirToAttraction * influenceStrength;
            }

            if (attractDirection.magnitude > 0.1f)
            {
                finalDirection += attractDirection.normalized * attractionForce;
            }
        }

        // Default behavior: Random movement within radius
        if (finalDirection.magnitude < 0.1f)
        {
            // Base movement on current direction plus random influence
            finalDirection = currentMovementDirection + randomMovementInfluence;

            // Apply boundary force if getting too far from start position
            float distanceFromStart = Vector2.Distance(transform.position, startPosition);
            if (distanceFromStart > movementRadius * 0.6f)
            {
                // The further we are, the stronger we try to return
                float boundaryMultiplier = Mathf.Clamp01((distanceFromStart - movementRadius * 0.6f) / (movementRadius * 0.4f));
                Vector2 dirToStart = (Vector2)(startPosition - transform.position).normalized;
                finalDirection += dirToStart * boundaryMultiplier * boundaryForce;
            }
        }

        // Always apply collision avoidance
        finalDirection += CalculateCollisionAvoidance();

        // Normalize the final direction
        return finalDirection.normalized;
    }

    private void PerceiveEnvironment()
    {
        // Clear previous perception results
        currentThreats.Clear();
        currentAttractions.Clear();
        currentFollowTarget = null;
        currentObstacles.Clear();

        // Detect all colliders within eyesight
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, eyeSight, perceptionLayers);

        foreach (var collider in nearbyColliders)
        {
            // Skip self
            if (collider.transform == transform) continue;

            bool processed = false;
            GameObject targetObj = collider.gameObject;

            // Check if this object has a tag we're scared of
            foreach (var scaryTag in scaredOfTags)
            {
                if (string.IsNullOrEmpty(scaryTag)) continue;

                if (targetObj.CompareTag(scaryTag))
                {
                    currentThreats.Add(collider.transform);
                    processed = true;
                    break;
                }
            }

            // Check if this object has a tag we're attracted to
            if (!processed)
            {
                foreach (var attractiveTag in attractedToTags)
                {
                    if (string.IsNullOrEmpty(attractiveTag)) continue;

                    if (targetObj.CompareTag(attractiveTag))
                    {
                        currentAttractions.Add(collider.transform);
                        processed = true;
                        break;
                    }
                }
            }

            // Check if this object has a tag we want to follow
            if (!processed && !string.IsNullOrEmpty(followTag))
            {
                if (targetObj.CompareTag(followTag))
                {
                    // Only follow one target at a time
                    currentFollowTarget = collider.transform;
                    processed = true;
                }
            }

            // Check if this is an object we should avoid colliding with
            foreach (var avoidTag in avoidCollisionWithTags)
            {
                if (string.IsNullOrEmpty(avoidTag)) continue;

                if (targetObj.CompareTag(avoidTag))
                {
                    currentObstacles.Add(collider.transform);
                    break;
                }
            }
        }
    }

    private Vector2 CalculateCollisionAvoidance()
    {
        Vector2 avoidanceDirection = Vector2.zero;

        foreach (var obstacle in currentObstacles)
        {
            if (obstacle == null) continue;

            float distance = Vector2.Distance(transform.position, obstacle.position);

            // Only avoid if within collision avoidance distance
            if (distance < collisionAvoidanceDistance)
            {
                Vector2 dirFromObstacle = (Vector2)(transform.position - obstacle.position).normalized;
                float avoidanceStrength = 1f - Mathf.Clamp01(distance / collisionAvoidanceDistance);

                avoidanceDirection += dirFromObstacle * avoidanceStrength;
            }
        }

        if (avoidanceDirection.magnitude > 0.1f)
        {
            return avoidanceDirection.normalized * collisionAvoidanceForce;
        }

        return Vector2.zero;
    }

    private void UpdateRandomMovementInfluence()
    {
        // Add some randomness to movement
        randomMovementInfluence = Random.insideUnitCircle * movementVariation;

        // Update current movement direction slightly
        currentMovementDirection = (currentMovementDirection + randomMovementInfluence * 0.2f).normalized;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Draw movement radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(startPosition, movementRadius);

        // Draw eyesight
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, eyeSight);

        // Draw collision avoidance radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, collisionAvoidanceDistance);

        // Draw movement direction
        Gizmos.color = Color.green;
        Vector2 currentMovement = CalculateMovement();
        Gizmos.DrawRay(transform.position, currentMovement * 2);
    }
}