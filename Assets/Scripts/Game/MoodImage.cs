
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class MoodImage : MonoBehaviour
{
    [Header("Mood Images (0-3)")]
    public Texture[] moodImages = new Texture[4];

    [Header("Settings")]
    public RawImage targetImage;
    public bool autoUpdate = true;

    private float currentMood = -1f;

    private string SavePath =>
        Path.Combine(
            Application.persistentDataPath,
            "space_agency_save.json"
        );

    [Serializable]
    private class AgencyMoodData
    {
        public float mood;
    }

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<RawImage>();
    }

    private void Start()
    {
        UpdateMoodImage();
    }

    private void Update()
    {
        if (autoUpdate)
            UpdateMoodImage();
    }

    public void UpdateMoodImage()
    {
        if (targetImage == null || moodImages == null ||
            moodImages.Length < 4)
            return;

        float mood = ReadMood();

        if (Mathf.Approximately(mood, currentMood))
            return;

        currentMood = mood;

        int index;

        if (mood < 10f)
            index = 0;
        else if (mood < 20f)
            index = 1;
        else if (mood < 30f)
            index = 2;
        else
            index = 3;

        if (moodImages[index] != null)
            targetImage.texture = moodImages[index];
    }

    private float ReadMood()
    {
        try
        {
            if (!File.Exists(SavePath))
                return 40f;

            string json = File.ReadAllText(SavePath);

            AgencyMoodData data =
                JsonUtility.FromJson<AgencyMoodData>(json);

            return data != null
                ? Mathf.Clamp(data.mood, 0f, 40f)
                : 40f;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[MoodImage] " + e.Message);
            return 40f;
        }
    }
}
