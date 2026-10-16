# Long-form segments: scripts

The horizontal long-form segments (1920 × 1080): the five discoveries (xs1 … xs5) and the zeros inside E8 (xz1 … xz5). This file is written by `python3 Tools/video_series.py --doc` from [`Tools/video_series_spec.py`](../../Tools/video_series_spec.py), which also writes the chapters into [`experiments.json`](../../Assets/Resources/Narration/experiments.json). Narrate a segment with `Tools/generate_narration.py --script experiments --only xs1`, re-time its cues to the narration with `Tools/video_series.py`, and record it with `Tools/render_chapters.py --script experiments --chapters xs1 --shapes horizontal`.

## xs1: The saddle is a wave

*discovery 1 of 5:  two master waves on the Sacks spiral*  ·  18 lines, 741 words

1. Claude, an AI math research assistant, and I went looking for the geometry hidden in the counting numbers. We begin with the plainest picture of them, the number line: one dot for each number, the primes in gold. Two, three, five, seven, eleven, thirteen.
   *Board:* **the number line** — one dot for each counting number;  the primes in gold
2. Over this line the zeros of zeta draw a wave. The first zero, at height fourteen point one three, adds a single cosine. Its crests tick on the logarithm of n: at seven point four, eleven point five, eighteen and twenty-eight, each about one and a half times the last.
   *Board:* **one zero, one wave** — −cos(14.13 · log n):  its crests tick on log n
   *Callouts:* 7.4; 11.5; 18.0; 28.1
3. Add the waves of more zeros: ten, a hundred, a thousand, ten thousand. They agree at the primes and cancel between them, and the sum sharpens into spikes, one at every prime and at every power of a prime.
   *Board:* **adding zeros** — they agree at the primes and cancel between them
4. Seven is a prime, and it has a spike. Eight is two cubed, a power of two, and it has one too; so does nine, three squared. But ten, two times five, has no spike, and neither does twelve, two squared times three.
   *Board:* **spikes at the primes and prime powers** — and nowhere else
   *Callouts:* 7:  prime; 8 = 2³; 9 = 3²; 10 = 2·5:  no spike; 12 = 2²·3:  no spike
5. This is the cosine wave of the home view, drawn the way a mathematician would draw it, along the number line. It is the explicit formula of Riemann and von Mangoldt: added up, the zeros spike exactly at the prime powers.
   *Board:* **the prime master wave** — 10 000 zeros:  the explicit formula, on the number line
6. Now wind the number line into a spiral, one full turn for every step of the square root. Each number keeps its place along the line as it curls, and one, four, nine, sixteen, twenty-five, the squares, come to rest on a single ray. This is the Sacks spiral.
   *Board:* **winding the line** — one turn for each step of √n:  the Sacks spiral
   *Callouts:* 16 = 4²; 25 = 5²
7. The wave comes along. On the spiral it is the curtain of the home view, a spike on every prime and every prime power, rising out of the plane; ninety-seven, a prime, and sixty-four, two to the sixth.
   *Board:* **the master wave on the Sacks spiral** — the home view's curtain
   *Callouts:* 97:  prime; 64 = 2^6
8. Each zero's wave on its own looks very different here. Take the hundredth zero alone. Its crests still tick on the logarithm, but the spiral turns on the square root, so they drift from turn to turn and wind into arms.
   *Board:* **one zero's wave on the spiral** — the 100th zero:  −cos(236.5 · log n)
9. Now count divisors instead of primes. Voronoi found in nineteen oh four that the count of divisors has waves of its own. Laid on a number line, his first wave ticks on the square root of n: its crests spread apart, but only slowly, at eight point six, eleven point eight, fifteen point five.
   *Board:* **Voronoi's first wave** — sin(4π√n − π/4):  it ticks on √n
   *Callouts:* 8.6; 11.8; 15.5
10. Add more of his waves, ten, a hundred, four thousand, and they too sharpen into spikes. But now there is a spike at every whole number, and each one stands as tall as its number's count of divisors.
   *Board:* **Voronoi's divisor wave** — a spike at every n, as tall as its count of divisors
