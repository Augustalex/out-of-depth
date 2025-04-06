// BadFishAgent.cs:
using UnityEngine;
using System.Collections.Generic;

public class BadFishAgent : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the BadFishController component")]
    public BadFishController fishController; // Renamed reference

    [Header("Detection Settings")]
    [Tooltip("Tags that will trigger the fish's mouth to open")]
    public List<string> detectionTags = new List<string>() { "Player", "Enemy" };

    [Tooltip("Radius within which to detect tagged objects")]
    public float detectionRadius = 5f;

    [Tooltip("Radius within which to attack detected objects (must be smaller than detection radius)")]
    public float attackRange = 2f;

    // Private tracking variables
    private bool taggedObjectDetected = false;
    private bool objectInAttackRange = false;
    private GameObject currentTarget = null;

    private void Start()
    {
        // Attempt to find the controller if not assigned
        if (fishController == null)
        {
            fishController = GetComponent<BadFishController>();

            if (fishController == null)
            {
                Debug.LogError("BadFishController not found! Please assign it in the inspector or add it to the same GameObject.", this);
                enabled = false; // Disable the script if controller is missing
                return;
            }
        }

        // Ensure the mouth starts closed
        fishController.CloseMouth();
    }

    private void Update()
    {
        // Check for nearby objects and get detection results
        DetectionResult result = DetectNearbyObjects();

        // Update mouth state based on detection but not attack
        if (!result.inAttackRange)
        {
            HandleMouthState(result.detected);
        }

        // Handle attack behavior separately
        HandleAttackState(result.inAttackRange);
    }

    // Structure to hold detection results
    private struct DetectionResult
    {
        public bool detected;
        public bool inAttackRange;
        public GameObject target;
    }

    /// <summary>
    /// Checks for nearby game objects with specified tags within the detection radius.
    /// </summary>
    /// <returns>Detection results including if object is detected and if in attack range.</returns>
    private DetectionResult DetectNearbyObjects()
    {
        DetectionResult result = new DetectionResult
        {
            detected = false,
            inAttackRange = false,
            target = null
        };

        float closestDistance = float.MaxValue;

        // Check for each tag
        foreach (string tag in detectionTags)
        {
            // Find all GameObjects with the current tag
            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);

            // Check if any are within range
            foreach (GameObject obj in taggedObjects)
            {
                // Ensure the detected object isn't this fish itself
                if (obj == this.gameObject) continue;

                float distance = Vector3.Distance(transform.position, obj.transform.position);

                // Track the closest object
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    result.target = obj;
                }

                // Check detection range
                if (distance <= detectionRadius)
                {
                    result.detected = true;

                    // Check if within attack range
                    if (distance <= attackRange)
                    {
                        result.inAttackRange = true;
                    }
                }
            }
        }

        currentTarget = result.target; // Store the current target
        return result;
    }

    /// <summary>
    /// Tells the FishController to open or close the mouth based on detection status.
    /// </summary>
    /// <param name="isDetected">Whether a tagged object was detected this frame.</param>
    private void HandleMouthState(bool isDetected)
    {
        // If detection state changed, update the mouth
        if (isDetected != taggedObjectDetected)
        {
            taggedObjectDetected = isDetected; // Update the tracked state

            if (taggedObjectDetected)
            {
                fishController.OpenMouth();
                // Optional: Trigger an animation or sound when opening mouth
                // fishController.TriggerAttackSquish(); // Example if you still want a squish
            }
            else
            {
                fishController.CloseMouth();
                // Optional: Trigger an animation or sound when closing mouth
            }
        }
    }

    /// <summary>
    /// Handles the attack behavior when objects enter attack range.
    /// </summary>
    /// <param name="isInAttackRange">Whether a tagged object is within attack range.</param>
    private void HandleAttackState(bool isInAttackRange)
    {
        // If attack state changed
        if (isInAttackRange != objectInAttackRange)
        {
            objectInAttackRange = isInAttackRange; // Update tracked state

            if (objectInAttackRange)
            {
                // Close mouth when attacking
                fishController.CloseMouth();

                // Execute attack
                fishController.Attack(currentTarget);
            }
        }
    }

    // Helper method to visualize the detection radius in the editor
    private void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Draw attack range with a different color
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}