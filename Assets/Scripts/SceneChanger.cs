using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneChanger : MonoBehaviour
{
    [Header("Scene Transition")]
    public string sceneName;
    public Button sceneChangeButton;

    private void Start()
    {
        sceneChangeButton.onClick.AddListener(OnToggleClicked);
    }

    private void OnToggleClicked()
    {
        SceneManager.LoadScene(sceneName);
    }
}
