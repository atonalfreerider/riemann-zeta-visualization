#!/usr/bin/env python3
"""Data for "Five families and a landscape of mixtures" (section LF of docs/experiments/README.md), written to
Assets/Tour/ClassFiveData.cs.

Discriminant -47 has class number 5: the reduced forms x^2 + xy + 12y^2 (the principal class), 2x^2 +- xy + 6y^2 (the classes
g and g^-1, a form and its mirror image) and 3x^2 +- xy + 4y^2 (g^2 and g^-2; 2 lies on 2x^2 + xy + 6y^2 and 4 = 2^2 on
3x^2 + xy + 4y^2). So there are three distinct zeta functions Z1, Z2, Z3 and a plane of mixtures Z1 + l2 Z2 + l3 Z3. The class
group C5 has three real characters' worth of L-functions: (Z1 + 2 c_k Z2 + 2 c_2k Z3)/2 with c_j = cos(2 pi j/5), k = 0, 1, 2,
that is the mixtures (l2, l3) = (2, 2), (0.618, -1.618) and (-1.618, 0.618).
* Chowla-Selberg against direct lattice sums; (Z1 + 2Z2 + 2Z3)/2 = zeta(s) L(s, chi_-47); the coefficients of the other two
  combinations are multiplicative (an Euler product), checked for coprime m, n with mn <= 400.
* A prime's family is the splitting of a quintic mod p: five roots on the principal family, none on the others, one when
  (-47/p) = -1. The quintic (the smallest whose discriminant is 47^2) is found by search and checked for p < 3000.
* Every mixture has the same functional equation, so on Re s = 1/2 it is real, and its zeros there are the t where
  Lambda1 + l2 Lambda2 + l3 Lambda3 = 0: for each t a line in the (l2, l3) plane. The completed functions on the line are
  written out (t = 0.02 ... 50); the landscape of zero counts over the plane is drawn from them in Unity.

Run from the repository root:  python3 Tools/class_five_data.py [--cache DIR]   (about 5 minutes on 8 cores).
"""
import itertools, json, math, os, sys
from fractions import Fraction
from math import gcd
from multiprocessing import Pool

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import mpmath
from class_group_data import Z, lam, reduced_forms, proper_reps

OUT = os.path.join("Assets", "Tour", "ClassFiveData.cs")
D = -47
FORMS = [(1, 1, 12), (2, 1, 6), (3, 1, 4)]
C1, C2 = math.cos(2 * math.pi / 5), math.cos(4 * math.pi / 5)
LPOINTS = [(2.0, 2.0), (2 * C1, 2 * C2), (2 * C2, 2 * C1)]
T0, TSTEP, NT = 0.02, 0.02, 2500


