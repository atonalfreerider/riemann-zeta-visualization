using System;
using UnityEngine;

/// <summary>
/// Wires the tour, captions, annotations and bloom into the scene at runtime, so the
/// original zeta.unity scene needs no manual changes.
///
/// Command line (player builds):  -tour  -record  -landscape | -portrait  -chapter &lt;id&gt;  (default format: the screen's shape)
/// Editor: use the Zeta menu (Assets/Editor/ZetaMenu.cs) or press T in play mode.
/// </summary>
public static class ZetaBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        Ensure();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        Ensure();
    }

    /// <summary>Adds bloom and the tour to the current scene if it contains the visualization and has no tour yet.</summary>
    public static void Ensure()
    {
        Main main = UnityEngine.Object.FindAnyObjectByType<Main>();
        if (main == null) return; // not the visualization scene

        Camera cam = Camera.main != null ? Camera.main : UnityEngine.Object.FindAnyObjectByType<Camera>();
        bool vr = VRMode.Wanted(out bool headset);
        if (cam != null)
        {
            // a headset renders to a plain 8-bit eye buffer: no HDR, no post-processing
            cam.allowHDR = !vr;
            cam.backgroundColor = new Color(0.012f, 0.012f, 0.02f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            if (!vr && cam.GetComponent<BloomEffect>() == null)
            {
                cam.gameObject.AddComponent<BloomEffect>();
            }
        }

        if (UnityEngine.Object.FindAnyObjectByType<TourDirector>() != null) return;

        if (vr && cam != null && !VRMode.Enabled)
        {
            // the rig and the interface come up first so the tour's captions and camera cues find them
            VRApp.Create(VRRig.Create(cam, headset));
        }

        GameObject tourObject = new("Tour");
        TourDirector director = tourObject.AddComponent<TourDirector>();
        if (vr) director.Portrait = false;

        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-tour": director.AutoStart = true; break;
                case "-record": director.AutoStart = true; director.RecordOnStart = true; break;
                case "-landscape": director.Portrait = false; break;
                case "-portrait": director.Portrait = true; break;
                case "-chapter": if (i + 1 < args.Length) director.StartChapter = args[++i]; break;
                case "-storyboard": director.AutoStart = true; director.Storyboard = true; break;
                case "-storyboard-dir": if (i + 1 < args.Length) director.StoryboardDirectory = args[++i]; break;
            }
        }

#if UNITY_EDITOR
        if (UnityEditor.EditorPrefs.GetBool("Zeta.AutoStartTour", false))
        {
            director.AutoStart = true;
            UnityEditor.EditorPrefs.SetBool("Zeta.AutoStartTour", false);
        }

        if (UnityEditor.EditorPrefs.GetBool("Zeta.RecordTour", false))
        {
            director.AutoStart = true;
            director.RecordOnStart = true;
            UnityEditor.EditorPrefs.SetBool("Zeta.RecordTour", false);
        }

        if (UnityEditor.EditorPrefs.GetBool("Zeta.Storyboard", false))
        {
            director.AutoStart = true;
            director.Storyboard = true;
            director.StoryboardDirectory = UnityEditor.EditorPrefs.GetString("Zeta.StoryboardDir", "");
            director.StoryboardSecondsPerLine = UnityEditor.EditorPrefs.GetFloat("Zeta.StoryboardSeconds", 3f);
            UnityEditor.EditorPrefs.SetBool("Zeta.Storyboard", false);
        }

        string chapter = UnityEditor.EditorPrefs.GetString("Zeta.StartChapter", "");
        if (!string.IsNullOrEmpty(chapter))
        {
            director.StartChapter = chapter;
            UnityEditor.EditorPrefs.SetString("Zeta.StartChapter", "");
        }
#endif
    }
}
