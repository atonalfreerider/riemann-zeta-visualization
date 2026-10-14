using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The chapter scrubber: a slim bar along the bottom of the screen while a chapter plays. One segment per narration line,
/// sized by its length, a playhead, and previous line / pause / next line buttons. Clicking (or dragging and releasing)
/// on the bar jumps to that line: lines in between run quickly so everything they set up is in place. A screen overlay,
/// so it is never in recordings, and hidden while recording. Not used in VR (the dock has its own controls).
/// </summary>
public class TourScrubber : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    static readonly Color BarColor = new(.12f, .12f, .17f, .85f);
    static readonly Color DoneColor = new(.35f, .6f, .85f, .95f);
    static readonly Color PanelColor = new(.035f, .035f, .06f, .78f);

    TourDirector director;
    Canvas canvas;
    RectTransform panel, bar, playhead, hover;
    TextMeshProUGUI title, clock;
    Image pauseIcon;
    readonly List<Image> segments = new();
    readonly List<float> starts = new();
    float total;
    int builtFor = -1;
    bool dragging;
    float dragFraction;

    public static TourScrubber Create(TourDirector director)
    {
        GameObject go = new("TourScrubber");
        TourScrubber s = go.AddComponent<TourScrubber>();
        s.director = director;
        s.Build();
        return s;
    }

    void Build()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 990; // over the world and the launch card, under the side menu
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
        gameObject.AddComponent<GraphicRaycaster>();

        panel = Rect("Panel", transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(.5f, 0));
        panel.offsetMin = new Vector2(56, 8);
        panel.offsetMax = new Vector2(-16, 66);
        panel.gameObject.AddComponent<Image>().color = PanelColor;

        IconButton(VRIcons.Id.PrevLine, 22, () => SeekRelative(-1));
        pauseIcon = IconButton(VRIcons.Id.Pause, 58, () => director.TogglePause());
        IconButton(VRIcons.Id.NextLine, 94, () => SeekRelative(1));

        title = Text("Title", 18, new Vector2(122, 34), TextAlignmentOptions.BottomLeft);
        clock = Text("Clock", 18, new Vector2(-14, 34), TextAlignmentOptions.BottomRight, true);

        bar = Rect("Bar", panel, new Vector2(0, 0), new Vector2(1, 0), new Vector2(.5f, 0));
        bar.offsetMin = new Vector2(122, 10);
        bar.offsetMax = new Vector2(-14, 26);
        Image hit = bar.gameObject.AddComponent<Image>();
        hit.color = new Color(0, 0, 0, 0); // catches the pointer; the segments draw the bar
        TourScrubberHit relay = bar.gameObject.AddComponent<TourScrubberHit>();
        relay.Owner = this;

        playhead = Rect("Playhead", bar, new Vector2(0, -.4f), new Vector2(0, 1.4f), new Vector2(.5f, .5f));
        playhead.sizeDelta = new Vector2(4, 0);
        Image ph = playhead.gameObject.AddComponent<Image>();
        ph.color = Color.white;
        ph.raycastTarget = false;
        hover = Rect("Hover", bar, new Vector2(0, -.4f), new Vector2(0, 1.4f), new Vector2(.5f, .5f));
        hover.sizeDelta = new Vector2(2, 0);
        Image hv = hover.gameObject.AddComponent<Image>();
        hv.color = new Color(1f, .84f, 0f, .9f);
        hv.raycastTarget = false;
        hover.gameObject.SetActive(false);
        canvas.enabled = false;
    }

    static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        RectTransform r = new GameObject(name).AddComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.pivot = pivot;
        r.offsetMin = r.offsetMax = Vector2.zero;
        return r;
    }

    Image IconButton(VRIcons.Id id, float x, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform r = Rect(id.ToString(), panel, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(.5f, .5f));
        r.anchoredPosition = new Vector2(x, 0);
        r.sizeDelta = new Vector2(32, 32);
        Image bg = r.gameObject.AddComponent<Image>();
        bg.color = new Color(1, 1, 1, .001f);
        r.gameObject.AddComponent<Button>().onClick.AddListener(onClick);
        RectTransform ir = Rect("Icon", r, Vector2.zero, Vector2.one, new Vector2(.5f, .5f));
        Image icon = ir.gameObject.AddComponent<Image>();
        icon.sprite = VRIcons.Get(id);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        return icon;
    }

    TextMeshProUGUI Text(string name, float size, Vector2 position, TextAlignmentOptions alignment, bool right = false)
    {
        RectTransform r = Rect(name, panel, new Vector2(right ? 1 : 0, 0), new Vector2(right ? 1 : 0, 0), new Vector2(right ? 1 : 0, 0));
        r.anchoredPosition = position;
        r.sizeDelta = new Vector2(right ? 220 : 700, 26);
        TextMeshProUGUI t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = TMP_Settings.defaultFontAsset;
        t.fontSize = size;
        t.color = new Color(.85f, .85f, .9f);
        t.alignment = alignment;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Ellipsis;
        return t;
    }

    void Rebuild(int chapterIndex)
    {
        builtFor = chapterIndex;
        foreach (Image s in segments) Destroy(s.gameObject);
        segments.Clear();
        starts.Clear();
        TourChapter chapter = director.Chapters[chapterIndex];
        total = 0f;
        foreach (TourLine line in chapter.lines)
        {
            starts.Add(total);
            total += TourDirector.LineSeconds(line);
        }

        for (int i = 0; i < chapter.lines.Length; i++)
        {
            float a = starts[i] / total, b = (i + 1 < starts.Count ? starts[i + 1] : total) / total;
            RectTransform r = Rect("Line " + (i + 1), bar, new Vector2(a, 0), new Vector2(b, 1), new Vector2(0, .5f));
            r.offsetMin = new Vector2(1.5f, 0);
            r.offsetMax = new Vector2(-1.5f, 0);
            Image img = r.gameObject.AddComponent<Image>();
            img.color = BarColor;
            img.raycastTarget = false;
            segments.Add(img);
        }

        playhead.SetAsLastSibling();
        hover.SetAsLastSibling();
        title.text = $"{chapterIndex + 1}.  {chapter.title}";
    }

    void Update()
    {
        bool show = director.Running && !director.Recorder.IsOffscreen && !VRMode.Enabled && director.CurrentChapterIndex >= 0;
        canvas.enabled = show;
        if (!show) return;
        if (builtFor != director.CurrentChapterIndex) Rebuild(director.CurrentChapterIndex);
        int line = Mathf.Clamp(director.CurrentLineIndex, 0, segments.Count - 1);
        float within = director.LineDuration > 0f ? Mathf.Clamp01(director.LineElapsed / director.LineDuration) : 0f;
        float now = starts.Count > 0 ? starts[line] + within * TourDirector.LineSeconds(director.Chapters[builtFor].lines[line]) : 0f;
        float u = total > 0f ? Mathf.Clamp01(now / total) : 0f;
        playhead.anchorMin = new Vector2(u, -.4f);
        playhead.anchorMax = new Vector2(u, 1.4f);
        for (int i = 0; i < segments.Count; i++) segments[i].color = i < line ? DoneColor : i == line ? Color.Lerp(BarColor, DoneColor, .55f) : BarColor;
        clock.text = $"{Clock(now)} / {Clock(total)}" + (director.Seeking ? "   seeking…" : "");
        pauseIcon.sprite = VRIcons.Get(director.Paused ? VRIcons.Id.Play : VRIcons.Id.Pause);
        hover.gameObject.SetActive(dragging);
        if (dragging)
        {
            hover.anchorMin = new Vector2(dragFraction, -.4f);
            hover.anchorMax = new Vector2(dragFraction, 1.4f);
        }
    }

    static string Clock(float seconds) => $"{(int)seconds / 60}:{(int)seconds % 60:00}";

    void SeekRelative(int step)
    {
        int target = director.CurrentLineIndex + step;
        if (step < 0 && director.LineElapsed > 2.5f) target = director.CurrentLineIndex; // first press: back to the start of this line
        if (target >= 0 && target < segments.Count) director.SeekToLine(target);
        else if (step > 0) director.NextChapter();
    }

    float FractionAt(Vector2 screen)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(bar, screen, null, out Vector2 local);
        return Mathf.Clamp01((local.x - bar.rect.xMin) / bar.rect.width);
    }

    int LineAt(float fraction)
    {
        float t = fraction * total;
        int line = 0;
        for (int i = 0; i < starts.Count; i++) if (t >= starts[i]) line = i;
        return line;
    }

    public void OnPointerDown(PointerEventData e)
    {
        dragging = true;
        dragFraction = FractionAt(e.position);
    }

    public void OnDrag(PointerEventData e) => dragFraction = FractionAt(e.position);

    public void OnPointerUp(PointerEventData e)
    {
        if (!dragging) return;
        dragging = false;
        director.SeekToLine(LineAt(FractionAt(e.position)));
    }
}

/// <summary>Passes pointer events on the scrubber's bar to the scrubber.</summary>
public class TourScrubberHit : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public TourScrubber Owner;
    public void OnPointerDown(PointerEventData e) => Owner.OnPointerDown(e);
    public void OnDrag(PointerEventData e) => Owner.OnDrag(e);
    public void OnPointerUp(PointerEventData e) => Owner.OnPointerUp(e);
}
