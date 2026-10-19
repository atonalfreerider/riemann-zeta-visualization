using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Prime hunt on the Sacks spiral (n ≤ 400 laid flat, n at radius √n and angle τ√n). Every number shows its kind in the
/// games' colours, dimmed, except this round's candidates: silver mystery marbles with a ring. Point at the ones you
/// think are prime. A prime turns gold (points, and a streak bonus from three in a row); anything else shows its colour
/// and its building blocks as a marble fingerprint. Rounds walk through a variety of examples: the first primes, a prime
/// gap, Euler's prime arm k² + k + 41, the primes among prime-looking impostors (91 = 7 · 13, 119 = 7 · 17, …), twin
/// primes, and the squares and pronic numbers round 121.
/// </summary>
public class PrimeHuntGame : GameModel
{
    const int N = 400;
    const float K = 3f, Dot = 1.15f, Candidate = 1.7f;

    static readonly (string title, long[] candidates)[] Rounds =
    {
        ("The first primes", Range(2, 12)),
        ("A prime gap: 89 to 101", Range(88, 101)),
        ("Euler's arm k² + k + 41, and its neighbours", new long[] { 41, 42, 43, 45, 47, 49, 53, 55, 61, 63, 71, 75, 83, 85, 97, 99, 113, 115 }),
        ("Primes or impostors", new long[] { 101, 103, 119, 121, 127, 133, 137, 143, 149, 161, 163, 169, 173, 187, 191, 193, 197, 199 }),
        ("Twin primes near 300", Range(280, 316)),
        ("Round 121: squares and pronic numbers", Range(108, 133)),
    };

    static string Primes(int k) => k == 1 ? "1 prime" : $"{k} primes";

    static long[] Range(long a, long b)
    {
        long[] r = new long[b - a + 1];
        for (long i = a; i <= b; i++) r[i - a] = i;
        return r;
    }

    readonly MarbleBatch dots = new(1);
    readonly Vector3[] pos = new Vector3[N + 1];
    readonly HashSet<long> revealed = new();
    readonly List<LineRenderer> rings = new();
    int round = -1;
    int primesLeft;
    float advanceAt = -1f;
    bool dirty = true;

    public override string Id => "primehunt";
    public override string Title => "Prime hunt";
    public override string Short => "Primes";
    public override VRIcons.Id Icon => VRIcons.Id.Target;

    Vector3 Pos(long n)
    {
        float r = K * Mathf.Sqrt(n), a = Mathf.PI * 2f * Mathf.Sqrt(n);
        return O + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * r;
    }

    protected override void OnBuild()
    {
        for (int n = 1; n <= N; n++) pos[n] = Pos(n);
        Controls.Add(LabControl.Button("Next round", VRIcons.Id.NextLine, () => StartRound(round + 1), "N"));
        Controls.Add(LabControl.Button("Restart", VRIcons.Id.Replay, Restart, "Backspace"));
        AddCommonControls();
        Restart();
    }

    // each round frames its own candidates
    LabView roundView = new() { Focus = Vector3.zero, Distance = 150f, Yaw = -90f, Pitch = 64f, Fov = 50f };

    public override LabView Home => roundView;

    /// <summary>The fingerprint's place: left of the candidates, across a fifth of the view, marbles sized to the view.</summary>
    // lower left of the view (−y is down the screen here), so the volume it unpacks into has the left half above it
    Vector3 PrintSpot => roundView.Focus + Vector3.left * (roundView.Distance * .46f) + Vector3.down * (roundView.Distance * .215f) + Up * (roundView.Distance * .04f);
    float PrintScale => roundView.Distance * .014f;

    public override void OnEnter() => UpdateStatus();

    void Restart()
    {
        Score = Streak = BestStreak = Finds = Misses = 0;
        StartRound(0);
    }

    bool InRound(long n) => round >= 0 && round < Rounds.Length && System.Array.IndexOf(Rounds[round].candidates, n) >= 0;

