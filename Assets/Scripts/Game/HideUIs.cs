using System.Collections;
using UnityEngine;

public class HideUIs : MonoBehaviour
{
    [Header("Das Canvas / Panel")]
    [SerializeField] private GameObject targetCanvas;

    public void ShowUI()
    {
        StartCoroutine(ShowUIAfterDelay(2f));
    }

    private IEnumerator ShowUIAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (targetCanvas != null)
        {
            targetCanvas.SetActive(true);
            Debug.Log("[CanvasController] Canvas AN geschaltet: " + targetCanvas.name);
        }
    }

    public void HideUI()
    {
        if (targetCanvas != null)
        {
            targetCanvas.SetActive(false);
            Debug.Log("[CanvasController] Canvas AUS geschaltet: " + targetCanvas.name);
        }
    }
}