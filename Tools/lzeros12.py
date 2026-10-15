# Zeros of L(s, chi) for the three characters mod 12 (chi_-4, chi_-3, chi_12) below height T: python3 Tools/lzeros12.py 500
import mpmath as mp, time, json, sys
mp.mp.dps = 15
chars = {
  "m4":  ([0, 1, 0, -1], 4, 1),                              # chi_{-4}: odd -> the square lattice
  "m3":  ([0, 1, -1], 3, 1),                                 # chi_{-3}: odd -> the triangular lattice
  "m12": ([0, 1, 0, 0, 0, -1, 0, -1, 0, 0, 0, 1], 12, 0),    # chi_12: even
}
T = float(sys.argv[1]) if len(sys.argv) > 1 else 120.0
out = {}
for name, (chi, q, a) in chars.items():
    def Z(t):
        s = mp.mpc(0.5, t)
        th = (t / 2) * mp.log(q / mp.pi) + mp.loggamma((0.5 + a + 1j * t) / 2).imag
        return (mp.exp(1j * th) * mp.dirichlet(s, chi)).real
    t0 = time.time()
    zeros = []
    h = 0.05
    t = 0.3; z0 = Z(t)
    while t < T:
        t1 = t + h; z1 = Z(t1)
        if z0 == 0: zeros.append(t)
        elif z0 * z1 < 0:
            r = mp.findroot(Z, (t, t1), solver="anderson")
            zeros.append(float(r))
        t, z0 = t1, z1
    # check a few against |L|
    worst = max(abs(mp.dirichlet(mp.mpc(0.5, g), chi)) for g in zeros[:10])
    expect = (T / (2 * mp.pi)) * mp.log(q * T / (2 * mp.pi * mp.e))
    print(f"{name}: {len(zeros)} zeros below {T} (N(T) estimate {float(expect):.1f}), first {zeros[:4]}, max |L| at first ten {float(worst):.1e}, {time.time()-t0:.0f}s", flush=True)
    out[name] = zeros
json.dump(out, open("lzeros12.json", "w"))
