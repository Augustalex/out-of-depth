using UnityEngine;

public class SnusEffectReceiver : MonoBehaviour
{
    [Tooltip("Reference to the PlayerController to apply boost effects")]
    [SerializeField] private PlayerController playerController;

    [Tooltip("Reference to the Eater component that detects consumed objects")]
    [SerializeField] private Eater eater;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Auto-find references if not set in inspector
        if (playerController == null)
            playerController = GetComponentInParent<PlayerController>();

        if (eater == null)
            eater = GetComponentInParent<Eater>();

        // Check if components were found
        if (playerController == null)
            Debug.LogError("SnusEffectReceiver: PlayerController reference not found!", this);

        if (eater == null)
            Debug.LogError("SnusEffectReceiver: Eater reference not found!", this);
        else
            // Subscribe to the OnObjectEaten event
            eater.OnObjectEaten += HandleObjectEaten;
    }

    private void OnDestroy()
    {
        // Unsubscribe from the event when this component is destroyed
        if (eater != null)
            eater.OnObjectEaten -= HandleObjectEaten;
    }

    // Handler for when an object is eaten
    private void HandleObjectEaten(GameObject eatenObject, Edible edibleComponent)
    {
        // Check if the eaten object has the "Dosa" tag
        if (eatenObject.CompareTag("Dosa"))
        {
            // Trigger the boost effect on the player controller
            if (playerController != null)
            {
                playerController.Boost();
                Debug.Log("Dosa consumed! Speed boost activated!");
            }
        }
    }
}
