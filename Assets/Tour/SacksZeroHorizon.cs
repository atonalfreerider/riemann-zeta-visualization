using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The chain's horn and how far the zeros see (docs/experiments/README.md, sections OB and OD). The links n^(−s) on the
/// critical line are one inward logarithmic spiral, radius 1/√n, sampled at the heights log n; their running sum climbs an
/// outward horn, radius √n/|1 − s|, around ζ(s), turning at the same rate t per unit of log n: at every height the two radii
/// are the sides of one rectangle of area 1/|1 − s|. At a zero the horn's axis passes through the chain's own start.
/// The landscape rebuilds Λ(n) from the first K zeros (data from Tools/zero_horizon_data.py): every n up to about
/// 0.6 t/τ comes out exact, and the horizon moves out with the height of the zeros.
/// </summary>
public partial class SacksSpiral
{
    public const float ZeroHorizonOffset = 112000f;

    /// <summary>
    /// The chain at height t as a horn (OB2): partial sums S_n at height H·log n/log nMax, the links as spokes from the axis
    /// through the start, faint rings on the horn's cone, the axis through ζ(s).
    /// </summary>
    public void ExpChainHorn(Vector3 o, double t, float L, float H, float labelSize, int nMax = 20000)
    {
        Transform g = ExpGroup("OB chain horn").transform;
        double lnMax = System.Math.Log(nMax);
        float Hgt(double n) => (float)(H * System.Math.Log(n) / lnMax);
        Vector3 P(double re, double im, float h) => o + L * ((float)re * Vector3.right + (float)im * Vector3.forward) + h * Vector3.up;
        double fold = System.Math.Sqrt(t / (2 * System.Math.PI)), last = t / (2 * System.Math.PI);
        // ζ(s) as the far end of the chain: the partial sum minus the horn's growth n^(1−s)/(1−s) and half the last link
        double Sre = 0, Sim = 0;
        List<(Vector3, Vector3, Color, float)> chain = new(), spokes = new();
        Vector3 prev = P(0, 0, 0f);
        double turned = 0, cRe = 0, cIm = 0;
        Color gold = new(1f, .82f, .34f), magenta = new(1f, .45f, .8f), cyan = new(.35f, .9f, .9f);
        for (int n = 1; n <= nMax; n++)
        {
            double ln = System.Math.Log(n), mag = 1 / System.Math.Sqrt(n), ph = -t * ln;
            double lre = mag * System.Math.Cos(ph), lim = mag * System.Math.Sin(ph);
            Sre += lre; Sim += lim;
            if (n <= 300)
                spokes.Add((P(0, 0, Hgt(n)), P(lre, lim, Hgt(n)), new Color(1f, .82f, .34f) * .8f, L * .03f));
            turned += t / n;
            if (n <= 400 || turned > .05 || n == nMax)
            {
                turned = 0;
                Vector3 p = P(Sre, Sim, Hgt(n));
                Color col = n <= fold ? gold : n <= last ? magenta : Color.Lerp(cyan, Color.white, (float)(ln / lnMax));
                chain.Add((prev, p, col, L * (n <= 400 ? .035f : .03f)));
                prev = p;
            }

            if (n == nMax)
            {
                // the horn's centre: S_n − n^(1−s)/(1−s) − n^(−s)/2
                double are = .5, aim = -t;                                   // 1 − s
                double pre = System.Math.Sqrt(n) * System.Math.Cos(ph), pim = System.Math.Sqrt(n) * System.Math.Sin(ph);   // n^(1−s)
                double d = are * are + aim * aim;
                double qre = (pre * are + pim * aim) / d, qim = (pim * are - pre * aim) / d;
                cRe = Sre - qre - .5 * lre; cIm = Sim - qim - .5 * lim;
            }
        }

        MeshRenderer cm = ColouredSegments(g, "chain", chain, Factory.GlowIntensity);
        cm.sortingOrder = 6;
        ColouredSegments(g, "links", spokes, Factory.NoGlow).sortingOrder = 4;
        // the horn's cone, a ring every decade, and its axis through ζ(s)
        double modS = System.Math.Sqrt(.25 + t * t);
        List<(Vector3, Vector3)> rings = new();
        foreach (int n in new[] { 10, 100, 1000, 10000 })
        {
            if (n > nMax) continue;
            double r = System.Math.Sqrt(n) / modS;
            for (int k = 0; k < 64; k++)
            {
                double a0 = k * System.Math.PI * 2 / 64, a1 = (k + 1) * System.Math.PI * 2 / 64;
                rings.Add((P(cRe + r * System.Math.Cos(a0), cIm + r * System.Math.Sin(a0), Hgt(n)), P(cRe + r * System.Math.Cos(a1), cIm + r * System.Math.Sin(a1), Hgt(n))));
            }

            TextBox l = ExpLabelWide(g, $"n = {n:N0}", P(cRe + r, cIm, Hgt(n)) + Vector3.right * (L * .4f), labelSize * .13f, new Color(.75f, .78f, .9f), TextAlignmentOptions.Left);
            Outline(l.TextField);
        }

        Segments(g, "cone rings", rings, L * .02f, new Color(.55f, .6f, .78f) * .7f);
        ExpLine(g, "axis", L * .05f, Color.white * .9f, Factory.GlowIntensity, new[] { P(cRe, cIm, -H * .04f), P(cRe, cIm, H * 1.04f) });
        ExpDot(g, "start", P(0, 0, 0f), L * .2f, Color.white, AttentionIntensity);
        double off = System.Math.Sqrt(cRe * cRe + cIm * cIm);
        if (off > .05)
        {
            ExpLine(g, "start's vertical", L * .03f, new Color(.6f, .62f, .75f), Factory.NoGlow, new[] { P(0, 0, 0f), P(0, 0, H * .6f) });
            ExpLine(g, "offset", L * .05f, new Color(1f, .45f, .45f), Factory.GlowIntensity, new[] { P(0, 0, 0f), P(cRe, cIm, 0f) });
            TextBox ol = ExpLabelWide(g, $"the horn's axis is ζ(s) = {cRe:0.00}{(cIm >= 0 ? "+" : "−")}{System.Math.Abs(cIm):0.00}i from the start",
                P(cRe * .5, cIm * .5, 0f) + Vector3.down * (L * .9f), labelSize * .14f, new Color(1f, .55f, .55f));
            Outline(ol.TextField);
        }
        else
        {
            TextBox sl = ExpLabelWide(g, "the start, under the horn's axis", P(0, 0, 0f) + Vector3.down * (L * .9f), labelSize * .14f, Color.white);
            Outline(sl.TextField);
        }
    }

