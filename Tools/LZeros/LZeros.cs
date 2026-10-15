using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;

// Zeros of the Dirichlet L-function L(s, χ) with χ(n) = (n/163), the Kronecker symbol of discriminant −163.
// L(½ + it) = Σ_{n ≤ Kq} χ(n) n^(−s) + Σ_a χ(a) [Euler–Maclaurin tail of Σ_{m ≥ K} (mq + a)^(−s)];
// Z(t) = Re(e^{iθ(t)} L(½ + it)) with θ(t) = (t/2) log(q/π) + arg Γ(¾ + it/2) is real (χ odd, root number 1).
static class LZeros
{
    const int Q = 163;
    static readonly int[] Chi = new int[Q];
    static double[] logn, invSqrt;
    static int[] chiN;
    static readonly double[] B2j = { 1.0 / 6, -1.0 / 30, 1.0 / 42, -1.0 / 30, 5.0 / 66, -691.0 / 2730, 7.0 / 6, -3617.0 / 510 };

    static long ModPow(long b, long e, long m) { long r = 1; b %= m; while (e > 0) { if ((e & 1) == 1) r = r * b % m; b = b * b % m; e >>= 1; } return r; }

    static int KFor(double t) => (int)Math.Ceiling(Math.Abs(t) / (2 * Math.PI) * 4.0) + 12;

    public static Complex L(double t)
    {
        int K = KFor(t), N = K * Q;
        double re = 0, im = 0;
        for (int n = 1; n <= N; n++)
        {
            int c = chiN[n];
            if (c == 0) continue;
            double a = t * logn[n], w = c * invSqrt[n];
            re += w * Math.Cos(a);
            im -= w * Math.Sin(a);
        }

        Complex s = new(0.5, t), tail = Complex.Zero;
        for (int a = 1; a < Q; a++)
        {
            int c = Chi[a];
            if (c == 0) continue;
            double x = (double)K * Q + a, lx = Math.Log(x);
            Complex xs = Complex.Exp(-s * lx);                       // x^(−s)
            Complex term = x * xs / (Q * (s - 1)) + xs / 2;            // ∫ + f(K)/2
            Complex rising = s;                                        // (s)_{2j−1}
            double qx = Q / x;                                         // (q/x)^{2j−1}
            double fact = 2;                                           // (2j)!
            for (int j = 1; j <= 8; j++)
            {
                term += B2j[j - 1] / fact * rising * qx * xs;
                rising *= (s + 2 * j - 1) * (s + 2 * j);
                qx *= (Q / x) * (Q / x);
                fact *= (2 * j + 1) * (2 * j + 2);
            }

            tail += c * term;
        }

        return new Complex(re, im) + tail;
    }

    static Complex LogGamma(Complex z)
    {
        Complex shift = Complex.Zero;
        for (int k = 0; k < 10; k++) shift += Complex.Log(z + k);
        Complex w = z + 10;
        Complex w2 = w * w;
        Complex lg = (w - 0.5) * Complex.Log(w) - w + 0.5 * Math.Log(2 * Math.PI)
                     + 1.0 / (12 * w) - 1.0 / (360 * w * w2) + 1.0 / (1260 * w * w2 * w2) - 1.0 / (1680 * w * w2 * w2 * w2);
        return lg - shift;
    }

    public static double Theta(double t) => t / 2 * Math.Log(Q / Math.PI) + LogGamma(new Complex(0.75, t / 2)).Imaginary;

    public static double Z(double t)
    {
        Complex v = Complex.Exp(new Complex(0, Theta(t))) * L(t);
        return v.Real;
    }

    static double Refine(double a, double b, double fa, double fb)
    {
        // Illinois regula falsi on a bracket with a sign change
        int side = 0;
        for (int it = 0; it < 60; it++)
        {
            double c = (a * fb - b * fa) / (fb - fa);
            double fc = Z(c);
            if (Math.Abs(b - a) < 1e-11 || fc == 0) return c;
            if (fc * fb > 0) { b = c; fb = fc; if (side == -1) fa /= 2; side = -1; }
            else { a = c; fa = fc; if (side == 1) fb /= 2; side = 1; }
        }

        return (a + b) / 2;
    }

