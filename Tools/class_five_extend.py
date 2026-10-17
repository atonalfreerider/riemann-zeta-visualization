#!/usr/bin/env python3
"""Extend the three completed zeta functions of discriminant -47 (Assets/Tour/ClassFiveData.cs, t ≤ 50) to t = 100, for
Tools/family_sphere_data.py (sections NC–ND of docs/experiments/README.md).

    python3 Tools/class_five_extend.py line OUT.json
        the critical line t = 50.02 … 100 every 0.02, the counting box's right edge Re s = 2.2 (t = 50.1 … 100 every 0.1) and
        its top edge t = 100 (Re s = 2.2 … 0.55, 170 points): about 40 minutes on 8 cores (Chowla–Selberg slows with t)
    python3 Tools/class_five_extend.py window T0 T1 STEP OUT.json
        the line resampled from T0 to T1 every STEP, where two zeros fall within one 0.02 step: up to t = 100 a zero of ζ
        (84.7355) and one of L(s, χ₋₄₇) (84.7260) do, so  window 84.70 84.78 0.001  (a minute)
Each value is [re, im] for Λ1, Λ2, Λ3 (the forms x² + xy + 12y², 2x² + xy + 6y², 3x² + xy + 4y²).
"""
import json, os, sys, time
from multiprocessing import Pool

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from class_group_data import lam

D, FORMS = -47, [(1, 1, 12), (2, 1, 6), (3, 1, 4)]


def job(z):
    return [[v.real, v.imag] for v in lam((D, FORMS, z))]


def evaluate(pts):
    t0 = time.time()
    res = [None] * len(pts)
    with Pool(8) as pool:
        # the largest t first, so the slowest points do not trail at the end
        order = sorted(range(len(pts)), key=lambda i: -pts[i].imag)
        for done, (i, v) in enumerate(zip(order, pool.imap(job, [pts[i] for i in order], chunksize=4))):
            res[i] = v
            if done % 250 == 0:
                print(f"{done}/{len(pts)} after {time.time() - t0:.0f} s", flush=True)
    print(f"done in {time.time() - t0:.0f} s", flush=True)
    return res


if __name__ == "__main__":
    if sys.argv[1] == "line":
        line = [complex(0.5, 50.02 + 0.02 * k) for k in range(2500)]
        right = [complex(2.2, 50.0 + 0.1 * k) for k in range(1, 501)]
        top = [complex(2.2 - (2.2 - 0.55) * k / 170, 100.0) for k in range(170)]
        res = evaluate(line + right + top)
        json.dump({"line": res[:2500], "right": res[2500:3000], "top": res[3000:]}, open(sys.argv[2], "w"))
    elif sys.argv[1] == "window":
        a, b, step = (float(x) for x in sys.argv[2:5])
        ts = [a + step * k for k in range(round((b - a) / step) + 1)]
        json.dump({"t": ts, "vals": evaluate([complex(0.5, t) for t in ts])}, open(sys.argv[5], "w"))
    else:
        sys.exit(__doc__)