def reps(f, n):
    """All representations of n by the form f (primitive or not)."""
    a, b, c = f
    lim = int(math.isqrt(4 * a * n // (4 * a * c - b * b))) + 2
    return sum(1 for y in range(-lim, lim + 1) for x in range(-4 * lim - 2, 4 * lim + 3) if a * x * x + b * x * y + c * y * y == n)


def disc(coeffs):
    """Discriminant of a monic polynomial (coefficients from the top), by the resultant of f and f'."""
    n = len(coeffs) - 1
    f = [Fraction(c) for c in coeffs]
    df = [Fraction(c * (n - i)) for i, c in enumerate(coeffs[:-1])]
    m = len(df) - 1
    size = n + m
    rows = []
    for i in range(m):
        rows.append([Fraction(0)] * i + f + [Fraction(0)] * (size - i - len(f)))
    for i in range(n):
        rows.append([Fraction(0)] * i + df + [Fraction(0)] * (size - i - len(df)))
    det = Fraction(1)
    for col in range(size):
        piv = next((r for r in range(col, size) if rows[r][col] != 0), None)
        if piv is None:
            return 0
        if piv != col:
            rows[col], rows[piv] = rows[piv], rows[col]
            det = -det
        det *= rows[col][col]
        for r in range(col + 1, size):
            k = rows[r][col] / rows[col][col]
            if k:
                rows[r] = [x - k * y for x, y in zip(rows[r], rows[col])]
    return int((-1) ** (n * (n - 1) // 2) * det)


def roots_mod(coeffs, p):
    return sum(1 for x in range(p) if sum(c * pow(x, len(coeffs) - 1 - i, p) for i, c in enumerate(coeffs)) % p == 0)


def isprime(n):
    return n > 1 and all(n % d for d in range(2, int(n ** .5) + 1))


def line_values(args):
    t, = args
    return [v.real for v in lam((D, FORMS, complex(0.5, t)))]


def checks():
    mpmath.mp.dps = 25
    for f in FORMS:
        a, b, c = f
        direct = mpmath.fsum(mpmath.power(a * x * x + b * x * y + c * y * y, -3) for x in range(-150, 151) for y in range(-150, 151) if x or y)
        print(f"Chowla-Selberg {f} at s = 3: {mpmath.nstr(Z(f, D, 3).real, 11)}   direct (150): {mpmath.nstr(direct, 11)}")
    s = mpmath.mpc(2.3, 1.7)
    z1, z2, z3 = (Z(f, D, s) for f in FORMS)
    chi = [0] + [(1 if pow(n, 23, 47) == 1 else -1) if n % 47 else 0 for n in range(1, 47)]
    print("(Z1 + 2Z2 + 2Z3)/2 =", mpmath.nstr((z1 + 2 * z2 + 2 * z3) / 2, 10), "   zeta L(chi_-47) =",
          mpmath.nstr(mpmath.zeta(s) * mpmath.dirichlet(s, chi), 10))
    N = 400
    r = {f: [0] + [reps(f, n) for n in range(1, N + 1)] for f in FORMS}
    worst = 0.0
    for k in (1, 2):
        ck, c2k = math.cos(2 * math.pi * k / 5), math.cos(4 * math.pi * k / 5)
        a = [0] + [(r[FORMS[0]][n] + 2 * ck * r[FORMS[1]][n] + 2 * c2k * r[FORMS[2]][n]) / 2 for n in range(1, N + 1)]
        assert abs(a[1] - 1) < 1e-12
        for m in range(2, N + 1):
            for n in range(2, N // m + 1):
                if gcd(m, n) == 1:
                    worst = max(worst, abs(a[m * n] - a[m] * a[n]))
    print(f"the coefficients of the two other combinations are multiplicative (mn <= {N}): largest error {worst:.1e}")
    assert worst < 1e-9


def main():
    cache = sys.argv[sys.argv.index("--cache") + 1] if "--cache" in sys.argv else None
    forms = reduced_forms(D)
    assert sorted(forms) == sorted([(1, 1, 12), (2, -1, 6), (2, 1, 6), (3, -1, 4), (3, 1, 4)])
    assert proper_reps((2, 1, 6), 2) and proper_reps((3, 1, 4), 4) and not proper_reps((1, 1, 12), 4)
    checks()
    # the quintic: the smallest monic one (coefficients -2 ... 2) with discriminant 47^2 whose splitting matches the families
    def family(p):
        if p == 47:
            return None
        if proper_reps((1, 1, 12), p):
            return 0
        if proper_reps((2, 1, 6), p) or proper_reps((2, -1, 6), p):
            return 1
        if proper_reps((3, 1, 4), p) or proper_reps((3, -1, 4), p):
            return 2
        return 3
    primes = [p for p in range(2, 3000) if isprime(p) and p != 47]
    fam = {p: family(p) for p in primes}
    want = {0: 5, 1: 0, 2: 0, 3: 1}
    quintic = None
    for co in sorted(itertools.product(range(-2, 3), repeat=5), key=lambda c: (sum(map(abs, c)), c)):
        coeffs = [1, *co]
        if disc(coeffs) != 47 * 47:
            continue
        if all(roots_mod(coeffs, p) == want[fam[p]] for p in primes):
            quintic = coeffs
            break
    assert quintic, "no quintic found"
    counts = [sum(1 for p in primes if fam[p] == k) for k in range(4)]
    names = ["x^2 + xy + 12y^2", "2x^2 +- xy + 6y^2", "3x^2 +- xy + 4y^2", "neither"]
    print("quintic:", quintic, " discriminant", disc(quintic), "; primes below 3000:", dict(zip(names, counts)))
    print("  first on each family:", {names[k]: [p for p in primes if fam[p] == k][:6] for k in range(4)})
    path = os.path.join(cache, "line47.json") if cache else None
    if path and os.path.exists(path):
        vals = json.load(open(path))
    else:
        ts = [T0 + TSTEP * k for k in range(NT)]
        with Pool(8) as pool:
            vals = pool.map(line_values, [(t,) for t in ts])
        if path:
            os.makedirs(cache, exist_ok=True)
            json.dump(vals, open(path, "w"))
    e, g, h = ([v[i] for v in vals] for i in range(3))
    # the right half of the box round the zeros with 0.3 < t <= 50: bottom edge, Re s = 2.2, top edge back to the line
    x0, x1, T = 0.55, 2.2, 50.0
    edge = ([complex(x0 + (x1 - x0) * k / 170, 0.3) for k in range(170)] + [complex(x1, 0.3 + (T - 0.3) * k / 500) for k in range(500)]
            + [complex(x1 - (x1 - x0) * k / 170, T) for k in range(170)])
    epath = os.path.join(cache, "path47.json") if cache else None
    if epath and os.path.exists(epath):
        pv = [[complex(*z) for z in v] for v in json.load(open(epath))]
    else:
        with Pool(8) as pool:
            pv = pool.map(lam, [(D, FORMS, z) for z in edge])
        if epath:
            json.dump([[[z.real, z.imag] for z in v] for v in pv], open(epath, "w"))
    k03 = round((0.3 - T0) / TSTEP)
    path = [[complex(x, 0) for x in (e[k03], g[k03], h[k03])]] + pv + [[complex(x, 0) for x in (e[-1], g[-1], h[-1])]]

    def total(l2, l3):
        vals = [a + l2 * b + l3 * c for a, b, c in path]
        return round(sum(math.atan2((q / p).imag, (q / p).real) for p, q in zip(vals, vals[1:])) / math.pi)

    def on_line(l2, l3):
        f = [a + l2 * b + l3 * c for a, b, c in zip(e, g, h)]
        return sum(1 for i in range(len(f) - 1) if f[i] * f[i + 1] < 0)

    named = {"Z1 alone": (0, 0), "zeta L(chi_-47)": LPOINTS[0], "L1": LPOINTS[1], "L2": LPOINTS[2]}
    for k, (l2, l3) in named.items():
        n_on, n_all = on_line(l2, l3), total(l2, l3)
        print(f"zeros up to t = 50 of {k:16} ({l2:+.3f}, {l3:+.3f}): {n_all}, on the line {n_on}, off it {n_all - n_on}")
    for k in range(3):
        assert total(*LPOINTS[k]) == on_line(*LPOINTS[k]), "an L-function with a zero off the line?"
    grid = [(l2 / 10, l3 / 10) for l2 in range(-30, 31, 2) for l3 in range(-30, 31, 2)]
    offs = [total(*p) - on_line(*p) for p in grid]
    assert min(offs) >= 0, min(offs)
    print(f"on a coarse grid of [-3, 3]^2 (step 0.2): {offs.count(0)} of {len(grid)} mixtures have no zero off the line:",
          [p for p, o in zip(grid, offs) if o == 0], "; most off:", max(offs))
    peaks = [on_line(*p) for p in LPOINTS]
    with open(OUT, "w", encoding="utf-8") as f:
        f.write("// Generated by Tools/class_five_data.py: the three zeta functions of the forms of discriminant -47 on the critical\n")
        f.write("// line, for section LF of docs/experiments/README.md. Do not edit by hand.\n")
        f.write("public static class ClassFiveData\n{\n")
        f.write(f"    public const float T0 = {T0}f, TStep = {TSTEP}f;\n")
        for name, arr, doc in (("LineE", e, "Λ₁(½ + it): x² + xy + 12y² (the principal class), t = T0 + k·TStep"),
                               ("Line2", g, "Λ₂(½ + it): 2x² ± xy + 6y² (the classes g, g⁻¹)"),
                               ("Line3", h, "Λ₃(½ + it): 3x² ± xy + 4y² (the classes g², g⁻²)")):
            f.write(f"    /// <summary>{doc}.</summary>\n")
            f.write(f"    public static readonly float[] {name} = {{ " + ", ".join(f"{v:.6g}f" for v in arr) + " };\n")
        f.write("    /// <summary>The mixtures Λ₁ + λ₂Λ₂ + λ₃Λ₃ that are the class group's L-functions: ζ(s)L(s, χ₋₄₇) and the two others.</summary>\n")
        f.write("    public static readonly float[] PeakL2 = { " + ", ".join(f"{p[0]:.6f}f" for p in LPOINTS) + " };\n")
        f.write("    public static readonly float[] PeakL3 = { " + ", ".join(f"{p[1]:.6f}f" for p in LPOINTS) + " };\n")
        f.write("    public static readonly int[] PeakZeros = { " + ", ".join(str(n) for n in peaks) + " };\n")
        f.write("    /// <summary>The three completed functions round the right half of the box 0.55 ≤ Re s ≤ 2.2, 0.3 ≤ t ≤ 50: the bottom\n")
        f.write("    /// edge (170 points), Re s = 2.2 (500) and the top edge back towards the line (170). With the line's values at t = 0.3\n")
        f.write("    /// and t = 50 at the ends, the change in argument of a mixture along it, over π, is the number of its zeros.</summary>\n")
        for i, name in enumerate(("E", "2", "3")):
            f.write(f"    public static readonly float[] PathRe{name} = {{ " + ", ".join(f"{v[i].real:.6g}f" for v in pv) + " };\n")
            f.write(f"    public static readonly float[] PathIm{name} = {{ " + ", ".join(f"{v[i].imag:.6g}f" for v in pv) + " };\n")
        f.write(f"    public const int PathStart = {k03};\n")
        f.write("    /// <summary>The quintic whose splitting mod p is a prime's family, from x⁵ down; primes below 3000 on each family.</summary>\n")
        f.write("    public static readonly int[] Quintic = { " + ", ".join(str(c) for c in quintic) + " };\n")
        f.write("    public static readonly int[] FamilyPrimes = { " + ", ".join(str(c) for c in counts) + " };\n")
        f.write("}\n")
    print("wrote", OUT, os.path.getsize(OUT), "bytes")


if __name__ == "__main__":
    main()
