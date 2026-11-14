using UnityEngine;

public class SceneButtonControllerss : MonoBehaviour
{
    public void TriggerCamera()
    {
        Debug.Log("[SceneButton] Button wurde geklickt!");

        AutoFramingCamera framingCamera = FindFirstObjectByType<AutoFramingCamera>();

        if (framingCamera != null)
        {
            Debug.Log("[SceneButton] Kamera gefunden! Triggere Fahrt zu Modell 1...");
            framingCamera.TriggerTransitionToModel1();
        }
        else
        {
            Debug.LogError("[SceneButton] Keine AutoFramingCamera in der Szene gefunden!");
        }
    }
}