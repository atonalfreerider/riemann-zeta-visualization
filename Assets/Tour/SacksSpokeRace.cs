using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The prime race on E8's eight spokes (docs/experiments/README.md, sections MA–MB): the lattice race of section AE run on
/// the wheel of E8's exponents, and rebuilt from the zeros of the L-functions mod 30 of section JC. Checked in Python first
/// (Tools/race_data.py writes RaceData.cs).
///   MA  E_h(x) = (log x/√x)(8 π(x; 30, h) − π*(x)) for the eight gold hours, 10² ≤ x ≤ 10⁷: the squares 1 and 19 run about
///       three units behind (Chebyshev's bias), and the zeros of the seven L-functions mod 30 rebuild every curve
///   MB  why: on a logarithmic clock of 30, squaring doubles the radius and folds the eight hours onto 1 and 19, so every
///       prime square lands there; count each as half a prime (Riemann's Π) and the race is fair
/// </summary>
public partial class SacksSpiral
{
    public const float SpokeRaceOffset = 80000f;

    static Vector3 RaceDir(float h)
    {
        float a = (90f - 12f * h) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
    }

    /// <summary>
    /// One panel of the race: eight curves (n samples each, every <paramref name="stride"/>th of the 2000 points of log₁₀ x from 2
    /// to 7), the dashed <paramref name="means"/>, the y ticks, a title, and the spokes' names at the right end. Returns P(lg, e).
    /// </summary>
    Func<float, float, Vector3> RacePanel(Transform g, Vector3 po, float w, float sy, float[] data, int stride, string title, bool xLabels,
        float labelSize, float[] means)
    {
        const float lo = -8f, hi = 6f;
        Color axisCol = new(.7f, .74f, .88f);
        int n = RaceData.Count / stride;
        Vector3 P(float lg, float e) => po + new Vector3((lg - 4.5f) / 5f * w, (e - (lo + hi) * .5f) * sy, 0f);
        List<(Vector3, Vector3)> ax = new() { (P(2f, lo), P(7f, lo)), (P(2f, lo), P(2f, hi)) };
        for (int d = 2; d <= 7; d++)
        {
            ax.Add((P(d, lo), P(d, lo - .45f)));
            if (xLabels) ExpLabelWide(g, $"x = 10<sup>{d}</sup>", P(d, lo - 2.1f), labelSize * .15f, axisCol);
        }

        foreach (int e in new[] { -6, -3, 0, 3, 6 })
        {
            ax.Add((P(1.97f, e), P(2f, e)));
            ExpLabelWide(g, e.ToString().Replace("-", "−"), P(1.95f, e), labelSize * .14f, axisCol, TextAlignmentOptions.Right);
        }

        Segments(g, "axes", ax, .12f, new Color(.55f, .6f, .78f) * .7f, Factory.NoGlow);
        foreach (float m in means)
        {
            List<(Vector3, Vector3)> dash = new();
            for (float lg = 2f; lg < 7f; lg += .08f) dash.Add((P(lg, m), P(Mathf.Min(lg + .045f, 7f), m)));
            Segments(g, $"mean {m}", dash, .14f, Color.white * .7f, Factory.NoGlow);
        }

        ExpLine(g, "zero", .08f, new Color(.55f, .6f, .78f) * .5f, Factory.NoGlow, new[] { P(2f, 0f), P(7f, 0f) });
        ExpLabelWide(g, title, P(2.06f, hi + .3f), labelSize * .2f, Color.white, TextAlignmentOptions.Left);
        // the six non-squares first, so the two squares draw on top
        foreach (int k in new[] { 1, 2, 3, 4, 6, 7, 0, 5 })
        {
            int h = RaceData.Hours[k];
            bool sq = h == 1 || h == 19;
            Vector3[] pts = new Vector3[n];
            for (int i = 0; i < n; i++)
                pts[i] = P(RaceData.Log0 + RaceData.LogStep * i * stride, Mathf.Clamp(data[k * n + i], lo, hi)) + Vector3.back * ((sq ? .4f : 0f) + .03f * k);
            ExpLine(g, $"spoke {h}", sq ? .5f : .28f, ZsHourColor(h), sq ? Factory.GlowIntensity : Factory.SoftGlowIntensity, pts);
        }

        // the spokes' names at the right end, spread so they do not overlap
        List<(float e, int k)> ends = new();
        for (int k = 0; k < 8; k++) ends.Add((data[k * n + n - 1], k));
        ends.Sort((a, b) => a.e.CompareTo(b.e));
        float last = -99f;
        foreach (var (e, k) in ends)
        {
            float y = Mathf.Max(Mathf.Clamp(e, lo, hi), last + 1.3f);
            last = y;
            ExpLabelWide(g, RaceData.Hours[k].ToString(), P(7f, y) + new Vector3(2.5f, 0f, 0f), labelSize * .18f, ZsHourColor(RaceData.Hours[k]), TextAlignmentOptions.Left);
        }

        return P;
    }

