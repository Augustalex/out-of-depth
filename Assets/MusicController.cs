using UnityEngine;
using System.Collections;

// Ensures there are at least two AudioSource components on this GameObject
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(AudioSource))]
public class MusicController : MonoBehaviour
{
    // --- Enums ---

    public enum GameMode
    {
        Intro,
        Normal,
        Danger
    }

    // --- Public Fields (assignable in Inspector) ---

    [Header("Music Clips")]
    public AudioClip introMusic;
    public AudioClip normalMusic;
    public AudioClip dangerMusic;

    [Header("Core Settings")]
    [Range(0.1f, 10.0f)]
    public float crossfadeDuration = 2.0f; // Duration of the crossfade in seconds
    public GameMode startMode = GameMode.Intro; // Which mode to start in

    [Header("Proximity Detection")]
    public Transform playerTransform; // Assign the player's Transform here
    public LayerMask enemyLayerMask;  // Set this to the layer(s) your enemies are on
    public float detectionRange = 15.0f; // Max distance to check for enemies
    [Tooltip("How often (in seconds) to check for nearby enemies. Lower values are more responsive but less performant.")]
    public float enemyCheckInterval = 0.25f; // Check frequency

    // --- Private Fields ---

    private AudioSource audioSource1;
    private AudioSource audioSource2;
    private AudioSource activeAudioSource; // The source currently playing or fading in
    private GameMode currentMode;
    private Coroutine fadeCoroutine;
    private float enemyCheckTimer = 0f;
    private bool isEnemyNearby = false; // Store the last known state

    // --- Unity Methods ---

    void Awake()
    {
        // Get the AudioSource components
        AudioSource[] sources = GetComponents<AudioSource>();
        audioSource1 = sources[0];
        audioSource2 = sources[1];

        // Configure AudioSources
        ConfigureAudioSource(audioSource1);
        ConfigureAudioSource(audioSource2);

        // Ensure only one source is initially active
        audioSource1.volume = 0f;
        audioSource2.volume = 0f;

        // Initial check for player assignment
        if (playerTransform == null)
        {
            Debug.LogWarning("MusicController: Player Transform is not assigned in the Inspector. Proximity detection will not work.", this);
        }
    }

    void Start()
    {
        // Start with the initial mode
        currentMode = startMode;
        AudioClip startingClip = GetClipForMode(currentMode);

        if (startingClip != null)
        {
            activeAudioSource = audioSource1; // Start with source 1
            activeAudioSource.clip = startingClip;
            activeAudioSource.volume = 1f; // Start at full volume
            activeAudioSource.Play();
        }
        else
        {
            Debug.LogWarning($"MusicController: No audio clip assigned for the starting mode '{startMode}'.", this);
        }

        // Perform an initial enemy check if not starting in Intro mode
        if (currentMode != GameMode.Intro && playerTransform != null)
        {
            CheckProximityAndSwitch();
        }
    }

    void Update()
    {
        // --- Proximity Check Logic ---
        // Only run proximity checks if NOT in Intro mode and player is assigned
        if (currentMode != GameMode.Intro && playerTransform != null)
        {
            enemyCheckTimer += Time.deltaTime;
            if (enemyCheckTimer >= enemyCheckInterval)
            {
                enemyCheckTimer = 0f; // Reset timer
                CheckProximityAndSwitch();
            }
        }
    }

    // --- Public Methods ---

    /// <summary>
    /// Call this method to explicitly exit the Intro mode and transition to Normal mode.
    /// </summary>
    public void EndIntroMode()
    {
        if (currentMode == GameMode.Intro)
        {
            Debug.Log("MusicController: Ending Intro Mode, switching to Normal.");
            SwitchMode(GameMode.Normal);
            // Optional: Immediately check proximity after switching from intro
            if (playerTransform != null)
            {
                CheckProximityAndSwitch();
            }
        }
        else
        {
            Debug.LogWarning("MusicController: EndIntroMode called, but already out of Intro mode.", this);
        }
    }

    /// <summary>
    /// Switches the background music to the specified mode with a crossfade.
    /// Can be called externally, but proximity logic might override Normal/Danger states later.
    /// </summary>
    /// <param name="newMode">The GameMode to switch to.</param>
    public void SwitchMode(GameMode newMode)
    {
        // Prevent switching to the same mode or starting a fade if already fading to the target mode
        if (newMode == currentMode && (fadeCoroutine == null || GetClipForMode(newMode) == activeAudioSource?.clip))
        {
            // If already fading, let it finish unless the target clip is different somehow
            // Allow re-triggering if target mode is same but current active clip is wrong (edge case)
            if (fadeCoroutine == null && GetClipForMode(newMode) != activeAudioSource?.clip && activeAudioSource != null)
            {
                Debug.Log($"MusicController: Re-syncing clip for mode '{newMode}'.");
                // Re-trigger fade to ensure correct clip is playing even if mode enum is correct
            }
            else
            {
                // Debug.Log($"MusicController: Already in mode '{newMode}' or fading to it. No change needed.");
                return; // Already in the target mode or fading correctly
            }
        }


        AudioClip newClip = GetClipForMode(newMode);

        if (newClip == null)
        {
            Debug.LogWarning($"MusicController: No audio clip assigned for mode '{newMode}'. Cannot switch.", this);
            return;
        }

        // Stop any existing fade coroutine before starting a new one
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            // Reset volumes potentially mid-fade before starting new one
            ResetVolumesBeforeFade();
        }

