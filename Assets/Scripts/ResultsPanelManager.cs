using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ResultsPanelManager : MonoBehaviour
{
    [Header("Game Settings")]
    public GameType currentGameType = GameType.Calorie; // <--- Set this in inspector!


    [Header("Score Texts")]
    public TMP_Text finalPlayer1ScoreText;
    public TMP_Text finalPlayer2ScoreText;
    public TMP_Text player1NameText; 
    public TMP_Text player2NameText; 

    [Header("Winner UI")]
    public TMP_Text winnerText;
    public GameObject player1CrownIcon;
    public GameObject player2CrownIcon;
    public GameObject drawMessage;

    [Header("Player Result Images")]
    public RawImage player1ResultImage;
    public RawImage player2ResultImage;

    [Header("Main Buttons")]
    public Button restartButton;
    public Button menuButton;
    public Button openSavePanelButton;
    public TMP_Text openSavePanelButtonText; // To change text to "Saved!"

    [Header("Save UI Popup")]
    public GameObject savePopupPanel;
    public TMP_Dropdown p1Dropdown;
    public TMP_InputField p1NewNameInput;
    public TMP_Dropdown p2Dropdown;
    public TMP_InputField p2NewNameInput;
    public Button submitSaveButton;
    public Button cancelSaveButton;

    [Header("Panel Dependencies")]
    public GameObject gamePanel; 
    public GameObject resultPanel;
    public GameObject detectionPanel;
    
    private int p1FinalScore;
    private int p2FinalScore;
    private bool hasSaved = false;

    void Start()
    {
        restartButton.onClick.AddListener(OnRestartButtonPressed);
        menuButton.onClick.AddListener(OnMenuButtonPressed);
        
        // Setup Save System Buttons
        openSavePanelButton.onClick.AddListener(OpenSavePopup);
        submitSaveButton.onClick.AddListener(SaveResultsToDatabase);
        cancelSaveButton.onClick.AddListener(() => savePopupPanel.SetActive(false));
        
        savePopupPanel.SetActive(false);
    }

    public void ShowResults(int p1TotalScore, int p2TotalScore, Texture2D p1Image, Texture2D p2Image, string p1Name, string p2Name)
    {
        p1FinalScore = p1TotalScore;
        p2FinalScore = p2TotalScore;
        hasSaved = false;
        
        // Reset Save Button
        openSavePanelButton.interactable = true;
        if(openSavePanelButtonText) openSavePanelButtonText.text = "Save Results";
        savePopupPanel.SetActive(false);

        if (gamePanel != null) gamePanel.SetActive(false);
        resultPanel.SetActive(true);

        if (player1NameText) player1NameText.text = p1Name;
        if (player2NameText) player2NameText.text = p2Name;

        if (player1ResultImage) { player1ResultImage.texture = p1Image; player1ResultImage.gameObject.SetActive(p1Image != null); }
        if (player2ResultImage) { player2ResultImage.texture = p2Image; player2ResultImage.gameObject.SetActive(p2Image != null); }

        finalPlayer1ScoreText.text = $"{p1TotalScore}";
        finalPlayer2ScoreText.text = $"{p2TotalScore}";

        player1CrownIcon.SetActive(false);
        player2CrownIcon.SetActive(false);
        drawMessage.SetActive(false);
        winnerText.gameObject.SetActive(true);

        if (p1TotalScore > p2TotalScore) { winnerText.text = $"{p1Name} Wins!"; player1CrownIcon.SetActive(true); }
        else if (p2TotalScore > p1TotalScore) { winnerText.text = $"{p2Name} Wins!"; player2CrownIcon.SetActive(true); }
        else { winnerText.gameObject.SetActive(false); drawMessage.SetActive(true); }
    }

    // --- NEW SAVE LOGIC ---

    void OpenSavePopup()
    {
        if (hasSaved) return;

        savePopupPanel.SetActive(true);
        p1NewNameInput.text = "";
        p2NewNameInput.text = "";

        // Populate Dropdowns from DB
        PlayerDatabase db = SaveSystem.Load();
        List<string> names = new List<string> { "-- Select Existing --" };
        foreach (var p in db.profiles) names.Add(p.playerName);

        p1Dropdown.ClearOptions(); p1Dropdown.AddOptions(names);
        p2Dropdown.ClearOptions(); p2Dropdown.AddOptions(names);
    }

    void SaveResultsToDatabase()
    {
        // Get P1 Name (Prioritize Input field over dropdown)
        string p1Name = "Player 1";
        if (!string.IsNullOrEmpty(p1NewNameInput.text)) p1Name = p1NewNameInput.text;
        else if (p1Dropdown.value > 0) p1Name = p1Dropdown.options[p1Dropdown.value].text;

        // Get P2 Name
        string p2Name = "Player 2";
        if (!string.IsNullOrEmpty(p2NewNameInput.text)) p2Name = p2NewNameInput.text;
        else if (p2Dropdown.value > 0) p2Name = p2Dropdown.options[p2Dropdown.value].text;

        // Save
        SaveSystem.SaveMatchResults(p1Name, p1FinalScore, p2Name, p2FinalScore, currentGameType);

        // Update UI Text visually
        if (player1NameText) player1NameText.text = p1Name;
        if (player2NameText) player2NameText.text = p2Name;
        if (p1FinalScore > p2FinalScore) winnerText.text = $"{p1Name} Wins!";
        else if (p2FinalScore > p1FinalScore) winnerText.text = $"{p2Name} Wins!";

        // Close panel and lock button
        savePopupPanel.SetActive(false);
        hasSaved = true;
        openSavePanelButton.interactable = false;
        if (openSavePanelButtonText) openSavePanelButtonText.text = "Saved!";
    }

    // ----------------------

    public void OnRestartButtonPressed()
    {
        var detection = FindObjectOfType<DetectionManager>();
        if (detection != null) detection.ResetDetection();
        
        resultPanel.SetActive(false);
        detectionPanel.SetActive(true);

        var gameMgr = FindObjectOfType<CalorieGameManager>();
        if (gameMgr != null) gameMgr.ResetGame();
    }

    public void OnMenuButtonPressed()
    {
        resultPanel.SetActive(false);
        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
    }
}