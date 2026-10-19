using System.Collections.Generic;
using UnityEngine;

/// <summary>A thing the player can point at in a game: a number, an E8 shell, a prime container, a vortex …</summary>
public struct Pickable
{
    public Vector3 Position;
    public long Value;
    /// <summary>How big it is (world units): sizes its ripple.</summary>
    public float Radius;
    /// <summary>What it is: "number", "shell", "prime", "rest", "vortex" … (the game reads it).</summary>
    public string Kind;
}

/// <summary>
/// A game in the model lab. The lab already gives every model desktop pointing (hover within 22 px, click to pick), VR
/// pointing (a fingertip touch, or the finger's ray held still) and its controls on the desktop panel and the VR dock;
/// a game adds what a game needs:
/// * pickables: the numbers (shells, vortices, prime containers …) the player can point at, by value and kind;
/// * a marble fingerprint (MarbleFingerprint) of the number being looked at, facing the player, that unpacks into its
///   full volume and packs back into its shorthand; flat (symmetric spirals) on the desktop, deep (spiral sphere packs)
///   in VR, and the 3D toggle (F) switches;
/// * points, a streak, rounds of examples, and feedback: a gold ripple for a find, a grey one for a miss;
/// * the status: one head line and two small lines (the prompt and what the last pick showed), on the desktop board and
///   on the VR dock.
/// Colours follow NumberKind everywhere: gold is always prime; a prime's square is a deep red; pronic numbers are blue;
/// other composites grey.
/// </summary>
public abstract class GameModel : LabModel
{
    protected readonly List<Pickable> Picks = new();
    protected int Score, Streak, BestStreak, Finds, Misses;
    protected string Prompt = "", Note = "";
    protected bool DigitsOn = true;
    /// <summary>The marbles deep (spiral sphere packs) or flat (symmetric spirals): deep in VR, flat on the desktop, until toggled.</summary>
    protected bool DeepOn => deepChoice ?? VRMode.Enabled;
    bool? deepChoice;

    MarbleFingerprint print;
    Vector3 printAt;
    float printScale = 1f;
    bool printBeside;

    readonly List<(LineRenderer ring, Vector3 at, float size, float age, Color c)> ripples = new();

    public override int PickCount => Picks.Count;
    public override Vector3 PickPoint(int i) => Picks[i].Position;
    public override string PickLabel(int i) => LabelFor(Picks[i]);

    protected virtual string LabelFor(Pickable p) => p.Value.ToString();

    public override bool IsGame => true;

    /// <summary>The model's up (the lab's floor is the XY plane; up is world −z).</summary>
    protected static Vector3 Up => LabView.Up;

    // ---- the fingerprint ----------------------------------------------------------------------------------

    public long PrintNumber => print != null ? print.N : 0;
    public MarbleFingerprint Print => print;

    /// <summary>
    /// Show n's fingerprint at a world point (its badge end), marbles `scale` across; the same n just moves. beside: it
    /// unpacks to the right of its string (else above it).
    /// </summary>
    protected void ShowPrint(long n, Vector3 at, float scale, bool beside = false)
    {
        if (print == null || print.N != n || !Mathf.Approximately(printScale, scale))
        {
            HidePrint();
            print = MarbleFingerprint.Create(Root, n, scale, DigitsOn, DeepOn, beside);
        }

        printAt = at;
        printScale = scale;
        printBeside = beside;
        FacePrint();
    }

    protected void HidePrint()
    {
        if (print != null) Object.Destroy(print.gameObject);
        print = null;
    }

    protected void TogglePrint()
    {
        if (print != null) print.Toggle();
    }

    void FacePrint()
    {
        if (print == null) return;
        Camera cam = Camera.main;
        print.transform.position = printAt;
        if (cam == null) return;
        // the string reads left to right across the player's view, upright on the screen
        Vector3 fwd = (printAt - cam.transform.position).normalized;
        print.transform.rotation = Quaternion.LookRotation(fwd, cam.transform.up);
    }

    // ---- points and feedback ------------------------------------------------------------------------------

    protected void Found(Vector3 at, int points, float size)
    {
        Finds++;
        Streak++;
        BestStreak = Mathf.Max(BestStreak, Streak);
        Score += points + (Streak >= 3 ? Streak - 2 : 0);
        Ripple(at, NumberKind.Gold, size);
        if (VRMode.Enabled) VRAudio.Click();
    }

    protected void Missed(Vector3 at, int penalty, float size)
    {
        Misses++;
        Streak = 0;
        Score = Mathf.Max(0, Score - penalty);
        Ripple(at, NumberKind.Grey, size);
    }

    /// <summary>A ring that opens and fades on the floor round a point.</summary>
    protected void Ripple(Vector3 at, Color c, float size)
    {
        LineRenderer r = LabDraw.Ring(Root, "ripple", at, size, size * .12f, c, Factory.GlowIntensity, 64);
        ripples.Add((r, at, size, 0f, c));
    }

    void UpdateRipples(float dt)
    {
        for (int i = ripples.Count - 1; i >= 0; i--)
        {
            var (ring, at, size, age, c) = ripples[i];
            age += dt;
            if (ring == null || age > 1.1f)
            {
                if (ring != null) Object.Destroy(ring.gameObject);
                ripples.RemoveAt(i);
                continue;
            }

            float u = age / 1.1f, rad = size * (1f + 2.2f * u);
            Vector3[] pts = new Vector3[64];
            for (int k = 0; k < 64; k++)
            {
                float a = k / 64f * Mathf.PI * 2f;
                pts[k] = at + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * rad;
            }

            ring.SetPositions(pts);
            ring.startColor = ring.endColor = new Color(c.r, c.g, c.b, 1f - u);
            ripples[i] = (ring, at, size, age, c);
        }
    }

