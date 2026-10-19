using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The desktop controls of the model lab: a panel on the right edge with the three models, the active model's buttons,
/// toggles and sliders (with their keys), and how to move around. A screen overlay, hidden while recording; not used in VR
/// (the dock's Models page has the same controls there).
/// </summary>
public class LabPanel : MonoBehaviour
{
    const float Width = 340f;
    static readonly Color PanelColor = new(0.035f, 0.035f, 0.06f, 0.88f);
    static readonly Color ButtonColor = new(0.13f, 0.13f, 0.19f, 1f);
    static readonly Color ButtonActive = new(0.2f, 0.36f, 0.5f, 1f);
    static readonly Color TextColor = new(0.92f, 0.92f, 0.96f);
    static readonly Color DimText = new(0.6f, 0.6f, 0.68f);

    ModelLab lab;
    Canvas canvas;
    RectTransform panel, content;
    LabModel shown;
    readonly List<(Image image, LabControl control, TextMeshProUGUI text)> rows = new();
    readonly List<(RectTransform fill, TextMeshProUGUI text, LabControl control)> sliders = new();
    readonly List<(Image image, LabModel model)> modelButtons = new();

    public static LabPanel Create(ModelLab lab)
    {
        GameObject go = new("Lab Panel");
        LabPanel p = go.AddComponent<LabPanel>();
        p.lab = lab;
        p.Build();
        lab.Changed += p.Refresh;
        return p;
    }

    void Build()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 990;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
        gameObject.AddComponent<GraphicRaycaster>();

        GameObject p = new("Panel");
        p.transform.SetParent(transform, false);
        panel = p.AddComponent<RectTransform>();
        panel.anchorMin = new Vector2(1, 0);
        panel.anchorMax = new Vector2(1, 1);
        panel.pivot = new Vector2(1, .5f);
        panel.sizeDelta = new Vector2(Width, 0);
        p.AddComponent<Image>().color = PanelColor;

