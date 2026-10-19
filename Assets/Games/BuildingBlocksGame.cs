using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Building blocks: a number stands as its raw volume, one marble per unit, in its own colour (grey composite, deep red
/// p², blue pronic). Below it, a tray of gold prime containers (2 … 37). Point at a prime: if it divides what is left, the
/// marbles split into that many equal groups and one group stays (the prime joins the fingerprint string on the left); if
/// it does not, the marbles try to split and the leftover ones shine red. When the rest is itself prime (even past 37),
/// point at the block to say so. When nothing is left the fingerprint is the whole number: it unpacks into the full
/// volume and packs back into its shorthand. A variety of examples: squares of primes, pronic numbers, products of three
/// primes, powers of 2, 360 and 1001.
/// </summary>
public class BuildingBlocksGame : GameModel
{
    static readonly long[] Examples = { 12, 49, 30, 91, 56, 360, 121, 105, 72, 143, 128, 210, 169, 90, 1001, 246, 221, 180, 77, 1024 };
    static readonly long[] Tray = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37 };

    const float M = 1.55f;                       // a marble, and the pitch of the volume
    readonly MarbleBatch marbles = new(1);
    readonly List<TextBox> trayLabels = new();
    int example = -1;
    long target, rest, found;
    // the split animation: rest → rest / p (or a failed split with a remainder)
    long splitFrom, splitP;
    bool splitOk;
    float splitT = -1f;
    float nextAt = -1f, unpackAt = -1f, packAt = -1f;
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
    Vector3 TrayAt(int i) => Wall(-50f + i * 9.4f, 1f);

    protected override void OnBuild()
    {
        List<Pickable> picks = new();
        for (int i = 0; i < Tray.Length; i++)
        {
            Vector3 c = TrayAt(i) + Up * 3f;
            picks.Add(new Pickable { Position = c, Value = Tray[i], Radius = 4f, Kind = "prime" });
            trayLabels.Add(LabDraw.Label(Root, Tray[i].ToString(), TrayAt(i) + Up * -2.6f, 40f, NumberKind.Gold));
        }

        Picks.AddRange(picks);
        restLabel = LabDraw.Label(Root, "", VolumeCentre, 48f, Color.white);
        Controls.Add(LabControl.Button("Next number", VRIcons.Id.NextLine, () => StartExample(example + 1), "N"));
        Controls.Add(LabControl.Button("Restart", VRIcons.Id.Replay, Restart, "Backspace"));
        AddCommonControls();
        Restart();
    }

    // a little from the right and above, so the volume shows its depth
    public override LabView Home => new() { Focus = Wall(-4f, 19f), Distance = 94f, Yaw = -76f, Pitch = 16f, Fov = 50f };

    public override void OnEnter() => UpdateStatus();

    void Restart()
    {
        Score = Streak = BestStreak = Finds = Misses = 0;
        StartExample(0);
    }

    void StartExample(int e)
    {
        example = ((e % Examples.Length) + Examples.Length) % Examples.Length;
        target = rest = Examples[example];
        found = 1;
        splitT = -1f;
        nextAt = unpackAt = packAt = -1f;
        HidePrint();
        // the block itself is pickable too: "what is left is prime"
        Picks.RemoveAll(p => p.Kind == "rest");
        Picks.Add(new Pickable { Position = VolumeCentre, Value = 0, Radius = 9f, Kind = "rest" });
        Prompt = $"Number {example + 1} of {Examples.Length}:  split {target} into its prime building blocks" +
                 (LargestFactor(target) > 37 ? ";  the block, once the rest is prime" : "");
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
                Found(VolumeCentre, 8, 9f);
                Note = $"{rest} is prime:  it is its own building block";
                Divide(rest, true);
            }
            else
            {
                Missed(VolumeCentre, 2, 9f);
                Note = $"{rest} is not prime:  {NumberKind.Shorthand(rest)}.  Keep splitting";
            }

            UpdateStatus();
            return;
        }

        long q = p.Value;
        if (rest % q == 0)
        {
            Found(p.Position, 5, 3.4f);
            Note = $"{rest} ÷ {q} = {rest / q}:  {q} equal groups, one stays";
            Divide(q, true);
        }
        else
        {
            Missed(p.Position, 2, 3.4f);
            splitFrom = rest;
            splitP = q;
            splitOk = false;
            splitT = 0f;
            Note = $"{q} does not divide {rest}:  {q} groups leave {rest % q} over";
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
        ShowPrint(found, PrintAnchor, 2.1f);
        if (rest == 1)
        {
            Score += 10;
            Note = $"{target} = {NumberKind.Shorthand(target)}:  every building block found.  Unpack it (U)";
            if (AutoPlay) unpackAt = Time.time + 2.2f;
        }
    }

    protected override void OnTick(float dt)
    {
        if (splitT >= 0f)
        {
            splitT += dt / 1.6f;
            if (splitT >= 1f) splitT = -1f;
        }

        if (unpackAt > 0f && Time.time >= unpackAt)
        {
            unpackAt = -1f;
            Print?.Unpack();
            packAt = Time.time + (Print != null ? Print.Steps * Print.StepSeconds + 2.5f : 3f);
        }

        if (packAt > 0f && Time.time >= packAt)
        {
            packAt = -1f;
            Print?.Pack();
            nextAt = Time.time + (Print != null ? Print.Steps * Print.StepSeconds + 1.5f : 2f);
        }

        if (nextAt > 0f && Time.time >= nextAt)
        {
            nextAt = -1f;
            StartExample(example + 1);
        }

        Draw();
    }

    /// <summary>A block of m marbles about its centre: rows of up to `cols`, stacked up, then deeper.</summary>
    static Vector3 BlockAt(long i, long m, int cols)
    {
        int rows = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(m / (float)cols)));
        int perLayer = cols * rows;
        long layer = i / perLayer, r = (i % perLayer) / cols, c = i % cols;
        int layers = Mathf.Max(1, Mathf.CeilToInt(m / (float)perLayer));
        int usedCols = (int)Mathf.Min(cols, m);
        return new Vector3((c - (usedCols - 1) * .5f) * M, (layer - (layers - 1) * .5f) * M, 0f) + Up * ((r - (rows - 1) * .5f) * M);
    }

    static int ColsFor(long m) => Mathf.Clamp(Mathf.CeilToInt(Mathf.Pow(m, 1f / 3f) * 1.6f), 1, 16);

    void Draw()
    {
        marbles.Clear();
        // the tray: each prime's container, its marbles in rows of five
        for (int t = 0; t < Tray.Length; t++)
        {
            long p = Tray[t];
            // as in the fingerprints: up to 7 marbles in one row, larger primes in rows of five
            int row = p <= 7 ? (int)p : 5;
            float s = p > 13 ? 1f : p > 5 ? 1.1f : 1.3f;
            for (int i = 0; i < p; i++)
            {
                int cols = Mathf.Min(row, (int)p);
                Vector3 at = TrayAt(t) + new Vector3((i % row - (cols - 1) * .5f) * s * 1.1f, 0f, 0f) + Up * ((i / row) * s * 1.1f + .4f);
                marbles.Add(at, s, NumberKind.Gold, 1.15f);
            }
        }

        Color own = NumberKind.ColorOf(target);
        if (splitT >= 0f)
        {
            // rest marbles regroup into p groups side by side; then the other groups fly off to the string
            long from = splitFrom, q = splitP, per = from / q;
            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(splitT * 2f));
            float w = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(splitT * 2f - 1f));
            int colsFrom = ColsFor(from), colsPer = ColsFor(System.Math.Max(1L, per));
            float groupW = (Mathf.Min(colsPer, per) + 1.2f) * M;
            for (long i = 0; i < from; i++)
            {
                Vector3 a = VolumeCentre + BlockAt(i, from, colsFrom);
                long g = splitOk ? i % q : i % q, k = i / q;
                bool leftover = !splitOk && i >= per * q;
                Vector3 b = leftover
                    ? VolumeCentre + new Vector3((q * .5f + 1f) * groupW, 0f, 0f) + BlockAt(i - per * q, System.Math.Max(1L, from - per * q), 3)
                    : VolumeCentre + new Vector3((g - (q - 1) * .5f) * groupW, 0f, 0f) + BlockAt(k, System.Math.Max(1L, per), colsPer);
                Vector3 pos = Vector3.Lerp(a, b, u);
                Color c = leftover ? Color.Lerp(own, NumberKind.DeepRed, u) : own;
                float size = 1f;
                if (splitOk && g != 0)
                {
                    // the other groups go to the fingerprint and fade
                    pos = Vector3.Lerp(pos, PrintAnchor + Up * 4f, w);
                    size = 1f - w;
                }
                else if (splitOk) pos = Vector3.Lerp(pos, VolumeCentre + BlockAt(k, System.Math.Max(1L, per), ColsFor(System.Math.Max(1L, per))), w);
                else if (splitT > .5f) pos = Vector3.Lerp(b, a, w);
                marbles.Add(pos, size * M * .92f, c, 1.08f);
            }
        }
        else if (rest > 1)
        {
            int cols = ColsFor(rest);
            for (long i = 0; i < rest; i++) marbles.Add(VolumeCentre + BlockAt(i, rest, cols), M * .92f, own, 1.08f);
        }

        if (restLabel != null)
        {
            restLabel.TextField.text = rest > 1 && splitT < 0f ? $"{rest}" : "";
            restLabel.transform.position = VolumeCentre + Up * (BlockHeight(rest) * .5f + 4f);
        }

        marbles.Draw(Matrix4x4.identity);
    }

    float BlockHeight(long m)
    {
        int cols = ColsFor(m);
        int rows = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(m / (float)cols)));
        return rows * M;
    }

    public override bool OnKey(Keyboard k)
    {
        if (k.nKey.wasPressedThisFrame) { StartExample(example + 1); return true; }
        if (k.backspaceKey.wasPressedThisFrame) { Restart(); return true; }
        return CommonKey(k);
    }

    // for tests
    public void GoToExample(int e) => StartExample(e);
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
}
