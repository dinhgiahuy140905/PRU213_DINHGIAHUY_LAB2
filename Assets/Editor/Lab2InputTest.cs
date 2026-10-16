#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// Diagnostic: boots the game, injects a real keyboard event (the left arrow key) and
/// reports how far the boarder rotated. Use it to prove that the arrow keys reach the
/// player controller.
///
/// Run with:
///   Unity.exe -batchmode -nographics -projectPath &lt;project&gt; -executeMethod Lab2InputTest.Run
/// </summary>
public static class Lab2InputTest
{
    const string ActiveKey = "Lab2Input.Active";
    const string PhaseKey = "Lab2Input.Phase";
    const string PhaseStartKey = "Lab2Input.PhaseStart";
    const string StartKey = "Lab2Input.Start";

    static bool hooked;
    static float rotationBefore;
    static float heightBefore;

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
        Hook();
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    static float Now() => (float)EditorApplication.timeSinceStartup;

    static void Tick()
    {
        if (SessionState.GetInt(ActiveKey, 0) != 1)
        {
            return;
        }

        int phase = SessionState.GetInt(PhaseKey, 0);
        float elapsed = Now() - SessionState.GetFloat(PhaseStartKey, Now());
        bool playing = EditorApplication.isPlaying;

        switch (phase)
        {
            case 0:
                if (playing)
                {
                    Keyboard keyboard = Keyboard.current;
                    Debug.Log($"[InputTest] Keyboard device = {(keyboard != null ? keyboard.displayName : "NULL")}");
                    Next(1);
                }
                break;

            case 1:
                if (elapsed > 1.5f)
                {
                    MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
                    if (menu == null)
                    {
                        Debug.LogError("[InputTest] MainMenuController not found.");
                    }
                    else
                    {
                        menu.StartGame();
                    }
                    Next(2);
                }
                break;

            case 2: // wait for the level, then hold the LEFT arrow
                if (elapsed > 3f)
                {
                    PlayerController player = Object.FindAnyObjectByType<PlayerController>();
                    if (player == null)
                    {
                        Debug.LogError("[InputTest] No player found.");
                        Next(6);
                        break;
                    }

                    rotationBefore = player.transform.eulerAngles.z;
                    Debug.Log($"[InputTest] Before: rotation={rotationBefore:F1} grounded={player.IsGrounded} speed={player.CurrentSpeed:F1}");
                    SetKey(Key.LeftArrow, true);
                    Next(3);
                }
                break;

            case 3: // holding LEFT for a while
                if (elapsed > 1f)
                {
                    PlayerController player = Object.FindAnyObjectByType<PlayerController>();
                    float delta = Mathf.DeltaAngle(rotationBefore, player.transform.eulerAngles.z);
                    Debug.Log($"[InputTest] LEFT held 1s -> rotation delta = {delta:F1} deg " +
                              $"(grounded={player.IsGrounded}, speed={player.CurrentSpeed:F1})");
                    SetKey(Key.LeftArrow, false);

                    // Now try a jump.
                    heightBefore = player.transform.position.y;
                    SetKey(Key.Space, true);
                    Next(4);
                }
                break;

            case 4: // holding SPACE
                if (elapsed > 1f)
                {
                    PlayerController player = Object.FindAnyObjectByType<PlayerController>();
                    float rise = player.transform.position.y - heightBefore;
                    Debug.Log($"[InputTest] SPACE held 1s -> height change = {rise:F2}");
                    SetKey(Key.Space, false);
                    Next(5);
                }
                break;

            case 5:
                if (elapsed > 0.5f)
                {
                    EditorApplication.ExitPlaymode();
                    Next(6);
                }
                break;

            case 6:
                if (!playing)
                {
                    SessionState.SetInt(ActiveKey, 0);
                    EditorApplication.update -= Tick;
                    Debug.Log("[InputTest] Done.");
                    EditorApplication.Exit(0);
                }
                break;
        }
    }

    static void SetKey(Key key, bool pressed)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        InputSystem.QueueStateEvent(keyboard, pressed ? new KeyboardState(key) : new KeyboardState());
        InputSystem.Update();
    }

    static void Next(int phase)
    {
        SessionState.SetInt(PhaseKey, phase);
        SessionState.SetFloat(PhaseStartKey, Now());
    }
}
#endif
