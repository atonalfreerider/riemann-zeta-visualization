# Long-form segments: scripts

The horizontal long-form segments (1920 × 1080): the five discoveries (xs1 … xs5) and the zeros inside E8 (xz1 … xz5). This file is written by `python3 Tools/video_series.py --doc` from [`Tools/video_series_spec.py`](../../Tools/video_series_spec.py), which also writes the chapters into [`experiments.json`](../../Assets/Resources/Narration/experiments.json). Narrate a segment with `Tools/generate_narration.py --script experiments --only xs1`, re-time its cues to the narration with `Tools/video_series.py`, and record it with `Tools/render_chapters.py --script experiments --chapters xs1 --shapes horizontal`.

## xs1: The saddle is a wave

*discovery 1 of 5:  two master waves on the Sacks spiral*  ·  23 lines, 1076 words

1. Claude, an AI math research assistant, and I set out to find the geometry hidden in the counting numbers. This is where it leads: the Sacks spiral on its saddle, carrying the master wave of zeta's zeros, which spikes at every prime, and the curve of zeta rising out of the saddle's centre, touching its axis at every zero. Here is how it is built.
   *Board:* **where this leads** — ζ's master wave on the Sacks spiral  ·  ζ's curve rising from the saddle's centre
2. We begin with the plainest picture: the counting numbers on a line, one dot for each. A prime can be divided only by one and itself. 2, 3, 5 and 7 are prime, and so are 11 and 13, in gold. 4 = 2 · 2, 6 = 2 · 3, 9 = 3 · 3: those are not.
   *Board:* **the counting numbers** — a prime can be divided only by 1 and itself
   *Callouts:* 2; 3; 5; 7; 11; 13; 4 = 2·2; 6 = 2·3; 9 = 3·3
3. Over this line the zeros of zeta draw a wave. The zeros lie on the critical line, real part one half, and the first is at height 14.13. That height becomes the frequency of a cosine: −cos(14.13 · log x). Its crests tick on the logarithm: at 7.4, 11.5, 18 and 28, each about one and a half times the last.
   *Board:* **each zero, one wave** — the zero ½ + 14.13i:  its height is the frequency of −cos(14.13 · log x)
   *Callouts:* 7.4; 11.5; 18.0; 28.1
4. The second zero, at 21.02, adds its own cosine, and the third, at 25.01, another. Each zero's height is the frequency of one wave, and the waves add up.
   *Board:* **three zeros, three waves** — −cos(14.13 · log x) − cos(21.02 · log x) − cos(25.01 · log x)
5. Add the waves of more zeros: 10, 100, 1,000, 10,000. They agree at the primes and cancel between them, and the sum sharpens into spikes, one at every prime and at every power of a prime.
   *Board:* **adding zeros** — −Σ cos(γ · log x) over the zeros ½ + iγ:  they agree at the primes
6. 7 is a prime, and it has a spike. 8 = 2³, a power of 2, and it has one too; so does 9 = 3². But 10 = 2 · 5 has no spike, and neither does 12 = 2² · 3.
   *Board:* **spikes at the primes and prime powers** — and nowhere else
   *Callouts:* 7:  prime; 8 = 2³; 9 = 3²; 10 = 2·5:  no spike; 12 = 2²·3:  no spike
7. This is the cosine wave of the home view, drawn the way a mathematician would draw it, along the number line. It is the explicit formula of Riemann and von Mangoldt: added up, the zeros spike exactly at the prime powers.
   *Board:* **the prime master wave** — 10 000 zeros:  the explicit formula, on the number line
8. Now curl the number line up, one full turn for each step of the square root. Watch the graph curl with it: each number keeps its place along the line, and 1, 4, 9, 16, 25, the squares, come to rest on a single ray. This is the Sacks spiral.
   *Board:* **curling the line** — one turn for each step of √n:  the Sacks spiral
   *Callouts:* 16 = 4²; 25 = 5²
9. The square root is n<sup>½</sup>: the same ½ as zeta's critical line. On that line every zero's wave swells like the square root of n, the radius of this very spiral, while every term of zeta shrinks like one over it. The spiral and the critical line are mirror images through the power ½, which is why they reflect the same distribution of primes.
   *Board:* **one half, twice** — the spiral's radius √n = n^½   ·   ζ's critical line:  Re s = ½
10. Now stand the graph up out of the plane. On the spiral it becomes the curtain of the home view: a spike on every prime and every prime power; 97, a prime, and 64 = 2<sup>6</sup>.
   *Board:* **the master wave on the Sacks spiral** — the home view's curtain
   *Callouts:* 97:  prime; 64 = 2^6
11. One zero's wave on its own looks very different here. Take the 100th zero alone. Its crests still tick on the logarithm, but the spiral turns on the square root, so they drift from turn to turn and wind into arms.
   *Board:* **one zero's wave on the spiral** — the 100th zero:  −cos(236.5 · log n)
12. Go back to the first zero's wave, and uncurl the spiral into the number line again: the very first graph comes back.
   *Board:* **uncurling** — the first zero's wave, back on the number line
13. Now count divisors instead of primes. Voronoi found in 1904 that the count of divisors has waves of its own. His first wave is a sine of 4π√x, so it ticks on the square root, and its crests spread apart only slowly: 8.6, 11.8, 15.5.
   *Board:* **Voronoi's first wave** — sin(4π√x − π/4):  it ticks on √x
   *Callouts:* 8.6; 11.8; 15.5
