using System.Collections;
using System.Collections.Generic;
using Meta.Numerics;
using TMPro;
using UnityEngine;

/// <summary>
/// The bridge from a zero to the counting numbers as a row of clocks (the long-form zeros segments). The columns of
/// n = 1 … nMax from E8's factor funnel stand side by side, each as tall as log n (its height in the funnel), and each
/// carries a clock whose hand has turned by γ·log n: the zero's height times the column's height. That turn is the
/// direction of link n of the chain Σ n^(−½ − iγ). The live version sweeps each hand round from the start (pointing
/// right, the direction of link 1), then carries it, shrunk by 1/√n, to the chain above, where it becomes link n; at the
/// end the chain's tail (ZetaMath's Euler–Maclaurin correction) closes it at ζ(½ + iγ), the centre at a zero.
/// Groups: "BC n" (column, clock face and labels of n), "BC chain axes"; the live parts under the live root.
/// </summary>
public partial class SacksSpiral
{
    public const float BridgeClocksOffset = ZeroBridgeOffset + 400f;
    const float ClockGap = 14f, ClockR = 4.6f, ClockH = 10f, ClockBase = -26f, ClockChain = 30f;
    int clocksN = 8;
    double clocksGamma = 14.134725;

    Vector3 ClockOrigin => Center + new Vector3(BridgeClocksOffset, 0f, 0f);
    Vector3 ClockColumnBase(int n) => ClockOrigin + new Vector3((n - (clocksN + 1) * .5f) * ClockGap, ClockBase, 0f);
    Vector3 ClockCentre(int n) => ClockColumnBase(n) + Vector3.up * (ClockH * Mathf.Log(n) + ClockR + 1.2f);
    Vector3 ClockChainOrigin => ClockOrigin + new Vector3(-12f, 22f, 0f);

    /// <summary>The centre of n's clock (for callouts, exp:callclock).</summary>
    public Vector3 ClockPosition(int n) => ClockCentre(Mathf.Clamp(n, 1, clocksN)) + Vector3.back * .3f;

    /// <summary>Where the clocks' chain ends for the zero (or height) γ, with its tail: ζ(½ + iγ) (exp:callclockend).</summary>
    public Vector3 ClockChainEnd(double gamma)
    {
        ZetaMath.SpiralDecomposition d = ZetaMath.Decompose(new Complex(.5, gamma), clocksN + 1);
        return ClockChainOrigin + new Vector3((float)d.Total.Re, (float)d.Total.Im, 0f) * ClockChain + Vector3.back * .3f;
    }

    /// <summary>The middle of link n of the clocks' chain (exp:callclocklink).</summary>
    public Vector3 ClockLinkMid(int n)
    {
        Vector3[] pts = BridgeClockChain(clocksGamma, Mathf.Max(1, n));
        return (pts[n - 1] + pts[n]) * .5f + Vector3.back * .3f;
    }

    Vector3[] BridgeClockChain(double gamma, int n)
    {
        Vector3[] pts = new Vector3[n + 1];
        pts[0] = ClockChainOrigin;
        for (int k = 1; k <= n; k++) pts[k] = pts[k - 1] + Dir2(-gamma * System.Math.Log(k)) * (ClockChain / Mathf.Sqrt(k));
        return pts;
    }

