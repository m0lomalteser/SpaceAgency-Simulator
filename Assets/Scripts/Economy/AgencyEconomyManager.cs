using System;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// Welches der vier Upgrades gekauft wurde.
/// Die Werte entsprechen gleichzeitig den Indizes im bestehenden
/// "upgrades"-Array der space_agency_save.json.
/// </summary>
public enum AgencyUpgradeType
{
    Advertisements = 0,
    ReusableRockets = 1,
    MannedMissions = 2,
    CheaperParts = 3
}

/// <summary>
/// Zentraler Backend-Manager für Geld, Mood, Regierungsgeld und Upgrades.
///
/// Er liest und schreibt die bestehende space_agency_save.json und
/// erhält dabei alle bereits vorhandenen Felder (Agency-Name, Logo,
/// Statistiken usw.).
///
/// Integration:
/// - Komponente irgendwo in die Game-Szene legen (oder sie wird zur
///   Laufzeit automatisch erzeugt, wenn ein anderes Skript darauf zugreift).
/// - Alle öffentlichen Methoden/Events können aus anderen Skripten oder
///   über Unity-Events verwendet werden.
/// </summary>
[DefaultExecutionOrder(-200)]
public class AgencyEconomyManager : MonoBehaviour
{
    public const string SaveFileName = "space_agency_save.json";

    // Das Spiel benutzt aktuell 0 - 40 als Mood-Bereich.
    public const float MaxMood = 40f;

    private static AgencyEconomyManager instance;

    /// <summary>Bereits vorhandene Instanz, ohne automatisch eine neue zu erzeugen.</summary>
    public static AgencyEconomyManager ExistingInstance => instance;

    public static AgencyEconomyManager Instance
    {
        get
        {
            if (instance != null)
                return instance;

            instance = FindFirstObjectByType<AgencyEconomyManager>();

            if (instance == null)
            {
                GameObject managerObject =
                    new GameObject("AgencyEconomyManager");

                instance = managerObject.AddComponent<AgencyEconomyManager>();
            }

            return instance;
        }
    }

    // =========================================================
    // INSPECTOR - REGIERUNGSGELD
    // =========================================================

    [Header("Regierungsgeld")]
    [Tooltip("Auszahlung bei vollem Mood (40). Niedriger Mood reduziert den Betrag.")]
    public long maxDailyGovernmentGrant = 10_000_000;

    [Tooltip("Beim allerersten Start sofort eine Tageszahlung leisten.")]
    public bool grantOnFirstStart = true;

    [Tooltip("Maximale Anzahl Tage, die bei einem großen Zeitsprung nachgezahlt werden.")]
    public int maxCatchUpDays = 30;

    // =========================================================
    // INSPECTOR - UPGRADE KOSTEN
    // =========================================================

    [Header("Upgrade-Kosten")]
    public long advertisementsCost = 2_000_000_000;
    public long reusableRocketsCost = 1_000_000_000;
    public long mannedMissionsCost = 400_000_000;
    public long cheaperPartsCost = 5_000_000;

    // =========================================================
    // INSPECTOR - UPGRADE EFFEKTE
    // =========================================================

    [Header("Advertisements")]
    [Tooltip("Zusätzliches Geld, das zu Beginn eines jeden Monats ausgezahlt wird.")]
    public long advertisementMonthlyIncome = 500_000_000;

    [Tooltip("Mood, der alle 7 Tage dazukommt (wird bei 40 gedeckelt).")]
    [Range(0f, 40f)]
    public float advertisementWeeklyMood = 5f;

    [Header("Reusable Rockets")]
    [Tooltip("Wie oft eine Rakete insgesamt verwendet werden kann.")]
    [Min(1)]
    public int rocketReuseCount = 5;

    [Tooltip("Kostenfaktor für eine bereits benutzte Rakete (0.35 = 35 %).")]
    [Range(0f, 1f)]
    public float reusedRocketCostMultiplier = 0.35f;

