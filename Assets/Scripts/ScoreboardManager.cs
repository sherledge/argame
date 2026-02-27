using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;

public class ScoreboardManager : MonoBehaviour
{
    public Transform contentParent; // ScrollView Content
    public GameObject rowPrefab;    // ScoreboardRowUI prefab

    private PlayerDatabase database;

    void Start()
    {
        database = SaveSystem.Load();
        ShowScoreboard(GameType.Pose); // Default view
    }

    public void ShowScoreboard(GameType gameType)
    {
        ClearRows();

        List<PlayerProfile> sortedList = GetSortedPlayers(gameType);

        for (int i = 0; i < sortedList.Count; i++)
        {
            GameObject row = Instantiate(rowPrefab, contentParent);
            SetupRow(row, sortedList[i], i + 1, gameType);
        }
    }

    public void ShowWinsScoreboard()
    {
        ClearRows();

        var sorted = database.profiles
            .OrderByDescending(p => p.totalWins)
            .ToList();

        for (int i = 0; i < sorted.Count; i++)
        {
            GameObject row = Instantiate(rowPrefab, contentParent);
            SetupRowWins(row, sorted[i], i + 1);
        }
    }

    private List<PlayerProfile> GetSortedPlayers(GameType gameType)
    {
        switch (gameType)
        {
            case GameType.Pose:
                return database.profiles
                    .Where(p => p.highScore_Pose > 0)
                    .OrderByDescending(p => p.highScore_Pose)
                    .ToList();

            case GameType.Reaction:
                return database.profiles
                    .Where(p => p.highScore_Reaction > 0)
                    .OrderByDescending(p => p.highScore_Reaction)
                    .ToList();

            case GameType.Memory:
                return database.profiles
                    .Where(p => p.highScore_Memory > 0)
                    .OrderByDescending(p => p.highScore_Memory)
                    .ToList();

            case GameType.Calorie:
                return database.profiles
                    .Where(p => p.highScore_Calorie > 0)
                    .OrderByDescending(p => p.highScore_Calorie)
                    .ToList();

            default:
                return new List<PlayerProfile>();
        }
    }

    private void SetupRow(GameObject row, PlayerProfile profile, int rank, GameType gameType)
    {
        row.transform.Find("RankText").GetComponent<TextMeshProUGUI>().text = rank.ToString();
        row.transform.Find("PlayerNameText").GetComponent<TextMeshProUGUI>().text = profile.playerName;

        int score = 0;
        switch (gameType)
        {
            case GameType.Pose: score = profile.highScore_Pose; break;
            case GameType.Reaction: score = profile.highScore_Reaction; break;
            case GameType.Memory: score = profile.highScore_Memory; break;
            case GameType.Calorie: score = profile.highScore_Calorie; break;
        }

        row.transform.Find("ScoreText").GetComponent<TextMeshProUGUI>().text = score.ToString();

    }

    private void SetupRowWins(GameObject row, PlayerProfile profile, int rank)
    {
        row.transform.Find("RankText").GetComponent<TextMeshProUGUI>().text = rank.ToString();
        row.transform.Find("PlayerNameText").GetComponent<TextMeshProUGUI>().text = profile.playerName;
        row.transform.Find("ScoreText").GetComponent<TextMeshProUGUI>().text = profile.totalWins.ToString();
    }

    private void ClearRows()
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);
    }
}
