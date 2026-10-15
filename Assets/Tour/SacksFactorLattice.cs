using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Sections FA–FD (docs/experiments/README.md): unique factorization as geometry. Every n = Π p^a is the point a of the
/// exponent lattice (one axis per prime); multiplying is a step along an axis, so in any picture where multiplication
/// becomes addition the paths from the primes land exactly on their product.
///
/// FA. The factor lattice of 2, 3, 5: the 24 divisors of 360 fill a 4 × 3 × 2 box; the number line is its shadow on the
///     direction (log 2, log 3, log 5), injective because those logarithms are rationally independent (unique factorization).
/// FB. Two factors: the hyperbola xy = n carries every factor pair of n and meets the line y = 1 at n; in log–log
///     coordinates it is the straight line u + v = log n: the slide rule.
/// FC. n factors: each prime a vector of length log p in its own direction; the divisors of n are the vertices of a
///     zonotope (a projected box), and all orderings of n's prime factors are edge paths from 1 to n.
/// FD. A multiplicative wheel: modulo 31 the units are cyclic (3 is a primitive root), so placing n at hour ind₃(n) and
///     radius log n turns multiplication into rotation plus a step out: products land at the sum of their factors' moves.
/// </summary>
public partial class SacksSpiral
{
    public const float FactorLatticeOffset = 44000f;
    public const float ShadowOffset = 45000f;
    public const float HyperbolaOffset = 46000f;
    public const float ZonotopeOffset = 47000f;
    public const float IndexWheelOffset = 48000f;

    /// <summary>One colour per prime (2, 3, 5, 7, 11, 13 distinct; larger primes grey-blue).</summary>
    static Color PrimeHue(long p) => p switch
    {
        2 => new Color(1f, .36f, .32f),
        3 => new Color(.38f, 1f, .5f),
        5 => new Color(.45f, .66f, 1f),
        7 => new Color(1f, .68f, .25f),
        11 => new Color(.78f, .5f, 1f),
        13 => new Color(.3f, .95f, .95f),
        _ => new Color(.7f, .72f, .85f),
    };

    static Dictionary<long, int> FactorMap(long n)
    {
        Dictionary<long, int> f = new();
        foreach (long p in Factorize(n)) f[p] = f.TryGetValue(p, out int a) ? a + 1 : 1;
        return f;
    }

    static string FactorString(long n)
    {
        if (n == 1) return "1";
        List<string> parts = new();
        foreach (var kv in FactorMap(n)) parts.Add(kv.Value == 1 ? kv.Key.ToString() : kv.Key + Superscript(kv.Value));
        return string.Join("·", parts);
    }

    /// <summary>An exponent as rich text (the label font has ² and ³ but not the other superscript digits).</summary>
    static string Superscript(int k) => k == 2 ? "²" : k == 3 ? "³" : $"<sup>{k}</sup>";

    // ================================================================================================
    // FA. the factor lattice of 2, 3, 5, and its shadow
    // ================================================================================================

