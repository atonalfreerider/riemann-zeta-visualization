using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Second round of section experiments (docs/experiments/README.md, sections J–O).
///
/// Every quadratic n = q²k² + Bk + C traces a curve on the Sacks spiral that straightens, far out, into a line pointing
/// B/2q of a turn round, at perpendicular offset −πR·Δ/4q² from that ray, where Δ = B² − 4q²C is the discriminant.
/// So a vertical plane through the centre at a rational angle holds the families of that direction (a parabola of
/// curvature cos 2α on the saddle); a vertical plane off the axis holds one quadratic (a comb ordered by Δ); a tilted
/// plane cuts a hyperbola whose asymptotes are two of the saddle's straight lines; the mirror y → −y turns the
/// cancellation curves k² − m² into k² + m² (Euler's criterion); and twisting the saddle is, on the spiral, folding it.
/// Height is measured toward the trail (−z): the saddle falls away along ±x (the squares and pronic rays run down) and
/// rises along ±y (the one-to-four products climb).
/// </summary>
public partial class SacksSpiral
{
    // ================================================================================================
    // the surface these experiments draw on: z = bend · K · r² · cos(folds · θ − twist · r)
    // ================================================================================================

    int expFolds = 2;
    float expTwist;

    /// <summary>folds 2, twist 0 is the tour's saddle. On the spiral the phase is (2π·folds − twist·R)·√n.</summary>
    public void ExpSetSurface(int folds, float twistTimesR)
    {
        expFolds = Mathf.Max(1, folds);
        expTwist = twistTimesR / R;
    }

    float ExpZ(float r, float theta) => bend * SaddleK * r * r * Mathf.Cos(expFolds * theta - expTwist * r);

    Vector3 ExpAt(float r, float theta, float lift = 0f) =>
        origin + new Vector3(r * Mathf.Cos(theta), r * Mathf.Sin(theta), ExpZ(r, theta)) + Vector3.back * lift;

    Vector3 ExpAtXY(float x, float y, float lift = 0f) => ExpAt(Mathf.Sqrt(x * x + y * y), Mathf.Atan2(y, x), lift);

    Vector3 ExpNumberAt(double n, float lift = 0f)
    {
        double s = System.Math.Sqrt(System.Math.Max(0.0, n));
        return ExpAt(R * (float)s, 2f * Mathf.PI * (float)(s - System.Math.Floor(s)), lift);
    }

    /// <summary>The route trick for a curve whose k-th value is about q·k turns out: angle 2π(√n − turns).</summary>
    Vector3 ExpCurveAt(double n, double turns, float lift = 0f)
    {
        double s = System.Math.Sqrt(System.Math.Max(1.0, n));
        double a = s - turns;
        a -= System.Math.Floor(a);
        return ExpAt(R * (float)s, 2f * Mathf.PI * (float)a, lift);
    }

    // ================================================================================================
    // primes, discriminants, kinds
    // ================================================================================================

    static bool[] expNotPrime;

    static void ExpEnsureSieve(long limit)
    {
        if (expNotPrime != null && expNotPrime.Length > limit) return;
        int n = (int)limit + 1;
        expNotPrime = new bool[n];
        expNotPrime[0] = expNotPrime[1] = true;
        for (int i = 2; (long)i * i < n; i++)
        {
            if (expNotPrime[i]) continue;
            for (long j = (long)i * i; j < n; j += i) expNotPrime[j] = true;
        }
    }

    public static bool ExpPrime(long n)
    {
        if (n < 2) return false;
        if (expNotPrime != null && n < expNotPrime.Length) return !expNotPrime[n];
        if (n % 2 == 0) return n == 2;
        for (long d = 3; d * d <= n; d += 2) if (n % d == 0) return false;
        return true;
    }

    public static long Discriminant(int q, int B, long C) => (long)B * B - 4L * q * q * C;

    static bool IsSquareLong(long d)
    {
        if (d < 0) return false;
        long r = (long)System.Math.Round(System.Math.Sqrt(d));
        return r * r == d;
    }

    static readonly Color CancelCurveColor = new(1f, .32f, .26f);
    static readonly Color BetweenCurveColor = new(.42f, .72f, 1f);
    static readonly Color PrimeCurveColor = new(.35f, 1f, .55f);
    static readonly Color UncancelColor = new(1f, .45f, .95f);
    static readonly Color CompositeDotColor = new(.95f, .35f, .3f);
    static readonly Color DimDotColor = new(.3f, .3f, .38f);
    static readonly Color FallColor = new(1f, .45f, .3f);
    static readonly Color RiseColor = new(.35f, .65f, 1f);

    /// <summary>Red: Δ a perfect square (the quadratic factors). Blue: Δ &gt; 0, not a square. Green: Δ &lt; 0.</summary>
    static Color KindColor(int q, int B, long C)
    {
        long D = Discriminant(q, B, C);
        return IsSquareLong(D) ? CancelCurveColor : D > 0 ? BetweenCurveColor : PrimeCurveColor;
    }

    static string Poly(int q, int B, long C)
    {
        string a = q == 1 ? "k²" : $"{q * q}k²";
        string b = B == 0 ? "" : B == 1 ? " + k" : $" + {B}k";
        string c = C == 0 ? "" : C > 0 ? $" + {C}" : $" − {-C}";
        return a + b + c;
    }

    static float KMaxFor(int q, int B, long C, long nMax) =>
        (float)((-B + System.Math.Sqrt((double)B * B + 4.0 * q * q * (nMax - C))) / (2.0 * q * q));

    // ================================================================================================
    // curves and number fields on the current surface
    // ================================================================================================

