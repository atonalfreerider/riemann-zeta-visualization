using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The twist on the twist (docs/experiments/README.md, section Q*). Each link of the zeta chain has a perpendicular of the
/// same length, so that link and perpendicular are two sides of one square; as the links turn clockwise by t·log n, each
/// perpendicular (its "fin") turns about its own link by α·log n, orbiting the joint. Summed, the fins are two zeta chains,
/// half at height t − α and half at t + α, in the plane, and the chain at height α out of it. The same second rotation is
/// laid on the coil of E8's logarithmic axis (a coil on the coil) and on the Sacks spiral, where each number's square of side
/// √n is tilted by a zero's phase γ·log n; summed over the zeros, the √n cancels Landau's 1/√n and the fins stand up on the
/// prime powers.
/// </summary>
public partial class SacksSpiral
{
    public const float TwistOffset = 120000f;
    static readonly Color TwUp = new(1f, .78f, .3f), TwDown = new(.3f, .85f, 1f), TwFlat = new(.92f, .93f, 1f);

    static Color TiltColour(float s) => s >= 0f ? Color.Lerp(TwFlat, TwUp, s) : Color.Lerp(TwFlat, TwDown, -s);

    /// <summary>Two triangles of a quad with one colour, appended to the lists.</summary>
    static void AddQuad(List<Vector3> v, List<Color> c, List<int> tr, Vector3 a, Vector3 b, Vector3 d, Vector3 e, Color col)
    {
        int i = v.Count;
        v.Add(a); v.Add(b); v.Add(d); v.Add(e);
        for (int k = 0; k < 4; k++) c.Add(col);
        tr.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3, i, i + 2, i + 1, i, i + 3, i + 2 });
    }

    static MeshRenderer QuadMesh(Transform g, string name, List<Vector3> v, List<Color> c, List<int> tr, float glow, int order)
    {
        Mesh mesh = new() { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(v); mesh.SetColors(c); mesh.SetTriangles(tr, 0); mesh.RecalculateBounds();
        GameObject go = new(name);
        go.transform.SetParent(g, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = Factory.mainMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.sortingOrder = order;
        Factory.SetIntensity(mr, glow);
        return mr;
    }

    /// <summary>
    /// The chain at height t in the floor plane with a square on every link, its far side (the fin) turned about the link by
    /// α·log n: gold where the fin points up, cyan down, white flat. The chain's spokes (n = t/τk) are marked. With finChain,
    /// the fins' own running sum is drawn as a 3D curve beside it.
    /// </summary>
    public void ExpTwistedChain(Vector3 o, double t, double alpha, int nMax, float scale, float labelSize, string title, bool finChain = false, Vector3? finChainAt = null)
    {
        Transform g = ExpGroup($"twisted chain {alpha / t:0.0}").transform;
        Vector3 P(double re, double im, double up) => o + scale * ((float)re * Vector3.right + (float)im * Vector3.forward + (float)up * Vector3.up);
        List<Vector3> v = new(); List<Color> c = new(); List<int> tr = new();
        List<(Vector3, Vector3, Color, float)> links = new(), fins = new();
        double x = 0, y = 0, fx = 0, fy = 0, fz = 0;
        List<Vector3> finPath = new() { finChainAt ?? o };
        for (int n = 1; n <= nMax; n++)
        {
            double ln = System.Math.Log(n), r = 1 / System.Math.Sqrt(n), th = -t * ln, ph = -alpha * ln;
            double lx = r * System.Math.Cos(th), ly = r * System.Math.Sin(th);
            double wx = -System.Math.Sin(th), wy = System.Math.Cos(th);                      // the in-plane perpendicular
            double cx = r * System.Math.Cos(ph) * wx, cy = r * System.Math.Cos(ph) * wy, cz = r * System.Math.Sin(ph);   // the fin
            Vector3 a = P(x, y, 0), b = P(x + lx, y + ly, 0), b2 = P(x + lx + cx, y + ly + cy, cz), a2 = P(x + cx, y + cy, cz);
            float tilt = (float)System.Math.Sin(ph);
            Color col = TiltColour(tilt);
            AddQuad(v, c, tr, a, b, b2, a2, col * .38f);
            links.Add((a, b, new Color(1f, .82f, .34f), scale * .012f));
            fins.Add((b, b2, col, scale * .008f));
            fins.Add((a2, b2, col * .8f, scale * .006f));
            x += lx; y += ly;
            fx += cx; fy += cy; fz += cz;
            if (finChain) finPath.Add((finChainAt ?? o) + scale * ((float)fx * Vector3.right + (float)fy * Vector3.forward + (float)fz * Vector3.up));
        }

        QuadMesh(g, "squares", v, c, tr, Factory.NoGlow, 2);
        ColouredSegments(g, "links", links, Factory.GlowIntensity).sortingOrder = 6;
        ColouredSegments(g, "fins", fins, Factory.SoftGlowIntensity).sortingOrder = 5;
        // the spokes of the chain: where consecutive links differ by k whole turns
        for (int k = 1; k <= 4; k++)
        {
            int n = Mathf.RoundToInt((float)(t / (2 * System.Math.PI * k)));
            if (n < 2 || n > nMax) continue;
            double sx = 0, sy = 0;
            for (int m = 1; m <= n; m++) { double th = -t * System.Math.Log(m); sx += System.Math.Cos(th) / System.Math.Sqrt(m); sy += System.Math.Sin(th) / System.Math.Sqrt(m); }
            Vector3 at = P(sx, sy, 0), lab = at + Vector3.up * (scale * (.55f + .22f * k)) + Vector3.left * (scale * (.75f + .05f * k));
            ExpLine(g, $"spoke leader {k}", scale * .006f, new Color(.8f, .83f, .95f) * .6f, Factory.NoGlow, new[] { at, lab + Vector3.down * (scale * .05f) });
            TextBox l = ExpLabelWide(g, $"spoke {k}  (n ≈ {t / (2 * System.Math.PI * k):0.#})", lab, labelSize * .12f, new Color(.8f, .83f, .95f), TextAlignmentOptions.Right);
            Outline(l.TextField);
        }

        if (finChain)
        {
            List<(Vector3, Vector3, Color, float)> fc = new();
            for (int i = 0; i + 1 < finPath.Count; i++)
                fc.Add((finPath[i], finPath[i + 1], Color.Lerp(new Color(.55f, .45f, 1f), Color.white, i / (float)finPath.Count), scale * .014f));
            ColouredSegments(g, "fin chain", fc, Factory.GlowIntensity).sortingOrder = 7;
        }

        if (!string.IsNullOrEmpty(title))
        {
            TextBox tl = ExpLabelWide(g, title, o + Vector3.up * (scale * 1.6f) + Vector3.forward * (scale * 1.2f), labelSize * .16f, Color.white);
            Outline(tl.TextField);
        }
    }

    /// <summary>
    /// The fins added up at α = t, less their straight road (half the chain at height 0, Σ n^(−½)/2, which never turns): what is
    /// left is half the chain an octave up, at 2t, in the plane, lifted out of it by the chain's own imaginary part. Its
    /// vortices (n = 2t/τk) are marked: the even ones are the chain's spokes, the odd ones its knots.
    /// </summary>
    public void ExpFinChainOctave(Vector3 o, double t, int nMax, float scale, float labelSize)
    {
        Transform g = ExpGroup("fin chain octave").transform;
        List<(Vector3, Vector3, Color, float)> segs = new();
        double fx = 0, fy = 0, fz = 0, maxX = 0;
        Vector3 prev = o;
        var marks = new SortedDictionary<int, int>();
        for (int k = 1; k <= 6; k++) marks[Mathf.RoundToInt((float)(2 * t / (2 * System.Math.PI * k)))] = k;
        List<(int k, Vector3 p, double y, double z)> found = new();
        for (int n = 1; n <= nMax; n++)
        {
            double ln = System.Math.Log(n), r = 1 / System.Math.Sqrt(n);
            // (i/2) n^(−½ − 2it): half the octave chain, turned a quarter; and Im of the chain at t, out of the plane (drawn at 0.35)
            double a2 = -2 * t * ln;
            fx += -.5 * r * System.Math.Sin(a2); fy += .5 * r * System.Math.Cos(a2); fz += -.35 * r * System.Math.Sin(t * ln);
            maxX = System.Math.Max(maxX, fx);
            Vector3 p = o + scale * ((float)fx * Vector3.right + (float)fy * Vector3.forward + (float)fz * Vector3.up);
            segs.Add((prev, p, Color.Lerp(new Color(.55f, .45f, 1f), Color.white, (float)(ln / System.Math.Log(nMax))), scale * .006f));
            prev = p;
            if (marks.TryGetValue(n, out int kk)) found.Add((kk, p, fy, fz));
        }

        ColouredSegments(g, "octave", segs, Factory.GlowIntensity).sortingOrder = 6;
        // the vortices' labels in a column right of the curve, at their own heights, spread at least a little apart
        found.Sort((a, b) => a.z.CompareTo(b.z));
        double last = double.NegativeInfinity, yl = 0;
        foreach (var f in found) yl += f.y / found.Count;
        foreach ((int kk, Vector3 p, double y, double z) in found)
        {
            double zl = System.Math.Max(z, last + .085);
            last = zl;
            Vector3 lab = o + scale * ((float)(maxX + .14) * Vector3.right + (float)yl * Vector3.forward + (float)zl * Vector3.up);
            ExpDot(g, $"octave vortex {kk}", p, scale * .012f, kk % 2 == 0 ? new Color(1f, .82f, .34f) : new Color(.35f, .9f, .9f), AttentionIntensity);
            ExpLine(g, $"octave mark {kk}", scale * .002f, Color.white * .45f, Factory.NoGlow, new[] { p, lab });
            TextBox l = ExpLabelWide(g, $"n ≈ {2 * t / (2 * System.Math.PI * kk):0.#}:  the chain's " + (kk % 2 == 0 ? $"spoke {kk / 2}" : $"knot {(kk - 1) / 2}"),
                lab, labelSize * .1f, kk % 2 == 0 ? new Color(1f, .82f, .34f) : new Color(.35f, .9f, .9f), TextAlignmentOptions.Left);
            Outline(l.TextField);
        }
    }

    /// <summary>
    /// A coil on the coil (E8's logarithmic axis): every n's hand at height log n, one unit long, turned γ·log n, as in the
    /// funnel's coil; at its tip a fin of length finLen perpendicular to the hand, turned about the hand by α·log n. The coil
    /// runs through the hands' tips (gold), the second coil through the fins' tips (cyan).
    /// </summary>
    public void ExpCoilOnCoil(Vector3 o, double gamma, double alpha, int nMax, float radius, float hScale, float finLen, float labelSize, string title)
    {
        Transform g = ExpGroup("coil on coil").transform;
        List<(Vector3, Vector3, Color, float)> hands = new(), fins = new(), coil = new(), coil2 = new();
        Vector3 prevTip = Vector3.zero, prevFin = Vector3.zero;
        int steps = nMax * 8;
        for (int k = 0; k <= steps; k++)
        {
            // the coils as smooth curves in log n
            double u = System.Math.Log(1 + (nMax - 1) * k / (double)steps);
            double th = gamma * u, ph = alpha * u;
            Vector3 dir = new((float)System.Math.Cos(th), 0f, (float)System.Math.Sin(th)), side = new(-(float)System.Math.Sin(th), 0f, (float)System.Math.Cos(th));
            Vector3 axisP = o + Vector3.up * ((float)u * hScale);
            Vector3 tip = axisP + dir * radius;
            Vector3 fin = tip + finLen * ((float)System.Math.Cos(ph) * side + (float)System.Math.Sin(ph) * Vector3.up);
            if (k > 0)
            {
                coil.Add((prevTip, tip, new Color(1f, .82f, .34f), radius * .025f));
                coil2.Add((prevFin, fin, TiltColour((float)System.Math.Sin(ph)), radius * .02f));
            }

            prevTip = tip; prevFin = fin;
        }

        for (int n = 1; n <= nMax; n++)
        {
            double u = System.Math.Log(n), th = gamma * u, ph = alpha * u;
            Vector3 dir = new((float)System.Math.Cos(th), 0f, (float)System.Math.Sin(th)), side = new(-(float)System.Math.Sin(th), 0f, (float)System.Math.Cos(th));
            Vector3 axisP = o + Vector3.up * ((float)u * hScale), tip = axisP + dir * radius;
            Vector3 fin = tip + finLen * ((float)System.Math.Cos(ph) * side + (float)System.Math.Sin(ph) * Vector3.up);
            hands.Add((axisP, tip, new Color(.9f, .9f, 1f) * .55f, radius * .012f));
            fins.Add((tip, fin, TiltColour((float)System.Math.Sin(ph)), radius * .016f));
            if (n == 1 || n == 2 || n == 3 || n == 5 || n == 10 || n == 30)
            {
                TextBox l = ExpLabelWide(g, n.ToString(), tip + dir * (radius * .32f), labelSize * .1f, new Color(.8f, .83f, .95f));
                Outline(l.TextField);
            }
        }

        ExpLine(g, "axis", radius * .02f, Color.white * .6f, Factory.NoGlow, new[] { o, o + Vector3.up * ((float)System.Math.Log(nMax) * hScale * 1.03f) });
        ColouredSegments(g, "hands", hands, Factory.NoGlow).sortingOrder = 3;
        ColouredSegments(g, "fins", fins, Factory.SoftGlowIntensity).sortingOrder = 4;
        ColouredSegments(g, "coil", coil, Factory.GlowIntensity).sortingOrder = 5;
        ColouredSegments(g, "coil on the coil", coil2, Factory.GlowIntensity).sortingOrder = 6;
        TextBox tl = ExpLabelWide(g, title, o + Vector3.down * (radius * .45f), labelSize * .16f, Color.white);
        Outline(tl.TextField);
    }

    /// <summary>
    /// The Sacks spiral with every number's square: the radius (length √n) and a fin of the same length perpendicular to it,
    /// turned about the radius by a zero's phase φ = γ·log n − τ/4 (so the fin's height is −√n cos(γ log n)), drawn at a
    /// fraction finScale of its length. Gold up, cyan down.
    /// </summary>
    public void ExpSacksTwistedSquares(Vector3 o, double gamma, int nMax, float R, float finScale, float labelSize, string title)
    {
        Transform g = ExpGroup("sacks twisted squares").transform;
        ExpEnsureSieve(nMax + 1);
        List<(Vector3, Vector3, Color, float)> fins = new();
        List<Vector3> dots = new(); List<Color> cols = new(); List<float> sizes = new();
        for (int n = 1; n <= nMax; n++)
        {
            double rr = System.Math.Sqrt(n), ang = 2 * System.Math.PI * rr;
            Vector3 radial = new((float)System.Math.Cos(ang), 0f, (float)System.Math.Sin(ang)), side = new(-(float)System.Math.Sin(ang), 0f, (float)System.Math.Cos(ang));
            Vector3 p = o + radial * ((float)rr * R);
            double ph = gamma * System.Math.Log(n) - System.Math.PI / 2;
            float len = (float)rr * R * finScale;
            Vector3 tip = p + len * ((float)System.Math.Cos(ph) * side + (float)System.Math.Sin(ph) * Vector3.up);
            Color col = TiltColour((float)System.Math.Sin(ph));
            fins.Add((p, tip, col, R * .13f));
            dots.Add(p); cols.Add(ExpPrime(n) ? new Color(1f, .82f, .34f) : new Color(.6f, .63f, .78f)); sizes.Add(R * (ExpPrime(n) ? .16f : .1f));
        }

        ColouredSegments(g, "fins", fins, Factory.SoftGlowIntensity).sortingOrder = 4;
        ExpPointCloud(g, "numbers", dots, cols, sizes, Factory.SoftGlowIntensity).sortingOrder = 5;
        TextBox tl = ExpLabelWide(g, title, o + Vector3.up * (R * (float)System.Math.Sqrt(nMax) * .55f) + Vector3.forward * (R * (float)System.Math.Sqrt(nMax) * 1.05f),
            labelSize * .16f, Color.white);
        Outline(tl.TextField);
    }

    /// <summary>
    /// The same squares summed over the first 10 000 zeros (Gaussian taper, ZeroHorizonData's last row): with the pole's share
    /// removed, each number's summed fin stands at Λ̂(n), log p on the prime powers and nothing elsewhere: a needle on every
    /// prime power of the Sacks spiral.
    /// </summary>
    public void ExpSacksZeroNeedles(Vector3 o, int nMax, float R, float hScale, float labelSize, string title)
    {
        Transform g = ExpGroup("sacks zero needles").transform;
        int last = ZeroHorizonData.K.Length - 1, nm = ZeroHorizonData.NMax;
        nMax = Mathf.Min(nMax, nm);
        ExpEnsureSieve(nMax + 1);
        List<(Vector3, Vector3, Color, float)> needles = new();
        List<Vector3> dots = new(); List<Color> cols = new(); List<float> sizes = new();
        for (int n = 2; n <= nMax; n++)
        {
            double rr = System.Math.Sqrt(n), ang = 2 * System.Math.PI * rr;
            Vector3 p = o + new Vector3((float)System.Math.Cos(ang), 0f, (float)System.Math.Sin(ang)) * ((float)rr * R);
            float h = ZeroHorizonData.Teeth[last * (nm - 1) + (n - 2)] / 100f;
            bool pp = h > .35f;
            if (Mathf.Abs(h) > .05f)
                needles.Add((p, p + Vector3.up * (h * hScale), pp ? (ExpPrime(n) ? new Color(1f, .82f, .34f) : new Color(1f, .5f, .3f)) : new Color(.6f, .63f, .78f), R * .12f));
            dots.Add(p); cols.Add(new Color(.55f, .58f, .72f)); sizes.Add(R * .08f);
        }

        ColouredSegments(g, "needles", needles, Factory.GlowIntensity).sortingOrder = 5;
        ExpPointCloud(g, "numbers", dots, cols, sizes, Factory.NoGlow).sortingOrder = 4;
        TextBox tl = ExpLabelWide(g, title, o + Vector3.up * (hScale * 8.5f) + Vector3.forward * (R * (float)System.Math.Sqrt(nMax) * 1.05f), labelSize * .16f, Color.white);
        Outline(tl.TextField);
    }
}
