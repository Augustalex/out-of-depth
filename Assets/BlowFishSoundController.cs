using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class BlowFishSoundController : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField]
    private List<AudioClip> idleSounds = new List<AudioClip>();

    [SerializeField]
    private List<AudioClip> puffSounds = new List<AudioClip>();

    [SerializeField]
    private List<AudioClip> calmSounds = new List<AudioClip>();

    [Header("Configuration")]
    [Range(0f, 1f)]
    [SerializeField]
    private float idleVolume = 1.0f;

    [Range(0f, 1f)]
    [SerializeField]
    private float puffVolume = 1.0f;

    [Range(0f, 1f)]
    [SerializeField]
    private float calmVolume = 1.0f;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.0f;
    }

    public void PlayIdleSound()
    {
        if (idleSounds == null || idleSounds.Count == 0)
        {
            // Debug.LogWarning("BlowFishSoundController: No idle sounds assigned.", this);
            return;
        }

        PlayRandomSound(idleSounds, idleVolume);
    }

    public void PlayPuffSound()
    {
        if (puffSounds == null || puffSounds.Count == 0)
        {
            Debug.LogWarning("BlowFishSoundController: No puff sounds assigned.", this);
            return;
        }

        PlayRandomSound(puffSounds, puffVolume);
    }

    public void PlayCalmSound()
    {
        if (calmSounds == null || calmSounds.Count == 0)
        {
            Debug.LogWarning("BlowFishSoundController: No calm sounds assigned.", this);
            return;
        }

        PlayRandomSound(calmSounds, calmVolume);
    }

    private void PlayRandomSound(List<AudioClip> sounds, float volume)
    {
        if (audioSource == null) return;

        int randomIndex = Random.Range(0, sounds.Count);
        AudioClip clipToPlay = sounds[randomIndex];

        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay, volume);
        }
        else
        {
            Debug.LogWarning($"BlowFishSoundController: AudioClip at index {randomIndex} is null.", this);
        }
    }
}
