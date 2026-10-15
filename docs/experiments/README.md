# Experiments on the Sacks saddle

Thought experiments drawn on the saddle-bent Sacks spiral, rendered headlessly from
[`SacksExperiments.cs`](../../Assets/Tour/SacksExperiments.cs) by
[`ExperimentsRenderTest.cs`](../../Assets/Tests/ExperimentsRenderTest.cs). Everything here lives on the
`experiments/sacks-cancellation` branch and touches nothing the tour or the VR build uses: `SacksSpiral` only
became a `partial` class so the experiments can reach its surface helpers.

Each section says what was drawn, what it shows, and what it does *not* show. The numbers were checked
independently in Python (mpmath / plain integer arithmetic) before being drawn.

## A. Cancellation curves and where they cut Euler's line

**Definition.** Every composite is a product `n = u · v` with `2 ≤ u ≤ v`. Write `v = u + d`. For a fixed gap `d`
the numbers `u(u + d)`, `u = 1, 2, 3, …` form one curve on the Sacks spiral, because the spiral puts `n` at
winding angle `2π√n` and `√(u(u + d)) ≈ u + d/2`: stepping `u → u + 1` advances the angle by almost exactly one
turn, so the points line up. These are the *cancellation curves* `C_d`:

* `C_0` is the squares (`u · u`), `C_1` the pronic numbers (`u(u + 1)`), the two routes already in the model.
* `C_d` is born at `u = 1`, i.e. at the number `d + 1` itself (small ring in the pictures), and every point after
  the birth is composite. A prime lies on exactly one cancellation curve, the one born at it.
