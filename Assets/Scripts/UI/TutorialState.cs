using UnityEngine;

/// <summary>
/// Merkt sich, ob das Erststart-Tutorial bereits gezeigt wurde.
/// </summary>
public static class TutorialState
{
    private const string CompletedKey = "Tutorial.Completed";

    public static bool IsCompleted =>
        PlayerPrefs.GetInt(CompletedKey, 0) != 0;

    public static void MarkCompleted()
    {
        PlayerPrefs.SetInt(CompletedKey, 1);
        PlayerPrefs.Save();
    }

    public static void ResetTutorial()
    {
        PlayerPrefs.DeleteKey(CompletedKey);
        PlayerPrefs.Save();
    }
}
