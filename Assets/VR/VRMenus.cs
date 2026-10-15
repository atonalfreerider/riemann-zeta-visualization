using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The pages of the dock and the palm menu. Every control is a big icon with a short word under it, pushed with one
/// finger; nothing needs a ray, a controller or a pinch.
///
///   Tour         one button per chapter (touch the playing one to restart it)
///   Experiments  the thought experiments on the saddle, one button per module (the same modules as the desktop menu)
///   Views        fly to a visualization on its own, and show or hide its layers
///   Space     zoom, turn, tilt and move the model; reset the view
///   Settings  captions, hands, comfort, volume, model distance
///   Footer    previous / replay / play-pause / skip / next, follow the story, room or dark
/// </summary>
public partial class VRApp
{
    const float DockWidth = 600f, DockHeight = 450f;
    const float PageTop = 70f, PageHeight = 296f;

    static readonly string[] ChapterShort = { "Sieve", "Sacks", "Zeno", "Stage", "Dial", "Waves", "Steps", "Key", "2026", "Saddle" };
    static readonly VRIcons.Id[] ChapterIcon =
    {
        VRIcons.Id.Grid, VRIcons.Id.Sacks, VRIcons.Id.Zeno, VRIcons.Id.Stage, VRIcons.Id.Dial,
        VRIcons.Id.Waves, VRIcons.Id.Stairs, VRIcons.Id.Key, VRIcons.Id.Strip, VRIcons.Id.Saddle
    };

    /// <summary>A chapter's icon, shared with the desktop side menu.</summary>
    public static VRIcons.Id ChapterIconFor(int index) => index >= 0 && index < ChapterIcon.Length ? ChapterIcon[index] : VRIcons.Id.Play;

    readonly List<RectTransform> pages = new();
    readonly List<VRButton> tabs = new();
    int page;
    TextMeshProUGUI nowLabel, labLabel;
    VRButton replayButton, skipButton, prevChapterButton, nextChapterButton;
    bool autoNextChapter = true;
    const double ExploreLoopEnd = 100d;
    double exploreLoopStart;
    int nowKey = -2;

    class Layers
    {
        public bool Chain = true, Tail = true, Zeros = true, Trail = true, Waves = true, Stairs = true, Grid = true, Equations = true;
        public void All(bool on) => Chain = Tail = Zeros = Trail = Waves = Stairs = Grid = Equations = on;
    }

    readonly Layers layers = new();

    bool PlayingNow => tour.Running ? !tour.Paused : main.IsPlaying;

    // ---- dock ---------------------------------------------------------------------------------------

    void BuildDock()
    {
        Dock = VRPanel.Create("Dock", new Vector2(DockWidth, DockHeight), Rig.Ui);

        string[] tabNames = { "Tour", "Experiments", "Views", "Space", "Settings" };
        VRIcons.Id[] tabIcons = { VRIcons.Id.List, VRIcons.Id.Flask, VRIcons.Id.Cube, VRIcons.Id.Move, VRIcons.Id.Gear };
        float[] tabWidths = { 78f, 140f, 88f, 88f, 104f };
        float tabX = 8f;
        for (int i = 0; i < tabNames.Length; i++)
        {
            int index = i;
            VRButton tab = Dock.AddButton(tabX, 8, tabWidths[i], 54, tabNames[i], tabIcons[i], () => ShowPage(index), true, () => page == index, 21f);
            tabs.Add(tab);
            tabX += tabWidths[i] + 4f;
        }

        Dock.AddButton(530, 8, 62, 54, "", VRIcons.Id.Close, () => Dock.SetOpen(false));

        for (int i = 0; i < tabNames.Length; i++) pages.Add(Dock.AddPage(tabNames[i], 0, PageTop, DockWidth, PageHeight));
        BuildTourPage(pages[0]);
        BuildLabPage(pages[1]);
        BuildViewsPage(pages[2]);
        BuildSpacePage(pages[3]);
        BuildSettingsPage(pages[4]);
        ShowPage(0);
        BuildFooter();
    }

