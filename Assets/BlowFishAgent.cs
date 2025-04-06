using UnityEngine;
using System.Collections.Generic;

public class BlowFishAgent : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the BlowFishController component")]
    public BlowFishController blowFishController;

    [Header("Detection Settings")]
    [Tooltip("Tags that will trigger the blowfish to expand")]
    public List<string> detectionTags = new List<string>() { "Player", "Enemy" };

    [Tooltip("Radius within which to detect tagged objects")]
    public float detectionRadius = 5f;

    [Tooltip("Time in seconds to wait after losing sight of tagged objects before returning to small state")]
    public float shrinkCooldown = 3f;

    [Header("Mouth Animation Settings (Small State)")]
    [Tooltip("Minimum time between mouth openings")]
    public float minTimeBetweenMouthOpen = 2f;

    [Tooltip("Maximum time between mouth openings")]
    public float maxTimeBetweenMouthOpen = 5f;

    [Tooltip("How long the mouth stays open before closing")]
    public float mouthOpenDuration = 0.5f;

    // Private tracking variables
    private float shrinkTimer = 0f;
    private float nextMouthActionTime = 0f;
    private float mouthCloseTime = 0f;
    private bool waitingToClose = false;
    private bool taggedObjectDetected = false;

    private void Start()
    {
        if (blowFishController == null)
        {
            blowFishController = GetComponent<BlowFishController>();

            if (blowFishController == null)
            {
                Debug.LogError("BlowFishController not found! Please assign it in the inspector or add it to the same GameObject.", this);
                enabled = false;
                return;
            }
        }

        // Initialize with random time for first mouth open
        nextMouthActionTime = Time.time + Random.Range(minTimeBetweenMouthOpen, maxTimeBetweenMouthOpen);
    }

    private void Update()
    {
        // Check for nearby objects with specified tags
        DetectNearbyObjects();

        // Handle state transitions based on detection
        HandleStateTransitions();

        // Handle mouth animations when in small state
        if (blowFishController.IsSmallState())
        {
            HandleMouthAnimations();
        }
    }

    private void DetectNearbyObjects()
    {
        // Reset detection flag
        bool detectedThisFrame = false;

        // Check for each tag
        foreach (string tag in detectionTags)
        {
            // Find all GameObjects with the current tag
            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);

            // Check if any are within range
            foreach (GameObject obj in taggedObjects)
            {
                float distance = Vector3.Distance(transform.position, obj.transform.position);
                if (distance <= detectionRadius)
                {
                    detectedThisFrame = true;
                    break;
                }
            }

            if (detectedThisFrame) break;
        }

        // Update detection state
        taggedObjectDetected = detectedThisFrame;
    }

    private void HandleStateTransitions()
    {
        if (taggedObjectDetected)
        {
            // Reset the shrink timer when a tagged object is detected
            shrinkTimer = 0f;

            // Change to big state if not already
            if (blowFishController.IsSmallState())
            {
                blowFishController.SetBigState();
                blowFishController.TriggerAttackSquish(); // Trigger attack squish when expanding
            }
        }
        else
        {
            // If no objects detected and currently big, increment timer
            if (blowFishController.IsBigState())
            {
                shrinkTimer += Time.deltaTime;

                // Change to small state after cooldown
                if (shrinkTimer >= shrinkCooldown)
                {
                    blowFishController.TriggerAttackSquish(); // Trigger attack squish before shrinking
                    blowFishController.SetSmallState();
                    // Reset to ensure mouth is closed when going back to small state
                    blowFishController.CloseMouth();
                    waitingToClose = false;

                    // Set next mouth action time
                    nextMouthActionTime = Time.time + Random.Range(minTimeBetweenMouthOpen, maxTimeBetweenMouthOpen);
                }
            }
        }
    }

    private void HandleMouthAnimations()
    {
        float currentTime = Time.time;

        // If waiting to close the mouth
        if (waitingToClose)
        {
            if (currentTime >= mouthCloseTime)
            {
                blowFishController.CloseMouth();
                waitingToClose = false;

                // Schedule next mouth opening
                nextMouthActionTime = currentTime + Random.Range(minTimeBetweenMouthOpen, maxTimeBetweenMouthOpen);
            }
        }
        // If it's time to open the mouth
        else if (currentTime >= nextMouthActionTime)
        {
            blowFishController.OpenMouth();
            waitingToClose = true;
            mouthCloseTime = currentTime + mouthOpenDuration;
        }
    }

    // Helper method to visualize the detection radius in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
