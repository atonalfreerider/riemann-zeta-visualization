using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The class group as families (docs/experiments/README.md, sections LA–LC): the README's open question "draw all h
/// families of curves for a discriminant of class number h". Discriminant −23 has h = 3: the reduced forms x² + xy + 6y²,
/// 2x² + xy + 3y² and 2x² − xy + 3y², three families and three lattices. Checked in Python first (Tools/class_group_data.py
/// writes ClassGroupData.cs).
///   LA  the three lattices, with the rings of norm 6, 13 and 59 through them: a prime lies on one family, a composite on
///       several, and Euler's one-representation test fails only because it looks at one family
///   LB  Euler's test on n ≤ 400: with the principal family alone it calls 6, 12, 18, … prime; with all three, only primes
///   LC  each family's own zeta function keeps about a quarter of its zeros off the critical line; the class group's
///       characters combine the families into ζ(s) L(s, χ₋₂₃) and L(η(z)η(23z), s), whose zeros are all on it
///   LD  the three families are three points of the modular surface (the CM points of −23), drawn on section KD's disk;
///       their j-values are the roots of the class polynomial, and p is on the principal family when x³ − x − 1 splits mod p
///   LE  every mixture Λ₁ + λΛ₂ of the two families has the same functional equation, so on Re s = ½ its zeros are the
///       crossings of one curve, λ = −Λ₁/Λ₂(½ + it), with the height λ; up to t = 50 every zero is on the line only for
///       λ in two small windows round the L-functions, 1.99 to 2 and −1.02 to −0.88
/// </summary>
public partial class SacksSpiral
{
    public const float ClassGroupOffset = 76000f;
    static readonly (int a, int b, int c)[] Forms23 = { (1, 1, 6), (2, 1, 3), (2, -1, 3) };
    static readonly Color[] FormColors = { new(1f, .8f, .32f), new(.35f, .85f, 1f), new(1f, .45f, .8f) };

    /// <summary>The lattice point x·e₁ + y·e₂ of the form (a, b, c): |·|² = a x² + b x y + c y².</summary>
    static Vector2 FormPoint((int a, int b, int c) f, int x, int y)
    {
        double sa = Math.Sqrt(f.a), d = Math.Sqrt(4d * f.a * f.c - f.b * f.b);
        return new Vector2((float)(x * sa + y * f.b / (2d * sa)), (float)(y * d / (2d * sa)));
    }

    static List<(int x, int y)> FormReps((int a, int b, int c) f, int n, bool proper)
    {
        List<(int, int)> r = new();
        int lim = (int)Math.Ceiling(Math.Sqrt(4d * n * Math.Max(f.a, f.c) / (4d * f.a * f.c - f.b * f.b))) + 1;
        for (int x = -lim; x <= lim; x++)
            for (int y = -lim; y <= lim; y++)
                if ((x != 0 || y != 0) && f.a * x * x + f.b * x * y + f.c * y * y == n && (!proper || LabMath.Gcd(Math.Abs(x), Math.Abs(y)) == 1))
                    r.Add((x, y));
        return r;
    }

    // ================================================================================================
    // LA. three families, three lattices
    // ================================================================================================