    public void ShowPage(int index)
    {
        page = index;
        for (int i = 0; i < pages.Count; i++) pages[i].gameObject.SetActive(i == index);
    }

    // ---- tour page -----------------------------------------------------------------------------------

    void BuildTourPage(RectTransform p)
    {
        TourChapter[] chapters = tour.Chapters;
        for (int i = 0; i < chapters.Length; i++)
        {
            int index = i;
            int col = i % 5, row = i / 5;
            string label = $"{i + 1}  {(i < ChapterShort.Length ? ChapterShort[i] : chapters[i].title)}";
            VRIcons.Id icon = i < ChapterIcon.Length ? ChapterIcon[i] : VRIcons.Id.Play;
            Dock.AddButton(8 + col * 118, 2 + row * 134, 110, 128, label, icon,
                () => PlayChapter(index), true, () => tour.Running && !tour.PlayingExperiment && tour.CurrentChapterIndex == index, 21f, p);
        }

        nowLabel = Dock.AddLabel(10, 272, 580, 26, "", 21f, VRStyle.TextDim, TextAlignmentOptions.Left, FontStyles.Normal, p);
    }

    void PlayChapter(int index)
    {
        tour.LeaveExperiment(); // the chapter numbers are the tour's, not the experiment modules'
        Guide.ResetOffsets();
        Guide.SetFollow(true, true);
        tour.GoToChapter(index, autoNextChapter);
    }

    // ---- experiments page ------------------------------------------------------------------------------

    /// <summary>The short name and the icon of each experiment module (the desktop menu lists the full titles).</summary>
    static readonly Dictionary<string, (string label, VRIcons.Id icon)> LabInfo = new()
    {
        ["xcurves"] = ("Curves", VRIcons.Id.Curves),
        ["xlevels"] = ("Levels", VRIcons.Id.Contours),
        ["xprobe"] = ("Probe", VRIcons.Id.Probe),
        ["xmirror"] = ("Mirrors", VRIcons.Id.Mirror),
        ["xlattice"] = ("Counting", VRIcons.Id.Lattice),
        ["xideals"] = ("Ideals", VRIcons.Id.Ideals),
        ["xmirrorlines"] = ("Lines", VRIcons.Id.Chords),
        ["xcomb"] = ("Comb", VRIcons.Id.Comb),
        ["xpinch"] = ("Pinch", VRIcons.Id.Pinch),
        ["xtorus"] = ("Torus", VRIcons.Id.Torus),
        ["xlegendre"] = ("Legendre", VRIcons.Id.Legendre),
        ["xfinger"] = ("Prints", VRIcons.Id.Fingerprint),
        ["xclock"] = ("Clock", VRIcons.Id.Clock),
        ["xgoldbach"] = ("Pairs", VRIcons.Id.Pair),
        ["xsieve"] = ("Sieve", VRIcons.Id.Sieve),
        ["xtwist"] = ("Twist", VRIcons.Id.Twist),
    };

    const int LabColumns = 6, LabRows = 3;

    void BuildLabPage(RectTransform p)
    {
        TourChapter[] modules = tour.ExperimentChapters;
        if (modules.Length > LabColumns * LabRows) Debug.LogWarning($"VR: {modules.Length} experiment modules, room for {LabColumns * LabRows} on the page");
        for (int i = 0; i < modules.Length && i < LabColumns * LabRows; i++)
        {
            int index = i;
            (string label, VRIcons.Id icon) = LabInfo.TryGetValue(modules[i].id, out var info) ? info : (modules[i].title, VRIcons.Id.Flask);
            int col = i % LabColumns, row = i / LabColumns;
            Dock.AddButton(8 + col * 97, 2 + row * 88, 93, 84, label, icon,
                () => PlayExperiment(index), true, () => tour.Running && tour.PlayingExperiment && tour.CurrentChapterIndex == index, 17f, p);
        }

        labLabel = Dock.AddLabel(10, 272, 580, 26, "", 21f, VRStyle.TextDim, TextAlignmentOptions.Left, FontStyles.Normal, p);
    }

