using UnityEngine;
using UnityEngine.UI;
using TMPro;
// using System.IO; // No longer needed

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
    // The thumbnail and save button have been removed.
    public Button restartButton;
    public Button menuButton;

    [Header("Panel Dependencies")]
    public GameObject resultPanel;
    public GameObject detectionPanel;
    public GameObject introPanel;
    private DetectionManager detection;
    private GamePanelManager gameManager;
    private IntroTransition introTransition;
    
    // The variable for storing video data has been removed.

    void Start()
    {
        gameManager = FindObjectOfType<GamePanelManager>();
        introTransition = FindObjectOfType<IntroTransition>();

        restartButton.onClick.AddListener(OnRestartButtonPressed);
        menuButton.onClick.AddListener(OnMenuButtonPressed);
        // The listener for the save button has been removed.
        detection = FindObjectOfType<DetectionManager>();
    }

    // The method signature has been simplified.
public void ShowResults(int p1TotalScore, int p2TotalScore, Texture2D p1Image, Texture2D p2Image)
{
    resultPanel.SetActive(true);

    // PLAYER 1 IMAGE
    if (player1ResultImage != null)
    {
        Texture tex = p1Image;

        // If no explicit image passed, use the left overlay texture from detection
        if (tex == null && detection != null && detection.leftOverlay != null)
        {
            tex = detection.leftOverlay.texture;
        }

        if (tex != null)
        {
            player1ResultImage.texture = tex;
            player1ResultImage.gameObject.SetActive(true);
        }
        else
        {
            player1ResultImage.gameObject.SetActive(false);
        }
    }

    // PLAYER 2 IMAGE
    if (player2ResultImage != null)
    {
        Texture tex = p2Image;

        // If no explicit image passed, use the right overlay texture from detection
        if (tex == null && detection != null && detection.rightOverlay != null)
        {
            tex = detection.rightOverlay.texture;
        }

        if (tex != null)
        {
            player2ResultImage.texture = tex;
            player2ResultImage.gameObject.SetActive(true);
        }
        else
        {
            player2ResultImage.gameObject.SetActive(false);
        }
    }

    // scores + winner logic same as before
    finalPlayer1ScoreText.text = $"{p1TotalScore}";
    finalPlayer2ScoreText.text = $"{p2TotalScore}";

    player1CrownIcon.SetActive(false);
    player2CrownIcon.SetActive(false);
    drawMessage.SetActive(false);
    winnerText.gameObject.SetActive(true);

    if (p1TotalScore > p2TotalScore)
    {
        winnerText.text = "Player B Wins!";
        player1CrownIcon.SetActive(true);
    }
    else if (p2TotalScore > p1TotalScore)
    {
        winnerText.text = "Player A Wins!";
        player2CrownIcon.SetActive(true);
    }
    else
    {
        winnerText.gameObject.SetActive(false);
        drawMessage.SetActive(true);
    }
}

    // The OnSaveVideoPressed() method has been completely removed.

    public void OnRestartButtonPressed()
    {
        detection.leftOverlay.gameObject.SetActive(true);
        detection.rightOverlay.gameObject.SetActive(true);
        detection.countdownStarted = false;
        detection.leftReady = false;
        detection.rightReady = false;
        resultPanel.SetActive(false);
        detectionPanel.SetActive(true);
        if (gameManager != null) gameManager.ResetGame();
    }

    public void OnMenuButtonPressed()
    {
        resultPanel.SetActive(false);
        introPanel.SetActive(true);
        if (gameManager != null) gameManager.ResetGame();
        if (introTransition != null) introTransition.ResetIntro();
    }
}