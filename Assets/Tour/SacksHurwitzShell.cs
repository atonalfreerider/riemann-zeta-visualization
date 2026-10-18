using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// A number's sphere in four dimensions (docs/experiments/README.md, section PC). The Hurwitz quaternions of norm n (odd)
/// number 24 σ(n) (Jacobi), and the 24 units sort them, by left multiplication, into σ(n) rotated copies of the 24-cell. A prime
/// p has p + 1 copies, the fewest any number of its size can have; a composite has more. Drawn by stereographic projection of
/// the unit 3-sphere from the shell's deepest hole, each copy in its own colour, its 96 edges as projected great-circle arcs.
/// The cross-sections cut the 3-sphere by the hyperplanes a = k/2: each is an ordinary sphere of radius √(n − a²).
/// </summary>
public partial class SacksSpiral
{
    public const float HurwitzShellOffset = 116000f;

    // quaternions in doubled coordinates (a, b, c, d all even or all odd): q = (a + bi + cj + dk)/2
    static int[] QMul(int[] x, int[] y) => new[]
    {
        (x[0] * y[0] - x[1] * y[1] - x[2] * y[2] - x[3] * y[3]) / 2, (x[0] * y[1] + x[1] * y[0] + x[2] * y[3] - x[3] * y[2]) / 2,
        (x[0] * y[2] - x[1] * y[3] + x[2] * y[0] + x[3] * y[1]) / 2, (x[0] * y[3] + x[1] * y[2] - x[2] * y[1] + x[3] * y[0]) / 2,
    };

    static List<int[]> hurwitzUnits;

    static List<int[]> HurwitzUnits()
    {
        if (hurwitzUnits != null) return hurwitzUnits;
        hurwitzUnits = new List<int[]>();
        for (int a = -2; a <= 2; a++)
            for (int b = -2; b <= 2; b++)
                for (int c = -2; c <= 2; c++)
                    for (int d = -2; d <= 2; d++)
                        if (a * a + b * b + c * c + d * d == 4 && ((a ^ b) & 1) == 0 && ((a ^ c) & 1) == 0 && ((a ^ d) & 1) == 0)
                            hurwitzUnits.Add(new[] { a, b, c, d });
        return hurwitzUnits;
    }

    static int Cmp(int[] x, int[] y)
    {
        for (int i = 0; i < 4; i++)
            if (x[i] != y[i]) return x[i].CompareTo(y[i]);
        return 0;
    }

    /// <summary>The Hurwitz quaternions of norm n and their orbit (copy of the 24-cell) under left multiplication by the units.</summary>
    static (List<int[]> pts, int[] orbit, int orbits) HurwitzShell(int n)
    {
        int m = 2 * Mathf.CeilToInt(Mathf.Sqrt(n)) + 1;
        List<int[]> pts = new();
        for (int a = -m; a <= m; a++)
            for (int b = -m; b <= m; b++)
                for (int c = -m; c <= m; c++)
                    for (int d = -m; d <= m; d++)
                        if (a * a + b * b + c * c + d * d == 4 * n && ((a ^ b) & 1) == 0 && ((a ^ c) & 1) == 0 && ((a ^ d) & 1) == 0)
                            pts.Add(new[] { a, b, c, d });
        List<int[]> keys = new();
        int[] orbit = new int[pts.Count];
        for (int i = 0; i < pts.Count; i++)
        {
            int[] best = null;
            foreach (int[] u in HurwitzUnits())
            {
                int[] v = QMul(u, pts[i]);
                if (best == null || Cmp(v, best) < 0) best = v;
            }

            int k = keys.FindIndex(x => Cmp(x, best) == 0);
            if (k < 0) { keys.Add(best); k = keys.Count - 1; }
            orbit[i] = k;
        }

        return (pts, orbit, keys.Count);
    }

    static Color OrbitColour(int k, int count) => Color.HSVToRGB(Mathf.Repeat(k * .618034f, 1f), .78f, 1f);

