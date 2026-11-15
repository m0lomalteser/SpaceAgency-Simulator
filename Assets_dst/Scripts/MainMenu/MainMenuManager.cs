using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // Fields for Unity Editor
    [Header("Buttons")]
    [SerializeField] private Button newCampaignButton;
    [SerializeField] private Button continueMissionButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    [Header("Scene Names")]
    [Tooltip("Name der Szene für eine neue Kampagne")]
    [SerializeField] private string newCampaignSceneName;

    [Tooltip("Name der Szene für das Fortsetzen der Mission")]
    [SerializeField] private string continueMissionSceneName;

    [Tooltip("Name der Szene für die Einstellungen")]
    [SerializeField] private string settingsSceneName;

    private void Awake()
    {
        if (newCampaignButton != null)
            newCampaignButton.onClick.AddListener(OnNewCampaignClicked);

        if (continueMissionButton != null)
            continueMissionButton.onClick.AddListener(OnContinueMissionClicked);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);
    }

    private void OnDestroy()
    {
        if (newCampaignButton != null)
            newCampaignButton.onClick.RemoveListener(OnNewCampaignClicked);

        if (continueMissionButton != null)
            continueMissionButton.onClick.RemoveListener(OnContinueMissionClicked);

        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OnSettingsClicked);

        if (exitButton != null)
            exitButton.onClick.RemoveListener(OnExitClicked);
    }

    public void OnNewCampaignClicked()
    {
        LoadScene(newCampaignSceneName);
    }

    public void OnContinueMissionClicked()
    {
        LoadScene(continueMissionSceneName);
    }

    public void OnSettingsClicked()
    {
        LoadScene(settingsSceneName);
    }

    public void OnExitClicked()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private void LoadScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning($"[MainMenuManager] Kein Szenenname für diese Aktion eingetragen!");
        }
    }
}