using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Headless entry points, e.g.
///   Unity -batchmode -projectPath . -executeMethod ZetaBatch.Storyboard -logFile log.txt
///   Unity -batchmode -projectPath . -executeMethod ZetaBatch.Record     -logFile log.txt
/// Optional env: ZETA_OUT (output folder), ZETA_CHAPTER (chapter id), ZETA_STORYBOARD_SECONDS.
/// The editor leaves play mode when the tour ends and then exits.
/// </summary>
public static class ZetaBatch
{
    const string QuitFlag = "Zeta.BatchQuitAfterPlay";
    const string StartedKey = "Zeta.BatchStartedAt";

    public static void Storyboard()
    {
        EditorPrefs.SetBool("Zeta.Storyboard", true);
        EditorPrefs.SetString("Zeta.StoryboardDir", Environment.GetEnvironmentVariable("ZETA_OUT") ?? "");
        float seconds = float.TryParse(Environment.GetEnvironmentVariable("ZETA_STORYBOARD_SECONDS"), out float s) ? s : 3f;
        EditorPrefs.SetFloat("Zeta.StoryboardSeconds", seconds);
        Begin();
    }

    /// <summary>Back to the desktop build target after a Quest build (renders and tests then run as on the desktop).</summary>
    public static void SwitchToDesktop()
    {
        bool ok = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
        Debug.Log($"ZetaBatch: active build target {EditorUserBuildSettings.activeBuildTarget} (switched: {ok})");
        if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
    }

    public static void Record()
    {
        EditorPrefs.SetBool("Zeta.RecordTour", true);
        Begin();
    }

    static void Begin()
    {
        string chapter = Environment.GetEnvironmentVariable("ZETA_CHAPTER");
        if (!string.IsNullOrEmpty(chapter)) EditorPrefs.SetString("Zeta.StartChapter", chapter);
        EditorPrefs.SetBool(QuitFlag, true);
        EditorPrefs.SetString(StartedKey, DateTime.UtcNow.ToString("o"));
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        Debug.Log("ZetaBatch: entering play mode");
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    static void Resubscribe()
    {
        if (EditorPrefs.GetBool(QuitFlag, false))
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += Watchdog;
        }
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode && EditorPrefs.GetBool(QuitFlag, false))
        {
            EditorPrefs.SetBool(QuitFlag, false);
            Debug.Log("ZetaBatch: done, exiting editor");
            EditorApplication.Exit(0);
        }
    }

    static void Watchdog()
    {
        if (!EditorPrefs.GetBool(QuitFlag, false)) return;
        if (DateTime.TryParse(EditorPrefs.GetString(StartedKey, ""), null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime started)
            && (DateTime.UtcNow - started).TotalMinutes > 60)
        {
            EditorPrefs.SetBool(QuitFlag, false);
            Debug.LogError("ZetaBatch: watchdog timeout, exiting");
            EditorApplication.Exit(1);
        }
    }
}
