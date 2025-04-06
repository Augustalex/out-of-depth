using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Edible))]
[RequireComponent(typeof(FishData))]
public class FishEaten : MonoBehaviour
{
    [Header("Blood Effects")]
    [SerializeField]
    [Tooltip("List of blood effect prefabs to spawn randomly when eaten")]
    private List<GameObject> bloodTemplates = new List<GameObject>();

    [SerializeField]
    [Tooltip("Should the blood effect use random rotation?")]
    private bool useRandomRotation = true;

    [SerializeField]
    [Range(0f, 360f)]
    [Tooltip("Maximum random rotation angle for blood effects")]
    private float maxRandomRotationAngle = 360f;

    [SerializeField]
    [Tooltip("Should the blood effect use random scaling?")]
    private bool useRandomScale = true;

    [SerializeField]
    [Range(0.5f, 1f)]
    [Tooltip("Minimum scale multiplier for blood effects")]
    private float minScale = 0.8f;

    [SerializeField]
    [Range(1f, 2f)]
    [Tooltip("Maximum scale multiplier for blood effects")]
    private float maxScale = 1.2f;

    [Header("Fish Bones")]
    [SerializeField]
    [Tooltip("Fish bone prefab to spawn when the fish is eaten")]
    private GameObject fishBoneTemplate;

    private FishData fishData;

    void Awake()
    {
        fishData = GetComponent<FishData>();
    }

    void OnEnable()
    {
        var edible = GetComponent<Edible>();
        edible.onEaten.AddListener(OnEaten);
    }

    void OnDisable()
    {
        var edible = GetComponent<Edible>();
        edible.onEaten.RemoveListener(OnEaten);
    }

    void OnEaten()
    {
        Debug.Log($"[{nameof(FishEaten)}] {gameObject.name} has been eaten!");
        // Spawn blood effect if we have templates available
        if (bloodTemplates != null && bloodTemplates.Count > 0)
        {
            var randomCount = Random.Range(3, 12); // Randomly decide how many blood effects to spawn
            for (int i = 0; i < randomCount; i++)
            {
                SpawnBlood();
            }
        }

        // Spawn fish bone if template is available
        if (fishBoneTemplate != null)
        {
            SpawnFishBone();
        }

        // Destroy the fish
        Destroy(gameObject);
    }

    void SpawnBlood()
    {
        // Select a random blood template
        GameObject selectedTemplate = bloodTemplates[Random.Range(0, bloodTemplates.Count)];

        if (selectedTemplate != null)
        {
            // Create spawn parameters for the blood effect
            SpawnParameters spawnParams = new SpawnParameters(
                selectedTemplate,
                transform.position,
                useRandomRotation,
                maxRandomRotationAngle,
                useRandomScale,
                minScale,
                maxScale
            );

            // Spawn the blood effect using the global spawner
            GlobalItemSpawner.Spawn(spawnParams);
        }
    }

    void SpawnFishBone()
    {
        // Create spawn parameters for the fish bone
        SpawnParameters spawnParams = new SpawnParameters(
            fishBoneTemplate,
            transform.position,
            useRandomRotation,
            maxRandomRotationAngle,
            useRandomScale,
            minScale,
            maxScale
        );

        // Apply fish's initial scale if available
        if (fishData != null)
        {
            // If we have a FishData component, override the random scale and use the fish's original scale
            GameObject bone = GlobalItemSpawner.Spawn(spawnParams);
            if (bone != null)
            {
                // Apply the fish's initial scale to the bone
                bone.transform.localScale = fishData.InitialScale;
            }
        }
        else
        {
            // Spawn the fish bone using the global spawner with default parameters
            GlobalItemSpawner.Spawn(spawnParams);
        }
    }
}
