using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The race between the families of −23 (docs/experiments/README.md, section MC). A prime p ≠ 23 acts on the three roots of
/// x³ − x − 1 as an element of S3: the identity (three roots mod p: the principal family x² + xy + 6y²), a rotation (no
/// root: the second family 2x² ± xy + 3y²) or a reflection (one root: neither). Squaring sends every reflection to the
/// identity, so the squares of half of all primes land on the principal family, and it runs three units behind, as E8's
/// spokes 1 and 19 do (section MB). Checked in Python first (Tools/family_race_data.py writes FamilyRaceData.cs).
/// </summary>
public partial class SacksSpiral
{
    public const float FamilyRaceOffset = 84000f;
    static readonly Color[] FrobColors = { new(1f, .8f, .32f), new(.35f, .85f, 1f), new(.78f, .8f, .9f) };

    /// <summary>The sector of each class on the clock of S3, in degrees counterclockwise from +x: the identity on top (60°,
    /// a sixth), the rotations on the right (120°, a third), the reflections on the left and below (180°, a half).</summary>
    static readonly (float from, float to)[] FrobSectors = { (60f, 120f), (-60f, 60f), (120f, 300f) };

    /// <summary>One panel of the family race: three curves over log₁₀ x from 2 to 7, dashed means, ticks, a title.</summary>
    void FamilyPanel(Transform g, Vector3 po, float w, float sy, float lo, float hi, float[] data, string title, bool xLabels,
        float labelSize, float[] means)
    {
        Color axisCol = new(.7f, .74f, .88f);
        int n = FamilyRaceData.Count;
        Vector3 P(float lg, float e) => po + new Vector3((lg - 4.5f) / 5f * w, (e - (lo + hi) * .5f) * sy, 0f);
        List<(Vector3, Vector3)> ax = new() { (P(2f, lo), P(7f, lo)), (P(2f, lo), P(2f, hi)) };
        for (int d = 2; d <= 7; d++)
        {
            ax.Add((P(d, lo), P(d, lo - .35f)));
            if (xLabels) ExpLabelWide(g, $"10<sup>{d}</sup>", P(d, lo - 1.5f), labelSize * .15f, axisCol);
        }

        for (int e = Mathf.CeilToInt(lo / 3f) * 3; e <= hi; e += 3)
        {
            ax.Add((P(1.97f, e), P(2f, e)));
            ExpLabelWide(g, e.ToString().Replace("-", "−"), P(1.95f, e), labelSize * .14f, axisCol, TextAlignmentOptions.Right);
        }

        Segments(g, "axes", ax, .12f, new Color(.55f, .6f, .78f) * .7f, Factory.NoGlow);
        for (int k = 0; k < means.Length; k++)
        {
            List<(Vector3, Vector3)> dash = new();
            for (float lg = 2f; lg < 7f; lg += .08f) dash.Add((P(lg, means[k]), P(Mathf.Min(lg + .045f, 7f), means[k])));
            Segments(g, $"mean {k}", dash, .14f, (means.Length == 3 ? FrobColors[k] : Color.white) * .75f, Factory.NoGlow);
        }

        ExpLabelWide(g, title, P(2.06f, hi + .35f), labelSize * .19f, Color.white, TextAlignmentOptions.Left);
        foreach (int k in new[] { 2, 1, 0 })
        {
            Vector3[] pts = new Vector3[n];
            for (int i = 0; i < n; i++)
                pts[i] = P(FamilyRaceData.Log0 + FamilyRaceData.LogStep * i, Mathf.Clamp(data[k * n + i], lo, hi)) + Vector3.back * (.2f * (3 - k));
            ExpLine(g, $"class {k}", k == 0 ? .5f : .32f, FrobColors[k], k == 0 ? Factory.GlowIntensity : Factory.SoftGlowIntensity, pts);
        }
    }

    /// <summary>
    /// The fold of S3 on a logarithmic clock: each prime p ≤ √10⁷ in the sector of its class at radius ∝ log p, and an arc to
    /// p² (radius doubled) in the sector of the square of its class: reflections and the identity square to the identity,
    /// rotations to rotations.
    /// </summary>
    public void ExpFamilyFold(Vector3 o, float rim, float labelSize)
    {
        Transform g = ExpGroup("MC fold").transform;
        const double logMax = 16.11809565095832; // ln 10⁷
        Color axisCol = new(.7f, .74f, .88f);
        float R(double n) => rim * (float)(Math.Log(n) / logMax);
        Vector3 Polar(float deg, float r) => o + new Vector3(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad), 0f) * r;
        for (int k = 1; k <= 7; k++)
        {
            Vector3[] ring = new Vector3[121];
            for (int i = 0; i <= 120; i++) ring[i] = Polar(i * 3f, rim * k / 7f);
            ExpLine(g, $"ring 10^{k}", k == 7 ? .12f : .07f, new Color(.5f, .55f, .75f) * (k == 7 ? .7f : .45f), Factory.NoGlow, ring);
        }

