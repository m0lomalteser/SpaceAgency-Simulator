using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Erststart-Tutorial als additive Overlay-Szene.
///
/// Zeigt kurze englische Erklärungen und markiert die passenden
/// UI-Elemente der Game-Szene mit einem Rahmen. Vorhandene UI wird
/// dabei nicht verändert - das Overlay liegt nur darüber.
/// </summary>
public class TutorialOverlayController : MonoBehaviour
{
    [Serializable]
    public class TutorialStep
    {
        public string title;

        [TextArea(2, 6)]
        public string body;

        [Tooltip("Pfad in der Game-Szene, z. B. Canvas/EDIT (Button). Leer = zentriert.")]
        public string targetPath;
    }

    [Header("Darstellung")]
    public TMP_FontAsset fontAsset;

    public Color accentColor = new Color(0.20f, 0.88f, 1f, 1f);

    [Range(0f, 0.95f)]
    public float dimAlpha = 0.72f;

    public float markerPadding = 16f;

    [Header("Tutorial-Schritte")]
    [Tooltip("Leer lassen, um die eingebauten Standard-Schritte zu nutzen.")]
    public TutorialStep[] steps;

    private Canvas canvas;
    private RectTransform canvasRect;

    private Image dimTop;
    private Image dimBottom;
    private Image dimLeft;
    private Image dimRight;

    private RectTransform marker;
    private Image markerTop;
    private Image markerBottom;
    private Image markerLeft;
    private Image markerRight;

    private RectTransform card;
    private TextMeshProUGUI stepLabel;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI bodyText;

    private RectTransform target;
    private int index;
    private float previousTimeScale = 1f;
    private float markerPulse;
    private float openedAt;
    private bool finished;

    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void Awake()
    {
        RuntimeUIFactory.EnsureEventSystem();

        BuildUI();

        if (steps == null || steps.Length == 0)
            steps = CreateDefaultSteps();
    }

    private void Start()
    {
        previousTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
        Time.timeScale = 0f;

        openedAt = Time.unscaledTime;

        ShowStep(0);
    }

    private void Update()
    {
        if (finished)
            return;

        if (Time.unscaledTime - openedAt < 0.25f)
            return;

        if (OverlayInput.EscapePressed)
        {
            Finish();
            return;
        }

        if (OverlayInput.SubmitPressed)
        {
            Next();
            return;
        }

        UpdateMarker();
    }

    // =========================================================
    // UI
    // =========================================================

    private void BuildUI()
    {
        canvas = RuntimeUIFactory.CreateCanvas("TutorialCanvas", 7000);
        canvasRect = canvas.GetComponent<RectTransform>();

        // Unsichtbarer Blocker: Klicks gehen nicht an das Spiel darunter.
        RuntimeUIFactory.CreateFullScreen(
            canvas.transform,
            "InputBlocker",
            new Color(0f, 0f, 0f, 0f),
            true
        );

        Color dimColor = new Color(0f, 0f, 0f, dimAlpha);

        dimTop = RuntimeUIFactory.CreateFullScreen(
            canvas.transform, "DimTop", dimColor);

        dimBottom = RuntimeUIFactory.CreateFullScreen(
            canvas.transform, "DimBottom", dimColor);

        dimLeft = RuntimeUIFactory.CreateFullScreen(
            canvas.transform, "DimLeft", dimColor);

        dimRight = RuntimeUIFactory.CreateFullScreen(
            canvas.transform, "DimRight", dimColor);

        marker = RuntimeUIFactory.CreateRect(
            canvas.transform, "Highlight");

        markerTop = CreateMarkerEdge("Top");
        markerBottom = CreateMarkerEdge("Bottom");
        markerLeft = CreateMarkerEdge("Left");
        markerRight = CreateMarkerEdge("Right");

        BuildCard();
    }

    private Image CreateMarkerEdge(string edgeName)
    {
        Image image = RuntimeUIFactory.CreateImage(
            marker,
            edgeName,
            accentColor
        );

        RectTransform rect = image.rectTransform;

        switch (edgeName)
        {
            case "Top":
                RuntimeUIFactory.PlaceAnchored(
                    rect, new Vector2(0f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, 4f), new Vector2(0f, 0f));
                break;

            case "Bottom":
                RuntimeUIFactory.PlaceAnchored(
                    rect, new Vector2(0f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 4f), new Vector2(0f, 0f));
                break;

            case "Left":
                RuntimeUIFactory.PlaceAnchored(
                    rect, new Vector2(0f, 0f), new Vector2(0f, 0f),
                    new Vector2(4f, 0f), new Vector2(0f, 0f));
                break;

            default:
                RuntimeUIFactory.PlaceAnchored(
                    rect, new Vector2(1f, 0f), new Vector2(1f, 0f),
                    new Vector2(4f, 0f), new Vector2(0f, 0f));
                break;
        }

        return image;
    }

