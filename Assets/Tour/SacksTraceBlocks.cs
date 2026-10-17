using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// E8's clock inside the gold path of partial sums (the long-form segment on discovery 2). On the path that keeps only
/// the n on E8's eight gold hours, near spoke k (n ≈ 30t/2πk) the path runs as blocks of eight arrows, one per gold hour:
/// the arrow of n = c + h (c a multiple of 30) is turned from the block's direction by about t·h/c = 2πk·h/30, so seen
/// from the block, it points at hour k·h of E8's clock, and the block adds up to Σ cos(2πkh/30) = tr Cᵏ, the trace of
/// E8's Coxeter element (at t = 5000: −0.998 for k = 1, 3.98 for k = 5, −7.95 for k = 15).
///   ExpTraceBlock     light one block of the path (the path scene must be up, exp:pwinit)
///   ExpTraceLift      lift it onto E8's clock (the wheel drawn by exp:e8wheel): each arrow turned so the block's
///                     direction points at hour 0, scaled to an eighth of the clock's radius, chained from its centre;
///                     the chain ends on the vertical diameter at tr Cᵏ (−8 at the rim of hour 15, +8 at hour 0)
///   ExpTraceSweep     the same chain for every k at once, k sliding continuously: its end runs up and down the diameter,
///                     and as k passes each whole number a bar drops into the chart of traces
///   ExpTraceSieve     the thirty traces, each turned by an hour n, chained: for a gold hour they end on its rim, else at 0
/// </summary>
public partial class SacksSpiral
{
    static readonly int[] TbOffsets = { -13, -11, -7, -1, 1, 7, 11, 13 };

    class TbBlock
    {
        public int K;
        public long C;
        public double T;
        public long[] N = new long[8];
        public Vector3[] A = new Vector3[8], B = new Vector3[8];
        public double[] Dir = new double[8];
        public LineRenderer[] Lines = new LineRenderer[8];
        public Transform Root;
    }

    readonly Dictionary<int, TbBlock> tbBlocks = new();
    Vector3 tbBarsOrigin;
    // (wider bars in a 9:16 frame, where the chart stands under the clock and has the frame's width to itself)
    float tbBarDx => Tall ? 1.6f : 1.25f;
    const float tbBarUnit = 1.1f;
    bool tbBarsReady;
    readonly Dictionary<int, LineRenderer> tbBars = new();

    static int Trace30(int k)
    {
        double s = 0;
        foreach (int h in ZeroSums.Gold) s += Math.Cos(2 * Math.PI * k * h / 30.0);
        return (int)Math.Round(s);
    }

    static double TraceAt(double kappa)
    {
        double s = 0;
        foreach (int h in TbOffsets) s += Math.Cos(2 * Math.PI * kappa * h / 30.0);
        return s;
    }

    Vector3 ClockDir(double angle) => new((float)Math.Cos(angle), (float)Math.Sin(angle), 0f);

