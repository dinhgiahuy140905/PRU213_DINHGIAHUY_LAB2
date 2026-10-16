using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central game flow controller: lives, game over, level completion and scene
/// transitions between the main menu, the level and the end screen.
///
/// It is created by the main menu and persists through scene loads
/// (see <see cref="EnsureExists"/> for direct-level play in the editor).
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public const string MainMenuScene = "MainMenu";
    public const string EndScene = "EndGame";

    [SerializeField] int startingLives = 3;

    public int Lives { get; private set; } = 3;
    public int LevelIndex { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool LevelFinished { get; private set; }
    public bool IsInvincible => invincibilityTimer > 0f;
    public float InvincibilityTimeLeft => Mathf.Max(0f, invincibilityTimer);

    float invincibilityTimer;
    bool loading;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Lives = startingLives;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        if (invincibilityTimer > 0f)
        {
            invincibilityTimer -= Time.deltaTime;
        }
    }

    public static GameManager EnsureExists()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("Game Manager");
            Instance = go.AddComponent<GameManager>();
        }

        return Instance;
    }

    /// <summary>Called by the main menu "Start" button.</summary>
    public void StartNewGame()
    {
        Lives = startingLives;
        IsGameOver = false;
        LevelFinished = false;
        invincibilityTimer = 0f;
        ScoreManager.EnsureExists().ResetAll();
        loading = false;
        SceneManager.LoadScene(GameSettings.LevelSceneName);
    }

    /// <summary>Called by the crash detector once the crash animation finished.</summary>
    public void PlayerCrashed()
    {
        if (IsGameOver || loading)
        {
            return;
        }

        Lives--;
        if (Lives <= 0)
        {
            GameOver();
        }
        else
        {
            RestartLevel();
        }
    }

    /// <summary>Called by the finish line.</summary>
    public void LevelCompleted()
    {
        if (LevelFinished || loading)
        {
            return;
        }

        LevelFinished = true;
        GameSettings.SubmitScore(ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0);
        LoadEndScreen();
    }

    public void GameOver()
    {
        if (IsGameOver || loading)
        {
            return;
        }

        IsGameOver = true;
        GameSettings.SubmitScore(ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0);
        LoadEndScreen();
    }

    public void RestartLevel()
    {
        LoadLevel(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        LoadLevel(MainMenuScene);
    }

    void LoadEndScreen()
    {
        LoadLevel(EndScene);
    }

    public void LoadLevel(string sceneName)
    {
        if (loading)
        {
            return;
        }

        loading = true;
        StartCoroutine(LoadRoutine(sceneName));
    }

    IEnumerator LoadRoutine(string sceneName)
    {
        Time.timeScale = 1f;
        yield return null;
        SceneManager.LoadScene(sceneName);
        loading = false;
    }

    public void AddInvincibility(float duration)
    {
        invincibilityTimer = Mathf.Max(invincibilityTimer, duration);
    }

    public void AddLife(int amount = 1)
    {
        Lives += amount;
    }

    /// <summary>Applies the difficulty preset to the player that just spawned.</summary>
    public void ApplyDifficultyTo(PlayerController player)
    {
        if (player == null)
        {
            return;
        }

        // The designer's tuning is kept and simply scaled by the difficulty.
        float multiplier = GameSettings.SpeedMultiplier;
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

        player.ApplyDifficulty(
            player.BaseSpeed * multiplier,
            player.MaxSpeed * multiplier);

        if (rb != null)
        {
            rb.gravityScale = GameSettings.GravityScale;
        }
    }
}
