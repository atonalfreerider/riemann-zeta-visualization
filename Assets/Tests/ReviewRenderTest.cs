using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Single-frame layout checks (not a recording, not a correctness test): the scene is fast-forwarded with the time scale
/// and only a handful of PNGs are saved, at named moments.
///   ZETA_OUT=/path [ZETA_PORTRAIT=1] [ZETA_REVIEW=home,menu,hook,outro] Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter ReviewRenderTest
///   home   the home view: master wave ready, then the trail at t ≈ 150, 500 and 1000
///   menu   the side menu, open and tucked away
///   hook   one frame each from the sieve and the saddle routes
///   outro  one frame each from the per-zero waves, the full master wave and the closing home animation
/// </summary>
public class ReviewRenderTest
{
    const float FastForward = 6f;

    [UnityTest]
    [Timeout(3600000)]
    public IEnumerator RenderReview()
    {
        string outDir = Environment.GetEnvironmentVariable("ZETA_OUT");
        if (string.IsNullOrEmpty(outDir))
        {
            Assert.Ignore("ZETA_OUT not set");
            yield break;
        }

        string parts = Environment.GetEnvironmentVariable("ZETA_REVIEW");
        if (string.IsNullOrEmpty(parts)) parts = "home,menu,hook,outro";
        bool portrait = Environment.GetEnvironmentVariable("ZETA_PORTRAIT") == "1";

        SceneManager.LoadScene("zeta");
        yield return null;
        yield return null;
        TourDirector director = TourDirector.Instance;
        Camera cam = Camera.main;
        director.SetFormat(portrait);
        director.Recorder.BeginOffscreen(outDir);
        yield return null;
        string prefix = portrait ? "tall_" : "wide_";
        SacksSpiral sacks = director.Annotations.Sacks;

        IEnumerator Shot(string name)
        {
            director.Recorder.Snapshot(prefix + name + ".png");
            yield return null;
            yield return null;
        }

        if (parts.Contains("home"))
        {
            director.Home.Enter();
            // the master wave is computed in the background before it appears: wait for it in real time
            float until = Time.realtimeSinceStartup + 120f;
            while (sacks.FullWaveZerosShown < sacks.FullWaveZerosTarget && Time.realtimeSinceStartup < until) yield return null;
            Debug.Log($"[review] master wave ready: {sacks.FullWaveZerosShown} zeros, trail at t = {sacks.GrowthT:0}");
            for (int i = 0; i < 20; i++) yield return null;
            yield return Shot($"home_a_t{sacks.GrowthT:000}");
            float[] targets = { 150f, 500f, 1000f };
            foreach (float target in targets)
            {
                sacks.GrowthSpeedScale = 12f;
                while (sacks.GrowthT < target - 1f) yield return null;
                sacks.GrowthSpeedScale = 1f;
                for (int i = 0; i < 45; i++) yield return null; // let the camera settle
                yield return Shot($"home_b_t{sacks.GrowthT:0000}");
            }
        }

        if (parts.Contains("menu"))
        {
            SideMenu menu = UnityEngine.Object.FindAnyObjectByType<SideMenu>();
            Canvas canvas = menu.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; // draw the overlay into the offscreen frame so it can be seen
            canvas.worldCamera = cam;
            canvas.planeDistance = 0.4f;
            // batch mode has no real screen: size the canvas as the overlay would be on a screen the size of the frame
            UnityEngine.UI.CanvasScaler scaler = menu.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = Mathf.Min(director.Recorder.Width, director.Recorder.Height) / 1080f;
            menu.Open = true;
            float until = Time.realtimeSinceStartup + 1.2f;
            while (Time.realtimeSinceStartup < until) yield return null;
            yield return Shot("menu_open");
            menu.Open = false;
            until = Time.realtimeSinceStartup + 1.2f;
            while (Time.realtimeSinceStartup < until) yield return null;
            yield return Shot("menu_closed");
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        if (parts.Contains("hook"))
        {
            yield return ChapterFrames(director, "hook", new[] { ("hook_02c", .55f), ("hook_02d", .75f), ("hook_03b", .9f), ("hook_06", .6f), ("hook_06b", .6f), ("hook_06c", .6f) }, Shot);
        }

        if (parts.Contains("outro"))
        {
            yield return ChapterFrames(director, "outro", new[] { ("outro_04", .55f), ("outro_05", .5f), ("outro_06", .85f), ("outro_07", .6f) }, Shot);
        }

        Time.timeScale = 1f;
        yield return null;
        yield return null;
        director.Recorder.EndOffscreen();
    }

    /// <summary>Play a chapter fast-forwarded and save one frame at a fraction of each named line.</summary>
    static IEnumerator ChapterFrames(TourDirector director, string chapter, (string line, float at)[] moments, Func<string, IEnumerator> shot)
    {
        director.GoToChapter(chapter, false);
        Time.timeScale = FastForward;
        int next = 0;
        float start = Time.time;
        while (director.Running && next < moments.Length && Time.time - start < 900f)
        {
            if (director.CurrentLineId == moments[next].line && director.LineElapsed >= director.LineDuration * moments[next].at)
            {
                yield return shot($"{moments[next].line}_{(int)(moments[next].at * 100)}");
                next++;
                continue;
            }

            yield return null;
        }

        Time.timeScale = 1f;
        director.StopTour();
    }
}
