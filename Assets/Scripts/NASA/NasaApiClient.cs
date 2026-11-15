using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>Antwort von NASA APOD (Astronomy Picture of the Day).</summary>
[Serializable]
public class NasaApodData
{
    public string date;
    public string title;
    public string explanation;
    public string url;
    public string hdurl;
    public string thumbnail_url;
    public string media_type;
    public string copyright;
    public string service_version;
}

/// <summary>Ein Bild der NASA EPIC-Kamera (DSCOVR, volle Erdscheibe).</summary>
[Serializable]
public class NasaEpicImage
{
    public string identifier;
    public string caption;
    public string image;
    public string date;
    public NasaEpicCentroid centroid_coordinates;
}

[Serializable]
public class NasaEpicCentroid
{
    public float lat;
    public float lon;
}

[Serializable]
public class NasaEpicWrapper
{
    public NasaEpicImage[] items;
}

/// <summary>Antwort von NASA NeoWs (Near Earth Objects).</summary>
[Serializable]
public class NasaNeoFeed
{
    public int element_count;
}

/// <summary>
/// Lädt echte NASA-Daten und cached sie für einen Tag:
///
/// 1. APOD (Astronomy Picture of the Day) über api.nasa.gov
/// 2. EPIC-Erdbild über epic.gsfc.nasa.gov, falls APOD nicht verfügbar ist
/// 3. NeoWs: Anzahl der Near-Earth Objects des Tages
///
/// Der Dienst ist persistent und wird automatisch beim Spielstart erzeugt.
/// </summary>
[DefaultExecutionOrder(-400)]
public class NasaApiClient : MonoBehaviour
{
    public static NasaApiClient Instance { get; private set; }

    private const string ApodEndpoint = "https://api.nasa.gov/planetary/apod";
    private const string NeoEndpoint = "https://api.nasa.gov/neo/rest/v1/feed";
    private const string EpicEndpoint = "https://epic.gsfc.nasa.gov/api/natural";
    private const string EpicArchive = "https://epic.gsfc.nasa.gov/archive/natural";

    private const string ApodJsonKey = "Nasa.Apod.Json";
    private const string ApodFetchedKey = "Nasa.Apod.FetchedOn";
    private const string EpicJsonKey = "Nasa.Epic.Json";
    private const string EpicFetchedKey = "Nasa.Epic.FetchedOn";
    private const string NeoCountKey = "Nasa.Neo.Count";
    private const string NeoFetchedKey = "Nasa.Neo.FetchedOn";

    [Header("NASA API")]
    [Tooltip("API-Key von api.nasa.gov. Leer = DEMO_KEY.")]
    [SerializeField] private string apiKey =
        "MmDDwrc6mzmwBp7JdKjLMqzF7z9jcRcRMCLMzjM7";

    [Tooltip("Sekunden bis ein JSON-Request abgebrochen wird.")]
    [SerializeField] private int requestTimeout = 15;

    [Tooltip("Sekunden bis ein Bild-Download abgebrochen wird.")]
    [SerializeField] private int imageTimeout = 60;

    public NasaApodData Apod { get; private set; }
    public Texture2D ApodTexture { get; private set; }

    public NasaEpicImage Epic { get; private set; }
    public Texture2D EpicTexture { get; private set; }

    public int NearEarthObjectCount { get; private set; } = -1;
    public bool IsLoading { get; private set; }
    public string LastError { get; private set; }

    /// <summary>Wird bei Ladebeginn, nach jedem Teil-Ergebnis und am Ende gefeuert.</summary>
    public event Action Updated;

    private string EffectiveApiKey =>
        string.IsNullOrWhiteSpace(apiKey) ? "DEMO_KEY" : apiKey.Trim();

    private static string TodayKey =>
        DateTime.UtcNow.ToString("yyyy-MM-dd");

    // =========================================================
    // BOOTSTRAP
    // =========================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        if (FindAnyObjectByType<NasaApiClient>() != null)
            return;

