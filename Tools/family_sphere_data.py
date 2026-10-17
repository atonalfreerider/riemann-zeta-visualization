#!/usr/bin/env python3
"""Data for sections NC–ND of docs/experiments/README.md, written to Assets/Tour/FamilySphereData.cs.

-47's three family zeta functions on the critical line (Assets/Tour/ClassFiveData.cs to t = 50, extended to t = 100 by
EXTENSION, written by Tools/class_five_extend.py) form one real curve V(t) = (Λ1, Λ2, Λ3)(t). A mixture a·V vanishes on the
line exactly when V crosses the plane a⊥, so on the sphere of directions the curve's crossings of a great circle are that
mixture's zeros on the line, and the class group's L-functions are the great circles it crosses at every zero.
* the curve in the L-functions' own coordinates (t = 0.3 … 100, every 0.04): W = (ζL(χ₋₄₇), L(χ₁), L(χ₂)) on the line,
  times e^(τt/4); each coordinate plane is one L-function's zero set, and the curve hugs the axes;
* the landscape: zeros off the line up to t = 100 (all zeros by the argument principle round the box, minus the sign changes
  on the line) for the mixtures on a grid over the hemisphere centred on equal parts of the three L-functions;
* the race between the families: u(x) = −(ν + S(x)) in the characters' coordinates (sign, ρ1, ρ2), S from each L-function's
  zeros up to t = 100, on a log grid of x from 10^3 to 10^7, and the families' character vectors χ(C).

Run from the repository root:  python3 Tools/family_sphere_data.py [EXTENSION.json [WINDOW.json]]   (a minute on 8 cores).
Both files come from Tools/class_five_extend.py; without EXTENSION the data stop at t = 50. WINDOW resamples a stretch of
the line finely: up to t = 100 a zero of ζ (t = 84.7355) and one of L(χ₋₄₇) (t = 84.7260) lie within one 0.02 step, and
without it their sign changes cancel.
"""
import json, math, os, re, sys
from multiprocessing import Pool

OUT = os.path.join("Assets", "Tour", "FamilySphereData.cs")
src = open(os.path.join("Assets", "Tour", "ClassFiveData.cs"), encoding="utf-8").read()


def arr(name):
    m = re.search(r"\b" + name + r"\s*=\s*\{([^}]*)\}", src)
    return [float(x.strip().rstrip("f")) for x in m.group(1).split(",") if x.strip()]


new = json.load(open(sys.argv[1])) if len(sys.argv) > 1 else {"line": [], "right": [], "top": []}
E = arr("LineE") + [v[0][0] for v in new["line"]]
G = arr("Line2") + [v[1][0] for v in new["line"]]
H = arr("Line3") + [v[2][0] for v in new["line"]]
T0, TS = 0.02, 0.02
TL = [T0 + TS * k for k in range(len(E))]                 # the line's t values (the window below is finer)
if len(sys.argv) > 2:
    ref = json.load(open(sys.argv[2]))
    ka, kb = round((ref["t"][0] - T0) / TS), round((ref["t"][-1] - T0) / TS)
    E = E[:ka] + [v[0][0] for v in ref["vals"]] + E[kb + 1:]
    G = G[:ka] + [v[1][0] for v in ref["vals"]] + G[kb + 1:]
    H = H[:ka] + [v[2][0] for v in ref["vals"]] + H[kb + 1:]
    TL = TL[:ka] + list(ref["t"]) + TL[kb + 1:]
k03 = int(re.search(r"PathStart = (\d+)", src).group(1))
PR = [arr("PathRe" + k) for k in ("E", "2", "3")]
PI = [arr("PathIm" + k) for k in ("E", "2", "3")]
old = [tuple(complex(PR[i][j], PI[i][j]) for i in range(3)) for j in range(len(PR[0]))]
right = [tuple(complex(*v[i]) for i in range(3)) for v in new["right"]]
top = [tuple(complex(*v[i]) for i in range(3)) for v in new["top"]]
PATH = ([(complex(E[k03], 0), complex(G[k03], 0), complex(H[k03], 0))] + (old[:670] + right + top if right else old)
        + [(complex(E[-1], 0), complex(G[-1], 0), complex(H[-1], 0))])
TMAX = TL[-1]
c1, c2 = math.cos(math.tau / 5), math.cos(2 * math.tau / 5)
A = [(1.0, 2.0, 2.0), (1.0, 2 * c1, 2 * c2), (1.0, 2 * c2, 2 * c1)]     # ζ·L(χ₋₄₇), L(χ1), L(χ2) as mixtures of Λ1, Λ2, Λ3


