using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Portrait snapshots of the thought experiments on the Sacks saddle (SacksExperiments.cs). Not a correctness test.
///   ZETA_OUT=/path Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter ExperimentsRenderTest
/// </summary>
public class ExperimentsRenderTest
{
    [UnityTest]
    [Timeout(1800000)]
    public IEnumerator RenderExperiments()
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
        if (director.Home != null) director.Home.Exit();
        if (director.Orbit != null) director.Orbit.End();
        director.Rig.Release();
        cam.GetComponent<CameraControl>().enabled = false;
        director.Captions.GetComponent<Canvas>().enabled = false;
        main.CancelAnimations();
        main.Pause();
        main.SetPrimeCurveVisible(false); main.SetCosineVisible(false); main.SetEquationsVisible(false);
        main.SetZetaCurveVisible(false); main.SetGridVisible(false); main.SetZeroMarkersVisible(false);
        main.Limit.SetSpiralVisible(false);
        main.Limit.SetTailVisible(false);
        director.Annotations.CancelAnimations();
        director.Annotations.HideAll();
        SacksSpiral sacks = director.Annotations.Sacks;
        sacks.ShowFull();
        sacks.ExpCleanStage();
        sacks.ShowRoutes(true);
        sacks.ExpNumberField(SacksSpiral.Count);
        BloomEffect bloom = cam.GetComponent<BloomEffect>();
        if (bloom != null) bloom.Intensity = 0.6f;
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

        IEnumerator Flatten(bool flat)
        {
            sacks.SetFlat(flat, 0.01f);
            yield return new WaitForSeconds(0.2f);
            sacks.ExpCleanStage(); // the layout pass re-enables the spiral's own dots
            sacks.ExpNumberField(SacksSpiral.Count);
        }

        // portrait: at distance d with fov 55 the frame is 1.04 d tall and 0.585 d wide; a label of size .075 d is ~28 px
        const float Full = 205f, Centre = 100f, Core = 36f;

        // ZETA_EXP_ONLY=AH renders only those sections (each section leaves the stage as it found it)
        string only = Environment.GetEnvironmentVariable("ZETA_EXP_ONLY");
        bool Wanted(char section) => string.IsNullOrEmpty(only) || only.Contains(section);

