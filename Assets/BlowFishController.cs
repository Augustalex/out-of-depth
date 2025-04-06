using UnityEngine;

public class BlowFishController : MonoBehaviour
{
    [Header("Primary State Game Objects")]
    [Tooltip("The GameObject representing the small state of the blowfish")]
    public GameObject smallStateObject;

    [Tooltip("The GameObject representing the big state of the blowfish")]
    public GameObject bigStateObject;

    private bool isSmallState = true;
    private BodyController smallStateBodyController;
    private BlowFishSoundController soundController;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Get the reference to the BodyController component from the smallStateObject
        if (smallStateObject != null)
        {
            smallStateBodyController = smallStateObject.GetComponentInChildren<BodyController>();
            if (smallStateBodyController == null)
            {
                Debug.LogError("BodyController component not found on smallStateObject!", this);
            }
        }
        else
        {
            Debug.LogError("smallStateObject is not assigned!", this);
        }

        // Initialize the state to small with mouth closed
        SetSmallState();

        soundController = GetComponent<BlowFishSoundController>();
        if (soundController == null)
        {
            Debug.LogWarning("BlowFishSoundController not found!", this);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// Sets the blowfish to the small state
    /// </summary>
    public void SetSmallState()
    {
        if (smallStateObject != null && bigStateObject != null)
        {
            smallStateObject.SetActive(true);
            bigStateObject.SetActive(false);
            isSmallState = true;

            if (soundController != null)
            {
                soundController.PlayCalmSound();
            }
        }
        else
        {
            Debug.LogError("State objects are not properly assigned!", this);
        }
    }

    /// <summary>
    /// Sets the blowfish to the big state
    /// </summary>
    public void SetBigState()
    {
        if (smallStateObject != null && bigStateObject != null)
        {
            smallStateObject.SetActive(false);
            bigStateObject.SetActive(true);
            isSmallState = false;

            if (soundController != null)
            {
                soundController.PlayPuffSound();
            }
        }
        else
        {
            Debug.LogError("State objects are not properly assigned!", this);
        }
    }

    /// <summary>
    /// Sets the mouth state when in the small state
    /// </summary>
    /// <param name="isOpen">True to open the mouth, false to close it</param>
    public void SetSmallStateMouth(bool isOpen)
    {
        if (!isSmallState)
        {
            Debug.LogWarning("Cannot change mouth state while in big state!", this);
            return;
        }

        if (smallStateBodyController != null)
        {
            smallStateBodyController.SetMouthState(isOpen);
        }
        else
        {
            Debug.LogError("BodyController reference is missing!", this);
        }
    }

    /// <summary>
    /// Opens the mouth when in small state
    /// </summary>
    public void OpenMouth()
    {
        SetSmallStateMouth(true);
        if (soundController != null)
        {
            soundController.PlayIdleSound();
        }
    }

    /// <summary>
    /// Closes the mouth when in small state
    /// </summary>
    public void CloseMouth()
    {
        SetSmallStateMouth(false);
    }

    /// <summary>
    /// Checks if the blowfish is currently in the small state
    /// </summary>
    /// <returns>True if in small state, false if in big state</returns>
    public bool IsSmallState()
    {
        return isSmallState;
    }

    /// <summary>
    /// Checks if the blowfish is currently in the big state
    /// </summary>
    /// <returns>True if in big state, false if in small state</returns>
    public bool IsBigState()
    {
        return !isSmallState;
    }

    /// <summary>
    /// Checks if the mouth is open when in small state
    /// </summary>
    /// <returns>True if the mouth is open, false if closed or not in small state</returns>
    public bool IsMouthOpen()
    {
        if (!isSmallState || smallStateBodyController == null)
        {
            return false;
        }

        return smallStateBodyController.IsMouthOpen();
    }

    /// <summary>
    /// Triggers the attack squish animation on the FishSquisher component
    /// </summary>
    public void TriggerAttackSquish()
    {
        FishSquisher fishSquisher = GetComponentInChildren<FishSquisher>();
        if (fishSquisher != null)
        {
            fishSquisher.TriggerSquish(FishSquisher.SquishActionType.Attack);
        }
        else
        {
            Debug.LogWarning("FishSquisher component not found!", this);
        }
    }
}
