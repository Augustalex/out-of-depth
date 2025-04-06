using UnityEngine;

/// <summary>
/// Data Transfer Object holding parameters for spawning an item.
/// </summary>
[System.Serializable] // Makes it visible in the Inspector if used directly in another script, though not strictly necessary here.
public class SpawnParameters
{
    [Tooltip("The Prefab to instantiate.")]
    public GameObject ItemPrefab;

    [Tooltip("The world position where the item should be spawned.")]
    public Vector3 Position;

    [Tooltip("Should the spawned item have a random Z rotation applied?")]
    public bool RandomRotation = false;

    [Tooltip("Maximum random angle deviation (0-360) from the prefab's original Z rotation.")]
    [Range(0f, 360f)]
    public float MaxRandomRotationAngle = 45f;

    [Tooltip("Should the spawned item be randomly scaled (uniformly)?")]
    public bool RandomScale = false;

    [Tooltip("Minimum scale multiplier (e.g., 0.8 for 80% minimum size). Must be <= 1.")]
    [Range(0.01f, 1f)] // Ensure min scale is not zero and not > 1
    public float MinScale = 0.8f;

    [Tooltip("Maximum scale multiplier (e.g., 1.2 for 120% maximum size). Must be >= 1.")]
    [Min(1f)] // Ensure max scale is at least 1
    public float MaxScale = 1.2f;

    // --- Constructor (Optional but good practice) ---
    public SpawnParameters(
        GameObject itemPrefab,
        Vector3 position,
        bool randomRotation = false,
        float maxRandomRotationAngle = 0f,
        bool randomScale = false,
        float minScale = 1f,
        float maxScale = 1f)
    {
        ItemPrefab = itemPrefab;
        Position = position;
        RandomRotation = randomRotation;
        MaxRandomRotationAngle = Mathf.Clamp(maxRandomRotationAngle, 0f, 360f); // Clamp on creation
        RandomScale = randomScale;

        // Ensure min/max scale validity
        if (minScale > 1f) minScale = 1f;
        if (maxScale < 1f) maxScale = 1f;
        if (minScale > maxScale)
        {
            Debug.LogWarning("SpawnParameters: MinScale was greater than MaxScale. Swapping them.");
            (minScale, maxScale) = (maxScale, minScale); // Swap them
        }

        MinScale = minScale;
        MaxScale = maxScale;
    }

    // Default constructor needed for serialization if used directly
    public SpawnParameters() { }
}