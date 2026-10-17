#!/usr/bin/env python3
"""Data for "The class group as families" (sections LA–LC of docs/experiments/README.md), written to
Assets/Tour/ClassGroupData.cs.

Discriminant D = -23 has class number 3: the reduced forms x^2 + xy + 6y^2, 2x^2 + xy + 3y^2 and 2x^2 - xy + 3y^2, three
families of curves and three 2D lattices.
* Euler's one-representation test: with the principal form alone it calls 2174 composites below 20 000 prime; counting
  the representations by all three forms it calls none. (For x^2 + 11y^2, D = -44, the README's 1047 failures go to 0.)
* Each form's Epstein zeta Z_f(s), by the Chowla-Selberg formula (checked against direct lattice sums); completed,
  Lambda_f(s) = (sqrt|D|/2 pi)^s Gamma(s) Z_f(s) = Lambda_f(1 - s).
* The class group's characters combine them into L-functions: (Z_e + 2 Z_c)/2 = zeta(s) L(s, chi_-23) and
  (Z_e - Z_c)/2 = L(eta(z) eta(23z), s), checked.
* Zeros up to t = 50: on the line from sign changes of the real completed function, off it by the argument principle in
  0.55 < Re s < 2.2 (the edge sampled every 0.01, so zeros on the line cannot fool the count) and Newton. D = -20 is the
  control (its two combinations are zeta L(chi_-20) and L(chi_-4) L(chi_5): no zeros off the line).
* Every mixture Z1 + lam Z2 satisfies the same functional equation, so on the line its zeros are the crossings of the
  one curve R(t) = -Lambda1/Lambda2 with the height lam. With all of each mixture's zeros counted by the argument
  principle, the mixtures with every zero up to t = 50 on the line form two small windows round the two L-functions.

Run from the repository root:  python3 Tools/class_group_data.py [--cache DIR]   (about 30 minutes on 8 cores; with --cache the
values on the line, the box and the grid are kept, so a rerun takes seconds).
"""
import json, math, os, sys
from math import gcd
from multiprocessing import Pool
import mpmath

OUT = os.path.join("Assets", "Tour", "ClassGroupData.cs")
NT = 60

def reduced_forms(D):
    out, a = [], 1
    while 3 * a * a <= -D:
        for b in range(-a + 1, a + 1):
            if (b * b - D) % (4 * a): continue
            c = (b * b - D) // (4 * a)
            if c < a or (b < 0 and a == c) or gcd(gcd(a, abs(b)), c) != 1: continue
            out.append((a, b, c))
        a += 1
    return out

