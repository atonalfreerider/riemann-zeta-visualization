using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Discovery five, step by step (the long-form segment xs5):
///   ExpHourSquares   on E8's clock of squares (MB fold): an arc from each gold hour to the hour of its square — every one
///                    lands on 1 or 19, the only square hours (group "MB squares", one item per hour)
///   ExpPowerWalk     the powers of p hopping round the same clock: 7, 49, 343, 2401 on hours 7, 19, 13, 1 (group "MB powers p")
///   ExpRootsModP     the numbers 0 … p − 1 as p dots on a circle (group "S3 mod p"); ExpRootsTest puts each into x³ − x − 1
///                    in turn and lights the roots: 5 has one (x = 2), 2 has none, 59 has three (4, 13, 42)
///   ExpS3Animate     the three roots of x³ − x − 1 moved by a panel's permutation: identity (nothing moves), rotation (all
///                    three move round), reflection (two swap), a reflection twice (swap and swap back)
/// </summary>
public partial class SacksSpiral
{
    static readonly int[] GoldHours30 = { 1, 7, 11, 13, 17, 19, 23, 29 };

    static Vector3 FoldDir(float hour) => RaceDir(hour);

    /// <summary>On the fold clock: an arc outside the rim from each gold hour to the hour of its square, one after another.</summary>
    public void ExpHourSquares(float seconds, float labelSize) => StartCoroutine(HourSquaresRoutine(seconds, labelSize));

    IEnumerator HourSquaresRoutine(float seconds, float labelSize)
    {
        Transform g = ExpGroup("MB squares").transform;
        Vector3 o = foldOrigin;
        const float rim = 22f;
        // a clean clock of 30 hours, the eight gold ones named
        Vector3[] ring = new Vector3[121];
        for (int i = 0; i <= 120; i++) ring[i] = o + RaceDir(i / 4f) * rim;
        ExpLine(g, "clock", .2f, new Color(.6f, .64f, .85f), Factory.SoftGlowIntensity, ring);
        List<(Vector3, Vector3)> ticks = new();
        for (int h = 0; h < 30; h++) ticks.Add((o + RaceDir(h) * rim * .95f, o + RaceDir(h) * rim * 1.05f));
        Segments(g, "ticks", ticks, .14f, new Color(.6f, .64f, .85f), Factory.NoGlow);
        foreach (int h in GoldHours30)
        {
            ExpDot(g, $"hour {h}", o + RaceDir(h) * rim + Vector3.back * .3f, 1.3f, ZsHourColor(h), Factory.GlowIntensity);
            TextBox l = ExpLabelWide(g, h.ToString(), o + RaceDir(h) * (rim + 4.2f) + Vector3.back * .3f, labelSize * .17f, ZsHourColor(h));
            Outline(l.TextField);
        }

        float per = seconds / GoldHours30.Length;
        foreach (int h in GoldHours30)
        {
            int h2 = h * h % 30;
            Color col = ZsHourColor(h);
            float a0 = 90f - 12f * h, da = Mathf.DeltaAngle(a0, 90f - 12f * h2);
            Vector3[] pts = new Vector3[33];
            for (int s = 0; s <= 32; s++)
            {
                // inside the clock, from hour h to the hour of h²: every arc ends on 1 or 19 (1 to itself: a small loop)
                float t = s / 32f, ang = (a0 + (h2 == h ? 360f * t : da * t)) * Mathf.Deg2Rad;
                float r = rim * (1f - (h2 == h ? .22f : .5f) * Mathf.Sin(Mathf.PI * t));
                pts[s] = o + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * r + Vector3.back * .5f;
            }

            LineRenderer arc = ExpLine(g, $"arc {h}", .45f, Color.white, AttentionIntensity, new[] { pts[0], pts[0] });
            OnTop(arc, 20);
            float time = 0f;
            while (time < per * .7f)
            {
                if (arc == null) yield break;
                int n = Mathf.Clamp(Mathf.CeilToInt(33 * time / (per * .7f)), 2, 33);
                arc.positionCount = n;
                for (int i = 0; i < n; i++) arc.SetPosition(i, pts[i]);
                time += Time.deltaTime;
                yield return null;
            }

            if (arc == null) yield break;
            arc.positionCount = 33;
            arc.SetPositions(pts);
            StartCoroutine(SettleRoutine(arc, col, .8f));
            Flare(pts[32], 4f, .9f, 1);
            TextBox tag = ExpLabelWide(g, $"{h}² = {h * h}", o + FoldDir(h) * (rim + 10f) + Vector3.back * .6f, labelSize * .14f, col);
            Outline(tag.TextField);
            yield return new WaitForSeconds(per * .3f);
        }
    }

