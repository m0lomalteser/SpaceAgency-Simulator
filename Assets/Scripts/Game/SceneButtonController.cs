using UnityEngine;

public class SceneButtonController : MonoBehaviour
{
    public void TriggerCamera()
    {
        Debug.Log("[SceneButton] Button wurde geklickt!");

        AutoFramingCamera framingCamera = FindFirstObjectByType<AutoFramingCamera>();

        if (framingCamera != null)
        {
            Debug.Log("[SceneButton] Kamera gefunden! Triggere Fahrt zu Modell 2...");
            framingCamera.TriggerTransitionToModel2();
        }
        else
        {
            Debug.LogError("[SceneButton] Keine AutoFramingCamera in der Szene gefunden!");
        }
    }
}