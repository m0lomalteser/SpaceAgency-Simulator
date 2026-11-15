using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class RocketBuilderSceneSetup
{
    private const string ScenePath = "Assets/Scenes/RocketBuilder.unity";
    private const string RegistryPath = "Assets/PartRegister/PartRegistry.asset";
    private const string FontPath = "Assets/Fonts/Font SDF.asset";

    [MenuItem("SpaceAgency/Rocket Builder/Builder-Szene komplett generieren", priority = 1)]
    public static void SetupScene()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            BuildHierarchy();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[RocketBuilderSceneSetup] Builder-Szene erfolgreich generiert und gespeichert!");
        }
    }

    [InitializeOnLoadMethod]
    private static void AutoSetupIfEmpty()
    {
        EditorApplication.delayCall += () =>
        {
            if (File.Exists(ScenePath))
            {
                string text = File.ReadAllText(ScenePath);
                if (!text.Contains("RocketConfigurator"))
                {
                    SetupScene();
                }
            }
        };
    }

    private static void BuildHierarchy()
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Object.DestroyImmediate(roots[i]);
        }

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            font = TMP_Settings.defaultFontAsset;
        }

        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.06f, 0.10f, 1f);
        cam.fieldOfView = 50f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000f;
        camObj.AddComponent<AudioListener>();
        camObj.transform.position = new Vector3(0f, 0f, -14f);
        camObj.transform.rotation = Quaternion.identity;

        GameObject mainLightObj = new GameObject("Directional Light");
        Light mainLight = mainLightObj.AddComponent<Light>();
        mainLight.type = LightType.Directional;
        mainLight.intensity = 1.2f;
        mainLight.color = new Color(1f, 0.98f, 0.92f, 1f);
        mainLightObj.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

        GameObject fillLightObj = new GameObject("Fill Light");
        Light fillLight = fillLightObj.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.intensity = 0.45f;
        fillLight.color = new Color(0.65f, 0.8f, 1f, 1f);
        fillLightObj.transform.rotation = Quaternion.Euler(30f, 150f, 0f);

        GameObject rocketRootObj = new GameObject("RocketRoot");
        rocketRootObj.transform.position = Vector3.zero;

        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<EventSystem>();
        var actionsAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        var inputModule = eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        if (actionsAsset != null)
        {
            inputModule.actionsAsset = actionsAsset;
        }

        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject titleObj = CreateText(canvasObj.transform, "TitleText", "ROCKET CONFIGURATOR", font, 34, FontStyles.Bold, TextAlignmentOptions.Center);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -25f);
        titleRt.sizeDelta = new Vector2(600f, 50f);

        GameObject subTitleObj = CreateText(canvasObj.transform, "SubTitleText", "Combine parts | Drag to rotate the rocket", font, 16, FontStyles.Normal, TextAlignmentOptions.Center);
        RectTransform subTitleRt = subTitleObj.GetComponent<RectTransform>();
        subTitleRt.anchorMin = new Vector2(0.5f, 1f);
        subTitleRt.anchorMax = new Vector2(0.5f, 1f);
        subTitleRt.pivot = new Vector2(0.5f, 1f);
        subTitleRt.anchoredPosition = new Vector2(0f, -70f);
        subTitleRt.sizeDelta = new Vector2(600f, 30f);
        subTitleObj.GetComponent<TextMeshProUGUI>().color = new Color(0.7f, 0.75f, 0.85f, 1f);

        GameObject backBtnObj = CreateButton(canvasObj.transform, "BackButton", "BACK", font, 16);
        RectTransform backRt = backBtnObj.GetComponent<RectTransform>();
        backRt.anchorMin = new Vector2(0f, 1f);
        backRt.anchorMax = new Vector2(0f, 1f);
        backRt.pivot = new Vector2(0f, 1f);
        backRt.anchoredPosition = new Vector2(40f, -30f);
        backRt.sizeDelta = new Vector2(130f, 44f);

        string[] slotKeys = new string[] { "P", "U", "T", "E", "B" };
        string[] slotNames = new string[] { "Payload", "Upper Stage", "Main Tank", "Main Engine", "Boosters" };
        float[] yPositions = new float[] { 220f, 110f, 0f, -110f, -220f };

        RocketSlotUI[] slotUIs = new RocketSlotUI[slotKeys.Length];

        for (int i = 0; i < slotKeys.Length; i++)
        {
            GameObject slotRowObj = new GameObject($"SlotRow_{slotKeys[i]}");
            slotRowObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rowRt = slotRowObj.AddComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.5f, 0.5f);
            rowRt.anchorMax = new Vector2(0.5f, 0.5f);
            rowRt.pivot = new Vector2(0.5f, 0.5f);
            rowRt.anchoredPosition = new Vector2(0f, yPositions[i]);
            rowRt.sizeDelta = new Vector2(1000f, 80f);

            GameObject prevBtnObj = CreateButton(slotRowObj.transform, "PrevButton", "<", font, 24);
            RectTransform prevRt = prevBtnObj.GetComponent<RectTransform>();
            prevRt.anchoredPosition = new Vector2(-240f, 0f);
            prevRt.sizeDelta = new Vector2(50f, 50f);

            GameObject nextBtnObj = CreateButton(slotRowObj.transform, "NextButton", ">", font, 24);
            RectTransform nextRt = nextBtnObj.GetComponent<RectTransform>();
            nextRt.anchoredPosition = new Vector2(240f, 0f);
            nextRt.sizeDelta = new Vector2(50f, 50f);

            GameObject cardObj = CreatePanel(slotRowObj.transform, "InfoCard", new Color(0.08f, 0.12f, 0.18f, 0.75f));
            RectTransform cardRt = cardObj.GetComponent<RectTransform>();
            cardRt.anchoredPosition = new Vector2(-510f, 0f);
            cardRt.sizeDelta = new Vector2(360f, 65f);

            GameObject slotNameTextObj = CreateText(cardObj.transform, "SlotName", slotNames[i], font, 13, FontStyles.Bold, TextAlignmentOptions.Left);
            RectTransform snRt = slotNameTextObj.GetComponent<RectTransform>();
            snRt.anchorMin = new Vector2(0f, 1f);
            snRt.anchorMax = new Vector2(1f, 1f);
            snRt.pivot = new Vector2(0f, 1f);
            snRt.anchoredPosition = new Vector2(15f, -8f);
            snRt.sizeDelta = new Vector2(-30f, 20f);
            slotNameTextObj.GetComponent<TextMeshProUGUI>().color = new Color(0.35f, 0.75f, 1f, 1f);

            GameObject partNameTextObj = CreateText(cardObj.transform, "PartName", "Part", font, 16, FontStyles.Bold, TextAlignmentOptions.Left);
            RectTransform pnRt = partNameTextObj.GetComponent<RectTransform>();
            pnRt.anchorMin = new Vector2(0f, 0f);
            pnRt.anchorMax = new Vector2(0.65f, 1f);
            pnRt.pivot = new Vector2(0f, 0f);
            pnRt.anchoredPosition = new Vector2(15f, 8f);
            pnRt.sizeDelta = new Vector2(-15f, -30f);

            GameObject partPriceTextObj = CreateText(cardObj.transform, "PartPrice", "$0", font, 16, FontStyles.Bold, TextAlignmentOptions.Right);
            RectTransform ppRt = partPriceTextObj.GetComponent<RectTransform>();
            ppRt.anchorMin = new Vector2(0.65f, 0f);
            ppRt.anchorMax = new Vector2(1f, 1f);
            ppRt.pivot = new Vector2(1f, 0f);
            ppRt.anchoredPosition = new Vector2(-15f, 8f);
            ppRt.sizeDelta = new Vector2(-15f, -30f);
            partPriceTextObj.GetComponent<TextMeshProUGUI>().color = new Color(0.4f, 0.95f, 0.4f, 1f);

            RocketSlotUI slotUI = slotRowObj.AddComponent<RocketSlotUI>();
            slotUI.slotType = slotKeys[i];
            slotUI.slotDisplayName = slotNames[i];
            slotUI.prevButton = prevBtnObj.GetComponent<Button>();
            slotUI.nextButton = nextBtnObj.GetComponent<Button>();
            slotUI.slotNameText = slotNameTextObj.GetComponent<TextMeshProUGUI>();
            slotUI.partNameText = partNameTextObj.GetComponent<TextMeshProUGUI>();
            slotUI.partPriceText = partPriceTextObj.GetComponent<TextMeshProUGUI>();

            slotUIs[i] = slotUI;
        }

        GameObject statsPanelObj = CreatePanel(canvasObj.transform, "StatsPanel", new Color(0.06f, 0.09f, 0.15f, 0.9f));
        RectTransform statsRt = statsPanelObj.GetComponent<RectTransform>();
        statsRt.anchorMin = new Vector2(1f, 0.5f);
        statsRt.anchorMax = new Vector2(1f, 0.5f);
        statsRt.pivot = new Vector2(1f, 0.5f);
        statsRt.anchoredPosition = new Vector2(-40f, 0f);
        statsRt.sizeDelta = new Vector2(360f, 520f);

        GameObject statsHeaderObj = CreateText(statsPanelObj.transform, "StatsHeader", "SPECIFICATIONS", font, 22, FontStyles.Bold, TextAlignmentOptions.Center);
        RectTransform shRt = statsHeaderObj.GetComponent<RectTransform>();
        shRt.anchorMin = new Vector2(0.5f, 1f);
        shRt.anchorMax = new Vector2(0.5f, 1f);
        shRt.pivot = new Vector2(0.5f, 1f);
        shRt.anchoredPosition = new Vector2(0f, -20f);
        shRt.sizeDelta = new Vector2(320f, 35f);

        GameObject divObj = CreatePanel(statsPanelObj.transform, "Divider", new Color(0.25f, 0.35f, 0.5f, 0.6f));
        RectTransform divRt = divObj.GetComponent<RectTransform>();
        divRt.anchorMin = new Vector2(0.5f, 1f);
        divRt.anchorMax = new Vector2(0.5f, 1f);
        divRt.pivot = new Vector2(0.5f, 1f);
        divRt.anchoredPosition = new Vector2(0f, -60f);
        divRt.sizeDelta = new Vector2(310f, 2f);

        TMP_Text costVal = CreateStatRow(statsPanelObj.transform, font, "Total Cost:", "$0", -80f, new Color(0.4f, 0.95f, 0.4f, 1f));
        TMP_Text weightVal = CreateStatRow(statsPanelObj.transform, font, "Total Mass:", "0 kg", -135f, Color.white);
        TMP_Text fuelVal = CreateStatRow(statsPanelObj.transform, font, "Propellant:", "0 L", -190f, new Color(0.35f, 0.8f, 1f, 1f));
        TMP_Text thrustVal = CreateStatRow(statsPanelObj.transform, font, "Total Thrust:", "0 kN", -245f, new Color(1f, 0.7f, 0.25f, 1f));
        TMP_Text durVal = CreateStatRow(statsPanelObj.transform, font, "Durability:", "0 HP", -300f, new Color(0.85f, 0.85f, 0.85f, 1f));

        GameObject saveBtnObj = CreateButton(statsPanelObj.transform, "SaveButton", "SAVE ROCKET", font, 16);
        RectTransform saveRt = saveBtnObj.GetComponent<RectTransform>();
        saveRt.anchorMin = new Vector2(0.5f, 0f);
        saveRt.anchorMax = new Vector2(0.5f, 0f);
        saveRt.pivot = new Vector2(0.5f, 0f);
        saveRt.anchoredPosition = new Vector2(0f, 75f);
        saveRt.sizeDelta = new Vector2(300f, 50f);
        saveBtnObj.GetComponent<Image>().color = new Color(0.12f, 0.45f, 0.28f, 1f);

        GameObject statusObj = CreateText(statsPanelObj.transform, "SaveNotification", "Ready to save", font, 13, FontStyles.Normal, TextAlignmentOptions.Center);
        RectTransform statusRt = statusObj.GetComponent<RectTransform>();
        statusRt.anchorMin = new Vector2(0.5f, 0f);
        statusRt.anchorMax = new Vector2(0.5f, 0f);
        statusRt.pivot = new Vector2(0.5f, 0f);
        statusRt.anchoredPosition = new Vector2(0f, 30f);
        statusRt.sizeDelta = new Vector2(320f, 30f);
        statusObj.GetComponent<TextMeshProUGUI>().color = new Color(0.6f, 0.75f, 0.65f, 1f);

        GameObject managerObj = new GameObject("RocketConfigurator");
        RocketConfigurator configurator = managerObj.AddComponent<RocketConfigurator>();
        configurator.registry = AssetDatabase.LoadAssetAtPath<PartRegistry>(RegistryPath);
        configurator.rocketRoot = rocketRootObj.transform;
        configurator.slotUIs = slotUIs;
        configurator.totalCostText = costVal;
        configurator.totalWeightText = weightVal;
        configurator.totalFuelText = fuelVal;
        configurator.totalThrustText = thrustVal;
        configurator.totalDurabilityText = durVal;
        configurator.saveNotificationText = statusObj.GetComponent<TextMeshProUGUI>();
        configurator.saveButton = saveBtnObj.GetComponent<Button>();
        configurator.backButton = backBtnObj.GetComponent<Button>();
        configurator.targetCamera = cam;
        configurator.cameraBaseDistance = 14f;
        configurator.returnSceneName = "Game";
    }

    private static TMP_Text CreateStatRow(Transform parent, TMP_FontAsset font, string label, string defaultValue, float yOffset, Color valueColor)
    {
        GameObject rowObj = new GameObject("Row_" + label);
        rowObj.transform.SetParent(parent, false);
        RectTransform rt = rowObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, yOffset);
        rt.sizeDelta = new Vector2(300f, 40f);

        GameObject lblObj = CreateText(rowObj.transform, "Label", label, font, 15, FontStyles.Normal, TextAlignmentOptions.Left);
        RectTransform lRt = lblObj.GetComponent<RectTransform>();
        lRt.anchorMin = new Vector2(0f, 0f);
        lRt.anchorMax = new Vector2(0.5f, 1f);
        lRt.pivot = new Vector2(0f, 0.5f);
        lRt.anchoredPosition = Vector2.zero;
        lRt.sizeDelta = Vector2.zero;
        lblObj.GetComponent<TextMeshProUGUI>().color = new Color(0.7f, 0.75f, 0.85f, 1f);

        GameObject valObj = CreateText(rowObj.transform, "Value", defaultValue, font, 17, FontStyles.Bold, TextAlignmentOptions.Right);
        RectTransform vRt = valObj.GetComponent<RectTransform>();
        vRt.anchorMin = new Vector2(0.5f, 0f);
        vRt.anchorMax = new Vector2(1f, 1f);
        vRt.pivot = new Vector2(1f, 0.5f);
        vRt.anchoredPosition = Vector2.zero;
        vRt.sizeDelta = Vector2.zero;
        TextMeshProUGUI valTmp = valObj.GetComponent<TextMeshProUGUI>();
        valTmp.color = valueColor;

        return valTmp;
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        Image img = obj.AddComponent<Image>();
        img.color = color;
        return obj;
    }

    private static GameObject CreateText(Transform parent, string name, string text, TMP_FontAsset font, float fontSize, FontStyles style, TextAlignmentOptions align)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            tmp.font = font;
        }
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.color = Color.white;
        return obj;
    }

    private static GameObject CreateButton(Transform parent, string name, string label, TMP_FontAsset font, float fontSize)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.15f, 0.22f, 0.35f, 0.9f);
        Button btn = btnObj.AddComponent<Button>();

        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.15f, 0.22f, 0.35f, 0.9f);
        cb.highlightedColor = new Color(0.25f, 0.35f, 0.55f, 1f);
        cb.pressedColor = new Color(0.1f, 0.15f, 0.25f, 1f);
        cb.selectedColor = cb.highlightedColor;
        btn.colors = cb;

        GameObject txtObj = CreateText(btnObj.transform, "Text", label, font, fontSize, FontStyles.Bold, TextAlignmentOptions.Center);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;
        txtRt.anchoredPosition = Vector2.zero;

        return btnObj;
    }
}
