# The Spiral, the Zeros, and the Primes — narration script

Every line below is one caption/narration unit. The tour highlights each word in yellow as it is spoken.
Camera cues and scene actions are listed for reference; see docs/TOUR.md for the vocabulary.

## 1. A Machine for Primes
*The Riemann zeta function, drawn in three dimensions*

**intro_01** — This is the Riemann zeta function, drawn as a spiral that climbs a line through the complex plane.
  <sub>camera `overview_orbit` · actions `reset speed:2.5 play`</sub>

**intro_02** — Every time the spiral closes on the origin, a zero is born, and that zero becomes a wave.
  <sub>camera `follow_tip`</sub>

**intro_03** — Add enough of those waves together and a staircase appears, stepping up at every prime number.
  <sub>camera `prime_overview` · actions `speed:6`</sub>

**intro_04** — In the next few minutes we will build this picture from scratch, find the hidden axis, and see what a 2026 result changes.
  <sub>camera `overview_pull` · actions `speed:3`</sub>

## 2. Building the Spiral
*One arrow per term of the zeta series*

**spiral_01** — Zeta starts as an infinite sum: one, plus one over two to the s, plus one over three to the s, and so on forever.
  <sub>camera `spiral_close` · actions `reset jump:10 pause hide:tail`</sub>

**spiral_02** — Each term is an arrow. Its length shrinks like one over the square root of n, because the real part of s is one half.
  <sub>camera `spiral_top`</sub>

**spiral_03** — And each arrow is rotated by the imaginary part of s, by an angle of t times the log of n. The arrows chain head to tail.
  <sub>camera `spiral_top` · actions `show:components speed:0.6 play`</sub>

**spiral_04** — But here is the catch. On the critical line the plain sum never converges. Sixteen arrows, or a thousand, land nowhere in particular.
  <sub>camera `spiral_top` · actions `pause hide:components`</sub>

**spiral_05** — The fix is Euler and Maclaurin's. The missing tail of the sum is almost exactly an integral, and we can draw that integral as one more arrow.
  <sub>camera `spiral_top` · actions `show:tail flash:tail`</sub>

**spiral_06** — Add it, plus a few tiny Bernoulli corrections, and the chain lands on the true value of zeta to thirteen decimal places.
  <sub>camera `spiral_tail_zoom` · actions `flash:tail` · footnote: ζ(s) = Σ n^-s (n < N)  +  N^(1-s)/(s-1)  +  N^-s/2  +  Bernoulli terms      N ≈ t/2 + 4</sub>

**spiral_07** — That orange arrow is the difference between a pretty picture and an exact one. Now the spiral really does predict the zeros.
  <sub>camera `follow_tip` · actions `speed:2 play`</sub>

## 3. Where Is the Imaginary Axis?
*Two complex planes, not one*

**axis_01** — Here is the part that confuses everyone, including me. There are two complex planes in this picture, not one.
  <sub>camera `axes_wide` · actions `pause show:axes`</sub>

**axis_02** — The input, s, lives on the glowing panel to the left. Its real part runs left to right, from zero to one.
  <sub>camera `strip_panel` · actions `show:strip`</sub>

**axis_03** — Its imaginary part, t, runs straight up. So the vertical direction of this whole scene is the imaginary axis of the input.
  <sub>camera `strip_vertical` · actions `show:imaxis`</sub>

**axis_04** — We only feed zeta points with real part one half, so the input crawls up the critical line in the middle of the strip.
  <sub>camera `strip_probe` · actions `speed:1.2 play`</sub>

**axis_05** — The output is a second complex number, drawn flat. Real part to the right, imaginary part toward the back.
  <sub>camera `output_plane` · actions `show:outaxes`</sub>

**axis_06** — So the spiral lives in the output plane, floating at the height of its input. Climb the input axis and the spiral redraws itself.
  <sub>camera `follow_tip` · actions `speed:1.8`</sub>

**axis_07** — A zero is simply an input whose output lands on the origin: the spiral closes, and we drop a yellow marker on the vertical axis.
  <sub>camera `zero_marker` · actions `pause jump:14.4 flash:zero`</sub>

## 4. Hunting Zeros
*Hardy's Z function makes the hunt visible*

**zeros_01** — Riemann's Z function makes the hunt visible. It is zeta rotated to be real, so it just wiggles left and right of the critical line.
  <sub>camera `strip_z` · actions `reset show:zcurve hide:imaxis speed:2 play`</sub>

**zeros_02** — Every time the wiggle crosses the line, zeta is exactly zero. The first crossing is at t equals fourteen point one three four seven.
  <sub>camera `strip_z_first` · actions `jump:14.5 flash:zero pause`</sub>

**zeros_03** — Then twenty one point zero two, twenty five point zero one, thirty point four two. The zeros keep coming, irregular but never missing.
  <sub>camera `zero_ladder` · actions `speed:3 play`</sub>

**zeros_04** — Every one of the ten thousand zeros we loaded sits precisely on the line. Nobody has ever found one anywhere else.
  <sub>camera `zero_ladder_up` · actions `speed:5` · footnote: zeros from the LMFDB table · Assets/Resources/zetazeros.txt</sub>

## 5. From Zeros to Primes
*Riemann's explicit formula*

