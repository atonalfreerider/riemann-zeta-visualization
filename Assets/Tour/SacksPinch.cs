using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Sections R–U (docs/experiments/README.md): pinching the spiral, and where the primes go.
///
/// R. The fold. Doubling the angle (θ → 2θ) lays the squares ray and the pronic ray on one ray; every monic quadratic
///    k² + Bk + C then straightens at offset −πR·Δ/2 from it, so the two half-combs merge into one comb of all
///    discriminants Δ ≡ 0, 1 (mod 4): perfect squares (the cancellation curves) on one side, negative Δ on the other,
///    and a period-8 stripe from the prime 2 (Δ ≡ 1 mod 8: every value even; Δ ≡ 5 mod 8: never even).
/// S. The residue pinch. Angle 2πn/m: the spiral collapses onto m arms, one per residue class mod m; the primes ride the
///    φ(m) arms of classes coprime to m (enriched m/φ(m)); the other arms are cancellation arms.
/// T. The pleat. Each turn is folded into L slots after the pronic ray (n = k(k+1) + c ↦ slot c mod L). Slots whose line
///    k² + k + j has no root mod any prime dividing L gather the primes: 11 and 17 for L = 30; 11, 17, 41, 101, 137, 167
///    for L = 210.
/// U. Many dimensions. The residues mod 3, 5 and 7 as three axes: the primes fill the 2·4·6 = 48 cells off the three
///    zero planes; parity is a fourth axis.
/// Every pinch clusters primes exactly as a sieve by the primes it sees, by at most Π p/(p − 1), and no further: its
/// brightest curve still carries composites made of larger primes (the white marks).
/// </summary>
public partial class SacksSpiral
{
    static readonly Color AntiColor = new(1f, .8f, .25f);          // anti-cancellation: no root mod any prime the pinch sees
    static readonly Color DeadColor = new(.75f, .18f, .16f);       // cancellation: every value divisible by a prime the pinch sees
    static readonly Color PartColor = new(.38f, .45f, .62f);        // partly cancelled
    static readonly Color LeftoverColor = new(.95f, .95f, 1f);      // composites that survive the pinch's sieve

    // ---- attention marks for still pictures (an animated Flare fades away; these stay) --------------------------

    /// <summary>A white glow at a point: drawn far over the bloom threshold, like the modules' flares, but persistent.</summary>
    public void ExpGlowMark(Transform g, Vector3 at, float radius, float strength = 1f)
    {
        Color w = new(strength, strength, strength);
        GameObject core = Factory.NewSphere("glow", radius * .45f, w, AttentionIntensity);
        core.transform.SetParent(g, false);
        core.transform.position = at;
        ExpRing(g, "glow ring", at, radius, w, radius * .08f, AttentionIntensity);
    }

    /// <summary>A persistent white halo on a line (a module would pulse it instead).</summary>
    void GlowLine(LineRenderer line, float widthScale, float strength)
    {
        if (line == null) return;
        AttentionHalo h = Halo(line, widthScale, strength);
        h.Decay = 0f;
    }

    static int RootsMod(int p, long B, long j)
    {
        int r = 0;
        for (long k = 0; k < p; k++) if (((k * k + B * k + j) % p + p) % p == 0) r++;
        return r;
    }

    static List<int> PrimesDividing(int L)
    {
        List<int> ps = new();
        int m = L;
        for (int p = 2; p * p <= m; p++)
        {
            if (m % p != 0) continue;
            ps.Add(p);
            while (m % p == 0) m /= p;
        }

        if (m > 1) ps.Add(m);
        return ps;
    }

    // ================================================================================================
    // R. the fold
    // ================================================================================================

    /// <summary>n on the f-fold spiral: radius R√n, angle 2πf√n, on the current surface.</summary>
    Vector3 FoldAt(double n, float f, float lift = 0f)
    {
        double s = System.Math.Sqrt(n), a = f * s;
        a -= System.Math.Floor(a);
        return ExpAt(R * (float)s, 2f * Mathf.PI * (float)a, lift);
    }

