using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Five families and a landscape of mixtures (docs/experiments/README.md, section LF). Discriminant −47 has class number 5:
/// x² + xy + 12y², 2x² ± xy + 6y² and 3x² ± xy + 4y², three distinct zeta functions and a plane of mixtures
/// Λ₁ + λ₂Λ₂ + λ₃Λ₃. Every mixture has the same functional equation, so on Re s = ½ it is real, and for each t the mixtures
/// that vanish at ½ + it form a line in the plane. Over the plane the terrain is the number of each mixture's zeros (up to
/// t = 50) that lie off the line: all of its zeros by the argument principle, minus its zeros on the line. It is at sea level
/// only round the class group's three L-functions. Checked in Python first (Tools/class_five_data.py writes ClassFiveData.cs).
/// </summary>
public partial class SacksSpiral
{
    public const float ClassFiveOffset = 88000f;
    static readonly string[] FiveNames = { "ζ(s) L(s, χ<sub>−47</sub>)", "L<sub>1</sub>", "L<sub>2</sub>" };

    /// <summary>Zeros of Λ₁ + λ₂Λ₂ + λ₃Λ₃ on Re s = ½ with 0 &lt; t ≤ 50: sign changes along the line.</summary>
    static int FiveOnLine(float l2, float l3)
    {
        float[] e = ClassFiveData.LineE, a = ClassFiveData.Line2, b = ClassFiveData.Line3;
        int n = 0;
        float prev = e[0] + l2 * a[0] + l3 * b[0];
        for (int i = 1; i < e.Length; i++)
        {
            float v = e[i] + l2 * a[i] + l3 * b[i];
            if (v * prev < 0f) n++;
            prev = v;
        }

        return n;
    }

    /// <summary>All zeros of the mixture with 0.3 &lt; t ≤ 50: the change in its argument round the right half of the box, over π.</summary>
    static int FiveTotal(float l2, float l3)
    {
        int s = ClassFiveData.PathStart, last = ClassFiveData.LineE.Length - 1;
        double Re(int k) => k < 0 ? ClassFiveData.LineE[s] + l2 * ClassFiveData.Line2[s] + l3 * ClassFiveData.Line3[s]
            : k >= ClassFiveData.PathReE.Length ? ClassFiveData.LineE[last] + l2 * ClassFiveData.Line2[last] + l3 * ClassFiveData.Line3[last]
            : ClassFiveData.PathReE[k] + l2 * ClassFiveData.PathRe2[k] + l3 * ClassFiveData.PathRe3[k];
        double Im(int k) => k < 0 || k >= ClassFiveData.PathReE.Length ? 0d
            : ClassFiveData.PathImE[k] + l2 * ClassFiveData.PathIm2[k] + l3 * ClassFiveData.PathIm3[k];
        double arg = 0d, pr = Re(-1), pi = Im(-1);
        for (int k = 0; k <= ClassFiveData.PathReE.Length; k++)
        {
            double qr = Re(k), qi = Im(k);
            // the angle from p to q: arg(q / p) = atan2(Im(q p̄), Re(q p̄))
            arg += Math.Atan2(qi * pr - qr * pi, qr * pr + qi * pi);
            pr = qr;
            pi = qi;
        }

        return (int)Math.Round(arg / Math.PI);
    }

    /// <summary>Gold for no zero off the line, then steps: cyan (1–2), blue (3–5), violet (6–9), magenta (10–14), dark red (15 or more).</summary>
    static Color FiveHeight(int off) => off == 0 ? new Color(1f, .8f, .3f)
        : off <= 2 ? new Color(.35f, .9f, .95f)
        : off <= 5 ? new Color(.3f, .5f, .95f)
        : off <= 9 ? new Color(.55f, .38f, .85f)
        : off <= 14 ? new Color(.72f, .28f, .6f)
        : new Color(.45f, .16f, .3f);

