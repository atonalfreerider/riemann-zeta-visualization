using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Complex = System.Numerics.Complex;

/// <summary>
/// Sections EA–EE (docs/experiments/README.md): symmetries behind the primes, and the circularity of ζ.
///
/// EA. Cyclic symmetry: the star polygon {n/k} (join every k-th of n points) is one closed path exactly when gcd(n, k) = 1,
///     and otherwise falls into gcd(n, k) copies. The primes are the n for which every star is one path: Z/n has no proper
///     subgroup (it is a simple group), and the composition series of Z/n spells out the factorization of n.
/// EB. E8, a lattice defined by symmetry alone (the even unimodular lattice of dimension 8): its 240 roots in the Coxeter
///     plane, and its shells counted point by point. The shell of norm 2n holds 240·σ₃(n) vectors, so the divisor sums,
///     and with them the Euler product ζ(s)ζ(s − 3), come out of the geometry; the primes are the thinnest shells.
/// EC. j = E4³/Δ, with E4 the theta series of E8 (j is the comparison of E8⊕E8⊕E8 with the Leech lattice): its phase
///     portrait shows the modular symmetry, and at τ = (1 + i√D)/2 it is an integer exactly for the class-number-one
///     discriminants, which are the ones behind Euler's prime curves k² + k + (D + 1)/4.
/// ED. The supersymmetric primon gas: integers are many-particle states of prime "particles"; at every level N > 1 the
///     supercharge (move N's smallest prime between bosons and fermions) pairs the states, so Σ_{d|N} μ(d) = 0.
/// EE. The zeros' symmetry class: their spacings follow the unitary ensemble of random matrices (GUE).
/// </summary>
public partial class SacksSpiral
{
    public const float StarPolygonOffset = 31300f;
    public const float E8Offset = 32300f;
    public const float JOffset = 33300f;
    public const float SusyOffset = 34300f;
    public const float SpacingOffset = 35300f;

    static readonly Color[] ComponentColors =
    {
        new(1f, .38f, .32f), new(1f, .62f, .22f), new(1f, .35f, .7f), new(.75f, .45f, 1f), new(.45f, .7f, 1f), new(.4f, 1f, .7f),
    };

