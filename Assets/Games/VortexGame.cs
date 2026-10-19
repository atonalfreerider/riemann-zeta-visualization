using System.Collections.Generic;
using Meta.Numerics;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Zeta's chain, to play with: the path of partial sums Σ n^(−½ − it), laid flat, its end at ζ(½ + it). Move t up and down
/// and watch the chain wind and unwind: drag up or down anywhere (one finger on a touch screen, or the mouse; Ctrl for fine
/// steps), pinch the bead on the t rail in VR and lift or lower it, use ← → (Shift: fine), or glide to the previous or the
/// next zero of ζ. The rail's gold ticks are the zeros near t. At a zero the chain's end lands on 0 and the chain curls
/// into vortices; vortex k belongs to the counting number k (its eye is the chain's own k-th vertex reflected through ζ's
/// phase, ζ − e^(−2iθ) conj(S_k)) and is labelled k in k's colour. Touch a vortex or a vertex to see k's fingerprint, the
/// mirror line from the vertex to the eye, and where k sits on E8's clock (hour k mod 30) and on the Sacks spiral.
/// </summary>
public class VortexGame : GameModel
{
    const float S = 26f;                                   // world units per unit of the complex plane
    const int KMax = 7;
    const float RailLength = 46f, RailSpan = 8f;           // the rail: 8 units of t over its length, centred on t when held
    const double TPerPixel = .004;

    readonly MarbleBatch marbles = new(1);
    LineRenderer chain, tail, mirrorLine, landRing;
    readonly List<LineRenderer> eyeRings = new();
    readonly List<TextBox> eyeLabels = new();
    readonly List<LineRenderer> ticks = new();
    TextBox railLabel;
    readonly List<SacksSpiral.Vortex> vortices = new();
    Complex[] sums;
    Complex zeta;
    double t;
    int shown = -1;                                        // the number whose vortex was touched last
    double railCentre;                                     // the t at the rail's middle (re-centred whenever it is let go)
    bool railHeld;
    double steerFrom, steerTo;
    float steerStart = -1f, steerSeconds = 1f;
    double railFrom;
    float heightFrom;

    public override string Id => "vortex";
    public override string Title => "Zeta's chain";
    public override string Short => "Chain";
    public override VRIcons.Id Icon => VRIcons.Id.Chain;

    Vector3 Plane(Complex z) => O + new Vector3((float)z.Re, (float)z.Im, 0f) * S;
    Vector3 MapE8 => O + new Vector3(84f, 22f, 0f);
    Vector3 MapSacks => O + new Vector3(84f, -36f, 0f);
    Vector3 RailBase => O + new Vector3(-74f, -30f, 0f);
    /// <summary>
    /// The rail's direction: up the screen on the desktop (the chain's plane faces the camera), straight up in VR, where it
    /// stands out of the table-like plane so that lifting a pinched hand lifts t.
    /// </summary>
    Vector3 RailDir => VRMode.Enabled ? Up : Vector3.up;

    double[] AllZeros => Main.Instance != null && Main.Instance.zetaZeroValues != null && Main.Instance.zetaZeroValues.Length >= 1000
        ? Main.Instance.zetaZeroValues : null;

    int ZeroCount => AllZeros != null ? System.Math.Min(AllZeros.Length, 1000) : 1000;

    double ZeroT(int j)
    {
        if (AllZeros != null) return AllZeros[Mathf.Clamp(j, 1, AllZeros.Length) - 1];
        // without the table: the three zeros the old rounds used, and Gram's estimate between them
        return j switch { 1 => 14.1347251417, 100 => 236.5242296658, 300 => 541.8474371212, 1000 => 1419.4224809459, _ => Gram(j) };
    }

