# The Square-Root Key: a machine for manufacturing primes — narration script

Every line below is one caption/narration unit. The tour highlights each word in yellow as it is spoken.
Camera cues and scene actions are listed for reference; see docs/TOUR.md for the vocabulary.

## 1. Zeno's Chain
*and a gentler rule*

**zeno_01** — Zeno's old puzzle. Walk half way to the wall. Then half of what is left. Then half again. Infinitely many steps, and you never pass two.
  <sub>camera `zeno` · actions `reset pause hide:spiral hide:outaxes hide:imaxis hide:polelabel show:zeno`</sub>

**zeno_02** — Lay the steps out as arrows: one, a half, a quarter, an eighth. The chain shrinks fast and stops dead at two.
  <sub>camera `zeno`</sub>

**zeno_03** — Now a gentler rule. The second arrow is one over the square root of two. The third, one over root three. Every arrow is one over the square root of its number.
  <sub>camera `zeno` · actions `terms:1 hide:tail show:spiral show:lengths hide:center build:1:8`</sub>

**zeno_04** — Shrinking this slowly, the chain never stops. Sixty arrows reach fourteen. A million reach two thousand. It runs off the floor forever.
  <sub>camera `chain_long` · actions `hide:zeno hide:lengths build:8:60`</sub>

**zeno_05** — Hold on to that square root. The exponent one half is the key to everything that follows, and we will come back to it at the end.
  <sub>camera `chain_long`</sub>

## 2. The Stage
*A floor, a pole, and a dial*

**stage_01** — The chain lives on a flat floor with a pole going up. Mathematicians call the floor the complex plane. For us it is just a floor: so many steps right, so many steps back.
  <sub>camera `stage` · actions `hide:spiral show:outaxes show:imaxis show:polelabel`</sub>

**stage_02** — The pole is a dial. Its reading is called t. Turn it, and every joint in the chain bends.
  <sub>camera `stage` · actions `show:center`</sub>

## 3. Turn the Dial
*The tip keeps finding the centre*

**turn_01** — Each joint bends by the dial reading times the log of its number. The straight chain curls into a spiral.
  <sub>camera `spiral_curl` · actions `terms:24 show:spiral show:center hide:polelabel speed:0.35 play`</sub>

**turn_02** — The chain is endless, so one orange arrow stands in for the whole rest of it. Now the tip is exact.
  <sub>camera `spiral_curl` · actions `show:tail flash:tail speed:0.5` · footnote: the orange arrow is the Euler–Maclaurin tail: N^(1−s)/(s−1) + N^−s/2 + Bernoulli terms</sub>

**turn_03** — Keep turning. The tip wanders, then dives straight into the centre.
  <sub>camera `tip_center` · actions `show:tip speed:1.3`</sub>

**turn_04** — At a dial reading of fourteen point one three it hits dead centre. We drop a marker on the pole at that height and call it a collision.
  <sub>camera `tip_center` · actions `pause jump:14.134725 flash:zero`</sub>

**turn_05** — Twenty one. Twenty five. Thirty point four. The collisions never stop. Ten thousand are loaded here, all on this pole.
  <sub>camera `markers_axis` · actions `terms:auto speed:3 play`</sub>

**turn_06** — Here is the tip's whole path, threading the centre again and again. These collision heights are the famous zeros of the Riemann zeta function.
  <sub>camera `markers_axis_wide` · actions `show:trail speed:6`</sub>

## 4. Collisions Become Waves
*One wave per collision*

**wave_01** — Start with an ordinary cosine wave. Up, down, up, down, the same width forever.
  <sub>camera `wave` · actions `pause hide:spiral hide:tail hide:tip hide:center hide:trail hide:imaxis hide:outaxes hide:zeros show:numbers wave:plain`</sub>

**wave_02** — Now bend it. Stretch the wave with a logarithm, and set its speed to the first collision height, fourteen point one three. The ripples bunch up near zero and widen as x grows.
  <sub>camera `wave` · actions `wave:morph`</sub>

**wave_03** — That is the first collision's wave.
  <sub>camera `wave` · actions `cosine:1 show:cosine wave:off`</sub>

**wave_04** — Take the second collision, twenty one point zero two, build its wave the same way, and add it on top. Where the two agree the peaks reinforce. Where they disagree they cancel.
  <sub>camera `wave` · actions `cosine:build:1:2`</sub>

**wave_05** — Keep going. The third wave. The fourth. Ten waves. More and more of the pattern cancels out.
  <sub>camera `wave` · actions `cosine:build:2:10`</sub>

**wave_06** — Hundreds of waves. What survives are sharp spikes, and they sit exactly on two, three, five, seven, eleven, thirteen, and the prime powers four, eight, nine.
  <sub>camera `spikes` · actions `cosine:build:10:all speed:6 play`</sub>