        for (int k = 2; k <= 6; k += 2)
            ExpLabelWide(g, $"10<sup>{k}</sup>", Polar(-75f, rim * k / 7f) + new Vector3(.8f, 0f, 0f), labelSize * .13f, axisCol, TextAlignmentOptions.Left);
        List<(Vector3, Vector3)> walls = new();
        foreach (float a in new[] { 60f, 120f, 300f }) walls.Add((o, Polar(a, rim * 1.04f)));
        Segments(g, "sector walls", walls, .16f, new Color(.6f, .64f, .82f) * .8f, Factory.NoGlow);
        const float phi = .6180339887f;
        float Angle(int cls, long n)
        {
            var (from, to) = FrobSectors[cls];
            float u = (float)((n * (double)phi) % 1d);
            return from + (to - from) * (.06f + .88f * u);
        }

        List<Vector3> dots = new();
        List<Color> cols = new();
        List<float> sizes = new();
        Dictionary<int, List<(Vector3, Vector3)>> arcs = new();
        int[] landed = new int[3];
        for (int i = 0; i < FamilyRaceData.SmallPrimes.Length; i++)
        {
            int p = FamilyRaceData.SmallPrimes[i], cls = FamilyRaceData.SmallClasses[i];
            int sqCls = cls == 1 ? 1 : 0;
            landed[sqCls]++;
            float a0 = Angle(cls, p), a1 = Angle(sqCls, (long)p * p), r0 = R(p), da = Mathf.DeltaAngle(a0, a1);
            dots.Add(Polar(a0, r0)); cols.Add(FrobColors[cls]); sizes.Add(.75f);
            dots.Add(Polar(a1, 2f * r0) + Vector3.back * .2f); cols.Add(FrobColors[cls]); sizes.Add(1.05f);
            if (!arcs.TryGetValue(cls, out var list)) arcs[cls] = list = new List<(Vector3, Vector3)>();
            const int steps = 16;
            Vector3 prev = Polar(a0, r0);
            for (int s = 1; s <= steps; s++)
            {
                float t = s / (float)steps;
                Vector3 q = Polar(a0 + da * t, r0 * (1f + t)) + Vector3.back * (.1f + 3f * t * (1f - t));
                list.Add((prev, q));
                prev = q;
            }
        }

        foreach (var kv in arcs) Segments(g, $"arcs {kv.Key}", kv.Value, .06f, FrobColors[kv.Key] * .55f, Factory.SoftGlowIntensity);
        ExpPointCloud(g, "primes and squares", dots, cols, sizes, Factory.GlowIntensity);
        string[] names = { "identity:  x² + xy + 6y²", "rotations:  2x² ± xy + 3y²", "reflections:  neither family" };
        string[] subs = { "x³ − x − 1 has 3 roots mod p:  1/6 of the primes", "no root:  1/3", "1 root:  1/2" };
        Vector3[] at = { Polar(90f, rim + 19f), Polar(-30f, rim + 5f), Polar(250f, rim + 7f) };
        TextAlignmentOptions[] al = { TextAlignmentOptions.Center, TextAlignmentOptions.Left, TextAlignmentOptions.Center };
        for (int k = 0; k < 3; k++)
        {
            ExpLabelWide(g, names[k], at[k], labelSize * .19f, FrobColors[k], al[k]);
            ExpLabelWide(g, subs[k], at[k] + Vector3.down * 4.6f, labelSize * .14f, new Color(.8f, .83f, .93f), al[k]);
        }

        ExpLabelWide(g, $"{landed[0]} of the {landed[0] + landed[1]} prime squares land here", at[0] + Vector3.down * 9.2f, labelSize * .14f, FrobColors[0]);
    }

    /// <summary>MC1: the fold on the left, the race and the race with the squares put back on the right.</summary>
    public void ExpFamilyRace(Vector3 o, float labelSize)
    {
        ExpFamilyFold(o + new Vector3(-95f, -8f, 0f), 46f, labelSize);
        FamilyPanel(ExpGroup("MC race").transform, o + new Vector3(86f, 26f, 0f), 120f, 2.7f, -10f, 4f, FamilyRaceData.Race,
            "the race:  the principal family is never ahead", false, labelSize, new[] { -3f, 0f, 1f });
        FamilyPanel(ExpGroup("MC fair").transform, o + new Vector3(86f, -38f, 0f), 120f, 2.7f, -10f, 4f, FamilyRaceData.Fair,
            "each p<sup>k</sup> counted as 1/k:  the gap mostly closes", true, labelSize, new[] { 0f });
    }
}