    /// <summary>Every number on the f-fold spiral: primes gold, squares red, pronic numbers blue, the rest dim.</summary>
    public void ExpFoldField(int nMax, float f)
    {
        Transform g = ExpGroup("R fold field").transform;
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        for (int n = 2; n <= nMax; n++)
        {
            int k = Mathf.FloorToInt(Mathf.Sqrt(n));
            bool p = IsPrime(n), sq = k * k == n, pr = k * (k + 1) == n;
            pts.Add(FoldAt(n, f));
            cols.Add(p ? Factory.Palette.Zero : sq ? SquareColor : pr ? PronicColor : DimDotColor);
            sizes.Add(p ? .26f : sq || pr ? .32f : .11f);
        }

        ExpPointCloud(g, "numbers", pts, cols, sizes, Factory.SoftGlowIntensity);
    }

    /// <summary>k² + Bk + C on the f-fold spiral (integer f), by the route angle 2πf(√n − k).</summary>
    LineRenderer ExpFoldLine(Transform g, int B, long C, int f, long nMax, Color color, float width, float lift = .12f)
    {
        float k1 = KMaxFor(1, B, C, nMax);
        List<Vector3> pts = new();
        int steps = Mathf.Max(2, Mathf.CeilToInt(k1 * RouteSteps));
        for (int i = 0; i <= steps; i++)
        {
            double k = k1 * i / (double)steps, n = k * k + B * k + C;
            if (n < 1.5) continue;
            double s = System.Math.Sqrt(n), a = f * (s - k);
            a -= System.Math.Floor(a);
            pts.Add(ExpAt(R * (float)s, 2f * Mathf.PI * (float)a, lift));
        }

        return pts.Count < 2 ? null : ExpLine(g, $"fold {Poly(1, B, C)}", width, color, Factory.SoftGlowIntensity, pts.ToArray());
    }

    /// <summary>Red: a perfect square (the line factors). Dark red: Δ ≡ 1 mod 8 (every value even). Gold: Δ ≡ 5 mod 8 (never even). Blue: Δ ≡ 0 mod 4.</summary>
    static Color DiscriminantColor(long D)
    {
        if (IsSquareLong(D)) return CancelCurveColor;
        long r8 = ((D % 8) + 8) % 8;
        return r8 == 1 ? DeadColor : r8 == 5 ? AntiColor : BetweenCurveColor;
    }

    /// <summary>The merged comb: every Δ ≡ 0, 1 (mod 4) in [dMin, dMax] as its line on the 2-fold spiral; Euler's line (Δ = −163) glows.</summary>
    public void ExpFoldComb(int dMin, int dMax, long nMax, float labelSize, bool euler)
    {
        Transform g = ExpGroup("R fold comb").transform;
        for (int D = dMin; D <= dMax; D++)
        {
            int B = ((D % 4) + 4) % 4;
            if (B > 1) continue;
            long C = (B - (long)D) / 4;
            Color col = DiscriminantColor(D);
            LineRenderer l = ExpFoldLine(g, B, C, 2, nMax, col, IsSquareLong(D) ? .09f : .05f);
            if (l != null && IsSquareLong(D) && D > 0) GlowLine(l, 1.8f, .35f);
            if (labelSize > 0f && l != null && D % 4 == 0 && D % 8 == 0 && System.Math.Abs(D) <= 32)
                ExpLabel(g, $"Δ {D}", l.GetPosition(l.positionCount - 1) + new Vector3(0f, 0f, -.5f), labelSize, col, TextAlignmentOptions.Left);
        }

        if (!euler) return;
        LineRenderer e = ExpFoldLine(g, 1, 41, 2, nMax, Factory.Palette.Zero, .1f, .2f);
        GlowLine(e, 1.8f, .3f);
        if (labelSize > 0f && e != null) ExpLabel(g, "Euler, Δ −163", e.GetPosition(e.positionCount - 1) + new Vector3(0f, .6f * labelSize, -.5f), labelSize, Factory.Palette.Zero);
    }