11. Seven, a prime, has two divisors, one and seven, so its spike is short. Twelve has six: one, two, three, four, six and twelve. Sixteen, two to the fourth, has five.
   *Board:* **the spikes count divisors** — 7:  2   ·   12:  6   ·   16:  5
   *Callouts:* 7:  2 divisors; 12:  6 divisors; 16 = 2^4:  5 divisors
12. The count comes from the prime factors: add one to each exponent, and multiply. Twelve is two squared times three, so three times two, six. Thirty is two times three times five, two times two times two, eight. And thirty-six, two squared times three squared, has three times three, nine.
   *Board:* **count the divisors from the factors** — n = p^a q^b …  has  (a + 1)(b + 1) …  divisors
   *Callouts:* 12 = 2²·3:  3·2 = 6; 30 = 2·3·5:  2·2·2 = 8; 36 = 2²·3²:  3·3 = 9
13. Wind this line up the same way, one turn for every step of the square root.
   *Board:* **winding Voronoi's line** — the same Sacks spiral
14. Now take his sum back down to its first wave. On the spiral it stands still: two crests on every turn, at the same two angles turn after turn, beside the ray of the squares, like sixty-four, and the ray of the pronic numbers, like seventy-two, eight times nine.
   *Board:* **Voronoi's first wave on the spiral** — two crests per turn, on the same two rays
   *Callouts:* 64 = 8²; 72 = 8·9
15. Bend the plane into the saddle that the Sacks spiral is drawn on. It falls twice and rises twice on every turn, its valleys along those same two rays. The saddle has the shape of Voronoi's first wave, turned over.
   *Board:* **the saddle** — down twice and up twice on every turn:  Voronoi's first wave, turned over
16. Now build both waves back up on the saddle. On the left, the zeros of zeta light the primes. On the right, the whole numbers' own waves count every divisor.
   *Board:* **two master waves on the saddle** — ζ's zeros light the primes  ·  the whole numbers count divisors
17. The difference is the clock. The zeros' waves tick on the logarithm of n, so they drift across the spiral. Voronoi's waves tick on the square root, the spiral's own clock, so each stands still, and the first is the saddle.
   *Board:* **two clocks** — ζ's zeros tick on log n  ·  Voronoi's waves tick on √n
18. Voronoi's formula is from nineteen oh four, and the explicit formula for the primes from Riemann and von Mangoldt. What Claude and I add is the picture: the saddle under the Sacks spiral is the divisor count's first wave, and both master waves ride the same spiral.
   *Board:* **what is new** — the saddle is Voronoi's first wave;  both master waves ride one spiral

## xs2: E8 sets the vortices

*discovery 2 of 5:  the charges of zeta's partial-sum path are E8's Coxeter traces*  ·  10 lines, 423 words

1. Discovery two. Claude and I drew zeta's series as a path of partial sums: one arrow for every counting number n, one over root n long, each turning from the last by t times the logarithm of n over n minus one.
   *Board:* **the path of partial sums** — one arrow n^−½ − it for every counting number, laid end to end
2. Raise t, and the path winds up. At the first zero, t equal to fourteen point one three, its end lands on zero. Higher still, at the hundred and fifty-ninth zero, three hundred thirty-three point six, the path curls into vortices, with straight spokes between them.
   *Board:* **winding up** — the first zero, t = 14.13;  the 159th, t = 333.6
   *Callouts:* ζ = 0
3. The spokes sit near n equal to t over two pi k: fifty-three, twenty-seven, eighteen. Crossing spoke k, the path moves by the k-th term of zeta's reflected series, so every vortex belongs to a counting number.
   *Board:* **spoke k at n ≈ t / 2πk** — t = 333.6:  53,  27,  18, …
   *Callouts:* spoke 1:  n ≈ 53; spoke 2:  n ≈ 27; spoke 3:  n ≈ 18
4. Now keep only the arrows of numbers on E eight's eight gold hours, the ones with no factor two, three or five. The other arrows shrink away, and the path re-forms, with a spoke for every k.
   *Board:* **the gold path** — only n with no factor 2, 3 or 5:  E8's eight hours of 30
