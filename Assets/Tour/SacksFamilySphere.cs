using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// −47's families on the sphere of directions, and their race (docs/experiments/README.md, sections NC–ND; data from
/// Tools/family_sphere_data.py). On the critical line the three family zeta functions form one real curve; written in the
/// L-functions' own coordinates W = (ζL(χ₋₄₇), L(χ₁), L(χ₂)), each L-function's zero set is a coordinate plane, and since
/// Euler products swing so widely in size, one of them usually dominates and the curve lies along an axis. On the dome
/// (directions up to sign, centred on (1, 1, 1)) the three planes are the arcs of a spherical triangle with the axes at its
/// corners: the curve dwells at the corners and crosses each arc at every zero of that arc's L-function, so the zeros bead
/// the arcs near the corners. The second dome is the landscape of mixtures over the same directions: zeros off the line,
/// gold where none are. The race is one point u(x) = −(ν + S(x)) in the characters' coordinates; a family's race is χ(C)·u,
/// and among the three families only the two pair-characters' coordinates decide who is ahead.
/// </summary>
public partial class SacksSpiral
{
    public const float FamilySphereOffset = 108000f;
    static readonly Color[] FamilyLColors = { new(1f, .82f, .34f), new(1f, .45f, .8f), new(.35f, .9f, .85f) };
    static readonly string[] FamilyLNames = { "ζ·L(χ<sub>−47</sub>)", "L(χ<sub>1</sub>)", "L(χ<sub>2</sub>)" };

    // the dome's frame: centre (1, 1, 1)/√3 towards the camera (Vector3.back), the rest across the screen
    static readonly Vector3 DomeC = new Vector3(1f, 1f, 1f).normalized, DomeU1 = new Vector3(1f, -1f, 0f).normalized;
    static Vector3 DomeU2 => Vector3.Cross(DomeC, DomeU1).normalized;

    /// <summary>Where the unit vector w (in the L-functions' coordinates) sits on the sphere of radius R at o.</summary>
    static Vector3 DomeMap(Vector3 o, float R, Vector3 w) =>
        o + R * (Vector3.Dot(w, DomeU1) * Vector3.right + Vector3.Dot(w, DomeU2) * Vector3.up + Vector3.Dot(w, DomeC) * Vector3.back);

    /// <summary>A direction w up to sign, flipped onto the visible hemisphere round DomeC.</summary>
    static Vector3 DomePoint(Vector3 o, float R, Vector3 w)
    {
        w = w.normalized;
        return DomeMap(o, R, Vector3.Dot(w, DomeC) < 0f ? -w : w);
    }

    /// <summary>Corner j of the dome of radius R at o: the direction where L-function j alone is non-zero (for the camera).</summary>
    public static Vector3 FamilyDomeCorner(Vector3 o, float R, int j)
    {
        Vector3 e = Vector3.zero;
        e[j] = 1f;
        return DomePoint(o, R, e);
    }

    /// <summary>The visible half of the great circle w ⊥ n (one L-function's zero set), rim to rim; mid is its point nearest the centre.</summary>
    static Vector3[] DomeArc(Vector3 o, float R, Vector3 n, out Vector3 rimEnd)
    {
        n = n.normalized;
        Vector3 a = Vector3.Cross(n, DomeC).normalized, b = Vector3.Cross(n, a).normalized;   // a on the rim, b towards the centre
        if (Vector3.Dot(b, DomeC) < 0f) b = -b;
        Vector3[] pts = new Vector3[97];
        for (int i = 0; i <= 96; i++)
        {
            float s = Mathf.PI * i / 96f;
            pts[i] = DomeMap(o, R, Mathf.Cos(s) * a + Mathf.Sin(s) * b);   // no flipping: the ends lie on the rim itself
        }

        rimEnd = Vector3.Dot(a, DomeU2) < Vector3.Dot(-a, DomeU2) ? a : -a;   // the lower end, for its label
        return pts;
    }

    /// <summary>The rim of a dome, so the hemisphere reads as a shape.</summary>
    void DomeFrame(Transform g, Vector3 o, float R)
    {
        Vector3[] rim = new Vector3[129];
        for (int i = 0; i <= 128; i++)
        {
            float a = Mathf.PI * 2f * i / 128f;
            rim[i] = o + R * (Mathf.Cos(a) * Vector3.right + Mathf.Sin(a) * Vector3.up);
        }

        ExpLine(g, "rim", .25f, new Color(.55f, .6f, .78f), Factory.SoftGlowIntensity, rim);
    }

