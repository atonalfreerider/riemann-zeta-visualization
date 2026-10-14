using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Everything that makes the visualization usable with bare hands: hand tracking and visuals, pinch-to-move, the menu
/// dock (chapters, views, space controls, settings), the palm menu, toasts, and the link to the tour. Built once the
/// tour has started up. See VRMenus.cs for the pages.
/// </summary>
public partial class VRApp : MonoBehaviour
{
    public VRRig Rig { get; private set; }
    public VRHands Hands { get; private set; }
    public VRPoker Poker { get; private set; }
    public VRWorldGrab Grab { get; private set; }
    public VRHandVisual Visual { get; private set; }
    public VRPerf Perf { get; private set; }
    public VRGuide Guide { get; private set; }
    public VRPanel Dock { get; private set; }
    public VRPanel Palm { get; private set; }
    public bool Ready { get; private set; }
    public TourDirector Tour => tour;

    TourDirector tour;
    Main main;
    VRPanel toastPanel;
    TextMeshProUGUI toastText;
    float toastUntil;

    /// <summary>Which hand carries the palm menu (the other one pokes).</summary>
    public bool MenuOnRightHand;

    float palmShownFor;
    float palmHiddenFor = 1f;
    float dockAway;
    Vector3 dockWanted;
    Quaternion dockWantedRotation = Quaternion.identity;
    bool dockInitialised;
    float handsMissingFor;
    bool warnedNoHands;

    public static VRApp Create(VRRig rig)
    {
        VRApp app = rig.gameObject.AddComponent<VRApp>();
        app.Rig = rig;
        VRMode.App = app;
        return app;
    }

    IEnumerator Start()
    {
        QualitySettings.antiAliasing = 4;
        QualitySettings.vSyncCount = 0;
        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.pixelLightCount = 0;
        QualitySettings.realtimeReflectionProbes = false;

        VRAudio.Init(Rig.transform);
        Hands = VRHands.Create(Rig);
        Poker = VRPoker.Create(Rig, Hands);
        Grab = VRWorldGrab.Create(Rig, Hands, Poker);
        Guide = VRGuide.Create(Rig);
        Visual = VRHandVisual.Create(Rig, Hands, Poker, Grab);
        Perf = VRPerf.Create(Rig);

        while (TourDirector.Instance == null || TourDirector.Instance.Captions == null || TourDirector.Instance.Rig == null || TourDirector.Instance.Annotations == null)
        {
            yield return null;
        }

        tour = TourDirector.Instance;
        main = tour.Main;

        // wait (briefly) for head tracking, then take the user's standing position as the view centre
        float waited = 0f;
        while (Rig.Headset && Rig.Cam.transform.localPosition.y < 0.3f && waited < 5f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        Rig.Recenter();
        Rig.SnapHud();

        BuildToast();
        BuildDock();
        BuildPalm();
        Poker.Register(Dock);
        Poker.Register(Palm);

        Grab.Grabbed += () =>
        {
            if (Guide.Follow)
            {
                Guide.SetFollow(false);
                Toast("The model is yours. Tap  Follow  to go back to the story");
            }
        };
        Guide.FollowChanged += () => { };
        tour.ChapterStarted += (index, chapter) => Guide.ResetOffsets();
        tour.TourEnded += () =>
        {
            Perf.Mark("explore");
            Toast("Tour finished. Explore with Views, or pick a chapter");
        };
        tour.LineStarted += line => Perf.Mark(line.id);

        PlaceDock(true);
        Dock.SetOpen(true);
        Ready = true;

        // start with something alive in front of the user: the chain of arrows, turning (unless the command line started the tour)
        if (!tour.Running) ApplyView("explore"); // default: standing on the saddle, the zeta trail rising from its centre
        Toast("Touch a chapter to begin. Pinch and drag in the air to move the model", 7f);

        // launch options for testing from the command line (adb shell am start ... -e unity "-vrview waves -vrdark")
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-vrview" && i + 1 < args.Length) ApplyView(args[++i]);
            else if (args[i] == "-vrdark") SetRoom(false);
        }
    }

    // ---- per-frame --------------------------------------------------------------------------------

    void Update()
    {
        if (!Ready) return;
        float dt = Time.unscaledDeltaTime;

        UpdatePalm(dt);
        UpdateDock(dt);
        UpdateToast();
        UpdateButtonsAvailability();
        LoopExplore();

        if (Rig.Headset)
        {
            handsMissingFor = Hands.Left.Tracked || Hands.Right.Tracked ? 0f : handsMissingFor + dt;
            if (!warnedNoHands && !Hands.Available && Time.unscaledTime > 12f)
            {
                warnedNoHands = true;
                Toast("No hand tracking. In Quest Settings turn on Movement tracking > Hand tracking", 8f);
            }
        }
    }

    /// <summary>
    /// The views run the dial on their own; left alone it would climb for ever, and the trail it draws (and the work to
    /// redraw it every frame) grows with it. Past t = 100 the animation starts over from where the view began.
    /// </summary>
    void LoopExplore()
    {
        if (tour.Running || !main.IsPlaying || main.CurrentT < ExploreLoopEnd) return;
        main.JumpTo(exploreLoopStart);
        main.Play(); // going back to an earlier t resets the animation, which pauses it
    }

    // ---- toast ------------------------------------------------------------------------------------

    void BuildToast()
    {
        toastPanel = VRPanel.Create("Toast", new Vector2(1000, 74), Rig.Hud);
        toastPanel.transform.localPosition = new Vector3(0f, 0.43f, 0f);
        toastPanel.transform.localRotation = Quaternion.identity;
        toastText = toastPanel.AddLabel(14, 0, 972, 74, "", 32f, VRStyle.Text, TextAlignmentOptions.Center);
        toastText.isOverlay = true;
    }

