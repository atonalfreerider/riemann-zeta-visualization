using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// A number's fingerprint in counting marbles, with no symbols needed. Read left to right along a string, like the digits
/// of a written number:
/// * a badge marble in the number's own colour (gold prime, deep red p², blue pronic, grey composite);
/// * one round gold container per prime factor p, holding p marbles in a symmetric spiral (MarbleLayout): a pair, a
///   triangle, a pentagon, a centre and six round it, a centre and m arms …;
/// * the exponent k at the container's top right, as a shape of k dimensions: the shadow of a k-dimensional cube (a square
///   for p², a cube for p³, a tesseract for p⁴, …) with k pale marbles evenly round it, one per dimension.
/// Unpack plays the multiplication: each exponent opens into k copies of its container; then the first container pours
/// its marbles into the volume, and every next container j sends one marble to each of p copies of the volume so far,
/// round a ring, and the p copies wind together into p spiral arms: copy c becomes arm c. The volume ends with all n
/// marbles in the number's own colour, its last factor's arms still shaded. Pack plays it back down to the shorthand.
/// Flat, the marbles are symmetric spirals; Deep, every group of marbles is a spiral sphere pack (the volume wound with
/// one coil per copy), turning slowly, and the exponent's cube turns in depth.
/// </summary>
public class MarbleFingerprint : MonoBehaviour
{
    public long N { get; private set; }
    /// <summary>0: the shorthand; 1: every exponent opened; 1 + j: the first j + 1 factors multiplied; Steps: the volume.</summary>
    public float Phase { get; private set; }
    public int Steps => factors.Count + 1;
    public float StepSeconds = 1.25f;
    /// <summary>Digits under the containers and by the volume (off: the marbles alone carry the number).</summary>
    public bool Digits = true;
    /// <summary>0 flat (symmetric spirals) … 1 deep (spiral sphere packs); eases toward the Deep setting.</summary>
    public float Depth { get; private set; }
    public bool Deep { get => depthTarget > .5f; set => depthTarget = value ? 1f : 0f; }
    /// <summary>How far the deep packs have turned (degrees), and how fast they turn.</summary>
    public float SpinAngle;
    public float SpinDegreesPerSecond = 14f;

    public static readonly Color ExponentColor = new(.7f, .9f, 1f);
    /// <summary>The shorthand's extent in marbles, from the badge's left edge: (width, height).</summary>
    public Vector2 ShorthandSize { get; private set; }
    /// <summary>The marbles and lines as drawn this frame, in the fingerprint's own space (marble units).</summary>
    public Bounds LocalBounds { get; private set; }

    const float StringGap = 1.1f;                        // between one container (or exponent) and the next
    const float Rim = .45f;                              // from the outermost marble's edge to its container's ring
    const int SymmetricUpTo = 13;                        // p copies wind into p arms up to here; past it, into the volume's own arms

    float target, depthTarget;
    MarbleBatch marbles;                                 // made in Awake: Unity allows no render state in field initializers
    LineBatch lines;
    readonly List<long> factors = new();                 // the primes with multiplicity, ascending
    readonly List<(long p, int k)> groups = new();
    readonly List<TextBox> labels = new();
    TextBox volumeLabel;
    Vector3 lo, hi;

    // the string
    Vector3 badge;
    readonly List<float> shortX = new(), longX = new();  // container centres along the string (y = 0)
    readonly List<Vector3> glyphAt = new();              // each group's exponent: the corner its cube stands on
    readonly List<float> glyphL = new();
    float shortEnd, longEnd;

    // the volume: D_j holds the first j factors' product, D_1 the first container itself
    Vector3 volumeAt;
    float flatReach, deepReach;                          // the widest stage of the volume, flat and deep (marbles)
    bool beside;                                         // the volume to the right of the string, not above it
    /// <summary>The volume is kept within this many string marbles of its centre: past it, its marbles are drawn smaller.</summary>
    public const float VolumeCap = 9f;
    readonly List<int> count = new();                    // count[j] = P_j (count[0] = 1)
    readonly List<Vector2[]> flatOf = new();             // flatOf[j]: D_j flat
    readonly List<float[]> angleOf = new();              // angleOf[j]: D_j's flat angles, unwrapped along the arms
    readonly List<Vector3[]> deepOf = new();             // deepOf[j]: D_j deep
    readonly List<int> armsOf = new();                   // armsOf[j]: the arms D_j is shaded by (1: none)
    readonly List<Vector3[]> copyFlat = new(), copyDeep = new(); // for level j ≥ 1: where D_j's p_j copies sit
    bool volumeTooBig;

