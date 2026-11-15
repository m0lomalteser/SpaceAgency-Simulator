using UnityEngine;

public class SceneButtonControllerss : MonoBehaviour
{
    private CustomizeSelectionUI selectionUI;

    public void TriggerCamera()
    {
        OpenCustomizeSelection();
    }

    public void OpenCustomizeSelection()
    {
        if (selectionUI == null)
        {
            selectionUI = FindFirstObjectByType<CustomizeSelectionUI>();
            if (selectionUI == null)
            {
                GameObject host = gameObject;
                selectionUI = host.GetComponent<CustomizeSelectionUI>();
                if (selectionUI == null)
                {
                    selectionUI = host.AddComponent<CustomizeSelectionUI>();
                }
            }
        }

        selectionUI.Open();
    }
}