    /// <summary>n = q²k² + Bk + C for real k from k0 while n ≤ nMax; values at whole k dotted gold (prime) or red.</summary>
    public Vector3 ExpQuadratic(Transform g, int q, int B, long C, float k0, long nMax, Color color, float width, bool dots, float lift = .12f, float dotSize = .26f)
    {
        float k1 = KMaxFor(q, B, C, nMax);
        List<Vector3> pts = new();
        int steps = Mathf.Max(2, Mathf.CeilToInt((k1 - k0) * RouteSteps));
        for (int i = 0; i <= steps; i++)
        {
            double k = k0 + (k1 - k0) * i / (double)steps;
            double n = (double)q * q * k * k + B * k + C;
            if (n < 1.0) continue;
            pts.Add(ExpCurveAt(n, q * k, lift));
        }

        if (pts.Count > 1) ExpLine(g, Poly(q, B, C), width, color, Factory.SoftGlowIntensity, pts.ToArray());
        if (dots)
        {
            List<Vector3> dp = new(); List<Color> dc = new(); List<float> ds = new();
            for (int k = Mathf.CeilToInt(k0); k <= k1; k++)
            {
                long n = (long)q * q * k * k + (long)B * k + C;
                if (n < 2 || n > nMax) continue;
                bool p = ExpPrime(n);
                dp.Add(ExpNumberAt(n, lift + .05f));
                dc.Add(p ? Factory.Palette.Zero : CompositeDotColor);
                ds.Add(p ? dotSize * 1.2f : dotSize);
            }

            if (dp.Count > 0) ExpPointCloud(g, "values", dp, dc, ds, Factory.GlowIntensity);
        }

        return pts.Count > 0 ? pts[^1] : origin;
    }

    /// <summary>Every number on the current surface: primes gold, squares red, pronic numbers blue, the rest dim.</summary>
    public void ExpSurfaceField(int nMax, float lift = 0f)
    {
        Transform g = ExpGroup("O surface field").transform;
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        for (int n = 2; n <= nMax; n++)
        {
            int k = Mathf.FloorToInt(Mathf.Sqrt(n));
            bool p = IsPrime(n), sq = k * k == n, pr = k * (k + 1) == n;
            pts.Add(ExpNumberAt(n, lift));
            cols.Add(p ? Factory.Palette.Zero : sq ? SquareColor : pr ? PronicColor : DimDotColor);
            sizes.Add(p ? .26f : sq || pr ? .3f : .11f);
        }

        ExpPointCloud(g, "numbers", pts, cols, sizes, Factory.SoftGlowIntensity);
    }

    /// <summary>Rings and spokes on the current surface.</summary>
    public void ExpSurfaceWire(float rMax, Color c, float ringStep = 4f, int spokes = 24)
    {
        Transform g = ExpGroup("O surface wire").transform;
        for (float r = ringStep; r <= rMax + .01f; r += ringStep)
        {
            Vector3[] pts = new Vector3[181];
            for (int i = 0; i <= 180; i++) pts[i] = ExpAt(r, i / 180f * 2f * Mathf.PI);
            ExpLine(g, "ring " + r, .03f, c, Factory.NoGlow, pts);
        }

        if (Mathf.Abs(expTwist) > 1e-6f)
        {
            // a twisted surface corrugates along every spoke; its crests (cos = 1, falling furthest from the trail) and troughs
            // (cos = −1, rising toward it) are spirals θ = (twist·r + 2πj)/folds and (twist·r + π + 2πj)/folds
            for (int j = 0; j < expFolds; j++)
            {
                for (int kind = 0; kind < 2; kind++)
                {
                    Vector3[] pts = new Vector3[600];
                    for (int i = 0; i < 600; i++)
                    {
                        float r = rMax * i / 599f;
                        pts[i] = ExpAt(r, (expTwist * r + kind * Mathf.PI + 2f * Mathf.PI * j) / expFolds);
                    }

                    ExpLine(g, kind == 0 ? "crest" : "trough", .06f, kind == 0 ? FallColor : RiseColor, Factory.SoftGlowIntensity, pts);
                }
            }

            return;
        }

        for (int a = 0; a < spokes; a++)
        {
            float th = a / (float)spokes * 2f * Mathf.PI;
            Vector3[] pts = new Vector3[80];
            for (int i = 0; i < 80; i++) pts[i] = ExpAt(rMax * i / 79f, th);
            ExpLine(g, "spoke " + a, .03f, c, Factory.NoGlow, pts);
        }
    }

    /// <summary>The horizontal section z = h of the current surface: 2·folds arms (the hyperbola's half-branches when untwisted).</summary>
    public void ExpLevelArms(float h, Color color, float rMax, float width = .08f)
    {
        Transform g = ExpGroup("O level " + h).transform;
        float kk = bend * SaddleK;
        if (Mathf.Abs(kk) < 1e-6f) return;
        int F = expFolds;
        for (int sign = -1; sign <= 1; sign += 2)
        {
            for (int j = 0; j < F; j++)
            {
                List<Vector3> run = new();
                for (int i = 0; i <= 400; i++)
                {
                    float r = rMax * i / 400f;
                    if (r < 1e-3f) continue;
                    float v = h / (kk * r * r);
                    if (Mathf.Abs(v) > 1f) continue;
                    float th = (expTwist * r + sign * Mathf.Acos(v) + 2f * Mathf.PI * j) / F;
                    run.Add(ExpAt(r, th, .25f));
                }

                if (run.Count > 1) ExpLine(g, $"arm {sign} {j}", width, color, Factory.GlowIntensity, run.ToArray());
            }
        }
    }

    // ================================================================================================
    // J / K. vertical sections: through the centre at any angle, or off the axis
    // ================================================================================================

    static Vector2 Dir(float alphaDeg) => new(Mathf.Cos(alphaDeg * Mathf.Deg2Rad), Mathf.Sin(alphaDeg * Mathf.Deg2Rad));
    static Vector2 LeftOf(float alphaDeg) => new(-Mathf.Sin(alphaDeg * Mathf.Deg2Rad), Mathf.Cos(alphaDeg * Mathf.Deg2Rad));

