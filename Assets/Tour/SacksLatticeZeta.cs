using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// E8's own zeta function (docs/experiments/README.md, sections KA–KD). Every lattice has a zeta function,
/// Z_L(s) = Σ_{v ≠ 0} (v·v/2)^(−s) = Σ_n r(n) n^(−s). For E8 the shells give r(n) = 240·σ₃(n), so Z_E8(s) = 240 ζ(s) ζ(s − 3):
/// the zeros of ζ are the zeros of E8's own zeta function, twice. Checked in Python first (Tools/lattice_zeta_data.py writes
/// LatticeZetaData.cs).
///   KA  E8's first three shells (240, 2160, 6720 vectors) seen from the Coxeter plane, and their Dirichlet series
///   KB  the landscape log|Λ_E8(s)/Λ_E8(2 + it)|: a mirror image across Re s = 2 with craters at Re s = 1/2 and 7/2 only
///   KC  five lattices (Z², A2, D4, E8, Leech) and where their zeta functions keep their zeros against their critical lines
///   KD  E8's theta function on the hyperbolic plane: one zero, at ρ; the half-turn τ ↦ −1/τ is the functional equation
/// </summary>
public partial class SacksSpiral
{
    public const float LatticeZetaOffset = 72000f;

    // ================================================================================================
    // KA. E8's shells
    // ================================================================================================

    /// <summary>The E8 vectors (doubled coordinates) of norm 2n for n = 1 … nMax (nMax ≤ 3).</summary>
    static List<int[]>[] E8Shells(int nMax)
    {
        List<int[]>[] shells = new List<int[]>[nMax + 1];
        for (int n = 0; n <= nMax; n++) shells[n] = new List<int[]>();
        int limit = 8 * nMax;
        int[] even = { -4, -2, 0, 2, 4 }, odd = { -3, -1, 1, 3 };
        foreach (int[] set in new[] { even, odd })
        {
            int b = set.Length;
            int total = 1;
            for (int i = 0; i < 8; i++) total *= b;
            int[] v = new int[8];
            for (int code = 0; code < total; code++)
            {
                int c = code, q = 0, sum = 0;
                for (int i = 0; i < 8; i++)
                {
                    v[i] = set[c % b];
                    c /= b;
                    q += v[i] * v[i];
                    sum += v[i];
                }

                if (q == 0 || q > limit || q % 8 != 0 || ((sum % 4) + 4) % 4 != 0) continue;
                shells[q / 8].Add((int[])v.Clone());
            }
        }

        return shells;
    }

    /// <summary>
    /// The shells of norm 2, 4, 6 (240, 2160, 6720 vectors) projected onto E8's Coxeter plane, each in its own panel scaled to
    /// the same size: every shell is a union of Coxeter orbits of 30, so each shows as concentric 30-gons. On the right, their
    /// counts r(n) = 240·σ₃(n) and the Dirichlet series they make.
    /// </summary>
    public void ExpE8Shells(Vector3 o, float labelSize)
    {
        Transform g = ExpGroup("KA shells").transform;
        List<int[]>[] shells = E8Shells(3);
        Color[] cols = { Color.white, PrimeGold, new(.35f, .85f, 1f), new(1f, .45f, .8f) };
        float[] xs = { 0f, -98f, -40f, 18f };
        string[] notes = { "", "norm 2:  240", "norm 4:  2 160 = 240·9", "norm 6:  6 720 = 240·28" };
        for (int n = 1; n <= 3; n++)
        {
            List<Vector2> p = new();
            float rMax = 0f;
            foreach (int[] v in shells[n])
            {
                Vector2 q = E8Geometry.Project(v, 0);
                p.Add(q);
                rMax = Mathf.Max(rMax, q.magnitude);
            }

            Vector3 c = o + new Vector3(xs[n], 4f, 0f);
            float scale = 25f / rMax;
            List<Vector3> pts = new();
            List<Color> pc = new();
            List<float> ps = new();
            foreach (Vector2 q in p)
            {
                pts.Add(c + new Vector3(q.x, q.y, 0f) * scale);
                pc.Add(cols[n]);
                ps.Add(n == 1 ? .75f : n == 2 ? .42f : .26f);
            }

            ExpPointCloud(g, $"shell {n}", pts, pc, ps, Factory.GlowIntensity);
            ExpLabelWide(g, notes[n], c + new Vector3(0f, -31f, 0f), labelSize * .21f, cols[n]);
        }

        ExpLabelWide(g, "E8's shells, seen from its Coxeter plane", o + new Vector3(-40f, 38f, 0f), labelSize * .24f, Color.white);
        string text = "r(n) = 240·σ<sub>3</sub>(n) vectors of norm 2n\n\n" +
                      "Σ<sub>v ≠ 0</sub> (v·v/2)<sup>−s</sup>  =  Σ<sub>n</sub> 240 σ<sub>3</sub>(n) n<sup>−s</sup>\n" +
                      "=  240 ζ(s) ζ(s − 3)\n\n" +
                      $"at s = 6:  shells to n = 3 000 give {LatticeZetaData.E8AtSix:0.000000000},\n" +
                      $"240 ζ(6) ζ(3) = {LatticeZetaData.E8AtSixClosed:0.000000000}";
        ExpLabelWide(g, text, o + new Vector3(52f, 4f, 0f), labelSize * .2f, new Color(.85f, .88f, .96f), TextAlignmentOptions.Left);
    }

