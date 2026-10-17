using System;
using System.Collections;
using System.Collections.Generic;
using Meta.Numerics;
using TMPro;
using UnityEngine;

/// <summary>
/// The partial-sum path of ζ (the long-form segments; what the earlier modules called the chain): the sums
/// S_k = Σ_{n ≤ k} n^(−½ − it) drawn link by link, link n being 1/√n long and pointing at −t·log n, closed by the
/// Euler–Maclaurin tail (ZetaMath.Decompose) at ζ(½ + it). Drawn at a fixed scale (the camera never re-fits it), with
/// parameters the script eases (exp:tw):
///   pw.t        the height t
///   pw.n        links shown (fractional: the last one grows), pw.tail (0 … 1: the tail drawn)
///   pw.marks    0 … 1: on the link being laid, its length, its turn off the last link (t·log(n/(n−1)), gold arc) and its
///               direction (t·log n, blue arc from the direction of link 1)
///   pw.rise     0: the plane lies flat; 1: it climbs the t axis (height t·PwRise), the t axis is drawn with every zero
///               marked, and ζ(½ + it) leaves a trail: the critical-line curve, climbing round the axis
///   pw.curve    0 … 1: the trail of ζ(½ + it) since pw.t0
///   pw.eyes     0 … 1: rings at the vortex eyes (DetectVortices); pw.trails 1: each eye leaves a trail up the axis
///   pw.gold     0 … 1: the links of n off E8's eight gold hours (gcd(n, 30) > 1) shrink away
///   pw.logh     0: the plane climbs t·PwRise; 1: it climbs PwLogH·log₁₀(1 + t), so t = 1 … 5000 fits on the screen
///   pw.ground   0 … 1: the ground, the complex plane at t = 0 (a grid with its axes), where the t axis stands; the t
///               axis carries ticks at t = 10, 100, 1000 (log) or every 100 (linear)
/// </summary>
public partial class SacksSpiral
{
    public const float PathWorldOffset = 104000f;
    const float PwRise = 1.1f, PwScale = 12f, PwLogH = 16f;

    class PwScene
    {
        public Transform Root, Plane;
        public LineRenderer Path, Hot, Tail, Extension, TurnArc, DirArc, DirRef, Axis, Curve;
        public GameObject Zeta;
        public TextBox Readout, Desc, LenTag, TurnTag, DirTag, ZetaTag;
        public readonly List<LineRenderer> Rings = new();
        public readonly List<TextBox> RingTags = new();
        public readonly List<LineRenderer> Crosses = new();
        public MeshRenderer AllEyes;
        public readonly List<(double t, Complex z)> CurvePts = new();
        public double CurveT = double.NaN, CurveStart = double.NaN;
        public readonly List<Vector3>[] TrailPts = new List<Vector3>[8];
        public MeshRenderer[] TrailR = new MeshRenderer[8];
        public LineRenderer[] TrailL = new LineRenderer[8];
        public double TrailT = double.NaN;
        public double LastT = double.NaN;
        public string Sig = "";
        public float AxisTop = -1f;
        public int BoardLink = -1;
        public Transform Ground, Ticks;
        public string TickSig = "";
    }

    PwScene pw;
    Complex[] pwBuffer = new Complex[64];

    public Vector3 PathOrigin => Center + new Vector3(PathWorldOffset, 0f, 0f);

    /// <summary>The middle of the path's plane where it is now (the camera's "pathplane" target).</summary>
    public Vector3 PathFocus => PathOrigin + Vector3.back * PathHeight(Tw("pw.t", 1f));

    float PathHeight(double t) => Mathf.Lerp((float)(t * PwRise), PwLogH * (float)Math.Log10(1.0 + Math.Max(0.0, t)), Tw("pw.logh", 0f)) * Tw("pw.rise", 0f);

    Vector3 PwP(Complex z, double t) => PathOrigin + new Vector3((float)z.Re, (float)z.Im, 0f) * PwScale + Vector3.back * PathHeight(t);

