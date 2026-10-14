using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// A render driver, not a test: records every chapter of the tour as its own video, in both shapes, in one Unity
/// session. Each recording starts from a freshly loaded scene, so a chapter looks the same here as when it is started
/// alone from the VR chapter menu or from Zeta ▸ Play Tour from Chapter.
///
///   ZETA_OUT=/path/out  [ZETA_CHAPTERS=hook,zeno]  [ZETA_SHAPES=vertical,horizontal]  [ZETA_FPS=30]
///   Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter ChapterRecordingTest
///
/// Output: &lt;ZETA_OUT&gt;/NN-chapter-shape/{video.mp4,timeline.json}; Tools/render_chapters.py then adds the narration.
/// Vertical is 1080x1920, horizontal is 1920x1080. Skipped unless ZETA_OUT is set.
/// </summary>
public class ChapterRecordingTest
{
    [UnityTest]
    [Timeout(21600000)]
    public IEnumerator RenderChapters()
    {
        string outDir = Environment.GetEnvironmentVariable("ZETA_OUT");
        if (string.IsNullOrEmpty(outDir))
        {
            Assert.Ignore("ZETA_OUT not set; this is a render driver, not a test.");
            yield break;
        }

        string[] shapes = (Environment.GetEnvironmentVariable("ZETA_SHAPES") ?? "vertical,horizontal").Split(',', StringSplitOptions.RemoveEmptyEntries);
        string chapterList = Environment.GetEnvironmentVariable("ZETA_CHAPTERS");
        HashSet<string> only = string.IsNullOrEmpty(chapterList) ? null : new HashSet<string>(chapterList.Split(','));
        int fps = int.TryParse(Environment.GetEnvironmentVariable("ZETA_FPS"), out int f) ? f : 30;
        bool noCaptions = Environment.GetEnvironmentVariable("ZETA_NO_CAPTIONS") == "1";

        TourScriptData script = TourScriptLoader.Load();
        Assert.IsNotNull(script.chapters);

        foreach (string shape in shapes)
        {
            bool vertical = shape.Trim() == "vertical";
            for (int c = 0; c < script.chapters.Length; c++)
            {
                TourChapter chapter = script.chapters[c];
                if (only != null && !only.Contains(chapter.id)) continue;

                SceneManager.LoadScene("zeta");
                yield return null;
                yield return null;

                TourDirector director = TourDirector.Instance;
                Assert.IsNotNull(director, "no tour director after loading the scene");
                string folder = Path.Combine(outDir, $"{c + 1:D2}-{chapter.id}-{(vertical ? "vertical" : "horizontal")}");
                director.Storyboard = false;
                director.RecordOnStart = true;
                director.RecordDirectory = folder;
                director.StartChapter = chapter.id;
                director.EndChapter = chapter.id;
                director.ContinueIntoNextChapter = false;
                director.Portrait = vertical;
                director.Recorder.Width = vertical ? 1080 : 1920;
                director.Recorder.Height = vertical ? 1920 : 1080;
                director.Recorder.Fps = fps;
                if (noCaptions) director.Captions.GetComponent<Canvas>().enabled = false;

                yield return null;
                director.StartTour();
                yield return null;
                Assert.IsTrue(director.Running, $"{chapter.id}: tour did not start");

                float started = Time.realtimeSinceStartup;
                while (director.Running && Time.realtimeSinceStartup - started < 3000f) yield return null;
                Assert.IsFalse(director.Running, $"{chapter.id} ({shape}) did not finish in time");
                yield return new WaitForSeconds(0.5f);
                Debug.Log($"Recorded {chapter.id} ({shape}) in {Time.realtimeSinceStartup - started:0}s → {folder}");
            }
        }
    }
}
