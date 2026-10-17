#!/usr/bin/env python3
"""Data for "E8's own zeta function" (sections KA–KD of docs/experiments/README.md), written to
Assets/Tour/LatticeZetaData.cs.

Every lattice has a zeta function, Z_L(s) = sum over v != 0 of (v.v/2)^(-s) = sum_n r(n) n^(-s), r(n) the number of
vectors of norm 2n. Checked here against direct counts of lattice points:
  Z^2    4 zeta(s) L(s, chi_-4)                 A2 (hexagonal)   6 zeta(s) L(s, chi_-3)
  D4     24 (1 - 2^(1-s)) zeta(s) zeta(s-1)     E8               240 zeta(s) zeta(s-3)
  Leech  (65520/691) (zeta(s) zeta(s-11) - L(Delta, s))
Completed with (2 pi)^-s Gamma(s), each is symmetric about its critical line Re s = d/4 (D4 needs a factor 2^(s/2) as it is
not unimodular). Written out: the landscape log|Lambda_E8(s) / Lambda_E8(2 + it)| on 0.5 <= t <= 50, the zeros of L(s, chi_-4)
to t = 60, and the zeros of the Leech lattice's zeta in -4 <= Re s <= 6, t <= 50 (counted by the argument principle and found
at 45 digits; the other half is their mirror image in Re s = 6).

Run from the repository root:  python3 Tools/lattice_zeta_data.py [--cache DIR]
(the Leech zeros take about 20 minutes on 8 cores; with --cache they are kept in DIR/leech_zeros.json).
"""
import json, math, os, sys
from itertools import product
from multiprocessing import Pool
import mpmath

OUT = os.path.join("Assets", "Tour", "LatticeZetaData.cs")
mpmath.mp.dps = 20

# ---- Ramanujan's tau, for the Leech lattice ------------------------------------------------------------------------
M = 60
_c = [0] * (M + 1); _c[0] = 1
for _n in range(1, M + 1):
    for _ in range(24):
        for _k in range(M, _n - 1, -1): _c[_k] -= _c[_k - _n]
TAU = [0] + _c[:M]

def lam_delta(s):
    tot = mpmath.mpc(0)
    for n in range(1, M):
        x = 2 * mpmath.pi * n
        tot += TAU[n] * (x ** (-s) * mpmath.gammainc(s, x) + x ** (s - 12) * mpmath.gammainc(12 - s, x))
    return tot

def lam_leech(s):
    return mpmath.mpf(65520) / 691 * ((2 * mpmath.pi) ** (-s) * mpmath.gamma(s) * mpmath.zeta(s) * mpmath.zeta(s - 11) - lam_delta(s))

def z_leech(s):   # the same zeros, without the gamma factor
    s = mpmath.mpc(s)
    return complex(mpmath.zeta(s) * mpmath.zeta(s - 11) - lam_delta(s) * (2 * mpmath.pi) ** s / mpmath.gamma(s))

def lam_e8(s):
    return (2 * mpmath.pi) ** (-s) * mpmath.gamma(s) * 240 * mpmath.zeta(s) * mpmath.zeta(s - 3)

# ---- checks against the lattices ---------------------------------------------------------------------------------------
def divs(n): return [d for d in range(1, n + 1) if n % d == 0]
def sigma(k, n): return sum(d ** k for d in divs(n))

