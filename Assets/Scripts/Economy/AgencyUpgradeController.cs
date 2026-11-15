using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Inspector-freundliche Brücke zwischen der Upgrade-UI und dem
/// AgencyEconomyManager.
///
/// Du kannst diese Komponente z. B. auf das Upgrade-Panel oder einen
/// beliebigen Manager legen und die vier Buttons im Inspector zuweisen.
/// Alternativ können die öffentlichen Methoden direkt in den
/// Button-OnClick-Events benutzt werden.
/// </summary>
public class AgencyUpgradeController : MonoBehaviour
{
    [Header("Buttons (optional, werden automatisch verbunden)")]
    public Button advertisementsButton;
    public Button reusableRocketsButton;
    public Button mannedMissionsButton;
    public Button cheaperPartsButton;

    [Header("Feedback (optional)")]
    [Tooltip("Optionaler TMP-Text für Meldungen wie 'Nicht genug Geld'.")]
    public TMP_Text feedbackText;

    [Header("Einstellungen")]
    [Tooltip("Buttons beim Start automatisch mit den Kauf-Methoden verbinden.")]
    public bool autoWireButtons = true;

    [Tooltip("Buttons automatisch deaktivieren, wenn Geld fehlt oder das Upgrade schon gekauft ist.")]
    public bool updateButtonInteractable = true;

    [Tooltip("Meldungen zusätzlich in die Konsole schreiben.")]
    public bool logMessages = true;

    private AgencyEconomyManager Economy =>
        AgencyEconomyManager.Instance;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Start()
    {
        if (autoWireButtons)
            WireButtons();

        Economy.MoneyChanged += OnMoneyChanged;
        Economy.UpgradePurchased += OnUpgradePurchased;

        RefreshInteractable();
    }

    private void OnDestroy()
    {
        AgencyEconomyManager economy =
            AgencyEconomyManager.ExistingInstance;

        if (economy == null)
            return;

        economy.MoneyChanged -= OnMoneyChanged;
        economy.UpgradePurchased -= OnUpgradePurchased;

        if (!autoWireButtons)
            return;

        RemoveButton(advertisementsButton, BuyAdvertisements);
        RemoveButton(reusableRocketsButton, BuyReusableRockets);
        RemoveButton(mannedMissionsButton, BuyMannedMissions);
        RemoveButton(cheaperPartsButton, BuyCheaperParts);
    }

    private void WireButtons()
    {
        AddButton(advertisementsButton, BuyAdvertisements);
        AddButton(reusableRocketsButton, BuyReusableRockets);
        AddButton(mannedMissionsButton, BuyMannedMissions);
        AddButton(cheaperPartsButton, BuyCheaperParts);
    }

    private static void AddButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static void RemoveButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(action);
    }

    // =========================================================
    // PURCHASE METHODS
    // =========================================================

    public void BuyAdvertisements()
    {
        Purchase(AgencyUpgradeType.Advertisements);
    }

    public void BuyReusableRockets()
    {
        Purchase(AgencyUpgradeType.ReusableRockets);
    }

    public void BuyMannedMissions()
    {
        Purchase(AgencyUpgradeType.MannedMissions);
    }

    public void BuyCheaperParts()
    {
        Purchase(AgencyUpgradeType.CheaperParts);
    }

    /// <summary>
    /// Für Unity-Events, die einen int-Parameter übergeben:
    /// 0 = Advertisements, 1 = Reusable Rockets,
    /// 2 = Manned Missions, 3 = Cheaper Parts.
    /// </summary>
    public void BuyUpgradeByIndex(int index)
    {
        if (index < 0 || index > 3)
        {
            ShowFeedback("Unbekanntes Upgrade.");
            return;
        }

        Purchase((AgencyUpgradeType)index);
    }

    private void Purchase(AgencyUpgradeType type)
    {
        string message;

        bool purchased = Economy.BuyUpgrade(type, out message);

        ShowFeedback(message);

        if (purchased)
            RefreshInteractable();
    }

    // =========================================================
    // MANNED MISSIONS
    // =========================================================

    /// <summary>Kann direkt in ein UnityEvent für eine erfolgreiche Mission gelegt werden.</summary>
    public void ResolveMannedMissionSuccess()
    {
        string message;

        bool resolved = Economy.ResolveMannedMissionSuccess(out message);

        ShowFeedback(message);

        if (resolved)
            RefreshInteractable();
    }

    /// <summary>Kann direkt in ein UnityEvent für eine fehlgeschlagene Mission gelegt werden.</summary>
    public void ResolveMannedMissionFailure()
    {
        string message;

        bool resolved = Economy.ResolveMannedMissionFailure(out message);

        ShowFeedback(message);

        if (resolved)
            RefreshInteractable();
    }

    // =========================================================
    // HELPERS FÜR ANDERE SYSTEME
    // =========================================================

    public long GetDiscountedPartCost(long baseCostUsd)
    {
        return Economy.GetPartCost(baseCostUsd);
    }

    public long GetDiscountedPartCost(int baseCostUsd)
    {
        return Economy.GetPartCost(baseCostUsd);
    }

    public bool RocketsAreReusable => Economy.RocketsAreReusable;

    public int RocketReuseCount => Economy.RocketReuseCount;

    public float ReusedRocketCostMultiplier =>
        Economy.ReusedRocketCostMultiplier;

    public bool CanLaunchMannedMissions =>
        Economy.CanLaunchMannedMissions;

    public bool HasUpgrade(int index)
    {
        if (index < 0 || index > 3)
            return false;

        return Economy.HasUpgrade((AgencyUpgradeType)index);
    }

    // =========================================================
    // UI STATE
    // =========================================================

    private void OnMoneyChanged(long money)
    {
        RefreshInteractable();
    }

    private void OnUpgradePurchased(AgencyUpgradeType type)
    {
        RefreshInteractable();
    }

    public void RefreshInteractable()
    {
        if (!updateButtonInteractable)
            return;

        AgencyEconomyManager economy = Economy;

        SetInteractable(
            advertisementsButton,
            AgencyUpgradeType.Advertisements,
            economy
        );

        SetInteractable(
            reusableRocketsButton,
            AgencyUpgradeType.ReusableRockets,
            economy
        );

        SetInteractable(
            mannedMissionsButton,
            AgencyUpgradeType.MannedMissions,
            economy
        );

        SetInteractable(
            cheaperPartsButton,
            AgencyUpgradeType.CheaperParts,
            economy
        );
    }

    private static void SetInteractable(
        Button button,
        AgencyUpgradeType type,
        AgencyEconomyManager economy)
    {
        if (button == null)
            return;

        bool alreadyOwned = economy.HasUpgrade(type);
        bool canAfford = economy.CanAfford(
            economy.GetUpgradeCost(type)
        );

        button.interactable = !alreadyOwned && canAfford;
    }

    private void ShowFeedback(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        if (feedbackText != null)
            feedbackText.text = message;

        if (logMessages)
            Debug.Log("[AgencyUpgradeController] " + message);
    }
}
