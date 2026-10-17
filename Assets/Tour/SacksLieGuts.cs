using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Inside the Lie group (docs/experiments/README.md, sections IA–ID): the stills behind the answer to "are the vortices of the
/// zeta chain related to E8?" and the push into the structure of Lie groups. All numbers were checked in Python first
/// (Tools/lie_guts_data.py writes the measured data into LieGutsData.cs).
///   IA  E8's Coxeter traces tr(C^k) = Ramanujan sums c₃₀(k): the 8 eigenvalues to the k-th power, and the spoke charges of
///       the zeta chain restricted to E8's exponent hours (measured at t = 5000), which are those traces
///   IB  E8 as SU(2) spins (Kostant: 248 = 3 + 15 + … + 59 from the root heights) and Macdonald's η²⁴⁸ from the same roots
///   IC  ζ at minus E8's exponents, and the Weyl group counted by ζ at negative integers (the mass formula)
///   ID  Sato–Tate: the angles of Δ = (θ_E8³ − θ_Leech)/720 at the primes are SU(2)'s Haar measure, on every one of E8's spokes
/// </summary>
public partial class SacksSpiral
{
    public const float LieOffset = 62000f;
    static readonly int[] E8Exp = { 1, 7, 11, 13, 17, 19, 23, 29 };

    static int Gcd30(int k)
    {
        int a = k, b = 30;
        while (b != 0) (a, b) = (b, a % b);
        return a;
    }

    /// <summary>tr(C^k) for E8's Coxeter element C = the Ramanujan sum c₃₀(k), fixed by gcd(k, 30).</summary>
    public static int CoxeterTrace(int k) => Gcd30(k) switch { 1 => -1, 2 => 1, 3 => 2, 5 => 4, 6 => -2, 10 => -4, 15 => -8, _ => 8 };

    static Color ClassColour(int d) => d switch
    {
        1 => PrimeGold,
        2 => new Color(1f, .36f, .32f),
        3 => new Color(.38f, 1f, .5f),
        5 => new Color(.45f, .66f, 1f),
        6 => new Color(1f, .62f, .25f),
        10 => new Color(.78f, .5f, 1f),
        15 => new Color(.3f, .95f, .95f),
        _ => Color.white,
    };

    static string Grouped(long v) => v.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(",", " ").Replace("-", "−");

    // ================================================================================================
    // IA. E8's Coxeter traces, and the spoke charges of the gold chain
    // ================================================================================================

    /// <summary>
    /// Left: for k = 15, 1, 5 the eight eigenvalues e^(2πimk/30) of E8's Coxeter element (m the exponents) on the unit circle,
    /// and the same eight unit steps chained head to tail from the centre: they end on tr(C^k) = −8, −1, 4. Right: the charge of
    /// spoke k of the zeta chain restricted to n prime to 30 (measured at t = 5000), against tr(C^k) for k = 1 … 30.
    /// </summary>
    // where the IA picture was drawn (for callouts at its bars and wheels)
    Vector3 iaOrigin;

    /// <summary>The top of the filled bar of spoke k (its charge measured at t = 5000) in the IA picture.</summary>
    public Vector3 CoxeterBarTop(int k)
    {
        k = Mathf.Clamp(k, 1, 30);
        return iaOrigin + new Vector3(16f + (k - .5f) * 3.6f, -6f + LieGutsData.SpokeChargeRe[k - 1] * 4.1f, -.4f);
    }

    /// <summary>The gold point where the eight unit steps of wheel k (15, 1 or 5) end: tr Cᵏ.</summary>
    public Vector3 CoxeterWheelTrace(int k)
    {
        float x = k == 15 ? -86f : k == 1 ? -50f : -16f;
        return iaOrigin + new Vector3(x + CoxeterTrace(k) * 3.25f, 6f, -.4f);
    }

