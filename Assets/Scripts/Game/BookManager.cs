using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class BookManager : MonoBehaviour
{
    [Header("Canvas Windows")]
    [SerializeField] private GameObject canvasPage1;
    [SerializeField] private GameObject canvasPage2;

    [Header("Page 1 Buttons")]
    [SerializeField] private Button page1LeftButton;
    [SerializeField] private Button page1RightButton;

    [Header("Page 2 Buttons")]
    [SerializeField] private Button page2LeftButton;
    [SerializeField] private Button page2RightButton;

    [Header("Close Button")]
    [SerializeField] private Button closeButton;
    public UnityEvent ShowUI;
    public UnityEvent TriggerTransitionToModel1;

    private void Start()
    {
        if (page1RightButton != null) page1RightButton.onClick.AddListener(ShowPage2);
        if (page2LeftButton != null) page2LeftButton.onClick.AddListener(ShowPage1);
        if (closeButton != null) closeButton.onClick.AddListener(CloseBook);
        if (canvasPage1 != null) canvasPage1.SetActive(false);
        if (canvasPage2 != null) canvasPage2.SetActive(false);
    }

    public void ShowMissionWindows()
    {
        ShowPage1();
    }

    public void ShowPage1()
    {
        if (canvasPage1 != null) canvasPage1.SetActive(true);
        if (canvasPage2 != null) canvasPage2.SetActive(false);

        SetButtonState(page1LeftButton, false);
        SetButtonState(page1RightButton, true);
    }

    public void ShowPage2()
    {
        if (canvasPage1 != null) canvasPage1.SetActive(false);
        if (canvasPage2 != null) canvasPage2.SetActive(true);

        SetButtonState(page2LeftButton, true);
        SetButtonState(page2RightButton, false);
    }

    public void CloseBook()
    {
        if (canvasPage1 != null) canvasPage1.SetActive(false);
        if (canvasPage2 != null) canvasPage2.SetActive(false);
        ShowUI?.Invoke();
        TriggerTransitionToModel1?.Invoke();
    }

    private void SetButtonState(Button btn, bool interactable)
    {
        if (btn != null)
        {
            btn.interactable = interactable;
        }
    }
}