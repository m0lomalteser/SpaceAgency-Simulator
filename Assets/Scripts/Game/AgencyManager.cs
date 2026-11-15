
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(RawImage))]
public class AgencyCreationUI : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IDragHandler
{
    [Header("UI & Setup")]
    public TMP_InputField agencyNameInput;
    public Button createButton;

    [Header("Tools & Colors")]
    public Button penButton;
    public Button eraserButton;
    public Button[] colorButtons;

    public Color[] colors =
    {
        Color.red,
        Color.yellow,
        Color.magenta,
        Color.blue,
        Color.green
    };

    [Header("Settings")]
    public int textureSize = 512;
    public Color backgroundColor = Color.white;
    public int brushSize = 8;

    private RectTransform rectTransform;
    private RawImage rawImage;
    private Canvas parentCanvas;

    private Color32[] pixelBuffer;
    private Texture2D drawingTexture;

    private Color currentColor = Color.red;
    private bool isEraser;

    private bool isDrawing;
    private bool hasLastPosition;

    private Vector2 lastPosition;

    // Verhindert mehrfache Exporte durch mehrere Instanzen.
    private static int lastSuccessfulSaveFrame = -1;

    private string RawPath =>
        Path.Combine(
            Application.persistentDataPath,
            "agency_logo.raw"
        );

    private string PngPath =>
        Path.Combine(
            Application.persistentDataPath,
            "agency_logo.png"
        );

    // ==========================================
    // INITIALIZATION
    // ==========================================

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        rawImage = GetComponent<RawImage>();
        parentCanvas = GetComponentInParent<Canvas>();

        rawImage.raycastTarget = true;

        InitializeDrawing();
        SetupButtons();
        LoadExistingData();