    /// <summary>Play one experiment module (they play alone; Next goes on to the one after).</summary>
    public void PlayExperiment(int index)
    {
        Guide.ResetOffsets();
        Guide.SetFollow(true, true);
        tour.PlayExperiment(index);
    }

    /// <summary>For the command line: one module by id, or all of them one after the other.</summary>
    public void PlayExperiment(string id, bool chain)
    {
        Guide.ResetOffsets();
        Guide.SetFollow(true, true);
        tour.PlayExperiment(id, chain);
    }

    // ---- views page ----------------------------------------------------------------------------------

    struct ViewDef
    {
        public string Id, Label;
        public VRIcons.Id Icon;
    }

    static readonly ViewDef[] Views =
    {
        new() { Id = "chain", Label = "Chain", Icon = VRIcons.Id.Chain },
        new() { Id = "zeros", Label = "Zeros", Icon = VRIcons.Id.Zeros },
        new() { Id = "strip", Label = "Strip", Icon = VRIcons.Id.Strip },
        new() { Id = "waves", Label = "Waves", Icon = VRIcons.Id.Waves },
        new() { Id = "stairs", Label = "Staircase", Icon = VRIcons.Id.Stairs },
        new() { Id = "primes", Label = "Primes", Icon = VRIcons.Id.Sacks },
        new() { Id = "saddle", Label = "Saddle", Icon = VRIcons.Id.Saddle },
        new() { Id = "machine", Label = "Machine", Icon = VRIcons.Id.Machine },
        new() { Id = "explore", Label = "Stand on it", Icon = VRIcons.Id.Saddle },
    };

    void BuildViewsPage(RectTransform p)
    {
        for (int i = 0; i < Views.Length; i++)
        {
            ViewDef view = Views[i];
            int col = i % 5, row = i / 5;
            Dock.AddButton(8 + col * 118, 2 + row * 74, 112, 68, view.Label, view.Icon, () => ApplyView(view.Id), false, null, 19f, p);
        }

        Dock.AddLabel(10, 146, 400, 22, "Show or hide", 20f, VRStyle.TextDim, TextAlignmentOptions.Left, FontStyles.Normal, p);
        (string label, VRIcons.Id icon, System.Func<bool> get, System.Action<bool> set)[] toggles =
        {
            ("Chain", VRIcons.Id.Chain, () => layers.Chain, v => layers.Chain = v),
            ("Tail", VRIcons.Id.Tail, () => layers.Tail, v => layers.Tail = v),
            ("Zeros", VRIcons.Id.Zeros, () => layers.Zeros, v => layers.Zeros = v),
            ("Trail", VRIcons.Id.Trail, () => layers.Trail, v => layers.Trail = v),
            ("Waves", VRIcons.Id.Waves, () => layers.Waves, v => layers.Waves = v),
            ("Primes", VRIcons.Id.Stairs, () => layers.Stairs, v => layers.Stairs = v),
            ("Flat", VRIcons.Id.Saddle, () => tour.Annotations.Sacks.Flat, v => tour.Annotations.Sacks.SetFlat(v)),
        };
        for (int i = 0; i < toggles.Length; i++)
        {
            var t = toggles[i];
            Dock.AddButton(8 + i * 84, 170, 78, 58, t.label, t.icon, () =>
            {
                t.set(!t.get());
                ApplyLayers();
            }, true, t.get, 16f, p);
        }

        // the curves on the Sacks spiral: the squares, the neighbour products, Euler's and Sacks's product curves
        SacksSpiral sacks = tour.Annotations.Sacks;
        (string label, System.Func<bool> get, System.Action<bool> set)[] curves =
        {
            ("Squares", () => sacks.RouteVisible("squares"), v => sacks.SetRouteVisible("squares", v)),
            ("k(k+1)", () => sacks.RouteVisible("pronic"), v => sacks.SetRouteVisible("pronic", v)),
            ("Euler", () => sacks.RouteVisible("euler"), v => sacks.SetRouteVisible("euler", v)),
            ("Products", () => sacks.FamilyVisible, v => sacks.SetFamilyVisible(v)),
        };
        for (int i = 0; i < curves.Length; i++)
        {
            var c = curves[i];
            Dock.AddButton(8 + i * 84, 232, 78, 58, c.label, VRIcons.Id.Equations, () => c.set(!c.get()), true, c.get, 16f, p);
        }

        Dock.AddSlider(8 + curves.Length * 84 + 4, 232, 580 - curves.Length * 84 - 2, 58, "Dial speed", 0f, 10f, () => (float)main.Speed, v => main.Speed = v, v => $"{v:0.0}/s", p);
    }

