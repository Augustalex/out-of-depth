using UnityEngine;
using System.Collections;

public class FishSquisher : MonoBehaviour
{
    // Enum to easily identify the type of action triggering the squish
    public enum SquishActionType
    {
        Dash,
        Attack,
        Eat
    }

    // Struct to hold the unique animation parameters for each action type
    [System.Serializable] // Makes this visible and editable in the Unity Inspector
    public struct SquishSettings
    {
        [Tooltip("Total duration of the squish and return animation.")]
        public float duration;

        [Tooltip("How much to scale the fish on the X-axis at the peak of the squish (e.g., 1.2 for wider, 0.8 for thinner).")]
        public float targetScaleX;

        [Tooltip("How much to scale the fish on the Y-axis at the peak of the squish (e.g., 0.8 for shorter, 1.2 for taller).")]
        public float targetScaleY;

        [Tooltip("Optional: Use an AnimationCurve for custom easing. Time is 0 to 1 (representing start to end of duration), Value is 0 (no effect) to 1 (full squish effect). Curve should ideally start at 0, go to 1 (peak squish), and return to 0.")]
        public AnimationCurve scaleCurve; // Make sure this curve spans 0 to 1 in time and value typically goes 0 -> 1 -> 0
    }

    // -- Public Settings (Editable in Inspector) --
    [Header("Squish Settings per Action")]
    public SquishSettings dashSettings = new SquishSettings { duration = 0.3f, targetScaleX = 1.3f, targetScaleY = 0.7f };
    public SquishSettings attackSettings = new SquishSettings { duration = 0.2f, targetScaleX = 0.8f, targetScaleY = 1.2f };
    public SquishSettings eatSettings = new SquishSettings { duration = 0.5f, targetScaleX = 1.1f, targetScaleY = 0.9f };

    // -- Private Variables --
    private Vector3 originalScale;
    private Coroutine currentSquishCoroutine = null;
    private Transform objectTransform; // Cache the transform for performance

    void Awake()
    {
        // Get the Transform component we will be manipulating
        objectTransform = transform;
        // Store the initial scale when the game starts or object wakes up
        originalScale = objectTransform.localScale;
    }

    /// <summary>
    /// Public method to be called from other scripts (like PlayerController) to start the squish effect.
    /// </summary>
    /// <param name="actionType">Which action's settings to use (Dash, Attack, Eat).</param>
    public void TriggerSquish(SquishActionType actionType)
    {
        // Select the correct settings based on the action type
        SquishSettings settingsToUse;
        switch (actionType)
        {
            case SquishActionType.Dash:
                settingsToUse = dashSettings;
                break;
            case SquishActionType.Attack:
                settingsToUse = attackSettings;
                break;
            case SquishActionType.Eat:
                settingsToUse = eatSettings;
                break;
            default:
                Debug.LogError($"Squish settings for action type {actionType} not found!");
                return; // Exit if no settings match
        }

        // If a squish animation is already running, stop it first
        if (currentSquishCoroutine != null)
        {
            StopCoroutine(currentSquishCoroutine);
            // Ensure the scale is reset if interrupted mid-animation
            objectTransform.localScale = originalScale;
        }

        // Start the new squish animation coroutine
        currentSquishCoroutine = StartCoroutine(AnimateSquish(settingsToUse));
    }

    /// <summary>
    /// Coroutine that handles the squish and return-to-normal animation over time.
    /// </summary>
    /// <param name="settings">The specific settings (duration, scales) for this animation.</param>
    private IEnumerator AnimateSquish(SquishSettings settings)
    {
        float elapsedTime = 0f;
        Vector3 startScale = originalScale;
        // Calculate the target scale based on multipliers relative to the original scale
        Vector3 targetPeakScale = new Vector3(originalScale.x * settings.targetScaleX,
                                            originalScale.y * settings.targetScaleY,
                                            originalScale.z); // Keep Z scale original unless you need 3D squish

        // Ensure duration is positive to avoid issues
        if (settings.duration <= 0)
        {
            // If duration is zero or negative, just snap to original and exit
            objectTransform.localScale = originalScale;
            currentSquishCoroutine = null; // Mark as finished
            yield break; // Stop the coroutine
        }


        // Check if a custom animation curve is provided and valid
        bool useCurve = settings.scaleCurve != null && settings.scaleCurve.keys.Length >= 2;

        while (elapsedTime < settings.duration)
        {
            // Calculate the normalized time (progress from 0 to 1)
            float normalizedTime = elapsedTime / settings.duration;

            // Determine the interpolation factor (how much to apply the squish)
            float interpolationFactor;
            if (useCurve)
            {
                // Use the animation curve to control the blend factor
                interpolationFactor = settings.scaleCurve.Evaluate(normalizedTime);
            }
            else
            {
                // Default: Simple Sine wave ease-in/ease-out (0 -> 1 -> 0)
                // Peak is at half duration (normalizedTime = 0.5)
                interpolationFactor = Mathf.Sin(normalizedTime * Mathf.PI);
            }

            // Lerp (Linear Interpolate) between the original scale and the target peak scale
            // using the calculated interpolation factor.
            // LerpUnclamped allows going beyond the target if curve/factor > 1 (for bounce effects)
            objectTransform.localScale = Vector3.LerpUnclamped(startScale, targetPeakScale, interpolationFactor);

            // Increment elapsed time and wait for the next frame
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Animation finished, ensure the scale is exactly back to the original
        objectTransform.localScale = originalScale;
        currentSquishCoroutine = null; // Mark the coroutine as no longer running
    }

    // Optional: Add a method to reset scale immediately if needed elsewhere
    public void ResetScale()
    {
        if (currentSquishCoroutine != null)
        {
            StopCoroutine(currentSquishCoroutine);
            currentSquishCoroutine = null;
        }
        objectTransform.localScale = originalScale;
    }

    // Optional: Reset scale if the object is disabled
    void OnDisable()
    {
        ResetScale();
    }
}