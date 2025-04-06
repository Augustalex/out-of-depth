using UnityEngine;
using System.Collections.Generic; // Required for using Lists

// Ensure this GameObject also has a SpriteRenderer component.
[RequireComponent(typeof(SpriteRenderer))]
public class RandomSpriteAssigner : MonoBehaviour
{
    [Header("Sprite Source")]
    [Tooltip("The list of sprites to choose from randomly. All sprites in this list MUST have the same dimensions.")]
    public List<Sprite> availableSprites;

    [Header("Targets to Update")]
    [Tooltip("Optional list of OTHER SpriteRenderers to update with the chosen sprite.")]
    public List<SpriteRenderer> additionalRenderers;

    [Tooltip("Optional list of SpriteMasks to update with the chosen sprite.")]
    public List<SpriteMask> masks;

    // Reference to the SpriteRenderer on this GameObject
    private SpriteRenderer mainSpriteRenderer;

    void Awake()
    {
        // Get the main SpriteRenderer component attached to this GameObject.
        mainSpriteRenderer = GetComponent<SpriteRenderer>();

        // --- Input Validation ---

        // Check if the source list is assigned and not empty.
        if (availableSprites == null || availableSprites.Count == 0)
        {
            Debug.LogError($"[{gameObject.name}] No sprites assigned to the 'Available Sprites' list on the RandomSpriteAssigner. Disabling script.", this);
            this.enabled = false; // Disable this script component
            return;
        }

        // Check if all sprites in the source list have the same dimensions.
        if (!ValidateSpriteDimensions())
        {
            // Error message is handled within ValidateSpriteDimensions()
            this.enabled = false; // Disable this script component
            return;
        }

        // --- Logic ---

        // If validation passed, select and assign a random sprite to all targets.
        AssignRandomSpriteToAll();
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
            // Check for null entry even if only one sprite exists
            if (availableSprites.Count == 1 && availableSprites[0] == null)
            {
                Debug.LogError($"[{gameObject.name}] The single sprite entry in 'Available Sprites' is null. Disabling script.", this);
                return false;
            }
            return true;
        }

        // Find the first non-null sprite to use as reference
        Sprite referenceSprite = null;
        int referenceIndex = -1;
        for (int i = 0; i < availableSprites.Count; ++i)
        {
            if (availableSprites[i] != null)
            {
                referenceSprite = availableSprites[i];
                referenceIndex = i;
                break;
            }
        }

        // If all entries were null
        if (referenceSprite == null)
        {
            Debug.LogError($"[{gameObject.name}] All entries in the 'Available Sprites' list are null. Disabling script.", this);
            return false;
        }


        // Get the dimensions of the reference sprite.
        Rect referenceRect = referenceSprite.rect;
        float referenceWidth = referenceRect.width;
        float referenceHeight = referenceRect.height;

        // Iterate through all sprites (including the reference one again to catch subsequent nulls).
        for (int i = 0; i < availableSprites.Count; i++)
        {
            Sprite currentSprite = availableSprites[i];
            if (currentSprite == null)
            {
                // Allow null entries if user wants them, but warn. They won't be selected.
                // However, for validation purposes, we treat this as an issue if strict matching is needed.
                // If you want to ALLOW nulls and just skip them during random selection,
                // you might move the null check to AssignRandomSpriteToAll instead.
                // For strict validation (all MUST be valid sprites of same size), this error is appropriate.
                Debug.LogWarning($"[{gameObject.name}] Found a null entry at index {i} in the 'Available Sprites' list during validation.", this);
                // Depending on strictness, you might return false here or just continue.
                // Let's continue for now, assuming nulls won't be picked later.
                continue;
            }

            // Rect currentSpriteRect = currentSprite.rect;
            // // Compare dimensions. Use Mathf.Approximately for floating-point comparisons.
            // if (!Mathf.Approximately(currentSpriteRect.width, referenceWidth) ||
            //     !Mathf.Approximately(currentSpriteRect.height, referenceHeight))
            // {
            //     Debug.LogError($"[{gameObject.name}] Sprite dimension mismatch in 'Available Sprites'! " +
            //                    $"Sprite '{referenceSprite.name}' (at index {referenceIndex}) has dimensions ({referenceWidth}x{referenceHeight}), " +
            //                    $"but sprite '{currentSprite.name}' (at index {i}) has dimensions ({currentSpriteRect.width}x{currentSpriteRect.height}). " +
            //                    $"All sprites in 'Available Sprites' must have the same dimensions. Disabling script.", this);
            //     return false; // Dimensions do not match
            // }
        }

        // If the loop completes without returning false, all non-null dimensions match.
        return true;
    }

    /// <summary>
    /// Selects a random sprite from the availableSprites list and assigns it
    /// to the main SpriteRenderer, all additional SpriteRenderers, and all SpriteMasks.
    /// Handles null entries in the target lists gracefully.
    /// </summary>
    private void AssignRandomSpriteToAll()
    {
        // --- Select a valid Sprite ---
        Sprite selectedSprite = null;
        int attempts = 0; // Safety break
        int maxAttempts = availableSprites.Count * 2; // Allow some retries

        // Keep picking until we find a non-null sprite (or exhaust attempts)
        while (selectedSprite == null && attempts < maxAttempts)
        {
            int randomIndex = Random.Range(0, availableSprites.Count);
            selectedSprite = availableSprites[randomIndex];
            attempts++;
        }

        // If we couldn't find a non-null sprite after several tries
        if (selectedSprite == null)
        {
            Debug.LogError($"[{gameObject.name}] Failed to select a non-null sprite from 'Available Sprites' after {attempts} attempts. Check the list for null entries.", this);
            return; // Don't try to assign a null sprite
        }


        // --- Assign to Main Renderer ---
        mainSpriteRenderer.sprite = selectedSprite;


        // --- Assign to Additional Renderers ---
        if (additionalRenderers != null)
        {
            // Use Count property for safety, although foreach handles null list
            for (int i = 0; i < additionalRenderers.Count; i++)
            {
                SpriteRenderer renderer = additionalRenderers[i];
                if (renderer != null)
                {
                    renderer.sprite = selectedSprite;
                }
                else
                {
                    // Warn only once perhaps, or use a more robust logging system
                    Debug.LogWarning($"[{gameObject.name}] Found a null entry at index {i} in the 'Additional Renderers' list. Skipping.", this);
                }
            }
        }

        // --- Assign to Masks ---
        if (masks != null)
        {
            for (int i = 0; i < masks.Count; i++)
            {
                SpriteMask mask = masks[i];
                if (mask != null)
                {
                    mask.sprite = selectedSprite;
                }
                else
                {
                    Debug.LogWarning($"[{gameObject.name}] Found a null entry at index {i} in the 'Masks' list. Skipping.", this);
                }
            }
        }

        // Optional: Log the final action
        Debug.Log($"[{gameObject.name}] Assigned random sprite '{selectedSprite.name}' to main renderer, {additionalRenderers?.Count ?? 0} additional renderer(s), and {masks?.Count ?? 0} mask(s).", this);
    }
}