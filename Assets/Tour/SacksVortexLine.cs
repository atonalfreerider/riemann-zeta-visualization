using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The vortices of the path of partial sums laid out on the number line (long-form segment xz2). At height t, arrow n of
/// the path belongs to vortex k when it lies between spokes k + 1 and k, from n = t/2π(k + 1) to t/2πk (k = 0 is the tail
/// beyond spoke 1; above 7, the tangle at the start of the path, where a vortex is shorter than one arrow).
///   vl.unwind  0 … 1: the path's arrows straighten onto a number line, one unit per arrow, each coloured by its vortex
///   vl.marks   0 … 1: the spokes, the vortex numbers and the counting numbers on that line
///   vl.fold    0 … 1: the mirror n ↦ t/2πn: a second axis beside the first, on which vortex k fills the stretch from k to
///                     k + 1, and every arrow is joined to its mirror point
///   vl.merge   0 … 1: the second axis slides onto the first: the colours then read the same both ways from the fold √(t/2π)
///   vl.curl    0 … 1: the line curls into the Sacks spiral (n at radius √n, angle 2π√n); the fold becomes a circle
///   vl.vis     0 … 1: the whole scene
/// Lines and labels keep their size on the screen (they scale with the camera's distance), since the camera closes in
/// tenfold from the line to the spiral (orbit cue on=vlbox follows the scene's bounds).
/// </summary>
public partial class SacksSpiral
{
    class VlLabel
    {
        public TextBox Box;
        public Func<Vector3> At;
        public Func<float> Alpha;
        public Color Col;
        public float Frac; // the text's height as a fraction of the frame's height
    }

    class VlScene
    {
        public Transform Root;
        public double T;                 // t / 2π
        public int N;                    // the arrows laid out: 1 … N
        public Vector3 L0, U, W, V;      // the line's start, its direction, the side the curl turns to, the mirror axis's side
        public float Dx, Gap, Sc, Mag;   // world units per unit of n; the mirror axis's distance; the spiral's radius per √n;
                                         // the mirror axis's magnification (it shows 0 … √(t/2π) + 2 over the line's length)
        public Vector3[] PathPts;        // the path's vertices S_0 … S_N where the path drew them
        public int[] First;              // the sample index of vertex n
        public double[] Ws;              // √n of each sample
        public Vector3[] Curve;          // the line, curled by vl.curl, sample by sample
        public Vector3[] Pos;            // what is drawn: the path unwinding onto the curve
        public LineRenderer[] Arrows;
        public LineRenderer[] MirrorK;   // the mirror axis: the stretch from k to k + 1, k = 0 … 7
        public LineRenderer MirrorRest;  // and on from 8, grey (the mirror images of the tangle)
        public LineRenderer[] Fan;       // vertex n to its mirror point t/2πn
        public LineRenderer[] Spokes;
        public LineRenderer FoldBar, FoldCircle, FoldDisc;
        public GameObject[] Dots;
        public readonly List<VlLabel> Labels = new();
        public Bounds Box;
        public string Sig = "";
    }

    VlScene vl;
    const int VlMaxK = 7;

    /// <summary>The vortex of the point x of the line (x a position along the path's arrows): 0 past spoke 1, else ⌊T/x⌋.</summary>
    static int VlVortex(double T, double x) => x >= T ? 0 : (int)Math.Floor(T / Math.Max(1e-6, x));

    static Color VlColor(int k) => k > VlMaxK ? new Color(.42f, .45f, .58f) : VortexColor(k, 1f);

    /// <summary>The Sacks spiral's arc length from its centre to n (radius √n, angle 2π√n), in units of n (≈ n).</summary>
    static float VlArc(double n)
    {
        double w = Math.Sqrt(Math.Max(0.0, n)), a = 2 * Math.PI * w;
        return (float)((w / 2 * Math.Sqrt(a * a + 1) + Math.Log(a + Math.Sqrt(a * a + 1)) / (4 * Math.PI)) / Math.PI);
    }

    /// <summary>For the camera (on=vlbox): what the scene covers now, with room for its labels.</summary>
    public Bounds VlBounds() => vl == null ? new Bounds(PathFocus, Vector3.one * 40f) : vl.Box;

