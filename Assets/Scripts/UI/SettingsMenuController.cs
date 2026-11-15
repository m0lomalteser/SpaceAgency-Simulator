using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Settings-Bildschirm als eigene Szene.
///
/// Wird sowohl vom Hauptmenü (bestehender SETTINGS-Button) als auch
/// aus dem Pause-Menü geöffnet. Alle Einstellungen werden in PlayerPrefs
/// gespeichert. Die UI wird zur Laufzeit erzeugt.
/// </summary>
public class SettingsMenuController : MonoBehaviour
{
    private const string VolumeKey = "Settings.MasterVolume";
    private const string FullscreenKey = "Settings.Fullscreen";
    private const string QualityKey = "Settings.Quality";

    [Header("Darstellung")]
    public TMP_FontAsset fontAsset;

    [Header("Szenen")]
    public string mainMenuScene = "MainMenu";

    private Slider volumeSlider;
    private TextMeshProUGUI volumeValueText;
    private Toggle fullscreenToggle;
    private TextMeshProUGUI qualityValueText;
    private TextMeshProUGUI feedbackText;

    private float openedAt;
    private bool closing;

    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void Awake()
    {
        RuntimeUIFactory.EnsureEventSystem();

        ApplySavedSettings();
        BuildUI();
    }

    private void Start()
    {
        openedAt = Time.unscaledTime;
    }

    private void Update()
    {
        if (closing)
            return;

        if (Time.unscaledTime - openedAt < 0.3f)
            return;

        if (OverlayInput.EscapePressed)
            Back();
    }

    // =========================================================
    // SETTINGS
    // =========================================================

