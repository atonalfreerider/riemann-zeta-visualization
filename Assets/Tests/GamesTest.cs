using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// The games (Assets/Games): their arithmetic checked exactly; every game opened in the model lab and explored by scripted
/// picks, drags and a simulated touch screen on the desktop; the number spiral and zeta's chain in the desktop simulation
/// of the headset (a fingertip touch, the finger's ray, a pinch on the t rail); and the marble fingerprints drawn alone for
/// the legibility study.
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

        // ---- the number spiral: touch anything, pull a fingerprint apart by hand ----
        lab.Enter("primehunt");
        PrimeHuntGame spiral = (PrimeHuntGame)lab.Find("primehunt");
        Assert.AreEqual(spiral, lab.Active);
        StringAssert.Contains("Touch any number", lab.Status);
        StringAssert.DoesNotContain("points", lab.Status, "nothing is scored");
        Assert.IsTrue(spiral.PickValue(2));
        Assert.AreEqual(2, spiral.PrintNumber);
        StringAssert.Contains("prime", lab.Status);
        Assert.IsTrue(spiral.PickValue(91));
        Assert.AreEqual(91, spiral.Selected);
        StringAssert.Contains("7 · 13", lab.Status);
        yield return Shot("GA1-spiral-91-touched");
        // the fingerprint dragged to the right comes apart; to the left it packs again
        yield return Seconds(.2f);
        Assert.IsTrue(PrintCentre(spiral.Print, out Vector2 c), "the fingerprint is on the screen");
        Assert.IsTrue(spiral.DragBegin(c), "a drag that starts on the fingerprint takes hold of it");
        spiral.DragMove(c + new Vector2(600f, 0f), new Vector2(600f, 0f));
        spiral.DragEnd();
        Assert.Greater(spiral.Print.Phase, 1f, "pulled to the right, it unpacks");
        yield return Shot("GA2-91-pulled-apart");
        spiral.Print.Jump(spiral.Print.Steps);
        spiral.SetDeep(true);
        yield return Seconds(1.2f);
        yield return Shot("GA3-91-in-3d");
        spiral.SetDeep(false);
        spiral.LookAt(1);
        Assert.AreEqual(1, spiral.Look);
        yield return Shot("GA4-spiral-look-at-the-primes");
        spiral.LookAt(5);
        StringAssert.Contains("multiples of 91", lab.Status);
        spiral.LookAt(0);

        // ---- building blocks: split any number, nothing lost for a wrong prime ----
        lab.Enter("blocks");
        BuildingBlocksGame blocks = (BuildingBlocksGame)lab.Find("blocks");
        blocks.AutoPlay = false;
        blocks.GoToExample(5);
        Assert.AreEqual(360, blocks.Rest);
        StringAssert.DoesNotContain("points", lab.Status, "nothing is scored");
        yield return Shot("GB1-blocks-360-as-a-volume");
        Assert.IsTrue(blocks.PickPrime(7));
        Assert.AreEqual(360, blocks.Rest, "7 does not divide 360");
        StringAssert.Contains("leave 3 over", lab.Status);
        yield return Seconds(.3f);
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
        blocks.Print.Jump(6.4f);
        yield return Shot("GB5-blocks-360-multiplying");
        blocks.Print.Jump(blocks.Print.Steps);
        yield return Shot("GB6-blocks-360-full-volume");
        blocks.SetDeep(true);
        yield return Seconds(1.2f);
        yield return Shot("GB7-blocks-360-in-3d");
        blocks.SetDeep(false);
        // any number: 361 = 19² splits once, and the block that is left is prime
        blocks.GoToNumber(361);
        Assert.AreEqual(361, blocks.Target);
        Assert.IsTrue(blocks.PickPrime(19));
        blocks.FinishSplit();
        Assert.AreEqual(19, blocks.Rest);
        blocks.PickRest();
        Assert.AreEqual(1, blocks.Rest, "19 is prime: the rest is a building block");
        StringAssert.Contains("19²", lab.Status);

        // ---- E8's clock: the logarithmic stacks, a path down the staircase, the shells ----
        lab.Enter("e8spheres");
        E8SpheresGame e8 = (E8SpheresGame)lab.Find("e8spheres");
        Assert.IsFalse(e8.ShellsView);
        Assert.AreEqual(360, e8.Funnel.Selected, "the clock opens on 360's path back");
        yield return Seconds(5f);
        Assert.AreEqual(e8.Funnel.StepCount, e8.Funnel.StepsShown, "the path has stepped all the way down");
        StringAssert.Contains("÷2, ÷2, ÷2, ÷3, ÷3, ÷5", lab.Status);
        yield return Shot("GC1-e8-clock-360-down-the-staircase");
        Assert.IsTrue(e8.PickValue(343, "number"));
        Assert.AreEqual(343, e8.Funnel.Selected);
        StringAssert.Contains("7³", lab.Status);
        e8.StoryCommand("powers", new string[0]);
        yield return Seconds(2.6f);
        yield return Shot("GC2-e8-clock-343-and-the-powers");
        e8.StoryCommand("powers", new[] { "off" });
        e8.ShowShells(true);
        Assert.GreaterOrEqual(e8.Shells, 5, "at least shells 1–5 drawn");
        Assert.IsTrue(e8.PickValue(6, "shell"));
        StringAssert.Contains("1 + 2³ + 3³ + 6³ = 252", lab.Status);
        yield return Shot("GC3-e8-shell-6-and-its-divisor-cubes");
        e8.ShowShells(false);
        Assert.AreEqual(e8.Funnel.Selected > 0, true);

        // ---- zeta's chain: t moved by hand, a glide to the next zero, a vortex touched ----
        lab.Enter("vortex");
        VortexGame chainGame = (VortexGame)lab.Find("vortex");
        Assert.AreEqual(300, chainGame.Nearest);
        Assert.GreaterOrEqual(chainGame.VortexCount, 3, "zero 300 shows at least three clear vortices");
        Assert.IsTrue(chainGame.PickValue(3, "vortex"));
        Assert.AreEqual(3, chainGame.PrintNumber);
        yield return Shot("GD1-chain-at-zero-300-vortex-3");
        double t0 = chainGame.T;
        Assert.IsTrue(chainGame.DragBegin(new Vector2(960f, 300f)), "a drag off the fingerprint moves t");
        chainGame.DragMove(new Vector2(960f, 500f), new Vector2(0f, 200f));
        chainGame.DragEnd();
        Assert.AreEqual(t0 + .8, chainGame.T, 1e-6, "200 pixels up: t + 0.8");
        yield return Shot("GD2-chain-t-dragged-up");
        chainGame.StoryCommand("jump", new[] { "1" });
        yield return Seconds(1.3f);
        Assert.AreEqual(chainGame.ZeroAt(301), chainGame.T, 1e-6, "glided onto the next zero");
        StringAssert.Contains("on zero 301", lab.Status);
        yield return Shot("GD3-chain-on-zero-301");

        // a real touch screen: one finger dragged up the screen moves t; a tap on a vortex shows its number. A headless editor
        // never has the game view's focus, which would send pointer events to the editor instead of the game: ignore focus
        InputSettings.BackgroundBehavior background = InputSystem.settings.backgroundBehavior;
        InputSettings.EditorInputBehaviorInPlayMode editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        Touchscreen ts = InputSystem.AddDevice<Touchscreen>();
        try
        {
            double before = chainGame.T;
            // a stretch of the picture away from the panel (on the right) and from the fingerprint (on the left)
            Vector2 from = new(Screen.width * .5f, Screen.height * .25f);
            float rise = Mathf.Round(Screen.height * .2f);
            yield return TouchDrag(ts, from, from + new Vector2(0f, rise));
            Debug.Log($"touch: screen {Screen.width} × {Screen.height}, t {before:0.000} → {chainGame.T:0.000}");
            Assert.AreEqual(before + rise * .004, chainGame.T, .02, "one finger up the screen moves t up, .004 a pixel");
            chainGame.StoryCommand("zero", new[] { "300", "0.3" });
            yield return Seconds(.6f);
            int i2 = -1;
            for (int i = 0; i < chainGame.PickCount; i++) if (chainGame.PickLabel(i) == "vortex 2") i2 = i;
            Assert.GreaterOrEqual(i2, 0, "vortex 2 is on show at zero 300");
            Vector3 sp = Camera.main.WorldToScreenPoint(chainGame.PickPoint(i2));
            yield return TouchDrag(ts, new Vector2(sp.x, sp.y), new Vector2(sp.x, sp.y));
            Assert.AreEqual(2, chainGame.PrintNumber, "a tap on vortex 2 shows 2's fingerprint");
        }
        finally
        {
            InputSystem.RemoveDevice(ts);
            InputSystem.settings.backgroundBehavior = background;
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
        }

        lab.Exit();
        yield return Seconds(.5f);
        // the recorder hands the frame rate back now, not when the next test's scene load destroys it (after that test
        // has set a frame rate of its own)
        if (shots) director.Recorder.EndOffscreen();
        Application.logMessageReceived -= OnLog;
        Assert.IsEmpty(errors, "errors were logged:\n" + string.Join("\n", errors));
    }

    /// <summary>One finger down at a, moved to b over a few frames, lifted: a tap when a == b.</summary>
    static IEnumerator TouchDrag(Touchscreen ts, Vector2 a, Vector2 b)
    {
        InputSystem.QueueStateEvent(ts, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = a });
        yield return null;
        yield return null;
        for (int i = 1; i <= 6; i++)
        {
            InputSystem.QueueStateEvent(ts, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Moved, position = Vector2.Lerp(a, b, i / 6f) });
            yield return null;
        }

        InputSystem.QueueStateEvent(ts, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = b });
        yield return null;
        yield return null;
        yield return null;
    }

    /// <summary>The fingerprint's middle on the screen.</summary>
    static bool PrintCentre(MarbleFingerprint f, out Vector2 c)
    {
        c = default;
        if (f == null || Camera.main == null) return false;
        Vector3 sp = Camera.main.WorldToScreenPoint(f.transform.TransformPoint(f.LocalBounds.center));
        c = new Vector2(sp.x, sp.y);
        return sp.z > 0f;
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
        PrimeHuntGame spiral = (PrimeHuntGame)lab.Find("primehunt");
        yield return Seconds(2.5f);
        yield return Shot("GV1-models-page-with-the-games");

        // point at 11 from half a metre away and hold: the finger's ray picks it
        head.SetLook(0f, 0f);
        yield return Seconds(1f);
        int i11 = spiral.PickIndex(11);
        Assert.GreaterOrEqual(i11, 0, "11 is on the spiral");
        Vector3 world = spiral.PickPoint(i11);
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
        Assert.AreEqual(11, spiral.PrintNumber, "held on 11, the ray picks it");
        yield return Shot("GV2-finger-ray-picks-11");

        // and a fingertip touch on 4
        int i4 = spiral.PickIndex(4);
        Vector3 local4 = rig.Offset.InverseTransformPoint(spiral.PickPoint(i4));
        app.Hands.ScriptedRightPoint = Vector3.zero;
        app.Hands.ScriptedRightTip = local4 + Vector3.up * .2f;
        yield return Seconds(.3f);
        app.Hands.ScriptedRightTip = local4;
        yield return Seconds(.4f);
        Assert.AreEqual(4, spiral.PrintNumber, "touching 4 picks it");
        app.Hands.ScriptedRightTip = local4 + Vector3.up * .2f;
        yield return Seconds(1f);
        yield return Shot("GV3-touched-4-a-prime-squared");

        // zeta's chain: pinch the bead on the t rail and lift it, and t rises
        lab.Enter("vortex");
        VortexGame chainGame = (VortexGame)lab.Find("vortex");
        yield return Seconds(1.5f);
        double t0 = chainGame.T;
        Vector3 bead = rig.Offset.InverseTransformPoint(chainGame.BeadPoint);
        Vector3 upLocal = rig.Offset.InverseTransformDirection(LabView.Up).normalized;
        app.Hands.ScriptedRightPoint = Vector3.zero;
        app.Hands.ScriptedRightPinch = 0f;
        app.Hands.ScriptedRightTip = bead + Vector3.back * .05f;
        yield return Seconds(.3f);
        app.Hands.ScriptedRightTip = bead;
        yield return Seconds(.2f);
        app.Hands.ScriptedRightPinch = 1f;
        yield return Seconds(.3f);
        Assert.IsFalse(app.Grab.Holding, "a pinch on the bead does not grab the world");
        app.Hands.ScriptedRightTip = bead + upLocal * .05f;
        yield return Seconds(.5f);
        Assert.Greater(chainGame.T, t0 + .05, "lifting the pinched bead raises t");
        yield return Shot("GV4-chain-bead-lifted");
        app.Hands.ScriptedRightPinch = 0f;
        yield return Seconds(.3f);
        app.Hands.ScriptedRight = false;

        lab.Enter("e8spheres");
        E8SpheresGame e8 = (E8SpheresGame)lab.Find("e8spheres");
        Debug.Log($"VR E8: {e8.Shells} shells");
        yield return Seconds(1.5f);
        yield return Shot("GV5-e8-clock-in-vr");
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

        // a few in 3D: the same marbles as spiral sphere packs, turned a little
        foreach (long n in new long[] { 360, 49, 128, 1001 })
        {
            if (!numbers.Contains(n) && n != 1001) continue;
            MarbleFingerprint f = MarbleFingerprint.Create(root.transform, n, 1f, false, true);
            f.SpinDegreesPerSecond = 0f;
            f.SpinAngle = 30f;
            f.JumpDepth(true);
            yield return null;
            Bounds b = f.LocalBounds;
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(b.size.y * .5f * 1.35f, b.size.x * .5f * 1.18f / (1280f / 720f));
            cam.transform.SetPositionAndRotation(root.transform.TransformPoint(b.center) + Vector3.back * 60f, Quaternion.identity);
            yield return null;
            yield return null;
            director.Recorder.Snapshot($"fp-{n}-3d.png");
            yield return null;
            UnityEngine.Object.Destroy(f.gameObject);
            yield return null;
        }

        // one number's whole multiplication, with digits, framed on every stage of it: the shorthand, the exponents
        // opened, the first container poured in, each next one's copies blooming round a ring and winding into arms
        long un = numbers.Contains(360) ? 360 : numbers[0];
        foreach (bool deep in new[] { false, true })
        {
            MarbleFingerprint u = MarbleFingerprint.Create(root.transform, un, 1f, true, deep);
            u.SpinDegreesPerSecond = 0f;
            u.SpinAngle = 30f;
            u.JumpDepth(deep);
            Bounds full = new();
            for (float ph = 0f; ph <= u.Steps + .001f; ph += .125f)
            {
                u.Jump(ph);
                if (ph == 0f) full = u.LocalBounds;
                else full.Encapsulate(u.LocalBounds);
            }

            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(full.size.y * .5f * 1.08f, full.size.x * .5f * 1.06f / (1280f / 720f));
            cam.transform.SetPositionAndRotation(root.transform.TransformPoint(full.center) + Vector3.back * 60f, Quaternion.identity);
            float[] phases = deep ? new[] { 0f, 3f, 6.4f, (float)u.Steps } : new[] { 0f, .5f, 1f, 1.5f, 2f, 2.3f, 2.6f, 2.8f, 3f, 4f, 5f, 6f, 6.35f, 6.7f, (float)u.Steps };
            foreach (float ph in phases)
            {
                u.Jump(Mathf.Min(ph, u.Steps));
                yield return null;
                yield return null;
                director.Recorder.Snapshot($"unpack-{un}{(deep ? "-3d" : "")}-{ph:0.00}.png");
                yield return null;
            }

            UnityEngine.Object.Destroy(u.gameObject);
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