    /// <summary>
    /// The lattice points 2^a·3^b·5^c (a ≤ aMax, b ≤ bMax, c ≤ cMax) at s·(a·e2 + b·e3 + c·e5): the divisors of boxN bright and
    /// labelled, the rest faint; every multiply-by-p edge inside the box in p's colour; the primes gold, 1 white; one path from
    /// 1 to boxN (the factors in <paramref name="pathOrder"/>) in white; rings at the products in <paramref name="ringed"/>.
    /// </summary>
    public void ExpFactorLattice(Vector3 o, float s, int aMax, int bMax, int cMax, long boxN, Vector3 e2, Vector3 e3, Vector3 e5,
        long[] pathOrder, long[] ringed, float dot, float labelSize)
    {
        Transform g = ExpGroup("FA factor lattice").transform;
        Vector3 At(int a, int b, int c) => o + (e2 * a + e3 * b + e5 * c) * s;
        long Val(int a, int b, int c) { long v = 1; for (int i = 0; i < a; i++) v *= 2; for (int i = 0; i < b; i++) v *= 3; for (int i = 0; i < c; i++) v *= 5; return v; }
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        List<(Vector3, Vector3)> faint = new();
        Dictionary<long, List<(Vector3, Vector3)>> edges = new() { [2] = new(), [3] = new(), [5] = new() };
        for (int a = 0; a <= aMax; a++)
            for (int b = 0; b <= bMax; b++)
                for (int c = 0; c <= cMax; c++)
                {
                    long n = Val(a, b, c);
                    bool inBox = boxN % n == 0;
                    Vector3 p = At(a, b, c);
                    pts.Add(p);
                    cols.Add(n == 1 ? Color.white : n == 2 || n == 3 || n == 5 ? PrimeGold : inBox ? new Color(.86f, .88f, .96f) : new Color(.32f, .33f, .42f));
                    sizes.Add(inBox ? dot : dot * .55f);
                    void Edge(int da, int db, int dc, long prime)
                    {
                        if (a + da > aMax || b + db > bMax || c + dc > cMax) return;
                        long m = Val(a + da, b + db, c + dc);
                        Vector3 q = At(a + da, b + db, c + dc);
                        if (inBox && boxN % m == 0) edges[prime].Add((p, q)); else faint.Add((p, q));
                    }

                    Edge(1, 0, 0, 2); Edge(0, 1, 0, 3); Edge(0, 0, 1, 5);
                    if (inBox && labelSize > 0f)
                        ExpLabelWide(g, n.ToString(), p + (e2 + e3 - e5).normalized * (dot * 1.6f) + Vector3.back * (dot * 1.2f), labelSize * .3f,
                            n == 2 || n == 3 || n == 5 ? PrimeGold : n == boxN ? Color.white : new Color(.85f, .87f, .95f));
                }

        Segments(g, "lattice", faint, dot * .1f, new Color(.4f, .42f, .55f) * .4f);
        foreach (var kv in edges) Segments(g, $"x{kv.Key} edges", kv.Value, dot * .2f, PrimeHue(kv.Key) * .85f, Factory.SoftGlowIntensity);
        ExpPointCloud(g, "points", pts, cols, sizes, Factory.GlowIntensity);
        // the path
        List<Vector3> path = new() { At(0, 0, 0) };
        int pa = 0, pb = 0, pc = 0;
        foreach (long p in pathOrder)
        {
            if (p == 2) pa++; else if (p == 3) pb++; else pc++;
            path.Add(At(pa, pb, pc));
        }

        ExpLine(g, "path", dot * .45f, Color.white, AttentionIntensity, path.ToArray());
        foreach (long n in ringed)
        {
            var f = FactorMap(n);
            Vector3 p = At(f.TryGetValue(2, out int x) ? x : 0, f.TryGetValue(3, out int y) ? y : 0, f.TryGetValue(5, out int z) ? z : 0);
            RingAround(g, $"ring {n}", p, Vector3.back, dot * 2.2f, Color.white, dot * .18f, AttentionIntensity);
        }

        if (labelSize <= 0f) return;
        // axis labels
        ExpLabelWide(g, "× 2", At(aMax, 0, 0) + e2 * s * .55f, labelSize * .42f, PrimeHue(2));
        ExpLabelWide(g, "× 3", At(0, bMax, 0) + e3 * s * .55f, labelSize * .42f, PrimeHue(3));
        ExpLabelWide(g, "× 5", At(0, 0, cMax) + e5 * s * .55f, labelSize * .42f, PrimeHue(5));
    }

    /// <summary>
    /// The same lattice and its shadow: the axis through 1 along W = log 2·e2 + log 3·e3 + log 5·e5 (normalised), a tick where
    /// each lattice point's perpendicular lands (at s·log n / |w|), the perpendiculars themselves, and the ticks labelled.
    /// </summary>
    public void ExpLatticeShadow(Vector3 o, float s, int aMax, int bMax, int cMax, Vector3 e2, Vector3 e3, Vector3 e5, float dot, float labelSize, Vector3 labelSide, Vector3 pointLabelSide)
    {
        Transform g = ExpGroup("FA lattice shadow").transform;
        Vector3 w = (e2 * Mathf.Log(2f) + e3 * Mathf.Log(3f) + e5 * Mathf.Log(5f));
        float wl = w.magnitude;
        Vector3 W = w / wl;
        Vector3 At(int a, int b, int c) => o + (e2 * a + e3 * b + e5 * c) * s;
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        List<(Vector3, Vector3)> drops = new(), grid = new();
        List<(float t, long n)> ticks = new();
        for (int a = 0; a <= aMax; a++)
            for (int b = 0; b <= bMax; b++)
                for (int c = 0; c <= cMax; c++)
                {
                    long n = 1;
                    for (int i = 0; i < a; i++) n *= 2;
                    for (int i = 0; i < b; i++) n *= 3;
                    for (int i = 0; i < c; i++) n *= 5;
                    Vector3 p = At(a, b, c);
                    float t = Vector3.Dot(p - o, W);
                    Vector3 foot = o + W * t;
                    drops.Add((p, foot));
                    ticks.Add((t, n));
                    bool prime = n == 2 || n == 3 || n == 5;
                    pts.Add(p); cols.Add(n == 1 ? Color.white : prime ? PrimeGold : new Color(.82f, .85f, .95f)); sizes.Add(dot);
                    // factor labels, except where they would fall among the tick labels just below the line
                    float below = Vector3.Dot(p - foot, labelSide.normalized);
                    if (labelSize > 0f && n > 1 && !prime && !(below < 0f && below > -(dot * 2.4f + labelSize * .14f)))
                        ExpLabelWide(g, $"{FactorString(n)}", p + pointLabelSide.normalized * (dot * 2f + labelSize * .012f), labelSize * .22f, new Color(.7f, .74f, .9f));
                    if (a < aMax) grid.Add((p, At(a + 1, b, c)));
                    if (b < bMax) grid.Add((p, At(a, b + 1, c)));
                    if (c < cMax) grid.Add((p, At(a, b, c + 1)));
                }

        Segments(g, "grid", grid, dot * .08f, new Color(.45f, .5f, .65f) * .5f);
        Segments(g, "perpendiculars", drops, dot * .06f, new Color(.55f, .62f, .85f) * .5f);
        ExpPointCloud(g, "points", pts, cols, sizes, Factory.GlowIntensity);
        ticks.Sort((x, y) => x.t.CompareTo(y.t));
        float tMax = ticks[^1].t;
        ExpLine(g, "the number line", dot * .3f, new Color(.9f, .92f, 1f), Factory.GlowIntensity, new[] { o - W * (s * .4f), o + W * (tMax + s * .8f) });
        List<Vector3> tp = new(); List<Color> tc = new(); List<float> ts = new();
        Vector3 side = labelSide.normalized;
        int k = 0;
        foreach (var (t, n) in ticks)
        {
            Vector3 at = o + W * t;
            bool prime = n == 2 || n == 3 || n == 5;
            tp.Add(at); tc.Add(n == 1 ? Color.white : prime ? PrimeGold : new Color(.9f, .92f, 1f)); ts.Add(dot * .8f);
            if (labelSize > 0f)
            {
                // four rows below the line, so neighbouring ticks never share a row
                float off = -(dot * 2.4f + labelSize * .028f * (1 + k % 4));
                ExpLine(g, $"tick leader {n}", dot * .08f, new Color(.6f, .65f, .85f) * .5f, Factory.NoGlow, new[] { at, at + side * (off + labelSize * .012f) });
                ExpLabelWide(g, n.ToString(), at + side * off, labelSize * .28f, n == 1 ? Color.white : prime ? PrimeGold : new Color(.88f, .9f, .98f));
            }

            k++;
        }

        ExpPointCloud(g, "ticks", tp, tc, ts, AttentionIntensity);
    }