    /// <summary>A fingerprint of n under parent, marbles `scale` world units across, facing the parent's −z.</summary>
    /// <summary>beside: the volume unpacks to the right of the string (else above it).</summary>
    public static MarbleFingerprint Create(Transform parent, long n, float scale, bool digits = true, bool deep = false, bool beside = false)
    {
        GameObject go = new($"fingerprint {n}");
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one * scale;
        MarbleFingerprint f = go.AddComponent<MarbleFingerprint>();
        f.Digits = digits;
        f.beside = beside;
        f.depthTarget = f.Depth = deep ? 1f : 0f;
        f.Build(n);
        return f;
    }

    public void Unpack() => target = Steps;
    public void Pack() => target = 0f;
    public void Toggle() => target = target > 0f ? 0f : Steps;
    public void Jump(float phase) { Phase = target = Mathf.Clamp(phase, 0f, Steps); Render(); }
    /// <summary>Flat or deep at once (no easing), for pictures.</summary>
    public void JumpDepth(bool deep) { depthTarget = Depth = deep ? 1f : 0f; Render(); }
    public bool Moving => !Mathf.Approximately(Phase, target) || !Mathf.Approximately(Depth, depthTarget);

    void Awake()
    {
        marbles ??= new MarbleBatch(2);
        lines ??= new LineBatch();
    }

    // ---- layout -----------------------------------------------------------------------------------------------

    static Vector2[] ContainerFlat(long p)
    {
        (bool c, int m) = MarbleLayout.ShapeOf(p);
        return MarbleLayout.Flat((int)p, m, c);
    }

    static Vector3[] ContainerDeep(long p) => MarbleLayout.Deep((int)p, 1);

    /// <summary>A container's ring: round the flat spiral and the deep pack alike.</summary>
    static float CupRadius(long p) => Mathf.Max(MarbleLayout.Radius(ContainerFlat(p)), MarbleLayout.Radius(ContainerDeep(p))) + Rim;

    /// <summary>A container's ring as drawn: round the flat spiral, closing in on the deep pack.</summary>
    float CupNow(long p) => Mathf.Lerp(MarbleLayout.Radius(ContainerFlat(p)), MarbleLayout.Radius(ContainerDeep(p)), D) + Rim;

    /// <summary>The exponent's edge length: its k marbles (alternate corners of the 2k-gon) clear of each other.</summary>
    static float GlyphEdge(int k) => k <= 2 ? 1f : k == 3 ? .85f : k == 4 ? .72f : .6f;

    /// <summary>The 2k-gon's circumradius (its centre sits that far above the corner it stands on).</summary>
    static float GlyphRadius(int k) => GlyphEdge(k) / (2f * Mathf.Sin(Mathf.PI / (2f * k)));