    public void ExpCoxeterTraceCharges(Vector3 o, float labelSize)
    {
        iaOrigin = o;
        Transform g = ExpGroup("IA traces").transform;
        int[] ks = { 15, 1, 5 };
        float[] xs = { -86f, -50f, -16f };
        const float r = 13f, step = 3.25f;
        for (int i = 0; i < ks.Length; i++)
        {
            int k = ks[i];
            Vector3 c = o + new Vector3(xs[i], 6f, 0f);
            List<Vector3> ring = new();
            for (int j = 0; j <= 90; j++)
            {
                float a = j / 90f * Mathf.PI * 2f;
                ring.Add(c + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * r);
            }

            ExpLine(g, $"circle {k}", .14f, new Color(.5f, .55f, .75f) * .6f, Factory.NoGlow, ring.ToArray());
            List<(Vector3, Vector3)> ticks = new() { (c + Vector3.left * (r + 16f), c + Vector3.right * (r + 4f)) };
            for (int h = 0; h < 30; h++)
            {
                float a = h * 12f * Mathf.Deg2Rad;
                Vector3 d = new(Mathf.Cos(a), Mathf.Sin(a), 0f);
                ticks.Add((c + d * r * .93f, c + d * r * 1.07f));
            }

            Segments(g, $"ticks {k}", ticks, .1f, new Color(.5f, .55f, .75f) * .55f, Factory.NoGlow);
            List<Vector3> eig = new();
            List<Color> ec = new();
            List<float> es = new();
            Vector3 acc = c;
            List<Vector3> chain = new() { c };
            foreach (int m in E8Exp)
            {
                float a = 2f * Mathf.PI * m * k / 30f;
                Vector3 d = new(Mathf.Cos(a), Mathf.Sin(a), 0f);
                eig.Add(c + d * r + Vector3.back * .2f);
                ec.Add(PrimeGold);
                es.Add(.85f);
                acc += d * step;
                chain.Add(acc);
            }

            ExpPointCloud(g, $"eigenvalues {k}", eig, ec, es, Factory.GlowIntensity);
            ExpLine(g, $"chain {k}", .32f, Color.white, Factory.GlowIntensity, chain.ToArray());
            int tr = CoxeterTrace(k);
            ExpDot(g, $"trace {k}", c + Vector3.right * tr * step + Vector3.back * .3f, 1.5f, tr < 0 ? new Color(.35f, .8f, 1f) : PrimeGold, AttentionIntensity);
            ExpLabelWide(g, $"k = {k}", c + new Vector3(0f, r + 4.2f, 0f), labelSize * .3f, Color.white);
            ExpLabelWide(g, $"tr C<sup>{k}</sup> = {tr}".Replace("-", "−"), c + new Vector3(0f, -r - 5f, 0f), labelSize * .3f, tr < 0 ? new Color(.35f, .8f, 1f) : PrimeGold);
        }

        ExpLabelWide(g, "the 8 eigenvalues e<sup>2πimk/30</sup> (m = E8's exponents),\nand the same 8 unit steps chained from the centre",
            o + new Vector3(-52f, -26f, 0f), labelSize * .21f, new Color(.8f, .84f, .95f));

        // the charges
        const float x0 = 16f, dx = 3.6f, yb = -6f, unit = 4.1f;
        List<(Vector3, Vector3)> axis = new() { (o + new Vector3(x0 - 2f, yb, 0f), o + new Vector3(x0 + 30 * dx + 1f, yb, 0f)) };
        foreach (int v in new[] { -8, -4, 4, 8 })
        {
            axis.Add((o + new Vector3(x0 - 2.6f, yb + v * unit, 0f), o + new Vector3(x0 - 1.4f, yb + v * unit, 0f)));
            ExpLabelWide(g, v.ToString().Replace("-", "−"), o + new Vector3(x0 - 3.6f, yb + v * unit, 0f), labelSize * .2f, new Color(.6f, .64f, .8f), TextAlignmentOptions.Right);
        }

        Segments(g, "axis", axis, .14f, new Color(.6f, .64f, .8f) * .7f, Factory.NoGlow);
        List<(Vector3, Vector3)> outline = new();
        for (int k = 1; k <= 30; k++)
        {
            float x = x0 + (k - .5f) * dx, top = yb + CoxeterTrace(k) * unit;
            float hw = dx * .42f;
            outline.Add((o + new Vector3(x - hw, yb, 0f), o + new Vector3(x - hw, top, 0f)));
            outline.Add((o + new Vector3(x + hw, yb, 0f), o + new Vector3(x + hw, top, 0f)));
            outline.Add((o + new Vector3(x - hw, top, 0f), o + new Vector3(x + hw, top, 0f)));
            float measured = LieGutsData.SpokeChargeRe[k - 1];
            ExpLine(g, $"charge {k}", dx * .46f, ClassColour(Gcd30(k)), Factory.SoftGlowIntensity,
                new[] { o + new Vector3(x, yb, -.1f), o + new Vector3(x, yb + measured * unit, -.1f) });
            if (k == 1 || k % 5 == 0) ExpLabelWide(g, k.ToString(), o + new Vector3(x, yb - 41f, 0f), labelSize * .2f, new Color(.7f, .74f, .88f));
        }

        Segments(g, "traces", outline, .18f, Color.white, Factory.SoftGlowIntensity);
        ExpLabelWide(g, "spoke k", o + new Vector3(x0 + 15f * dx, yb - 45f, 0f), labelSize * .2f, new Color(.7f, .74f, .88f));
        ExpLabelWide(g, "outline:  tr C<sup>k</sup>  (exact)\nfilled:  charge of spoke k at t = 5000\ncolour:  gcd(k, 30)",
            o + new Vector3(x0 + 2f, 40f, 0f), labelSize * .21f, new Color(.8f, .84f, .95f), TextAlignmentOptions.Left);
    }

