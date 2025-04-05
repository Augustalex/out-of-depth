using UnityEngine;
using System.Collections; // Required for Coroutines

// Enum to define the different music states
public enum MusicType
{
    None, // To stop music
    Peaceful,
    Starving,
    Creepy,
    Danger
}

public class GlobalSoundManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    private static GlobalSoundManager _instance;
    public static GlobalSoundManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing instance in the scene
                _instance = FindObjectOfType<GlobalSoundManager>();

                if (_instance == null)
                {
                    // If not found, create a new GameObject and add the script
                    GameObject singletonObject = new GameObject("GlobalSoundManager");
                    _instance = singletonObject.AddComponent<GlobalSoundManager>();
                    Debug.Log("GlobalSoundManager instance created.");
                }
            }
            return _instance;
        }
    }

    // --- Audio Sources ---
    // We need multiple sources: one for ambience, two for music crossfading
    private AudioSource ambienceSource;
    private AudioSource musicSource1;
    private AudioSource musicSource2;
    private bool isMusicSource1Active = true; // Tracks which music source is currently playing/fading in

    // --- Audio Clips (Assign these in the Inspector) ---
    [Header("Audio Clips")]
    [SerializeField] private AudioClip backgroundAmbienceClip;
    [SerializeField] private AudioClip peacefulMusicClip;
    [SerializeField] private AudioClip starvingMusicClip;
    [SerializeField] private AudioClip creepyMusicClip;
    [SerializeField] private AudioClip dangerMusicClip;

    // --- Configuration ---
    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float maxAmbienceVolume = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float maxMusicVolume = 0.8f;

    [Header("Fading")]
    [SerializeField] private float musicCrossfadeDuration = 2.0f; // Duration in seconds for music crossfade

    private Coroutine musicFadeCoroutine; // To manage the active fade process

    // --- Initialization ---
    private void Awake()
    {
        // --- Singleton Enforcement ---
        if (_instance != null && _instance != this)
        {
            // If another instance exists, destroy this one
            Debug.LogWarning("Duplicate GlobalSoundManager detected. Destroying the new one.");
            Destroy(gameObject);
            return;
        }
        _instance = this;
        // Make this object persist across scene loads
        DontDestroyOnLoad(gameObject);

        // --- Create and Configure Audio Sources ---
        ambienceSource = gameObject.AddComponent<AudioSource>();
        musicSource1 = gameObject.AddComponent<AudioSource>();
        musicSource2 = gameObject.AddComponent<AudioSource>();

        ConfigureAudioSource(ambienceSource, true, false, maxAmbienceVolume); // Loop ambience, no play on awake (we start it manually)
        ConfigureAudioSource(musicSource1, true, false, 0f); // Loop music, start silent
        ConfigureAudioSource(musicSource2, true, false, 0f); // Loop music, start silent

        Debug.Log("GlobalSoundManager initialized.");
    }

    private void Start()
    {
        // --- Start Ambience ---
        PlayAmbience();

        // --- Start Initial Music (e.g., Peaceful) ---
        // You can change MusicType.Peaceful to MusicType.None if you
        // want no music initially.
        PlayMusic(MusicType.Peaceful, true); // Start immediately, no fade-in needed for first track
    }

    // Helper to configure common AudioSource settings
    private void ConfigureAudioSource(AudioSource source, bool loop, bool playOnAwake, float initialVolume)
    {
        source.loop = loop;
        source.playOnAwake = playOnAwake;
        source.volume = initialVolume;
        source.spatialBlend = 0.0f; // Ensure 2D sound
    }

    // --- Ambience Control ---
    private void PlayAmbience()
    {
        if (backgroundAmbienceClip != null && ambienceSource != null)
        {
            ambienceSource.clip = backgroundAmbienceClip;
            ambienceSource.volume = maxAmbienceVolume;
            ambienceSource.Play();
            Debug.Log("Playing background ambience.");
        }
        else
        {
            Debug.LogWarning("Cannot play ambience: Clip or AudioSource is missing.", this);
        }
    }

    public void SetAmbienceVolume(float volume)
    {
        maxAmbienceVolume = Mathf.Clamp01(volume);
        if (ambienceSource != null && ambienceSource.isPlaying)
        {
            ambienceSource.volume = maxAmbienceVolume;
        }
    }


    // --- Music Control ---

    /// <summary>
    /// Switches to the specified music type with a crossfade.
    /// </summary>
    /// <param name="type">The type of music to play (from the MusicType enum).</param>
    /// <param name="instant">If true, play instantly without fading (useful for initial start).</param>
    public void PlayMusic(MusicType type, bool instant = false)
    {
        AudioClip clipToPlay = GetClipByType(type);

        // If type is None or clip is null, fade out current music
        if (clipToPlay == null)
        {
            FadeOutCurrentMusic(instant ? 0f : musicCrossfadeDuration);
            return;
        }

        // Determine target and fading sources
        AudioSource activeSource = isMusicSource1Active ? musicSource1 : musicSource2;
        AudioSource newSource = isMusicSource1Active ? musicSource2 : musicSource1;

        // Avoid restarting the same track if it's already the active one playing
        // You can comment this out if you *want* to allow restarting the same track with a fade
        if (activeSource.clip == clipToPlay && activeSource.isPlaying && activeSource.volume > 0.1f) // Check if already playing this clip
        {
            Debug.Log($"Music type {type} is already playing.");
            // Ensure it's at max volume if it was fading out previously
            if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
            activeSource.volume = maxMusicVolume; // Snap to full volume
            newSource.Stop(); // Ensure the other source is stopped
            newSource.volume = 0f;
            musicFadeCoroutine = null;
            return;
        }


        // Stop any existing fade coroutine
        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
        }

        // Start the new clip on the inactive source
        newSource.clip = clipToPlay;
        newSource.Play(); // Start playing immediately, volume controlled by fade

        // Start the crossfade coroutine
        float duration = instant ? 0f : musicCrossfadeDuration;
        musicFadeCoroutine = StartCoroutine(CrossfadeRoutine(activeSource, newSource, duration));

        // Switch the active source tracker
        isMusicSource1Active = !isMusicSource1Active;

        Debug.Log($"Starting music: {type}{(instant ? " (Instant)" : $" (Fade: {duration}s)")}");
    }

    private void FadeOutCurrentMusic(float duration)
    {
        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
        }
        AudioSource activeSource = isMusicSource1Active ? musicSource1 : musicSource2;
        AudioSource inactiveSource = isMusicSource1Active ? musicSource2 : musicSource1; // Target for potential future fade-in

        musicFadeCoroutine = StartCoroutine(FadeOutRoutine(activeSource, inactiveSource, duration));
        Debug.Log($"Fading out music. Duration: {duration}s");
    }


    // Selects the correct AudioClip based on the MusicType
    private AudioClip GetClipByType(MusicType type)
    {
        switch (type)
        {
            case MusicType.Peaceful: return peacefulMusicClip;
            case MusicType.Starving: return starvingMusicClip;
            case MusicType.Creepy: return creepyMusicClip;
            case MusicType.Danger: return dangerMusicClip;
            case MusicType.None:
            default: return null; // No clip for 'None' or unknown types
        }
    }

    // Coroutine for crossfading between two audio sources
    private IEnumerator CrossfadeRoutine(AudioSource sourceToFadeOut, AudioSource sourceToFadeIn, float duration)
    {
        float timer = 0f;
        float startVolumeFadeOut = sourceToFadeOut.volume; // Fade from current volume
        // Ensure fade-in starts from 0, even if it was interrupted mid-fade previously
        sourceToFadeIn.volume = 0f;

        // If duration is zero, snap instantly
        if (duration <= 0f)
        {
            sourceToFadeOut.volume = 0f;
            sourceToFadeOut.Stop(); // Stop the old track
            sourceToFadeIn.volume = maxMusicVolume;
            musicFadeCoroutine = null; // Mark coroutine as finished
            yield break; // Exit coroutine
        }


        while (timer < duration)
        {
            // Calculate progress (0 to 1)
            float progress = timer / duration;

            // Decrease volume of the outgoing source
            sourceToFadeOut.volume = Mathf.Lerp(startVolumeFadeOut, 0f, progress);
            // Increase volume of the incoming source
            sourceToFadeIn.volume = Mathf.Lerp(0f, maxMusicVolume, progress);

            // Wait for the next frame
            timer += Time.unscaledDeltaTime; // Use unscaled time if you want fades during pause
            yield return null;
        }

        // Ensure final volumes are set precisely and stop the faded-out source
        sourceToFadeOut.volume = 0f;
        sourceToFadeOut.Stop();
        sourceToFadeIn.volume = maxMusicVolume;

        musicFadeCoroutine = null; // Mark coroutine as finished
    }

    // Coroutine to just fade out the active source and stop it
    private IEnumerator FadeOutRoutine(AudioSource sourceToFadeOut, AudioSource inactiveSource, float duration)
    {
        float timer = 0f;
        float startVolume = sourceToFadeOut.volume;

        // Ensure the other source is silent and stopped
        inactiveSource.Stop();
        inactiveSource.volume = 0f;


        // If duration is zero, snap instantly
        if (duration <= 0f)
        {
            sourceToFadeOut.volume = 0f;
            sourceToFadeOut.Stop(); // Stop the old track
            musicFadeCoroutine = null; // Mark coroutine as finished
            yield break; // Exit coroutine
        }


        while (timer < duration)
        {
            float progress = timer / duration;
            sourceToFadeOut.volume = Mathf.Lerp(startVolume, 0f, progress);
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        sourceToFadeOut.volume = 0f;
        sourceToFadeOut.Stop();
        musicFadeCoroutine = null;
    }

    // Optional: Method to set overall music volume
    public void SetMusicVolume(float volume)
    {
        maxMusicVolume = Mathf.Clamp01(volume);
        // Adjust the currently active source's volume immediately if no fade is happening
        if (musicFadeCoroutine == null)
        {
            AudioSource activeSource = isMusicSource1Active ? musicSource1 : musicSource2;
            activeSource.volume = maxMusicVolume;
        }
        // The fade routines use maxMusicVolume as their target, so future fades will respect the change.
    }

    // --- OnDestroy ---
    private void OnDestroy()
    {
        // Cleanup if this instance is destroyed (e.g., exiting play mode)
        if (_instance == this)
        {
            _instance = null;
            Debug.Log("GlobalSoundManager instance destroyed.");
        }
    }
}