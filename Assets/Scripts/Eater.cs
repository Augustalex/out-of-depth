using UnityEngine;

/// <summary>
/// Component that handles eating edible objects
/// </summary>
public class Eater : MonoBehaviour
{
    // Define delegate for the eaten event
    public delegate void ObjectEatenEventHandler(GameObject eatenObject, Edible edibleComponent);

    /// <summary>
    /// Event that fires when an object is eaten
    /// </summary>
    public event ObjectEatenEventHandler OnObjectEaten;

    [Header("Mouth Detection")]
    [Tooltip("Reference to a collider that represents the mouth area")]
    [SerializeField]
    public CircleCollider2D mouthCollider;

    private void Awake()
    {
        if (mouthCollider == null)
        {
            Debug.LogWarning("Eater: Mouth collider reference not set. Eating functionality will not work.", this);
        }
    }

    /// <summary>
    /// Checks for edible objects in the mouth area and consumes them
    /// </summary>
    public void CheckForEdibleObjects()
    {
        if (mouthCollider == null) return;

        // Get the world position of the mouth collider
        Vector2 mouthPosition = (Vector2)mouthCollider.transform.position + mouthCollider.offset;

        // Perform circle overlap check for edible objects (2D physics)
        Collider2D[] colliders = Physics2D.OverlapCircleAll(mouthPosition, mouthCollider.radius);

        foreach (Collider2D collider in colliders)
        {
            Debug.Log($"Checking collider: {collider.gameObject.name}");
            // Check if the object has an Edible component
            Edible edible = collider.GetComponent<Edible>();
            if (edible != null)
            {
                // Trigger the event before the object is eaten
                OnObjectEaten?.Invoke(collider.gameObject, edible);

                edible.GetEaten();
            }
        }
    }
}
