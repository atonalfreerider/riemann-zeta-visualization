using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// E8 exactly: the 240 roots in doubled coordinates (integers: all even or all odd, sum ≡ 0 mod 4), Bourbaki's simple roots,
/// reflections, the Coxeter element c = s₁s₂…s₈ (order 30, eight orbits of 30 roots, characteristic polynomial Φ₃₀), the four
/// planes on which c turns by 12m degrees for the exponents m = 1, 7, 11, 13 (paired with 29, 23, 19, 17), and the 2160 lattice
/// vectors of norm 4. Checked in Python: char poly x⁸ + x⁷ − x⁵ − x⁴ − x³ + x + 1, the four planes mutually orthogonal to 10⁻¹⁵,
/// the same eight ring radii in every plane, each Coxeter orbit's four ring indices a permutation (the Galois action).
/// </summary>
public static class E8Geometry
{
    /// <summary>The exponents that label the four planes (each stands for the pair m, 30 − m).</summary>
    public static readonly int[] Exponents = { 1, 7, 11, 13 };

    // orthonormal bases of the eigenplanes (from the eigenvectors of c for e^(2πi m/30), computed in Python)
    static readonly double[][] PlaneU =
    {
        new[] { 0d, 0.119117702264, 0.192736490926, 0.217638871657, 0.192736490926, 0.119117702264, 0d, 0.921933054885 },
        new[] { 0d, 0.383361320524, -0.236930326056, -0.749967911769, -0.236930326056, 0.383361320524, 0d, 0.177043408121 },
        new[] { 0d, -0.352147091614, -0.569785963271, 0.073618788661, -0.569785963271, -0.352147091614, 0d, 0.311854193190 },
        new[] { 0d, -0.463505659945, 0.286462251824, -0.620291646580, 0.286462251824, -0.463505659945, 0d, 0.146430994468 },
    };

    static readonly double[][] PlaneV =
    {
        new[] { -0.096899068700, -0.463505659945, -0.236930326056, 0d, 0.236930326056, 0.463505659945, 0.669823572348, 0d },
        new[] { -0.159410600689, 0.119117702264, 0.569785963271, 0d, -0.569785963271, -0.119117702264, 0.544883582540, 0d },
        new[] { -0.700435986001, 0.383361320524, -0.286462251824, 0d, 0.286462251824, -0.383361320524, 0.226575333889, 0d },
        new[] { -0.688903665535, -0.352147091614, 0.192736490926, 0d, -0.192736490926, 0.352147091614, -0.450668261007, 0d },
    };

    /// <summary>The 240 roots, doubled (a root's real coordinates are these over 2).</summary>
    public static readonly int[][] Roots;
    /// <summary>Bourbaki's simple roots α₁ … α₈, doubled.</summary>
    public static readonly int[][] Simple =
    {
        new[] { 1, -1, -1, -1, -1, -1, -1, 1 },
        new[] { 2, 2, 0, 0, 0, 0, 0, 0 },
        new[] { -2, 2, 0, 0, 0, 0, 0, 0 },
        new[] { 0, -2, 2, 0, 0, 0, 0, 0 },
        new[] { 0, 0, -2, 2, 0, 0, 0, 0 },
        new[] { 0, 0, 0, -2, 2, 0, 0, 0 },
        new[] { 0, 0, 0, 0, -2, 2, 0, 0 },
        new[] { 0, 0, 0, 0, 0, -2, 2, 0 },
    };

    /// <summary>Root index → index of c(root).</summary>
    public static readonly int[] CoxeterNext;
    /// <summary>Root index → its Coxeter orbit (0 … 7, numbered by its ring in the m = 1 plane, innermost first).</summary>
    public static readonly int[] Orbit;
    /// <summary>[plane, orbit] → the ring (0 … 7, innermost first) that the orbit lies on in that plane.</summary>
    public static readonly int[,] RingOfOrbit = new int[4, 8];
    /// <summary>The eight ring radii (the same in every plane).</summary>
    public static readonly double[] RingRadius = new double[8];

    static readonly Dictionary<long, int> index = new();
    static int[][] shell4;

    static E8Geometry()
    {
        List<int[]> roots = new();
        for (int i = 0; i < 8; i++)
            for (int j = i + 1; j < 8; j++)
                foreach (int si in new[] { 2, -2 })
                    foreach (int sj in new[] { 2, -2 })
                    {
                        int[] v = new int[8];
                        v[i] = si;
                        v[j] = sj;
                        roots.Add(v);
                    }

        for (int mask = 0; mask < 256; mask++)
        {
            int minus = 0;
            int[] v = new int[8];
            for (int i = 0; i < 8; i++)
            {
                bool neg = (mask >> i & 1) == 1;
                v[i] = neg ? -1 : 1;
                if (neg) minus++;
            }

            if (minus % 2 == 0) roots.Add(v);
        }

        Roots = roots.ToArray();
        for (int i = 0; i < Roots.Length; i++) index[Key(Roots[i])] = i;

        CoxeterNext = new int[Roots.Length];
        for (int i = 0; i < Roots.Length; i++) CoxeterNext[i] = IndexOf(Coxeter(Roots[i]));

        // radii in the m = 1 plane name the rings; orbits are numbered by their ring there
        List<double> radii = new();
        foreach (int[] r in Roots)
        {
            double rr = Math.Round(Project(r, 0).magnitude, 5);
            if (!radii.Exists(x => Math.Abs(x - rr) < 1e-4)) radii.Add(rr);
        }

        radii.Sort();
        for (int k = 0; k < 8 && k < radii.Count; k++) RingRadius[k] = radii[k];
        Orbit = new int[Roots.Length];
        for (int i = 0; i < Roots.Length; i++) Orbit[i] = RingIndex(Project(Roots[i], 0).magnitude);
        for (int plane = 0; plane < 4; plane++)
            for (int i = 0; i < Roots.Length; i++)
                RingOfOrbit[plane, Orbit[i]] = RingIndex(Project(Roots[i], plane).magnitude);
    }

