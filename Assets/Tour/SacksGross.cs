using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Sections V–Y (docs/experiments/README.md): 144 = 12 × 12 as a basis for grouping the spiral's curves.
///
/// V. The 12 × 12 torus. Every number has a turn k = ⌊√n⌋ and a step n − k² along it; taken mod 12 both ways they put
///    n on a 12 × 12 board that wraps into a torus. Every curve k² + Bk + C becomes a straight line (slope B), and the
///    primes above 3 can only sit in the 48 cells where k² + step is 1, 5, 7 or 11 mod 12. A line's prime density is
///    3 × the share of rows it spends in those cells: the diagonals of the Euler lines (c ≡ 5, 11 mod 12) never leave them.
/// W. The saddle computes Legendre symbols. Pinched to 2πn/24 on the saddle, every prime's height is ±0.866·Kr² with the
///    sign of (3/p); to 2πn/16, ±0.707·Kr² with the sign of (2/p); the 144-hour pinch on a 12-fold saddle gives (3/p) too.
/// X. Fingerprints mod 144: the last two base-12 digits of a curve's values, on a 12 × 12 board.
/// Y. The 144 clock: the 24 half-hour rays are the directions of the product families u(q²u + d) with q dividing 12.
/// </summary>
public partial class SacksSpiral
{
    static readonly Color TorusAllowed = new(1f, .82f, .25f);
    static readonly Color TorusForbidden = new(.16f, .1f, .12f);
    static readonly Color ClassBlue = new(.45f, .7f, 1f);
    static readonly Color ClassDim = new(.5f, .5f, .6f);
    static readonly Color ChiPlus = new(.3f, .95f, 1f);
    static readonly Color ChiMinus = new(1f, .4f, .9f);

    /// <summary>A label that keeps each line whole (the prefab wraps at a fixed width): break lines with \n yourself.</summary>
    TextBox ExpLabelWide(Transform parent, string text, Vector3 pos, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        TextBox l = ExpLabel(parent, text, pos, size, color, align);
        l.TextField.textWrappingMode = TextWrappingModes.NoWrap;
        return l;
    }

    static bool Allowed12(int row, int col) => ((row * row + col) % 12) is 1 or 5 or 7 or 11;

    static int AllowedRows(int delta)
    {
        int c = 0;
        for (int r = 0; r < 12; r++) if (Allowed12(r, ((r + delta) % 12 + 12) % 12)) c++;
        return c;
    }

    static Color BoardCellColor(bool allowed, float fill) =>
        allowed ? Color.Lerp(new Color(.3f, .22f, .05f), new Color(.85f, .66f, .18f), .5f + .5f * fill) : TorusForbidden;

    static Vector3 BoardCell(Vector3 center, float cell, float r, float c) => center + new Vector3((c - 5.5f) * cell, (5.5f - r) * cell, 0f);

    /// <summary>The board's row and column numbers and its two axis titles.</summary>
    void TorusBoardLabels(Transform g, Vector3 center, float cell, float labelSize)
    {
        for (int i = 0; i < 12; i++)
        {
            ExpLabel(g, i.ToString(), BoardCell(center, cell, -.95f, i), labelSize * .7f, new Color(.75f, .75f, .85f));
            ExpLabel(g, i.ToString(), BoardCell(center, cell, i, -1.1f), labelSize * .7f, new Color(.75f, .75f, .85f));
        }

        ExpLabelWide(g, "step  n − k²  (mod 12) →", BoardCell(center, cell, -1.9f, 5.5f), labelSize * .8f, Color.white);
        ExpLabelWide(g, "turn k ↓", BoardCell(center, cell, -1.9f, -.6f), labelSize * .8f, Color.white);
    }

    static Color DiagonalColor(int allowedRows) => allowedRows == 12 ? TorusAllowed : allowedRows == 8 ? ClassBlue : allowedRows == 4 ? ClassDim : DeadColor;