        new GameObject("NasaApiClient").AddComponent<NasaApiClient>();
    }

    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadCachedApod();
        LoadCachedEpic();
    }

    private void Start()
    {
        StartCoroutine(FetchAll(false));
    }

    // =========================================================
    // PUBLIC API - AKTIVE QUELLE
    // =========================================================

    /// <summary>APOD ist echt (kein "NASA Science"-Platzhalter).</summary>
    public bool HasApodContent =>
        Apod != null &&
        !string.IsNullOrEmpty(Apod.title) &&
        !IsApodPlaceholder(Apod);

    public bool HasEpicContent =>
        Epic != null &&
        !string.IsNullOrEmpty(Epic.image) &&
        EpicTexture != null;

    public bool HasAnyContent => HasApodContent || HasEpicContent;

    public bool ActiveIsVideo =>
        HasApodContent && Apod.media_type == "video";

    public Texture2D ActiveTexture =>
        HasApodContent ? ApodTexture : EpicTexture;

    public string ActiveTitle
    {
        get
        {
            if (HasApodContent)
                return Apod.title;

            if (HasEpicContent)
                return "EARTH FROM DSCOVR";

            return "NASA BRIEFING";
        }
    }

    public string ActiveMeta
    {
        get
        {
            if (HasApodContent)
            {
                string credit = string.IsNullOrEmpty(Apod.copyright)
                    ? "NASA"
                    : Apod.copyright.Trim();

                return (string.IsNullOrEmpty(Apod.date) ? "" : Apod.date + "  ·  ") +
                       credit;
            }

            if (Epic != null)
                return EpicDate + "  ·  NASA / NOAA DSCOVR";

            return "api.nasa.gov";
        }
    }

    public string ActiveBody
    {
        get
        {
            if (HasApodContent)
            {
                return string.IsNullOrEmpty(Apod.explanation)
                    ? "No explanation available."
                    : Apod.explanation;
            }

            if (HasEpicContent)
            {
                string caption = string.IsNullOrEmpty(Epic.caption)
                    ? "Latest full-disc image of Earth from NASA's EPIC camera."
                    : Epic.caption;

                if (Epic.centroid_coordinates != null)
                {
                    caption += "\n\nCentroid: " +
                        Epic.centroid_coordinates.lat.ToString("F1") +
                        "° lat, " +
                        Epic.centroid_coordinates.lon.ToString("F1") +
                        "° lon";
                }

                return caption;
            }

            return "Contacting api.nasa.gov. The NASA briefing is downloaded " +
                   "once per day and cached locally.";
        }
    }

    public string ActiveUrl
    {
        get
        {
            if (HasApodContent)
            {
                if (!string.IsNullOrEmpty(Apod.hdurl))
                    return Apod.hdurl;

                if (!string.IsNullOrEmpty(Apod.url))
                    return Apod.url;
            }

            return "https://epic.gsfc.nasa.gov/";
        }
    }

    public string EpicDate
    {
        get
        {
            if (Epic == null || string.IsNullOrEmpty(Epic.date))
                return "";

            return Epic.date.Length >= 10
                ? Epic.date.Substring(0, 10)
                : Epic.date;
        }
    }

    // =========================================================
    // PUBLIC API - AKTIONEN
    // =========================================================

    public void Refresh()
    {
        StartCoroutine(FetchAll(true));
    }

    // =========================================================
    // CACHE
    // =========================================================

    private void LoadCachedApod()
    {
        if (PlayerPrefs.GetString(ApodFetchedKey, string.Empty) != TodayKey)
            return;

        string json = PlayerPrefs.GetString(ApodJsonKey, string.Empty);

        if (string.IsNullOrEmpty(json))
            return;

        try
        {
            Apod = JsonUtility.FromJson<NasaApodData>(json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[NASA] APOD-Cache konnte nicht gelesen werden: " +
                exception.Message
            );
        }

        if (HasApodContent)
        {
            StartCoroutine(LoadCachedApodTexture());
        }
        else
        {
            // Platzhalter aus einem früheren Lauf verwerfen.
            Apod = null;
            PlayerPrefs.DeleteKey(ApodFetchedKey);
            PlayerPrefs.DeleteKey(ApodJsonKey);
            PlayerPrefs.Save();
        }
    }

    private static bool IsApodPlaceholder(NasaApodData data)
    {
        if (data == null)
            return true;

        if (string.Equals(
            data.title,
            "NASA Science",
            StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return data.explanation != null &&
               data.explanation.StartsWith(
                   "Discover the cosmos!",
                   StringComparison.Ordinal
               );
    }

    private void LoadCachedEpic()
    {
        if (PlayerPrefs.GetString(EpicFetchedKey, string.Empty) != TodayKey)
            return;

        string json = PlayerPrefs.GetString(EpicJsonKey, string.Empty);

        if (string.IsNullOrEmpty(json))
            return;

        try
        {
            NasaEpicWrapper wrapper = JsonUtility.FromJson<NasaEpicWrapper>(
                "{\"items\":" + json + "}"
            );

            if (wrapper != null &&
                wrapper.items != null &&
                wrapper.items.Length > 0)
            {
                Epic = wrapper.items[0];
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[NASA] EPIC-Cache konnte nicht gelesen werden: " +
                exception.Message
            );
        }
    }

    private IEnumerator LoadCachedApodTexture()
    {
        if (Apod == null)
            yield break;

        string path = GetApodCachePath();

        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            yield break;

        Texture2D texture = LoadTextureFile(path);

        if (texture == null)
            yield break;

        ApodTexture = texture;
        Updated?.Invoke();
    }

    private string GetApodCachePath()
    {
        if (Apod == null || string.IsNullOrEmpty(Apod.date))
            return null;

        return Path.Combine(
            Application.persistentDataPath,
            "nasa_apod_" + SafeName(Apod.date) + ".img"
        );
    }

    private string GetEpicCachePath()
    {
        if (Epic == null || string.IsNullOrEmpty(Epic.identifier))
            return null;

        return Path.Combine(
            Application.persistentDataPath,
            "nasa_epic_" + SafeName(Epic.identifier) + ".img"
        );
    }

    private static string SafeName(string value)
    {
        return value
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace(":", "-")
            .Replace(" ", "_");
    }

    private static Texture2D LoadTextureFile(string path)
    {
        try
        {
            byte[] bytes = File.ReadAllBytes(path);

            if (bytes == null || bytes.Length == 0)
                return null;

            Texture2D texture = new Texture2D(
                2, 2, TextureFormat.RGBA32, false
            );

            if (texture.LoadImage(bytes))
                return texture;

            Destroy(texture);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[NASA] Bild-Cache konnte nicht gelesen werden: " +
                exception.Message
            );
        }

        return null;
    }

    // =========================================================
    // FETCH
    // =========================================================

    private IEnumerator FetchAll(bool force)
    {
        if (IsLoading)
            yield break;

        IsLoading = true;
        LastError = null;
        Updated?.Invoke();

        bool apodFresh =
            !force &&
            PlayerPrefs.GetString(ApodFetchedKey, string.Empty) == TodayKey &&
            Apod != null;

        if (!apodFresh)
        {
            yield return FetchApod();
        }
        else if (HasApodContent && ApodTexture == null)
        {
            yield return DownloadApodTexture();
        }

        // Wenn APOD nichts Brauchbares liefert: echtes EPIC-Erdbild holen.
        if (!HasApodContent)
        {
            bool epicFresh =
                !force &&
                PlayerPrefs.GetString(EpicFetchedKey, string.Empty) == TodayKey &&
                Epic != null;

            if (!epicFresh)
            {
                yield return FetchEpic();
            }
            else if (EpicTexture == null)
            {
                yield return DownloadEpicTexture();
            }
        }

        bool neoFresh =
            !force &&
            PlayerPrefs.GetString(NeoFetchedKey, string.Empty) == TodayKey;

        if (!neoFresh)
            yield return FetchNeoCount();

        if (!HasAnyContent && string.IsNullOrEmpty(LastError))
        {
            LastError =
                "No NASA data available. Check the internet connection.";
        }

        IsLoading = false;
        Updated?.Invoke();
    }

    private IEnumerator FetchApod()
    {
        // Heute versuchen, sonst den Vortag (APOD erscheint je nach
        // Zeitzone manchmal erst später).
        for (int daysBack = 0; daysBack <= 1; daysBack++)
        {
            string date = DateTime.UtcNow
                .AddDays(-daysBack)
                .ToString("yyyy-MM-dd");

            string url =
                ApodEndpoint +
                "?api_key=" + UnityWebRequest.EscapeURL(EffectiveApiKey) +
                "&thumbs=true";

            if (daysBack > 0)
                url += "&date=" + date;

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = requestTimeout;

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    if (daysBack == 0)
                    {
                        Debug.LogWarning(
                            "[NASA] APOD für heute nicht verfügbar (" +
                            request.error + "), versuche Vortag."
                        );
                    }

                    continue;
                }

                NasaApodData data = null;

                try
                {
                    data = JsonUtility.FromJson<NasaApodData>(
                        request.downloadHandler.text
                    );
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        "[NASA] APOD parse failed: " + exception.Message
                    );

                    continue;
                }

                if (data == null || string.IsNullOrEmpty(data.title))
                    continue;

                if (IsApodPlaceholder(data))
                {
                    Debug.LogWarning(
                        "[NASA] APOD liefert Platzhalter für " + date +
                        ", versuche Vortag."
                    );

                    continue;
                }

                Apod = data;

                PlayerPrefs.SetString(
                    ApodJsonKey,
                    request.downloadHandler.text
                );

                PlayerPrefs.SetString(ApodFetchedKey, TodayKey);
                PlayerPrefs.Save();

                Updated?.Invoke();

                if (HasApodContent)
                    yield return DownloadApodTexture();

                yield break;
            }
        }

        Debug.LogWarning("[NASA] APOD konnte nicht geladen werden.");
    }

    private IEnumerator DownloadApodTexture()
    {
        string imageUrl = ApodImageUrl;

        if (string.IsNullOrEmpty(imageUrl))
        {
            ApodTexture = null;
            yield break;
        }

        string cachePath = GetApodCachePath();

        if (!string.IsNullOrEmpty(cachePath) && File.Exists(cachePath))
        {
            Texture2D cached = LoadTextureFile(cachePath);

            if (cached != null)
            {
                ApodTexture = cached;
                Updated?.Invoke();
                yield break;
            }
        }

        using (UnityWebRequest request =
            UnityWebRequestTexture.GetTexture(imageUrl, true))
        {
            request.timeout = imageTimeout;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(
                    "[NASA] APOD image failed: " + request.error
                );

                yield break;
            }

            ApodTexture = DownloadHandlerTexture.GetContent(request);

            SaveTextureCache(cachePath, request.downloadHandler.data);
            Updated?.Invoke();
        }
    }

    private IEnumerator FetchEpic()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(EpicEndpoint))
        {
            request.timeout = requestTimeout;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(
                    "[NASA] EPIC request failed: " + request.error
                );

                yield break;
            }

            string json = request.downloadHandler.text;

            try
            {
                NasaEpicWrapper wrapper = JsonUtility.FromJson<NasaEpicWrapper>(
                    "{\"items\":" + json + "}"
                );

                if (wrapper == null ||
                    wrapper.items == null ||
                    wrapper.items.Length == 0)
                {
                    Debug.LogWarning("[NASA] EPIC response was empty.");
                    yield break;
                }

                Epic = wrapper.items[0];

                PlayerPrefs.SetString(EpicJsonKey, json);
                PlayerPrefs.SetString(EpicFetchedKey, TodayKey);
                PlayerPrefs.Save();

                Updated?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[NASA] EPIC parse failed: " + exception.Message
                );

                yield break;
            }
        }

        yield return DownloadEpicTexture();
    }

    private IEnumerator DownloadEpicTexture()
    {
        string imageUrl = GetEpicImageUrl();

        if (string.IsNullOrEmpty(imageUrl))
        {
            EpicTexture = null;
            yield break;
        }

        string cachePath = GetEpicCachePath();

        if (!string.IsNullOrEmpty(cachePath) && File.Exists(cachePath))
        {
            Texture2D cached = LoadTextureFile(cachePath);

            if (cached != null)
            {
                EpicTexture = cached;
                Updated?.Invoke();
                yield break;
            }
        }

        using (UnityWebRequest request =
            UnityWebRequestTexture.GetTexture(imageUrl, true))
        {
            request.timeout = imageTimeout;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(
                    "[NASA] EPIC image failed: " + request.error
                );

                yield break;
            }

            EpicTexture = DownloadHandlerTexture.GetContent(request);

            SaveTextureCache(cachePath, request.downloadHandler.data);
            Updated?.Invoke();
        }
    }

    private string GetEpicImageUrl()
    {
        if (Epic == null ||
            string.IsNullOrEmpty(Epic.image) ||
            string.IsNullOrEmpty(Epic.date) ||
            Epic.date.Length < 10)
        {
            return null;
        }

        string datePart = Epic.date.Substring(0, 10);
        string[] parts = datePart.Split('-');

        if (parts.Length != 3)
            return null;

        return EpicArchive +
               "/" + parts[0] +
               "/" + parts[1] +
               "/" + parts[2] +
               "/png/" + Epic.image + ".png";
    }

    private static void SaveTextureCache(string path, byte[] bytes)
    {
        if (string.IsNullOrEmpty(path) || bytes == null || bytes.Length == 0)
            return;

        try
        {
            File.WriteAllBytes(path, bytes);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[NASA] Bild-Cache konnte nicht geschrieben werden: " +
                exception.Message
            );
        }
    }

    private IEnumerator FetchNeoCount()
    {
        string url =
            NeoEndpoint +
            "?start_date=" + TodayKey +
            "&end_date=" + TodayKey +
            "&api_key=" + UnityWebRequest.EscapeURL(EffectiveApiKey);

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = requestTimeout;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                // NEO ist optional - Fehler nicht als Hauptfehler anzeigen.
                Debug.LogWarning(
                    "[NASA] NEO request failed: " + request.error
                );

                yield break;
            }

            NasaNeoFeed feed = null;

            try
            {
                feed = JsonUtility.FromJson<NasaNeoFeed>(
                    request.downloadHandler.text
                );
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[NASA] NEO parse failed: " + exception.Message
                );

                yield break;
            }

            if (feed == null)
                yield break;

            NearEarthObjectCount = feed.element_count;

            PlayerPrefs.SetInt(NeoCountKey, NearEarthObjectCount);
            PlayerPrefs.SetString(NeoFetchedKey, TodayKey);
            PlayerPrefs.Save();

            Updated?.Invoke();
        }
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private string ApodImageUrl
    {
        get
        {
            if (Apod == null)
                return null;

            if (Apod.media_type == "video")
            {
                return string.IsNullOrEmpty(Apod.thumbnail_url)
                    ? null
                    : Apod.thumbnail_url;
            }

            return string.IsNullOrEmpty(Apod.url) ? null : Apod.url;
        }
    }
}
