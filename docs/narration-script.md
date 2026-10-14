# The Square-Root Key: a machine for manufacturing primes — narration script

Every line below is one caption/narration unit. The tour highlights each word in yellow as it is spoken.
Camera cues and scene actions are listed for reference; see docs/TOUR.md for the vocabulary.

## 1. The Prime Sieve
*What are the primes hiding?*

**hook_01** — This year, a machine produced a proof about the prime numbers, on a question mathematicians have chased since 1859. Can a machine see what we missed? Let's find out what the primes are hiding.
  <sub>camera `primes_grid` · actions `reset pause primes:on primes:build`</sub>

**hook_02** — In 1994, out of curiosity, Robert Sacks sketched the counting numbers on a notepad as a spiral. Let's do the same. Only the two stays lit: our first prime.
  <sub>camera `sieve` · actions `sacks:sievebegin`</sub>

**hook_03** — Two times two. Count two dots, and copy the row one level up. The copy slides along the spiral, and its last dot lands on four. The product overshoots three. No product will ever land there, so three is prime.
  <sub>camera `sieve` · actions `sacks:sieveto:4`</sub>

**hook_04** — Two times three lands on six, overshooting five. Two times four lands on eight, overshooting seven. Both are prime.
  <sub>camera `sieve` · actions `sacks:sieveto:9`</sub>

**hook_05** — Every new prime joins the multipliers. Up to thirty, the products overshoot eleven, thirteen, seventeen, nineteen, twenty three and twenty nine. Those are the primes.
  <sub>camera `sieve` · actions `sacks:sieveto:30`</sub>

## 2. The Sacks Spiral
*Bent over a saddle*

**sacks_01** — Now fill in Sacks's whole spiral: every number at a distance of root n from the centre, one full turn per square.
  <sub>camera `sacks_flat` · actions `sacks:spiral` · footnote: Sacks spiral: n at radius √n, angle 2π√n</sub>

**sacks_02** — Bend it over a saddle, and routes appear. Some never hold a prime. Others are crowded with them.
  <sub>camera `sacks_saddle` · actions `sacks:saddle sacks:composites sacks:squares sacks:routes equations:on`</sub>

**sacks_03** — In red, the squares: n equals k squared. All on one ray, and never prime.
  <sub>camera `route_follow` · actions `equations:squares`</sub>

**sacks_04** — In blue, n equals k times k plus one: a number times its neighbour. Always even, never prime.
  <sub>camera `route_follow` · actions `equations:pronic`</sub>

**sacks_05** — In green, Euler's curve: n equals k squared plus k plus forty one. For forty steps in a row, it lands only on primes.
  <sub>camera `route_follow` · actions `equations:euler`</sub>

**sacks_06** — Robert Sacks saw that Euler's curve is one of a family: the blue line, shifted out by a number. When the shift is prime, the curve is rich in primes.
  <sub>camera `sacks_routes` · actions `equations:family pulse:sacks` · footnote: Product curves: Robert Sacks, numberspiral.com  ·  explained by Michael M. Ross, naturalnumbers.org/sparticle.html</sub>

## 3. Zeno's Chain
*and a gentler rule*

**zeno_01** — Zeno's puzzle: walk half way to a wall, then half of what is left. The steps shrink so fast that you never pass two.
  <sub>camera `zeno_zoom` · actions `show:zeno zeno:reset zeno:build card:zeno`</sub>

**zeno_02** — Now a gentler rule. Each step is one over the square root of its number.
  <sub>camera `zeno` · actions `card:off terms:1 show:spiral show:lengths chainfeet:on rootlabel:chain build:1:8`</sub>

**zeno_03** — It shrinks so slowly that the chain never stops. Hold on to that square root: the exponent one half.
  <sub>camera `chain_long` · actions `hide:zeno hide:lengths chainfeet:off rootlabel:off show:spiral build:8:60`</sub>

## 4. The Stage
*A floor, a pole, and a dial*

**stage_01** — The chain lives on a floor, the complex plane, with a pole going up. The pole is a dial called t. Turn it, and every joint begins to bend.
  <sub>camera `stage` · actions `show:outaxes show:imaxis show:polelabel axes:sprout show:center terms:24 hide:lengths trail:thin speed:0.25 delay:5:show:spiral delay:5:play`</sub>

## 5. Turn the Dial
*The tip keeps finding the centre*

**turn_01** — Each arrow turns away from the one before by t times the log of n over n minus one. As the dial climbs, the turns keep winding.
  <sub>camera `spiral_angles` · actions `angles:on speed:0.3 play` · footnote: n^(−s) = e^(−s · log n): a complex exponent rotates, and the rotation is proportional to log n</sub>

**turn_02** — The arrows we leave out wind into a spring around one point, and one orange arrow jumps straight to it.
  <sub>camera `spiral_curl` · actions `tailghost:on show:tail flash:tail card:euler speed:0.5` · footnote: orange: Euler–Maclaurin tail N^(1−s)/(s−1) + N^−s/2 + Bernoulli terms</sub>

**turn_03** — Keep turning. The tip wanders, then dives into the centre.
  <sub>camera `tip_center` · actions `tailghost:off card:off show:tip speed:1.3`</sub>

**turn_04** — At fourteen point one three it hits dead centre: a collision. Laid end to end, the first twenty turns wind almost seven times around.
  <sub>camera `tip_stack` · actions `pause jump:14.14 flash:zero landing:on angles:stack`</sub>

