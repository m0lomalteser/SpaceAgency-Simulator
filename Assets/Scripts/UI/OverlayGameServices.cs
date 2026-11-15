using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistenter Overlay-Dienst (wird automatisch beim Spielstart erzeugt):
///
/// - ESC öffnet den Pause-Bildschirm in Gameplay-Szenen
/// - zeigt das Erststart-Tutorial beim ersten Laden der Game-Szene
/// - hält die Spielzeit beim Pausieren an und stellt sie danach wieder her
///
/// Es müssen keine Szenen oder UI-Elemente angepasst werden.
/// </summary>
[DefaultExecutionOrder(-500)]
public class OverlayGameServices : MonoBehaviour
{
    public static OverlayGameServices Instance { get; private set; }

    /// <summary>Time.timeScale, die vor dem Pausieren aktiv war.</summary>
    public static float PrePauseTimeScale { get; set; } = 1f;

    public static bool TutorialIsOpen =>
        IsSceneLoaded("TutorialOverlay");

    [Header("Szenen-Namen")]
    public string pauseSceneName = "PauseMenu";
    public string tutorialSceneName = "TutorialOverlay";
    public string gameSceneName = "Game";

    [Tooltip("In diesen Szenen kann mit ESC pausiert werden.")]
    public string[] pauseAllowedScenes =
    {
        "Game",
        "RocketBuilder",
        "RocketBuilderForMission",
        "Mission Selection Scene"
    };

    [Header("Tutorial")]
    [Tooltip("Tutorial beim ersten Start der Game-Szene automatisch zeigen.")]
    public bool showTutorialOnFirstStart = true;

    [Tooltip("Kurze Verzögerung, damit die Game-Szene fertig laden kann.")]
    public float tutorialStartDelay = 0.9f;

    private bool pauseLoading;
    private bool tutorialLoading;

    // =========================================================
    // BOOTSTRAP
    // =========================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        if (Object.FindAnyObjectByType<OverlayGameServices>() != null)
            return;

        GameObject go = new GameObject("OverlayGameServices");
        go.AddComponent<OverlayGameServices>();
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

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        HandlePauseInput();
        HandleFirstStartTutorial();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Sicherheitsnetz: Nach einem Menüwechsel darf die Zeit nie stehen bleiben.
        if (scene.name == "MainMenu" ||
            scene.name == "LoadingScreen" ||
            scene.name == "Setup" ||
            scene.name == "SetupChanged")
        {
            Time.timeScale = 1f;
        }
    }

    // =========================================================
    // PAUSE
    // =========================================================

    private void HandlePauseInput()
    {
        if (!OverlayInput.EscapePressed)
            return;

        if (!IsPausableScene(SceneManager.GetActiveScene().name))
            return;

        if (pauseLoading)
            return;

        if (IsSceneLoaded(pauseSceneName))
            return;

        if (IsSceneLoaded(tutorialSceneName))
            return;

        OpenPauseMenu();
    }

    public void OpenPauseMenu()
    {
        if (pauseLoading || IsSceneLoaded(pauseSceneName))
            return;

        if (!Application.CanStreamedLevelBeLoaded(pauseSceneName))
        {
            Debug.LogError(
                "[OverlayGameServices] Szene '" + pauseSceneName +
                "' fehlt in den Build Settings."
            );

            return;
        }

        StartCoroutine(OpenPauseRoutine());
    }

    private IEnumerator OpenPauseRoutine()
    {
        pauseLoading = true;

        Scene host = SceneManager.GetActiveScene();

        PrePauseTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
        Time.timeScale = 0f;

        yield return SceneManager.LoadSceneAsync(
            pauseSceneName,
            LoadSceneMode.Additive
        );

        // Additive Szenen dürfen die aktive Szene nicht übernehmen.
        if (host.IsValid() && host.isLoaded)
            SceneManager.SetActiveScene(host);

        pauseLoading = false;
    }

    public void ResumeGame()
    {
        Time.timeScale = PrePauseTimeScale <= 0f ? 1f : PrePauseTimeScale;

        if (IsSceneLoaded(pauseSceneName))
        {
            SceneManager.UnloadSceneAsync(pauseSceneName);
        }
    }

    public bool IsPausableScene(string sceneName)
    {
        if (pauseAllowedScenes == null)
            return false;

        foreach (string allowed in pauseAllowedScenes)
        {
            if (allowed == sceneName)
                return true;
        }

        return false;
    }

    // =========================================================
    // TUTORIAL
    // =========================================================

    private void HandleFirstStartTutorial()
    {
        if (!showTutorialOnFirstStart)
            return;

        if (TutorialState.IsCompleted)
            return;

        if (tutorialLoading)
            return;

        if (IsSceneLoaded(tutorialSceneName))
            return;

        if (IsSceneLoaded(pauseSceneName))
            return;

        // Tutorial nur über der eigentlichen Spielszene starten.
        if (!IsSceneLoaded(gameSceneName))
            return;

        if (!Application.CanStreamedLevelBeLoaded(tutorialSceneName))
        {
            Debug.LogError(
                "[OverlayGameServices] Szene '" + tutorialSceneName +
                "' fehlt in den Build Settings."
            );

            return;
        }

        StartCoroutine(LoadTutorialRoutine());
    }

    /// <summary>Startet das Tutorial sofort (z. B. für einen Replay-Button).</summary>
    public void ShowTutorialNow()
    {
        TutorialState.ResetTutorial();

        if (tutorialLoading || IsSceneLoaded(tutorialSceneName))
            return;

        if (!Application.CanStreamedLevelBeLoaded(tutorialSceneName))
            return;

        StartCoroutine(LoadTutorialRoutine());
    }

    private IEnumerator LoadTutorialRoutine()
    {
        tutorialLoading = true;

        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, tutorialStartDelay)
        );

        if (TutorialState.IsCompleted || IsSceneLoaded(tutorialSceneName))
        {
            tutorialLoading = false;
            yield break;
        }

        yield return SceneManager.LoadSceneAsync(
            tutorialSceneName,
            LoadSceneMode.Additive
        );

        // Game-Szene bleibt die aktive Szene.
        Scene host = SceneManager.GetSceneByName(gameSceneName);

        if (host.IsValid() && host.isLoaded)
            SceneManager.SetActiveScene(host);

        tutorialLoading = false;
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static bool IsSceneLoaded(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);

        return scene.IsValid() && scene.isLoaded;
    }
}