    /// <summary>
    /// The three reduced forms of discriminant −23 as lattices (points of norm ≤ 90), each with the rings of norm 6 = 2·3,
    /// 13 and 59 drawn through it and its points on those rings lit. 59 lies on the first lattice only, 13 on the other two
    /// (one representation, split between a form and its mirror), and 6 on all three: two representations, so composite.
    /// Below, what each ring meets.
    /// </summary>
    public void ExpClassLattices(Vector3 o, float labelSize)
    {
        Transform g = ExpGroup("LA lattices").transform;
        const float s = 4.2f;
        int[] ns = { 6, 13, 59 };
        Color[] ringCol = { new(1f, .4f, .35f), new(.45f, 1f, .55f), Color.white };
        string[] names = { "x² + xy + 6y²", "2x² + xy + 3y²", "2x² − xy + 3y²" };
        for (int k = 0; k < 3; k++)
        {
            var f = Forms23[k];
            Vector3 c = o + new Vector3(-98f + 98f * k, 0f, 0f);
            List<Vector3> pts = new();
            List<Color> pc = new();
            List<float> ps = new();
            for (int x = -12; x <= 12; x++)
                for (int y = -12; y <= 12; y++)
                {
                    int q = f.a * x * x + f.b * x * y + f.c * y * y;
                    if (q > 90 || (x == 0 && y == 0)) continue;
                    Vector2 p = FormPoint(f, x, y);
                    pts.Add(c + new Vector3(p.x, p.y, 0f) * s);
                    pc.Add(new Color(.45f, .48f, .62f));
                    ps.Add(.55f);
                }

            pts.Add(c);
            pc.Add(Color.white * .6f);
            ps.Add(.65f);
            ExpPointCloud(g, $"lattice {k}", pts, pc, ps, Factory.SoftGlowIntensity);
            List<Vector3> lit = new();
            List<Color> lc = new();
            List<float> ls = new();
            for (int r = 0; r < ns.Length; r++)
            {
                float rad = Mathf.Sqrt(ns[r]) * s;
                Vector3[] ring = new Vector3[97];
                for (int i = 0; i < ring.Length; i++)
                {
                    float a = i / 96f * Mathf.PI * 2f;
                    ring[i] = c + new Vector3(Mathf.Cos(a) * rad, Mathf.Sin(a) * rad, -.05f);
                }

                ExpLine(g, $"ring {k} {ns[r]}", .16f, ringCol[r] * .75f, Factory.SoftGlowIntensity, ring);
                foreach (var (x, y) in FormReps(f, ns[r], true))
                {
                    Vector2 p = FormPoint(f, x, y);
                    lit.Add(c + new Vector3(p.x * s, p.y * s, -.2f));
                    lc.Add(ringCol[r]);
                    ls.Add(1.6f);
                }
            }

            ExpPointCloud(g, $"lit {k}", lit, lc, ls, Factory.GlowIntensity);
            ExpLabelWide(g, names[k], c + new Vector3(0f, 52f, 0f), labelSize * .24f, FormColors[k]);
            ExpLabelWide(g, k == 0 ? "the principal family" : k == 1 ? "a second family" : "its mirror image", c + new Vector3(0f, 46f, 0f), labelSize * .17f, new Color(.75f, .78f, .9f));
        }

        // what the rings meet
        string[] rows =
        {
            "<color=#FFFFFF>59, the white ring:</color>  4 points on x² + xy + 6y², none on the other two:  one representation, a prime",
            "<color=#73FF8C>13, the green ring:</color>  none on x² + xy + 6y², 2 + 2 on the other two:  one representation, a prime",
            "<color=#FF6659>6 = 2·3, the red ring:</color>  4 points on x² + xy + 6y² and 2 + 2 on the other two:  two representations",
        };
        for (int r = 0; r < rows.Length; r++)
            ExpLabelWide(g, rows[r], o + new Vector3(-140f, -50f - 6.2f * r, 0f), labelSize * .19f, new Color(.82f, .85f, .94f), TextAlignmentOptions.Left);
    }

    // ================================================================================================
    // LB. Euler's test needs every family
    // ================================================================================================