    /// <summary>The merged comb as bars, one per discriminant (top: largest Δ), prime counts for k ≤ kMax, coloured as on the spiral.</summary>
    public void ExpFoldSkyline(int dMin, int dMax, int kMax, Vector3 center, float rowH, float maxLen, float labelSize)
    {
        Transform g = ExpGroup("R fold skyline").transform;
        List<(int D, int B, long C, int cnt)> rows = new();
        int maxCount = 1;
        for (int D = dMin; D <= dMax; D++)
        {
            int B = ((D % 4) + 4) % 4;
            if (B > 1) continue;
            long C = (B - (long)D) / 4;
            int cnt = CombCount(B, C, kMax);
            rows.Add((D, B, C, cnt));
            maxCount = Mathf.Max(maxCount, cnt);
        }

        float mid = (rows.Count - 1) * .5f;
        for (int i = 0; i < rows.Count; i++)
        {
            (int D, int B, long C, int cnt) = rows[i];
            Vector3 a = center + new Vector3(0f, (i - mid) * rowH, 0f);
            Color col = DiscriminantColor(D);
            float len = Mathf.Max(.5f, cnt / (float)maxCount * maxLen);
            ExpLine(g, "bar " + D, rowH * .62f, col, IsSquareLong(D) ? Factory.GlowIntensity : Factory.SoftGlowIntensity, new[] { a, a + Vector3.right * len });
            if (D % 20 == 0) ExpLabel(g, $"Δ {D}", a + Vector3.left * (.4f * labelSize), labelSize * .8f, new Color(.75f, .75f, .85f), TextAlignmentOptions.Right);
            if (D == -163 || D == -67 || D == -43 || D == 5 || D == -4 || D == 1)
                ExpLabel(g, $"{Poly(1, B, C)}   {cnt}", a + Vector3.right * (len + 1f), labelSize * .8f, col, TextAlignmentOptions.Left);
        }

        Vector3 top = center + new Vector3(maxLen * .5f, (rows.Count - 1 - mid) * rowH, 0f);
        ExpLabel(g, $"one comb after the fold: primes for k ≤ {kMax:N0}", top + Vector3.up * (.3f * labelSize + 2f), labelSize, Color.white);
        ExpLabel(g, "gold Δ ≡ 5 mod 8: never even    dark red Δ ≡ 1 mod 8: always even\nblue Δ ≡ 0 mod 4    red: a square, the line factors", top + Vector3.up * (.1f * labelSize + 1f), labelSize * .65f, new Color(.8f, .8f, .9f));
    }

    // ================================================================================================
    // S. the residue pinch
    // ================================================================================================

    static int Gcd(int a, int b) { while (b != 0) { int t = a % b; a = b; b = t; } return a; }

    /// <summary>n slides around its own circle from its Sacks angle to the arm of its class mod m (s = 0 … 1).</summary>
    Vector3 ResidueAt(int n, int m, float s, float lift = .05f)
    {
        double sq = System.Math.Sqrt(n), a0 = sq - System.Math.Floor(sq), a1 = (double)(n % m) / m;
        double d = a1 - a0;
        d -= System.Math.Round(d);
        return ExpAt(R * (float)sq, 2f * Mathf.PI * (float)(a0 + s * d), lift);
    }

