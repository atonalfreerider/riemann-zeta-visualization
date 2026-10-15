using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        // ZETA_EXP_WIDE=1: full horizontal frames (1920 × 1080) for the sections drawn for landscape (AA–AE, BA–BD, CA–CE, DA–DD)
        bool wideFrames = Environment.GetEnvironmentVariable("ZETA_EXP_WIDE") == "1";
        director.Recorder.Width = wideFrames ? 1920 : 1080;
        director.Recorder.Height = wideFrames ? 1080 : 1920;
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

        IEnumerator ShotOrtho(string name, Vector3 pos, Vector3 look, Vector3 up, float size)
        {
            cam.orthographic = true;
            cam.orthographicSize = size;
            cam.transform.position = pos;
            cam.transform.rotation = Quaternion.LookRotation(look - pos, up);
            yield return null;
            yield return null;
            director.Recorder.Snapshot(name + ".png");
            yield return null;
            yield return null;
            cam.orthographic = false;
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
        // H1: the plane y = 0 holds the x axis, where the saddle falls away. Every 1:1 product curve runs down it; the trail pierces it at the Gram points
        sacks.ExpSectionPlane('y', 0f, half, new Color(1f, .45f, .3f));
        sacks.ExpSectionCurve('y', 0f, new Color(1f, .45f, .3f));
        sacks.ExpSectionNumbers('y', 0f, 1.2f, new Color(1f, .45f, .3f), SacksSpiral.Count, false, 0f);
        sacks.ExpProductFamily(1, new[] { 0, 1, 2, 3, 4, 5, 6 }, SacksSpiral.Count, new Color(1f, .6f, .4f), .05f, 0f);
        sacks.ExpTrail(140f, true, false, 4f);
        yield return Shot("H1-x-axis-plane-side", c + new Vector3(0f, -200f, 15f), c + new Vector3(0f, 0f, 15f), back, 55f);
        yield return Shot("H2-x-axis-plane-oblique", c + new Vector3(-150f, -130f, -70f), c + back * 10f, back, 55f);
        yield return Shot("H3-x-axis-plane-trail", c + new Vector3(0f, -70f, -14f), c + back * 14f, back, 50f);
        sacks.ClearExperiments();
        // H2: the plane x = 0 holds the y axis, where the saddle rises. The 1:4 products with odd gap climb it; the trail pierces it at the half-Gram points
        sacks.ExpSectionPlane('x', 0f, half, new Color(.35f, .65f, 1f));
        sacks.ExpSectionCurve('x', 0f, new Color(.35f, .65f, 1f));
        sacks.ExpSectionNumbers('x', 0f, 1.2f, new Color(.35f, .65f, 1f), SacksSpiral.Count, false, 0f);
        sacks.ExpProductFamily(2, new[] { 1, 3, 5, 7, 9, 11 }, SacksSpiral.Count, new Color(.45f, .75f, 1f), .05f, 0f);
        sacks.ExpTrail(140f, false, true, 4f);
        yield return Shot("H4-y-axis-plane-side", c + new Vector3(-200f, 0f, 15f), c + new Vector3(0f, 0f, 15f), back, 55f);
        yield return Shot("H5-y-axis-plane-oblique", c + new Vector3(-130f, -150f, -70f), c + back * 10f, back, 55f);
        yield return Shot("H6-y-axis-plane-trail", c + new Vector3(-70f, 0f, -14f), c + back * 14f, back, 50f);
        sacks.ClearExperiments();
        // H3: the zero level z = 0: the diagonals; the 1:16 products with odd gap hug them
        sacks.ExpSectionCurve('z', 0f, new Color(.85f, .85f, .95f));
        sacks.ExpSectionNumbers('z', 0f, .6f, new Color(.85f, .85f, .95f), SacksSpiral.Count, false, 0f);
        sacks.ExpProductFamily(4, new[] { 1, 3, 5, 7 }, SacksSpiral.Count, new Color(.9f, .9f, 1f), .05f, 14f);
        sacks.ExpProductFamily(1, new[] { 0, 1, 2, 3 }, SacksSpiral.Count, new Color(1f, .6f, .4f), .04f, 0f);
        sacks.ExpProductFamily(2, new[] { 1, 3 }, SacksSpiral.Count, new Color(.45f, .75f, 1f), .04f, 0f);
        yield return Shot("H7-zero-level-full", c + back * Full, c, Vector3.up, 55f);
        sacks.ClearExperiments();
        // H3b: level planes z = h at bend 1: hyperbolas; the planes above the centre also cut the trail
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
        // H3c: the bend as the probe: the plane z = −15 (above the centre) at bend ¼, ½, 1, 2 sweeps inward through the one-to-four family
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

        if (Wanted('I')) {
        // I. vortex eyes at every zero, traced through the zero planes
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        double[] zs = main.zetaZeroValues;
        List<SacksSpiral.Vortex> all = new();
        for (int j = 0; j < zs.Length; j++) all.AddRange(SacksSpiral.DetectVortices(zs[j], j + 1, 12, Meta.Numerics.Complex.Zero));
        using (StreamWriter w = new(Path.Combine(outDir, "vortices.csv")))
        {
            w.WriteLine("j,t,k,knot,eye_re,eye_im,mirror_re,mirror_im,links,curl");
            foreach (SacksSpiral.Vortex v in all) w.WriteLine($"{v.Zero},{v.T:R},{v.K},{v.Knot},{v.Eye.Re:R},{v.Eye.Im:R},{v.Mirror.Re:R},{v.Mirror.Im:R},{v.Links},{v.Curl}");
        }

        Debug.Log($"vortices detected: {all.Count} over {zs.Length} zeros");
        Vector3 tower = c + new Vector3(1500f, 0f, 0f);
        // I1-I3: zeros 1 … 40 at their true heights, with the planes and the trail
        {
            double t0 = zs[0];
            float H = (float)(zs[39] - t0) * .4f;
            sacks.ExpVortexTower(all, 1, 40, tower, 4f, t => (float)(t - t0) * .4f, false, true, true, true, 6, 2.2f);
            yield return Shot("I1-vortex-tower-1-40-oblique", tower + new Vector3(-34f, -52f, -H * .5f), tower + back * (H * .5f), back, 55f);
            yield return Shot("I2-vortex-tower-1-40-side", tower + new Vector3(0f, -64f, -H * .5f), tower + back * (H * .5f), back, 55f);
            yield return Shot("I3-vortex-tower-1-40-top", tower + back * (H + 40f), tower + back * H, Vector3.up, 45f);
            sacks.ClearExperiments();
        }

        // I4-I5: zeros 1000 … 1040, and I6: zeros 5000 … 5040, at their true heights
        {
            double t0 = zs[999];
            float H = (float)(zs[1039] - t0) * 1.0f;
            sacks.ExpVortexTower(all, 1000, 1040, tower, 4f, t => (float)(t - t0) * 1.0f, false, true, true, true, 8, 2.2f);
            yield return Shot("I4-vortex-tower-1000-1040-oblique", tower + new Vector3(-34f, -52f, -H * .5f), tower + back * (H * .5f), back, 55f);
            yield return Shot("I5-vortex-tower-1000-1040-side", tower + new Vector3(0f, -64f, -H * .5f), tower + back * (H * .5f), back, 55f);
            sacks.ClearExperiments();
            double t5 = zs[4999];
            float H5 = (float)(zs[5039] - t5) * 1.6f;
            sacks.ExpVortexTower(all, 5000, 5040, tower, 4f, t => (float)(t - t5) * 1.6f, false, true, true, true, 12, 2.2f);
            yield return Shot("I6-vortex-tower-5000-5040-oblique", tower + new Vector3(-34f, -52f, -H5 * .5f), tower + back * (H5 * .5f), back, 55f);
            sacks.ClearExperiments();
        }

        // I7-I9: all ten thousand zeros looked at down the axis (orthographic), plain and with the Gram phase taken out
        {
            System.Func<double, float> byIndex = t =>
            {
                int lo = 0, hi = zs.Length - 1;
                while (hi - lo > 1) { int m = (lo + hi) / 2; if (zs[m] <= t) lo = m; else hi = m; }
                double f = (t - zs[lo]) / System.Math.Max(1e-9, zs[hi] - zs[lo]);
                return (float)((lo + f) * .03);
            };
            float H = 10000 * .03f;
            sacks.ExpVortexTower(all, 1, 10000, tower, 4f, byIndex, false, false, false, false, 12, 0f, 1, .007f, false, Factory.NoGlow);
            yield return ShotOrtho("I7-vortex-tower-all-down-axis", tower + back * (H + 50f), tower, Vector3.up, 10f);
            sacks.ClearExperiments();
            sacks.ExpVortexTower(all, 1, 10000, tower, 4f, byIndex, true, false, false, false, 12, 0f, 1, .007f, false, Factory.NoGlow);
            yield return ShotOrtho("I8-vortex-tower-all-derotated-down-axis", tower + new Vector3(-4f, 0f, 0f) + back * (H + 50f), tower + new Vector3(-4f, 0f, 0f), Vector3.up, 10f);
            sacks.ClearExperiments();
            sacks.ExpVortexTower(all, 1, 10000, tower, 4f, byIndex, true, false, false, false, 4, 0f, 1, .007f, false, Factory.NoGlow);
            yield return ShotOrtho("I9-vortex-tower-all-derotated-k1-4-down-axis", tower + new Vector3(-4f, 0f, 0f) + back * (H + 50f), tower + new Vector3(-4f, 0f, 0f), Vector3.up, 8f);
            sacks.ClearExperiments();
            sacks.ExpVortexTower(all, 2000, 2200, tower, 4f, t => (float)(t - zs[1999]) * 1.0f, true, false, true, false, 8, 2.2f);
            float H2 = (float)(zs[2199] - zs[1999]);
            yield return Shot("I10-vortex-tower-2000-2200-derotated-side", tower + new Vector3(0f, -200f, -H2 * .5f), tower + back * (H2 * .5f), back, 55f);
            sacks.ClearExperiments();
        }

        // I11-I13: the chain at one zero with its vortices ringed and the mirror points crossed
        foreach ((int j, string name) in new[] { (100, "I11-chain-vortices-zero-100"), (1000, "I12-chain-vortices-zero-1000"), (10000, "I13-chain-vortices-zero-10000") })
        {
            Vector3 panel = c + new Vector3(2000f, 0f, 0f);
            sacks.ExpChainVortexPanel(zs[j - 1], j, panel, 11f, 16f, 12, $"zero #{j}, t = {zs[j - 1]:0.000}", 1.4f);
            yield return Shot(name, panel + back * 48f, panel, Vector3.up, 50f);
            sacks.ClearExperiments();
        }

        sacks.ShowBackdrop(true);
        }

        // ---------------------------------------------------------------------------------------------
        // second round: sections J–O (SacksSections.cs)
        // ---------------------------------------------------------------------------------------------
        float L = sacks.Extent + 4f;
        int N = SacksSpiral.Count;
        if (Wanted('J')) {
        // J. vertical planes through the centre, rotated: parabolas of curvature cos 2α, straight at 45°
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(true);
        sacks.ExpSetSurface(2, 0f);
        foreach (float a in new[] { 0f, 22.5f, 45f, 67.5f, 90f, 112.5f, 135f, 157.5f }) sacks.ExpVSection(a, 0f, -L, L, L, SacksSpiral.FanColor(a), .1f);
        yield return Shot("J1-fan-of-parabolas-oblique", c + new Vector3(-40f, -70f, -45f), c + back * 2f, back, 60f);
        yield return Shot("J2-fan-of-parabolas-side", c + new Vector3(-70f, -150f, -12f), c + back * 4f, back, 50f);
        sacks.ClearExperiments();
        // J3: the plane at 90°: it holds the one-to-four families 4k² + k + C (up) and 4k² + 3k + C (down)
        sacks.ExpVPlane(90f, 0f, -L, L, -75f, 15f, SacksSpiral.FanColor(90f), 12f);
        sacks.ExpVSection(90f, 0f, -L, L, L, SacksSpiral.FanColor(90f), .1f);
        Vector2Int j90 = sacks.ExpVNumbers(90f, 0f, -L, L, 1.0f, N, new Color(.6f, .8f, 1f));
        sacks.ExpQuarterFamilies(new long[] { 0, 1, 2, 3 }, N);
        yield return Shot("J3-plane-90-quarter-families", c + new Vector3(-150f, -60f, -60f), c + back * 10f, back, 55f);
        Debug.Log($"J: plane 90°: {j90.x} numbers within 1 unit, {j90.y} primes");
        sacks.ClearExperiments();
        // J4: the plane at 45°: its section is a straight line of the saddle; the 1:16 families run beside it
        sacks.ExpVPlane(45f, 0f, -L, L, -40f, 40f, SacksSpiral.FanColor(45f), 12f);
        sacks.ExpVSection(45f, 0f, -L, L, L, Color.white, .12f);
        Vector2Int j45 = sacks.ExpVNumbers(45f, 0f, -L, L, 1.0f, N, Color.white);
        sacks.ExpDiagonalFamilies(new long[] { 0, 1, 2, 3 }, N);
        yield return Shot("J4-plane-45-straight-section", c + new Vector3(60f, -150f, -40f), c + back * 2f, back, 55f);
        Debug.Log($"J: plane 45°: {j45.x} numbers within 1 unit, {j45.y} primes");
        sacks.ClearExperiments();
        // J5: eight strips seen from above, with their counts
        string jText = "";
        foreach (float a in new[] { 0f, 30f, 45f, 60f, 90f, 120f, 135f, 150f })
        {
            Vector2Int cnt = sacks.ExpVNumbers(a, 0f, 0f, L, 1.0f, N, SacksSpiral.FanColor(a));
            sacks.ExpVSection(a, 0f, 0f, L, L, SacksSpiral.FanColor(a), .07f);
            sacks.ExpLabelAt(new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * (L + 6f), $"{a}°   {cnt.y} of {cnt.x} prime", SacksSpiral.FanColor(a), 9.5f);
            jText += $"{a}°: {cnt.x} numbers, {cnt.y} primes;  ";
        }

        Debug.Log("J strips: " + jText);
        yield return Shot("J5-eight-strips-top", c + new Vector3(10f, 25f, 0f) + back * 215f, c + new Vector3(10f, 25f, 0f), Vector3.up, 55f);
        sacks.ClearExperiments();
        }

        if (Wanted('K')) {
        // K. off-axis planes parallel to the x axis: one quadratic each, ordered by discriminant
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(true);
        sacks.ExpSetSurface(2, 0f);
        int[] sq = { -9, -4, -2, -1, 0, 1, 2, 4, 7 };
        int[] pr = { -6, -2, -1, 0, 1, 2, 3, 5 };
        sacks.ExpComb(sq, pr, -20f, 75f, true, true, true, 0f, N);
        yield return Shot("K1-comb-oblique", c + new Vector3(-40f, -95f, -60f), c + back * 2f, back, 60f);
        yield return Shot("K2-comb-end-on", c + new Vector3(200f, 0f, -30f), c + back * 4f, back, 50f);
        sacks.ClearExperiments();
        sacks.ExpComb(sq, pr, -20f, 75f, false, false, true, 9.5f, N);
        yield return Shot("K3-comb-top-squares-side", c + new Vector3(42f, 0f, 0f) + back * 205f, c + new Vector3(42f, 0f, 0f), Vector3.up, 55f);
        yield return Shot("K3-comb-top-pronic-side", c + new Vector3(-42f, 0f, 0f) + back * 205f, c + new Vector3(-42f, 0f, 0f), Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ShowBackdrop(false);
        Vector3 sky = c + new Vector3(2600f, 0f, 0f);
        float corr0 = sacks.ExpSkyline(0, -30, 60, 3000, sky, 2.1f, 42f, 12.5f);
        yield return Shot("K4-skyline-squares-side", sky + new Vector3(46f, 22f, 0f) + back * 250f, sky + new Vector3(46f, 22f, 0f), Vector3.up, 55f);
        sacks.ClearExperiments();
        float corr1 = sacks.ExpSkyline(1, -30, 60, 3000, sky, 2.1f, 42f, 12.5f);
        yield return Shot("K5-skyline-pronic-side", sky + new Vector3(46f, 22f, 0f) + back * 250f, sky + new Vector3(46f, 22f, 0f), Vector3.up, 55f);
        Debug.Log($"K: correlation with Hardy–Littlewood: squares side {corr0:0.0000}, pronic side {corr1:0.0000}");
        sacks.ClearExperiments();
        sacks.ShowBackdrop(true);
        }

        if (Wanted('L')) {
        // L. the mirror y → −y: k² − m² (cancellation) ↔ k² + m² (un-cancellation); Euler's criterion and Fermat's method
        sacks.ShowRoutes(false);
        yield return Flatten(true);
        sacks.ShowBackdrop(false);
        sacks.ExpSetSurface(2, 0f);
        sacks.ExpGaussianField(N);
        sacks.ExpMirrorComb(3, N, 9.5f, false);
        yield return Shot("L1-mirror-comb-full", c + back * 205f, c, Vector3.up, 55f);
        yield return Shot("L2-mirror-comb-right", c + new Vector3(30f, 0f, 0f) + back * 110f, c + new Vector3(30f, 0f, 0f), Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpGaussianField(N);
        string e1105 = sacks.ExpEulerShowcase(1105, 8.5f, new Vector3(0f, 14f, -1f));
        string e221 = sacks.ExpEulerShowcase(221, 8.5f, new Vector3(-6f, -16f, -1f));
        string e1601 = sacks.ExpEulerShowcase(1601, 8.5f, new Vector3(-22f, -9f, -1f));
        Debug.Log("L: " + e1105.Replace('\n', ' ') + " | " + e221.Replace('\n', ' ') + " | " + e1601.Replace('\n', ' '));
        yield return Shot("L3-euler-two-pairs-factor", c + back * 205f, c, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpGaussianField(N);
        string f1 = sacks.ExpFermatShowcase(1591, 7.5f, new Vector3(-20f, 7f, -1f));
        string f2 = sacks.ExpFermatShowcase(1003, 7.5f, new Vector3(0f, -10f, -1f));
        string f3 = sacks.ExpFermatShowcase(1501, 7.5f, new Vector3(0f, 10f, -1f));
        Debug.Log("L: " + f1 + " | " + f2 + " | " + f3);
        yield return Shot("L4-fermat-curves", c + back * 205f, c, Vector3.up, 55f);
        sacks.ClearExperiments();
        yield return Flatten(false);
        sacks.ShowBackdrop(true);
        }

        if (Wanted('M')) {
        // M. the atlas: where prime curves, cancellation curves and the lines between them meet
        sacks.ShowRoutes(false);
        yield return Flatten(true);
        sacks.ShowBackdrop(true);
        sacks.ExpSetSurface(2, 0f);
        SacksSpiral.AtlasCurve[] curves =
        {
            new(0, -1, 3), new(0, -4, 4), new(0, -9, 5), new(0, -16, 6),          // cancellation, squares side: (k − j)(k + j), u ≥ 2
            new(1, 0, 2), new(1, -2, 3), new(1, -6, 4), new(1, -12, 5),           // cancellation, pronic side: (k − j)(k + j + 1), u ≥ 2
            new(0, 1, 1), new(0, 2, 0), new(0, 4, 0), new(0, 6, 0),               // prime curves (Δ < 0), squares side
            new(1, 1, 0), new(1, 2, 0), new(1, 3, 0), new(1, 5, 0), new(1, 11, 0), new(1, 17, 0), new(1, 41, 0),
            new(0, -2, 2), new(0, -3, 2), new(0, -5, 3), new(1, -1, 1), new(1, -3, 2), new(1, -5, 2) // Δ > 0, not a square
        };
        string atlas = sacks.ExpAtlas(curves, N, 3.4f, new long[] { 1601, 197, 47, 173, 461, 131, 71 });
        Debug.Log("M: " + atlas);
        yield return Shot("M1-atlas-full", c + back * 205f, c, Vector3.up, 55f);
        yield return Shot("M2-atlas-centre", c + back * 70f, c, Vector3.up, 55f);
        Vector3 rimFocus = c + new Vector3(-30f, 25f, 0f);
        yield return Shot("M3-atlas-upper-left", rimFocus + back * 90f, rimFocus, Vector3.up, 55f);
        sacks.ClearExperiments();
        yield return Flatten(false);
        }

        if (Wanted('N')) {
        // N. tilted planes: hyperbolas whose centre slides round a circle as the tilt turns; the tangent plane's two straight lines
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(true);
        sacks.ExpSetSurface(2, 0f);
        float tilt = .6f;
        Vector2 n1 = sacks.ExpTiltedPlane(tilt, 0f, 0f, L, new Color(1f, .7f, .3f), true, true, L);
        yield return Shot("N1-tilted-plane-hyperbola", c + new Vector3(-60f, -110f, -70f), c + back * 4f, back, 55f);
        sacks.ClearExperiments();
        for (int b = 0; b < 6; b++)
        {
            float beta = b * 60f * Mathf.Deg2Rad;
            Color col = Color.HSVToRGB(b / 6f, .7f, 1f);
            Vector2 ctr = sacks.ExpTiltedPlane(tilt * Mathf.Cos(beta), tilt * Mathf.Sin(beta), 0f, 0f, col, false, true, L);
            sacks.ExpMarker(ctr, col, $"centre {b * 60}°");
        }

        sacks.ExpCentreCircle(tilt);
        yield return Shot("N2-six-tilts-top", c + back * 205f, c, Vector3.up, 55f);
        yield return Shot("N3-six-tilts-oblique", c + new Vector3(-40f, -70f, -45f), c + back * 2f, back, 60f);
        sacks.ClearExperiments();
        Vector2Int t1601 = sacks.ExpTangentPlane(1601, 30f, new Color(.6f, .9f, 1f), .9f, N);
        Debug.Log($"N: tangent plane at 1601: {t1601.x} numbers within 0.9 of its two straight lines, {t1601.y} primes");
        Vector3 at1601 = sacks.ExpNumberPosition(1601);
        yield return Shot("N4-tangent-plane-1601", at1601 + new Vector3(-30f, -70f, -50f), at1601, back, 55f);
        yield return Shot("N5-tangent-plane-1601-top", c + back * 205f, c, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpDiagonalFamilies(new long[] { -1, 0, 1, 2, 3 }, N * 4);
        foreach (long C in new long[] { -1, 0, 1, 2, 3 }) sacks.ExpRuling(true, -Mathf.PI * 1.45f * (1f - 64f * C) / 64f, sacks.Extent * 2f, new Color(1f, 1f, 1f, .6f), .04f);
        yield return Shot("N6-diagonal-families-on-straight-lines", c + new Vector3(70f, 70f, 0f) + back * 160f, c + new Vector3(70f, 70f, 0f), Vector3.up, 55f);
        sacks.ClearExperiments();
        }

        if (Wanted('O')) {
        // O. twist and fold: z = K r² cos(folds·θ − twist·r); on the spiral the phase is (2π·folds − twist·R)·√n
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        sacks.ExpSetBend(.5f);
        foreach ((int folds, float twistR, string name, float[] levels) in new[]
        {
            (2, 2f * Mathf.PI, "O1-twist-half-squares-down-pronics-up", new float[0]),
            (2, 4f * Mathf.PI, "O2-twist-full-spiral-on-one-line", new float[0]),
            (2, 4f * Mathf.PI * (1f - Mathf.Sqrt(2f)), "O3-twist-voronoi-2-ridge-2t2", new float[0]),
            (4, 0f, "O4-four-fold-saddle", new[] { 12f }),
            (3, 0f, "O5-three-fold-saddle", new float[0]),
        })
        {
            sacks.ExpSetSurface(folds, twistR);
            sacks.ExpSurfaceWire(L - 2f, new Color(.32f, .36f, .5f));
            sacks.ExpSurfaceField(N);
            sacks.ExpFamiliesOnSurface(folds, twistR, N);
            foreach (float h in levels) sacks.ExpLevelArms(h, h > 0 ? new Color(1f, .45f, .3f) : new Color(.35f, .65f, 1f), L - 2f);
            yield return Shot(name + "-oblique", c + new Vector3(-50f, -95f, -70f), c + back * 2f, back, 55f);
            yield return Shot(name + "-side", c + new Vector3(0f, -170f, -10f), c + back * 4f, back, 50f);
            sacks.ClearExperiments();
        }

        sacks.ExpSetSurface(2, 0f);
        sacks.ExpSetBend(1f);
        sacks.ExpNumberField(SacksSpiral.Count);
        sacks.ShowBackdrop(true);
        }

        if (Wanted('P')) {
        // P. two lattice-point problems: wobbles as charts (one faint line per turn of the spiral), first waves on their surfaces
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        sacks.ExpSetSurface(2, 0f);
        Vector3 chart = c + new Vector3(SacksSpiral.LatticeChartOffset, 0f, 0f);
        foreach (bool circle in new[] { false, true })
        {
            float corr = sacks.ExpLatticeWaveCorrelation(circle, 3000), corr1 = sacks.ExpLatticeWaveCorrelation(circle, 1);
            Debug.Log($"P: {(circle ? "circle" : "divisor")} problem: 3000 waves correlate {corr:0.000} with the wobble, the first wave alone {corr1:0.000}");
        }

        sacks.ExpLatticeChart(false, 3000, chart + new Vector3(0f, 22f, 0f), 54f, 26f, 10f, true, true, true);
        sacks.ExpLatticeChart(true, 3000, chart + new Vector3(0f, -22f, 0f), 54f, 26f, 10f, true, true, true);
        yield return Shot("P1-two-wobbles-charts", chart + back * 115f, chart, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpLatticeChart(false, 1, chart, 54f, 26f, 10f, true, false, true);
        yield return Shot("P2-divisor-first-wave-chart", chart + back * 100f, chart, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpLatticeChart(true, 1, chart, 54f, 26f, 10f, true, false, true);
        yield return Shot("P3-circle-first-wave-chart", chart + back * 100f, chart, Vector3.up, 55f);
        sacks.ClearExperiments();
        foreach ((float twistPi, bool circle, string name) in new[] { (0f, false, "P4-divisor-first-wave-on-the-saddle"), (2f, true, "P5-circle-first-wave-on-the-half-twist") })
        {
            sacks.ExpSetSurface(2, twistPi * Mathf.PI);
            sacks.ExpSurfaceWire(30f, new Color(.32f, .36f, .5f), 6f);
            sacks.ExpSurfaceField(400);
            sacks.ExpLatticeWave(circle, 1, 9f, 400f, 5f, false, false, true);
            yield return Shot(name + "-side", c + new Vector3(0f, -92f, -10f), c + back * 4f, back, 45f);
            yield return Shot(name + "-oblique", c + new Vector3(-34f, -66f, -46f), c + back * 3f, back, 50f);
            sacks.ClearExperiments();
        }

        sacks.ExpSetSurface(2, 0f);
        sacks.ShowBackdrop(true);
        }

        if (Wanted('Q')) {
        // Q. the prime ideals of Euler's ring: ζ's zeros, the L-function's zeros, and both
        sacks.ShowRoutes(false);
        yield return Flatten(true);
        sacks.ShowBackdrop(false);
        sacks.ExpSetSurface(2, 0f);
        foreach ((string which, string name) in new[] { ("zeta", "Q1-zeta-zeros-every-prime"), ("l", "Q2-l-zeros-split-up-inert-down"), ("both", "Q3-both-the-primes-of-euler-ring") })
        {
            sacks.ExpIdealField(800);
            sacks.ExpIdealNeedles(800, which, 6f);
            yield return Shot(name + "-oblique", c + new Vector3(-50f, -92f, -46f), c + back * 4f, back, 50f);
            sacks.ClearExperiments();
        }

        sacks.ExpIdealField(800);
        sacks.ExpIdealNeedles(800, "both", 6f);
        sacks.ExpEulerRoute(800);
        sacks.ExpNumberLabels(new long[] { 4, 9, 16, 25, 41, 43, 47, 53 }, 2.2f, Color.white);
        yield return Shot("Q4-centre-nothing-below-41", c + new Vector3(-14f, -34f, -22f), c + back * 3f, back, 50f);
        yield return Shot("Q5-both-with-euler-line", c + new Vector3(-50f, -92f, -46f), c + back * 4f, back, 50f);
        sacks.ClearExperiments();
        yield return Flatten(false);
        sacks.ShowBackdrop(true);
        }

        // ---------------------------------------------------------------------------------------------
        // pinching the spiral: sections R–U (SacksPinch.cs). Top views flat, oblique views on the saddle.
        // ---------------------------------------------------------------------------------------------
        if (Wanted('R')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        sacks.ExpSetSurface(2, 0f);
        sacks.ExpSetBend(0f);
        foreach ((float f, string name) in new[] { (1f, "R1a-fold-1-sacks"), (1.5f, "R1b-fold-1.5"), (2f, "R1c-fold-2") })
        {
            sacks.ExpFoldField(SacksSpiral.Count, f);
            yield return Shot(name, c + back * 215f, c, Vector3.up, 55f);
            sacks.ClearExperiments();
        }

        sacks.ExpFoldField(SacksSpiral.Count, 2f);
        sacks.ExpFoldComb(-32, 32, SacksSpiral.Count, 20f, true);
        yield return Shot("R2-fold-one-comb", c + back * 215f, c, Vector3.up, 55f);
        sacks.ClearExperiments();
        Vector3 fsky = c + new Vector3(5200f, 0f, 0f);
        sacks.ExpFoldSkyline(-164, 40, 3000, fsky, 2.1f, 46f, 18f);
        yield return Shot("R3-fold-comb-skyline", fsky + new Vector3(30f, 8f, 0f) + back * 250f, fsky + new Vector3(30f, 8f, 0f), Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpSetBend(1f);
        sacks.ExpFoldField(SacksSpiral.Count, 2f);
        sacks.ExpFoldComb(-32, 32, SacksSpiral.Count, 0f, true);
        yield return Shot("R4-fold-on-the-saddle", c + new Vector3(-50f, -95f, -70f), c + back * 2f, back, 55f);
        sacks.ClearExperiments();
        }

        if (Wanted('S')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        sacks.ExpSetSurface(2, 0f);
        // S0: mod 4 on the saddle: odd primes up the two rising lobes, even numbers down the falling ones
        sacks.ExpSetBend(1f);
        sacks.ExpResiduePinch(4, 1f, SacksSpiral.Count, true, 30f, true, .55f);
        yield return Shot("S0-mod-4-rising-lobes", c + new Vector3(-165f, -85f, -95f), c + back * 12f, back, 55f);
        sacks.ClearExperiments();
        sacks.ExpSetBend(0f);
        sacks.ExpResiduePinch(6, 1f, SacksSpiral.Count, true, 22f, true);
        yield return Shot("S1-mod-6", c + back * 230f, c, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpResiduePinch(30, .5f, SacksSpiral.Count, false, 0f, false);
        yield return Shot("S2-mod-30-halfway", c + back * 215f, c, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpResiduePinch(30, 1f, SacksSpiral.Count, true, 20f, true);
        yield return Shot("S3-mod-30", c + back * 235f, c, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpResiduePinch(210, 1f, SacksSpiral.Count, true, 0f, true);
        yield return Shot("S4-mod-210", c + back * 215f, c, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpSetBend(1f);
        sacks.ExpResiduePinch(30, 1f, SacksSpiral.Count, true, 0f, true);
        yield return Shot("S5-mod-30-on-the-saddle", c + new Vector3(-50f, -95f, -70f), c + back * 2f, back, 55f);
        sacks.ClearExperiments();
        }

        if (Wanted('T')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        sacks.ExpSetSurface(2, 0f);
        sacks.ExpSetBend(0f);
        sacks.ExpPleat(30, .5f, SacksSpiral.Count, .9f, 0f, false, false);
        yield return Shot("T1-pleat-30-halfway", c + back * 215f, c, Vector3.up, 55f);
        sacks.ClearExperiments();
        int left30 = sacks.ExpPleat(30, 1f, SacksSpiral.Count, .9f, 20f, true, true);
        Debug.Log($"T: pleat 30: {left30} composites left on the anti-cancelling slots");
        yield return Shot("T2-pleat-30", c + back * 215f, c, Vector3.up, 55f);
        Vector3 wedge = c + new Vector3(-16f, 8f, 0f);
        yield return Shot("T2b-pleat-30-close", wedge + back * 120f, wedge, Vector3.up, 55f);
        sacks.ClearExperiments();
        int left6 = sacks.ExpPleat(6, 1f, SacksSpiral.Count, .9f, 12f, true, true);
        Debug.Log($"T: pleat 6: {left6} composites left on the anti-cancelling slot");
        Vector3 band = c + new Vector3(-30f, -4f, 0f);
        yield return Shot("T3-pleat-6", band + back * 125f, band, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpSetBend(1f);
        sacks.ExpPleat(30, 1f, SacksSpiral.Count, 1.4f, 0f, true, true);
        yield return Shot("T4-pleat-30-on-the-saddle", c + new Vector3(-95f, -60f, -55f), c + back * 2f, back, 55f);
        sacks.ClearExperiments();
        Vector3 bars = c + new Vector3(5600f, 0f, 0f);
        sacks.ExpSlotBars(30, 10000000, bars, 2.6f, 36f, 16f);
        yield return Shot("T5-slot-bars-pronic-vs-squares", bars + new Vector3(0f, 4f, 0f) + back * 235f, bars + new Vector3(0f, 4f, 0f), Vector3.up, 55f);
        sacks.ClearExperiments();
        }

        if (Wanted('U')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        sacks.ExpSetBend(1f);
        Vector3 lat = c + new Vector3(6000f, 0f, 0f);
        Vector3 latMid = lat + new Vector3(6f, 12f, -18f);
        sacks.ExpResidueLattice(lat, 6f, 1601, false, 9f);
        yield return Shot("U1-residue-lattice-3-5-7", latMid + new Vector3(-30f, -30f, -66f), latMid + new Vector3(3f, 0f, 0f), Vector3.up, 55f);
        yield return Shot("U1b-residue-lattice-front", latMid + new Vector3(0f, 0f, -92f), latMid, Vector3.up, 50f);
        sacks.ClearExperiments();
        sacks.ExpResidueLattice(lat, 6f, 1601, true, 9f);
        Vector3 bothMid = lat + new Vector3(19f, 12f, -18f);
        yield return Shot("U2-residue-lattice-with-parity", bothMid + new Vector3(-30f, -42f, -96f), bothMid, Vector3.up, 55f);
        sacks.ClearExperiments();
        }

        // ---------------------------------------------------------------------------------------------
        // 144 = 12 × 12 as a basis: sections V–Y (SacksGross.cs)
        // ---------------------------------------------------------------------------------------------
        if (Wanted('V')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        sacks.ExpSetSurface(2, 0f);
        Vector3 board = c + new Vector3(7000f, 0f, 0f);
        sacks.ExpTorusBoard(board, 6f, 10000000, true, false, 32f);
        yield return Shot("V1-torus-board-diagonals", board + new Vector3(4f, 2f, 0f) + back * 180f, board + new Vector3(4f, 2f, 0f), Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpTorusBoard(board, 6f, 10000000, false, true, 32f);
        yield return Shot("V1b-torus-board-verticals", board + new Vector3(4f, 2f, 0f) + back * 180f, board + new Vector3(4f, 2f, 0f), Vector3.up, 55f);
        sacks.ClearExperiments();
        Vector3 donut = c + new Vector3(7600f, 0f, 0f);
        sacks.ExpTorus(donut, 30f, 12f, 10000, 0f);
        yield return Shot("V2-torus-3-4", donut + new Vector3(0f, -150f, -88f), donut, Vector3.back, 55f);
        sacks.ClearExperiments();
        sacks.ExpTorus(donut, 30f, 12f, 10000, 20f);
        yield return Shot("V2b-torus-top", donut + back * 185f, donut, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ShowBackdrop(true);
        sacks.ExpSetBend(0f);
        sacks.ExpNumberField(SacksSpiral.Count);
        sacks.ExpTorusClassesOnSpiral(41, SacksSpiral.Count, 30f, new[] { 41, 17, 11, 5, 1, 3 });
        yield return Shot("V3-classes-on-the-spiral", c + back * 225f, c, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpSetBend(1f);
        sacks.ExpNumberField(SacksSpiral.Count);
        sacks.ExpTorusClassesOnSpiral(41, SacksSpiral.Count, 0f, null);
        yield return Shot("V3b-classes-on-the-saddle", c + new Vector3(-40f, -95f, -70f), c + back * 2f, back, 55f);
        sacks.ClearExperiments();
        }

        if (Wanted('W')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        sacks.ExpSetSurface(2, 0f);
        sacks.ExpSetBend(1f);
        sacks.ExpCharacterPinch(24, n => n % 12 == 1 || n % 12 == 11 ? 1 : n % 3 == 0 ? 0 : -1, SacksSpiral.Count, 26f,
            "24 arms: each prime's height has the sign of (3/p)\ncyan p ≡ ±1 (mod 12): 3 is a square mod p, falls\nmagenta p ≡ ±5 (mod 12): rises");
        yield return Shot("W1-saddle-computes-3-over-p", c + new Vector3(-130f, -130f, -55f), c + back * 6f, back, 55f);
        sacks.ClearExperiments();
        sacks.ExpCharacterPinch(24, n => n % 12 == 1 || n % 12 == 11 ? 1 : n % 3 == 0 ? 0 : -1, SacksSpiral.Count, 26f, "");
        yield return Shot("W1b-saddle-computes-3-over-p-top", c + back * 245f, c, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpCharacterPinch(16, n => n % 2 == 0 ? 0 : n % 8 == 1 || n % 8 == 7 ? 1 : -1, SacksSpiral.Count, 26f,
            "16 arms: each prime's height has the sign of (2/p)\ncyan p ≡ ±1 (mod 8): falls\nmagenta p ≡ ±3 (mod 8): rises");
        yield return Shot("W2-saddle-computes-2-over-p", c + new Vector3(-130f, -130f, -55f), c + back * 6f, back, 55f);
        sacks.ClearExperiments();
        sacks.ExpSetSurface(12, 0f);
        sacks.ExpSetBend(.5f);
        sacks.ExpSurfaceWire(sacks.Extent, new Color(.3f, .32f, .42f), 8f, 24);
        sacks.ExpCharacterPinch(144, n => n % 12 == 1 || n % 12 == 11 ? 1 : n % 3 == 0 ? 0 : -1, SacksSpiral.Count, 26f,
            "144 arms on a 12-fold saddle\nthe 48 prime arms sit at ±0.87 of the height, by (3/p)");
        yield return Shot("W3-144-arms-12-fold-saddle", c + new Vector3(-120f, -150f, -60f), c + back * 4f, back, 55f);
        sacks.ClearExperiments();
        sacks.ExpSetSurface(2, 0f);
        sacks.ExpSetBend(1f);
        }

        if (Wanted('X')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        Vector3 fp = c + new Vector3(8200f, 0f, 0f);
        float fx = 16.5f, cellF = 2.2f, fl = 26f;
        sacks.ExpFingerprint(fp + new Vector3(-fx, 66f, 0f), cellF, null, 10000000, "primes", fl, true);
        sacks.ExpFingerprint(fp + new Vector3(fx, 66f, 0f), cellF, k => k * k, 1440, "k²", fl);
        sacks.ExpFingerprint(fp + new Vector3(-fx, 22f, 0f), cellF, k => k * k + k + 41, 1440, "k² + k + 41", fl);
        sacks.ExpFingerprint(fp + new Vector3(fx, 22f, 0f), cellF, k => k * k + k + 17, 1440, "k² + k + 17", fl);
        sacks.ExpFingerprint(fp + new Vector3(-fx, -22f, 0f), cellF, k => k * k + 1, 1440, "k² + 1", fl);
        sacks.ExpFingerprint(fp + new Vector3(fx, -22f, 0f), cellF, k => k * k + k + 1, 1440, "k² + k + 1", fl);
        sacks.ExpFingerprint(fp + new Vector3(-fx, -66f, 0f), cellF, k => k * (k + 1), 1440, "k(k + 1)", fl);
        sacks.ExpFingerprint(fp + new Vector3(fx, -66f, 0f), cellF, k => k * (4 * k + 1), 1440, "u(4u + 1)", fl);
        yield return Shot("X1-fingerprints-mod-144", fp + new Vector3(0f, 2f, 0f) + back * 188f, fp + new Vector3(0f, 2f, 0f), Vector3.up, 55f);
        Vector3 top2 = fp + new Vector3(0f, 44f, 0f);
        yield return Shot("X2-fingerprints-close", top2 + back * 118f, top2, Vector3.up, 55f);
        sacks.ClearExperiments();
        }

        if (Wanted('Y')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(true);
        sacks.ExpSetSurface(2, 0f);
        sacks.ExpSetBend(0f);
        sacks.ExpNumberField(SacksSpiral.Count);
        sacks.ExpClock144(SacksSpiral.Count, 4000000, 34f);
        yield return Shot("Y1-clock-144-top", c + new Vector3(0f, -8f, 0f) + back * 300f, c + new Vector3(0f, -8f, 0f), Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpSetBend(1f);
        sacks.ExpNumberField(SacksSpiral.Count);
        sacks.ExpClock144(SacksSpiral.Count, 4000000, 0f);
        yield return Shot("Y2-clock-144-on-the-saddle", c + new Vector3(-40f, -95f, -70f), c + back * 2f, back, 55f);
        sacks.ClearExperiments();
        }

        // ---------------------------------------------------------------------------------------------
        // Mirrors, the sieve machine, the torus knot, the umbilic torus: sections Z1–Z4 (SacksTwisted.cs),
        // run with the lowercase letters m, l, k, t
        // ---------------------------------------------------------------------------------------------
        if (Wanted('m')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(true);
        sacks.ExpSetSurface(2, 0f);
        sacks.ExpSetBend(0f);
        sacks.ExpNumberField(SacksSpiral.Count);
        sacks.ExpMirrorChords("pronic", 10, 40, .3f, true, true);
        yield return Shot("Z1a-mirror-chords-pronic", c + back * Full, c, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpMirrorChords("squares", 10, 40, .3f, true, true);
        yield return Shot("Z1b-mirror-chords-squares", c + back * Full, c, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpMirrorChords("euler", 10, 40, .3f, true, true);
        yield return Shot("Z1c-mirror-chords-euler", c + back * Full, c, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpSetBend(1f);
        sacks.ShowBackdrop(false);
        Vector3 sky = c + new Vector3(SacksSpiral.SkylineOffset, 0f, 0f);
        sacks.ExpMirrorSkyline(sky, 11f, 44f, 26f);
        yield return Shot("Z1e-mirror-skyline", sky + new Vector3(-16f, 6f, 0f) + back * 148f, sky + new Vector3(-16f, 6f, 0f), Vector3.up, 55f);
        sacks.ClearExperiments();
        Vector3 mob = c + new Vector3(SacksSpiral.MobiusOffset, 0f, 0f);
        sacks.ExpMobiusChords(mob, 30f, 12f, new[] { "pronic", "squares", "euler" }, 10, 60, 0f);
        yield return Shot("Z1f-mobius-chords", mob + new Vector3(0f, -150f, -88f), mob, back, 55f);
        sacks.ClearExperiments();
        sacks.ExpMobiusChords(mob, 30f, 12f, new[] { "pronic", "squares", "euler" }, 10, 60, 30f);
        yield return Shot("Z1g-mobius-chords-top", mob + new Vector3(0f, -14f, 0f) + back * 215f, mob + new Vector3(0f, -14f, 0f), Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ShowBackdrop(true);
        sacks.ExpNumberField(SacksSpiral.Count);
        }

        if (Wanted('l')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        Vector3 mach = c + new Vector3(SacksSpiral.MachineOffset, 0f, 0f);
        int[] mods = { 3, 4, 5, 7, 8, 9, 11, 13, 16, 17, 19 };
        sacks.ExpSieveBoard(mach, 1501, 39, 60, mods, 2.4f, 22f);
        yield return Shot("Z2a-sieve-board-1501", mach + new Vector3(-3f, 1f, 0f) + back * 82f, mach + new Vector3(-3f, 1f, 0f), Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpSieveBoard(mach, 50399, 225, 320, mods, 2f, 24f, 48);
        yield return Shot("Z2b-sieve-board-50399", mach + new Vector3(-3f, 1f, 0f) + back * 125f, mach + new Vector3(-3f, 1f, 0f), Vector3.up, 55f);
        sacks.ClearExperiments();
        Vector3 dial = mach + new Vector3(0f, 300f, 0f), dialView = dial + new Vector3(0f, -8f, 0f);
        sacks.ExpLehmerLive(dial, 1501, 60, mods, 4f, 30f);
        yield return new WaitForSeconds(.9f);
        yield return Shot("Z2c-sieve-dial-midway", dialView + back * 125f, dialView, Vector3.up, 55f);
        yield return new WaitForSeconds(3f);
        yield return Shot("Z2d-sieve-dial-1501", dialView + back * 125f, dialView, Vector3.up, 55f);
        sacks.ExpLehmerLive(dial, 50399, 320, mods, 6f, 30f);
        yield return new WaitForSeconds(14f);
        yield return Shot("Z2e-sieve-dial-50399", dialView + back * 125f, dialView, Vector3.up, 55f);
        sacks.ExpStopLive();
        sacks.ClearExperiments();
        }

        if (Wanted('k')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        Vector3 knot = c + new Vector3(SacksSpiral.KnotOffset, 0f, 0f);
        sacks.ExpTorusKnot(knot, 30f, 12f, 5, 7, 1000000, false, 0f);
        yield return Shot("Z3a-torus-knot-5-7", knot + new Vector3(0f, -150f, -88f), knot, back, 55f);
        sacks.ClearExperiments();
        sacks.ExpTorusKnot(knot, 30f, 12f, 5, 7, 1000000, true, 30f);
        yield return Shot("Z3b-torus-knot-5-7-euler", knot + new Vector3(0f, -125f, -125f), knot + new Vector3(0f, -6f, 0f), back, 55f);
        sacks.ClearExperiments();
        sacks.ExpTorusKnot(knot, 30f, 12f, 5, 7, 1000000, false, 30f);
        sacks.ExpKnotWalkLive(knot, 30f, 12f, 5, 7, 40, 4f, 30f);
        yield return new WaitForSeconds(5f);
        yield return Shot("Z3c-torus-knot-walk-40", knot + new Vector3(0f, -125f, -125f), knot + new Vector3(0f, -6f, 0f), back, 55f);
        sacks.ExpStopLive();
        sacks.ClearExperiments();
        }

        if (Wanted('t')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        Vector3 umb = c + new Vector3(SacksSpiral.UmbilicOffset, 0f, 0f);
        sacks.ExpUmbilic(umb, 4f, 1f, 40, null, 0, 0f);
        yield return Shot("Z4a-umbilic-torus-turns", umb + new Vector3(0f, -150f, -88f), umb, back, 55f);
        sacks.ClearExperiments();
        sacks.ExpUmbilic(umb, 4f, 1f, 40, new long[] { 1 }, 0, 0f);
        yield return Shot("Z4b-umbilic-k2-k-1", umb + new Vector3(0f, -150f, -88f), umb, back, 55f);
        yield return Shot("Z4c-umbilic-k2-k-1-close", umb + new Vector3(0f, -82f, -48f), umb + new Vector3(0f, -26f, 0f), back, 55f);
        sacks.ClearExperiments();
        sacks.ExpUmbilic(umb, 4f, 1f, 40, new long[] { 3 }, 0, 0f);
        yield return Shot("Z4d-umbilic-k2-k-3-close", umb + new Vector3(0f, -82f, -48f), umb + new Vector3(0f, -26f, 0f), back, 55f);
        sacks.ClearExperiments();
        sacks.ExpUmbilic(umb, 4f, 0f, 40, new long[] { 1 }, 0, 0f);
        yield return Shot("Z4e-untwisted-k2-k-1", umb + new Vector3(0f, -150f, -88f), umb, back, 55f);
        sacks.ClearExperiments();
        }

        // ---------------------------------------------------------------------------------------------
        // Intersecting geometries: sections AA–AE (SacksIntersect.cs), lowercase letters a–e, drawn for 1920 × 1080
        // (ZETA_EXP_WIDE=1). Landscape at fov 55: the frame is 1.85 d wide and 1.04 d tall.
        // ---------------------------------------------------------------------------------------------
        if (Wanted('a')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        Vector3 na = c + new Vector3(SacksSpiral.NormSurfaceOffset, 0f, 0f);
        sacks.ExpNormSurfaces(na, 2.5f, 6f, .18f, 10f, 7f, SacksSpiral.DefaultNormSlices, 30f);
        Vector3 look = na + new Vector3(24f, 18f, -8f);
        yield return Shot("AA1-paraboloid-and-saddle", look + new Vector3(-36f, -62f, -50f), look, back, 55f);
        sacks.ClearExperiments();
        long[] lv = { 13, 23, 25, 65 };
        for (int i = 0; i < lv.Length; i++) sacks.ExpFactorLevelSets(na + new Vector3(-84f + 56f * i, 0f, 0f), 25f, lv[i], 40f);
        yield return Shot("AA3-factor-pairs-and-squares", na + new Vector3(5f, -6f, 0f) + back * 134f, na + new Vector3(5f, -6f, 0f), Vector3.up, 55f);
        sacks.ClearExperiments();
        }

        if (Wanted('b')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        Vector3 rb = c + new Vector3(SacksSpiral.RingLatticeOffset, 0f, 0f);
        long[] pr = { 13, 17, 19, 23 };
        for (int i = 0; i < pr.Length; i++) sacks.ExpRingLattice(rb + new Vector3(-87f + 58f * i, 0f, 0f), 4f, pr[i], 40f);
        yield return Shot("AB1-rings-of-primes-meet-two-lattices", rb + new Vector3(0f, -8f, 0f) + back * 132f, rb + new Vector3(0f, -8f, 0f), Vector3.up, 55f);
        sacks.ClearExperiments();
        long[] co = { 65, 91, 1105, 1729 };
        for (int i = 0; i < co.Length; i++) sacks.ExpRingLattice(rb + new Vector3(-87f + 58f * i, 0f, 0f), 22f / Mathf.Sqrt(co[i]), co[i], 40f, 1.3f);
        yield return Shot("AB2-rings-of-composites", rb + new Vector3(0f, -8f, 0f) + back * 132f, rb + new Vector3(0f, -8f, 0f), Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpEulerLattice(rb, 1f, 40, 22f);
        Vector3 el = rb + new Vector3(12f, 18f, 0f);
        yield return Shot("AB3-euler-lattice-row", el + back * 66f, el, Vector3.up, 55f);
        sacks.ClearExperiments();
        }

        if (Wanted('c')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        Vector3 rt = c + new Vector3(SacksSpiral.ResidueTorusOffset, 0f, 0f);
        sacks.ExpResidueTorusMatrix(rt, .5f, 1000000, 12, "euler", 40f,
            "the residue torus of 2, 3, 5, …, 37|144 shadows: panel (i, j) is Z/p<sub>i</sub> × Z/p<sub>j</sub>|red: the cancellation cross (0 mod p<sub>i</sub> or p<sub>j</sub>)|white: the primes p<sub>i</sub>, p<sub>j</sub> themselves|green: the residues of k² + k + 41|— never on a cross");
        Vector3 rtl = rt + new Vector3(-30f, 0f, 0f);
        yield return Shot("AC1-144-shadows-euler", rtl + back * 124f, rtl, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpResidueTorusMatrix(rt, .5f, 1000000, 12, "pronic", 40f,
            "the residue torus of 2, 3, 5, …, 37|144 shadows: panel (i, j) is Z/p<sub>i</sub> × Z/p<sub>j</sub>|red: the residues of k(k + 1)|white: where they land on a cross|— every panel, both arms");
        yield return Shot("AC2-144-shadows-pronic", rtl + back * 124f, rtl, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpResidueTorusMatrix(rt, 1.5f, 1000000, 6, "euler,pronic,landau", 34f,
            "36 shadows: 2, 3, 5, 7, 11, 13|green: k² + k + 41 (never on a cross)|red: k(k + 1) (on every cross)|orange: k² + 1 (on the crosses of 2, 5, 13)");
        Vector3 rtc = rt + new Vector3(-22f, 3f, 0f);
        yield return Shot("AC3-36-shadows-close", rtc + back * 88f, rtc, Vector3.up, 55f);
        sacks.ClearExperiments();
        }

        if (Wanted('d')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        Vector3 md = c + new Vector3(SacksSpiral.ModularOffset, 0f, 0f);
        sacks.ExpModularPlane(md, 12f, 7.5f, 7f, 49, true, 40f, 24);
        Vector3 mdl = md + new Vector3(0f, 40f, 0f);
        yield return Shot("AD1-curves-on-the-modular-surface", mdl + back * 104f, mdl, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpModularPlane(md, 40f, 2.4f, 2.7f, 12, true, 64f);
        Vector3 mdz = md + new Vector3(0f, 44f, 0f);
        yield return Shot("AD2-fundamental-domain-close", mdz + back * 110f, mdz, Vector3.up, 55f);
        sacks.ClearExperiments();
        }

        if (Wanted('e')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        Vector3 ra = c + new Vector3(SacksSpiral.RaceOffset, 0f, 0f);
        sacks.ExpRaceChart(ra, 190f, 92f, 44f, false, true, new[] { 48231785.0 });
        Vector3 ral = ra + new Vector3(0f, -4f, 0f);
        yield return Shot("AE1-prime-race-mod-12", ral + back * 118f, ral, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpRaceChart(ra, 190f, 80f, 44f, true, true, new[] { 26861.0, 616841.0, 12306137.0 });
        yield return Shot("AE2-prime-race-mod-4", ral + back * 118f, ral, Vector3.up, 55f);
        sacks.ClearExperiments();
        }

        // ---------------------------------------------------------------------------------------------
        // The Sacks spiral spooled in 3D: sections BA–BD (SacksYarn.cs), lowercase letters f–i, drawn for 1920 × 1080.
        // ---------------------------------------------------------------------------------------------
        if (Wanted('f')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        Vector3 sb = c + new Vector3(SacksSpiral.SacksBallOffset, 0f, 0f);
        sacks.ExpSacksYarnBall(sb, 60, 1.45f, 24f, 76f);
        yield return Shot("BA1-sacks-yarn-ball", sb + new Vector3(-55f, 62f, -180f), sb, Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpSacksHelices(sb, 50, 1.45f, 24f, new[] { 1, 2 }, 30f);
        Vector3 hx = sb + new Vector3(45f, 0f, 0f);
        yield return Shot("BA2-double-helices", hx + new Vector3(0f, 16f, -56f), hx, Vector3.up, 55f);
        sacks.ClearExperiments();
        }

        if (Wanted('g')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        Vector3 cb = c + new Vector3(SacksSpiral.CubeBallOffset, 0f, 0f);
        sacks.ExpCubeYarnBall(cb, 40, 1.45f, 64f);
        yield return Shot("BB1-cube-yarn-ball", cb + new Vector3(-150f, 55f, -40f), cb + new Vector3(0f, -4f, 0f), Vector3.up, 55f);
        sacks.ClearExperiments();
        Vector3 cc = c + new Vector3(SacksSpiral.CubeCornerOffset, 0f, 0f);
        sacks.ExpCubeCorner(cc, 1.45f, 60, 120, 58f, 18f);
        yield return ShotOrtho("BB2-down-corner-1", cc + SacksSpiral.CubeCorner1 * 230f, cc, SacksSpiral.CubeCorner1Up, 27f);
        sacks.ClearExperiments();
        Vector3 ce = c + new Vector3(SacksSpiral.CubeEulerOffset, 0f, 0f);
        sacks.ExpCubeEulerLines(ce, .5f, 400, 90f);
        Vector3 cel = ce + new Vector3(-45f, 0f, 0f);
        yield return ShotOrtho("BB3-euler-lines-of-the-cube-ball", cel + Vector3.down * 500f, cel, Vector3.left, 125f);
        sacks.ClearExperiments();
        }

        if (Wanted('h')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        Vector3 cm = c + new Vector3(SacksSpiral.CubeCombOffset, 0f, 0f);
        sacks.ExpCubeComb(cm, 2f, 4f, 6, 24, 34f);
        Vector3 cml = cm + new Vector3(2f, 2f, 0f);
        yield return Shot("BC1-comb-at-corner-1", cml + back * 78f, cml, Vector3.up, 55f);
        sacks.ClearExperiments();
        }

        if (Wanted('i')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        Vector3 cs = c + new Vector3(SacksSpiral.CubeShellOffset, 0f, 0f);
        sacks.ExpCubeShell(cs, 30, 1.45f, 34f, 36f);
        Vector3 corner = cs + SacksSpiral.CubeCorner1 * (1.45f * Mathf.Pow(27000f + 2791f / 3f, 1f / 3f));
        yield return Shot("BD1-shell-at-corner-1", corner + SacksSpiral.CubeCorner1 * 84f, corner, SacksSpiral.CubeCorner1Up, 55f);
        sacks.ClearExperiments();
        }

        // ---------------------------------------------------------------------------------------------
        // The divisor spiral lifted into a funnel and cut by spheres: sections CA–CE (SacksFunnel.cs), letters n–r,
        // drawn for 1920 × 1080.
        // ---------------------------------------------------------------------------------------------
        if (Wanted('n')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        Vector3 ds = c + new Vector3(SacksSpiral.DivisorSpiralOffset, 0f, 0f);
        sacks.ExpDivisorSpiral(ds, 100000, 1.45f, 1f, 620f, true);
        yield return ShotOrtho("CA1-divisor-spiral", ds + back * 900f, ds, Vector3.up, 560f);
        sacks.ClearExperiments();
        sacks.ExpDivisorSpiral(ds, 100000, 1.45f, 1.05f, 0f, false);
        sacks.ExpDivisorCurves(ds, 100000, 1.45f, 1.05f, 400, 330f);
        Vector3 dsl = ds + new Vector3(-230f, 0f, 0f);
        yield return ShotOrtho("CA2-prime-curves-in-the-dark-lanes", dsl + back * 900f, dsl, Vector3.up, 250f);
        sacks.ClearExperiments();
        }

        if (Wanted('o')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        Vector3 fo = c + new Vector3(SacksSpiral.FunnelOffset, 0f, 0f);
        sacks.ExpFactorFunnel(fo, 20000, 20000, 1.45f, 1.2f, 170f, 4, 270f, new[] { 0f, 0f, -27f, 153f, 63f });
        yield return Shot("CB1-factorization-funnel", fo + new Vector3(-180f, -360f, -190f), fo + new Vector3(0f, 0f, -70f), back, 55f);
        sacks.ClearExperiments();
        sacks.ExpFactorNeedles(fo, new long[] { 5040, 5039, 5041 }, new[] { "5,040 = 7!:  30 factor pairs", "5,039 prime: empty", "5,041 = 71²: one point" },
            new[] { new Vector2(0f, 180f), new Vector2(-150f, 120f), new Vector2(150f, 120f) }, 6000, 1.45f, 1.1f, 165f, 260f, Vector3.up);
        Vector3 nd = fo + new Vector3(103f, -4.5f, -88f);
        yield return Shot("CB2-needles-of-factor-pairs", nd + new Vector3(232f, 0f, -40f), nd, back, 55f);
        sacks.ClearExperiments();
        }

        if (Wanted('p')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        Vector3 fe = c + new Vector3(SacksSpiral.FermatOffset, 0f, 0f);
        sacks.ExpFermatSpheres(fe, 1105, 52, new[] { 41, 49 }, 2700, 1.45f, 1f, 130f, new Vector3(-.92f, -.39f, 0f));
        yield return Shot("CC1-spheres-find-the-factors", fe + new Vector3(-70f, 190f, -80f), fe + new Vector3(0f, 25f, -25f), back, 55f);
        sacks.ClearExperiments();
        }

        if (Wanted('q')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        Vector3 ls = c + new Vector3(SacksSpiral.LatticeSphereOffset, 0f, 0f);
        sacks.ExpLatticeSpheres(ls, new long[] { 163, 59, 23, 35, 195, 1155 }, 3, 105f, 118f, 32f, 175f, new[] { "primes", "composites" });
        yield return Shot("CD1-spheres-through-the-cubic-lattice", ls + new Vector3(-20f, -95f, -255f), ls + new Vector3(-20f, -12f, 0f), Vector3.up, 55f);
        sacks.ClearExperiments();
        sacks.ExpThreeSquaresChart(ls, 5000, 300f, 160f, 36, 166f);
        yield return Shot("CD2-three-squares-parity", ls + new Vector3(0f, -2f, 0f) + back * 222f, ls + new Vector3(0f, -2f, 0f), Vector3.up, 55f);
        sacks.ClearExperiments();
        }

        if (Wanted('r')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        Vector3 cf = c + new Vector3(SacksSpiral.CubeFlatOffset, 0f, 0f);
        sacks.ExpCubeFlat(cf, 40, 1.45f, .2f, 90f);
        yield return ShotOrtho("CE1-cube-ball-flattened", cf + back * 300f, cf, Vector3.up, 75f);
        sacks.ClearExperiments();
        sacks.ExpCubeFlat(cf, 40, 1.45f, .12f, 26f, 13f);
        Vector3 cfz = cf + new Vector3(2f, 0f, 0f);
        yield return ShotOrtho("CE2-cube-ball-flattened-centre", cfz + back * 300f, cfz, Vector3.up, 21f);
        sacks.ClearExperiments();
        }

        // ---------------------------------------------------------------------------------------------
        // Exponential sequences, centroids, the Fermat spiral and the Sacks spiral on a cone: sections DA–DD
        // (SacksCardioid.cs), letters j, s, u, v, drawn for 1920 × 1080.
        // ---------------------------------------------------------------------------------------------
        if (Wanted('j')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        Vector3 cd = c + new Vector3(SacksSpiral.CardioidOffset, 0f, 0f);
        sacks.ExpDoublingCardioid(cd, 163, 2, 45f, cd + new Vector3(88f, 0f, 0f), 9f, 105f, "x² + x + 41");
        Vector3 cdl = cd + new Vector3(42f, 0f, 0f);
        yield return Shot("DA1-doubling-cardioid-and-euler-centroid", cdl + back * 118f, cdl, Vector3.up, 55f);
        sacks.ClearExperiments();
        Vector3 cg = c + new Vector3(SacksSpiral.CentroidGridOffset, 0f, 0f);
        sacks.ExpCentroidGrid(cg, new (long, long, string, string)[]
        {
            (11, 2, "11", "x² + x + 3"), (19, 2, "19", "x² + x + 5"), (43, 3, "43", "x² + x + 11"), (67, 2, "67", "x² + x + 17"),
            (23, 5, "23", "x² + x + 6: always even"), (31, 3, "31", "x² + x + 8: always even"), (341, 2, "341 = 11 · 31", "2<sup>340</sup> ≡ 1: 10 steps"), (561, 2, "561 = 3 · 11 · 17", "Carmichael: 40 steps"),
        }, 4, 62f, 64f, 22f, 150f, "dots: the centroids of the even (green) and odd (blue) steps of g<sup>k</sup> mod n|times (n − 1)/2 they are the roots of");
        yield return Shot("DA2-centroids-of-exponential-walks", cg + new Vector3(0f, -1f, 0f) + back * 146f, cg + new Vector3(0f, -1f, 0f), Vector3.up, 55f);
        sacks.ClearExperiments();
        }

        if (Wanted('s')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        Vector3 fs = c + new Vector3(SacksSpiral.FermatSpiralOffset, 0f, 0f);
        sacks.ExpFermatSpiral(fs, 30000, 2, 1.45f, 1.2f, 330f, new long[] { 1 }, new long[0], 0f, 1e9f, 3f, new[] { 170f }, new[] { "residue 1: every prime, and the pseudoprimes" });
        yield return ShotOrtho("DB1-fermat-spiral", fs + back * 600f, fs, Vector3.up, 262f);
        sacks.ClearExperiments();
        sacks.ExpFermatSpiral(fs, 30000, 2, 1.45f, .32f, 30f, new long[] { 1, 2, 4, 8, 16, 32, 64, 128 }, new long[] { 341, 561, 645, 1105, 1387, 1729, 1905, 2047, 2465, 2701 }, 0f, 76f, 2.5f,
            new[] { 6f, 9f, 13f, 18f, 25f, 33f, 42f, 56f }, new[] { "residue 1: p", "2: 2p", "4: 3p", "8: 4p", "16: 5p, p ≡ 1 (mod 4)", "32: 6p", "64: 7p, p ≡ 1 (mod 3)", "128: 8p" });
        Vector3 fz = fs + new Vector3(40f, 9f, 0f);
        yield return ShotOrtho("DB2-fermat-spiral-near-the-prime-ray", fz + back * 600f, fz, Vector3.up, 21f);
        sacks.ClearExperiments();
        }

        if (Wanted('u')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        Vector3 st = c + new Vector3(SacksSpiral.FermatStackOffset, 0f, 0f);
        sacks.ExpFermatStack(st, 30000, new long[] { 2, 3, 5, 7 }, 1.45f, 70f, 1.1f, 300f, new long[] { 561, 2465, 8911 });
        // top left of the frame (worked out from the camera below)
        sacks.ExpWorldLegend(st + new Vector3(-119f, 79.5f, -268f), new Vector3(-2.3f, -6.4f, 12.7f) * 1.1f,
            new[] { "gold edge: the primes, in every base", "red: composites that pass the test in that base" }, 102f, new Color(.8f, .83f, .93f));
        Vector3 stl = st + new Vector3(120f, -45f, -135f);
        yield return Shot("DC1-fermat-test-in-four-bases", stl + new Vector3(-110f, -300f, -170f), stl, back, 55f);
        sacks.ClearExperiments();
        }

        if (Wanted('v')) {
        sacks.ShowRoutes(false);
        sacks.ShowBackdrop(false);
        Vector3 cc = c + new Vector3(SacksSpiral.ConeCombOffset, 0f, 0f);
        sacks.ExpConeSpiral(cc, 6000, 1.45f, 2, new[] { (0, 0), (1, 0), (0, 1), (1, 1), (0, -1), (1, -1), (0, -2), (1, -2), (1, 41) }, .35f, 150f, 3);
        Vector3 ccl = cc + new Vector3(0f, 58f, -8f);
        yield return Shot("DD1-sacks-spiral-wrapped-on-a-cone", ccl + new Vector3(-74f, 82f, -129f), ccl + new Vector3(12f, 0f, 0f), Vector3.up, 55f);
        sacks.ClearExperiments();
        var strands = new List<(int, int, string)>();
        for (int D = -23; D <= 25; D++)
        {
            int m4 = ((D % 4) + 4) % 4;
            if (m4 == 0) strands.Add((0, -D / 4, ""));
            else if (m4 == 1) strands.Add((1, (1 - D) / 4, ""));
        }

        sacks.ExpConeComb(cc, 1.45f, 100f, 200f, 30f, strands.ToArray(), .45f, 130f);
        yield return Shot("DD2-one-comb-ordered-by-discriminant", cc + new Vector3(0f, 3f, 0f) + back * 104f, cc + new Vector3(0f, 3f, 0f), Vector3.up, 55f);
        sacks.ClearExperiments();
        }

        UnityEngine.Rendering.AsyncGPUReadback.WaitAllRequests();
        yield return new WaitForSeconds(1f);
        director.Recorder.EndOffscreen();
        Debug.Log("Experiment snapshots → " + outDir);
    }
}