    [Header("Manned Missions")]
    [Tooltip("Geldbelohnung bei einer erfolgreichen bemannten Mission.")]
    public long mannedMissionSuccessReward = 1_500_000_000;

    [Tooltip("Mood-Gewinn bei Erfolg (wird bei 40 gedeckelt).")]
    [Range(0f, 40f)]
    public float mannedMissionSuccessMood = 10f;

    [Tooltip("Mood-Verlust bei einem Fehlschlag.")]
    [Range(0f, 40f)]
    public float mannedMissionFailureMoodPenalty = 20f;

    [Tooltip("Schmerzensgeld, das bei einem Fehlschlag gezahlt werden muss.")]
    public long mannedMissionFailureCompensation = 300_000_000;

    [Header("Cheaper Parts")]
    [Tooltip("Kostenfaktor für alle Raketenteile (0.65 = 35 % günstiger).")]
    [Range(0.01f, 1f)]
    public float cheaperPartsCostMultiplier = 0.65f;

    // =========================================================
    // INSPECTOR - RUNTIME
    // =========================================================

    [Header("Runtime")]
    [Tooltip("Manager über Szenenwechsel hinweg behalten.")]
    public bool persistAcrossScenes = true;

    [Tooltip("Wie oft die JSON auf Änderungen anderer Skripte geprüft wird.")]
    [Min(0.1f)]
    public float refreshInterval = 0.5f;

    // =========================================================
    // EVENTS
    // =========================================================

    public event Action<long> MoneyChanged;
    public event Action<float> MoodChanged;
    public event Action<AgencyUpgradeType> UpgradePurchased;

    /// <summary>Tag, Betrag, Mood zum Zeitpunkt der Zahlung.</summary>
    public event Action<DateTime, long, float> GovernmentGrantPaid;

    /// <summary>Monatliche Advertisement-Einnahme.</summary>
    public event Action<long> AdvertisementIncomePaid;

    /// <summary>Wöchentlicher Mood-Gewinn durch Advertisements.</summary>
    public event Action<float> AdvertisementMoodBoosted;

    // =========================================================
    // STATE
    // =========================================================

    private SpaceAgencySaveData data;

    private DateTime lastWriteTimeUtc;
    private long lastFileLength = -1;

    private float nextRefreshTime;

    // =========================================================
    // PROPERTIES
    // =========================================================

    public long Money
    {
        get
        {
            RefreshIfNeeded();
            return data != null ? data.money : 0;
        }
    }

    /// <summary>Kumuliertes Regierungsgeld, das bereits ausgezahlt wurde.</summary>
    public long GovernmentMoney
    {
        get
        {
            RefreshIfNeeded();
            return data != null ? data.regierungsgeld : 0;
        }
    }

    public long LastGovernmentGrant
    {
        get
        {
            RefreshIfNeeded();
            return data != null ? data.lastGovernmentGrant : 0;
        }
    }

    public float Mood
    {
        get
        {
            RefreshIfNeeded();
            return data != null ? data.mood : 0f;
        }
    }

    public string SavePath =>
        Path.Combine(Application.persistentDataPath, SaveFileName);

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (persistAcrossScenes && transform.parent == null)
            DontDestroyOnLoad(gameObject);

