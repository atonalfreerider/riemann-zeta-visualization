using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Plays the narrated, chaptered fly-through described in Resources/Narration/script.json.
/// For each line: moves the camera to its cue, applies its actions to the visualization,
/// plays the narration clip (if generated) and runs the karaoke captions off the tour clock.
///
/// Keys:  T start/stop tour · N skip to next line · F9 start/stop recording · P toggle portrait · M side menu
/// </summary>
public class TourDirector : MonoBehaviour
{
    public static TourDirector Instance { get; private set; }

    public bool AutoStart;
    public bool RecordOnStart;
    /// <summary>Storyboard: snap the camera per line, compress each line to a few seconds and save two PNGs per line.</summary>
    public bool Storyboard;
    public float StoryboardSecondsPerLine = 3f;
    public string StoryboardDirectory = "";
    /// <summary>Where full recordings go (empty = Recordings/&lt;timestamp&gt; next to the project).</summary>
    public string RecordDirectory = "";
    /// <summary>9:16 framing (phones, Shorts) instead of 16:9. Defaults to the shape of the screen.</summary>
    public bool Portrait = true;
    public string StartChapter = "";
    /// <summary>Stop after this chapter id (empty = play to the end).</summary>
    public string EndChapter = "";
    public float ChapterTitleSeconds = 3.2f;

    public bool Running { get; private set; }
    /// <summary>Log wall-clock frame time per narration line (avg / max ms) to find slow lines.</summary>
    public bool Profile;
    public Main Main { get; private set; }
    public Annotations Annotations { get; private set; }
    public CameraRig Rig { get; private set; }
    public CaptionDisplay Captions { get; private set; }
    public FrameRecorder Recorder { get; private set; }
    public ExploreMode Explore { get; private set; }
    public HomeView Home { get; private set; }

    /// <summary>(line id, tour clock seconds, recorder frame) for every narration clip started — used to mux audio.</summary>
    public readonly List<(string id, float time, int frame)> AudioTimeline = new();

    TourScriptData script;
    AudioSource audioSource;
    Coroutine tourRoutine;
    float tourStartTime;
    bool skipRequested;
    bool replayRequested;

    // ---- navigation state (used by the VR interface, harmless on the desktop) -----------------------
    public bool Paused { get; private set; }
    public string CurrentChapterId { get; private set; } = "";
    public int CurrentChapterIndex { get; private set; } = -1;
    public string CurrentLineId { get; private set; } = "";
    /// <summary>Seconds into the current line, and how long the line lasts.</summary>
    public float LineElapsed { get; private set; }
    public float LineDuration { get; private set; }
    public TourChapter[] Chapters => script?.chapters ?? System.Array.Empty<TourChapter>();
    /// <summary>When true the tour carries on into the next chapter; when false it stops at the end of the chapter it was started on.</summary>
    public bool ContinueIntoNextChapter = true;
    public event Action<int, TourChapter> ChapterStarted;
    public event Action<TourLine> LineStarted;
    public event Action TourEnded;

    void Awake()
    {
        Instance = this;
        // a monitor stays landscape, a phone held upright is portrait (the command line and VR can still override)
        Portrait = Screen.height > Screen.width;
    }

    void Start()
    {
        Main = Main.Instance != null ? Main.Instance : FindAnyObjectByType<Main>();
        Camera cam = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();

        Annotations = Annotations.Create(Main);
        Rig = CameraRig.Create(cam, Main, Annotations);
        Rig.Portrait = Portrait;
        Captions = CaptionDisplay.Create(cam);
        Recorder = FrameRecorder.Create(cam);
        Explore = ExploreMode.Create(this);
        Home = HomeView.Create(this);
        SetFormat(Portrait);
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        script = TourScriptLoader.Load();
        if (!VRMode.Enabled) SideMenu.Create(this);

        if (AutoStart)
        {
            StartTour();
        }
        else if (!VRMode.Enabled)
        {
            // the first thing on screen: the saddle, slowly orbiting, with the zeta trail growing out of its centre
            Home.Enter();
        }
    }

    /// <summary>16:9 or 9:16: the preview framing, the caption layout and the recording size follow it.</summary>
    public void SetFormat(bool portrait)
    {
        Portrait = portrait;
        if (Rig != null) Rig.Portrait = portrait;
        if (Recorder != null && !Recorder.IsOffscreen && !VRMode.Enabled)
        {
            Recorder.Width = portrait ? 1080 : 1920;
            Recorder.Height = portrait ? 1920 : 1080;
        }

        if (Captions != null) Captions.SetLayout(!portrait);
    }

