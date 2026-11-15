using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Pause-Bildschirm als additive Overlay-Szene.
///
/// Wird per ESC (OverlayGameServices) geöffnet und kann auch direkt
/// über einen Button geladen werden. Die UI wird zur Laufzeit erzeugt,
/// damit keine bestehende Szene verändert werden muss.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("Darstellung")]
    public TMP_FontAsset fontAsset;

    [Header("Szenen")]
    public string mainMenuScene = "MainMenu";
    public string settingsScene = "Settings";

    [Header("Optionen")]
    public bool showReplayTutorial = true;

    private static bool isOpen;

    private Canvas canvas;
    private bool closing;
    private float openedAt;

    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void Awake()
    {
        RuntimeUIFactory.EnsureEventSystem();
        BuildUI();
    }

    public void Goon()
    {
        Start();
    }
    
    private void Start()
    {
        isOpen = true;

        if (Time.timeScale > 0f)
            OverlayGameServices.PrePauseTimeScale = Time.timeScale;

        Time.timeScale = 0f;
        openedAt = Time.unscaledTime;
    }

    private void OnDestroy()
    {
        isOpen = false;
    }

    private void Update()
    {
        if (closing)
            return;

        if (Time.unscaledTime - openedAt < 0.35f)
            return;

        // Das NASA-Briefing liegt über dem Pause-Menü und schließt mit ESC.
        if (NasaBriefingOverlay.Instance != null &&
            (NasaBriefingOverlay.Instance.IsVisible ||
             NasaBriefingOverlay.EscapeConsumed))
        {
            return;
        }

        if (OverlayInput.EscapePressed)
            Resume();
    }

    public static bool IsOpen => isOpen;

    // =========================================================
    // UI
    // =========================================================

    private void BuildUI()
    {
        canvas = RuntimeUIFactory.CreateCanvas("PauseCanvas", 6000);

        RuntimeUIFactory.CreateFullScreen(
            canvas.transform,
            "Backdrop",
            new Color(0f, 0f, 0f, 0.82f),
            true
        );

        Image border = RuntimeUIFactory.CreateImage(
            canvas.transform,
            "PanelBorder",
            new Color(
                RuntimeUIFactory.Accent.r,
                RuntimeUIFactory.Accent.g,
                RuntimeUIFactory.Accent.b,
                0.35f
            )
        );

        RuntimeUIFactory.PlaceCentered(
            border.rectTransform,
            new Vector2(668f, 690f),
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
            new Vector2(656f, 678f),
            Vector2.zero
        );

        TextMeshProUGUI title = RuntimeUIFactory.CreateText(
            panel.transform,
            "Title",
            "PAUSED",
            58f,
            Color.white,
            TextAlignmentOptions.Center,
            fontAsset
        );

        title.fontStyle = FontStyles.Bold;

        RuntimeUIFactory.PlaceAnchored(
            title.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(560f, 70f),
            new Vector2(0f, -48f)
        );

        TextMeshProUGUI subtitle = RuntimeUIFactory.CreateText(
            panel.transform,
            "Subtitle",
            "The clock keeps running. Take your time.",
            20f,
            Color.white,
            TextAlignmentOptions.Center,
            fontAsset
        );

        RuntimeUIFactory.PlaceAnchored(
            subtitle.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(560f, 30f),
            new Vector2(0f, -112f)
        );

        Image divider = RuntimeUIFactory.CreateImage(
            panel.transform,
            "Divider",
            new Color(0.20f, 0.88f, 1f, 0.35f)
        );

        RuntimeUIFactory.PlaceAnchored(
            divider.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(520f, 2f),
            new Vector2(0f, -148f)
        );

        float y = 150f;

        CreateMenuButton(panel.transform, "RESUME", y, Resume, true);
        y -= 78f;

        CreateMenuButton(
            panel.transform, "SETTINGS", y, OpenSettings, false);
        y -= 78f;

        CreateMenuButton(
            panel.transform,
            "NASA BRIEFING",
            y,
            OpenNasaBriefing,
            false
        );

        y -= 78f;

        bool canReplay =
            showReplayTutorial &&
            SceneManager.GetSceneByName("Game").isLoaded;

        if (canReplay)
        {
            CreateMenuButton(
                panel.transform,
                "REPLAY TUTORIAL",
                y,
                ReplayTutorial,
                false
            );

            y -= 78f;
        }

        CreateMenuButton(
            panel.transform, "MAIN MENU", y, ReturnToMainMenu, false);
        y -= 78f;

        CreateMenuButton(panel.transform, "QUIT GAME", y, QuitGame, false);

        TextMeshProUGUI hint = RuntimeUIFactory.CreateText(
            panel.transform,
            "Hint",
            "ESC to resume",
            17f,
            Color.white,
            TextAlignmentOptions.Center,
            fontAsset
        );

        RuntimeUIFactory.PlaceAnchored(
            hint.rectTransform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(500f, 26f),
            new Vector2(0f, 26f)
        );
    }

    private void CreateMenuButton(
        Transform parent,
        string label,
        float y,
        UnityEngine.Events.UnityAction action,
        bool accent
    )
    {
        Button button = RuntimeUIFactory.CreateButton(
            parent,
            label,
            label,
            new Vector2(460f, 64f),
            action,
            fontAsset,
            accent
        );

        RuntimeUIFactory.PlaceCentered(
            button.GetComponent<RectTransform>(),
            new Vector2(460f, 64f),
            new Vector2(0f, y)
        );
    }

    // =========================================================
    // ACTIONS
    // =========================================================

    public void Resume()
    {
        if (closing)
            return;

        closing = true;

        Time.timeScale = OverlayGameServices.PrePauseTimeScale <= 0f
            ? 1f
            : OverlayGameServices.PrePauseTimeScale;

        Scene scene = gameObject.scene;

        if (scene.IsValid() && scene.isLoaded)
            SceneManager.UnloadSceneAsync(scene);
    }

    public void OpenSettings()
    {
        if (closing)
            return;

        if (!Application.CanStreamedLevelBeLoaded(settingsScene))
        {
            Debug.LogError(
                "[PauseMenu] Szene '" + settingsScene +
                "' fehlt in den Build Settings."
            );

            return;
        }

        SceneManager.LoadScene(settingsScene, LoadSceneMode.Additive);
    }

    public void OpenNasaBriefing()
    {
        NasaBriefingOverlay overlay = NasaBriefingOverlay.Instance;

        if (overlay != null)
        {
            overlay.Show();
            return;
        }

        Debug.LogWarning(
            "[PauseMenu] NasaBriefingOverlay wurde nicht gefunden."
        );
    }

    public void ReplayTutorial()
    {
        if (closing)
            return;

        TutorialState.ResetTutorial();

        Resume();

        OverlayGameServices services = OverlayGameServices.Instance;

        if (services != null)
            services.ShowTutorialNow();
    }

    public void ReturnToMainMenu()
    {
        if (closing)
            return;

        closing = true;

        Time.timeScale = 1f;

        if (!Application.CanStreamedLevelBeLoaded(mainMenuScene))
        {
            Debug.LogError(
                "[PauseMenu] Szene '" + mainMenuScene +
                "' fehlt in den Build Settings."
            );

            return;
        }

        SceneManager.LoadScene(mainMenuScene, LoadSceneMode.Single);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