    /// <summary>Light the block of eight gold arrows of the path near spoke k (at the path's current t).</summary>
    public void ExpTraceBlock(int k)
    {
        if (pw == null) return;
        double t = Tw("pw.t", 1f);
        long m = (long)Math.Round(t / (2 * Math.PI * k));
        TbBlock b = new() { K = k, C = 30 * m, T = t, Root = ExpGroup($"TB block {k}").transform };
        float width = 0f;
        for (int i = 0; i < 8; i++)
        {
            long n = b.C + TbOffsets[i];
            b.N[i] = n;
            b.Dir[i] = -t * Math.Log(n);
            b.A[i] = PathVertex((int)n - 1) + Vector3.back * .05f;
            b.B[i] = PathVertex((int)n) + Vector3.back * .05f;
            width = Mathf.Max(width, Vector3.Distance(b.A[i], b.B[i]));
        }

        Transform marks = new GameObject("marks").transform;
        marks.SetParent(b.Root, false);
        Vector3 along = (b.B[7] - b.A[0]).normalized, side = Vector3.Cross(along, Vector3.back).normalized;
        for (int i = 0; i < 8; i++)
        {
            Color col = ZsHourColor((int)(b.N[i] % 30));
            b.Lines[i] = ExpLine(b.Root, $"arrow {b.N[i]}", width * .22f, Color.white, AttentionIntensity * 1.3f, new[] { b.A[i], b.B[i] });
            OnTop(b.Lines[i], 20);
            StartCoroutine(SettleRoutine(b.Lines[i], col, 1.2f));
            // its head, the joint it starts from, and its number and hour, on alternate sides
            ArrowHead(marks, $"head {b.N[i]}", b.A[i], b.B[i], width * .32f, col, width * .07f);
            ExpDot(marks, $"joint {b.N[i]}", b.A[i] + Vector3.back * .08f, width * .09f, Color.white, Factory.GlowIntensity);
            float sgn = i % 2 == 0 ? 1f : -1f;
            // (not enlarged in a 9:16 frame: the labels on one side are only two arrows apart, and the camera comes closer)
            TextBox tag = ExpLabelWide(marks, $"{b.N[i]}\n<size=70%>hour {b.N[i] % 30}</size>", (b.A[i] + b.B[i]) * .5f + side * (sgn * width * 1.45f) + Vector3.back * .1f,
                width * 7f / LabelBoost, col);
            Outline(tag.TextField);
        }

        if (tbBlocks.TryGetValue(k, out TbBlock old) && old.Root != null) Destroy(old.Root.gameObject);
        tbBlocks[k] = b;
        Vector3 mid = (b.A[0] + b.B[7]) * .5f;
        Flare(mid + Vector3.back * .2f, width * 6f, 1.2f, 2);
    }

    /// <summary>A V-shaped head at the end <paramref name="b"/> of the arrow from <paramref name="a"/>.</summary>
    void ArrowHead(Transform g, string name, Vector3 a, Vector3 b, float size, Color col, float width)
    {
        Vector3 dir = (b - a).normalized, side = Vector3.Cross(dir, Vector3.back).normalized;
        LineRenderer h = ExpLine(g, name, width, col, Factory.GlowIntensity,
            new[] { b - dir * size + side * size * .55f + Vector3.back * .06f, b + Vector3.back * .06f, b - dir * size - side * size * .55f + Vector3.back * .06f });
        OnTop(h, 21);
    }

    /// <summary>Where the block of spoke k sits on the path (for the camera and callouts).</summary>
    public Vector3 TraceBlockAt(int k)
    {
        if (tbBlocks.TryGetValue(k, out TbBlock b)) return (b.A[0] + b.B[7]) * .5f;
        if (pw == null) return PathOrigin;
        double t = Tw("pw.t", 1f);
        long c = 30 * (long)Math.Round(t / (2 * Math.PI * k));
        return PathVertex((int)c);
    }

    /// <summary>Lift the lit block of spoke k onto E8's clock, arrow by arrow, and mark where its sum lands.</summary>
    public void ExpTraceLift(int k, float seconds)
    {
        if (!tbBlocks.TryGetValue(k, out TbBlock b) || !e8WheelReady) return;
        StartCoroutine(TraceLiftRoutine(b, seconds));
    }