    /// <summary>Record the tour from a chapter (the whole tour when <paramref name="chapterId"/> is empty) in the given format.</summary>
    public void RecordTour(string chapterId, bool continueOn, bool portrait)
    {
        if (Recorder.IsRecording) StopRecording();
        if (Running)
        {
            jumping = true;
            StopTour();
            jumping = false;
        }

        SetFormat(portrait);
        RecordOnStart = true;
        if (string.IsNullOrEmpty(chapterId)) chapterId = Chapters.Length > 0 ? Chapters[0].id : "";
        GoToChapter(chapterId, continueOn);
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.tKey.wasPressedThisFrame)
        {
            if (Running) StopTour();
            else StartTour();
        }

        if (keyboard.nKey.wasPressedThisFrame && Running)
        {
            skipRequested = true;
        }

        if (keyboard.f9Key.wasPressedThisFrame)
        {
            if (Recorder.IsRecording) StopRecording();
            else StartRecording();
        }

        if (keyboard.pKey.wasPressedThisFrame && !Recorder.IsOffscreen)
        {
            SetFormat(!Portrait);
        }

        if (keyboard.fKey.wasPressedThisFrame)
        {
            Captions.ShowFps = !Captions.ShowFps;
            if (!Captions.ShowFps) Captions.SetHint("");
        }
    }

    // ---- control -----------------------------------------------------------------------------

    public void StartTour()
    {
        if (Running) return;
        Running = true;
        skipRequested = false;
        replayRequested = false;
        AudioTimeline.Clear();
        Recorder.ChapterMarks.Clear();
        tourStartTime = Time.time;
        exploreAfterTour = false;
        if (Home != null) Home.Exit();
        if (Explore != null && Explore.Active) Explore.Exit();
        // captions follow the shape of the recording, or of the chosen format
        Captions.SetLayout(RecordOnStart || Storyboard || Recorder.IsOffscreen ? Recorder.Width > Recorder.Height : !Portrait);

        if (Storyboard)
        {
            string dir = string.IsNullOrEmpty(StoryboardDirectory)
                ? System.IO.Path.Combine(Application.dataPath, "..", "Recordings", "storyboard-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"))
                : StoryboardDirectory;
            Recorder.BeginOffscreen(dir);
            Debug.Log("Storyboard → " + Recorder.OutputDirectory);
        }
        else if (RecordOnStart && !Recorder.IsRecording)
        {
            StartRecording();
        }

        RecordOnStart = false; // one recording per request; the next plain start just plays

        tourRoutine = StartCoroutine(RunTour());
    }

    public void StopTour()
    {
        if (!Running) return;
        Running = false;
        SetPaused(false);
        if (tourRoutine != null) StopCoroutine(tourRoutine);
        tourRoutine = null;
        audioSource.Stop();
        Captions.Clear();
        Rig.Release();
        if (Recorder.IsRecording) StopRecording();
        CurrentChapterId = "";
        CurrentChapterIndex = -1;
        CurrentLineId = "";
        if (!jumping)
        {
            TourEnded?.Invoke();
            if (!VRMode.Enabled && !Explore.Active) Home.Enter();
        }
    }

    bool jumping;

    // ---- navigation (VR interface) -------------------------------------------------------------

    /// <summary>Pause or resume everything: animation, narration and the tour clock (Time.timeScale), not the interface.</summary>
    public void SetPaused(bool paused)
    {
        if (Paused == paused) return;
        Paused = paused;
        Time.timeScale = paused ? 0f : 1f;
        AudioListener.pause = paused;
    }

    public void TogglePause()
    {
        if (Running) SetPaused(!Paused);
        else Main.Toggle();
    }

    /// <summary>Start (or restart) the narrated tour at a chapter, from a clean stage.</summary>
    public void GoToChapter(string chapterId, bool continueOn = true)
    {
        if (Running)
        {
            jumping = true;
            StopTour();
            jumping = false;
        }

        Main.CancelAnimations();
        Annotations.CancelAnimations();
        ContinueIntoNextChapter = continueOn;
        StartChapter = chapterId;
        EndChapter = continueOn ? "" : chapterId;
        StartTour();
    }

    public void GoToChapter(int index, bool continueOn = true)
    {
        if (index < 0 || index >= Chapters.Length) return;
        GoToChapter(Chapters[index].id, continueOn);
    }

    public void NextChapter()
    {
        int next = CurrentChapterIndex + 1;
        if (!Running || next >= Chapters.Length) return;
        GoToChapter(next, ContinueIntoNextChapter);
    }

    /// <summary>Restart the current chapter; pressed again within a moment it goes back one chapter.</summary>
    public void PreviousChapter()
    {
        if (!Running) return;
        int target = CurrentChapterIndex;
        if (ChapterElapsed < 3f && target > 0) target--;
        GoToChapter(target, ContinueIntoNextChapter);
    }

    float chapterStartedAt;
    float ChapterElapsed => Time.unscaledTime - chapterStartedAt;

    public void NextLine()
    {
        if (Running) skipRequested = true;
    }

    /// <summary>Play the current line again from its beginning (its camera and actions run again).</summary>
    public void ReplayLine()
    {
        if (!Running) return;
        replayRequested = true;
        skipRequested = true;
    }

    /// <summary>Run action strings such as "show:strip" or "sacks:saddle" (the same vocabulary as script.json).</summary>
    public void RunActions(params string[] actions) => ApplyActions(actions);

    float currentLineSeconds = 3f;
    bool exploreAfterTour;

    int lineSerial;

    IEnumerator Delayed(float seconds, string action)
    {
        int serial = lineSerial;
        yield return new WaitForSeconds(Storyboard ? seconds * (StoryboardSecondsPerLine / Mathf.Max(currentLineSeconds, 0.1f)) : seconds);
        // only if the tour is still on the line that scheduled it (skipping ahead cancels it)
        if (Running && serial == lineSerial) ApplyActions(new[] { action });
    }

    void PulseElement(string element, float seconds)
    {
        List<Renderer> renderers = new(Main.RenderersOf(element));
        renderers.AddRange(Annotations.RenderersOf(element));
        if (element == "sacks") renderers.AddRange(Annotations.Sacks.Renderers());
        if (renderers.Count == 0) Debug.LogWarning($"Tour: nothing to pulse for '{element}'");
        Annotations.PulseRenderers(renderers, seconds);
    }

    IEnumerator BuildWaves(int from, int to)
    {
        float seconds = Mathf.Max(0.5f, currentLineSeconds * 0.9f);
        int available = Main.PassedZeros.Count;
        int target = to < 0 ? available : Mathf.Min(to, available);
        int steps = Mathf.Max(1, target - from);
        float perStep = seconds / steps;
        bool showEach = perStep >= 0.6f; // only preview each incoming wave when there is time to see it

        for (int n = from; n < target; n++)
        {
            float amplitude = Main.CosineScale / (n + 1);
            if (showEach)
            {
                Annotations.SetWaveCount(n + 1);
                Annotations.AddStripDial(n, perStep * 0.5f);
                yield return Annotations.IncomingWaveRoutine(n, amplitude, perStep);
            }
            else
            {
                Annotations.SetWaveCount(n + 1);
                if (n < 12) Annotations.AddStripDial(n, perStep * 0.8f);
                Main.SetCosineZeroLimitAnimated(n + 1, perStep);
                yield return new WaitForSeconds(perStep);
            }
        }

        Annotations.HideIncomingWave();
        Main.SetCosineZeroLimitAnimated(to < 0 ? -1 : target, 0.6f);
    }

    IEnumerator BuildTerms(int from, int to)
    {
        float seconds = Mathf.Max(0.5f, currentLineSeconds * 0.85f);
        float start = Time.time;
        int last = -1;
        while (Time.time - start < seconds)
        {
            float v = Mathf.Lerp(from, to, (Time.time - start) / seconds);
            int n = Mathf.FloorToInt(v);
            Main.Limit.TermOverride = Mathf.Max(1, n + 1);
            Main.Limit.TermFraction = Mathf.Clamp01(v - n);
            if (n + 1 > to) { Main.Limit.TermOverride = to; Main.Limit.TermFraction = 1f; }
            Main.Limit.RenderLimitCurve(Main.CurrentT, null, null);
            last = n;
            yield return null;
        }

        Main.Limit.TermOverride = to;
        Main.Limit.TermFraction = 1f;
        Main.Limit.RenderLimitCurve(Main.CurrentT, null, null);
    }

    /// <summary>Jump to the next narration line (same as pressing N).</summary>
    public void SkipCurrentLine()
    {
        if (Running) skipRequested = true;
    }

    public void StartRecording()
    {
        string dir = string.IsNullOrEmpty(RecordDirectory)
            ? System.IO.Path.Combine(Application.dataPath, "..", "Recordings", DateTime.Now.ToString("yyyyMMdd-HHmmss"))
            : RecordDirectory;
        Recorder.StartRecording(dir);
    }

    public void StopRecording()
    {
        Recorder.StopRecording(AudioTimeline);
    }

    // ---- sequencing ----------------------------------------------------------------------------

    IEnumerator RunTour()
    {
        yield return null; // let Main/LimitCurve/Graph finish Start()

        foreach (TourChapter chapter in script.chapters)
        {
            foreach (TourLine line in chapter.lines)
            {
                if (!string.IsNullOrEmpty(line.camera) && !Rig.HasCue(line.camera))
                {
                    Debug.LogWarning($"Tour: line {line.id} uses unknown camera cue '{line.camera}'");
                }
            }
        }

        Main.CancelAnimations();
        Annotations.CancelAnimations();
        Annotations.HideAll();
        Main.SetSigma(.5d);
        Main.ResetAnimation();
        Main.Speed = 1.8d;
        Main.ZeroLabelLimit = 6;
        Main.CosineScale = 11f;
        Main.SetCosineZeroLimit(-1);
        Main.Limit.TermOverride = 0;
        Main.Limit.TermFraction = 1f;
        Main.Limit.SetTailVisible(true);
        Main.Limit.SetArrowheadsVisible(true);
        Main.Limit.SetLengthLabelsVisible(false);
        Main.Limit.SetTailGhostVisible(false);
        Main.SetTrailStyle(0.05f, Factory.SoftGlowIntensity, 0f);
        // start from an empty stage; chapters reveal what they need
        Main.SetPrimeCurveVisible(false);
        Main.SetCosineVisible(false);
        Main.SetEquationsVisible(false);
        Main.SetZetaCurveVisible(false);
        Main.SetGridVisible(false);

        bool started = string.IsNullOrEmpty(StartChapter);
        int total = script.chapters.Length;
        for (int c = 0; c < total; c++)
        {
            TourChapter chapter = script.chapters[c];
            if (!started)
            {
                if (chapter.id != StartChapter) continue;
                started = true;
            }

            CurrentChapterIndex = c;
            CurrentChapterId = chapter.id;
            chapterStartedAt = Time.unscaledTime;
            ChapterStarted?.Invoke(c, chapter);
            Captions.HideBoard();
            Captions.ShowEquations("off");
            Captions.SetChapterIndex(c + 1, total, chapter.title);
            Captions.ShowChapterTitle(chapter.title, chapter.subtitle, ChapterTitleSeconds);
            Recorder.ChapterMarks.Add((chapter.id, Recorder.FrameIndex));

            foreach (TourLine line in chapter.lines)
            {
                yield return PlayLine(line);
            }

            if (!string.IsNullOrEmpty(EndChapter) && chapter.id == EndChapter) break;
        }

        Captions.Clear();
        yield return new WaitForSeconds(1.5f);
        Running = false;
        Rig.Release();
        bool explore = exploreAfterTour && !Storyboard && !Recorder.IsOffscreen;
        if (Recorder.IsRecording) StopRecording();
        else if (Recorder.IsOffscreen) Recorder.EndOffscreen();
        Main.SpeedMultiplier = 1d;
        // the tour ends on the same picture the app opens with (in VR: standing on the saddle as the trail grows)
        if (VRMode.Enabled) { if (VRMode.App != null) VRMode.App.ApplyView("explore"); }
        else if (explore) RestoreFreeRoam();
        else Home.Enter();
        CurrentChapterId = "";
        CurrentChapterIndex = -1;
        CurrentLineId = "";
        TourEnded?.Invoke();
        if (explore)
        {
            exploreAfterTour = false;
            Explore.Enter();
        }

#if UNITY_EDITOR
        if (UnityEditor.EditorPrefs.GetBool("Zeta.BatchQuitAfterPlay", false))
        {
            Debug.Log("Tour finished in batch mode; leaving play mode.");
            UnityEditor.EditorApplication.ExitPlaymode();
        }
#endif
    }

    /// <summary>Put the stage back the way the original free-roam visualization starts (everything on, no annotations).</summary>
    public void RestoreFreeRoam()
    {
        Main.CancelAnimations();
        Annotations.CancelAnimations();
        Main.SetPrimeCurveVisible(true);
        Main.SetCosineVisible(true);
        Main.SetEquationsVisible(true);
        Main.SetZetaCurveVisible(true);
        Main.SetGridVisible(true);
        Main.SetZeroMarkersVisible(true);
        Main.Limit.SetSpiralVisible(true);
        Main.Limit.SetTailVisible(true);
        Main.Limit.SetArrowheadsVisible(true);
        Main.Limit.SetLengthLabelsVisible(false);
        Main.Limit.SetTailGhostVisible(false);
        Main.Limit.TermOverride = 0;
        Main.Limit.TermFraction = 1f;
        Main.ZeroLabelLimit = -1;
        Main.CosineScale = -1f;
        Main.SetCosineZeroLimit(-1);
        Main.SetTrailStyle(0.05f, Factory.SoftGlowIntensity, 0f);
        Annotations.Sacks.StopGrowth();
        Annotations.HideAll();
    }

    IEnumerator PlayLine(TourLine line)
    {
        CurrentLineId = line.id;
        replayRequested = false;
        yield return PlayLineOnce(line);
        while (replayRequested && Running)
        {
            replayRequested = false;
            yield return PlayLineOnce(line);
        }
    }

    IEnumerator PlayLineOnce(TourLine line)
    {
        skipRequested = false;
        lineSerial++;
        Rig.SetCue(line.camera);

        LineTiming timing = TourScriptLoader.LoadTiming(line.id);
        AudioClip clip = TourScriptLoader.LoadAudio(line.id);
        currentLineSeconds = Storyboard ? StoryboardSecondsPerLine : (clip != null ? clip.length : (timing ?? TourScriptLoader.Estimate(line.text)).duration);
        ApplyActions(line.actions);
        if (timing == null)
        {
            timing = TourScriptLoader.Estimate(line.text);
            if (clip != null && clip.length > 0.5f)
            {
                // scale the estimate onto the real clip length
                float scale = clip.length / timing.duration;
                foreach (WordTiming w in timing.words)
                {
                    w.s *= scale;
                    w.e *= scale;
                }

                timing.duration = clip.length;
            }
        }

        float duration = clip != null ? Mathf.Max(clip.length, timing.duration) : timing.duration;
        Captions.SetLine(timing);
        Captions.SetFootnote(line.footnote);
        LineDuration = duration + Mathf.Max(0f, line.pause);
        LineElapsed = 0f;
        LineStarted?.Invoke(line);

        if (Storyboard)
        {
            yield return StoryboardLine(line, timing, duration + Mathf.Max(0f, line.pause));
            yield break;
        }

        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
            AudioTimeline.Add((line.id, Time.time - tourStartTime, Recorder.FrameIndex));
        }

        float start = Time.time;
        float wallPrev = Time.realtimeSinceStartup, wallSum = 0f, wallMax = 0f;
        int wallFrames = 0;
        int gc0 = System.GC.CollectionCount(0);
        while (Time.time - start < duration + Mathf.Max(0f, line.pause))
        {
            if (skipRequested)
            {
                audioSource.Stop();
                break;
            }

            LineElapsed = Time.time - start;
            Captions.SetTime(LineElapsed);
            yield return null;
            float now = Time.realtimeSinceStartup;
            float dt = now - wallPrev;
            wallPrev = now;
            wallSum += dt;
            wallMax = Mathf.Max(wallMax, dt);
            wallFrames++;
        }

        if (Profile && wallFrames > 0)
        {
            Debug.Log($"[profile] {line.id,-12} avg {wallSum / wallFrames * 1000f,6:0.0} ms   max {wallMax * 1000f,5:0} ms   GC {System.GC.CollectionCount(0) - gc0,3}   frames {wallFrames}");
        }
    }

    int storyboardIndex;

    IEnumerator StoryboardLine(TourLine line, LineTiming timing, float realDuration)
    {
        // compress the real line duration into StoryboardSecondsPerLine, keeping the same amount of animation
        float seconds = Mathf.Max(0.5f, StoryboardSecondsPerLine);
        Main.SpeedMultiplier = realDuration / seconds;
        Rig.Snap();
        yield return null;
        Rig.Snap();

        int index = storyboardIndex++;
        float start = Time.time;
        bool first = false;
        while (Time.time - start < seconds)
        {
            float elapsed = Time.time - start;
            Captions.SetTime(elapsed / seconds * timing.duration);
            if (!first && elapsed >= seconds * 0.45f)
            {
                first = true;
                Recorder.Snapshot($"{index:D2}_{line.id}_a.png");
            }

            yield return null;
        }

        Recorder.Snapshot($"{index:D2}_{line.id}_b.png");
        yield return null;
        yield return null;
    }

    /// <summary>
    /// Action vocabulary (see docs/TOUR.md):
    ///   play · pause · reset · speed:X · jump:T · flash:zero · flash:tail · ghost:on · ghost:off · ghost:beta:gamma
    ///   show:/hide: tail spiral components strip axes imaxis outaxes zcurve band78 band18 classic band1112 gap
    ///                cosine prime equations zeros zetacurve
    /// </summary>
    void ApplyActions(string[] actions)
    {
        if (actions == null) return;
        foreach (string raw in actions)
        {
            string[] parts = raw.Split(':');
            string verb = parts[0].Trim().ToLowerInvariant();
            string arg = parts.Length > 1 ? parts[1].Trim().ToLowerInvariant() : "";
            try
            {
                switch (verb)
                {
                    case "delay":
                        // delay:SECONDS:action[:args] — run the rest after a pause (e.g. delay:3:sacks:sieve)
                        StartCoroutine(Delayed(float.Parse(parts[1], CultureInfo.InvariantCulture), string.Join(":", parts, 2, parts.Length - 2)));
                        break;
                    case "play": Main.Play(); break;
                    case "pause": Main.Pause(); break;
                    case "reset": Main.ResetAnimation(); break;
                    case "speed": Main.Speed = double.Parse(arg, CultureInfo.InvariantCulture); break;
                    case "jump": Main.JumpTo(double.Parse(arg, CultureInfo.InvariantCulture)); break;
                    case "flash":
                        if (arg == "zero") Annotations.PulseZero(Main.ZeroCount - 1);
                        else if (arg == "tail") Annotations.PulseTail();
                        break;
                    case "terms":
                        Main.Limit.TermOverride = arg == "auto" ? 0 : int.Parse(arg, CultureInfo.InvariantCulture);
                        Main.Limit.RenderLimitCurve(Main.CurrentT, null, null);
                        break;
                    case "build":
                        // build:from:to — raise the arrow count across the line's duration
                        StartCoroutine(BuildTerms(int.Parse(parts[1], CultureInfo.InvariantCulture), int.Parse(parts[2], CultureInfo.InvariantCulture)));
                        break;
                    case "sigma": Main.SetSigma(double.Parse(arg, CultureInfo.InvariantCulture)); break;
                    case "cosine":
                        if (arg == "build")
                        {
                            // cosine:build:from:to — add collision waves one at a time across the line ("to" may be "all")
                            int from = int.Parse(parts[2], CultureInfo.InvariantCulture);
                            int to = parts[3] == "all" ? -1 : int.Parse(parts[3], CultureInfo.InvariantCulture);
                            StartCoroutine(BuildWaves(from, to));
                        }
                        else
                        {
                            Main.SetCosineZeroLimit(arg == "all" ? -1 : int.Parse(arg, CultureInfo.InvariantCulture));
                        }
                        break;
                    case "wave":
                        // wave:plain[:γ] · wave:dial:γ (sweep γ across the line) · wave:log (bend with the logarithm) · wave:off
                        if (arg == "plain") Annotations.ShowDemoWavePlain(Main.CosineScale, parts.Length > 2 ? float.Parse(parts[2], CultureInfo.InvariantCulture) : 2.4f);
                        else if (arg == "dial") Annotations.SweepDemoGamma(float.Parse(parts[2], CultureInfo.InvariantCulture), Mathf.Max(1f, currentLineSeconds * 0.8f));
                        else if (arg == "log" || arg == "morph") Annotations.MorphDemoWave(Mathf.Max(1f, currentLineSeconds * 0.7f), Main.CosineScale);
                        else Annotations.HideDemoWave();
                        break;
                    case "dial": Annotations.ShowDial(arg != "off"); break;
                    case "zeno":
                        if (arg == "build") Annotations.BuildZeno(Mathf.Max(2f, currentLineSeconds * 0.85f));
                        else if (arg == "reset") Annotations.ResetZeno();
                        break;
                    case "card":
                        if (arg == "off") Captions.HideCard();
                        else Captions.ShowCard(arg);
                        break;
                    case "trail":
                        Main.SetZetaCurveVisible(true);
                        if (arg == "thin") Main.SetTrailStyle(0.012f, 0.75f, 1f);
                        else Main.SetTrailStyle(0.05f, Factory.SoftGlowIntensity, 2.5f);
                        break;
                    case "pulse":
                        // pulse:element — breathe the glow of an element for the rest of the line
                        PulseElement(arg, Mathf.Max(1.5f, currentLineSeconds - 0.3f));
                        break;
                    case "tailghost": Main.Limit.SetTailGhostVisible(arg != "off"); break;
                    case "primes":
                        if (arg == "build") Annotations.BuildPrimes(Mathf.Max(3f, currentLineSeconds * 0.9f));
                        else if (arg == "arrows") Annotations.ShowPrimeArrows(true);
                        else if (arg == "noarrows") Annotations.ShowPrimeArrows(false);
                        else if (arg == "highlight") Annotations.HighlightPrimes(Mathf.Max(3f, currentLineSeconds * 0.9f));
                        else Annotations.ShowPrimes(arg == "on");
                        break;
                    case "axes": if (arg == "sprout") Annotations.SproutAxes(Mathf.Max(1.5f, currentLineSeconds * 0.6f)); break;
                    case "angles": Annotations.ShowAngles(arg != "off"); break;
                    case "dialstrip":
                        if (arg == "clear") Annotations.ClearDialStrip();
                        else Annotations.AddStripDial(int.Parse(arg, CultureInfo.InvariantCulture) - 1, 1.2f);
                        break;
                    case "stair": if (arg == "build") Main.BuildStaircase(Mathf.Max(2f, currentLineSeconds * 0.8f)); break;
                    case "rootlabel": Annotations.ShowRootFormula(arg); break;
                    case "sacks":
                        if (arg == "spiral") { Annotations.Sacks.gameObject.SetActive(true); Annotations.Sacks.StartCoroutine(Annotations.Sacks.MorphToSpiral(Mathf.Max(2.5f, currentLineSeconds * 0.8f))); }
                        else if (arg == "saddle") { Annotations.Sacks.gameObject.SetActive(true); Annotations.Sacks.SetFlat(false, Mathf.Max(2.5f, currentLineSeconds * 0.8f)); }
                        else if (arg == "flat") { Annotations.Sacks.gameObject.SetActive(true); Annotations.Sacks.SetFlat(true, Mathf.Max(2f, currentLineSeconds * 0.6f)); }
                        else if (arg == "lattice") { Annotations.Sacks.gameObject.SetActive(true); Annotations.Sacks.StartCoroutine(Annotations.Sacks.BuildLatticeRows(Mathf.Max(4f, currentLineSeconds * 0.9f))); }
                        else if (arg == "sievebegin")
                        {
                            // sacks:sievebegin[:seconds] — the grid breaks open onto the spiral, only the 2 stays lit
                            float seconds = parts.Length > 2 ? float.Parse(parts[2], CultureInfo.InvariantCulture) : Mathf.Max(3f, currentLineSeconds * 0.85f);
                            if (Storyboard) seconds *= StoryboardSecondsPerLine / Mathf.Max(currentLineSeconds, 0.1f);
                            Annotations.Sacks.BeginSieve(seconds);
                        }
                        else if (arg == "sieveto")
                        {
                            // sacks:sieveto:N[:seconds] — make every composite up to N as p × m (p its smallest prime), revealing the skipped primes
                            int upTo = int.Parse(parts[2], CultureInfo.InvariantCulture);
                            float seconds = parts.Length > 3 ? float.Parse(parts[3], CultureInfo.InvariantCulture) : Mathf.Max(2f, currentLineSeconds * 0.92f);
                            if (Storyboard) seconds *= StoryboardSecondsPerLine / Mathf.Max(currentLineSeconds, 0.1f);
                            Annotations.Sacks.QueueSieve(upTo, seconds);
                        }
                        else if (arg == "highlight") Annotations.Sacks.HighlightRoute(parts.Length > 2 ? parts[2].Trim().ToLowerInvariant() : "all");
                        else if (arg == "sieve")
                        {
                            float seconds = parts.Length > 2 ? float.Parse(parts[2], CultureInfo.InvariantCulture) : Mathf.Max(8f, currentLineSeconds * 2f);
                            if (Storyboard) seconds *= StoryboardSecondsPerLine / Mathf.Max(currentLineSeconds, 0.1f);
                            Annotations.Sacks.StartSieve(seconds);
                        }
                        else if (arg == "squares") Annotations.Sacks.PulseSquares(Mathf.Max(2f, currentLineSeconds * 0.8f));
                        else if (arg == "composites") Annotations.Sacks.PulseComposites(Mathf.Max(2f, currentLineSeconds * 0.8f));
                        else if (arg == "routes") Annotations.Sacks.ShowRoutes(true);
                        else if (arg == "noroutes") Annotations.Sacks.ShowRoutes(false);
                        else if (arg == "show") Annotations.Sacks.ShowFull();
                        else Annotations.Sacks.Hide();
                        break;
                    case "chainfeet": Annotations.ShowChainFeet(arg != "off"); break;
                    case "landing":
                        if (arg == "off") Annotations.ShowLanding(false);
                        else Annotations.ShowLanding(true, $"<color=#C8A0FF>t = {Main.CurrentT:0.00}</color>\nevery joint turned t·log(n/(n−1))\nand the chain closed on the centre");
                        break;
                    case "explore":
                        exploreAfterTour = arg != "off";
                        break;
                    case "equations":
                        // equations:on · equations:off · equations:squares|pronic|euler (highlight one route and its equation)
                        Captions.ShowEquations(arg);
                        if (arg != "off") Annotations.Sacks.HighlightRoute(arg == "on" ? "all" : arg);
                        break;
                    case "home":
                        // home:scene — the saddle with the zeta trail growing from its centre (content only, the camera is left alone)
                        Home.PrepareScene(true);
                        break;
                    case "hypothesis":
                        if (arg == "drop") Annotations.Sacks.DropZeros(Mathf.Max(4f, currentLineSeconds * 0.9f));
                        else if (arg == "zeta") Annotations.Sacks.ShowZetaOnly();
                        else if (arg == "sheet") { Annotations.Sacks.ShowZetaOnly(); Annotations.Sacks.ShowFringeSheet(60f, 62f, 13); Annotations.Sacks.ShowTrail(true); }
                        else if (arg == "full") Annotations.Sacks.StartFullMasterWave(parts.Length > 2 ? int.Parse(parts[2], CultureInfo.InvariantCulture) : Main.zetaZeroValues.Length, 160000, 1300f, Mathf.Max(6f, currentLineSeconds * 0.9f));
                        else if (arg == "notrail" || arg == "trail") Annotations.Sacks.SetGrowthTrailVisible(arg == "trail");
                        else if (arg == "grow")
                        {
                            // hypothesis:grow:TMAX:SECONDS — the trail grows from the saddle's centre to t = TMAX; every zero it
                            // passes sends its wave along the spiral at its height, and the wave sinks into the master wave
                            float tMax = parts.Length > 2 ? float.Parse(parts[2], CultureInfo.InvariantCulture) : 101.5f;
                            float seconds = parts.Length > 3 ? float.Parse(parts[3], CultureInfo.InvariantCulture) : Mathf.Max(6f, currentLineSeconds * 0.9f);
                            if (Storyboard) seconds *= StoryboardSecondsPerLine / Mathf.Max(currentLineSeconds, 0.1f);
                            Annotations.Sacks.ShowHypothesis(false); // the static trail and rings make way for the growing one
                            Annotations.Sacks.StartGrowth(SacksSpiral.GrowthMode.Waves, tMax, seconds, false);
                        }
                        else if (arg == "wavestack") Annotations.Sacks.StartWaveStack(Mathf.Max(6f, currentLineSeconds * 0.95f), parts.Length > 2 ? int.Parse(parts[2], CultureInfo.InvariantCulture) : 24);
                        else if (arg == "interference") Annotations.Sacks.StartInterference(Mathf.Max(4f, currentLineSeconds * 0.9f));
                        else if (arg == "nointerference") Annotations.Sacks.HideInterference();
                        else Annotations.Sacks.ShowHypothesis(arg != "off");
                        break;
                    case "logseq": Annotations.ShowLogSequence(arg != "off"); break;
                    case "squares": Annotations.ShowSquaresLabel(arg != "off"); break;
                    case "primepowers": Annotations.ShowPrimePowers(arg != "off"); break;
                    case "waveview": Annotations.SetWaveCount(int.Parse(arg, CultureInfo.InvariantCulture)); break;
                    case "labels": Main.ZeroLabelLimit = arg == "all" ? -1 : int.Parse(arg, CultureInfo.InvariantCulture); break;
                    case "ghost":
                        if (arg == "off") Annotations.SetGhostZero(false);
                        else if (arg == "on") Annotations.SetGhostZero(true);
                        else if (arg == "all") Annotations.SetGhostAllZeros(parts.Length > 2 ? double.Parse(parts[2], CultureInfo.InvariantCulture) : 0.9);
                        else if (arg == "sweep") Annotations.SweepGhostBeta(float.Parse(parts[2], CultureInfo.InvariantCulture), float.Parse(parts[3], CultureInfo.InvariantCulture), Mathf.Max(2f, currentLineSeconds * 0.9f), parts.Length > 4 && parts[4] == "pingpong");
                        else Annotations.SetGhostZero(true, double.Parse(arg, CultureInfo.InvariantCulture), parts.Length > 2 ? double.Parse(parts[2], CultureInfo.InvariantCulture) : 14.134725);
                        break;
                    case "show":
                    case "hide":
                        SetVisible(arg, verb == "show");
                        break;
                    default:
                        Debug.LogWarning($"Tour: unknown action '{raw}'");
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Tour: action '{raw}' failed: {e.Message}");
            }
        }
    }

    void SetVisible(string what, bool on)
    {
        switch (what)
        {
            case "tail": Main.Limit.SetTailVisible(on); break;
            case "spiral": Main.Limit.SetSpiralVisible(on); break;
            case "arrows": Main.Limit.SetArrowheadsVisible(on); break;
            case "lengths": Main.Limit.SetLengthLabelsVisible(on); break;
            case "tip": Annotations.ShowTip(on); break;
            case "center": Annotations.ShowCenter(on); break;
            case "numbers": Annotations.ShowNumberLine(on); break;
            case "polelabel": Annotations.ShowPoleLabel(on); break;
            case "zeno": Annotations.ShowZeno(on); break;
            case "grid": Main.SetGridVisible(on); break;
            case "trail": Main.SetZetaCurveVisible(on); break;
            case "components":
                if (Main.Limit.ShowSpirals != on)
                {
                    Main.Limit.ShowSpirals = on;
                    Main.Limit.Reset();
                }
                break;
            case "strip": Annotations.ShowStrip(on); break;
            case "axes": Annotations.ShowOutputAxes(on); Annotations.ShowStrip(on); break;
            case "imaxis": Annotations.ShowImAxis(on); break;
            case "outaxes": Annotations.ShowOutputAxes(on); break;
            case "zcurve": Annotations.ShowZCurve(on); break;
            case "band78": Annotations.ShowBand78(on); break;
            case "band18": Annotations.ShowBand18(on); break;
            case "classic": Annotations.ShowClassic(on); break;
            case "band1112": Annotations.ShowBand1112(on); break;
            case "gap": Annotations.ShowGap(on); break;
            case "cosine": Main.SetCosineVisible(on); break;
            case "prime": Main.SetPrimeCurveVisible(on); break;
            case "equations": Main.SetEquationsVisible(on); break;
            case "zeros": Main.SetZeroMarkersVisible(on); break;
            case "zetacurve": Main.SetZetaCurveVisible(on); break;
            default: Debug.LogWarning($"Tour: unknown element '{what}'"); break;
        }
    }
}
