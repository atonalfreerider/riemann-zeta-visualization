using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// A number's fingerprint in counting marbles, with no symbols needed. Read left to right along a string, like the digits
/// of a written number:
/// * a badge marble in the number's own colour (gold prime, deep red p², teal pronic, grey composite);
/// * one gold container per prime factor p, holding p marbles (2 … 7 in one row, larger primes in rows of five);
/// * the exponent k as k small pale marbles strung in a raised row at the container's top right, where an exponent sits.
/// Unpack plays the multiplication: each exponent opens into k copies of its container, then the containers multiply a
/// block one by one, each a new direction (right, up, deep, then right again a level larger), until the block holds all
/// n marbles: the number's full volume, in its own colour. Pack plays it back down to the shorthand.
/// </summary>
public class MarbleFingerprint : MonoBehaviour
{
    public long N { get; private set; }
    /// <summary>0: the shorthand; 1: every exponent opened; 1 + j: the first j + 1 factors multiplied; Steps: the volume.</summary>
    public float Phase { get; private set; }
    public int Steps => factors.Count + 1;
    public float StepSeconds = .9f;
    /// <summary>Digits under the containers and on the volume (off: the marbles alone carry the number).</summary>
    public bool Digits = true;

    public static readonly Color ExponentColor = new(.7f, .9f, 1f);
    /// <summary>The shorthand's extent in marbles, from the badge's left edge: (width, height).</summary>
    public Vector2 ShorthandSize { get; private set; }
    /// <summary>The marbles as drawn this frame, in the fingerprint's own space (marble units).</summary>
    public Bounds LocalBounds { get; private set; }
    Vector3 lo, hi;

    void Put(Vector3 p, float d, Color c, float glow = 1f)
    {
        marbles.Add(p, d, c, glow);
        if (d > .01f) Mark(p, d);
    }

    void Mark(Vector3 p, float d)
    {
        lo = Vector3.Min(lo, p - Vector3.one * (d * .5f));
        hi = Vector3.Max(hi, p + Vector3.one * (d * .5f));
    }
    const float Gap = 1.25f, Pitch = 1.12f;

    float target;
    MarbleBatch marbles;                                 // made in Awake: Unity allows no render state in field initializers
    readonly List<long> factors = new();                 // the primes with multiplicity, ascending
    readonly List<(long p, int k)> groups = new();
    readonly List<Vector3[]> shortCups = new(), longCups = new();
    readonly List<LineRenderer> cupLines = new();
    LineRenderer stringLine;
    readonly List<TextBox> labels = new();
    TextBox volumeLabel;
    Vector3 badge;
    float[] stepOf;                                      // the volume: step of each factor along its axis
    int[] axisOf;
    Vector3 volumeOrigin;

    /// <summary>A fingerprint of n under parent, marbles `scale` world units across, facing the parent's −z.</summary>
    public static MarbleFingerprint Create(Transform parent, long n, float scale, bool digits = true)
    {
        GameObject go = new($"fingerprint {n}");
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one * scale;
        MarbleFingerprint f = go.AddComponent<MarbleFingerprint>();
        f.Digits = digits;
        f.Build(n);
        return f;
    }

    public void Unpack() => target = Steps;
    public void Pack() => target = 0f;
    public void Toggle() => target = target > 0f ? 0f : Steps;
    public void Jump(float phase) { Phase = target = Mathf.Clamp(phase, 0f, Steps); }
    public bool Moving => !Mathf.Approximately(Phase, target);

    // ---- layout -----------------------------------------------------------------------------------------------

    /// <summary>
    /// The marbles of a container of p, relative to its bottom-left: up to 7 in one row (a 7 never reads as 5 + 2),
    /// larger primes in rows of five from the bottom.
    /// </summary>
    static int RowOf(long p) => p <= 7 ? (int)p : 5;

    static Vector3 InCup(int i, long p) => new((i % RowOf(p)) * Pitch, (i / RowOf(p)) * Pitch, 0f);

    static Vector2 CupSize(long p) => new(Mathf.Min(RowOf(p), (int)p) * Pitch + .3f, Mathf.Ceil(p / (float)RowOf(p)) * Pitch + .5f);

    void Awake() => marbles ??= new MarbleBatch(2);