5. Each spoke of this gold path carries a charge, the jump the path makes across it. Measured at t equal to five thousand, the charge depends only on the common factor of k and thirty.
   *Board:* **the charge of spoke k** — the gold path's jump across spoke k, measured at t = 5000
6. Spoke one: minus one. Two: one. Three: two. Five: four. Six: minus two. Ten: minus four. Past twelve the spokes crowd together and the measurement drifts: spoke fifteen reads minus nine and a half, against minus eight.
   *Callouts:* k = 1:  −1; 2:  1; 3:  2; 5:  4; 6:  −2; 10:  −4; 15:  −9.5,  tr C^15 = −8
7. These are the traces of E eight's Coxeter element, the rotation by a thirtieth of a turn. Its eight eigenvalues sit on a circle at E eight's exponents. Raise them to the power k and lay the eight unit steps end to end: a little path of eight arrows, ending at the trace.
   *Board:* **tr C^k:  E8's Coxeter element** — 8 eigenvalues e^2πimk/30, m = 1, 7, 11, 13, 17, 19, 23, 29
8. For k equal to one, the eight steps nearly cancel, and end at minus one. For five, they pair off at sixty degrees: four. For fifteen, all eight point the same way: minus eight. These are the charges of the spokes, to within a quarter, up to spoke twelve.
   *Callouts:* nearly cancel:  −1; pairs at 60°:  4; all aligned:  −8
9. Added over the thirty hours, the traces rebuild the sieve: one on the gold hours, and zero on the rest. Seven gets one; six gets zero. E eight's Coxeter element is the sieve of two, three and five, written as a spectrum.
   *Board:* **the sieve as a spectrum** — (1/30) Σ_k tr C^k e^2πikn/30 = 1 on the gold hours, 0 off them
   *Callouts:* 7:  1; 6 = 2·3:  0
10. Why it matters: a symmetry of eight dimensions sets the sign and the strength of every vortex of the gold path. The identity behind it is Ramanujan's, from nineteen eighteen. What Claude and I add is watching E eight decide the vortices of zeta's path.
   *Board:* **what is new** — Ramanujan's sum c_30(k) = tr C^k sets every vortex of the gold path

## xs3: Where the zeros live in E8

*discovery 3 of 5:  a zero is a coil up E8's factor funnel*  ·  10 lines, 433 words

1. Discovery three. Claude and I asked where zeta's zeros live in E eight's layout of the counting numbers. Here it is: every number up to four hundred at its hour on E eight's wheel of thirty, with a column as tall as its logarithm, cut at its prime factors.
   *Board:* **E8's factor funnel** — n at its hour n mod 30, its column as tall as log n, cut at its prime factors
2. That is too much at once, so grey it out and light one number at a time. Seven is a prime, at hour seven: one piece, log seven tall. Six is two times three, at hour six: a piece log three tall, and on top of it one log two tall.
   *Board:* **one number at a time** — 7:  prime, one piece log 7   ·   6 = 2·3:  log 3 + log 2
3. Now bring in the path of partial sums from discovery two, at the first zero, t equal to fourteen point one three, and lay it on the funnel's floor, starting at the axis. Its arrows wind round and its tail brings it back to the axis.
   *Board:* **the path at the first zero, on the floor** — t = 14.13:  its arrows n^−½ − it close on the axis
4. Lift each arrow to the height of its own number. Arrow n rises to log n, the top of n's column; its tail moves onto the axis, and it keeps its direction. Arrow one stays on the floor, arrow two rises to log two, arrow six to log six.
   *Board:* **lift arrow n to the height log n** — its direction stays:  γ·log n
5. Stacked, the arrows are the hands of a clock that turns by gamma for every unit of height, and a coil runs through their tips, one turn every point four four. That coil is the zero, wound up E eight's funnel.
   *Board:* **a zero is a coil** — one turn every 2π/γ = 0.44 of height
6. And the coil reads the prime factors. Six is two times three, so log six is log two plus log three, and the hand at six has turned the turn of two plus the turn of three.
   *Board:* **the hand of 6 = the hand of 2 + the hand of 3** — γ·log 6 = γ·log 2 + γ·log 3