    // ================================================================================================
    // IB. Kostant's SU(2) in E8, and Macdonald's η²⁴⁸
    // ================================================================================================

    /// <summary>
    /// E8's 120 positive roots by height: row j has as many roots as exponents ≥ j, so column i has height m_i, the spin of
    /// one SU(2) piece of E8 (dimension 2m_i + 1). Beside the top rows: the coefficients of ∏(1 − qⁿ)²⁴⁸ = q^(−31/3) η(q)²⁴⁸,
    /// which Macdonald's identity gives as d(ρ − 30β) summed over the roots β of height 30 − K (d = Weyl's dimension formula).
    /// </summary>
    public void ExpKostantMacdonald(Vector3 o, float labelSize)
    {
        Transform g = ExpGroup("IB kostant").transform;
        int[] rows = { 29, 23, 19, 17, 13, 11, 7, 1 };
        const float cell = 3.9f, rowGap = 4.6f;
        Vector3 b0 = o + new Vector3(-86f, 34f, 0f);
        List<Vector3> pts = new();
        List<Color> cs = new();
        List<float> ss = new();
        for (int i = 0; i < rows.Length; i++)
        {
            float y = -i * rowGap;
            for (int j = 1; j <= rows[i]; j++)
            {
                pts.Add(b0 + new Vector3((j - .5f) * cell, y, -.1f));
                cs.Add(Color.Lerp(new Color(.35f, .75f, 1f), PrimeGold, (j - 1) / 28f));
                ss.Add(.85f);
            }

            ExpLabelWide(g, $"spin {rows[i]}", b0 + new Vector3(-16f, y, 0f), labelSize * .2f, PrimeGold, TextAlignmentOptions.Right);
            ExpLabelWide(g, $"dim {2 * rows[i] + 1}", b0 + new Vector3(-2.4f, y, 0f), labelSize * .2f, Color.white, TextAlignmentOptions.Right);
        }

        ExpPointCloud(g, "positive roots by height", pts, cs, ss, Factory.GlowIntensity);
        float yAxis = -(rows.Length - 1) * rowGap - 4.4f;
        foreach (int j in new[] { 1, 5, 10, 15, 20, 25, 29 })
            ExpLabelWide(g, j.ToString(), b0 + new Vector3((j - .5f) * cell, yAxis, 0f), labelSize * .18f, new Color(.6f, .64f, .8f));
        ExpLabelWide(g, "height of the root", b0 + new Vector3(15f * cell, yAxis - 4.4f, 0f), labelSize * .18f, new Color(.6f, .64f, .8f));
        // the twelve top columns (heights 29 … 18) give q¹ … q¹² of η²⁴⁸
        List<(Vector3, Vector3)> bracket = new()
        {
            (b0 + new Vector3(17f * cell, 3.2f, 0f), b0 + new Vector3(29f * cell, 3.2f, 0f)),
            (b0 + new Vector3(17f * cell, 3.2f, 0f), b0 + new Vector3(17f * cell, 1.6f, 0f)),
            (b0 + new Vector3(29f * cell, 3.2f, 0f), b0 + new Vector3(29f * cell, 1.6f, 0f)),
        };
        Segments(g, "bracket", bracket, .18f, Color.white * .7f, Factory.SoftGlowIntensity);
        ExpLabelWide(g, "heights 29 … 18  →  q<sup>1</sup> … q<sup>12</sup>", b0 + new Vector3(23f * cell, 6.4f, 0f), labelSize * .2f, Color.white);

        string[] coef =
        {
            "−248", "30 380", "−2 450 240", "146 325 270", "−6 899 079 264", "267 413 986 840", "−8 760 957 040 640", "247 544 345 233 185",
            "−6 125 150 439 153 200", "134 310 078 987 466 356", "−2 634 774 572 077 659 648", "46 596 338 416 013 280 790"
        };
        Vector3 t0 = o + new Vector3(-118f, -18f, 0f);
        ExpLabelWide(g, "coefficients of ∏ (1 − q<sup>n</sup>)<sup>248</sup> = q<sup>−31/3</sup> η(q)<sup>248</sup>, read off E8's roots:", t0, labelSize * .21f, new Color(.8f, .84f, .95f), TextAlignmentOptions.Left);
        for (int K = 1; K <= 12; K++)
        {
            int col = (K - 1) / 6, row = (K - 1) % 6;
            Vector3 at = t0 + new Vector3(col * 120f, -(row + 1) * 4.9f, 0f);
            ExpLabelWide(g, $"q<sup>{K}</sup>", at, labelSize * .2f, new Color(.7f, .74f, .88f), TextAlignmentOptions.Left);
            ExpLabelWide(g, coef[K - 1], at + new Vector3(68f, 0f, 0f), labelSize * .2f, K % 2 == 1 ? new Color(.45f, .8f, 1f) : PrimeGold, TextAlignmentOptions.Right);
        }
    }

