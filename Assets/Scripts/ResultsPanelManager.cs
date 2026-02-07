using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResultsPanelManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text finalPlayer1ScoreText;
    public TMP_Text finalPlayer2ScoreText;
    public TMP_Text winnerText;
    public GameObject player1CrownIcon;
    public GameObject player2CrownIcon;
    public GameObject drawMessage;

    [Header("Player Result Images")]
    public RawImage player1ResultImage;
    public RawImage player2ResultImage;

    [Header("Result Display")]
    public Button restartButton;
    public Button menuButton;

    [Header("Panel Dependencies")]
    public GameObject gamePanel; // ✅ ADD THIS

    public GameObject resultPanel;
    public GameObject detectionPanel;
    
    private DetectionManager detection;
    private GamePanelManager gameManager; // Assuming you have this script

    void Start()
    {
        // Find references safely
        GameObject gamePanelObj = GameObject.Find("GamePanel"); 
        if (gamePanelObj != null)
        {
             // Try to find the manager if it exists, otherwise ignore (prevents crash)
             gameManager = gamePanelObj.GetComponent<GamePanelManager>();
        }

        detection = FindObjectOfType<DetectionManager>();

        restartButton.onClick.AddListener(OnRestartButtonPressed);
        menuButton.onClick.AddListener(OnMenuButtonPressed);
    }

    public void ShowResults(int p1TotalScore, int p2TotalScore, Texture2D p1Image, Texture2D p2Image)
    {
            // 🔴 FORCE game panel OFF
    if (gamePanel != null)
        gamePanel.SetActive(false);

        resultPanel.SetActive(true);

        // --- PLAYER 1 IMAGE ---
        if (player1ResultImage != null)
        {
            if (p1Image != null)
            {
                player1ResultImage.texture = p1Image;
                player1ResultImage.gameObject.SetActive(true);
            }
            else
            {
                // No image provided, hide the RawImage so we don't show a white square
                player1ResultImage.gameObject.SetActive(false);
            }
        }

        // --- PLAYER 2 IMAGE ---
        if (player2ResultImage != null)
        {
            if (p2Image != null)
            {
                player2ResultImage.texture = p2Image;
                player2ResultImage.gameObject.SetActive(true);
            }
            else
            {
                player2ResultImage.gameObject.SetActive(false);
            }
        }

        // --- SCORES & WINNER LOGIC ---
        finalPlayer1ScoreText.text = $"{p1TotalScore}";
        finalPlayer2ScoreText.text = $"{p2TotalScore}";

        player1CrownIcon.SetActive(false);
        player2CrownIcon.SetActive(false);
        drawMessage.SetActive(false);
        winnerText.gameObject.SetActive(true);

        if (p1TotalScore > p2TotalScore)
        {
            winnerText.text = "Player 1 Wins!";
            player1CrownIcon.SetActive(true);
        }
        else if (p2TotalScore > p1TotalScore)
        {
            winnerText.text = "Player 2 Wins!";
            player2CrownIcon.SetActive(true);
        }
        else
        {
            winnerText.gameObject.SetActive(false);
            drawMessage.SetActive(true);
        }
    }

    public void OnRestartButtonPressed()
    {
        // 1. Reset the Detection Manager using the new public method
        if (detection != null)
        {
            detection.ResetDetection();
        }

        // 2. Switch Panels
        resultPanel.SetActive(false);
        detectionPanel.SetActive(true);

        // 3. Reset Game Logic
        if (gameManager != null) 
        {
            // Assuming your GamePanelManager has a ResetGame method
             gameManager.ResetGame();
        }
    }

    public void OnMenuButtonPressed()
    {
        resultPanel.SetActive(false);
        
        // Return to main menu or reset game state
        if (gameManager != null) 
        {
             gameManager.ResetGame();
        }
        
        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
    }
}