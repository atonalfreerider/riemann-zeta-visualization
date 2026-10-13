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
    const int MaxPageCharacters = 58;

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
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 0.5f;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        // chapter title card
        GameObject titleCard = new("TitleCard");
        titleCard.transform.SetParent(transform, false);
        RectTransform titleRect = titleCard.AddComponent<RectTransform>();
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
        chapterIndex.rectTransform.anchoredPosition = new Vector2(48, -140);
        chapterIndex.rectTransform.sizeDelta = new Vector2(900, 50);
        chapterIndex.color = new Color(.7f, .7f, .75f);

        // caption block (lower third)
        GameObject backdrop = new("CaptionBackdrop");
        backdrop.transform.SetParent(transform, false);
        captionBackdrop = backdrop.AddComponent<Image>();
        captionBackdrop.color = new Color(0, 0, 0, .45f);
        RectTransform backdropRect = captionBackdrop.rectTransform;
        backdropRect.anchorMin = new Vector2(0.5f, 0.2f);
        backdropRect.anchorMax = new Vector2(0.5f, 0.2f);
        backdropRect.sizeDelta = new Vector2(980, 330);

        caption = MakeText(transform, "Caption", 60, FontStyles.Bold, TextAlignmentOptions.Center);
        caption.rectTransform.anchorMin = new Vector2(0.5f, 0.2f);
        caption.rectTransform.anchorMax = new Vector2(0.5f, 0.2f);
        caption.rectTransform.sizeDelta = new Vector2(920, 330);
        caption.textWrappingMode = TextWrappingModes.Normal;
        caption.lineSpacing = 8;

        footnote = MakeText(transform, "Footnote", 26, FontStyles.Italic, TextAlignmentOptions.Center);
        footnote.rectTransform.anchorMin = new Vector2(0.5f, 0.2f);
        footnote.rectTransform.anchorMax = new Vector2(0.5f, 0.2f);
        footnote.rectTransform.anchoredPosition = new Vector2(0, 200);
        footnote.rectTransform.sizeDelta = new Vector2(960, 70);
        footnote.color = new Color(.75f, .75f, .8f);

        hint = MakeText(transform, "Hint", 28, FontStyles.Normal, TextAlignmentOptions.Center);
        hint.rectTransform.anchorMin = new Vector2(0.5f, 0);
        hint.rectTransform.anchorMax = new Vector2(0.5f, 0);
        hint.rectTransform.anchoredPosition = new Vector2(0, 70);
        hint.rectTransform.sizeDelta = new Vector2(1000, 80);
        hint.color = new Color(.6f, .6f, .65f);

        Clear();
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

    public void SetHint(string text)
    {
        hint.text = text ?? "";
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
            if (chars + length > MaxPageCharacters && i > start)
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

        (int start, int end) = pages[pageIndex];
        caption.text = TourScriptLoader.Highlight(words, start, end, index, highlightHex, dimHex);
    }

    public void Clear()
    {
        current = null;
        pages.Clear();
        caption.text = "";
        captionBackdrop.enabled = false;
        footnote.text = "";
        chapterIndex.text = "";
        titleGroup.alpha = 0f;
    }
}