7. Every zero is such a coil. The next two, twenty-one point zero two and twenty-five point zero one, turn every hand further, the higher ones more: tighter coils.
   *Board:* **the next zeros** — γ = 21.02,  25.01:  tighter coils
8. Add the coils of ten thousand zeros. At the height of a prime or a prime power they agree and make a tooth of log p, reaching to the top of that prime's column; everywhere else they cancel. Seven, eight and nine are lit; six and ten stay dark.
   *Board:* **added up, the coils light every prime power** — a tooth of log p;  nothing at the composites
   *Callouts:* 7; 8 = 2³; 9 = 3²; 6:  dark; 10:  dark
9. Zeta's zeros fix a prime's height but not its hour. E eight's field has seven more L-functions, and their zeros fix the hour: together, the eight put every prime on its spoke. Forty-nine, seven squared, lights on hour nineteen.
   *Board:* **eight L-functions, eight spokes** — ζ's zeros give the height, the other seven the hour
   *Callouts:* 49 = 7²:  hour 19
10. Why it matters: the zeros are not points in E eight's stack. They are frequencies of its height, and every coil reads the prime factors. The explicit formula is classical; what Claude and I add is where each zero sits in E eight's funnel.
   *Board:* **what is new** — a zero is a coil up E8's funnel, turning with the logarithm of every number

## xs4: What keeps zeros on the line

*discovery 4 of 5:  symmetry is not enough*  ·  8 lines, 301 words

1. Discovery four. Discriminant minus twenty-three has three families of quadratic forms, and each family is a lattice. A number lies on a family when the lattice has a point at that distance squared from the centre.
   *Board:* **three families, three lattices** — x² + xy + 6y²  ·  2x² + xy + 3y²  ·  2x² − xy + 3y²
2. Fifty-nine, a prime, lies on the first family only. Thirteen, a prime, lies on the other two. But six, two times three, lies on all three: a composite shows up on more than one family.
   *Callouts:* 59:  4 points; 13; 6 = 2·3:  on all three
3. Each family has its own zeta function, counting its lattice points by distance, and each has Riemann's symmetry. Yet their zeros leave the critical line. One sits at real part one point zero zero seven, where no function with an Euler product can vanish.
   *Board:* **each family's own zeta function** — red: zeros off the critical line Re s = ½
   *Callouts:* 0.953 + 16.29i; 1.007 + 22.57i
4. The class group's characters mix the families into two L-functions with Euler products, and those two keep every zero on the line: forty-four and forty-two zeros, all gold.
   *Board:* **the class group's L-functions** — (Z_1 + 2Z_2)/2 and (Z_1 − Z_2)/2:  every zero on the line
   *Callouts:* 44 on the line; 42 on the line
5. Now mix the two families with any weight lambda. Every mixture keeps the functional equation, and one curve holds all their zeros on the line at once: a mixture's zeros are where the curve crosses its height lambda.
   *Board:* **every mixture at once** — the zeros of Λ_1 + λΛ_2 on the line:  where the curve meets the height λ
6. Counted in full, up to height fifty, only two small windows of mixtures keep every zero on the line, and both surround the class group's L-functions, at lambda equal to two and to minus one.
   *Board:* **no zero off the line:  two small windows** — λ from 1.99 to 2,  and from −1.02 to −0.88
   *Callouts:* λ = 2:  ζ(s) L(s, χ<sub>−23</sub>); λ = −1:  L(η(z) η(23z), s)
7. With five families, for minus forty-seven, the mixtures fill a plane. Colour each by its zeros off the line, and gold appears only in small islands: round the three L-functions, and one more island with no L-function in it.
   *Board:* **five families:  a plane of mixtures** — gold:  no zero off the line up to t = 50
   *Callouts:* ζ(s) L(s, χ<sub>−47</sub>); L<sub>1</sub>; L<sub>2</sub>; no L-function here
8. Why it matters: symmetry alone does not keep zeros on the line. Davenport and Heilbronn proved that in nineteen thirty-six. As far as anyone has checked, the functions that keep them there have Euler products. What Claude and I add is seeing every mixture at once.
   *Board:* **what is new** — symmetry is not enough:  every mixture seen at once

