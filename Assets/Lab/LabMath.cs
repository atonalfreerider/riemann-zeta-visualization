using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>A complex number in double precision: just what the zeta sums of the model lab need.</summary>
public readonly struct Cpx
{
    public readonly double Re, Im;

    public Cpx(double re, double im)
    {
        Re = re;
        Im = im;
    }

    public static readonly Cpx Zero = new(0d, 0d);
    public static readonly Cpx One = new(1d, 0d);

    public double Abs => Math.Sqrt(Re * Re + Im * Im);
    public double Arg => Math.Atan2(Im, Re);

    public static Cpx operator +(Cpx a, Cpx b) => new(a.Re + b.Re, a.Im + b.Im);
    public static Cpx operator -(Cpx a, Cpx b) => new(a.Re - b.Re, a.Im - b.Im);
    public static Cpx operator -(Cpx a) => new(-a.Re, -a.Im);
    public static Cpx operator *(Cpx a, Cpx b) => new(a.Re * b.Re - a.Im * b.Im, a.Re * b.Im + a.Im * b.Re);
    public static Cpx operator *(Cpx a, double k) => new(a.Re * k, a.Im * k);
    public static Cpx operator *(double k, Cpx a) => new(a.Re * k, a.Im * k);
    public static Cpx operator /(Cpx a, double k) => new(a.Re / k, a.Im / k);

    public static Cpx operator /(Cpx a, Cpx b)
    {
        double d = b.Re * b.Re + b.Im * b.Im;
        return new Cpx((a.Re * b.Re + a.Im * b.Im) / d, (a.Im * b.Re - a.Re * b.Im) / d);
    }

    public static Cpx Polar(double r, double angle) => new(r * Math.Cos(angle), r * Math.Sin(angle));

    /// <summary>x^(−s) for real x &gt; 0.</summary>
    public static Cpx PowNeg(double x, Cpx s)
    {
        double l = Math.Log(x);
        return Polar(Math.Exp(-s.Re * l), -s.Im * l);
    }

    public Vector2 ToVector2() => new((float)Re, (float)Im);
    public override string ToString() => $"{Re:0.####}{(Im < 0 ? " − " : " + ")}{Math.Abs(Im):0.####}i";
}

/// <summary>
/// The arithmetic behind the model lab: a smallest-prime-factor sieve, factorizations written with exponents, σ₃, the
/// multiplicative order mod 30, the zeta zeros below 66, and the Hurwitz zeta function by Euler–Maclaurin summation (checked
/// against mpmath to 2·10⁻¹⁰ for t ≤ 60), which splits ζ(½ + it) into one sum per hour of E8's wheel:
/// S_h(s) = Σ_{n ≡ h (mod 30)} n^(−s) = 30^(−s) ζ(s, h/30), with Σ_h S_h = ζ(s).
/// </summary>
public static class LabMath
{
    static int[] spf = new int[2];

    /// <summary>The imaginary parts of the first 15 zeros of ζ on the critical line (mpmath, rounded).</summary>
    public static readonly double[] ZetaZeros =
    {
        14.134725, 21.022040, 25.010858, 30.424876, 32.935062, 37.586178, 40.918719, 43.327073,
        48.005151, 49.773832, 52.970321, 56.446248, 59.347044, 60.831779, 65.112544
    };

    /// <summary>
    /// The lab's gold, for E8's exponent hours (the hours prime to 30) and the primes' tops and labels. The pieces of the
    /// factor columns, which are all primes, use the games' gold (NumberKind.Gold), which stays gold when dimmed.
    /// </summary>
    public static readonly Color Gold = new(1f, .8f, .32f);

    /// <summary>
    /// The eight Dirichlet characters mod 30 (the characters of E8's exponent group (Z/30)* = ⟨7⟩ × ⟨11⟩), named by their values
    /// at 7 and 11: χ = ψ₃^a ψ₅^b with ψ₃ the character mod 3 and ψ₅(2) = i mod 5. Index 0 is the trivial character (ζ).
    /// </summary>
    public static readonly (int a, int b, int conductor, string name)[] Characters =
    {
        (0, 0, 1, "trivial:  ζ"),
        (1, 0, 3, "χ(7) = 1,  χ(11) = −1  (mod 3)"),
        (0, 2, 5, "χ(7) = −1,  χ(11) = 1  (mod 5)"),
        (1, 2, 15, "χ(7) = −1,  χ(11) = −1  (mod 15)"),
        (0, 1, 5, "χ(7) = i,  χ(11) = 1  (mod 5)"),
        (0, 3, 5, "χ(7) = −i,  χ(11) = 1  (mod 5)"),
        (1, 1, 15, "χ(7) = i,  χ(11) = −1  (mod 15)"),
        (1, 3, 15, "χ(7) = −i,  χ(11) = −1  (mod 15)"),
    };