    /// <summary>Filled quads with vertex colours (cells of boards), one mesh.</summary>
    MeshRenderer ExpQuads(Transform g, string name, List<(Vector3 c, Vector3 u, Vector3 v, Color col)> quads, float glow)
    {
        List<Vector3> verts = new(); List<Color> cols = new(); List<int> tris = new();
        foreach ((Vector3 c, Vector3 u, Vector3 v, Color col) in quads)
        {
            int i = verts.Count;
            verts.Add(c - u - v); verts.Add(c + u - v); verts.Add(c + u + v); verts.Add(c - u + v);
            for (int k = 0; k < 4; k++) cols.Add(col);
            tris.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2, i, i + 1, i + 2, i, i + 2, i + 3 });
        }

        Mesh mesh = new() { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(verts); mesh.SetColors(cols); mesh.SetTriangles(tris, 0); mesh.RecalculateBounds();
        GameObject go = new(name);
        go.transform.SetParent(g, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = Factory.mainMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        // the glow material is transparent and sorted by distance: a board's cells must draw before every line laid over
        // them, or the lines whose centres lie farther from the camera than the board's centre disappear under the cells
        mr.sortingOrder = -10;
        Factory.SetIntensity(mr, glow);
        return mr;
    }

    static int[,] torusPrimes;
    static int torusStat;

    static int[,] TorusPrimeCounts(int nStat)
    {
        if (torusPrimes != null && torusStat == nStat) return torusPrimes;
        ExpEnsureSieve(nStat + 1);
        torusPrimes = new int[12, 12];
        for (int n = 2; n <= nStat; n++)
        {
            if (!ExpPrime(n)) continue;
            int k = (int)System.Math.Sqrt(n);
            while ((long)k * k > n) k--;
            while ((long)(k + 1) * (k + 1) <= n) k++;
            torusPrimes[k % 12, (n - k * k) % 12]++;
        }

        torusStat = nStat;
        return torusPrimes;
    }

    // ================================================================================================
    // V. the 12 × 12 torus
    // ================================================================================================

    /// <summary>
    /// The flat board: row = turn mod 12 (top to bottom), column = step mod 12. Allowed cells gold (brightness by their prime
    /// count over n ≤ nStat), the rest dark. With <paramref name="diagonals"/> the slope-1 lines (the curves k² + k + c,
    /// grouped by c mod 12) run across it; with <paramref name="verticals"/> the slope-0 lines (k² + C).
    /// </summary>
    public void ExpTorusBoard(Vector3 center, float cell, int nStat, bool diagonals, bool verticals, float labelSize)
    {
        Transform g = ExpGroup("V torus board").transform;
        int[,] cnt = TorusPrimeCounts(nStat);
        int max = 1;
        for (int r = 0; r < 12; r++) for (int c = 0; c < 12; c++) max = Mathf.Max(max, cnt[r, c]);
        Vector3 Cell(float r, float c) => center + new Vector3((c - 5.5f) * cell, (5.5f - r) * cell, 0f);
        List<(Vector3, Vector3, Vector3, Color)> quads = new();
        for (int r = 0; r < 12; r++)
        for (int c = 0; c < 12; c++)
        {
            bool a = Allowed12(r, c);
            float f = cnt[r, c] / (float)max;
            quads.Add((Cell(r, c) + Vector3.forward * .05f, Vector3.right * cell * .46f, Vector3.up * cell * .46f, BoardCellColor(a, f)));
        }

        ExpQuads(g, "cells", quads, Factory.SoftGlowIntensity);
        if (labelSize > 0f) TorusBoardLabels(g, center, cell, labelSize);

        if (diagonals)
        {
            for (int delta = 0; delta < 12; delta++)
            {
                int rows = AllowedRows(delta);
                Color col = DiagonalColor(rows);
                // the line step = k + delta, cell centres (r, r + delta), broken where it wraps
                List<Vector3> run = new();
                for (int r = 0; r < 12; r++)
                {
                    int c = (r + delta) % 12;
                    if (c == 0 && run.Count > 0) { Diagonal(run); run.Clear(); }
                    run.Add(Cell(r, c) + Vector3.back * .2f);
                }

                Diagonal(run);
                void Diagonal(List<Vector3> pts)
                {
                    if (pts.Count < 2) return;
                    Color lineCol = rows == 12 ? Color.white : col;
                    LineRenderer l = ExpLine(g, $"diagonal {delta}", rows == 12 ? .9f : rows == 0 ? .3f : .55f, lineCol, rows == 12 ? Factory.GlowIntensity : Factory.SoftGlowIntensity, pts.ToArray());
                    if (rows == 12) GlowLine(l, 1.6f, .7f);
                }

                if (labelSize > 0f && delta % 2 == 1)
                    ExpLabelWide(g, $"c ≡ {delta}: ×{rows / 4f:0.#}", Cell(12.2f + (delta % 4 == 1 ? 0f : .8f), (delta + 11) % 12), labelSize * .62f, col);
            }
        }

        if (verticals)
        {
            for (int C = 0; C < 12; C++)
            {
                int rows = 0;
                for (int r = 0; r < 12; r++) if (Allowed12(r, C)) rows++;
                Color col = rows == 6 ? ClassBlue : rows == 4 ? ClassDim : rows == 2 ? new Color(.5f, .3f, .3f) : DeadColor;
                ExpLine(g, $"vertical {C}", .55f, col, Factory.SoftGlowIntensity, new[] { Cell(-.4f, C) + Vector3.back * .2f, Cell(11.4f, C) + Vector3.back * .2f });
                if (labelSize > 0f) ExpLabelWide(g, $"×{rows / 4f:0.#}", Cell(12.1f, C), labelSize * .6f, col);
            }
        }
    }

    /// <summary>
    /// The board wrapped into a torus: the turn goes the long way round, the step the short way. Every n ≤ nMax sits in its
    /// cell (spread inside it by k div 12 and step div 12); primes gold, numbers in forbidden cells dark. The Euler-class
    /// diagonals (c ≡ 5, 11 mod 12) are drawn as (1, 1) knots round the torus, the c ≡ 1, 7 ones in blue.
    /// </summary>
    public void ExpTorus(Vector3 center, float majorR, float minorR, int nMax, float labelSize)
    {
        Transform g = ExpGroup("V torus").transform;
        Vector3 T(float phi, float psi, float lift = 0f)
        {
            float rr = minorR + lift;
            return center + new Vector3((majorR + rr * Mathf.Cos(psi)) * Mathf.Cos(phi), (majorR + rr * Mathf.Cos(psi)) * Mathf.Sin(phi), -rr * Mathf.Sin(psi));
        }

        // grid: 12 meridians and 12 parallels (cell borders)
        Color wire = new(.3f, .32f, .42f);
        for (int i = 0; i < 12; i++)
        {
            Vector3[] mer = new Vector3[49], par = new Vector3[121];
            for (int t = 0; t <= 48; t++) mer[t] = T(2f * Mathf.PI * i / 12f, 2f * Mathf.PI * t / 48f, -.05f);
            for (int t = 0; t <= 120; t++) par[t] = T(2f * Mathf.PI * t / 120f, 2f * Mathf.PI * i / 12f, -.05f);
            ExpLine(g, "meridian", .06f, wire, Factory.NoGlow, mer);
            ExpLine(g, "parallel", .06f, wire, Factory.NoGlow, par);
        }

        int kTop = (int)System.Math.Sqrt(nMax);
        float kSpan = kTop / 12 + 1, sSpan = (2 * kTop + 1) / 12 + 1;
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        for (int n = 2; n <= nMax; n++)
        {
            int k = (int)System.Math.Sqrt(n);
            while ((long)k * k > n) k--;
            int st = n - k * k;
            float fk = .12f + .76f * (k / 12) / Mathf.Max(1f, kSpan - 1f), fs = .12f + .76f * (st / 12) / Mathf.Max(1f, sSpan - 1f);
            Vector3 p = T(2f * Mathf.PI * (k % 12 + fk) / 12f, 2f * Mathf.PI * (st % 12 + fs) / 12f, .15f);
            bool prime = IsPrime(n), a = Allowed12(k % 12, st % 12);
            pts.Add(p);
            cols.Add(prime ? Factory.Palette.Zero : a ? new Color(.55f, .5f, .35f) : new Color(.2f, .14f, .16f));
            sizes.Add(prime ? .32f : .14f);
        }

        ExpPointCloud(g, "numbers", pts, cols, sizes, Factory.SoftGlowIntensity);
        foreach (int delta in new[] { 5, 11, 1, 7 })
        {
            bool euler = delta == 5 || delta == 11;
            Vector3[] knot = new Vector3[241];
            for (int t = 0; t <= 240; t++)
            {
                float r = 12f * t / 240f;
                knot[t] = T(2f * Mathf.PI * (r + .5f) / 12f, 2f * Mathf.PI * (r + delta + .5f) / 12f, .5f);
            }

            LineRenderer l = ExpLine(g, "knot " + delta, euler ? .32f : .2f, euler ? TorusAllowed : ClassBlue, euler ? Factory.GlowIntensity : Factory.SoftGlowIntensity, knot);
            if (euler) GlowLine(l, 1.8f, .5f);
        }

        if (labelSize > 0f)
        {
            ExpLabelWide(g, "turn mod 12 the long way round\nstep mod 12 the short way", center + new Vector3(0f, majorR + minorR + .2f * labelSize + 2f, 0f), labelSize * .8f, Color.white);
            ExpLabelWide(g, "gold knots: k² + k + c, c ≡ 5 or 11 (mod 12)\nthey never leave the allowed cells", center + new Vector3(0f, -majorR - minorR - .2f * labelSize - 2f, 0f), labelSize * .7f, TorusAllowed);
        }
    }

    /// <summary>
    /// The pronic-side lines k² + k + c for odd c ≤ cMax on the current surface, coloured by their torus class (gold ×3, blue ×2,
    /// grey ×1); the gold ones glow. A legend below the spiral, and labels at the ends of the named lines, placed toward the centre.
    /// </summary>
    public void ExpTorusClassesOnSpiral(int cMax, long nMax, float labelSize, int[] named)
    {
        Transform g = ExpGroup("V classes on the spiral").transform;
        for (int c = 1; c <= cMax; c += 2)
        {
            int rows = AllowedRows(c % 12);
            Color col = DiagonalColor(rows);
            Vector3 end = ExpQuadratic(g, 1, 1, c, 0f, nMax, col, rows == 12 ? .1f : .05f, false, .12f);
            if (rows == 12)
            {
                LineRenderer l = g.Find(Poly(1, 1, c))?.GetComponent<LineRenderer>();
                if (l != null) GlowLine(l, 1.8f, c == 41 ? .75f : .4f);
            }

            if (labelSize > 0f && named != null && System.Array.IndexOf(named, c) >= 0)
            {
                bool left = end.x < origin.x;
                Vector3 at = end + new Vector3(left ? .25f * labelSize : -.25f * labelSize, 0f, -.5f);
                ExpLabelWide(g, $"k² + k + {c}  ×{rows / 4f:0.#}", at, labelSize * .8f, col, left ? TextAlignmentOptions.Left : TextAlignmentOptions.Right);
            }
        }

        if (labelSize > 0f)
            ExpLabelWide(g, "k² + k + c by c mod 12:   gold 5, 11: ×3    blue 1, 7: ×2    grey 3, 9: ×1",
                origin + new Vector3(0f, -Extent - .25f * labelSize - 3f, 0f), labelSize * .75f, Color.white);
    }

    // ================================================================================================
    // W. the saddle computes a Legendre symbol
    // ================================================================================================

    static int Legendre(long a, long p)
    {
        a %= p;
        if (a < 0) a += p;
        if (a == 0) return 0;
        return ModPow(a, (p - 1) / 2, p) == 1 ? 1 : -1;
    }

    /// <summary>
    /// The residue pinch mod m on the current surface with the primes coloured by a character: cyan +1, magenta −1, white 0.
    /// The arms of the classes coprime to 6 are drawn on the surface in the colour of the character there.
    /// </summary>
    public void ExpCharacterPinch(int m, System.Func<int, int> chi, int nMax, float labelSize, string legend)
    {
        Transform g = ExpGroup($"W character pinch {m}").transform;
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        int[] count = new int[m];
        for (int n = 2; n <= nMax; n++)
        {
            bool p = IsPrime(n);
            if (p) count[n % m]++;
            int x = p ? chi(n) : 0;
            pts.Add(ResidueAt(n, m, 1f, .1f));
            cols.Add(p ? (x > 0 ? ChiPlus : x < 0 ? ChiMinus : Color.white) : new Color(.26f, .26f, .32f));
            sizes.Add(p ? .3f : .1f);
        }

        ExpPointCloud(g, "numbers", pts, cols, sizes, Factory.GlowIntensity);
        float rMax = R * Mathf.Sqrt(nMax);
        for (int j = 0; j < m; j++)
        {
            if (Gcd(j, m) != 1) continue;
            int x = chi(j);
            float th = 2f * Mathf.PI * j / m;
            Vector3[] line = new Vector3[60];
            for (int i = 0; i < 60; i++) line[i] = ExpAt(Mathf.Lerp(R, rMax, i / 59f), th, .05f);
            LineRenderer l = ExpLine(g, "arm " + j, .07f, x > 0 ? ChiPlus : ChiMinus, Factory.GlowIntensity, line);
            if (m <= 24) GlowLine(l, 2f, .4f);
            if (labelSize > 0f && m <= 24) ExpLabelWide(g, $"{j}: {count[j]}", ExpAt(rMax + .12f * labelSize + 1.5f, th, 0f), labelSize * .8f, x > 0 ? ChiPlus : ChiMinus);
        }

        if (labelSize > 0f && !string.IsNullOrEmpty(legend))
            ExpLabelWide(g, legend, origin + Vector3.back * (bend * SaddleK * rMax * rMax * .9f + .3f * labelSize + 4f), labelSize, Color.white);
    }

    // ================================================================================================
    // X. fingerprints mod 144
    // ================================================================================================

    /// <summary>
    /// A 12 × 12 board of residues mod 144 (row = second-to-last base-12 digit, column = last digit). Cells the curve's values
    /// visit are lit: gold if coprime to 6 (a prime may sit there), dim red otherwise; with <paramref name="counts"/> the
    /// cells are shaded by how many of the values (or primes) land there.
    /// </summary>
    public void ExpFingerprint(Vector3 center, float cell, System.Func<long, long> f, int kMax, string title, float labelSize, bool primes = false)
    {
        Transform g = ExpGroup("X fingerprint " + title).transform;
        int[] hits = new int[144];
        if (primes)
        {
            ExpEnsureSieve(kMax + 1);
            for (int n = 5; n <= kMax; n++) if (ExpPrime(n)) hits[n % 144]++;
        }
        else
        {
            for (long k = 0; k < kMax; k++) hits[((f(k) % 144) + 144) % 144]++;
        }

        int max = 1, visited = 0, units = 0;
        for (int r = 0; r < 144; r++)
        {
            max = Mathf.Max(max, hits[r]);
            if (hits[r] == 0) continue;
            visited++;
            if (Gcd(r, 6) == 1) units++;
        }

        Vector3 Cell(int row, int col) => center + new Vector3((col - 5.5f) * cell, (5.5f - row) * cell, 0f);
        List<(Vector3, Vector3, Vector3, Color)> quads = new();
        for (int r = 0; r < 144; r++)
        {
            bool unit = Gcd(r, 6) == 1;
            float f2 = hits[r] / (float)max;
            Color col = hits[r] == 0 ? (unit ? new Color(.1f, .09f, .06f) : new Color(.05f, .05f, .07f))
                : unit ? Color.Lerp(new Color(.4f, .3f, .05f), TorusAllowed, .45f + .55f * f2) : Color.Lerp(new Color(.25f, .08f, .08f), DeadColor, .4f + .6f * f2);
            quads.Add((Cell(r / 12, r % 12) + Vector3.forward * .05f, Vector3.right * cell * .44f, Vector3.up * cell * .44f, col));
        }

        ExpQuads(g, "cells", quads, Factory.SoftGlowIntensity);
        // the four columns where every prime above 3 must end: 1, 5, 7, b
        foreach (int col in new[] { 1, 5, 7, 11 })
            ExpLine(g, "unit column", .06f, new Color(.6f, .55f, .4f), Factory.NoGlow, new[] { Cell(-1, col) + new Vector3(0f, -cell * .3f, -.1f), Cell(12, col) + new Vector3(0f, cell * .3f, -.1f) });
        if (labelSize <= 0f) return;
        ExpLabelWide(g, title, Cell(-1, 0) + new Vector3(5.5f * cell, .6f * cell + .12f * labelSize, 0f), labelSize, Color.white);
        string digits = "0123456789ab";
        for (int i = 0; i < 12; i++) ExpLabel(g, digits[i].ToString(), Cell(12, i) + new Vector3(0f, -.25f * cell, 0f), labelSize * .5f, new Color(.6f, .6f, .7f));
        ExpLabelWide(g, primes ? "the 48 unit cells only" : $"{visited} cells, {units} units",
            Cell(12, 0) + new Vector3(5.5f * cell, -.9f * cell - .1f * labelSize, 0f), labelSize * .7f, units > 0 ? TorusAllowed : DeadColor);
    }

    // ================================================================================================
    // Y. the 144 clock of product families
    // ================================================================================================

    /// <summary>
    /// The 24 half-hour rays j/24 of a turn, each with its product family u(q²u + d) (q = 12/gcd(j, 12), d = jq/12) on the
    /// current surface: red where a thin strip round the ray holds no primes below nStat, orange (with the count) where it does.
    /// </summary>
    readonly List<(LineRenderer line, bool free)> clockFamilies = new();

    public void ExpClock144(long nMax, int nStat, float labelSize)
    {
        Transform g = ExpGroup("Y clock 144").transform;
        clockFamilies.Clear();
        ExpEnsureSieve(nStat + 1);
        float rMax = R * Mathf.Sqrt(nMax);
        for (int j = 0; j < 24; j++)
        {
            int q = 12 / Gcd(j, 12), d = j * q / 12;
            // strip count: numbers within R/2 of the ray, turns 60 … √nStat
            int tot = 0, pri = 0;
            int kTop = (int)System.Math.Sqrt(nStat) - 2;
            for (int k = 60; k < kTop; k++)
            {
                double s0 = k + j / 24.0, w = .5 / (2 * System.Math.PI * s0);
                long lo = (long)System.Math.Ceiling((s0 - w) * (s0 - w)), hi = (long)System.Math.Floor((s0 + w) * (s0 + w));
                for (long n = lo; n <= hi; n++) { tot++; if (ExpPrime(n)) pri++; }
            }

            bool free = pri == 0;
            Color col = free ? CancelCurveColor : new Color(1f, .6f, .2f);
            float th = 2f * Mathf.PI * j / 24f;
            Vector3[] ray = new Vector3[50];
            for (int i = 0; i < 50; i++) ray[i] = ExpAt(Mathf.Lerp(R, rMax, i / 49f), th, .02f);
            ExpLine(g, "ray " + j, .03f, new Color(col.r * .5f, col.g * .5f, col.b * .5f), Factory.NoGlow, ray);
            // the family u(q²u + d) from u = 1, by the route angle 2π(√n − q·u)
            List<Vector3> pts = new();
            double uMax = (-d + System.Math.Sqrt((double)d * d + 4.0 * q * q * nMax)) / (2.0 * q * q);
            int steps = Mathf.Max(4, Mathf.CeilToInt((float)(uMax - 1.0) * RouteSteps * 2));
            for (int i = 0; i <= steps; i++)
            {
                double u = 1.0 + (uMax - 1.0) * i / steps;
                pts.Add(ExpCurveAt(u * (q * q * u + d), q * u, .15f));
            }

            LineRenderer l = ExpLine(g, $"u({q * q}u+{d})", .08f, col, Factory.GlowIntensity, pts.ToArray());
            if (q == 12) GlowLine(l, 1.8f, .35f);
            clockFamilies.Add((l, free));
            if (labelSize > 0f)
                ExpLabelWide(g, free ? $"u({q * q}u + {d})" : $"u({q * q}u + {d})  {pri}", ExpAt(rMax + .3f * labelSize + 2f, th, 0f), labelSize * .7f, col);
        }

        if (labelSize > 0f)
            ExpLabelWide(g, $"the 24 half-hours: product families with q² dividing 144\nred: no primes near the ray below {nStat:N0}\norange: primes in the strip (count)",
                origin + new Vector3(0f, -rMax - .5f * labelSize - 6f, 0f), labelSize * .75f, Color.white);
    }
}
