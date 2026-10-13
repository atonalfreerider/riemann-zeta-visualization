# The Spiral, the Collisions, and the Primes — narration script

Every line below is one caption/narration unit. The tour highlights each word in yellow as it is spoken.
Camera cues and scene actions are listed for reference; see docs/TOUR.md for the vocabulary.

## 1. The Stage
*A floor, a pole, and a dial*

**stage_01** — Start with an empty stage. A flat floor, and a pole going straight up.
  <sub>camera `stage` · actions `reset pause hide:spiral show:outaxes show:imaxis show:polelabel`</sub>

**stage_02** — Every point on the floor is a complex number: so many steps to the right, so many steps toward the back.
  <sub>camera `stage_floor`</sub>

**stage_03** — The pole is a dial. We will turn it slowly, and everything on the floor will move. Its reading is called t.
  <sub>camera `stage` · actions `show:center`</sub>

## 2. Zeno's Chain
*Shrink, then turn*

**chain_01** — Lay down an arrow of length one.
  <sub>camera `chain_flat` · actions `terms:1 hide:tail show:spiral show:lengths hide:center hide:outaxes hide:imaxis hide:polelabel`</sub>

**chain_02** — Add a second, shorter arrow, then a third, shorter still. Zeno would halve each step. Our rule is gentler: one over the square root of the step number.
  <sub>camera `chain_flat` · actions `build:1:6`</sub>

**chain_03** — One. Point seven one. Point five eight. Point five. The arrows shrink, but only slowly.
  <sub>camera `chain_flat` · actions `build:6:12`</sub>

**chain_04** — Zeno's halves add up to two and stop. Ours never stop. Sixty arrows reach fourteen, a million reach two thousand. The chain runs off the floor forever.
  <sub>camera `chain_long` · actions `hide:lengths build:12:60`</sub>

**chain_05** — Now turn the dial. At every joint, bend the chain by the dial reading times the log of the step number. The straight chain curls into a spiral.
  <sub>camera `spiral_curl` · actions `terms:24 show:center show:imaxis speed:0.35 play`</sub>

**chain_06** — The chain is endless, so we add one honest orange arrow standing in for every arrow too small to draw. Now the tip is exact.
  <sub>camera `spiral_curl` · actions `show:tail flash:tail speed:0.5` · footnote: the orange arrow is the Euler–Maclaurin tail: N^(1−s)/(s−1) + N^−s/2 + Bernoulli terms</sub>

**chain_07** — Here is the strange talent of the one half rule. Keep turning, and the tip wanders, then dives straight into the centre.
  <sub>camera `tip_center` · actions `show:tip speed:1.3`</sub>

**chain_08** — At a dial reading of fourteen point one three, it hits dead centre. We drop a marker on the pole at that height.
  <sub>camera `tip_center` · actions `pause jump:14.134725 flash:zero`</sub>

## 3. Collisions
*The tip keeps finding the centre*

**coll_01** — Keep turning. Twenty one. Twenty five. Thirty point four. The tip keeps finding the centre.
  <sub>camera `markers_axis` · actions `terms:auto speed:3 play`</sub>

**coll_02** — It never stops. Riemann proved the collisions go on forever. Ten thousand of them are loaded here, every one on this pole.
  <sub>camera `markers_axis` · actions `speed:5`</sub>

**coll_03** — Here is the tip's whole path, winding up the pole and threading the centre again and again. Those collision heights are the famous zeros of the zeta function.
  <sub>camera `markers_axis_wide` · actions `show:trail speed:6`</sub>

## 4. Collisions Become Waves
*A probability map of the primes*

**wave_01** — Now take the first collision height, fourteen point one three, and turn it into a wave: the cosine of fourteen point one three times the log of x.
  <sub>camera `wave` · actions `pause hide:spiral hide:tail hide:tip hide:center hide:trail hide:imaxis hide:outaxes hide:zeros cosine:1 show:cosine show:numbers`</sub>

**wave_02** — Add the wave from the second collision.
  <sub>camera `wave` · actions `cosine:2`</sub>

**wave_03** — And the third.
  <sub>camera `wave` · actions `cosine:3`</sub>

**wave_04** — Ten waves. Most of the time they cancel each other out.
  <sub>camera `wave` · actions `cosine:10`</sub>

