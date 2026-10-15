using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>
/// Sections AA–AE (docs/experiments/README.md): geometries that intersect along the prime, cancellation and
/// anti-cancellation curves. Snapshots only (1920 × 1080).
///
/// AA. Two norm surfaces. Every curve k² + Bk + C is a vertical slice of the paraboloid z = x² + y² (Δ &lt; 0) or of the saddle
///     z = x² − y² (Δ &gt; 0), taken at y = √|Δ|/2 and read at x = k + B/2; the two surfaces meet along the squares. The saddle's
///     level set z = n carries the factor pairs of n, the paraboloid's carries its sums of two squares.
/// AB. The Sacks ring of n is the circle of radius √n. It meets the square lattice in r₂(n) points and the triangular lattice in
///     r₃(n) points; a prime p meets them in (8, 12), (8, 0), (0, 12) or (0, 0) points as p ≡ 1, 5, 7, 11 (mod 12). Euler's
///     values are the first row of the lattice of discriminant −163.
/// AC. The first twelve primes make a 12-dimensional residue torus, seen through its 144 pairwise shadows. Euler's line never
///     meets a cancellation cross; below 41² the torus decides primality.
/// AD. On the modular surface the curves are points (Δ &lt; 0), closed geodesics (Δ &gt; 0, not a square) and geodesics running
///     into the cusps (Δ a square: the cancellation curves).
/// AE. The prime race mod 12 between the four lattice classes of AB, and the cosine waves of the zeros of L(s, χ₋₄),
///     L(s, χ₋₃) and L(s, χ₁₂) that drive it.
/// </summary>
public partial class SacksSpiral
{
    public const float NormSurfaceOffset = 12000f;
    public const float RingLatticeOffset = 12800f;
    public const float ResidueTorusOffset = 13600f;
    public const float ModularOffset = 14400f;
    public const float RaceOffset = 15200f;

    static readonly Color ParaboloidColor = new(.38f, .6f, 1f), SaddleWireColor = new(1f, .58f, .3f);
    static readonly Color TriangularColor = new(.3f, .9f, 1f);
    static Color PrimeDot => Factory.Palette.Zero;

    // ================================================================================================
    // AA. two norm surfaces
    // ================================================================================================

    /// <summary>A point of a norm surface: (x, y) in the plane, the norm n as height toward the viewer (−z).</summary>
    static Vector3 NormPoint(Vector3 o, float xs, float hs, float x, float y, float n) => o + new Vector3(x * xs, y * xs, -n * hs);

    public readonly struct NormSlice
    {
        public readonly string Name;
        public readonly int B;
        public readonly long C;
        public readonly Color Color;
        public NormSlice(string name, int b, long c, Color color) { Name = name; B = b; C = c; Color = color; }
        public long Disc => (long)B * B - 4 * C;
        public float Y => Mathf.Sqrt(Mathf.Abs(Disc)) * .5f;
        public long Value(long k) => k * k + B * k + C;
    }

    public static readonly NormSlice[] DefaultNormSlices =
    {
        new("k² + k + 41", 1, 41, new Color(.35f, 1f, .55f)),
        new("k² + k + 17", 1, 17, new Color(.5f, .9f, .75f)),
        new("k² + 1", 0, 1, new Color(1f, .85f, .35f)),
        new("k(k + 1)", 1, 0, new Color(.45f, .6f, 1f)),
        new("k² + k − 1", 1, -1, new Color(1f, .7f, .35f)),
        new("u(u + 4)", 4, 0, new Color(1f, .4f, .35f)),
        new("k² + k − 3", 1, -3, new Color(1f, .78f, .5f)),
        new("u(u + 6)", 6, 0, new Color(1f, .32f, .3f)),
    };