    /// <summary>
    /// Two panels on one axis of log₁₀ x: above, the eight races counted from the primes; below, the same rebuilt from the
    /// zeros alone. Dashed: the predicted means −3 (the squares 1 and 19) and +1 (the other six).
    /// </summary>
    // the counted race's mapping (log₁₀ x, E) → world, for the morph to the fair race
    Func<float, float, Vector3> raceCountedP;

    /// <summary>Morph the counted race's curves into the fair race (each p^k counted as 1/k) in place, over <paramref name="seconds"/>.</summary>
    public void ExpRaceMorph(float seconds) => StartCoroutine(RaceMorphRoutine(seconds));

    System.Collections.IEnumerator RaceMorphRoutine(float seconds)
    {
        Transform g = ExpRoot().Find("MA counted");
        if (g == null || raceCountedP == null) yield break;
        const float lo = -8f, hi = 6f;
        int n = RaceData.Count;
        Dictionary<int, LineRenderer> lines = new();
        foreach (LineRenderer l in g.GetComponentsInChildren<LineRenderer>(true))
            if (l.name.StartsWith("spoke ") && int.TryParse(l.name.Substring(6), out int h)) lines[h] = l;
        float time = 0f;
        while (true)
        {
            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / seconds));
            for (int k = 0; k < 8; k++)
            {
                int h = RaceData.Hours[k];
                if (!lines.TryGetValue(h, out LineRenderer l) || l == null) yield break;
                bool sq = h == 1 || h == 19;
                Vector3[] pts = new Vector3[n];
                for (int i = 0; i < n; i++)
                {
                    float e = Mathf.Lerp(RaceData.Race[k * n + i], RaceData.Fair[k * n + i], u);
                    pts[i] = raceCountedP(RaceData.Log0 + RaceData.LogStep * i, Mathf.Clamp(e, lo, hi)) + Vector3.back * ((sq ? .4f : 0f) + .03f * k);
                }

                l.positionCount = n;
                l.SetPositions(pts);
            }

            if (u >= 1f) yield break;
            time += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>For callouts: the point (log₁₀ x, E) of the counted race, wherever it is drawn (exp:callrace:LG,E).</summary>
    public Vector3 RacePoint(float lg, float e) => raceCountedP != null ? raceCountedP(lg, e) + Vector3.back * .4f : Center;

    public void ExpSpokeRace(Vector3 o, float labelSize)
    {
        // a 9:16 frame stacks the race over the fold, so the race is narrower there
        float w = Tall ? 150f : 280f, sy = Tall ? 4.4f : 4f, gap = Tall ? 76f : 70f;
        raceCountedP = RacePanel(ExpGroup("MA counted").transform, o + Vector3.up * (gap * .5f), w, sy, RaceData.Race, 1,
            "counted:  the primes up to 10<sup>7</sup>", false, labelSize, new[] { -3f, 1f });
        RacePanel(ExpGroup("MA rebuilt").transform, o + Vector3.down * (gap * .5f), w, sy, RaceData.Rebuild, RaceData.RebuildStride,
            $"rebuilt:  from the {RaceData.ZeroCount / 1000} {RaceData.ZeroCount % 1000:000} zeros of the seven L-functions mod 30 up to height 1000",
            true, labelSize, new[] { -3f, 1f });
    }

    /// <summary>
    /// Why the squares lose. A logarithmic clock of 30: n in the direction of its hour at radius ∝ log n (rings at 10, 10², …,
    /// 10⁷). Squaring doubles the radius and sends 1, 11, 19, 29 to hour 1 and 7, 13, 17, 23 to hour 19: an arc from every
    /// prime p ≤ √10⁷ (dots on all eight spokes) to its square (dots on spokes 1 and 19 only, in the colour of p's spoke).
    /// </summary>
    Vector3 foldOrigin;
    float foldRim = 60f;

    /// <summary>Where n sits on the logarithmic clock of the last square fold drawn (its hour, radius ∝ log n).</summary>
    public Vector3 FoldPosition(double n) =>
        foldOrigin + RaceDir((float)(((long)n) % 30)) * (foldRim * (float)(System.Math.Log(n) / 16.11809565095832)) + Vector3.back * .4f;