    private void BuildCard()
    {
        Image cardImage = RuntimeUIFactory.CreateImage(
            canvas.transform,
            "TutorialCard",
            RuntimeUIFactory.Panel,
            true
        );

        card = cardImage.rectTransform;
        RuntimeUIFactory.PlaceCentered(
            card,
            new Vector2(1000f, 286f),
            new Vector2(0f, -320f)
        );

        Image accentBar = RuntimeUIFactory.CreateImage(
            card,
            "AccentBar",
            accentColor
        );

        accentBar.rectTransform.anchorMin = new Vector2(0f, 0f);
        accentBar.rectTransform.anchorMax = new Vector2(0f, 1f);
        accentBar.rectTransform.pivot = new Vector2(0f, 0.5f);
        accentBar.rectTransform.sizeDelta = new Vector2(5f, 0f);
        accentBar.rectTransform.anchoredPosition = Vector2.zero;

        stepLabel = RuntimeUIFactory.CreateText(
            card,
            "StepLabel",
            "STEP 1 / 1",
            20f,
            accentColor,
            TextAlignmentOptions.Left,
            fontAsset
        );

        RuntimeUIFactory.PlaceAnchored(
            stepLabel.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(700f, 28f),
            new Vector2(38f, -26f)
        );

        titleText = RuntimeUIFactory.CreateText(
            card,
            "Title",
            "TITLE",
            36f,
            RuntimeUIFactory.TextColor,
            TextAlignmentOptions.Left,
            fontAsset
        );

        titleText.fontStyle = FontStyles.Bold;

        RuntimeUIFactory.PlaceAnchored(
            titleText.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(920f, 46f),
            new Vector2(38f, -58f)
        );

        bodyText = RuntimeUIFactory.CreateText(
            card,
            "Body",
            "Body",
            22f,
            RuntimeUIFactory.TextMuted,
            TextAlignmentOptions.TopLeft,
            fontAsset
        );

        RuntimeUIFactory.PlaceAnchored(
            bodyText.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(920f, 92f),
            new Vector2(38f, -112f)
        );

        Button skipButton = RuntimeUIFactory.CreateButton(
            card,
            "SkipButton",
            "SKIP",
            new Vector2(170f, 52f),
            Finish,
            fontAsset
        );

        RuntimeUIFactory.PlaceAnchored(
            skipButton.GetComponent<RectTransform>(),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(170f, 52f),
            new Vector2(38f, 26f)
        );

        Button nextButton = RuntimeUIFactory.CreateButton(
            card,
            "NextButton",
            "NEXT  [ENTER]",
            new Vector2(250f, 56f),
            Next,
            fontAsset,
            true
        );

        RuntimeUIFactory.PlaceAnchored(
            nextButton.GetComponent<RectTransform>(),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(250f, 56f),
            new Vector2(-38f, 26f)
        );
    }

    // =========================================================
    // STEPS
    // =========================================================

    private void ShowStep(int stepIndex)
    {
        if (steps == null || steps.Length == 0)
            return;

        index = Mathf.Clamp(stepIndex, 0, steps.Length - 1);

        TutorialStep step = steps[index];

        target = FindTarget(step.targetPath);

        stepLabel.text =
            "STEP " + (index + 1) + " / " + steps.Length;

        titleText.text = step.title;
        bodyText.text = step.body;

        LayoutCard();
        UpdateMarker();
    }

    private void Next()
    {
        if (index + 1 >= steps.Length)
        {
            Finish();
            return;
        }

        ShowStep(index + 1);
    }

    private void Finish()
    {
        if (finished)
            return;

        finished = true;

        TutorialState.MarkCompleted();

        Time.timeScale = previousTimeScale <= 0f
            ? 1f
            : previousTimeScale;

        Scene scene = gameObject.scene;

        if (scene.IsValid() && scene.isLoaded)
            SceneManager.UnloadSceneAsync(scene);
    }

    // =========================================================
    // TARGET / MARKER
    // =========================================================

    private RectTransform FindTarget(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        GameObject found = GameObject.Find(path);

        if (found == null)
        {
            int slash = path.LastIndexOf('/');

            if (slash >= 0 && slash + 1 < path.Length)
                found = GameObject.Find(path.Substring(slash + 1));
        }

        if (found == null)
            return null;

        RectTransform rect = found.transform as RectTransform;

        if (rect == null || !rect.gameObject.activeInHierarchy)
            return null;

        return rect;
    }

    private void UpdateMarker()
    {
        Rect canvasBounds = canvasRect.rect;

        if (target == null)
        {
            SetLocalRect(
                dimTop.rectTransform,
                canvasBounds.xMin,
                canvasBounds.yMin,
                canvasBounds.xMax,
                canvasBounds.yMax
            );

            SetLocalRect(dimBottom.rectTransform, 0f, 0f, 0f, 0f);
            SetLocalRect(dimLeft.rectTransform, 0f, 0f, 0f, 0f);
            SetLocalRect(dimRight.rectTransform, 0f, 0f, 0f, 0f);

            marker.gameObject.SetActive(false);
            return;
        }

        marker.gameObject.SetActive(true);

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        foreach (Vector3 corner in corners)
        {
            Vector2 screenPoint =
                RectTransformUtility.WorldToScreenPoint(null, corner);

            Vector2 localPoint;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPoint,
                null,
                out localPoint))
            {
                continue;
            }

