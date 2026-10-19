using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// E8's spheres. E8's lattice points, shell by shell: shell n is a sphere of 240 σ₃(n) points (E8SphereField), as many
/// shells as the platform draws (about 340 000 points, shells 1–8, on the desktop; 57 000, shells 1–5, in VR). Three
/// rounds:
/// 1. Which shells are prime? The spheres start silver; point at the shells whose n is prime. A prime shell holds just
///    two cubes' worth, 240 (1 + p³), the fewest a shell its size can.
/// 2. Building blocks: point at a shell and its divisors stand beside it as cubes of marbles, d × d × d for each divisor d,
///    adding up to σ₃(n): every prime factor of n shows up as an extra cube. Its fingerprint shows the factors.
/// 3. Prime or impostor on E8's clock: the numbers up to 360 at their hours (n mod 30), the primes' eight gold hours
///    lit. Point at the impostors: the composites that sit on a gold hour (49, 77, 91, 119, 121, …).
/// </summary>
public class E8SpheresGame : GameModel
{
    const float Unit = 4.2f;
    const int ClockN = 360;
    E8SphereField field;
    readonly MarbleBatch cubes = new(1), clock = new(1);
    readonly HashSet<long> found = new();
    readonly List<TextBox> labels = new();
    readonly List<TextBox> hourLabels = new();
    int round, opened = -1;
    float nextAt = -1f;
    Vector3 clockC;

    public override string Id => "e8spheres";
    public override string Title => "E8 spheres";
    public override string Short => "E8 game";
    public override VRIcons.Id Icon => VRIcons.Id.Lattice;

    static readonly string[] RoundTitles = { "Which shells are prime?", "The building blocks of each shell", "Prime or impostor on E8's clock" };

    protected override void OnBuild()
    {
        long budget = VRMode.Enabled ? 60000 : 400000;
        field = E8SphereField.Create(Root, O + new Vector3(-100f, 30f, 0f) + Up * 26f, Unit, budget, 12);
        field.ColourOf = n => round == 0 && !found.Contains(n) ? new Color(.78f, .8f, .88f) : NumberKind.ColorOf(n);
        for (int n = 1; n <= field.Shells; n++)
        {
            TextBox l = LabDraw.Label(Root, n.ToString(), field.Centre(n) + Up * (field.Radius(n) + 4f), 40f, Color.white);
            labels.Add(l);
        }

        clockC = O + new Vector3(0f, -10f, 0f);
        for (int h = 0; h < 30; h++)
        {
            float a = h / 30f * Mathf.PI * 2f;
            Vector3 at = clockC + new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f) * 55f;
            hourLabels.Add(LabDraw.Label(Root, h.ToString(), at, 26f, GoldHour(h) ? NumberKind.Gold : new Color(.6f, .62f, .72f)));
        }

