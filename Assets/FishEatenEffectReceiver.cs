using UnityEngine;

public class FishEatenEffectReceiver : MonoBehaviour
{
    public float hunger = 0.8f; // Amount of hunger to increase when a fish is eaten

    [Tooltip("Reference to the Eater component that detects consumed objects")]
    [SerializeField] private Eater eater;

    [SerializeField] private UiHungerController uiHungerController;
    [SerializeField] private PlayerHurt playerHurt;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (eater == null)
            eater = GetComponentInParent<Eater>();

        if (eater == null)
            Debug.LogError("SnusEffectReceiver: Eater reference not found!", this);
        else
            // Subscribe to the OnObjectEaten event
            eater.OnObjectEaten += HandleObjectEaten;

        if (uiHungerController != null)
        {
            uiHungerController.UpdateHungerFromStats(hunger);
            Debug.Log("Hunger initialized to: " + hunger);
        }
        else
        {
            Debug.LogError("UiHungerController reference not set!", this);
        }
    }

    void Update()
    {
        hunger -= Time.deltaTime * 0.01f;

        if (hunger <= 0)
        {
            playerHurt.OnPlayerHurt();
            hunger = 0.1f;
        }

        hunger = Mathf.Clamp(hunger, 0f, 1f); // Ensure hunger doesn't exceed 1

        //Slowly over time decrease the hunger   
        if (uiHungerController != null)
        {
            uiHungerController.UpdateHungerFromStats(hunger);
        }
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
        if (eatenObject.CompareTag("Fish"))
        {
            // Trigger the boost effect on the player controller
            if (uiHungerController != null)
            {
                hunger += 0.1f;
                hunger = Mathf.Clamp(hunger, 0f, 1f); // Ensure hunger doesn't exceed 1
                uiHungerController.UpdateHungerFromStats(hunger);
                Debug.Log("Fish consumed! Hunger increased!");
            }
        }
    }
}
