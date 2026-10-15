using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Sections Z1–Z4 (docs/experiments/README.md): mirrors, the sieve machine, the torus knot of residues, the umbilic torus.
///
/// Z1. Every number m is a mirror: m − j and m + j both prime is a Goldbach pair of 2m, and within one turn the two sit
///     symmetric across the ray through m. Mirrors on cancellation curves reflect the most prime pairs (the pronic curve,
///     2.15 × Euler's line) and mirrors on anti-cancellation curves the fewest: the count follows Goldbach's singular series
///     Π (p − 1)/(p − 2) over the odd primes dividing m. The chords live on a Möbius band (unordered pairs of angles), where
///     the squares mirror and the pronic mirror are one rung.
/// Z2. The sieve machine: Fermat's walk up the squares ray (is a² − n a square?) filtered by rings of residues, one per
///     modulus, with holes where a can still work; light passes only where every ring is open (D. H. Lehmer's machines).
///     For 1501 one candidate of 22 survives and for 50 399 one of 96, and each is the factorization.
/// Z3. The torus knot of residues: n at angles (2πn/7, 2πn/5) winds a torus knot with 35 beads; the primes above 7 sit only
///     on the 24 beads off the two cancellation circles.
/// Z4. The umbilic torus: one lap per turn; its 1/3 twist carries the single cusp line through the three lobes in turn, so
///     the lobes sort the turns by k mod 3 and every curve is cancelled by 3 on whole lobes.
/// </summary>
public partial class SacksSpiral
{
    public const float MachineOffset = 9000f;
    public const float KnotOffset = 9800f;
    public const float MobiusOffset = 10600f;
    public const float UmbilicOffset = 11400f;
    public const float SkylineOffset = 10200f;

    // the module text goes on the caption board (top centre, screen space), so it reads at any camera angle
    static string Small(string text) => "<size=58%>" + text + "</size>";

    /// <summary>Clear the caption board (exp:noboard).</summary>
    public static void ExpClearBoard() => Board("");
    static string Tint(Color c, string text) => "<color=#" + Hex(c) + ">" + text + "</color>";

    // ================================================================================================
    // Z1. mirrors
    // ================================================================================================

    static (System.Func<long, long> f, int B, long C, Color color, string name) MirrorCurve(string code) => code switch
    {
        "squares" => (k => k * k, 0, 0, SquareColor, "k²"),
        "euler" => (k => k * k + k + 41, 1, 41, EulerColor, "k² + k + 41"),
        "landau" => (k => k * k + 1, 0, 1, new Color(1f, .85f, .35f), "k² + 1"),
        "u6" => (k => k * (k + 6), 6, 0, new Color(1f, .6f, .3f), "u(u + 6)"),
        _ => (k => k * (k + 1), 1, 0, PronicColor, "k(k + 1)")
    };

    /// <summary>
    /// The mirror chords of a curve: for every value m = f(k), k0 ≤ k &lt; k1, each pair (m − j, m + j), j ≤ k, of primes is a
    /// chord across the ray through m, its ends gold. Returns the number of chords.
    /// </summary>
    public int ExpMirrorChords(string code, int k0, int k1, float lift, bool drawCurve, bool centers)
    {
        var (f, B, C, col, _) = MirrorCurve(code);
        Transform g = ExpGroup("Z1 mirrors " + code).transform;
        ExpEnsureSieve(f(k1) + k1 + 2);
        if (drawCurve) ExpQuadratic(g, 1, B, C, Mathf.Max(1, k0 - 1), f(k1), col, .08f, false, lift - .05f);
        List<Vector3> a = new(), b = new();
        List<Color> ca = new(), cb = new();
        List<Vector3> ends = new();
        List<Color> endCols = new();
        List<float> endSizes = new();
        int chords = 0;
        for (int k = k0; k < k1; k++)
        {
            long m = f(k);
            for (int j = 1; j <= k; j++)
            {
                if (m - j < 2 || !ExpPrime(m - j) || !ExpPrime(m + j)) continue;
                Vector3 p = ExpNumberAt(m - j, lift), q = ExpNumberAt(m + j, lift);
                a.Add(p); b.Add(q); ca.Add(col); cb.Add(col);
                ends.Add(p); ends.Add(q);
                endCols.Add(Factory.Palette.Zero); endCols.Add(Factory.Palette.Zero);
                endSizes.Add(.32f); endSizes.Add(.32f);
                chords++;
            }

            if (centers) ExpRing(g, "mirror " + m, ExpNumberAt(m, lift), .5f, col, .05f, Factory.SoftGlowIntensity);
        }

        if (a.Count > 0) ExpRibbons(g, "chords", a, b, ca, cb, .07f, Factory.GlowIntensity);
        if (ends.Count > 0) ExpPointCloud(g, "pair ends", ends, endCols, endSizes, Factory.GlowIntensity);
        return chords;
    }

    /// <summary>The same chords appearing mirror by mirror outward, each with a white pulse and flares at its ends; the board keeps count.</summary>
    public void ExpMirrorLive(string code, int k0, int k1, float seconds, float lift)
    {
        StartLive(MirrorLive(code, k0, k1, seconds, lift));
    }

    IEnumerator MirrorLive(string code, int k0, int k1, float seconds, float lift)
    {
        var (f, B, C, col, name) = MirrorCurve(code);
        Transform g = ExpGroup("Z1 mirrors live " + code).transform; // the chords stay until exp:clear
        ExpEnsureSieve(f(k1) + k1 + 2);
        ExpQuadratic(g, 1, B, C, Mathf.Max(1, k0 - 1), f(k1), col, .08f, false, lift - .05f);
        float per = seconds * .92f / Mathf.Max(1, k1 - k0), clock = 0f;
        int total = 0, empty = 0;
        for (int k = k0; k < k1; k++)
        {
            long m = f(k);
            int here = 0;
            Vector3 center = ExpNumberAt(m, lift);
            for (int j = 1; j <= k; j++)
            {
                if (m - j < 2 || !ExpPrime(m - j) || !ExpPrime(m + j)) continue;
                Vector3 p = ExpNumberAt(m - j, lift), q = ExpNumberAt(m + j, lift);
                LineRenderer l = ExpLine(g, $"chord {m - j} {m + j}", .08f, col, Factory.GlowIntensity, new[] { p, q });
                Halo(l, 2.4f, 1f).Decay = 1.1f;
                ExpDot(g, "end", p, .34f, Factory.Palette.Zero);
                ExpDot(g, "end", q, .34f, Factory.Palette.Zero);
                Flare(p, .9f, .6f, 1);
                Flare(q, .9f, .6f, 1);
                here++;
            }

            ExpRing(g, "mirror " + m, center, .5f, col, .05f, Factory.SoftGlowIntensity);
            total += here;
            if (here == 0) empty++;
            Board($"mirrors on {Tint(col, name)}:  {total} pairs\n" +
                  Small($"m = {m:N0}:  {here} pair{(here == 1 ? "" : "s")}      mirrors with none: {empty} of {k - k0 + 1}"));
            float until = (k - k0 + 1) * per;
            while (clock < until)
            {
                yield return null;
                clock += Time.deltaTime;
            }
        }

        liveRoutine = null;
    }