    public void ExpSquareFold(Vector3 o, float rim, float labelSize)
    {
        foldOrigin = o;
        foldRim = rim;
        Transform g = ExpGroup("MB fold").transform;
        const double logMax = 16.11809565095832; // ln 10⁷
        Color axisCol = new(.7f, .74f, .88f);
        float R(double n) => rim * (float)(Math.Log(n) / logMax);
        for (int k = 1; k <= 7; k++)
        {
            Vector3[] ring = new Vector3[121];
            for (int i = 0; i <= 120; i++) ring[i] = o + RaceDir(i / 4f) * (rim * k / 7f);
            ExpLine(g, $"ring 10^{k}", k == 7 ? .12f : .07f, new Color(.5f, .55f, .75f) * (k == 7 ? .7f : .45f), Factory.NoGlow, ring);
            ExpLabelWide(g, $"10<sup>{k}</sup>", o + RaceDir(4f) * (rim * k / 7f) + new Vector3(1.2f, 1.6f, 0f), labelSize * .13f, axisCol, TextAlignmentOptions.Left);
        }

        List<(Vector3, Vector3)> spokes = new();
        foreach (int h in RaceData.Hours) spokes.Add((o, o + RaceDir(h) * rim * 1.02f));
        Segments(g, "spokes", spokes, .1f, PrimeGold * .35f, Factory.NoGlow);
        ExpEnsureSieve(3200);
        List<Vector3> dots = new();
        List<Color> cols = new();
        List<float> sizes = new();
        Dictionary<int, List<(Vector3, Vector3)>> arcs = new();
        Dictionary<int, int> landed = new();
        for (int p = 7; (long)p * p <= 10000000L; p++)
        {
            if (expNotPrime[p]) continue;
            int h = p % 30, h2 = (int)((long)p * p % 30);
            Color col = ZsHourColor(h);
            Vector3 a = o + RaceDir(h) * R(p), b = o + RaceDir(h2) * R((double)p * p);
            dots.Add(a); cols.Add(col); sizes.Add(.75f);
            dots.Add(b + Vector3.back * .2f); cols.Add(col); sizes.Add(1.1f);
            landed[h2] = landed.TryGetValue(h2, out int c) ? c + 1 : 1;
            // the arc turns the short way round from p's hour to the square's while the radius doubles
            float a0 = 90f - 12f * h, da = Mathf.DeltaAngle(a0, 90f - 12f * h2), r0 = R(p);
            if (!arcs.TryGetValue(h, out var list)) arcs[h] = list = new List<(Vector3, Vector3)>();
            const int steps = 16;
            Vector3 prev = a;
            for (int s = 1; s <= steps; s++)
            {
                float t = s / (float)steps, ang = (a0 + da * t) * Mathf.Deg2Rad;
                Vector3 q = o + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * (r0 * (1f + t)) + Vector3.back * (.1f + 3f * t * (1f - t));
                list.Add((prev, q));
                prev = q;
            }
        }

        foreach (var kv in arcs) Segments(g, $"arcs {kv.Key}", kv.Value, .06f, ZsHourColor(kv.Key) * .6f, Factory.SoftGlowIntensity);
        ExpPointCloud(g, "primes and squares", dots, cols, sizes, Factory.GlowIntensity);
        foreach (int h in RaceData.Hours)
        {
            Vector3 d = RaceDir(h);
            string text = landed.TryGetValue(h, out int c) ? $"{h}:  {c} squares" : h.ToString();
            TextAlignmentOptions al = !landed.ContainsKey(h) ? TextAlignmentOptions.Center : d.x < 0f ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
            Vector3 at = o + d * (rim + 6.5f);
            if (landed.ContainsKey(h)) at += new Vector3(d.x < 0f ? 2.6f : -2.6f, 0f, 0f);
            ExpLabelWide(g, text, at, labelSize * .2f, ZsHourColor(h), al).gameObject.name = $"hour label {h}";
        }
    }

    /// <summary>The fair race: every prime power p^k counted as 1/k, so the squares are back on spokes 1 and 19.</summary>
    public void ExpFairRace(Vector3 o, float w, float sy, float labelSize) =>
        RacePanel(ExpGroup("MB fair").transform, o, w, sy, RaceData.Fair, 1, "the squares put back, each p<sup>k</sup> counted as 1/k:  a fair race", true,
            labelSize, new[] { 0f });
}