    /// <summary>Set up the path scene (axes in the plane, the path, the marks, the t axis) and start its frame loop.</summary>
    public void ExpPathWorld()
    {
        ClearTweens("pw.");
        pw = new PwScene();
        pw.Root = ExpGroup("PW path").transform;
        pw.Plane = new GameObject("plane").transform;
        pw.Plane.SetParent(pw.Root, false);
        pw.Plane.position = PathOrigin;
        // the plane's axes, ticks and numbers (they ride with the plane as it climbs)
        Color axis = new Color(.55f, .6f, .78f) * .75f, text = new(.72f, .76f, .9f);
        List<(Vector3, Vector3)> segs = new()
        {
            (new Vector3(-2.6f, 0f, 0f) * PwScale, new Vector3(3.4f, 0f, 0f) * PwScale),
            (new Vector3(0f, -3.4f, 0f) * PwScale, new Vector3(0f, 1.8f, 0f) * PwScale),
        };
        foreach (int k in new[] { -2, -1, 1, 2, 3 })
        {
            segs.Add((new Vector3(k, -.05f, 0f) * PwScale, new Vector3(k, .05f, 0f) * PwScale));
            TextBox l = ExpLabelWide(pw.Plane, k.ToString().Replace("-", "−"), Vector3.zero, 9f, text);
            l.transform.localPosition = new Vector3(k, -.17f, 0f) * PwScale;
        }

        foreach (int k in new[] { -3, -2, -1, 1 })
        {
            segs.Add((new Vector3(-.05f, k, 0f) * PwScale, new Vector3(.05f, k, 0f) * PwScale));
            TextBox l = ExpLabelWide(pw.Plane, $"{k}i".Replace("-", "−").Replace("1i", "i"), Vector3.zero, 9f, text, TextAlignmentOptions.Right);
            l.transform.localPosition = new Vector3(-.09f, k, 0f) * PwScale;
        }

        LineRenderer axes = ExpLine(pw.Plane, "axes", .1f, axis, Factory.NoGlow, new Vector3[0]);
        Destroy(axes.gameObject);
        foreach (var (a, b) in segs)
        {
            LineRenderer l = ExpLine(pw.Plane, "axis", .1f, axis, Factory.NoGlow, new[] { a, b });
            l.useWorldSpace = false;
        }

        TextBox zero = ExpLabelWide(pw.Plane, "0", Vector3.zero, 10f, Color.white);
        zero.transform.localPosition = new Vector3(-.09f, -.15f, 0f) * PwScale;
        // the path and its parts
        pw.Path = ExpLine(pw.Root, "path", .3f, new Color(.95f, .97f, 1f), Factory.SoftGlowIntensity, new[] { PathOrigin, PathOrigin });
        pw.Hot = ExpLine(pw.Root, "link", .5f, Color.white, AttentionIntensity * 1.2f, new[] { PathOrigin, PathOrigin });
        pw.Tail = ExpLine(pw.Root, "tail", .4f, new Color(1f, .55f, .2f), Factory.GlowIntensity, new[] { PathOrigin, PathOrigin });
        pw.Extension = ExpLine(pw.Root, "extension", .14f, new Color(1f, .86f, .45f) * .8f, Factory.SoftGlowIntensity, new[] { PathOrigin, PathOrigin });
        pw.TurnArc = ExpLine(pw.Root, "turn", .2f, new Color(1f, .82f, .3f), Factory.GlowIntensity, new[] { PathOrigin, PathOrigin });
        pw.DirRef = ExpLine(pw.Root, "direction of link 1", .1f, new Color(.45f, .75f, 1f) * .8f, Factory.SoftGlowIntensity, new[] { PathOrigin, PathOrigin });
        pw.DirArc = ExpLine(pw.Root, "direction", .16f, new Color(.45f, .75f, 1f), Factory.GlowIntensity, new[] { PathOrigin, PathOrigin });
        pw.Axis = ExpLine(pw.Root, "t axis", .2f, new Color(.6f, .64f, .85f), Factory.SoftGlowIntensity, new[] { PathOrigin, PathOrigin });
        pw.Curve = ExpLine(pw.Root, "zeta curve", .3f, Factory.Palette.Zero, Factory.GlowIntensity, new[] { PathOrigin, PathOrigin });
        pw.Zeta = Factory.NewSphere("zeta", 1.1f, Factory.Palette.Zero, AttentionIntensity);
        pw.Zeta.transform.SetParent(pw.Root, false);
        pw.Readout = ExpLabelWide(pw.Root, "", PathOrigin, 14f, Color.white, TextAlignmentOptions.Left);
        pw.Desc = ExpLabelWide(pw.Root, "", PathOrigin, 9f, new Color(1f, .9f, .6f), TextAlignmentOptions.Left);
        pw.LenTag = ExpLabelWide(pw.Root, "", PathOrigin, 9f, Color.white, TextAlignmentOptions.Center);
        pw.TurnTag = ExpLabelWide(pw.Root, "", PathOrigin, 9f, new Color(1f, .82f, .3f), TextAlignmentOptions.Center);
        pw.DirTag = ExpLabelWide(pw.Root, "", PathOrigin, 9f, new Color(.45f, .75f, 1f), TextAlignmentOptions.Center);
        pw.ZetaTag = ExpLabelWide(pw.Root, "", PathOrigin, 10f, Factory.Palette.Zero, TextAlignmentOptions.Left);
        foreach (TextBox t in new[] { pw.Readout, pw.Desc, pw.LenTag, pw.TurnTag, pw.DirTag, pw.ZetaTag }) Outline(t.TextField);
        for (int k = 0; k < 8; k++) pw.TrailPts[k] = new List<Vector3>();
        // the ground: the complex plane at t = 0, a faint grid with its axes, under the climbing plane
        pw.Ground = new GameObject("ground").transform;
        pw.Ground.SetParent(pw.Root, false);
        List<(Vector3, Vector3)> grid = new();
        for (float gx = -1.5f; gx <= 2.51f; gx += .5f) grid.Add((PathOrigin + new Vector3(gx, -2f, 0f) * PwScale, PathOrigin + new Vector3(gx, 1.5f, 0f) * PwScale));
        for (float gy = -2f; gy <= 1.51f; gy += .5f) grid.Add((PathOrigin + new Vector3(-1.5f, gy, 0f) * PwScale, PathOrigin + new Vector3(2.5f, gy, 0f) * PwScale));
        Segments(pw.Ground, "grid", grid, .08f, new Color(.4f, .45f, .62f) * .6f, Factory.NoGlow);
        Segments(pw.Ground, "axes", new List<(Vector3, Vector3)>
        {
            (PathOrigin + new Vector3(-1.5f, 0f, 0f) * PwScale, PathOrigin + new Vector3(2.5f, 0f, 0f) * PwScale),
            (PathOrigin + new Vector3(0f, -2f, 0f) * PwScale, PathOrigin + new Vector3(0f, 1.5f, 0f) * PwScale),
        }, .16f, new Color(.6f, .65f, .85f) * .8f, Factory.NoGlow);
        ExpDot(pw.Ground, "origin", PathOrigin + Vector3.back * .1f, .9f, Color.white, Factory.SoftGlowIntensity);
        TextBox ground = ExpLabelWide(pw.Ground, "the ground:  t = 0", PathOrigin + new Vector3(2.6f, -.15f, -.1f) * PwScale, 22f, new Color(.72f, .76f, .9f), TextAlignmentOptions.Left);
        Outline(ground.TextField);
        pw.Ground.gameObject.SetActive(false);
        pw.Ticks = new GameObject("t ticks").transform;
        pw.Ticks.SetParent(pw.Root, false);
        StartCoroutine(PathWorldLoop(pw));
    }