    // ================================================================================================
    // FB. two factors: hyperbolas and the slide rule
    // ================================================================================================

    /// <summary>
    /// Two panels. Left: the lattice points (a, b), 1 ≤ a, b ≤ max, with the hyperbolas xy = n for n in <paramref name="ns"/>,
    /// their lattice points ringed, and the arc from (p, q) along xy = pq to (pq, 1) on the number line y = 1. Right: the same in
    /// log–log coordinates (u, v) = (log x, log y), where each hyperbola is the line u + v = log n and the arc is straight; below
    /// it, a slide rule: a fixed log scale and a sliding one shifted by log p, whose q stands over pq.
    /// </summary>
    public void ExpLandingHyperbolas(Vector3 left, Vector3 right, float size, int max, long[] ns, long p, long q, float dot, float labelSize)
    {
        Transform g = ExpGroup("FB hyperbolas").transform;
        float sc = size / max;
        Vector3 L(double x, double y) => left + new Vector3((float)(x * sc), (float)(y * sc), 0f);
        float lmax = Mathf.Log(max), lsc = size / lmax;
        Vector3 Rr(double u, double v) => right + new Vector3((float)(u * lsc), (float)(v * lsc), 0f);
        // frames and grids
        List<(Vector3, Vector3)> axes = new() { (L(0, 0), L(max, 0)), (L(0, 0), L(0, max)), (Rr(0, 0), Rr(lmax, 0)), (Rr(0, 0), Rr(0, lmax)) };
        Segments(g, "axes", axes, dot * .14f, new Color(.6f, .62f, .75f) * .7f);
        Segments(g, "number line", new List<(Vector3, Vector3)> { (L(1, 1), L(max, 1)), (Rr(0, 0) + Vector3.back * .1f, Rr(lmax, 0) + Vector3.back * .1f) }, dot * .3f, new Color(.95f, .95f, 1f) * .8f, Factory.SoftGlowIntensity);
        List<Vector3> lp = new(); List<Color> lc = new(); List<float> ls = new();
        for (int a = 1; a <= max; a++)
            for (int b = 1; b <= max; b++)
            {
                lp.Add(L(a, b)); lc.Add(new Color(.32f, .34f, .44f)); ls.Add(dot * .35f);
                lp.Add(Rr(Mathf.Log(a), Mathf.Log(b))); lc.Add(new Color(.32f, .34f, .44f)); ls.Add(dot * .3f);
            }

        ExpPointCloud(g, "lattice", lp, lc, ls, Factory.NoGlow);
        Color[] hcol = { new(.45f, .66f, 1f), new(.78f, .5f, 1f), PrimeGold, new(1f, .5f, .45f), new(.4f, 1f, .6f) };
        for (int i = 0; i < ns.Length; i++)
        {
            long n = ns[i];
            Color col = ExpPrime(n) ? PrimeGold : hcol[i % hcol.Length];
            List<Vector3> hyp = new(), line = new();
            for (int k = 0; k <= 200; k++)
            {
                double x = (double)n / max + (max - (double)n / max) * k / 200.0;
                hyp.Add(L(x, n / x) + Vector3.back * .05f);
            }

            line.Add(Rr(Mathf.Log(n) - lmax, lmax) + Vector3.back * .05f);
            line.Add(Rr(lmax, Mathf.Log(n) - lmax) + Vector3.back * .05f);
            ExpLine(g, $"xy = {n}", dot * .16f, col * .8f, Factory.SoftGlowIntensity, hyp.ToArray());
            ExpLine(g, $"u + v = log {n}", dot * .16f, col * .8f, Factory.SoftGlowIntensity, line.ToArray());
            List<Vector3> fp = new(); List<Color> fc = new(); List<float> fs = new();
            for (long a = 1; a <= n; a++)
            {
                if (n % a != 0 || a > max || n / a > max) continue;
                fp.Add(L(a, n / a) + Vector3.back * .2f); fc.Add(col); fs.Add(dot);
                fp.Add(Rr(Mathf.Log(a), Mathf.Log(n / a)) + Vector3.back * .2f); fc.Add(col); fs.Add(dot);
            }

            ExpPointCloud(g, $"pairs of {n}", fp, fc, fs, Factory.GlowIntensity);
            if (labelSize > 0f)
            {
                float hy = new[] { 10f, 17f, 26f, 31f, 33f }[i % 5];
                Vector3 tag = L(n / hy, hy);
                ExpLabelWide(g, $"xy = {n}", tag + new Vector3(dot * 1.2f, dot * 2.2f, -.3f), labelSize * .28f, col, TextAlignmentOptions.Left);
                Vector3 tag2 = Rr(Mathf.Log(n) * .3f, Mathf.Log(n) * .7f);
                ExpLabelWide(g, $"u + v = log {n}", tag2 + new Vector3(dot * 1.5f, dot * 1.5f, -.3f), labelSize * .26f, col, TextAlignmentOptions.Left);
            }
        }

        // the landing arc from (p, q) to (pq, 1), and its straight image
        long pq = p * q;
        List<Vector3> arc = new();
        for (int k = 0; k <= 80; k++) { double x = p + (pq - p) * k / 80.0; arc.Add(L(x, pq / x) + Vector3.back * .3f); }
        ExpLine(g, "landing arc", dot * .42f, Color.white, AttentionIntensity, arc.ToArray());
        ExpLine(g, "landing line", dot * .42f, Color.white, AttentionIntensity, new[] { Rr(Mathf.Log(p), Mathf.Log(q)) + Vector3.back * .3f, Rr(Mathf.Log(pq), 0) + Vector3.back * .3f });
        RingAround(g, "start", L(p, q) + Vector3.back * .4f, Vector3.back, dot * 2.2f, Color.white, dot * .2f, AttentionIntensity);
        RingAround(g, "land", L(pq, 1) + Vector3.back * .4f, Vector3.back, dot * 2.2f, Color.white, dot * .2f, AttentionIntensity);
        RingAround(g, "start log", Rr(Mathf.Log(p), Mathf.Log(q)) + Vector3.back * .4f, Vector3.back, dot * 2.2f, Color.white, dot * .2f, AttentionIntensity);
        RingAround(g, "land log", Rr(Mathf.Log(pq), 0) + Vector3.back * .4f, Vector3.back, dot * 2.2f, Color.white, dot * .2f, AttentionIntensity);
        // the slide rule under the right panel
        float ruleY = -size * .16f, ruleGap = size * .055f;
        Vector3 R0 = right + new Vector3(0f, ruleY, 0f), R1 = R0 + new Vector3(0f, -ruleGap, 0f);
        List<(Vector3, Vector3)> rule = new() { (R0, R0 + Vector3.right * size), (R1 + Vector3.right * Mathf.Log(p) * lsc, R1 + Vector3.right * (Mathf.Log(p) + lmax) * lsc) };
        Segments(g, "rule bars", rule, dot * .2f, new Color(.85f, .87f, .96f) * .7f);
        List<(Vector3, Vector3)> marks = new();
        foreach (int k in new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 15, 20, 30, 35, 40 })
        {
            if (k > max) continue;
            float x0 = Mathf.Log(k) * lsc;
            marks.Add((R0 + Vector3.right * x0, R0 + Vector3.right * x0 + Vector3.up * size * .02f));
            if (labelSize > 0f && (k <= 10 || k == 20 || k == 35 || k == 40))
                ExpLabelWide(g, k.ToString(), R0 + Vector3.right * x0 + Vector3.up * size * .045f, labelSize * .22f, k == pq ? Color.white : new Color(.8f, .82f, .92f));
            float x1 = (Mathf.Log(p) + Mathf.Log(k)) * lsc;
            if (Mathf.Log(p) + Mathf.Log(k) <= lmax + Mathf.Log(p))
            {
                marks.Add((R1 + Vector3.right * x1, R1 + Vector3.right * x1 - Vector3.up * size * .02f));
                if (labelSize > 0f && k <= 8)
                    ExpLabelWide(g, k.ToString(), R1 + Vector3.right * x1 - Vector3.up * size * .045f, labelSize * .22f, k == q ? Color.white : new Color(.8f, .82f, .92f));
            }
        }

