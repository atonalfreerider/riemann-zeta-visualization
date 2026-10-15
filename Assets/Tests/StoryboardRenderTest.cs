using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Not a test of correctness: a headless render driver. Runs the tour in storyboard mode
/// (two PNGs per narration line) or full recording mode, so frames can be reviewed without a window.
///
///   ZETA_OUT=/path/out ZETA_MODE=storyboard  Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter StoryboardRenderTest
///   ZETA_MODE=record (full video via ffmpeg), ZETA_CHAPTER=<id>, ZETA_STORYBOARD_SECONDS=3, ZETA_FPS=30
/// Skipped unless ZETA_OUT is set.
/// </summary>
public class StoryboardRenderTest
{
    [UnityTest]
    [Timeout(3600000)]
    public IEnumerator RenderTour()
    {
        string outDir = Environment.GetEnvironmentVariable("ZETA_OUT");
        if (string.IsNullOrEmpty(outDir))
        {
            Assert.Ignore("ZETA_OUT not set; this is a render driver, not a test.");
            yield break;
        }

        string mode = Environment.GetEnvironmentVariable("ZETA_MODE") ?? "storyboard";
        SceneManager.LoadScene("zeta");
        yield return null;
        yield return null;

        TourDirector director = TourDirector.Instance;
        Assert.IsNotNull(director);
        director.Storyboard = mode == "storyboard";
        if (mode == "profile") Time.captureFramerate = 0; // real time, no capture: frame times reflect script + engine cost
        director.StoryboardDirectory = outDir;
        director.RecordOnStart = mode == "record";
        director.RecordDirectory = outDir;
        if (float.TryParse(Environment.GetEnvironmentVariable("ZETA_STORYBOARD_SECONDS"), out float seconds)) director.StoryboardSecondsPerLine = seconds;
        string chapter = Environment.GetEnvironmentVariable("ZETA_CHAPTER");
        if (!string.IsNullOrEmpty(chapter)) director.StartChapter = chapter;
        string endChapter = Environment.GetEnvironmentVariable("ZETA_END_CHAPTER");
        if (!string.IsNullOrEmpty(endChapter)) director.EndChapter = endChapter;
        director.Profile = Environment.GetEnvironmentVariable("ZETA_PROFILE") == "1";
        if (int.TryParse(Environment.GetEnvironmentVariable("ZETA_FPS"), out int fps)) director.Recorder.Fps = fps;
        if (int.TryParse(Environment.GetEnvironmentVariable("ZETA_WIDTH"), out int w)) director.Recorder.Width = w;
        if (int.TryParse(Environment.GetEnvironmentVariable("ZETA_HEIGHT"), out int hgt)) director.Recorder.Height = hgt;

        if (Environment.GetEnvironmentVariable("ZETA_SCRIPT") == "experiments") director.UseExperimentsScript(); // the Experiments modules instead of the tour
        if (Environment.GetEnvironmentVariable("ZETA_NO_CAPTIONS") == "1") director.Captions.GetComponent<Canvas>().enabled = false; // clean frames for thumbnails
        yield return null;
        director.StartTour();
        yield return null;
        Assert.IsTrue(director.Running);

        float started = Time.realtimeSinceStartup;
        while (director.Running && Time.realtimeSinceStartup - started < 3500f)
        {
            yield return null;
        }

        Assert.IsFalse(director.Running, "tour did not finish in time");
        yield return new WaitForSeconds(1f);
        Debug.Log("Render finished → " + outDir);
    }
}
