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
    GameObject tipSphere, centerSphere;
    TextBox dialLabel;
    LineRenderer centerRing;

    public static Annotations Create(Main main)
    {
        GameObject go = new("Annotations");
        Annotations a = go.AddComponent<Annotations>();
        a.main = main;
        a.Build();
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
        baseline.SetPositions(new[] { new Vector3(0, 0, 0.05f), new Vector3(24, 0, 0.05f) });
        for (int n = 1; n <= 24; n++)
        {
            bool prime = n is 2 or 3 or 5 or 7 or 11 or 13 or 17 or 19 or 23;
            bool primePower = n is 4 or 8 or 9 or 16;
            LineRenderer tick = Factory.NewLine("tick", .03f, new Color(.6f, .6f, .65f));
            tick.transform.SetParent(numberLineRoot.transform, false);
            tick.positionCount = 2;
            tick.SetPositions(new[] { new Vector3(n, -.25f, 0.05f), new Vector3(n, .25f, 0.05f) });
            Color c = prime ? Factory.Palette.Dagger : primePower ? new Color(1f, .7f, .5f) : new Color(.55f, .55f, .6f);
            TextBox label = Label(numberLineRoot.transform, n.ToString(), new Vector3(n, -1.7f, 0), prime ? 4.2f : 2.8f, c, TextAlignmentOptions.Center);
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
            LineRenderer seg = Factory.NewLine("zeno " + i, Mathf.Lerp(.06f, .02f, i / 7f), c, Factory.SoftGlowIntensity);
            seg.transform.SetParent(arrow.transform, false);
            seg.positionCount = 2;
            seg.numCapVertices = 4;
            seg.SetPositions(new[] { from, to });
            float size = Mathf.Clamp(length * .28f, .03f, .13f);
            LineRenderer head = Factory.NewLine("zeno head", Mathf.Clamp(length * .07f, .012f, .045f), c, Factory.SoftGlowIntensity);
            head.transform.SetParent(arrow.transform, false);
            head.positionCount = 3;
            head.SetPositions(new[] { to - Vector3.right * size + Vector3.forward * size * .55f, to, to - Vector3.right * size - Vector3.forward * size * .55f });
            zenoArrows.Add(arrow);
            if (i < 4)
            {
                TextBox label = Label(zenoRoot.transform, fractions[i], (from + to) * .5f + Vector3.forward * .4f, 1.6f, new Color(1f, 1f, 1f, .85f), TextAlignmentOptions.Center);
                label.gameObject.AddComponent<Billboard>();
                zenoLabels.Add(label.gameObject);
            }

            xPos += length;
        }

        // footprints at the end of each step, alternating left/right foot, with "step n" labels
        float fx = 0f;
        for (int i = 0; i < 6; i++)
        {
            float length = Mathf.Pow(0.5f, i);
            fx += length;
            float side = i % 2 == 0 ? .28f : -.28f;
            GameObject foot = new GameObject("footprint " + i);
            foot.transform.SetParent(zenoRoot.transform, false);
            float scale = Mathf.Clamp(length * .5f, .08f, .22f);
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
            if (i < 3)
            {
                TextBox step = Label(zenoRoot.transform, "step " + (i + 1), new Vector3(fx - length * .5f, 0, zenoZ - .95f), 1.3f, new Color(.8f, .8f, .9f), TextAlignmentOptions.Center);
                step.gameObject.AddComponent<Billboard>();
                zenoStepLabels.Add(step.gameObject);
            }
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

        // ---- primes intro grid (1..30) ---------------------------------------------------------
        primesRoot = new GameObject("PrimesGrid");
        primesRoot.transform.SetParent(transform, false);
        Vector3 gridOrigin = new(-40f, 26f, 0f);
        const float cell = 3.0f;
        for (int n = 1; n <= 30; n++)
        {
            int col = (n - 1) % 5, row = (n - 1) / 5;
            Vector3 pos = gridOrigin + new Vector3(col * cell, -row * cell, 0);
            bool prime = IsPrime(n);
            TextBox label = Label(primesRoot.transform, n.ToString(), pos, prime ? 4.6f : 3.4f, new Color(.45f, .45f, .5f), TextAlignmentOptions.Center);
            GameObject glow = Factory.NewSphere("prime glow", 0.001f, new Color(1f, .8f, .2f), Factory.SoftGlowIntensity);
            glow.transform.SetParent(primesRoot.transform, false);
            glow.transform.position = pos + Vector3.forward * .9f;
            glow.SetActive(false);
            if (!prime && n > 1)
            {
                TextBox fac = Label(primesRoot.transform, Factorization(n), pos + Vector3.down * 1.0f, 1.5f, new Color(.55f, .55f, .65f), TextAlignmentOptions.Center);
                fac.gameObject.SetActive(false);
                primeCells.Add((-n, fac, null));
            }

            primeCells.Add((n, label, glow));
        }

        primesRoot.SetActive(false);

        // ---- joint angle arcs ------------------------------------------------------------------
        anglesRoot = new GameObject("JointAngles");
        anglesRoot.transform.SetParent(transform, false);
        for (int i = 0; i < 8; i++)
        {
            LineRenderer arc = Factory.NewLine("arc", .025f, new Color(1f, .5f, .9f), Factory.GlowIntensity);
            arc.transform.SetParent(anglesRoot.transform, false);
            angleArcs.Add(arc);
            TextBox l = Label(anglesRoot.transform, "", Vector3.zero, 1.0f, new Color(1f, .6f, .95f), TextAlignmentOptions.Center);
            l.gameObject.AddComponent<Billboard>();
            angleLabels.Add(l);
        }

        angleFormula = Label(anglesRoot.transform, "", Vector3.zero, 1.5f, new Color(1f, .6f, .95f), TextAlignmentOptions.Left);
        angleFormula.gameObject.AddComponent<Billboard>();
        anglesRoot.SetActive(false);

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

        if (gapRoot.activeSelf)
        {
            float pulse = .14f + .08f * Mathf.Sin(Time.time * 2.5f);
            Factory.SetColor(gapQuad, new Color(1f, .5f, .2f, pulse));
        }

        if (tipRoot.activeSelf)
        {
            Vector3[] joints = main.Limit.LastJoints;
            Vector3 tip = main.Limit.ShowTail && main.Limit.LastDecomposition.PartialSums != null
                ? LimitCurve.ToWorld(main.Limit.LastDecomposition.Total, (float)main.Limit.LastT * Graph.yScale)
                : joints.Length > 0 ? joints[^1] : Vector3.zero;
            tipSphere.transform.position = tip;
        }

        if (centerRoot.activeSelf)
        {
            float y = (float)t * Graph.yScale;
            Vector3 c = new(0, y, 0);
            centerSphere.transform.position = c;
            float r = .5f;
            Vector3[] ring = new Vector3[centerRing.positionCount];
            for (int i = 0; i < ring.Length; i++)
            {
                float a = i / (float)ring.Length * Mathf.PI * 2f;
                ring[i] = c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
            }

            centerRing.SetPositions(ring);
            double distance = main.Limit.LastDecomposition.PartialSums != null ? ComplexMath.Abs(main.Limit.LastDecomposition.Total) : 0;
            dialLabel.Text = $"dial  t = {t:0.00}\ntip to centre  {distance:0.00}";
            // the first arrow always points along +x, so the −x side stays free
            dialLabel.transform.position = c + new Vector3(-1.1f, .05f, -0.9f);
        }
    }

    public void ShowTip(bool on) => tipRoot.SetActive(on);
    public void ShowZeno(bool on) => zenoRoot.SetActive(on);

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
        primesRoot.SetActive(on);
        if (on)
        {
            foreach ((int n, TextBox label, GameObject glow) in primeCells)
            {
                if (n < 0) label.gameObject.SetActive(false);
                else { label.Color = new Color(.35f, .35f, .4f); glow?.SetActive(false); }
            }
        }
    }

    /// <summary>Light the primes one by one; composites fade and show their factors.</summary>
    public void BuildPrimes(float seconds)
    {
        StartCoroutine(BuildPrimesRoutine(seconds));
    }

    IEnumerator BuildPrimesRoutine(float seconds)
    {
        ShowPrimes(true);
        List<(int n, TextBox label, GameObject glow)> cells = primeCells.FindAll(c => c.n > 0);
        float per = seconds / cells.Count;
        foreach ((int n, TextBox label, GameObject glow) in cells)
        {
            if (IsPrime(n))
            {
                label.Color = Factory.Palette.Zero;
                glow.SetActive(true);
                StartCoroutine(ShrinkGlow(glow.transform, 1.6f, .18f, .7f));
            }
            else
            {
                label.Color = new Color(.28f, .28f, .34f);
                (int fn, TextBox fac, GameObject _) = primeCells.Find(c => c.n == -n);
                if (fac != null) fac.gameObject.SetActive(true);
            }

            yield return new WaitForSeconds(per);
        }
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
    public void ShowAngles(bool on)
    {
        showAngles = on;
        anglesRoot.SetActive(on);
        if (on) UpdateAngles();
    }

    void UpdateAngles()
    {
        Vector3[] joints = main.Limit.LastJoints;
        double t = main.Limit.LastT;
        int count = Mathf.Min(angleArcs.Count, joints.Length - 2);
        for (int i = 0; i < angleArcs.Count; i++)
        {
            bool active = i < count;
            angleArcs[i].enabled = active;
            angleLabels[i].gameObject.SetActive(active);
            if (!active) continue;
            // arc at joint i+1 between the incoming arrow (i → i+1) and the outgoing arrow (i+1 → i+2)
            Vector3 j = joints[i + 1];
            Vector3 a = joints[i + 1] - joints[i];
            Vector3 b = joints[i + 2] - joints[i + 1];
            if (a.sqrMagnitude < 1e-6f || b.sqrMagnitude < 1e-6f) { angleArcs[i].enabled = false; continue; }
            float r = Mathf.Clamp(Mathf.Min(a.magnitude, b.magnitude) * .45f, .05f, .3f);
            float a0 = Mathf.Atan2(a.z, a.x), a1 = Mathf.Atan2(b.z, b.x);
            float delta = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            const int segs = 12;
            Vector3[] pts = new Vector3[segs + 1];
            for (int k = 0; k <= segs; k++)
            {
                float ang = a0 + delta * k / segs;
                pts[k] = j + new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * r + Vector3.up * .02f;
            }

            angleArcs[i].positionCount = pts.Length;
            angleArcs[i].SetPositions(pts);
            int n = i + 2; // the outgoing arrow is arrow n
            double turn = t * System.Math.Log((double)n / (n - 1)) * 180 / System.Math.PI;
            angleLabels[i].Text = $"{turn:0}°";
            float mid = a0 + delta * .5f;
            angleLabels[i].transform.position = j + new Vector3(Mathf.Cos(mid), 0, Mathf.Sin(mid)) * (r + .22f);
        }

        angleFormula.Text = $"dial t = {t:0.00}\nturn at joint n = t · log( n / (n−1) )\njoint 2: {t * System.Math.Log(2) * 180 / System.Math.PI:0}°   joint 3: {t * System.Math.Log(1.5) * 180 / System.Math.PI:0}°   joint 4: {t * System.Math.Log(4.0 / 3) * 180 / System.Math.PI:0}°";
        angleFormula.transform.position = new Vector3(-2.3f, (float)t * Graph.yScale + .05f, 2.5f);
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
        Color accent = index == 0 ? Factory.Palette.Cosine : new Color(1f, .35f, .85f);
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
        rootFormula.transform.position = new Vector3(0.3f, y + .05f, 1.55f);
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
    void DrawDemoWave()
    {
        Vector3[] pts = new Vector3[Main.NumPrimeCurvePoints];
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
            }

            if (i < zenoStepLabels.Count) zenoStepLabels[i].SetActive(true);
            yield return new WaitForSeconds(per * .2f);
        }

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
        MaterialPropertyBlock block = new();
        r.GetPropertyBlock(block);
        float baseIntensity = block.GetFloat("_Intensity");
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
            "primes" => primesRoot, "angles" => anglesRoot, "dialstrip" => dialStripRoot, "primearrows" => primeArrowsRoot,
            _ => null
        };
        if (root == null) yield break;
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>()) if (r is LineRenderer or MeshRenderer) yield return r;
    }

    /// <summary>Draw collision number <paramref name="index"/>'s wave by itself, before it joins the chorus.</summary>
    public void ShowIncomingWave(int index, float amplitude, float lift)
    {
        if (index < 0 || index >= main.zetaZeroValues.Length) return;
        double gamma = main.zetaZeroValues[index];
        Vector3[] pts = new Vector3[Main.NumPrimeCurvePoints];
        for (int i = 0; i < pts.Length; i++)
        {
            float x = (float)Main.PrimeCurveX(i);
            pts[i] = x < 1.5f ? new Vector3(1.5f, lift, 0.02f) : new Vector3(x, lift - amplitude * (float)System.Math.Cos(gamma * System.Math.Log(x)), 0.02f);
        }

        incomingWave.SetPositions(pts);
        incomingLabel.Text = $"collision {index + 1}:  {gamma:0.00}";
        incomingLabel.transform.position = new Vector3(8f, lift + amplitude + 1.6f, 0);
        incomingRoot.SetActive(true);
    }

    public void HideIncomingWave() => incomingRoot.SetActive(false);
    public void ShowNumberLine(bool on) => numberLineRoot.SetActive(on);
    public void ShowPoleLabel(bool on) => poleLabelRoot.SetActive(on);
    public void ShowCenter(bool on) => centerRoot.SetActive(on);

    void UpdateGhost()
    {
        Vector3[] pts = new Vector3[Main.NumPrimeCurvePoints];
        // use at least the first 40 zeros so the wobble is unmistakable even early in the tour
        IReadOnlyList<double> zeros = null;
        if (ghostAllZeros)
        {
            int count = System.Math.Max(main.PassedZeros.Count, 40);
            zeros = main.zetaZeroValues.Take(System.Math.Min(count, main.zetaZeroValues.Length)).ToList();
        }
        string beta = ghostBeta.ToString("0.##", CultureInfo.InvariantCulture);
        for (int i = 0; i < Main.NumPrimeCurvePoints; i++)
        {
            double x = Main.PrimeCurveX(i);
            double psi;
            if (ghostAllZeros)
            {
                // every collision moved from ½ to β: ψ(x) = x − ln 2π − Σ 2·Re(x^ρ/ρ)
                double sum = 0;
                for (int z = 0; z < zeros.Count; z++) sum += ZetaMath.OffLineZeroPairTerm(x, ghostBeta, zeros[z]);
                psi = x - System.Math.Log(2 * System.Math.PI) - sum;
            }
            else
            {
                psi = main.PsiValues[i] - ZetaMath.OffLineZeroPairTerm(x, ghostBeta, ghostGamma);
            }

            pts[i] = x < 2 ? Vector3.zero : new Vector3((float)x, (float)System.Math.Max(psi, -40), -0.05f);
        }

        ghostCurve.SetPositions(pts);
        ghostFormula.Text = $"wave n  ∝  x<sup><color=#FF5A6E>{beta}</color></sup>      exponent β = <color=#FF5A6E>{beta}</color>";
        ghostLabel.Text = ghostAllZeros
            ? $"every collision moved to a shrink rate of {beta}\neach wave now grows like x^{beta}"
            : $"if a zero sat at Re(s) = {beta}\nits wave would grow like x^{beta}";
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

    public void HideAll()
    {
        ShowTip(false);
        ShowCenter(false);
        ShowNumberLine(false);
        ShowPoleLabel(false);
        ShowZeno(false);
        ShowPrimes(false);
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
