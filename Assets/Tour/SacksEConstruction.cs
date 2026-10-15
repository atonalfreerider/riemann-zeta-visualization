using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The scenes of the modules "E5 to E8, from mirrors" and "The paths back" (docs/experiments/README.md, sections GA, GB):
/// one mirror and its root; two mirrors at 60° (A2) and at 90° (A1 × A1); the E-series diagrams growing from E5 to E8 (and the
/// infinite E9); the roots of E5–E8 on their Coxeter planes with the Coxeter element turning each by one hour; the E-wheel stack
/// with its threads revealed in groups; the factor columns of E8's wheel growing, cut by prime; and the descent paths of single
/// numbers, step by step, with the exponents tallied on the caption board.
/// </summary>
public partial class SacksSpiral
{
    public const float MirrorOffset = 54000f;
    public const float DihedralOffset = 54300f;
    public const float DynkinOffset = 54600f;
    public const float CoxPanelsOffset = 54900f;

    static readonly Color MirrorColor = new(.75f, .82f, 1f);
    static readonly Color ImageBlue = new(.45f, .66f, 1f);

    /// <summary>A new reveal item (a child of the group's "reveal") with a caption-board note.</summary>
    Transform RevealItem(Transform g, string name, string note)
    {
        GameObject it = new(name);
        it.transform.SetParent(RevealItems(g), false);
        RevealNote rn = it.AddComponent<RevealNote>();
        rn.Text = note;
        return it.transform;
    }

    void ArrowLine(Transform g, string name, Vector3 a, Vector3 b, float width, Color col, float glow)
    {
        Vector3 dir = (b - a).normalized, side = Vector3.Cross(dir, Vector3.back).normalized;
        float head = width * 4f;
        ExpLine(g, name, width, col, glow, new[] { a, b - dir * head * .6f });
        ExpLine(g, name + " head", width, col, glow, new[] { b - dir * head * 1.6f + side * head, b, b - dir * head * 1.6f - side * head });
    }

    // ================================================================================================
    // one mirror
    // ================================================================================================

    /// <summary>
    /// One mirror (a vertical line), an asymmetric shape of dots on its left, their images on the right joined by the
    /// perpendiculars they jump along, and the root α at right angles to the mirror. Group "EC mirror", four reveal items.
    /// </summary>
    public void ExpMirrorScene(Vector3 o, float unit, float dot, float labelSize)
    {
        Transform g = ExpGroup("EC mirror").transform;
        Vector2[] shape = { new(-6f, -5f), new(-6f, -2f), new(-6f, 1f), new(-6f, 4f), new(-3f, 4f), new(-1.5f, 4f), new(-3f, 1f), new(-4.5f, 1f), new(-2.5f, -3.5f) };
        Vector3 P(Vector2 v) => o + new Vector3(v.x, v.y, 0f) * unit;
        Transform m = RevealItem(g, "mirror", "a mirror\n" + Small("reflect the plane across one line"));
        ExpLine(m, "mirror line", dot * .3f, MirrorColor, Factory.GlowIntensity, new[] { o + Vector3.down * 8.5f * unit, o + Vector3.up * 8.5f * unit });
        if (labelSize > 0f) ExpLabelWide(m, "mirror", o + Vector3.up * 9.6f * unit, labelSize * .4f, MirrorColor);
        Transform a = RevealItem(g, "shape", "a shape on one side\n" + Small("nine points"));
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        foreach (Vector2 v in shape) { pts.Add(P(v)); cols.Add(PrimeGold); sizes.Add(dot * 1.2f); }
        ExpPointCloud(a, "shape", pts, cols, sizes, Factory.GlowIntensity);
        ExpLine(a, "shape outline", dot * .2f, PrimeGold * .6f, Factory.SoftGlowIntensity, new[] { P(shape[0]), P(shape[1]), P(shape[2]), P(shape[3]), P(shape[4]), P(shape[5]) });
        Transform b = RevealItem(g, "images", "every point jumps to its image\n" + Small("straight across, the same distance on the other side"));
        List<(Vector3, Vector3)> jumps = new();
        List<Vector3> ip = new(); List<Color> ic = new(); List<float> isz = new();
        foreach (Vector2 v in shape)
        {
            Vector2 w = new(-v.x, v.y);
            jumps.Add((P(v), P(w)));
            ip.Add(P(w)); ic.Add(ImageBlue); isz.Add(dot * 1.2f);
        }

        Segments(b, "jumps", jumps, dot * .12f, Color.white * .4f);
        ExpPointCloud(b, "images", ip, ic, isz, Factory.GlowIntensity);
        Vector2[] img = new Vector2[shape.Length];
        for (int i = 0; i < shape.Length; i++) img[i] = new Vector2(-shape[i].x, shape[i].y);
        ExpLine(b, "image outline", dot * .2f, ImageBlue * .6f, Factory.SoftGlowIntensity, new[] { P(img[0]), P(img[1]), P(img[2]), P(img[3]), P(img[4]), P(img[5]) });
        Transform r = RevealItem(g, "root", "the root α\n" + Small("the arrow at right angles to the mirror"));
        ArrowLine(r, "root", o + Vector3.back * .3f, o + new Vector3(5f * unit, 0f, -.3f), dot * .45f, Color.white, AttentionIntensity);
        if (labelSize > 0f) ExpLabelWide(r, "root α", o + new Vector3(5.6f * unit, -.5f * unit, -.3f), labelSize * .4f, Color.white, TextAlignmentOptions.Left);
    }

