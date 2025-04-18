using UnityEngine;
using UnityEngine.UI;

public class UiLifeLightBulbController : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite onSprite; // Sprite for when life is active
    public Sprite offSprite; // Sprite for when life is lost

    private Image bulbRenderer;

    void Start()
    {
        // Get the Image component
        bulbRenderer = GetComponent<Image>();
        if (bulbRenderer == null)
        {
            Debug.LogError("No Image component found on " + gameObject.name);
        }

        // Set the default state to "on"
        RestoreLife();
    }

    public void TakeLife()
    {
        // Change sprite to off (take life)
        if (bulbRenderer != null)
        {
            bulbRenderer.sprite = offSprite;
        }
    }

    public void RestoreLife()
    {
        // Change sprite to on (restore life)
        if (bulbRenderer != null)
        {
            bulbRenderer.sprite = onSprite;
        }
    }

    public bool IsLifeActive()
    {
        // Check if the bulb is on (life is active)
        return bulbRenderer != null && bulbRenderer.sprite == onSprite;
    }
}