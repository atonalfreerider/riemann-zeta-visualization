using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// A number's fingerprint in counting marbles, with no symbols needed. Read left to right along a string, like the digits
/// of a written number:
/// * a badge marble in the number's own colour (gold prime, deep red p², blue pronic, grey composite);
/// * one round gold container per prime factor p, holding p marbles in a symmetric spiral (MarbleLayout): a pair, a
///   triangle, a pentagon, a centre and six round it, a centre and m arms …;
/// * the exponent k in the container itself, not beside it: k − 1 more rings round the container's centre, each in a plane
///   of its own, tilted out of the picture (one more dimension each), their tilts fanned by half a turn, like the rings
///   of an armillary sphere. Flat, each is an ellipse through the container, its front half bright and its back half
///   faint behind the marbles; the rings precess slowly, and from the fourth ring on they rock as if turning through a
///   further dimension. Count the rings, the container's own flat one included: that many times the prime is multiplied
///   in.
/// Unpack plays the multiplication: each container's rings slide out along the string, close into whole rings and fill
/// with marbles: k copies of the container; then the first container pours
/// its marbles into the volume, and every next container j sends one marble to each of p copies of the volume so far,
/// round a ring, and the p copies wind together into p spiral arms: copy c becomes arm c. The volume ends with all n
/// marbles in the number's own colour, its last factor's arms still shaded. Pack plays it back down to the shorthand.
/// Flat, the marbles are symmetric spirals; Deep, every group of marbles is a spiral sphere pack (the volume wound with
/// one coil per copy), turning slowly, and the exponent's rings stand out of the plane.
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
    float shortEnd, longEnd;
    /// <summary>Between a container's ring and its first exponent ring, and between exponent rings (marbles).</summary>
    public const float RingGap = .5f;

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

    /// <summary>
    /// A fingerprint of n under parent, marbles `scale` world units across, facing the parent's −z; beside: the volume
    /// unpacks to the right of the string (else above it).
    /// </summary>
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

    /// <summary>
    /// 0 … 1: the shorthand building itself, for the videos: the badge, then each container in turn (its ring, its marbles
    /// one by one, its exponent rings one by one). 1 (the default) shows it whole.
    /// </summary>
    public float Reveal = 1f;
    /// <summary>0 … 1: the whole fingerprint fades (marbles shrink, lines and digits fade).</summary>
    public float Fade = 1f;

    /// <summary>Set the phase and the depth from outside (a video's eased parameters), with no easing of its own.</summary>
    public void Drive(float phase, float depth)
    {
        Phase = target = Mathf.Clamp(phase, 0f, Steps);
        Depth = depthTarget = Mathf.Clamp01(depth);
        Render();
    }

    // ---- where its parts are, for callouts (world space) ----------------------------------------------------------

    public int GroupCount => groups.Count;
    public (long p, int k) Group(int g) => groups[Mathf.Clamp(g, 0, groups.Count - 1)];
    public Vector3 BadgeWorld => transform.TransformPoint(badge);

    /// <summary>Container g of the shorthand (or, once opened, its first copy on the long string): its top.</summary>
    public Vector3 CupTopWorld(int g)
    {
        g = Mathf.Clamp(g, 0, groups.Count - 1);
        float x = shortX[g];
        if (Phase >= 1f)
        {
            int fi = 0;
            for (int i = 0; i < g; i++) fi += groups[i].k;
            x = longX[fi];
        }

        return transform.TransformPoint(new Vector3(x, CupNow(groups[g].p) + .1f, 0f));
    }

    /// <summary>The top of container g's outermost exponent ring (its own ring when it has none).</summary>
    public Vector3 RingTopWorld(int g)
    {
        g = Mathf.Clamp(g, 0, groups.Count - 1);
        (long p, int k) = groups[g];
        return transform.TransformPoint(new Vector3(shortX[g], OuterRadius(p, k) - CupRadius(p) + CupNow(p) + .1f, 0f));
    }

    /// <summary>Container j of the opened string (one per factor): its top.</summary>
    public Vector3 LongTopWorld(int j)
    {
        j = Mathf.Clamp(j, 0, Mathf.Max(0, factors.Count - 1));
        return factors.Count == 0 ? BadgeWorld : transform.TransformPoint(new Vector3(longX[j], CupNow(factors[j]) + .1f, 0f));
    }

    /// <summary>The volume's centre, and the top of its rim as drawn now.</summary>
    public Vector3 VolumeWorld => transform.TransformPoint(volumeAt);
    public Vector3 VolumeTopWorld
    {
        get
        {
            int m = factors.Count;
            float r = m == 0 || volumeTooBig ? 1f : Mathf.Lerp(MarbleLayout.Radius(flatOf[m]), MarbleLayout.Radius(deepOf[m]), D) * VolumeScale;
            return transform.TransformPoint(volumeAt + new Vector3(0f, r + .2f, 0f));
        }
    }

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

    /// <summary>Exponent ring i (1 … k − 1) of a container of p: one ring gap further out per ring.</summary>
    static float RingRadius(long p, int i) => CupRadius(p) + i * RingGap;

    /// <summary>A container with its exponent's rings: how far its outermost ring reaches.</summary>
    static float OuterRadius(long p, int k) => RingRadius(p, k - 1);

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
        // the shorthand: one container per distinct prime, wearing its exponent's rings
        float x = badge.x + .8f + .5f, tallest = 1.6f;
        foreach ((long p, int k) in groups)
        {
            float r = OuterRadius(p, k);
            float cx = x + r;
            shortX.Add(cx);
            x = cx + r + (k > 1 && Digits ? .7f : 0f);
            tallest = Mathf.Max(tallest, 2f * r);
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
        d *= Fade;
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
        lines.Alpha = Fade;
        lo = Vector3.one * 1e9f;
        hi = -lo;
        Color gold = NumberKind.Gold, own = NumberKind.ColorOf(N);
        float ph = Phase;
        int m = factors.Count;
        float rv = ph < 1f ? Mathf.Clamp01(Reveal) : 1f;
        Put(badge, 1.6f * Ease(rv / .08f), own, 1.15f);
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
            int groupCount = groups.Count;
            float stringTo = badge.x + .8f;
            for (int g = 0; g < groups.Count; g++)
            {
                (long p, int k) = groups[g];
                Vector2[] cf = ContainerFlat(p);
                Vector3[] cd = ContainerDeep(p);
                float rNow = CupNow(p);
                Vector3 from = new(shortX[g], 0f, 0f);
                // the build-up: this container's share of the reveal, its ring first, then its marbles, then its rings
                float v = Mathf.Clamp01((rv - .08f - .92f * g / groupCount) / (.92f / groupCount));
                float cupIn = Ease(v / .25f);
                if (cupIn > 0f) stringTo = Mathf.Max(stringTo, Mathf.Lerp(from.x - rNow, from.x + OuterRadius(p, k) - CupRadius(p) + rNow, cupIn));
                for (int c = 0; c < k; c++)
                {
                    Vector3 to = new(longX[fi + c], 0f, 0f);
                    // copy 0 is the container itself; copy c (≥ 1) is its c-th ring, which slides out along the string,
                    // closes, shrinks onto the container's size and fills with p marbles
                    Vector3 o = Vector3.Lerp(from, to, open);
                    float grow = c == 0 ? 1f : open;
                    for (int i = 0; i < p; i++)
                    {
                        float pop = c == 0 ? Ease((v - .25f - .5f * i / p) / Mathf.Max(.04f, .5f / p)) : 1f;
                        Put(o + Blend(cf[i], cd[i]) * grow, grow * pop, gold);
                    }

                    if (c == 0) Cup(o, rNow * (.6f + .4f * cupIn), cupIn);
                    else
                    {
                        float ringIn = Ease((v - .75f - .25f * (c - 1) / Mathf.Max(1, k - 1)) / Mathf.Max(.06f, .25f / Mathf.Max(1, k - 1)));
                        ExponentRing(o, Mathf.Lerp(RingRadius(p, c) - CupRadius(p) + rNow, rNow, open) * (.7f + .3f * ringIn), c, k, open, ringIn);
                    }

                    if (c == 0) SetLabel(label, $"{p}", o + new Vector3(0f, -rNow - .75f, 0f), cupIn);
                }

                label++;
                if (k > 1)
                {
                    // the exponent's digit, by its outermost ring
                    float ro = OuterRadius(p, k) - CupRadius(p) + rNow;
                    SetLabel(label, $"{k}", from + new Vector3(Mathf.Cos(.6f), Mathf.Sin(.6f), 0f) * (ro + .7f), (1f - open) * Ease((v - .9f) / .1f));
                }
                else SetLabel(label, "", Vector3.zero, 0f);

                label++;
                fi += k;
            }

            if (volumeLabel != null) volumeLabel.TextField.alpha = 0f;
            if (rv < 1f) endX = Mathf.Min(endX, stringTo - .3f);
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
                float vr = Mathf.Lerp(MarbleLayout.Radius(flatOf[m]), MarbleLayout.Radius(deepOf[m]), D);
                volumeLabel.transform.localPosition = beside
                    ? volumeAt + new Vector3(0f, vr * VolumeScale + 1.3f, 0f)
                    : volumeAt + new Vector3(-vr * VolumeScale - .9f - .5f * N.ToString().Length, 0f, 0f);
                volumeLabel.TextField.alpha = Ease((lv - (m - 1) - .5f) * 2f) * Fade;
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

    // ---- the exponent: tilted rings round the container, one more dimension each -----------------------------------

    /// <summary>
    /// Exponent ring i (1 … k − 1) of a container at o, radius r: a circle about the container's centre in a plane of its
    /// own, tilted out of the picture, the tilt axes fanned by half a turn over the rings, like the rings of an armillary
    /// sphere. Seen flat it is an ellipse through the container, its front half bright and its back half faint (behind
    /// the marbles). The rings precess slowly; from ring 3 on the tilt also rocks, as a ring turning through a further
    /// dimension would look from here. close (0 … 1) is the unpacking: the ring settles flat into the picture, a whole
    /// container ring.
    /// </summary>
    void ExponentRing(Vector3 o, float r, int i, int k, float close, float fade = 1f)
    {
        if (fade <= .01f) return;
        float open = 1f - close;
        float tilt = 62f;
        if (i >= 3) tilt += 16f * Mathf.Sin(SpinAngle * Mathf.Deg2Rad * 1.6f + i * 1.7f);
        tilt *= open;
        float axis = 90f + 180f * (i - 1) / Mathf.Max(1, k - 1) + SpinAngle * (i % 2 == 0 ? .5f : -.5f);
        Quaternion q = Quaternion.AngleAxis(axis, Vector3.forward) * Quaternion.AngleAxis(tilt, Vector3.right);
        Color g = NumberKind.Gold;
        const int segs = 72;
        Vector3 prev = Vector3.zero;
        float prevZ = 0f;
        for (int s = 0; s <= segs; s++)
        {
            float a = Mathf.PI * 2f * s / segs;
            Vector3 rel = q * new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
            Vector3 pt = o + rel;
            if (s > 0)
            {
                // the near half (toward the viewer, −z) bright, the far half faint; settled flat, all of it bright
                float near = Mathf.Clamp01(.5f - (rel.z + prevZ) * .5f / Mathf.Max(.01f, r * .2f));
                float alpha = Mathf.Lerp(Mathf.Lerp(.28f, .95f, near), 1f, close) * fade;
                float width = Mathf.Lerp(Mathf.Lerp(.05f, .085f, near), .09f, close);
                lines.Add(prev, pt, width, new Color(g.r, g.g, g.b, alpha));
            }

            prev = pt;
            prevZ = rel.z;
        }

        Mark(o, 2f * r);
    }

    void SetLabel(int i, string text, Vector3 at, float alpha)
    {
        if (!Digits || i >= labels.Count) return;
        TextBox t = labels[i];
        if (t.TextField == null) return;
        t.TextField.text = text;
        t.transform.localPosition = at;
        t.TextField.alpha = alpha * Fade;
    }
}
