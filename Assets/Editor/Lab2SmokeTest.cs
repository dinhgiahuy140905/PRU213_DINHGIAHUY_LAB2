#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Head-less smoke test for the lab.
///
/// It boots the game from the main menu, presses START and then plays the level
/// until the run finishes (crash -> life lost -> level restart -> ... -> game over
/// -> end screen). Any error / exception / assertion logged along the way is
/// reported and turns the exit code into 2.
///
/// The state is kept inside <see cref="SessionState"/> so the test survives the
/// domain reload that happens when entering / leaving play mode.
///
/// Run with:
///   Unity.exe -batchmode -nographics -projectPath &lt;project&gt; -executeMethod Lab2SmokeTest.Run
/// </summary>
public static class Lab2SmokeTest
{
    const string ActiveKey = "Lab2Smoke.Active";
    const string PhaseKey = "Lab2Smoke.Phase";
    const string StartKey = "Lab2Smoke.Start";
    const string PhaseStartKey = "Lab2Smoke.PhaseStart";
    const string ErrorsKey = "Lab2Smoke.Errors";
    const string NextLogKey = "Lab2Smoke.NextLog";

    const string EndScene = "EndGame";

    const float MenuWait = 2f;
    const float LogInterval = 2f;      // game seconds between progress logs
    const float PlayTimeout = 300f;    // wall-clock safety for the play phase
    const float HardTimeout = 420f;

    static bool hooked;
    static float stuckTimer;

    [InitializeOnLoadMethod]
    static void AutoHook()
    {
        if (SessionState.GetInt(ActiveKey, 0) == 1)
        {
            Hook();
        }
    }

    static void Hook()
    {
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;

        if (!hooked)
        {
            hooked = true;
            EditorApplication.update += Tick;
        }
    }

    public static void Run()
    {
        SessionState.SetInt(ActiveKey, 1);
        SessionState.SetInt(PhaseKey, 0);
        SessionState.SetFloat(StartKey, Now());
        SessionState.SetFloat(PhaseStartKey, Now());
        SessionState.SetFloat(NextLogKey, 0f);
        SessionState.SetString(ErrorsKey, string.Empty);
        stuckTimer = 0f;

        Hook();
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    static float Now() => (float)EditorApplication.timeSinceStartup;

    static void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            string current = SessionState.GetString(ErrorsKey, string.Empty);
            SessionState.SetString(ErrorsKey, current + $"[{type}] {condition}\n{stackTrace}\n---\n");
        }
    }

    static void Tick()
    {
        if (SessionState.GetInt(ActiveKey, 0) != 1)
        {
            return;
        }

        float elapsed = Now() - SessionState.GetFloat(StartKey, Now());
        if (elapsed > HardTimeout)
        {
            Debug.Log("[SmokeTest] Hard timeout reached.");
            Finish();
            return;
        }

        int phase = SessionState.GetInt(PhaseKey, 0);
        float phaseElapsed = Now() - SessionState.GetFloat(PhaseStartKey, Now());

        switch (phase)
        {
            case 0: // wait for play mode
                if (EditorApplication.isPlaying)
                {
                    Debug.Log("[SmokeTest] Play mode entered on " + SceneManager.GetActiveScene().name);
                    Next(1);
                }
                break;

            case 1: // main menu -> press START
                if (phaseElapsed >= MenuWait)
                {
                    MainMenuController menu = UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
                    if (menu == null)
                    {
                        Log("MainMenuController not found.");
                    }
                    else
                    {
                        Debug.Log("[SmokeTest] Pressing START.");
                        menu.StartGame();
                    }
                    Next(2);
                }
                break;

            case 2: // play until the end screen shows up
            {
                string scene = SceneManager.GetActiveScene().name;
                float gameTime = Time.timeSinceLevelLoad;

                AutoPlay();

                if (gameTime >= SessionState.GetFloat(NextLogKey, 0f))
                {
                    SessionState.SetFloat(NextLogKey, gameTime + LogInterval);
                    ScoreManager score = ScoreManager.Instance;
                    GameManager game = GameManager.Instance;
                    PlayerController player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                    Debug.Log($"[SmokeTest] t={gameTime:0.0}s scene={scene} " +
                              $"score={(score != null ? score.Score : -1)} " +
                              $"flakes={(score != null ? score.Snowflakes : -1)} " +
                              $"tricks={(score != null ? score.Tricks : -1)} " +
                              $"lives={(game != null ? game.Lives : -1)} " +
                              $"pos={(player != null ? player.transform.position.ToString("F1") : "none")} " +
                              $"speed={(player != null ? player.CurrentSpeed.ToString("F1") : "0")}");
                }

                if (scene == EndScene)
                {
                    Debug.Log("[SmokeTest] Reached the end screen.");
                    Next(3);
                    EditorApplication.ExitPlaymode();
                }
                else if (phaseElapsed > PlayTimeout)
                {
                    Debug.LogWarning($"[SmokeTest] Play timeout - still in scene '{scene}'.");
                    Next(3);
                    EditorApplication.ExitPlaymode();
                }
                break;
            }

            case 3: // leaving play mode
                if (!EditorApplication.isPlaying)
                {
                    Next(4);
                }
                break;

            case 4:
                if (!EditorApplication.isPlaying)
                {
                    Finish();
                }
                break;
        }
    }

    /// <summary>
    /// Tiny "bot" for the test: it jumps when a fatal obstacle is coming up, which also
    /// proves that the course is actually completable.
    /// </summary>
    static void AutoPlay()
    {
        PlayerController player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        if (player == null)
        {
            return;
        }

        if (player.GetComponent<PhysicsSpy>() == null)
        {
            player.gameObject.AddComponent<PhysicsSpy>();
        }

        // Jump over fatal obstacles before reaching them. The lead distance scales with the
        // current speed, so a fast boarder jumps earlier (and cannot overrun the window).
        Vector3 position = player.transform.position;
        float lead = Mathf.Clamp(player.CurrentSpeed * 0.9f, 6f, 60f);
        Obstacle[] obstacles = UnityEngine.Object.FindObjectsByType<Obstacle>(FindObjectsInactive.Exclude);

        foreach (Obstacle obstacle in obstacles)
        {
            if (obstacle == null || !obstacle.IsFatal)
            {
                continue;
            }

            float distance = obstacle.transform.position.x - position.x;
            if (distance > 1.5f && distance < lead)
            {
                player.Jump();
                return;
            }
        }

        // Last resort: if the board has been crawling for a while, hop once.
        stuckTimer = player.CurrentSpeed < 1.5f ? stuckTimer + Time.deltaTime : 0f;
        if (stuckTimer > 2.5f)
        {
            stuckTimer = 0f;
            player.Jump();
        }
    }

    static void Next(int nextPhase)
    {
        SessionState.SetInt(PhaseKey, nextPhase);
        SessionState.SetFloat(PhaseStartKey, Now());
    }
    static void Log(string message)
    {
        OnLog(message, string.Empty, LogType.Error);
    }

    static void Finish()
    {
        SessionState.SetInt(ActiveKey, 0);
        EditorApplication.update -= Tick;
        hooked = false;

        string errors = SessionState.GetString(ErrorsKey, string.Empty);
        int count = errors.Length == 0
            ? 0
            : errors.Split(new[] { "\n---\n" }, StringSplitOptions.RemoveEmptyEntries).Length;

        Debug.Log($"[SmokeTest] Finished with {count} error(s).");
        if (count > 0)
        {
            Debug.Log("[SmokeTest] " + errors);
        }

        EditorApplication.Exit(count == 0 ? 0 : 2);
    }
}
#endif