14. His wave number m ticks on √(mx), and its strength is m's own count of divisors. 2 has two divisors, 1 and 2; 3 has two; 4 has three, 1, 2 and 4. Each enters his formula as one more wave.
   *Board:* **Voronoi's waves** — the wave of m:  d(m) · m^−¼ · sin(4π√(mx) − π/4),  d(m) its divisors
15. Add more of his waves, 10, 100, 4,000, and they too sharpen into spikes. But now there is a spike at every whole number, and each one stands as tall as its number's count of divisors.
   *Board:* **Voronoi's divisor wave** — a spike at every n, as tall as its count of divisors
16. 7, a prime, has two divisors, 1 and 7, so its spike is short. 12 has six: 1, 2, 3, 4, 6 and 12. 16 = 2<sup>4</sup> has five.
   *Board:* **the spikes count divisors** — 7:  2   ·   12:  6   ·   16:  5
   *Callouts:* 7:  2 divisors; 12:  6 divisors; 16 = 2^4:  5 divisors
17. The count comes from the prime factors: add one to each exponent, and multiply. 12 = 2² · 3, so 3 · 2 = 6. 30 = 2 · 3 · 5, 2 · 2 · 2 = 8. And 36 = 2² · 3² has 3 · 3 = 9.
   *Board:* **count the divisors from the factors** — n = p^a q^b …  has  (a + 1)(b + 1) …  divisors
   *Callouts:* 12 = 2²·3:  3·2 = 6; 30 = 2·3·5:  2·2·2 = 8; 36 = 2²·3²:  3·3 = 9
18. Take his sum back down to its first wave, and curl the line up into the spiral once more.
   *Board:* **curling Voronoi's first wave** — the same Sacks spiral
19. On the spiral his first wave stands still: two crests on every turn, at the same two angles turn after turn, 22.5° before the ray of the squares, just short of 64, and before the ray of the pronic numbers, just short of 72.
   *Board:* **Voronoi's first wave on the spiral** — two crests per turn, 22.5° before the squares and the pronic numbers
   *Callouts:* 64 = 8²; 72 = 8·9
20. Turn it into a zero's wave on the same spiral, and its crests start to drift into arms, because the zeros tick on the logarithm. Turn it back: Voronoi's waves tick on the square root, the spiral's own clock, so each one stands still.
   *Board:* **two clocks** — ζ's zeros tick on log n  ·  Voronoi's waves tick on √n
21. Bend the plane into the saddle that the Sacks spiral is drawn on. It falls twice and rises twice on every turn, its valleys along the rays of the squares and the pronic numbers, where primes are scarcest. Voronoi's first wave has the same shape, turned over and turned by 22.5°: the −π/4 in his formula moves every crest by a sixteenth of a turn.
   *Board:* **the saddle** — down twice and up twice on every turn:  valleys on the squares and pronic numbers
22. Now build both master waves on the saddle, on this one spiral. The zeros' waves light the primes; turn them into the whole numbers' own waves, and every number lights up by its count of divisors.
   *Board:* **two master waves on one saddle** — ζ's zeros light the primes  ·  the whole numbers count divisors
23. Voronoi's formula is from 1904, and the explicit formula for the primes from Riemann and von Mangoldt. What Claude and I add is the picture: the saddle under the Sacks spiral is the divisor count's first wave, turned a sixteenth of a turn, and both master waves ride the same spiral.
   *Board:* **what is new** — the saddle is Voronoi's first wave, turned 22.5°;  both master waves ride one spiral

## xs2: E8 sets the vortices

*discovery 2 of 5:  E8's clock inside zeta's path of partial sums*  ·  14 lines, 613 words

1. Discovery two. Claude and I drew zeta's series as a path of partial sums: one arrow for every counting number n, 1/√n long, each turning from the last by t · log(n/(n − 1)).
   *Board:* **the path of partial sums** — one arrow n^−½ − it for every counting number, laid end to end
2. Raise t, and the path climbs its axis, winding up as it goes; the grid below is the ground, where t is zero. At the first zero, t = 14.13, the path's end lands on zero. Higher still, at the 159th zero, 333.6, it curls into vortices, with straight spokes between them.
   *Board:* **climbing the t axis** — the first zero, t = 14.13;  the 159th, t = 333.6;  the ground is t = 0
   *Callouts:* ζ = 0
3. The spokes sit near n ≈ t/2πk: 53, 27, 18. Crossing spoke k, the path moves by the k-th term of zeta's reflected series, so every vortex belongs to a counting number.
   *Board:* **spoke k at n ≈ t / 2πk** — t = 333.6:  53,  27,  18, …
   *Callouts:* spoke 1:  n ≈ 53; spoke 2:  n ≈ 27; spoke 3:  n ≈ 18
4. Bring the plane back down to the ground, and beside it, E8's clock: its 240 roots turn by a thirtieth of a circle, and eight of the 30 hours are gold, the ones with no factor two, three or five. Keep only the arrows of numbers on those eight hours: the other arrows shrink away, and the path re-forms.
   *Board:* **E8's clock and its eight gold hours** — keep only the arrows of n with no factor 2, 3 or 5