* Even `d` curves straighten into lines `y = −π d²/4` just below the squares ray; odd `d` curves straighten into
  `y = +π d²/4` just above the pronic ray (in units of the spiral's radius constant). The depth of a composite is
  how many curves pass through it (section C).

![A1](A1-cancellation-saddle-full.jpg)
![A3](A3-cancellation-saddle-oblique.jpg)

**The crossing law for Euler's line.** The candidate line `E(k) = k² + k + 41` is prime for `k = 0 … 39`. Beyond
that it is cut by the curves, and the cuts come in two exact families (checked for every `k ≤ 120`):

| family | where | what the value is | first cases |
|---|---|---|---|
| I  | `k = 40 + j²` | `E(k) = E(j − 1) · E(j)` — two *neighbouring* values of the same line | 40 (41·41), 41 (41·43), 44 (43·47), 49 (47·53), 56, 65, 76, 89, 104 |
| II | `k = 2i² + i + 81` | `E(k) = E(i) · D(i)` with `D(j) = (2j)² + 163` the *sister line* | 81 (41·163), 84 (43·167), 91 (47·179), 102, 117 |
| II | `k = 2i² + 3i + 82` | `E(k) = E(i) · D(i + 1)` | 82 (41·167), 87 (43·179), 96 (47·199), 109 |

These 18 are *all* the composites with `k ≤ 120`; there is no third family that low. Family I sits on the
curve `C_2j`, family II on curves with `d ≥ 122`, which is why its cuts look like long straight chords coming in
from the sister line.

**Why those and nothing else.** `E(k)` is the norm of `(2k + 1 + √−163)/2` and `D(j)` is the norm of
`2j + √−163` in the ring `Z[(1 + √−163)/2]`, which has unique factorisation (class number one, the reason
Euler's polynomial works at all). A value is composite exactly when its element factors in the ring, and a
product of two elements lands back on Euler's row only when their rows add up to one: row 1 × row 1 with
conjugation (family I), or row 1 × row 2 (family II). So the "cancellation curves" that matter for a
prime-generating line are not arbitrary: they are the multiplication table of one quadratic ring, and the
sister line `D` is that ring's second row. The sister line is itself prime for `j = 0 … 19` (163, 167, 179, 199,
227, …, 1459), for the same reason the Euler line is.

![A4](A4-euler-crossings-flat-wide.jpg)
![A5](A5-euler-crossings-flat-close.jpg)

**What this does and does not give you.** Intersections with the curves *are* the composites, by definition, so
"eliminate candidates at the intersections" is the sieve itself, restated geometrically. What is new in the
picture is the bookkeeping: on a line of discriminant `−D` the intersections are indexed by the divisors of
`D + (2j)²`, so the first composites after the initial prime run are predictable without factoring anything
(family I needs no factoring at all: it is a square-indexed rule). It does not give a prime test, because
deciding whether `(2k + 1 + √−163)/2` factors is exactly as hard as factoring `E(k)`.

## A2. The zeta wave along Euler's line

The master wave of the final chapter, `−Σ cos(γ · log n)` over all 10 000 zeros, evaluated along the line
`n = k² + k + 41` instead of along the spiral. The explicit formula says this sum spikes at prime powers and dips
between them; along Euler's line it should therefore be a comb of upward spikes for `k ≤ 39` and show holes at
the family I and II crossings.

![A9](A9-zeta-wave-on-euler-close.jpg)

It does: every prime value carries a spike and every composite a trough, with no extra structure. The zeros
know nothing about `√−163`; they simply mark primes, and the line's own algebra decides where the primes stop.
The two descriptions agree point by point, which is the honest content of "the zeros encode the primes".

## C. Composite depth, and whose clock the saddle is

**Depth bars.** A bar at every composite of height proportional to the number of cancellation curves through it
(number of divisors up to `√n`, minus one). Primes are flat. The tall bars collect on the squares ray and the
pronic ray and along the straightened curves, the familiar Sacks "composite lanes".

![C2](C2-depth-bars-oblique.jpg)

**Voronoi's formula.** Summing the depth over all `n ≤ x` gives the divisor staircase `Σ d(n)`, whose smooth part
is `x log x + (2γ − 1)x` and whose wobble `Δ(x)` has its own explicit formula (Voronoi, 1904):

    Δ(x) ≈ (x^¼ / π√2) · Σ_n d(n) n^(−3/4) cos(4π√(nx) − π/4)

The waves in this formula tick with `√x`, which is exactly the Sacks clock (`2π√n` per turn), the way the prime
staircase's waves `cos(γ log x)` tick with the log clock. The first harmonic alone is `cos(4π√x − π/4)`: two
oscillations per turn of the spiral. The saddle height is `z ∝ x² − y² = r² cos(2φ) = n · cos(4π√n)`: the same
two oscillations per turn, an eighth of a turn ahead in phase and with a steeper envelope. So the saddle the
spiral is bent on is not decoration: up to that phase and envelope it is the leading wave of the divisor
function's explicit formula, the `ζ²` analogue of the master cosine wave.

![C4](C4-voronoi-oblique.jpg)
![C6](C6-voronoi-first-harmonic-side.jpg)

White: the exact `Δ(x)` for `9 ≤ x ≤ 400`. Cyan: 3 000 terms of the Voronoi sum. Orange: the first harmonic.
The second picture keeps only the first harmonic on the three outer turns, over a wire of the saddle, so the
ribbon's two oscillations per turn can be compared with the surface's own. The truncated sum tracks the sawtooth
closely but not exactly (it converges only conditionally; at `x = 400.3` it gives 7.09 against the exact 7.50),
so this is an illustration, not a proof of anything.

## D. The zeros on their own Sacks spiral

The Sacks spiral is an Archimedean spiral in the clock `√n`: whole clock values (the squares) line up on one
ray. The zeros have a clock too, `G = θ(γ)/π + 1` with `θ` the Riemann–Siegel phase; its whole values are the
Gram points. Plotting zero `j` at radius `√G` and angle `2πG` puts the Gram points on a ray, the analogue of the
squares ray.

![D1](D1-gram-spiral-full.jpg)

For the first 1 600 zeros the ray is visibly thinner than its surroundings but not empty: the fractional part of
`G` has decile counts (first 4 000 zeros) of 278, 321, 397, 467, 538, 525, 513, 384, 315, 262. Zeros avoid Gram
points about as mildly as primes avoid the neighbourhood of squares, and the 277 Gram-law exceptions among the
first 4 000 zeros are the ones that stray onto the ray. The analogy is real but weak; it does not make the Gram
ray a "cancellation curve" for zeros.

## E. Anatomy of the chain at one dial value

The partial sums `Σ n^(−½−it)` at a fixed `t`, flat, with the structure the dial produces as `n` grows:

* the turn per link is `t/n`; where it is a multiple of `2π` consecutive links align into a straight **spoke**,
  at `n = t/2πk`; where it is an odd multiple of `π` they fold into a **knot**, at `n = t/(2k + 1)π`;
* the spokes are the mirror images of the integers `k` and the knots of the half-integers under the
  Riemann–Siegel reflection `n ↔ t/2πn`; vortex `k` (between spoke `k + 1` and spoke `k`) holds `t/2πk(k + 1)`
  links, an inverse-square count, and turns through `t · log(1 + 1/k)` radians, a logarithm;
* past the last spoke (`n > t/2π`) the links turn less than a full circle each and the chain spirals outward
  around `ζ(½ + it)` itself. At a zero that centre is the origin; between zeros it is off-centre; at a Gram point
  it lies on the real axis.

![E1](E1-chain-anatomy-three.jpg)

The "inverse squares, logarithms and number of turns" are therefore not three mysteries but one: the chain's
turn per link is `t / r²` where `r = √n` is the Sacks radius of that integer (Kepler's area law with angular
momentum `t`), and integrating `t/r²` over links is what produces the logarithm `t · log n`.

## F. One zero's crests on the spiral

The wave of a single zero, `cos(γ log n)`, crests at `n_k = e^{2πk/γ}`. On the spiral the winding angle of
crest `k` is `φ_k = 2π e^{πk/γ}`: a geometric progression, i.e. the crests are *equally spaced in the logarithm
of the winding angle*, with spacing `π/γ`. Between crests the ribbon makes `γ/(π√n)` oscillations per turn, so at
`n = (γ/πm)²` the crests line up into `m` radial arms (white rings). Those radii form an inverse-square ladder in
`m`, and between rungs the arms twist into spirals, the "vortices" visible in the full interference picture.

![F3](F3-crests-zero100-full.jpg)
![F1](F1-crests-zero1-core.jpg)

So each zero draws its own set of nested spiral arms on the saddle; the master wave is their superposition.
The arms are a moiré between the spiral's `√n` clock and the zero's `log n` clock. They are real geometry of the
drawing, not of the primes: the primes appear only when thousands of these patterns are added (section G).

## G. The truncated explicit formula on the number field

Every number coloured by `−Σ_{z≤K} cos(γ_z log n)` (gold up, blue down, each disc scaled to its own extreme),
for `K` = 1, 3, 10, 100, 1 000 and 10 000 zeros on the saddle, then nine such discs stacked (flat, so the
layers read as layers).

![G 1](G-heat-00001-full.jpg)
![G 100](G-heat-00100-full.jpg)
![G 10000](G-heat-10000-full.jpg)
![G stack](G-heat-stack-flat-side.jpg)

With one zero the field is the moiré of section F; with ten it is a tangle of arms; by a hundred the gold
begins to settle on the prime dots; by ten thousand almost nothing but primes and prime powers is gold. Looking
down the stack is watching the explicit formula converge: the vortex structure is what the first few zeros
know about the primes, and it is erased, arm by arm, by the next ones.

## H. Cross sections of the saddle

*Orientation.* Height is measured toward the trail, the tour's up (`−z`). Seen that way the saddle falls away along
`±x`, where the squares and pronic rays run, and rises along `±y`, where the one-to-four products climb. An earlier
version of this page called the x axis the ridge and the y axis the valley, which is how the surface looks from the
other side (`+z`, the side the tour's saddle wire is labelled from); the pictures here are taken from the trail's side.
The narrated Experiments modules keep the recorded ridge/valley wording, which is the `+z` reading.

The saddle is `z = b · K (x² − y²)` with the bend `b` (1 in the tour, 0 flat). Its sections by the three
coordinate planes are a parabola falling away from the trail (`y = 0`), a parabola rising toward it (`x = 0`), and hyperbolas
(`z = h`), with the two diagonals at `z = 0`. What each plane collects, exactly:

| plane | saddle section | numbers in it | product curves that run along it | zeta trail pierces it at |
|---|---|---|---|---|
| `y = 0` (x axis, falls away) | `z = bKx²` | the squares (`+x`), the pronic numbers (`−x`) | every 1:1 product `u(u + d)` (all the cancellation curves) | the **Gram points** (`ζ` real, `θ = mπ`) and the zeros |
| `x = 0` (y axis, rises) | `z = −bKy²` | `√n ≡ ¼` and `¾`: `u(4u + 1)` on `+y`, `u(4u + 3)` on `−y` | the 1:4 products with odd gap | the **half-Gram points** (`ζ` imaginary, `θ = (m + ½)π`) and the zeros |
| `z = 0` (level) | the diagonals `y = ±x` | `√n ≡ ⅛, ⅜, ⅝, ⅞` | the 1:16 products with odd gap, `u(16u + d)` | the trail's foot, `t = 0` |
| `z = h` (level) | hyperbola `x² − y² = h/bK` | all `n` with `n · cos(4π√n) = h / bKR²`: one contour of the Voronoi harmonic | cut once each, near `n ≈ h/bKR²` on the x axis | one dial value, `t = −4h` (planes above the centre only) |

The general rule behind the middle column: a product `u(q²u + d)` has `√n ≈ qu + d/2q`, so it straightens
along the ray at angle `2π · d/2q`. `q = 1` gives the two x-axis rays, `q = 2` the two y-axis rays, `q = 4` the
four diagonals. The bend does not move any of this (the planes `x = c`, `y = c` cut the same numbers at every
bend); it only decides how steeply the x-axis family falls away and the y-axis family rises, and which contour a level
plane picks: raising the bend from ¼ to 2 pulls the `z = −15` contour in from `n ≈ 1370` to `n ≈ 170`, so a
fixed plane sweeps inward through the one-to-four family and reads off where each curve crosses that height.

![H1](H1-x-axis-plane-side.jpg)
![H3](H3-x-axis-plane-trail.jpg)
![H4](H4-y-axis-plane-side.jpg)
![H7](H7-zero-level-full.jpg)

The trail column is the one worth keeping. `ζ(½ + it) = Z(t) e^{−iθ(t)}` with `Z` real, so the trail crosses
the x-axis plane exactly when `sin θ = 0` (the Gram points) and the y-axis plane exactly when `cos θ = 0`. The
coordinate planes of the saddle are therefore the Gram planes of the trail, and the correspondence

| Sacks spiral, clock `√n` | zeta trail, clock `θ/π` |
|---|---|
| squares and pronics: `√n ≡ 0, ½` (x-axis plane) | Gram points: `θ/π ≡ 0` (x-axis plane) |
| `u(4u + odd)`: `√n ≡ ¼, ¾` (y-axis plane) | half-Gram points: `θ/π ≡ ½` (y-axis plane) |
| `u(16u + odd)`: `√n ≡ odd/8` (diagonals) | `θ/π ≡ ±¼`: quarter-Gram points |

is exact, not a metaphor: both are "where the clock reads a whole or half value". Section D is the same
correspondence seen from above.

![H8](H8-level-planes-oblique.jpg)
![H10](H10-level-contours-full.jpg)
![H11](H11-bend-0.25-planes-15.jpg)
![H11b](H11-bend-2.00-planes-15.jpg)

**Unconventional spatial readings that fall out of the sections.**

* *The two clocks as two axes.* Unroll the spiral into a chart with `√n` along `x` (turn count) and the position
  within the turn along `y`. The saddle depends only on `y` (falling away at 0 and ½, rising at ¼ and ¾, horizontal
  stripes); a zero's wave depends only on `x` (its crests are the log-spaced vertical lines); the cancellation
  curves are the `1/u` hyperbolas converging to the stripes at 0 and ½, and Euler's line converges to the pronic
  stripe from above. The primes are the gold points no hyperbola passes through. The chart makes "the √ clock
  and the log clock are orthogonal" literal.

  ![H12](H12-unrolled-chart.jpg)

* *The saddle is doubly ruled.* `z = K(x − y)(x + y)`, so every vertical plane `x − y = a` (or `x + y = a`) cuts the
  saddle in a straight line. The sections along the diagonal directions are the only straight ones.
* *The saddle height is a Lorentz invariant.* A hyperbolic rotation of the plane (`x, y → x cosh a + y sinh a,
  x sinh a + y cosh a`) preserves `x² − y²`, so it slides every number along its own level hyperbola without
  changing its height: the level sets of the saddle are boost orbits, and the Voronoi harmonic
  `n cos(4π√n)` is the invariant.
* *Inversion.* Inverting the plane in the unit circle (`r → 1/r`) sends the straightened cancellation curves
  `y = −πd²/4` to circles through the origin of diameter `4/πd²`: an inverse-square family of circles, all tangent
  at the origin, and it puts the integer `n` at radius `1/√n`, which is the length of the chain's `n`-th link. The
  chain's inverse-square-root link lengths and the Sacks radii are the same numbers seen from the two sides of
  the unit circle.

**Double cancellations.** A number on two cancellation curves is more composite, not less, so no intersection
of product curves ever reverts to a prime. But the *bookkeeping* of the sieve does flip sign at intersections,
and that sign is where the zeta function re-enters. Legendre's count of primes strikes every multiple of each
small prime, then restores every number struck twice (two distinct prime factors), strikes again those struck
three times, and so on. The net sign at `n` is the Möbius function `μ(n)`: `−1` on a prime, `+1` on a product of
two distinct primes (struck twice, restored once), `−1` on three, `0` whenever a prime factor repeats. On the
saddle that is a four-colour field (gold, cyan, magenta, dark), and its running total `M(x) = Σ μ(n)` is the
Mertens function, whose size is equivalent to the Riemann hypothesis (`M(x) = O(x^{½+ε})`). Its Dirichlet series
is `1/ζ`: the "restored" numbers are exactly the ones that make the inverse chain converge where the chain
itself diverges. Up to 1 601 the field has 252 primes, 450 cyan restorations (plus 34 numbers with four distinct primes, also
`+1`), 241 magenta triples and 623 dark numbers, and `|M(x)|` never exceeds `√x`.

![H14](H14-mobius-full.jpg)
![H18](H18-mertens-panel.jpg)
![H16](H16-mertens-oblique.jpg)

## I. The vortex eyes of the chain, traced through the zero planes

**Detection.** At a dial value `t` the chain's `n`-th link turns by `−t · log(1 + 1/n)`, reduced to `(−π, π]`. The
detector uses nothing else: a *knot* is where that turn flips through `±π` (the chain folds back on itself), a
*spoke* where it flips through `0` (the chain runs straight). Each knot is the eye of one vortex; the vortex's
extent is the stretch between the spokes on either side; its certainty is `1 − e^{−L/6}` with `L` the number of
links in that stretch turning by more than half a radian. A vortex drawn by two links is faded to black, one
drawn by thirty is at full colour. The eye is the mean of the four vertices around the knot. Vortex `k` is the
one whose knot sits near `n = t/(2k + 1)π`; vortex 0 is the tail. This was run at all 10 000 zeros in the table
(199 096 vortices, `k ≤ 12`), and the detector's output is written to `vortices.csv` beside the frames.

![I1](I1-vortex-tower-1-40-oblique.jpg)
![I4](I4-vortex-tower-1000-1040-oblique.jpg)

**What the traces do.** Colour is the vortex index, brightness its certainty, each zero is a plane, the white
curve is `ζ(½ + it)` threading the planes through their centres. Vortex 1 (cyan) winds once around the axis per
zero on a circle of radius exactly one; vortex 2 (magenta) rides an epicycle on it; the higher vortices are
dimmer, more erratic, and farther out.

**The law behind it.** The eyes are not new information. At every zero, to the accuracy of the detector,

    eye_k(γ) = −e^{−2iθ(γ)} · conj(S_k(γ)),       S_k = Σ_{n≤k} n^{−½−iγ}

the eye of vortex `k` is the chain's own `k`-th vertex, reflected (conjugated and turned by twice the
Riemann–Siegel phase). Off a zero the same holds with `ζ(½ + it)` added. This is the approximate functional
equation `Σ_{n≤x} n^{−s} ≈ ζ(s) − χ(s) Σ_{m≤t/2πx} m^{s−1}` read at the knots, where the dual cutoff `t/2πx = k + ½`
falls exactly between integers and the equation is at its most accurate. The far end of the chain is the near
end in a mirror, vortex by vortex. Grey diamonds in the pictures are the mirror points; they sit inside the eyes.

| vortex `k` | zeros with `t < 1000`: median distance eye → mirror | zeros with `t ≥ 1000` |
|---|---|---|
| 1 | 0.001 | 0.000 |
| 2 | 0.005 | 0.000 |
| 5 | 0.036 | 0.001 |
| 8 | 0.127 | 0.003 |
| 12 | 0.193 | 0.009 |

The mismatch is entirely a matter of how many links draw the vortex: median 0.052 for vortices drawn by fewer
than three curling links, 0.017 for three to five, 0.002 for twelve to thirty, 0.000 beyond. The certainty
fade is therefore honest: faint eyes are the ones the mirror law places least well.

![I11](I11-chain-vortices-zero-100.jpg)
![I13](I13-chain-vortices-zero-10000.jpg)

The three views down the axis are all 10 000 zeros at once: plain (every eye of vortices 1 to 12), with the Gram
phase taken out, and the same for vortices 1 to 4 only, where the point `−1`, the circle of radius `1/√2` and the
next epicycle stand out.

**Vortex 1 is the Gram clock.** With `S_1 = 1` the first eye sits at `−e^{−2iθ(γ)}`: on the unit circle (radius
1.000 ± 0.004 over 9 994 zeros, angle residual below 0.001 rad), at angle `π − 2θ(γ)`. Since `θ` advances by
about `π` per zero, the cyan trace is a helix with one turn per zero, and the amount by which its phase slips
from one zero plane to the next is the Gram offset of that zero: the eye angle reads the zero's position within
its Gram interval. Taking the Gram phase out (multiplying every eye by `e^{2iθ}`) collapses the whole tower onto
the chain's own first vertices: vortex 1 to the single point `−1`, vortex 2 to the circle of radius `1/√2` around
it, vortex 3 to an epicycle of radius `1/√3` on that, and so on.

![I6](I6-vortex-tower-5000-5040-oblique.jpg)
![I7](I7-vortex-tower-all-down-axis.jpg)
![I8](I8-vortex-tower-all-derotated-down-axis.jpg)
![I9](I9-vortex-tower-all-derotated-k1-4-down-axis.jpg)

**Predictive structure.** Over the 10 000 zeros:

| correlation | value |
|---|---|
| Gram offset of zero `j` with the offset of zero `j + 1` | +0.02 |
| Gram offset of zero `j` with the spacing to zero `j + 1` (in Gram units) | −0.28 |
| spacing `j` with spacing `j + 1` | −0.37 |

So the eye angle at one zero plane (its Gram offset) says a little about where the next plane is, a zero late in
its Gram interval being followed by a shorter gap, and consecutive gaps alternate, which is the repulsion of the
zeros that random-matrix statistics predict. It says nothing about the next zero's own offset. Everything the
vortex eyes carry is `θ(γ_j)` and the first dozen terms of the chain, which are the same functions of `γ_j` the
chain started from; the picture is a faithful mirror, not an oracle. What it does give is a clean geometric
object: the chain's far end is its near end reflected through the Gram phase, and the helix of the first eye is
the zeros' Gram clock drawn as a curve through the planes.

## J–O. Sections, second round: the discriminant law

**One law organises every section below.** A quadratic `n = q²k² + Bk + C` traces a curve on the Sacks spiral.
Far out, the curve straightens into a line pointing `B/2q` of a turn round, at perpendicular offset

    offset = −πR · Δ / (4q²),        Δ = B² − 4q²C   (the discriminant)

from that ray. Checked numerically at `k = 10⁶` (`R = 1.45`):

| quadratic | Δ | direction (turns) | measured offset | predicted |
|---|---|---|---|---|
| `k² + 1` | −4 | 0 | 4.5557 | 4.5553 |
| `k² − 4` | 16 | 0 | −18.2218 | −18.2212 |
| `k² + k + 41` | −163 | ½ | 185.6290 | 185.6289 |
| `k² + k − 2` (the curve `C₃`) | 9 | ½ | −10.2505 | −10.2494 |
| `4k² + k + 5` | −79 | ¼ | 22.4925 | 22.4918 |
| `9k² + 2k + 7` | −248 | ⅓ | 31.3816 | 31.3810 |

So the direction of a line is set by `B/2q` and its distance from its ray by its discriminant. Three kinds of line
follow:

* **cancellation curves**: `Δ` a perfect square. The quadratic factors over the integers, so every value past the
  birth is composite. These are exactly the product curves `u(q²u + d)`.
* **prime curves**: `Δ` not a square. They carry primes at a rate fixed by `Δ` (below). `Δ < 0` puts them on the
  far side of the ray from the cancellation curves; `Δ > 0` puts them on the same side, in between the cancellation
  curves.
* **un-cancellation curves**: the mirror images of the cancellation curves, section L.

## J. Planes through the centre, rotated

A vertical plane through the centre at angle `α` cuts the saddle in the parabola `z = bK·t²·cos 2α`. The parabola
falls away from the trail for `|α| < 45°`, is a straight line at 45° (one of the saddle's rulings), and rises
beyond. Turning the plane sweeps the parabola from falling to straight to rising.

![J1](J1-fan-of-parabolas-oblique.jpg)
![J2](J2-fan-of-parabolas-side.jpg)

A plane at a rational angle `a/b` of a turn holds, far out, the families of that direction: `q²k² + Bk + C` with
`B/2q ≡ a/b`. At 90° that is `4k² + k + C` (up) and `4k² + 3k + C` (down); at 45° it is `16k² + k + C` and
`16k² + 5k + C`, which lie along the straight section. An irrational angle holds no family at all.

![J3](J3-plane-90-quarter-families.jpg)
![J4](J4-plane-45-straight-section.jpg)

Counting the numbers in a thin strip around each plane (perpendicular half-width `R/2`, all `n ≤ 2·10⁷`):

| direction (turns) | numbers in the strip | primes |
|---|---|---|
| 0 (squares ray) | 4 420 | 0 |
| ¼, ¾ | 2 210 each | 0 |
| ⅓, ⅔ | 1 473 each | 0 |
| ⅛ | 1 105 | 0 |
| ⅙ | 1 473 | 0 |
| ⅕ | 884 | 0 |
| ⅜ | 2 210 | 155 (7.0 %) |
| ⅖ | 884 | 119 (13.5 %) |
| √2 − 1, π − 3, φ − 1 (irrational) | about 1 405 each | 110, 95, 89 (6–8 %) |

The strip at a small-denominator angle holds only the product line `k(q²k + B)`, which sits almost exactly on the
ray, so it holds no primes. At ⅜ and ⅖ a prime curve of the same family also falls inside the strip. An irrational
angle samples the numbers at random and gets the average prime rate. Rotating the plane therefore finds no
prime-rich direction. It finds prime-free rational directions, set by the product lines, and that structure fades
as the strip is widened.

![J5](J5-eight-strips-top.jpg)

## K. Planes off the axis: the discriminant comb

Vertical planes parallel to the x axis, at `y = πR·C`, each hold **one** quadratic far out. On the `+x` side that is
`k² + C`. On the `−x` side, the plane at `y = −πR(c − ¼)` holds `k² + k + c`. Sliding the plane sideways steps
through the quadratics in order of discriminant. Each plane cuts the saddle in a parabola `z = bK(x² − y²)`
lowered by `bK·y²`, and its quadratic's values climb that parabola. In the pictures, red marks a curve that
factors, blue `Δ > 0`, and green `Δ < 0`.

![K1](K1-comb-oblique.jpg)
![K3a](K3-comb-top-squares-side.jpg)
![K3b](K3-comb-top-pronic-side.jpg)

**How many primes each plane holds.** The prime count on every line from `C = −30` to `60`, for `k ≤ 3 000`, is
drawn as a column of bars, one per plane. Each bar is compared with the Hardy–Littlewood constant
`Π_p (1 − w(p)/p)/(1 − 1/p)`, where `w(p)` is the number of roots mod `p`. That constant depends only on the
discriminant through the Legendre symbols `(Δ/p)`.

![K4](K4-skyline-squares-side.jpg)
![K5](K5-skyline-pronic-side.jpg)

| line | Δ | primes, k ≤ 3 000 | Hardy–Littlewood |
|---|---|---|---|
| `k² + k + 41` (Euler) | −163 | 1 449 | 6.65 |
| `k² + k + 17` | −67 | 938 | 4.17 |
| `k² + k − 1` | +5 | 759 | 3.54 |
| `k² + k + 11` | −43 | 714 | 3.26 |
| `k² + k − 29` | +117 | 711 | 3.23 |
| `k² + 58` | −232 | 651 | 2.97 |
| `k² − 17` | +68 | 528 | 2.36 |
| `k² − 2` | +8 | 413 | 1.85 |
| `k² + 1` (Landau) | −4 | 303 | 1.37 |
| `k² + k + 2` | −7 | 1 (always even) | 0 |
| `k² − 1`, `k² − 4`, `k² + k`, `k² + k − 2`, … | squares | 0 or 1 (the birth) | 0 |

Over the 175 lines that do not factor, the counts and the constants correlate at 0.9995. Three findings,
honestly stated:

1. **Only the square discriminants are prime-free.** Every other plane carries primes, on either side of the ray.
2. **The side of the ray does not decide.** Lines with `Δ > 0` between the cancellation curves average 262–272
   primes, slightly *more* than the `Δ < 0` lines (221–224, whose values are larger). `k² + k − 1` (`Δ = 5`) beats
   Landau's `k² + 1` by 2.5 to 1.
3. **Euler's line is the record**, at the class-number-one discriminant −163. It is followed by −67, −43 and −19.
   This is Rabinowitsch's theorem made visible: `k² + k + c` is prime for all `k ≤ c − 2` exactly when `1 − 4c` is
   one of −3, −7, −11, −19, −43, −67, −163. The lines with an even `c` on the pronic side are always even, which
   is a second, 2-adic way to carry no primes.

## L. The mirror: un-cancellation curves

**Reflecting across the squares ray sends a line of discriminant `Δ` to the line of discriminant `−Δ`.** On the
`+x` side that maps `k² + C` to `k² − C`. The cancellation curves `k² − m² = (k − m)(k + m)` (`Δ = 4m²`) therefore
reflect into `k² + m²` (`Δ = −4m²`), the rows of the Gaussian integers. On the pronic side the mirror image of an
integer line falls between the integers, since an odd `−Δ` is not a discriminant. So only the squares side has
integer mirror images.

These mirror lines earn the name **un-cancellation curves**, because they turn the cancellation test inside out.
Every number has some pairs of them through it, one pair for each proper representation `n = x² + y²` with
`gcd(x, y) = 1`, lying on `k² + y²` at `k = x` and on `k² + x²` at `k = y`.

* **Exactly one pair ⟹ prime (power).** An odd number on exactly one pair is a prime power `pᵏ` with `p ≡ 1 (mod 4)`,
  and every prime `≡ 1 (mod 4)` is on exactly one pair. This is Euler's criterion. It held for every odd
  `n ≤ 200 000`, with no exception.
* **Two pairs ⟹ composite, and the factor falls out.** If `n = a² + b² = c² + d²`, then `gcd(n, ad − bc)` is a proper
  factor. Euler factored `1 000 009 = 1000² + 3² = 972² + 235² = 293 × 3413` this way. Below 1 601, 63 numbers sit on
  two or more pairs:

      1105 = 33² + 4² = 32² + 9² = 31² + 12² = 24² + 23²    gcd(1105, 33·9 − 4·32) = 13    1105 = 13 × 85
       221 = 14² + 5² = 11² + 10²                            gcd(221, 14·10 − 5·11) = 17    221 = 17 × 13
      1601 = 40² + 1²                                        one pair only                   prime

![L1](L1-mirror-comb-full.jpg)
![L3](L3-euler-two-pairs-factor.jpg)

The same holds for the mirror images of `k² − N·m²`, the rows `k² + N·m²`, when `N` is one of Euler's 65 idoneal
numbers. It was checked for `N` = 1, 2, 3, 5, 6, 7 (odd `n ≤ 200 000`) and 1848 (`n ≤ 3 000 000`), with no
exception. It fails as soon as `N` is not idoneal: for `N = 11`, `15 = 2² + 11·1²` is composite with only one
representation, and there are 1 047 such failures below 20 000. Euler used `N = 1848` to prove `18 518 809 =
197² + 1848·100²` prime by finding it on exactly one line. The test is silent on primes that the form does not
represent, such as `p ≡ 3 (mod 4)` for `N = 1`.

**Fermat's method lives on the other side of the mirror.** Writing `n = u·v` puts `n` on the cancellation curve
`C_{v−u}`. Fermat's method starts at `⌈√n⌉` on the squares ray and steps outward until `m² − n` is a square, and
the number of steps is `(u + v)/2 − ⌈√n⌉ ≈ d²/8√n`. That is proportional to how far the curve through `n` sits from
the ray:

| n | factors | curve | Fermat steps |
|---|---|---|---|
| 1591 | 37 × 43 | `C₆` | 0 |
| 1003 | 17 × 59 | `C₄₂` | 6 |
| 1501 | 19 × 79 | `C₆₀` | 10 |

![L4](L4-fermat-curves.jpg)

So the two classical factoring methods are the two sides of one mirror. Fermat looks for **one** cancellation curve
through `n`. Euler looks for **two** pairs of un-cancellation curves.

## M. The intersection atlas

Twenty-five curves were drawn: eight cancellation curves (`u ≥ 2`, so every value is composite), eleven prime curves
with `Δ < 0` (from `k² + 1` to Euler's) and six lines with `Δ > 0`. Every number that two of them share up to 1 601
is marked, gold if prime and red if composite.

![M1](M1-atlas-full.jpg)
![M2](M2-atlas-centre.jpg)

| pair of curves | meetings | prime |
|---|---|---|
| two prime curves (`Δ < 0`) | 72 | 66 |
| one curve that factors | 78 | 0 |
| other pairs (involving `Δ > 0`) | 94 | 74 |

In all there are 118 meeting points, 34 of them where three or more curves meet. **The law for every meeting point:**
two monic curves `f`, `g` share a value once for each factorization

    (2k + B_f − 2m − B_g) · (2k + B_f + 2m + B_g) = Δ_f − Δ_g

so a pair meets only finitely often, at points fixed by the divisors of the difference of their discriminants.

* Euler's line and Landau's `k² + 1`: the difference is `−159 = −3 · 53`, so they meet at exactly **197 and 1601**,
  both prime. 1601 is the last prime of Euler's run and the edge of the tour's spiral.
* Euler and `k² + k + 17` (difference −96): 47 and 173. Euler and `k² + k − 1` (difference −168): 41, 71, 131, 461.
  All are prime, because they fall inside Euler's prime run.
* A cancellation curve `C_{2j}` meets `f(k) = k² + C` exactly where `(u + j − k)(u + j + k) = f(j)`. The composite
  values of a line are coded by factorizations of the line's own values. The trivial factorization gives the
  identity `f(x + f(x)) = f(x)·f(x + 1)`, which holds for every monic quadratic and is family I of section A. For
  Landau's line, the even case is Sophie Germain's `4i⁴ + 1 = (2i² + 2i + 1)(2i² − 2i + 1)`. For Euler's line the
  values factored are `4E(j − ½) = 4j² + 163`, the sister line of section A.

## N. Tilted planes: hyperbolas off the axis

A plane `z = px + qy + d` cuts the saddle in

    bK (x − p/2bK)² − bK (y + q/2bK)² = d + (p² − q²)/4bK

a hyperbola centred **off the axis** at `(p/2bK, −q/2bK)`, with its axes still along x and y. Turning a plane of fixed
slope `s` about the vertical slides that centre round a circle of radius `s/2bK` (14.4 units for `s = 0.6`), and the
hyperbola never turns.

![N1](N1-tilted-plane-hyperbola.jpg)
![N2](N2-six-tilts-top.jpg)

When the right side is zero the plane is tangent, and the hyperbola collapses into **two straight lines**. These are
the two rulings through the point of tangency: the saddle is doubly ruled, with every point on one straight line
in each diagonal direction. At 1601 the two lines pass within 0.9 units of 44 numbers, 5 of them prime, which is
the ordinary rate and nothing special. Far out, the rulings parallel to the diagonals are the homes of the
one-to-sixteen families `16k² + k + C`, at offsets `−πR(1 − 64C)/64` from the diagonal. The saddle's straight lines
carry those quadratics the way the axis planes carry `k² + C`.

![N5](N5-tangent-plane-1601-top.jpg)
![N6](N6-diagonal-families-on-straight-lines.jpg)

## O. Twisting and folding the saddle

The surface was generalised to `z = bK · r² · cos(F·θ − τ·r)`: `F` folds and a twist `τ` per unit of radius. On the
spiral (`r = R√n`, `θ = 2π√n`) this becomes

    z = bK R² · n · cos((2πF − τR) · √n)

so **on the spiral, a twist is a change in the number of folds**: a twist of `τR = 2π(F − F′)` turns an `F`-fold
saddle into the `F′`-fold pattern. A non-integer `F′ = 2√m` is Voronoi's `m`-th harmonic `cos(4π√(mn))`, so the
divisor function's wobble is a sum of twisted saddles. The pictures use half the tour's bend so the twisted
surfaces stay in frame.

* `τR = 2π` (one fold on the spiral): the squares sit on the crests (`cos = 1`) and fall away, while the pronic
  numbers sit in the troughs and rise. The twist pulls apart the two cancellation families that the plain saddle
  keeps together.
* `τR = 4π` (no folds): every number is on a crest, and the whole spiral lies on the single bowl `z = bKR²n`.
* `τR = 4π(1 − √2)`: Voronoi's second harmonic. Its exact crest numbers are `2t²`, and in general `m·t²` for
  squarefree `m`.
* Four folds put the squares, the pronics and both one-to-four families on crests together. Three folds put the
  squares and the one-to-nine products `k(9k + 2)`, `k(9k + 4)` there.

![O1](O1-twist-half-squares-down-pronics-up-side.jpg)
![O2](O2-twist-full-spiral-on-one-line-side.jpg)
![O4](O4-four-fold-saddle-oblique.jpg)

Twisting and folding change which product families share a crest. They do not gather the primes: no surface of this
kind puts the primes on its crests, because the primes are spread over every family at that family's
Hardy–Littlewood rate.

## P. Two counting problems: the saddle and its half-twist

Two classical lattice-point counts both wobble, and each wobble is a sum of waves that tick with the Sacks clock
`√x`. The waves run at different speeds, and that puts the two problems on two different surfaces.

* **Divisors.** `Σ_{n≤x} d(n)` counts the lattice points under the hyperbola `uv = x`, one for each point of a
  cancellation curve. Around `x log x + (2γ − 1)x` it wobbles by `Δ(x)`. Voronoi (1904) wrote `Δ(x)` as a sum of
  waves `d(n) n^(−3/4) cos(4π√(nx) − π/4)` (section C). Each wave goes up and down **twice** per turn of the spiral,
  and the first one is the saddle.
* **Circles.** `N(x) = #{a² + b² ≤ x}` counts the lattice points in a disk, one for each representation
  `n = a² + b²`, which is one for each point of the mirror lines `k² + m²` (section L). Around `πx` it wobbles by
  `P(x)`. Hardy (1915) wrote `P(x) ≈ (x^¼/π) Σ r₂(n) n^(−3/4) cos(2π√(nx) − 3π/4)`. Each wave goes up and down
  **once** per turn, and the first one lives on the saddle twisted half a turn per turn (section O's `τR = 2π`).

The charts plot `√x` from 3 to 20 along the bottom, with one faint line per turn of the spiral. White is the exact
wobble, cyan the sum of 3 000 waves, orange the first wave alone.

![P1](P1-two-wobbles-charts.jpg)

| problem | 3 000 waves, correlation with the wobble | first wave alone |
|---|---|---|
| divisors, `Δ(x)` | 0.968 | 0.194 |
| circles, `P(x)` | 0.988 | 0.589 |

The first wave of the circle problem carries much more of its wobble than the divisor problem's first wave does,
because `r₂(1) = 4` while `d(n)` keeps growing.

![P2](P2-divisor-first-wave-chart.jpg)
![P3](P3-circle-first-wave-chart.jpg)

On the surfaces, the divisor problem's first wave rides the plain saddle, crossing each turn twice. The circle
problem's first wave rides the half-twisted surface, where the squares sit on the crests and the pronic numbers
in the troughs.

![P4](P4-divisor-first-wave-on-the-saddle-side.jpg)
![P5](P5-circle-first-wave-on-the-half-twist-side.jpg)

**What this is and is not.** It is a pairing of pictures resting on Voronoi's and Hardy's formulas. Divisors and
the cancellation curves go with the saddle; sums of two squares and the mirror lines go with its half-twist. Both
wobbles are conjectured to grow like `x^(¼ + ε)`, and for both problems the best proven exponent is still above
0.31. The pictures do not touch that gap.

## Q. A second set of zeros: the primes of Euler's ring

Euler's `E(k) = k² + k + 41` is the norm of `(2k + 1 + √−163)/2` in the ring `O = Z[(1 + √−163)/2]`, which has
unique factorization. In `O` an ordinary prime `p` **splits** into two primes of norm `p` when `(−163/p) = 1`,
**stays inert** as a prime of norm `p²` when `(−163/p) = −1`, and ramifies when `p = 163`. Only split primes and
163 can divide a value of `E`. Every prime below 41 is inert and 41 is the first that splits, which is why
`E(0) … E(39)` are all prime.

Split primes are counted by the zeros of the Dirichlet L-function `L(s, χ)` with `χ(n) = (n/163)`, just as all
primes are counted by the zeros of `ζ`. Its zeros were computed for this: **10 462 zeros below height 6 000**, the
first at `t = 0.2029`. The program and its checks are in `Tools/LZeros`: values below 1e-12 by mpmath's
independent evaluation, and the zero count within half a zero of `(T/2π) log(qT/2πe)` at every checkpoint.

The wave `−Σ (1 − γ/T) cos(γ log n)` over zeros below `T = 6 000` spikes at prime powers. Rescaled by `√n/log n`,
ζ's version stands at about 1 at every prime and `1/k` at `p^k`. The L-function's version stands at `+1` at split
primes, `−1` at inert primes and `+½` at their squares. Their sum is the explicit formula of the ring's zeta
function `ζ(s)·L(s, χ)`: it spikes at the norms of the ring's prime ideals.

![Q1](Q1-zeta-zeros-every-prime-oblique.jpg)
![Q2](Q2-l-zeros-split-up-inert-down-oblique.jpg)
![Q3](Q3-both-the-primes-of-euler-ring-oblique.jpg)

| n ≤ 800 | count | ζ zeros | L zeros | both |
|---|---|---|---|---|
| split primes | 60 | 0.95 | 1.02 | **1.97** |
| inert primes | 78 | 0.96 | −1.02 | **−0.06** |
| 163 | 1 | 0.99 | 0.00 | 0.99 |
| even powers of inert primes (4, 9, 16, 25, 49, …) | 15 | 0.35 | 0.39 | 0.74 |
| odd powers of inert primes | 8 | 0.22 | −0.25 | −0.03 |
| not a prime power | 637 | −0.06 | 0.00 | −0.06 |

The separation is complete up to 800. The lowest split prime stands at 1.92, and every inert prime and every
non-prime-power stays below 0. Below 41 the only spikes left standing are 4, 9, 16 and 25: 4, 9 and 25 are the
norms of the inert primes 2, 3 and 5, and 16 is the norm of 2².

![Q4](Q4-centre-nothing-below-41.jpg)
![Q5](Q5-both-with-euler-line.jpg)

**What this is and is not.** The second set of zeros knows which primes can divide Euler's numbers, and every
crossing of a cancellation curve with his line is built from those spikes. That is exactly the information in one
Legendre symbol `(−163/p)`, written as a wave. It does not say which values `E(k)` are prime: that is the
Bunyakovsky and Hardy–Littlewood question of section K, still open for every quadratic.

## R–U. Pinching the spiral

A pinch brings different parts of the spiral together. It identifies points that were apart, so whatever the
identified points share piles up in one place. Four pinches were tried, each a map with a parameter `s` from 0 (the
Sacks spiral) to 1 (fully pinched). The white glows mark what needs attention. They are drawn with the experiment
modules' attention bloom (`Halo` on lines, persistent glow marks at points) far above the bloom threshold. Top views
are on the flat spiral, so the arms and slots read as straight lines; oblique views are on the saddle.

### R. The fold: one comb of all discriminants

Doubling the angle (`θ → 2θ`) folds the plane onto itself and lays the squares ray and the pronic ray on one ray.
Every monic quadratic `k² + Bk + C` then straightens at a distance from that ray set by its discriminant alone, for
both families (checked at `k = 10⁶`):

    offset after the fold = −πR · Δ / 2

| line | Δ | measured | predicted |
|---|---|---|---|
| `k² + 1` | −4 | 9.1113 | 9.1106 |
| `k² + k − 1` | 5 | −11.3884 | −11.3883 |
| `k² − 4` | 16 | −36.4437 | −36.4425 |
| `k² + k + 41` | −163 | 371.2581 | 371.2577 |

So the fold merges the squares-side comb (Δ ≡ 0 mod 4) and the pronic-side comb (Δ ≡ 1 mod 4) into **one comb of
all quadratic discriminants**. The cancellation curves (perfect squares Δ) all lie on one side of the ray and the
Euler-type lines (negative Δ) on the other. The merged comb shows the prime 2 as a period-8 stripe (|Δ| ≤ 120,
primes for `k ≤ 3 000`):

| lines with | count | mean primes |
|---|---|---|
| Δ ≡ 5 (mod 8): values never even | 30 | **438.3** |
| Δ ≡ 0 (mod 8) | 28 | 241.0 |
| Δ ≡ 4 (mod 8) | 27 | 244.2 |
| Δ ≡ 1 (mod 8), not a square: values always even | 25 | 0.2 |
| Δ a perfect square: the line factors | 11 | 0.5 |

![R1a](R1a-fold-1-sacks.jpg)
![R1c](R1c-fold-2.jpg)
![R2](R2-fold-one-comb.jpg)
![R3](R3-fold-comb-skyline.jpg)
![R4](R4-fold-on-the-saddle.jpg)

### S. The residue pinch: arms

Sliding every number around its own circle to the angle `2πn/m` collapses the spiral onto `m` arms, one per residue
class mod `m`. The primes ride the `φ(m)` arms of the classes coprime to `m`. The other arms are **cancellation
arms**: the only prime on each is the prime that divides `m` (glowing). Each prime arm is enriched by `m/φ(m)`:

| m | arms | prime arms | enrichment | numbers below 10⁷: arms that are squares mod m vs the rest |
|---|---|---|---|---|
| 4 | 4 | 2 | 2 | 1 mod 4: 332 180  ·  3 mod 4: 332 398 |
| 6 | 6 | 2 | 3 | 1 mod 6: 332 194  ·  5 mod 6: 332 383 |
| 30 | 30 | 8 | 3.75 | mean of 1, 19: 83 006.5  ·  others: 83 093.8 |
| 210 | 210 | 48 | 4.375 | mean of the 6 squares: 13 806.7  ·  others: 13 850.8 |

Pinched mod 4 on the saddle, the two prime arms (1 and 3 mod 4) are exactly the two rising lobes along ±y, and the
even numbers fall away along ±x. Within the tour's 1 601 numbers the 3-mod-4 arm leads 131 to 120. It first falls
behind at 26 861. The arms of quadratic residues lag in every row: that is Chebyshev's bias, which the zeros of the
Dirichlet L-functions control (section Q).

![S0](S0-mod-4-rising-lobes.jpg)
![S1](S1-mod-6.jpg)
![S2](S2-mod-30-halfway.jpg)
![S3](S3-mod-30.jpg)
![S4](S4-mod-210.jpg)
![S5](S5-mod-30-on-the-saddle.jpg)

### T. The pleat: curves that gather primes

Write every number as `n = k(k + 1) + c` (`0 ≤ c ≤ 2k + 1`, counted from the pronic number before it) and slide it
back along its own turn to slot `c mod L`, stacking the folds along the surface normal. Each turn is pleated into `L`
slots after the pronic ray, and slot `j` becomes the curve `k² + k + j`. A slot curve can dodge a prime `p` only if
`k² + k + j` has no root mod `p`. The slots that dodge every prime dividing `L` are the **anti-cancellation curves**,
glowing gold. The even slots are cancellation curves, since `k² + k` is always even.

| L | anti-cancellation slots | enrichment, n ≤ 1 601 | n ≤ 10⁷ | predicted Π p/(p − 1) |
|---|---|---|---|---|
| 6 | 5 | 3.07 | 3.00 | 3.000 |
| 30 | 11, 17 | 4.41, 4.28 | 3.76, 3.74 | 3.750 |
| 210 | 11, 17, 41, 101, 137, 167 | (too few per slot) | 41: 4.54, 101: 4.46, 17: 4.37, 137: 4.33 | 4.375 |

The pleat mod 30 gathers the primes onto two curves named by Euler's lucky numbers, `k² + k + 11` and `k² + k + 17`;
`k² + k + 41` folds onto slot 11. The 14 even slots hold no primes at all. On the squares side (`n = k² + C`, slot
`C mod L`) the best slots reach only 1.50 (L = 6) and 1.88 (L = 30), half the pronic side. `k² + C` takes both
parities, so no squares-side curve can dodge 2. **This is why the prime-rich Euler lines all live on the pronic
side.**

The gold curves still carry composites, glowing white: 18 on slot 11 and 16 on slot 17 below 1 601. Examples are
`121 = 11²`, `143 = 11·13`, `221 = 13·17`, `289 = 17²`, `323 = 17·19` and `749 = 7·107`. Each is built from primes
larger than 5, which the pleat mod 30 cannot see.

![T1](T1-pleat-30-halfway.jpg)
![T2](T2-pleat-30.jpg)
![T2b](T2b-pleat-30-close.jpg)
![T3](T3-pleat-6.jpg)
![T4](T4-pleat-30-on-the-saddle.jpg)
![T5](T5-slot-bars-pronic-vs-squares.jpg)

### U. Many dimensions: the residue lattice

Pinching modulo 3, 5 and 7 at once, as three axes, places every odd number in one cell of a 3 × 5 × 7 lattice. The
three zero planes (`n ≡ 0` mod 3, 5 or 7) are cancellation planes. Up to 10⁷ the primes fill exactly the `2·4·6 = 48`
cells off them, with between 13 757 and 13 922 primes per cell (a spread of 1.2 %). The only primes on the planes are
3, 5 and 7 themselves. Parity is a fourth axis: the even lattice beside it holds the single prime 2.

![U1](U1-residue-lattice-3-5-7.jpg)
![U2](U2-residue-lattice-with-parity.jpg)

### What pinching can and cannot do

Every pinch that clusters primes does it the same way. It sorts numbers by their remainders modulo the primes it
sees, and it gathers the primes onto the curves, arms, slots or cells that never meet those primes. The enrichment
of the best curve is `Π p/(p − 1)` over the primes it sees, which grows only like `e^γ log P` (Mertens):

| primes seen, up to | 2 | 3 | 5 | 7 | 11 | 13 | 17 | 19 | 23 | 29 | 31 | 37 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| enrichment of the best curve | 2.00 | 3.00 | 3.75 | 4.38 | 4.81 | 5.21 | 5.54 | 5.85 | 6.11 | 6.33 | 6.54 | 6.72 |

Euler's line is the anti-cancellation curve for every prime up to 37, and its Hardy–Littlewood constant, 6.648, is
that product (6.724) trimmed slightly by the primes above 41. The pinch pictures therefore show the sieve as
geometry. They cannot do better than the sieve: the brightest pinched curve always keeps the composites made of
primes it cannot see. Seeing more primes needs a finer pinch, and the gain grows only with the logarithm of the
largest prime used.

## V–Y. 144 = 12 × 12 as a basis

Twelve is the smallest number whose remainders decide divisibility by both 2 and 3, the two primes that do most of
the cancelling. 144 = 12 × 12 gives two independent copies of that clock. Four ways of grouping the spiral's curves
with it follow.

### V. The 12 × 12 torus: every curve becomes a straight line

Every number has a turn `k = ⌊√n⌋` (which ring of the spiral it sits on) and a step `n − k²` (how far it is round
that ring from the squares ray). Taken mod 12, both put `n` on a 12 × 12 board, and the board wraps into a torus:
the turn goes the long way round, the step the short way. On this board every curve `k² + Bk + C` is a **straight
line of slope B**. The squares-side curves `k² + C` are vertical lines, and the pronic-side curves `k² + k + c` are
diagonals.

A prime above 3 can sit only where `k² + step` is 1, 5, 7 or 11 mod 12. That happens in exactly 48 of the 144 cells,
4 per row, in a pattern that repeats every 6 rows. Up to 10⁷ each of those cells holds between 13 626 and 13 997
primes, and the other 96 hold only 2 and 3.

On average, a line's prime density is therefore **three times the share of rows it spends in allowed cells**. The diagonal of
`k² + k + c` depends only on `c mod 12`:

| c mod 12 | rows in allowed cells | predicted density | measured (50 lines each, k ≤ 3 000) |
|---|---|---|---|
| 5, 11 | 12 of 12 | 3 | 2.99, 3.00 |
| 1, 7 | 8 of 12 | 2 | 1.94, 2.03 |
| 3, 9 | 4 of 12 | 1 | 1.00, 0.99 |
| even | 0 | 0 | 0 |

Density is relative to `1/log n`. **The Euler lines are the two diagonals that never leave the allowed cells.**
41 ≡ 5, 17 ≡ 5 and 11 ≡ 11 (mod 12) all belong there. The vertical lines `k² + C` spend at most 6 rows of 12 in
allowed cells: C ≡ 1 (mod 3) gives 1.5 (measured 1.48 to 1.51), C ≡ 0 gives 1.0 and C ≡ 2 gives 0.5. So no
squares-side curve can match a pronic-side one, the same factor of 2 found in section T.

![V1](V1-torus-board-diagonals.jpg)
![V1b](V1b-torus-board-verticals.jpg)
![V2](V2-torus-3-4.jpg)
![V2b](V2b-torus-top.jpg)
![V3](V3-classes-on-the-spiral.jpg)
![V3b](V3b-classes-on-the-saddle.jpg)

### W. The saddle computes Legendre symbols

The saddle's height is `K r² cos 2θ`. Pinch the spiral by residues (section S), so that `n` sits at angle `2πn/m`.
Then a number's height is `K r² cos(4πn/m)`, a twelfth-root-of-unity sum in disguise. For two moduli this cosine is
exactly a quadratic character:

* **m = 24:** `cos(πn/6) = ±0.866` with the sign of **(3/p)**, the Legendre symbol that says whether 3 is a square
  mod `p`. Primes `≡ ±1 (mod 12)` fall away from the trail; primes `≡ ±5 (mod 12)` rise.
* **m = 16:** `cos(πn/4) = ±0.707` with the sign of **(2/p)**. Primes `≡ ±1 (mod 8)` fall, `≡ ±3 (mod 8)` rise.
* On the saddle turned 45° (`z = 2Kxy`), m = 8 gives `(−1/p)` and m = 6 gives `(−3/p)`.

All four agree with the Legendre symbol for every prime up to 2 000 000, with no exception. The 144-hour pinch on a
12-fold saddle (`z = K r² cos 12θ`) computes the same cosine, `cos(πn/6)`: its 48 prime arms sit at exactly ±0.866 of
the height, by (3/p). Up to 10⁷ there are 332 225 primes with (3/p) = +1 and 332 352 with (3/p) = −1. By class, 1 mod 12 has
166 011, 5 has 166 169, 7 has 166 183 and 11 has 166 214: the residue class 1 lags, which is Chebyshev's bias again.

The identities behind this are `cos(πn/6) = (√3/2)·(3/n)` and `cos(πn/4) = (√2/2)·(2/n)` for n prime to 6 and to 2.
They hold because `√3 = 2 cos(π/6)` and `√2 = 2 cos(π/4)` are Gauss sums. A pinch to 10 arms computes `(5/p)` too,
but at two unequal heights, `(√5 − 1)/4 = 0.309` and `−(√5 + 1)/4 = −0.809`, since `cos(2π/5)` and `cos(4π/5)` are
the two halves of the Gauss sum `√5` (checked for every prime up to 2 000 000).

![W1](W1-saddle-computes-3-over-p.jpg)
![W1b](W1b-saddle-computes-3-over-p-top.jpg)
![W2](W2-saddle-computes-2-over-p.jpg)
![W3](W3-144-arms-12-fold-saddle.jpg)

### X. Fingerprints mod 144: the last two base-12 digits

`n mod 144` is the last two digits of `n` written in base 12, the dozen digit and the unit digit. Laid out as a
12 × 12 board, every prime above 3 ends in 1, 5, 7 or b, so the primes fill exactly four columns: the 48 unit cells,
with 13 768 to 13 902 primes each up to 10⁷. A curve's fingerprint is the set of cells its values visit:

| curve | cells visited | of the 48 unit cells |
|---|---|---|
| squares `k²` | 16 | 6 (1, 25, 49, 73, 97, 121: every `p²` is 1 mod 24) |
| `k² + 1` | 16 | 8 |
| pronic `k(k + 1)` | 32 | 0 (always even) |
| Euler `k² + k + 41` | 32 | **32** |
| `k² + k + 17` | 32 | **32** (24 cells shared with Euler's) |
| `k² + k + 1` | 32 | 24 |
| product `u(4u + 1)` | 64 | 8 |

Euler's line only ever visits unit cells. It fills the columns 5 and b completely and the columns 1 and 7 in four
rows each: in base 12 its values end in 5 or b twice as often as in 1 or 7.

![X1](X1-fingerprints-mod-144.jpg)
![X2](X2-fingerprints-close.jpg)

### Y. The 144 clock: product families on the 24 half-hours

A family `q²k² + Bk + C` straightens toward the direction `B/2q` of a turn. The directions that are multiples of
1/24 turn, the 24 half-hours of a clock, are exactly those with `q` dividing 12, so with `q²` dividing 144. On each
half-hour lies a product family `u(q²u + d)`:

| hours (24ths of a turn) | family |
|---|---|
| 0, 12 | squares `u·u`, pronic `u(u + 1)` |
| 6, 18 | `u(4u + 1)`, `u(4u + 3)` |
| 4, 8, 16, 20 | `u(9u + 1)`, `u(9u + 2)`, `u(9u + 4)`, `u(9u + 5)` |
| 3, 9, 15, 21 | `u(16u + 1)`, `u(16u + 3)`, `u(16u + 5)`, `u(16u + 7)` |
| 2, 10, 14, 22 | `u(36u + 1)`, `u(36u + 5)`, `u(36u + 7)`, `u(36u + 11)` |
| odd hours prime to 3 | `u(144u + d)`, d = 1, 5, 7, 11, 13, 17, 19, 23 |

In a strip of half-width `R/2` round each ray (n ≤ 4·10⁶), 14 rays hold no primes at all: the 12 even hours (`q`
dividing 6) and hours 3 and 21. The other ten strips hold 16 to 75 primes. Each of them also contains a prime-carrying family with a
small non-square discriminant: `16k² + 11k + 2` (Δ = −7) beside `u(16u + 3)` at hour 9, and `144k² − 23k + 1`
(Δ = −47) beside `u(144u + 1)` at hour 1.

![Y1](Y1-clock-144-top.jpg)
![Y2](Y2-clock-144-on-the-saddle.jpg)

### What the 12 × 12 basis shows

144 is the natural size for these pictures. It holds two copies of the twelve-clock that decides divisibility by 2
and 3, and its square divisors are exactly the leading coefficients of the families on the 24 half-hours. In that
basis:

* the curves become straight lines on a torus;
* their prime densities become a count of allowed cells (3, 2, 1 or 0 for the pronic side, at most 1.5 for the squares side);
* the saddle's own height evaluates (3/p) and (2/p).

All of it is the local behaviour at 2 and 3. It explains why Euler's lines are three times as prime as an average line,
but no grouping by 144 can see a prime larger than 3.

## Z1–Z4. Mirrors, a sieve machine, and twisted tori

This round asked whether a toroidal, umbilic or Möbius transformation of the spiral can separate the primes, the
cancellation curves and the anti-cancellation curves well enough to find primes or to factor. Four constructions were
tried ([`SacksTwisted.cs`](../../Assets/Tour/SacksTwisted.cs)). The first gave a quantitative surprise, the second is a
factoring machine, and the last two sort numbers by residues and do nothing more.

### Z1. Every number is a mirror for prime pairs

Take a number `m` and step `j` to either side. Within one turn of the spiral, `m − j` and `m + j` sit symmetric
across the ray through `m`. When both are prime they are a Goldbach pair of `2m`, drawn as a chord across the ray.
Counting the pairs with `j ≤ k` on the mirrors of each curve gives the reverse of what the prime curves suggest.

| mirror curve | pairs per mirror, relative to Euler's line | Hardy–Littlewood prediction |
|---|---|---|
| `k(k + 1)` (pronic, a cancellation curve) | 2.152 | 2.125 |
| `u(u + 6)` | 1.723 | 1.700 |
| `u(u + 30)` | 1.610 | 1.600 |
| `k²` (squares) | 1.517 | 1.504 |
| `k² + 1` (Landau) | 1.179 | 1.161 |
| `k² + k − 1` | 1.100 | 1.098 |
| `k² + k + 17` | 1.028 | 1.021 |
| `k² + k + 41` (Euler) | 1.000 | 1.000 |

The counts run over `k` from 1 000 to 3 000 and are normalised by `k / log² m`. The pronic mirrors hold 48 726 pairs
and Euler's 22 638. The prediction is the singular series of Goldbach's conjecture in Hardy and Littlewood's form.
Every odd prime `p` that divides the mirror multiplies its pairs by `(p − 1)/(p − 2)`, because such a `p` spoils a pair
`m ± j` only when `p | j`, one residue class of `j` instead of two. Pronic numbers are always even, and divisible by 3
two times in three. Euler's values have no prime factor below 41. So **the cancellation curves are the best mirrors
for prime pairs, and the anti-cancellation curves are the worst.**

Inside the tour's spiral, over turns 10 to 39, the pronic mirrors reflect 54 pairs, the squares 37 and Euler's line 16.
Fifteen of Euler's thirty mirrors reflect none. The chords of the squares and pronic mirrors are all perpendicular to
the x axis, a barcode across the spiral. Euler's chords point every which way, because the direction of its ray swings
from turn to turn.

![Z1a](Z1a-mirror-chords-pronic.jpg)
![Z1b](Z1b-mirror-chords-squares.jpg)
![Z1c](Z1c-mirror-chords-euler.jpg)
![Z1e](Z1e-mirror-skyline.jpg)

**The Möbius band of chords.** A chord is an unordered pair of angles `{θ₁, θ₂}`. The unordered pairs of points on a
circle form a Möbius band whose single edge is the diagonal `θ₁ = θ₂`. With centre `c = θ₁ + d/2` and half-width
`w = d/2`, where `d = θ₂ − θ₁ mod 2π`, the band is the strip `(c, w)` glued by `(c, w) ~ (c + π, π − w)`. Every chord
of the squares and pronic mirrors lands on one rung, `c ≡ 0 (mod π)`, which is the reflection across the x axis. For
`k` from 10 to 59 that is 61 squares chords and 95 pronic chords, entering the rung from opposite ends. Euler's 35
chords scatter over the band.

![Z1f](Z1f-mobius-chords.jpg)
![Z1g](Z1g-mobius-chords-top.jpg)

### Z2. The sieve machine

Fermat factors `n` by finding `a` with `a² − n = b²`, so that `n = (a − b)(a + b)`. Most candidates `a` can be thrown
out before any square root is taken, because `a² − n` must be a square modulo every `m`. The machine has one ring per
modulus (3, 4, 5, 7, 8, 9, 11, 13, 16, 17 and 19) with a hole at every residue of `a`, gold where `a² − n` can still be
a square. Stepping `a` turns every ring by one hole. Light from the centre runs out through the window at twelve
o'clock until a closed ring stops it.

| n | candidates `a` | left after each ring | first `a` through | factors |
|---|---|---|---|---|
| 1 501 | 39–60 (22) | 14, 7, 4, 1, 1, 1, 1, 1, 1, 1, 1 | 49, with 49² − 1 501 = 30² | 19 × 79 |
| 50 399 | 225–320 (96) | 32, 16, 10, 4, 3, 3, 2, 2, 2, 2, 1 | 300, with 300² − 50 399 = 199² | 101 × 499 |

For 1 501 the ring for 19 is gold all the way round. When a prime `p` divides `n`, `a² − n ≡ a²` is always a square
mod `p`. When it does not, at most `(p + 1)/2` of the `p` holes are open. A prime ring with no dark holes names a
factor by itself.

This is D. H. Lehmer's sieve, which he built from bicycle chains and, in 1932, from gears and a photocell. Its rings
are the circles of a torus (Z3), and turning them together walks a torus knot through the cells. It is fast when the
factors are close to `√n`. When they are far apart, `a` has to climb about `(p + q)/2 − √n` steps, and the rings shorten
the climb only by a constant factor.

![Z2a](Z2a-sieve-board-1501.jpg)
![Z2b](Z2b-sieve-board-50399.jpg)
![Z2c](Z2c-sieve-dial-midway.jpg)
![Z2d](Z2d-sieve-dial-1501.jpg)
![Z2e](Z2e-sieve-dial-50399.jpg)

### Z3. The torus knot of residues

Put `n` at angle `2πn/7` around a torus and `2πn/5` around its tube. The integers wind the (5, 7) torus knot through 35
beads, one for each residue mod 35. Every prime other than 5 and 7 lies on one of the 24 beads coprime to 35, with
3 250 to 3 293 primes on each up to 10⁶. The other 11 beads lie on two circles, the multiples of 5 on a parallel and
the multiples of 7 on a meridian, and hold only 5 and 7 themselves. Euler's `k² + k + 41` visits 12 of the 24 beads and
never touches either circle, since its discriminant −163 is not a square mod 5 or mod 7.

![Z3a](Z3a-torus-knot-5-7.jpg)
![Z3b](Z3b-torus-knot-5-7-euler.jpg)
![Z3c](Z3c-torus-knot-walk-40.jpg)

### Z4. The umbilic torus sorts the turns mod 3

Ferguson's umbilic torus, `x = sin u (7 + cos(u/3 − 2v) + 2 cos(u/3 + v))`, `y = cos u (…)`,
`z = sin(u/3 − 2v) + 2 sin(u/3 + v)`, has a deltoid cross-section that turns a third of a turn per lap. Its single cusp
edge runs around three times before it closes. Wind the spiral onto it, one lap per turn (`u = 2π√n`), just off the
cusp edge and drifting across the face so that the turns stack. Cut at the squares ray, every cross-section has three
positions, and turn `k` lies in position `k mod 3`. The colours of the three stacks trade places at the cut, because the
surface has only one face.

On this surface every curve meets 3 on whole stacks. For the values that lie on their own turn (`k ≥ C`):

| curve | multiples of 3 | primes |
|---|---|---|
| `k² + k + 1` | all 14 values in position 1 | 8 in position 0, 7 in position 2 |
| `k² + k + 3` | all 25 values in positions 0 and 2 | all 8 in position 1 |
| `k² + k + C` with `C ≡ 2 (mod 3)`, such as 17 and 41 | none | in all three |

Without the twist, every turn lies on the same face and the multiples of 3 mix with the primes. A half twist of a flat
strip is a Möbius band, which sorts the turns by parity, and a tube with `q` cusps twisting by `1/q` sorts them mod `q`.
The sorting is the residue lattice of section U, drawn on a surface.

![Z4a](Z4a-umbilic-torus-turns.jpg)
![Z4b](Z4b-umbilic-k2-k-1.jpg)
![Z4c](Z4c-umbilic-k2-k-1-close.jpg)
![Z4d](Z4d-umbilic-k2-k-3-close.jpg)
![Z4e](Z4e-untwisted-k2-k-1.jpg)

### What the twisted shapes do

* **The mirrors are new as a picture, and quantitative.** The pronic ray reflects 2.15 times as many Goldbach pairs as
  Euler's line. The singular series predicts every curve's mirror strength to within 1.5 %. For pairs of primes,
  cancellation and anti-cancellation trade places.
* **The sieve machine factors, but it is classical.** It is Fermat's method with Lehmer's residue filter, and its cost
  still grows like the distance between the factors.
* **The torus knot and the umbilic torus make residue classes visible.** The knot shows `n mod 35`, and the umbilic
  torus shows the turn mod 3 as the three stacks of a twisted tube. Neither can find a prime that the residues have not
  already allowed.

## AA–AE. Intersecting geometries

This round crossed the spiral's curves with other geometries: two quadric surfaces, two lattices, a 12-dimensional
torus, the modular surface, and the cosine waves of L-function zeros. The question was where the intersections carry
the prime, cancellation and anti-cancellation curves ([`SacksIntersect.cs`](../../Assets/Tour/SacksIntersect.cs)). The
frames are 1920 × 1080 (`ZETA_EXP_WIDE=1`).

### AA. The paraboloid and the saddle

Every curve `k² + Bk + C` equals `x² − Δ/4` at `x = k + B/2`. Put `y = √|Δ|/2`: that is `x² + y²` when `Δ < 0` and
`x² − y²` when `Δ > 0`. So **every curve is a vertical slice of the paraboloid `z = x² + y²` or of the saddle
`z = x² − y²`**, read at half-integer steps. The two surfaces meet along `y = 0`, where both are `x²`: the squares.

| slice | Δ | surface, y | prime, k = 0 … 9 or 10 |
|---|---|---|---|
| `k² + k + 41` | −163 | paraboloid, 6.38 | 10/10 (40/40 for k < 40) |
| `k² + k + 17` | −67 | paraboloid, 4.09 | 10/10 |
| `k² + 1` | −4 | paraboloid, 1 | 5/11 |
| `k(k + 1)` | 1 | saddle, ½ | 1/10 |
| `k² + k − 1` | 5 | saddle, 1.12 | 7/10 |
| `u(u + 4)` | 16 | saddle, 2 | 1/11 |
| `k² + k − 3` | 13 | saddle, 1.80 | 3/10 |
| `u(u + 6)` | 36 | saddle, 3 | 1/11 |

The cancellation curves are the saddle's slices at half-integer `y = d/2`, where `x² − d²/4 = u(u + d)` factors; each is
prime only at its birth `d + 1`. The saddle's slices at irrational `y` and all the paraboloid's slices are irreducible, so
they carry primes unless a fixed prime divides every value (`k² + k + C` with `C` even is always even).

The level sets carry factorizations. On the saddle, `x² − y² = n` passes through `((a + b)/2, (b − a)/2)` for every
factorization `n = a·b`: `2·d(n)` points, four for a prime. On the paraboloid, `x² + y² = n` is the Sacks ring of `n`, and
it passes through `r₂(n)` integer points (sums of two squares).

| n | factor points | square points |
|---|---|---|
| 13 | 4 | 8 |
| 23 | 4 | 0 |
| 25 | 6 | 12 (5·5 lies on both, where the surfaces meet) |
| 65 | 8 | 16 |

The two lattice-point problems of section P live on these surfaces. Counting the integer points under the paraboloid's
level sets is the circle problem, and under the saddle's is the divisor problem.

![AA1](AA1-paraboloid-and-saddle.jpg)
![AA3](AA3-factor-pairs-and-squares.jpg)

### AB. Sacks rings through two lattices

The ring of `n` meets the square lattice in `r₂(n) = 4 Σ χ₋₄(d)` points and the triangular lattice in
`r₃(n) = 6 Σ χ₋₃(d)` points, summed over the divisors `d` of `n`. For a prime the four classes mod 12 are four intersection
patterns:

| p mod 12 | square points | triangular points | primes ≤ 1601 |
|---|---|---|---|
| 1 | 8 | 12 | 56 |
| 5 | 8 | 0 | 64 |
| 7 | 0 | 12 | 65 |
| 11 | 0 | 0 | 65 |

The ring of a prime `p ≡ 11 (mod 12)` threads between every point of both lattices. Composite counts multiply: 65 has
16 square points, 91 has 24 triangular, 1105 = 5·13·17 is the first number with 32 square points, and
1729 = 7·13·19 = 12³ + 1 is the first with 48 triangular points (and has no square points). The Sacks point of `n` lies on
the same ring.

**Euler's line is the first row of its lattice.** In the lattice `Z[(1 + √−163)/2]` the norm of `a + bτ` is
`a² + ab + 41b²`. Row 0 holds the squares, and row 1 holds Euler's values: the ring of `k² + k + 41` passes through
the lattice point `(k + ½, √163/2)`. All forty rings for `k < 40` are prime, and the forty-first, 1681 = 41², is the
first composite.

![AB1](AB1-rings-of-primes-meet-two-lattices.jpg)
![AB2](AB2-rings-of-composites.jpg)
![AB3](AB3-euler-lattice-row.jpg)

### AC. The 12-dimensional residue torus and its 144 shadows

`n ↦ (n mod 2, n mod 3, …, n mod 37)` is a point of a 12-dimensional torus. Primes above 37 avoid every hyperplane
"≡ 0". Below 41² = 1681 that is exactly primality, so for the tour's spiral, which stops at 1601, this torus decides it.

The 144 pairwise shadows `Z/pᵢ × Z/pⱼ` show it panel by panel. Primes up to 10⁶ fill the unit cells evenly: the 5 × 7
panel's 24 cells hold 6 171 to 6 241 each. Each panel's cancellation cross holds only `pᵢ` and `pⱼ`. The curves differ
sharply:

* `k² + k + 41` never lands on a cross, because −163 is a non-residue modulo every prime up to 37 (checked for k < 10⁵).
* `k(k + 1)` lands on both arms of every cross.
* `k² + 1` lands only on the crosses of 2, 5, 13, 17, 29 and 37, that is 2 and the primes ≡ 1 (mod 4).

![AC1](AC1-144-shadows-euler.jpg)
![AC2](AC2-144-shadows-pronic.jpg)
![AC3](AC3-36-shadows-close.jpg)

### AD. The curves on the modular surface

Send `k² + Bk + C` to the roots of `x² + Bx + C` in the upper half-plane. For `Δ < 0` that is the point
`τ = (−B + i√|Δ|)/2`, at the paraboloid slice's height. For `Δ > 0` it is the geodesic semicircle of radius `√Δ/2`
about `−B/2`, at the saddle slice's height. The squares side (`B = 0`) lives on `Re τ = 0`, and the pronic side
(`B = 1`) on `Re τ = −½`, the left edge of the modular fundamental domain. The curves fall into three kinds:

* **Points**, for definite forms: the anti-cancellation side.
* **Closed geodesics**, for `Δ > 0` not a square, of length `2 log ε`. Examples: `k² + k − 1` has length 1.92
  (`2 log φ²`), `k² − 2` has 3.53, and `k² − 3` has 2.63.
* **Geodesics that run into the cusps**, for `Δ` a square, where both roots are integers. These are the cancellation
  curves: `k(k + 1)` runs from cusp −1 to cusp 0.

The floor of the fundamental domain, `|τ| = 1`, is the cancellation geodesic of `k² − 1 = (k − 1)(k + 1)`. Landau's point
`i` and the point `ρ` of `k² + k + 1` stand on it.

| curve | j at its point | prime yield |
|---|---|---|
| `k² + k + 1` | 0 | 2.25 |
| `k² + 1` | 1728 = 12³ | 1.37 |
| `k² + 2` | 20³ | 0.70 |
| `k² + k + 11` | −960³ | 3.23 |
| `k² + k + 17` | −5280³ | 4.24 |
| `k² + k + 41` | −640320³ | 6.57 |

Prime yield is primes per `Σ 1/log value` over 3 000 values, that is relative to random numbers of the same size.
Euler's `j` is why `e^{π√163}` = 262 537 412 640 768 743.99999999999925 is almost an integer. The curves `k² + k + C`
with `C` even are always even (grey).

![AD1](AD1-curves-on-the-modular-surface.jpg)
![AD2](AD2-fundamental-domain-close.jpg)

### AE. The prime race between the lattice classes

The four classes of AB race. Each chart plots `(log x/√x)·φ(q)·(π(x; q, a) − π(x; q, 1))`. The prediction has two parts:

* **A bias from prime squares**, `((N₁ − N_a)/2)·π(√x)·log x/√x`. Mod 12 all four units square to 1, so the bias is
  about `4·(1 + 2/log x)`.
* **A sum of cosine waves `2 S_χ(x)`**, one for each character that separates `a` from 1, where
  `S_χ(x) = Σ_γ x^{iγ}/(½ + iγ)` runs over the zeros of `L(s, χ)`. Class 5 needs `χ₋₃` and `χ₁₂`, class 7 needs
  `χ₋₄` and `χ₁₂`, and class 11 needs `χ₋₄` and `χ₋₃`.

The zeros below height 500 come from mpmath, and their counts match N(T): 379 for `L(s, χ₋₄)` (Dirichlet's beta), 356
for `L(s, χ₋₃)` and 466 for `L(s, χ₁₂)` ([`Tools/lzeros12.py`](../../Tools/lzeros12.py),
[`Tools/race12.py`](../../Tools/race12.py)).

| race, to 10⁸ | result | wave fit, rms |
|---|---|---|
| mod 12, class 1 ("both lattices") against 5, 7, 11 | class 1 never leads | 0.28, 0.25, 0.26 |
| mod 4, class 1 ("hits the square lattice") against 3 | 128 lead intervals | 0.16 |

The closest mod-12 call is class 7 against class 1 at 48 231 785, measured at 0.56. The waves put their lowest point,
0.49, at the same x. Mod 4, class 1 first leads at 26 861, then in clusters at 616 841–633 797 and
12 306 137–12 382 313. The waves of the 379 zeros of β(s) dip to 0.07 at 26 861.

Read geometrically: primes whose rings miss a lattice stay ahead of primes whose rings hit it. The lead changes happen
where the cosine waves of that lattice's L-function zeros overpower the bias from prime squares.

![AE1](AE1-prime-race-mod-12.jpg)
![AE2](AE2-prime-race-mod-4.jpg)

### What did not intersect

* **The zeta phase against the Sacks angle.** I took the numbers with `γ log n ≡ 2π√n (mod 2π)` within ±0.15, for the
  first 30 zeros and n up to 10⁶: 1.43 million pairs. Primes came out at 0.997 of the expected count, against 0.995 for a
  shifted control.
* **A 12-fold saddle crossing the 2-fold one.** They cross along rays at tenths and fourteenths of a turn. The prime
  density along those rays is 0.96 to 1.06 of `1/log n`, the same as at a generic angle (1.00).

### What the intersections give

* **Exact restatements.** The quadratic curves are slices of the two norm surfaces. Lattice points on the rings are
  representation numbers, and they multiply. The 12-dimensional torus is the sieve. The modular picture sorts the curves
  into points, closed geodesics and cusp geodesics. The race is set by L-function zeros.
* **New as pictures.** Euler's line is the first row of its lattice. The cancellation curves are the geodesics that end
  in cusps. The floor of the fundamental domain is the geodesic of `k² − 1`. The race mod 12 is a race between lattice
  classes, and the cosine waves locate its near-miss at 48 231 785.
* **No faster prime-finding.** None of it finds primes faster than a sieve.

## BA–BD. The Sacks spiral wound into a ball of yarn

The flat Sacks spiral puts n at radius R√n and turns once per square. The numbers fill the disc evenly, and the squares
line up on one ray. This round wound the counting numbers in three dimensions in two ways
([`SacksYarn.cs`](../../Assets/Tour/SacksYarn.cs)). In each, I looked for the pronic curves and for Euler's line, and for
the intersections that carry prime, cancellation and anti-cancellation curves. The frames are 1920 × 1080
(`ZETA_EXP_WIDE=1`).

### BA. The Sacks yarn ball: every quadratic family becomes a helix

Wind each turn of the spiral (k² to (k + 1)²) as one wrap. A wrap is a loop in a plane through the squares axis, and
each turn's plane is rotated 24° further about that axis. The yarn is continuous, because every wrap starts and ends on
the axis, at a square. The pronic numbers `k(k + 1)` sit half a turn round, at the other end of the axis.

A quadratic family keeps the offset it has in the flat spiral, `πR|Δ|/4` from its ray, but the plane it lives in turns
about the axis. So **every family `k² + Bk + C` becomes a helix round the axis, of radius `πR|Δ|/4`**, and the families
with equal `|Δ|` share one cylinder. On the squares side, `k² + m` and `k² − m` (`Δ = ∓4m`) wind round the cylinder of
radius `πRm` as a double helix, half a turn apart. When m is a square the second strand factors: `k² − 1 = (k − 1)(k + 1)`
and `k² − 4 = (k − 2)(k + 2)` are cancellation helices beside the prime-rich `k² + 1` and `k² + 4`. The distances
measured from the axis, against `πRm`:

| cylinder (R = 1.45) | strands | distance from the axis | primes for k ≤ 1000 |
|---|---|---|---|
| `πR` = 4.555 | `k² + 1` / `k² − 1` | 4.553 / 4.553 at k = 59 | 112 / 1 (only 3) |
| `2πR` = 9.111 | `k² + 2` / `k² − 2` | 9.095 / 9.092 at k = 59 | 68 / 157 |
| `4πR` = 18.22 | `k² + 4` / `k² − 4` | 18.210 / 18.209 at k = 200 | 125 / 1 (only 5) |

**Euler's line is not a helix inside this ball.** Its `Δ = −163` gives a radius of `πR·163/4` = 185.6, wider than the
whole 60-turn ball (radius 87). Inside the ball its values still jump from wrap to wrap: they sit 4.6 from the axis at
k = 39, where `1601 = 40² + 1` lies on Landau's helix (the meeting point of the two lines, see
*Surprises* below), then 72.8 at k = 59. They reach 152.8 at k = 120 and 182.5 at
k = 400.

![BA1](BA1-sacks-yarn-ball.jpg)
![BA2](BA2-double-helices.jpg)

### BB. The cube yarn ball: three corners of a tetrahedron

To fill a ball evenly, the radius has to grow like the cube root, so put n at radius `R∛n`. Layer k holds the numbers
from k³ to (k + 1)³, that is 3k² + 3k + 1 of them. They are wound on the layer's sphere like yarn: down from the north
pole to the south pole and back up, going round `3k + 1` times. The height runs `z = 1 − 4t`, then `−3 + 4t`, with
`t = (n − k³)/(3k² + 3k + 1)`, which covers the sphere at even area. Near height −⅓ each unit of n then moves the same
distance along the yarn on every layer, `2πR·√8/3 ≈ 5.92R`. The cubes sit on the polar axis, as the squares sit on the
flat spiral's ray. The down pass and the up pass cross each other everywhere, as in a real ball of yarn.

Put a cubic family `k³ + Ak² + Bk + C` in the ball. Its place along the yarn tends to `t = A/3`, and its longitude
settles at `120°·A`. So **every monic cubic converges on one of three corners of a regular tetrahedron inscribed in the
ball, sorted by A mod 3**:

* the north pole (A ≡ 0);
* corner 1, at height −⅓ (19.5° below the equator) and longitude 120° (A ≡ 1);
* corner 2, at height −⅓ and longitude 240° (A ≡ 2).

The fourth corner, at longitude 0°, collects no monic cubic. Near corners 1 and 2 the families do more than converge in
direction. They run as parallel strands at fixed offsets, affine in (B, C):

* across the windings: `5.924R · (C − 2B/3 + A/3)`;
* down the windings: `R√2 · (B − A)`.

I checked these at k = 200 to 1600. At corner 1, `k³ + k² + C` moves 5.92R across per unit of C. Each unit of B moves
it 3.95R the other way and 1.41R down. Near the pole the families spread out like √k instead (their angle from the pole
halves each time k grows fourfold), so the pole is a fan, not a comb.

**The 3D pronic numbers** are the cubic products of consecutive numbers, and they are the cancellation strands:

* `(k − 1)k(k + 1) = k³ − k` goes to the pole, with 0 primes for k ≤ 400.
* `k²(k + 1) = k³ + k²` goes to corner 1, with one prime (2).
* `k(k + 1)² = k³ + 2k² + k` goes to corner 2, with 0 primes.

**Euler's quadratic makes no strand.** A quadratic is too slow for the cube clock. Successive values of `k² + k + 41`
are 2k + 2 apart. On their layer, about k^⅔, that is roughly 2∛k windings, so they land at effectively random longitudes
(green).

![BB1](BB1-cube-yarn-ball.jpg)

**Seen down a corner, the ball is a comb.** This view looks along the axis of corner 1 at layers 60 to 120, on the
yarn's down pass. Each strand `k³ + k² + Bk + C` is seen end-on as one spot, and the spots form a lattice. Its columns
are 5.92R apart, its rows R√2 apart, and each row is shifted ⅔ of a column from the last.

* Strands with an integer root stay dark: `k²(k + 1)` and `k(k² + k + 1)`, ringed red.
* The always-odd strands carry the primes: `k²(k + 1) ± 1`, ringed gold.
* Spots further out are short arcs, because their offsets have not settled yet.

The up pass crosses the same corner and makes a second, finer lattice, which is left out of this picture.

![BB2](BB2-down-corner-1.jpg)

**Euler's lines of the cube ball.** Seen from below, the cone at height −⅓ through the two corners shows the flat
spiral's picture again. The 3D pronic strands run straight out along the corner rays. The richest cubics curl in from
the centre and straighten into lines parallel to them, just as Euler's line straightens beside the pronic ray. Offsets
are in columns of 5.92R, and the yield is the number of primes divided by `Σ 1/log f(k)`, the count for random numbers of
the same size:

| strand | corner | offset | primes, k ≤ 400 | yield | first values prime |
|---|---|---|---|---|---|
| `k²(k + 1) + 17` | 1 | +17⅓ | 165 | 5.80 | 11: 17, 19, 29, 53, 97, …, 1117 |
| `k²(k + 1) − 13` | 1 | −12⅔ | 126 | 4.51 | |
| `k(k + 1)² + 13` | 2 | +13 | 126 | 4.44 | 9: 13, 17, 31, 61, …, 661 |
| `k(k + 1)² − 17` | 2 | −17 | 161 | 5.79 | |
| `k² + k + 41` (Euler, for comparison) | none | | 270 | 6.47 | 40 |

`k²(k + 1) + 17` is to corner 1 what Euler's `k(k + 1) + 41` is to the pronic ray: the richest strand, and the one
with the longest run of primes. A wider scan found two further records. It covered every cubic without an integer root
with A = 0, 1, 2, |B| ≤ 15, |C| ≤ 60, for k ≤ 400.

* The best yield is at the pole: `k³ − 13k − 41` (6.19).
* The longest run is `k³ − 13k + 41`: 41, 29, 23, 29, 53, 101, …, 2069 for k = 0 to 13, so 41 turns up again.

![BB3](BB3-euler-lines-of-the-cube-ball.jpg)

### BC. The comb at corner 1: cancellation lines and anti-cancellation spots

The chart puts every strand `k³ + k² + Bk + C` with |B| ≤ 6 and |C| ≤ 24 at its offset, 637 strands in all: across at
`u = C − (2B − 1)/3` columns, down at `v = B − 1` rows. The rows are drawn farther apart than in the ball, for legibility.
The strands fall into three kinds:

* **Cancellation lines.** A strand with an integer root `k = −r` factors as `(k + r)·(quadratic)`, so it is composite
  for every k beyond the first few. Its coefficients satisfy `C = r³ − r² + rB`, which is a straight line through the
  chart for each r (red). 69 of the 637 strands lie on these lines. Where two lines cross, the cubic has two integer
  roots and splits into three linear factors.
* **Parity.** `k³ + k²` is always even, so the strands with B and C both even are always even: 139 strands, drawn dark.
* **Anti-cancellation.** Off the lines the yield over k ≤ 120 varies more than tenfold. The brightest strands are:
  * `k²(k + 1) + 17` (6.03)
  * `k²(k + 1) − 13` (4.95)
  * `k³ + k² − 6k − 19` (4.41)
  * `k³ + k² + 4k + 11` (4.11)
  * `k³ + k² + 4k − 7` (4.10)

![BC1](BC1-comb-at-corner-1.jpg)

### BD. One shell: the multiples cross the windings

One layer, k₀ = 30, holds the numbers 27 000 to 29 790. Near corner 1 its windings are the comb's rows. A winding holds
about k₀ numbers, so the multiples of `m = k₀ + r` step about one winding at a time. Each step also drifts sideways by a
fixed angle, about `(360°/k₀)(r − ⅔)`: −7.9° for 30, +3.9° for 31, +15.6° for 32. So the multiples of each m run in
straight lines across the windings, one slope per m. These are the cancellation lines of a single shell. The steep ones
drift all the way round the sphere and come back into the window as parallel runs.

Where three lines cross sits a number with three divisors near k₀:

* 27 621 = 27·1023 = 31·891 = 33·837
* 27 776 = 28·992 = 31·896 = 32·868
* 27 840 = 29·960 = 30·928 = 32·870
* 28 050 = 30·935 = 33·850 = 34·825

The 3D pronic number 27 900 = 30²·31 lies on two lines, and 27 917 = 30²·31 + 17 is prime.

![BD1](BD1-shell-at-corner-1.jpg)

### What the yarn balls give

* **The flat discriminant law survives in 3D, in two forms.** In the Sacks ball it becomes a helix radius. In the cube
  ball it becomes an affine comb of parallel strands at three corners of a tetrahedron.
* **New as pictures.**
  * Landau's `k² + 1` has a cancellation twin, `k² − 1`, on the same cylinder.
  * The 3D pronic numbers mark three corners of a tetrahedron.
  * Euler's line has cubic analogues that straighten beside the 3D pronic strands, the best being `k²(k + 1) + 17`.
* **No faster prime-finding.** A strand's richness is set by its residues mod small primes, the same Hardy–Littlewood
  count as in the plane, and every strand contains composites.

## CA–CE. The divisor spiral, lifted into a funnel and cut by spheres

The picture *Sacks spiral divisors* (Wikimedia Commons) draws every number up to 100 000 with a dot sized by its
number of divisors d(n). Its bright lines are the factorizations. This round lifted that picture into 3D, cut it with
planes, cones and spheres, and did the same with the cube yarn ball (BB) seen flat
([`SacksFunnel.cs`](../../Assets/Tour/SacksFunnel.cs)). The frames are 1920 × 1080 (`ZETA_EXP_WIDE=1`).

### CA. The divisor spiral: bright product lines, dark prime lanes

A product of two linear factors `(ak + b)(ck + e)` with `ac = q²` has `√n ≈ qk + (ae + bc)/2q`. It therefore runs at
a fixed direction, `(ae + bc)/2q` of a turn, and these are the bright lines of the picture:

| factor ratio | directions | examples |
|---|---|---|
| 1 : 1 | 0, ½ | squares, `u(u + 2)` (right); pronic `u(u + 1)`, `u(u + 3)` (left) |
| 1 : 4 | ¼, ¾ | `k(4k + 1)` at the top, `k(4k + 3)` at the bottom |
| 1 : 9 | ⅙, ⅓, ⅔, ⅚ | `k(9k + 1)` at 60°, `k(9k + 2)` at 120° |
| 1 : 16 | eighths | `k(16k + 1)` at 45° |

(measured at k = 3000: 90.00°, 269.98°, 60.00°, 120.00°, 45.00°.)

![CA1](CA1-divisor-spiral.jpg)

**The prime curves are the dark lanes.** Up to 100 000 the mean of d(n) is 11.7, close to log N = 11.5. Along the
curves:

| curve | mean d(n) | kind |
|---|---|---|
| `k² + k + 41` (Euler) | 2.6 | prime curve |
| `k² + k + 17` | 3.3 | prime curve |
| `k² + k − 1` | 3.8 | prime curve |
| `k² + 1` (Landau) | 6.4 | prime curve, even for odd k |
| `k² + k + 2` | 9.4 | always even, but not a product |
| squares | 16.9 | cancellation |
| `k(4k + 1)` | 20.2 | cancellation, ratio 4 |
| `k(9k + 1)` | 22.5 | cancellation, ratio 9 |
| `u(u + 6)` | 24.1 | cancellation |
| `u(u + 2)` | 26.3 | cancellation |
| `k(k + 1)` (pronic) | 28.8 | cancellation |

So the cancellation curves are two to three times as dense in divisors as an average number, and the prime curves a
quarter to a third. Euler's line and `k² + k + 17` run beside the pronic bundle, between its bright lines.

![CA2](CA2-prime-curves-in-the-dark-lanes.jpg)

### CB. The factorization funnel: parabolic coordinates

Lift each factor pair `n = u·v` (u ≤ v) above n's Sacks point, to the height `R(v − u)/2`. Then for every pair:

* its distance from the axis is `R√(uv)`, the **geometric mean** of the factors (the Sacks radius);
* its distance from the centre is `R(u + v)/2`, the **arithmetic mean**;
* its height is half the **difference**, so AM² = GM² + (half the difference)²: the AM–GM inequality is Pythagoras.

These are parabolic coordinates (σ² = Ru, τ² = Rv), checked to 10⁻¹³ for all pairs with u < 60, v < 200. Every
classical surface cuts out an arithmetic set:

| surface | holds the pairs with | what it shows |
|---|---|---|
| floor (z = 0) | u = v | the squares |
| horizontal plane z = Rd/2 | v − u = d | the cancellation curve `u(u + d)` |
| sphere about the centre, radius Rs/2 | u + v = s | the factorizations `(x − y)(x + y)`, x = s/2 |
| cone about the axis | v/u fixed | one factor ratio: one bundle of CA |
| paraboloid opening up, focus at the centre | u fixed | the multiples of u |
| paraboloid opening down, focus at the centre | v fixed | the numbers with a divisor v |
| vertical line above n | uv = n | the factor pairs of n |

**Each bundle of the divisor spiral is the shadow of rays on its own cone.** A family `(k + b)(q²k + e)` climbs at
slope `(q² − 1)/2q` in a straight line, at half-angle `atan(2q/(q² − 1))` from the axis:

| ratio | slope | half-angle |
|---|---|---|
| 1 : 1 | flat | (the floor) |
| 1 : 4 | 3/4 | 53.13° |
| 1 : 9 | 4/3 | 36.87° |
| 1 : 16 | 15/8 | 28.07° |

These were measured at k = 5000 to four decimals. So the funnels nest by how unequal the factors are, and the bright
lines of the flat picture separate in height.

![CB1](CB1-factorization-funnel.jpg)

**Factorization by intersection.** The vertical line above n pierces the funnel once for each factor pair, at height
`R(v − u)/2`. A number where k bright lines cross in the flat picture has k pairs, and in the funnel those lines pass
over one another at k different heights. The three needles above 5039, 5040 and 5041, neighbours on the spiral, show
this:

* 5040 = 7! has 30 factor pairs, 14 of them below the frame's top: 70·72, 63·80, 60·84, …, 21·240.
* 5041 = 71² has a single point, on the floor.
* The prime 5039 is empty: its only pair, 1·5039, is 3653 units up.

The lowest point on a needle is the most balanced factorization, which is the first one Fermat's method finds.

![CB2](CB2-needles-of-factor-pairs.jpg)

### CC. Spheres find the factors

The sphere of radius `Rx` about the centre crosses the needle above n at height `R√(x² − n)`. That height is a whole
number of R exactly when `x² − n = y²`, and then the crossing is the factor pair `(x − y, x + y)`: **Fermat's method as
spheres crossing a needle.** For 1105, the spheres x = 34 … 40 cross between the ticks:

* x = 41 lands on the tick y = 24 (1105 = 17·65).
* x = 49 lands on y = 36 (1105 = 13·85).
* x = 113 gives 5·221, out of the frame.
* x = 553 gives the trivial 1·1105, also out of the frame.

Each sphere carries the factor points of every `x² − y²`, from the square x² on the floor up to `1·(2x − 1)` at the
top. A prime p appears on a single sphere, x = (p + 1)/2, at its topmost point.

![CC1](CC1-spheres-find-the-factors.jpg)

### CD. Spheres through the cubic lattice: Euler's prime curves are the loneliest spheres

The Sacks ring of n has radius √n, and it is the equator of the sphere of radius √n. That sphere meets the cubic
lattice Z³ in `r₃(n)` points, the ways to write `n = a² + b² + c²`. For squarefree `n ≡ 3 (mod 8)`, Gauss's formula
gives `r₃(n) = 24·h(−n)`, where h is the class number. I checked every such n up to 3000 by direct count:

* **n is prime exactly when r₃(n)/24 is odd.** This held for all 507 squarefree n ≡ 3 (mod 8) up to 5000, of which
  167 are prime. By genus theory, 2^(ω − 1) divides h(−n) when n has ω prime factors, so the point count gives a lower
  bound on the number of prime factors:
  * 35 = 5·7 has 48 points;
  * 195 = 3·5·13 has 96;
  * 1155 = 3·5·7·11 has 192.
* **The sphere holds only 24 points** (a single orbit of `(±a, ±b, ±c)` permutations) **exactly for the Heegner primes
  11, 19, 43, 67 and 163.** These are the discriminants of Euler's prime curves `k² + k + 3, 5, 11, 17, 41`, prime for
  k < (p − 3)/4. For example, 163 = 1² + 9² + 9², and nothing else.
* **Primes p ≡ 7 (mod 8) miss the lattice entirely**, for example 23.

Because n ≡ 3 (mod 4), the equators of these spheres, the Sacks rings, also miss the square lattice (section AB). The
sphere finds the points the ring cannot.

![CD1](CD1-spheres-through-the-cubic-lattice.jpg)
![CD2](CD2-three-squares-parity.jpg)

### CE. The cube ball flattened looks like the divisor spiral

Seen straight down its axis, the cube yarn ball of section BB looks like the divisor spiral. Bright lines leave the
centre along the horizontal axis, with dark lanes between them. Both poles project onto the centre.

* **North pole.** The families converging on it, `k³ + Bk + C`, open out to the right, at distance `R√(8|B|k/3)` from
  the centre. Their angle closes in on 0° like 1/k: −6° at k = 39 and −0.6° at k = 400 for `k³ + k`. The two red
  strands are `k³ + k` and `(k − 1)k(k + 1)`.
* **South pole.** The south pole of layer k is the midpoint `(k³ + (k + 1)³)/2 = (2k + 1)(k² + k + 1)/2`, which is never
  an integer. The two integers beside it factor, as `k(2k² + 3k + 3)/2` and `(k + 1)(2k² + k + 2)/2`, and are composite
  apart from 17 and 5 (checked for k < 400).
  * In the flat view they sit on a ring of radius `2R/√3` ≈ 1.674 around the centre at every layer (measured 1.674 to
    1.675 for k = 10 to 39).
  * They fall on the left for even k and on the right for odd k.
  * They are the cube ball's pronic numbers: the integers just beside the pole, as `k(k + 1)` sits just beside
    `(k + ½)²` on the flat spiral.

The two corners at 120° and 240°, where the comb of section BB lives, are buried under the layers projected on top of
them.

![CE1](CE1-cube-ball-flattened.jpg)
![CE2](CE2-cube-ball-flattened-centre.jpg)

### What the funnel and the spheres give

* **Exact statements, new as pictures.**
  * The divisor spiral is the shadow of the factor pairs in parabolic coordinates.
  * Planes, spheres, cones, paraboloids and vertical lines each cut out a classical set: equal gaps, equal sums,
    equal ratios, multiples, and the factor pairs of one number.
* **Factorization by intersection is Fermat's method.** It finds the most balanced pair first and is fast only when
  the factors are close.
* **Spheres through the lattice see class numbers.** That gives a parity test for primality of squarefree n ≡ 3
  (mod 8), and the Heegner primes behind Euler's curves are the spheres with the fewest points. Counting lattice
  points on a sphere costs about n steps, so none of this finds primes faster than a sieve.

## What the sections give, as algorithms

Everything that came out of the sections as an *algorithm* is classical. The saddle draws each one as geometry:

1. **Primality by counting mirror lines (Euler).** An odd number on exactly one pair of the un-cancellation curves
   `k² + N·m²` (`N` idoneal) is a prime power, and one on two pairs is composite and factored by a gcd. This is
   exact but costs about `√n` steps.
2. **Factoring by finding the cancellation curve (Fermat).** It is fast exactly when the curve through `n` is near
   the squares ray, at a cost of about `d²/8√n` steps.
3. **Sieving a line.** Along a quadratic of discriminant `Δ`, the crossings with cancellation curves repeat with
   period `p` at the roots of the quadratic mod `p`, and only for primes with `(Δ/p) = 1`. Marking them is the
   sieve for prime values of the quadratic. How many primes survive on each plane is the Hardy–Littlewood constant
   of `Δ` (correlation 0.9995).
4. **Long prime runs need class number one (Rabinowitsch).** `k² + k + c` is prime for all `k ≤ c − 2` only for
   `c` = 2, 3, 5, 11, 17, 41, a finite list (Heegner, Stark).
5. **Fermat's walk through residue rings (Lehmer).** Keeping only the `a` for which `a² − n` is a square modulo 3, 4,
   5, …, 19 leaves one candidate of 22 for 1 501 and one of 96 for 50 399, and that candidate is the factorization. A
   prime modulus all of whose residues survive divides `n`. The filter shortens Fermat's walk by a constant factor and
   does not change how it grows.

A purely algebraic description of all primes does exist. Jones, Sato, Wada and Wiens (1976) wrote down a polynomial
of degree 25 in 26 variables whose positive values are exactly the primes. It re-encodes Wilson's theorem, and it is
useless for finding primes. None of the five methods above beats a modern primality test or factoring algorithm (Miller–Rabin, ECPP, AKS).
What the saddle sections add is a single picture in which the sieve, Fermat's and Euler's methods, and the
Hardy–Littlewood constants are all one statement about quadratic curves and their discriminants.

## What I would take from this

1. The cancellation curves are the product curves `n = u(u + d)`. For a prime-generating line they are indexed
   by a quadratic ring, and the first composites after the prime run obey the closed rules in section A.
2. The saddle is the first harmonic of Voronoi's formula for the divisor function: the Sacks spiral's `√n` clock
   is the clock of `ζ²`, the spiral's log clock is the clock of `ζ`. The two live together on this surface.
3. The chain's vortices are inverse-square link counts and logarithmic turning, mirror images of the integers
   and half-integers; a zero's wave makes an inverse-square ladder of spiral arms on the saddle.
4. The saddle's coordinate planes are the trail's Gram planes: the x-axis plane holds the squares, the pronics
   and every cancellation curve, and is pierced by the trail at the Gram points; the y-axis plane holds the
   1:4 products and the half-Gram points. The bend is a probe, not a parameter of the arithmetic.
5. Double intersections do not restore primes, but the sieve's sign at them is the Möbius function, whose running
   total is the Mertens function and whose series is `1/ζ`: that is the honest form of "double cancellation".
6. The vortex eyes of the chain at a zero are the chain's first vertices reflected through twice the Gram
   phase; the first eye's helix through the zero planes is the Gram clock, and its phase slips carry only the
   weak next-gap correlation of zero repulsion (−0.28).
7. Every curve on the spiral is a quadratic, and far out it is a straight line whose direction is `B/2q` and whose
   distance from its ray is `−πRΔ/4q²`. Off-axis planes read the quadratics in order of discriminant. Their prime
   yield is the Hardy–Littlewood constant of `Δ` (correlation 0.9995): zero exactly when `Δ` is a square, highest
   at the class-number-one discriminants, and just as high for `Δ > 0` between the cancellation curves as beyond
   them.
8. The mirror across the squares ray turns the cancellation curves into un-cancellation curves. One pair through a
   number certifies a prime power (Euler's criterion), and two pairs factor it. On the cancellation side, Fermat's
   method finds the one curve through a composite, in steps that grow with that curve's distance from the ray.
9. Any two curves meet once per factorization of the difference of their discriminants. Euler's line and
   `k² + 1` meet only at the primes 197 and 1601.
10. Tilted sections are hyperbolas whose centres circle the axis; tangent sections are the saddle's straight lines,
    the homes of the one-to-sixteen quadratics. Twisting the saddle is folding it, as far as the spiral can tell, and
    the divisor function's wobble is a sum of twisted saddles.
11. Pinching the spiral clusters the primes onto curves exactly as a sieve by the primes the pinch sees: residue
    arms, pleated slot curves and lattice cells all reach the same ceiling, Π p/(p − 1), which grows like
    `e^γ log P`. The fold merges the two half-combs into one comb of discriminants with a period-8 stripe from the
    prime 2, and the pleat shows why the Euler lines live on the pronic side. The brightest pinched curve always
    keeps composites built from primes it cannot see.
12. With 144 = 12 × 12 as a basis, every curve becomes a straight line on a 12 × 12 torus, and its prime density is a
    count of allowed cells: 3, 2, 1 or 0 for the pronic side by `c mod 12`, at most 1.5 for the squares side. Pinched
    to 24 or 16 arms, the saddle's height computes the Legendre symbols (3/p) and (2/p) exactly. All of this is the
    local behaviour at 2 and 3, and no grouping by 144 can see a larger prime.
13. Mirrors reverse the roles of the curves. The pronic ray reflects 2.15 times as many Goldbach pairs as Euler's
    line, as Hardy and Littlewood's singular series predicts to within 1.5 %. The twisted tori (a torus knot of the
    residues mod 35, the umbilic torus sorting the turns mod 3) sort numbers by residues and add no sieving power. Lehmer's
    ring sieve factors 1 501 and 50 399 at the first candidate that passes every ring.
14. Every curve is a slice of the paraboloid `x² + y²` (definite: anti-cancellation side) or the saddle `x² − y²`
    (indefinite: cancellation side and the prime-carrying Δ > 0 curves), and the two meet along the squares. On the
    modular surface the same curves are points, closed geodesics and geodesics into the cusps. The Sacks ring of a prime
    meets the square and triangular lattices in a pattern fixed by p mod 12, and the prime race between those patterns
    is driven by the zeros of three L-functions.
15. None of this predicts a prime or a zero that the sieve, the explicit formula or the functional equation does
    not already predict. The algorithms that do come out of the sections (Euler's criterion, Fermat's method, the
    sieve along a line) are classical. What the sections add is one geometry in which they are all the same
    statement about quadratics and their discriminants.

## Surprises, and what is worth exploring next

**What was surprising.**

1. *The un-cancellation idea has an exact classical form, with the logic inverted.* On the mirror side of the
   squares ray a **single** pair of lines through a number proves it prime, and a **second** pair proves it composite
   and hands over a factor (Euler). The test works only for Euler's 65 idoneal numbers: for `N = 11` it fails 1 047
   times below 20 000. Whether intersections decide primality is a question about class groups.
2. *Which side of the ray a line lies on says nothing about its primes.* The visual split into a composite side and
   a prime side is wrong. Lines between the cancellation curves (`Δ > 0`, not a square) are as prime-rich as those
   beyond the ray: `k² + k − 1` beats Landau's `k² + 1` by 2.5 to 1. Only a square discriminant, or a fixed prime
   divisor such as 2, empties a line. At only `k ≤ 3 000` the Hardy–Littlewood constant already predicts each line's
   count with correlation 0.9995.
3. *1601 is a meeting point.* The tour's spiral stops at 1601, chosen as the last prime of Euler's run. Euler's line
   and Landau's `k² + 1` share exactly two values, 197 and 1601.
4. *The curves without primes are the best mirrors for pairs of primes.* Mirrors on the pronic ray reflect 2.15 times
   as many Goldbach pairs as mirrors on Euler's line (section Z1). The prime-free curves are built from small factors,
   and a small factor of the mirror can spoil a pair in only one way.
5. *The floor of the modular fundamental domain is a cancellation curve.* The arc `|τ| = 1` is the geodesic of
   `k² − 1 = (k − 1)(k + 1)`, and Landau's point `i` and the point `ρ` of `k² + k + 1` stand on it (section AD).

**Worth exploring.** The first two are now sections P and Q above.

1. **The circle problem lives on the half-twisted saddle.** The number of lattice points in a circle of radius `√x`
   wobbles around `πx`. Hardy's formula writes that wobble as `(x^¼/π) Σ r₂(n) n^(−3/4) cos(2π√(nx) − 3π/4)`, which
   reproduces it with correlation 0.987 at 3 000 terms. Its first harmonic runs at **one** wave per turn of the Sacks
   spiral, the half-twisted surface of section O, against the divisor problem's two. So two pairings appear. The
   cancellation curves count divisors and go with the plain saddle. The mirror lines count sums of two squares and
   go with the half-twist. Rendering both Voronoi sums as stacks of twisted surfaces would put the two lattice-point
   problems side by side.
2. **L-function zeros along a single line.** The crossings of the cancellation curves with a line of discriminant
   `Δ` happen at the primes that split in `Q(√Δ)`. Those primes are governed by the zeros of the Dirichlet
   L-function `L(s, χ_Δ)`, as the primes are by the zeros of `ζ`. A master wave built from the zeros of `L(s, χ_{−163})`
   and run along Euler's line should spike at its crossings. That would join the zeta half of the project to the
   quadratic half.
3. **The class group as families of curves.** For a discriminant with class number `h` there are `h` reduced forms,
   and so `h` families of mirror lines. Drawing all of them would show why Euler's one-pair test works only when each
   genus holds one class, and the class number would appear as a count of curve families on the saddle.

## Explainer modules

The side menu's **Experiments** section plays four directed modules built from these experiments: *Cancellation
curves* (section A), *Level contours* (H10), *The bend as a probe* (H11, with the bend animated from flat to twice
the tour's while a plane at height ±15 sweeps the curves) and *Mirrors, not oracles* (section I). Their lines live
in [`experiments.json`](../../Assets/Resources/Narration/experiments.json); the actions and camera cues are in
[`ExperimentTour.cs`](../../Assets/Tour/ExperimentTour.cs) and `CameraRig.cs`. They are narrated with the tour's
ElevenLabs voice (JohnV4) and record as one vertical video each with
`Tools/render_chapters.py --script experiments --shapes vertical`.

Four more modules, now narrated as well, cover the second round: *Two counting problems*
(section P: the divisor and circle wobbles as charts, their waves added one by one, then the saddle twisting from two
waves per turn to one while the squares slide onto the crests), *Primes of Euler's ring* (section Q: needles built
from ζ's zeros, then the L-function's zeros joining so the inert primes sink and the split primes double), *Mirror
lines* (section L: `k² − m²` swinging into `k² + m²`, 1601 proved prime by one pair, 221 and 1105 factored by two,
Fermat's walk out to 1501) and *The discriminant comb* (section K: a plane sliding across the comb with its prime
count, the yield of every plane, Euler's and Landau's lines meeting at 197 and 1601). Their shots use `fit`, so they
frame the same subject in 16:9 and 9:16.

The four 144 modules are scripted, directed, animated and narrated with the same voice.
*The 12 × 12 torus* (section V) marks numbers with their turn and step, fills the board as the primes up to ten
million arrive (only 2 and 3 land off the 48 gold cells, glowing), sweeps the diagonals across it cell by cell (white
flares on gold cells, red rings on the others) and rolls the board into a torus. *The saddle computes (3/p)*
(section W) pinches the spiral to 24 arms and raises the saddle so every prime falls or rises by (3/p), then repeats
it for (2/p), (5/p) and 144 arms on a 12-fold saddle. *Base-12 fingerprints* (section X) stamps curves onto the board
of last two base-12 digits one value at a time, with each value written in base 12. *The 144 clock* (section Y)
reveals the 24 half-hours with their product families, pulses the prime-free hours and lights `16k² + 11k + 2`
beside hour 9.

Three modules cover the twisted shapes, narrated with the same voice. *Mirrors for prime pairs* (section Z1) draws the
Goldbach chords mirror by mirror on the pronic ray and then on Euler's line, with the running count on the caption
board. It then shows the mirror strength of five curves against their predictions and the chords arriving on the
Möbius band. *The sieve machine* (section Z2) turns the dial for 1 501 and for 50 399, lays a white ring over the ring
for 19 that never closes, and ends on the record of every candidate. *Twisted tori* (sections Z3 and Z4) walks the
torus knot with a flare at every prime, winds the spiral onto the umbilic torus turn by turn, lights the multiples of 3
of `k² + k + 1` and `k² + k + 3` on whole stacks, and unwinds the twist for contrast. Their counts and status lines are
on the caption board at the top of the screen, so they read at any camera angle.

Five modules cover the intersecting geometries (sections AA–AE), narrated with the same voice.

* *Two norm surfaces* (AA) draws the bowl and the saddle meeting along the squares, then lays each curve on as a slice,
  with its discriminant and prime count on the caption board: Euler's line, `k² + k + 17` and Landau's `k² + 1` on the
  bowl, the factoring half-integer slices and `k² + k − 1` on the saddle. It ends on the level sets of 13 and 65, with
  four and eight factor points on the saddle and eight and sixteen square points on the bowl.
* *Rings through lattices* (AB) lays the square and triangular lattices under the rings of 13, 17, 19 and 23 (one prime
  for each class mod 12), then under 1105 and 1729. It then draws Euler's lattice one ring at a time: forty primes, then
  `41²`.
* *A twelve-dimensional torus* (AC) recolours the 144 shadows panel by panel while Euler's line, then the pronic
  numbers, pass through them, with the count of cancellation crosses hit on the board. It closes up on the 36 shadows
  of 2 to 13.
* *The modular surface* (AD) brings in the points, then their names and j-invariants, then the geodesics. It zooms to
  the fundamental domain for the closed geodesic of `k² + k − 1` and the cusp geodesic of `k(k + 1)`.
* *The lattice race* (AE) draws the race mod 4 and its rebuild from the zeros of Dirichlet's β live, flaring at
  26 861, 616 841 and 12 306 137. The race mod 12 follows, with its near miss at 48 231 785.

## Rendering

    ZETA_OUT=/some/dir Unity -batchmode -projectPath . -runTests -testPlatform PlayMode \
        -testFilter ExperimentsRenderTest -testResults /some/dir/results.xml -logFile /some/dir/run.log

`ZETA_EXP_ONLY=AH` renders only the named sections; the lowercase letters `m`, `l`, `k` and `t` are Z1 to Z4, `a` to
`e` are AA to AE, `f` to `i` are BA to BD, and `n` to `r` are CA to CE. Sections AA–AE, BA–BD and CA–CE are drawn for full
horizontal frames: run them with `ZETA_EXP_WIDE=1` (1920 × 1080; the copies in this folder are 960 × 540). The frames are 1080 × 1920; the copies in this folder are
scaled to 540 × 960. All numbers quoted above were recomputed in Python before the pictures were drawn
(product-curve crossings by integer arithmetic, the Gram statistics with the Riemann–Siegel theta function,
Voronoi's sum with 3 000 terms, the Möbius counts directly).
