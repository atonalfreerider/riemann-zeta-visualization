using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Sections GA–GE (docs/experiments/README.md): searching the geometries for a locator that says prime / not prime from a
/// position and, for a composite, leads back to its prime factors and their exponents.
///
/// GA. The E-series wheels stacked: E5 (= D5, 8 hours), E6 (12), E7 (18), E8 (30). Their hour counts are built from 2, 3, 5
///     only, so together they still remove only the multiples of 2, 3 and 5.
/// GB. The factor columns on E8's wheel: n at hour n mod 30 and radius s√n, its column rising to the funnel z = log n, cut
///     into one segment of height log p per prime factor (with multiplicity). The descent staircase drops one segment and
///     hops to the column of n/p, so it reaches 1 after one drop per prime factor: equal drops in a row are an exponent.
/// GC. Exponent waves: for each prime p, a sum of non-negative cosine waves (Fejér kernels of periods p, p², p³, …) whose
///     value at every integer n is the exponent of p in n; where every wave of a prime ≤ √n vanishes, n is prime.
/// GD. Square-root paths (Miller–Rabin): a^d, a^2d, …, a^(n−1) mod n. A prime reaches 1 only through −1; a composite that
///     reaches 1 through another square root of 1 hands over a factor, gcd(x − 1, n); most composites never reach 1.
/// GE. The search, as a table.
/// </summary>
public partial class SacksSpiral
{
    public const float EStackOffset = 49000f;
    public const float FactorFunnelOffset = 50000f;
    public const float ExponentWaveOffset = 51000f;
    public const float SquareRootOffset = 52000f;
    public const float SearchTableOffset = 53000f;

    static int CoxeterNumberOf(int rank) => rank switch { 5 => 8, 6 => 12, 7 => 18, _ => 30 };

    static string ELabel(int rank) => rank == 5 ? "E5 = D5" : "E" + rank;

    /// <summary>The colour of a prime's segment: 2, 3, 5, 7, 11, 13 their own hues, every prime from 17 on gold.</summary>
    static Color SegmentColor(long p) => p <= 13 ? PrimeHue(p) : PrimeGold;

    // ================================================================================================
    // GA. the E-series wheels stacked
    // ================================================================================================

    /// <summary>
    /// The wheels of the given ranks (E5 … E8) stacked along +z, gap apart: numbers 1 … nMax at their hours (primes gold,
    /// numbers on a spoke pale, the rest dim), the spokes gold; one thread per number in <paramref name="threads"/> through its
    /// positions, coloured by its smallest prime factor (gold for a prime), ringed bright where it sits on a spoke and dark where
    /// it has fallen off.
    /// </summary>
    public void ExpEStack(Vector3 o, int[] ranks, int nMax, float gap, float from, float to, float dot, float labelSize, long[] threads, int[] labelLayers,
        string[] threadGroups = null, string[] threadNotes = null)
    {
        Transform g = ExpGroup("GA E stack").transform;
        ExpEnsureSieve(nMax + 2);
        Vector3 Pos(int layer, long n)
        {
            int h = CoxeterNumberOf(ranks[layer]);
            float r = from + (to - from) * (n - 1) / (float)nMax, a = (90f - 360f / h * (n % h)) * Mathf.Deg2Rad;
            return o + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, gap * layer);
        }

