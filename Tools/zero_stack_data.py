#!/usr/bin/env python3
"""Data for "The zeros inside E8's stack" (sections JA–JF of docs/experiments/README.md), written to
Assets/Tour/ZeroStackData.cs.

* The first 10 500 zeros of zeta on the critical line (mpmath.zetazero, which uses Gram blocks and Rosser's rule, so none
  are skipped).
* The zeros 0 < t <= 1000 of the other seven L-functions of Q(zeta_30) = Q(zeta_15), the field E8's Coxeter element makes
  E8 a lattice over: L(s, chi) for the eight characters of (Z/30)* = (Z/15)* (LabMath.Characters order). They are the
  sign changes of the real Hardy function Z(t) = eps^(-1/2) e^(i theta(t)) L(1/2 + it, chi), with
  theta(t) = (t/2) log(q/pi) + Im log Gamma((1/2 + a + it)/2) and eps = tau(chi)/(i^a sqrt q) the root number; dips of |Z|
  that do not cross zero are rescanned finely, and the count is checked against theta(t)/pi for drift.
* Checks, printed: the explicit formula for every character,
      sum_n chi(n) Lambda(n)/sqrt(n) G(log n - z) = S_chi(z) - sum_rho exp(-gamma^2 / 2 sigma^2) e^(i gamma z)
  (G a Gaussian of width 1/sigma, S_chi the pole and the trivial zeros), against the primes directly; the lamps
  sqrt(n) sqrt(2 pi)/sigma (S - sum) -> Lambda(n) at every column n; and the combination over the eight characters for
  each of E8's gold hours, which keeps only the prime powers on that spoke.
* The zeros among themselves: pair correlation and nearest-neighbour spacings (unfolded), within one L-function and
  across the eight.

Run from the repository root:  python3 Tools/zero_stack_data.py [--cache DIR] [--kzeta K]
(about 20 minutes on 8 cores without a cache; the zeros are written to DIR and reused).
"""
import cmath, math, os, sys, bisect
from multiprocessing import Pool
import mpmath

KZETA = 10500
TL = 1000.0
OUT = os.path.join("Assets", "Tour", "ZeroStackData.cs")

# ---- the eight characters of (Z/15)* --------------------------------------------------------------------------------
CHARS = [(0, 0, 1), (1, 0, 3), (0, 2, 5), (1, 2, 15), (0, 1, 5), (0, 3, 5), (1, 1, 15), (1, 3, 15)]   # psi3^a psi5^b, conductor
PSI5 = {0: 0, 1: 1, 2: 1j, 4: -1, 3: -1j}
CONJ = {4: 5, 5: 4, 6: 7, 7: 6}

def chi_val(k, n):
    a, b, q = CHARS[k]
    if q == 1: return 1
    v = 1
    if a:
        r = n % 3
        v *= 0 if r == 0 else (1 if r == 1 else -1)
    if b: v *= PSI5[n % 5] ** b
    return v

def chi_table(k):
    q = CHARS[k][2]
    return q, [chi_val(k, b) for b in range(q)]

def parity(k):
    q, tab = chi_table(k)
    return 0 if q == 1 or abs(tab[q - 1] - 1) < 1e-12 else 1

# ---- L(1/2 + it, chi) by Euler-Maclaurin --------------------------------------------------------------------------------
B2 = [1/6, -1/30, 1/42, -1/30, 5/66, -691/2730, 7/6, -3617/510, 43867/798, -174611/330, 854513/138,
      -236364091/2730, 8553103/6, -23749461029/870, 8615841276005/14322]
FACT = [math.factorial(k) for k in range(40)]

def hurwitz(s, a, M=14):
    N = int(abs(s.imag) / (2 * math.pi) * 2.2) + 20
    tot = 0j
    for k in range(N): tot += cmath.exp(-s * math.log(k + a))
    x = N + a
    xs = cmath.exp(-s * math.log(x))
    tot += x * xs / (s - 1) + xs / 2
    poch, xp = s, xs / x
    for j in range(1, M + 1):
        tot += B2[j - 1] / FACT[2 * j] * poch * xp
        poch *= (s + 2 * j - 1) * (s + 2 * j)
        xp /= x * x
    return tot

def L(s, k):
    q, tab = chi_table(k)
    if q == 1: return hurwitz(s, 1.0)
    return sum(tab[b] * hurwitz(s, b / q) for b in range(1, q) if tab[b] != 0) * cmath.exp(-s * math.log(q))

def theta(k, t):
    q = CHARS[k][2]; a = parity(k)
    return t / 2 * math.log(q / math.pi) + float(mpmath.loggamma(mpmath.mpc((0.5 + a) / 2, t / 2)).imag)

