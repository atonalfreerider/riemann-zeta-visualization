using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The zeros inside E8's stack (docs/experiments/README.md, sections JA–JF): where the zeros of ζ sit in the factor funnel
/// of "The paths back" (n at hour n mod 30, radius s√n, height log n). All numbers were checked in Python first
/// (Tools/zero_stack_data.py writes the zeros and their spacing statistics into ZeroStackData.cs).
///   JA  a zero 1/2 + iγ turns the number n by γ·log n, its height times γ: each zero is a coil up the funnel's axis; at the
///       height of a prime power the coils of all the zeros lean toward −1 (Landau), elsewhere they cancel
///   JB  summed over 10 500 zeros (with ζ's pole) they make one function of height: a tooth at every prime power, as long as
///       log p, nothing in between; spun about the axis, a ring through the top of that column — the height, not the hour
///   JC  E8's Coxeter element makes E8 a lattice over Z[ζ30]; the field Q(ζ30) has eight L-functions, one per character of
///       (Z/30)*; the zeros of each grow the same teeth, turned to χ(p)
///   JD  turned back by χ(h) and added, the eight sets of zeros keep only the prime powers on spoke h: one comb per gold
///       spoke, each tooth reaching the top of its column — the height and the hour
///   JE  log n = Σ_{d | n} Λ(d): every drop of a path back is the lamp the zeros light on a prime power dividing n
///   JF  the zeros among themselves: within one L-function they repel like the eigenvalues of a random unitary matrix;
///       zeros of different L-functions of Q(ζ30) do not see each other
/// </summary>
public partial class SacksSpiral
{
    public const float ZeroStackOffset = 66000f;
    static int[] ZsGold => ZeroSums.Gold;
    const double ZsSigmaL = ZeroSums.SigmaL;

    static Vector3 ZsHourDir(int h)
    {
        float a = (90f - 12f * h) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
    }

    /// <summary>One colour per gold hour, evenly round the hue circle (the same in JC and JD).</summary>
    public static Color ZsHourColor(int h)
    {
        int i = Array.IndexOf(ZsGold, ((h % 30) + 30) % 30);
        return i < 0 ? new Color(.7f, .72f, .85f) : Color.HSVToRGB((i / 8f + .02f) % 1f, .62f, 1f);
    }

    /// <summary>A key: "hour of n mod 30:" and the eight gold hours in their colours, then 2, 3, 5 in theirs, along <paramref name="right"/>.</summary>
    public void ExpHourKey(Vector3 at, Vector3 right, float size, bool ramified)
    {
        Transform g = ExpGroup("hour key").transform;
        ExpLabelWide(g, "colour = hour of n mod 30:", at, size, new Color(.82f, .85f, .94f), TextAlignmentOptions.Right);
        float step = size * .16f;
        for (int i = 0; i < ZsGold.Length; i++) ExpLabelWide(g, ZsGold[i].ToString(), at + right * (step * (i + 1.2f)), size * 1.1f, ZsHourColor(ZsGold[i]));
        if (!ramified) return;
        int[] ps = { 2, 3, 5 };
        for (int i = 0; i < 3; i++) ExpLabelWide(g, ps[i].ToString(), at + right * (step * (10f + i)), size * 1.1f, PrimeHue(ps[i]));
    }

    // ================================================================================================
    // The module "The zeros inside E8's stack" (experiments.json, xzeros)
    // ================================================================================================

    public const float ZeroModuleOffset = 68000f;
    const int ZmN = 400;
    const float ZmS = 2.6f, ZmH = 6f;

    /// <summary>The sums the module draws, worked out once on a worker thread (started with the experiments).</summary>
    sealed class ZeroModuleCache
    {
        public readonly Dictionary<int, (double[] z, double[] w)> Teeth = new();
        public readonly Dictionary<int, double> Peak = new();
        public double[] FanZ;
        public double[][] FanW = new double[8][];
        public readonly Dictionary<int, double> FanTip = new();
    }

    static System.Threading.Tasks.Task<ZeroModuleCache> zeroModuleJob;

    /// <summary>Start the module's sums in the background (cheap to call again).</summary>
    public static void WarmZeroModule() => zeroModuleJob ??= System.Threading.Tasks.Task.Run(ComputeZeroModule);

    static ZeroModuleCache ComputeZeroModule()
    {
        ZeroModuleCache c = new();
        ZeroSums.Sum zeta = ZeroSums.ZetaAll;
        double sig = zeta.Sigma;
        for (int m = 2; m <= ZmN; m++)
        {
            if (!ZeroSums.PrimePower(m, out _, out _)) continue;
            double zc = Math.Log(m);
            List<double> z = new(), w = new();
            for (int j = -26; j <= 26; j++)
            {
                double zz = zc + j * .23d / sig;
                z.Add(zz);
                w.Add(zeta.At(zz).Re);
            }

            c.Teeth[m] = (z.ToArray(), w.ToArray());
            c.Peak[m] = zeta.At(zc).Re;
        }

        List<double> spikes = new();
        for (int m = 7; m <= ZmN; m++)
            if (m % 2 != 0 && m % 3 != 0 && m % 5 != 0 && ZeroSums.PrimePower(m, out _, out _))
                spikes.Add(Math.Log(m));
        List<double> zs = ZeroSums.Samples(1.0d, Math.Log(ZmN) + .02d, .004d, spikes, ZeroSums.SigmaL);
        c.FanZ = zs.ToArray();
        for (int h = 0; h < 8; h++) c.FanW[h] = new double[zs.Count];
        ZeroSums.Sum[] sums = ZeroSums.LSums;
        Cpx[] wk = new Cpx[8];
        for (int i = 0; i < zs.Count; i++)
        {
            for (int k = 0; k < 8; k++) wk[k] = sums[k].At(zs[i]);
            for (int h = 0; h < 8; h++) c.FanW[h][i] = ZeroSums.SpokeFrom(ZeroSums.Gold[h], zs[i], wk);
        }

        foreach (int hour in ZeroSums.Gold)
            for (int m = hour; m <= ZmN; m += 30)
                if (ZeroSums.PrimePower(m, out _, out _))
                    c.FanTip[m] = ZeroSums.Spoke(hour, Math.Log(m));
        return c;
    }

