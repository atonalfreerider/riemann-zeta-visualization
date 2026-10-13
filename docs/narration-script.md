# The Square-Root Key: a machine for manufacturing primes — narration script

Every line below is one caption/narration unit. The tour highlights each word in yellow as it is spoken.
Camera cues and scene actions are listed for reference; see docs/TOUR.md for the vocabulary.

## 1. The Primes
*Building blocks with no pattern*

**hook_01** — Prime numbers. Two, three, five, seven, eleven. They are the building blocks of every other number, yet they arrive with no pattern anyone can predict.
  <sub>camera `sieve` · actions `reset pause hide:spiral hide:outaxes hide:imaxis hide:polelabel primes:on primes:build delay:3:sacks:sieve:52`</sub>

**hook_02** — Break the grid open and let every number find its place on the spiral. Now multiply. Two dots leave the two along one axis, two more along the other: a square of four dots. Count them along the spiral road, and they land on four, a perfect square.
  <sub>camera `sieve`</sub>

**hook_02b** — Two dots from the two, three dots from the three: six dots in a rectangle, counted along the road to six. Then two by five makes ten dots, two by seven makes fourteen.
  <sub>camera `sieve`</sub>

**hook_03** — Three by three: nine dots, and the road ends on the next perfect square. Three by five, three by seven, then five by five. Every composite is a rectangle of dots; every square is a square of dots, in orange.
  <sub>camera `sieve` · actions `pulse:primes`</sub>

**hook_04** — What survives the sieve is the primes. Now fill in the whole spiral, each number a little farther out, one full turn per square, and the primes stop looking random.
  <sub>camera `sacks_flat` · actions `sacks:spiral` · footnote: Sacks spiral: n at radius √n, angle 2π√n</sub>

**hook_05** — Bend that spiral over a saddle. The composites fill the valleys, the squares line up along one ridge, and the pattern leaps out: whole routes from the centre with no primes at all, and other routes crowded with them.
  <sub>camera `sacks_saddle` · actions `sacks:saddle sacks:composites sacks:squares sacks:routes pulse:sacks`</sub>

**hook_06** — Along the squares, never a prime. Along Euler's route, forty primes in a row. Humans carried this search for twenty three centuries: Euclid, Euler, Riemann, Hardy.
  <sub>camera `sacks_routes` · actions `pulse:sacks`</sub>

**hook_07** — Now we are in the age of agentic AI mathematical discovery. The last lock will be opened by humans and machines working together. This is the story of a square root, a dial, a spiral, and a staircase.
  <sub>camera `sacks_routes` · actions `card:machine`</sub>

## 2. Zeno's Chain
*and a gentler rule*

**zeno_01** — Zeno's old puzzle. Step one: walk half way to the wall. Step two: half of what is left. Step three: half again. The footsteps keep their shape, only the scale shrinks, forever, and you never pass two.
  <sub>camera `zeno_zoom` · actions `card:off sacks:hide primes:off hide:spiral show:zeno zeno:reset zeno:build card:zeno`</sub>

**zeno_02** — Lay the steps out as arrows: one, a half, a quarter, an eighth. The chain shrinks fast and stops dead at two.
  <sub>camera `zeno` · actions `pulse:zeno`</sub>

**zeno_03** — Now a gentler rule, step by step. Step one: length one. Step two: one over the square root of two. Step three: one over root three. Every footstep is one over the square root of its number. Hold on to that square root.
  <sub>camera `zeno` · actions `card:off terms:1 hide:tail show:spiral show:lengths chainfeet:on rootlabel:chain hide:center build:1:8 pulse:spiral`</sub>

**zeno_04** — Shrinking this slowly, the chain never stops. Sixty arrows reach fourteen. A million reach two thousand. It runs off the floor forever.
  <sub>camera `chain_long` · actions `hide:zeno hide:lengths chainfeet:off rootlabel:off build:8:60`</sub>

**zeno_05** — Hold on to that square root. The exponent one half is the key to everything that follows, and we will come back to it at the end.
  <sub>camera `chain_long` · actions `pulse:spiral`</sub>

## 3. The Stage
*A floor, a pole, and a dial*

**stage_01** — The chain lives on a flat floor with a pole going up. Mathematicians call the floor the complex plane. For us it is just a floor, with a grid: so many steps right, so many steps back.
  <sub>camera `stage` · actions `hide:spiral show:outaxes show:imaxis show:polelabel axes:sprout pulse:floorgrid`</sub>

**stage_02** — The pole is a dial. Its reading is called t. Turn it, and watch: every joint in the chain begins to bend.
  <sub>camera `stage` · actions `show:center terms:24 show:spiral hide:lengths trail:thin speed:0.25 play pulse:imaxis`</sub>

## 4. Turn the Dial
*The tip keeps finding the centre*

**turn_01** — Two rules, side by side. The lengths follow square roots: one over root n. The bends follow logarithms: at every joint, the dial reading times the log of its number.
  <sub>camera `spiral_angles` · actions `hide:polelabel angles:on speed:0.3 play pulse:angles`</sub>