5. Raise t to 5,000. The gold path stretches out: a tangle at the start, then long smooth runs, and at its end a coil of tiny loops.
   *Board:* **the gold path at t = 5000** — only the n on E8's eight gold hours:  25 795 terms
6. Look closely at the long run near n = 1590. These are the arrows of actual counting numbers: 1577, 1579, 1583 and on to 1603, the eight numbers here on E8's gold hours, one for each hour. All eight arrows point the same way.
   *Board:* **one run, close up** — the arrows of 1577, 1579, 1583, 1589, 1591, 1597, 1601, 1603:  one per gold hour
7. Why here? 30 numbers on, each arrow has turned by about 30t/n. Near n = 30t/(2π · 15), about 1590, that is 15 whole turns, so every 30 numbers the pattern repeats exactly: the run is block after block of the same eight arrows.
   *Board:* **the turn over thirty numbers** — t · log((n + 30)/n) ≈ 30t/n = 2π · 15  at  n ≈ 30t / 30π ≈ 1590
8. Lift one block onto E8's clock. Seen from the block, each arrow points at 15 times its own hour, and every gold hour times 15 is hour 15: all eight line up, and the block adds to −8.
   *Board:* **the block on E8's clock** — arrow n points at hour 15·n:  all eight at hour 15, the sum −8
9. Near n = 4770, each arrow points at five times its hour: four land on hour 5 and four on hour 25, and the block adds to four.
   *Board:* **near n = 4770:  k = 5** — arrow n points at hour 5·n:  hours 5 and 25, the sum 4
   *Callouts:* n ≈ 4770
10. And at the very end, near n = 23,880, each arrow points at its own hour, and the eight nearly cancel: −1. Their prime factors play no part. 23,881 is 11 times 13 times 167, but on the clock it is simply hour 1.
   *Board:* **near n = 23 880:  k = 1** — arrow n points at its own hour:  the sum −1
   *Callouts:* n ≈ 23 880
11. Let k run from 1 to 30, and the chain of the eight gold hours dances on the clock. Each time k is a whole number it lands on a trace of E8's Coxeter element: −1, 1, 2, 4, −2, −4, −8, 8.
   *Board:* **tr C^k:  E8's Coxeter element** — the chain of the eight gold hours, each turned k hours per hour
12. Measured on the path itself at t = 5,000, the charges of the spokes match these traces, to within a quarter up to spoke 12.
   *Board:* **measured on the gold path at t = 5000** — white: the charge of spoke k;  colour: tr C^k
13. Added up again, each turned by an hour, the 30 traces give back E8's sieve: for seven they reach hour 7 on the rim; for six they close up at the centre.
   *Board:* **the sieve as a spectrum** — (1/30) Σ_k tr C^k e^−2πikn/30:  1 on the gold hours, 0 off them
14. Why it matters: a symmetry of eight dimensions sets the sign and the strength of every vortex of the gold path. The identity is Ramanujan's; what Claude and I add is seeing E8's clock inside zeta's own path.
   *Board:* **what is new** — Ramanujan's sum c_30(k) = tr C^k:  E8's clock inside the path of ζ

## xs3: Where the zeros live in E8

*discovery 3 of 5:  a zero is a coil up E8's factor funnel*  ·  12 lines, 573 words

1. Discovery three. Claude and I asked where zeta's zeros live in E8's layout of the counting numbers. Here it is: every number up to 400 at its hour on E8's wheel of 30, with a column as tall as its logarithm, cut at its prime factors.
   *Board:* **E8's factor funnel** — n at its hour n mod 30, its column as tall as log n, cut at its prime factors
2. That is too much at once, so grey it out and light one number at a time. Seven is a prime: its arm points to hour 7 on the floor, and its column is one piece, log 7 tall. 6 = 2 · 3, at hour 6: a piece log 3 tall, and on top of it one log 2 tall.
   *Board:* **one number at a time** — 7:  prime, one piece log 7   ·   6 = 2·3:  log 3 + log 2
3. Now bring in the path of partial sums from discovery two, at the first zero, t = 14.13, and lay it on the funnel's floor, starting at the axis. Its arrows wind round and its tail brings it back to the axis.
   *Board:* **the path at the first zero, on the floor** — t = 14.13:  its arrows n^−½ − it close on the axis
4. Lift each arrow to the height of its own number. Arrow n rises to log n, the top of n's column; its tail moves onto the axis, and it keeps both its direction and its length, 1/√n. Arrow one stays on the floor, arrow two rises to log 2, arrow six to log 6: stacked, the shrinking arrows narrow like a horn.
   *Board:* **lift arrow n to the height log n** — it keeps its direction γ·log n and its length 1/√n:  a horn
5. Now stretch each arrow by the square root of its number: the distance of n from the centre on the Sacks spiral, and on this funnel's floor. 1/√n × √n = 1. On the critical line the length and the radius cancel, every arrow becomes one unit long, and all that is left is its direction.
   *Board:* **stretch arrow n by √n** — 1/√n × √n = 1:  the critical line's ½ cancels the spiral's radius
6. Stacked, the arrows are the hands of a clock that turns by γ for every unit of height, and a coil runs through their tips, one turn every 0.44. That coil is the zero, wound up E8's funnel.
   *Board:* **a zero is a coil** — one turn every 2π/γ = 0.44 of height
