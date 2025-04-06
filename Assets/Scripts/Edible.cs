using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Component to mark objects that can be eaten by the player
/// </summary>
public class Edible : MonoBehaviour
{
    [Tooltip("Points awarded when this object is eaten")]
    public int pointValue = 1;

    [Tooltip("Event triggered when this object is eaten")]
    public UnityEvent onEaten;

    /// <summary>
    /// Called when the player eats this object
    /// </summary>
    public void GetEaten()
    {
        Debug.Log($"[{nameof(Edible)}] {gameObject.name} has been eaten!");
        // Trigger the onEaten event
        onEaten?.Invoke();

        // You could add score here or through the event

        // Destroy the object
        Destroy(gameObject);
    }
}