    // ---- the status ---------------------------------------------------------------------------------------

    /// <summary>The head line: the game, the points and the streak; then the prompt and the note, small.</summary>
    protected void UpdateStatus()
    {
        string gold = "#" + ColorUtility.ToHtmlStringRGB(NumberKind.Gold);
        string head = $"{Title}   <color={gold}>{Score}</color> points" + (Streak >= 2 ? $"   streak {Streak}" : "");
        string small = Prompt + (string.IsNullOrEmpty(Note) ? "" : "\n" + Note);
        Lab.SetStatus(string.IsNullOrEmpty(small) ? head : $"{head}\n<size=58%>{small}</size>");
    }

    /// <summary>A key for the colours, for the note line.</summary>
    protected static string Legend =>
        $"<color={NumberKind.Hex(2)}>gold prime</color>   <color={NumberKind.Hex(49)}>deep red p²</color>   " +
        $"<color={NumberKind.Hex(56)}>blue pronic k(k + 1)</color>   <color={NumberKind.Hex(91)}>grey composite</color>";

    public override void Tick(float dt)
    {
        FacePrint();
        UpdateRipples(dt);
        OnTick(dt);
    }

    protected virtual void OnTick(float dt) { }

    public override void OnExit()
    {
        HidePrint();
        foreach (var r in ripples) if (r.ring != null) Object.Destroy(r.ring.gameObject);
        ripples.Clear();
    }

    /// <summary>Flat ↔ deep: the fingerprint eases between its symmetric spirals and its spiral sphere packs.</summary>
    protected void ToggleDeep()
    {
        deepChoice = !DeepOn;
        if (print != null) print.Deep = DeepOn;
        OnDeepChanged();
    }

    /// <summary>A game's own marbles follow the 3D toggle here.</summary>
    protected virtual void OnDeepChanged() { }

    public bool Deep => DeepOn;

    /// <summary>
    /// A narrated chapter plays the game (a tutorial), with exp:lab:VERB[:ARGS]: point VALUE[:KIND] (hover it, its dot and
    /// label), pick VALUE[:KIND] (and pick it), unpoint, unpack, pack, deep, flat, digits on|off, home (its own view).
    /// A game adds its own verbs (round, example, steer …).
    /// </summary>
    public virtual bool StoryCommand(string verb, string[] a)
    {
        switch (verb)
        {
            case "point":
            {
                int i = FindPick(a);
                if (i >= 0) Lab.PointAt(i);
                return true;
            }
            case "pick":
            {
                int i = FindPick(a);
                if (i < 0) return true;
                Lab.PointAt(i);
                OnPick(i);
                return true;
            }
            case "unpoint": Lab.PointAt(-1); return true;
            case "unpack":
                // unpack[:SECONDS] — seconds per step, for a narrated line that has less time than a player
                if (print != null && a.Length > 0 && float.TryParse(a[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float step))
                    print.StepSeconds = step;
                print?.Unpack();
                return true;
            case "pack": print?.Pack(); return true;
            case "deep": SetDeep(true); return true;
            case "flat": SetDeep(false); return true;
            case "home": Lab.ResetView(false); return true;
            case "digits":
                if ((a.Length == 0 || a[0] != "off") != DigitsOn) Controls.Find(c => c.Label == "Digits")?.Clicked?.Invoke();
                return true;
        }

        return false;
    }

    /// <summary>The pick with this value (and kind, if given), or −1.</summary>
    protected int FindPick(string[] a)
    {
        if (a.Length == 0 || !long.TryParse(a[0], out long v)) return -1;
        string kind = a.Length > 1 && a[1].Length > 0 ? a[1] : null;
        return Picks.FindIndex(p => p.Value == v && (kind == null || p.Kind == kind));
    }

    /// <summary>A round that is done moves on by itself, except while a narrated chapter plays the game (it says when).</summary>
    protected bool AutoAdvance => Lab == null || !Lab.StoryMode;

    /// <summary>For tests: flat or 3D.</summary>
    public void SetDeep(bool on)
    {
        if (DeepOn != on) ToggleDeep();
    }

    /// <summary>The controls every game has: unpack the fingerprint, digits on or off, flat or 3D, re-frame.</summary>
    protected void AddCommonControls()
    {
        Controls.Add(LabControl.Button("Unpack", VRIcons.Id.Cube, TogglePrint, "U"));
        Controls.Add(LabControl.Toggle("3D", VRIcons.Id.Torus, () => DeepOn, ToggleDeep, "F"));
        Controls.Add(LabControl.Toggle("Digits", VRIcons.Id.Captions, () => DigitsOn, () =>
        {
            DigitsOn = !DigitsOn;
            if (print != null)
            {
                long n = print.N;
                HidePrint();
                ShowPrint(n, printAt, printScale, printBeside);
            }
        }, "D"));
        Controls.Add(LabControl.Button("Reset view", VRIcons.Id.Target, () => Lab.ResetView(false), "Home"));
    }

    /// <summary>The keys every game has (U unpack, D digits, F flat or 3D); a game's own keys first.</summary>
    protected bool CommonKey(UnityEngine.InputSystem.Keyboard k)
    {
        if (k.uKey.wasPressedThisFrame) { TogglePrint(); return true; }
        if (k.fKey.wasPressedThisFrame) { ToggleDeep(); return true; }
        if (k.dKey.wasPressedThisFrame) { Controls.Find(c => c.Label == "Digits")?.Clicked?.Invoke(); return true; }
        return false;
    }
}