        Debug.Log("Agency UI initialized: " + name);
    }

    private void InitializeDrawing()
    {
        textureSize = Mathf.Clamp(textureSize, 16, 4096);

        pixelBuffer = new Color32[textureSize * textureSize];

        Color32 bg = backgroundColor;

        for (int i = 0; i < pixelBuffer.Length; i++)
            pixelBuffer[i] = bg;

        CreateDisplayTexture();
        UpdateDisplay();
    }

    private void CreateDisplayTexture()
    {
        if (drawingTexture != null)
            Destroy(drawingTexture);

        drawingTexture = new Texture2D(
            textureSize,
            textureSize,
            TextureFormat.RGBA32,
            false
        );

        drawingTexture.name = "AgencyDrawingTexture";
        drawingTexture.filterMode = FilterMode.Bilinear;
        drawingTexture.wrapMode = TextureWrapMode.Clamp;

        rawImage.texture = drawingTexture;
    }

    private void UpdateDisplay()
    {
        drawingTexture.SetPixels32(pixelBuffer);
        drawingTexture.Apply(false, false);
    }

    // ==========================================
    // BUTTONS
    // ==========================================

    private void SetupButtons()
    {
        if (createButton != null)
        {
            createButton.onClick.RemoveListener(SaveAgencyData);
            createButton.onClick.AddListener(SaveAgencyData);
        }

        if (penButton != null)
            penButton.onClick.AddListener(() => isEraser = false);

        if (eraserButton != null)
            eraserButton.onClick.AddListener(() => isEraser = true);

        if (colorButtons == null)
            return;

        for (int i = 0; i < colorButtons.Length && i < colors.Length; i++)
        {
            int index = i;

            if (colorButtons[i] == null)
                continue;

            colorButtons[i].onClick.AddListener(() =>
            {
                currentColor = colors[index];
                isEraser = false;
            });
        }
    }

    // ==========================================
    // INPUT
    // ==========================================

    public void OnPointerDown(PointerEventData eventData)
    {
        isDrawing = true;
        hasLastPosition = false;

        if (!GetTexturePosition(eventData, out Vector2 position))
            return;

        lastPosition = position;
        hasLastPosition = true;

        DrawSegment(position, position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDrawing)
            return;

        if (!GetTexturePosition(eventData, out Vector2 position))
        {
            hasLastPosition = false;
            return;
        }

        if (!hasLastPosition)
        {
            lastPosition = position;
            hasLastPosition = true;

            DrawSegment(position, position);
            return;
        }

        DrawSegment(lastPosition, position);
        lastPosition = position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDrawing = false;
        hasLastPosition = false;
    }

    private bool GetTexturePosition(
        PointerEventData eventData,
        out Vector2 position)
    {
        position = Vector2.zero;

        Camera cam = null;

        if (parentCanvas != null &&
            parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = eventData.pressEventCamera;

            if (cam == null)
                cam = parentCanvas.worldCamera;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            cam,
            out Vector2 localPoint))
            return false;

        Rect rect = rectTransform.rect;

        if (rect.width <= 0 || rect.height <= 0)
            return false;

        float nx = (localPoint.x - rect.xMin) / rect.width;
        float ny = (localPoint.y - rect.yMin) / rect.height;

        if (nx < 0 || nx > 1 || ny < 0 || ny > 1)
            return false;

        Rect uv = rawImage.uvRect;

        float u = uv.x + nx * uv.width;
        float v = uv.y + ny * uv.height;

        position = new Vector2(
            Mathf.Clamp01(u) * (textureSize - 1),
            Mathf.Clamp01(v) * (textureSize - 1)
        );

        return true;
    }

    // ==========================================
    // SMOOTH BRUSH
    // ==========================================

    private void DrawSegment(Vector2 start, Vector2 end)
    {
        float radius = Mathf.Max(0.5f, brushSize);

        Color32 brushColor = isEraser
            ? (Color32)backgroundColor
            : (Color32)currentColor;

        int xMin = Mathf.Clamp(
            Mathf.FloorToInt(Mathf.Min(start.x, end.x) - radius - 1),
            0,
            textureSize - 1
        );

        int xMax = Mathf.Clamp(
            Mathf.CeilToInt(Mathf.Max(start.x, end.x) + radius + 1),
            0,
            textureSize - 1
        );

        int yMin = Mathf.Clamp(
            Mathf.FloorToInt(Mathf.Min(start.y, end.y) - radius - 1),
            0,
            textureSize - 1
        );

        int yMax = Mathf.Clamp(
            Mathf.CeilToInt(Mathf.Max(start.y, end.y) + radius + 1),
            0,
            textureSize - 1
        );

        Vector2 direction = end - start;
        float lengthSquared = direction.sqrMagnitude;

        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                Vector2 point = new Vector2(x + 0.5f, y + 0.5f);

                float t = 0f;

                if (lengthSquared > 0.0001f)
                {
                    t = Mathf.Clamp01(
                        Vector2.Dot(point - start, direction) /
                        lengthSquared
                    );
                }

                Vector2 closest = start + direction * t;

                float distance = Vector2.Distance(point, closest);

                float coverage = Mathf.Clamp01(
                    radius + 0.5f - distance
                );

                if (coverage <= 0f)
                    continue;

                int index = y * textureSize + x;

                pixelBuffer[index] = BlendPixel(
                    pixelBuffer[index],
                    brushColor,
                    coverage
                );
            }
        }

        UpdateDisplay();
    }

    private Color32 BlendPixel(
        Color32 destination,
        Color32 source,
        float amount)
    {
        return new Color32(
            (byte)Mathf.RoundToInt(Mathf.Lerp(destination.r, source.r, amount)),
            (byte)Mathf.RoundToInt(Mathf.Lerp(destination.g, source.g, amount)),
            (byte)Mathf.RoundToInt(Mathf.Lerp(destination.b, source.b, amount)),
            255
        );
    }

    // ==========================================
    // CLEAR
    // ==========================================

    public void ClearDrawing()
    {
        Color32 bg = backgroundColor;

        for (int i = 0; i < pixelBuffer.Length; i++)
            pixelBuffer[i] = bg;

        UpdateDisplay();
    }

    // ==========================================
    // SAVE
    // ==========================================

    public void SaveAgencyData()
    {
        string agencyName = agencyNameInput != null
            ? agencyNameInput.text.Trim()
            : "";

        if (string.IsNullOrWhiteSpace(agencyName))
        {
            Debug.LogWarning("Bitte Agency-Namen eingeben!");
            return;
        }

        if (pixelBuffer == null)
        {
            Debug.LogError("Pixelbuffer fehlt!");
            return;
        }

        try
        {
            // 1. Pixel prüfen
            int drawnPixels = 0;
            Color32 bg = backgroundColor;

            foreach (Color32 pixel in pixelBuffer)
            {
                if (!pixel.Equals(bg))
                    drawnPixels++;
            }

            Debug.Log(
                "Save request | Object: " + name +
                " | Changed pixels: " + drawnPixels
            );

            // LEERE INSTANZEN DÜRFEN NICHT SPEICHERN
            if (drawnPixels == 0)
            {
                Debug.LogWarning(
                    "Leere Zeichnung erkannt. Bestehende Datei bleibt erhalten."
                );
                return;
            }

            // DOPPELTEN EXPORT IM SELBEN FRAME VERHINDERN
            if (lastSuccessfulSaveFrame == Time.frameCount)
            {
                Debug.LogWarning(
                    "Doppelter Speichervorgang verhindert."
                );
                return;
            }

            // 2. RAW erstellen
            byte[] rawBytes = new byte[pixelBuffer.Length * 4];

            for (int i = 0; i < pixelBuffer.Length; i++)
            {
                int offset = i * 4;
                Color32 pixel = pixelBuffer[i];

                rawBytes[offset] = pixel.r;
                rawBytes[offset + 1] = pixel.g;
                rawBytes[offset + 2] = pixel.b;
                rawBytes[offset + 3] = pixel.a;
            }

            // 3. PNG erstellen
            Texture2D exportTexture = new Texture2D(
                textureSize,
                textureSize,
                TextureFormat.RGBA32,
                false
            );

            exportTexture.SetPixels32(pixelBuffer);
            exportTexture.Apply(false, false);

            byte[] pngBytes = exportTexture.EncodeToPNG();

            Destroy(exportTexture);

            if (pngBytes == null || pngBytes.Length == 0)
            {
                Debug.LogError("PNG-Encoding fehlgeschlagen!");
                return;
            }

            // 4. Dateien schreiben
            string rawTemp = RawPath + ".tmp";
            string pngTemp = PngPath + ".tmp";

            File.WriteAllBytes(rawTemp, rawBytes);
            File.WriteAllBytes(pngTemp, pngBytes);

            if (File.Exists(RawPath))
                File.Delete(RawPath);

            if (File.Exists(PngPath))
                File.Delete(PngPath);

            File.Move(rawTemp, RawPath);
            File.Move(pngTemp, PngPath);

            // 5. Metadaten
            PlayerPrefs.SetString("SavedAgencyName", agencyName);
            PlayerPrefs.SetString("SavedAgencyLogoPath", RawPath);
            PlayerPrefs.SetString("SavedAgencyPngPath", PngPath);
            PlayerPrefs.SetInt("SavedAgencyTextureSize", textureSize);
            PlayerPrefs.SetInt("HasAgencySaveState", 1);
            PlayerPrefs.Save();

            lastSuccessfulSaveFrame = Time.frameCount;

            Debug.Log("========== EXPORT SUCCESS ==========");
            Debug.Log("Agency: " + agencyName);
            Debug.Log("Drawn pixels: " + drawnPixels);
            Debug.Log("PNG bytes: " + pngBytes.Length);
            Debug.Log("PNG: " + PngPath);
            Debug.Log("RAW bytes: " + rawBytes.Length);
            Debug.Log("RAW: " + RawPath);
            Debug.Log("====================================");
        }
        catch (Exception e)
        {
            Debug.LogError("Export failed: " + e);
        }
    }

    // ==========================================
    // LOAD
    // ==========================================

    private void LoadExistingData()
    {
        if (PlayerPrefs.GetInt("HasAgencySaveState", 0) != 1)
            return;

        if (agencyNameInput != null)
        {
            agencyNameInput.text =
                PlayerPrefs.GetString("SavedAgencyName", "");
        }

        string path = PlayerPrefs.GetString(
            "SavedAgencyLogoPath",
            RawPath
        );

        if (!File.Exists(path))
        {
            Debug.LogWarning("Keine RAW-Datei gefunden.");
            return;
        }

        try
        {
            int savedSize = PlayerPrefs.GetInt(
                "SavedAgencyTextureSize",
                textureSize
            );

            byte[] rawBytes = File.ReadAllBytes(path);

            long expectedLength = (long)savedSize * savedSize * 4;

            if (savedSize < 16 ||
                savedSize > 4096 ||
                rawBytes.LongLength != expectedLength)
            {
                Debug.LogError(
                    "Ungültige RAW-Datei. Erwartet: " +
                    expectedLength + " Bytes, erhalten: " +
                    rawBytes.Length
                );
                return;
            }

            Color32[] loadedPixels =
                new Color32[savedSize * savedSize];

            for (int i = 0; i < loadedPixels.Length; i++)
            {
                int offset = i * 4;

                loadedPixels[i] = new Color32(
                    rawBytes[offset],
                    rawBytes[offset + 1],
                    rawBytes[offset + 2],
                    rawBytes[offset + 3]
                );
            }

            textureSize = savedSize;
            pixelBuffer = loadedPixels;

            CreateDisplayTexture();
            UpdateDisplay();

            Debug.Log("RAW texture loaded successfully!");
        }
        catch (Exception e)
        {
            Debug.LogError("Load failed: " + e);
        }
    }

    // ==========================================
    // PUBLIC ACCESS
    // ==========================================

    public Texture2D GetDrawingTexture()
    {
        return drawingTexture;
    }

    private void OnDestroy()
    {
        if (drawingTexture != null)
            Destroy(drawingTexture);
    }
}
