using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The funnel scene's transforms (SacksFunnelWorld), so each picture of the zeros module grows out of the last:
///   fw.scan   a ring climbs the funnel's axis; at its height the coils of all 10 500 zeros add up to one hand, drawn out to
///             the side, and the comb's teeth appear as the ring passes them: the comb is that hand at every height
///   fw.spin   the comb turns about the axis, each tooth drawing its ring: ζ's zeros fix a prime's height, not its hour
///   ExpFunnelChars / ExpFunnelTurn   at the height of n, the teeth of E8's eight L-functions, turned by their characters
///             χ(n) (±1, ±i); turned back by χ(h) for each gold hour h and chained, they line up only for the hour of n
///   fw.fan    the comb splits into eight copies that swing out to the eight gold hours, each becoming that hour's fan:
///             (1/8) Σ conj χ(h) W_χ, which keeps only the prime powers on spoke h
/// </summary>
public partial class SacksSpiral
{
    sealed class FanCache
    {
        public double[] Z;
        public double[] W0;
        public double[][] F = new double[8][];
    }

    static System.Threading.Tasks.Task<FanCache> fanJob;

    static FanCache ComputeFanCache()
    {
        FanCache c = new();
        List<double> spikes = new();
        for (long m = 2; m <= ZmN; m++) if (ZeroSums.PrimePower(m, out _, out _)) spikes.Add(Math.Log(m));
        List<double> zs = ZeroSums.Samples(.6d, Math.Log(ZmN) + .02d, .004d, spikes, ZeroSums.SigmaL);
        c.Z = zs.ToArray();
        c.W0 = new double[zs.Count];
        for (int h = 0; h < 8; h++) c.F[h] = new double[zs.Count];
        ZeroSums.Sum[] sums = ZeroSums.LSums;
        Cpx[] wk = new Cpx[8];
        for (int i = 0; i < zs.Count; i++)
        {
            for (int k = 0; k < 8; k++) wk[k] = sums[k].At(zs[i]);
            c.W0[i] = wk[0].Re;
            for (int h = 0; h < 8; h++) c.F[h][i] = ZeroSums.SpokeFrom(ZeroSums.Gold[h], zs[i], wk);
        }

        return c;
    }

    float FwReach(double z, double w) => (float)(ZmS * Math.Exp(z / 2d) * w / Math.Max(.2d, z));

    // the scene's extra parts
    LineRenderer fwScanRing, fwScanHand;
    TextBox fwScanTag;
    readonly List<(long m, double z, float r, LineRenderer arc)> fwArcs = new();
    readonly LineRenderer[] fwFans = new LineRenderer[8];
    Transform fwCombGroup, fwCombLabels;
    string fwMorphSig = "";

    /// <summary>
    /// For callouts on the comb (exp:calltooth:N): the tip of n's tooth, where the hand of all the coils reaches at the
    /// height log n; for a number with no tooth (a composite), the axis at that height, where the hand stays.
    /// </summary>
    public Vector3 FwToothTip(long n)
    {
        double z = Math.Log(Math.Max(2, n));
        Vector3 axis = FwOrigin + FwUp(z) + Vector3.back * .3f;
        if (!ZeroSums.PrimePower(n, out _, out _)) return axis;
        WarmZeroModule();
        ZeroModuleCache cache = zeroModuleJob.Result;
        return cache.Peak.TryGetValue((int)n, out double peak) ? axis + Vector3.right * FwReach(z, peak) : axis;
    }

    /// <summary>Start the fans' sums on a worker thread (cheap to call again).</summary>
    public static void WarmFans() => fanJob ??= System.Threading.Tasks.Task.Run(ComputeFanCache);