        Reload(true);
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime)
            return;

        nextRefreshTime =
            Time.unscaledTime + Mathf.Max(0.1f, refreshInterval);

        RefreshIfNeeded();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    // =========================================================
    // LOAD / SAVE
    // =========================================================

    public void Reload()
    {
        Reload(true);
    }

    private void Reload(bool force)
    {
        if (!File.Exists(SavePath))
        {
            if (data == null || force)
            {
                data = CreateDefaultData();
                lastWriteTimeUtc = default;
                lastFileLength = -1;
            }

            return;
        }

        try
        {
            FileInfo info = new FileInfo(SavePath);

            if (!force &&
                info.LastWriteTimeUtc == lastWriteTimeUtc &&
                info.Length == lastFileLength)
            {
                return;
            }

            string json = File.ReadAllText(SavePath);
            SpaceAgencySaveData loaded =
                JsonUtility.FromJson<SpaceAgencySaveData>(json);

            if (loaded == null)
                return;

            Normalize(loaded);

            long oldMoney = data != null ? data.money : loaded.money;
            float oldMood = data != null ? data.mood : loaded.mood;
            int[] oldUpgrades =
                data != null && data.upgrades != null
                    ? (int[])data.upgrades.Clone()
                    : null;

            data = loaded;

            lastWriteTimeUtc = info.LastWriteTimeUtc;
            lastFileLength = info.Length;

            if (oldMoney != data.money)
                MoneyChanged?.Invoke(data.money);

            if (!Mathf.Approximately(oldMood, data.mood))
                MoodChanged?.Invoke(data.mood);

            NotifyNewUpgrades(oldUpgrades, data.upgrades);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[AgencyEconomyManager] Save konnte nicht gelesen werden: " +
                exception.Message
            );
        }
    }

    private void RefreshIfNeeded()
    {
        if (data == null)
        {
            Reload(true);
            return;
        }

        if (Time.unscaledTime < nextRefreshTime)
            return;

        nextRefreshTime =
            Time.unscaledTime + Mathf.Max(0.1f, refreshInterval);

        Reload(false);
    }

    private void EnsureData()
    {
        if (data == null)
            data = CreateDefaultData();
    }

    private void Save()
    {
        if (data == null)
            return;

        try
        {
            Normalize(data);

            data.updatedAt = DateTime.Now.ToString(
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture
            );

            string json = JsonUtility.ToJson(data, true);
            string temporaryPath = SavePath + ".tmp";

            string directory = Path.GetDirectoryName(SavePath);

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(temporaryPath, json);

            if (File.Exists(SavePath))
                File.Delete(SavePath);

            File.Move(temporaryPath, SavePath);

            FileInfo info = new FileInfo(SavePath);

            lastWriteTimeUtc = info.LastWriteTimeUtc;
            lastFileLength = info.Length;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[AgencyEconomyManager] Save konnte nicht geschrieben werden: " +
                exception
            );
        }
    }

    private SpaceAgencySaveData CreateDefaultData()
    {
        SpaceAgencySaveData created = new SpaceAgencySaveData();

        created.version = 2;
        created.money = 30_000_000;
        created.mood = 40f;
        created.day = 1;
        created.month = 1;
        created.year = 1;
        created.upgrades = new int[6];
        created.pendingMission = -1;
        created.lastResultMission = -1;
        created.lastResultSuccess = -1;
        created.createdAt = DateTime.Now.ToString(
            "yyyy-MM-dd HH:mm:ss",
            CultureInfo.InvariantCulture
        );
        created.updatedAt = created.createdAt;

        return created;
    }

    private static void Normalize(SpaceAgencySaveData target)
    {
        if (target == null)
            return;

        if (target.upgrades == null || target.upgrades.Length < 6)
        {
            int[] fixedUpgrades = new int[6];

            if (target.upgrades != null)
            {
                int count = Mathf.Min(
                    target.upgrades.Length,
                    fixedUpgrades.Length
                );

                Array.Copy(
                    target.upgrades,
                    fixedUpgrades,
                    count
                );
            }

            target.upgrades = fixedUpgrades;
        }

        if (target.agencyName == null)
            target.agencyName = "";

        if (target.logoPng == null)
            target.logoPng = "";

        if (target.lastResultText == null)
            target.lastResultText = "";

        if (target.createdAt == null)
            target.createdAt = "";

        if (target.updatedAt == null)
            target.updatedAt = "";

        if (target.lastGovernmentGrantDate == null)
            target.lastGovernmentGrantDate = "";

        if (target.lastAdvertisementPayoutDate == null)
            target.lastAdvertisementPayoutDate = "";

        if (target.lastAdvertisementMoodDate == null)
            target.lastAdvertisementMoodDate = "";

        target.money = Math.Max(0, target.money);
        target.mood = Mathf.Clamp(target.mood, 0f, MaxMood);
        target.regierungsgeld = Math.Max(0, target.regierungsgeld);
        target.lastGovernmentGrant = Math.Max(0, target.lastGovernmentGrant);
        target.lastGovernmentGrantMood = Mathf.Clamp(
            target.lastGovernmentGrantMood,
            0f,
            MaxMood
        );

        target.mannedMissionLaunches = Math.Max(
            0,
            target.mannedMissionLaunches
        );

        target.mannedMissionSuccesses = Math.Max(
            0,
            target.mannedMissionSuccesses
        );

        target.mannedMissionFailures = Math.Max(
            0,
            target.mannedMissionFailures
        );

        target.mannedMissionCompensationPaid = Math.Max(
            0,
            target.mannedMissionCompensationPaid
        );
    }

    private void NotifyNewUpgrades(int[] oldUpgrades, int[] newUpgrades)
    {
        if (oldUpgrades == null || newUpgrades == null)
            return;

        int count = Mathf.Min(oldUpgrades.Length, newUpgrades.Length);

        for (int i = 0; i < count; i++)
        {
            if (oldUpgrades[i] == 0 && newUpgrades[i] != 0)
                UpgradePurchased?.Invoke((AgencyUpgradeType)i);
        }
    }

    // =========================================================
    // MONEY / MOOD
    // =========================================================

    public bool CanAfford(long amount)
    {
        return amount <= 0 || Money >= amount;
    }

    public void AddMoney(long amount, string reason = null)
    {
        if (amount == 0)
            return;

        Reload(true);
        EnsureData();

        long oldMoney = data.money;

        data.money = Math.Max(0, data.money + amount);

        if (oldMoney == data.money)
            return;

        Save();
        MoneyChanged?.Invoke(data.money);

        if (!string.IsNullOrEmpty(reason))
        {
            Debug.Log(
                "[AgencyEconomyManager] " + reason + " (" +
                FormatMoney(data.money) + ")"
            );
        }
    }

    public bool SpendMoney(long amount, string reason = null)
    {
        if (amount <= 0)
            return true;

        Reload(true);
        EnsureData();

        if (data.money < amount)
            return false;

        data.money -= amount;
        data.lifetimeSpent += amount;

        Save();
        MoneyChanged?.Invoke(data.money);

        if (!string.IsNullOrEmpty(reason))
        {
            Debug.Log(
                "[AgencyEconomyManager] " + reason + " (-" +
                FormatMoney(amount) + ")"
            );
        }

        return true;
    }

    public void SetMoney(long amount)
    {
        Reload(true);
        EnsureData();

        long newMoney = Math.Max(0, amount);

        if (data.money == newMoney)
            return;

        data.money = newMoney;
        Save();
        MoneyChanged?.Invoke(data.money);
    }

    public void SetMood(float mood)
    {
        Reload(true);
        EnsureData();

        float newMood = Mathf.Clamp(mood, 0f, MaxMood);

        if (Mathf.Approximately(data.mood, newMood))
            return;

        data.mood = newMood;
        Save();
        MoodChanged?.Invoke(data.mood);
    }

    public void AddMood(float amount)
    {
        if (Mathf.Approximately(amount, 0f))
            return;

        Reload(true);
        EnsureData();

        float oldMood = data.mood;

        data.mood = Mathf.Clamp(data.mood + amount, 0f, MaxMood);

        if (Mathf.Approximately(oldMood, data.mood))
            return;

        Save();
        MoodChanged?.Invoke(data.mood);
    }

    // =========================================================
    // REGIERUNGSGELD
    // =========================================================

    /// <summary>
    /// Berechnet die Tageszahlung anhand des Moods.
    ///
    /// 0 Mood  = 10 % = 1.000.000
    /// 10 Mood = 25 % = 2.500.000
    /// 20 Mood = 50 % = 5.000.000
    /// 40 Mood = 100 % = 10.000.000
    /// </summary>
    public long CalculateGovernmentGrant(float mood)
    {
        float clampedMood = Mathf.Clamp(mood, 0f, MaxMood);

        float fraction;

        if (clampedMood <= 10f)
        {
            fraction = Mathf.Lerp(
                0.10f,
                0.25f,
                clampedMood / 10f
            );
        }
        else if (clampedMood <= 20f)
        {
            fraction = Mathf.Lerp(
                0.25f,
                0.50f,
                (clampedMood - 10f) / 10f
            );
        }
        else
        {
            fraction = Mathf.Lerp(
                0.50f,
                1f,
                (clampedMood - 20f) / (MaxMood - 20f)
            );
        }

        return (long)Math.Round(
            maxDailyGovernmentGrant * fraction,
            MidpointRounding.AwayFromZero
        );
    }

    public long CurrentGovernmentGrant =>
        CalculateGovernmentGrant(Mood);

    /// <summary>
    /// Wird vom GameTimeManager bei jedem neuen Tag (00:00) aufgerufen.
    /// Verarbeitet auch verpasste Tage und alle täglichen Upgrade-Effekte.
    /// </summary>
    /// <returns>Anzahl der verarbeiteten Tage.</returns>
    public int ProcessDate(DateTime date)
    {
        Reload(true);

        if (data == null)
            return 0;

        DateTime today = date.Date;

        DateTime lastDate;

        bool hasLastDate = TryParseDate(
            data.lastGovernmentGrantDate,
            out lastDate
        );

        if (!hasLastDate)
        {
            InitializeAdvertisementDates(today);

            if (!grantOnFirstStart)
            {
                data.lastGovernmentGrantDate = DateToString(today);
                Save();
                return 0;
            }

            long oldMoney = data.money;
            float oldMood = data.mood;

            ProcessSingleDay(today);

            data.lastGovernmentGrantDate = DateToString(today);

            Save();

            if (oldMoney != data.money)
                MoneyChanged?.Invoke(data.money);

            if (!Mathf.Approximately(oldMood, data.mood))
                MoodChanged?.Invoke(data.mood);

            return 1;
        }

        if (today <= lastDate)
            return 0;

        long moneyBefore = data.money;
        float moodBefore = data.mood;

        int daysToProcess = (today - lastDate).Days;
        int processedDays = 0;

        DateTime cursor = lastDate;
        int limit = Mathf.Max(1, maxCatchUpDays);

        while (cursor < today && processedDays < limit)
        {
            cursor = cursor.AddDays(1);

            ProcessSingleDay(cursor);

            processedDays++;
        }

        // Auch wenn das Catch-Up-Limit erreicht wurde, wird der Zeitstempel
        // auf heute gesetzt, damit nicht in jedem Frame erneut nachgezahlt wird.
        data.lastGovernmentGrantDate = DateToString(today);

        Save();

        if (moneyBefore != data.money)
            MoneyChanged?.Invoke(data.money);

        if (!Mathf.Approximately(moodBefore, data.mood))
            MoodChanged?.Invoke(data.mood);

        if (daysToProcess > processedDays)
        {
            Debug.LogWarning(
                "[AgencyEconomyManager] " + (daysToProcess - processedDays) +
                " Tage wurden wegen maxCatchUpDays übersprungen."
            );
        }

        return processedDays;
    }

    private void ProcessSingleDay(DateTime day)
    {
        float moodAtStart = Mathf.Clamp(data.mood, 0f, MaxMood);
        long grant = CalculateGovernmentGrant(moodAtStart);

        data.money += grant;
        data.regierungsgeld += grant;
        data.lastGovernmentGrant = grant;
        data.lastGovernmentGrantMood = moodAtStart;

        // Bestehende Langzeit-Statistiken sinnvoll mitführen.
        data.lifetimeBudget += grant;
        data.lastBudget = grant;

        GovernmentGrantPaid?.Invoke(day, grant, moodAtStart);

        ApplyAdvertisementEffects(day);
    }

    private void ApplyAdvertisementEffects(DateTime day)
    {
        if (!HasUpgradeInternal(AgencyUpgradeType.Advertisements))
            return;

        DateTime lastPayoutDate;

        if (TryParseDate(data.lastAdvertisementPayoutDate, out lastPayoutDate))
        {
            bool monthChanged =
                day.Year != lastPayoutDate.Year ||
                day.Month != lastPayoutDate.Month;

            if (monthChanged)
            {
                data.money += advertisementMonthlyIncome;

                data.lastAdvertisementPayoutDate = DateToString(day);

                AdvertisementIncomePaid?.Invoke(
                    advertisementMonthlyIncome
                );
            }
        }
        else
        {
            data.lastAdvertisementPayoutDate = DateToString(day);
        }

        DateTime lastMoodDate;

        if (TryParseDate(data.lastAdvertisementMoodDate, out lastMoodDate))
        {
            if ((day - lastMoodDate).Days >= 7)
            {
                float oldMood = data.mood;

                data.mood = Mathf.Min(
                    MaxMood,
                    data.mood + advertisementWeeklyMood
                );

                data.lastAdvertisementMoodDate = DateToString(day);

                if (data.mood > oldMood)
                    AdvertisementMoodBoosted?.Invoke(data.mood - oldMood);
            }
        }
        else
        {
            data.lastAdvertisementMoodDate = DateToString(day);
        }
    }

    private void InitializeAdvertisementDates(DateTime day)
    {
        if (!HasUpgradeInternal(AgencyUpgradeType.Advertisements))
            return;

        if (string.IsNullOrEmpty(data.lastAdvertisementPayoutDate))
            data.lastAdvertisementPayoutDate = DateToString(day);

        if (string.IsNullOrEmpty(data.lastAdvertisementMoodDate))
            data.lastAdvertisementMoodDate = DateToString(day);
    }

    // =========================================================
    // UPGRADES
    // =========================================================

    public bool HasUpgrade(AgencyUpgradeType type)
    {
        RefreshIfNeeded();
        return HasUpgradeInternal(type);
    }

    private bool HasUpgradeInternal(AgencyUpgradeType type)
    {
        if (data == null || data.upgrades == null)
            return false;

        int index = (int)type;

        return index >= 0 &&
               index < data.upgrades.Length &&
               data.upgrades[index] != 0;
    }

    public long GetUpgradeCost(AgencyUpgradeType type)
    {
        switch (type)
        {
            case AgencyUpgradeType.Advertisements:
                return advertisementsCost;

            case AgencyUpgradeType.ReusableRockets:
                return reusableRocketsCost;

            case AgencyUpgradeType.MannedMissions:
                return mannedMissionsCost;

            case AgencyUpgradeType.CheaperParts:
                return cheaperPartsCost;

            default:
                return 0;
        }
    }

    public static string GetUpgradeName(AgencyUpgradeType type)
    {
        switch (type)
        {
            case AgencyUpgradeType.Advertisements:
                return "Advertisements";

            case AgencyUpgradeType.ReusableRockets:
                return "Reusable Rockets";

            case AgencyUpgradeType.MannedMissions:
                return "Manned Missions";

            case AgencyUpgradeType.CheaperParts:
                return "Cheaper Parts";

            default:
                return type.ToString();
        }
    }

    public bool BuyUpgrade(AgencyUpgradeType type, out string message)
    {
        Reload(true);
        EnsureData();

        int index = (int)type;

        if (index < 0 || index >= 4)
        {
            message = "Unbekanntes Upgrade.";
            return false;
        }

        Normalize(data);

        string friendlyName = GetUpgradeName(type);

        if (data.upgrades[index] != 0)
        {
            message = friendlyName + " ist bereits gekauft.";
            return false;
        }

        long cost = GetUpgradeCost(type);

        if (data.money < cost)
        {
            message =
                "Nicht genug Geld für " + friendlyName + ". Benötigt: " +
                FormatMoney(cost) + ".";

            return false;
        }

        data.money -= cost;
        data.lifetimeSpent += cost;
        data.upgrades[index] = 1;

        if (type == AgencyUpgradeType.Advertisements)
        {
            DateTime now = GetCurrentEconomyDate();

            data.lastAdvertisementPayoutDate = DateToString(now);
            data.lastAdvertisementMoodDate = DateToString(now);
        }

        Save();

        MoneyChanged?.Invoke(data.money);
        UpgradePurchased?.Invoke(type);

        message = friendlyName + " gekauft für " + FormatMoney(cost) + ".";

        return true;
    }

    public bool BuyAdvertisements(out string message)
    {
        return BuyUpgrade(AgencyUpgradeType.Advertisements, out message);
    }

    public bool BuyReusableRockets(out string message)
    {
        return BuyUpgrade(AgencyUpgradeType.ReusableRockets, out message);
    }

    public bool BuyMannedMissions(out string message)
    {
        return BuyUpgrade(AgencyUpgradeType.MannedMissions, out message);
    }

    public bool BuyCheaperParts(out string message)
    {
        return BuyUpgrade(AgencyUpgradeType.CheaperParts, out message);
    }

    // =========================================================
    // EFFECTS FOR OTHER SYSTEMS
    // =========================================================

    public bool RocketsAreReusable =>
        HasUpgrade(AgencyUpgradeType.ReusableRockets);

    /// <summary>1 = ohne Upgrade, sonst der eingestellte Wert.</summary>
    public int RocketReuseCount =>
        RocketsAreReusable ? Mathf.Max(1, rocketReuseCount) : 1;

    /// <summary>
    /// Multiplikator für die Kosten einer bereits benutzten Rakete.
    /// 1 = voller Preis, 0.35 = 35 % des Preises.
    /// </summary>
    public float ReusedRocketCostMultiplier =>
        RocketsAreReusable ? reusedRocketCostMultiplier : 1f;

    public long GetReusedRocketCost(long baseCost)
    {
        return (long)Math.Round(
            baseCost * (double)ReusedRocketCostMultiplier,
            MidpointRounding.AwayFromZero
        );
    }

    public bool CanLaunchMannedMissions =>
        HasUpgrade(AgencyUpgradeType.MannedMissions);

    public float PartCostMultiplier =>
        HasUpgrade(AgencyUpgradeType.CheaperParts)
            ? cheaperPartsCostMultiplier
            : 1f;

    /// <summary>
    /// Liefert den tatsächlichen Preis eines Teils inklusive Cheaper-Parts-Rabatt.
    /// </summary>
    public long GetPartCost(long baseCostUsd)
    {
        return (long)Math.Round(
            baseCostUsd * (double)PartCostMultiplier,
            MidpointRounding.AwayFromZero
        );
    }

    public long GetPartCost(int baseCostUsd)
    {
        return GetPartCost((long)baseCostUsd);
    }

    /// <summary>
    /// Erfolg: viel Geld + etwas Mood.
    /// Fehlschlag: Schmerzensgeld abziehen + viel Mood verlieren.
    /// </summary>
    public bool ResolveMannedMission(bool success, out string message)
    {
        Reload(true);
        EnsureData();

        Normalize(data);

        if (!HasUpgradeInternal(AgencyUpgradeType.MannedMissions))
        {
            message =
                "Manned Missions ist noch nicht gekauft.";

            return false;
        }

        long oldMoney = data.money;
        float oldMood = data.mood;

        data.mannedMissionLaunches++;

        if (success)
        {
            data.money += mannedMissionSuccessReward;

            data.mood = Mathf.Min(
                MaxMood,
                data.mood + mannedMissionSuccessMood
            );

            data.mannedMissionSuccesses++;

            message =
                "Bemannte Mission erfolgreich: +" +
                FormatMoney(mannedMissionSuccessReward) +
                ", Mood +" + mannedMissionSuccessMood + ".";
        }
        else
        {
            long compensation = Math.Min(
                data.money,
                Math.Max(0, mannedMissionFailureCompensation)
            );

            data.money -= compensation;
            data.mannedMissionCompensationPaid += compensation;

            data.mood = Mathf.Max(
                0f,
                data.mood - mannedMissionFailureMoodPenalty
            );

            data.mannedMissionFailures++;

            message =
                "Bemannte Mission fehlgeschlagen: -" +
                FormatMoney(compensation) + " Schmerzensgeld, Mood -" +
                mannedMissionFailureMoodPenalty + ".";
        }

        Save();

        if (oldMoney != data.money)
            MoneyChanged?.Invoke(data.money);

        if (!Mathf.Approximately(oldMood, data.mood))
            MoodChanged?.Invoke(data.mood);

        return true;
    }

    public bool ResolveMannedMissionSuccess(out string message)
    {
        return ResolveMannedMission(true, out message);
    }

    public bool ResolveMannedMissionFailure(out string message)
    {
        return ResolveMannedMission(false, out message);
    }

    // =========================================================
    // CLOCK SAVE (used by GameTimeManager)
    // =========================================================

    public bool TryGetGameTime(
        out int year,
        out int month,
        out int day,
        out float timeOfDay)
    {
        year = 1;
        month = 1;
        day = 1;
        timeOfDay = 0f;

        // Ohne echte Save-Datei soll die Uhr mit der Fallback-Zeit starten,
        // nicht mit dem Standarddatum 01.01.0001.
        if (!File.Exists(SavePath))
            return false;

        Reload(true);

        if (data == null)
            return false;

        if (data.year < 1 ||
            data.month < 1 || data.month > 12 ||
            data.day < 1 || data.day > 31)
        {
            return false;
        }

        int daysInMonth = DateTime.DaysInMonth(data.year, data.month);

        year = data.year;
        month = data.month;
        day = Mathf.Clamp(data.day, 1, daysInMonth);
        timeOfDay = Mathf.Repeat(data.timeOfDay, 1f);

        return true;
    }

    public void SaveGameTime(
        int year,
        int month,
        int day,
        float timeOfDay)
    {
        Reload(true);
        EnsureData();

        data.year = Mathf.Max(1, year);
        data.month = Mathf.Clamp(month, 1, 12);

        int daysInMonth = DateTime.DaysInMonth(
            data.year,
            data.month
        );

        data.day = Mathf.Clamp(day, 1, daysInMonth);
        data.timeOfDay = Mathf.Repeat(timeOfDay, 1f);

        Save();
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private DateTime GetCurrentEconomyDate()
    {
        GameTimeManager clock = GameTimeManager.ExistingInstance;

        if (clock != null)
            return clock.CurrentDate;

        return DateTime.Today;
    }

    private static bool TryParseDate(string value, out DateTime date)
    {
        if (string.IsNullOrEmpty(value))
        {
            date = default;
            return false;
        }

        return DateTime.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date
        );
    }

    private static string DateToString(DateTime date)
    {
        return date.ToString(
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture
        );
    }

    public static string FormatMoney(long amount)
    {
        long absolute = Math.Abs(amount);
        string sign = amount < 0 ? "-" : "";

        if (absolute >= 1_000_000_000_000L)
            return sign + "$" + (absolute / 1_000_000_000_000.0).ToString("0.##", CultureInfo.InvariantCulture) + "T";

        if (absolute >= 1_000_000_000L)
            return sign + "$" + (absolute / 1_000_000_000.0).ToString("0.##", CultureInfo.InvariantCulture) + "B";

        if (absolute >= 1_000_000L)
            return sign + "$" + (absolute / 1_000_000.0).ToString("0.##", CultureInfo.InvariantCulture) + "M";

        if (absolute >= 1_000L)
            return sign + "$" + (absolute / 1_000.0).ToString("0.##", CultureInfo.InvariantCulture) + "k";

        return sign + "$" + absolute.ToString("N0", CultureInfo.InvariantCulture);
    }
}