    /// <summary>
    /// The numbers 2 … 400 prime to 23, in rows of 50: on top the numbers with exactly one representation by the principal
    /// form alone (gold primes, red composites: Euler fooled), below those with exactly one representation counting all three
    /// forms (only primes and prime powers).
    /// </summary>
    public void ExpClassEuler(Vector3 o, float labelSize)
    {
        Transform g = ExpGroup("LB euler").transform;
        const int perRow = 50, nMax = 400;
        const float dx = 5.4f, rowH = 7.5f;
        for (int panel = 0; panel < 2; panel++)
        {
            Vector3 c0 = o + new Vector3(-perRow * dx * .5f, panel == 0 ? 40f : -28f, 0f);
            List<Vector3> pts = new();
            List<Color> pc = new();
            List<float> ps = new();
            int fooled = 0;
            for (int n = 2; n <= nMax; n++)
            {
                int row = (n - 1) / perRow, col = (n - 1) % perRow;
                Vector3 p = c0 + new Vector3(col * dx, -row * rowH, 0f);
                if (n % 23 == 0) continue;
                int rp = FormReps(Forms23[0], n, true).Count;
                int rt = rp + FormReps(Forms23[1], n, true).Count + FormReps(Forms23[2], n, true).Count;
                bool one = panel == 0 ? rp == 4 : rt == 4;
                bool pp = ZeroSums.PrimePower(n, out _, out _);
                if (!one)
                {
                    pts.Add(p); pc.Add(new Color(.3f, .32f, .42f)); ps.Add(.35f);
                    continue;
                }

                if (!pp) fooled++;
                pts.Add(p); pc.Add(pp ? PrimeGold : new Color(1f, .3f, .28f)); ps.Add(1.15f);
                if (!pp && panel == 0 && n <= 100) ExpLabelWide(g, n.ToString(), p + new Vector3(0f, 2.7f, -.2f), labelSize * .15f, new Color(1f, .45f, .4f));
            }

            ExpPointCloud(g, $"numbers {panel}", pts, pc, ps, Factory.GlowIntensity);
            string head = panel == 0
                ? $"one representation by x² + xy + 6y² alone:  {fooled} composites up to {nMax} pass (red);  {ClassGroupData.Euler23Principal} below 20 000"
                : $"one representation counting all three families:  {fooled} composites;  {ClassGroupData.Euler23All} below 20 000";
            ExpLabelWide(g, head, c0 + new Vector3(-2f, 8f, 0f), labelSize * .19f, panel == 0 ? new Color(1f, .6f, .55f) : PrimeGold, TextAlignmentOptions.Left);
        }

        ExpLabelWide(g, "rows of 50:  2 … 400 (multiples of 23 left out);  gold:  primes and prime powers", o + new Vector3(0f, -88f, 0f), labelSize * .16f, new Color(.75f, .78f, .9f));
    }

    // ================================================================================================
    // LC. where each family keeps its zeros
    // ================================================================================================