## xs5: Chebyshev's bias is a fold

*discovery 5 of 5:  why two of E8's spokes run behind*  ·  9 lines, 319 words

1. Discovery five. Every prime above five lies on one of E eight's eight gold spokes. Race them up to ten million, and two of the spokes, one and nineteen, run behind nearly all the way.
   *Board:* **the prime race on E8's eight spokes** — the primes up to 10^7, counted and rebuilt from the zeros
   *Callouts:* spokes 1 and 19:  about −3; the other six:  about +1
2. They are ahead of their share only zero point three and one point four percent of the way, and never in the lead. Below, six thousand seven hundred and seventy-one zeros of E eight's L-functions rebuild every curve.
   *Board:* **spokes 1 and 19 run behind** — ahead of their share 0.3 % and 1.4 % of the way;  rebuilt from 6 771 zeros
3. The reason is a fold. The zeros count prime powers, not only primes. On a logarithmic clock of thirty, an arc runs from every prime to its square, and squaring folds every spoke onto one or nineteen.
   *Board:* **the reason:  a fold** — squaring sends spokes 1, 11, 19, 29 to 1, and 7, 13, 17, 23 to 19
4. Seven squared is forty-nine, on spoke nineteen. Eleven squared, one hundred and twenty-one, lands on one. Thirteen squared, one sixty-nine, on nineteen. Twenty-nine squared, eight forty-one, on one.
   *Callouts:* 7² = 49:  spoke 19; 11² = 121:  spoke 1; 13² = 169:  spoke 19; 29² = 841:  spoke 1
5. Every square of a prime above five lands on spoke one or nineteen: all four hundred and forty-three of them up to ten million. A count of primes leaves out half a prime for each. Put them back, and the race is fair.
   *Board:* **put the squares back:  a fair race** — 218 squares on spoke 1,  225 on spoke 19,  none elsewhere
6. The same fold turns up in a second race. For minus twenty-three, a prime's family is decided by how it acts on the three roots of x cubed minus x minus one.
   *Board:* **x³ − x − 1:  three roots** — r_1 = 1.3247,  r_2, r_3 = −0.6624 ± 0.5623i
7. Fifty-nine splits it into three factors: every root stays put, the identity. Two leaves it whole: the roots turn round, a rotation. Five gives one root: two roots swap, a reflection.
   *Board:* **how a prime acts on the roots** — identity · rotation · reflection
8. And a reflection done twice is the identity. So the squares of half of all primes fall in the principal family, which runs behind its share and is never in the lead.
   *Board:* **a reflection, twice:  the identity** — the inert primes' squares land in the principal family
9. Why it matters: one fold explains both races. Chebyshev noticed the bias in eighteen fifty-three, and Rubinstein and Sarnak measured it in nineteen ninety-four. What Claude and I add is one picture for E eight's wheel and for the families of minus twenty-three.
   *Board:* **what is new** — one fold:  E8's wheel and the families of −23
   *Callouts:* the principal family:  never in the lead

## xz1: The path of partial sums

*the zeros inside E8, part 1:  how ζ is built, link by link*  ·  9 lines, 377 words

1. Claude, an AI math research assistant, and I set out to find where the zeros of zeta live among the counting numbers. First, a refresher on how zeta is built: one arrow for every counting number, laid end to end. Mathematicians call this the path of partial sums.
   *Board:* **the path of partial sums** — ζ(s) = 1 + 1/2^s + 1/3^s + …,  one arrow per term
2. Take the height t equal to one. The first arrow is one unit long and points to the right. The second is one over the square root of two long, and it turns away from the first by the logarithm of two: forty degrees.
3. The third is one over root three long, and turns a further log of three halves, twenty-three degrees. Its direction is the sum of the turns: log three, sixty-three degrees from the first. Every direction is a logarithm.
4. Four, five, six, seven, eight: shorter and shorter arrows, turning less and less, seventeen degrees, then thirteen, ten, nine and eight, because the logarithm climbs more and more slowly.
5. Now raise the height t. Every angle is t times a logarithm, so the path winds up tighter. The arrows go on forever, but past a point the rest add up to a simple tail, drawn in orange, and the end of that tail is zeta of one half plus i t.
   *Board:* **raising t** — every angle is t × a logarithm;  the tail adds all the arrows beyond
