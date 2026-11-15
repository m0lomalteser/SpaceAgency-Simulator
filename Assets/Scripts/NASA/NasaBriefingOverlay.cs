using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Zeigt echte NASA-Daten als Overlay: Astronomy Picture of the Day (APOD)
/// plus Anzahl der Near-Earth Objects des Tages.
///
/// Öffnen/Schließen mit F1, über den Button im Pause-Menü oder über den
/// Hinweis-Toast. Bestehende UI wird nicht verändert.
/// </summary>
[DefaultExecutionOrder(-450)]
public class NasaBriefingOverlay : MonoBehaviour
{
    public static NasaBriefingOverlay Instance { get; private set; }

    [Header("Darstellung")]
    public TMP_FontAsset fontAsset;

    public Color accentColor = new Color(0.20f, 0.88f, 1f, 1f);

    [Header("Verhalten")]
    [Tooltip("Sekunden, die der Hinweis-Toast sichtbar bleibt.")]
    public float toastDuration = 7f;

    [Tooltip("Beim Öffnen die Spielzeit anhalten.")]
    public bool pauseWhileOpen = true;

    private Canvas canvas;
    private GameObject root;

    private RawImage image;
    private AspectRatioFitter imageFitter;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI metaText;
    private TextMeshProUGUI bodyText;
    private TextMeshProUGUI neoText;
    private TextMeshProUGUI errorText;
    private TextMeshProUGUI loadingText;
    private TextMeshProUGUI imagePlaceholder;
    private Button openButton;

    private GameObject toastRoot;
    private TextMeshProUGUI toastSubtitle;
    private float toastUntil;
    private bool toastShown;

    private bool visible;
    private bool pausedByOverlay;
    private float previousTimeScale = 1f;

    private static int escapeConsumedFrame = -1;

    /// <summary>True, wenn das Overlay ESC in diesem Frame verarbeitet hat.</summary>
    public static bool EscapeConsumed =>
        escapeConsumedFrame == Time.frameCount;

    public bool IsVisible => visible;

    // =========================================================
    // BOOTSTRAP
    // =========================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        if (FindAnyObjectByType<NasaBriefingOverlay>() != null)
            return;