    IEnumerator PathWorldLoop(PwScene s)
    {
        while (s == pw && s.Root != null)
        {
            PwUpdate(s);
            yield return null;
        }
    }

    static bool GoldHour(int n) => n % 2 != 0 && n % 3 != 0 && n % 5 != 0;

    void PwUpdate(PwScene s)
    {
        double t = Tw("pw.t", 1f);
        float shown = Tw("pw.n", 1000f), tail = Tw("pw.tail", 1f), marks = Tw("pw.marks", 0f), rise = Tw("pw.rise", 0f);
        float curveVis = Tw("pw.curve", 0f), eyes = Tw("pw.eyes", 0f), trails = Tw("pw.trails", 0f), gold = Tw("pw.gold", 0f);
        float readout = Tw("pw.readout", 1f), axisVis = Tw("pw.axis", rise);
        // the path's width follows the zoom, so a close-up on a few tiny arrows stays readable
        Camera cam = Camera.main;
        if (cam != null)
        {
            // the distance to the part of the plane in view (not to the path's origin), so a close-up far from the origin
            // draws the path as thin as a wide shot does
            Vector3 cp = cam.transform.position, fwd = cam.transform.forward;
            float dist = Vector3.Distance(cp, PathFocus);
            if (Mathf.Abs(fwd.z) > .2f)
            {
                float along = (PathFocus.z - cp.z) / fwd.z;
                if (along > 0f) dist = Mathf.Min(dist, along);
            }

            s.Path.widthMultiplier = Mathf.Clamp(dist * .0028f / .3f, .01f, 1f);
        }

        // the readout, fixed in the top-left corner of the screen
        s.Readout.gameObject.SetActive(false);
        Hud(0, readout > .01f ? $"t = {t:0.00}" : "", new Color(1f, 1f, 1f, readout));

        string sig = $"{t:F5}|{shown:F3}|{tail:F3}|{marks:F3}|{rise:F3}|{curveVis:F3}|{eyes:F3}|{trails:F3}|{gold:F3}|{axisVis:F3}|{Tw("pw.mirrors", 0f):F3}|{Tw("pw.alleyes", 0f):F3}|{Tw("pw.logh", 0f):F3}|{Tw("pw.ground", 0f):F3}";
        if (sig == s.Sig) return;
        s.Sig = sig;
        float height = PathHeight(t);
        s.Plane.position = PathOrigin + Vector3.back * height;
        // the links: n^(−½ − it), the ones off the gold hours shrinking away with pw.gold
        int nMax = Mathf.Max(16, (int)(t / Math.PI) + 12);
        if (gold > 0f) nMax = Mathf.Max(nMax, (int)(30.0 * t / (2 * Math.PI) * 1.08) + 12);
        if (pwBuffer.Length < nMax + 2) pwBuffer = new Complex[nMax + 64];
        double re = 0, im = 0;
        pwBuffer[0] = Complex.Zero;
        for (int n = 1; n <= nMax; n++)
        {
            double w = gold > 0f && !GoldHour(n) ? 1.0 - gold : 1.0, a = -t * Math.Log(n), len = w / Math.Sqrt(n);
            re += len * Math.Cos(a);
            im += len * Math.Sin(a);
            pwBuffer[n] = new Complex(re, im);
        }

        int whole = Mathf.Clamp(Mathf.FloorToInt(shown), 0, nMax);
        float frac = whole < nMax ? Mathf.Clamp01(shown - whole) : 0f;
        List<Vector3> pts = new(whole + 2);
        for (int n = 0; n <= whole; n++) pts.Add(PwP(pwBuffer[n], t));
        Vector3 growing = Vector3.zero;
        if (frac > 0f && whole + 1 <= nMax)
        {
            growing = Vector3.Lerp(PwP(pwBuffer[whole], t), PwP(pwBuffer[whole + 1], t), Mathf.SmoothStep(0f, 1f, frac));
            pts.Add(growing);
        }

        s.Path.positionCount = pts.Count;
        s.Path.SetPositions(pts.ToArray());
        // the link being laid, white hot, with its marks
        int cur = frac > 0f ? whole + 1 : whole;
        bool building = shown < nMax - .001f && cur >= 1;
        s.Hot.gameObject.SetActive(building);
        if (building)
        {
            Vector3 a0 = PwP(pwBuffer[cur - 1], t), a1 = frac > 0f ? growing : PwP(pwBuffer[cur], t);
            s.Hot.SetPositions(new[] { a0 + Vector3.back * .05f, a1 + Vector3.back * .05f });
        }

        PwMarks(s, t, cur, marks * (building || shown <= nMax ? 1f : 0f), frac);
        // the tail and ζ(½ + it)
        bool full = shown >= nMax - .001f;
        Complex zeta = Complex.Zero;
        bool tailOn = full && tail > .001f && gold <= 0f;
        s.Tail.gameObject.SetActive(tailOn);
        s.Zeta.SetActive(tailOn && tail > .99f);
        s.ZetaTag.gameObject.SetActive(false);
        if (tailOn)
        {
            ZetaMath.SpiralDecomposition d = ZetaMath.Decompose(new Complex(.5, t), nMax + 1, pwBuffer2 ??= new Complex[4096]);
            zeta = d.Total;
            Vector3 e0 = PwP(pwBuffer[nMax], t), e1 = PwP(zeta, t);
            s.Tail.SetPositions(new[] { e0 + Vector3.back * .04f, Vector3.Lerp(e0, e1, Mathf.SmoothStep(0f, 1f, tail)) + Vector3.back * .04f });
            s.Zeta.transform.position = e1 + Vector3.back * .1f;
        }

        // the t readout, and the zeros passed (a flare as the curve crosses the axis)
        double[] zs = Main.Instance.zetaZeroValues;
        if (!double.IsNaN(s.LastT) && t > s.LastT)
            foreach (double z in zs)
            {
                if (z <= s.LastT) continue;
                if (z > t) break;
                Flare(PathOrigin + Vector3.back * (PathHeight(z) + .2f), 3.2f, 1.4f, 2);
            }

        s.LastT = t;
        // the t axis with its zeros
        bool axisOn = axisVis > .01f && rise > .01f;
        s.Axis.gameObject.SetActive(axisOn);
        if (axisOn)
        {
            float top = Mathf.Max(PathHeight(t) + 12f, s.AxisTop);
            s.AxisTop = top;
            s.Axis.SetPositions(new[] { PathOrigin, PathOrigin + Vector3.back * top });
            s.Axis.startColor = s.Axis.endColor = new Color(.6f, .64f, .85f, axisVis);
        }

        float groundVis = Tw("pw.ground", 0f);
        s.Ground.gameObject.SetActive(groundVis > .01f);
        if (groundVis > .01f)
            foreach (Renderer r in s.Ground.GetComponentsInChildren<Renderer>(true))
                if (r.GetComponent<TMP_Text>() == null) Factory.SetColor(r, new Color(1f, 1f, 1f, groundVis));
        PwTicks(s, t, axisOn ? axisVis : 0f);

        // the critical-line curve: ζ(½ + iτ) for τ from pw.t0 to t, in the climbing plane's frame
        PwCurve(s, t, curveVis);
        PwEyes(s, t, eyes, trails);
    }

