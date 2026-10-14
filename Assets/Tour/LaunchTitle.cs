using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The launch graphic: a small Sacks spiral draws itself out from 1 (primes in gold, squares in red), the title and the
/// subtitle rise in beneath it, it holds, then the whole card lifts away. Played over the saddle the first time it lands
/// on the desktop, and in front of the user when the headset app starts. <see cref="Freeze"/> holds one moment, for the
/// still image baked into the Quest's system splash screen.
/// </summary>
public class LaunchTitle : MonoBehaviour
{
    const int Dots = 260;
    const float DotsIn = 2.2f, TitleIn = 1.6f, Hold = 3.2f, Out = 1.4f;
    public const float Duration = DotsIn + Hold + Out;

    CanvasGroup group;
    RectTransform card, emblem;
    readonly RectTransform[] dots = new RectTransform[Dots];
    readonly float[] dotSize = new float[Dots];
    RectTransform title, subtitle, rule;
    CanvasGroup titleGroup, subtitleGroup;
    float time;
    bool frozen;

    static Sprite disc, glow;

    /// <summary>Show the card over the screen (desktop) or on the headset's floating panel (VR), then remove it.</summary>
    public static LaunchTitle Play()
    {
        LaunchTitle t = Create(VRMode.Enabled);
        return t;
    }

