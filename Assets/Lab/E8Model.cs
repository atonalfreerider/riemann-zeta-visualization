using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// E8's 240 roots as a lantern: each root at its projection onto one Coxeter eigenplane (exponent m = 1, 7, 11 or 13), raised
/// by its distance from the origin in the next plane of the Galois cycle (×7 mod 30). The Coxeter element c = s₁…s₈ turns
/// every plane (by 12m°), so its 8 orbits of 30 roots are 8 horizontal rings and c turns the lantern a notch. Reflecting in a
/// picked root swaps 56 pairs and fixes the 126 roots of an E7; the Frobenius of a prime p (p mod 30 = 7, 11, 13) carries plane
/// m to ±pm and permutes the rings, (0 5 4 6)(1 2 7 3) for 7. Colours: the two Galois families of rings, warm and cool.
/// </summary>
public class E8Model : LabModel
{
    const float R = 42f, Hz = 40f;
    const float Spin = .55f;

    readonly int[][] vec = new int[240][];
    readonly Vector3[] pos = new Vector3[240];
    int[][] shellVec;
    Vector3[] shellPos;
    int plane, lift;
    PointMesh points, shell;
    LineMesh edges;
    SegmentMesh rootArrows;
    int selected = -1;
    bool neighbours, edgesOn, shellOn, spinning;
    int coxeterCount;
    string note = "";

    enum Anim { None, Coxeter, Reflect, Morph }

    Anim anim;
    float animU, animSeconds;
    Vector3[] fromPos, toPos, fromShell, toShell;
    float rotation;
    int[] alpha;
    int[] rootCoef, shellCoef;
    int targetPlane, targetLift, morphPrime;

    static (int, int)[] edgePairs;

    // the two Galois families of rings (warm: 0, 4, 5, 6; cool: 1, 2, 3, 7)
    static readonly Color[] RingColors =
    {
        new(1f, .42f, .55f), new(.45f, 1f, .55f), new(.35f, .9f, 1f), new(.5f, .6f, 1f),
        new(1f, .6f, .3f), new(1f, .84f, .3f), new(1f, .34f, .3f), new(.8f, .56f, 1f)
    };

    public override string Id => "e8";
    public override string Title => "E8";
    public override string Short => "E8";
    public override VRIcons.Id Icon => VRIcons.Id.E8;

