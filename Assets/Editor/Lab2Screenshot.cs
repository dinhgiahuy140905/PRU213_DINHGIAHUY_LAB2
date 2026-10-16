#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Developer helper: boots the game and writes screenshots of the main menu and of the
/// level, so the layout can be reviewed without a human sitting in front of the editor.
///
/// Run with (note: no -nographics, we need real rendering):
///   Unity.exe -batchmode -projectPath &lt;project&gt; -executeMethod Lab2Screenshot.Run
/// </summary>
public static class Lab2Screenshot
{
    const string ActiveKey = "Lab2Shot.Active";
    const string PhaseKey = "Lab2Shot.Phase";
    const string StartKey = "Lab2Shot.Start";
    const string PhaseStartKey = "Lab2Shot.PhaseStart";

    const string OutputFolder = "C:/Users/Admin/AppData/Local/Temp/opencode/shots";

    static bool hooked;

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
        System.IO.Directory.CreateDirectory(OutputFolder);

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
                    Next(1);
                }
                break;

            case 1:
                if (elapsed > 1.5f)
                {
                    Capture("01_main_menu");
                    Next(2);
                }
                break;

            case 2:
                if (elapsed > 0.8f)
                {
                    MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
                    if (menu != null)
                    {
                        menu.StartGame();
                    }
                    Next(3);
                }
                break;

            case 3:
                if (elapsed > 8f)
                {
                    Capture("02_level");
                    Next(4);
                }
                break;

            case 4:
                if (elapsed > 0.4f)
                {
                    CrashDetector crash = Object.FindAnyObjectByType<CrashDetector>();
                    if (crash != null)
                    {
                        crash.StartCoroutine(crash.Crash());
                    }
                    Next(5);
                }
                break;

            case 5:
                if (elapsed > 0.3f)
                {
                    Capture("03_crash");
                    Next(6);
                }
                break;

            case 6:
                if (elapsed > 2.5f)
                {
                    GameManager game = Object.FindAnyObjectByType<GameManager>();
                    if (game != null)
                    {
                        game.GameOver();
                    }
                    Next(7);
                }
                break;

            case 7:
                if (elapsed > 2f)
                {
                    Capture("04_end_game");
                    Next(8);
                }
                break;

            case 8:
                if (elapsed > 0.6f)
                {
                    EditorApplication.ExitPlaymode();
                    Next(9);
                }
                break;

            case 9:
                if (!playing)
                {
                    SessionState.SetInt(ActiveKey, 0);
                    EditorApplication.update -= Tick;
                    Debug.Log("[Screenshot] Done.");
                    EditorApplication.Exit(0);
                }
                break;
        }
    }

    static void Next(int phase)
    {
        SessionState.SetInt(PhaseKey, phase);
        SessionState.SetFloat(PhaseStartKey, Now());
    }

    static void Capture(string name)
    {
        string path = $"{OutputFolder}/{name}.png";

        Camera camera = Camera.main;
        if (camera == null)
        {
            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude);
            if (cameras.Length > 0)
            {
                camera = cameras[0];
            }
        }

        if (camera == null)
        {
            Debug.LogWarning("[Screenshot] No camera to render.");
            return;
        }

        // Screen-space overlay canvases are drawn by the UI system, not by the camera, so
        // they have to be switched to camera space for an off-screen render to include them.
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
        RenderMode[] modes = new RenderMode[canvases.Length];
        Camera[] owners = new Camera[canvases.Length];

        for (int i = 0; i < canvases.Length; i++)
        {
            modes[i] = canvases[i].renderMode;
            owners[i] = canvases[i].worldCamera;
            canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
            canvases[i].worldCamera = camera;
            canvases[i].planeDistance = 5f;
        }

        const int width = 1920;
        const int height = 1080;

        RenderTexture target = new RenderTexture(width, height, 24);
        RenderTexture previous = camera.targetTexture;

        camera.targetTexture = target;
        camera.Render();

        RenderTexture.active = target;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
        texture.Apply();
        RenderTexture.active = null;

        camera.targetTexture = previous;
        System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());

        Object.DestroyImmediate(texture);
        Object.DestroyImmediate(target);

        for (int i = 0; i < canvases.Length; i++)
        {
            canvases[i].renderMode = modes[i];
            canvases[i].worldCamera = owners[i];
        }

        Debug.Log($"[Screenshot] {path} (scene={SceneManager.GetActiveScene().name}, camera={camera.name})");
    }
}
#endif