    /// <summary>
    /// Four strips of the s-plane (0 ≤ t ≤ 50, −0.4 ≤ Re s ≤ 1.4): the zeta functions of the two families (x² + xy + 6y²;
    /// 2x² ± xy + 3y²) and the class group's two L-functions (ζ(s) L(s, χ₋₂₃); L(η(z)η(23z), s)). Gold: zeros on the critical
    /// line Re s = ½; red: zeros off it, with their mirror images in the line.
    /// </summary>
    public void ExpClassZeros(Vector3 o, float labelSize)
    {
        Transform g = ExpGroup("LC zeros").transform;
        const float w = 50f, sy = 1.9f, tMax = 50f, s0 = -.4f, s1 = 1.4f;
        (string name, string formula, double[] on, double[] offRe, double[] offIm, Color col)[] F =
        {
            ("x² + xy + 6y²", "its Epstein zeta", ClassGroupData.OnM23e, ClassGroupData.OffReM23e, ClassGroupData.OffImM23e, FormColors[0]),
            ("2x² ± xy + 3y²", "its Epstein zeta", ClassGroupData.OnM23c, ClassGroupData.OffReM23c, ClassGroupData.OffImM23c, FormColors[1]),
            ("ζ(s) L(s, χ<sub>−23</sub>)", "(Z<sub>1</sub> + 2 Z<sub>2</sub>)/2", ClassGroupData.OnM23L0, ClassGroupData.OffReM23L0, ClassGroupData.OffImM23L0, Color.white),
            ("L(η(z) η(23z), s)", "(Z<sub>1</sub> − Z<sub>2</sub>)/2", ClassGroupData.OnM23L1, ClassGroupData.OffReM23L1, ClassGroupData.OffImM23L1, Color.white),
        };
        for (int k = 0; k < F.Length; k++)
        {
            var (name, formula, on, offRe, offIm, col) = F[k];
            Vector3 c = o + new Vector3((k - 1.5f) * (w + 14f), -tMax * sy * .5f - 6f, 0f);
            Vector3 P(double sigma, double t) => c + new Vector3((float)((sigma - (s0 + s1) * .5f) / (s1 - s0)) * w, (float)t * sy, 0f);
            Segments(g, $"frame {k}", new List<(Vector3, Vector3)>
            {
                (P(s0, 0), P(s1, 0)), (P(s0, 0), P(s0, tMax)), (P(s1, 0), P(s1, tMax)), (P(s0, tMax), P(s1, tMax)),
                (P(1, 0), P(1, tMax)), (P(0, 0), P(0, tMax)),
            }, .09f, new Color(.5f, .55f, .75f) * .55f, Factory.NoGlow);
            ExpLine(g, $"line {k}", .3f, PrimeGold * .9f, Factory.GlowIntensity, new[] { P(.5, 0), P(.5, tMax) });
            List<Vector3> pts = new();
            List<Color> pc = new();
            List<float> ps = new();
            foreach (double t in on) { pts.Add(P(.5, t) + Vector3.back * .2f); pc.Add(PrimeGold); ps.Add(.85f); }
            for (int i = 0; i < offRe.Length; i++)
            {
                pts.Add(P(offRe[i], offIm[i]) + Vector3.back * .2f); pc.Add(new Color(1f, .3f, .28f)); ps.Add(1.15f);
                pts.Add(P(1d - offRe[i], offIm[i]) + Vector3.back * .2f); pc.Add(new Color(1f, .3f, .28f)); ps.Add(1.15f);
            }

            ExpPointCloud(g, $"zeros {k}", pts, pc, ps, Factory.GlowIntensity);
            ExpLabelWide(g, name, P(.5, tMax) + Vector3.up * 9f, labelSize * .21f, col);
            ExpLabelWide(g, formula, P(.5, tMax) + Vector3.up * 4.4f, labelSize * .15f, new Color(.8f, .84f, .95f));
            ExpLabelWide(g, $"{on.Length} on the line,  {2 * offRe.Length} off it", P(.5, 0) + Vector3.down * 4.2f, labelSize * .16f, offRe.Length > 0 ? new Color(1f, .5f, .45f) : PrimeGold);
            foreach ((double sg, string txt) in new[] { (0d, "0"), (.5d, "½"), (1d, "1") })
                ExpLabelWide(g, txt, P(sg, 0) + Vector3.down * 8.6f, labelSize * .12f, new Color(.6f, .64f, .8f));
        }

        ExpLabelWide(g, "the two families", o + new Vector3(-(w + 14f), 60f, 0f), labelSize * .18f, new Color(.85f, .88f, .96f));
        ExpLabelWide(g, "the class group's L-functions", o + new Vector3((w + 14f), 60f, 0f), labelSize * .18f, new Color(.85f, .88f, .96f));
    }

    // ================================================================================================
    // LD. three points of the modular surface
    // ================================================================================================