**primes_01** — Now the magic. Each zero, gamma, becomes a wave in x: the cosine of gamma times the log of x.
  <sub>camera `cosine_wave` · actions `pause hide:prime hide:strip hide:outaxes hide:zcurve show:cosine`</sub>

**primes_02** — One wave says nothing. But stack the waves from many zeros and they interfere, cancelling almost everywhere.
  <sub>camera `cosine_wave` · actions `speed:4 play`</sub>

**primes_03** — Where they do not cancel, they spike. And they spike exactly at the primes and their powers: two, three, four, five, seven, eight, nine, eleven.
  <sub>camera `cosine_spikes`</sub>

**primes_04** — Riemann's explicit formula turns that chorus into a staircase. Start with the line psi equals x, then subtract four root x times a weighted wave for every zero.
  <sub>camera `formula` · actions `hide:cosine show:prime show:equations`</sub>

**primes_05** — The weights, cosine plus two gamma sine over one plus four gamma squared, come from dividing x to the rho by rho. Nothing is fudged.
  <sub>camera `formula_close` · footnote: ψ(x) = x − Σ_ρ x^ρ/ρ − ln 2π − ½ ln(1 − x^-2)        ρ = ½ ± iγ</sub>

**primes_06** — With a few hundred zeros the staircase sharpens and the red markers find the primes on their own: two, three, five, seven, eleven, thirteen.
  <sub>camera `staircase` · actions `speed:8 play`</sub>

**primes_07** — The zeros of zeta and the primes are the same information, written in two languages. The spiral is the translator.
  <sub>camera `staircase_wide`</sub>

## 6. Why One Half Matters
*The real part sets the size of every wave*

**half_01** — Why does everyone care that the zeros sit at real part one half? Because the real part sets the size of each wave.
  <sub>camera `strip_panel` · actions `pause show:strip`</sub>

**half_02** — A zero at real part beta adds a wave of height x to the beta. At one half, that is the square root of x, the smallest wobble possible.
  <sub>camera `staircase`</sub>

**half_03** — Suppose a rogue zero existed at real part zero point nine. Its wave would grow like x to the zero point nine, and the staircase would be wrecked.
  <sub>camera `ghost` · actions `ghost:0.9:14.134725` · footnote: red: the staircase if the first zero were moved from ½ + 14.13i to 0.9 + 14.13i (hypothetical)</sub>

**half_04** — The Riemann Hypothesis says no such rogue exists. The error in counting primes is essentially as small as it can be: about the square root of x, for every x.
  <sub>camera `staircase` · actions `ghost:off`</sub>

## 7. 2026: A Wall at Seven Eighths
*OpenAI's quasi-Riemann hypothesis claim*

**qrh_01** — On October sixth, 2026, OpenAI released an AI generated proof of what it calls the quasi Riemann hypothesis.
  <sub>camera `strip_panel` · actions `pause show:strip` · footnote: openai/math · family 003 · manuscript dated Sept 30, 2026 · released Oct 6, 2026</sub>

**qrh_02** — The claim: the zeta function, and every Dirichlet L function, has no zeros with real part greater than seven eighths.
  <sub>camera `strip_band` · actions `show:band78`</sub>

**qrh_03** — By the functional equation that also clears everything below one eighth. The shaded bands are now off limits.
  <sub>camera `strip_band_sym` · actions `show:band18`</sub>

**qrh_04** — Before this, the known zero free region was a sliver hugging real part one, shrinking as t grows. A fixed wall at seven eighths is new territory.
  <sub>camera `strip_band_classic` · actions `show:classic`</sub>

**qrh_05** — The proof goes through cubic theta functions and Hecke L functions over Q adjoined root negative three, then transfers to zeta. A companion paper reaches eleven twelfths by a different route.
  <sub>camera `strip_band` · actions `hide:classic show:band1112`</sub>

**qrh_06** — The core theorem was formalized in Lean and re checked by an independent kernel. Human peer review is still in progress, so treat it as a strong claim, not settled fact.
  <sub>camera `strip_band` · footnote: Lean statement: riemannZeta s ≠ 0 for 7/8 < Re s · axioms: propext, Classical.choice, Quot.sound · not yet peer reviewed</sub>

**qrh_07** — For the staircase it means every rogue wave is at most x to the seven eighths. For this picture it means the yellow markers can only hide in the narrow unshaded band.
  <sub>camera `strip_band_sym` · actions `hide:band1112`</sub>

**qrh_08** — The gap from seven eighths down to one half is where the Riemann Hypothesis still lives. Commentators expect the method to stall near three quarters. A new idea is needed.
  <sub>camera `strip_gap` · actions `show:gap`</sub>

## 8. The State of Play

**outro_01** — So this is the state of play. Ten thousand zeros on the line, a spiral that lands on every one of them, and a wall at seven eighths.
  <sub>camera `overview_orbit` · actions `hide:gap show:imaxis show:cosine speed:4 play`</sub>

**outro_02** — Prove the wall is at one half and the primes are finally tamed. Until then, watch the spiral climb.
  <sub>camera `overview_pull_final`</sub>

---
1017 words ≈ 6.8 minutes of speech at 150 wpm, plus pauses and title cards.
