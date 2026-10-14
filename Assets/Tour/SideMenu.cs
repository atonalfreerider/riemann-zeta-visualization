using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// The desktop and phone interface: a panel that tucks into the left edge (tab or M to open and close).
///   View      Home (the growing saddle) · Explore (fly around the final picture) · Graph (the original zeta / staircase graph)
///   Format    16:9 or 9:16: the tour's framing, caption layout and recording size
///   Tour      play the whole tour, or start at any chapter; stop
///   Record    record the whole tour or a single chapter (the ● on its row) in the chosen format
/// It is a screen overlay, so it never appears in recordings. Not used in VR (the VR layer has its own menus).
/// </summary>
public class SideMenu : MonoBehaviour
{
    const float PanelWidth = 470f;
    const float RowHeight = 58f;
    static readonly Color PanelColor = new(0.035f, 0.035f, 0.06f, 0.9f);
    static readonly Color ButtonColor = new(0.13f, 0.13f, 0.19f, 1f);
    static readonly Color ButtonActive = new(0.2f, 0.36f, 0.5f, 1f);
    static readonly Color RecordColor = new(0.85f, 0.18f, 0.2f, 1f);
    static readonly Color TextColor = new(0.92f, 0.92f, 0.96f);
    static readonly Color DimText = new(0.6f, 0.6f, 0.68f);

    TourDirector director;
    RectTransform panel;
    bool open = true;
    float slide = 1f;
    Image landscapeButton, portraitButton, homeButton, exploreButton, graphButton;
    TextMeshProUGUI status;
    readonly List<Image> chapterRows = new();
    Button stopRecordingButton;

    public static SideMenu Create(TourDirector director)
    {
        GameObject go = new("SideMenu");
        SideMenu menu = go.AddComponent<SideMenu>();
        menu.director = director;
        menu.Build();
        return menu;
    }

    public bool Open
    {
        get => open;
        set => open = value;
    }

    void Build()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink; // the short side of the screen sets the size
        gameObject.AddComponent<GraphicRaycaster>();

        // the panel, stretched down the left edge
        GameObject panelGo = new("Panel");
        panelGo.transform.SetParent(transform, false);
        panel = panelGo.AddComponent<RectTransform>();
        panel.anchorMin = new Vector2(0, 0);
        panel.anchorMax = new Vector2(0, 1);
        panel.pivot = new Vector2(0, 0.5f);
        panel.sizeDelta = new Vector2(PanelWidth, 0);
        panelGo.AddComponent<Image>().color = PanelColor;

        // the tab hangs off the panel's right edge, so it stays on screen when the panel is tucked away
        GameObject tabGo = new("Tab");
        tabGo.transform.SetParent(panel, false);
        RectTransform tab = tabGo.AddComponent<RectTransform>();
        tab.anchorMin = tab.anchorMax = new Vector2(1, 1);
        tab.pivot = new Vector2(0, 1);
        tab.anchoredPosition = new Vector2(0, -24);
        tab.sizeDelta = new Vector2(66, 66);
        Image tabBg = tabGo.AddComponent<Image>();
        tabBg.color = PanelColor;
        tabGo.AddComponent<Button>().onClick.AddListener(() => open = !open);
        for (int i = 0; i < 3; i++)
        {
            RectTransform bar = new GameObject("Bar").AddComponent<RectTransform>();
            bar.SetParent(tab, false);
            bar.anchorMin = bar.anchorMax = new Vector2(.5f, .5f);
            bar.sizeDelta = new Vector2(32, 4);
            bar.anchoredPosition = new Vector2(0, 11 - i * 11);
            bar.gameObject.AddComponent<Image>().color = TextColor;
        }

        // scrolling content
        GameObject viewportGo = new("Viewport");
        viewportGo.transform.SetParent(panel, false);
        RectTransform viewport = viewportGo.AddComponent<RectTransform>();
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(0, 12);
        viewport.offsetMax = new Vector2(0, -12);
        viewportGo.AddComponent<RectMask2D>();
        viewportGo.AddComponent<Image>().color = new Color(0, 0, 0, 0); // catches drags for scrolling

