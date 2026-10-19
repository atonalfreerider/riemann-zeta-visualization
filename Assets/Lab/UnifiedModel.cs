using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// One model for E8, the primes, the Sacks spiral and ζ:
///   the funnel        n ≤ 600 on E8's wheel, columns cut by prime, a picked n's path back
///   the E8 plate      E8's 240 roots seen from their Coxeter plane, under the funnel, turned so the root directions fall on
///                     the 30 hours: the Coxeter element turns it one hour, as n → n + 1 turns the wheel; the Frobenius ζ ↦ ζ^p
///                     of a prime acts on E8's planes as n → p·n acts on the hours
///   the zeta tower    ζ(½ + it) as a trail rising with t, the zeros marked; at height t, the 30 hour sums
///                     S_h = Σ_{n ≡ h (30)} n^(−s) chained head to tail end at ζ(½ + it). Ordered by gcd(h, 30) the chain comes
///                     back to the axis after every class at a zero: each class sums to d^(−s) Π_{p | 30/d} (1 − p^(−s)) ζ(s)
///   the phase path    n^(−s) for the picked n and each number on its path back: each drop by p turns by t·log p and
///                     stretches by √p (n ↦ n^(−s) turns multiplication into spiral similarities); the drops gold like the
///                     funnel's pieces, each number a bead in the colour of its column's top
///   the Sacks layout  the same columns at angle 2π√n instead of the hour, with Euler's k² + k + 41 on one smooth curve
///                     (prime for k ≤ 39 because Q(√−163) has class number 1; j = E4³/Δ, E4 the theta series of E8, is
///                     −640320³ at (1 + √−163)/2)
/// </summary>
public class UnifiedModel : LabModel
{
    public const int Count = 600;
    const float Kt = .55f, Kz = 9f, TMax = 66f;
    FunnelView funnel;
    Vector3 tower;
    SegmentMesh chain, phase, plateSpokes;
    PointMesh plate, returns, stations;
    LineRenderer trail, trailWindow;
    GameObject trailDot;
    readonly List<GameObject> towerLabels = new();
    readonly List<Vector3> trailPoints = new();
    const float TrailStep = .04f;
    readonly List<GameObject> zeroMarkers = new();
    readonly List<Vector3> zeroPoints = new();
    Transform plateRoot;
    float t = 14.134725f;
    bool playing, grouped = true, phaseOn = true, plateOn = true;
    float sacks;
    int selected = 360;
    readonly Cpx[] hours = new Cpx[31];
    Cpx zeta;
    bool dirty = true;
    float statusClock;
    int viewMode;
    float followedT = -1f;
    int chi;
    readonly List<GameObject> zeroLabels = new();

    static readonly int[] Units = { 1, 7, 11, 13, 17, 19, 23, 29 };
    static readonly string[] ChiShort = { "χ: ζ", "χ mod 3", "χ mod 5, ±1", "χ mod 15, ±1", "χ mod 5, i", "χ mod 5, −i", "χ mod 15, i", "χ mod 15, −i" };

    static readonly string[] ViewNames = { "whole", "ζ plane", "funnel" };

    static readonly int[] ClassOrder = { 1, 2, 3, 5, 6, 10, 15, 30 };

    public override string Id => "unified";
    public override string Title => "E8, the primes and ζ";
    public override string Short => "Unified";
    public override VRIcons.Id Icon => VRIcons.Id.Unify;
    public float T => t;

    public static Color ClassColor(int d) => d switch
    {
        1 => LabMath.Gold,
        2 => new Color(1f, .36f, .32f),
        3 => new Color(.38f, 1f, .5f),
        5 => new Color(.45f, .66f, 1f),
        6 => new Color(1f, .62f, .25f),
        10 => new Color(.78f, .5f, 1f),
        15 => new Color(.3f, .95f, .95f),
        _ => Color.white,
    };

