# Exploration snapshots: interference on the saddle

Portrait snapshots rendered headlessly by `ExplorationRenderTest` (set `ZETA_OUT`, run with
`-testFilter ExplorationRenderTest`). All use the first 30 zeros (60 in one case).

| file | what it shows |
|---|---|
| 01 / 02 master-*-top | the master wave −Σ cos(γ·log n) riding the Sacks spiral on the saddle, seen from above the centre; spikes reach the yellow prime dots |
| 04 master-center-oblique | the same with the zeta trail rising from the centre |
| 06 ringfield-center-top | the same sum swept around the centre as rings: ridges are circles through the primes |
| 08 layers-side | every wave kept at its own height γ·0.25 above the saddle |
| 10 / 11 layersection-* | each wave alone along a straight ray, at its own height: the fringe pattern of cos(γ·log n) in the (log n, γ) plane, crests on the curves γ·log n = 2πm |
| 12 / 15 sumsection-* | cumulative sums S_k along a ray, stacked by k (blue → yellow): columns grow over the prime ticks as k increases |
| 16 partialsums-spiral-oblique | the cumulative sums along the spiral, stacked by k |

Reading (see the conversation notes in the commit message): the sum depends on n only, so on the saddle it is
purely radial; the saddle's angular shape cannot add information about the primes. The one genuinely
two-dimensional structure is the fringe family γ·log n = 2πm in the layer sections.

## Second set: connections between the saddle, the Sacks spiral, the chain, the trail and the waves

| file | what it shows |
|---|---|
| 20 / 21 saddle-wire | the saddle z = (x² − y²)/k as a polar wireframe under the spiral: the ridge (x axis, orange) carries the numbers with √n ≈ whole or half (squares k², pronics k(k+1), never prime); the valley (y axis, blue) the numbers with √n ≈ ¼ or ¾ |
| 22 / 23 / 24 chain-field | the chain's arrows placed on the spiral for a given dial t: arrow n has length 1/√n and direction −t·log n; its x-component is the wave cos(t·log n). The field rotates quickly near the centre and ever more slowly outward (rate t/n per step) |
| 25 / 26 / 27 explicit-wave | the explicit-formula weights instead of plain cosines: 4√n·Σ(cos + 2γ sin)/(1 + 4γ²) = n − ψ(n) − ln 2π riding the spiral, a sawtooth that drops by log p at each prime (read it from the oblique shot; the displacement is along the normal) |
| 28 – 31 fringe-sheet | the fringe sheet cos(t · log n) standing on the ridge: n along the radius (placed at √n so it matches the spiral), t up the axis. Yellow rows are the zeros. Their sum is the cyan master wave along the bottom (spikes on the prime ticks). The columns summed over n with weight 1/√n are ζ(½ + it): |ζ| is the white curve up the left edge and touches zero exactly at the yellow rows |
| 32 sheet-on-saddle | the sheet, the master wave, the trail and the saddle wireframe together |

### Reading

- **Chain = waves.** The cosine wave for dial t is the x-component of the chain's arrow at that number. The fringe sheet is the whole family at once: rows are waves, columns are chain arrows.
- **Trail = column sums, primes = row sums.** Summing the sheet down a column (over n, weight 1/√n) gives the zeta trail, whose returns to the axis pick the yellow rows. Summing the yellow rows across gives the master wave with its spikes on the primes. The explicit formula is this one sheet read in the two directions.
- **Fringes.** The crests of the sheet lie on t·log n = 2πm: hyperbolas in (n, t), bending toward the axis. The zero rows cut them where the column sums cancel.
- **Saddle = the other clock.** The Sacks angle 2π√n and the chain phase t·log n are two independent coordinates of n. The saddle's ridge/valley encodes √n mod 1 (where squares and pronics live); the waves depend only on log n and are constant on circles. That is why the prime ridges (circles) cross the saddle's ridges at right angles and why the saddle, as drawn, cannot carry extra information about the zeros. The visual unity is real; the two clocks are not linked by any identity we know.
