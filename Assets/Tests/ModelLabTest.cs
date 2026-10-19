using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// The model lab (Assets/Lab): its arithmetic and E8 geometry checked exactly, every model opened and driven through its
/// controls on the desktop, and the VR Models page and fingertip picking in the desktop simulation of the headset.
///   Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter ModelLabTest
/// With ZETA_OUT set, it also writes 1920 × 1080 pictures of the models (with the board and the strip) and of the VR page.
/// </summary>
public class ModelLabTest
{
    readonly List<string> errors = new();

    void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception) errors.Add(message);
    }

    [Test]
    public void Arithmetic()
    {
        Assert.AreEqual("2³·3²·5", LabMath.FactorString(360));
        Assert.AreEqual("2<sup>8</sup>", LabMath.FactorString(256));
        Assert.AreEqual(55798470L, LabMath.Sigma3(360));
        Assert.AreEqual(1L + 397L * 397 * 397, LabMath.Sigma3(397));
        Assert.AreEqual(4, LabMath.Order(7, 30));
        Assert.AreEqual(2, LabMath.Order(11, 30));
        Assert.AreEqual(1, LabMath.Order(31, 30));

        // the Hurwitz sums against mpmath (t = 17): S_h = 30^(−s) ζ(s, h/30)
        void Near(Cpx z, double re, double im, double tol, string what)
        {
            Assert.AreEqual(re, z.Re, tol, what + " (real part)");
            Assert.AreEqual(im, z.Im, tol, what + " (imaginary part)");
        }

        Near(LabMath.HourSum(1, 17d), 1.12508015262, -0.333508967306, 1e-8, "S_1(½ + 17i)");
        Near(LabMath.HourSum(7, 17d), -0.124525141422, -0.387300741009, 1e-8, "S_7(½ + 17i)");
        Near(LabMath.HourSum(30, 17d), 0.260947196029, -0.291456819323, 1e-8, "S_30(½ + 17i)");
        Near(LabMath.CriticalZeta(17d), 1.946654834, 0.895405192, 1e-8, "ζ(½ + 17i)");

        // at the first three zeros every class of hours by gcd(h, 30) sums to zero on its own
        foreach (double t in new[] { 14.1347251417, 21.0220396388, 25.0108575801 })
        {
            foreach (int d in new[] { 1, 2, 3, 5, 6, 10, 15, 30 })
            {
                Cpx s = Cpx.Zero;
                double biggest = 0d;
                for (int h = 1; h <= 30; h++)
                {
                    if (LabMath.Gcd(h, 30) != d) continue;
                    Cpx v = LabMath.HourSum(h, t);
                    s += v;
                    biggest = Math.Max(biggest, v.Abs);
                }

                Assert.Less(s.Abs, 1e-7, $"class gcd {d} at t = {t}: sum {s}");
                if (d < 15) Assert.Greater(biggest, .1, $"class gcd {d}: the arrows themselves are not small");
            }
        }
    }

    [Test]
    public void E8()
    {
        Assert.AreEqual(240, E8Geometry.Roots.Length);
        foreach (int[] r in E8Geometry.Roots) Assert.AreEqual(2, E8Geometry.Dot(r, r));
        // the Coxeter element: order 30 on every root, 8 orbits
        HashSet<int> orbitIds = new();
        for (int i = 0; i < 240; i++)
        {
            int j = i, k = 0;
            do
            {
                j = E8Geometry.CoxeterNext[j];
                k++;
            } while (j != i && k < 100);

            Assert.AreEqual(30, k, "every root comes back after 30 steps");
            orbitIds.Add(E8Geometry.Orbit[i]);
        }

        Assert.AreEqual(8, orbitIds.Count);
        // c turns plane m by −12m degrees
        for (int plane = 0; plane < 4; plane++)
        {
            float expect = -12f * E8Geometry.Exponents[plane];
            foreach (int[] r in E8Geometry.Roots)
            {
                Vector2 a = E8Geometry.Project(r, plane), b = E8Geometry.Project(E8Geometry.Coxeter(r), plane);
                if (a.magnitude < .1f) continue;
                Assert.AreEqual(a.magnitude, b.magnitude, 1e-4f);
                float turn = Mathf.DeltaAngle(0f, Vector2.SignedAngle(a, b));
                Assert.AreEqual(Mathf.DeltaAngle(0f, expect), turn, .01f, $"plane {E8Geometry.Exponents[plane]}");
            }
        }

        // each orbit's rings across the planes, and each plane's rings across the orbits, are permutations
        for (int plane = 0; plane < 4; plane++)
        {
            HashSet<int> rings = new();
            for (int o = 0; o < 8; o++) rings.Add(E8Geometry.RingOfOrbit[plane, o]);
            Assert.AreEqual(8, rings.Count);
        }

        // reflections permute the roots
        int[] alpha = E8Geometry.Roots[17];
        int fixedCount = 0;
        foreach (int[] r in E8Geometry.Roots)
        {
            Assert.GreaterOrEqual(E8Geometry.IndexOf(E8Geometry.Reflect(r, alpha)), 0);
            if (E8Geometry.Dot(r, alpha) == 0) fixedCount++;
        }

        Assert.AreEqual(126, fixedCount, "the roots orthogonal to a root form E7");
        // Frobenius on the planes: 7 cycles 1 → 7 → 11 → 13, 11 swaps them in pairs, 31 fixes them
        Assert.AreEqual(1, E8Geometry.FrobeniusPlane(0, 7));
        Assert.AreEqual(2, E8Geometry.FrobeniusPlane(1, 7));
        Assert.AreEqual(3, E8Geometry.FrobeniusPlane(2, 7));
        Assert.AreEqual(0, E8Geometry.FrobeniusPlane(3, 7));
        Assert.AreEqual(2, E8Geometry.FrobeniusPlane(0, 11));
        Assert.AreEqual(3, E8Geometry.FrobeniusPlane(1, 41));
        Assert.AreEqual(0, E8Geometry.FrobeniusPlane(0, 31));
        Assert.AreEqual(2160, E8Geometry.Shell4.Length);
        foreach (int[] v in E8Geometry.Shell4) Assert.AreEqual(4, E8Geometry.Dot(v, v));
    }

    [UnityTest]
    [Timeout(1800000)]
    public IEnumerator DesktopModels()
    {
        Application.logMessageReceived += OnLog;
        string outDir = Environment.GetEnvironmentVariable("ZETA_OUT");
        bool shots = !string.IsNullOrEmpty(outDir);
        SceneManager.LoadScene("zeta");
        yield return null;
        yield return null;
        yield return Until(() => ModelLab.Instance != null && TourDirector.Instance != null && TourDirector.Instance.Captions != null, 20f);
        TourDirector director = TourDirector.Instance;
        ModelLab lab = ModelLab.Instance;
        Assert.IsNotNull(lab, "the model lab was not created");
        if (shots)
        {
            Directory.CreateDirectory(outDir);
            director.SetFormat(false);
            director.Recorder.Width = 1920;
            director.Recorder.Height = 1080;
            director.Recorder.BeginOffscreen(outDir);
            BloomEffect bloom = Camera.main.GetComponent<BloomEffect>();
            if (bloom != null) bloom.Intensity = 0.6f;
        }

        IEnumerator Shot(string name)
        {
            yield return Seconds(.4f);
            if (shots) director.Recorder.Snapshot(name + ".png");
            yield return null;
            yield return null;
        }

        // paths
        lab.Enter("paths");
        PathsModel paths = (PathsModel)lab.Find("paths");
        Assert.AreEqual(paths, lab.Active);
        paths.Funnel.Select(360, false);
        Assert.AreEqual(6, paths.Funnel.StepCount);
        Assert.AreEqual(360, lab.StripNumber);
        Assert.AreEqual(6, lab.StripLit);
        StringAssert.Contains("2³·3²·5", lab.Status);
        yield return Shot("HA1-paths-360");
        paths.Funnel.Select(397, true);
        yield return Seconds(1.8f);
        Assert.AreEqual(1, paths.Funnel.StepsShown, "a prime drops to 1 in one step");
        paths.Funnel.SetTree(1);
        lab.SetView(new LabView { Focus = paths.Funnel.Origin + Vector3.back * 6f, Distance = 175f, Yaw = -90f, Pitch = 62f, Fov = 50f }, true);
        yield return Shot("HA2-tree-of-first-steps");
        paths.Funnel.SetTree(7);
        lab.SetView(new LabView { Focus = paths.Funnel.Origin + Vector3.back * 6f, Distance = 150f, Yaw = -90f, Pitch = 89f, Fov = 50f }, true);
        yield return Shot("HA3-drops-by-7-from-above");
        paths.Funnel.SetTree(0);
        paths.Funnel.SetPowers(true);
        paths.Funnel.Select(343, false);
        lab.ResetView(true);
        yield return Shot("HA4-prime-power-spirals");
        paths.Funnel.SetPowers(false);

        // E8
        lab.Enter("e8");
        LabModel e8 = lab.Find("e8");
        yield return Shot("HB1-e8-lantern");
        Click(e8, "Frobenius 7");
        yield return Seconds(2.2f);
        StringAssert.Contains("(0 5 4 6)(1 2 7 3)", lab.Status);
        yield return Shot("HB2-after-frobenius-7");
        Click(e8, "Frobenius 13");
        yield return Seconds(2.2f);
        e8.OnPick(0);
        StringAssert.Contains("an E7", lab.Status);
        yield return Shot("HB3-neighbours-of-a-root");
        Click(e8, "Reflect");
        yield return Seconds(2f);
        StringAssert.Contains("56 pairs swap", lab.Status);
        yield return Shot("HB4-reflected");
        Click(e8, "Reset");
        Click(e8, "Edges");
        yield return Shot("HB5-edges");
        Click(e8, "Edges");
        Click(e8, "c step");
        yield return Seconds(1.2f);
        StringAssert.Contains("c applied 1 time", lab.Status);

        // unified
        lab.Enter("unified");
        UnifiedModel unified = (UnifiedModel)lab.Find("unified");
        unified.SetT(14.134725f);
        yield return Seconds(.5f);
        StringAssert.Contains("zero 1", lab.Status);
        yield return Shot("HC1-unified-whole-at-the-first-zero");
        Click(unified, "View");
        yield return Seconds(1.3f);
        yield return Shot("HC2-zeta-plane-at-the-first-zero");
        unified.SetT(17f);
        yield return Seconds(1.3f);
        yield return Shot("HC3-zeta-plane-t17");
        Click(unified, "By gcd");
        unified.SetT(21.022040f);
        yield return Seconds(1.3f);
        StringAssert.Contains("zero 2", lab.Status);
        yield return Shot("HC4-zeta-plane-zero-2-hour-order");
        Click(unified, "By gcd");
        Click(unified, "View");
        LabControl sacks = unified.Controls.Find(c => c.Label == "Sacks");
        sacks.Set(1f);
        yield return Seconds(1.3f);
        StringAssert.Contains("640320", lab.Status);
        yield return Shot("HC5-sacks-layout-and-euler");
        sacks.Set(0f);

        // a character: the 8 gold arrows turned by χ(h) add up to L(s, χ) and close at its zeros
        unified.SetCharacter(1);
        unified.SetT(8.039737f);
        Click(unified, "View");
        Click(unified, "View");
        yield return Seconds(1.3f);
        StringAssert.Contains("zero 1 of L(s, χ)", lab.Status);
        yield return Shot("HC6-zeta-plane-character-mod-3-zero-1");
        unified.SetCharacter(0);
        Click(unified, "View");
        Click(unified, "View");

        // zeros: the sums run on worker threads; wait for them, then check them against log p
        lab.Enter("zeros");
        ZerosModel zeros = (ZerosModel)lab.Find("zeros");
        yield return Until(() => zeros.Ready, 180f);
        Assert.IsTrue(zeros.Ready, "the zero sums did not finish");
        Assert.AreEqual(Mathf.Log(7f), (float)zeros.LampAt(7), .01f, "ζ's zeros give log 7 at the column of 7");
        Assert.AreEqual(Mathf.Log(2f), (float)zeros.LampAt(8), .01f, "and log 2 at 8 = 2³");
        Assert.AreEqual(0f, (float)zeros.LampAt(6), .02f, "and nothing at 6");
        Assert.AreEqual(0f, (float)zeros.LampAt(360), .02f, "or at 360");
        Assert.AreEqual(Mathf.Log(7f), (float)zeros.FanAt(7), .03f, "spoke 7's fan reaches 7");
        Assert.AreEqual(Mathf.Log(7f), (float)zeros.FanAt(49), .03f, "spoke 19's fan reaches 49 = 7² at log 7");
        Assert.AreEqual(Mathf.Log(397f), (float)zeros.FanAt(397), .03f, "and spoke 7's reaches 397");
        Assert.IsTrue(double.IsNaN(zeros.FanAt(77)), "77 is not a prime power: no tooth");
        zeros.Pick(7);
        yield return Seconds(.5f);
        StringAssert.Contains("Landau", lab.Status);
        yield return Shot("HD1-zeros-coils-comb-phases");
        zeros.SetLayers(false, true, true, false, false, false);
        yield return Shot("HD2-zeros-comb-and-rings");
        zeros.SetLayers(false, false, false, true, false, false);
        lab.SetView(new LabView { Focus = zeros.Funnel.Origin + zeros.Funnel.Up(3.4f), Distance = 165f, Yaw = -90f, Pitch = 50f, Fov = 50f }, true);
        yield return Shot("HD3-zeros-e8-fans");
        zeros.SetSpoke(1);
        StringAssert.Contains("spoke 7", lab.Status.ToLowerInvariant());
        yield return Shot("HD4-zeros-fan-of-spoke-7");
        zeros.SetSpoke(-1);
        zeros.SetLayers(false, false, false, false, true, false);
        zeros.Pick(360);
        lab.ResetView(true);
        yield return Seconds(2.5f);
        yield return Shot("HD5-zeros-lamps-and-the-path-of-360");
        zeros.SetZeroLevel(2);
        yield return Until(() => zeros.Ready, 60f);
        zeros.SetLayers(false, true, false, false, true, false);
        StringAssert.Contains("100 zeros", lab.Status);
        yield return Shot("HD6-zeros-only-100-zeros");
        zeros.SetZeroLevel(0);

        lab.Exit();
        Assert.IsNull(lab.Active);
        yield return Seconds(.5f);
        Application.logMessageReceived -= OnLog;
        Assert.IsEmpty(errors, "errors were logged:\n" + string.Join("\n", errors));
    }

    [UnityTest]
    [Timeout(1800000)]
    public IEnumerator VRModelsPage()
    {
        Application.logMessageReceived += OnLog;
        string outDir = Environment.GetEnvironmentVariable("ZETA_OUT");
        VRMode.RequestSimulation = true;
        SceneManager.LoadScene("zeta");
        yield return null;
        yield return null;
        yield return Until(() => VRMode.App != null && VRMode.App.Ready, 30f);
        VRApp app = VRMode.App;
        Assert.IsNotNull(app, "VR app did not start");
        ModelLab lab = ModelLab.Instance;
        Assert.IsNotNull(lab);
        VRRig rig = app.Rig;
        Camera cam = rig.Cam;
        RenderTexture target = new(1600, 900, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        VRSimulatedHead head = cam.GetComponent<VRSimulatedHead>();

        IEnumerator Shot(string name)
        {
            yield return null;
            if (string.IsNullOrEmpty(outDir)) yield break;
            Directory.CreateDirectory(outDir);
            cam.targetTexture = target;
            cam.Render();
            cam.targetTexture = null;
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            Texture2D tex = new(target.width, target.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            Color32[] px = tex.GetPixels32();
            for (int i = 0; i < px.Length; i++)
            {
                float a = px[i].a / 255f;
                px[i] = new Color32((byte)Mathf.Min(255, px[i].r + (1f - a) * 92f), (byte)Mathf.Min(255, px[i].g + (1f - a) * 96f), (byte)Mathf.Min(255, px[i].b + (1f - a) * 104f), 255);
            }

            tex.SetPixels32(px);
            File.WriteAllBytes(Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
            UnityEngine.Object.Destroy(tex);
        }

        app.SetRoom(false);
        app.ShowPage(3);
        head.SetLook(0f, 24f);
        lab.Enter("paths");
        yield return Seconds(2.5f);
        yield return Shot("VR1-models-page-paths");
        PathsModel paths = (PathsModel)lab.Find("paths");

        // touch the top of 97's column with the right index finger (the tops are 1–2 cm apart at this scale, so go straight to it)
        head.SetLook(0f, 0f);
        yield return Seconds(1f);
        Vector3 world = paths.Funnel.Top(97);
        Vector3 local = rig.Offset.InverseTransformPoint(world);
        Vector3 toward = rig.Offset.InverseTransformDirection((cam.transform.position - world).normalized);
        app.Hands.ScriptedRight = true;
        app.Hands.ScriptedRightPinch = 0f;
        int before = paths.Funnel.Selected;
        app.Hands.ScriptedRightTip = local + toward * .3f;
        yield return Seconds(.4f);
        // pointing from 30 cm away, the finger's ray (ModelLab.FingerRay) hovers the point; picking needs a 0.9 s hold
        Assert.AreEqual(95, lab.Hover, "the finger's ray, from 30 cm away, hovers 97's top");
        Assert.AreEqual(before, paths.Funnel.Selected, "and does not pick it before the hold");
        app.Hands.ScriptedRightTip = local;
        yield return Seconds(.4f);
        Assert.AreEqual(95, lab.Hover, "a fingertip on 97's top hovers it (pick index n − 2)");
        Assert.AreEqual(97, paths.Funnel.Selected, "touching 97's top picks 97");
        Debug.Log($"VR model scale: {rig.Scale:0.0} world units per metre");
        app.Hands.ScriptedRightTip = local + toward * .2f;
        yield return Seconds(1.5f);
        yield return Shot("VR2-picked-97");
        app.Hands.ScriptedRight = false;

        lab.Enter("e8");
        yield return Seconds(1.5f);
        yield return Shot("VR3-e8");
        lab.Enter("unified");
        yield return Seconds(1.5f);
        head.SetLook(0f, 24f);
        yield return Seconds(.6f);
        yield return Shot("VR4-unified-and-page");
        lab.Enter("zeros");
        ZerosModel zeros = (ZerosModel)lab.Find("zeros");
        yield return Until(() => zeros.Ready, 180f);
        zeros.SetLayers(true, true, false, true, false, true);
        zeros.Pick(7);
        yield return Seconds(1f);
        yield return Shot("VR5-zeros-and-page");
        lab.Exit();
        yield return Seconds(.5f);
        VRMode.RequestSimulation = false;
        Application.logMessageReceived -= OnLog;
        Assert.IsEmpty(errors, "errors were logged:\n" + string.Join("\n", errors));
    }

    static void Click(LabModel m, string label)
    {
        LabControl c = m.Controls.Find(x => x.Label == label);
        Assert.IsNotNull(c, $"{m.Id} has no control {label}");
        c.Clicked();
    }

    static IEnumerator Seconds(float s)
    {
        float t = 0f;
        while (t < s)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    static IEnumerator Until(Func<bool> condition, float timeout)
    {
        float t = 0f;
        while (!condition() && t < timeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}
