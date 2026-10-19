using System.Collections.Generic;
using Meta.Numerics;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Vortex hunt on zeta's chain (the path of partial sums Σ n^(−½ − it), laid flat, its end at ζ(½ + it)). At a zero the
/// chain curls into vortices; vortex k belongs to the counting number k: its eye is the chain's own k-th vertex,
/// reflected through ζ's phase (ζ − e^(−2iθ) · conj(S_k)). Each vortex lights in its number's colour (gold if k is prime,
/// deep red if a prime's square, blue if pronic, grey otherwise), and two small maps show where k sits among the primes:
/// on E8's clock (hour k mod 30) and on the Sacks spiral. Three rounds, at several zeros:
/// 1. Find vortex k: point at its eye.
/// 2. Connect: point at a vortex, then at the vertex it mirrors.
/// 3. Land on a zero: steer t (the slider) until the chain's end lands on 0.
/// </summary>
public class VortexGame : GameModel
{
    static readonly int[] Zeros = { 300, 100, 1000 };      // the zeros the rounds visit (1-based)
    const float S = 26f;                                   // world units per unit of the complex plane
    const int KMax = 7;

    readonly MarbleBatch marbles = new(1);
    LineRenderer chain, tail, mirrorLine;
    readonly List<LineRenderer> eyeRings = new();
    readonly List<TextBox> eyeLabels = new();
    readonly List<SacksSpiral.Vortex> vortices = new();
    readonly HashSet<int> found = new();
    readonly List<(int k, Vector3 eye, Vector3 vertex)> links = new();
    readonly List<LineRenderer> linkLines = new();
    Complex[] sums;
    Complex zeta;
    double t;
    int zeroIndex, round, ask = 1, chosenVortex = -1;
    float nextAt = -1f;
    double landT;
    bool landed;

    public override string Id => "vortex";
    public override string Title => "Vortex hunt";
    public override string Short => "Vortices";
    public override VRIcons.Id Icon => VRIcons.Id.Chain;

    Vector3 Plane(Complex z) => O + new Vector3((float)z.Re, (float)z.Im, 0f) * S;
    Vector3 MapE8 => O + new Vector3(78f, 18f, 0f);
    Vector3 MapSacks => O + new Vector3(78f, -40f, 0f);

    protected override void OnBuild()
    {
        chain = LabDraw.Line(Root, "chain", .32f, new Color(.92f, .94f, 1f), Factory.SoftGlowIntensity, new[] { O, O });
        tail = LabDraw.Line(Root, "tail", .3f, new Color(1f, .55f, .2f), Factory.GlowIntensity, new[] { O, O });
        mirrorLine = LabDraw.Line(Root, "mirror", .22f, NumberKind.Gold, Factory.GlowIntensity, new[] { O, O });
        mirrorLine.gameObject.SetActive(false);
        LabDraw.Label(Root, "E8's clock", MapE8 + new Vector3(0f, 25f, 0f), 30f, new Color(.75f, .78f, .92f));
        LabDraw.Label(Root, "the Sacks spiral", MapSacks + new Vector3(0f, 25f, 0f), 30f, new Color(.75f, .78f, .92f));
        Controls.Add(LabControl.Button("Next round", VRIcons.Id.NextLine, () => StartRound(round + 1), "N"));
        Controls.Add(LabControl.Button("Restart", VRIcons.Id.Replay, Restart, "Backspace"));
        Controls.Add(LabControl.SliderOf("t", 0f, 1f, () => (float)SliderOf(t), v => { if (round == 2) SetT(TOf(v)); },
            v => $"t = {TOf(v):0.000}", 3, "← →"));
        AddCommonControls();
        Restart();
    }

    public override LabView Home => new() { Focus = O + new Vector3(2f, 4f, 0f) + Up * 2f, Distance = 150f, Yaw = -90f, Pitch = 76f, Fov = 50f };

    public override void OnEnter() => UpdateStatus();

    double[] AllZeros => Main.Instance != null && Main.Instance.zetaZeroValues != null && Main.Instance.zetaZeroValues.Length >= 1000
        ? Main.Instance.zetaZeroValues : null;

