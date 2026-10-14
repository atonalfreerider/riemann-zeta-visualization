using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space captions for the tour, laid out for a 1080×1920 portrait frame
/// (scales to any aspect). The spoken word is highlighted yellow, karaoke style.
/// </summary>
public class CaptionDisplay : MonoBehaviour
{
    int maxPageCharacters = 58;
    RectTransform titleRect, backdropRect, captionRect, footnoteRect;

    Canvas canvas;
    TextMeshProUGUI chapterTitle;
    TextMeshProUGUI chapterSubtitle;
    TextMeshProUGUI chapterIndex;
    TextMeshProUGUI caption;
    TextMeshProUGUI footnote;
    TextMeshProUGUI hint;
    Image captionBackdrop;
    CanvasGroup titleGroup;

    LineTiming current;
    RectTransform card;
    CanvasGroup cardGroup;
    Image cardMedallion;
    RawImage cardPortrait;
    TextMeshProUGUI cardGlyph, cardName, cardYears, cardRole;
    Coroutine cardRoutine;
    Image cardRing;

    struct Character { public string Name, Years, Role, Glyph; public Color Accent; }
    static readonly Dictionary<string, Character> Characters = new()
    {
        ["zeno"] = new Character { Name = "Zeno of Elea", Years = "c. 490 – 430 BC", Role = "The paradox of endless halving: infinitely many steps that never pass a wall", Glyph = "Z", Accent = new Color(.55f, .85f, 1f) },
        ["euler"] = new Character { Name = "Leonhard Euler", Years = "1707 – 1783", Role = "Tamed the endless sum; with Maclaurin, the formula that turns the missing tail into one exact arrow", Glyph = "E", Accent = new Color(1f, .7f, .35f) },
        ["riemann"] = new Character { Name = "Bernhard Riemann", Years = "1826 – 1866", Role = "Turned the dial into the complex plane, found the collisions, wrote the staircase recipe, and made the hypothesis", Glyph = "R", Accent = new Color(1f, .85f, .3f) },
        ["hardy"] = new Character { Name = "G. H. Hardy", Years = "1877 – 1947", Role = "Proved in 1914 that infinitely many collisions sit exactly on the one-half line", Glyph = "H", Accent = new Color(.6f, 1f, .7f) },
        ["machine"] = new Character { Name = "OpenAI's internal model", Years = "October 2026", Role = "AI-generated, Lean-checked proof of the quasi-Riemann hypothesis: no collision past seven eighths", Glyph = "AI", Accent = new Color(.5f, .9f, 1f) },
    };
    readonly List<(int start, int end)> pages = new();
    string highlightHex;
    string dimHex;
    Coroutine titleRoutine;

    public static CaptionDisplay Create(Camera cam)
    {
        GameObject go = new("Captions");
        CaptionDisplay display = go.AddComponent<CaptionDisplay>();
        display.Build(cam);
        return display;
    }

    void Build(Camera cam)
    {
        highlightHex = ColorUtility.ToHtmlStringRGB(Factory.Palette.CaptionHighlight);
        dimHex = ColorUtility.ToHtmlStringRGB(new Color(.78f, .78f, .8f));

        canvas = gameObject.AddComponent<Canvas>();
        if (VRMode.Enabled)
        {
            // a headset has no screen to overlay: hang the captions on the lazily gaze-following HUD anchor,
            // 1080 x 1080 units at about 1.2 mm per unit, drawn over the model
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 8; // after the model's lines, before the menus
            transform.SetParent(VRMode.Rig.Hud, false);
            RectTransform rect = GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1080, 1080);
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * 0.0011f;
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 0.5f;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
        }

