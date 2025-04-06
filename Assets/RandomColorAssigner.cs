using UnityEngine;
using System.Collections.Generic;
using System;

public class RandomColorAssigner : MonoBehaviour
{
    [System.Serializable]
    public class ColorPair
    {
        public string primaryHexCode;
        public string secondaryHexCode;
    }

    [Header("Color Sources")]
    [Tooltip("List of color pairs to choose from randomly")]
    [SerializeField] private List<ColorPair> colorPairs = new List<ColorPair>();

    [Header("Advanced Color Options")]
    [Tooltip("Enable this to use a random alpha value within the specified range")]
    [SerializeField] private bool randomizeAlpha = false;

    [Tooltip("Minimum alpha value (0-1) if randomization is enabled")]
    [Range(0f, 1f)]
    [SerializeField] private float minAlpha = 0.5f;

    [Tooltip("Maximum alpha value (0-1) if randomization is enabled")]
    [Range(0f, 1f)]
    [SerializeField] private float maxAlpha = 1f;

    [Header("Renderer Targets")]
    [Tooltip("SpriteRenderers to update with the primary color")]
    [SerializeField] private SpriteRenderer[] primaryRenderers;

    [Tooltip("SpriteRenderers to update with the secondary color")]
    [SerializeField] private SpriteRenderer[] secondaryRenderers;

    void Awake()
    {
        // Validate inputs
        if (colorPairs == null || colorPairs.Count == 0)
        {
            Debug.LogError($"[{gameObject.name}] No color pairs assigned to the RandomColorAssigner. Disabling script.", this);
            this.enabled = false;
            return;
        }

        // Assign a random color pair to all targets
        AssignRandomColorPairToAll();
    }

    /// <summary>
    /// Selects a random color pair from the colorPairs list and assigns them
    /// to the primary and secondary renderers respectively.
    /// </summary>
    private void AssignRandomColorPairToAll()
    {
        // Select a random valid color pair
        int randomIndex = UnityEngine.Random.Range(0, colorPairs.Count);
        ColorPair selectedPair = colorPairs[randomIndex];

        if (selectedPair == null ||
            string.IsNullOrWhiteSpace(selectedPair.primaryHexCode) ||
            string.IsNullOrWhiteSpace(selectedPair.secondaryHexCode))
        {
            Debug.LogError($"[{gameObject.name}] Invalid color pair at index {randomIndex}. Using default colors.", this);
            return;
        }

        // Parse colors
        Color primaryColor = Color.white;
        Color secondaryColor = Color.white;

        bool validPrimary = TryParseHexColor(selectedPair.primaryHexCode, out primaryColor);
        bool validSecondary = TryParseHexColor(selectedPair.secondaryHexCode, out secondaryColor);

        if (!validPrimary || !validSecondary)
        {
            Debug.LogWarning($"[{gameObject.name}] Invalid color codes in pair at index {randomIndex}. Using fallback colors.", this);
        }

        // Apply random alpha if enabled
        if (randomizeAlpha)
        {
            float alpha = UnityEngine.Random.Range(minAlpha, maxAlpha);
            primaryColor.a = alpha;
            secondaryColor.a = alpha;
        }

        // Assign to primary renderers
        int primaryCount = 0;
        if (primaryRenderers != null)
        {
            foreach (var renderer in primaryRenderers)
            {
                if (renderer != null)
                {
                    renderer.color = primaryColor;
                    primaryCount++;
                }
            }
        }

        // Assign to secondary renderers
        int secondaryCount = 0;
        if (secondaryRenderers != null)
        {
            foreach (var renderer in secondaryRenderers)
            {
                if (renderer != null)
                {
                    renderer.color = secondaryColor;
                    secondaryCount++;
                }
            }
        }

        // Log the final action
        Debug.Log($"[{gameObject.name}] Assigned primary color '{ColorToHex(primaryColor)}' to {primaryCount} renderer(s) and " +
                  $"secondary color '{ColorToHex(secondaryColor)}' to {secondaryCount} renderer(s).", this);
    }

    /// <summary>
    /// Attempts to parse a hex color string to a Unity Color.
    /// </summary>
    /// <param name="hex">The hex color string (with or without # prefix)</param>
    /// <param name="color">The resulting Unity Color if successful</param>
    /// <returns>True if parsing was successful, false otherwise</returns>
    private bool TryParseHexColor(string hex, out Color color)
    {
        color = Color.white; // Default

        // Remove # if present
        if (hex.StartsWith("#"))
        {
            hex = hex.Substring(1);
        }

        // Check if valid hex length
        if (hex.Length != 6 && hex.Length != 8)
        {
            return false;
        }

        // Try to parse
        if (ColorUtility.TryParseHtmlString("#" + hex, out Color parsedColor))
        {
            color = parsedColor;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Converts a Unity Color to hex string format
    /// </summary>
    /// <param name="color">The color to convert</param>
    /// <returns>Hex string representation of the color</returns>
    private string ColorToHex(Color color)
    {
        return "#" + ColorUtility.ToHtmlStringRGBA(color);
    }
}