    /// <summary>One mesh of (nx + 1)·(ny + 1) vertices spanning the rectangle, coloured per vertex (smooth fields).</summary>
    MeshRenderer ExpColorGrid(Transform g, string name, Vector3 corner, Vector3 du, Vector3 dv, int nx, int ny, System.Func<int, int, Color> color, float glow)
    {
        Vector3[] verts = new Vector3[(nx + 1) * (ny + 1)];
        Color[] cols = new Color[verts.Length];
        for (int j = 0; j <= ny; j++)
            for (int i = 0; i <= nx; i++)
            {
                verts[j * (nx + 1) + i] = corner + du * ((float)i / nx) + dv * ((float)j / ny);
                cols[j * (nx + 1) + i] = color(i, j);
            }

        int[] tris = new int[nx * ny * 12];
        int t = 0;
        for (int j = 0; j < ny; j++)
            for (int i = 0; i < nx; i++)
            {
                int a = j * (nx + 1) + i, b = a + 1, c = a + nx + 2, d = a + nx + 1;
                tris[t++] = a; tris[t++] = c; tris[t++] = b; tris[t++] = a; tris[t++] = d; tris[t++] = c;
                tris[t++] = a; tris[t++] = b; tris[t++] = c; tris[t++] = a; tris[t++] = c; tris[t++] = d;
            }

        Mesh mesh = new() { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.vertices = verts; mesh.colors = cols; mesh.triangles = tris; mesh.RecalculateBounds();
        GameObject go = new(name);
        go.transform.SetParent(g, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = Factory.mainMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.sortingOrder = -10;
        Factory.SetIntensity(mr, glow);
        return mr;
    }

    // ================================================================================================
    // EA. star polygons
    // ================================================================================================

    /// <summary>
    /// The star polygons {n/k} for n = nFrom … nTo (columns) and k = 1 … ⌊n/2⌋ (rows): one closed path when gcd(n, k) = 1
    /// (gold for a prime n, pale otherwise), else gcd(n, k) separate polygons, each in its own colour.
    /// </summary>
    public void ExpStarPolygons(Vector3 o, int nFrom, int nTo, float cellX, float cellY, float radius, float labelSize)
    {
        Transform g = ExpGroup("EA star polygons").transform;
        ExpEnsureSieve(nTo + 2);
        int cols = nTo - nFrom + 1, rows = nTo / 2;
        Vector3 Cell(int n, int k) => o + new Vector3((n - nFrom - (cols - 1) * .5f) * cellX, ((rows - 1) * .5f - (k - 1)) * cellY, 0f);
        for (int n = nFrom; n <= nTo; n++)
        {
            bool prime = ExpPrime(n);
            List<Vector3> dots = new(); List<Color> dc = new(); List<float> ds = new();
            for (int k = 1; k <= n / 2; k++)
            {
                Vector3 c = Cell(n, k);
                Vector3 P(int i) { float a = Mathf.PI * .5f - 2f * Mathf.PI * i / n; return c + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius; }
                int gcd = (int)Gcd(n, k);
                for (int comp = 0; comp < gcd; comp++)
                {
                    List<Vector3> path = new();
                    for (int s = 0; s <= n / gcd; s++) path.Add(P((comp + s * k) % n));
                    Color col = gcd == 1 ? (prime ? PrimeGold : new Color(.75f, .8f, .92f)) : ComponentColors[comp % ComponentColors.Length];
                    ExpLine(g, $"{{{n}/{k}}} part {comp}", radius * (gcd == 1 ? .045f : .06f), col, gcd == 1 ? Factory.SoftGlowIntensity : Factory.GlowIntensity, path.ToArray());
                }

                for (int i = 0; i < n; i++) { dots.Add(P(i) + Vector3.back * .1f); dc.Add(Color.white * .85f); ds.Add(radius * .06f); }
                if (gcd > 1 && labelSize > 0f)
                    ExpLabelWide(g, $"{gcd} pieces", c + new Vector3(0f, -radius * 1.32f, -.2f), labelSize * .26f, ComponentColors[0]);
            }

            ExpPointCloud(g, $"points {n}", dots, dc, ds, Factory.GlowIntensity);
            if (labelSize <= 0f) continue;
            Vector3 head = Cell(n, 1) + new Vector3(0f, cellY * .72f, 0f);
            ExpLabelWide(g, n.ToString(), head, labelSize * .5f, prime ? PrimeGold : Color.white);
            if (!prime) ExpLabelWide(g, string.Join("·", Factorize(n)), head + new Vector3(0f, -labelSize * .045f, 0f), labelSize * .28f, new Color(.75f, .78f, .9f));
        }

        if (labelSize <= 0f) return;
        for (int k = 1; k <= rows; k++)
            ExpLabelWide(g, $"k = {k}", Cell(nFrom, k) + new Vector3(-cellX * .62f, 0f, 0f), labelSize * .3f, new Color(.75f, .78f, .9f), TextAlignmentOptions.Right);
    }

    // ================================================================================================
    // EB. E8
    // ================================================================================================

    /// <summary>The 240 roots of E8 (norm 2): (±1, ±1, 0⁶) in all positions and (±½)⁸ with an even number of minus signs.</summary>
    static List<double[]> E8Roots()
    {
        List<double[]> roots = new();
        for (int i = 0; i < 8; i++)
            for (int j = i + 1; j < 8; j++)
                for (int si = -1; si <= 1; si += 2)
                    for (int sj = -1; sj <= 1; sj += 2)
                    {
                        double[] v = new double[8]; v[i] = si; v[j] = sj; roots.Add(v);
                    }

        for (int mask = 0; mask < 256; mask++)
        {
            int minus = 0;
            for (int b = 0; b < 8; b++) minus += mask >> b & 1;
            if (minus % 2 != 0) continue;
            double[] v = new double[8];
            for (int b = 0; b < 8; b++) v[b] = (mask >> b & 1) == 1 ? -.5 : .5;
            roots.Add(v);
        }

        return roots;
    }

    static double Dot8(double[] a, double[] b) { double s = 0; for (int i = 0; i < 8; i++) s += a[i] * b[i]; return s; }

    static double[,] Mul8(double[,] a, double[,] b)
    {
        double[,] c = new double[8, 8];
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                double s = 0;
                for (int k = 0; k < 8; k++) s += a[i, k] * b[k, j];
                c[i, j] = s;
            }

        return c;
    }

    /// <summary>
    /// An orthonormal basis of the Coxeter plane: c = s₁s₂…s₈ (Bourbaki's simple roots) has order 30, and the projector onto
    /// its eigenplane of angle 2π/30 is (2/30) Σ cos(2πk/30) cᵏ.
    /// </summary>
    static (double[] u1, double[] u2) E8CoxeterPlane()
    {
        double h = .5;
        double[][] simple =
        {
            new[] { h, -h, -h, -h, -h, -h, -h, h }, new double[] { 1, 1, 0, 0, 0, 0, 0, 0 }, new double[] { -1, 1, 0, 0, 0, 0, 0, 0 }, new double[] { 0, -1, 1, 0, 0, 0, 0, 0 },
            new double[] { 0, 0, -1, 1, 0, 0, 0, 0 }, new double[] { 0, 0, 0, -1, 1, 0, 0, 0 }, new double[] { 0, 0, 0, 0, -1, 1, 0, 0 }, new double[] { 0, 0, 0, 0, 0, -1, 1, 0 },
        };
        double[,] c = new double[8, 8];
        for (int i = 0; i < 8; i++) c[i, i] = 1;
        foreach (double[] a in simple)
        {
            double[,] r = new double[8, 8];
            double aa = Dot8(a, a);
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++) r[i, j] = (i == j ? 1 : 0) - 2 * a[i] * a[j] / aa;
            c = Mul8(c, r);
        }

        double[,] p = new double[8, 8], ck = new double[8, 8];
        for (int i = 0; i < 8; i++) ck[i, i] = 1;
        for (int k = 0; k < 30; k++)
        {
            double w = 2.0 / 30 * System.Math.Cos(2 * System.Math.PI * k / 30);
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++) p[i, j] += w * ck[i, j];
            ck = Mul8(ck, c);
        }

