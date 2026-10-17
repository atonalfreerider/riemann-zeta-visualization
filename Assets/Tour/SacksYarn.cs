using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Sections BA–BD (docs/experiments/README.md): the Sacks spiral spooled in 3D, like a ball of yarn.
///
/// BA. The Sacks yarn ball: every turn of the spiral (k² to (k + 1)²) is one wrap, a loop in a plane through the
///     squares axis, turned about that axis by a fixed angle per turn. The yarn is continuous (each wrap starts and ends
///     on the axis, at a square). The squares stay on the axis and the pronic numbers hug its other end. A quadratic
///     family with discriminant Δ keeps its flat offset πR|Δ|/4 from the axis, so it winds round it as a helix of that
///     radius: k² + m and k² − m share one cylinder as a double helix, and when m is a square the second strand is the
///     cancellation curve (k − √m)(k + √m).
/// BB. The cube yarn ball: the counting numbers fill a ball at even density. Layer k (k³ to (k + 1)³, 3k² + 3k + 1
///     numbers) is a sphere of radius R∛n wound as a spherical spiral from the north pole to the south pole and back,
///     3k + 1 times round. Cubic families k³ + Ak² + Bk + C converge on three corners of a tetrahedron by A mod 3 (the
///     pole and two corners at latitude −1/3), as parallel strands whose offsets are affine in (B, C): seen down a corner,
///     the ball is a comb of spots, one per strand.
/// BC. The comb at corner 1 as a chart: strands with an integer root (always composite) fall on straight lines, the
///     strands with no small prime factor shine; k²(k + 1) + 17 is the richest, as Euler's line k(k + 1) + 41 is in 2D.
/// BD. One shell: near a corner its windings are the comb's rows, and the multiples of k₀ + r cross them as straight
///     lines, one slope per r, meeting at highly composite numbers.
/// </summary>
public partial class SacksSpiral
{
    public const float SacksBallOffset = 16000f;
    public const float CubeBallOffset = 16800f;
    public const float CubeShellOffset = 17600f;
    public const float CubeCombOffset = 18400f;
    public const float CubeCornerOffset = 19200f;
    public const float CubeEulerOffset = 20000f;

    static readonly Color YarnColor = new(.42f, .46f, .62f);
    static readonly Color PrimeGold = new(1f, .8f, .32f);
    static readonly Color NumberDim = new(.26f, .26f, .33f);
    static readonly Color CornerTwoColor = new(.75f, .5f, 1f);
    static readonly Color HelixOrange = new(1f, .55f, .2f);

    /// <summary>A circle about <paramref name="normal"/> (ExpRing only draws in the screen plane).</summary>
    LineRenderer RingAround(Transform g, string name, Vector3 center, Vector3 normal, float radius, Color col, float width, float glow)
    {
        Vector3 a = Vector3.Cross(normal, Mathf.Abs(normal.y) < .9f ? Vector3.up : Vector3.right).normalized, b = Vector3.Cross(normal, a).normalized;
        Vector3[] pts = new Vector3[40];
        for (int i = 0; i < pts.Length; i++)
        {
            float t = i / (float)pts.Length * Mathf.PI * 2f;
            pts[i] = center + (a * Mathf.Cos(t) + b * Mathf.Sin(t)) * radius;
        }

        return ExpLine(g, name, width, col, glow, pts, true);
    }

    /// <summary>
    /// The values f(k0) … f(k1) of one family as dots (primes gold, squares red, others <paramref name="other"/>),
    /// joined by a line of colour <paramref name="col"/> when <paramref name="join"/>.
    /// </summary>
    void Strand(Transform g, string name, System.Func<long, long> f, long k0, long k1, System.Func<double, Vector3> at, Color col, float dot, float width, bool join = true, Color? other = null, Color? prime = null)
    {
        List<Vector3> pts = new(), path = new(); List<Color> cols = new(); List<float> sizes = new();
        for (long k = k0; k <= k1; k++)
        {
            long n = f(k);
            if (n < 1) continue;
            ExpEnsureSieve(n + 1);
            Vector3 p = at(n);
            path.Add(p);
            pts.Add(p);
            bool pr = ExpPrime(n);
            cols.Add(pr ? prime ?? PrimeGold : IsSquareLong(n) ? SquareColor : other ?? DeadColor);
            sizes.Add(pr ? dot * 1.15f : dot);
        }

        if (join && path.Count > 1) ExpLine(g, name, width, col, Factory.SoftGlowIntensity, path.ToArray());
        if (pts.Count > 0) ExpPointCloud(g, name + " values", pts, cols, sizes, Factory.GlowIntensity);
    }

    // ================================================================================================
    // BA. the Sacks yarn ball
    // ================================================================================================

