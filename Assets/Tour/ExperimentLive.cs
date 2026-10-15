using System.Collections;
using System.Collections.Generic;
using Meta.Numerics;
using TMPro;
using UnityEngine;

/// <summary>
/// The moving pictures of the vortex module (Mirrors, not oracles): the chain redrawn every frame as the dial turns,
/// with its vortex eyes detected live from the turning angles (so they can be seen forming, drifting and settling),
/// the mirror points of the chain's first vertices, and the zero the dial is passing; and the vortex tower grown zero
/// by zero, with the live chain climbing through the zero planes and leaving the eye traces behind it.
/// Lives on its own root so the static experiment builders can clear and rebuild around it.
/// </summary>
public partial class SacksSpiral
{
    GameObject liveRoot;
    Coroutine liveRoutine;
    LineRenderer liveChain, liveTip;
    GameObject liveZeta;
    TextBox liveDial, liveZero;
    readonly List<LineRenderer> liveRings = new(), liveCrosses = new();
    readonly List<TextBox> liveRingLabels = new();
    float liveScale = -1f;
    Vector3 liveShift;
    // the t axis the panel climbs: a pole through the chain's origin, the zeros marked on it
    GameObject liveAxis;
    double liveAxisLo, liveAxisHi;
    const float LiveRise = 1.2f; // height per unit of t

    /// <summary>How far the live chain's plane has climbed its t axis (the camera follows it).</summary>
    public float ExpPanelHeight { get; private set; }

    /// <summary>The dial value the live chain last showed (the next sweep can start from it).</summary>
    public double ExpLiveT { get; private set; } = -1d;

    /// <summary>Height above the tower's foot of the plane the live chain is in (the camera climbs with it).</summary>
    public float ExpLiveHeight { get; private set; }

    Transform LiveRoot()
    {
        if (liveRoot == null)
        {
            liveRoot = new GameObject("Experiments live");
            liveRoot.transform.SetParent(transform, false);
        }

        return liveRoot.transform;
    }

    public void ExpStopLive()
    {
        if (liveRoutine != null) StopCoroutine(liveRoutine);
        liveRoutine = null;
        if (liveRoot != null) Destroy(liveRoot);
        liveRoot = null;
        liveChain = liveTip = null;
        liveZeta = null;
        liveDial = liveZero = null;
        liveRings.Clear();
        liveCrosses.Clear();
        liveRingLabels.Clear();
        liveScale = -1f;
        liveAxis = null;
        ExpLiveHeight = 0f;
        ExpPanelHeight = 0f;
        Board("");
    }

    void StartLive(IEnumerator routine)
    {
        if (liveRoutine != null) StopCoroutine(liveRoutine);
        liveRoutine = StartCoroutine(routine);
    }

    // ---- the flat chain on the panel -------------------------------------------------------------------------------

    /// <summary>
    /// Turn the dial from <paramref name="t0"/> to <paramref name="t1"/> over <paramref name="seconds"/> and redraw the
    /// chain every frame (eased in and out). <paramref name="rings"/> rings every detected eye, <paramref name="mirrors"/>
    /// adds the mirror crosses. The framing follows the chain smoothly.
    /// </summary>
    public void ExpChainLive(double t0, double t1, float seconds, Vector3 center, float halfWidth, float halfHeight, bool rings, bool mirrors, float labelSize)
    {
        StartLive(ChainLive(t0, t1, seconds, center, halfWidth, halfHeight, rings, mirrors, labelSize));
    }

    IEnumerator ChainLive(double t0, double t1, float seconds, Vector3 center, float halfWidth, float halfHeight, bool rings, bool mirrors, float labelSize)
    {
        EnsureLivePanel(center, halfWidth, halfHeight, labelSize);
        double lo = System.Math.Min(t0, t1), hi = System.Math.Max(t0, t1);
        if (liveAxis == null || lo < liveAxisLo || hi > liveAxisHi) BuildLiveAxis(lo - 1.0, hi + 2.5, labelSize);
        float time = 0f;
        while (true)
        {
            float u = seconds <= 0f ? 1f : Mathf.Clamp01(time / seconds);
            double t = t0 + (t1 - t0) * Mathf.SmoothStep(0f, 1f, u);
            DrawLivePanel(t, center, halfWidth, halfHeight, rings, mirrors, labelSize, Time.deltaTime);
            if (u >= 1f) break;
            time += Time.deltaTime;
            yield return null;
        }

        liveRoutine = null;
    }

