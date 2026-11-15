using System.Collections;
using UnityEngine;

public class SceneButtonControllers : MonoBehaviour
{
    [Header("Das Canvas / Panel, das nach 2 Sek. eingeblendet wird")]
    [SerializeField] private GameObject targetCanvas;

    public void TriggerCamera()
    {
        Debug.Log("[SceneButton] Button wurde geklickt!");

        // 1. Kamera-Logik ausführen
        AutoFramingCamera framingCamera = FindFirstObjectByType<AutoFramingCamera>();

        if (framingCamera != null)
        {
            Debug.Log("[SceneButton] Kamera gefunden! Triggere Fahrt zu Modell 3s...");
            framingCamera.TriggerTransitionToModel3();
        }
        else
        {
            Debug.LogError("[SceneButton] Keine AutoFramingCamera in der Szene gefunden!");
        }

        // 2. Canvas nach 2 Sekunden aktivieren (falls zugewiesen)
        if (targetCanvas != null)
        {
            StartCoroutine(ShowUIAfterDelay(2f));
        }
    }

    private IEnumerator ShowUIAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (targetCanvas != null)
        {
            targetCanvas.SetActive(true);
            Debug.Log("[SceneButton] Canvas AN geschaltet: " + targetCanvas.name);
        }
    }
}