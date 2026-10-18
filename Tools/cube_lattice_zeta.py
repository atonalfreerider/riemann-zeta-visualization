#!/usr/bin/env python3
"""The zeros of the cubic lattice's zeta function, for section PB of docs/experiments/README.md.

Z₃(s) = Σ' |m|^(−2s) over m in Z³ = Σ r₃(n) n^(−s), completed Λ(s) = π^(−s) Γ(s) Z₃(s) = Λ(3/2 − s), is computed by Riemann's split
of the theta integral:
    Λ(s) = −1/s − 1/(3/2 − s) + Σ_n r₃(n) [ (πn)^(−s) Γ(s, πn) + (πn)^(s−3/2) Γ(3/2 − s, πn) ]
(mpmath, with 25 + 0.7 t digits, since Λ is about e^(−πt/2) on the line). The zeros up to height T are counted twice:
* on the centre line Re s = 3/4, where Λ is real, by its sign changes (step 0.02);
* in all, by the argument principle round the right half of the box 3/4 ≤ σ ≤ 4, 1 ≤ t ≤ T (the mirror s → 3/2 − s̄ makes
  the left half add the same).
The zeros right of the line are then located by the winding of arg Λ round the cells of a grid and refined by Newton.

Run:  python3 Tools/cube_lattice_zeta.py [T]   (T = 60 by default: about 10 minutes on 8 cores)
"""
import json, math, sys, time
from multiprocessing import Pool
import mpmath

NMAX = 400


def r3_table(N):
    m = math.isqrt(N)
    sq = [0] * (N + 1)
    for a in range(-m, m + 1):
        sq[a * a] += 1
    cur = [1] + [0] * N
    for _ in range(3):
        new = [0] * (N + 1)
        for i, c in enumerate(cur):
            if c:
                for a in range(0, N - i + 1):
                    if sq[a]:
                        new[i + a] += c * sq[a]
        cur = new
    return cur


R3 = r3_table(NMAX)


def lam3(s, dps=None):
    t = abs(complex(s).imag)
    with mpmath.workdps(dps or int(25 + 0.7 * t)):
        s = mpmath.mpc(s)
        h = mpmath.mpf(3) / 2
        tot = -1 / s - 1 / (h - s)
        n = 1
        while True:
            if R3[n]:
                x = mpmath.pi * n
                term = R3[n] * (x ** (-s) * mpmath.gammainc(s, x) + x ** (s - h) * mpmath.gammainc(h - s, x))
                tot += term
                if abs(term) < mpmath.mpf(10) ** (-(mpmath.mp.dps + 3)) and n > 3:
                    break
            n += 1
        return complex(tot)


def line(t):
    return lam3(complex(.75, t)).real


def refine(z0):
    with mpmath.workdps(int(25 + 0.7 * z0.imag)):
        try:
            r = mpmath.findroot(lambda s: mpmath.mpc(lam3(complex(s))), mpmath.mpc(z0), tol=1e-30)
            return complex(r)
        except (ZeroDivisionError, ValueError):
            return None


def main():
    T = float(sys.argv[1]) if len(sys.argv) > 1 else 60.0
    t0 = time.time()
    ts = [1 + 0.02 * k for k in range(int((T - 1) / 0.02) + 1)]
    path = ([complex(.75 + 3.25 * k / 60, 1) for k in range(61)] + [complex(4, 1 + 0.1 * k) for k in range(1, int((T - 1) / 0.1) + 1)]
            + [complex(4 - 3.25 * k / 160, T) for k in range(1, 161)])
    sig = [0.76] + [0.75 + 0.1 * k for k in range(1, 26)]
    grid_t = [1 + 0.25 * k for k in range(int((T - 1) / 0.25) + 1)]
    with Pool(8) as pool:
        L = pool.map(line, ts, chunksize=16)
        P = pool.map(lam3, path, chunksize=8)
        G = pool.map(lam3, [complex(s, t) for t in grid_t for s in sig], chunksize=20)
        on = [ts[i] + 0.02 * L[i] / (L[i] - L[i + 1]) for i in range(len(L) - 1) if L[i] * L[i + 1] < 0]
        total = sum(math.atan2((b / a).imag, (b / a).real) for a, b in zip(P, P[1:])) / math.pi
        ns = len(sig)
        rows = [G[i * ns:(i + 1) * ns] for i in range(len(grid_t))]
        darg = lambda a, b: math.atan2((b / a).imag, (b / a).real)
        cells = []
        for i in range(len(grid_t) - 1):
            for j in range(ns - 1):
                a, b, c, d = rows[i][j], rows[i][j + 1], rows[i + 1][j + 1], rows[i + 1][j]
                if abs(darg(a, b) + darg(b, c) + darg(c, d) + darg(d, a)) > math.pi:
                    cells.append(complex((sig[j] + sig[j + 1]) / 2, (grid_t[i] + grid_t[i + 1]) / 2))
        off = [z for z in pool.map(refine, cells) if z is not None]
    print(f"zeros of Z₃ with 1 < t < {T:g}: {round(total)} in all (argument principle: {total:.3f});  on the centre line: {len(on)};"
          f"  right of it: {len(off)} (each with its mirror at 3/2 − σ);  {time.time() - t0:.0f} s")
    print("on the line:", ", ".join(f"{t:.2f}" for t in on))
    for z in sorted(off, key=lambda z: z.imag):
        print(f"  {z.real:.4f} + {z.imag:.4f}i")
    assert round(total) == len(on) + 2 * len(off), "the zeros found do not add up to the count"


if __name__ == "__main__":
    main()