    Complex[] pwBuffer2;

    /// <summary>Ticks on the t axis up to the plane's height: t = 1, 10, 100, 1000 when the climb is logarithmic, else every 100.</summary>
    void PwTicks(PwScene s, double t, float vis)
    {
        float logh = Tw("pw.logh", 0f), rise = Tw("pw.rise", 0f);
        string sig = vis <= .01f ? "off" : $"{logh:F2}|{rise:F2}|{(int)Math.Floor(Math.Log10(Math.Max(1.0, t)))}|{(int)(t / 100)}";
        if (sig == s.TickSig) return;
        s.TickSig = sig;
        foreach (Transform old in s.Ticks) Destroy(old.gameObject);
        if (vis <= .01f) return;
        List<double> marks = new();
        if (logh > .5f) { for (double m = 1; m <= Math.Max(1, t) * 1.0001; m *= 10) marks.Add(m); }
        else for (double m = 100; m <= t + 1e-6; m += 100) marks.Add(m);
        Color c = new(.72f, .76f, .9f, vis);
        foreach (double m in marks)
        {
            Vector3 at = PathOrigin + Vector3.back * PathHeight(m);
            ExpLine(s.Ticks, $"tick {m}", .2f, c, Factory.NoGlow, new[] { at + Vector3.left * 1.6f, at + Vector3.right * 1.6f });
            TextBox l = ExpLabelWide(s.Ticks, $"t = {m:0}", at + Vector3.left * 2f, 22f, c, TextAlignmentOptions.Right);
            Outline(l.TextField);
        }
    }