    static int RingIndex(double r)
    {
        int best = 0;
        for (int k = 1; k < 8; k++)
            if (Math.Abs(RingRadius[k] - r) < Math.Abs(RingRadius[best] - r)) best = k;
        return best;
    }

    static long Key(int[] v)
    {
        long k = 0;
        foreach (int x in v) k = k * 9 + (x + 4);
        return k;
    }

    /// <summary>The index of a root (−1 if v is not a root).</summary>
    public static int IndexOf(int[] v) => index.TryGetValue(Key(v), out int i) ? i : -1;

    /// <summary>The inner product of two vectors given in doubled coordinates (an integer for lattice vectors).</summary>
    public static int Dot(int[] a, int[] b)
    {
        int s = 0;
        for (int i = 0; i < 8; i++) s += a[i] * b[i];
        return s / 4;
    }

    /// <summary>The reflection in the mirror of the root α: v − (v·α) α.</summary>
    public static int[] Reflect(int[] v, int[] alpha)
    {
        int c = Dot(v, alpha);
        int[] w = new int[8];
        for (int i = 0; i < 8; i++) w[i] = v[i] - c * alpha[i];
        return w;
    }

    /// <summary>The Coxeter element: reflect in α₁, then α₂, … then α₈.</summary>
    public static int[] Coxeter(int[] v)
    {
        foreach (int[] a in Simple) v = Reflect(v, a);
        return v;
    }

    /// <summary>The projection of a doubled vector onto eigenplane 0 … 3 (exponent 1, 7, 11, 13).</summary>
    public static Vector2 Project(int[] v, int plane)
    {
        double x = 0d, y = 0d;
        for (int i = 0; i < 8; i++)
        {
            x += v[i] * .5d * PlaneU[plane][i];
            y += v[i] * .5d * PlaneV[plane][i];
        }

        return new Vector2((float)x, (float)y);
    }

    /// <summary>The same for a real 8-vector (used while a reflection is animating).</summary>
    public static Vector2 Project(double[] v, int plane)
    {
        double x = 0d, y = 0d;
        for (int i = 0; i < 8; i++)
        {
            x += v[i] * PlaneU[plane][i];
            y += v[i] * PlaneV[plane][i];
        }

        return new Vector2((float)x, (float)y);
    }

    public static int PlaneOfExponent(int m)
    {
        m = LabMath.Mod(m, 30);
        if (m > 15) m = 30 - m;
        return Array.IndexOf(Exponents, m);
    }

    /// <summary>
    /// The plane that the Galois symmetry ζ₃₀ ↦ ζ₃₀^p (the Frobenius of a prime p, by p mod 30) carries plane <paramref name="plane"/> to:
    /// exponent m goes to ±p·m mod 30. Primes ≡ ±1 fix every plane, ±11 swap them in pairs, ±7 and ±13 cycle all four.
    /// </summary>
    public static int FrobeniusPlane(int plane, int p) => PlaneOfExponent(Exponents[plane] * LabMath.Mod(p, 30));

    /// <summary>The 2160 vectors of norm 4 (doubled): (±2, 0⁷) and (±1⁴, 0⁴) integer, and (±3/2, ±1/2⁷) with an even sum.</summary>
    public static int[][] Shell4
    {
        get
        {
            if (shell4 != null) return shell4;
            List<int[]> list = new();
            int[] values = { -4, -2, 0, 2, 4, -3, -1, 1, 3 };
            void Search(int[] v, int i, int norm, bool odd)
            {
                if (i == 8)
                {
                    if (norm != 16) return;
                    int sum = 0;
                    foreach (int x in v) sum += x;
                    if (sum % 4 == 0) list.Add((int[])v.Clone());
                    return;
                }

                foreach (int x in values)
                {
                    if ((x & 1) == 1 != odd || norm + x * x > 16) continue;
                    v[i] = x;
                    Search(v, i + 1, norm + x * x, odd);
                }

                v[i] = 0;
            }

            Search(new int[8], 0, 0, false);
            Search(new int[8], 0, 0, true);
            shell4 = list.ToArray();
            return shell4;
        }
    }
}
