using UnityEngine;

[RequireComponent(typeof(Edible))]
public class ThingEaten : MonoBehaviour
{
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
        Destroy(gameObject);
    }
}