        new GameObject("NasaBriefing").AddComponent<NasaBriefingOverlay>();
    }

    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        RuntimeUIFactory.EnsureEventSystem();

        EnsureClient();

        BuildUI();
        BuildToast();

        root.SetActive(false);

        NasaApiClient client = NasaApiClient.Instance;

        if (client != null)
            client.Updated += HandleClientUpdated;
    }

    private void Start()
    {
        NasaApiClient client = NasaApiClient.Instance;

        if (client != null && client.HasAnyContent)
            HandleClientUpdated();
    }

    private void OnDestroy()
    {
        NasaApiClient client = NasaApiClient.Instance;

        if (client != null)
            client.Updated -= HandleClientUpdated;

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (visible && OverlayInput.EscapePressed)
        {
            escapeConsumedFrame = Time.frameCount;
            Hide();
            return;
        }

        if (OverlayInput.NasaPressed)
            Toggle();

        if (visible && EventSystem.current == null)
            RuntimeUIFactory.EnsureEventSystem();

        if (toastRoot != null &&
            toastRoot.activeSelf &&
            Time.unscaledTime >= toastUntil)
        {
            toastRoot.SetActive(false);
        }
    }

    private void EnsureClient()
    {
        if (NasaApiClient.Instance != null)
            return;

        if (FindAnyObjectByType<NasaApiClient>() != null)
            return;

        new GameObject("NasaApiClient").AddComponent<NasaApiClient>();
    }

    // =========================================================
    // SHOW / HIDE
    // =========================================================

    public void Toggle()
    {
        if (visible)
            Hide();
        else
            Show();
    }

    public void Show()
    {
        if (visible)
            return;

        visible = true;
        root.SetActive(true);

        if (pauseWhileOpen && Time.timeScale > 0f)
        {
            previousTimeScale = Time.timeScale;
            pausedByOverlay = true;
            Time.timeScale = 0f;
        }
        else
        {
            pausedByOverlay = false;
        }

        ApplyData();

        NasaApiClient client = NasaApiClient.Instance;

        if (client != null && !client.HasAnyContent && !client.IsLoading)
            client.Refresh();
    }

    public void Hide()
    {
        if (!visible)
            return;

        visible = false;
        root.SetActive(false);

        if (pausedByOverlay)
        {
            Time.timeScale = previousTimeScale <= 0f
                ? 1f
                : previousTimeScale;

            pausedByOverlay = false;
        }
    }

    // =========================================================
    // DATA
    // =========================================================

    private void HandleClientUpdated()
    {
        if (visible)
            ApplyData();

        NasaApiClient client = NasaApiClient.Instance;

        if (!toastShown && client != null && client.HasAnyContent)
        {
            toastShown = true;
            ShowToast();
        }
    }

    private void ApplyData()
    {
        NasaApiClient client = NasaApiClient.Instance;

        if (client == null)
            return;

        titleText.text = client.ActiveTitle;
        metaText.text = client.ActiveMeta;
        bodyText.text = client.ActiveBody;

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            bodyText.rectTransform
        );

        Texture2D texture = client.ActiveTexture;

        image.texture = texture;
        image.gameObject.SetActive(texture != null);

        if (texture != null)
        {
            imageFitter.aspectRatio =
                (float)texture.width / Mathf.Max(1, texture.height);
        }

        if (texture != null)
        {
            imagePlaceholder.text = "";
        }
        else if (client.ActiveIsVideo)
        {
            imagePlaceholder.text = "VIDEO · OPEN IN BROWSER";
        }
        else
        {
            imagePlaceholder.text = client.HasAnyContent
                ? ""
                : "IMAGE UNAVAILABLE";
        }

        neoText.text = client.NearEarthObjectCount >= 0
            ? "NEAR-EARTH OBJECTS TODAY: " + client.NearEarthObjectCount
            : "NEAR-EARTH OBJECTS TODAY: --";

        errorText.text = string.IsNullOrEmpty(client.LastError)
            ? ""
            : client.LastError;

        loadingText.gameObject.SetActive(client.IsLoading);

        openButton.interactable = client.HasAnyContent;
    }

    // =========================================================
    // UI
    // =========================================================

    private void BuildUI()
    {
        canvas = RuntimeUIFactory.CreateCanvas("NasaBriefingCanvas", 8000);
        root = canvas.gameObject;

        DontDestroyOnLoad(root);

        RuntimeUIFactory.CreateFullScreen(
            canvas.transform,
            "Backdrop",
            new Color(0f, 0f, 0f, 0.90f),
            true
        );

        Image border = RuntimeUIFactory.CreateImage(
            canvas.transform,
            "PanelBorder",
            new Color(accentColor.r, accentColor.g, accentColor.b, 0.30f)
        );

        RuntimeUIFactory.PlaceCentered(
            border.rectTransform,
            new Vector2(1568f, 908f),
            Vector2.zero
        );

        Image panel = RuntimeUIFactory.CreateImage(
            canvas.transform,
            "Panel",
            RuntimeUIFactory.Panel,
            true
        );

        RuntimeUIFactory.PlaceCentered(
            panel.rectTransform,
            new Vector2(1560f, 900f),
            Vector2.zero
        );

        TextMeshProUGUI kicker = RuntimeUIFactory.CreateText(
            panel.transform,
            "Kicker",
            "NASA · ASTRONOMY PICTURE OF THE DAY",
            18f,
            Color.white,
            TextAlignmentOptions.Left,
            fontAsset
        );

        RuntimeUIFactory.PlaceAnchored(
            kicker.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(1000f, 26f),
            new Vector2(56f, -38f)
        );

        titleText = RuntimeUIFactory.CreateText(
            panel.transform,
            "Title",
            "NASA BRIEFING",
            40f,
            Color.white,
            TextAlignmentOptions.Left,
            fontAsset
        );

        titleText.fontStyle = FontStyles.Bold;

        RuntimeUIFactory.PlaceAnchored(
            titleText.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(1448f, 58f),
            new Vector2(56f, -72f)
        );

        metaText = RuntimeUIFactory.CreateText(
            panel.transform,
            "Meta",
            "Astronomy Picture of the Day",
            19f,
            Color.white,
            TextAlignmentOptions.Left,
            fontAsset
        );

        RuntimeUIFactory.PlaceAnchored(
            metaText.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(1448f, 26f),
            new Vector2(56f, -138f)
        );

        errorText = RuntimeUIFactory.CreateText(
            panel.transform,
            "Error",
            "",
            17f,
            RuntimeUIFactory.Danger,
            TextAlignmentOptions.Left,
            fontAsset
        );

        RuntimeUIFactory.PlaceAnchored(
            errorText.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(1448f, 26f),
            new Vector2(56f, -168f)
        );

        BuildImageArea(panel.transform);
        BuildTextArea(panel.transform);
        BuildFooter(panel.transform);
    }

    private void BuildImageArea(Transform parent)
    {
        Image frame = RuntimeUIFactory.CreateImage(
            parent,
            "ImageFrame",
            new Color(0.02f, 0.035f, 0.065f, 1f)
        );

        RuntimeUIFactory.PlaceCentered(
            frame.rectTransform,
            new Vector2(720f, 560f),
            new Vector2(-368f, -24f)
        );

        GameObject rawGo = new GameObject(
            "ApodImage",
            typeof(RectTransform),
            typeof(RawImage),
            typeof(AspectRatioFitter)
        );

        rawGo.layer = RuntimeUIFactory.UILayer;

        RectTransform rawRect = rawGo.GetComponent<RectTransform>();
        rawRect.SetParent(frame.transform, false);

        rawRect.anchorMin = Vector2.zero;
        rawRect.anchorMax = Vector2.one;
        rawRect.offsetMin = new Vector2(8f, 8f);
        rawRect.offsetMax = new Vector2(-8f, -8f);

        image = rawGo.GetComponent<RawImage>();
        image.color = Color.white;
        image.raycastTarget = false;

        imageFitter = rawGo.GetComponent<AspectRatioFitter>();
        imageFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        imageFitter.aspectRatio = 1f;

        imagePlaceholder = RuntimeUIFactory.CreateText(
            frame.transform,
            "Placeholder",
            "IMAGE UNAVAILABLE",
            22f,
            Color.white,
            TextAlignmentOptions.Center,
            fontAsset
        );

        RuntimeUIFactory.Stretch(imagePlaceholder.rectTransform);
        imagePlaceholder.rectTransform.offsetMin = new Vector2(20f, 20f);
        imagePlaceholder.rectTransform.offsetMax = new Vector2(-20f, -20f);
    }

    private void BuildTextArea(Transform parent)
    {
        RectTransform scrollView = RuntimeUIFactory.CreateRect(
            parent,
            "ExplanationScroll"
        );

        RuntimeUIFactory.PlaceCentered(
            scrollView,
            new Vector2(700f, 560f),
            new Vector2(382f, -24f)
        );

        RectTransform viewport = RuntimeUIFactory.CreateRect(
            scrollView,
            "Viewport"
        );

        RuntimeUIFactory.Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();

        // Unsichtbare Fläche, damit Mausrad/Ziehen den ScrollRect erreicht.
        Image scrollHitArea = viewport.gameObject.AddComponent<Image>();
        scrollHitArea.color = new Color(0f, 0f, 0f, 0.001f);
        scrollHitArea.raycastTarget = true;

        bodyText = RuntimeUIFactory.CreateText(
            viewport,
            "Explanation",
            "",
            22f,
            Color.white,
            TextAlignmentOptions.TopLeft,
            fontAsset
        );

        RectTransform bodyRect = bodyText.rectTransform;

        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.anchoredPosition = Vector2.zero;
        bodyRect.sizeDelta = new Vector2(0f, 100f);

        ContentSizeFitter fitter =
            bodyText.gameObject.AddComponent<ContentSizeFitter>();

        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = scrollView.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = bodyRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 45f;
        scroll.inertia = true;
        scroll.decelerationRate = 0.12f;

        loadingText = RuntimeUIFactory.CreateText(
            parent,
            "Loading",
            "UPDATING FROM NASA ...",
            18f,
            Color.white,
            TextAlignmentOptions.Center,
            fontAsset
        );

        RuntimeUIFactory.PlaceAnchored(
            loadingText.rectTransform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(600f, 28f),
            new Vector2(0f, 112f)
        );
    }

    private void BuildFooter(Transform parent)
    {
        neoText = RuntimeUIFactory.CreateText(
            parent,
            "NeoCount",
            "NEAR-EARTH OBJECTS TODAY: --",
            18f,
            Color.white,
            TextAlignmentOptions.Left,
            fontAsset
        );

        RuntimeUIFactory.PlaceAnchored(
            neoText.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(760f, 30f),
            new Vector2(56f, 44f)
        );

        Button closeButton = RuntimeUIFactory.CreateButton(
            parent,
            "CloseButton",
            "CLOSE",
            new Vector2(160f, 52f),
            Hide,
            fontAsset
        );

        RuntimeUIFactory.PlaceAnchored(
            closeButton.GetComponent<RectTransform>(),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(160f, 52f),
            new Vector2(-56f, 44f)
        );

        Button refreshButton = RuntimeUIFactory.CreateButton(
            parent,
            "RefreshButton",
            "REFRESH",
            new Vector2(160f, 52f),
            Refresh,
            fontAsset
        );

        RuntimeUIFactory.PlaceAnchored(
            refreshButton.GetComponent<RectTransform>(),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(160f, 52f),
            new Vector2(-232f, 44f)
        );

        openButton = RuntimeUIFactory.CreateButton(
            parent,
            "OpenButton",
            "OPEN IN BROWSER",
            new Vector2(230f, 52f),
            OpenInBrowser,
            fontAsset,
            true
        );

        RuntimeUIFactory.PlaceAnchored(
            openButton.GetComponent<RectTransform>(),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(230f, 52f),
            new Vector2(-408f, 44f)
        );

        TextMeshProUGUI hint = RuntimeUIFactory.CreateText(
            parent,
            "Hint",
            "F1 closes this briefing  ·  data: api.nasa.gov",
            17f,
            Color.white,
            TextAlignmentOptions.Center,
            fontAsset
        );

        RuntimeUIFactory.PlaceAnchored(
            hint.rectTransform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(700f, 24f),
            new Vector2(0f, 14f)
        );
    }

    // =========================================================
    // TOAST
    // =========================================================

    private void BuildToast()
    {
        Canvas toastCanvas = RuntimeUIFactory.CreateCanvas(
            "NasaToastCanvas",
            9000
        );

        DontDestroyOnLoad(toastCanvas.gameObject);

        Image background = RuntimeUIFactory.CreateImage(
            toastCanvas.transform,
            "Toast",
            RuntimeUIFactory.Panel,
            true
        );

        toastRoot = background.gameObject;

        RuntimeUIFactory.PlaceAnchored(
            background.rectTransform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(500f, 84f),
            new Vector2(-36f, -36f)
        );

        Button button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(Show);

        Image bar = RuntimeUIFactory.CreateImage(
            background.transform,
            "AccentBar",
            accentColor
        );

        bar.rectTransform.anchorMin = new Vector2(0f, 0f);
        bar.rectTransform.anchorMax = new Vector2(0f, 1f);
        bar.rectTransform.pivot = new Vector2(0f, 0.5f);
        bar.rectTransform.sizeDelta = new Vector2(5f, 0f);
        bar.rectTransform.anchoredPosition = Vector2.zero;

        TextMeshProUGUI title = RuntimeUIFactory.CreateText(
            background.transform,
            "Title",
            "NASA BRIEFING READY",
            18f,
            Color.white,
            TextAlignmentOptions.Left,
            fontAsset
        );

        title.fontStyle = FontStyles.Bold;

        RuntimeUIFactory.PlaceAnchored(
            title.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(440f, 26f),
            new Vector2(22f, -16f)
        );

        toastSubtitle = RuntimeUIFactory.CreateText(
            background.transform,
            "Subtitle",
            "Press F1 for today's APOD",
            17f,
            Color.white,
            TextAlignmentOptions.Left,
            fontAsset
        );

        RuntimeUIFactory.PlaceAnchored(
            toastSubtitle.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(440f, 24f),
            new Vector2(22f, -46f)
        );

        toastRoot.SetActive(false);
    }

    private void ShowToast()
    {
        if (toastRoot == null)
            return;

        NasaApiClient client = NasaApiClient.Instance;

        if (client != null && client.HasAnyContent)
            toastSubtitle.text = "Press F1 for today's NASA briefing";

        toastRoot.SetActive(true);
        toastUntil = Time.unscaledTime + Mathf.Max(2f, toastDuration);
    }

    // =========================================================
    // ACTIONS
    // =========================================================

    private void Refresh()
    {
        NasaApiClient client = NasaApiClient.Instance;

        if (client != null)
            client.Refresh();
    }

    private void OpenInBrowser()
    {
        NasaApiClient client = NasaApiClient.Instance;

        string url = client != null
            ? client.ActiveUrl
            : "https://apod.nasa.gov/apod/astropix.html";

        Application.OpenURL(url);
    }
}
