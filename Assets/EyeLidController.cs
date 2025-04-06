using UnityEngine;

public class EyeLidController : MonoBehaviour
{
    [Tooltip("The child GameObject with the 'eyes open' sprite/visuals.")]
    public GameObject eyesOpenObject;

    [Tooltip("The child GameObject with the 'eyes normal' sprite/visuals.")]
    public GameObject eyesNormalObject;

    [Tooltip("The child GameObject with the 'eyes closed' sprite/visuals.")]
    public GameObject eyesClosedObject;

    public enum EyeState
    {
        Open,
        Normal,
        Closed
    }

    private EyeState currentEyeState = EyeState.Normal;

    private void Awake()
    {
        // Basic validation to ensure the references are set in the Inspector
        if (eyesOpenObject == null)
        {
            Debug.LogError("EyeLidController: 'Eyes Open Object' is not assigned!", this);
        }
        if (eyesNormalObject == null)
        {
            Debug.LogError("EyeLidController: 'Eyes Normal Object' is not assigned!", this);
        }
        if (eyesClosedObject == null)
        {
            Debug.LogError("EyeLidController: 'Eyes Closed Object' is not assigned!", this);
        }

        // Ensure the eyes start in a known state (normal by default)
        SetEyeState(EyeState.Normal);
    }

    /// <summary>
    /// Sets the visual state of the eyelids.
    /// </summary>
    /// <param name="eyeState">The desired eye state (Open, Normal, or Closed)</param>
    public void SetEyeState(EyeState eyeState)
    {
        currentEyeState = eyeState;

        if (eyesOpenObject != null)
        {
            eyesOpenObject.SetActive(eyeState == EyeState.Open);
        }
        else if (eyeState == EyeState.Open) // Only log error if we *tried* to activate the missing object
        {
            Debug.LogError("EyeLidController: Tried to open eyes, but 'Eyes Open Object' is not assigned!", this);
        }

        if (eyesNormalObject != null)
        {
            eyesNormalObject.SetActive(eyeState == EyeState.Normal);
        }
        else if (eyeState == EyeState.Normal) // Only log error if we *tried* to activate the missing object
        {
            Debug.LogError("EyeLidController: Tried to set normal eyes, but 'Eyes Normal Object' is not assigned!", this);
        }

        if (eyesClosedObject != null)
        {
            eyesClosedObject.SetActive(eyeState == EyeState.Closed);
        }
        else if (eyeState == EyeState.Closed) // Only log error if we *tried* to activate the missing object
        {
            Debug.LogError("EyeLidController: Tried to close eyes, but 'Eyes Closed Object' is not assigned!", this);
        }
    }

    /// <summary>
    /// Returns the current eye state
    /// </summary>
    /// <returns>The current eye state (Open, Normal, or Closed)</returns>
    public EyeState GetEyeState()
    {
        return currentEyeState;
    }
}
