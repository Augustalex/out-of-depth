using UnityEngine;
using System.Collections; // Required for IEnumerator if needed later, but not currently

public class ClusteredObjectSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The BoxCollider2D representing the sea floor.")]
    [SerializeField] private BoxCollider2D seaFloorCollider;
    [Tooltip("Optional: Parent transform to keep spawned objects organized.")]
    [SerializeField] private Transform spawnParent;

    [Header("Object Prefabs")]
    [Tooltip("Prefabs for the interactable objects to spawn.")]
    [SerializeField] private GameObject[] interactablePrefabs;

    [Header("Cluster Settings")]
    [Tooltip("Minimum number of items in a single cluster.")]
    [SerializeField] private int minClusterSize = 2;
    [Tooltip("Maximum number of items in a single cluster.")]
    [SerializeField] private int maxClusterSize = 5;

    [Header("Spacing Settings")]
    [Tooltip("Minimum horizontal distance between items WITHIN a cluster.")]
    [SerializeField] private float minClusterGap = 0.5f;
    [Tooltip("Maximum horizontal distance between items WITHIN a cluster.")]
    [SerializeField] private float maxClusterGap = 1.5f;
    [Tooltip("Minimum horizontal distance/margin BETWEEN separate clusters.")]
    [SerializeField] private float minMargin = 3.0f;
    [Tooltip("Maximum horizontal distance/margin BETWEEN separate clusters.")]
    [SerializeField] private float maxMargin = 6.0f;

    [Header("Layer Depth")]
    [Tooltip("The Z position where these interactable objects will be spawned.")]
    [SerializeField] private float spawnZ = 0f;

    void Start()
    {
        // --- Input Validation ---
        if (seaFloorCollider == null)
        {
            Debug.LogError("ClusteredObjectSpawner: Sea Floor Collider is not assigned!", this);
            return; // Stop execution if no floor is defined
        }
        if (interactablePrefabs == null || interactablePrefabs.Length == 0)
        {
            Debug.LogWarning("ClusteredObjectSpawner: No interactable prefabs assigned. Nothing will be spawned.", this);
            return; // Stop if there are no prefabs to spawn
        }

        // Simple validation for ranges
        if (maxClusterSize < minClusterSize) maxClusterSize = minClusterSize;
        if (maxClusterGap < minClusterGap) maxClusterGap = minClusterGap;
        if (maxMargin < minMargin) maxMargin = minMargin;


        // --- Setup ---
        // Create a parent transform if one isn't assigned, for organization
        if (spawnParent == null)
        {
            spawnParent = new GameObject("Spawned Interactables Cluster").transform;
            // Optional: Make it a child of this spawner object
            // spawnParent.SetParent(this.transform);
        }

        Bounds floorBounds = seaFloorCollider.bounds;
        float floorTopY = floorBounds.max.y; // The Y position to align origins to
        float startX = floorBounds.min.x;
        float endX = floorBounds.max.x;
        float currentX = startX;

        // --- Spawning Loop ---
        while (currentX < endX)
        {
            // 1. Determine the size of the next cluster
            int itemsInThisCluster = Random.Range(minClusterSize, maxClusterSize + 1); // Max is exclusive for int Random.Range

            // 2. Spawn the items for this cluster
            for (int i = 0; i < itemsInThisCluster; i++)
            {
                // Check if the current position is already beyond the floor's end
                if (currentX >= endX)
                {
                    break; // Stop spawning items for this cluster if we're off the edge
                }

                // Select a random prefab from the list
                int prefabIndex = Random.Range(0, interactablePrefabs.Length);
                GameObject prefabToSpawn = interactablePrefabs[prefabIndex];

                if (prefabToSpawn == null)
                {
                    Debug.LogWarning($"Interactable prefab at index {prefabIndex} is null. Skipping this item.", this);
                    // Decide how to advance X here. Maybe add minimum gap?
                    if (i < itemsInThisCluster - 1) // Add gap if not the last intended item
                    {
                        currentX += minClusterGap; // Advance minimally to avoid getting stuck
                    }
                    continue; // Skip to the next item in the cluster
                }

                // Calculate the spawn position: Origin aligned with floorTopY
                Vector3 spawnPosition = new Vector3(currentX, floorTopY, spawnZ);

                // Instantiate the object
                Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity, spawnParent);

                // 3. Advance the X position for the next item WITHIN the cluster
                // Don't add a gap after the very last item of the cluster
                if (i < itemsInThisCluster - 1)
                {
                    currentX += Random.Range(minClusterGap, maxClusterGap);
                }
            } // End of loop for items within a cluster

            // 4. Advance the X position by the margin BETWEEN clusters
            // This happens *after* a cluster is finished, preparing for the next one
            // We need to ensure we add the margin even if the loop above exited early due to reaching endX
            // The 'currentX' here is potentially the position of the last spawned item, or where the next item *would* have been placed.
            currentX += Random.Range(minMargin, maxMargin);

        } // End of while loop (spawning clusters along the floor)
    }
}