#!/usr/bin/env python3
"""Data for "The prime race on E8's eight spokes" (sections MA–MB of docs/experiments/README.md), written to
Assets/Tour/RaceData.cs.

E_h(x) = (log x / sqrt x)(8 pi(x; 30, h) - pi*(x)) for E8's eight gold hours h, at 2000 points from 10^2 to 10^7, where
pi*(x) = pi(x) - 3 counts the primes on the eight spokes (all but 2, 3, 5). The fair race F_h is the same with every prime
power p^k counted as 1/k (Riemann's Pi(x) in place of pi(x)): the squares p^2 all land on the spokes 1 and 19. The
explicit formula from the zeros of the seven non-trivial L-functions of (Z/30)* (read from Assets/Tour/ZeroStackData.cs,
t <= 1000):  E_h(x) ~ -(c(h) - 1) - sum_{chi != 1} conj chi(h) sum_rho x^(i gamma)/(1/2 + i gamma), c(h) = the number of
square roots of h mod 30 (4 for the squares 1 and 19, 0 for the other six). The shares of log x with each spoke above the
average, measured and predicted (Rubinstein-Sarnak: the zeros' phases taken as independent uniform angles, 3000 draws);
F_h ~ the zero sum alone, with mean 0.

Run from the repository root:  python3 Tools/race_data.py
"""
import cmath, math, os, random, re

GOLD = [1, 7, 11, 13, 17, 19, 23, 29]
OUT = os.path.join("Assets", "Tour", "RaceData.cs")
STRIDE = 4
CHARS = [(0, 0, 1), (1, 0, 3), (0, 2, 5), (1, 2, 15), (0, 1, 5), (0, 3, 5), (1, 1, 15), (1, 3, 15)]
PSI5 = {0: 0, 1: 1, 2: 1j, 4: -1, 3: -1j}

def chi(k, n):
    a, b, q = CHARS[k]
    v = 1
    if a: v *= 0 if n % 3 == 0 else (1 if n % 3 == 1 else -1)
    if b: v *= PSI5[n % 5] ** b
    return complex(v)

def zeros():
    src = open(os.path.join("Assets", "Tour", "ZeroStackData.cs"), encoding="utf-8").read()
    block = src[src.index("public static readonly double[][] L"):]
    rows = re.findall(r"new\[\] \{([^}]*)\}", block)[:7]
    pos = {k + 1: [float(v) for v in r.split(",")] for k, r in enumerate(rows)}
    conj = {4: 5, 5: 4, 6: 7, 7: 6}
    return pos, {k: pos[k] + [-g for g in pos[conj.get(k, k)]] for k in range(1, 8)}