    /// <summary>χ_k(h) for an hour h prime to 30.</summary>
    public static Cpx Character(int k, int h)
    {
        var (a, b, _, _) = Characters[k];
        Cpx v = Cpx.One;
        if (a == 1 && h % 3 == 2) v = -v;
        Cpx psi5 = (h % 5) switch { 2 => new Cpx(0d, 1d), 4 => new Cpx(-1d, 0d), 3 => new Cpx(0d, -1d), _ => Cpx.One };
        for (int i = 0; i < b; i++) v = v * psi5;
        return v;
    }

    /// <summary>The zeros 0 &lt; t ≤ 66.2 on the critical line of L(s, χ_k) for k = 1 … 7 (mpmath; the same as the primitive L's).</summary>
    public static readonly double[][] CharacterZeros =
    {
        new[] { 8.039737, 11.249206, 15.704619, 18.261997, 20.455771, 24.059415, 26.577869, 28.218165, 30.745040, 33.897389, 35.608413, 37.551797, 39.485207, 42.616379, 44.120573, 46.274118, 47.514105, 50.375139, 52.496750, 54.193843, 55.642559, 57.584056, 60.026857, 62.206078, 63.176993, 65.294926 },
        new[] { 6.648453, 9.831444, 11.958846, 16.033821, 17.566994, 19.540733, 22.227405, 24.588466, 26.776096, 28.461035, 29.707909, 33.000456, 34.728813, 35.868638, 38.129185, 39.560573, 41.842439, 44.031290, 45.427300, 46.492727, 48.345662, 51.087752, 52.125902, 53.830445, 55.589280, 56.838866, 58.386117, 61.138865, 62.132905, 63.709543, 64.637360 },
        new[] { 3.057018, 5.343192, 7.267631, 10.133748, 12.159699, 13.489385, 15.240409, 16.633558, 19.103497, 20.662363, 22.176398, 23.367999, 24.903501, 26.319766, 28.657014, 29.692278, 31.364706, 32.323380, 33.781179, 35.097056, 36.995847, 38.757341, 39.440898, 41.228232, 41.997403, 43.426530, 44.925232, 46.724256, 48.003149, 49.079970, 50.284583, 51.642078, 52.456943, 54.330357, 55.789977, 57.128062, 58.226582, 59.192210, 60.726500, 61.402092, 63.079443, 64.820147, 65.726852 },
        new[] { 6.183578, 8.457229, 12.674946, 14.825026, 17.337802, 18.998588, 22.487585, 24.365280, 25.531187, 27.982757, 30.463641, 32.195160, 34.457229, 35.490893, 37.271951, 40.396115, 41.536457, 42.992085, 44.826176, 46.590161, 48.477847, 50.664210, 51.977053, 53.442232, 54.485442, 57.297932, 58.893673, 60.028487, 61.699283, 63.519620, 64.347462 },
        new[] { 4.132904, 9.442931, 11.282896, 14.115464, 16.995904, 19.729055, 21.283047, 22.965576, 26.472789, 27.812470, 29.702781, 31.714038, 33.307746, 36.096527, 37.847618, 39.091616, 40.508934, 43.125435, 45.085823, 46.266414, 48.477660, 49.520112, 51.089988, 53.756673, 55.532551, 56.184383, 57.976453, 59.651545, 61.704026, 63.122751, 65.065142 },
        new[] { 2.734604, 5.243010, 8.414685, 10.187603, 11.907372, 13.337396, 15.366140, 17.772390, 18.977000, 20.627632, 21.883973, 23.338480, 25.388028, 27.194732, 28.398977, 29.665371, 31.192357, 32.158491, 34.259777, 35.819879, 37.222973, 38.279684, 39.617177, 40.869737, 42.125163, 44.320306, 45.166483, 46.960225, 47.530765, 49.113320, 50.143430, 51.586861, 53.505263, 54.556219, 55.707645, 57.158340, 57.800308, 59.366054, 60.487475, 62.346811, 63.589651, 64.527979, 65.844035 },
        new[] { 4.406700, 6.590783, 8.264502, 10.326204, 13.028956, 14.464461, 16.171893, 17.444764, 19.137842, 21.284644, 23.148945, 24.071858, 25.892589, 26.796236, 28.552717, 30.519346, 31.997741, 33.297029, 34.379513, 35.991184, 36.853006, 39.029945, 40.407248, 41.808350, 42.887073, 44.088589, 45.434599, 46.570990, 48.643492, 49.792597, 50.996732, 52.397088, 53.108967, 54.722734, 55.709232, 57.711770, 58.892178, 59.993115, 61.201792, 62.379795, 63.278914, 64.805451, 66.179431 },
    };

    public static double[] ZerosOf(int character) => character == 0 ? ZetaZeros : CharacterZeros[character - 1];

    public static void EnsureSieve(int n)
    {
        if (n < spf.Length) return;
        int size = Math.Max(n + 1, spf.Length * 2);
        spf = new int[size];
        for (int i = 2; i < size; i++)
        {
            if (spf[i] != 0) continue;
            for (long j = i; j < size; j += i)
                if (spf[j] == 0) spf[j] = i;
        }
    }

    /// <summary>The smallest prime factor (n itself for a prime, 1 for 1).</summary>
    public static int Spf(int n)
    {
        if (n < 2) return 1;
        EnsureSieve(n);
        return spf[n];
    }

