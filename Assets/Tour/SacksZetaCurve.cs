using System.Collections;
using System.Collections.Generic;
using Meta.Numerics;
using TMPro;
using UnityEngine;

/// <summary>
/// ζ on the critical line, as it is usually drawn (the long-form zeros segments): the curve ζ(½ + it) in the complex
/// plane, which passes through the origin at every zero, and the chain that makes each of its points, the arrows
/// n^(−s) = n^(−½) e^(−it log n) laid end to end, closed by the Euler–Maclaurin tail (ZetaMath.Decompose), whose end is ζ.
///   ExpZetaCurveLive  t grows: the chain re-forms every frame, its end traces the curve, and the origin flares at each zero
///   ExpZetaChainBuild one t: the chain is laid down link by link, then the tail, then the value
/// </summary>
public partial class SacksSpiral
{
    public const float ZetaCurveOffset = 96000f;
    const float ZetaCurveScale = 24f;

    public Vector3 ZetaCurveOrigin => Center + new Vector3(ZetaCurveOffset, 0f, 0f);

    Vector3 ZetaPlane(Complex z) => ZetaCurveOrigin + new Vector3((float)z.Re, (float)z.Im, 0f) * ZetaCurveScale;

    /// <summary>The point ζ(½ + it) in the plane, for callouts (exp:callzeta).</summary>
    public Vector3 ZetaCurvePoint(double t) => ZetaPlane(ZetaMath.Exact(new Complex(.5, t)));

    /// <summary>The plane's axes, tick marks from −3 to 4 and from −3i to 3i, and their labels (group "ZC axes").</summary>
    public void ExpZetaPlane(float labelSize)
    {
        Transform g = ExpGroup("ZC axes").transform;
        Vector3 o = ZetaCurveOrigin;
        float u = ZetaCurveScale, tick = u * .05f;
        Color axis = new Color(.55f, .6f, .78f) * .7f, text = new(.7f, .74f, .88f);
        List<(Vector3, Vector3)> segs = new() { (o + Vector3.left * u * 3.6f, o + Vector3.right * u * 4.4f), (o + Vector3.down * u * 3.3f, o + Vector3.up * u * 3.3f) };
        foreach (int k in new[] { -3, -2, -1, 1, 2, 3, 4 })
        {
            segs.Add((o + new Vector3(k * u, -tick, 0f), o + new Vector3(k * u, tick, 0f)));
            ExpLabelWide(g, k.ToString().Replace("-", "−"), o + new Vector3(k * u, -u * .17f, 0f), labelSize * .25f, text);
        }

        foreach (int k in new[] { -3, -2, -1, 1, 2, 3 })
        {
            segs.Add((o + new Vector3(-tick, k * u, 0f), o + new Vector3(tick, k * u, 0f)));
            ExpLabelWide(g, $"{k}i".Replace("-", "−").Replace("1i", "i"), o + new Vector3(-u * .1f, k * u, 0f), labelSize * .25f, text,
                TextAlignmentOptions.Right);
        }

        Segments(g, "axes", segs, .14f, axis, Factory.NoGlow);
        ExpLabelWide(g, "0", o + new Vector3(-u * .1f, -u * .15f, 0f), labelSize * .26f, Color.white);
    }

    /// <summary>ζ(½ + it) for t from 0 to t1, drawn at once (group "ZC curve").</summary>
    public void ExpZetaCurve(double t1)
    {
        Transform g = ExpGroup("ZC curve").transform;
        List<Vector3> pts = new();
        for (double t = 0; t <= t1; t += .02) pts.Add(ZetaPlane(ZetaMath.Exact(new Complex(.5, t))));
        ExpLine(g, "curve", .38f, Factory.Palette.Zero, Factory.GlowIntensity, pts.ToArray());
    }

    /// <summary>
    /// t from t0 to t1 in `seconds`: the curve grows (gold), the chain at t (white) with its tail (orange) ends on it, a
    /// readout gives t and the zeros passed so far, and the origin flares whenever t passes a zero.
    /// </summary>
    public void ExpZetaCurveLive(double t0, double t1, float seconds, float labelSize, bool chain)
    {
        StartLive(ZetaCurveLive(t0, t1, seconds, labelSize, chain));
    }

