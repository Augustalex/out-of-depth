using UnityEngine;

public class FishData : MonoBehaviour
{
    [Header("Fish Properties")]
    [Tooltip("The initial scale of the fish")]
    [SerializeField] private Vector3 initialScale = Vector3.one;

    // Property to access the initial scale
    public Vector3 InitialScale => initialScale;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Set the initial scale of the fish
        transform.localScale = initialScale;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