    static void Main(string[] args)
    {
        double T = args.Length > 0 ? double.Parse(args[0], CultureInfo.InvariantCulture) : 3000;
        string outPath = args.Length > 1 ? args[1] : "lzeros163.txt";
        double step = args.Length > 2 ? double.Parse(args[2], CultureInfo.InvariantCulture) : 0.05;
        for (int a = 0; a < Q; a++) Chi[a] = a == 0 ? 0 : (ModPow(a, (Q - 1) / 2, Q) == 1 ? 1 : -1);
        int Nmax = KFor(T + 1) * Q + 1;
        logn = new double[Nmax + 1]; invSqrt = new double[Nmax + 1]; chiN = new int[Nmax + 1];
        for (int n = 1; n <= Nmax; n++) { logn[n] = Math.Log(n); invSqrt[n] = 1 / Math.Sqrt(n); chiN[n] = Chi[n % Q]; }

        // sample Z on a grid, in parallel
        int samples = (int)Math.Ceiling(T / step) + 1;
        double[] ts = new double[samples], zs = new double[samples];
        for (int i = 0; i < samples; i++) ts[i] = 0.02 + i * step;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Parallel.For(0, samples, i => zs[i] = Z(ts[i]));
        Console.Error.WriteLine($"sampled {samples} points in {sw.Elapsed.TotalSeconds:0.0} s");

        List<(int i, double a, double b, double fa, double fb)> brackets = new();
        for (int i = 0; i + 1 < samples; i++) if (zs[i] * zs[i + 1] < 0) brackets.Add((i, ts[i], ts[i + 1], zs[i], zs[i + 1]));
        // a close pair of zeros inside one grid step leaves no sign change: look for |Z| dipping toward 0 without crossing
        // (a local minimum of |Z| between samples of one sign) and scan those stretches finely
        List<int> dips = new();
        for (int i = 1; i + 1 < samples; i++)
            if (zs[i - 1] * zs[i] > 0 && zs[i] * zs[i + 1] > 0 && Math.Abs(zs[i]) < Math.Abs(zs[i - 1]) && Math.Abs(zs[i]) < Math.Abs(zs[i + 1])) dips.Add(i);
        var extra = new System.Collections.Concurrent.ConcurrentBag<(int i, double a, double b, double fa, double fb)>();
        Parallel.ForEach(dips, i =>
        {
            const int fine = 40;
            double a0 = ts[i - 1], h = (ts[i + 1] - ts[i - 1]) / fine, prevT = a0, prevZ = zs[i - 1];
            for (int k = 1; k <= fine; k++)
            {
                double t = a0 + k * h, z = k == fine ? zs[i + 1] : Z(t);
                if (prevZ * z < 0) extra.Add((i, prevT, t, prevZ, z));
                prevT = t; prevZ = z;
            }
        });
        brackets.AddRange(extra);
        Console.Error.WriteLine($"{dips.Count} near-misses scanned finely, {extra.Count} hidden sign changes found");
        double[] roots = new double[brackets.Count];
        Parallel.For(0, brackets.Count, k => roots[k] = Refine(brackets[k].a, brackets[k].b, brackets[k].fa, brackets[k].fb));
        Array.Sort(roots);
        Console.Error.WriteLine($"{roots.Length} zeros below {T} in {sw.Elapsed.TotalSeconds:0.0} s");

        using StreamWriter w = new(outPath);
        for (int k = 0; k < roots.Length; k++) w.WriteLine($"{k + 1} {roots[k].ToString("R", CultureInfo.InvariantCulture)}");

        // counts against the smooth zero-counting function (T/2π) log(qT/2πe), at checkpoints
        foreach (double c in new[] { 100.0, 500, 1000, 2000, T })
        {
            if (c > T) continue;
            int cnt = 0; foreach (double r in roots) if (r < c) cnt++;
            double smooth = c / (2 * Math.PI) * Math.Log(Q * c / (2 * Math.PI * Math.E));
            Console.Error.WriteLine($"N({c}) = {cnt}, smooth {smooth:0.00}, difference {cnt - smooth:0.00}");
        }

        // spacing check: smallest gaps relative to the mean
        double minGap = double.MaxValue; for (int k = 1; k < roots.Length; k++) minGap = Math.Min(minGap, roots[k] - roots[k - 1]);
        Console.Error.WriteLine($"smallest gap {minGap:0.0000}; first zeros {string.Join(", ", roots[..Math.Min(5, roots.Length)])}");
        Console.Error.WriteLine($"|Z| at the first zero (should be ~0): {Math.Abs(Z(roots[0])):E2}");
    }
}
