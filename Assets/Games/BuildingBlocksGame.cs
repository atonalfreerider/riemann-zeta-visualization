using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Building blocks: a number stands as its raw volume, one marble per unit, in its own colour (grey composite, deep red p²,
/// blue pronic) as a symmetric spiral; below it, a tray of gold prime containers (2 … 37). Point at a prime: if it divides
/// what is left, the marbles part into that many equal groups round a ring, one stays and the others join the
/// fingerprint string on the left; if it does not, the groups form with the leftover marbles in the middle, shining red,
/// and everything goes back together, nothing lost. When what is left is itself prime (even past 37), point at the block.
/// Once nothing is left the fingerprint is the whole number, and it unpacks into the full volume beside its string. Any
/// number from 2 to 1024: step with n ± 1, or take the next of a set of examples (squares of primes, pronic numbers,
/// products of three primes, powers of 2, 360 and 1001). Nothing is scored.
/// </summary>
public class BuildingBlocksGame : GameModel
{
    static readonly long[] Examples = { 12, 49, 30, 91, 56, 360, 121, 105, 72, 143, 128, 210, 169, 90, 1001, 246, 221, 180, 77, 1024 };
    static readonly long[] Tray = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37 };

    readonly MarbleBatch marbles = new(1);
    readonly LineBatch rings = new();
    float depth = -1f, spin;
    readonly List<TextBox> trayLabels = new();
    int example = -1;
    long target, rest, found;
    // the split animation: rest → rest / p (or a failed split with a remainder)
    long splitFrom, splitP;
    bool splitOk;
    float splitT = -1f;
    float unpackAt = -1f;
    const long MaxN = 1024;
    TextBox restLabel;

    public override string Id => "blocks";
    public override string Title => "Building blocks";
    public override string Short => "Blocks";
    public override VRIcons.Id Icon => VRIcons.Id.Cube;

    // the wall: x to the right, up is the lab's up (−z), depth +y away from the player
    Vector3 Wall(float x, float up, float depth = 0f) => O + new Vector3(x, depth, 0f) + Up * up;
    Vector3 VolumeCentre => Wall(22f, 27f);
    Vector3 PrintAnchor => Wall(-46f, 31f);
    /// <summary>Play on by itself after a number is done (unpack, pack, next); tests turn it off.</summary>
    public bool AutoPlay = true;
    /// <summary>How long a split takes (seconds); a narrated tutorial shortens it.</summary>
    public float SplitSeconds = 1.6f;
    Vector3 TrayAt(int i) => Wall(-50f + i * 9.4f, 1f);

    protected override void OnBuild()
    {
        List<Pickable> picks = new();
        for (int i = 0; i < Tray.Length; i++)
        {
            Vector3 c = TrayCentre(i);
            picks.Add(new Pickable { Position = c, Value = Tray[i], Radius = 4f, Kind = "prime" });
            trayLabels.Add(LabDraw.Label(Root, Tray[i].ToString(), TrayAt(i) + Up * -2.6f, 40f, NumberKind.Gold));
        }

        Picks.AddRange(picks);
        restLabel = LabDraw.Label(Root, "", VolumeCentre, 48f, Color.white);
        Controls.Add(LabControl.Button("n − 1", VRIcons.Id.PrevLine, () => StartNumber(target - 1), "←"));
        Controls.Add(LabControl.Button("n + 1", VRIcons.Id.NextLine, () => StartNumber(target + 1), "→"));
        Controls.Add(LabControl.Button("Example", VRIcons.Id.NextChapter, () => StartExample(example + 1), "N"));
        AddCommonControls();
        StartExample(5);
    }

    // a little from the right and above, so the volume shows its depth
    public override LabView Home => StoryFramed(new LabView { Focus = Wall(-4f, 19f), Distance = 94f, Yaw = -76f, Pitch = 16f, Fov = 50f }, 1.12f, .02f);

    public override void OnEnter() => UpdateStatus();

    void StartExample(int e)
    {
        example = ((e % Examples.Length) + Examples.Length) % Examples.Length;
        StartNumber(Examples[example]);
    }

    /// <summary>Any number from 2 to 1024 as its raw volume, ready to split.</summary>
    void StartNumber(long n)
    {
        n = System.Math.Clamp(n, 2L, MaxN);
        target = rest = n;
        found = 1;
        splitT = -1f;
        unpackAt = -1f;
        HidePrint();
        // the block itself is pickable too: "what is left is prime"
        Picks.RemoveAll(p => p.Kind == "rest");
        Picks.Add(new Pickable { Position = VolumeCentre, Value = 0, Radius = 9f, Kind = "rest" });
        Prompt = $"Split {target} into its prime building blocks:  point at a prime in the tray" +
                 (LargestFactor(target) > 37 ? ";  at the block itself once what is left is prime" : "");
        Note = $"{target} marbles, {NumberKind.NameOf(NumberKind.Classify(target))}";
        UpdateStatus();
    }

    static long LargestFactor(long n)
    {
        List<(long p, int k)> f = NumberKind.Factor(n);
        return f.Count > 0 ? f[^1].p : 1;
    }

    protected override string LabelFor(Pickable p) => p.Kind == "rest" ? (rest > 1 ? $"{rest}:  is what is left prime?" : "") : $"{p.Value}";

    public override void OnPick(int i)
    {
        if (splitT >= 0f || rest <= 1) return;
        Pickable p = Picks[i];
        if (p.Kind == "rest")
        {
            if (NumberKind.IsPrime(rest))
            {
                Light(VolumeCentre, rest, 9f);
                Note = $"{rest} is prime:  it is its own building block";
                Divide(rest, true);
            }
            else
            {
                Light(VolumeCentre, rest, 9f);
                Note = $"{rest} still splits:  {rest} = {NumberKind.Shorthand(rest)}";
            }

            UpdateStatus();
            return;
        }

        long q = p.Value;
        if (rest % q == 0)
        {
            Light(p.Position, q, 3.4f);
            Note = $"{rest} ÷ {q} = {rest / q}:  {q} equal groups, one stays";
            Divide(q, true);
        }
        else
        {
            Ripple(p.Position, NumberKind.DeepRed, 3.4f);
            splitFrom = rest;
            splitP = q;
            splitOk = false;
            splitT = 0f;
            Note = $"{q} groups of {rest / q} leave {rest % q} over:  {q} is not one of {rest}'s blocks";
        }

        UpdateStatus();
    }

    void Divide(long q, bool ok)
    {
        splitFrom = rest;
        splitP = q;
        splitOk = ok;
        splitT = 0f;
        rest /= q;
        found *= q;
        ShowPrint(found, PrintAnchor, 2.1f, true);
        if (rest == 1)
        {
            Note = $"{target} = {NumberKind.Shorthand(target)}:  every building block.  Pull the fingerprint apart, or Unpack (U)";
            if (AutoPlay) unpackAt = Time.time + 1.6f;
        }
    }

    protected override void OnTick(float dt)
    {
        float want = DeepOn ? 1f : 0f;
        depth = depth < 0f ? want : Mathf.MoveTowards(depth, want, dt / .9f);
        spin = (spin + 14f * dt) % 360f;
        if (splitT >= 0f)
        {
            splitT += dt / Mathf.Max(.2f, SplitSeconds);
            if (splitT >= 1f) splitT = -1f;
        }

        // once every block is found, it rebuilds itself (and stays: the next number is the player's to choose)
        if (unpackAt > 0f && Time.time >= unpackAt)
        {
            unpackAt = -1f;
            Print?.Unpack();
        }

        Draw();
    }

    // ---- the marbles: the tray's containers and what is left, flat (symmetric spirals) or deep (spiral sphere packs) --

    /// <summary>A layout point on the wall: x right, y up, z into the wall.</summary>
    Vector3 OnWall(Vector3 centre, Vector3 p, float s) => centre + new Vector3(p.x * s, p.z * s, 0f) + Up * (p.y * s);

    float Ease01(float u) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u));

    /// <summary>Turned about the upright axis as the deep packs spin.</summary>
    Vector3 Turn(Vector3 p)
    {
        float a = spin * Mathf.Deg2Rad, c = Mathf.Cos(a), sn = Mathf.Sin(a);
        return new Vector3(c * p.x + sn * p.z, p.y, -sn * p.x + c * p.z);
    }

    Vector3 Blend(Vector2 flat, Vector3 deep) => Vector3.LerpUnclamped(flat, Turn(deep), Ease01(depth));

    /// <summary>
    /// A count's own shape: its flat spiral and its deep pack (m arms, a centre for odd counts); a composite's arms wide
    /// apart, as an unpacked volume's are.
    /// </summary>
    static (Vector2[] flat, Vector3[] deep) Shape(long n)
    {
        (bool c, int m) = MarbleLayout.ShapeOf(n);
        return (MarbleLayout.Flat((int)n, m, c, !NumberKind.IsPrime(n)), MarbleLayout.Deep((int)n, c ? 1 : m));
    }

    /// <summary>A tray marble's size (world units): the small primes larger.</summary>
    static float TraySize(long p) => p > 13 ? .95f : p > 5 ? 1.1f : 1.3f;

    float CupRadius(long p)
    {
        var (f, d) = Shape(p);
        return Mathf.Max(MarbleLayout.Radius(f), MarbleLayout.Radius(d)) + .45f;
    }

    /// <summary>The centre of tray container i: its ring sits on the tray line.</summary>
    Vector3 TrayCentre(int i) => TrayAt(i) + Up * (CupRadius(Tray[i]) * TraySize(Tray[i]));

    /// <summary>What is left: marbles about the volume's centre, a world size each, kept within about 16 units.</summary>
    static float VolumeSize(long n)
    {
        var (f, d) = Shape(System.Math.Max(1L, n));
        return Mathf.Min(1.45f, 16f / Mathf.Max(MarbleLayout.Radius(f), MarbleLayout.Radius(d)));
    }

    void Draw()
    {
        marbles.Clear();
        rings.Clear();
        Color gold = NumberKind.Gold;
        // the tray: each prime's container, its marbles in their spiral, a gold ring round them
        for (int t = 0; t < Tray.Length; t++)
        {
            long p = Tray[t];
            float s = TraySize(p);
            var (f, d) = Shape(p);
            Vector3 c = TrayCentre(t);
            for (int i = 0; i < p; i++) marbles.Add(OnWall(c, Blend(f[i], d[i]), s), s, gold, 1.15f);
            // in the wall's own plane (x right, y up), drawn through WallMatrix
            rings.Circle(new Vector3(c.x - O.x, O.z - c.z, 0f), CupRadius(p) * s, .14f, new Color(gold.r, gold.g, gold.b, .85f));
        }

        Color own = NumberKind.ColorOf(target);
        if (splitT >= 0f) DrawSplit(own);
        else if (rest > 1)
        {
            var (f, d) = Shape(rest);
            float s = VolumeSize(rest);
            for (int i = 0; i < rest; i++) marbles.Add(OnWall(VolumeCentre, Blend(f[i], d[i]), s), s * .92f, own, 1.08f);
        }

        if (restLabel != null)
        {
            restLabel.TextField.text = rest > 1 && splitT < 0f ? $"{rest}" : "";
            // left of the block, at its middle (above it, it ran under the status board)
            restLabel.transform.position = VolumeCentre + Vector3.left * (VolumeRadius(rest) + 6f);
        }

        marbles.Draw(Matrix4x4.identity);
        rings.Draw(WallMatrix);
    }

    /// <summary>The wall's plane: local x right, local y up (the lab's −z), local z into the wall (+y).</summary>
    Matrix4x4 WallMatrix
    {
        get
        {
            Matrix4x4 m = Matrix4x4.identity;
            m.SetColumn(0, new Vector4(1f, 0f, 0f, 0f));
            m.SetColumn(1, new Vector4(Up.x, Up.y, Up.z, 0f));
            m.SetColumn(2, new Vector4(0f, 1f, 0f, 0f));
            m.SetColumn(3, new Vector4(O.x, O.y, O.z, 1f));
            return m;
        }
    }

    float VolumeRadius(long n)
    {
        if (n <= 1) return 0f;
        var (f, d) = Shape(n);
        return Mathf.Lerp(MarbleLayout.Radius(f), MarbleLayout.Radius(d), Ease01(depth)) * VolumeSize(n);
    }

    /// <summary>
    /// The split: what is left parts into q equal groups round a ring (marble i to group i mod q, as q arms would unwind),
    /// then one group stays and the others fly to the fingerprint. A failed split leaves the remainder in the middle,
    /// shining red, and everything goes back.
    /// </summary>
    void DrawSplit(Color own)
    {
        long from = splitFrom, q = splitP, per = from / q, left = from - per * q;
        var (ff, fd) = Shape(from);
        var (pf, pd) = Shape(System.Math.Max(1L, per));
        float sFrom = VolumeSize(from), sPer = VolumeSize(System.Math.Max(1L, per));
        float perR = Mathf.Max(MarbleLayout.Radius(pf), MarbleLayout.Radius(pd)) * sPer;
        float u = Ease01(splitT * 2.2f);                         // apart into groups
        float w = Ease01(splitT * 2f - 1.05f);                   // one stays, the rest fly (or all go back)
        float ring = q == 1 ? 0f : (perR + .8f) / Mathf.Sin(Mathf.PI / q);
        // the groups' ring kept within about 17 units of the centre: past that, the groups' marbles draw smaller
        float fit = Mathf.Min(1f, 17f / (ring + perR)), sPerFull = sPer;
        ring *= fit;
        sPer *= fit;
        Vector3 Spot(long g)
        {
            float a = Mathf.PI / 2f + Mathf.PI * 2f * g / q;
            return new Vector3(Mathf.Cos(a) * ring, 0f, 0f) + Up * (Mathf.Sin(a) * ring);
        }

        var (lf, ld) = Shape(System.Math.Max(1L, left));
        for (long i = 0; i < from; i++)
        {
            Vector3 a = OnWall(VolumeCentre, Blend(ff[i], fd[i]), sFrom);
            bool leftover = i >= per * q;
            long g = i % q, k = i / q;
            Vector3 b = leftover
                ? OnWall(VolumeCentre, Blend(lf[i - per * q], ld[i - per * q]), sPer)
                : VolumeCentre + Spot(g) + (OnWall(Vector3.zero, Blend(pf[k], pd[k]), sPer));
            Vector3 pos = Vector3.Lerp(a, b, u);
            float size = Mathf.Lerp(sFrom, sPer, u) * .92f;
            Color c = leftover ? Color.Lerp(own, NumberKind.DeepRed, u) : own;
            if (splitOk)
            {
                if (g != 0)
                {
                    // the other groups go to the fingerprint and fade
                    pos = Vector3.Lerp(pos, PrintAnchor + Up * 4f, w);
                    size *= 1f - w;
                }
                else
                {
                    // the group that stays settles in the middle at what is left's own size
                    pos = Vector3.Lerp(pos, OnWall(VolumeCentre, Blend(pf[k], pd[k]), sPerFull), w);
                    size = Mathf.Lerp(size, sPerFull * .92f, w);
                }
            }
            else if (splitT > .5f)
            {
                // a failed split goes back together, the leftovers red until they are home
                pos = Vector3.Lerp(b, a, w);
                size = Mathf.Lerp(size, sFrom * .92f, w);
                if (leftover) c = Color.Lerp(NumberKind.DeepRed, own, Ease01((w - .7f) / .3f));
            }

            if (size > .01f) marbles.Add(pos, size, c, 1.08f);
        }
    }

    public override bool OnKey(Keyboard k)
    {
        if (k.nKey.wasPressedThisFrame) { StartExample(example + 1); return true; }
        if (k.leftArrowKey.wasPressedThisFrame) { StartNumber(target - 1); return true; }
        if (k.rightArrowKey.wasPressedThisFrame) { StartNumber(target + 1); return true; }
        return CommonKey(k);
    }

    // for tests
    public void GoToExample(int e) => StartExample(e);
    public void GoToNumber(long n) => StartNumber(n);
    public long Target => target;
    public long Rest => rest;

    public bool PickPrime(long p)
    {
        int i = Picks.FindIndex(x => x.Kind == "prime" && x.Value == p);
        if (i < 0) return false;
        OnPick(i);
        return true;
    }

    public void PickRest()
    {
        int i = Picks.FindIndex(x => x.Kind == "rest");
        if (i >= 0) OnPick(i);
    }

    public void FinishSplit() => splitT = -1f;

    public override bool StoryCommand(string verb, string[] a)
    {
        switch (verb)
        {
            case "example":
                if (a.Length > 0 && int.TryParse(a[0], out int e)) StartExample(e);
                return true;
            case "number":
                if (a.Length > 0 && long.TryParse(a[0], out long nn)) StartNumber(nn);
                return true;
            case "prime":
            {
                // point at the tray's container, then pick it (a split still running ends first, so no pick is lost)
                if (splitT >= 0f) FinishSplit();
                int i = a.Length > 0 && long.TryParse(a[0], out long p) ? Picks.FindIndex(x => x.Kind == "prime" && x.Value == p) : -1;
                if (i >= 0) { Lab.PointAt(i); OnPick(i); }
                return true;
            }
            case "rest":
            {
                if (splitT >= 0f) FinishSplit();
                int i = Picks.FindIndex(x => x.Kind == "rest");
                if (i >= 0) { Lab.PointAt(i); OnPick(i); }
                return true;
            }
            case "autoplay": AutoPlay = a.Length == 0 || a[0] != "off"; return true;
            case "splitspeed":
                if (a.Length > 0 && float.TryParse(a[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float sec)) SplitSeconds = sec;
                return true;
            case "finish": FinishSplit(); return true;
        }

        return base.StoryCommand(verb, a);
    }
}