    static List<int> smallPrimes;

    static double SingularFactor(long m)
    {
        if (smallPrimes == null)
        {
            smallPrimes = new List<int>();
            for (int p = 3; p < 5000; p += 2)
            {
                bool pr = true;
                for (int d = 3; d * d <= p; d += 2) if (p % d == 0) { pr = false; break; }
                if (pr) smallPrimes.Add(p);
            }
        }

        double s = 1;
        while (m % 2 == 0) m /= 2;
        foreach (int p in smallPrimes)
        {
            if ((long)p * p > m) break;
            if (m % p != 0) continue;
            s *= (p - 1.0) / (p - 2.0);
            while (m % p == 0) m /= p;
        }

        if (m > 2) s *= (m - 1.0) / (m - 2.0);
        return s;
    }

    /// <summary>
    /// Mirror power of each curve: prime mirror pairs within one turn per k / log² m for k0 ≤ k &lt; k1, relative to Euler's line
    /// (bars), against the mean of Goldbach's singular series Π (p − 1)/(p − 2) over the odd primes dividing m (white ticks).
    /// </summary>
    readonly List<LineRenderer> skylineTicks = new();

    /// <summary>Flare the skyline's prediction ticks (exp:goldbachticks): the white marks are the point of the line.</summary>
    public void ExpSkylineTicksPulse()
    {
        foreach (LineRenderer tick in skylineTicks)
        {
            if (tick == null) continue;
            Halo(tick, 3.2f, 1f).Pulse(3f);
            Flare((tick.GetPosition(0) + tick.GetPosition(1)) * .5f, 1.6f, 2.4f, 3);
        }
    }

    public void ExpMirrorSkyline(Vector3 center, float rowH, float maxLen, float labelSize, bool legend = true, int k0 = 1000, int k1 = 3000)
    {
        Transform g = ExpGroup("Z1 mirror skyline").transform;
        skylineTicks.Clear();
        string[] codes = { "pronic", "u6", "squares", "landau", "euler" };
        double[] power = new double[codes.Length], sing = new double[codes.Length];
        for (int i = 0; i < codes.Length; i++)
        {
            var (f, _, _, _, _) = MirrorCurve(codes[i]);
            ExpEnsureSieve(f(k1) + k1 + 2);
            double count = 0, norm = 0, s = 0;
            for (int k = k0; k < k1; k++)
            {
                long m = f(k);
                int c = 0;
                for (int j = 1; j <= k; j++) if (ExpPrime(m - j) && ExpPrime(m + j)) c++;
                count += c;
                double lg = System.Math.Log(m);
                norm += k / (lg * lg);
                s += SingularFactor(m);
            }

            power[i] = count / norm;
            sing[i] = s / (k1 - k0);
        }

        int euler = System.Array.IndexOf(codes, "euler");
        double unit = power[euler], sUnit = sing[euler];
        double max = 0;
        for (int i = 0; i < codes.Length; i++) max = System.Math.Max(max, power[i] / unit);
        for (int i = 0; i < codes.Length; i++)
        {
            var (_, _, _, col, name) = MirrorCurve(codes[i]);
            Vector3 a = center + new Vector3(-maxLen * .5f, ((codes.Length - 1) * .5f - i) * rowH, 0f);
            float len = (float)(power[i] / unit / max) * maxLen;
            LineRenderer bar = ExpLine(g, "bar " + codes[i], rowH * .55f, col, Factory.GlowIntensity, new[] { a, a + Vector3.right * len });
            if (i == 0) GlowLine(bar, 1.3f, .4f);
            float tick = (float)(sing[i] / sUnit / max) * maxLen;
            skylineTicks.Add(ExpLine(g, "prediction " + codes[i], .12f, Color.white, AttentionIntensity, new[] { a + new Vector3(tick, -rowH * .38f, -.1f), a + new Vector3(tick, rowH * .38f, -.1f) }));
            ExpLabelWide(g, $"{name}   ×{power[i] / unit:0.00}", a + Vector3.left * (.25f * labelSize), labelSize, col, TextAlignmentOptions.Right);
        }

        Board("prime pairs per mirror\n" + Small($"relative to Euler's line, turns {k0:N0} to {k1:N0}\nwhite ticks: Goldbach's singular series"));
        if (legend)
            ExpLabelWide(g, $"prime pairs per mirror, turns {k0:N0} to {k1:N0},\nrelative to Euler's line\nwhite ticks: Goldbach's singular series",
                center + new Vector3(-.36f * maxLen, ((codes.Length - 1) * .5f + .3f) * rowH + 3f + .16f * labelSize, 0f), labelSize * .87f, Color.white);
    }

    // ---- the Möbius band of chords: unordered pairs of angles --------------------------------------------------------------

    /// <summary>The Möbius band at φ ∈ [0, 4π) (one lap per 2π, the band flipping each lap) and t ∈ [−1, 1].</summary>
    static Vector3 MobiusPoint(Vector3 center, float Rb, float W, float phi, float t)
    {
        float h = phi * .5f;
        return center + new Vector3((Rb + t * W * Mathf.Cos(h)) * Mathf.Cos(phi), (Rb + t * W * Mathf.Cos(h)) * Mathf.Sin(phi), t * W * Mathf.Sin(h));
    }

    /// <summary>The point of the band for the unordered pair of angles {θ1, θ2}: centre c = θ1 + d/2, half-width w = d/2, d = (θ2 − θ1) mod 2π.</summary>
    static Vector3 ChordOnMobius(Vector3 center, float Rb, float W, double th1, double th2)
    {
        double d = (th2 - th1) % (2 * System.Math.PI);
        if (d < 0) d += 2 * System.Math.PI;
        double c = th1 + d / 2, w = d / 2;
        return MobiusPoint(center, Rb, W, (float)(2 * c), (float)((w - System.Math.PI / 2) / (System.Math.PI / 2)));
    }

    static readonly Dictionary<string, int> mobiusCounts = new();

    static void MobiusBoard()
    {
        List<string> counts = new();
        foreach (string code in new[] { "pronic", "squares", "euler", "landau", "u6" })
        {
            if (!mobiusCounts.TryGetValue(code, out int c)) continue;
            var (_, _, _, col, name) = MirrorCurve(code);
            counts.Add(Tint(col, name) + $"  {c}");
        }

        Board("chords on a Möbius band\n" + Small("white edge: equal angles\nglowing rung: the x-axis mirror") +
              (counts.Count > 0 ? "\n" + Small(string.Join("      ", counts)) : ""));
    }

