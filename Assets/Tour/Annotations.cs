using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Meta.Numerics;
using TMPro;
using UnityEngine;

/// <summary>
/// Explanatory geometry layered on top of the visualization during the tour:
///   • the INPUT panel: the critical strip 0 ≤ Re(s) ≤ 1 drawn to the left of the scene,
///     with the critical line, the moving probe s = 1/2 + it, Hardy's Z(t) wiggle,
///     zero ticks (linked to the output-space markers) and the zero-free bands
///     (classical, 11/12 and the 2026 "quasi-RH" 7/8 claim)
///   • OUTPUT axes (Re ζ, Im ζ) and the vertical t axis (the input's imaginary axis)
///   • a hypothetical "rogue" zero off the critical line, drawn against the prime staircase
/// </summary>
public class Annotations : MonoBehaviour
{
    // Input panel: Re(s) ∈ [0,1] maps to x ∈ [PanelLeft, PanelLeft + PanelWidth]; Im(s) = t maps to y = t·yScale.
    public const float PanelLeft = -11f;
    public const float PanelWidth = 4f;
    public const float PanelCenterX = PanelLeft + PanelWidth * .5f;
    public const float PanelMaxT = 140f;
    const float ZScale = 0.35f;

    Main main;

    GameObject stripRoot, outputAxesRoot, imAxisRoot, zCurveRoot, band78Root, band18Root, classicRoot, band1112Root, gapRoot, ghostRoot;
    readonly List<GameObject> zeroTicks = new();
    LineRenderer zCurve;
    readonly List<Vector3> zPoints = new();
    double zCurveT = -1;
    GameObject probe;
    LineRenderer ghostCurve;
    TextBox ghostLabel;
    double ghostBeta = 0.9, ghostGamma = 14.134725;
    bool ghostOn;
    bool ghostAllZeros;
    MeshRenderer gapQuad;
    GameObject tipRoot, centerRoot, numberLineRoot, poleLabelRoot, zenoRoot, waveDemoRoot, incomingRoot;
    LineRenderer waveDemo, incomingWave;
    TextBox incomingLabel;
    Coroutine waveMorph;
    // demo wave state
    float demoGamma = 2.4f;
    float demoBend = 0f; // 0 = cos(γ·k·x), 1 = cos(γ·log x)
    float demoAmplitude = 11f;
    TextBox formulaLabel;
    GameObject dialRoot;
    LineRenderer dialNeedle;
    const float PlainK = 0.35f;
    // zeno build
    readonly List<GameObject> zenoArrows = new();
    readonly List<GameObject> zenoLabels = new();
    GameObject zenoWall;
    readonly Dictionary<Renderer, Coroutine> pulses = new();
    // primes intro grid
    GameObject primesRoot;
    readonly List<(int n, TextBox label, GameObject glow)> primeCells = new();
    // zeno footprints / wall slide
    readonly List<GameObject> zenoFootprints = new();
    readonly List<GameObject> zenoStepLabels = new();
    // sprouting axes
    readonly List<(LineRenderer line, Vector3[] full)> sproutLines = new();
    // joint angles
    GameObject anglesRoot;
    readonly List<LineRenderer> angleArcs = new();
    readonly List<LineRenderer> legExtensions = new();
    const int AngleCount = 20, AngleLabelCount = 8;
    Vector3[] angleSpiralBuffer = new Vector3[512];

