using UnityEngine;

/// <summary>
/// Game Logic and Scoring Systems.
///
/// Owns the score, the snowflake counter, the trick counter and the combo /
/// multiplier system. It survives scene loads so a crash + level restart keeps
/// the score, while starting a new game from the menu resets it.
///
/// Scoring rules
///  * Snowflakes ............. +50 * multiplier
///  * Tricks ................. based on degrees of rotation, * multiplier
///  * Speed .................. small trickle of points while going fast
///  * Finish bonus ........... awarded by the finish line
/// The multiplier grows with the combo counter and resets when the combo times out
/// or when the player crashes.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    /// <summary>Raised whenever any displayed value changes.</summary>
    public event System.Action Changed;

    [Header("Combo")]
    [Tooltip("How long the combo survives without a new successful move.")]
    [SerializeField] float comboWindow = 3.5f;
    [SerializeField] int maxMultiplier = 5;

    [Header("Speed scoring")]
    [Tooltip("Points per second, per unit of speed.")]
    [SerializeField] float speedScoreRate = 0.15f;

    public int Score { get; private set; }
    public int Snowflakes { get; private set; }
    public int Tricks { get; private set; }
    public int ComboCount { get; private set; }
    public int BestCombo { get; private set; }
    public int Multiplier => Mathf.Clamp(1 + ComboCount / 3, 1, maxMultiplier);
    public float ComboTimeLeft => Mathf.Max(0f, comboTimer);
    public float ComboWindow => comboWindow;

    PlayerController player;
    float comboTimer;
    float speedAccumulator;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>Makes sure a score manager exists even when a level is played directly.</summary>
    public static ScoreManager EnsureExists()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("Score Manager");
            Instance = go.AddComponent<ScoreManager>();
        }

        return Instance;
    }

    void Update()
    {
        if (comboTimer > 0f)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                BreakCombo();
            }
        }

        // Speed scoring - a small trickle of points while the player is really moving.
        const float minScoringSpeed = 10f;

        if (player != null && player.CurrentSpeed > minScoringSpeed
            && GameManager.Instance != null && !GameManager.Instance.IsGameOver)
        {
            speedAccumulator += player.CurrentSpeed * speedScoreRate * Time.deltaTime;
            if (speedAccumulator >= 1f)
            {
                int points = (int)speedAccumulator;
                speedAccumulator -= points;
                Score += points;
                RaiseChanged();
            }
        }
    }

    /// <summary>The level's player registers itself here.</summary>
    public void SetPlayer(PlayerController playerController)
    {
        player = playerController;
        RaiseChanged();
    }

    /// <summary>A new run started from the main menu.</summary>
    public void ResetAll()
    {
        Score = 0;
        Snowflakes = 0;
        Tricks = 0;
        ComboCount = 0;
        BestCombo = 0;
        comboTimer = 0f;
        speedAccumulator = 0f;
        player = null;
        RaiseChanged();
    }

    public void CollectItem(int points)
    {
        Snowflakes++;
        RegisterCombo();
        AddPoints(points * Multiplier);
    }

    /// <summary>Registers a completed trick, e.g. 360 degrees.</summary>
    public void RegisterTrick(int degrees)
    {
        Tricks++;
        RegisterCombo();
        AddPoints(TrickPoints(degrees) * Multiplier);
    }

    public void AwardFinishBonus(int points)
    {
        AddPoints(points * Multiplier);
    }

    public void BreakCombo()
    {
        if (ComboCount == 0)
        {
            return;
        }

        ComboCount = 0;
        comboTimer = 0f;
        RaiseChanged();
    }

    static int TrickPoints(int degrees)
    {
        if (degrees >= 720) return 600;
        if (degrees >= 540) return 350;
        if (degrees >= 360) return 250;
        if (degrees >= 180) return 100;
        return 0;
    }

    void RegisterCombo()
    {
        ComboCount++;
        BestCombo = Mathf.Max(BestCombo, ComboCount);
        comboTimer = comboWindow;
        RaiseChanged();
    }

    void AddPoints(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Score += amount;
        RaiseChanged();
    }

    void RaiseChanged()
    {
        Changed?.Invoke();
    }
}