    public static LaunchTitle Create(bool vr)
    {
        GameObject go = new("LaunchTitle");
        LaunchTitle t = go.AddComponent<LaunchTitle>();
        Canvas canvas = go.AddComponent<Canvas>();
        if (vr && VRMode.Rig != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 20;
            go.transform.SetParent(VRMode.Rig.Hud, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1080, 1080);
            rect.localPosition = new Vector3(0f, .05f, 0f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * .0011f;
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 950; // over the world and the recording preview, under the side menu
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
        }

        t.Build(vr);
        return t;
    }

    /// <summary>Hold the animation at <paramref name="seconds"/> (for a still image).</summary>
    public void Freeze(float seconds)
    {
        frozen = true;
        time = seconds;
        Apply();
    }

    void Build(bool vr)
    {
        group = gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        card = new GameObject("Card").AddComponent<RectTransform>();
        card.SetParent(transform, false);
        card.anchorMin = card.anchorMax = new Vector2(.5f, .5f);
        card.sizeDelta = new Vector2(1000, 1000);

        // a soft dark pool so the card reads over the bright saddle
        Image pool = Img(card, "Pool", Glow(), new Color(0f, 0f, .02f, .82f), Vector2.zero, new Vector2(1500, 1300));
        pool.transform.SetAsFirstSibling();

        emblem = new GameObject("Emblem").AddComponent<RectTransform>();
        emblem.SetParent(card, false);
        emblem.anchoredPosition = new Vector2(0, 110);
        emblem.sizeDelta = new Vector2(760, 760);
        Img(emblem, "Halo", Glow(), new Color(1f, .82f, .2f, .16f), Vector2.zero, new Vector2(820, 820));
        for (int i = 0; i < Dots; i++)
        {
            int n = i + 1;
            bool prime = IsPrime(n), square = Mathf.RoundToInt(Mathf.Sqrt(n)) * Mathf.RoundToInt(Mathf.Sqrt(n)) == n;
            float r = 21f * Mathf.Sqrt(n), a = 2f * Mathf.PI * Mathf.Sqrt(n);
            Color c = prime ? Factory.Palette.Zero : square ? SacksSpiral.SquareColor : new Color(.55f, .58f, .7f, .55f);
            float size = prime ? 15f : square ? 12f : 6.5f;
            Image d = Img(emblem, "n" + n, Disc(), c, new Vector2(r * Mathf.Cos(a), r * Mathf.Sin(a)), Vector2.one * size);
            dots[i] = d.rectTransform;
            dotSize[i] = size;
            if (prime) Img(d.rectTransform, "glow", Glow(), new Color(c.r, c.g, c.b, .45f), Vector2.zero, Vector2.one * size * 3.2f).transform.SetAsFirstSibling();
        }

        title = Text(card, "Zeta Viewer", 112, FontStyles.Bold, Color.white, new Vector2(0, -330), out titleGroup);
        title.GetComponent<TextMeshProUGUI>().characterSpacing = 14;
        rule = Img(card, "Rule", null, new Color(1f, .82f, .2f, .9f), new Vector2(0, -395), new Vector2(520, 3)).rectTransform;
        subtitle = Text(card, "the primes  ·  the Sacks spiral  ·  the zeta zeros", 36, FontStyles.Normal, new Color(.82f, .84f, .92f), new Vector2(0, -440), out subtitleGroup);
        subtitle.GetComponent<TextMeshProUGUI>().characterSpacing = 4;
        rule.SetParent(subtitle, true);

        if (vr)
        {
            // drawn over the model in the headset, like the other floating panels
            foreach (Graphic g in GetComponentsInChildren<Graphic>(true))
            {
                if (g is TMP_Text text) text.isOverlay = true;
                else g.material = VRStyle.UiMaterial;
            }
        }

        Apply();
    }

    void Update()
    {
        if (frozen) return;
        time += Time.unscaledDeltaTime;
        Apply();
        if (time >= Duration) Destroy(gameObject);
    }

    void Apply()
    {
        float reach = Mathf.Sqrt(Dots);
        for (int i = 0; i < Dots; i++)
        {
            // each number appears as the drawing passes its distance √n from the centre
            float at = Mathf.Sqrt(i + 1) / reach * DotsIn;
            float u = Mathf.Clamp01((time - at) / .35f);
            float pop = u < 1f ? Mathf.SmoothStep(0f, 1f, u) * (1f + .6f * Mathf.Sin(u * Mathf.PI)) : 1f;
            dots[i].localScale = Vector3.one * pop;
        }

        emblem.localRotation = Quaternion.Euler(0, 0, -time * 6f);
        float tu = Mathf.Clamp01((time - DotsIn * .55f) / TitleIn);
        titleGroup.alpha = Mathf.SmoothStep(0f, 1f, tu);
        title.anchoredPosition = new Vector2(0, -330 - 40f * (1f - Mathf.SmoothStep(0f, 1f, tu)));
        float su = Mathf.Clamp01((time - DotsIn * .55f - .5f) / TitleIn);
        subtitleGroup.alpha = Mathf.SmoothStep(0f, 1f, su);
        rule.localScale = new Vector3(Mathf.SmoothStep(0f, 1f, su), 1f, 1f);
        float outU = Mathf.Clamp01((time - DotsIn - Hold) / Out);
        group.alpha = 1f - Mathf.SmoothStep(0f, 1f, outU);
        card.localScale = Vector3.one * (1f + .08f * Mathf.SmoothStep(0f, 1f, outU));
    }

    static bool IsPrime(int n)
    {
        if (n < 2) return false;
        for (int d = 2; d * d <= n; d++) if (n % d == 0) return false;
        return true;
    }

    static Image Img(RectTransform parent, string name, Sprite sprite, Color color, Vector2 position, Vector2 size)
    {
        RectTransform r = new GameObject(name).AddComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
        r.anchoredPosition = position;
        r.sizeDelta = size;
        Image img = r.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static RectTransform Text(RectTransform parent, string text, float size, FontStyles style, Color color, Vector2 position, out CanvasGroup g)
    {
        RectTransform r = new GameObject("Text").AddComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
        r.anchoredPosition = position;
        r.sizeDelta = new Vector2(1400, size * 1.4f);
        TextMeshProUGUI t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = TMP_Settings.defaultFontAsset;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        g = r.gameObject.AddComponent<CanvasGroup>();
        return r;
    }

    static Sprite Disc()
    {
        if (disc != null) return disc;
        disc = Radial(64, d => Mathf.Clamp01((.5f - d) * 64f / 1.5f));
        return disc;
    }

    static Sprite Glow()
    {
        if (glow != null) return glow;
        glow = Radial(128, d => Mathf.Pow(Mathf.Clamp01(1f - d * 2f), 2.2f));
        return glow;
    }

    /// <summary>A white circular sprite whose alpha is f(distance from centre), distance 0 … 0.5 at the edge.</summary>
    static Sprite Radial(int n, System.Func<float, float> alpha)
    {
        Texture2D tex = new(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n * .5f, n * .5f)) / n;
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha(d)));
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(.5f, .5f), 100f);
    }
}