    /// <summary>Set up the scene from the path as it is now (exp:vlinit); its parameters start at 0.</summary>
    public void ExpVortexLine()
    {
        if (pw == null || pwBuffer == null) return;
        Transform old = ExpRoot().Find("VL vortex line");
        if (old != null) Destroy(old.gameObject);
        double t = Tw("pw.t", 1f);
        VlScene s = new() { Root = ExpGroup("VL vortex line").transform, T = t / (2 * Math.PI) };
        s.N = Mathf.Clamp((int)Math.Floor(s.T) + 3, 10, pwBuffer.Length - 2);
        s.Dx = 1f;
        s.Sc = s.Dx / Mathf.PI;
        s.Gap = Tall ? 22f : 12f;
        s.Mag = (float)(s.N / (Math.Sqrt(s.T) + 2.0));
        // along the frame's long side: right in 16:9, up in 9:16; the curl turns counter-clockwise, the mirror axis on the other side
        s.U = Tall ? Vector3.up : Vector3.right;
        s.W = new Vector3(-s.U.y, s.U.x, 0f);
        s.V = -s.W;
        foreach (string k in new[] { "vl.unwind", "vl.marks", "vl.fold", "vl.merge", "vl.curl" }) TwSet(k, 0f);
        TwSet("vl.vis", 1f);

        // the path's vertices, and the line through the middle of the path
        s.PathPts = new Vector3[s.N + 1];
        Bounds pb = new(PathVertex(0), Vector3.zero);
        for (int n = 0; n <= s.N; n++)
        {
            s.PathPts[n] = PathVertex(n);
            pb.Encapsulate(s.PathPts[n]);
        }

        float len = VlArc(s.N) * s.Dx;
        s.L0 = pb.center - s.U * (len * .5f) + Vector3.back * .1f;

        // samples along each arrow, more where the spiral turns fast (the first arrow is a whole turn of it)
        s.First = new int[s.N + 1];
        List<double> ws = new() { 0.0 };
        for (int n = 1; n <= s.N; n++)
        {
            double w0 = Math.Sqrt(n - 1), w1 = Math.Sqrt(n);
            int steps = Mathf.Max(4, Mathf.CeilToInt((float)(40.0 * (w1 - w0))));
            for (int j = 1; j <= steps; j++) ws.Add(w0 + (w1 - w0) * j / steps);
            s.First[n] = ws.Count - 1;
        }

        s.Ws = ws.ToArray();
        s.Curve = new Vector3[s.Ws.Length];
        s.Pos = new Vector3[s.Ws.Length];

        s.Arrows = new LineRenderer[s.N + 1];
        for (int n = 1; n <= s.N; n++)
        {
            s.Arrows[n] = ExpLine(s.Root, $"arrow {n}", .2f, Color.white, Factory.GlowIntensity, new[] { s.L0, s.L0 });
            s.Arrows[n].numCornerVertices = 2;
            OnTop(s.Arrows[n], 12);
        }

        s.MirrorK = new LineRenderer[VlMaxK + 1];
        for (int k = 0; k <= VlMaxK; k++)
        {
            s.MirrorK[k] = ExpLine(s.Root, $"mirror {k}", .2f, Color.white, Factory.GlowIntensity, new[] { s.L0, s.L0 });
            OnTop(s.MirrorK[k], 13);
        }

        s.MirrorRest = ExpLine(s.Root, "mirror rest", .2f, Color.white, Factory.SoftGlowIntensity, new[] { s.L0, s.L0 });
        s.Fan = new LineRenderer[s.N + 1];
        for (int n = 1; n <= s.N; n++)
            s.Fan[n] = ExpLine(s.Root, $"fan {n}", .05f, Color.white, Factory.SoftGlowIntensity, new[] { s.L0, s.L0 });
        s.Spokes = new LineRenderer[VlMaxK + 1];
        for (int k = 1; k <= VlMaxK; k++)
        {
            s.Spokes[k] = ExpLine(s.Root, $"spoke {k}", .08f, Color.white, Factory.GlowIntensity, new[] { s.L0, s.L0 });
            OnTop(s.Spokes[k], 14);
        }

        s.FoldBar = ExpLine(s.Root, "fold", .08f, Color.white, AttentionIntensity, new[] { s.L0, s.L0 });
        OnTop(s.FoldBar, 15);
        s.FoldCircle = ExpLine(s.Root, "fold circle", .05f, Color.white, AttentionIntensity, new Vector3[65]);
        OnTop(s.FoldCircle, 15);
        // a ring as wide as its radius, centred on half the radius: a filled disc
        s.FoldDisc = ExpLine(s.Root, "fold disc", .05f, Color.white, Factory.NoGlow, new Vector3[65]);
        s.FoldDisc.loop = true;
        OnTop(s.FoldDisc, 11);
        s.Dots = new GameObject[s.N + 1];
        for (int n = 0; n <= s.N; n++) s.Dots[n] = ExpDot(s.Root, $"n {n}", s.L0, .3f, new Color(.62f, .65f, .78f), Factory.NoGlow);

        // ---- labels ----
        VlScene sc = s;
        float Marks() => Tw("vl.marks", 0f);
        float Fold() => Tw("vl.fold", 0f);
        float Merge() => Tw("vl.merge", 0f);
        float Curl() => Tw("vl.curl", 0f);
        Vector3 OnLine(double x) => sc.L0 + sc.U * (VlArc(x) * sc.Dx);
        Vector3 OnMirror(double m) => sc.L0 + sc.U * (VlArc(m) * sc.Dx * Mathf.Lerp(sc.Mag, 1f, Merge())) + sc.V * (sc.Gap * (1f - Merge()));
        void Label(string text, Color col, float frac, Func<Vector3> at, Func<float> alpha, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            TextBox box = ExpLabelWide(sc.Root, text, sc.L0, 10f, col, align);
            Outline(box.TextField);
            sc.Labels.Add(new VlLabel { Box = box, At = at, Alpha = alpha, Col = col, Frac = frac });
        }

        float lineView() => Marks() * (1f - Mathf.Clamp01(Curl() * 8f));
        float wideBoost = Tall ? 1f : 1.35f;
        // the vortices, over the middle of their stretches (at their eyes, x = T/(k + ½)), on the side the curl turns to
        for (int k = 1; k <= 5; k++)
        {
            double eye = s.T / (k + .5);
            // 4 and 5 are close together: smaller, and staggered further out
            float size = (k <= 3 ? .034f : k == 4 ? .026f : .022f) * wideBoost, outBy = k <= 3 ? .045f : k == 4 ? .07f : .1f;
            Label(k.ToString(), VlColor(k), size, () => OnLine(eye) + sc.W * (VlFrameH() * outBy), lineView);
        }

        // the spokes' places, on the other side (until the mirror axis comes in there)
        for (int k = 1; k <= 4; k++)
        {
            double sp = s.T / k;
            float size = (k <= 3 ? .026f : .02f) * wideBoost, outBy = k <= 3 ? .04f : .065f;
            Label($"{sp:0}", new Color(.85f, .88f, 1f), size, () => OnLine(sp) + sc.V * (VlFrameH() * outBy),
                () => lineView() * (1f - Fold()));
        }

        Label("n", new Color(.85f, .88f, 1f), .03f * wideBoost, () => OnLine(sc.N + .5) + sc.U * (VlFrameH() * .03f), lineView);
        // the mirror axis: the counting numbers, the vortex on each stretch, its name; the fold
        for (int m = 1; m <= 8; m++)
        {
            int mm = m;
            Label(m.ToString(), VlColor(m <= VlMaxK ? m : VlMaxK + 1) * .5f + new Color(.5f, .5f, .5f), .03f * wideBoost,
                () => OnMirror(mm) + sc.V * (VlFrameH() * .04f), () => Fold() * (1f - Merge()));
        }

        Label("t / 2πn", new Color(.85f, .88f, 1f), .028f * wideBoost, () => OnMirror(sc.N / sc.Mag + .3) + sc.U * (VlFrameH() * .03f),
            () => Fold() * (1f - Merge()));
        // (on two lines in a 9:16 frame, where one line would reach across to the mirror axis's numbers)
        Label(Tall ? $"√(t/2π)\n= {Math.Sqrt(s.T):0.0}" : $"√(t/2π) = {Math.Sqrt(s.T):0.0}", PrimeGold, .03f * wideBoost,
            () => OnLine(Math.Sqrt(sc.T)) + sc.V * (VlFrameH() * .035f) + sc.U * (VlFrameH() * .01f),
            () => Fold() * (1f - Merge()), TextAlignmentOptions.Left);
        // on the spiral: the counting numbers inside the fold, the vortices on their bands outside it, the circle
        float spiralView() => Marks() * Mathf.Clamp01((Curl() - .85f) / .15f);
        for (int n = 1; n <= 7; n++)
        {
            int nn = n;
            Label(n.ToString(), new Color(.92f, .94f, 1f), .02f * wideBoost, () => VlOutward(sc, nn, .045f), spiralView);
        }

        for (int k = 1; k <= 3; k++)
        {
            double eye = s.T / (k + .5);
            Label(k.ToString(), VlColor(k), .036f * wideBoost, () => VlOutward(sc, eye, .07f), spiralView);
        }

        foreach (VlLabel l in s.Labels) l.Box.TextField.alpha = 0f;

        vl = s;
        StartCoroutine(VortexLineLoop(s));
    }