    /// <summary>The 2D Sacks point of n, its plane turned by alpha·k (radians) about the squares axis (x): one wrap per turn.</summary>
    static Vector3 SacksBallAt(Vector3 o, double n, float R, float alpha)
    {
        double s = System.Math.Sqrt(System.Math.Max(0.0, n));
        long k = (long)System.Math.Floor(s);
        double th = 2 * System.Math.PI * (s - k), r = R * s, b = alpha * k;
        double x = r * System.Math.Cos(th), y = r * System.Math.Sin(th);
        return o + new Vector3((float)x, (float)(y * System.Math.Cos(b)), (float)(y * System.Math.Sin(b)));
    }

    /// <summary>
    /// The Sacks yarn ball for the first <paramref name="turns"/> turns: the yarn wrap by wrap, the numbers (primes gold),
    /// the squares on the axis, the pronic numbers at its other end, the double helix k² + 1 / k² − 1 and Euler's k² + k + 41.
    /// </summary>
    public void ExpSacksYarnBall(Vector3 o, int turns, float R, float alphaDeg, float labelSize)
    {
        Transform g = ExpGroup("BA Sacks yarn ball").transform;
        float alpha = alphaDeg * Mathf.Deg2Rad;
        System.Func<double, Vector3> at = n => SacksBallAt(o, n, R, alpha);
        long nMax = (long)turns * turns;
        ExpEnsureSieve(nMax + 2000);
        for (int k = 1; k < turns; k++)
        {
            int segs = 6 * (2 * k + 1);
            Vector3[] wrap = new Vector3[segs + 1];
            for (int s = 0; s <= segs; s++) wrap[s] = at(k * k + (2.0 * k + 1) * s / segs);
            ExpLine(g, "wrap " + k, .05f, YarnColor * .7f, Factory.NoGlow, wrap);
        }

        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        for (long n = 2; n < nMax; n++)
        {
            bool p = ExpPrime(n);
            pts.Add(at(n)); cols.Add(p ? PrimeGold * .9f : NumberDim); sizes.Add(p ? .32f : .16f);
        }

        ExpPointCloud(g, "numbers", pts, cols, sizes, Factory.SoftGlowIntensity);
        float reach = R * turns + 4f;
        ExpLine(g, "squares axis", .12f, SquareColor * .8f, Factory.SoftGlowIntensity, new[] { o, o + Vector3.right * reach });
        ExpLine(g, "pronic axis", .12f, PronicColor * .8f, Factory.SoftGlowIntensity, new[] { o, o + Vector3.left * reach });
        Strand(g, "squares", k => k * k, 1, turns - 1, at, SquareColor, .6f, .12f);
        Strand(g, "pronic", k => k * (k + 1), 1, turns - 1, at, PronicColor, .6f, .12f, true, PronicColor);
        Strand(g, "k² + 1", k => k * k + 1, 1, turns - 1, at, HelixOrange, .55f, .14f, true, NumberDim);
        Strand(g, "k² − 1", k => k * k - 1, 2, turns - 1, at, DeadColor, .55f, .14f);
        Strand(g, "Euler", k => k * k + k + 41, 0, turns - 1, at, EulerColor, .9f, .16f, false, EulerColor * .45f, EulerColor);
        if (labelSize > 0f)
        {
            ExpLabelWide(g, "squares", o + Vector3.right * (reach + 3f), labelSize * .6f, SquareColor, TextAlignmentOptions.Left);
            ExpLabelWide(g, "pronic numbers", o + Vector3.left * (reach + 3f), labelSize * .6f, PronicColor, TextAlignmentOptions.Right);
            ExpLabelWide(g, "k² + 1 and k² − 1", o + new Vector3(R * turns - 6f, 9f, 0f), labelSize * .5f, HelixOrange, TextAlignmentOptions.Left);
            ExpLabelWide(g, "Euler's k² + k + 41: scattered", o + new Vector3(R * turns * .1f, -R * turns * .62f, 0f), labelSize * .5f, EulerColor);
        }

        Board("the Sacks spiral wound into a ball\n" + Small($"each turn is one wrap, turned {alphaDeg:0}° about the squares axis: gold = primes"));
    }