6. At t equal to fourteen point one three, the end lands exactly on zero. That is the first zero of zeta: all the arrows, from one to infinity, cancel out.
   *Board:* **the first zero** — t = 14.1347:  ζ(½ + it) = 0
   *Callouts:* ζ(½ + 14.13i) = 0
7. Now let t run on, and stand the plane up on a t axis, so that it climbs as t grows. The end of the path leaves a trail, the curve of zeta along the critical line. It touches the axis at every zero: twenty-one point zero two, twenty-five point zero one, thirty point four two.
   *Board:* **the curve of ζ on the critical line** — the plane climbs the t axis;  the curve touches it at every zero
8. Between the zeros, the curve swings out and comes back. Riemann's hypothesis says that every zero of this kind lies on this line, real part one half. Trillions have been checked, and every one does, but no one has proved that all of them must.
   *Board:* **Riemann's hypothesis** — every nontrivial zero of ζ has real part ½
9. Higher up, the path needs more and more arrows before the tail takes over, and it begins to curl into knots. Those knots are its vortices, and the next part follows them up the axis.
   *Board:* **the vortices begin** — knots in the path, ringed

## xz2: How the vortices develop

*the zeros inside E8, part 2:  the path of partial sums high up*  ·  7 lines, 234 words

1. Climbing higher, the path needs about t over pi arrows before its tail takes over, and it curls into knots: vortices. The rings mark their eyes, numbered from the far end of the path.
   *Board:* **vortices** — about t/2π of them at height t
2. Each time t over two pi passes another whole number, a new vortex is born near the start of the path, and the older ones move out along it.
   *Board:* **a vortex is born** — each time t/2π passes a whole number
3. Between the vortices the path runs straight. Those straight runs are the spokes, where each arrow turns by a whole number of full turns from the last. They sit near n equal to t over two pi k: at the hundred and fifty-ninth zero, fifty-three, twenty-seven and eighteen.
   *Board:* **spoke k at n ≈ t / 2πk** — where each arrow turns a whole number of turns from the last
   *Callouts:* spoke 1:  n ≈ 53; spoke 2:  n ≈ 27; spoke 3:  n ≈ 18
4. Each vortex has an eye, and each eye is a mirror image: the path's own k-th vertex, reflected through zeta's phase. The crosses mark those mirror points, and they land inside the rings.
   *Board:* **every eye is a mirror** — eye of vortex k = the path's k-th vertex, reflected through ζ's phase
5. Now let the plane climb on, and at every zero keep each eye where it was. The eyes build a tower; vortex one, in cyan, always sits on a circle of radius one.
   *Board:* **the eyes through the zeros** — kept at every zero:  vortex 1 on a circle of radius 1
6. Look straight down the axis. The eyes of ten thousand zeros, stacked: the unit circle of vortex one, and rings for all the others.
   *Board:* **all 10 000 zeros, down the axis** — the vortices belong to the counting numbers
7. So the vortices develop as t climbs. New ones are born at the start, the spokes slide outward, and every vortex belongs to a counting number: a reflection of the path's own beginning.
   *Board:* **how the vortices develop** — one per counting number, each a mirror of the path's start

## xz3: From a zero to E8's heights

*the zeros inside E8, part 3:  the turn of n is γ times log n*  ·  9 lines, 333 words

1. This is E eight's factor funnel. Every counting number sits at its hour on the wheel of thirty, with a column as tall as its logarithm. Seven, at hour seven: one point nine five. Thirty, two times three times five, at hour zero: three point four.
   *Board:* **E8's factor funnel** — n at its hour n mod 30, its column as tall as log n
   *Callouts:* log 7 = 1.95; 30 = 2·3·5:  log 30 = 3.40
2. Grey it out, and light the powers of two. Two is one piece, log two tall. Four is two pieces, eight is three, sixteen is four: each column is its prime factors' logarithms, stacked.
   *Board:* **a column is its prime factors, stacked** — log 16 = log 2 + log 2 + log 2 + log 2
