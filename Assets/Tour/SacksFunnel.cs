using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Sections CA–CE (docs/experiments/README.md): the divisor spiral, lifted into a funnel and cut by spheres.
///
/// CA. The divisor spiral: every number at its Sacks point with a dot sized by d(n), its number of divisors (as in the
///     picture "Sacks spiral divisors"). The bright lines are product families (ak + b)(ck + e) with ac = q², each at a
///     direction j/2q of a turn; the prime curves run in the dark lanes between them.
/// CB. The factorization funnel: each factor pair n = u·v (u ≤ v) is lifted above n's Sacks point to the height
///     R(v − u)/2. Its distance from the axis is R√(uv), the geometric mean, and its distance from the centre is
///     R(u + v)/2, the arithmetic mean: these are parabolic coordinates. Horizontal planes hold the cancellation curves
///     u(u + d), and a cone about the axis holds one factor ratio, so each bundle of the divisor spiral is the shadow of
///     straight rays climbing its own cone at slope (q² − 1)/2q.
/// CC. Spheres find the factors: the sphere of radius Rx about the centre crosses the vertical line above n at height
///     R√(x² − n), a whole number of R exactly when n = (x − y)(x + y). This is Fermat's method, as spheres crossing a needle.
/// CD. Spheres through the cubic lattice: the sphere of radius √n holds r₃(n) lattice points. For squarefree n ≡ 3 (mod 8)
///     that is 24·h(−n): an odd multiple of 24 exactly when n is prime, and 24 itself only for the Heegner primes
///     11, 19, 43, 67 and 163 behind Euler's prime curves k² + k + 3, 5, 11, 17, 41.
/// CE. The cube ball flattened: seen down its axis, the cube yarn ball (BB) looks like the divisor spiral. Both poles fall on
///     the centre. The north pole's families k³ + Bk + C open out to one side at distance R√(8|B|k/3); the two integers
///     beside the south pole, (k³ + (k + 1)³ ± 1)/2, always factor and ring the centre at radius 2R/√3.
/// </summary>
public partial class SacksSpiral
{
    public const float DivisorSpiralOffset = 21000f;
    public const float FunnelOffset = 22500f;
    public const float FermatOffset = 23500f;
    public const float LatticeSphereOffset = 24500f;
    public const float CubeFlatOffset = 25500f;

    static readonly Color DivisorBlue = new(.42f, .66f, 1f);
    static readonly Color[] RatioColors = { new(1f, 1f, 1f), new(1f, .42f, .34f), new(1f, .78f, .25f), new(.4f, 1f, .55f), new(.45f, .62f, 1f) };
    static int[] divisorCounts;

    /// <summary>d(n) for every n up to <paramref name="n"/> (one sieve, cached).</summary>
    static int[] DivisorCounts(int n)
    {
        if (divisorCounts != null && divisorCounts.Length > n) return divisorCounts;
        int[] d = new int[n + 1];
        for (int a = 1; a <= n; a++)
            for (int b = a; b <= n; b += a) d[b]++;
        divisorCounts = d;
        return d;
    }

    static float DivisorDot(int d, float scale) => scale * (.22f + .2f * Mathf.Sqrt(d));

    static Color DivisorTint(int d) => Color.Lerp(DivisorBlue * .4f, new Color(.72f, .86f, 1f), Mathf.Clamp01((d - 2) / 30f));

    /// <summary>The flat Sacks point of n.</summary>
    static Vector3 SacksFlat(Vector3 o, double n, float R)
    {
        double s = System.Math.Sqrt(n), th = 2 * System.Math.PI * s;
        return o + new Vector3((float)(R * s * System.Math.Cos(th)), (float)(R * s * System.Math.Sin(th)), 0f);
    }

    /// <summary>The factor pair (u, v) of n = uv lifted above n's Sacks point to the height R(v − u)/2 (towards the viewer, −z).</summary>
    static Vector3 FactorLift(Vector3 o, long u, long v, float R) => SacksFlat(o, (double)u * v, R) + Vector3.back * (R * (v - u) * .5f);

    void DivisorCloud(Transform g, Vector3 o, int N, float R, float dotScale, float dim = 1f)
    {
        int[] d = DivisorCounts(N);
        List<Vector3> pts = new(N); List<Color> cols = new(N); List<float> sizes = new(N);
        for (int n = 2; n <= N; n++)
        {
            pts.Add(SacksFlat(o, n, R)); cols.Add(DivisorTint(d[n]) * dim); sizes.Add(DivisorDot(d[n], dotScale));
        }

        ExpPointCloud(g, "divisor spiral", pts, cols, sizes, Factory.SoftGlowIntensity);
    }

