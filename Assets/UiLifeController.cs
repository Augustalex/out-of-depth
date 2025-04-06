using UnityEngine;
using System.Collections;
using System;

public class UiLifeController : MonoBehaviour
{
    [Header("UI Elements")]
    public UiLifeLightBulbController[] lightBulbs;

    void Start()
    {
        foreach (var bulb in lightBulbs)
        {
            bulb.RestoreLife();
        }
    }

    public void TakeOneLife()
    {
        var latestActiveBulb = Array.FindLast(lightBulbs, bulb => bulb.IsLifeActive());
        if (latestActiveBulb != null)
        {
            latestActiveBulb.TakeLife();
        }
        else
        {
            Debug.LogWarning("No active light bulb found to take life from.");
        }
    }

    public bool HasNoMoreLife()
    {
        foreach (var bulb in lightBulbs)
        {
            if (bulb.IsLifeActive())
            {
                return false; // At least one life is available
            }
        }
        return true; // No lives left
    }
}