**turn_01b** — Here is that sequence of bends. Log of two over one, log of three over two, log of four over three. Each bend is smaller than the last, and all of them grow together as the dial turns.
  <sub>camera `spiral_angles` · actions `logseq:on pulse:logseq`</sub>

**turn_01c** — Why logarithms? Euler wrote each term as a power of n. Riemann, in 1859, let that power be a complex number, and a complex power does not stretch, it turns: by the logarithm of n.
  <sub>camera `spiral_angles` · actions `card:riemann logseq:off` · footnote: n^(−s) = e^(−s · log n): a complex exponent rotates, and the rotation is proportional to log n</sub>

**turn_02** — The chain is endless. Watch the arrows we skip: they wind up like a spring, every turn pointing toward one centre. That centre is where the endless chain truly ends.
  <sub>camera `spiral_curl` · actions `angles:off card:off tailghost:on card:euler speed:0.5 pulse:tail` · footnote: faint spring: the next 120 arrows we do not draw</sub>

**turn_03** — One orange arrow jumps straight to it. Euler and Maclaurin's formula pins that point down exactly, to a trillionth.
  <sub>camera `spiral_curl` · actions `show:tail flash:tail pulse:tail` · footnote: orange: Euler–Maclaurin tail N^(1−s)/(s−1) + N^−s/2 + Bernoulli terms</sub>

**turn_04** — Now the tip is exact. Keep turning. The tip wanders, then dives straight into the centre.
  <sub>camera `tip_center` · actions `tailghost:off card:off show:tip speed:1.3`</sub>

**turn_05** — At a dial reading of fourteen point one three it hits dead centre. We drop a marker on the pole at that height and call it a collision.
  <sub>camera `tip_center` · actions `pause jump:14.14 flash:zero pulse:zeros`</sub>

**turn_06** — Twenty one. Twenty five. Thirty point four. The collisions never stop. In 1914 Hardy proved there are infinitely many on this pole; ten thousand are loaded here.
  <sub>camera `markers_axis` · actions `terms:auto trail:thin card:hardy speed:3 play`</sub>

**turn_07** — Here is the tip's whole path, threading the centre again and again. These collision heights are the famous zeros of the Riemann zeta function.
  <sub>camera `markers_axis_wide` · actions `trail:full card:riemann speed:6 pulse:trail`</sub>

## 5. Collisions Become Waves
*One wave per collision*

**wave_01** — Start with an ordinary cosine wave, and a dial, gamma, that sets its speed. The yellow mark on the dial is the first collision height.
  <sub>camera `wave` · actions `pause card:off hide:spiral hide:tail hide:tip hide:center hide:trail hide:imaxis hide:outaxes hide:zeros show:numbers wave:plain:2.4 dial:on pulse:dial`</sub>

**wave_02** — Turn the dial up and the ripples pack tighter. The number inside the cosine is the only thing changing.
  <sub>camera `wave` · actions `wave:dial:6`</sub>

**wave_03** — Now bend it. Put a logarithm inside, and the ripples bunch up near zero and widen as x grows.
  <sub>camera `wave` · actions `wave:log`</sub>

**wave_04** — Dial it up to fourteen point one three. This is the first collision's wave, and its dial goes on the shelf at the top.
  <sub>camera `wave` · actions `wave:dial:14.134725 dialstrip:1`</sub>

**wave_05** — Now the second collision, in its own colour. It is born as a plain cosine, and as its dial turns it winds up like a spring, the ripples forced to the left. Then it sinks into the master wave: where they agree the peaks reinforce, where they disagree they cancel.
  <sub>camera `wave` · actions `cosine:1 show:cosine wave:off dial:off waveview:1 cosine:build:1:2`</sub>

**wave_06** — Keep going. Each new dial winds up its own wave and pours it into the master wave, and the view widens to make room. Wave by wave, the pattern cancels itself out.
  <sub>camera `wave` · actions `cosine:build:2:10`</sub>

**wave_07** — Hundreds of waves. What survives are sharp spikes, and they sit on two, three, five, seven, eleven, thirteen. Notice the squares, four, nine, sixteen, twenty five: faint spikes at first, muted as the waves pile up.
  <sub>camera `spikes` · actions `cosine:build:10:all squares:on speed:6 play`</sub>

**wave_08** — The spikes sit exactly on two, three, five, seven, eleven, thirteen. A map of the primes, built from nothing but collision heights.
  <sub>camera `spikes` · actions `primes:arrows primes:highlight pulse:numbers`</sub>

## 6. The Staircase
*Riemann's exact recipe*

**stair_01** — Riemann's exact recipe turns the map into a count. Start from the straight line y equals x, and subtract one weighted wave per collision. Watch the line bend into steps.
  <sub>camera `staircase_close` · actions `pause primes:noarrows hide:numbers dialstrip:clear hide:cosine show:prime stair:build card:riemann`</sub>

