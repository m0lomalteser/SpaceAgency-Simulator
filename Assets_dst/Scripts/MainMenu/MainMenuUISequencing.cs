using System.Collections;
using UnityEngine;

public class MainMenuUISequencer : MonoBehaviour
{
    [System.Serializable]
    public class UIElement
    {
        public CanvasGroup canvasGroup;
        public RectTransform rectTransform; // Optional für Slide-In Effekt
    }

    [Header("UI Elements (in der Reihenfolge des Aufbaus)")]
    [SerializeField] private UIElement[] menuElements;

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float delayBetweenElements = 0.15f;
    [SerializeField] private float slideOffset = 30f; // Wie viele Pixel das Element von unten hochgleitet

    private void Start()
    {
        // Alle Elemente vorab unsichtbar machen und leicht nach unten versetzen
        foreach (var item in menuElements)
        {
            if (item.canvasGroup != null)
            {
                item.canvasGroup.alpha = 0f;
            }

            if (item.rectTransform != null)
            {
                item.rectTransform.anchoredPosition -= new Vector2(0, slideOffset);
            }
        }

        StartCoroutine(AnimateMenuIn());
    }

    private IEnumerator AnimateMenuIn()
    {
        // Kurzer Wait, damit der Szenenübergang komplett smooth verläuft
        yield return new WaitForSeconds(0.1f);

        foreach (var item in menuElements)
        {
            if (item.canvasGroup != null)
            {
                StartCoroutine(FadeAndSlideIn(item));
            }
            yield return new WaitForSeconds(delayBetweenElements);
        }
    }

    private IEnumerator FadeAndSlideIn(UIElement item)
    {
        float elapsedTime = 0f;
        Vector2 startPos = item.rectTransform != null ? item.rectTransform.anchoredPosition : Vector2.zero;
        Vector2 targetPos = item.rectTransform != null ? startPos + new Vector2(0, slideOffset) : Vector2.zero;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;

            // Smooth Step Curve für weichere Bewegung (Ease-Out)
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            item.canvasGroup.alpha = Mathf.Lerp(0f, 1f, smoothT);

            if (item.rectTransform != null)
            {
                item.rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothT);
            }

            yield return null;
        }

        item.canvasGroup.alpha = 1f;
        if (item.rectTransform != null)
        {
            item.rectTransform.anchoredPosition = targetPos;
        }
    }
}