def proper_reps(form, n):
    a, b, c = form
    D = b * b - 4 * a * c
    reps = set()
    ymax = math.isqrt(4 * a * n // -D) + 1
    for y in range(-ymax, ymax + 1):
        disc = b * b * y * y - 4 * a * (c * y * y - n)
        if disc < 0: continue
        r = math.isqrt(disc)
        if r * r != disc: continue
        for sg in (1, -1):
            num = -b * y + sg * r
            if num % (2 * a) == 0 and gcd(num // (2 * a), y) == 1: reps.add((num // (2 * a), y))
    return reps

def primepower(n):
    p = 2
    while n % p: p += 1
    while n % p == 0: n //= p
    return n == 1

def euler_counts(D, principal, LIM=20000):
    forms = reduced_forms(D)
    sym = 4 if principal[1] in (0, principal[0]) else 2       # an ambiguous principal form has an improper automorph
    fp = fa = 0
    for n in range(2, LIM):
        if gcd(n, abs(D)) != 1: continue
        rp = len(proper_reps(principal, n))
        rt = sum(len(proper_reps(f, n)) for f in forms)
        if rp == sym and not primepower(n): fp += 1
        if rt == 4 and not primepower(n): fa += 1
    return forms, fp, fa

# ---- Epstein zeta by Chowla-Selberg -----------------------------------------------------------------------------------------
def Z(form, D, s):
    a, b, c = form
    s = mpmath.mpc(s)
    aD = abs(D)
    t1 = 2 * mpmath.zeta(2 * s) * mpmath.power(a, -s)
    t2 = mpmath.power(2, 2 * s) * mpmath.power(a, s - 1) * mpmath.sqrt(mpmath.pi) * mpmath.gamma(s - 0.5) * mpmath.zeta(2 * s - 1) / (mpmath.gamma(s) * mpmath.power(aD, s - 0.5))
    pref = mpmath.power(2, s + 2.5) * mpmath.power(mpmath.pi, s) / (mpmath.sqrt(a) * mpmath.gamma(s) * mpmath.power(aD, s / 2 - 0.25))
    tot = mpmath.mpc(0)
    for n in range(1, NT):
        sig = mpmath.fsum(mpmath.power(d, 1 - 2 * s) for d in range(1, n + 1) if n % d == 0)
        tot += mpmath.power(n, s - 0.5) * sig * mpmath.cos(n * mpmath.pi * b / a) * mpmath.besselk(s - 0.5, mpmath.pi * n * mpmath.sqrt(aD) / a)
    return t1 + t2 + pref * tot

def lam(args):
    D, forms, s = args
    mpmath.mp.dps = 25
    s = mpmath.mpc(s)
    g = mpmath.power(mpmath.sqrt(abs(D)) / (2 * mpmath.pi), s) * mpmath.gamma(s)
    return [complex(g * Z(f, D, s)) for f in forms]

def winding(v):
    tot = 0.0
    for a, b in zip(v, v[1:]):
        q = b / a
        tot += math.atan2(q.imag, q.real)
    return tot / (2 * math.pi)

def newton(args):
    D, forms, w, z = args
    f = lambda u: sum(wi * vi for wi, vi in zip(w, lam((D, forms, u))))
    for _ in range(50):
        v = f(z)
        d = (f(z + 1e-7) - f(z - 1e-7)) / 2e-7
        step = v / d
        z -= step
        if abs(step) < 1e-12: break
    return z, abs(f(z)) / abs(f(z + 0.05))

def checks():
    mpmath.mp.dps = 25
    for form, D in (((1, 1, 6), -23), ((2, 1, 3), -23), ((1, 0, 5), -20), ((2, 2, 3), -20)):
        a, b, c = form
        direct = mpmath.fsum(mpmath.power(a * x * x + b * x * y + c * y * y, -3) for x in range(-150, 151) for y in range(-150, 151) if x or y)
        print(f"Chowla-Selberg {form} at s = 3: {mpmath.nstr(Z(form, D, 3).real, 11)}   direct (150): {mpmath.nstr(direct, 11)}")
    s = mpmath.mpc(2.3, 1.7)
    ze, zc = Z((1, 1, 6), -23, s), Z((2, 1, 3), -23, s)
    chi = [0] + [(1 if pow(n, 11, 23) == 1 else -1) if n % 23 else 0 for n in range(1, 23)]
    M = 2000
    cf = [0] * (M + 1); cf[0] = 1
    for n in range(1, M + 1):
        for st in (n, 23 * n):
            if st <= M:
                for k in range(M, st - 1, -1): cf[k] -= cf[k - st]
    eta = mpmath.fsum(cf[n - 1] * mpmath.power(n, -s) for n in range(1, M + 1))
    print("(Z_e + 2 Z_c)/2 =", mpmath.nstr((ze + 2 * zc) / 2, 10), "   zeta L(chi_-23) =", mpmath.nstr(mpmath.zeta(s) * mpmath.dirichlet(s, chi), 10))
    print("(Z_e - Z_c)/2   =", mpmath.nstr((ze - zc) / 2, 10), "   L(eta(z) eta(23z)) (2000 terms) =", mpmath.nstr(eta, 10))

def main():
    cache = sys.argv[sys.argv.index("--cache") + 1] if "--cache" in sys.argv else None
    checks()
    forms23, fp23, fa23 = euler_counts(-23, (1, 1, 6))
    forms44, fp44, fa44 = euler_counts(-44, (1, 0, 11))
    print(f"Euler, D = -23: principal alone {fp23} composites below 20000, all {len(forms23)} forms {fa23};  D = -44: {fp44} -> {fa44}")
    path = os.path.join(cache, "class_zeros.json") if cache else None
    lines, boxes = {}, {}
    if path and os.path.exists(path):
        Zs = json.load(open(path))
    else:
        Zs = {}
        T, x0, x1 = 50.0, 0.55, 2.2
        with Pool(8) as pool:
            for D, forms, funcs in ((-23, [(1, 1, 6), (2, 1, 3)], {"e": [1, 0], "c": [0, 1], "L0": [.5, 1], "L1": [.5, -.5]}),
                                    (-20, [(1, 0, 5), (2, 2, 3)], {"e": [1, 0], "c": [0, 1], "L0": [.5, .5], "L1": [.5, -.5]})):
                line = [0.5 + 1j * (0.02 * k + 0.02) for k in range(int(T / 0.02))]
                # the grid for the zeros right of the line: Newton from every local minimum of |f| (edges included)
                xs = [round(0.5 + 0.05 * i, 2) for i in range(37)]
                ys = [round(0.25 * j, 2) for j in range(1, 201)]
                grid = [complex(x, y) for y in ys for x in xs]
                nL = int((T - 0.3) / 0.01)
                box = ([complex(x0 + (x1 - x0) * k / 170, 0.3) for k in range(170)] + [complex(x1, 0.3 + (T - 0.3) * k / 500) for k in range(500)] +
                       [complex(x1 - (x1 - x0) * k / 170, T) for k in range(170)] + [complex(x0, T - (T - 0.3) * k / nL) for k in range(nL)] + [complex(x0, 0.3)])
                part = os.path.join(cache, f"edges{-D}.json") if cache else None
                if part and os.path.exists(part):
                    vl, vb, gv = [[[complex(*z) for z in v] for v in vs] for vs in json.load(open(part))]
                else:
                    vl = pool.map(lam, [(D, forms, s) for s in line])
                    vb = pool.map(lam, [(D, forms, s) for s in box])
                    gv = pool.map(lam, [(D, forms, z) for z in grid])
                    if part: json.dump([[[[z.real, z.imag] for z in v] for v in vs] for vs in (vl, vb, gv)], open(part, "w"))
                lines[D], boxes[D] = vl, vb
                for name, w in funcs.items():
                    comb = lambda v: sum(wi * vi for wi, vi in zip(w, v))
                    on = [comb(v) for v in vl]
                    onz = []
                    for k in range(len(on) - 1):
                        if on[k].real * on[k + 1].real < 0:
                            a, b, fa, fb = line[k].imag, line[k + 1].imag, on[k].real, on[k + 1].real
                            onz.append(a + (b - a) * fa / (fa - fb))
                    off_n = round(winding([comb(v) for v in vb]))
                    off = []
                    if off_n:
                        g = {(z.real, z.imag): math.log(abs(comb(v)) + 1e-300) for z, v in zip(grid, gv)}
                        nx, ny = len(xs), len(ys)
                        mins = [complex(xs[i], ys[j]) for j in range(ny) for i in range(1, nx)
                                if all(g[(xs[i], ys[j])] < g[(xs[i + p], ys[j + q])] for p in (-1, 0, 1) for q in (-1, 0, 1)
                                       if (p or q) and 0 <= i + p < nx and 0 <= j + q < ny)]
                        for z, rel in pool.map(newton, [(D, forms, w, m) for m in mins]):
                            if rel < 1e-10 and x0 < z.real < x1 and 0 < z.imag < T and all(abs(z - complex(*q)) > 1e-6 for q in off): off.append([z.real, z.imag])
                    print(f"D={D} {name:3}: {len(onz)} zeros on Re s = 1/2 to t = 50; {off_n} right of the line (found {len(off)}):",
                          ", ".join(f"{a:.4f}+{b:.3f}i" for a, b in sorted(off, key=lambda q: q[1])), flush=True)
                    assert len(off) == off_n
                    Zs[f"{D} {name}"] = {"on": onz, "off": sorted(off, key=lambda z: z[1])}
        if path: json.dump(Zs, open(path, "w"))
    for k, v in Zs.items():
        print(f"{k:8}: {len(v['on'])} on the line, {2 * len(v['off'])} off it:", ", ".join(f"{a:.3f}+{b:.2f}i" for a, b in v["off"]))

    # every mixture Z1 + lam Z2 has the same functional equation, so on the line its completed function e + lam c is real,
    # and its zeros there are the crossings of the one curve lam = R(t) = -e/c with the height lam
    if -23 not in lines:
        lines[-23], boxes[-23] = ([[complex(*z) for z in v] for v in vs] for vs in json.load(open(os.path.join(cache, "edges23.json")))[:2])
    e = [v[0].real for v in lines[-23]]; c = [v[1].real for v in lines[-23]]
    # the total number of zeros of each mixture with 0.3 < t <= 50, by the argument principle on the right half of the
    # box (bottom edge, right edge at Re s = 2.2, top edge back to the line): N = (1/pi) * the change in argument
    nb = len(boxes[-23])
    k03, k50 = round((0.3 - 0.02) / 0.02), len(lines[-23]) - 1
    path = [lines[-23][k03]] + boxes[-23][:170 + 500 + 170] + [lines[-23][k50]]
    def total(lam):
        vals = [a + lam * b for a, b in path]
        return round(sum(math.atan2((q / p).imag, (q / p).real) for p, q in zip(vals, vals[1:])) / math.pi)
    R = [-a / b for a, b in zip(e, c)]
    lams = [round(-3 + 0.01 * k, 2) for k in range(701)]
    def on_line(lam):
        f = [a + lam * b for a, b in zip(e, c)]
        return sum(1 for i in range(len(f) - 1) if f[i] * f[i + 1] < 0)
    counts = [on_line(l) for l in lams]
    off = [total(l) - n for l, n in zip(lams, counts)]
    assert min(off) >= 0, min(off)
    windows, k = [], 0
    while k < len(lams):
        if off[k] == 0:
            j = k
            while j + 1 < len(lams) and off[j + 1] == 0:
                j += 1
            windows.append((lams[k], lams[j]))
            k = j
        k += 1
    print("mixtures with every zero up to t = 50 on the line: lam in", windows, "; off the line at lam = 0:", off[300])
    best = sorted(range(len(lams)), key=lambda k: -counts[k])[:6]
    print("zeros on the line of Z1 + lam Z2 (t <= 50): most at", [(lams[k], counts[k]) for k in best],
          "; at lam = 0:", counts[300], "; the L-functions:", counts[500], counts[200])
    assert counts[500] == len(Zs["-23 L0"]["on"]) and counts[200] == len(Zs["-23 L1"]["on"])

    def arr(name, vals, doc, fmt="{:.4f}"):
        return f"    /// <summary>{doc}</summary>\n    public static readonly double[] {name} = {{ " + ", ".join(fmt.format(x) for x in vals) + " };\n"
    with open(OUT, "w", encoding="utf-8") as f:
        f.write("// Generated by Tools/class_group_data.py: the zeros of the Epstein zeta functions of the forms of discriminant -23 (and\n")
        f.write("// -20) and of their class-group combinations, for the sections LA–LC of docs/experiments/README.md. Do not edit by hand.\n")
        f.write("public static class ClassGroupData\n{\n")
        f.write(f"    /// <summary>Composites below 20 000 that Euler's test calls prime: with the principal form alone, and with all forms.</summary>\n")
        f.write(f"    public const int Euler23Principal = {fp23}, Euler23All = {fa23}, Euler44Principal = {fp44}, Euler44All = {fa44};\n")
        for D in (-23, -20):
            for name in ("e", "c", "L0", "L1"):
                v = Zs[f"{D} {name}"]
                tag = f"{'M23' if D == -23 else 'M20'}{name}"
                f.write(arr(f"On{tag}", v["on"], f"D = {D}, {name}: the zeros on Re s = 1/2 up to t = 50 (imaginary parts)."))
                f.write(arr(f"OffRe{tag}", [a for a, b in v["off"]], f"D = {D}, {name}: the zeros right of the line (their mirrors are at 1 − Re s): real parts."))
                f.write(arr(f"OffIm{tag}", [b for a, b in v["off"]], "Their imaginary parts."))
        f.write(arr("MixR", R, "D = −23: R(t) = −Λ₁/Λ₂ on Re s = ½ at t = 0.02, 0.04, … 50 (Λ₁, Λ₂ the completed zetas of the two families).", "{:.4g}"))
        f.write("    /// <summary>Zeros on Re s = ½ up to t = 50 of Λ₁ + λΛ₂ for λ = −3, −2.99, … 4.</summary>\n")
        f.write("    public static readonly int[] MixOnLine = { " + ", ".join(str(n) for n in counts) + " };\n")
        f.write("    /// <summary>Zeros off Re s = ½ up to t = 50 of Λ₁ + λΛ₂ (all of its zeros, by the argument principle, minus those on the line).</summary>\n")
        f.write("    public static readonly int[] MixOffLine = { " + ", ".join(str(n) for n in off) + " };\n")
        f.write("}\n")
    print("wrote", OUT, os.path.getsize(OUT), "bytes")

if __name__ == "__main__":
    main()
