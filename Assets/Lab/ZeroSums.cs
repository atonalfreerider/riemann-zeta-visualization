using System;
using System.Collections.Generic;

/// <summary>
/// The zeros of ζ and of the other seven L-functions of Q(ζ30) (ZeroStackData, written by Tools/zero_stack_data.py), summed by
/// the explicit formula (docs/experiments/README.md, sections JA–JF). Shared by the stills (SacksZeroStack) and the model lab
/// (ZerosModel). Characters are indexed as in LabMath.Characters: 0 is ζ, 1 … 7 the characters of (Z/30)* = (Z/15)*.
/// All of it is plain arithmetic on read-only arrays, so it can run off the main thread.
/// </summary>
public static class ZeroSums
{
    /// <summary>E8's exponents: the gold hours of its wheel.</summary>
    public static readonly int[] Gold = { 1, 7, 11, 13, 17, 19, 23, 29 };

    /// <summary>The Gaussian width in γ for the eight L-functions (their zeros are known to t = 1000 = 4σ).</summary>
    public const double SigmaL = 250d;

    /// <summary>χ_k(n) for the primitive character k (conductor 1, 3, 5 or 15), for any n.</summary>
    public static Cpx Chi(int k, long n)
    {
        var (a, b, q, _) = LabMath.Characters[k];
        if (q == 1) return Cpx.One;
        Cpx v = Cpx.One;
        if (a == 1)
        {
            long r = n % 3;
            if (r == 0) return Cpx.Zero;
            if (r == 2) v = -v;
        }

        if (b > 0)
        {
            Cpx p5 = (n % 5) switch { 0 => Cpx.Zero, 1 => Cpx.One, 2 => new Cpx(0d, 1d), 4 => new Cpx(-1d, 0d), _ => new Cpx(0d, -1d) };
            for (int i = 0; i < b; i++) v = v * p5;
        }

        return v;
    }

    public static int Parity(int k)
    {
        var (a, b, q, _) = LabMath.Characters[k];
        return q == 1 ? 0 : (a + b) % 2;
    }

    /// <summary>The smooth side of the explicit formula, ζ's pole and the trivial zeros, in the units of Σ χ(n) Λ(n)/√n δ(z − log n).</summary>
    public static double Smooth(int k, double z)
    {
        double e2 = 1d - Math.Exp(-2d * z);
        if (k == 0) return Math.Exp(z / 2d) - Math.Exp(-2.5d * z) / e2;
        return Parity(k) == 0 ? -Math.Exp(-.5d * z) / e2 : -Math.Exp(-1.5d * z) / e2;
    }

    /// <summary>log p if n = p^j, else 0.</summary>
    public static double Mangoldt(long n) => PrimePower(n, out long p, out _) ? Math.Log(p) : 0d;

    /// <summary>Whether n is a prime power p^j (by trial division; n is small here).</summary>
    public static bool PrimePower(long n, out long p, out int j)
    {
        p = 0;
        j = 0;
        if (n < 2) return false;
        long q = n;
        for (long d = 2; d * d <= q; d++)
        {
            if (q % d != 0) continue;
            p = d;
            while (q % d == 0)
            {
                q /= d;
                j++;
            }

            return q == 1;
        }

        p = n;
        j = 1;
        return true;
    }

    /// <summary>
    /// The zeros of L(s, χ_k) summed at height z = log x with Gaussian weights e^(−γ²/2σ²):
    ///   W(z) = e^(z/2) √(2π)/σ · (S_k(z) − Σ_ρ e^(−γ²/2σ²) e^(iγz)).
    /// By the explicit formula W(z) = Σ_n χ(n) Λ(n) e^((z − log n)/2) e^(−σ²(z − log n)²/2): a tooth of height χ(n)·log p at the
    /// height of each prime power n = p^j, nothing in between (checked against the primes in Tools/zero_stack_data.py).
    /// </summary>
    public sealed class Sum
    {
        readonly double[] gam, w, gamNeg, wNeg;
        readonly int k;

        public double Sigma { get; }
        /// <summary>How many zeros (with positive imaginary part, of χ and of its conjugate) go into the sum.</summary>
        public int Zeros => gam.Length + (gamNeg?.Length ?? 0);

        /// <param name="first">For ζ: use only the first zeros (the lab lowers it to show the teeth blur).</param>
        public Sum(int k, double sigma, double cut, int first = int.MaxValue)
        {
            this.k = k;
            Sigma = sigma;
            double[] pos = k == 0 ? ZeroStackData.Zeta : ZeroStackData.L[k];
            (gam, w) = Weighted(pos, sigma, cut, first);
            int c = k switch { 4 => 5, 5 => 4, 6 => 7, 7 => 6, _ => k };
            if (c != k) (gamNeg, wNeg) = Weighted(ZeroStackData.L[c], sigma, cut, first);
        }

        static (double[], double[]) Weighted(double[] g, double sigma, double cut, int first)
        {
            List<double> a = new(), b = new();
            for (int i = 0; i < g.Length && i < first; i++)
            {
                double x = g[i];
                if (x > cut) break;
                a.Add(x);
                b.Add(Math.Exp(-.5d * (x / sigma) * (x / sigma)));
            }

            return (a.ToArray(), b.ToArray());
        }

