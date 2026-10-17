using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// The experiment modules in VR (desktop simulation, no headset): the Experiments tab of the dock plays them, Back / Next
/// stay among the modules, a tour chapter takes the script back, and every line of every module runs without an error
/// or exception while it is shown the way the headset shows it (an eye of about 1440 x 1584).
///
///   ExperimentNavigation    always runs.
///   PlayEveryExperiment     a render driver: ZETA_OUT=/path/out  [ZETA_EXP_ONLY=xcurves,xtorus]  [ZETA_SPEED=4]
///                           writes one picture per line (about two thirds through it) and fails on any logged error.
/// </summary>
public class VRExperimentsTest
{
    const int EyeWidth = 1440, EyeHeight = 1584;

    static IEnumerator Start(Action<VRApp> ready)
    {
        VRMode.RequestSimulation = true;
        SceneManager.LoadScene("zeta");
        yield return null;
        yield return null;
        float waited = 0f;
        while ((VRMode.App == null || !VRMode.App.Ready) && waited < 40f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        Assert.IsNotNull(VRMode.App, "VR app did not start");
        Assert.IsTrue(VRMode.App.Ready, "VR app did not become ready");
        ready(VRMode.App);
    }

    [UnityTest]
    [Timeout(600000)]
    public IEnumerator ExperimentNavigation()
    {
        VRApp app = null;
        yield return Start(a => app = a);
        TourDirector tour = app.Tour;
        AudioListener.volume = 0f;
        Assert.Greater(tour.ExperimentChapters.Length, 1, "the experiment modules should be loaded");
        string firstId = tour.ExperimentChapters[0].id, secondId = tour.ExperimentChapters[1].id;
        string tourChapterId = tour.Chapters[2].id;

        app.PlayExperiment(0);
        yield return WaitUntil(() => tour.Running && tour.PlayingExperiment && tour.CurrentLineId != "", 20f);
        Assert.IsTrue(tour.PlayingExperiment, "the first module should be playing");
        Assert.AreEqual(firstId, tour.CurrentChapterId);

        tour.NextChapter();
        yield return WaitUntil(() => tour.Running && tour.PlayingExperiment && tour.CurrentChapterId == secondId, 20f);
        Assert.IsTrue(tour.PlayingExperiment, "Next from a module should stay among the modules");
        Assert.AreEqual(secondId, tour.CurrentChapterId, "Next should go to the second module");

        // pressed again within a moment, Back goes to the module before
        tour.PreviousChapter();
        yield return WaitUntil(() => tour.Running && tour.PlayingExperiment && tour.CurrentChapterId == firstId, 20f);
        Assert.IsTrue(tour.PlayingExperiment, "Back from a module should stay among the modules");
        Assert.AreEqual(firstId, tour.CurrentChapterId, "Back should go to the first module");

        // a tour chapter from the Tour page: leaves the modules and plays the tour's own chapter
        tour.LeaveExperiment();
        tour.GoToChapter(2, false);
        yield return WaitUntil(() => tour.Running && !tour.PlayingExperiment && tour.CurrentChapterId == tourChapterId, 20f);
        Assert.IsFalse(tour.PlayingExperiment, "a tour chapter is not an experiment");
        Assert.AreEqual(tourChapterId, tour.CurrentChapterId, "the chapter of the tour should play");
        tour.StopTour();

        // a module played again after the tour is still the module
        app.PlayExperiment(1);
        yield return WaitUntil(() => tour.Running && tour.PlayingExperiment && tour.CurrentChapterId == secondId, 20f);
        Assert.AreEqual(secondId, tour.CurrentChapterId);
        tour.StopTour();
        Assert.IsFalse(tour.PlayingExperiment, "stopping puts the main script back");
        Assert.AreEqual(tourChapterId, tour.Chapters[2].id, "the tour's chapters are back");
    }

    [UnityTest]
    [Timeout(3600000)]
    public IEnumerator PlayEveryExperiment()
    {
        string outDir = Environment.GetEnvironmentVariable("ZETA_OUT");
        if (string.IsNullOrEmpty(outDir))
        {
            Assert.Ignore("ZETA_OUT not set; this is a render driver.");
            yield break;
        }

        Directory.CreateDirectory(outDir);
        string only = Environment.GetEnvironmentVariable("ZETA_EXP_ONLY") ?? "";
        float speed = float.TryParse(Environment.GetEnvironmentVariable("ZETA_SPEED"), out float sp) ? sp : 4f;

        List<string> problems = new();
        LogAssert.ignoreFailingMessages = true;
        Application.LogCallback collect = (condition, stack, type) =>
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                string first = stack.Split('\n')[0];
                problems.Add($"{type}: {condition}  [{first}]");
            }
        };
        Application.logMessageReceived += collect;

