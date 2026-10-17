using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The terms of the two master waves entering one at a time, over the flat number line of SacksNumberWorld (the long-form
/// segment on discovery 1), and two marks for the spiral:
///   ExpNwRows(kind, count)  rows above the line, one per term, hidden until they enter: for ζ's zeros (kind p) the wave
///       −cos(γ log x) of the k-th zero; for Voronoi's waves (kind d) his m-th wave, √2 x^¼ d(m) m^(−¼) sin(4π√(mx) − π/4),
///       drawn at its true size against the first; each with its formula. For the zeros a little critical line stands at
///       the left with the zeros ½ + iγ at their heights.
///   ExpNwEnter(k, seconds)  term k enters: the zero's dot leaves the critical line and flies into the row's formula (for
///       Voronoi's waves, the divisors of m light by the formula); the row draws on from left to right; then it drops onto
///       the line, and the line's sum takes it in (nw.terms.p or nw.terms.d becomes k).
///   ExpNwAngle(on)  on the spiral: the rays where Voronoi's first wave crests, 22.5° before the squares and the pronic
///       numbers (his formula's phase −π/4 is a sixteenth of a turn on the spiral), with the arc between.
///   ExpNwRadius(n, text)  the radius of n on the spiral, R√n, from the centre, labelled.
/// </summary>
public partial class SacksSpiral
{
    sealed class NwRow
    {
        public int K;
        public Transform Root;
        public LineRenderer Base, Wave;
        public TextBox Label;
        public Vector3[] Pts;      // the wave at the row's own height
        public Vector3 Lift;       // from the line up to the row
        public Color Col;
    }

    readonly List<NwRow> nwRows = new();
    readonly List<(GameObject dot, TextBox label)> nwZeroDots = new();
    string nwRowKind = "p";
    Transform nwRowsRoot;
    float nwRowGap = 10f;

    static readonly Color NwRowZero = new(.55f, 1f, .92f), NwRowVoronoi = new(1f, .72f, .45f);

    /// <summary>Rows for the first <paramref name="count"/> terms of kind p (ζ's zeros) or d (Voronoi's waves), above the flat line.</summary>
    public void ExpNwRows(string kind, int count, float gap, float amp, float labelSize, int first = 1)
    {
        if (nw == null) return;
        ExpNwRowsClear();
        nwRowKind = kind == "d" ? "d" : "p";
        nwRowGap = gap;
        nwRowsRoot = ExpGroup("NW rows").transform;
        NwLane L = nw;
        bool zeros = nwRowKind == "p";
        double[] zs = Main.Instance.zetaZeroValues;
        int i0 = (int)Math.Ceiling(Math.Sqrt(NwXMin) / nwDw);
        int cols = nwCols.Length - 1;
        // the first Voronoi wave's largest size sets the scale of all of his rows (so each is drawn at its true size)
        double vTop = 1e-9;
        if (!zeros)
            for (int c = 0; c < cols; c++)
            {
                int j = (nwCols[c] + nwCols[c + 1]) / 2;
                double x = nwW[j] * nwW[j];
                if (x >= NwXMin && x <= 40) vTop = Math.Max(vTop, Math.Abs(NwVoronoiTerm(1, x)));
            }

        for (int k = first; k <= count; k++)
        {
            NwRow row = new() { K = k, Root = new GameObject($"row {k}").transform, Col = zeros ? NwRowZero : NwRowVoronoi };
            row.Root.SetParent(nwRowsRoot, false);
            float off = gap * (k - first + 1) + amp * .3f;
            row.Lift = L.Left[NwSamples / 2] * off;
            List<Vector3> pts = new(cols);
            for (int c = 0; c < cols; c++)
            {
                int j = (nwCols[c] + nwCols[c + 1]) / 2;
                if (j < i0) continue;
                double x = nwW[j] * nwW[j];
                double v = zeros ? -Math.Cos(zs[k - 1] * nwLogX[j]) : NwVoronoiTerm(k, x) / vTop;
                pts.Add(L.P[j] + L.Left[j] * (off + (float)v * amp));
            }

            row.Pts = pts.ToArray();
            Vector3 b0 = L.P[i0] + L.Left[i0] * off, b1 = L.P[NwSamples] + L.Left[NwSamples] * off;
            row.Base = ExpLine(row.Root, "base", .08f, new Color(.6f, .64f, .8f) * .6f, Factory.NoGlow, new[] { b0, b1 });
            row.Wave = ExpLine(row.Root, "wave", .22f, row.Col, Factory.GlowIntensity, new[] { pts[0], pts[0] });
            string text;
            if (zeros)
            {
                double g = zs[k - 1];
                text = $"<color=#FFFFFF>γ<sub>{k}</sub> = {g:0.00}</color>   −cos({g:0.00} · log x)";
            }
            else
            {
                int d = waveDivisors[k];
                List<string> divs = new();
                for (int a = 1; a <= k; a++) if (k % a == 0) divs.Add(a.ToString());
                string root = k == 1 ? "√x" : $"√({k}x)";
                string amp2 = k == 1 ? "" : $"{d} · {k}<sup>−¼</sup> · ";
                text = $"<color=#FFFFFF>m = {k}:  d({k}) = {d}</color>  ({string.Join(", ", divs)})   {amp2}sin(4π{root} − π/4)";
            }

            // the formula above the row's left end
            row.Label = ExpLabelWide(row.Root, text, b0 + L.Left[i0] * (amp + 1f + labelSize * .06f) + Vector3.back * .2f, labelSize, row.Col, TextAlignmentOptions.Left);
            Outline(row.Label.TextField);
            row.Root.gameObject.SetActive(false);
            nwRows.Add(row);
        }

        // ζ's zeros on a little critical line, at the left: the dots that enter the formula
        if (zeros)
        {
            Transform axis = new GameObject("critical line").transform;
            axis.SetParent(nwRowsRoot, false);
            Vector3 foot = L.P[i0] - Vector3.right * 40f + L.Left[i0] * (gap * .5f);
            float top = gap * (count - first + 1.8f);
            ExpLine(axis, "line", .14f, PrimeGold * .8f, Factory.SoftGlowIntensity, new[] { foot, foot + L.Left[i0] * top });
            TextBox t = ExpLabelWide(axis, "Re s = ½", foot + L.Left[i0] * (top + labelSize * .14f), labelSize * .9f, PrimeGold);
            Outline(t.TextField);
            double gTop = zs[Mathf.Max(0, count - 1)];
            for (int k = first; k <= count; k++)
            {
                float hgt = (float)(zs[k - 1] / gTop) * gap * (count - first + 1);
                Vector3 at = foot + L.Left[i0] * hgt + Vector3.back * .3f;
                GameObject dot = ExpDot(axis, $"zero {k}", at, labelSize * .09f, Factory.Palette.Zero, AttentionIntensity);
                TextBox dl = ExpLabelWide(axis, $"½ + {zs[k - 1]:0.00}i", at + Vector3.right * (labelSize * .2f), labelSize * .8f, Color.white, TextAlignmentOptions.Left);
                Outline(dl.TextField);
                nwZeroDots.Add((dot, dl));
            }
        }
    }

    /// <summary>Voronoi's m-th wave as the line draws it (√2 x^¼ d(m) m^(−¼) sin(4π√(mx) − π/4), with the line's sign).</summary>
    double NwVoronoiTerm(int m, double x)
    {
        int d = waveDivisors[Mathf.Min(m, waveDivisors.Length - 1)];
        return -Math.Sqrt(2.0) * Math.Pow(x, .25) * d * Math.Pow(m, -.25) * Math.Sin(4.0 * Math.PI * Math.Sqrt(m * x) - Math.PI / 4.0);
    }

    public void ExpNwRowsClear()
    {
        if (nwRowsRoot != null) Destroy(nwRowsRoot.gameObject);
        nwRowsRoot = null;
        nwRows.Clear();
        nwZeroDots.Clear();
    }

    /// <summary>Term k enters: its zero flies into the formula, the row draws on, then drops into the line's sum.</summary>
    public void ExpNwEnter(int k, float seconds)
    {
        NwRow row = nwRows.Find(r => r.K == k);
        if (row == null) return;
        StartCoroutine(NwEnterRoutine(row, seconds));
    }

    IEnumerator NwEnterRoutine(NwRow row, float seconds)
    {
        bool zeros = nwRowKind == "p";
        float tFly = zeros ? seconds * .25f : 0f, tDraw = seconds * .35f, tDrop = seconds - tFly - tDraw - seconds * .1f;
        row.Root.gameObject.SetActive(true);
        row.Label.TextField.alpha = 0f;
        row.Wave.positionCount = 1;
        row.Wave.SetPosition(0, row.Pts[0]);
        Factory.SetMute(row.Base, 0f);
        // the zero's dot flies from the critical line into the formula
        int zi = nwRows.IndexOf(row);
        if (zeros && zi >= 0 && zi < nwZeroDots.Count && nwZeroDots[zi].dot != null)
        {
            var (dot, dl) = nwZeroDots[zi];
            GameObject fly = ExpDot(row.Root, "flying zero", dot.transform.position, dot.transform.localScale.x, Factory.Palette.Zero, AttentionIntensity * 1.4f);
            Vector3 a = dot.transform.position, b = row.Label.transform.position + Vector3.left * 3f;
            Flare(a, 2.5f, .8f, 1);
            float time = 0f;
            while (time < tFly)
            {
                if (fly == null) yield break;
                float u = Mathf.SmoothStep(0f, 1f, time / tFly);
                fly.transform.position = Vector3.Lerp(a, b, u) + Vector3.back * (4f * u * (1f - u));
                row.Label.TextField.alpha = Mathf.Clamp01((u - .6f) / .4f);
                time += Time.deltaTime;
                yield return null;
            }

            if (fly != null) Destroy(fly);
            Flare(b, 3f, .9f, 1);
            if (dl != null) dl.TextField.color = Color.Lerp(dl.TextField.color, Factory.Palette.Zero, .6f);
        }

        row.Label.TextField.alpha = 1f;
        // the row draws on
        float t2 = 0f;
        while (t2 < tDraw)
        {
            if (row.Wave == null) yield break;
            int n = Mathf.Clamp(Mathf.CeilToInt(row.Pts.Length * Mathf.SmoothStep(0f, 1f, t2 / tDraw)), 1, row.Pts.Length);
            row.Wave.positionCount = n;
            for (int i = 0; i < n; i++) row.Wave.SetPosition(i, row.Pts[i]);
            t2 += Time.deltaTime;
            yield return null;
        }

        if (row.Wave == null) yield break;
        row.Wave.positionCount = row.Pts.Length;
        row.Wave.SetPositions(row.Pts);
        yield return new WaitForSeconds(seconds * .1f);
        // it drops onto the line and the sum takes it in
        float t3 = 0f;
        Vector3[] moved = new Vector3[row.Pts.Length];
        while (t3 < tDrop)
        {
            if (row.Wave == null) yield break;
            float u = Mathf.SmoothStep(0f, 1f, t3 / tDrop);
            for (int i = 0; i < moved.Length; i++) moved[i] = row.Pts[i] - row.Lift * u;
            row.Wave.SetPositions(moved);
            row.Wave.startColor = row.Wave.endColor = new Color(row.Col.r, row.Col.g, row.Col.b, 1f - u * u);
            Factory.SetMute(row.Base, u * 1.6f);
            t3 += Time.deltaTime;
            yield return null;
        }

        if (row.Wave == null) yield break;
        row.Wave.gameObject.SetActive(false);
        Factory.SetMute(row.Base, 1.3f);
        row.Label.TextField.alpha = .45f;
        TwSet(zeros ? "nw.terms.p" : "nw.terms.d", row.K);
        if (nw != null) Flare(NwAt(nw, 20).p + Vector3.back * .4f, 6f, .8f, 1);
    }

    /// <summary>On the spiral: Voronoi's first-wave crest rays, 22.5° before the squares' and the pronic numbers' rays, with the arc.</summary>
    public void ExpNwAngle(bool on, float labelSize)
    {
        Transform old = ExpRoot().Find("NW angle");
        if (old != null) Destroy(old.gameObject);
        if (!on || nw == null) return;
        Transform g = ExpGroup("NW angle").transform;
        float k = Tw("nw.bend", 0f) * SaddleK, rMax = R * Mathf.Sqrt(NwN) * 1.04f;
        Vector3 Polar(float deg, float r)
        {
            float a = deg * Mathf.Deg2Rad, x = r * Mathf.Cos(a), y = r * Mathf.Sin(a);
            return nw.Centre + new Vector3(x, y, k * (x * x - y * y)) + Vector3.back * .15f;
        }

        Color vc = DivisorWaveColor;
        foreach (float deg in new[] { -22.5f, 157.5f })
        {
            Vector3[] ray = new Vector3[41];
            for (int i = 0; i <= 40; i++) ray[i] = Polar(deg, rMax * i / 40f);
            ExpLine(g, $"crest ray {deg}", .4f, vc, AttentionIntensity, ray);
        }

        foreach (float from in new[] { 0f, 180f })
        {
            // the arc between the square (or pronic) ray and the crest ray, inside the spiral, labelled nearer the centre
            Vector3[] arc = new Vector3[25];
            for (int i = 0; i <= 24; i++) arc[i] = Polar(from - 22.5f * i / 24f, rMax * .5f);
            ExpLine(g, $"arc {from}", .35f, Color.white, AttentionIntensity, arc);
            if (labelSize <= 0f) continue;
            TextBox t = ExpLabelWide(g, "22.5°", Polar(from - 11.25f, rMax * .28f), labelSize, Color.white);
            Outline(t.TextField);
        }
    }

    /// <summary>The radius of n on the spiral, from the centre, with a label at its middle.</summary>
    public void ExpNwRadius(double n, string text, float labelSize)
    {
        Transform old = ExpRoot().Find("NW radius");
        if (old != null) Destroy(old.gameObject);
        if (nw == null || n <= 0) return;
        Transform g = ExpGroup("NW radius").transform;
        var (p, up, _) = NwAt(nw, n);
        Vector3 c = nw.Centre + up * .2f, tip = p + up * .2f;
        LineRenderer l = ExpLine(g, "radius", .3f, Color.white, AttentionIntensity, new[] { c, tip });
        OnTop(l, 20);
        Vector3 dir = (tip - c).normalized, side = Vector3.Cross(dir, Vector3.back).normalized;
        ExpLine(g, "head", .3f, Color.white, AttentionIntensity, new[] { tip - dir * 1.6f + side * .9f, tip, tip - dir * 1.6f - side * .9f });
        TextBox t = ExpLabelWide(g, text, (c + tip) * .5f + side * (labelSize * .14f) + Vector3.back * .3f, labelSize, Color.white);
        Outline(t.TextField);
        Flare(tip + Vector3.back * .3f, 2.5f, 1f, 2);
    }
}
