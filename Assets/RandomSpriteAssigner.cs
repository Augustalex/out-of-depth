using UnityEngine;
using System.Collections.Generic; // Required for using Lists

// Ensure this GameObject also has a SpriteRenderer component.
[RequireComponent(typeof(SpriteRenderer))]
public class RandomSpriteAssigner : MonoBehaviour
{
    [Tooltip("The list of sprites to choose from randomly.")]
    public List<Sprite> availableSprites;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        // Get the SpriteRenderer component attached to this GameObject.
        spriteRenderer = GetComponent<SpriteRenderer>();

        // --- Input Validation ---

        // Check if the list is assigned and not empty.
        if (availableSprites == null || availableSprites.Count == 0)
        {
            Debug.LogError($"[{gameObject.name}] No sprites assigned to the RandomSpriteAssigner list. Disabling script.", this);
            this.enabled = false; // Disable this script component
            return;
        }

        // Check if all sprites have the same dimensions.
        if (!ValidateSpriteDimensions())
        {
            // Error message is handled within ValidateSpriteDimensions()
            this.enabled = false; // Disable this script component
            return;
        }

        // --- Logic ---

        // If validation passed, select and assign a random sprite.
        AssignRandomSprite();
    }

    /// <summary>
    /// Checks if all sprites in the availableSprites list have the same dimensions.
    /// Logs an error if they don't.
    /// </summary>
    /// <returns>True if all sprites have the same dimensions or if there's only one sprite, False otherwise.</returns>
    private bool ValidateSpriteDimensions()
    {
        // If there's 0 or 1 sprite, dimensions are trivially consistent.
        if (availableSprites.Count <= 1)
        {
            return true;
        }

        // Get the dimensions of the first sprite to use as a reference.
        // Using sprite.rect which respects packing and trimming.
        Rect firstSpriteRect = availableSprites[0].rect;
        float referenceWidth = firstSpriteRect.width;
        float referenceHeight = firstSpriteRect.height;

        // Iterate through the rest of the sprites (starting from the second one).
        for (int i = 1; i < availableSprites.Count; i++)
        {
            Sprite currentSprite = availableSprites[i];
            if (currentSprite == null)
            {
                Debug.LogWarning($"[{gameObject.name}] Found a null entry at index {i} in the availableSprites list.", this);
                continue; // Skip null entries, but warn the user
            }

            Rect currentSpriteRect = currentSprite.rect;

            // Compare dimensions. Use Mathf.Approximately for floating-point comparisons.
            if (!Mathf.Approximately(currentSpriteRect.width, referenceWidth) ||
                !Mathf.Approximately(currentSpriteRect.height, referenceHeight))
            {
                Debug.LogError($"[{gameObject.name}] Sprite dimension mismatch! " +
                               $"Sprite '{availableSprites[0].name}' has dimensions ({referenceWidth}x{referenceHeight}), " +
                               $"but sprite '{currentSprite.name}' (at index {i}) has dimensions ({currentSpriteRect.width}x{currentSpriteRect.height}). " +
                               $"All sprites must have the same dimensions. Disabling script.", this);
                return false; // Dimensions do not match
            }
        }

        // If the loop completes without returning false, all dimensions match.
        return true;
    }

    /// <summary>
    /// Selects a random sprite from the list and assigns it to the SpriteRenderer.
    /// Assumes the list is not empty and validation has passed.
    /// </summary>
    private void AssignRandomSprite()
    {
        // Select a random index from the list.
        int randomIndex = Random.Range(0, availableSprites.Count);

        // Get the sprite at the random index.
        Sprite selectedSprite = availableSprites[randomIndex];

        // Assign the selected sprite to the SpriteRenderer.
        if (selectedSprite != null)
        {
            spriteRenderer.sprite = selectedSprite;
            // Optional: Log which sprite was chosen
            // Debug.Log($"[{gameObject.name}] Assigned random sprite: {selectedSprite.name}", this);
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] The randomly selected sprite at index {randomIndex} was null.", this);
        }
    }
}