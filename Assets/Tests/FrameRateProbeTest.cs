using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Frame-time diagnosis (no images): runs parts of the app at real speed and logs, every couple of seconds, the average
/// and worst frame and the per-frame cost of each <see cref="FrameProbe"/> section. Lines start with [probe].
///   ZETA_PROBE=home,stage,turn Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter FrameRateProbeTest
/// </summary>
public class FrameRateProbeTest
{
    [UnityTest]
    [Timeout(1800000)]
    public IEnumerator Probe()
    {
        string parts = Environment.GetEnvironmentVariable("ZETA_PROBE");
        if (string.IsNullOrEmpty(parts))
        {
            Assert.Ignore("ZETA_PROBE not set");
            yield break;
        }

        SceneManager.LoadScene("zeta");
        yield return null;
        yield return null;
        TourDirector director = TourDirector.Instance;
        FrameProbe.Enabled = true;

        if (parts.Contains("home"))
        {
            director.Home.Enter();
            yield return Watch("home", 24f, () => $"t = {director.Annotations.Sacks.GrowthT:0}");
        }

        if (parts.Contains("stage") || parts.Contains("turn"))
        {
            director.GoToChapter(parts.Contains("stage") ? "stage" : "turn", true);
            yield return Watch("tour", 150f, () => $"{director.CurrentLineId}  t = {director.Main.CurrentT:0.0}  zeros {director.Main.ZeroCount}  trail {director.Main.ZeroCount}");
            director.StopTour();
        }

        FrameProbe.Enabled = false;
    }

    static IEnumerator Watch(string label, float seconds, Func<string> state)
    {
        FrameProbe.Report(1);
        float start = Time.realtimeSinceStartup, windowStart = start, prev = start, worst = 0f;
        int frames = 0;
        while (Time.realtimeSinceStartup - start < seconds)
        {
            yield return null;
            float now = Time.realtimeSinceStartup;
            worst = Mathf.Max(worst, now - prev);
            prev = now;
            frames++;
            if (now - windowStart >= 2f)
            {
                Debug.Log($"[probe] {label} {now - start:0}s  {state()}  avg {(now - windowStart) / frames * 1000f:0.0} ms  worst {worst * 1000f:0} ms  GC0 {GC.CollectionCount(0)}\n{FrameProbe.Report(frames)}");
                windowStart = now;
                frames = 0;
                worst = 0f;
            }
        }
    }
}
