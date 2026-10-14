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
///   splash the launch card held still (also the Quest's system splash image)
///   hook, sacks, turn, staircase, key, outro   a few frames from each of those chapters
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
        if (string.IsNullOrEmpty(parts)) parts = "home,menu,splash,hook,sacks,turn,staircase,key,outro";
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

        if (parts.Contains("logo"))
        {
            // the graph view's intro: the launch card should be playing over it
            director.Home.Enter();
            LaunchTitle card = UnityEngine.Object.FindAnyObjectByType<LaunchTitle>();
            Assert.IsNotNull(card, "no launch card when the graph view opened");
            Canvas canvas = card.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 0.4f;
            UnityEngine.UI.CanvasScaler scaler = card.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = Mathf.Min(director.Recorder.Width, director.Recorder.Height) / 1080f;
            float until = Time.realtimeSinceStartup + 2.6f;
            while (Time.realtimeSinceStartup < until) yield return null;
            yield return Shot("logo_on_graph");
        }

        if (parts.Contains("sparkle"))
        {
            // consecutive frames of the home view as the camera orbits: flicker on the master wave shows up as differences
            director.Home.Enter();
            float until = Time.time + 12f;
            while (Time.time < until) yield return null;
            for (int f = 0; f < 3; f++)
            {
                director.Recorder.Snapshot($"{prefix}sparkle_{f}.png");
                yield return null;
            }

            yield return null;
            yield return null;
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

        if (parts.Contains("splash"))
        {
            // the launch card over the saddle, held fully drawn: the still used as the Quest's system splash
            director.Home.Enter();
            for (int i = 0; i < 90; i++) yield return null;
            foreach (LaunchTitle old in UnityEngine.Object.FindObjectsByType<LaunchTitle>(FindObjectsSortMode.None)) UnityEngine.Object.Destroy(old.gameObject);
            LaunchTitle card = LaunchTitle.Create(false);
            Canvas canvas = card.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 0.4f;
            UnityEngine.UI.CanvasScaler scaler = card.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = Mathf.Min(director.Recorder.Width, director.Recorder.Height) / 1080f;
            card.Freeze(4.2f);
            for (int i = 0; i < 4; i++) yield return null;
            yield return Shot("splash");
            UnityEngine.Object.Destroy(card.gameObject);
            card = LaunchTitle.Create(false);
            canvas = card.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 0.4f;
            scaler = card.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = Mathf.Min(director.Recorder.Width, director.Recorder.Height) / 1080f;
            card.Freeze(1.4f);
            for (int i = 0; i < 4; i++) yield return null;
            yield return Shot("splash_drawing");
            UnityEngine.Object.Destroy(card.gameObject);
        }

        (string chapter, (string line, float at)[] moments)[] chapters =
        {
            ("hook", new[] { ("hook_01", .85f), ("hook_03", .8f) }),
            ("sacks", new[] { ("sacks_01", .3f), ("sacks_05", .9f), ("sacks_06", .8f) }),
            ("turn", new[] { ("turn_01", .6f), ("turn_05", .6f) }),
            ("waves", new[] { ("wave_01", .5f), ("wave_04", .6f), ("wave_05", .7f) }),
            ("staircase", new[] { ("stair_04", .95f) }),
            ("key", new[] { ("key_01", .9f), ("key_03", .6f) }),
            ("outro", new[] { ("outro_02", .7f), ("outro_04", .8f) }),
        };
        foreach ((string chapter, (string line, float at)[] moments) in chapters)
        {
            if (parts.Contains(chapter)) yield return ChapterFrames(director, chapter, moments, Shot);
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