    MeshRenderer Segments(Transform g, string name, List<(Vector3 a, Vector3 b)> segs, float width, Color col, float glow = Factory.NoGlow)
    {
        MeshRenderer r = Factory.NewSegmentBatch(name, segs, width, col, glow);
        r.transform.SetParent(g, false);
        return r;
    }

    // ================================================================================================
    // CA. the divisor spiral
    // ================================================================================================

    /// <summary>
    /// The Sacks spiral up to N with each dot sized by d(n) (the primes are the smallest dots); with bundles, the
    /// directions of the product families are named at the rim by their factor ratio.
    /// </summary>
    public void ExpDivisorSpiral(Vector3 o, int N, float R, float dotScale, float labelSize, bool bundles)
    {
        Transform g = ExpGroup("CA divisor spiral").transform;
        DivisorCloud(g, o, N, R, dotScale);
        if (!bundles || labelSize <= 0f) return;
        float rim = R * Mathf.Sqrt(N);
        (float turn, string text)[] marks =
        {
            (0f, "1 : 1   n², u(u + 2), …"), (.5f, "1 : 1   u(u + 1), u(u + 3), …"), (.25f, "1 : 4   k(4k + 1)"), (.75f, "1 : 4   k(4k + 3)"),
            (1f / 6f, "1 : 9   k(9k + 1)"), (1f / 3f, "1 : 9   k(9k + 2)"),
        };
        List<(Vector3, Vector3)> ticks = new();
        foreach (var m in marks)
        {
            float a = m.turn * 2f * Mathf.PI;
            Vector3 dir = new(Mathf.Cos(a), Mathf.Sin(a), 0f);
            ticks.Add((o + dir * (rim * 1.01f), o + dir * (rim * 1.07f)));
            TextAlignmentOptions align = dir.x > .3f ? TextAlignmentOptions.Left : dir.x < -.3f ? TextAlignmentOptions.Right : TextAlignmentOptions.Center;
            Vector3 at = o + dir * (rim * 1.1f) + (Mathf.Abs(dir.x) <= .3f ? new Vector3(0f, dir.y * labelSize * .04f, 0f) : Vector3.zero);
            ExpLabelWide(g, m.text, at, labelSize * .5f, new Color(.82f, .9f, 1f), align);
        }

        Segments(g, "bundle ticks", ticks, rim * .004f, new Color(.82f, .9f, 1f), Factory.SoftGlowIntensity);
    }

    /// <summary>
    /// Prime curves laid over the divisor spiral, each with its mean number of divisors over all its values up to N: Euler's
    /// k² + k + 41, k² + k + 17 and Landau's k² + 1 in the dark lanes, the pronic numbers on a bright line.
    /// </summary>
    public void ExpDivisorCurves(Vector3 o, int N, float R, float dotScale, int kMax, float labelSize)
    {
        Transform g = ExpGroup("CA divisor curves").transform;
        int[] d = DivisorCounts(N);
        (string name, System.Func<long, long> f, Color col, Vector3 off)[] curves =
        {
            ("k² + k + 41", k => k * k + k + 41, EulerColor, new Vector3(1f, 1f, 0f)),
            ("k² + k + 17", k => k * k + k + 17, new Color(1f, .86f, .3f), new Vector3(1f, -1f, 0f)),
            ("k(k + 1)", k => k * (k + 1), new Color(1f, .38f, .32f), new Vector3(-1f, 1f, 0f)),
        };
        foreach (var c in curves)
        {
            List<Vector3> path = new(), pts = new(); List<Color> cols = new(); List<float> sizes = new();
            double sum = 0; int count = 0;
            for (long k = 0; ; k++)
            {
                long n = c.f(k);
                if (n > N) break;
                if (n < 2) continue;
                sum += d[n]; count++;
                if (k > kMax) continue;
                Vector3 p = SacksFlat(o, n, R) + Vector3.back * .4f;
                path.Add(p); pts.Add(p); cols.Add(c.col); sizes.Add(DivisorDot(d[n], dotScale) * 1.6f);
            }

            ExpLine(g, c.name, dotScale * .5f, c.col * .7f, Factory.SoftGlowIntensity, path.ToArray());
            ExpPointCloud(g, c.name + " values", pts, cols, sizes, Factory.GlowIntensity);
            if (labelSize > 0f && path.Count > 1)
            {
                Vector3 end = path[^1];
                ExpLabelWide(g, $"{c.name}:  {sum / count:0.0} divisors on average", end + new Vector3(labelSize * .01f, labelSize * .045f, -.5f),
                    labelSize * .42f, c.col, TextAlignmentOptions.Left);
            }
        }
    }