    /// <summary>The powers p, p², … p^maxPow on the fold clock (hour n mod 30, radius ∝ log n), joined by arcs, labelled.</summary>
    public void ExpPowerWalk(long p, int maxPow, float seconds, float labelSize) => StartCoroutine(PowerWalkRoutine(p, maxPow, seconds, labelSize));

    IEnumerator PowerWalkRoutine(long p, int maxPow, float seconds, float labelSize)
    {
        Transform g = ExpGroup($"MB powers {p}").transform;
        float per = seconds / maxPow;
        long n = p;
        string[] sup = { "", "", "²", "³", "<sup>4</sup>", "<sup>5</sup>", "<sup>6</sup>" };
        Vector3 Pos(long m) => FoldPosition(m) + Vector3.back * .4f;
        ExpDot(g, $"power 1", Pos(p), 1.6f, Color.white, AttentionIntensity);
        TextBox t1 = ExpLabelWide(g, $"{p}:  hour {p % 30}", Pos(p) + new Vector3(0f, 4.5f, -.3f), labelSize * .17f, Color.white);
        Outline(t1.TextField);
        Flare(Pos(p), 4f, 1f, 2);
        for (int k = 2; k <= maxPow; k++)
        {
            long m = n * p;
            int h0 = (int)(n % 30), h1 = (int)(m % 30);
            float a0 = 90f - 12f * h0, da = Mathf.DeltaAngle(a0, 90f - 12f * h1);
            float r0 = (Pos(n) - foldOrigin).magnitude, r1 = (Pos(m) - foldOrigin).magnitude;
            Vector3[] pts = new Vector3[25];
            for (int s = 0; s <= 24; s++)
            {
                float t = s / 24f, ang = (a0 + da * t) * Mathf.Deg2Rad;
                pts[s] = foldOrigin + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * Mathf.Lerp(r0, r1, t) + Vector3.back * (.5f + 3f * t * (1f - t));
            }

            LineRenderer arc = ExpLine(g, $"hop {k}", .4f, Color.white, AttentionIntensity, new[] { pts[0], pts[0] });
            OnTop(arc, 20);
            float time = 0f;
            while (time < per * .6f)
            {
                if (arc == null) yield break;
                int c = Mathf.Clamp(Mathf.CeilToInt(25 * time / (per * .6f)), 2, 25);
                arc.positionCount = c;
                for (int i = 0; i < c; i++) arc.SetPosition(i, pts[i]);
                time += Time.deltaTime;
                yield return null;
            }

            if (arc == null) yield break;
            arc.positionCount = 25;
            arc.SetPositions(pts);
            bool square = h1 == 1 || h1 == 19;
            Color col = square && k % 2 == 0 ? PrimeGold : new Color(.8f, .85f, 1f);
            StartCoroutine(SettleRoutine(arc, col, .8f));
            ExpDot(g, $"power {k}", Pos(m), 1.6f, col, AttentionIntensity);
            Flare(Pos(m), 4.5f, 1f, 2);
            TextBox tag = ExpLabelWide(g, $"{p}{sup[k]} = {m:N0}:  hour {h1}", Pos(m) + new Vector3(0f, 4.5f, -.3f), labelSize * .17f, col);
            Outline(tag.TextField);
            n = m;
            yield return new WaitForSeconds(per * .4f);
        }
    }