    /// <summary>Alternating opaque and clear stretches (a gradient in fixed mode): a dotted line in one LineRenderer.</summary>
    static Gradient Dotted(Color c)
    {
        Gradient g = new() { mode = GradientMode.Fixed };
        GradientAlphaKey[] alpha = new GradientAlphaKey[8];
        for (int k = 0; k < 8; k++) alpha[k] = new GradientAlphaKey(k % 2 == 0 ? .95f : 0f, (k + 1) / 8f);
        g.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) }, alpha);
        return g;
    }
    readonly List<TextBox> angleLabels = new();
    TextBox angleFormula;
    bool showAngles;
    // dial strip
    GameObject dialStripRoot;
    readonly List<(GameObject root, LineRenderer needle, Vector3 center)> stripDials = new();
    TextBox dialMarkLabel;
    // prime arrows
    GameObject primeArrowsRoot;
    readonly List<TextBox> numberLabels = new();
    // root formula
    TextBox rootFormula;
    GameObject rootFormulaRoot;
    TextBox ghostFormula;
    public SacksSpiral Sacks { get; private set; }
    public StaircaseLab Lab { get; private set; }
    // zeno zoom: the camera follows the newest footprint and halves its distance each step
    public Vector3 ZenoFocus { get; private set; } = new(1f, 0f, 1.15f);
    public float ZenoZoom { get; private set; } = 9.5f;
    // footprints along the √n chain
    GameObject chainFeetRoot;
    readonly List<GameObject> chainFeet = new();
    bool showChainFeet;
    // waves: colour per collision, difference lines, growing view
    readonly List<LineRenderer> diffLines = new();
    GameObject diffRoot;
    public float WaveViewMax { get; private set; } = 16f;
    TextBox squaresLabel;
    GameObject primePowersRoot;
    readonly List<TextBox> primePowerLabels = new();
    readonly List<LineRenderer> primePowerTicks = new();
    // floor grid (sprouts with the axes)
    GameObject floorGridRoot;
    // logarithm sequence panel
    GameObject logSeqRoot;
    readonly List<LineRenderer> logBars = new();
    readonly List<TextBox> logBarLabels = new();
    TextBox logSeqTitle;
    bool showLogSeq;
    // cached buffers / throttles
    readonly Vector3[] ringBuffer = new Vector3[40];
    readonly Vector3[] pair = new Vector3[2];
    float nextReadoutTime;
    string lastDialText = "", lastAngleText = "";
    Vector3[] masterBuffer;
    readonly Vector3[] soleBuffer = new Vector3[14];
    readonly Vector3[] arcBuffer = new Vector3[13];
    GameObject tipSphere, centerSphere;
    TextBox dialLabel;
    LineRenderer centerRing;

    public static Annotations Create(Main main)
    {
        GameObject go = new("Annotations");
        Annotations a = go.AddComponent<Annotations>();
        a.main = main;
        a.Build();
        a.Lab = StaircaseLab.Create(main);
        a.Lab.transform.SetParent(go.transform, false);
        main.ZeroPassed += a.OnZeroPassed;
        main.AnimationReset += a.OnAnimationReset;
        return a;
    }

    void OnAnimationReset()
    {
        foreach (GameObject tick in zeroTicks) Destroy(tick);
        zeroTicks.Clear();
        zPoints.Clear();
        zCurveT = -1;
        if (zCurve != null) zCurve.positionCount = 0;
    }

    public Vector3 StripPoint(double sigma, double t)
    {
        return new Vector3(PanelLeft + (float)sigma * PanelWidth, (float)t * Graph.yScale, 0);
    }

    // ---------------------------------------------------------------------------------------

    void Build()
    {
        float height = PanelMaxT * Graph.yScale;
        float midY = height * .5f;

        // ---- input panel -------------------------------------------------------------------
        stripRoot = new GameObject("InputPanel");
        stripRoot.transform.SetParent(transform, false);

        MeshRenderer strip = Factory.NewQuad("CriticalStrip", new Vector2(PanelWidth, height), new Color(.5f, .55f, .8f, .08f));
        strip.transform.SetParent(stripRoot.transform, false);
        strip.transform.position = new Vector3(PanelCenterX, midY, 0.02f);

        LineRenderer critical = Factory.NewLine("CriticalLine", .06f, Factory.Palette.CriticalLine, Factory.GlowIntensity);
        critical.transform.SetParent(stripRoot.transform, false);
        critical.positionCount = 2;
        critical.SetPositions(new[] { StripPoint(0.5, 0), StripPoint(0.5, PanelMaxT) });

        foreach (double edge in new[] { 0d, 1d })
        {
            LineRenderer e = Factory.NewLine("StripEdge", .03f, new Color(.6f, .6f, .8f, .6f));
            e.transform.SetParent(stripRoot.transform, false);
            e.positionCount = 2;
            e.SetPositions(new[] { StripPoint(edge, 0), StripPoint(edge, PanelMaxT) });
        }

        // Re(s) axis with ticks
        LineRenderer reAxis = Factory.NewLine("Re(s) axis", .04f, Factory.Palette.ReAxis, Factory.SoftGlowIntensity);
        reAxis.transform.SetParent(stripRoot.transform, false);
        reAxis.positionCount = 2;
        reAxis.SetPositions(new[] { StripPoint(-0.08, -2.5) , StripPoint(1.12, -2.5) });
        Arrowhead(stripRoot.transform, StripPoint(1.12, -2.5), Vector3.right, Factory.Palette.ReAxis, .25f);
        Label(stripRoot.transform, "Re(s)  — real part of the input", StripPoint(0.5, -5.5), 2.2f, Factory.Palette.ReAxis, TextAlignmentOptions.Center);
        foreach ((double sigma, string text) in new[] { (0d, "0"), (0.5d, "½"), (0.875d, "7/8"), (1d, "1") })
        {
            LineRenderer tick = Factory.NewLine("tick", .03f, Factory.Palette.ReAxis);
            tick.transform.SetParent(stripRoot.transform, false);
            tick.positionCount = 2;
            tick.SetPositions(new[] { StripPoint(sigma, -2.5) + Vector3.down * .25f, StripPoint(sigma, -2.5) + Vector3.up * .25f });
            Label(stripRoot.transform, text, StripPoint(sigma, -2.5) + Vector3.down * 1.1f, 2.4f, Factory.Palette.ReAxis, TextAlignmentOptions.Center);
        }

        // Im(s) axis (vertical) along the left edge of the panel
        LineRenderer imAxis = Factory.NewLine("Im(s) axis", .04f, Factory.Palette.ImAxis, Factory.SoftGlowIntensity);
        imAxis.transform.SetParent(stripRoot.transform, false);
        imAxis.positionCount = 2;
        imAxis.SetPositions(new[] { StripPoint(-0.08, -2.5), StripPoint(-0.08, PanelMaxT) });
        Arrowhead(stripRoot.transform, StripPoint(-0.08, PanelMaxT), Vector3.up, Factory.Palette.ImAxis, .25f);
        for (int t = 10; t < PanelMaxT; t += 10)
        {
            LineRenderer tick = Factory.NewLine("tick", .03f, Factory.Palette.ImAxis);
            tick.transform.SetParent(stripRoot.transform, false);
            tick.positionCount = 2;
            tick.SetPositions(new[] { StripPoint(-0.08, t) + Vector3.left * .2f, StripPoint(-0.08, t) + Vector3.right * .2f });
            Label(stripRoot.transform, "t = " + t, StripPoint(-0.08, t) + Vector3.left * .5f, 1.8f, Factory.Palette.ImAxis, TextAlignmentOptions.Right);
        }

        Label(stripRoot.transform, "Im(s) = t\nimaginary part\nof the input", StripPoint(-0.08, 4) + Vector3.left * 1.2f, 2.0f, Factory.Palette.ImAxis, TextAlignmentOptions.Right);
        Label(stripRoot.transform, "the one-half line", StripPoint(0.5, -1.2), 2.2f, Factory.Palette.CriticalLine, TextAlignmentOptions.Center);
        Label(stripRoot.transform, "INPUT\ns = ½ + i·t", StripPoint(1.14, 46), 2.6f, Color.white, TextAlignmentOptions.Left);

        probe = Factory.NewSphere("Probe", .28f, Color.white, Factory.GlowIntensity);
        probe.transform.SetParent(stripRoot.transform, false);

        // Hardy Z(t) wiggle along the critical line
        zCurveRoot = new GameObject("HardyZ");
        zCurveRoot.transform.SetParent(stripRoot.transform, false);
        zCurve = Factory.NewLine("Z(t)", .05f, new Color(.6f, .95f, 1f), Factory.SoftGlowIntensity);
        zCurve.transform.SetParent(zCurveRoot.transform, false);
        Label(zCurveRoot.transform, "Z(t) = e^{iθ(t)} ζ(½ + it)\nreal valued — crosses the line exactly at the zeros", StripPoint(1.12, 1.5), 1.6f, new Color(.6f, .95f, 1f), TextAlignmentOptions.Left);
        zCurveRoot.SetActive(false);

        // zero-free bands
        band78Root = Band("ZeroFree_7_8", 0.875, 1.0, height, "shrink rate > 7/8\nno collisions\n(OpenAI, 2026 —\nLean-checked claim)", TextAlignmentOptions.Left, 1.14);
        band18Root = Band("ZeroFree_1_8", 0.0, 0.125, height, "< 1/8\nnone either\n(by symmetry)", TextAlignmentOptions.Right, -0.14);
        band1112Root = DashedVertical("Line_11_12", 11d / 12d, height, "11/12\ncompanion\nproof", TextAlignmentOptions.Left, 30);
        classicRoot = ClassicRegion(height);
        gapRoot = new GameObject("OpenGap");
        gapRoot.transform.SetParent(stripRoot.transform, false);
        gapQuad = Factory.NewQuad("Gap", new Vector2(PanelWidth * .375f, height), new Color(1f, .5f, .2f, .18f));
        gapQuad.transform.SetParent(gapRoot.transform, false);
        gapQuad.transform.position = new Vector3(PanelLeft + PanelWidth * (0.5f + 0.375f * .5f), midY, 0.01f);
        Label(gapRoot.transform, "still open\n½ < rate ≤ 7/8\nthe Riemann\nHypothesis\nlives here", StripPoint(0.6875, 16), 2.2f, new Color(1f, .7f, .4f), TextAlignmentOptions.Center);
        band78Root.SetActive(false);
        band18Root.SetActive(false);
        band1112Root.SetActive(false);
        classicRoot.SetActive(false);
        gapRoot.SetActive(false);
        stripRoot.SetActive(false);

        // ---- output axes -------------------------------------------------------------------
        outputAxesRoot = new GameObject("OutputAxes");
        outputAxesRoot.transform.SetParent(transform, false);
        Axis(outputAxesRoot.transform, Vector3.zero, Vector3.right * 4.5f, Factory.Palette.ReAxis, "Re ζ(s)", Vector3.right * 5.2f);
        Axis(outputAxesRoot.transform, Vector3.zero, Vector3.forward * 4.5f, Factory.Palette.ImAxis, "Im ζ(s)", Vector3.forward * 5.2f + Vector3.up * .6f);
        Label(outputAxesRoot.transform, "OUTPUT   ζ(s)  — drawn flat at height t", new Vector3(2.2f, -0.9f, 0), 2.2f, Color.white, TextAlignmentOptions.Center);
        floorGridRoot = new GameObject("FloorGrid");
        floorGridRoot.transform.SetParent(outputAxesRoot.transform, false);
        for (int i = -4; i <= 6; i++)
        {
            LineRenderer gx = Factory.NewLine("floor x", i == 0 ? .03f : .018f, new Color(.3f, .34f, .42f));
            gx.transform.SetParent(floorGridRoot.transform, false);
            gx.positionCount = 2;
            gx.SetPositions(new[] { new Vector3(i, -0.005f, -4), new Vector3(i, -0.005f, 6) });
            sproutLines.Add((gx, new[] { new Vector3(i, -0.005f, -4), new Vector3(i, -0.005f, 6) }));
            LineRenderer gz = Factory.NewLine("floor z", i == 0 ? .03f : .018f, new Color(.3f, .34f, .42f));
            gz.transform.SetParent(floorGridRoot.transform, false);
            gz.positionCount = 2;
            gz.SetPositions(new[] { new Vector3(-4, -0.005f, i), new Vector3(6, -0.005f, i) });
            sproutLines.Add((gz, new[] { new Vector3(-4, -0.005f, i), new Vector3(6, -0.005f, i) }));
        }

        outputAxesRoot.SetActive(false);

        imAxisRoot = new GameObject("ImaginaryAxisHighlight");
        imAxisRoot.transform.SetParent(transform, false);
        LineRenderer tAxis = Factory.NewLine("t axis", .035f, Factory.Palette.ImAxis, Factory.SoftGlowIntensity);
        tAxis.transform.SetParent(imAxisRoot.transform, false);
        tAxis.positionCount = 2;
        tAxis.SetPositions(new[] { Vector3.zero, Vector3.up * (PanelMaxT * Graph.yScale) });
        sproutLines.Add((tAxis, new[] { Vector3.zero, Vector3.up * (PanelMaxT * Graph.yScale) }));
        Arrowhead(imAxisRoot.transform, Vector3.up * (PanelMaxT * Graph.yScale), Vector3.up, Factory.Palette.ImAxis, .4f);
        for (int t = 10; t < PanelMaxT; t += 10)
        {
            TextBox tick = Label(imAxisRoot.transform, "t = " + t, new Vector3(.45f, t * Graph.yScale, 0), 1.3f, Factory.Palette.ImAxis, TextAlignmentOptions.Left);
            tick.gameObject.AddComponent<Billboard>();
        }

        // ---- number line under the waves / spikes --------------------------------------------
        numberLineRoot = new GameObject("NumberLine");
        numberLineRoot.transform.SetParent(transform, false);
        LineRenderer baseline = Factory.NewLine("baseline", .04f, new Color(.6f, .6f, .65f));
        baseline.transform.SetParent(numberLineRoot.transform, false);
        baseline.positionCount = 2;
        baseline.SetPositions(new[] { new Vector3(0, 0, 0.05f), new Vector3(42, 0, 0.05f) });
        for (int n = 1; n <= 41; n++)
        {
            bool prime = n is 2 or 3 or 5 or 7 or 11 or 13 or 17 or 19 or 23 or 29 or 31 or 37 or 41;
            bool primePower = n is 4 or 8 or 9 or 16 or 25 or 27 or 32;
            LineRenderer tick = Factory.NewLine("tick", .03f, new Color(.6f, .6f, .65f));
            tick.transform.SetParent(numberLineRoot.transform, false);
            tick.positionCount = 2;
            tick.SetPositions(new[] { new Vector3(n, -.25f, 0.05f), new Vector3(n, .25f, 0.05f) });
            Color c = prime ? Factory.Palette.Dagger : primePower ? new Color(1f, .6f, .25f) : new Color(.55f, .55f, .6f);
            TextBox label = Label(numberLineRoot.transform, n.ToString(), new Vector3(n, -1.7f, 0), prime ? 4.2f : primePower ? 3.4f : 2.8f, c, TextAlignmentOptions.Center);
            label.gameObject.AddComponent<Billboard>();
            label.name = "number: " + n;
        }

        numberLineRoot.SetActive(false);

        poleLabelRoot = new GameObject("PoleLabel");
        poleLabelRoot.transform.SetParent(transform, false);
        TextBox axisLabel = Label(poleLabelRoot.transform, "t  — the dial\n(the imaginary part of s)", new Vector3(.8f, 1.6f, 0), 2.0f, Factory.Palette.ImAxis, TextAlignmentOptions.Left);
        axisLabel.gameObject.AddComponent<Billboard>();
        poleLabelRoot.SetActive(false);
        imAxisRoot.SetActive(false);

        // ---- Zeno's halving chain, drawn beside the real chain for comparison -------------------
        zenoRoot = new GameObject("ZenoChain");
        zenoRoot.transform.SetParent(transform, false);
        const float zenoZ = 1.15f;
        float xPos = 0f;
        string[] fractions = { "1", "½", "¼", "⅛", "1/16", "1/32", "1/64", "1/128" };
        for (int i = 0; i < 8; i++)
        {
            float length = Mathf.Pow(0.5f, i);
            Vector3 from = new(xPos, 0, zenoZ);
            Vector3 to = new(xPos + length, 0, zenoZ);
            Color c = LimitCurve.ChainColor(i / 7f);
            GameObject arrow = new GameObject("zeno arrow " + i);
            arrow.transform.SetParent(zenoRoot.transform, false);
            LineRenderer seg = Factory.NewLine("zeno " + i, .06f * length, c, Factory.SoftGlowIntensity);
            seg.transform.SetParent(arrow.transform, false);
            seg.positionCount = 2;
            seg.numCapVertices = 4;
            seg.SetPositions(new[] { from, to });
            float size = Mathf.Clamp(length * .28f, .03f, .13f);
            LineRenderer head = Factory.NewLine("zeno head", length * .05f, c, Factory.SoftGlowIntensity);
            head.transform.SetParent(arrow.transform, false);
            head.positionCount = 3;
            head.SetPositions(new[] { to - Vector3.right * size + Vector3.forward * size * .55f, to, to - Vector3.right * size - Vector3.forward * size * .55f });
            zenoArrows.Add(arrow);
            TextBox label = Label(zenoRoot.transform, fractions[i], (from + to) * .5f + Vector3.forward * .4f * length, 1.6f * length, new Color(1f, 1f, 1f, .85f), TextAlignmentOptions.Center);
            label.gameObject.AddComponent<Billboard>();
            zenoLabels.Add(label.gameObject);

            xPos += length;
        }

        // footprints at the end of each step, alternating left/right foot, with "step n" labels
        float fx = 0f;
        for (int i = 0; i < 6; i++)
        {
            float length = Mathf.Pow(0.5f, i);
            fx += length;
            float side = (i % 2 == 0 ? .28f : -.28f) * length;
            GameObject foot = new GameObject("footprint " + i);
            foot.transform.SetParent(zenoRoot.transform, false);
            float scale = length * .34f;
            LineRenderer sole = Factory.NewLine("sole", scale * .35f, new Color(1f, .95f, .8f), Factory.SoftGlowIntensity);
            sole.transform.SetParent(foot.transform, false);
            sole.loop = true;
            sole.positionCount = 14;
            Vector3[] sp = new Vector3[14];
            for (int k = 0; k < 14; k++)
            {
                float a = k / 14f * Mathf.PI * 2f;
                sp[k] = new Vector3(fx - scale * .3f + Mathf.Cos(a) * scale * .5f, 0.01f, zenoZ + side + Mathf.Sin(a) * scale * .28f);
            }

            sole.SetPositions(sp);
            LineRenderer toes = Factory.NewLine("toes", scale * .3f, new Color(1f, .95f, .8f), Factory.SoftGlowIntensity);
            toes.transform.SetParent(foot.transform, false);
            toes.positionCount = 2;
            toes.SetPositions(new[] { new Vector3(fx + scale * .3f, 0.01f, zenoZ + side - scale * .2f), new Vector3(fx + scale * .3f, 0.01f, zenoZ + side + scale * .2f) });
            zenoFootprints.Add(foot);
            TextBox step = Label(zenoRoot.transform, "step " + (i + 1), new Vector3(fx - length * .5f, 0, zenoZ - .95f * length), 1.3f * length, new Color(.8f, .8f, .9f), TextAlignmentOptions.Center);
            step.gameObject.AddComponent<Billboard>();
            zenoStepLabels.Add(step.gameObject);
        }

        zenoWall = new GameObject("zeno wall");
        zenoWall.transform.SetParent(zenoRoot.transform, false);
        LineRenderer wall = Factory.NewLine("zeno wall line", .04f, Factory.Palette.Dagger, Factory.GlowIntensity);
        wall.transform.SetParent(zenoWall.transform, false);
        wall.positionCount = 2;
        wall.SetPositions(new[] { new Vector3(2f, 0, zenoZ - .45f), new Vector3(2f, 0, zenoZ + .45f) });
        TextBox wallLabel = Label(zenoWall.transform, "stops at 2", new Vector3(2f, 0, zenoZ + .95f), 1.6f, Factory.Palette.Dagger, TextAlignmentOptions.Center);
        wallLabel.gameObject.AddComponent<Billboard>();
        TextBox zenoTitle = Label(zenoRoot.transform, "Zeno: halve every step", new Vector3(-0.3f, 0, zenoZ), 1.5f, new Color(.8f, .8f, .9f), TextAlignmentOptions.Right);
        zenoTitle.gameObject.AddComponent<Billboard>();
        TextBox oursTitle = Label(zenoRoot.transform, "ours: 1 / √n", new Vector3(-0.3f, 0, 0), 1.5f, Factory.Palette.Zero, TextAlignmentOptions.Right);
        oursTitle.gameObject.AddComponent<Billboard>();
        zenoRoot.SetActive(false);

        // ---- demo wave (plain cosine that bends into the first collision's wave) -----------------
        waveDemoRoot = new GameObject("WaveDemo");
        waveDemoRoot.transform.SetParent(transform, false);
        waveDemo = Factory.NewLine("demo wave", .05f, Factory.Palette.Cosine, Factory.SoftGlowIntensity);
        waveDemo.transform.SetParent(waveDemoRoot.transform, false);
        waveDemo.positionCount = Main.NumPrimeCurvePoints;
        formulaLabel = Label(waveDemoRoot.transform, "", new Vector3(8.5f, 12.8f, 0), 6.5f, Color.white, TextAlignmentOptions.Center);
        formulaLabel.gameObject.AddComponent<Billboard>();
        waveDemoRoot.SetActive(false);

        // the dial that sets γ
        dialRoot = new GameObject("WaveDial");
        dialRoot.transform.SetParent(transform, false);
        Vector3 dialCenter = new(13.5f, -9f, 0);
        const float dialRadius = 2.2f;
        LineRenderer ring = Factory.NewLine("dial ring", .05f, Factory.Palette.ImAxis, Factory.SoftGlowIntensity);
        ring.transform.SetParent(dialRoot.transform, false);
        ring.loop = true;
        ring.positionCount = 48;
        Vector3[] ringPts = new Vector3[48];
        for (int i = 0; i < 48; i++)
        {
            float a = i / 48f * Mathf.PI * 2f;
            ringPts[i] = dialCenter + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * dialRadius;
        }

        ring.SetPositions(ringPts);
        for (int i = 0; i <= 10; i++)
        {
            float a = DialAngle(i / 10f * 15f);
            Vector3 dir = new(Mathf.Cos(a), Mathf.Sin(a), 0);
            LineRenderer tick = Factory.NewLine("dial tick", .03f, Factory.Palette.ImAxis);
            tick.transform.SetParent(dialRoot.transform, false);
            tick.positionCount = 2;
            tick.SetPositions(new[] { dialCenter + dir * dialRadius * .82f, dialCenter + dir * dialRadius * .97f });
        }

        dialNeedle = Factory.NewLine("dial needle", .07f, Factory.Palette.Zero, Factory.GlowIntensity);
        dialNeedle.transform.SetParent(dialRoot.transform, false);
        dialNeedle.positionCount = 2;
        dialNeedle.numCapVertices = 4;
        TextBox dialTitle = Label(dialRoot.transform, "γ", dialCenter + Vector3.down * (dialRadius + 1.1f), 3.2f, Factory.Palette.ImAxis, TextAlignmentOptions.Center);
        dialTitle.gameObject.AddComponent<Billboard>();
        dialRoot.SetActive(false);

        incomingRoot = new GameObject("IncomingWave");
        incomingRoot.transform.SetParent(transform, false);
        incomingWave = Factory.NewLine("incoming wave", .045f, new Color(1f, .35f, .85f), Factory.GlowIntensity);
        incomingWave.transform.SetParent(incomingRoot.transform, false);
        incomingWave.positionCount = Main.NumPrimeCurvePoints;
        incomingLabel = Label(incomingRoot.transform, "", new Vector3(12f, 9f, 0), 3.2f, new Color(1f, .35f, .85f), TextAlignmentOptions.Center);
        incomingLabel.gameObject.AddComponent<Billboard>();
        incomingRoot.SetActive(false);

        // ---- spiral tip and the centre it must hit ---------------------------------------------
        tipRoot = new GameObject("Tip");
        tipRoot.transform.SetParent(transform, false);
        tipSphere = Factory.NewSphere("tip", .16f, Color.white, Factory.GlowIntensity);
        tipSphere.transform.SetParent(tipRoot.transform, false);
        tipRoot.SetActive(false);

        centerRoot = new GameObject("Centre");
        centerRoot.transform.SetParent(transform, false);
        centerSphere = Factory.NewSphere("centre", .12f, Factory.Palette.Zero, Factory.SoftGlowIntensity);
        centerSphere.transform.SetParent(centerRoot.transform, false);
        centerRing = Factory.NewLine("centre ring", .025f, Factory.Palette.Zero, Factory.SoftGlowIntensity);
        centerRing.transform.SetParent(centerRoot.transform, false);
        centerRing.loop = true;
        const int ringSegments = 40;
        centerRing.positionCount = ringSegments;
        dialLabel = Label(centerRoot.transform, "", Vector3.zero, 1.0f, Factory.Palette.ImAxis, TextAlignmentOptions.Right);
        dialLabel.gameObject.AddComponent<Billboard>();
        centerRoot.SetActive(false);

        // ---- joint angle arcs ------------------------------------------------------------------
        anglesRoot = new GameObject("JointAngles");
        anglesRoot.transform.SetParent(transform, false);
        for (int i = 0; i < AngleCount; i++)
        {
            LineRenderer arc = Factory.NewLine("arc", .025f, new Color(1f, .5f, .9f), Factory.GlowIntensity);
            arc.transform.SetParent(anglesRoot.transform, false);
            arc.numCornerVertices = 1;
            angleArcs.Add(arc);
            // the previous leg carried on past the joint, dotted: the purple turn is measured from it to the next leg
            LineRenderer leg = Factory.NewLine("leg extension", .014f, Color.white, Factory.NoGlow);
            leg.transform.SetParent(anglesRoot.transform, false);
            leg.positionCount = 2;
            leg.textureMode = LineTextureMode.Stretch;
            leg.colorGradient = Dotted(new Color(.85f, .85f, .95f));
            legExtensions.Add(leg);
            TextBox l = Label(anglesRoot.transform, "", Vector3.zero, 0.85f, new Color(1f, .6f, .95f), TextAlignmentOptions.Center);
            l.gameObject.AddComponent<Billboard>();
            l.gameObject.SetActive(false);
            angleLabels.Add(l);
        }

        angleFormula = Label(anglesRoot.transform, "", Vector3.zero, 0.85f, new Color(1f, .6f, .95f), TextAlignmentOptions.Left);
        angleFormula.gameObject.AddComponent<Billboard>();
        anglesRoot.SetActive(false);

        // ---- the logarithm sequence: bars log(n/(n−1)) for n = 2..11, lying on the floor beside the spiral --------
        logSeqRoot = new GameObject("LogSequence");
        logSeqRoot.transform.SetParent(transform, false);
        for (int n = 2; n <= 11; n++)
        {
            LineRenderer bar = Factory.NewLine("log bar", .18f, new Color(1f, .6f, .95f), Factory.SoftGlowIntensity);
            bar.transform.SetParent(logSeqRoot.transform, false);
            bar.positionCount = 2;
            logBars.Add(bar);
            TextBox l = Label(logSeqRoot.transform, "", Vector3.zero, 0.9f, new Color(1f, .75f, 1f), TextAlignmentOptions.Center);
            l.gameObject.AddComponent<Billboard>();
            logBarLabels.Add(l);
        }

        logSeqTitle = Label(logSeqRoot.transform, "", Vector3.zero, 1.0f, new Color(1f, .6f, .95f), TextAlignmentOptions.Center);
        logSeqTitle.gameObject.AddComponent<Billboard>();
        logSeqRoot.SetActive(false);

        // ---- dial strip (one small dial per collision added to the chorus) ----------------------
        dialStripRoot = new GameObject("DialStrip");
        dialStripRoot.transform.SetParent(transform, false);
        dialStripRoot.SetActive(false);

        // the main dial: mark the first collision height
        float ma = DialAngle(14.134725f);
        Vector3 mdir = new(Mathf.Cos(ma), Mathf.Sin(ma), 0);
        Vector3 dc = new(13.5f, -9f, 0);
        LineRenderer mark = Factory.NewLine("dial mark", .06f, Factory.Palette.Zero, Factory.GlowIntensity);
        mark.transform.SetParent(dialRoot.transform, false);
        mark.positionCount = 2;
        mark.SetPositions(new[] { dc + mdir * 1.75f, dc + mdir * 2.6f });
        dialMarkLabel = Label(dialRoot.transform, "14.13", dc + mdir * 3.4f, 1.6f, Factory.Palette.Zero, TextAlignmentOptions.Center);
        dialMarkLabel.gameObject.AddComponent<Billboard>();

        // ---- prime arrows above the spikes ------------------------------------------------------
        primeArrowsRoot = new GameObject("PrimeArrows");
        primeArrowsRoot.transform.SetParent(transform, false);
        foreach (int p in new[] { 2, 3, 5, 7, 11, 13 })
        {
            LineRenderer shaft = Factory.NewLine("prime arrow", .06f, Factory.Palette.Dagger, Factory.GlowIntensity);
            shaft.transform.SetParent(primeArrowsRoot.transform, false);
            shaft.positionCount = 2;
            shaft.SetPositions(new[] { new Vector3(p, 8.4f, -0.05f), new Vector3(p, 6.4f, -0.05f) });
            Arrowhead(primeArrowsRoot.transform, new Vector3(p, 6.4f, -0.05f), Vector3.down, Factory.Palette.Dagger, .45f);
        }

        primeArrowsRoot.SetActive(false);

        // ---- formulas for the key chapter -------------------------------------------------------
        rootFormulaRoot = new GameObject("RootFormula");
        rootFormulaRoot.transform.SetParent(transform, false);
        rootFormula = Label(rootFormulaRoot.transform, "", Vector3.zero, 1.25f, Color.white, TextAlignmentOptions.Center);
        rootFormula.gameObject.AddComponent<Billboard>();
        rootFormulaRoot.SetActive(false);

        Sacks = SacksSpiral.Create();
        Sacks.transform.SetParent(transform, false);

        // ---- footprints along the √n chain -------------------------------------------------------
        chainFeetRoot = new GameObject("ChainFeet");
        chainFeetRoot.transform.SetParent(transform, false);
        chainFeetRoot.SetActive(false);

        // ---- difference lines between the master wave and an incoming wave -----------------------
        diffRoot = new GameObject("WaveDifferences");
        diffRoot.transform.SetParent(transform, false);
        for (int i = 0; i < 70; i++)
        {
            LineRenderer d = Factory.NewLine("diff", .035f, Color.white, Factory.SoftGlowIntensity);
            d.transform.SetParent(diffRoot.transform, false);
            d.positionCount = 2;
            diffLines.Add(d);
        }

        diffRoot.SetActive(false);

        squaresLabel = Label(numberLineRoot.transform, "squares 4, 9, 16, 25 and cubes 8, 27:\nfaint spikes at first, muted as the waves pile up", new Vector3(16f, -7.5f, 0), 4.6f, new Color(1f, .6f, .25f), TextAlignmentOptions.Center);
        squaresLabel.gameObject.AddComponent<Billboard>();
        squaresLabel.gameObject.SetActive(false);

        primePowersRoot = new GameObject("PrimePowers");
        primePowersRoot.transform.SetParent(transform, false);
        foreach (int q in new[] { 4, 8, 9, 16, 25, 27 })
        {
            LineRenderer tick = Factory.NewLine("pp tick", .05f, new Color(1f, .6f, .25f), Factory.GlowIntensity);
            tick.transform.SetParent(primePowersRoot.transform, false);
            tick.positionCount = 2;
            primePowerTicks.Add(tick);
            TextBox l = Label(primePowersRoot.transform, q.ToString(), Vector3.zero, 2.2f, new Color(1f, .6f, .25f), TextAlignmentOptions.Center);
            l.gameObject.AddComponent<Billboard>();
            primePowerLabels.Add(l);
        }

        primePowersRoot.SetActive(false);

        // ---- rogue zero ---------------------------------------------------------------------
        ghostRoot = new GameObject("RogueZero");
        ghostRoot.transform.SetParent(transform, false);
        ghostCurve = Factory.NewLine("rogue psi", .09f, Factory.Palette.Ghost, Factory.GlowIntensity);
        ghostCurve.transform.SetParent(ghostRoot.transform, false);
        ghostCurve.positionCount = Main.NumPrimeCurvePoints;
        ghostLabel = Label(ghostRoot.transform, "", new Vector3(60, 85, 0), 3.5f, Factory.Palette.Ghost, TextAlignmentOptions.Center);
        ghostFormula = Label(ghostRoot.transform, "", new Vector3(22, 66, 0), 13f, Color.white, TextAlignmentOptions.Center);
        ghostRoot.SetActive(false);
    }

    GameObject Band(string name, double from, double to, float height, string text, TextAlignmentOptions align, double labelSigma)
    {
        GameObject root = new(name);
        root.transform.SetParent(stripRoot.transform, false);
        float w = (float)(to - from) * PanelWidth;
        MeshRenderer quad = Factory.NewQuad(name + " quad", new Vector2(w, height), new Color(Factory.Palette.ZeroFree.r, Factory.Palette.ZeroFree.g, Factory.Palette.ZeroFree.b, .38f));
        quad.transform.SetParent(root.transform, false);
        quad.transform.position = new Vector3(PanelLeft + (float)(from + to) * .5f * PanelWidth, height * .5f, 0.01f);
        LineRenderer edge = Factory.NewLine(name + " edge", .05f, Factory.Palette.ZeroFree, Factory.GlowIntensity);
        edge.transform.SetParent(root.transform, false);
        edge.positionCount = 2;
        double edgeSigma = from > 0.5 ? from : to;
        edge.SetPositions(new[] { StripPoint(edgeSigma, 0), StripPoint(edgeSigma, PanelMaxT) });
        Label(root.transform, text, StripPoint(labelSigma, 12), 2.3f, Factory.Palette.ZeroFree, align);
        return root;
    }

    GameObject DashedVertical(string name, double sigma, float height, string text, TextAlignmentOptions align, float labelT)
    {
        GameObject root = new(name);
        root.transform.SetParent(stripRoot.transform, false);
        const float dash = .6f;
        for (float y = 0; y < height; y += dash * 2)
        {
            LineRenderer seg = Factory.NewLine("dash", .035f, new Color(.7f, .85f, 1f), Factory.SoftGlowIntensity);
            seg.transform.SetParent(root.transform, false);
            seg.positionCount = 2;
            Vector3 p = StripPoint(sigma, 0) + Vector3.up * y;
            seg.SetPositions(new[] { p, p + Vector3.up * Mathf.Min(dash, height - y) });
        }

        Label(root.transform, text, StripPoint(1.14, labelT), 2.1f, new Color(.7f, .85f, 1f), align);
        return root;
    }

    GameObject ClassicRegion(float height)
    {
        // Schematic of the classical zero-free region σ > 1 − c/log t: a sliver hugging Re(s) = 1 that
        // narrows as t grows. Drawn to scale in spirit, not with a rigorous constant.
        GameObject root = new("ClassicZeroFree");
        root.transform.SetParent(stripRoot.transform, false);
        LineRenderer curve = Factory.NewLine("classic boundary", .045f, new Color(1f, .6f, .9f), Factory.GlowIntensity);
        curve.transform.SetParent(root.transform, false);
        List<Vector3> pts = new();
        for (double t = 2; t <= PanelMaxT; t += 1.0)
        {
            double sigma = 1d - 0.45d / System.Math.Log(t + 2);
            pts.Add(StripPoint(sigma, t) + Vector3.back * .01f);
        }

        curve.positionCount = pts.Count;
        curve.SetPositions(pts.ToArray());
        Label(root.transform, "classical zero-free region\nσ > 1 − c / log t\n(a sliver that shrinks as t grows)", StripPoint(1.14, 40), 1.6f, new Color(1f, .6f, .9f), TextAlignmentOptions.Left);
        return root;
    }

    void Axis(Transform parent, Vector3 from, Vector3 to, Color color, string text, Vector3 labelPos)
    {
        LineRenderer line = Factory.NewLine(text, .06f, color, Factory.GlowIntensity);
        line.transform.SetParent(parent, false);
        line.positionCount = 2;
        line.SetPositions(new[] { from, to });
        sproutLines.Add((line, new[] { from, to }));
        Arrowhead(parent, to, (to - from).normalized, color, .35f);
        Label(parent, text, labelPos, 2.4f, color, TextAlignmentOptions.Center);
    }

    static void Arrowhead(Transform parent, Vector3 tip, Vector3 direction, Color color, float size)
    {
        Vector3 side = Vector3.Cross(direction, Vector3.up);
        if (side.sqrMagnitude < 1e-4f) side = Vector3.Cross(direction, Vector3.forward);
        side.Normalize();
        LineRenderer head = Factory.NewLine("arrowhead", .06f, color, Factory.GlowIntensity);
        head.transform.SetParent(parent, false);
        head.positionCount = 3;
        head.SetPositions(new[] { tip - direction * size + side * size * .5f, tip, tip - direction * size - side * size * .5f });
    }

    static TextBox Label(Transform parent, string text, Vector3 position, float size, Color color, TextAlignmentOptions align)
    {
        TextBox label = TextBox.Create(text, size);
        label.transform.SetParent(parent, false);
        label.Alignment = align;
        label.transform.position = position;
        label.Color = color;
        return label;
    }

    // ---------------------------------------------------------------------------------------

    void OnZeroPassed(int index, double gamma, Vector3 markerPosition)
    {
        GameObject tick = Factory.NewSphere($"zero tick {index + 1}", .22f, Factory.Palette.Zero);
        tick.transform.SetParent(stripRoot.transform, false);
        tick.transform.position = StripPoint(0.5, gamma) + Vector3.back * .02f;
        LineRenderer guide = Factory.NewLine("guide", .02f, new Color(1f, .85f, .1f, .35f));
        guide.transform.SetParent(tick.transform, false);
        guide.positionCount = 2;
        guide.SetPositions(new[] { StripPoint(0.5, gamma), markerPosition });
        zeroTicks.Add(tick);
        StartCoroutine(Pulse(tick.transform, .22f, 2.2f, .9f));
    }

    void Update()
    {
        if (main == null) return;
        double t = main.CurrentT;
        probe.transform.position = StripPoint(0.5, t) + Vector3.back * .03f;

        if (t < zCurveT)
        {
            zPoints.Clear();
            zCurveT = -1;
        }

        if (zCurveRoot.activeSelf)
        {
            const double step = 0.08;
            if (zCurveT < 0) zCurveT = 0;
            while (zCurveT + step <= t)
            {
                zCurveT += step;
                double z = ZetaMath.HardyZ(zCurveT, zCurveT == t ? main.CurrentZeta : ZetaMath.Exact(new Complex(.5d, zCurveT)));
                float dx = Mathf.Clamp((float)z * ZScale, -PanelWidth * .49f, PanelWidth * .49f);
                zPoints.Add(StripPoint(0.5, zCurveT) + Vector3.right * dx + Vector3.back * .02f);
            }

            zCurve.positionCount = zPoints.Count;
            if (zPoints.Count > 0) zCurve.SetPositions(zPoints.ToArray());
        }

        if (ghostOn && ghostRoot.activeSelf)
        {
            UpdateGhost();
        }

        if (showAngles && anglesRoot.activeSelf)
        {
            UpdateAngles();
        }

        if (showLogSeq) UpdateLogSequence();
        if (showChainFeet) UpdateChainFeet();
        if (Time.time >= nextReadoutTime) nextReadoutTime = Time.time + 0.1f; // text readouts refresh at 10 Hz

        if (gapRoot.activeSelf)
        {
            float pulse = .14f + .08f * Mathf.Sin(Time.time * 2.5f);
            Factory.SetColor(gapQuad, new Color(1f, .5f, .2f, pulse));
        }

        if (missRoot != null && missRoot.activeSelf) UpdateMiss();

        if (tipRoot.activeSelf)
        {
            Vector3[] joints = main.Limit.LastJoints;
            Vector3 tip = main.Limit.ShowTail && main.Limit.LastDecomposition.PartialSums != null
                ? LimitCurve.ToWorld(main.Limit.LastDecomposition.Total, (float)main.Limit.LastT * Graph.yScale)
                : main.Limit.LastJointCount > 0 ? joints[main.Limit.LastJointCount - 1] : Vector3.zero;
            tipSphere.transform.position = tip;
        }

        if (centerRoot.activeSelf)
        {
            float y = (float)t * Graph.yScale;
            Vector3 c = new(0, y, 0);
            centerSphere.transform.position = c;
            float r = .5f;
            for (int i = 0; i < ringBuffer.Length; i++)
            {
                float a = i / (float)ringBuffer.Length * Mathf.PI * 2f;
                ringBuffer[i] = c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
            }

            centerRing.SetPositions(ringBuffer);
            if (Time.time >= nextReadoutTime)
            {
                double distance = main.Limit.LastDecomposition.PartialSums != null ? ComplexMath.Abs(main.Limit.LastDecomposition.Total) : 0;
                string text = $"dial  t = {t:0.00}\ntip to centre  {distance:0.00}";
                if (text != lastDialText) { lastDialText = text; dialLabel.Text = text; }
            }

            // the first arrow always points along +x, so the −x side stays free
            dialLabel.transform.position = c + new Vector3(-1.1f, .05f, -0.9f);
        }
    }

    public void ShowTip(bool on) => tipRoot.SetActive(on);

    // ---- miss distance: how far the chain's tip passes from the centre (the square-root key chapter) ----------------
    GameObject missRoot;
    LineRenderer missLine, closestLine;
    TextBox missLabel, closestLabel;
    GameObject closestDot;
    double closestMiss = double.MaxValue, closestT;
    float lastMissT = -1f;

    /// <summary>
    /// A line from the centre (the pole) to the chain's tip with its length, and the closest pass so far kept on screen:
    /// on the ½ line the tip hits the centre (miss 0); at exponent 0.9 it always misses, by the amount shown.
    /// </summary>
    public void ShowMiss(bool on)
    {
        if (on && missRoot == null)
        {
            missRoot = new GameObject("Miss");
            missRoot.transform.SetParent(transform, false);
            missLine = Factory.NewLine("miss", .05f, Factory.Palette.Ghost, Factory.GlowIntensity);
            missLine.transform.SetParent(missRoot.transform, false);
            missLine.positionCount = 2;
            closestLine = Factory.NewLine("closest miss", .04f, new Color(1f, .6f, .2f), Factory.SoftGlowIntensity);
            closestLine.transform.SetParent(missRoot.transform, false);
            closestLine.positionCount = 2;
            closestDot = Factory.NewSphere("closest tip", .12f, new Color(1f, .6f, .2f), Factory.GlowIntensity);
            closestDot.transform.SetParent(missRoot.transform, false);
            missLabel = TextBox.Create("", 3.4f);
            missLabel.transform.SetParent(missRoot.transform, false);
            missLabel.Alignment = TMPro.TextAlignmentOptions.Left;
            missLabel.Color = Factory.Palette.Ghost;
            missLabel.gameObject.AddComponent<Billboard>();
            closestLabel = TextBox.Create("", 3f);
            closestLabel.transform.SetParent(missRoot.transform, false);
            closestLabel.Alignment = TMPro.TextAlignmentOptions.Left;
            closestLabel.Color = new Color(1f, .6f, .2f);
            closestLabel.gameObject.AddComponent<Billboard>();
        }

        if (missRoot == null) return;
        missRoot.SetActive(on);
        closestMiss = double.MaxValue;
        lastMissT = -1f;
        if (on) UpdateMiss();
    }

    void UpdateMiss()
    {
        ZetaMath.SpiralDecomposition d = main.Limit.LastDecomposition;
        if (d.PartialSums == null) return;
        float y = (float)main.Limit.LastT * Graph.yScale;
        Vector3 centre = new(0, y, 0);
        Vector3 tip = LimitCurve.ToWorld(d.Total, y);
        double miss = ComplexMath.Abs(d.Total);
        if ((float)main.Limit.LastT < lastMissT) closestMiss = double.MaxValue; // the dial went back: start over
        lastMissT = (float)main.Limit.LastT;
        if (miss < closestMiss)
        {
            closestMiss = miss;
            closestT = main.Limit.LastT;
            closestDot.transform.position = tip;
            closestLine.SetPositions(new[] { centre, tip });
            closestLabel.Text = $"closest pass: {closestMiss:0.000}\nat t = {closestT:0.00}";
            closestLabel.transform.position = tip + new Vector3(.25f, .05f, -.35f);
        }

        missLine.SetPositions(new[] { centre + Vector3.up * .01f, tip + Vector3.up * .01f });
        missLabel.Text = $"miss = {miss:0.000}";
        missLabel.transform.position = (centre + tip) * .5f + new Vector3(.1f, .06f, .12f);
    }
    public void ShowZeno(bool on) => zenoRoot.SetActive(on);

    TextBox landingLabel;

    /// <summary>Callout at the centre when the tip lands: the dial value that closed the chain.</summary>
    public void ShowLanding(bool on, string text = "")
    {
        if (landingLabel == null)
        {
            landingLabel = Label(transform, "", Vector3.zero, 1.4f, Factory.Palette.Zero, TextAlignmentOptions.Left);
            landingLabel.gameObject.AddComponent<Billboard>();
        }

        landingLabel.gameObject.SetActive(on);
        if (!on) return;
        landingLabel.Text = text;
        landingLabel.transform.position = new Vector3(.9f, (float)main.CurrentT * Graph.yScale + .05f, -1.6f);
    }

    static bool IsPrime(int n)
    {
        if (n < 2) return false;
        for (int d = 2; d * d <= n; d++) if (n % d == 0) return false;
        return true;
    }

    static string Factorization(int n)
    {
        List<string> f = new();
        int m = n;
        for (int d = 2; d <= m; d++)
        {
            while (m % d == 0) { f.Add(d.ToString()); m /= d; }
        }

        return string.Join(" × ", f);
    }

    // ---- primes intro -----------------------------------------------------------------------------
    public void ShowPrimes(bool on)
    {
        if (on) Sacks.ShowGrid(); else Sacks.Hide();
    }

    /// <summary>Light the primes one by one; composites fade and show their factors.</summary>
    public void BuildPrimes(float seconds)
    {
        Sacks.gameObject.SetActive(true);
        Sacks.StartCoroutine(Sacks.BuildGrid(seconds));
    }

    static IEnumerator ShrinkGlow(Transform t, float from, float to, float seconds)
    {
        float e = 0f;
        while (e < seconds && t != null)
        {
            e += Time.deltaTime;
            t.localScale = Vector3.one * Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, e / seconds));
            yield return null;
        }
    }

    // ---- sprouting axes ---------------------------------------------------------------------------
    public void SproutAxes(float seconds)
    {
        StartCoroutine(SproutRoutine(seconds));
    }

    IEnumerator SproutRoutine(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / seconds);
            foreach ((LineRenderer line, Vector3[] full) in sproutLines)
            {
                line.SetPositions(new[] { full[0], Vector3.Lerp(full[0], full[1], k) });
            }

            yield return null;
        }

        foreach ((LineRenderer line, Vector3[] full) in sproutLines) line.SetPositions(full);
    }

    // ---- joint angles -----------------------------------------------------------------------------
    /// <summary>The joint angles on or off; off also clears the stacked turns (the angles are not drawn again after them).</summary>
    public void ShowAngles(bool on)
    {
        SetJointAngles(on);
        if (!on) HideAngleStack();
    }

    void SetJointAngles(bool on)
    {
        showAngles = on;
        anglesRoot.SetActive(on);
        if (on) UpdateAngles();
    }

    void UpdateAngles()
    {
        Vector3[] joints = main.Limit.LastJoints;
        double t = main.Limit.LastT;
        int count = Mathf.Min(angleArcs.Count, main.Limit.LastJointCount - 2);
        for (int i = 0; i < angleArcs.Count; i++)
        {
            bool active = i < count;
            angleArcs[i].enabled = active;
            legExtensions[i].enabled = active;
            angleLabels[i].gameObject.SetActive(active && i < AngleLabelCount);
            if (!active) continue;
            // the joint between arrow n − 1 (incoming) and arrow n (outgoing)
            Vector3 j = joints[i + 1];
            Vector3 a = joints[i + 1] - joints[i];
            Vector3 b = joints[i + 2] - joints[i + 1];
            if (a.sqrMagnitude < 1e-6f || b.sqrMagnitude < 1e-6f) { angleArcs[i].enabled = false; legExtensions[i].enabled = false; continue; }
            float r = Mathf.Clamp(Mathf.Min(a.magnitude, b.magnitude) * .45f, .05f, .3f);
            float a0 = Mathf.Atan2(a.z, a.x);
            int n = i + 2;
            // the whole turn t·log(n / (n−1)), not folded into ±180°: as the dial climbs it keeps winding round the joint,
            // spiralling outward a little each full turn (the arrows turn clockwise: arg n^−s = −t·log n)
            float turn = (float)(t * System.Math.Log((double)n / (n - 1)));
            int segs = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(turn) / (2f * Mathf.PI) * 48f), 8, angleSpiralBuffer.Length - 1);
            for (int k = 0; k <= segs; k++)
            {
                float u = k / (float)segs;
                float ang = a0 - turn * u;
                float rr = r * (1f + .22f * turn * u / (2f * Mathf.PI));
                angleSpiralBuffer[k] = j + new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * rr + Vector3.up * .02f;
            }

            angleArcs[i].positionCount = segs + 1;
            angleArcs[i].SetPositions(angleSpiralBuffer);
            legExtensions[i].SetPosition(0, j + Vector3.up * .015f);
            legExtensions[i].SetPosition(1, j + a.normalized * (r * 1.9f) + Vector3.up * .015f);
            if (i < AngleLabelCount)
            {
                double degrees = turn * 180 / System.Math.PI;
                if (Time.time >= nextReadoutTime) angleLabels[i].Text = $"<color=#C8A0FF>t</color>·{System.Math.Log((double)n / (n - 1)):0.00} = {degrees:0}°";
                float mid = a0 - Mathf.Min(Mathf.Abs(turn), Mathf.PI) * .5f * Mathf.Sign(turn);
                angleLabels[i].transform.position = j + new Vector3(Mathf.Cos(mid), 0, Mathf.Sin(mid)) * (r * (1f + .22f * Mathf.Abs(turn) / (2f * Mathf.PI)) + .22f);
            }
        }

        if (Time.time >= nextReadoutTime)
        {
            string text = $"dial t = {t:0.00}\n<color=#FFD400>lengths follow square roots:</color>  arrow n = 1/√n\n<color=#FF99F0>bends follow logarithms:</color>  turn at joint n = t · log( n / (n−1) )\njoint 2: {t * System.Math.Log(2) * 180 / System.Math.PI:0}°   joint 3: {t * System.Math.Log(1.5) * 180 / System.Math.PI:0}°   joint 4: {t * System.Math.Log(4.0 / 3) * 180 / System.Math.PI:0}°";
            if (text != lastAngleText) { lastAngleText = text; angleFormula.Text = text; }
        }
        angleFormula.transform.position = new Vector3(-3.7f, (float)t * Graph.yScale + .05f, 3.3f);
    }

    // ---- the first twenty turns stacked end to end: what they add up to --------------------------------------
    GameObject angleStackRoot;
    public Vector3 AngleStackCentre { get; private set; }

    /// <summary>
    /// At the first collision: the turns at joints 2 … 21 leave their joints and are laid end to end as one spiral beside the
    /// chain, each in its own colour, with their sum. The sum telescopes: t·(log 2/1 + log 3/2 + … + log 21/20) = t·log 21.
    /// The joint angles are not drawn again after this.
    /// </summary>
    public void StackAngles(float seconds)
    {
        if (angleStackRoot != null) Destroy(angleStackRoot);
        SetJointAngles(false);
        angleStackRoot = new GameObject("AngleStack");
        angleStackRoot.transform.SetParent(transform, false);
        StartCoroutine(StackAnglesRoutine(seconds));
    }

    public void HideAngleStack()
    {
        if (angleStackRoot != null) Destroy(angleStackRoot);
        angleStackRoot = null;
    }

    IEnumerator StackAnglesRoutine(float seconds)
    {
        double t = main.Limit.LastT;
        float y = (float)t * Graph.yScale;
        Camera cam = Camera.main;
        bool tall = cam != null && cam.aspect < 1f;
        // beside the chain in a wide frame, below it in a tall one (the top-down shot has +z as screen-up)
        AngleStackCentre = tall ? new Vector3(.2f, y, -2.3f) : new Vector3(-3.1f, y, .1f);
        Vector3 c = AngleStackCentre + Vector3.up * .02f;
        float per = seconds * .75f / AngleCount;
        float cumulative = 0f;
        for (int i = 0; i < AngleCount; i++)
        {
            int n = i + 2;
            float turn = (float)(t * System.Math.Log((double)n / (n - 1)));
            Color col = Color.HSVToRGB(Mathf.Repeat(.78f - i * .045f, 1f), .55f, 1f);
            LineRenderer seg = Factory.NewLine("turn " + n, .035f, col, Factory.GlowIntensity);
            seg.transform.SetParent(angleStackRoot.transform, false);
            int segs = Mathf.Clamp(Mathf.CeilToInt(turn / (2f * Mathf.PI) * 64f), 6, 256);
            Vector3[] pts = new Vector3[segs + 1];
            for (int k = 0; k <= segs; k++)
            {
                float ang = Mathf.PI * .5f - (cumulative + turn * k / segs);
                float rr = .32f + .075f * (cumulative + turn * k / segs) / (2f * Mathf.PI);
                pts[k] = c + new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * rr;
            }

            // the segment draws itself on, in order
            float e = 0f;
            while (e < per)
            {
                e += Time.deltaTime;
                int shown = Mathf.Clamp(Mathf.CeilToInt(e / per * segs) + 1, 2, segs + 1);
                seg.positionCount = shown;
                for (int k = 0; k < shown; k++) seg.SetPosition(k, pts[k]);
                yield return null;
            }

            seg.positionCount = pts.Length;
            seg.SetPositions(pts);
            cumulative += turn;
        }

        double total = t * System.Math.Log(AngleCount + 1);
        TextBox sum = Label(angleStackRoot.transform, $"the first {AngleCount} turns, end to end\n<color=#C8A0FF>t</color> · log 21 = {total:0.0} radians = {total / (2 * System.Math.PI):0.0} full turns",
            c + (tall ? new Vector3(0, 0, -1.25f) : new Vector3(0, 0, -1.15f)), .8f, new Color(1f, .75f, .97f), TextAlignmentOptions.Center);
        sum.gameObject.AddComponent<Billboard>();
    }

    // ---- logarithm sequence ---------------------------------------------------------------------------
    public void ShowLogSequence(bool on)
    {
        showLogSeq = on;
        logSeqRoot.SetActive(on);
        if (on) UpdateLogSequence();
    }

    void UpdateLogSequence()
    {
        double t = main.Limit.LastT;
        float y = (float)t * Graph.yScale + .02f;
        // upper-left of the top-down spiral shot (screen-right = +x, screen-up = +z)
        Vector3 origin = new(-3.7f, y, 0.9f);
        const float spacing = .36f, heightScale = 2.2f;
        bool refresh = Time.time >= nextReadoutTime;
        for (int i = 0; i < logBars.Count; i++)
        {
            int n = i + 2;
            float logv = Mathf.Log((float)n / (n - 1));
            float h = logv * heightScale;
            Vector3 b = origin + Vector3.right * (i * spacing);
            pair[0] = b;
            pair[1] = b + Vector3.forward * h;
            logBars[i].SetPositions(pair);
            if (refresh)
            {
                double turn = t * logv * 180 / System.Math.PI;
                logBarLabels[i].Text = $"log {n}/{n - 1}\n{logv:0.00}\n{turn:0}°";
            }

            logBarLabels[i].transform.position = b + Vector3.forward * (h + .55f);
        }

        if (refresh) logSeqTitle.Text = "the bends: t × log(n / (n−1)), a shrinking sequence";
        logSeqTitle.transform.position = origin + new Vector3(spacing * 4.5f, 0, -.55f);
    }

    // ---- dial strip ---------------------------------------------------------------------------------
    public void ClearDialStrip()
    {
        foreach ((GameObject root, LineRenderer _, Vector3 _) in stripDials) Destroy(root);
        stripDials.Clear();
        dialStripRoot.SetActive(false);
    }

    /// <summary>Add a small dial for collision <paramref name="index"/> and sweep its needle to γ.</summary>
    public void AddStripDial(int index, float seconds)
    {
        dialStripRoot.SetActive(true);
        if (index >= main.zetaZeroValues.Length) return;
        float gamma = (float)main.zetaZeroValues[index];
        int slot = stripDials.Count;
        Vector3 center = new(2.6f + slot * 2.7f, 13.6f, 0);
        GameObject root = new GameObject("strip dial " + (index + 1));
        root.transform.SetParent(dialStripRoot.transform, false);
        Color accent = CollisionColor(index);
        LineRenderer ring = Factory.NewLine("ring", .03f, accent, Factory.SoftGlowIntensity);
        ring.transform.SetParent(root.transform, false);
        ring.loop = true;
        ring.positionCount = 24;
        Vector3[] pts = new Vector3[24];
        for (int i = 0; i < 24; i++)
        {
            float a = i / 24f * Mathf.PI * 2f;
            pts[i] = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * 1.05f;
        }

        ring.SetPositions(pts);
        LineRenderer needle = Factory.NewLine("needle", .05f, Factory.Palette.Zero, Factory.GlowIntensity);
        needle.transform.SetParent(root.transform, false);
        needle.positionCount = 2;
        TextBox label = Label(root.transform, gamma.ToString("0.00"), center + Vector3.down * 1.7f, 1.7f, accent, TextAlignmentOptions.Center);
        label.gameObject.AddComponent<Billboard>();
        stripDials.Add((root, needle, center));
        StartCoroutine(SweepNeedle(needle, center, 0f, Mathf.Min(gamma, 60f), seconds, .9f, 60f));
    }

    IEnumerator SweepNeedle(LineRenderer needle, Vector3 center, float g0, float g1, float seconds, float length, float fullScale)
    {
        float t = 0f;
        while (t < seconds && needle != null)
        {
            t += Time.deltaTime;
            float g = Mathf.Lerp(g0, g1, Mathf.SmoothStep(0, 1, t / seconds));
            float a = Mathf.Deg2Rad * (225f - g / fullScale * 270f);
            needle.SetPositions(new[] { center, center + new Vector3(Mathf.Cos(a), Mathf.Sin(a), -.02f) * length });
            yield return null;
        }
    }

    // ---- prime arrows + sequential highlight ------------------------------------------------------
    public void ShowPrimeArrows(bool on) => primeArrowsRoot.SetActive(on);

    public void HighlightPrimes(float seconds)
    {
        StartCoroutine(HighlightPrimesRoutine(seconds));
    }

    IEnumerator HighlightPrimesRoutine(float seconds)
    {
        int[] primes = { 2, 3, 5, 7, 11, 13 };
        float per = seconds / primes.Length;
        Transform[] arrows = new Transform[primeArrowsRoot.transform.childCount];
        for (int i = 0; i < arrows.Length; i++) arrows[i] = primeArrowsRoot.transform.GetChild(i);
        foreach (int p in primes)
        {
            foreach (Transform child in numberLineRoot.transform)
            {
                TextBox tb = child.GetComponent<TextBox>();
                if (tb != null && child.name.EndsWith(": " + p)) StartCoroutine(Pulse(child, 1f, 1.9f, per * .9f));
            }

            // the arrow for this prime is children (2k, 2k+1)
            int k = System.Array.IndexOf(primes, p);
            if (2 * k < arrows.Length) StartCoroutine(Pulse(arrows[2 * k], 1f, 1.5f, per * .9f));
            yield return new WaitForSeconds(per);
        }
    }

    // ---- formulas ---------------------------------------------------------------------------------
    /// <summary>Big formula beside the spiral: the shrink rule in use.</summary>
    public void ShowRootFormula(string mode)
    {
        if (mode == "off") { rootFormulaRoot.SetActive(false); return; }
        rootFormulaRoot.SetActive(true);
        float y = (float)main.CurrentT * Graph.yScale;
        rootFormula.transform.position = mode == "chain" ? new Vector3(2.4f, .05f, 1.9f) : new Vector3(0.3f, y + .05f, 1.55f);
        if (mode == "chain") mode = "sqrt";
        rootFormula.Text = mode == "sqrt"
            ? "arrow n  =  <color=#FFD400>1 / √n</color>  =  1 / n<sup>½</sup>"
            : $"arrow n  =  <color=#FF5A6E>1 / n<sup>{mode}</sup></color>";
    }

    Coroutine ghostSweep;

    /// <summary>Sweep the rogue exponent β back and forth; the red staircase and the formula follow.</summary>
    public void SweepGhostBeta(float from, float to, float seconds, bool pingPong)
    {
        if (ghostSweep != null) StopCoroutine(ghostSweep);
        ghostSweep = StartCoroutine(SweepGhostRoutine(from, to, seconds, pingPong));
    }

    IEnumerator SweepGhostRoutine(float from, float to, float seconds, bool pingPong)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float u = t / seconds;
            float k = pingPong ? 0.5f - 0.5f * Mathf.Cos(u * Mathf.PI * 2f) : Mathf.SmoothStep(0, 1, u);
            ghostBeta = Mathf.Lerp(from, to, k);
            yield return null;
        }

        ghostBeta = pingPong ? from : to;
    }

    static float DialAngle(float gamma) => Mathf.Deg2Rad * (225f - Mathf.Clamp(gamma, 0f, 15f) / 15f * 270f);

    /// <summary>Demo wave: cos(γ·k·x) bent by <see cref="demoBend"/> toward cos(γ·log x); formula label and dial follow γ.</summary>
    Vector3[] demoWaveBuffer;

    void DrawDemoWave()
    {
        // called every frame while the wave is dialled: reuse the buffer instead of allocating 12 to 48 KB each time
        Vector3[] pts = demoWaveBuffer ??= new Vector3[Main.NumPrimeCurvePoints];
        for (int i = 0; i < pts.Length; i++)
        {
            float x = (float)Main.PrimeCurveX(i);
            if (x < 1.5f)
            {
                pts[i] = new Vector3(1.5f, 0, 0);
                continue;
            }

            float plain = demoGamma * PlainK * (x - 1.5f);
            float bent = demoGamma * Mathf.Log(x);
            float arg = Mathf.Lerp(plain, bent, demoBend);
            pts[i] = new Vector3(x, -demoAmplitude * Mathf.Cos(arg), 0);
        }

        waveDemo.SetPositions(pts);
        string g = $"<color=#FFD400>{demoGamma:0.00}</color>";
        string inner = demoBend < 0.5f
            ? $"{g} · x"
            : $"{g} · <color=#80E8FF>log x</color>";
        if (demoBend > 0.02f && demoBend < 0.98f) inner = $"{g} · <color=#80E8FF>log</color> x";
        formulaLabel.Text = $"cos( {inner} )";

        if (dialRoot.activeSelf)
        {
            Vector3 center = new(13.5f, -9f, 0);
            float a = DialAngle(demoGamma);
            dialNeedle.SetPositions(new[] { center, center + new Vector3(Mathf.Cos(a), Mathf.Sin(a), -0.02f) * 1.9f });
        }
    }

    public void ShowDial(bool on)
    {
        dialRoot.SetActive(on);
        if (on) DrawDemoWave();
    }

    public void ShowDemoWavePlain(float amplitude, float gamma)
    {
        if (waveMorph != null) StopCoroutine(waveMorph);
        waveDemoRoot.SetActive(true);
        demoAmplitude = amplitude;
        demoGamma = gamma;
        demoBend = 0f;
        DrawDemoWave();
    }

    /// <summary>Turn the dial: sweep γ (keeps whatever bend is current).</summary>
    public void SweepDemoGamma(float to, float seconds)
    {
        if (waveMorph != null) StopCoroutine(waveMorph);
        waveDemoRoot.SetActive(true);
        waveMorph = StartCoroutine(SweepRoutine(demoGamma, to, demoBend, demoBend, seconds));
    }

    /// <summary>Bend the wave with the logarithm at the current γ.</summary>
    public void MorphDemoWave(float seconds, float amplitude)
    {
        if (waveMorph != null) StopCoroutine(waveMorph);
        waveDemoRoot.SetActive(true);
        demoAmplitude = amplitude;
        waveMorph = StartCoroutine(SweepRoutine(demoGamma, demoGamma, demoBend, 1f, seconds));
    }

    IEnumerator SweepRoutine(float g0, float g1, float b0, float b1, float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / seconds);
            demoGamma = Mathf.Lerp(g0, g1, k);
            demoBend = Mathf.Lerp(b0, b1, k);
            DrawDemoWave();
            yield return null;
        }

        demoGamma = g1;
        demoBend = b1;
        DrawDemoWave();
    }

    public void HideDemoWave()
    {
        if (waveMorph != null) StopCoroutine(waveMorph);
        waveDemoRoot.SetActive(false);
    }

    // ---- Zeno build-up: arrows grow one after another ------------------------------------------
    public void ResetZeno()
    {
        ZenoFocus = new Vector3(1f, 0f, 1.15f);
        ZenoZoom = 9.5f;
        foreach (GameObject a in zenoArrows) a.SetActive(false);
        foreach (GameObject l in zenoLabels) l.SetActive(false);
        foreach (GameObject f in zenoFootprints) f.SetActive(false);
        foreach (GameObject s in zenoStepLabels) s.SetActive(false);
        zenoWall.SetActive(false);
    }

    public void BuildZeno(float seconds)
    {
        StartCoroutine(BuildZenoRoutine(seconds));
    }

    IEnumerator BuildZenoRoutine(float seconds)
    {
        ResetZeno();
        float per = seconds / (zenoArrows.Count + 1);
        for (int i = 0; i < zenoArrows.Count; i++)
        {
            GameObject arrow = zenoArrows[i];
            if (i < zenoFootprints.Count) zenoFootprints[i].SetActive(false);
            LineRenderer seg = arrow.transform.GetChild(0).GetComponent<LineRenderer>();
            LineRenderer head = arrow.transform.GetChild(1).GetComponent<LineRenderer>();
            Vector3[] full = new Vector3[2];
            seg.GetPositions(full);
            arrow.SetActive(true);
            head.enabled = false;
            float t = 0f;
            while (t < per * .8f)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / (per * .8f));
                seg.SetPositions(new[] { full[0], Vector3.Lerp(full[0], full[1], k) });
                yield return null;
            }

            seg.SetPositions(full);
            head.enabled = true;
            if (i < zenoLabels.Count) zenoLabels[i].SetActive(true);
            if (i < zenoFootprints.Count)
            {
                zenoFootprints[i].SetActive(true);
                StartCoroutine(Pulse(zenoFootprints[i].transform, 1f, 1.5f, .5f));
                float reached = 2f - Mathf.Pow(0.5f, i);
                ZenoFocus = new Vector3(reached - Mathf.Pow(0.5f, i) * .3f, 0f, 1.15f);
                ZenoZoom = Mathf.Max(0.35f, 9.5f * Mathf.Pow(0.5f, i));
            }

            if (i < zenoStepLabels.Count) zenoStepLabels[i].SetActive(true);
            yield return new WaitForSeconds(per * .2f);
        }

        ZenoFocus = new Vector3(1f, 0f, 1.15f);
        ZenoZoom = 9.5f;

        // the wall slides in from the left of the screen (screen-left is +z in the zeno cue)
        zenoWall.SetActive(true);
        float slide = 0f;
        while (slide < 0.8f)
        {
            slide += Time.deltaTime;
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(slide / 0.8f), 3f);
            zenoWall.transform.localPosition = new Vector3(0, 0, Mathf.Lerp(7f, 0f, k));
            yield return null;
        }

        zenoWall.transform.localPosition = Vector3.zero;
        StartCoroutine(Pulse(zenoWall.transform, 1f, 1.6f, 1.2f));
    }

    // ---- attention pulses: breathe the glow of an element for a while -----------------------------
    public void PulseRenderers(IEnumerable<Renderer> renderers, float seconds, float strength = 0.7f)
    {
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            if (pulses.TryGetValue(r, out Coroutine running) && running != null) StopCoroutine(running);
            pulses[r] = StartCoroutine(PulseGlow(r, seconds, strength));
        }
    }

    IEnumerator PulseGlow(Renderer r, float seconds, float strength)
    {
        float baseIntensity = Factory.GetIntensity(r);
        if (baseIntensity <= 0f) baseIntensity = Factory.NoGlow;
        float t = 0f;
        while (t < seconds && r != null)
        {
            t += Time.deltaTime;
            float env = Mathf.Min(1f, Mathf.Min(t, seconds - t) / 0.6f); // ease in/out
            float k = 0.5f + 0.5f * Mathf.Sin(t * 4.2f);
            Factory.SetIntensity(r, baseIntensity * (1f + strength * k * env));
            yield return null;
        }

        if (r != null) Factory.SetIntensity(r, baseIntensity);
        pulses.Remove(r);
    }

    public IEnumerable<Renderer> RenderersOf(string element)
    {
        GameObject root = element switch
        {
            "strip" => stripRoot, "band78" => band78Root, "band18" => band18Root, "gap" => gapRoot,
            "numbers" => numberLineRoot, "zeno" => zenoRoot, "wave" => waveDemoRoot, "dial" => dialRoot,
            "center" => centerRoot, "tip" => tipRoot, "imaxis" => imAxisRoot, "outaxes" => outputAxesRoot, "ghost" => ghostRoot,
            "primes" => Sacks.gameObject, "angles" => anglesRoot, "dialstrip" => dialStripRoot, "primearrows" => primeArrowsRoot,
            "chainfeet" => chainFeetRoot, "primepowers" => primePowersRoot, "logseq" => logSeqRoot, "floorgrid" => floorGridRoot,
            _ => null
        };
        if (root == null) yield break;
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>()) if (r is LineRenderer or MeshRenderer) yield return r;
    }

    /// <summary>Draw collision number <paramref name="index"/>'s wave by itself, before it joins the chorus.</summary>
    /// <summary>One distinct hue per collision, shared by its dial, its wave and its difference lines.</summary>
    static readonly float[] CollisionHues = { 0.86f, 0.08f, 0.28f, 0.72f, 0.0f, 0.15f, 0.62f, 0.42f, 0.95f, 0.05f };

    public static Color CollisionColor(int index)
    {
        if (index == 0) return Factory.Palette.Cosine;
        return Color.HSVToRGB(CollisionHues[(index - 1) % CollisionHues.Length], .85f, 1f);
    }

    /// <summary>Draw collision <paramref name="index"/>'s wave on the SAME axis as the chorus, plus the gap to the chorus.</summary>
    public void ShowIncomingWave(int index, float amplitude, float lift)
    {
        if (index < 0 || index >= main.zetaZeroValues.Length) return;
        double gamma = main.zetaZeroValues[index];
        Color c = CollisionColor(index);
        incomingWave.startColor = c;
        incomingWave.endColor = c;
        incomingLabel.Color = c;
        Vector3[] pts = new Vector3[Main.NumPrimeCurvePoints];
        for (int i = 0; i < pts.Length; i++)
        {
            float x = (float)Main.PrimeCurveX(i);
            pts[i] = x < 1.5f ? new Vector3(1.5f, 0, 0.02f) : new Vector3(x, -amplitude * (float)System.Math.Cos(gamma * System.Math.Log(x)), 0.02f);
        }

        incomingWave.SetPositions(pts);
        incomingLabel.Text = $"collision {index + 1}:  {gamma:0.00}";
        incomingLabel.transform.position = new Vector3(WaveViewMax * .5f, amplitude * 1.15f + 2.2f, 0);
        incomingRoot.SetActive(true);

        // difference lines: from the master wave to the new wave, across the visible range
        Vector3[] master = new Vector3[Main.NumPrimeCurvePoints];
        main.GetCosinePositions(master);
        diffRoot.SetActive(true);
        for (int k = 0; k < diffLines.Count; k++)
        {
            float x = 1.8f + (WaveViewMax - 2f) * k / (diffLines.Count - 1f);
            int i = Mathf.Clamp(Mathf.RoundToInt(x / Main.PrimeCurveXLength * Main.NumPrimeCurvePoints) - 1, 0, Main.NumPrimeCurvePoints - 1);
            diffLines[k].startColor = c;
            diffLines[k].endColor = new Color(c.r, c.g, c.b, .25f);
            diffLines[k].SetPositions(new[] { new Vector3(pts[i].x, pts[i].y, 0.03f), new Vector3(master[i].x, master[i].y, 0.03f) });
        }
    }

    public void HideIncomingWave()
    {
        incomingRoot.SetActive(false);
        diffRoot.SetActive(false);
    }

    readonly Vector3[] incomingBuffer = new Vector3[Main.NumPrimeCurvePoints];

    /// <summary>
    /// One collision joining the chorus, as a continuous animation:
    /// born as a plain cosine, wound up by its dial (γ sweeps 1 → γₙ, the ripples pack to the left),
    /// then absorbed into the master wave while the gap bars collapse.
    /// </summary>
    public IEnumerator IncomingWaveRoutine(int index, float amplitude, float seconds)
    {
        if (index < 0 || index >= main.zetaZeroValues.Length) yield break;
        float gamma = (float)main.zetaZeroValues[index];
        Color c = CollisionColor(index);
        incomingWave.startColor = c;
        incomingWave.endColor = c;
        incomingLabel.Color = c;
        incomingRoot.SetActive(true);
        diffRoot.SetActive(true);
        masterBuffer ??= new Vector3[Main.NumPrimeCurvePoints];
        main.GetCosinePositions(masterBuffer);

        float wind = seconds * .5f, absorb = seconds * .5f;
        float t = 0f;
        while (t < wind)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / wind);
            float g = Mathf.Lerp(1f, gamma, k);
            DrawIncoming(g, amplitude, 1f, c);
            incomingLabel.Text = $"collision {index + 1}:  dial → {g:0.00}";
            incomingLabel.transform.position = new Vector3(WaveViewMax * .5f, amplitude * 1.15f + 2.2f, 0);
            yield return null;
        }

        main.SetCosineZeroLimitAnimated(index + 1, absorb);
        t = 0f;
        while (t < absorb)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / absorb);
            DrawIncoming(gamma, amplitude, 1f - k, c);
            incomingLabel.Text = $"collision {index + 1}:  {gamma:0.00}  joins the master wave";
            yield return null;
        }

        HideIncomingWave();
    }

    void DrawIncoming(float gamma, float amplitude, float scale, Color c)
    {
        for (int i = 0; i < incomingBuffer.Length; i++)
        {
            float x = (float)Main.PrimeCurveX(i);
            incomingBuffer[i] = x < 1.5f ? new Vector3(1.5f, 0, 0.02f) : new Vector3(x, -amplitude * scale * Mathf.Cos(gamma * Mathf.Log(x)), 0.02f);
        }

        incomingWave.SetPositions(incomingBuffer);
        main.GetCosinePositions(masterBuffer);
        for (int k = 0; k < diffLines.Count; k++)
        {
            float x = 1.8f + (WaveViewMax - 2f) * k / (diffLines.Count - 1f);
            int i = Mathf.Clamp(Mathf.RoundToInt(x / Main.PrimeCurveXLength * Main.NumPrimeCurvePoints) - 1, 0, Main.NumPrimeCurvePoints - 1);
            diffLines[k].startColor = c;
            diffLines[k].endColor = new Color(c.r, c.g, c.b, .25f);
            pair[0] = new Vector3(incomingBuffer[i].x, incomingBuffer[i].y, 0.03f);
            pair[1] = new Vector3(masterBuffer[i].x, masterBuffer[i].y, 0.03f);
            diffLines[k].SetPositions(pair);
        }
    }

    /// <summary>Widen the wave view as more collisions join (the camera cues read this).</summary>
    public void SetWaveCount(int count)
    {
        WaveViewMax = Mathf.Clamp(14f + count * 2.6f, 16f, 40f);
    }

    public void ShowSquaresLabel(bool on) => squaresLabel.gameObject.SetActive(on);

    /// <summary>Orange ticks on the staircase at the prime powers 4, 8, 9, 16, 25, 27.</summary>
    public void ShowPrimePowers(bool on)
    {
        primePowersRoot.SetActive(on);
        if (!on) return;
        int[] q = { 4, 8, 9, 16, 25, 27 };
        for (int k = 0; k < q.Length; k++)
        {
            int i = Mathf.Clamp(Mathf.RoundToInt((float)q[k] / Main.PrimeCurveXLength * Main.NumPrimeCurvePoints) - 1, 0, Main.NumPrimeCurvePoints - 1);
            float y = main.PsiValues[i];
            primePowerTicks[k].SetPositions(new[] { new Vector3(q[k], y + 0.6f, -0.05f), new Vector3(q[k], y + 3.2f, -0.05f) });
            primePowerLabels[k].transform.position = new Vector3(q[k], y + 4.3f, -0.05f);
        }
    }

    // ---- footprints along the √n chain -------------------------------------------------------------
    public void ShowChainFeet(bool on)
    {
        showChainFeet = on;
        chainFeetRoot.SetActive(on);
        if (on) UpdateChainFeet();
    }

    void UpdateChainFeet()
    {
        Vector3[] joints = main.Limit.LastJoints;
        int count = Mathf.Min(main.Limit.LastJointCount - 1, 12);
        while (chainFeet.Count < count)
        {
            GameObject foot = new GameObject("chain foot " + chainFeet.Count);
            foot.transform.SetParent(chainFeetRoot.transform, false);
            LineRenderer sole = Factory.NewLine("sole", .02f, new Color(1f, .95f, .8f), Factory.SoftGlowIntensity);
            sole.transform.SetParent(foot.transform, false);
            sole.loop = true;
            sole.positionCount = 14;
            chainFeet.Add(foot);
        }

        for (int i = 0; i < chainFeet.Count; i++)
        {
            bool active = i < count;
            chainFeet[i].SetActive(active);
            if (!active) continue;
            Vector3 from = joints[i], to = joints[i + 1];
            Vector3 dir = to - from;
            float length = dir.magnitude;
            if (length < 1e-4f) { chainFeet[i].SetActive(false); continue; }
            dir /= length;
            Vector3 side = Vector3.Cross(dir, Vector3.up).normalized * (i % 2 == 0 ? 1f : -1f);
            float scale = Mathf.Clamp(length * .34f, .04f, .3f);
            Vector3 centre = to - dir * scale * .6f + side * scale * .9f;
            LineRenderer sole = chainFeet[i].transform.GetChild(0).GetComponent<LineRenderer>();
            sole.startWidth = scale * .3f;
            sole.endWidth = scale * .3f;
            for (int k = 0; k < 14; k++)
            {
                float a = k / 14f * Mathf.PI * 2f;
                soleBuffer[k] = centre + dir * (Mathf.Cos(a) * scale * .5f) + side * (Mathf.Sin(a) * scale * .28f) + Vector3.up * .01f;
            }

            sole.SetPositions(soleBuffer);
        }
    }
    public void ShowNumberLine(bool on) => numberLineRoot.SetActive(on);
    public void ShowPoleLabel(bool on) => poleLabelRoot.SetActive(on);
    public void ShowCenter(bool on) => centerRoot.SetActive(on);

    void UpdateGhost()
    {
        string beta = ghostBeta.ToString("0.##", CultureInfo.InvariantCulture);
        ghostFormula.Text = $"wave n  ∝  x<sup><color=#FF5A6E>{beta}</color></sup>      exponent β = <color=#FF5A6E>{beta}</color>";
        ghostLabel.Text = ghostAllZeros
            ? $"every collision moved to a shrink rate of {beta}\neach wave now grows like x^{beta}"
            : $"if a zero sat at Re(s) = {beta}\nits wave would grow like x^{beta}";

        if (VRMode.Enabled)
        {
            // on a headset the curve is computed on a worker thread and picked up a frame or two later
            // (the desktop recorder keeps the synchronous path so its frames stay exactly reproducible)
            ghostDirty = true;
            StartGhostWorker();
            return;
        }

        Vector3[] pts = new Vector3[Main.NumPrimeCurvePoints];
        // use at least the first 40 zeros so the wobble is unmistakable even early in the tour
        IReadOnlyList<double> zeros = null;
        if (ghostAllZeros)
        {
            int count = System.Math.Max(main.PassedZeros.Count, 40);
            zeros = main.zetaZeroValues.Take(System.Math.Min(count, main.zetaZeroValues.Length)).ToList();
        }

        ComputeGhost(pts, ghostAllZeros, ghostBeta, ghostGamma, zeros, main.PsiValues);
        ghostCurve.SetPositions(pts);
    }

    static void ComputeGhost(Vector3[] pts, bool allZeros, double beta, double gamma, IReadOnlyList<double> zeros, float[] psiValues)
    {
        for (int i = 0; i < pts.Length; i++)
        {
            double x = Main.PrimeCurveX(i);
            double psi;
            if (allZeros)
            {
                // every collision moved from ½ to β: ψ(x) = x − ln 2π − Σ 2·Re(x^ρ/ρ)
                double sum = 0;
                for (int z = 0; z < zeros.Count; z++) sum += ZetaMath.OffLineZeroPairTerm(x, beta, zeros[z]);
                psi = x - System.Math.Log(2 * System.Math.PI) - sum;
            }
            else
            {
                psi = psiValues[i] - ZetaMath.OffLineZeroPairTerm(x, beta, gamma);
            }

            pts[i] = new Vector3((float)x, x < 2 ? 0f : (float)System.Math.Max(psi, -40), -0.05f);
        }
    }

    // ---- ghost curve on a worker thread (VR) ------------------------------------------------------------
    volatile Vector3[] ghostResult;
    volatile bool ghostBusy;
    bool ghostDirty;

    void StartGhostWorker()
    {
        if (ghostBusy || !ghostDirty) return;
        ghostDirty = false;
        ghostBusy = true;
        bool all = ghostAllZeros;
        double beta = ghostBeta, gamma = ghostGamma;
        float[] psi = (float[])main.PsiValues.Clone();
        List<double> zeros = null;
        if (all)
        {
            int count = System.Math.Max(main.PassedZeros.Count, 40);
            zeros = main.zetaZeroValues.Take(System.Math.Min(count, main.zetaZeroValues.Length)).ToList();
        }

        System.Threading.Tasks.Task.Run(() =>
        {
            Vector3[] pts = new Vector3[Main.NumPrimeCurvePoints];
            ComputeGhost(pts, all, beta, gamma, zeros, psi);
            ghostResult = pts;
            ghostBusy = false;
        });
    }

    void LateUpdate()
    {
        Vector3[] ready = ghostResult;
        if (ready == null) return;
        ghostResult = null;
        ghostCurve.SetPositions(ready);
        if (ghostDirty) StartGhostWorker();
    }

    // ---- toggles used by the tour ------------------------------------------------------------

    public void ShowStrip(bool on) => stripRoot.SetActive(on);
    public void ShowOutputAxes(bool on) => outputAxesRoot.SetActive(on);
    public void ShowImAxis(bool on) => imAxisRoot.SetActive(on);
    public void ShowZCurve(bool on) => zCurveRoot.SetActive(on);
    public void ShowBand78(bool on) => band78Root.SetActive(on);
    public void ShowBand18(bool on) => band18Root.SetActive(on);
    public void ShowClassic(bool on) => classicRoot.SetActive(on);
    public void ShowBand1112(bool on) => band1112Root.SetActive(on);
    public void ShowGap(bool on) => gapRoot.SetActive(on);

    public void SetGhostZero(bool on, double beta = 0.9, double gamma = 14.134725)
    {
        ghostOn = on;
        ghostAllZeros = false;
        ghostBeta = beta;
        ghostGamma = gamma;
        ghostRoot.SetActive(on);
        if (on) UpdateGhost();
    }

    /// <summary>Draw the staircase as if every zero found so far sat at Re(s) = β.</summary>
    public void SetGhostAllZeros(double beta)
    {
        ghostOn = true;
        ghostAllZeros = true;
        ghostBeta = beta;
        ghostRoot.SetActive(true);
        UpdateGhost();
    }

    /// <summary>Stop the animations of the annotations and the Sacks spiral (used when the tour jumps to another chapter).</summary>
    public void CancelAnimations()
    {
        StopAllCoroutines();
        if (Sacks != null) Sacks.StopAllCoroutines();
    }

    public void HideAll()
    {
        Lab.Hide();
        HideAngleStack();
        ShowMiss(false);
        ShowTip(false);
        ShowCenter(false);
        ShowNumberLine(false);
        ShowPoleLabel(false);
        ShowZeno(false);
        ShowLanding(false);
        ShowPrimes(false);
        Sacks.Hide();
        ShowChainFeet(false);
        ShowLogSequence(false);
        HideIncomingWave();
        ShowSquaresLabel(false);
        ShowPrimePowers(false);
        SetWaveCount(0);
        ShowAngles(false);
        ClearDialStrip();
        ShowPrimeArrows(false);
        ShowRootFormula("off");
        HideDemoWave();
        ShowDial(false);
        HideIncomingWave();
        ShowStrip(false);
        ShowOutputAxes(false);
        ShowImAxis(false);
        ShowZCurve(false);
        ShowBand78(false);
        ShowBand18(false);
        ShowClassic(false);
        ShowBand1112(false);
        ShowGap(false);
        SetGhostZero(false);
    }

    public void PulseZero(int index)
    {
        if (index < 0 || index >= main.ZeroMarkers.Count) return;
        StartCoroutine(Pulse(main.ZeroMarkers[index].transform, .3f, 3f, 1.2f));
    }

    public void PulseTail()
    {
        StartCoroutine(PulseWidth(main.Limit.TailCurve, .07f, .2f, 1.4f));
    }

    static IEnumerator Pulse(Transform target, float baseScale, float factor, float seconds)
    {
        float t = 0;
        while (t < seconds && target != null)
        {
            t += Time.deltaTime;
            float k = Mathf.Sin(t / seconds * Mathf.PI);
            target.localScale = Vector3.one * (baseScale * (1f + (factor - 1f) * k));
            yield return null;
        }

        if (target != null) target.localScale = Vector3.one * baseScale;
    }

    static IEnumerator PulseWidth(LineRenderer line, float baseWidth, float peak, float seconds)
    {
        float t = 0;
        while (t < seconds && line != null)
        {
            t += Time.deltaTime;
            float k = Mathf.Sin(t / seconds * Mathf.PI);
            float w = Mathf.Lerp(baseWidth, peak, k);
            line.startWidth = w;
            line.endWidth = w;
            yield return null;
        }

        if (line != null)
        {
            line.startWidth = baseWidth;
            line.endWidth = baseWidth;
        }
    }
}