    protected override void OnBuild()
    {
        funnel = new FunnelView(Root, O, Count, 2.2f, 5f);
        funnel.Stepped += lit => Lab.SetStrip(funnel.Selected, lit);
        BuildPlate();
        tower = O + new Vector3(funnel.Rim + 34f, 0f, 0f);
        BuildTower();
        chain = new SegmentMesh(Root, "hour chain", Factory.GlowIntensity);
        phase = new SegmentMesh(Root, "phase path", Factory.GlowIntensity);
        stations = new PointMesh(Root, "phase path numbers", Factory.GlowIntensity);
        returns = new PointMesh(Root, "class returns", Factory.GlowIntensity);

        Controls.Add(LabControl.Button("Prev zero", VRIcons.Id.PrevChapter, () => JumpZero(-1), ","));
        Controls.Add(LabControl.Button("Next zero", VRIcons.Id.NextChapter, () => JumpZero(1), "."));
        Controls.Add(LabControl.Toggle("Play t", VRIcons.Id.Play, () => playing, () => playing = !playing, "Space"));
        Controls.Add(LabControl.SliderOf("t", 0f, TMax, () => t, v => SetT(v), v => $"{v:0.00}", 2, "[ ]"));
        Controls.Add(LabControl.Toggle("By gcd", VRIcons.Id.Lattice, () => grouped, () => { grouped = !grouped; dirty = true; }, "G"));
        Controls.Add(LabControl.Toggle("Phase path", VRIcons.Id.Chain, () => phaseOn, () => { phaseOn = !phaseOn; dirty = true; }, "H"));
        Controls.Add(LabControl.Toggle("E8 plate", VRIcons.Id.E8, () => plateOn, () => { plateOn = !plateOn; plateRoot.gameObject.SetActive(plateOn); }, "E"));
        Controls.Add(LabControl.SliderOf("Sacks", 0f, 1f, () => sacks, v => SetSacks(v), v => v < .01f ? "E8 wheel" : v > .99f ? "Sacks spiral" : $"{v:0.00}", 2, "K L"));
        Controls.Add(new LabControl
        {
            Label = "Character", Icon = VRIcons.Id.Legendre, Clicked = () => SetCharacter((chi + 1) % LabMath.Characters.Length),
            DynamicLabel = () => ChiShort[chi], On = () => chi != 0, Keys = "C"
        });
        Controls.Add(new LabControl
        {
            Label = "View", Icon = VRIcons.Id.Eye, Clicked = CycleView, DynamicLabel = () => "View: " + ViewNames[viewMode], Keys = "V"
        });
    }

    void CycleView()
    {
        viewMode = (viewMode + 1) % ViewNames.Length;
        Lab.SetView(CurrentView(), false);
        followedT = t;
        ApplyViewLayers();
        dirty = true;
    }

    /// <summary>Seen from above, the whole trail would pile every turn onto the plane: show only its last few units of t there.</summary>
    void ApplyViewLayers()
    {
        bool plane = viewMode == 1;
        trail.gameObject.SetActive(!plane);
        trailWindow.gameObject.SetActive(plane);
        foreach (GameObject l in towerLabels) l.SetActive(!plane);
        foreach (GameObject l in zeroLabels) l.SetActive(!plane);
        foreach (GameObject m in zeroMarkers) m.SetActive(!plane);
    }

    /// <summary>The whole model; the plane of ζ at height t seen from above (it follows t); or the funnel alone.</summary>
    LabView CurrentView() => viewMode switch
    {
        1 => new LabView { Focus = InPlane(Level(t), zeta * .5d) + new Vector3(0f, -4f, 0f), Distance = 48f, Yaw = -90f, Pitch = 72f, Fov = 50f },
        2 => new LabView { Focus = O + Vector3.back * 12f, Distance = 140f, Yaw = -70f, Pitch = 34f, Fov = 50f },
        _ => new LabView { Focus = O + new Vector3(funnel.Rim * .45f, 0f, 2f), Distance = 196f, Yaw = -90f, Pitch = 26f, Fov = 50f },
    };

    public override LabView Home => CurrentView();

    public override void OnEnter()
    {
        if (funnel.Selected < 2) funnel.Select(selected, true);
        else Lab.SetStrip(funnel.Selected, funnel.StepsShown);
        dirty = true;
    }

    // ---- the E8 plate ------------------------------------------------------------------------------------