7. And the coil reads the prime factors. 6 = 2 · 3, so log 6 = log 2 + log 3, and the hand at six has turned the turn of two plus the turn of three.
   *Board:* **the hand of 6 = the hand of 2 + the hand of 3** — γ·log 6 = γ·log 2 + γ·log 3:  1.56 + 2.47 = 4.03 turns
8. Every zero is such a coil. The next two: 21.02 and 25.01, turn every hand further, the higher ones more: tighter coils.
   *Board:* **the next zeros** — γ = 21.02,  25.01:  tighter coils
9. Now add the coils of all 10,500 zeros. A ring climbs the funnel's axis, and at each height the coils add up to a single hand. Where they agree, at a prime or a power of a prime, the hand shoots out to the top of that prime's column: 7, 8, 9. At 6 and 10 they cancel, and the hand stays at the axis.
   *Board:* **adding the coils, height by height** — where they agree, a tooth of log p;  elsewhere they cancel
   *Callouts:* 7; 8 = 2³; 9 = 3²; 6:  dark; 10:  dark
10. Climbing on, the ring lights every prime power up to 400, and leaves every composite dark. The comb it draws is the master wave of discovery one, stood up along the logarithm.
   *Board:* **ζ's comb** — a tooth at every prime power:  the master wave, along log n
11. Zeta's zeros fix a prime's height but not its hour. E8's field has seven more L-functions; turned back by their characters, they split the comb into eight fans, one for each gold hour. 49 = 7² lands on hour 19.
   *Board:* **eight L-functions, eight spokes** — ζ's zeros give the height, the other seven the hour
   *Callouts:* 49 = 7²:  hour 19
12. Why it matters: the zeros are not points in E8's stack. They are frequencies of its height, and every coil reads the prime factors. The explicit formula is classical; what Claude and I add is where each zero sits in E8's funnel.
   *Board:* **what is new** — a zero is a coil up E8's funnel, turning with the logarithm of every number

## xs4: What keeps zeros on the line

*discovery 4 of 5:  symmetry is not enough*  ·  15 lines, 644 words

1. Discovery four. Discriminant −23 has three families of quadratic forms, and each family is a lattice of points. A number lies on a family when some point of its lattice sits at that number's distance squared from the centre.
   *Board:* **three families, three lattices** — x² + xy + 6y²  ·  2x² + xy + 3y²  ·  2x² − xy + 3y²
   *Callouts:* (0, 1):  0 + 0 + 6 = 6
2. 59, a prime, lies on the first family only. 13, a prime, lies on the other two. But 6 = 2·3 lies on all three: a composite shows up on more than one family.
   *Callouts:* 59:  4 points; 13; 6 = 2·3:  on all three
3. Now count the points on each ring. On the second family, two has two points, and three has two points.
   *Board:* **count the points on each ring** — 2x² + xy + 3y²:  2 has 2 points,  3 has 2 points
   *Callouts:* 2:  (±1, 0); 3:  (0, ±1)
4. On the first family, two and three have no points at all. But 6 = 2·3 has four.
   *Board:* **count the points on each ring** — x² + xy + 6y²:  2 has none,  3 has none,  6 = 2·3 has 4
   *Callouts:* 2, 3:  no points; 6:  4 points
5. So the first family's counts cannot be built from the prime factors: 0 × 0 ≠ 4. But take the first family's count minus the second's, and halve it. 2 → −1, 3 → −1, and 6 → +1 = (−1) × (−1). Building every count from its prime factors is exactly what an Euler product does: zeta's own product over the primes.
   *Board:* **counts built from the prime factors** — (first − second)/2:  6 → +1 = (−1) × (−1):  an Euler product
6. Each family has its own zeta function, counting its lattice points by distance, with Riemann's symmetry. The lattices move up to head their strips of zeros. Yet those zeros leave the critical line; one sits at real part 1.007, where no function with an Euler product can vanish.
   *Board:* **each family's own zeta function** — red: zeros off the critical line Re s = ½
   *Callouts:* 0.953 + 16.29i; 1.007 + 22.57i
7. The two combinations of the families with Euler products keep every zero on the line: 44 and 42 zeros, all gold.
   *Board:* **the class group's L-functions** — (Z_1 + 2Z_2)/2 and (Z_1 − Z_2)/2:  every zero on the line
   *Callouts:* 44 on the line; 42 on the line
8. Now the curves of the chart, built step by step. On the critical line each family's completed zeta function is a real wave: gold for the first family, blue for the second. Their zeros are where the waves cross the axis.
   *Board:* **each family on the critical line** — Λ(½ + it) is real:  gold x² + xy + 6y²,  blue 2x² ± xy + 3y²
9. Mix them: the first plus λ times the second. That mixture is zero where the first equals minus λ times the second, that is, where the ratio, minus the first over the second, equals λ. Sweep along t and plot that ratio: wherever the blue wave crosses zero it divides by zero and shoots off to infinity, so the curve breaks into these branches.
   *Board:* **Λ_1 + λΛ_2 = 0  ⟺  λ = −Λ_1/Λ_2** — the ratio of the two waves, with a pole at every zero of the blue one
10. A level line at height λ crosses this curve exactly at that mixture's zeros on the line. At zero it meets the gold dots, the first family's own zeros. At λ = 2 and λ = −1 it meets the two L-functions with Euler products, and every one of their zeros is there.
   *Board:* **a level line λ:  the zeros of Λ_1 + λΛ_2 on the line** — λ = 0:  the first family  ·  λ = 2 and −1:  the L-functions