    void EnsureLivePanel(Vector3 center, float halfWidth, float halfHeight, float labelSize)
    {
        Transform g = LiveRoot();
        if (liveChain != null) return;
        liveChain = Factory.NewLine("live chain", .06f, Factory.Palette.Zeta, Factory.SoftGlowIntensity);
        liveChain.transform.SetParent(g, false);
        liveChain.numCornerVertices = 2;
        liveChain.startColor = Factory.Palette.Zeta;
        liveChain.endColor = Factory.Palette.Tail;
        liveChain.widthCurve = new AnimationCurve(new Keyframe(0f, .08f), new Keyframe(1f, .035f));
        liveTip = Factory.NewLine("origin", .05f, Color.white, Factory.GlowIntensity);
        liveTip.transform.SetParent(g, false);
        liveZeta = Factory.NewSphere("zeta", .45f, Factory.Palette.Zero, Factory.GlowIntensity);
        liveZeta.transform.SetParent(g, false);
        liveScale = -1f;
    }

    /// <summary>The t axis from <paramref name="lo"/> to <paramref name="hi"/>: a pole along the saddle normal with every zero in range marked and labelled.</summary>
    void BuildLiveAxis(double lo, double hi, float labelSize)
    {
        if (liveAxis != null) Destroy(liveAxis);
        liveAxis = new GameObject("t axis");
        liveAxis.transform.SetParent(LiveRoot(), false);
        liveAxisLo = lo;
        liveAxisHi = hi;
        float top = (float)(hi - lo) * LiveRise;
        LineRenderer pole = Factory.NewLine("pole", .08f, Factory.Palette.ImAxis, Factory.SoftGlowIntensity);
        pole.transform.SetParent(liveAxis.transform, false);
        pole.useWorldSpace = false;
        pole.positionCount = 2;
        pole.SetPositions(new[] { Vector3.zero, Vector3.back * top });
        TextBox name = TextBoxLabel("t", labelSize * .45f, Factory.Palette.ImAxis);
        name.transform.SetParent(liveAxis.transform, false);
        name.transform.localPosition = Vector3.back * (top + 1.2f);
        double[] zs = Main.Instance.zetaZeroValues;
        int labelled = 0;
        for (int j = 0; j < zs.Length && zs[j] <= hi; j++)
        {
            if (zs[j] < lo) continue;
            float h = (float)(zs[j] - lo) * LiveRise;
            GameObject dot = Factory.NewSphere("zero " + (j + 1), .5f, Factory.Palette.Zero, Factory.GlowIntensity);
            dot.transform.SetParent(liveAxis.transform, false);
            dot.transform.localPosition = Vector3.back * h;
            if (labelled++ < 12)
            {
                TextBox l = TextBoxLabel($"zero #{j + 1}   t = {zs[j]:0.000}", labelSize * .3f, Factory.Palette.Zero);
                l.transform.SetParent(liveAxis.transform, false);
                l.Alignment = TextAlignmentOptions.Left;
                l.transform.localPosition = Vector3.back * h + Vector3.right * 1.2f;
            }
        }
    }

    LineRenderer PoolLine(List<LineRenderer> pool, int i, string name, float width)
    {
        while (pool.Count <= i)
        {
            LineRenderer l = Factory.NewLine(name + " " + pool.Count, width, Color.white, Factory.GlowIntensity);
            l.transform.SetParent(LiveRoot(), false);
            l.numCornerVertices = 2;
            pool.Add(l);
        }

        pool[i].enabled = true;
        return pool[i];
    }

    TextBox PoolLabel(int i, float size)
    {
        while (liveRingLabels.Count <= i)
        {
            TextBox l = TextBoxLabel("", size, Color.white);
            l.transform.SetParent(LiveRoot(), false);
            liveRingLabels.Add(l);
        }

        liveRingLabels[i].gameObject.SetActive(true);
        return liveRingLabels[i];
    }

    static readonly Vector3[] circle28 = BuildCircle(28);

    static Vector3[] BuildCircle(int n)
    {
        Vector3[] c = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            c[i] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
        }