    void StartRound(int r)
    {
        round = ((r % Rounds.Length) + Rounds.Length) % Rounds.Length;
        foreach (long n in Rounds[round].candidates) revealed.Remove(n);
        primesLeft = 0;
        foreach (long n in Rounds[round].candidates)
            if (NumberKind.IsPrime(n)) primesLeft++;
        advanceAt = -1f;
        HidePrint();
        Picks.Clear();
        foreach (long n in Rounds[round].candidates)
            Picks.Add(new Pickable { Position = pos[n] + Up * .3f, Value = n, Radius = Candidate, Kind = "number" });
        foreach (LineRenderer l in rings) if (l != null) Object.Destroy(l.gameObject);
        rings.Clear();
        foreach (long n in Rounds[round].candidates)
            rings.Add(LabDraw.Ring(Root, $"candidate {n}", pos[n], Candidate * .95f, .16f, new Color(.8f, .85f, 1f, .7f), Factory.SoftGlowIntensity, 32));
        // frame the candidates: their middle, far enough to hold them all with room for the fingerprint on the left
        Vector3 c = Vector3.zero;
        foreach (long n in Rounds[round].candidates) c += pos[n];
        c /= Rounds[round].candidates.Length;
        float extent = 0f;
        foreach (long n in Rounds[round].candidates) extent = Mathf.Max(extent, (pos[n] - c).magnitude);
        roundView = new LabView { Focus = c + Vector3.right * (extent * .35f + 6f), Distance = Mathf.Max(48f, extent * 3.4f + 20f), Yaw = -90f, Pitch = 64f, Fov = 50f };
        if (Lab != null && Lab.Active == this) Lab.ResetView(false);
        Prompt = $"Round {round + 1} of {Rounds.Length}:  {Rounds[round].title}.  Point at the primes:  {Primes(primesLeft)} to find";
        Note = Legend;
        dirty = true;
        UpdateStatus();
    }

    protected override string LabelFor(Pickable p) => revealed.Contains(p.Value) ? NumberKind.Describe(p.Value) : p.Value.ToString();

    public override void OnPick(int i)
    {
        Pickable p = Picks[i];
        long n = p.Value;
        Vector3 at = pos[n];
        ShowPrint(n, PrintSpot, PrintScale);
        if (revealed.Contains(n))
        {
            Note = NumberKind.Describe(n);
            UpdateStatus();
            return;
        }

        revealed.Add(n);
        dirty = true;
        if (NumberKind.IsPrime(n))
        {
            primesLeft--;
            Found(at, 10, Candidate);
            Note = $"{NumberKind.Describe(n)}:  one container of {n} marbles";
            if (primesLeft == 0)
            {
                Note += $"\nRound {round + 1} done:  every prime found";
                advanceAt = Time.time + 3f;
            }
        }
        else
        {
            Missed(at, 3, Candidate);
            Note = $"{NumberKind.Describe(n)}:  {NumberKind.NameOf(NumberKind.Classify(n))}.  Unpack it (U)";
        }

        Prompt = $"Round {round + 1} of {Rounds.Length}:  {Rounds[round].title}.  " + (primesLeft > 0 ? $"{Primes(primesLeft)} to find" : "all found");
        UpdateStatus();
    }

    protected override void OnTick(float dt)
    {
        if (advanceAt > 0f && Time.time >= advanceAt)
        {
            advanceAt = -1f;
            StartRound(round + 1);
        }

        if (dirty) Refill();
        dots.Draw(Matrix4x4.identity);
    }

    void Refill()
    {
        dots.Clear();
        Color silver = new(.82f, .85f, .92f);
        for (int n = 1; n <= N; n++)
        {
            bool cand = InRound(n);
            if (cand && !revealed.Contains(n))
            {
                dots.Add(pos[n], Candidate, silver, 1.1f);
                continue;
            }

            Color c = NumberKind.ColorOf(n);
            if (cand) dots.Add(pos[n], Candidate, c, NumberKind.IsPrime(n) ? 1.35f : 1.05f);
            else dots.Add(pos[n], Dot, Color.Lerp(c, new Color(.12f, .13f, .17f), .5f), 1f);
        }

        dirty = false;
    }

    public override bool OnKey(Keyboard k)
    {
        if (k.nKey.wasPressedThisFrame) { StartRound(round + 1); return true; }
        if (k.backspaceKey.wasPressedThisFrame) { Restart(); return true; }
        return CommonKey(k);
    }

    // for tests: pick a number of the round by value
    public bool PickValue(long n)
    {
        int i = Picks.FindIndex(p => p.Value == n);
        if (i < 0) return false;
        OnPick(i);
        return true;
    }

    public int Round => round;
    public void GoToRound(int r) => StartRound(r);
}
