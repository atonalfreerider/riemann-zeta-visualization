using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// "The paths back" (video 24) as a model to walk around: the factor funnel on E8's wheel for n ≤ 720. Pick a column (click it,
/// or touch its top in VR) and its path back plays: one drop per prime factor, the exponents tallied in the strip. The tree
/// shows every number's first step at once, one prime at a time if asked; the powers show the spirals of p^k; the readout
/// names the hour (and, for a prime from 7 on, how its Frobenius moves E8's four Coxeter planes) and the E8 shell of norm 2n.
/// </summary>
public class PathsModel : LabModel
{
    public const int Count = 720;
    FunnelView funnel;
    float spiralTarget, spiral;
    readonly System.Random random = new(30);

    public FunnelView Funnel => funnel;
    public override string Id => "paths";
    public override string Title => "The paths back";
    public override string Short => "Paths";
    public override VRIcons.Id Icon => VRIcons.Id.Funnel;

    protected override void OnBuild()
    {
        funnel = new FunnelView(Root, O, Count, 2.2f, 5f);
        funnel.Stepped += lit =>
        {
            Lab.SetStrip(funnel.Selected, lit);
            UpdateStatus();
        };
        Controls.Add(LabControl.Button("n − 1", VRIcons.Id.PrevLine, () => Go(funnel.Selected - 1), "←"));
        Controls.Add(LabControl.Button("n + 1", VRIcons.Id.NextLine, () => Go(funnel.Selected + 1), "→"));
        Controls.Add(LabControl.Button("n − 30", VRIcons.Id.Down, () => Go(funnel.Selected - 30), "↓"));
        Controls.Add(LabControl.Button("n + 30", VRIcons.Id.Up, () => Go(funnel.Selected + 30), "↑"));
        Controls.Add(LabControl.Button("Step down", VRIcons.Id.Stairs, StepDown, "PgDn"));
        Controls.Add(LabControl.Button("Replay", VRIcons.Id.Replay, () => funnel.Replay(), "Space"));
        Controls.Add(new LabControl
        {
            Label = "Tree", Icon = VRIcons.Id.Chords, Clicked = () => { funnel.CycleTree(); UpdateStatus(); }, On = () => funnel.TreeMode != 0,
            DynamicLabel = () => funnel.TreeMode switch { 0 => "Tree", 1 => "Tree: all", _ => $"Tree: ÷{funnel.TreeMode}" }, Keys = "G"
        });
        Controls.Add(LabControl.Toggle("Powers", VRIcons.Id.Curves, () => funnel.PowersOn, () => funnel.SetPowers(!funnel.PowersOn), "K"));
        Controls.Add(LabControl.Toggle("Primes only", VRIcons.Id.Sacks, () => funnel.PrimesOnly, () => funnel.SetPrimesOnly(!funnel.PrimesOnly), "J"));
        Controls.Add(LabControl.Toggle("Sacks spiral", VRIcons.Id.Twist, () => spiralTarget > .5f, () => spiralTarget = spiralTarget > .5f ? 0f : 1f, "L"));
        Controls.Add(LabControl.Button("Random n", VRIcons.Id.Dial, () => Go(2 + random.Next(Count - 1)), "R"));
        Controls.Add(LabControl.Button("Reset view", VRIcons.Id.Reset, () => Lab.ResetView(false), "Home"));
    }

    public override LabView Home => new()
    {
        Focus = O + funnel.Up(Mathf.Log(Count) * .36f), Distance = 150f, Yaw = -62f, Pitch = 38f, Fov = 50f
    };

    public override void OnEnter()
    {
        if (funnel.Selected < 2) funnel.Select(360, true);
        else
        {
            Lab.SetStrip(funnel.Selected, funnel.StepsShown);
            UpdateStatus();
        }
    }

    public override void Tick(float dt)
    {
        funnel.Tick(dt);
        funnel.SetDim(funnel.TreeMode != 0 || funnel.PowersOn ? .3f : .55f);
        if (Mathf.Abs(spiral - spiralTarget) > 1e-3f)
        {
            spiral = Mathf.MoveTowards(spiral, spiralTarget, dt * .5f);
            funnel.SetSacks(Mathf.SmoothStep(0f, 1f, spiral));
            UpdateStatus();
        }
    }

    void Go(int n)
    {
        n = Mathf.Clamp(n, 2, Count);
        funnel.Select(n, true);
    }

    void StepDown()
    {
        int n = funnel.Selected;
        if (n < 2) return;
        int m = n / LabMath.Spf(n);
        if (m >= 2) Go(m);
    }

