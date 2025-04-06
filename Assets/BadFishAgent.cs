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

    // Private tracking variable
    private bool taggedObjectDetected = false;

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
        // Check for nearby objects with specified tags
        bool detectedThisFrame = DetectNearbyObjects();

        // Update mouth state based on detection
        HandleMouthState(detectedThisFrame);
    }

    /// <summary>
    /// Checks for nearby game objects with specified tags within the detection radius.
    /// </summary>
    /// <returns>True if a tagged object is detected, false otherwise.</returns>
    private bool DetectNearbyObjects()
    {
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
                if (distance <= detectionRadius)
                {
                    return true; // Found a tagged object within range
                }
            }
        }

        // No tagged objects found within range
        return false;
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

    // Helper method to visualize the detection radius in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan; // Changed color for differentiation
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}