using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The number line that becomes the Sacks spiral, and then the saddle (the long-form segment on discovery 1). Two lanes,
/// each the numbers 1 … 150 with a wave over them: "p", the zeros' wave −Σ cos(γ_k log x) (the home view's master wave),
/// and "d", the derivative of Voronoi's divisor formula times √x (a spike at every n, as tall as its count of divisors).
/// Each lane is drawn through one map from x to a point, with parameters the script eases (exp:tw):
///   nw.roll.L   0: a straight number line from the lane's centre to the right, 1: the Sacks spiral (radius R√x, angle
///               2π√x). In between, the curve with the spiral's turning times roll, so the line winds up continuously and
///               every number keeps its distance along it (πR per unit of x, as on the spiral).
///   nw.bend     0: flat, 1: the tour's saddle z = K(x² − y²) round each lane's centre; heights stand on its normal.
///   nw.terms.L  the number of waves added (zeros of ζ from the nw.first.L-th, or terms of Voronoi's series), eased
///               geometrically; above 1 000 zeros the sum blends into the baked 10 000-zero wave.
///   nw.wave.L (0 … 1, times nw.h), nw.count.L (numbers shown), nw.labels.L, nw.dot, nw.label, nw.rays.L, nw.vis.L.
/// </summary>
public partial class SacksSpiral
{
    public const float NumberWorldOffset = 100000f;
    const int NwN = 150, NwSamples = 36000, NwLabelMax = 60;
    const float NwXMin = 1.5f, NwColumnArc = .12f, NwLaneX = 22f;

    static double[] nwW, nwLogX, nwPhiMid, nwDs;
    static int[] nwCols;
    static double nwDw;

    class NwLane
    {
        public string Name;
        public Vector3 Centre;
        public Transform Root;
        public double[] Acc;
        public double[] Baked;
        public int Terms, First = 1;
        public float Beta;
        public readonly Vector3[] P = new Vector3[NwSamples + 1], Nrm = new Vector3[NwSamples + 1];
        public readonly float[] Shown = new float[NwSamples + 1];
        public LineRenderer Curve, Crest, RaySquares, RayPronic;
        public Mesh Curtain, Ribbon, Dots;
        public MeshRenderer CurtainR, RibbonR, DotsR;
        public TextBox[] Labels;
        public TextBox Counter;
        public string LayoutSig = "", DrawSig = "";
        public Bounds Box;
        public bool WavesChanged = true;
        public float LastCount;
    }

    NwLane[] nwLanes;

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

    /// <summary>Set up the two lanes (both hidden until nw.vis.L is raised) and start the scene's frame loop.</summary>
    public void ExpNumberWorld()
    {
        NwGrid();
        EnsureDivisorCounts(4001);
        ExpEnsureSieve(NwN + 2);
        ClearTweens("nw.");
        nwLanes = new NwLane[2];
        for (int k = 0; k < 2; k++)
        {
            NwLane L = new() { Name = k == 0 ? "p" : "d" };
            L.Centre = NumberWorldOrigin + new Vector3(k == 0 ? -NwLaneX : NwLaneX, 0f, 0f);
            L.Root = ExpGroup($"NW lane {L.Name}").transform;
            L.Acc = new double[NwSamples + 1];
            Color col = k == 0 ? Factory.Palette.Cosine : DivisorWaveColor;
            L.Curve = ExpLine(L.Root, "number line", .12f, new Color(.62f, .66f, .85f), Factory.NoGlow, new[] { L.Centre, L.Centre });
            L.Crest = ExpLine(L.Root, "wave", .22f, col, Factory.GlowIntensity, new[] { L.Centre, L.Centre });
            L.RaySquares = ExpLine(L.Root, "squares ray", .14f, new Color(.45f, .75f, 1f), Factory.SoftGlowIntensity, new[] { L.Centre, L.Centre });
            L.RayPronic = ExpLine(L.Root, "pronic ray", .14f, new Color(.45f, .75f, 1f), Factory.SoftGlowIntensity, new[] { L.Centre, L.Centre });
            L.Curtain = NwMesh(L.Root, "curtain", out L.CurtainR);
            Factory.SetIntensity(L.CurtainR, .82f);
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

            L.Counter = ExpLabelWide(L.Root, "", L.Centre, 4f, col, TextAlignmentOptions.Left);
            Outline(L.Counter.TextField);
            L.Root.gameObject.SetActive(false);
            nwLanes[k] = L;
        }

        StartCoroutine(NumberWorldLoop(nwLanes));
    }

