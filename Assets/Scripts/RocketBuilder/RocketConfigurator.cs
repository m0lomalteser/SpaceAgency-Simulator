using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[System.Serializable]
public class AgencySaveData
{
    public int version;
    public string agencyName;
    public string logoPng;
    public long money;
    public float mood;
    public int day;
    public int month;
    public int year;
    public float timeOfDay;
    public int tutorialStep;
    public bool tutorialDone;
    public int totalLaunches;
    public int satelliteLaunches;
    public int satelliteSuccesses;
    public int satelliteFailures;
    public int moonLaunches;
    public int moonSuccesses;
    public int moonFailures;
    public int marsLaunches;
    public int marsSuccesses;
    public int marsFailures;
    public int consecutiveFailures;
    public int[] upgrades;
    public long lifetimeBudget;
    public long lifetimeSpent;
    public long lastBudget;
    public int pendingMission;
    public long pendingLaunchCost;
    public int lastResultMission;
    public int lastResultSuccess;
    public string lastResultText;

    // Wirtschaft / Regierungsgeld
    public long regierungsgeld;
    public long lastGovernmentGrant;
    public float lastGovernmentGrantMood;
    public string lastGovernmentGrantDate;
    public string lastAdvertisementPayoutDate;
    public string lastAdvertisementMoodDate;
    public int mannedMissionLaunches;
    public int mannedMissionSuccesses;
    public int mannedMissionFailures;
    public long mannedMissionCompensationPaid;

    public string createdAt;
    public string updatedAt;
}

public class RocketConfigurator : MonoBehaviour
{
    public PartRegistry registry;
    public Transform rocketRoot;
    public RocketSlotUI[] slotUIs;

    public TMP_Text totalCostText;
    public TMP_Text totalWeightText;
    public TMP_Text totalFuelText;
    public TMP_Text totalThrustText;
    public TMP_Text totalDurabilityText;
    public TMP_Text saveNotificationText;

    public Button saveButton;
    public Button backButton;
    public string returnSceneName = "Game";

    public Camera targetCamera;
    public float cameraBaseDistance = 14f;

    [Header("Economy")]
    [Tooltip("Wendet den Cheaper-Parts-Rabatt aus der AgencyEconomy an.")]
    public bool applyEconomyDiscounts = true;

    private readonly string[] slotKeys = new string[] { "P", "U", "T", "E", "B" };
    private readonly string[] slotNames = new string[] { "Payload", "Upper Stage", "Main Tank", "Main Engine", "Boosters" };

    private List<RocketPartData>[] slotParts;
    private int[] selectedIndices;
    private List<GameObject> spawnedParts = new List<GameObject>();

    // Pfad zur space_agency_save.json im Persistent Data Path (AppStorage)
    private string AgencySavePath => Path.Combine(Application.persistentDataPath, "space_agency_save.json");

    private void Awake()
    {
        if (PlayerPrefs.HasKey("RocketBuilder.ReturnScene"))
        {
            returnSceneName = PlayerPrefs.GetString("RocketBuilder.ReturnScene", returnSceneName);
        }

        registry = RocketAssembly.LoadRegistry(registry);
        InitializeSlots();
        TryLoadSavedSelection();
    }

    private void Start()
    {
        BindUI();
        RebuildRocket();
        UpdateUI();
    }

