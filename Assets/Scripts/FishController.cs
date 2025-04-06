// FishController.cs:
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(FishData))]
[RequireComponent(typeof(FishAgent))] // Add requirement for agent access
public class FishController : MonoBehaviour
{
    // --- Component References ---
    private Rigidbody2D rb;
    public FishVisualController fishVisuals;
    public PlayerSoundController playerSoundController;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // --- CRITICAL Rigidbody Settings ---
        rb.gravityScale = 0;
        // --- Auto-find & Error Checks ---
        if (fishVisuals == null) fishVisuals = GetComponentInChildren<FishVisualController>() ?? GetComponentInParent<FishVisualController>();
    }

    void Update()
    {
        if (fishVisuals != null) { fishVisuals.UpdateVisuals(rb.linearVelocity); }
    }
}