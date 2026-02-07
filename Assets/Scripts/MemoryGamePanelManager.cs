using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MemoryGamePanelManager : MonoBehaviour, IGameStarter, ITutorialListener
{
    [Header("Audio")]
public AudioSource sfxSource;
public AudioClip padTouchSfx;

    public enum MemoryColor
    {
        Red,
        Blue,
        Green,
        Yellow
    }

    [Header("Game Settings")]
    public int totalRounds = 3;
    public float memorizeDuration = 5f;
    public float roundDuration = 10f;

    [Header("Camera Feed")]
    public RawImage cameraFeedRawImage; // <--- ASSIGN THIS IN INSPECTOR!

    [Header("Item Sprites")]
    public Sprite spriteRed;
    public Sprite spriteBlue;
    public Sprite spriteGreen;
    public Sprite spriteYellow;

    [Header("UI: Preview 3x4 Grid")]
    public Image[] previewCells; 
    public GameObject memorizePanel;

    [Header("UI: Round Result Center Row")]
    public GameObject resultsOverlayPanel; 
    public Image[] correctRowSlots; 

    [Header("UI: Player Rows")]
    public Image[] player1RowSlots; 
    public Image[] player1MarkIcons; 
    public Image[] player2RowSlots; 
    public Image[] player2MarkIcons; 

    [Header("UI: Texts")]
    public TMP_Text roundLabelText;
    public TMP_Text timerText;
    public TMP_Text player1ScoreText;
    public TMP_Text player2ScoreText;

    [Header("Feedback Sprites")]
    public Sprite tickSprite;
    public Sprite crossSprite;
    public Sprite emptyMarkSprite;

    [Header("Dependencies")]
    public ResultsPanelManager resultsPanelManager;
    public GameObject colorPanel;
    public GameObject gamePanel;

    // --- Internal State ---
    private MemoryColor[][] _roundPatterns;
    private List<MemoryColor> _p1Selections;
    private List<MemoryColor> _p2Selections;
    private int _currentRoundIndex = 0;
    private int _player1TotalScore = 0;
    private int _player2TotalScore = 0;
    private bool _roundActive = false;

    // --- Image Capture Variables ---
    private Texture2D player1FinalImage;
    private Texture2D player2FinalImage;

    public static MemoryGamePanelManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    public void StartGame()
    {
        StopAllCoroutines();
        ResetGame();
        gamePanel.SetActive(true);
        StartCoroutine(GameFlowCoroutine());
    }
bool IsSfxEnabled()
{
    return PlayerPrefs.GetInt("SFX_ENABLED", 1) == 1;
}

    private void ResetGame()
    {
        _currentRoundIndex = 0;
        _player1TotalScore = 0;
        _player2TotalScore = 0;
        _roundActive = false;
        _p1Selections = new List<MemoryColor>(4);
        _p2Selections = new List<MemoryColor>(4);

        // Cleanup old textures to free memory
        if (player1FinalImage != null) { Destroy(player1FinalImage); player1FinalImage = null; }
        if (player2FinalImage != null) { Destroy(player2FinalImage); player2FinalImage = null; }

        UpdateScoreTexts();
        ClearAllUI();
    }

    private void ClearAllUI()
    {
        foreach (var img in previewCells) HideImage(img);
        foreach (var img in correctRowSlots) HideImage(img);
        ClearRow(player1RowSlots, player1MarkIcons);
        ClearRow(player2RowSlots, player2MarkIcons);

        if (timerText != null) timerText.text = "";
        if (roundLabelText != null) roundLabelText.text = "";
        if (resultsOverlayPanel != null) resultsOverlayPanel.SetActive(false);
        if (memorizePanel != null) memorizePanel.SetActive(false);
    }

    private void HideImage(Image img)
    {
        if (img != null)
        {
            img.sprite = null;
            img.color = new Color(1, 1, 1, 0); 
        }
    }

    public void OnTutorialFinished()
    {
        StartGame();
    }

    private void ShowImage(Image img, Sprite s)
    {
        if (img != null && s != null)
        {
            img.sprite = s;
            img.color = Color.white; 
        }
    }

    private void ClearRow(Image[] rowSlots, Image[] markIcons)
    {
        if (rowSlots != null) foreach (var img in rowSlots) HideImage(img);
        if (markIcons != null)
        {
            foreach (var img in markIcons)
            {
                if (img != null)
                {
                    img.sprite = emptyMarkSprite;
                    img.enabled = (emptyMarkSprite != null);
                }
            }
        }
    }

    private IEnumerator GameFlowCoroutine()
    {
        GenerateAllRoundPatterns();
        ShowMemorizeGrid();

        if (memorizeDuration > 0f) yield return new WaitForSeconds(memorizeDuration);
        if (memorizePanel != null) memorizePanel.SetActive(false);
        
        if (colorPanel != null) colorPanel.SetActive(true);
        
        for (_currentRoundIndex = 0; _currentRoundIndex < totalRounds; _currentRoundIndex++)
        {
            yield return StartCoroutine(PlaySingleRound());
        }

        yield return new WaitForSeconds(1.0f);

        // --- CHANGED: Capture and Pass Images ---
        if (resultsPanelManager != null)
        {
            // 1. Capture the full screen feed
            Texture2D fullSnap = CaptureFromRawImage(cameraFeedRawImage);
            
            // 2. Split it into P1/P2
            SplitAndAssignFinalImages(fullSnap);

            // 3. Pass to Results Panel
            resultsPanelManager.ShowResults(_player2TotalScore, _player1TotalScore, player1FinalImage, player2FinalImage);
        }
    }

    // ... [Logic for Round Generation, PlaySingleRound, and Scoring remains the same] ...
    
    private void GenerateAllRoundPatterns()
    {
        _roundPatterns = new MemoryColor[totalRounds][];
        var baseColors = new List<MemoryColor> { MemoryColor.Red, MemoryColor.Blue, MemoryColor.Green, MemoryColor.Yellow };
        for (int r = 0; r < totalRounds; r++) _roundPatterns[r] = GenerateRandomPermutation(baseColors);
    }

    private MemoryColor[] GenerateRandomPermutation(List<MemoryColor> baseColors)
    {
        var list = new List<MemoryColor>(baseColors);
        MemoryColor[] result = new MemoryColor[list.Count];
        for (int i = 0; i < result.Length; i++)
        {
            int randomIndex = Random.Range(0, list.Count);
            result[i] = list[randomIndex];
            list.RemoveAt(randomIndex);
        }
        return result;
    }

    private void ShowMemorizeGrid()
    {
        if (memorizePanel != null) memorizePanel.SetActive(true);
        for (int r = 0; r < totalRounds; r++)
        {
            var rowPattern = _roundPatterns[r];
            for (int c = 0; c < 4; c++)
            {
                int index = r * 4 + c;
                ShowImage(previewCells[index], GetItemSprite(rowPattern[c]));
            }
        }
    }

    private IEnumerator PlaySingleRound()
    {
        _p1Selections.Clear();
        _p2Selections.Clear();
        ClearRow(player1RowSlots, player1MarkIcons);
        ClearRow(player2RowSlots, player2MarkIcons);

        if (roundLabelText != null) roundLabelText.text = $"Round {_currentRoundIndex + 1} / {totalRounds}";
        
        float timeLeft = roundDuration;
        _roundActive = true;

        while (timeLeft > 0f && (_p1Selections.Count < 4 || _p2Selections.Count < 4))
        {
            timeLeft -= Time.deltaTime;
            if (timerText != null) timerText.text = Mathf.CeilToInt(timeLeft).ToString("0");
            yield return null;
        }

        _roundActive = false;
        EvaluateRound();
        if (resultsOverlayPanel != null) resultsOverlayPanel.SetActive(true);
        yield return new WaitForSeconds(4f);
        if (resultsOverlayPanel != null) resultsOverlayPanel.SetActive(false);
    }

public void OnPlayerColorTouched(int playerIndex, MemoryColor color)
{
    if (!_roundActive) return;

    List<MemoryColor> targetList = playerIndex == 1 ? _p1Selections : _p2Selections;

    // Reject invalid touches
    if (targetList.Count >= 4 || targetList.Contains(color))
        return;

    // 🔊 PLAY SFX (valid touch only)
    if (IsSfxEnabled() && sfxSource != null && padTouchSfx != null)
    {
        sfxSource.PlayOneShot(padTouchSfx);
    }

    targetList.Add(color);
    UpdatePlayerRowUI(playerIndex, targetList);
}


    private void UpdatePlayerRowUI(int playerIndex, List<MemoryColor> selections)
    {
        Image[] rowSlots = playerIndex == 1 ? player1RowSlots : player2RowSlots;
        for (int i = 0; i < rowSlots.Length; i++)
        {
            if (i < selections.Count) ShowImage(rowSlots[i], GetItemSprite(selections[i]));
            else HideImage(rowSlots[i]);
        }
    }

    private void EvaluateRound()
    {
        var correctRow = _roundPatterns[_currentRoundIndex];
        for (int i = 0; i < 4; i++) ShowImage(correctRowSlots[i], GetItemSprite(correctRow[i]));

        _player1TotalScore += MarkPlayerRow(player1RowSlots, player1MarkIcons, BuildFixedRow(_p1Selections), correctRow);
        _player2TotalScore += MarkPlayerRow(player2RowSlots, player2MarkIcons, BuildFixedRow(_p2Selections), correctRow);
        UpdateScoreTexts();
    }

    private MemoryColor[] BuildFixedRow(List<MemoryColor> selections)
    {
        MemoryColor[] row = new MemoryColor[4];
        for (int i = 0; i < 4; i++) 
            row[i] = (i < selections.Count) ? selections[i] : (MemoryColor)(-1); 
        return row;
    }

    private int MarkPlayerRow(Image[] rowSlots, Image[] markIcons, MemoryColor[] playerRow, MemoryColor[] correctRow)
    {
        int score = 0;
        for (int i = 0; i < 4; i++)
        {
            bool isCorrect = playerRow[i] == correctRow[i];
            if (isCorrect) score++;

            ShowImage(rowSlots[i], GetItemSprite(playerRow[i]));
            if (markIcons[i] != null)
            {
                markIcons[i].enabled = true;
                markIcons[i].sprite = isCorrect ? tickSprite : crossSprite;
            }
        }
        return score;
    }

    private void UpdateScoreTexts()
    {
        if (player1ScoreText != null) player1ScoreText.text = _player1TotalScore.ToString();
        if (player2ScoreText != null) player2ScoreText.text = _player2TotalScore.ToString();
    }

    private Sprite GetItemSprite(MemoryColor color)
    {
        switch (color)
        {
            case MemoryColor.Red: return spriteRed;
            case MemoryColor.Blue: return spriteBlue;
            case MemoryColor.Green: return spriteGreen;
            case MemoryColor.Yellow: return spriteYellow;
            default: return null;
        }
    }

    // --- COPIED IMAGE PROCESSING LOGIC ---

    Texture2D CaptureFromRawImage(RawImage rawImage)
    {
        if (rawImage == null || rawImage.texture == null)
            return null;

        Texture src = rawImage.texture;

        RenderTexture rt = RenderTexture.GetTemporary(
            src.width,
            src.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear
        );

        Graphics.Blit(src, rt);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        return tex;
    }

    void SplitAndAssignFinalImages(Texture2D source)
    {
        if (source == null) return;

        int width = source.width;
        int height = source.height;
        int halfWidth = width / 2;

        // Player 1 (Right side in mirrored feed)
        Texture2D p1Tex = new Texture2D(halfWidth, height, source.format, false);
        p1Tex.SetPixels(source.GetPixels(halfWidth, 0, halfWidth, height));
        p1Tex.Apply();

        // Player 2 (Left side in mirrored feed)
        Texture2D p2Tex = new Texture2D(halfWidth, height, source.format, false);
        p2Tex.SetPixels(source.GetPixels(0, 0, halfWidth, height));
        p2Tex.Apply();

        player1FinalImage = p1Tex;
        player2FinalImage = p2Tex;
    }
}