        // chapter title card
        GameObject titleCard = new("TitleCard");
        titleCard.transform.SetParent(transform, false);
        titleRect = titleCard.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.78f);
        titleRect.anchorMax = new Vector2(0.5f, 0.78f);
        titleRect.sizeDelta = new Vector2(960, 400);
        titleGroup = titleCard.AddComponent<CanvasGroup>();
        titleGroup.alpha = 0f;

        chapterTitle = MakeText(titleCard.transform, "ChapterTitle", 84, FontStyles.Bold, TextAlignmentOptions.Center);
        chapterTitle.rectTransform.anchoredPosition = new Vector2(0, 40);
        chapterTitle.rectTransform.sizeDelta = new Vector2(960, 240);
        chapterSubtitle = MakeText(titleCard.transform, "ChapterSubtitle", 42, FontStyles.Normal, TextAlignmentOptions.Center);
        chapterSubtitle.rectTransform.anchoredPosition = new Vector2(0, -110);
        chapterSubtitle.rectTransform.sizeDelta = new Vector2(960, 120);
        chapterSubtitle.color = new Color(.85f, .85f, .9f);

        chapterIndex = MakeText(transform, "ChapterIndex", 30, FontStyles.Normal, TextAlignmentOptions.Left);
        chapterIndex.rectTransform.anchorMin = new Vector2(0, 1);
        chapterIndex.rectTransform.anchorMax = new Vector2(0, 1);
        chapterIndex.rectTransform.pivot = new Vector2(0, 1);
        chapterIndex.rectTransform.anchoredPosition = new Vector2(48, -60);
        chapterIndex.rectTransform.sizeDelta = new Vector2(900, 50);
        chapterIndex.color = new Color(.7f, .7f, .75f);

        // caption block (lower third)
        GameObject backdrop = new("CaptionBackdrop");
        backdrop.transform.SetParent(transform, false);
        captionBackdrop = backdrop.AddComponent<Image>();
        captionBackdrop.color = new Color(0, 0, 0, .45f);
        backdropRect = captionBackdrop.rectTransform;
        backdropRect.anchorMin = new Vector2(0.5f, 0.2f);
        backdropRect.anchorMax = new Vector2(0.5f, 0.2f);
        backdropRect.sizeDelta = new Vector2(980, 330);

        caption = MakeText(transform, "Caption", 60, FontStyles.Bold, TextAlignmentOptions.Center);
        caption.rectTransform.anchorMin = new Vector2(0.5f, 0.2f);
        caption.rectTransform.anchorMax = new Vector2(0.5f, 0.2f);
        caption.rectTransform.sizeDelta = new Vector2(920, 330);
        caption.textWrappingMode = TextWrappingModes.Normal;
        caption.lineSpacing = 8;
        captionRect = caption.rectTransform;

        footnote = MakeText(transform, "Footnote", 26, FontStyles.Italic, TextAlignmentOptions.Center);
        footnote.rectTransform.anchorMin = new Vector2(0.5f, 0.2f);
        footnote.rectTransform.anchorMax = new Vector2(0.5f, 0.2f);
        footnote.rectTransform.anchoredPosition = new Vector2(0, 200);
        footnote.rectTransform.sizeDelta = new Vector2(960, 70);
        footnote.color = new Color(.75f, .75f, .8f);
        footnoteRect = footnote.rectTransform;

        hint = MakeText(transform, "Hint", 28, FontStyles.Normal, TextAlignmentOptions.Center);
        hint.rectTransform.anchorMin = new Vector2(0.5f, 0);
        hint.rectTransform.anchorMax = new Vector2(0.5f, 0);
        hint.rectTransform.anchoredPosition = new Vector2(0, 70);
        hint.rectTransform.sizeDelta = new Vector2(1000, 80);
        hint.color = new Color(.6f, .6f, .65f);

        BuildCard();
        Clear();
        if (VRMode.Enabled) MakeOverlay();
    }


    /// <summary>
    /// Lay the captions out for a wide (1920×1080) frame instead of the tall (1080×1920) default: a broad caption bar
    /// along the bottom with longer pages, and a title card that uses the width.
    /// </summary>
    public void SetLayout(bool landscape)
    {
        if (VRMode.Enabled) return;
        float x = 0.5f;
        if (landscape)
        {
            maxPageCharacters = 84;
            SetAnchor(titleRect, x, 0.72f, new Vector2(1500, 400));
            chapterTitle.rectTransform.sizeDelta = new Vector2(1500, 200);
            chapterSubtitle.rectTransform.sizeDelta = new Vector2(1500, 100);
            SetAnchor(backdropRect, x, 0.13f, new Vector2(1560, 220));
            SetAnchor(captionRect, x, 0.13f, new Vector2(1480, 200));
            SetAnchor(footnoteRect, x, 0.13f, new Vector2(1500, 60));
            footnoteRect.anchoredPosition = new Vector2(0, 150);
        }
        else
        {
            maxPageCharacters = 58;
            SetAnchor(titleRect, x, 0.78f, new Vector2(960, 400));
            chapterTitle.rectTransform.sizeDelta = new Vector2(960, 240);
            chapterSubtitle.rectTransform.sizeDelta = new Vector2(960, 120);
            SetAnchor(backdropRect, x, 0.2f, new Vector2(980, 330));
            SetAnchor(captionRect, x, 0.2f, new Vector2(920, 330));
            SetAnchor(footnoteRect, x, 0.2f, new Vector2(960, 70));
            footnoteRect.anchoredPosition = new Vector2(0, 200);
        }
    }

    static void SetAnchor(RectTransform r, float x, float y, Vector2 size)
    {
        r.anchorMin = r.anchorMax = new Vector2(x, y);
        r.anchoredPosition = Vector2.zero;
        r.sizeDelta = size;
    }

    /// <summary>In VR every graphic of the captions draws on top of the model (the scene is scaled around the user and would otherwise hide it).</summary>
    void MakeOverlay()
    {
        foreach (Graphic g in GetComponentsInChildren<Graphic>(true))
        {
            if (g is TMP_Text text) text.isOverlay = true;
            else g.material = VRStyle.UiMaterial;
        }
    }

    /// <summary>Captions can be switched off from the VR settings page.</summary>
    public bool Visible
    {
        get => canvas != null && canvas.enabled;
        set
        {
            if (canvas != null) canvas.enabled = value;
        }
    }

    void BuildCard()
    {
        GameObject go = new("CharacterCard");
        go.transform.SetParent(transform, false);
        card = go.AddComponent<RectTransform>();
        card.anchorMin = new Vector2(1, 1);
        card.anchorMax = new Vector2(1, 1);
        card.pivot = new Vector2(1, 1);
        card.anchoredPosition = new Vector2(-24, -96);
        card.sizeDelta = new Vector2(470, 236);
        cardGroup = go.AddComponent<CanvasGroup>();
        cardGroup.alpha = 0f;
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.03f, 0.03f, 0.06f, 0.78f);
        bg.raycastTarget = false;

        GameObject ringGo = new("Ring");
        ringGo.transform.SetParent(go.transform, false);
        cardRing = ringGo.AddComponent<Image>();
        cardRing.sprite = CircleSprite();
        cardRing.color = Color.white;
        cardRing.raycastTarget = false;
        RectTransform ringRect = cardRing.rectTransform;
        ringRect.anchorMin = new Vector2(0, 0.5f);
        ringRect.anchorMax = new Vector2(0, 0.5f);
        ringRect.anchoredPosition = new Vector2(78, 0);
        ringRect.sizeDelta = new Vector2(124, 124);

        GameObject medGo = new("Medallion");
        medGo.transform.SetParent(go.transform, false);
        cardMedallion = medGo.AddComponent<Image>();
        cardMedallion.sprite = CircleSprite();
        cardMedallion.color = new Color(0.1f, 0.1f, 0.16f, 1f);
        cardMedallion.raycastTarget = false;
        medGo.AddComponent<Mask>().showMaskGraphic = true;
        RectTransform medRect = cardMedallion.rectTransform;
        medRect.anchorMin = new Vector2(0, 0.5f);
        medRect.anchorMax = new Vector2(0, 0.5f);
        medRect.anchoredPosition = new Vector2(78, 0);
        medRect.sizeDelta = new Vector2(110, 110);

        GameObject portraitGo = new("Portrait");
        portraitGo.transform.SetParent(medGo.transform, false);
        cardPortrait = portraitGo.AddComponent<RawImage>();
        cardPortrait.raycastTarget = false;
        RectTransform pr = cardPortrait.rectTransform;
        pr.anchorMin = Vector2.zero;
        pr.anchorMax = Vector2.one;
        pr.offsetMin = new Vector2(6, 6);
        pr.offsetMax = new Vector2(-6, -6);
        cardPortrait.enabled = false;

        cardGlyph = MakeText(medGo.transform, "Glyph", 64, FontStyles.Bold, TextAlignmentOptions.Center);
        cardGlyph.rectTransform.anchorMin = Vector2.zero;
        cardGlyph.rectTransform.anchorMax = Vector2.one;
        cardGlyph.rectTransform.offsetMin = Vector2.zero;
        cardGlyph.rectTransform.offsetMax = Vector2.zero;

        cardName = MakeText(go.transform, "Name", 27, FontStyles.Bold, TextAlignmentOptions.Left);
        cardName.rectTransform.anchorMin = new Vector2(0, 1);
        cardName.rectTransform.anchorMax = new Vector2(1, 1);
        cardName.rectTransform.pivot = new Vector2(0, 1);
        cardName.rectTransform.anchoredPosition = new Vector2(152, -18);
        cardName.rectTransform.sizeDelta = new Vector2(-170, 44);
        cardYears = MakeText(go.transform, "Years", 22, FontStyles.Normal, TextAlignmentOptions.Left);
        cardYears.rectTransform.anchorMin = new Vector2(0, 1);
        cardYears.rectTransform.anchorMax = new Vector2(1, 1);
        cardYears.rectTransform.pivot = new Vector2(0, 1);
        cardYears.rectTransform.anchoredPosition = new Vector2(152, -60);
        cardYears.rectTransform.sizeDelta = new Vector2(-170, 32);
        cardYears.color = new Color(.75f, .75f, .8f);
        cardRole = MakeText(go.transform, "Role", 21, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        cardRole.rectTransform.anchorMin = new Vector2(0, 1);
        cardRole.rectTransform.anchorMax = new Vector2(1, 1);
        cardRole.rectTransform.pivot = new Vector2(0, 1);
        cardRole.rectTransform.anchoredPosition = new Vector2(152, -94);
        cardRole.rectTransform.sizeDelta = new Vector2(-170, 140);
        cardRole.textWrappingMode = TextWrappingModes.Normal;
        cardRole.color = new Color(.9f, .9f, .95f);
    }

    static Sprite circleSprite;
    static Sprite CircleSprite()
    {
        if (circleSprite != null) return circleSprite;
        const int size = 128;
        Texture2D tex = new(size, size, TextureFormat.RGBA32, false);
        Color32[] px = new Color32[size * size];
        float r = size * .5f - 1f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(size * .5f, size * .5f));
            float a = Mathf.Clamp01(r - d + .5f);
            px[y * size + x] = new Color(1, 1, 1, a);
        }

        tex.SetPixels32(px);
        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f);
        return circleSprite;
    }

    /// <summary>Show a character card (top right). Drops in a portrait from Resources/Portraits/&lt;id&gt; when present.</summary>
    public void ShowCard(string id)
    {
        if (!Characters.TryGetValue(id, out Character c))
        {
            Debug.LogWarning($"Captions: unknown character '{id}'");
            return;
        }

        cardName.text = c.Name;
        cardYears.text = c.Years;
        cardRole.text = c.Role;
        cardGlyph.text = c.Glyph;
        cardGlyph.color = c.Accent;
        cardRing.color = c.Accent;
        Texture2D portrait = Resources.Load<Texture2D>("Portraits/" + id);
        cardPortrait.enabled = portrait != null;
        cardGlyph.enabled = portrait == null;
        if (portrait != null)
        {
            cardPortrait.texture = portrait;
            float aspect = (float)portrait.width / portrait.height;
            cardPortrait.uvRect = aspect > 1f
                ? new Rect((1f - 1f / aspect) * .5f, 0f, 1f / aspect, 1f)
                : new Rect(0f, (1f - aspect) * .5f + (1f - aspect) * .25f, 1f, aspect);
        }
        if (cardRoutine != null) StopCoroutine(cardRoutine);
        cardRoutine = StartCoroutine(CardIn(id == "machine"));
    }

    public void HideCard()
    {
        if (cardRoutine != null) StopCoroutine(cardRoutine);
        cardRoutine = StartCoroutine(CardOut());
    }

    IEnumerator CardIn(bool machine)
    {
        float t = 0f;
        Vector2 target = new(-24, -96);
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0, 1, t / 0.6f);
            cardGroup.alpha = k;
            card.anchoredPosition = target + new Vector2((1 - k) * 220f, 0);
            yield return null;
        }

        cardGroup.alpha = 1f;
        card.anchoredPosition = target;
        // keep the medallion ring breathing; the machine's ring spins as well
        float time = 0f;
        while (true)
        {
            time += Time.deltaTime;
            float s = 1f + 0.05f * Mathf.Sin(time * 3f);
            cardRing.rectTransform.localScale = Vector3.one * s;
            if (machine) cardRing.rectTransform.localRotation = Quaternion.Euler(0, 0, time * 40f);
            yield return null;
        }
    }

    IEnumerator CardOut()
    {
        float a0 = cardGroup.alpha;
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            cardGroup.alpha = Mathf.Lerp(a0, 0f, t / 0.4f);
            yield return null;
        }

        cardGroup.alpha = 0f;
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, float size, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Factory.Palette.Caption;
        text.richText = true;
        text.raycastTarget = false;
        return text;
    }

    string hintText = "";
    public bool ShowFps;
    float fpsAccum, fpsTime; int fpsFrames; float fps; float worstFrame; int gcAtWindowStart;

    public void SetHint(string text)
    {
        hintText = text ?? "";
        hint.text = hintText;
    }

    void Update()
    {
        if (!ShowFps) return;
        fpsFrames++;
        fpsTime += Time.unscaledDeltaTime;
        worstFrame = Mathf.Max(worstFrame, Time.unscaledDeltaTime);
        if (fpsTime >= 1f)
        {
            fps = fpsFrames / fpsTime;
            int gcNow = System.GC.CollectionCount(0);
            hint.text = $"{fps:0} fps   worst {worstFrame * 1000f:0} ms   GC/s {gcNow - gcAtWindowStart}   {hintText}";
            gcAtWindowStart = gcNow;
            fpsFrames = 0;
            fpsTime = 0f;
            worstFrame = 0f;
        }
    }

    public void SetChapterIndex(int index, int total, string title)
    {
        chapterIndex.text = index > 0 ? $"{index} / {total}   {title}" : "";
    }

    public void ShowChapterTitle(string title, string subtitle, float seconds)
    {
        chapterTitle.text = title;
        chapterSubtitle.text = subtitle ?? "";
        if (titleRoutine != null) StopCoroutine(titleRoutine);
        titleRoutine = StartCoroutine(FadeTitle(seconds));
    }

    IEnumerator FadeTitle(float seconds)
    {
        const float fade = 0.6f;
        float t = 0f;
        while (t < fade)
        {
            t += Time.deltaTime;
            titleGroup.alpha = Mathf.SmoothStep(0, 1, t / fade);
            yield return null;
        }

        yield return new WaitForSeconds(Mathf.Max(0, seconds - 2 * fade));
        t = 0f;
        while (t < fade)
        {
            t += Time.deltaTime;
            titleGroup.alpha = Mathf.SmoothStep(1, 0, t / fade);
            yield return null;
        }

        titleGroup.alpha = 0f;
    }

    public void SetFootnote(string text)
    {
        footnote.text = text ?? "";
    }

    public void SetLine(LineTiming timing)
    {
        current = timing;
        pages.Clear();
        if (timing?.words == null) return;

        int start = 0;
        int chars = 0;
        for (int i = 0; i < timing.words.Length; i++)
        {
            int length = timing.words[i].w.Length + 1;
            if (chars + length > maxPageCharacters && i > start)
            {
                pages.Add((start, i));
                start = i;
                chars = 0;
            }

            chars += length;
        }

        pages.Add((start, timing.words.Length));
        captionBackdrop.enabled = true;
        SetTime(0f);
    }

    public void SetTime(float time)
    {
        if (current?.words == null || pages.Count == 0) return;

        WordTiming[] words = current.words;
        int index = -1;
        for (int i = 0; i < words.Length; i++)
        {
            if (time >= words[i].s) index = i;
            else break;
        }

        int pageIndex = 0;
        for (int p = 0; p < pages.Count; p++)
        {
            if (index >= pages[p].start) pageIndex = p;
        }

        // rebuild the highlighted string only when the spoken word (or the page) changes, not on every frame
        if (pageIndex == shownPage && index == shownWord && ReferenceEquals(current, shownTiming)) return;
        shownPage = pageIndex;
        shownWord = index;
        shownTiming = current;
        (int start, int end) = pages[pageIndex];
        caption.text = TourScriptLoader.Highlight(words, start, end, index, highlightHex, dimHex);
    }

    int shownPage = -1, shownWord = -2;
    LineTiming shownTiming;

    public void Clear()
    {
        if (cardRoutine != null) StopCoroutine(cardRoutine);
        if (cardGroup != null) cardGroup.alpha = 0f;
        current = null;
        shownTiming = null;
        pages.Clear();
        caption.text = "";
        captionBackdrop.enabled = false;
        footnote.text = "";
        chapterIndex.text = "";
        titleGroup.alpha = 0f;
    }
}