11. Sweep λ through every mixture. Each mixture has a set number of zeros up to height 50; where the level line crosses the curve fewer times, the rest have left the line. Only two small windows of mixtures keep every zero on it, and both surround the L-functions.
   *Board:* **no zero off the line:  two small windows** — λ from 1.99 to 2,  and from −1.02 to −0.88
   *Callouts:* λ = 2; λ = −1
12. With five families, for −47, there are two weights instead of one, and the mixtures fill a plane. Build it row by row: each row is a sweep like the one we just made, every cell coloured by how many zeros leave the line, gold where none do.
   *Board:* **five families:  a plane of mixtures** — Λ_1 + λ_2Λ_2 + λ_3Λ_3:  every row a sweep
13. Now raise every mixture by its number of zeros off the line, and the plane becomes a landscape. The gold patches are its valleys, at sea level. Slice through it, and each slice is a sweep like −23's: where it dips to the floor, a window.
   *Board:* **the landscape of mixtures** — height:  the zeros off the line up to t = 50;  gold valleys:  none
14. Three valleys surround the class group's three L-functions, and their counts, like −23's, are built from their prime factors: Euler products. One more valley has no L-function in it.
   *Board:* **the valleys** — round the three L-functions:  Euler products,  counts built from primes
   *Callouts:* ζ(s) L(s, χ<sub>−47</sub>); L<sub>1</sub>; L<sub>2</sub>; no L-function here
15. Why it matters: symmetry alone does not keep zeros on the line. Davenport and Heilbronn proved that in 1936. As far as anyone has checked, the functions that keep them there are the ones whose counts are built from their prime factors: the Euler products. What Claude and I add is seeing every mixture at once.
   *Board:* **what is new** — symmetry is not enough:  every mixture seen at once

## xs5: Chebyshev's bias is a fold

*discovery 5 of 5:  why two of E8's spokes run behind*  ·  13 lines, 533 words

1. Discovery five. Every prime above five lies on one of E8's eight gold spokes. Race them up to 10 million, and two of the spokes, 1 and 19, run behind nearly all the way.
   *Board:* **the prime race on E8's eight spokes** — the primes up to 10^7, counted and rebuilt from the zeros
   *Callouts:* spokes 1 and 19:  about −3; the other six:  about +1
2. They are ahead of their share only 0.3 and 1.4% of the way, and never in the lead. Below, 6,771 zeros of E8's L-functions rebuild every curve.
   *Board:* **spokes 1 and 19 run behind** — ahead of their share 0.3 % and 1.4 % of the way;  rebuilt from 6 771 zeros
3. Why 1 and 19? Square any gold hour, and see where it lands on E8's clock. 1² = 1. 7² = 49, hour 19. 11² = 121, hour 1; 13² = 169, hour 19. All eight gold hours, squared, land on 1 or 19: the only two square hours on the clock.
   *Board:* **squares on E8's clock** — 1² = 1,  7² = 49 → 19,  11² = 121 → 1,  13² = 169 → 19, …:  only hours 1 and 19
4. Follow one prime's powers round the clock, the radius growing with the logarithm. 7: hour 7. 7² = 49:  hour 19. 7³ = 343:  hour 13. 7<sup>4</sup> = 2401: back on hour 1. Every even power lands on 1 or 19.
   *Board:* **the powers of 7** — 7 → 49 → 343 → 2401:  hours 7, 19, 13, 1
5. Now every prime at once: an arc from each prime to its square. The zeros count prime powers, not only primes, and squaring folds every spoke onto 1 or 19.
   *Board:* **the reason:  a fold** — squaring sends spokes 1, 11, 19, 29 to 1, and 7, 13, 17, 23 to 19
6. 7² = 49, on spoke 19. 11² = 121 lands on 1. 13² = 169, on 19. 29² = 841, on 1.
   *Callouts:* 7² = 49:  spoke 19; 11² = 121:  spoke 1; 13² = 169:  spoke 19; 29² = 841:  spoke 1
7. Every square of a prime above five lands on spoke 1 or 19: all 443 of them up to 10 million. A count of primes leaves out half a prime for each. Put them back, and watch the race even out.
   *Board:* **put the squares back:  a fair race** — 218 squares on spoke 1,  225 on spoke 19,  none elsewhere
8. The same fold turns up in a second race. For −23, a prime's family is decided by how many roots x³ − x − 1 has among the numbers modulo that prime. Take five: five numbers, zero to four. Put each one in. Only two gives a multiple of five, 8 − 2 − 1: one root.
   *Board:* **x³ − x − 1 modulo a prime** — 5:  0, 1, 2, 3, 4  →  only x = 2 gives 8 − 2 − 1 = 5 ≡ 0
9. Two has two numbers, zero and one, and neither is a root. 59 has 59, and exactly three of them are roots: 4, 13 and 42.
   *Board:* **count the roots** — 2:  none   ·   5:  one   ·   59:  three (4, 13, 42)
10. Those counts tell how the prime moves the three roots of x³ − x − 1. Three roots fixed: the identity, nothing moves. No root fixed: a rotation, all three move round. One root fixed: a reflection, the mirror: one root stays and the other two swap.
   *Board:* **how a prime moves the roots** — 3 roots: identity  ·  none: rotation  ·  1 root: reflection