    /// <summary>
    /// The squares end of the Sacks yarn ball without the yarn: the squares on the axis and, for each m, the cylinder of
    /// radius πRm with its two helices, k² + m (orange) and k² − m (red when m is a square: the cancellation curve).
    /// </summary>
    public void ExpSacksHelices(Vector3 o, int turns, float R, float alphaDeg, int[] ms, float labelSize)
    {
        Transform g = ExpGroup("BA double helices").transform;
        float alpha = alphaDeg * Mathf.Deg2Rad;
        System.Func<double, Vector3> at = n => SacksBallAt(o, n, R, alpha);
        long nMax = (long)turns * turns + 4000;
        ExpEnsureSieve(nMax);
        float reach = R * turns + 2f;
        ExpLine(g, "squares axis", .14f, SquareColor * .8f, Factory.SoftGlowIntensity, new[] { o, o + Vector3.right * reach });
        Strand(g, "squares", k => k * k, 1, turns - 1, at, SquareColor, .6f, .1f, false);
        Color[] pos = { HelixOrange, new(1f, .78f, .3f), new(.95f, .9f, .45f) };
        for (int i = 0; i < ms.Length; i++)
        {
            int m = ms[i];
            float rad = Mathf.PI * R * m;
            bool square = IsSquareLong(m);
            Color plus = pos[i % pos.Length], minus = square ? DeadColor : new Color(.4f, .75f, 1f);
            for (int k = 6; k < turns; k += 6)
            {
                Vector3[] ring = new Vector3[48];
                for (int s = 0; s < ring.Length; s++)
                {
                    float a = s / (float)ring.Length * Mathf.PI * 2f;
                    ring[s] = o + new Vector3(R * k, rad * Mathf.Cos(a), rad * Mathf.Sin(a));
                }

                ExpLine(g, $"cylinder {m} ring {k}", .04f, YarnColor * .6f, Factory.NoGlow, ring, true);
            }

            int mm = m;
            int k0 = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(m)) + 1);
            Strand(g, $"k² + {m}", k => k * k + mm, k0, turns - 1, at, plus, .5f, .14f, true, NumberDim);
            Strand(g, $"k² − {m}", k => k * k - mm, k0, turns - 1, at, minus, .5f, .14f, true, square ? DeadColor : NumberDim);
            if (labelSize > 0f)
            {
                long ke = turns - 1;
                string minusText = square ? $"k² − {m} = (k − {Mathf.RoundToInt(Mathf.Sqrt(m))})(k + {Mathf.RoundToInt(Mathf.Sqrt(m))})" : $"k² − {m}";
                float lx = R * turns + 4f, ly = rad + 2.5f + 3.5f * i;
                ExpLabelWide(g, $"k² + {m}", o + new Vector3(lx, ly, 0f), labelSize * .5f, plus, TextAlignmentOptions.Left);
                ExpLabelWide(g, minusText, o + new Vector3(lx, -ly, 0f), labelSize * .5f, minus, TextAlignmentOptions.Left);
            }
        }

        if (labelSize > 0f) ExpLabelWide(g, "squares", o + Vector3.right * (reach + 2f), labelSize * .5f, SquareColor, TextAlignmentOptions.Left);
        Board("double helices round the squares axis\n" + Small("k² + m and k² − m wind round one cylinder of radius πRm:  red = (k − 1)(k + 1)"));
    }

    // ================================================================================================
    // BB–BD. the cube yarn ball
    // ================================================================================================

    static long ICbrt(long n)
    {
        long k = (long)System.Math.Round(System.Math.Pow(System.Math.Max(0, n), 1.0 / 3));
        while (k * k * k > n) k--;
        while ((k + 1) * (k + 1) * (k + 1) <= n) k++;
        return k;
    }

    /// <summary>
    /// n's point in the cube yarn ball (polar axis = world up): layer k = ⌊∛n⌋ at radius R∛n; t = (n − k³)/(3k² + 3k + 1)
    /// runs down from the north pole and back up (z = 1 − 4t, then −3 + 4t, even area) while winding 3k + 1 times.
    /// </summary>
    static Vector3 CubeBallAt(Vector3 o, double n, float R)
    {
        long k = ICbrt((long)System.Math.Floor(n));
        double L = 3.0 * k * k + 3.0 * k + 1, t = (n - (double)k * k * k) / L;
        double z = t < .5 ? 1 - 4 * t : -3 + 4 * t, sx = System.Math.Sqrt(System.Math.Max(0, 1 - z * z));
        double phi = 2 * System.Math.PI * (3 * k + 1) * t, rho = R * System.Math.Pow(n, 1.0 / 3);
        return o + new Vector3((float)(rho * sx * System.Math.Cos(phi)), (float)(rho * z), (float)(rho * sx * System.Math.Sin(phi)));
    }

    /// <summary>The cube pole and the two corners the cubic families converge on (world directions), and the unused fourth corner.</summary>
    public static readonly Vector3 CubePole = Vector3.up;
    public static readonly Vector3 CubeCorner1 = new(-.4714045f, -1f / 3f, .8164966f);
    public static readonly Vector3 CubeCorner2 = new(-.4714045f, -1f / 3f, -.8164966f);
    public static readonly Vector3 CubeCorner3 = new(.9428090f, -1f / 3f, 0f);

    /// <summary>Screen-up for a camera looking down corner 1: the polar axis with its corner-1 component removed.</summary>
    public static Vector3 CubeCorner1Up => (Vector3.up + CubeCorner1 / 3f).normalized;

    /// <summary>
    /// The cube yarn ball for layers below <paramref name="kMax"/>: the outer layer's yarn, the primes, the corner axes,
    /// the cubes on the pole, the 3D pronic numbers (k − 1)k(k + 1), k²(k + 1), k(k + 1)², the richest cubic at each
    /// corner (gold) and Euler's quadratic (green, scattered).
    /// </summary>
    public void ExpCubeYarnBall(Vector3 o, int kMax, float R, float labelSize, bool euler = true)
    {
        Transform g = ExpGroup("BB cube yarn ball").transform;
        System.Func<double, Vector3> at = n => CubeBallAt(o, n, R);
        long nMax = (long)kMax * kMax * kMax;
        ExpEnsureSieve(nMax + 5000);
        int kOut = kMax - 1;
        long a = (long)kOut * kOut * kOut, L = 3L * kOut * kOut + 3L * kOut + 1;
        int segs = (int)System.Math.Min(9000L, L * 2);
        Vector3[] yarn = new Vector3[segs + 1];
        for (int s = 0; s <= segs; s++) yarn[s] = at(a + (L - 1e-6) * s / segs);
        ExpLine(g, "yarn, outer layer", .045f, YarnColor * .75f, Factory.NoGlow, yarn);
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        for (long n = 2; n < nMax; n++)
        {
            if (!ExpPrime(n)) continue;
            pts.Add(at(n)); cols.Add(PrimeGold * .85f); sizes.Add(.2f);
        }

        ExpPointCloud(g, "primes", pts, cols, sizes, Factory.SoftGlowIntensity);
        float reach = R * kMax + 6f;
        ExpLine(g, "cube axis", .12f, SquareColor * .8f, Factory.SoftGlowIntensity, new[] { o, o + CubePole * reach });
        ExpLine(g, "corner 1", .12f, PronicColor * .9f, Factory.SoftGlowIntensity, new[] { o, o + CubeCorner1 * reach });
        ExpLine(g, "corner 2", .12f, CornerTwoColor, Factory.SoftGlowIntensity, new[] { o, o + CubeCorner2 * reach });
        ExpLine(g, "corner 3 (empty)", .06f, new Color(.4f, .4f, .48f), Factory.NoGlow, new[] { o, o + CubeCorner3 * reach });
        long k1 = kMax - 1;
        Strand(g, "cubes", k => k * k * k, 1, k1, at, SquareColor, .6f, .1f, false);
        Strand(g, "(k−1)k(k+1)", k => k * k * k - k, 2, k1, at, DeadColor, .5f, .1f);
        Strand(g, "k²(k+1)", k => k * k * (k + 1), 1, k1, at, PronicColor, .6f, .12f, true, PronicColor);
        Strand(g, "k(k+1)²", k => k * (k + 1) * (k + 1), 1, k1 - 1, at, CornerTwoColor, .6f, .12f, true, CornerTwoColor);
        Strand(g, "k²(k+1) + 17", k => k * k * (k + 1) + 17, 0, k1, at, PrimeGold, .7f, .16f, true, NumberDim);
        Strand(g, "k(k+1)² + 13", k => k * (k + 1) * (k + 1) + 13, 0, k1 - 1, at, PrimeGold * .9f, .65f, .14f, true, NumberDim);
        if (euler)
        {
            int kE = (int)System.Math.Floor((-1 + System.Math.Sqrt(1 + 4.0 * (nMax - 41))) / 2);
            Strand(g, "Euler's quadratic", k => k * k + k + 41, 0, kE, at, EulerColor, .6f, .08f, false, EulerColor * .45f, EulerColor);
        }

        if (labelSize > 0f)
        {
            ExpLabelWide(g, "cubes  k³", o + CubePole * (reach + 4f), labelSize * .55f, SquareColor);
            ExpLabelWide(g, "k²(k + 1)", o + CubeCorner1 * (reach + 5f), labelSize * .55f, PronicColor);
            ExpLabelWide(g, "k(k + 1)²", o + CubeCorner2 * (reach + 5f), labelSize * .55f, CornerTwoColor);
            ExpLabelWide(g, "empty corner", o + CubeCorner3 * (reach + 5f), labelSize * .45f, new Color(.55f, .55f, .62f));
            Vector3 e = at(k1 * k1 * (k1 + 1) + 17);
            ExpLabelWide(g, "k²(k + 1) + 17", e + new Vector3(0f, -5f, 0f), labelSize * .5f, PrimeGold);
            ExpLabelWide(g, "(k − 1)k(k + 1)", o + CubePole * (reach * .55f) + new Vector3(0f, 0f, -9f), labelSize * .42f, new Color(1f, .5f, .45f), TextAlignmentOptions.Left);
            ExpLabelWide(g, "Euler's k² + k + 41: scattered", o + new Vector3(0f, -reach * .82f, reach * .3f), labelSize * .42f, EulerColor);
        }

        Board("the cube yarn ball\n" + Small("layer k holds k³ … (k + 1)³, wound pole to pole and back, 3k + 1 times round:  gold = primes,  green = Euler's k² + k + 41"));
    }

    /// <summary>
    /// The corner cone (latitude −1/3) seen from below, out to layer <paramref name="kMax"/>: the corner rays, the 3D pronic
    /// strands k²(k + 1) and k(k + 1)² running straight out along them, and the richest cubics beside them, which curl in
    /// from the centre and straighten into parallel lines, as Euler's line does beside the pronic ray of the flat spiral.
    /// </summary>
    public void ExpCubeEulerLines(Vector3 o, float R, int kMax, float labelSize)
    {
        Transform g = ExpGroup("BB Euler lines").transform;
        System.Func<double, Vector3> at = n => CubeBallAt(o, n, R);
        ExpEnsureSieve((long)kMax * kMax * kMax + 3L * kMax * kMax + 50L * kMax + 100);
        float reach = R * kMax;
        ExpLine(g, "corner 1", .25f, PronicColor * .7f, Factory.SoftGlowIntensity, new[] { o, o + CubeCorner1 * reach * 1.04f });
        ExpLine(g, "corner 2", .25f, CornerTwoColor * .7f, Factory.SoftGlowIntensity, new[] { o, o + CubeCorner2 * reach * 1.04f });
        for (int k = kMax / 4; k <= kMax; k += kMax / 4)
        {
            Vector3[] ring = new Vector3[120];
            float rho = R * k;
            for (int s = 0; s < ring.Length; s++)
            {
                float a = s / (float)ring.Length * Mathf.PI * 2f;
                ring[s] = o + new Vector3(rho * .9428f * Mathf.Cos(a), -rho / 3f, rho * .9428f * Mathf.Sin(a));
            }

            ExpLine(g, $"cone ring {k}", .12f, YarnColor * .55f, Factory.NoGlow, ring, true);
            if (labelSize > 0f && k >= kMax / 2) ExpLabelWide(g, $"layer {k}", o + new Vector3(-rho * .9428f - 3f, -rho / 3f, 0f), labelSize * .32f, YarnColor);
        }

        long k1 = kMax;
        Strand(g, "k²(k+1)", k => k * k * (k + 1), 1, k1, at, PronicColor, .9f, .3f, true, PronicColor * .8f);
        Strand(g, "k(k+1)²", k => k * (k + 1) * (k + 1), 1, k1, at, CornerTwoColor, .9f, .3f, true, CornerTwoColor * .8f);
        Strand(g, "k²(k+1) + 17", k => k * k * (k + 1) + 17, 0, k1, at, PrimeGold, 1.1f, .3f, true, NumberDim);
        Strand(g, "k²(k+1) − 13", k => k * k * (k + 1) - 13, 3, k1, at, HelixOrange, 1f, .26f, true, NumberDim);
        Strand(g, "k(k+1)² + 13", k => k * (k + 1) * (k + 1) + 13, 0, k1, at, new Color(1f, .9f, .55f), 1f, .26f, true, NumberDim);
        Strand(g, "k(k+1)² − 17", k => k * (k + 1) * (k + 1) - 17, 3, k1, at, new Color(1f, .62f, .5f), 1f, .26f, true, NumberDim);
        if (labelSize > 0f)
        {
            Vector3 down = Vector3.down * 4f;
            ExpLabelWide(g, "k²(k + 1)", at(k1 * k1 * (k1 + 1)) + CubeCorner1 * 9f + down, labelSize * .5f, PronicColor);
            ExpLabelWide(g, "k(k + 1)²", at(k1 * (k1 + 1) * (k1 + 1)) + CubeCorner2 * 9f + down, labelSize * .5f, CornerTwoColor);
            ExpLabelWide(g, "k²(k + 1) + 17", at(k1 * k1 * (k1 + 1) + 17) + CubeCorner1 * 9f + down, labelSize * .5f, PrimeGold);
            ExpLabelWide(g, "k²(k + 1) − 13", at(k1 * k1 * (k1 + 1) - 13) + CubeCorner1 * 9f + down, labelSize * .45f, HelixOrange);
            ExpLabelWide(g, "k(k + 1)² + 13", at(k1 * (k1 + 1) * (k1 + 1) + 13) + CubeCorner2 * 9f + down, labelSize * .45f, new Color(1f, .9f, .55f));
            ExpLabelWide(g, "k(k + 1)² − 17", at(k1 * (k1 + 1) * (k1 + 1) - 17) + CubeCorner2 * 9f + down, labelSize * .45f, new Color(1f, .62f, .5f));
        }

        Board("Euler's lines of the cube ball\n" + Small("the corner cone from below: the 3D pronic strands run out along the corners, the richest cubics straighten beside them"));
    }

    /// <summary>
    /// The ball seen down corner 1: every number of layers k0 … k1 on the down pass within W of the corner axis (primes
    /// gold), so each strand k³ + k² + Bk + C shows end-on as one spot; a few strands ringed and named.
    /// </summary>
    public void ExpCubeCorner(Vector3 o, float R, int k0, int k1, float W, float labelSize)
    {
        Transform g = ExpGroup("BB cube corner").transform;
        Vector3 u1 = CubeCorner1, up = CubeCorner1Up, right = Vector3.Cross(u1, up).normalized; // screen right for a camera looking along −u1
        ExpEnsureSieve((long)(k1 + 1) * (k1 + 1) * (k1 + 1) + 10);
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        for (long k = k0; k <= k1; k++)
        {
            long a = k * k * k, L = 3 * k * k + 3 * k + 1;
            for (long n = a + L / 8; n < a + L / 2; n++)
            {
                Vector3 p = CubeBallAt(Vector3.zero, n, R);
                float d = Vector3.Dot(p, u1);
                if (d <= 0f || (p - d * u1).sqrMagnitude > W * W) continue;
                bool pr = ExpPrime(n);
                pts.Add(o + p); cols.Add(pr ? PrimeGold : NumberDim * .8f); sizes.Add(pr ? .3f : .1f);
            }
        }

        ExpPointCloud(g, "numbers", pts, cols, sizes, Factory.GlowIntensity);
        (long B, long C, string text, Color col, Vector2 off)[] marks =
        {
            (0, 0, "k²(k + 1)", new Color(1f, .45f, .4f), new Vector2(3.6f, 2.6f)),
            (0, -1, "k²(k + 1) − 1", PrimeGold, new Vector2(-5.5f, 2.6f)),
            (0, 1, "k²(k + 1) + 1", PrimeGold, new Vector2(4f, -2.8f)),
            (1, 0, "k(k² + k + 1)", new Color(1f, .45f, .4f), new Vector2(-4.5f, -2.9f)),
        };
        Vector3 axis = o + u1 * (R * k1 + 4f);
        ExpLine(g, "axis h", .1f, Color.white * .8f, Factory.SoftGlowIntensity, new[] { axis - right * .9f, axis + right * .9f });
        ExpLine(g, "axis v", .1f, Color.white * .8f, Factory.SoftGlowIntensity, new[] { axis - up * .9f, axis + up * .9f });
        foreach (var m in marks)
        {
            long k = k1;
            Vector3 s = o + CubeBallAt(Vector3.zero, k * k * k + k * k + m.B * k + m.C, R) + u1 * 2f;
            RingAround(g, m.text, s, u1, 1.5f, m.col, .12f, Factory.GlowIntensity);
            if (labelSize > 0f) ExpLabelWide(g, m.text, s + up * m.off.y + right * m.off.x, labelSize * .5f, m.col);
        }

        Board("the cube ball seen down corner 1\n" + Small($"layers {k0} to {k1}: each spot is one strand k³ + k² + Bk + C seen end-on;  gold = primes"));
    }

    /// <summary>
    /// The comb at corner 1: every strand k³ + k² + Bk + C (|B| ≤ bMax, |C| ≤ cMax) at its asymptotic place
    /// u = C − (2B − 1)/3 across, v = B − 1 down (true spacing 2πR·√8/3 across and R√2 down; here su and sv), sized and
    /// coloured by its prime yield over k ≤ 120; the strands with an integer root (always composite) red, the always-even
    /// ones dark; the line of each root k = −r drawn through its strands; k²(k + 1) and k²(k + 1) + 17 ringed.
    /// </summary>
    public void ExpCubeComb(Vector3 o, float su, float sv, int bMax, int cMax, float labelSize)
    {
        Transform g = ExpGroup("BC cube comb").transform;
        Vector3 At(double B, double C) => o + new Vector3((float)((C - (2 * B - 1) / 3.0) * su), (float)(-(B - 1) * sv), 0f);
        ExpEnsureSieve(122L * 122 * 122 + 2000);
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        for (int B = -bMax; B <= bMax; B++)
            for (int C = -cMax; C <= cMax; C++)
            {
                bool root = C == 0;
                for (long r = 1; !root && r <= System.Math.Abs(C); r++)
                    if (C % r == 0 && (r * r * r + r * r + B * r + C == 0 || -r * r * r + r * r - B * r + C == 0)) root = true;
                bool even = B % 2 == 0 && C % 2 == 0;
                double hits = 0, expect = 0;
                if (!root && !even)
                    for (long k = 1; k <= 120; k++)
                    {
                        long v = k * k * k + k * k + B * k + C;
                        if (v < 3) continue;
                        if (ExpPrime(v)) hits++;
                        expect += 1.0 / System.Math.Log(v);
                    }

                double y = expect > 0 ? hits / expect : 0;
                pts.Add(At(B, C));
                cols.Add(root ? DeadColor : even ? new Color(.2f, .2f, .25f) : YieldColor(y));
                sizes.Add(root ? .5f : even ? .22f : .26f + .16f * (float)y);
            }

        ExpPointCloud(g, "strands", pts, cols, sizes, Factory.GlowIntensity);
        for (int r = -3; r <= 3; r++)
        {
            // the root k = −r: C = r³ − r² + rB, one straight line through the comb
            double b0 = -bMax - .4, b1 = bMax + .4;
            double c0 = r * r * r - r * r + r * b0, c1 = r * r * r - r * r + r * b1;
            if (r != 0)
            {
                // clip to the drawn C range
                double bl = (-cMax - .4 - (r * r * r - r * r)) / r, bh = (cMax + .4 - (r * r * r - r * r)) / r;
                double lo = System.Math.Max(b0, System.Math.Min(bl, bh)), hi = System.Math.Min(b1, System.Math.Max(bl, bh));
                if (lo >= hi) continue;
                b0 = lo; b1 = hi; c0 = r * r * r - r * r + r * b0; c1 = r * r * r - r * r + r * b1;
            }

            Vector3 p0 = At(b0, c0) + Vector3.forward * .3f, p1 = At(b1, c1) + Vector3.forward * .3f;
            ExpLine(g, $"root {-r}", .1f, DeadColor * .75f, Factory.SoftGlowIntensity, new[] { p0, p1 });
            if (labelSize > 0f) ExpLabelWide(g, $"k = {-r}", p1 + (p1 - p0).normalized * 2.6f + new Vector3(0f, 0f, -.3f), labelSize * .4f, DeadColor);
        }

        ExpRing(g, "k²(k+1)", At(0, 0) + Vector3.back * .2f, 1.1f, Color.white, .1f, Factory.GlowIntensity);
        ExpRing(g, "k²(k+1) + 17", At(0, 17) + Vector3.back * .2f, 1.4f, Color.white, .14f, AttentionIntensity);
        if (labelSize > 0f)
        {
            ExpLabelWide(g, "k²(k + 1)", At(0, 0) + new Vector3(0f, 2.3f, -.3f), labelSize * .45f, Color.white);
            ExpLabelWide(g, "k²(k + 1) + 17", At(0, 17) + new Vector3(0f, 2.6f, -.3f), labelSize * .5f, Color.white);
            for (int B = -bMax; B <= bMax; B += 2)
                ExpLabelWide(g, $"B = {B}", At(B, -cMax) + new Vector3(-3.5f, 0f, -.3f), labelSize * .36f, new Color(.75f, .75f, .85f), TextAlignmentOptions.Right);
            ExpLabelWide(g, "C →", At(bMax, cMax) + new Vector3(4f, 0f, -.3f), labelSize * .36f, new Color(.75f, .75f, .85f), TextAlignmentOptions.Left);
            ExpLabelWide(g, "red lines: the strands with an integer root k  (always composite)      gold: no small root, larger = more primes for k ≤ 120      dark: always even",
                At(bMax, 0) + new Vector3(0f, -9f, -.3f), labelSize * .36f, new Color(.75f, .75f, .85f));
        }

        Board("the comb at corner 1\n" + Small("every strand k³ + k² + Bk + C at its place:  brighter = more primes,  red = an integer root (always composite)"));
    }

    /// <summary>
    /// One layer (the shell of k₀) around corner 1, down pass only: its yarn and numbers within <paramref name="radius"/>
    /// of the corner, the lines through the multiples of k₀ + r (r from −2 to 3) crossing the windings, and rings at
    /// k₀²(k₀ + 1), k₀²(k₀ + 1) + 17 and the numbers on three or more lines.
    /// </summary>
    public void ExpCubeShell(Vector3 o, int k0, float R, float radius, float labelSize)
    {
        Transform g = ExpGroup("BD cube shell").transform;
        System.Func<double, Vector3> at = n => CubeBallAt(o, n, R);
        long a = (long)k0 * k0 * k0, L = 3L * k0 * k0 + 3L * k0 + 1;
        ExpEnsureSieve(a + L + 10);
        Vector3 corner = o + CubeCorner1 * (R * Mathf.Pow(a + L / 3f, 1f / 3f));
        bool Near(double n) => n >= a && n < a + L / 2 && (at(n) - corner).sqrMagnitude < radius * radius;
        Vector3 Lift(double n) { Vector3 p = at(n); return p + (p - o).normalized * .4f; }
        List<Vector3> run = new();
        void Flush() { if (run.Count > 1) ExpLine(g, "yarn", .07f, YarnColor, Factory.NoGlow, run.ToArray()); run.Clear(); }
        for (double n = a; n < a + L / 2; n += .125)
        {
            if (Near(n)) run.Add(at(n));
            else Flush();
        }

        Flush();
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        for (long n = a; n < a + L / 2; n++)
        {
            if (!Near(n)) continue;
            bool p = ExpPrime(n);
            pts.Add(Lift(n)); cols.Add(p ? PrimeGold : NumberDim); sizes.Add(p ? .6f : .32f);
        }

        ExpPointCloud(g, "numbers", pts, cols, sizes, Factory.GlowIntensity);
        Color[] pencil = { new(.6f, .8f, 1f), new(1f, .35f, .3f), new(1f, .55f, .25f), new(1f, .3f, .6f), new(.85f, .45f, 1f), new(1f, .75f, .45f), new(.95f, .25f, .25f), new(.5f, 1f, .7f) };
        Dictionary<long, int> hits = new();
        int ci = 0;
        for (int r = -3; r <= 4; r++, ci++)
        {
            // the multiples of m drift round the sphere and come back into the window: one straight run per pass
            long m = k0 + r;
            Color col = pencil[ci % pencil.Length];
            List<Vector3> line = new(), longest = new();
            void Run()
            {
                if (line.Count > 1) ExpLine(g, $"multiples of {m}", .14f, col, Factory.GlowIntensity, line.ToArray());
                if (line.Count > longest.Count) longest = new List<Vector3>(line);
                line.Clear();
            }

            for (long n = (a / m + 1) * m; n < a + L / 2; n += m)
            {
                if (!Near(n)) { Run(); continue; }
                line.Add(Lift(n) + (Lift(n) - o).normalized * .2f);
                hits[n] = hits.TryGetValue(n, out int h) ? h + 1 : 1;
            }

            Run();
            if (labelSize > 0f && longest.Count > 1) ExpLabelWide(g, $"×{m}", longest[^1] + (longest[^1] - longest[^2]).normalized * 3f, labelSize * .42f, col);
        }

        foreach (KeyValuePair<long, int> h in hits)
            if (h.Value >= 3)
            {
                RingAround(g, $"{h.Key} on {h.Value} lines", Lift(h.Key), (Lift(h.Key) - o).normalized, 1.3f, Color.white, .1f, AttentionIntensity);
                if (labelSize > 0f) ExpLabelWide(g, $"{h.Key:N0}", Lift(h.Key) + CubeCorner1Up * 2.6f, labelSize * .4f, Color.white);
            }

        long pron = (long)k0 * k0 * (k0 + 1);
        if (Near(pron))
        {
            RingAround(g, "k²(k+1)", Lift(pron), (Lift(pron) - o).normalized, 1.3f, PronicColor, .12f, Factory.GlowIntensity);
            if (labelSize > 0f) ExpLabelWide(g, $"{pron:N0} = {k0}²·{k0 + 1}", Lift(pron) - CubeCorner1Up * 2.6f, labelSize * .4f, PronicColor);
        }

        if (Near(pron + 17))
        {
            RingAround(g, "k²(k+1) + 17", Lift(pron + 17), (Lift(pron + 17) - o).normalized, 1.3f, PrimeGold, .12f, Factory.GlowIntensity);
            if (labelSize > 0f) ExpLabelWide(g, $"+ 17 = {pron + 17:N0}" + (ExpPrime(pron + 17) ? ", prime" : ""), Lift(pron + 17) + CubeCorner1Up * 2.6f, labelSize * .4f, PrimeGold);
        }

        Board($"one shell of the ball: layer {k0} at corner 1\n" + Small($"the multiples of {k0 - 3} … {k0 + 4} cross the windings in straight lines;  white rings: on three lines"));
    }
}