    /// <summary>The Möbius band of unordered pairs of angles: rungs, the white edge (equal angles), the core (antipodal pairs), and the glowing rung of the x-axis mirror.</summary>
    public void ExpMobiusBand(Vector3 center, float Rb, float W, float labelSize)
    {
        Transform g = ExpGroup("Z1 mobius band").transform;
        mobiusCounts.Clear();
        MobiusBoard();
        Color wire = new(.3f, .32f, .42f);
        for (int i = 0; i < 24; i++)
        {
            float phi = i / 24f * 2f * Mathf.PI; // the rung at φ + 2π is the rung at φ, reversed
            Vector3[] rung = new Vector3[21];
            for (int s = 0; s <= 20; s++) rung[s] = MobiusPoint(center, Rb, W, phi, -1f + s / 10f);
            ExpLine(g, "rung", .05f, wire, Factory.NoGlow, rung);
        }

        foreach (float t in new[] { -1f, -.5f, 0f })
        {
            int steps = t == 0f ? 120 : 240;
            float span = t == 0f ? 2f * Mathf.PI : 4f * Mathf.PI;
            Vector3[] line = new Vector3[steps + 1];
            for (int s = 0; s <= steps; s++) line[s] = MobiusPoint(center, Rb, W, s / (float)steps * span, t);
            ExpLine(g, t == -1f ? "edge: the diagonal" : t == 0f ? "core: antipodal pairs" : "parallel", t == -1f ? .12f : .05f, t == -1f ? Color.white : wire, t == -1f ? Factory.SoftGlowIntensity : Factory.NoGlow, line);
        }

        // the rung of the x-axis mirror (c ≡ 0 mod π): the chords of the squares mirror and of the pronic mirror both lie on
        // it, the short squares chords at one end and the short pronic chords at the other
        Vector3[] axis = new Vector3[41];
        for (int s = 0; s <= 40; s++) axis[s] = MobiusPoint(center, Rb, W, 0f, -1f + s / 20f);
        LineRenderer ax = ExpLine(g, "x-axis rung", .14f, Color.white, AttentionIntensity, axis);
        GlowLine(ax, 1.6f, .5f);
        if (labelSize > 0f)
        {
            ExpLabelWide(g, "every chord is an unordered pair of angles:\na point of a Möbius band", center + new Vector3(0f, Rb + W + .25f * labelSize + 3f, 0f), labelSize, Color.white);
            ExpLabelWide(g, "white edge: equal angles\nglowing rung: the x-axis mirror", center + new Vector3(0f, -Rb - W - .25f * labelSize - 3f, 0f), labelSize * .8f, new Color(.8f, .8f, .9f));
        }
    }

    /// <summary>The prime chords of a curve's mirrors (k0 ≤ k &lt; k1, j ≤ k) as points of the Möbius band.</summary>
    static List<Vector3> MobiusChordPoints(Vector3 center, float Rb, float W, System.Func<long, long> f, int k0, int k1, List<int> mirrorOf)
    {
        List<Vector3> pts = new();
        for (int k = k0; k < k1; k++)
        {
            long m = f(k);
            for (int j = 1; j <= k; j++)
            {
                if (m - j < 2 || !ExpPrime(m - j) || !ExpPrime(m + j)) continue;
                double s1 = System.Math.Sqrt(m - j), s2 = System.Math.Sqrt(m + j);
                pts.Add(ChordOnMobius(center, Rb, W, 2 * System.Math.PI * (s1 - System.Math.Floor(s1)), 2 * System.Math.PI * (s2 - System.Math.Floor(s2))));
                mirrorOf?.Add(k);
            }
        }

        return pts;
    }

    /// <summary>Where a curve's chord count is written under the band: one row per curve, so several counts can stand together.</summary>
    static Vector3 MobiusCountAt(Vector3 center, float Rb, float W, string code, float labelSize)
    {
        int row = code switch { "pronic" => 0, "squares" => 1, "euler" => 2, _ => 3 };
        return center + new Vector3(0f, -Rb - W - .25f * labelSize - 3f - (.55f + .3f * row) * labelSize, 0f);
    }

    public void ExpMobiusPoints(Vector3 center, float Rb, float W, string code, int k0, int k1, float labelSize = 0f)
    {
        var (f, _, _, col, name) = MirrorCurve(code);
        ExpEnsureSieve(f(k1) + k1 + 2);
        Transform g = ExpGroup("Z1 mobius points " + code).transform;
        List<Vector3> pts = MobiusChordPoints(center, Rb, W, f, k0, k1, null);
        List<Color> cols = new(); List<float> sizes = new();
        foreach (Vector3 _ in pts) { cols.Add(col); sizes.Add(.5f); }
        if (pts.Count > 0) ExpPointCloud(g, "chords " + code, pts, cols, sizes, Factory.GlowIntensity);
        mobiusCounts[code] = pts.Count;
        MobiusBoard();
        if (labelSize > 0f) ExpLabelWide(g, $"{name}:  {pts.Count} chords", MobiusCountAt(center, Rb, W, code, labelSize), labelSize * .8f, col);
    }

    public void ExpMobiusChords(Vector3 center, float Rb, float W, string[] codes, int k0, int k1, float labelSize)
    {
        ExpMobiusBand(center, Rb, W, labelSize);
        foreach (string code in codes) ExpMobiusPoints(center, Rb, W, code, k0, k1, labelSize);
    }

    /// <summary>A curve's chords arriving on the band mirror by mirror, each mirror's points flaring; a running count.</summary>
    public void ExpMobiusLive(Vector3 center, float Rb, float W, string code, int k0, int k1, float seconds, float labelSize)
    {
        StartLive(MobiusLive(center, Rb, W, code, k0, k1, seconds, labelSize));
    }

    IEnumerator MobiusLive(Vector3 center, float Rb, float W, string code, int k0, int k1, float seconds, float labelSize)
    {
        var (f, _, _, col, name) = MirrorCurve(code);
        Transform g = ExpGroup("Z1 mobius live " + code).transform;
        ExpEnsureSieve(f(k1) + k1 + 2);
        List<int> mirrorOf = new();
        List<Vector3> pts = MobiusChordPoints(center, Rb, W, f, k0, k1, mirrorOf);
        TextBox counter = labelSize > 0f ? ExpLabelWide(g, "", MobiusCountAt(center, Rb, W, code, labelSize), labelSize * .8f, col) : null;
        float per = seconds * .92f / Mathf.Max(1, k1 - k0), clock = 0f;
        int i = 0;
        for (int k = k0; k < k1; k++)
        {
            while (i < pts.Count && mirrorOf[i] == k)
            {
                ExpDot(g, "chord", pts[i], .75f, col);
                Flare(pts[i], .8f, .6f, 1);
                i++;
            }

            mobiusCounts[code] = i;
            MobiusBoard();
            if (counter != null) counter.Text = $"{name}:  {i} chord{(i == 1 ? "" : "s")}";
            float until = (k - k0 + 1) * per;
            while (clock < until)
            {
                yield return null;
                clock += Time.deltaTime;
            }
        }

        liveRoutine = null;
    }

