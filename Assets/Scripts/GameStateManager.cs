using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public bool isInDanger = false;
    public bool isPlayerStarving = false; // Example state

    private MusicType currentMusic = MusicType.Peaceful; // Keep track of requested music

    void Update()
    {
        // Example Logic: Determine the correct music based on game state
        MusicType targetMusic = MusicType.Peaceful; // Default

        if (isPlayerStarving)
        {
            targetMusic = MusicType.Starving;
        }

        if (isInDanger) // Danger might override starving music
        {
            targetMusic = MusicType.Danger;
        }
        // Add more conditions for Creepy, etc.

        // Check if the music needs to change
        if (targetMusic != currentMusic)
        {
            // Tell the GlobalSoundManager to play the new music type
            GlobalSoundManager.Instance.PlayMusic(targetMusic);
            currentMusic = targetMusic; // Update our tracked state
        }
    }

    // Example triggers
    public void PlayerEnteredDangerZone()
    {
        isInDanger = true;
        // Update() will handle the music change on the next frame
    }

    public void PlayerLeftDangerZone()
    {
        isInDanger = false;
        // Update() will handle the music change on the next frame
    }

    public void SetStarvingState(bool starving)
    {
        isPlayerStarving = starving;
        // Update() will handle the music change on the next frame
    }

    public void StopAllMusic()
    {
        GlobalSoundManager.Instance.PlayMusic(MusicType.None);
        currentMusic = MusicType.None;
    }
}