        GameObject c = new("Content");
        c.transform.SetParent(panel, false);
        content = c.AddComponent<RectTransform>();
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(.5f, 1);
        content.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = c.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.spacing = 7;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        c.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        panel.gameObject.SetActive(false);
    }

    void Refresh()
    {
        if (lab.Active != shown) Rebuild();
        panel.gameObject.SetActive(lab.Active != null);
    }

    void Rebuild()
    {
        shown = lab.Active;
        foreach (Transform t in content) Destroy(t.gameObject);
        rows.Clear();
        sliders.Clear();
        modelButtons.Clear();
        if (shown == null) return;

        Label("MODEL LAB", 18, FontStyles.Bold, DimText, 26);
        // the models in one row, the games in the next
        foreach (bool games in new[] { false, true })
        {
            RectTransform models = Row(48);
            foreach (LabModel m in lab.Models)
            {
                if (m.IsGame != games) continue;
                LabModel model = m;
                modelButtons.Add((Button(models, m.Short, () => lab.Enter(model.Id), VRIcons.Get(m.Icon)), m));
            }
        }

        Label(shown.Title, 28, FontStyles.Bold, TextColor, 40);
        foreach (LabControl control in shown.Controls)
        {
            LabControl ctl = control;
            if (ctl.Slider)
            {
                Slider(ctl);
                continue;
            }

            RectTransform row = Row(44);
            Image image = Button(row, ctl.Text, () =>
            {
                ctl.Clicked?.Invoke();
                Refresh();
            }, VRIcons.Get(ctl.Icon), ctl.Keys);
            rows.Add((image, ctl, image.GetComponentInChildren<TextMeshProUGUI>()));
        }

        RectTransform leave = Row(44);
        Button(leave, "Leave the lab", () => lab.Exit(), VRIcons.Get(VRIcons.Id.Close));
        Label("drag: orbit  ·  wheel: zoom  ·  right drag or Shift + drag: move  ·  click a point to pick it  ·  Home: reset the view", 15, FontStyles.Normal, DimText, 64)
            .textWrappingMode = TextWrappingModes.Normal;
    }

    void Update()
    {
        if (lab == null) return;
        canvas.enabled = TourDirector.Instance == null || !TourDirector.Instance.Recorder.IsOffscreen;
        if (shown != lab.Active) Refresh();
        if (shown == null) return;
        foreach (var (image, control, text) in rows)
        {
            image.color = control.On != null && control.On() ? ButtonActive : ButtonColor;
            if (control.DynamicLabel != null && text != null) text.text = control.Text;
        }

        foreach (var (fill, text, control) in sliders)
        {
            float v = control.Get();
            fill.anchorMax = new Vector2(Mathf.InverseLerp(control.Min, control.Max, v), 1f);
            text.text = $"{control.Label}   {(control.Format != null ? control.Format(v) : v.ToString("0.00"))}" + (string.IsNullOrEmpty(control.Keys) ? "" : $"   <color=#8C8CA0>{control.Keys}</color>");
        }

        foreach (var (image, model) in modelButtons) image.color = model == lab.Active ? ButtonActive : ButtonColor;
    }

    // ---- builders ---------------------------------------------------------------------------------------

    TextMeshProUGUI Label(string text, float size, FontStyles style, Color color, float height)
    {
        GameObject go = new("Label");
        go.transform.SetParent(content, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.font = TMP_Settings.defaultFontAsset;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAlignmentOptions.Left;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        go.AddComponent<LayoutElement>().preferredHeight = height;
        return t;
    }

    RectTransform Row(float height)
    {
        GameObject go = new("Row");
        go.transform.SetParent(content, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        HorizontalLayoutGroup h = go.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 6;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = true;
        h.childForceExpandHeight = true;
        go.AddComponent<LayoutElement>().preferredHeight = height;
        return rt;
    }

    Image Button(Transform parent, string text, UnityEngine.Events.UnityAction onClick, Sprite icon, string keys = "")
    {
        GameObject go = new("Button " + text);
        go.transform.SetParent(parent, false);
        Image bg = go.AddComponent<Image>();
        bg.color = ButtonColor;
        Button b = go.AddComponent<Button>();
        b.targetGraphic = bg;
        b.onClick.AddListener(onClick);
        go.AddComponent<LayoutElement>().flexibleWidth = 1f;
        float x = 10f;
        if (icon != null)
        {
            GameObject ig = new("Icon");
            ig.transform.SetParent(go.transform, false);
            Image img = ig.AddComponent<Image>();
            img.sprite = icon;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = TextColor;
            RectTransform ir = img.rectTransform;
            ir.anchorMin = ir.anchorMax = new Vector2(0, .5f);
            ir.pivot = new Vector2(0, .5f);
            ir.sizeDelta = new Vector2(26, 26);
            ir.anchoredPosition = new Vector2(8, 0);
            x = 40f;
        }

        GameObject tg = new("Text");
        tg.transform.SetParent(go.transform, false);
        TextMeshProUGUI t = tg.AddComponent<TextMeshProUGUI>();
        t.font = TMP_Settings.defaultFontAsset;
        t.text = text;
        t.fontSize = 20;
        t.color = TextColor;
        t.alignment = TextAlignmentOptions.Left;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.raycastTarget = false;
        RectTransform tr = t.rectTransform;
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(x, 0);
        tr.offsetMax = new Vector2(string.IsNullOrEmpty(keys) ? -6 : -70, 0);
        if (!string.IsNullOrEmpty(keys))
        {
            GameObject kg = new("Keys");
            kg.transform.SetParent(go.transform, false);
            TextMeshProUGUI k = kg.AddComponent<TextMeshProUGUI>();
            k.font = TMP_Settings.defaultFontAsset;
            k.text = keys;
            k.fontSize = 16;
            k.color = DimText;
            k.alignment = TextAlignmentOptions.Right;
            k.raycastTarget = false;
            RectTransform kr = k.rectTransform;
            kr.anchorMin = new Vector2(1, 0);
            kr.anchorMax = new Vector2(1, 1);
            kr.pivot = new Vector2(1, .5f);
            kr.sizeDelta = new Vector2(64, 0);
            kr.anchoredPosition = new Vector2(-8, 0);
        }

        return bg;
    }

    void Slider(LabControl ctl)
    {
        GameObject go = new("Slider " + ctl.Label);
        go.transform.SetParent(content, false);
        go.AddComponent<LayoutElement>().preferredHeight = 56;
        RectTransform rt = go.AddComponent<RectTransform>();
        GameObject tg = new("Text");
        tg.transform.SetParent(go.transform, false);
        TextMeshProUGUI t = tg.AddComponent<TextMeshProUGUI>();
        t.font = TMP_Settings.defaultFontAsset;
        t.fontSize = 19;
        t.color = TextColor;
        t.alignment = TextAlignmentOptions.Left;
        t.raycastTarget = false;
        RectTransform tr = t.rectTransform;
        tr.anchorMin = new Vector2(0, 1);
        tr.anchorMax = new Vector2(1, 1);
        tr.pivot = new Vector2(.5f, 1);
        tr.sizeDelta = new Vector2(0, 26);
        tr.anchoredPosition = Vector2.zero;

        GameObject track = new("Track");
        track.transform.SetParent(go.transform, false);
        Image ti = track.AddComponent<Image>();
        ti.color = ButtonColor;
        RectTransform trk = ti.rectTransform;
        trk.anchorMin = new Vector2(0, 0);
        trk.anchorMax = new Vector2(1, 0);
        trk.pivot = new Vector2(.5f, 0);
        trk.sizeDelta = new Vector2(0, 22);
        trk.anchoredPosition = new Vector2(0, 2);
        GameObject fill = new("Fill");
        fill.transform.SetParent(track.transform, false);
        Image fi = fill.AddComponent<Image>();
        fi.color = ButtonActive;
        fi.raycastTarget = false;
        RectTransform fr = fi.rectTransform;
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = new Vector2(.5f, 1);
        fr.offsetMin = fr.offsetMax = Vector2.zero;
        LabSliderDrag drag = track.AddComponent<LabSliderDrag>();
        drag.Changed = u => ctl.Set(Mathf.Lerp(ctl.Min, ctl.Max, u));
        sliders.Add((fr, t, ctl));
    }
}

/// <summary>Pointer down or drag on a slider's track sets its value from the pointer's position along it.</summary>
public class LabSliderDrag : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public System.Action<float> Changed;

    public void OnPointerDown(PointerEventData e) => Apply(e);
    public void OnDrag(PointerEventData e) => Apply(e);

    void Apply(PointerEventData e)
    {
        RectTransform rt = (RectTransform)transform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out Vector2 local)) return;
        Rect r = rt.rect;
        Changed?.Invoke(Mathf.Clamp01((local.x - r.xMin) / Mathf.Max(1f, r.width)));
    }
}
