using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Baut die Overlay-UI (Pause, Settings, Tutorial) zur Laufzeit auf.
/// Dadurch bleiben bestehende Szenen, Prefabs und UI-Elemente unverändert.
/// </summary>
public static class RuntimeUIFactory
{
    public const int UILayer = 5;

    public static readonly Color Backdrop = new Color(0.012f, 0.018f, 0.035f, 1f);
    public static readonly Color Panel = new Color(0.035f, 0.060f, 0.110f, 0.98f);
    public static readonly Color PanelLight = new Color(0.060f, 0.100f, 0.170f, 1f);
    public static readonly Color Accent = new Color(0.20f, 0.88f, 1f, 1f);
    public static readonly Color AccentSoft = new Color(0.20f, 0.88f, 1f, 0.20f);
    public static readonly Color TextColor = new Color(0.91f, 0.96f, 1f, 1f);
    public static readonly Color TextMuted = new Color(0.60f, 0.70f, 0.80f, 1f);
    public static readonly Color Danger = new Color(1f, 0.42f, 0.38f, 1f);

    // =========================================================
    // CANVAS / EVENT SYSTEM
    // =========================================================

    public static Canvas CreateCanvas(string name, int sortingOrder)
    {
        GameObject go = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        go.layer = UILayer;

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        canvas.pixelPerfect = false;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    public static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        if (Object.FindAnyObjectByType<EventSystem>(
            FindObjectsInactive.Include
        ) != null)
        {
            return;
        }

        GameObject go = new GameObject("EventSystem", typeof(EventSystem));

        // Neues Input System verwenden, falls installiert.
        System.Type moduleType = System.Type.GetType(
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem"
        );

        if (moduleType != null)
            go.AddComponent(moduleType);
        else
            go.AddComponent<StandaloneInputModule>();
    }

    // =========================================================
    // RECTS
    // =========================================================

    public static RectTransform CreateRect(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = UILayer;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        return rect;
    }

    public static Image CreateImage(
        Transform parent,
        string name,
        Color color,
        bool raycast = false
    )
    {
        GameObject go = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Image)
        );

        go.layer = UILayer;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;

        return image;
    }

    public static Image CreateFullScreen(
        Transform parent,
        string name,
        Color color,
        bool raycast = false
    )
    {
        Image image = CreateImage(parent, name, color, raycast);
        Stretch(image.rectTransform);
        return image;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static void PlaceCentered(
        RectTransform rect,
        Vector2 size,
        Vector2 position
    )
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    public static void PlaceAnchored(
        RectTransform rect,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 size,
        Vector2 position
    )
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    // =========================================================
    // TEXT
    // =========================================================

    public static TextMeshProUGUI CreateText(
        Transform parent,
        string name,
        string content,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment,
        TMP_FontAsset font
    )
    {
        GameObject go = new GameObject(
            name,
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        go.layer = UILayer;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();

        if (font != null)
            text.font = font;

        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Overflow;

        return text;
    }

    // =========================================================
    // BUTTONS
    // =========================================================

    public static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 size,
        UnityAction onClick,
        TMP_FontAsset font,
        bool accent = false
    )
    {
        Image background = CreateImage(
            parent,
            name,
            accent ? Accent : PanelLight,
            true
        );

        background.rectTransform.sizeDelta = size;

        Button button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = accent
            ? new Color(0.72f, 1f, 1f, 1f)
            : new Color(0.72f, 0.86f, 0.96f, 1f);
        colors.pressedColor = new Color(0.55f, 0.78f, 0.9f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.32f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TextMeshProUGUI text = CreateText(
            background.transform,
            "Label",
            label,
            28f,
            accent ? new Color(0.02f, 0.07f, 0.10f, 1f) : TextColor,
            TextAlignmentOptions.Center,
            font
        );

        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(16f, 0f);
        text.rectTransform.offsetMax = new Vector2(-16f, 0f);

        if (onClick != null)
            button.onClick.AddListener(onClick);

        return button;
    }

    // =========================================================
    // SLIDER
    // =========================================================

    public static Slider CreateSlider(
        Transform parent,
        string name,
        float value,
        UnityAction<float> onChanged
    )
    {
        GameObject go = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Slider)
        );

        go.layer = UILayer;

        RectTransform root = go.GetComponent<RectTransform>();
        root.SetParent(parent, false);

        Image background = CreateImage(
            root,
            "Background",
            new Color(0.11f, 0.17f, 0.25f, 1f)
        );

        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(1f, 0.5f);
        backgroundRect.sizeDelta = new Vector2(0f, 16f);
        backgroundRect.anchoredPosition = Vector2.zero;

        RectTransform fillArea = CreateRect(root, "Fill Area");
        fillArea.anchorMin = new Vector2(0f, 0.5f);
        fillArea.anchorMax = new Vector2(1f, 0.5f);
        fillArea.sizeDelta = new Vector2(-16f, 16f);
        fillArea.anchoredPosition = Vector2.zero;

        Image fill = CreateImage(fillArea, "Fill", Accent);
        Stretch(fill.rectTransform);

        RectTransform handleArea = CreateRect(root, "Handle Slide Area");
        handleArea.anchorMin = new Vector2(0f, 0.5f);
        handleArea.anchorMax = new Vector2(1f, 0.5f);
        handleArea.sizeDelta = new Vector2(-22f, 0f);
        handleArea.anchoredPosition = Vector2.zero;

        Image handle = CreateImage(handleArea, "Handle", TextColor, true);
        handle.rectTransform.sizeDelta = new Vector2(22f, 34f);

        Slider slider = go.GetComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = Mathf.Clamp01(value);
        slider.onValueChanged.AddListener(onChanged);

        return slider;
    }

    // =========================================================
    // TOGGLE
    // =========================================================

    public static Toggle CreateToggle(
        Transform parent,
        string name,
        bool value,
        UnityAction<bool> onChanged
    )
    {
        Image background = CreateImage(
            parent,
            name,
            new Color(0.11f, 0.17f, 0.25f, 1f),
            true
        );

        background.rectTransform.sizeDelta = new Vector2(60f, 60f);

        Image checkmark = CreateImage(
            background.transform,
            "Checkmark",
            Accent
        );

        checkmark.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        checkmark.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        checkmark.rectTransform.sizeDelta = new Vector2(32f, 32f);
        checkmark.rectTransform.anchoredPosition = Vector2.zero;

        Toggle toggle = background.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = background;
        toggle.graphic = checkmark;
        toggle.isOn = value;
        toggle.onValueChanged.AddListener(onChanged);

        return toggle;
    }
}