    protected override void OnBuild()
    {
        for (int i = 0; i < 240; i++) vec[i] = (int[])E8Geometry.Roots[i].Clone();
        plane = 0;
        lift = E8Geometry.FrobeniusPlane(0, 7);
        if (edgePairs == null)
        {
            List<(int, int)> pairs = new();
            for (int i = 0; i < 240; i++)
                for (int j = i + 1; j < 240; j++)
                    if (E8Geometry.Dot(E8Geometry.Roots[i], E8Geometry.Roots[j]) == 1) pairs.Add((i, j));
            edgePairs = pairs.ToArray();
        }

        // an axis and a floor circle to stand the lantern on
        List<(Vector3, Vector3, Color, float)> frame = new() { (O, O + Vector3.back * Hz * 1.25f, new Color(.5f, .55f, .75f) * .5f, .2f) };
        Vector3 prev = Vector3.zero;
        for (int i = 0; i <= 96; i++)
        {
            float a = i / 96f * Mathf.PI * 2f;
            Vector3 q = O + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * R * 1.2f;
            if (i > 0) frame.Add((prev, q, new Color(.4f, .45f, .65f) * .45f, .2f));
            prev = q;
        }

        new SegmentMesh(Root, "frame").Set(frame);
        edges = new LineMesh(Root, "edges");
        shell = new PointMesh(Root, "norm 4", Factory.SoftGlowIntensity);
        points = new PointMesh(Root, "roots", Factory.GlowIntensity);
        rootArrows = new SegmentMesh(Root, "root and its negative", 2.2f);
        PlaceAll();
        points.Set(pos, Colours(), Sizes());
        edges.Set(edgePairs, pos, new Color(.42f, .5f, .8f) * .5f);
        edges.Visible = false;
        shell.Visible = false;

        Controls.Add(LabControl.Button("c step", VRIcons.Id.Dial, () => StartCoxeter(), "C"));
        Controls.Add(LabControl.Toggle("Spin", VRIcons.Id.Replay, () => spinning, () => { spinning = !spinning; if (spinning) StartCoxeter(); }, "S"));
        Controls.Add(LabControl.Button("Frobenius 7", VRIcons.Id.Twist, () => StartMorph(7), "7"));
        Controls.Add(LabControl.Button("Frobenius 11", VRIcons.Id.Twist, () => StartMorph(11), "1"));
        Controls.Add(LabControl.Button("Frobenius 13", VRIcons.Id.Twist, () => StartMorph(13), "3"));
        Controls.Add(LabControl.Button("Reflect", VRIcons.Id.Mirror, StartReflect, "X"));
        Controls.Add(LabControl.Toggle("Neighbours", VRIcons.Id.Lattice, () => neighbours, () => { neighbours = !neighbours; Recolour(); UpdateStatus(); }, "H"));
        Controls.Add(LabControl.Toggle("Edges", VRIcons.Id.Chords, () => edgesOn, () => { edgesOn = !edgesOn; edges.Visible = edgesOn; if (edgesOn) edges.Move(pos); }, "E"));
        Controls.Add(LabControl.Toggle("Norm 4", VRIcons.Id.Ideals, () => shellOn, ToggleShell, "4"));
        Controls.Add(LabControl.Button("Reset", VRIcons.Id.Reset, ResetRoots, "Backspace"));
        Controls.Add(LabControl.Button("Reset view", VRIcons.Id.Target, () => Lab.ResetView(false), "Home"));
    }

    public override LabView Home => new() { Focus = O + Vector3.back * Hz * .58f, Distance = 112f, Yaw = -80f, Pitch = 24f, Fov = 50f };

    public override void OnEnter() => UpdateStatus();

    // ---- placement ---------------------------------------------------------------------------------------

    Vector3 Place(int[] v, int p, int q)
    {
        Vector2 xy = E8Geometry.Project(v, p) * R;
        float z = E8Geometry.Project(v, q).magnitude * Hz;
        return O + new Vector3(xy.x, xy.y, 0f) + Vector3.back * z;
    }

    Vector3 Place(double[] v, int p, int q)
    {
        Vector2 xy = E8Geometry.Project(v, p) * R;
        float z = E8Geometry.Project(v, q).magnitude * Hz;
        return O + new Vector3(xy.x, xy.y, 0f) + Vector3.back * z;
    }

    void PlaceAll()
    {
        for (int i = 0; i < 240; i++) pos[i] = Place(vec[i], plane, lift);
        if (shellVec != null)
            for (int i = 0; i < shellVec.Length; i++) shellPos[i] = Place(shellVec[i], plane, lift);
    }

    List<Color> Colours()
    {
        List<Color> c = new(240);
        int[] a = selected >= 0 ? vec[selected] : null;
        for (int i = 0; i < 240; i++)
        {
            if (neighbours && a != null)
            {
                int d = E8Geometry.Dot(vec[i], a);
                c.Add(d switch
                {
                    2 => Color.white,
                    1 => LabMath.Gold,
                    0 => new Color(.35f, .6f, 1f),
                    -1 => new Color(1f, .3f, .28f),
                    _ => new Color(.75f, .75f, .8f),
                });
            }
            else c.Add(RingColors[E8Geometry.Orbit[i]]);
        }

        return c;
    }

    List<float> Sizes()
    {
        List<float> s = new(240);
        for (int i = 0; i < 240; i++) s.Add(i == selected ? 2.6f : 1.45f);
        return s;
    }

    void Recolour()
    {
        points.Recolour(Colours(), Sizes());
        points.Move(pos);
        DrawRootArrows();
    }

