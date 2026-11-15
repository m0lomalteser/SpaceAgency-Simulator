using System.Collections;
using UnityEngine;

public class SceneButtonControll : MonoBehaviour
{
    public void ShowMissionWindows()
    {
        Debug.Log("[SceneButton] ShowMissionWindows geklickt! Starte 3 Sekunden Timer...");
        StartCoroutine(ShowMissionWindowsDelayed(2f));
    }

    private IEnumerator ShowMissionWindowsDelayed(float delaySeconds)
    {
        // Wartet exakt die angegebene Zeit (hier 3 Sekunden)
        yield return new WaitForSeconds(delaySeconds);

        BookManager bookManager = FindFirstObjectByType<BookManager>();

        if (bookManager != null)
        {
            Debug.Log("[SceneButton] 3 Sekunden vorbei. BookManager gefunden! Öffne Mission-Fenster...");
            bookManager.ShowMissionWindows();
        }
        else
        {
            Debug.LogError("[SceneButton] Kein BookManager in der Szene gefunden!");
        }
    }
}