    /// <summary>Many short segments, each with its own colour and width, as one mesh (two crossed quads each).</summary>
    static MeshRenderer ColouredSegments(Transform g, string name, List<(Vector3 a, Vector3 b, Color c, float w)> segs, float glow)
    {
        Vector3[] v = new Vector3[segs.Count * 8];
        Color[] col = new Color[v.Length];
        int[] tri = new int[segs.Count * 12];
        for (int i = 0; i < segs.Count; i++)
        {
            (Vector3 a, Vector3 b, Color c, float w) = segs[i];
            Vector3 d = b - a;
            d = d.sqrMagnitude > 1e-12f ? d.normalized : Vector3.right;
            Vector3 n1 = Vector3.Cross(d, Mathf.Abs(d.y) > .9f ? Vector3.right : Vector3.up).normalized * (w * .5f);
            Vector3 n2 = Vector3.Cross(d, n1).normalized * (w * .5f);
            int k = i * 8;
            v[k] = a - n1; v[k + 1] = a + n1; v[k + 2] = b + n1; v[k + 3] = b - n1;
            v[k + 4] = a - n2; v[k + 5] = a + n2; v[k + 6] = b + n2; v[k + 7] = b - n2;
            for (int q = 0; q < 8; q++) col[k + q] = c;
            for (int q = 0; q < 2; q++)
            {
                int o = k + q * 4, t = i * 12 + q * 6;
                tri[t] = o; tri[t + 1] = o + 1; tri[t + 2] = o + 2; tri[t + 3] = o; tri[t + 4] = o + 2; tri[t + 5] = o + 3;
            }
        }

        Mesh mesh = new() { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.vertices = v;
        mesh.colors = col;
        mesh.triangles = tri;
        mesh.RecalculateBounds();
        GameObject go = new(name);
        go.transform.SetParent(g, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = Factory.mainMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        Factory.SetColor(mr, Color.white);
        Factory.SetIntensity(mr, glow);
        return mr;
    }

    /// <summary>
    /// The families' curve on the dome (NC4): the three L-functions' zero arcs (a spherical triangle with the axes at its
    /// corners) and the curve W(t) for t ≤ TMax, bright in the colour of the L-function that carries it near a corner, faint
    /// in between; a bead wherever it crosses an arc, one for every zero of that arc's L-function. With a zoom corner (NC5)
    /// only the visit t = tFrom … tTo is bright, and its beads are labelled with their heights.
    /// </summary>
    public void ExpFamilyCurveDome(Vector3 o, float R, float labelSize, int zoomCorner = -1, float tFrom = 0f, float tTo = 0f)
    {
        Transform g = ExpGroup("NC curve dome").transform;
        bool zoom = zoomCorner >= 0;
        float ws = zoom ? .42f : 1f;
        if (!zoom) DomeFrame(g, o, R);
        for (int j = 0; j < 3; j++)
        {
            Vector3 n = Vector3.zero;
            n[j] = 1f;
            LineRenderer arc = ExpLine(g, $"zeros of {j}", zoom ? .12f : .3f, FamilyLColors[j] * .75f, Factory.NoGlow, DomeArc(o, R, n, out Vector3 end));
            OnTop(arc, 3);
            if (zoom) continue;
            Vector3 at = DomeMap(o, R, end), outward = (at - o).normalized;
            TextBox l = ExpLabelWide(g, FamilyLNames[j] + " = 0", at + outward * (R * .1f) + Vector3.back * .5f, labelSize * .15f, FamilyLColors[j],
                outward.x > .3f ? TextAlignmentOptions.Left : outward.x < -.3f ? TextAlignmentOptions.Right : TextAlignmentOptions.Center);
            Outline(l.TextField);
        }

        // the curve: W never vanishes, so its direction moves continuously; between samples W itself is interpolated (so a
        // quick swing past the origin turns the right way) in steps of at most 1.2°. Each point keeps its height t.
        float[] c = FamilySphereData.Curve;
        int n3 = c.Length / 3;
        float T(float i) => FamilySphereData.CurveT0 + FamilySphereData.CurveStep * i;
        List<(Vector3 w, float t)> q = new();
        void Fill(Vector3 a, float ta, Vector3 b, float tb, int depth)
        {
            if (depth < 9 && Vector3.Angle(a, b) > 1.2f)
            {
                Vector3 m = (a + b) * .5f;
                float tm = (ta + tb) * .5f;
                Fill(a, ta, m, tm, depth + 1);
                Fill(m, tm, b, tb, depth + 1);
            }
            else q.Add((b, tb));
        }

        Vector3 W(int i) => new(c[3 * i], c[3 * i + 1], c[3 * i + 2]);
        q.Add((W(0), T(0)));
        List<Vector3> beads = new();
        List<Color> beadCols = new();
        List<float> beadSizes = new();
        List<(Vector3 z, int arc, float t)> named = new();
        for (int i = 1; i < n3; i++)
        {
            Vector3 a = W(i - 1), b = W(i);
            Fill(a, T(i - 1), b, T(i), 0);
            for (int j = 0; j < 3; j++)
                if (a[j] * b[j] < 0f)
                {
                    float lam = a[j] / (a[j] - b[j]), t = T(i - 1 + lam);
                    Vector3 z = Vector3.Lerp(a, b, lam);
                    bool focus = zoom && t >= tFrom && t <= tTo;
                    if (zoom && !focus) continue;
                    beads.Add(DomePoint(o, R * 1.012f, z));
                    beadCols.Add(FamilyLColors[j]);
                    beadSizes.Add(R * (zoom ? .0062f : .011f));
                    if (focus) named.Add((z.normalized, j, t));
                }
        }

        List<(Vector3, Vector3, Color, float)> segs = new();
        Color faint = new(.13f, .15f, .24f);
        float cosNear = Mathf.Cos(12f * Mathf.Deg2Rad), cosMid = Mathf.Cos(25f * Mathf.Deg2Rad);
        (Color, float) Style(Vector3 m, float t)
        {
            m = m.normalized;
            int j = 0;
            float d = Mathf.Abs(m.x);
            if (Mathf.Abs(m.y) > d) { d = Mathf.Abs(m.y); j = 1; }
            if (Mathf.Abs(m.z) > d) { d = Mathf.Abs(m.z); j = 2; }
            if (zoom)
                return t >= tFrom && t <= tTo ? (Color.Lerp(FamilyLColors[j], Color.white, .35f), R * .0042f) : (FamilyLColors[j] * .22f, R * .0016f);
            return d >= cosNear ? (FamilyLColors[j], R * .0066f * ws) : d >= cosMid ? (FamilyLColors[j] * .5f, R * .005f * ws) : (faint, R * .003f * ws);
        }

        float rc = R * 1.006f;
        for (int i = 0; i + 1 < q.Count; i++)
        {
            Vector3 a = q[i].w.normalized, b = q[i + 1].w.normalized;
            float sa = Vector3.Dot(a, DomeC), sb = Vector3.Dot(b, DomeC), sg = sa >= 0f ? 1f : -1f;
            (Color col, float wid) = Style(a + b, (q[i].t + q[i + 1].t) * .5f);
            if (sa >= 0f == sb >= 0f) segs.Add((DomeMap(o, rc, sg * a), DomeMap(o, rc, sg * b), col, wid));
            else
            {
                // over the rim: on to the rim on this side, then on from the opposite point of the rim
                Vector3 r = Vector3.Lerp(a, b, sa / (sa - sb)).normalized;
                segs.Add((DomeMap(o, rc, sg * a), DomeMap(o, rc, sg * r), col, wid));
                segs.Add((DomeMap(o, rc, -sg * r), DomeMap(o, rc, -sg * b), col, wid));
            }
        }

        MeshRenderer cm = ColouredSegments(g, "families' curve", segs, Factory.GlowIntensity);
        cm.sortingOrder = 5;
        MeshRenderer bm = ExpPointCloud(g, "zeros", beads, beadCols, beadSizes, Factory.GlowIntensity);
        bm.sortingOrder = 6;
        if (zoom)
        {
            // the corner close up: the two arcs through it are the zero sets of the other two L-functions
            Vector3 e = Vector3.zero;
            e[zoomCorner] = 1f;
            Vector3 cp = DomeMap(o, R * 1.01f, e), nrm = (cp - o).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, -nrm).normalized, up = Vector3.Cross(-nrm, right);
            Vector3[] ring = new Vector3[33];
            for (int k = 0; k <= 32; k++)
            {
                float a = Mathf.PI * 2f * k / 32f;
                ring[k] = cp + nrm * .2f + R * .012f * (Mathf.Cos(a) * right + Mathf.Sin(a) * up);
            }

            OnTop(ExpLine(g, "the corner", .08f, Color.white, Factory.GlowIntensity, ring), 8);
            Vector3 lp = cp + nrm * .3f + (up * .55f - right * .62f) * (R * .17f);
            TextBox cl = ExpLabelWide(g, "the corner:  " + FamilyLNames[zoomCorner] + " alone", lp, labelSize * .03f, FamilyLColors[zoomCorner]);
            Outline(cl.TextField);
            ExpLine(g, "corner leader", .05f, FamilyLColors[zoomCorner] * .7f, Factory.NoGlow,
                new[] { cp + nrm * .2f + (lp - cp).normalized * (R * .014f), lp - (lp - cp).normalized * (R * .02f) });
            for (int k = 0; k < 3; k++)
            {
                if (k == zoomCorner) continue;
                int other = 3 - k - zoomCorner;
                Vector3 f = Vector3.zero;
                f[other] = 1f;
                // the arc of L-function k runs from this corner towards the third corner
                Vector3 along = DomePoint(o, R * 1.01f, e * Mathf.Cos(.21f) - f * Mathf.Sin(.21f));
                TextBox al = ExpLabelWide(g, FamilyLNames[k] + " = 0", along + (along - o).normalized * .3f, labelSize * .03f, FamilyLColors[k]);
                Outline(al.TextField);
            }

            // each bead of the visit: its height, and whether it is a zero of ζ itself
            int side = 1;
            foreach ((Vector3 z, int arc, float t) in named)
            {
                Vector3 off = Vector3.zero;
                off[arc] = 1f;
                if (Vector3.Dot(z, off) * side < 0f) off = -off;
                side = -side;
                Vector3 bp = DomePoint(o, R * 1.012f, z), tp = DomePoint(o, R * 1.012f, z + off * .058f);
                bool zeta = false;
                foreach (float g0 in FamilySphereData.ZetaZeros) zeta |= arc == 0 && Mathf.Abs(g0 - t) < .02f;
                TextBox bl = ExpLabelWide(g, $"t = {t:0.00}" + (zeta ? ",  a zero of ζ" : ""), tp + nrm * .3f, labelSize * .024f, FamilyLColors[arc]);
                Outline(bl.TextField);
                ExpLine(g, "bead leader", .04f, FamilyLColors[arc] * .6f, Factory.NoGlow, new[] { bp + (tp - bp) * .25f, bp + (tp - bp) * .75f });
            }

            Vector3 enter = DomePoint(o, R * 1.01f, q.Find(p => p.t >= tFrom).w), leave = DomePoint(o, R * 1.01f, q.FindLast(p => p.t <= tTo).w);
            TextBox el = ExpLabelWide(g, $"in at t = {tFrom:0.00}", enter + nrm * .3f + up * (R * .012f), labelSize * .022f, Color.white);
            Outline(el.TextField);
            TextBox ll = ExpLabelWide(g, $"out at t = {tTo:0.00}", leave + nrm * .3f + up * (R * .012f), labelSize * .022f, Color.white);
            Outline(ll.TextField);
            return;
        }

        // the corners: where one L-function carries the curve
        for (int j = 0; j < 3; j++)
        {
            Vector3 p = FamilyDomeCorner(o, R * 1.01f, j);
            Vector3 outward = p - o;
            outward.z = 0f;
            outward = outward.sqrMagnitude > 1e-4f ? outward.normalized : Vector3.up;
            TextBox l = ExpLabelWide(g, FamilyLNames[j], o + outward * (R * 1.13f) + Vector3.back * .5f, labelSize * .2f, FamilyLColors[j],
                outward.x > .3f ? TextAlignmentOptions.Left : outward.x < -.3f ? TextAlignmentOptions.Right : TextAlignmentOptions.Center);
            Outline(l.TextField);
            ExpLine(g, $"corner leader {j}", .12f, FamilyLColors[j] * .6f, Factory.NoGlow, new[] { p + Vector3.back * .3f, o + outward * (R * 1.06f) });
        }
    }

    /// <summary>
    /// The landscape of mixtures on the dome (NC4): every mixture b0 ζL + b1 L1 + b2 L2 (b up to sign) coloured by its zeros off
    /// the line up to TMax; the L-functions are the corners, joined by their zero arcs as on the curve's dome.
    /// </summary>
    // the landscape's colour steps (lower ends): to t = 100 most mixtures have 20 to 30 zeros off the line
    static readonly int[] DomeSteps = { 0, 1, 7, 13, 21, 31 };
    static readonly string[] DomeStepNames = { "none", "1–6", "7–12", "13–20", "21–30", "31 +" };

    static Color DomeHeight(int off)
    {
        int k = DomeSteps.Length - 1;
        while (off < DomeSteps[k]) k--;
        return FiveHeight(new[] { 0, 1, 3, 6, 10, 15 }[k]);
    }

    public void ExpFamilyLandscapeDome(Vector3 o, float R, float labelSize)
    {
        Transform g = ExpGroup("NC landscape dome").transform;
        int nt = FamilySphereData.NTheta, np = FamilySphereData.NPhi;
        byte[] off = FamilySphereData.Off;
        List<Vector3> verts = new();
        List<Color> cols = new();
        List<int> tris = new();
        for (int i = 0; i <= nt; i++)
            for (int j = 0; j < np; j++)
            {
                float th = FamilySphereData.Theta[i] * Mathf.Deg2Rad, ph = Mathf.PI * 2f * j / np;
                verts.Add(o + R * (Mathf.Sin(th) * Mathf.Cos(ph) * Vector3.right + Mathf.Sin(th) * Mathf.Sin(ph) * Vector3.up + Mathf.Cos(th) * Vector3.back));
                cols.Add(DomeHeight(off[i * np + j]));
            }

        for (int i = 0; i < nt; i++)
            for (int j = 0; j < np; j++)
            {
                int a = i * np + j, b = i * np + (j + 1) % np, c2 = (i + 1) * np + j, d = (i + 1) * np + (j + 1) % np;
                tris.AddRange(new[] { a, c2, b, b, c2, d });
            }

        Mesh mesh = new() { name = "landscape dome", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(verts);
        mesh.SetColors(cols);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        GameObject go = new("landscape dome");
        go.transform.SetParent(g, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = Factory.mainMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.sortingOrder = -10;
        Factory.SetIntensity(mr, Factory.NoGlow);
        DomeFrame(g, o, R);
        // every mixture with all its zeros on the line, as a gold dot over the mesh
        List<Vector3> gold = new();
        for (int i = 0; i <= nt; i++)
            for (int j = 0; j < np; j++)
                if (off[i * np + j] == 0)
                {
                    float th = FamilySphereData.Theta[i] * Mathf.Deg2Rad, ph = Mathf.PI * 2f * j / np;
                    gold.Add(o + R * 1.008f * (Mathf.Sin(th) * Mathf.Cos(ph) * Vector3.right + Mathf.Sin(th) * Mathf.Sin(ph) * Vector3.up + Mathf.Cos(th) * Vector3.back));
                }

        ExpPointCloud(g, "zero-free", gold, gold.ConvertAll(_ => FiveHeight(0)), gold.ConvertAll(_ => R * .011f), Factory.GlowIntensity).sortingOrder = 4;
        for (int j = 0; j < 3; j++)
        {
            Vector3 n = Vector3.zero;
            n[j] = 1f;
            LineRenderer arc = ExpLine(g, $"zeros of {j}", .16f, new Color(.85f, .88f, 1f) * .55f, Factory.NoGlow, DomeArc(o, R * 1.004f, n, out _));
            OnTop(arc, 3);
            Vector3 p = FamilyDomeCorner(o, R * 1.01f, j);
            Vector3[] ring = new Vector3[33];
            for (int k = 0; k <= 32; k++)
            {
                float a = Mathf.PI * 2f * k / 32f;
                ring[k] = p + Vector3.back * .4f + R * .06f * (Mathf.Cos(a) * Vector3.right + Mathf.Sin(a) * Vector3.up);
            }

            OnTop(ExpLine(g, $"L {j}", .18f, Color.white * .8f, Factory.SoftGlowIntensity, ring), 4);
            Vector3 outward = p - o;
            outward.z = 0f;
            outward = outward.sqrMagnitude > 1e-4f ? outward.normalized : Vector3.up;
            TextBox l = ExpLabelWide(g, FamilyLNames[j], o + outward * (R * 1.13f) + Vector3.back * .5f, labelSize * .2f, Color.white,
                outward.x > .3f ? TextAlignmentOptions.Left : outward.x < -.3f ? TextAlignmentOptions.Right : TextAlignmentOptions.Center);
            Outline(l.TextField);
            ExpLine(g, $"corner leader {j}", .12f, Color.white * .5f, Factory.NoGlow, new[] { p + Vector3.back * .3f + (o + outward * (R * 1.06f) - p).normalized * (R * .045f), o + outward * (R * 1.06f) });
        }

        // the legend: zeros off the line up to TMax
        int[] steps = DomeSteps;
        string[] names = DomeStepNames;
        List<(Vector3 c, Vector3 u, Vector3 v, Color col)> keys = new();
        Vector3 row = o + new Vector3(-R * .95f, -R * 1.5f, 0f);
        float gap = R * .38f;
        for (int k = 0; k < steps.Length; k++)
        {
            Vector3 at = row + Vector3.right * (gap * k);
            keys.Add((at, Vector3.right * R * .05f, Vector3.up * R * .05f, DomeHeight(steps[k])));
            ExpLabelWide(g, names[k], at + Vector3.right * (R * .08f), labelSize * .14f, new Color(.82f, .85f, .94f), TextAlignmentOptions.Left);
        }

        ExpQuads(g, "legend", keys, Factory.NoGlow);
        ExpLabelWide(g, $"zeros off the line up to t = {FamilySphereData.TMax:0}:", row + new Vector3(-R * .08f, R * .13f, 0f), labelSize * .14f,
            new Color(.82f, .85f, .94f), TextAlignmentOptions.Left);
    }

    /// <summary>
    /// How the race between −47's three families is won (ND2). The floor is the plane of the two pair-characters'
    /// coordinates (ρ1, ρ2): each family is ahead in one wedge of it (the sign coordinate is the same for all three and
    /// cancels). The race point u(x) = −(ν + S(x)) climbs with log x from 10³ to 10⁷, coloured by the family ahead; its
    /// shadow on the floor is the cloud it sweeps. The fold −ν = (−1, −1), where squaring the primes puts it, is the white
    /// axis it winds round: on the line where the two pairs tie, opposite the principal family's wedge.
    /// </summary>
    public void ExpFamilyRaceColumn(Vector3 o, float s, float height, float labelSize)
    {
        Transform g = ExpGroup("ND race column").transform;
        const float lo = -4f, hi = 2.5f;
        Vector3 P(float u1, float u2, float h) => o + s * (u1 * Vector3.right + u2 * Vector3.forward) + h * Vector3.up;
        float[] chi = FamilySphereData.Chi;
        Color[] famCols = { new(1f, .84f, .35f), new(1f, .43f, .78f), new(.35f, .9f, .82f) };
        string[] famNames = { "x² + xy + 12y² ahead", "2x² ± xy + 6y² ahead", "3x² ± xy + 4y² ahead" };
        int Lead(float u1, float u2)
        {
            int best = 0;
            float bv = float.MinValue;
            for (int k = 0; k < 3; k++)
            {
                float v = chi[3 * k + 1] * u1 + chi[3 * k + 2] * u2;
                if (v > bv) { bv = v; best = k; }
            }

            return best;
        }

        // the floor: each cell in the colour of the family ahead there
        const int cells = 52;
        float step = (hi - lo) / cells;
        List<(Vector3 c, Vector3 u, Vector3 v, Color col)> floor = new();
        for (int i = 0; i < cells; i++)
            for (int j = 0; j < cells; j++)
            {
                float u1 = lo + (i + .5f) * step, u2 = lo + (j + .5f) * step;
                floor.Add((P(u1, u2, 0f), Vector3.right * (step * s * .5f), Vector3.forward * (step * s * .5f), famCols[Lead(u1, u2)] * .2f));
            }

        ExpQuads(g, "floor", floor, Factory.NoGlow);
        // the wedges' edges: where two families tie, from the axis where all three do
        List<(Vector3, Vector3)> edges = new();
        for (int k = 0; k < 3; k++)
            for (int l = k + 1; l < 3; l++)
            {
                Vector2 d = new(-(chi[3 * k + 2] - chi[3 * l + 2]), chi[3 * k + 1] - chi[3 * l + 1]);
                d.Normalize();
                int m = 3 - k - l;
                if (Lead(d.x, d.y) == m || chi[3 * m + 1] * d.x + chi[3 * m + 2] * d.y > chi[3 * k + 1] * d.x + chi[3 * k + 2] * d.y) d = -d;
                float reach = Mathf.Min(d.x > 0 ? hi / d.x : lo / d.x, d.y > 0 ? hi / d.y : lo / d.y);
                edges.Add((P(0f, 0f, .05f), P(d.x * reach, d.y * reach, .05f)));
                edges.Add((P(d.x * reach, d.y * reach, 0f), P(d.x * reach, d.y * reach, height)));
                edges.Add((P(0f, 0f, height), P(d.x * reach, d.y * reach, height)));
            }

        edges.Add((P(0f, 0f, 0f), P(0f, 0f, height)));
        Segments(g, "ties", edges, .16f, new Color(.6f, .64f, .8f) * .7f);
        // the frame and the scale of x up the back corner
        List<(Vector3, Vector3)> frame = new()
        {
            (P(lo, lo, 0), P(hi, lo, 0)), (P(hi, lo, 0), P(hi, hi, 0)), (P(hi, hi, 0), P(lo, hi, 0)), (P(lo, hi, 0), P(lo, lo, 0)),
            (P(lo, lo, 0), P(lo, lo, height)),
        };
        Color axisCol = new(.7f, .74f, .88f);
        for (int e = 3; e <= 7; e++)
        {
            float h = height * (e - 3) / 4f;
            frame.Add((P(lo, lo, h), P(lo - .2f, lo, h)));
            ExpLabelWide(g, $"x = 10<sup>{e}</sup>", P(lo - .35f, lo, h), labelSize * .15f, axisCol, TextAlignmentOptions.Right);
        }

        Segments(g, "frame", frame, .14f, new Color(.55f, .6f, .78f) * .8f);
        ExpLabelWide(g, "ρ<sub>1</sub>:  the coordinate of L(χ<sub>1</sub>)", P((lo + hi) * .5f + .6f, lo - 1.1f, 0f), labelSize * .15f, axisCol);
        ExpLabelWide(g, "ρ<sub>2</sub>:  the coordinate of L(χ<sub>2</sub>)", P(hi + .4f, (lo + hi) * .5f, 0f), labelSize * .15f, axisCol, TextAlignmentOptions.Left);
        // the fold's axis
        ExpLine(g, "fold axis", .3f, Color.white * .85f, Factory.GlowIntensity, new[] { P(-1f, -1f, 0f), P(-1f, -1f, height * 1.03f) });
        TextBox fl = ExpLabelWide(g, "the fold  −ν = (−1, −1):  where squaring the primes puts the race", P(-1f, -1f, height * 1.07f), labelSize * .15f, Color.white);
        Outline(fl.TextField);
        // the race and its shadow
        float[] r = FamilySphereData.Race;
        int n = r.Length / 3;
        List<(Vector3, Vector3, Color, float)> race = new(), shadow = new();
        for (int i = 0; i + 1 < n; i++)
        {
            float a1 = r[3 * i + 1], a2 = r[3 * i + 2], b1 = r[3 * i + 4], b2 = r[3 * i + 5];
            int lead = Lead((a1 + b1) * .5f, (a2 + b2) * .5f);
            race.Add((P(a1, a2, height * i / (n - 1f)), P(b1, b2, height * (i + 1) / (n - 1f)), famCols[lead], .3f));
            shadow.Add((P(a1, a2, .1f), P(b1, b2, .1f), famCols[lead] * .45f, .14f));
        }

        MeshRenderer rm = ColouredSegments(g, "race", race, Factory.GlowIntensity);
        rm.sortingOrder = 7;
        MeshRenderer sm = ColouredSegments(g, "race's shadow", shadow, Factory.NoGlow);
        sm.sortingOrder = 2;
        // the wedges' names, on the floor clear of the race's shadow
        Vector2[] at = { new(1.3f, 1.5f), new(1.4f, -2.4f), new(-3.3f, -1.95f) };
        for (int k = 0; k < 3; k++)
        {
            TextBox l = ExpLabelWide(g, famNames[k], P(at[k].x, at[k].y, .3f), labelSize * .17f, famCols[k]);
            Outline(l.TextField);
        }
    }
}