    /// <summary>A rough t of zero j (where the zero count first passes j): enough for the rail without the table.</summary>
    static double Gram(int j)
    {
        double x = 2 * System.Math.PI * System.Math.Max(1, j) / System.Math.Log(System.Math.Max(3, j));
        for (int i = 0; i < 20; i++)
        {
            double n = x / (2 * System.Math.PI) * System.Math.Log(x / (2 * System.Math.PI * System.Math.E)) + 7.0 / 8.0;
            x += (j - .5 - n) * 2 * System.Math.PI / System.Math.Log(x / (2 * System.Math.PI));
        }

        return x;
    }

    double TMin => ZeroT(1) - 4.0;
    double TMax => ZeroT(ZeroCount) + 2.0;

    /// <summary>The zero nearest x (1-based), by bisection over the sorted zeros.</summary>
    int NearestZero(double x)
    {
        int lo = 1, hi = ZeroCount;
        while (hi - lo > 1)
        {
            int mid = (lo + hi) / 2;
            if (ZeroT(mid) < x) lo = mid;
            else hi = mid;
        }

        return System.Math.Abs(ZeroT(lo) - x) <= System.Math.Abs(ZeroT(hi) - x) ? lo : hi;
    }

    protected override void OnBuild()
    {
        chain = LabDraw.Line(Root, "chain", .32f, new Color(.92f, .94f, 1f), Factory.SoftGlowIntensity, new[] { O, O });
        tail = LabDraw.Line(Root, "tail", .3f, new Color(1f, .55f, .2f), Factory.GlowIntensity, new[] { O, O });
        mirrorLine = LabDraw.Line(Root, "mirror", .22f, NumberKind.Gold, Factory.GlowIntensity, new[] { O, O });
        mirrorLine.gameObject.SetActive(false);
        landRing = LabDraw.Ring(Root, "on a zero", O, 3.2f, .4f, NumberKind.Gold, Factory.GlowIntensity, 48);
        landRing.gameObject.SetActive(false);
        LabDraw.Line(Root, "t rail", .5f, new Color(.6f, .65f, .85f), Factory.SoftGlowIntensity, new[] { RailBase, RailBase + RailDir * RailLength });
        railLabel = LabDraw.Label(Root, "t", RailBase + RailDir * (RailLength + 4f), 34f, Color.white);
        LabDraw.Label(Root, "E8's clock", MapE8 + new Vector3(0f, 25f, 0f), 30f, new Color(.75f, .78f, .92f));
        LabDraw.Label(Root, "the Sacks spiral", MapSacks + new Vector3(0f, 25f, 0f), 30f, new Color(.75f, .78f, .92f));
        for (int k = 1; k <= KMax; k++)
        {
            LineRenderer r = LabDraw.Ring(Root, $"eye {k}", O, 3f, .4f, Color.white, Factory.GlowIntensity, 40);
            r.gameObject.SetActive(false);
            eyeRings.Add(r);
            TextBox l = LabDraw.Label(Root, k.ToString(), O, 46f, NumberKind.ColorOf(k));
            l.gameObject.SetActive(false);
            eyeLabels.Add(l);
        }

        Controls.Add(LabControl.Button("◀ zero", VRIcons.Id.PrevLine, () => Jump(-1), ","));
        Controls.Add(LabControl.Button("zero ▶", VRIcons.Id.NextLine, () => Jump(1), "."));
        Controls.Add(LabControl.SliderOf("t", 0f, 1f, () => (float)((t - TMin) / (TMax - TMin)), v => { steerStart = -1f; SetT(TMin + (TMax - TMin) * v); },
            v => $"t = {TMin + (TMax - TMin) * v:0.0}", 3, "← →"));
        AddCommonControls();
        Prompt = "Drag up or down to move t:  at each zero the chain's end lands on 0";
        SetT(ZeroT(300));
    }

    // the chain a little low, clear of the board's header (it mostly winds up and to the left of 0)
    public override LabView Home => StoryFramed(new LabView { Focus = O + new Vector3(4f, 12f, 0f) + Up * 2f, Distance = 210f, Yaw = -90f, Pitch = 76f, Fov = 50f }, 1.25f, .05f);

