using UnityEngine;
using System.Collections; // Required for Coroutines

/// <summary>
/// Fades out the SpriteRenderer on this GameObject over a random duration.
/// Assumes the sprite starts fully opaque (alpha = 1).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))] // Ensures a SpriteRenderer exists on this GameObject
public class FadeOutOverTime : MonoBehaviour
{
    [Header("Fade Configuration")]

    [SerializeField]
    [Min(0f)] // Ensure time isn't negative
    [Tooltip("The minimum time (in seconds) the fade out effect can take.")]
    private float minFadeTime = 1.0f;

    [SerializeField]
    [Min(0f)] // Ensure time isn't negative
    [Tooltip("The maximum time (in seconds) the fade out effect can take.")]
    private float maxFadeTime = 3.0f;

    [SerializeField]
    [Tooltip("Should the fade start automatically when the object is enabled?")]
    private bool startFadeOnEnable = true;

    // Optional: Define what happens after the fade is complete
    public enum FadeCompleteAction
    {
        None, // Do nothing, just leave it transparent
        DisableRenderer, // Disable the SpriteRenderer component
        DisableGameObject, // Disable the entire GameObject
        DestroyGameObject // Destroy the entire GameObject
    }

    [SerializeField]
    [Tooltip("Action to perform once the sprite is fully faded out.")]
    private FadeCompleteAction actionOnComplete = FadeCompleteAction.None;


    private SpriteRenderer spriteRenderer;
    private Coroutine activeFadeCoroutine = null;

    // --- Unity Lifecycle Methods ---

    void Awake()
    {
        // Get the SpriteRenderer component
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            // Should not happen due to [RequireComponent], but good practice
            Debug.LogError($"[{nameof(FadeOutOverTime)}] SpriteRenderer component not found on {gameObject.name}. Disabling script.", this);
            enabled = false;
        }
    }

    void OnEnable()
    {
        // When the object/component is enabled:
        // 1. Optionally reset alpha to full (in case it was previously faded and reused)
        ResetAlpha();

        // 2. Start the fade automatically if configured
        if (startFadeOnEnable)
        {
            StartFade();
        }
    }

    void OnDisable()
    {
        // If the GameObject or this component is disabled mid-fade, stop the coroutine
        // to prevent it from continuing in the background or causing errors.
        if (activeFadeCoroutine != null)
        {
            StopCoroutine(activeFadeCoroutine);
            activeFadeCoroutine = null; // Clear the reference
        }
    }

    // Optional: Editor validation for configuration values
    void OnValidate()
    {
        // Ensure minFadeTime is not greater than maxFadeTime when values are changed in the Inspector
        if (minFadeTime > maxFadeTime)
        {
            Debug.LogWarning($"[{nameof(FadeOutOverTime)}] Min Fade Time ({minFadeTime}) cannot be greater than Max Fade Time ({maxFadeTime}) on {gameObject.name}. Adjusting Min Fade Time.", this);
            minFadeTime = maxFadeTime;
        }
        if (maxFadeTime < minFadeTime) // Also handle if maxFadeTime is adjusted below minFadeTime
        {
            Debug.LogWarning($"[{nameof(FadeOutOverTime)}] Max Fade Time ({maxFadeTime}) cannot be less than Min Fade Time ({minFadeTime}) on {gameObject.name}. Adjusting Max Fade Time.", this);
            maxFadeTime = minFadeTime;
        }
    }

    // --- Public Methods ---

    /// <summary>
    /// Starts the fade out process. If a fade is already in progress, it will be stopped and restarted.
    /// </summary>
    public void StartFade()
    {
        // Stop any existing fade coroutine before starting a new one
        if (activeFadeCoroutine != null)
        {
            StopCoroutine(activeFadeCoroutine);
        }

        // Ensure sprite renderer is valid
        if (spriteRenderer == null)
        {
            Debug.LogError($"[{nameof(FadeOutOverTime)}] Cannot start fade, SpriteRenderer is missing on {gameObject.name}.", this);
            return;
        }

        // Choose the random duration for this fade instance
        float actualFadeDuration = Random.Range(minFadeTime, maxFadeTime);

        // Start the coroutine and store a reference to it
        activeFadeCoroutine = StartCoroutine(FadeCoroutine(actualFadeDuration));
    }

    /// <summary>
    /// Resets the sprite's alpha to 1 (fully opaque). Stops any active fade.
    /// </summary>
    public void ResetAlpha()
    {
        if (activeFadeCoroutine != null)
        {
            StopCoroutine(activeFadeCoroutine);
            activeFadeCoroutine = null;
        }
        if (spriteRenderer != null)
        {
            Color currentColor = spriteRenderer.color;
            currentColor.a = 1.0f;
            spriteRenderer.color = currentColor;
        }
    }

    // --- Coroutine for Fading ---

    /// <summary>
    /// The actual coroutine that performs the fade logic over time.
    /// </summary>
    /// <param name="duration">The total time the fade should take.</param>
    private IEnumerator FadeCoroutine(float duration)
    {
        // Handle invalid duration immediately
        if (duration <= 0f)
        {
            // If duration is zero or less, fade instantly
            Color endColor = spriteRenderer.color;
            endColor.a = 0f;
            spriteRenderer.color = endColor;
            activeFadeCoroutine = null; // Coroutine finished instantly
            HandleFadeCompleteAction(); // Perform post-fade action
            yield break; // Exit the coroutine immediately
        }

        float elapsedTime = 0f;
        // Get the starting color (we only modify the alpha part)
        Color baseColor = spriteRenderer.color;

        // Ensure starting alpha is 1 for the calculation, even if it wasn't initially.
        // Or, if you want to fade from current alpha: float startAlpha = spriteRenderer.color.a;
        float startAlpha = 1.0f;

        while (elapsedTime < duration)
        {
            // Increment time passed
            elapsedTime += Time.deltaTime;

            // Calculate the current alpha value, lerping from startAlpha down to 0
            // Mathf.Clamp01 ensures the value stays between 0 and 1
            float currentAlpha = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(elapsedTime / duration));

            // Apply the new alpha to the sprite's color
            spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, currentAlpha);

            // Wait until the next frame before continuing the loop
            yield return null;
        }

        // Ensure the alpha is exactly 0 at the end of the fade
        spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);

        // Coroutine finished
        activeFadeCoroutine = null;

        // Perform the specified action after fading is complete
        HandleFadeCompleteAction();
    }

    /// <summary>
    /// Performs the action specified in the actionOnComplete variable.
    /// </summary>
    private void HandleFadeCompleteAction()
    {
        // Optional: Add a small delay if needed before the action
        // yield return new WaitForSeconds(0.1f); // Example delay

        switch (actionOnComplete)
        {
            case FadeCompleteAction.DisableRenderer:
                if (spriteRenderer != null)
                {
                    spriteRenderer.enabled = false;
                }
                break;
            case FadeCompleteAction.DisableGameObject:
                gameObject.SetActive(false);
                break;
            case FadeCompleteAction.DestroyGameObject:
                Destroy(gameObject);
                break;
            case FadeCompleteAction.None:
            default:
                // Do nothing further
                break;
        }
    }
}