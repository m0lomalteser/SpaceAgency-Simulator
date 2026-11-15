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
public class AgencySavePayload
{
    public int version = 2;

    public string agencyName = "";
    public string logoPng = "";

    public long money = 30000000;
    public float mood = 50f;

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

    public long regierungsgeld = 0;

    public long lastGovernmentGrant = 0;
    public float lastGovernmentGrantMood = 0f;
    public string lastGovernmentGrantDate = "";

    public string lastAdvertisementPayoutDate = "";
    public string lastAdvertisementMoodDate = "";

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
public class EnterpriseCustomizerController : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IDragHandler
{
    [Header("UI & Setup")]
    public TMP_InputField nameInputField;
    public Button submitButton;

    [Header("Tools & Colors")]
    public Button drawPenButton;
    public Button eraserToolButton;
    public Button[] paletteColorButtons;

    public Color[] paletteColors =
    {
        Color.red,
        Color.yellow,
        Color.magenta,
        Color.blue,
        Color.green
    };

    [Header("Settings")]
    public int canvasTextureSize = 512;
    public Color defaultBgColor = Color.white;
    public int currentBrushRadius = 8;

    private RectTransform agencyRectTransform;
    private RawImage canvasRawImage;
    private Canvas rootCanvas;

    private Color32[] canvasPixelArray;
    private Texture2D activeDrawingTexture;

    private Color activeDrawColor = Color.red;
    private bool eraserModeActive;

    private bool isUserDrawing;
    private bool hasValidLastPos;

    private Vector2 previousDrawPos;

    private static int lastSaveFrameIndex = -1;

    // ==========================================
    // PATHS
    // ==========================================

    private string StorageDirectory =>
        Application.persistentDataPath;

    private string RawFilePath =>
        Path.Combine(StorageDirectory, "agency_logo.raw");

    private string PngFilePath =>
        Path.Combine(StorageDirectory, "agency_logo.png");

    private string JsonFilePath =>
        Path.Combine(StorageDirectory, "space_agency_save.json");

    // ==========================================
    // INITIALIZATION
    // ==========================================

    private void Start()
    {
        agencyRectTransform = GetComponent<RectTransform>();
        canvasRawImage = GetComponent<RawImage>();
        rootCanvas = GetComponentInParent<Canvas>();

        canvasRawImage.raycastTarget = true;

        SetupDrawingCanvas();
        InitializeUIButtons();
        RestoreExistingSaveData();

        Debug.Log("Agency Profile Controller initialized: " + name);
        Debug.Log("JSON path: " + JsonFilePath);
    }

    private void SetupDrawingCanvas()
    {
        canvasTextureSize = Mathf.Clamp(canvasTextureSize, 16, 4096);

        canvasPixelArray = new Color32[canvasTextureSize * canvasTextureSize];

        Color32 bg = defaultBgColor;

        for (int i = 0; i < canvasPixelArray.Length; i++)
            canvasPixelArray[i] = bg;

        BuildDisplayTexture();
        RefreshDisplayPixels();
    }

    private void BuildDisplayTexture()
    {
        if (activeDrawingTexture != null)
            Destroy(activeDrawingTexture);

        activeDrawingTexture = new Texture2D(
            canvasTextureSize,
            canvasTextureSize,
            TextureFormat.RGBA32,
            false
        );

        activeDrawingTexture.name = "CustomAgencyDrawingTexture";
        activeDrawingTexture.filterMode = FilterMode.Bilinear;
        activeDrawingTexture.wrapMode = TextureWrapMode.Clamp;

        canvasRawImage.texture = activeDrawingTexture;
    }

    private void RefreshDisplayPixels()
    {
        activeDrawingTexture.SetPixels32(canvasPixelArray);
        activeDrawingTexture.Apply(false, false);
    }

    // ==========================================
    // BUTTONS
    // ==========================================

    private void InitializeUIButtons()
    {
        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(CommitAgencyChanges);
            submitButton.onClick.AddListener(CommitAgencyChanges);
        }

        if (drawPenButton != null)
            drawPenButton.onClick.AddListener(() => eraserModeActive = false);

        if (eraserToolButton != null)
            eraserToolButton.onClick.AddListener(() => eraserModeActive = true);

        if (paletteColorButtons == null)
            return;