**wave_07** — A map of where the primes are, built from nothing but collision heights.
  <sub>camera `spikes`</sub>

## 5. The Staircase
*Riemann's exact recipe*

**stair_01** — Riemann's exact recipe turns the map into a count. Start from the straight line y equals x, and subtract one weighted wave per collision.
  <sub>camera `staircase_close` · actions `pause hide:cosine hide:numbers show:prime`</sub>

**stair_02** — Each weight is fixed by the recipe. Nothing is tuned by hand.
  <sub>camera `staircase_close` · actions `show:equations` · footnote: ψ(x) = x − ln 2π − ½ ln(1 − x^-2) − 4√x Σ (cos(γ log x) + 2γ sin(γ log x)) / (1 + 4γ²)</sub>

**stair_03** — The result is a staircase. Every step is a prime: two, three, five, seven, eleven, thirteen. The red markers find them on their own.
  <sub>camera `staircase` · actions `hide:equations speed:8 play`</sub>

**stair_04** — More collisions, sharper steps. With a few hundred of them the staircase counts the primes exactly.
  <sub>camera `staircase_wide`</sub>

## 6. The Whole Machine

**machine_01** — Now see it all at once. The chain shrinks by the square root rule. The dial turns. The tip strikes the centre, and every strike drops a marker on the pole.
  <sub>camera `machine` · actions `show:spiral show:tail show:trail show:zeros show:imaxis show:cosine cosine:all speed:4 play`</sub>

**machine_02** — Every marker becomes a wave, the waves pile up on the primes, and the staircase counts them. A machine for manufacturing prime numbers.
  <sub>camera `machine`</sub>

## 7. The Square-Root Key
*Why one half, and nothing else*

**key_01** — Back to the key. Everything used the square root: exponent one half. Change it to zero point nine and watch.
  <sub>camera `sigma_spiral` · actions `pause hide:prime hide:cosine hide:trail hide:imaxis jump:14.134725 sigma:0.9 terms:auto show:spiral show:tail show:tip show:center`</sub>

**key_02** — The spiral is tighter, and the tip never hits the centre. No collisions at all.
  <sub>camera `sigma_spiral` · actions `speed:2.5 play`</sub>

**key_03** — Picture every possible exponent between zero and one as a strip. Our collisions all sit on the one half line, right down the middle.
  <sub>camera `strip_panel` · actions `pause sigma:0.5 hide:spiral hide:tail hide:tip hide:center show:strip`</sub>

**key_04** — Suppose a collision strayed off that line, out to zero point nine. Its wave would grow like x to the zero point nine, and the staircase would wobble off the rails.
  <sub>camera `wobble` · actions `hide:strip show:prime ghost:all:0.9` · footnote: red: the staircase if every collision had exponent 0.9 instead of ½</sub>

**key_05** — The Riemann Hypothesis says it never happens: every collision, forever, exactly on the one half line. That is why the square root is the magic key.
  <sub>camera `wobble` · actions `ghost:off`</sub>

## 8. 2026: A Wall at Seven Eighths
*OpenAI's quasi-Riemann hypothesis claim*

**qrh_01** — In October 2026, OpenAI published an AI generated proof that no collision can stray past seven eighths. They call it the quasi Riemann hypothesis.
  <sub>camera `strip_band` · actions `hide:prime show:strip show:band78` · footnote: openai/math, family 003 · manuscript Sept 30, 2026 · released Oct 6, 2026</sub>

**qrh_02** — By symmetry, nothing below one eighth either. The blue bands are a wall: no collision can live there.
  <sub>camera `strip_band_sym` · actions `show:band18`</sub>

**qrh_03** — The core theorem is machine checked in Lean, and a companion proof reaches eleven twelfths. Human peer review is still under way, so call it a strong claim, not settled fact.
  <sub>camera `strip_band` · actions `show:band1112` · footnote: Lean: riemannZeta s ≠ 0 for 7/8 < Re s · independently re-checked · not yet peer reviewed</sub>

**qrh_04** — The orange gap, from one half out to seven eighths, is where the Riemann Hypothesis still lives. Push the wall all the way to one half, and the primes are finally tamed.
  <sub>camera `strip_gap` · actions `hide:band1112 show:gap`</sub>

## 9. Watch the Spiral Climb

**outro_01** — A chain that shrinks by the square root. A dial that turns. A tip that keeps striking the centre. And a staircase of primes that falls out of it.
  <sub>camera `outro` · actions `hide:strip hide:gap show:spiral show:tail show:trail show:zeros show:prime show:imaxis speed:4 play`</sub>

**outro_02** — Watch the spiral climb.
  <sub>camera `outro`</sub>

---
831 words ≈ 5.5 minutes of speech at 150 wpm, plus pauses and title cards.