    void ApplyLayers()
    {
        main.Limit.SetTailVisible(layers.Tail);
        main.Limit.SetSpiralVisible(layers.Chain);
        main.SetZeroMarkersVisible(layers.Zeros);
        main.SetZetaCurveVisible(layers.Trail);
        main.SetCosineVisible(layers.Waves);
        main.SetPrimeCurveVisible(layers.Stairs);
        main.SetGridVisible(layers.Grid);
        main.SetEquationsVisible(layers.Equations);
    }

    /// <summary>Leave the narrated tour and show one visualization on its own, framed for the user, running.</summary>
    public void ApplyView(string id)
    {
        if (tour.Running) tour.StopTour();
        tour.RestoreFreeRoam();
        main.Speed = 1.8d;
        main.SpeedMultiplier = 1d;
        layers.All(true);

        Grab.LockRotation = false;
        string cue = "machine";
        double jump = -1d;
        bool play = true;
        string[] actions = System.Array.Empty<string>();
        string name = id;
        switch (id)
        {
            case "chain":
                layers.Stairs = layers.Waves = layers.Equations = false;
                actions = new[] { "show:tip", "show:center", "show:polelabel", "show:outaxes" };
                cue = "spiral_close";
                name = "The chain of arrows. Turn the dial with the speed slider";
                break;
            case "zeros":
                layers.Stairs = layers.Waves = layers.Equations = false;
                actions = new[] { "show:polelabel", "show:outaxes", "labels:all" };
                cue = "markers_axis";
                jump = 20d;
                name = "Every collision drops a marker on the pole";
                break;
            case "strip":
                layers.Stairs = layers.Waves = layers.Equations = layers.Zeros = layers.Trail = false;
                actions = new[] { "show:strip", "show:zcurve", "show:imaxis", "show:outaxes", "show:band78", "show:band18" };
                cue = "strip_panel";
                jump = 30d;
                name = "The critical strip, and the 2026 walls at 7/8 and 1/8";
                break;
            case "waves":
                layers.Stairs = layers.Chain = layers.Tail = layers.Trail = layers.Equations = false;
                cue = "cosine_wave";
                jump = 100d;
                play = false; // a still picture: replaying it would redraw the chorus every time a collision passes
                name = "One cosine wave per collision, added together";
                break;
            case "stairs":
                layers.Waves = layers.Chain = layers.Tail = layers.Trail = layers.Equations = false;
                cue = "staircase";
                jump = 100d;
                play = false;
                name = "The prime staircase from Riemann's formula";
                break;
            case "primes":
                layers.All(false);
                actions = new[] { "sacks:show" };
                cue = "sacks_flat";
                play = false;
                name = "The primes on the Sacks spiral";
                break;
            case "saddle":
                layers.All(false);
                actions = new[] { "sacks:show", "sacks:saddle", "sacks:routes" };
                cue = "sacks_saddle";
                play = false;
                name = "The spiral bent over a saddle";
                break;
            case "explore":
                // the final picture at room scale: you stand at the saddle's centre, the saddle is the floor, and the
                // zeta trail grows from the floor up; every zero it passes sends its wave along the spiral into the master wave
                layers.All(false);
                ApplyLayers();
                main.Pause();
                tour.RunActions(new[] { "home:scene" });
                Guide.SetFollow(false);
                Grab.LockRotation = true; // standing view: pinch slides and scales, never turns
                Rig.CancelAnimation();
                Rig.SetPlacement(ExplorePlacement()); // standing on the saddle: the zeta trail is vertical, straight up from the floor
                Rig.AnimateTo(ExplorePlacement(), 0.3f);
                return;
            default:
                jump = 40d;
                name = "The whole machine";
                break;
        }

        ApplyLayers();
        exploreLoopStart = System.Math.Max(0d, jump);
        if (jump >= 0d) main.JumpTo(jump);
        else main.ResetAnimation();
        tour.RunActions(actions);
        if (play) main.Play();
        else main.Pause();

        Guide.ResetOffsets();
        Guide.SetFollow(true, true);
        tour.Rig.SetCue(cue);
        Toast(name);
    }

