
using System;
using System.IO;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// ==========================================
// JSON SAVE DATA
// ==========================================

[Serializable]
public class SpaceAgencySaveData
{
    public int version = 2;

    public string agencyName = "";
    public string logoPng = "";

    public long money = 300000000;
    public float mood = 40f;

    public int day = 1;
    public int month = 1;
    public int year = 1;

    public float timeOfDay = 0f;

    public int tutorialStep = 0;
    public bool tutorialDone = false;

    public int totalLaunches = 0;

    public int satelliteLaunches = 0;
    public int satelliteSuccesses = 0;
    public int satelliteFailures = 0;

    public int moonLaunches = 0;
    public int moonSuccesses = 0;
    public int moonFailures = 0;

    public int marsLaunches = 0;
    public int marsSuccesses = 0;
    public int marsFailures = 0;

    public int consecutiveFailures = 0;

    public int[] upgrades = new int[6];

    public long lifetimeBudget = 0;
    public long lifetimeSpent = 0;
    public long lastBudget = 0;

    public int pendingMission = -1;
    public long pendingLaunchCost = 0;

    public int lastResultMission = -1;
    public int lastResultSuccess = -1;

    public string lastResultText = "";

    // ==========================================
    // ECONOMY / GOVERNMENT GRANT
    // ==========================================

    // Kumuliertes Geld, das über das tägliche Regierungsgeld kam.
    public long regierungsgeld = 0;

    public long lastGovernmentGrant = 0;
    public float lastGovernmentGrantMood = 0f;
    public string lastGovernmentGrantDate = "";

    // Advertisement-Effekte
    public string lastAdvertisementPayoutDate = "";
    public string lastAdvertisementMoodDate = "";

    // Bemannte Missionen
    public int mannedMissionLaunches = 0;
    public int mannedMissionSuccesses = 0;
    public int mannedMissionFailures = 0;
    public long mannedMissionCompensationPaid = 0;

    public string createdAt = "";
    public string updatedAt = "";
}

