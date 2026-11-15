using UnityEngine;
using TMPro;

public class SpaceAgencyMoneyDisplay : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("TextMeshPro-Element für die Geldanzeige")]
    [SerializeField] private TextMeshProUGUI moneyText;

    [Header("References")]
    [Tooltip("Ziehe hier dein SpaceAgencyLoader-Skript hinein")]
    [SerializeField] private SpaceAgencyLoader agencyLoader;

    void Update()
    {
        // Falls der Loader nicht im Inspector zugewiesen wurde, suchen wir ihn automatisch
        if (agencyLoader == null)
        {
            agencyLoader = FindObjectOfType<SpaceAgencyLoader>();
            return;
        }

        // Geld aus dem Loader abrufen und anzeigen
        UpdateMoneyUI(agencyLoader.CurrentMoney);
    }

    private void UpdateMoneyUI(long money)
    {
        if (moneyText == null) return;

        moneyText.text = FormatMoney(money);
    }

    private string FormatMoney(long amount)
    {
        // Formatierung mit Kürzeln (k, M, B, T)
        if (amount >= 1000000000000L)
            return $"{amount / 1000000000000.0:0.##}T $";
        if (amount >= 1000000000L)
            return $"{amount / 1000000000.0:0.##}B $";
        if (amount >= 1000000L)
            return $"{amount / 1000000.0:0.##}M $";
        if (amount >= 1000L)
            return $"{amount / 1000.0:0.##}k $";

        return $"{amount} $";
    }
}
