using UnityEngine;

public class FishMovementBubbles : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private ParticleSystem bubbleParticleSystem; // Assign your BubbleTrail Particle System here in the Inspector

    [Header("Movement Settings")]
    [SerializeField]
    [Tooltip("Minimum speed the fish must be moving to emit bubbles.")]
    private float movementThreshold = 0.1f; // Adjust this value based on testing

    [SerializeField]
    [Tooltip("Rigibody")]
    private Rigidbody2D rb;

    private ParticleSystem.EmissionModule emissionModule; // Cache the emission module

    void Awake()
    {
        // Ensure the particle system reference is set
        if (bubbleParticleSystem == null)
        {
            Debug.LogError("Bubble Particle System is not assigned in the Inspector!", this);
            enabled = false; // Disable this script if the particle system isn't set
            return;
        }

        // Get the emission module from the particle system
        emissionModule = bubbleParticleSystem.emission;

        // Ensure particles are initially off
        emissionModule.enabled = false;
    }

    void Update()
    {
        // Check if the particle system reference is valid
        if (bubbleParticleSystem == null) return;

        // Get the current speed (magnitude of the velocity vector)
        float currentSpeed = rb.linearVelocity.magnitude;

        // Check if the speed is above the threshold
        if (currentSpeed > movementThreshold)
        {
            // If moving fast enough and particles are not emitting, turn them on
            if (!emissionModule.enabled)
            {
                emissionModule.enabled = true;
                // Optional: If the system wasn't playing, start it. Usually enabling emission is enough if Looping=true.
                // if (!bubbleParticleSystem.isPlaying)
                // {
                //     bubbleParticleSystem.Play();
                // }
            }
        }
        else
        {
            // If moving too slow or stopped, turn off emission
            if (emissionModule.enabled)
            {
                emissionModule.enabled = false;
                // Optional: Stop the system entirely if you want particles to clear instantly. Usually disabling emission is better.
                // bubbleParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}