    // ================================================================================================
    // KB. the landscape of E8's zeta function
    // ================================================================================================

    /// <summary>
    /// log|Λ_E8(σ + it)/Λ_E8(2 + it)| over −0.5 ≤ σ ≤ 4.5, 0.5 ≤ t ≤ 50 as a wire surface (up is −z): zero along the critical
    /// line Re s = 2 (gold), mirror-symmetric across it, with craters only at Re s = 1/2 and 7/2, at the heights of ζ's zeros.
    /// </summary>
    public void ExpE8Landscape(Vector3 o, float labelSize)
    {
        Transform g = ExpGroup("KB landscape").transform;
        const float sx = 13f, sy = 2f, hz = 3.1f, hLo = -4.5f, hHi = 2.6f;
        int ns = LatticeZetaData.E8SigmaCount, nt = LatticeZetaData.E8TCount;
        float Sig(int i) => LatticeZetaData.E8Sigma0 + LatticeZetaData.E8SigmaStep * i;
        float T(int j) => LatticeZetaData.E8T0 + LatticeZetaData.E8TStep * j;
        float H(int i, int j) => Mathf.Clamp(LatticeZetaData.E8Height[j * ns + i], hLo, hHi);
        Vector3 P(float sigma, float t, float h) => o + new Vector3((sigma - 2f) * sx, (t - 25f) * sy, -h * hz);
        Color[] band =
        {
            new(.25f, .35f, 1f), new(.3f, .6f, 1f), new(.35f, .85f, .95f), new(.7f, .8f, .9f), new(.95f, .85f, .6f), new(1f, .7f, .35f)
        };
        int Band(float h) => Mathf.Clamp(Mathf.FloorToInt((h - hLo) / (hHi - hLo) * band.Length), 0, band.Length - 1);
        List<(Vector3, Vector3)>[] segs = new List<(Vector3, Vector3)>[band.Length];
        for (int b = 0; b < band.Length; b++) segs[b] = new();
        for (int j = 0; j < nt; j += 2)
            for (int i = 0; i + 1 < ns; i++)
            {
                float h0 = H(i, j), h1 = H(i + 1, j);
                segs[Band((h0 + h1) * .5f)].Add((P(Sig(i), T(j), h0), P(Sig(i + 1), T(j), h1)));
            }

        for (int i = 0; i < ns; i += 5)
            for (int j = 0; j + 1 < nt; j++)
            {
                float h0 = H(i, j), h1 = H(i, j + 1);
                segs[Band((h0 + h1) * .5f)].Add((P(Sig(i), T(j), h0), P(Sig(i), T(j + 1), h1)));
            }

        for (int b = 0; b < band.Length; b++) Segments(g, $"band {b}", segs[b], .09f, band[b] * .9f, Factory.SoftGlowIntensity);
        ExpLine(g, "critical line", .4f, PrimeGold, Factory.GlowIntensity, new[] { P(2f, T(0), 0f), P(2f, T(nt - 1), 0f) });

        // the craters: ζ's zeros on Re s = 1/2 and, shifted by 3, on Re s = 7/2
        List<Vector3> zp = new();
        List<Color> zc = new();
        List<float> zsz = new();
        foreach (double gm in ZeroStackData.Zeta)
        {
            if (gm > 50d) break;
            foreach (float sigma in new[] { .5f, 3.5f })
            {
                zp.Add(P(sigma, (float)gm, hLo) + Vector3.back * .3f);
                zc.Add(Color.white);
                zsz.Add(1.1f);
            }

            ExpLabelWide(g, gm.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), P(4.55f, (float)gm, hLo) + Vector3.right * 2f,
                labelSize * .15f, new Color(.8f, .84f, .95f), TextAlignmentOptions.Left);
        }

