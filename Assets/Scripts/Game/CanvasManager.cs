using UnityEngine;
using UnityEngine.UI;

public class CanvasManager : MonoBehaviour
{
    [Header("UI Elemente")]
    [Tooltip("Das UI-Fenster, das geschlossen werden soll.")]
    [SerializeField] private GameObject windowToClose;

    [Tooltip("Der Button, der das Fenster schließt.")]
    [SerializeField] private Button closeButton;

    [Header("Referenz auf das HideUIs-Skript")]
    [SerializeField] private HideUIs hideUIsController;

    private void Start()
    {
        // Sicherstellen, dass der Button zugewiesen ist
        if (closeButton != null)
        {
            // Listener hinzufügen, der die Schließen-Methode aufruft, wenn geklickt wird
            closeButton.onClick.AddListener(CloseWindow);
        }
        else
        {
            Debug.LogWarning("Kein Schließen-Button im CanvasManager zugewiesen!", this);
        }
    }

    private void OnDestroy()
    {
        // Speicherbereinigung: Listener wieder entfernen, wenn das Objekt zerstört wird
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseWindow);
        }
    }

    /// <summary>
    /// Schließt das zugewiesene Fenster (schaltet das GameObject ab).
    /// </summary>
    public void CloseWindow()
    {
        if (windowToClose != null)
        {
            windowToClose.SetActive(false);
            hideUIsController.ShowUI();
        }
        else
        {
            Debug.LogWarning("Es ist kein Fenster zum Schließen zugewiesen!", this);
        }
    }
}