        Debug.Log($"MusicController: Switching mode from '{currentMode}' to '{newMode}'.");
        GameMode oldMode = currentMode;
        currentMode = newMode; // Update the mode immediately
        fadeCoroutine = StartCoroutine(CrossfadeMusic(newClip, oldMode));
    }

    // --- Private Methods ---

    /// <summary>
    /// Resets volumes after stopping a fade abruptly to prepare for a new one.
    /// The 'active' source might not be at full volume if interrupted.
    /// </summary>
    private void ResetVolumesBeforeFade()
    {
        // Try to reasonably set volumes based on which one *was* the active source
        // This prevents starting a new fade from weird intermediate volumes
        if (activeAudioSource == audioSource1)
        {
            audioSource1.volume = 1f; // Assume this was the one meant to be loud
            audioSource2.volume = 0f;
        }
        else if (activeAudioSource == audioSource2)
        {
            audioSource2.volume = 1f;
            audioSource1.volume = 0f;
        }
        // If activeAudioSource is null (very early state), maybe do nothing or set both to 0
        else
        {
            audioSource1.volume = 0f;
            audioSource2.volume = 0f;
        }
    }

    /// <summary>
    /// Checks for nearby enemies using Physics.CheckSphere and triggers mode switches
    /// between Normal and Danger if necessary.
    /// </summary>
    private void CheckProximityAndSwitch()
    {
        if (playerTransform == null) return; // Should not happen due to checks in Update/Start, but safe guard

        // Perform the physics check
        bool enemyDetected = Physics.CheckSphere(
            playerTransform.position,
            detectionRange,
            enemyLayerMask, // Use the specified layer mask
            QueryTriggerInteraction.Ignore // Or .Collide if triggers should count as enemies
        );

        // --- State Change Logic ---
        if (enemyDetected && currentMode == GameMode.Normal)
        {
            // Enemy nearby, and we are in Normal mode -> Switch to Danger
            Debug.Log("MusicController: Enemy detected nearby. Switching to Danger mode.");
            SwitchMode(GameMode.Danger);
            isEnemyNearby = true;
        }
        else if (!enemyDetected && currentMode == GameMode.Danger)
        {
            // No enemy nearby, and we are in Danger mode -> Switch back to Normal
            Debug.Log("MusicController: No enemies nearby. Switching back to Normal mode.");
            SwitchMode(GameMode.Normal);
            isEnemyNearby = false;
        }
        else if (enemyDetected != isEnemyNearby)
        {
            // Update internal state even if mode didn't change (e.g., enemy appeared while already in Danger)
            isEnemyNearby = enemyDetected;
        }
    }


    /// <summary>
    /// Configures common settings for an AudioSource used by this controller.
    /// </summary>
    private void ConfigureAudioSource(AudioSource source)
    {
        if (source != null)
        {
            source.loop = true;
            source.playOnAwake = false;
            // You might want spatialBlend = 0f for background music
            source.spatialBlend = 0f;
        }
    }

    /// <summary>
    /// Gets the appropriate AudioClip for a given GameMode.
    /// </summary>
    private AudioClip GetClipForMode(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.Intro:
                return introMusic;
            case GameMode.Normal:
                return normalMusic;
            case GameMode.Danger:
                return dangerMusic;
            default:
                Debug.LogError($"MusicController: Unknown GameMode '{mode}'.", this);
                return null;
        }
    }

    /// <summary>
    /// Coroutine to handle the crossfading between two audio sources.
    /// </summary>
    /// <param name="newClip">The AudioClip to fade in.</param>
    /// <param name="previousMode">The mode we are fading FROM.</param>
    private IEnumerator CrossfadeMusic(AudioClip newClip, GameMode previousMode)
    {
        AudioSource sourceToFadeOut = activeAudioSource;
        AudioSource sourceToFadeIn = (activeAudioSource == audioSource1) ? audioSource2 : audioSource1;

        // If no source was active yet (e.g., very start or after error), pick one
        if (sourceToFadeOut == null)
        {
            sourceToFadeOut = audioSource2; // Assign arbitrarily, it will have 0 volume
            sourceToFadeIn = audioSource1;
            activeAudioSource = sourceToFadeIn; // Pre-assign the target as active
        }


        // Setup the inactive source to play the new clip
        sourceToFadeIn.clip = newClip;
        sourceToFadeIn.volume = 0f;
        sourceToFadeIn.Play();

        float initialFadeOutVolume = (sourceToFadeOut != null) ? sourceToFadeOut.volume : 0f;
        // If the sourceToFadeOut was already fading out, its volume might not be 1f
        // We capture its current volume to fade FROM that point.

        float timer = 0f;

        while (timer < crossfadeDuration)
        {
            float progress = timer / crossfadeDuration;
            progress = Mathf.Clamp01(progress); // Ensure progress stays between 0 and 1

            if (sourceToFadeOut != null)
                sourceToFadeOut.volume = Mathf.Lerp(initialFadeOutVolume, 0f, progress);

            sourceToFadeIn.volume = Mathf.Lerp(0f, 1f, progress);

            timer += Time.deltaTime;
            yield return null;
        }

        // Ensure final states
        if (sourceToFadeOut != null)
        {
            sourceToFadeOut.volume = 0f;
            sourceToFadeOut.Stop();
            sourceToFadeOut.clip = null; // Optional cleanup
        }
        sourceToFadeIn.volume = 1f;

        activeAudioSource = sourceToFadeIn; // Update the truly active source

        fadeCoroutine = null;
        // Debug.Log("MusicController: Crossfade complete."); // Less verbose logging
    }
}