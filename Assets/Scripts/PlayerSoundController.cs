using UnityEngine;
using System.Collections.Generic; // Required for using List

// Require an AudioSource component to be attached to the same GameObject
// This ensures you don't forget to add it in the Unity Editor.
[RequireComponent(typeof(AudioSource))]
public class PlayerSoundController : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] // Expose in Inspector but keep private
    private List<AudioClip> dashSounds = new List<AudioClip>(); // List to hold dash sounds

    [SerializeField] // Expose in Inspector but keep private
    private List<AudioClip> eatSounds = new List<AudioClip>(); // List to hold eat sounds

    [Header("Configuration")]
    [Range(0f, 1f)] // Add a slider for volume in the Inspector
    [SerializeField]
    private float dashVolume = 1.0f;

    [Range(0f, 1f)] // Add a slider for volume in the Inspector
    [SerializeField]
    private float eatVolume = 1.0f;

    // Reference to the required AudioSource component
    private AudioSource audioSource;

    void Awake()
    {
        // Get the AudioSource component attached to this GameObject
        audioSource = GetComponent<AudioSource>();

        // Optional: Basic configuration for the AudioSource
        // You might want to adjust these settings based on your game's needs
        audioSource.playOnAwake = false; // Don't play sound automatically on start
                                         // audioSource.spatialBlend = 1.0f; // Set to 1.0f for 3D sound, 0.0f for 2D
                                         // For a 2D game, 0.0f is usually appropriate
        audioSource.spatialBlend = 0.0f;
    }

    /// <summary>
    /// Plays a random dash sound from the list.
    /// </summary>
    public void PlayDashSound()
    {
        // Check if there are any dash sounds assigned in the list
        if (dashSounds == null || dashSounds.Count == 0)
        {
            Debug.LogWarning("PlayerSoundController: No dash sounds assigned in the list.", this);
            return; // Exit if no sounds are available
        }

        // Check if the AudioSource component is ready
        if (audioSource == null)
        {
            Debug.LogError("PlayerSoundController: AudioSource component not found!", this);
            return; // Exit if AudioSource is missing (shouldn't happen due to [RequireComponent])
        }

        // Select a random AudioClip from the list
        int randomIndex = Random.Range(0, dashSounds.Count);
        AudioClip clipToPlay = dashSounds[randomIndex];

        // Check if the selected clip is actually valid
        if (clipToPlay != null)
        {
            // Play the selected sound effect once, using the specified volume
            // PlayOneShot is good for effects as it doesn't interrupt other sounds
            // playing on the same AudioSource (if needed) and can overlap.
            audioSource.PlayOneShot(clipToPlay, dashVolume);
        }
        else
        {
            Debug.LogWarning($"PlayerSoundController: AudioClip at index {randomIndex} is null.", this);
        }
    }

    /// <summary>
    /// Plays a random eat sound from the list.
    /// </summary>
    public void PlayEatSound()
    {
        // Check if there are any eat sounds assigned in the list
        if (eatSounds == null || eatSounds.Count == 0)
        {
            Debug.LogWarning("PlayerSoundController: No eat sounds assigned in the list.", this);
            return; // Exit if no sounds are available
        }

        // Check if the AudioSource component is ready
        if (audioSource == null)
        {
            Debug.LogError("PlayerSoundController: AudioSource component not found!", this);
            return; // Exit if AudioSource is missing (shouldn't happen due to [RequireComponent])
        }

        // Select a random AudioClip from the list
        int randomIndex = Random.Range(0, eatSounds.Count);
        AudioClip clipToPlay = eatSounds[randomIndex];

        // Check if the selected clip is actually valid
        if (clipToPlay != null)
        {
            // Play the selected sound effect once, using the specified volume
            audioSource.PlayOneShot(clipToPlay, eatVolume);
        }
        else
        {
            Debug.LogWarning($"PlayerSoundController: AudioClip at index {randomIndex} is null.", this);
        }
    }

    // --- Example: Add more sound methods later ---
    // public void PlayHurtSound() { /* ... */ }
    // public void PlaySwimSound() { /* ... */ }
}