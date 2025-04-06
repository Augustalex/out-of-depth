using UnityEngine;
using System.Collections;

// Ensures there are at least THREE AudioSource components on this GameObject
// Two for music crossfading, one for one-shot sounds
[RequireComponent(typeof(AudioSource), typeof(AudioSource), typeof(AudioSource))]
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

    [Header("One-Shot Sounds")]
    public AudioClip startSound; // Sound to play when exiting intro
    [Range(0f, 1f)]
    public float startSoundVolume = 0.8f; // Volume for the start sound

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

    private AudioSource musicAudioSource1;    // Renamed for clarity
    private AudioSource musicAudioSource2;    // Renamed for clarity
    private AudioSource oneShotAudioSource;   // Dedicated source for one-shots
    private AudioSource activeMusicAudioSource; // Renamed for clarity

    private GameMode currentMode;
    private Coroutine fadeCoroutine;
    private float enemyCheckTimer = 0f;
    private bool isEnemyNearby = false;

    // --- Unity Methods ---

    void Awake()
    {
        // Get the AudioSource components
        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length < 3)
        {
            // This should technically not happen due to [RequireComponent]
            Debug.LogError("MusicController requires exactly three AudioSource components.", this);
            return;
        }
        musicAudioSource1 = sources[0];
        musicAudioSource2 = sources[1];
        oneShotAudioSource = sources[2]; // Assign the third source

        // Configure Music AudioSources
        ConfigureMusicAudioSource(musicAudioSource1);
        ConfigureMusicAudioSource(musicAudioSource2);

        // Configure OneShot AudioSource
        ConfigureOneShotAudioSource(oneShotAudioSource);

        // Ensure music sources start silent
        musicAudioSource1.volume = 0f;
        musicAudioSource2.volume = 0f;

        // Initial check for player assignment
        if (playerTransform == null)
        {
            Debug.LogWarning("MusicController: Player Transform is not assigned. Proximity detection inactive.", this);
        }
        // Initial check for start sound assignment
        if (startSound == null)
        {
            Debug.LogWarning("MusicController: Start Sound is not assigned. No sound will play on intro exit.", this);
        }
    }

    void Start()
    {
        // Start with the initial mode
        currentMode = startMode;
        AudioClip startingClip = GetClipForMode(currentMode);

        if (startingClip != null)
        {
            activeMusicAudioSource = musicAudioSource1; // Start with source 1
            activeMusicAudioSource.clip = startingClip;
            activeMusicAudioSource.volume = 1f; // Start at full volume
            activeMusicAudioSource.Play();
        }
        else
        {
            Debug.LogWarning($"MusicController: No audio clip assigned for starting mode '{startMode}'.", this);
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
        if (currentMode != GameMode.Intro && playerTransform != null)
        {
            enemyCheckTimer += Time.deltaTime;
            if (enemyCheckTimer >= enemyCheckInterval)
            {
                enemyCheckTimer = 0f;
                CheckProximityAndSwitch();
            }
        }
    }

    // --- Public Methods ---

    /// <summary>
    /// Call this method to explicitly exit the Intro mode, play the start sound,
    /// and transition to Normal mode music.
    /// </summary>
    public void EndIntroMode()
    {
        if (currentMode == GameMode.Intro)
        {
            Debug.Log("MusicController: Ending Intro Mode.");

            // Play the one-shot start sound
            PlayStartSound();

            // Switch music to Normal mode
            SwitchMode(GameMode.Normal);

            // Optional: Immediately check proximity after switching from intro
            if (playerTransform != null)
            {
                CheckProximityAndSwitch();
            }
        }
        else
        {
            Debug.LogWarning("MusicController: EndIntroMode called, but not in Intro mode.", this);
        }
    }

    /// <summary>
    /// Switches the background music to the specified mode with a crossfade.
    /// </summary>
    public void SwitchMode(GameMode newMode)
    {
        // Prevent switching to the same mode or starting unnecessary fades
        if (newMode == currentMode && (fadeCoroutine == null || GetClipForMode(newMode) == activeMusicAudioSource?.clip))
        {
            if (fadeCoroutine == null && GetClipForMode(newMode) != activeMusicAudioSource?.clip && activeMusicAudioSource != null)
            {
                Debug.Log($"MusicController: Re-syncing clip for mode '{newMode}'.");
            }
            else
            {
                return; // Already in the target mode or fading correctly
            }
        }

        AudioClip newClip = GetClipForMode(newMode);
        if (newClip == null)
        {
            Debug.LogWarning($"MusicController: No audio clip for mode '{newMode}'. Cannot switch.", this);
            return;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            ResetMusicVolumesBeforeFade();
        }

        Debug.Log($"MusicController: Switching music from '{currentMode}' to '{newMode}'.");
        GameMode oldMode = currentMode;
        currentMode = newMode;
        fadeCoroutine = StartCoroutine(CrossfadeMusic(newClip, oldMode));
    }

    // --- Private Methods ---

    /// <summary>
    /// Plays the assigned startSound using the oneShotAudioSource.
    /// </summary>
    private void PlayStartSound()
    {
        if (startSound != null && oneShotAudioSource != null)
        {
            oneShotAudioSource.PlayOneShot(startSound, startSoundVolume);
            Debug.Log($"MusicController: Playing start sound '{startSound.name}'.");
        }
        else if (startSound == null)
        {
            Debug.LogWarning("MusicController: Cannot play start sound - AudioClip not assigned.", this);
        }
        else // oneShotAudioSource == null
        {
            Debug.LogError("MusicController: Cannot play start sound - OneShot AudioSource is missing!", this);
        }
    }

    /// <summary>
    /// Configures common settings for an AudioSource used for looping background music.
    /// </summary>
    private void ConfigureMusicAudioSource(AudioSource source)
    {
        if (source != null)
        {
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f; // Background music usually not spatialized
            source.volume = 0f;       // Start silent
        }
    }

    /// <summary>
    /// Configures common settings for the AudioSource used for one-shot sounds.
    /// </summary>
    private void ConfigureOneShotAudioSource(AudioSource source)
    {
        if (source != null)
        {
            source.loop = false;        // One-shot sounds don't loop
            source.playOnAwake = false;
            source.spatialBlend = 0f; // Typically UI/non-diegetic sounds are 2D
            source.volume = 1f;       // Volume controlled per-clip via PlayOneShot
            source.priority = 128;    // Default priority, adjust if needed
        }
    }

    /// <summary>
    /// Resets music source volumes after stopping a fade abruptly.
    /// </summary>
    private void ResetMusicVolumesBeforeFade()
    {
        if (activeMusicAudioSource == musicAudioSource1)
        {
            musicAudioSource1.volume = 1f;
            musicAudioSource2.volume = 0f;
        }
        else if (activeMusicAudioSource == musicAudioSource2)
        {
            musicAudioSource2.volume = 1f;
            musicAudioSource1.volume = 0f;
        }
        else
        {
            musicAudioSource1.volume = 0f;
            musicAudioSource2.volume = 0f;
        }
    }

    /// <summary>
    /// Checks for nearby enemies and triggers mode switches between Normal and Danger.
    /// </summary>
    private void CheckProximityAndSwitch()
    {
        if (playerTransform == null) return;

        bool enemyDetected = Physics.CheckSphere(
            playerTransform.position, detectionRange, enemyLayerMask, QueryTriggerInteraction.Ignore
        );

        if (enemyDetected && currentMode == GameMode.Normal)
        {
            // Debug.Log("MusicController: Enemy detected nearby. Switching to Danger mode.");
            SwitchMode(GameMode.Danger);
            isEnemyNearby = true;
        }
        else if (!enemyDetected && currentMode == GameMode.Danger)
        {
            // Debug.Log("MusicController: No enemies nearby. Switching back to Normal mode.");
            SwitchMode(GameMode.Normal);
            isEnemyNearby = false;
        }
        else if (enemyDetected != isEnemyNearby)
        {
            isEnemyNearby = enemyDetected;
        }
    }

    /// <summary>
    /// Gets the appropriate AudioClip for a given GameMode.
    /// </summary>
    private AudioClip GetClipForMode(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.Intro: return introMusic;
            case GameMode.Normal: return normalMusic;
            case GameMode.Danger: return dangerMusic;
            default:
                Debug.LogError($"MusicController: Unknown GameMode '{mode}'.", this);
                return null;
        }
    }

    /// <summary>
    /// Coroutine to handle the crossfading between two music audio sources.
    /// </summary>
    private IEnumerator CrossfadeMusic(AudioClip newClip, GameMode previousMode)
    {
        AudioSource sourceToFadeOut = activeMusicAudioSource;
        AudioSource sourceToFadeIn = (activeMusicAudioSource == musicAudioSource1) ? musicAudioSource2 : musicAudioSource1;

        if (sourceToFadeOut == null)
        {
            sourceToFadeOut = musicAudioSource2;
            sourceToFadeIn = musicAudioSource1;
            // Don't pre-assign activeMusicAudioSource here, wait until fade completes
        }

        sourceToFadeIn.clip = newClip;
        sourceToFadeIn.volume = 0f;
        sourceToFadeIn.Play();

        float initialFadeOutVolume = (sourceToFadeOut != null) ? sourceToFadeOut.volume : 0f;
        float timer = 0f;

        while (timer < crossfadeDuration)
        {
            float progress = Mathf.Clamp01(timer / crossfadeDuration);
            if (sourceToFadeOut != null)
                sourceToFadeOut.volume = Mathf.Lerp(initialFadeOutVolume, 0f, progress);
            sourceToFadeIn.volume = Mathf.Lerp(0f, 1f, progress);
            timer += Time.deltaTime;
            yield return null;
        }

        if (sourceToFadeOut != null)
        {
            sourceToFadeOut.volume = 0f;
            sourceToFadeOut.Stop();
            sourceToFadeOut.clip = null;
        }
        sourceToFadeIn.volume = 1f;

        activeMusicAudioSource = sourceToFadeIn; // Update the truly active music source
        fadeCoroutine = null;
    }
}