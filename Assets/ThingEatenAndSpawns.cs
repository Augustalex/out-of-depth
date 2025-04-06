using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Edible))]
public class ThingEatenAndSpawns : MonoBehaviour
{
    [Header("Blood Effects")]
    [SerializeField]
    [Tooltip("List of blood effect prefabs to spawn randomly when eaten")]
    private List<GameObject> toSpawn = new List<GameObject>();

    [SerializeField]
    [Tooltip("Should the blood effect use random rotation?")]
    private bool useRandomRotation = true;

    [SerializeField]
    [Range(0f, 360f)]
    [Tooltip("Maximum random rotation angle for blood effects")]
    private float maxRandomRotationAngle = 360f;

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
        foreach (var template in toSpawn)
        {
            if (template != null)
            {
                SpawnParameters spawnParams = new SpawnParameters(
                    template,
                    transform.position,
                    useRandomRotation,
                    maxRandomRotationAngle
                );

                // Spawn the blood effect using the global spawner
                GlobalItemSpawner.Spawn(spawnParams);
            }
        }

        Destroy(gameObject);
    }
}
