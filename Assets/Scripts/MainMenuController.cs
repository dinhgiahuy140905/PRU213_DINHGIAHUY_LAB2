using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// User Interface Design - the main menu (Start / Options / Quit).
///
/// Buttons are wired up by name at runtime, which keeps the scene setup simple
/// (see <c>Lab2SceneBuilder</c>) while still exposing public methods that can be
/// linked manually from the inspector.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [SerializeField] GameObject optionsPanel;
    [SerializeField] GameObject howToPanel;
    [SerializeField] Text difficultyLabel;
    [SerializeField] Text highScoreText;
    [SerializeField] Slider volumeSlider;

    void Awake()
    {
        HookButton("StartButton", StartGame);
        HookButton("OptionsButton", OpenOptions);
        HookButton("QuitButton", QuitGame);
        HookButton("CloseOptionsButton", CloseOptions);
        HookButton("HowToPlayButton", OpenHowToPlay);
        HookButton("CloseHelpButton", CloseHowToPlay);
        HookButton("EasyButton", () => SetDifficulty(GameDifficulty.Easy));
        HookButton("NormalButton", () => SetDifficulty(GameDifficulty.Normal));
        HookButton("HardButton", () => SetDifficulty(GameDifficulty.Hard));

        if (volumeSlider != null)
        {
            volumeSlider.value = GameSettings.MasterVolume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }

        if (howToPanel != null)
        {
            howToPanel.SetActive(false);
        }
    }

    void Start()
    {
        AudioListener.volume = GameSettings.MasterVolume;
        UpdateLabels();
        SelectFirstButton("StartButton");
    }

    /// <summary>Selects a button so the menu can be driven with the keyboard / gamepad.</summary>
    static void SelectFirstButton(string buttonName)
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            return;
        }

        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Exclude);
        foreach (Button button in buttons)
        {
            if (button.name == buttonName)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(button.gameObject);
                return;
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

    void UpdateLabels()
    {
        if (difficultyLabel != null)
        {
            difficultyLabel.text = "DIFFICULTY: " + GameSettings.Difficulty.ToString().ToUpperInvariant();
        }

        if (highScoreText != null)
        {
            highScoreText.text = GameSettings.HighScore.ToString("N0");
        }
    }

    // ---- button callbacks -------------------------------------------------

    public void StartGame()
    {
        GameManager.EnsureExists().StartNewGame();
    }

    public void OpenOptions()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(true);
        }
    }

    public void CloseOptions()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }

        UpdateLabels();
    }

    public void OpenHowToPlay()
    {
        if (howToPanel != null)
        {
            howToPanel.SetActive(true);
        }
    }

    public void CloseHowToPlay()
    {
        if (howToPanel != null)
        {
            howToPanel.SetActive(false);
        }
    }

    public void SetDifficulty(int value)
    {
        GameSettings.Difficulty = (GameDifficulty)Mathf.Clamp(value, 0, 2);
        UpdateLabels();
    }

    public void SetDifficulty(GameDifficulty value)
    {
        GameSettings.Difficulty = value;
        UpdateLabels();
    }

    public void SetVolume(float value)
    {
        GameSettings.MasterVolume = Mathf.Clamp01(value);
        AudioListener.volume = GameSettings.MasterVolume;
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
