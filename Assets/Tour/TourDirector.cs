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
/// Keys:  T start/stop tour · N skip to next line · F9 start/stop recording · P toggle portrait
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
    public bool Portrait = true;
    public string StartChapter = "";
    public float ChapterTitleSeconds = 3.2f;

    public bool Running { get; private set; }
    public Main Main { get; private set; }
    public Annotations Annotations { get; private set; }
    public CameraRig Rig { get; private set; }
    public CaptionDisplay Captions { get; private set; }
    public FrameRecorder Recorder { get; private set; }

    /// <summary>(line id, tour clock seconds, recorder frame) for every narration clip started — used to mux audio.</summary>
    public readonly List<(string id, float time, int frame)> AudioTimeline = new();

    TourScriptData script;
    AudioSource audioSource;
    Coroutine tourRoutine;
    float tourStartTime;
    bool skipRequested;

    void Awake()
    {
        Instance = this;
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
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        script = TourScriptLoader.Load();
        Captions.SetHint("T  start tour     Space  play / pause     R  component spirals     F9  record     P  portrait");

        if (AutoStart)
        {
            StartTour();
        }
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

        if (keyboard.pKey.wasPressedThisFrame)
        {
            Portrait = !Portrait;
            Rig.Portrait = Portrait;
        }
    }

    // ---- control -----------------------------------------------------------------------------

    public void StartTour()
    {
        if (Running) return;
        Running = true;
        skipRequested = false;
        AudioTimeline.Clear();
        tourStartTime = Time.time;
        Captions.SetHint("");

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

        tourRoutine = StartCoroutine(RunTour());
    }

    public void StopTour()
    {
        if (!Running) return;
        Running = false;
        if (tourRoutine != null) StopCoroutine(tourRoutine);
        tourRoutine = null;
        audioSource.Stop();
        Captions.Clear();
        Captions.SetHint("T  start tour     Space  play / pause     R  component spirals     F9  record     P  portrait");
        Rig.Release();
        if (Recorder.IsRecording) StopRecording();
    }

    float currentLineSeconds = 3f;

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
                Annotations.ShowIncomingWave(n, amplitude, Main.CosineScale * 0.7f);
                yield return new WaitForSeconds(perStep * 0.55f);
                Annotations.HideIncomingWave();
                Main.SetCosineZeroLimit(n + 1);
                yield return new WaitForSeconds(perStep * 0.45f);
            }
            else
            {
                Main.SetCosineZeroLimit(n + 1);
                yield return new WaitForSeconds(perStep);
            }
        }

        Annotations.HideIncomingWave();
        Main.SetCosineZeroLimit(to < 0 ? -1 : target);
    }

    IEnumerator BuildTerms(int from, int to)
    {
        float seconds = Mathf.Max(0.5f, currentLineSeconds * 0.85f);
        float start = Time.time;
        int last = -1;
        while (Time.time - start < seconds)
        {
            int n = Mathf.RoundToInt(Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, (Time.time - start) / seconds)));
            if (n != last)
            {
                last = n;
                Main.Limit.TermOverride = n;
                Main.Limit.RenderLimitCurve(Main.CurrentT, null, null);
            }

            yield return null;
        }

        Main.Limit.TermOverride = to;
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

        Annotations.HideAll();
        Main.SetSigma(.5d);
        Main.ResetAnimation();
        Main.Speed = 1.8d;
        Main.ZeroLabelLimit = 6;
        Main.CosineScale = 11f;
        Main.SetCosineZeroLimit(-1);
        Main.Limit.TermOverride = 0;
        Main.Limit.SetTailVisible(true);
        Main.Limit.SetArrowheadsVisible(true);
        Main.Limit.SetLengthLabelsVisible(false);
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

            Captions.SetChapterIndex(c + 1, total, chapter.title);
            Captions.ShowChapterTitle(chapter.title, chapter.subtitle, ChapterTitleSeconds);

            foreach (TourLine line in chapter.lines)
            {
                yield return PlayLine(line);
            }
        }

        Captions.Clear();
        yield return new WaitForSeconds(1.5f);
        Running = false;
        Rig.Release();
        // restore the free-roam visualization
        Main.SetPrimeCurveVisible(true);
        Main.SetCosineVisible(true);
        Main.SetEquationsVisible(true);
        Main.SetZetaCurveVisible(true);
        Main.SetGridVisible(true);
        Main.Limit.TermOverride = 0;
        Main.ZeroLabelLimit = -1;
        Main.CosineScale = -1f;
        Main.SetCosineZeroLimit(-1);
        Annotations.HideAll();
        if (Recorder.IsRecording) StopRecording();
        else if (Recorder.IsOffscreen) Recorder.EndOffscreen();
        Main.SpeedMultiplier = 1d;
        Captions.SetHint("T  start tour     Space  play / pause     R  component spirals     F9  record     P  portrait");

#if UNITY_EDITOR
        if (UnityEditor.EditorPrefs.GetBool("Zeta.BatchQuitAfterPlay", false))
        {
            Debug.Log("Tour finished in batch mode; leaving play mode.");
            UnityEditor.EditorApplication.ExitPlaymode();
        }
#endif
    }

    IEnumerator PlayLine(TourLine line)
    {
        skipRequested = false;
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
        while (Time.time - start < duration + Mathf.Max(0f, line.pause))
        {
            if (skipRequested)
            {
                audioSource.Stop();
                break;
            }

            Captions.SetTime(Time.time - start);
            yield return null;
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
                        if (arg == "plain") Annotations.ShowDemoWavePlain(Main.CosineScale);
                        else if (arg == "morph") Annotations.MorphDemoWave(Mathf.Max(1f, currentLineSeconds * 0.7f), Main.CosineScale);
                        else Annotations.HideDemoWave();
                        break;
                    case "labels": Main.ZeroLabelLimit = arg == "all" ? -1 : int.Parse(arg, CultureInfo.InvariantCulture); break;
                    case "ghost":
                        if (arg == "off") Annotations.SetGhostZero(false);
                        else if (arg == "on") Annotations.SetGhostZero(true);
                        else if (arg == "all") Annotations.SetGhostAllZeros(parts.Length > 2 ? double.Parse(parts[2], CultureInfo.InvariantCulture) : 0.9);
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
