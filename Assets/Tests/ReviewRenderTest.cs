using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Headless review snapshots of the start-up experience (not a correctness test): the home view as the zeta trail grows,
/// the side menu, and the first chapter (sieve on the spiral, saddle routes and their equations) at regular intervals.
///   ZETA_OUT=/path [ZETA_PORTRAIT=1] [ZETA_REVIEW=home,menu,hook] Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter ReviewRenderTest
/// Also logs the wall-clock frame time while the home view grows (Debug.Log lines starting with [review]).
/// </summary>
public class ReviewRenderTest
{
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
        if (string.IsNullOrEmpty(parts)) parts = "home,menu,hook";
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

        if (parts.Contains("home"))
        {
            director.Home.Enter();
            float wallPrev = Time.realtimeSinceStartup, wallMax = 0f, wallSum = 0f;
            int frames = 0, shot = 0;
            float[] shotsAt = { 4f, 20f, 45f, 70f, 95f };
            float start = Time.time;
            while (Time.time - start < 100f)
            {
                float t = Time.time - start;
                if (shot < shotsAt.Length && t >= shotsAt[shot])
                {
                    director.Recorder.Snapshot($"{prefix}home_{shot}_{t:000}s_t{sacks.GrowthT:000}.png");
                    shot++;
                }

                yield return null;
                float now = Time.realtimeSinceStartup;
                float dt = now - wallPrev;
                wallPrev = now;
                if (frames > 5)
                {
                    wallSum += dt;
                    wallMax = Mathf.Max(wallMax, dt);
                }

                frames++;
            }

            Debug.Log($"[review] home view: {frames} frames, avg {wallSum / Mathf.Max(1, frames - 6) * 1000f:0.0} ms, max {wallMax * 1000f:0} ms, zeros merged {sacks.GrowthZerosMerged}");
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
            director.Recorder.Snapshot(prefix + "menu_open.png");
            yield return null;
            menu.Open = false;
            until = Time.realtimeSinceStartup + 1.2f;
            while (Time.realtimeSinceStartup < until) yield return null;
            director.Recorder.Snapshot(prefix + "menu_closed.png");
            yield return null;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        if (parts.Contains("hook"))
        {
            director.GoToChapter("hook", false);
            float start = Time.time;
            int n = 0;
            while (director.Running && Time.time - start < 400f)
            {
                if (Time.time - start >= n * 2.5f)
                {
                    director.Recorder.Snapshot($"{prefix}hook_{n:000}_{director.CurrentLineId}.png");
                    n++;
                }

                yield return null;
            }

            Debug.Log($"[review] hook chapter took {Time.time - start:0.0} s");
        }

        yield return null;
        yield return null;
        director.Recorder.EndOffscreen();
    }
}