    IEnumerator ZetaCurveLive(double t0, double t1, float seconds, float labelSize, bool chain)
    {
        Transform g = LiveRoot();
        List<Vector3> curve = new();
        for (double t = 0; t < t0; t += .02) curve.Add(ZetaPlane(ZetaMath.Exact(new Complex(.5, t))));
        LineRenderer curveLine = ExpLine(g, "curve", .38f, Factory.Palette.Zero, Factory.GlowIntensity, curve.Count > 1 ? curve.ToArray() : new[] { ZetaCurveOrigin, ZetaCurveOrigin });
        LineRenderer chainLine = chain ? ExpLine(g, "chain", .24f, new Color(.95f, .97f, 1f), Factory.SoftGlowIntensity, new[] { ZetaCurveOrigin, ZetaCurveOrigin }) : null;
        LineRenderer tail = chain ? ExpLine(g, "tail", .3f, new Color(1f, .55f, .2f), Factory.GlowIntensity, new[] { ZetaCurveOrigin, ZetaCurveOrigin }) : null;
        GameObject dot = Factory.NewSphere("zeta", 2f, Factory.Palette.Zero, AttentionIntensity);
        dot.transform.SetParent(g, false);
        Vector3 corner = ZetaPlane(new Complex(-4.4, 2.0));
        TextBox readout = ExpLabelWide(g, "", corner, labelSize * .62f, Color.white, TextAlignmentOptions.Left);
        TextBox passed = ExpLabelWide(g, "", corner + Vector3.down * ZetaCurveScale * .4f, labelSize * .42f, Factory.Palette.Zero, TextAlignmentOptions.Left);
        Outline(readout.TextField);
        Outline(passed.TextField);
        double[] zs = Main.Instance.zetaZeroValues;
        int nextZero = System.Array.BinarySearch(zs, t0);
        if (nextZero < 0) nextZero = ~nextZero;
        List<string> seen = new();
        for (int k = 0; k < nextZero; k++) seen.Add(zs[k].ToString("0.00"));
        Complex[] buffer = new Complex[4096];
        double nextS = curve.Count * .02;
        float time = 0f;
        while (true)
        {
            if (curveLine == null) yield break; // the stage was cleared under us
            float u = Mathf.Clamp01(time / seconds);
            double t = t0 + (t1 - t0) * Mathf.SmoothStep(0f, 1f, u);
            // the curve: a point every .02 of t passed, then the moving end at t itself
            for (; nextS < t; nextS += .02) curve.Add(ZetaPlane(ZetaMath.Exact(new Complex(.5, nextS))));
            Complex zt = ZetaMath.Exact(new Complex(.5, t));
            curve.Add(ZetaPlane(zt));
            curveLine.positionCount = curve.Count;
            curveLine.SetPositions(curve.ToArray());
            curve.RemoveAt(curve.Count - 1);
            if (chain)
            {
                ZetaMath.SpiralDecomposition d = ZetaMath.Decompose(new Complex(.5, t), ZetaMath.TermCount(t), buffer);
                Vector3[] pts = new Vector3[d.N];
                for (int k = 0; k < d.N; k++) pts[k] = ZetaPlane(d.PartialSums[k]) + Vector3.back * .1f;
                chainLine.positionCount = pts.Length;
                chainLine.SetPositions(pts);
                tail.SetPositions(new[] { ZetaPlane(d.SpiralEnd), ZetaPlane(d.Total) });
            }

            dot.transform.position = ZetaPlane(zt) + Vector3.back * .3f;
            readout.TextField.text = $"t = {t:0.00}";
            while (nextZero < zs.Length && zs[nextZero] <= t)
            {
                Flare(ZetaCurveOrigin + Vector3.back * .4f, 3.6f, 1.6f, 2);
                seen.Add(zs[nextZero].ToString("0.00"));
                nextZero++;
            }

            passed.TextField.text = seen.Count == 0 ? "" : "zeros:  " + string.Join("  ·  ", seen);
            if (u >= 1f) yield break;
            time += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// The chain at one t laid down link by link in `seconds`. The link being laid is white hot, and a label names it
    /// (its length 1/√n and its turn t·log n, in turns) for the first six; then the tail, then the value ζ(½ + it) as a
    /// gold point, with a flare if it is a zero.
    /// </summary>
    public void ExpZetaChainBuild(double t, float seconds, float labelSize)
    {
        StartLive(ZetaChainBuild(t, seconds, labelSize));
    }

    IEnumerator ZetaChainBuild(double t, float seconds, float labelSize)
    {
        Transform g = LiveRoot();
        ZetaMath.SpiralDecomposition d = ZetaMath.Decompose(new Complex(.5, t), ZetaMath.TermCount(t));
        int n = d.N;
        Vector3[] all = new Vector3[n];
        for (int k = 0; k < n; k++) all[k] = ZetaPlane(d.PartialSums[k]) + Vector3.back * .1f;
        // PartialSums[0] is the origin and PartialSums[k] the sum of the first k links
        LineRenderer chainLine = ExpLine(g, "chain", .28f, new Color(.95f, .97f, 1f), Factory.SoftGlowIntensity, new[] { all[0], all[0] });
        LineRenderer hot = ExpLine(g, "link", .42f, Color.white, AttentionIntensity * 1.3f, new[] { all[0], all[0] });
        // the link being laid: a tag with its number beside it, and its description at a fixed place right of the plane's 1
        TextBox tag = ExpLabelWide(g, "", all[0], labelSize * .34f, Color.white);
        Outline(tag.TextField);
        TextBox label = ExpLabelWide(g, "", ZetaPlane(new Complex(1.18, .62)), labelSize * .3f, new Color(1f, .9f, .6f), TextAlignmentOptions.Left);
        Outline(label.TextField);
        const int named = 6;
        float slow = seconds * .62f / named, fast = seconds * .26f / Mathf.Max(1, n - 1 - named);
        for (int k = 1; k < n; k++)
        {
            float time = 0f, dur = k <= named ? slow : fast;
            if (k <= named)
            {
                double turns = t * System.Math.Log(k) / (2 * System.Math.PI);
                label.TextField.text = k == 1 ? "link 1:  1, pointing right" : $"link {k}:  1/√{k} long,\nturned t·log {k} = {turns:0.00} turns";
                Vector3 mid = (all[k - 1] + all[k]) * .5f, side = Vector3.Cross((all[k] - all[k - 1]).normalized, Vector3.back);
                tag.TextField.text = k.ToString();
                tag.transform.position = mid + side * ZetaCurveScale * .07f + Vector3.back * .6f;
            }
            else
            {
                label.TextField.text = $"links {named + 1} … {n - 1}:  shorter and shorter";
                tag.TextField.text = "";
            }

            while (time < dur)
            {
                if (chainLine == null) yield break; // the stage was cleared under us
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / (dur * .7f)));
                Vector3 end = Vector3.Lerp(all[k - 1], all[k], u);
                hot.SetPositions(new[] { all[k - 1], end });
                Vector3[] part = new Vector3[k + 1];
                System.Array.Copy(all, part, k);
                part[k] = end;
                chainLine.positionCount = part.Length;
                chainLine.SetPositions(part);
                time += Time.deltaTime;
                yield return null;
            }
        }

        if (chainLine == null) yield break;
        Destroy(hot.gameObject);
        chainLine.positionCount = n;
        chainLine.SetPositions(all);
        label.TextField.text = $"{n - 1} links,  then the tail:\nall the links beyond, added at once";
        LineRenderer tail = ExpLine(g, "tail", .36f, new Color(1f, .55f, .2f), Factory.GlowIntensity, new[] { ZetaPlane(d.SpiralEnd), ZetaPlane(d.SpiralEnd) });
        float tt = 0f;
        while (tt < .8f)
        {
            if (tail == null) yield break;
            tail.SetPositions(new[] { ZetaPlane(d.SpiralEnd), Vector3.Lerp(ZetaPlane(d.SpiralEnd), ZetaPlane(d.Total), tt / .8f) });
            tt += Time.deltaTime;
            yield return null;
        }

        tail.SetPositions(new[] { ZetaPlane(d.SpiralEnd), ZetaPlane(d.Total) });
        GameObject dot = Factory.NewSphere("zeta", 2f, Factory.Palette.Zero, AttentionIntensity);
        dot.transform.SetParent(g, false);
        dot.transform.position = ZetaPlane(d.Total) + Vector3.back * .3f;
        bool zero = d.Total.Re * d.Total.Re + d.Total.Im * d.Total.Im < 1e-4;
        Flare(ZetaPlane(d.Total) + Vector3.back * .4f, zero ? 4f : 2.6f, 1.4f, zero ? 2 : 1);
        label.TextField.text = zero ? $"the chain closes:  ζ(½ + {t:0.00}i) = 0" : $"the chain ends at ζ(½ + {t:0.00}i)";
        label.TextField.color = Factory.Palette.Zero;
    }
}