    /// <summary>A vertical plane along direction α (degrees) at signed offset c to its left, t in [t0, t1], z in [z0, z1] relative to the centre.</summary>
    public void ExpVPlane(float alphaDeg, float c, float t0, float t1, float z0, float z1, Color color, float spacing, bool grid = true)
    {
        Transform g = ExpGroup($"J plane {alphaDeg} {c}").transform;
        Vector2 d = Dir(alphaDeg), nrm = LeftOf(alphaDeg);
        Vector3 P(float t, float z) { Vector2 xy = d * t + nrm * c; return origin + new Vector3(xy.x, xy.y, z); }
        Color dim = new(color.r * .38f, color.g * .38f, color.b * .38f);
        if (grid)
        {
            for (float t = t0; t <= t1 + .01f; t += spacing) ExpLine(g, "grid t", .025f, dim, Factory.NoGlow, new[] { P(t, z0), P(t, z1) });
            for (float z = z0; z <= z1 + .01f; z += spacing) ExpLine(g, "grid z", .025f, dim, Factory.NoGlow, new[] { P(t0, z), P(t1, z) });
        }

        ExpLine(g, "rim", .05f, color, Factory.SoftGlowIntensity, new[] { P(t0, z0), P(t1, z0), P(t1, z1), P(t0, z1) }, true);
    }

    /// <summary>The section of the current surface by that plane, inside radius rMax (a parabola on the saddle; a straight line at 45°).</summary>
    public void ExpVSection(float alphaDeg, float c, float t0, float t1, float rMax, Color color, float width)
    {
        Transform g = ExpGroup($"J section {alphaDeg} {c}").transform;
        Vector2 d = Dir(alphaDeg), nrm = LeftOf(alphaDeg);
        List<Vector3> run = new();
        void Flush() { if (run.Count > 1) ExpLine(g, "section", width, color, Factory.GlowIntensity, run.ToArray()); run.Clear(); }
        for (int i = 0; i <= 300; i++)
        {
            float t = Mathf.Lerp(t0, t1, i / 300f);
            Vector2 xy = d * t + nrm * c;
            if (xy.magnitude > rMax) { Flush(); continue; }
            run.Add(ExpAtXY(xy.x, xy.y, .2f));
        }

        Flush();
    }

    /// <summary>The numbers within tol of the plane with t in [t0, t1], lit (gold primes). Returns (numbers, primes).</summary>
    public Vector2Int ExpVNumbers(float alphaDeg, float c, float t0, float t1, float tol, int nMax, Color color)
    {
        Transform g = ExpGroup($"J numbers {alphaDeg} {c}").transform;
        Vector2 d = Dir(alphaDeg), nrm = LeftOf(alphaDeg);
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        int primes = 0;
        for (int n = 2; n <= nMax; n++)
        {
            float s = Mathf.Sqrt(n), a = 2f * Mathf.PI * s;
            Vector2 xy = new(R * s * Mathf.Cos(a), R * s * Mathf.Sin(a));
            float t = Vector2.Dot(xy, d), off = Vector2.Dot(xy, nrm) - c;
            if (Mathf.Abs(off) > tol || t < t0 || t > t1) continue;
            bool p = IsPrime(n);
            if (p) primes++;
            pts.Add(ExpNumberAt(n, .3f));
            cols.Add(p ? Factory.Palette.Zero : color);
            sizes.Add(p ? .42f : .32f);
        }

        if (pts.Count > 0) ExpPointCloud(g, "in plane", pts, cols, sizes, Factory.GlowIntensity);
        return new Vector2Int(pts.Count, primes);
    }

    /// <summary>Orange where the section falls away from the trail (cos 2α &gt; 0), blue where it rises, white for the straight one.</summary>
    public static Color FanColor(float alphaDeg)
    {
        float c2 = Mathf.Cos(2f * alphaDeg * Mathf.Deg2Rad);
        return c2 >= 0 ? Color.Lerp(Color.white, FallColor, c2) : Color.Lerp(Color.white, RiseColor, -c2);
    }

    /// <summary>
    /// The comb of vertical planes parallel to the x axis: on the +x side the plane y = πR·C holds k² + C far out, on the −x
    /// side the plane y = −πR(c − ¼) holds k² + k + c. Each plane gets its rim, its parabola and its quadratic (dotted).
    /// </summary>
    public void ExpComb(int[] squaresSide, int[] pronicSide, float zLow, float zHigh, bool rims, bool sections, bool curves, float labelSize, int nMax)
    {
        Transform g = ExpGroup("K comb").transform;
        float L = Extent + 4f;
        foreach (int C in squaresSide)
        {
            float y = Mathf.PI * R * C;
            if (Mathf.Abs(y) > L - 1f) continue;
            Color col = KindColor(1, 0, C);
            if (rims) ExpVPlane(0f, y, 0f, L, zLow, zHigh, col, 12f, false);
            if (sections) ExpVSection(0f, y, 0f, L, L, col, .07f);
            if (curves) ExpQuadratic(g, 1, 0, C, 0f, nMax, col, .06f, true);
            if (labelSize > 0f) ExpLabel(g, $"{Poly(1, 0, C)}   Δ {Discriminant(1, 0, C)}", origin + new Vector3(L + 1.5f, y, 0f) + Vector3.back * 1f, labelSize, col, TextAlignmentOptions.Left);
        }

        foreach (int c in pronicSide)
        {
            float off = Mathf.PI * R * (c - .25f);
            if (Mathf.Abs(off) > L - 1f) continue;
            Color col = KindColor(1, 1, c);
            if (rims) ExpVPlane(180f, off, 0f, L, zLow, zHigh, col, 12f, false);
            if (sections) ExpVSection(180f, off, 0f, L, L, col, .07f);
            if (curves) ExpQuadratic(g, 1, 1, c, 0f, nMax, col, .06f, true);
            if (labelSize > 0f) ExpLabel(g, $"{Poly(1, 1, c)}   Δ {Discriminant(1, 1, c)}", origin + new Vector3(-L - 1.5f, -off, 0f) + Vector3.back * 1f, labelSize, col, TextAlignmentOptions.Right);
        }
    }

    static List<int> hlPrimes;

    static long ModPow(long b, long e, long m)
    {
        long r = 1;
        b %= m;
        if (b < 0) b += m;
        while (e > 0)
        {
            if ((e & 1) == 1) r = r * b % m;
            b = b * b % m;
            e >>= 1;
        }

        return r;
    }

