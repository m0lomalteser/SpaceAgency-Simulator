using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

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

    private readonly string[] slotKeys = new string[] { "P", "U", "T", "E", "B" };
    private readonly string[] slotNames = new string[] { "Payload", "Upper Stage", "Main Tank", "Main Engine", "Boosters" };

    private List<RocketPartData>[] slotParts;
    private int[] selectedIndices;
    private List<GameObject> spawnedParts = new List<GameObject>();

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
                totalCost += part.costUsd;
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
    }

    public void SaveRocket()
    {
        int totalCost = 0;
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
                totalCost += part.costUsd;
                totalWeight += part.weightKg;
                totalFuel += part.fuelUnits;
                totalThrust += part.thrustKn;
                totalDurability += part.durabilityHp;

                data.parts.Add(new SavedPartData
                {
                    slot = part.slot,
                    id = part.id,
                    partName = part.partName,
                    costUsd = part.costUsd,
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

        float factor = Mathf.Clamp01((totalCost - 5_000_000f) / 2_000_000_000f);
        data.explosionProbability = Mathf.Round(Mathf.Lerp(0.72f, 0.03f, factor) * 1000f) / 1000f;

        string payload = JsonUtility.ToJson(data, true);
        File.WriteAllText(RocketAssembly.SavePath, payload);

        try
        {
            File.WriteAllText(Path.Combine(Application.dataPath, "rocket_build.json"), payload);
        }
        catch { }

        if (saveNotificationText != null)
        {
            saveNotificationText.text = "Rocket saved";
        }
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