// ==========================================
// AGENCY MANAGER
// ==========================================

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(RawImage))]
public class AgencyManager : MonoBehaviour,
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

    private static int lastSuccessfulSaveFrame = -1;

    // ==========================================
    // PATHS
    // ==========================================

    private string SaveDirectory =>
        Application.persistentDataPath;

    private string RawPath =>
        Path.Combine(SaveDirectory, "agency_logo.raw");

    private string PngPath =>
        Path.Combine(SaveDirectory, "agency_logo.png");

    private string JsonPath =>
        Path.Combine(SaveDirectory, "space_agency_save.json");

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

        Debug.Log("Agency Manager initialized: " + name);
        Debug.Log("JSON path: " + JsonPath);
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
    // CLEAR DRAWING
    // ==========================================

    public void ClearDrawing()
    {
        Color32 bg = backgroundColor;

        for (int i = 0; i < pixelBuffer.Length; i++)
            pixelBuffer[i] = bg;

        UpdateDisplay();
    }

    // ==========================================
    // JSON HELPERS
    // ==========================================

    private SpaceAgencySaveData CreateDefaultSaveData()
    {
        SpaceAgencySaveData data = new SpaceAgencySaveData();

        data.version = 2;
        data.agencyName = "";
        data.logoPng = "";

        data.money = 300000000;
        data.mood = 40f;

        data.day = 1;
        data.month = 1;
        data.year = 1;

        data.timeOfDay = 0f;

        data.tutorialStep = 0;
        data.tutorialDone = false;

        data.upgrades = new int[6];

        data.pendingMission = -1;
        data.lastResultMission = -1;
        data.lastResultSuccess = -1;

        data.createdAt = "";
        data.updatedAt = "";

        return data;
    }

    private SpaceAgencySaveData ReadSaveData()
    {
        if (!File.Exists(JsonPath))
            return CreateDefaultSaveData();

        string json = File.ReadAllText(JsonPath);

        SpaceAgencySaveData data =
            JsonUtility.FromJson<SpaceAgencySaveData>(json);

        if (data == null)
            throw new Exception("JSON konnte nicht gelesen werden!");

        NormalizeSaveData(data);

        return data;
    }

    private void NormalizeSaveData(SpaceAgencySaveData data)
    {
        if (data.upgrades == null)
            data.upgrades = new int[6];

        if (data.lastResultText == null)
            data.lastResultText = "";

        if (data.agencyName == null)
            data.agencyName = "";

        if (data.logoPng == null)
            data.logoPng = "";

        if (data.lastGovernmentGrantDate == null)
            data.lastGovernmentGrantDate = "";

        if (data.lastAdvertisementPayoutDate == null)
            data.lastAdvertisementPayoutDate = "";

        if (data.lastAdvertisementMoodDate == null)
            data.lastAdvertisementMoodDate = "";

        if (string.IsNullOrEmpty(data.createdAt))
            data.createdAt = GetCurrentTimestamp();
    }

    private string GetCurrentTimestamp()
    {
        return DateTime.Now.ToString(
            "yyyy-MM-dd HH:mm:ss",
            CultureInfo.InvariantCulture
        );
    }

    private void WriteJsonSave(SpaceAgencySaveData data)
    {
        string json = JsonUtility.ToJson(data, true);

        string temporaryPath = JsonPath + ".tmp";

        File.WriteAllText(temporaryPath, json);

        if (File.Exists(JsonPath))
            File.Delete(JsonPath);

        File.Move(temporaryPath, JsonPath);
    }

    // ==========================================
    // SAVE AGENCY + COMPLETE JSON
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

        if (lastSuccessfulSaveFrame == Time.frameCount)
        {
            Debug.LogWarning("Doppelter Speichervorgang verhindert.");
            return;
        }

        try
        {
            Directory.CreateDirectory(SaveDirectory);

            // ==================================
            // 1. CHECK DRAWING
            // ==================================

            int drawnPixels = 0;
            Color32 bg = backgroundColor;

            foreach (Color32 pixel in pixelBuffer)
            {
                if (!pixel.Equals(bg))
                    drawnPixels++;
            }

            if (drawnPixels == 0)
            {
                Debug.LogWarning(
                    "Leere Zeichnung. Speichern abgebrochen."
                );
                return;
            }

            // ==================================
            // 2. CREATE PNG
            // ==================================

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

            // Base64 für JSON
            string logoBase64 = Convert.ToBase64String(pngBytes);

            // ==================================
            // 3. LOAD EXISTING GAME DATA
            // ==================================

            SpaceAgencySaveData saveData;

            try
            {
                saveData = ReadSaveData();
            }
            catch (Exception jsonError)
            {
                // Keine vorhandenen Spieldaten überschreiben,
                // wenn die vorhandene JSON-Datei defekt ist.
                Debug.LogError(
                    "Vorhandener Spielstand konnte nicht gelesen werden. " +
                    "Speichern abgebrochen: " + jsonError
                );
                return;
            }

            // ==================================
            // 4. UPDATE ONLY AGENCY INFORMATION
            // ==================================

            // Alle anderen Spielwerte bleiben erhalten.
            saveData.version = 2;
            saveData.agencyName = agencyName;
            saveData.logoPng = logoBase64;

            NormalizeSaveData(saveData);

            saveData.updatedAt = GetCurrentTimestamp();

            // ==================================
            // 5. SAVE IMAGE FILES
            // ==================================

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

            File.WriteAllBytes(RawPath, rawBytes);
            File.WriteAllBytes(PngPath, pngBytes);

            // ==================================
            // 6. SAVE JSON
            // ==================================

            WriteJsonSave(saveData);

            // ==================================
            // 7. SAVE PLAYERPREFS
            // ==================================

            PlayerPrefs.SetString(
                "SavedAgencyName",
                agencyName
            );

            PlayerPrefs.SetString(
                "SavedAgencyLogoPath",
                RawPath
            );

            PlayerPrefs.SetString(
                "SavedAgencyPngPath",
                PngPath
            );

            PlayerPrefs.SetInt(
                "SavedAgencyTextureSize",
                textureSize
            );

            PlayerPrefs.SetInt(
                "HasAgencySaveState",
                1
            );

            PlayerPrefs.Save();

            lastSuccessfulSaveFrame = Time.frameCount;

            // ==================================
            // 8. LOG
            // ==================================

            Debug.Log("========== AGENCY SAVE SUCCESS ==========");
            Debug.Log("Agency: " + agencyName);
            Debug.Log("Drawn pixels: " + drawnPixels);
            Debug.Log("PNG bytes: " + pngBytes.Length);
            Debug.Log("Base64 length: " + logoBase64.Length);
            Debug.Log("JSON: " + JsonPath);
            Debug.Log("RAW: " + RawPath);
            Debug.Log("PNG: " + PngPath);
            Debug.Log("Money preserved: " + saveData.money);
            Debug.Log("Mood preserved: " + saveData.mood);
            Debug.Log("Launches preserved: " + saveData.totalLaunches);
            Debug.Log("=========================================");
        }
        catch (Exception e)
        {
            Debug.LogError("Agency save failed: " + e);
        }
    }

    // ==========================================
    // LOAD EXISTING DATA
    // ==========================================

    private void LoadExistingData()
    {
        SpaceAgencySaveData saveData = null;

        // JSON zuerst laden
        if (File.Exists(JsonPath))
        {
            try
            {
                saveData = ReadSaveData();

                if (agencyNameInput != null)
                    agencyNameInput.text = saveData.agencyName;

                // Logo direkt aus JSON-Base64 laden
                if (!string.IsNullOrEmpty(saveData.logoPng))
                {
                    byte[] pngBytes =
                        Convert.FromBase64String(saveData.logoPng);

                    if (LoadPngTexture(pngBytes))
                    {
                        Debug.Log("Agency logo loaded from JSON!");
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("JSON load error: " + e);
            }
        }

        // Fallback: PlayerPrefs
        if (agencyNameInput != null && saveData == null)
        {
            agencyNameInput.text =
                PlayerPrefs.GetString("SavedAgencyName", "");
        }

        // Fallback: RAW-Datei
        string rawPath = PlayerPrefs.GetString(
            "SavedAgencyLogoPath",
            RawPath
        );

        if (File.Exists(rawPath))
        {
            LoadRawTexture(rawPath);
        }
    }

    private bool LoadPngTexture(byte[] pngBytes)
    {
        Texture2D loadedTexture = new Texture2D(2, 2);

        if (!loadedTexture.LoadImage(pngBytes))
        {
            Destroy(loadedTexture);
            return false;
        }

        if (loadedTexture.width != loadedTexture.height ||
            loadedTexture.width < 16 ||
            loadedTexture.width > 4096)
        {
            Destroy(loadedTexture);
            Debug.LogWarning("Ungültige Logo-Auflösung.");
            return false;
        }

        textureSize = loadedTexture.width;

        pixelBuffer = loadedTexture.GetPixels32();

        Destroy(loadedTexture);

        CreateDisplayTexture();
        UpdateDisplay();

        return true;
    }

    private bool LoadRawTexture(string path)
    {
        try
        {
            int savedSize = PlayerPrefs.GetInt(
                "SavedAgencyTextureSize",
                textureSize
            );

            byte[] rawBytes = File.ReadAllBytes(path);

            long expectedLength =
                (long)savedSize * savedSize * 4;

            if (savedSize < 16 ||
                savedSize > 4096 ||
                rawBytes.LongLength != expectedLength)
            {
                Debug.LogError("Ungültige RAW-Datei!");
                return false;
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

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("RAW load failed: " + e);
            return false;
        }
    }

    // ==========================================
    // PUBLIC ACCESS
    // ==========================================

    public Texture2D GetDrawingTexture()
    {
        return drawingTexture;
    }

    public string GetSaveFilePath()
    {
        return JsonPath;
    }

    private void OnDestroy()
    {
        if (drawingTexture != null)
            Destroy(drawingTexture);
    }
}