    void DrawRootArrows()
    {
        if (selected < 0)
        {
            rootArrows.Clear();
            return;
        }

        int[] neg = new int[8];
        for (int k = 0; k < 8; k++) neg[k] = -vec[selected][k];
        int ni = System.Array.FindIndex(vec, v => v[0] == neg[0] && v[1] == neg[1] && v[2] == neg[2] && v[3] == neg[3] && v[4] == neg[4] && v[5] == neg[5] && v[6] == neg[6] && v[7] == neg[7]);
        List<(Vector3, Vector3, Color, float)> segs = new() { (O, pos[selected], Color.white, .45f) };
        if (ni >= 0) segs.Add((O, pos[ni], new Color(.8f, .8f, .85f) * .7f, .3f));
        rootArrows.Set(segs);
    }

    void ToggleShell()
    {
        shellOn = !shellOn;
        if (shellOn && shellVec == null)
        {
            int[][] s = E8Geometry.Shell4;
            shellVec = new int[s.Length][];
            shellPos = new Vector3[s.Length];
            for (int i = 0; i < s.Length; i++) shellVec[i] = (int[])s[i].Clone();
            for (int i = 0; i < s.Length; i++) shellPos[i] = Place(shellVec[i], plane, lift);
            List<Color> c = new();
            List<float> z = new();
            for (int i = 0; i < s.Length; i++)
            {
                c.Add(new Color(.62f, .68f, .9f) * .7f);
                z.Add(.6f);
            }

            shell.Set(shellPos, c, z);
        }

        shell.Visible = shellOn;
        if (shellOn) shell.Move(shellPos);
        note = shellOn ? $"the {E8Geometry.Shell4.Length} lattice vectors of norm 4 (240·σ<sub>3</sub>(2) = 2160), √2 farther out" : "";
        UpdateStatus();
    }

    // ---- actions -----------------------------------------------------------------------------------------

    void Begin(Anim a, float seconds)
    {
        anim = a;
        animU = 0f;
        animSeconds = seconds;
        fromPos = (Vector3[])pos.Clone();
        if (shellOn && shellVec != null) fromShell = (Vector3[])shellPos.Clone();
    }

    void StartCoxeter()
    {
        if (anim != Anim.None) return;
        Begin(Anim.Coxeter, spinning ? Spin : .7f);
        rotation = -12f * E8Geometry.Exponents[plane];
    }

    void StartReflect()
    {
        if (anim != Anim.None) return;
        if (selected < 0)
        {
            note = "pick a root first: its mirror is the one to reflect in";
            UpdateStatus();
            return;
        }

        spinning = false;
        alpha = (int[])vec[selected].Clone();
        rootCoef = new int[240];
        for (int i = 0; i < 240; i++) rootCoef[i] = E8Geometry.Dot(vec[i], alpha);
        if (shellOn && shellVec != null)
        {
            shellCoef = new int[shellVec.Length];
            for (int i = 0; i < shellVec.Length; i++) shellCoef[i] = E8Geometry.Dot(shellVec[i], alpha);
        }

        Begin(Anim.Reflect, 1.3f);
    }

    void StartMorph(int p)
    {
        if (anim != Anim.None) return;
        spinning = false;
        morphPrime = p;
        targetPlane = E8Geometry.FrobeniusPlane(plane, p);
        targetLift = E8Geometry.FrobeniusPlane(lift, p);
        Begin(Anim.Morph, 1.5f);
        toPos = new Vector3[240];
        for (int i = 0; i < 240; i++) toPos[i] = Place(vec[i], targetPlane, targetLift);
        if (shellOn && shellVec != null)
        {
            toShell = new Vector3[shellVec.Length];
            for (int i = 0; i < shellVec.Length; i++) toShell[i] = Place(shellVec[i], targetPlane, targetLift);
        }
    }