    /// <summary>
    /// Section KD's disk (θ_E8 dimmed) with the CM points τ = (−b + i√23)/2a of the three forms and all their images under
    /// τ → τ ± 1 and τ → −1/τ, in the families' colours, and the outline of the fundamental domain.
    /// </summary>
    public void ExpClassHeegner(Vector3 o, float radius, float labelSize)
    {
        ExpE8Hyperbolic(o, radius, labelSize, .42f, false);
        Transform g = ExpGroup("LD heegner").transform;
        Cpx I = new(0d, 1d);
        Vector3 W(Cpx t)
        {
            Cpx w = (t - I) / (t + I);
            return o + new Vector3((float)w.Re, (float)w.Im, 0f) * radius;
        }

        // the fundamental domain |Re τ| ≤ ½, |τ| ≥ 1
        List<(Vector3, Vector3)> edge = new();
        void Edge(Func<double, Cpx> f, int n)
        {
            for (int i = 0; i < n; i++) edge.Add((W(f(i / (double)n)) + Vector3.back * .3f, W(f((i + 1) / (double)n)) + Vector3.back * .3f));
        }

        double h0 = Math.Sqrt(3d) / 2d;
        Edge(u => new Cpx(-.5d, h0 / (1d - u * .985d)), 120);
        Edge(u => new Cpx(.5d, h0 / (1d - u * .985d)), 120);
        Edge(u => new Cpx(Math.Cos(Math.PI * (2d / 3d - u / 3d)), Math.Sin(Math.PI * (2d / 3d - u / 3d))), 60);
        Segments(g, "fundamental domain", edge, .22f, Color.white * .8f, Factory.SoftGlowIntensity);
        double r23 = Math.Sqrt(23d);
        string[] names = { "x² + xy + 6y²", "2x² + xy + 3y²", "2x² − xy + 3y²" };
        for (int k = 0; k < 3; k++)
        {
            var f = Forms23[k];
            Cpx t0 = new(-f.b / (2d * f.a), r23 / (2d * f.a));
            HashSet<(int, int)> seen = new();
            List<Vector3> pts = new();
            List<Color> pc = new();
            List<float> ps = new();
            Queue<(Cpx t, int depth)> queue = new();
            queue.Enqueue((t0, 0));
            while (queue.Count > 0)
            {
                var (t, depth) = queue.Dequeue();
                Cpx w = (t - I) / (t + I);
                double mag = Math.Sqrt(w.Re * w.Re + w.Im * w.Im);
                if (mag > .975d || !seen.Add(((int)Math.Round(w.Re * 4000d), (int)Math.Round(w.Im * 4000d)))) continue;
                pts.Add(o + new Vector3((float)w.Re * radius, (float)w.Im * radius, -.5f));
                pc.Add(FormColors[k]);
                ps.Add(depth == 0 ? 2.1f : radius * .024f * (float)(1d - mag * mag) + .3f);
                if (depth >= 9) continue;
                double r2 = t.Re * t.Re + t.Im * t.Im;
                queue.Enqueue((new Cpx(t.Re + 1d, t.Im), depth + 1));
                queue.Enqueue((new Cpx(t.Re - 1d, t.Im), depth + 1));
                queue.Enqueue((new Cpx(-t.Re / r2, t.Im / r2), depth + 1));
            }

            ExpPointCloud(g, $"orbit {k}", pts, pc, ps, Factory.GlowIntensity);
            Vector3 at = W(t0);
            Vector3 off = k == 0 ? new Vector3(3.2f, 3.4f, -.6f) : k == 1 ? new Vector3(-3.2f, 3.6f, -.6f) : new Vector3(-3.2f, -3.6f, -.6f);
            ExpLabelWide(g, names[k], at + off, labelSize * .16f, FormColors[k], k == 0 ? TextAlignmentOptions.Left : TextAlignmentOptions.Right);
        }
    }

    // ================================================================================================
    // LE. every mixture of the two families
    // ================================================================================================