    // ================================================================================================
    // Z2. the sieve machine
    // ================================================================================================

    static HashSet<int> SquaresModulo(int m)
    {
        HashSet<int> s = new();
        for (int x = 0; x < m; x++) s.Add(x * x % m);
        return s;
    }

    /// <summary>a can still work mod m when a² − n is a square mod m.</summary>
    static bool FermatOpen(long n, long a, int m, HashSet<int> squares) => squares.Contains((int)(((a * a - n) % m + m) % m));

    static long CeilSqrt(long n)
    {
        long a = (long)System.Math.Sqrt(n);
        while (a * a < n) a++;
        while ((a - 1) * (a - 1) >= n) a--;
        return a;
    }

    /// <summary>
    /// The record of the sieve: one row per candidate a (top to bottom), one column per modulus; gold where a can still work
    /// mod m, dark red where it cannot. The rows wrap into blocks of <paramref name="perBlock"/> side by side (a portrait frame).
    /// Rows that pass every column glow white; a is written beside every fifth row, the ends and the survivors.
    /// </summary>
    public void ExpSieveBoard(Vector3 center, long n, long a0, long a1, int[] mods, float cell, float labelSize, int perBlock = 0, long upTo = long.MaxValue)
    {
        Transform g = ExpGroup("Z2 sieve board").transform;
        int rows = (int)(a1 - a0 + 1), cols = mods.Length;
        if (perBlock <= 0) perBlock = rows;
        int blocks = (rows + perBlock - 1) / perBlock, tall = Mathf.Min(rows, perBlock);
        const float gap = 4f; // cells between blocks: room for the a labels
        float width = blocks * cols + (blocks - 1) * gap;
        Vector3 Cell(int row, int col)
        {
            int b = row / perBlock, r = row % perBlock;
            return center + new Vector3((b * (cols + gap) + col - (width - 1) * .5f) * cell, ((tall - 1) * .5f - r) * cell, 0f);
        }

        HashSet<int>[] squares = new HashSet<int>[cols];
        for (int i = 0; i < cols; i++) squares[i] = SquaresModulo(mods[i]);
        List<(Vector3, Vector3, Vector3, Color)> quads = new();
        List<long> survivors = new();
        for (int row = 0; row < rows; row++)
        {
            long a = a0 + row;
            bool shown = a <= upTo, all = true;
            for (int i = 0; i < cols; i++)
            {
                bool open = FermatOpen(n, a, mods[i], squares[i]);
                all &= open;
                Color col = !shown ? new Color(.07f, .07f, .09f) : open ? new Color(.85f, .66f, .18f) : new Color(.3f, .09f, .09f);
                quads.Add((Cell(row, i) + Vector3.forward * .05f, Vector3.right * cell * .42f, Vector3.up * cell * .42f, col));
            }

            if (shown && all)
            {
                LineRenderer beam = ExpLine(g, "survivor " + a, cell * .5f, Color.white, AttentionIntensity,
                    new[] { Cell(row, 0) + new Vector3(-cell, 0f, -.3f), Cell(row, cols - 1) + new Vector3(cell, 0f, -.3f) });
                GlowLine(beam, 1.4f, .6f);
            }

            if (shown && all) survivors.Add(a);
            if (labelSize > 0f && (row == 0 || row == rows - 1 || (shown && all) || a % 5 == 0))
                ExpLabelWide(g, a.ToString(), Cell(row, 0) + Vector3.left * (cell * 1.1f), labelSize * (shown && all ? .8f : .55f), shown && all ? Color.white : new Color(.65f, .65f, .75f), TextAlignmentOptions.Right);
        }

        Board("the record\n" + Small("one row per candidate a, one column per ring\n" +
              (survivors.Count == 1 ? $"only a = {survivors[0]} passes every ring" : $"{survivors.Count} rows pass every ring")));

        ExpQuads(g, "cells", quads, Factory.SoftGlowIntensity);
        if (labelSize > 0f)
            for (int b = 0; b < blocks; b++)
                for (int i = 0; i < cols; i++)
                    ExpLabel(g, mods[i].ToString(), Cell(b * perBlock, i) + Vector3.up * (cell * .8f + .05f * labelSize), labelSize * .5f, new Color(.75f, .75f, .85f));
    }

    /// <summary>
    /// The lock: one ring per modulus, concentric (the smallest modulus innermost), with a hole for every residue r at r/m of a
    /// turn clockwise from twelve o'clock; the holes where a can still work are gold, the rest dark. The window is the slot at
    /// twelve o'clock. Each step a → a + 1 turns every ring one hole, so the hole for a mod m faces the window; light from the
    /// centre runs out along the slot until the first closed ring stops it, and leaves the dial only when every ring is open
    /// there. It stops at the first a with a² − n a square, and writes the factorization.
    /// </summary>
    public void ExpLehmerLive(Vector3 center, long n, long a1, int[] mods, float seconds, float labelSize, bool legend = true)
    {
        StartLive(LehmerLive(center, n, a1, mods, seconds, labelSize, legend));
    }

    public const float DialInner = 5f, DialStep = 2.6f;
    readonly List<Transform> dialOpenHoles = new();

    /// <summary>Flare every gold hole of the sieve dial (exp:dialholes): the residues where a² − n can still be a square.</summary>
    public void ExpDialHolesPulse()
    {
        foreach (Transform h in dialOpenHoles) if (h != null) Flare(h.position + Vector3.back * .3f, .9f, 2.4f, 2);
    }

    /// <summary>A white ring laid over ring <paramref name="index"/> of the sieve dial (the ring that never closes), with flares all round it.</summary>
    public void ExpDialRingPulse(Vector3 center, int index)
    {
        float rad = DialInner + DialStep * index;
        Transform parent = sectionLive != null ? sectionLive.transform : LiveRoot(); // goes with the dial
        Vector3[] circle = new Vector3[97];
        for (int s = 0; s <= 96; s++)
        {
            float a = s / 96f * 2f * Mathf.PI;
            circle[s] = center + new Vector3(Mathf.Sin(a) * rad, Mathf.Cos(a) * rad, -.4f);
        }

        GlowLine(ExpLine(parent, "never closes", .3f, Color.white, AttentionIntensity, circle), 1.8f, .7f);
        for (int i = 0; i < 16; i++)
        {
            float a = i / 16f * 2f * Mathf.PI;
            Flare(center + new Vector3(Mathf.Sin(a) * rad, Mathf.Cos(a) * rad, -.5f), 1.2f, 4f, 4);
        }
    }