    /// <summary>
    /// The terrain of zeros off the line over λ₂, λ₃ ∈ [−3, 3]: a flat top per cell at the height of its count and walls where
    /// the count steps, gold where it is 0. Pins at the three L-functions and at the first family alone.
    /// </summary>
    public void ExpClassLandscape(Vector3 o, float size, float hz, int cells, float labelSize, bool summits = true)
    {
        Transform g = ExpGroup("LF landscape").transform;
        const float lo = -3f, hi = 3f;
        float step = (hi - lo) / cells, unit = size / (hi - lo);
        int[,] off = FiveGrid(cells);

        int top = 0;
        foreach (int v in off) top = Mathf.Max(top, v);
        // λ₂ along x, λ₃ along y, the height towards the camera (−z): with summits, the fewer zeros off the line the higher,
        // so the mixtures with none stand on top
        float Height(int count) => summits ? top - count : count;
        Vector3 At(float l2, float l3, float h) => o + new Vector3((l2 - (lo + hi) * .5f) * unit, (l3 - (lo + hi) * .5f) * unit, -h * hz);
        List<(Vector3 c, Vector3 u, Vector3 v, Color col)> quads = new();
        float half = step * unit * .5f;
        for (int i = 0; i < cells; i++)
            for (int j = 0; j < cells; j++)
            {
                float l2 = lo + (i + .5f) * step, l3 = lo + (j + .5f) * step;
                int h = off[i, j];
                Color col = FiveHeight(h);
                quads.Add((At(l2, l3, Height(h)), Vector3.right * half, Vector3.up * half, col));
                // walls down to each lower neighbour (east and north), facing the camera's side
                if (hz > 0f && i + 1 < cells && off[i + 1, j] != h)
                {
                    float a = Height(h), b = Height(off[i + 1, j]);
                    Vector3 mid = At(l2 + step * .5f, l3, (a + b) * .5f);
                    quads.Add((mid, Vector3.up * half, Vector3.back * (Mathf.Abs(a - b) * hz * .5f), FiveHeight(a > b ? h : off[i + 1, j]) * .55f));
                }

                if (hz > 0f && j + 1 < cells && off[i, j + 1] != h)
                {
                    float a = Height(h), b = Height(off[i, j + 1]);
                    Vector3 mid = At(l2, l3 + step * .5f, (a + b) * .5f);
                    quads.Add((mid, Vector3.right * half, Vector3.back * (Mathf.Abs(a - b) * hz * .5f), FiveHeight(a > b ? h : off[i, j + 1]) * .55f));
                }
            }

        ExpQuads(g, "terrain", quads, Factory.NoGlow);
        // the frame and the axes on the floor
        Color axisCol = new(.7f, .74f, .88f);
        List<(Vector3, Vector3)> frame = new()
        {
            (At(lo, lo, 0), At(hi, lo, 0)), (At(hi, lo, 0), At(hi, hi, 0)), (At(hi, hi, 0), At(lo, hi, 0)), (At(lo, hi, 0), At(lo, lo, 0)),
        };
        for (int k = -3; k <= 3; k++)
        {
            frame.Add((At(k, lo, 0), At(k, lo - .12f, 0)));
            frame.Add((At(lo, k, 0), At(lo - .12f, k, 0)));
            ExpLabelWide(g, k.ToString().Replace("-", "−"), At(k, lo - .42f, 0), labelSize * .14f, axisCol);
            ExpLabelWide(g, k.ToString().Replace("-", "−"), At(lo - .3f, k, 0), labelSize * .14f, axisCol, TextAlignmentOptions.Right);
        }

        Segments(g, "frame", frame, .14f, new Color(.55f, .6f, .78f) * .8f, Factory.NoGlow);
        ExpLabelWide(g, "λ<sub>2</sub>", At(hi + .35f, lo - .1f, 0), labelSize * .18f, axisCol, TextAlignmentOptions.Left);
        ExpLabelWide(g, "λ<sub>3</sub>", At(lo - .2f, hi + .35f, 0), labelSize * .18f, axisCol);
        // pins at the three L-functions and at the first family alone, standing on the terrain
        float pin = summits ? 9f : 0f;
        int Off(float l2, float l3) => off[Mathf.Clamp(Mathf.FloorToInt((l2 - lo) / step), 0, cells - 1), Mathf.Clamp(Mathf.FloorToInt((l3 - lo) / step), 0, cells - 1)];
        Vector3[] labelOffset = { new(-2f, 3.5f, 0f), new(4f, -2f, 0f), new(-4f, 3f, 0f) };
        TextAlignmentOptions[] labelAlign = { TextAlignmentOptions.Right, TextAlignmentOptions.Left, TextAlignmentOptions.Right };
        List<Vector3> rings = new();
        List<Color> ringCols = new();
        List<float> ringSizes = new();
        for (int k = 0; k < 3; k++)
        {
            float l2 = ClassFiveData.PeakL2[k], l3 = ClassFiveData.PeakL3[k];
            Vector3 foot = At(l2, l3, Height(Off(l2, l3)) + .05f);
            ExpLine(g, $"pin {k}", .3f, PrimeGold, Factory.GlowIntensity, new[] { foot, foot + Vector3.back * pin * hz });
            rings.Add(foot + Vector3.back * pin * hz); ringCols.Add(PrimeGold); ringSizes.Add(1.3f);
            ExpLabelWide(g, $"{FiveNames[k]}:  {ClassFiveData.PeakZeros[k]} zeros, all on the line", foot + Vector3.back * pin * hz + labelOffset[k],
                labelSize * .17f, PrimeGold, labelAlign[k]);
        }

        Vector3 z0 = At(0, 0, Height(Off(0, 0)) + .05f);
        rings.Add(z0 + Vector3.back * pin * hz); ringCols.Add(Color.white); ringSizes.Add(1.1f);
        if (pin > 0f) ExpLine(g, "pin first family", .22f, Color.white, Factory.SoftGlowIntensity, new[] { z0, z0 + Vector3.back * pin * hz });
        ExpLabelWide(g, $"x² + xy + 12y² alone:  {FiveTotal(0f, 0f) - FiveOnLine(0f, 0f)} off the line", z0 + Vector3.back * pin * hz + new Vector3(3f, -2.5f, -.3f),
            labelSize * .16f, Color.white, TextAlignmentOptions.Left);
        // gold cells that are not round an L-function: label the biggest such island
        float sx2 = 0f, sy2 = 0f;
        int n2 = 0;
        for (int i = 0; i < cells; i++)
            for (int j = 0; j < cells; j++)
            {
                if (off[i, j] != 0) continue;
                float l2 = lo + (i + .5f) * step, l3 = lo + (j + .5f) * step;
                bool near = false;
                for (int k = 0; k < 3; k++) near |= Mathf.Abs(l2 - ClassFiveData.PeakL2[k]) < .35f && Mathf.Abs(l3 - ClassFiveData.PeakL3[k]) < .35f;
                if (near) continue;
                sx2 += l2; sy2 += l3; n2++;
            }

        if (n2 > 0)
            ExpLabelWide(g, "no L-function here:  no zero off the line below t = 50", At(sx2 / n2, sy2 / n2, Height(0)) + new Vector3(3.5f, -1f, -.3f),
                labelSize * .14f, Color.white, TextAlignmentOptions.Left);
        ExpPointCloud(g, "pin heads", rings, ringCols, ringSizes, Factory.GlowIntensity);
    }
}
