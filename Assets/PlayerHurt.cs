using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Required for using Lists
using System.Linq; // Required for Contains() method on list

public class PlayerHurt : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("The distance within which enemies trigger the hurt effect.")]
    [SerializeField] private float detectionRadius = 2.0f;

    [Tooltip("List of tags that identify an enemy.")]
    [SerializeField] private List<string> enemyTags = new List<string> { "Enemy" }; // Default with "Enemy" tag

    [Tooltip("Specify the layer(s) the enemies are on for optimized detection.")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("Hurt Effects")]
    [Tooltip("(Optional) The GameObject reference to disable when hurt.")]
    [SerializeField] private GameObject objectToDisable;

    [Tooltip("(Optional) The Component reference (e.g., SpriteRenderer, another script) to disable when hurt.")]
    [SerializeField] private Behaviour componentToDisable; // Behaviour is base for Component/MonoBehaviour

    [Tooltip("How long the reference stays disabled (in seconds).")]
    [SerializeField] private float disableDuration = 1.0f;

    [Header("Cooldown")]
    [Tooltip("How long the player is invincible after the reference is re-enabled (in seconds).")]
    [SerializeField] private float hurtCooldown = 2.0f;

    // --- Internal State ---
    private bool canBeHurt = true;
    private float currentCooldownTimer = 0f;
    private Coroutine disableCoroutine = null; // To track the running disable/enable process

    void Update()
    {
        // --- Cooldown Management ---
        if (!canBeHurt)
        {
            currentCooldownTimer -= Time.deltaTime;
            if (currentCooldownTimer <= 0)
            {
                canBeHurt = true;
                // Optional: Add feedback when invincibility ends
                // Debug.Log("Player can be hurt again.");
            }
            // If on cooldown or currently being hurt, don't check for enemies
            return;
        }

        // If the disable/enable coroutine is already running, wait for it to finish
        if (disableCoroutine != null)
        {
            return;
        }

        // --- Enemy Detection ---
        CheckForNearbyEnemies();
    }

    void CheckForNearbyEnemies()
    {
        // Find all colliders within the detection radius on the specified enemy layers
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, detectionRadius, enemyLayer);

        foreach (var hitCollider in hitColliders)
        {
            // Check if the detected collider's tag is in our list of enemy tags
            if (enemyTags.Contains(hitCollider.tag))
            {
                // Found a valid enemy within range! Trigger the hurt sequence.
                TriggerHurtSequence();
                // Optional: Stop checking after finding the first enemy in range this frame
                return;
            }
        }
    }

    void TriggerHurtSequence()
    {
        // Double-check if we can be hurt and aren't already processing a hurt event
        if (canBeHurt && disableCoroutine == null)
        {
            //Debug.Log("Player Hurt!"); // Optional feedback

            canBeHurt = false; // Player is now invincible
            currentCooldownTimer = hurtCooldown; // Set the cooldown duration (timer starts ticking in Update)

            // --- Call your custom hurt logic ---
            OnPlayerHurt();

            // --- Start the process to disable and re-enable the reference ---
            // Check if there's actually something assigned to disable
            if (objectToDisable != null || componentToDisable != null)
            {
                disableCoroutine = StartCoroutine(DisableAndEnableReference());
            }
            else
            {
                // If nothing is assigned, we still trigger OnPlayerHurt and the cooldown,
                // but there's no coroutine to wait for.
                // We set the timer here, and Update will handle the cooldown countdown.
                Debug.LogWarning("PlayerHurt: No GameObject or Component assigned to disable, but hurt triggered.");
                // No coroutine running, cooldown timer will start ticking in Update immediately.
                // Note: 'canBeHurt' is already false.
            }
        }
    }

    // Coroutine to handle the temporary disabling and re-enabling
    IEnumerator DisableAndEnableReference()
    {
        bool referenceChanged = false;

        // --- Disable Phase ---
        if (objectToDisable != null && objectToDisable.activeSelf)
        {
            objectToDisable.SetActive(false);
            referenceChanged = true;
            // Debug.Log($"Disabled GameObject: {objectToDisable.name}");
        }
        else if (componentToDisable != null && componentToDisable.enabled)
        {
            // Ensure it's a Behaviour we can disable/enable
            componentToDisable.enabled = false;
            referenceChanged = true;
            // Debug.Log($"Disabled Component: {componentToDisable.GetType().Name} on {componentToDisable.gameObject.name}");
        }

        // Wait for the specified duration ONLY if something was actually disabled
        if (referenceChanged)
        {
            yield return new WaitForSeconds(disableDuration);

            // --- Re-enable Phase ---
            // Check the specific reference that was disabled
            if (objectToDisable != null && !objectToDisable.activeSelf) // Re-enable the GameObject
            {
                objectToDisable.SetActive(true);
                // Debug.Log($"Re-enabled GameObject: {objectToDisable.name}");
            }
            else if (componentToDisable != null && !componentToDisable.enabled) // Re-enable the Component
            {
                componentToDisable.enabled = true;
                // Debug.Log($"Re-enabled Component: {componentToDisable.GetType().Name} on {componentToDisable.gameObject.name}");
            }
        }
        else
        {
            // This case should ideally be caught before starting the coroutine, but good as a fallback.
            Debug.LogWarning("PlayerHurt: Coroutine ran but no active/enabled reference found to disable.");
        }


        // Coroutine finished
        disableCoroutine = null;
        // The cooldown timer (currentCooldownTimer) is already set and will now start ticking down in Update.
    }

    // --- Your Custom Hurt Logic Goes Here ---
    public void OnPlayerHurt()
    {
        Debug.Log("OnPlayerHurt() called - Add custom logic here!");

        // ===== ADD YOUR CUSTOM HURT CODE BELOW THIS LINE =====
        // For example:
        // - Play a hurt sound effect (e.g., GetComponent<AudioSource>().PlayOneShot(hurtSoundClip);)
        // - Trigger a particle effect (e.g., hurtParticles.Play();)
        // - Apply a small knockback force
        // - Flash the player's sprite color
        // - Update UI elements (like health bar flashing)
        // - If health is managed elsewhere, call that script: FindObjectOfType<PlayerHealth>()?.TakeDamage(1);

        // ===== ADD YOUR CUSTOM HURT CODE ABOVE THIS LINE =====
    }

    // Optional: Draw a gizmo in the Scene view to visualize the detection radius
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}