    /// <summary>Standing on the saddle's centre: rig 'up' = the saddle normal (world −Z), model at half size.</summary>
    VRRig.Placement ExplorePlacement()
    {
        const float scale = 2f;
        Vector3 centre = tour.Annotations.Sacks.Center;
        Quaternion rotation = Quaternion.LookRotation(Vector3.up, Vector3.back) * Quaternion.Inverse(Rig.YawHome);
        Vector3 floorLocal = new(Rig.HeadHome.x, 0f, Rig.HeadHome.z);
        return new VRRig.Placement { Rotation = rotation, Scale = scale, Position = centre - rotation * (scale * floorLocal) };
    }

    // ---- space page ----------------------------------------------------------------------------------

    void BuildSpacePage(RectTransform p)
    {
        (string label, VRIcons.Id icon, VRGuide.NudgeKind kind, float amount)[] buttons =
        {
            ("Bigger", VRIcons.Id.ZoomIn, VRGuide.NudgeKind.Zoom, 1f),
            ("Smaller", VRIcons.Id.ZoomOut, VRGuide.NudgeKind.Zoom, -1f),
            ("Turn left", VRIcons.Id.RotateLeft, VRGuide.NudgeKind.Yaw, -1f),
            ("Turn right", VRIcons.Id.RotateRight, VRGuide.NudgeKind.Yaw, 1f),
            ("Tilt up", VRIcons.Id.TiltUp, VRGuide.NudgeKind.Pitch, 1f),
            ("Tilt down", VRIcons.Id.TiltDown, VRGuide.NudgeKind.Pitch, -1f),
            ("Raise", VRIcons.Id.Up, VRGuide.NudgeKind.Raise, -1f),
            ("Lower", VRIcons.Id.Down, VRGuide.NudgeKind.Raise, 1f),
            ("Closer", VRIcons.Id.Closer, VRGuide.NudgeKind.Closer, 1f),
            ("Farther", VRIcons.Id.Farther, VRGuide.NudgeKind.Closer, -1f),
        };
        for (int i = 0; i < buttons.Length; i++)
        {
            var b = buttons[i];
            int col = i % 6, row = i / 6;
            Dock.AddButton(8 + col * 99, 4 + row * 102, 91, 96, b.label, b.icon, () => Guide.Nudge(b.kind, b.amount), false, null, 18f, p);
        }

        // the last two cells of the second row
        Dock.AddButton(8 + 4 * 99, 4 + 102, 91, 96, "Reset", VRIcons.Id.Reset, ResetModelView, false, null, 18f, p);
        Dock.AddButton(8 + 5 * 99, 4 + 102, 91, 96, "Re-centre", VRIcons.Id.Target, () =>
        {
            Rig.Recenter();
            Rig.SnapHud();
            SummonDock();
            Toast("Centred on where you stand and look");
        }, false, null, 18f, p);

        Dock.AddLabel(10, 212, 580, 80, "Pinch and drag in the air to move the model.\nPinch with both hands to scale and turn it.", 22f, VRStyle.TextDim, TextAlignmentOptions.Left, FontStyles.Normal, p);
    }