    void Build(long n)
    {
        marbles ??= new MarbleBatch(2);
        N = n;
        factors.Clear();
        groups.Clear();
        foreach ((long p, int k) in NumberKind.Factor(n))
        {
            groups.Add((p, k));
            for (int j = 0; j < k; j++) factors.Add(p);
        }

        if (n == 1) factors.Clear();
        badge = new Vector3(-1.6f, .55f, 0f);
        // the shorthand: one container per distinct prime, its exponent strung above its top right
        float x = .4f;
        foreach ((long p, int k) in groups)
        {
            Vector2 s = CupSize(p);
            shortCups.Add(new[] { new Vector3(x, 0f, 0f), new Vector3(s.x, s.y, 0f) });
            x += s.x + (k > 1 ? k * .62f + .3f : 0f) + Gap;
        }

        float tallest = 0f;
        foreach ((long p, int k) in groups) tallest = Mathf.Max(tallest, CupSize(p).y + (k > 1 ? .8f : 0f));
        ShorthandSize = new Vector2(x + 2.4f - Gap, Mathf.Max(2.2f, tallest + .4f));
        // the opened string: one container per factor
        x = .4f;
        foreach (long p in factors)
        {
            Vector2 s = CupSize(p);
            longCups.Add(new[] { new Vector3(x, 0f, 0f), new Vector3(s.x, s.y, 0f) });
            x += s.x + Gap;
        }

        // the volume: factor j repeats the block along axis j mod 3, one level larger each time round
        stepOf = new float[factors.Count];
        axisOf = new int[factors.Count];
        float[] extent = { 1f, 1f, 1f };
        for (int j = 0; j < factors.Count; j++)
        {
            int a = j % 3;
            int level = j / 3;
            float step = j < 3 ? Pitch : extent[a] + .55f + .35f * level;
            axisOf[j] = a;
            stepOf[j] = step;
            extent[a] = step * (factors[j] - 1) + extent[a];
        }

        float topOfCups = 0f;
        foreach (long p in factors) topOfCups = Mathf.Max(topOfCups, CupSize(p).y);
        volumeOrigin = new Vector3(.55f, topOfCups + 2.2f, 0f);
        BuildLines();
        Render();
    }

    void BuildLines()
    {
        Color gold = NumberKind.Gold;
        stringLine = Line("string", new Color(.75f, .78f, .9f, .5f), .07f);
        for (int i = 0; i < Mathf.Max(shortCups.Count, longCups.Count); i++)
            cupLines.Add(Line($"container {i}", gold, .09f));
        // digits: each container's prime under it, each exponent by its marbles, and the volume's count
        if (!Digits) return;
        for (int i = 0; i < Mathf.Max(groups.Count, factors.Count); i++)
        {
            labels.Add(Label("", 1.1f, gold));
            labels.Add(Label("", .8f, ExponentColor));
        }

        volumeLabel = Label(N.ToString(), 1.6f, NumberKind.ColorOf(N));
    }

    LineRenderer Line(string name, Color c, float w)
    {
        LineRenderer l = Factory.NewLine(name, w, c, Factory.SoftGlowIntensity);
        l.transform.SetParent(transform, false);
        l.useWorldSpace = false;
        l.positionCount = 0;
        return l;
    }

    TextBox Label(string text, float size, Color c)
    {
        // a label about `size` marbles tall (TextMeshPro's height is about a tenth of its font size)
        TextBox t = TextBox.Create(text, size * 10f);
        t.transform.SetParent(transform, false);
        t.Alignment = TextAlignmentOptions.Center;
        t.Color = c;
        t.TextField.textWrappingMode = TextWrappingModes.NoWrap;
        return t;
    }

    // ---- the picture at the current phase ---------------------------------------------------------------------

    void Update()
    {
        if (!Mathf.Approximately(Phase, target))
        {
            float speed = 1f / Mathf.Max(.05f, StepSeconds);
            Phase = Mathf.MoveTowards(Phase, target, speed * Time.deltaTime);
        }

        Render();
    }

    void LateUpdate() => marbles.Draw(transform.localToWorldMatrix);

