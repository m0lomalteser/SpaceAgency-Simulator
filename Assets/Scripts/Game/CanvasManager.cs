
using UnityEngine;
using UnityEngine.UI;

public class CanvasManager : MonoBehaviour
{
    [Header("UI Elemente")]
    [SerializeField] private GameObject windowToClose;
    [SerializeField] private Button closeButton;

    [Header("Referenz auf HideUIs")]
    [SerializeField] private HideUIs hideUIsController;

    private void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseWindow);
        }
        else
        {
            Debug.LogWarning(
                "Kein Schließen-Button zugewiesen!", this
            );
        }
    }

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseWindow);
        }
    }

    public void CloseWindow()
    {
        if (windowToClose == null)
        {
            Debug.LogWarning(
                "Kein Fenster zum Schließen zugewiesen!", this
            );
            return;
        }

        windowToClose.SetActive(false);

        if (hideUIsController != null)
        {
            hideUIsController.ShowUI();
        }
        else
        {
            Debug.LogWarning(
                "HideUIs-Controller fehlt im Inspector!", this
            );
        }
    }
}