    public override void OnEnter() => UpdateStatus();

    void SetT(double nt)
    {
        t = System.Math.Clamp(nt, TMin, TMax);
        int n = (int)(t / System.Math.PI) + 12;
        sums = SacksSpiral.ChainSums(t, n);
        // the series to n plus its Euler–Maclaurin tail (ZetaMath.Exact does not converge this high up)
        zeta = ZetaMath.Decompose(new Complex(.5, t), n + 1).Total;
        List<Vector3> pts = new(n + 1);
        for (int i = 0; i <= n; i++) pts.Add(Plane(sums[i]));
        LabDraw.SetLine(chain, pts);
        tail.SetPositions(new[] { Plane(sums[n]), Plane(zeta) });
        vortices.Clear();
        foreach (SacksSpiral.Vortex v in SacksSpiral.DetectVortices(t, NearestZero(t), KMax, zeta))
            if (v.K >= 1 && v.K <= KMax && v.Certainty > .3f) vortices.Add(v);
        vortices.Sort((a, b) => a.K.CompareTo(b.K));
        if (!railHeld) railCentre = t;
        Rebuild();
    }

    /// <summary>The eyes (labelled with their numbers), the vertices, the rail's bead and ticks, the readout.</summary>
    void Rebuild()
    {
        Picks.Clear();
        for (int k = 1; k <= KMax; k++)
        {
            int at = vortices.FindIndex(x => x.K == k);
            bool on = at >= 0;
            eyeRings[k - 1].gameObject.SetActive(on);
            eyeLabels[k - 1].gameObject.SetActive(on);
            if (on)
            {
                Vector3 e = Plane(vortices[at].Mirror);
                Color c = NumberKind.ColorOf(k);
                LabDraw.SetRing(eyeRings[k - 1], e, 3f + .15f * k, 40);
                eyeRings[k - 1].startColor = eyeRings[k - 1].endColor = new Color(c.r, c.g, c.b, k == shown ? 1f : .7f);
                eyeLabels[k - 1].transform.position = e + new Vector3(0f, 6f, 0f) + Up * .5f;
                Picks.Add(new Pickable { Position = e + Up * .3f, Value = k, Radius = 3.2f, Kind = "vortex" });
            }

            if (k < sums.Length) Picks.Add(new Pickable { Position = Plane(sums[k]) + Up * .3f, Value = k, Radius = 2f, Kind = "vertex" });
        }

        int si = shown > 0 ? vortices.FindIndex(x => x.K == shown) : -1;
        mirrorLine.gameObject.SetActive(si >= 0 && shown < sums.Length);
        if (si >= 0 && shown < sums.Length) mirrorLine.SetPositions(new[] { Plane(sums[shown]) + Up * .2f, Plane(vortices[si].Mirror) + Up * .2f });

        // on a zero: the chain's end is on 0
        double mag = System.Math.Sqrt(zeta.Re * zeta.Re + zeta.Im * zeta.Im);
        int z = NearestZero(t);
        landRing.gameObject.SetActive(mag < .02);
        if (mag < .02) LabDraw.SetRing(landRing, O + Up * .2f, 3.2f, 48);
        RebuildRail();
        Note = mag < .02
            ? $"t = {t:0.0000}:  on zero {z}, ζ(½ + it) = 0.  {vortices.Count} vortices, each a counting number's"
            : $"t = {t:0.0000}   |ζ(½ + it)| = {mag:0.000}   the nearest zero is number {z}, at t = {ZeroT(z):0.000}";
        if (shown > 0) Note += $"\nvortex {shown}:  {NumberKind.Describe(shown)}, on E8's hour {shown % 30}";
        UpdateStatus();
    }