    void ResetRoots()
    {
        anim = Anim.None;
        spinning = false;
        for (int i = 0; i < 240; i++) vec[i] = (int[])E8Geometry.Roots[i].Clone();
        if (shellVec != null)
            for (int i = 0; i < shellVec.Length; i++) shellVec[i] = (int[])E8Geometry.Shell4[i].Clone();
        plane = 0;
        lift = E8Geometry.FrobeniusPlane(0, 7);
        coxeterCount = 0;
        selected = -1;
        note = "";
        PlaceAll();
        Recolour();
        if (edgesOn) edges.Move(pos);
        if (shellOn) shell.Move(shellPos);
        UpdateStatus();
    }

    public override void Tick(float dt)
    {
        if (anim == Anim.None) return;
        animU = Mathf.Min(1f, animU + dt / animSeconds);
        float u = anim == Anim.Coxeter && spinning ? animU : Mathf.SmoothStep(0f, 1f, animU);
        switch (anim)
        {
            case Anim.Coxeter:
            {
                Quaternion q = Quaternion.AngleAxis(-rotation * u, Vector3.back);
                for (int i = 0; i < 240; i++) pos[i] = O + q * (fromPos[i] - O);
                if (shellOn && fromShell != null)
                    for (int i = 0; i < shellPos.Length; i++) shellPos[i] = O + q * (fromShell[i] - O);
                break;
            }
            case Anim.Reflect:
            {
                double[] v = new double[8];
                for (int i = 0; i < 240; i++)
                {
                    for (int k = 0; k < 8; k++) v[k] = (vec[i][k] - u * rootCoef[i] * alpha[k]) * .5d;
                    pos[i] = Place(v, plane, lift);
                }

                if (shellOn && shellVec != null && shellCoef != null)
                    for (int i = 0; i < shellVec.Length; i++)
                    {
                        for (int k = 0; k < 8; k++) v[k] = (shellVec[i][k] - u * shellCoef[i] * alpha[k]) * .5d;
                        shellPos[i] = Place(v, plane, lift);
                    }

                break;
            }
            case Anim.Morph:
                for (int i = 0; i < 240; i++) pos[i] = Vector3.Lerp(fromPos[i], toPos[i], u);
                if (shellOn && toShell != null && fromShell != null)
                    for (int i = 0; i < shellPos.Length; i++) shellPos[i] = Vector3.Lerp(fromShell[i], toShell[i], u);
                break;
        }

        points.Move(pos);
        if (edgesOn) edges.Move(pos);
        if (shellOn && shellPos != null) shell.Move(shellPos);
        if (selected >= 0) DrawRootArrows();
        if (animU < 1f) return;
        Finish();
    }

    void Finish()
    {
        Anim done = anim;
        anim = Anim.None;
        switch (done)
        {
            case Anim.Coxeter:
                for (int i = 0; i < 240; i++) vec[i] = E8Geometry.Coxeter(vec[i]);
                if (shellVec != null)
                    for (int i = 0; i < shellVec.Length; i++) shellVec[i] = E8Geometry.Coxeter(shellVec[i]);
                coxeterCount = (coxeterCount + 1) % 30;
                note = coxeterCount == 0 ? "c applied 30 times: every root is home again" : $"c applied {coxeterCount} time{(coxeterCount == 1 ? "" : "s")} (30 bring every root home)";
                break;
            case Anim.Reflect:
                for (int i = 0; i < 240; i++) vec[i] = E8Geometry.Reflect(vec[i], alpha);
                if (shellVec != null)
                    for (int i = 0; i < shellVec.Length; i++) shellVec[i] = E8Geometry.Reflect(shellVec[i], alpha);
                note = "reflected in α's mirror:  the 126 roots of an E7 stay,  56 pairs swap,  α ↔ −α";
                break;
            case Anim.Morph:
                note = $"Frobenius of p ≡ {morphPrime} (mod 30):  plane {E8Geometry.Exponents[plane]} → {E8Geometry.Exponents[targetPlane]},  rings {RingCycles(plane, targetPlane)}";
                plane = targetPlane;
                lift = targetLift;
                break;
        }

        PlaceAll();
        points.Move(pos);
        if (edgesOn) edges.Move(pos);
        if (shellOn && shellPos != null) shell.Move(shellPos);
        Recolour();
        UpdateStatus();
        if (done == Anim.Coxeter && spinning) StartCoxeter();
    }