    void BuildPlate()
    {
        plateRoot = new GameObject("E8 plate").transform;
        plateRoot.SetParent(Root, false);
        Vector3 c = O + Vector3.forward * 16f; // under the floor (the model's up is −Z)
        float scale = funnel.Rim * .92f / (float)E8Geometry.RingRadius[7];
        // turn the plane so the roots on the aligned rings point at the hours (90° − 12h)
        float best = 0f;
        foreach (int[] r in E8Geometry.Roots)
        {
            Vector2 p = E8Geometry.Project(r, 0);
            if (p.magnitude > (float)E8Geometry.RingRadius[7] - 1e-3f)
            {
                best = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
                break;
            }
        }

        // the outer ring's directions are best + 12k; pick the turn that puts one of them on an hour (90 − 12h ≡ 6 mod 12)
        float turn = Mathf.Repeat(6f - best, 12f);
        Quaternion q = Quaternion.AngleAxis(-turn, Vector3.back);
        List<Vector3> pts = new();
        List<Color> cols = new();
        List<float> sz = new();
        Color[] ring = { new(1f, .42f, .55f), new(.45f, 1f, .55f), new(.35f, .9f, 1f), new(.5f, .6f, 1f), new(1f, .6f, .3f), new(1f, .84f, .3f), new(1f, .34f, .3f), new(.8f, .56f, 1f) };
        for (int i = 0; i < 240; i++)
        {
            Vector2 p = E8Geometry.Project(E8Geometry.Roots[i], 0) * scale;
            pts.Add(c + q * new Vector3(p.x, p.y, 0f));
            cols.Add(ring[E8Geometry.Orbit[i]]);
            sz.Add(1.15f);
        }

        plate = new PointMesh(plateRoot, "E8 roots", Factory.GlowIntensity);
        plate.Set(pts, cols, sz);
        List<(Vector3, Vector3, Color, float)> spokes = new();
        for (int h = 0; h < 30; h++)
        {
            float a = (90f - 12f * h) * Mathf.Deg2Rad;
            Vector3 d = new(Mathf.Cos(a), Mathf.Sin(a), 0f);
            bool unit = System.Array.IndexOf(FunnelView.Exponents, h) >= 0;
            spokes.Add((c, c + d * funnel.Rim * .97f, unit ? LabMath.Gold * .7f : new Color(.45f, .48f, .62f) * .5f, unit ? .22f : .12f));
            if (unit) spokes.Add((O + d * funnel.Rim * 1.02f, c + d * funnel.Rim * .97f, LabMath.Gold * .45f, .12f));
        }

        plateSpokes = new SegmentMesh(plateRoot, "plate spokes");
        plateSpokes.Set(spokes);
        LabDraw.Label(plateRoot, "E8's 240 roots:  c turns them one hour,\nas n → n + 1 turns the wheel",
            c + new Vector3(-funnel.Rim * 1.25f, 0f, 0f), 44f, new Color(.8f, .84f, .95f));
    }

    // ---- the zeta tower ----------------------------------------------------------------------------------

    Vector3 Level(float tt) => tower + Vector3.back * (tt * Kt);
    Vector3 InPlane(Vector3 level, Cpx z) => level + new Vector3((float)z.Re * Kz, (float)z.Im * Kz, 0f);

    void BuildTower()
    {
        trail = LabDraw.Line(Root, "zeta trail", .3f, new Color(.85f, .88f, .95f), Factory.SoftGlowIntensity, new[] { Vector3.zero, Vector3.zero });
        BuildTrail();
        trailWindow = LabDraw.Line(Root, "zeta trail, the last few units of t", .3f, new Color(.85f, .88f, .95f), Factory.SoftGlowIntensity, new[] { Vector3.zero, Vector3.zero });
        trailWindow.gameObject.SetActive(false);
        List<(Vector3, Vector3, Color, float)> frame = new() { (Level(0f), Level(TMax), new Color(.6f, .62f, .75f) * .6f, .2f) };
        for (int k = 0; k <= 6; k++)
        {
            Vector3 l = Level(k * 10f);
            frame.Add((l, l + new Vector3(1.6f, 0f, 0f), new Color(.6f, .62f, .75f) * .6f, .15f));
            towerLabels.Add(LabDraw.Label(Root, $"t = {k * 10}", l + new Vector3(-4f, 0f, 0f), 22f, new Color(.6f, .64f, .8f), TextAlignmentOptions.Right).gameObject);
        }

        // the unit circle at the floor, for scale
        Vector3 prev = Vector3.zero;
        for (int i = 0; i <= 64; i++)
        {
            float a = i / 64f * Mathf.PI * 2f;
            Vector3 q = Level(0f) + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * Kz;
            if (i > 0) frame.Add((prev, q, new Color(.5f, .55f, .75f) * .5f, .12f));
            prev = q;
        }

        new SegmentMesh(Root, "tower frame").Set(frame);
        BuildZeroMarkers();
        towerLabels.Add(LabDraw.Label(Root, "ζ(½ + it)", Level(TMax) + new Vector3(0f, 0f, -2.5f), 40f, Color.white).gameObject);
        trailDot = LabDraw.Dot(Root, "zeta now", Level(t), 1.1f, Color.white);
    }

