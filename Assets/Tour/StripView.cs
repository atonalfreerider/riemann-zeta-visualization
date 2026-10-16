using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A number's column laid on its side: a bar cut into one piece per prime factor (as wide as log p, in the prime's colour,
/// the prime written inside), the values at the cuts above it, and under each run of equal pieces a bracket with the prime
/// and its exponent. Pieces light one at a time as the path back drops them. Built into any UI parent: the caption overlay
/// (sizes in pixels, scale 1) or a VR panel (sizes in millimetres).
/// </summary>
public class StripView
{
    readonly RectTransform root;
    readonly float k;
    readonly Func<Transform, string, float, TextMeshProUGUI> makeText;
    readonly List<Image> pieces = new();
    readonly List<Color> colors = new();
    readonly List<TextMeshProUGUI> inner = new(), marks = new();
    readonly List<(GameObject go, int last)> groups = new();

    /// <summary>A material for the bars (the VR panels use one that ignores scene depth); null keeps the default UI material.</summary>
    public Material ImageMaterial;

    public StripView(RectTransform root, float scale, Func<Transform, string, float, TextMeshProUGUI> makeText)
    {
        this.root = root;
        k = scale;
        this.makeText = makeText;
    }

    /// <summary>The strip data of n: pieces (smallest prime first), cut values n, n/p₁, … 1, and the runs with their exponents.</summary>
    public static (float[] weights, Color[] colors, string[] inner, string[] marks, (int first, int last, string label, Color color)[] runs) ForNumber(int n)
    {
        List<int> f = LabMath.Factorize(Mathf.Max(2, n));
        float[] w = new float[f.Count];
        Color[] c = new Color[f.Count];
        string[] inside = new string[f.Count], cuts = new string[f.Count + 1];
        List<(int, int, string, Color)> runs = new();
        int rest = n;
        cuts[0] = n.ToString();
        for (int i = 0; i < f.Count; i++)
        {
            w[i] = Mathf.Log(f[i]);
            c[i] = LabMath.PrimeColor(f[i]);
            inside[i] = f[i].ToString();
            rest /= f[i];
            cuts[i + 1] = rest.ToString();
        }

        for (int i = 0; i < f.Count;)
        {
            int j = i;
            while (j + 1 < f.Count && f[j + 1] == f[i]) j++;
            int e = j - i + 1;
            runs.Add((i, j, e == 1 ? f[i].ToString() : f[i] + LabMath.Superscript(e), c[i]));
            i = j + 1;
        }

        return (w, c, inside, cuts, runs.ToArray());
    }

    public void Clear()
    {
        foreach (Transform t in root) UnityEngine.Object.Destroy(t.gameObject);
        pieces.Clear();
        colors.Clear();
        inner.Clear();
        marks.Clear();
        groups.Clear();
    }

    public void Show(int n, float barWidth)
    {
        var s = ForNumber(n);
        Show(s.weights, s.colors, s.inner, s.marks, s.runs, barWidth);
    }

    public void Show(float[] weights, Color[] cols, string[] insideLabels, string[] cuts, (int first, int last, string label, Color color)[] runs, float barWidth)
    {
        Clear();
        float total = 0f;
        foreach (float w in weights) total += w;
        float X(float sum) => -barWidth * .5f + barWidth * sum / Mathf.Max(1e-6f, total);
        float barY = 8f * k, barH = 46f * k, gap = 3f * k;
        RectTransform Box(Transform parent, string name, float x0, float x1, float y, float h)
        {
            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            RectTransform r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
            r.pivot = new Vector2(.5f, .5f);
            r.anchoredPosition = new Vector2((x0 + x1) * .5f, y);
            r.sizeDelta = new Vector2(Mathf.Max(k, x1 - x0), h);
            return r;
        }

        TextMeshProUGUI Label(Transform parent, string text, float x, float y, float size, Color color)
        {
            TextMeshProUGUI t = makeText(parent, text, size);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.alignment = TextAlignmentOptions.Center;
            t.fontStyle = FontStyles.Bold;
            RectTransform r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
            r.pivot = new Vector2(.5f, .5f);
            r.sizeDelta = new Vector2(260f * k, size * 1.5f);
            r.anchoredPosition = new Vector2(x, y);
            t.color = color;
            return t;
        }

        float acc = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            float a = X(acc), b = X(acc + weights[i]);
            Image piece = Box(root, $"piece {i}", a + gap, b - gap, barY, barH).gameObject.AddComponent<Image>();
            piece.raycastTarget = false;
            if (ImageMaterial != null) piece.material = ImageMaterial;
            pieces.Add(piece);
            colors.Add(cols[i]);
            inner.Add(Label(root, insideLabels != null && i < insideLabels.Length ? insideLabels[i] : "", (a + b) * .5f, barY, 32f * k, Color.black));
            acc += weights[i];
        }

        acc = 0f;
        for (int i = 0; i < cuts.Length; i++)
        {
            marks.Add(Label(root, cuts[i], X(acc), barY + 60f * k, 36f * k, Color.white));
            if (i < weights.Length) acc += weights[i];
        }

        if (runs != null)
            foreach (var (first, last, label, color) in runs)
            {
                float a = 0f, b = 0f;
                for (int i = 0; i <= last; i++)
                {
                    if (i < first) a += weights[i];
                    b += weights[i];
                }

                RectTransform br = Box(root, $"bracket {first}", X(a) + gap, X(b) - gap, barY - 38f * k, 6f * k);
                Image line = br.gameObject.AddComponent<Image>();
                line.color = color;
                line.raycastTarget = false;
                if (ImageMaterial != null) line.material = ImageMaterial;
                Label(br, label, 0f, -34f * k, 46f * k, color);
                groups.Add((br.gameObject, last));
            }

        Light(0);
    }

    /// <summary>Light the first <paramref name="lit"/> pieces; show the cut values up to the last lit cut and the brackets of the finished runs.</summary>
    public void Light(int lit)
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i] == null) continue;
            Color c = colors[i];
            bool on = i < lit;
            pieces[i].color = on ? c : new Color(c.r * .5f, c.g * .5f, c.b * .5f, .5f);
            inner[i].color = on ? new Color(.04f, .04f, .07f) : new Color(.85f, .86f, .9f, .6f);
        }

        for (int i = 0; i < marks.Count; i++)
            if (marks[i] != null) marks[i].gameObject.SetActive(i <= lit);
        foreach (var (go, last) in groups)
            if (go != null) go.SetActive(lit > last);
    }
}
