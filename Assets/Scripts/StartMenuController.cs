using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenuController : MonoBehaviour
{
    [Header("UI References")]
    public Button startButton;

    [Header("Scene Settings")]
    public string menuSceneName = "MenuScene";  // change to your scene name

    private void Start()
    {
        // Assign the click event
        startButton.onClick.AddListener(OnStartClicked);
    }

    private void OnStartClicked()
    {
        Debug.Log("Start button clicked → Loading menu scene...");
        SceneManager.LoadScene(menuSceneName);
    }
}
