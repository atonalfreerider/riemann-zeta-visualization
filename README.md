![the Sacks spiral bent on its saddle, with the master wave of 10,000 zeta zeros spiking at the primes](docs/gifs/00-saddle.gif)

This is a Unity application for graphing the Riemann Zeta function along the critical strip at 1/2, and feeding the non-trivial Zeta Zero outputs into the Prime Counting function.

The Riemann Zeta function is given by:  
`ζ(x) = Σ 1 / n ^ s`

The Prime Counting function is approximated by Riemann's explicit formula:  
`ψ(x) = x − ln 2π − ½ ln(1 − x⁻²) − 4√x Σ (cos(γ log x) + 2γ sin(γ log x)) / (1 + 4γ²)`

An animation of the Zeta function is shown below. The Zeta curve spirals and intersects with the axis, resulting in Zeta Zeros (yellow dots). These values are fed into the Prime Counting function to the right. 

![the chain on its floor, with the dial t climbing the pole](docs/gifs/04-stage.gif)

When enough non-trivial Zeta Zero values are provided to the Prime Counting function, prime numbers can be inferred from the large verticle steps in the graph.

![the explicit formula with 300 zeros: the staircase steps up at every prime](docs/screenshots/07-staircase.png)

The Zeta function is a composite of line segments whose segment lengths are determined by the limit of the real component of `s`, and whose rotation values are determined by the limit of the imaginary component of `s`. In the animation below, each segment is graphed in a color-coded spiral to show how the values sum together. 

The plain series does not converge on the critical line, so the spiral alone never lands on ζ(s). The orange segment is the Euler–Maclaurin tail (`N^(1−s)/(s−1) + N^−s/2 + Bernoulli terms`, see `Assets/Math/ZetaMath.cs`) which closes the spiral onto the exact value to ~1e-13 — so the spiral now really does predict the zeros. The number of spiral segments grows with t (N ≈ t/2 + 4).

![the arrows of the chain, with the orange tail that closes it, diving into the centre at t = 14.13](docs/gifs/05-turn.gif)

There are other tools built into this animation to help break down and visualize how these mysterious functions work.

The app opens on the Sacks spiral bent on its saddle, with the master wave of 10,000 zeros riding it and the zeta
trail (with its chain of arrows at the tip) growing out of the centre. The camera starts close to the centre, pulls
back and orbits; drag or W A S D to orbit and change altitude, wheel or Q E to zoom, Shift + drag to move the centre.
A side menu tucked into the left edge (the arrow, **M** or **Tab**) switches views (Saddle, XY graph), toggles layers
(saddle / flat, master wave, trail, chain, numbers, the k², k(k+1) and k² + k + 41 curves, trail height), picks the
format (16:9 or 9:16), plays the tour or any chapter, and records the whole tour or a single chapter.

![the graph view opening close to the centre: the master wave of 10,000 zeros on the saddle, the zeta trail and its chain](img/home.png)

Keys: **M** menu · **T** start/stop the narrated tour · **N** next line · **F9** record · **P** switch 16:9 / 9:16 ·
**F** frame rate.

## The guided tour, chapter by chapter

A narrated, chaptered camera fly-through explains the whole machine from first principles. It renders in
9:16 for Shorts, with karaoke captions that highlight the spoken word. Run it with **Zeta ▸ Play Tour** in
the editor (or press **T** in Play mode); see [docs/TOUR.md](docs/TOUR.md) for recording, narration and the
scripting vocabulary, and [docs/narration-script.md](docs/narration-script.md) for every caption.

