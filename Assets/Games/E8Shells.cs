using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// E8's lattice, shell by shell: shell n holds the 240 σ₃(n) vectors of norm 2n (E8 = D8 ∪ (D8 + ½·1), coordinates all
/// integers or all half-integers, with an even sum). Vectors are kept doubled (all integers) to stay exact. For the
/// games each shell is drawn as a sphere: its vectors projected to 3D (the Coxeter plane and a third axis) and pushed out
/// to the sphere's surface, so shell n becomes one ball of 240 σ₃(n) points.
/// </summary>
public static class E8Shells
{
    /// <summary>Shell n's vectors, doubled (2v, integer coordinates), in a fixed order.</summary>
    public static List<sbyte[]> Doubled(int n)
    {
        List<sbyte[]> out_ = new();
        int target = 8 * n;                 // |2v|² = 4 · 2n
        int m = (int)System.Math.Sqrt(target);
        for (int parity = 0; parity < 2; parity++)
        {
            List<int> vals = new();
            for (int c = -m; c <= m; c++)
                if (((c % 2) + 2) % 2 == parity) vals.Add(c);
            int[] cur = new int[8];
            Rec(0, target, parity, vals, cur, out_);
        }

        return out_;
    }

    static void Rec(int i, int rem, int parity, List<int> vals, int[] cur, List<sbyte[]> out_)
    {
        if (i == 8)
        {
            if (rem != 0) return;
            int sum = 0;
            for (int j = 0; j < 8; j++) sum += cur[j];
            // the coordinates of v sum to an even number: Σ 2v ≡ 0 (mod 4)
            if (((sum / 2) % 2 + 2) % 2 != 0 || sum % 2 != 0) return;
            sbyte[] v = new sbyte[8];
            for (int j = 0; j < 8; j++) v[j] = (sbyte)cur[j];
            out_.Add(v);
            return;
        }

        int minRest = (7 - i) * (parity == 1 ? 1 : 0);
        foreach (int c in vals)
        {
            int r = rem - c * c;
            if (r < minRest) continue;
            cur[i] = c;
            Rec(i + 1, r, parity, vals, cur, out_);
        }
    }

    static Vector3[] basis;

    /// <summary>
    /// An orthonormal 3 × 8 projection: the Coxeter plane (where E8's roots fall in 8 rings of 30) and one more axis, so
    /// that a shell seen down the third axis shows E8's familiar wheel.
    /// </summary>
    static void Basis(out double[] u1, out double[] u2, out double[] u3)
    {
        double h = .5;
        double[][] simple =
        {
            new[] { h, -h, -h, -h, -h, -h, -h, h }, new double[] { 1, 1, 0, 0, 0, 0, 0, 0 }, new double[] { -1, 1, 0, 0, 0, 0, 0, 0 },
            new double[] { 0, -1, 1, 0, 0, 0, 0, 0 }, new double[] { 0, 0, -1, 1, 0, 0, 0, 0 }, new double[] { 0, 0, 0, -1, 1, 0, 0, 0 },
            new double[] { 0, 0, 0, 0, -1, 1, 0, 0 }, new double[] { 0, 0, 0, 0, 0, -1, 1, 0 },
        };
        double[,] c = Identity();
        foreach (double[] a in simple)
        {
            double aa = Dot(a, a);
            double[,] r = new double[8, 8];
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++) r[i, j] = (i == j ? 1 : 0) - 2 * a[i] * a[j] / aa;
            c = Mul(c, r);
        }

        // the projector onto the eigenplane of angle τ/30, and onto that of angle 7τ/30 for the third axis
        double[,] p1 = Projector(c, 1), p7 = Projector(c, 7);
        double[] e = { 1, .3, .7, .2, .9, .4, .6, .8 };
        u1 = Apply(p1, e);
        Normalize(u1);
        u2 = Apply(c, u1);
        double cth = Dot(u1, u2);
        for (int i = 0; i < 8; i++) u2[i] -= cth * u1[i];
        Normalize(u2);
        u3 = Apply(p7, e);
        Normalize(u3);
    }

    static double[,] Projector(double[,] c, int m)
    {
        double[,] p = new double[8, 8], ck = Identity();
        for (int k = 0; k < 30; k++)
        {
            double w = 2.0 / 30 * System.Math.Cos(2 * System.Math.PI * m * k / 30);
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++) p[i, j] += w * ck[i, j];
            ck = Mul(ck, c);
        }

        return p;
    }

    static double[,] Identity()
    {
        double[,] m = new double[8, 8];
        for (int i = 0; i < 8; i++) m[i, i] = 1;
        return m;
    }

    static double Dot(double[] a, double[] b)
    {
        double s = 0;
        for (int i = 0; i < 8; i++) s += a[i] * b[i];
        return s;
    }

    static void Normalize(double[] a)
    {
        double n = System.Math.Sqrt(Dot(a, a));
        for (int i = 0; i < 8; i++) a[i] /= n;
    }

    static double[] Apply(double[,] m, double[] v)
    {
        double[] r = new double[8];
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++) r[i] += m[i, j] * v[j];
        return r;
    }

    static double[,] Mul(double[,] a, double[,] b)
    {
        double[,] c = new double[8, 8];
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                double s = 0;
                for (int k = 0; k < 8; k++) s += a[i, k] * b[k, j];
                c[i, j] = s;
            }

        return c;
    }

    /// <summary>A doubled vector projected to 3D (half its doubled projection: the vector's own projection).</summary>
    public static Vector3 Project(sbyte[] v2)
    {
        if (basis == null)
        {
            Basis(out double[] u1, out double[] u2, out double[] u3);
            basis = new Vector3[8];
            for (int i = 0; i < 8; i++) basis[i] = new Vector3((float)u1[i], (float)u3[i], (float)u2[i]);
        }

        Vector3 p = Vector3.zero;
        for (int i = 0; i < 8; i++) p += basis[i] * (v2[i] * .5f);
        return p;
    }

    /// <summary>Shell n as points on a sphere of radius r about the origin: each vector's projection, pushed out to r.</summary>
    public static List<Vector3> Sphere(int n, float r)
    {
        List<Vector3> pts = new();
        foreach (sbyte[] v in Doubled(n))
        {
            Vector3 p = Project(v);
            pts.Add(p.sqrMagnitude > 1e-12f ? p.normalized * r : Vector3.up * r);
        }

        return pts;
    }

    /// <summary>240 σ₃(n), the size of shell n, without enumerating it.</summary>
    public static long Count(int n) => 240 * NumberKind.Sigma3(n);
}