    /// <summary>Called by the funnel scene's loop every frame: the scan, the spin and the fans.</summary>
    void FwMorphUpdate(FwScene s)
    {
        float scan = Tw("fw.scan", -1f), spin = Tw("fw.spin", 0f), fan = Tw("fw.fan", 0f);
        string sig = $"{scan:F4}|{spin:F4}|{fan:F4}";
        if (sig == fwMorphSig) return;
        fwMorphSig = sig;
        Vector3 o = FwOrigin;
        Vector3 side = Vector3.right;
        // ---- the scan: the comb drawn by a climbing ring ----
        fwCombGroup ??= ExpRoot().Find("JM comb");
        bool scanning = scan >= 0f;
        if (fwScanRing == null)
        {
            fwScanRing = ExpLine(s.Root, "scan ring", .22f, Color.white, AttentionIntensity, new[] { o, o });
            fwScanHand = ExpLine(s.Root, "scan hand", .4f, Color.white, AttentionIntensity, new[] { o, o });
            fwScanTag = ExpLabelWide(s.Root, "", o, 12f, Color.white, TextAlignmentOptions.Left);
            Outline(fwScanTag.TextField);
        }

        fwScanRing.gameObject.SetActive(scanning && scan < 99f);
        fwScanHand.gameObject.SetActive(scanning && scan < 99f);
        fwScanTag.gameObject.SetActive(scanning && scan < 99f);
        // the teeth and their numbers follow the ring only while it climbs (scan < 99); once the comb is whole they are left
        // to exp:show / exp:hide, so a later spin or fan never brings back a hidden part
        if (scanning && scan < 99f && fwCombGroup != null)
        {
            float z = Mathf.Min(scan, (float)Math.Log(ZmN) + .02f);
            foreach (Transform t in fwCombGroup)
            {
                if (t.name == "frame") { t.gameObject.SetActive(true); continue; }
                if (t.name.StartsWith("tooth ") && long.TryParse(t.name.Substring(6), out long m))
                    t.gameObject.SetActive(Math.Log(m) <= z + .002);
            }

            // each prime's number appears with its tooth
            fwCombLabels ??= ExpRoot().Find("JM comb labels");
            if (fwCombLabels != null)
                foreach (TextBox l in fwCombLabels.GetComponentsInChildren<TextBox>(true))
                    if (long.TryParse(l.TextField.text, out long lm)) l.gameObject.SetActive(Math.Log(lm) <= z + .002);

            if (scan < 99f)
            {
                Vector3 c = o + FwUp(z);
                Vector3[] ring = new Vector3[65];
                float rr = ZmS * Mathf.Exp(z / 2f) * 1.05f;
                for (int i = 0; i <= 64; i++) ring[i] = c + FwDir(i * Math.PI / 32) * rr + Vector3.back * .05f;
                fwScanRing.positionCount = 65;
                fwScanRing.SetPositions(ring);
                double w = ZeroSums.ZetaAll.At(z).Re;
                float reach = Mathf.Clamp(FwReach(z, w), -rr * 1.4f, rr * 1.4f);
                fwScanHand.SetPositions(new[] { c + Vector3.back * .1f, c + side * reach + Vector3.back * .1f });
                long n = (long)Math.Round(Math.Exp(z));
                bool lamp = ZeroSums.PrimePower(n, out _, out _) && Math.Abs(Math.Log(n) - z) < .004;
                fwScanTag.TextField.text = lamp ? (Factorize(n).Count == 1 ? $"{n}:  prime" : $"{n} = {FactorString(n)}") : "";
                fwScanTag.transform.position = c + side * (Mathf.Max(reach, 0f) + 2f) + Vector3.back * .3f;
            }
        }

        // ---- the spin: the comb turns about the axis, each tooth drawing its ring ----
        if (spin > 0f && fwCombGroup != null)
        {
            if (fwArcs.Count == 0)
            {
                WarmZeroModule();
                ZeroModuleCache cache = zeroModuleJob.Result;
                foreach (Transform t in fwCombGroup)
                    foreach (LineRenderer l in t.GetComponentsInChildren<LineRenderer>(true)) l.useWorldSpace = false;
                foreach (LineRenderer l in fwCombGroup.GetComponents<LineRenderer>()) l.useWorldSpace = false;
                foreach (var kv in cache.Peak)
                {
                    long m = kv.Key;
                    ZeroSums.PrimePower(m, out long p, out int j);
                    double zm = Math.Log(m);
                    float r = FwReach(zm, kv.Value);
                    LineRenderer arc = ExpLine(s.Root, $"ring {m}", j == 1 ? .1f : .07f, SegmentColor(p) * (j == 1 ? .85f : .55f), Factory.SoftGlowIntensity, new[] { o, o });
                    fwArcs.Add((m, zm, r, arc));
                }
            }

            float ang = spin * 360f;
            fwCombGroup.rotation = Quaternion.AngleAxis(ang, Vector3.back);
            fwCombGroup.position = o - fwCombGroup.rotation * o;
            foreach (var (m, zm, r, arc) in fwArcs)
            {
                int steps = Mathf.Max(2, Mathf.CeilToInt(spin * 96f));
                Vector3[] pts = new Vector3[steps + 1];
                for (int i = 0; i <= steps; i++)
                {
                    float a = spin * 2f * Mathf.PI * i / steps;
                    pts[i] = o + FwUp(zm) + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * r;
                }

                arc.positionCount = pts.Length;
                arc.SetPositions(pts);
            }
        }

        // ---- the fans: eight copies of the comb swing out to the gold hours ----
        bool fanning = fan > 0f;
        if (fanning && fanJob != null && fanJob.IsCompleted)
        {
            FanCache fc = fanJob.Result;
            for (int h = 0; h < 8; h++)
            {
                int hour = ZeroSums.Gold[h];
                if (fwFans[h] == null) fwFans[h] = ExpLine(s.Root, $"fan {hour}", .17f, Color.white, Factory.GlowIntensity, new[] { o, o });
                Vector3 u = ZsHourDir(hour);
                float a0 = 0f, a1 = Mathf.Atan2(u.y, u.x) * Mathf.Rad2Deg;
                float a = Mathf.LerpAngle(a0, a1, fan) * Mathf.Deg2Rad;
                Vector3 dir = new(Mathf.Cos(a), Mathf.Sin(a), 0f);
                Vector3[] pts = new Vector3[fc.Z.Length];
                for (int i = 0; i < fc.Z.Length; i++)
                {
                    double w = (1 - fan) * fc.W0[i] + fan * fc.F[h][i];
                    pts[i] = o + dir * FwReach(fc.Z[i], w) + FwUp(fc.Z[i]) + Vector3.back * .05f;
                }

                fwFans[h].positionCount = pts.Length;
                fwFans[h].SetPositions(pts);
                Color col = Color.Lerp(Color.white, ZsHourColor(hour), fan);
                fwFans[h].startColor = fwFans[h].endColor = col;
                fwFans[h].gameObject.SetActive(true);
            }
        }
        else
            foreach (LineRenderer l in fwFans)
                if (l != null) l.gameObject.SetActive(false);
    }