    /// <summary>
    /// How far the zeros see (OD3): a row of rebuilt teeth Λ̂(n) for each of 24 numbers of zeros K (front: 10 000, back: 10),
    /// each row drawn a little past its own horizon (to 1.6 x* + 12); exact teeth gold inside the horizon and white past it,
    /// wrong ones red; the horizon a cyan line across the rows.
    /// </summary>
    public void ExpCertaintyLandscape(Vector3 o, float dx, float dz, float hz, float labelSize)
    {
        Transform g = ExpGroup("OD landscape").transform;
        int rowsN = ZeroHorizonData.K.Length, nMax = ZeroHorizonData.NMax;
        short[] teeth = ZeroHorizonData.Teeth;
        float[] lam = new float[nMax + 1];
        for (int n = 2; n <= nMax; n++)
        {
            int m = n, p = 0;
            for (int q = 2; q * q <= m && p == 0; q++)
                if (m % q == 0) p = q;
            if (p == 0) { lam[n] = Mathf.Log(n); continue; }
            while (m % p == 0) m /= p;
            lam[n] = m == 1 ? Mathf.Log(p) : 0f;
        }

        // row i (K[i] zeros) sits rowsN − 1 − i rows back, so the row with the most zeros runs along the front
        Vector3 At(float n, int i, float h) => o + Vector3.right * ((n - 2f) * dx) + Vector3.forward * ((rowsN - 1 - i) * dz) + Vector3.up * (h * hz);
        List<(Vector3, Vector3, Color, float)> bars = new();
        Color ok = new(.92f, .93f, 1f), primeCol = new(1f, .82f, .34f), wrong = new(1f, .35f, .35f);
        int[] reach = new int[rowsN];
        for (int i = 0; i < rowsN; i++) reach[i] = Mathf.Min(nMax, Mathf.RoundToInt(ZeroHorizonData.Horizon[i] * 1.6f) + 12);
        for (int i = 0; i < rowsN; i++)
            for (int n = 2; n <= reach[i]; n++)
            {
                float v = teeth[i * (nMax - 1) + (n - 2)] / 100f;
                bool good = Mathf.Abs(v - lam[n]) <= .3f;
                if (good && lam[n] == 0f) continue;
                v = Mathf.Clamp(v, -1.5f, 8.5f);
                if (Mathf.Abs(v) < .04f) continue;
                Color c = !good ? wrong : n <= ZeroHorizonData.Horizon[i] ? primeCol : ok;
                bars.Add((At(n, i, 0f), At(n, i, v), c * (good ? 1f : .9f), dx * .55f));
            }

        ColouredSegments(g, "teeth", bars, Factory.SoftGlowIntensity).sortingOrder = 5;
        // the floor: one line per row, the horizon across them, the axis of n at the front
        List<(Vector3, Vector3)> floor = new();
        for (int i = 0; i < rowsN; i++) floor.Add((At(2, i, 0f), At(reach[i], i, 0f)));
        Segments(g, "rows", floor, dx * .6f, new Color(.45f, .5f, .68f) * .6f);
        List<Vector3> hz2 = new();
        for (int i = 0; i < rowsN; i++) hz2.Add(At(ZeroHorizonData.Horizon[i] + .5f, i, .02f));
        OnTop(ExpLine(g, "horizon", dx * 2.4f, new Color(.35f, .9f, .9f), Factory.GlowIntensity, hz2.ToArray()), 8);
        Color axisCol = new(.7f, .74f, .88f);
        List<(Vector3, Vector3)> ticks = new();
        for (int n = 100; n <= nMax; n += 100)
        {
            ticks.Add((At(n, rowsN - 1, 0f) + Vector3.back * dz * .3f, At(n, rowsN - 1, 0f) + Vector3.back * dz * .9f));
            ExpLabelWide(g, n.ToString(), At(n, rowsN - 1, 0f) + Vector3.back * dz * 1.8f, labelSize * .12f, axisCol);
        }

        Segments(g, "ticks", ticks, dx * .6f, axisCol * .8f);
        ExpLabelWide(g, "n", At(nMax, rowsN - 1, 0f) + Vector3.back * dz * 1.8f + Vector3.right * dx * 40f, labelSize * .14f, axisCol, TextAlignmentOptions.Left);
        for (int i = 0; i < rowsN; i++)
        {
            int K = ZeroHorizonData.K[i];
            if (i % 4 != 0 && i != rowsN - 1) continue;
            TextBox l = ExpLabelWide(g, $"{K:N0} zeros (t ≤ {ZeroHorizonData.T[i]:N0})", At(2, i, 0f) + Vector3.left * dx * 25f, labelSize * .12f, axisCol,
                TextAlignmentOptions.Right);
            Outline(l.TextField);
        }

        int last = rowsN - 1;
        TextBox hl = ExpLabelWide(g, $"horizon:  every n ≤ {ZeroHorizonData.Horizon[last]} exact with {ZeroHorizonData.K[last]:N0} zeros",
            At(ZeroHorizonData.Horizon[last], last, 0f) + Vector3.back * dz * 4.2f + Vector3.left * dx * 120f, labelSize * .15f, new Color(.35f, .9f, .9f));
        Outline(hl.TextField);
    }
}
