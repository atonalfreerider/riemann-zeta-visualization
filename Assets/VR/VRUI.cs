using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Colours, fonts and the shared always-on-top UI material.</summary>
public static class VRStyle
{
    public static readonly Color Panel = new(0.045f, 0.055f, 0.095f, 0.90f);
    public static readonly Color Button = new(0.15f, 0.17f, 0.27f, 0.96f);
    public static readonly Color ButtonHover = new(0.27f, 0.32f, 0.52f, 1f);
    public static readonly Color Accent = new(1f, 0.80f, 0.16f, 1f);
    public static readonly Color AccentDim = new(0.55f, 0.43f, 0.08f, 1f);
    public static readonly Color Text = new(0.96f, 0.97f, 1f, 1f);
    public static readonly Color TextDim = new(0.70f, 0.73f, 0.84f, 1f);
    public static readonly Color Good = new(0.35f, 0.92f, 0.62f, 1f);

    static Material ui;

    /// <summary>Flat UI shader that ignores scene depth (the interface must never vanish into the model).</summary>
    public static Material UiMaterial
    {
        get
        {
            if (ui != null) return ui;
            Shader shader = Shader.Find("Zeta/VRUI");
            ui = new Material(shader != null ? shader : Shader.Find("UI/Default")) { name = "VR UI" };
            ui.renderQueue = 4000;
            return ui;
        }
    }

    public static TextMeshProUGUI Text_(Transform parent, string text, float size, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center, FontStyles style = FontStyles.Bold)
    {
        GameObject go = new("Text");
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.font = TMP_Settings.defaultFontAsset;
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = alignment;
        t.color = color;
        t.text = text;
        t.richText = true;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.isOverlay = true; // draw over the visualization
        return t;
    }

    public static Image Image_(Transform parent, Sprite sprite, Color color, bool sliced = false)
    {
        GameObject go = new("Image");
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.material = UiMaterial;
        image.raycastTarget = false;
        if (sliced) image.type = Image.Type.Sliced;
        return image;
    }
}

/// <summary>A flat world-space canvas measured in millimetres (1 canvas unit = 1 mm of the user's own space).</summary>
public class VRPanel : MonoBehaviour
{
    public RectTransform Rect { get; private set; }
    public Vector2 Size { get; private set; }
    public readonly List<VRControl> Controls = new();
    public bool Open { get; private set; }
    /// <summary>Panels this size or smaller are palm sized; bigger ones are tablets.</summary>
    public CanvasGroup Group { get; private set; }

    float alpha;
    float targetAlpha;

    public static VRPanel Create(string name, Vector2 sizeMm, Transform parent, bool backdrop = true)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;
        VRPanel panel = go.AddComponent<VRPanel>();
        panel.Group = go.AddComponent<CanvasGroup>();
        panel.Rect = go.GetComponent<RectTransform>();
        panel.Rect.sizeDelta = sizeMm;
        panel.Rect.localScale = Vector3.one * 0.001f;
        panel.Size = sizeMm;
        if (backdrop)
        {
            Image back = VRStyle.Image_(go.transform, VRIcons.RoundedBox(), VRStyle.Panel, true);
            back.rectTransform.anchorMin = Vector2.zero;
            back.rectTransform.anchorMax = Vector2.one;
            back.rectTransform.offsetMin = Vector2.zero;
            back.rectTransform.offsetMax = Vector2.zero;
        }

