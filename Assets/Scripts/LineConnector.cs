using UnityEngine;

/// <summary>
/// Connects two Transforms with this GameObject's sprite, positioning and scaling
/// it so its visual edges align with the start/end points.
/// Stretches along its local X-axis only and rotates appropriately.
/// Assumes the sprite's "length" is along its local X-axis and its pivot is centered.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class LineConnector : MonoBehaviour
{
    [Tooltip("The starting point of the line (e.g., the angler fish bulb).")]
    public Transform startPoint;

    [Tooltip("The ending point of the line (e.g., the angler fish forehead).")]
    public Transform endPoint;

    private float initialYScale;
    private float initialZScale;
    private float spriteBaseWidth = 1f; // Default to 1, will be calculated

    void Awake()
    {
        initialYScale = transform.localScale.y;
        initialZScale = transform.localScale.z;

        // --- Calculate the sprite's original width in world units ---
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr.sprite != null)
        {
            // The sprite's bounds.size gives its dimensions in world units
            // when the GameObject's scale is (1, 1, 1).
            spriteBaseWidth = sr.sprite.bounds.size.x;

            // Add a check for zero width, which would break scaling
            if (Mathf.Approximately(spriteBaseWidth, 0f))
            {
                Debug.LogError("LineConnector: Sprite base width (sprite.bounds.size.x) is zero! Cannot scale correctly. Check sprite import settings (especially Pixels Per Unit). Defaulting to 1.", this);
                spriteBaseWidth = 1f; // Prevent division by zero in Update
            }
        }
        else
        {
            Debug.LogError("LineConnector: No sprite assigned to the SpriteRenderer component!", this);
            // Keep spriteBaseWidth = 1f as a fallback
        }
        // ------------------------------------------------------------


        if (Mathf.Approximately(initialYScale, 0f))
        {
            Debug.LogWarning("LineConnector: Initial Y scale is zero. Line may be invisible. Adjust Y scale in Inspector.", this);
            // Optional: force a minimum thickness
            // initialYScale = 0.1f;
        }
    }

    void Update()
    {
        if (startPoint == null || endPoint == null)
        {
            // Optionally hide if points aren't set
            // GetComponent<SpriteRenderer>().enabled = false;
            return;
        }
        // GetComponent<SpriteRenderer>().enabled = true;

        // --- Calculations ---
        Vector3 direction = endPoint.position - startPoint.position;
        float distance = direction.magnitude;
        Vector3 midpoint = startPoint.position + (direction / 2.0f);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // --- Apply Transformations ---

        // 1. Position: Keep the center pivot at the midpoint.
        transform.position = new Vector3(midpoint.x, midpoint.y, transform.position.z);

        // 2. Rotation: Point along the direction.
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // 3. Scale: Calculate the required X scale.
        // We need the final visual width to be 'distance'.
        // The scale factor required is distance / (original width when scale was 1).
        float requiredScaleX = (spriteBaseWidth > 0) ? (distance / spriteBaseWidth) : 0f; // Avoid division by zero

        transform.localScale = new Vector3(requiredScaleX, initialYScale, initialZScale);
    }
}