    /// <summary>
    /// The row: for n = 1 … nMax a column of height log n (cut into its prime factors' logs, like the funnel's), a clock
    /// face on top with a tick where every hand starts, n under the column and its log under that. The chain's axes above.
    /// </summary>
    public void ExpBridgeClocks(int nMax, float labelSize)
    {
        clocksN = nMax;
        Color face = new Color(.6f, .65f, .85f) * .8f, text = new(.78f, .82f, .94f);
        for (int n = 1; n <= nMax; n++)
        {
            Transform g = ExpGroup($"BC {n}").transform;
            Vector3 b = ClockColumnBase(n), c = ClockCentre(n);
            // the column, one segment per prime factor (smallest at the top, as in the funnel), a hair of gap between
            List<long> fs = new(Factorize(n));
            fs.Sort();
            fs.Reverse();
            float h = 0f;
            foreach (long p in fs)
            {
                float lp = ClockH * Mathf.Log(p);
                ExpLine(g, $"column {p}", 2.2f, FunnelPrimeColour(p), Factory.SoftGlowIntensity,
                    new[] { b + Vector3.up * (h + .25f), b + Vector3.up * (h + lp - .25f) });
                h += lp;
            }

            Vector3[] ring = new Vector3[49];
            for (int k = 0; k <= 48; k++) ring[k] = c + Dir2(k * System.Math.PI / 24) * ClockR;
            ExpLine(g, "face", .18f, face, Factory.SoftGlowIntensity, ring);
            ExpLine(g, "start", .18f, face, Factory.SoftGlowIntensity, new[] { c + Vector3.right * ClockR * .8f, c + Vector3.right * ClockR * 1.15f });
            ExpDot(g, "pin", c + Vector3.back * .2f, .7f, Color.white, Factory.GlowIntensity);
            TextBox num = ExpLabelWide(g, n.ToString(), b + Vector3.down * 3.4f, labelSize * .34f, Color.white);
            Outline(num.TextField);
            TextBox log = ExpLabelWide(g, n == 1 ? "0" : $"{System.Math.Log(n):0.000}", b + Vector3.down * 6.9f, labelSize * .22f, text);
            Outline(log.TextField);
        }

        Transform lg = ExpGroup("BC legend").transform;
        Vector3 left = ClockColumnBase(1) + Vector3.left * (ClockGap * .75f);
        TextBox l1 = ExpLabelWide(lg, "n", left + Vector3.down * 3.4f, labelSize * .3f, Color.white, TextAlignmentOptions.Right);
        TextBox l2 = ExpLabelWide(lg, "log n", left + Vector3.down * 6.9f, labelSize * .22f, text, TextAlignmentOptions.Right);
        TextBox l3 = ExpLabelWide(lg, "turns:", ClockCentre(1) + new Vector3(-ClockGap * .75f, ClockR + 2f, 0f), labelSize * .22f, text, TextAlignmentOptions.Right);
        Outline(l1.TextField);
        Outline(l2.TextField);
        Outline(l3.TextField);
        Transform a = ExpGroup("BC chain axes").transform;
        Vector3 co = ClockChainOrigin;
        Color axis = new Color(.55f, .6f, .78f) * .6f;
        ExpLine(a, "re", .12f, axis, Factory.NoGlow, new[] { co + Vector3.left * 12f, co + Vector3.right * 30f });
        ExpLine(a, "im", .12f, axis, Factory.NoGlow, new[] { co + Vector3.down * 9f, co + Vector3.up * 11f });
        TextBox zero = ExpLabelWide(a, "0", co + new Vector3(-1.2f, -1.6f, 0f), labelSize * .22f, text);
        Outline(zero.TextField);
    }

    static Color FunnelPrimeColour(long p) => p switch
    {
        2 => new Color(.35f, .75f, 1f),
        3 => new Color(.45f, .95f, .55f),
        5 => new Color(1f, .62f, .3f),
        7 => new Color(.95f, .45f, .85f),
        _ => new Color(1f, .85f, .4f),
    };

    /// <summary>
    /// The hands of the zero γ on every clock at once, the chain they make, and its tail (group "BC hands γ"), for a still
    /// or for comparing two zeros.
    /// </summary>
    public void ExpBridgeClockHands(double gamma, Color col, float labelSize, bool chain)
    {
        clocksGamma = gamma;
        Transform g = ExpGroup($"BC hands {gamma:0.00}").transform;
        for (int n = 1; n <= clocksN; n++)
        {
            Vector3 c = ClockCentre(n);
            double a = -gamma * System.Math.Log(n);
            ExpLine(g, $"hand {n}", .5f, col, Factory.GlowIntensity, new[] { c + Vector3.back * .1f, c + Dir2(a) * ClockR * .92f + Vector3.back * .1f });
            TextBox turns = ExpLabelWide(g, $"{gamma * System.Math.Log(n) / (2 * System.Math.PI):0.00}", c + Vector3.up * (ClockR + 2f), labelSize * .26f, col);
            Outline(turns.TextField);
        }

        if (!chain) return;
        Vector3[] pts = BridgeClockChain(gamma, clocksN);
        ExpLine(g, "chain", .34f, col, Factory.SoftGlowIntensity, pts);
        BridgeClockTail(g, gamma, clocksN);
    }

    void BridgeClockTail(Transform g, double gamma, int n)
    {
        ZetaMath.SpiralDecomposition d = ZetaMath.Decompose(new Complex(.5, gamma), n + 1);
        Vector3 P(Complex z) => ClockChainOrigin + new Vector3((float)z.Re, (float)z.Im, 0f) * ClockChain + Vector3.back * .1f;
        ExpLine(g, "tail", .4f, new Color(1f, .55f, .2f), Factory.GlowIntensity, new[] { P(d.SpiralEnd), P(d.Total) });
        ExpDot(g, "zeta", P(d.Total) + Vector3.back * .2f, 1.4f, Factory.Palette.Zero, AttentionIntensity);
        bool zero = d.Total.Re * d.Total.Re + d.Total.Im * d.Total.Im < 1e-4;
        Flare(P(d.Total) + Vector3.back * .4f, zero ? 4f : 2.4f, 1.4f, zero ? 2 : 1);
    }