    // ---- the roots of x³ − x − 1 among the numbers mod p ------------------------------------------------------------

    static long CubicModP(long x, long p) => (((x * x % p) * x - x - 1) % p + p) % p;

    /// <summary>p dots on a circle at o, numbered (when p ≤ 13), hidden until ExpRootsTest shows them (group "S3 mod p").</summary>
    public void ExpRootsModP(Vector3 o, int p, float radius, float labelSize)
    {
        Transform g = ExpGroup($"S3 mod {p}").transform;
        s3ModCentre[p] = o;
        float dot = Mathf.Clamp(radius * 2.4f / Mathf.Max(3, p), .55f, 2.2f);
        for (int x = 0; x < p; x++)
        {
            float a = (90f - 360f * x / p) * Mathf.Deg2Rad;
            Vector3 at = o + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
            GameObject d = ExpDot(g, $"x {x}", at, dot, new Color(.62f, .66f, .85f), Factory.SoftGlowIntensity);
            if (p <= 13 || CubicModP(x, p) == 0)
            {
                TextBox l = ExpLabelWide(g, x.ToString(), o + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * (radius + Mathf.Max(3.5f, labelSize * .022f)),
                    labelSize * .17f, new Color(.85f, .88f, 1f));
                Outline(l.TextField);
                l.gameObject.name = $"label {x}";
                if (p > 13) l.gameObject.SetActive(false);
            }
        }

        TextBox title = ExpLabelWide(g, $"the numbers mod {p}", o + Vector3.up * (radius + Mathf.Max(9f, labelSize * .05f)), labelSize * .18f, Color.white);
        Outline(title.TextField);
        title.gameObject.name = "title";
        g.gameObject.SetActive(false);
    }

    /// <summary>Show the dots, then put each x into x³ − x − 1 in turn: its value mod p appears, and the roots turn gold.</summary>
    public void ExpRootsTest(int p, float seconds, float labelSize) => StartCoroutine(RootsTestRoutine(p, seconds, labelSize));

    IEnumerator RootsTestRoutine(int p, float seconds, float labelSize)
    {
        Transform g = ExpRoot().Find($"S3 mod {p}");
        if (g == null) yield break;
        g.gameObject.SetActive(true);
        float per = seconds * .9f / p;
        int roots = 0;
        for (int x = 0; x < p; x++)
        {
            Transform d = g.Find($"x {x}");
            if (d == null) continue;
            long v = CubicModP(x, p);
            bool root = v == 0;
            Flare(d.position + Vector3.back * .3f, d.localScale.x * 2.2f, .6f, 1);
            if (p <= 13)
            {
                long raw = (long)x * x * x - x - 1;
                Vector3 centre = s3ModCentre.TryGetValue(p, out Vector3 cc) ? cc : d.position;
                Vector3 inside = Vector3.Lerp(d.position, centre, .36f);
                TextBox val = ExpLabelWide(g, root ? $"{raw} ≡ 0" : $"{raw} ≡ {v}".Replace("-", "−"), inside + Vector3.back * .3f, labelSize * .14f,
                    root ? PrimeGold : new Color(.75f, .78f, .9f));
                Outline(val.TextField);
            }

            if (root)
            {
                roots++;
                Destroy(d.gameObject);
                GameObject gd = ExpDot(g, $"root {x}", d.position + Vector3.back * .2f, d.localScale.x * 1.5f, PrimeGold, AttentionIntensity);
                RingAround(g, $"ring {x}", d.position + Vector3.back * .3f, Vector3.back, d.localScale.x * 1.6f, PrimeGold, d.localScale.x * .2f, AttentionIntensity);
                Flare(d.position + Vector3.back * .4f, d.localScale.x * 4f, 1.2f, 2);
                Transform lab = g.Find($"label {x}");
                if (lab != null)
                {
                    lab.gameObject.SetActive(true);
                    lab.GetComponent<TextBox>().TextField.color = PrimeGold;
                }
            }

            yield return new WaitForSeconds(per);
        }

        Transform t = g.Find("title");
        if (t != null)
        {
            TextBox tb = t.GetComponent<TextBox>();
            string kind = roots == 3 ? "three roots:  the identity" : roots == 1 ? "one root:  a reflection" : "no root:  a rotation";
            tb.TextField.text = $"mod {p}:  {kind}";
            tb.TextField.color = roots == 1 ? new Color(.85f, .87f, .95f) : roots == 0 ? new Color(.35f, .85f, 1f) : new Color(1f, .8f, .32f);
        }
    }

