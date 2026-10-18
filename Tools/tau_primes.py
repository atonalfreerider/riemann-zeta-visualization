#!/usr/bin/env python3
"""
The digits of τ and the primes (docs/experiments/README.md, section R). Four checks, each printing the numbers the README
quotes:

  python3 Tools/tau_primes.py products        # RA: τ from Euler products, τ^(2k) = (2k − 1)! · c · ζ(2k); 8·L(1, χ₄)
  python3 Tools/tau_primes.py readout 50,400  # RB: read the primes out of D digits of τ (3200 digits take ~4 min)
  python3 Tools/tau_primes.py arms            # RC: the helix of one radian per number, its arms mod 6, 44, 710 and 30
  python3 Tools/tau_primes.py digits          # RD: τ's digits in base 10, 30 and e and its continued fraction, tested
                                              #     against e, √2, φ and a seeded random sequence
"""
import math
import random
import sys

import mpmath

sys.set_int_max_str_digits(0)
sys.setrecursionlimit(10000)


def primes(n):
    s = bytearray([1]) * (n + 1)
    s[0] = s[1] = 0
    for i in range(2, int(n ** .5) + 1):
        if s[i]:
            s[i * i::i] = bytearray(len(s[i * i::i]))
    return s


def prime_list(n):
    s = primes(n)
    return [i for i in range(n + 1) if s[i]]


# ---------------------------------------------------------------------------------------------------------------------
# RA. one turn from the primes
# ---------------------------------------------------------------------------------------------------------------------

def products():
    mpmath.mp.dps = 60
    tau = 2 * mpmath.pi
    pr = prime_list(10000)
    marks = (0, 2, 3, 5, 7, 97, 997, 9973)
    for name, s, c in (("τ² = 1!·24·ζ(2)", 2, 24), ("τ⁴ = 3!·240·ζ(4)  (240: E8's roots)", 4, 6 * 240),
                       ("τ⁶ = 5!·504·ζ(6)", 6, 120 * 504), ("τ⁸ = 7!·480·ζ(8)  (480: E8⊕E8's roots)", 8, 5040 * 480)):
        assert abs(c * mpmath.zeta(s) - tau ** s) < mpmath.mpf(10) ** -45
        print(f"== {name}")
        P = mpmath.mpf(c)
        rows = [(0, P)]
        for p in pr:
            P /= 1 - mpmath.mpf(p) ** -s
            if p in marks:
                rows.append((p, P))
        for p, v in rows:
            t = v ** (mpmath.mpf(1) / s)
            gap = tau - t
            print(f"   primes ≤ {p:>5}:  short of a turn by {mpmath.nstr(gap * 180 / mpmath.pi, 3):>10}°   "
                  f"({float(-mpmath.log10(gap / tau)):.1f} digits of τ)")
    # τ = 8 Π over odd primes p/(p − χ₄(p)): conditionally convergent; how often it is above τ (Chebyshev's bias)
    n = 10_000_000
    s = primes(n)
    lp, lt, above, total = math.log(8), math.log(2 * math.pi), 0, 0
    for p in range(3, n + 1, 2):
        if s[p]:
            lp += math.log(p / (p - (1 if p % 4 == 1 else -1)))
            if p > 100:
                total += 1
                above += lp > lt
    print(f"== τ = 8 Π p/(p − χ₄(p)): above τ after {above / total:.4f} of the odd primes 100 < p ≤ {n:,}")


# ---------------------------------------------------------------------------------------------------------------------
# RB. reading the primes back out of τ
# ---------------------------------------------------------------------------------------------------------------------

def tangent_numbers(n):
    """T_1 … T_n (1, 2, 16, 272, 7936, …), Brent and Harvey's in-place recurrence: integers only, no primes."""
    t = [0] * (n + 1)
    t[1] = 1
    for k in range(2, n + 1):
        t[k] = (k - 1) * t[k - 1]
    for k in range(2, n + 1):
        for j in range(k, n + 1):
            t[j] = (j - k) * t[j - 1] + (j - k + 2) * t[j]
    return t