    // ================================================================================================
    // two mirrors: A2 (60°) and A1 × A1 (90°)
    // ================================================================================================

    /// <summary>
    /// Two mirrors at 180°/m (m = 3: A2, m = 2: A1 × A1) through o, as reveal items of group "EC dihedral {m}": the two
    /// mirrors; a point's two reflections with arrows and the turn they make; the rest of the mirrors they generate with the
    /// whole orbit of the point (even images gold, odd images blue); the roots. The diagram (two dots, joined when the mirrors
    /// meet at 60°) goes to group "EC diagrams", so the diagrams of both pairs can be revealed together.
    /// </summary>
    public void ExpDihedralScene(Vector3 o, int m, float unit, float dot, float labelSize)
    {
        Transform g = ExpGroup($"EC dihedral {m}").transform;
        float angle = 180f / m;
        // root (normal) directions: α1 at 0°, α2 at 180° − angle; mirror lines are perpendicular to them
        Vector2 Dir(float deg) => new(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad));
        Vector3 W(Vector2 v) => o + new Vector3(v.x, v.y, 0f) * unit;
        Vector2 Refl(Vector2 p, Vector2 n) => p - 2f * Vector2.Dot(p, n) * n;
        Vector2 n1 = Dir(0f), n2 = Dir(180f - angle);
        float L = 9f;
        Transform t0 = RevealItem(g, "two mirrors", (m == 3 ? "two mirrors at 60°" : "two mirrors at 90°") + "\n" + Small(m == 3 ? "the root arrows meet at 120°" : "the root arrows meet at 90°"));
        foreach (Vector2 n in new[] { n1, n2 })
        {
            Vector2 d = new(-n.y, n.x);
            ExpLine(t0, "mirror", dot * .3f, MirrorColor, Factory.GlowIntensity, new[] { W(-d * L), W(d * L) });
        }

        // the orbit of a point at 15°
        Vector2 p0 = Dir(15f) * 6f;
        List<(Vector2 p, int parity)> orbit = new() { (p0, 0) };
        for (int it = 0; it < 12; it++)
        {
            List<(Vector2, int)> add = new();
            foreach (var (p, par) in orbit)
                foreach (Vector2 n in new[] { n1, n2 })
                {
                    Vector2 q = Refl(p, n);
                    bool seen = orbit.Exists(x => (x.p - q).sqrMagnitude < 1e-4f) || add.Exists(x => (x.Item1 - q).sqrMagnitude < 1e-4f);
                    if (!seen) add.Add((q, 1 - par));
                }

            orbit.AddRange(add);
        }

