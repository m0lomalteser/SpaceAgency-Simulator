using UnityEngine;
using UnityEngine.SceneManagement;

public class AdditiveSceneLoader : MonoBehaviour
{
    [SerializeField] private string sceneName; 
    
    void Start() 
    {
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }
}