def read_primes(tau_text, D, T):
    """The primes, read in order from D digits of τ (tau_text "6.2831…") and the tangent numbers T."""
    mpmath.mp.dps = D + 10
    pi = mpmath.mpf(tau_text[:D + 2]) / 2
    left = {}
    fact = mpmath.mpf(1)
    for k in range(1, len(T)):
        if k > 1:
            fact *= (2 * k - 2) * (2 * k - 1)
        left[2 * k] = mpmath.log(pi ** (2 * k) * T[k] / (2 * (mpmath.mpf(4) ** k - 1) * fact))   # log ζ(2k)
    floor = mpmath.mpf(10) ** (-(D - 5))
    found = []
    while True:
        best, prev = None, None
        for s in sorted(left):
            if left[s] < floor * 1e3:
                break
            # what is left is p^(−s)(1 + (p/p′)^s + …): the estimate creeps up on p; take it once it sits on a whole
            # number and has stopped moving
            est = left[s] ** (-mpmath.mpf(1) / s)
            r = int(mpmath.nint(est))
            if prev is not None and abs(est - r) < 1e-3 and abs(est - prev) < 1e-4:
                best = r
                break
            prev = est
        if best is None or (found and best <= found[-1]):
            return found
        found.append(best)
        for s in left:
            left[s] += mpmath.log(1 - mpmath.mpf(best) ** -s)


