using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The factor funnel of "The paths back", interactive: every n ≤ N on E8's wheel (hour n mod 30, radius s√n), its column
/// rising to h·log n (toward −Z) cut into one piece per prime factor, largest at the floor and smallest at the top, each as
/// tall as log p. The colours are the games' (NumberKind): every piece is gold, because the pieces are primes and gold always
/// means prime, so a prime's column is one gold piece and a composite's a stack of them; the top says what n is (gold prime,
/// deep red p², blue pronic, grey any other composite). Which prime a piece is shows in its length, log p (the picked column
/// is banded white at each joint), and in labels: ÷p at each drop of a path, p^k on the powers, ÷p beside the tree of one
/// prime, the prime inside each piece of the strip.
/// A picked n shows its path back: down the top piece (÷ its smallest prime p), across at that height to the top of n/p's
/// column, and so on to 1, one step at a time. The tree draws every number's first step at once (all primes, or one prime:
/// the hops of a prime p ≥ 7 multiply the hour by p⁻¹ mod 30, the same permutation of E8's exponent spokes for every prime
/// of p's class). The powers p^k show where runs of one prime go: a discrete spiral that climbs log p per step and turns
/// with the order of p mod 30. The layout morphs from the wheel's hours (angle 12° per n) to the Sacks spiral (angle τ√n,
/// τ = 2π one turn) without moving any column off its radius.
/// </summary>
public class FunnelView
{
    public readonly int N;
    public readonly float S, H;
    readonly Transform root;
    readonly Vector3 o;
    float sacks;
    readonly SegmentMesh primeColumns, compositeColumns, floor, tree, powers, euler;
    readonly PointMesh primeTops, compositeTops;
    readonly List<TextBox> hourLabels = new();
    readonly List<TextBox> powerLabels = new();
    TextBox treeLabel;
    Transform pathRoot;
    readonly List<GameObject> pathSteps = new();
    float stepClock;
    TextBox eulerLabel;
    float dim = 1f;
    float pathRedraw = -1f;

    public int Selected { get; private set; }
    public int StepsShown { get; private set; }
    public int StepCount { get; private set; }
    public bool Playing { get; private set; }
    /// <summary>0 off, 1 every prime, otherwise only the first steps that drop this prime.</summary>
    public int TreeMode { get; private set; }
    public bool PowersOn { get; private set; }
    public bool PrimesOnly { get; private set; }
    public bool EulerOn { get; private set; }
    public float SacksMix => sacks;
    /// <summary>Raised when the path shows one more step (the number of pieces lit).</summary>
    public event Action<int> Stepped;

    public static readonly int[] PowerPrimes = { 2, 3, 5, 7, 11, 13 };
    public static readonly int[] Exponents = { 1, 7, 11, 13, 17, 19, 23, 29 };

    public FunnelView(Transform parent, Vector3 origin, int n, float s, float h)
    {
        N = n;
        S = s;
        H = h;
        o = origin;
        GameObject r = new("funnel");
        r.transform.SetParent(parent, false);
        root = r.transform;
        LabMath.EnsureSieve(N + 2);
        floor = new SegmentMesh(root, "floor", Factory.NoGlow);
        compositeColumns = new SegmentMesh(root, "composite columns", Factory.SoftGlowIntensity);
        primeColumns = new SegmentMesh(root, "prime columns", Factory.SoftGlowIntensity);
        compositeTops = new PointMesh(root, "composite tops", Factory.SoftGlowIntensity);
        primeTops = new PointMesh(root, "prime tops", Factory.GlowIntensity);
        tree = new SegmentMesh(root, "tree", Factory.SoftGlowIntensity);
        powers = new SegmentMesh(root, "powers", Factory.GlowIntensity);
        euler = new SegmentMesh(root, "euler", Factory.GlowIntensity);
        for (int hr = 0; hr < 30; hr++)
        {
            bool unit = Array.IndexOf(Exponents, hr) >= 0;
            hourLabels.Add(LabDraw.Label(root, hr.ToString(), o, unit ? 30f : 22f, unit ? LabMath.Gold : new Color(.55f, .58f, .7f)));
        }

        Rebuild();
    }

    // ---- layout ------------------------------------------------------------------------------------------

    /// <summary>The angle of n (radians): the wheel's hour, unwrapped (−12° per n), blended toward the Sacks angle −τ√n.</summary>
    public float Angle(float n)
    {
        float hour = (90f - 12f * n) * Mathf.Deg2Rad;
        float spiral = (90f - 360f * Mathf.Sqrt(n)) * Mathf.Deg2Rad;
        return Mathf.Lerp(hour, spiral, sacks);
    }