    public void ExpResiduePinch(int m, float s, int nMax, bool arms, float labelSize, bool marks, float labelAt = 1f)
    {
        Transform g = ExpGroup($"S residue pinch {m}").transform;
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        int[] count = new int[m];
        for (int n = 2; n <= nMax; n++)
        {
            bool p = IsPrime(n), unit = Gcd(n % m, m) == 1;
            if (p) count[n % m]++;
            pts.Add(ResidueAt(n, m, s));
            cols.Add(p ? Factory.Palette.Zero : unit ? PartColor : new Color(.42f, .16f, .15f));
            sizes.Add(p ? .26f : .12f);
        }

        ExpPointCloud(g, "numbers", pts, cols, sizes, Factory.SoftGlowIntensity);
        float rMax = R * Mathf.Sqrt(nMax);
        if (arms)
        {
            for (int j = 0; j < m; j++)
            {
                bool unit = Gcd(j, m) == 1;
                float th = 2f * Mathf.PI * j / m;
                Vector3[] line = new Vector3[60];
                for (int i = 0; i < 60; i++) line[i] = ExpAt(Mathf.Lerp(R, rMax, i / 59f), th, .02f);
                LineRenderer l = ExpLine(g, "arm " + j, unit ? .06f : .035f, unit ? AntiColor : DeadColor, unit ? Factory.GlowIntensity : Factory.NoGlow, line);
                if (unit && m <= 30) GlowLine(l, 2.2f, .45f);
                if (labelSize > 0f && m <= 30)
                    ExpLabel(g, unit ? $"{j}: {count[j]}" : $"{j}", ExpAt(rMax * labelAt + .12f * labelSize + 1.5f, th, labelAt < 1f ? .3f * labelSize : .5f), labelSize * (unit ? 1f : .7f), unit ? AntiColor : DeadColor);
            }
        }

        if (!marks || s < .99f) return;
        // the only primes on cancellation arms are the primes that divide m
        foreach (int p in PrimesDividing(m)) if (p <= nMax) ExpGlowMark(g, ResidueAt(p, m, 1f, .3f), 1.6f);
    }

    // ================================================================================================
    // T. the pleat
    // ================================================================================================

    static (int k, int c) PronicCoordinates(int n)
    {
        int k = ((int)System.Math.Sqrt(4.0 * n + 1) - 1) / 2;
        while ((long)(k + 1) * (k + 2) <= n) k++;
        while ((long)k * (k + 1) > n) k--;
        return (k, n - k * (k + 1));
    }

    /// <summary>The slot's kind: anti-cancelling (k² + k + j has no root mod any prime dividing L), dead (always even), or partly cancelled.</summary>
    static int SlotKind(int L, int j)
    {
        List<int> ps = PrimesDividing(L);
        if (L % 2 == 0 && j % 2 == 0) return -1;
        foreach (int p in ps) if (RootsMod(p, 1, j) > 0) return 0;
        return 1;
    }

    /// <summary>Prime fraction of each slot over n ≤ nStat divided by the overall prime fraction (pronic or squares coordinates).</summary>
    public static float[] SlotEnrichment(int L, int nStat, bool pronic)
    {
        ExpEnsureSieve(nStat + 1);
        long[] num = new long[L], pri = new long[L];
        long tn = 0, tp = 0;
        for (int n = 2; n <= nStat; n++)
        {
            int j;
            if (pronic) j = PronicCoordinates(n).c % L;
            else { int k = (int)System.Math.Sqrt(n); while ((long)k * k > n) k--; while ((long)(k + 1) * (k + 1) <= n) k++; j = (n - k * k) % L; }
            num[j]++; tn++;
            if (ExpPrime(n)) { pri[j]++; tp++; }
        }

        float[] e = new float[L];
        double dens = tp / (double)tn;
        for (int j = 0; j < L; j++) e[j] = num[j] == 0 ? 0 : (float)(pri[j] / (double)num[j] / dens);
        return e;
    }

