using System.Collections;
using UnityEngine;

public class SceneButtonControl : MonoBehaviour
{
    public void ShowMissionWindows()
    {
        Debug.Log("[SceneButton] Button geklickt! Starte 3 Sekunden Timer...");
        StartCoroutine(ShowMissionWindowsDelayed(1.5f));
    }

    private IEnumerator ShowMissionWindowsDelayed(float delaySeconds)
    {
        // Wartet exakt die angegebene Zeit (hier 3 Sekunden)
        yield return new WaitForSeconds(delaySeconds);

        // HideUI-Skript in der Szene suchen und ShowUI() aufrufen
        HideUIs hideUiScript = FindFirstObjectByType<HideUIs>();

        if (hideUiScript != null)
        {
            Debug.Log("[SceneButton] HideUI-Skript gefunden! Rufe ShowUI() auf...");
            hideUiScript.ShowUI();
        }
        else
        {
            Debug.LogWarning("[SceneButton] Kein HideUI-Skript in der Szene gefunden!");
        }
    }
}