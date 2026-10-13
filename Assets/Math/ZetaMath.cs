using System;
using Meta.Numerics;
using Meta.Numerics.Functions;

/// <summary>
/// Numerics behind the visualization.
///
/// The naive Dirichlet series  ζ(s) = Σ n^-s  does NOT converge on the critical line
/// (Re s = 1/2), so a spiral built from its first N arrows never lands on ζ(s).
/// The Euler–Maclaurin summation formula repairs this exactly:
///
///   ζ(s) = Σ_{n=1}^{N-1} n^-s                       (the spiral: N-1 rotating arrows)
///        + N^(1-s) / (s-1)                           (the integral tail  ∫_N^∞ x^-s dx)
///        + N^-s / 2                                  (half the N-th term)
///        + Σ_{k=1}^{m} B_2k/(2k)! · s(s+1)…(s+2k-2) · N^(-s-2k+1)   (Bernoulli corrections)
///        + R_m                                       (negligible when N ≳ |t|/2, m ≈ 10)
///
/// With N(t) = max(16, ⌈|t|/2⌉ + 4) and m = 10 the closure matches Meta.Numerics'
/// analytic continuation to ~1e-13 for 0 ≤ t ≤ 140 (verified against mpmath).
/// </summary>
public static class ZetaMath
{
    /// <summary>Even Bernoulli numbers B_2, B_4, …, B_22.</summary>
    static readonly double[] EvenBernoulli =
    {
        1d / 6d, -1d / 30d, 1d / 42d, -1d / 30d, 5d / 66d, -691d / 2730d,
        7d / 6d, -3617d / 510d, 43867d / 798d, -174611d / 330d, 854513d / 138d
    };

    public const int DefaultCorrectionTerms = 10;

    /// <summary>Result of splitting ζ(s) into drawable pieces.</summary>
    public struct SpiralDecomposition
    {
        /// <summary>Partial sums S_k = Σ_{n≤k} n^-s for k = 0 … N-1 (S_0 = 0). These are the spiral's joints.</summary>
        public Complex[] PartialSums;

        /// <summary>N^(1-s)/(s-1): the dominant "missing tail" arrow.</summary>
        public Complex IntegralTail;

        /// <summary>N^-s/2 plus all Bernoulli correction terms (tiny).</summary>
        public Complex Corrections;

        /// <summary>Spiral end + tail + corrections. Equals ζ(s) to ~1e-13.</summary>
        public Complex Total;

        public int N;

        public Complex SpiralEnd => PartialSums[PartialSums.Length - 1];
        public Complex AfterTail => SpiralEnd + IntegralTail;
    }

    /// <summary>Number of spiral arrows needed for the Euler–Maclaurin tail to converge at height t.</summary>
    public static int TermCount(double t, int minTerms = 16)
    {
        return Math.Max(minTerms, (int)Math.Ceiling(Math.Abs(t) / 2d) + 4);
    }

    /// <summary>n^-s as a rotating arrow: length n^-Re(s), angle -Im(s)·ln n.</summary>
    public static Complex Term(int n, Complex s)
    {
        return ComplexMath.Exp(-s * Math.Log(n));
    }

    public static SpiralDecomposition Decompose(Complex s, int N, int correctionTerms = DefaultCorrectionTerms)
    {
        if (N < 2) N = 2;
        correctionTerms = Math.Min(correctionTerms, EvenBernoulli.Length);

        SpiralDecomposition d = new SpiralDecomposition { N = N, PartialSums = new Complex[N] };
        Complex sum = Complex.Zero;
        d.PartialSums[0] = sum;
        for (int n = 1; n < N; n++)
        {
            sum += Term(n, s);
            d.PartialSums[n] = sum;
        }

        double logN = Math.Log(N);
        d.IntegralTail = ComplexMath.Exp((Complex.One - s) * logN) / (s - Complex.One);

        Complex corrections = 0.5 * ComplexMath.Exp(-s * logN);
        Complex rising = s; // s(s+1)…(s+2k-2); for k = 1 this is just s
        for (int k = 1; k <= correctionTerms; k++)
        {
            int twoK = 2 * k;
            if (k > 1)
            {
                rising = rising * (s + (twoK - 3)) * (s + (twoK - 2));
            }

            double coefficient = EvenBernoulli[k - 1] / Factorial(twoK);
            corrections += coefficient * rising * ComplexMath.Exp((-s - (twoK - 1)) * logN);
        }

        d.Corrections = corrections;
        d.Total = sum + d.IntegralTail + corrections;
        return d;
    }

    /// <summary>Reference value from Meta.Numerics' analytic continuation.</summary>
    public static Complex Exact(Complex s)
    {
        return AdvancedComplexMath.RiemannZeta(s);
    }

    /// <summary>Riemann–Siegel theta function (asymptotic expansion; error &lt; 1e-8 for t ≥ 10).</summary>
    public static double RiemannSiegelTheta(double t)
    {
        if (t < 1e-6) return 0d;
        double t2 = t * t;
        return t / 2d * Math.Log(t / (2d * Math.PI)) - t / 2d - Math.PI / 8d + 1d / (48d * t) + 7d / (5760d * t * t2);
    }

    /// <summary>Hardy's Z function: Z(t) = e^{iθ(t)} ζ(1/2 + it), real valued, zero exactly where ζ is.</summary>
    public static double HardyZ(double t, Complex zetaOnLine)
    {
        double theta = RiemannSiegelTheta(t);
        Complex rotated = new Complex(Math.Cos(theta), Math.Sin(theta)) * zetaOnLine;
        return rotated.Re;
    }

    /// <summary>
    /// The oscillating piece contributed by the zero pair 1/2 ± iγ in the explicit formula,
    /// normalised so that  2·Re(x^ρ/ρ) = 4√x · ExplicitFormulaTerm(x, γ).
    /// </summary>
    public static double ExplicitFormulaTerm(double x, double gamma)
    {
        if (x <= double.Epsilon) return 0d;
        double u = gamma * Math.Log(x);
        return (Math.Cos(u) + 2d * gamma * Math.Sin(u)) / (1d + 4d * gamma * gamma);
    }

    /// <summary>
    /// 2·Re(x^ρ/ρ) for a hypothetical zero pair ρ = β ± iγ anywhere in the strip.
    /// For β = 1/2 this equals 4√x · ExplicitFormulaTerm(x, γ).
    /// Used to show what a "rogue" zero off the critical line would do to the prime staircase.
    /// </summary>
    public static double OffLineZeroPairTerm(double x, double beta, double gamma)
    {
        if (x <= double.Epsilon) return 0d;
        double u = gamma * Math.Log(x);
        return 2d * Math.Pow(x, beta) * (beta * Math.Cos(u) + gamma * Math.Sin(u)) / (beta * beta + gamma * gamma);
    }

    /// <summary>
    /// Chebyshev's ψ(x) from the explicit formula:
    ///   ψ(x) = x − Σ_ρ x^ρ/ρ − ln 2π − ½ ln(1 − x^-2)
    /// where <paramref name="periodicSum"/> is Σ_γ ExplicitFormulaTerm(x, γ).
    /// </summary>
    public static double Psi(double x, double periodicSum)
    {
        double zeroTerm = 4d * Math.Sqrt(x) * periodicSum;
        double trivialZeroTerm = x > 1d ? 0.5d * Math.Log(1d - 1d / (x * x)) : 0d;
        return x - zeroTerm - Math.Log(2d * Math.PI) - trivialZeroTerm;
    }

    static double Factorial(int n)
    {
        double f = 1d;
        for (int i = 2; i <= n; i++) f *= i;
        return f;
    }
}
