using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The number spiral, to explore: the Sacks spiral of 1 … 400 (n at radius √n and angle τ√n), every number in its colour
/// (gold prime, deep red p², blue pronic, grey composite). Touch any number: it lights, and its fingerprint shows what it
/// is made of; pull the fingerprint apart (drag it to the right, or pinch it in VR and pull) to see the number rebuilt from
/// its primes, push it back to pack it into its shorthand. Step to the neighbours (n ± 1). "Look at" lights one family at
/// a time, as places to look rather than answers to find: the primes, the twin primes, Euler's k² + k + 41, the squares
/// and the pronic numbers (each on a line of its own), or the multiples of the number last touched.
/// </summary>
public class PrimeHuntGame : GameModel
{
    const int N = 400;
    const float K = 3f, Dot = 1.2f, Big = 1.55f;
    const int Looks = 6;

    readonly MarbleBatch dots = new(1);
    readonly Vector3[] pos = new Vector3[N + 1];
    LineRenderer marker;
    int look, selected;
    bool dirty = true;

    public override string Id => "primehunt";
    public override string Title => "The number spiral";
    public override string Short => "Spiral";
    public override VRIcons.Id Icon => VRIcons.Id.Sacks;

    Vector3 Pos(long n)
    {
        float r = K * Mathf.Sqrt(n), a = Mathf.PI * 2f * Mathf.Sqrt(n);
        return O + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * r;
    }

    protected override void OnBuild()
    {
        for (int n = 1; n <= N; n++)
        {
            pos[n] = Pos(n);
            Picks.Add(new Pickable { Position = pos[n] + Up * .3f, Value = n, Radius = 1.4f, Kind = "number" });
        }

        marker = LabDraw.Ring(Root, "touched", O, 2.1f, .22f, Color.white, Factory.GlowIntensity, 40);
        marker.gameObject.SetActive(false);
        Controls.Add(LabControl.Button("n − 1", VRIcons.Id.PrevLine, () => Step(-1), "←"));
        Controls.Add(LabControl.Button("n + 1", VRIcons.Id.NextLine, () => Step(1), "→"));
        Controls.Add(new LabControl
        {
            Label = "Look at", Icon = VRIcons.Id.Eye, Clicked = () => SetLook(look + 1), On = () => look != 0,
            DynamicLabel = () => look switch { 0 => "Look at", 1 => "primes", 2 => "twins", 3 => "k² + k + 41", 4 => "squares", _ => $"× {Mathf.Max(2, selected)}" },
            Keys = "L",
        });
        AddCommonControls();
        Prompt = "Touch any number to see what it is made of.  Drag its fingerprint apart";
        Note = Legend;
    }

    // the whole spiral, a little tilted, with room on the left for the fingerprint
    public override LabView Home => StoryFramed(new LabView { Focus = O + new Vector3(-14f, -4f, 0f), Distance = 178f, Yaw = -90f, Pitch = 66f, Fov = 50f }, 1.2f, .04f);

    /// <summary>The fingerprint's place: left of the spiral, low, so its volume has room above it.</summary>
    Vector3 PrintSpot => O + new Vector3(-104f, -34f, 0f) + Up * 3f;
    const float PrintScale = 2.2f;

    public override void OnEnter() => UpdateStatus();

    protected override string LabelFor(Pickable p) => NumberKind.Describe(p.Value);

    public override void OnPick(int i) => Select(Picks[i].Value);

    void Select(long n)
    {
        n = System.Math.Clamp(n, 1L, N);
        selected = (int)n;
        Light(pos[n], n, Big);
        marker.gameObject.SetActive(true);
        LabDraw.SetRing(marker, pos[n] + Up * .2f, 2.1f, 40);
        ShowPrint(n, PrintSpot, PrintScale);
        Note = NumberKind.Describe(n) + (n == 1
            ? ""
            : NumberKind.IsPrime(n) ? ":  its only building block is itself" : ".  " + Neighbours(n));
        if (look == 5) dirty = true;
        UpdateStatus();
    }

    /// <summary>A line about the neighbours: whether they are prime, so steps along the spiral show the gaps.</summary>
    static string Neighbours(long n)
    {
        string Of(long m) => m < 1 ? "" : m == 1 ? "1" : NumberKind.IsPrime(m) ? $"<color={NumberKind.Hex(m)}>{m}</color> prime" : $"{m} = {NumberKind.Shorthand(m)}";
        return $"Beside it:  {Of(n - 1)},  {Of(n + 1)}";
    }

    void Step(int d) => Select(Mathf.Clamp((selected < 1 ? 1 : selected) + d, 1, N));

    void SetLook(int l)
    {
        look = ((l % Looks) + Looks) % Looks;
        if (look == 5 && selected < 2) Select(6);
        Note = look switch
        {
            0 => Legend,
            1 => "The primes, gold:  they keep to some curves and avoid others",
            2 => "Twin primes, p and p + 2 both prime",
            3 => "k² + k + 41 for k = 0, 1, 2 …:  prime for every k up to 39, on one smooth curve",
            4 => "The squares run out along one straight line (deep red for a prime's square), the pronic numbers k(k + 1) along the opposite one",
            _ => $"The multiples of {selected}:  every one of them is built on {selected}'s blocks",
        };
        dirty = true;
        UpdateStatus();
    }

    bool Lit(int n) => look switch
    {
        1 => NumberKind.IsPrime(n),
        2 => NumberKind.IsPrime(n) && (NumberKind.IsPrime(n - 2) || NumberKind.IsPrime(n + 2)),
        3 => IsEuler(n),
        4 => NumberKind.ISqrt(n) * NumberKind.ISqrt(n) == n || NumberKind.IsPronic(n),
        5 => selected >= 2 && n % selected == 0,
        _ => true,
    };

    static bool IsEuler(int n)
    {
        for (int k = 0; k * k + k + 41 <= n; k++) if (k * k + k + 41 == n) return true;
        return false;
    }

    protected override void OnTick(float dt)
    {
        if (dirty) Refill();
        dots.Draw(Matrix4x4.identity);
    }

    void Refill()
    {
        dots.Clear();
        for (int n = 1; n <= N; n++)
        {
            Color c = n == 1 ? NumberKind.Unit : NumberKind.ColorOf(n);
            bool lit = Lit(n);
            if (!lit) c = Color.Lerp(c, new Color(.1f, .11f, .15f), .72f);
            float size = NumberKind.IsPrime(n) ? Big : Dot;
            dots.Add(pos[n], lit && look != 0 ? size * 1.15f : size, c, lit ? (NumberKind.IsPrime(n) ? 1.3f : 1.1f) : 1f);
        }

        dirty = false;
    }

    public override bool OnKey(Keyboard k)
    {
        if (k.leftArrowKey.wasPressedThisFrame) { Step(-1); return true; }
        if (k.rightArrowKey.wasPressedThisFrame) { Step(1); return true; }
        if (k.lKey.wasPressedThisFrame) { SetLook(look + 1); return true; }
        return CommonKey(k);
    }

    public override bool StoryCommand(string verb, string[] a)
    {
        if (verb == "look" && a.Length > 0 && int.TryParse(a[0], out int l)) { SetLook(l); return true; }
        if (verb == "step" && a.Length > 0 && int.TryParse(a[0], out int d)) { Step(d); return true; }
        return base.StoryCommand(verb, a);
    }

    // for tests
    public int Selected => selected;
    public int Look => look;
    public void LookAt(int l) => SetLook(l);
}