def nrm(u):
    n = math.sqrt(sum(x * x for x in u))
    return tuple(x / n for x in u)


def cross(u, v):
    return (u[1] * v[2] - u[2] * v[1], u[2] * v[0] - u[0] * v[2], u[0] * v[1] - u[1] * v[0])


def counts(a):
    f = [a[0] * x + a[1] * y + a[2] * z for x, y, z in zip(E, G, H)]
    on = sum(1 for i in range(k03, len(f) - 1) if f[i] * f[i + 1] < 0)
    vals = [a[0] * x + a[1] * y + a[2] * z for x, y, z in PATH]
    tot = 0.0
    for p, q in zip(vals, vals[1:]):
        r = q / p
        tot += math.atan2(r.imag, r.real)
    return on, round(tot / math.pi)


CEN = nrm((1, 1, 1))                  # the grid's centre, in the L-functions' coordinates
U1 = nrm((1, -1, 0))
U2 = nrm(cross(CEN, U1))
NTH, NPH = 72, 180
# polar angles from the centre: even steps, bent so that one ring passes exactly through the three corners (the L-functions,
# 54.74° out, at azimuths 30°, 150° and 270°, which the azimuth grid hits every 2°)
TC = math.degrees(math.acos(1 / math.sqrt(3)))
IC = round(NTH * TC / 90)
THETA = [TC * i / IC if i <= IC else TC + (90 - TC) * (i - IC) / (NTH - IC) for i in range(NTH + 1)]


def grid_dir(i, j):
    th = math.radians(THETA[i])
    ph = math.tau * j / NPH
    b = tuple(math.cos(th) * c + math.sin(th) * (math.cos(ph) * u + math.sin(ph) * v) for c, u, v in zip(CEN, U1, U2))
    return b


def mix(b):
    return tuple(sum(b[j] * A[j][i] for j in range(3)) for i in range(3))


def job(ij):
    on, tot = counts(mix(grid_dir(*ij)))
    return tot - on


def zeros_of(a):
    f = [a[0] * x + a[1] * y + a[2] * z for x, y, z in zip(E, G, H)]
    return [TL[i] + (TL[i + 1] - TL[i]) * f[i] / (f[i] - f[i + 1]) for i in range(k03, len(f) - 1) if f[i] * f[i + 1] < 0]