    // ---- state -------------------------------------------------------------------------------------------

    public void SetT(float v)
    {
        v = Mathf.Clamp(v, 0f, TMax);
        if (Mathf.Abs(v - t) < 1e-5f) return;
        t = v;
        dirty = true;
    }

    void SetSacks(float v)
    {
        sacks = Mathf.Clamp01(v);
        funnel.SetSacks(sacks);
        if (funnel.EulerOn != sacks > .5f) funnel.SetEuler(sacks > .5f);
        dirty = true;
    }

    /// <summary>
    /// Weight E8's 8 exponent hours by a Dirichlet character mod 30: the arrows χ(h)·S_h add up to L(s, χ) (checked against
    /// mpmath to 10⁻²⁵), so the trail becomes L(½ + it, χ) and the chain of 8 closes at its zeros. Character 0 is ζ itself.
    /// Up to the Euler factors at 2, 3 and 5 the eight L-functions multiply to the Dedekind zeta function of Q(ζ₁₅).
    /// </summary>
    public void SetCharacter(int k)
    {
        chi = Mathf.Clamp(k, 0, LabMath.Characters.Length - 1);
        BuildTrail();
        BuildZeroMarkers();
        ApplyViewLayers();
        dirty = true;
    }

    Cpx Value(float tt)
    {
        if (chi == 0) return LabMath.CriticalZeta(tt);
        Cpx s = Cpx.Zero;
        foreach (int h in Units) s += LabMath.Character(chi, h) * LabMath.HourSum(h, tt);
        return s;
    }

    void BuildTrail()
    {
        trailPoints.Clear();
        for (float tt = 0f; tt <= TMax + 1e-3f; tt += TrailStep) trailPoints.Add(InPlane(Level(tt), Value(tt)));
        LabDraw.SetLine(trail, trailPoints);
    }

    void BuildZeroMarkers()
    {
        foreach (GameObject g in zeroMarkers) Object.Destroy(g);
        foreach (GameObject g in zeroLabels) Object.Destroy(g);
        zeroMarkers.Clear();
        zeroLabels.Clear();
        zeroPoints.Clear();
        double[] zeros = LabMath.ZerosOf(chi);
        for (int k = 0; k < zeros.Length; k++)
        {
            float tz = (float)zeros[k];
            if (tz > TMax) break;
            Vector3 p = Level(tz);
            zeroPoints.Add(p);
            zeroMarkers.Add(LabDraw.Dot(Root, $"zero {k + 1}", p, .85f, LabMath.Gold));
            if (k < 5) zeroLabels.Add(LabDraw.Label(Root, $"t<sub>{k + 1}</sub> = {tz:0.00}", p + new Vector3(3.2f, 0f, 0f), 24f, LabMath.Gold, TextAlignmentOptions.Left).gameObject);
        }
    }

    void JumpZero(int dir)
    {
        double[] z = LabMath.ZerosOf(chi);
        if (dir > 0)
        {
            foreach (double tz in z)
                if (tz > t + 1e-3)
                {
                    SetT((float)tz);
                    return;
                }

            SetT((float)z[0]);
        }
        else
        {
            for (int k = z.Length - 1; k >= 0; k--)
                if (z[k] < t - 1e-3)
                {
                    SetT((float)z[k]);
                    return;
                }

            SetT((float)z[z.Length - 1]);
        }

        playing = false;
    }