    double ZeroT(int j) => AllZeros != null ? AllZeros[j - 1] : j == 100 ? 236.5242296658 : j == 1000 ? 1419.4224809459 : 541.8474371212;

    // the slider spans the zero ± one mean gap
    double SliderLo => ZeroT(zeroIndex) - .8;
    double SliderHi => ZeroT(zeroIndex) + .8;
    double TOf(float v) => SliderLo + (SliderHi - SliderLo) * v;
    double SliderOf(double x) => (x - SliderLo) / (SliderHi - SliderLo);

    void Restart()
    {
        Score = Streak = BestStreak = Finds = Misses = 0;
        StartRound(0);
    }

    void StartRound(int r)
    {
        round = ((r % 3) + 3) % 3;
        zeroIndex = Zeros[round];
        found.Clear();
        links.Clear();
        foreach (LineRenderer l in linkLines) if (l != null) Object.Destroy(l.gameObject);
        linkLines.Clear();
        chosenVortex = -1;
        ask = 1;
        nextAt = -1f;
        landed = false;
        HidePrint();
        mirrorLine.gameObject.SetActive(false);
        if (round == 2)
        {
            // start away from the zero: the player steers t onto it
            landT = ZeroT(zeroIndex);
            SetT(landT + .55);
        }
        else SetT(ZeroT(zeroIndex));

        Prompt = round switch
        {
            0 => $"Round 1 of 3:  zero {zeroIndex}.  Find vortex {ask}:  vortex k belongs to the number k",
            1 => $"Round 2 of 3:  zero {zeroIndex}.  Join each vortex to the vertex it mirrors",
            _ => $"Round 3 of 3:  steer t (slider, ← →) until the chain lands on zero {zeroIndex}",
        };
        Note = Legend;
        UpdateStatus();
    }

    void SetT(double nt)
    {
        t = nt;
        int n = (int)(t / System.Math.PI) + 12;
        sums = SacksSpiral.ChainSums(t, n);
        // the series to n plus its Euler–Maclaurin tail (ZetaMath.Exact does not converge this high up)
        zeta = ZetaMath.Decompose(new Complex(.5, t), n + 1).Total;
        List<Vector3> pts = new(n + 1);
        for (int i = 0; i <= n; i++) pts.Add(Plane(sums[i]));
        LabDraw.SetLine(chain, pts);
        tail.SetPositions(new[] { Plane(sums[n]), Plane(zeta) });
        vortices.Clear();
        foreach (SacksSpiral.Vortex v in SacksSpiral.DetectVortices(t, round == 2 ? 0 : zeroIndex, KMax, zeta))
            if (v.K >= 1 && v.K <= KMax && v.Certainty > .3f) vortices.Add(v);
        vortices.Sort((a, b) => a.K.CompareTo(b.K));
        BuildPicks();
        if (round == 2) CheckLanding();
    }

    void BuildPicks()
    {
        Picks.Clear();
        foreach (LineRenderer l in eyeRings) if (l != null) Object.Destroy(l.gameObject);
        foreach (TextBox l in eyeLabels) if (l != null) Object.Destroy(l.gameObject);
        eyeRings.Clear();
        eyeLabels.Clear();
        if (round == 2) return;
        foreach (SacksSpiral.Vortex v in vortices)
        {
            Vector3 e = Plane(v.Mirror);
            Picks.Add(new Pickable { Position = e + Up * .3f, Value = v.K, Radius = 3.2f, Kind = "vortex" });
            bool lit = found.Contains(v.K);
            Color c = lit ? NumberKind.ColorOf(v.K) : new Color(.78f, .8f, .9f, .55f);
            eyeRings.Add(LabDraw.Ring(Root, $"eye {v.K}", e, 3f + .15f * v.K, lit ? .55f : .25f, c, lit ? Factory.GlowIntensity : Factory.SoftGlowIntensity, 40));
            if (lit) eyeLabels.Add(LabDraw.Label(Root, v.K.ToString(), e + new Vector3(0f, 6f, 0f) + Up * .5f, 46f, NumberKind.ColorOf(v.K)));
        }

        if (round == 1)
            for (int k = 1; k <= KMax && k < sums.Length; k++)
                Picks.Add(new Pickable { Position = Plane(sums[k]) + Up * .3f, Value = k, Radius = 2f, Kind = "vertex" });
    }

