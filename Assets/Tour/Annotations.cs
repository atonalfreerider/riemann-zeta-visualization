using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
    MeshRenderer gapQuad;

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
        Label(stripRoot.transform, "the critical line  Re(s) = ½", StripPoint(0.5, -1.2), 1.9f, Factory.Palette.CriticalLine, TextAlignmentOptions.Center);
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
        band78Root = Band("ZeroFree_7_8", 0.875, 1.0, height, "Re(s) > 7/8\nzero-free\n(OpenAI, Sept 2026 — Lean-checked claim)", TextAlignmentOptions.Left, 1.14);
        band18Root = Band("ZeroFree_1_8", 0.0, 0.125, height, "Re(s) < 1/8\nzero-free by symmetry\nζ(s) ↔ ζ(1 − s)", TextAlignmentOptions.Right, -0.14);
        band1112Root = DashedVertical("Line_11_12", 11d / 12d, height, "11/12 — companion proof", TextAlignmentOptions.Left, 24);
        classicRoot = ClassicRegion(height);
        gapRoot = new GameObject("OpenGap");
        gapRoot.transform.SetParent(stripRoot.transform, false);
        gapQuad = Factory.NewQuad("Gap", new Vector2(PanelWidth * .375f, height), new Color(1f, .5f, .2f, .18f));
        gapQuad.transform.SetParent(gapRoot.transform, false);
        gapQuad.transform.position = new Vector3(PanelLeft + PanelWidth * (0.5f + 0.375f * .5f), midY, 0.01f);
        Label(gapRoot.transform, "still open:\n½ < Re(s) ≤ 7/8\nthe Riemann Hypothesis lives here", StripPoint(0.6875, 12), 1.7f, new Color(1f, .7f, .4f), TextAlignmentOptions.Center);
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
        LineRenderer tAxis = Factory.NewLine("t axis", .07f, Factory.Palette.ImAxis, Factory.GlowIntensity);
        tAxis.transform.SetParent(imAxisRoot.transform, false);
        tAxis.positionCount = 2;
        tAxis.SetPositions(new[] { Vector3.zero, Vector3.up * (PanelMaxT * Graph.yScale) });
        Arrowhead(imAxisRoot.transform, Vector3.up * (PanelMaxT * Graph.yScale), Vector3.up, Factory.Palette.ImAxis, .4f);
        for (int t = 10; t < PanelMaxT; t += 10)
        {
            Label(imAxisRoot.transform, "t = " + t, new Vector3(.6f, t * Graph.yScale, 0), 1.8f, Factory.Palette.ImAxis, TextAlignmentOptions.Left);
        }

        Label(imAxisRoot.transform, "the imaginary axis of the input: t = Im(s)", new Vector3(.8f, 2f, 0), 2.2f, Factory.Palette.ImAxis, TextAlignmentOptions.Left);
        imAxisRoot.SetActive(false);

        // ---- rogue zero ---------------------------------------------------------------------
        ghostRoot = new GameObject("RogueZero");
        ghostRoot.transform.SetParent(transform, false);
        ghostCurve = Factory.NewLine("rogue psi", .06f, Factory.Palette.Ghost, Factory.GlowIntensity);
        ghostCurve.transform.SetParent(ghostRoot.transform, false);
        ghostCurve.positionCount = Main.NumPrimeCurvePoints;
        ghostLabel = Label(ghostRoot.transform, "", new Vector3(60, 85, 0), 3.5f, Factory.Palette.Ghost, TextAlignmentOptions.Center);
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
        Label(root.transform, text, StripPoint(labelSigma, 8), 1.7f, Factory.Palette.ZeroFree, align);
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

        Label(root.transform, text, StripPoint(1.14, labelT), 1.6f, new Color(.7f, .85f, 1f), align);
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

        if (gapRoot.activeSelf)
        {
            float pulse = .14f + .08f * Mathf.Sin(Time.time * 2.5f);
            Factory.SetColor(gapQuad, new Color(1f, .5f, .2f, pulse));
        }
    }

    void UpdateGhost()
    {
        Vector3[] pts = new Vector3[Main.NumPrimeCurvePoints];
        for (int i = 0; i < Main.NumPrimeCurvePoints; i++)
        {
            double x = Main.PrimeCurveX(i);
            float psi = main.PsiValues[i] - (float)ZetaMath.OffLineZeroPairTerm(x, ghostBeta, ghostGamma);
            pts[i] = x < 2 || psi < 0 ? Vector3.zero : new Vector3((float)x, psi, -0.05f);
        }

        ghostCurve.SetPositions(pts);
        ghostLabel.Text = $"if a zero sat at Re(s) = {ghostBeta.ToString("0.##", CultureInfo.InvariantCulture)}\nits wave would grow like x^{ghostBeta.ToString("0.##", CultureInfo.InvariantCulture)}";
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
        ghostBeta = beta;
        ghostGamma = gamma;
        ghostRoot.SetActive(on);
        if (on) UpdateGhost();
    }

    public void HideAll()
    {
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