    public void Toast(string text, float seconds = 4f)
    {
        // No text in front of the eyes in VR: toasts are off. Menus are the only interface text.
        return;
#pragma warning disable CS0162
        if (toastPanel == null) return;
        toastText.text = text;
        toastUntil = Time.unscaledTime + seconds;
        toastPanel.SetOpen(true);
    }
#pragma warning restore CS0162

    void UpdateToast()
    {
        if (toastPanel.Open && Time.unscaledTime > toastUntil) toastPanel.SetOpen(false);
    }

    // ---- palm menu ----------------------------------------------------------------------------------

    /// <summary>Palm facing the user's face, a comfortable distance away: the menu floats above the palm.</summary>
    void UpdatePalm(float dt)
    {
        VRHand hand = MenuOnRightHand ? Hands.Right : Hands.Left;
        Vector3 head = Rig.Cam.transform.localPosition;
        bool facing = false;
        if (hand.Tracked && hand.TrackedFor > 0.2f)
        {
            Vector3 toHead = head - hand.PalmCenter;
            float distance = toHead.magnitude;
            float dot = distance > 1e-3f ? Vector3.Dot(hand.PalmNormal, toHead / distance) : 0f;
            float threshold = Palm.Open ? 0.35f : 0.6f;
            facing = dot > threshold && distance > 0.14f && distance < 0.7f && !hand.Pinching;
        }

        if (facing)
        {
            palmShownFor += dt;
            palmHiddenFor = 0f;
        }
        else
        {
            palmHiddenFor += dt;
            palmShownFor = 0f;
        }

        bool justOpened = false;
        if (!Palm.Open && palmShownFor > 0.25f)
        {
            Palm.SetOpen(true);
            justOpened = true;
        }

        if (Palm.Open && palmHiddenFor > 0.3f) Palm.SetOpen(false);

        if (Palm.Open && hand.Tracked)
        {
            Vector3 center = hand.PalmCenter + hand.PalmNormal * 0.115f + hand.FingerDirection * 0.01f;
            Vector3 look = center - head;
            Quaternion rotation = look.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(look, Vector3.up) : Quaternion.identity;
            if (justOpened)
            {
                Palm.transform.localPosition = center;
                Palm.transform.localRotation = rotation;
            }
            else
            {
                float k = 1f - Mathf.Exp(-18f * dt);
                Palm.transform.localPosition = Vector3.Lerp(Palm.transform.localPosition, center, k);
                Palm.transform.localRotation = Quaternion.Slerp(Palm.transform.localRotation, rotation, k);
            }
        }
    }

    // ---- dock ---------------------------------------------------------------------------------------

    /// <summary>The dock sits about half a metre ahead and a little below the eyes, tilted to face them.</summary>
    void ComputeDockPlacement(out Vector3 position, out Quaternion rotation)
    {
        Vector3 head = Rig.Cam.transform.localPosition;
        Vector3 forward = Rig.Cam.transform.localRotation * Vector3.forward;
        forward.y = 0f;
        // looking almost straight up or down leaves no usable "forward": fall back to the way the user was facing
        forward = forward.sqrMagnitude > 0.07f ? forward.normalized : Rig.YawHome * Vector3.forward;
        // low enough that its top edge sits below the captions (about 24 degrees below eye level)
        position = head + forward * 0.5f + Vector3.down * 0.42f;
        rotation = Quaternion.LookRotation(position - head, Vector3.up);
    }

    void PlaceDock(bool snap)
    {
        ComputeDockPlacement(out dockWanted, out dockWantedRotation);
        if (snap || !dockInitialised)
        {
            Dock.transform.localPosition = dockWanted;
            Dock.transform.localRotation = dockWantedRotation;
            dockInitialised = true;
        }
    }

    /// <summary>Bring the dock in front of the user (palm menu, or after walking away from it).</summary>
    public void SummonDock()
    {
        PlaceDock(true);
        dockAway = 0f;
        dockMoving = false;
        Dock.SetOpen(true);
    }

    public void ToggleDock()
    {
        if (!Dock.Open) SummonDock();
        else Dock.SetOpen(false);
    }

    void UpdateDock(float dt)
    {
        if (!Dock.Open) return;
        Vector3 head = Rig.Cam.transform.localPosition;
        Vector3 toDock = Dock.transform.localPosition - head;
        Vector3 gaze = Rig.Cam.transform.localRotation * Vector3.forward;
        float angle = Vector3.Angle(gaze, toDock);
        bool handNear = (Hands.Left.Tracked && Vector3.Distance(Hands.Left.IndexTip, Dock.transform.localPosition) < 0.35f)
                        || (Hands.Right.Tracked && Vector3.Distance(Hands.Right.IndexTip, Dock.transform.localPosition) < 0.35f);

        // out of sight or out of reach for a moment (and no finger on it): glide it back in front of the user
        bool lost = (angle > 75f || toDock.magnitude > 1.1f) && !handNear;
        if (!dockMoving)
        {
            dockAway = lost ? dockAway + dt : 0f;
            if (dockAway > 1.5f)
            {
                dockMoving = true;
                ComputeDockPlacement(out dockWanted, out dockWantedRotation);
            }
        }

        if (dockMoving)
        {
            float k = 1f - Mathf.Exp(-5f * dt);
            Dock.transform.localPosition = Vector3.Lerp(Dock.transform.localPosition, dockWanted, k);
            Dock.transform.localRotation = Quaternion.Slerp(Dock.transform.localRotation, dockWantedRotation, k);
            if (Vector3.Distance(Dock.transform.localPosition, dockWanted) < 0.004f)
            {
                dockMoving = false;
                dockAway = 0f;
            }
        }
    }

    bool dockMoving;
}