    /// <summary>The marks on link n: its length, the dashed run of link n − 1, the gold turn arc and the blue direction arc.</summary>
    void PwMarks(PwScene s, double t, int n, float marks, float frac)
    {
        bool on = marks > .01f && n >= 1;
        foreach (Component c in new Component[] { s.Extension, s.TurnArc, s.DirArc, s.DirRef, s.LenTag, s.TurnTag, s.DirTag, s.Desc })
            c.gameObject.SetActive(on);
        if (!on) return;
        Vector3 back = Vector3.back * .08f;
        Vector3 j = PwP(pwBuffer[n - 1], t), end = PwP(pwBuffer[n], t);
        double len = 1.0 / Math.Sqrt(n), dirN = -t * Math.Log(n), dirPrev = n >= 2 ? -t * Math.Log(n - 1) : 0.0;
        double turn = dirN - dirPrev;
        float r = (float)(len * PwScale);
        Vector3 Dir(double a) => new((float)Math.Cos(a), (float)Math.Sin(a), 0f);
        // the run of the last link, carried on past the joint (what the path would do without the turn)
        s.Extension.gameObject.SetActive(n >= 2);
        if (n >= 2) s.Extension.SetPositions(new[] { j + back, j + Dir(dirPrev) * (r * 1.15f) + back });
        // the turn, from that run round to link n
        Vector3[] arc = new Vector3[33];
        float ra = r * .55f;
        for (int i = 0; i <= 32; i++) arc[i] = j + Dir(dirPrev + turn * i / 32.0) * ra + back;
        s.TurnArc.gameObject.SetActive(n >= 2);
        s.TurnArc.positionCount = 33;
        s.TurnArc.SetPositions(arc);
        // the direction, from the direction of link 1 (to the right) round to link n
        Vector3[] darc = new Vector3[49];
        float rd = r * .9f;
        for (int i = 0; i <= 48; i++) darc[i] = j + Dir(dirN * i / 48.0) * rd + back * 1.5f;
        s.DirArc.positionCount = 49;
        s.DirArc.SetPositions(darc);
        s.DirRef.SetPositions(new[] { j + back, j + Vector3.right * (rd * 1.25f) + back });
        // the tags: length at the middle of the link, turn by its arc, direction by its arc
        Vector3 mid = (j + end) * .5f, side = Vector3.Cross(Dir(dirN), Vector3.back).normalized;
        s.LenTag.TextField.text = n == 1 ? "1" : $"1/√{n}";
        s.LenTag.transform.position = mid + side * 2.2f + Vector3.back * .4f;
        double turnDeg = Math.Abs(turn) * 180.0 / Math.PI, dirDeg = Math.Abs(dirN) * 180.0 / Math.PI;
        s.TurnTag.TextField.text = $"{turnDeg:0}°";
        s.TurnTag.transform.position = j + Dir(dirPrev + turn * .5) * (ra + 1.8f) + Vector3.back * .4f;
        s.DirTag.TextField.text = n == 1 ? "" : $"{dirDeg:0}°";
        s.DirTag.transform.position = j + Dir(dirN * .5) * (rd + 2.0f) + Vector3.back * .4f;
        string tl = Math.Abs(t - 1) < 1e-6 ? "" : $"{t:0.##}·";
        s.Desc.gameObject.SetActive(false);
        if (n != s.BoardLink && marks > .99f && !TwMoving("pw.marks"))
        {
            s.BoardLink = n;
            Board(n == 1
                ? "arrow 1\n<size=58%>1 unit long, pointing right</size>"
                : $"arrow {n}\n<size=58%>1/√{n} = {len:0.00} long  ·  <color=#FFD24D>turned {tl}log({n}/{n - 1}) = {turnDeg:0.0}° from arrow {n - 1}</color>  ·  " +
                  $"<color=#73BFFF>pointing {tl}log {n} = {dirDeg:0.0}° from arrow 1</color></size>");
        }

        foreach (TextBox tb in new[] { s.LenTag, s.TurnTag, s.DirTag }) tb.TextField.alpha = marks;
    }