    // ---- the eight characters at the height of n ----

    /// <summary>At the height of n: eight arrows from the axis, one per L-function of E8's field, pointing along χ(n).</summary>
    public void ExpFunnelChars(long n)
    {
        Transform g = ExpGroup($"FW chars {n}").transform;
        Vector3 c = FwOrigin + FwUp(Math.Log(n)) + Vector3.back * .3f;
        float len = FwCoilR * .55f;
        for (int k = 0; k < 8; k++)
        {
            Cpx x = ZeroSums.Chi(k, n);
            // the eight spread a little round their direction, so the ones that agree can still be told apart
            float spread = (k - 3.5f) * 2.2f * Mathf.Deg2Rad;
            float a = Mathf.Atan2((float)x.Im, (float)x.Re) + spread;
            Color col = k == 0 ? Color.white : Color.HSVToRGB(k / 8f, .55f, 1f);
            LineRenderer l = ExpLine(g, $"chi {k}", .32f, Color.white, AttentionIntensity, new[] { c, c + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * len });
            OnTop(l, 21);
            StartCoroutine(SettleRoutine(l, col, 1f));
        }

        Flare(c, FwCoilR * .6f, 1.2f, 2);
    }

    /// <summary>
    /// Turn the eight arrows at the height of n back by χ(h) for each gold hour h in turn (ending on the hour of n), chained
    /// end to end: they line up (8) only for the hour of n, and cancel for every other; the chain then swings to that spoke.
    /// </summary>
    public void ExpFunnelTurn(long n, float seconds) => StartCoroutine(FunnelTurnRoutine(n, seconds));

