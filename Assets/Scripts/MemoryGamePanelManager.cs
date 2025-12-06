using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Memory AR game:
/// - Pre-phase: show 3x4 grid of colors (3 rounds × 4 colors).
/// - Each row is a round, each color appears once per row (R, G, B, Y).
/// - During a round, players "touch" colors in AR (via their hands).
/// - We record each player's order (4 colors max).
/// - When time ends OR both selected 4 colors, we show:
///     - Correct order in the center
///     - Each player's row and tick/cross per slot
///     - Update scores
/// - After 3 rounds, show final results (via ResultsPanelManager).
/// </summary>
public class MemoryGamePanelManager : MonoBehaviour
{
    public enum MemoryColor
    {
        Red,
        Blue,
        Green,
        Yellow
    }

    [Header("Game Settings")]
    [Tooltip("How many rounds (rows) – your design says 3.")]
    public int totalRounds = 3;

    [Tooltip("Seconds to show the initial 3x4 pattern.")]
    public float memorizeDuration = 5f;

    [Tooltip("Seconds allowed per round for players to input.")]
    public float roundDuration = 10f;

    [Header("UI: Preview 3x4 Grid")]
    [Tooltip("12 image cells, in row-major order (R1C1..R1C4, R2C1..R2C4, R3C1..R3C4).")]
    public Image[] previewCells; // length = 12
    public GameObject memorizePanel; // the whole panel showing the 3x4 grid

    [Header("UI: Round Result Center Row")]
    public GameObject resultsOverlayPanel; // dim background + center area
    public Image[] correctRowSlots; // 4 slots

    [Header("UI: Player 1 Row + Marks")]
    public Image[] player1RowSlots; // 4 slots for colors
    public Image[] player1MarkIcons; // 4 slots for tick/cross

    [Header("UI: Player 2 Row + Marks")]
    public Image[] player2RowSlots; // 4 slots for colors
    public Image[] player2MarkIcons; // 4 slots for tick/cross

    [Header("UI: Texts")]
    public TMP_Text roundLabelText;
    public TMP_Text timerText;
    public TMP_Text player1ScoreText;
    public TMP_Text player2ScoreText;

    [Header("Sprites")]
    public Sprite tickSprite;
    public Sprite crossSprite;
    public Sprite emptyMarkSprite; // optional, used when no mark yet

    [Header("Dependencies")]
    [Tooltip("Optional: use your existing results panel to show final scores.")]
    public ResultsPanelManager resultsPanelManager;

    [Header("Panels")]
    [Tooltip("Root panel for the whole memory game UI.")]
    public GameObject gamePanel;

    // --- Internal State ---
    private MemoryColor[][] _roundPatterns;    // [round][slot index 0..3]
    private List<MemoryColor> _p1Selections;   // per round
    private List<MemoryColor> _p2Selections;   // per round

    private int _currentRoundIndex = 0;        // 0..totalRounds-1
    private int _player1TotalScore = 0;
    private int _player2TotalScore = 0;

    private bool _roundActive = false;

