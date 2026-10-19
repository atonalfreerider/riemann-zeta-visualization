#!/usr/bin/env python3
"""Can the marble fingerprints be read without symbols? A legibility study of the games' marble language
(Assets/Games/MarbleFingerprint.cs) by TypeSafe's System One model Jev (https://docs.typesafe.ai).

Each picture is one number's fingerprint with no digits at all: a badge marble, then round gold containers left to
right, each holding as many marbles as its prime in a symmetric spiral (a pair, a triangle, a pentagon, a centre and
arms), and over a container's top right a pale figure, the shadow of a k-dimensional cube (a square, a cube, a
tesseract …) with k small pale marbles evenly round it: how many times that prime repeats (its exponent).
For each picture Jev gets one Choice question: which product does it show? The
options are the true factorization and three near misses made in code (an exponent one off, a prime swapped for its
neighbour, a factor dropped or added), shuffled. Code keeps the score: how often the top choice is right, and how sure.
This is the "select instead of generate" pattern: the candidates are exact, the model only has to see which one fits.

Run from the repository root, with the key in the environment (never on the command line):
    TYPESAFE_API_KEY=… python3 Tools/jev_marbles.py DIR [N1,N2,…]     # DIR holds fp-N.png (GamesTest.Fingerprints)
JEV_REPORT=out.json keeps every answer.
"""
import json, os, random, sys, uuid
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from jev_check import ask, prepare

PRIMES = [p for p in range(2, 200) if all(p % d for d in range(2, int(p ** .5) + 1))]
SUP = "⁰¹²³⁴⁵⁶⁷⁸⁹"


def factor(n):
    f, p = [], 2
    while p * p <= n:
        k = 0
        while n % p == 0:
            n //= p
            k += 1
        if k:
            f.append((p, k))
        p += 1
    if n > 1:
        f.append((n, 1))
    return f


def show(f):
    return " × ".join(f"{p}" + ("".join(SUP[int(c)] for c in str(k)) if k > 1 else "") for p, k in f)


def near_misses(f, rng):
    """Three factorizations close to f, none equal to it."""
    out = []
    seen = {tuple(f)}

    def add(g):
        g = tuple(sorted((p, k) for p, k in g if k > 0))
        if g and g not in seen:
            seen.add(g)
            out.append(list(g))

    tries = 0
    while len(out) < 3 and tries < 200:
        tries += 1
        g = [list(x) for x in f]
        kind = rng.randrange(4)
        i = rng.randrange(len(g))
        if kind == 0:                      # an exponent one off
            g[i][1] += rng.choice([-1, 1]) if g[i][1] > 1 else 1
        elif kind == 1:                    # a prime swapped for a neighbour
            j = PRIMES.index(g[i][0]) if g[i][0] in PRIMES else 0
            q = PRIMES[max(0, min(len(PRIMES) - 1, j + rng.choice([-1, 1])))]
            if any(p == q for p, _ in g):
                continue
            g[i][0] = q
        elif kind == 2 and len(g) > 1:     # a factor dropped
            g.pop(i)
        else:                              # a factor added
            q = rng.choice([2, 3, 5, 7])
            if any(p == q for p, _ in g):
                for x in g:
                    if x[0] == q:
                        x[1] += 1
            else:
                g.append([q, 1])
        add([tuple(x) for x in g])
    return out


INSTRUCTIONS = [
    "`picture` shows one whole number written with marbles instead of digits. Read it left to right.",
    "The large marble at the far left is only a badge for the whole number; ignore it when counting.",
    "Then come round gold containers (thin gold rings) along a thin string. Each ring holds gold marbles in a symmetric "
    "pattern: a pair, a triangle, a ring, or one marble in the middle with equal arms spiralling out round it. Count every "
    "gold marble inside the ring. That count is a prime factor.",
    "Some containers have a small pale figure just above their top right, joined to the ring by a short line: an "
    "exponent. It is the outline of a square, a cube, or a higher-dimensional cube, with small pale marbles evenly round "
    "it. The number of small pale marbles says how many times that container's prime is multiplied in (two round a "
    "square: squared; three round a cube: cubed; four round a tesseract: to the fourth; and so on). A container with no "
    "figure counts once.",
    "Which product of prime powers does `picture` show?",
]


def main():
    args = sys.argv[1:]
    if not args:
        raise SystemExit(__doc__)
    folder = args[0]
    numbers = [int(x) for x in args[1].split(",")] if len(args) > 1 else sorted(
        int(f[3:-4]) for f in os.listdir(folder) if f.startswith("fp-") and f.endswith(".png") and f[3:-4].isdigit())
    rng = random.Random(7)
    report, right = [], 0
    confident_right = confident = 0
    for n in numbers:
        f = factor(n)
        options = [f] + near_misses(f, rng)
        rng.shuffle(options)
        names = ["A", "B", "C", "D"][:len(options)]
        truth = names[options.index(f)]
        ref = "fp-" + uuid.uuid4().hex
        q = {"product": {"type": "choice", "instructions": INSTRUCTIONS,
                         "criteria": {name: show(o) for name, o in zip(names, options)}}}
        res = ask({"picture": ref}, q, {ref: {"type": "image", "format": "jpeg", "data_base64": prepare(os.path.join(folder, f"fp-{n}.png"))}})
        a = res["answers"]["product"]
        ok = a["choice"] == truth
        right += ok
        if a["confidence"] >= .6:
            confident += 1
            confident_right += ok
        row = {"n": n, "truth": show(f), "chosen": show(options[names.index(a["choice"])]), "right": ok,
               "p_truth": round(a["probabilities"][truth], 3), "confidence": round(a["confidence"], 3),
               "options": {name: show(o) for name, o in zip(names, options)}}
        report.append(row)
        print(f"{n:>5} = {row['truth']:<16} {'right' if ok else 'WRONG'}  chose {row['chosen']:<16} p(truth) {row['p_truth']:.2f}  confidence {row['confidence']:.2f}", flush=True)
    print(f"\n{right} of {len(numbers)} read right ({right / len(numbers):.0%});  confident (≥ 0.6) on {confident}, of which {confident_right} right")
    json.dump(report, open(os.environ.get("JEV_REPORT", os.devnull), "w"), indent=1, ensure_ascii=False)


if __name__ == "__main__":
    main()