    // ================================================================================================
    // CB. the factorization funnel
    // ================================================================================================

    /// <summary>
    /// The divisor spiral up to floorN on the floor, and above it the product families lifted to the heights R(v − u)/2:
    /// u(u + δ) as level lines (ratio 1) and (k + b)(q²k + e) for q = 2, 3, 4 as rays on the cones of ratio q²
    /// (half-angle atan(2q/(q² − 1)) from the axis), each cone drawn as a wire funnel; heights stop at hMax.
    /// </summary>
    public void ExpFactorFunnel(Vector3 o, int floorN, int liftN, float R, float dotScale, float hMax, int qMax, float labelSize, float[] labelDeg = null)
    {
        Transform g = ExpGroup("CB factor funnel").transform;
        DivisorCloud(g, o, floorN, R, dotScale, .55f);
        int[] d = DivisorCounts(System.Math.Max(floorN, liftN));
        for (int q = 1; q <= qMax; q++)
        {
            Color col = RatioColors[q];
            List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
            void Family(System.Func<long, (long u, long v)> pair, string name)
            {
                List<Vector3> path = new();
                for (long k = 1; k < 100000; k++)
                {
                    (long u, long v) = pair(k);
                    if (u < 2 || v < u) continue;
                    if (u * v > liftN || R * (v - u) * .5f > hMax) break;
                    Vector3 p = FactorLift(o, u, v, R);
                    path.Add(p); pts.Add(p); cols.Add(col); sizes.Add(DivisorDot(d[u * v], dotScale) * .9f);
                }

                if (path.Count > 1) ExpLine(g, name, dotScale * .08f, col * .55f, Factory.NoGlow, path.ToArray());
            }

            if (q == 1)
                for (int dl = 1; dl <= 10; dl++) { int dd = dl; Family(k => (k, k + dd), $"u(u + {dl})"); }
            else
                for (int b = 0; b <= 1; b++)
                    for (int e = -2 * q; e <= 2 * q; e++)
                    {
                        int bb = b, ee = e, qq = q * q;
                        Family(k => (k + bb, qq * k + ee), $"(k + {b})({qq}k + {e})");
                    }

            ExpPointCloud(g, $"ratio {q * q} lifts", pts, cols, sizes, Factory.GlowIntensity);
            if (q >= 2)
            {
                // the cone of ratio q²: z / r = (q² − 1) / 2q
                float slope = (q * q - 1f) / (2f * q), rTop = hMax / slope;
                List<(Vector3, Vector3)> wire = new();
                for (int i = 0; i < 24; i++)
                {
                    float a = i / 24f * Mathf.PI * 2f;
                    wire.Add((o, o + new Vector3(rTop * Mathf.Cos(a), rTop * Mathf.Sin(a), -hMax)));
                }

                for (int ring = 1; ring <= 3; ring++)
                {
                    float h = hMax * ring / 3f, rr = h / slope;
                    for (int i = 0; i < 72; i++)
                    {
                        float a0 = i / 72f * Mathf.PI * 2f, a1 = (i + 1) / 72f * Mathf.PI * 2f;
                        wire.Add((o + new Vector3(rr * Mathf.Cos(a0), rr * Mathf.Sin(a0), -h), o + new Vector3(rr * Mathf.Cos(a1), rr * Mathf.Sin(a1), -h)));
                    }
                }

                Segments(g, $"cone {q * q}", wire, dotScale * .06f, col * .35f);
                if (labelSize > 0f)
                {
                    float a = (labelDeg != null && labelDeg.Length > q ? labelDeg[q] : 45f) * Mathf.Deg2Rad, h = hMax * 1.02f, rr = h / slope;
                    long num = q * q - 1, den = 2 * q, gd = Gcd(num, den);
                    ExpLabelWide(g, $"1 : {q * q}   slope {num / gd}/{den / gd}", o + new Vector3(rr * Mathf.Cos(a), rr * Mathf.Sin(a), -h - labelSize * .02f),
                        labelSize * .45f, col);
                }
            }
            else if (labelSize > 0f)
                ExpLabelWide(g, "1 : 1   u(u + d) at height d/2", o + new Vector3(R * Mathf.Sqrt(liftN) * .95f, -labelSize * .02f, -R * 9f), labelSize * .45f, col, TextAlignmentOptions.Left);
        }
    }

