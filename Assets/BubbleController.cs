using UnityEngine;

// Ensures this script is attached to a GameObject that also has a ParticleSystem component.
[RequireComponent(typeof(ParticleSystem))]
public class BubbleController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the Fish's Rigidbody2D component here.")]
    public Rigidbody2D fishRigidbody; // Public variable to drag the Rigidbody2D onto in the Inspector

    [Header("Emission Settings")]
    [Tooltip("The minimum bubble emission rate (when the fish is still).")]
    public float minEmissionRate = 1f;

    [Tooltip("The maximum bubble emission rate (at or above max speed).")]
    public float maxEmissionRate = 10f;

    [Tooltip("The speed at which the maximum emission rate is reached.")]
    public float speedForMaxEmission = 5f;

    // Private variables
    private ParticleSystem bubbleParticleSystem;
    private ParticleSystem.EmissionModule emissionModule; // To control emission properties

    void Awake()
    {
        // Get the ParticleSystem component attached to this same GameObject
        bubbleParticleSystem = GetComponent<ParticleSystem>();

        // Get the Emission module from the particle system so we can change its properties
        // Note: You need to get the module into a variable to modify it.
        emissionModule = bubbleParticleSystem.emission;
    }

    void Start()
    {
        // Check if the Rigidbody reference was set in the inspector
        if (fishRigidbody == null)
        {
            Debug.LogError("Fish Rigidbody reference not set in the Inspector for BubbleController!", this);
            // Disable the script if the reference is missing to prevent errors
            enabled = false;
            return;
        }

        // Set the initial emission rate (optional, could just wait for Update)
        emissionModule.rateOverTime = minEmissionRate;
    }

    void Update()
    {
        // If the fish Rigidbody somehow becomes null (e.g., destroyed), stop checking
        if (fishRigidbody == null)
        {
            emissionModule.rateOverTime = 0; // Optional: stop emitting if fish is gone
            return;
        }

        // Get the current speed of the fish Rigidbody
        // velocity is a Vector2, magnitude gives its length (which is the speed)
        float currentSpeed = fishRigidbody.linearVelocity.magnitude;

        // Calculate how far the current speed is towards the max speed (0 to 1 range)
        // Mathf.Clamp01 ensures the value stays between 0 and 1, even if speed exceeds speedForMaxEmission or speedForMaxEmission is 0.
        float speedRatio = 0f;
        if (speedForMaxEmission > 0) // Avoid division by zero
        {
            speedRatio = Mathf.Clamp01(currentSpeed / speedForMaxEmission);
        }
        else if (currentSpeed > 0) // If max speed is 0, any movement triggers max emission
        {
            speedRatio = 1f;
        }


        // Linearly interpolate between the min and max emission rate based on the speed ratio
        // Lerp(a, b, t) calculates: a + (b - a) * t
        float desiredEmissionRate = Mathf.Lerp(minEmissionRate, maxEmissionRate, speedRatio);

        // Apply the calculated rate to the particle system's emission module
        emissionModule.rateOverTime = desiredEmissionRate;
    }
}