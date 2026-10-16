using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// User Interface Design - the end-of-run screen.
///
/// Shows whether the run was completed or lost, the final score, the best score,
/// the collected snowflakes, the number of tricks and the best combo, then offers
/// Retry / Main menu / Quit.
/// </summary>
public class EndGameController : MonoBehaviour
{
    [SerializeField] Text titleText;
    [SerializeField] Text scoreText;
    [SerializeField] Text detailText;
    [SerializeField] Text highScoreText;

    void Awake()
    {
        HookButton("RetryButton", Retry);
        HookButton("MenuButton", GoToMenu);
        HookButton("QuitButton", QuitGame);
    }

    void Start()
    {
        ScoreManager score = ScoreManager.Instance;
        GameManager game = GameManager.Instance;

        bool completed = game != null && game.LevelFinished && !game.IsGameOver;
        int finalScore = score != null ? score.Score : 0;
        GameSettings.SubmitScore(finalScore);

        if (titleText != null)
        {
            titleText.text = completed ? "LEVEL COMPLETE!" : "GAME OVER";
            titleText.color = completed ? new Color(0.55f, 0.95f, 0.55f) : new Color(1f, 0.5f, 0.5f);
        }

        if (scoreText != null)
        {
            scoreText.text = "SCORE  " + finalScore.ToString("000000");
        }

        if (detailText != null && score != null)
        {
            detailText.text = $"Snowflakes  {score.Snowflakes}      Tricks  {score.Tricks}      Best combo  {score.BestCombo}";
        }

        if (highScoreText != null)
        {
            highScoreText.text = "BEST  " + GameSettings.HighScore.ToString("000000");
        }

        // Keyboard / gamepad navigation.
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Exclude);
            foreach (Button button in buttons)
            {
                if (button.name == "RetryButton")
                {
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(button.gameObject);
                    break;
                }
            }
        }
    }

    void HookButton(string buttonName, UnityEngine.Events.UnityAction action)
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
        foreach (Button button in buttons)
        {
            if (button.name == buttonName)
            {
                button.onClick.AddListener(action);
            }
        }
    }

    public void Retry()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(GameSettings.LevelSceneName);
        }
    }

    public void GoToMenu()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadMainMenu();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(GameManager.MainMenuScene);
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
