using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class CustomizeSelectionUI : MonoBehaviour
{
    [Header("UI Referenzen (Direkt aus der Szene oder dem Prefab)")]
    [Tooltip("Ziehe hier das Haupt-Panel (das direkt unsichtbar sein soll) hinein.")]
    public GameObject panelRoot;
    
    [Tooltip("Ziehe hier deinen Cancel-/Schließen-Button hinein.")]
    public Button cancelButton;
    
    [Tooltip("Ziehe hier deinen Agency-Button hinein (optional).")]
    public Button agencyButton;
    
    [Tooltip("Ziehe hier deinen Rocket-Button hinein (optional).")]
    public Button rocketButton;

    [Header("Scene Settings")]
    public string rocketBuilderScene = "RocketBuilder";

    private void Awake()
    {
        // 1. Sofort beim Start sicherstellen, dass das Panel UNSICHTBAR ist!
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
        else
        {
            Debug.LogError("[CustomizeSelectionUI] Kein 'Panel Root' zugewiesen! Bitte im Inspector zuweisen.", this);
        }

        // 2. Buttons sicher per Code verknüpfen, falls sie nicht im Inspector verknüpft sind
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(Close);
            cancelButton.onClick.AddListener(Close);
        }
        else
        {
            Debug.LogWarning("[CustomizeSelectionUI] Kein 'Cancel Button' zugewiesen!");
        }

        if (agencyButton != null)
        {
            agencyButton.onClick.RemoveListener(ChooseAgency);
            agencyButton.onClick.AddListener(ChooseAgency);
        }

        if (rocketButton != null)
        {
            rocketButton.onClick.RemoveListener(ChooseRocket);
            rocketButton.onClick.AddListener(ChooseRocket);
        }
    }

    public void Open()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            Debug.Log("[CustomizeSelectionUI] Panel geöffnet.");
        }
    }

    public void Close()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
            Debug.Log("[CustomizeSelectionUI] Panel geschlossen (Cancel ausgeführt).");
        }
    }

    public void ChooseAgency()
    {
        Close();

        AutoFramingCamera framingCamera = FindFirstObjectByType<AutoFramingCamera>();
        if (framingCamera != null)
        {
            framingCamera.TriggerTransitionToModel1();
        }

        HideUIs hide = FindFirstObjectByType<HideUIs>();
        if (hide != null) hide.HideUI();
    }

    public void ChooseRocket()
    {
        Close();
        PlayerPrefs.SetString("RocketBuilder.ReturnScene", "Game");
        PlayerPrefs.Save();
        SceneManager.LoadScene(rocketBuilderScene);
    }
}