    /// <summary>
    /// Each turn folded into L slots after the pronic ray: n = k(k+1) + c slides back along its turn to k(k+1) + (c mod L)
    /// (s = 0 … 1) and stacks along the surface normal by c div L. The slot curves k² + k + j are drawn with their kind;
    /// with <paramref name="marks"/> the composites on anti-cancelling slots (the pleat's leftovers) glow white.
    /// </summary>
    public int ExpPleat(int L, float s, int nMax, float stack, float labelSize, bool curves, bool marks, int nStat = 10000000)
    {
        Transform g = ExpGroup($"T pleat {L}").transform;
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        List<Vector3> leftovers = new();
        for (int n = 2; n <= nMax; n++)
        {
            (int k, int c) = PronicCoordinates(n);
            int j = c % L, q = c / L;
            double np = n - s * (c - j);
            Vector3 pos = ExpNumberAt(np, .05f) + SurfaceNormalAt((float)np) * (s * q * stack);
            bool p = IsPrime(n);
            int kind = SlotKind(L, j);
            pts.Add(pos);
            cols.Add(p ? Factory.Palette.Zero : kind == 1 ? LeftoverColor : kind == -1 ? new Color(.42f, .16f, .15f) : DimDotColor);
            sizes.Add(p ? .27f : kind == 1 ? .2f : .11f);
            if (!p && kind == 1) leftovers.Add(pos);
        }

        ExpPointCloud(g, "numbers", pts, cols, sizes, Factory.SoftGlowIntensity);
        if (curves)
        {
            float[] enrich = SlotEnrichment(L, nStat, true);
            int kMax = PronicCoordinates(nMax).k;
            for (int j = 0; j < L; j++)
            {
                int kind = SlotKind(L, j);
                int k0 = Mathf.Max(0, (j - 1 + 1) / 2);
                List<Vector3> line = new();
                for (int i = 0; i <= (kMax - k0) * RouteSteps; i++)
                {
                    double k = k0 + i / (double)RouteSteps, n = k * k + k + j;
                    if (n < 1.5 || n > nMax) continue;
                    line.Add(ExpCurveAt(n, k, .12f));
                }

                if (line.Count < 2) continue;
                Color col = kind == 1 ? AntiColor : kind == -1 ? DeadColor : PartColor;
                LineRenderer l = ExpLine(g, "slot " + j, kind == 1 ? .09f : .035f, col, kind == 1 ? Factory.GlowIntensity : Factory.NoGlow, line.ToArray());
                if (kind == 1) GlowLine(l, 2.2f, .6f);
                if (labelSize > 0f && kind == 1)
                    ExpLabel(g, $"k² + k + {j}   ×{enrich[j]:0.00}", line[^1] + new Vector3(0f, -.3f * labelSize - 1f, -.5f), labelSize, AntiColor);
            }
        }

        if (marks) foreach (Vector3 v in leftovers) ExpGlowMark(g, v + Vector3.back * .2f, 1.1f, .9f);
        return leftovers.Count;
    }

    /// <summary>Two columns of L bars: each slot's prime enrichment over n ≤ nStat, pronic coordinates (left) and squares coordinates (right).</summary>
    public void ExpSlotBars(int L, int nStat, Vector3 center, float rowH, float maxLen, float labelSize)
    {
        Transform g = ExpGroup("T slot bars " + L).transform;
        float[] pe = SlotEnrichment(L, nStat, true), se = SlotEnrichment(L, nStat, false);
        float maxE = 0f;
        for (int j = 0; j < L; j++) maxE = Mathf.Max(maxE, Mathf.Max(pe[j], se[j]));
        float gap = maxLen + 14f;
        for (int side = 0; side < 2; side++)
        {
            float[] e = side == 0 ? pe : se;
            Vector3 x0 = center + new Vector3(side == 0 ? -gap * .5f - maxLen * .5f : gap * .5f - maxLen * .5f, 0f, 0f);
            for (int j = 0; j < L; j++)
            {
                Vector3 a = x0 + new Vector3(0f, ((L - 1) * .5f - j) * rowH, 0f);
                int kind = side == 0 ? SlotKind(L, j) : 0;
                Color col = side == 0 ? (kind == 1 ? AntiColor : kind == -1 ? DeadColor : PartColor) : BetweenCurveColor;
                float len = Mathf.Max(.4f, e[j] / maxE * maxLen);
                LineRenderer l = ExpLine(g, $"bar {side} {j}", rowH * .62f, col, Factory.SoftGlowIntensity, new[] { a, a + Vector3.right * len });
                if (side == 0 && kind == 1) GlowLine(l, 1.6f, .5f);
                if (e[j] >= (side == 0 ? 3f : 1.8f) || j % 5 == 0)
                    ExpLabel(g, e[j] >= (side == 0 ? 3f : 1.8f) ? $"{j}  ×{e[j]:0.00}" : $"{j}", a + Vector3.right * (len + .8f), labelSize * .75f, col, TextAlignmentOptions.Left);
            }

            ExpLabel(g, side == 0 ? $"pronic side: n = k(k+1) + c\nslot c mod {L}" : $"squares side: n = k² + C\nslot C mod {L}",
                x0 + new Vector3(maxLen * .5f, ((L - 1) * .5f) * rowH + .2f * labelSize + 2.5f, 0f), labelSize, Color.white);
        }

        ExpLabel(g, $"prime fraction of each slot over n ≤ {nStat:N0}, relative to all numbers",
            center + new Vector3(0f, -((L - 1) * .5f) * rowH - .2f * labelSize - 2.5f, 0f), labelSize * .8f, new Color(.8f, .8f, .9f));
    }

