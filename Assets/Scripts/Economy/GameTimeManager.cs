using System;
using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>
/// Uhrzeit-Manager für TextMeshPro.
///
/// - schreibt die Uhrzeit fortlaufend in ein TMP_Text
///   (Beispiel Format: 11:11 PM)
/// - meldet jeden neuen Tag um 00:00 an den AgencyEconomyManager,
///   damit das Regierungsgeld ausgezahlt wird
/// - kann entweder die echte Systemzeit oder eine beschleunigte
///   Spielzeit aus der space_agency_save.json verwenden
/// </summary>
[DefaultExecutionOrder(-150)]
public class GameTimeManager : MonoBehaviour
{
    private static GameTimeManager existingInstance;

    /// <summary>
    /// Bereits vorhandene Instanz, ohne automatisch eine neue zu erzeugen.
    /// </summary>
    public static GameTimeManager ExistingInstance =>
        existingInstance;

    // =========================================================
    // INSPECTOR
    // =========================================================

    [Header("TextMeshPro")]
    [Tooltip("Hier wird die Uhrzeit angezeigt, z. B. 11:11 PM.")]
    public TMP_Text timeText;

    [Tooltip("Optional: Hier wird zusätzlich das Datum angezeigt.")]
    public TMP_Text dateText;

    [Header("Zeitquelle")]
    [Tooltip("Echte Systemzeit benutzen. Sonst läuft die Spielzeit beschleunigt.")]
    public bool useSystemTime = false;

    [Tooltip("Beim Start die gespeicherte Spielzeit aus der JSON laden.")]
    public bool useSavedGameTime = true;

    [Tooltip("Echte Sekunden, die ein kompletter Spieltag dauert.")]
    [Min(1f)]
    public float secondsPerGameDay = 600f;

    [Tooltip("Startstunde, falls keine gespeicherte Spielzeit vorhanden ist.")]
    [Range(0, 23)]
    public int fallbackStartHour = 0;

    [Tooltip("Startminute, falls keine gespeicherte Spielzeit vorhanden ist.")]
    [Range(0, 59)]
    public int fallbackStartMinute = 0;

    [Header("Formatierung")]
    [Tooltip("TMP-Format für die Uhrzeit. 'h:mm tt' ergibt z. B. 11:11 PM.")]
    public string timeFormat = "h:mm tt";

    [Tooltip("TMP-Format für das optionale Datum.")]
    public string dateFormat = "dd.MM.yyyy";

    [Header("Speichern")]
    [Tooltip("Manager über Szenenwechsel hinweg behalten.")]
    public bool persistAcrossScenes = true;

    [Tooltip("Spielzeit regelmäßig in die space_agency_save.json schreiben.")]
    public bool syncGameTimeToSave = true;

    [Tooltip("Wie oft die Spielzeit in die JSON geschrieben wird.")]
    [Min(1f)]
    public float clockSaveInterval = 5f;

    [Header("Wirtschaft")]
    [Tooltip("Bei jedem neuen Tag automatisch das Regierungsgeld auszahlen.")]
    public bool processDailyEconomy = true;

    [Header("Runtime")]
    [Tooltip("Unabhängig von Time.timeScale laufen.")]
    public bool useUnscaledTime = true;

    public bool IsPaused { get; private set; }

    public DateTime CurrentTime { get; private set; }

    public DateTime CurrentDate => CurrentTime.Date;

    public event Action<DateTime> NewDay;
    public event Action<DateTime> TimeChanged;

    // =========================================================
    // STATE
    // =========================================================

    private DateTime lastDay;
    private string lastTimeString = "";
    private string lastDateString = "";

    private float clockSaveTimer;

    private bool initialized;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        if (existingInstance != null && existingInstance != this)
        {
            // Neue Szene geladen: vorhandene, persistente Uhr übernimmt die
            // frischen Text-Referenzen und läuft nahtlos weiter.
            existingInstance.AdoptSceneBindings(this);

            Destroy(gameObject);
            return;
        }

        existingInstance = this;

        // Immer persistent machen und aus einem Elternobjekt lösen,
        // damit die Zeit über Szenenwechsel hinweg erhalten bleibt.
        if (persistAcrossScenes)
        {
            if (transform.parent != null)
                transform.SetParent(null, true);

            DontDestroyOnLoad(gameObject);
        }