11. And a reflection done twice is the identity: swap, then swap back. So the square of every prime with one root, like 25 = 5², acts like a prime with three roots, and lands in the principal family.
   *Board:* **a reflection, twice:  the identity** — 5 has one root;  25 = 5² lands in the principal family x² + xy + 6y²
12. Half of all primes have one root, so their squares fall in the principal family, which runs behind its share and is never in the lead.
   *Board:* **the squares of the reflections** — land in the principal family:  it runs behind
13. Put those squares back too, and this race evens out as well. Why it matters: one fold explains both races. Chebyshev noticed the bias in 1853; what Claude and I add is one picture for E8's wheel and for the families of −23.
   *Board:* **what is new** — one fold:  E8's wheel and the families of −23

## xz1: The path of partial sums

*the zeros inside E8, part 1:  how ζ is built, link by link*  ·  9 lines, 377 words

1. Claude, an AI math research assistant, and I set out to find where the zeros of zeta live among the counting numbers. First, a refresher on how zeta is built: one arrow for every counting number, laid end to end. Mathematicians call this the path of partial sums.
   *Board:* **the path of partial sums** — ζ(s) = 1 + 1/2^s + 1/3^s + …,  one arrow per term
2. Take the height t = 1. The first arrow is one unit long and points to the right. The second is 1/√2 long, and it turns away from the first by log 2: 40°.
3. The third is 1/√3 long, and turns a further log(3/2), 23°. Its direction is the sum of the turns: log 3 = 63° from the first. Every direction is a logarithm.
4. Four, five, six, seven, eight: shorter and shorter arrows, turning less and less, 17°, 13°, 10°, 9° and 8°, because the logarithm climbs more and more slowly.
5. Now raise the height t. Every angle is t times a logarithm, so the path winds up tighter. The arrows go on forever, but past a point the rest add up to a simple tail, drawn in orange, and the end of that tail is ζ(½ + it).
   *Board:* **raising t** — every angle is t × a logarithm;  the tail adds all the arrows beyond
6. At t = 14.13, the end lands exactly on zero. That is the first zero of zeta: all the arrows, from one to infinity, cancel out.
   *Board:* **the first zero** — t = 14.1347:  ζ(½ + it) = 0
   *Callouts:* ζ(½ + 14.13i) = 0
7. Now let t run on, and stand the plane up on a t axis, so that it climbs as t grows. The end of the path leaves a trail, the curve of zeta along the critical line. It touches the axis at every zero: 21.02, 25.01, 30.42.
   *Board:* **the curve of ζ on the critical line** — the plane climbs the t axis;  the curve touches it at every zero
8. Between the zeros, the curve swings out and comes back. Riemann's hypothesis says that every zero of this kind lies on this line, Re s = ½. Trillions have been checked, and every one does, but no one has proved that all of them must.
   *Board:* **Riemann's hypothesis** — every nontrivial zero of ζ has real part ½
9. Higher up, the path needs more and more arrows before the tail takes over, and it begins to curl into knots. Those knots are its vortices, and the next part follows them up the axis.
   *Board:* **the vortices begin** — knots in the path, ringed

## xz2: How the vortices develop

*the zeros inside E8, part 2:  the path of partial sums high up*  ·  9 lines, 340 words

1. Climbing higher, the path needs about t over pi arrows before its tail takes over, and it curls into knots: vortices. The rings mark their eyes, numbered from the far end of the path.
   *Board:* **vortices** — about t/2π of them at height t
2. Each time t over two pi passes another whole number, a new vortex is born near the start of the path, and the older ones move out along it.
   *Board:* **a vortex is born** — each time t/2π passes a whole number
3. Between the vortices the path runs straight. Those straight runs are the spokes, where each arrow turns by a whole number of full turns from the last. They sit near n ≈ t/2πk: at the 159th zero, 53, 27 and 18.
   *Board:* **spoke k at n ≈ t / 2πk** — where each arrow turns a whole number of turns from the last
   *Callouts:* spoke 1:  n ≈ 53; spoke 2:  n ≈ 27; spoke 3:  n ≈ 18
4. Each vortex has an eye, and each eye is a mirror image: the path's own k-th vertex, reflected through zeta's phase. The crosses mark those mirror points, and they land inside the rings.
   *Board:* **every eye is a mirror** — eye of vortex k = the path's k-th vertex, reflected through ζ's phase
5. Now let the plane climb on, and at every zero keep each eye where it was. The eyes build a tower; vortex one, in cyan, always sits on a circle of radius one.
   *Board:* **the eyes through the zeros** — kept at every zero:  vortex 1 on a circle of radius 1
6. Look straight down the axis. The eyes of 10,000 zeros, stacked: the unit circle of vortex one, and rings for all the others.
   *Board:* **all 10 000 zeros, down the axis** — the vortices belong to the counting numbers
7. Look closer: the eyes thin out beyond a faint circle at radius 1.71, and a brighter ring glows at 0.29. At a zero, vortex two's eye is as far from the centre as the path's second vertex: one, plus the arrow of two, 1/√2 long.
   *Board:* **a faint circle at r = 1.71** — vortex 2's eye:  |1 + 2^−½ − iγ|,  one plus an arrow 1/√2 long