    void Build(long n)
    {
        marbles ??= new MarbleBatch(2);
        lines ??= new LineBatch();
        N = n;
        factors.Clear();
        groups.Clear();
        foreach ((long p, int k) in NumberKind.Factor(n))
        {
            groups.Add((p, k));
            for (int j = 0; j < k; j++) factors.Add(p);
        }

        badge = new Vector3(-.2f, 0f, 0f);
        // the shorthand: one container per distinct prime, its exponent's cube at its top right
        float x = badge.x + .8f + .5f, tallest = 1.6f;
        foreach ((long p, int k) in groups)
        {
            float r = CupRadius(p);
            float cx = x + r;
            shortX.Add(cx);
            x = cx + r;
            tallest = Mathf.Max(tallest, 2f * r);
            if (k > 1)
            {
                float gr = GlyphRadius(k);
                // its centre out along 55° from the container's centre, just clear of the ring, so it sits over its own
                // container's top right (not between two containers); it stands on its bottom corner
                Vector3 centre = new Vector3(cx, 0f, 0f) + new Vector3(Mathf.Cos(.96f), Mathf.Sin(.96f), 0f) * (r + gr + .25f);
                glyphAt.Add(centre - new Vector3(0f, gr, 0f));
                glyphL.Add(GlyphEdge(k));
                // and the next container keeps its distance
                x = Mathf.Max(x, centre.x + gr + (Digits ? .9f : .2f)) + .5f;
                tallest = Mathf.Max(tallest, r + centre.y + gr + .4f);
            }
            else
            {
                glyphAt.Add(Vector3.zero);
                glyphL.Add(0f);
            }

            x += StringGap;
        }

        shortEnd = x - StringGap;
        ShorthandSize = new Vector2(shortEnd - (badge.x - .8f), tallest);
        // the opened string: one container per factor
        x = badge.x + .8f + .5f;
        float topOfCups = 1f;
        foreach (long p in factors)
        {
            float r = CupRadius(p);
            longX.Add(x + r);
            x += 2f * r + StringGap;
            topOfCups = Mathf.Max(topOfCups, r);
        }

        longEnd = x - StringGap;
        BuildVolume();
        // the volume above the opened string (or beside it), clear of its tallest container; the copies' ring, while it
        // lasts, may reach past that
        flatReach = deepReach = 1f;
        for (int j = 1; j < count.Count; j++)
        {
            flatReach = Mathf.Max(flatReach, MarbleLayout.Radius(flatOf[j]));
            deepReach = Mathf.Max(deepReach, MarbleLayout.Radius(deepOf[j]));
        }

        float reach = flatReach * Mathf.Min(1f, VolumeCap / flatReach);
        volumeAt = beside
            ? new Vector3(longEnd + 1.6f + reach, 0f, 0f)
            : new Vector3((badge.x + Mathf.Max(longEnd, shortEnd)) * .5f, topOfCups + (Digits ? 1.6f : 1f) + reach, 0f);
        BuildLabels();
        Render();
    }

    void BuildVolume()
    {
        count.Clear(); flatOf.Clear(); angleOf.Clear(); deepOf.Clear(); armsOf.Clear(); copyFlat.Clear(); copyDeep.Clear();
        count.Add(1); flatOf.Add(new Vector2[0]); angleOf.Add(new float[0]); deepOf.Add(new Vector3[0]); armsOf.Add(1);
        copyFlat.Add(null); copyDeep.Add(null);
        long total = 1;
        foreach (long p in factors) total *= p;
        volumeTooBig = total > 40000;
        if (factors.Count == 0 || volumeTooBig) return;
        // D_1: the first container, as it is
        count.Add((int)factors[0]);
        flatOf.Add(ContainerFlat(factors[0]));
        (bool c0, int m0) = MarbleLayout.ShapeOf(factors[0]);
        angleOf.Add(MarbleLayout.FlatAngles((int)factors[0], m0, c0));
        deepOf.Add(ContainerDeep(factors[0]));
        armsOf.Add(1);
        copyFlat.Add(null); copyDeep.Add(null);
        for (int j = 1; j < factors.Count; j++)
        {
            int p = (int)factors[j], before = count[j], after = before * p;
            float rf = MarbleLayout.Radius(flatOf[j]), rd = MarbleLayout.Radius(deepOf[j]);
            copyFlat[j] = CopySpots(p, rf);
            copyDeep[j] = CopySpots(p, rd);
            // p copies wind into p arms (copy c → arm c); past thirteen the volume takes arms of its own
            int m = p <= SymmetricUpTo ? p : MarbleLayout.ArmsFor(after);
            count.Add(after);
            flatOf.Add(MarbleLayout.Flat(after, m, false, true));
            angleOf.Add(MarbleLayout.FlatAngles(after, m, false, true));
            deepOf.Add(MarbleLayout.Deep(after, m));
            armsOf.Add(m);
            copyFlat.Add(null); copyDeep.Add(null);
        }
    }

