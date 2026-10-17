using System.Collections;
using System.Collections.Generic;
using Meta.Numerics;
using TMPro;
using UnityEngine;

/// <summary>
/// From a zero's height to the heights of the counting numbers (the long-form zeros segments). At a zero ½ + iγ the chain
/// Σ n^(−s) has links n^(−½) e^(−iγ log n): link n turns by γ·log n, and log n is the height of n's column in E8's funnel.
/// So a coil wound up the funnel's axis, turning −γ per unit of height, points at the height of every column exactly where
/// that number's link points. Left: the chain at γ, link by link, closed by its tail; right: a small funnel (n ≤ 60) with
/// the coil. The live bridge draws a level line from the top of n's column to the axis, a hand from the axis to the coil
/// at that height, and carries the hand to the chain as link n, shrunk by 1/√n.
/// </summary>
public partial class SacksSpiral
{
    public const float ZeroBridgeOffset = 92000f;
    const int BridgeN = 60;
    const float BridgeS = 4f, BridgeH = 8f, BridgeCoil = 7f, BridgeChain = 22f;

    /// <summary>The origin of the bridge's chain panel and of its funnel.</summary>
    public Vector3 BridgeChainOrigin => Center + new Vector3(ZeroBridgeOffset - 52f, 0f, 0f);
    public Vector3 BridgeFunnelOrigin => Center + new Vector3(ZeroBridgeOffset + 40f, 0f, 0f);

    static Vector3 Dir2(double angle) => new((float)System.Math.Cos(angle), (float)System.Math.Sin(angle), 0f);

    /// <summary>The partial sums of the chain at ½ + iγ: point k is Σ_{n ≤ k} n^(−s), scaled.</summary>
    static Vector3[] BridgeChainPoints(Vector3 o, double gamma, int n)
    {
        Vector3[] pts = new Vector3[n + 1];
        pts[0] = o;
        for (int k = 1; k <= n; k++)
            pts[k] = pts[k - 1] + Dir2(-gamma * System.Math.Log(k)) * (BridgeChain / Mathf.Sqrt(k));
        return pts;
    }

    // the zero the bridge was last drawn for (for callouts on its links)
    double bridgeGamma = 14.134725;

    /// <summary>The middle of link n of the chain at the zero last drawn (for callouts).</summary>
    public Vector3 BridgeLinkMid(int n) => BridgeLinkMid(bridgeGamma, n);

    /// <summary>The middle of link n of the chain at γ.</summary>
    public Vector3 BridgeLinkMid(double gamma, int n)
    {
        Vector3[] pts = BridgeChainPoints(BridgeChainOrigin, gamma, Mathf.Max(1, n));
        return (pts[n - 1] + pts[n]) * .5f;
    }

    Vector3 BridgeCoilPoint(double gamma, double height)
    {
        Vector3 f = BridgeFunnelOrigin;
        return f + Dir2(-gamma * height) * BridgeCoil + Vector3.back * (float)(BridgeH * height);
    }

    /// <summary>
    /// The static parts: the funnel of n ≤ 60 (columns cut by prime), the coil of the zero γ up its axis (group "ZB coil"),
    /// the chain's axes (group "ZB chain axes"). The chain itself is drawn by the live bridge or by exp:zbridgechain.
    /// </summary>
    public void ExpZeroBridge(double gamma, float labelSize)
    {
        Vector3 f = BridgeFunnelOrigin, co = BridgeChainOrigin;
        bridgeGamma = gamma;
        ExpFactorColumns(f, BridgeN, BridgeS, BridgeH, .7f, 0f, new long[0], true, false, .75f);
        ExpBridgeCoil(gamma, "ZB coil", new Color(1f, .85f, .2f));
        Transform a = ExpGroup("ZB chain axes").transform;
        Color axis = new Color(.55f, .6f, .78f) * .6f;
        ExpLine(a, "re", .1f, axis, Factory.NoGlow, new[] { co + Vector3.left * 20f, co + Vector3.right * 34f });
        ExpLine(a, "im", .1f, axis, Factory.NoGlow, new[] { co + Vector3.down * 20f, co + Vector3.up * 20f });
        TextBox zero = ExpLabelWide(a, "0", co + new Vector3(-1.4f, -1.8f, 0f), labelSize * .2f, new Color(.8f, .84f, .95f));
        Outline(zero.TextField);
    }

    /// <summary>The coil of the zero γ up the bridge funnel's axis, in its own group (so a second zero's coil can be compared).</summary>
    public void ExpBridgeCoil(double gamma, string group, Color col)
    {
        Vector3 f = BridgeFunnelOrigin;
        Transform g = ExpGroup(group).transform;
        double top = System.Math.Log(BridgeN) * 1.04;
        int steps = Mathf.CeilToInt((float)(gamma * top / (2 * System.Math.PI)) * 64f);
        Vector3[] coil = new Vector3[steps + 1];
        for (int i = 0; i <= steps; i++) coil[i] = BridgeCoilPoint(gamma, top * i / steps);
        ExpLine(g, "coil", .3f, col, Factory.GlowIntensity, coil);
        ExpLine(g, "axis", .12f, new Color(.6f, .64f, .82f) * .6f, Factory.NoGlow, new[] { f, f + Vector3.back * (float)(BridgeH * top) });
    }