    public Vector3 Base(float n)
    {
        float a = Angle(n), r = S * Mathf.Sqrt(n);
        return o + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
    }

    /// <summary>The colour of n's top, the games' colours: gold prime, deep red p², blue pronic, a light grey for any other composite, white 1.</summary>
    public static Color TopColor(int n) => NumberKind.Classify(n) switch
    {
        NumberClass.Prime => LabMath.Gold,
        NumberClass.PrimeSquare => NumberKind.DeepRed * 1.25f,
        NumberClass.Pronic => NumberKind.Pronic,
        NumberClass.Composite => new Color(.8f, .82f, .92f),
        _ => Color.white,
    };

    public Vector3 Up(float h) => Vector3.back * (H * h);
    public Vector3 Top(int n) => Base(n) + Up(Mathf.Log(n));
    public float Rim => S * Mathf.Sqrt(N);
    public float Height => H * Mathf.Log(N);
    public Vector3 Origin => o;

    /// <summary>An arc at a fixed height from column a to column b, the short way round.</summary>
    void Arc(List<(Vector3, Vector3, Color, float)> segs, int a, int b, float height, Color c, float w, int steps = 10)
    {
        Vector3 pa = Base(a) - o, pb = Base(b) - o;
        float a0 = Mathf.Atan2(pa.y, pa.x), a1 = Mathf.Atan2(pb.y, pb.x);
        float da = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
        float r0 = pa.magnitude, r1 = pb.magnitude;
        Vector3 prev = Vector3.zero;
        for (int i = 0; i <= steps; i++)
        {
            float u = i / (float)steps, ang = a0 + da * u, r = Mathf.Lerp(r0, r1, u);
            Vector3 q = o + new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r, 0f) + Up(height);
            if (i > 0) segs.Add((prev, q, c, w));
            prev = q;
        }
    }

    /// <summary>Lay everything out again (after the Sacks blend or the dimming changes).</summary>
    public void Rebuild()
    {
        RebuildBase();
        if (Selected >= 2) BuildPath(Selected, StepsShown);
    }

    void RebuildBase()
    {
        List<(Vector3, Vector3, Color, float)> segs = new();
        // floor: rings at log 10, log 100 and the rim, the eight gold spokes of E8's exponents, ticks at all 30 hours
        foreach (float m in new[] { 10f, 100f, N })
        {
            float r = S * Mathf.Sqrt(m);
            Vector3 prev = Vector3.zero;
            for (int i = 0; i <= 120; i++)
            {
                float a = i / 120f * Mathf.PI * 2f;
                Vector3 q = o + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
                if (i > 0) segs.Add((prev, q, new Color(.4f, .44f, .62f) * .55f, .12f));
                prev = q;
            }
        }

        bool wheel = sacks < .02f;
        if (wheel)
        {
            for (int hr = 0; hr < 30; hr++)
            {
                float a = (90f - 12f * hr) * Mathf.Deg2Rad;
                Vector3 d = new(Mathf.Cos(a), Mathf.Sin(a), 0f);
                bool unit = Array.IndexOf(Exponents, hr) >= 0;
                if (unit) segs.Add((o + d * S, o + d * Rim * 1.02f, LabMath.Gold * .55f, .2f));
                segs.Add((o + d * Rim * 1.02f, o + d * Rim * 1.06f, unit ? LabMath.Gold : new Color(.5f, .52f, .65f), unit ? .35f : .2f));
            }
        }

        floor.Set(segs);
        for (int hr = 0; hr < 30; hr++)
        {
            float a = (90f - 12f * hr) * Mathf.Deg2Rad;
            hourLabels[hr].gameObject.SetActive(wheel);
            hourLabels[hr].transform.position = o + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * Rim * 1.12f;
        }

        // columns: every piece in the games' gold (a prime); a prime's own column one solid piece, brighter and wider than a
        // composite's stack, whose wider gaps show where one prime's piece ends and the next begins
        List<(Vector3, Vector3, Color, float)> ps = new(), cs = new();
        List<Vector3> pt = new(), ct = new();
        List<Color> pc = new(), cc = new();
        List<float> psz = new(), csz = new();
        for (int n = 2; n <= N; n++)
        {
            bool prime = LabMath.IsPrime(n);
            List<int> f = LabMath.Factorize(n);
            Vector3 b = Base(n);
            float h = 0f;
            for (int i = f.Count - 1; i >= 0; i--)
            {
                float seg = Mathf.Log(f[i]), cut = seg * (prime ? .05f : .09f);
                (prime ? ps : cs).Add((b + Up(h + cut), b + Up(h + seg - cut), NumberKind.Gold * ((prime ? 1f : .6f) * dim), prime ? .42f : .3f));
                h += seg;
            }

            if (prime)
            {
                pt.Add(Top(n));
                pc.Add(LabMath.Gold * Mathf.Lerp(.55f, 1f, dim));
                psz.Add(.55f);
            }
            else
            {
                ct.Add(Top(n));
                cc.Add(TopColor(n) * Mathf.Lerp(.45f, 1f, dim));
                csz.Add(NumberKind.Classify(n) == NumberClass.Composite ? .32f : .42f);
            }
        }

        primeColumns.Set(ps);
        compositeColumns.Set(cs);
        primeTops.Set(pt, pc, psz);
        compositeTops.Set(ct, cc, csz);
        compositeColumns.Visible = compositeTops.Visible = !PrimesOnly;
        BuildTree();
        BuildPowers();
        BuildEuler();
    }

    /// <summary>How bright the columns are (1 full; lower lets a path, the tree or the powers stand out).</summary>
    public void SetDim(float d)
    {
        d = Mathf.Clamp(d, .1f, 1f);
        if (Mathf.Abs(d - dim) < 1e-3f) return;
        dim = d;
        Rebuild();
    }

    /// <summary>Blend the layout toward the Sacks spiral. The path is hidden while the blend moves and redrawn at either end.</summary>
    public void SetSacks(float mix)
    {
        mix = Mathf.Clamp01(mix);
        if (Mathf.Abs(mix - sacks) < 1e-4f) return;
        sacks = mix;
        RebuildBase();
        bool end = sacks < 1e-3f || sacks > .999f;
        if (end && Selected >= 2) BuildPath(Selected, StepsShown);
        else
        {
            if (pathRoot != null) pathRoot.gameObject.SetActive(false);
            pathRedraw = .25f; // draw it again once the blend rests
        }
    }

    // ---- the tree, the powers, Euler's curve -------------------------------------------------------------

    public void SetTree(int mode)
    {
        TreeMode = mode;
        BuildTree();
    }

    /// <summary>Off → every prime → 2 → 3 → 5 → 7 → 11 → 13 → off.</summary>
    public void CycleTree()
    {
        int[] order = { 0, 1, 2, 3, 5, 7, 11, 13 };
        int i = Array.IndexOf(order, TreeMode);
        SetTree(order[(i + 1) % order.Length]);
    }

    void BuildTree()
    {
        if (treeLabel != null) UnityEngine.Object.Destroy(treeLabel.gameObject);
        treeLabel = null;
        if (TreeMode == 0)
        {
            tree.Clear();
            return;
        }

        // every drop is a prime piece, so gold: the prime shows in the drop's length, and a tree of one prime is named outside the rim
        List<(Vector3, Vector3, Color, float)> segs = new();
        for (int n = 2; n <= N; n++)
        {
            int p = LabMath.Spf(n);
            if (TreeMode != 1 && p != TreeMode) continue;
            if (PrimesOnly && !LabMath.IsPrime(n)) continue;
            int m = n / p;
            float h1 = Mathf.Log(m);
            segs.Add((Base(n) + Up(Mathf.Log(n)), Base(n) + Up(h1), NumberKind.Gold, .3f));
            if (m > 1) Arc(segs, n, m, h1, NumberKind.Gold * .6f, .16f, 8);
        }

        tree.Set(segs);
        if (TreeMode != 1) treeLabel = LabDraw.Label(root, $"first steps ÷{TreeMode}", o + new Vector3(-Rim * 1.32f, 0f, 0f) + Up(.5f), 40f, NumberKind.Gold);
    }

    public void SetPowers(bool on)
    {
        PowersOn = on;
        BuildPowers();
    }

    void BuildPowers()
    {
        foreach (TextBox t in powerLabels)
            if (t != null) UnityEngine.Object.Destroy(t.gameObject);
        powerLabels.Clear();
        if (!PowersOn)
        {
            powers.Clear();
            return;
        }

        // each spiral is a run of one prime, so gold; its label p^k at the top names it, and its steps climb log p
        List<(Vector3, Vector3, Color, float)> segs = new();
        foreach (int p in PowerPrimes)
        {
            int k = 0, top = 1;
            while ((long)top * p <= N)
            {
                top *= p;
                k++;
            }

            Color c = NumberKind.Gold;
            for (int n = top; n > 1; n /= p)
            {
                int m = n / p;
                segs.Add((Base(n) + Up(Mathf.Log(n)), Base(n) + Up(Mathf.Log(m)), c, .6f));
                if (m > 1) Arc(segs, n, m, Mathf.Log(m), c, .4f, 12);
            }

            powerLabels.Add(LabDraw.Haloed(LabDraw.Label(root, $"{p}{LabMath.Superscript(k)}", Top(top) + Up(.25f), 34f, c)));
        }

        powers.Set(segs);
    }

    public void SetEuler(bool on)
    {
        EulerOn = on;
        BuildEuler();
    }

    /// <summary>Euler's k² + k + 41 (prime for k = 0 … 39): on the Sacks layout its values run along one smooth curve.</summary>
    void BuildEuler()
    {
        if (eulerLabel != null) UnityEngine.Object.Destroy(eulerLabel.gameObject);
        eulerLabel = null;
        if (!EulerOn)
        {
            euler.Clear();
            return;
        }

        List<(Vector3, Vector3, Color, float)> segs = new();
        Color c = new(.35f, 1f, .78f);
        float kMax = (-1f + Mathf.Sqrt(1f + 4f * (N - 41))) * .5f;
        Vector3 prev = Top(41) + Up(.15f);
        if (sacks > .98f)
        {
            // Sacks's curve: the angle τ(√n − k) with the whole turns taken out, through every Euler prime and never coiling
            for (int i = 0; i <= 600; i++)
            {
                float k = kMax * i / 600f, n = k * k + k + 41f;
                float a = (90f - 360f * (Mathf.Sqrt(n) - k)) * Mathf.Deg2Rad, r = S * Mathf.Sqrt(n);
                Vector3 q = o + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f) + Up(Mathf.Log(n) + .15f);
                if (i > 0) segs.Add((prev, q, c, .4f));
                prev = q;
            }
        }

        // the integer k: Euler's primes, as dots on the curve
        for (int k = 0; k * k + k + 41 <= N; k++)
        {
            Vector3 q = Top(k * k + k + 41) + Up(.15f);
            segs.Add((q - Vector3.right * .6f, q + Vector3.right * .6f, Color.white, .9f));
        }

        euler.Set(segs);
        eulerLabel = LabDraw.Label(root, "k² + k + 41", prev + Up(1.4f), 34f, c);
    }

    public void SetPrimesOnly(bool on)
    {
        PrimesOnly = on;
        compositeColumns.Visible = compositeTops.Visible = !on;
        BuildTree();
    }

    // ---- the path back -----------------------------------------------------------------------------------

    /// <summary>Show n's path back; with <paramref name="play"/> it is drawn one drop at a time.</summary>
    public void Select(int n, bool play)
    {
        n = Mathf.Clamp(n, 2, N);
        Selected = n;
        StepCount = LabMath.Factorize(n).Count;
        stepClock = 0f;
        Playing = play;
        BuildPath(n, play ? 0 : StepCount);
        Stepped?.Invoke(StepsShown);
    }

    public void Replay() => Select(Selected, true);

    public void ClearPath()
    {
        Selected = 0;
        if (pathRoot != null) UnityEngine.Object.Destroy(pathRoot.gameObject);
        pathRoot = null;
        pathSteps.Clear();
    }

    void BuildPath(int n0, int shown)
    {
        if (pathRoot != null) UnityEngine.Object.Destroy(pathRoot.gameObject);
        pathSteps.Clear();
        pathRoot = new GameObject($"path {n0}").transform;
        pathRoot.SetParent(root, false);
        // the path's labels: where they bunch up (small numbers stand close together near the middle), they stack down
        // the screen in the path's order; the ÷ labels give way
        LabelStack stack = pathRoot.gameObject.AddComponent<LabelStack>();

        // step 0: the column itself, bright, with a ring and its number, and a white band at each joint between two pieces
        GameObject s0 = new("start");
        s0.transform.SetParent(pathRoot, false);
        List<(Vector3, Vector3, Color, float)> col = new();
        List<int> f = LabMath.Factorize(n0);
        float hh = 0f;
        Vector3 b0 = Base(n0);
        for (int i = f.Count - 1; i >= 0; i--)
        {
            float seg = Mathf.Log(f[i]), cut = seg * .05f;
            col.Add((b0 + Up(hh + cut), b0 + Up(hh + seg - cut), NumberKind.Gold, .9f));
            hh += seg;
            if (i > 0) LabDraw.Ring(s0.transform, "joint", b0 + Up(hh), .8f, .16f, Color.white, Factory.GlowIntensity, 24);
        }

        new SegmentMesh(s0.transform, "column", Factory.GlowIntensity).Set(col);
        LabDraw.Ring(s0.transform, "ring", Top(n0) + Up(.05f), 1.6f, .18f, Color.white, Factory.GlowIntensity, 40);
        stack.Add(LabDraw.Haloed(LabDraw.Label(s0.transform, Named(n0), Top(n0) + Up(1.3f), 52f, LabMath.IsPrime(n0) ? LabMath.Gold : Color.white)), Top(n0) + Up(1.3f), Vector2.zero);
        pathSteps.Add(s0);

        int n = n0;
        float h = Mathf.Log(n);
        while (n > 1)
        {
            int p = LabMath.Spf(n), m = n / p;
            float h1 = h - Mathf.Log(p);
            GameObject st = new($"÷{p} → {m}");
            st.transform.SetParent(pathRoot, false);
            List<(Vector3, Vector3, Color, float)> segs = new() { (Base(n) + Up(h), Base(n) + Up(h1), NumberKind.Gold, 1.4f) };
            if (m > 1) Arc(segs, n, m, h1, Color.white, .7f, 16);
            new SegmentMesh(st.transform, "step", 2.2f).Set(segs);
            Vector3 land = m > 1 ? Top(m) : Base(1);
            if (m == 1) LabDraw.Dot(st.transform, "one", Base(1), .9f, Color.white);
            string name = Named(m);
            TextBox l = LabDraw.Haloed(LabDraw.Label(st.transform, name, land + Up(.6f), 34f, m == 1 ? Color.white : LabMath.IsPrime(m) ? LabMath.Gold : new Color(.85f, .87f, .96f), TextAlignmentOptions.Center));
            stack.Add(l, land + Up(.6f), new Vector2(.052f * 34f * VisibleLength(name) * .5f + 1f, .6f));
            Vector3 mid = Base(n) + Up((h + h1) * .5f);
            stack.Add(LabDraw.Haloed(LabDraw.Label(st.transform, $"÷{p}", mid, 30f, NumberKind.Gold)), mid, new Vector2(-2.4f, 0f), true);
            pathSteps.Add(st);
            n = m;
            h = h1;
        }

        StepsShown = Mathf.Clamp(shown, 0, pathSteps.Count - 1);
        for (int i = 0; i < pathSteps.Count; i++) pathSteps[i].SetActive(i <= StepsShown);
    }

    /// <summary>
    /// A number as the staircase names it: a composite in exponent notation (360 = 2³ · 3² · 5), so each landing shows the
    /// blocks still to drop; a prime, or 1, as itself.
    /// </summary>
    static string Named(int n) => n < 4 || LabMath.IsPrime(n) ? n.ToString() : $"{n} = {NumberKind.Shorthand(n)}";

    /// <summary>The characters a label shows (rich-text tags left out), for placing it beside its anchor.</summary>
    static int VisibleLength(string s) => System.Text.RegularExpressions.Regex.Replace(s, "<[^>]+>", "").Length;

    public void Tick(float dt)
    {
        if (pathRedraw > 0f)
        {
            pathRedraw -= dt;
            if (pathRedraw <= 0f && Selected >= 2) BuildPath(Selected, StepsShown);
        }

        if (!Playing || Selected < 2) return;
        stepClock += dt;
        if (stepClock < .75f) return;
        stepClock = 0f;
        if (StepsShown >= pathSteps.Count - 1)
        {
            Playing = false;
            return;
        }

        StepsShown++;
        pathSteps[StepsShown].SetActive(true);
        Stepped?.Invoke(StepsShown);
        if (StepsShown >= pathSteps.Count - 1) Playing = false;
    }

    // ---- picking -----------------------------------------------------------------------------------------

    /// <summary>The pickable numbers: every column top (only primes when composites are hidden).</summary>
    public int PickNumber(int i) => i + 2;
    public int PickCount => N - 1;
    public bool Pickable(int n) => !PrimesOnly || LabMath.IsPrime(n);
}
