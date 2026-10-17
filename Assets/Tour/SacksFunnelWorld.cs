using System;
using System.Collections;
using System.Collections.Generic;
using Meta.Numerics;
using TMPro;
using UnityEngine;

/// <summary>
/// Where a zero of ζ lives in E8's factor funnel, assembled step by step (the long-form segments on discovery 3 and the
/// zeros inside E8). The funnel (exp:zfunnel: every n ≤ 400 at its hour n mod 30, radius s√n, its column as tall as log n,
/// cut at its prime factors) is greyed out with exp:mute, and this scene draws on top of it:
///   the path of partial sums of ζ(½ + iγ) lying on the funnel's floor, centred on the axis (fw.path, fw.n links);
///   the lift (fw.lift 0 → 1): arrow n leaves the path and rises to the height log n of n's column, its tail moving to the
///   axis and its length to the coil's radius, but its direction kept: −γ·log n. Stacked, the arrows are the hands of a
///   clock that turns γ per unit of height, and the coil (fw.coil 0 → 1, drawn from the bottom up) runs through their tips.
///   So a zero is a coil up E8's funnel: the coil at the height of n points the way the path's n-th arrow points.
///   highlights (exp:fwhl:N): n's column bright in its prime factors' colours, its top, its level (a ring round the axis and
///   a line from the column to the axis) and its hand; exp:fwangles:N shows the hand of n = p·q as the sum of the turns of
///   its prime factors (log n = log p + log q).
/// fw.gamma is the zero; easing it to another zero turns every hand by Δγ·log n, the higher ones more: a tighter coil.
/// </summary>
public partial class SacksSpiral
{
    // the coil's radius is the floor path's own scale: an arrow of length 1/√n (drawn FwPathScale/√n) stretched by √n is
    // exactly FwCoilR long (fw.stretch), so the coil carries each arrow's direction at its true scale of one unit
    const float FwPathScale = 18f, FwCoilR = 18f;
    const int FwHands = 15;

    class FwScene
    {
        public Transform Root;
        public LineRenderer Path, Tail, Coil, Axis;
        public LineRenderer[] Arrows = new LineRenderer[FwHands + 1];
        public TextBox[] Tags = new TextBox[FwHands + 1];
        public readonly List<LineRenderer> Turns = new();   // the coil, one line per turn, each a gradient (fw.coil)
        public TextBox StretchTag;
        public GameObject Zeta;
        public string Sig = "";
        public readonly Dictionary<long, Transform> Lit = new();
    }

    FwScene fw;

    Vector3 FwOrigin => Center + new Vector3(ZeroModuleOffset, 0f, 0f);

    Vector3 FwUp(double h) => Vector3.back * (float)(ZmH * h);

    static Vector3 FwDir(double a) => new((float)Math.Cos(a), (float)Math.Sin(a), 0f);

    /// <summary>Set up the scene over the zeros module's funnel (draw that first with exp:zfunnel) and start its frame loop.</summary>
    public void ExpFunnelWorld()
    {
        ClearTweens("fw.");
        fw = new FwScene { Root = ExpGroup("FW coil scene").transform };
        fwScanRing = fwScanHand = null;
        fwScanTag = null;
        fwArcs.Clear();
        fwGhosts.Clear();
        fwOutline = null;
        Array.Clear(fwFans, 0, fwFans.Length);
        fwCombGroup = fwCombLabels = null;
        fwMorphSig = "";
        fwAngleHands.Clear();
        WarmFans();
        Color white = new(.95f, .97f, 1f);
        fw.Path = ExpLine(fw.Root, "path", .3f, white, Factory.SoftGlowIntensity, new[] { FwOrigin, FwOrigin });
        fw.Tail = ExpLine(fw.Root, "tail", .36f, new Color(1f, .55f, .2f), Factory.GlowIntensity, new[] { FwOrigin, FwOrigin });
        fw.Coil = ExpLine(fw.Root, "coil", .34f, new Color(1f, .85f, .1f), Factory.GlowIntensity, new[] { FwOrigin, FwOrigin });
        fw.Axis = ExpLine(fw.Root, "axis", .16f, Color.white * .8f, Factory.SoftGlowIntensity, new[] { FwOrigin, FwOrigin + FwUp(Math.Log(ZmN)) });
        for (int n = 1; n <= FwHands; n++)
        {
            fw.Arrows[n] = ExpLine(fw.Root, $"arrow {n}", .34f, white, Factory.GlowIntensity, new[] { FwOrigin, FwOrigin });
            if (n <= 9)
            {
                fw.Tags[n] = ExpLabelWide(fw.Root, n.ToString(), FwOrigin, 14f, new Color(1f, .9f, .6f));
                Outline(fw.Tags[n].TextField);
            }
        }

        fw.Zeta = Factory.NewSphere("zeta", 1.2f, Factory.Palette.Zero, AttentionIntensity);
        fw.Zeta.transform.SetParent(fw.Root, false);
        fw.Coil.gameObject.SetActive(false);
        fw.StretchTag = ExpLabelWide(fw.Root, "", FwOrigin, 16f, Color.white, TextAlignmentOptions.Left);
        Outline(fw.StretchTag.TextField);
        fw.StretchTag.gameObject.SetActive(false);
        StartCoroutine(FunnelWorldLoop(fw));
    }

