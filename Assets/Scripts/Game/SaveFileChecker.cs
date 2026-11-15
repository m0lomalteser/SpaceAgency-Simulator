using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SaveFileChecker : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button saveFileButton;

    private string fileName = "space_agency_save.json";

    private void Start()
    {
        CheckSaveFileExistence();
    }

    private void CheckSaveFileExistence()
    {
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        
        bool fileExists = File.Exists(filePath);

        if (saveFileButton != null)
        {
            saveFileButton.interactable = fileExists;
        }
        else
        {
            Debug.LogWarning("Kein Button im Inspector zugewiesen!", this);
        }
    }
    
    public void LoadGameScene()
    {
        SceneManager.LoadScene("Game");
    }
}