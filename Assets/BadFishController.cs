// BadFishController.cs:
using UnityEngine;

public class BadFishController : MonoBehaviour
{
    [Header("Mouth Control References")]
    [Tooltip("Reference to the BodyController (or similar script) that handles mouth visuals")]
    public BodyController bodyController; // Assumes you still use a BodyController

    // Optional: Reference to the FishSquisher if you still want squish animations
    [Header("Animation References (Optional)")]
    [Tooltip("Reference to the FishSquisher component for animations")]
    public FishSquisher fishSquisher;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Attempt to find the BodyController if not assigned
        if (bodyController == null)
        {
            // Assumes BodyController is a child component
            bodyController = GetComponentInChildren<BodyController>();
            if (bodyController == null)
            {
                Debug.LogError("BodyController component not found! Please assign it or ensure it's a child.", this);
                // Depending on your setup, you might disable the controller or agent here
            }
        }

        // Attempt to find the FishSquisher if not assigned (optional)
        if (fishSquisher == null)
        {
            fishSquisher = GetComponentInChildren<FishSquisher>();
            // No error needed if it's optional, just won't trigger squish
            // if (fishSquisher == null) {
            //     Debug.LogWarning("FishSquisher component not found!", this);
            // }
        }

        // Ensure mouth starts closed
        CloseMouth();
    }

    /// <summary>
    /// Opens the fish's mouth via the BodyController.
    /// </summary>
    public void OpenMouth()
    {
        if (bodyController != null)
        {
            bodyController.SetMouthState(true);
        }
        else
        {
            Debug.LogError("BodyController reference is missing! Cannot open mouth.", this);
        }
    }

    /// <summary>
    /// Closes the fish's mouth via the BodyController.
    /// </summary>
    public void CloseMouth()
    {
        if (bodyController != null)
        {
            bodyController.SetMouthState(false);
        }
        else
        {
            Debug.LogError("BodyController reference is missing! Cannot close mouth.", this);
        }
    }

    /// <summary>
    /// Checks if the mouth is currently open via the BodyController.
    /// </summary>
    /// <returns>True if the mouth is open, false otherwise.</returns>
    public bool IsMouthOpen()
    {
        if (bodyController == null)
        {
            Debug.LogError("BodyController reference is missing! Cannot check mouth state.", this);
            return false; // Assume closed if controller is missing
        }

        return bodyController.IsMouthOpen();
    }

    /// <summary>
    /// Triggers the attack squish animation on the FishSquisher component (Optional).
    /// </summary>
    public void TriggerAttackSquish()
    {
        if (fishSquisher != null)
        {
            fishSquisher.TriggerSquish(FishSquisher.SquishActionType.Attack);
        }
        // No warning needed if optional and not found
        // else
        // {
        //     Debug.LogWarning("FishSquisher component not found! Cannot trigger squish.", this);
        // }
    }

    /// <summary>
    /// Performs an attack action on the target.
    /// </summary>
    /// <param name="target">The GameObject being attacked (can be null)</param>
    public void Attack(GameObject target)
    {
        // Trigger attack animation if available
        TriggerAttackSquish();

        // Call the attack event that can be implemented later
        OnAttack(target);

        // Debug log for now
        Debug.Log("Fish attacking: " + (target != null ? target.name : "no target"), this);
    }

    /// <summary>
    /// Override this method to implement custom attack logic.
    /// </summary>
    /// <param name="target">The GameObject being attacked (can be null)</param>
    protected virtual void OnAttack(GameObject target)
    {
        // This is a placeholder for custom attack logic
        // Override this in derived classes to implement specific behavior
    }
}