    /// <summary>The frame's height at the scene (world units), from the camera's distance to it.</summary>
    float VlFrameH()
    {
        Camera cam = Camera.main;
        if (cam == null || vl == null) return 40f;
        float d = Vector3.Distance(cam.transform.position, vl.Box.size.sqrMagnitude > 0f ? vl.Box.center : vl.L0);
        return 2f * d * Mathf.Tan(cam.fieldOfView * .5f * Mathf.Deg2Rad);
    }

    /// <summary>A point beside the curled line at x (radially out from the spiral's centre once it is curled).</summary>
    Vector3 VlOutward(VlScene s, double x, float frac)
    {
        Vector3 p = VlAt(s, x);
        Vector3 d = p - s.L0;
        d = d.sqrMagnitude > 1e-6f ? d.normalized : s.W;
        return p + d * (VlFrameH() * frac);
    }

    /// <summary>The drawn point at x along the line (x may fall between two vertices).</summary>
    Vector3 VlAt(VlScene s, double x)
    {
        x = Math.Max(0.0, Math.Min(s.N, x));
        double w = Math.Sqrt(x);
        int i = Array.BinarySearch(s.Ws, w);
        if (i >= 0) return s.Pos[i];
        i = ~i;
        if (i <= 0) return s.Pos[0];
        if (i >= s.Ws.Length) return s.Pos[s.Ws.Length - 1];
        float f = (float)((w - s.Ws[i - 1]) / (s.Ws[i] - s.Ws[i - 1]));
        return Vector3.Lerp(s.Pos[i - 1], s.Pos[i], f);
    }