    /// <summary>
    /// The paraboloid z = x² + y² (blue) and the saddle z = x² − y² (orange) over |x| ≤ X, 0 ≤ y ≤ Y: a wedge that opens from the
    /// squares (white, where the two surfaces meet at y = 0). Each slice k² + Bk + C runs along its surface at y = √|Δ|/2 (the
    /// paraboloid for Δ &lt; 0, the saddle for Δ &gt; 0) with its values at x = k + B/2: gold where prime, red where composite.
    /// x is scaled by xs, the depth y by ys, the height by hs; a legend column stands to the right.
    /// </summary>
    public void ExpNormSurfaces(Vector3 o, float xs, float ys, float hs, float X, float Y, NormSlice[] slices, float labelSize)
    {
        Transform g = ExpGroup("AA norm surfaces").transform;
        Vector3 W(float x, float y, float n) => o + new Vector3(x * xs, y * ys, -n * hs);
        const int steps = 80;
        for (int sgn = 1; sgn >= -1; sgn -= 2)
        {
            Color col = (sgn > 0 ? ParaboloidColor : SaddleWireColor) * .6f;
            for (float y = 0f; y <= Y + 1e-3f; y += .5f)
            {
                Vector3[] line = new Vector3[steps + 1];
                for (int st = 0; st <= steps; st++) { float x = -X + 2f * X * st / steps; line[st] = W(x, y, x * x + sgn * y * y); }
                ExpLine(g, sgn > 0 ? "paraboloid" : "saddle", .05f, col, Factory.NoGlow, line);
            }

            for (float x = -X; x <= X + 1e-3f; x += 1f)
            {
                Vector3[] line = new Vector3[41];
                for (int st = 0; st <= 40; st++) { float y = Y * st / 40f; line[st] = W(x, y, x * x + sgn * y * y); }
                ExpLine(g, sgn > 0 ? "paraboloid" : "saddle", .05f, col, Factory.NoGlow, line);
            }
        }

        Vector3[] meet = new Vector3[steps + 1];
        for (int st = 0; st <= steps; st++) { float x = -X + 2f * X * st / steps; meet[st] = W(x, 0f, x * x); }
        GlowLine(ExpLine(g, "the squares: where they meet", .2f, Color.white, AttentionIntensity, meet), 1.6f, .45f);
        List<Vector3> sq = new(); List<Color> sqc = new(); List<float> sqs = new();
        for (int k = -(int)X; k <= (int)X; k++) { sq.Add(W(k, 0f, k * k) + Vector3.back * .2f); sqc.Add(SquareColor); sqs.Add(.45f); }
        ExpPointCloud(g, "squares", sq, sqc, sqs, Factory.GlowIntensity);
        float row = 0f;
        Vector3 legend = o + new Vector3(X * xs + 20f, Y * ys * .5f, -(X * X + .3f * Y * Y) * hs);
        if (labelSize > 0f)
        {
            ExpLabelWide(g, "paraboloid  x² + y²", legend, labelSize * .8f, ParaboloidColor, TextAlignmentOptions.Left);
            ExpLabelWide(g, "saddle  x² − y²", legend + Vector3.forward * (.17f * labelSize), labelSize * .8f, SaddleWireColor, TextAlignmentOptions.Left);
            ExpLabelWide(g, "they meet along the squares  k²", legend + Vector3.forward * (.34f * labelSize), labelSize * .65f, Color.white, TextAlignmentOptions.Left);
            row = .6f * labelSize;
        }

        foreach (NormSlice sl in slices)
        {
            float y = sl.Y, shift = -sl.Disc * .25f;
            Vector3[] line = new Vector3[steps + 1];
            for (int st = 0; st <= steps; st++) { float x = -X + 2f * X * st / steps; line[st] = W(x, y, x * x + shift); }
            ExpLine(g, "slice " + sl.Name, .14f, sl.Color, Factory.SoftGlowIntensity, line);
            List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
            int primes = 0, values = 0;
            for (long k = -(long)X - sl.B; k <= (long)X; k++)
            {
                float x = k + sl.B * .5f;
                if (Mathf.Abs(x) > X + 1e-3f) continue;
                long n = sl.Value(k);
                pts.Add(W(x, y, n) + Vector3.back * .25f);
                bool p = ExpPrime(n);
                cols.Add(n < 2 ? new Color(.45f, .45f, .5f) : p ? PrimeDot : DeadColor);
                sizes.Add(.55f);
                if (x >= 0f) { values++; if (p) primes++; }
            }

            ExpPointCloud(g, "values " + sl.Name, pts, cols, sizes, Factory.GlowIntensity);
            if (labelSize > 0f)
            {
                string surf = sl.Disc < 0 ? "paraboloid" : "saddle";
                ExpLabelWide(g, $"{sl.Name}    Δ = {sl.Disc}    {surf}, y = {sl.Y:0.##}    {primes}/{values} prime",
                    legend + Vector3.forward * row, labelSize * .6f, sl.Color, TextAlignmentOptions.Left);
                row += .13f * labelSize;
            }
        }
    }

    /// <summary>
    /// One number's two level sets: the circle x² + y² = n with its integer points (sums of two squares, gold) and the hyperbola
    /// x² − y² = n with its points ((a + b)/2, (b − a)/2) for every factorization n = a·b (white, labelled a·b).
    /// </summary>
    public void ExpFactorLevelSets(Vector3 o, float half, long n, float labelSize)
    {
        Transform g = ExpGroup("AA level sets " + n).transform;
        float r = Mathf.Sqrt(n), ext = Mathf.Max(r + 2.5f, (n + 1) * .5f + 1.5f), unit = half / ext; // every factor point fits
        Vector3 P(float x, float y) => o + new Vector3(x * unit, y * unit, 0f);
        Color grid = new(.2f, .21f, .27f);
        ExpLine(g, "x axis", .05f, grid * 1.4f, Factory.NoGlow, new[] { P(-ext, 0), P(ext, 0) });
        ExpLine(g, "y axis", .05f, grid * 1.4f, Factory.NoGlow, new[] { P(0, -ext), P(0, ext) });
        Vector3[] circle = new Vector3[161];
        for (int s = 0; s <= 160; s++) { float a = s / 160f * 2f * Mathf.PI; circle[s] = P(r * Mathf.Cos(a), r * Mathf.Sin(a)); }
        ExpLine(g, "circle", .1f, ParaboloidColor, Factory.SoftGlowIntensity, circle);
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3[] h = new Vector3[121];
            for (int s = 0; s <= 120; s++) { float y = -ext + 2f * ext * s / 120f; h[s] = P(side * Mathf.Sqrt(n + y * y), y); }
            ExpLine(g, "hyperbola", .1f, SaddleWireColor, Factory.SoftGlowIntensity, h);
        }

        int sq = 0, fac = 0;
        for (int a = -(int)r - 1; a <= (int)r + 1; a++)
            for (int b = -(int)r - 1; b <= (int)r + 1; b++)
                if ((long)a * a + (long)b * b == n) { ExpDot(g, "sum of squares", P(a, b) + Vector3.back * .2f, .9f, PrimeDot); sq++; }

        for (long a = -n; a <= n; a++)
        {
            if (a == 0 || n % a != 0) continue;
            long b = n / a;
            float x = (a + b) * .5f, y = (b - a) * .5f;
            if (Mathf.Abs(y) > ext) continue;
            ExpDot(g, "factor pair", P(x, y) + Vector3.back * .25f, .8f, Color.white, AttentionIntensity);
            fac++;
            if (labelSize > 0f && a > 0)
                ExpLabelWide(g, $"{a}·{b}", P(x, y) + new Vector3(1.5f, 0f, -.4f), labelSize * .6f, new Color(.9f, .9f, 1f), TextAlignmentOptions.Left);
        }

