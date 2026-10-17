# The prime race mod 12 and mod 4 up to 1e8: exact counts at 1201 log-spaced x (writes race12.json; Assets/Resources/race12.txt is made from it).
import math, time, json
from math import isqrt
t0 = time.time()
N = 100_000_000
sv = bytearray([1]) * (N + 1); sv[0] = sv[1] = 0
for i in range(2, isqrt(N) + 1):
    if sv[i]: sv[i*i::i] = bytes(len(range(i*i, N + 1, i)))
print("sieve", round(time.time() - t0, 1), "s")
c = {1: 0, 5: 0, 7: 0, 11: 0}
first = {}
leads = {5: 0, 7: 0, 11: 0}      # number of primes at which class 1 is strictly ahead of class a
changes = {5: 0, 7: 0, 11: 0}
ahead = {5: False, 7: False, 11: False}
c4 = {1: 0, 3: 1}; first4 = None  # 3 is the first prime = 3 mod 4
# log-spaced checkpoints for the chart
checks = sorted(set(int(round(10 ** (3 + 5 * i / 1200))) for i in range(1201)))
ci = 0; rows = []
pos = 5; total = 2  # 2 and 3
while True:
    p = sv.find(1, pos)
    if p < 0: break
    while ci < len(checks) and checks[ci] < p:
        x = checks[ci]; ci += 1
        rows.append((x, c[1], c[5], c[7], c[11], c4[1], c4[3]))
    r = p % 12
    c[r] += 1
    c4[p % 4] += 1
    if first4 is None and c4[1] > c4[3]: first4 = p
    for a in (5, 7, 11):
        now = c[1] > c[a]
        if now:
            leads[a] += 1
            if a not in first: first[a] = p
        if now != ahead[a]:
            changes[a] += 1; ahead[a] = now
    pos = p + 1
while ci < len(checks):
    x = checks[ci]; ci += 1
    rows.append((x, c[1], c[5], c[7], c[11], c4[1], c4[3]))
print("loop", round(time.time() - t0, 1), "s")
print("counts to 1e8:", c, "  mod 4:", c4)
print("first x where class 1 leads class a:", first, "  mod-4 race first lead of class 1:", first4)
print("primes at which class 1 is ahead:", leads, " sign changes:", changes)
json.dump(rows, open("race12.json", "w"))