        ExpPointCloud(g, "zeros", zp, zc, zsz, AttentionIntensity);
        foreach ((float sigma, string label) in new[] { (.5f, "Re s = ½"), (2f, "Re s = 2"), (3.5f, "Re s = 7/2") })
            ExpLabelWide(g, label, P(sigma, -2.5f, 0f), labelSize * .18f, sigma == 2f ? PrimeGold : Color.white);
        ExpLabelWide(g, "Im s = 50", P(2f, 53.5f, 0f), labelSize * .16f, new Color(.7f, .74f, .88f));
    }

    // ================================================================================================
    // KC. five lattices and their zeros
    // ================================================================================================

    /// <summary>
    /// For Z², A2, D4, E8 and the Leech lattice, a strip of the s-plane (0 ≤ t ≤ 50) with the zeros of the lattice's zeta
    /// function and its critical line Re s = d/4 (gold): ζ's zeros gold, an L-function's blue, D4's factor 1 − 2^(1−s) green,
    /// the Leech lattice's own zeros white.
    /// </summary>
    public void ExpLatticeZeros(Vector3 o, float labelSize)
    {
        Transform g = ExpGroup("KC lattices").transform;
        const float w = 40f, sy = 1.9f, tMax = 50f;
        (string name, string formula, float s0, float s1, float crit)[] L =
        {
            ("Z²", "4 ζ(s) L(s, χ<sub>−4</sub>)", -.5f, 1.5f, .5f),
            ("A2, hexagonal", "6 ζ(s) L(s, χ<sub>−3</sub>)", -.5f, 1.5f, .5f),
            ("D4", "24 (1 − 2<sup>1−s</sup>) ζ(s) ζ(s − 1)", -.5f, 2.5f, 1f),
            ("E8", "240 ζ(s) ζ(s − 3)", -.5f, 4.5f, 2f),
            ("Leech", "(65520/691)(ζ(s) ζ(s − 11) − L(Δ, s))", -1.5f, 13.5f, 6f),
        };
        string[] verdict = { "all on its line", "all on its line", "only 1 − 2<sup>1−s</sup>'s on its line", "none on its line", "none on its line" };
        Color zg = PrimeGold, lb = new(.35f, .8f, 1f), gr = new(.45f, 1f, .55f), wh = Color.white;
        for (int k = 0; k < L.Length; k++)
        {
            var (name, formula, s0, s1, crit) = L[k];
            Vector3 c = o + new Vector3((k - 2) * (w + 13f), -tMax * sy * .5f, 0f);
            Vector3 P(float sigma, float t) => c + new Vector3((sigma - (s0 + s1) * .5f) / (s1 - s0) * w, t * sy, 0f);
            Segments(g, $"frame {k}", new List<(Vector3, Vector3)>
            {
                (P(s0, 0f), P(s1, 0f)), (P(s0, 0f), P(s0, tMax)), (P(s1, 0f), P(s1, tMax)), (P(s0, tMax), P(s1, tMax)),
            }, .1f, new Color(.5f, .55f, .75f) * .6f, Factory.NoGlow);
            ExpLine(g, $"critical {k}", .32f, PrimeGold * .9f, Factory.GlowIntensity, new[] { P(crit, 0f), P(crit, tMax) });
            List<Vector3> pts = new();
            List<Color> pc = new();
            List<float> ps = new();
            void Add(float sigma, double t, Color col)
            {
                if (t > tMax || sigma < s0 || sigma > s1) return;
                pts.Add(P(sigma, (float)t) + Vector3.back * .2f);
                pc.Add(col);
                ps.Add(.95f);
            }

            foreach (double gm in ZeroStackData.Zeta)
            {
                if (gm > tMax) break;
                if (k <= 1) Add(.5f, gm, zg);
                else if (k == 2) { Add(.5f, gm, zg); Add(1.5f, gm, zg); }
                else if (k == 3) { Add(.5f, gm, zg); Add(3.5f, gm, zg); }
            }

            if (k == 0) foreach (double b in LatticeZetaData.BetaZeros) Add(.5f, b, lb);
            if (k == 1) foreach (double b in ZeroStackData.L[1]) Add(.5f, b, lb);
            if (k == 2) for (int m = 1; 2d * Math.PI * m / Math.Log(2d) <= tMax; m++) Add(1f, 2d * Math.PI * m / Math.Log(2d), gr);
            if (k == 4)
                for (int i = 0; i < LatticeZetaData.LeechZerosRe.Length; i++)
                {
                    Add((float)LatticeZetaData.LeechZerosRe[i], LatticeZetaData.LeechZerosIm[i], wh);
                    Add(12f - (float)LatticeZetaData.LeechZerosRe[i], LatticeZetaData.LeechZerosIm[i], wh);
                }

            ExpPointCloud(g, $"zeros {k}", pts, pc, ps, Factory.GlowIntensity);
            ExpLabelWide(g, name, P((s0 + s1) * .5f, tMax) + Vector3.up * 9f, labelSize * .23f, Color.white);
            ExpLabelWide(g, formula, P((s0 + s1) * .5f, tMax) + Vector3.up * 4.3f, labelSize * .145f, new Color(.82f, .85f, .94f));
            string cl = crit == .5f ? "½" : crit.ToString(System.Globalization.CultureInfo.InvariantCulture);
            ExpLabelWide(g, $"critical line Re s = {cl}", P((s0 + s1) * .5f, 0f) + Vector3.down * 4f, labelSize * .15f, PrimeGold);
            ExpLabelWide(g, verdict[k], P((s0 + s1) * .5f, 0f) + Vector3.down * 8.5f, labelSize * .15f, new Color(.82f, .85f, .94f));
            ExpLabelWide(g, s0.ToString(System.Globalization.CultureInfo.InvariantCulture).Replace("-", "−"), P(s0, 0f) + Vector3.down * 1.8f, labelSize * .12f, new Color(.6f, .64f, .8f));
            ExpLabelWide(g, s1.ToString(System.Globalization.CultureInfo.InvariantCulture), P(s1, 0f) + Vector3.down * 1.8f, labelSize * .12f, new Color(.6f, .64f, .8f));
        }

        ExpLabelWide(g, "t = 50", o + new Vector3(-2f * (w + 13f) - w * .5f - 5f, tMax * sy * .5f, 0f), labelSize * .13f, new Color(.6f, .64f, .8f), TextAlignmentOptions.Right);
        ExpLabelWide(g, "t = 0", o + new Vector3(-2f * (w + 13f) - w * .5f - 5f, -tMax * sy * .5f, 0f), labelSize * .13f, new Color(.6f, .64f, .8f), TextAlignmentOptions.Right);
    }

    // ================================================================================================
    // KD. E8's theta function on the hyperbolic plane
    // ================================================================================================

    static readonly long[] Sigma3Small = { 0, 1, 9, 28, 73, 126, 252, 344, 585, 757, 1134 };

    /// <summary>
    /// θ_E8(τ) = E4(τ), by moving τ into the fundamental domain (E4(γτ) = (cτ + d)⁴ E4(τ)) and summing 1 + 240 Σ σ₃(n) qⁿ there.
    /// Returns E4(τ) and the invariant y²|E4(τ)| (the same at every image of τ under the modular group).
    /// </summary>
    static (Cpx e4, double invariant) E4At(Cpx tau)
    {
        Cpx z = tau;
        long a = 1, b = 0, c = 0, d = 1;
        for (int it = 0; it < 200; it++)
        {
            double n = Math.Round(z.Re);
            if (n != 0d)
            {
                z = new Cpx(z.Re - n, z.Im);
                a -= (long)n * c;
                b -= (long)n * d;
            }

            double r2 = z.Re * z.Re + z.Im * z.Im;
            if (r2 >= 1d - 1e-12) break;
            z = new Cpx(-z.Re / r2, z.Im / r2); // −1/z
            (a, b, c, d) = (-c, -d, a, b);
        }

        Cpx q = Cpx.Polar(Math.Exp(-2d * Math.PI * z.Im), 2d * Math.PI * z.Re), qn = Cpx.One, e = Cpx.One;
        for (int n = 1; n < Sigma3Small.Length; n++)
        {
            qn = qn * q;
            e = e + qn * (240d * Sigma3Small[n]);
        }

        Cpx ct = new Cpx(c * tau.Re + d, c * tau.Im), ct2 = ct * ct;
        Cpx e4 = e / (ct2 * ct2);
        double inv = z.Im * z.Im * Math.Sqrt(e.Re * e.Re + e.Im * e.Im);
        return (e4, inv);
    }

    /// <summary>
    /// The Poincaré disk (τ = i(1 + w)/(1 − w), τ = i at the centre, the cusp i∞ on the right): each point coloured by the
    /// argument of θ_E8(τ) and lit by the invariant y²|θ_E8(τ)|, which is dark only at the images of ρ = e^(2πi/3), E4's single
    /// zero. Gold: the imaginary axis, along which the Mellin transform of θ_E8 gives E8's zeta function; the half-turn
    /// w ↦ −w is τ ↦ −1/τ, the functional equation s ↔ 4 − s.
    /// </summary>
    public void ExpE8Hyperbolic(Vector3 o, float radius, float labelSize)
    {
        Transform g = ExpGroup("KD hyperbolic").transform;
        const double step = .0062d;
        List<Vector3> pts = new();
        List<Color> cols = new();
        List<float> sizes = new();
        for (double x = -1d; x <= 1d; x += step)
            for (double y = -1d; y <= 1d; y += step)
            {
                double r2 = x * x + y * y;
                if (r2 > .985d * .985d) continue;
                Cpx w = new(x, y), tau = new Cpx(0d, 1d) * (Cpx.One + w) / (Cpx.One - w);
                var (e4, inv) = E4At(tau);
                float hue = (float)((Math.Atan2(e4.Im, e4.Re) / (2d * Math.PI) + 1d) % 1d);
                float val = (float)(.12d + .88d * Math.Tanh(inv * .9d));
                float band = (float)(.82d + .18d * Math.Cos(2d * Math.PI * Math.Log(Math.Max(inv, 1e-9)) * 1.5d));
                cols.Add(Color.HSVToRGB(hue, .62f, val * band));
                pts.Add(o + new Vector3((float)x, (float)y, 0f) * radius);
                sizes.Add(radius * (float)step * 1.05f);
            }

        ExpPointCloud(g, "theta", pts, cols, sizes, Factory.NoGlow);
        // the boundary, the imaginary axis (the real diameter), the zeros at the images of ρ
        Vector3[] rim = new Vector3[181];
        for (int i = 0; i < rim.Length; i++)
        {
            float a = i / 180f * Mathf.PI * 2f;
            rim[i] = o + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, -.1f);
        }

        ExpLine(g, "boundary", .35f, new Color(.6f, .64f, .8f), Factory.NoGlow, rim);
        ExpLine(g, "imaginary axis", .5f, PrimeGold, Factory.GlowIntensity, new[] { o + new Vector3(-radius, 0f, -.2f), o + new Vector3(radius, 0f, -.2f) });
        HashSet<(int, int)> seen = new();
        List<Vector3> zp = new();
        List<Color> zc = new();
        List<float> zs = new();
        Cpx rho = new(-.5d, Math.Sqrt(3d) / 2d);
        Queue<(Cpx tau, int depth)> queue = new();
        queue.Enqueue((rho, 0));
        while (queue.Count > 0)
        {
            var (t, depth) = queue.Dequeue();
            Cpx w = (t - new Cpx(0d, 1d)) / (t + new Cpx(0d, 1d));
            double mag = Math.Sqrt(w.Re * w.Re + w.Im * w.Im);
            if (mag > .975d) continue;
            if (!seen.Add(((int)Math.Round(w.Re * 4000d), (int)Math.Round(w.Im * 4000d)))) continue;
            zp.Add(o + new Vector3((float)w.Re * radius, (float)w.Im * radius, -.3f));
            zc.Add(Color.white);
            zs.Add(radius * .022f * (float)(1d - mag * mag) + .25f);
            if (depth >= 9) continue;
            double r2 = t.Re * t.Re + t.Im * t.Im;
            queue.Enqueue((new Cpx(t.Re + 1d, t.Im), depth + 1));
            queue.Enqueue((new Cpx(t.Re - 1d, t.Im), depth + 1));
            queue.Enqueue((new Cpx(-t.Re / r2, t.Im / r2), depth + 1));
        }

        ExpPointCloud(g, "zeros of E4", zp, zc, zs, AttentionIntensity);
        ExpLabelWide(g, "τ = i", o + new Vector3(0f, 3.2f, -.4f), labelSize * .17f, Color.white);
        ExpLabelWide(g, "i∞", o + new Vector3(radius + 4f, 0f, 0f), labelSize * .2f, new Color(.8f, .84f, .95f), TextAlignmentOptions.Left);
        ExpLabelWide(g, "0", o + new Vector3(-radius - 4f, 0f, 0f), labelSize * .2f, new Color(.8f, .84f, .95f), TextAlignmentOptions.Right);
        Cpx wr = (rho - new Cpx(0d, 1d)) / (rho + new Cpx(0d, 1d));
        ExpLabelWide(g, "ρ:  θ<sub>E8</sub>(ρ) = 0", o + new Vector3((float)wr.Re, (float)wr.Im, 0f) * radius + new Vector3(-2f, 5f, -.4f), labelSize * .17f, Color.white, TextAlignmentOptions.Right);
    }
}