    /// <summary>
    /// The whole chain at γ up to n links (group "ZB chain"), then its tail (orange, ZetaMath's Euler–Maclaurin correction)
    /// to ζ(½ + iγ), which is the origin at a zero.
    /// </summary>
    public void ExpBridgeChain(double gamma, int n)
    {
        Transform g = ExpGroup("ZB chain").transform;
        bridgeGamma = gamma;
        Vector3[] pts = BridgeChainPoints(BridgeChainOrigin, gamma, n);
        ExpLine(g, "chain", .24f, new Color(.95f, .97f, 1f), Factory.SoftGlowIntensity, pts);
        List<Vector3> dots = new(pts);
        List<Color> cols = new();
        List<float> sizes = new();
        foreach (Vector3 _ in pts) { cols.Add(new Color(1f, .86f, .45f)); sizes.Add(.5f); }
        ExpPointCloud(g, "vertices", dots, cols, sizes, Factory.GlowIntensity);
        ZetaMath.SpiralDecomposition d = ZetaMath.Decompose(new Complex(.5, gamma), n + 1);
        Vector3 P(Complex z) => BridgeChainOrigin + new Vector3((float)z.Re, (float)z.Im, 0f) * BridgeChain;
        ExpLine(g, "tail", .34f, new Color(1f, .55f, .2f), Factory.GlowIntensity, new[] { P(d.SpiralEnd), P(d.Total) });
        ExpDot(g, "zeta", P(d.Total) + Vector3.back * .3f, 1.3f, Factory.Palette.Zero, AttentionIntensity);
    }

    /// <summary>
    /// The live bridge: for n = from … to, a level line from the top of n's column to the axis, a hand from the axis to the
    /// coil at that height, a label with the turn γ·log n; then a copy of the hand shrinks by 1/√n, flies to the chain and
    /// becomes link n. The number's column lights while it happens.
    /// </summary>
    public void ExpBridgeLive(double gamma, int from, int to, float seconds, float labelSize)
    {
        StartLive(BridgeLive(gamma, from, to, seconds, labelSize));
    }

    IEnumerator BridgeLive(double gamma, int from, int to, float seconds, float labelSize)
    {
        Transform g = LiveRoot();
        Vector3 f = BridgeFunnelOrigin;
        bridgeGamma = gamma;
        Vector3[] chain = BridgeChainPoints(BridgeChainOrigin, gamma, to);
        // the links already in place before `from`
        if (from > 1)
        {
            Vector3[] done = new Vector3[from];
            System.Array.Copy(chain, done, from);
            ExpLine(g, "chain so far", .24f, new Color(.95f, .97f, 1f), Factory.SoftGlowIntensity, done);
        }

        float per = seconds / Mathf.Max(1, to - from + 1);
        for (int n = from; n <= to; n++)
        {
            double h = System.Math.Log(n);
            Vector3 axisPt = f + Vector3.back * (float)(BridgeH * h), coilPt = BridgeCoilPoint(gamma, h), colTop = FunnelTop(f, n, BridgeS, BridgeH);
            LineRenderer level = ExpLine(g, $"level {n}", .14f, new Color(.8f, .85f, 1f), Factory.SoftGlowIntensity, new[] { colTop, colTop });
            LineRenderer hand = ExpLine(g, $"hand {n}", .36f, Color.white, AttentionIntensity, new[] { axisPt, axisPt });
            LineRenderer column = ExpLine(g, $"column {n}", .55f, Color.white, AttentionIntensity, new[] { FunnelBase(f, n, BridgeS), colTop });
            double turns = gamma * h / (2 * System.Math.PI);
            TextBox tag = ExpLabelWide(g, n == 1 ? "1:  log 1 = 0,  no turn" : $"{n}:  γ·log {n} = {gamma:0.00} × {h:0.000} = {turns:0.00} turns",
                coilPt + new Vector3(3f, 3f, -.6f), labelSize * .26f, new Color(1f, .9f, .6f), TextAlignmentOptions.Left);
            Outline(tag.TextField);
            LineRenderer fly = ExpLine(g, $"link {n}", .34f, Color.white, AttentionIntensity, new[] { axisPt, axisPt });
            Vector3 linkFrom = chain[n - 1], linkTo = chain[n];
            float time = 0f;
            while (time < per)
            {
                if (fly == null) yield break; // the stage was cleared under us
                float u = time / per;
                // the level line, then the hand, hold, then fly and shrink into place
                float lv = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / .15f)), hd = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - .12f) / .15f));
                level.SetPositions(new[] { colTop, Vector3.Lerp(colTop, axisPt, lv) });
                hand.SetPositions(new[] { axisPt, Vector3.Lerp(axisPt, coilPt, hd) });
                float m = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - .45f) / .45f));
                Vector3 a = Vector3.Lerp(axisPt, linkFrom, m), b = Vector3.Lerp(Vector3.Lerp(axisPt, coilPt, hd), linkTo, m);
                fly.SetPositions(new[] { a, b });
                time += Time.deltaTime;
                yield return null;
            }

            if (fly == null) yield break;
            fly.SetPositions(new[] { linkFrom, linkTo });
            fly.startColor = fly.endColor = new Color(.95f, .97f, 1f);
            Factory.SetIntensity(fly, Factory.SoftGlowIntensity);
            fly.widthMultiplier = .24f;
            hand.startColor = hand.endColor = new Color(1f, .85f, .2f) * .7f;
            Factory.SetIntensity(hand, Factory.NoGlow);
            hand.widthMultiplier = .14f;
            Destroy(level.gameObject);
            Destroy(column.gameObject);
            if (n < to) Destroy(tag.gameObject);
        }
    }
}