def hardy(k):
    q, tab = chi_table(k)
    a = parity(k)
    tau = sum(tab[b] * cmath.exp(2j * math.pi * b / q) for b in range(q))
    rot = 1 / cmath.sqrt(tau / ((1j) ** a * math.sqrt(q)))
    return lambda t: rot * cmath.exp(1j * theta(k, t)) * L(complex(0.5, t), k)

def refine(Z, a, b, fa, fb, tol=1e-11):
    side, c = 0, a
    for _ in range(200):
        c = (a * fb - b * fa) / (fb - fa)
        fc = Z(c)
        if abs(b - a) < tol or fc == 0: return c
        if fc * fb > 0:
            b, fb = c, fc
            if side == -1: fa /= 2
            side = -1
        else:
            a, fa = c, fc
            if side == 1: fb /= 2
            side = 1
    return c

def l_zeros(k, T=TL, h=0.02):
    Zc = hardy(k)
    Z = lambda t: Zc(t).real
    ts = [h * (i + 0.5) for i in range(int(T / h))]
    vals = [Zc(t) for t in ts]
    re = [v.real for v in vals]
    zs = []
    for i in range(len(ts) - 1):
        if re[i] * re[i + 1] < 0:
            zs.append(refine(Z, ts[i], ts[i + 1], re[i], re[i + 1]))
        elif 0 < i and abs(re[i]) < min(abs(re[i - 1]), abs(re[i + 1]), 0.2):
            fine = [ts[i - 1] + (ts[i + 1] - ts[i - 1]) * j / 80 for j in range(81)]
            fv = [Z(x) for x in fine]
            for j in range(80):
                if fv[j] * fv[j + 1] < 0: zs.append(refine(Z, fine[j], fine[j + 1], fv[j], fv[j + 1]))
    zs = sorted(set(round(z, 9) for z in zs))
    th0 = theta(k, 1e-9)
    res = [(j + 1) - (theta(k, (zs[j] + zs[j + 1]) / 2) - th0) / math.pi for j in range(len(zs) - 1)]
    w = max(1, len(res) // 8)
    drift = [sum(res[i:i + w]) / len(res[i:i + w]) for i in range(0, len(res), w)]
    return k, zs, max(abs(v.imag) for v in vals), drift

def zeta_zero(n):
    mpmath.mp.dps = 20
    return n, float(mpmath.zetazero(n).imag)

# ---- primes and the explicit formula ----------------------------------------------------------------------------------
def sieve(n):
    s = list(range(n + 1))
    for i in range(2, int(n ** .5) + 1):
        if s[i] == i:
            for j in range(i * i, n + 1, i):
                if s[j] == j: s[j] = i
    return s
SPF = sieve(5000)

def mangoldt(n):
    if n < 2: return 0.0
    p, m = SPF[n], n
    while m % p == 0: m //= p
    return math.log(p) if m == 1 else 0.0

def smooth_term(k, z):
    e2 = 1 - math.exp(-2 * z)
    if k == 0: return math.exp(z / 2) - math.exp(-2.5 * z) / e2
    return -math.exp(-0.5 * z) / e2 if parity(k) == 0 else -math.exp(-1.5 * z) / e2

def signed(pos, k):
    return pos[k] + [-g for g in pos[CONJ.get(k, k)]]

def zero_sum(zs, z, sigma, cut):
    return sum(math.exp(-0.5 * (g / sigma) ** 2) * cmath.exp(1j * g * z) for g in zs if abs(g) <= cut)

def prime_side(k, z, sigma):
    tot = 0j
    for n in range(max(2, int(math.exp(z - 8 / sigma))), int(math.exp(z + 8 / sigma)) + 2):
        lam = mangoldt(n)
        if lam: tot += chi_val(k, n) * lam / math.sqrt(n) * sigma / math.sqrt(2 * math.pi) * math.exp(-0.5 * (sigma * (math.log(n) - z)) ** 2)
    return tot

def lamp(zs, k, n, sigma, cut):
    z = math.log(n)
    return math.sqrt(n) * math.sqrt(2 * math.pi) / sigma * (smooth_term(k, z) - zero_sum(zs, z, sigma, cut))

# ---- main ---------------------------------------------------------------------------------------------------------------
def main():
    global KZETA
    cache = None
    if "--cache" in sys.argv: cache = sys.argv[sys.argv.index("--cache") + 1]; os.makedirs(cache, exist_ok=True)
    if "--kzeta" in sys.argv: KZETA = int(sys.argv[sys.argv.index("--kzeta") + 1])
    pos = {}
    zpath = os.path.join(cache, "zeta.txt") if cache else None
    have = {}
    if zpath and os.path.exists(zpath):
        for line in open(zpath):
            n, g = line.split(); have[int(n)] = float(g)
    need = [n for n in range(1, KZETA + 1) if n not in have]
    if need:
        print(f"zeta: computing {len(need)} zeros ...", flush=True)
        with Pool(8) as p:
            for n, g in p.imap_unordered(zeta_zero, need, chunksize=25): have[n] = g
        if zpath:
            with open(zpath, "w") as f:
                for n in sorted(have): f.write(f"{n} {have[n]:.10f}\n")
    pos[0] = [have[n] for n in range(1, KZETA + 1)]
    todo = [k for k in range(1, 8) if not (cache and os.path.exists(os.path.join(cache, f"l{k}.txt")))]
    if todo:
        print(f"L: scanning characters {todo} to t = {TL} ...", flush=True)
        with Pool(7) as p:
            for k, zs, maxim, drift in p.imap_unordered(l_zeros, todo):
                print(f"  chi {k}: {len(zs)} zeros, max |Im Z| = {maxim:.1e}, count residual by window " + " ".join(f"{d:+.2f}" for d in drift), flush=True)
                pos[k] = zs
                if cache:
                    with open(os.path.join(cache, f"l{k}.txt"), "w") as f: f.write("\n".join(f"{z:.10f}" for z in zs) + "\n")
    for k in range(1, 8):
        if k not in pos: pos[k] = [float(l) for l in open(os.path.join(cache, f"l{k}.txt")) if l.strip()]
    Z = {k: signed(pos, k) for k in range(8)}
    for k in range(8): print(f"chi {k} {CHARS[k]}: {len(pos[k])} zeros in (0, {max(pos[k]):.1f}]")

    # the explicit formula, every character, sigma = 250
    sig = 250.0
    zs_check = [0.6 + 0.0983 * i for i in range(60)] + [math.log(n) for n in (7, 11, 31, 97, 211, 331, 509, 599, 600)]
    worst = 0.0
    for k in range(8):
        for z in zs_check:
            worst = max(worst, abs(prime_side(k, z, sig) - (smooth_term(k, z) - zero_sum(Z[k], z, sig, TL))))
    print(f"explicit formula, 8 characters, sigma 250: max |primes - (S - zeros)| = {worst:.4f}  (spikes reach 73)")
    # the lamps of zeta at every column
    sz = max(pos[0]) / 4
    errc = max(abs(lamp(Z[0], 0, n, sz, 1e9).real) for n in range(2, 721) if mangoldt(n) == 0)
    errp = max(abs(lamp(Z[0], 0, n, sz, 1e9).real - mangoldt(n)) for n in range(2, 721) if mangoldt(n) > 0)
    print(f"zeta lamps, {KZETA} zeros, sigma {sz:.0f}, n <= 720: composites max {errc:.3f}, prime powers max |lamp - log p| {errp:.3f}")
    # per gold hour, the eight characters combined, sigma 250
    lamps = {k: {n: lamp(Z[k], k, n, sig, TL) for n in range(2, 721)} for k in range(8)}
    def ramified(h, n):
        for p in (2, 3, 5):
            m, j = n, 0
            while m % p == 0: m //= p; j += 1
            if m == 1 and j:
                if p == 2: return math.log(2) if (n - h) % 15 == 0 else 0.0
                if p == 3: return math.log(3) / 2 if (n - h) % 5 == 0 else 0.0
                return math.log(5) / 4 if (n - h) % 3 == 0 else 0.0
        return 0.0
    on = off = 0.0
    for h in (1, 7, 11, 13, 17, 19, 23, 29):
        for n in range(h, 721, 30):
            if n < 2: continue
            v = sum(complex(chi_val(k, h)).conjugate() * lamps[k][n] for k in range(8)) / 8 - ramified(h, n)
            if mangoldt(n): on = max(on, abs(v - mangoldt(n)))
            else: off = max(off, abs(v))
    print(f"per gold hour, 8 L-functions (t <= {TL:.0f}): on-spoke prime powers max err {on:.3f}, on-spoke composites max {off:.3f}")

    # the zeros among themselves
    def unfold(k, gs):
        t0 = theta(k, 1e-9)
        return [(theta(k, g) - t0) / math.pi for g in gs]
    def density(k, t): return math.log(CHARS[k][2] * t / (2 * math.pi)) / (2 * math.pi)
    def hist(vals, lo, hi, nb):
        h = [0] * nb
        for v in vals:
            if lo <= v < hi: h[int((v - lo) / (hi - lo) * nb)] += 1
        return h
    def pairs(x, umax=3.0):
        out = []
        for i in range(len(x)):
            j = i + 1
            while j < len(x) and x[j] - x[i] < umax: out.append(x[j] - x[i]); j += 1
        return out
    xz = unfold(0, pos[0])[999:]
    pz = hist(pairs(xz), 0, 3, 30)
    pair_zeta = [c / len(xz) / .1 for c in pz]
    spz = [xz[i + 1] - xz[i] for i in range(len(xz) - 1)]
    nn_zeta = [c / len(spz) / .1 for c in hist(spz, 0, 3, 30)]
    within, nw = [], 0
    for k in range(1, 8):
        x = unfold(k, pos[k]); within += pairs(x); nw += len(x)
    pair_l = [c / nw / .1 for c in hist(within, 0, 3, 30)]
    across, na = [], 0
    for a in range(8):
        ga = pos[a] if a else [g for g in pos[0] if g <= TL]
        for b in range(8):
            if a == b: continue
            gb = pos[b] if b else [g for g in pos[0] if g <= TL]
            for g in ga:
                if g < 50 or g > TL - 10: continue
                na += 1
                d = density(b, g)
                lo, hi = bisect.bisect_left(gb, g - 3 / d), bisect.bisect_right(gb, g + 3 / d)
                across += [(g2 - g) * d for g2 in gb[lo:hi]]
    pair_x = [c / na / .2 for c in hist(across, -3, 3, 30)]
    merged = sorted([g for k in range(1, 8) for g in pos[k]] + [g for g in pos[0] if g <= TL])
    tot = lambda t: sum(density(k, t) for k in range(8))
    spm = [(merged[i + 1] - merged[i]) * tot((merged[i] + merged[i + 1]) / 2) for i in range(len(merged) - 1) if merged[i] > 50]
    nn_merged = [c / len(spm) / .1 for c in hist(spm, 0, 3, 30)]
    small = lambda sp: sum(1 for s in sp if s < .25) / len(sp)
    print(f"spacings < 1/4: zeta {small(spz):.4f}, merged eight {small(spm):.4f} (GUE 0.0117, Poisson 0.221)")
    print("pair correlation, zeta zeros 1000-10500: " + " ".join(f"{v:.2f}" for v in pair_zeta))
    print("across different L-functions: " + " ".join(f"{v:.2f}" for v in pair_x))

    def arr(name, vals, fmt, typ, doc):
        body = ", ".join(fmt.format(v) for v in vals)
        return f"    /// <summary>{doc}</summary>\n    public static readonly {typ}[] {name} = {{ {body} }};\n"
    with open(OUT, "w", encoding="utf-8") as f:
        f.write("// Generated by Tools/zero_stack_data.py: the zeros of zeta and of the other seven L-functions of Q(zeta_30), and their\n")
        f.write("// spacing statistics, for the sections JA–JF of docs/experiments/README.md. Do not edit by hand.\n")
        f.write("public static class ZeroStackData\n{\n")
        f.write(arr("Zeta", pos[0], "{:.6f}", "double", f"The first {KZETA} zeros of ζ on the critical line (imaginary parts)."))
        f.write("    /// <summary>The zeros 0 &lt; t ≤ 1000 of L(s, χ_k), k = 1 … 7 in LabMath.Characters order (index 0 is empty: ζ is Zeta).</summary>\n")
        f.write("    public static readonly double[][] L =\n    {\n        new double[0],\n")
        for k in range(1, 8): f.write("        new[] { " + ", ".join(f"{g:.6f}" for g in pos[k]) + " },\n")
        f.write("    };\n")
        f.write(arr("PairZeta", pair_zeta, "{:.4f}f", "float", "Pair correlation of ζ's zeros 1000 … 10 500, unfolded, u = 0 … 3 in steps of 0.1 (GUE: 1 − (sin πu/πu)²)."))
        f.write(arr("SpacingZeta", nn_zeta, "{:.4f}f", "float", "Nearest-neighbour spacings of the same zeros, s = 0 … 3 in steps of 0.1."))
        f.write(arr("PairWithinL", pair_l, "{:.4f}f", "float", "Pair correlation within each of the seven other L-functions (t ≤ 1000), pooled, u = 0 … 3."))
        f.write(arr("PairAcross", pair_x, "{:.4f}f", "float", "Correlation between zeros of different L-functions of Q(ζ30), u = −3 … 3 in steps of 0.2 (independent: 1)."))
        f.write(arr("SpacingMerged", nn_merged, "{:.4f}f", "float", "Nearest-neighbour spacings of all eight L-functions' zeros merged (the zeros of Q(ζ30)'s zeta), s = 0 … 3."))
        f.write("}\n")
    print("wrote", OUT, os.path.getsize(OUT), "bytes")

if __name__ == "__main__":
    main()
