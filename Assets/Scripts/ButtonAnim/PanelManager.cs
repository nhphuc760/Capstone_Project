using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PanelManager : MonoBehaviour
{

    [SerializeField] private Button NewGameBTN;
    [SerializeField] private Button LoadGameBTN;

    void Start()
    {

        NewGameBTN.onClick.AddListener(() => SceneChange("GameScene"));
        LoadGameBTN.onClick.AddListener(() => SceneChange("LoadGameScene"));
    }

    
    void Update()
    {
        
    }

    private void SceneChange(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