    private AgencySaveData LoadAgencyData()
    {
        try
        {
            if (File.Exists(AgencySavePath))
            {
                string json = File.ReadAllText(AgencySavePath);
                return JsonUtility.FromJson<AgencySaveData>(json);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed to load agency save: " + e.Message);
        }
        return null;
    }

    private void SaveAgencyData(AgencySaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(AgencySavePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save agency data: " + e.Message);
        }
    }

    /// <label>Prüft, ob die aktuell ausgewählten Teile exakt der letzten gespeicherten Rakete entsprechen</label>
    private bool IsCurrentRocketIdenticalToSaved()
    {
        RocketSaveData save = RocketAssembly.LoadSave();
        if (save == null || save.parts == null) return false;

        for (int i = 0; i < slotKeys.Length; i++)
        {
            RocketPartData currentPart = GetCurrentPart(i);
            string currentId = currentPart != null ? currentPart.id : string.Empty;
            string currentSlot = slotKeys[i];

            SavedPartData savedPart = save.parts.Find(p => p != null && p.slot == currentSlot);
            string savedId = savedPart != null ? savedPart.id : string.Empty;

            if (currentId != savedId)
            {
                return false; // Es gibt mindestens einen Unterschied
            }
        }
        return true; // 1:1 identisch mit der gespeicherten Rakete
    }

    private void InitializeSlots()
    {
        slotParts = new List<RocketPartData>[slotKeys.Length];
        selectedIndices = new int[slotKeys.Length];

        for (int i = 0; i < slotKeys.Length; i++)
        {
            slotParts[i] = registry != null ? registry.GetPartsBySlot(slotKeys[i]) : new List<RocketPartData>();
            selectedIndices[i] = 0;
        }
    }

    private void TryLoadSavedSelection()
    {
        RocketSaveData save = RocketAssembly.LoadSave();
        if (save == null || save.parts == null) return;

        for (int i = 0; i < slotKeys.Length; i++)
        {
            SavedPartData saved = save.parts.Find(p => p != null && p.slot == slotKeys[i]);
            if (saved == null || slotParts[i] == null) continue;

            int idx = slotParts[i].FindIndex(p => p != null && p.id == saved.id);
            if (idx >= 0) selectedIndices[i] = idx;
        }
    }

    private void BindUI()
    {
        if (slotUIs != null)
        {
            for (int i = 0; i < slotUIs.Length && i < slotKeys.Length; i++)
            {
                int slotIndex = i;
                if (slotUIs[i] != null)
                {
                    slotUIs[i].slotType = slotKeys[i];
                    slotUIs[i].slotDisplayName = slotNames[i];

                    if (slotUIs[i].prevButton != null)
                    {
                        slotUIs[i].prevButton.onClick.AddListener(() => ChangePart(slotIndex, -1));
                    }
                    if (slotUIs[i].nextButton != null)
                    {
                        slotUIs[i].nextButton.onClick.AddListener(() => ChangePart(slotIndex, 1));
                    }
                }
            }
        }

        if (saveButton != null)
        {
            saveButton.onClick.AddListener(SaveRocket);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }
    }

    public void ChangePart(int slotIndex, int delta)
    {
        if (slotParts == null || slotIndex < 0 || slotIndex >= slotParts.Length) return;
        if (slotParts[slotIndex] == null || slotParts[slotIndex].Count == 0) return;

        int count = slotParts[slotIndex].Count;
        selectedIndices[slotIndex] = (selectedIndices[slotIndex] + delta + count) % count;

        RebuildRocket();
        UpdateUI();
    }

    public RocketPartData GetCurrentPart(int slotIndex)
    {
        if (slotParts == null || slotIndex < 0 || slotIndex >= slotParts.Length) return null;
        if (slotParts[slotIndex] == null || slotParts[slotIndex].Count == 0) return null;

        int idx = Mathf.Clamp(selectedIndices[slotIndex], 0, slotParts[slotIndex].Count - 1);
        return slotParts[slotIndex][idx];
    }

    public void RebuildRocket()
    {
        spawnedParts.Clear();
        if (rocketRoot == null) return;

        Quaternion savedRotation = rocketRoot.rotation;
        rocketRoot.rotation = Quaternion.identity;
        rocketRoot.position = Vector3.zero;
        rocketRoot.localScale = Vector3.one;

        List<RocketPartData> parts = new List<RocketPartData>();
        for (int i = 0; i < slotKeys.Length; i++)
        {
            RocketPartData part = GetCurrentPart(i);
            if (part != null) parts.Add(part);
        }

        RocketAssembly.BuildStackedRocket(rocketRoot, parts, out float currentY);

        for (int i = 0; i < rocketRoot.childCount; i++)
        {
            spawnedParts.Add(rocketRoot.GetChild(i).gameObject);
        }

        rocketRoot.position = new Vector3(0f, -currentY * 0.5f, 0f);
        rocketRoot.rotation = savedRotation;

        if (targetCamera != null)
        {
            float distance = Mathf.Max(cameraBaseDistance, currentY * 1.15f + 4f);
            targetCamera.transform.position = new Vector3(0f, 0f, -distance);
        }
    }

    /// <summary>
    /// Tatsächlicher Preis eines Teils inklusive Cheaper-Parts-Rabatt.
    /// </summary>
    public long GetEffectivePartCost(RocketPartData part)
    {
        if (part == null)
            return 0;

        if (!applyEconomyDiscounts)
            return part.costUsd;

        AgencyEconomyManager economy = AgencyEconomyManager.ExistingInstance;

        return economy != null ? economy.GetPartCost(part.costUsd) : part.costUsd;
    }

    public void UpdateUI()
    {
        int totalCost = 0;
        float totalWeight = 0f;
        float totalFuel = 0f;
        float totalThrust = 0f;
        float totalDurability = 0f;

        for (int i = 0; i < slotKeys.Length; i++)
        {
            RocketPartData part = GetCurrentPart(i);
            if (slotUIs != null && i < slotUIs.Length && slotUIs[i] != null)
            {
                slotUIs[i].SetPart(part);
            }

            if (part != null)
            {
                totalCost += (int)GetEffectivePartCost(part);
                totalWeight += part.weightKg;
                totalFuel += part.fuelUnits;
                totalThrust += part.thrustKn;
                totalDurability += part.durabilityHp;
            }
        }

        if (totalCostText != null)
            totalCostText.text = FormatUsd(totalCost);
        if (totalWeightText != null)
            totalWeightText.text = $"{totalWeight:N0} kg";
        if (totalFuelText != null)
            totalFuelText.text = $"{totalFuel:N0} L";
        if (totalThrustText != null)
            totalThrustText.text = $"{totalThrust:N0} kN";
        if (totalDurabilityText != null)
            totalDurabilityText.text = $"{totalDurability:N0} HP";

        // Budget- und Identik-Prüfung für den Save-Button
        bool isIdentical = IsCurrentRocketIdenticalToSaved();
        AgencySaveData agencyData = LoadAgencyData();
        long availableMoney = agencyData != null ? agencyData.money : 0;

        // Button ist aktiv, wenn sie 1:1 identisch ist ODER genug Budget vorhanden ist
        bool canSave = isIdentical || (availableMoney >= totalCost);

        if (saveButton != null)
        {
            saveButton.interactable = canSave;
        }

        if (saveNotificationText != null)
        {
            if (!isIdentical && !canSave)
                saveNotificationText.text = "Insufficient Budget!";
            else if (isIdentical)
                saveNotificationText.text = "";
            else
                saveNotificationText.text = "";
        }
    }

    public void SaveRocket()
    {
        bool isIdentical = IsCurrentRocketIdenticalToSaved();
        int totalCost = 0;
        int baseTotalCost = 0;

        for (int i = 0; i < slotKeys.Length; i++)
        {
            RocketPartData part = GetCurrentPart(i);
            if (part == null)
                continue;

            totalCost += (int)GetEffectivePartCost(part);
            baseTotalCost += part.costUsd;
        }

        // 1. Budget nur prüfen und abziehen, wenn sich etwas geändert hat (nicht 1:1 identisch)
        AgencySaveData agencyData = LoadAgencyData();
        if (agencyData != null && !isIdentical)
        {
            if (agencyData.money < totalCost)
            {
                if (saveNotificationText != null)
                    saveNotificationText.text = "Error: Insufficient Budget!";
                return;
            }

            // Geld abziehen & Aktualisierungszeit setzen
            agencyData.money -= totalCost;
            agencyData.lifetimeSpent += totalCost;
            agencyData.updatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SaveAgencyData(agencyData);
        }

        // 2. Raketendaten zusammenstellen & speichern
        float totalWeight = 0f;
        float totalFuel = 0f;
        float totalThrust = 0f;
        float totalDurability = 0f;

        RocketSaveData data = new RocketSaveData();
        data.rocketName = "Rocket_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        data.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        for (int i = 0; i < slotKeys.Length; i++)
        {
            RocketPartData part = GetCurrentPart(i);
            if (part != null)
            {
                totalWeight += part.weightKg;
                totalFuel += part.fuelUnits;
                totalThrust += part.thrustKn;
                totalDurability += part.durabilityHp;

                data.parts.Add(new SavedPartData
                {
                    slot = part.slot,
                    id = part.id,
                    partName = part.partName,
                    costUsd = (int)GetEffectivePartCost(part),
                    weightKg = part.weightKg,
                    fuelUnits = part.fuelUnits,
                    thrustKn = part.thrustKn,
                    durabilityHp = part.durabilityHp
                });
            }
        }

        data.totalCost = totalCost;
        data.totalWeightKg = totalWeight;
        data.totalFuelUnits = totalFuel;
        data.totalThrustKn = totalThrust;
        data.durabilityHp = totalDurability;

        // Explosionsrisiko hängt vom Grundpreis ab, damit der Cheaper-Parts-Rabatt
        // die Rakete nicht künstlich unsicherer macht.
        float factor = Mathf.Clamp01((baseTotalCost - 5_000_000f) / 2_000_000_000f);
        data.explosionProbability = Mathf.Round(Mathf.Lerp(0.72f, 0.03f, factor) * 1000f) / 1000f;

        AgencyEconomyManager economy = AgencyEconomyManager.ExistingInstance;
        data.reuseUsesRemaining = economy != null ? economy.RocketReuseCount : 0;

        string payload = JsonUtility.ToJson(data, true);
        File.WriteAllText(RocketAssembly.SavePath, payload);

        try
        {
            File.WriteAllText(Path.Combine(Application.dataPath, "rocket_build.json"), payload);
        }
        catch { }

        if (saveNotificationText != null)
        {
            saveNotificationText.text = isIdentical ? "Saved" : "Rocket built and Budget updated!";
        }

        UpdateUI();
    }

    private void OnBackClicked()
    {
        string scene = string.IsNullOrEmpty(returnSceneName) ? "Game" : returnSceneName;
        UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
    }

    private static string FormatUsd(int amount)
    {
        if (amount >= 1_000_000_000)
            return $"${amount / 1_000_000_000f:0.##}B";
        if (amount >= 1_000_000)
            return $"${amount / 1_000_000f:0.##}M";
        return $"${amount:N0}";
    }

    private void Update()
    {
        if (Input.GetMouseButton(0) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
        {
            float rotX = Input.GetAxis("Mouse X") * 5f;
            if (rocketRoot != null)
            {
                rocketRoot.Rotate(Vector3.up, -rotX, Space.World);
            }
        }
    }
}