    /// <summary>
    /// The funnel scene of the module: the factor funnel for n ≤ 400 and, each in its own reveal group (hidden until a line
    /// shows it): "JM coils" (the first three zeros), "JM disc 7" and "JM disc 6" (where all 10 500 coils point at those
    /// heights), "JM comb" (the funnel's profile and the axis, then one tooth per prime power from the bottom up), "JM comb
    /// labels", "JM rings" (one per prime power) and "JM fans" (one per gold hour, with its tips and its number).
    /// </summary>
    public void ExpZeroModuleFunnel(Vector3 o, float labelSize)
    {
        WarmZeroModule();
        ZeroModuleCache cache = zeroModuleJob.Result;
        ExpFactorColumns(o, ZmN, ZmS, ZmH, .55f, 0f, new long[0], true, false, .6f);
        Vector3 Up(double h) => Vector3.back * (ZmH * (float)h);
        Vector3 side = Vector3.right;
        float Reach(double z, double w) => (float)(ZmS * Math.Exp(z / 2d) * w / z);
        Transform Item(Transform g, string name)
        {
            GameObject go = new(name);
            go.transform.SetParent(g, false);
            return go.transform;
        }

        // the coils
        Transform coils = ExpGroup("JM coils").transform;
        Color[] cc = { new(1f, .85f, .1f), new(.35f, .85f, 1f), new(1f, .45f, .8f) };
        float zMax = Mathf.Log(ZmN);
        for (int k = 0; k < 3; k++)
        {
            float r = ZmS * (3.4f - .9f * k), gm = (float)ZeroStackData.Zeta[k];
            int steps = Mathf.CeilToInt(gm * zMax / (2f * Mathf.PI) * 48f);
            Vector3[] pts = new Vector3[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                float z = zMax * i / steps, a = gm * z;
                pts[i] = o + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f) + Up(z);
            }

            ExpLine(coils, $"coil {k + 1}", .34f - .06f * k, cc[k], Factory.GlowIntensity, pts);
        }

        // where all the coils point, at the heights of 7 and 6: rings of bars round the axis
        foreach (int n in new[] { 7, 6 })
        {
            Transform g = ExpGroup($"JM disc {n}").transform;
            double[] zs = ZeroStackData.Zeta;
            const int bins = 48;
            int[] hist = new int[bins];
            double zn = Math.Log(n), mr = 0d, mi = 0d;
            foreach (double gm in zs)
            {
                double a = gm * zn;
                mr += Math.Cos(a);
                mi += Math.Sin(a);
                hist[(int)(a % (2d * Math.PI) / (2d * Math.PI) * bins) % bins]++;
            }

            mr /= zs.Length;
            mi /= zs.Length;
            Vector3 c = o + Up(zn);
            float r0 = ZmS * 5.5f, unit = ZmS * 3.6f, mean = zs.Length / (float)bins;
            Color col = n == 7 ? new Color(1f, .68f, .25f) : new Color(.75f, .78f, .9f);
            List<(Vector3, Vector3)> ring = new(), bars = new();
            for (int i = 0; i < 96; i += 2)
            {
                float a0 = i / 96f * Mathf.PI * 2f, a1 = (i + 1) / 96f * Mathf.PI * 2f;
                ring.Add((c + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * (r0 + unit), c + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * (r0 + unit)));
            }

            for (int i = 0; i < bins; i++)
            {
                float a = (i + .5f) / bins * Mathf.PI * 2f;
                Vector3 d = new(Mathf.Cos(a), Mathf.Sin(a), 0f);
                bars.Add((c + d * r0, c + d * (r0 + unit * hist[i] / mean)));
            }

            Segments(g, "even share", ring, .08f, Color.white * .5f, Factory.NoGlow);
            Segments(g, "bars", bars, .42f, col, Factory.SoftGlowIntensity);
            float gain = r0 * .9f / .12f;
            Transform arrow = Item(g, "mean");
            ExpLine(arrow, "mean line", .36f, Color.white, AttentionIntensity, new[] { c, c + new Vector3((float)mr, (float)mi, 0f) * gain });
            ExpDot(arrow, "mean tip", c + new Vector3((float)mr, (float)mi, 0f) * gain, 1.1f, Color.white, AttentionIntensity);
        }

        // ζ's comb: the profile and the axis, then the teeth from the bottom up
        Transform comb = ExpGroup("JM comb").transform;
        Transform combLabels = ExpGroup("JM comb labels").transform;
        Transform rings = ExpGroup("JM rings").transform;
        Transform frame = Item(comb, "frame");
        List<Vector3> profile = new();
        for (double z = .42d; z <= Math.Log(ZmN) + .02d; z += .02d) profile.Add(o + side * (float)(ZmS * Math.Exp(z / 2d)) + Up(z));
        ExpLine(frame, "funnel profile", .1f, new Color(.5f, .55f, .75f) * .7f, Factory.NoGlow, profile.ToArray());
        ExpLine(frame, "axis", .12f, Color.white * .7f, Factory.SoftGlowIntensity, new[] { o + Up(.42d), o + Up(Math.Log(ZmN) + .02d) });
        List<int> pp = new(cache.Teeth.Keys);
        pp.Sort();
        foreach (int m in pp)
        {
            var (z, w) = cache.Teeth[m];
            Vector3[] pts = new Vector3[z.Length];
            for (int i = 0; i < z.Length; i++) pts[i] = o + side * Reach(z[i], w[i]) + Up(z[i]);
            ExpLine(comb, $"tooth {m}", .16f, Color.white, Factory.GlowIntensity, pts);
            ZeroSums.PrimePower(m, out long p, out int j);
            double zm = Math.Log(m), peak = cache.Peak[m];
            float rr = Reach(zm, peak);
            Vector3[] loop = new Vector3[97];
            for (int i = 0; i < loop.Length; i++)
            {
                float a = i / 96f * Mathf.PI * 2f;
                loop[i] = o + new Vector3(Mathf.Cos(a) * rr, Mathf.Sin(a) * rr, 0f) + Up(zm);
            }

            ExpLine(rings, $"ring {m}", j == 1 ? .09f : .06f, SegmentColor(p) * (j == 1 ? .8f : .5f), Factory.SoftGlowIntensity, loop);
            if (j == 1 && (m <= 13 || m == 31 || m == 97 || m == 211 || m == 397))
                ExpLabelWide(combLabels, m.ToString(), o + side * (rr + (m >= 31 ? 5f : 1.8f)) + Up(zm) + Vector3.back * (m == 13 ? 1.4f : m == 11 ? -1f : 0f),
                    labelSize * .2f, SegmentColor(p), TextAlignmentOptions.Left);
        }