    private void ApplySavedSettings()
    {
        float volume = PlayerPrefs.GetFloat(VolumeKey, 0.8f);

        AudioListener.volume = Mathf.Clamp01(volume);

        bool fullscreen = PlayerPrefs.GetInt(
            FullscreenKey,
            Screen.fullScreen ? 1 : 0
        ) != 0;

        Screen.fullScreen = fullscreen;

        if (QualitySettings.names.Length > 0)
        {
            int quality = Mathf.Clamp(
                PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel()),
                0,
                QualitySettings.names.Length - 1
            );

            QualitySettings.SetQualityLevel(quality, true);
        }
    }

    private void SetVolume(float value)
    {
        float clamped = Mathf.Clamp01(value);

        AudioListener.volume = clamped;

        PlayerPrefs.SetFloat(VolumeKey, clamped);
        PlayerPrefs.Save();

        if (volumeValueText != null)
            volumeValueText.text = Mathf.RoundToInt(clamped * 100f) + "%";
    }

    private void SetFullscreen(bool value)
    {
        Screen.fullScreen = value;

        PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ChangeQuality(int direction)
    {
        string[] names = QualitySettings.names;

        if (names == null || names.Length == 0)
            return;

        int index = Mathf.Clamp(
            QualitySettings.GetQualityLevel() + direction,
            0,
            names.Length - 1
        );

        QualitySettings.SetQualityLevel(index, true);

        PlayerPrefs.SetInt(QualityKey, index);
        PlayerPrefs.Save();

        UpdateQualityLabel();
    }

    private void UpdateQualityLabel()
    {
        if (qualityValueText == null)
            return;

        string[] names = QualitySettings.names;

        if (names == null || names.Length == 0)
        {
            qualityValueText.text = "-";
            return;
        }

        int index = Mathf.Clamp(
            QualitySettings.GetQualityLevel(),
            0,
            names.Length - 1
        );

        qualityValueText.text = names[index];
    }

    private void ReplayTutorial()
    {
        TutorialState.ResetTutorial();

        if (feedbackText != null)
        {
            feedbackText.text =
                "Tutorial enabled. It will start when you return to the game.";
        }

        bool pauseOpen = SceneManager.GetSceneByName("PauseMenu").isLoaded;
        bool gameOpen = SceneManager.GetSceneByName("Game").isLoaded;

        if (!pauseOpen &&
            gameOpen &&
            OverlayGameServices.Instance != null)
        {
            OverlayGameServices.Instance.ShowTutorialNow();
        }
    }

    // =========================================================
    // UI
    // =========================================================

    private void BuildUI()
    {
        Canvas canvas = RuntimeUIFactory.CreateCanvas("SettingsCanvas", 6100);

        RuntimeUIFactory.CreateFullScreen(
            canvas.transform,
            "Backdrop",
            RuntimeUIFactory.Backdrop,
            true
        );

        TextMeshProUGUI title = RuntimeUIFactory.CreateText(
            canvas.transform,
            "Title",
            "SETTINGS",
            58f,
            RuntimeUIFactory.Accent,
            TextAlignmentOptions.Center,
            fontAsset
        );

        title.fontStyle = FontStyles.Bold;

        RuntimeUIFactory.PlaceAnchored(
            title.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(900f, 70f),
            new Vector2(0f, -70f)
        );

        TextMeshProUGUI subtitle = RuntimeUIFactory.CreateText(
            canvas.transform,
            "Subtitle",
            "Audio, display and tutorial options.",
            22f,
            RuntimeUIFactory.TextMuted,
            TextAlignmentOptions.Center,
            fontAsset
        );

        RuntimeUIFactory.PlaceAnchored(
            subtitle.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(900f, 30f),
            new Vector2(0f, -136f)
        );

        Image border = RuntimeUIFactory.CreateImage(
            canvas.transform,
            "PanelBorder",
            new Color(
                RuntimeUIFactory.Accent.r,
                RuntimeUIFactory.Accent.g,
                RuntimeUIFactory.Accent.b,
                0.28f
            )
        );

        RuntimeUIFactory.PlaceCentered(
            border.rectTransform,
            new Vector2(944f, 704f),
            new Vector2(0f, -56f)
        );

        Image panel = RuntimeUIFactory.CreateImage(
            canvas.transform,
            "Panel",
            RuntimeUIFactory.Panel,
            true
        );

        RuntimeUIFactory.PlaceCentered(
            panel.rectTransform,
            new Vector2(932f, 692f),
            new Vector2(0f, -56f)
        );

        BuildVolumeRow(panel.transform, 232f);
        BuildFullscreenRow(panel.transform, 130f);
        BuildQualityRow(panel.transform, 28f);

        Image divider = RuntimeUIFactory.CreateImage(
            panel.transform,
            "Divider",
            new Color(0.20f, 0.88f, 1f, 0.25f)
        );

        RuntimeUIFactory.PlaceCentered(
            divider.rectTransform,
            new Vector2(820f, 2f),
            new Vector2(0f, -48f)
        );

        BuildTutorialRow(panel.transform, -122f);

        feedbackText = RuntimeUIFactory.CreateText(
            panel.transform,
            "Feedback",
            "",
            17f,
            RuntimeUIFactory.TextMuted,
            TextAlignmentOptions.Center,
            fontAsset
        );

        RuntimeUIFactory.PlaceCentered(
            feedbackText.rectTransform,
            new Vector2(820f, 30f),
            new Vector2(0f, -206f)
        );

        Button backButton = RuntimeUIFactory.CreateButton(
            panel.transform,
            "BackButton",
            "BACK",
            new Vector2(260f, 62f),
            Back,
            fontAsset,
            true
        );

        RuntimeUIFactory.PlaceCentered(
            backButton.GetComponent<RectTransform>(),
            new Vector2(260f, 62f),
            new Vector2(0f, -272f)
        );

        TextMeshProUGUI hint = RuntimeUIFactory.CreateText(
            canvas.transform,
            "Hint",
            "Settings are saved automatically.",
            17f,
            RuntimeUIFactory.TextMuted,
            TextAlignmentOptions.Center,
            fontAsset
        );

        RuntimeUIFactory.PlaceAnchored(
            hint.rectTransform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(900f, 26f),
            new Vector2(0f, 26f)
        );
    }

    private RectTransform CreateRow(
        Transform parent,
        string name,
        float y
    )
    {
        RectTransform row = RuntimeUIFactory.CreateRect(parent, name);

        RuntimeUIFactory.PlaceCentered(
            row,
            new Vector2(840f, 88f),
            new Vector2(0f, y)
        );

        return row;
    }

    private TextMeshProUGUI CreateRowLabel(
        Transform row,
        string label
    )
    {
        TextMeshProUGUI text = RuntimeUIFactory.CreateText(
            row,
            "Label",
            label,
            24f,
            RuntimeUIFactory.TextColor,
            TextAlignmentOptions.Left,
            fontAsset
        );

        RuntimeUIFactory.PlaceAnchored(
            text.rectTransform,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(380f, 40f),
            new Vector2(8f, 0f)
        );

        return text;
    }

    private void BuildVolumeRow(Transform parent, float y)
    {
        RectTransform row = CreateRow(parent, "VolumeRow", y);

        CreateRowLabel(row, "MASTER VOLUME");

        float volume = PlayerPrefs.GetFloat(VolumeKey, 0.8f);

        volumeSlider = RuntimeUIFactory.CreateSlider(
            row,
            "VolumeSlider",
            volume,
            SetVolume
        );

        RuntimeUIFactory.PlaceAnchored(
            volumeSlider.GetComponent<RectTransform>(),
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(320f, 40f),
            new Vector2(-72f, 0f)
        );

        volumeValueText = RuntimeUIFactory.CreateText(
            row,
            "VolumeValue",
            Mathf.RoundToInt(volume * 100f) + "%",
            22f,
            RuntimeUIFactory.Accent,
            TextAlignmentOptions.Right,
            fontAsset
        );

        RuntimeUIFactory.PlaceAnchored(
            volumeValueText.rectTransform,
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(62f, 40f),
            new Vector2(0f, 0f)
        );
    }

    private void BuildFullscreenRow(Transform parent, float y)
    {
        RectTransform row = CreateRow(parent, "FullscreenRow", y);

        CreateRowLabel(row, "FULLSCREEN");

        bool fullscreen = PlayerPrefs.GetInt(
            FullscreenKey,
            Screen.fullScreen ? 1 : 0
        ) != 0;

        fullscreenToggle = RuntimeUIFactory.CreateToggle(
            row,
            "FullscreenToggle",
            fullscreen,
            SetFullscreen
        );

        RuntimeUIFactory.PlaceAnchored(
            fullscreenToggle.GetComponent<RectTransform>(),
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(60f, 60f),
            new Vector2(-8f, 0f)
        );
    }

    private void BuildQualityRow(Transform parent, float y)
    {
        RectTransform row = CreateRow(parent, "QualityRow", y);

        CreateRowLabel(row, "GRAPHICS QUALITY");

        Button previousButton = RuntimeUIFactory.CreateButton(
            row,
            "QualityPrevious",
            "<",
            new Vector2(50f, 50f),
            () => ChangeQuality(-1),
            fontAsset
        );

        RuntimeUIFactory.PlaceAnchored(
            previousButton.GetComponent<RectTransform>(),
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(50f, 50f),
            new Vector2(-292f, 0f)
        );

        qualityValueText = RuntimeUIFactory.CreateText(
            row,
            "QualityValue",
            "-",
            21f,
            RuntimeUIFactory.Accent,
            TextAlignmentOptions.Center,
            fontAsset
        );

        RuntimeUIFactory.PlaceAnchored(
            qualityValueText.rectTransform,
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(220f, 40f),
            new Vector2(-66f, 0f)
        );

        Button nextButton = RuntimeUIFactory.CreateButton(
            row,
            "QualityNext",
            ">",
            new Vector2(50f, 50f),
            () => ChangeQuality(1),
            fontAsset
        );

        RuntimeUIFactory.PlaceAnchored(
            nextButton.GetComponent<RectTransform>(),
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(50f, 50f),
            new Vector2(-8f, 0f)
        );

        UpdateQualityLabel();
    }

    private void BuildTutorialRow(Transform parent, float y)
    {
        RectTransform row = CreateRow(parent, "TutorialRow", y);

        TextMeshProUGUI label = RuntimeUIFactory.CreateText(
            row,
            "Label",
            "SHOW TUTORIAL AGAIN",
            24f,
            RuntimeUIFactory.TextColor,
            TextAlignmentOptions.Left,
            fontAsset
        );

        RuntimeUIFactory.PlaceAnchored(
            label.rectTransform,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(520f, 32f),
            new Vector2(8f, 14f)
        );

        TextMeshProUGUI description = RuntimeUIFactory.CreateText(
            row,
            "Description",
            "Replays the first-start tutorial over the game scene.",
            18f,
            RuntimeUIFactory.TextMuted,
            TextAlignmentOptions.Left,
            fontAsset
        );

        RuntimeUIFactory.PlaceAnchored(
            description.rectTransform,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(560f, 28f),
            new Vector2(8f, -20f)
        );

        Button replayButton = RuntimeUIFactory.CreateButton(
            row,
            "ReplayButton",
            "REPLAY",
            new Vector2(200f, 54f),
            ReplayTutorial,
            fontAsset
        );

        RuntimeUIFactory.PlaceAnchored(
            replayButton.GetComponent<RectTransform>(),
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(200f, 54f),
            new Vector2(-8f, 0f)
        );
    }

    // =========================================================
    // NAVIGATION
    // =========================================================

    public void Back()
    {
        if (closing)
            return;

        closing = true;

        Scene pauseScene = SceneManager.GetSceneByName("PauseMenu");

        if (pauseScene.IsValid() && pauseScene.isLoaded)
        {
            Scene scene = gameObject.scene;

            if (scene.IsValid() && scene.isLoaded)
                SceneManager.UnloadSceneAsync(scene);

            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene, LoadSceneMode.Single);
    }
}
