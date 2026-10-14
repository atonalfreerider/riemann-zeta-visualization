using System.Collections.Generic;
using Meta.Numerics;
using TMPro;
using UnityEngine;

/// <summary>
/// Thought experiments drawn on the Sacks saddle (branch experiments/sacks-cancellation). Rendered by
/// ExperimentsRenderTest; docs/experiments/README.md says what each one claims and what it does not.
///
///  A  Cancellation curves C_d: n = u(u + d), u = 1, 2, …  (Fermat's n = m² − (d/2)², the product curves Sacks noticed).
///     Every composite lies on one with u ≥ 2; a prime only on the curve born at it (u = 1, n = d + 1).
///     Euler's line E(k) = k² + k + 41 is cut by them in two families (k ≤ 120):
///        I   k = 40 + j²            E(k) = E(j − 1) · E(j)          (neighbours on the line, curve C_2j)
///        II  k = 2i² + i + 81       E(k) = E(i) · D(i)              D(j) = (2j)² + 163, the sister line
///            k = 2i² + 3i + 82      E(k) = E(i) · D(i + 1)
///     Both are products in Z[(1 + √−163)/2]: E(k) = N((2k + 1 + √−163)/2), D(j) = N(2j + √−163).
///  A2 The zeta master wave −Σ cos(γ · log n) evaluated along Euler's line: the log clock's verdict on the crossings.
///  C  Composite depth (how many cancellation curves pass through n) and Voronoi's explicit formula for the divisor
///     staircase, whose waves cos(4π√(nx)) tick with the Sacks clock: the saddle z ∝ n · cos(4π√n) is its first harmonic.
///  D  The zeros on their own Sacks spiral: radius √G, angle 2πG with G = θ(γ)/π + 1 (Gram points play the squares).
///  E  Anatomy of the chain at a dial value t: knots at n = t/((2k + 1)π), spokes at n = t/(2πk) — the mirror images of
///     the half-integers and the integers under n ↔ t/(2πn) — and the tail spiral winding around ζ(½ + it).
///  F  One zero's crests on the spiral, n_k = e^{2πk/γ}: equally spaced in the logarithm of the winding angle,
///     m crests per turn at n = (γ/πm)², the inverse-square ladder of the moiré arms.
///  G  The truncated explicit formula −Σ_{z≤K} cos(γ_z log n) on the number field for K = 1 … 10 000: a stack of discs
///     showing the vortex pattern of few zeros turn into the primes.
/// </summary>
public partial class SacksSpiral
{
    GameObject experimentsRoot;
    GameObject backdropRoot;

    Transform ExpRoot()
    {
        if (experimentsRoot == null)
        {
            experimentsRoot = new GameObject("Experiments");
            experimentsRoot.transform.SetParent(transform, false);
        }

        return experimentsRoot.transform;
    }

    GameObject ExpGroup(string name)
    {
        GameObject g = new(name);
        g.transform.SetParent(ExpRoot(), false);
        return g;
    }

    public void ClearExperiments()
    {
        if (experimentsRoot != null) Destroy(experimentsRoot);
        experimentsRoot = null;
    }

    /// <summary>Hide every stage element except the saddle's routes: the experiments draw their own number field.</summary>
    public void ExpCleanStage()
    {
        gameObject.SetActive(true);
        StopGrowth();
        foreach (GameObject r in new[] { growRoot, hypothesisRoot, zeroDropsRoot, explorationRoot, sieveRoot, latticeRoot, routeNumbersRoot })
        {
            if (r != null) r.SetActive(false);
        }

        foreach (GameObject d in dots) d.SetActive(false);
        foreach (TextBox l in labels) l.gameObject.SetActive(false);
        if (spiralPath != null) spiralPath.enabled = false;
        if (caption != null) caption.gameObject.SetActive(false);
    }

    /// <summary>Every number 2 … nMax as a dot on the current surface: primes gold, squares red, the rest dim.</summary>
    public void ExpNumberField(int nMax, float compositeSize = .11f, float primeSize = .24f)
    {
        ClearBackdrop();
        backdropRoot = new GameObject("Backdrop");
        backdropRoot.transform.SetParent(transform, false);
        List<Vector3> pts = new();
        List<Color> cols = new();
        List<float> sizes = new();
        for (int n = 2; n <= nMax; n++)
        {
            bool p = IsPrime(n), sq = IsSquare(n);
            pts.Add(OnSpiral(n, 0f, 0f));
            cols.Add(p ? Factory.Palette.Zero : sq ? SquareColor : new Color(.32f, .32f, .4f));
            sizes.Add(p ? primeSize : sq ? primeSize * .8f : compositeSize);
        }

        ExpPointCloud(backdropRoot.transform, "numbers", pts, cols, sizes, Factory.SoftGlowIntensity);
    }

    public void ClearBackdrop()
    {
        if (backdropRoot != null) Destroy(backdropRoot);
        backdropRoot = null;
    }

    public void ShowBackdrop(bool on)
    {
        if (backdropRoot != null) backdropRoot.SetActive(on);
    }

    TextBox ExpLabel(Transform parent, string text, Vector3 pos, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        TextBox l = TextBoxLabel(text, size, color);
        l.transform.SetParent(parent, false);
        l.Alignment = align;
        l.transform.position = pos;
        return l;
    }

    LineRenderer ExpLine(Transform parent, string name, float width, Color color, float glow, Vector3[] pts, bool loop = false)
    {
        LineRenderer line = Factory.NewLine(name, width, color, glow);
        line.transform.SetParent(parent, false);
        line.numCornerVertices = 3;
        line.loop = loop;
        line.positionCount = pts.Length;
        line.SetPositions(pts);
        return line;
    }

    GameObject ExpDot(Transform parent, string name, Vector3 pos, float size, Color color, float glow = Factory.GlowIntensity)
    {
        GameObject d = Factory.NewSphere(name, size, color, glow);
        d.transform.SetParent(parent, false);
        d.transform.position = pos;
        return d;
    }

    LineRenderer ExpRing(Transform parent, string name, Vector3 center, float radius, Color color, float width = .04f, float glow = Factory.SoftGlowIntensity)
    {
        Vector3[] ring = new Vector3[28];
        for (int i = 0; i < ring.Length; i++)
        {
            float a = i / (float)ring.Length * Mathf.PI * 2f;
            ring[i] = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * radius;
        }

        return ExpLine(parent, name, width, color, glow, ring, true);
    }

    /// <summary>A circle of Sacks radius √n drawn on the current surface.</summary>
    LineRenderer ExpSurfaceRing(Transform parent, string name, float n, Color color, float width, float lift = .3f)
    {
        float r = R * Mathf.Sqrt(n);
        Vector3[] pts = new Vector3[120];
        for (int i = 0; i < pts.Length; i++)
        {
            float a = i / (float)pts.Length * Mathf.PI * 2f;
            float x = r * Mathf.Cos(a), y = r * Mathf.Sin(a);
            pts[i] = origin + new Vector3(x, y, bend * SaddleK * (x * x - y * y)) + SurfaceNormal(x, y) * lift;
        }

        return ExpLine(parent, name, width, color, Factory.SoftGlowIntensity, pts, true);
    }