8. That arrow can point any way, so the eye stays between 1 − 1/√2 and 1 + 1/√2, and it lingers at those two extremes, so both circles glow. Beyond 1.71 only vortex three and higher can reach, and the eyes thin out.
   *Board:* **between 1 − 1/√2 = 0.29 and 1 + 1/√2 = 1.71** — beyond 1.71 only vortices 3 and up:  the eyes thin out 2.7-fold
9. So the vortices develop as t climbs. New ones are born at the start, the spokes slide outward, and every vortex belongs to a counting number: a reflection of the path's own beginning.
   *Board:* **how the vortices develop** — one per counting number, each a mirror of the path's start

## xz3: From a zero to E8's heights

*the zeros inside E8, part 3:  the turn of n is γ times log n*  ·  9 lines, 381 words

1. This is E8's factor funnel. Every counting number sits at its hour on the wheel of 30, with a column as tall as its logarithm. 7, at hour 7: log 7 = 1.95. 30 = 2 · 3 · 5, at hour 0:  log 30 = 3.40.
   *Board:* **E8's factor funnel** — n at its hour n mod 30, its column as tall as log n
   *Callouts:* log 7 = 1.95; 30 = 2·3·5:  log 30 = 3.40
2. Grey it out, and light the powers of two. Two is one piece, log 2 tall, on the arm of hour 2. 4 is 2 pieces, 8 is 3, 16 is 4: each column is its prime factors' logarithms, stacked.
   *Board:* **a column is its prime factors, stacked** — log 16 = log 2 + log 2 + log 2 + log 2
3. Now the path of partial sums at the first zero, t = 14.13, on the funnel's floor. Arrow one, arrow two, arrow three, and on, each 1/√n long, each turned by t · log n.
   *Board:* **the path at the first zero** — arrow n:  1/√n long, turned 14.13 · log n
4. Its tail closes it on the axis: that is what a zero is. Now lift every arrow to the height of its own number, keeping its direction and its length: arrow two to log 2, arrow three to log 3, arrow four to log 4, twice as high as two. The arrows shrink as they climb, like a horn.
   *Board:* **lift arrow n to the height log n** — keeping its direction −14.13 · log n and its length 1/√n
5. Stretch each arrow by the square root of its number, its distance from the centre on the Sacks spiral and on this floor. 1/√n × √n = 1: every arrow becomes one unit long, and only its direction is left. Through their tips runs a coil that turns by γ for every unit of height: the zero, wound up E8's funnel.
   *Board:* **stretch arrow n by √n** — 1/√n × √n = 1:  every arrow one unit long, only its direction left
6. The coil multiplies by adding heights. 4 = 2 · 2, so its hand has turned twice the turn of two. 6 = 2 · 3: the turn of two plus the turn of three.
   *Board:* **multiplying is adding heights** — γ·log 4 = 2 γ·log 2   ·   γ·log 6 = γ·log 2 + γ·log 3
7. Every zero winds its own coil. 21.02 turns each hand further, the higher ones more; 25.01, further still.
   *Board:* **other zeros, other coils** — γ = 21.02,  25.01
8. At a height that is not a zero, like 18, the hands turn just the same, but drop them back onto the floor and the path does not close: its tail ends away from the axis.
   *Board:* **t = 18:  not a zero** — the arrows dropped back into the path:  it ends away from the axis
9. That is the bridge between a zero and E8's layout: the zero's height, times the height of n's column, is the turn of n's arrow. Each prime factor adds its own turn.
   *Board:* **the bridge** — the turn of n at a zero = γ × log n = γ × (the sum of log p over its prime factors)

## xz4: Lamps and spokes

*the zeros inside E8, part 4:  all the coils together*  ·  11 lines, 400 words

1. Every zero winds its own coil up the funnel. Here are the first three: 14.13, 21.02 and 25.01, over the greyed-out columns.
   *Board:* **every zero, a coil** — γ = 14.13,  21.02,  25.01
2. Take all 10,500 zeros, and look at one height: the top of seven's column. Each coil points somewhere there; on average they lean towards −1, by −0.115, exactly as Landau's formula predicts for a prime.
   *Board:* **at the height of 7, a prime** — 10 500 coils lean to −1:  mean −0.115,  Landau −0.115
3. At the top of six's column, 6 = 2 · 3, the coils point every way, and their mean is zero. The zeros lean only at the powers of primes.
   *Board:* **at the height of 6 = 2·3** — the coils cancel:  mean 0.000
4. Now do it at every height. A ring climbs the axis, and at each height all the coils add up to one hand, drawn out to the side. Where they agree, at a prime power, it shoots out to the top of that prime's column: 7, 8, 9. At 6, 10 and 12 it stays at the axis.
   *Board:* **all the coils, height by height** — a tooth of log p at every prime power
   *Callouts:* 7; 8 = 2³; 9 = 3²; 6:  dark; 10:  dark; 12:  dark
5. Climbing on, it lights every prime power up to 400: a comb, the master wave from the first discovery, stood up along the logarithm.
   *Board:* **ζ's comb** — a tooth at every prime power
6. But the comb is the same in every direction. Spin it round the axis, slowly, and watch each tooth sweep out a ring through its prime's column. Zeta's zeros know how high a prime sits, not its hour on the wheel.
   *Board:* **spun round the axis:  a ring per prime power** — ζ's zeros fix the height, not the hour