    // ================================================================================================
    // IC. ζ at minus E8's exponents; the Weyl group counted by ζ at negative integers
    // ================================================================================================

    /// <summary>log₁₀ |ζ(−m)| for the odd m ≤ 29, E8's exponents in gold, and the exact values at the exponents.</summary>
    public void ExpZetaAtExponents(Vector3 o, float labelSize)
    {
        Transform g = ExpGroup("IC zeta").transform;
        (int m, string value, double log)[] z =
        {
            (1, "−1/12", -1.0792), (3, "1/120", -2.0792), (5, "−1/252", -2.4014), (7, "1/240", -2.3802), (9, "−1/132", -2.1206),
            (11, "691/32760", -1.6760), (13, "−1/12", -1.0792), (15, "3617/8160", -0.3533), (17, "−43867/14364", 0.4849),
            (19, "174611/6600", 1.4225), (21, "−77683/276", 2.4494), (23, "236364091/65520", 3.5572), (25, "−657931/12", 4.7390),
            (27, "3392780147/3480", 5.9889), (29, "−1723168255201/85932", 7.3021)
        };
        const float x0 = -116f, dx = 8.2f, yb = -38f, k = 6f;
        List<(Vector3, Vector3)> axis = new() { (o + new Vector3(x0 - 2f, yb, 0f), o + new Vector3(x0 + 15 * dx, yb, 0f)) };
        for (int v = -2; v <= 7; v += 3)
        {
            float y = yb + (v + 3) * k;
            axis.Add((o + new Vector3(x0 - 2.5f, y, 0f), o + new Vector3(x0 - 1.2f, y, 0f)));
            ExpLabelWide(g, $"10<sup>{v}</sup>".Replace("-", "−"), o + new Vector3(x0 - 3.4f, y, 0f), labelSize * .18f, new Color(.6f, .64f, .8f), TextAlignmentOptions.Right);
        }

        Segments(g, "axis", axis, .12f, new Color(.6f, .64f, .8f) * .7f, Factory.NoGlow);
        for (int i = 0; i < z.Length; i++)
        {
            bool e8 = System.Array.IndexOf(E8Exp, z[i].m) >= 0;
            float x = x0 + (i + .5f) * dx;
            ExpLine(g, $"bar {z[i].m}", dx * .62f, e8 ? PrimeGold : new Color(.42f, .45f, .58f), e8 ? Factory.SoftGlowIntensity : Factory.NoGlow,
                new[] { o + new Vector3(x, yb, 0f), o + new Vector3(x, yb + (float)(z[i].log + 3d) * k, 0f) });
            ExpLabelWide(g, $"−{z[i].m}", o + new Vector3(x, yb - 3.2f, 0f), labelSize * .17f, e8 ? PrimeGold : new Color(.6f, .64f, .8f));
        }

        ExpLabelWide(g, "|ζ(−m)|,  m odd", o + new Vector3(x0 + 7.5f * dx, yb - 7.4f, 0f), labelSize * .19f, new Color(.7f, .74f, .88f));
        float ty = 34f;
        ExpLabelWide(g, "at E8's exponents", o + new Vector3(18f, ty, 0f), labelSize * .22f, PrimeGold, TextAlignmentOptions.Left);
        int row = 1;
        foreach (var (m, value, _) in z)
        {
            if (System.Array.IndexOf(E8Exp, m) < 0) continue;
            string v = value.Contains("691") ? value.Replace("691", "<color=#FF8A6A>691</color>") : value;
            ExpLabelWide(g, $"ζ(−{m})", o + new Vector3(18f, ty - row * 5.4f, 0f), labelSize * .21f, Color.white, TextAlignmentOptions.Left);
            ExpLabelWide(g, $"= {v}", o + new Vector3(36f, ty - row * 5.4f, 0f), labelSize * .21f, new Color(.85f, .87f, .96f), TextAlignmentOptions.Left);
            row++;
        }
    }