    IEnumerator FunnelWorldLoop(FwScene s)
    {
        while (s == fw && s.Root != null)
        {
            FwUpdate(s);
            FwMorphUpdate(s);
            yield return null;
        }
    }

    void FwUpdate(FwScene s)
    {
        double g = Tw("fw.gamma", 14.134725f);
        float shown = Tw("fw.n", 0f), lift = Tw("fw.lift", 0f), coil = Tw("fw.coil", 0f), pathVis = Tw("fw.path", 1f), tail = Tw("fw.tail", 0f);
        float hands = Tw("fw.hands", 1f), axis = Tw("fw.axis", 0f), dim = Tw("fw.dim", 0f), stretch = Tw("fw.stretch", 1f);
        string lit = string.Join(",", s.Lit.Keys) + "/" + string.Join(",", fwAngleHands);
        string sig = $"{g:F5}|{shown:F3}|{lift:F3}|{coil:F3}|{pathVis:F3}|{tail:F3}|{hands:F3}|{axis:F3}|{dim:F3}|{stretch:F4}|{lit}";
        if (sig == s.Sig) return;
        s.Sig = sig;
        Vector3 o = FwOrigin + Vector3.back * .15f;
        // the path on the floor: S_k = Σ_{n ≤ k} n^(−½ − iγ)
        Vector3[] floor = new Vector3[FwHands + 1];
        double re = 0, im = 0;
        floor[0] = o;
        for (int n = 1; n <= FwHands; n++)
        {
            double a = -g * Math.Log(n), len = 1.0 / Math.Sqrt(n);
            re += len * Math.Cos(a);
            im += len * Math.Sin(a);
            floor[n] = o + new Vector3((float)re, (float)im, 0f) * FwPathScale;
        }

        int whole = Mathf.Clamp(Mathf.FloorToInt(shown), 0, FwHands);
        float frac = whole < FwHands ? Mathf.Clamp01(shown - whole) : 0f;
        // the floor path (what remains of it: the arrows not yet lifted)
        List<Vector3> path = new();
        for (int n = 0; n <= whole; n++) path.Add(floor[n]);
        if (frac > 0f) path.Add(Vector3.Lerp(floor[whole], floor[whole + 1], Mathf.SmoothStep(0f, 1f, frac)));
        s.Path.positionCount = path.Count;
        s.Path.SetPositions(path.ToArray());
        s.Path.startColor = s.Path.endColor = new Color(.95f, .97f, 1f, pathVis * .55f);
        // the tail to ζ(½ + iγ) and its value
        bool tailOn = tail > .01f && whole >= FwHands;
        s.Tail.gameObject.SetActive(tailOn);
        s.Zeta.SetActive(tailOn && tail > .99f);
        if (tailOn)
        {
            ZetaMath.SpiralDecomposition d = ZetaMath.Decompose(new Complex(.5, g), FwHands + 1);
            Vector3 e = o + new Vector3((float)d.Total.Re, (float)d.Total.Im, 0f) * FwPathScale;
            s.Tail.SetPositions(new[] { floor[FwHands], Vector3.Lerp(floor[FwHands], e, tail) });
            s.Zeta.transform.position = e + Vector3.back * .1f;
        }

        // the arrows, lifting one after another to their heights, turning into the hands of the coil
        for (int n = 1; n <= FwHands; n++)
        {
            LineRenderer a = s.Arrows[n];
            float exist = n <= whole ? 1f : n == whole + 1 ? frac : 0f;
            bool on = exist > 0f && hands > .01f;
            a.gameObject.SetActive(on);
            if (s.Tags[n] != null) s.Tags[n].gameObject.SetActive(on && lift > .5f);
            if (!on) continue;
            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((lift - (n - 1) / (float)FwHands * .55f) / .45f));
            double h = Math.Log(n), dir = -g * h;
            // lifted, the arrow keeps its true length 1/√n until it is stretched by √n (fw.stretch) to the coil's radius
            float handLen = Mathf.Lerp(FwPathScale / Mathf.Sqrt(n), FwCoilR, stretch);
            Vector3 handTail = o + FwUp(h), handHead = handTail + FwDir(dir) * handLen;
            Vector3 t0 = Vector3.Lerp(floor[n - 1], handTail, u), t1 = Vector3.Lerp(Vector3.Lerp(floor[n - 1], floor[n], exist), handHead, u);
            a.SetPositions(new[] { t0 + Vector3.back * .05f, t1 + Vector3.back * .05f });
            bool bright = s.Lit.ContainsKey(n) || fwAngleHands.Contains(n);
            float alpha = bright ? Mathf.Max(hands, dim) : hands * (1f - .85f * dim);
            a.widthMultiplier = Mathf.Lerp(.3f, .42f, u) * (bright ? 1f + .4f * dim : 1f);
            Color c = Color.Lerp(new Color(.95f, .97f, 1f), new Color(1f, .9f, .45f), u);
            if (bright) c = Color.Lerp(c, Color.white, dim);
            a.startColor = a.endColor = new Color(c.r, c.g, c.b, alpha);
            if (s.Tags[n] != null)
            {
                s.Tags[n].transform.position = handHead + FwDir(dir) * 1.6f + Vector3.back * .3f;
                s.Tags[n].TextField.alpha = alpha * Mathf.Clamp01((u - .5f) * 2f);
            }
        }