    protected override string LabelFor(Pickable p) => p.Kind switch
    {
        "vortex" => found.Contains((int)p.Value) || round == 1 ? $"vortex {p.Value}" : "a vortex",
        "vertex" => $"vertex {p.Value}",
        _ => p.Value.ToString(),
    };

    public override void OnPick(int i)
    {
        Pickable p = Picks[i];
        int k = (int)p.Value;
        if (round == 0 && p.Kind == "vortex")
        {
            if (k == ask && !found.Contains(k))
            {
                found.Add(k);
                Found(p.Position, 10, 3f);
                ShowVortex(k, p.Position);
                ask = NextAsk();
                Prompt = ask > 0 ? $"Round 1 of 3:  zero {zeroIndex}.  Find vortex {ask}" : $"Round 1 of 3:  every vortex found";
                if (ask < 0 && AutoAdvance) nextAt = Time.time + 3.5f;
            }
            else if (!found.Contains(k))
            {
                Missed(p.Position, 2, 3f);
                Note = $"that is vortex {k}, not {ask}:  vortex k's knot sits at n ≈ t/((2k + 1)π) = {t / ((2 * ask + 1) * System.Math.PI):0}";
            }
            else ShowVortex(k, p.Position);
        }
        else if (round == 1)
        {
            if (p.Kind == "vortex")
            {
                chosenVortex = k;
                Note = $"vortex {k} chosen:  now point at the vertex of the chain it mirrors";
            }
            else if (chosenVortex > 0)
            {
                Vector3 eye = Picks.Find(x => x.Kind == "vortex" && x.Value == chosenVortex).Position;
                if (k == chosenVortex)
                {
                    found.Add(k);
                    Found(p.Position, 10, 2f);
                    LineRenderer l = LabDraw.Line(Root, $"link {k}", .3f, NumberKind.ColorOf(k), Factory.GlowIntensity, new[] { p.Position, eye });
                    linkLines.Add(l);
                    ShowVortex(k, eye);
                    Note = $"vertex {k} ↔ vortex {k}:  the eye is ζ − e<sup>−2iθ</sup> conj(S<sub>{k}</sub>).  " + NumberKind.Describe(k);
                    if (found.Count >= vortices.Count && AutoAdvance) nextAt = Time.time + 3.5f;
                }
                else
                {
                    Missed(p.Position, 2, 2f);
                    Note = $"vertex {k} does not mirror into vortex {chosenVortex}";
                }

                chosenVortex = -1;
            }
        }

        BuildPicks();
        UpdateStatus();
    }

    int NextAsk()
    {
        foreach (SacksSpiral.Vortex v in vortices) if (!found.Contains(v.K)) return v.K;
        return -1;
    }

    /// <summary>Light vortex k: its number's fingerprint, and the mirror line from the k-th vertex to the eye.</summary>
    void ShowVortex(int k, Vector3 eye)
    {
        ShowPrint(k, O + new Vector3(-80f, 34f, 0f) + Up * 4f, 3.4f);
        if (k < sums.Length)
        {
            mirrorLine.gameObject.SetActive(true);
            mirrorLine.SetPositions(new[] { Plane(sums[k]) + Up * .2f, eye + Up * .2f });
            Color c = NumberKind.ColorOf(k);
            mirrorLine.startColor = mirrorLine.endColor = new Color(c.r, c.g, c.b, .8f);
        }

        Note = $"vortex {k} is vertex {k} reflected.  {NumberKind.Describe(k)}, on E8's hour {k % 30}";
    }

    void CheckLanding()
    {
        float d = (float)System.Math.Sqrt(zeta.Re * zeta.Re + zeta.Im * zeta.Im);
        Note = $"|ζ(½ + it)| = {d:0.0000}   (t = {t:0.0000})";
        if (!landed && d < .02f)
        {
            landed = true;
            Found(Plane(zeta), 25, 3f);
            Note = $"Landed on zero {zeroIndex}:  t = {t:0.0000}, |ζ| = {d:0.0000}";
            if (AutoAdvance) nextAt = Time.time + 4f;
        }

        UpdateStatus();
    }