    /// <summary>
    /// Needles above chosen numbers: the vertical line above n meets the funnel at each factor pair (u, v) of n, at
    /// height R(v − u)/2 (shown up to hMax, each labelled u·v); a prime's needle stays empty below its trivial pair.
    /// The divisor spiral up to floorN lies underneath.
    /// </summary>
    public void ExpFactorNeedles(Vector3 o, long[] ns, string[] titles, Vector2[] titleAt, int floorN, float R, float dotScale, float hMax, float labelSize, Vector3 labelDir)
    {
        Transform g = ExpGroup("CB factor needles").transform;
        DivisorCloud(g, o, floorN, R, dotScale, .55f);
        ExpEnsureSieve(System.Linq.Enumerable.Max(ns) + 10);
        for (int ni = 0; ni < ns.Length; ni++)
        {
            long n = ns[ni];
            Vector3 foot = SacksFlat(o, n, R);
            bool prime = ExpPrime(n);
            Color col = prime ? PrimeGold : IsSquareLong(n) ? SquareColor : new Color(.55f, .78f, 1f);
            ExpLine(g, $"needle {n}", dotScale * .18f, col * .8f, Factory.SoftGlowIntensity, new[] { foot, foot + Vector3.back * hMax });
            List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
            int shown = 0;
            for (long u = (long)System.Math.Sqrt(n); u >= 1; u--)
            {
                if (n % u != 0) continue;
                long v = n / u;
                float h = R * (v - u) * .5f;
                if (h > hMax) break;
                Vector3 p = foot + Vector3.back * h;
                pts.Add(p); cols.Add(col); sizes.Add(dotScale * 1.1f);
                if (labelSize > 0f && ni == 0)
                {
                    bool right = shown % 2 == 0;
                    ExpLabelWide(g, u == v ? $"{u}²" : $"{u} · {v}", p + labelDir * (labelSize * .022f * (right ? 1f : -1f)),
                        labelSize * .3f, col, right ? TextAlignmentOptions.Left : TextAlignmentOptions.Right);
                }
                shown++;
            }

            if (pts.Count > 0) ExpPointCloud(g, $"factor pairs of {n}", pts, cols, sizes, AttentionIntensity);
            if (labelSize <= 0f) continue;
            // titleAt: (sideways along labelDir, height above the floor)
            Vector3 title = foot + labelDir * titleAt[ni].x + Vector3.back * titleAt[ni].y;
            ExpLabelWide(g, titles[ni], title, labelSize * .3f, col);
            if (titleAt[ni].x != 0f)
            {
                Vector3 end = title - labelDir * (Mathf.Sign(titleAt[ni].x) * labelSize * .17f);
                ExpLine(g, $"leader {n}", dotScale * .12f, col * .6f, Factory.NoGlow, new[] { foot + Vector3.back * titleAt[ni].y, end });
            }
        }
    }

    // ================================================================================================
    // CC. spheres find the factors
    // ================================================================================================