            minX = Mathf.Min(minX, localPoint.x);
            minY = Mathf.Min(minY, localPoint.y);
            maxX = Mathf.Max(maxX, localPoint.x);
            maxY = Mathf.Max(maxY, localPoint.y);
        }

        if (minX > maxX || minY > maxY)
        {
            marker.gameObject.SetActive(false);
            return;
        }

        float pad = Mathf.Max(0f, markerPadding);

        marker.anchoredPosition = new Vector2(
            (minX + maxX) * 0.5f,
            (minY + maxY) * 0.5f
        );

        marker.sizeDelta = new Vector2(
            (maxX - minX) + pad * 2f,
            (maxY - minY) + pad * 2f
        );

        markerPulse += Time.unscaledDeltaTime * 3.4f;

        float pulse = 0.6f + 0.4f * Mathf.Sin(markerPulse);

        Color edgeColor = accentColor;
        edgeColor.a = pulse;

        markerTop.color = edgeColor;
        markerBottom.color = edgeColor;
        markerLeft.color = edgeColor;
        markerRight.color = edgeColor;

        float left = Mathf.Clamp(minX - pad, canvasBounds.xMin, canvasBounds.xMax);
        float right = Mathf.Clamp(maxX + pad, canvasBounds.xMin, canvasBounds.xMax);
        float bottom = Mathf.Clamp(minY - pad, canvasBounds.yMin, canvasBounds.yMax);
        float top = Mathf.Clamp(maxY + pad, canvasBounds.yMin, canvasBounds.yMax);

        SetLocalRect(
            dimTop.rectTransform,
            canvasBounds.xMin, top,
            canvasBounds.xMax, canvasBounds.yMax
        );

        SetLocalRect(
            dimBottom.rectTransform,
            canvasBounds.xMin, canvasBounds.yMin,
            canvasBounds.xMax, bottom
        );

        SetLocalRect(
            dimLeft.rectTransform,
            canvasBounds.xMin, bottom,
            left, top
        );

        SetLocalRect(
            dimRight.rectTransform,
            right, bottom,
            canvasBounds.xMax, top
        );

        LayoutCard();
    }

    private void LayoutCard()
    {
        float canvasHeight = canvasRect.rect.height;

        float y;

        if (target == null)
        {
            y = -20f;
        }
        else if (marker.anchoredPosition.y < 0f)
        {
            // Ziel ist unten -> Karte nach oben.
            y = canvasHeight * 0.5f - 260f;
        }
        else
        {
            // Ziel ist oben -> Karte nach unten.
            y = -canvasHeight * 0.5f + 260f;
        }

        card.anchoredPosition = new Vector2(0f, y);
    }

    private static void SetLocalRect(
        RectTransform rect,
        float xMin,
        float yMin,
        float xMax,
        float yMax
    )
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        rect.anchoredPosition = new Vector2(
            (xMin + xMax) * 0.5f,
            (yMin + yMax) * 0.5f
        );

        rect.sizeDelta = new Vector2(
            Mathf.Max(0f, xMax - xMin),
            Mathf.Max(0f, yMax - yMin)
        );
    }

    // =========================================================
    // DEFAULT STEPS
    // =========================================================

    private static TutorialStep[] CreateDefaultSteps()
    {
        return new[]
        {
            new TutorialStep
            {
                title = "WELCOME, DIRECTOR",
                body = "You run a space agency. Build rockets, launch missions and keep your mood high to earn more money.",
                targetPath = ""
            },
            new TutorialStep
            {
                title = "YOUR BUDGET",
                body = "This is your current budget. Building rockets costs money - successful missions and daily government payouts fill it up again.",
                targetPath = "Canvas/Text (TMP)"
            },
            new TutorialStep
            {
                title = "MOOD",
                body = "Your mood decides how much the government pays you every day. Success raises it, failures hurt it.",
                targetPath = "Canvas/RawImage"
            },
            new TutorialStep
            {
                title = "TIME AND REGIERUNGSGELD",
                body = "The clock keeps running. Every day at 00:00 you receive government funding based on your mood - up to 10M per day.",
                targetPath = "Canvas/CLOCK"
            },
            new TutorialStep
            {
                title = "BUILD A ROCKET",
                body = "Open the rocket builder, pick parts for every slot and save the design. The Cheaper Parts upgrade reduces every price.",
                targetPath = "Canvas/EDIT (Button)"
            },
            new TutorialStep
            {
                title = "UPGRADES",
                body = "Buy Advertisements, Reusable Rockets, Manned Missions and Cheaper Parts. Each upgrade changes how you earn or spend money.",
                targetPath = "Canvas/UPGRADES (Button)"
            },
            new TutorialStep
            {
                title = "MISSIONS",
                body = "Start missions from here. Success pays a lot and raises mood. Failure costs compensation and a lot of mood.",
                targetPath = "Canvas/MISSION (Button)"
            },
            new TutorialStep
            {
                title = "PAUSE ANY TIME",
                body = "Press ESC to pause, open the settings or return to the main menu. That's it - good luck, Director.",
                targetPath = ""
            }
        };
    }
}
