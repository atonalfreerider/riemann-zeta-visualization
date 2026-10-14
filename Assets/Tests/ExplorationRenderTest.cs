using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Headless snapshot set exploring the interference on the saddle (portrait PNGs). Not a correctness test.
///   ZETA_OUT=/path Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter ExplorationRenderTest
/// </summary>
public class ExplorationRenderTest
{
    [UnityTest]
    [Timeout(1800000)]
    public IEnumerator RenderSnapshots()
    {
        string outDir = Environment.GetEnvironmentVariable("ZETA_OUT");
        if (string.IsNullOrEmpty(outDir))
        {
            Assert.Ignore("ZETA_OUT not set");
            yield break;
        }

        SceneManager.LoadScene("zeta");
        yield return null;
        yield return null;
        TourDirector director = TourDirector.Instance;
        Main main = Main.Instance;
        Camera cam = Camera.main;
        cam.GetComponent<CameraControl>().enabled = false;
        director.Captions.GetComponent<Canvas>().enabled = false;
        main.SetPrimeCurveVisible(false); main.SetCosineVisible(false); main.SetEquationsVisible(false);
        main.SetZetaCurveVisible(false); main.SetGridVisible(false); main.SetZeroMarkersVisible(false);
        main.Limit.SetSpiralVisible(false);
        director.Annotations.HideAll();
        SacksSpiral sacks = director.Annotations.Sacks;
        sacks.ShowFull();
        sacks.ShowZetaOnly();
        BloomEffect bloom = cam.GetComponent<BloomEffect>();
        if (bloom != null) bloom.Intensity = 0.75f;
        director.Recorder.Width = 1080;
        director.Recorder.Height = 1920;
        director.Recorder.BeginOffscreen(outDir);
        Vector3 c = sacks.Center;
        Vector3 back = Vector3.back;

        IEnumerator Shot(string name, Vector3 pos, Vector3 look, Vector3 up, float fov = 55f)
        {
            cam.transform.position = pos;
            cam.transform.rotation = Quaternion.LookRotation(look - pos, up);
            cam.fieldOfView = fov;
            yield return null;
            yield return null;
            director.Recorder.Snapshot(name + ".png");
            yield return null;
            yield return null;
        }

        float Rr = 1.45f;
        // A. the saddle itself, with the spiral on it: ridge and valley spokes
        sacks.ShowSaddleWire(Rr * Mathf.Sqrt(420f), true);
        sacks.ShowTrail(false);
        yield return Shot("20-saddle-wire-top", c + back * 80f, c, Vector3.up, 55f);
        yield return Shot("21-saddle-wire-oblique", c + new Vector3(-40f, -70f, -45f), c + back * 2f, back);
        sacks.ShowSaddleWire(0f, false);

        // B. the chain's arrows on the spiral at the first zero's dial
        sacks.ShowChainField(14.134725f, 60);
        yield return Shot("22-chain-field-t14-top", c + back * 24f, c, Vector3.up, 50f);
        sacks.ShowChainField(21.022040f, 60);
        yield return Shot("23-chain-field-t21-top", c + back * 24f, c, Vector3.up, 50f);
        sacks.ShowChainField(3f, 60);
        yield return Shot("24-chain-field-t3-top", c + back * 24f, c, Vector3.up, 50f);

        // C. explicit-formula weights: the sawtooth n − ψ(n) riding the spiral
        sacks.ShowExplicitWave(30, 60f, 0.45f);
        yield return Shot("25-explicit-wave-center-top", c + back * 24f, c, Vector3.up, 50f);
        sacks.ShowExplicitWave(100, 120f, 0.3f);
        yield return Shot("26-explicit-wave-100zeros-top", c + back * 40f, c, Vector3.up, 50f);
        yield return Shot("27-explicit-wave-oblique", c + new Vector3(-20f, -34f, -20f), c + back * 1f, back);
        sacks.RestoreMasterWave();

        // D. the fringe sheet: n along the ridge, t up the axis
        sacks.ShowFringeSheet(60f, 62f, 13);
        Vector3 sheetMid = c + new Vector3(Rr * Mathf.Sqrt(60f) * .5f, 0f, 0f) + back * (31f * .25f);
        yield return Shot("28-fringe-sheet-front", sheetMid + new Vector3(0f, -30f, 0f), sheetMid, back, 55f);
        yield return Shot("29-fringe-sheet-oblique", sheetMid + new Vector3(-14f, -26f, -8f), sheetMid, back, 55f);
        yield return Shot("30-fringe-sheet-wide", sheetMid + new Vector3(-10f, -44f, -6f), sheetMid, back, 60f);
        sacks.ShowFringeSheet(120f, 62f, 13);
        Vector3 sheetMid2 = c + new Vector3(Rr * Mathf.Sqrt(120f) * .5f, 0f, 0f) + back * (31f * .25f);
        yield return Shot("31-fringe-sheet-120", sheetMid2 + new Vector3(0f, -38f, 0f), sheetMid2, back, 55f);

        // E. the whole picture: sheet standing on the saddle wire with the master wave and the trail
        sacks.ShowSaddleWire(Rr * Mathf.Sqrt(420f), true);
        sacks.ShowTrail(true);
        yield return Shot("32-sheet-on-saddle", c + new Vector3(-26f, -62f, -30f), c + new Vector3(6f, 0f, 0f) + back * 8f, back, 60f);

        AsyncGPUReadback_WaitAll();
        yield return new WaitForSeconds(1f);
        director.Recorder.EndOffscreen();
        Debug.Log("Exploration snapshots → " + outDir);
    }

    static void AsyncGPUReadback_WaitAll() => UnityEngine.Rendering.AsyncGPUReadback.WaitAllRequests();
}