    /// <summary>
    /// Fermat's method in the funnel: the divisor spiral up to floorN, the needle above n with a tick at every whole
    /// height R·y, the crossing of every sphere x = ⌈√n⌉ … xMax with the needle at R√(x² − n) (white), gold rings where a
    /// crossing falls on a tick (a factor pair), and for each x in <paramref name="spheres"/> the wire half-sphere with all
    /// its factor points (x − y)(x + y), y = 0 … x − 1.
    /// </summary>
    public void ExpFermatSpheres(Vector3 o, long n, int xMax, int[] spheres, int floorN, float R, float dotScale, float labelSize, Vector3 labelDir)
    {
        Transform g = ExpGroup("CC Fermat spheres").transform;
        DivisorCloud(g, o, floorN, R, dotScale, .5f);
        ExpEnsureSieve(4L * xMax * xMax + 10);
        Vector3 foot = SacksFlat(o, n, R);
        long x0 = (long)System.Math.Ceiling(System.Math.Sqrt(n));
        double yTop = System.Math.Sqrt((double)xMax * xMax - n) + 3;
        ExpLine(g, "needle", dotScale * .14f, Color.white * .8f, Factory.SoftGlowIntensity, new[] { foot, foot + Vector3.back * (float)(R * yTop) });
        List<(Vector3, Vector3)> ticks = new();
        Vector3 side = Vector3.Cross(Vector3.back, (foot - o).normalized).normalized;
        for (int y = 1; y <= (int)yTop; y++)
        {
            Vector3 p = foot + Vector3.back * (R * y);
            float len = y % 5 == 0 ? 1.2f : .6f;
            ticks.Add((p - side * len, p + side * len));
        }

        Segments(g, "needle ticks", ticks, dotScale * .08f, Color.white * .7f);
        List<Vector3> cross = new(); List<Color> crossCols = new(); List<float> crossSizes = new();
        for (long x = x0; x <= xMax; x++)
        {
            double h = System.Math.Sqrt((double)x * x - n);
            long y = (long)System.Math.Round(h);
            bool hit = y * y == x * x - n;
            Vector3 p = foot + Vector3.back * (float)(R * h);
            cross.Add(p); crossCols.Add(hit ? PrimeGold : new Color(.85f, .88f, 1f)); crossSizes.Add(hit ? dotScale * .9f : dotScale * .6f);
            if (!hit) continue;
            RingAround(g, $"factor point x = {x}", p, side, dotScale * 2.2f, PrimeGold, dotScale * .14f, AttentionIntensity);
            if (labelSize > 0f)
                ExpLabelWide(g, $"x = {x}:  {x}² − {n:N0} = {y}²  →  {n:N0} = {x - y} · {x + y}", p + labelDir * (labelSize * .05f),
                    labelSize * .38f, PrimeGold, TextAlignmentOptions.Left);
        }

        ExpPointCloud(g, "sphere crossings", cross, crossCols, crossSizes, Factory.GlowIntensity);
        foreach (int x in spheres)
        {
            // the wire half-sphere of radius Rx, and the factor points of x² − y² on it
            float rad = R * x;
            List<(Vector3, Vector3)> wire = new();
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2f;
                for (int s = 0; s < 18; s++)
                {
                    float b0 = s / 18f * Mathf.PI * .5f, b1 = (s + 1) / 18f * Mathf.PI * .5f;
                    Vector3 P(float b) => o + new Vector3(rad * Mathf.Cos(b) * Mathf.Cos(a), rad * Mathf.Cos(b) * Mathf.Sin(a), -rad * Mathf.Sin(b));
                    wire.Add((P(b0), P(b1)));
                }
            }

            for (int ring = 0; ring < 3; ring++)
            {
                float b = ring / 3f * Mathf.PI * .5f;
                for (int i = 0; i < 96; i++)
                {
                    float a0 = i / 96f * Mathf.PI * 2f, a1 = (i + 1) / 96f * Mathf.PI * 2f;
                    wire.Add((o + new Vector3(rad * Mathf.Cos(b) * Mathf.Cos(a0), rad * Mathf.Cos(b) * Mathf.Sin(a0), -rad * Mathf.Sin(b)),
                        o + new Vector3(rad * Mathf.Cos(b) * Mathf.Cos(a1), rad * Mathf.Cos(b) * Mathf.Sin(a1), -rad * Mathf.Sin(b))));
                }
            }

            Segments(g, $"sphere {x}", wire, dotScale * .05f, YarnColor * .55f);
            List<Vector3> fp = new(), path = new(); List<Color> fc = new(); List<float> fs = new();
            for (long y = 0; y < x; y++)
            {
                long u = x - y, v = x + y, m = u * v;
                Vector3 p = FactorLift(o, u, v, R);
                path.Add(p); fp.Add(p);
                fc.Add(y == 0 ? SquareColor : u == 1 ? (ExpPrime(m) ? PrimeGold : NumberDim) : m == n ? Color.white : new Color(.5f, .7f, 1f));
                fs.Add(m == n ? dotScale * .9f : dotScale * .5f);
            }

            ExpPointCloud(g, $"factor points of {x}² − y²", fp, fc, fs, Factory.GlowIntensity);
            if (labelSize > 0f)
                ExpLabelWide(g, $"sphere x = {x}:  the points ({x} − y)({x} + y)", o + Vector3.back * (rad + labelSize * .03f), labelSize * .38f, new Color(.6f, .75f, 1f));
        }
    }

    // ================================================================================================
    // CD. spheres through the cubic lattice
    // ================================================================================================

    static int[] r3Counts;

    /// <summary>r₃(n), the number of ways to write n as a² + b² + c² (signs and order counted), for n up to nMax.</summary>
    static int[] ThreeSquares(int nMax)
    {
        if (r3Counts != null && r3Counts.Length > nMax) return r3Counts;
        int m = (int)System.Math.Sqrt(nMax);
        int[] r2 = new int[nMax + 1];
        for (int a = -m; a <= m; a++)
            for (int b = -m; b <= m; b++)
            {
                int s = a * a + b * b;
                if (s <= nMax) r2[s]++;
            }

        int[] r3 = new int[nMax + 1];
        for (int n = 0; n <= nMax; n++)
            for (int c = -(int)System.Math.Sqrt(n); c * c <= n; c++) r3[n] += r2[n - c * c];
        r3Counts = r3;
        return r3;
    }

    /// <summary>The class number of primitive positive definite forms of discriminant D &lt; 0 (reduced forms counted).</summary>
    static int ClassNumber(long D)
    {
        int h = 0;
        for (long a = 1; 3 * a * a <= -D; a++)
            for (long b = -a + 1; b <= a; b++)
            {
                if ((b * b - D) % (4 * a) != 0) continue;
                long c = (b * b - D) / (4 * a);
                if (c < a || (b < 0 && a == c)) continue;
                if (Gcd(Gcd(a, System.Math.Abs(b)), c) != 1) continue;
                h++;
            }

        return h;
    }

    static int DistinctPrimeFactors(long n)
    {
        int w = 0;
        for (long p = 2; p * p <= n; p++)
            if (n % p == 0) { w++; while (n % p == 0) n /= p; }
        return w + (n > 1 ? 1 : 0);
    }

    /// <summary>
    /// The spheres of radius √n through the cubic lattice, side by side and drawn at one size: each lattice point with
    /// a² + b² + c² = n as a dot (those on the equator, the Sacks ring of n, in white), the wire sphere, and r₃(n) with
    /// its class number underneath.
    /// </summary>
    public void ExpLatticeSpheres(Vector3 o, long[] ns, int columns, float spacing, float rowGap, float radius, float labelSize, string[] rowNames = null)
    {
        Transform g = ExpGroup("CD lattice spheres").transform;
        ExpEnsureSieve(ns.Length == 0 ? 10 : System.Linq.Enumerable.Max(ns) + 10);
        int rows = (ns.Length + columns - 1) / columns;
        for (int i = 0; i < ns.Length; i++)
        {
            long n = ns[i];
            int col = i % columns, row = i / columns;
            Vector3 c = o + new Vector3((col - (columns - 1) * .5f) * spacing, ((rows - 1) * .5f - row) * rowGap, 0f);
            if (col == 0 && rowNames != null && row < rowNames.Length && labelSize > 0f)
                ExpLabelWide(g, rowNames[row], c + new Vector3(-radius * 1.5f, 0f, 0f), labelSize * .42f, row == 0 ? PrimeGold : new Color(.6f, .78f, 1f), TextAlignmentOptions.Right);
            float unit = radius / Mathf.Sqrt(n);
            List<(Vector3, Vector3)> wire = new();
            for (int k = 0; k < 12; k++)
            {
                float a = k / 12f * Mathf.PI * 2f;
                for (int s = 0; s < 24; s++)
                {
                    float b0 = -Mathf.PI * .5f + s / 24f * Mathf.PI, b1 = -Mathf.PI * .5f + (s + 1) / 24f * Mathf.PI;
                    Vector3 P(float b) => c + new Vector3(radius * Mathf.Cos(b) * Mathf.Cos(a), radius * Mathf.Cos(b) * Mathf.Sin(a), -radius * Mathf.Sin(b));
                    wire.Add((P(b0), P(b1)));
                }
            }

            Segments(g, $"sphere {n}", wire, radius * .006f, YarnColor * .5f);
            ExpRing(g, $"equator {n}", c, radius, new Color(.8f, .85f, 1f) * .6f, radius * .008f, Factory.SoftGlowIntensity);
            List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
            int m = (int)System.Math.Sqrt(n), count = 0;
            bool prime = ExpPrime(n);
            for (int a = -m; a <= m; a++)
                for (int b = -m; b <= m; b++)
                {
                    long rest = n - a * a - b * b;
                    if (rest < 0) continue;
                    long cc = (long)System.Math.Round(System.Math.Sqrt(rest));
                    if (cc * cc != rest) continue;
                    foreach (long z in cc == 0 ? new[] { 0L } : new[] { cc, -cc })
                    {
                        pts.Add(c + new Vector3(a * unit, b * unit, -z * unit));
                        cols.Add(z == 0 ? Color.white : prime ? PrimeGold : new Color(.5f, .72f, 1f));
                        sizes.Add(radius * .045f);
                        count++;
                    }
                }

            if (pts.Count > 0) ExpPointCloud(g, $"lattice points of {n}", pts, cols, sizes, Factory.GlowIntensity);
            if (labelSize <= 0f) continue;
            string head = n.ToString();
            string factors = prime ? "prime" : string.Join(" · ", Factorize(n));
            string tail = n % 8 == 3 && IsSquareFree(n) ? $"{count} = 24 × {count / 24}:  h(−{n}) = {ClassNumber(-n)}" : n % 8 == 7 ? $"{count} points:  n ≡ 7 (mod 8)" : $"{count} points";
            ExpLabelWide(g, $"{head}  ({factors})", c + new Vector3(0f, -radius * 1.32f, 0f), labelSize * .5f, prime ? PrimeGold : new Color(.6f, .78f, 1f));
            ExpLabelWide(g, tail, c + new Vector3(0f, -radius * 1.62f, 0f), labelSize * .4f, new Color(.82f, .86f, .95f));
        }
    }

    static bool IsSquareFree(long n)
    {
        for (long p = 2; p * p <= n; p++) if (n % (p * p) == 0) return false;
        return true;
    }

    static List<long> Factorize(long n)
    {
        List<long> f = new();
        for (long p = 2; p * p <= n; p++) while (n % p == 0) { f.Add(p); n /= p; }
        if (n > 1) f.Add(n);
        return f;
    }

    /// <summary>
    /// r₃(n)/24 = h(−n) for every squarefree n ≡ 3 (mod 8) up to nMax, as a chart: primes in gold (always odd),
    /// composites coloured by their number of prime factors ω (always a multiple of 2^(ω−1)); the Heegner primes named.
    /// </summary>
    public void ExpThreeSquaresChart(Vector3 o, int nMax, float width, float height, int hShown, float labelSize)
    {
        Transform g = ExpGroup("CD three squares chart").transform;
        int[] r3 = ThreeSquares(nMax);
        ExpEnsureSieve(nMax + 10);
        Color[] byOmega = { Color.white, PrimeGold, new(.45f, .68f, 1f), new(.78f, .5f, 1f), new(1f, .4f, .4f), new(1f, .4f, .4f) };
        Vector3 At(double n, double h) => o + new Vector3((float)(n / nMax * width - width * .5f), (float)(h / hShown * height - height * .5f), 0f);
        List<(Vector3, Vector3)> grid = new();
        for (int h = 0; h <= hShown; h += 2) grid.Add((At(0, h), At(nMax, h)));
        Segments(g, "grid", grid, height * .002f, Factory.Palette.Grid);
        List<(Vector3, Vector3)> odd = new();
        for (int h = 1; h <= hShown; h += 2) odd.Add((At(0, h), At(nMax, h)));
        Segments(g, "odd lines", odd, height * .0016f, PrimeGold * .3f);
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        for (int n = 11; n <= nMax; n += 8)
        {
            if (!IsSquareFree(n)) continue;
            int h = r3[n] / 24, w = DistinctPrimeFactors(n);
            pts.Add(At(n, h) + Vector3.back * .2f); cols.Add(byOmega[System.Math.Min(w, 5)]); sizes.Add(height * (w == 1 ? .0075f : .006f));
        }

        ExpPointCloud(g, "h(-n)", pts, cols, sizes, Factory.GlowIntensity);
        if (labelSize <= 0f) return;
        Segments(g, "Heegner leader", new List<(Vector3, Vector3)> { (At(175, 1), At(nMax * .13, 1)) }, height * .003f, PrimeGold * .7f);
        ExpLabelWide(g, "h = 1:  11, 19, 43, 67, 163", At(nMax * .135, 1) + new Vector3(0f, 0f, -.3f), labelSize * .36f, PrimeGold, TextAlignmentOptions.Left);
        for (int h = 0; h <= hShown; h += 4)
            ExpLabelWide(g, h.ToString(), At(0, h) + new Vector3(-width * .012f, 0f, -.3f), labelSize * .34f, new Color(.7f, .72f, .8f), TextAlignmentOptions.Right);
        for (int n = 0; n <= nMax; n += 1000)
            ExpLabelWide(g, n.ToString("N0"), At(n, 0) + new Vector3(0f, -height * .06f, -.3f), labelSize * .34f, new Color(.7f, .72f, .8f));
        ExpLabelWide(g, "h(−n) = r<sub>3</sub>(n) / 24  for squarefree n ≡ 3 (mod 8)", At(0, hShown) + new Vector3(0f, height * .07f, -.3f), labelSize * .42f, Color.white, TextAlignmentOptions.Left);
        ExpLabelWide(g, "gold: n prime, h odd      blue: two prime factors, 2 | h      violet: three, 4 | h      red: four, 8 | h",
            At(nMax * .5, 0) + new Vector3(0f, -height * .13f, -.3f), labelSize * .36f, new Color(.82f, .86f, .95f));
    }

    // ================================================================================================
    // CE. the cube ball flattened
    // ================================================================================================

    /// <summary>
    /// The cube yarn ball (BB) seen straight down its axis: every n below kMax³ at (ρ sin ψ cos φ, ρ sin ψ sin φ), with
    /// dots sized by d(n) as in CA; both poles fall on the centre. Overlaid: the north-pole strands k³ + k and k³ − k (red),
    /// the two integers beside the south pole, k³ + 3k(k + 1)/2 and + 1 (orange), and the corner rays. With labelAt > 0 the
    /// labels are laid out for a close-up of the centre.
    /// </summary>
    public void ExpCubeFlat(Vector3 o, int kMax, float R, float dotScale, float labelSize, float labelAt = 0f)
    {
        Transform g = ExpGroup("CE cube flat").transform;
        int N = kMax * kMax * kMax;
        int[] d = DivisorCounts(N);
        ExpEnsureSieve(N + 10);
        Vector3 Flat(double n)
        {
            Vector3 p = CubeBallAt(Vector3.zero, n, R); // world: x, y = polar axis, z
            return o + new Vector3(p.x, p.z, 0f);
        }

        List<Vector3> pts = new(N); List<Color> cols = new(N); List<float> sizes = new(N);
        for (int n = 2; n < N; n++)
        {
            pts.Add(Flat(n)); cols.Add(DivisorTint(d[n])); sizes.Add(DivisorDot(d[n], dotScale));
        }

        ExpPointCloud(g, "flattened ball", pts, cols, sizes, Factory.SoftGlowIntensity);
        void Overlay(string name, System.Func<long, long> f, long k0, Color col, long step = 1)
        {
            List<Vector3> path = new(), dots = new(); List<Color> dc = new(); List<float> ds = new();
            for (long k = k0; k < kMax; k += step)
            {
                long n = f(k);
                if (n < 2 || n >= N) continue;
                Vector3 p = Flat(n) + Vector3.back * .3f;
                path.Add(p); dots.Add(p); dc.Add(ExpPrime(n) ? PrimeGold : col); ds.Add(dotScale * 2.2f);
            }

            ExpPointCloud(g, name + " values", dots, dc, ds, Factory.GlowIntensity);
        }

        Overlay("k³ + k", k => k * k * k + k, 2, new Color(1f, .4f, .35f));
        Overlay("(k − 1)k(k + 1)", k => k * k * k - k, 2, new Color(1f, .4f, .35f));
        Overlay("k(2k² + 3k + 3)/2", k => k * k * k + 3 * k * (k + 1) / 2, 2, HelixOrange);
        Overlay("(k + 1)(2k² + k + 2)/2", k => k * k * k + 3 * k * (k + 1) / 2 + 1, 2, HelixOrange);
        float rim = R * kMax;
        List<(Vector3, Vector3)> rays = new();
        foreach (float deg in new[] { 120f, 240f })
        {
            float a = deg * Mathf.Deg2Rad;
            rays.Add((o, o + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * rim * .943f));
        }

        Segments(g, "corner rays", rays, dotScale * .1f, PronicColor * .7f, Factory.SoftGlowIntensity);
        if (labelSize <= 0f) return;
        if (labelAt > 0f)
        {
            // the close-up: the north-pole strands, and the ring of the south pole's two neighbours
            Vector3 strand = Flat(30L * 30 * 30 + 30) + Vector3.back * .3f;
            ExpLabelWide(g, "north pole:  k³ + k  and  (k − 1)k(k + 1)", strand + new Vector3(0f, labelAt * .3f, 0f), labelSize * .45f, new Color(1f, .55f, .5f));
            float ring = 2f * R / Mathf.Sqrt(3f);
            Vector3 at = o + new Vector3(-ring * .7f, -ring * .7f, -.3f), lab = o + new Vector3(-labelAt * .45f, -labelAt * .55f, -.3f);
            ExpLine(g, "south leader", dotScale * .5f, HelixOrange * .7f, Factory.NoGlow, new[] { at, lab + new Vector3(labelAt * .02f, labelAt * .03f, 0f) });
            ExpLabelWide(g, "south pole's neighbours  (k³ + (k + 1)³ ± 1)/2:", lab, labelSize * .42f, HelixOrange, TextAlignmentOptions.Right);
            ExpLabelWide(g, "composite, on a ring of radius 2R/√3", lab + new Vector3(0f, -labelSize * .05f, 0f), labelSize * .42f, HelixOrange, TextAlignmentOptions.Right);
            return;
        }

        ExpLabelWide(g, "north-pole families  →", o + new Vector3(rim * 1.04f, rim * .1f, 0f), labelSize * .45f, new Color(1f, .55f, .5f), TextAlignmentOptions.Left);
        ExpLabelWide(g, "corner 1", o + new Vector3(-.5f, .866f, 0f) * rim * 1.04f, labelSize * .4f, PronicColor);
        ExpLabelWide(g, "corner 2", o + new Vector3(-.5f, -.866f, 0f) * rim * 1.04f, labelSize * .4f, PronicColor);
    }
}