    IEnumerator VortexLineLoop(VlScene s)
    {
        while (s == vl && s.Root != null)
        {
            VlUpdate(s);
            yield return null;
        }
    }

    void VlUpdate(VlScene s)
    {
        float unwind = Tw("vl.unwind", 0f), marks = Tw("vl.marks", 0f), fold = Tw("vl.fold", 0f), merge = Tw("vl.merge", 0f);
        float curl = Tw("vl.curl", 0f), vis = Tw("vl.vis", 1f);
        float fh = VlFrameH();
        s.Root.gameObject.SetActive(vis > .002f);
        if (vis <= .002f) return;

        // the line, curled: the tangent turns by curl × the Sacks spiral's own tangent angle φ(w) = 2πw + atan 2πw along its
        // arc length, so at curl 1 vertex n lands at radius √n, angle 2π√n (scaled by Sc), and at curl 0 the line is straight
        Vector2 p = Vector2.zero;
        s.Curve[0] = s.L0;
        for (int i = 1; i < s.Ws.Length; i++)
        {
            double w0 = s.Ws[i - 1], w1 = s.Ws[i], a = Math.PI * (w0 + w1);
            double phi = curl * (a + Math.Atan(a)), ds = s.Sc * Math.Sqrt(1 + a * a) * (w1 - w0);
            p += new Vector2((float)(Math.Cos(phi) * ds), (float)(Math.Sin(phi) * ds));
            s.Curve[i] = s.L0 + s.U * p.x + s.W * p.y;
        }

        // the path unwinding onto it, from its start outward
        float[] uv = new float[s.N + 1];
        for (int n = 0; n <= s.N; n++) uv[n] = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(unwind * 1.6f - .6f * n / s.N));
        for (int n = 1; n <= s.N; n++)
        {
            int i0 = s.First[n - 1], i1 = s.First[n];
            for (int i = i0; i <= i1; i++)
            {
                float f = (float)(i - i0) / (i1 - i0);
                Vector3 onPath = Vector3.Lerp(s.PathPts[n - 1], s.PathPts[n], f);
                s.Pos[i] = Vector3.Lerp(onPath, s.Curve[i], Mathf.Lerp(uv[n - 1], uv[n], f));
            }
        }