        // the coil through the hands' tips, drawn from the floor up to coil × log 400, one line per turn: each turn a
        // gradient from deep orange to bright yellow, so the turns can be counted as the coil tightens. Before the hands
        // are stretched it is a horn: radius FwPathScale·e^(−h/2), the tips of the arrows at their true lengths.
        bool coilOn = coil > .001f;
        double topH = Math.Log(ZmN) * coil;
        int turns = coilOn ? Mathf.Max(1, Mathf.CeilToInt((float)(g * topH / (2 * Math.PI)))) : 0;
        while (s.Turns.Count < turns)
        {
            LineRenderer l = ExpLine(s.Root, $"coil turn {s.Turns.Count}", .34f, Color.white, Factory.GlowIntensity, new[] { o, o });
            s.Turns.Add(l);
        }

        for (int k = 0; k < s.Turns.Count; k++)
        {
            LineRenderer l = s.Turns[k];
            bool on = k < turns;
            l.gameObject.SetActive(on);
            if (!on) continue;
            double h0 = 2 * Math.PI * k / g, h1 = Math.Min(topH, 2 * Math.PI * (k + 1) / g);
            const int steps = 48;
            Vector3[] pts = new Vector3[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                double h = h0 + (h1 - h0) * i / steps;
                float r = Mathf.Lerp(FwPathScale * Mathf.Exp(-(float)h * .5f), FwCoilR, stretch);
                pts[i] = o + FwUp(h) + FwDir(-g * h) * r;
            }

            l.positionCount = pts.Length;
            l.SetPositions(pts);
            float a = 1f - .9f * dim, part = (float)((h1 - h0) / (2 * Math.PI / g));
            l.startColor = new Color(1f, .42f, .08f, a);
            l.endColor = Color.Lerp(new Color(1f, .42f, .08f, a), new Color(1f, .95f, .35f, a), part);
        }

