using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The number line that coils into the Sacks spiral and bends into the saddle, carrying its wave (the long-form segment
/// on discovery 1). One line of the numbers 1 … 150 and one wave over it, drawn through a single map with parameters the
/// script eases (exp:tw), so every picture turns into the next:
///   nw.roll    0: a straight line from the centre to the right; 1: the Sacks spiral (radius R√x, angle 2π√x). Between,
///              the spiral's turning times roll: the line curls up continuously, every number keeping its distance along
///              it (πR per unit of x, as on the spiral). Eased geometrically, each doubling of roll adds a visible curl.
///   nw.stand   0: the wave is drawn in the plane, as a graph over the line (up = the line's left); 1: it stands out of
///              the plane, as the home view's curtain. Between, it turns up about the line.
///   nw.bend    0: flat; 1: the tour's saddle z = K(x² − y²); the wave stands on the surface's normal.
///   nw.mix     0: the zeros' wave −Σ cos(γ log x) (nw.terms.p zeros from the nw.first.p-th; above 1 000 it blends
///              into the baked 10 000-zero wave); 1: Voronoi's divisor wave (nw.terms.d of his waves, times √x so each
///              spike is as tall as its count of divisors). Between, the two (each at its own scale) blend.
///   nw.h (height), nw.wave (0 … 1), nw.count (numbers shown), nw.labels, nw.label (their size), nw.dot, nw.rays
///   (the squares and pronic rays), nw.ribbon (the wave's sign painted on the line), nw.counter (the wave count, in a
///   fixed corner of the screen), nw.vis.
///   nw.frame   the camera (on=nwboxp) frames only the numbers up to this one: while the line curls, it follows the inner
///              turns as they form instead of the whole line (whose mid-curl arc is hundreds of units tall).
///   nw.split   0: one wave, the blend of the two by nw.mix; 1: both at once, the zeros' wave standing above the line (the
///              saddle) and Voronoi's divisor wave hanging below it, reflected.
/// The line, the crest and the numbers never get thinner on the screen than a few pixels, wherever the camera is.
/// </summary>
public partial class SacksSpiral
{
    public const float NumberWorldOffset = 100000f;
    const int NwN = 150, NwSamples = 36000, NwLabelMax = 60;
    const float NwXMin = 1.5f, NwColumnArc = .12f;

    static double[] nwW, nwLogX, nwPhiMid, nwDs;
    static int[] nwCols;
    static double nwDw;

    class NwLane
    {
        public Vector3 Centre;
        public Transform Root;
        public double[] AccP, AccD, BakedP;
        public int TermsP, FirstP = 1, TermsD;
        public float BetaP;
        public readonly float[] ShownP = new float[NwSamples + 1], ShownD = new float[NwSamples + 1], Shown = new float[NwSamples + 1];
        public readonly Vector3[] P = new Vector3[NwSamples + 1], Up = new Vector3[NwSamples + 1], Left = new Vector3[NwSamples + 1];
        public LineRenderer Curve, Crest, RaySquares, RayPronic;
        public Mesh Curtain, Ribbon, Dots, Under;
        public MeshRenderer CurtainR, RibbonR, DotsR, UnderR;
        public TextBox[] Labels;
        public string LayoutSig = "", DrawSig = "", MixSig = "";
        public bool WavesChanged = true;
        public float LastCount;
        public Bounds Box;
    }

    NwLane nw;

    public Vector3 NumberWorldOrigin => Center + new Vector3(NumberWorldOffset, 0f, 0f);

    static void NwGrid()
    {
        if (nwW != null) return;
        int m = NwSamples;
        nwDw = Math.Sqrt(NwN + 1.0) / m;
        nwW = new double[m + 1];
        nwLogX = new double[m + 1];
        nwPhiMid = new double[m];
        nwDs = new double[m];
        for (int i = 0; i <= m; i++)
        {
            nwW[i] = i * nwDw;
            nwLogX[i] = i > 0 ? 2.0 * Math.Log(nwW[i]) : -50.0;
        }

        for (int i = 0; i < m; i++)
        {
            // the Sacks spiral r = R w, angle 2πw (w = √x): its tangent turns by 2πw + atan(2πw), and ds = R √(4π²w² + 1) dw
            double a = 2.0 * Math.PI * (i + .5) * nwDw;
            nwPhiMid[i] = a + Math.Atan(a);
            nwDs[i] = R * Math.Sqrt(a * a + 1.0) * nwDw;
        }

        List<int> cols = new() { 0 };
        double acc = 0;
        for (int i = 0; i < m; i++)
        {
            acc += nwDs[i];
            if (acc < NwColumnArc) continue;
            cols.Add(i + 1);
            acc = 0;
        }

        if (cols[^1] != m) cols.Add(m);
        nwCols = cols.ToArray();
    }

    static Mesh NwMesh(Transform parent, string name, out MeshRenderer mr)
    {
        Mesh mesh = new() { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.MarkDynamic();
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = Factory.mainMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return mesh;
    }

    /// <summary>Set up the line (hidden until nw.vis is raised) and start the scene's frame loop.</summary>
    public void ExpNumberWorld()
    {
        NwGrid();
        EnsureDivisorCounts(4001);
        ExpEnsureSieve(NwN + 2);
        ClearTweens("nw.");
        NwLane L = new() { Centre = NumberWorldOrigin, Root = ExpGroup("NW line").transform };
        L.AccP = new double[NwSamples + 1];
        L.AccD = new double[NwSamples + 1];
        L.Curve = ExpLine(L.Root, "number line", .12f, new Color(.74f, .8f, 1f), Factory.SoftGlowIntensity, new[] { L.Centre, L.Centre });
        L.Crest = ExpLine(L.Root, "wave", .22f, Factory.Palette.Cosine, Factory.GlowIntensity, new[] { L.Centre, L.Centre });
        L.RaySquares = ExpLine(L.Root, "squares ray", .14f, new Color(.45f, .75f, 1f), Factory.SoftGlowIntensity, new[] { L.Centre, L.Centre });
        L.RayPronic = ExpLine(L.Root, "pronic ray", .14f, new Color(.45f, .75f, 1f), Factory.SoftGlowIntensity, new[] { L.Centre, L.Centre });
        L.Curtain = NwMesh(L.Root, "curtain", out L.CurtainR);
        Factory.SetIntensity(L.CurtainR, .82f);
        L.Under = NwMesh(L.Root, "divisor wave below", out L.UnderR);
        Factory.SetIntensity(L.UnderR, .82f);
        L.Ribbon = NwMesh(L.Root, "ribbon", out L.RibbonR);
        Factory.SetIntensity(L.RibbonR, 1.1f);
        L.Dots = NwMesh(L.Root, "numbers", out L.DotsR);
        Factory.SetIntensity(L.DotsR, Factory.GlowIntensity);
        L.Labels = new TextBox[NwLabelMax + 1];
        for (int n = 1; n <= NwLabelMax; n++)
        {
            bool prime = !expNotPrime[n] && n > 1;
            L.Labels[n] = ExpLabelWide(L.Root, n.ToString(), L.Centre, 3f, prime ? new Color(1f, .86f, .45f) : new Color(.8f, .83f, .93f));
            Outline(L.Labels[n].TextField, .2f);
            L.Labels[n].gameObject.SetActive(false);
        }

        L.Root.gameObject.SetActive(false);
        nw = L;
        StartCoroutine(NumberWorldLoop(L));
    }

    IEnumerator NumberWorldLoop(NwLane L)
    {
        while (L == nw && L.Root != null)
        {
            NwUpdate(L);
            yield return null;
        }
    }

    void NwUpdate(NwLane L)
    {
        float vis = Tw("nw.vis", 1f);
        bool on = vis > .002f;
        if (L.Root.gameObject.activeSelf != on) L.Root.gameObject.SetActive(on);
        NwCounter(L, Tw("nw.counter", 0f) * vis);
        if (!on) return;
        float roll = Tw("nw.roll", 0f), bend = Tw("nw.bend", 0f), stand = Tw("nw.stand", 0f), mix = Tw("nw.mix", 0f);
        float h = Tw("nw.h", 22f) * Tw("nw.wave", 0f);
        float count = Tw("nw.count", 0f), labels = Tw("nw.labels", 0f), dot = Tw("nw.dot", 2f), lab = Tw("nw.label", 14f);
        float rays = Tw("nw.rays", 0f), ribbon = Tw("nw.ribbon", 0f);
        NwTerms(L, Mathf.Max(1, Mathf.RoundToInt(Tw("nw.first.p", 1f))), Tw("nw.terms.p", 0f), Tw("nw.terms.d", 0f));
        string mixSig = $"{mix:F4}";
        if (L.WavesChanged || mixSig != L.MixSig)
        {
            L.MixSig = mixSig;
            for (int i = 0; i <= NwSamples; i++) L.Shown[i] = Mathf.Lerp(L.ShownP[i], L.ShownD[i], mix);
            L.WavesChanged = true;
        }

        string layout = $"{roll:F5}|{bend:F5}";
        bool moved = layout != L.LayoutSig;
        if (moved)
        {
            L.LayoutSig = layout;
            NwLayout(L, roll, bend);
        }

        float frameH = NwFrameHeight(L);
        int fq = Mathf.RoundToInt(Mathf.Log(Mathf.Max(1f, frameH)) * 20f);
        string draw = $"{h:F3}|{stand:F4}|{count:F2}|{labels:F3}|{dot:F3}|{lab:F3}|{rays:F3}|{vis:F3}|{ribbon:F3}|{fq}|{Tw("nw.frame", 1e6f):F2}|{Tw("nw.split", 0f):F3}";
        if (!moved && !L.WavesChanged && draw == L.DrawSig) return;
        L.DrawSig = draw;
        L.WavesChanged = false;
        NwDraw(L, roll, stand, h, count, labels, dot, lab, rays, vis, ribbon);
    }

    /// <summary>The height of the camera's frame at the line (world units), for widths that stay visible on the screen.</summary>
    static float NwFrameHeight(NwLane L)
    {
        Camera cam = Camera.main;
        if (cam == null) return 60f;
        float d = Vector3.Distance(cam.transform.position, L.Box.size.sqrMagnitude > 0f ? L.Box.center : L.Centre);
        return 2f * d * Mathf.Tan(cam.fieldOfView * .5f * Mathf.Deg2Rad);
    }

    /// <summary>Bring the two sums to the wanted numbers of terms (adding or removing waves) and refresh their display values.</summary>
    void NwTerms(NwLane L, int firstP, float termsPF, float termsDF)
    {
        int kp = Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(termsPF, 1000f)), 0, 1000);
        float beta = termsPF > 1000f ? Mathf.Clamp01(Mathf.Log(termsPF / 1000f) / Mathf.Log(10f)) : 0f;
        int kd = Mathf.Clamp(Mathf.RoundToInt(termsDF), 0, 4000);
        bool changedP = false, changedD = false;
        if (firstP != L.FirstP)
        {
            Array.Clear(L.AccP, 0, L.AccP.Length);
            L.TermsP = 0;
            L.FirstP = firstP;
            changedP = true;
        }

        if (kp != L.TermsP)
        {
            NwAddPrime(L, L.TermsP, kp);
            L.TermsP = kp;
            changedP = true;
        }

        if (Mathf.Abs(beta - L.BetaP) > 1e-4f)
        {
            L.BetaP = beta;
            if (beta > 0f && L.BakedP == null) L.BakedP = NwBakedOnGrid();
            changedP = true;
        }

        if (kd != L.TermsD)
        {
            NwAddDivisor(L, L.TermsD, kd);
            L.TermsD = kd;
            changedD = true;
        }

        if (changedP) NwShown(L, true);
        if (changedD) NwShown(L, false);
        if (changedP || changedD) L.WavesChanged = true;
    }

    void NwAddPrime(NwLane L, int from, int to)
    {
        int a = Mathf.Min(from, to), b = Mathf.Max(from, to);
        double sign = to > from ? 1.0 : -1.0;
        int i0 = (int)Math.Ceiling(Math.Sqrt(NwXMin) / nwDw);
        double[] zs = Main.Instance.zetaZeroValues;
        for (int j = a; j < b; j++)
        {
            int z = L.FirstP - 1 + j;
            if (z >= zs.Length) break;
            double g = zs[z];
            for (int i = i0; i <= NwSamples; i++) L.AccP[i] -= sign * Math.Cos(g * nwLogX[i]);
        }
    }

    /// <summary>Voronoi's m-th wave d(m) m^(−¼) sin(4π√m w − π/4) with the smooth weight of a 4 000-term sum (a fixed turn per sample).</summary>
    void NwAddDivisor(NwLane L, int from, int to)
    {
        int a = Mathf.Min(from, to), b = Mathf.Max(from, to);
        double sign = to > from ? 1.0 : -1.0;
        int i0 = (int)Math.Ceiling(Math.Sqrt(NwXMin) / nwDw);
        for (int j = a; j < b; j++)
        {
            int mm = 1 + j;
            double wgt = Math.Exp(-4.0 * (mm / 4000.0) * (mm / 4000.0)) * waveDivisors[Mathf.Min(mm, waveDivisors.Length - 1)] * Math.Pow(mm, -.25);
            double step = 4.0 * Math.PI * Math.Sqrt(mm) * nwDw, c0 = -Math.PI / 4.0 + step * i0;
            double cr = Math.Cos(c0), ci = Math.Sin(c0), rr = Math.Cos(step), ri = Math.Sin(step);
            for (int i = i0; i <= NwSamples; i++)
            {
                L.AccD[i] += sign * wgt * ci;
                double nr = cr * rr - ci * ri;
                ci = cr * ri + ci * rr;
                cr = nr;
            }
        }
    }

    /// <summary>The home view's baked 10 000-zero wave on this scene's grid: per cell, the sample of largest size.</summary>
    double[] NwBakedOnGrid()
    {
        double[] g = new double[NwSamples + 1];
        float[] b = BakedPrimeWave();
        if (b == null) return g;
        double l0 = Math.Log(FieldNMin), l1 = Math.Log(PrimeWaveMax);
        for (int j = 0; j < b.Length; j++)
        {
            double x = Math.Exp(l0 + (l1 - l0) * j / (b.Length - 1.0));
            if (x > NwN + 1) break;
            int i = (int)(Math.Sqrt(x) / nwDw);
            if (i < 0 || i > NwSamples) continue;
            if (Math.Abs(b[j]) > Math.Abs(g[i])) g[i] = b[j];
        }

        return g;
    }

    /// <summary>A wave's display values, scaled to its largest size over the outer three quarters.</summary>
    void NwShown(NwLane L, bool prime)
    {
        float[] dst = prime ? L.ShownP : L.ShownD;
        double top = 1e-9, xq = NwN * .25;
        double[] v = new double[NwSamples + 1];
        for (int i = 0; i <= NwSamples; i++)
        {
            double x = nwW[i] * nwW[i];
            if (x < NwXMin) continue;
            double s = prime ? (L.BetaP > 0f && L.BakedP != null ? (1 - L.BetaP) * L.AccP[i] + L.BetaP * L.BakedP[i] : L.AccP[i])
                : -Math.Sqrt(2.0) * Math.Pow(x, .25) * L.AccD[i];
            v[i] = s;
            if (x >= xq && x <= NwN) top = Math.Max(top, Math.Abs(s));
        }

        bool any = prime ? (L.TermsP > 0 || L.BetaP > 0f) : L.TermsD > 0;
        for (int i = 0; i <= NwSamples; i++) dst[i] = any ? Mathf.Clamp((float)(v[i] / top), -1.2f, 1.6f) : 0f;
    }

    /// <summary>The curve this frame: the spiral's turning times roll, integrated from the centre, then bent onto the saddle.</summary>
    void NwLayout(NwLane L, float roll, float bend)
    {
        Vector3 c = L.Centre;
        float k = bend * SaddleK;
        double px = 0, py = 0;
        L.P[0] = c;
        L.Up[0] = Vector3.back;
        for (int i = 0; i < NwSamples; i++)
        {
            double psi = roll * nwPhiMid[i];
            px += nwDs[i] * Math.Cos(psi);
            py += nwDs[i] * Math.Sin(psi);
            float x = (float)px, y = (float)py;
            L.P[i + 1] = c + new Vector3(x, y, k * (x * x - y * y));
            L.Up[i + 1] = new Vector3(2f * k * x, -2f * k * y, -1f).normalized;
        }

        // the line's left, within the surface (for a line along +x seen from above: up the screen)
        for (int i = 0; i <= NwSamples; i++)
        {
            Vector3 t = (L.P[Mathf.Min(NwSamples, i + 1)] - L.P[Mathf.Max(0, i - 1)]).normalized;
            L.Left[i] = Vector3.Cross(t, L.Up[i]).normalized;
        }

        L.Left[0] = L.Left[1];
    }

    /// <summary>Where x sits on the curve, with its up and left there.</summary>
    (Vector3 p, Vector3 up, Vector3 left) NwAt(NwLane L, double x)
    {
        double f = Math.Sqrt(Math.Max(0, x)) / nwDw;
        int i = Mathf.Clamp((int)f, 0, NwSamples - 1);
        float t = (float)(f - i);
        return (Vector3.Lerp(L.P[i], L.P[i + 1], t), Vector3.Lerp(L.Up[i], L.Up[i + 1], t).normalized, Vector3.Lerp(L.Left[i], L.Left[i + 1], t).normalized);
    }

    /// <summary>The wave's direction at a point: from the line's left (in the plane) turned up to the surface's normal.</summary>
    static Vector3 NwDir(Vector3 up, Vector3 left, float stand)
    {
        float a = stand * Mathf.PI * .5f;
        return (left * Mathf.Cos(a) + up * Mathf.Sin(a)).normalized;
    }

    /// <summary>The highest display value within a quarter of a unit of x.</summary>
    float NwPeak(NwLane L, double x)
    {
        int i0 = Mathf.Clamp((int)(Math.Sqrt(Math.Max(0, x - .25)) / nwDw), 0, NwSamples);
        int i1 = Mathf.Clamp((int)(Math.Sqrt(x + .25) / nwDw) + 1, 0, NwSamples);
        float top = 0f;
        for (int i = i0; i <= i1; i++) top = Mathf.Max(top, L.Shown[i]);
        return top;
    }

    void NwDraw(NwLane L, float roll, float stand, float h, float count, float labels, float dot, float lab, float rays, float vis, float ribbon)
    {
        float mix = Tw("nw.mix", 0f), split = Tw("nw.split", 0f), frameLimit = Tw("nw.frame", 1e6f);
        float frameH = NwFrameHeight(L);
        Color col = Color.Lerp(Factory.Palette.Cosine, DivisorWaveColor, mix);
        int cols = nwCols.Length - 1;
        int i0 = (int)Math.Ceiling(Math.Sqrt(NwXMin) / nwDw);
        bool smooth = L.BetaP <= 0f && (mix < .5f ? L.TermsP <= 3 : L.TermsD <= 3);
        // the line (the spiral): never thinner than about four pixels
        List<Vector3> curve = new(cols + 1);
        for (int c = 0; c <= cols; c++) curve.Add(L.P[nwCols[c]]);
        L.Curve.positionCount = curve.Count;
        L.Curve.SetPositions(curve.ToArray());
        L.Curve.widthMultiplier = Mathf.Max(Mathf.Lerp(.36f, .2f, roll), .0036f * frameH);
        // the wave: a filled band from the line to the wave (a column per ~.12 units of arc), its crest when it is smooth
        bool waveOn = h > .005f;
        L.CurtainR.gameObject.SetActive(waveOn);
        L.Crest.gameObject.SetActive(waveOn && smooth);
        Bounds box = new(L.P[0], Vector3.zero);
        if (waveOn)
        {
            List<Vector3> verts = new(cols * 2);
            List<Color> vc = new(cols * 2);
            List<int> tris = new(cols * 12);
            List<Vector3> crest = new(cols);
            Color colTop = split > .001f ? Factory.Palette.Cosine : col;
            Color foot = new(colTop.r, colTop.g, colTop.b, .22f), head = smooth ? new Color(colTop.r, colTop.g, colTop.b, .8f) : colTop;
            float[] shownTop = split > .001f ? L.ShownP : L.Shown;
            for (int c = 0; c < cols; c++)
            {
                int a = Mathf.Max(nwCols[c], i0), b = nwCols[c + 1];
                if (b <= a) continue;
                float lo = 0f, hi = 0f;
                for (int i = a; i <= b; i++)
                {
                    float sv = Mathf.Lerp(L.Shown[i], shownTop[i], split);
                    lo = Mathf.Min(lo, sv);
                    hi = Mathf.Max(hi, sv);
                }

                int j = (a + b) / 2;
                Vector3 p = L.P[j], d = NwDir(L.Up[j], L.Left[j], stand);
                int v = verts.Count;
                verts.Add(p + d * (lo * h));
                verts.Add(p + d * (hi * h + .04f));
                vc.Add(foot);
                vc.Add(head);
                if (v >= 2) tris.AddRange(new[] { v - 2, v - 1, v + 1, v - 2, v + 1, v, v - 2, v + 1, v - 1, v - 2, v, v + 1 });
                if (smooth) crest.Add(p + d * (Mathf.Lerp(L.Shown[j], shownTop[j], split) * h + .05f) + L.Up[j] * .04f);
                if ((c & 7) == 0 && nwW[j] * nwW[j] <= frameLimit)
                {
                    box.Encapsulate(p);
                    box.Encapsulate(p + d * (hi * h));
                    box.Encapsulate(p + d * (lo * h));
                }
            }

            L.Curtain.Clear();
            L.Curtain.SetVertices(verts);
            L.Curtain.SetColors(vc);
            L.Curtain.SetTriangles(tris, 0);
            L.Curtain.RecalculateBounds();
            if (smooth)
            {
                L.Crest.positionCount = crest.Count;
                L.Crest.SetPositions(crest.ToArray());
                L.Crest.startColor = L.Crest.endColor = colTop;
                L.Crest.widthMultiplier = Mathf.Max(Mathf.Lerp(.34f, .2f, roll), .003f * frameH);
            }
        }

        // with split: Voronoi's divisor wave hangs below the line, reflected (its spikes point down, into the saddle)
        bool underOn = waveOn && split > .001f;
        L.UnderR.gameObject.SetActive(underOn);
        if (underOn)
        {
            List<Vector3> uv = new(cols * 2);
            List<Color> uc = new(cols * 2);
            List<int> ut = new(cols * 12);
            Color dc = DivisorWaveColor;
            Color ufoot = new(dc.r, dc.g, dc.b, .22f * split), uhead = new(dc.r, dc.g, dc.b, split);
            for (int c = 0; c < cols; c++)
            {
                int a = Mathf.Max(nwCols[c], i0), b = nwCols[c + 1];
                if (b <= a) continue;
                float lo = 0f, hi = 0f;
                for (int i = a; i <= b; i++)
                {
                    lo = Mathf.Min(lo, L.ShownD[i]);
                    hi = Mathf.Max(hi, L.ShownD[i]);
                }

                int j = (a + b) / 2;
                Vector3 p = L.P[j], d = -NwDir(L.Up[j], L.Left[j], stand);
                int v = uv.Count;
                uv.Add(p + d * (lo * h));
                uv.Add(p + d * (hi * h + .04f));
                uc.Add(ufoot);
                uc.Add(uhead);
                if (v >= 2) ut.AddRange(new[] { v - 2, v - 1, v + 1, v - 2, v + 1, v, v - 2, v + 1, v - 1, v - 2, v, v + 1 });
                if ((c & 7) == 0 && nwW[j] * nwW[j] <= frameLimit) box.Encapsulate(p + d * (hi * h));
            }

            L.Under.Clear();
            L.Under.SetVertices(uv);
            L.Under.SetColors(uc);
            L.Under.SetTriangles(ut, 0);
            L.Under.RecalculateBounds();
        }

        // the ribbon: the wave's sign painted along the line (bright up, deep blue down), for reading a pattern from above
        bool ribbonOn = ribbon > .01f && waveOn;
        L.RibbonR.gameObject.SetActive(ribbonOn);
        if (ribbonOn)
        {
            List<Vector3> rv = new(cols * 2);
            List<Color> rc = new(cols * 2);
            List<int> rt = new(cols * 12);
            Color down = new(.1f, .16f, .5f), mid = new(.07f, .07f, .12f);
            for (int c = 0; c < cols; c++)
            {
                int a = Mathf.Max(nwCols[c], i0), b = nwCols[c + 1];
                if (b <= a) continue;
                int j = (a + b) / 2;
                float s = Mathf.Clamp(L.Shown[j], -1f, 1f);
                Color cc = s >= 0f ? Color.Lerp(mid, col * 1.15f, s) : Color.Lerp(mid, down, -s);
                cc.a = ribbon;
                int r = rv.Count;
                rv.Add(L.P[j] + L.Left[j] * .6f + L.Up[j] * .03f);
                rv.Add(L.P[j] - L.Left[j] * .6f + L.Up[j] * .03f);
                rc.Add(cc);
                rc.Add(cc);
                if (r >= 2) rt.AddRange(new[] { r - 2, r - 1, r + 1, r - 2, r + 1, r, r - 2, r + 1, r - 1, r - 2, r, r + 1 });
            }

            L.Ribbon.Clear();
            L.Ribbon.SetVertices(rv);
            L.Ribbon.SetColors(rc);
            L.Ribbon.SetTriangles(rt, 0);
            L.Ribbon.RecalculateBounds();
        }

        // the numbers: gold primes, blue squares, grey composites; each flares as it arrives
        int shown = Mathf.Clamp(Mathf.FloorToInt(count), 0, NwN);
        List<Vector3> dv = new(shown * 6);
        List<Color> dcl = new(shown * 6);
        List<int> dt = new(shown * 24);
        for (int n = 1; n <= shown; n++)
        {
            var (p, up, _) = NwAt(L, n);
            bool isPrime = n > 1 && !expNotPrime[n];
            int rt2 = Mathf.RoundToInt(Mathf.Sqrt(n));
            bool square = rt2 * rt2 == n;
            Color cc = isPrime ? PrimeBead : square ? new Color(.45f, .75f, 1f) : new Color(.62f, .65f, .78f);
            float s = Mathf.Max(dot * (isPrime || square ? .5f : .34f), (isPrime || square ? .0065f : .0045f) * frameH);
            OctaPoint(dv, dcl, dt, p + up * (s * .6f), s, cc);
            if (n <= frameLimit) box.Encapsulate(p);
        }

        L.Dots.Clear();
        L.Dots.SetVertices(dv);
        L.Dots.SetColors(dcl);
        L.Dots.SetTriangles(dt, 0);
        L.Dots.RecalculateBounds();
        if (count > L.LastCount + .5f && count - L.LastCount < 4f)
            for (int n = Mathf.FloorToInt(L.LastCount) + 1; n <= shown; n++)
                Flare(NwAt(L, n).p + Vector3.back * .3f, dot * 1.6f, .9f, 1);
        L.LastCount = count;
        // the labels, under the numbers (on the line's right side, away from the wave)
        for (int n = 1; n <= NwLabelMax; n++)
        {
            TextBox t = L.Labels[n];
            bool show = labels > .01f && n <= shown;
            if (t.gameObject.activeSelf != show) t.gameObject.SetActive(show);
            if (!show) continue;
            var (p, up, left) = NwAt(L, n);
            t.transform.position = p - left * (lab * .22f + dot * .6f) + up * .05f;
            t.TextField.fontSize = lab;
            t.TextField.alpha = labels * vis;
        }

        // the squares ray and the pronic ray
        bool rayOn = rays > .01f;
        L.RaySquares.gameObject.SetActive(rayOn);
        L.RayPronic.gameObject.SetActive(rayOn);
        if (rayOn)
        {
            float k = Tw("nw.bend", 0f) * SaddleK, rMax = R * Mathf.Sqrt(NwN) * 1.04f;
            Vector3[] a = new Vector3[41], b2 = new Vector3[41];
            for (int i = 0; i <= 40; i++)
            {
                float r = rMax * i / 40f;
                a[i] = L.Centre + new Vector3(r, 0f, k * r * r) + Vector3.back * .12f;
                b2[i] = L.Centre + new Vector3(-r, 0f, k * r * r) + Vector3.back * .12f;
            }

            L.RaySquares.positionCount = 41;
            L.RaySquares.SetPositions(a);
            L.RayPronic.positionCount = 41;
            L.RayPronic.SetPositions(b2);
            Color rcol = new(.45f, .75f, 1f, rays);
            L.RaySquares.startColor = L.RaySquares.endColor = rcol;
            L.RayPronic.startColor = L.RayPronic.endColor = rcol;
        }

        L.Box = box;
        foreach (Renderer r in new Renderer[] { L.Curve, L.Crest, L.RaySquares, L.RayPronic, L.CurtainR, L.DotsR, L.RibbonR, L.UnderR })
            Factory.SetColor(r, new Color(1f, 1f, 1f, vis));
    }

    /// <summary>The count of waves added, fixed in the top-left corner of the screen (the zeros' or Voronoi's, by nw.mix).</summary>
    void NwCounter(NwLane L, float counter)
    {
        if (counter <= .01f)
        {
            Hud(0, "", Color.white);
            return;
        }

        bool prime = Tw("nw.mix", 0f) < .5f;
        float terms = L.BetaP > 0f ? 1000f * Mathf.Pow(10f, L.BetaP) : L.TermsP;
        string text = prime
            ? (L.FirstP > 1 && L.TermsP == 1 ? $"zero #{L.FirstP} of ζ alone" : $"{terms:N0} zero{(Mathf.Abs(terms - 1f) < .5f ? "" : "s")} of ζ")
            : (L.TermsD == 1 ? "Voronoi's first wave" : $"{L.TermsD:N0} of Voronoi's waves");
        Color c = prime ? Factory.Palette.Cosine : DivisorWaveColor;
        Hud(0, text, new Color(c.r, c.g, c.b, counter));
    }

    /// <summary>An octahedron of half-size s at p (the point clouds' marker), appended to the lists.</summary>
    static void OctaPoint(List<Vector3> verts, List<Color> colors, List<int> tris, Vector3 p, float s, Color c)
    {
        int v = verts.Count;
        verts.Add(p + Vector3.right * s); verts.Add(p - Vector3.right * s);
        verts.Add(p + Vector3.up * s); verts.Add(p - Vector3.up * s);
        verts.Add(p + Vector3.forward * s); verts.Add(p - Vector3.forward * s);
        for (int k = 0; k < 6; k++) colors.Add(c);
        int[] f = { 0, 2, 4, 2, 1, 4, 1, 3, 4, 3, 0, 4, 2, 0, 5, 1, 2, 5, 3, 1, 5, 0, 3, 5 };
        foreach (int i in f) tris.Add(v + i);
    }

    /// <summary>A white flare at number n (as the narration names it).</summary>
    public void ExpNwFlare(double n, string lane)
    {
        if (nw == null) return;
        var (p, up, _) = NwAt(nw, n);
        Flare(p + up * .4f, Tw("nw.dot", 2f) * 2.2f, 1.2f, 2);
    }

    /// <summary>The bounds of the line as drawn now (its curve, the numbers shown and its wave), for the camera.</summary>
    public Bounds NwBounds(string lane) => nw == null ? new Bounds(NumberWorldOrigin, Vector3.one * 40f) : nw.Box;

    /// <summary>For callouts: the top of the wave at n (or its number's dot when the wave is down).</summary>
    public Vector3 NwPoint(double n, string lane)
    {
        if (nw == null) return NumberWorldOrigin;
        var (p, up, left) = NwAt(nw, n);
        float h = Tw("nw.h", 22f) * Tw("nw.wave", 0f);
        Vector3 d = NwDir(up, left, Tw("nw.stand", 0f));
        return p + d * (Mathf.Max(0f, NwPeak(nw, n)) * h) + up * (Tw("nw.dot", 2f) * .5f);
    }
}
