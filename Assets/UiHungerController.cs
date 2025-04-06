using UnityEngine;
using UnityEngine.UI;

public class UiHungerController : MonoBehaviour
{
    // Assign the 'FullFishImage' GameObject's Image component in the Inspector
    [SerializeField] private Image fullFishImage;

    // Optional: Add references to track the player's hunger
    // [SerializeField] private PlayerStats playerStats;

    // Call this function whenever the hunger value changes
    // hungerValue should be normalized (0.0 = empty/starving, 1.0 = full)
    public void UpdateHungerVisual(float normalizedHunger)
    {
        if (fullFishImage == null)
        {
            Debug.LogError("Full Fish Image is not assigned in the HungerBarController!");
            return;
        }

        // Clamp the value just in case it goes outside the 0-1 range
        normalizedHunger = Mathf.Clamp01(normalizedHunger);

        // Set the fill amount
        fullFishImage.fillAmount = normalizedHunger;
    }

    // --- Example Usage (Call this from your game logic) ---
    // Example: Assuming you have currentHunger and maxHunger variables
    public void UpdateHungerFromStats(float currentHunger, float maxHunger)
    {
        if (maxHunger <= 0) // Avoid division by zero
        {
            UpdateHungerVisual(0f);
            return;
        }

        float normalizedValue = currentHunger / maxHunger;
        UpdateHungerVisual(normalizedValue);
    }

    // --- Example: Testing the bar in the editor ---
    [Range(0f, 1f)] // Adds a slider in the Inspector for testing
    public float testHungerValue = 1f;

    private void Update() // Update is fine for testing, but call UpdateHungerVisual directly when hunger changes in game
    {
#if UNITY_EDITOR // Only run test logic in the editor
        if (Application.isPlaying) // Optional: only update if game is running
        {
            UpdateHungerVisual(testHungerValue);
        }
#endif
    }

}