        GameObject contentGo = new("Content");
        contentGo.transform.SetParent(viewport, false);
        RectTransform content = contentGo.AddComponent<RectTransform>();
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(.5f, 1);
        content.sizeDelta = Vector2.zero; // exactly the viewport's width
        VerticalLayoutGroup layout = contentGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(26, 26, 18, 26);
        layout.spacing = 10;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = panelGo.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        Label(content, "Zeta Viewer", 40, FontStyles.Bold, TextColor, 52);
        Label(content, "Primes, the Sacks spiral and the zeta zeros", 22, FontStyles.Normal, DimText, 30);

        Header(content, "VIEW");
        RectTransform viewRow = Row(content);
        homeButton = Button(viewRow, "Home", () => { director.StopTour(); if (director.Explore.Active) director.Explore.Exit(); director.Home.Enter(); });
        exploreButton = Button(viewRow, "Explore", () => { director.StopTour(); director.Explore.Enter(); open = false; });
        graphButton = Button(viewRow, "Graph", ShowGraph);

        Header(content, "FORMAT");
        RectTransform formatRow = Row(content);
        landscapeButton = Button(formatRow, "16:9 wide", () => director.SetFormat(false));
        portraitButton = Button(formatRow, "9:16 tall", () => director.SetFormat(true));

        Header(content, "TOUR");
        RectTransform tourRow = Row(content);
        Button(tourRow, "Play whole tour", () => { director.GoToChapter(0, true); open = false; }, 2f);
        Button(tourRow, "Stop", () => director.StopTour());

        Header(content, "CHAPTERS   tap to play  ·  ● to record one");
        TourChapter[] chapters = director.Chapters;
        for (int i = 0; i < chapters.Length; i++)
        {
            int index = i;
            RectTransform row = Row(content);
            Image play = Button(row, $"{i + 1}.  {chapters[i].title}", () => { director.GoToChapter(index, true); open = false; }, 5.2f, TextAlignmentOptions.Left);
            chapterRows.Add(play);
            Image rec = Button(row, "●", () => { director.RecordTour(director.Chapters[index].id, false, director.Portrait); open = false; }, 1f);
            rec.GetComponentInChildren<TextMeshProUGUI>().color = RecordColor;
        }

        Header(content, "RECORD");
        RectTransform recordRow = Row(content);
        Image recordAll = Button(recordRow, "●  Record whole tour", () => { director.RecordTour("", true, director.Portrait); open = false; }, 2f);
        recordAll.GetComponentInChildren<TextMeshProUGUI>().color = new Color(1f, .55f, .55f);
        stopRecordingButton = Button(recordRow, "Stop", () => { if (director.Recorder.IsRecording) director.StopRecording(); }).GetComponent<Button>();
        status = Label(content, "", 22, FontStyles.Normal, DimText, 64);
        status.textWrappingMode = TextWrappingModes.Normal;
        Label(content, "Keys:  M menu   T tour   N next line   F9 record   P wide / tall   F frame rate", 19, FontStyles.Normal, new Color(.45f, .45f, .52f), 50).textWrappingMode = TextWrappingModes.Normal;