3. Now the path of partial sums at the first zero, t equal to fourteen point one three, on the funnel's floor. Arrow one, arrow two, arrow three, and on, each one over root n long, each turned by t times log n.
   *Board:* **the path at the first zero** — arrow n:  1/√n long, turned 14.13 · log n
4. Its tail closes it on the axis: that is what a zero is. Now lift every arrow to the height of its own number: arrow two to log two, arrow three to log three, arrow four to log four, twice as high as two.
   *Board:* **lift arrow n to the height log n** — keeping its direction:  −14.13 · log n
5. Each arrow becomes a hand, pointing out from the axis at the top of its number's column. Through their tips runs a coil that turns by gamma for every unit of height: the zero, wound up E eight's funnel.
   *Board:* **the coil:  the hands at every height** — turning 14.13 per unit of height, one turn every 0.44
6. The coil multiplies by adding heights. Four is two times two, so its hand has turned twice the turn of two. Six is two times three: the turn of two plus the turn of three.
   *Board:* **multiplying is adding heights** — γ·log 4 = 2 γ·log 2   ·   γ·log 6 = γ·log 2 + γ·log 3
7. Every zero winds its own coil. Twenty-one point zero two turns each hand further, the higher ones more; twenty-five point zero one, further still.
   *Board:* **other zeros, other coils** — γ = 21.02,  25.01
8. At a height that is not a zero, like eighteen, the hands turn just the same, but drop them back onto the floor and the path does not close: its tail ends away from the axis.
   *Board:* **t = 18:  not a zero** — the arrows dropped back into the path:  it ends away from the axis
9. That is the bridge between a zero and E eight's layout: the zero's height, times the height of n's column, is the turn of n's arrow. Each prime factor adds its own turn.
   *Board:* **the bridge** — the turn of n at a zero = γ × log n = γ × (the sum of log p over its prime factors)

## xz4: Lamps and spokes

*the zeros inside E8, part 4:  all the coils together*  ·  11 lines, 377 words

1. Every zero winds its own coil up the funnel. Here are the first three, fourteen point one three, twenty-one point zero two and twenty-five point zero one, over the greyed-out columns.
   *Board:* **every zero, a coil** — γ = 14.13,  21.02,  25.01
2. Take all ten thousand five hundred zeros, and look at one height: the top of seven's column. Each coil points somewhere there; on average they lean towards minus one, by minus point one one five, exactly as Landau's formula predicts for a prime.
   *Board:* **at the height of 7, a prime** — 10 500 coils lean to −1:  mean −0.115,  Landau −0.115
3. At the top of six's column, two times three, the coils point every way, and their mean is zero. The zeros lean only at the powers of primes.
   *Board:* **at the height of 6 = 2·3** — the coils cancel:  mean 0.000
4. So add up all the coils at every height, with zeta's pole, and draw the sum out to the side. What is left is a comb: a tooth of exactly log p at every prime power, reaching the top of that prime's column, and nothing at the composites.
   *Board:* **added up, the coils light every prime power** — a tooth of log p, reaching the top of p's column
5. Seven, eight and nine are lit: seven a prime, eight two cubed, nine three squared. Six, ten and twelve stay dark.
   *Callouts:* 7; 8 = 2³; 9 = 3²; 6:  dark; 10:  dark; 12:  dark
6. But the comb is the same in every direction. Spin it round the axis, and each tooth becomes a ring through its prime's column. Zeta's zeros know how high a prime sits, not its hour on the wheel.
   *Board:* **spun round the axis:  a ring per prime power** — ζ's zeros fix the height, not the hour
7. E eight knows more. Its Coxeter element makes it a lattice over the thirtieth roots of unity, and their field has eight L-functions, one for each character of E eight's exponent group. Zeta is the first.
   *Board:* **E8's field has eight L-functions** — one per character of (Z/30)*:  ζ is the first
8. The zeros of each one light the same prime powers, but turned by its character: back and forth for the real ones, and all four ways for the others.
   *Board:* **each turns the primes by χ(p)** — ±1 for the real characters, ±1 and ±i for the rest