    /// <summary>
    /// Left: the curve λ = R(t) = −Λ₁/Λ₂(½ + it) for 0 &lt; t ≤ 50 (broken at the zeros of Λ₂), with the heights λ = 2 and −1
    /// (the class group's L-functions, gold) and λ = 0 (the first family alone, blue) and their crossings. Right, on the same
    /// λ axis: how many zeros off the line the mixture Λ₁ + λΛ₂ has, for every λ from −3 to 4: none only in two small
    /// windows (gold), round the two L-functions.
    /// </summary>
    public void ExpClassMixtures(Vector3 o, float labelSize)
    {
        Transform g = ExpGroup("LE mixtures").transform;
        const float w = 190f, sy = 12f, lo = -3f, hi = 4f, x0 = -140f, bx = 72f, sx = 3.2f;
        Color axisCol = new(.7f, .74f, .88f), blue = FormColors[1];
        Vector3 P(double t, double lam) => o + new Vector3(x0 + (float)(t / 50d) * w, (float)lam * sy, 0f);
        double[] R = ClassGroupData.MixR;
        double T(int k) => .02d * k + .02d;
        // axes and ticks
        List<(Vector3, Vector3)> ax = new() { (P(0, lo), P(50, lo)), (P(0, lo), P(0, hi)) };
        for (int t = 0; t <= 50; t += 10)
        {
            ax.Add((P(t, lo), P(t, lo - .25)));
            ExpLabelWide(g, t.ToString(), P(t, lo - .75), labelSize * .14f, axisCol);
        }

        for (int l = -3; l <= 4; l++)
        {
            ax.Add((P(0, l), P(-.6, l)));
            ExpLabelWide(g, l.ToString().Replace("-", "−"), P(-1.1, l), labelSize * .14f, axisCol, TextAlignmentOptions.Right);
        }

        Segments(g, "axes", ax, .12f, new Color(.55f, .6f, .78f) * .7f, Factory.NoGlow);
        ExpLabelWide(g, "t", P(51.5, lo), labelSize * .16f, axisCol, TextAlignmentOptions.Left);
        ExpLabelWide(g, "λ", P(-2.6, hi + .3), labelSize * .18f, axisCol);
        // the curve, broken where Λ₂ vanishes (R jumps through ±∞)
        List<Vector3> run = new();
        int part = 0;
        void Flush()
        {
            if (run.Count > 1) ExpLine(g, $"R {part++}", .3f, new Color(.92f, .94f, 1f), Factory.SoftGlowIntensity, run.ToArray());
            run.Clear();
        }

        for (int k = 0; k < R.Length; k++)
        {
            bool inside = R[k] >= lo - .5 && R[k] <= hi + .5;
            if (k > 0 && Math.Abs(R[k] - R[k - 1]) > 4d) Flush();
            if (inside) run.Add(P(T(k), Math.Max(lo, Math.Min(hi, R[k]))));
            else
            {
                if (run.Count > 0) run.Add(P(T(k), Math.Max(lo, Math.Min(hi, R[k]))));
                Flush();
            }
        }

        Flush();
        // the three heights and their crossings
        (double lam, Color col, string name)[] heights =
        {
            (2d, PrimeGold, "ζ(s) L(s, χ<sub>−23</sub>)"), (-1d, PrimeGold, "L(η(z) η(23z), s)"), (0d, blue, "x² + xy + 6y² alone"),
        };
        List<Vector3> dots = new();
        List<Color> dc = new();
        List<float> ds = new();
        foreach (var (lam, col, name) in heights)
        {
            ExpLine(g, $"height {lam}", .22f, col * .9f, Factory.GlowIntensity, new[] { P(0, lam) + Vector3.forward * .1f, P(50, lam) + Vector3.forward * .1f });
            for (int k = 1; k < R.Length; k++)
            {
                if (Math.Abs(R[k] - R[k - 1]) > 4d || (R[k] - lam) * (R[k - 1] - lam) > 0d) continue;
                double f = (lam - R[k - 1]) / (R[k] - R[k - 1]);
                dots.Add(P(T(k - 1) + .02d * f, lam) + Vector3.back * .3f);
                dc.Add(col);
                ds.Add(1.3f);
            }
        }

        ExpPointCloud(g, "crossings", dots, dc, ds, Factory.GlowIntensity);
        // the zeros off the line for every λ (all of the mixture's zeros, counted by the argument principle, minus those on the
        // line), on the same axis: none only in two small windows, round the two L-functions
        int[] off = ClassGroupData.MixOffLine;
        Vector3 Q(double lam, double count) => o + new Vector3(bx + (float)count * sx, (float)lam * sy, 0f);
        Vector3[] prof = new Vector3[off.Length];
        List<(Vector3, Vector3)> fill = new(), gold = new();
        for (int k = 0; k < off.Length; k++)
        {
            double lam = lo + .01d * k;
            prof[k] = Q(lam, off[k]);
            if (k % 2 == 0) fill.Add((Q(lam, 0), Q(lam, off[k])));
            if (off[k] == 0) gold.Add((Q(lam - .006, 0) + Vector3.back * .2f, Q(lam + .006, 0) + Vector3.back * .2f));
        }

        Segments(g, "off fill", fill, .1f, new Color(.45f, .5f, .7f) * .55f, Factory.NoGlow);
        ExpLine(g, "off", .26f, new Color(.85f, .88f, 1f), Factory.SoftGlowIntensity, prof);
        Segments(g, "none off", gold, .9f, PrimeGold, Factory.GlowIntensity);
        List<(Vector3, Vector3)> cax = new() { (Q(lo, 0), Q(hi, 0)), (Q(lo, 0), Q(lo, 16)) };
        foreach (int c in new[] { 0, 8, 16 })
        {
            cax.Add((Q(lo, c), Q(lo - .12, c)));
            ExpLabelWide(g, c.ToString(), Q(lo - .45, c), labelSize * .14f, axisCol);
        }

        Segments(g, "count axes", cax, .12f, new Color(.55f, .6f, .78f) * .7f, Factory.NoGlow);
        ExpLabelWide(g, "zeros off the line", Q(lo - 1.05, 8), labelSize * .15f, axisCol);
        foreach (var (lam, col, name) in heights)
        {
            int k = (int)Math.Round((lam - lo) / .01d);
            ExpLabelWide(g, $"{off[k]} off:  {name}", Q(lam, 17), labelSize * .15f, col, TextAlignmentOptions.Left);
        }
    }