        // the two reflections and the turn they make
        Transform t1 = RevealItem(g, "turn", m == 3 ? "reflect twice:  a turn of 120°\n" + Small("two mirrors at 60° turn the plane by a third of a circle")
            : "reflect twice:  a half turn\n" + Small("two mirrors at 90° turn the plane by 180°"));
        Vector2 p1 = Refl(p0, n1), p2 = Refl(p1, n2);
        ExpPointCloud(t1, "point and images", new List<Vector3> { W(p0) + Vector3.back * .2f, W(p1) + Vector3.back * .2f, W(p2) + Vector3.back * .2f },
            new List<Color> { PrimeGold, ImageBlue, PrimeGold }, new List<float> { dot * 1.5f, dot * 1.5f, dot * 1.5f }, Factory.GlowIntensity);
        ArrowLine(t1, "first reflection", W(p0) + Vector3.back * .3f, W(p1) + Vector3.back * .3f, dot * .3f, Color.white, Factory.GlowIntensity);
        ArrowLine(t1, "second reflection", W(p1) + Vector3.back * .3f, W(p2) + Vector3.back * .3f, dot * .3f, Color.white, Factory.GlowIntensity);
        List<Vector3> arc = new();
        float a0 = Mathf.Atan2(p0.y, p0.x), a2 = Mathf.Atan2(p2.y, p2.x);
        float da = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a2 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
        for (int s = 0; s <= 30; s++) { float a = a0 + da * s / 30f; arc.Add(W(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 7.6f) + Vector3.back * .3f); }
        ExpLine(t1, "turn arc", dot * .32f, PrimeGold, AttentionIntensity, arc.ToArray());
        // the mirrors they generate (for m = 3, a third) and the whole orbit
        Transform t2 = RevealItem(g, "all mirrors", m == 3 ? "they generate a third mirror\n" + Small("and six images of any point") : "they generate nothing more\n" + Small("four images of any point"));
        if (m == 3)
        {
            Vector2 n3 = Dir(60f), d3 = new(-n3.y, n3.x);
            ExpLine(t2, "mirror 3", dot * .22f, MirrorColor * .6f, Factory.SoftGlowIntensity, new[] { W(-d3 * L), W(d3 * L) });
        }

        List<Vector3> op = new(); List<Color> oc = new(); List<float> os = new();
        foreach (var (p, par) in orbit) { op.Add(W(p) + Vector3.back * .2f); oc.Add(par == 0 ? PrimeGold : ImageBlue); os.Add(dot * 1.3f); }
        ExpPointCloud(t2, "orbit", op, oc, os, Factory.GlowIntensity);
        List<Vector3> ring = new();
        orbit.Sort((x, y) => Mathf.Atan2(x.p.y, x.p.x).CompareTo(Mathf.Atan2(y.p.y, y.p.x)));
        foreach (var (p, _) in orbit) ring.Add(W(p));
        ExpLine(t2, "orbit ring", dot * .14f, Color.white * .35f, Factory.NoGlow, ring.ToArray(), true);
        // the roots
        Transform t3 = RevealItem(g, "roots", m == 3 ? "six roots, in a hexagon\n" + Small("one pair for each mirror") : "four roots\n" + Small("one pair for each mirror"));
        int nRoots = 2 * m;
        for (int k = 0; k < nRoots; k++)
        {
            Vector2 d = Dir(k * 180f / m) * 4.2f;
            ArrowLine(t3, $"root {k}", o + Vector3.back * .35f, W(d) + Vector3.back * .35f, dot * .32f, Color.white, AttentionIntensity);
        }

