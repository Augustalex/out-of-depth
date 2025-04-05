using UnityEngine;

/// <summary>
/// Connects two Transforms with this GameObject's sprite,
/// stretching it along its local X-axis only and rotating it appropriately.
/// Assumes the sprite's "length" is along its local X-axis and its pivot is centered.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))] // Ensure there's a sprite to work with
public class LineConnector : MonoBehaviour
{
    [Tooltip("The starting point of the line (e.g., the angler fish bulb).")]
    public Transform startPoint;

    [Tooltip("The ending point of the line (e.g., the angler fish forehead).")]
    public Transform endPoint;

    // We store the initial local scale to maintain thickness
    private float initialYScale;
    private float initialZScale;

    void Awake()
    {
        // Store the initial Y and Z scale set in the Inspector.
        // This defines the line's thickness and 2D appearance.
        initialYScale = transform.localScale.y;
        initialZScale = transform.localScale.z; // Usually 1 for 2D sprites

        // Optional: Add a check for zero scale which might be unintentional
        if (Mathf.Approximately(initialYScale, 0f))
        {
            Debug.LogWarning("LineConnector: Initial Y scale is zero or very close to it. Line may be invisible. Adjust Y scale in Inspector.", this);
            // You could force a default minimum thickness here if desired:
            // initialYScale = 0.1f;
            // transform.localScale = new Vector3(transform.localScale.x, initialYScale, initialZScale);
        }
    }

    void Update()
    {
        // Ensure both points are assigned before proceeding
        if (startPoint == null || endPoint == null)
        {
            // Optionally disable the renderer if points are missing
            // GetComponent<SpriteRenderer>().enabled = false;
            Debug.LogWarning("Line Connector: Start or End point not assigned.", this);
            return; // Exit Update if points are not set
        }
        // Ensure renderer is enabled if points are valid (if you disabled it above)
        // GetComponent<SpriteRenderer>().enabled = true;


        // --- Calculations ---

        // 1. Calculate the vector from start to end
        Vector3 direction = endPoint.position - startPoint.position;

        // 2. Calculate the distance (magnitude of the direction vector)
        float distance = direction.magnitude;

        // 3. Calculate the midpoint for positioning the line object
        Vector3 midpoint = startPoint.position + (direction / 2.0f);
        // Alternative midpoint calculation: (startPoint.position + endPoint.position) / 2.0f;


        // --- Apply Transformations ---

        // 1. Set Position: Place the line's center at the calculated midpoint.
        // Since it's 2D, we usually keep the original Z position.
        transform.position = new Vector3(midpoint.x, midpoint.y, transform.position.z);

        // 2. Set Rotation: Rotate the line to point along the direction vector.
        // Mathf.Atan2 gives the angle in radians relative to the positive X-axis.
        // Convert to degrees for Quaternion.Euler.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle); // Rotate around the Z-axis for 2D

        // 3. Set Scale: Stretch the line along its local X-axis to match the distance.
        // Keep the initial Y and Z scale to maintain thickness.
        transform.localScale = new Vector3(distance, initialYScale, initialZScale);
    }
}