    /// <summary>Where p copies of a disc of radius r sit: a ring of p (copy 0 at the top), or past thirteen, p's own spiral.</summary>
    static Vector3[] CopySpots(int p, float r)
    {
        Vector3[] at = new Vector3[p];
        float pitch = 2f * r + .7f;
        if (p <= SymmetricUpTo)
        {
            float ring = p == 1 ? 0f : pitch * .5f / Mathf.Sin(Mathf.PI / p);
            for (int c = 0; c < p; c++)
            {
                float a = Mathf.PI / 2f + Mathf.PI * 2f * c / p;
                at[c] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * ring;
            }
        }
        else
        {
            Vector2[] f = ContainerFlat(p);
            for (int c = 0; c < p; c++) at[c] = (Vector3)(f[c] * (pitch / MarbleLayout.Gap));
        }

        return at;
    }

    void BuildLabels()
    {
        if (!Digits) return;
        for (int i = 0; i < Mathf.Max(groups.Count, factors.Count); i++)
        {
            labels.Add(Label("", 1.1f, NumberKind.Gold));
            labels.Add(Label("", .8f, ExponentColor));
        }

        volumeLabel = Label(N.ToString(), 1.6f, NumberKind.ColorOf(N));
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
            Phase = Mathf.MoveTowards(Phase, target, Time.deltaTime / Mathf.Max(.05f, StepSeconds));
        if (!Mathf.Approximately(Depth, depthTarget))
            Depth = Mathf.MoveTowards(Depth, depthTarget, Time.deltaTime / .9f);
        SpinAngle = (SpinAngle + SpinDegreesPerSecond * Time.deltaTime) % 360f;
        Render();
    }

    void LateUpdate()
    {
        Matrix4x4 m = transform.localToWorldMatrix;
        marbles.Draw(m);
        lines.Draw(m, gameObject.layer);
    }