        public Cpx At(double z)
        {
            double re = 0d, im = 0d;
            if (gamNeg == null)
            {
                // a real character: its zeros come in ±γ, the imaginary parts cancel
                for (int i = 0; i < gam.Length; i++) re += w[i] * Math.Cos(gam[i] * z);
                re *= 2d;
            }
            else
            {
                for (int i = 0; i < gam.Length; i++)
                {
                    double a = gam[i] * z;
                    re += w[i] * Math.Cos(a);
                    im += w[i] * Math.Sin(a);
                }

                for (int i = 0; i < gamNeg.Length; i++)
                {
                    double a = gamNeg[i] * z;
                    re += wNeg[i] * Math.Cos(a);
                    im -= wNeg[i] * Math.Sin(a);
                }
            }

            double f = Math.Exp(z / 2d) * Math.Sqrt(2d * Math.PI) / Sigma;
            return new Cpx(f * (Smooth(k, z) - re), -f * im);
        }
    }

    static Sum[] lSums;
    static Sum zetaAll;
    static readonly object gate = new();

    /// <summary>The eight sums with σ = 250 and the zeros to t = 1000 (built once).</summary>
    public static Sum[] LSums
    {
        get
        {
            lock (gate)
            {
                if (lSums != null) return lSums;
                Sum[] s = new Sum[8];
                for (int k = 0; k < 8; k++) s[k] = new Sum(k, SigmaL, 1000d);
                return lSums = s;
            }
        }
    }

    /// <summary>ζ alone with all 10 500 zeros (σ = a quarter of the last).</summary>
    public static Sum ZetaAll
    {
        get
        {
            lock (gate) return zetaAll ??= new Sum(0, ZeroStackData.Zeta[^1] / 4d, double.MaxValue);
        }
    }

    /// <summary>ζ with only its first <paramref name="count"/> zeros (σ = a quarter of the last one used).</summary>
    public static Sum ZetaFirst(int count)
    {
        count = Math.Clamp(count, 1, ZeroStackData.Zeta.Length);
        return count == ZeroStackData.Zeta.Length ? ZetaAll : new Sum(0, ZeroStackData.Zeta[count - 1] / 4d, double.MaxValue, count);
    }

    /// <summary>
    /// The eight sums turned back by χ(h) and averaged, (1/8) Σ_k conj χ_k(h) W_k(z), less the powers of 2, 3 and 5, whose
    /// terms are known exactly (Q(ζ30) puts 2 at hour 17 mod 15, 3 and 5 are ramified; E8's wheel has no spoke for them).
    /// What is left is Σ_{n ≡ h (30)} Λ(n) e^((z − log n)/2) e^(−σ²(z − log n)²/2).
    /// </summary>
    public static double Spoke(int h, double z)
    {
        Sum[] sums = LSums;
        Cpx[] w = new Cpx[8];
        for (int k = 0; k < 8; k++) w[k] = sums[k].At(z);
        return SpokeFrom(h, z, w);
    }

    /// <summary>The same from the eight sums already evaluated at z.</summary>
    public static double SpokeFrom(int h, double z, Cpx[] w)
    {
        double re = 0d;
        for (int k = 0; k < 8; k++)
        {
            Cpx c = Chi(k, h);
            re += c.Re * w[k].Re + c.Im * w[k].Im; // Re(conj(c)·w)
        }

        re /= 8d;
        foreach (int p in new[] { 2, 3, 5 })
        {
            for (long m = p; m < 1000000; m *= p)
            {
                double share = p switch { 2 => (m - h) % 15 == 0 ? 1d : 0d, 3 => (m - h) % 5 == 0 ? .5d : 0d, _ => (m - h) % 3 == 0 ? .25d : 0d };
                if (share == 0d) continue;
                double u = z - Math.Log(m);
                if (Math.Abs(u) * SigmaL > 12d) continue;
                re -= share * Math.Log(p) * Math.Exp(u / 2d - .5d * SigmaL * SigmaL * u * u);
            }
        }

        return re;
    }

    /// <summary>Heights to sample: a coarse grid, and fine around each given spike (the teeth are 1/σ wide).</summary>
    public static List<double> Samples(double z0, double z1, double coarse, IEnumerable<double> spikes, double sigma)
    {
        List<double> s = new();
        for (double z = z0; z <= z1; z += coarse) s.Add(z);
        foreach (double c in spikes)
        {
            if (c < z0 || c > z1) continue;
            for (int j = -28; j <= 28; j++) s.Add(c + j * .22d / sigma);
        }

        s.Sort();
        List<double> o = new();
        foreach (double z in s)
            if (o.Count == 0 || z - o[^1] > 1e-9)
                o.Add(z);
        return o;
    }

    /// <summary>The mean of n^(iγ) over the first <paramref name="count"/> zeros of ζ: where all their coils point at the height of n.</summary>
    public static (double re, double im) MeanPhase(int n, int count)
    {
        double[] zs = ZeroStackData.Zeta;
        count = Math.Clamp(count, 1, zs.Length);
        double re = 0d, im = 0d, z = Math.Log(n);
        for (int i = 0; i < count; i++)
        {
            re += Math.Cos(zs[i] * z);
            im += Math.Sin(zs[i] * z);
        }

        return (re / count, im / count);
    }

    /// <summary>Landau's prediction for that mean: −(T/2π)·Λ(n)/√n / N(T), with T the last zero used.</summary>
    public static double LandauMean(int n, int count)
    {
        double[] zs = ZeroStackData.Zeta;
        count = Math.Clamp(count, 1, zs.Length);
        return -zs[count - 1] / (2d * Math.PI) * Mangoldt(n) / Math.Sqrt(n) / count;
    }
}