        for (int layer = 0; layer < ranks.Length; layer++)
        {
            int h = CoxeterNumberOf(ranks[layer]);
            Vector3 c = o + Vector3.forward * (gap * layer);
            RingAround(g, $"rim {h}", c, Vector3.back, to * 1.06f, new Color(.5f, .55f, .7f) * .5f, dot * .12f, Factory.NoGlow);
            List<(Vector3, Vector3)> spokes = new();
            for (int k = 1; k < h; k++)
            {
                if (Gcd(k, h) != 1) continue;
                float a = (90f - 360f / h * k) * Mathf.Deg2Rad;
                Vector3 d = new(Mathf.Cos(a), Mathf.Sin(a), 0f);
                spokes.Add((c + d * from * .85f, c + d * to * 1.02f));
                if (labelSize > 0f) ExpLabelWide(g, k.ToString(), c + d * (to * 1.06f + labelSize * .035f), labelSize * .3f, PrimeGold);
            }

            Segments(g, $"spokes {h}", spokes, dot * .18f, PrimeGold * .4f, Factory.SoftGlowIntensity);
            List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
            for (long n = 1; n <= nMax; n++)
            {
                bool prime = ExpPrime(n), unit = Gcd(n, h) == 1;
                pts.Add(Pos(layer, n) + Vector3.back * .2f);
                cols.Add(prime ? PrimeGold : unit && n > 1 ? new Color(.82f, .84f, .95f) : new Color(.3f, .3f, .38f));
                sizes.Add(prime || unit ? dot : dot * .55f);
            }

            ExpPointCloud(g, $"numbers {h}", pts, cols, sizes, Factory.GlowIntensity);
            if (labelSize > 0f)
            {
                string sieves = h == 8 ? "removes 2" : h == 30 ? "removes 2, 3, 5" : "removes 2, 3";
                ExpLabelWide(g, $"E{ranks[layer]}:  {h} hours", c + new Vector3(-to * 1.4f, 0f, -labelSize * .022f), labelSize * .44f, Color.white, TextAlignmentOptions.Right);
                ExpLabelWide(g, sieves, c + new Vector3(-to * 1.4f, 0f, labelSize * .03f), labelSize * .32f, new Color(.8f, .83f, .93f), TextAlignmentOptions.Right);
            }
        }