    /// <summary>Outer radius of the sieve dial for <paramref name="rings"/> rings.</summary>
    public static float DialRadius(int rings) => DialInner + DialStep * (rings - 1);

    IEnumerator LehmerLive(Vector3 center, long n, long a1, int[] mods, float seconds, float labelSize, bool legend)
    {
        Transform g = SectionLiveRoot();
        long a0 = CeilSqrt(n);
        int rings = mods.Length;
        float Radius(int i) => DialInner + DialStep * i;
        float outer = DialRadius(rings);
        Transform[] pivots = new Transform[rings];
        HashSet<int>[] squares = new HashSet<int>[rings];
        Color gold = new(1f, .78f, .25f), dark = new(.42f, .12f, .1f), rimColor = new(.38f, .38f, .48f);
        dialOpenHoles.Clear();
        for (int i = 0; i < rings; i++)
        {
            int m = mods[i];
            squares[i] = SquaresModulo(m);
            GameObject pivot = new("ring mod " + m);
            pivot.transform.SetParent(g, false);
            pivot.transform.position = center;
            pivots[i] = pivot.transform;
            float rad = Radius(i);
            Vector3[] circle = new Vector3[97];
            for (int s = 0; s <= 96; s++)
            {
                float a = s / 96f * 2f * Mathf.PI;
                circle[s] = new Vector3(Mathf.Sin(a) * rad, Mathf.Cos(a) * rad, 0f);
            }

            LineRenderer rim = ExpLine(pivot.transform, "rim", .09f, rimColor, Factory.NoGlow, circle);
            rim.useWorldSpace = false;
            float size = Mathf.Min(1.5f, rad * 2f * Mathf.PI / m * .42f);
            for (int r = 0; r < m; r++)
            {
                bool open = FermatOpen(n, r, m, squares[i]);
                float a = r / (float)m * 2f * Mathf.PI;
                GameObject h = Factory.NewSphere($"hole {r}", open ? size : size * .55f, open ? gold : dark, open ? Factory.GlowIntensity : Factory.NoGlow);
                h.transform.SetParent(pivot.transform, false);
                h.transform.localPosition = new Vector3(Mathf.Sin(a) * rad, Mathf.Cos(a) * rad, 0f);
                if (open) dialOpenHoles.Add(h.transform);
            }

            if (labelSize > 0f)
                ExpLabelWide(g, m.ToString(), center + new Vector3(-1.6f, rad, -.4f), labelSize * .5f, new Color(.78f, .78f, .9f), TextAlignmentOptions.Right);
        }

        // the window: a slot at twelve o'clock, and the light running out along it
        Vector3 Slot(float r) => center + new Vector3(0f, r, -.3f);
        Color frame = new(.6f, .6f, .7f);
        ExpLine(g, "window left", .07f, frame, Factory.SoftGlowIntensity, new[] { Slot(DialInner - 1.6f) + Vector3.left * 1.1f, Slot(outer + 1.6f) + Vector3.left * 1.1f });
        ExpLine(g, "window right", .07f, frame, Factory.SoftGlowIntensity, new[] { Slot(DialInner - 1.6f) + Vector3.right * 1.1f, Slot(outer + 1.6f) + Vector3.right * 1.1f });
        LineRenderer beam = ExpLine(g, "light", .55f, Color.white, AttentionIntensity, new[] { Slot(0f), Slot(0f) });
        AttentionHalo halo = Halo(beam, 2.4f, 0f);
        ExpDot(g, "lamp", center + Vector3.back * .3f, 1.6f, Color.white, AttentionIntensity);
        TextBox label = legend && labelSize > 0f ? ExpLabelWide(g, "", center + new Vector3(0f, -outer - 3f - .12f * labelSize, -.4f), labelSize * .75f, Color.white) : null;
        TextBox tally = legend && labelSize > 0f ? ExpLabelWide(g, "", center + new Vector3(0f, -outer - 3f - .42f * labelSize, -.4f), labelSize * .55f, new Color(.75f, .75f, .85f)) : null;
        float[] angle = new float[rings];
        for (int i = 0; i < rings; i++)
        {
            angle[i] = 360f * (((a0 - 1) % mods[i] + mods[i]) % mods[i]) / mods[i];
            pivots[i].rotation = Quaternion.AngleAxis(angle[i], Vector3.forward);
        }

        if (seconds <= 0f)
        {
            // hold: the dial set at the first candidate, nothing turning yet
            if (label != null) label.Text = $"n = {n:N0}:   a starts at {a0},   the first a with a² ≥ n";
            Board($"n = {n:N0}\n" + Small($"a starts at {a0}, the first a with a² ≥ n"));
            liveRoutine = null;
            yield break;
        }

        int steps = (int)(a1 - a0 + 1), tried = 0, through = 0;
        // an absolute schedule (step i runs from i·per to (i + 1)·per), so the whole walk takes `seconds` at any frame rate
        float per = seconds * .92f / Mathf.Max(1, steps), turn = per * .4f, run = per * .25f, clock = 0f;
        float reach = 0f;
        for (int step = 0; step < steps; step++)
        {
            long a = a0 + step;
            float s0 = step * per;
            // every ring turns one hole forward (counterclockwise), together; the light goes out while they turn
            float[] from = (float[])angle.Clone();
            float startReach = reach;
            while (clock < s0 + turn)
            {
                float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((clock - s0) / turn));
                for (int i = 0; i < rings; i++) pivots[i].rotation = Quaternion.AngleAxis(from[i] + s * 360f / mods[i], Vector3.forward);
                reach = Mathf.Lerp(startReach, 0f, s);
                beam.SetPosition(1, Slot(reach));
                yield return null;
                clock += Time.deltaTime;
            }

            for (int i = 0; i < rings; i++)
            {
                angle[i] = (from[i] + 360f / mods[i]) % 360f;
                pivots[i].rotation = Quaternion.AngleAxis(angle[i], Vector3.forward);
            }

            tried++;
            int blocked = -1;
            for (int i = 0; i < rings && blocked < 0; i++) if (!FermatOpen(n, a, mods[i], squares[i])) blocked = i;
            long r2 = a * a - n, b = (long)System.Math.Sqrt(r2);
            while (b * b > r2) b--;
            while ((b + 1) * (b + 1) <= r2) b++;
            bool square = b * b == r2;
            float target = blocked < 0 ? outer + 4f : Radius(blocked) - .9f;
            if (blocked < 0) through++;
            // the light runs out along the slot to the first closed ring
            halo.Strength = blocked < 0 ? 1f : .35f;
            while (clock < s0 + turn + run)
            {
                reach = Mathf.Lerp(0f, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((clock - s0 - turn) / run)));
                beam.SetPosition(1, Slot(reach));
                yield return null;
                clock += Time.deltaTime;
            }