        // the diagram
        Transform gd = FindExpGroup("EC diagrams") ?? ExpGroup("EC diagrams").transform;
        Transform t4 = RevealItem(gd, $"diagram {m}", m == 3 ? "the diagram:  a line for 60°\n" + Small("two dots joined:  the mirrors meet at 60°")
            : "the diagram:  no line for 90°\n" + Small("two dots apart:  the mirrors are at right angles"));
        Vector3 dg = o + new Vector3(12.5f * unit, 0f, 0f);
        List<Vector3> nodes = new() { dg + Vector3.up * 1.6f * unit, dg - Vector3.up * 1.6f * unit };
        ExpPointCloud(t4, "nodes", nodes, new List<Color> { MirrorColor, MirrorColor }, new List<float> { dot * 2f, dot * 2f }, AttentionIntensity);
        if (m == 3) ExpLine(t4, "edge", dot * .35f, MirrorColor, Factory.GlowIntensity, new[] { nodes[0], nodes[1] });
        if (labelSize > 0f) ExpLabelWide(t4, m == 3 ? "A2" : "A1 × A1", dg + Vector3.down * 3.6f * unit, labelSize * .4f, MirrorColor);
    }

    // ================================================================================================
    // the E-series diagrams
    // ================================================================================================

    /// <summary>
    /// The diagrams of E5 (= D5), E6, E7, E8 stacked downwards, and a ghost E9: Bourbaki's nodes 1-3-4-5-6-7-8 in a row with
    /// node 2 above node 4; each row one reveal item of group "EC dynkin".
    /// </summary>
    public void ExpDynkinE(Vector3 o, float spacing, float rowGap, float dot, float labelSize)
    {
        Transform g = ExpGroup("EC dynkin").transform;
        // node → (column, row-up) in units of spacing
        Dictionary<int, Vector2> at = new() { [1] = new(0, 0), [3] = new(1, 0), [4] = new(2, 0), [5] = new(3, 0), [6] = new(4, 0), [7] = new(5, 0), [8] = new(6, 0), [2] = new(2, 1), [9] = new(7, 0) };
        (int a, int b)[] edges = { (1, 3), (3, 4), (4, 5), (2, 4), (5, 6), (6, 7), (7, 8), (8, 9) };
        string[] names = { "E5 = D5", "E6", "E7", "E8", "E9" };
        string[] notes =
        {
            "E5:  five mirrors in a fork\n" + Small("the same diagram as D5"),
            "E6:  add a dot to an arm\n" + Small("72 roots"),
            "E7:  extend that arm again\n" + Small("126 roots"),
            "E8:  and once more\n" + Small("240 roots"),
            "E9:  never closes up\n" + Small("a ninth mirror generates infinitely many images"),
        };
        for (int row = 0; row < 5; row++)
        {
            int n = 5 + row;
            bool ghost = n == 9;
            Transform it = RevealItem(g, names[row], notes[row]);
            Vector3 b = o + Vector3.down * rowGap * row;
            Vector3 P(int node) => b + new Vector3(at[node].x, at[node].y * .9f, 0f) * spacing;
            List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
            foreach (var kv in at)
            {
                if (kv.Key > n) continue;
                bool fresh = kv.Key == n && n > 5;
                pts.Add(P(kv.Key) + Vector3.back * .2f);
                cols.Add(ghost ? new Color(.45f, .45f, .55f) : fresh ? PrimeGold : MirrorColor);
                sizes.Add(dot * (fresh ? 2.4f : 2f));
            }

            ExpPointCloud(it, "nodes", pts, cols, sizes, ghost ? Factory.NoGlow : AttentionIntensity);
            foreach (var (a, c) in edges)
            {
                if (a > n || c > n) continue;
                bool fresh = c == n && n > 5;
                ExpLine(it, $"edge {a}-{c}", dot * .35f, ghost ? new Color(.45f, .45f, .55f) : fresh ? PrimeGold : MirrorColor * .85f, ghost ? Factory.NoGlow : Factory.GlowIntensity, new[] { P(a), P(c) });
            }

            if (labelSize > 0f)
            {
                ExpLabelWide(it, names[row], b + new Vector3(-spacing * 1.1f, 0f, 0f), labelSize * .42f, ghost ? new Color(.55f, .55f, .65f) : Color.white, TextAlignmentOptions.Right);
                string tail = n switch { 5 => "40 roots", 6 => "72 roots", 7 => "126 roots", 8 => "240 roots", _ => "infinite" };
                ExpLabelWide(it, tail, b + new Vector3(spacing * (n - 1) + spacing * .55f, 0f, 0f), labelSize * .3f, ghost ? new Color(.55f, .55f, .65f) : new Color(.8f, .84f, .95f), TextAlignmentOptions.Left);
            }
        }
    }

    // ================================================================================================
    // the roots on their Coxeter planes, and the Coxeter element's turn
    // ================================================================================================

    readonly List<(Vector3 center, float radius, float a0, int h)> coxPanels = new();

    /// <summary>
    /// The roots of E5–E8 on their Coxeter planes, in a grid (group "EC panels", one item per lattice), each scaled to the
    /// same outer radius with the edges between roots at inner product 1; the hour formulas (group "EC hours").
    /// </summary>
    public void ExpCoxeterPanels(Vector3 o, int[] ranks, int columns, float spacingX, float spacingY, float radius, float dot, float labelSize)
    {
        Transform g = ExpGroup("EC panels").transform;
        Transform gh = ExpGroup("EC hours").transform;
        coxPanels.Clear();
        int rows = (ranks.Length + columns - 1) / columns;
        for (int i = 0; i < ranks.Length; i++)
        {
            int rank = ranks[i];
            var (roots, u1, u2, h) = CoxeterSystem(rank);
            Vector3 c = o + new Vector3((i % columns - (columns - 1) * .5f) * spacingX, ((rows - 1) * .5f - i / columns) * spacingY, 0f);
            Vector2[] p = new Vector2[roots.Count];
            float outer = 0f;
            for (int k = 0; k < roots.Count; k++) { p[k] = new Vector2((float)Dot8(roots[k], u1), (float)Dot8(roots[k], u2)); outer = Mathf.Max(outer, p[k].magnitude); }
            float sc = radius / outer;
            Transform it = RevealItem(g, ELabel(rank), $"{ELabel(rank)}:  {roots.Count} roots\n" + Small($"{rank} mirrors generate them all"));
            List<(Vector3, Vector3)> edges = new();
            for (int a = 0; a < roots.Count; a++)
                for (int b = a + 1; b < roots.Count; b++)
                    if (System.Math.Abs(Dot8(roots[a], roots[b]) - 1) < 1e-9) edges.Add((c + (Vector3)(p[a] * sc) + Vector3.forward * .1f, c + (Vector3)(p[b] * sc) + Vector3.forward * .1f));
            Segments(it, "edges", edges, dot * .06f, new Color(.45f, .55f, .9f) * .45f);
            List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
            float a0 = 0f; bool found = false;
            for (int k = 0; k < roots.Count; k++)
            {
                float r = p[k].magnitude / outer;
                pts.Add(c + (Vector3)(p[k] * sc) + Vector3.back * .2f);
                cols.Add(Color.Lerp(new Color(1f, .45f, .6f), new Color(1f, .85f, .35f), r));
                sizes.Add(dot * .9f);
                if (!found && r > .999f) { a0 = Mathf.Atan2(p[k].y, p[k].x); found = true; }
            }

            ExpPointCloud(it, "roots", pts, cols, sizes, Factory.GlowIntensity);
            RingAround(it, "outer ring", c, Vector3.back, radius * 1.04f, new Color(.5f, .55f, .7f) * .45f, dot * .1f, Factory.NoGlow);
            coxPanels.Add((c, radius, a0, h));
            if (labelSize <= 0f) continue;
            ExpLabelWide(it, ELabel(rank), c + new Vector3(0f, radius * 1.18f + labelSize * .025f, 0f), labelSize * .42f, Color.white);
            ExpLabelWide(it, $"{roots.Count} roots", c + new Vector3(0f, radius * 1.18f - labelSize * .015f, 0f), labelSize * .3f, new Color(.8f, .84f, .95f));
            Transform hi = RevealItem(gh, $"hours {rank}", $"{ELabel(rank)}:  {roots.Count} ÷ {rank} = {h} hours\n" + Small($"{roots.Count} roots, {rank} mirrors:  the Coxeter element turns by 1/{h}"));
            ExpLabelWide(hi, $"{roots.Count} ÷ {rank} = {h} hours", c + new Vector3(0f, -radius * 1.2f - labelSize * .01f, 0f), labelSize * .36f, PrimeGold);
        }
    }

    /// <summary>A walker on each panel's outer ring, stepping one hour (1/h of a turn) at a time: one full turn per panel in the time given.</summary>
    public void ExpCoxeterWalkPanels(float seconds) => StartLive(CoxeterWalkPanels(seconds));

    IEnumerator CoxeterWalkPanels(float seconds)
    {
        if (coxPanels.Count == 0) yield break;
        Transform g = ExpGroup("EC walkers").transform;
        List<GameObject> walkers = new();
        foreach (var pnl in coxPanels)
            walkers.Add(ExpDot(g, "walker", pnl.center + new Vector3(Mathf.Cos(pnl.a0), Mathf.Sin(pnl.a0), 0f) * pnl.radius + Vector3.back * .5f, pnl.radius * .07f, Color.white, AttentionIntensity));
        int[] last = new int[coxPanels.Count];
        float clock = 0f, T = seconds * .92f;
        Board("the Coxeter element\n" + Small("reflect in every mirror once:  on its plane, a turn by one hour"));
        while (clock < T)
        {
            for (int i = 0; i < coxPanels.Count; i++)
            {
                var pnl = coxPanels[i];
                int k = Mathf.Min(pnl.h, Mathf.FloorToInt(clock / T * pnl.h + 1e-4f));
                float t = Mathf.Clamp01(clock / T * pnl.h - k);
                float ease = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 3f));
                float a = pnl.a0 + 2f * Mathf.PI * (k + ease) / pnl.h;
                if (walkers[i] == null) yield break;
                walkers[i].transform.position = pnl.center + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * pnl.radius + Vector3.back * .5f;
                if (k != last[i] && k <= pnl.h) { last[i] = k; }
            }

            yield return null;
            clock += Time.deltaTime;
        }

        liveRoutine = null;
    }

    /// <summary>Caption-board text from a script line (exp:say:HEAD|SMALL; no colons inside).</summary>
    public static void ExpSay(string head, string small) => Board(string.IsNullOrEmpty(small) ? head : head + "\n" + Small(small));

    /// <summary>The E-wheel stack of the module, with its threads in three reveal groups (A: a factor 3, B: a factor 5, C: factors ≥ 7).</summary>
    public void ExpEStackModule(Vector3 o, float labelSize)
    {
        long[] threads = { 9, 15, 25, 35, 49, 77, 91 };
        int[] layers = { 0, 0, 2, 1, 3, 2, 1 };
        string[] groups = { "EC threads A", "EC threads A", "EC threads B", "EC threads B", "EC threads C", "EC threads C", "EC threads C" };
        string[] notes =
        {
            "9 = 3·3:  on a spoke of E5\n" + Small("off E6, E7, E8:  3 divides 12, 18 and 30"),
            "15 = 3·5:  on a spoke of E5\n" + Small("off E6, E7, E8:  3 divides 12, 18 and 30"),
            "25 = 5·5:  on E5, E6, E7\n" + Small("off E8:  5 divides 30"),
            "35 = 5·7:  on E5, E6, E7\n" + Small("off E8:  5 divides 30"),
            "49 = 7·7:  on all four\n" + Small("8, 12, 18, 30 have no factor 7"),
            "77 = 7·11:  on all four\n" + Small("8, 12, 18, 30 have no factor 7 or 11"),
            "91 = 7·13:  on all four\n" + Small("no E-wheel removes 7"),
        };
        ExpEStack(o, new[] { 5, 6, 7, 8 }, 180, 40f, 10f, 34f, .55f, labelSize, threads, layers, groups, notes);
    }

    /// <summary>The six example paths unrolled, in two rows (for a portrait frame).</summary>
    public void ExpDescentRows(Vector3 o, float labelSize)
    {
        ExpDescentChart(o, new long[] { 360, 256, 343 }, 3.6f, 4.6f, 5f, .5f, labelSize);
        ExpDescentChart(o + new Vector3(0f, -46f, 0f), new long[] { 210, 324, 397 }, 3.6f, 4.6f, 5f, .5f, labelSize);
    }

    // ================================================================================================
    // the factor columns, growing; and one number's path back, step by step
    // ================================================================================================

    /// <summary>
    /// The funnel base (rings at log 10 and log 100, the gold spokes, group "GB base"), the columns of 2 … nMax as plain white
    /// lines (group "GB plain") and cut into prime segments (group "GB cut"), both in reveal batches of <paramref name="batch"/>
    /// numbers; and a dim copy of the cut columns as one item (group "GB faint").
    /// </summary>
    public void ExpFactorColumnsLive(Vector3 o, int nMax, float s, float hScale, float dot, float labelSize, int batch)
    {
        ExpEnsureSieve(nMax + 2);
        Vector3 Up(float h) => Vector3.back * (hScale * h);
        Transform gb = ExpGroup("GB base").transform;
        foreach (int m in new[] { 10, 100 })
        {
            List<Vector3> ring = new();
            float r = s * Mathf.Sqrt(m);
            for (int i = 0; i <= 120; i++) { float a = i / 120f * Mathf.PI * 2f; ring.Add(o + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f) + Up(Mathf.Log(m))); }
            ExpLine(gb, $"funnel ring {m}", dot * .1f, new Color(.5f, .55f, .75f) * .5f, Factory.NoGlow, ring.ToArray());
        }

        List<(Vector3, Vector3)> spokes = new();
        float rMax = s * Mathf.Sqrt(nMax);
        foreach (int k in new[] { 1, 7, 11, 13, 17, 19, 23, 29 })
        {
            float a = (90f - 12f * k) * Mathf.Deg2Rad;
            spokes.Add((o, o + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * rMax * 1.03f));
        }

        Segments(gb, "spokes", spokes, dot * .12f, PrimeGold * .3f, Factory.NoGlow);
        Transform gp = ExpGroup("GB plain").transform, gc = ExpGroup("GB cut").transform, gf = ExpGroup("GB faint").transform;
        Transform faintItem = RevealItem(gf, "faint", null);
        Dictionary<long, List<(Vector3, Vector3)>> faint = new();
        for (int start = 2; start <= nMax; start += batch)
        {
            int end = Mathf.Min(nMax, start + batch - 1);
            Transform pi = RevealItem(gp, $"plain {start}", null), ci = RevealItem(gc, $"cut {start}", null);
            List<(Vector3, Vector3)> plain = new();
            Dictionary<long, List<(Vector3, Vector3)>> cut = new();
            List<Vector3> tops = new(); List<Color> topCols = new(); List<float> topSizes = new();
            for (long n = start; n <= end; n++)
            {
                Vector3 b = FunnelBase(o, n, s);
                plain.Add((b, b + Up(Mathf.Log(n))));
                List<long> f = Factorize(n);
                f.Reverse();
                float h = 0f;
                foreach (long p in f)
                {
                    float seg = Mathf.Log(p), cutGap = seg * .05f;
                    long key = p <= 13 ? p : 17;
                    if (!cut.ContainsKey(key)) cut[key] = new();
                    if (!faint.ContainsKey(key)) faint[key] = new();
                    cut[key].Add((b + Up(h + cutGap), b + Up(h + seg - cutGap)));
                    faint[key].Add((b + Up(h + cutGap), b + Up(h + seg - cutGap)));
                    h += seg;
                }

                tops.Add(b + Up(h)); topCols.Add(f.Count == 1 ? PrimeGold : new Color(.8f, .82f, .92f)); topSizes.Add(f.Count == 1 ? dot * .9f : dot * .55f);
            }

            Segments(pi, "plain", plain, dot * .22f, new Color(.85f, .87f, .95f) * .7f, Factory.SoftGlowIntensity);
            foreach (var kv in cut) Segments(ci, $"cut {kv.Key}", kv.Value, dot * .3f, SegmentColor(kv.Key) * .85f, Factory.SoftGlowIntensity);
            ExpPointCloud(ci, "tops", tops, topCols, topSizes, Factory.GlowIntensity);
        }

        foreach (var kv in faint) Segments(faintItem, $"faint {kv.Key}", kv.Value, dot * .2f, SegmentColor(kv.Key) * .3f, Factory.NoGlow);
        if (labelSize > 0f)
        {
            Transform gl = ExpGroup("GB ring labels").transform;
            ExpLabelWide(gl, "log 10", o + new Vector3(s * Mathf.Sqrt(10f) * 1.1f, 0f, 0f) + Up(Mathf.Log(10f)), labelSize * .26f, new Color(.6f, .65f, .85f), TextAlignmentOptions.Left);
            ExpLabelWide(gl, "log 100", o + new Vector3(s * 10f * 1.05f, 0f, 0f) + Up(Mathf.Log(100f)), labelSize * .26f, new Color(.6f, .65f, .85f), TextAlignmentOptions.Left);
        }
    }

    /// <summary>
    /// One number's path back, as reveal items: item 0 = its column and a ring at the top; then one item per prime factor
    /// (smallest first) = the drop (in the prime's colour), the step across to the column of n/p, that column, and a passing
    /// label of n/p. Each item's note writes the step and the exponents found so far on the board, and the strip above the
    /// captions shows the column laid on its side (top on the left): a piece lit per drop, the values at the cuts, and the
    /// exponent under each finished run of equal pieces. Items are split into groups "stair N #k" by <paramref name="splits"/>
    /// (item counts), or all in "stair N #0".
    /// </summary>
    public void ExpFactorStairLive(Vector3 o, long n0, float s, float hScale, float dot, float labelSize, int[] splits)
    {
        Vector3 Up(float h) => Vector3.back * (hScale * h);
        List<long> drops = Factorize(n0);
        int total = 1 + drops.Count;
        if (splits == null || splits.Length == 0) splits = new[] { total };
        int gi = 0, inGroup = 0;
        Transform g = ExpGroup($"stair {n0} #0").transform;
        Transform Next(string name, string note)
        {
            if (inGroup >= splits[Mathf.Min(gi, splits.Length - 1)] && gi + 1 < splits.Length) { gi++; inGroup = 0; g = ExpGroup($"stair {n0} #{gi}").transform; }
            inGroup++;
            return RevealItem(g, name, note);
        }

        void Column(Transform t, long m)
        {
            if (m < 2) return;
            List<long> f = Factorize(m);
            f.Reverse();
            Vector3 b = FunnelBase(o, m, s);
            float h = 0f;
            foreach (long p in f)
            {
                float seg = Mathf.Log(p), cut = seg * .05f;
                ExpLine(t, $"segment {m} {p}", dot * .5f, SegmentColor(p) * .9f, Factory.GlowIntensity, new[] { b + Up(h + cut), b + Up(h + seg - cut) });
                h += seg;
            }
        }

        // a label beside the point it names, on the camera's right whatever the view
        void Beside(Transform t, string text, Vector3 at, float size, Color col, float showFor)
        {
            TextBox l = ExpLabelWide(t, text, at, size, col);
            ScreenNudge nudge = l.gameObject.AddComponent<ScreenNudge>();
            nudge.Anchor = at;
            nudge.Offset = new Vector2(.052f * size * text.Length * .5f + dot * 2.5f, .1f * size * .4f);
            if (showFor > 0f) l.gameObject.AddComponent<ShowFor>().Seconds = showFor;
        }

        string head = ExpPrime(n0) ? $"{n0}:  prime" : $"{n0} = {FactorString(n0)}";
        Transform first = Next("start", head + "\n" + Small("start at the top of its column"));
        Column(first, n0);
        Vector3 top = FunnelBase(o, n0, s) + Up(Mathf.Log(n0));
        RingAround(first, "start ring", top + Vector3.back * .3f, Vector3.back, dot * 2.6f, Color.white, dot * .22f, AttentionIntensity);
        if (labelSize > 0f) ExpLabelWide(first, n0.ToString(), top + Up(.3f) + Vector3.back * .3f, labelSize * .4f, ExpPrime(n0) ? PrimeGold : Color.white);
        RevealNote startNote = first.GetComponent<RevealNote>();
        startNote.FlareAt.Add(top);
        // the strip: the column laid on its side, the top (the first drop) on the left
        float[] weights = new float[drops.Count];
        Color[] colors = new Color[drops.Count];
        string[] inner = new string[drops.Count], marks = new string[drops.Count + 1];
        List<(int first, int last, string label, Color color)> runs = new();
        long rest = n0;
        marks[0] = n0.ToString();
        for (int i = 0; i < drops.Count; i++)
        {
            weights[i] = Mathf.Log(drops[i]);
            colors[i] = SegmentColor(drops[i]);
            inner[i] = drops[i].ToString();
            rest /= drops[i];
            marks[i + 1] = rest.ToString();
            if (i == 0 || drops[i] != drops[i - 1]) runs.Add((i, i, "", colors[i]));
            else runs[^1] = (runs[^1].first, i, "", colors[i]);
        }

        for (int k = 0; k < runs.Count; k++)
        {
            int count = runs[k].last - runs[k].first + 1;
            long p = drops[runs[k].first];
            runs[k] = (runs[k].first, runs[k].last, count == 1 ? p.ToString() : p + Superscript(count), runs[k].color);
        }

        var runArray = runs.ToArray();
        startNote.OnShow = () => StripShow(weights, colors, inner, marks, runArray);
        long n = n0;
        float hh = Mathf.Log(n);
        int lit = 0;
        while (n > 1)
        {
            long p = Factorize(n)[0];
            long m = n / p;
            string tally = FactorString(n0 / m);
            Transform t = Next($"÷{p} → {m}", head + "\n" + Small($"÷{p}  →  {m}") + "      " + Small(m == 1 ? $"all found:  {tally}" : $"found so far:  {tally}"));
            Vector3 b = FunnelBase(o, n, s), bm = FunnelBase(o, m, s);
            float h1 = hh - Mathf.Log(p);
            ExpLine(t, "drop", dot * .9f, SegmentColor(p), AttentionIntensity, new[] { b + Up(hh) + Vector3.back * .25f, b + Up(h1) + Vector3.back * .25f });
            float a0 = Mathf.Atan2(b.y - o.y, b.x - o.x), a1 = Mathf.Atan2(bm.y - o.y, bm.x - o.x);
            float da = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            float r0 = (b - o).magnitude, r1 = (bm - o).magnitude;
            List<Vector3> arc = new();
            for (int i = 0; i <= 28; i++)
            {
                float u = i / 28f, a = a0 + da * u, r = Mathf.Lerp(r0, r1, u);
                arc.Add(o + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f) + Up(h1) + Vector3.back * .25f);
            }

            ExpLine(t, "across", dot * .36f, Color.white * .9f, Factory.GlowIntensity, arc.ToArray());
            Column(t, m);
            Vector3 land = bm + Up(h1);
            if (m == 1) ExpDot(t, "one", land + Vector3.back * .3f, dot * 2f, Color.white, AttentionIntensity);
            if (labelSize > 0f) Beside(t, m.ToString(), land + Up(.25f) + Vector3.back * .3f, labelSize * .3f, m == 1 ? Color.white : new Color(.85f, .87f, .96f), m == 1 ? 0f : 1.8f);
            int litNow = ++lit;
            RevealNote note = t.GetComponent<RevealNote>();
            note.FlareAt.Add(land);
            note.OnShow = () => StripLight(litNow);
            n = m;
            hh = h1;
        }
    }

    // ---- the strip above the captions -------------------------------------------------------------------------------------

    static CaptionDisplay StripDisplay() => TourDirector.Instance != null ? TourDirector.Instance.Captions : null;

    static void StripShow(float[] weights, Color[] colors, string[] inner, string[] marks, (int, int, string, Color)[] runs)
    {
        CaptionDisplay d = StripDisplay();
        if (d != null) d.ShowStrip(weights, colors, inner, marks, runs);
    }

    static void StripLight(int lit)
    {
        CaptionDisplay d = StripDisplay();
        if (d != null) d.StripLight(lit);
    }

    /// <summary>Hide the strip above the captions (exp:strip).</summary>
    public static void StripOff()
    {
        CaptionDisplay d = StripDisplay();
        if (d != null) d.HideStrip();
    }
}
