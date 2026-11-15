using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RocketSlotUI : MonoBehaviour
{
    public string slotType;
    public string slotDisplayName;
    public Button prevButton;
    public Button nextButton;
    public TMP_Text slotNameText;
    public TMP_Text partNameText;
    public TMP_Text partPriceText;

    public void SetPart(RocketPartData part)
    {
        if (slotNameText != null && !string.IsNullOrEmpty(slotDisplayName))
        {
            slotNameText.text = slotDisplayName;
        }

        if (partNameText != null)
        {
            partNameText.text = part != null ? part.partName : "No part";
        }

        if (partPriceText != null)
        {
            partPriceText.text = part != null ? FormatUsd(part.costUsd) : "$0";
        }
    }

    private static string FormatUsd(int amount)
    {
        if (amount >= 1_000_000_000) return $"${amount / 1_000_000_000f:0.##}B";
        if (amount >= 1_000_000) return $"${amount / 1_000_000f:0.##}M";
        return $"${amount:N0}";
    }
}