            reach = target;
            beam.SetPosition(1, Slot(reach));
            if (blocked < 0) Flare(Slot(outer + 4f), 2f, square ? 2f : 1f, square ? 2 : 1);
            else Flare(Slot(Radius(blocked)), 1.2f, .5f, 1);
            string tallyText = $"tried {tried},  light through {through}";
            if (square)
                Board($"{n:N0} = {a - b} × {a + b}\n" + Small($"a = {a}:  a² − {n:N0} = {r2:N0} = {b}²\n{tallyText}"));
            else if (blocked < 0)
                Board($"a = {a}:  every ring open\n" + Small($"a² − {n:N0} = {r2:N0}, not a square\n{tallyText}"));
            else
                Board($"a = {a}\n" + Small($"stopped by the ring mod {mods[blocked]}\n{tallyText}"));
            if (label != null)
            {
                label.Text = square ? $"a = {a}:   a² − {n:N0} = {r2:N0} = {b}²     {n:N0} = {a - b} × {a + b}"
                    : blocked < 0 ? $"a = {a}:   every ring open,   a² − {n:N0} = {r2:N0}" : $"a = {a}:   stopped by the ring mod {mods[blocked]}";
                label.Color = square ? Factory.Palette.Zero : Color.white;
            }

            if (tally != null) tally.Text = $"candidates tried {tried},   light through {through}";
            if (square)
            {
                halo.Strength = 1f;
                for (int i = 0; i < rings; i++) Flare(Slot(Radius(i)), 1.4f, 1.6f, 2);
                break;
            }

