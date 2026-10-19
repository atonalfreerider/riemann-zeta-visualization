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
/// A game in the model lab: open play with numbers, never a test. Nothing is scored and nothing is locked: every number can
/// be picked at any time, and picking one always shows what it is made of. The lab already gives every model pointing (a
/// click, a tap, a fingertip, the finger's ray), orbiting (a drag, one finger, a pinch on the model in VR) and its
/// controls; a game adds:
/// * pickables: the numbers (shells, vortices, prime containers …) by value and kind;
/// * a marble fingerprint (MarbleFingerprint) of the number being looked at, facing the player, that unpacks into its
///   full volume and packs back into its shorthand: with the Unpack button (U), or by hand: drag it sideways (a finger on
///   a touch screen, the mouse) or pinch it in VR and pull, to the right to pull the number apart into its primes, to the
///   left to pack it back. Flat (symmetric spirals) on the desktop, deep (spiral sphere packs) in VR; the 3D toggle (F);
/// * a ripple in the number's own colour where it was picked;
/// * the status: the game's name and two small lines (what to try, and what the last pick showed), on the desktop board
///   and on the VR dock.
/// Colours follow NumberKind everywhere: gold is always prime; a prime's square is a deep red; pronic numbers are blue;
/// other composites grey.
/// </summary>
public abstract class GameModel : LabModel
{
    protected readonly List<Pickable> Picks = new();
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

    /// <summary>Move the fingerprint (its badge end) to a new world point, say to keep it in view as the model turns.</summary>
    protected void MovePrint(Vector3 at)
    {
        printAt = at;
        FacePrint();
    }