    // ================================================================================================
    // U. many dimensions: the residue lattice
    // ================================================================================================

    /// <summary>
    /// Odd n ≤ nMax at the cell (n mod 3, n mod 5, n mod 7) of a lattice at <paramref name="center"/> (x, y, and toward the
    /// trail), a little cluster per cell; the three zero planes as red grids; with <paramref name="evenToo"/> the even
    /// numbers in a second lattice beside it (parity is the fourth axis). The primes on the zero planes glow.
    /// </summary>
    public void ExpResidueLattice(Vector3 center, float sp, int nMax, bool evenToo, float labelSize)
    {
        Transform g = ExpGroup("U lattice").transform;
        Vector3 Cell(int n, Vector3 basePoint)
        {
            int t = n / 210;
            float a = t * 2.39996f, r = sp * .11f * Mathf.Sqrt(t + .6f);
            Vector3 jitter = new(r * Mathf.Cos(a), r * Mathf.Sin(a), -.12f * sp * Mathf.Sin(a * 1.7f));
            return basePoint + new Vector3(n % 3 * sp, n % 5 * sp, -(n % 7) * sp) + jitter;
        }

        Vector3 evenBase = center + new Vector3(4.2f * sp, 0f, 0f);
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        for (int n = 2; n <= nMax; n++)
        {
            bool odd = n % 2 == 1;
            if (!odd && !evenToo) continue;
            bool p = IsPrime(n), onPlane = n % 3 == 0 || n % 5 == 0 || n % 7 == 0;
            pts.Add(Cell(n, odd ? center : evenBase));
            cols.Add(p ? Factory.Palette.Zero : !odd ? new Color(.22f, .1f, .1f) : onPlane ? new Color(.5f, .18f, .16f) : PartColor);
            sizes.Add(p ? .3f * sp / 6f : .16f * sp / 6f);
        }

        ExpPointCloud(g, "lattice", pts, cols, sizes, Factory.SoftGlowIntensity);
        void Planes(Vector3 b, float strength)
        {
            Color red = new(DeadColor.r * strength, DeadColor.g * strength, DeadColor.b * strength);
            float x1 = 2.5f * sp, y1 = 4.5f * sp, z1 = 6.5f * sp, h = .5f * sp;
            for (float y = -h; y <= y1 + .01f; y += sp) ExpLine(g, "plane x", .05f, red, Factory.NoGlow, new[] { b + new Vector3(0, y, h), b + new Vector3(0, y, -z1) });
            for (float z = -h; z <= z1 + .01f; z += sp) ExpLine(g, "plane x", .05f, red, Factory.NoGlow, new[] { b + new Vector3(0, -h, -z), b + new Vector3(0, y1, -z) });
            for (float x = -h; x <= x1 + .01f; x += sp) ExpLine(g, "plane y", .05f, red, Factory.NoGlow, new[] { b + new Vector3(x, 0, h), b + new Vector3(x, 0, -z1) });
            for (float z = -h; z <= z1 + .01f; z += sp) ExpLine(g, "plane y", .05f, red, Factory.NoGlow, new[] { b + new Vector3(-h, 0, -z), b + new Vector3(x1, 0, -z) });
            for (float x = -h; x <= x1 + .01f; x += sp) ExpLine(g, "plane z", .05f, red, Factory.NoGlow, new[] { b + new Vector3(x, -h, 0), b + new Vector3(x, y1, 0) });
            for (float y = -h; y <= y1 + .01f; y += sp) ExpLine(g, "plane z", .05f, red, Factory.NoGlow, new[] { b + new Vector3(-h, y, 0), b + new Vector3(x1, y, 0) });
        }

        Planes(center, 1f);
        foreach (int p in new[] { 3, 5, 7 }) ExpGlowMark(g, Cell(p, center), .32f * sp);
        if (labelSize > 0f)
        {
            ExpLabel(g, "n mod 3 →", center + new Vector3(1f * sp, -1.1f * sp, 0f), labelSize, Color.white);
            ExpLabel(g, "n mod 5 ↑", center + new Vector3(-1.3f * sp, 2f * sp, 0f), labelSize, Color.white, TextAlignmentOptions.Right);
            ExpLabel(g, "n mod 7", center + new Vector3(-1f * sp, -1f * sp, -6f * sp), labelSize, Color.white, TextAlignmentOptions.Right);
            ExpLabel(g, "odd numbers", center + new Vector3(1f * sp, 5.4f * sp, -3f * sp), labelSize, Color.white);
        }

        if (!evenToo) return;
        Planes(evenBase, .35f);
        ExpGlowMark(g, Cell(2, evenBase), .32f * sp);
        if (labelSize > 0f) ExpLabel(g, "even numbers", evenBase + new Vector3(1f * sp, 5.4f * sp, -3f * sp), labelSize, new Color(.7f, .5f, .5f));
    }