9. Turn each set back by its character at hour h, and add all eight. Only the primes on spoke h survive. Spoke one. Seven. Eleven. Thirteen. Seventeen. Nineteen. Twenty-three. Twenty-nine.
   *Board:* **turned back by χ(h) and added** — only the primes on spoke h survive
10. Forty-nine lights on spoke nineteen, as seven squared. Eleven and forty-one share spoke eleven. Seven thousand four hundred zeros sort every prime up to four hundred onto its spoke.
   *Board:* **7 420 zeros up to t = 1000** — ζ's give the height, the other seven the hour
   *Callouts:* 49 = 7²:  hour 19; 11; 41 = 11 + 30
11. So the zeros are not points in the stack. They are frequencies of its height. Added up, they rebuild the prime powers, and turned by E eight's characters, they put each prime on its spoke. They reconstruct the primes; they do not predict them.
   *Board:* **the zeros are frequencies of the height** — they rebuild the primes:  they do not predict them

## xz5: 240 roots, infinitely many numbers

*the zeros inside E8, part 5:  E8's clock and E8's shells*  ·  10 lines, 348 words

1. E eight has two hundred and forty roots, shown here in its Coxeter plane as eight rings of thirty. So how can so few points hold infinitely many counting numbers?
   *Board:* **E8:  240 roots** — 8 rings of 30 in the Coxeter plane
2. They don't have to. The roots set E eight's clock: its Coxeter element turns the plane by a thirtieth of a turn, so the wheel has thirty hours, and eight of them, E eight's exponents, are gold.
   *Board:* **E8's clock:  30 hours** — the 8 gold hours:  1, 7, 11, 13, 17, 19, 23, 29
3. Drop every counting number onto the wheel at its hour, n mod thirty, one turn further out each time round. The spiral never ends.
   *Board:* **every n at its hour** — n mod 30:  the hours repeat, the radius grows
4. Grey it out and follow one hour. Seven, thirty-seven, sixty-seven and ninety-seven all land on hour seven, each one turn further out: all four are primes. One hundred and twenty-seven, too.
   *Board:* **one hour:  7, 37, 67, 97, 127** — n ≡ 7 (mod 30)
   *Callouts:* 7; 37 = 7 + 30; 67 = 7 + 2·30; 97 = 7 + 3·30; 127 = 7 + 4·30
5. Hour six holds six, thirty-six, sixty-six and ninety-six: every one a multiple of six, off the gold spokes. The clock sorts out two, three and five; the rest it cannot see.
   *Board:* **hour 6:  6, 36, 66, 96** — multiples of 6, off the gold spokes
   *Callouts:* 6 = 2·3; 96 = 2^5·3
6. And E eight itself is not two hundred and forty points. It is an infinite lattice, and the roots are only its first shell: the lattice points at distance root two from the centre.
   *Board:* **E8 is a lattice:  shells** — the 240 roots are its first shell, at distance √2
7. The next shell, at distance two, holds two thousand one hundred and sixty points. Then six thousand seven hundred and twenty, then seventeen thousand five hundred and twenty. Every counting number n has a shell of its own, at distance root two n.
   *Board:* **a shell for every counting number** — shell n:  the points at distance √2n:  240, 2 160, 6 720, 17 520, …
8. And each shell knows its number's divisors: it holds two hundred and forty times the sum of the cubes of n's divisors. Seven is prime: one plus three hundred and forty-three, times two forty, makes eighty-two thousand five hundred and sixty.
   *Board:* **shell n holds 240 σ_3(n) points** — σ_3(n):  the sum of the cubes of n's divisors
9. Six has four divisors, one, two, three and six, so its cubes are one, eight, twenty-seven and two hundred and sixteen: sixty thousand four hundred and eighty points, with cubes in between for its factors.
   *Board:* **6:  1 + 8 + 27 + 216 = 252** — 240 × 252 = 60 480
10. Counted over every shell at once, the points of E eight give its own zeta function: two hundred and forty times zeta of s, times zeta of s minus three. A finite symmetry, and infinitely many shells, one for every counting number.
   *Board:* **Σ 240 σ_3(n) n^−s = 240 ζ(s) ζ(s − 3)** — a finite symmetry, a shell for every counting number