        return c;
    }

    readonly Vector3[] liveRingBuffer = new Vector3[28];

    void SetRing(LineRenderer l, Vector3 c, float r, Color col)
    {
        for (int i = 0; i < 28; i++) liveRingBuffer[i] = c + circle28[i] * r;
        l.loop = true;
        l.positionCount = 28;
        l.SetPositions(liveRingBuffer);
        l.startColor = l.endColor = col;
    }

    void DrawLivePanel(double t, Vector3 baseCenter, float halfWidth, float halfHeight, bool rings, bool mirrors, float labelSize, float dt)
    {
        ExpLiveT = t;
        ExpPanelHeight = (float)(t - liveAxisLo) * LiveRise;
        Vector3 center = baseCenter + Vector3.back * ExpPanelHeight;
        int N = (int)(t / System.Math.PI) + 12;
        Complex[] S = ChainSums(t, N);
        double minRe = 0, maxRe = 0, minIm = 0, maxIm = 0;
        for (int n = 1; n <= N; n++)
        {
            minRe = System.Math.Min(minRe, S[n].Re); maxRe = System.Math.Max(maxRe, S[n].Re);
            minIm = System.Math.Min(minIm, S[n].Im); maxIm = System.Math.Max(maxIm, S[n].Im);
        }

        // the framing eases toward the chain's box, so the picture breathes with it instead of jumping
        float targetScale = Mathf.Min(halfWidth * 2f / (float)(maxRe - minRe + 1e-6), halfHeight * 2f / (float)(maxIm - minIm + 1e-6));
        Vector3 targetShift = new((float)(-(minRe + maxRe) * .5), (float)(-(minIm + maxIm) * .5), 0f);
        if (liveScale < 0f)
        {
            liveScale = targetScale;
            liveShift = targetShift;
        }
        else
        {
            float k = 1f - Mathf.Exp(-dt * 30f); // the box moves continuously with t: follow it almost exactly, just without a hard edge
            liveScale = Mathf.Lerp(liveScale, targetScale, k);
            liveShift = Vector3.Lerp(liveShift, targetShift, k);
        }

        float scale = liveScale;
        Vector3 o = center + liveShift * scale;
        if (liveAxis != null) liveAxis.transform.position = new Vector3(o.x, o.y, baseCenter.z);
        Vector3 P(Complex z) => o + new Vector3((float)z.Re * scale, (float)z.Im * scale, 0f);
        Vector3[] pts = new Vector3[N + 1];
        pts[0] = o;
        for (int n = 1; n <= N; n++) pts[n] = P(S[n]);
        liveChain.positionCount = pts.Length;
        liveChain.SetPositions(pts);
        liveTip.positionCount = 5;
        liveTip.SetPositions(new[] { o + Vector3.left * .6f, o + Vector3.right * .6f, o, o + Vector3.up * .6f, o + Vector3.down * .6f });

        Complex zeta = ZetaMath.Decompose(new Complex(.5, t), ZetaMath.TermCount(t)).Total;
        liveZeta.transform.position = P(zeta) + Vector3.back * .05f;

        // where the dial is among the zeros
        double[] zs = Main.Instance.zetaZeroValues;
        int j = System.Array.BinarySearch(zs, t);
        if (j < 0) j = ~j;
        int near = j;
        if (j > 0 && (j >= zs.Length || System.Math.Abs(zs[j - 1] - t) < System.Math.Abs(zs[j] - t))) near = j - 1;
        double gap = System.Math.Abs(zs[near] - t);
        bool atZero = gap < 0.004;
        // the dial on the screen's board: where t is, and the zero it is at or between
        string gold = ColorUtility.ToHtmlStringRGB(Factory.Palette.Zero);
        Board($"t = {t:0.000}\n<size=58%>" + (atZero
            ? $"<color=#{gold}>zero #{near + 1}:  ζ(½ + it) = 0, the gold centre is on the axis</color>"
            : $"between zeros #{System.Math.Max(1, j)} and #{j + 1}:  the gold centre ζ(½ + it) is off the axis") + "</size>");
        liveZeta.transform.localScale = Vector3.one * (atZero ? .8f : .45f);

        int used = 0, crosses = 0;
        if (rings)
        {
            foreach (Vortex v in DetectVortices(t, 0, 12, zeta))
            {
                Color c = VortexColor(v.K, v.Certainty);
                Vector3 e = P(v.Eye) + Vector3.back * .02f;
                float r = .25f + .4f * v.Certainty;
                SetRing(PoolLine(liveRings, 2 * used, "eye", .05f), e, r, c);
                SetRing(PoolLine(liveRings, 2 * used + 1, "eye inner", .035f), e, r * .5f, c);
                TextBox lab = PoolLabel(used, labelSize * .3f);
                lab.Text = $"{v.K}  ({v.Curl} links)";
                lab.Color = c;
                lab.transform.position = e + Vector3.up * (r + .12f * labelSize);
                used++;
                if (mirrors && v.K >= 1)
                {
                    Vector3 m = P(v.Mirror) + Vector3.back * .03f;
                    LineRenderer a = PoolLine(liveCrosses, 2 * crosses, "mirror", .04f), b = PoolLine(liveCrosses, 2 * crosses + 1, "mirror", .04f);
                    a.loop = b.loop = false;
                    a.positionCount = b.positionCount = 2;
                    a.SetPositions(new[] { m + new Vector3(-.32f, -.32f, 0), m + new Vector3(.32f, .32f, 0) });
                    b.SetPositions(new[] { m + new Vector3(-.32f, .32f, 0), m + new Vector3(.32f, -.32f, 0) });
                    a.startColor = a.endColor = b.startColor = b.endColor = Color.Lerp(c, Color.white, .35f);
                    crosses++;
                }
            }
        }

        for (int i = 2 * used; i < liveRings.Count; i++) liveRings[i].enabled = false;
        for (int i = used; i < liveRingLabels.Count; i++) liveRingLabels[i].gameObject.SetActive(false);
        for (int i = 2 * crosses; i < liveCrosses.Count; i++) liveCrosses[i].enabled = false;
    }

    // ---- the tower, grown zero by zero -----------------------------------------------------------------------------

    /// <summary>
    /// Grow the vortex tower of zeros <paramref name="first"/> … <paramref name="last"/> over <paramref name="seconds"/>:
    /// the chain at the current dial value is drawn flat in its own plane at its height and climbs with the dial; every
    /// zero it reaches adds its plane, its eyes and the next piece of each trace, and is labelled as it is passed.
    /// </summary>
    public void ExpTowerGrow(int first, int last, float seconds, Vector3 center, float xyScale, System.Func<double, float> heightOfT, float labelSize)
    {
        StartLive(TowerGrow(first, last, seconds, center, xyScale, heightOfT, labelSize));
    }

    IEnumerator TowerGrow(int first, int last, float seconds, Vector3 center, float xyScale, System.Func<double, float> heightOfT, float labelSize)
    {
        double[] zs = Main.Instance.zetaZeroValues;
        List<Vortex> all = ExpVortices(zs);
        Transform g = LiveRoot();
        LineRenderer chain = Factory.NewLine("climbing chain", .05f * xyScale, Factory.Palette.Zeta, Factory.GlowIntensity);
        chain.transform.SetParent(g, false);
        chain.numCornerVertices = 2;
        chain.startColor = Factory.Palette.Zeta;
        chain.endColor = Factory.Palette.Tail;
        chain.widthCurve = new AnimationCurve(new Keyframe(0f, .028f * xyScale), new Keyframe(1f, .012f * xyScale));
        LineRenderer planeRing = Factory.NewLine("current plane", .02f * xyScale, Color.white, Factory.SoftGlowIntensity);
        planeRing.transform.SetParent(g, false);
        TextBox zeroLabel = TextBoxLabel("", labelSize * .8f, Factory.Palette.Zero);
        zeroLabel.transform.SetParent(g, false);
        TextBox dialLabel = TextBoxLabel("", labelSize * .7f, Color.white);
        dialLabel.transform.SetParent(g, false);

        Transform built = null;
        int builtTo = -1;
        float h0 = heightOfT(zs[first - 1]);
        float time = 0f;
        while (true)
        {
            float u = seconds <= 0f ? 1f : Mathf.Clamp01(time / seconds);
            float idx = Mathf.Lerp(first, last, u);
            int lo = Mathf.Clamp(Mathf.FloorToInt(idx), first, last);
            int hi = Mathf.Min(lo + 1, last);
            double t = zs[lo - 1] + (zs[hi - 1] - zs[lo - 1]) * (idx - lo);
            if (lo != builtTo)
            {
                if (built != null) Destroy(built.gameObject);
                built = ExpVortexTower(all, first, lo, center, xyScale, heightOfT, false, true, lo > first, true, 8, 0f);
                built.SetParent(g, true);
                builtTo = lo;
                zeroLabel.Text = $"zero #{lo}   t = {zs[lo - 1]:0.000}";
                zeroLabel.transform.position = center + Vector3.back * heightOfT(zs[lo - 1]) + Vector3.right * (3.2f * xyScale);
            }

            // the chain at the current dial value, flat in its plane
            int N = (int)(t / System.Math.PI) + 12;
            Complex[] S = ChainSums(t, N);
            float z = heightOfT(t);
            Vector3 foot = center + Vector3.back * z;
            Vector3[] pts = new Vector3[N + 1];
            for (int n = 0; n <= N; n++) pts[n] = foot + new Vector3((float)S[n].Re * xyScale, (float)S[n].Im * xyScale, 0f);
            chain.positionCount = pts.Length;
            chain.SetPositions(pts);
            for (int i = 0; i < 28; i++) liveRingBuffer[i] = foot + circle28[i] * (2.2f * xyScale);
            planeRing.loop = true;
            planeRing.positionCount = 28;
            planeRing.SetPositions(liveRingBuffer);
            dialLabel.Text = $"t = {t:0.00}";
            dialLabel.transform.position = foot + Vector3.left * (3.2f * xyScale);
            ExpLiveHeight = z - h0;
            if (u >= 1f) break;
            time += Time.deltaTime;
            yield return null;
        }

        liveRoutine = null;
    }
}
