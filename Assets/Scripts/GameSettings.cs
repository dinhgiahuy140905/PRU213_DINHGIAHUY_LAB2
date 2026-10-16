using UnityEngine;

/// <summary>
/// Persistent settings chosen in the main menu: difficulty and character.
/// Static so the values survive scene changes without extra plumbing.
/// </summary>
public enum GameDifficulty
{
    Easy,
    Normal,
    Hard
}

public static class GameSettings
{
    public const string HighScoreKey = "SnowBoarder_HighScore";

    /// <summary>The scene played for the currently selected difficulty.</summary>
    public static string LevelSceneName => Difficulty switch
    {
        GameDifficulty.Easy => "Level1_Easy",
        GameDifficulty.Hard => "Level1_Hard",
        _ => "Level1_Normal"
    };

    /// <summary>Multiplier applied on top of the boarder's tuning (per difficulty).</summary>
    public static float SpeedMultiplier => Difficulty switch
    {
        GameDifficulty.Easy => 0.8f,
        GameDifficulty.Hard => 1.3f,
        _ => 1f
    };

    /// <summary>
    /// 100 Hz physics: the boarder can reach ~35 units/second, which is far more than
    /// the thickness of the slope collider, so a smaller fixed step is the cheapest way
    /// to rule out tunnelling through the ground.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ConfigurePhysics()
    {
        Time.fixedDeltaTime = 1f / 100f;
    }

    public static GameDifficulty Difficulty = GameDifficulty.Normal;
    public static float MasterVolume = 1f;

    /// <summary>Name shown in the HUD ("Snowboarder - Barry").</summary>
    public static string CharacterName = "Barry";

    public static float BaseSpeed => Difficulty switch
    {
        GameDifficulty.Easy => 16f,
        GameDifficulty.Hard => 26f,
        _ => 20f
    };

    public static float BoostSpeed => Difficulty switch
    {
        GameDifficulty.Easy => 32f,
        GameDifficulty.Hard => 50f,
        _ => 40f
    };

    public static float GravityScale => Difficulty switch
    {
        GameDifficulty.Easy => 1.6f,
        GameDifficulty.Hard => 2.4f,
        _ => 2f
    };

    public static int HighScore
    {
        get => PlayerPrefs.GetInt(HighScoreKey, 0);
        set
        {
            PlayerPrefs.SetInt(HighScoreKey, value);
            PlayerPrefs.Save();
        }
    }

    public static void SubmitScore(int score)
    {
        if (score > HighScore)
        {
            HighScore = score;
        }
    }
}