    IEnumerator NumberWorldLoop(NwLane[] lanes)
    {
        while (lanes == nwLanes && lanes[0].Root != null && lanes[1].Root != null)
        {
            foreach (NwLane L in lanes) NwUpdate(L);
            yield return null;
        }
    }

    void NwUpdate(NwLane L)
    {
        string n = L.Name;
        float vis = Tw($"nw.vis.{n}", 0f);
        bool on = vis > .002f;
        if (L.Root.gameObject.activeSelf != on) L.Root.gameObject.SetActive(on);
        if (!on) return;
        float roll = Tw($"nw.roll.{n}", 0f), bend = Tw("nw.bend", 0f);
        float h = Tw("nw.h", 22f) * Tw($"nw.wave.{n}", 0f);
        float count = Tw($"nw.count.{n}", 0f), labels = Tw($"nw.labels.{n}", 0f), dot = Tw("nw.dot", 2f), lab = Tw("nw.label", 11f);
        float rays = Tw($"nw.rays.{n}", 0f), counter = Tw($"nw.counter.{n}", 0f);
        int first = Mathf.Max(1, Mathf.RoundToInt(Tw($"nw.first.{n}", 1f)));
        NwTerms(L, first, Tw($"nw.terms.{n}", 0f));
        string layout = $"{roll:F5}|{bend:F5}";
        bool moved = layout != L.LayoutSig;
        if (moved)
        {
            L.LayoutSig = layout;
            NwLayout(L, roll, bend);
        }

        // the lane's bounds (the curve and its wave), for the camera's "nwbox" shots while the line winds up
        Bounds box = new(L.P[0], Vector3.zero);
        int shownTo = Mathf.Clamp((int)(Math.Sqrt(Mathf.Max(1f, count)) / nwDw), 1, NwSamples);
        for (int i = 0; i <= shownTo; i += 40)
        {
            box.Encapsulate(L.P[i]);
            box.Encapsulate(L.P[i] + L.Nrm[i] * (h * .8f));
        }

        L.Box = box;
        NwCounter(L, counter, vis);
        string draw = $"{h:F3}|{count:F2}|{labels:F3}|{dot:F3}|{lab:F3}|{rays:F3}|{vis:F3}";
        if (!moved && !L.WavesChanged && draw == L.DrawSig) return;
        L.DrawSig = draw;
        L.WavesChanged = false;
        NwDraw(L, roll, h, count, labels, dot, lab, rays, vis);
    }

