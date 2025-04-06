using UnityEngine;
using System.Collections.Generic; // Required for Queue

/// <summary>
/// Manages spawning items globally with configuration options and item limits.
/// Uses the Singleton pattern for easy static access.
/// </summary>
public class GlobalItemSpawner : MonoBehaviour
{
    // --- Singleton Instance ---
    public static GlobalItemSpawner Instance { get; private set; }

    // --- Configuration (Editable in Inspector) ---
    [Header("Spawning Limits")]
    [SerializeField]
    [Min(1)] // Ensure max items is at least 1
    [Tooltip("The maximum number of items managed by this spawner allowed to exist simultaneously.")]
    private int maxConcurrentItems = 100;

    // --- Internal State ---
    // Queue to keep track of spawned items in the order they were created (FIFO)
    private Queue<GameObject> spawnedItems = new Queue<GameObject>();

    // --- Unity Lifecycle Methods ---
    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[{nameof(GlobalItemSpawner)}] Another instance found. Destroying this duplicate.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optional: Uncomment below if the spawner needs to persist across scene loads
            // DontDestroyOnLoad(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Clear static instance when the object is destroyed
        if (Instance == this)
        {
            Instance = null;
        }
        // Optional: Clean up any remaining managed items if needed,
        // though scene destruction usually handles this.
        // ClearManagedItems();
    }

    // --- Static Spawning Method (The Public API) ---

    /// <summary>
    /// Spawns an item based on the provided parameters.
    /// Enforces the maxConcurrentItems limit by destroying the oldest item if necessary.
    /// </summary>
    /// <param name="parameters">The DTO containing all spawning details.</param>
    /// <returns>The newly spawned GameObject, or null if spawning failed (e.g., null prefab).</returns>
    public static GameObject Spawn(SpawnParameters parameters)
    {
        if (Instance == null)
        {
            Debug.LogError($"[{nameof(GlobalItemSpawner)}] Attempted to spawn but no instance exists in the scene!");
            return null;
        }
        if (parameters.ItemPrefab == null)
        {
            Debug.LogError($"[{nameof(GlobalItemSpawner)}] Attempted to spawn but ItemPrefab in parameters is null!");
            return null;
        }

        // Delegate the actual spawning work to the instance method
        return Instance.SpawnInternal(parameters);
    }


    // --- Internal Spawning Logic ---

    private GameObject SpawnInternal(SpawnParameters parameters)
    {
        // 1. Enforce Item Limit - Cull oldest if necessary
        CullOldestItemIfLimitReached();

        // 2. Instantiate the Prefab
        // Start with the prefab's default rotation (Quaternion.identity overrides it)
        GameObject newItem = Instantiate(parameters.ItemPrefab, parameters.Position, parameters.ItemPrefab.transform.rotation);

        // 3. Apply Random Rotation (if requested)
        if (parameters.RandomRotation && parameters.MaxRandomRotationAngle > 0)
        {
            float randomZRotation = Random.Range(-parameters.MaxRandomRotationAngle, parameters.MaxRandomRotationAngle);
            // Additive rotation around the Z axis (common for 2D)
            newItem.transform.Rotate(0f, 0f, randomZRotation, Space.Self);
        }

        // 4. Apply Random Scale (if requested)
        if (parameters.RandomScale && parameters.MaxScale > parameters.MinScale)
        {
            float randomScaleMultiplier = Random.Range(parameters.MinScale, parameters.MaxScale);
            // Apply uniform scaling based on the multiplier
            Vector3 originalScale = parameters.ItemPrefab.transform.localScale; // Use prefab scale as base
            newItem.transform.localScale = originalScale * randomScaleMultiplier;
            // Ensure Z scale remains appropriate for 2D if necessary (often 1 or prefab's Z)
            newItem.transform.localScale = new Vector3(newItem.transform.localScale.x, newItem.transform.localScale.y, originalScale.z);
        }

        // 5. Track the New Item
        spawnedItems.Enqueue(newItem);

        // 6. Return the spawned item
        return newItem;
    }

    // --- Helper Methods ---

    private void CullOldestItemIfLimitReached()
    {
        // Use >= because we're about to add one more
        while (spawnedItems.Count >= maxConcurrentItems)
        {
            GameObject oldestItem = spawnedItems.Dequeue(); // Remove the oldest from the queue

            // Important: Check if the item hasn't already been destroyed by other game logic
            if (oldestItem != null)
            {
                Destroy(oldestItem);
                // Debug.Log($"[{nameof(GlobalItemSpawner)}] Max item limit ({maxConcurrentItems}) reached. Destroyed oldest item: {oldestItem.name}");
            }
            else
            {
                // Item was likely destroyed elsewhere, just remove the null entry.
                // Log is optional, can be noisy.
                // Debug.LogWarning($"[{nameof(GlobalItemSpawner)}] Oldest item in queue was already destroyed.");
            }
        }
    }

    /// <summary>
    /// Optional: Manually clears all items managed by this spawner.
    /// </summary>
    public void ClearManagedItems()
    {
        while (spawnedItems.Count > 0)
        {
            GameObject item = spawnedItems.Dequeue();
            if (item != null)
            {
                Destroy(item);
            }
        }
        Debug.Log($"[{nameof(GlobalItemSpawner)}] Cleared all managed items.");
    }
}