**wave_05** — All of them. Where they agree, they pile up into spikes, and the spikes sit exactly on two, three, five, seven, eleven, thirteen, and their powers: four, eight, nine.
  <sub>camera `spikes` · actions `cosine:all speed:6 play`</sub>

**wave_06** — This is a probability map of the primes, built from nothing but collision heights.
  <sub>camera `spikes`</sub>

## 5. The Staircase
*Riemann's exact recipe*

**stair_01** — Riemann's exact recipe turns the map into a count. Start from the straight line y equals x, and subtract one weighted wave per collision.
  <sub>camera `staircase_close` · actions `pause hide:cosine show:prime hide:numbers`</sub>

**stair_02** — Each weight is fixed by the recipe. Nothing is tuned by hand.
  <sub>camera `staircase_close` · actions `show:equations` · footnote: ψ(x) = x − ln 2π − ½ ln(1 − x⁻²) − 4√x Σ (cos(γ log x) + 2γ sin(γ log x)) / (1 + 4γ²)</sub>

**stair_03** — The result is a staircase. Every step is a prime: two, three, five, seven, eleven, thirteen. The red markers find them on their own.
  <sub>camera `staircase` · actions `hide:equations speed:8 play`</sub>

**stair_04** — More collisions, sharper steps. With a few hundred of them the staircase counts the primes exactly.
  <sub>camera `staircase_wide`</sub>

## 6. Move the One Half
*Why the shrink rate matters*

**half_01** — Everything so far used the one half rule. Let us move it. Shrink the arrows faster: n to the zero point nine.
  <sub>camera `sigma_spiral` · actions `pause hide:prime jump:14.134725 sigma:0.9 terms:auto show:spiral show:tail show:tip show:center`</sub>

**half_02** — Turn the dial. The spiral is tighter, and the tip never hits the centre. No collisions, no markers.
  <sub>camera `sigma_spiral` · actions `speed:2.5 play`</sub>

**half_03** — And if we pretend the collisions did happen at zero point nine, every wave grows like x to the zero point nine. The staircase wobbles out of control and stops counting.
  <sub>camera `wobble` · actions `pause sigma:0.5 hide:spiral hide:tail hide:tip hide:center show:prime ghost:all:0.9` · footnote: red: the staircase if every collision had shrink rate 0.9 instead of ½</sub>

**half_04** — The Riemann Hypothesis is the claim that the one half rule is the only one that works: every collision, forever, at exactly one half.
  <sub>camera `wobble` · actions `ghost:off`</sub>

## 7. 2026: A Wall at Seven Eighths
*OpenAI's quasi-Riemann hypothesis claim*

**qrh_01** — In October 2026, OpenAI published an AI generated proof that no collision can happen with a shrink rate above seven eighths. They call it the quasi Riemann hypothesis.
  <sub>camera `strip_band` · actions `hide:prime show:strip show:band78` · footnote: openai/math, family 003 · manuscript Sept 30, 2026 · released Oct 6, 2026</sub>

**qrh_02** — By symmetry, none below one eighth either. The blue bands are now off limits.
  <sub>camera `strip_band_sym` · actions `show:band18`</sub>

**qrh_03** — The core theorem is machine checked in Lean, and a companion proof reaches eleven twelfths. Human peer review is still under way, so call it a strong claim, not settled fact.
  <sub>camera `strip_band` · actions `show:band1112` · footnote: Lean: riemannZeta s ≠ 0 for 7/8 < Re s · independently re-checked · not yet peer reviewed</sub>

**qrh_04** — The orange gap, from one half to seven eighths, is where the Riemann Hypothesis still lives. Close it, and the primes are finally tamed.
  <sub>camera `strip_gap` · actions `hide:band1112 show:gap`</sub>

## 8. The Whole Machine

**outro_01** — A chain that shrinks by one half. A dial that turns. A tip that keeps striking the centre. And a staircase of primes that falls out of it.
  <sub>camera `outro` · actions `hide:strip hide:gap show:spiral show:tail show:trail show:zeros show:prime show:imaxis speed:4 play`</sub>

**outro_02** — Watch the spiral climb.
  <sub>camera `outro`</sub>

---
689 words ≈ 4.6 minutes of speech at 150 wpm, plus pauses and title cards.