        Segments(g, "rule marks", marks, dot * .12f, new Color(.85f, .87f, .96f) * .7f);
        ExpLine(g, "rule read", dot * .3f, Color.white, AttentionIntensity,
            new[] { R1 + Vector3.right * Mathf.Log(pq) * lsc, R0 + Vector3.right * Mathf.Log(pq) * lsc + Vector3.up * size * .02f });
        if (labelSize <= 0f) return;
        ExpLabelWide(g, $"({p}, {q})", L(p, q) + new Vector3(-dot * 3f, dot * 2.5f, -.3f), labelSize * .3f, Color.white, TextAlignmentOptions.Right);
        ExpLabelWide(g, $"lands on {pq}", L(pq, 1) + new Vector3(0f, dot * 3.5f, -.3f), labelSize * .3f, Color.white);
        ExpLabelWide(g, "y = 1:  the number line", L(max * .62f, 1) + new Vector3(0f, -dot * 3.5f, -.3f), labelSize * .26f, new Color(.85f, .87f, .96f));
        ExpLabelWide(g, $"slide rule:  the lower scale shifted by log {p};  its {q} stands over {pq}", R1 + new Vector3(size * .5f, -size * .085f, 0f), labelSize * .26f, new Color(.85f, .87f, .96f));
        ExpLabelWide(g, "x", L(max, 0) + new Vector3(dot * 3f, 0f, 0f), labelSize * .3f, new Color(.75f, .77f, .88f));
        ExpLabelWide(g, "y", L(0, max) + new Vector3(0f, dot * 3f, 0f), labelSize * .3f, new Color(.75f, .77f, .88f));
        ExpLabelWide(g, "log x", Rr(lmax, 0) + new Vector3(dot * 5f, 0f, 0f), labelSize * .3f, new Color(.75f, .77f, .88f));
        ExpLabelWide(g, "log y", Rr(0, lmax) + new Vector3(0f, dot * 3f, 0f), labelSize * .3f, new Color(.75f, .77f, .88f));
    }

    // ================================================================================================
    // FC. n factors: the prime star and the divisor zonotopes
    // ================================================================================================

    static readonly long[] StarPrimes = { 2, 3, 5, 7, 11, 13 };
    static readonly float[] StarAngles = { 15f, 45f, 75f, 105f, 135f, 165f };

    static Vector3 StarVector(long p, float scale, float fan = 1f)
    {
        int i = System.Array.IndexOf(StarPrimes, p);
        float a = (i >= 0 ? 90f + (StarAngles[i] - 90f) * fan : 90f) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * (Mathf.Log(p) * scale);
    }

    /// <summary>
    /// The divisor lattice of n drawn in the prime star (each prime p a vector of length log p in its own direction): every
    /// divisor d at the sum of its prime vectors, every edge d → d·p in p's colour; the divisors labelled; one path from 1 to n
    /// (its factors in <paramref name="pathOrder"/>) in white.
    /// </summary>
    public void ExpDivisorZonotope(Vector3 o, long n, float scale, long[] pathOrder, float dot, float labelSize, bool labelAll, string title)
    {
        Transform g = ExpGroup($"FC zonotope {n}").transform;
        Vector3 Pos(long d)
        {
            Vector3 v = Vector3.zero;
            foreach (var kv in FactorMap(d)) v += StarVector(kv.Key, scale) * kv.Value;
            return o + v;
        }

        List<long> divs = Divisors(n);
        Dictionary<long, List<(Vector3, Vector3)>> edges = new();
        foreach (long d in divs)
            foreach (long p in FactorMap(n).Keys)
            {
                if (n % (d * p) != 0) continue;
                if (!edges.ContainsKey(p)) edges[p] = new();
                edges[p].Add((Pos(d), Pos(d * p)));
            }

        foreach (var kv in edges) Segments(g, $"x{kv.Key}", kv.Value, dot * .2f, PrimeHue(kv.Key) * .85f, Factory.SoftGlowIntensity);
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        foreach (long d in divs)
        {
            pts.Add(Pos(d) + Vector3.back * .2f);
            cols.Add(d == 1 ? Color.white : ExpPrime(d) ? PrimeGold : d == n ? Color.white : new Color(.85f, .87f, .96f));
            sizes.Add(d == n || d == 1 ? dot * 1.3f : dot);
        }

        ExpPointCloud(g, "divisors", pts, cols, sizes, Factory.GlowIntensity);
        List<Vector3> path = new() { Pos(1) + Vector3.back * .25f };
        long cur = 1;
        foreach (long p in pathOrder) { cur *= p; path.Add(Pos(cur) + Vector3.back * .25f); }
        ExpLine(g, "path", dot * .4f, Color.white, AttentionIntensity, path.ToArray());
        if (labelSize <= 0f) return;
        foreach (long d in divs)
        {
            if (!labelAll && !(d == 1 || d == n || ExpPrime(d))) continue;
            ExpLabelWide(g, d.ToString(), Pos(d) + new Vector3(dot * 1.4f, dot * 1.2f, -.3f), labelSize * .24f,
                ExpPrime(d) ? PrimeGold : d == n || d == 1 ? Color.white : new Color(.85f, .87f, .96f), TextAlignmentOptions.Left);
        }

        if (title != null)
        {
            string[] lines = title.Split('|');
            Vector3 top = o + new Vector3(0f, -scale * .55f, 0f);
            for (int i = 0; i < lines.Length; i++)
                ExpLabelWide(g, lines[i], top + Vector3.down * (labelSize * .05f * i), labelSize * (i == 0 ? .42f : .3f), i == 0 ? Color.white : new Color(.82f, .85f, .94f));
        }
    }

    /// <summary>The prime star itself: the six vectors log p in their directions, labelled, from <paramref name="o"/>.</summary>
    public void ExpPrimeStar(Vector3 o, float scale, float dot, float labelSize)
    {
        Transform g = ExpGroup("FC prime star").transform;
        foreach (long p in StarPrimes)
        {
            Vector3 v = StarVector(p, scale);
            ExpLine(g, $"star {p}", dot * .35f, PrimeHue(p), Factory.GlowIntensity, new[] { o, o + v });
            if (labelSize > 0f) ExpLabelWide(g, $"{p}", o + v * 1.12f + Vector3.back * .3f, labelSize * .32f, PrimeHue(p));
        }

        ExpDot(g, "one", o + Vector3.back * .2f, dot * 1.3f, Color.white, Factory.GlowIntensity);
        if (labelSize > 0f) ExpLabelWide(g, "each prime p:  a step of length log p in its own direction", o + new Vector3(0f, -labelSize * .05f, -.3f), labelSize * .28f, new Color(.82f, .85f, .94f));
    }

    // ================================================================================================
    // FD. the multiplicative wheel mod 31
    // ================================================================================================

    /// <summary>
    /// The index wheel modulo 31: hour k (of 30) holds the residues 3^k mod 31, so ind₃(ab) = ind₃(a) + ind₃(b) and every
    /// multiplication is a rotation. Each n ≤ nMax coprime to 31 sits at its hour and at radius r0 + s·log n (primes gold);
    /// the factor paths of the products in <paramref name="examples"/> are drawn step by step (rotate by ind₃(p) hours,
    /// move out by s·log p), every order ending on the product.
    /// </summary>
    public void ExpIndexWheel(Vector3 o, int nMax, float r0, float s, long[][] examples, float dot, float labelSize)
    {
        Transform g = ExpGroup("FD index wheel").transform;
        const int m = 31, gen = 3;
        int[] ind = new int[m];
        for (int k = 0, x = 1; k < m - 1; k++, x = x * gen % m) ind[x] = k;
        ExpEnsureSieve(nMax + 2);
        Vector3 Pos(double logn, double hour)
        {
            double a = (90 - 12 * hour) * Mathf.Deg2Rad, r = r0 + s * logn;
            return o + new Vector3((float)(r * System.Math.Cos(a)), (float)(r * System.Math.Sin(a)), 0f);
        }

        float rMax = r0 + s * Mathf.Log(nMax);
        List<(Vector3, Vector3)> spokes = new();
        for (int k = 0; k < 30; k++) spokes.Add((Pos(-r0 / s * .4f, k), Pos(Mathf.Log(nMax) * 1.02f, k)));
        Segments(g, "hours", spokes, dot * .08f, new Color(.45f, .5f, .68f) * .45f);
        for (int ring = 1; ring <= 4; ring++)
        {
            float lr = Mathf.Log(Mathf.Pow(10, ring));
            if (lr > Mathf.Log(nMax)) break;
            List<Vector3> circle = new();
            for (int i = 0; i <= 120; i++) circle.Add(Pos(lr, i / 4f));
            ExpLine(g, $"ring 10^{ring}", dot * .08f, new Color(.45f, .5f, .68f) * .5f, Factory.NoGlow, circle.ToArray());
            if (labelSize > 0f) ExpLabelWide(g, ring == 1 ? "10" : ring == 2 ? "100" : ring == 3 ? "1,000" : "10,000", Pos(lr, 7.5f) + new Vector3(dot, dot, -.3f), labelSize * .22f, new Color(.6f, .64f, .8f));
        }

        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        for (long n = 1; n <= nMax; n++)
        {
            if (n % m == 0) continue;
            bool prime = ExpPrime(n);
            pts.Add(Pos(System.Math.Log(n), ind[n % m])); cols.Add(n == 1 ? Color.white : prime ? PrimeGold * .9f : new Color(.42f, .45f, .58f)); sizes.Add(prime ? dot * .7f : dot * .45f);
        }

        ExpPointCloud(g, "numbers", pts, cols, sizes, Factory.SoftGlowIntensity);
        // the factor paths: each step rotates by ind(p) hours (the short way round counts) and moves out by log p
        foreach (long[] factors in examples)
        {
            long prod = 1;
            foreach (long f in factors) prod *= f;
            for (int order = 0; order < 2; order++)
            {
                long[] seq = order == 0 ? factors : Reverse(factors);
                double logn = 0, hour = 0;
                List<Vector3> path = new() { Pos(0, 0) };
                foreach (long p in seq)
                {
                    double step = ind[p % m];
                    if (step > 15) step -= 30; // the short way round: the same hour mod 30
                    for (int k = 1; k <= 24; k++) path.Add(Pos(logn + System.Math.Log(p) * k / 24.0, hour + step * k / 24.0) + Vector3.back * .2f);
                    logn += System.Math.Log(p);
                    hour += step;
                }

                Color col = order == 0 ? Color.white : new Color(1f, .75f, .4f);
                ExpLine(g, $"path {prod} {order}", dot * (order == 0 ? .3f : .22f), col, AttentionIntensity, path.ToArray());
                // the partial products the path passes through
                long partial = 1;
                for (int i = 0; i + 1 < seq.Length; i++)
                {
                    partial *= seq[i];
                    Vector3 at = Pos(System.Math.Log(partial), ind[partial % m]) + Vector3.back * .3f;
                    RingAround(g, $"partial {partial}", at, Vector3.back, dot * 1.7f, col, dot * .16f, AttentionIntensity);
                    if (labelSize > 0f) ExpLabelWide(g, partial.ToString(), at + new Vector3(dot * 2.4f, dot * 1.6f, 0f), labelSize * .24f, col, TextAlignmentOptions.Left);
                }
            }

            Vector3 end = Pos(System.Math.Log(prod), ind[prod % m]);
            RingAround(g, $"product {prod}", end + Vector3.back * .3f, Vector3.back, dot * 2.4f, Color.white, dot * .2f, AttentionIntensity);
            if (labelSize > 0f)
                ExpLabelWide(g, $"{prod} = {string.Join("·", factors)}", end + (end - o).normalized * (dot * 3f + labelSize * .04f) + Vector3.back * .4f, labelSize * .3f, Color.white);
        }

        if (labelSize <= 0f) return;
        for (int k = 0; k < 30; k++)
        {
            int residue = 1;
            for (int i = 0; i < k; i++) residue = residue * gen % m;
            bool small = residue == 2 || residue == 3 || residue == 5 || residue == 7 || residue == 11 || residue == 13;
            ExpLabelWide(g, residue.ToString(), Pos(Mathf.Log(nMax) * 1.08f, k) + Vector3.back * .3f, labelSize * .26f, small ? PrimeGold : new Color(.7f, .74f, .88f));
        }
    }

    static long[] Reverse(long[] a) { long[] b = (long[])a.Clone(); System.Array.Reverse(b); return b; }

    // ================================================================================================
    // FE. the lattice read backwards: a quadratic sieve
    // ================================================================================================

    /// <summary>
    /// A quadratic sieve on N, drawn as the exponent lattice mod 2: the values x² − N (x from ⌈√N⌉ on) that factor over the
    /// base, one row each, with their exponents (odd cells lit amber, even non-zero dim blue); the rows in <paramref name="chosen"/>
    /// highlighted and summed in a last row, every column even (green), so the product of the chosen x² − N is a square Y²
    /// while the product of the x is X; gcd(X − Y, N) is a factor of N. Rows: up to <paramref name="rows"/> smooth values.
    /// </summary>
    public void ExpSieveTable(Vector3 o, long N, long[] baseP, int rows, long[] chosen, float cell, float labelSize)
    {
        Transform g = ExpGroup("FE sieve").transform;
        List<(long x, long v, int[] ex)> rels = new();
        long x0 = (long)System.Math.Ceiling(System.Math.Sqrt(N));
        for (long x = x0; rels.Count < rows && x < x0 + 100000; x++)
        {
            long v = x * x - N, r = v;
            int[] ex = new int[baseP.Length];
            for (int i = 0; i < baseP.Length; i++) while (r % baseP[i] == 0) { r /= baseP[i]; ex[i]++; }
            if (r == 1) rels.Add((x, v, ex));
        }

        int cols = baseP.Length;
        float colX0 = cell * 7.5f; // room for the x and x² − N columns
        Vector3 Cell(int row, int col) => o + new Vector3(colX0 + col * cell, -row * cell, 0f);
        List<(Vector3 c, Vector3 u, Vector3 v, Color col)> quads = new();
        int[] sum = new int[cols];
        Color dimText = new(.75f, .78f, .9f);
        for (int r = 0; r < rels.Count; r++)
        {
            bool pick = System.Array.IndexOf(chosen, rels[r].x) >= 0;
            if (pick) for (int i = 0; i < cols; i++) sum[i] += rels[r].ex[i];
            // the row band
            quads.Add((o + new Vector3((colX0 + (cols - 1) * cell) * .5f - cell * .25f, -r * cell, .4f), new Vector3((colX0 + cols * cell) * .5f + cell * .2f, 0f, 0f),
                new Vector3(0f, cell * .48f, 0f), pick ? new Color(.22f, .2f, .12f) : new Color(.07f, .07f, .1f)));
            for (int i = 0; i < cols; i++)
            {
                int e = rels[r].ex[i];
                Color cc = e == 0 ? new Color(.12f, .12f, .16f) : (e & 1) == 1 ? new Color(1f, .7f, .25f) : new Color(.3f, .42f, .75f);
                quads.Add((Cell(r, i) + Vector3.forward * .2f, new Vector3(cell * .44f, 0f, 0f), new Vector3(0f, cell * .44f, 0f), cc * (pick ? 1f : .75f)));
                if (labelSize > 0f && e > 0) ExpLabelWide(g, e.ToString(), Cell(r, i) + Vector3.back * .2f, labelSize * .3f, (e & 1) == 1 ? new Color(.1f, .08f, .05f) : Color.white);
            }

            if (labelSize > 0f)
            {
                ExpLabelWide(g, rels[r].x.ToString(), o + new Vector3(cell * .2f, -r * cell, -.2f), labelSize * .3f, pick ? Color.white : dimText, TextAlignmentOptions.Left);
                ExpLabelWide(g, rels[r].v.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), o + new Vector3(colX0 - cell * .9f, -r * cell, -.2f), labelSize * .3f,
                    pick ? Color.white : dimText, TextAlignmentOptions.Right);
            }
        }

        // the sum row
        int sr = rels.Count + 1;
        for (int i = 0; i < cols; i++)
        {
            Color cc = sum[i] == 0 ? new Color(.12f, .12f, .16f) : (sum[i] & 1) == 0 ? new Color(.35f, 1f, .5f) : new Color(1f, .3f, .3f);
            quads.Add((Cell(sr, i) + Vector3.forward * .2f, new Vector3(cell * .44f, 0f, 0f), new Vector3(0f, cell * .44f, 0f), cc));
            if (labelSize > 0f && sum[i] > 0) ExpLabelWide(g, sum[i].ToString(), Cell(sr, i) + Vector3.back * .2f, labelSize * .3f, new Color(.05f, .1f, .05f));
        }

        ExpQuads(g, "cells", quads, Factory.NoGlow);
        if (labelSize <= 0f) return;
        for (int i = 0; i < cols; i++) ExpLabelWide(g, baseP[i].ToString(), Cell(-1, i), labelSize * .32f, PrimeGold);
        ExpLabelWide(g, "x", o + new Vector3(cell * .2f, cell, -.2f), labelSize * .32f, PrimeGold, TextAlignmentOptions.Left);
        ExpLabelWide(g, "x² − N", o + new Vector3(colX0 - cell * .9f, cell, -.2f), labelSize * .32f, PrimeGold, TextAlignmentOptions.Right);
        ExpLabelWide(g, "sum of the lit rows:", o + new Vector3(colX0 - cell * .9f, -sr * cell, -.2f), labelSize * .3f, new Color(.4f, 1f, .55f), TextAlignmentOptions.Right);
        // the arithmetic
        System.Numerics.BigInteger X = 1, Y = 1;
        foreach (var rel in rels) if (System.Array.IndexOf(chosen, rel.x) >= 0) X = X * rel.x % N;
        List<string> half = new();
        for (int i = 0; i < cols; i++)
        {
            if (sum[i] == 0) continue;
            Y = Y * System.Numerics.BigInteger.ModPow(baseP[i], sum[i] / 2, N) % N;
            half.Add(sum[i] / 2 == 1 ? baseP[i].ToString() : baseP[i] + Superscript(sum[i] / 2));
        }

        long d = (long)System.Numerics.BigInteger.GreatestCommonDivisor(System.Numerics.BigInteger.Abs(X - Y), N);
        Vector3 t0 = o + new Vector3(colX0 + cols * cell + cell * 1.5f, -cell * .2f, -.2f);
        string[] lines =
        {
            $"N = {N:N0}",
            $"X = {string.Join(" · ", System.Array.ConvertAll(chosen, v => v.ToString()))}  ≡ {X} (mod N)",
            $"Y = {string.Join("·", half)}  ≡ {Y} (mod N)",
            "X² ≡ Y² (mod N), and X ≠ ±Y (mod N)",
            $"gcd(X − Y, N) = {d}",
            $"N = {N / d:N0} × {d:N0}",
        };
        for (int i = 0; i < lines.Length; i++)
            ExpLabelWide(g, lines[i], t0 + Vector3.down * (labelSize * .045f * i), labelSize * (i == lines.Length - 1 ? .42f : .32f), i == lines.Length - 1 ? PrimeGold : Color.white, TextAlignmentOptions.Left);
    }
}
