using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PrefabSpawner : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The prefabs to choose from when spawning.")]
    [SerializeField] private List<GameObject> prefabsToSpawn; // Allow multiple prefabs

    [Tooltip("Maximum number of items allowed to be active at once.")]
    [SerializeField] private int maxSpawnedItems = 10;

    [Tooltip("Minimum time in seconds before trying to respawn an item.")]
    [SerializeField] private float minRespawnTime = 1.0f;

    [Tooltip("Maximum time in seconds before trying to respawn an item.")]
    [SerializeField] private float maxRespawnTime = 3.0f;

    [Tooltip("The CircleCollider2D defining the spawn area.")]
    [SerializeField] private CircleCollider2D spawnAreaCollider;

    [Tooltip("The tag used to find the player GameObject.")]
    [SerializeField] private string playerTag = "Player"; // Default tag

    [Tooltip("Reference to the main camera to check view boundaries.")]
    [SerializeField] private Camera mainCamera;

    [Header("Runtime Info (Read Only)")]
    [SerializeField][ReadOnly] private List<GameObject> spawnedObjects = new List<GameObject>();
    [SerializeField][ReadOnly] private float timeUntilNextSpawn = 0f;
    [SerializeField][ReadOnly] private Transform playerTransform; // Keep track of the player

    // Internal variables
    private Vector2 spawnCenter;
    private float spawnRadius;
    private int maxSpawnAttemptsPerFrame = 10; // To prevent potential infinite loops

    // --- Unity Lifecycle Methods ---

    void Start()
    {
        if (!ValidateConfiguration())
        {
            enabled = false; // Disable script if configuration is invalid
            return;
        }

        // Cache collider info
        spawnCenter = spawnAreaCollider.bounds.center; // Use bounds.center for world position
        spawnRadius = spawnAreaCollider.radius * Mathf.Max(transform.localScale.x, transform.localScale.y); // Account for spawner's scale

        // Find the player
        GameObject playerObject = GameObject.FindWithTag(playerTag);
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            Debug.LogWarning($"PrefabSpawner: Player object with tag '{playerTag}' not found. Player position checks might not work as expected if needed later.");
            // Note: Player reference isn't strictly needed for camera view check,
            // but kept it as requested.
        }

        // Initial spawn fill
        InitializeSpawns();

        // Start the respawn timer immediately if needed
        if (spawnedObjects.Count < maxSpawnedItems)
        {
            ScheduleNextSpawn();
        }
    }

    void Update()
    {
        CleanupDestroyedObjects();

        // Check if we need to spawn more items
        if (spawnedObjects.Count < maxSpawnedItems)
        {
            timeUntilNextSpawn -= Time.deltaTime;

            if (timeUntilNextSpawn <= 0f)
            {
                TrySpawnObject();
                // Schedule next attempt even if spawn failed this time,
                // but only if we are still below the max count.
                if (spawnedObjects.Count < maxSpawnedItems)
                {
                    ScheduleNextSpawn();
                }
            }
        }
    }

    // --- Core Logic Methods ---

    /// <summary>
    /// Checks if the essential configuration is valid.
    /// </summary>
    private bool ValidateConfiguration()
    {
        bool isValid = true;
        if (prefabsToSpawn == null || prefabsToSpawn.Count == 0)
        {
            Debug.LogError("PrefabSpawner: No prefabs assigned to spawn list!", this);
            isValid = false;
        }
        else
        {
            // Optional: Check if any prefab entry is null
            for (int i = 0; i < prefabsToSpawn.Count; i++)
            {
                if (prefabsToSpawn[i] == null)
                {
                    Debug.LogError($"PrefabSpawner: Prefab entry at index {i} is null!", this);
                    isValid = false;
                }
            }
        }

        if (spawnAreaCollider == null)
        {
            Debug.LogError("PrefabSpawner: Spawn Area Collider is not assigned!", this);
            isValid = false;
        }

        if (mainCamera == null)
        {
            Debug.LogWarning("PrefabSpawner: Main Camera is not assigned. Trying to find MainCamera automatically.");
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("PrefabSpawner: Main Camera reference not assigned and Camera.main is null! Cannot check view boundaries.", this);
                isValid = false;
            }
        }

        if (maxSpawnedItems <= 0)
        {
            Debug.LogWarning("PrefabSpawner: Max Spawned Items is zero or negative. No items will be spawned.", this);
            // Technically valid, but likely not intended.
        }
        if (string.IsNullOrEmpty(playerTag))
        {
            Debug.LogWarning("PrefabSpawner: Player Tag is not set. Cannot find player object.", this);
            // Not critical if player reference isn't used heavily later.
        }

        return isValid;
    }

    /// <summary>
    /// Spawns initial objects up to the maximum limit.
    /// </summary>
    private void InitializeSpawns()
    {
        int initialSpawnCount = maxSpawnedItems - spawnedObjects.Count;
        for (int i = 0; i < initialSpawnCount; i++)
        {
            // Try multiple times per object in case valid spots are rare initially
            bool spawned = false;
            for (int attempt = 0; attempt < maxSpawnAttemptsPerFrame * 2; attempt++) // More attempts for initial fill
            {
                if (TrySpawnObject())
                {
                    spawned = true;
                    break;
                }
            }
            if (!spawned)
            {
                Debug.LogWarning($"PrefabSpawner: Failed to find a valid spawn location for initial object {i + 1} after multiple attempts.", this);
                break; // Stop trying if we can't find spots
            }
        }
        Debug.Log($"PrefabSpawner: Initialized with {spawnedObjects.Count} objects.");
    }


    /// <summary>
    /// Removes references to objects that have been destroyed.
    /// </summary>
    private void CleanupDestroyedObjects()
    {
        // Iterate backwards to safely remove items from the list
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] == null) // Check if the GameObject was destroyed
            {
                spawnedObjects.RemoveAt(i);
                // No need to schedule a spawn here, Update loop handles that
            }
        }
    }

    /// <summary>
    /// Sets the timer for the next spawn attempt.
    /// </summary>
    private void ScheduleNextSpawn()
    {
        timeUntilNextSpawn = Random.Range(minRespawnTime, maxRespawnTime);
    }

    /// <summary>
    /// Attempts to find a valid spawn position and instantiate a prefab.
    /// </summary>
    /// <returns>True if an object was successfully spawned, false otherwise.</returns>
    private bool TrySpawnObject()
    {
        if (prefabsToSpawn == null || prefabsToSpawn.Count == 0) return false; // No prefabs configured

        GameObject prefabToSpawn = prefabsToSpawn[Random.Range(0, prefabsToSpawn.Count)];
        if (prefabToSpawn == null)
        {
            Debug.LogError("PrefabSpawner: Selected prefab is null!", this);
            return false;
        }

        for (int i = 0; i < maxSpawnAttemptsPerFrame; i++)
        {
            // 1. Get a random point within the circle
            Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
            Vector3 potentialPosition = spawnCenter + randomOffset;
            potentialPosition.z = 0; // Ensure Z is appropriate for 2D

            // 2. Check if the point is outside the camera view
            if (!IsPointInCameraView(potentialPosition))
            {
                // 3. Spawn the object
                GameObject newObject = Instantiate(prefabToSpawn, potentialPosition, Quaternion.identity);
                spawnedObjects.Add(newObject);
                // Debug.Log($"Spawned {newObject.name} at {potentialPosition}");
                return true; // Successfully spawned
            }
        }

        // Failed to find a suitable position after several attempts
        // Debug.LogWarning("PrefabSpawner: Could not find a valid spawn position outside camera view after multiple attempts.", this);
        return false;
    }

    /// <summary>
    /// Checks if a given world position is currently visible by the main camera.
    /// </summary>
    /// <param name="worldPoint">The world position to check.</param>
    /// <returns>True if the point is within the camera's viewport, false otherwise.</returns>
    private bool IsPointInCameraView(Vector3 worldPoint)
    {
        if (mainCamera == null) return false; // Cannot check without camera

        Vector3 viewportPoint = mainCamera.WorldToViewportPoint(worldPoint);

        // Check if the point is within the viewport rectangle (0,0) to (1,1)
        // Add a small buffer just in case floating point inaccuracies cause issues at the edges
        float buffer = 0.01f;
        return viewportPoint.z > mainCamera.nearClipPlane && // Check if it's in front of the camera
               viewportPoint.x >= 0 + buffer && viewportPoint.x <= 1 - buffer &&
               viewportPoint.y >= 0 + buffer && viewportPoint.y <= 1 - buffer;
    }

    // --- Gizmos (for visualization in Scene view) ---

    void OnDrawGizmosSelected()
    {
        // Draw the spawn area collider's circle
        if (spawnAreaCollider != null)
        {
            Gizmos.color = Color.green;
            Vector2 center = Application.isPlaying ? spawnCenter : (Vector2)spawnAreaCollider.bounds.center;
            float radius = Application.isPlaying ? spawnRadius : spawnAreaCollider.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);
            // DrawWireSphere doesn't exist for 2D, draw a circle using lines
            DrawGizmoCircle(center, radius, 32);
        }

        // Draw camera frustum (simple rectangle for orthographic)
        if (mainCamera != null && mainCamera.orthographic)
        {
            Gizmos.color = Color.red;
            float orthoHeight = mainCamera.orthographicSize;
            float orthoWidth = mainCamera.aspect * orthoHeight;
            Vector3 camPos = mainCamera.transform.position;

            Vector3 topLeft = camPos + mainCamera.transform.up * orthoHeight - mainCamera.transform.right * orthoWidth;
            Vector3 topRight = camPos + mainCamera.transform.up * orthoHeight + mainCamera.transform.right * orthoWidth;
            Vector3 bottomLeft = camPos - mainCamera.transform.up * orthoHeight - mainCamera.transform.right * orthoWidth;
            Vector3 bottomRight = camPos - mainCamera.transform.up * orthoHeight + mainCamera.transform.right * orthoWidth;

            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);
        }
        else if (mainCamera != null && !mainCamera.orthographic)
        {
            // Basic frustum drawing for perspective (less accurate representation for the 2D check)
            Gizmos.color = Color.yellow;
            Gizmos.matrix = Matrix4x4.TRS(mainCamera.transform.position, mainCamera.transform.rotation, Vector3.one);
            Gizmos.DrawFrustum(Vector3.zero, mainCamera.fieldOfView, mainCamera.farClipPlane, mainCamera.nearClipPlane, mainCamera.aspect);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }

    // Helper to draw circle gizmo
    private void DrawGizmoCircle(Vector2 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector2(Mathf.Cos(0) * radius, Mathf.Sin(0) * radius);
        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.Deg2Rad * (i * angleStep);
            Vector3 currentPoint = center + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }
    }
}


// Optional Helper Attribute to make fields read-only in the inspector
public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
{
    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false; // Disable editing
        UnityEditor.EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true; // Re-enable GUI for other fields
    }
    public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
    {
        return UnityEditor.EditorGUI.GetPropertyHeight(property, label, true);
    }
}
#endif // UNITY_EDITOR