    // ================================================================================================
    // live: one coroutine for every pinch (for modules); s eases 0 → 1
    // ================================================================================================

    public void ExpPinchLive(string kind, int param, float seconds, int nMax)
    {
        StartLive(PinchLive(kind, param, seconds, nMax));
    }

    IEnumerator PinchLive(string kind, int param, float seconds, int nMax)
    {
        Transform g = SectionLiveRoot();
        List<Vector3> v0 = new(); List<Color> cols = new(); List<float> sizes = new();
        for (int n = 2; n <= nMax; n++)
        {
            bool p = IsPrime(n);
            v0.Add(Vector3.zero);
            Color c = p ? Factory.Palette.Zero : DimDotColor;
            if (!p && kind == "residue" && Gcd(n % param, param) != 1) c = new Color(.42f, .16f, .15f);
            if (!p && kind == "pleat") { int sk = SlotKind(param, PronicCoordinates(n).c % param); c = sk == 1 ? LeftoverColor : sk == -1 ? new Color(.42f, .16f, .15f) : DimDotColor; }
            cols.Add(c);
            sizes.Add(p ? .27f : .12f);
        }

        Mesh mesh = ExpPointCloud(g, "pinching", v0, cols, sizes, Factory.SoftGlowIntensity).GetComponent<MeshFilter>().sharedMesh;
        Vector3[] verts = new Vector3[v0.Count * 6];
        Vector3[] axes = { Vector3.right, Vector3.up, Vector3.forward };
        float time = 0f;
        while (true)
        {
            float u = seconds <= 0f ? 1f : Mathf.Clamp01(time / seconds);
            float s = Mathf.SmoothStep(0f, 1f, u);
            for (int i = 0; i < v0.Count; i++)
            {
                int n = i + 2;
                Vector3 p = kind switch
                {
                    "fold" => FoldAt(n, 1f + s * (param - 1)),
                    "residue" => ResidueAt(n, param, s),
                    _ => PleatAt(n, param, s, .6f)
                };
                float sz = sizes[i];
                verts[6 * i] = p + axes[0] * sz; verts[6 * i + 1] = p - axes[0] * sz;
                verts[6 * i + 2] = p + axes[1] * sz; verts[6 * i + 3] = p - axes[1] * sz;
                verts[6 * i + 4] = p + axes[2] * sz; verts[6 * i + 5] = p - axes[2] * sz;
            }

            mesh.vertices = verts;
            mesh.RecalculateBounds();
            if (u >= 1f) break;
            time += Time.deltaTime;
            yield return null;
        }

        liveRoutine = null;
    }

    Vector3 PleatAt(int n, int L, float s, float stack)
    {
        (int k, int c) = PronicCoordinates(n);
        int j = c % L, q = c / L;
        double np = n - s * (c - j);
        return ExpNumberAt(np, .05f) + SurfaceNormalAt((float)np) * (s * q * stack);
    }
}