    /// <summary>
    /// The live bridge: for n = from … to the clock of n lights, its hand sweeps from the start round by γ·log n turns (a
    /// counter beside it), then a copy shrinks by 1/√n, flies to the end of the chain and becomes link n. After the last,
    /// the tail closes the chain.
    /// </summary>
    public void ExpBridgeClocksLive(double gamma, int from, int to, float seconds, float labelSize, bool tail)
    {
        StartLive(BridgeClocksLive(gamma, from, to, seconds, labelSize, tail));
    }

    IEnumerator BridgeClocksLive(double gamma, int from, int to, float seconds, float labelSize, bool tail)
    {
        clocksGamma = gamma;
        Transform g = LiveRoot();
        Vector3[] chain = BridgeClockChain(gamma, to);
        Color gold = new(1f, .85f, .3f), white = new(.95f, .97f, 1f);
        if (from > 1)
        {
            Vector3[] done = new Vector3[from];
            System.Array.Copy(chain, done, from);
            ExpLine(g, "chain so far", .34f, white, Factory.SoftGlowIntensity, done);
            for (int n = 1; n < from; n++)
            {
                Vector3 c = ClockCentre(n);
                ExpLine(g, $"hand {n}", .5f, gold, Factory.GlowIntensity, new[] { c + Vector3.back * .1f, c + Dir2(-gamma * System.Math.Log(n)) * ClockR * .92f + Vector3.back * .1f });
            }
        }

        float per = seconds * (tail ? .9f : 1f) / Mathf.Max(1, to - from + 1);
        for (int n = from; n <= to; n++)
        {
            Vector3 c = ClockCentre(n);
            double full = -gamma * System.Math.Log(n);
            LineRenderer hand = ExpLine(g, $"hand {n}", .6f, Color.white, AttentionIntensity * 1.2f, new[] { c + Vector3.back * .1f, c + Vector3.right * ClockR * .92f + Vector3.back * .1f });
            Vector3[] halo = new Vector3[49];
            for (int k = 0; k <= 48; k++) halo[k] = c + Dir2(k * System.Math.PI / 24) * (ClockR * 1.12f) + Vector3.back * .1f;
            LineRenderer ring = ExpLine(g, $"lit {n}", .3f, Color.white, AttentionIntensity, halo);
            TextBox count = ExpLabelWide(g, "", c + Vector3.up * (ClockR + 2f), labelSize * .26f, Color.white);
            Outline(count.TextField);
            LineRenderer fly = null;
            float time = 0f;
            while (time < per)
            {
                if (hand == null) yield break; // the stage was cleared under us
                float u = time / per;
                // sweep (the first half), hold, then fly and shrink into place
                float sw = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / .5f));
                Vector3 tip = c + Dir2(full * sw) * ClockR * .92f;
                hand.SetPositions(new[] { c + Vector3.back * .1f, tip + Vector3.back * .1f });
                count.TextField.text = $"{gamma * System.Math.Log(n) * sw / (2 * System.Math.PI):0.00}";
                float m = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - .62f) / .33f));
                if (u > .62f)
                {
                    fly ??= ExpLine(g, $"link {n}", .5f, Color.white, AttentionIntensity, new[] { c, tip });
                    fly.SetPositions(new[] { Vector3.Lerp(c, chain[n - 1], m) + Vector3.back * .2f, Vector3.Lerp(tip, chain[n], m) + Vector3.back * .2f });
                    fly.widthMultiplier = Mathf.Lerp(.5f, .34f, m);
                }

                time += Time.deltaTime;
                yield return null;
            }

            if (hand == null) yield break;
            hand.SetPositions(new[] { c + Vector3.back * .1f, c + Dir2(full) * ClockR * .92f + Vector3.back * .1f });
            hand.startColor = hand.endColor = gold;
            Factory.SetIntensity(hand, Factory.GlowIntensity);
            hand.widthMultiplier = .5f;
            count.TextField.text = $"{gamma * System.Math.Log(n) / (2 * System.Math.PI):0.00}";
            count.TextField.color = gold;
            Destroy(ring.gameObject);
            fly ??= ExpLine(g, $"link {n}", .34f, white, Factory.SoftGlowIntensity, new[] { chain[n - 1], chain[n] });
            fly.SetPositions(new[] { chain[n - 1] + Vector3.back * .2f, chain[n] + Vector3.back * .2f });
            fly.startColor = fly.endColor = white;
            Factory.SetIntensity(fly, Factory.SoftGlowIntensity);
            fly.widthMultiplier = .34f;
        }

        if (tail) BridgeClockTail(g, gamma, to);
    }
}
