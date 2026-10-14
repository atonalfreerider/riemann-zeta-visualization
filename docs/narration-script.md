# The Square-Root Key: a machine for manufacturing primes — narration script

Every line below is one caption/narration unit. The tour highlights each word in yellow as it is spoken.
Camera cues and scene actions are listed for reference; see docs/TOUR.md for the vocabulary.

## 1. The Prime Sieve
*Building blocks with no pattern*

**hook_01** — Come along on a discovery. Prime numbers: two, three, five, seven, eleven. They build every other number, yet nobody has found the pattern in how they arrive.
  <sub>camera `primes_grid` · actions `reset pause hide:spiral hide:outaxes hide:imaxis hide:polelabel primes:on primes:build`</sub>

**hook_02** — In 1994 Robert Sacks, out of pure curiosity, sketched the counting numbers on his notepad as a spiral. Let's do the same: break the grid open and let every number find its place. Only the two stays lit, our first prime.
  <sub>camera `sieve` · actions `sacks:sievebegin`</sub>

**hook_03** — Two times two. Count two dots. The two nearer the centre is the multiplier: it copies the row one level up, the copy slides along the Sacks spiral, and its final dot lands on four. Looking back, nothing landed on three, so three is prime.
  <sub>camera `sieve` · actions `sacks:sieveto:4`</sub>

**hook_04** — Two times three lands on six, so five is prime. Two times four lands on eight, so seven is prime. Three times three, three rows of three, lands on nine.
  <sub>camera `sieve` · actions `sacks:sieveto:9`</sub>

**hook_05** — Every new prime joins the multipliers. On to thirty: eleven, thirteen, seventeen, nineteen, twenty three and twenty nine are never landed on. Whatever no multiplication reaches is a prime. Now let's see what Sacks saw.
  <sub>camera `sieve` · actions `sacks:sieveto:30`</sub>

## 2. The Sacks Spiral
*Bent over a saddle*

**sacks_01** — This is Sacks's spiral: every number n at a distance of root n from the centre, one full turn per square. Fill it in the way he did, and the primes stop looking random.
  <sub>camera `sacks_flat` · actions `sacks:spiral` · footnote: Sacks spiral: n at radius √n, angle 2π√n</sub>

**sacks_02** — Bend the Sacks spiral over a saddle. Composites fill the valleys, and whole routes from the centre appear: some with no primes at all, others crowded with them.
  <sub>camera `sacks_saddle` · actions `sacks:saddle sacks:composites sacks:squares sacks:routes equations:on`</sub>

**sacks_03** — In red, the squares, n equals k squared. One lands at the end of every turn, all on one ray, and a square is never prime.
  <sub>camera `route_follow` · actions `equations:squares`</sub>

**sacks_04** — In blue, n equals k times k plus one: a number times its neighbour, always even, never prime. It runs out on the opposite side from the squares.
  <sub>camera `route_follow` · actions `equations:pronic`</sub>

**sacks_05** — In green, Euler's route, n equals k squared plus k plus forty one: forty primes in a row, every one of them on this saddle.
  <sub>camera `route_follow` · actions `equations:euler`</sub>

**sacks_06** — Humans carried this search for twenty three centuries, from Euclid to Riemann. In the age of AI discovery, the last lock will be opened by humans and machines together.
  <sub>camera `sacks_routes` · actions `equations:on card:machine`</sub>

## 3. Zeno's Chain
*and a gentler rule*

**zeno_01** — Zeno's puzzle: walk half way to the wall, then half of what is left, then half again. The steps shrink so fast that you never pass two.
  <sub>camera `zeno_zoom` · actions `equations:off card:off sacks:hide primes:off hide:spiral show:zeno zeno:reset zeno:build card:zeno`</sub>

**zeno_02** — Now a gentler rule: every step is one over the square root of its number. One, one over root two, one over root three.
  <sub>camera `zeno` · actions `card:off terms:1 hide:tail show:spiral show:lengths chainfeet:on rootlabel:chain hide:center build:1:8`</sub>

**zeno_03** — Shrinking this slowly, the chain never stops: it runs off the floor forever. Hold on to that square root, exponent one half. It is the key to everything that follows.
  <sub>camera `chain_long` · actions `hide:zeno hide:lengths chainfeet:off rootlabel:off build:8:60`</sub>

## 4. The Stage
*A floor, a pole, and a dial*

**stage_01** — The chain lives on a floor, the complex plane, with a pole going up. The pole is a dial, and its reading is called t. Turn it, and every joint of the chain begins to bend.
  <sub>camera `stage` · actions `hide:spiral show:outaxes show:imaxis show:polelabel axes:sprout show:center terms:24 hide:lengths trail:thin speed:0.25 delay:5:show:spiral delay:5:play`</sub>