        InitializeClock();
    }

    /// <summary>
    /// Übernimmt Zeit- und Datumsanzeige aus einer neu geladenen Szene,
    /// ohne die laufende Spielzeit zurückzusetzen.
    /// </summary>
    private void AdoptSceneBindings(GameTimeManager sceneInstance)
    {
        if (sceneInstance == null)
            return;

        if (sceneInstance.timeText != null)
            timeText = sceneInstance.timeText;

        if (sceneInstance.dateText != null)
            dateText = sceneInstance.dateText;

        if (syncGameTimeToSave && !useSystemTime)
            SaveClockToJson();

        UpdateText(true);
    }

    private void Update()
    {
        if (!initialized || IsPaused)
            return;

        float deltaTime = useUnscaledTime
            ? Time.unscaledDeltaTime
            : Time.deltaTime;

        if (useSystemTime)
        {
            CurrentTime = DateTime.Now;
        }
        else
        {
            double gameSeconds =
                deltaTime * 86400.0 /
                Mathf.Max(1f, secondsPerGameDay);

            CurrentTime = CurrentTime.AddSeconds(gameSeconds);
        }

        if (CurrentTime.Date != lastDay)
        {
            lastDay = CurrentTime.Date;

            if (syncGameTimeToSave && !useSystemTime)
                SaveClockToJson();

            if (processDailyEconomy)
            {
                AgencyEconomyManager economy =
                    AgencyEconomyManager.Instance;

                if (economy != null)
                    economy.ProcessDate(CurrentDate);
            }

            NewDay?.Invoke(CurrentDate);
        }

        UpdateText(false);

        if (!useSystemTime && syncGameTimeToSave)
        {
            clockSaveTimer += deltaTime;

            if (clockSaveTimer >= Mathf.Max(1f, clockSaveInterval))
            {
                clockSaveTimer = 0f;
                SaveClockToJson();
            }
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            SaveClockToJson();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            SaveClockToJson();
    }

    private void OnApplicationQuit()
    {
        SaveClockToJson();
    }

    private void OnDestroy()
    {
        if (existingInstance == this)
            existingInstance = null;
    }

    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void InitializeClock()
    {
        if (useSystemTime)
        {
            CurrentTime = DateTime.Now;
        }
        else
        {
            bool loadedFromSave = false;

            if (useSavedGameTime)
            {
                int year;
                int month;
                int day;
                float timeOfDay;

                if (AgencyEconomyManager.Instance.TryGetGameTime(
                    out year,
                    out month,
                    out day,
                    out timeOfDay))
                {
                    try
                    {
                        CurrentTime = new DateTime(year, month, day);

                        CurrentTime = CurrentTime.Add(
                            TimeSpan.FromDays(timeOfDay)
                        );

                        loadedFromSave = true;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning(
                            "[GameTimeManager] Gespeicherte Spielzeit " +
                            "konnte nicht geladen werden: " +
                            exception.Message
                        );
                    }
                }
            }

            if (!loadedFromSave)
            {
                CurrentTime = DateTime.Today;

                CurrentTime = CurrentTime.AddHours(fallbackStartHour);
                CurrentTime = CurrentTime.AddMinutes(fallbackStartMinute);
            }
        }

        lastDay = CurrentTime.Date;

        UpdateText(true);

        if (processDailyEconomy)
        {
            AgencyEconomyManager economy = AgencyEconomyManager.Instance;

            if (economy != null)
                economy.ProcessDate(CurrentDate);
        }

        initialized = true;
    }

    // =========================================================
    // TEXT
    // =========================================================

    private void UpdateText(bool force)
    {
        if (timeText != null)
        {
            string formattedTime = CurrentTime.ToString(
                timeFormat,
                CultureInfo.InvariantCulture
            );

            if (force || formattedTime != lastTimeString)
            {
                lastTimeString = formattedTime;
                timeText.text = formattedTime;
                TimeChanged?.Invoke(CurrentTime);
            }
        }

        if (dateText != null)
        {
            string formattedDate = CurrentTime.ToString(
                dateFormat,
                CultureInfo.InvariantCulture
            );

            if (force || formattedDate != lastDateString)
            {
                lastDateString = formattedDate;
                dateText.text = formattedDate;
            }
        }
    }

    // =========================================================
    // PUBLIC CONTROL
    // =========================================================

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
    }

    public void TogglePaused()
    {
        IsPaused = !IsPaused;
    }

    public void SetTime(int hour, int minute, int second = 0)
    {
        DateTime updated = new DateTime(
            CurrentTime.Year,
            CurrentTime.Month,
            CurrentTime.Day,
            Mathf.Clamp(hour, 0, 23),
            Mathf.Clamp(minute, 0, 59),
            Mathf.Clamp(second, 0, 59)
        );

        SetDateTime(updated);
    }

    public void SetDateTime(DateTime value)
    {
        CurrentTime = value;

        UpdateText(true);

        if (CurrentTime.Date == lastDay)
            return;

        lastDay = CurrentTime.Date;

        if (syncGameTimeToSave && !useSystemTime)
            SaveClockToJson();

        if (processDailyEconomy)
        {
            AgencyEconomyManager economy = AgencyEconomyManager.Instance;

            if (economy != null)
                economy.ProcessDate(CurrentDate);
        }

        NewDay?.Invoke(CurrentDate);
    }

    public void AdvanceDays(int days)
    {
        if (days == 0)
            return;

        SetDateTime(CurrentTime.AddDays(days));
    }

    public void AdvanceMinutes(double minutes)
    {
        if (Math.Abs(minutes) < 0.0001)
            return;

        SetDateTime(CurrentTime.AddMinutes(minutes));
    }

    public string GetFormattedTime()
    {
        return CurrentTime.ToString(
            timeFormat,
            CultureInfo.InvariantCulture
        );
    }

    // =========================================================
    // SAVE
    // =========================================================

    public void SaveClockToJson()
    {
        if (useSystemTime || !syncGameTimeToSave)
            return;

        AgencyEconomyManager economy = AgencyEconomyManager.Instance;

        if (economy == null)
            return;

        float normalizedTimeOfDay =
            (float)CurrentTime.TimeOfDay.TotalDays;

        economy.SaveGameTime(
            CurrentTime.Year,
            CurrentTime.Month,
            CurrentTime.Day,
            normalizedTimeOfDay
        );
    }
}