    IEnumerator TraceLiftRoutine(TbBlock b, float seconds)
    {
        Transform g = ExpGroup($"TB chain {b.K}").transform;
        Vector3 c = e8WheelCenter + Vector3.back * .6f;
        float unit = e8HourRadius / 8f;
        double turn = Math.PI / 2 + b.T * Math.Log(b.C);
        Vector3[] q = new Vector3[9];
        q[0] = c;
        for (int i = 0; i < 8; i++) q[i + 1] = q[i] + ClockDir(b.Dir[i] + turn) * unit;
        LineRenderer[] flying = new LineRenderer[8];
        Transform marks = b.Root != null ? b.Root.Find("marks") : null;
        if (marks != null) Destroy(marks.gameObject);
        float w0 = b.Lines[0] != null ? b.Lines[0].widthMultiplier : .1f, w1 = unit * .3f;
        for (int i = 0; i < 8; i++)
        {
            Color col = ZsHourColor((int)(b.N[i] % 30));
            flying[i] = ExpLine(g, $"arrow {b.N[i]}", w0, col, Factory.GlowIntensity, new[] { b.A[i], b.B[i] });
            OnTop(flying[i], 21);
        }

        // the hours the arrows point at: k·h on the clock, each ringed; when they are few (k = 15: one, k = 5: two) each is
        // also named, beside the wheel's own hour numbers
        HashSet<int> marked = new();
        foreach (int h in ZeroSums.Gold) marked.Add((int)(((long)b.K * h) % 30));
        foreach (int hour in marked)
        {
            Vector3 at = E8Hour(hour, e8HourRadius * 1.08f) + Vector3.back * .6f;
            RingAround(g, $"hour {hour}", at, Vector3.back, unit * .35f, Color.white * .9f, unit * .05f, AttentionIntensity);
            // the clock arm of the hour, from the centre to the rim
            LineRenderer arm = ExpLine(g, $"arm {hour}", unit * .14f, new Color(1f, 1f, 1f, .75f), Factory.SoftGlowIntensity,
                new[] { e8WheelCenter + Vector3.back * .45f, E8Hour(hour, e8HourRadius * 1.04f) + Vector3.back * .45f });
            OnTop(arm, 19);
            if (marked.Count > 2) continue;
            TextBox tag = ExpLabelWide(g, $"hour {hour}", E8Hour(hour, e8HourRadius * 1.08f + unit * 1.4f) + Vector3.back * .7f, unit * 16f, Color.white);
            Outline(tag.TextField);
        }

        float time = 0f;
        while (time < seconds)
        {
            if (g == null) yield break;
            for (int i = 0; i < 8; i++)
            {
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((time / seconds - i * .045f) / .62f));
                if (flying[i] == null) yield break;
                flying[i].SetPositions(new[] { Vector3.Lerp(b.A[i], q[i], u), Vector3.Lerp(b.B[i], q[i + 1], u) });
                flying[i].widthMultiplier = Mathf.Lerp(w0, w1, u);
            }

            time += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < 8; i++)
        {
            if (flying[i] == null) continue;
            flying[i].SetPositions(new[] { q[i], q[i + 1] });
            Color col = ZsHourColor((int)(b.N[i] % 30));
            ArrowHead(g, $"head {b.N[i]}", q[i], q[i + 1], unit * .34f, col, unit * .1f);
            if (i > 0) ExpDot(g, $"joint {i}", q[i] + Vector3.back * .15f, unit * .2f, Color.white, Factory.GlowIntensity);
        }