**turn_05** — The collisions never stop. These heights are the famous zeros of the Riemann zeta function.
  <sub>camera `markers_axis_wide` · actions `landing:off angles:off terms:auto trail:full speed:4 play`</sub>

## 6. Collisions Become Waves
*One wave per collision*

**wave_01** — Every collision becomes a wave. Start with a cosine, and a dial that sets its speed.
  <sub>camera `wave` · actions `show:numbers wave:plain:2.4 dial:on delay:3:wave:dial:6`</sub>

**wave_02** — Bend it with a logarithm, and dial it to fourteen point one three: the first collision's wave.
  <sub>camera `wave` · actions `wave:log delay:4:wave:dial:14.134725 delay:4:dialstrip:1`</sub>

**wave_03** — Each new collision adds its own wave. Where they agree, they reinforce. Where they disagree, they cancel.
  <sub>camera `wave` · actions `cosine:1 show:cosine wave:off dial:off waveview:1 cosine:build:1:6`</sub>

**wave_04** — Hundreds of waves later, what survives are sharp spikes.
  <sub>camera `spikes` · actions `cosine:build:6:all speed:6 play`</sub>

**wave_05** — They sit on two, three, five, seven, eleven and thirteen: the primes. Fainter spikes mark the squares, four, nine and sixteen.
  <sub>camera `spikes_close` · actions `squares:on primes:arrows primes:highlight`</sub>

## 7. The Staircase
*Riemann's exact recipe*

**stair_01** — Riemann's recipe turns the collisions into a count of the primes. Start with the line y equals x.
  <sub>camera `stair_terms` · actions `card:riemann stairlab:terms`</sub>

**stair_02** — Shift it down by the log of two pi, and add a small correction that fades away.
  <sub>camera `stair_terms` · actions `card:off stairlab:const:0.45 stairlab:trivial:0.45`</sub>

**stair_03** — Now add the first collision's wave. On its own, it only bends the line.
  <sub>camera `stair_terms` · actions `stairlab:waves:0:1`</sub>

**stair_04** — Add hundreds more, and the line breaks into steps: one at every prime.
  <sub>camera `stair_terms` · actions `stairlab:waves:1:3:0.35 stairlab:waves:3:all:0.6`</sub>

## 8. The Square-Root Key
*Why one half, and nothing else*

**key_01** — Back to the square root: exponent one half. Change it to zero point nine, and the tip never reaches the centre. It misses.
  <sub>camera `sigma_spiral` · actions `jump:14.14 terms:auto show:spiral show:tail show:tip show:center rootlabel:sqrt miss:on delay:5:sigma:0.9 delay:5:rootlabel:0.9 delay:5:speed:2.5 delay:5:play delay:5:miss:on`</sub>

**key_02** — Every collision we know sits exactly on the one half line.
  <sub>camera `strip_panel` · actions `miss:off pause sigma:0.5 rootlabel:off hide:spiral hide:tail hide:tip hide:center show:strip`</sub>

**key_03** — Move them off it, and the staircase wobbles away from the primes.
  <sub>camera `stair_wobble` · actions `hide:strip stairlab:wobble:40:0.5 stairlab:sweep:0.5:0.95:pingpong` · footnote: red: the staircase if every collision had exponent β instead of ½</sub>

**key_04** — The Riemann Hypothesis says that never happens. Every collision, forever, on the one half line.
  <sub>camera `stair_wobble` · actions `stairlab:beta:0.5`</sub>

## 9. 2026: A Wall at Seven Eighths
*OpenAI's quasi-Riemann hypothesis claim*

**qrh_01** — In October 2026, OpenAI published an AI generated proof that no collision strays past seven eighths, or below one eighth.
  <sub>camera `strip_band` · actions `show:band78 card:machine delay:5:show:band18` · footnote: openai/math, family 003 · manuscript Sept 30, 2026 · released Oct 6, 2026</sub>

**qrh_02** — It is checked by machine, but not yet by human review. And the gap from one half to seven eighths is where the hypothesis still lives.
  <sub>camera `strip_gap` · actions `show:band1112 show:gap card:off` · footnote: Lean: riemannZeta s ≠ 0 for 7/8 < Re s · independently re-checked · not yet peer reviewed</sub>

## 10. Written Into the Universe

**outro_01** — Back to Sacks's spiral on its saddle.
  <sub>camera `sacks_saddle` · actions `sacks:show sacks:saddle sacks:routes`</sub>

**outro_02** — One last picture: a guess, not a result. Let the zeta trail rise from the centre of the spiral.
  <sub>camera `sacks_3d` · actions `sacks:noroutes hypothesis:grow:101.5:24` · footnote: a visual hypothesis, not a theorem</sub>

**outro_03** — Each collision sends its wave along the spiral, and the waves settle onto the saddle as one master wave.
  <sub>camera `sacks_wave`</sub>

**outro_04** — From above, the master wave spikes at every prime. With all ten thousand collisions, the spikes grow sharp.
  <sub>camera `master_top_pull` · actions `hypothesis:notrail delay:6:hypothesis:full`</sub>

**outro_05** — A machine may have pushed the wall to seven eighths. Proving one half is still open, to humans and machines alike. The picture is yours. Explore it.
  <sub>camera `home_orbit` · actions `home:scene` · footnote: open the side menu (M) to explore this picture, pick a chapter or record</sub>

---
796 words ≈ 5.3 minutes of speech at 150 wpm, plus pauses and title cards.