        VRApp app = null;
        yield return Start(a => app = a);
        TourDirector tour = app.Tour;
        VRRig rig = app.Rig;
        Camera cam = rig.Cam;
        AudioListener.volume = 0f;
        cam.aspect = EyeWidth / (float)EyeHeight; // the headset's eye, not the desktop window
        RenderTexture target = new(EyeWidth, EyeHeight, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        VRSimulatedHead head = cam.GetComponent<VRSimulatedHead>();

        // the Experiments tab, as the user sees it
        head.SetLook(0f, 24f);
        app.ShowPage(1);
        for (int i = 0; i < 20; i++) yield return null;
        yield return Shot(cam, target, Path.Combine(outDir, "00-experiments-tab.jpg"));
        app.Dock.SetOpen(false);
        head.SetLook(0f, 0f);

        TourChapter[] modules = tour.ExperimentChapters;
        for (int m = 0; m < modules.Length; m++)
        {
            TourChapter module = modules[m];
            if (!string.IsNullOrEmpty(only) && !(("," + only + ",").Contains("," + module.id + ","))) continue;

            string dir = Path.Combine(outDir, $"{m + 1:00}-{module.id}");
            Directory.CreateDirectory(dir);
            int problemsBefore = problems.Count;
            app.PlayExperiment(m);
            yield return WaitUntil(() => tour.Running && tour.PlayingExperiment && tour.CurrentChapterId == module.id, 30f);
            Assert.AreEqual(module.id, tour.CurrentChapterId, $"{module.id} did not start");

            Time.timeScale = speed;
            int lastLine = -1;
            bool shot = false;
            float started = Time.realtimeSinceStartup;
            while (tour.Running && tour.PlayingExperiment && tour.CurrentChapterId == module.id)
            {
                if (tour.CurrentLineIndex != lastLine)
                {
                    lastLine = tour.CurrentLineIndex;
                    shot = false;
                }

                if (!shot && tour.LineDuration > 0f && tour.LineElapsed >= 0.65f * tour.LineDuration)
                {
                    shot = true;
                    Time.timeScale = 1f; // a still frame for the picture, then on again
                    yield return Shot(cam, target, Path.Combine(dir, $"{lastLine + 1:00}-{tour.CurrentLineId}.jpg"));
                    Time.timeScale = speed;
                }

                if (Time.realtimeSinceStartup - started > 900f)
                {
                    Time.timeScale = 1f;
                    Assert.Fail($"{module.id} did not finish within 15 minutes");
                }

                yield return null;
            }

            Time.timeScale = 1f;
            Debug.Log($"[experiments] {module.id}: {module.lines.Length} lines, {Time.realtimeSinceStartup - started:0} s, {problems.Count - problemsBefore} problem(s)");
            for (int i = 0; i < 10; i++) yield return null;
        }

        Application.logMessageReceived -= collect;
        File.WriteAllLines(Path.Combine(outDir, "problems.txt"), problems);
        Assert.IsEmpty(problems, $"{problems.Count} error(s) while the modules ran:\n" + string.Join("\n", problems.GetRange(0, Math.Min(25, problems.Count))));
    }

    static IEnumerator WaitUntil(Func<bool> condition, float timeout)
    {
        float t = 0f;
        while (!condition() && t < timeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    /// <summary>The eye's picture, over a flat grey "room" so passthrough (alpha 0) shows.</summary>
    static IEnumerator Shot(Camera cam, RenderTexture target, string path)
    {
        yield return null;
        cam.targetTexture = target;
        cam.Render();
        cam.targetTexture = null;
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;
        Texture2D tex = new(target.width, target.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        tex.Apply();
        RenderTexture.active = previous;

        Color32[] px = tex.GetPixels32();
        for (int i = 0; i < px.Length; i++)
        {
            float a = px[i].a / 255f; // the eye buffer holds premultiplied colour
            px[i] = new Color32(
                (byte)Mathf.Min(255, px[i].r + (1f - a) * 92f),
                (byte)Mathf.Min(255, px[i].g + (1f - a) * 96f),
                (byte)Mathf.Min(255, px[i].b + (1f - a) * 104f), 255);
        }

        tex.SetPixels32(px);
        File.WriteAllBytes(path, tex.EncodeToJPG(86));
        UnityEngine.Object.Destroy(tex);
    }
}