## 5. Turn the Dial
*The tip keeps finding the centre*

**turn_01** — Two rules. The lengths follow square roots. The bends follow logarithms: each arrow turns away from the leg before it by t times the log of n over n minus one. As the dial climbs, the turns keep winding.
  <sub>camera `spiral_angles` · actions `hide:polelabel angles:on speed:0.3 play`</sub>

**turn_02** — Why logarithms? Riemann, in 1859, let the power of n be a complex number, and a complex power does not stretch. It turns, by the logarithm of n.
  <sub>camera `spiral_angles` · actions `card:riemann` · footnote: n^(−s) = e^(−s · log n): a complex exponent rotates, and the rotation is proportional to log n</sub>

**turn_03** — The chain is endless, but the arrows we skip wind up like a spring around one centre, and one orange arrow, Euler and Maclaurin's, jumps straight to it.
  <sub>camera `spiral_curl` · actions `card:off tailghost:on card:euler show:tail flash:tail speed:0.5` · footnote: orange: Euler–Maclaurin tail N^(1−s)/(s−1) + N^−s/2 + Bernoulli terms</sub>

**turn_04** — Keep turning. The tip wanders, then dives straight into the centre.
  <sub>camera `tip_center` · actions `tailghost:off card:off show:tip speed:1.3`</sub>

**turn_05** — At fourteen point one three it hits dead centre: a collision. Stack the first twenty turns end to end: together they wind t times the log of twenty one, almost seven full turns.
  <sub>camera `tip_stack` · actions `pause jump:14.14 flash:zero landing:on angles:stack`</sub>

**turn_06** — Turn on, and the collisions never stop: twenty one, twenty five, thirty. Hardy proved there are infinitely many. These heights are the famous zeros of the Riemann zeta function.
  <sub>camera `markers_axis_wide` · actions `landing:off angles:off terms:auto trail:full card:hardy speed:4 play`</sub>

## 6. Collisions Become Waves
*One wave per collision*

**wave_01** — Start with an ordinary cosine wave and a dial, gamma, that sets its speed. Turn the dial up, and the ripples pack tighter.
  <sub>camera `wave` · actions `atleast:120 pause card:off hide:spiral hide:tail hide:tip hide:center hide:trail hide:imaxis hide:outaxes hide:zeros show:numbers wave:plain:2.4 dial:on delay:4:wave:dial:6`</sub>

**wave_02** — Put a logarithm inside, and the ripples bunch up near zero. Dial it to fourteen point one three: the first collision's wave.
  <sub>camera `wave` · actions `wave:log delay:4:wave:dial:14.134725 delay:4:dialstrip:1`</sub>

**wave_03** — The second collision is born as its own cosine, winds up, and sinks into the master wave. Where they agree the peaks reinforce; where they disagree they cancel.
  <sub>camera `wave` · actions `cosine:1 show:cosine wave:off dial:off waveview:1 cosine:build:1:2`</sub>

**wave_04** — Wave after wave, hundreds of them. What survives are sharp spikes on two, three, five, seven, eleven and thirteen, and faint ones on the squares.
  <sub>camera `spikes` · actions `cosine:build:2:all squares:on speed:6 play`</sub>

**wave_05** — A map of the primes, built from nothing but collision heights.
  <sub>camera `spikes` · actions `primes:arrows primes:highlight`</sub>

## 7. The Staircase
*Riemann's exact recipe*

**stair_01** — Riemann's recipe turns the collisions into a count of the primes, one piece at a time. Start with the straight line y equals x.
  <sub>camera `stair_terms` · actions `pause primes:noarrows hide:numbers dialstrip:clear hide:cosine hide:prime hide:zetacurve hide:zeros hide:spiral hide:tail hide:tip hide:center hide:imaxis hide:outaxes hide:equations card:riemann stairlab:terms`</sub>

**stair_02** — Subtract the logarithm of two pi, a constant shift, and a small correction from the trivial zeros that fades away.
  <sub>camera `stair_terms` · actions `card:off stairlab:const:0.45 stairlab:trivial:0.45`</sub>

**stair_03** — Now the first collision's wave. It grows like root x and oscillates with log x. On its own, it only bends the line.
  <sub>camera `stair_terms` · actions `stairlab:waves:0:1`</sub>