    protected void HidePrint()
    {
        if (print != null) Object.Destroy(print.gameObject);
        print = null;
        scrubbing = false;
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

    // ---- the fingerprint by hand: pulled apart and pushed back together --------------------------------------

    bool scrubbing;
    float scrubFrom, scrubPixels;
    Vector3 scrubStart;
    /// <summary>A drag or a pinch is pulling the fingerprint apart (so a game's own drag leaves it alone).</summary>
    protected bool Scrubbing => scrubbing;

    /// <summary>The fingerprint's extent on the screen (its string, its containers and its volume as drawn now).</summary>
    bool PrintOnScreen(out Rect r)
    {
        r = default;
        Camera cam = Camera.main;
        if (print == null || cam == null) return false;
        Bounds b = print.LocalBounds;
        Vector2 lo = new(float.MaxValue, float.MaxValue), hi = new(float.MinValue, float.MinValue);
        for (int i = 0; i < 8; i++)
        {
            Vector3 c = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            Vector3 sp = cam.WorldToScreenPoint(print.transform.TransformPoint(c));
            if (sp.z <= 0f) return false;
            lo = Vector2.Min(lo, sp);
            hi = Vector2.Max(hi, sp);
        }

        r = Rect.MinMaxRect(lo.x - 16f, lo.y - 16f, hi.x + 16f, hi.y + 16f);
        return true;
    }

    /// <summary>A drag that starts on the fingerprint pulls it apart (to the right) or packs it (to the left).</summary>
    public override bool DragBegin(Vector2 screen)
    {
        if (!PrintOnScreen(out Rect r) || !r.Contains(screen)) return false;
        scrubbing = true;
        scrubFrom = print.Phase;
        // about the width of one container on the screen per step
        scrubPixels = Mathf.Clamp(r.width / Mathf.Max(3f, print.Steps + 2f), 40f, 160f);
        return true;
    }

    public override void DragMove(Vector2 screen, Vector2 delta)
    {
        if (!scrubbing || print == null) return;
        // held within the phases, so a drag back from past either end answers at once
        scrubFrom = Mathf.Clamp(scrubFrom + delta.x / scrubPixels, 0f, print.Steps);
        print.Jump(scrubFrom);
    }

    public override void DragEnd() => scrubbing = false;

    /// <summary>VR: a pinch on the fingerprint takes hold of it; pulling the hand along its string unpacks or packs it.</summary>
    public override bool WantsPinch(Vector3 world)
    {
        if (print == null) return false;
        Bounds b = print.LocalBounds;
        b.Expand(new Vector3(3f, 3f, 6f));
        return b.Contains(print.transform.InverseTransformPoint(world));
    }

    public override void PinchBegin(Vector3 world)
    {
        if (print == null) return;
        scrubbing = true;
        scrubStart = world;
        scrubFrom = print.Phase;
    }

    public override void PinchMove(Vector3 world)
    {
        if (!scrubbing || print == null) return;
        // three marbles' width along the string per step
        float along = Vector3.Dot(world - scrubStart, print.transform.right) / (printScale * 3f);
        print.Jump(Mathf.Clamp(scrubFrom + along, 0f, print.Steps));
    }

    public override void PinchEnd() => scrubbing = false;

    // ---- feedback -----------------------------------------------------------------------------------------

    /// <summary>A number was picked: a ripple in its own colour (gold for a prime), and a click in VR.</summary>
    protected void Light(Vector3 at, long n, float size)
    {
        Ripple(at, n <= 1 ? NumberKind.Unit : NumberKind.ColorOf(n), size);
        if (VRMode.Enabled) VRAudio.Click();
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

    /// <summary>The head line, the game's name; then what to try and what the last pick showed, small.</summary>
    protected void UpdateStatus()
    {
        string small = Prompt + (string.IsNullOrEmpty(Note) ? "" : "\n" + Note);
        Lab.SetStatus(string.IsNullOrEmpty(small) ? Title : $"{Title}\n<size=58%>{small}</size>");
    }

    /// <summary>A key for the colours, for the note line.</summary>
    protected static string Legend =>
        $"<color={NumberKind.Hex(2)}>gold prime</color>   <color={NumberKind.Hex(49)}>deep red p²</color>   " +
        $"<color={NumberKind.Hex(56)}>blue pronic k(k + 1)</color>   <color={NumberKind.Hex(91)}>grey composite</color>";

    // a narrated chapter pulling the fingerprint (exp:lab:pull)
    float pullFrom, pullTo, pullStart = -1f, pullSeconds = 1f;

    public override void Tick(float dt)
    {
        FacePrint();
        UpdateRipples(dt);
        if (pullStart >= 0f && print != null)
        {
            float u = Mathf.Clamp01((Time.time - pullStart) / pullSeconds);
            print.Jump(Mathf.Lerp(pullFrom, pullTo, Mathf.SmoothStep(0f, 1f, u)));
            if (u >= 1f) pullStart = -1f;
        }

        OnTick(dt);
    }

    protected virtual void OnTick(float dt) { }

    /// <summary>
    /// A view as a narrated chapter frames it: the chapter's captions take the bottom quarter of the picture, so the model
    /// is drawn a little smaller (the camera <paramref name="zoom"/> times farther) and raised by <paramref name="lift"/>
    /// of the picture's height. Outside a chapter, the view itself.
    /// </summary>
    protected LabView StoryFramed(LabView v, float zoom, float lift)
    {
        if (Lab == null || !Lab.StoryMode) return v;
        v.Distance *= zoom;
        float height = 2f * v.Distance * Mathf.Tan((v.Fov > 0f ? v.Fov : 55f) * .5f * Mathf.Deg2Rad);
        v.Focus -= v.ScreenUp * (lift * height);
        return v;
    }

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
    /// label), pick VALUE[:KIND] (and pick it), unpoint, unpack[:SECONDS] (seconds per step), pack, pull PHASE[:SECONDS]
    /// (the fingerprint pulled by hand to a phase), deep, flat, digits on|off, home (its own view), view YAW:PITCH[:DISTANCE]
    /// (turned, as a drag would). A game adds its own verbs.
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
                Lab.RefreshHover();
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
            case "pull":
            {
                // pull:PHASE[:SECONDS] — the fingerprint pulled by hand to a phase, as a drag would
                if (print == null || a.Length == 0 || !float.TryParse(a[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float to)) return true;
                pullFrom = print.Phase;
                pullTo = Mathf.Clamp(to, 0f, print.Steps);
                pullSeconds = a.Length > 1 && float.TryParse(a[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float sec) ? Mathf.Max(.1f, sec) : 2f;
                pullStart = Time.time;
                return true;
            }
            case "deep": SetDeep(true); return true;
            case "flat": SetDeep(false); return true;
            case "home": Lab.ResetView(false); return true;
            case "view":
            {
                // view:YAW:PITCH[:DISTANCE] — the camera eases round the model's own focus, as a drag would turn it
                float Num(int i, float d) => a.Length > i && float.TryParse(a[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : d;
                LabView h = Home;
                Lab.SetView(new LabView { Focus = h.Focus, Yaw = Num(0, h.Yaw), Pitch = Num(1, h.Pitch), Distance = Num(2, h.Distance), Fov = h.Fov }, false);
                return true;
            }
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

    /// <summary>For tests: flat or 3D.</summary>
    public void SetDeep(bool on)
    {
        if (DeepOn != on) ToggleDeep();
    }

    /// <summary>For tests: the index of the pick with this value (and kind), or −1.</summary>
    public int PickIndex(long n, string kind = null) => Picks.FindIndex(p => p.Value == n && (kind == null || p.Kind == kind));

    /// <summary>For tests: pick a number (of a kind, if given) as a click would; false if there is none.</summary>
    public bool PickValue(long n, string kind = null)
    {
        int i = Picks.FindIndex(p => p.Value == n && (kind == null || p.Kind == kind));
        if (i < 0) return false;
        OnPick(i);
        return true;
    }

    /// <summary>The controls every game has: unpack the fingerprint, flat or 3D, digits on or off, re-frame.</summary>
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

    /// <summary>The keys every game has (U unpack, F flat or 3D, D digits); a game's own keys first.</summary>
    protected bool CommonKey(UnityEngine.InputSystem.Keyboard k)
    {
        if (k.uKey.wasPressedThisFrame) { TogglePrint(); return true; }
        if (k.fKey.wasPressedThisFrame) { ToggleDeep(); return true; }
        if (k.dKey.wasPressedThisFrame) { Controls.Find(c => c.Label == "Digits")?.Clicked?.Invoke(); return true; }
        return false;
    }
}