def check_counts():
    N = 12
    chi4 = lambda d: 0 if d % 2 == 0 else (1 if d % 4 == 1 else -1)
    chi3 = lambda d: 0 if d % 3 == 0 else (1 if d % 3 == 1 else -1)
    rz = [0] * (N + 1); ra = [0] * (N + 1); rd = [0] * (N + 1)
    for x in range(-6, 7):
        for y in range(-6, 7):
            if 0 < x * x + y * y <= N: rz[x * x + y * y] += 1
            if 0 < x * x + x * y + y * y <= N: ra[x * x + x * y + y * y] += 1
    for v in product(range(-5, 6), repeat=4):
        q = sum(t * t for t in v)
        if sum(v) % 2 == 0 and q and q // 2 <= N: rd[q // 2] += 1
    odd = lambda n: n if n % 2 else odd(n // 2)
    ok = (rz[1:] == [4 * sum(chi4(d) for d in divs(n)) for n in range(1, N + 1)],
          ra[1:] == [6 * sum(chi3(d) for d in divs(n)) for n in range(1, N + 1)],
          rd[1:] == [24 * sigma(1, odd(n)) for n in range(1, N + 1)])
    # E8 (doubled coordinates, all even or all odd, sum = 0 mod 4), norms 2n for n <= 4
    re = [0] * 5
    for v in product(range(-4, 5, 2), repeat=8):
        q = sum(t * t for t in v)
        if q and sum(v) % 4 == 0 and q % 8 == 0 and q // 8 <= 4: re[q // 8] += 1
    for v in product((-5, -3, -1, 1, 3, 5), repeat=8):
        q = sum(t * t for t in v)
        if sum(v) % 4 == 0 and q % 8 == 0 and q // 8 <= 4: re[q // 8] += 1
    ok += (re[1:] == [240 * sigma(3, n) for n in range(1, 5)],)
    leech = [65520 * (sigma(11, n) - TAU[n]) // 691 for n in range(1, 6)]
    print("counts: Z2", ok[0], " A2", ok[1], " D4", ok[2], " E8", ok[3], re[1:], " Leech shells", leech)
    assert all(ok) and leech == [0, 196560, 16773120, 398034000, 4629381120]
    # E8's Dirichlet series at s = 6, from the shells
    s = 6
    part = sum(240 * sigma(3, n) * mpmath.power(n, -s) for n in range(1, 3001))
    print("E8 at s = 6: shells 1..3000", mpmath.nstr(part, 12), " 240 zeta(6) zeta(3)", mpmath.nstr(240 * mpmath.zeta(6) * mpmath.zeta(3), 12))
    return float(part), float(240 * mpmath.zeta(6) * mpmath.zeta(3))

def e8_row(t):
    sig = [round(-0.5 + 0.05 * i, 3) for i in range(101)]
    ref = abs(lam_e8(mpmath.mpc(2, t)))
    return t, [float(mpmath.log(abs(lam_e8(mpmath.mpc(x, t))) / ref)) for x in sig]

def beta_zeros(T=60.0):
    L = lambda t: mpmath.dirichlet(mpmath.mpc(0.5, t), [0, 1, 0, -1])
    th = lambda t: t / 2 * math.log(4 / math.pi) + float(mpmath.loggamma(mpmath.mpc(0.75, t / 2)).imag)
    Z = lambda t: complex(mpmath.exp(1j * th(t)) * L(t)).real
    ts = [0.02 * (i + 1) for i in range(int(T / 0.02))]
    vals = [Z(t) for t in ts]
    return [float(mpmath.findroot(Z, (a, b), solver="anderson")) for a, b, fa, fb in zip(ts, ts[1:], vals, vals[1:]) if fa * fb < 0]

# ---- the Leech zeros (45 digits: the Delta series loses everything at 25 digits above t ~ 50) ------------------------
LEECH_DPS = 45

def z_leech45(s):
    mpmath.mp.dps = LEECH_DPS
    return z_leech(s)

def logabs(p): return (p, math.log(abs(z_leech45(complex(*p)))))

def winding(vals):
    tot = 0.0
    for a, b in zip(vals, vals[1:]):
        q = b / a
        tot += math.atan2(q.imag, q.real)
    return tot / (2 * math.pi)

def newton(z):
    for _ in range(60):
        f = z_leech45(z)
        d = (z_leech45(z + 1e-8) - z_leech45(z - 1e-8)) / 2e-8
        step = f / d
        z -= step
        if abs(step) < 1e-13: break
    return z, abs(z_leech45(z)) / abs(z_leech45(z + 0.05))

def leech_zeros(pool, x0=-4.0, x1=6.0, y0=0.3, y1=50.0):
    """Zeros in [x0, x1] x [y0, y1]: Newton from the local minima of log|Z| on a grid, checked against the argument principle."""
    xs = [x0 + 0.25 * i for i in range(41)]
    ys = [y0 + 0.5 * j for j in range(100)]
    grid = dict(pool.map(logabs, [(x, y) for y in ys for x in xs]))
    mins = [complex(xs[i], ys[j]) for j in range(1, len(ys) - 1) for i in range(1, len(xs) - 1)
            if all(grid[(xs[i], ys[j])] < grid[(xs[i + a], ys[j + b])] for a in (-1, 0, 1) for b in (-1, 0, 1) if a or b)]
    out = []
    for z, rel in pool.map(newton, mins):
        if rel < 1e-12 and x0 - .2 < z.real < x1 + .2 and y0 < z.imag < y1 and all(abs(z - q) > 1e-6 for q in out): out.append(z)
    n = 250
    path = ([complex(x0 + (x1 - x0) * k / n, y0) for k in range(n)] + [complex(x1, y0 + (y1 - y0) * k / (4 * n)) for k in range(4 * n)] +
            [complex(x1 - (x1 - x0) * k / n, y1) for k in range(n)] + [complex(x0, y1 - (y1 - y0) * k / (4 * n)) for k in range(4 * n)] + [complex(x0, y0)])
    total = winding(pool.map(z_leech45, path))
    print(f"Leech: {len(out)} zeros found; the argument principle counts {total:.3f} in [{x0}, {x1}] x [{y0}, {y1}]")
    assert round(total) == len(out), "missed zeros"
    return sorted(out, key=lambda z: z.imag)

def main():
    cache = sys.argv[sys.argv.index("--cache") + 1] if "--cache" in sys.argv else None
    s6, z6 = check_counts()
    for s in (mpmath.mpc(0.3, 7.1), mpmath.mpc(1.7, 23.4)):
        print("E8 Lambda(s)/Lambda(4 - s):", mpmath.nstr(lam_e8(s) / lam_e8(4 - s), 10), "   Leech Lambda(s)/Lambda(12 - s):", mpmath.nstr(lam_leech(s + 2) / lam_leech(10 - s), 10))
    on_line = [lam_e8(mpmath.mpc(2, 0.5 + 0.1 * k)) for k in range(600)]
    print("E8 on Re s = 2 (t to 60): real to", max(abs(float(v.imag / v.real)) for v in on_line), " signs", sorted(set(1 if v.real > 0 else -1 for v in on_line)))
    with Pool(8) as pool:
        ts = [round(0.5 + 0.25 * j, 3) for j in range(199)]
        rows = sorted(pool.map(e8_row, ts))
        path = os.path.join(cache, "leech_zeros.json") if cache else None
        if path and os.path.exists(path):
            lz = [complex(a, b) for a, b in json.load(open(path))]
        else:
            lz = leech_zeros(pool)
            if path: json.dump([[z.real, z.imag] for z in lz], open(path, "w"))
        mpmath.mp.dps = LEECH_DPS
        lead = [lam_leech(mpmath.mpc(6, t)) for t in [0.25 + 0.25 * k for k in range(200)]]
        print("Leech on Re s = 6 (t to 50, 45 digits): real to", max(abs(float(v.imag / v.real)) for v in lead), " sign changes", sum(1 for a, b in zip(lead, lead[1:]) if a.real * b.real < 0))
        mpmath.mp.dps = 20
    bz = beta_zeros()
    print("beta zeros:", len(bz), [round(z, 4) for z in bz[:4]])
    for z in lz: print(f"   Leech zero {z.real:+.6f} + {z.imag:.6f} i   |Z| relative to nearby: {abs(z_leech45(z)) / abs(z_leech45(z + 0.05)):.1e}")
    mpmath.mp.dps = 20

    def arr(name, vals, fmt, typ, doc):
        return f"    /// <summary>{doc}</summary>\n    public static readonly {typ}[] {name} = {{ " + ", ".join(fmt.format(v) for v in vals) + " };\n"
    with open(OUT, "w", encoding="utf-8") as f:
        f.write("// Generated by Tools/lattice_zeta_data.py: E8's zeta landscape and the zeros of the lattice zeta functions, for the\n")
        f.write("// sections KA–KD of docs/experiments/README.md. Do not edit by hand.\n")
        f.write("public static class LatticeZetaData\n{\n")
        f.write("    /// <summary>Re s of the landscape grid: −0.5 … 4.5 in steps of 0.05.</summary>\n    public const float E8Sigma0 = -.5f, E8SigmaStep = .05f;\n")
        f.write("    public const int E8SigmaCount = 101;\n")
        f.write("    /// <summary>Im s of the landscape grid: 0.5 … 50 in steps of 0.25.</summary>\n    public const float E8T0 = .5f, E8TStep = .25f;\n")
        f.write("    public const int E8TCount = 199;\n")
        f.write(arr("E8Height", [h for _, r in rows for h in r], "{:.3f}f", "float", "log|Λ_E8(s) / Λ_E8(2 + it)|, row by row in t (Λ_E8 = (2π)^−s Γ(s) 240 ζ(s) ζ(s − 3); 0 on Re s = 2, −∞ at the zeros)."))
        f.write(arr("BetaZeros", bz, "{:.6f}", "double", "The zeros 0 &lt; t ≤ 60 of L(s, χ₋₄) (for the square lattice Z²)."))
        f.write(arr("LeechZerosRe", [z.real for z in lz], "{:.6f}", "double", "The zeros of the Leech lattice's zeta in −4 ≤ Re s ≤ 6, 0 &lt; t ≤ 50: real parts (mirror: 12 − Re s)."))
        f.write(arr("LeechZerosIm", [z.imag for z in lz], "{:.6f}", "double", "Their imaginary parts."))
        f.write(f"    /// <summary>E8's Dirichlet series at s = 6 from its shells n ≤ 3000, and 240 ζ(6) ζ(3).</summary>\n    public const double E8AtSix = {s6:.12f}, E8AtSixClosed = {z6:.12f};\n")
        f.write("}\n")
    print("wrote", OUT, os.path.getsize(OUT), "bytes")

if __name__ == "__main__":
    main()
