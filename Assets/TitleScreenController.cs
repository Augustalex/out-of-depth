using UnityEngine;
using System.Collections; // Required for Coroutines

public class TitleScreenController : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("The parent GameObject holding the title screen UI elements (e.g., the Canvas or a Panel under it)")]
    public GameObject titleScreenUIContainer; // Assign the Canvas or a specific panel holding TitleImage and StartPromptImage

    [Header("Game Elements")]
    [Tooltip("The main Player GameObject")]
    public GameObject playerObject; // Assign your Player GameObject

    [Tooltip("The in-game UI GameObject")]
    public GameObject inGameUI; // Assign your Player GameObject

    [Header("Timing Settings")]
    [Tooltip("Delay in seconds after hiding UI before enabling the player")]
    public float delayBeforePlayerEnable = 2.0f; // Set your desired delay

    [Tooltip("Automatically start the game after this many seconds if no input is received. Set to 0 or less to disable.")]
    public float autoStartTime = 15.0f; // <<< NEW: Timeout delay (X seconds)

    private bool titleIsActive = true; // Flag to prevent multiple triggers

    void Start()
    {
        // Ensure the player is initially disabled
        if (playerObject != null)
        {
            playerObject.SetActive(false);
        }
        else
        {
            Debug.LogError("Player Object not assigned in the TitleScreenController!");
        }

        if (inGameUI != null)
        {
            inGameUI.SetActive(false);
        }
        else
        {
            Debug.LogError("In-Game UI not assigned in the TitleScreenController!");
        }

        // Ensure the Title Screen UI is initially enabled
        if (titleScreenUIContainer != null)
        {
            titleScreenUIContainer.SetActive(true);
        }
        else
        {
            Debug.LogError("Title Screen UI Container not assigned in the TitleScreenController!");
        }

        // Start the automatic timer IF autoStartTime is positive
        if (autoStartTime > 0)
        {
            // Schedule the StartGameSequence method to be called after 'autoStartTime' seconds.
            // Using nameof() is safer than using the string "StartGameSequence" directly.
            Invoke(nameof(StartGameSequence), autoStartTime);
            Debug.Log($"Title screen auto-start scheduled in {autoStartTime} seconds.");
        }
    }

    void Update()
    {
        // // Only check for input if the title screen is currently active
        // if (titleIsActive && Input.anyKeyDown) // Detects any key or mouse button press
        // {
        //     Debug.Log("Input detected, starting game sequence manually.");
        //     // Input received, so cancel the scheduled automatic start (if it was scheduled)
        //     CancelInvoke(nameof(StartGameSequence));
        //     // Trigger the game start sequence immediately
        //     StartGameSequence();
        // }
    }

    // This method handles the transition away from the title screen.
    // It's now called EITHER by player input (via Update) OR automatically (via Invoke).
    void StartGameSequence()
    {
        // Check if the sequence has already been triggered to prevent running twice
        if (!titleIsActive)
        {
            return; // Already transitioning, do nothing more
        }

        // Mark the title screen as no longer active
        titleIsActive = false;

        var musicController = FindObjectOfType<MusicController>();
        musicController.EndIntroMode();

        // Just in case, cancel any pending invokes again (belt-and-suspenders)
        CancelInvoke(nameof(StartGameSequence));

        Debug.Log("Starting Game Sequence: Hiding UI and starting player enable timer.");

        // Hide the title screen UI
        if (titleScreenUIContainer != null)
        {
            titleScreenUIContainer.SetActive(false);
        }

        // Start the coroutine to wait and then enable the player
        StartCoroutine(EnablePlayerAfterDelay());
    }

    IEnumerator EnablePlayerAfterDelay()
    {
        // Wait for the specified delay before enabling the player
        yield return new WaitForSeconds(delayBeforePlayerEnable);

        // Enable the player object
        if (playerObject != null)
        {
            playerObject.SetActive(true);
            Debug.Log("Player GameObject enabled after delay.");
        }
        else
        {
            Debug.LogError("Cannot enable Player Object - it was not assigned!");
        }

        if (inGameUI != null)
        {
            inGameUI.SetActive(true);
            Debug.Log("In-Game UI enabled.");
        }
        else
        {
            Debug.LogError("Cannot enable In-Game UI - it was not assigned!");
        }

        // Optional: You might want to disable this controller script now if it's no longer needed
        // this.enabled = false;
    }
}