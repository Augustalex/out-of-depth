using UnityEngine;

/// <summary>
/// Applies a fluttering effect (Z rotation and/or X/Y squish) to a Transform,
/// driven by an external velocity value. Attach this to individual fins or the main body container.
/// </summary>
public class FlutterDriver : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("An overall multiplier for all frequencies.")]
    [SerializeField] private float baseFrequencyMultiplier = 1.0f;
    [Tooltip("How quickly the flutter intensity reacts to velocity changes.")]
    [SerializeField] private float intensityLerpSpeed = 5.0f;
    [Tooltip("The velocity value at which the flutter effect reaches its maximum intensity.")]
    [SerializeField] private float velocityForMaxIntensity = 5.0f;

    [Header("Rotation (Z-Axis)")]
    [Tooltip("Enable Z-axis rotation flutter.")]
    [SerializeField] private bool useRotateZ = true;
    [Tooltip("Maximum rotation angle in degrees from the initial rotation.")]
    [SerializeField] private float rotationAmount = 15.0f; // Degrees
    [Tooltip("How fast the rotation oscillates.")]
    [SerializeField] private float rotationFrequency = 5.0f;

    [Header("Squish (X-Axis)")]
    [Tooltip("Enable X-axis squish flutter.")]
    [SerializeField] private bool useSquishX = false;
    [Tooltip("Maximum change in X scale as a percentage (e.g., 0.1 for 10%) relative to initial scale.")]
    [SerializeField] private float squishXAmount = 0.1f; // Percentage
    [Tooltip("How fast the X-squish oscillates.")]
    [SerializeField] private float squishXFrequency = 6.0f;

    [Header("Squish (Y-Axis)")]
    [Tooltip("Enable Y-axis squish flutter.")]
    [SerializeField] private bool useSquishY = false;
    [Tooltip("Maximum change in Y scale as a percentage (e.g., 0.1 for 10%) relative to initial scale.")]
    [SerializeField] private float squishYAmount = 0.1f; // Percentage
    [Tooltip("How fast the Y-squish oscillates.")]
    [SerializeField] private float squishYFrequency = 6.0f;

    // --- Private State ---
    private float targetIntensity = 0f;
    private float currentIntensity = 0f;
    private Quaternion initialRotation;
    private Vector3 initialScale;
    private float timeOffset; // Used to desynchronize parts slightly

    private void Awake()
    {
        // Store the initial local rotation and scale when the object first wakes up.
        initialRotation = transform.localRotation;
        initialScale = transform.localScale;

        // Add a random offset to the time calculation for each driver.
        // This prevents all parts with the same frequency from moving identically.
        timeOffset = Random.Range(0f, Mathf.PI * 2f); // Offset in radians for sine wave
    }

    /// <summary>
    /// Sets the driving velocity for the flutter effect.
    /// The magnitude of this velocity determines the flutter intensity.
    /// </summary>
    /// <param name="velocityMagnitude">The current speed of the object.</param>
    public void SetVelocity(float velocityMagnitude)
    {
        // Calculate the target intensity based on velocity.
        // Clamp01 ensures the intensity is between 0 and 1.
        if (velocityForMaxIntensity <= 0) velocityForMaxIntensity = 1f; // Avoid division by zero
        targetIntensity = Mathf.Clamp01(Mathf.Abs(velocityMagnitude) / velocityForMaxIntensity);
    }

    private void Update()
    {
        // Smoothly interpolate the current intensity towards the target intensity.
        // This prevents jerky starts/stops in the flutter animation.
        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * intensityLerpSpeed);

        // If intensity is very low, don't bother calculating flutter.
        // We could also add code here to smoothly Lerp back to exact initial state if needed.
        if (currentIntensity < 0.01f)
        {
            // Optional: Smoothly return to initial state when stopping
            if (transform.localRotation != initialRotation || transform.localScale != initialScale)
            {
                transform.localRotation = Quaternion.Slerp(transform.localRotation, initialRotation, Time.deltaTime * intensityLerpSpeed);
                transform.localScale = Vector3.Lerp(transform.localScale, initialScale, Time.deltaTime * intensityLerpSpeed);
            }
            return;
        }

        // Get the current time, adjusted by the offset and base frequency multiplier.
        float currentTime = (Time.time + timeOffset) * baseFrequencyMultiplier;

        // --- Calculate Rotation ---
        Quaternion currentRotation = initialRotation;
        if (useRotateZ)
        {
            // Calculate the oscillation angle using Sine wave.
            float angle = Mathf.Sin(currentTime * rotationFrequency) * rotationAmount * currentIntensity;
            // Apply the rotation relative to the initial rotation.
            currentRotation = initialRotation * Quaternion.Euler(0, 0, angle);
        }

        // --- Calculate Scale ---
        Vector3 currentScale = initialScale;
        if (useSquishX)
        {
            // Calculate the scale change using Sine wave.
            // Scale factor ranges from -1 to 1, scaled by amount and intensity.
            float scaleFactorX = Mathf.Sin(currentTime * squishXFrequency) * squishXAmount * currentIntensity;
            // Apply relative to initial scale: initial + (initial * factor) = initial * (1 + factor)
            currentScale.x = initialScale.x * (1.0f + scaleFactorX);
            currentScale.x = Mathf.Max(0.01f, currentScale.x); // Prevent scale going to zero or negative
        }
        if (useSquishY)
        {
            float scaleFactorY = Mathf.Sin(currentTime * squishYFrequency) * squishYAmount * currentIntensity;
            currentScale.y = initialScale.y * (1.0f + scaleFactorY);
            currentScale.y = Mathf.Max(0.01f, currentScale.y); // Prevent scale going to zero or negative
        }

        // --- Apply Transforms ---
        transform.localRotation = currentRotation;
        transform.localScale = currentScale;
    }
}