    static float Ease(float u) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u));

    /// <summary>Where marble i of the volume sits when only the first `level` factors have multiplied.</summary>
    Vector3 VolumeAt(long i, int level)
    {
        Vector3 p = volumeOrigin;
        long rest = i;
        for (int j = 0; j < factors.Count; j++)
        {
            long d = rest % factors[j];
            rest /= factors[j];
            if (j >= level) break;
            Vector3 axis = axisOf[j] == 0 ? Vector3.right : axisOf[j] == 1 ? Vector3.up : Vector3.forward;
            p += axis * (d * stepOf[j]);
            // a hair's gap after every fifth marble of the first row, for counting
            if (j == 0 && factors[0] > 5) p += Vector3.right * ((d / 5) * .3f);
        }

        return p;
    }

    void Render()
    {
        marbles.Clear();
        lo = Vector3.one * 1e9f;
        hi = -lo;
        Color gold = NumberKind.Gold, own = NumberKind.ColorOf(N);
        float ph = Phase;
        float open = Ease(ph);                              // 0 → 1: the exponents open
        int m = factors.Count;
        // the badge: the whole number in its own colour
        Put(badge, 1.6f, own, 1.15f);
        if (N <= 1)
        {
            stringLine.positionCount = 0;
            LocalBounds = new Bounds((lo + hi) * .5f, hi - lo);
            return;
        }

        // the containers along the string
        float endX = 0f;
        int cup = 0, label = 0;
        if (ph < 1f || m == 0)
        {
            // shorthand → opened: copy c of group g is factor index (start of g) + c
            int fi = 0;
            for (int g = 0; g < groups.Count; g++)
            {
                (long p, int k) = groups[g];
                Vector3 from = shortCups[g][0];
                for (int c = 0; c < k; c++)
                {
                    Vector3 to = longCups[fi + c][0];
                    // copy 0 slides; copies 1 … k − 1 come out of exponent marble c
                    Vector3 expo = ExponentPos(g, c);
                    Vector3 o = c == 0 ? Vector3.Lerp(from, to, open) : Vector3.Lerp(expo, to, open);
                    float grow = c == 0 ? 1f : open;
                    for (int i = 0; i < p; i++)
                        Put(o + (InCup(i, p) + new Vector3(.15f + .5f, .45f, 0f)) * grow, grow, gold, 1.12f);
                    DrawCup(cup++, o, CupSize(p), grow, c == 0 && k > 1 ? 1f - open : 0f);
                    // the string ends at the last container shown (copies still folded in their exponent do not count)
                    if (grow > .05f) endX = Mathf.Max(endX, o.x + CupSize(p).x * grow);
                    if (c == 0) SetLabel(2 * g, $"{p}", o + new Vector3(CupSize(p).x * .5f, -.75f, 0f), 1f);
                }

                // the exponent marbles, shrinking into the copies they open
                if (k > 1)
                    for (int c = 0; c < k; c++)
                    {
                        Vector3 e = Vector3.Lerp(ExponentPos(g, c), longCups[fi + c][0] + new Vector3(CupSize(p).x * .5f, CupSize(p).y + .3f, 0f), open);
                        Put(e, .56f * (1f - open), ExponentColor, 1.15f);
                    }

                // each group's prime in slot 2g (gold), its exponent in slot 2g + 1 (pale), empty when it is 1
                SetLabel(2 * g + 1, k > 1 ? $"{k}" : "", ExponentPos(g, k - 1) + new Vector3(.75f, .35f, 0f), 1f - open);
                label = 2 * g + 2;
                fi += k;
            }
        }
        else
        {
            // the opened string, the containers handing their marbles to the block one by one
            float lv = ph - 1f;                             // 0 … m: factors multiplied beyond the first
            for (int j = 0; j < m; j++)
            {
                Vector3 o = longCups[j][0];
                // container 0 becomes the block's first row; container j (≥ 1) is spent as level j opens
                float spent = j == 0 ? Ease(lv * 2f) : Ease(lv - (j - 1));
                float keep = 1f - spent;
                if (j > 0)
                    for (int i = 0; i < factors[j]; i++)
                        Put(o + InCup(i, factors[j]) + new Vector3(.65f, .45f, 0f), keep, gold, 1.12f);
                DrawCup(cup++, o, CupSize(factors[j]), Mathf.Max(.25f, keep));
                endX = Mathf.Max(endX, o.x + CupSize(factors[j]).x);
                // the opened string uses the prime labels only (the even slots; the odd ones, the exponents', stay empty)
                SetLabel(2 * j, $"{factors[j]}", o + new Vector3(CupSize(factors[j]).x * .5f, -.75f, 0f), Mathf.Max(.3f, keep));
                SetLabel(2 * j + 1, "", Vector3.zero, 0f);
                label = 2 * j + 2;
            }

            // the block: marble i is born when its last non-zero digit's level opens, out of its parent
            long total = 1;
            foreach (long p in factors) total *= p;
            int whole = Mathf.Clamp(Mathf.FloorToInt(lv), 0, m);
            float frac = Mathf.Clamp01(lv - whole);
            Color c = Color.Lerp(gold, own, Ease((lv - (m - 1)) * 1f));
            for (long i = 0; i < total; i++)
            {
                // its highest non-zero digit
                long rest = i;
                int top = -1;
                for (int j = 0; j < m; j++)
                {
                    if (rest % factors[j] != 0) top = j;
                    rest /= factors[j];
                }

                Vector3 p;
                if (top <= 0)
                {
                    // the first row, out of container 0
                    Vector3 cupPos = longCups[0][0] + InCup((int)i, factors[0]) + new Vector3(.65f, .45f, 0f);
                    p = Vector3.Lerp(cupPos, VolumeAt(i, m), Ease(lv * 2f));
                    Put(p, 1f, c, 1.12f);
                    continue;
                }

                if (top > whole) continue;                   // not born yet
                float u = top < whole ? 1f : Ease(frac);
                Vector3 parent = VolumeAt(i % Prefix(top), m), own_ = VolumeAt(i, m);
                p = Vector3.Lerp(parent, own_, u);
                Put(p, top < whole ? 1f : Mathf.Lerp(.6f, 1f, u), c, 1.12f);
            }

            if (volumeLabel != null)
            {
                // left of the block's first marble, clear of it
                volumeLabel.transform.localPosition = volumeOrigin + new Vector3(-.75f - .55f * N.ToString().Length, .2f, 0f);
                volumeLabel.TextField.alpha = Ease((lv - (m - 1)) * 1.5f);
            }
        }

        for (int i = cup; i < cupLines.Count; i++) cupLines[i].positionCount = 0;
        for (int i = label; i < labels.Count; i++) SetLabel(i, "", Vector3.zero, 0f);
        if (volumeLabel != null && ph < 1f) volumeLabel.TextField.alpha = 0f;
        stringLine.positionCount = 2;
        stringLine.SetPositions(new[] { new Vector3(badge.x + .8f, 0f, 0f), new Vector3(endX + .4f, 0f, 0f) });
        Mark(new Vector3(endX + .4f, 0f, 0f), .1f);
        LocalBounds = new Bounds((lo + hi) * .5f, hi - lo);
    }

    long Prefix(int j)
    {
        long q = 1;
        for (int i = 0; i < j; i++) q *= factors[i];
        return q;
    }

    Vector3 ExponentPos(int g, int c)
    {
        Vector3 o = shortCups[g][0];
        Vector2 s = CupSize(groups[g].p);
        return o + new Vector3(s.x + .35f + c * .62f, s.y + .15f, 0f);
    }

    /// <summary>A container's outline; with an exponent (stem &gt; 0), its right wall rises to the exponent's string and hooks to it.</summary>
    void DrawCup(int i, Vector3 o, Vector2 s, float alpha, float stem = 0f)
    {
        if (i >= cupLines.Count) return;
        LineRenderer l = cupLines[i];
        float h = s.y, w = s.x;
        if (stem > .01f)
        {
            l.positionCount = 6;
            l.SetPositions(new[] { o + new Vector3(0f, h, 0f), o, o + new Vector3(w, 0f, 0f), o + new Vector3(w, h, 0f),
                o + new Vector3(w, h + .15f * stem, 0f), o + new Vector3(w + .14f * stem, h + .15f * stem, 0f) });
        }
        else
        {
            l.positionCount = 4;
            l.SetPositions(new[] { o + new Vector3(0f, h, 0f), o, o + new Vector3(w, 0f, 0f), o + new Vector3(w, h, 0f) });
        }
        Color c = NumberKind.Gold;
        l.startColor = l.endColor = new Color(c.r, c.g, c.b, alpha);
    }

    void SetLabel(int i, string text, Vector3 at, float alpha)
    {
        if (!Digits || i >= labels.Count) return;
        TextBox t = labels[i];
        if (t.TextField == null) return;
        t.TextField.text = text;
        t.transform.localPosition = at;
        t.TextField.alpha = alpha;
    }
}