    public override int PickCount => funnel.PickCount;

    public override Vector3 PickPoint(int i)
    {
        int n = funnel.PickNumber(i);
        return funnel.Pickable(n) ? funnel.Top(n) : new Vector3(1e9f, 1e9f, 1e9f);
    }

    public override string PickLabel(int i)
    {
        int n = funnel.PickNumber(i);
        return LabMath.IsPrime(n) ? $"{n}:  prime" : $"{n} = {LabMath.FactorString(n)}";
    }

    public override void OnPick(int i) => Go(funnel.PickNumber(i));

    public override bool OnKey(Keyboard k)
    {
        bool used = true;
        if (k.leftArrowKey.wasPressedThisFrame) Go(funnel.Selected - 1);
        else if (k.rightArrowKey.wasPressedThisFrame) Go(funnel.Selected + 1);
        else if (k.downArrowKey.wasPressedThisFrame) Go(funnel.Selected - 30);
        else if (k.upArrowKey.wasPressedThisFrame) Go(funnel.Selected + 30);
        else if (k.pageDownKey.wasPressedThisFrame || k.backspaceKey.wasPressedThisFrame) StepDown();
        else if (k.spaceKey.wasPressedThisFrame) funnel.Replay();
        else if (k.gKey.wasPressedThisFrame) { funnel.CycleTree(); UpdateStatus(); }
        else if (k.kKey.wasPressedThisFrame) funnel.SetPowers(!funnel.PowersOn);
        else if (k.jKey.wasPressedThisFrame) funnel.SetPrimesOnly(!funnel.PrimesOnly);
        else if (k.lKey.wasPressedThisFrame) spiralTarget = spiralTarget > .5f ? 0f : 1f;
        else if (k.rKey.wasPressedThisFrame) Go(2 + random.Next(Count - 1));
        else used = false;
        return used;
    }

    /// <summary>The board: the factorization, the step (or the path), and what the hour and the E8 shell say about n.</summary>
    void UpdateStatus()
    {
        int n = funnel.Selected;
        if (n < 2) return;
        Lab.SetStatus(Describe(n, funnel.StepsShown, funnel.SacksMix > .5f));
    }

    public static string Describe(int n, int shown, bool spiral)
    {
        bool prime = LabMath.IsPrime(n);
        string head = prime ? $"{n}:  prime" : $"{n} = {LabMath.FactorString(n)}";
        var steps = LabMath.Factorize(n);
        string line;
        if (shown >= 1 && shown < steps.Count)
        {
            int rest = n, found = 1;
            for (int i = 0; i < shown; i++)
            {
                rest /= steps[i];
                found *= steps[i];
            }

            line = $"÷{steps[shown - 1]}  →  {rest}      found so far:  {LabMath.FactorString(found)}";
        }
        else line = prime ? "one drop to 1" : "path back:  " + string.Join(" ", steps.ConvertAll(p => "÷" + p)) + "  →  1";

        string place = spiral ? "Sacks layout:  angle 2π√n" : Hour(n);
        string shell = prime
            ? $"E8 shell of norm {2 * n}:  240·(1 + {n}³) vectors, the fewest for any n"
            : $"E8 shell of norm {2 * n}:  240·σ<sub>3</sub>({n}) = {Group(240L * LabMath.Sigma3(n))} vectors";
        return head + "\n" + Small(line + "      " + place) + "\n" + Small(shell);
    }

    /// <summary>The hour of n on E8's wheel, and for a prime from 7 on how its Frobenius moves E8's four Coxeter planes.</summary>
    public static string Hour(int n)
    {
        int h = n % 30;
        if (LabMath.Gcd(h, 30) != 1) return $"hour {h}:  not an E8 exponent";
        if (!LabMath.IsPrime(n))
        {
            // the hour of a product is the product of the hours: the path back walks the group of E8's exponents
            var hours = LabMath.Factorize(n).ConvertAll(p => (p % 30).ToString());
            return $"hour {h} = {string.Join("·", hours)} (mod 30):  E8 exponent";
        }
        int r = h > 15 ? 30 - h : h;
        string action = r switch
        {
            1 => "Frobenius fixes E8's planes",
            11 => "Frobenius swaps E8's planes 1↔11, 7↔13",
            7 => "Frobenius cycles E8's planes 1→7→11→13",
            _ => "Frobenius cycles E8's planes 1→13→11→7",
        };
        return $"hour {h}:  {action}";
    }

    public static string Small(string s) => "<size=58%>" + s + "</size>";

    public static string Group(long v) => v.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(",", " ");
}