**stair_02** — Each weight is fixed by the recipe. Nothing is tuned by hand.
  <sub>camera `staircase_close` · actions `show:equations pulse:prime` · footnote: ψ(x) = x − ln 2π − ½ ln(1 − x^-2) − 4√x Σ (cos(γ log x) + 2γ sin(γ log x)) / (1 + 4γ²)</sub>

**stair_03** — The result is a staircase. Every step is a prime: two, three, five, seven, eleven, thirteen. The red markers find them on their own. The squares and cubes are here too, as small orange steps.
  <sub>camera `staircase` · actions `hide:equations primepowers:on speed:8 play pulse:primepowers`</sub>

**stair_04** — More collisions, sharper steps. With a few hundred of them the staircase counts the primes exactly.
  <sub>camera `staircase_wide` · actions `primepowers:off card:off pulse:prime`</sub>

## 7. The Whole Machine

**machine_01** — Now see it all at once. The chain shrinks by the square root rule. The dial turns. The tip strikes the centre, and every strike drops a marker on the pole.
  <sub>camera `machine` · actions `show:spiral show:tail show:trail show:zeros show:imaxis show:cosine cosine:all speed:4 play pulse:zeros`</sub>

**machine_02** — Every marker becomes a wave, the waves pile up on the primes, and the staircase counts them. A machine for manufacturing prime numbers.
  <sub>camera `machine` · actions `pulse:prime`</sub>

## 8. The Square-Root Key
*Why one half, and nothing else*

**key_01** — Back to the key. Every arrow was one over the square root of n: exponent one half.
  <sub>camera `sigma_spiral` · actions `pause hide:prime hide:cosine hide:trail hide:imaxis jump:14.14 terms:auto show:spiral show:tail show:tip show:center rootlabel:sqrt pulse:spiral`</sub>

**key_02** — Change the exponent to zero point nine and watch. The spiral is tighter, and the tip never hits the centre. No collisions at all.
  <sub>camera `sigma_spiral` · actions `sigma:0.9 rootlabel:0.9 speed:2.5 play`</sub>

**key_03** — Picture every possible exponent between zero and one as a strip. Our collisions all sit on the one half line, right down the middle.
  <sub>camera `strip_panel` · actions `pause sigma:0.5 rootlabel:off hide:spiral hide:tail hide:tip hide:center show:strip pulse:strip`</sub>

**key_04** — Suppose the collisions strayed off that line. Slide the exponent up toward zero point nine and back, and watch the staircase wobble off the rails.
  <sub>camera `wobble` · actions `hide:strip show:prime ghost:all:0.9 ghost:sweep:0.55:0.95:pingpong pulse:ghost` · footnote: red: the staircase if every collision had exponent β instead of ½</sub>

**key_05** — The Riemann Hypothesis says it never happens: every collision, forever, exactly on the one half line. That is why the square root is the magic key.
  <sub>camera `wobble` · actions `ghost:off pulse:prime`</sub>

## 9. 2026: A Wall at Seven Eighths
*OpenAI's quasi-Riemann hypothesis claim*

**qrh_01** — In October 2026, OpenAI published an AI generated proof that no collision can stray past seven eighths. They call it the quasi Riemann hypothesis.
  <sub>camera `strip_band` · actions `hide:prime show:strip show:band78 card:machine pulse:band78` · footnote: openai/math, family 003 · manuscript Sept 30, 2026 · released Oct 6, 2026</sub>

**qrh_02** — By symmetry, nothing below one eighth either. The blue bands are a wall: no collision can live there.
  <sub>camera `strip_band_sym` · actions `show:band18 pulse:band18`</sub>

**qrh_03** — The core theorem is machine checked in Lean, and a companion proof reaches eleven twelfths. Human peer review is still under way, so call it a strong claim, not settled fact.
  <sub>camera `strip_band` · actions `show:band1112 pulse:band78` · footnote: Lean: riemannZeta s ≠ 0 for 7/8 < Re s · independently re-checked · not yet peer reviewed</sub>

**qrh_04** — The orange gap, from one half out to seven eighths, is where the Riemann Hypothesis still lives. Push the wall all the way to one half, and the primes are finally tamed.
  <sub>camera `strip_gap` · actions `hide:band1112 show:gap card:off`</sub>

## 10. Written Into the Universe

**outro_01** — A square root. A dial. A spiral that keeps striking its centre. Waves that cancel into a map of the primes.
  <sub>camera `outro` · actions `hide:strip hide:gap show:spiral show:tail show:trail show:zeros show:prime show:imaxis speed:4 play`</sub>

**outro_02** — Put the keys together and something strange appears. The primes, the most irregular objects in arithmetic, obey a rule written into the structure of numbers themselves, as if the mathematics of the universe came with these instructions built in.
  <sub>camera `sacks_saddle` · actions `sacks:show sacks:routes pulse:sacks`</sub>

**outro_03** — Prove the one half key, and we will have read one line of that instruction set.
  <sub>camera `sacks_saddle` · actions `pulse:sacks`</sub>

---
1407 words ≈ 9.4 minutes of speech at 150 wpm, plus pauses and title cards.