    /// <summary>The rail spans RailSpan of t about its centre; the bead is t, the gold ticks the zeros in reach.</summary>
    void RebuildRail()
    {
        double lo = railCentre - RailSpan * .5, hi = railCentre + RailSpan * .5;
        int tick = 0;
        for (int j = System.Math.Max(1, NearestZero(lo) - 1); j <= System.Math.Min(ZeroCount, NearestZero(hi) + 1) && tick < 24; j++)
        {
            double zt = ZeroT(j);
            if (zt < lo || zt > hi) continue;
            if (tick >= ticks.Count) ticks.Add(LabDraw.Line(Root, "zero tick", .45f, NumberKind.Gold, Factory.GlowIntensity, new[] { O, O }));
            Vector3 at = RailBase + RailDir * (float)((zt - lo) / RailSpan * RailLength);
            ticks[tick].gameObject.SetActive(true);
            ticks[tick].SetPositions(new[] { at + Vector3.left * 3f, at + Vector3.right * 3f });
            tick++;
        }

        for (int i = tick; i < ticks.Count; i++) ticks[i].gameObject.SetActive(false);
        railLabel.transform.position = Bead + Vector3.left * 11f;
        railLabel.TextField.text = $"t = {t:0.00}";
    }

    Vector3 Bead => RailBase + RailDir * (float)((t - (railCentre - RailSpan * .5)) / RailSpan * RailLength);

    protected override string LabelFor(Pickable p) => p.Kind == "vortex" ? $"vortex {p.Value}" : $"vertex {p.Value}";

    public override void OnPick(int i)
    {
        Pickable p = Picks[i];
        Show((int)p.Value, p.Position);
    }

    /// <summary>Light number k: its fingerprint, the mirror line from its vertex to its vortex's eye, its places on the maps.</summary>
    void Show(int k, Vector3 at)
    {
        shown = k;
        Light(at, k, 3f);
        ShowPrint(k, O + new Vector3(-124f, 14f, 0f) + Up * 4f, 3.2f);
        Rebuild();
    }

    /// <summary>Glide to the previous (−1) or the next (+1) zero of ζ.</summary>
    void Jump(int dir)
    {
        int z = NearestZero(t);
        double zt = ZeroT(z);
        // already on (or past) the nearest zero in that direction: the one after it
        if (dir > 0 && zt <= t + 1e-6) z++;
        if (dir < 0 && zt >= t - 1e-6) z--;
        z = Mathf.Clamp(z, 1, ZeroCount);
        Steer(ZeroT(z), .9f);
    }

    void Steer(double to, float seconds)
    {
        steerFrom = t;
        steerTo = to;
        steerSeconds = Mathf.Max(.1f, seconds);
        steerStart = Time.time;
    }

    // ---- t by hand ------------------------------------------------------------------------------------------

    public override bool DragBegin(Vector2 screen)
    {
        if (base.DragBegin(screen)) return true;     // the fingerprint, pulled apart
        steerStart = -1f;
        return true;                                 // anywhere else: t
    }

    public override void DragMove(Vector2 screen, Vector2 delta)
    {
        if (Scrubbing) { base.DragMove(screen, delta); return; }
        SetT(t + delta.y * TPerPixel);
    }

    /// <summary>VR: a pinch near the rail's bead holds it (else, as in every game, the fingerprint).</summary>
    public override bool WantsPinch(Vector3 world)
    {
        if (base.WantsPinch(world)) return true;
        // the bead and a little of the rail either side of it, in the model's own units (it scales with the model)
        Vector3 d = world - Bead;
        float along = Vector3.Dot(d, RailDir), across = (d - RailDir * along).magnitude;
        return Mathf.Abs(along) < 7f && across < 6f;
    }

    public override void PinchBegin(Vector3 world)
    {
        if (base.WantsPinch(world)) { base.PinchBegin(world); return; }
        railHeld = true;
        steerStart = -1f;
        railFrom = t;
        heightFrom = Vector3.Dot(world - RailBase, RailDir);
    }

    public override void PinchMove(Vector3 world)
    {
        if (!railHeld) { base.PinchMove(world); return; }
        float h = Vector3.Dot(world - RailBase, RailDir);
        SetT(railFrom + (h - heightFrom) / RailLength * RailSpan);
    }