        s.Pos[0] = Vector3.Lerp(s.PathPts[0], s.Curve[0], uv[0]);
        float width = fh * .0065f, alpha = vis;
        // arrows: their vortex's colour; once the mirror axis lies on the line, the first eight take its colours
        bool merged = merge >= .999f;
        for (int n = 1; n <= s.N; n++)
        {
            LineRenderer l = s.Arrows[n];
            int i0 = s.First[n - 1], i1 = s.First[n];
            l.positionCount = i1 - i0 + 1;
            for (int i = i0; i <= i1; i++) l.SetPosition(i - i0, s.Pos[i]);
            int k = merged && n <= 8 ? n - 1 : VlVortex(s.T, n - .5);
            Color c = VlColor(k);
            l.startColor = l.endColor = new Color(c.r, c.g, c.b, alpha);
            l.widthMultiplier = width;
        }

        for (int n = 0; n <= s.N; n++)
        {
            s.Dots[n].transform.position = s.Pos[s.First[n]] + Vector3.back * .02f;
            s.Dots[n].transform.localScale = Vector3.one * (fh * .0042f * vis);
        }

        // the spokes: short bars across the line at t/2πk
        for (int k = 1; k <= VlMaxK; k++)
        {
            float a = marks * (1f - Mathf.Clamp01(curl * 8f)) * vis;
            LineRenderer b = s.Spokes[k];
            b.gameObject.SetActive(a > .01f);
            if (a <= .01f) continue;
            Vector3 c = VlAt(s, s.T / k) + Vector3.back * .05f;
            b.SetPositions(new[] { c - s.W * (fh * .018f), c + s.W * (fh * .018f) });
            b.widthMultiplier = fh * .0026f;
            b.startColor = b.endColor = new Color(1f, 1f, 1f, a);
        }

        // the mirror axis, beside the line, sliding onto it with vl.merge
        Vector3 off = s.V * (s.Gap * (1f - merge)) + Vector3.back * .08f;
        float mag = Mathf.Lerp(s.Mag, 1f, merge), mEnd = s.N / s.Mag;
        Vector3 M(double m) => s.L0 + s.U * (VlArc(m) * s.Dx * mag) + off;
        bool mirrorOn = fold > .01f && !merged;
        for (int k = 0; k <= VlMaxK; k++)
        {
            LineRenderer l = s.MirrorK[k];
            l.gameObject.SetActive(mirrorOn);
            if (!mirrorOn) continue;
            // drawn on from the start with vl.fold
            double m1 = Math.Min(k + 1, fold * 1.3 * mEnd);
            if (m1 <= k) { l.gameObject.SetActive(false); continue; }
            l.SetPositions(new[] { M(k), M(m1) });
            Color c = VlColor(k);
            l.startColor = l.endColor = new Color(c.r, c.g, c.b, alpha);
            l.widthMultiplier = width;
        }

        s.MirrorRest.gameObject.SetActive(mirrorOn && fold * 1.3 * mEnd > VlMaxK + 1);
        if (s.MirrorRest.gameObject.activeSelf)
        {
            s.MirrorRest.SetPositions(new[] { M(VlMaxK + 1), M(Math.Min(mEnd, fold * 1.3 * mEnd)) });
            Color g = VlColor(VlMaxK + 1);
            s.MirrorRest.startColor = s.MirrorRest.endColor = new Color(g.r, g.g, g.b, alpha * (1f - merge));
            s.MirrorRest.widthMultiplier = width * .7f;
        }