        double[] e = { 1, .3, .7, .2, .9, .4, .6, .8 }, u1 = new double[8], u2 = new double[8];
        for (int i = 0; i < 8; i++) for (int j = 0; j < 8; j++) u1[i] += p[i, j] * e[j];
        double n1 = System.Math.Sqrt(Dot8(u1, u1));
        for (int i = 0; i < 8; i++) u1[i] /= n1;
        for (int i = 0; i < 8; i++) for (int j = 0; j < 8; j++) u2[i] += c[i, j] * u1[j];
        double cth = Dot8(u1, u2);
        for (int i = 0; i < 8; i++) u2[i] -= cth * u1[i];
        double n2 = System.Math.Sqrt(Dot8(u2, u2));
        for (int i = 0; i < 8; i++) u2[i] /= n2;
        return (u1, u2);
    }

    /// <summary>The 240 roots of E8 projected to its Coxeter plane (8 rings of 30), with the 6720 edges of the polytope 4₂₁.</summary>
    public void ExpE8Petrie(Vector3 o, float scale, float dot, float labelSize)
    {
        Transform g = ExpGroup("EB E8 roots").transform;
        List<double[]> roots = E8Roots();
        (double[] u1, double[] u2) = E8CoxeterPlane();
        Vector3[] at = new Vector3[roots.Count];
        float[] rad = new float[roots.Count];
        for (int i = 0; i < roots.Count; i++)
        {
            at[i] = o + new Vector3((float)Dot8(roots[i], u1), (float)Dot8(roots[i], u2), 0f) * scale;
            rad[i] = (at[i] - o).magnitude;
        }

        List<float> rings = new();
        foreach (float r in rad) if (!rings.Exists(x => Mathf.Abs(x - r) < scale * 1e-3f)) rings.Add(r);
        rings.Sort();
        List<(Vector3, Vector3)> edges = new();
        for (int i = 0; i < roots.Count; i++)
            for (int j = i + 1; j < roots.Count; j++)
                if (System.Math.Abs(Dot8(roots[i], roots[j]) - 1) < 1e-9) edges.Add((at[i] + Vector3.forward * .2f, at[j] + Vector3.forward * .2f));
        Segments(g, "edges", edges, dot * .07f, new Color(.45f, .55f, .9f) * .5f);
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        for (int i = 0; i < roots.Count; i++)
        {
            int ring = rings.FindIndex(x => Mathf.Abs(x - rad[i]) < scale * 1e-3f);
            pts.Add(at[i] + Vector3.back * .2f);
            cols.Add(Color.Lerp(new Color(1f, .85f, .35f), new Color(1f, .4f, .55f), ring / 7f));
            sizes.Add(dot);
        }

        ExpPointCloud(g, "roots", pts, cols, sizes, Factory.GlowIntensity);
        if (labelSize <= 0f) return;
        float top = rings[^1] + scale * .12f;
        ExpLabelWide(g, $"E8:  240 roots,  {rings.Count} rings of 30,  {edges.Count} edges", o + new Vector3(0f, top + labelSize * .05f, -.3f), labelSize * .42f, Color.white);
        ExpLabelWide(g, "the Coxeter plane: c = s<sub>1</sub>s<sub>2</sub>…s<sub>8</sub> turns it by 2π/30", o + new Vector3(0f, -top - labelSize * .02f, -.3f), labelSize * .32f, new Color(.75f, .78f, .9f));
    }

    /// <summary>
    /// The number of E8 vectors of norm 2n for n ≤ nMax, counted from the lattice itself: integer vectors with an even
    /// coordinate sum, plus half-integer vectors with an even coordinate sum, by multiplying one-coordinate series.
    /// </summary>
    public static long[] E8ShellCounts(int nMax)
    {
        int M = 2 * nMax;
        long[] ei = new long[M + 1], oi = new long[M + 1], eh = new long[M + 1], oh = new long[M + 1];
        for (int k = -20; k <= 20; k++)
        {
            if (k * k <= M) { if ((k & 1) == 0) ei[k * k]++; else oi[k * k]++; }
            if (k * k + k >= 0 && k * k + k <= M) { if ((k & 1) == 0) eh[k * k + k]++; else oh[k * k + k]++; }
        }

        long[] Power8(long[] e1, long[] o1)
        {
            long[] E = new long[M + 1], O = new long[M + 1];
            E[0] = 1;
            for (int r = 0; r < 8; r++)
            {
                long[] nE = new long[M + 1], nO = new long[M + 1];
                for (int a = 0; a <= M; a++)
                {
                    if (E[a] == 0 && O[a] == 0) continue;
                    for (int b = 0; a + b <= M; b++)
                    {
                        nE[a + b] += E[a] * e1[b] + O[a] * o1[b];
                        nO[a + b] += E[a] * o1[b] + O[a] * e1[b];
                    }
                }

                E = nE; O = nO;
            }

            return E;
        }

        long[] aInt = Power8(ei, oi), aHalf = Power8(eh, oh), shells = new long[nMax + 1];
        for (int n = 0; n <= nMax; n++) shells[n] = aInt[2 * n] + (2 * n >= 2 ? aHalf[2 * n - 2] : 0);
        return shells;
    }

    static long Sigma3(long n) { long s = 0; for (long d = 1; d <= n; d++) if (n % d == 0) s += d * d * d; return s; }

    /// <summary>
    /// The E8 shells as a chart: N(2n)/(240 n³) against n (the counts from the lattice, which equal 240·σ₃(n)); primes on the
    /// floor 1 + 1/n³, the composites lifted by their divisors in bands by smallest prime factor, under the ceiling ζ(3).
    /// </summary>
    public void ExpE8ShellChart(Vector3 o, int nMax, float width, float height, float labelSize)
    {
        Transform g = ExpGroup("EB E8 shells").transform;
        long[] shells = E8ShellCounts(nMax);
        ExpEnsureSieve(nMax + 2);
        float y0 = .99f, y1 = 1.215f;
        Vector3 At(double n, double y) => o + new Vector3((float)((n - .5) / nMax * width - width * .5f), (float)((y - y0) / (y1 - y0) * height - height * .5f), 0f);
        List<(Vector3, Vector3)> grid = new() { (At(.5, 1), At(nMax + .5, 1)) };
        Segments(g, "axis", grid, height * .003f, new Color(.6f, .62f, .75f));
        (double y, string text, Color col)[] refs =
        {
            (1.2020569, "ζ(3) = 1.202:  the ceiling", new Color(.75f, .78f, .9f)),
            (1.125, "1 + 1/2³:  every even n sits above", ComponentColors[0]),
            (1 + 1.0 / 27, "1 + 1/3³:  odd multiples of 3", ComponentColors[5]),
        };
        foreach (var r in refs)
        {
            Segments(g, "ref " + r.text, new List<(Vector3, Vector3)> { (At(.5, r.y), At(nMax + .5, r.y)) }, height * .002f, r.col * .45f);
            if (labelSize > 0f) ExpLabelWide(g, r.text, At(nMax + .5, r.y) + new Vector3(width * .012f, 0f, -.3f), labelSize * .3f, r.col, TextAlignmentOptions.Left);
        }

        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        List<Vector3> floor = new();
        bool allMatch = true;
        for (int n = 1; n <= nMax; n++)
        {
            if (shells[n] != 240 * Sigma3(n)) allMatch = false;
            double y = shells[n] / (240.0 * n * n * n);
            bool prime = ExpPrime(n);
            int spf = n == 1 ? 1 : (int)Factorize(n)[0];
            Color col = n == 1 ? Color.white : prime ? PrimeGold : spf == 2 ? ComponentColors[0] : spf == 3 ? ComponentColors[5] : spf == 5 ? ComponentColors[3] : ComponentColors[4];
            pts.Add(At(n, y) + Vector3.back * .2f); cols.Add(col); sizes.Add(height * (prime ? .011f : .009f));
            if (prime) floor.Add(At(n, y) + Vector3.back * .1f);
            List<(Vector3, Vector3)> stem = new() { (At(n, 1), At(n, y)) };
            Segments(g, $"stem {n}", stem, height * .0025f, col * .4f);
        }

        ExpPointCloud(g, "shells", pts, cols, sizes, Factory.GlowIntensity);
        ExpLine(g, "prime floor", height * .004f, PrimeGold * .6f, Factory.SoftGlowIntensity, floor.ToArray());
        if (labelSize <= 0f) return;
        for (int n = 10; n <= nMax; n += 10)
            ExpLabelWide(g, n.ToString(), At(n, 1) + new Vector3(0f, -height * .05f, -.3f), labelSize * .3f, new Color(.7f, .72f, .8f));
        ExpLabelWide(g, "n", At(nMax * .5, 1) + new Vector3(0f, -height * .1f, -.3f), labelSize * .3f, new Color(.7f, .72f, .8f));
        ExpLabelWide(g, $"N(2n) / 240n³:  the shell of norm 2n, counted point by point" + (allMatch ? $" — always 240·σ<sub>3</sub>(n), n ≤ {nMax}" : ""),
            At(.5, y1) + new Vector3(0f, height * .05f, -.3f), labelSize * .38f, Color.white, TextAlignmentOptions.Left);
        ExpLabelWide(g, "gold: the primes, on the floor 1 + 1/n³ (the thinnest shells)      red: even      green: odd multiples of 3      violet: of 5      blue: of 7",
            At(.5, y0) + new Vector3(0f, -height * .17f, -.3f), labelSize * .3f, new Color(.8f, .83f, .93f), TextAlignmentOptions.Left);
        ExpLabelWide(g, "240, 2160, 6720, 17520, 30240, 60480, 82560, 140400, …", At(.5, y1) + new Vector3(0f, height * .005f, -.3f), labelSize * .3f, new Color(.75f, .78f, .9f), TextAlignmentOptions.Left);
    }

    // ================================================================================================
    // EC. j from E8
    // ================================================================================================

    static readonly long[] E4Coefficients = BuildE4();

    static long[] BuildE4()
    {
        long[] shells = E8ShellCounts(14);
        shells[0] = 1;
        return shells;
    }

    /// <summary>j(τ) = E4³/Δ with E4 the E8 theta series and Δ = q Π(1 − qⁿ)²⁴, after moving τ into the fundamental domain.</summary>
    static Complex JInvariant(double x, double y)
    {
        for (int it = 0; it < 400; it++)
        {
            x -= System.Math.Round(x);
            double r2 = x * x + y * y;
            if (r2 >= 1 - 1e-12) break;
            x = -x / r2; y = y / r2;
        }

        Complex q = Complex.Exp(new Complex(-2 * System.Math.PI * y, 2 * System.Math.PI * x));
        Complex e4 = Complex.Zero, qn = Complex.One;
        for (int n = 0; n < E4Coefficients.Length; n++) { e4 += E4Coefficients[n] * qn; qn *= q; }
        Complex prod = Complex.One;
        qn = q;
        for (int n = 1; n <= 14; n++)
        {
            Complex z = Complex.One - qn, z2 = z * z, z4 = z2 * z2, z8 = z4 * z4, z16 = z8 * z8;
            prod *= z16 * z8;
            qn *= q;
        }
        return e4 * e4 * e4 / (q * prod);
    }

    static Color PhaseColor(Complex z)
    {
        double arg = System.Math.Atan2(z.Imaginary, z.Real), mag = z.Magnitude;
        float hue = (float)((arg / (2 * System.Math.PI) + 1) % 1);
        float shade = mag <= 0 ? 0f : (float)(System.Math.Log(mag, 12) % 1);
        if (shade < 0) shade += 1;
        Color c = Color.HSVToRGB(hue, .78f, .55f + .4f * shade);
        return c;
    }

    /// <summary>
    /// The phase portrait of j over [reMin, reMax] × [imMin, imMax] (hue = arg j, brightness steps every factor of 12 in |j|),
    /// with the fundamental domain outlined and i (j = 1728) and ρ (j = 0, a triple zero) marked.
    /// </summary>
    public void ExpJPortrait(Vector3 o, float width, float height, double reMin, double reMax, double imMin, double imMax, int nx, int ny, float labelSize)
    {
        Transform g = ExpGroup("EC j portrait").transform;
        Vector3 corner = o - new Vector3(width * .5f, height * .5f, 0f);
        ExpColorGrid(g, "j phase", corner, new Vector3(width, 0f, 0f), new Vector3(0f, height, 0f), nx, ny,
            (i, j) => PhaseColor(JInvariant(reMin + (reMax - reMin) * i / nx, imMin + (imMax - imMin) * j / ny)), Factory.NoGlow);
        Vector3 W(double x, double y) => corner + new Vector3((float)((x - reMin) / (reMax - reMin) * width), (float)((y - imMin) / (imMax - imMin) * height), -.3f);
        // the fundamental domain: |Re τ| ≤ ½, |τ| ≥ 1
        List<Vector3> fd = new() { W(-.5, imMax) };
        for (int s = 0; s <= 40; s++) { double a = System.Math.PI * (2.0 / 3 - s / 120.0); fd.Add(W(System.Math.Cos(a), System.Math.Sin(a))); }
        fd.Add(W(.5, imMax));
        ExpLine(g, "fundamental domain", width * .003f, Color.white, Factory.GlowIntensity, fd.ToArray());
        ExpDot(g, "i", W(0, 1), width * .007f, Color.white, AttentionIntensity);
        ExpDot(g, "rho", W(-.5, System.Math.Sqrt(3) / 2), width * .007f, Color.white, AttentionIntensity);
        if (labelSize <= 0f) return;
        ExpLabelWide(g, "i:  j = 1728 = 12³", W(0, 1) + new Vector3(0f, -height * .05f, 0f), labelSize * .3f, Color.white);
        ExpLabelWide(g, "ρ:  j = 0", W(-.5, System.Math.Sqrt(3) / 2) + new Vector3(-width * .01f, -height * .05f, 0f), labelSize * .3f, Color.white, TextAlignmentOptions.Right);
        ExpLabelWide(g, "j(τ) = E<sub>4</sub>(τ)³ / Δ(τ),  E<sub>4</sub> = the theta series of E8:  hue = arg j, a brightness step every factor of 12 in |j|", o + new Vector3(0f, height * .5f + labelSize * .035f, -.3f), labelSize * .33f, Color.white);
        ExpLabelWide(g, "every tile is a copy of the white fundamental domain under τ → τ + 1 and τ → −1/τ", o + new Vector3(0f, -height * .5f - labelSize * .035f, -.3f), labelSize * .3f, new Color(.8f, .83f, .93f));
    }

    // j(τ_D) for every discriminant −D ≥ −170, from 80-digit arithmetic (docs/experiments/README.md, section EC):
    // (D, distance of j from the nearest integer, class number)
    static readonly (int D, float dist, int h)[] JDistances =
    {
        (3, 0f, 1), (4, 0f, 1), (7, 0f, 1), (8, 0f, 1), (11, 0f, 1), (12, 0f, 1), (15, .1671f, 2), (16, 0f, 1), (19, 0f, 1), (20, .0905f, 2), (23, .3f, 3),
        (24, .0966f, 2), (27, 0f, 1), (28, 0f, 1), (31, .0884f, 3), (32, .1377f, 2), (35, .2332f, 2), (36, .3967f, 2), (39, .3599f, 4), (40, .3812f, 2),
        (43, 0f, 1), (44, .1321f, 3), (47, .3128f, 5), (48, .4223f, 2), (51, .1117f, 2), (52, .315f, 2), (55, .3514f, 4), (56, .0595f, 4), (59, .0068f, 3),
        (60, .2018f, 2), (63, .3649f, 4), (64, .405f, 2), (67, 0f, 1), (68, .4276f, 4), (71, .2223f, 7), (72, .3133f, 2), (75, .3036f, 2), (76, .4955f, 3),
        (79, .1552f, 5), (80, .2386f, 4), (83, .1176f, 3), (84, .0867f, 4), (87, .1234f, 6), (88, .0767f, 2), (91, .2387f, 2), (92, .2862f, 3),
        (95, .1452f, 8), (96, .0535f, 4), (99, .2804f, 2), (100, .029f, 2), (103, .0019f, 5), (104, .2212f, 6), (107, .3155f, 3), (108, .4979f, 3),
        (111, .485f, 8), (112, .4072f, 2), (115, .3751f, 2), (116, .4758f, 6), (119, .1107f, 10), (120, .3894f, 4), (123, .2292f, 2), (124, .4307f, 3),
        (127, .0972f, 5), (128, .4103f, 4), (131, .493f, 5), (132, .3667f, 4), (135, .4376f, 6), (136, .0801f, 4), (139, .3033f, 3), (140, .0684f, 6),
        (143, .3089f, 10), (144, .1321f, 4), (147, .3998f, 2), (148, .001f, 2), (151, .2991f, 7), (152, .4455f, 6), (155, .2974f, 4), (156, .4938f, 4),
        (159, .1502f, 10), (160, .3747f, 4), (163, 0f, 1), (164, .0027f, 8), (167, .1112f, 11), (168, .2873f, 4),
    };

    /// <summary>
    /// For every discriminant −D down to −170: how far j at the root of the principal form ((1 + i√D)/2 or i√D/2) lies from
    /// the nearest integer. Zero exactly at the 13 class-number-one discriminants; their j values and prime curves named.
    /// </summary>
    public void ExpJIntegerChart(Vector3 o, float width, float height, float labelSize)
    {
        Transform g = ExpGroup("EC j integers").transform;
        int dMax = 170;
        Vector3 At(double D, double d) => o + new Vector3((float)(D / dMax * width - width * .5f), (float)(d / .5 * height - height * .5f), 0f);
        Segments(g, "axis", new List<(Vector3, Vector3)> { (At(0, 0), At(dMax, 0)), (At(0, 0), At(0, .5)) }, height * .004f, new Color(.6f, .62f, .75f));
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        List<(Vector3, Vector3)> stems = new();
        foreach (var (D, dist, h) in JDistances)
        {
            bool one = h == 1;
            stems.Add((At(D, 0), At(D, dist)));
            pts.Add(At(D, dist) + Vector3.back * .2f); cols.Add(one ? PrimeGold : new Color(.55f, .62f, .85f)); sizes.Add(height * (one ? .016f : .01f));
        }

        Segments(g, "stems", stems, height * .003f, new Color(.45f, .5f, .7f) * .6f);
        ExpPointCloud(g, "j distances", pts, cols, sizes, Factory.GlowIntensity);
        if (labelSize <= 0f) return;
        (int D, string j, string curve)[] named =
        {
            (7, "−15³", "k² + k + 2"), (11, "−32³", "k² + k + 3"), (19, "−96³", "k² + k + 5"), (43, "−960³", "k² + k + 11"), (67, "−5280³", "k² + k + 17"), (163, "−640320³", "k² + k + 41"),
        };
        // label anchors (in D) and rows, spread so the crowded small discriminants do not collide
        float[] anchorD = { 0f, 14f, 28f, 46f, 67f, 160f };
        int[] row = { 0, 1, 0, 1, 0, 0 };
        for (int i = 0; i < named.Length; i++)
        {
            var nm = named[i];
            float depth = height * (.17f + row[i] * .13f);
            Vector3 p = At(nm.D, 0), lab = At(anchorD[i], 0) + new Vector3(0f, -depth, 0f);
            Segments(g, $"leader {nm.D}", new List<(Vector3, Vector3)> { (p - Vector3.up * height * .02f, lab + Vector3.up * height * .035f) }, height * .002f, PrimeGold * .5f);
            ExpLabelWide(g, $"{nm.D}:  j = {nm.j}", lab, labelSize * .27f, PrimeGold);
            ExpLabelWide(g, nm.curve, lab - new Vector3(0f, labelSize * .03f, 0f), labelSize * .25f, new Color(.85f, .88f, .95f));
        }

        for (int D = 20; D <= dMax; D += 20)
            if (D != 160) ExpLabelWide(g, D.ToString(), At(D, 0) + new Vector3(0f, -height * .05f, -.3f), labelSize * .27f, new Color(.7f, .72f, .8f));
        ExpLabelWide(g, ".5", At(0, .5) + new Vector3(-width * .01f, 0f, -.3f), labelSize * .27f, new Color(.7f, .72f, .8f), TextAlignmentOptions.Right);
        ExpLabelWide(g, "0", At(0, 0) + new Vector3(-width * .01f, 0f, -.3f), labelSize * .27f, new Color(.7f, .72f, .8f), TextAlignmentOptions.Right);
        ExpLabelWide(g, "distance of j(τ<sub>D</sub>) from the nearest integer, for every discriminant −D:  zero exactly when the class number is 1 (gold)",
            At(0, .5) + new Vector3(0f, height * .08f, -.3f), labelSize * .33f, Color.white, TextAlignmentOptions.Left);
        ExpLabelWide(g, "D →", At(dMax, 0) + new Vector3(width * .012f, 0f, -.3f), labelSize * .28f, new Color(.7f, .72f, .8f), TextAlignmentOptions.Left);
    }

    // ================================================================================================
    // ED. the supersymmetric primon gas
    // ================================================================================================

    /// <summary>
    /// The states of the supersymmetric primon gas at levels N = 1 … nMax: a state is a split N = m·d with d squarefree
    /// (the primes of d are fermions, those of m bosons); bosonic states (an even number of fermions) above the axis,
    /// fermionic below, each joined to its partner under the supercharge Q, which moves N's smallest prime between m and d.
    /// Only the vacuum N = 1 has no partner.
    /// </summary>
    public void ExpSusyLadder(Vector3 o, int nMax, float dx, float dy, float dot, float labelSize, int[] annotate)
    {
        Transform g = ExpGroup("ED susy ladder").transform;
        ExpEnsureSieve(nMax + 2);
        Vector3 X(int n) => o + new Vector3((n - (nMax + 1) * .5f) * dx, 0f, 0f);
        Segments(g, "axis", new List<(Vector3, Vector3)> { (X(0) + Vector3.up * dy * .5f, X(nMax + 1) + Vector3.up * dy * .5f), (X(0) - Vector3.up * dy * .5f, X(nMax + 1) - Vector3.up * dy * .5f) },
            dot * .08f, new Color(.6f, .62f, .75f) * .45f);
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        List<(int n, string text)> examples = new();
        Color[] primeColors = { ComponentColors[0], ComponentColors[1], new(1f, .9f, .35f), new(.4f, 1f, .55f), ComponentColors[4], ComponentColors[3] };
        int[] small = { 2, 3, 5, 7, 11, 13 };
        for (int n = 1; n <= nMax; n++)
        {
            Vector3 x = X(n);
            if (n == 1)
            {
                pts.Add(x + Vector3.up * dy); cols.Add(Color.white); sizes.Add(dot * 1.3f);
                RingAround(g, "vacuum", x + Vector3.up * dy + Vector3.back * .2f, Vector3.back, dot * 2.2f, Color.white, dot * .15f, AttentionIntensity);
                continue;
            }

            List<long> ps = new();
            foreach (long f in Factorize(n)) if (!ps.Contains(f)) ps.Add(f);
            long p = ps[0];
            // the squarefree divisors d without p: each pairs (m, d) [p in m] with (m/p, dp) [p in d]
            List<long> rest = ps.GetRange(1, ps.Count - 1);
            int pairs = 1 << rest.Count;
            Color pc = primeColors[System.Math.Max(0, System.Array.IndexOf(small, (int)p))];
            if (System.Array.IndexOf(small, (int)p) < 0) pc = primeColors[^1];
            for (int mask = 0; mask < pairs; mask++)
            {
                long d = 1; int fermions = 0;
                for (int b = 0; b < rest.Count; b++) if ((mask >> b & 1) == 1) { d *= rest[b]; fermions++; }
                // (n/d, d) has `fermions` fermions; its partner (n/(dp), dp) has one more
                bool firstBosonic = fermions % 2 == 0;
                float level = (mask + 1) * dy, shift = (mask - (pairs - 1) * .5f) * dx * .14f;
                Vector3 col0 = x + Vector3.right * shift, up = col0 + Vector3.up * level, down = col0 + Vector3.down * level;
                pts.Add(up); cols.Add(pc); sizes.Add(dot);
                pts.Add(down); cols.Add(pc); sizes.Add(dot);
                ExpLine(g, $"Q pair {n}.{mask} up", dot * .22f, pc * .7f, Factory.SoftGlowIntensity, new[] { up, col0 + Vector3.up * dy * .38f });
                ExpLine(g, $"Q pair {n}.{mask} down", dot * .22f, pc * .7f, Factory.SoftGlowIntensity, new[] { col0 + Vector3.down * dy * .38f, down });
                if (System.Array.IndexOf(annotate, n) >= 0)
                {
                    long bm = firstBosonic ? n / d : n / (d * p), bd = firstBosonic ? d : d * p;
                    long fm = firstBosonic ? n / (d * p) : n / d, fd = firstBosonic ? d * p : d;
                    examples.Add((n, $"({bm}, {bd}) ↔ ({fm}, {fd})"));
                }
            }
        }

        ExpPointCloud(g, "states", pts, cols, sizes, Factory.GlowIntensity);
        if (labelSize <= 0f) return;
        for (int n = 1; n <= nMax; n++)
            ExpLabelWide(g, n.ToString(), X(n) + new Vector3(0f, -labelSize * .012f, -.3f), labelSize * .22f,
                n == 1 ? Color.white : ExpPrime(n) ? PrimeGold : new Color(.62f, .65f, .78f));
        ExpLabelWide(g, "bosonic states", X(0) + new Vector3(-dx * .4f, dy * 2f, -.3f), labelSize * .3f, new Color(.85f, .88f, .95f), TextAlignmentOptions.Right);
        ExpLabelWide(g, "fermionic states", X(0) + new Vector3(-dx * .4f, -dy * 2f, -.3f), labelSize * .3f, new Color(.85f, .88f, .95f), TextAlignmentOptions.Right);
        ExpLabelWide(g, "the vacuum: no partner", X(1) + new Vector3(0f, dy * 2.6f, -.3f), labelSize * .26f, Color.white, TextAlignmentOptions.Left);
        int row = 0;
        foreach (int n in annotate)
        {
            List<string> parts = new();
            foreach (var e in examples) if (e.n == n) parts.Add(e.text);
            ExpLabelWide(g, $"N = {n}:   " + string.Join("     ", parts), X(0) + new Vector3(0f, -dy * 5.2f - row * labelSize * .036f, -.3f), labelSize * .26f,
                new Color(.85f, .88f, .95f), TextAlignmentOptions.Left);
            row++;
        }
    }

    // ================================================================================================
    // EE. the zeros' symmetry class
    // ================================================================================================

    /// <summary>
    /// The nearest-neighbour spacings of the first zeros of ζ (unfolded so the mean spacing is 1) as a histogram, with the
    /// unitary (GUE), orthogonal (GOE) and Poisson laws; above it, the first 50 unfolded zeros against 50 Poisson points.
    /// </summary>
    public void ExpZeroSpacing(Vector3 o, float width, float height, float labelSize)
    {
        Transform g = ExpGroup("EE zero spacing").transform;
        TextAsset ta = Resources.Load<TextAsset>("zetazeros");
        List<double> zs = new();
        foreach (string line in ta.text.Split('\n'))
        {
            string[] parts = line.Trim().Split(' ');
            if (parts.Length >= 2 && double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double v)) zs.Add(v);
        }

        double Smooth(double t) => t / (2 * System.Math.PI) * System.Math.Log(t / (2 * System.Math.PI * System.Math.E)) + .875;
        List<double> x = new(); foreach (double t in zs) x.Add(Smooth(t));
        int bins = 30; float sMax = 3f;
        int[] hist = new int[bins];
        for (int i = 0; i + 1 < x.Count; i++) { int b = (int)((x[i + 1] - x[i]) / (sMax / bins)); if (b < bins) hist[b]++; }
        int count = x.Count - 1;
        float yMax = 1.2f;
        Vector3 At(double s, double p) => o + new Vector3((float)(s / sMax * width - width * .5f), (float)(p / yMax * height - height * .5f), 0f);
        List<(Vector3 c, Vector3 u, Vector3 v, Color col)> bars = new();
        for (int b = 0; b < bins; b++)
        {
            double dens = hist[b] / (double)count / (sMax / bins);
            Vector3 lo = At(b * sMax / bins, 0), hi = At((b + 1) * sMax / bins, dens);
            Vector3 c = (lo + hi) * .5f + Vector3.forward * .3f;
            bars.Add((c, new Vector3((hi.x - lo.x) * .46f, 0f, 0f), new Vector3(0f, (hi.y - lo.y) * .5f, 0f), new Color(.32f, .42f, .7f)));
        }

        ExpQuads(g, "histogram", bars, Factory.NoGlow);
        Segments(g, "axes", new List<(Vector3, Vector3)> { (At(0, 0), At(sMax, 0)), (At(0, 0), At(0, yMax)) }, height * .004f, new Color(.6f, .62f, .75f));
        (System.Func<double, double> f, Color col, string name)[] laws =
        {
            (s => 32 / (System.Math.PI * System.Math.PI) * s * s * System.Math.Exp(-4 * s * s / System.Math.PI), PrimeGold, "GUE (unitary symmetry)"),
            (s => System.Math.PI / 2 * s * System.Math.Exp(-System.Math.PI * s * s / 4), new Color(.45f, 1f, .6f), "GOE (time-reversal symmetry)"),
            (s => System.Math.Exp(-s), new Color(.75f, .75f, .8f), "Poisson (no correlation)"),
        };
        for (int li = 0; li < laws.Length; li++)
        {
            List<Vector3> curve = new();
            for (int i = 0; i <= 150; i++) { double s = sMax * i / 150.0; curve.Add(At(s, System.Math.Min(yMax, laws[li].f(s))) + Vector3.back * .3f); }
            ExpLine(g, laws[li].name, height * (li == 0 ? .007f : .005f), laws[li].col, li == 0 ? Factory.GlowIntensity : Factory.SoftGlowIntensity, curve.ToArray());
            if (labelSize > 0f) ExpLabelWide(g, laws[li].name, At(sMax * .62, yMax * (.92f - .1f * li)), labelSize * .3f, laws[li].col, TextAlignmentOptions.Left);
        }

        if (labelSize > 0f)
        {
            ExpLabelWide(g, $"histogram: the spacings of the first {zs.Count:N0}", At(sMax * .62, yMax * .62f), labelSize * .28f, new Color(.6f, .72f, 1f), TextAlignmentOptions.Left);
            ExpLabelWide(g, "zeros of ζ, unfolded to a mean of 1", At(sMax * .62, yMax * .62f) - new Vector3(0f, labelSize * .032f, 0f), labelSize * .28f, new Color(.6f, .72f, 1f), TextAlignmentOptions.Left);
            for (int s = 0; s <= 3; s++) ExpLabelWide(g, s.ToString(), At(s, 0) + new Vector3(0f, -height * .05f, -.3f), labelSize * .28f, new Color(.7f, .72f, .8f));
            ExpLabelWide(g, "spacing", At(sMax * .5, 0) + new Vector3(0f, -height * .1f, -.3f), labelSize * .28f, new Color(.7f, .72f, .8f));
        }

        // strips: 50 unfolded zeros, and 50 Poisson points with the same mean spacing
        float stripW = width, top = height * .5f + height * .2f;
        Vector3 S(int row, double u) => o + new Vector3((float)(u / 50 * stripW - stripW * .5f), top + row * height * .1f, 0f);
        System.Random rng = new(20261015);
        double acc = 0;
        List<Vector3> sp = new(); List<Color> sc = new(); List<float> ss = new();
        for (int i = 0; i < 50; i++)
        {
            sp.Add(S(1, x[i] - x[0] + .5)); sc.Add(PrimeGold); ss.Add(height * .009f);
            acc += -System.Math.Log(1 - rng.NextDouble());
            sp.Add(S(0, acc)); sc.Add(new Color(.75f, .75f, .8f)); ss.Add(height * .009f);
        }

        ExpPointCloud(g, "strips", sp, sc, ss, Factory.GlowIntensity);
        Segments(g, "strip lines", new List<(Vector3, Vector3)> { (S(0, 0), S(0, 50)), (S(1, 0), S(1, 50)) }, height * .002f, new Color(.5f, .52f, .65f) * .6f);
        if (labelSize > 0f)
        {
            ExpLabelWide(g, "zeros of ζ: they keep their distance", S(1, 0) + new Vector3(-width * .01f, 0f, -.3f), labelSize * .28f, PrimeGold, TextAlignmentOptions.Right);
            ExpLabelWide(g, "random (Poisson) points: they clump", S(0, 0) + new Vector3(-width * .01f, 0f, -.3f), labelSize * .28f, new Color(.75f, .75f, .8f), TextAlignmentOptions.Right);
        }
    }
}