        // the sum: a dot where the chain ends, on the clock's vertical diameter
        int tr = Trace30(b.K);
        ExpDot(g, "sum", q[8] + Vector3.back * .2f, unit * .45f, Color.white, AttentionIntensity);
        Flare(q[8] + Vector3.back * .3f, unit * 1.4f, 1.2f, 2);
        LineRenderer sumArrow = ExpLine(g, "sum arrow", unit * .16f, Color.white, AttentionIntensity, new[] { c, q[8] });
        OnTop(sumArrow, 22);
        StartCoroutine(SettleRoutine(sumArrow, tr < 0 ? new Color(.35f, .8f, 1f) : PrimeGold, 1f));
    }

    // ---- E8's wheel brought onto a block: the clock turns to the path, so no arrow has to turn -----------------------

    static readonly string[] WheelGroups = { "E8 rings", "E8 walk", "TB scale" };
    readonly Dictionary<LineRenderer, float> wheelWidths = new();
    Vector3 wheelPos;           // where the wheel's centre is now
    Quaternion wheelRot = Quaternion.identity;
    float wheelScale = 1f;
    bool wheelPosed;

    /// <summary>Lines of the wheel's groups switched to local space (their points kept where they are) so they move with it.</summary>
    void WheelLines()
    {
        foreach (Transform g in ExpRoot())
        {
            if (Array.IndexOf(WheelGroups, g.name) < 0) continue;
            foreach (LineRenderer l in g.GetComponentsInChildren<LineRenderer>(true))
            {
                if (!l.useWorldSpace) continue;
                Vector3[] pts = new Vector3[l.positionCount];
                l.GetPositions(pts);
                for (int i = 0; i < pts.Length; i++) pts[i] = l.transform.InverseTransformPoint(pts[i]);
                l.useWorldSpace = false;
                l.SetPositions(pts);
                wheelWidths[l] = l.widthMultiplier / Mathf.Max(.001f, wheelScale);
            }
        }
    }

    /// <summary>Pose the wheel: its centre at <paramref name="pos"/>, turned by <paramref name="rot"/>, scaled by <paramref name="scale"/>.</summary>
    void WheelPose(Vector3 pos, Quaternion rot, float scale)
    {
        WheelLines();
        foreach (Transform g in ExpRoot())
        {
            if (Array.IndexOf(WheelGroups, g.name) < 0) continue;
            g.localScale = Vector3.one * scale;
            g.rotation = rot;
            g.position = pos - rot * (e8WheelCenter * scale);
        }

        foreach (var kv in wheelWidths) if (kv.Key != null) kv.Key.widthMultiplier = kv.Value * scale;
        wheelPos = pos;
        wheelRot = rot;
        wheelScale = scale;
        wheelPosed = true;
    }

    /// <summary>Where a point of the wheel (as drawn at home) is now.</summary>
    Vector3 WheelPoint(Vector3 home) => wheelPosed ? wheelPos + wheelRot * ((home - e8WheelCenter) * wheelScale) : home;

    IEnumerator WheelMoveRoutine(Vector3 pos, Quaternion rot, float scale, float seconds)
    {
        if (!wheelPosed) { wheelPos = e8WheelCenter; wheelRot = Quaternion.identity; wheelScale = 1f; }
        Vector3 p0 = wheelPos;
        Quaternion r0 = wheelRot;
        float s0 = wheelScale, time = 0f;
        // the scale eases geometrically, so the shrinking wheel never seems to rush
        while (time < seconds)
        {
            float u = Mathf.SmoothStep(0f, 1f, time / seconds);
            WheelPose(Vector3.Lerp(p0, pos, u), Quaternion.Slerp(r0, rot, u), s0 * Mathf.Pow(scale / s0, u));
            time += Time.deltaTime;
            yield return null;
        }

        WheelPose(pos, rot, scale);
    }

    /// <summary>
    /// Lift the lit block of spoke k onto E8's wheel without turning a single arrow: a copy of the block flies to the wheel
    /// and grows (each arrow keeps the direction it has on the path), while the wheel turns until its hour 0 points the
    /// way the arrow of n = c would point (−t·log c). Seen on the turned clock, the arrow of n = c + h points at hour k·h,
    /// so the chain of eight ends on the wheel's 0–15 diameter at tr Cᵏ.
    /// </summary>
    public void ExpTraceOverlay(int k, float seconds)
    {
        if (!tbBlocks.TryGetValue(k, out TbBlock b) || !e8WheelReady) return;
        if (ExpRoot().Find("TB scale") == null) ExpTraceScale(e8WheelLabel * .2f);
        StartCoroutine(TraceOverlayRoutine(b, seconds));
    }

    IEnumerator TraceOverlayRoutine(TbBlock b, float seconds)
    {
        Transform old = ExpRoot().Find($"TB chain {b.K}");
        if (old != null) Destroy(old.gameObject);
        Transform g = ExpGroup($"TB chain {b.K}").transform;
        double phi0 = -b.T * Math.Log(b.C);
        Quaternion rot = Quaternion.AngleAxis((float)(phi0 * Mathf.Rad2Deg) - 90f, Vector3.forward);
        if (Quaternion.Dot(rot, wheelPosed ? wheelRot : Quaternion.identity) < 0f) rot = new Quaternion(-rot.x, -rot.y, -rot.z, -rot.w);
        // the chain on the wheel: the block's own directions, each arrow an eighth of the wheel's radius
        Vector3 c = e8WheelCenter + Vector3.back * .6f;
        float unit = e8HourRadius / 8f;
        Vector3[] q = new Vector3[9];
        q[0] = c;
        for (int i = 0; i < 8; i++) q[i + 1] = q[i] + new Vector3((float)Math.Cos(b.Dir[i]), (float)Math.Sin(b.Dir[i]), 0f) * unit;
        LineRenderer[] flying = new LineRenderer[8];
        float w0 = b.Lines[0] != null ? b.Lines[0].widthMultiplier : .1f, w1 = unit * .3f;
        for (int i = 0; i < 8; i++)
        {
            flying[i] = ExpLine(g, $"arrow {b.N[i]}", w0, ZsHourColor((int)(b.N[i] % 30)), Factory.GlowIntensity, new[] { b.A[i], b.B[i] });
            OnTop(flying[i], 21);
        }

        // the wheel turns while the copy flies
        float t1 = seconds * .7f;
        StartCoroutine(WheelMoveRoutine(e8WheelCenter, rot, 1f, t1));
        float time = 0f;
        while (time < t1)
        {
            if (g == null) yield break;
            for (int i = 0; i < 8; i++)
            {
                if (flying[i] == null) yield break;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((time / t1 - i * .04f) / .7f));
                flying[i].SetPositions(new[] { Vector3.Lerp(b.A[i], q[i], u), Vector3.Lerp(b.B[i], q[i + 1], u) });
                flying[i].widthMultiplier = Mathf.Lerp(w0, w1, u);
            }

            time += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < 8; i++)
        {
            if (flying[i] == null) continue;
            flying[i].SetPositions(new[] { q[i], q[i + 1] });
            flying[i].widthMultiplier = w1;
            Color col = ZsHourColor((int)(b.N[i] % 30));
            ArrowHead(g, $"head {b.N[i]}", q[i], q[i + 1], unit * .34f, col, unit * .1f);
            if (i > 0) ExpDot(g, $"joint {i}", q[i] + Vector3.back * .15f, unit * .2f, Color.white, Factory.GlowIntensity);
        }

        // the hours the arrows point at on the turned clock, each ringed with its arm
        HashSet<int> marked = new();
        foreach (int h in ZeroSums.Gold) marked.Add((int)(((long)b.K * h) % 30));
        foreach (int hour in marked)
        {
            Vector3 rim = WheelPoint(E8Hour(hour, e8HourRadius * 1.08f)) + Vector3.back * .6f;
            RingAround(g, $"hour {hour}", rim, Vector3.back, unit * .35f, Color.white * .9f, unit * .05f, AttentionIntensity);
            LineRenderer arm = ExpLine(g, $"arm {hour}", unit * .14f, new Color(1f, 1f, 1f, .75f), Factory.SoftGlowIntensity,
                new[] { c + Vector3.back * .05f, rim });
            OnTop(arm, 19);
            if (marked.Count > 2) continue;
            TextBox tag = ExpLabelWide(g, $"hour {hour}", WheelPoint(E8Hour(hour, e8HourRadius * 1.08f + unit * 1.4f)) + Vector3.back * .7f, unit * 16f, Color.white);
            Outline(tag.TextField);
        }

        // the sum, from the centre along the turned 0–15 diameter
        int tr = Trace30(b.K);
        ExpDot(g, "sum", q[8] + Vector3.back * .2f, unit * .45f, Color.white, AttentionIntensity);
        Flare(q[8] + Vector3.back * .3f, unit * 1.4f, 1.2f, 2);
        LineRenderer sumArrow = ExpLine(g, "sum arrow", unit * .16f, Color.white, AttentionIntensity, new[] { c, q[8] });
        OnTop(sumArrow, 22);
        StartCoroutine(SettleRoutine(sumArrow, tr < 0 ? new Color(.35f, .8f, 1f) : PrimeGold, 1f));
    }

    /// <summary>E8's wheel back to its own place, turned back and full size.</summary>
    public void ExpWheelHome(float seconds)
    {
        if (!wheelPosed) return;
        StartCoroutine(WheelMoveRoutine(e8WheelCenter, Quaternion.identity, 1f, seconds));
    }

    /// <summary>The trace scale on the clock's vertical diameter (−8 at hour 15, +8 at hour 0), with ticks and numbers.</summary>
    public void ExpTraceScale(float labelSize)
    {
        if (!e8WheelReady) return;
        Transform g = ExpGroup("TB scale").transform;
        float unit = e8HourRadius / 8f;
        Vector3 c = e8WheelCenter + Vector3.back * .5f;
        List<(Vector3, Vector3)> ticks = new() { (c + Vector3.down * unit * 8.4f, c + Vector3.up * unit * 8.4f) };
        foreach (int v in new[] { -8, -4, -2, -1, 1, 2, 4, 8 })
        {
            ticks.Add((c + new Vector3(-unit * .3f, v * unit, 0f), c + new Vector3(unit * .3f, v * unit, 0f)));
            TextBox l = ExpLabelWide(g, v.ToString().Replace("-", "−"), c + new Vector3(-unit * .55f, v * unit, -.2f), labelSize, new Color(.75f, .8f, .95f), TextAlignmentOptions.Right);
            Outline(l.TextField);
        }

        Segments(g, "scale", ticks, unit * .06f, new Color(.7f, .75f, .95f) * .8f, Factory.SoftGlowIntensity);
    }

    /// <summary>The chart of traces: bars for k = 1 … 30 at <paramref name="o"/> (filled by the sweep, or all at once).</summary>
    public void ExpTraceBars(Vector3 o, float labelSize)
    {
        tbBarsOrigin = o;
        tbBarsReady = true;
        tbBars.Clear();
        Transform g = ExpGroup("TB bars").transform;
        List<(Vector3, Vector3)> axis = new() { (o + Vector3.left * .6f, o + Vector3.right * (30.6f * tbBarDx)) };
        foreach (int v in new[] { -8, -4, 4, 8 })
        {
            axis.Add((o + new Vector3(-.6f, v * tbBarUnit, 0f), o + new Vector3(-.2f, v * tbBarUnit, 0f)));
            TextBox l = ExpLabelWide(g, v.ToString().Replace("-", "−"), o + new Vector3(-.9f, v * tbBarUnit, 0f), labelSize, new Color(.7f, .74f, .9f), TextAlignmentOptions.Right);
            Outline(l.TextField);
        }

        foreach (int k in new[] { 1, 5, 10, 15, 20, 25, 30 })
        {
            TextBox l = ExpLabelWide(g, k.ToString(), o + new Vector3((k - .5f) * tbBarDx, -9.4f * tbBarUnit, 0f), labelSize, new Color(.7f, .74f, .9f));
            Outline(l.TextField);
        }

        TextBox name = ExpLabelWide(g, "k", o + new Vector3(31.4f * tbBarDx, -.2f, 0f), labelSize * 1.1f, new Color(.8f, .84f, .96f), TextAlignmentOptions.Left);
        Outline(name.TextField);
        Segments(g, "axis", axis, .12f, new Color(.6f, .64f, .8f) * .8f, Factory.NoGlow);
    }

    LineRenderer TraceBar(int k, bool flare)
    {
        if (!tbBarsReady || tbBars.ContainsKey(k)) return null;
        Transform g = ExpRoot().Find("TB bars");
        if (g == null) return null;
        int tr = Trace30(k);
        float x = (k - .5f) * tbBarDx;
        Color col = TraceColour(k);
        LineRenderer bar = ExpLine(g, $"bar {k}", tbBarDx * .7f, Color.white, AttentionIntensity, new[] { tbBarsOrigin + new Vector3(x, 0f, -.1f), tbBarsOrigin + new Vector3(x, tr * tbBarUnit, -.1f) });
        StartCoroutine(SettleRoutine(bar, col, .9f));
        tbBars[k] = bar;
        if (flare) Flare(tbBarsOrigin + new Vector3(x, tr * tbBarUnit, -.3f), tbBarDx * 1.4f, .8f, 1);
        return bar;
    }

    static Color TraceColour(int k) => Gcd30(k) switch
    {
        1 => new Color(.35f, .8f, 1f),
        2 => new Color(.4f, 1f, .55f),
        3 => new Color(1f, .85f, .35f),
        5 => new Color(1f, .55f, .3f),
        6 => new Color(.75f, .55f, 1f),
        10 => new Color(1f, .45f, .75f),
        15 => new Color(.3f, 1f, .95f),
        _ => Color.white,
    };

    /// <summary>Fill every bar at once; with measured, add the charges measured on the path at t = 5000 beside them.</summary>
    public void ExpTraceBarsAll(bool measured)
    {
        for (int k = 1; k <= 30; k++) TraceBar(k, false);
        if (!measured) return;
        Transform g = ExpGroup("TB measured").transform;
        for (int k = 1; k <= 30; k++)
        {
            float x = (k - .5f) * tbBarDx + tbBarDx * .28f, v = LieGutsData.SpokeChargeRe[k - 1];
            ExpLine(g, $"measured {k}", tbBarDx * .22f, Color.white * .95f, Factory.GlowIntensity,
                new[] { tbBarsOrigin + new Vector3(x, 0f, -.2f), tbBarsOrigin + new Vector3(x, v * tbBarUnit, -.2f) });
        }
    }

    /// <summary>The chain of the eight gold hours with k sliding from k0 to k1 (a live scene), dropping a bar at each whole k.</summary>
    public void ExpTraceSweep(float k0, float k1, float seconds)
    {
        if (!e8WheelReady) return;
        StartLive(TraceSweepRoutine(k0, k1, seconds));
    }

    IEnumerator TraceSweepRoutine(float k0, float k1, float seconds)
    {
        Transform g = LiveRoot();
        Vector3 c = e8WheelCenter + Vector3.back * .7f;
        float unit = e8HourRadius / 8f;
        LineRenderer[] arrows = new LineRenderer[8];
        for (int i = 0; i < 8; i++)
        {
            arrows[i] = ExpLine(g, $"arrow {i}", unit * .3f, ZsHourColor(((TbOffsets[i] % 30) + 30) % 30), Factory.GlowIntensity, new[] { c, c });
            OnTop(arrows[i], 21);
        }

        LineRenderer sum = ExpLine(g, "sum", unit * .16f, Color.white, AttentionIntensity, new[] { c, c });
        OnTop(sum, 22);
        GameObject dot = ExpDot(g, "end", c, unit * .45f, Color.white, AttentionIntensity);
        // the continuous curve of Σ cos(2πκh/30) traced on the chart as κ runs
        LineRenderer curve = tbBarsReady ? ExpLine(g, "curve", .2f, new Color(.8f, .85f, 1f) * .8f, Factory.SoftGlowIntensity, new[] { tbBarsOrigin, tbBarsOrigin }) : null;
        List<Vector3> curvePts = new();
        int lastWhole = Mathf.FloorToInt(k0) - 1;
        float time = 0f;
        while (true)
        {
            if (dot == null) yield break;
            float u = Mathf.Clamp01(time / seconds);
            double kappa = k0 + (k1 - k0) * u;
            Vector3 p = c;
            for (int i = 0; i < 8; i++)
            {
                double a = Math.PI / 2 - 2 * Math.PI * kappa * TbOffsets[i] / 30.0;
                Vector3 next = p + ClockDir(a) * unit;
                arrows[i].SetPositions(new[] { p, next });
                p = next;
            }

            sum.SetPositions(new[] { c, p });
            dot.transform.position = p + Vector3.back * .1f;
            if (curve != null)
            {
                curvePts.Add(tbBarsOrigin + new Vector3((float)(kappa - .5) * tbBarDx, (float)TraceAt(kappa) * tbBarUnit, -.15f));
                curve.positionCount = curvePts.Count;
                curve.SetPositions(curvePts.ToArray());
            }

            int whole = (int)Math.Floor(kappa + 1e-9);
            for (int k = lastWhole + 1; k <= whole; k++)
                if (k >= 1 && k <= 30)
                {
                    // the sum arrow, from the clock's centre up or down its diameter, flies to the chart and becomes bar k
                    StartCoroutine(FlyBarRoutine(k, c, p, .7f));
                    Flare(p + Vector3.back * .2f, unit * 1.2f, .7f, 1);
                    Board($"k = {k}:  tr C<sup>{k}</sup> = {Trace30(k)}".Replace("-", "−") + "\n" + Small($"the eight hours times {k}"));
                }

            lastWhole = Math.Max(lastWhole, whole);
            if (u >= 1f) break;
            time += Time.deltaTime;
            yield return null;
        }

        liveRoutine = null;
    }

    /// <summary>A copy of the sum arrow (a to b on the clock) flies along an arc to the chart and settles as bar k.</summary>
    IEnumerator FlyBarRoutine(int k, Vector3 a, Vector3 b, float seconds)
    {
        if (!tbBarsReady) { TraceBar(k, true); yield break; }
        Transform g = ExpRoot().Find("TB bars");
        if (g == null) yield break;
        float x = (k - .5f) * tbBarDx;
        Vector3 a1 = tbBarsOrigin + new Vector3(x, 0f, -.12f), b1 = tbBarsOrigin + new Vector3(x, Trace30(k) * tbBarUnit, -.12f);
        float w0 = e8HourRadius / 8f * .16f, w1 = tbBarDx * .7f;
        LineRenderer fly = ExpLine(g, $"flying {k}", w0, Color.white, AttentionIntensity, new[] { a, b });
        OnTop(fly, 24);
        float lift = Vector3.Distance(a, a1) * .25f, time = 0f;
        while (time < seconds)
        {
            if (fly == null) yield break;
            float u = Mathf.SmoothStep(0f, 1f, time / seconds);
            Vector3 hop = Vector3.up * (lift * 4f * u * (1f - u));
            fly.SetPositions(new[] { Vector3.Lerp(a, a1, u) + hop, Vector3.Lerp(b, b1, u) + hop });
            fly.widthMultiplier = Mathf.Lerp(w0, w1, u);
            time += Time.deltaTime;
            yield return null;
        }

        if (fly != null) Destroy(fly.gameObject);
        TraceBar(k, true);
    }

    /// <summary>The thirty traces, each turned by hour n, chained from the clock's centre: (1/30) Σ tr Cᵏ e^(−2πikn/30), turned to hour n.</summary>
    public void ExpTraceSieve(int n, float seconds)
    {
        if (!e8WheelReady) return;
        StartCoroutine(TraceSieveRoutine(n, seconds));
    }

    IEnumerator TraceSieveRoutine(int n, float seconds)
    {
        Transform g = ExpGroup($"TB sieve {n}").transform;
        Vector3 c = e8WheelCenter + Vector3.back * .8f;
        double rot = Math.PI / 2 - 2 * Math.PI * n / 30.0;
        Vector3[] q = new Vector3[31];
        q[0] = c;
        for (int k = 1; k <= 30; k++)
        {
            double mag = Trace30(k) / 30.0, a = -2 * Math.PI * k * n / 30.0 + rot;
            q[k] = q[k - 1] + new Vector3((float)(mag * Math.Cos(a)), (float)(mag * Math.Sin(a)), 0f) * e8HourRadius;
        }

        LineRenderer chain = ExpLine(g, "chain", e8HourRadius * .035f, Color.white, Factory.GlowIntensity, new[] { c, c });
        OnTop(chain, 23);
        float per = seconds * .85f / 30f;
        for (int k = 1; k <= 30; k++)
        {
            float time = 0f;
            while (time < per)
            {
                if (chain == null) yield break;
                Vector3[] part = new Vector3[k + 1];
                Array.Copy(q, part, k);
                part[k] = Vector3.Lerp(q[k - 1], q[k], Mathf.SmoothStep(0f, 1f, time / per));
                chain.positionCount = part.Length;
                chain.SetPositions(part);
                time += Time.deltaTime;
                yield return null;
            }
        }

        if (chain == null) yield break;
        chain.positionCount = 31;
        chain.SetPositions(q);
        bool gold = GoldHour(n);
        ExpDot(g, "end", q[30] + Vector3.back * .2f, e8HourRadius * .05f, gold ? PrimeGold : new Color(1f, .45f, .4f), AttentionIntensity);
        Flare(q[30] + Vector3.back * .3f, e8HourRadius * .15f, 1.2f, 2);
        StartCoroutine(SettleRoutine(chain, gold ? PrimeGold : new Color(1f, .45f, .4f), 1f));
    }
}