        for (int t = 0; t < threads.Length; t++)
        {
            long n = threads[t];
            Transform gt = g;
            if (threadGroups != null)
            {
                Transform grp = FindExpGroup(threadGroups[t]) ?? ExpGroup(threadGroups[t]).transform;
                gt = RevealItem(grp, $"thread {n}", threadNotes != null ? threadNotes[t] : null);
                RevealNote rn = gt.GetComponent<RevealNote>();
                for (int layer = 0; layer < ranks.Length; layer++) if (Gcd(n, CoxeterNumberOf(ranks[layer])) != 1) { rn.FlareAt.Add(Pos(layer, n)); break; }
            }

            bool prime = ExpPrime(n);
            long spf = prime ? n : Factorize(n)[0];
            Color col = prime ? PrimeGold : spf == 3 ? new Color(.4f, 1f, .55f) : spf == 5 ? new Color(.45f, .65f, 1f) : E8Composite;
            List<Vector3> line = new();
            for (int layer = 0; layer < ranks.Length; layer++) line.Add(Pos(layer, n) + Vector3.back * .3f);
            ExpLine(gt, $"thread {n}", dot * .3f, col, Factory.GlowIntensity, line.ToArray());
            for (int layer = 0; layer < ranks.Length; layer++)
            {
                bool on = Gcd(n, CoxeterNumberOf(ranks[layer])) == 1;
                RingAround(gt, $"thread {n} ring {layer}", Pos(layer, n) + Vector3.back * .35f, Vector3.back, dot * 2f, on ? col : new Color(.25f, .25f, .3f),
                    dot * (on ? .22f : .3f), on ? AttentionIntensity : Factory.NoGlow);
            }

            if (labelSize > 0f)
            {
                int layer = labelLayers[t];
                Vector3 at = Pos(layer, n), outDir = (at - (o + Vector3.forward * (gap * layer))).normalized;
                ExpLabelWide(gt, prime ? n.ToString() : $"{n} = {string.Join("·", Factorize(n))}", at + outDir * (dot * 3f + labelSize * .055f) + Vector3.back * .4f,
                    labelSize * .3f, col);
            }
        }
    }

    // ================================================================================================
    // GB. the factor columns on E8's wheel, and the descent staircases
    // ================================================================================================

    Vector3 FunnelBase(Vector3 o, long n, float s)
    {
        float a = (90f - 12f * (n % 30)) * Mathf.Deg2Rad, r = s * Mathf.Sqrt(n);
        return o + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
    }

    /// <summary>
    /// Every n ≤ nMax on E8's wheel (hour n mod 30, radius s√n) with its column rising to the height hScale·log n (towards the
    /// viewer, −z), cut into one segment per prime factor counted with multiplicity: the largest prime at the floor, the smallest
    /// at the top, each of height hScale·log p in its prime's colour, with a small gap so they can be counted. The funnel
    /// z = log n is drawn as rings at 10, 100, 1000; the gold spokes on the floor. Then, for each n in <paramref name="stairs"/>,
    /// the descent: down the top segment (÷ smallest prime p), across at that height to the top of n/p's column, and so on to 1.
    /// </summary>
    public void ExpFactorColumns(Vector3 o, int nMax, float s, float hScale, float dot, float labelSize, long[] stairs, bool columns, bool labelStairs, float dim = 1f)
    {
        Transform g = ExpGroup("GB factor columns").transform;
        ExpEnsureSieve(nMax + 2);
        Vector3 Up(float h) => Vector3.back * (hScale * h);
        float gapFrac = .1f;
        if (columns)
        {
            Dictionary<long, List<(Vector3, Vector3)>> byPrime = new();
            List<Vector3> tops = new(); List<Color> topCols = new(); List<float> topSizes = new();
            for (long n = 2; n <= nMax; n++)
            {
                List<long> f = Factorize(n);
                f.Reverse(); // largest prime at the floor
                Vector3 b = FunnelBase(o, n, s);
                float h = 0f;
                foreach (long p in f)
                {
                    float seg = Mathf.Log(p), cut = seg * gapFrac * .5f;
                    long key = p <= 13 ? p : 17;
                    if (!byPrime.ContainsKey(key)) byPrime[key] = new();
                    byPrime[key].Add((b + Up(h + cut), b + Up(h + seg - cut)));
                    h += seg;
                }

                bool prime = f.Count == 1;
                tops.Add(b + Up(h)); topCols.Add((prime ? PrimeGold : new Color(.8f, .82f, .92f)) * dim); topSizes.Add(prime ? dot * .9f : dot * .55f);
            }

            foreach (var kv in byPrime) Segments(g, $"segments {kv.Key}", kv.Value, dot * .32f, SegmentColor(kv.Key) * (.85f * dim), Factory.SoftGlowIntensity);
            ExpPointCloud(g, "column tops", tops, topCols, topSizes, Factory.GlowIntensity);
        }

        // the funnel: rings at the heights of 10, 100, 1000 and the gold spokes on the floor
        foreach (int m in new[] { 10, 100, 1000 })
        {
            if (m > nMax * 1.2f) break;
            List<Vector3> ring = new();
            float r = s * Mathf.Sqrt(m);
            for (int i = 0; i <= 120; i++) { float a = i / 120f * Mathf.PI * 2f; ring.Add(o + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f) + Up(Mathf.Log(m))); }
            ExpLine(g, $"funnel ring {m}", dot * .1f, new Color(.5f, .55f, .75f) * .55f, Factory.NoGlow, ring.ToArray());
            if (labelSize > 0f) ExpLabelWide(g, $"log {m}", ring[15] + new Vector3(dot * 2f, 0f, 0f), labelSize * .26f, new Color(.6f, .65f, .85f), TextAlignmentOptions.Left);
        }

        List<(Vector3, Vector3)> spokes = new();
        float rMax = s * Mathf.Sqrt(nMax);
        foreach (int k in new[] { 1, 7, 11, 13, 17, 19, 23, 29 })
        {
            float a = (90f - 12f * k) * Mathf.Deg2Rad;
            spokes.Add((o, o + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * rMax * 1.03f));
        }

        Segments(g, "spokes", spokes, dot * .12f, PrimeGold * .3f, Factory.NoGlow);
        // the descent staircases
        foreach (long n0 in stairs)
        {
            long n = n0;
            float h = Mathf.Log(n);
            List<(Vector3, Vector3)> hops = new();
            Vector3 top = FunnelBase(o, n, s) + Up(h);
            RingAround(g, $"start {n0}", top + Vector3.back * .3f, Vector3.back, dot * 2.6f, Color.white, dot * .22f, AttentionIntensity);
            int step = 0;
            while (n > 1)
            {
                long p = Factorize(n)[0];
                Vector3 b = FunnelBase(o, n, s);
                float h1 = h - Mathf.Log(p);
                ExpLine(g, $"drop {n0}.{step}", dot * .75f, SegmentColor(p), AttentionIntensity, new[] { b + Up(h) + Vector3.back * .2f, b + Up(h1) + Vector3.back * .2f });
                long m = n / p;
                Vector3 bm = FunnelBase(o, m, s);
                // the hop: across at height h1 from n's column to the top of m's column, sweeping the shorter way round
                float a0 = Mathf.Atan2(b.y - o.y, b.x - o.x), a1 = Mathf.Atan2(bm.y - o.y, bm.x - o.x);
                float da = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                float r0 = (b - o).magnitude, r1 = (bm - o).magnitude;
                List<Vector3> arc = new();
                for (int i = 0; i <= 24; i++)
                {
                    float t = i / 24f, a = a0 + da * t, r = Mathf.Lerp(r0, r1, t);
                    arc.Add(o + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f) + Up(h1) + Vector3.back * .2f);
                }

                ExpLine(g, $"hop {n0}.{step}", dot * .3f, Color.white * .9f, Factory.GlowIntensity, arc.ToArray());
                if (labelStairs && labelSize > 0f)
                    ExpLabelWide(g, $"÷{p}", b + Up((h + h1) * .5f) + new Vector3(dot * 2.2f, 0f, -.3f), labelSize * .26f, SegmentColor(p), TextAlignmentOptions.Left);
                n = m; h = h1; step++;
            }

            ExpDot(g, $"one {n0}", FunnelBase(o, 1, s) + Vector3.back * .3f, dot * 1.6f, Color.white, AttentionIntensity);
            if (labelSize > 0f)
            {
                string f = FactorString(n0);
                ExpLabelWide(g, ExpPrime(n0) ? $"{n0}:  prime" : $"{n0} = {f}", top + Up(.35f) + new Vector3(0f, 0f, -.3f), labelSize * .34f, Color.white);
            }
        }
    }

    /// <summary>
    /// The paths back, unrolled: for each n, a staircase from height log n down to 0 (= 1), one drop of log p per prime factor
    /// (smallest first, in the prime's colour, labelled ÷p) and one step across between drops; the value reached after each drop
    /// written beside it. A log scale (1, 10, 100, 1000) on the left.
    /// </summary>
    public void ExpDescentChart(Vector3 o, long[] ns, float xStep, float yScale, float groupGap, float dot, float labelSize)
    {
        Transform g = ExpGroup("GB descent chart").transform;
        float x = 0f, hMax = 0f;
        foreach (long n in ns) hMax = Mathf.Max(hMax, Mathf.Log(n));
        List<(Vector3, Vector3)> grid = new();
        // each path takes its steps' width, or its title's if that is wider
        string Title(long n) => ExpPrime(n) ? $"{n}:  prime" : $"{n} = {FactorString(n)}";
        float Advance(long n) => Mathf.Max(Factorize(n).Count * xStep, .052f * labelSize * .34f * System.Text.RegularExpressions.Regex.Replace(Title(n), "<[^>]+>", "").Length + xStep * .3f) + groupGap;
        float totalW = 0f;
        foreach (long n in ns) totalW += Advance(n);
        foreach (int m in new[] { 1, 10, 100, 1000 })
        {
            float y = Mathf.Log(m) * yScale;
            if (Mathf.Log(m) > hMax * 1.05f) break;
            grid.Add((o + new Vector3(-xStep * .6f, y, .2f), o + new Vector3(totalW, y, .2f)));
            if (labelSize > 0f) ExpLabelWide(g, m.ToString("N0"), o + new Vector3(-xStep * .9f, y, 0f), labelSize * .26f, new Color(.6f, .64f, .8f), TextAlignmentOptions.Right);
        }

        Segments(g, "log grid", grid, dot * .08f, new Color(.4f, .44f, .6f) * .5f);
        foreach (long n0 in ns)
        {
            long n = n0;
            float h = Mathf.Log(n);
            Vector3 start = o + new Vector3(x, h * yScale, 0f);
            ExpDot(g, $"start {n0}", start + Vector3.back * .2f, dot * 1.5f, ExpPrime(n0) ? PrimeGold : Color.white, AttentionIntensity);
            float x0 = x;
            if (labelSize > 0f)
                ExpLabelWide(g, Title(n0), start + new Vector3(0f, labelSize * .05f, 0f), labelSize * .34f, ExpPrime(n0) ? PrimeGold : Color.white, TextAlignmentOptions.Left);
            while (n > 1)
            {
                long p = Factorize(n)[0];
                float h1 = h - Mathf.Log(p);
                Vector3 a = o + new Vector3(x, h * yScale, 0f), b = o + new Vector3(x, h1 * yScale, 0f);
                ExpLine(g, $"drop {n0} {n}", dot * .7f, SegmentColor(p), AttentionIntensity, new[] { a + Vector3.back * .1f, b + Vector3.back * .1f });
                long m = n / p;
                Vector3 c = b + new Vector3(xStep, 0f, 0f);
                ExpLine(g, $"step {n0} {n}", dot * .25f, Color.white * .8f, Factory.SoftGlowIntensity, new[] { b + Vector3.back * .1f, c + Vector3.back * .1f });
                ExpDot(g, $"at {n0} {m}", c + Vector3.back * .2f, dot * .9f, m == 1 ? Color.white : new Color(.85f, .87f, .96f), Factory.GlowIntensity);
                if (labelSize > 0f)
                {
                    ExpLabelWide(g, $"÷{p}", (a + b) * .5f + new Vector3(-dot * 1.6f, 0f, -.2f), labelSize * .27f, SegmentColor(p), TextAlignmentOptions.Right);
                    ExpLabelWide(g, m.ToString(), c + new Vector3(dot * 1.2f, labelSize * .022f, -.2f), labelSize * .22f, new Color(.78f, .8f, .92f), TextAlignmentOptions.Left);
                }

                n = m; h = h1; x += xStep;
            }

            x = x0 + Advance(n0);
        }
    }

    // ================================================================================================
    // GC. exponent waves
    // ================================================================================================

    /// <summary>The Fejér kernel (1/q²)·(sin πx / sin(πx/q))²: a sum of cosines of periods q/j, 1 at multiples of q, 0 at other integers.</summary>
    static double Fejer(int q, double x)
    {
        double den = System.Math.Sin(System.Math.PI * x / q);
        if (System.Math.Abs(den) < 1e-9) return 1.0;
        double num = System.Math.Sin(System.Math.PI * x);
        return num * num / (q * q * den * den);
    }

    static double ExponentWave(int p, double x, int nMax)
    {
        double y = 0;
        for (long q = p; q <= 4L * nMax; q *= p) y += Fejer((int)q, x);
        return y;
    }

    /// <summary>
    /// One layer per prime (stacked downwards): the wave y_p(x) = Σ_k F_{p^k}(x) over 1 … nMax, whose value at each integer n is
    /// the exponent of p in n; crests of height ≥ 2 labelled. Highlighted columns read the exponent vector of chosen n; along
    /// the bottom, the n where every wave vanishes (above the largest prime drawn): the primes, in gold.
    /// </summary>
    public void ExpExponentWaves(Vector3 o, int nMax, int[] primes, float xs, float unit, float layerGap, float dot, float labelSize, long[] highlight)
    {
        Transform g = ExpGroup("GC exponent waves").transform;
        ExpEnsureSieve(nMax + 2);
        float y = 0f;
        float[] baseY = new float[primes.Length];
        int[] maxExp = new int[primes.Length];
        for (int i = 0; i < primes.Length; i++)
        {
            int e = 0; long q = primes[i];
            while (q <= nMax) { e++; q *= primes[i]; }
            maxExp[i] = e;
            y -= unit * e + layerGap;
            baseY[i] = y;
        }

        float Xof(double x) => (float)((x - (nMax + 1) * .5) * xs);
        for (int i = 0; i < primes.Length; i++)
        {
            int p = primes[i];
            Color col = PrimeHue(p);
            Vector3 b = o + new Vector3(0f, baseY[i], 0f);
            ExpLine(g, $"baseline {p}", dot * .1f, new Color(.5f, .52f, .65f) * .6f, Factory.NoGlow, new[] { b + new Vector3(Xof(.5), 0f, 0f), b + new Vector3(Xof(nMax + .5), 0f, 0f) });
            List<Vector3> wave = new();
            for (int k = 0; k <= nMax * 16; k++)
            {
                double x = .5 + k / 16.0;
                wave.Add(b + new Vector3(Xof(x), (float)(ExponentWave(p, x, nMax) * unit), -.1f));
            }

            ExpLine(g, $"wave {p}", dot * .28f, col, Factory.GlowIntensity, wave.ToArray());
            if (labelSize <= 0f) continue;
            ExpLabelWide(g, $"p = {p}", b + new Vector3(Xof(.5) - xs * 1.5f, unit * .5f, 0f), labelSize * .42f, col, TextAlignmentOptions.Right);
            for (long n = p; n <= nMax; n += p)
            {
                int e = 0; long m = n;
                while (m % p == 0) { e++; m /= p; }
                if (e < (p == 2 ? 3 : 2)) continue;
                ExpLabelWide(g, e.ToString(), b + new Vector3(Xof(n), unit * e + labelSize * .025f, -.2f), labelSize * .26f, col);
            }
        }

        // the highlighted columns
        float top = unit * .4f, bottom = baseY[^1] - layerGap * .55f;
        int hi = 0;
        foreach (long n in highlight)
        {
            float lift = (hi++ % 2) * labelSize * .045f;
            List<(Vector3 c, Vector3 u, Vector3 v, Color col)> band = new()
            {
                (o + new Vector3(Xof(n), (top + bottom) * .5f, .5f), new Vector3(xs * .42f, 0f, 0f), new Vector3(0f, (top - bottom) * .5f, 0f), new Color(.16f, .17f, .26f)),
            };
            ExpQuads(g, $"band {n}", band, Factory.NoGlow);
            if (labelSize > 0f)
            {
                bool prime = ExpPrime(n);
                ExpLabelWide(g, prime ? $"{n}:  prime" : $"{n} = {FactorString(n)}", o + new Vector3(Xof(n), top + labelSize * .03f + lift, -.2f), labelSize * .3f, prime ? PrimeGold : Color.white);
            }
        }

        // the primes beyond the largest drawn prime: every wave vanishes there
        float lineY = bottom - layerGap * .2f;
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        int pmax = primes[^1];
        for (long n = 2; n <= nMax; n++)
        {
            bool zero = true;
            foreach (int p in primes) if (n % p == 0) { zero = false; break; }
            pts.Add(o + new Vector3(Xof(n), lineY, 0f));
            cols.Add(zero ? PrimeGold : new Color(.35f, .36f, .45f)); sizes.Add(zero ? dot * 1.1f : dot * .5f);
        }

        ExpPointCloud(g, "number line", pts, cols, sizes, Factory.GlowIntensity);
        if (labelSize <= 0f) return;
        for (int n = 10; n <= nMax; n += 10) ExpLabelWide(g, n.ToString(), o + new Vector3(Xof(n), lineY - labelSize * .03f, 0f), labelSize * .24f, new Color(.65f, .68f, .82f));
        ExpLabelWide(g, $"gold:  every wave is zero — the primes above {pmax}", o + new Vector3(Xof(.5), lineY - labelSize * .065f, 0f), labelSize * .3f, PrimeGold, TextAlignmentOptions.Left);
    }

    // ================================================================================================
    // GD. square-root paths
    // ================================================================================================

    static (int s, long d, List<long> xs) SquarePath(long n, long a)
    {
        long d = n - 1; int s = 0;
        while (d % 2 == 0) { d /= 2; s++; }
        List<long> xs = new() { ModPowSmall(a, d, n) };
        for (int i = 0; i < s; i++) xs.Add(xs[^1] * xs[^1] % n);
        return (s, d, xs);
    }

    static string PathText(long n, long a)
    {
        var (s, d, xs) = SquarePath(n, a);
        return string.Join(" → ", xs.ConvertAll(x => x == n - 1 ? "−1" : x.ToString()));
    }

    /// <summary>
    /// A prime's square-root path on its index circle: residue x at angle 2π·ind_g(x)/(p − 1), clockwise from the top, so 1 is at
    /// the top, −1 at the bottom, and squaring doubles the angle; the path a^d → … → 1 drawn as chords.
    /// </summary>
    public void ExpIndexCirclePath(Vector3 o, long p, long g, long a, float radius, float dot, float labelSize, string title)
    {
        Transform gr = ExpGroup($"GD index circle {p}").transform;
        Dictionary<long, long> ind = new();
        long x = 1;
        for (long k = 0; k < p - 1; k++) { ind[x] = k; x = x * g % p; }
        Vector3 At(long r) { float t = ind[r] / (float)(p - 1), ang = (90f - 360f * t) * Mathf.Deg2Rad; return o + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * radius; }
        RingAround(gr, "circle", o, Vector3.back, radius, new Color(.55f, .58f, .75f) * .6f, dot * .12f, Factory.NoGlow);
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        for (long r = 1; r < p; r++) { pts.Add(At(r)); cols.Add(new Color(.4f, .42f, .55f)); sizes.Add(dot * .35f); }
        ExpPointCloud(gr, "residues", pts, cols, sizes, Factory.NoGlow);
        var (s, d, xs) = SquarePath(p, a);
        for (int i = 0; i + 1 < xs.Count; i++)
        {
            if (xs[i] == xs[i + 1]) continue;
            Arrow(gr, $"step {i}", At(xs[i]), At(xs[i + 1]), dot, Color.white);
        }

        ExpDot(gr, "one", At(1) + Vector3.back * .2f, dot * 1.6f, PrimeGold, AttentionIntensity);
        ExpDot(gr, "minus one", At(p - 1) + Vector3.back * .2f, dot * 1.6f, Color.white, AttentionIntensity);
        if (labelSize <= 0f) return;
        foreach (long v in xs)
        {
            Vector3 at = At(v), outDir = (at - o).normalized;
            string t = v == 1 ? "1" : v == p - 1 ? "−1" : v.ToString();
            ExpLabelWide(gr, t, at + outDir * (dot * 2.5f + labelSize * .025f), labelSize * .3f, v == 1 ? PrimeGold : Color.white);
        }

        string[] lines = title.Split('|');
        for (int i = 0; i < lines.Length; i++)
            ExpLabelWide(gr, lines[i], o + new Vector3(0f, radius + labelSize * (.17f - .036f * i), 0f), labelSize * (i == 0 ? .4f : .28f), i == 0 ? PrimeGold : new Color(.82f, .85f, .94f));
    }

    void Arrow(Transform g, string name, Vector3 a, Vector3 b, float dot, Color col)
    {
        Vector3 dir = (b - a).normalized, side = Vector3.Cross(dir, Vector3.back).normalized;
        Vector3 tip = b - dir * dot * 1.2f;
        ExpLine(g, name, dot * .3f, col, Factory.GlowIntensity, new[] { a + dir * dot * 1.2f + Vector3.back * .15f, tip + Vector3.back * .15f });
        ExpLine(g, name + " head", dot * .3f, col, Factory.GlowIntensity,
            new[] { tip - dir * dot * 1.4f + side * dot * .8f + Vector3.back * .15f, tip + Vector3.back * .15f, tip - dir * dot * 1.4f - side * dot * .8f + Vector3.back * .15f });
    }

    /// <summary>
    /// A composite's square-root path on its CRT grid: residue x at (x mod A, x mod B), both taken in (−A/2, A/2] and (−B/2, B/2]
    /// so that the square roots of 1 — (1, 1), (−1, −1), (1, −1), (−1, 1) — form a small square at the centre. (1, 1) gold,
    /// (−1, −1) white, the two mixed corners red: arriving at 1 through a mixed corner hands over a factor.
    /// </summary>
    public void ExpCrtPath(Vector3 o, long n, long A, long B, long a, float cell, float dot, float labelSize, string title, string verdict)
    {
        Transform gr = ExpGroup($"GD crt {n}").transform;
        long Sg(long x, long m) { x %= m; if (x < 0) x += m; return x > m / 2 ? x - m : x; }
        Vector3 At(long x) => o + new Vector3(Sg(x, A) * cell, Sg(x, B) * cell, 0f);
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        for (long x = 0; x < n; x++) { pts.Add(At(x)); cols.Add(new Color(.32f, .34f, .45f)); sizes.Add(dot * .3f); }
        ExpPointCloud(gr, "residues", pts, cols, sizes, Factory.NoGlow);
        // the factor lines: x ≡ ±1 (mod A) and x ≡ ±1 (mod B); a path point on one of them (off the full corner) leaks a factor
        float halfA = A / 2f * cell + cell * .5f, halfB = B / 2f * cell + cell * .5f;
        List<(Vector3, Vector3)> factorLines = new();
        foreach (int sgn in new[] { 1, -1 })
        {
            factorLines.Add((o + new Vector3(sgn * cell, -halfB, .1f), o + new Vector3(sgn * cell, halfB, .1f)));
            factorLines.Add((o + new Vector3(-halfA, sgn * cell, .1f), o + new Vector3(halfA, sgn * cell, .1f)));
        }

        Segments(gr, "factor lines", factorLines, dot * .14f, E8Composite * .45f);
        // the four square roots of 1 (for two coprime parts)
        (long xa, long xb, Color col)[] roots = { (1, 1, PrimeGold), (-1, -1, Color.white), (1, -1, E8Composite), (-1, 1, E8Composite) };
        foreach (var r in roots)
        {
            Vector3 at = o + new Vector3(r.xa * cell, r.xb * cell, -.2f);
            RingAround(gr, $"root {r.xa},{r.xb}", at, Vector3.back, dot * 1.6f, r.col, dot * .2f, AttentionIntensity);
        }

        var (s, d, xs) = SquarePath(n, a);
        for (int i = 0; i + 1 < xs.Count; i++)
        {
            if (xs[i] == xs[i + 1]) continue;
            Arrow(gr, $"step {i}", At(xs[i]), At(xs[i + 1]), dot, Color.white);
        }

        List<Vector3> pp = new(); List<Color> pc = new(); List<float> ps = new();
        foreach (long v in xs) { pp.Add(At(v) + Vector3.back * .25f); pc.Add(v == 1 ? PrimeGold : Color.white); ps.Add(dot * .9f); }
        ExpPointCloud(gr, "path", pp, pc, ps, AttentionIntensity);
        if (labelSize <= 0f) return;
        float halfW = A / 2f * cell, halfH = B / 2f * cell;
        string[] lines = title.Split('|');
        for (int i = 0; i < lines.Length; i++)
            ExpLabelWide(gr, lines[i], o + new Vector3(0f, halfH + labelSize * (.1f - .033f * i), 0f), labelSize * (i == 0 ? .38f : .27f), i == 0 ? Color.white : new Color(.82f, .85f, .94f));
        string[] vl = verdict.Split('|');
        for (int i = 0; i < vl.Length; i++)
            ExpLabelWide(gr, vl[i], o + new Vector3(0f, -halfH - labelSize * (.04f + .033f * i), 0f), labelSize * .28f, i == 0 ? PrimeGold : new Color(.82f, .85f, .94f));
        ExpLabelWide(gr, $"x mod {A} →", o + new Vector3(halfW + cell, -cell * 1.5f, 0f), labelSize * .22f, new Color(.6f, .64f, .8f), TextAlignmentOptions.Left);
        ExpLabelWide(gr, $"x mod {B} ↑", o + new Vector3(cell * 1.5f, halfH + cell * .5f, 0f), labelSize * .22f, new Color(.6f, .64f, .8f), TextAlignmentOptions.Left);
    }

    // ================================================================================================
    // GE. the search table
    // ================================================================================================

    /// <summary>
    /// A table: one row per geometry, one column per question; each cell a short text on a coloured tile (g green: yes, cheaply;
    /// a amber: yes, at a search or factoring cost; r red: no; n grey: not applicable). Rows are "name|cell:code|cell:code|…".
    /// </summary>
    public void ExpSearchTable(Vector3 o, string[] header, float[] widths, string[] rows, float rowH, float labelSize)
    {
        Transform g = ExpGroup("GE search table").transform;
        List<(Vector3 c, Vector3 u, Vector3 v, Color col)> quads = new();
        float totalW = 0f;
        foreach (float w in widths) totalW += w;
        float x0 = -totalW * .5f;
        Color Tile(char code) => code switch
        {
            'g' => new Color(.12f, .38f, .2f),
            'a' => new Color(.42f, .32f, .1f),
            'r' => new Color(.4f, .13f, .13f),
            _ => new Color(.18f, .18f, .22f),
        };
        // header
        float x = x0;
        for (int c = 0; c < header.Length; c++)
        {
            ExpLabelWide(g, header[c], o + new Vector3(x + widths[c] * .5f, rowH * .55f, -.2f), labelSize * .3f, PrimeGold);
            x += widths[c];
        }

        for (int r = 0; r < rows.Length; r++)
        {
            string[] cells = rows[r].Split('|');
            x = x0;
            float yc = -r * rowH - rowH * .5f;
            for (int c = 0; c < cells.Length && c < widths.Length; c++)
            {
                string text = cells[c];
                char code = 'n';
                int colon = text.LastIndexOf(':');
                if (c > 0 && colon >= 0 && colon == text.Length - 2) { code = text[^1]; text = text.Substring(0, colon); }
                Vector3 center = o + new Vector3(x + widths[c] * .5f, yc, 0f);
                quads.Add((center + Vector3.forward * .3f, new Vector3(widths[c] * .5f - .25f, 0f, 0f), new Vector3(0f, rowH * .5f - .25f, 0f), c == 0 ? new Color(.12f, .12f, .16f) : Tile(code)));
                ExpLabelWide(g, text, center + Vector3.back * .2f, labelSize * (c == 0 ? .27f : .25f), c == 0 ? Color.white : new Color(.93f, .94f, .98f));
                x += widths[c];
            }
        }

        ExpQuads(g, "tiles", quads, Factory.NoGlow);
    }
}