        // the stretch, labelled at the hand of 2 while it happens
        bool stretching = stretch > .02f && stretch < .98f && whole >= 2;
        s.StretchTag.gameObject.SetActive(stretching);
        if (stretching)
        {
            double h2 = Math.Log(2);
            float len = Mathf.Lerp(FwPathScale / Mathf.Sqrt(2f), FwCoilR, stretch);
            s.StretchTag.transform.position = o + FwUp(h2) + FwDir(-g * h2) * (len + 3f) + Vector3.back * .4f;
            s.StretchTag.TextField.text = $"1/√2 × √2 = 1";
        }

        s.Axis.gameObject.SetActive(axis > .01f);
        s.Axis.startColor = s.Axis.endColor = new Color(1f, 1f, 1f, axis * .8f);
    }

    /// <summary>Where the hand of n points at the top of its level, for the current zero (callouts: exp:callhand:N).</summary>
    public Vector3 FwHandTip(long n)
    {
        double g = Tw("fw.gamma", 14.134725f), h = Math.Log(Math.Max(1, n));
        return FwOrigin + FwUp(h) + FwDir(-g * h) * FwCoilR + Vector3.back * .3f;
    }

    /// <summary>
    /// Light n in the funnel: its column in its prime factors' colours (largest prime at the floor), each segment labelled
    /// log p; the top of the column; its level, a ring round the axis at the height log n and a line from the column's top to
    /// the axis; and a label n = p·q. Group "FW lit n" (not muted); exp:fwunhl removes it.
    /// </summary>
    public void ExpFunnelLight(long n, bool segLabels)
    {
        if (fw == null) return;
        ExpFunnelUnlight(n);
        Transform g = ExpGroup($"FW lit {n}").transform;
        fw.Lit[n] = g;
        Vector3 o = FwOrigin;
        Vector3 b = FunnelBase(o, n, ZmS);
        List<long> f = Factorize(n);
        f.Reverse(); // largest prime at the floor
        double h = 0;
        foreach (long p in f)
        {
            double lp = Math.Log(p);
            Vector3 a0 = b + FwUp(h + .03) + Vector3.back * .1f, a1 = b + FwUp(h + lp - .03) + Vector3.back * .1f;
            LineRenderer seg = ExpLine(g, $"segment {p}", .55f, Color.white, AttentionIntensity * 1.2f, new[] { a0, a1 });
            StartCoroutine(SettleRoutine(seg, SegmentColor(p), 1f));
            if (segLabels)
            {
                TextBox l = ExpLabelWide(g, $"log {p}", (a0 + a1) * .5f + new Vector3(1.6f, 0f, -.3f), 12f, SegmentColor(p), TextAlignmentOptions.Left);
                Outline(l.TextField);
            }

            h += lp;
        }

        Vector3 top = b + FwUp(h);
        ExpDot(g, "top", top + Vector3.back * .2f, 1.1f, n > 1 && Factorize(n).Count == 1 ? PrimeGold : Color.white, AttentionIntensity);
        Flare(top + Vector3.back * .3f, 3f, 1.2f, 2);
        // the clock arm: on the floor, from the axis out through the base of n's column to the rim, at hour n mod 30
        int hour = (int)(n % 30);
        // the arm reaches a little past the column's base, so a close shot holds it and its hour
        float ah = (90f - 12f * hour) * Mathf.Deg2Rad, rim = Mathf.Max(ZmS * Mathf.Sqrt(n) + 3f, 9f);
        Vector3 hourDir = new(Mathf.Cos(ah), Mathf.Sin(ah), 0f);
        Color hourCol = GoldHour(hour) ? PrimeGold : new Color(.85f, .88f, 1f);
        LineRenderer arm = ExpLine(g, "hour arm", .22f, Color.white, AttentionIntensity, new[] { o + Vector3.back * .15f, o + hourDir * rim + Vector3.back * .15f });
        OnTop(arm, 18);
        StartCoroutine(SettleRoutine(arm, hourCol, 1f));
        TextBox hourTag = ExpLabelWide(g, $"hour {hour}", o + hourDir * (rim + 3f) + Vector3.back * .4f, 14f, hourCol);
        Outline(hourTag.TextField);
        // the level: a ring round the axis and a line from the column's top to the axis
        Vector3 c = o + FwUp(h);
        Vector3[] ring = new Vector3[65];
        for (int i = 0; i <= 64; i++) ring[i] = c + FwDir(i * Math.PI / 32) * 10f + Vector3.back * .05f;
        ExpLine(g, "level ring", .12f, new Color(.85f, .9f, 1f, .7f), Factory.SoftGlowIntensity, ring);
        ExpLine(g, "level line", .12f, new Color(.85f, .9f, 1f, .7f), Factory.SoftGlowIntensity, new[] { top + Vector3.back * .05f, c + Vector3.back * .05f });
        string name = n == 1 ? "1" : Factorize(n).Count == 1 ? $"{n}:  prime" : $"{n} = {FactorString(n)}";
        TextBox lab = ExpLabelWide(g, name, top + new Vector3(0f, 0f, -2.2f), 16f, Color.white);
        Outline(lab.TextField);
    }

    /// <summary>Remove the highlight of n (or of every number with n = 0).</summary>
    public void ExpFunnelUnlight(long n)
    {
        if (fw == null) return;
        List<long> keys = new(fw.Lit.Keys);
        foreach (long k in keys)
        {
            if (n != 0 && k != n) continue;
            if (fw.Lit[k] != null) Destroy(fw.Lit[k].gameObject);
            fw.Lit.Remove(k);
        }

        if (n == 0) fwAngleHands.Clear();
    }

    // the hands lit by ExpFunnelAngles: n and its prime factors (kept bright when fw.dim fades the rest)
    readonly HashSet<long> fwAngleHands = new();

    /// <summary>
    /// At the level of n = p·q·…: the turn of each prime factor as an arc from the direction of the hand of 1 (the floor's
    /// "right"), the arcs chained, so that γ·log n = γ·log p + γ·log q + … Whole turns change nothing on a clock, so each
    /// arc shows only what is left after them, and its label the full count ("γ·log 2 = 1.56 turns" draws .56 of a turn).
    /// The hand of n is their sum (group "FW angles n"); n and its factors' hands stay bright under fw.dim.
    /// </summary>
    public void ExpFunnelAngles(long n)
    {
        Transform g = ExpGroup($"FW angles {n}").transform;
        double gm = Tw("fw.gamma", 14.134725f), hn = Math.Log(n);
        Vector3 c = FwOrigin + FwUp(hn) + Vector3.back * .25f;
        List<long> f = Factorize(n);
        fwAngleHands.Clear();
        fwAngleHands.Add(n);
        foreach (long p in f) fwAngleHands.Add(p);
        double acc = 0;
        float r = FwCoilR * .42f;
        int i = 0;
        // the start: the direction of the hand of 1
        ExpLine(g, "zero", .14f, new Color(.8f, .84f, .95f), Factory.SoftGlowIntensity, new[] { c, c + FwDir(0) * FwCoilR * 1.05f });
        foreach (long p in f)
        {
            double full = gm * Math.Log(p), part = full - 2 * Math.PI * Math.Floor(full / (2 * Math.PI)), turn = -part;
            float rr = r + i * 1.6f;
            List<Vector3> arc = new();
            int steps = Mathf.Max(8, Mathf.CeilToInt((float)part * 16f));
            for (int k = 0; k <= steps; k++) arc.Add(c + FwDir(acc + turn * k / steps) * rr);
            LineRenderer l = ExpLine(g, $"turn {p} #{i}", .34f, SegmentColor(p), Factory.GlowIntensity, arc.ToArray());
            OnTop(l, 21);
            ArrowHead(g, $"turn head {p} #{i}", arc[^2], arc[^1], .9f, SegmentColor(p), .26f);
            TextBox t = ExpLabelWide(g, $"γ·log {p} = {full / (2 * Math.PI):0.00} turns", c + FwDir(acc + turn * .5) * (rr + 2.6f + i * .4f),
                12f, SegmentColor(p));
            Outline(t.TextField);
            acc += turn;
            i++;
        }

        LineRenderer sum = ExpLine(g, "sum", .4f, Color.white, AttentionIntensity, new[] { c, c + FwDir(acc) * FwCoilR });
        OnTop(sum, 22);
        TextBox st = ExpLabelWide(g, $"γ·log {n} = {gm * hn / (2 * Math.PI):0.00} turns", c + FwDir(acc) * (FwCoilR + 3.4f), 13f, Color.white);
        Outline(st.TextField);
    }
}