        // every vertex n joined to its mirror point t/2πn; the fold, where the two meet
        float fanA = Mathf.Clamp01(fold * 1.5f - .3f) * (1f - merge) * vis;
        for (int n = 1; n <= s.N; n++)
        {
            LineRenderer l = s.Fan[n];
            double m = s.T / n;
            bool on = fanA > .01f && m <= mEnd;
            l.gameObject.SetActive(on);
            if (!on) continue;
            Vector3 a = s.Pos[s.First[n]] + Vector3.back * .03f, b = M(m);
            l.SetPositions(new[] { a, Vector3.Lerp(a, b, Mathf.Clamp01(fold * 1.3f)) });
            int k = VlVortex(s.T, n);
            Color c = VlColor(k);
            l.startColor = l.endColor = new Color(c.r, c.g, c.b, fanA * (k > VlMaxK ? .35f : .55f));
            l.widthMultiplier = width * .3f;
        }

        double fx = Math.Sqrt(s.T);
        float foldA = fold * (1f - merge) * vis;
        s.FoldBar.gameObject.SetActive(foldA > .01f);
        if (foldA > .01f)
        {
            Vector3 a = VlAt(s, fx) + Vector3.back * .1f;
            s.FoldBar.SetPositions(new[] { a, Vector3.Lerp(a, M(fx) + Vector3.back * .02f, Mathf.Clamp01(fold * 1.3f)) });
            s.FoldBar.widthMultiplier = fh * .0032f;
            s.FoldBar.startColor = s.FoldBar.endColor = new Color(PrimeGold.r, PrimeGold.g, PrimeGold.b, foldA);
        }

        // on the spiral the fold is a circle: radius Sc × (t/2π)^¼, where n = √(t/2π)
        float circleA = Mathf.Clamp01((curl - .8f) / .2f) * vis;
        s.FoldCircle.gameObject.SetActive(circleA > .01f);
        s.FoldDisc.gameObject.SetActive(circleA > .01f);
        if (circleA > .01f)
        {
            float r = s.Sc * (float)Math.Pow(s.T, .25);
            for (int i = 0; i <= 64; i++)
            {
                float an = i / 64f * Mathf.PI * 2f;
                Vector3 dir = s.U * Mathf.Cos(an) + s.W * Mathf.Sin(an);
                s.FoldCircle.SetPosition(i, s.L0 + dir * r + Vector3.back * .04f);
                s.FoldDisc.SetPosition(i, s.L0 + dir * (r * .5f) + Vector3.back * .01f);
            }

            s.FoldCircle.widthMultiplier = fh * .0075f;
            s.FoldCircle.startColor = s.FoldCircle.endColor = new Color(PrimeGold.r, PrimeGold.g, PrimeGold.b, circleA);
            s.FoldDisc.widthMultiplier = r;
            s.FoldDisc.startColor = s.FoldDisc.endColor = new Color(PrimeGold.r, PrimeGold.g, PrimeGold.b, circleA * .16f);
        }

        // the bounds the camera follows: the drawn line (and the mirror axis while it is apart), with room for the labels
        Bounds box = new(s.Pos[0], Vector3.zero);
        foreach (Vector3 q in s.Pos) box.Encapsulate(q);
        if (mirrorOn) { box.Encapsulate(M(0)); box.Encapsulate(M(mEnd)); }
        box.Expand(curl > .5f ? box.size.magnitude * .08f : 2.5f);
        s.Box = box;

        foreach (VlLabel l in s.Labels)
        {
            float a = l.Alpha() * vis;
            l.Box.gameObject.SetActive(a > .01f);
            if (a <= .01f) continue;
            l.Box.transform.position = l.At() + Vector3.back * .3f;
            l.Box.TextField.fontSize = l.Frac * fh * 10f * LabelBoost;
            l.Box.TextField.color = new Color(l.Col.r, l.Col.g, l.Col.b, a);
        }
    }
}