        if (Wanted('A')) {
        // A. cancellation curves on the saddle (inside the dotted spiral)
        sacks.ExpCancellationCurves(12, SacksSpiral.Count, 39, false, 4f, false, false);
        yield return Shot("A1-cancellation-saddle-full", c + back * Full, c, Vector3.up, 55f);
        yield return Shot("A2-cancellation-saddle-centre", c + back * Centre, c, Vector3.up, 55f);
        yield return Shot("A3-cancellation-saddle-oblique", c + new Vector3(-40f, -70f, -45f), c + back * 2f, back, 60f);
        sacks.ClearExperiments();

        // A, flat and extended: Euler's line to k = 110 with its crossings and the sister line
        yield return Flatten(true);
        sacks.ExpCancellationCurves(16, 12000, 110, true, 14f, true, true);
        Vector3 wide = c + new Vector3(12f, -58f, 0f);
        yield return Shot("A4-euler-crossings-flat-wide", wide + back * 215f, wide, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpCancellationCurves(16, 12000, 110, true, 7f, true, true);
        Vector3 lowerRight = c + new Vector3(44f, -62f, 0f);
        yield return Shot("A5-euler-crossings-flat-close", lowerRight + back * Centre, lowerRight, Vector3.up, 55f);
        Vector3 first = c + new Vector3(58f, -16f, 0f);
        yield return Shot("A6-euler-first-crossings", first + back * 60f, first, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpCancellationCurves(-1, 12000, 110, true, 4.5f, true, true);
        Vector3 centre = c + new Vector3(0f, -6f, 0f);
        yield return Shot("A7-euler-sister-centre", centre + back * 60f, centre, Vector3.up, 55f);

        // A2. the zeta wave along Euler's line (flat, same region)
        sacks.ClearExperiments();
        sacks.ExpCancellationCurves(-1, 12000, 110, false, 7f, false, false);
        sacks.ExpZetaWaveAlongEuler(110, main.zetaZeroValues.Length, 7f, 7f);
        yield return Shot("A8-zeta-wave-on-euler-wide", wide + back * 215f, wide, Vector3.up, 55f);
        yield return Shot("A9-zeta-wave-on-euler-close", lowerRight + back * Centre, lowerRight, Vector3.up, 55f);
        yield return Shot("A10-zeta-wave-on-euler-oblique", lowerRight + new Vector3(-20f, -70f, 0f) + back * 60f, lowerRight + back * 4f, back, 60f);
        sacks.ClearExperiments();
        yield return Flatten(false);
        }

        if (Wanted('C')) {
        // C. depth bars and Voronoi (the routes off: the surface alone)
        sacks.ShowRoutes(false);
        sacks.ExpDepthBars(SacksSpiral.Count, 0.55f);
        yield return Shot("C1-depth-bars-centre", c + back * Core, c, Vector3.up, 50f);
        yield return Shot("C2-depth-bars-oblique", c + new Vector3(-40f, -70f, -45f), c + back * 2f, back, 60f);
        yield return Shot("C3-depth-bars-low", c + new Vector3(-20f, -60f, -16f), c + back * 4f, back, 55f);
        sacks.ClearExperiments();
        sacks.ExpVoronoi(3000, 9f, 400f, 2.6f, false, false);
        yield return Shot("C4-voronoi-oblique", c + new Vector3(-24f, -40f, -30f), c + back * 2f, back, 60f);
        yield return Shot("C5-voronoi-top", c + back * 56f, c, Vector3.up, 50f);
        sacks.ClearExperiments();
        sacks.ShowBackdrop(false);
        sacks.ExpVoronoi(3000, 256f, 400f, 3.5f, true, true);
        yield return Shot("C6-voronoi-first-harmonic-side", c + new Vector3(0f, -78f, -30f), c + back * 4f, back, 50f);
        yield return Shot("C7-voronoi-first-harmonic-top", c + back * 66f, c, Vector3.up, 50f);
        sacks.ClearExperiments();
        sacks.ShowBackdrop(true);
        }

        if (Wanted('D')) {
        // D. the zeros' Sacks spiral (the number field off: the zeros alone on the saddle)
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        sacks.ExpGramSpiral(SacksSpiral.Count, 1.5f);
        yield return Shot("D1-gram-spiral-full", c + back * Full, c, Vector3.up, 55f);
        yield return Shot("D2-gram-spiral-centre", c + back * Centre, c, Vector3.up, 55f);
        yield return Shot("D3-gram-spiral-oblique", c + new Vector3(-40f, -70f, -45f), c + back * 2f, back, 60f);
        sacks.ClearExperiments();
        sacks.ShowBackdrop(true);
        }

        if (Wanted('E')) {
        // E. chain anatomy: at a zero, between zeros, at a Gram point (panels far from the saddle)
        double g100 = main.zetaZeroValues[99], g101 = main.zetaZeroValues[100];
        double mid = (g100 + g101) * .5;
        double gram;
        {
            double target = Math.Ceiling(ZetaMath.RiemannSiegelTheta(g100) / Math.PI) * Math.PI;
            double lo = g100, hi = g101 + 5;
            for (int i = 0; i < 60; i++) { double m = (lo + hi) * .5; if (ZetaMath.RiemannSiegelTheta(m) < target) lo = m; else hi = m; }
            gram = (lo + hi) * .5;
        }

        Vector3 far = c + new Vector3(600f, 0f, 0f);
        Vector3 panelA = far + new Vector3(0f, 40f, 0f);
        Vector3 panelB = far;
        Vector3 panelC = far + new Vector3(0f, -40f, 0f);
        sacks.ExpChainPanel(g100, 400, panelA, 10f, 15f, $"at the 100th zero, t = {g100:0.000}: the tail winds onto the origin", 3.4f);
        sacks.ExpChainPanel(mid, 400, panelB, 10f, 15f, $"between zeros, t = {mid:0.000}: the tail winds onto ζ, off the origin", 3.4f);
        sacks.ExpChainPanel(gram, 400, panelC, 10f, 15f, $"at the next Gram point, t = {gram:0.000}: ζ is real", 3.4f);
        yield return Shot("E1-chain-anatomy-three", panelB + back * 118f, panelB, Vector3.up, 55f);
        yield return Shot("E2-chain-at-zero", panelA + back * 46f, panelA, Vector3.up, 50f);
        yield return Shot("E3-chain-between-zeros", panelB + back * 46f, panelB, Vector3.up, 50f);
        yield return Shot("E4-chain-at-gram", panelC + back * 46f, panelC, Vector3.up, 50f);
        sacks.ClearExperiments();
        }

        if (Wanted('F')) {
        sacks.ShowRoutes(false);
        // F. one zero's crests: the first zero (few crests, rings inside), the 10th, the 100th (many crests, arms)
        sacks.ExpCrestComb(0, 3, 1.5f, new Color(1f, .5f, .2f), 2.8f);
        yield return Shot("F1-crests-zero1-core", c + back * 40f, c, Vector3.up, 50f);
        sacks.ClearExperiments();
        sacks.ExpCrestComb(9, 6, 1.2f, new Color(1f, .5f, .2f), 7f);
        yield return Shot("F2-crests-zero10-centre", c + back * Centre, c, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpCrestComb(99, 12, 1.2f, new Color(1f, .5f, .2f), 14f);
        yield return Shot("F3-crests-zero100-full", c + back * Full, c, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpCrestComb(99, 12, 1.2f, new Color(1f, .5f, .2f), 7f);
        yield return Shot("F4-crests-zero100-centre", c + back * Centre, c, Vector3.up, 55f);
        sacks.ClearExperiments();
        }

        if (Wanted('G')) {
        sacks.ShowRoutes(false);
        // G. the truncated explicit formula on the number field: single discs on the saddle, then the stack flat
        sacks.ShowBackdrop(false);
        foreach (int K in new[] { 1, 3, 10, 100, 1000, 10000 })
        {
            sacks.ExpHeatStack(new[] { K }, SacksSpiral.Count, 0f, 14f, false);
            yield return Shot($"G-heat-{K:D5}-full", c + back * Full, c, Vector3.up, 55f);
            sacks.ClearExperiments();
        }

        sacks.ExpHeatStack(new[] { 1, 3, 10, 30, 100, 300, 1000, 3000, 10000 }, SacksSpiral.Count, 9f, 7f, true);
        yield return Shot("G-heat-stack-saddle-oblique", c + new Vector3(-90f, -150f, -120f), c + back * 36f, back, 55f);
        sacks.ClearExperiments();
        yield return Flatten(true);
        sacks.ShowBackdrop(false);
        sacks.ExpHeatStack(new[] { 1, 3, 10, 30, 100, 300, 1000, 3000, 10000 }, SacksSpiral.Count, 9f, 7f, true);
        yield return Shot("G-heat-stack-flat-side", c + new Vector3(0f, -200f, -80f), c + back * 36f, back, 55f);
        yield return Shot("G-heat-stack-flat-oblique", c + new Vector3(-120f, -150f, -100f), c + back * 36f, back, 55f);
        sacks.ClearExperiments();
        yield return Flatten(false);
        }

        if (Wanted('H')) {
        // H. cross sections of the saddle against the trail and the product curves
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(true);
        float half = sacks.Extent + 6f;
        // H1: the plane y = 0 holds the x axis: the ridge. Every 1:1 product curve climbs it; the trail pierces it at the Gram points
        sacks.ExpSectionPlane('y', 0f, half, new Color(1f, .45f, .3f));
        sacks.ExpSectionCurve('y', 0f, new Color(1f, .45f, .3f));
        sacks.ExpSectionNumbers('y', 0f, 1.2f, new Color(1f, .45f, .3f), SacksSpiral.Count, false, 0f);
        sacks.ExpProductFamily(1, new[] { 0, 1, 2, 3, 4, 5, 6 }, SacksSpiral.Count, new Color(1f, .6f, .4f), .05f, 0f);
        sacks.ExpTrail(140f, true, false, 4f);
        yield return Shot("H1-ridge-plane-side", c + new Vector3(0f, -200f, 15f), c + new Vector3(0f, 0f, 15f), back, 55f);
        yield return Shot("H2-ridge-plane-oblique", c + new Vector3(-150f, -130f, -70f), c + back * 10f, back, 55f);
        yield return Shot("H3-ridge-plane-trail", c + new Vector3(0f, -70f, -14f), c + back * 14f, back, 50f);
        sacks.ClearExperiments();
        // H2: the plane x = 0 holds the y axis: the valley. The 1:4 products with odd gap run down it; the trail pierces it at the half-Gram points
        sacks.ExpSectionPlane('x', 0f, half, new Color(.35f, .65f, 1f));
        sacks.ExpSectionCurve('x', 0f, new Color(.35f, .65f, 1f));
        sacks.ExpSectionNumbers('x', 0f, 1.2f, new Color(.35f, .65f, 1f), SacksSpiral.Count, false, 0f);
        sacks.ExpProductFamily(2, new[] { 1, 3, 5, 7, 9, 11 }, SacksSpiral.Count, new Color(.45f, .75f, 1f), .05f, 0f);
        sacks.ExpTrail(140f, false, true, 4f);
        yield return Shot("H4-valley-plane-side", c + new Vector3(-200f, 0f, 15f), c + new Vector3(0f, 0f, 15f), back, 55f);
        yield return Shot("H5-valley-plane-oblique", c + new Vector3(-130f, -150f, -70f), c + back * 10f, back, 55f);
        yield return Shot("H6-valley-plane-trail", c + new Vector3(-70f, 0f, -14f), c + back * 14f, back, 50f);
        sacks.ClearExperiments();
        // H3: the zero level z = 0: the diagonals; the 1:16 products with odd gap hug them
        sacks.ExpSectionCurve('z', 0f, new Color(.85f, .85f, .95f));
        sacks.ExpSectionNumbers('z', 0f, .6f, new Color(.85f, .85f, .95f), SacksSpiral.Count, false, 0f);
        sacks.ExpProductFamily(4, new[] { 1, 3, 5, 7 }, SacksSpiral.Count, new Color(.9f, .9f, 1f), .05f, 14f);
        sacks.ExpProductFamily(1, new[] { 0, 1, 2, 3 }, SacksSpiral.Count, new Color(1f, .6f, .4f), .04f, 0f);
        sacks.ExpProductFamily(2, new[] { 1, 3 }, SacksSpiral.Count, new Color(.45f, .75f, 1f), .04f, 0f);
        yield return Shot("H7-zero-level-full", c + back * Full, c, Vector3.up, 55f);
        sacks.ClearExperiments();
        // H3b: level planes z = h at bend 1: hyperbolas; the valley planes also cut the trail
        foreach (float hgt in new[] { 30f, 15f, -15f, -30f })
        {
            Color lc = hgt > 0 ? new Color(1f, .45f, .3f) : new Color(.35f, .65f, 1f);
            sacks.ExpSectionPlane('z', hgt, half, lc, 16f);
            sacks.ExpSectionCurve('z', hgt, lc, .06f);
            sacks.ExpSectionNumbers('z', hgt, 1.0f, lc, SacksSpiral.Count, false, 0f);
        }

        sacks.ExpTrail(140f, false, false, 0f);
        yield return Shot("H8-level-planes-oblique", c + new Vector3(-150f, -150f, -90f), c + back * 4f, back, 55f);
        yield return Shot("H9-level-planes-side", c + new Vector3(0f, -210f, -10f), c + back * 4f, back, 55f);
        sacks.ClearExperiments();
        sacks.ExpSectionCurve('z', 15f, new Color(1f, .45f, .3f), .06f);
        sacks.ExpSectionCurve('z', -15f, new Color(.35f, .65f, 1f), .06f);
        sacks.ExpSectionCurve('z', 30f, new Color(1f, .45f, .3f), .06f);
        sacks.ExpSectionCurve('z', -30f, new Color(.35f, .65f, 1f), .06f);
        foreach (float hgt in new[] { 30f, 15f, -15f, -30f }) sacks.ExpSectionNumbers('z', hgt, 1.0f, hgt > 0 ? new Color(1f, .45f, .3f) : new Color(.35f, .65f, 1f), SacksSpiral.Count, false, 0f);
        yield return Shot("H10-level-contours-full", c + back * Full, c, Vector3.up, 55f);
        sacks.ClearExperiments();
        // H3c: the bend as the probe: the plane z = −15 at bend ¼, ½, 1, 2 sweeps inward through the valley family
        foreach (float b in new[] { .25f, .5f, 1f, 2f })
        {
            sacks.ExpSetBend(b);
            sacks.ExpNumberField(SacksSpiral.Count);
            sacks.ExpSectionCurve('z', -15f, new Color(.35f, .65f, 1f), .08f);
            sacks.ExpSectionCurve('z', 15f, new Color(1f, .45f, .3f), .08f);
            sacks.ExpSectionNumbers('z', -15f, 1.0f, new Color(.35f, .65f, 1f), SacksSpiral.Count, false, 0f);
            sacks.ExpSectionNumbers('z', 15f, 1.0f, new Color(1f, .45f, .3f), SacksSpiral.Count, false, 0f);
            sacks.ExpProductFamily(2, new[] { 1, 3, 5, 7 }, SacksSpiral.Count, new Color(.45f, .75f, 1f), .05f, 0f);
            sacks.ExpProductFamily(1, new[] { 0, 1, 2, 3, 4 }, SacksSpiral.Count, new Color(1f, .6f, .4f), .05f, 0f);
            yield return Shot($"H11-bend-{b:0.00}-planes-15", c + back * Full, c, Vector3.up, 55f);
            sacks.ClearExperiments();
        }

        sacks.ExpSetBend(1f);
        sacks.ExpNumberField(SacksSpiral.Count);
        // H4: the unrolled chart (far panel)
        sacks.ShowBackdrop(false);
        Vector3 chart = c + new Vector3(900f, 0f, 0f);
        sacks.ExpUnrolledChart(chart, 96f, 60f, SacksSpiral.Count, 0, new[] { 1, 2, 3, 4, 5, 6 }, 4f);
        yield return Shot("H12-unrolled-chart", chart + back * 115f, chart, Vector3.up, 55f);
        yield return Shot("H13-unrolled-chart-left", chart + new Vector3(-28f, 0f, 0f) + back * 60f, chart + new Vector3(-28f, 0f, 0f), Vector3.up, 55f);
        sacks.ClearExperiments();
        // H5: the Möbius field and Mertens' ribbon
        sacks.ExpMobiusField(SacksSpiral.Count, 14f);
        yield return Shot("H14-mobius-full", c + back * Full, c, Vector3.up, 55f);
        yield return Shot("H15-mobius-centre", c + back * Centre, c, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ShowBackdrop(true);
        sacks.ExpMertensRibbon(SacksSpiral.Count, 4f, 14f);
        yield return Shot("H16-mertens-oblique", c + new Vector3(-40f, -70f, -45f), c + back * 2f, back, 60f);
        yield return Shot("H17-mertens-full", c + back * Full, c, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ShowBackdrop(false);
        Vector3 mpanel = c + new Vector3(1200f, 0f, 0f);
        sacks.ExpMertensPanel(mpanel, 100f, 40f, SacksSpiral.Count, 3.6f);
        yield return Shot("H18-mertens-panel", mpanel + back * 112f, mpanel, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ShowBackdrop(true);
        }

        UnityEngine.Rendering.AsyncGPUReadback.WaitAllRequests();
        yield return new WaitForSeconds(1f);
        director.Recorder.EndOffscreen();
        Debug.Log("Experiment snapshots → " + outDir);
    }
}