    public static bool IsPrime(int n) => n >= 2 && Spf(n) == n;

    /// <summary>The prime factors with multiplicity, smallest first: the order in which the path back drops them.</summary>
    public static List<int> Factorize(int n)
    {
        List<int> f = new();
        while (n > 1)
        {
            int p = Spf(n);
            f.Add(p);
            n /= p;
        }

        return f;
    }

    /// <summary>An exponent as rich text: the label font has ² and ³ but not the other superscript digits.</summary>
    public static string Superscript(int k) => k == 2 ? "²" : k == 3 ? "³" : $"<sup>{k}</sup>";

    /// <summary>360 → 2³·3²·5.</summary>
    public static string FactorString(int n)
    {
        if (n < 2) return n.ToString();
        List<int> f = Factorize(n);
        List<string> parts = new();
        for (int i = 0; i < f.Count;)
        {
            int j = i;
            while (j < f.Count && f[j] == f[i]) j++;
            parts.Add(j - i == 1 ? f[i].ToString() : f[i] + Superscript(j - i));
            i = j;
        }

        return string.Join("·", parts);
    }

    /// <summary>The runs of equal primes in the path back: (prime, exponent), smallest first.</summary>
    public static List<(int p, int e)> Runs(int n)
    {
        List<(int, int)> runs = new();
        List<int> f = Factorize(n);
        for (int i = 0; i < f.Count;)
        {
            int j = i;
            while (j < f.Count && f[j] == f[i]) j++;
            runs.Add((f[i], j - i));
            i = j;
        }

        return runs;
    }

    /// <summary>σ₃(n) = the sum of the cubes of n's divisors; E8 has 240 σ₃(n) vectors of norm 2n.</summary>
    public static long Sigma3(int n)
    {
        long s = 1;
        foreach ((int p, int e) in Runs(n))
        {
            long term = 1, pk = 1;
            for (int k = 0; k < e; k++)
            {
                pk *= (long)p * p * p;
                term += pk;
            }

            s *= term;
        }

        return s;
    }

    public static int Gcd(int a, int b)
    {
        while (b != 0) (a, b) = (b, a % b);
        return Math.Abs(a);
    }

    public static int Mod(int a, int m) => ((a % m) + m) % m;

    /// <summary>The multiplicative order of a mod m (0 if a and m share a factor).</summary>
    public static int Order(int a, int m)
    {
        a = Mod(a, m);
        if (Gcd(a, m) != 1) return 0;
        int x = a, k = 1;
        while (x != 1)
        {
            x = x * a % m;
            k++;
        }

        return k;
    }

    // ---- zeta --------------------------------------------------------------------------------------------------------

    static readonly double[] Bernoulli = { 1d / 6, -1d / 30, 1d / 42, -1d / 30, 5d / 66, -691d / 2730, 7d / 6, -3617d / 510 };
    static readonly double[] Factorial2J = { 2d, 24d, 720d, 40320d, 3628800d, 479001600d, 87178291200d, 20922789888000d };

    /// <summary>
    /// The Hurwitz zeta function ζ(s, a) = Σ_{k ≥ 0} (k + a)^(−s) for 0 &lt; a ≤ 1, by Euler–Maclaurin summation with 30 terms and
    /// 8 Bernoulli corrections: accurate to about 10⁻¹⁰ on the critical line up to t ≈ 60, and good to a few digits by t = 100.
    /// </summary>
    public static Cpx Hurwitz(Cpx s, double a)
    {
        const int N = 30;
        Cpx sum = Cpx.Zero;
        for (int k = 0; k < N; k++) sum += Cpx.PowNeg(k + a, s);
        double x = N + a;
        Cpx xs = Cpx.PowNeg(x, s);
        sum += x * xs / (s - Cpx.One) + 0.5 * xs;
        Cpx poch = s, xp = xs / x;
        for (int j = 1; j <= Bernoulli.Length; j++)
        {
            sum += Bernoulli[j - 1] / Factorial2J[j - 1] * poch * xp;
            poch = poch * (s + new Cpx(2 * j - 1, 0d)) * (s + new Cpx(2 * j, 0d));
            xp = xp / (x * x);
        }

        return sum;
    }

    public static Cpx Zeta(Cpx s) => Hurwitz(s, 1d);

    public static Cpx CriticalZeta(double t) => Zeta(new Cpx(.5d, t));

    /// <summary>S_h(½ + it) = Σ_{n ≡ h (mod 30)} n^(−½−it) = 30^(−s) ζ(s, h/30), for the hours h = 1 … 30 (30 is hour 0).</summary>
    public static Cpx HourSum(int h, double t)
    {
        Cpx s = new(.5d, t);
        return Cpx.PowNeg(30d, s) * Hurwitz(s, h / 30d);
    }

    /// <summary>n^(−½−it): the term of n in the zeta sum, as a point of the plane.</summary>
    public static Cpx Term(double n, double t) => Cpx.PowNeg(n, new Cpx(.5d, t));
}