    /// <summary>One mesh of small octahedra, each with its own vertex colour and size (thousands of points in one draw).</summary>
    MeshRenderer ExpPointCloud(Transform parent, string name, IList<Vector3> pts, IList<Color> cols, IList<float> sizes, float glow)
    {
        Vector3[] axes = { Vector3.right, Vector3.up, Vector3.forward };
        List<Vector3> verts = new(pts.Count * 6);
        List<Color> colors = new(pts.Count * 6);
        List<int> tris = new(pts.Count * 24);
        int[][] faces =
        {
            new[] { 0, 2, 4 }, new[] { 2, 1, 4 }, new[] { 1, 3, 4 }, new[] { 3, 0, 4 },
            new[] { 2, 0, 5 }, new[] { 1, 2, 5 }, new[] { 3, 1, 5 }, new[] { 0, 3, 5 }
        };
        for (int i = 0; i < pts.Count; i++)
        {
            int v = verts.Count;
            float s = sizes[i];
            Vector3 p = pts[i];
            verts.Add(p + axes[0] * s); verts.Add(p - axes[0] * s);
            verts.Add(p + axes[1] * s); verts.Add(p - axes[1] * s);
            verts.Add(p + axes[2] * s); verts.Add(p - axes[2] * s);
            for (int k = 0; k < 6; k++) colors.Add(cols[i]);
            foreach (int[] f in faces) { tris.Add(v + f[0]); tris.Add(v + f[1]); tris.Add(v + f[2]); }
        }

        Mesh mesh = new() { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(verts);
        mesh.SetColors(colors);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = Factory.mainMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Factory.SetIntensity(mr, glow);
        return mr;
    }

    /// <summary>A point on the current surface for real n with turn index k (the route trick: smooth between turns).</summary>
    Vector3 ExpRoutePoint(float n, float k, float lift = .15f) => RoutePoint(n, k) + SurfaceNormalAt(n) * lift;

    static int SmallestDivisorUpToSqrt(int n)
    {
        for (int u = 2; u * u <= n; u++) if (n % u == 0) return u;
        return 1;
    }

    static int Depth(int n)
    {
        int c = 0;
        for (int u = 1; u * u <= n; u++) if (n % u == 0) c++;
        return c;
    }

    static int Euler(int k) => k * k + k + 41;
    static int Sister(int j) => 4 * j * j + 163;

    // ------------------------------------------------------------------------------------------------
    // A. cancellation curves
    // ------------------------------------------------------------------------------------------------

    static Color CurveColor(int d)
    {
        bool even = d % 2 == 0;
        Color near = even ? SquareColor : PronicColor;
        Color far = even ? new Color(1f, .75f, .5f) : new Color(.55f, .9f, 1f);
        return Color.Lerp(near, far, Mathf.Clamp01(d / 30f));
    }

    static readonly Color SisterColor = new(1f, .55f, 1f);
    static readonly Color FamilyOneColor = new(1f, .8f, .3f);
    static readonly Color FamilyTwoColor = new(1f, .55f, 1f);

    /// <summary>
    /// C_d for d = 0..maxD, each from its birth u = 1 (the number d + 1) outward while u(u + d) ≤ nMax, plus the curves
    /// a composite on Euler's line needs. Euler's line is extended to eulerKMax with its values dotted (gold prime,
    /// red composite); with <paramref name="chords"/> every composite is joined to its two factors and labelled by family.
    /// </summary>
    public void ExpCancellationCurves(int maxD, int nMax, int eulerKMax, bool chords, float labelSize, bool sister, bool farSegments)
    {
        Transform g = ExpGroup("A cancellation curves").transform;

        // the composites on Euler's line and the curve each one sits on (d → the u of the crossing)
        List<(int k, int u, int v)> composites = new();
        Dictionary<int, int> farCurves = new();
        for (int k = 0; k <= eulerKMax; k++)
        {
            int n = Euler(k);
            int u = SmallestDivisorUpToSqrt(n);
            if (u == 1) continue;
            composites.Add((k, u, n / u));
            int d = n / u - u;
            if (d > maxD && !farCurves.ContainsKey(d)) farCurves[d] = u;
        }

        for (int d = 0; d <= maxD; d++)
        {
            float uMax = Mathf.Sqrt(nMax + d * d / 4f) - d / 2f;
            if (uMax < 1.5f) continue;
            int steps = Mathf.FloorToInt((uMax - 1f) * RouteSteps);
            Vector3[] pts = new Vector3[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                float u = 1f + i / (float)RouteSteps;
                pts[i] = ExpRoutePoint(u * (u + d), u, .12f);
            }

            Color c = CurveColor(d);
            ExpLine(g, "C_" + d, .07f, c, Factory.SoftGlowIntensity, pts);
            ExpRing(g, "birth " + (d + 1), ExpRoutePoint(1f + d, 1f, .2f), .33f, c, .035f);
            if (d <= 12) ExpLabel(g, $"C<sub>{d}</sub>", pts[^1] + Vector3.back * .5f, labelSize * .6f, c);
        }

        // a far curve (d ≥ 122: a small Euler prime times a sister prime) winds fifty times before it straightens;
        // only the stretch through its crossing is drawn, u ± 3
        if (farSegments)
        {
            foreach ((int d, int uc) in farCurves)
            {
                int steps = 6 * RouteSteps;
                Vector3[] pts = new Vector3[steps + 1];
                for (int i = 0; i <= steps; i++)
                {
                    float u = uc - 3f + i / (float)RouteSteps;
                    pts[i] = ExpRoutePoint(u * (u + d), u, .12f);
                }

                Color c = CurveColor(d);
                ExpLine(g, "C_" + d + " segment", .05f, new Color(c.r, c.g, c.b, .7f), Factory.NoGlow, pts);
            }
        }

        // the sister line D(j) = (2j)² + 163 = N(2j + √−163): drawn as the route n = k² + 163, dotted at even k
        if (sister)
        {
            int kMax = Mathf.FloorToInt(Mathf.Sqrt(nMax - 163f));
            Vector3[] pts = new Vector3[kMax * RouteSteps + 1];
            for (int i = 0; i < pts.Length; i++)
            {
                float k = i / (float)RouteSteps;
                pts[i] = ExpRoutePoint(k * k + 163f, k, .18f);
            }

            ExpLine(g, "sister line", .08f, SisterColor, Factory.SoftGlowIntensity, pts);
            for (int j = 0; 2 * j <= kMax; j++)
            {
                int n = Sister(j);
                bool p = SmallestDivisorUpToSqrt(n) == 1;
                ExpDot(g, "sister " + n, ExpRoutePoint(n, 2 * j, .22f), p ? .22f : .3f, p ? SisterColor : SquareColor);
            }

            ExpLabel(g, "D(j) = (2j)² + 163   the sister line", ExpRoutePoint(kMax * kMax + 163f, kMax, .2f) + Vector3.up * .4f * labelSize, labelSize * .8f, SisterColor);
        }

        // Euler's line, extended
        Transform e = ExpGroup("A euler extension").transform;
        int ePts = eulerKMax * RouteSteps + 1;
        Vector3[] ePath = new Vector3[ePts];
        for (int i = 0; i < ePts; i++)
        {
            float k = i / (float)RouteSteps;
            ePath[i] = ExpRoutePoint(k * k + k + 41f, k, .18f);
        }

        ExpLine(e, "euler extended", .09f, EulerColor, Factory.GlowIntensity, ePath);
        for (int k = 0; k <= eulerKMax; k++)
        {
            int n = Euler(k);
            if (SmallestDivisorUpToSqrt(n) == 1) ExpDot(e, "euler prime " + n, ExpRoutePoint(n, k, .22f), k > 39 ? .3f : .2f, Factory.Palette.Zero);
        }

        foreach ((int k, int u, int v) in composites)
        {
            int n = u * v, d = v - u, j = d / 2;
            Vector3 p = ExpRoutePoint(n, k, .22f);
            ExpDot(e, "euler composite " + n, p, .4f, SquareColor);
            ExpRing(e, "crossing ring " + n, p, .8f, CurveColor(d), .06f, Factory.GlowIntensity);
            if (!chords) continue;

            // family I: k = 40 + j², E(k) = E(j−1)·E(j).  family II: E(k) = E(i)·D(i) or E(i)·D(i+1)
            int jj = Mathf.RoundToInt(Mathf.Sqrt(Mathf.Max(0, k - 40)));
            bool one = k >= 40 && jj * jj == k - 40 && (long)Euler(jj - 1) * Euler(jj) == n;
            int i2 = -1, j2 = -1;
            if (!one)
            {
                for (int i = 0; i <= 40 && i2 < 0; i++)
                {
                    if (n % Euler(i) != 0) continue;
                    int q = n / Euler(i);
                    for (int jd = 0; jd <= 60; jd++) if (Sister(jd) == q) { i2 = i; j2 = jd; break; }
                }
            }

            Color fc = one ? FamilyOneColor : i2 >= 0 ? FamilyTwoColor : Color.white;
            Vector3 fa, fb;
            string text;
            if (one)
            {
                fa = ExpRoutePoint(Euler(jj - 1), Mathf.Max(0, jj - 1), .22f);
                fb = ExpRoutePoint(Euler(jj), jj, .22f);
                text = $"k = 40 + {jj}²   E({jj - 1}) · E({jj}) = {u} · {v}   on C<sub>{d}</sub>";
            }
            else if (i2 >= 0)
            {
                fa = ExpRoutePoint(Euler(i2), i2, .22f);
                fb = ExpRoutePoint(Sister(j2), 2 * j2, .22f);
                string law = j2 == i2 ? $"k = 2·{i2}² + {i2} + 81" : $"k = 2·{i2}² + 3·{i2} + 82";
                text = $"{law}   E({i2}) · D({j2}) = {u} · {v}   on C<sub>{d}</sub>";
            }
            else
            {
                fa = fb = p;
                text = $"k = {k}   {u} · {v}   on C<sub>{d}</sub>";
            }

            if (fa != p) ExpLine(e, "chord a " + n, .03f, new Color(fc.r, fc.g, fc.b, .6f), Factory.NoGlow, new[] { fa, p });
            if (fb != p) ExpLine(e, "chord b " + n, .03f, new Color(fc.r, fc.g, fc.b, .6f), Factory.NoGlow, new[] { fb, p });
            ExpLabel(e, text, p + Vector3.back * .6f + Vector3.up * (.45f * labelSize), labelSize * .8f, fc);
        }
    }

    // ------------------------------------------------------------------------------------------------
    // A2. the zeta wave along Euler's line
    // ------------------------------------------------------------------------------------------------

    /// <summary>−Σ_{z&lt;zeros} cos(γ_z · log n) along n(k) = k² + k + 41, as a ribbon above the line.</summary>
    public void ExpZetaWaveAlongEuler(int kMax, int zeros, float amp, float labelSize)
    {
        Transform g = ExpGroup("A2 zeta wave along euler").transform;
        double[] zs = Main.Instance.zetaZeroValues;
        zeros = Mathf.Min(zeros, zs.Length);
        const int per = 24;
        int count = kMax * per + 1;
        double[] w = new double[count];
        double max = 1e-9;
        for (int i = 0; i < count; i++)
        {
            double k = i / (double)per;
            double logN = System.Math.Log(k * k + k + 41.0);
            double s = 0;
            for (int z = 0; z < zeros; z++) s -= System.Math.Cos(zs[z] * logN);
            w[i] = s;
            if (System.Math.Abs(s) > max) max = System.Math.Abs(s);
        }

        Vector3[] pts = new Vector3[count], zero = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            float k = i / (float)per;
            float n = k * k + k + 41f;
            pts[i] = ExpRoutePoint(n, k, .25f + amp * (float)(w[i] / max));
            zero[i] = ExpRoutePoint(n, k, .25f);
        }

        ExpLine(g, "zeta wave on euler", .08f, Factory.Palette.Cosine, Factory.GlowIntensity, pts);
        ExpLine(g, "ribbon zero line", .025f, new Color(.5f, .5f, .6f), Factory.NoGlow, zero);
        // the wall between ribbon and line, so the sign reads at a glance
        List<Vector3> verts = new();
        List<Color> cols = new();
        List<int> tris = new();
        for (int i = 0; i < count; i++)
        {
            verts.Add(zero[i]); verts.Add(pts[i]);
            Color c = w[i] >= 0 ? new Color(.25f, .9f, .85f, .35f) : new Color(1f, .3f, .3f, .35f);
            cols.Add(c); cols.Add(c);
            if (i == 0) continue;
            int v = verts.Count - 4;
            tris.Add(v); tris.Add(v + 1); tris.Add(v + 2); tris.Add(v + 2); tris.Add(v + 1); tris.Add(v + 3);
            tris.Add(v); tris.Add(v + 2); tris.Add(v + 1); tris.Add(v + 2); tris.Add(v + 3); tris.Add(v + 1);
        }

        Mesh mesh = new() { name = "wave wall", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(verts); mesh.SetColors(cols); mesh.SetTriangles(tris, 0); mesh.RecalculateBounds();
        GameObject go = new("wave wall");
        go.transform.SetParent(g, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = Factory.mainMat;
        Factory.SetIntensity(mr, Factory.NoGlow);
        ExpLabel(g, $"−Σ cos(γ · log n) over {zeros:N0} zeros, along k² + k + 41\nup at every prime value, down at every crossing", pts[count / 2] + Vector3.up * 1.2f * labelSize, labelSize * .8f, Factory.Palette.Cosine);
    }

    // ------------------------------------------------------------------------------------------------
    // C. composite depth bars and Voronoi's wave
    // ------------------------------------------------------------------------------------------------

    /// <summary>A bar at every n ≤ nMax of height ∝ (depth − 1): primes are flat; the number of cancellation curves through n.</summary>
    public void ExpDepthBars(int nMax, float unit)
    {
        Transform g = ExpGroup("C depth bars").transform;
        List<Vector3> verts = new();
        List<Color> cols = new();
        List<int> tris = new();
        for (int n = 2; n <= nMax; n++)
        {
            int depth = Depth(n);
            if (depth <= 1) continue;
            float h = unit * (depth - 1);
            Vector3 b = OnSpiral(n, 0f, .05f), top = OnSpiral(n, 0f, .05f + h);
            Vector3 normal = SurfaceNormalAt(n);
            Vector3 s1 = Vector3.Cross(normal, Vector3.right).normalized * .07f;
            Vector3 s2 = Vector3.Cross(normal, s1).normalized * .07f;
            Color c = depth == 2 ? new Color(.45f, .6f, 1f) : depth == 3 ? new Color(.7f, .5f, 1f) : depth == 4 ? new Color(1f, .55f, .3f) : new Color(1f, .3f, .3f);
            foreach (Vector3 side in new[] { s1, s2 })
            {
                int v = verts.Count;
                verts.Add(b - side); verts.Add(b + side); verts.Add(top - side); verts.Add(top + side);
                cols.Add(c); cols.Add(c); cols.Add(c); cols.Add(c);
                tris.Add(v); tris.Add(v + 2); tris.Add(v + 1); tris.Add(v + 1); tris.Add(v + 2); tris.Add(v + 3);
                tris.Add(v); tris.Add(v + 1); tris.Add(v + 2); tris.Add(v + 1); tris.Add(v + 3); tris.Add(v + 2);
            }
        }

        Mesh mesh = new() { name = "depth bars", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(verts);
        mesh.SetColors(cols);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        GameObject go = new("depth bars mesh");
        go.transform.SetParent(g, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = Factory.mainMat;
        Factory.SetIntensity(mr, Factory.SoftGlowIntensity);
    }

    static int DivisorCount(int n)
    {
        int c = 0;
        for (int u = 1; u * u <= n; u++) if (n % u == 0) c += u * u == n ? 1 : 2;
        return c;
    }

    /// <summary>
    /// Along the spiral (x from 1.5 to xMax): the divisor staircase's error term Δ(x) = Σ_{n≤x} d(n) − x log x − (2γ−1)x
    /// (white), Voronoi's wave sum (x^¼/π√2) Σ_{n≤N} d(n) n^−¾ cos(4π√(nx) − π/4) (cyan), and its first harmonic alone
    /// (orange): the Sacks clock 2π√x is the clock of this explicit formula; the saddle z ∝ x·cos(4π√x) is its first term.
    /// </summary>
    public void ExpVoronoi(int terms, float xMin, float xMax, float amp, bool firstOnly, bool wire)
    {
        Transform g = ExpGroup("C voronoi").transform;
        if (wire)
        {
            Color wc = new(.35f, .4f, .55f, .55f);
            for (float r = 4f; r <= R * Mathf.Sqrt(xMax) + 2f; r += 4f) ExpSurfaceRing(g, "wire ring " + r, r * r / (R * R), wc, .03f, 0f);
            for (int a = 0; a < 12; a++)
            {
                float ang = a / 12f * Mathf.PI * 2f, rMax = R * Mathf.Sqrt(xMax) + 2f;
                Vector3[] spoke = new Vector3[40];
                for (int i = 0; i < 40; i++)
                {
                    float r = rMax * i / 39f;
                    float x = r * Mathf.Cos(ang), y = r * Mathf.Sin(ang);
                    spoke[i] = origin + new Vector3(x, y, bend * SaddleK * (x * x - y * y));
                }

                ExpLine(g, "wire spoke " + a, .03f, wc, Factory.NoGlow, spoke);
            }
        }

        int[] dn = new int[terms + 1];
        for (int n = 1; n <= terms; n++) dn[n] = DivisorCount(n);
        int xMaxInt = Mathf.CeilToInt(xMax);
        double[] D = new double[xMaxInt + 2];
        for (int n = 1; n <= xMaxInt + 1; n++) D[n] = D[n - 1] + DivisorCount(n);
        const double euler = 0.5772156649015329;
        const int pts = 3000;
        double[] exact = new double[pts], series = new double[pts], first = new double[pts];
        double maxE = 1e-9;
        float[] xs = new float[pts];
        for (int i = 0; i < pts; i++)
        {
            double s = System.Math.Sqrt(xMin) + (System.Math.Sqrt(xMax) - System.Math.Sqrt(xMin)) * i / (pts - 1.0);
            double x = s * s;
            xs[i] = (float)x;
            exact[i] = D[(int)x] - x * System.Math.Log(x) - (2 * euler - 1) * x;
            double sum = 0;
            for (int n = 1; n <= terms; n++) sum += dn[n] * System.Math.Pow(n, -0.75) * System.Math.Cos(4 * System.Math.PI * System.Math.Sqrt(n * x) - System.Math.PI / 4);
            series[i] = System.Math.Pow(x, 0.25) / (System.Math.PI * System.Math.Sqrt(2)) * sum;
            first[i] = System.Math.Pow(x, 0.25) / (System.Math.PI * System.Math.Sqrt(2)) * System.Math.Cos(4 * System.Math.PI * System.Math.Sqrt(x) - System.Math.PI / 4);
            maxE = System.Math.Max(maxE, System.Math.Abs(exact[i]));
        }

        float scale = (float)(amp / maxE);
        Vector3[] pE = new Vector3[pts], pS = new Vector3[pts], pF = new Vector3[pts], pZ = new Vector3[pts];
        for (int i = 0; i < pts; i++)
        {
            pE[i] = OnSpiral(xs[i], 1.2f, (float)exact[i] * scale);
            pS[i] = OnSpiral(xs[i], 1.2f, (float)series[i] * scale);
            pF[i] = OnSpiral(xs[i], 1.2f, (float)first[i] * scale);
            pZ[i] = OnSpiral(xs[i], 1.2f, 0f);
        }

        ExpLine(g, "zero line", .02f, new Color(.45f, .45f, .55f), Factory.NoGlow, pZ);
        ExpLine(g, "divisor staircase error", firstOnly ? .05f : .07f, Color.white, Factory.SoftGlowIntensity, pE);
        if (!firstOnly) ExpLine(g, "voronoi wave", .06f, Factory.Palette.Cosine, Factory.GlowIntensity, pS);
        ExpLine(g, "voronoi first harmonic", firstOnly ? .09f : .05f, new Color(1f, .6f, .25f), Factory.GlowIntensity, pF);
    }

    // ------------------------------------------------------------------------------------------------
    // D. the zeros' own Sacks spiral
    // ------------------------------------------------------------------------------------------------

    /// <summary>Radius R√G (so zero j sits about as far out as the integer j), angle 2πG, on the saddle surface.</summary>
    Vector3 GramPoint(double G)
    {
        float r = R * Mathf.Sqrt((float)G);
        float a = (float)(2.0 * System.Math.PI * (G - System.Math.Floor(G)));
        float x = r * Mathf.Cos(a), y = r * Mathf.Sin(a);
        return origin + new Vector3(x, y, bend * SaddleK * (x * x - y * y));
    }

    /// <summary>Zero j at radius √G and angle 2πG with G = θ(γ_j)/π + 1: a Gram point is a whole G, like a square is a whole √n.</summary>
    public void ExpGramSpiral(int count, float altitude)
    {
        Transform g = ExpGroup("D gram spiral").transform;
        double[] zs = Main.Instance.zetaZeroValues;
        count = Mathf.Min(count, zs.Length);
        List<Vector3> pts = new();
        List<Color> cols = new();
        List<float> sizes = new();
        for (int j = 0; j < count; j++)
        {
            double G = ZetaMath.RiemannSiegelTheta(zs[j]) / System.Math.PI + 1.0;
            if (G > Count) break;
            float frac = (float)(G - System.Math.Floor(G));
            float mid = 1f - Mathf.Abs(frac - .5f) * 2f; // 1 mid-interval, 0 at a Gram point
            pts.Add(GramPoint(G) + Vector3.back * altitude);
            cols.Add(Color.Lerp(SquareColor, Factory.Palette.Cosine, mid));
            sizes.Add(.2f);
        }

        ExpPointCloud(g, "zeros", pts, cols, sizes, Factory.SoftGlowIntensity);
        // the Gram ray (whole G ↔ angle 0): the analogue of the squares ray
        Vector3[] ray = new Vector3[60];
        for (int i = 0; i < 60; i++)
        {
            float x = R * Mathf.Lerp(1f, Mathf.Sqrt(Count), i / 59f);
            ray[i] = origin + new Vector3(x, 0f, bend * SaddleK * x * x) + Vector3.back * altitude;
        }

        ExpLine(g, "gram ray", .05f, SquareColor, Factory.SoftGlowIntensity, ray);
    }

    // ------------------------------------------------------------------------------------------------
    // E. anatomy of the chain
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    /// The partial sums Σ_{n≤N} n^(−½−it) drawn flat in the XY plane inside a portrait box around <paramref name="center"/>:
    /// knots (magenta) where the turn per step t/n ≈ odd multiple of π, spokes (cyan) where it ≈ multiple of 2π,
    /// the tail spiral beyond n &gt; t/2π winding onto ζ(½ + it) (gold), the origin (white cross).
    /// </summary>
    public void ExpChainPanel(double t, int N, Vector3 center, float halfWidth, float halfHeight, string title, float labelSize)
    {
        Transform g = ExpGroup("E chain " + title).transform;
        Complex s = new(.5, t);
        Complex[] sums = new Complex[N + 1];
        Complex sum = Complex.Zero;
        double minRe = 0, maxRe = 0, minIm = 0, maxIm = 0;
        for (int n = 1; n <= N; n++)
        {
            sum += ZetaMath.Term(n, s);
            sums[n] = sum;
            minRe = System.Math.Min(minRe, sum.Re); maxRe = System.Math.Max(maxRe, sum.Re);
            minIm = System.Math.Min(minIm, sum.Im); maxIm = System.Math.Max(maxIm, sum.Im);
        }

        float scale = Mathf.Min(halfWidth * 2f / (float)(maxRe - minRe + 1e-6), halfHeight * 2f / (float)(maxIm - minIm + 1e-6));
        Vector3 shift = new((float)(-(minRe + maxRe) * .5) * scale, (float)(-(minIm + maxIm) * .5) * scale, 0f);
        Vector3 o = center + shift; // the complex origin
        Vector3 P(Complex z) => o + new Vector3((float)z.Re * scale, (float)z.Im * scale, 0f);
        Vector3[] pts = new Vector3[N + 1];
        pts[0] = o;
        for (int n = 1; n <= N; n++) pts[n] = P(sums[n]);

        LineRenderer chain = ExpLine(g, "chain", .07f, Factory.Palette.Zeta, Factory.SoftGlowIntensity, pts);
        chain.startColor = Factory.Palette.Zeta;
        chain.endColor = Factory.Palette.Tail;
        chain.widthCurve = new AnimationCurve(new Keyframe(0f, .09f), new Keyframe(1f, .035f));
        ExpLine(g, "origin x", .04f, Color.white, Factory.GlowIntensity, new[] { o + Vector3.left * .6f, o + Vector3.right * .6f });
        ExpLine(g, "origin y", .04f, Color.white, Factory.GlowIntensity, new[] { o + Vector3.down * .6f, o + Vector3.up * .6f });

        // ζ itself from the Euler–Maclaurin closure: the centre of the tail spiral
        Complex zeta = ZetaMath.Decompose(s, ZetaMath.TermCount(t)).Total;
        Vector3 zp = P(zeta);
        ExpDot(g, "zeta", zp, .3f, Factory.Palette.Zero);
        ExpLabel(g, $"ζ(½ + {t:0.00}i)   |ζ| = {ComplexMath.Abs(zeta):0.000}", zp + Vector3.up * 1.0f, labelSize, Factory.Palette.Zero);

        // knots and spokes: the mirror images of the half-integers and the integers under n ↔ t/(2πn)
        Color knotColor = new(1f, .4f, .9f);
        for (int k = 0; k < 8; k++)
        {
            double nKnot = t / ((2 * k + 1) * System.Math.PI);
            if (nKnot >= 2 && nKnot <= N)
            {
                int n = (int)System.Math.Round(nKnot);
                ExpDot(g, "knot " + k, pts[n], .22f, knotColor);
                ExpLabel(g, $"knot  n = t/{2 * k + 1}π = {nKnot:0.0}  ↔  {k}½", pts[n] + Vector3.up * .6f, labelSize * .75f, knotColor);
            }

            double nSpoke = t / (2 * (k + 1) * System.Math.PI);
            if (nSpoke >= 2 && nSpoke <= N)
            {
                int n = (int)System.Math.Round(nSpoke);
                ExpDot(g, "spoke " + k, pts[n], .2f, Factory.Palette.Cosine);
                ExpLabel(g, $"spoke  n = t/{2 * (k + 1)}π = {nSpoke:0.0}  ↔  {k + 1}", pts[n] + Vector3.down * .7f, labelSize * .75f, Factory.Palette.Cosine);
            }
        }

        ExpLabel(g, title, center + Vector3.up * (halfHeight + 1.2f * labelSize), labelSize, Color.white);
    }

    // ------------------------------------------------------------------------------------------------
    // F. one zero's crests on the spiral
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    /// The crests of cos(γ · log n) are at n_k = e^{2πk/γ}: on the spiral their winding angles φ_k = 2π e^{πk/γ} are a
    /// geometric progression, so the crests are equally spaced in log φ. They make m crests per turn at n = (γ/πm)²
    /// (rings): the inverse-square ladder of the moiré arms. The wave itself rides the spiral as a ribbon.
    /// </summary>
    public void ExpCrestComb(int zeroIndex, int arms, float amp, Color color, float labelSize)
    {
        double gamma = Main.Instance.zetaZeroValues[zeroIndex];
        Transform g = ExpGroup("F crests of zero " + (zeroIndex + 1)).transform;
        List<Vector3> crests = new();
        for (int k = 0; ; k++)
        {
            double n = System.Math.Exp(2 * System.Math.PI * k / gamma);
            if (n > Count) break;
            crests.Add(OnSpiral((float)n, .3f, 0f));
        }

        for (int i = 0; i < crests.Count; i++) ExpDot(g, "crest " + i, crests[i], .26f, color);
        ExpLine(g, "crest chain", .03f, new Color(color.r, color.g, color.b, .7f), Factory.NoGlow, crests.ToArray());

        // the wave as a ribbon along the spiral
        const int samples = 6000;
        Vector3[] pts = new Vector3[samples];
        for (int i = 0; i < samples; i++)
        {
            float sq = Mathf.Lerp(1f, Mathf.Sqrt(Count), i / (samples - 1f));
            float n = sq * sq;
            pts[i] = OnSpiral(n, .3f, amp * (float)System.Math.Cos(gamma * System.Math.Log(n)));
        }

        ExpLine(g, "wave ribbon", .035f, new Color(color.r, color.g, color.b, .8f), Factory.NoGlow, pts);

        for (int m = 1; m <= arms; m++)
        {
            float n = (float)((gamma / (System.Math.PI * m)) * (gamma / (System.Math.PI * m)));
            if (n > Count || n < 1.5f) continue;
            ExpSurfaceRing(g, "arms " + m, n, Color.white, .035f, .25f);
            Vector3 at = SpiralPos(n, bend) + SurfaceNormalAt(n) * .5f;
            ExpLabel(g, $"{m} crest{(m > 1 ? "s" : "")} per turn   n = (γ/{m}π)² = {n:0.#}", at + Vector3.up * .5f * labelSize, labelSize * .7f, Color.white);
        }

        ExpLabel(g, $"zero #{zeroIndex + 1}   γ = {gamma:0.000}   crests at n = e<sup>2πk/γ</sup>: {crests.Count} inside\nwinding angle of crest k is 2π·e<sup>πk/γ</sup>: each crest's angle is the last one's × {System.Math.Exp(System.Math.PI / gamma):0.000}", origin + new Vector3(0f, -Extent - 1.2f * labelSize, 0f) + Vector3.back * 2f, labelSize, color);
    }

    // ------------------------------------------------------------------------------------------------
    // G. the truncated explicit formula on the number field
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    /// A disc per entry of <paramref name="zeroCounts"/>: every number n ≤ nMax coloured by −Σ_{z≤K} cos(γ_z log n)
    /// (gold up, blue down, each disc scaled to its own extreme), lifted by altitude index × step along the surface normal.
    /// </summary>
    public void ExpHeatStack(int[] zeroCounts, int nMax, float step, float labelSize, bool labelsAtSide)
    {
        Transform g = ExpGroup("G heat stack").transform;
        double[] zs = Main.Instance.zetaZeroValues;
        double[] logs = new double[nMax + 1];
        for (int n = 1; n <= nMax; n++) logs[n] = System.Math.Log(n);
        double[] acc = new double[nMax + 1];
        int done = 0;
        for (int level = 0; level < zeroCounts.Length; level++)
        {
            int K = Mathf.Min(zeroCounts[level], zs.Length);
            for (int z = done; z < K; z++)
            {
                double gm = zs[z];
                for (int n = 2; n <= nMax; n++) acc[n] -= System.Math.Cos(gm * logs[n]);
            }

            done = K;
            double max = 1e-9;
            for (int n = 2; n <= nMax; n++) max = System.Math.Max(max, System.Math.Abs(acc[n]));
            List<Vector3> pts = new();
            List<Color> cols = new();
            List<float> sizes = new();
            float altitude = level * step;
            for (int n = 2; n <= nMax; n++)
            {
                float v = (float)(acc[n] / max);
                Color c = v >= 0 ? Color.Lerp(new Color(.12f, .12f, .16f), Factory.Palette.Zero, v) : Color.Lerp(new Color(.12f, .12f, .16f), new Color(.2f, .4f, 1f), -v);
                pts.Add(OnSpiral(n, altitude, 0f));
                cols.Add(c);
                sizes.Add(.09f + .2f * Mathf.Abs(v));
            }

            ExpPointCloud(g, "level " + K, pts, cols, sizes, Factory.SoftGlowIntensity);
            Vector3 at = labelsAtSide
                ? origin + new Vector3(Extent + .4f * labelSize, 0f, 0f) + Vector3.back * altitude
                : origin + new Vector3(0f, -Extent - 1.2f * labelSize, 0f) + Vector3.back * (altitude + 2f);
            ExpLabel(g, $"{K:N0} zero{(K > 1 ? "s" : "")}", at, labelSize, Color.white, labelsAtSide ? TextAlignmentOptions.Left : TextAlignmentOptions.Center);
        }
    }

    // ------------------------------------------------------------------------------------------------
    // H. cross sections of the saddle, at any bend, against the trail and the product curves
    // ------------------------------------------------------------------------------------------------

    /// <summary>Set the bend directly (0 flat … 1 the saddle … beyond) and re-lay the surface.</summary>
    public void ExpSetBend(float b)
    {
        bend = b;
        Flat = b < 1e-3f;
        Layout();
        foreach (GameObject d in dots) d.SetActive(false); // the layout pass re-enables the spiral's own dots
    }

    public float ExpBend => bend;

    static readonly Color RidgeColor = new(1f, .45f, .3f);
    static readonly Color ValleyColor = new(.35f, .65f, 1f);
    static readonly Color LevelColor = new(.85f, .85f, .95f);

    float SurfaceZ(float x, float y) => bend * SaddleK * (x * x - y * y);

    /// <summary>A plane x = c ('x'), y = c ('y') or z = c ('z') through the saddle, drawn as a wire grid (the glow material does not blend).</summary>
    public void ExpSectionPlane(char axis, float c, float halfSize, Color color, float spacing = 10f)
    {
        Transform g = ExpGroup("H plane " + axis + " " + c).transform;
        Vector3 Map(float a, float b) => axis == 'x' ? origin + new Vector3(c, a, b) : axis == 'y' ? origin + new Vector3(a, c, b) : origin + new Vector3(a, b, c);
        Color dim = new(color.r * .45f, color.g * .45f, color.b * .45f);
        for (float a = -halfSize; a <= halfSize + .01f; a += spacing)
        {
            ExpLine(g, "grid a " + a, .025f, dim, Factory.NoGlow, new[] { Map(a, -halfSize), Map(a, halfSize) });
            ExpLine(g, "grid b " + a, .025f, dim, Factory.NoGlow, new[] { Map(-halfSize, a), Map(halfSize, a) });
        }

        ExpLine(g, "rim", .05f, color, Factory.SoftGlowIntensity, new[] { Map(-halfSize, -halfSize), Map(halfSize, -halfSize), Map(halfSize, halfSize), Map(-halfSize, halfSize) }, true);
    }

    /// <summary>The curve in which the plane cuts the surface: a parabola for x or y, a hyperbola (or the two diagonals) for z.</summary>
    public void ExpSectionCurve(char axis, float c, Color color, float width = .08f)
    {
        Transform g = ExpGroup("H section " + axis + " " + c).transform;
        float L = Extent + 4f;
        if (axis == 'x' || axis == 'y')
        {
            float span = Mathf.Sqrt(Mathf.Max(0f, L * L - c * c));
            Vector3[] pts = new Vector3[121];
            for (int i = 0; i <= 120; i++)
            {
                float s = Mathf.Lerp(-span, span, i / 120f);
                float x = axis == 'x' ? c : s, y = axis == 'x' ? s : c;
                pts[i] = origin + new Vector3(x, y, SurfaceZ(x, y)) + Vector3.back * .25f;
            }

            ExpLine(g, "section", width, color, Factory.GlowIntensity, pts);
            return;
        }

        float k = bend * SaddleK;
        if (Mathf.Abs(k) < 1e-6f) return; // flat: the plane z = c misses the surface (or contains it)
        float a2 = c / k; // x² − y² = a2
        if (Mathf.Abs(a2) < 1e-3f)
        {
            ExpLine(g, "diagonal +", width, color, Factory.GlowIntensity, new[] { origin + new Vector3(-L, -L, 0f) + Vector3.back * .25f, origin + new Vector3(L, L, 0f) + Vector3.back * .25f });
            ExpLine(g, "diagonal −", width, color, Factory.GlowIntensity, new[] { origin + new Vector3(-L, L, 0f) + Vector3.back * .25f, origin + new Vector3(L, -L, 0f) + Vector3.back * .25f });
            return;
        }

        float a = Mathf.Sqrt(Mathf.Abs(a2));
        if (a > L) return;
        float S = Mathf.Log(L / a + Mathf.Sqrt(L * L / (a * a) + 1f)); // cosh S ≈ L / a
        for (int branch = -1; branch <= 1; branch += 2)
        {
            Vector3[] pts = new Vector3[121];
            for (int i = 0; i <= 120; i++)
            {
                float t = Mathf.Lerp(-S, S, i / 120f);
                float ch = (float)System.Math.Cosh(t), sh = (float)System.Math.Sinh(t);
                float x = a2 > 0 ? branch * a * ch : a * sh, y = a2 > 0 ? a * sh : branch * a * ch;
                pts[i] = origin + new Vector3(x, y, c) + Vector3.back * .25f;
            }

            ExpLine(g, "hyperbola " + branch, width, color, Factory.GlowIntensity, pts);
        }
    }

    /// <summary>Every number within <paramref name="tol"/> of the plane, lit; the rest are left to the backdrop. Returns the count.</summary>
    public int ExpSectionNumbers(char axis, float c, float tol, Color color, int nMax, bool label, float labelSize)
    {
        Transform g = ExpGroup("H numbers " + axis + " " + c).transform;
        int count = 0;
        for (int n = 2; n <= nMax; n++)
        {
            Vector3 p = SpiralPos(n, bend) - origin;
            float dist = axis == 'x' ? Mathf.Abs(p.x - c) : axis == 'y' ? Mathf.Abs(p.y - c) : Mathf.Abs(p.z - c);
            if (dist > tol) continue;
            count++;
            float w = 1f - dist / tol;
            bool prime = IsPrime(n);
            Color col = prime ? Factory.Palette.Zero : color;
            ExpDot(g, "in plane " + n, SpiralPos(n, bend) + Vector3.back * .3f, .2f + .25f * w, col, prime ? Factory.GlowIntensity : Factory.SoftGlowIntensity);
            if (label) ExpLabel(g, n.ToString(), SpiralPos(n, bend) + Vector3.back * .6f + Vector3.up * .45f * labelSize, labelSize * .7f, col);
        }

        return count;
    }

    /// <summary>
    /// The product curves n = u(q²u + d), u = 1, 2, …: for q = 1 the cancellation curves (they climb the ridge, both rays);
    /// q = 2 with odd d runs down the valley (the ±y axis); q = 4 with odd d hugs the zero level (the diagonals).
    /// </summary>
    public void ExpProductFamily(int q, int[] ds, int nMax, Color color, float width, float labelSize)
    {
        Transform g = ExpGroup($"H products {q}²:1").transform;
        foreach (int d in ds)
        {
            // u(q²u + d) ≤ nMax
            float uMax = (-d + Mathf.Sqrt(d * d + 4f * q * q * nMax)) / (2f * q * q);
            if (uMax < 1.5f) continue;
            int steps = Mathf.FloorToInt((uMax - 1f) * RouteSteps);
            Vector3[] pts = new Vector3[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                float u = 1f + i / (float)RouteSteps;
                pts[i] = ExpRoutePoint(u * (q * q * u + d), q * u, .12f);
            }

            ExpLine(g, $"u({q * q}u+{d})", width, color, Factory.SoftGlowIntensity, pts);
            ExpRing(g, "birth " + (q * q + d), ExpRoutePoint(q * q + d, q, .2f), .33f, color, .035f);
            if (labelSize > 0f) ExpLabel(g, q == 1 ? $"u(u + {d})" : $"u({q * q}u + {d})", pts[^1] + Vector3.back * .5f + Vector3.up * .4f * labelSize, labelSize * .7f, color);
        }
    }

    /// <summary>
    /// The zeta trail as the tour draws it: ζ(½ + it) in the plane (× ZetaXY) rising by t × ZetaT along −z, from the
    /// Euler–Maclaurin closure. Zeros gold; with <paramref name="gram"/> the Gram points (ζ real, θ = mπ) red; with
    /// <paramref name="halfGram"/> the half-Gram points (ζ imaginary, θ = (m + ½)π) cyan.
    /// </summary>
    public void ExpTrail(float tMax, bool gram, bool halfGram, float labelSize)
    {
        Transform g = ExpGroup("H trail").transform;
        Vector3 At(double t)
        {
            Complex z = t < 1e-6 ? new Complex(-1.4603545088095868, 0) : ZetaMath.Decompose(new Complex(.5, t), ZetaMath.TermCount(t)).Total;
            return origin + new Vector3((float)z.Re * ZetaXY, (float)z.Im * ZetaXY, 0f) + Vector3.back * ((float)t * ZetaT);
        }

        int count = Mathf.CeilToInt(tMax / .05f) + 1;
        Vector3[] pts = new Vector3[count];
        for (int i = 0; i < count; i++) pts[i] = At(i * .05);
        ExpLine(g, "trail", .07f, Factory.Palette.Zeta, Factory.SoftGlowIntensity, pts);
        ExpLine(g, "axis", .03f, new Color(.6f, .6f, .7f, .6f), Factory.NoGlow, new[] { origin, origin + Vector3.back * (tMax * ZetaT) });

        foreach (double z in Main.Instance.zetaZeroValues)
        {
            if (z > tMax) break;
            ExpDot(g, "zero " + z.ToString("0.0"), At(z), .3f, Factory.Palette.Zero);
        }

        if (!gram && !halfGram) return;
        // θ is monotone above t ≈ 6.3: bracket each level on a grid, then bisect
        double Theta(double t) => ZetaMath.RiemannSiegelTheta(t);
        for (int m = -1; ; m++)
        {
            foreach (double offset in new[] { 0.0, 0.5 })
            {
                if ((offset == 0.0 && !gram) || (offset == 0.5 && !halfGram)) continue;
                double target = (m + offset) * System.Math.PI;
                double lo = 7.0, hi = tMax;
                if (Theta(lo) > target || Theta(hi) < target) continue;
                for (int i = 0; i < 60; i++) { double mid = (lo + hi) * .5; if (Theta(mid) < target) lo = mid; else hi = mid; }
                double t = (lo + hi) * .5;
                bool whole = offset == 0.0;
                Color c = whole ? SquareColor : Factory.Palette.Cosine;
                ExpDot(g, (whole ? "gram " : "half-gram ") + m, At(t), .22f, c, Factory.SoftGlowIntensity);
            }

            if ((m + 1) * System.Math.PI > Theta(tMax)) break;
        }

        if (labelSize > 0f)
        {
            string text = gram && halfGram ? "red: Gram points (ζ real)   cyan: half-Gram points (ζ imaginary)   gold: zeros"
                : gram ? "red: the Gram points, where the trail pierces the plane y = 0 (ζ is real)   gold: zeros"
                : "cyan: the half-Gram points, where the trail pierces the plane x = 0 (ζ is imaginary)   gold: zeros";
            ExpLabel(g, text, origin + Vector3.back * (tMax * ZetaT + 1.2f * labelSize), labelSize, Color.white);
        }
    }

    // ------------------------------------------------------------------------------------------------
    // H4. the unrolled chart: the two clocks as the two axes
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    /// A flat panel with √n along x (the turn count) and the position within the turn along y (frac √n = angle / 2π).
    /// The saddle's height depends only on y (ridge at 0 and ½, valley at ¼ and ¾); a zero's wave depends only on x
    /// (vertical crest lines, log-spaced); the product curves u(u + d) are the 1/u hyperbolas, Euler's line one too.
    /// </summary>
    public void ExpUnrolledChart(Vector3 center, float width, float height, int nMax, int crestZero, int[] ds, float labelSize)
    {
        Transform g = ExpGroup("H4 unrolled chart").transform;
        float cMax = Mathf.Sqrt(nMax);
        Vector3 P(float c, float frac) => center + new Vector3((c - 1f) / (cMax - 1f) * width - width * .5f, frac * height - height * .5f, 0f);
        // frame
        ExpLine(g, "frame", .04f, new Color(.5f, .5f, .6f), Factory.NoGlow, new[] { P(1f, 0f), P(cMax, 0f), P(cMax, 1f), P(1f, 1f) }, true);
        // saddle: ridge lines at 0 and ½ (and 1), valley at ¼ and ¾
        foreach ((float f, Color col, string name) in new[] { (0f, RidgeColor, "ridge (squares ray)"), (.5f, RidgeColor, "ridge (pronic ray)"), (1f, RidgeColor, ""), (.25f, ValleyColor, "valley (+y axis)"), (.75f, ValleyColor, "valley (−y axis)") })
        {
            ExpLine(g, "stripe " + f, .05f, new Color(col.r, col.g, col.b, .8f), Factory.SoftGlowIntensity, new[] { P(1f, f), P(cMax, f) });
            if (name.Length > 0) ExpLabel(g, name, P(cMax, f) + Vector3.right * 2.2f * labelSize, labelSize * .7f, col, TextAlignmentOptions.Left);
        }

        // the numbers
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        for (int n = 2; n <= nMax; n++)
        {
            float c = Mathf.Sqrt(n);
            bool p = IsPrime(n), sq = IsSquare(n);
            pts.Add(P(c, c - Mathf.Floor(c)) + Vector3.back * .1f);
            cols.Add(p ? Factory.Palette.Zero : sq ? SquareColor : new Color(.35f, .35f, .45f));
            sizes.Add(p ? .16f : sq ? .14f : .08f);
        }

        ExpPointCloud(g, "numbers", pts, cols, sizes, Factory.SoftGlowIntensity);

        // product curves and Euler's line, broken where frac wraps
        void Curve(System.Func<float, float> nOfU, float u0, float u1, Color col, float w, string name)
        {
            List<Vector3> run = new();
            float lastFrac = -1f;
            int steps = Mathf.CeilToInt((u1 - u0) * 40f);
            for (int i = 0; i <= steps; i++)
            {
                float u = Mathf.Lerp(u0, u1, i / (float)steps);
                float c = Mathf.Sqrt(nOfU(u));
                if (c > cMax) break;
                float frac = c - Mathf.Floor(c);
                if (lastFrac >= 0f && Mathf.Abs(frac - lastFrac) > .5f)
                {
                    if (run.Count > 1) ExpLine(g, name, w, col, Factory.SoftGlowIntensity, run.ToArray());
                    run.Clear();
                }

                run.Add(P(c, frac) + Vector3.back * .05f);
                lastFrac = frac;
            }

            if (run.Count > 1) ExpLine(g, name, w, col, Factory.SoftGlowIntensity, run.ToArray());
        }

        foreach (int d in ds) Curve(u => u * (u + d), 1f, cMax, CurveColor(d), .04f, "C_" + d);
        Curve(k => k * k + k + 41f, 0f, cMax, EulerColor, .05f, "euler");
        Curve(k => k * k + 17f, 0f, cMax, FamilyColors[0], .04f, "k²+17");

        // one zero's crests: vertical lines at c_k = e^{πk/γ}
        double gamma = Main.Instance.zetaZeroValues[crestZero];
        Color zc = new(1f, .5f, .2f);
        for (int k = 0; ; k++)
        {
            float c = (float)System.Math.Exp(System.Math.PI * k / gamma);
            if (c > cMax) break;
            ExpLine(g, "crest " + k, .03f, new Color(zc.r, zc.g, zc.b, .6f), Factory.NoGlow, new[] { P(c, 0f) + Vector3.back * .02f, P(c, 1f) + Vector3.back * .02f });
        }

        // the master wave along the bottom: −Σ cos(2γ log c) over every zero, spikes at c = √p
        double[] zs = Main.Instance.zetaZeroValues;
        const int samples = 2400;
        Vector3[] wave = new Vector3[samples];
        double[] w2 = new double[samples];
        double max = 1e-9;
        for (int i = 0; i < samples; i++)
        {
            double c = 1.0 + (cMax - 1.0) * i / (samples - 1.0);
            double lg = 2.0 * System.Math.Log(c);
            double sum = 0;
            for (int z = 0; z < zs.Length; z++) sum -= System.Math.Cos(zs[z] * lg);
            w2[i] = sum;
            max = System.Math.Max(max, System.Math.Abs(sum));
        }

        for (int i = 0; i < samples; i++) wave[i] = P(1f + (cMax - 1f) * i / (samples - 1f), 0f) + Vector3.down * (height * .06f) + Vector3.up * (float)(w2[i] / max) * height * .05f;
        ExpLine(g, "master wave", .035f, Factory.Palette.Cosine, Factory.SoftGlowIntensity, wave);

        ExpLabel(g, "√n  →   (one unit = one turn of the Sacks spiral)", P((1f + cMax) * .5f, 0f) + Vector3.down * (height * .14f), labelSize, Color.white);
        ExpLabel(g, "angle / 2π  ↑", P(1f, .5f) + Vector3.left * 2.4f * labelSize, labelSize, Color.white);
        ExpLabel(g, $"orange verticals: the crests of zero #{crestZero + 1}, γ = {gamma:0.00}, at √n = e<sup>πk/γ</sup>   cyan: −Σ cos(γ log n) over 10 000 zeros, spiking at every √p", P((1f + cMax) * .5f, 1f) + Vector3.up * (height * .07f), labelSize * .8f, zc);
    }

    // ------------------------------------------------------------------------------------------------
    // H5. double cancellations: the Möbius sign on the number field, and its running sum
    // ------------------------------------------------------------------------------------------------

    static int Omega(int n)
    {
        int c = 0;
        for (int d = 2; d * d <= n; d++)
        {
            if (n % d != 0) continue;
            c++;
            while (n % d == 0) n /= d;
        }

        return n > 1 ? c + 1 : c;
    }

    static int Mobius(int n)
    {
        for (int d = 2; d * d <= n; d++) if (n % (d * d) == 0) return 0;
        return Omega(n) % 2 == 0 ? 1 : -1;
    }

    /// <summary>
    /// Inclusion–exclusion on the number field: a number on one prime's curve is struck (−1); on two distinct primes'
    /// curves it is struck twice and restored once (+1); on three, struck again (−1); a repeated prime factor zeroes it.
    /// That sign is μ(n), and Σ μ(n) n^−s = 1/ζ(s).
    /// </summary>
    public void ExpMobiusField(int nMax, float labelSize)
    {
        Transform g = ExpGroup("H5 mobius field").transform;
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        for (int n = 2; n <= nMax; n++)
        {
            int mu = Mobius(n), om = Omega(n);
            Color c = mu == 0 ? new Color(.16f, .16f, .2f) : mu > 0 ? Factory.Palette.Cosine : om == 1 ? Factory.Palette.Zero : new Color(1f, .4f, .9f);
            pts.Add(OnSpiral(n, 0f, .05f));
            cols.Add(c);
            sizes.Add(mu == 0 ? .08f : om == 1 ? .24f : .2f);
        }

        ExpPointCloud(g, "mobius", pts, cols, sizes, Factory.SoftGlowIntensity);
        if (labelSize > 0f) ExpLabel(g, "μ(n):  gold −1 primes   cyan +1 two distinct primes (struck twice, restored once)   magenta −1 three primes   dark 0 (a square factor)", origin + new Vector3(0f, -Extent - 1.2f * labelSize, 0f) + Vector3.back * 2f, labelSize * .8f, Color.white);
    }

    /// <summary>Mertens' function as a flat graph: x along the panel (linear), M(x) up, ±√x for scale, μ(n) as ticks along the bottom.</summary>
    public void ExpMertensPanel(Vector3 center, float width, float height, int nMax, float labelSize)
    {
        Transform g = ExpGroup("H5 mertens panel").transform;
        int[] M = new int[nMax + 1];
        for (int n = 1; n <= nMax; n++) M[n] = M[n - 1] + Mobius(n);
        float sq = Mathf.Sqrt(nMax);
        float yScale = height * .5f / sq;
        Vector3 P(float x, float y) => center + new Vector3(x / nMax * width - width * .5f, y * yScale, 0f);
        Vector3[] steps = new Vector3[2 * nMax];
        for (int n = 1; n <= nMax; n++)
        {
            steps[2 * (n - 1)] = P(n - 1, M[n]);
            steps[2 * (n - 1) + 1] = P(n, M[n]);
        }

        Vector3[] up = new Vector3[200], dn = new Vector3[200];
        for (int i = 0; i < 200; i++)
        {
            float x = nMax * i / 199f;
            up[i] = P(x, Mathf.Sqrt(x));
            dn[i] = P(x, -Mathf.Sqrt(x));
        }

        ExpLine(g, "axis", .03f, new Color(.5f, .5f, .6f), Factory.NoGlow, new[] { P(0f, 0f), P(nMax, 0f) });
        ExpLine(g, "+sqrt", .04f, new Color(.7f, .7f, .8f), Factory.NoGlow, up);
        ExpLine(g, "−sqrt", .04f, new Color(.7f, .7f, .8f), Factory.NoGlow, dn);
        ExpLine(g, "mertens", .09f, Factory.Palette.Cosine, Factory.GlowIntensity, steps);
        // μ ticks under the axis: gold −1 prime, magenta −1 otherwise, cyan +1
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        for (int n = 2; n <= nMax; n++)
        {
            int mu = Mobius(n);
            if (mu == 0) continue;
            pts.Add(P(n, 0f) + Vector3.down * (height * .5f + 1.5f) + Vector3.down * (mu > 0 ? 0f : 1.2f));
            cols.Add(mu > 0 ? Factory.Palette.Cosine : IsPrime(n) ? Factory.Palette.Zero : new Color(1f, .4f, .9f));
            sizes.Add(.12f);
        }

        ExpPointCloud(g, "mu ticks", pts, cols, sizes, Factory.SoftGlowIntensity);
        ExpLabel(g, $"M(x) = Σ μ(n) for x ≤ {nMax:N0}, between ±√x.   Riemann's hypothesis ⟺ M(x) grows no faster than x<sup>½+ε</sup>", P(nMax * .5f, 0f) + Vector3.up * (height * .5f + 1.6f * labelSize), labelSize, Factory.Palette.Cosine);
        ExpLabel(g, "the sign at each n: cyan +1 (two or four distinct primes: struck twice, restored)   gold −1 prime   magenta −1 three primes", P(nMax * .5f, 0f) + Vector3.down * (height * .5f + 2.4f + 1.4f * labelSize), labelSize * .75f, Color.white);
    }

    /// <summary>Mertens' function M(x) = Σ_{n≤x} μ(n) as a ribbon along the spiral, with ±√x for scale (RH ⟺ |M(x)| ≪ x^{½+ε}).</summary>
    public void ExpMertensRibbon(int nMax, float amp, float labelSize)
    {
        Transform g = ExpGroup("H5 mertens").transform;
        int[] M = new int[nMax + 1];
        for (int n = 1; n <= nMax; n++) M[n] = M[n - 1] + Mobius(n);
        float scale = amp / Mathf.Sqrt(nMax);
        const int samples = 4000;
        Vector3[] pts = new Vector3[samples], up = new Vector3[samples], dn = new Vector3[samples], zero = new Vector3[samples];
        for (int i = 0; i < samples; i++)
        {
            float sq = Mathf.Lerp(1.2f, Mathf.Sqrt(nMax), i / (samples - 1f));
            float x = sq * sq;
            pts[i] = OnSpiral(x, 1.0f, M[(int)x] * scale);
            up[i] = OnSpiral(x, 1.0f, sq * scale);
            dn[i] = OnSpiral(x, 1.0f, -sq * scale);
            zero[i] = OnSpiral(x, 1.0f, 0f);
        }

        ExpLine(g, "zero line", .02f, new Color(.45f, .45f, .55f), Factory.NoGlow, zero);
        ExpLine(g, "+sqrt", .03f, new Color(.6f, .6f, .7f, .7f), Factory.NoGlow, up);
        ExpLine(g, "−sqrt", .03f, new Color(.6f, .6f, .7f, .7f), Factory.NoGlow, dn);
        ExpLine(g, "mertens", .08f, Factory.Palette.Cosine, Factory.GlowIntensity, pts);
        if (labelSize > 0f) ExpLabel(g, $"M(x) = Σ μ(n), the running total of the sieve's signs, between ±√x   (M({nMax:N0}) = {M[nMax]})", origin + new Vector3(0f, -Extent - 1.2f * labelSize, 0f) + Vector3.back * 2f, labelSize * .8f, Factory.Palette.Cosine);
    }
}