    int ZeroIndexAt(float tt)
    {
        double[] z = LabMath.ZerosOf(chi);
        for (int k = 0; k < z.Length; k++)
            if (Mathf.Abs(tt - (float)z[k]) < .002f) return k;
        return -1;
    }

    public override void Tick(float dt)
    {
        funnel.Tick(dt);
        if (playing)
        {
            float near = float.MaxValue;
            foreach (double tz in LabMath.ZerosOf(chi)) near = Mathf.Min(near, Mathf.Abs(t - (float)tz));
            float speed = 1.6f * Mathf.Clamp(near / 1.2f, .1f, 1f);
            float next = t + dt * speed;
            SetT(next > TMax ? .5f : next);
        }

        if (funnel.Selected >= 2 && funnel.Selected != selected)
        {
            selected = funnel.Selected;
            dirty = true;
        }

        funnel.SetDim(viewMode == 1 ? .15f : .45f);
        if (viewMode == 1 && Mathf.Abs(followedT - t) > 1e-4f)
        {
            followedT = t;
            Lab.FollowFocus(CurrentView());
        }

        statusClock -= dt;
        if (!dirty) return;
        dirty = false;
        Recompute();
        Redraw();
        if (statusClock <= 0f || !playing)
        {
            UpdateStatus();
            statusClock = .2f;
        }
    }

    void Recompute()
    {
        zeta = Cpx.Zero;
        for (int h = 1; h <= 30; h++)
        {
            hours[h] = LabMath.HourSum(h, t);
            if (chi == 0) zeta += hours[h];
        }

        if (chi != 0)
            foreach (int h in Units) zeta += LabMath.Character(chi, h) * hours[h];
    }

    /// <summary>The colour of a character value: 1 gold, i cyan, −1 red, −i violet.</summary>
    static Color WeightColor(Cpx w) => w.Re > .5 ? LabMath.Gold : w.Re < -.5 ? new Color(1f, .36f, .32f) : w.Im > 0 ? new Color(.3f, .95f, .95f) : new Color(.78f, .5f, 1f);

    /// <summary>The hours in chain order: by class of gcd(h, 30) (E8's exponents first), or 1 … 30.</summary>
    List<int> ChainOrder()
    {
        List<int> order = new();
        if (grouped)
        {
            foreach (int d in ClassOrder)
                for (int h = 1; h <= 30; h++)
                    if (LabMath.Gcd(h, 30) == d) order.Add(h);
        }
        else
            for (int h = 1; h <= 30; h++) order.Add(h);

        return order;
    }

