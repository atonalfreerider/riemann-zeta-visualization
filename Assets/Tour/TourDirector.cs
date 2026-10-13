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

        if (RecordOnStart && !Recorder.IsRecording)
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

    /// <summary>Jump to the next narration line (same as pressing N).</summary>
    public void SkipCurrentLine()
    {
        if (Running) skipRequested = true;
    }

    public void StartRecording()
    {
        string dir = System.IO.Path.Combine(Application.dataPath, "..", "Recordings", DateTime.Now.ToString("yyyyMMdd-HHmmss"));
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
        Main.ResetAnimation();
        Main.Speed = 1.8d;
        Main.SetPrimeCurveVisible(true);
        Main.SetCosineVisible(true);
        Main.Limit.SetTailVisible(true);

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
        if (Recorder.IsRecording) StopRecording();
        Captions.SetHint("T  start tour     Space  play / pause     R  component spirals     F9  record     P  portrait");
    }

    IEnumerator PlayLine(TourLine line)
    {
        skipRequested = false;
        Rig.SetCue(line.camera);
        ApplyActions(line.actions);

        LineTiming timing = TourScriptLoader.LoadTiming(line.id);
        AudioClip clip = TourScriptLoader.LoadAudio(line.id);
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
                    case "ghost":
                        if (arg == "off") Annotations.SetGhostZero(false);
                        else if (arg == "on") Annotations.SetGhostZero(true);
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
