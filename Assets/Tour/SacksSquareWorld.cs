using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// The Sacks spiral's twisted squares, live (section QC; the long-form segment xorbit). Every n ≤ SqN sits at radius √n and
/// angle τ√n, in the plane of the path of partial sums and centred on its origin (PathOrigin), up being out of the plane
/// toward the camera. Each n carries its fin: perpendicular to its radius and, at full size, as long. Parameters (exp:tw):
///   sq.dots    0 … 1: the numbers (the primes gold)
///   sq.fins    0 … 1: the fins grow, lying flat along the spiral
///   sq.tilt    0 … 1: each fin turned about its radius by the first zero's phase, γ₁·log n − τ/4, so that its height is
///              −√n·cos(γ₁ log n)
///   sq.k       0 … 13: the fins summed over the first K zeros, K = 1, 2, 3, 5, 10 … 10 000 (blended between those rows),
///              each sum divided by T_K/τ: by Landau a prime power's fin tends to log p and every other fin to 0
///   sq.h       world units per unit of fin (at the first zero a fin is √n/(γ₁/τ) units: sq.h = 0.675 draws it a quarter
///              as long as its radius)
///   sq.demo    0 … 1: one number's square in full (n = sq.demon, 40 by default): its radius, its fin and the square between
///   sq.hud     0 … 1: the count of zeros in the top-right corner
/// </summary>
public partial class SacksSpiral
{
    const int SqN = 400;
    const float SqR = 1.2f;
    static readonly int[] SqRows = { 1, 2, 3, 5, 10, 20, 50, 100, 200, 500, 1000, 2000, 5000, 10000 };

    class SqScene
    {
        public Transform Root;
        public MeshRenderer Dots;
        public LiveMesh Fins, Demo;
        public float[][] V, H;              // [row][n]: the summed fins' height and sideways part, ÷ T_K/τ
        public double Gamma1;
        public string Sig = "";
        public readonly Vector3[] Heads = new Vector3[SqN + 1];
    }

    SqScene sq;

    Vector3 SqPos(int n)
    {
        double r = Math.Sqrt(n), a = 2 * Math.PI * r;
        return PathOrigin + new Vector3((float)Math.Cos(a), (float)Math.Sin(a), 0f) * ((float)r * SqR);
    }

    /// <summary>Set up the spiral (its numbers, fins and the one full square), sum the zeros' fins and start its frame loop.</summary>
    public void ExpSquareWorld()
    {
        ClearTweens("sq.");
        sq = new SqScene();
        sq.Root = ExpGroup("SQ spiral").transform;
        ExpEnsureSieve(SqN + 1);
        SqSums(sq);
        List<Vector3> pts = new();
        List<Color> cols = new();
        List<float> sizes = new();
        for (int n = 1; n <= SqN; n++)
        {
            bool p = ExpPrime(n);
            pts.Add(SqPos(n));
            cols.Add(p ? new Color(1f, .82f, .34f) : new Color(.6f, .63f, .78f));
            sizes.Add(p ? .3f : .2f);
        }

        sq.Dots = ExpPointCloud(sq.Root, "numbers", pts, cols, sizes, Factory.SoftGlowIntensity);
        sq.Dots.sortingOrder = -8;
        sq.Fins = new LiveMesh(sq.Root, "fins", Factory.SoftGlowIntensity, -9);
        sq.Demo = new LiveMesh(sq.Root, "square", Factory.GlowIntensity, -7);
        StartCoroutine(SquareWorldLoop(sq));
    }

    /// <summary>The fins summed over the first K zeros, for every row K, in one pass over the zeros.</summary>
    static void SqSums(SqScene s)
    {
        double[] zs = Main.Instance.zetaZeroValues;
        int rows = SqRows.Length;
        s.V = new float[rows][];
        s.H = new float[rows][];
        s.Gamma1 = zs.Length > 0 ? zs[0] : 14.134725141734693;
        double[] c = new double[SqN + 1], si = new double[SqN + 1], lg = new double[SqN + 1];
        for (int n = 2; n <= SqN; n++) lg[n] = Math.Log(n);
        int row = 0;
        for (int k = 0; k < zs.Length && row < rows; k++)
        {
            double g = zs[k];
            for (int n = 2; n <= SqN; n++)
            {
                double a = g * lg[n];
                c[n] += Math.Cos(a);
                si[n] += Math.Sin(a);
            }

            if (k + 1 != SqRows[row]) continue;
            double tt = g / (2 * Math.PI);
            float[] v = new float[SqN + 1], h = new float[SqN + 1];
            for (int n = 2; n <= SqN; n++)
            {
                double rt = Math.Sqrt(n);
                v[n] = (float)(-rt * c[n] / tt);
                h[n] = (float)(rt * si[n] / tt);
            }

            s.V[row] = v;
            s.H[row] = h;
            row++;
        }

        // fewer zeros loaded than a row needs: the rows past them repeat the last one
        for (; row < rows; row++)
        {
            s.V[row] = row > 0 ? s.V[row - 1] : new float[SqN + 1];
            s.H[row] = row > 0 ? s.H[row - 1] : new float[SqN + 1];
        }
    }