    /// <summary>
    /// The shell of norm n (odd) as σ(n) rotated 24-cells on the 3-sphere, stereographically projected to R³ around o: every point
    /// in the colour of its copy, the edges of one copy bright and the rest faint; arcs that fly far out are clipped.
    /// </summary>
    public void ExpHurwitzShell(Vector3 o, int n, float scale, float labelSize, string title)
    {
        Transform g = ExpGroup($"PC shell {n}").transform;
        (List<int[]> pts, int[] orbit, int orbits) = HurwitzShell(n);
        float rn = 2f * Mathf.Sqrt(n);
        Vector4[] u = new Vector4[pts.Count];
        for (int i = 0; i < pts.Count; i++) u[i] = new Vector4(pts[i][0], pts[i][1], pts[i][2], pts[i][3]) / rn;
        // the pole: the deepest hole of the shell, by a seeded random search
        System.Random rnd = new(7);
        Vector4 pole = Vector4.zero;
        float bestDot = 2f;
        for (int k = 0; k < 3000; k++)
        {
            Vector4 p = new((float)Gauss(rnd), (float)Gauss(rnd), (float)Gauss(rnd), (float)Gauss(rnd));
            p.Normalize();
            float worst = -2f;
            foreach (Vector4 x in u) worst = Mathf.Max(worst, Vector4.Dot(p, x));
            if (worst < bestDot) { bestDot = worst; pole = p; }
        }

        // an orthonormal basis of the hyperplane ⊥ pole
        Vector4[] basis = new Vector4[3];
        Vector4[] seeds = { new(1, 0, 0, 0), new(0, 1, 0, 0), new(0, 0, 1, 0), new(0, 0, 0, 1) };
        int bi = 0;
        foreach (Vector4 sd in seeds)
        {
            if (bi == 3) break;
            Vector4 v = sd - Vector4.Dot(sd, pole) * pole;
            for (int j = 0; j < bi; j++) v -= Vector4.Dot(v, basis[j]) * basis[j];
            if (v.magnitude > 1e-3f) basis[bi++] = v.normalized;
        }

        Vector3 Flat(Vector4 x) => new Vector3(Vector4.Dot(x, basis[0]), Vector4.Dot(x, basis[1]), Vector4.Dot(x, basis[2])) / (1f - Vector4.Dot(x, pole));
        Vector3 Proj(Vector4 x) => o + scale * Flat(x);
        // the copy drawn bright: the one whose points sit nearest the middle of the picture
        int bright = 0;
        float bestR = float.MaxValue;
        for (int i = 0; i < pts.Count; i++)
        {
            float r = Flat(u[i]).magnitude;
            if (r < bestR) { bestR = r; bright = orbit[i]; }
        }
        const float clip = 3.2f;

        List<(Vector3, Vector3, Color, float)> segs = new();
        for (int i = 0; i < pts.Count; i++)
            for (int j = i + 1; j < pts.Count; j++)
            {
                if (orbit[i] != orbit[j]) continue;
                int d2 = 0;
                for (int k = 0; k < 4; k++) d2 += (pts[i][k] - pts[j][k]) * (pts[i][k] - pts[j][k]);
                if (d2 != 4 * n) continue;                                  // neighbours in the 24-cell: one unit apart
                float th = Mathf.Acos(Mathf.Clamp(Vector4.Dot(u[i], u[j]), -1f, 1f));
                bool hi = orbit[i] == bright;
                Color col = OrbitColour(orbit[i], orbits) * (hi ? 1f : .32f);
                Vector3 prev = Flat(u[i]);
                for (int s = 1; s <= 24; s++)
                {
                    float f = s / 24f;
                    Vector4 x = (Mathf.Sin((1 - f) * th) * u[i] + Mathf.Sin(f * th) * u[j]) / Mathf.Sin(th);
                    Vector3 p = Flat(x);
                    if (prev.magnitude < clip && p.magnitude < clip) segs.Add((o + scale * prev, o + scale * p, col, scale * (hi ? .022f : .007f)));
                    prev = p;
                }
            }

        ColouredSegments(g, "24-cells", segs, Factory.SoftGlowIntensity).sortingOrder = 4;
        List<Vector3> dots = new();
        List<Color> cols = new();
        List<float> sizes = new();
        for (int i = 0; i < pts.Count; i++)
        {
            if (Flat(u[i]).magnitude > clip) continue;
            dots.Add(Proj(u[i])); cols.Add(OrbitColour(orbit[i], orbits)); sizes.Add(scale * (orbit[i] == bright ? .05f : .034f));
        }

        ExpPointCloud(g, "points", dots, cols, sizes, Factory.SoftGlowIntensity).sortingOrder = 5;
        TextBox l = ExpLabelWide(g, title, o + Vector3.up * (scale * 3.4f), labelSize * .22f, Color.white);
        Outline(l.TextField);
    }