            while (clock < s0 + per)
            {
                yield return null;
                clock += Time.deltaTime;
            }
        }

        liveRoutine = null;
    }

    // ================================================================================================
    // Z3. the torus knot of residues
    // ================================================================================================

    static Vector3 TorusAt(Vector3 center, float R, float r, float phi, float psi) =>
        center + new Vector3((R + r * Mathf.Cos(psi)) * Mathf.Cos(phi), (R + r * Mathf.Cos(psi)) * Mathf.Sin(phi), -r * Mathf.Sin(psi));

    /// <summary>
    /// n at φ = 2πn/q round the ring and ψ = 2πn/p round the tube: the integers wind a torus knot with pq beads. Bead size by the
    /// number of primes up to nStat in its class; the two cancellation circles (n ≡ 0 mod p, n ≡ 0 mod q) in red, the primes
    /// p and q on them glowing; with <paramref name="euler"/> the beads Euler's values visit ringed in green.
    /// </summary>
    public void ExpTorusKnot(Vector3 center, float R, float r, int p, int q, long nStat, bool euler, float labelSize, bool legend = true)
    {
        Transform g = ExpGroup("Z3 torus knot").transform;
        int L = p * q;
        Color wire = new(.25f, .27f, .36f);
        for (int i = 0; i < 12; i++)
        {
            Vector3[] mer = new Vector3[41], par = new Vector3[121];
            for (int s = 0; s <= 40; s++) mer[s] = TorusAt(center, R, r, i / 12f * 2f * Mathf.PI, s / 40f * 2f * Mathf.PI);
            for (int s = 0; s <= 120; s++) par[s] = TorusAt(center, R, r, s / 120f * 2f * Mathf.PI, i / 12f * 2f * Mathf.PI);
            ExpLine(g, "meridian", .04f, wire, Factory.NoGlow, mer);
            ExpLine(g, "parallel", .04f, wire, Factory.NoGlow, par);
        }

        // the cancellation circles
        Vector3[] cp = new Vector3[121], cq = new Vector3[41];
        for (int s = 0; s <= 120; s++) cp[s] = TorusAt(center, R, r + .15f, s / 120f * 2f * Mathf.PI, 0f);
        for (int s = 0; s <= 40; s++) cq[s] = TorusAt(center, R, r + .15f, 0f, s / 40f * 2f * Mathf.PI);
        ExpLine(g, $"n ≡ 0 mod {p}", .22f, DeadColor, Factory.GlowIntensity, cp);
        ExpLine(g, $"n ≡ 0 mod {q}", .22f, DeadColor, Factory.GlowIntensity, cq);
        // the knot
        Vector3[] knot = new Vector3[L * 16 + 1];
        for (int s = 0; s <= L * 16; s++)
        {
            float t = s / 16f;
            knot[s] = TorusAt(center, R, r + .2f, 2f * Mathf.PI * t / q, 2f * Mathf.PI * t / p);
        }

        LineRenderer kl = ExpLine(g, "knot", .1f, new Color(.6f, .65f, .85f), Factory.SoftGlowIntensity, knot);
        // beads
        ExpEnsureSieve(nStat + 1);
        int[] count = new int[L];
        for (long n = 2; n <= nStat; n++) if (ExpPrime(n)) count[n % L]++;
        int max = 1;
        foreach (int c in count) max = Mathf.Max(max, c);
        HashSet<int> eulerBeads = new();
        if (euler) for (long k = 0; k < L; k++) eulerBeads.Add((int)((k * k + k + 41) % L));
        for (int b = 0; b < L; b++)
        {
            Vector3 at = TorusAt(center, R, r + .4f, 2f * Mathf.PI * b / q, 2f * Mathf.PI * b / p);
            bool cancelled = b % p == 0 || b % q == 0;
            float size = cancelled ? 1.3f : 1.2f + 1.4f * Mathf.Sqrt(count[b] / (float)max);
            ExpDot(g, $"bead {b}", at, size, cancelled ? new Color(.62f, .16f, .12f) : Factory.Palette.Zero, cancelled ? Factory.SoftGlowIntensity : Factory.GlowIntensity);
            if (cancelled && count[b] > 0)
            {
                ExpGlowMark(g, at, 1.6f); // the primes p and q themselves
                if (labelSize > 0f) ExpLabelWide(g, b.ToString(), at + Vector3.back * 1.5f + Vector3.up * 1.6f, labelSize * .7f, Color.white);
            }

            if (euler && eulerBeads.Contains(b)) ExpRing(g, $"euler bead {b}", at + Vector3.back * .3f, 1.9f, EulerColor, .14f, Factory.GlowIntensity);
        }

        int open = 0;
        for (int b = 0; b < L; b++) if (b % p != 0 && b % q != 0) open++;
        Board($"{L} beads:  n mod {q}, n mod {p}\n" + Small($"every prime but {p} and {q} on the {open} beads\noff the red circles" +
              (euler ? $"\n{Tint(EulerColor, "Euler's line")} visits {eulerBeads.Count} of them" : "")));
        if (legend && labelSize > 0f)
            ExpLabelWide(g, $"n at angles (2πn/{q}, 2πn/{p}): a torus knot with {L} beads\nprimes up to {nStat:N0} on the {open} beads off the red circles",
                center + new Vector3(0f, -R - r - .2f * labelSize - 3f, 0f), labelSize * .8f, Color.white);
    }


    /// <summary>
    /// Walk the knot: a white bead runs n = 1, 2, 3, … along the torus knot, one bead of the 35 per step, flaring at every
    /// prime; the label keeps count of the primes on the red circles (only p and q) and off them.
    /// </summary>
    public void ExpKnotWalkLive(Vector3 center, float R, float r, int p, int q, int nMax, float seconds, float labelSize, int n0 = 1)
    {
        StartLive(KnotWalk(center, R, r, p, q, n0, nMax, seconds, labelSize));
    }

    IEnumerator KnotWalk(Vector3 center, float R, float r, int p, int q, int n0, int nMax, float seconds, float labelSize)
    {
        Transform g = SectionLiveRoot();
        Vector3 Knot(float t) => TorusAt(center, R, r + .4f, 2f * Mathf.PI * t / q, 2f * Mathf.PI * t / p);
        GameObject walker = Factory.NewSphere("walker", 2.2f, Color.white, AttentionIntensity);
        walker.transform.SetParent(g, false);
        walker.transform.position = Knot(n0 - 1);
        LineRenderer trail = ExpLine(g, "trail", .14f, Color.white, Factory.GlowIntensity, new[] { Knot(n0 - 1), Knot(n0 - 1) });
        AttentionHalo halo = Halo(trail, 2f, .6f);
        TextBox label = labelSize > 0f ? ExpLabelWide(g, "", center + new Vector3(0f, -R - r - 3f - .95f * labelSize, 0f), labelSize * .8f, Color.white) : null;
        float per = seconds * .95f / Mathf.Max(1, nMax - n0 + 1), clock = 0f;
        List<Vector3> path = new() { Knot(n0 - 1) };
        int on = 0, off = 0;
        for (int n = 2; n < n0; n++) if (IsPrime(n)) { if (n % p == 0 || n % q == 0) on++; else off++; }
        for (int n = n0; n <= nMax; n++)
        {
            float s0 = (n - n0) * per;
            while (clock < s0 + per)
            {
                walker.transform.position = Knot(n - 1 + Mathf.Clamp01((clock - s0) / per));
                yield return null;
                clock += Time.deltaTime;
            }

            for (int s = 1; s <= 12; s++) path.Add(Knot(n - 1 + s / 12f));
            if (path.Count > 12 * 35) path.RemoveRange(0, path.Count - 12 * 35); // the trail keeps the last lap of 35
            trail.positionCount = path.Count;
            trail.SetPositions(path.ToArray());
            walker.transform.position = Knot(n);
            bool prime = IsPrime(n);
            if (prime)
            {
                if (n % p == 0 || n % q == 0) on++; else off++;
                Flare(Knot(n), 2.4f, .9f, 1);
                halo.Pulse(.6f);
            }

            if (label != null) label.Text = $"n = {n}" + (prime ? $"  prime,  bead {n % (p * q)}" : "") + $"\nprimes on the red circles: {on}     off them: {off}";
            Board($"n = {n}" + (prime ? $"   {Tint(Factory.Palette.Zero, "prime")},  bead {n % (p * q)}" : "") +
                  "\n" + Small($"primes on the red circles: {on}      off them: {off}"));
        }

        liveRoutine = null;
    }

    // ================================================================================================
    // Z4. the umbilic torus
    // ================================================================================================

    /// <summary>Ferguson's umbilic torus (deltoid cross-section turning 1/3 per lap), scaled; twist 0 gives the untwisted 3-lobed torus.</summary>
    static Vector3 UmbilicPoint(Vector3 center, float scale, float u, float v, float twist)
    {
        float w = twist * u / 3f;
        float ring = 7f + Mathf.Cos(w - 2f * v) + 2f * Mathf.Cos(w + v);
        return center + scale * new Vector3(Mathf.Sin(u) * ring, Mathf.Cos(u) * ring, Mathf.Sin(w - 2f * v) + 2f * Mathf.Sin(w + v));
    }

    static readonly Color[] FaceColors = { new(.3f, .8f, .9f), new(.78f, .5f, 1f), new(1f, .66f, .3f) };

    /// <summary>How far across its face turn k sits (v from the cusp line), so the turns stack.</summary>
    static float UmbilicV(float turn, int kMax) => .1f + .85f * turn / Mathf.Max(1, kMax);

    Vector3 UmbilicNumber(Vector3 center, float scale, float twist, long n, int kMax) =>
        UmbilicPoint(center, scale, 2f * Mathf.PI * Mathf.Sqrt(n), UmbilicV(TurnOf(n), kMax), twist);

    void UmbilicWire(Transform g, Vector3 center, float scale, float twist)
    {
        Color wire = new(.27f, .29f, .38f);
        for (int i = 0; i < 18; i++)
        {
            float u = i / 18f * 2f * Mathf.PI;
            Vector3[] sec = new Vector3[61];
            for (int s = 0; s <= 60; s++) sec[s] = UmbilicPoint(center, scale, u, s / 60f * 2f * Mathf.PI, twist);
            ExpLine(g, "section", .05f, wire, Factory.NoGlow, sec);
        }

        int laps = twist > 0f ? 3 : 1;
        Vector3[] cusp = new Vector3[120 * laps + 1];
        for (int s = 0; s <= 120 * laps; s++) cusp[s] = UmbilicPoint(center, scale, s / 120f * 2f * Mathf.PI, 0f, twist);
        ExpLine(g, "cusp line", .12f, Color.white, Factory.SoftGlowIntensity, cusp);
    }

    /// <summary>Turn k as one lap of the umbilic torus, just off the cusp line, in the colour of its face (k mod 3).</summary>
    LineRenderer UmbilicTurn(Transform g, Vector3 center, float scale, float twist, int k, int kMax, float width)
    {
        Vector3[] lap = new Vector3[65];
        float v = UmbilicV(k, kMax);
        for (int s = 0; s <= 64; s++) lap[s] = UmbilicPoint(center, scale, 2f * Mathf.PI * (k + s / 64f), v, twist);
        Color col = twist > 0f ? FaceColors[k % 3] : FaceColors[0];
        return ExpLine(g, $"turn {k}", width, col * .8f, Factory.SoftGlowIntensity, lap);
    }

    /// <summary>
    /// The values of k² + k + C on the umbilic torus: red where 3 divides them, gold where prime. Only k ≥ C, where the value
    /// lies on its own turn k (so it sits on face k mod 3); the first few values of a curve spill onto later turns.
    /// </summary>
    void UmbilicCurve(Transform g, Vector3 center, float scale, float twist, long C, int kMax, float size)
    {
        List<Vector3> cp = new(); List<Color> cc = new(); List<float> cs = new();
        for (long k = System.Math.Max(0, C); ; k++)
        {
            long n = k * k + k + C;
            if (TurnOf(n) > kMax) break;
            cp.Add(UmbilicNumber(center, scale, twist, n, kMax));
            bool three = n % 3 == 0, prime = ExpPrime(n);
            cc.Add(three ? DeadColor : prime ? Factory.Palette.Zero : new Color(.6f, .62f, .7f));
            cs.Add(three ? size * 1.15f : size);
        }

        ExpPointCloud(g, $"k² + k + {C}", cp, cc, cs, Factory.GlowIntensity);
    }

    /// <summary>
    /// The spiral wound on Ferguson's umbilic torus, one lap per turn, just off the cusp line and drifting across the face so the
    /// turns stack. The tube is a deltoid that turns a third of a turn per lap, so the cusp line visits the three faces in turn
    /// and turn k lands on face k mod 3 (twist 0: every turn on one face). The values of k² + k + C for the given C are marked:
    /// red where 3 divides them, gold where prime. nMax &gt; 0 adds the numbers themselves.
    /// </summary>
    public void ExpUmbilic(Vector3 center, float scale, float twist, int kMax, long[] curves, int nMax, float labelSize)
    {
        Board(twist > 0f ? "the umbilic torus\n" + Small("one lap per turn:  turn k in position k mod 3") : "no twist\n" + Small("every turn on the same face"));
        Transform g = ExpGroup("Z4 umbilic").transform;
        ExpEnsureSieve((long)(kMax + 2) * (kMax + 2) + 100);
        UmbilicWire(g, center, scale, twist);
        for (int k = 1; k <= kMax; k++) UmbilicTurn(g, center, scale, twist, k, kMax, .14f);
        if (nMax > 0)
        {
            List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
            for (int n = 2; n <= nMax; n++)
            {
                bool p = IsPrime(n);
                pts.Add(UmbilicNumber(center, scale, twist, n, kMax));
                cols.Add(p ? new Color(.75f, .65f, .3f) : new Color(.26f, .26f, .32f));
                sizes.Add(p ? .1f * scale : .06f * scale);
            }

            ExpPointCloud(g, "numbers", pts, cols, sizes, Factory.SoftGlowIntensity);
        }

        if (curves != null) foreach (long C in curves) UmbilicCurve(g, center, scale, twist, C, kMax, .24f * scale);
        if (curves != null && curves.Length > 0) UmbilicCurveBoard(curves, twist);
        if (labelSize > 0f)
            ExpLabelWide(g, twist > 0f ? "one lap per turn: turn k in position k mod 3" : "no twist: every turn on the same face",
                center + new Vector3(0f, -10f * scale - .3f * labelSize - 2f, 0f), labelSize, Color.white);
    }

    /// <summary>The board for curves k² + k + C on the umbilic torus: which positions (k mod 3) hold its multiples of 3.</summary>
    static void UmbilicCurveBoard(long[] curves, float twist)
    {
        List<string> rows = new();
        foreach (long C in curves)
        {
            List<int> dead = new();
            for (int j = 0; j < 3; j++) if ((j * j + j + C) % 3 == 0) dead.Add(j);
            string where = dead.Count == 0 ? "never a multiple of 3"
                : twist > 0f ? $"multiples of 3 in position{(dead.Count > 1 ? "s" : "")} {string.Join(" and ", dead)}" : "multiples of 3 mixed in";
            rows.Add($"k² + k + {C}:  " + Tint(DeadColor, where));
        }

        Board((twist > 0f ? "the umbilic torus" : "no twist") + "\n" + Small(string.Join("\n", rows) + "\n" + Tint(Factory.Palette.Zero, "gold: primes")));
    }

    /// <summary>The spiral winding onto the umbilic torus turn by turn: each lap drawn by a white head, in its face's colour.</summary>
    public void ExpUmbilicLive(Vector3 center, float scale, float twist, int kMax, float seconds, float labelSize)
    {
        StartLive(UmbilicLive(center, scale, twist, kMax, seconds, labelSize));
    }

    IEnumerator UmbilicLive(Vector3 center, float scale, float twist, int kMax, float seconds, float labelSize)
    {
        Transform g = ExpGroup("Z4 umbilic live").transform;
        UmbilicWire(g, center, scale, twist);
        Transform live = SectionLiveRoot();
        GameObject head = Factory.NewSphere("head", 1.6f, Color.white, AttentionIntensity);
        head.transform.SetParent(live, false);
        TextBox label = labelSize > 0f ? ExpLabelWide(live, "", center + new Vector3(0f, -10f * scale - .3f * labelSize - 2f, 0f), labelSize, Color.white) : null;
        float per = seconds * .95f / Mathf.Max(1, kMax), clock = 0f;
        for (int k = 1; k <= kMax; k++)
        {
            LineRenderer lap = UmbilicTurn(g, center, scale, twist, k, kMax, .16f);
            Vector3[] pts = new Vector3[lap.positionCount];
            lap.GetPositions(pts);
            lap.positionCount = 1;
            AttentionHalo halo = Halo(lap, 2f, 1f);
            halo.Decay = 1.4f;
            string text = twist > 0f ? $"turn {k}  →  position {k % 3}" : $"turn {k}";
            if (label != null) label.Text = text;
            Board(text + "\n" + Small(twist > 0f ? $"{k} mod 3 = {k % 3}" : "every turn on the same face"));
            float s0 = (k - 1) * per;
            while (clock < s0 + per)
            {
                int shown = Mathf.Clamp(Mathf.CeilToInt((clock - s0) / per * (pts.Length - 1)) + 1, 2, pts.Length);
                lap.positionCount = shown;
                for (int i = 0; i < shown; i++) lap.SetPosition(i, pts[i]);
                head.transform.position = pts[shown - 1];
                yield return null;
                clock += Time.deltaTime;
            }

            lap.positionCount = pts.Length;
            lap.SetPositions(pts);
        }

        if (head != null) Destroy(head);
        Board(twist > 0f ? "three stacks\n" + Small("turn k in position k mod 3") : "one face\n" + Small("every turn on the same face"));
        liveRoutine = null;
    }

    /// <summary>Add the values of k² + k + C to the umbilic torus already drawn (exp:umbilic or exp:umbiliclive), flaring the multiples of 3.</summary>
    public void ExpUmbilicCurve(Vector3 center, float scale, float twist, long C, int kMax, bool flare)
    {
        Transform g = ExpGroup("Z4 umbilic curve " + C).transform;
        ExpEnsureSieve((long)(kMax + 2) * (kMax + 2) + 100);
        UmbilicCurve(g, center, scale, twist, C, kMax, .24f * scale);
        UmbilicCurveBoard(new[] { C }, twist);
        if (!flare) return;
        for (long k = System.Math.Max(0, C); ; k++)
        {
            long n = k * k + k + C;
            if (TurnOf(n) > kMax) break;
            if (n % 3 == 0) Flare(UmbilicNumber(center, scale, twist, n, kMax), 1.2f, 1.4f, 2);
        }
    }
}