    /// <summary>The curve ζ(½ + iτ) for τ from pw.t0 up to t, each point at its own height when the plane climbs.</summary>
    void PwCurve(PwScene s, double t, float vis)
    {
        bool on = vis > .01f;
        s.Curve.gameObject.SetActive(on);
        if (!on) return;
        double t0 = Tw("pw.t0", 0f);
        // rebuild from t0 if t went back or the start moved; else extend
        if (double.IsNaN(s.CurveT) || t < s.CurveT - 1e-9 || s.CurvePts.Count == 0 || Math.Abs(Tw("pw.t0", 0f) - s.CurveStart) > 1e-6)
        {
            s.CurvePts.Clear();
            s.CurveT = t0;
            s.CurveStart = t0;
        }

        for (; s.CurveT + .02 <= t; s.CurveT += .02)
        {
            double tt = s.CurveT + .02;
            s.CurvePts.Add((tt, ZetaMath.Exact(new Complex(.5, tt))));
        }

        // each point at the height of its own t (PathHeight scales with pw.rise: flat, they all lie in the plane)
        List<Vector3> pts = new(s.CurvePts.Count + 1);
        foreach (var (tt, z) in s.CurvePts) pts.Add(PwP(z, tt));
        pts.Add(PwP(ZetaMath.Exact(new Complex(.5, t)), t));
        s.Curve.positionCount = pts.Count;
        s.Curve.SetPositions(pts.ToArray());
        s.Curve.startColor = s.Curve.endColor = new Color(Factory.Palette.Zero.r, Factory.Palette.Zero.g, Factory.Palette.Zero.b, vis);
    }

