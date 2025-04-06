// BodyController.cs
using UnityEngine;

public class BodyController : MonoBehaviour
{
    [Tooltip("The child GameObject with the 'mouth open' sprite/visuals.")]
    public GameObject mouthOpenObject;

    [Tooltip("The child GameObject with the 'mouth closed' sprite/visuals.")]
    public GameObject mouthClosedObject;

    private bool isMouthOpen = false;

    private void Awake()
    {
        // Basic validation to ensure the references are set in the Inspector
        if (mouthOpenObject == null)
        {
            Debug.LogError("BodyController: 'Mouth Open Object' is not assigned!", this);
        }
        if (mouthClosedObject == null)
        {
            Debug.LogError("BodyController: 'Mouth Closed Object' is not assigned!", this);
        }

        // Ensure the mouth starts in a known state (e.g., closed)
        SetMouthState(false);
    }

    /// <summary>
    /// Sets the visual state of the mouth.
    /// </summary>
    /// <param name="isOpen">True to show the open mouth, false to show the closed mouth.</param>
    public void SetMouthState(bool isOpen)
    {
        isMouthOpen = isOpen;

        if (mouthOpenObject != null)
        {
            mouthOpenObject.SetActive(isOpen);
        }
        else if (isOpen) // Only log error if we *tried* to activate the missing object
        {
            Debug.LogError("BodyController: Tried to open mouth, but 'Mouth Open Object' is not assigned!", this);
        }

        if (mouthClosedObject != null)
        {
            mouthClosedObject.SetActive(!isOpen); // Activate closed mouth if isOpen is false
        }
        else if (!isOpen) // Only log error if we *tried* to activate the missing object
        {
            Debug.LogError("BodyController: Tried to close mouth, but 'Mouth Closed Object' is not assigned!", this);
        }
    }

    /// <summary>
    /// Returns the current mouth state
    /// </summary>
    /// <returns>True if mouth is open, false if closed</returns>
    public bool IsMouthOpen()
    {
        return isMouthOpen;
    }
}