def main():
    N = 10 ** 7
    sieve = bytearray([1]) * (N + 1); sieve[0] = sieve[1] = 0
    for i in range(2, int(N ** .5) + 1):
        if sieve[i]: sieve[i * i::i] = bytearray(len(range(i * i, N + 1, i)))
    xs = [int(round(10 ** (2 + 5 * k / 1999))) for k in range(2000)]
    powers = {}
    for q in range(7, int(N ** .5) + 1):
        if sieve[q] and q % 30 in GOLD:
            m, j = q * q, 2
            while m <= N: powers[m] = 1 / j; m *= q; j += 1
    cnt = {h: 0 for h in GOLD}; tot = 0; E = {h: [] for h in GOLD}; k = 0
    pw = {h: 0.0 for h in GOLD}; ptot = 0.0; F = {h: [] for h in GOLD}
    for n in range(2, N + 1):
        if sieve[n] and n % 30 in cnt:
            tot += 1; cnt[n % 30] += 1
        elif n in powers:
            ptot += powers[n]; pw[n % 30] += powers[n]
        while k < len(xs) and xs[k] == n:
            for h in GOLD:
                E[h].append((8 * cnt[h] - tot) * math.log(n) / math.sqrt(n))
                F[h].append((8 * (cnt[h] + pw[h]) - tot - ptot) * math.log(n) / math.sqrt(n))
            k += 1
    squares = {h: sum(1 for m in powers if m % 30 == h and powers[m] == .5) for h in GOLD}
    print("prime squares up to 10^7 on each spoke:", squares)
    csq = {h: sum(1 for b in GOLD if b * b % 30 == h) for h in GOLD}
    print("pi*(10^7) =", tot, " per spoke:", cnt)
    print("fair race, mean F_h:", {h: round(sum(F[h]) / len(F[h]), 2) for h in GOLD},
          " share above the average:", {h: round(sum(1 for v in F[h] if v > 0) / len(xs), 3) for h in GOLD})
    print("mean E_h:", {h: round(sum(E[h]) / len(E[h]), 2) for h in GOLD}, " predicted:", {h: 1 - csq[h] for h in GOLD})
    above = {h: sum(1 for v in E[h] if v > 0) / len(xs) for h in GOLD}
    lead = {h: 0 for h in GOLD}
    for i in range(len(xs)): lead[max(GOLD, key=lambda h: E[h][i])] += 1
    print("share above the average:", {h: round(above[h], 3) for h in GOLD}, " in the lead:", {h: round(lead[h] / len(xs), 3) for h in GOLD})
    pos, Z = zeros()
    used = {k: [g for g in Z[k] if abs(g) <= 1000] for k in Z}
    nzeros = sum(1 for k in pos for g in pos[k] if g <= 1000)
    xr = xs[::STRIDE]
    sums = [{k: sum(cmath.exp(1j * g * math.log(x)) / complex(0.5, g) for g in used[k]) for k in used} for x in xr]
    R = {h: [-(csq[h] - 1) - sum((chi(k, h).conjugate() * s[k]).real for k in s) for s in sums] for h in GOLD}
    print("zeros used (0 < gamma <= 1000, seven L-functions):", nzeros)
    corr = {}
    for h in GOLD:
        a, b = E[h][::STRIDE], R[h]
        ma, mb = sum(a) / len(a), sum(b) / len(b)
        corr[h] = sum((p - ma) * (q - mb) for p, q in zip(a, b)) / math.sqrt(sum((p - ma) ** 2 for p in a) * sum((q - mb) ** 2 for q in b))
    print("correlation of the rebuild with the race:", {h: round(corr[h], 3) for h in GOLD})
    random.seed(7)
    real = {k: [1 / complex(0.5, g) for g in pos[k] if g <= 1000] for k in (1, 2, 3)}
    cplx = {k: [1 / complex(0.5, g) for g in Z[k] if abs(g) <= 1000] for k in (4, 6)}
    S = 3000; over = {h: 0 for h in GOLD}
    for _ in range(S):
        acc = {k: 2 * sum(a * cmath.exp(2j * math.pi * random.random()) for a in amps).real for k, amps in real.items()}
        acc.update({k: sum(a * cmath.exp(2j * math.pi * random.random()) for a in amps) for k, amps in cplx.items()})
        for h in GOLD:
            v = -(csq[h] - 1) - sum(chi(k, h).real * acc[k] for k in (1, 2, 3))
            for k, kb in ((4, 5), (6, 7)): v -= (chi(k, h).conjugate() * acc[k]).real + (chi(kb, h).conjugate() * acc[k].conjugate()).real
            if v > 0: over[h] += 1
    pred = {h: over[h] / S for h in GOLD}
    print("predicted share above the average (Rubinstein-Sarnak, zeros to t = 1000):", {h: round(pred[h], 4) for h in GOLD})
    with open(OUT, "w", encoding="utf-8") as f:
        f.write("// Generated by Tools/race_data.py: the prime race on E8's eight spokes to 10^7 and its rebuild from the zeros of the\n")
        f.write("// L-functions mod 30, for the sections MA–MB of docs/experiments/README.md. Do not edit by hand.\n")
        f.write("public static class RaceData\n{\n")
        f.write("    /// <summary>The gold hours, in the order of the arrays below.</summary>\n    public static readonly int[] Hours = { 1, 7, 11, 13, 17, 19, 23, 29 };\n")
        f.write(f"    /// <summary>log₁₀ x of the race samples (2000 points, 2 … 7) and of the rebuild (every {STRIDE}th); the zeros used.</summary>\n")
        f.write(f"    public const float Log0 = 2f, LogStep = 5f / 1999f; public const int Count = 2000, RebuildStride = {STRIDE}, ZeroCount = {nzeros};\n")
        f.write("    /// <summary>E_h(x) = (log x/√x)(8 π(x; 30, h) − π*(x)), π* = the primes on all eight spokes, spoke by spoke (8 × 2000).</summary>\n")
        f.write("    public static readonly float[] Race = { " + ", ".join(f"{v:.3f}f" for h in GOLD for v in E[h]) + " };\n")
        f.write("    /// <summary>The fair race: every prime power p^k counted as 1/k (8 × 2000).</summary>\n")
        f.write("    public static readonly float[] Fair = { " + ", ".join(f"{v:.3f}f" for h in GOLD for v in F[h]) + " };\n")
        f.write(f"    /// <summary>The same from the zeros of the seven L-functions (t ≤ 1000), spoke by spoke (8 × {len(xr)}).</summary>\n")
        f.write("    public static readonly float[] Rebuild = { " + ", ".join(f"{v:.3f}f" for h in GOLD for v in R[h]) + " };\n")
        f.write("    /// <summary>Primes up to 10⁷ on each spoke, and on all eight; prime squares up to 10⁷ on each spoke.</summary>\n")
        f.write("    public static readonly int[] PrimesTo10M = { " + ", ".join(str(cnt[h]) for h in GOLD) + f" }};\n    public const int Pi10M = {tot};\n")
        f.write("    public static readonly int[] SquaresTo10M = { " + ", ".join(str(squares[h]) for h in GOLD) + " };\n")
        f.write("    /// <summary>Share of log x (10² … 10⁷) with each spoke above the average: measured, and predicted from the zeros.</summary>\n")
        f.write("    public static readonly float[] AboveMeasured = { " + ", ".join(f"{above[h]:.4f}f" for h in GOLD) + " };\n")
        f.write("    public static readonly float[] AbovePredicted = { " + ", ".join(f"{pred[h]:.4f}f" for h in GOLD) + " };\n")
        f.write("    /// <summary>Correlation of the rebuild with the race, spoke by spoke.</summary>\n")
        f.write("    public static readonly float[] Correlation = { " + ", ".join(f"{corr[h]:.3f}f" for h in GOLD) + " };\n")
        f.write("}\n")
    print("wrote", OUT, os.path.getsize(OUT), "bytes")

if __name__ == "__main__":
    main()
