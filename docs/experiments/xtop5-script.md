# Video script: Five discoveries

Module `xtop5` (the 26th in the Experiments menu). It presents the five findings of this search that matter most, with the narration saying for each one what was found, why it matters, and what is new about it. It is drawn for a horizontal frame (1920 × 1080), because its scenes are the landscape stills of the sections it cites. The lines below are the narration as recorded in [`experiments.json`](../../Assets/Resources/Narration/experiments.json); narrate with `Tools/generate_narration.py --script experiments --only xtop5` and record with `Tools/render_chapters.py --script experiments --chapters xtop5 --shapes horizontal`.

**Title card:** Five discoveries — *what this search found, and why it matters*

831 words, about 5.5 minutes at the narrator's pace.

## How the five were chosen

The search ran from section A to section MC. The five below were chosen because each one joins two things that looked separate, was checked numerically, and changes how the earlier pictures read. For each, the narration says plainly which part is classical (Voronoi 1904, Hardy 1915, Ramanujan's sums, Hecke, Davenport and Heilbronn 1936, Chebyshev 1853, Rubinstein and Sarnak 1994) and what the search adds. What it adds is the picture or the connection, never a new theorem. None of the five finds primes faster or proves the Riemann hypothesis, and the last line says so.

| | discovery | sections | what is classical | what the search adds |
|---|---|---|---|---|
| 1 | The saddle under the Sacks spiral is the first wave of Voronoi's divisor formula. Hardy's circle-problem wave rides the saddle twisted half a turn per turn. | C, P | Voronoi's and Hardy's formulas | the two lattice-point problems as two surfaces under one spiral |
| 2 | On E8's eight hours, the vortex charges of the zeta chain are the traces of E8's Coxeter element. | E, I, IA | Ramanujan's sums c₃₀(k) = tr Cᵏ | E8 setting the strength of each vortex, measured at t = 5000 |
| 3 | ζ's zeros live in E8 as frequencies: coils whose sum lights the prime powers, with E8's eight L-functions putting each prime on its spoke. E8's own zeta function, 240ζ(s)ζ(s − 3), keeps every zero off its critical line Re s = 2. | JA–JD, KA–KB | the explicit formula, Hecke's Eisenstein series | where the zeros sit in E8's stack, and the empty critical line |
| 4 | The families' zeta functions have zeros off the line (one at Re s = 1.007). Of all the mixtures, only those in small windows round the class group's L-functions keep every zero on it (to t = 50). | LC–LF | Davenport and Heilbronn (1936), Bombieri and Hejhal (1995) | every mixture seen at once: one curve for −23, a plane for −47 |
| 5 | Chebyshev's bias is a fold. Squaring folds E8's wheel onto 1 and 19 and folds S₃ onto the identity, so in both races the identity's class runs three behind. | MA–MC | Chebyshev (1853), Rubinstein and Sarnak (1994) | one fold for both races, and the race between the families of −23 |

## 1. five discoveries

*On screen:* The Sacks spiral bent over its saddle, from high above, turning slowly.

*Board:* **five discoveries** — what this search found, and why it matters

> This search began with a spiral of numbers and one question: what geometry hides in the primes? Five findings stood out. For each one, here is what we found, why it matters, and what is new.

## 2. 1.  the saddle is a wave

*On screen:* The saddle's wireframe and field, with Voronoi's first wave (two crests per turn) riding it. Low angle, turning.

*Board:* **1.  the saddle is a wave** — the first wave of Voronoi's divisor formula:  two crests per turn

> One. The saddle under the spiral is a wave. We bent the Sacks spiral over a saddle to make its curves stand out. The saddle rises and falls twice on every turn, the same rhythm as the first wave of Voronoi's formula for counting divisors.

## 3. circles:  one crest per turn

*On screen:* The saddle twists half a turn per turn of the spiral, and Hardy's first wave for the circle problem rides it (one crest per turn).

*Board:* **circles:  one crest per turn** — Hardy's waves ride the saddle twisted half a turn per turn

> Count points in circles instead of under hyperbolas, and the waves rise once per turn. They need the saddle twisted half a turn for every turn of the spiral. Two counting problems, two surfaces under one spiral.

## 4. why it matters

*On screen:* Two charts, stacked: the divisor wobble Δ(x) (top) and the circle wobble P(x) (bottom), each with its sum of waves and its first wave.

*Board:* **why it matters** — the surface that shows the products is the products' own wave

> Why it matters: the curves on the Sacks spiral are products, and these waves are how the count of products wobbles. Voronoi and Hardy found the formulas a century ago. What is new is seeing them as the surfaces beneath the spiral.

## 5. 2.  the vortices of the zeta chain

*On screen:* The zeta chain at the 159th zero, link by link, curling into vortices.

*Board:* **2.  the vortices of the zeta chain** — one vortex for every counting number

> Two. Add one over n to the s, arrow by arrow, at a zero of zeta. The chain curls into vortices, one for every counting number, each the reflection of the chain's own start.

## 6. the charges are E8's Coxeter traces

*On screen:* Section IA's picture: three of E8's Coxeter eigenvalue wheels (k = 15, 1, 5) and the bar chart of the 30 spoke charges, measured (filled) against tr Cᵏ (outlined).

*Board:* **the charges are E8's Coxeter traces** — tr C^k = −1, 1, 2, 4, −2, −4, −8, 8, set by gcd(k, 30)

> Keep only the numbers on E eight's eight gold hours, and each vortex carries a charge. Measured at height five thousand, the charges are the traces of E eight's Coxeter element: minus one, one, two, four, and so on, set only by the common factor with thirty.

## 7. why it matters

*On screen:* The same picture, closing in on the bars.

*Board:* **why it matters** — a Lie group sets the strength of each vortex

> Why it matters: E eight's Coxeter trace is the sieve of two, three and five, written as a spectrum. So a Lie group sets the strength of every vortex. The identity behind it is Ramanujan's; what is new is watching E eight decide the vortices of zeta's chain.

## 8. 3.  where the zeros live in E8

*On screen:* E8's factor funnel with the first three zeros' coils, then E8's eight fans laid down one hour at a time.

*Board:* **3.  where the zeros live in E8** — coils up the funnel;  eight L-functions put each prime on its spoke

> Three. Where are zeta's zeros inside E eight? Not at points. Each zero is a coil wound up the factor funnel, and together the coils light every prime power. Zeta's zeros fix a prime's height; the zeros of seven more L-functions of E eight's field fix its spoke.

## 9. E8's own zeta function

*On screen:* Section KB's landscape of E8's own zeta function, Λ_E8(s)/Λ_E8(2 + it), with its gold critical line Re s = 2 and craters at ζ's zeros on Re s = ½ and 7/2.

*Board:* **E8's own zeta function** — 240 ζ(s) ζ(s − 3):  no zero on its own critical line, Re s = 2

> E eight also has its own zeta function. Count its vectors by length, and you get two hundred and forty times zeta of s, times zeta of s minus three. Its critical line is real part two, and no zero sits on it: they lie on one half and on seven halves, mirror images.

## 10. why it matters

*On screen:* The landscape again, the camera rising.

*Board:* **why it matters** — ζ's zeros are E8's own zeros, twice, all off its line

> Why it matters: Riemann's zeros are E eight's own zeros, twice, and not one of them lies on E eight's own critical line. The formula is classical, Hecke's. The empty critical line is the picture this search adds.

## 11. 4.  what keeps zeros on the line

*On screen:* Section LC's four strips of the s-plane: the zeros of the two families' zeta functions (red off the line, gold on it) and of the class group's two L-functions (all gold).

*Board:* **4.  what keeps zeros on the line** — each family's own zeta function has zeros off it

> Four: what the Riemann hypothesis depends on. Discriminant minus twenty-three has three families of curves, each with its own zeta function and Riemann's symmetry. Yet they have zeros off the critical line, one even at real part one point zero zero seven, where no Euler product can vanish.

## 12. every mixture at once

*On screen:* Section LE: the curve λ = −Λ₁/Λ₂(½ + it) with its crossings at λ = 2, 0 and −1, and the count of each mixture's zeros off the line.

*Board:* **every mixture at once** — no zero off the line only round the two L-functions

> Mix the families. Every mixture shares the functional equation, and one curve holds all their zeros on the line at once. Counted in full, up to height fifty, only two small windows of mixtures keep every zero on the line, both around the class group's L-functions.

## 13. five families:  a plane of mixtures

*On screen:* Section LF: the plane of −47's mixtures, coloured by zeros off the line, with the three L-functions marked.

*Board:* **five families:  a plane of mixtures** — gold:  no zero off the line up to t = 50

> With five families, for minus forty-seven, the mixtures fill a plane. Colour each one by its zeros off the line, and gold appears only in small islands: around the three L-functions, and one more island with no L-function in it.

## 14. why it matters

*On screen:* The plane again, closing in on the islands.

*Board:* **why it matters** — symmetry alone does not keep zeros on the line

> Why it matters: symmetry alone does not put zeros on the line. Davenport and Heilbronn proved that in 1936. The functions that keep them there, as far as anyone has checked, are the ones with Euler products. What is new is seeing every mixture at once.

## 15. 5.  the prime race on E8's spokes

*On screen:* Section MA: the race of the eight spokes to 10⁷, counted (top) and rebuilt from 6 771 zeros (bottom).

*Board:* **5.  the prime race on E8's spokes** — spokes 1 and 19 run three behind

> Five: the prime race. Race the primes on E eight's eight spokes up to ten million. Spokes one and nineteen run three behind, ahead of their share only zero point three and one point four percent of the time, and six thousand seven hundred and seventy-one zeros rebuild every curve.

## 16. the reason:  a fold

*On screen:* Section MB: the logarithmic clock of 30 with an arc from each prime to its square, and the fair race beside it.

*Board:* **the reason:  a fold** — squaring sends every spoke to 1 or 19

> The reason is a fold. The zeros count prime powers, and squaring folds the eight spokes onto one and nineteen, so every prime square lands there. Put the squares back, and the race is fair.

## 17. the same fold in S3

*On screen:* Section MC: the clock of S₃, where the reflections' squares fold onto the identity, and the race between the three families.

*Board:* **the same fold in S3** — every reflection squares to the identity

> The same fold decides another race: the families of minus twenty-three. Each prime acts on the roots of x cubed minus x minus one as a symmetry of a triangle. Every reflection squares to the identity, so the principal family runs three behind and never leads, up to ten million.

## 18. why it matters

*On screen:* The same picture, closing in on the fold.

*Board:* **why it matters** — one fold for both races

> Why it matters: Chebyshev saw the bias in 1853, and Rubinstein and Sarnak explained it with the zeros. What is new is one picture for both races: when four elements square to the identity, the identity's class falls three behind.

## 19. five discoveries

*On screen:* Back to the Sacks saddle from above.

*Board:* **five discoveries** — a surface, a Lie group, a plane of mixtures and a fold

> None of this finds primes faster than a sieve, and none of it proves the Riemann hypothesis. What the search found is geometry: a surface, a Lie group, a plane of mixtures and a fold, where classical facts that looked separate turn out to be one picture.
