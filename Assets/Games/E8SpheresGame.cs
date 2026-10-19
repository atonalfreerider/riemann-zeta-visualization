using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// E8's clock, to explore. Two views of the numbers, switched with "Shells":
/// * The clock (the default): every n ≤ 600 on E8's wheel (hour n mod 30, radius s√n), standing as a stack of its prime
///   factors, one gold piece as tall as log p each (FunnelView): the logarithmic stacks. Touch any column and its path back
///   plays down the staircase: down its top piece (÷ its smallest prime), across at that height to the column of what is
///   left, and so on down to 1. Every landing is named in exponent notation where that says more (360 = 2³ · 3² · 5, then
///   180 = 2² · 3² · 5 …), the pieces are tallied in the strip, and the number's fingerprint floats above the stacks. The
///   eight gold spokes are the hours of E8's exponents (1, 7, 11, 13, 17, 19, 23, 29): every prime past 5 stands on one.
///   Powers lays out where the runs of one prime go (2, 4, 8 … up the spiral of 2^k, each labelled with its exponent).
/// * The shells: E8's lattice points shell by shell, shell n a sphere of 240 σ₃(n) points; touch a shell and its divisors
///   stand under it as cubes of marbles, d × d × d, adding up to σ₃(n).
/// Turn the model with a drag (one finger on a touch screen), zoom with the wheel or a pinch; in VR pinch it to move and
/// turn it. Nothing is scored: every column and every shell can be touched at any time.
/// </summary>
public class E8SpheresGame : GameModel
{
    const int Count = 600;
    const float Unit = 4.2f;
    FunnelView funnel;
    E8SphereField field;
    readonly MarbleBatch cubes = new(1);
    readonly List<TextBox> shellLabels = new();
    bool shells;
    int opened = -1;

    public override string Id => "e8spheres";
    public override string Title => "E8's clock";
    public override string Short => "E8 clock";
    public override VRIcons.Id Icon => VRIcons.Id.Clock;

    protected override void OnBuild()
    {
        funnel = new FunnelView(Root, O, Count, 2.2f, 5f);
        funnel.Stepped += lit =>
        {
            if (!shells) Lab.SetStrip(funnel.Selected, lit);
        };
        long budget = VRMode.Enabled ? 60000 : 400000;
        field = E8SphereField.Create(Root, O + new Vector3(0f, 260f, 0f) + Up * 26f, Unit, budget, 12);
        for (int n = 1; n <= field.Shells; n++)
            shellLabels.Add(LabDraw.Label(Root, n.ToString(), field.Centre(n) + Up * (field.Radius(n) + 4f), 40f, Color.white));

        Controls.Add(LabControl.Button("n − 1", VRIcons.Id.PrevLine, () => Go(funnel.Selected - 1), "←"));
        Controls.Add(LabControl.Button("n + 1", VRIcons.Id.NextLine, () => Go(funnel.Selected + 1), "→"));
        Controls.Add(LabControl.Button("n − 30", VRIcons.Id.Down, () => Go(funnel.Selected - 30), "↓"));
        Controls.Add(LabControl.Button("n + 30", VRIcons.Id.Up, () => Go(funnel.Selected + 30), "↑"));
        Controls.Add(LabControl.Button("Replay", VRIcons.Id.Replay, () => funnel.Replay(), "Space"));
        Controls.Add(LabControl.Toggle("Powers", VRIcons.Id.Curves, () => funnel.PowersOn, () => funnel.SetPowers(!funnel.PowersOn), "K"));
        Controls.Add(LabControl.Toggle("Shells", VRIcons.Id.Lattice, () => shells, () => SetShells(!shells), "V"));
        AddCommonControls();
        SetShells(false);
    }

    public override LabView Home => shells
        ? new LabView { Focus = (field.Centre(1) + field.Centre(field.Shells)) * .5f + Up * 6f, Distance = 196f, Yaw = -90f, Pitch = 18f, Fov = 50f }
        : StoryFramed(new LabView { Focus = O + funnel.Up(Mathf.Log(Count) * .62f), Distance = 196f, Yaw = -62f, Pitch = 40f, Fov = 50f }, 1.15f, .03f);

    public override void OnEnter()
    {
        if (!shells && funnel.Selected < 2) Go(360);
        UpdateStatus();
    }

    void SetShells(bool on)
    {
        shells = on;
        HidePrint();
        Picks.Clear();
        opened = -1;
        field.gameObject.SetActive(on);
        foreach (TextBox l in shellLabels) l.gameObject.SetActive(on);
        if (on)
        {
            for (int n = 1; n <= field.Shells; n++)
                Picks.Add(new Pickable { Position = field.Centre(n), Value = n, Radius = field.Radius(n), Kind = "shell" });
            field.Highlight(-1);
            Lab.SetStrip(0, 0);
            Prompt = "Touch a shell:  its divisors stand under it as cubes, adding up to its points";
            Note = $"{field.Points:#,0} points in {field.Shells} shells:  shell n holds 240 σ<sub>3</sub>(n)".Replace(",", " ");
        }
        else
        {
            for (int n = 2; n <= Count; n++)
                Picks.Add(new Pickable { Position = funnel.Top(n) + Up * .3f, Value = n, Radius = 1.6f, Kind = "number" });
            Prompt = "Touch any column:  its path back steps down the staircase to its primes";
            Note = Legend;
            if (funnel.Selected >= 2) Go(funnel.Selected);
        }

        if (Lab != null) Lab.ResetView(false);
        UpdateStatus();
    }