def readout(spec):
    Ds = [int(x) for x in spec.split(",")]
    mpmath.mp.dps = max(Ds) + 30
    tau_text = mpmath.nstr(2 * mpmath.pi, max(Ds) + 20, strip_zeros=False)
    T = tangent_numbers(min(2400, max(300, max(Ds) * 3 // 4)))
    truth = prime_list(20000)
    for D in Ds:
        got = read_primes(tau_text, D, T)
        ok = 0
        while ok < len(got) and got[ok] == truth[ok]:
            ok += 1
        print(f"{D:>6} digits of τ:  {len(got)} primes read, {ok} right (up to {truth[ok - 1] if ok else '-'})")


# ---------------------------------------------------------------------------------------------------------------------
# RC. the helix of one radian per number
# ---------------------------------------------------------------------------------------------------------------------

def arms():
    mpmath.mp.dps = 50
    tau = 2 * mpmath.pi
    x, cf = tau, []
    for _ in range(12):
        a = int(mpmath.floor(x))
        cf.append(a)
        x = 1 / (x - a)
    h0, h1, k0, k1 = 1, cf[0], 0, 1
    conv = [(h1, k1)]
    for a in cf[1:8]:
        h0, h1 = h1, a * h1 + h0
        k0, k1 = k1, a * k1 + k0
        conv.append((h1, k1))
    print("τ = [" + str(cf[0]) + "; " + ", ".join(map(str, cf[1:])) + ", …];  convergents " + ", ".join(f"{h}/{k}" for h, k in conv))
    n = 10_000_000
    s = primes(n)
    pr = [i for i in range(n + 1) if s[i]]
    for q, k in ((6, 1), (44, 7), (710, 113), (30, None)):
        cnt = [0] * q
        for p in pr:
            cnt[p % q] += 1
        cop = [r for r in range(q) if math.gcd(r, q) == 1]
        drift = f"{float(q - k * tau):+.3e} rad" if k else "exact"
        print(f"q = {q:>4} ({drift}): primes ≤ {n:,} on {len(cop)} coprime arms, "
              f"{min(cnt[r] for r in cop)}–{max(cnt[r] for r in cop)} on each")


# ---------------------------------------------------------------------------------------------------------------------
# RD. τ's digits as data
# ---------------------------------------------------------------------------------------------------------------------

def chi2_p(stat, dof):
    return float(mpmath.gammainc(dof / 2, stat / 2, mpmath.inf, regularized=True))


def chi2_uniform(counts):
    n = sum(counts)
    e = n / len(counts)
    return chi2_p(sum((c - e) ** 2 / e for c in counts), len(counts) - 1)


def chi2_homog(a, b):
    na, nb = sum(a), sum(b)
    st = 0.0
    for x, y in zip(a, b):
        t = x + y
        if t:
            ea, eb = t * na / (na + nb), t * nb / (na + nb)
            st += (x - ea) ** 2 / ea + (y - eb) ** 2 / eb
    return chi2_p(st, len(a) - 1)


def to_base(x, b, k):
    if k <= 64:
        out = []
        for _ in range(k):
            x, r = divmod(x, b)
            out.append(r)
        return out[::-1]
    h = k // 2
    hi, lo = divmod(x, b ** h)
    return to_base(hi, b, k - h) + to_base(lo, b, h)


def digits():
    N = 200000
    mpmath.mp.dps = N + 40
    consts = {"tau": 2 * mpmath.pi, "e": mpmath.e, "sqrt2": mpmath.sqrt(2), "phi": (1 + mpmath.sqrt(5)) / 2}
    dec, b30 = {}, {}
    k30 = int(N * 0.677) - 10
    for name, v in consts.items():
        dec[name] = mpmath.nstr(v, N + 20, strip_zeros=False).split(".")[1][:N]
        b30[name] = to_base(int(mpmath.floor((v - mpmath.floor(v)) * mpmath.mpf(30) ** k30)), 30, k30)
    mpmath.mp.dps = 30                       # the χ² tails need no more (at 200 000 digits they take minutes each)
    random.seed(1)
    dec["random"] = "".join(random.choice("0123456789") for _ in range(N))
    b30["random"] = [random.randrange(30) for _ in range(k30)]
    s10, s30 = primes(N), primes(k30)
    gold = {1, 7, 11, 13, 17, 19, 23, 29}
    print(f"p-values ({N} base-10 digits, {k30} base-30 digits after the point)")
    print(f"{'':8} {'10 uniform':>11} {'30 uniform':>11} {'10 prime pos':>13} {'30 prime pos':>13} {'gold share':>11} {'p':>7}")
    for name in ["tau", "e", "sqrt2", "phi", "random"]:
        d10 = [int(c) for c in dec[name]]
        c10, c30 = [0] * 10, [0] * 30
        pa, pb, qa, qb = [0] * 10, [0] * 10, [0] * 30, [0] * 30
        for k, x in enumerate(d10, 1):
            c10[x] += 1
            (pa if s10[k] else pb)[x] += 1
        for k, x in enumerate(b30[name], 1):
            c30[x] += 1
            (qa if s30[k] else qb)[x] += 1
        g = sum(1 for x in b30[name] if x in gold)
        z = (g - k30 * 8 / 30) / math.sqrt(k30 * 8 / 30 * 22 / 30)
        print(f"{name:8} {chi2_uniform(c10):11.3f} {chi2_uniform(c30):11.3f} {chi2_homog(pa, pb):13.3f} {chi2_homog(qa, qb):13.3f} "
              f"{g / k30:11.5f} {math.erfc(abs(z) / math.sqrt(2)):7.3f}")
    # the continued fraction against Gauss–Kuzmin
    mpmath.mp.dps = 22050
    x, cf = 2 * mpmath.pi, []
    for _ in range(20000):
        a = int(mpmath.floor(x))
        cf.append(a)
        x = 1 / (x - a)
    qs = cf[1:]
    mpmath.mp.dps = 30
    bins = [(1, 1), (2, 2), (3, 3), (4, 4), (5, 5), (6, 10), (11, 20), (21, 100), (101, None)]

    def gk(k):
        return -math.log2(1 - 1 / (k + 1) ** 2)

    obs = [sum(1 for a in qs if lo <= a and (hi is None or a <= hi)) for lo, hi in bins]
    exp = [len(qs) * (sum(gk(k) for k in range(lo, hi + 1)) if hi else math.log2(1 + 1 / lo)) for lo, hi in bins]
    st = sum((o - e) ** 2 / e for o, e in zip(obs, exp))
    sp = primes(200000)
    print(f"continued fraction, {len(qs)} partial quotients: Gauss–Kuzmin p = {chi2_p(st, len(bins) - 1):.3f};  Khinchin mean "
          f"{math.exp(sum(math.log(a) for a in qs) / len(qs)):.4f} (2.6854);  prime share {sum(1 for a in qs if a < 200000 and sp[a]) / len(qs):.4f} "
          f"({sum(gk(k) for k in range(2, 200000) if sp[k]):.4f});  quotients ≥ 1000: {sum(1 for a in qs if a >= 1000)} "
          f"({len(qs) * math.log2(1 + 1 / 1000):.1f})")
    # base e: the greedy expansion, digits 0, 1, 2
    mpmath.mp.dps = 3000
    E = mpmath.e

    def beta(x, m):
        out = []
        for _ in range(m):
            x *= E
            d = int(mpmath.floor(x))
            out.append(d)
            x -= d
        return out

    dt = beta(2 * mpmath.pi - 2 * E, 5000)
    pool = [0, 0, 0]
    random.seed(11)
    for _ in range(40):
        xs = beta(mpmath.rand(), 5000)
        for k in range(3):
            pool[k] += xs.count(k)
    ct = [dt.count(k) for k in range(3)]
    mpmath.mp.dps = 30
    print(f"base e: τ = (20.{''.join(map(str, dt[:30]))}…)_e;  digits 0, 1, 2 at {ct[0] / 5000:.3f}, {ct[1] / 5000:.3f}, {ct[2] / 5000:.3f};  "
          f"random reals {pool[0] / sum(pool):.3f}, {pool[1] / sum(pool):.3f}, {pool[2] / sum(pool):.3f};  p = {chi2_homog(ct, pool):.3f}")


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else "products"
    if cmd == "products":
        products()
    elif cmd == "readout":
        readout(sys.argv[2] if len(sys.argv) > 2 else "50,100,200,400")
    elif cmd == "arms":
        arms()
    elif cmd == "digits":
        digits()
    else:
        sys.exit(__doc__)