        director.ChapterStarted += (_, _) => open = false;
    }

    void ShowGraph()
    {
        director.StopTour();
        if (director.Explore.Active) director.Explore.Exit();
        director.Home.Exit();
        director.RestoreFreeRoam();
        director.Rig.Release();
        Camera cam = Camera.main;
        if (cam != null)
        {
            // the original free-roam view of the zeta trail, zeros, chorus and staircase
            cam.transform.position = new Vector3(26f, 30f, -92f);
            cam.transform.rotation = Quaternion.LookRotation(new Vector3(0f, -2f, 92f), Vector3.up);
            cam.fieldOfView = 60f;
        }

        director.Main.Play();
        open = false;
    }

    void Update()
    {
        Keyboard k = Keyboard.current;
        if (k != null && (k.mKey.wasPressedThisFrame || k.tabKey.wasPressedThisFrame)) open = !open;

        slide = Mathf.MoveTowards(slide, open ? 1f : 0f, Time.unscaledDeltaTime * 5f);
        float eased = Mathf.SmoothStep(0f, 1f, slide);
        panel.anchoredPosition = new Vector2(-PanelWidth * (1f - eased), 0f);

        landscapeButton.color = director.Portrait ? ButtonColor : ButtonActive;
        portraitButton.color = director.Portrait ? ButtonActive : ButtonColor;
        homeButton.color = director.Home.Active ? ButtonActive : ButtonColor;
        exploreButton.color = director.Explore.Active ? ButtonActive : ButtonColor;
        graphButton.color = ButtonColor;
        for (int i = 0; i < chapterRows.Count; i++)
        {
            chapterRows[i].color = director.Running && director.CurrentChapterIndex == i ? ButtonActive : ButtonColor;
        }

        FrameRecorder r = director.Recorder;
        stopRecordingButton.interactable = r.IsRecording;
        string text = r.IsRecording
            ? $"<color=#FF5A5A>● recording</color>  {r.Width}×{r.Height}  ·  {r.FrameIndex / Mathf.Max(1, r.Fps) / 60:00}:{r.FrameIndex / Mathf.Max(1, r.Fps) % 60:00}\n{r.OutputDirectory}"
            : $"records {(director.Portrait ? "1080×1920 (9:16)" : "1920×1080 (16:9)")} to the Recordings folder";
        if (status.text != text) status.text = text;
    }

    // ---- small builders ----------------------------------------------------------------------------

    static TextMeshProUGUI Label(Transform parent, string text, float size, FontStyles style, Color color, float height)
    {
        GameObject go = new("Label");
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.font = TMP_Settings.defaultFontAsset;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.raycastTarget = false;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Ellipsis;
        go.AddComponent<LayoutElement>().preferredHeight = height;
        return t;
    }

    static void Header(Transform parent, string text)
    {
        TextMeshProUGUI t = Label(parent, text, 19, FontStyles.Bold, new Color(.55f, .75f, .95f), 40);
        t.alignment = TextAlignmentOptions.BottomLeft;
        t.characterSpacing = 4;
    }

    static RectTransform Row(Transform parent)
    {
        GameObject go = new("Row");
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        HorizontalLayoutGroup h = go.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 8;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = true;
        h.childForceExpandHeight = true;
        go.AddComponent<LayoutElement>().preferredHeight = RowHeight;
        return rect;
    }

    static Image Button(Transform parent, string text, UnityEngine.Events.UnityAction onClick, float weight = 1f, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        GameObject go = new("Button " + text);
        go.transform.SetParent(parent, false);
        Image bg = go.AddComponent<Image>();
        bg.color = ButtonColor;
        Button b = go.AddComponent<Button>();
        ColorBlock colors = b.colors;
        colors.highlightedColor = new Color(1.25f, 1.25f, 1.35f);
        colors.pressedColor = new Color(.8f, .8f, .9f);
        b.colors = colors;
        b.onClick.AddListener(onClick);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = weight;
        le.minWidth = 0;

        GameObject labelGo = new("Text");
        labelGo.transform.SetParent(go.transform, false);
        RectTransform lr = labelGo.AddComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = new Vector2(16, 0);
        lr.offsetMax = new Vector2(-16, 0);
        TextMeshProUGUI t = labelGo.AddComponent<TextMeshProUGUI>();
        t.font = TMP_Settings.defaultFontAsset;
        t.text = text;
        t.fontSize = 24;
        t.color = TextColor;
        t.alignment = alignment;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Ellipsis;
        return bg;
    }
}