    // ================================================================================================
    // ID. Sato–Tate from E8's theta series
    // ================================================================================================

    static List<float> DeltaAnglesOnSpoke(int hour)
    {
        List<float> a = new();
        for (int i = 0; i < LieGutsData.Primes.Length; i++)
            if (hour < 0 || LieGutsData.Primes[i] % 30 == hour) a.Add(LieGutsData.DeltaAngles[i]);
        return a;
    }

    /// <summary>A histogram of angles in [0, π] as density, with SU(2)'s (2/π) sin²θ over it.</summary>
    void AngleHistogram(Transform g, Vector3 o, List<float> angles, int bins, float w, float h, float lineWidth, Color barColour)
    {
        int[] count = new int[bins];
        foreach (float a in angles) count[Mathf.Clamp((int)(a / Mathf.PI * bins), 0, bins - 1)]++;
        float bw = Mathf.PI / bins, scale = h / .7f;
        for (int b = 0; b < bins; b++)
        {
            float density = count[b] / (angles.Count * bw);
            float x = (b + .5f) / bins * w;
            ExpLine(g, $"bin {b}", w / bins * .82f, barColour, Factory.NoGlow, new[] { o + new Vector3(x, 0f, 0f), o + new Vector3(x, density * scale, 0f) });
        }

        List<Vector3> curve = new();
        for (int i = 0; i <= 80; i++)
        {
            float th = i / 80f * Mathf.PI;
            curve.Add(o + new Vector3(i / 80f * w, 2f / Mathf.PI * Mathf.Sin(th) * Mathf.Sin(th) * scale, -.2f));
        }

        ExpLine(g, "SU(2)", lineWidth, Color.white, Factory.GlowIntensity, curve.ToArray());
        Segments(g, "base", new List<(Vector3, Vector3)> { (o, o + new Vector3(w, 0f, 0f)) }, lineWidth * .5f, new Color(.6f, .64f, .8f) * .7f, Factory.NoGlow);
    }