    IEnumerator FunnelTurnRoutine(long n, float seconds)
    {
        Transform g = ExpRoot().Find($"FW chars {n}");
        if (g == null) yield break;
        LineRenderer[] arrows = new LineRenderer[8];
        foreach (LineRenderer l in g.GetComponentsInChildren<LineRenderer>(true))
            if (l.name.StartsWith("chi ") && int.TryParse(l.name.Substring(4), out int k)) arrows[k] = l;
        Vector3 c = FwOrigin + FwUp(Math.Log(n)) + Vector3.back * .3f;
        float len = FwCoilR * .22f;
        int own = (int)(n % 30);
        List<int> hours = new();
        foreach (int h in ZeroSums.Gold) if (h != own) hours.Add(h);
        hours.Add(own);
        float per = seconds * .82f / hours.Count;
        Vector3[] prev = new Vector3[8];
        for (int k = 0; k < 8; k++) prev[k] = arrows[k] != null ? arrows[k].GetPosition(1) - arrows[k].GetPosition(0) : Vector3.right * len;
        foreach (int h in hours)
        {
            // the target: χ(n)·conj χ(h), chained from the axis
            Vector3[] dir = new Vector3[8];
            for (int k = 0; k < 8; k++)
            {
                Cpx x = ZeroSums.Chi(k, n), y = ZeroSums.Chi(k, h);
                Cpx z = new(x.Re * y.Re + x.Im * y.Im, x.Im * y.Re - x.Re * y.Im);
                dir[k] = new Vector3((float)z.Re, (float)z.Im, 0f) * len;
            }

            float time = 0f;
            while (time < per)
            {
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / (per * .6f)));
                Vector3 p = c;
                for (int k = 0; k < 8; k++)
                {
                    if (arrows[k] == null) yield break;
                    Vector3 v = Vector3.Slerp(prev[k], dir[k], u);
                    if (v.sqrMagnitude < 1e-6f) v = dir[k];
                    arrows[k].SetPositions(new[] { p, p + v });
                    p += v;
                }

                time += Time.deltaTime;
                yield return null;
            }

            Vector3 sum = Vector3.zero;
            for (int k = 0; k < 8; k++) { prev[k] = dir[k]; sum += dir[k]; }
            bool lined = h == own;
            Board($"turned back by χ({h}):  the eight add to {Mathf.RoundToInt(sum.magnitude / len)}" + "\n" +
                  Small(lined ? $"all eight line up:  {n} is on spoke {h}" : $"they cancel:  {n} is not on spoke {h}"));
            if (lined) Flare(c + sum, FwCoilR * .5f, 1.2f, 2);
        }

        // the lined-up chain swings to its spoke
        Vector3 u0 = Vector3.right, u1 = ZsHourDir(own);
        float t2 = 0f, swing = seconds * .18f;
        while (t2 < swing)
        {
            float u = Mathf.SmoothStep(0f, 1f, t2 / swing);
            Vector3 d = Vector3.Slerp(u0, u1, u).normalized;
            Vector3 p = c;
            for (int k = 0; k < 8; k++)
            {
                if (arrows[k] == null) yield break;
                arrows[k].SetPositions(new[] { p, p + d * len });
                p += d * len;
            }

            t2 += Time.deltaTime;
            yield return null;
        }
    }
}