    // the column whose path is showing is named by the path itself
    protected override string LabelFor(Pickable p) => p.Kind == "shell"
        ? $"shell {p.Value}:  {Group(E8Shells.Count((int)p.Value))} points"
        : p.Value == funnel.Selected ? "" : $"{NumberKind.Describe(p.Value)}   (hour {p.Value % 30})";

    public override void OnPick(int i)
    {
        Pickable p = Picks[i];
        if (p.Kind == "shell") OpenShell((int)p.Value, p);
        else
        {
            Light(p.Position, p.Value, 2.2f);
            Go((int)p.Value);
        }
    }

    /// <summary>n's column: its path back plays, its fingerprint shows, the board names its blocks and its hour.</summary>
    void Go(int n)
    {
        if (shells) return;
        n = Mathf.Clamp(n, 2, Count);
        funnel.Select(n, true);
        ShowPrint(n, PrintPlace(n), 2f);
        List<(long p, int k)> f = NumberKind.Factor(n);
        int drops = 0;
        foreach ((long _, int k) in f) drops += k;
        string path = NumberKind.IsPrime(n)
            ? "one drop of log " + n + " to 1"
            : $"{drops} drops down the staircase:  " + string.Join(", ", Drops(n));
        Note = $"{NumberKind.Describe(n)}  (hour {n % 30}{(GoldHour(n % 30) ? ", a gold hour" : "")}).  {path}";
        UpdateStatus();
    }

    /// <summary>The primes n drops by on its way back, smallest first, as the staircase takes them.</summary>
    static IEnumerable<string> Drops(int n)
    {
        while (n > 1)
        {
            int p = LabMath.Spf(n);
            yield return $"÷{p}";
            n /= p;
        }
    }

    static bool GoldHour(long h) => h % 2 != 0 && h % 3 != 0 && h % 5 != 0;

    /// <summary>Above the stacks, its string centred across the view, so it reads from wherever the model is turned.</summary>
    Vector3 PrintPlace(int n)
    {
        Camera cam = Camera.main;
        Vector3 right = cam != null ? cam.transform.right : Vector3.right;
        float half = Print != null ? Print.ShorthandSize.x * 2f * .5f : 10f;
        return O + funnel.Up(Mathf.Log(Count) + 7.5f) - right * half;
    }

    void OpenShell(int n, Pickable p)
    {
        opened = n;
        field.Highlight(n, .6f);
        Light(p.Position, n, field.Radius(n));
        ShowPrint(n, p.Position + Up * (field.Radius(n) + 20f) + Vector3.left * 12f, 3.2f);
        Note = $"shell {NumberKind.Describe(n)}:  {SigmaText(n)}";
        UpdateStatus();
    }

    static string SigmaText(int n)
    {
        List<long> d = NumberKind.Divisors(n);
        List<string> parts = new();
        foreach (long x in d) parts.Add(x == 1 ? "1" : $"{x}³");
        return $"σ<sub>3</sub>({n}) = {string.Join(" + ", parts)} = {NumberKind.Sigma3(n)}:  {Group(E8Shells.Count(n))} points";
    }

    static string Group(long v) => v.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(",", " ");

    protected override void OnTick(float dt)
    {
        if (!shells)
        {
            funnel.Tick(dt);
            funnel.SetDim(funnel.PowersOn ? .3f : .55f);
            if (Print != null) MovePrint(PrintPlace(funnel.Selected));
        }

        cubes.Clear();
        if (shells && opened > 0) DrawCubes(opened);
        cubes.Draw(Matrix4x4.identity);
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

    public override bool OnKey(Keyboard k)
    {
        if (!shells)
        {
            if (k.leftArrowKey.wasPressedThisFrame) { Go(funnel.Selected - 1); return true; }
            if (k.rightArrowKey.wasPressedThisFrame) { Go(funnel.Selected + 1); return true; }
            if (k.downArrowKey.wasPressedThisFrame) { Go(funnel.Selected - 30); return true; }
            if (k.upArrowKey.wasPressedThisFrame) { Go(funnel.Selected + 30); return true; }
            if (k.spaceKey.wasPressedThisFrame) { funnel.Replay(); return true; }
            if (k.kKey.wasPressedThisFrame) { funnel.SetPowers(!funnel.PowersOn); return true; }
        }

        if (k.vKey.wasPressedThisFrame) { SetShells(!shells); return true; }
        return CommonKey(k);
    }

    public override bool StoryCommand(string verb, string[] a)
    {
        switch (verb)
        {
            case "shells": SetShells(a.Length == 0 || a[0] != "off"); return true;
            case "powers": funnel.SetPowers(a.Length == 0 || a[0] != "off"); return true;
            case "replay": funnel.Replay(); return true;
        }

        return base.StoryCommand(verb, a);
    }

    // for tests
    public int Shells => field != null ? field.Shells : 0;
    public bool ShellsView => shells;
    public void ShowShells(bool on) => SetShells(on);
    public FunnelView Funnel => funnel;
}