        panel.alpha = 0f;
        panel.Group.alpha = 0f;
        go.SetActive(false);
        return panel;
    }

    public void SetOpen(bool open)
    {
        Open = open;
        targetAlpha = open ? 1f : 0f;
        if (open) gameObject.SetActive(true);
    }

    void Update()
    {
        alpha = Mathf.MoveTowards(alpha, targetAlpha, Time.unscaledDeltaTime * 6f);
        Group.alpha = alpha;
        if (alpha <= 0f && !Open) gameObject.SetActive(false);
    }

    /// <summary>True when the panel is solid enough to be touched.</summary>
    public bool Touchable => Open && alpha > 0.5f;

    // ---- layout helpers: (x, y) is the control's top-left corner measured from the panel's top-left, y downward ----

    public static void Place(RectTransform r, float x, float y, float w, float h)
    {
        r.anchorMin = new Vector2(0, 1);
        r.anchorMax = new Vector2(0, 1);
        r.pivot = new Vector2(0, 1);
        r.anchoredPosition = new Vector2(x, -y);
        r.sizeDelta = new Vector2(w, h);
    }

    /// <summary>A page: a transparent child that fills the panel, so a whole tab of controls can be shown or hidden at once.</summary>
    public RectTransform AddPage(string name, float x, float y, float w, float h)
    {
        GameObject go = new("Page " + name);
        go.transform.SetParent(transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        Place(rt, x, y, w, h);
        return rt;
    }

    public VRButton AddButton(float x, float y, float w, float h, string label, VRIcons.Id? icon, Action clicked, bool toggle = false, Func<bool> state = null, float fontSize = 22f, Transform parent = null)
    {
        GameObject go = new("Button " + label);
        go.transform.SetParent(parent != null ? parent : transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        Place(rt, x, y, w, h);
        VRButton button = go.AddComponent<VRButton>();
        button.Build(this, rt, label, icon, clicked, toggle, state, fontSize);
        Controls.Add(button);
        return button;
    }

    public VRSlider AddSlider(float x, float y, float w, float h, string label, float min, float max, Func<float> get, Action<float> set, Func<float, string> format = null, Transform parent = null)
    {
        GameObject go = new("Slider " + label);
        go.transform.SetParent(parent != null ? parent : transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        Place(rt, x, y, w, h);
        VRSlider slider = go.AddComponent<VRSlider>();
        slider.Build(this, rt, label, min, max, get, set, format);
        Controls.Add(slider);
        return slider;
    }

    public TextMeshProUGUI AddLabel(float x, float y, float w, float h, string text, float size, Color? color = null, TextAlignmentOptions alignment = TextAlignmentOptions.Left, FontStyles style = FontStyles.Bold, Transform parent = null)
    {
        TextMeshProUGUI t = VRStyle.Text_(parent != null ? parent : transform, text, size, color ?? VRStyle.Text, alignment, style);
        Place(t.rectTransform, x, y, w, h);
        return t;
    }
}

/// <summary>Anything a fingertip can touch.</summary>
public abstract class VRControl : MonoBehaviour
{
    public VRPanel Panel { get; protected set; }
    public RectTransform Rt { get; protected set; }
    public bool Interactable = true;
    /// <summary>0..1: how close the fingertip hovers.</summary>
    public float HoverAmount;
    /// <summary>0..1: how far the fingertip has pushed in.</summary>
    public float PushAmount;
    public bool Held { get; protected set; }

    public virtual bool Contains(Vector3 local, float padding)
    {
        Rect r = Rt.rect;
        return local.x >= r.xMin - padding && local.x <= r.xMax + padding && local.y >= r.yMin - padding && local.y <= r.yMax + padding;
    }

    public virtual void PressStart(Vector3 local) { Held = true; }
    public virtual void PressDrag(Vector3 local) { }
    public virtual void PressEnd() { Held = false; }
}

public class VRButton : VRControl
{
    public Action Clicked;
    public bool Toggle;
    public Func<bool> State;
    public bool On;
    public string Label { get; private set; }
    public Color OnColor = VRStyle.Accent;

    Image back;
    Image icon;
    TextMeshProUGUI text;
    RectTransform content;
    Func<VRIcons.Id> dynamicIcon;
    Func<string> dynamicLabel;
    float pulse;

    public void Build(VRPanel panel, RectTransform rt, string label, VRIcons.Id? iconId, Action clicked, bool toggle, Func<bool> state, float fontSize)
    {
        Panel = panel;
        Rt = rt;
        Label = label;
        Clicked = clicked;
        Toggle = toggle;
        State = state;

        back = VRStyle.Image_(transform, VRIcons.RoundedBox(), VRStyle.Button, true);
        Stretch(back.rectTransform);

        GameObject c = new("Content");
        c.transform.SetParent(transform, false);
        content = c.AddComponent<RectTransform>();
        Stretch(content);

        float w = rt.sizeDelta.x, h = rt.sizeDelta.y;
        bool hasText = !string.IsNullOrEmpty(label);
        if (iconId.HasValue)
        {
            icon = VRStyle.Image_(content, VRIcons.Get(iconId.Value), VRStyle.Text);
            icon.preserveAspect = true;
            float side = hasText ? Mathf.Min(w - 12f, h - fontSize * 1.55f - 6f) : Mathf.Min(w, h) - 14f;
            side = Mathf.Max(side, 20f);
            RectTransform ir = icon.rectTransform;
            ir.anchorMin = ir.anchorMax = new Vector2(.5f, hasText ? 1f : .5f);
            ir.pivot = new Vector2(.5f, hasText ? 1f : .5f);
            ir.sizeDelta = new Vector2(side, side);
            ir.anchoredPosition = new Vector2(0, hasText ? -6f : 0f);
        }

        if (hasText)
        {
            text = VRStyle.Text_(content, label, fontSize, VRStyle.Text);
            RectTransform tr = text.rectTransform;
            tr.anchorMin = new Vector2(0, 0);
            tr.anchorMax = new Vector2(1, 0);
            tr.pivot = new Vector2(.5f, 0);
            tr.sizeDelta = new Vector2(-6f, iconId.HasValue ? fontSize * 1.5f : h);
            tr.anchoredPosition = new Vector2(0, iconId.HasValue ? 3f : 0f);
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = true;
            text.fontSizeMax = fontSize;
            text.fontSizeMin = fontSize * 0.6f;
        }
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    /// <summary>Icon and label that depend on state (play/pause, passthrough/blackout).</summary>
    public VRButton Dynamic(Func<VRIcons.Id> iconFunc, Func<string> labelFunc = null)
    {
        dynamicIcon = iconFunc;
        dynamicLabel = labelFunc;
        return this;
    }

    public void Flash() => pulse = 1f;

    public override void PressStart(Vector3 local)
    {
        base.PressStart(local);
        pulse = 1f;
        VRAudio.Click();
        Clicked?.Invoke();
        if (Toggle && State == null) On = !On;
    }

    void Update()
    {
        if (State != null) On = State();
        if (dynamicIcon != null && icon != null) icon.sprite = VRIcons.Get(dynamicIcon());
        if (dynamicLabel != null && text != null)
        {
            string l = dynamicLabel();
            if (text.text != l) text.text = l;
        }

        pulse = Mathf.MoveTowards(pulse, 0f, Time.unscaledDeltaTime * 4f);
        Color baseColor = Color.Lerp(VRStyle.Button, VRStyle.ButtonHover, HoverAmount);
        if (On && Toggle) baseColor = Color.Lerp(OnColor * 0.55f, OnColor, HoverAmount * 0.5f + 0.5f * pulse);
        baseColor = Color.Lerp(baseColor, Color.white, pulse * 0.55f);
        baseColor.a = Interactable ? 1f : 0.35f;
        back.color = baseColor;
        bool dark = On && Toggle;
        Color fg = dark ? new Color(0.08f, 0.07f, 0.02f, 1f) : VRStyle.Text;
        if (!Interactable) fg.a = 0.4f;
        if (icon != null) icon.color = fg;
        if (text != null) text.color = fg;

        float lift = HoverAmount * 4f;
        float push = PushAmount * 8f;
        content.anchoredPosition3D = new Vector3(0, 0, -lift + push);
        transform.localScale = Vector3.one * (1f + HoverAmount * 0.035f);
    }
}

public class VRSlider : VRControl
{
    float min, max;
    Func<float> get;
    Action<float> set;
    Func<float, string> format;
    RectTransform track, fill, knob;
    Image fillImage, knobImage;
    TextMeshProUGUI title, valueText;
    float trackLeft, trackWidth;

    public void Build(VRPanel panel, RectTransform rt, string label, float min, float max, Func<float> get, Action<float> set, Func<float, string> format)
    {
        Panel = panel;
        Rt = rt;
        this.min = min;
        this.max = max;
        this.get = get;
        this.set = set;
        this.format = format ?? (v => v.ToString("0.0"));

        float w = rt.sizeDelta.x, h = rt.sizeDelta.y;
        title = VRStyle.Text_(transform, label, 22f, VRStyle.TextDim, TextAlignmentOptions.Left);
        VRPanel.Place(title.rectTransform, 4, 0, w * 0.55f, 28);
        valueText = VRStyle.Text_(transform, "", 22f, VRStyle.Text, TextAlignmentOptions.Right);
        VRPanel.Place(valueText.rectTransform, w * 0.5f, 0, w * 0.5f - 4, 28);

        trackLeft = 22f;
        trackWidth = w - 44f;
        Image trackImage = VRStyle.Image_(transform, VRIcons.RoundedBox(), new Color(0.18f, 0.2f, 0.3f, 1f), true);
        track = trackImage.rectTransform;
        VRPanel.Place(track, trackLeft, 36, trackWidth, 14);
        fillImage = VRStyle.Image_(transform, VRIcons.RoundedBox(), VRStyle.Accent, true);
        fill = fillImage.rectTransform;
        VRPanel.Place(fill, trackLeft, 36, 14, 14);
        knobImage = VRStyle.Image_(transform, VRIcons.Circle(), Color.white);
        knob = knobImage.rectTransform;
        VRPanel.Place(knob, trackLeft, 28, 30, 30);
    }

    public override bool Contains(Vector3 local, float padding)
    {
        Rect r = Rt.rect;
        // the whole slider row is touchable, taller than it looks so the finger need not be exact
        return local.x >= r.xMin - padding && local.x <= r.xMax + padding && local.y >= r.yMin - padding - 14f && local.y <= r.yMax + padding;
    }

    public override void PressStart(Vector3 local)
    {
        base.PressStart(local);
        VRAudio.Click();
        Apply(local);
    }

    public override void PressDrag(Vector3 local) => Apply(local);

    void Apply(Vector3 local)
    {
        // local is in the slider rect's own space; convert to the position along the track
        float xFromLeft = local.x - Rt.rect.xMin;
        float t = Mathf.Clamp01((xFromLeft - trackLeft) / trackWidth);
        set?.Invoke(Mathf.Lerp(min, max, t));
    }

    void Update()
    {
        float v = Mathf.Clamp(get(), min, max);
        float t = Mathf.InverseLerp(min, max, v);
        valueText.text = format(v);
        VRPanel.Place(fill, trackLeft, 36, Mathf.Max(14f, trackWidth * t), 14);
        VRPanel.Place(knob, trackLeft + trackWidth * t - 15f, 28, 30, 30);
        knobImage.color = Color.Lerp(Color.white, VRStyle.Accent, Mathf.Max(HoverAmount, Held ? 1f : 0f));
        transform.localScale = Vector3.one * (1f + HoverAmount * 0.02f);
    }
}

/// <summary>Short synthesized UI sounds (no audio assets).</summary>
public static class VRAudio
{
    static AudioSource source;
    static AudioClip click, tick;

    public static void Init(Transform parent)
    {
        if (source != null) return;
        GameObject go = new("VR UI Audio");
        go.transform.SetParent(parent, false);
        source = go.AddComponent<AudioSource>();
        source.spatialBlend = 0f;
        source.playOnAwake = false;
        source.ignoreListenerPause = true;
        click = Tone("click", 0.07f, 1180f, 0.0f);
        tick = Tone("tick", 0.035f, 760f, 0.0f);
    }

    static AudioClip Tone(string name, float seconds, float frequency, float vibrato)
    {
        int rate = 24000, n = Mathf.RoundToInt(seconds * rate);
        float[] data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float env = Mathf.Exp(-t * 55f) * Mathf.Clamp01(i / 24f);
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * env * 0.35f + Mathf.Sin(2f * Mathf.PI * frequency * 2.01f * t) * env * 0.12f;
        }

        AudioClip clip = AudioClip.Create(name, n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public static void Click()
    {
        if (source != null) source.PlayOneShot(click, 0.7f);
    }

    public static void Tick()
    {
        if (source != null) source.PlayOneShot(tick, 0.4f);
    }
}

/// <summary>
/// Tests the index fingertips against every open panel. A button is pressed by pushing the fingertip through it
/// (about 12 mm past its face), fires once, and re-arms when the finger comes back out; there is no ray and no pinch
/// to learn, you just touch things.
/// </summary>
public class VRPoker : MonoBehaviour
{
    public const float TipRadius = 0.008f;   // metres: the tip joint sits inside the fingertip
    public const float HoverStart = 0.050f;
    public const float PressDepth = 0.010f;
    public const float ReleaseDepth = 0.026f;

    class FingerState
    {
        public VRControl Hover;
        public VRControl Down;
        public bool Armed = true;
        public float Depth;
    }

    readonly List<VRPanel> panels = new();
    readonly Dictionary<VRHand, FingerState> states = new();
    VRRig rig;
    VRHands hands;

    public static VRPoker Create(VRRig rig, VRHands hands)
    {
        VRPoker poker = rig.gameObject.AddComponent<VRPoker>();
        poker.rig = rig;
        poker.hands = hands;
        poker.states[hands.Left] = new FingerState();
        poker.states[hands.Right] = new FingerState();
        return poker;
    }

    public void Register(VRPanel panel) => panels.Add(panel);
    public IReadOnlyList<VRPanel> Panels => panels;

    /// <summary>Hover/press feedback for the hand visuals: -1 none, 0 hovering, 1 pressed.</summary>
    public int Feedback(VRHand hand)
    {
        FingerState s = states[hand];
        if (s.Down != null) return 1;
        return s.Hover != null ? 0 : -1;
    }

    public bool Hovering(VRHand hand) => states[hand].Hover != null || states[hand].Down != null;

    /// <summary>True if the point (tracking space) is within <paramref name="margin"/> metres of any open panel.</summary>
    public bool NearPanel(Vector3 trackingPoint, float margin)
    {
        Vector3 world = rig.Offset.TransformPoint(trackingPoint);
        foreach (VRPanel panel in panels)
        {
            if (!panel.Touchable) continue;
            Vector3 lp = panel.Rect.InverseTransformPoint(world);
            Rect r = panel.Rect.rect;
            float m = margin * 1000f;
            if (lp.x > r.xMin - m && lp.x < r.xMax + m && lp.y > r.yMin - m && lp.y < r.yMax + m && Mathf.Abs(lp.z) < m * 1.5f) return true;
        }

        return false;
    }

    void Update()
    {
        foreach (VRHand hand in new[] { hands.Left, hands.Right }) UpdateHand(hand, states[hand]);
        foreach (VRPanel panel in panels)
        {
            if (!panel.Open) continue;
            foreach (VRControl control in panel.Controls)
            {
                bool hovered = false, pushed = false;
                foreach (FingerState s in states.Values)
                {
                    if (s.Hover == control || s.Down == control)
                    {
                        hovered = true;
                        pushed |= s.Down == control;
                    }
                }

                control.HoverAmount = Mathf.MoveTowards(control.HoverAmount, hovered ? 1f : 0f, Time.unscaledDeltaTime * 9f);
                float pushTarget = 0f;
                foreach (FingerState s in states.Values)
                {
                    if (s.Hover == control || s.Down == control) pushTarget = Mathf.Max(pushTarget, Mathf.InverseLerp(HoverStart * 0.6f, 0f, s.Depth));
                }

                control.PushAmount = Mathf.MoveTowards(control.PushAmount, pushTarget, Time.unscaledDeltaTime * 14f);
            }
        }
    }

    void UpdateHand(VRHand hand, FingerState s)
    {
        if (!hand.Tracked || hand.Pinching || !hand.IndexExtended && hand.PinchDistance < 0.05f)
        {
            Release(s);
            s.Hover = null;
            s.Armed = true;
            return;
        }

        Vector3 tip = rig.Offset.TransformPoint(hand.IndexTip);
        VRControl best = null;
        float bestDepth = float.MaxValue;
        Vector3 bestLocal = Vector3.zero;
        foreach (VRPanel panel in panels)
        {
            if (!panel.Touchable) continue;
            foreach (VRControl control in panel.Controls)
            {
                if (!control.Interactable || !control.isActiveAndEnabled) continue;
                Vector3 lp = control.Rt.InverseTransformPoint(tip);
                float depth = -lp.z * 0.001f - TipRadius; // metres in front of the face (the canvas faces -z)
                if (depth > HoverStart || depth < -0.04f) continue;
                if (!control.Contains(lp, 3f)) continue;
                if (depth < bestDepth)
                {
                    best = control;
                    bestDepth = depth;
                    bestLocal = lp;
                }
            }
        }

        if (s.Down != null)
        {
            // a held control keeps the finger until it leaves the panel or retreats
            Vector3 lp = s.Down.Rt.InverseTransformPoint(tip);
            float depth = -lp.z * 0.001f - TipRadius;
            s.Depth = depth;
            if (depth > ReleaseDepth || depth < -0.05f || !s.Down.Contains(lp, 40f))
            {
                Release(s);
            }
            else
            {
                s.Down.PressDrag(lp);
                return;
            }
        }

        s.Hover = best;
        s.Depth = best != null ? bestDepth : 1f;
        if (best == null)
        {
            s.Armed = true;
            return;
        }

        if (bestDepth > ReleaseDepth) s.Armed = true;
        if (s.Armed && bestDepth < PressDepth)
        {
            s.Armed = false;
            s.Down = best;
            best.PressStart(bestLocal);
        }
    }

    static void Release(FingerState s)
    {
        if (s.Down != null) s.Down.PressEnd();
        s.Down = null;
    }
}