    void Redraw()
    {
        Vector3 level = Level(t);
        trailDot.transform.position = InPlane(level, zeta);
        if (viewMode == 1)
        {
            int end = Mathf.Clamp(Mathf.RoundToInt(t / TrailStep), 0, trailPoints.Count - 1);
            int start = Mathf.Max(0, end - Mathf.RoundToInt(4f / TrailStep));
            List<Vector3> window = trailPoints.GetRange(start, end - start + 1);
            window.Add(InPlane(level, zeta));
            LabDraw.SetLine(trailWindow, window);
        }
        List<(Vector3, Vector3, Color, float)> segs = new();
        List<Vector3> ends = new();
        List<Color> endCols = new();
        List<float> endSizes = new();
        Cpx acc = Cpx.Zero;
        int lastClass = -1;
        if (chi != 0)
        {
            foreach (int h in Units)
            {
                Cpx w = LabMath.Character(chi, h);
                Vector3 a = InPlane(level, acc);
                acc += w * hours[h];
                Arrow(segs, a, InPlane(level, acc), WeightColor(w), .45f);
            }
        }
        else
        foreach (int h in ChainOrder())
        {
            int d = LabMath.Gcd(h, 30);
            if (grouped && lastClass >= 0 && d != lastClass)
            {
                ends.Add(InPlane(level, acc));
                endCols.Add(ClassColor(lastClass));
                endSizes.Add(.7f);
            }

            lastClass = d;
            Vector3 a = InPlane(level, acc);
            acc += hours[h];
            Vector3 b = InPlane(level, acc);
            Arrow(segs, a, b, ClassColor(d), .45f);
        }

        if (chi == 0 && grouped && lastClass >= 0)
        {
            ends.Add(InPlane(level, acc));
            endCols.Add(ClassColor(lastClass));
            endSizes.Add(.7f);
        }

        // the axis point at this height (where the classes close at a zero) and the unit circle there
        for (int i = 0; i < 16; i++)
        {
            float a0 = i / 16f * Mathf.PI * 2f, a1 = (i + 1) / 16f * Mathf.PI * 2f;
            segs.Add((level + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * .9f, level + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * .9f, Color.white, .22f));
        }

        Vector3 prev = Vector3.zero;
        for (int i = 0; i <= 48; i++)
        {
            float a = i / 48f * Mathf.PI * 2f;
            Vector3 q = level + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * Kz;
            if (i > 0) segs.Add((prev, q, new Color(.55f, .6f, .8f) * .45f, .1f));
            prev = q;
        }

        chain.Set(segs);
        if (ends.Count > 0) returns.Set(ends, endCols, endSizes);
        returns.Visible = grouped && chi == 0;

        // the phase path: one gold step per drop by a prime (the funnel's piece; its prime shows in its turn t·log p and its
        // stretch √p), thinner than the hour arrows, and a bead at each number in the colour of its column's top
        List<(Vector3, Vector3, Color, float)> ps = new();
        List<Vector3> beads = new();
        List<Color> beadColors = new();
        List<float> beadSizes = new();
        if (phaseOn && selected >= 2)
        {
            int n = selected;
            Vector3 p0 = InPlane(level, LabMath.Term(n, t));
            beads.Add(p0);
            beadColors.Add(FunnelView.TopColor(n));
            beadSizes.Add(.4f);
            while (n > 1)
            {
                int p = LabMath.Spf(n), m = n / p;
                Vector3 p1 = InPlane(level, LabMath.Term(m, t));
                ps.Add((p0, p1, NumberKind.Gold, .3f));
                beads.Add(p1);
                beadColors.Add(FunnelView.TopColor(m));
                beadSizes.Add(.4f);
                p0 = p1;
                n = m;
            }
        }

        phase.Set(ps);
        stations.Set(beads, beadColors, beadSizes);
    }