        // E8's fans: one per gold hour
        Transform fans = ExpGroup("JM fans").transform;
        for (int h = 0; h < 8; h++)
        {
            int hour = ZeroSums.Gold[h];
            Transform fan = Item(fans, $"fan {hour}");
            Vector3 u = ZsHourDir(hour);
            Color col = ZsHourColor(hour);
            Vector3[] pts = new Vector3[cache.FanZ.Length];
            for (int i = 0; i < pts.Length; i++) pts[i] = o + u * Reach(cache.FanZ[i], cache.FanW[h][i]) + Up(cache.FanZ[i]);
            ExpLine(fan, "comb", .17f, col, Factory.GlowIntensity, pts);
            List<Vector3> tips = new();
            List<Color> tc = new();
            List<float> ts = new();
            foreach (var kv in cache.FanTip)
            {
                if (kv.Key % 30 != hour) continue;
                double z = Math.Log(kv.Key);
                ZeroSums.PrimePower(kv.Key, out _, out int j);
                tips.Add(o + u * Reach(z, kv.Value) + Up(z));
                tc.Add(col);
                ts.Add(j == 1 ? .8f : .55f);
            }

            ExpPointCloud(fan, "tips", tips, tc, ts, Factory.GlowIntensity);
            ExpLabelWide(fan, hour.ToString(), o + u * (ZmS * Mathf.Sqrt(ZmN) * 1.13f) + Up(Math.Log(ZmN)), labelSize * .3f, col);
        }