    // ---- the permutations, moving ------------------------------------------------------------------------------------

    /// <summary>Move the three roots of panel <paramref name="name"/> ("S3 identity", "S3 rotation", "S3 reflection", "S3 square").</summary>
    public void ExpS3Animate(string name, float seconds) => StartCoroutine(S3AnimateRoutine(name, seconds));

    IEnumerator S3AnimateRoutine(string name, float seconds)
    {
        Transform g = ExpRoot().Find(name);
        if (g == null) yield break;
        // the panel's centre: S3Triangle draws the roots around c with scale s3Scale
        Vector3 c = s3Centre.TryGetValue(name, out Vector3 v) ? v : g.position;
        float scale = s3Scale;
        Vector3 P(int i) => c + new Vector3(S3Roots[i].x, S3Roots[i].y, 0f) * scale + Vector3.back * .8f;
        Color[] cols = { new(1f, .8f, .32f), new(.35f, .85f, 1f), new(1f, .45f, .8f) };
        GameObject[] dots = new GameObject[3];
        for (int i = 0; i < 3; i++) dots[i] = ExpDot(g, $"moving r{i + 1}", P(i), scale * .2f, cols[i], AttentionIntensity);
        // where each root goes, one step at a time (the square: swap, then swap back)
        int[][] steps = name switch
        {
            "S3 rotation" => new[] { new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 0, 1, 2 } },
            "S3 reflection" => new[] { new[] { 0, 2, 1 }, new[] { 0, 1, 2 } },
            "S3 square" => new[] { new[] { 0, 2, 1 }, new[] { 0, 1, 2 } },
            _ => new[] { new[] { 0, 1, 2 }, new[] { 0, 1, 2 } },
        };
        int[] at = { 0, 1, 2 };
        float per = seconds / steps.Length;
        foreach (int[] to in steps)
        {
            Vector3[] a = new Vector3[3], b = new Vector3[3];
            for (int i = 0; i < 3; i++) { a[i] = P(at[i]); b[i] = P(to[i]); }
            float time = 0f;
            while (time < per * .7f)
            {
                float u = Mathf.SmoothStep(0f, 1f, time / (per * .7f));
                for (int i = 0; i < 3; i++)
                {
                    if (dots[i] == null) yield break;
                    Vector3 mid = (a[i] + b[i]) * .5f, outward = (mid - c).normalized;
                    if (outward.sqrMagnitude < 1e-6f) outward = Vector3.up;
                    Vector3 ctrl = mid + outward * scale * .45f;
                    bool still = (a[i] - b[i]).sqrMagnitude < 1e-4f;
                    dots[i].transform.position = still ? a[i] + Vector3.back * (Mathf.Sin(u * Mathf.PI) * .8f)
                        : (1 - u) * (1 - u) * a[i] + 2 * (1 - u) * u * ctrl + u * u * b[i];
                    dots[i].transform.localScale = Vector3.one * scale * .2f * (still ? 1f + .4f * Mathf.Sin(u * Mathf.PI) : 1f);
                }

                time += Time.deltaTime;
                yield return null;
            }

            for (int i = 0; i < 3; i++)
            {
                if (dots[i] != null) dots[i].transform.position = b[i];
                at[i] = to[i];
            }

            yield return new WaitForSeconds(per * .3f);
        }
    }

    readonly Dictionary<string, Vector3> s3Centre = new();
    readonly Dictionary<int, Vector3> s3ModCentre = new();
    float s3Scale = 18f;
}
