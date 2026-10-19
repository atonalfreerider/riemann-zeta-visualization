using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>The kinds a counting number is shown as in the games, in order of precedence.</summary>
public enum NumberClass
{
    /// <summary>1: neither prime nor composite.</summary>
    Unit,
    /// <summary>Gold, always.</summary>
    Prime,
    /// <summary>p²: a deep red.</summary>
    PrimeSquare,
    /// <summary>k(k + 1), on the pronic curves (2 = 1 · 2 is a prime, so gold wins).</summary>
    Pronic,
    /// <summary>Every other composite: grey.</summary>
    Composite,
}

/// <summary>
/// The games' colour language and the arithmetic behind it: gold always means prime; a prime's square is a deep red; a
/// number on the pronic curves k(k + 1) is blue; every other composite is grey; 1 is white. Also factorization, the
/// divisors, σ₃ (E8's shells) and the symbolic shorthand that the marble fingerprints compress to.
/// </summary>
public static class NumberKind
{
    // the gold of the primes on the main spiral (Factory.Palette.Zero) and the blue of its pronic route (SacksSpiral.PronicColor)
    public static readonly Color Gold = new(1f, .85f, .1f);
    public static readonly Color DeepRed = new(.72f, .07f, .13f);
    public static readonly Color Pronic = new(.4f, .55f, 1f);
    public static readonly Color Grey = new(.56f, .58f, .66f);
    public static readonly Color Unit = new(.95f, .96f, 1f);

    public static NumberClass Classify(long n)
    {
        if (n <= 1) return NumberClass.Unit;
        if (IsPrime(n)) return NumberClass.Prime;
        long r = ISqrt(n);
        if (r * r == n && IsPrime(r)) return NumberClass.PrimeSquare;
        if (IsPronic(n)) return NumberClass.Pronic;
        return NumberClass.Composite;
    }

    public static Color ColorOf(NumberClass c) => c switch
    {
        NumberClass.Prime => Gold,
        NumberClass.PrimeSquare => DeepRed,
        NumberClass.Pronic => Pronic,
        NumberClass.Composite => Grey,
        _ => Unit,
    };

    public static Color ColorOf(long n) => ColorOf(Classify(n));

    public static string NameOf(NumberClass c) => c switch
    {
        NumberClass.Prime => "prime",
        NumberClass.PrimeSquare => "a prime squared",
        NumberClass.Pronic => "pronic",
        NumberClass.Composite => "composite",
        _ => "one",
    };

    /// <summary>The colour as rich-text hex (for captions and labels).</summary>
    public static string Hex(long n) => "#" + ColorUtility.ToHtmlStringRGB(ColorOf(n));

    public static long ISqrt(long n)
    {
        if (n < 0) return 0;
        long r = (long)System.Math.Sqrt(n);
        while (r * r > n) r--;
        while ((r + 1) * (r + 1) <= n) r++;
        return r;
    }

    public static bool IsPronic(long n)
    {
        if (n < 2) return false;
        long k = ISqrt(n);
        return k * (k + 1) == n;
    }

    /// <summary>Deterministic for every n below 3.3 · 10²⁴ (Miller–Rabin with the first twelve primes as witnesses).</summary>
    public static bool IsPrime(long n)
    {
        if (n < 2) return false;
        foreach (long p in SmallPrimes)
        {
            if (n == p) return true;
            if (n % p == 0) return false;
        }

        long d = n - 1;
        int s = 0;
        while ((d & 1) == 0) { d >>= 1; s++; }
        foreach (long a in SmallPrimes)
        {
            long x = PowMod(a, d, n);
            if (x == 1 || x == n - 1) continue;
            bool composite = true;
            for (int i = 1; i < s; i++)
            {
                x = MulMod(x, x, n);
                if (x == n - 1) { composite = false; break; }
            }

            if (composite) return false;
        }

        return true;
    }

    static readonly long[] SmallPrimes = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37 };

    static long MulMod(long a, long b, long m) => (long)((System.Numerics.BigInteger)a * b % m);

    static long PowMod(long b, long e, long m)
    {
        long r = 1;
        b %= m;
        while (e > 0)
        {
            if ((e & 1) == 1) r = MulMod(r, b, m);
            b = MulMod(b, b, m);
            e >>= 1;
        }

        return r;
    }

    /// <summary>The prime factorization, ascending: [(p, k), …] with n = Π p^k (empty for 1).</summary>
    public static List<(long p, int k)> Factor(long n)
    {
        List<(long, int)> f = new();
        if (n < 2) return f;
        for (long p = 2; p * p <= n; p += p == 2 ? 1 : 2)
        {
            if (n % p != 0) continue;
            int k = 0;
            while (n % p == 0) { n /= p; k++; }
            f.Add((p, k));
        }

        if (n > 1) f.Add((n, 1));
        return f;
    }

    /// <summary>All divisors, ascending.</summary>
    public static List<long> Divisors(long n)
    {
        List<long> d = new() { 1 };
        foreach ((long p, int k) in Factor(n))
        {
            int count = d.Count;
            long pk = 1;
            for (int e = 1; e <= k; e++)
            {
                pk *= p;
                for (int i = 0; i < count; i++) d.Add(d[i] * pk);
            }
        }

        d.Sort();
        return d;
    }

    /// <summary>σ₃(n) = Σ_{d | n} d³: E8's shell n holds 240 σ₃(n) points.</summary>
    public static long Sigma3(long n)
    {
        long s = 0;
        foreach (long d in Divisors(n)) s += d * d * d;
        return s;
    }

    static readonly string Sup = "⁰¹²³⁴⁵⁶⁷⁸⁹";

    /// <summary>
    /// The symbolic shorthand a fingerprint compresses to, e.g. 2³ · 3² · 5. superscriptDigits: Unicode superscripts (for
    /// plain text and files) or TMP rich text &lt;sup&gt; (the label font has ² and ³ but not ⁴–⁹).
    /// </summary>
    public static string Shorthand(long n, bool richText = true)
    {
        if (n < 2) return n.ToString();
        StringBuilder sb = new();
        foreach ((long p, int k) in Factor(n))
        {
            if (sb.Length > 0) sb.Append(" · ");
            sb.Append(p);
            if (k == 1) continue;
            if (richText && k > 3) sb.Append("<sup>").Append(k).Append("</sup>");
            else foreach (char c in k.ToString()) sb.Append(Sup[c - '0']);
        }

        return sb.ToString();
    }

    /// <summary>One line about n: its class and its building blocks.</summary>
    public static string Describe(long n)
    {
        NumberClass c = Classify(n);
        string hex = Hex(n);
        return c switch
        {
            NumberClass.Unit => "1:  the unit, no building blocks",
            NumberClass.Prime => $"<color={hex}>{n}</color>:  prime",
            NumberClass.PrimeSquare => $"<color={hex}>{n}</color> = {Shorthand(n)}:  a prime squared",
            NumberClass.Pronic => Shorthand(n) == $"{ISqrt(n)} · {ISqrt(n) + 1}"
                ? $"<color={hex}>{n}</color> = {ISqrt(n)} · {ISqrt(n) + 1}:  pronic"
                : $"<color={hex}>{n}</color> = {Shorthand(n)} = {ISqrt(n)} · {ISqrt(n) + 1}:  pronic",
            _ => $"<color={hex}>{n}</color> = {Shorthand(n)}",
        };
    }
}