    /// <summary>All primes p ≤ 20000: the histogram of θ_p against (2/π) sin²θ, and the moments of 2cos θ_p against the Catalan numbers.</summary>
    public void ExpSatoTate(Vector3 o, float labelSize)
    {
        Transform g = ExpGroup("ID sato-tate").transform;
        List<float> all = DeltaAnglesOnSpoke(-1);
        Vector3 h0 = o + new Vector3(-118f, -40f, 0f);
        const float w = 132f, h = 62f;
        AngleHistogram(g, h0, all, 36, w, h, .35f, new Color(.38f, .62f, 1f) * .9f);
        string[] ticks = { "0", "π/4", "π/2", "3π/4", "π" };
        for (int i = 0; i < 5; i++) ExpLabelWide(g, ticks[i], h0 + new Vector3(i * w / 4f, -3.4f, 0f), labelSize * .2f, new Color(.7f, .74f, .88f));
        ExpLabelWide(g, "θ<sub>p</sub>", h0 + new Vector3(w / 2f, -8f, 0f), labelSize * .22f, Color.white);
        ExpLabelWide(g, "white:  (2/π) sin²θ,  SU(2)'s Haar measure", h0 + new Vector3(w * .5f, h * 1.2f, 0f), labelSize * .2f, Color.white);

        // moments of 2 cos θ against the Catalan numbers (the SU(2)-invariants of V⊗2k)
        int[] catalan = { 1, 2, 5, 14, 42 };
        Vector3 t0 = o + new Vector3(30f, 26f, 0f);
        ExpLabelWide(g, "k", t0, labelSize * .2f, new Color(.7f, .74f, .88f), TextAlignmentOptions.Left);
        ExpLabelWide(g, "mean (2cos θ<sub>p</sub>)<sup>2k</sup>", t0 + new Vector3(8f, 0f, 0f), labelSize * .2f, new Color(.7f, .74f, .88f), TextAlignmentOptions.Left);
        ExpLabelWide(g, "Catalan", t0 + new Vector3(68f, 0f, 0f), labelSize * .2f, new Color(.7f, .74f, .88f), TextAlignmentOptions.Left);
        for (int k = 1; k <= 5; k++)
        {
            double sum = 0d;
            foreach (float a in all) sum += System.Math.Pow(2d * System.Math.Cos(a), 2 * k);
            Vector3 r = t0 + new Vector3(0f, -k * 6f, 0f);
            ExpLabelWide(g, k.ToString(), r, labelSize * .22f, Color.white, TextAlignmentOptions.Left);
            ExpLabelWide(g, (sum / all.Count).ToString("0.000"), r + new Vector3(8f, 0f, 0f), labelSize * .22f, new Color(.45f, .7f, 1f), TextAlignmentOptions.Left);
            ExpLabelWide(g, catalan[k - 1].ToString(), r + new Vector3(68f, 0f, 0f), labelSize * .22f, PrimeGold, TextAlignmentOptions.Left);
        }

        ExpLabelWide(g, $"{all.Count} primes p ≤ 20 000", t0 + new Vector3(0f, -38f, 0f), labelSize * .2f, new Color(.8f, .84f, .95f), TextAlignmentOptions.Left);
    }

    /// <summary>The same angles, prime by prime on E8's eight spokes (p mod 30): a small histogram at each gold hour.</summary>
    public void ExpSatoTateSpokes(Vector3 o, float labelSize)
    {
        Transform g = ExpGroup("ID spokes").transform;
        const float w = 44f, h = 19f;
        for (int i = 0; i < E8Exp.Length; i++)
        {
            int hour = E8Exp[i];
            int col = i % 4, row = i / 4;
            Vector3 at = o + new Vector3(-120f + col * 52f, 16f - row * 42f, 0f);
            List<float> angles = DeltaAnglesOnSpoke(hour);
            AngleHistogram(g, at, angles, 14, w, h, .24f, PrimeGold * .75f);
            ExpLabelWide(g, $"hour {hour}:  {angles.Count} primes", at + new Vector3(w * .5f, -3.4f, 0f), labelSize * .19f, PrimeGold);
        }

        // E8's wheel with its eight gold spokes, small, at the right
        Vector3 c = o + new Vector3(96f, -6f, 0f);
        const float R = 17f;
        List<Vector3> ring = new();
        for (int i = 0; i <= 120; i++)
        {
            float a = i / 120f * Mathf.PI * 2f;
            ring.Add(c + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * R);
        }

        ExpLine(g, "wheel", .16f, new Color(.5f, .55f, .75f) * .6f, Factory.NoGlow, ring.ToArray());
        List<(Vector3, Vector3)> spokes = new();
        foreach (int hour in E8Exp)
        {
            float a = (90f - 12f * hour) * Mathf.Deg2Rad;
            Vector3 d = new(Mathf.Cos(a), Mathf.Sin(a), 0f);
            spokes.Add((c, c + d * R));
            ExpLabelWide(g, hour.ToString(), c + d * (R + 3.4f), labelSize * .18f, PrimeGold);
        }

        Segments(g, "spokes", spokes, .24f, PrimeGold * .7f, Factory.NoGlow);
        ExpLabelWide(g, "E8's wheel:  the 8 gold hours", c + new Vector3(0f, -R - 7f, 0f), labelSize * .19f, new Color(.8f, .84f, .95f));
    }
}