        foreach (string name in new[] { "JM coils", "JM disc 7", "JM disc 6", "JM comb", "JM comb labels", "JM rings", "JM fans" }) ExpHideItems(name);
    }

    // ================================================================================================
    // JA. a zero is a coil up the axis
    // ================================================================================================

    /// <summary>
    /// The factor funnel (dimmed) with the first three zeros of ζ as coils up its axis: at height z = log n the coil of γ has
    /// turned γz, the turn of the term n^(−1/2 − iγ). Two discs mark the heights of 6 and 7.
    /// </summary>
    public void ExpZeroCoils(Vector3 o, int nMax, float s, float hScale, float dot, float labelSize)
    {
        Transform g = ExpGroup("JA coils").transform;
        ExpFactorColumns(o, nMax, s, hScale, dot, 0f, new long[0], true, false, .32f);
        Vector3 Up(float h) => Vector3.back * (hScale * h);
        Color[] cc = { new(1f, .85f, .1f), new(.35f, .85f, 1f), new(1f, .45f, .8f) };
        float zMax = Mathf.Log(nMax);
        for (int k = 0; k < 3; k++)
        {
            float r = s * (3.4f - .9f * k), gm = (float)ZeroStackData.Zeta[k];
            int steps = Mathf.CeilToInt(gm * zMax / (2f * Mathf.PI) * 56f);
            Vector3[] pts = new Vector3[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                float z = zMax * i / steps, a = gm * z;
                pts[i] = o + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f) + Up(z);
            }

            ExpLine(g, $"coil {k + 1}", dot * (.34f - .06f * k), cc[k], Factory.GlowIntensity, pts);
        }

        foreach (int n in new[] { 6, 7 })
        {
            float z = Mathf.Log(n);
            Vector3[] ring = new Vector3[73];
            float rr = s * 4.4f;
            for (int i = 0; i < ring.Length; i++)
            {
                float a = i / 72f * Mathf.PI * 2f;
                ring[i] = o + new Vector3(Mathf.Cos(a) * rr, Mathf.Sin(a) * rr, 0f) + Up(z);
            }

            Color rc = n == 7 ? new Color(1f, .68f, .25f) : new Color(.75f, .78f, .9f);
            ExpLine(g, $"height of {n}", dot * .14f, rc, Factory.SoftGlowIntensity, ring);
            ExpLabelWide(g, $"log {n}", o + new Vector3(n == 7 ? rr * 1.55f : -rr * 1.55f, 0f, 0f) + Up(z), labelSize * .2f, rc);
        }
    }

    /// <summary>
    /// The phases γ·log n (mod 2π) of all 10 500 zeros, as a ring of bars in the plane (right, up): bar length ∝ the number of
    /// zeros in each 7.5° bin (the dashed circle is the even share), and the arrow the mean of e^(iγ log n), scaled up.
    /// Landau: Σ_{γ ≤ T} n^(iγ) = −(T/2π)·Λ(n)/√n + O(log T), so the mean leans to −1 by Λ(n)/√n·T/(2π N(T)) at a prime power.
    /// </summary>
    public void ExpPhaseWheel(Vector3 c, Vector3 right, Vector3 up, int n, float radius, float labelSize, string title)
    {
        Transform g = ExpGroup($"JA phases {n}").transform;
        double[] zs = ZeroStackData.Zeta;
        const int bins = 48;
        int[] count = new int[bins];
        double mr = 0d, mi = 0d, z = Math.Log(n);
        foreach (double gm in zs)
        {
            double a = gm * z;
            mr += Math.Cos(a); mi += Math.Sin(a);
            double ph = a % (2d * Math.PI);
            count[(int)(ph / (2d * Math.PI) * bins) % bins]++;
        }

        mr /= zs.Length; mi /= zs.Length;
        float r0 = radius * .42f, unit = radius * .4f, mean = zs.Length / (float)bins;
        Vector3 P(float ang, float rad) => c + (right * Mathf.Cos(ang) + up * Mathf.Sin(ang)) * rad;
        List<(Vector3, Vector3)> bars = new();
        for (int i = 0; i < bins; i++)
        {
            float a = (i + .5f) / bins * Mathf.PI * 2f;
            bars.Add((P(a, r0), P(a, r0 + unit * count[i] / mean)));
        }

        Segments(g, "bars", bars, radius * .045f, n == 7 ? new Color(1f, .68f, .25f) : new Color(.75f, .78f, .9f), Factory.SoftGlowIntensity);
        List<(Vector3, Vector3)> dash = new();
        for (int i = 0; i < 96; i += 2) dash.Add((P(i / 96f * Mathf.PI * 2f, r0 + unit), P((i + 1) / 96f * Mathf.PI * 2f, r0 + unit)));
        Segments(g, "even share", dash, radius * .012f, Color.white * .6f, Factory.NoGlow);
        Vector3[] baseRing = new Vector3[97];
        for (int i = 0; i < baseRing.Length; i++) baseRing[i] = P(i / 96f * Mathf.PI * 2f, r0);
        ExpLine(g, "base", radius * .01f, new Color(.5f, .55f, .75f) * .7f, Factory.NoGlow, baseRing);
        Segments(g, "axes", new List<(Vector3, Vector3)> { (P(0f, r0 * .15f), P(0f, r0 * .9f)), (P(Mathf.PI, r0 * .15f), P(Mathf.PI, r0 * .9f)) }, radius * .008f, Color.white * .35f, Factory.NoGlow);
        float gain = r0 * .9f / .12f;
        Vector3 tip = c + (right * (float)mr + up * (float)mi) * gain;
        ExpLine(g, "mean", radius * .035f, Color.white, AttentionIntensity, new[] { c, tip });
        ExpDot(g, "mean tip", tip, radius * .07f, Color.white, AttentionIntensity);
        ExpLabelWide(g, "+1", P(0f, r0 + unit * 1.55f), labelSize * .2f, new Color(.7f, .74f, .88f));
        ExpLabelWide(g, "−1", P(Mathf.PI, r0 + unit * 1.55f), labelSize * .2f, new Color(.7f, .74f, .88f));
        ExpLabelWide(g, title, c + up * (r0 + unit * 2.05f), labelSize * .24f, Color.white);
        string m = $"mean of n<sup>iγ</sup>:  {mr:+0.000;−0.000}" + (Math.Abs(mi) >= .0005d ? $" {(mi >= 0 ? "+" : "−")} {Math.Abs(mi):0.000}i" : "");
        ExpLabelWide(g, m, c - up * (r0 + unit * 1.9f), labelSize * .21f, new Color(.82f, .85f, .94f));
        double lam = 0d;
        for (long q = 2; q <= n; q++)
        {
            if (n % q != 0) continue;
            long t = n;
            while (t % q == 0) t /= q;
            lam = t == 1 ? Math.Log(q) : 0d;
            break;
        }

        double pred = -zs[^1] / (2d * Math.PI) * lam / Math.Sqrt(n) / zs.Length;
        ExpLabelWide(g, $"Landau:  −(T/2π)·Λ({n})/√{n} / N(T) = {pred:+0.000;−0.000}", c - up * (r0 + unit * 2.6f), labelSize * .18f, new Color(.7f, .74f, .88f));
    }

    // ================================================================================================
    // JB. ζ's zeros, summed: a tooth at the height of every prime power
    // ================================================================================================

    /// <summary>
    /// ζ's 10 500 zeros (and its pole) summed at every height, W(z) → Λ(n) at z = log n, drawn as a comb in the vertical
    /// half-plane toward <paramref name="side"/>: radius s·e^(z/2)·W(z)/z, so a tooth of log p reaches the funnel (the top of
    /// p's column), a tooth at p^j reaches 1/j of the way. Spun round the axis each tooth is a ring at that height: the zeros fix
    /// the height of every prime power but are the same in every direction.
    /// </summary>
    public void ExpZetaComb(Vector3 o, int nMax, float s, float hScale, float dot, float labelSize, Vector3 side)
    {
        Transform g = ExpGroup("JB comb").transform;
        ExpFactorColumns(o, nMax, s, hScale, dot, 0f, new long[0], true, false, .3f);
        ExpEnsureSieve(nMax + 2);
        Vector3 Up(double h) => Vector3.back * (hScale * (float)h);
        side = new Vector3(side.x, side.y, 0f).normalized;
        ZeroSums.Sum zeta = ZeroSums.ZetaAll;
        List<double> spikes = new();
        List<long> pp = new();
        for (long m = 2; m <= nMax; m++)
            if (ZeroSums.PrimePower(m, out _, out _)) { pp.Add(m); spikes.Add(Math.Log(m)); }

        double zTop = Math.Log(nMax) + .02d;
        List<double> zs = ZeroSums.Samples(.42d, zTop, .0025d, spikes, zeta.Sigma);
        Vector3[] comb = new Vector3[zs.Count];
        for (int i = 0; i < zs.Count; i++)
        {
            double z = zs[i], w = zeta.At(z).Re;
            comb[i] = o + side * (float)(s * Math.Exp(z / 2d) * w / z) + Up(z);
        }

        ExpLine(g, "comb", dot * .16f, Color.white, Factory.GlowIntensity, comb);
        List<Vector3> profile = new();
        for (double z = .42d; z <= zTop; z += .02d) profile.Add(o + side * (float)(s * Math.Exp(z / 2d)) + Up(z));
        ExpLine(g, "funnel profile", dot * .1f, new Color(.5f, .55f, .75f) * .7f, Factory.NoGlow, profile.ToArray());
        ExpLine(g, "axis", dot * .1f, new Color(.6f, .64f, .8f) * .6f, Factory.NoGlow, new[] { o, o + Up(zTop) });
        List<Vector3> tips = new(); List<Color> tc = new(); List<float> ts = new();
        foreach (long m in pp)
        {
            ZeroSums.PrimePower(m, out long p, out int j);
            double z = Math.Log(m), w = zeta.At(z).Re;
            float rr = (float)(s * Math.Sqrt(m) * w / z);
            Color col = SegmentColor(p);
            Vector3[] ring = new Vector3[97];
            for (int i = 0; i < ring.Length; i++)
            {
                float a = i / 96f * Mathf.PI * 2f;
                ring[i] = o + new Vector3(Mathf.Cos(a) * rr, Mathf.Sin(a) * rr, 0f) + Up(z);
            }

            ExpLine(g, $"ring {m}", dot * (j == 1 ? .07f : .05f), col * (j == 1 ? .75f : .5f), Factory.SoftGlowIntensity, ring);
            tips.Add(o + side * rr + Up(z)); tc.Add(col); ts.Add(dot * (j == 1 ? .75f : .5f));
            if (j == 1 && (m <= 13 || m == 31 || m == 97 || m == 211 || m == 397))
                ExpLabelWide(g, m.ToString(), o + side * (rr + dot * (m >= 31 ? 9f : 3f)) + Up(z) + Vector3.back * (m == 13 ? 1.4f : m == 11 ? -1f : m >= 31 ? 1.2f : 0f), labelSize * .2f, col, TextAlignmentOptions.Left);
        }

        ExpPointCloud(g, "tips", tips, tc, ts, Factory.GlowIntensity);
    }

    // ================================================================================================
    // JC. the eight L-functions of Q(ζ30), each turned to χ(p)
    // ================================================================================================

    /// <summary>
    /// For each of the eight characters of E8's exponent group (Z/30)* (= (Z/15)*), the zeros of L(s, χ) summed at every
    /// height up to log nMax, W_χ(z) → χ(n)·Λ(n), drawn as a curve about a vertical axis: Re to the right, Im away. Each tooth
    /// points along χ(p); tips coloured by the hour of n (2, 3, 5 in their own colours).
    /// </summary>
    public void ExpCharacterTrees(Vector3 o, float dx, int nMax, float hScale, float tooth, float dot, float labelSize, Vector3 imDir, Vector3 rowDir,
        int rows = 1, float rowGap = 0f)
    {
        Transform g = ExpGroup("JC trees").transform;
        ExpEnsureSieve(nMax + 2);
        Vector3 Up(double h) => Vector3.back * (hScale * (float)h);
        ZeroSums.Sum[] sums = ZeroSums.LSums;
        List<double> spikes = new();
        List<long> pp = new();
        for (long m = 2; m <= nMax; m++)
            if (ZeroSums.PrimePower(m, out _, out _)) { pp.Add(m); spikes.Add(Math.Log(m)); }

        double zTop = Math.Log(nMax) + .03d;
        List<double> zs = ZeroSums.Samples(.42d, zTop, .003d, spikes, ZsSigmaL);
        string[] names = { "trivial:  ζ itself", "mod 3", "mod 5,  real", "mod 15,  real", "mod 5,  χ(7) = i", "mod 5,  χ(7) = −i", "mod 15,  χ(7) = i", "mod 15,  χ(7) = −i" };
        int perRow = 8 / Mathf.Max(1, rows);
        for (int k = 0; k < 8; k++)
        {
            int col = k % perRow, row = k / perRow;
            Vector3 b = o + rowDir * (dx * (col - (perRow - 1) * .5f)) + Vector3.back * (rowGap * (rows - 1 - row));
            Transform tg = new GameObject($"tree {k}").transform;
            tg.SetParent(g, false);
            Vector3[] curve = new Vector3[zs.Count];
            for (int i = 0; i < zs.Count; i++)
            {
                Cpx w = sums[k].At(zs[i]);
                curve[i] = b + (Vector3.right * (float)w.Re + imDir * (float)w.Im) * tooth + Up(zs[i]);
            }

            ExpLine(tg, $"tree {k}", dot * .1f, Color.white * .55f, Factory.SoftGlowIntensity, curve);
            ExpLine(tg, $"axis {k}", dot * .08f, new Color(.6f, .64f, .8f) * .5f, Factory.NoGlow, new[] { b, b + Up(zTop) });
            List<(Vector3, Vector3)> cross = new()
            {
                (b - Vector3.right * tooth * 2.2f, b + Vector3.right * tooth * 2.2f), (b - imDir * tooth * 2.2f, b + imDir * tooth * 2.2f),
            };
            Segments(tg, $"compass {k}", cross, dot * .07f, new Color(.6f, .64f, .8f) * .6f, Factory.NoGlow);
            if (k == 0 || k == 4)
            {
                ExpLabelWide(tg, "1", b + Vector3.right * tooth * 2.8f, labelSize * .17f, new Color(.7f, .74f, .88f));
                ExpLabelWide(tg, "i", b + imDir * tooth * 2.8f, labelSize * .17f, new Color(.7f, .74f, .88f));
            }

            List<Vector3> tips = new(); List<Color> tc = new(); List<float> ts = new();
            Dictionary<int, List<(Vector3, Vector3)>> teeth = new();
            foreach (long m in pp)
            {
                ZeroSums.PrimePower(m, out long p, out int j);
                if (ZeroSums.Chi(k, m).Re == 0d && ZeroSums.Chi(k, m).Im == 0d) continue;
                Cpx w = sums[k].At(Math.Log(m));
                Vector3 axisPt = b + Up(Math.Log(m)), tip = axisPt + (Vector3.right * (float)w.Re + imDir * (float)w.Im) * tooth;
                tips.Add(tip);
                int key = p <= 5 ? (int)p : (int)(m % 30);
                tc.Add(p <= 5 ? PrimeHue(p) : ZsHourColor(key));
                ts.Add(dot * (j == 1 ? .75f : .5f));
                if (!teeth.ContainsKey(key)) teeth[key] = new();
                teeth[key].Add((axisPt, tip));
            }

            foreach (var kv in teeth) Segments(tg, $"teeth {k} {kv.Key}", kv.Value, dot * .26f, kv.Key <= 5 ? PrimeHue(kv.Key) : ZsHourColor(kv.Key), Factory.SoftGlowIntensity);

            ExpPointCloud(tg, $"tips {k}", tips, tc, ts, Factory.GlowIntensity);
            ExpLabelWide(tg, names[k], b - Vector3.up * (dot * 2f) + Up(-.9d), labelSize * .2f, k == 0 ? Factory.Palette.Zero : new Color(.85f, .88f, .96f));
        }
    }

    // ================================================================================================
    // JD. turned back by χ(h) and added: one comb per gold spoke
    // ================================================================================================

    /// <summary>
    /// The factor funnel (dimmed) and, for each of E8's eight gold hours h, (1/8) Σ_χ conj χ(h) W_χ(z) drawn in the vertical
    /// half-plane of spoke h at radius s·e^(z/2)·W/z: a tooth reaching the top of every prime's column on that spoke (1/j of
    /// the way for p^j), nothing at the composites. Zeros of the eight L-functions to t = 1000.
    /// </summary>
    public void ExpSpokeCombs(Vector3 o, int nMax, float s, float hScale, float dot, float labelSize)
    {
        Transform g = ExpGroup("JD combs").transform;
        ExpFactorColumns(o, nMax, s, hScale, dot, 0f, new long[0], true, false, .3f);
        ExpEnsureSieve(nMax + 2);
        Vector3 Up(double h) => Vector3.back * (hScale * (float)h);
        double zTop = Math.Log(nMax) + .02d;
        // the eight sums once, on heights shared by all the spokes (fine around every prime power prime to 30)
        List<double> spikes = new();
        for (long m = 7; m <= nMax; m++)
            if (m % 2 != 0 && m % 3 != 0 && m % 5 != 0 && ZeroSums.PrimePower(m, out _, out _)) spikes.Add(Math.Log(m));
        List<double> zs = ZeroSums.Samples(1.0d, zTop, .004d, spikes, ZsSigmaL);
        ZeroSums.Sum[] sums = ZeroSums.LSums;
        Cpx[][] w = new Cpx[zs.Count][];
        for (int i = 0; i < zs.Count; i++)
        {
            w[i] = new Cpx[8];
            for (int k = 0; k < 8; k++) w[i][k] = sums[k].At(zs[i]);
        }

        foreach (int h in ZsGold)
        {
            Vector3 u = ZsHourDir(h);
            Vector3[] comb = new Vector3[zs.Count];
            for (int i = 0; i < zs.Count; i++)
            {
                double z = zs[i];
                comb[i] = o + u * (float)(s * Math.Exp(z / 2d) * ZeroSums.SpokeFrom(h, z, w[i]) / z) + Up(z);
            }

            Color col = ZsHourColor(h);
            ExpLine(g, $"comb {h}", dot * .15f, col, Factory.GlowIntensity, comb);
            List<Vector3> tips = new(); List<Color> tc = new(); List<float> ts = new();
            for (long m = h; m <= nMax; m += 30)
            {
                if (!ZeroSums.PrimePower(m, out _, out int j)) continue;
                double z = Math.Log(m);
                tips.Add(o + u * (float)(s * Math.Exp(z / 2d) * ZeroSums.Spoke(h, z) / z) + Up(z));
                tc.Add(col); ts.Add(dot * (j == 1 ? .7f : .5f));
            }

            ExpPointCloud(g, $"tips {h}", tips, tc, ts, Factory.GlowIntensity);
            ExpLabelWide(g, h.ToString(), o + u * (s * Mathf.Sqrt(nMax) * 1.12f) + Up(Math.Log(nMax)), labelSize * .3f, col);
        }
    }

    // ================================================================================================
    // JE. every drop of a path back is a lamp the zeros light
    // ================================================================================================

    /// <summary>
    /// The paths back from each n, unrolled left to right as staircases (height log of the number, unit per log unit): one
    /// drop of log p per prime factor, smallest first. Beside each drop, in the prime's colour, the sum of ζ's 10 500 zeros
    /// (with its pole) at the prime power p^j the drop removes (p^j the full power of p in the number before the drop): it is
    /// log p, as tall as the drop. Under each stop, the same sum at the stop itself: 0 unless the stop is a prime power.
    /// log n = Σ_{d | n} Λ(d).
    /// </summary>
    public void ExpPathLampChart(Vector3 o, long[] ns, float xStep, float unit, float groupGap, float dot, float labelSize, bool stacked = false, float rowGap = 0f)
    {
        Transform g = ExpGroup("JE path lamps").transform;
        ExpEnsureSieve(2000);
        ZeroSums.Sum zeta = ZeroSums.ZetaAll;
        double Lamp(long m) => m < 2 ? 0d : zeta.At(Math.Log(m)).Re;
        Color dim = new(.6f, .64f, .8f);
        float x = 0f;
        Vector3 o0 = o;
        for (int r = 0; r < ns.Length; r++)
        {
            long n0 = ns[r];
            Transform pg = new GameObject($"path {n0}").transform;
            pg.SetParent(g, false);
            if (stacked)
            {
                o = o0 + Vector3.down * (rowGap * r);
                x = 0f;
            }

            List<(Vector3, Vector3)> guide = new();
            List<long> stops = new() { n0 };
            for (long n = n0; n > 1;) { n /= Factorize(n)[0]; stops.Add(n); }
            float x0 = x;
            for (int i = 0; i < stops.Count; i++)
            {
                long a = stops[i];
                float xa = x0 + i * xStep, ya = unit * Mathf.Log(a);
                Vector3 pa = o + new Vector3(xa, ya, 0f);
                ExpDot(pg, $"stop {n0}.{i}", pa + Vector3.back * .2f, dot * 1.3f, Color.white, Factory.GlowIntensity);
                if (i == 0) ExpLabelWide(pg, a.ToString(), pa + new Vector3(-1.2f, 3.2f, 0f), labelSize * .2f, Color.white, TextAlignmentOptions.Right);
                else ExpLabelWide(pg, a.ToString(), pa + new Vector3(-1.8f, -3.4f, 0f), labelSize * .2f, Color.white);
                double la = Lamp(a);
                string lt = Math.Abs(la) < .0005d ? "0.000" : la.ToString("0.000");
                ExpLabelWide(pg, lt, o + new Vector3(xa, -7f, 0f), labelSize * .17f, Math.Abs(la) < .05d ? dim : (a <= 13 ? PrimeHue(a) : PrimeGold), TextAlignmentOptions.Center);
                guide.Add((o + new Vector3(xa, -4.5f, 0f), o + new Vector3(xa, -1f, 0f)));
                if (i + 1 == stops.Count) break;
                long b = stops[i + 1], p = a / b;
                int j = 0;
                for (long t = a; t % p == 0; t /= p) j++;
                long d = 1;
                for (int k = 0; k < j; k++) d *= p;
                float xd = xa + xStep * .38f, yb = unit * Mathf.Log(b);
                Color pc = SegmentColor(p);
                ExpLine(pg, $"tread {n0}.{i}", dot * .32f, Color.white * .85f, Factory.SoftGlowIntensity, new[] { pa, o + new Vector3(xd, ya, 0f) });
                ExpLine(pg, $"drop {n0}.{i}", dot * .5f, Color.white, Factory.GlowIntensity, new[] { o + new Vector3(xd, ya, 0f), o + new Vector3(xd, yb, 0f) });
                ExpLine(pg, $"after {n0}.{i}", dot * .32f, Color.white * .85f, Factory.SoftGlowIntensity, new[] { o + new Vector3(xd, yb, 0f), o + new Vector3(xa + xStep, yb, 0f) });
                double ld = Lamp(d);
                float xl = xa + xStep * .62f;
                ExpLine(pg, $"lamp {n0}.{i}", dot * 1.25f, pc, AttentionIntensity, new[] { o + new Vector3(xl, yb, -.05f), o + new Vector3(xl, yb + unit * (float)ld, -.05f) });
                string dl = j == 1 ? d.ToString() : $"{p}{LabMath.Superscript(j)}";
                ExpLabelWide(pg, $"÷{p}", o + new Vector3(xd - 1f, yb + 1.7f, 0f), labelSize * .18f, Color.white, TextAlignmentOptions.Right);
                ExpLabelWide(pg, $"{dl}:  {ld:0.000}", o + new Vector3(xl + 1.6f, yb + unit * (float)ld * .5f, 0f), labelSize * .17f, pc, TextAlignmentOptions.Left);
            }

            x = x0 + (stops.Count - 1) * xStep + groupGap;
            Segments(pg, "guides", guide, .08f, dim * .5f, Factory.NoGlow);
        }
    }

    // ================================================================================================
    // JF. the zeros among themselves
    // ================================================================================================

    /// <summary>
    /// Three charts: the pair correlation of ζ's zeros 1000 … 10 500 against Montgomery's 1 − (sin πu/πu)² (GUE); the
    /// correlation between zeros of two different L-functions of Q(ζ30) (flat: they ignore each other); nearest-neighbour
    /// spacings, ζ alone against the GUE (Wigner) curve and all eight merged against the Poisson curve e^(−s).
    /// </summary>
    public void ExpZeroSpacing(Vector3 o, float labelSize, bool stacked = false)
    {
        Transform g = ExpGroup("JF spacing").transform;
        const float w = 66f, hgt = 42f;
        Color axisC = new Color(.6f, .64f, .8f) * .7f, text = new(.8f, .84f, .95f);
        void Axes(Vector3 c, float x0, float x1, string xl, float[] ticks, string[] tickText)
        {
            Segments(g, $"axes {xl}", new List<(Vector3, Vector3)> { (c, c + Vector3.right * w), (c, c + Vector3.up * hgt) }, .16f, axisC, Factory.NoGlow);
            for (int i = 0; i < ticks.Length; i++)
            {
                float x = (ticks[i] - x0) / (x1 - x0) * w;
                Segments(g, $"tick {xl} {i}", new List<(Vector3, Vector3)> { (c + Vector3.right * x, c + new Vector3(x, -1.2f, 0f)) }, .12f, axisC, Factory.NoGlow);
                ExpLabelWide(g, tickText[i], c + new Vector3(x, -3.6f, 0f), labelSize * .18f, text);
            }

            ExpLabelWide(g, xl, c + new Vector3(w * .5f, -7.6f, 0f), labelSize * .19f, text);
        }

        void Bars(Vector3 c, float[] vals, float x0, float x1, float yMax, Color col, bool filled, string name)
        {
            float bw = w / vals.Length;
            if (filled)
            {
                for (int i = 0; i < vals.Length; i++)
                    ExpLine(g, $"{name} {i}", bw * .82f, col, Factory.SoftGlowIntensity, new[] { c + new Vector3((i + .5f) * bw, 0f, -.05f), c + new Vector3((i + .5f) * bw, vals[i] / yMax * hgt, -.05f) });
            }
            else
            {
                List<(Vector3, Vector3)> o2 = new();
                for (int i = 0; i < vals.Length; i++)
                {
                    float y = vals[i] / yMax * hgt, yp = i > 0 ? vals[i - 1] / yMax * hgt : 0f;
                    o2.Add((c + new Vector3(i * bw, yp, -.1f), c + new Vector3(i * bw, y, -.1f)));
                    o2.Add((c + new Vector3(i * bw, y, -.1f), c + new Vector3((i + 1) * bw, y, -.1f)));
                }

                Segments(g, name, o2, .22f, col, Factory.SoftGlowIntensity);
            }
        }

        void Curve(Vector3 c, Func<float, float> f, float x0, float x1, float yMax, Color col, string name, bool dashed = false)
        {
            List<Vector3> pts = new();
            for (int i = 0; i <= 160; i++)
            {
                float x = x0 + (x1 - x0) * i / 160f;
                pts.Add(c + new Vector3(i / 160f * w, Mathf.Min(f(x), yMax) / yMax * hgt, -.2f));
            }

            if (!dashed) { ExpLine(g, name, .28f, col, Factory.GlowIntensity, pts.ToArray()); return; }
            List<(Vector3, Vector3)> d = new();
            for (int i = 0; i + 1 < pts.Count; i += 4) d.Add((pts[i], pts[Mathf.Min(i + 2, pts.Count - 1)]));
            Segments(g, name, d, .2f, col, Factory.GlowIntensity);
        }

        static float Gue(float u) => u < 1e-4f ? 0f : 1f - Mathf.Pow(Mathf.Sin(Mathf.PI * u) / (Mathf.PI * u), 2f);
        Color zc = Factory.Palette.Zero, lc = new(.35f, .85f, 1f);

        // 1. within ζ
        Vector3 c1 = stacked ? o + new Vector3(-w * .5f, 62f, 0f) : o + new Vector3(-118f, -20f, 0f);
        Bars(c1, ZeroStackData.PairZeta, 0f, 3f, 1.5f, zc * .8f, true, "pair zeta");
        Bars(c1, ZeroStackData.PairWithinL, 0f, 3f, 1.5f, lc, false, "pair L");
        Curve(c1, Gue, 0f, 3f, 1.5f, Color.white, "gue");
        Axes(c1, 0f, 3f, "u  (distance between two zeros, in mean spacings)", new[] { 0f, 1f, 2f, 3f }, new[] { "0", "1", "2", "3" });
        ExpLabelWide(g, "1", c1 + new Vector3(-2.4f, 1f / 1.5f * hgt, 0f), labelSize * .18f, text, TextAlignmentOptions.Right);
        ExpLabelWide(g, "within one L-function", c1 + new Vector3(w * .5f, hgt + 6f, 0f), labelSize * .25f, Color.white);
        ExpLabelWide(g, "gold:  ζ, zeros 1000 … 10 500\nblue outline:  the other seven, t ≤ 1000\nwhite:  1 − (sin πu / πu)²  (random unitary matrices)",
            c1 + new Vector3(0f, -17.5f, 0f), labelSize * .17f, text, TextAlignmentOptions.Left);

        // 2. across the eight
        Vector3 c2 = stacked ? o + new Vector3(-w * .5f, -18f, 0f) : o + new Vector3(-33f, -20f, 0f);
        Bars(c2, ZeroStackData.PairAcross, -3f, 3f, 1.5f, new Color(.55f, .9f, .6f) * .85f, true, "across");
        Curve(c2, u => Gue(Mathf.Abs(u)), -3f, 3f, 1.5f, Color.white * .8f, "gue across", true);
        Axes(c2, -3f, 3f, "u  (from a zero of one L-function to a zero of another)", new[] { -3f, -2f, -1f, 0f, 1f, 2f, 3f }, new[] { "−3", "−2", "−1", "0", "1", "2", "3" });
        ExpLabelWide(g, "between different L-functions of Q(ζ<sub>30</sub>)", c2 + new Vector3(w * .5f, hgt + 6f, 0f), labelSize * .25f, Color.white);
        ExpLabelWide(g, "green:  pairs from two different characters\ndashed:  the hole they would show if they repelled",
            c2 + new Vector3(0f, -16.5f, 0f), labelSize * .17f, text, TextAlignmentOptions.Left);

        // 3. nearest neighbours
        Vector3 c3 = stacked ? o + new Vector3(-w * .5f, -98f, 0f) : o + new Vector3(52f, -20f, 0f);
        Bars(c3, ZeroStackData.SpacingMerged, 0f, 3f, 1.2f, new Color(.55f, .9f, .6f) * .8f, true, "merged");
        Bars(c3, ZeroStackData.SpacingZeta, 0f, 3f, 1.2f, zc, false, "spacing zeta");
        Curve(c3, s => 32f / (Mathf.PI * Mathf.PI) * s * s * Mathf.Exp(-4f * s * s / Mathf.PI), 0f, 3f, 1.2f, Color.white, "wigner");
        Curve(c3, s => Mathf.Exp(-s), 0f, 3f, 1.2f, Color.white * .8f, "poisson", true);
        Axes(c3, 0f, 3f, "s  (gap to the next zero, in mean spacings)", new[] { 0f, 1f, 2f, 3f }, new[] { "0", "1", "2", "3" });
        ExpLabelWide(g, "gaps to the next zero", c3 + new Vector3(w * .5f, hgt + 6f, 0f), labelSize * .25f, Color.white);
        ExpLabelWide(g, "gold outline:  ζ alone      white:  the GUE gap law\ngreen:  all eight merged      dashed:  e<sup>−s</sup>  (no repulsion)",
            c3 + new Vector3(0f, -16.5f, 0f), labelSize * .17f, text, TextAlignmentOptions.Left);
    }
}