    static void Arrow(List<(Vector3, Vector3, Color, float)> segs, Vector3 a, Vector3 b, Color c, float w)
    {
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 1e-4f) return;
        segs.Add((a, b, c, w));
        Vector3 dir = d / len, side = Vector3.Cross(dir, Vector3.back).normalized;
        float head = Mathf.Min(len * .35f, 1.3f);
        segs.Add((b, b - dir * head + side * head * .45f, c, w * .9f));
        segs.Add((b, b - dir * head - side * head * .45f, c, w * .9f));
    }

    void UpdateStatus()
    {
        int k = ZeroIndexAt(t);
        if (chi != 0)
        {
            string lv = $"{zeta.Re:0.000} {(zeta.Im < 0 ? "−" : "+")} {System.Math.Abs(zeta.Im):0.000}i";
            string h = k >= 0 ? $"t = {t:0.0000}:  zero {k + 1} of L(s, χ)" : $"t = {t:0.00}:  L(½ + it, χ) = {lv}";
            string l1 = LabMath.Characters[chi].name + (k >= 0 ? ":  the 8 gold arrows, turned by χ(h), close up" : ":  the 8 gold arrows turned by χ(h) add up to L(s, χ)");
            string l2 = "∏ L(s, χ) = ζ of Q(ζ<sub>15</sub>), the field of E8's Coxeter eigenvalues (up to 2, 3, 5)";
            Lab.SetStatus(h + "\n" + PathsModel.Small(l1) + "\n" + PathsModel.Small(l2));
            return;
        }

        string head = k >= 0
            ? $"t = {t:0.0000}:  zero {k + 1} of ζ"
            : $"t = {t:0.00}:  ζ(½ + it) = {zeta.Re:0.000} {(zeta.Im < 0 ? "−" : "+")} {System.Math.Abs(zeta.Im):0.000}i";
        string line1;
        if (k >= 0)
        {
            double worst = 0d;
            foreach (int d in ClassOrder)
            {
                Cpx s = Cpx.Zero;
                for (int h = 1; h <= 30; h++)
                    if (LabMath.Gcd(h, 30) == d) s += hours[h];
                worst = System.Math.Max(worst, s.Abs);
            }

            line1 = $"8 classes of hours by gcd(h, 30):  each class sums to 0 on its own  (to {worst:0.0e0})";
        }
        else line1 = "ζ = sum of 30 hour sums  S<sub>h</sub> = Σ n<sup>−s</sup> over n ≡ h (30);  gold: E8's exponents";

        string line2 = selected >= 2 && phaseOn
            ? $"{selected}<sup>−s</sup> → 1:  each drop by p turns by t·log p and stretches by √p"
            : "touch a column to follow its path back, in the funnel and in ζ's plane";
        if (sacks > .5f) line2 = "k² + k + 41 lies on one curve:  E4 = θ<sub>E8</sub>,  E4³/Δ = −640320³ at (1+√−163)/2";
        Lab.SetStatus(head + "\n" + PathsModel.Small(line1) + "\n" + PathsModel.Small(line2));
    }

    // ---- picking and keys --------------------------------------------------------------------------------

    public override int PickCount => funnel.PickCount + zeroPoints.Count;

    public override Vector3 PickPoint(int i)
    {
        if (i < funnel.PickCount)
        {
            int n = funnel.PickNumber(i);
            return funnel.Pickable(n) ? funnel.Top(n) : new Vector3(1e9f, 1e9f, 1e9f);
        }

        return zeroPoints[i - funnel.PickCount];
    }

    public override string PickLabel(int i)
    {
        if (i >= funnel.PickCount) return $"zero {i - funnel.PickCount + 1}:  t = {LabMath.ZetaZeros[i - funnel.PickCount]:0.000}";
        int n = funnel.PickNumber(i);
        return LabMath.IsPrime(n) ? $"{n}:  prime" : $"{n} = {LabMath.FactorString(n)}";
    }

    public override void OnPick(int i)
    {
        if (i >= funnel.PickCount)
        {
            SetT((float)LabMath.ZetaZeros[i - funnel.PickCount]);
            playing = false;
            return;
        }

        funnel.Select(funnel.PickNumber(i), true);
        dirty = true;
    }

    public override bool OnKey(Keyboard k)
    {
        bool used = true;
        if (k.leftArrowKey.wasPressedThisFrame) funnel.Select(funnel.Selected - 1, true);
        else if (k.rightArrowKey.wasPressedThisFrame) funnel.Select(funnel.Selected + 1, true);
        else if (k.downArrowKey.wasPressedThisFrame) funnel.Select(funnel.Selected - 30, true);
        else if (k.upArrowKey.wasPressedThisFrame) funnel.Select(funnel.Selected + 30, true);
        else if (k.commaKey.wasPressedThisFrame) JumpZero(-1);
        else if (k.periodKey.wasPressedThisFrame) JumpZero(1);
        else if (k.leftBracketKey.isPressed) SetT(t - Time.deltaTime * 2f);
        else if (k.rightBracketKey.isPressed) SetT(t + Time.deltaTime * 2f);
        else if (k.spaceKey.wasPressedThisFrame) playing = !playing;
        else if (k.gKey.wasPressedThisFrame) { grouped = !grouped; dirty = true; }
        else if (k.hKey.wasPressedThisFrame) { phaseOn = !phaseOn; dirty = true; }
        else if (k.eKey.wasPressedThisFrame) { plateOn = !plateOn; plateRoot.gameObject.SetActive(plateOn); }
        else if (k.cKey.wasPressedThisFrame) SetCharacter((chi + 1) % LabMath.Characters.Length);
        else if (k.vKey.wasPressedThisFrame) CycleView();
        else if (k.kKey.isPressed) SetSacks(sacks - Time.deltaTime * .5f);
        else if (k.lKey.isPressed) SetSacks(sacks + Time.deltaTime * .5f);
        else used = false;
        if (used) dirty = true;
        return used;
    }
}