    IEnumerator SquareWorldLoop(SqScene s)
    {
        while (s == sq && s.Root != null)
        {
            SqUpdate(s);
            yield return null;
        }

        Hud(1, "", Color.white);
    }

    void SqUpdate(SqScene s)
    {
        float dots = Tw("sq.dots", 0f), grow = Tw("sq.fins", 0f), tilt = Tw("sq.tilt", 0f), k = Tw("sq.k", 0f), h = Tw("sq.h", .675f);
        float demo = Tw("sq.demo", 0f), hud = Tw("sq.hud", 0f);
        int demoN = Mathf.Clamp(Mathf.RoundToInt(Tw("sq.demon", 40f)), 1, SqN);
        // the rows either side of sq.k; the count of zeros between them grows geometrically
        int r0 = Mathf.Clamp(Mathf.FloorToInt(k), 0, SqRows.Length - 1), r1 = Mathf.Min(r0 + 1, SqRows.Length - 1);
        float f = Mathf.Clamp01(k - r0);
        int zeros = Mathf.RoundToInt(Mathf.Exp(Mathf.Lerp(Mathf.Log(SqRows[r0]), Mathf.Log(SqRows[r1]), f)));
        Hud(1, hud > .01f ? "zeros: " + zeros.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " ") : "", new Color(1f, 1f, 1f, hud));
        s.Dots.gameObject.SetActive(dots > .01f);
        Factory.SetColor(s.Dots, new Color(1f, 1f, 1f, dots));
        // the fins' width follows the zoom
        Camera cam = Camera.main;
        float dist = cam != null ? Vector3.Distance(cam.transform.position, PathOrigin) : 100f;
        float w = Mathf.Clamp(dist * .0019f, .02f, 2f);
        string sig = $"{grow:F4}|{tilt:F5}|{k:F4}|{h:F3}|{demo:F3}|{demoN}|{w:F3}";
        if (sig == s.Sig) return;
        s.Sig = sig;
        Vector3 up = Vector3.back;
        double norm = s.Gamma1 / (2 * Math.PI);
        s.Fins.Begin();
        for (int n = 2; n <= SqN; n++)
        {
            double rt = Math.Sqrt(n), a = 2 * Math.PI * rt;
            Vector3 p = SqPos(n), side = new((float)-Math.Sin(a), (float)Math.Cos(a), 0f);
            float across, height;
            if (k <= 0f)
            {
                // the first zero's fin (÷ γ₁/τ, as row 0 of the sums), turned by tilt × its phase
                double ph = tilt * (s.Gamma1 * Math.Log(n) - Math.PI / 2), len = rt / norm;
                across = (float)(len * Math.Cos(ph));
                height = (float)(len * Math.Sin(ph));
            }
            else
            {
                across = Mathf.Lerp(s.H[r0][n], s.H[r1][n], f);
                height = Mathf.Lerp(s.V[r0][n], s.V[r1][n], f);
            }

            Vector3 vec = (across * side + height * up) * (h * grow);
            float len2 = Mathf.Sqrt(across * across + height * height);
            s.Heads[n] = p + vec;
            if (grow > .001f) s.Fins.Seg(p, p + vec, TiltColour(len2 > 1e-6f ? height / len2 : 0f), w);
        }

        s.Fins.End();
        // one number's square, in full: its radius, its fin as long, and the square between them, flat in the plane
        s.Demo.Begin();
        if (demo > .01f)
        {
            double rt = Math.Sqrt(demoN), a = 2 * Math.PI * rt;
            Vector3 o = PathOrigin, p = SqPos(demoN), side = new Vector3((float)-Math.Sin(a), (float)Math.Cos(a), 0f) * ((float)rt * SqR);
            Color gold = new(1f, .82f, .34f, demo), white = new(1f, 1f, 1f, demo);
            s.Demo.Quad(o, p, p + side, o + side, new Color(1f, .82f, .34f, .22f * demo));
            s.Demo.Seg(o, p, white, w * 1.4f);
            s.Demo.Seg(p, p + side, gold, w * 1.4f);
            s.Demo.Seg(p + side, o + side, new Color(1f, .82f, .34f, .6f * demo), w * .8f);
            s.Demo.Seg(o + side, o, new Color(1f, 1f, 1f, .6f * demo), w * .8f);
        }

        s.Demo.End();
    }

    /// <summary>For callouts: the tip of n's fin as drawn now (n's place on the spiral before the fins grow).</summary>
    public Vector3 SqFinTip(int n) => sq == null || n < 2 || n > SqN ? PathOrigin : sq.Heads[n] == Vector3.zero ? SqPos(n) : sq.Heads[n] + Vector3.back * .1f;

    /// <summary>For callouts: the middle of n's full square (sq.demo).</summary>
    public Vector3 SqSquareMiddle(int n)
    {
        if (n < 1 || n > SqN) return PathOrigin;
        double rt = Math.Sqrt(n), a = 2 * Math.PI * rt;
        Vector3 side = new Vector3((float)-Math.Sin(a), (float)Math.Cos(a), 0f) * ((float)rt * SqR);
        return PathOrigin + ((SqPos(n) - PathOrigin) + side) * .5f + Vector3.back * .1f;
    }
}