    void ResetModelView()
    {
        Guide.ResetView();
        Guide.SetFollow(true, true);
        Toast("Back to the story's view");
    }

    // ---- settings page --------------------------------------------------------------------------------

    void BuildSettingsPage(RectTransform p)
    {
        Dock.AddButton(206, 4, 190, 78, "Hand outline", VRIcons.Id.Skeleton, () =>
        {
            Visual.Skeleton = Visual.Skeleton switch
            {
                VRHandVisual.SkeletonMode.Auto => VRHandVisual.SkeletonMode.On,
                VRHandVisual.SkeletonMode.On => VRHandVisual.SkeletonMode.Off,
                _ => VRHandVisual.SkeletonMode.Auto
            };
            Toast("Hand outline: " + Visual.Skeleton + (Visual.Skeleton == VRHandVisual.SkeletonMode.Auto ? " (shown in blackout)" : ""));
        }, true, () => Visual.Skeleton != VRHandVisual.SkeletonMode.Off, 21f, p);
        Dock.AddButton(404, 4, 190, 78, "Gentle motion", VRIcons.Id.Speed, () => Guide.Comfort = !Guide.Comfort, true, () => Guide.Comfort, 21f, p);

        Dock.AddButton(8, 90, 190, 78, "Grid", VRIcons.Id.Grid, () => { layers.Grid = !layers.Grid; ApplyLayers(); }, true, () => layers.Grid, 21f, p);
        Dock.AddButton(206, 90, 190, 78, "Formulas", VRIcons.Id.Equations, () => { layers.Equations = !layers.Equations; ApplyLayers(); }, true, () => layers.Equations, 21f, p);
        Dock.AddButton(404, 90, 190, 78, "Play on", VRIcons.Id.NextChapter, () => autoNextChapter = !autoNextChapter, true, () => autoNextChapter, 21f, p);

        Dock.AddSlider(10, 176, 580, 56, "Volume", 0f, 1f, () => AudioListener.volume, v => AudioListener.volume = v, v => $"{Mathf.RoundToInt(v * 100f)}%", p);
        Dock.AddSlider(10, 236, 580, 56, "Model distance", 0.5f, 2f, () => Rig.ModelDistance, v => Rig.ModelDistance = v, v => $"{v:0.0} m", p);
    }

    // ---- footer: transport, follow, room / dark ----------------------------------------------------------

    void BuildFooter()
    {
        const float y = 372f, h = 70f, w = 70f;
        prevChapterButton = Dock.AddButton(8, y, w, h, "Back", VRIcons.Id.PrevChapter, () => tour.PreviousChapter(), false, null, 18f);
        replayButton = Dock.AddButton(83, y, w, h, "Replay", VRIcons.Id.Replay, () => tour.ReplayLine(), false, null, 18f);
        VRButton play = Dock.AddButton(158, y, w, h, "Pause", VRIcons.Id.Pause, PlayPause, false, null, 18f);
        play.Dynamic(() => PlayingNow ? VRIcons.Id.Pause : VRIcons.Id.Play, () => PlayingNow ? "Pause" : "Play");
        skipButton = Dock.AddButton(233, y, w, h, "Skip", VRIcons.Id.NextLine, () => tour.NextLine(), false, null, 18f);
        nextChapterButton = Dock.AddButton(308, y, w, h, "Next", VRIcons.Id.NextChapter, NextChapter, false, null, 18f);

        Dock.AddButton(386, y, 74, h, "Follow", VRIcons.Id.Pin, ToggleFollow, true, () => Guide.Follow, 18f);
        Dock.AddButton(466, y, 62, h, "Room", VRIcons.Id.Eye, () => SetRoom(true), true, () => Rig.Passthrough, 18f);
        Dock.AddButton(534, y, 58, h, "Dark", VRIcons.Id.Moon, () => SetRoom(false), true, () => !Rig.Passthrough, 18f);
    }