    static float Ease(float u) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u));

    float D => Ease(Depth);

    /// <summary>A deep point turned by the spin about its own group's upright axis.</summary>
    Vector3 Turn(Vector3 p)
    {
        float a = SpinAngle * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
        return new Vector3(c * p.x + s * p.z, p.y, -s * p.x + c * p.z);
    }

    /// <summary>Marble i of a group: its flat spot or its deep one (turned), by the depth.</summary>
    Vector3 Blend(Vector2 flat, Vector3 deep) => Vector3.LerpUnclamped(flat, Turn(deep), D);

    /// <summary>Arm shading: alternate arms a little darker, so the spiral's arms show in one colour.</summary>
    static float Shade(int arm, int arms)
    {
        if (arms <= 1) return 1f;
        if (arms % 2 == 1 && arm == arms - 1) return .8f;
        return arm % 2 == 0 ? 1f : .6f;
    }

    static Color Shaded(Color c, float k) => new(c.r * k, c.g * k, c.b * k, c.a);

    void Put(Vector3 p, float d, Color c, float glow = 1.12f)
    {
        if (d <= .01f) return;
        marbles.Add(p, d, c, glow);
        Mark(p, d);
    }

    void Mark(Vector3 p, float d)
    {
        lo = Vector3.Min(lo, p - Vector3.one * (d * .5f));
        hi = Vector3.Max(hi, p + Vector3.one * (d * .5f));
    }

    void Cup(Vector3 centre, float r, float alpha)
    {
        if (alpha <= .01f) return;
        Color g = NumberKind.Gold;
        lines.Circle(centre, r, .09f, new Color(g.r, g.g, g.b, alpha), 56);
        Mark(centre, 2f * r);
    }

    void Render()
    {
        if (marbles == null) return;
        marbles.Clear();
        lines.Clear();
        lo = Vector3.one * 1e9f;
        hi = -lo;
        Color gold = NumberKind.Gold, own = NumberKind.ColorOf(N);
        float ph = Phase;
        int m = factors.Count;
        Put(badge, 1.6f, own, 1.15f);
        if (N <= 1)
        {
            LocalBounds = new Bounds((lo + hi) * .5f, hi - lo);
            return;
        }

        int label = 0;
        float endX;
        if (ph < 1f || m == 0)
        {
            // shorthand → opened: copy c of group g becomes long container (start of g) + c
            float open = Ease(ph);
            int fi = 0;
            endX = Mathf.Lerp(shortEnd, longEnd, open);
            for (int g = 0; g < groups.Count; g++)
            {
                (long p, int k) = groups[g];
                Vector2[] cf = ContainerFlat(p);
                Vector3[] cd = ContainerDeep(p);
                float r = CupRadius(p), rNow = CupNow(p);
                Vector3 from = new(shortX[g], 0f, 0f);
                Vector3[] tips = k > 1 ? GlyphTips(g) : null;
                for (int c = 0; c < k; c++)
                {
                    Vector3 to = new(longX[fi + c], 0f, 0f);
                    // copy 0 slides along the string; copy c (≥ 1) grows out of the exponent's marble c
                    Vector3 o = c == 0 ? Vector3.Lerp(from, to, open) : Vector3.Lerp(tips[c], to, open);
                    float grow = c == 0 ? 1f : open;
                    for (int i = 0; i < p; i++) Put(o + Blend(cf[i], cd[i]) * grow, grow, gold);
                    Cup(o, rNow * grow, grow);
                    if (c == 0) SetLabel(label, $"{p}", o + new Vector3(0f, -rNow - .75f, 0f), 1f);
                }

                label++;
                if (k > 1)
                {
                    // the exponent: its marbles fly into the copies they open, its cube fades
                    for (int c = 0; c < k; c++)
                    {
                        Vector3 into = new(longX[fi + c], 0f, 0f);
                        Put(Vector3.Lerp(tips[c], into, open), .5f * (1f - open), ExponentColor, 1.15f);
                    }

                    DrawGlyph(g, 1f - open);
                    float gr = GlyphRadius(k);
                    SetLabel(label, $"{k}", glyphAt[g] + new Vector3(gr + .75f, gr, 0f), 1f - open);
                }
                else SetLabel(label, "", Vector3.zero, 0f);

                label++;
                fi += k;
            }

            if (volumeLabel != null) volumeLabel.TextField.alpha = 0f;
        }
        else
        {
            endX = longEnd;
            float lv = ph - 1f;                             // 0 … m: level j multiplies by factor j
            // the opened string: container j is spent as level j opens (container 0 pours into the volume)
            for (int j = 0; j < m; j++)
            {
                long p = factors[j];
                Vector3 o = new(longX[j], 0f, 0f);
                float spent = j == 0 ? Ease(lv * 1.6f) : Ease((lv - j) * 2.8f);
                float r = CupNow(p);
                Cup(o, r, Mathf.Max(.25f, 1f - spent));
                if (j > 0 && spent < 1f && !volumeTooBig)
                {
                    // container j's marbles fly out, one to each copy of the volume, and bloom there
                    Vector2[] cf = ContainerFlat(p);
                    Vector3[] cd = ContainerDeep(p);
                    float u = Mathf.Clamp01(lv - j);
                    float fly = Ease(u / .3f), fade = 1f - Ease((u - .2f) / .2f);
                    Vector3[] spots = CopySpots(j);
                    float vs = VolumeScale;
                    for (int i = 0; i < p; i++)
                    {
                        Vector3 home = o + Blend(cf[i], cd[i]);
                        Put(Vector3.Lerp(home, volumeAt + spots[i] * vs, fly), fade * Mathf.Lerp(1f, vs, fly), gold);
                    }
                }
                else if (j > 0 && lv < j)
                {
                    Vector2[] cf = ContainerFlat(p);
                    Vector3[] cd = ContainerDeep(p);
                    for (int i = 0; i < p; i++) Put(o + Blend(cf[i], cd[i]), 1f, gold);
                }

                SetLabel(label++, $"{p}", o + new Vector3(0f, -r - .75f, 0f), Mathf.Max(.3f, 1f - spent));
                SetLabel(label++, "", Vector3.zero, 0f);
            }

            if (!volumeTooBig) RenderVolume(lv, gold, own);
            if (volumeLabel != null)
            {
                float rv = Mathf.Lerp(MarbleLayout.Radius(flatOf[m]), MarbleLayout.Radius(deepOf[m]), D);
                volumeLabel.transform.localPosition = beside
                    ? volumeAt + new Vector3(0f, rv * VolumeScale + 1.3f, 0f)
                    : volumeAt + new Vector3(-rv * VolumeScale - .9f - .5f * N.ToString().Length, 0f, 0f);
                volumeLabel.TextField.alpha = Ease((lv - (m - 1) - .5f) * 2f);
            }
        }

        for (int i = label; i < labels.Count; i++) SetLabel(i, "", Vector3.zero, 0f);
        // the string, from the badge to the last container, behind the marbles
        Color s = new(.75f, .78f, .9f, .5f);
        lines.Add(new Vector3(badge.x + .8f, 0f, .6f), new Vector3(endX + .3f, 0f, .6f), .07f, s);
        Mark(new Vector3(endX + .3f, 0f, 0f), .1f);
        LocalBounds = new Bounds((lo + hi) * .5f, hi - lo);
    }

    /// <summary>The volume's marbles relative to the string's: 1, or smaller for a volume wider than VolumeCap.</summary>
    float VolumeScale => Mathf.Min(1f, VolumeCap / Mathf.Max(1f, Mathf.Lerp(flatReach, deepReach, D)));

    Vector3[] CopySpots(int j)
    {
        Vector3[] f = copyFlat[j], d = copyDeep[j];
        Vector3[] at = new Vector3[f.Length];
        for (int c = 0; c < f.Length; c++) at[c] = Vector3.LerpUnclamped(f[c], d[c], D);
        return at;
    }

    /// <summary>
    /// The volume at level lv: D_1 pours in from container 0 (0 → 1); at level j, D_j moves to copy 0's spot while
    /// copies 1 … p − 1 bloom where container j's marbles landed (u 0 → .5), then the copies wind into the arms of D_j+1.
    /// </summary>
    void RenderVolume(float lv, Color gold, Color own)
    {
        int m = factors.Count;
        float vs = VolumeScale;
        Color c = Color.Lerp(gold, own, Ease((lv - (m - 1) - .5f) * 2f));
        if (lv < 1f)
        {
            // container 0 pours into the volume, keeping its spiral
            Vector3 o = new(longX[0], 0f, 0f);
            float u = Ease(lv);
            for (int i = 0; i < count[1]; i++)
            {
                Vector3 s = Blend(flatOf[1][i], deepOf[1][i]);
                Put(Vector3.Lerp(o + s, volumeAt + s * vs, u), Mathf.Lerp(1f, vs, u), m == 1 ? c : gold);
            }

            return;
        }

        int level = Mathf.Min(Mathf.FloorToInt(lv), m);
        if (level >= m)
        {
            int arms = armsOf[m];
            for (int i = 0; i < count[m]; i++) Put(volumeAt + Blend(flatOf[m][i], deepOf[m][i]) * vs, vs, Shaded(c, Shade(i % arms, arms)));
            return;
        }

        // level j: multiply D_j by p_j
        int jj = level, p = (int)factors[jj], before = count[jj];
        float w = lv - jj;
        float bloom = Ease((w - .15f) / .3f), gather = Ease((w - .5f) / .45f), toSpot = Ease(w / .45f);
        int armsBefore = armsOf[jj], armsAfter = armsOf[jj + 1];
        bool swirl = armsAfter == p;                     // copy c winds into arm c
        Vector3[] sf = copyFlat[jj], sd = copyDeep[jj];
        for (int cpy = 0; cpy < p; cpy++)
        {
            float grow = cpy == 0 ? 1f : bloom;
            if (grow <= .01f) continue;
            float toward = cpy == 0 ? toSpot : 1f;
            Vector3 homeF = sf[cpy] * toward, homeD = sd[cpy] * toward;
            float arm0 = Mathf.PI / 2f + Mathf.PI * 2f * cpy / p;
            for (int t = 0; t < before; t++)
            {
                int slot = t * p + cpy;
                // flat: from the copy round the ring to the arm, swirling the way the arm winds
                Vector3 startF = homeF + (Vector3)flatOf[jj][t] * grow;
                Vector3 endF = flatOf[jj + 1][slot];
                Vector3 flatAt;
                if (swirl && gather > 0f && gather < 1f)
                {
                    float r1 = startF.magnitude, r2 = endF.magnitude;
                    float a1 = Mathf.Atan2(startF.y, startF.x);
                    a1 += Mathf.Round((arm0 - a1) / (2f * Mathf.PI)) * 2f * Mathf.PI;
                    float a2 = angleOf[jj + 1][slot];
                    float r = Mathf.Lerp(r1, r2, gather), a = Mathf.Lerp(a1, a2, gather);
                    flatAt = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
                }
                else flatAt = Vector3.Lerp(startF, endF, gather);
                // deep: straight in
                Vector3 deepAt = Vector3.Lerp(homeD + Turn(deepOf[jj][t]) * grow, Turn(deepOf[jj + 1][slot]), gather);
                Vector3 at = volumeAt + Vector3.LerpUnclamped(flatAt, deepAt, D) * vs;
                // shading: D_j's own arms, then the copy's, then the merged arm (the copy's, when it winds into arm c)
                float k = Mathf.Lerp(Mathf.Lerp(Shade(t % armsBefore, armsBefore), Shade(cpy, p), Ease(w / .3f)),
                    Shade(slot % armsAfter, armsAfter), gather);
                Put(at, grow * vs, Shaded(c, k));
            }
        }
    }

    // ---- the exponent: a k-dimensional cube's shadow ------------------------------------------------------------

    /// <summary>The cube's vertex (mask b), relative to the corner it stands on, by the depth and the spin.</summary>
    Vector3 GlyphVertex(int g, int k, int b)
    {
        float L = glyphL[g], gr = GlyphRadius(k);
        Vector3 v = Vector3.zero;
        for (int j = 0; j < k; j++)
            if ((b >> j & 1) == 1) v += MarbleLayout.CubeEdge(k, j, L, 1f);
        // flat: the shadow (x, y); deep: the cube itself, turning about the shadow's centre (gr above the corner)
        Vector3 rel = v - new Vector3(0f, gr, 0f);
        return new Vector3(0f, gr, 0f) + Vector3.LerpUnclamped(new Vector3(rel.x, rel.y, 0f), Turn(rel), D);
    }

    /// <summary>The exponent's k marbles: alternate corners of its 2k-gon (one per dimension, evenly round it).</summary>
    Vector3[] GlyphTips(int g)
    {
        int k = groups[g].k;
        int[] ring = MarbleLayout.CubeOutline(k);
        Vector3[] tips = new Vector3[k];
        for (int c = 0; c < k; c++) tips[c] = glyphAt[g] + GlyphVertex(g, k, ring[2 * c + 1]) + new Vector3(0f, 0f, -.05f);
        return tips;
    }

    void DrawGlyph(int g, float alpha)
    {
        int k = groups[g].k;
        if (alpha <= .01f || k < 2) return;
        Vector3 o = glyphAt[g];
        Color e = ExponentColor;
        // its edges: all of them up to six dimensions (fainter the more there are); past that the ones at the bottom
        // corner and the top one, enough to show the shape
        float inner = k <= 3 ? .85f : k == 4 ? .6f : k <= 6 ? .38f : .3f;
        int all = (1 << k) - 1;
        for (int b = 0; b <= all; b++)
            for (int j = 0; j < k; j++)
            {
                if ((b >> j & 1) == 1) continue;
                int b2 = b | 1 << j;
                if (k > 6 && b != 0 && b2 != all) continue;
                lines.Add(o + GlyphVertex(g, k, b), o + GlyphVertex(g, k, b2), .05f, new Color(e.r, e.g, e.b, alpha * inner));
            }

        int[] ring = MarbleLayout.CubeOutline(k);
        List<Vector3> path = new();
        foreach (int b in ring) path.Add(o + GlyphVertex(g, k, b));
        lines.Path(path, .075f, new Color(e.r, e.g, e.b, alpha * .95f), true);
        foreach (Vector3 v in path) Mark(v, .2f);
        // a short stem from the container's ring to the corner the cube stands on
        float r = CupNow(groups[g].p);
        Vector3 cup = new(shortX[g], 0f, 0f);
        Vector3 toCorner = (o - cup).normalized;
        lines.Add(cup + toCorner * (r + .05f), o, .06f, new Color(e.r, e.g, e.b, alpha * .8f));
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