        for (int i = 0; i < paletteColorButtons.Length && i < paletteColors.Length; i++)
        {
            int idx = i;

            if (paletteColorButtons[i] == null)
                continue;

            paletteColorButtons[i].onClick.AddListener(() =>
            {
                activeDrawColor = paletteColors[idx];
                eraserModeActive = false;
            });
        }
    }

    // ==========================================
    // INPUT
    // ==========================================

    public void OnPointerDown(PointerEventData eventData)
    {
        isUserDrawing = true;
        hasValidLastPos = false;

        if (!CalculateTextureUVPosition(eventData, out Vector2 computedPos))
            return;

        previousDrawPos = computedPos;
        hasValidLastPos = true;

        RenderStrokeSegment(computedPos, computedPos);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isUserDrawing)
            return;

        if (!CalculateTextureUVPosition(eventData, out Vector2 computedPos))
        {
            hasValidLastPos = false;
            return;
        }

        if (!hasValidLastPos)
        {
            previousDrawPos = computedPos;
            hasValidLastPos = true;

            RenderStrokeSegment(computedPos, computedPos);
            return;
        }

        RenderStrokeSegment(previousDrawPos, computedPos);
        previousDrawPos = computedPos;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isUserDrawing = false;
        hasValidLastPos = false;
    }

    private bool CalculateTextureUVPosition(
        PointerEventData eventData,
        out Vector2 resultPos)
    {
        resultPos = Vector2.zero;

        Camera activeCam = null;

        if (rootCanvas != null &&
            rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            activeCam = eventData.pressEventCamera;

            if (activeCam == null)
                activeCam = rootCanvas.worldCamera;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            agencyRectTransform,
            eventData.position,
            activeCam,
            out Vector2 localPt))
            return false;

        Rect rectBounds = agencyRectTransform.rect;

        if (rectBounds.width <= 0 || rectBounds.height <= 0)
            return false;

        float normX = (localPt.x - rectBounds.xMin) / rectBounds.width;
        float normY = (localPt.y - rectBounds.yMin) / rectBounds.height;

        if (normX < 0 || normX > 1 || normY < 0 || normY > 1)
            return false;

        Rect uvArea = canvasRawImage.uvRect;

        float uvU = uvArea.x + normX * uvArea.width;
        float uvV = uvArea.y + normY * uvArea.height;

        resultPos = new Vector2(
            Mathf.Clamp01(uvU) * (canvasTextureSize - 1),
            Mathf.Clamp01(uvV) * (canvasTextureSize - 1)
        );

        return true;
    }

    // ==========================================
    // SMOOTH BRUSH
    // ==========================================

    private void RenderStrokeSegment(Vector2 startPoint, Vector2 endPoint)
    {
        float brushRadius = Mathf.Max(0.5f, currentBrushRadius);

        Color32 paintColor = eraserModeActive
            ? (Color32)defaultBgColor
            : (Color32)activeDrawColor;

        int minX = Mathf.Clamp(
            Mathf.FloorToInt(Mathf.Min(startPoint.x, endPoint.x) - brushRadius - 1),
            0,
            canvasTextureSize - 1
        );

        int maxX = Mathf.Clamp(
            Mathf.CeilToInt(Mathf.Max(startPoint.x, endPoint.x) + brushRadius + 1),
            0,
            canvasTextureSize - 1
        );

        int minY = Mathf.Clamp(
            Mathf.FloorToInt(Mathf.Min(startPoint.y, endPoint.y) - brushRadius - 1),
            0,
            canvasTextureSize - 1
        );

        int maxY = Mathf.Clamp(
            Mathf.CeilToInt(Mathf.Max(startPoint.y, endPoint.y) + brushRadius + 1),
            0,
            canvasTextureSize - 1
        );

        Vector2 travelDir = endPoint - startPoint;
        float sqrLength = travelDir.sqrMagnitude;

        for (int yCoord = minY; yCoord <= maxY; yCoord++)
        {
            for (int xCoord = minX; xCoord <= maxX; xCoord++)
            {
                Vector2 samplePoint = new Vector2(xCoord + 0.5f, yCoord + 0.5f);

                float stepT = 0f;

                if (sqrLength > 0.0001f)
                {
                    stepT = Mathf.Clamp01(
                        Vector2.Dot(samplePoint - startPoint, travelDir) /
                        sqrLength
                    );
                }

                Vector2 nearestVector = startPoint + travelDir * stepT;

                float distToPath = Vector2.Distance(samplePoint, nearestVector);

                float pixelCoverage = Mathf.Clamp01(
                    brushRadius + 0.5f - distToPath
                );

                if (pixelCoverage <= 0f)
                    continue;

                int pixelIndex = yCoord * canvasTextureSize + xCoord;

                canvasPixelArray[pixelIndex] = MixPixelColor(
                    canvasPixelArray[pixelIndex],
                    paintColor,
                    pixelCoverage
                );
            }
        }

        RefreshDisplayPixels();
    }

    private Color32 MixPixelColor(
        Color32 sourceColor,
        Color32 targetColor,
        float weight)
    {
        return new Color32(
            (byte)Mathf.RoundToInt(Mathf.Lerp(sourceColor.r, targetColor.r, weight)),
            (byte)Mathf.RoundToInt(Mathf.Lerp(sourceColor.g, targetColor.g, weight)),
            (byte)Mathf.RoundToInt(Mathf.Lerp(sourceColor.b, targetColor.b, weight)),
            255
        );
    }

    // ==========================================
    // CLEAR DRAWING
    // ==========================================

    public void ClearDrawingCanvas()
    {
        Color32 bg = defaultBgColor;

        for (int i = 0; i < canvasPixelArray.Length; i++)
            canvasPixelArray[i] = bg;

        RefreshDisplayPixels();
    }

    // ==========================================
    // JSON HELPERS
    // ==========================================

    private AgencySavePayload GenerateDefaultSaveRecord()
    {
        AgencySavePayload defaultPayload = new AgencySavePayload();

        defaultPayload.version = 2;
        defaultPayload.agencyName = "";
        defaultPayload.logoPng = "";

        defaultPayload.money = 30000000;
        defaultPayload.mood = 50f;

        defaultPayload.day = 1;
        defaultPayload.month = 1;
        defaultPayload.year = 1;

        defaultPayload.timeOfDay = 0f;

        defaultPayload.tutorialStep = 0;
        defaultPayload.tutorialDone = false;

        defaultPayload.upgrades = new int[6];

        defaultPayload.pendingMission = -1;
        defaultPayload.lastResultMission = -1;
        defaultPayload.lastResultSuccess = -1;

        defaultPayload.createdAt = FetchCurrentTimestampString();
        defaultPayload.updatedAt = FetchCurrentTimestampString();

        return defaultPayload;
    }

    private AgencySavePayload LoadSaveRecord()
    {
        if (!File.Exists(JsonFilePath))
            return GenerateDefaultSaveRecord();

        string jsonContent = File.ReadAllText(JsonFilePath);

        AgencySavePayload loadedPayload =
            JsonUtility.FromJson<AgencySavePayload>(jsonContent);

        if (loadedPayload == null)
            throw new Exception("JSON-Datensatz konnte nicht geparst werden!");

        SanitizeSaveRecord(loadedPayload);

        return loadedPayload;
    }

    private void SanitizeSaveRecord(AgencySavePayload payloadData)
    {
        if (payloadData.upgrades == null)
            payloadData.upgrades = new int[6];

        if (payloadData.lastResultText == null)
            payloadData.lastResultText = "";

        if (payloadData.agencyName == null)
            payloadData.agencyName = "";

        if (payloadData.logoPng == null)
            payloadData.logoPng = "";

        if (payloadData.lastGovernmentGrantDate == null)
            payloadData.lastGovernmentGrantDate = "";

        if (payloadData.lastAdvertisementPayoutDate == null)
            payloadData.lastAdvertisementPayoutDate = "";

        if (payloadData.lastAdvertisementMoodDate == null)
            payloadData.lastAdvertisementMoodDate = "";

        if (string.IsNullOrEmpty(payloadData.createdAt))
            payloadData.createdAt = FetchCurrentTimestampString();
    }

    private string FetchCurrentTimestampString()
    {
        return DateTime.Now.ToString(
            "yyyy-MM-dd HH:mm:ss",
            CultureInfo.InvariantCulture
        );
    }

    private void SerializeAndWriteJson(AgencySavePayload payloadData)
    {
        string serializedJson = JsonUtility.ToJson(payloadData, true);

        string tempFilePath = JsonFilePath + ".tmp";

        File.WriteAllText(tempFilePath, serializedJson);

        if (File.Exists(JsonFilePath))
            File.Delete(JsonFilePath);

        File.Move(tempFilePath, JsonFilePath);
    }

    // ==========================================
    // SAVE AGENCY + COMPLETE JSON
    // ==========================================

    public void CommitAgencyChanges()
    {
        string inputAgencyTitle = nameInputField != null
            ? nameInputField.text.Trim()
            : "";

        if (string.IsNullOrWhiteSpace(inputAgencyTitle))
        {
            Debug.LogWarning("Bitte Agency-Namen eingeben!");
            return;
        }

        if (canvasPixelArray == null)
        {
            Debug.LogError("Pixelbuffer fehlt!");
            return;
        }

        if (lastSaveFrameIndex == Time.frameCount)
        {
            Debug.LogWarning("Doppelter Speichervorgang in diesem Frame verhindert.");
            return;
        }

        try
        {
            Directory.CreateDirectory(StorageDirectory);

            // ==================================
            // 1. CHECK DRAWING
            // ==================================

            int nonBgPixelCount = 0;
            Color32 bg = defaultBgColor;

            foreach (Color32 pix in canvasPixelArray)
            {
                if (!pix.Equals(bg))
                    nonBgPixelCount++;
            }

            if (nonBgPixelCount == 0)
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
                canvasTextureSize,
                canvasTextureSize,
                TextureFormat.RGBA32,
                false
            );

            exportTexture.SetPixels32(canvasPixelArray);
            exportTexture.Apply(false, false);

            byte[] encodedPngBytes = exportTexture.EncodeToPNG();

            Destroy(exportTexture);

            if (encodedPngBytes == null || encodedPngBytes.Length == 0)
            {
                Debug.LogError("PNG-Encoding fehlgeschlagen!");
                return;
            }

            string base64LogoString = Convert.ToBase64String(encodedPngBytes);

            // ==================================
            // 3. LOAD EXISTING GAME DATA
            // ==================================

            AgencySavePayload targetPayload;

            try
            {
                targetPayload = LoadSaveRecord();
            }
            catch (Exception parseEx)
            {
                Debug.LogWarning(
                    "Vorhandener Spielstand beschädigt. Erstelle frische Basisdaten: " + parseEx
                );
                targetPayload = GenerateDefaultSaveRecord();
            }

            // ==================================
            // 4. UPDATE ONLY AGENCY NAME, LOGO & TIMESTAMP
            // ==================================

            targetPayload.agencyName = inputAgencyTitle;
            targetPayload.logoPng = base64LogoString;

            SanitizeSaveRecord(targetPayload);

            targetPayload.updatedAt = FetchCurrentTimestampString();

            // ==================================
            // 5. SAVE IMAGE FILES
            // ==================================

            byte[] rawBufferBytes = new byte[canvasPixelArray.Length * 4];

            for (int i = 0; i < canvasPixelArray.Length; i++)
            {
                int byteOffset = i * 4;
                Color32 colorVal = canvasPixelArray[i];

                rawBufferBytes[byteOffset] = colorVal.r;
                rawBufferBytes[byteOffset + 1] = colorVal.g;
                rawBufferBytes[byteOffset + 2] = colorVal.b;
                rawBufferBytes[byteOffset + 3] = colorVal.a;
            }

            File.WriteAllBytes(RawFilePath, rawBufferBytes);
            File.WriteAllBytes(PngFilePath, encodedPngBytes);

            // ==================================
            // 6. SAVE JSON
            // ==================================

            SerializeAndWriteJson(targetPayload);

            // ==================================
            // 7. SAVE PLAYERPREFS
            // ==================================

            PlayerPrefs.SetString(
                "SavedAgencyName",
                inputAgencyTitle
            );

            PlayerPrefs.SetString(
                "SavedAgencyLogoPath",
                RawFilePath
            );

            PlayerPrefs.SetString(
                "SavedAgencyPngPath",
                PngFilePath
            );

            PlayerPrefs.SetInt(
                "SavedAgencyTextureSize",
                canvasTextureSize
            );

            PlayerPrefs.SetInt(
                "HasAgencySaveState",
                1
            );

            PlayerPrefs.Save();

            lastSaveFrameIndex = Time.frameCount;

            // ==================================
            // 8. LOG
            // ==================================

            Debug.Log("========== AGENCY SAVE SUCCESS ==========");
            Debug.Log("Agency: " + inputAgencyTitle);
            Debug.Log("Drawn pixels: " + nonBgPixelCount);
            Debug.Log("PNG bytes: " + encodedPngBytes.Length);
            Debug.Log("Base64 length: " + base64LogoString.Length);
            Debug.Log("JSON: " + JsonFilePath);
            Debug.Log("RAW: " + RawFilePath);
            Debug.Log("PNG: " + PngFilePath);
            Debug.Log("Money preserved: " + targetPayload.money);
            Debug.Log("Mood preserved: " + targetPayload.mood);
            Debug.Log("Launches preserved: " + targetPayload.totalLaunches);
            Debug.Log("=========================================");
        }
        catch (Exception err)
        {
            Debug.LogError("Agency save failed: " + err);
        }
    }

    // ==========================================
    // LOAD EXISTING DATA
    // ==========================================

    private void RestoreExistingSaveData()
    {
        AgencySavePayload targetPayload = null;

        if (File.Exists(JsonFilePath))
        {
            try
            {
                targetPayload = LoadSaveRecord();

                if (nameInputField != null)
                    nameInputField.text = targetPayload.agencyName;

                if (!string.IsNullOrEmpty(targetPayload.logoPng))
                {
                    byte[] pngBuffer =
                        Convert.FromBase64String(targetPayload.logoPng);

                    if (DecodeAndApplyPngTexture(pngBuffer))
                    {
                        Debug.Log("Agency logo loaded from JSON!");
                        return;
                    }
                }
            }
            catch (Exception loadEx)
            {
                Debug.LogError("JSON load error: " + loadEx);
            }
        }

        if (nameInputField != null && targetPayload == null)
        {
            nameInputField.text =
                PlayerPrefs.GetString("SavedAgencyName", "");
        }

        string rawPrefPath = PlayerPrefs.GetString(
            "SavedAgencyLogoPath",
            RawFilePath
        );

        if (File.Exists(rawPrefPath))
        {
            ReadAndApplyRawTexture(rawPrefPath);
        }
    }

    private bool DecodeAndApplyPngTexture(byte[] pngBytes)
    {
        Texture2D tempLoadedTex = new Texture2D(2, 2);

        if (!tempLoadedTex.LoadImage(pngBytes))
        {
            Destroy(tempLoadedTex);
            return false;
        }

        if (tempLoadedTex.width != tempLoadedTex.height ||
            tempLoadedTex.width < 16 ||
            tempLoadedTex.width > 4096)
        {
            Destroy(tempLoadedTex);
            Debug.LogWarning("Ungültige Logo-Auflösung.");
            return false;
        }

        canvasTextureSize = tempLoadedTex.width;

        canvasPixelArray = tempLoadedTex.GetPixels32();

        Destroy(tempLoadedTex);

        BuildDisplayTexture();
        RefreshDisplayPixels();

        return true;
    }

    private bool ReadAndApplyRawTexture(string targetPath)
    {
        try
        {
            int savedResSize = PlayerPrefs.GetInt(
                "SavedAgencyTextureSize",
                canvasTextureSize
            );

            byte[] rawBytes = File.ReadAllBytes(targetPath);

            long expectedDataLength =
                (long)savedResSize * savedResSize * 4;

            if (savedResSize < 16 ||
                savedResSize > 4096 ||
                rawBytes.LongLength != expectedDataLength)
            {
                Debug.LogError("Ungültige RAW-Datei!");
                return false;
            }

            Color32[] restoredPixels =
                new Color32[savedResSize * savedResSize];

            for (int i = 0; i < restoredPixels.Length; i++)
            {
                int offsetIdx = i * 4;

                restoredPixels[i] = new Color32(
                    rawBytes[offsetIdx],
                    rawBytes[offsetIdx + 1],
                    rawBytes[offsetIdx + 2],
                    rawBytes[offsetIdx + 3]
                );
            }

            canvasTextureSize = savedResSize;
            canvasPixelArray = restoredPixels;

            BuildDisplayTexture();
            RefreshDisplayPixels();

            Debug.Log("RAW texture loaded successfully!");

            return true;
        }
        catch (Exception rawEx)
        {
            Debug.LogError("RAW load failed: " + rawEx);
            return false;
        }
    }

    // ==========================================
    // PUBLIC ACCESS
    // ==========================================

    public Texture2D GetDrawingTexture()
    {
        return activeDrawingTexture;
    }

    public string GetSaveFilePath()
    {
        return JsonFilePath;
    }

    private void OnDestroy()
    {
        if (activeDrawingTexture != null)
            Destroy(activeDrawingTexture);
    }
}