    void PlayPause() => tour.TogglePause();

    void NextChapter()
    {
        if (tour.Running) tour.NextChapter();
        else PlayChapter(0);
    }

    void ToggleFollow()
    {
        if (Guide.Follow)
        {
            Guide.SetFollow(false);
            Toast("The model stays where it is. Pinch to move it");
        }
        else
        {
            Guide.ResetOffsets();
            Guide.SetFollow(true);
            Toast("Following the story");
        }
    }

    public void SetRoom(bool room)
    {
        Rig.SetPassthrough(room);
        Toast(room ? "Room view (passthrough)" : "Blackout. Only the model is visible");
    }

    // ---- palm menu --------------------------------------------------------------------------------------

    void BuildPalm()
    {
        // a pad about as wide as a hand, floating over the palm: two rows of three 42 mm buttons
        Palm = VRPanel.Create("Palm menu", new Vector2(152f, 118f), Rig.Ui);
        const float w = 42f, h = 48f, gap = 6f, x0 = 8f, y0 = 8f;
        float X(int c) => x0 + c * (w + gap);
        Palm.AddButton(X(0), y0, w, h, "Menu", VRIcons.Id.Menu, ToggleDock, false, null, 13f);
        VRButton play = Palm.AddButton(X(1), y0, w, h, "Pause", VRIcons.Id.Pause, PlayPause, false, null, 13f);
        play.Dynamic(() => PlayingNow ? VRIcons.Id.Pause : VRIcons.Id.Play, () => PlayingNow ? "Pause" : "Play");
        Palm.AddButton(X(2), y0, w, h, "Skip", VRIcons.Id.NextLine, () => tour.NextLine(), false, null, 13f);
        float y1 = y0 + h + gap;
        Palm.AddButton(X(0), y1, w, h, "Follow", VRIcons.Id.Pin, ToggleFollow, true, () => Guide.Follow, 13f);
        Palm.AddButton(X(1), y1, w, h, "Room", VRIcons.Id.Eye, () => SetRoom(true), true, () => Rig.Passthrough, 13f);
        Palm.AddButton(X(2), y1, w, h, "Dark", VRIcons.Id.Moon, () => SetRoom(false), true, () => !Rig.Passthrough, 13f);
    }

    // ---- state shown on the buttons ----------------------------------------------------------------------

    void UpdateButtonsAvailability()
    {
        bool running = tour.Running;
        replayButton.Interactable = running;
        skipButton.Interactable = running;
        prevChapterButton.Interactable = running;

        // while a module plays, Chapters is the experiments' list: each page describes only its own kind
        bool lab = tour.PlayingExperiment;
        TourChapter[] chapters = tour.Chapters;
        bool known = running && tour.CurrentChapterIndex >= 0 && tour.CurrentChapterIndex < chapters.Length;
        int key = known ? (tour.CurrentChapterIndex * 2 + (tour.Paused ? 1 : 0)) * 2 + (lab ? 1 : 0) : -1;
        if (key == nowKey) return;
        nowKey = key;
        string playing = known
            ? $"<color=#FFD129>{tour.CurrentChapterIndex + 1}</color>  {chapters[tour.CurrentChapterIndex].title}" + (tour.Paused ? "   (paused)" : "")
            : null;
        if (nowLabel != null) nowLabel.text = playing != null && !lab ? playing : "Touch a chapter to hear the story";
        if (labLabel != null) labLabel.text = playing != null && lab ? playing : "Touch an experiment to watch it";
    }
}
