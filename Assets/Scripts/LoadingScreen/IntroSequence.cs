using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroSequence : MonoBehaviour
{
    [Header("Canvas Reference")]
    [SerializeField] private CanvasGroup canvas1;
    [SerializeField] private CanvasGroup canvas2;

    [Header("Timing Settings")]
    [SerializeField] private float fadeInDuration = 1.0f;
    [SerializeField] private float displayDuration = 1.8f;
    [SerializeField] private float fadeOutDuration = 0.8f;
    [SerializeField] private float pauseBetweenCanvas = 0.3f;

    [Header("Scene Settings")]
    [SerializeField] private string sceneToLoad;

    private void Start()
    {
        if (canvas1 != null) canvas1.alpha = 0f;
        if (canvas2 != null) canvas2.alpha = 0f;

        StartCoroutine(PlayIntroSequence());
    }

    private IEnumerator PlayIntroSequence()
    {
        // 1. Erster Canvas Fade
        if (canvas1 != null)
        {
            yield return StartCoroutine(Fade(canvas1, 0f, 1f, fadeInDuration));
            yield return new WaitForSeconds(displayDuration);
            yield return StartCoroutine(Fade(canvas1, 1f, 0f, fadeOutDuration));
        }

        yield return new WaitForSeconds(pauseBetweenCanvas);

        // 2. Zweiter Canvas Fade-In & Hold
        if (canvas2 != null)
        {
            yield return StartCoroutine(Fade(canvas2, 0f, 1f, fadeInDuration));
            yield return new WaitForSeconds(displayDuration);
        }

        // 3. Preload der Hauptmenü-Szene im Hintergrund (Hier war der Fix: SceneManager statt SceneManagement.SceneManager)
        AsyncOperation asyncLoad = null;
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
            asyncLoad.allowSceneActivation = false; // Noch nicht direkt anzeigen

            // Warten bis die Szene fast fertig geladen ist (0.9f entspricht 100% geladen bei Async)
            while (asyncLoad.progress < 0.9f)
            {
                yield return null;
            }
        }

        // 4. Zweiter Canvas Ausfaden und nahtlos die Szene aktivieren
        if (canvas2 != null)
        {
            yield return StartCoroutine(Fade(canvas2, 1f, 0f, fadeOutDuration));
        }

        if (asyncLoad != null)
        {
            asyncLoad.allowSceneActivation = true;
        }
    }

    private IEnumerator Fade(CanvasGroup cg, float startAlpha, float targetAlpha, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        cg.alpha = targetAlpha;
    }
}