    /// <summary>Where the rings of plane <paramref name="from"/> go in plane <paramref name="to"/>, as cycles: (0 5 4 6)(1 2 7 3).</summary>
    static string RingCycles(int from, int to)
    {
        int[] map = new int[8];
        for (int o = 0; o < 8; o++) map[E8Geometry.RingOfOrbit[from, o]] = E8Geometry.RingOfOrbit[to, o];
        bool[] seen = new bool[8];
        List<string> cycles = new();
        for (int i = 0; i < 8; i++)
        {
            if (seen[i] || map[i] == i) continue;
            List<int> cyc = new();
            for (int j = i; !seen[j]; j = map[j])
            {
                seen[j] = true;
                cyc.Add(j);
            }

            cycles.Add("(" + string.Join(" ", cyc) + ")");
        }

        return cycles.Count == 0 ? "all fixed" : string.Concat(cycles);
    }

    public static string VectorString(int[] d)
    {
        bool half = (d[0] & 1) != 0;
        List<string> parts = new();
        foreach (int x in d) parts.Add((half ? x : x / 2).ToString().Replace("-", "−"));
        return (half ? "½(" : "(") + string.Join(", ", parts) + ")";
    }

    // ---- picking and keys --------------------------------------------------------------------------------

    public override int PickCount => 240;
    public override Vector3 PickPoint(int i) => pos[i];
    public override string PickLabel(int i) => VectorString(vec[i]);

    public override void OnPick(int i)
    {
        selected = selected == i ? -1 : i;
        if (selected >= 0 && !neighbours) neighbours = true;
        note = "";
        Recolour();
        UpdateStatus();
    }

    public override bool OnKey(Keyboard k)
    {
        bool used = true;
        if (k.cKey.wasPressedThisFrame) StartCoxeter();
        else if (k.sKey.wasPressedThisFrame) { spinning = !spinning; if (spinning) StartCoxeter(); }
        else if (k.digit7Key.wasPressedThisFrame) StartMorph(7);
        else if (k.digit1Key.wasPressedThisFrame) StartMorph(11);
        else if (k.digit3Key.wasPressedThisFrame) StartMorph(13);
        else if (k.xKey.wasPressedThisFrame) StartReflect();
        else if (k.hKey.wasPressedThisFrame) { neighbours = !neighbours; Recolour(); UpdateStatus(); }
        else if (k.eKey.wasPressedThisFrame) { edgesOn = !edgesOn; edges.Visible = edgesOn; if (edgesOn) edges.Move(pos); }
        else if (k.digit4Key.wasPressedThisFrame) ToggleShell();
        else if (k.backspaceKey.wasPressedThisFrame) ResetRoots();
        else used = false;
        return used;
    }

    void UpdateStatus()
    {
        int m = E8Geometry.Exponents[plane], mLift = E8Geometry.Exponents[lift];
        string head, line1, line2;
        if (selected >= 0)
        {
            head = "root  " + VectorString(vec[selected]);
            line1 = neighbours
                ? "56 at 60° (gold)  ·  126 at 90° (blue: an E7)  ·  56 at 120° (red)  ·  its negative"
                : "touch Neighbours to colour the other roots by their angle to it";
        }
        else
        {
            head = $"E8:  240 roots, seen in plane m = {m}";
            line1 = $"c = s<sub>1</sub>s<sub>2</sub>…s<sub>8</sub> turns this plane by {12 * m}°:  its 8 orbits of 30 lie on 8 rings";
        }

        line2 = string.IsNullOrEmpty(note)
            ? $"rings raised by their radius in plane {mLift}  (every plane has the same 8 radii)"
            : note;
        Lab.SetStatus(head + "\n" + PathsModel.Small(line1) + "\n" + PathsModel.Small(line2));
    }
}
