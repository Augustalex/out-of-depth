using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerHurt : MonoBehaviour
{
    [Header("Collision Settings")]
    [Tooltip("List of tags that identify an enemy.")]
    [SerializeField] private List<string> enemyTags = new List<string> { "Enemy" };

    [Header("Hurt Effects")]
    [Tooltip("(Optional) The GameObject reference to disable when hurt.")]
    [SerializeField] private GameObject objectToDisable;

    [Tooltip("(Optional) The Component reference (e.g., SpriteRenderer, another script) to disable when hurt.")]
    [SerializeField] private Behaviour componentToDisable;

    [Tooltip("How long the reference stays disabled (in seconds).")]
    [SerializeField] private float disableDuration = 1.0f;

    [Header("Cooldown")]
    [Tooltip("How long the player is invincible after getting hurt (in seconds).")]
    [SerializeField] private float hurtCooldown = 2.0f;

    // Internal State
    private bool canBeHurt = true;
    private Coroutine disableCoroutine = null;
    private PlayerSoundController soundController;

    private void Awake()
    {
        // Get the sound controller component from the same GameObject
        soundController = GetComponent<PlayerSoundController>();
        if (soundController == null)
        {
            Debug.LogWarning("PlayerSoundController component not found on the same GameObject. Hurt sounds won't play.");
        }
    }

    // Called when this collider/rigidbody makes contact with another collider/rigidbody
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Skip collision check if player can't be hurt
        if (!canBeHurt) return;

        // Check if colliding object has an enemy tag
        if (enemyTags.Contains(collision.gameObject.tag))
        {
            TriggerHurtSequence();
        }
    }

    void TriggerHurtSequence()
    {
        // Skip if already being processed
        if (!canBeHurt || disableCoroutine != null) return;

        canBeHurt = false;

        // Call custom hurt logic
        OnPlayerHurt();

        // Start coroutine to handle disabling/enabling and cooldown
        disableCoroutine = StartCoroutine(DisableAndEnableCooldown());
    }

    IEnumerator DisableAndEnableCooldown()
    {
        bool referenceChanged = false;

        // Disable phase
        if (objectToDisable != null && objectToDisable.activeSelf)
        {
            objectToDisable.SetActive(false);
            referenceChanged = true;
        }
        else if (componentToDisable != null && componentToDisable.enabled)
        {
            componentToDisable.enabled = false;
            referenceChanged = true;
        }

        // Wait if something was disabled
        if (referenceChanged)
        {
            yield return new WaitForSeconds(disableDuration);

            // Re-enable phase
            if (objectToDisable != null && !objectToDisable.activeSelf)
            {
                objectToDisable.SetActive(true);
            }
            else if (componentToDisable != null && !componentToDisable.enabled)
            {
                componentToDisable.enabled = true;
            }
        }

        // Wait for cooldown
        yield return new WaitForSeconds(hurtCooldown);

        // Reset state
        canBeHurt = true;
        disableCoroutine = null;
    }

    // Your custom hurt logic goes here
    public void OnPlayerHurt()
    {
        Debug.Log("OnPlayerHurt() called - Add custom logic here!");

        // Play the hurt sound if the sound controller is available
        if (soundController != null)
        {
            soundController.PlayHurtSound();
        }

        // ===== ADD YOUR CUSTOM HURT CODE BELOW THIS LINE =====
        // For example:
        // - Play a hurt sound effect
        // - Trigger a particle effect
        // - Apply knockback force
        // - Flash the player's sprite
        // - Update UI elements
        // ===== ADD YOUR CUSTOM HURT CODE ABOVE THIS LINE =====
    }
}