| | | |
|---|---|---|
| ![01-sieve](docs/gifs/01-sieve.gif) | ![01-sieve](docs/screenshots/01-sieve.png) | **1. The Prime Sieve.** A grid of 1–30 breaks open onto the Sacks spiral, drawn the way Robert Sacks first sketched it on a notepad, and only the 2 stays lit (gold). Every composite is made in order as p × m (p its smallest prime, nearer the centre): m white dots are counted, p copies the row up a level at a time, the copies slide along the spiral and only the final dot lands on p × m. Looking back from the landing, every number the product overshot gets an open circle and becomes the next prime, which joins the multipliers. |
| ![02-sacks](docs/gifs/02-sacks.gif) | ![02-sacks](docs/screenshots/02-sacks.png) | **2. The Sacks Spiral.** The whole Sacks spiral (to 1601, Euler's 40th prime) starts flat and bends over a saddle. The camera follows each route while its numbers are written along it: the squares n = k² (red), the neighbour products n = k(k + 1) (blue) and Euler's n = k² + k + 41 (green, 40 primes in a row). Then Robert Sacks's [product curves](https://numberspiral.com) n = k² + k + p, prime-rich when p is prime. |
| ![03-zeno](docs/gifs/03-zeno.gif) | ![03-zeno](docs/screenshots/03-zeno.png) | **3. Zeno's Chain.** Zeno's halving steps stop dead at 2; the gentler rule, one over the square root of the step number, never stops. The exponent one half is the key. |
| ![04-stage](docs/gifs/04-stage.gif) | ![04-stage](docs/screenshots/04-stage.png) | **4. The Stage.** A floor (the complex plane), a pole, and a dial t that bends every joint of the chain. |
| ![05-turn](docs/gifs/05-turn.gif) | ![05-turn](docs/screenshots/05-turn.png) | **5. Turn the Dial.** Lengths follow square roots, bends follow logarithms: each joint's turn t·log(n/(n−1)) is drawn from a dotted extension of the previous leg and keeps winding as t climbs. The spring of skipped arrows and the Euler–Maclaurin tail; at t = 14.13 the tip hits the centre, and the first 20 turns are stacked end to end (they add up to t·log 21). |
| ![06-waves](docs/gifs/06-waves.gif) | ![06-waves](docs/screenshots/06-waves.png) | **6. Collisions Become Waves.** A cosine with a γ dial, bent by a logarithm; each collision adds its own wave, and hundreds of them cancel into spikes on the primes, with fainter ones on the squares. |
| ![07-staircase](docs/gifs/07-staircase.gif) | ![07-staircase](docs/screenshots/07-staircase.png) | **7. The Staircase.** Riemann's explicit formula built one term at a time against the exact ψ(x): y = x, − ln 2π, the trivial zeros, then each zero's wave shown alone and poured in, until the steps land on the primes and prime powers. |
| ![08-key](docs/gifs/08-key.gif) | ![08-key](docs/screenshots/08-key.png) | **8. The Square-Root Key.** At exponent 0.9 the tip misses the centre (the miss distance and closest pass are drawn); on the strip the collisions sit on the ½ line; moved off it, the staircase wobbles away from the original, with the difference shown. |
| ![09-qrh](docs/gifs/09-qrh.gif) | ![09-qrh](docs/screenshots/09-qrh.png) | **9. 2026: A Wall at Seven Eighths.** OpenAI's AI-generated, Lean-checked claim that no collision strays past 7/8 (or below 1/8), its status, and the open gap where the hypothesis still lives. |
| ![10-outro](docs/gifs/10-outro.gif) | ![10-outro](docs/screenshots/10-outro.png) | **10. Written Into the Universe.** Straight back to the Sacks spiral on its saddle; the zeta trail rises from the centre, each collision sends its wave along the spiral, and the waves settle onto the saddle as one master wave that spikes at every prime (a picture, not a result). It ends on the graph view, ready to explore. |

## Games: point at the numbers

Four games in the model lab, on the desktop and in VR (side menu **M** ▸ **GAMES**, or the VR dock's **Models** tab):
**Prime hunt** on the Sacks spiral, **Building blocks** (split a number into its primes), **E8 spheres** (E8's lattice
shells as spheres of 240 σ₃(n) points, 340 320 of them on the desktop, their divisor cubes, and the impostors on E8's
clock) and **Vortex hunt** (the vortices of ζ's chain and the counting numbers they belong to). Gold is always prime,
deep red a prime squared, blue a pronic number k(k + 1), grey any other composite. Every number unpacks as a marble
fingerprint, its factors and exponents counted in marbles, into its full volume and back (**U**). In VR you point with
your index finger and hold, or touch. See [docs/GAMES.md](docs/GAMES.md).

| | |
|---|---|
| ![Prime hunt](docs/games/prime-hunt.jpg) | ![E8 spheres](docs/games/e8-spheres.jpg) |
| ![Building blocks: 360's full volume](docs/games/blocks-volume.jpg) | ![Marble fingerprints](docs/games/fingerprints.jpg) |

## Meta Quest 2 (VR, hands only)

The same visualization runs on a Quest 2 with hand tracking only: passthrough is the default backdrop, one touch
turns it into a blackout, and the whole tour (narrated, captioned) and every math view are driven with big icon buttons
you push with a finger. Pinch to move the model, pinch with both hands to scale and turn it, turn your left palm toward
your face for a quick menu. See [docs/VR.md](docs/VR.md) for building, installing and using it.

Thumbnails for the video are in [docs/thumbnail](docs/thumbnail).

## Guided tour, bloom and recording

Everything glows through a built-in-pipeline bloom (`Assets/Rendering`), and a narrated, chaptered
camera fly-through explains the spiral, the two complex planes (where the imaginary axis is), the
zeros, the explicit formula, why Re(s) = ½ matters, and the October 2026 OpenAI "quasi-Riemann
hypothesis" claim (zero-free for Re s > 7/8). See [docs/TOUR.md](docs/TOUR.md) for running,
recording and generating the ElevenLabs narration, and [docs/narration-script.md](docs/narration-script.md)
for the full captions.

Hold right mouse button for freelook  

References:  
https://medium.com/cantors-paradise/the-riemann-hypothesis-explained-fa01c1f75d3f  
https://en.wikipedia.org/wiki/Explicit_formulae_for_L-functions  
https://github.com/danielhutama/Riemann-Explicit-Formula-for-Primes  
3Blue1Brown:  
https://youtu.be/sD0NjbwqlYw  