7. E8 knows more. Its field has eight L-functions, and at the height of seven each one's zeros light the tooth of seven, turned by its own character: along 1, −1, i or −i.
   *Board:* **eight L-functions at the height of 7** — each tooth turned by its character χ(7):  1, −1, i or −i
8. Turn each one back by its character at an hour, and chain the eight. At every other hour they cancel; only at hour 7 do all eight line up, and the chain swings onto the spoke of 7.
   *Board:* **turned back by χ(h), and chained** — the eight line up only for the hour of 7
9. Do this at every height, and the comb splits into eight fans, one for each gold hour: only the primes on spoke h survive in fan h.
   *Board:* **eight L-functions, eight fans** — only the primes on spoke h survive
10. 49 = 7² lights on spoke 19. 11 and 41 share spoke 11. 7,400 zeros sort every prime up to 400 onto its spoke.
   *Board:* **7 420 zeros up to t = 1000** — ζ's give the height, the other seven the hour
   *Callouts:* 49 = 7²:  hour 19; 11; 41 = 11 + 30
11. So the zeros are not points in the stack. They are frequencies of its height. Added up, they rebuild the prime powers, and turned by E8's characters, they put each prime on its spoke. They reconstruct the primes; they do not predict them.
   *Board:* **the zeros are frequencies of the height** — they rebuild the primes:  they do not predict them

## xz5: 240 roots, infinitely many numbers

*the zeros inside E8, part 5:  E8's clock and E8's shells*  ·  12 lines, 456 words

1. E8 has 240 roots, shown here in its Coxeter plane as eight rings of 30. So how can so few points hold infinitely many counting numbers?
   *Board:* **E8:  240 roots** — 8 rings of 30 in the Coxeter plane
2. They don't have to. The roots set E8's clock: its Coxeter element turns the plane by a thirtieth of a turn, so the wheel has 30 hours, and eight of them, E8's exponents, are gold.
   *Board:* **E8's clock:  30 hours** — the 8 gold hours:  1, 7, 11, 13, 17, 19, 23, 29
3. Drop every counting number onto the wheel at its hour, n mod 30, one turn further out each time round. The spiral never ends.
   *Board:* **every n at its hour** — n mod 30:  the hours repeat, the radius grows
4. Grey it out and follow one hour, its arm drawn from the centre. 7, 37, 67 and 97 all land on hour 7, each one turn further out: all four are primes. 127, too.
   *Board:* **one hour:  7, 37, 67, 97, 127** — n ≡ 7 (mod 30)
   *Callouts:* 7; 37 = 7 + 30; 67 = 7 + 2·30; 97 = 7 + 3·30; 127 = 7 + 4·30
5. Hour 6 holds 6, 36, 66 and 96: every one a multiple of six, off the gold spokes. The clock sorts out two, three and five; the rest it cannot see.
   *Board:* **hour 6:  6, 36, 66, 96** — multiples of 6, off the gold spokes
   *Callouts:* 6 = 2·3; 96 = 2^5·3
6. And E8 itself is not 240 points. It is an infinite lattice, and the roots are only its first shell: the points at distance √2. Tilt the wheel, and the next shells appear beneath it.
   *Board:* **E8 is a lattice:  shells** — the 240 roots are its first shell, at distance √2
7. Each shell n holds the points at distance √(2n): 2,160 for n = 2, 6,720 for 3, 17,520 for 4, and on down to 7. Every counting number has a shell of its own.
   *Board:* **a shell for every counting number** — shell n:  the points at distance √2n:  240, 2 160, 6 720, 17 520, …
8. Each shell's size is 240 times a sum of cubes, one cube for each divisor of n. 1 has the single cube 1. 2 has 1 and 8. 3 has 1 and 27.
   *Board:* **shell n holds 240 σ_3(n) points** — σ_3(n):  one cube d³ for each divisor d of n
9. 4 has 1, 8 and 64: its divisors 1, 2 and 4. 5, a prime, has only 1 and 125. 6 has four cubes: 1, 8, 27 and 216. 7, a prime, has 1 and 343.
   *Board:* **the cubes of the divisors** — 4:  1 + 8 + 64   ·   5:  1 + 125   ·   6:  1 + 8 + 27 + 216   ·   7:  1 + 343
10. So the shells are linked by divisibility: each shell sends its cube to the shells of its multiples. 1 feeds every shell. 2 feeds 4 and 6. 3 feeds 6.
   *Board:* **linked by divisibility** — shell d sends d³ to every shell n that d divides
11. That is why the shells are a useful way to look at numbers. A prime's shell holds the fewest points it can, just 1 and p³. Every extra cube is a divisor: 6's extra cubes, 8 and 27, are 2³ and 3³, so E8's geometry has factored 6.
   *Board:* **a prime's shell:  240 (1 + p³), the fewest** — 6:  the extra cubes 8 = 2³ and 27 = 3³ are its factors
12. Counted over every shell at once, the points of E8 give its own zeta function: 240 ζ(s) ζ(s − 3). A finite symmetry, and infinitely many shells, one for every counting number.
   *Board:* **Σ 240 σ_3(n) n^−s = 240 ζ(s) ζ(s − 3)** — a finite symmetry, a shell for every counting number