    /// <summary>
    /// The sweep over LE's chart at o (as drawn by ExpClassMixtures): a gold height λ climbs from lam0 to lam1 in `seconds`,
    /// its crossings with the curve light up (each a zero of that mixture on the line), a marker rides the count of zeros off
    /// the line in the right panel, and a label reads λ and both counts.
    /// </summary>
    public void ExpMixtureSweepLive(Vector3 o, float lam0, float lam1, float seconds, float labelSize)
    {
        StartLive(MixtureSweep(o, lam0, lam1, seconds, labelSize));
    }

    System.Collections.IEnumerator MixtureSweep(Vector3 o, float lam0, float lam1, float seconds, float labelSize)
    {
        const float w = 190f, sy = 12f, lo = -3f, x0 = -140f, bx = 72f, sx = 3.2f;
        Vector3 P(double t, double lam) => o + new Vector3(x0 + (float)(t / 50d) * w, (float)lam * sy, -.4f);
        Vector3 Q(double lam, double count) => o + new Vector3(bx + (float)count * sx, (float)lam * sy, -.4f);
        double[] R = ClassGroupData.MixR;
        Transform g = LiveRoot();
        LineRenderer height = ExpLine(g, "height", .3f, Color.white, AttentionIntensity, new[] { P(0, lam0), P(50, lam0) });
        LineRenderer marker = ExpLine(g, "marker", .5f, Color.white, AttentionIntensity, new[] { Q(lam0, 0), Q(lam0, 1) });
        TextBox readout = ExpLabelWide(g, "", o + new Vector3(x0 + 4f, 4.6f * sy, -.5f), labelSize * .19f, Color.white, TextAlignmentOptions.Left);
        GameObject dotsHolder = null;
        float time = 0f;
        while (true)
        {
            if (height == null) yield break; // the stage was cleared under us
            float u = Mathf.Clamp01(time / seconds);
            double lam = lam0 + (lam1 - lam0) * Mathf.SmoothStep(0f, 1f, u);
            height.SetPositions(new[] { P(0, lam), P(50, lam) });
            int k = Mathf.Clamp((int)System.Math.Round((lam - lo) / .01), 0, ClassGroupData.MixOnLine.Length - 1);
            int on = ClassGroupData.MixOnLine[k], off = ClassGroupData.MixOffLine[k];
            marker.SetPositions(new[] { Q(lam, 0), Q(lam, off) });
            readout.TextField.text = $"λ = {lam:0.00}:   <color=#FFD54A>{on} zeros on the line</color>,   {(off == 0 ? "<color=#FFD54A>none off it</color>" : $"{off} off it")}".Replace("-", "−");
            // the crossings of the curve with the height: the zeros of this mixture on the line
            if (dotsHolder != null) Destroy(dotsHolder);
            dotsHolder = new GameObject("crossings");
            dotsHolder.transform.SetParent(g, false);
            List<Vector3> dots = new();
            List<Color> dc = new();
            List<float> ds = new();
            for (int i = 1; i < R.Length; i++)
            {
                if (System.Math.Abs(R[i] - R[i - 1]) > 4d || (R[i] - lam) * (R[i - 1] - lam) > 0d) continue;
                double f = (lam - R[i - 1]) / (R[i] - R[i - 1]);
                dots.Add(P(.02 * i + .02 * f, lam) + Vector3.back * .2f);
                dc.Add(off == 0 ? PrimeGold : Color.white);
                ds.Add(1.2f);
            }

            ExpPointCloud(dotsHolder.transform, "dots", dots, dc, ds, AttentionIntensity);
            if (u >= 1f) yield break;
            time += Time.deltaTime;
            yield return null;
        }
    }
}