    // a narrated chapter steering t (exp:lab:steer): from steerFrom to steerTo over steerSeconds
    double steerFrom, steerTo;
    float steerStart = -1f, steerSeconds = 1f;

    public override bool StoryCommand(string verb, string[] a)
    {
        switch (verb)
        {
            case "round":
                if (a.Length > 0 && int.TryParse(a[0], out int r)) StartRound(r);
                return true;
            case "steer":
            {
                // steer:DT[:SECONDS]: ease t to the round's zero + DT
                double dt = a.Length > 0 && double.TryParse(a[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double d) ? d : 0;
                steerFrom = t;
                steerTo = ZeroT(zeroIndex) + dt;
                steerSeconds = a.Length > 1 && float.TryParse(a[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float sec) ? Mathf.Max(.1f, sec) : 3f;
                steerStart = Time.time;
                return true;
            }
        }

        return base.StoryCommand(verb, a);
    }

    protected override void OnTick(float dt)
    {
        if (steerStart >= 0f)
        {
            float u = Mathf.Clamp01((Time.time - steerStart) / steerSeconds);
            SetT(steerFrom + (steerTo - steerFrom) * Mathf.SmoothStep(0f, 1f, u));
            if (u >= 1f) steerStart = -1f;
        }

        if (nextAt > 0f && Time.time >= nextAt)
        {
            nextAt = -1f;
            StartRound(round + 1);
        }

        marbles.Clear();
        // ζ itself, and the vertices 1 … KMax (round 2: the ones to connect)
        marbles.Add(Plane(zeta), 2.2f, NumberKind.Gold, 1.3f);
        for (int k = 1; k <= KMax && k < sums.Length; k++)
            marbles.Add(Plane(sums[k]), round == 1 ? 1.9f : 1.2f, found.Contains(k) ? NumberKind.ColorOf(k) : new Color(.8f, .83f, .92f), 1.1f);
        DrawMaps();
        marbles.Draw(Matrix4x4.identity);
    }

    /// <summary>The small maps: n ≤ 60 on E8's clock (hour n mod 30) and on the Sacks spiral, the found vortices' numbers large.</summary>
    void DrawMaps()
    {
        for (int n = 1; n <= 60; n++)
        {
            bool lit = found.Contains(n);
            Color c = NumberKind.ColorOf(n);
            if (!lit) c = Color.Lerp(c, new Color(.12f, .13f, .17f), .45f);
            float hour = (n % 30) / 30f * Mathf.PI * 2f, r = 6f + 7f * (n / 30f);
            marbles.Add(MapE8 + new Vector3(Mathf.Sin(hour), Mathf.Cos(hour), 0f) * r, lit ? 2.4f : 1.1f, c, lit ? 1.35f : 1f);
            float a = Mathf.PI * 2f * Mathf.Sqrt(n), rs = 2.6f * Mathf.Sqrt(n);
            marbles.Add(MapSacks + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * rs, lit ? 2.4f : 1.1f, c, lit ? 1.35f : 1f);
        }
    }

    public override bool OnKey(Keyboard k)
    {
        if (k.nKey.wasPressedThisFrame) { StartRound(round + 1); return true; }
        if (k.backspaceKey.wasPressedThisFrame) { Restart(); return true; }
        if (round == 2 && (k.leftArrowKey.isPressed || k.rightArrowKey.isPressed))
        {
            double step = (k.leftShiftKey.isPressed ? .0005 : .004) * (k.leftArrowKey.isPressed ? -1 : 1);
            SetT(System.Math.Clamp(t + step, SliderLo, SliderHi));
            return true;
        }

        return CommonKey(k);
    }

    // for tests
    public void GoToRound(int r) => StartRound(r);
    public int VortexCount => vortices.Count;
    public void SteerTo(double nt) => SetT(nt);
    public double ZeroOfRound => ZeroT(zeroIndex);

    public bool PickValue(long v, string kind)
    {
        int i = Picks.FindIndex(x => x.Value == v && x.Kind == kind);
        if (i < 0) return false;
        OnPick(i);
        return true;
    }
}
