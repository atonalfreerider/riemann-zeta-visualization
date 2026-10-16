using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The triangle of the roots of x³ − x − 1 (r₁ = 1.3247, the plastic number, and r₂, r₃ = −0.6624 ± 0.5623i) and the three ways
/// a prime can act on it (section MC): the identity (59: x³ − x − 1 ≡ (x − 4)(x − 13)(x − 42) mod 59), a rotation (2: no root
/// mod 2) and a reflection (5: one root, x ≡ 2). A fourth panel shows a reflection done twice: the identity. Each panel is
/// its own group ("S3 identity", "S3 rotation", "S3 reflection", "S3 square") so a segment can reveal them one at a time.
/// </summary>
public partial class SacksSpiral
{
    static readonly Vector2[] S3Roots = { new(1.3247180f, 0f), new(-.6623590f, .5622795f), new(-.6623590f, -.5622795f) };
    static readonly string[] S3RootNames = { "r<sub>1</sub>", "r<sub>2</sub>", "r<sub>3</sub>" };

    void S3Triangle(Transform g, Vector3 c, float scale, float labelSize, Color edge)
    {
        Vector3 P(int i) => c + new Vector3(S3Roots[i].x, S3Roots[i].y, 0f) * scale;
        ExpLine(g, "triangle", .14f, edge, Factory.SoftGlowIntensity, new[] { P(0), P(1), P(2), P(0) });
        ExpLine(g, "real axis", .06f, new Color(.5f, .55f, .75f) * .6f, Factory.NoGlow, new[] { c + Vector3.left * scale * 1.1f, c + Vector3.right * scale * 1.75f });
        List<Vector3> pts = new();
        List<Color> cols = new();
        List<float> sizes = new();
        for (int i = 0; i < 3; i++)
        {
            pts.Add(P(i));
            cols.Add(new Color(1f, .86f, .45f));
            sizes.Add(scale * .12f);
            Vector3 away = (P(i) - c).normalized;
            ExpLabelWide(g, S3RootNames[i], P(i) + away * scale * .26f, labelSize * .2f, Color.white);
        }

        ExpPointCloud(g, "roots", pts, cols, sizes, Factory.GlowIntensity);
    }

    /// <summary>A curved arrow from root i to root j of the triangle at c, bowing outwards.</summary>
    void S3Arrow(Transform g, Vector3 c, float scale, int i, int j, Color col, float bow = .35f)
    {
        Vector3 a = c + new Vector3(S3Roots[i].x, S3Roots[i].y, 0f) * scale, b = c + new Vector3(S3Roots[j].x, S3Roots[j].y, 0f) * scale;
        Vector3 mid = (a + b) * .5f, outward = (mid - c).normalized;
        if (outward.sqrMagnitude < 1e-6f) outward = Vector3.up;
        Vector3 ctrl = mid + outward * scale * bow;
        const int n = 24;
        Vector3[] pts = new Vector3[n + 1];
        for (int k = 0; k <= n; k++)
        {
            float t = Mathf.Lerp(.16f, .84f, k / (float)n);
            pts[k] = (1 - t) * (1 - t) * a + 2 * (1 - t) * t * ctrl + t * t * b + Vector3.back * .2f;
        }

        ExpLine(g, $"arrow {i}{j}", .2f, col, Factory.GlowIntensity, pts);
        Vector3 tip = pts[n], dir = (pts[n] - pts[n - 2]).normalized, side = Vector3.Cross(dir, Vector3.back).normalized;
        ExpLine(g, $"head {i}{j}", .2f, col, Factory.GlowIntensity,
            new[] { tip - dir * scale * .12f + side * scale * .07f, tip, tip - dir * scale * .12f - side * scale * .07f });
    }

    /// <summary>A loop at root i: the root stays where it is.</summary>
    void S3Fixed(Transform g, Vector3 c, float scale, int i, Color col)
    {
        Vector3 a = c + new Vector3(S3Roots[i].x, S3Roots[i].y, 0f) * scale, outward = (a - c).normalized;
        Vector3 centre = a + outward * scale * .2f;
        Vector3[] ring = new Vector3[33];
        for (int k = 0; k <= 32; k++)
        {
            float t = k / 32f * Mathf.PI * 2f;
            ring[k] = centre + new Vector3(Mathf.Cos(t), Mathf.Sin(t), 0f) * scale * .14f + Vector3.back * .2f;
        }

        ExpLine(g, $"fixed {i}", .16f, col, Factory.GlowIntensity, ring);
    }

    /// <summary>
    /// Four panels in a row at o: the identity (59), a rotation (2), a reflection (5), and a reflection done twice. Above each:
    /// what the prime does to x³ − x − 1; below: its family and share of the primes.
    /// </summary>
    public void ExpS3Roots(Vector3 o, float scale, float gap, float labelSize)
    {
        Color gold = new(1f, .8f, .32f), blue = new(.35f, .85f, 1f), grey = new(.85f, .87f, .95f);
        string[] names = { "S3 identity", "S3 rotation", "S3 reflection", "S3 square" };
        string[] heads = { "p = 59:  three roots", "p = 2:  no root", "p = 5:  one root", "a reflection, twice" };
        string[] lines =
        {
            "x³ − x − 1 ≡ (x − 4)(x − 13)(x − 42)  mod 59",
            "x³ − x − 1 has no root mod 2",
            "x³ − x − 1 ≡ (x − 2)(x² + 2x + 3)  mod 5",
            "swap r<sub>2</sub> and r<sub>3</sub>, then swap them back",
        };
        string[] foot =
        {
            "the identity:  x² + xy + 6y²,  1/6 of the primes",
            "a rotation:  2x² ± xy + 3y²,  1/3 of the primes",
            "a reflection:  neither family,  1/2 of the primes",
            "= the identity:  every reflection squares to it",
        };
        Color[] cols = { gold, blue, grey, gold };
        for (int k = 0; k < 4; k++)
        {
            Transform g = ExpGroup(names[k]).transform;
            Vector3 c = o + Vector3.right * (gap * (k - 1.5f));
            S3Triangle(g, c, scale, labelSize, cols[k] * .8f);
            switch (k)
            {
                case 0:
                    for (int i = 0; i < 3; i++) S3Fixed(g, c, scale, i, gold);
                    break;
                case 1:
                    S3Arrow(g, c, scale, 0, 1, blue);
                    S3Arrow(g, c, scale, 1, 2, blue);
                    S3Arrow(g, c, scale, 2, 0, blue);
                    break;
                case 2:
                    S3Fixed(g, c, scale, 0, grey);
                    S3Arrow(g, c, scale, 1, 2, grey, .25f);
                    S3Arrow(g, c, scale, 2, 1, grey, -.25f);
                    break;
                default:
                    S3Arrow(g, c, scale, 1, 2, grey, .25f);
                    S3Arrow(g, c, scale, 2, 1, grey, -.25f);
                    for (int i = 0; i < 3; i++) S3Fixed(g, c, scale, i, gold);
                    break;
            }

            ExpLabelWide(g, heads[k], c + Vector3.up * scale * 1.55f, labelSize * .2f, cols[k]);
            ExpLabelWide(g, lines[k], c + Vector3.up * scale * 1.2f, labelSize * .14f, new Color(.82f, .85f, .94f));
            ExpLabelWide(g, foot[k], c + Vector3.down * scale * 1.25f, labelSize * .15f, cols[k]);
        }
    }
}