        Controls.Add(LabControl.Button("Next round", VRIcons.Id.NextLine, () => StartRound(round + 1), "N"));
        Controls.Add(LabControl.Button("Restart", VRIcons.Id.Replay, Restart, "Backspace"));
        AddCommonControls();
        Restart();
    }

    static bool GoldHour(long h) => h % 2 != 0 && h % 3 != 0 && h % 5 != 0;

    public override LabView Home => round < 2
        ? new LabView { Focus = field != null ? (field.Centre(1) + field.Centre(field.Shells)) * .5f + Up * 6f : O, Distance = 196f, Yaw = -90f, Pitch = 18f, Fov = 50f }
        : new LabView { Focus = clockC + new Vector3(0f, 9f, 0f) + Up * 2f, Distance = 165f, Yaw = -90f, Pitch = 74f, Fov = 50f };

    public override void OnEnter() => UpdateStatus();

    void Restart()
    {
        Score = Streak = BestStreak = Finds = Misses = 0;
        StartRound(0);
    }

    Vector3 ClockPos(long n)
    {
        long h = n % 30;
        float a = h / 30f * Mathf.PI * 2f, r = 10f + 3.2f * (n / 30f);
        return clockC + new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f) * r;
    }

    void StartRound(int r)
    {
        round = ((r % 3) + 3) % 3;
        found.Clear();
        opened = -1;
        nextAt = -1f;
        HidePrint();
        Picks.Clear();
        field.gameObject.SetActive(round < 2);
        foreach (TextBox l in labels) l.gameObject.SetActive(round < 2);
        foreach (TextBox l in hourLabels) l.gameObject.SetActive(round == 2);
        if (round < 2)
        {
            for (int n = 1; n <= field.Shells; n++)
                Picks.Add(new Pickable { Position = field.Centre(n), Value = n, Radius = field.Radius(n), Kind = "shell" });
            field.Highlight(-1);
        }
        else
        {
            for (long n = 31; n <= ClockN; n++)
                if (GoldHour(n % 30)) Picks.Add(new Pickable { Position = ClockPos(n) + Up * .3f, Value = n, Radius = 1.6f, Kind = "number" });
        }

        field.Refresh();
        Prompt = round switch
        {
            0 => $"Round 1 of 3:  which shells are prime?  {PrimeShells()} of the {field.Shells} are",
            1 => $"Round 2 of 3:  point at a shell to see its divisors as cubes, d × d × d",
            _ => $"Round 3 of 3:  the gold hours hold every prime past 5.  Find the {Impostors()} impostors",
        };
        Note = round == 0
            ? $"{field.Points:#,0} points in {field.Shells} shells.  A prime shell holds 240 (1 + p³):  two cubes".Replace(",", " ")
            : Legend;
        Lab.ResetView(false);
        UpdateStatus();
    }

    int PrimeShells()
    {
        int c = 0;
        for (int n = 1; n <= field.Shells; n++) if (NumberKind.IsPrime(n)) c++;
        return c;
    }

    static int Impostors()
    {
        int c = 0;
        for (long n = 31; n <= ClockN; n++) if (GoldHour(n % 30) && !NumberKind.IsPrime(n)) c++;
        return c;
    }

    protected override string LabelFor(Pickable p)
    {
        if (p.Kind == "shell")
        {
            string count = $"{E8Shells.Count((int)p.Value):#,0}".Replace(",", " ");
            return round == 0 && !found.Contains(p.Value) ? $"shell {p.Value}:  {count} points" : $"shell {NumberKind.Describe(p.Value)}:  {count} points";
        }

        return found.Contains(p.Value) ? NumberKind.Describe(p.Value) : $"{p.Value}  (hour {p.Value % 30})";
    }

    public override void OnPick(int i)
    {
        Pickable p = Picks[i];
        long n = p.Value;
        if (p.Kind == "shell") PickShell((int)n, p);
        else PickClock(n, p);
        UpdateStatus();
    }

    void PickShell(int n, Pickable p)
    {
        ShowPrint(n, p.Position + Up * (field.Radius(n) + 20f) + Vector3.left * 12f, 3.2f);
        string sigma = SigmaText(n);
        if (round == 0)
        {
            if (found.Contains(n)) { Note = sigma; return; }
            found.Add(n);
            field.Refresh();
            if (NumberKind.IsPrime(n))
            {
                Found(p.Position, 10, field.Radius(n));
                Note = $"shell {n} is prime:  {sigma}";
                if (FoundPrimes() == PrimeShells())
                {
                    Note += "\nEvery prime shell found.  Next:  their building blocks";
                    nextAt = Time.time + 3.5f;
                }
            }
            else
            {
                Missed(p.Position, 3, field.Radius(n));
                Note = $"{NumberKind.Describe(n)}.  {sigma}";
            }

            return;
        }

        // round 2: open the shell's divisor cubes
        if (opened != n)
        {
            opened = n;
            field.Highlight(n, .6f);
            if (!found.Contains(n))
            {
                found.Add(n);
                Found(p.Position, NumberKind.Divisors(n).Count, field.Radius(n));
            }
        }

        Note = $"{NumberKind.Describe(n)}:  {sigma}";
    }

    int FoundPrimes()
    {
        int c = 0;
        foreach (long n in found) if (NumberKind.IsPrime(n)) c++;
        return c;
    }

    static string SigmaText(int n)
    {
        List<long> d = NumberKind.Divisors(n);
        List<string> parts = new();
        foreach (long x in d) parts.Add(x == 1 ? "1" : $"{x}³");
        string total = $"{E8Shells.Count(n):#,0}".Replace(",", " ");
        return $"σ<sub>3</sub>({n}) = {string.Join(" + ", parts)} = {NumberKind.Sigma3(n)}:  {total} points";
    }

    void PickClock(long n, Pickable p)
    {
        ShowPrint(n, clockC + new Vector3(-104f, 30f, 0f) + Up * 4f, 2.2f);
        if (found.Contains(n)) { Note = NumberKind.Describe(n); return; }
        found.Add(n);
        if (!NumberKind.IsPrime(n))
        {
            Found(p.Position, 10, 1.8f);
            int left = Impostors() - FoundImpostors();
            Note = $"{NumberKind.Describe(n)}:  an impostor on hour {n % 30}.  {left} to go";
            if (left == 0) Note += "\nEvery impostor found";
        }
        else
        {
            Missed(p.Position, 3, 1.8f);
            Note = $"{n} is prime:  it belongs on its gold hour ({n % 30})";
        }
    }

    int FoundImpostors()
    {
        int c = 0;
        foreach (long n in found) if (!NumberKind.IsPrime(n)) c++;
        return c;
    }

    protected override void OnTick(float dt)
    {
        if (nextAt > 0f && Time.time >= nextAt)
        {
            nextAt = -1f;
            StartRound(round + 1);
        }

        cubes.Clear();
        if (round == 1 && opened > 0) DrawCubes(opened);
        cubes.Draw(Matrix4x4.identity);
        if (round == 2) DrawClock();
    }

    /// <summary>Shell n's divisors as cubes of marbles in a row under it, d × d × d each, in d's own colour.</summary>
    void DrawCubes(int n)
    {
        List<long> ds = NumberKind.Divisors(n);
        float m = 2f, x = 0f;
        float total = 0f;
        foreach (long d in ds) total += d * m + 3f;
        Vector3 start = field.Centre(n) + Vector3.left * (total * .5f) + Up * -(field.Radius(n) + 6f) + Vector3.down * 4f;
        foreach (long d in ds)
        {
            Color c = d == 1 ? NumberKind.Unit : NumberKind.ColorOf(d);
            for (long a = 0; a < d; a++)
                for (long b = 0; b < d; b++)
                    for (long e = 0; e < d; e++)
                        cubes.Add(start + new Vector3(x + a * m, e * m, 0f) + Up * (-b * m), m * .88f, c, 1.1f);
            x += d * m + 3f;
        }
    }

    void DrawClock()
    {
        clock.Clear();
        for (long n = 1; n <= ClockN; n++)
        {
            Color c = NumberKind.ColorOf(n);
            bool cand = n > 30 && GoldHour(n % 30);
            // every candidate is silver until picked, primes and impostors alike
            if (cand && !found.Contains(n)) c = new Color(.82f, .85f, .92f);
            else if (!cand) c = Color.Lerp(c, new Color(.12f, .13f, .17f), .55f);
            clock.Add(ClockPos(n), cand ? 1.5f : 1f, c, cand ? 1.2f : 1f);
        }

        clock.Draw(Matrix4x4.identity);
    }

    public override bool OnKey(Keyboard k)
    {
        if (k.nKey.wasPressedThisFrame) { StartRound(round + 1); return true; }
        if (k.backspaceKey.wasPressedThisFrame) { Restart(); return true; }
        return CommonKey(k);
    }

    // for tests
    public void GoToRound(int r) => StartRound(r);
    public int Shells => field != null ? field.Shells : 0;

    public bool PickValue(long n, string kind)
    {
        int i = Picks.FindIndex(x => x.Value == n && x.Kind == kind);
        if (i < 0) return false;
        OnPick(i);
        return true;
    }
}
