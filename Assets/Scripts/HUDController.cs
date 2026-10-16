using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// User Interface Design - the in-game HUD.
///
/// Shows the score, the current combo / multiplier, the speed, the remaining lives
/// and the elapsed run time. It reads from the persistent <see cref="ScoreManager"/>
/// and <see cref="GameManager"/> and from the level's <see cref="PlayerController"/>.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Text fields")]
    [SerializeField] Text scoreText;
    [SerializeField] Text multiplierText;
    [SerializeField] Text comboText;
    [SerializeField] Text speedText;
    [SerializeField] Text livesText;
    [SerializeField] Text timerText;
    [SerializeField] Text trickText;

    [Header("Status")]
    [Tooltip("Shows temporary states such as INVINCIBLE.")]
    [SerializeField] Text statusText;

    [Header("Crash feedback")]
    [Tooltip("Full screen tint flashed when the player crashes.")]
    [SerializeField] Image crashFlash;

    [Header("Speed bar")]
    [SerializeField] Image speedBar;

    [Header("Banner")]
    [Tooltip("Short message shown for tricks / pickups.")]
    [SerializeField] Text bannerText;

    PlayerController player;
    float elapsed;

    void Start()
    {
        player = FindAnyObjectByType<PlayerController>();

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.Changed += Refresh;
        }

        if (bannerText != null)
        {
            bannerText.text = string.Empty;
        }

        Refresh();
    }

    void OnDestroy()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.Changed -= Refresh;
        }
    }

    void Update()
    {
        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>();
        }

        elapsed += Time.deltaTime;

        float speed = player != null ? player.DisplaySpeed : 0f;
        if (speedText != null)
        {
            speedText.text = $"SPEED  {speed,5:0} km/h";
        }

        if (speedBar != null && player != null)
        {
            speedBar.fillAmount = Mathf.Clamp01(player.CurrentSpeed / Mathf.Max(1f, player.MaxSpeed));
        }

        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(elapsed / 60f);
            int seconds = Mathf.FloorToInt(elapsed % 60f);
            timerText.text = $"TIME  {minutes:00}:{seconds:00}";
        }

        if (statusText != null)
        {
            bool invincible = GameManager.Instance != null && GameManager.Instance.IsInvincible;
            statusText.text = invincible ? "INVINCIBLE!" : string.Empty;
        }

        // Fade the crash tint out in real time (the game is in slow motion then).
        if (crashFlash != null)
        {
            Color color = crashFlash.color;
            if (color.a > 0f)
            {
                color.a = Mathf.MoveTowards(color.a, 0f, Time.unscaledDeltaTime * 0.8f);
                crashFlash.color = color;
            }
        }
    }

    /// <summary>Big, unmissable feedback when the player crashes into an obstacle.</summary>
    public void ShowCrash()
    {
        ShowBanner("CRASH!", 1.4f);

        if (crashFlash != null)
        {
            crashFlash.color = new Color(1f, 0.15f, 0.15f, 0.55f);
        }

        if (statusText != null)
        {
            statusText.text = string.Empty;
        }
    }

    /// <summary>Feedback for a non fatal hit (an ice patch / powder mound).</summary>
    public void ShowSlowDown()
    {
        ShowBanner("SLOWED!", 0.7f);

        if (crashFlash != null)
        {
            crashFlash.color = new Color(0.4f, 0.7f, 1f, 0.25f);
        }
    }

    /// <summary>Refreshes every value that comes from the score / game managers.</summary>
    public void Refresh()
    {
        ScoreManager score = ScoreManager.Instance;
        GameManager game = GameManager.Instance;

        if (scoreText != null)
        {
            scoreText.text = score != null ? $"SCORE  {score.Score:000000}" : "SCORE  000000";
        }

        if (multiplierText != null)
        {
            int multiplier = score != null ? score.Multiplier : 1;
            multiplierText.text = $"x{multiplier}";
            multiplierText.color = multiplier > 1 ? new Color(1f, 0.83f, 0.2f) : Color.white;
        }

        if (comboText != null)
        {
            int combo = score != null ? score.ComboCount : 0;
            float left = score != null ? score.ComboTimeLeft : 0f;
            comboText.text = combo > 0 ? $"COMBO {combo}   ({left:0.0}s)" : "COMBO  -";
        }

        if (livesText != null)
        {
            int lives = game != null ? game.Lives : 3;
            livesText.text = "LIVES  " + new string('\u2665', Mathf.Max(0, lives));
        }

        if (trickText != null && score != null)
        {
            trickText.text = $"TRICKS  {score.Tricks}    SNOWFLAKES  {score.Snowflakes}";
        }
    }

    /// <summary>Shows a temporary message in the middle of the screen.</summary>
    public void ShowBanner(string message, float duration = 1.2f)
    {
        if (bannerText == null)
        {
            return;
        }

        bannerText.text = message;
        CancelInvoke(nameof(ClearBanner));
        Invoke(nameof(ClearBanner), duration);
    }

    void ClearBanner()
    {
        if (bannerText != null)
        {
            bannerText.text = string.Empty;
        }
    }
}