    /// <summary>
    /// Rings at the vortex eyes in the current plane (pw.eyes), crosses at their mirror points (pw.mirrors: the path's own
    /// k-th vertex reflected through ζ's phase, ζ − χ·conj(S_k)); with pw.trails, at every zero passed, each eye is kept at
    /// that zero's height, so the eyes build a tower; pw.alleyes lays the eyes of all 10 000 zeros in the plane (seen down
    /// the axis: the unit circle of vortex 1 and the rings of the others).
    /// </summary>
    void PwEyes(PwScene s, double t, float eyes, float trails)
    {
        float mirrors = Tw("pw.mirrors", 0f), all = Tw("pw.alleyes", 0f);
        bool on = eyes > .01f;
        Complex zeta = ZetaMath.Decompose(new Complex(.5, t), ZetaMath.TermCount(t), pwBuffer2 ??= new Complex[4096]).Total;
        List<Vortex> vs = on ? DetectVortices(t, 0, 7, zeta) : new List<Vortex>();
        int used = 0;
        foreach (Vortex v in vs)
        {
            if (v.K < 1 || v.K > 7) continue;
            while (s.Rings.Count <= used)
            {
                LineRenderer ring = ExpLine(s.Root, "eye", .16f, Color.white, Factory.GlowIntensity, new Vector3[2]);
                s.Rings.Add(ring);
                TextBox tag = ExpLabelWide(s.Root, "", PathOrigin, 28f, Color.white);
                Outline(tag.TextField);
                s.RingTags.Add(tag);
                s.Crosses.Add(ExpLine(s.Root, "mirror", .14f, Color.white, Factory.GlowIntensity, new Vector3[2]));
            }

            LineRenderer r = s.Rings[used];
            TextBox tg = s.RingTags[used];
            LineRenderer x = s.Crosses[used];
            r.gameObject.SetActive(true);
            tg.gameObject.SetActive(true);
            Color c = VortexColor(v.K, v.Certainty);
            Vector3 e = PwP(v.Eye, t) + Vector3.back * .12f;
            float rad = (.08f + .07f * v.Certainty) * PwScale;
            Vector3[] loop = new Vector3[33];
            for (int i = 0; i <= 32; i++)
            {
                float a = i / 32f * Mathf.PI * 2f;
                loop[i] = e + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * rad;
            }

            r.positionCount = 33;
            r.SetPositions(loop);
            r.startColor = r.endColor = new Color(c.r, c.g, c.b, eyes);
            tg.TextField.text = v.K.ToString();
            tg.TextField.color = new Color(c.r, c.g, c.b, eyes);
            tg.transform.position = e + new Vector3(0f, rad + 1.3f, -.2f);
            bool cross = mirrors > .01f;
            x.gameObject.SetActive(cross);
            if (cross)
            {
                Vector3 m = PwP(v.Mirror, t) + Vector3.back * .16f;
                float k = rad * .55f;
                x.positionCount = 5;
                x.SetPositions(new[] { m + new Vector3(-k, -k, 0f), m + new Vector3(k, k, 0f), m, m + new Vector3(-k, k, 0f), m + new Vector3(k, -k, 0f) });
                x.startColor = x.endColor = new Color(1f, 1f, 1f, mirrors);
            }

            used++;
        }

        for (int i = used; i < s.Rings.Count; i++)
        {
            s.Rings[i].gameObject.SetActive(false);
            s.RingTags[i].gameObject.SetActive(false);
            s.Crosses[i].gameObject.SetActive(false);
        }

        // the trails: at every zero passed, the eyes there, kept at that zero's height
        if (trails > .01f && !double.IsNaN(s.TrailT) && t > s.TrailT)
        {
            bool added = false;
            foreach (double z in Main.Instance.zetaZeroValues)
            {
                if (z <= s.TrailT) continue;
                if (z > t) break;
                foreach (Vortex v in DetectVortices(z, 1, 7, Complex.Zero))
                {
                    if (v.K < 1 || v.K > 7) continue;
                    s.TrailPts[v.K].Add(PwP(v.Eye, z) + Vector3.back * .1f);
                    added = true;
                }
            }

            if (added)
                for (int k = 1; k <= 7; k++)
                {
                    if (s.TrailPts[k].Count == 0) continue;
                    if (s.TrailR[k] != null) Destroy(s.TrailR[k].gameObject);
                    if (s.TrailL[k] != null) Destroy(s.TrailL[k].gameObject);
                    Color c = VortexColor(k, 1f);
                    List<Color> cs = new();
                    List<float> sz = new();
                    foreach (Vector3 _ in s.TrailPts[k]) { cs.Add(c); sz.Add(k == 1 ? .42f : .26f); }
                    s.TrailR[k] = ExpPointCloud(s.Root, $"trail {k}", s.TrailPts[k], cs, sz, Factory.GlowIntensity);
                }
        }

        s.TrailT = t;
        for (int k = 1; k <= 7; k++)
            if (s.TrailR[k] != null) Factory.SetColor(s.TrailR[k], new Color(1f, 1f, 1f, Mathf.Clamp01(trails)));
        // all 10 000 zeros' eyes, laid in the plane where it is now
        if (all > .01f && s.AllEyes == null)
        {
            List<Vortex> every = ExpVortices(Main.Instance.zetaZeroValues);
            List<Vector3> pts = new();
            List<Color> cs = new();
            List<float> sz = new();
            float h = PathHeight(t);
            foreach (Vortex v in every)
            {
                if (v.K < 1 || v.K > 5) continue;
                pts.Add(PathOrigin + new Vector3((float)v.Eye.Re, (float)v.Eye.Im, 0f) * PwScale + Vector3.back * (h + .05f));
                cs.Add(VortexColor(v.K, v.Certainty));
                sz.Add(v.K == 1 ? .06f : .035f);
            }

            s.AllEyes = ExpPointCloud(s.Root, "all eyes", pts, cs, sz, Factory.SoftGlowIntensity);
        }

        if (s.AllEyes != null) Factory.SetColor(s.AllEyes, new Color(1f, 1f, 1f, all));
    }

    /// <summary>For callouts: vertex k of the path (the sum of its first k links) as drawn now.</summary>
    public Vector3 PathVertex(int k)
    {
        if (pw == null || pwBuffer == null || k < 0 || k >= pwBuffer.Length) return PathOrigin;
        return PwP(pwBuffer[k], Tw("pw.t", 1f)) + Vector3.back * .1f;
    }

    /// <summary>For callouts: ζ(½ + it) at the path's current t (the end of its tail).</summary>
    public Vector3 PathZeta() => pw != null && pw.Zeta != null ? pw.Zeta.transform.position : PathOrigin;
}
