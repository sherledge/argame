using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public enum GameType { Pose, Reaction, Memory, Calorie }

[System.Serializable]
public class PlayerProfile
{
    public string playerName;
    
    // Personal Bests
    public int highScore_Pose;
    public int highScore_Reaction;
    public int highScore_Memory;
    public int highScore_Calorie;

    // Global Stats
    public int totalWins; 
}

[System.Serializable]
public class PlayerDatabase
{
    public List<PlayerProfile> profiles = new List<PlayerProfile>();
}

public static class SaveSystem
{
    private static string path => Application.persistentDataPath + "/playerData.json";

    public static void Save(PlayerDatabase db)
    {
        string json = JsonUtility.ToJson(db);
        File.WriteAllText(path, json);
    }

    public static PlayerDatabase Load()
    {
        if (File.Exists(path))
            return JsonUtility.FromJson<PlayerDatabase>(File.ReadAllText(path));
        return new PlayerDatabase();
    }

    // Helper to process and save match results
    public static void SaveMatchResults(string p1Name, int p1Score, string p2Name, int p2Score, GameType gameType)
    {
        PlayerDatabase db = Load();

        // Find or Create Player 1
        PlayerProfile p1 = db.profiles.FirstOrDefault(p => p.playerName.ToLower() == p1Name.ToLower());
        if (p1 == null) { p1 = new PlayerProfile { playerName = p1Name }; db.profiles.Add(p1); }

        // Find or Create Player 2
        PlayerProfile p2 = db.profiles.FirstOrDefault(p => p.playerName.ToLower() == p2Name.ToLower());
        if (p2 == null) { p2 = new PlayerProfile { playerName = p2Name }; db.profiles.Add(p2); }

        // Determine Winners
        if (p1Score > p2Score) p1.totalWins++;
        else if (p2Score > p1Score) p2.totalWins++;

        // Update High Scores
        UpdateHighScore(p1, gameType, p1Score);
        UpdateHighScore(p2, gameType, p2Score);

        Save(db);
        Debug.Log($"Saved Results for {p1Name} and {p2Name}");
    }

    private static void UpdateHighScore(PlayerProfile player, GameType gameType, int score)
    {
        switch (gameType)
        {
            case GameType.Pose: if (score > player.highScore_Pose) player.highScore_Pose = score; break;
            case GameType.Reaction: if (score > player.highScore_Reaction) player.highScore_Reaction = score; break;
            case GameType.Memory: if (score > player.highScore_Memory) player.highScore_Memory = score; break;
            case GameType.Calorie: if (score > player.highScore_Calorie) player.highScore_Calorie = score; break;
        }
    }
}