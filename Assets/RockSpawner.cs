using UnityEngine;
using System.Collections.Generic; // Optional: If you prefer Lists over arrays

public class RockSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The BoxCollider2D representing the sea floor.")]
    [SerializeField] private BoxCollider2D seaFloorCollider;
    [Tooltip("Optional: Parent transform to keep spawned rocks organized.")]
    [SerializeField] private Transform rockParentTransform;

    [Header("Rock Prefabs")]
    [Tooltip("Prefabs for rocks in the main environment layer.")]
    [SerializeField] private GameObject[] environmentRockPrefabs;
    [Tooltip("Prefabs for rocks in the foreground layer.")]
    [SerializeField] private GameObject[] foregroundRockPrefabs;
    [Tooltip("Prefabs for rocks in the background layer.")]
    [SerializeField] private GameObject[] backgroundRockPrefabs;

    [Header("Spawning Settings")]
    [Tooltip("Minimum horizontal distance between spawned rocks (center to center).")]
    [SerializeField] private float minDistance = 1.0f;
    [Tooltip("Maximum horizontal distance between spawned rocks (center to center).")]
    [SerializeField] private float maxDistance = 5.0f;

    [Header("Layer Depths (Z-Position)")]
    [SerializeField] private float environmentZ = 0f;
    [SerializeField] private float foregroundZ = -1f; // Closer to camera
    [SerializeField] private float backgroundZ = 1f;  // Further from camera

    void Start()
    {
        if (seaFloorCollider == null)
        {
            Debug.LogError("Sea Floor Collider is not assigned in the RockSpawner script!", this);
            return;
        }

        // Create parent if it doesn't exist and wasn't assigned
        if (rockParentTransform == null)
        {
            rockParentTransform = new GameObject("Spawned Rocks").transform;
        }

        // Spawn each layer of rocks
        SpawnRocksLayer(environmentRockPrefabs, environmentZ, "Environment");
        SpawnRocksLayer(foregroundRockPrefabs, foregroundZ, "Foreground");
        SpawnRocksLayer(backgroundRockPrefabs, backgroundZ, "Background");
    }

    void SpawnRocksLayer(GameObject[] rockPrefabs, float layerZ, string layerName)
    {
        if (rockPrefabs == null || rockPrefabs.Length == 0)
        {
            Debug.LogWarning($"No rock prefabs assigned for the {layerName} layer. Skipping spawning.", this);
            return;
        }

        Bounds floorBounds = seaFloorCollider.bounds;
        float floorTopY = floorBounds.max.y;
        float currentX = floorBounds.min.x;
        float endX = floorBounds.max.x;

        // Create a child object for this layer for better organization
        Transform layerParent = new GameObject($"{layerName}Rocks").transform;
        layerParent.SetParent(rockParentTransform);


        while (currentX < endX)
        {
            // 1. Select a random rock prefab from the array
            int randomIndex = Random.Range(0, rockPrefabs.Length);
            GameObject rockPrefabToSpawn = rockPrefabs[randomIndex];

            if (rockPrefabToSpawn == null) continue; // Skip if a prefab slot is empty

            // 2. Instantiate the rock (temporarily at origin)
            GameObject spawnedRock = Instantiate(rockPrefabToSpawn, Vector3.zero, Quaternion.identity, layerParent);

            // 3. Get the SpriteRenderer to calculate bounds
            SpriteRenderer rockRenderer = spawnedRock.GetComponentInChildren<SpriteRenderer>(); // Use GetComponentInChildren if sprite isn't on root
            if (rockRenderer == null)
            {
                Debug.LogError($"Prefab '{rockPrefabToSpawn.name}' in {layerName} layer is missing a SpriteRenderer! Destroying instance.", spawnedRock);
                Destroy(spawnedRock);
                // Decide how to proceed: skip this spawn attempt or stop? Let's skip.
                // Calculate next position naively for now to avoid infinite loop if all prefabs lack renderers
                currentX += Random.Range(minDistance, maxDistance);
                continue;
            }

            // Recalculate bounds *after* potential parenting/scaling (though default instantiate is fine here)
            Bounds rockBounds = rockRenderer.bounds;
            float rockBottomY = rockBounds.min.y;
            float rockPivotY = spawnedRock.transform.position.y; // Current pivot Y (likely 0 at this point)

            // Calculate the offset needed to align the rock's bottom with the floor's top
            // Offset = targetY - currentPivotY
            // targetY should be such that rockBottomY aligns with floorTopY
            // The distance from the pivot to the bottom is (rockPivotY - rockBottomY)
            // So, the pivot needs to be at floorTopY + (rockPivotY - rockBottomY)
            float targetPivotY = floorTopY + (rockPivotY - rockBottomY);

            // 4. Calculate the final spawn position
            Vector3 spawnPosition = new Vector3(currentX, targetPivotY, layerZ);
            spawnedRock.transform.position = spawnPosition;

            // --- Determine next spawn position ---

            // Option A: Simple spacing (center-to-center)
            // float distanceToNext = Random.Range(minDistance, maxDistance);
            // currentX += distanceToNext;

            // Option B: Spacing based on current rock's edge (ensures min/max gap *between* rocks)
            // Move currentX past the right edge of the *current* rock, then add the random gap.
            float rockWidth = rockBounds.size.x;
            float gap = Random.Range(minDistance, maxDistance);
            // We start placing from the left (currentX is the center), so advance past half the rock, then the gap.
            currentX += (rockWidth / 2f) + gap;
            // To be more robust if rocks have varying widths, it might be better to place the *next* rock relative
            // to the right edge of the *current* one.
            // currentX = spawnedRock.transform.position.x + rockBounds.extents.x + Random.Range(minDistance, maxDistance);
            // Let's stick to the simpler method first (advancing currentX directly) unless spacing looks bad.
            // Refining Option B: Place next rock's *center* relative to current rock's *center*.
            currentX = spawnedRock.transform.position.x + Random.Range(minDistance, maxDistance);


            // Make sure the *next* rock's potential *left edge* doesn't go past the end line.
            // This is a simplification; a more precise check would involve the next rock's width.
            if (currentX > endX) // Basic check to prevent spawning way off screen right.
            {
                break; // Exit loop if next position is beyond the floor end
            }
        }
    }
}