    /// <summary>The Hardy–Littlewood constant of k² + Bk + C: Π_p (1 − w(p)/p)/(1 − 1/p), w(p) the number of roots mod p (0 when Δ is a square).</summary>
    public static double HardyLittlewood(int B, long C)
    {
        long D = (long)B * B - 4 * C;
        if (IsSquareLong(D)) return 0;
        if (hlPrimes == null)
        {
            hlPrimes = new List<int>();
            for (int p = 3; p < 20000; p += 2)
            {
                bool pr = true;
                for (int d = 3; d * d <= p; d += 2) if (p % d == 0) { pr = false; break; }
                if (pr) hlPrimes.Add(p);
            }
        }

        int w2 = 0;
        for (int k = 0; k < 2; k++) if (((k * k + B * k + C) % 2 + 2) % 2 == 0) w2++;
        double prod = (1 - w2 / 2.0) / .5;
        foreach (int p in hlPrimes)
        {
            long dm = ((D % p) + p) % p;
            int w = dm == 0 ? 1 : (ModPow(dm, (p - 1) / 2, p) == 1 ? 2 : 0);
            prod *= (1 - w / (double)p) / (1 - 1.0 / p);
        }

        return prod;
    }

    /// <summary>
    /// The prime yield of every plane in the comb as a column of bars: one bar per C (top: largest), length the number of
    /// primes among k² + Bk + C for k ≤ kMax, colour by the kind of discriminant, a white tick at the Hardy–Littlewood
    /// prediction. Returns the correlation between counts and constants over the lines that do not factor.
    /// </summary>
    public float ExpSkyline(int B, int cMin, int cMax, int kMax, Vector3 center, float rowH, float maxLen, float labelSize)
    {
        Transform g = ExpGroup("K skyline " + B).transform;
        ExpEnsureSieve((long)kMax * kMax + (long)B * kMax + cMax + 1);
        int nC = cMax - cMin + 1;
        int[] counts = new int[nC];
        double[] pred = new double[nC], hl = new double[nC];
        int maxCount = 1;
        for (int i = 0; i < nC; i++)
        {
            int C = cMin + i, cnt = 0;
            double sum = 0;
            for (int k = 0; k <= kMax; k++)
            {
                long v = (long)k * k + (long)B * k + C;
                if (v < 2) continue;
                if (ExpPrime(v)) cnt++;
                sum += 1.0 / System.Math.Log(System.Math.Max(3, v));
            }

            counts[i] = cnt;
            hl[i] = HardyLittlewood(B, C);
            pred[i] = hl[i] * sum;
            maxCount = Mathf.Max(maxCount, cnt);
        }

        float yMid = (cMin + cMax) * .5f;
        Vector3 Row(int C) => center + new Vector3(0f, (C - yMid) * rowH, 0f);
        List<int> order = new();
        for (int i = 0; i < nC; i++) order.Add(i);
        order.Sort((a, b) => counts[b].CompareTo(counts[a]));
        HashSet<int> labelled = new();
        for (int i = 0; i < 6; i++) labelled.Add(order[i]);
        for (int i = 0; i < nC; i++)
        {
            int C = cMin + i;
            Vector3 a = Row(C);
            float len = counts[i] / (float)maxCount * maxLen;
            Color col = KindColor(1, B, C);
            bool factors = IsSquareLong(Discriminant(1, B, C));
            ExpLine(g, "bar " + C, rowH * .62f, col, factors ? Factory.GlowIntensity : Factory.SoftGlowIntensity, new[] { a, a + Vector3.right * Mathf.Max(len, factors ? .6f : .05f) });
            float px = (float)(pred[i] / maxCount) * maxLen;
            if (!factors) ExpLine(g, "prediction " + C, .07f, Color.white, Factory.GlowIntensity, new[] { a + new Vector3(px, -rowH * .45f, -.1f), a + new Vector3(px, rowH * .45f, -.1f) });
            if (C % 10 == 0) ExpLabel(g, C.ToString(), a + Vector3.left * (.5f * labelSize), labelSize * .8f, new Color(.75f, .75f, .85f), TextAlignmentOptions.Right);
            if (labelled.Contains(i) || factors && C >= cMin)
            {
                string t = factors ? $"{Poly(1, B, C)}   factors: {counts[i]}" : $"{Poly(1, B, C)}   Δ {Discriminant(1, B, C)}   {counts[i]}";
                ExpLabel(g, t, a + Vector3.right * (Mathf.Max(len, .6f) + 1.0f), labelSize * .8f, col, TextAlignmentOptions.Left);
            }
        }

        // correlation over the lines that do not factor
        List<double> xs = new(), ys = new();
        for (int i = 0; i < nC; i++)
        {
            if (hl[i] <= 0) continue;
            xs.Add(hl[i]);
            ys.Add(counts[i]);
        }

        double mx = 0, my = 0;
        for (int i = 0; i < xs.Count; i++) { mx += xs[i]; my += ys[i]; }
        mx /= xs.Count; my /= xs.Count;
        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < xs.Count; i++) { sxy += (xs[i] - mx) * (ys[i] - my); sxx += (xs[i] - mx) * (xs[i] - mx); syy += (ys[i] - my) * (ys[i] - my); }
        float corr = (float)(sxy / System.Math.Sqrt(sxx * syy));
        string title = B == 0 ? $"primes on k² + C, k ≤ {kMax:N0}: one bar per plane" : $"primes on k² + k + c, k ≤ {kMax:N0}: one bar per plane";
        ExpLabel(g, title, Row(cMax) + new Vector3(maxLen * .9f, 3.4f * labelSize, 0f), labelSize * 1.15f, Color.white);
        ExpLabel(g, $"red: the line factors   blue: Δ > 0   green: Δ < 0\nwhite ticks: Hardy–Littlewood prediction, correlation {corr:0.000}",
            Row(cMax) + new Vector3(maxLen * .9f, 1.5f * labelSize, 0f), labelSize * .75f, new Color(.8f, .8f, .9f));
        return corr;
    }

    // ================================================================================================
    // L. the mirror: k² − m² ↔ k² + m², Euler's criterion and Fermat's method
    // ================================================================================================

    static long Gcd(long a, long b)
    {
        a = System.Math.Abs(a); b = System.Math.Abs(b);
        while (b != 0) { long t = a % b; a = b; b = t; }
        return a;
    }

    /// <summary>Proper representations n = x² + N·y² (gcd(x, N·y) = 1; for N = 1 unordered, x ≥ y ≥ 1).</summary>
    public static List<(long x, long y)> ProperReps(long n, int N)
    {
        List<(long, long)> reps = new();
        for (long y = 0; N * y * y <= n; y++)
        {
            long x2 = n - N * y * y;
            long x = (long)System.Math.Round(System.Math.Sqrt(x2));
            if (x * x != x2 || Gcd(x, N * y) != 1) continue;
            if (N == 1 && x < y) continue;
            reps.Add((x, y));
        }

        return reps;
    }

    /// <summary>Every number coloured by how many pairs of mirror lines k² + m² pass through it (proper representations as a sum of two squares).</summary>
    public void ExpGaussianField(int nMax)
    {
        Transform g = ExpGroup("L gaussian field").transform;
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        for (int n = 2; n <= nMax; n++)
        {
            int reps = ProperReps(n, 1).Count;
            bool p = IsPrime(n);
            Color c = reps == 0 ? DimDotColor : reps == 1 ? (p ? Factory.Palette.Zero : new Color(1f, .9f, .6f)) : CompositeDotColor;
            pts.Add(ExpNumberAt(n, .05f));
            cols.Add(c);
            sizes.Add(reps == 0 ? .1f : reps == 1 ? .3f : .36f);
        }

        ExpPointCloud(g, "gaussian", pts, cols, sizes, Factory.GlowIntensity);
    }

    /// <summary>The comb on the squares side: k² − m² (red, the cancellation curves) and their mirror images k² + m² (magenta), m ≤ mMax.</summary>
    public void ExpMirrorComb(int mMax, int nMax, float labelSize, bool between)
    {
        Transform g = ExpGroup("L mirror comb").transform;
        ExpQuadratic(g, 1, 0, 0, 1f, nMax, SquareColor, .1f, false);
        for (int m = 1; m <= mMax; m++)
        {
            Vector3 a = ExpQuadratic(g, 1, 0, -(long)m * m, m + 1f, nMax, CancelCurveColor, .07f, false);
            Vector3 b = ExpQuadratic(g, 1, 0, (long)m * m, 0f, nMax, UncancelColor, .07f, false);
            if (labelSize > 0f)
            {
                ExpLabel(g, $"k² − {m * m}", a + new Vector3(1.2f, 0f, -.5f), labelSize, CancelCurveColor, TextAlignmentOptions.Left);
                ExpLabel(g, $"k² + {m * m}", b + new Vector3(1.2f, 0f, -.5f), labelSize, UncancelColor, TextAlignmentOptions.Left);
            }
        }

        if (!between) return;
        foreach (int C in new[] { -2, -3, -5, 2, 3, 5 }) ExpQuadratic(g, 1, 0, C, 0f, nMax, new Color(.4f, .4f, .5f), .035f, false);
    }

    /// <summary>The mirror lines through n (one per proper representation, the nearer of its two rows), a ring, and the factor a second pair gives.</summary>
    public string ExpEulerShowcase(long n, float labelSize, Vector3 labelOffset)
    {
        Transform g = ExpGroup("L euler " + n).transform;
        List<(long x, long y)> reps = ProperReps(n, 1);
        Color[] palette = { UncancelColor, new(1f, .8f, .3f), new(.4f, 1f, .9f), new(.75f, .55f, 1f) };
        string text = $"{n}";
        for (int i = 0; i < reps.Count; i++)
        {
            (long x, long y) = reps[i];
            // the mirror line k² + y² passes through n at k = x: draw it from k = x − 3 to k = x + 1
            ExpQuadraticRange(g, 1, 0, y * y, Mathf.Max(0f, x - 3f), x + 1f, palette[i % palette.Length], .12f, true);
            text += $" = {x}² + {y}²";
        }

        Vector3 p = ExpNumberAt(n, .3f);
        ExpRing(g, "ring", p, 1.6f, Color.white, .08f, Factory.GlowIntensity);
        ExpDot(g, "n", p, .6f, reps.Count >= 2 ? CompositeDotColor : Factory.Palette.Zero);
        if (reps.Count >= 2)
        {
            (long a, long b) = reps[0];
            (long c, long d) = reps[1];
            long f = Gcd(n, a * d - b * c);
            if (f == 1 || f == n) f = Gcd(n, a * c - b * d);
            text += $"\n→  gcd({n}, {a}·{d} − {b}·{c}) = {f}:  {n} = {f} × {n / f}";
        }
        else if (reps.Count == 1)
        {
            text += "\none pair only  →  " + (IsPrime((int)n) ? "prime" : "a prime power");
        }

        ExpLabel(g, text, p + labelOffset, labelSize, Color.white);
        return text;
    }

    /// <summary>Fermat's method draws the cancellation curve through n: C_d with d the gap between the two factors nearest √n.</summary>
    public string ExpFermatShowcase(long n, float labelSize, Vector3 labelOffset)
    {
        Transform g = ExpGroup("L fermat " + n).transform;
        long u = (long)System.Math.Floor(System.Math.Sqrt(n));
        while (u > 1 && n % u != 0) u--;
        long v = n / u, d = v - u;
        long root = (long)System.Math.Ceiling(System.Math.Sqrt(n));
        long steps = (u + v) / 2 - root;
        // C_d: u(u + d), u from 1 (the birth at d + 1) to just past n
        List<Vector3> pts = new();
        float uMax = u + 1.5f;
        int samples = Mathf.CeilToInt((uMax - 1f) * RouteSteps);
        for (int i = 0; i <= samples; i++)
        {
            double uu = 1.0 + (uMax - 1.0) * i / samples;
            pts.Add(ExpCurveAt(uu * (uu + d), uu, .15f));
        }

        Color col = CurveColor((int)d);
        ExpLine(g, "C_" + d, .08f, col, Factory.GlowIntensity, pts.ToArray());
        Vector3 p = ExpNumberAt(n, .3f);
        ExpRing(g, "ring", p, 1.1f, Color.white, .06f, Factory.GlowIntensity);
        ExpDot(g, "n", p, .45f, CompositeDotColor);
        ExpRing(g, "birth", ExpNumberAt(d + 1, .3f), .7f, col, .05f, Factory.SoftGlowIntensity);
        string text = $"{n} = {u} × {v}  on C<sub>{d}</sub>  (born at {d + 1}):  Fermat {(steps == 0 ? "finds it at once" : $"needs {steps} step{(steps == 1 ? "" : "s")}")}";
        ExpLabel(g, text, p + labelOffset, labelSize, col);
        return text;
    }

    // ================================================================================================
    // M. the intersection atlas
    // ================================================================================================

    public struct AtlasCurve
    {
        public int B;
        public long C;
        public int KMin;

        public AtlasCurve(int b, long c, int kMin) { B = b; C = c; KMin = kMin; }
    }

    /// <summary>
    /// Draw the curves and mark every number two or more of them share: gold when prime, red when composite, rings by how
    /// many curves meet there. Two curves meet once for each factorization of the difference of their discriminants.
    /// Returns a summary line per pair kind.
    /// </summary>
    public string ExpAtlas(AtlasCurve[] curves, long nMax, float labelSize, long[] labelThese)
    {
        Transform g = ExpGroup("M atlas").transform;
        Dictionary<long, List<int>> at = new();
        for (int i = 0; i < curves.Length; i++)
        {
            AtlasCurve cv = curves[i];
            ExpQuadratic(g, 1, cv.B, cv.C, cv.KMin, nMax, KindColor(1, cv.B, cv.C), .055f, false, .1f);
            for (long k = cv.KMin; ; k++)
            {
                long n = k * k + cv.B * k + cv.C;
                if (n > nMax) break;
                if (n < 2) continue;
                if (!at.TryGetValue(n, out List<int> l)) at[n] = l = new List<int>();
                l.Add(i);
            }
        }

        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        int pp = 0, ppPrime = 0, pc = 0, pb = 0, pbPrime = 0, triples = 0;
        foreach ((long n, List<int> l) in at)
        {
            if (l.Count < 2) continue;
            bool prime = ExpPrime(n);
            Vector3 p = ExpNumberAt(n, .25f);
            pts.Add(p);
            cols.Add(prime ? Factory.Palette.Zero : CompositeDotColor);
            sizes.Add(.32f + .1f * l.Count);
            if (l.Count >= 3) triples++;
            for (int a = 0; a < l.Count; a++)
            for (int b = a + 1; b < l.Count; b++)
            {
                long Da = Discriminant(1, curves[l[a]].B, curves[l[a]].C), Db = Discriminant(1, curves[l[b]].B, curves[l[b]].C);
                bool ca = IsSquareLong(Da), cb = IsSquareLong(Db);
                if (ca || cb) pc++;
                else if (Da < 0 && Db < 0) { pp++; if (prime) ppPrime++; }
                else { pb++; if (prime) pbPrime++; }
            }

            ExpRing(g, "meet " + n, p, .55f + .18f * l.Count, prime ? Factory.Palette.Zero : CompositeDotColor, .04f, Factory.SoftGlowIntensity);
        }

        if (pts.Count > 0) ExpPointCloud(g, "meetings", pts, cols, sizes, Factory.GlowIntensity);
        if (labelThese != null)
        {
            foreach (long n in labelThese)
            {
                if (!at.TryGetValue(n, out List<int> l) || l.Count < 2) continue;
                string text = n.ToString();
                foreach (int i in l)
                {
                    AtlasCurve cv = curves[i];
                    long k = (long)System.Math.Round((-cv.B + System.Math.Sqrt(cv.B * cv.B - 4.0 * (cv.C - n))) / 2.0);
                    text += $" = {Poly(1, cv.B, cv.C).Replace("k", k.ToString())}";
                }

                ExpLabel(g, text, ExpNumberAt(n, .3f) + new Vector3(0f, 1.6f + (labelSize > 8f ? .12f : .6f) * labelSize, -.5f), labelSize, ExpPrime(n) ? Factory.Palette.Zero : CompositeDotColor);
            }
        }

        return $"meeting points {pts.Count} (three or more curves: {triples}); pairs of curves with Δ < 0: {pp} meetings, {ppPrime} prime; " +
               $"involving a curve that factors: {pc}, all composite; other pairs: {pb}, {pbPrime} prime";
    }

    // ================================================================================================
    // N. tilted planes: hyperbolas off the axis, tangent planes, the saddle's straight lines
    // ================================================================================================

    /// <summary>The plane z = p·x + q·y + d0 (relative to the centre) and its section of the saddle; returns the hyperbola's centre (x, y).</summary>
    public Vector2 ExpTiltedPlane(float p, float q, float d0, float half, Color color, bool grid, bool section, float rMax, float width = .08f)
    {
        Transform g = ExpGroup($"N plane {p} {q} {d0}").transform;
        float kk = bend * SaddleK;
        Vector3 P(float x, float y) => origin + new Vector3(x, y, p * x + q * y + d0);
        if (grid)
        {
            Color dim = new(color.r * .35f, color.g * .35f, color.b * .35f);
            for (float s = -half; s <= half + .01f; s += 10f)
            {
                ExpLine(g, "grid x", .025f, dim, Factory.NoGlow, new[] { P(s, -half), P(s, half) });
                ExpLine(g, "grid y", .025f, dim, Factory.NoGlow, new[] { P(-half, s), P(half, s) });
            }

            ExpLine(g, "rim", .05f, color, Factory.SoftGlowIntensity, new[] { P(-half, -half), P(half, -half), P(half, half), P(-half, half) }, true);
        }

        if (Mathf.Abs(kk) < 1e-6f) return Vector2.zero;
        Vector2 ctr = new(p / (2f * kk), -q / (2f * kk));
        float H = d0 + (p * p - q * q) / (4f * kk);
        if (!section) return ctr;
        void Draw(System.Func<float, Vector2> curve, float tMin, float tMax, string name)
        {
            List<Vector3> run = new();
            for (int i = 0; i <= 300; i++)
            {
                Vector2 xy = curve(Mathf.Lerp(tMin, tMax, i / 300f));
                if (xy.magnitude > rMax)
                {
                    if (run.Count > 1) ExpLine(g, name, width, color, Factory.GlowIntensity, run.ToArray());
                    run.Clear();
                    continue;
                }

                run.Add(origin + new Vector3(xy.x, xy.y, p * xy.x + q * xy.y + d0) + Vector3.back * .2f);
            }

            if (run.Count > 1) ExpLine(g, name, width, color, Factory.GlowIntensity, run.ToArray());
        }

        float span = rMax + ctr.magnitude;
        if (Mathf.Abs(H) < 1e-4f)
        {
            Draw(t => ctr + new Vector2(t, t), -span, span, "ruling +");
            Draw(t => ctr + new Vector2(t, -t), -span, span, "ruling −");
            return ctr;
        }

        float a = Mathf.Sqrt(Mathf.Abs(H) / kk);
        float T = Mathf.Log(span / a + Mathf.Sqrt(span * span / (a * a) + 1f));
        for (int b = -1; b <= 1; b += 2)
        {
            int branch = b;
            if (H > 0) Draw(t => ctr + new Vector2(branch * a * (float)System.Math.Cosh(t), a * (float)System.Math.Sinh(t)), -T, T, "branch " + b);
            else Draw(t => ctr + new Vector2(a * (float)System.Math.Sinh(t), branch * a * (float)System.Math.Cosh(t)), -T, T, "branch " + b);
        }

        return ctr;
    }

    /// <summary>The tangent plane at the number n: it cuts the saddle in the two straight lines through n. Numbers within tol of those lines are lit.</summary>
    public Vector2Int ExpTangentPlane(int n, float half, Color color, float tol, int nMax)
    {
        float s = Mathf.Sqrt(n), ang = 2f * Mathf.PI * s;
        float x0 = R * s * Mathf.Cos(ang), y0 = R * s * Mathf.Sin(ang);
        float kk = bend * SaddleK, z0 = kk * (x0 * x0 - y0 * y0);
        float p = 2f * kk * x0, q = -2f * kk * y0, d0 = z0 - p * x0 - q * y0;
        Transform g = ExpGroup("N tangent " + n).transform;
        Vector3 P(float x, float y) => origin + new Vector3(x, y, p * x + q * y + d0);
        Color dim = new(color.r * .35f, color.g * .35f, color.b * .35f);
        for (float t = -half; t <= half + .01f; t += 8f)
        {
            ExpLine(g, "grid a", .025f, dim, Factory.NoGlow, new[] { P(x0 + t, y0 - half), P(x0 + t, y0 + half) });
            ExpLine(g, "grid b", .025f, dim, Factory.NoGlow, new[] { P(x0 - half, y0 + t), P(x0 + half, y0 + t) });
        }

        ExpTiltedPlane(p, q, d0, 0f, Color.white, false, true, Extent + 3f, .1f);
        ExpDot(g, "n", ExpNumberAt(n, .3f), .55f, IsPrime(n) ? Factory.Palette.Zero : CompositeDotColor);
        // numbers near the two rulings
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        int primes = 0;
        Vector2 d1 = new Vector2(1f, 1f).normalized, d2 = new Vector2(1f, -1f).normalized;
        for (int m = 2; m <= nMax; m++)
        {
            if (m == n) continue;
            float sm = Mathf.Sqrt(m), am = 2f * Mathf.PI * sm;
            Vector2 xy = new(R * sm * Mathf.Cos(am) - x0, R * sm * Mathf.Sin(am) - y0);
            float off1 = Mathf.Abs(xy.x * d1.y - xy.y * d1.x), off2 = Mathf.Abs(xy.x * d2.y - xy.y * d2.x);
            if (Mathf.Min(off1, off2) > tol) continue;
            bool pr = IsPrime(m);
            if (pr) primes++;
            pts.Add(ExpNumberAt(m, .3f));
            cols.Add(pr ? Factory.Palette.Zero : new Color(.85f, .85f, .95f));
            sizes.Add(pr ? .4f : .3f);
        }

        if (pts.Count > 0) ExpPointCloud(g, "on the rulings", pts, cols, sizes, Factory.GlowIntensity);
        return new Vector2Int(pts.Count, primes);
    }

    /// <summary>A straight line of the saddle (x − y = a·√2 or x + y = a·√2) as a 3D segment, inside rMax.</summary>
    public void ExpRuling(bool plusDiagonal, float offset, float rMax, Color color, float width)
    {
        Transform g = ExpGroup($"N ruling {plusDiagonal} {offset}").transform;
        Vector2 d = plusDiagonal ? new Vector2(1f, 1f).normalized : new Vector2(1f, -1f).normalized;
        Vector2 nrm = new(-d.y, d.x);
        float span = Mathf.Sqrt(Mathf.Max(0f, rMax * rMax - offset * offset));
        Vector2 a = nrm * offset - d * span, b = nrm * offset + d * span;
        ExpLine(g, "ruling", width, color, Factory.SoftGlowIntensity, new[] { ExpAtXY(a.x, a.y, .2f), ExpAtXY(b.x, b.y, .2f) });
    }

    // ================================================================================================
    // small helpers for the shots
    // ================================================================================================

    public Vector3 ExpNumberPosition(int n) => ExpNumberAt(n, .3f);

    /// <summary>A dot (and a label) at (x, y) on the current surface.</summary>
    public void ExpMarker(Vector2 xy, Color color, string label)
    {
        Transform g = ExpGroup("N marker " + label).transform;
        Vector3 p = ExpAtXY(xy.x, xy.y, .4f);
        ExpDot(g, "marker", p, .7f, color);
        if (!string.IsNullOrEmpty(label)) ExpLabel(g, label, p + new Vector3(0f, 3.4f, -.5f), 7f, color);
    }

    /// <summary>The circle the hyperbolas' centres run round as a plane of slope s turns about the vertical: radius s / 2bK.</summary>
    public void ExpCentreCircle(float slope)
    {
        Transform g = ExpGroup("N centre circle").transform;
        float rad = slope / (2f * bend * SaddleK);
        Vector3[] pts = new Vector3[121];
        for (int i = 0; i <= 120; i++)
        {
            float a = i / 120f * 2f * Mathf.PI;
            pts[i] = ExpAtXY(rad * Mathf.Cos(a), rad * Mathf.Sin(a), .3f);
        }

        ExpLine(g, "circle", .06f, Color.white, Factory.SoftGlowIntensity, pts);
    }

    /// <summary>4k² + k + C (a quarter turn round: +y) and 4k² + 3k + C (three quarters: −y), coloured by the kind of discriminant.</summary>
    public void ExpQuarterFamilies(long[] Cs, long nMax)
    {
        Transform g = ExpGroup("J quarter families").transform;
        foreach (long C in Cs)
        {
            ExpQuadratic(g, 2, 1, C, 0f, nMax, KindColor(2, 1, C), .06f, true);
            ExpQuadratic(g, 2, 3, C, 0f, nMax, KindColor(2, 3, C), .06f, true);
        }
    }

    /// <summary>16k² + k + C (an eighth of a turn: 45°) and 16k² + 5k + C (225°), coloured by the kind of discriminant.</summary>
    public void ExpDiagonalFamilies(long[] Cs, long nMax)
    {
        Transform g = ExpGroup("J diagonal families").transform;
        foreach (long C in Cs)
        {
            ExpQuadratic(g, 4, 1, C, 0f, nMax, KindColor(4, 1, C), .06f, true);
            ExpQuadratic(g, 4, 5, C, 0f, nMax, KindColor(4, 5, C), .06f, true);
        }
    }

    /// <summary>The squares, the pronic numbers, Landau's k² + 1 and Euler's line on the current (twisted or folded) surface; on the
    /// four-fold saddle the one-to-four products, on the three-fold the one-to-nine; on Voronoi's second twist the numbers 2t².</summary>
    public void ExpFamiliesOnSurface(int folds, float twistR, long nMax)
    {
        Transform g = ExpGroup("O families").transform;
        if (Mathf.Abs(twistR) > 1e-6f)
        {
            ExpValuesPolyline(g, 1, 0, 0, 1, nMax, SquareColor, .1f, .5f);
            ExpValuesPolyline(g, 1, 1, 0, 1, nMax, PronicColor, .1f, .5f);
            ExpValuesPolyline(g, 1, 1, 41, 0, nMax, EulerColor, .06f, .4f);
        }
        else
        {
            ExpQuadratic(g, 1, 0, 0, 1f, nMax, SquareColor, .09f, false);
            ExpQuadratic(g, 1, 1, 0, 1f, nMax, PronicColor, .09f, false);
            ExpQuadratic(g, 1, 0, 1, 1f, nMax, new Color(1f, .85f, .35f), .05f, false);
            ExpQuadratic(g, 1, 1, 41, 0f, nMax, EulerColor, .06f, false);
        }
        if (folds == 4)
        {
            ExpQuadratic(g, 2, 1, 0, 1f, nMax, new Color(.45f, .75f, 1f), .08f, false);
            ExpQuadratic(g, 2, 3, 0, 1f, nMax, new Color(.45f, .75f, 1f), .08f, false);
        }

        if (folds == 3)
        {
            ExpQuadratic(g, 3, 2, 0, 1f, nMax, new Color(.75f, .55f, 1f), .08f, false);
            ExpQuadratic(g, 3, 4, 0, 1f, nMax, new Color(.75f, .55f, 1f), .08f, false);
        }

        if (Mathf.Abs(twistR - 4f * Mathf.PI * (1f - Mathf.Sqrt(2f))) < 1e-3f)
        {
            List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
            for (long t = 1; 2 * t * t <= nMax; t++)
            {
                pts.Add(ExpNumberAt(2 * t * t, .35f));
                cols.Add(new Color(.4f, 1f, .95f));
                sizes.Add(.55f);
            }

            ExpPointCloud(g, "2t²", pts, cols, sizes, Factory.GlowIntensity);
        }
    }

    /// <summary>A label at (x, y) relative to the centre, just above the current surface.</summary>
    public void ExpLabelAt(Vector2 xy, string text, Color color, float size)
    {
        ExpLabel(ExpGroup("label " + text).transform, text, ExpAtXY(xy.x, xy.y, 1f), size, color);
    }

    /// <summary>n = q²k² + Bk + C for real k in [kA, kB] (no n limit), values at whole k dotted.</summary>
    public void ExpQuadraticRange(Transform g, int q, int B, long C, float kA, float kB, Color color, float width, bool dots, float lift = .14f)
    {
        List<Vector3> pts = new();
        int steps = Mathf.Max(2, Mathf.CeilToInt((kB - kA) * RouteSteps));
        for (int i = 0; i <= steps; i++)
        {
            double k = kA + (kB - kA) * i / (double)steps;
            double n = (double)q * q * k * k + B * k + C;
            if (n < 1.0) continue;
            pts.Add(ExpCurveAt(n, q * k, lift));
        }

        if (pts.Count > 1) ExpLine(g, Poly(q, B, C) + " segment", width, color, Factory.GlowIntensity, pts.ToArray());
        if (!dots) return;
        List<Vector3> dp = new(); List<Color> dc = new(); List<float> ds = new();
        for (int k = Mathf.CeilToInt(kA); k <= kB; k++)
        {
            long n = (long)q * q * k * k + (long)B * k + C;
            if (n < 2) continue;
            dp.Add(ExpNumberAt(n, lift + .05f));
            dc.Add(color);
            ds.Add(.3f);
        }

        if (dp.Count > 0) ExpPointCloud(g, "segment values", dp, dc, ds, Factory.GlowIntensity);
    }

    /// <summary>On a twisted surface a family is drawn through its actual values only (straight segments between whole k), since
    /// the route between them corrugates with the twist.</summary>
    public void ExpValuesPolyline(Transform g, int q, int B, long C, int kFrom, long nMax, Color color, float width, float dotSize)
    {
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        for (long k = kFrom; ; k++)
        {
            long n = (long)q * q * k * k + B * k + C;
            if (n > nMax) break;
            if (n < 2) continue;
            pts.Add(ExpNumberAt(n, .2f));
            cols.Add(color);
            sizes.Add(dotSize);
        }

        if (pts.Count > 1) ExpLine(g, Poly(q, B, C) + " values", width, color, Factory.SoftGlowIntensity, pts.ToArray());
        if (pts.Count > 0) ExpPointCloud(g, "values", pts, cols, sizes, Factory.GlowIntensity);
    }
}
