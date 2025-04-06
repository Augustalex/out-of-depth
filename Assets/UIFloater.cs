using UnityEngine;

// Automatically adds a RectTransform component if one doesn't exist,
// though UI elements like Image already have it.
[RequireComponent(typeof(RectTransform))]
public class UIFloater : MonoBehaviour
{
    [Header("Float Settings")]
    [Tooltip("The maximum distance (in UI units/pixels) the element moves up and down from its starting point.")]
    public float amplitude = 10.0f; // Adjust how high/low it floats

    [Tooltip("The speed of the floating oscillation. Higher values mean faster movement.")]
    public float frequency = 0.5f; // Adjust how fast it floats

    private RectTransform rectTransform;
    private Vector2 startAnchoredPosition;

    // Awake is called when the script instance is being loaded.
    void Awake()
    {
        // Get the RectTransform component attached to this UI element.
        rectTransform = GetComponent<RectTransform>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // Store the initial position based on the anchors.
        // This ensures the floating is relative to where you placed it in the editor,
        // respecting its anchor settings.
        startAnchoredPosition = rectTransform.anchoredPosition;
    }

    // Update is called once per frame
    void Update()
    {
        // Calculate the vertical offset using a Sine wave.
        // Mathf.Sin returns values between -1 and 1.
        // Time.time provides the absolute time since the game started, ensuring synchronization.
        // Multiplying by frequency controls the speed of the oscillation.
        float verticalOffset = Mathf.Sin(Time.time * frequency) * amplitude;

        // Create the new position: Keep the original X position, but modify the Y position
        // by adding the calculated vertical offset to the starting Y position.
        Vector2 newPosition = new Vector2(startAnchoredPosition.x, startAnchoredPosition.y + verticalOffset);

        // Apply the calculated position back to the UI element's anchoredPosition.
        rectTransform.anchoredPosition = newPosition;
    }
}