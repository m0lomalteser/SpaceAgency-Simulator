using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PersistentLaunchManager : MonoBehaviour
{
    public static PersistentLaunchManager Instance { get; private set; }

    private bool shouldLaunchOnStart = false;
    private bool cachedIsTutorial = false; // Speichert, ob es ein Tutorial-Launch ist

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Lädt die Szene und startet den Launch. 
    /// isTutorial = true verhindert Explosionen beim ersten Mal.
    /// </summary>
    public void LoadGameAndLaunch(string sceneName, bool isTutorial = false)
    {
        shouldLaunchOnStart = true;
        cachedIsTutorial = isTutorial;
        SceneManager.LoadScene(sceneName);
        
        StartCoroutine(TriggerLaunchWhenReady());
    }

    private IEnumerator TriggerLaunchWhenReady()
    {
        // Kurzer Puffer, bis die Szene und alle GameObjects geladen sind
        yield return new WaitForSeconds(0.2f);

        if (shouldLaunchOnStart)
        {
            shouldLaunchOnStart = false;

            Launch launchScript = FindFirstObjectByType<Launch>();
            if (launchScript != null)
            {
                // Hier übergeben wir jetzt den Parameter an LaunchRocket(bool)
                launchScript.LaunchRocket(cachedIsTutorial);
                Debug.Log($"[PersistentLaunchManager] Launch in der neuen Szene getriggert (Tutorial: {cachedIsTutorial})!");
            }
            else
            {
                Debug.LogWarning("[PersistentLaunchManager] Kein 'Launch'-Skript in der neuen Szene gefunden!");
            }
        }
    }
}