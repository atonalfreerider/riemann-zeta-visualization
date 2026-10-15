using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// The desktop and phone interface: a panel that tucks into the left edge (the arrow at the middle of the edge, or M).
///   View      Saddle (the graph view: the growing trail over the saddle, orbit camera) · XY graph (the original zeta / staircase graph)
///   Layers    saddle or flat, master wave, zeta trail, chain, numbers, the three curves (k², k(k+1), k² + k + 41), trail height
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
    RectTransform panel, arrow;
    readonly System.Collections.Generic.List<(Image image, System.Func<bool> on)> toggles = new();
    TextMeshProUGUI heightLabel;
    bool open; // starts tucked away: the arrow on the left edge opens it
    float slide;
    Image landscapeButton, portraitButton, homeButton, graphButton;
    TextMeshProUGUI status;
    readonly List<Image> chapterRows = new();
    readonly List<Image> experimentRows = new();
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

        // the tab: a small arrow hanging off the panel's right edge at mid height, so it stays on screen when tucked away
        GameObject tabGo = new("Tab");
        tabGo.transform.SetParent(panel, false);
        RectTransform tab = tabGo.AddComponent<RectTransform>();
        tab.anchorMin = tab.anchorMax = new Vector2(1, .5f);
        tab.pivot = new Vector2(0, .5f);
        tab.anchoredPosition = Vector2.zero;
        tab.sizeDelta = new Vector2(30, 62);
        Image tabBg = tabGo.AddComponent<Image>();
        tabBg.color = PanelColor;
        tabGo.AddComponent<Button>().onClick.AddListener(() => open = !open);
        arrow = new GameObject("Arrow").AddComponent<RectTransform>();
        arrow.SetParent(tab, false);
        arrow.anchorMin = arrow.anchorMax = new Vector2(.5f, .5f);
        arrow.sizeDelta = new Vector2(16, 28);
        for (int i = 0; i < 2; i++)
        {
            // a chevron from two short bars: '>' when tucked away, turned to '<' when open
            RectTransform bar = new GameObject("Bar").AddComponent<RectTransform>();
            bar.SetParent(arrow, false);
            bar.anchorMin = bar.anchorMax = new Vector2(.5f, .5f);
            bar.sizeDelta = new Vector2(15, 3.5f);
            bar.anchoredPosition = new Vector2(0, i == 0 ? 4.6f : -4.6f);
            bar.localRotation = Quaternion.Euler(0, 0, i == 0 ? -45f : 45f);
            Image img = bar.gameObject.AddComponent<Image>();
            img.color = TextColor;
            img.raycastTarget = false;
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
        homeButton = Button(viewRow, "Saddle", () => { director.StopTour(); if (director.Explore.Active) director.Explore.Exit(); director.Home.Enter(); }, 1f, TextAlignmentOptions.Left, Icon(VRIcons.Id.Saddle));
        graphButton = Button(viewRow, "XY graph", ShowGraph, 1f, TextAlignmentOptions.Left, Icon(VRIcons.Id.Grid));

        Header(content, "LAYERS   saddle view");
        SacksSpiral sacks = director.Annotations.Sacks;
        RectTransform layerRow = Row(content);
        Toggle(layerRow, "Saddle", () => !sacks.Flat, () => sacks.SetFlat(!sacks.Flat, 2f), Icon(VRIcons.Id.Saddle));
        Toggle(layerRow, "Master wave", () => sacks.MasterWaveVisible, () => sacks.SetMasterWaveVisible(!sacks.MasterWaveVisible), Icon(VRIcons.Id.Waves));
        layerRow = Row(content);
        Toggle(layerRow, "Zeta trail", () => !sacks.GrowthTrailHidden, () => sacks.SetGrowthTrailVisible(sacks.GrowthTrailHidden), Icon(VRIcons.Id.Trail));
        Toggle(layerRow, "Chain", () => sacks.ChainVisible, () => sacks.SetChainVisible(!sacks.ChainVisible), Icon(VRIcons.Id.Chain));
        layerRow = Row(content);
        Toggle(layerRow, "Numbers", () => sacks.DotsVisible, () => sacks.SetDotsVisible(!sacks.DotsVisible), Icon(VRIcons.Id.Sacks));
        Toggle(layerRow, "n = k²", () => sacks.RouteVisible("squares"), () => sacks.SetRouteVisible("squares", !sacks.RouteVisible("squares")), Icon(VRIcons.Id.Equations), SacksSpiral.SquareColor);
        layerRow = Row(content);
        Toggle(layerRow, "n = k(k+1)", () => sacks.RouteVisible("pronic"), () => sacks.SetRouteVisible("pronic", !sacks.RouteVisible("pronic")), Icon(VRIcons.Id.Equations), SacksSpiral.PronicColor);
        Toggle(layerRow, "k² + k + 41", () => sacks.RouteVisible("euler"), () => sacks.SetRouteVisible("euler", !sacks.RouteVisible("euler")), Icon(VRIcons.Id.Equations), SacksSpiral.EulerColor);
        layerRow = Row(content);
        Toggle(layerRow, "Product curves (Sacks)", () => sacks.FamilyVisible, () => sacks.SetFamilyVisible(!sacks.FamilyVisible), Icon(VRIcons.Id.Equations), SacksSpiral.FamilyColors[0]);
        layerRow = Row(content);
        // the trail's length: how many zeros it climbs past (each click × 1.5)
        Button(layerRow, "", () => sacks.SetGrowthZeroTarget(Mathf.RoundToInt(sacks.GrowthZeroTarget / 1.5f)), .55f, TextAlignmentOptions.Center, Icon(VRIcons.Id.Down));
        heightLabel = Button(layerRow, "", () => sacks.SetGrowthZeroTarget(649), 2f, TextAlignmentOptions.Center, Icon(VRIcons.Id.Zeros)).GetComponentInChildren<TextMeshProUGUI>();
        Button(layerRow, "", () => sacks.SetGrowthZeroTarget(Mathf.RoundToInt(sacks.GrowthZeroTarget * 1.5f)), .55f, TextAlignmentOptions.Center, Icon(VRIcons.Id.Up));
        Button(layerRow, "Reset", () => { if (director.Orbit != null) director.Orbit.ResetView(); }, 1.3f, TextAlignmentOptions.Left, Icon(VRIcons.Id.Reset));

        Header(content, "FORMAT");
        RectTransform formatRow = Row(content);
        landscapeButton = Button(formatRow, "16:9 wide", () => director.SetFormat(false), 1f, TextAlignmentOptions.Left, FrameIcon(true));
        portraitButton = Button(formatRow, "9:16 tall", () => director.SetFormat(true), 1f, TextAlignmentOptions.Left, FrameIcon(false));

        Header(content, "TOUR");
        RectTransform tourRow = Row(content);
        Button(tourRow, "Play whole tour", () => { director.GoToChapter(0, true); open = false; }, 2f, TextAlignmentOptions.Left, Icon(VRIcons.Id.Play));
        Button(tourRow, "Stop", () => director.StopTour(), 1f, TextAlignmentOptions.Left, Icon(VRIcons.Id.Pause));

        Header(content, "CHAPTERS   tap to play  ·  red dot records one");
        TourChapter[] chapters = director.Chapters;
        for (int i = 0; i < chapters.Length; i++)
        {
            int index = i;
            RectTransform row = Row(content);
            Image play = Button(row, $"{i + 1}.  {chapters[i].title}", () => { director.GoToChapter(index, true); open = false; }, 5.2f, TextAlignmentOptions.Left, Icon(VRApp.ChapterIconFor(i)));
            chapterRows.Add(play);
            Button(row, "", () => { director.RecordTour(director.Chapters[index].id, false, director.Portrait); open = false; }, 1f, TextAlignmentOptions.Center, VRIcons.Circle(), RecordColor);
        }

        Header(content, "EXPERIMENTS   thought experiments on the saddle");
        TourChapter[] experiments = director.ExperimentChapters;
        for (int i = 0; i < experiments.Length; i++)
        {
            int index = i;
            RectTransform row = Row(content);
            experimentRows.Add(Button(row, experiments[i].title, () => { director.PlayExperiment(index); open = false; }, 5.2f, TextAlignmentOptions.Left, Icon(VRIcons.Id.Play)));
        }

        Label(content, "Scripted in Narration/experiments.json; captions only, no narration recorded. Findings in docs/experiments.", 19, FontStyles.Normal, new Color(.45f, .45f, .52f), 50).textWrappingMode = TextWrappingModes.Normal;

        Header(content, "RECORD");
        RectTransform recordRow = Row(content);
        Button(recordRow, "Record whole tour", () => { director.RecordTour("", true, director.Portrait); open = false; }, 2f, TextAlignmentOptions.Left, VRIcons.Circle(), RecordColor);
        stopRecordingButton = Button(recordRow, "Stop", () => { if (director.Recorder.IsRecording) director.StopRecording(); }, 1f, TextAlignmentOptions.Left, Icon(VRIcons.Id.Pause)).GetComponent<Button>();
        status = Label(content, "", 22, FontStyles.Normal, DimText, 64);
        status.textWrappingMode = TextWrappingModes.Normal;
        Label(content, "Saddle view:  drag or W A S D / arrows orbit and change altitude · wheel or Q E zoom · Shift + drag or Shift + W A S D move the centre", 19, FontStyles.Normal, new Color(.45f, .45f, .52f), 74).textWrappingMode = TextWrappingModes.Normal;
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

    Canvas menuCanvas;

    void Update()
    {
        menuCanvas ??= GetComponent<Canvas>();
        menuCanvas.enabled = !director.Recorder.IsOffscreen; // recording: nothing over the frame but the frame
        Keyboard k = Keyboard.current;
        if (k != null && (k.mKey.wasPressedThisFrame || k.tabKey.wasPressedThisFrame)) open = !open;

        slide = Mathf.MoveTowards(slide, open ? 1f : 0f, Time.unscaledDeltaTime * 5f);
        float eased = Mathf.SmoothStep(0f, 1f, slide);
        panel.anchoredPosition = new Vector2(-PanelWidth * (1f - eased), 0f);
        arrow.localRotation = Quaternion.Euler(0, 0, 180f * eased);
        foreach ((Image image, System.Func<bool> on) in toggles) image.color = on() ? ButtonActive : ButtonColor;
        string height = $"{director.Annotations.Sacks.GrowthZeroTarget:N0} zeros";
        if (heightLabel.text != height) heightLabel.text = height;

        landscapeButton.color = director.Portrait ? ButtonColor : ButtonActive;
        portraitButton.color = director.Portrait ? ButtonActive : ButtonColor;
        homeButton.color = director.Home.Active ? ButtonActive : ButtonColor;
        graphButton.color = ButtonColor;
        for (int i = 0; i < chapterRows.Count; i++)
        {
            chapterRows[i].color = director.Running && !director.PlayingExperiment && director.CurrentChapterIndex == i ? ButtonActive : ButtonColor;
        }

        for (int i = 0; i < experimentRows.Count; i++)
        {
            experimentRows[i].color = director.Running && director.PlayingExperiment && director.CurrentChapterIndex == i ? ButtonActive : ButtonColor;
        }

        FrameRecorder r = director.Recorder;
        stopRecordingButton.interactable = r.IsRecording;
        string text = r.IsRecording
            ? $"<color=#FF5A5A>● recording</color>  {r.Width}×{r.Height}  ·  {r.FrameIndex / Mathf.Max(1, r.Fps) / 60:00}:{r.FrameIndex / Mathf.Max(1, r.Fps) % 60:00}\n{r.OutputDirectory}"
            : $"records {(director.Portrait ? "1080×1920 (9:16)" : "1920×1080 (16:9)")} to the Recordings folder";
        if (status.text != text) status.text = text;
    }

    // ---- small builders ----------------------------------------------------------------------------

    Image Toggle(Transform parent, string text, System.Func<bool> on, UnityEngine.Events.UnityAction onClick, Sprite icon = null, Color? iconColor = null)
    {
        Image image = Button(parent, text, onClick, 1f, TextAlignmentOptions.Left, icon, iconColor);
        toggles.Add((image, on));
        return image;
    }

    /// <summary>The same icons as the VR menus.</summary>
    static Sprite Icon(VRIcons.Id id) => VRIcons.Get(id);

    static Sprite wideFrame, tallFrame;

    /// <summary>A frame outline in the shape of the format (16:9 or 9:16).</summary>
    static Sprite FrameIcon(bool wide)
    {
        ref Sprite cached = ref wide ? ref wideFrame : ref tallFrame;
        if (cached != null) return cached;
        const int n = 64;
        Texture2D tex = new(n, n, TextureFormat.RGBA32, false);
        float hw = wide ? 30f : 17f, hh = wide ? 17f : 30f;
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float dx = Mathf.Abs(x + .5f - n * .5f), dy = Mathf.Abs(y + .5f - n * .5f);
            bool inside = dx <= hw && dy <= hh;
            bool border = inside && (dx >= hw - 4f || dy >= hh - 4f);
            tex.SetPixel(x, y, border ? Color.white : Color.clear);
        }

        tex.Apply();
        cached = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(.5f, .5f), 100f);
        return cached;
    }

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
        t.enableAutoSizing = true; // long labels shrink to fit instead of being cut off
        t.fontSizeMax = size;
        t.fontSizeMin = Mathf.Min(size, 13f);
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

    static Image Button(Transform parent, string text, UnityEngine.Events.UnityAction onClick, float weight = 1f, TextAlignmentOptions alignment = TextAlignmentOptions.Center, Sprite icon = null, Color? iconColor = null)
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
        lr.offsetMin = new Vector2(icon != null && !string.IsNullOrEmpty(text) ? 52 : 14, 0);
        lr.offsetMax = new Vector2(-12, 0);
        if (icon != null)
        {
            RectTransform ir = new GameObject("Icon").AddComponent<RectTransform>();
            ir.SetParent(go.transform, false);
            bool alone = string.IsNullOrEmpty(text);
            ir.anchorMin = ir.anchorMax = new Vector2(alone ? .5f : 0f, .5f);
            ir.pivot = new Vector2(alone ? .5f : 0f, .5f);
            ir.anchoredPosition = new Vector2(alone ? 0f : 14f, 0f);
            ir.sizeDelta = new Vector2(30, 30);
            Image img = ir.gameObject.AddComponent<Image>();
            img.sprite = icon;
            img.color = iconColor ?? TextColor;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }

        TextMeshProUGUI t = labelGo.AddComponent<TextMeshProUGUI>();
        t.font = TMP_Settings.defaultFontAsset;
        t.text = text;
        t.fontSize = 24;
        t.enableAutoSizing = true; // long labels shrink to fit the button instead of being cut off
        t.fontSizeMax = 24;
        t.fontSizeMin = 13;
        t.color = TextColor;
        t.alignment = alignment;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Ellipsis;
        return bg;
    }
}
