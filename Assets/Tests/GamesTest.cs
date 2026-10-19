using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// The games (Assets/Games): their arithmetic checked exactly; every game opened in the model lab and played through by
/// scripted picks on the desktop; Prime hunt played in the desktop simulation of the headset, by a fingertip touch and
/// by holding the finger's ray on a number; and the marble fingerprints drawn alone for the legibility study.
///   Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter GamesTest
/// With ZETA_OUT set it writes 1920 × 1080 pictures of the games (with the board) and of the VR view; with ZETA_FP_OUT,
/// one picture per fingerprint (no digits: the marbles alone), named by the number.
/// </summary>
public class GamesTest
{
    readonly List<string> errors = new();

    void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception) errors.Add(message);
    }

    /// <summary>A test that stops early must not leave the next ones in the headset simulation, or listening twice.</summary>
    [TearDown]
    public void TearDown()
    {
        VRMode.RequestSimulation = false;
        Application.logMessageReceived -= OnLog;
        errors.Clear();
    }

    [Test]
    public void Arithmetic()
    {
        Assert.AreEqual(NumberClass.Unit, NumberKind.Classify(1));
        foreach (long p in new long[] { 2, 3, 5, 7, 97, 7919, 1000003 }) Assert.AreEqual(NumberClass.Prime, NumberKind.Classify(p), $"{p}");
        foreach (long n in new long[] { 4, 9, 25, 49, 121, 169, 994009 }) Assert.AreEqual(NumberClass.PrimeSquare, NumberKind.Classify(n), $"{n}");
        foreach (long n in new long[] { 6, 12, 20, 30, 42, 56, 72, 90, 110, 132 }) Assert.AreEqual(NumberClass.Pronic, NumberKind.Classify(n), $"{n}");
        // 2 = 1 · 2 is pronic and prime: gold wins
        Assert.AreEqual(NumberClass.Prime, NumberKind.Classify(2));
        foreach (long n in new long[] { 8, 10, 16, 36, 91, 100, 360, 1001 }) Assert.AreEqual(NumberClass.Composite, NumberKind.Classify(n), $"{n}");
        Assert.AreEqual("2³ · 3² · 5", NumberKind.Shorthand(360));
        Assert.AreEqual("2<sup>10</sup>", NumberKind.Shorthand(1024));
        Assert.AreEqual("2¹⁰", NumberKind.Shorthand(1024, false));
        Assert.AreEqual("7 · 11 · 13", NumberKind.Shorthand(1001));
        CollectionAssert.AreEqual(new long[] { 1, 2, 3, 4, 6, 12 }, NumberKind.Divisors(12));
        Assert.AreEqual(2044L, NumberKind.Sigma3(12));
        Assert.AreEqual(55798470L, NumberKind.Sigma3(360));
        // E8's shells: 240 σ₃(n) vectors of norm 2n, every one in E8 (all integer or all half-integer, even sum)
        for (int n = 1; n <= 4; n++)
        {
            List<sbyte[]> v = E8Shells.Doubled(n);
            Assert.AreEqual(E8Shells.Count(n), v.Count, $"shell {n}");
            foreach (sbyte[] x in v)
            {
                int norm = 0, sum = 0;
                bool odd = (x[0] & 1) != 0;
                foreach (sbyte c in x)
                {
                    norm += c * c;
                    sum += c;
                    Assert.AreEqual(odd, (c & 1) != 0, "coordinates all integers or all half-integers");
                }

                Assert.AreEqual(8 * n, norm, "|2v|² = 8n");
                Assert.AreEqual(0, ((sum / 2) % 2 + 2) % 2, "the coordinates of v sum to an even number");
            }
        }

        Assert.AreEqual(240L * (1 + 8 + 64 + 512 + 4096), E8Shells.Count(16), "σ₃(16) = 1 + 2³ + 4³ + 8³ + 16³");
    }

    [UnityTest]
    [Timeout(1800000)]
    public IEnumerator DesktopGames()
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
            yield return Seconds(.5f);
            if (shots) director.Recorder.Snapshot(name + ".png");
            yield return null;
            yield return null;
        }

        // ---- Prime hunt ----
        lab.Enter("primehunt");
        PrimeHuntGame hunt = (PrimeHuntGame)lab.Find("primehunt");
        Assert.AreEqual(hunt, lab.Active);
        Assert.AreEqual(0, hunt.Round);
        StringAssert.Contains("Point at the primes", lab.Status);
        yield return Shot("GA1-prime-hunt-round-1");
        Assert.IsTrue(hunt.PickValue(2));
        Assert.IsTrue(hunt.PickValue(3));
        Assert.IsTrue(hunt.PickValue(9));
        StringAssert.Contains("3²", lab.Status);
        Assert.AreEqual(9, hunt.PrintNumber, "a miss shows the number's fingerprint");
        Assert.IsTrue(hunt.PickValue(5));
        Assert.IsTrue(hunt.PickValue(7));
        StringAssert.Contains("streak", lab.Status);
        yield return Shot("GA2-prime-hunt-a-miss-and-a-streak");
        hunt.GoToRound(3);
        Assert.IsTrue(hunt.PickValue(127));
        Assert.IsTrue(hunt.PickValue(143));
        StringAssert.Contains("11 · 13", lab.Status);
        lab.SetView(new LabView { Focus = ModelLab.Origin + LabView.Up * 8f + new Vector3(-6f, -14f, 0f), Distance = 95f, Yaw = -90f, Pitch = 58f, Fov = 50f }, true);
        yield return Shot("GA3-prime-hunt-impostors");
        hunt.Print.Unpack();
        yield return Seconds(hunt.Print.Steps * hunt.Print.StepSeconds + .6f);
        Assert.AreEqual(hunt.Print.Steps, hunt.Print.Phase, .01f, "unpacked");
        yield return Shot("GA4-143-unpacked-into-its-volume");

        // ---- Building blocks ----
        lab.Enter("blocks");
        BuildingBlocksGame blocks = (BuildingBlocksGame)lab.Find("blocks");
        blocks.AutoPlay = false;
        blocks.GoToExample(5);
        Assert.AreEqual(360, blocks.Rest);
        yield return Shot("GB1-blocks-360-as-a-volume");
        Assert.IsTrue(blocks.PickPrime(7));
        Assert.AreEqual(360, blocks.Rest, "7 does not divide 360");
        yield return Seconds(.9f);
        yield return Shot("GB2-blocks-7-leaves-a-remainder");
        blocks.FinishSplit();
        Assert.IsTrue(blocks.PickPrime(2));
        Assert.AreEqual(180, blocks.Rest);
        yield return Seconds(.6f);
        yield return Shot("GB3-blocks-split-by-2");
        foreach (long p in new long[] { 2, 2, 3, 3, 5 })
        {
            blocks.FinishSplit();
            Assert.IsTrue(blocks.PickPrime(p));
        }

        Assert.AreEqual(1, blocks.Rest);
        StringAssert.Contains("2³ · 3² · 5", lab.Status);
        blocks.FinishSplit();
        yield return Shot("GB4-blocks-360-found");
        blocks.Print.Jump(3.4f);
        lab.SetView(new LabView { Focus = blocks.Print.transform.position + LabView.Up * 12f + Vector3.right * 14f, Distance = 80f, Yaw = -72f, Pitch = 16f, Fov = 50f }, true);
        yield return Shot("GB5-blocks-360-multiplying");
        blocks.Print.Jump(blocks.Print.Steps);
        yield return Shot("GB6-blocks-360-full-volume");
        lab.ResetView(true);
        blocks.GoToExample(15);
        Assert.AreEqual(246, blocks.Rest);
        foreach (long p in new long[] { 2, 3 })
        {
            blocks.FinishSplit();
            Assert.IsTrue(blocks.PickPrime(p));
        }

        blocks.FinishSplit();
        blocks.PickRest();
        Assert.AreEqual(1, blocks.Rest, "41 is prime: the rest is a building block");

        // ---- E8 spheres ----
        lab.Enter("e8spheres");
        E8SpheresGame e8 = (E8SpheresGame)lab.Find("e8spheres");
        Assert.GreaterOrEqual(e8.Shells, 5, "at least shells 1–5 drawn");
        Debug.Log($"E8 spheres: {e8.Shells} shells drawn");
        yield return Shot("GC1-e8-spheres-which-are-prime");
        Assert.IsTrue(e8.PickValue(2, "shell"));
        Assert.IsTrue(e8.PickValue(3, "shell"));
        Assert.IsTrue(e8.PickValue(4, "shell"));
        StringAssert.Contains("σ<sub>3</sub>(4)", lab.Status);
        yield return Shot("GC2-e8-spheres-2-3-gold-4-deep-red");
        e8.GoToRound(1);
        Assert.IsTrue(e8.PickValue(6, "shell"));
        StringAssert.Contains("1 + 2³ + 3³ + 6³ = 252", lab.Status);
        yield return Shot("GC3-e8-shell-6-and-its-divisor-cubes");
        e8.GoToRound(2);
        Assert.IsTrue(e8.PickValue(91, "number"));
        Assert.IsTrue(e8.PickValue(97, "number"));
        Assert.IsTrue(e8.PickValue(121, "number"));
        StringAssert.Contains("11²", lab.Status);
        yield return Shot("GC4-e8-clock-impostors");

        // ---- Vortex hunt ----
        lab.Enter("vortex");
        VortexGame vortex = (VortexGame)lab.Find("vortex");
        Assert.GreaterOrEqual(vortex.VortexCount, 3, "zero 300 shows at least three clear vortices");
        Assert.IsTrue(vortex.PickValue(1, "vortex"));
        Assert.IsTrue(vortex.PickValue(2, "vortex"));
        StringAssert.Contains("vortex 2", lab.Status);
        yield return Shot("GD1-vortex-hunt-1-and-2-found");
        vortex.GoToRound(1);
        Assert.IsTrue(vortex.PickValue(3, "vortex"));
        Assert.IsTrue(vortex.PickValue(3, "vertex"));
        StringAssert.Contains("vertex 3 ↔ vortex 3", lab.Status);
        yield return Shot("GD2-vortex-3-joined-to-vertex-3");
        vortex.GoToRound(2);
        vortex.SteerTo(vortex.ZeroOfRound + 1e-4);
        StringAssert.Contains("Landed", lab.Status);
        yield return Shot("GD3-landed-on-a-zero");

        lab.Exit();
        yield return Seconds(.5f);
        Application.logMessageReceived -= OnLog;
        Assert.IsEmpty(errors, "errors were logged:\n" + string.Join("\n", errors));
    }

    [UnityTest]
    [Timeout(1800000)]
    public IEnumerator VRGames()
    {
        Application.logMessageReceived += OnLog;
        string outDir = Environment.GetEnvironmentVariable("ZETA_OUT");
        VRMode.RequestSimulation = true;
        SceneManager.LoadScene("zeta");
        yield return null;
        yield return null;
        yield return Until(() => VRMode.App != null && VRMode.App.Ready, 30f);
        VRApp app = VRMode.App;
        ModelLab lab = ModelLab.Instance;
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
        lab.Enter("primehunt");
        PrimeHuntGame hunt = (PrimeHuntGame)lab.Find("primehunt");
        yield return Seconds(2.5f);
        yield return Shot("GV1-models-page-with-the-games");

        // point at 11 from half a metre away and hold: the finger's ray picks it
        head.SetLook(0f, 0f);
        yield return Seconds(1f);
        int i11 = -1;
        for (int i = 0; i < hunt.PickCount; i++) if (hunt.PickLabel(i) == "11") i11 = i;
        Assert.GreaterOrEqual(i11, 0, "11 is a candidate of round 1");
        Vector3 world = hunt.PickPoint(i11);
        Vector3 local = rig.Offset.InverseTransformPoint(world);
        Vector3 headLocal = cam.transform.localPosition;
        Vector3 dir = (local - headLocal).normalized;
        app.Hands.ScriptedRight = true;
        app.Hands.ScriptedRightPinch = 0f;
        app.Hands.ScriptedRightTip = headLocal + dir * .45f + new Vector3(.05f, -.1f, 0f);
        app.Hands.ScriptedRightPoint = (local - app.Hands.ScriptedRightTip).normalized;
        yield return Seconds(.3f);
        Assert.AreEqual(i11, lab.Hover, "the finger's ray hovers 11");
        yield return Seconds(1.2f);
        Assert.AreEqual(11, hunt.PrintNumber, "held on 11, the ray picks it");
        yield return Shot("GV2-finger-ray-picks-11");

        // and a fingertip touch on 4 (round 1's candidates are 2 … 12)
        int i4 = -1;
        for (int i = 0; i < hunt.PickCount; i++) if (hunt.PickLabel(i) == "4") i4 = i;
        Vector3 local4 = rig.Offset.InverseTransformPoint(hunt.PickPoint(i4));
        app.Hands.ScriptedRightPoint = Vector3.zero;
        app.Hands.ScriptedRightTip = local4 + Vector3.up * .2f;
        yield return Seconds(.3f);
        app.Hands.ScriptedRightTip = local4;
        yield return Seconds(.4f);
        Assert.AreEqual(4, hunt.PrintNumber, "touching 4 picks it");
        app.Hands.ScriptedRightTip = local4 + Vector3.up * .2f;
        yield return Seconds(1f);
        yield return Shot("GV3-touched-4-a-prime-squared");
        app.Hands.ScriptedRight = false;

        lab.Enter("e8spheres");
        E8SpheresGame e8 = (E8SpheresGame)lab.Find("e8spheres");
        Debug.Log($"VR E8 spheres: {e8.Shells} shells");
        yield return Seconds(1.5f);
        yield return Shot("GV4-e8-spheres-in-vr");
        lab.Exit();
        yield return Seconds(.5f);
        Application.logMessageReceived -= OnLog;
        Assert.IsEmpty(errors, "errors were logged:\n" + string.Join("\n", errors));
    }

    /// <summary>One picture per fingerprint, marbles only, for the legibility study (Tools/jev_marbles.py).</summary>
    [UnityTest]
    [Timeout(1800000)]
    public IEnumerator Fingerprints()
    {
        string outDir = Environment.GetEnvironmentVariable("ZETA_FP_OUT");
        if (string.IsNullOrEmpty(outDir))
        {
            Assert.Ignore("ZETA_FP_OUT not set");
            yield break;
        }

        SceneManager.LoadScene("zeta");
        yield return null;
        yield return null;
        yield return Until(() => TourDirector.Instance != null && TourDirector.Instance.Captions != null, 20f);
        TourDirector director = TourDirector.Instance;
        if (director.Home != null) director.Home.Exit();
        if (director.Orbit != null) director.Orbit.End();
        director.Rig.Release();
        Camera cam = Camera.main;
        cam.GetComponent<CameraControl>().enabled = false;
        director.Captions.GetComponent<Canvas>().enabled = false;
        director.Annotations.HideAll();
        Directory.CreateDirectory(outDir);
        director.Recorder.Width = 1280;
        director.Recorder.Height = 720;
        director.Recorder.BeginOffscreen(outDir);
        string list = Environment.GetEnvironmentVariable("ZETA_FP_NUMBERS");
        List<long> numbers = new();
        foreach (string s in (string.IsNullOrEmpty(list) ? "12,18,30,49,56,60,72,77,90,91,100,105,121,128,143,169,180,210,221,360" : list).Split(','))
            numbers.Add(long.Parse(s));
        GameObject root = new("fingerprints");
        root.transform.position = new Vector3(70000f, 0f, 0f);
        foreach (long n in numbers)
        {
            MarbleFingerprint f = MarbleFingerprint.Create(root.transform, n, 1f, false);
            yield return null;
            // straight on and orthographic, framed on the marbles as drawn, with a margin all round
            Bounds b = f.LocalBounds;
            Vector3 centre = root.transform.TransformPoint(b.center);
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(b.size.y * .5f * 1.35f, b.size.x * .5f * 1.18f / (1280f / 720f));
            cam.transform.SetPositionAndRotation(centre + Vector3.back * 60f, Quaternion.identity);
            yield return null;
            yield return null;
            director.Recorder.Snapshot($"fp-{n}.png");
            yield return null;
            UnityEngine.Object.Destroy(f.gameObject);
            yield return null;
        }

        // one number's whole multiplication, with digits, framed on the unpacked volume: the shorthand, the exponents
        // opened, the block growing, the full volume
        long un = numbers.Contains(360) ? 360 : numbers[0];
        MarbleFingerprint u = MarbleFingerprint.Create(root.transform, un, 1f, true);
        u.Jump(u.Steps);
        yield return null;
        Bounds full = u.LocalBounds;
        u.Jump(0f);
        yield return null;
        Bounds b0 = u.LocalBounds;
        full.Encapsulate(b0);
        cam.orthographic = true;
        cam.orthographicSize = Mathf.Max(full.size.y * .5f * 1.15f, full.size.x * .5f * 1.12f / (1280f / 720f));
        cam.transform.SetPositionAndRotation(root.transform.TransformPoint(full.center) + new Vector3(-8f, 10f, -60f), Quaternion.identity);
        cam.transform.LookAt(root.transform.TransformPoint(full.center), Vector3.up);
        cam.transform.position = root.transform.TransformPoint(full.center) - cam.transform.forward * 60f;
        foreach (float ph in new[] { 0f, 1f, 2f, 3f, 4f, (float)u.Steps })
        {
            u.Jump(Mathf.Min(ph, u.Steps));
            yield return null;
            yield return null;
            director.Recorder.Snapshot($"unpack-{un}-{ph:0.0}.png");
            yield return null;
        }

        UnityEngine.Rendering.AsyncGPUReadback.WaitAllRequests();
        yield return Seconds(1f);
        director.Recorder.EndOffscreen();
    }

    /// <summary>Wait in game time: the recorder fixes the frame rate, and the games' own timers run on game time.</summary>
    static IEnumerator Seconds(float s)
    {
        float end = Time.time + s;
        int frames = 0;
        while (Time.time < end && frames++ < 100000) yield return null;
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