    public override void PinchEnd()
    {
        if (!railHeld) { base.PinchEnd(); return; }
        railHeld = false;
        railCentre = t;
        RebuildRail();
    }

    protected override void OnTick(float dt)
    {
        if (steerStart >= 0f)
        {
            float u = Mathf.Clamp01((Time.time - steerStart) / steerSeconds);
            SetT(steerFrom + (steerTo - steerFrom) * Mathf.SmoothStep(0f, 1f, u));
            if (u >= 1f) steerStart = -1f;
        }

        marbles.Clear();
        // ζ itself, the vertices 1 … KMax in their numbers' colours, the rail's bead, and the maps
        marbles.Add(Plane(zeta), 2.2f, NumberKind.Gold, 1.3f);
        for (int k = 1; k <= KMax && k < sums.Length; k++)
            marbles.Add(Plane(sums[k]), k == shown ? 2.1f : 1.4f, NumberKind.ColorOf(k), k == shown ? 1.3f : 1.05f);
        marbles.Add(Bead, railHeld ? 4.2f : 3.4f, Color.white, railHeld ? 1.5f : 1.25f);
        DrawMaps();
        marbles.Draw(Matrix4x4.identity);
    }

    /// <summary>The small maps: n ≤ 60 on E8's clock (hour n mod 30) and on the Sacks spiral, the touched number large.</summary>
    void DrawMaps()
    {
        for (int n = 1; n <= 60; n++)
        {
            bool lit = n == shown || vortices.Exists(v => v.K == n);
            Color c = NumberKind.ColorOf(n);
            if (!lit) c = Color.Lerp(c, new Color(.12f, .13f, .17f), .45f);
            float size = n == shown ? 2.8f : lit ? 1.8f : 1.1f;
            float hour = (n % 30) / 30f * Mathf.PI * 2f, r = 6f + 7f * (n / 30f);
            marbles.Add(MapE8 + new Vector3(Mathf.Sin(hour), Mathf.Cos(hour), 0f) * r, size, c, lit ? 1.35f : 1f);
            float a = Mathf.PI * 2f * Mathf.Sqrt(n), rs = 2.6f * Mathf.Sqrt(n);
            marbles.Add(MapSacks + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * rs, size, c, lit ? 1.35f : 1f);
        }
    }

    public override bool OnKey(Keyboard k)
    {
        if (k.leftArrowKey.isPressed || k.rightArrowKey.isPressed)
        {
            steerStart = -1f;
            double step = (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed ? .0005 : .004) * (k.leftArrowKey.isPressed ? -1 : 1);
            SetT(t + step);
            return true;
        }

        if (k.commaKey.wasPressedThisFrame) { Jump(-1); return true; }
        if (k.periodKey.wasPressedThisFrame) { Jump(1); return true; }
        return CommonKey(k);
    }

    public override bool StoryCommand(string verb, string[] a)
    {
        double Num(int i, double d) => a.Length > i && double.TryParse(a[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : d;
        switch (verb)
        {
            // zero:J[:SECONDS] — glide to zero J; steer:DT[:SECONDS] — glide t by DT, as a drag up or down would;
            // jump:±1 — the previous or the next zero
            case "zero": Steer(ZeroT((int)Num(0, 300)), (float)Num(1, 1.5)); return true;
            case "steer": Steer(t + Num(0, 0), (float)Num(1, 3)); return true;
            case "jump": Jump(Num(0, 1) < 0 ? -1 : 1); return true;
        }

        return base.StoryCommand(verb, a);
    }

    // for tests
    public int VortexCount => vortices.Count;
    public double T => t;
    public void SteerTo(double nt) { steerStart = -1f; SetT(nt); }
    public double ZeroAt(int j) => ZeroT(j);
    public int Nearest => NearestZero(t);
    public Vector3 BeadPoint => Bead;
}
