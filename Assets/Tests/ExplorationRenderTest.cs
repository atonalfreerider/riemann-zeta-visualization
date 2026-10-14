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

        // 1. master wave with 30 zeros: centre region (trail hidden for the top-down views)
        sacks.ShowMasterStatic(30);
        sacks.ShowTrail(false);
        yield return Shot("01-master-center-top", c + back * 22f, c, Vector3.up, 50f);
        yield return Shot("02-master-inner-top", c + back * 12f, c, Vector3.up, 50f);
        yield return Shot("03-master-wide-top", c + back * 80f, c, Vector3.up, 55f);
        sacks.ShowTrail(true);
        yield return Shot("04-master-center-oblique", c + new Vector3(-12f, -20f, -14f), c + back * 1f, back);
        yield return Shot("05-master-wide-oblique", c + new Vector3(-40f, -70f, -45f), c + back * 2f, back);

        // 2. ring field, centre, trail hidden
        sacks.HideWaveStack();
        sacks.ShowRingFieldStatic(30);
        sacks.ShowTrail(false);
        yield return Shot("06-ringfield-center-top", c + back * 22f, c, Vector3.up, 50f);
        yield return Shot("07-ringfield-wide-top", c + back * 80f, c, Vector3.up, 55f);
        sacks.ShowTrail(true);
        sacks.HideInterference();

        // 3. layer stack, each wave at its own height (n ≤ 60 so the crests resolve)
        sacks.ShowLayerStack(30, 60f, 1.0f);
        sacks.HideWaveStack();
        yield return Shot("08-layers-side", c + new Vector3(0f, -46f, -16f), c + back * 17f, back, 60f);
        yield return Shot("09-layers-oblique", c + new Vector3(-30f, -30f, -28f), c + back * 15f, back, 60f);

        // 4. layer cross-sections along rays: cos(γ·log n) as a (log n, γ) fringe pattern
        sacks.ShowLayerSection(30, 60f, 0.9f, new Vector3(1f, 1f, 0f));
        yield return Shot("10-layersection-diagonal", c + new Vector3(24f, -24f, -17f), c + new Vector3(7f, 7f, 0f) + back * 17f, back, 60f);
        sacks.ShowLayerSection(30, 60f, 0.9f, new Vector3(1f, 0f, 0f));
        yield return Shot("11-layersection-x", c + new Vector3(8f, -34f, -17f), c + new Vector3(8f, 0f, 0f) + back * 17f, back, 60f);

        // 5. cumulative-sum cross-sections: prime towers emerging with k
        sacks.ShowCrossSection(30, 1.1f, 60f, 2.0f, new Vector3(1f, 1f, 0f));
        yield return Shot("12-sumsection-diagonal", c + new Vector3(24f, -24f, -17f), c + new Vector3(7f, 7f, 0f) + back * 17f, back, 60f);
        sacks.ShowCrossSection(30, 1.1f, 60f, 2.0f, new Vector3(1f, 0f, 0f));
        yield return Shot("13-sumsection-x-ridge", c + new Vector3(8f, -34f, -17f), c + new Vector3(8f, 0f, 0f) + back * 17f, back, 60f);
        sacks.ShowCrossSection(30, 1.1f, 60f, 2.0f, new Vector3(0f, 1f, 0f));
        yield return Shot("14-sumsection-y-valley", c + new Vector3(34f, 8f, -17f), c + new Vector3(0f, 8f, 0f) + back * 17f, back, 60f);
        sacks.ShowCrossSection(60, 0.6f, 120f, 2.0f, new Vector3(1f, 1f, 0f));
        yield return Shot("15-sumsection-60zeros", c + new Vector3(34f, -34f, -18f), c + new Vector3(10f, 10f, 0f) + back * 18f, back, 60f);

        // 6. partial-sum sheet along the spiral, inner turns only
        sacks.ShowPartialSumSheet(30, 1.1f, 60f, 2.0f);
        yield return Shot("16-partialsums-spiral-oblique", c + new Vector3(-22f, -30f, -26f), c + back * 16f, back, 60f);
        yield return Shot("17-partialsums-spiral-top", c + back * 60f, c, Vector3.up, 55f);

        // 7. trail + master wave together
        sacks.ShowMasterStatic(30);
        yield return Shot("18-trail-and-master", c + new Vector3(-30f, -75f, -30f), c + back * 14f, back);

        AsyncGPUReadback_WaitAll();
        yield return new WaitForSeconds(1f);
        director.Recorder.EndOffscreen();
        Debug.Log("Exploration snapshots → " + outDir);
    }

    static void AsyncGPUReadback_WaitAll() => UnityEngine.Rendering.AsyncGPUReadback.WaitAllRequests();
}