def main():
    # checks: every L-function keeps all its zeros on the line up to TMAX
    for name, a in zip(("ζ·L(χ₋₄₇)", "L(χ₁)", "L(χ₂)"), A):
        on, tot = counts(a)
        print(f"{name}: {tot} zeros up to t = {TMAX:.0f}, {on} on the line")
        assert tot == on
    cells = [(i, j) for i in range(NTH + 1) for j in range(NPH)]
    with Pool(8) as pool:
        off = pool.map(job, cells, chunksize=32)
    print(f"landscape: {len(cells)} mixtures; {off.count(0)} with every zero up to t = {TMAX:.0f} on the line")
    assert min(off) >= 0, "a mixture with fewer zeros in all than on the line: the box path is too coarse"
    for (i, j), o in zip(cells, off):
        if o == 0:
            b = grid_dir(i, j)
            print(f"  zero-free: θ {THETA[i]:.2f}°, φ {360 * j / NPH:.0f}°: {math.degrees(math.acos(min(1, max(abs(x) for x in b)))):.2f}° from an L-function")
    # the curve in the L-functions' own coordinates, W = (L0, L1, L2) = (a_j · V), every 0.04: each coordinate plane is one
    # L-function's zero set, and where one L-function dominates W lies along an axis. Scaled by e^(τt/4), which undoes the
    # Gamma factor's decay, so the values stay near 1 and the curve can be interpolated linearly between samples
    curve, curve_t = [], []
    k = k03
    while k < len(E):
        curve_t.append(TL[k])
        V = (E[k], G[k], H[k])
        f = math.exp(math.tau * TL[k] / 4)
        curve.append(tuple(f * sum(a * v for a, v in zip(A[j], V)) for j in range(3)))
        k = next((i for i in range(k + 1, len(E)) if TL[i] >= TL[k] + 0.0399), len(E))
    print("curve: |W| e^(τt/4) from %.3g to %.3g" % (min(math.sqrt(sum(x * x for x in w)) for w in curve),
                                                     max(math.sqrt(sum(x * x for x in w)) for w in curve)))
    # the race: S per L-function from its zeros, on a log grid of x
    import mpmath
    assert all(abs(b - a - 0.04) < 1e-6 for a, b in zip(curve_t, curve_t[1:])), "the curve's samples are not evenly spaced"
    z0 = zeros_of(A[0])
    zeta = []
    while (g := float(mpmath.zetazero(len(zeta) + 1).imag)) < TMAX:   # each zero of ζ takes away the one zero of ζ·L(χ₋₄₇) nearest it
        i = min(range(len(z0)), key=lambda m: abs(z0[m] - g))
        assert abs(z0[i] - g) < 0.01, (g, z0[i])
        z0.pop(i)
        zeta.append(g)
    z1, z2 = zeros_of(A[1]), zeros_of(A[2])
    print(f"zeros up to t = {TMAX:.0f}: L(χ₋₄₇) {len(z0)}, L(χ₁) {len(z1)}, L(χ₂) {len(z2)}")

    def S(zs, x):
        u = math.log(x)
        return sum(2 * (.5 * math.cos(g * u) + g * math.sin(g * u)) / (.25 + g * g) for g in zs)

    xs = [10 ** (3 + 4 * k / 1600) for k in range(1601)]
    race = [tuple(-(1 + S(z, x)) for z in (z0, z1, z2)) for x in xs]
    chi = [(1.0, 2.0, 2.0), (1.0, 2 * c1, 2 * c2), (1.0, 2 * c2, 2 * c1), (-1.0, 0.0, 0.0)]
    lead = [max(range(3), key=lambda k: sum(a * b for a, b in zip(chi[k], u))) for u in race]   # among the three families
    print("leader among the three families:", {k: lead.count(k) / len(lead) for k in range(3)})

    def f3(v, d=5):
        return ", ".join(f"{x:.{d}f}f" for x in v)

    with open(OUT, "w", encoding="utf-8") as f:
        f.write("// Generated by Tools/family_sphere_data.py: -47's families on the sphere of directions and their race, for sections\n")
        f.write("// NC–ND of docs/experiments/README.md. Do not edit by hand.\n")
        f.write("public static class FamilySphereData\n{\n")
        f.write(f"    public const float TMax = {TMAX:.2f}f;\n")
        f.write("    /// <summary>The families' curve in the L-functions' coordinates, W(t) e^(τt/4) with W = (ζL(χ₋₄₇), L(χ₁), L(χ₂)) on\n")
        f.write("    /// the critical line (each a_j·V), t = 0.3 … TMax every 0.04, as x, y, z triples (only its direction matters).</summary>\n")
        f.write("    public static readonly float[] Curve = { " + ", ".join(f3(v, 4) for v in curve) + " };\n")
        f.write(f"    public const float CurveT0 = {curve_t[0]:.2f}f, CurveStep = 0.04f;\n")
        f.write("    /// <summary>The zeros of ζ itself up to TMax (each is also a zero of ζ·L(χ₋₄₇)).</summary>\n")
        f.write("    public static readonly float[] ZetaZeros = { " + ", ".join(f"{g:.4f}f" for g in zeta) + " };\n")
        f.write(f"    /// <summary>The landscape: zeros off the line up to TMax of the mixture at grid direction (i, j): polar angle\n")
        f.write(f"    /// Theta[i] (degrees) from the centre (1, 1, 1)/√3 and azimuth τ·j/{NPH} about it, in the L-functions' coordinates;\n")
        f.write(f"    /// ring {IC} passes through the three L-functions.</summary>\n")
        f.write(f"    public const int NTheta = {NTH}, NPhi = {NPH};\n")
        f.write("    public static readonly float[] Theta = { " + ", ".join(f"{x:.4f}f" for x in THETA) + " };\n")
        f.write("    public static readonly byte[] Off = { " + ", ".join(str(min(255, o)) for o in off) + " };\n")
        f.write("    /// <summary>The race u(x) = −(ν + S(x)) in the characters' coordinates (sign, ρ1, ρ2), x = 10^3 … 10^7 (1601 points,\n")
        f.write("    /// evenly in log x); a family's race is χ(C)·u.</summary>\n")
        f.write("    public static readonly float[] Race = { " + ", ".join(f3(u, 4) for u in race) + " };\n")
        f.write("    public static readonly float[] Chi = { " + ", ".join(f3(v) for v in chi) + " };\n")
        f.write("}\n")
    print("wrote", OUT, os.path.getsize(OUT), "bytes")


if __name__ == "__main__":
    main()
