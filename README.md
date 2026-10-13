This is a Unity application for graphing the Riemann Zeta function along the critical strip at 1/2, and feeding the non-trivial Zeta Zero outputs into the Prime Counting function.

The Riemann Zeta function is given by:  
`ζ(x) = Σ 1 / n ^ s`

The Prime Counting function is approximated by Riemann's explicit formula:  
`ψ(x) = x − ln 2π − ½ ln(1 − x⁻²) − 4√x Σ (cos(γ log x) + 2γ sin(γ log x)) / (1 + 4γ²)`

An animation of the Zeta function is shown below. The Zeta curve spirals and intersects with the axis, resulting in Zeta Zeros (yellow dots). These values are fed into the Prime Counting function to the right. 

![plot](./img/zeta-visualization-1.gif)

When enough non-trivial Zeta Zero values are provided to the Prime Counting function, prime numbers can be inferred from the large verticle steps in the graph.

![plot](./img/500-pts.png)

The Zeta function is a composite of line segments whose segment lengths are determined by the limit of the real component of `s`, and whose rotation values are determined by the limit of the imaginary component of `s`. In the animation below, each segment is graphed in a color-coded spiral to show how the values sum together. 

The plain series does not converge on the critical line, so the spiral alone never lands on ζ(s). The orange segment is the Euler–Maclaurin tail (`N^(1−s)/(s−1) + N^−s/2 + Bernoulli terms`, see `Assets/Math/ZetaMath.cs`) which closes the spiral onto the exact value to ~1e-13 — so the spiral now really does predict the zeros. The number of spiral segments grows with t (N ≈ t/2 + 4).

![plot](./img/zeta-visualization-2.gif)

There are other tools built into this animation to help break down and visualize how these mysterious functions work.

Keyboard Commands:  
- WASD QE: navigation  
- Spacebar: play/pause animation  
- Escape: reset animation  
- R key:  toggle additional limit curves  
- T: start/stop the narrated guided tour (N: next line, F9: record 1080×1920, P: toggle 9:16)  

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