        if (labelSize > 0f)
        {
            int total = 0;
            for (long a = 1; a <= n; a++) if (n % a == 0) total++;
            ExpLabelWide(g, $"n = {n}", P(0f, -ext) + Vector3.down * (.12f * labelSize + 1f), labelSize * .8f, Color.white);
            ExpLabelWide(g, $"{2 * total} factor points  ·  {sq} square points", P(0f, -ext) + Vector3.down * (.34f * labelSize + 1f), labelSize * .55f, new Color(.8f, .8f, .9f));
        }
    }

    // ================================================================================================
    // AB. rings meet lattices
    // ================================================================================================

    static long R2Count(long n)
    {
        long c = 0;
        for (long a = -(long)System.Math.Sqrt(n) - 1; a * a <= n; a++)
        {
            long b2 = n - a * a;
            if (b2 < 0) continue;
            long b = (long)System.Math.Round(System.Math.Sqrt(b2));
            if (b * b == b2) c += b == 0 ? 1 : 2;
        }

        return c;
    }

    static long R3Count(long n)
    {
        long c = 0, L = (long)System.Math.Sqrt(4.0 * n / 3.0) + 2;
        for (long a = -L; a <= L; a++)
            for (long b = -L; b <= L; b++)
                if (a * a + a * b + b * b == n) c++;
        return c;
    }

    /// <summary>
    /// The Sacks ring of n, the circle of radius √n in lattice units, over the square lattice (grey-blue) and the triangular
    /// lattice (grey-teal). Its points on the square lattice are the ways n = a² + b² (gold), on the triangular lattice the ways
    /// n = a² + ab + b² (cyan); the Sacks point of n, at angle 2π√n, lies on the same ring (white). With band &gt; 0 only lattice
    /// points within that distance of the ring are drawn.
    /// </summary>
    public void ExpRingLattice(Vector3 center, float unit, long n, float labelSize, float band = 0f)
    {
        Transform g = ExpGroup("AB ring " + n).transform;
        float r = Mathf.Sqrt(n), reach = r + 1.7f;
        int L = Mathf.CeilToInt(reach) + 1;
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        int sqHits = 0, triHits = 0;
        for (int i = -L; i <= L; i++)
            for (int j = -L; j <= L; j++)
            {
                float d = Mathf.Sqrt(i * i + j * j);
                if (d > reach || (band > 0f && Mathf.Abs(d - r) > band)) continue;
                Vector3 p = center + new Vector3(i, j, 0f) * unit;
                if ((long)i * i + (long)j * j == n) { ExpDot(g, "a² + b²", p + Vector3.back * .3f, Mathf.Max(unit * .34f, 1.1f), PrimeDot); sqHits++; }
                else { pts.Add(p); cols.Add(new Color(.42f, .48f, .68f)); sizes.Add(Mathf.Max(unit * .07f, .16f)); }
            }

        for (int a = -2 * L; a <= 2 * L; a++)
            for (int b = -2 * L; b <= 2 * L; b++)
            {
                float x = a + b * .5f, y = b * .8660254f, d = Mathf.Sqrt(x * x + y * y);
                if (d > reach || (band > 0f && Mathf.Abs(d - r) > band)) continue;
                Vector3 p = center + new Vector3(x, y, 0f) * unit;
                if ((long)a * a + (long)a * b + (long)b * b == n) { ExpDot(g, "a² + ab + b²", p + Vector3.back * .35f, Mathf.Max(unit * .28f, .95f), TriangularColor); triHits++; }
                else { pts.Add(p + Vector3.forward * .05f); cols.Add(new Color(.25f, .55f, .6f)); sizes.Add(Mathf.Max(unit * .055f, .13f)); }
            }

        ExpPointCloud(g, "lattices", pts, cols, sizes, Factory.SoftGlowIntensity);
        Vector3[] ring = new Vector3[201];
        for (int s = 0; s <= 200; s++) { float a = s / 200f * 2f * Mathf.PI; ring[s] = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * r * unit + Vector3.back * .1f; }
        ExpLine(g, "ring", .1f, new Color(.92f, .92f, 1f), Factory.SoftGlowIntensity, ring);
        double s0 = System.Math.Sqrt(n);
        float th = 2f * Mathf.PI * (float)(s0 - System.Math.Floor(s0));
        Vector3 sacks = center + new Vector3(Mathf.Cos(th), Mathf.Sin(th), 0f) * r * unit + Vector3.back * .4f;
        ExpDot(g, "Sacks point", sacks, Mathf.Max(unit * .22f, .8f), Color.white, AttentionIntensity);
        ExpRing(g, "Sacks point ring", sacks, Mathf.Max(unit * .45f, 1.6f), Color.white, .1f, AttentionIntensity);
        if (labelSize > 0f)
        {
            string cls = n % 12 == 1 ? "both lattices" : n % 12 == 5 ? "square lattice only" : n % 12 == 7 ? "triangular lattice only" : n % 12 == 11 ? "neither lattice" : "";
            float below = (r + 1.9f) * unit;
            ExpLabelWide(g, $"{n} ≡ {n % 12} (mod 12)", center + Vector3.down * (below + .12f * labelSize), labelSize * .8f, Color.white);
            ExpLabelWide(g, $"{sqHits} square + {triHits} triangular points" + (cls.Length > 0 && ExpPrime(n) ? $"\n{cls}" : ""),
                center + Vector3.down * (below + .38f * labelSize), labelSize * .55f, new Color(.8f, .85f, .95f));
        }
    }

    /// <summary>
    /// Euler's lattice Z[(1 + √−163)/2]: rows at heights 0, √163/2, √163, … The norm of a + bτ is a² + ab + 41b², so row 0 holds
    /// the squares and row 1 Euler's values: the ring of k² + k + 41 passes through (k + ½, √163/2). Upper halves of the rings
    /// for k ≤ kMax, gold while prime.
    /// </summary>
    public void ExpEulerLattice(Vector3 o, float unit, int kMax, float labelSize)
    {
        Transform g = ExpGroup("AB Euler lattice").transform;
        float h = Mathf.Sqrt(163f) * .5f;
        float X = kMax + 2.5f;
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        for (int b = -1; b <= 7; b++)
        {
            float y = b * h;
            ExpLine(g, "row " + b, .05f, new Color(.25f, .27f, .36f), Factory.NoGlow, new[] { o + new Vector3(-X, y, 0f) * unit, o + new Vector3(X, y, 0f) * unit });
            for (int a = -(int)X - 4; a <= (int)X + 4; a++)
            {
                float x = a + b * .5f;
                if (Mathf.Abs(x) > X) continue;
                pts.Add(o + new Vector3(x, y, 0f) * unit); cols.Add(new Color(.5f, .55f, .72f)); sizes.Add(unit * .12f);
            }
        }

        ExpPointCloud(g, "lattice", pts, cols, sizes, Factory.SoftGlowIntensity);
        for (int k = 0; k <= kMax; k++)
        {
            long n = (long)k * k + k + 41;
            float r = Mathf.Sqrt(n);
            bool prime = ExpPrime(n);
            Vector3[] arc = new Vector3[121];
            for (int s = 0; s <= 120; s++) { float a = s / 120f * Mathf.PI; arc[s] = o + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * r * unit; }
            ExpLine(g, "ring " + n, prime ? .07f : .12f, prime ? PrimeDot * .75f : DeadColor, prime ? Factory.SoftGlowIntensity : Factory.GlowIntensity, arc);
            Vector3 hit = o + new Vector3(k + .5f, h, 0f) * unit + Vector3.back * .2f, mirror = o + new Vector3(-k - .5f, h, 0f) * unit + Vector3.back * .2f;
            ExpDot(g, "k + ½", hit, unit * .38f, prime ? PrimeDot : DeadColor);
            ExpDot(g, "−k − ½", mirror, unit * .38f, prime ? PrimeDot : DeadColor);
            if (labelSize > 0f && (k == 0 || k == kMax - 1 || k == kMax))
                ExpLabelWide(g, k == kMax && !prime ? $"{n} = 41²" : n.ToString(), o + new Vector3(0f, r + .9f, -.3f) * unit, labelSize * .5f, prime ? PrimeDot : DeadColor);
        }

        GlowLine(ExpLine(g, "row 1: Euler's line", .12f, EulerColor, Factory.GlowIntensity, new[] { o + new Vector3(-X, h, -.1f) * unit, o + new Vector3(X, h, -.1f) * unit }), 1.4f, .4f);
        if (labelSize > 0f)
        {
            ExpLabelWide(g, "row 1:  a² + a + 41", o + new Vector3(X + 1f, h, 0f) * unit, labelSize * .7f, EulerColor, TextAlignmentOptions.Left);
            ExpLabelWide(g, "row 0:  a²", o + new Vector3(X + 1f, 0f, 0f) * unit, labelSize * .7f, SquareColor, TextAlignmentOptions.Left);
            ExpLabelWide(g, "row 2:  a² + 2a + 164", o + new Vector3(X + 1f, 2 * h, 0f) * unit, labelSize * .7f, new Color(.7f, .75f, .9f), TextAlignmentOptions.Left);
        }
    }

    // ================================================================================================
    // AC. the 12-dimensional residue torus and its 144 shadows
    // ================================================================================================

    static readonly int[] TwelvePrimes = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37 };

    static long CurveValue(string code, long k) => code switch
    {
        "euler" => k * k + k + 41,
        "pronic" => k * (k + 1),
        "landau" => k * k + 1,
        "squares" => k * k,
        _ => k
    };

    static Color CurveOverlayColor(string code) => code switch
    {
        "euler" => EulerColor,
        "pronic" => new Color(1f, .35f, .3f),
        "landau" => new Color(1f, .7f, .25f),
        _ => Color.white
    };

    /// <summary>
    /// The 12-dimensional residue torus Z/2 × Z/3 × … × Z/37 through its pairwise shadows: panel (i, j) is the torus
    /// Z/p_i × Z/p_j, one cell per pair of residues, gold by the number of primes up to nStat in the cell. The zero row and column
    /// of a panel (its cancellation cross) hold only p_i and p_j themselves (white). Overlays (comma-separated: euler, pronic,
    /// landau) mark the cells a curve's values visit, each in its own corner of the cell.
    /// </summary>
    public void ExpResidueTorusMatrix(Vector3 o, float cell, long nStat, int count, string overlays, float labelSize, string legend = null)
    {
        Transform g = ExpGroup("AC residue torus").transform;
        int[] P = new int[count];
        System.Array.Copy(TwelvePrimes, P, count);
        const int gap = 2;
        int[] start = new int[count + 1];
        for (int i = 0; i < count; i++) start[i + 1] = start[i] + P[i] + gap;
        int total = start[count] - gap;
        Vector3 Cell(int i, int r, int j, int s) => o + new Vector3((start[j] + s + .5f - total * .5f) * cell, (total * .5f - start[i] - r - .5f) * cell, 0f);
        ExpEnsureSieve(nStat + 1);
        int[][,] counts = new int[count * count][,];
        for (int i = 0; i < count; i++) for (int j = 0; j < count; j++) counts[i * count + j] = new int[P[i], P[j]];
        int[] res = new int[count];
        long primes = 0;
        for (long n = 2; n <= nStat; n++)
        {
            if (!ExpPrime(n)) continue;
            primes++;
            for (int i = 0; i < count; i++) res[i] = (int)(n % P[i]);
            for (int i = 0; i < count; i++) for (int j = 0; j < count; j++) counts[i * count + j][res[i], res[j]]++;
        }

        List<(Vector3, Vector3, Vector3, Color)> quads = new();
        Vector3 du = Vector3.right * cell * .46f, dv = Vector3.up * cell * .46f;
        Color empty = new(.3f, .06f, .06f), impossible = new(.05f, .05f, .07f);
        for (int i = 0; i < count; i++)
            for (int j = 0; j < count; j++)
            {
                double mean = i == j ? primes / (double)(P[i] - 1) : primes / (double)((P[i] - 1) * (P[j] - 1));
                for (int r = 0; r < P[i]; r++)
                    for (int s = 0; s < P[j]; s++)
                    {
                        int c = counts[i * count + j][r, s];
                        Color col;
                        if (i == j && r != s) col = impossible;
                        else if (r == 0 || s == 0) col = c > 0 ? Color.white : empty;
                        else col = Color.Lerp(new Color(.25f, .2f, .06f), count > 6 ? new Color(.95f, .72f, .2f) : new Color(.3f, .24f, .09f), Mathf.Clamp01((float)(c / mean)));
                        quads.Add((Cell(i, r, j, s), du, dv, col));
                    }
            }

        string[] codesAll = string.IsNullOrEmpty(overlays) ? new string[0] : overlays.Split(',');
        if (codesAll.Length == 1 && count > 6)
        {
            // one curve on the full matrix: recolour the cells it visits (bright), dim the rest
            string code = codesAll[0].Trim();
            HashSet<(int, int, int, int)> visit = new();
            for (int i = 0; i < count; i++)
                for (int j = 0; j < count; j++)
                {
                    long period = (long)P[i] * P[j];
                    for (long k = 0; k < period; k++) { long v = CurveValue(code, k); visit.Add((i, (int)(v % P[i]), j, (int)(v % P[j]))); }
                }

            Color hit = CurveOverlayColor(code);
            int q = 0;
            for (int i = 0; i < count; i++)
                for (int j = 0; j < count; j++)
                    for (int r = 0; r < P[i]; r++)
                        for (int s = 0; s < P[j]; s++, q++)
                        {
                            var (c0, u0, v0, col0) = quads[q];
                            bool on = visit.Contains((i, r, j, s)) && !(i == j && r != s);
                            bool cross = r == 0 || s == 0;
                            quads[q] = (c0, u0, v0, on ? (cross ? Color.white : hit) : col0 * (cross ? 1f : .45f));
                        }

            overlays = "";
        }

        ExpQuads(g, "cells", quads, Factory.SoftGlowIntensity);
        if (!string.IsNullOrEmpty(overlays))
        {
            string[] codes = overlays.Split(',');
            Vector3[] corner = { new(-.2f, .2f, 0f), new(.2f, -.2f, 0f), new(.2f, .2f, 0f), new(-.2f, -.2f, 0f) };
            for (int c = 0; c < codes.Length; c++)
            {
                string code = codes[c].Trim();
                List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
                Vector3 off = codes.Length > 1 ? corner[c % 4] * cell : Vector3.zero;
                for (int i = 0; i < count; i++)
                    for (int j = 0; j < count; j++)
                    {
                        HashSet<(int, int)> seen = new();
                        long period = (long)P[i] * P[j];
                        for (long k = 0; k < period; k++)
                        {
                            long v = CurveValue(code, k);
                            seen.Add(((int)(v % P[i]), (int)(v % P[j])));
                        }

                        foreach ((int r, int s) in seen)
                        {
                            if (i == j && r != s) continue;
                            pts.Add(Cell(i, r, j, s) + off + Vector3.back * .1f);
                            cols.Add(CurveOverlayColor(code));
                            sizes.Add(cell * (codes.Length > 1 ? .26f : .32f));
                        }
                    }

                ExpPointCloud(g, "overlay " + code, pts, cols, sizes, Factory.GlowIntensity);
            }
        }

        if (labelSize > 0f && !string.IsNullOrEmpty(legend))
        {
            string[] lines = legend.Split('|');
            for (int l = 0; l < lines.Length; l++)
                ExpLabelWide(g, lines[l], o + new Vector3(-total * .5f * cell - 4f - .12f * labelSize, total * .5f * cell - .14f * labelSize * l, 0f),
                    labelSize * (l == 0 ? .75f : .55f), l == 0 ? Color.white : new Color(.85f, .85f, .95f), TextAlignmentOptions.Right);
        }

        if (labelSize > 0f)
            for (int i = 0; i < count; i++)
            {
                Vector3 top = o + new Vector3((start[i] + P[i] * .5f - total * .5f) * cell, (total * .5f + 1.2f) * cell + .05f * labelSize, 0f);
                Vector3 left = o + new Vector3((-total * .5f - 1.4f) * cell, (total * .5f - start[i] - P[i] * .5f) * cell, 0f);
                ExpLabelWide(g, P[i].ToString(), top, labelSize * .5f, new Color(.85f, .85f, .95f));
                ExpLabelWide(g, P[i].ToString(), left, labelSize * .5f, new Color(.85f, .85f, .95f), TextAlignmentOptions.Right);
            }
    }

    // ================================================================================================
    // AD. the modular surface
    // ================================================================================================

    static Vector3 HalfPlane(Vector3 o, float unit, double u, double v) => o + new Vector3((float)u * unit, (float)v * unit, 0f);

    /// <summary>Primes among |k² + Bk + C| for k from the first positive value on, relative to Σ 1/log|value| (the Hardy–Littlewood ratio).</summary>
    static double PrimeYield(int B, long C, int terms = 3000)
    {
        double hits = 0, expect = 0;
        int seen = 0;
        for (long k = 0; seen < terms && k < 200000; k++)
        {
            long v = k * k + B * k + C;
            if (v < 3) continue;
            seen++;
            ExpEnsureSieve(v + 1);
            if (ExpPrime(v)) hits++;
            expect += 1.0 / System.Math.Log(v);
        }

        return expect > 0 ? hits / expect : 0;
    }

    static Color YieldColor(double y) => y < .05 ? DeadColor : Color.Lerp(new Color(.35f, .3f, .2f), new Color(1f, .82f, .3f), Mathf.Clamp01((float)(y / 3.4)));

    static bool IsSquareNumber(long d)
    {
        if (d < 0) return false;
        long r = (long)System.Math.Round(System.Math.Sqrt(d));
        return r * r == d;
    }

    /// <summary>
    /// The upper half-plane, where the quadratic k² + Bk + C becomes the root pair of x² + Bx + C. Definite curves (Δ &lt; 0) are
    /// points (−B/2, √|Δ|/2) — on the line Re τ = 0 for the squares side, Re τ = −½ for the pronic side; indefinite curves are
    /// semicircles of radius √Δ/2 about −B/2: the geodesics between the roots. A square Δ puts both roots at integers (cusps):
    /// those are the cancellation curves (red); other Δ &gt; 0 close up into geodesics of length 2 log ε (gold by prime yield).
    /// Point colour and size follow the prime yield; the curves that are always even are red.
    /// </summary>
    public void ExpModularPlane(Vector3 o, float unit, float U, float V, int cMax, bool farey, float labelSize, int arcMax = -1)
    {
        if (arcMax < 0) arcMax = cMax;
        Color evenColor = new(.3f, .3f, .36f);
        Transform g = ExpGroup("AD modular plane").transform;
        Color axis = new(.6f, .6f, .7f), faint = new(.24f, .25f, .33f);
        ExpLine(g, "real axis", .1f, axis, Factory.NoGlow, new[] { HalfPlane(o, unit, -U, 0), HalfPlane(o, unit, U, 0) });
        for (int m = Mathf.CeilToInt(-U); m <= Mathf.FloorToInt(U); m++)
        {
            ExpLine(g, "tick", .07f, axis, Factory.NoGlow, new[] { HalfPlane(o, unit, m, 0), HalfPlane(o, unit, m, -.12) });
            if (labelSize > 0f) ExpLabelWide(g, m.ToString(), HalfPlane(o, unit, m, -.12) + Vector3.down * (.1f * labelSize), labelSize * .45f, axis);
        }

        void Semicircle(string name, double c, double r, float width, Color col, float glow)
        {
            List<Vector3> pts = new();
            for (int s = 0; s <= 96; s++)
            {
                double a = s / 96.0 * System.Math.PI, u = c + r * System.Math.Cos(a), v = r * System.Math.Sin(a);
                if (u < -U || u > U || v > V) { if (pts.Count > 1) ExpLine(g, name, width, col, glow, pts.ToArray()); pts.Clear(); continue; }
                pts.Add(HalfPlane(o, unit, u, v));
            }

            if (pts.Count > 1) ExpLine(g, name, width, col, glow, pts.ToArray());
        }

        if (farey)
        {
            // the Farey tessellation: geodesics between neighbouring fractions a/b, c/d (|ad − bc| = 1), denominators ≤ 4
            List<(int, int)> fr = new();
            for (int b = 1; b <= 4; b++)
                for (int a = Mathf.FloorToInt(-U * b) - 1; a <= Mathf.CeilToInt(U * b) + 1; a++)
                    if (System.Math.Abs(Gcd(System.Math.Abs(a), b)) == 1) fr.Add((a, b));
            for (int x = 0; x < fr.Count; x++)
                for (int y = x + 1; y < fr.Count; y++)
                {
                    (int a, int b) = fr[x]; (int c, int d) = fr[y];
                    if (System.Math.Abs(a * d - b * c) != 1) continue;
                    double p = a / (double)b, q = c / (double)d;
                    Semicircle("farey", (p + q) * .5, System.Math.Abs(p - q) * .5, .04f, faint, Factory.NoGlow);
                }
        }

        // the fundamental domain |τ| ≥ 1, |Re τ| ≤ ½
        Color fd = new(.85f, .85f, .95f);
        ExpLine(g, "fd left", .08f, fd, Factory.SoftGlowIntensity, new[] { HalfPlane(o, unit, -.5, .8660254), HalfPlane(o, unit, -.5, V) });
        ExpLine(g, "fd right", .08f, fd, Factory.SoftGlowIntensity, new[] { HalfPlane(o, unit, .5, .8660254), HalfPlane(o, unit, .5, V) });
        List<Vector3> arc = new();
        for (int s = 0; s <= 30; s++) { double a = System.Math.PI / 3 + s / 30.0 * System.Math.PI / 3; arc.Add(HalfPlane(o, unit, System.Math.Cos(a), System.Math.Sin(a))); }
        ExpLine(g, "fd arc", .08f, fd, Factory.SoftGlowIntensity, arc.ToArray());
        ExpLine(g, "Re = 0", .05f, faint * 1.6f, Factory.NoGlow, new[] { HalfPlane(o, unit, 0, 0), HalfPlane(o, unit, 0, V) });

        // indefinite curves: semicircles
        for (int C = 1; C <= arcMax; C++)
        {
            // k² − C: roots ±√C
            bool cancel = IsSquareNumber(C);
            double y = cancel ? 0 : PrimeYield(0, -C);
            Semicircle($"k² − {C}", 0, System.Math.Sqrt(C), cancel ? .13f : .07f, cancel ? DeadColor : YieldColor(y), cancel ? Factory.GlowIntensity : Factory.SoftGlowIntensity);
        }

        for (int C = 0; C <= arcMax; C++)
        {
            // k² + k − C: roots (−1 ± √(1 + 4C))/2; k² + k is even, so k² + k − C has C's parity: even C is always even (grey)
            bool cancel = IsSquareNumber(1 + 4L * C);
            bool even = C % 2 == 0;
            double y = cancel || even ? 0 : PrimeYield(1, -C);
            Color col = cancel ? DeadColor : even ? evenColor : YieldColor(y);
            Semicircle($"k² + k − {C}", -.5, System.Math.Sqrt(1 + 4.0 * C) * .5, cancel ? .13f : .07f, col, cancel ? Factory.GlowIntensity : Factory.SoftGlowIntensity);
        }

        // definite curves: points
        for (int C = 1; C <= cMax; C++)
        {
            double y0 = PrimeYield(0, C), v0 = System.Math.Sqrt(C);
            if (v0 <= V) ExpDot(g, $"k² + {C}", HalfPlane(o, unit, 0, v0) + Vector3.back * .2f, .35f + .22f * (float)y0, YieldColor(y0));
            bool even = C % 2 == 0;
            double y1 = even ? 0 : PrimeYield(1, C), v1 = System.Math.Sqrt(4.0 * C - 1) * .5;
            if (v1 <= V) ExpDot(g, $"k² + k + {C}", HalfPlane(o, unit, -.5, v1) + Vector3.back * .2f, .35f + .22f * (float)y1, even ? evenColor : YieldColor(y1));
        }

        ExpGlowMark(g, HalfPlane(o, unit, -.5, System.Math.Sqrt(163) * .5), 1.1f);
        if (labelSize > 0f)
        {
            Color lab = new(.9f, .9f, 1f);
            void Tag(double u, double v, string text, bool left)
            {
                Vector3 at = HalfPlane(o, unit, u, v) + new Vector3(left ? -1.1f : 1.1f, 0f, -.3f);
                ExpLabelWide(g, text, at, labelSize * .5f, lab, left ? TextAlignmentOptions.Right : TextAlignmentOptions.Left);
            }

            Tag(-.5, System.Math.Sqrt(163) * .5, "k² + k + 41   j = −640320³", true);
            Tag(-.5, System.Math.Sqrt(67) * .5, "k² + k + 17   j = −5280³", true);
            Tag(-.5, System.Math.Sqrt(43) * .5, "k² + k + 11   j = −960³", true);
            Tag(0, 1, "k² + 1   j = 1728 = 12³", false);
            Tag(0, System.Math.Sqrt(2), "k² + 2   j = 20³", false);
            Tag(-.5, .8660254, "k² + k + 1   j = 0", true);
            if (arcMax >= 2 && unit >= 30f)
            {
                // a closed geodesic and a cusp geodesic, labelled at their tops
                ExpLabelWide(g, "k² + k − 1:  closed geodesic, length 2 log φ² = 1.92", HalfPlane(o, unit, -.5, System.Math.Sqrt(5) * .5) + new Vector3(0f, .9f, -.3f),
                    labelSize * .42f, YieldColor(2.5), TextAlignmentOptions.Center);
                ExpLabelWide(g, "k(k + 1): from cusp −1 to cusp 0", HalfPlane(o, unit, -.5, .5) + new Vector3(0f, .9f, -.3f),
                    labelSize * .42f, DeadColor, TextAlignmentOptions.Center);
            }
        }
    }

    // ================================================================================================
    // AE. the prime race mod 12
    // ================================================================================================

    static List<double[]> raceRows;
    static Dictionary<string, List<double>> raceZeros;

    static void LoadRace()
    {
        if (raceRows != null) return;
        raceRows = new List<double[]>();
        TextAsset t = Resources.Load<TextAsset>("race12");
        if (t != null)
            foreach (string line in t.text.Split('\n'))
            {
                string[] f = line.Trim().Split(' ');
                if (f.Length < 7 || line.StartsWith("#")) continue;
                double[] row = new double[7];
                for (int i = 0; i < 7; i++) row[i] = double.Parse(f[i], CultureInfo.InvariantCulture);
                raceRows.Add(row);
            }

        raceZeros = new Dictionary<string, List<double>>();
        TextAsset z = Resources.Load<TextAsset>("lzeros12");
        if (z != null)
            foreach (string line in z.text.Split('\n'))
            {
                string[] f = line.Trim().Split(' ');
                if (f.Length < 2 || line.StartsWith("#")) continue;
                if (!raceZeros.TryGetValue(f[0], out List<double> list)) raceZeros[f[0]] = list = new List<double>();
                list.Add(double.Parse(f[1], CultureInfo.InvariantCulture));
            }
    }

    /// <summary>S_χ(x) = Σ over the zeros ½ ± iγ of L(s, χ) of x^{iγ}/(½ + iγ), a sum of cosine waves in log x.</summary>
    static double ZeroWaves(string chi, double x)
    {
        if (!raceZeros.TryGetValue(chi, out List<double> zs)) return 0;
        double L = System.Math.Log(x), s = 0;
        foreach (double gm in zs) s += 2 * (.5 * System.Math.Cos(gm * L) + gm * System.Math.Sin(gm * L)) / (.25 + gm * gm);
        return s;
    }

    /// <summary>
    /// The race mod 12 (or mod 4): for each class a, (log x/√x)·φ(q)·(π(x; q, a) − π(x; q, 1)) against log x, measured (thick)
    /// and rebuilt from the cosine waves of the L-function zeros (thin): bias 4 (mod 12) or 2 (mod 4) plus 2·S for each character
    /// that separates a from 1. Class 1 (both lattices) leads only where a curve dips below the white zero line.
    /// </summary>
    /// <summary>
    /// The bias from prime squares, ((N₁ − N_a)/2)·π(√x)·log x/√x: N₁ − N_a = 4 mod 12 (all four units square to 1), 2 mod 4.
    /// Asymptotically 4 (or 2); about 4·(1 + 2/log x) in range, which is why the measured race sits above 4.
    /// </summary>
    static double SquareBias(double x, int rootsDiff)
    {
        long s = (long)System.Math.Sqrt(x);
        ExpEnsureSieve(s + 2);
        long pis = 0;
        for (long p = 2; p <= s; p++) if (ExpPrime(p)) pis++;
        return rootsDiff * .5 * pis * System.Math.Log(x) / System.Math.Sqrt(x);
    }

    public void ExpRaceChart(Vector3 o, float width, float height, float labelSize, bool mod4, bool waves, double[] markers = null)
    {
        LoadRace();
        Transform g = ExpGroup("AE race").transform;
        double lx0 = mod4 ? 3 : 4, lx1 = 8;
        Color axis = new(.55f, .55f, .65f);
        (int a, Color col, string chiA, string chiB, string name)[] lanes = mod4
            ? new[] { (3, new Color(1f, .45f, .35f), "m4", "", "3 mod 4  (misses the square lattice)  vs  1 mod 4") }
            : new[]
            {
                (5, new Color(1f, .82f, .3f), "m3", "m12", "5 mod 12  (square lattice only)  vs  1"),
                (7, TriangularColor, "m4", "m12", "7 mod 12  (triangular lattice only)  vs  1"),
                (11, new Color(1f, .45f, .35f), "m4", "m3", "11 mod 12  (neither lattice)  vs  1"),
            };
        int phi = mod4 ? 2 : 4, rootsDiff = mod4 ? 2 : 4;
        double bias = mod4 ? 2 : 4, vMin = mod4 ? -1.5 : -1, vMax = mod4 ? 6.5 : 10;
        float laneH = height / lanes.Length, gapH = mod4 ? 0f : laneH * .24f;
        for (int li = 0; li < lanes.Length; li++)
        {
            var lane = lanes[li];
            float top = height * .5f - li * laneH, h = laneH - gapH;
            Vector3 Pt(double lx, double v) => o + new Vector3((float)((lx - lx0) / (lx1 - lx0) - .5) * width,
                top - h + (float)((System.Math.Max(vMin, System.Math.Min(vMax, v)) - vMin) / (vMax - vMin)) * h, 0f);
            ExpLine(g, "frame", .06f, axis * .7f, Factory.NoGlow, new[] { Pt(lx0, vMin), Pt(lx1, vMin) });
            ExpLine(g, "frame", .06f, axis * .7f, Factory.NoGlow, new[] { Pt(lx0, vMin), Pt(lx0, vMax) });
            GlowLine(ExpLine(g, "zero: class 1 leads below", .12f, Color.white, Factory.GlowIntensity, new[] { Pt(lx0, 0), Pt(lx1, 0) }), 1.3f, .35f);
            for (double lx = lx0; lx < lx1; lx += .1)
                ExpLine(g, "asymptotic bias", .05f, axis, Factory.NoGlow, new[] { Pt(lx, bias), Pt(System.Math.Min(lx + .05, lx1), bias) });
            List<Vector3> meas = new(), fit = new();
            foreach (double[] r in raceRows)
            {
                double x = r[0], lx = System.Math.Log10(x);
                if (lx < lx0 - 1e-9) continue;
                double diff = mod4 ? r[6] - r[5] : r[lane.a == 5 ? 2 : lane.a == 7 ? 3 : 4] - r[1];
                meas.Add(Pt(lx, System.Math.Log(x) / System.Math.Sqrt(x) * phi * diff) + Vector3.back * .1f);
                if (waves)
                    fit.Add(Pt(lx, SquareBias(x, rootsDiff) + 2 * ZeroWaves(lane.chiA, x) + (lane.chiB.Length > 0 ? 2 * ZeroWaves(lane.chiB, x) : 0)) + Vector3.back * .2f);
            }

            ExpLine(g, "measured " + lane.a, .1f, lane.col * .9f, Factory.SoftGlowIntensity, meas.ToArray());
            if (waves) ExpLine(g, "cosine waves of the zeros " + lane.a, .09f, Color.white, Factory.GlowIntensity, fit.ToArray());
            if (labelSize > 0f)
            {
                ExpLabelWide(g, lane.name, Pt(lx0, vMax) + new Vector3(0f, .1f * labelSize, 0f), labelSize * .5f, lane.col, TextAlignmentOptions.Left);
                ExpLabelWide(g, "0", Pt(lx0, 0) + new Vector3(-1.2f, 0f, 0f), labelSize * .4f, Color.white, TextAlignmentOptions.Right);
                ExpLabelWide(g, $"{bias:0}", Pt(lx0, bias) + new Vector3(-1.2f, 0f, 0f), labelSize * .4f, axis, TextAlignmentOptions.Right);
            }

            if (markers != null)
                foreach (double m in markers)
                {
                    double lm = System.Math.Log10(m);
                    ExpLine(g, "marker", .07f, new Color(.9f, .9f, 1f), Factory.SoftGlowIntensity, new[] { Pt(lm, vMin), Pt(lm, vMax) });
                    if (labelSize > 0f && li == 0)
                        ExpLabelWide(g, m.ToString("N0", CultureInfo.InvariantCulture), Pt(lm, vMax) + new Vector3(0f, .22f * labelSize, 0f), labelSize * .45f, new Color(.9f, .9f, 1f));
                }

            if (li == lanes.Length - 1)
                for (int e = (int)lx0; e <= 8; e++)
                {
                    ExpLine(g, "tick", .05f, axis, Factory.NoGlow, new[] { Pt(e, vMin), Pt(e, vMin) + Vector3.down * 1f });
                    if (labelSize > 0f) ExpLabelWide(g, $"10<sup>{e}</sup>", Pt(e, vMin) + Vector3.down * (1.2f + .1f * labelSize), labelSize * .45f, axis);
                }
        }

        if (labelSize > 0f)
            ExpLabelWide(g, "coloured: measured   white: bias from prime squares + cosine waves of the L-function zeros",
                o + new Vector3(0f, -height * .5f - .45f * labelSize - 2f, 0f), labelSize * .45f, new Color(.85f, .85f, .95f));
    }
}