    /// <summary>Bring the lane's sum to the wanted number of terms (adding or removing waves), and refresh its display values.</summary>
    void NwTerms(NwLane L, int first, float termsF)
    {
        bool prime = L.Name == "p";
        int cap = prime ? 1000 : 4000;
        int k = Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(termsF, cap)), 0, cap);
        float beta = prime && termsF > 1000f ? Mathf.Clamp01(Mathf.Log(termsF / 1000f) / Mathf.Log(10f)) : 0f;
        bool changed = false;
        if (first != L.First)
        {
            Array.Clear(L.Acc, 0, L.Acc.Length);
            L.Terms = 0;
            L.First = first;
            changed = true;
        }

        if (k != L.Terms)
        {
            NwAddTerms(L, L.Terms, k);
            L.Terms = k;
            changed = true;
        }

        if (Mathf.Abs(beta - L.Beta) > 1e-4f)
        {
            L.Beta = beta;
            if (beta > 0f && L.Baked == null) L.Baked = NwBakedOnGrid();
            changed = true;
        }

        if (!changed) return;
        NwShown(L);
        L.WavesChanged = true;
    }

    /// <summary>Add the terms with indices [from, to) to the lane's sum, or take [to, from) away.</summary>
    void NwAddTerms(NwLane L, int from, int to)
    {
        int a = Mathf.Min(from, to), b = Mathf.Max(from, to);
        double sign = to > from ? 1.0 : -1.0;
        int m = NwSamples;
        int i0 = (int)Math.Ceiling(Math.Sqrt(NwXMin) / nwDw);
        if (L.Name == "p")
        {
            double[] zs = Main.Instance.zetaZeroValues;
            for (int j = a; j < b; j++)
            {
                int z = L.First - 1 + j;
                if (z >= zs.Length) break;
                double g = zs[z];
                for (int i = i0; i <= m; i++) L.Acc[i] -= sign * Math.Cos(g * nwLogX[i]);
            }

            return;
        }

        // Voronoi's m-th wave d(m) m^(−¼) sin(4π√m w − π/4), with the smooth weight of a 4 000-term sum; the phase is linear
        // in w, so each sample is the last one turned by a fixed angle
        for (int j = a; j < b; j++)
        {
            int mm = L.First + j;
            double wgt = Math.Exp(-4.0 * (mm / 4000.0) * (mm / 4000.0)) * waveDivisors[Mathf.Min(mm, waveDivisors.Length - 1)] * Math.Pow(mm, -.25);
            double step = 4.0 * Math.PI * Math.Sqrt(mm) * nwDw, c0 = -Math.PI / 4.0 + step * i0;
            double cr = Math.Cos(c0), ci = Math.Sin(c0), rr = Math.Cos(step), ri = Math.Sin(step);
            for (int i = i0; i <= m; i++)
            {
                L.Acc[i] += sign * wgt * ci;
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

    /// <summary>The display values: the wave (times √x·… for Voronoi's), scaled to its largest size over the outer three quarters.</summary>
    void NwShown(NwLane L)
    {
        bool prime = L.Name == "p";
        int m = NwSamples;
        double top = 1e-9;
        double xq = NwN * .25;
        double[] v = new double[m + 1];
        for (int i = 0; i <= m; i++)
        {
            double x = nwW[i] * nwW[i];
            if (x < NwXMin) continue;
            double s = prime ? (L.Beta > 0f && L.Baked != null ? (1 - L.Beta) * L.Acc[i] + L.Beta * L.Baked[i] : L.Acc[i])
                : -Math.Sqrt(2.0) * Math.Pow(x, .25) * L.Acc[i];
            v[i] = s;
            if (x >= xq && x <= NwN) top = Math.Max(top, Math.Abs(s));
        }

        for (int i = 0; i <= m; i++) L.Shown[i] = Mathf.Clamp((float)(v[i] / top), -1.2f, 1.6f);
    }

    /// <summary>The lane's curve this frame: the spiral's turning times roll, integrated from the centre, then bent onto the saddle.</summary>
    void NwLayout(NwLane L, float roll, float bend)
    {
        Vector3 c = L.Centre;
        float k = bend * SaddleK;
        double px = 0, py = 0;
        L.P[0] = c;
        L.Nrm[0] = Vector3.back;
        for (int i = 0; i < NwSamples; i++)
        {
            double psi = roll * nwPhiMid[i];
            px += nwDs[i] * Math.Cos(psi);
            py += nwDs[i] * Math.Sin(psi);
            float x = (float)px, y = (float)py;
            L.P[i + 1] = c + new Vector3(x, y, k * (x * x - y * y));
            L.Nrm[i + 1] = new Vector3(2f * k * x, -2f * k * y, -1f).normalized;
        }
    }

    /// <summary>Where x sits on the lane's curve (between grid samples), and the normal there.</summary>
    (Vector3 p, Vector3 n) NwAt(NwLane L, double x)
    {
        double f = Math.Sqrt(Math.Max(0, x)) / nwDw;
        int i = Mathf.Clamp((int)f, 0, NwSamples - 1);
        float t = (float)(f - i);
        return (Vector3.Lerp(L.P[i], L.P[i + 1], t), Vector3.Lerp(L.Nrm[i], L.Nrm[i + 1], t).normalized);
    }

    /// <summary>The highest display value of the lane's wave within a quarter of a unit of x.</summary>
    float NwPeak(NwLane L, double x)
    {
        int i0 = Mathf.Clamp((int)(Math.Sqrt(Math.Max(0, x - .25)) / nwDw), 0, NwSamples);
        int i1 = Mathf.Clamp((int)(Math.Sqrt(x + .25) / nwDw) + 1, 0, NwSamples);
        float top = 0f;
        for (int i = i0; i <= i1; i++) top = Mathf.Max(top, L.Shown[i]);
        return top;
    }

    void NwDraw(NwLane L, float roll, float h, float count, float labels, float dot, float lab, float rays, float vis)
    {
        bool prime = L.Name == "p";
        Color col = prime ? Factory.Palette.Cosine : DivisorWaveColor;
        int cols = nwCols.Length - 1;
        int i0 = (int)Math.Ceiling(Math.Sqrt(NwXMin) / nwDw);
        // the number line / spiral itself
        List<Vector3> curve = new(cols + 1);
        for (int c = 0; c <= cols; c++) curve.Add(L.P[nwCols[c]]);
        L.Curve.positionCount = curve.Count;
        L.Curve.SetPositions(curve.ToArray());
        L.Curve.widthMultiplier = Mathf.Lerp(.32f, .12f, roll);
        // the wave: a curtain from the curve to the wave (a column per ~.12 units of arc, spanning the values it covers)
        bool smooth = L.Terms <= 3 && L.Beta <= 0f;
        bool waveOn = h > .01f;
        L.CurtainR.gameObject.SetActive(waveOn);
        L.Crest.gameObject.SetActive(waveOn && smooth);
        L.RibbonR.gameObject.SetActive(waveOn && smooth && roll > .25f);
        if (waveOn)
        {
            List<Vector3> verts = new(cols * 2);
            List<Color> vc = new(cols * 2);
            List<int> tris = new(cols * 12);
            List<Vector3> crest = new(cols);
            List<Vector3> rv = new(cols * 2);
            List<Color> rc = new(cols * 2);
            List<int> rt = new(cols * 12);
            Color foot = new(col.r, col.g, col.b, .1f), head = smooth ? new Color(col.r, col.g, col.b, Mathf.Lerp(.55f, .18f, roll)) : col;
            Color down = new(.1f, .16f, .5f), mid = new(.07f, .07f, .12f);
            for (int c = 0; c < cols; c++)
            {
                int a = Mathf.Max(nwCols[c], i0), b = nwCols[c + 1];
                if (b <= a) continue;
                float lo = 0f, hi = 0f;
                for (int i = a; i <= b; i++)
                {
                    lo = Mathf.Min(lo, L.Shown[i]);
                    hi = Mathf.Max(hi, L.Shown[i]);
                }

                int j = (a + b) / 2;
                Vector3 p = L.P[j], nrm = L.Nrm[j];
                int v = verts.Count;
                verts.Add(p + nrm * (lo * h));
                verts.Add(p + nrm * (hi * h + .06f));
                vc.Add(foot);
                vc.Add(head);
                if (v >= 2) tris.AddRange(new[] { v - 2, v - 1, v + 1, v - 2, v + 1, v, v - 2, v + 1, v - 1, v - 2, v, v + 1 });
                if (smooth)
                {
                    crest.Add(p + nrm * (L.Shown[j] * h + .08f));
                    Vector3 along = (L.P[Mathf.Min(b, NwSamples)] - L.P[a]).normalized, side = Vector3.Cross(along, nrm).normalized;
                    float s = Mathf.Clamp(L.Shown[j], -1f, 1f);
                    Color cc = s >= 0f ? Color.Lerp(mid, col * 1.15f, s) : Color.Lerp(mid, down, -s);
                    int r = rv.Count;
                    float half = Mathf.Lerp(.3f, .62f, roll);
                    rv.Add(p + side * half + nrm * .05f);
                    rv.Add(p - side * half + nrm * .05f);
                    rc.Add(cc);
                    rc.Add(cc);
                    if (r >= 2) rt.AddRange(new[] { r - 2, r - 1, r + 1, r - 2, r + 1, r, r - 2, r + 1, r - 1, r - 2, r, r + 1 });
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
                L.Ribbon.Clear();
                L.Ribbon.SetVertices(rv);
                L.Ribbon.SetColors(rc);
                L.Ribbon.SetTriangles(rt, 0);
                L.Ribbon.RecalculateBounds();

            }
        }

        // the numbers: gold primes, blue squares, grey composites; each flares as it arrives
        int shown = Mathf.Clamp(Mathf.FloorToInt(count), 0, NwN);
        List<Vector3> dv = new(shown * 6);
        List<Color> dcl = new(shown * 6);
        List<int> dt = new(shown * 24);
        for (int n = 1; n <= shown; n++)
        {
            var (p, nrm) = NwAt(L, n);
            bool isPrime = n > 1 && !expNotPrime[n];
            int rt2 = Mathf.RoundToInt(Mathf.Sqrt(n));
            bool square = rt2 * rt2 == n;
            Color cc = isPrime ? PrimeBead : square ? new Color(.45f, .75f, 1f) : new Color(.55f, .58f, .7f);
            float s = dot * (isPrime || square ? .5f : .34f);
            OctaPoint(dv, dcl, dt, p + nrm * (s * .6f), s, cc);
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
        // the labels under the numbers on the line
        for (int n = 1; n <= NwLabelMax; n++)
        {
            TextBox t = L.Labels[n];
            bool show = labels > .01f && n <= shown;
            if (t.gameObject.activeSelf != show) t.gameObject.SetActive(show);
            if (!show) continue;
            var (p, nrm) = NwAt(L, n);
            t.transform.position = p - nrm * (lab * .3f + dot * .6f) + Vector3.back * .01f;
            t.TextField.fontSize = lab;
            t.TextField.alpha = labels * vis;
        }

        // the squares ray and the pronic ray, on the plane (or the saddle)
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

        foreach (Renderer r in new Renderer[] { L.Curve, L.Crest, L.RaySquares, L.RayPronic, L.CurtainR, L.DotsR })
            Factory.SetColor(r, new Color(1f, 1f, 1f, vis));
        Factory.SetColor(L.RibbonR, new Color(1f, 1f, 1f, vis * Mathf.Clamp01((roll - .25f) / .5f)));
    }

    /// <summary>The count of waves added, held at a fixed place in the shot (left for the zeros, right for Voronoi's).</summary>
    void NwCounter(NwLane L, float counter, float vis)
    {
        bool prime = L.Name == "p";
        bool on = counter > .01f && (L.Terms > 0 || L.Beta > 0f);
        L.Counter.gameObject.SetActive(on);
        if (!on) return;
        float terms = L.Beta > 0f ? 1000f * Mathf.Pow(10f, L.Beta) : L.Terms;
        L.Counter.TextField.text = prime ? (L.First > 1 && L.Terms == 1 ? $"zero #{L.First} alone" : $"{terms:N0} zero{(terms > 1.5f ? "s" : "")} of ζ")
            : (L.Terms == 1 ? "Voronoi's first wave" : $"{terms:N0} of Voronoi's waves");
        HudPlace(L.Counter, L.Box.center, new Vector2(prime ? .045f : .955f, .74f), .038f);
        L.Counter.TextField.alpha = counter * vis;
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

    /// <summary>A white flare at number n of lane L (as the narration names it).</summary>
    public void ExpNwFlare(double n, string lane)
    {
        if (nwLanes == null) return;
        NwLane L = lane == "d" ? nwLanes[1] : nwLanes[0];
        var (p, nrm) = NwAt(L, n);
        Flare(p + nrm * .4f, Tw("nw.dot", 2f) * 2.2f, 1.2f, 2);
    }

    /// <summary>The bounds of lane L as drawn now (its curve, the numbers shown and its wave), for the camera.</summary>
    public Bounds NwBounds(string lane) =>
        nwLanes == null ? new Bounds(NumberWorldOrigin, Vector3.one * 40f) : (lane == "d" ? nwLanes[1] : nwLanes[0]).Box;

    /// <summary>For callouts: the top of lane L's wave at n (or its number's dot when the wave is down).</summary>
    public Vector3 NwPoint(double n, string lane)
    {
        if (nwLanes == null) return NumberWorldOrigin;
        NwLane L = lane == "d" ? nwLanes[1] : nwLanes[0];
        var (p, nrm) = NwAt(L, n);
        float h = Tw("nw.h", 22f) * Tw($"nw.wave.{L.Name}", 0f);
        return p + nrm * (Mathf.Max(0f, NwPeak(L, n)) * h + Tw("nw.dot", 2f) * .7f);
    }
}
