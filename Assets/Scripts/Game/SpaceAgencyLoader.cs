using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpaceAgencyLoader : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Ziehe hier dein Text-UI-Element (oder TextMeshProUGUI) hinein")]
    [SerializeField] private TextMeshProUGUI agencyNameText; // Nutze dies falls TMP

    [Tooltip("Ziehe hier dein RawImage-UI-Element für das Logo hinein")]
    [SerializeField] private RawImage logoRawImage;

    [Header("File Settings")]
    [Tooltip("Name der Speicherdatei (z.B. save.json oder wie sie bei dir heißt)")]
    [SerializeField] private string fileName = "space_agency_save.json"; // Bitte anpassen, falls die Datei anders heißt!

    // Öffentliche Variable, damit andere Skripte das Geld abrufen können
    public long CurrentMoney { get; private set; }

    [Serializable]
    public class GameData
    {
        public int version;
        public string agencyName;
        public string logoPng;
        public long money;
        public float mood;
        // Weitere Felder falls benötigt...
    }

    void Start()
    {
        LoadAndApplyData();
    }

    public void LoadAndApplyData()
    {
        // Pfad zusammenbauen
        string folderPath = Path.Combine(Application.persistentDataPath, "../DefaultCompany/SpaceAgency-Simulator");
        string fullPath = Path.Combine(folderPath, fileName);

        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"[SpaceAgencyLoader] Datei nicht gefunden unter: {fullPath}");
            fullPath = Path.Combine(Application.persistentDataPath, fileName);
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"[SpaceAgencyLoader] Konnte Savegame nirgendwo finden!");
                return;
            }
        }

        try
        {
            string jsonContent = File.ReadAllText(fullPath);
            GameData data = JsonUtility.FromJson<GameData>(jsonContent);

            // Geld zwischenspeichern
            if (data != null)
            {
                CurrentMoney = data.money;
            }

            // 1. Agency Name in Text setzen
            if (agencyNameText != null && data != null)
            {
                agencyNameText.text = data.agencyName;
            }

            // 2. Base64 PNG-String in Texture2D umwandeln und in RawImage anzeigen
            if (logoRawImage != null && data != null && !string.IsNullOrEmpty(data.logoPng))
            {
                byte[] imageBytes = Convert.FromBase64String(data.logoPng);
                
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(imageBytes))
                {
                    logoRawImage.texture = tex;
                    logoRawImage.gameObject.SetActive(true);
                }
                else
                {
                    Debug.LogWarning("[SpaceAgencyLoader] Konnte Base64-Bilddaten nicht als Textur laden.");
                }
            }

            Debug.Log("[SpaceAgencyLoader] Spieldaten erfolgreich geladen!");
        }
        catch (Exception e) {
            Debug.LogError($"[SpaceAgencyLoader] Fehler beim Lesen/Parsen der JSON-Datei: {e.Message}");
        }
    }
}