    static double Gauss(System.Random r) => System.Math.Sqrt(-2 * System.Math.Log(1 - r.NextDouble())) * System.Math.Cos(2 * System.Math.PI * r.NextDouble());

    /// <summary>
    /// The cross-sections of the shell of norm n by the hyperplanes a = k/2: a row of ordinary spheres of radius √(n − a²), each
    /// holding the shell's points with that first coordinate, coloured by their copy of the 24-cell.
    /// </summary>
    public void ExpHurwitzSlices(Vector3 o, int n, float scale, float spacing, float labelSize)
    {
        Transform g = ExpGroup($"PC slices {n}").transform;
        (List<int[]> pts, int[] orbit, int orbits) = HurwitzShell(n);
        int amax = Mathf.FloorToInt(2 * Mathf.Sqrt(n));
        int count = amax + 1;                                   // a ≥ 0; the slices at −a mirror those at a
        Color sphereCol = new(.55f, .6f, .78f);
        for (int a2 = 0; a2 <= amax; a2++)
        {
            Vector3 c = o + Vector3.right * ((a2 - (count - 1) * .5f) * spacing);
            float r2 = n - a2 * a2 / 4f;
            if (r2 <= 0f) continue;
            float r = Mathf.Sqrt(r2) * scale;
            // the sphere as three great circles
            List<(Vector3, Vector3)> rings = new();
            for (int k = 0; k < 48; k++)
            {
                float t0 = Mathf.PI * 2f * k / 48f, t1 = Mathf.PI * 2f * (k + 1) / 48f;
                rings.Add((c + r * new Vector3(Mathf.Cos(t0), Mathf.Sin(t0), 0f), c + r * new Vector3(Mathf.Cos(t1), Mathf.Sin(t1), 0f)));
                rings.Add((c + r * new Vector3(Mathf.Cos(t0), 0f, Mathf.Sin(t0)), c + r * new Vector3(Mathf.Cos(t1), 0f, Mathf.Sin(t1))));
                rings.Add((c + r * new Vector3(0f, Mathf.Cos(t0), Mathf.Sin(t0)), c + r * new Vector3(0f, Mathf.Cos(t1), Mathf.Sin(t1))));
            }

            Segments(g, $"slice {a2}", rings, scale * .035f, sphereCol * .7f);
            List<Vector3> dots = new();
            List<Color> cols = new();
            List<float> sizes = new();
            int here = 0;
            for (int i = 0; i < pts.Count; i++)
                if (pts[i][0] == a2)
                {
                    dots.Add(c + scale * new Vector3(pts[i][1], pts[i][2], pts[i][3]) * .5f);
                    cols.Add(OrbitColour(orbit[i], orbits));
                    sizes.Add(scale * .17f);
                    here++;
                }

            if (dots.Count > 0) ExpPointCloud(g, $"slice points {a2}", dots, cols, sizes, Factory.GlowIntensity).sortingOrder = 5;
            string aText = a2 % 2 == 0 ? (a2 / 2).ToString() : $"{a2}/2";
            ExpLabelWide(g, $"a = {aText}", c + Vector3.down * (Mathf.Sqrt(n) * scale + spacing * .3f), labelSize * .13f, new Color(.75f, .78f, .9f));
            ExpLabelWide(g, $"{here} points", c + Vector3.down * (Mathf.Sqrt(n) * scale + spacing * .58f), labelSize * .13f, new Color(1f, .82f, .34f));
        }
    }
}