**stair_04** — Add the second and third waves, then hundreds more. The steps sharpen on the primes, with smaller steps on the powers of primes.
  <sub>camera `stair_terms` · actions `stairlab:waves:1:3:0.4 stairlab:waves:3:all:0.55`</sub>

**stair_05** — With enough collisions the staircase lands on the exact count: a step at every prime, as tall as its logarithm.
  <sub>camera `stair_terms`</sub>

## 8. The Square-Root Key
*Why one half, and nothing else*

**key_01** — Back to the key: every arrow was one over root n, exponent one half. Change it to zero point nine, and the tip never hits the centre. Watch the miss.
  <sub>camera `sigma_spiral` · actions `pause hide:prime hide:cosine hide:trail hide:imaxis jump:14.14 terms:auto show:spiral show:tail show:tip show:center rootlabel:sqrt miss:on delay:5:sigma:0.9 delay:5:rootlabel:0.9 delay:5:speed:2.5 delay:5:play delay:5:miss:on`</sub>

**key_02** — Picture every exponent between zero and one as a strip. Our collisions all sit on the one half line.
  <sub>camera `strip_panel` · actions `miss:off pause sigma:0.5 rootlabel:off hide:spiral hide:tail hide:tip hide:center show:strip`</sub>

**key_03** — Suppose they strayed off that line. Slide the exponent toward zero point nine, and the staircase wobbles off its rails.
  <sub>camera `stair_wobble` · actions `hide:strip hide:cosine hide:prime hide:zetacurve hide:zeros hide:spiral hide:tail hide:tip hide:center hide:imaxis hide:outaxes hide:equations stairlab:wobble:40:0.5 stairlab:sweep:0.5:0.95:pingpong` · footnote: red: the staircase if every collision had exponent β instead of ½</sub>

**key_04** — The Riemann Hypothesis says it never happens: every collision, forever, on the one half line.
  <sub>camera `stair_wobble` · actions `stairlab:beta:0.5`</sub>

## 9. 2026: A Wall at Seven Eighths
*OpenAI's quasi-Riemann hypothesis claim*

**qrh_01** — In October 2026, OpenAI published an AI generated proof that no collision can stray past seven eighths, and by symmetry none below one eighth: the quasi Riemann hypothesis.
  <sub>camera `strip_band` · actions `stairlab:off hide:prime show:strip show:band78 card:machine delay:6:show:band18` · footnote: openai/math, family 003 · manuscript Sept 30, 2026 · released Oct 6, 2026</sub>

**qrh_02** — It is machine checked in Lean, but human peer review is still under way: a strong claim, not settled fact.
  <sub>camera `strip_band` · actions `show:band1112` · footnote: Lean: riemannZeta s ≠ 0 for 7/8 < Re s · independently re-checked · not yet peer reviewed</sub>

**qrh_03** — The orange gap, from one half to seven eighths, is where the Riemann Hypothesis still lives.
  <sub>camera `strip_gap` · actions `hide:band1112 show:gap card:off`</sub>

## 10. Written Into the Universe

**outro_01** — Back to the Sacks spiral on its saddle. The primes, the most irregular objects in arithmetic, seem to follow a rule written into the numbers themselves.
  <sub>camera `sacks_saddle` · actions `pause hide:strip hide:gap stairlab:off hide:cosine hide:prime hide:zetacurve hide:zeros hide:spiral hide:tail hide:tip hide:center hide:imaxis hide:outaxes hide:equations hide:trail sacks:show sacks:saddle sacks:routes`</sub>

**outro_02** — One last picture, and this one is a guess, not a result. Let the zeta trail rise from the centre of the Sacks spiral, with a collision wherever it returns to the axis.
  <sub>camera `sacks_3d` · actions `sacks:noroutes hypothesis:grow:101.5:26` · footnote: a visual hypothesis, not a theorem</sub>

**outro_03** — Each collision's wave is born at its own height, runs out along the Sacks spiral, and sinks onto the saddle, building the master wave.
  <sub>camera `sacks_wave`</sub>

**outro_04** — From above, the master wave spikes at every prime. Dial in all ten thousand collisions, and the spikes sharpen, prime after prime.
  <sub>camera `master_top_pull` · actions `hypothesis:notrail delay:6:hypothesis:full`</sub>

**outro_05** — Saddle, trail and waves: one structure seen three ways. Prove the one half key, and we will have read one line of the instruction set. The picture is yours now. Explore it.
  <sub>camera `home_orbit` · actions `home:scene` · footnote: open the side menu (M) to explore this picture, pick a chapter or record</sub>

---
1137 words ≈ 7.6 minutes of speech at 150 wpm, plus pauses and title cards.
