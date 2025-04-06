using UnityEngine;
using UnityEngine.UI;

public class UiHungerController : MonoBehaviour
{
    [SerializeField] private Image fullFishImage;

    private void UpdateHungerVisual(float normalizedHunger)
    {
        if (fullFishImage == null)
        {
            Debug.LogError("Full Fish Image is not assigned in the HungerBarController!");
            return;
        }

        fullFishImage.fillAmount = normalizedHunger;
    }

    public void UpdateHungerFromStats(float currentHunger)
    {
        if (currentHunger <= 0)
        {
            UpdateHungerVisual(0f);
            return;
        }

        UpdateHungerVisual(Mathf.Min(currentHunger, 1f));
    }

}