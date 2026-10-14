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