    // For easy static access from other scripts (optional)
    public static MemoryGamePanelManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple MemoryGamePanelManagers in scene. Keeping the first one.");
        }
        else
        {
            Instance = this;
        }
    }

    private void OnEnable()
    {
        ResetGame();
    }

    // Call this from your "Start" button or from another manager.
    public void StartGame()
    {
        StopAllCoroutines();
        ResetGame();
        gamePanel.SetActive(true);
        StartCoroutine(GameFlowCoroutine());
    }

    private void ResetGame()
    {
        _currentRoundIndex = 0;
        _player1TotalScore = 0;
        _player2TotalScore = 0;
        _roundActive = false;

        _p1Selections = new List<MemoryColor>(4);
        _p2Selections = new List<MemoryColor>(4);

        UpdateScoreTexts();
        ClearAllUI();
    }

    private void ClearAllUI()
    {
        // Clear preview cells
        if (previewCells != null)
        {
            foreach (var img in previewCells)
            {
                if (img != null) img.color = Color.clear;
            }
        }

        // Clear center correct row
        if (correctRowSlots != null)
        {
            foreach (var img in correctRowSlots)
            {
                if (img != null) img.color = Color.clear;
            }
        }

        // Clear player rows and marks
        ClearRow(player1RowSlots, player1MarkIcons);
        ClearRow(player2RowSlots, player2MarkIcons);

        if (timerText != null) timerText.text = "";
        if (roundLabelText != null) roundLabelText.text = "";

        if (resultsOverlayPanel != null) resultsOverlayPanel.SetActive(false);
        if (memorizePanel != null) memorizePanel.SetActive(false);
    }

    private void ClearRow(Image[] rowSlots, Image[] markIcons)
    {
        if (rowSlots != null)
        {
            foreach (var img in rowSlots)
            {
                if (img != null) img.color = Color.clear;
            }
        }

        if (markIcons != null)
        {
            foreach (var img in markIcons)
            {
                if (img != null)
                {
                    if (emptyMarkSprite != null)
                        img.sprite = emptyMarkSprite;
                    else
                        img.enabled = false;
                }
            }
        }
    }

    private IEnumerator GameFlowCoroutine()
    {
        // 1. Generate random patterns for all rounds
        GenerateAllRoundPatterns();

        // 2. Show memorize grid (3x4)
        ShowMemorizeGrid();
        if (memorizeDuration > 0f)
        {
            yield return new WaitForSeconds(memorizeDuration);
        }
        if (memorizePanel != null) memorizePanel.SetActive(false);

        // 3. Play each round
        for (_currentRoundIndex = 0; _currentRoundIndex < totalRounds; _currentRoundIndex++)
        {
            yield return StartCoroutine(PlaySingleRound());
        }

        // 4. Final results
        yield return new WaitForSeconds(1.0f);

        if (resultsPanelManager != null)
        {
            // Re-use your existing results panel; we don't need images, so pass nulls.
            resultsPanelManager.ShowResults(
                _player1TotalScore,
                _player2TotalScore,
                null,
                null
            );
        }

        // Optionally hide game panel when finished
        // gamePanel.SetActive(false);
    }

    #region Pattern Generation

    private void GenerateAllRoundPatterns()
    {
        _roundPatterns = new MemoryColor[totalRounds][];

        // We will create permutations of the 4 colors for each round.
        var baseColors = new List<MemoryColor>
        {
            MemoryColor.Red,
            MemoryColor.Blue,
            MemoryColor.Green,
            MemoryColor.Yellow
        };

        for (int r = 0; r < totalRounds; r++)
        {
            _roundPatterns[r] = GenerateRandomPermutation(baseColors);
        }
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
        if (previewCells == null || previewCells.Length < totalRounds * 4)
        {
            Debug.LogWarning("Preview cells not set correctly. Need 3x4 = 12 cells.");
            return;
        }

        // Fill 3x4 grid: row r, col c => index = r*4 + c
        for (int r = 0; r < totalRounds; r++)
        {
            var rowPattern = _roundPatterns[r];
            for (int c = 0; c < 4; c++)
            {
                int index = r * 4 + c;
                var img = previewCells[index];
                if (img != null)
                {
                    img.color = GetUnityColor(rowPattern[c]);
                }
            }
        }
    }

    #endregion

    #region Round Logic

    private IEnumerator PlaySingleRound()
    {
        // Reset per-round selections
        _p1Selections.Clear();
        _p2Selections.Clear();
        ClearRow(player1RowSlots, player1MarkIcons);
        ClearRow(player2RowSlots, player2MarkIcons);

        // Update round label
        if (roundLabelText != null)
        {
            roundLabelText.text = $"Round {_currentRoundIndex + 1} / {totalRounds}";
        }

        // Don't show the full 3x4 grid now — only show rows in results.
        if (memorizePanel != null) memorizePanel.SetActive(false);
        if (resultsOverlayPanel != null) resultsOverlayPanel.SetActive(false);

        // Now start the input phase
        float timeLeft = roundDuration;
        _roundActive = true;

        while (timeLeft > 0f && (!HasFinishedInput(_p1Selections) || !HasFinishedInput(_p2Selections)))
        {
            timeLeft -= Time.deltaTime;
            if (timerText != null)
            {
                timerText.text = Mathf.CeilToInt(timeLeft).ToString("0");
            }
            yield return null;
        }

        _roundActive = false;

        if (timerText != null) timerText.text = "Time!";

        // Evaluate results
        EvaluateRound();

        // Show results overlay for a few seconds
        if (resultsOverlayPanel != null) resultsOverlayPanel.SetActive(true);

        yield return new WaitForSeconds(4f);

        if (resultsOverlayPanel != null) resultsOverlayPanel.SetActive(false);
        if (timerText != null) timerText.text = "";
    }

    private bool HasFinishedInput(List<MemoryColor> selections)
    {
        return selections.Count >= 4;
    }

    /// <summary>
    /// Call this when a player's hand touches a color pad in AR.
    /// playerIndex: 1 or 2 (player 1, player 2).
    /// </summary>
    public void OnPlayerColorTouched(int playerIndex, MemoryColor color)
    {
        if (!_roundActive)
            return;

        List<MemoryColor> targetList = playerIndex == 1 ? _p1Selections : _p2Selections;
        if (targetList == null)
            return;

        // If player already chose 4 colors, ignore further touches
        if (targetList.Count >= 4)
            return;

        // Optional: disallow repeating the same color in the same round
        if (targetList.Contains(color))
            return;

        targetList.Add(color);
        UpdatePlayerRowUI(playerIndex, targetList);
    }

    private void UpdatePlayerRowUI(int playerIndex, List<MemoryColor> selections)
    {
        Image[] rowSlots = playerIndex == 1 ? player1RowSlots : player2RowSlots;
        if (rowSlots == null) return;

        for (int i = 0; i < rowSlots.Length; i++)
        {
            if (rowSlots[i] == null) continue;

            if (i < selections.Count)
            {
                rowSlots[i].color = GetUnityColor(selections[i]);
            }
            else
            {
                rowSlots[i].color = Color.clear;
            }
        }
    }

    private void EvaluateRound()
    {
        var correctRow = _roundPatterns[_currentRoundIndex];

        // Ensure each player has a full 4-length list (pad with dummy if needed)
        var p1Row = BuildFixedRow(_p1Selections);
        var p2Row = BuildFixedRow(_p2Selections);

        // Show correct row in center
        if (correctRowSlots != null && correctRowSlots.Length == 4)
        {
            for (int i = 0; i < 4; i++)
            {
                if (correctRowSlots[i] != null)
                {
                    correctRowSlots[i].color = GetUnityColor(correctRow[i]);
                }
            }
        }

        // Compare and mark
        int p1ScoreThisRound = MarkPlayerRow(player1RowSlots, player1MarkIcons, p1Row, correctRow);
        int p2ScoreThisRound = MarkPlayerRow(player2RowSlots, player2MarkIcons, p2Row, correctRow);

        _player1TotalScore += p1ScoreThisRound;
        _player2TotalScore += p2ScoreThisRound;

        UpdateScoreTexts();

        Debug.Log($"Round {_currentRoundIndex + 1}: P1 +{p1ScoreThisRound}, P2 +{p2ScoreThisRound}");
    }

    private MemoryColor[] BuildFixedRow(List<MemoryColor> selections)
    {
        MemoryColor[] row = new MemoryColor[4];
        for (int i = 0; i < 4; i++)
        {
            if (selections != null && i < selections.Count)
            {
                row[i] = selections[i];
            }
            else
            {
                // Default filler; won't score unless by chance matches
                row[i] = MemoryColor.Red;
            }
        }
        return row;
    }

    private int MarkPlayerRow(Image[] rowSlots, Image[] markIcons, MemoryColor[] playerRow, MemoryColor[] correctRow)
    {
        int score = 0;

        for (int i = 0; i < 4; i++)
        {
            bool isCorrect = playerRow[i] == correctRow[i];
            if (isCorrect)
            {
                score++;
            }

            // Ensure the row slots show the playerRow colors
            if (rowSlots != null && i < rowSlots.Length && rowSlots[i] != null)
            {
                rowSlots[i].color = GetUnityColor(playerRow[i]);
            }

            // Tick/cross in markIcons
            if (markIcons != null && i < markIcons.Length && markIcons[i] != null)
            {
                markIcons[i].enabled = true;
                markIcons[i].sprite = isCorrect ? tickSprite : crossSprite;
            }
        }

        return score;
    }

    private void UpdateScoreTexts()
    {
        if (player1ScoreText != null)
        {
            player1ScoreText.text = _player1TotalScore.ToString();
        }

        if (player2ScoreText != null)
        {
            player2ScoreText.text = _player2TotalScore.ToString();
        }
    }

    #endregion

    #region Utility

    private Color GetUnityColor(MemoryColor color)
    {
        switch (color)
        {
            case MemoryColor.Red:
                return Color.red;
            case MemoryColor.Blue:
                return Color.blue;
            case MemoryColor.Green:
                return Color.green;
            case MemoryColor.Yellow:
                // Slightly more visible yellow
                return new Color(1f, 0.92f, 0.016f, 1f);
            default:
                return Color.white;
        }
    }

    #endregion
}
