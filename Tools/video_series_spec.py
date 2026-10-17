"""
The long-form horizontal segments (1920 × 1080): the five discoveries (xs1 … xs5) and the zeros inside E8 (xz1 … xz5),
each a chapter of Assets/Resources/Narration/experiments.json built by Tools/video_series.py.

A line is {text, camera, actions, pause}. The camera is an orbit cue as a dict (see CameraRig.OrbitCue); az "+" continues
the previous line's turn from where it stopped. An action "@word|exp:…" runs as the narrator says the word (its k-th
occurrence with "@word#k"), "@3.5|exp:…" 3.5 s into the line.

Callouts (ExperimentCallouts.cs): exp:callKIND:TARGET:DX,DY[,FRAC[,RRGGBB]]:TEXT puts TEXT off in the screen direction
(DX, DY) and grows an arrow from it to the target, which blooms white as it lands. Kinds: num (a number on the surface),
wave (the top of a number's spike, TARGET N or N@prime|divisor), fun (a column top in the zeros funnel), clock and
clocklink (the bridge's clocks), bar and wheel (section IA), vertex (the live chain), fold (section MB), e8 (the wheel's
spiral), zeta (ζ(½ + it) on the curve), at (a point X,Y,Z from the centre).
"""

GOLD, RED, BLUE, GREEN, WHITE, CYAN = "ffd973", "ff6b5a", "66c4ff", "7dff8f", "ffffff", "59e8d9"


def C(at="0,0,0", fit="50,30", el=89.5, az=-90, spin=0, rise=.05, on="centre", **kw):
    """An orbit cue: look at `at` (from the centre of `on`), framing a fit = W,H box; 'A>B' moves over span seconds."""
    d = dict(on=on, at=at, fit=fit, el=el, az=az, spin=spin, **kw)
    d["raise"] = rise
    return d


def call(kind, target, dx, dy, text, frac=None, col=None):
    o = f"{dx},{dy}"
    if frac is not None or col:
        o += f",{frac if frac is not None else .12}"
    if col:
        o += f",{col}"
    return f"exp:call{kind}:{target}:{o}:{text}"


def sup(x):
    """A superscript the label font has (², ³ and ¹ are in it; the rest are drawn as rich-text <sup>)."""
    return f"<sup>{x}</sup>"


def at(word, act):
    return f"@{word}|{act}"


def say(title, sub=""):
    return f"exp:say:{title}|{sub}" if sub else f"exp:say:{title}"


# ---------------------------------------------------------------------------------------------------------------
# 1. the saddle is a wave: one graph over the number line, coiled into the Sacks spiral, stood up, bent into the saddle
# ---------------------------------------------------------------------------------------------------------------

import math as _m


def arc(n):
    """How far along the line (and the spiral) the number n sits: the Sacks spiral's arc length, πR per unit of n."""
    w = _m.sqrt(n)
    a = 2 * _m.pi * w
    return 1.45 * ((w / 2) * _m.sqrt(a * a + 1) + _m.asinh(a) / (4 * _m.pi))


def xp(n):
    return round(100000 + arc(n), 1)


def tw(key, value, seconds, how=""):
    return f"exp:tw:{key}:{value}:{seconds}" + (f":{how}" if how else "")


def BOX(x0, x1, y0, y1, aspect=16 / 9):
    """Fit and look point that keep the box [x0, x1] × [y0, y1] (world units, from the centre, seen from straight above)
    between the caption board and the captions: the safe band is .25 … .82 of the frame's height and .03 … .97 of its width,
    so the box sits at .535 of the height (raise .035)."""
    ch, cw = y1 - y0, x1 - x0
    hf = max(ch / .57, cw / (.94 * aspect))
    fh = hf / 1.06
    return dict(at=f"{(x0 + x1) / 2:.1f},{(y0 + y1) / 2:.1f},0", fit=f"{fh * aspect:.1f},{fh:.1f}")


def TOP(*boxes, **kw):
    """A shot from straight above framing a box in the safe band; with two boxes it eases from the first to the second."""
    b = [BOX(*x) for x in boxes]
    return C(at=">".join(x["at"] for x in b), fit=">".join(x["fit"] for x in b), el=89.5, az=-90, spin=0, rise=.035, **kw)


def LINE(n, fit="170,96", y=3):
    """The number line seen from straight above, framed round n: the graph is drawn in the plane, up the screen."""
    return C(at=f"{xp(n)},{y},0", fit=fit, el=89.5, az=-90, spin=0, rise=0)


COIL = {"on": "nwboxp", "at": "0,0,0", "el": 89.5, "az": -90, "spin": 0, "span": 6, "raise": .035}   # follows the curling line from above
SPIRAL = dict(at="100000,0,-1", fit="106,59.6", rise=.035)          # the whole spiral (radius ~18) in the safe band


def curl(word, end, h_to=.6, dot_to=.5):
    """Curl the line into the spiral between `word` and `end`, a doubling of the curl at a time, the graph shrinking with it."""
    return [at(word, tw("nw.roll", .004, .8)), at(f"{word}+0.8", tw("nw.roll", 1, f"{{{word}~{end}}}", "geo")),
            at(word, tw("nw.h", h_to, f"{{{word}~{end}}}", "geo")), at(word, tw("nw.dot", dot_to, f"{{{word}~{end}}}", "geo")),
            at(word, tw("nw.labels", 0, 2))]


def uncurl(word, end, h_to=22, dot_to=2):
    """The reverse: the spiral unwinds into the line, the graph growing back."""
    return [at(word, tw("nw.roll", .004, f"{{{word}~{end}}}", "geo")), at(end, tw("nw.roll", 0, .8)),
            at(word, tw("nw.h", h_to, f"{{{word}~{end}}}", "geo")), at(word, tw("nw.dot", dot_to, f"{{{word}~{end}}}", "geo")),
            at(end, tw("nw.labels", 1, 1.5))]


XS1 = dict(
    id="xs1", title="The saddle is a wave", subtitle="discovery 1 of 5:  two master waves on the Sacks spiral",
    setup=["exp:stage", "exp:backdrop:off", "exp:nwinit", tw("nw.vis", 1, 0), tw("nw.labels", 1, 0), tw("nw.dot", 2, 0),
           tw("nw.label", 14, 0), tw("nw.h", 22, 0), tw("nw.terms.p", 1, 0), tw("nw.terms.d", 1, 0), tw("nw.mix", 0, 0),
           tw("nw.stand", 0, 0), tw("nw.wave", 0, 0), tw("nw.count", 0, 0)],
    lines=[
        dict(text="Claude, an AI math research assistant, and I went looking for the geometry hidden in the counting numbers. "
                  "We begin with the plainest picture of them, the number line: one dot for each number, the primes in gold. "
                  "Two, three, five, seven, eleven, thirteen.",
             camera=LINE(16),
             actions=[say("the number line", "one dot for each counting number;  the primes in gold"),
                      "@0.8|" + tw("nw.count", 32, "{went~two}", "linear"),
                      at("two", "exp:nwflare:2"), at("three", "exp:nwflare:3"), at("five", "exp:nwflare:5"),
                      at("seven", "exp:nwflare:7"), at("eleven", "exp:nwflare:11"), at("thirteen", "exp:nwflare:13"), "exp:snap"]),
        dict(text="Over this line the zeros of zeta draw a wave. The first zero, at height fourteen point one three, adds a single "
                  "cosine. Its crests tick on the logarithm of n: at seven point four, eleven point five, eighteen and twenty-eight, "
                  "each about one and a half times the last.",
             camera=LINE(16),
             actions=[at("draw", tw("nw.wave", 1, 2.5)), at("draw", tw("nw.counter", 1, 1)),
                      say("one zero, one wave", "−cos(14.13 · log n):  its crests tick on log n"),
                      at("seven", call("nw", "7.39", 0, 0, "7.4")), at("eleven", call("nw", "11.53", 0, 0, "11.5")),
                      at("eighteen", call("nw", "17.98", 0, 0, "18.0")), at("twentyeight", call("nw", "28.05", 0, 0, "28.1"))]),
        dict(text="Add the waves of more zeros: ten, a hundred, a thousand, ten thousand. They agree at the primes and cancel "
                  "between them, and the sum sharpens into spikes, one at every prime and at every power of a prime.",
             camera=LINE(22, fit="220,124"),
             actions=["exp:uncall", tw("nw.count", 45, 3), tw("nw.terms.p", 10000, "{add~ten#2}", "geo"),
                      say("adding zeros", "they agree at the primes and cancel between them")]),
        dict(text="Seven is a prime, and it has a spike. Eight is two cubed, a power of two, and it has one too; so does nine, "
                  "three squared. But ten, two times five, has no spike, and neither does twelve, two squared times three.",
             camera=LINE(9.5, fit="64,36", y=3),
             # the callouts wait for the graph to finish shrinking (nw.h), so they land on the spikes as drawn
             actions=[tw("nw.h", 7, 1.2), at("seven+1.3", call("nw", "7", -.3, 1, "7:  prime")), at("eight", call("nw", "8", .1, 1, "8 = 2³")),
                      at("nine", call("nw", "9", .6, 1, "9 = 3²")),
                      at("ten", call("nw", "10", -.6, -1, "10 = 2·5:  no spike", frac=.17, col=RED)),
                      at("twelve", call("nw", "12", .6, -1, "12 = 2²·3:  no spike", frac=.17, col=RED)),
                      say("spikes at the primes and prime powers", "and nowhere else")]),
        dict(text="This is the cosine wave of the home view, drawn the way a mathematician would draw it, along the number line. "
                  "It is the explicit formula of Riemann and von Mangoldt: added up, the zeros spike exactly at the prime powers.",
             camera=LINE(75, fit="720,405", y=0),
             actions=["exp:uncall", tw("nw.h", 66, 1.5), tw("nw.count", 150, "{this~end}", "linear"),
                      say("the prime master wave", "10 000 zeros:  the explicit formula, on the number line")]),
        dict(text="Now curl the number line up, one full turn for each step of the square root. Watch the graph curl with it: "
                  "each number keeps its place along the line, and one, four, nine, sixteen, twenty-five, the squares, come to rest "
                  "on a single ray. This is the Sacks spiral.",
             camera=COIL,
             actions=["exp:uncall"] + curl("curl", "ray") +
                     [at("squares", tw("nw.rays", 1, 1.5)),
                      at("ray+0.3", call("nw", "16", 0, 0, "16 = 4²", col=BLUE)), at("ray+0.9", call("nw", "25", 0, 0, "25 = 5²", col=BLUE)),
                      say("curling the line", "one turn for each step of √n:  the Sacks spiral")]),
        dict(text="Now stand the graph up out of the plane. On the spiral it becomes the curtain of the home view: a spike on every "
                  "prime and every prime power; ninety-seven, a prime, and sixty-four, two to the sixth.",
             camera=C(**SPIRAL, el="89.5>40", az=-90, spin=0, span=7),
             actions=["exp:uncall", tw("nw.rays", 0, 1.5), at("stand", tw("nw.stand", 1, "{stand~home}")), at("stand", tw("nw.h", 6, "{stand~home}")),
                      at("ninetyseven", call("nw", "97", 0, 0, "97:  prime")), at("sixtyfour", call("nw", "64", 0, 0, f"64 = 2{sup(6)}")),
                      say("the master wave on the Sacks spiral", "the home view's curtain")]),
        dict(text="One zero's wave on its own looks very different here. Take the hundredth zero alone. Its crests still tick on "
                  "the logarithm, but the spiral turns on the square root, so they drift from turn to turn and wind into arms.",
             camera=C(**SPIRAL, el="40>86", az=-90, spin=0, span=5),
             actions=["exp:uncall", tw("nw.terms.p", 1, "{one~alone}", "geo"), at("alone", tw("nw.first.p", 100, 0)),
                      at("alone", tw("nw.h", 1, 1.5)), at("alone", tw("nw.ribbon", 1, 1.5)),
                      say("one zero's wave on the spiral", "the 100th zero:  −cos(236.5 · log n)")]),
        dict(text="Go back to the first zero's wave, and uncurl the spiral into the number line again: the very first graph "
                  "comes back.",
             camera=COIL,
             actions=[at("first", tw("nw.first.p", 1, 0)), at("first", tw("nw.ribbon", 0, 1.5)), at("uncurl", tw("nw.stand", 0, 2.5))] +
                     uncurl("uncurl", "back#2") + [say("uncurling", "the first zero's wave, back on the number line")]),
        dict(text="Now count divisors instead of primes. Voronoi found in nineteen oh four that the count of divisors has waves of "
                  "its own. His first wave ticks on the square root of n, so its crests spread apart only slowly: eight point six, "
                  "eleven point eight, fifteen point five.",
             camera=LINE(15),
             actions=[at("count", tw("nw.mix", 1, "{count~his}")),
                      at("eight", call("nw", "8.63", 0, 0, "8.6")), at("eleven", call("nw", "11.82", 0, 0, "11.8")),
                      at("fifteen", call("nw", "15.5", 0, 0, "15.5")),
                      say("Voronoi's first wave", "sin(4π√n − π/4):  it ticks on √n")]),
        dict(text="Add more of his waves, ten, a hundred, four thousand, and they too sharpen into spikes. But now there is a "
                  "spike at every whole number, and each one stands as tall as its number's count of divisors.",
             camera=LINE(20, fit="200,112"),
             actions=["exp:uncall", tw("nw.count", 45, 3), tw("nw.terms.d", 4000, "{add~divisors}", "geo"),
                      say("Voronoi's divisor wave", "a spike at every n, as tall as its count of divisors")]),
        dict(text="Seven, a prime, has two divisors, one and seven, so its spike is short. Twelve has six: one, two, three, four, "
                  "six and twelve. Sixteen, two to the fourth, has five.",
             camera=LINE(11.5, fit="64,36", y=3),
             actions=[tw("nw.h", 12, 1.2), at("seven#1+1.3", call("nw", "7", 0, 0, "7:  2 divisors", col=GOLD)),
                      at("twelve", call("nw", "12", 0, 0, "12:  6 divisors")), at("one#2", "exp:nwdivisors:12:ffd973:{one#2~twelve#2}"),
                      at("sixteen", "exp:uncall"), at("sixteen+0.1", call("nw", "16", 0, 0, f"16 = 2{sup(4)}:  5 divisors")),
                      say("the spikes count divisors", "7:  2   ·   12:  6   ·   16:  5")]),
        dict(text="The count comes from the prime factors: add one to each exponent, and multiply. Twelve is two squared times "
                  "three, so three times two, six. Thirty is two times three times five, two times two times two, eight. And "
                  "thirty-six, two squared times three squared, has three times three, nine.",
             camera=LINE(24, fit="170,96"),
             actions=["exp:uncall", tw("nw.h", 22, 1.5), say("count the divisors from the factors", "n = p<sup>a</sup> q<sup>b</sup> …  has  (a + 1)(b + 1) …  divisors"),
                      at("twelve#1+1.5", call("nw", "12", -.5, 1, "12 = 2²·3:  3·2 = 6")),
                      at("thirty", call("nw", "30", -.2, 1, "30 = 2·3·5:  2·2·2 = 8")),
                      at("thirtysix", call("nw", "36", .5, 1, "36 = 2²·3²:  3·3 = 9"))]),
        dict(text="Take his sum back down to its first wave, and curl the line up into the spiral once more.",
             camera=COIL,
             actions=["exp:uncall", tw("nw.count", 150, 1.5), tw("nw.terms.d", 1, "{take~curl}", "geo")] + curl("curl", "end", h_to=1) +
                     [say("curling Voronoi's first wave", "the same Sacks spiral")]),
        dict(text="On the spiral his first wave stands still: two crests on every turn, at the same two angles turn after turn, "
                  "beside the ray of the squares, like sixty-four, and the ray of the pronic numbers, like seventy-two.",
             camera=C(**SPIRAL, el=86, az=-90, spin=0),
             actions=[tw("nw.stand", 1, 1.5), tw("nw.ribbon", 1, 1.5), at("squares", tw("nw.rays", 1, 1.5)),
                      at("sixtyfour", call("nw", "64", 0, 0, "64 = 8²", col=WHITE)),
                      at("seventytwo", call("nw", "72", 0, 0, "72 = 8·9", col=WHITE)),
                      say("Voronoi's first wave on the spiral", "two crests per turn, on the same two rays")]),
        dict(text="Turn it into a zero's wave on the same spiral, and its crests start to drift into arms, because the zeros tick "
                  "on the logarithm. Turn it back: Voronoi's waves tick on the square root, the spiral's own clock, so each one "
                  "stands still.",
             camera=C(**SPIRAL, el=86, az=-90, spin=0),
             actions=["exp:uncall", tw("nw.rays", 0, 1.5), tw("nw.first.p", 100, 0), at("turn", tw("nw.mix", 0, "{turn~drift}")),
                      at("back", tw("nw.mix", 1, "{back~square}")),
                      say("two clocks", "ζ's zeros tick on log n  ·  Voronoi's waves tick on √n")]),
        dict(text="Bend the plane into the saddle that the Sacks spiral is drawn on. It falls twice and rises twice on every "
                  "turn, its valleys along those same two rays. The saddle has the shape of Voronoi's first wave, turned over.",
             camera=C(at="100000,0,-3", fit="106,59.6>56,42", el="86>26", az=-90, spin=2, rise=.04, span=8),
             actions=[at("bend", tw("nw.bend", 1, "{bend~rays}")), at("bend", tw("nw.h", .4, "{bend~rays}")),
                      say("the saddle", "down twice and up twice on every turn:  Voronoi's first wave, turned over")]),
        dict(text="Now build both master waves on the saddle, on this one spiral. The zeros' waves light the primes; turn them "
                  "into the whole numbers' own waves, and every number lights up by its count of divisors.",
             camera=C(at="100000,0,-3", fit="64,50", el=34, az="+", spin=2, rise=.075),
             actions=[tw("nw.ribbon", 0, 1.5), tw("nw.first.p", 1, 0), at("build", tw("nw.mix", 0, 2)),
                      at("build", tw("nw.terms.p", 10000, "{build~light}", "geo")), at("build", tw("nw.terms.d", 4000, "{build~light}", "geo")),
                      at("build", tw("nw.h", 6, 2)), at("turn", tw("nw.mix", 1, "{turn~divisors}")),
                      say("two master waves on one saddle", "ζ's zeros light the primes  ·  the whole numbers count divisors")]),
        dict(text="Voronoi's formula is from nineteen oh four, and the explicit formula for the primes from Riemann and von "
                  "Mangoldt. What Claude and I add is the picture: the saddle under the Sacks spiral is the divisor count's first "
                  "wave, and both master waves ride the same spiral.",
             camera=C(at="100000,0,-3", fit="64,50>70,54", el="34>28", az="+", spin=2, rise=.075, span=14),
             actions=[at("explicit", tw("nw.mix", 0, 3)), at("both", tw("nw.mix", 1, 3)),
                      say("what is new", "the saddle is Voronoi's first wave;  both master waves ride one spiral")], pause=1.4),
    ])

# ---------------------------------------------------------------------------------------------------------------
# 2. E8 sets the vortices: E8's clock beside the path of partial sums, and the blocks of the gold path lifted onto it
# ---------------------------------------------------------------------------------------------------------------

E8W = dict(at="36300,0,0", fit="100,100", el=89.5, spin=0)
PW = 104000          # the path's origin (from the centre); 12 world units per unit of the complex plane
CLOCK = "104040,-9,0"
PATH_SETUP = ["exp:stage", "exp:backdrop:off", "exp:vortexprep", "exp:pwinit", tw("pw.t", 1, 0), tw("pw.n", 0, 0), tw("pw.tail", 0, 0),
              tw("pw.marks", 1, 0), tw("pw.readout", 1, 0)]


def PC(at="26,-12,0", fit="92,54", el=89.5, az=-90, spin=0, rise=.035, **kw):
    """A shot of the partial-sum path's plane (it follows the plane as it climbs the t axis)."""
    return C(on="pathplane", at=at, fit=fit, el=el, az=az, spin=spin, rise=rise, **kw)


def W(x, y, fit, **kw):
    """A shot from straight above, centred at (PW + x, y)."""
    return C(at=f"{PW + x},{y},0", fit=fit, el=89.5, az=-90, spin=0, rise=kw.pop("rise", .035), **kw)


# boxes on the stage (world units from the centre): E8's clock with its hour labels, the gold path at t = 5000, one block of
# eight arrows on it (n = 1577 … 1603), and the chart of traces beside the clock
WHEEL_BOX = (104024, 104056, -23.5, 6.5)
PATH_WHEEL = (103998, 104057, -23.5, 6.5)
BLOCK15 = (104005.5, 104014.1, -14.6, -10.8)
WHEEL_BARS = (104024, 104106, -23.5, 6.5)
ALL2 = (103998, 104106, -23.5, 6.5)
XS2 = dict(
    id="xs2", title="E8 sets the vortices", subtitle="discovery 2 of 5:  E8's clock inside zeta's path of partial sums",
    setup=PATH_SETUP + [f"exp:e8wheel:40:150:11:12:{CLOCK}:6", "exp:hide:E8 spiral", "exp:hide:E8 impostors", "exp:hide:E8 rings"],
    lines=[
        dict(text="Discovery two. Claude and I drew zeta's series as a path of partial sums: one arrow for every counting number "
                  "n, one over root n long, each turning from the last by t times the logarithm of n over n minus one.",
             camera=PC(at="11,-7,0>12,-16,0", fit="44,26>70,42", span=6, hold=8),
             actions=[say("the path of partial sums", "one arrow n<sup>−½ − it</sup> for every counting number, laid end to end"),
                      at("arrow", tw("pw.n", 1, 1.2)), at("over", tw("pw.n", 2, 1.4)), at("turning", tw("pw.n", 3, 1.4)),
                      at("logarithm", tw("pw.n", 4, 1.4)), at("minus", tw("pw.n", 8, "{minus~end}", "linear")), "exp:snap"]),
        dict(text="Raise t, and the path winds up. At the first zero, t equal to fourteen point one three, its end lands on zero. "
                  "Higher still, at the hundred and fifty-ninth zero, three hundred thirty-three point six, the path curls into "
                  "vortices, with straight spokes between them.",
             camera=PC(at="12,-16,0>5,-5,0", fit="70,42>56,32", span=6),
             actions=[tw("pw.marks", 0, .8), tw("pw.n", 1000000, 1.5), at("raise", tw("pw.t", 14.134725, "{raise~lands}", "geo")),
                      at("winds", tw("pw.tail", 1, 1.5)), at("zero#2", call("pathzeta", 0, 0, 0, "ζ = 0", col=GOLD)),
                      at("higher", "exp:uncall"), at("higher", tw("pw.t", 333.645, "{higher~vortices}", "geo")),
                      at("vortices", tw("pw.eyes", 1, 1.5)),
                      say("winding up", "the first zero, t = 14.13;  the 159th, t = 333.6")]),
        dict(text="The spokes sit near n equal to t over two pi k: fifty-three, twenty-seven, eighteen. Crossing spoke k, the path "
                  "moves by the k-th term of zeta's reflected series, so every vortex belongs to a counting number.",
             camera=PC(at="5,-5,0", fit="56,32"),
             actions=[say("spoke k at n ≈ t / 2πk", "t = 333.6:  53,  27,  18, …"),
                      at("fiftythree", call("path", 53, 0, 0, "spoke 1:  n ≈ 53")), at("twentyseven", call("path", 27, 0, 0, "spoke 2:  n ≈ 27")),
                      at("eighteen", call("path", 18, 0, 0, "spoke 3:  n ≈ 18")),
                      at("crossing", say("each spoke is a counting number", "crossing spoke k, the path moves by χ(s) k<sup>s−1</sup>"))]),
        dict(text="Now bring in E eight's clock: its two hundred and forty roots turn by a thirtieth of a circle, and eight of the "
                  "thirty hours are gold, the ones with no factor two, three or five. Keep only the arrows of numbers on those eight "
                  "hours: the other arrows shrink away, and the path re-forms.",
             camera=TOP(PATH_WHEEL),
             actions=["exp:uncall", tw("pw.eyes", 0, 1), at("clock", "exp:reveal:E8 rings:{clock~eight#2}"),
                      at("eight#2", "exp:e8walk:{eight#2~keep}"), at("keep", tw("pw.gold", 1, "{keep~reforms}")),
                      say("E8's clock and its eight gold hours", "keep only the arrows of n with no factor 2, 3 or 5")]),
        dict(text="Raise t to five thousand. The gold path stretches out: a tangle at the start, then long smooth runs, and at its "
                  "end a coil of tiny loops.",
             camera=TOP(PATH_WHEEL),
             actions=[at("raise", tw("pw.t", 5000, "{raise~stretches}", "geo")),
                      say("the gold path at t = 5000", "only the n on E8's eight gold hours:  25 795 terms")]),
        dict(text="Look closely at the long run near n equal to fifteen ninety. It is made of blocks of eight arrows, one for each "
                  "gold hour, and in every block all eight arrows point the same way.",
             camera=TOP(PATH_WHEEL, BLOCK15, span=4),
             actions=[at("blocks", "exp:tbblock:15"), at("blocks", "exp:mute:PW path:1.5"),
                      say("one run, close up", "blocks of eight arrows:  1577, 1579, 1583, 1589, 1591, 1597, 1601, 1603")]),
        dict(text="Lift one block onto E eight's clock. Seen from the block, each arrow points at fifteen times its own hour, and "
                  "every gold hour times fifteen is hour fifteen: all eight line up, and the block adds to minus eight.",
             camera=TOP(BLOCK15, PATH_WHEEL, span=4),
             actions=[at("lift", "exp:mute:E8 rings:1.5"), at("lift", "exp:tblift:15:{lift~seen}"), at("lift", "exp:tbscale:8"),
                      say("the block on E8's clock", "arrow n points at hour 15·n:  all eight at hour 15, the sum −8")]),
        dict(text="Near n equal to forty-seven seventy, each arrow points at five times its hour: four land on hour five and four "
                  "on hour twenty-five, and the block adds to four.",
             camera=TOP(PATH_WHEEL),
             actions=["exp:clearchild:TB chain", at("near", "exp:tbblock:5"), at("near", call("block", 5, 0, 0, "n ≈ 4770")),
                      at("each", "exp:tblift:5:{each~four#2}"),
                      say("near n = 4770:  k = 5", "arrow n points at hour 5·n:  hours 5 and 25, the sum 4")]),
        dict(text="And at the very end, near n equal to twenty-three thousand eight hundred and eighty, each arrow points at its "
                  "own hour, and the eight nearly cancel: minus one. Every tiny loop of that coil is one such block.",
             camera=TOP(PATH_WHEEL),
             actions=["exp:uncall", "exp:clearchild:TB chain", at("near", "exp:tbblock:1"), at("near", call("block", 1, 0, 0, "n ≈ 23 880")),
                      at("each", "exp:tblift:1:{each~nearly}"),
                      say("near n = 23 880:  k = 1", "arrow n points at its own hour:  the sum −1")]),
        dict(text="Let k run from one to thirty, and the chain of the eight gold hours dances on the clock. Each time k is a whole "
                  "number it lands on a trace of E eight's Coxeter element: minus one, one, two, four, minus two, minus four, minus "
                  "eight, eight.",
             camera=TOP(PATH_WHEEL, WHEEL_BARS, span=3),
             actions=["exp:uncall", "exp:clearchild:TB chain", "exp:clearchild:TB block", "exp:tbbars:104064,-9,0:14",
                      at("let", "exp:tbsweep:1:30:{let~end}"),
                      say("tr C<sup>k</sup>:  E8's Coxeter element", "the chain of the eight gold hours, each turned k hours per hour")]),
        dict(text="Measured on the path itself at t equal to five thousand, the charges of the spokes match these traces, to within "
                  "a quarter up to spoke twelve.",
             camera=TOP(WHEEL_BARS),
             actions=[at("measured", "exp:tbbarsall:measured"),
                      say("measured on the gold path at t = 5000", "white: the charge of spoke k;  colour: tr C<sup>k</sup>")]),
        dict(text="Added up again, each turned by an hour, the thirty traces give back E eight's sieve: for seven they reach hour "
                  "seven on the rim; for six they close up at the centre.",
             camera=TOP(WHEEL_BARS, WHEEL_BOX, span=3),
             actions=[at("seven", "exp:tbsieve:7:{seven~six}"), at("six", "exp:tbsieve:6:3"),
                      say("the sieve as a spectrum", "(1/30) Σ<sub>k</sub> tr C<sup>k</sup> e<sup>−2πikn/30</sup>:  1 on the gold hours, 0 off them")]),
        dict(text="Why it matters: a symmetry of eight dimensions sets the sign and the strength of every vortex of the gold path. "
                  "The identity is Ramanujan's; what Claude and I add is seeing E eight's clock inside zeta's own path.",
             camera=TOP(WHEEL_BOX, ALL2, span=10),
             actions=["exp:unmute:E8 rings,PW path:2",
                      say("what is new", "Ramanujan's sum c<sub>30</sub>(k) = tr C<sup>k</sup>:  E8's clock inside the path of ζ")], pause=1.4),
    ])

# ---------------------------------------------------------------------------------------------------------------
# 3. where the zeros live in E8: the funnel greyed out, one number lit at a time, the coil assembled from the path's arrows
# ---------------------------------------------------------------------------------------------------------------

G1, G2, G3 = 14.134725, 21.022040, 25.010858
FUNNEL = dict(at="68000,0,-16", fit="124,140", el=26, spin=2)
FW = dict(at="68000,0,-9", fit="60,52", el=24, rise=.04)          # the coil, close
FUNNEL_SETUP = ["exp:stage", "exp:backdrop:off", "exp:zfunnel", "exp:fwinit", tw("fw.gamma", G1, 0), tw("fw.n", 0, 0), tw("fw.lift", 0, 0),
                tw("fw.coil", 0, 0), tw("fw.tail", 0, 0), tw("fw.path", 1, 0), tw("fw.hands", 1, 0)]
XS3 = dict(
    id="xs3", title="Where the zeros live in E8", subtitle="discovery 3 of 5:  a zero is a coil up E8's factor funnel",
    setup=FUNNEL_SETUP,
    lines=[
        dict(text="Discovery three. Claude and I asked where zeta's zeros live in E eight's layout of the counting numbers. Here "
                  "it is: every number up to four hundred at its hour on E eight's wheel of thirty, with a column as tall as its "
                  "logarithm, cut at its prime factors.",
             camera=C(**FUNNEL, az=-100),
             actions=[say("E8's factor funnel", "n at its hour n mod 30, its column as tall as log n, cut at its prime factors"), "exp:snap"]),
        dict(text="That is too much at once, so grey it out and light one number at a time. Seven is a prime, at hour seven: one "
                  "piece, log seven tall. Six is two times three, at hour six: a piece log three tall, and on top of it one log two tall.",
             camera=C(at="68000,0,-16>68004,0,-6", fit="124,140>36,28", el="26>22", az="+", spin=1, rise=.04, span=5),
             actions=[at("grey", "exp:mute:GB factor columns:1.2"), at("seven#1", "exp:fwhl:7:logs"),
                      at("six", "exp:fwunhl:7"), at("six", "exp:fwhl:6:logs"),
                      say("one number at a time", "7:  prime, one piece log 7   ·   6 = 2·3:  log 3 + log 2")]),
        # the floor path is flat: the camera comes over the top and turns square to it (az −90) by the end of the line
        dict(text="Now bring in the path of partial sums from discovery two, at the first zero, t equal to fourteen point one three, "
                  "and lay it on the funnel's floor, starting at the axis. Its arrows wind round and its tail brings it back to the axis.",
             camera=C(at="68004,0,-6>68006.8,0.2,0", fit="36,28>40,22.5", el="22>84", az="+", az_to=-90, rise=.035, span=4),
             actions=["exp:fwunhl", "exp:mute:GB factor columns:1.5:1.85", at("bring", tw("fw.n", 15, "{bring~tail}", "linear")),
                      at("tail", tw("fw.tail", 1, 1.5)),
                      say("the path at the first zero, on the floor", "t = 14.13:  its arrows n<sup>−½ − it</sup> close on the axis")]),
        dict(text="Lift each arrow to the height of its own number. Arrow n rises to log n, the top of n's column; its tail moves "
                  "onto the axis, and it keeps its direction. Arrow one stays on the floor, arrow two rises to log two, arrow six "
                  "to log six.",
             camera=C(at="68006.8,0.2,0>68002,0,-7", fit="40,22.5>44,40", el="84>22", az="+", spin=1.5, rise=.04, span=6),
             actions=[tw("fw.tail", 0, 1), tw("fw.axis", 1, 1), tw("fw.path", 0, "{lift~end}"), at("lift", tw("fw.lift", 1, "{lift~six}")),
                      at("two#2", "exp:fwhl:2"), at("six", "exp:fwhl:6"),
                      say("lift arrow n to the height log n", "its direction stays:  γ·log n")]),
        dict(text="Stacked, the arrows are the hands of a clock that turns by gamma for every unit of height, and a coil runs "
                  "through their tips, one turn every point four four. That coil is the zero, wound up E eight's funnel.",
             camera=C(at="68000,0,-14", fit="66,74", el=22, az="+", az_to=-90, rise=.04),
             actions=["exp:fwunhl", at("coil", tw("fw.coil", 1, "{coil~end}")),
                      say("a zero is a coil", "one turn every 2π/γ = 0.44 of height")]),
        # the clock face from above: the hands of 2, 3 and 6 bright, the rest and the coil faded, the turns as arcs
        dict(text="And the coil reads the prime factors. Six is two times three, so log six is log two plus log three, and the "
                  "hand at six has turned the turn of two plus the turn of three.",
             camera=C(at="68000,0,-14>68004,-1,-9.5", fit="66,74>64,36", el="22>64", az=-90, spin=0, rise=.035, span=5),
             actions=[tw("fw.coil", .32, 2), tw("fw.dim", 1, 2), at("six#1", "exp:fwhl:6:logs"), at("plus#1", "exp:fwangles:6"),
                      say("the hand of 6 = the hand of 2 + the hand of 3", "γ·log 6 = γ·log 2 + γ·log 3:  1.56 + 2.47 = 4.03 turns")]),
        dict(text="Every zero is such a coil. The next two, twenty-one point zero two and twenty-five point zero one, turn every "
                  "hand further, the higher ones more: tighter coils.",
             camera=C(at="68004,-1,-9.5>68000,0,-14", fit="64,36>66,74", el="64>24", az=-90, spin=0, rise=.04, span=5),
             actions=["exp:fwunhl", "exp:clearchild:FW angles", tw("fw.dim", 0, 1.5), tw("fw.coil", 1, 2.5),
                      at("twentyone", tw("fw.gamma", G2, 2)), at("twentyfive", tw("fw.gamma", G3, 2)),
                      say("the next zeros", "γ = 21.02,  25.01:  tighter coils")]),
        dict(text="Now add the coils of all ten thousand five hundred zeros. A ring climbs the funnel's axis, and at each height "
                  "the coils add up to a single hand. Where they agree, at a prime or a power of a prime, the hand shoots out to the "
                  "top of that prime's column: seven, eight, nine. At six and ten they cancel, and the hand stays at the axis.",
             camera=C(at="68000,0,-14>68004,0,-10", fit="66,74>56,31.5", el="24>10", az=-90, spin=0, rise=.035, span=4),
             actions=[tw("fw.coil", 0, 1.5), tw("fw.hands", 0, 1.5), tw("fw.axis", 0, 1.5), "exp:mute:GB factor columns:1.5:1",
                      at("climbs", tw("fw.scan", 1.5, 0)), at("climbs", tw("fw.scan", 1.75, "{climbs~seven}")),
                      at("seven", tw("fw.scan", 2.32, "{seven~cancel}", "linear")),
                      say("adding the coils, height by height", "where they agree, a tooth of log p;  elsewhere they cancel"),
                      at("seven", call("tooth", 7, 1, -.7, "7", col=GOLD)), at("eight", call("tooth", 8, 1, .05, "8 = 2³", col=GOLD)),
                      at("nine", call("tooth", 9, 1, .8, "9 = 3²", col=GOLD)), at("six", call("tooth", 6, -1, -.6, "6:  dark", col=RED)),
                      at("ten#2", call("tooth", 10, -1, .6, "10:  dark", col=RED))]),
        dict(text="Climbing on, the ring lights every prime power up to four hundred, and leaves every composite dark. The comb it "
                  "draws is the master wave of discovery one, stood up along the logarithm.",
             camera=C(at="68004,0,-10>68008,0,-19", fit="56,31.5>130,73", el=8, az=-90, spin=0, rise=.035, span=6),
             actions=["exp:uncall", at("climbing", tw("fw.scan", 6.02, "{climbing~dark}")), at("comb", tw("fw.scan", 100, 0)),
                      say("ζ's comb", "a tooth at every prime power:  the master wave, along log n")]),
        dict(text="Zeta's zeros fix a prime's height but not its hour. E eight's field has seven more L-functions; turned back by "
                  "their characters, they split the comb into eight fans, one for each gold hour. Forty-nine, seven squared, "
                  "lands on hour nineteen.",
             camera=C(at="68008,0,-19>68000,0,-16", fit="130,73>130,130", el="8>50", az=-90, spin=0, rise=.04, span=8),
             actions=[at("split", tw("fw.scan", -1, 0)), at("split", "exp:hide:JM comb"), at("split", "exp:hide:JM comb labels"),
                      at("split", tw("fw.fan", 1, "{split~hour}")),
                      at("fortynine", call("fun", 49, 0, 0, "49 = 7²:  hour 19")),
                      say("eight L-functions, eight spokes", "ζ's zeros give the height, the other seven the hour")]),
        dict(text="Why it matters: the zeros are not points in E eight's stack. They are frequencies of its height, and every "
                  "coil reads the prime factors. The explicit formula is classical; what Claude and I add is where each zero sits "
                  "in E eight's funnel.",
             camera=C(at="68000,0,-16", fit="130,130>124,140", el="50>30", az="+", spin=3, rise=.04, span=12),
             actions=["exp:uncall", say("what is new", "a zero is a coil up E8's funnel, turning with the logarithm of every number")],
             pause=1.4),
    ])

# ---------------------------------------------------------------------------------------------------------------
# 4. what keeps zeros on the line: the lattices become the headers of their zeta functions' strips, the strips fold into
#    the chart of every mixture, and that line of mixtures grows into a plane
# ---------------------------------------------------------------------------------------------------------------

LATT = TOP((75858, 76142, -64, 56))                  # the three lattices with their names and what the rings meet
STRIPS = (75876, 76124, -68, 78)                     # the four strips of zeros, the lattices shrunk onto their heads
XS4 = dict(
    id="xs4", title="What keeps zeros on the line", subtitle="discovery 4 of 5:  symmetry is not enough",
    setup=["exp:stage", "exp:backdrop:off", "exp:t5lattice3", "exp:t5families:76000,-8,0", "exp:t5mix:76000,0,0", "exp:t5plane:76330,-6,0",
           "exp:hidechild:LA lattices:ring", "exp:hidechild:LA lattices:lit", "exp:hide:LC zeros", "exp:hide:LE mixtures",
           "exp:twgroup:LF landscape:0,0,0:.02:0", "exp:hide:LF landscape"],
    lines=[
        dict(text="Discovery four. Discriminant minus twenty-three has three families of quadratic forms, and each family is a "
                  "lattice. A number lies on a family when the lattice has a point at that distance squared from the centre.",
             camera=LATT,
             actions=[say("three families, three lattices", "x² + xy + 6y²  ·  2x² + xy + 3y²  ·  2x² − xy + 3y²"), "exp:snap"]),
        dict(text="Fifty-nine, a prime, lies on the first family only. Thirteen, a prime, lies on the other two. But six, two times "
                  "three, lies on all three: a composite shows up on more than one family.",
             camera=LATT,
             actions=[at("fiftynine", "exp:showchild:LA lattices:ring 0 59"), at("fiftynine", "exp:showchild:LA lattices:lit"),
                      at("fiftynine", call("at", "75934.3,0,-0.4", 1, 1, "59:  4 points", col=WHITE)),
                      at("thirteen", "exp:showchild:LA lattices:ring 1 13"), at("thirteen", "exp:showchild:LA lattices:ring 2 13"),
                      at("thirteen", call("at", "76015.1,0,-0.4", 1, 1, "13", col=GREEN)),
                      at("six", "exp:showchild:LA lattices:ring 0 6"), at("six", "exp:showchild:LA lattices:ring 1 6"),
                      at("six", "exp:showchild:LA lattices:ring 2 6"),
                      at("six", call("at", "76108.3,0,-0.4", 1, -1, "6 = 2·3:  on all three", col=RED))]),
        dict(text="Each family has its own zeta function, counting its lattice points by distance, with Riemann's symmetry. The "
                  "lattices move up to head their strips of zeros. Yet those zeros leave the critical line; one sits at real part "
                  "one point zero zero seven, where no function with an Euler product can vanish.",
             camera=TOP((75858, 76142, -64, 56), STRIPS, span=4),
             actions=["exp:uncall", at("lattices", "exp:hidechild:LA lattices:row"), at("lattices", "exp:hidechild:LA lattices:name"),
                      at("lattices", "exp:hidechild:LA lattices:sub"),
                      # each lattice shrinks onto the head of its own strip; the mirror pair shares one (the same zeta function)
                      at("lattices", "exp:twgroup:LA lattices/family 0:2,66,0:.3:{lattices~strips}"),
                      at("lattices", "exp:twgroup:LA lattices/family 1:-45,66,0:.3:{lattices~strips}"),
                      at("lattices", "exp:twgroup:LA lattices/family 2:-117,66,0:.3:{lattices~strips}"),
                      at("strips", "exp:show:LC zeros"), at("strips", "exp:hidechild:LC zeros:tick"),
                      at("strips", "exp:hidechild:LC zeros:section"),
                      at("strips", "exp:drawon:LC zeros/line:2"),
                      say("each family's own zeta function", "red: zeros off the critical line Re s = ½"),
                      at("leave", call("at", "75916.6,-30.5,-0.4", -1, 1, "0.953 + 16.29i", col=RED)),
                      at("one#2", call("at", "75982.1,-18.6,-0.4", 1, 1, "1.007 + 22.57i", col=RED))]),
        dict(text="The class group's characters mix the families into two L-functions with Euler products, and those two keep every "
                  "zero on the line: forty-four and forty-two zeros, all gold.",
             camera=TOP(STRIPS),
             actions=["exp:uncall", say("the class group's L-functions", "(Z<sub>1</sub> + 2Z<sub>2</sub>)/2 and (Z<sub>1</sub> − Z<sub>2</sub>)/2:  every zero on the line"),
                      at("fortyfour", call("at", "76032,-65.7,-0.4", -1, -1, "44 on the line", col=GOLD)),
                      at("fortytwo", call("at", "76096,-65.7,-0.4", 1, -1, "42 on the line", col=GOLD))]),
        dict(text="Now mix the two families with any weight lambda. Fold the four strips down onto one chart: every zero on the line "
                  "lands where a single curve crosses its height, lambda zero for the first family, two and minus one for the "
                  "L-functions, and the second family's zeros become the curve's poles.",
             camera=C(at=f"{BOX(*STRIPS)['at']}>76000,4,0", fit=f"{BOX(*STRIPS)['fit']}>300,165", el=89.5, az=-90, spin=0, span=4),
             actions=["exp:uncall", at("fold", "exp:stripsmorph:76000,-8,0:76000,0,0:{fold~poles}"), at("fold", "exp:mute:LC zeros:2"),
                      at("fold", "exp:mute:LA lattices:2"), at("lands", "exp:show:LE mixtures"), at("lands", "exp:drawon:LE mixtures/R:4"),
                      say("every mixture at once", "the zeros of Λ<sub>1</sub> + λΛ<sub>2</sub> on the line:  where the curve meets the height λ")]),
        dict(text="Sweep the height lambda through every mixture. Counted in full, up to height fifty, only two small windows of "
                  "mixtures keep every zero on the line, and both surround the class group's L-functions.",
             camera=C(at="76000,4,0", fit="300,165"),
             actions=[at("sweep", "exp:mixsweep:-3:4:{sweep~counted}:76000,0,0"),
                      say("no zero off the line:  two small windows", "λ from 1.99 to 2,  and from −1.02 to −0.88"),
                      at("windows", call("at", "76072,24,-0.4", 1, 1, "λ = 2", col=GOLD)),
                      at("both", call("at", "76072,-11.4,-0.4", 1, -1, "λ = −1", col=GOLD))]),
        dict(text="With five families, for minus forty-seven, the line of mixtures grows into a plane. Colour each by its zeros off "
                  "the line, and gold appears only in small islands: round the three L-functions, and one more island with no "
                  "L-function in it.",
             camera=C(at="76000,4,0>76170,-6,0", fit="300,165>480,270", el=89.5, az=-90, spin=0, span=4),
             actions=["exp:uncall", at("grows", "exp:show:LF landscape"), at("grows", "exp:twgroup:LF landscape:0,0,0:1:{grows~colour}"),
                      say("five families:  a plane of mixtures", "gold:  no zero off the line up to t = 50"),
                      at("three", call("at", "76371.3,35.3,-0.4", 1, 1, "ζ(s) L(s, χ<sub>−47</sub>)", col=GOLD)),
                      at("three", call("at", "76342.8,-39.4,-0.4", 1, -1, "L<sub>1</sub>", col=GOLD)),
                      at("three", call("at", "76296.6,6.8,-0.4", -1, 1, "L<sub>2</sub>", col=GOLD)),
                      at("island", call("at", "76364.1,18.8,-0.4", 1, -1, "no L-function here", col=WHITE))]),
        dict(text="Why it matters: symmetry alone does not keep zeros on the line. Davenport and Heilbronn proved that in nineteen "
                  "thirty-six. As far as anyone has checked, the functions that keep them there have Euler products. What Claude "
                  "and I add is seeing every mixture at once.",
             camera=C(at="76170,-6,0>76170,10,0", fit="480,270>500,290", el=89.5, az=-90, spin=0, span=14),
             actions=["exp:uncall", "exp:unmute:LA lattices:3", say("what is new", "symmetry is not enough:  every mixture seen at once")], pause=1.4),
    ])

# ---------------------------------------------------------------------------------------------------------------
# 5. Chebyshev's bias is a fold: the race and its explanation in one frame; the race morphs into the fair race, the fold of
#    E8's clock gives way to the triangle of S3, and the race between the families takes the race's place
# ---------------------------------------------------------------------------------------------------------------

RACE_TOP = (79855, 80145, 8, 64)                    # the race counted to 10⁷
RACES = (79855, 80145, -62, 64)                     # counted above, rebuilt from the zeros below
RACE_FOLD = (79840, 80335, -78, 64)                 # the race beside E8's clock of squares (the fold, centred 80260, −8)
FOLD = (80195, 80325, -72, 56)
S3 = (80215, 80305, -30, 28)                        # the triangle of roots, where the fold was
XS5 = dict(
    id="xs5", title="Chebyshev's bias is a fold", subtitle="discovery 5 of 5:  why two of E8's spokes run behind",
    setup=["exp:stage", "exp:backdrop:off", "exp:t5race", "exp:t5fold:80330,0,0", "exp:s3roots:80260,-4,0:0",
           "exp:t5family:79914,9,0:80260,-8,0:60:280", "exp:twgroup:MC fold:0,0,0:.3:0", "exp:mute:MA rebuilt:0:1.9",
           "exp:hide:MB fair", "exp:hide:MB fold", "exp:hide:S3 identity", "exp:hide:S3 rotation", "exp:hide:S3 reflection",
           "exp:hide:S3 square", "exp:hide:MC fold", "exp:hide:MC race", "exp:hide:MC fair"],
    lines=[
        dict(text="Discovery five. Every prime above five lies on one of E eight's eight gold spokes. Race them up to ten million, "
                  "and two of the spokes, one and nineteen, run behind nearly all the way.",
             camera=TOP(RACE_TOP),
             actions=[say("the prime race on E8's eight spokes", "the primes up to 10<sup>7</sup>, counted and rebuilt from the zeros"),
                      at("behind", call("at", "80112,27,-0.4", .4, -1, "spokes 1 and 19:  about −3", col=RED)),
                      at("behind+0.4", call("at", "80112,43,-0.4", .4, 1, "the other six:  about +1", col=GOLD)), "exp:snap"]),
        dict(text="They are ahead of their share only zero point three and one point four percent of the way, and never in the "
                  "lead. Below, six thousand seven hundred and seventy-one zeros of E eight's L-functions rebuild every curve.",
             camera=TOP(RACE_TOP, RACES, span=4),
             actions=["exp:uncall", say("spokes 1 and 19 run behind", "ahead of their share 0.3 % and 1.4 % of the way;  rebuilt from 6 771 zeros"),
                      at("below", "exp:unmute:MA rebuilt:1.2"), at("below", "exp:pulse:MA rebuilt:2.4")]),
        dict(text="The reason is a fold. The zeros count prime powers, not only primes. Beside the race, E eight's clock of thirty, "
                  "with an arc from every prime to its square: squaring folds every spoke onto one or nineteen.",
             camera=TOP(RACES, RACE_FOLD, span=4),
             actions=[at("reason", "exp:mute:MA rebuilt:1.5"), at("beside", "exp:show:MB fold"), at("beside", "exp:hidechild:MB fold:arcs"),
                      at("arc", "exp:showchild:MB fold:arcs"), at("arc", "exp:drawon:MB fold/arcs:{arc~end}"),
                      say("the reason:  a fold", "squaring sends spokes 1, 11, 19, 29 to 1, and 7, 13, 17, 23 to 19")]),
        dict(text="Seven squared is forty-nine, on spoke nineteen. Eleven squared, one hundred and twenty-one, lands on one. "
                  "Thirteen squared, one sixty-nine, on nineteen. Twenty-nine squared, eight forty-one, on one.",
             camera=TOP(RACE_FOLD, FOLD, span=3),
             actions=[at("seven", call("fold", 49, 1, 1, "7² = 49:  spoke 19")), at("eleven", call("fold", 121, -1, 1, "11² = 121:  spoke 1")),
                      at("thirteen", call("fold", 169, 1, -1, "13² = 169:  spoke 19")), at("twentynine", call("fold", 841, -1, -1, "29² = 841:  spoke 1"))]),
        dict(text="Every square of a prime above five lands on spoke one or nineteen: all four hundred and forty-three of them up "
                  "to ten million. A count of primes leaves out half a prime for each. Put them back, and watch the race even out.",
             camera=TOP(FOLD, RACE_FOLD, span=4),
             actions=["exp:uncall", at("put", "exp:racemorph:{put~end}"), at("put", "exp:pulse:MB fold:2.4"),
                      say("put the squares back:  a fair race", "218 squares on spoke 1,  225 on spoke 19,  none elsewhere")]),
        # the second race: the triangle of x³ − x − 1 takes the fold's place
        dict(text="The same fold turns up in a second race. For minus twenty-three, a prime's family is decided by how it acts on "
                  "the three roots of x cubed minus x minus one.",
             camera=TOP(RACE_FOLD, S3, span=4),
             actions=[at("same", "exp:mute:MA counted:1.5"), at("same", "exp:hide:MB fold"), at("family", "exp:show:S3 identity"),
                      say("x³ − x − 1:  three roots", "r<sub>1</sub> = 1.3247,  r<sub>2</sub>, r<sub>3</sub> = −0.6624 ± 0.5623i")]),
        dict(text="Fifty-nine splits it into three factors: every root stays put, the identity. Two leaves it whole: the roots "
                  "turn round, a rotation. Five gives one root: two roots swap, a reflection.",
             camera=TOP(S3),
             actions=[at("two", "exp:hide:S3 identity"), at("two", "exp:show:S3 rotation"), at("five", "exp:hide:S3 rotation"),
                      at("five", "exp:show:S3 reflection"), say("how a prime acts on the roots", "identity · rotation · reflection")]),
        # the family race takes the prime race's place, and the fold of S3 the triangle's
        dict(text="And a reflection done twice is the identity. So the squares of half of all primes fall in the principal family, "
                  "which runs behind its share and is never in the lead.",
             camera=TOP(S3, RACE_FOLD, span=6, hold=4),
             actions=[at("twice", "exp:hide:S3 reflection"), at("twice", "exp:show:S3 square"),
                      # the fold of S₃ grows out of the triangle's place as the camera pulls back to the races
                      at("squares", "exp:hide:S3 square"), at("squares", "exp:show:MC fold"),
                      at("squares", "exp:twgroup:MC fold:0,0,0:1:{squares~end}"),
                      at("squares", "exp:mute:MA counted,MA rebuilt:1:2"), at("squares+1", "exp:hide:MA counted"),
                      at("squares+1", "exp:hide:MA rebuilt"), at("squares", "exp:show:MC race"), at("squares", "exp:drawon:MC race/class:3"),
                      say("a reflection, twice:  the identity", "the inert primes' squares land in the principal family")]),
        dict(text="Put those squares back too, and this race evens out as well. Why it matters: one fold explains both races. "
                  "Chebyshev noticed the bias in eighteen fifty-three; what Claude and I add is one picture for E eight's wheel and "
                  "for the families of minus twenty-three.",
             camera=TOP(RACE_FOLD),
             actions=[at("put", "exp:familymorph:{put~why}"),
                      say("what is new", "one fold:  E8's wheel and the families of −23")], pause=1.4),
    ])

# ---------------------------------------------------------------------------------------------------------------
# the zeros inside E8, 1: the path of partial sums, link by link, and the curve of ζ climbing its t axis
# ---------------------------------------------------------------------------------------------------------------

XZ1 = dict(
    id="xz1", title="The path of partial sums", subtitle="the zeros inside E8, part 1:  how ζ is built, link by link",
    setup=PATH_SETUP,
    lines=[
        dict(text="Claude, an AI math research assistant, and I set out to find where the zeros of zeta live among the counting "
                  "numbers. First, a refresher on how zeta is built: one arrow for every counting number, laid end to end. "
                  "Mathematicians call this the path of partial sums.",
             camera=PC(at="11,-6.5,0", fit="44,26", rise=.035),
             actions=[say("the path of partial sums", "ζ(s) = 1 + 1/2<sup>s</sup> + 1/3<sup>s</sup> + …,  one arrow per term"),
                      at("arrow", tw("pw.n", 1, 1.5)), "exp:snap"]),
        dict(text="Take the height t equal to one. The first arrow is one unit long and points to the right. The second is one over "
                  "the square root of two long, and it turns away from the first by the logarithm of two: forty degrees.",
             camera=PC(at="11,-6.5,0", fit="44,26", rise=.035),
             actions=[at("second", tw("pw.n", 2, 2.2))]),
        dict(text="The third is one over root three long, and turns a further log of three halves, twenty-three degrees. Its "
                  "direction is the sum of the turns: log three, sixty-three degrees from the first. Every direction is a logarithm.",
             camera=PC(at="11,-6.5,0", fit="44,26", rise=.035),
             actions=[at("third", tw("pw.n", 3, 2.2)), at("sum", "exp:pulse:PW path:1.6")]),
        dict(text="Four, five, six, seven, eight: shorter and shorter arrows, turning less and less, seventeen degrees, then "
                  "thirteen, ten, nine and eight, because the logarithm climbs more and more slowly.",
             camera=PC(at="11,-6.5,0>11.5,-17.5,0", fit="44,26>114.7,64.5", span=4, rise=.035),
             actions=[at("four", tw("pw.n", 4, .7)), at("five", tw("pw.n", 5, .7)), at("six", tw("pw.n", 6, .7)),
                      at("seven", tw("pw.n", 7, .7)), at("eight#1", tw("pw.n", 8, .7))]),
        dict(text="Now raise the height t. Every angle is t times a logarithm, so the path winds up tighter. The arrows go on "
                  "forever, but past a point the rest add up to a simple tail, drawn in orange, and the end of that tail is zeta "
                  "of one half plus i t.",
             camera=PC(at="11.5,-17.5,0>10,-4,0", fit="114.7,64.5>58,36", span=10),
             actions=[tw("pw.marks", 0, .8), at("raise", tw("pw.n", 1000, 2)), at("raise", tw("pw.t", 10, "{raise~tail}", "geo")),
                      at("tail", tw("pw.tail", 1, 1.6)),
                      say("raising t", "every angle is t × a logarithm;  the tail adds all the arrows beyond")]),
        dict(text="At t equal to fourteen point one three, the end lands exactly on zero. That is the first zero of zeta: all the "
                  "arrows, from one to infinity, cancel out.",
             camera=PC(at="6,0,0", fit="48,30"),
             actions=[tw("pw.t", 14.134725, "{at~zero#1}"), at("zero#1", call("pathzeta", 0, 0, 0, "ζ(½ + 14.13i) = 0", col=GOLD)),
                      say("the first zero", "t = 14.1347:  ζ(½ + it) = 0")]),
        dict(text="Now let t run on, and stand the plane up on a t axis, so that it climbs as t grows. The end of the path leaves a "
                  "trail, the curve of zeta along the critical line. It touches the axis at every zero: twenty-one point zero "
                  "two, twenty-five point zero one, thirty point four two.",
             camera=PC(at="6,0,0", fit="48,30>96,64", el="89.5>30", az=-90, spin=3, span=8),
             actions=["exp:uncall", at("stand", tw("pw.rise", 1, 3)), at("stand", tw("pw.curve", 1, 2)),
                      at("run", tw("pw.t", 32, "{run~end}", "linear")),
                      say("the curve of ζ on the critical line", "the plane climbs the t axis;  the curve touches it at every zero")]),
        dict(text="Between the zeros, the curve swings out and comes back. Riemann's hypothesis says that every zero of this kind "
                  "lies on this line, real part one half. Trillions have been checked, and every one does, but no one has proved "
                  "that all of them must.",
             camera=PC(at="6,0,-10", fit="96,64>124,90", el="30>24", az="+", spin=3, span=12),
             actions=[tw("pw.t", 50, "{between~end}", "linear"),
                      say("Riemann's hypothesis", "every nontrivial zero of ζ has real part ½")]),
        dict(text="Higher up, the path needs more and more arrows before the tail takes over, and it begins to curl into knots. "
                  "Those knots are its vortices, and the next part follows them up the axis.",
             camera=PC(at="6,-2,0", fit="124,90>84,56", el="24>36", az="+", spin=3, span=10),
             actions=[tw("pw.t", 80, "{higher~end}", "linear"), tw("pw.curve", .2, 3), at("knots", tw("pw.eyes", 1, 1.5)),
                      say("the vortices begin", "knots in the path, ringed")], pause=1.4),
    ])

# ---------------------------------------------------------------------------------------------------------------
# the zeros inside E8, 2: how the vortices develop as the plane climbs
# ---------------------------------------------------------------------------------------------------------------

XZ2 = dict(
    id="xz2", title="How the vortices develop", subtitle="the zeros inside E8, part 2:  the path of partial sums high up",
    setup=["exp:stage", "exp:backdrop:off", "exp:vortexprep", "exp:pwinit", tw("pw.t", 50, 0), tw("pw.n", 1000, 0), tw("pw.tail", 1, 0),
           tw("pw.rise", 1, 0), tw("pw.curve", 0, 0), tw("pw.readout", 1, 0)],
    lines=[
        dict(text="Climbing higher, the path needs about t over pi arrows before its tail takes over, and it curls into knots: "
                  "vortices. The rings mark their eyes, numbered from the far end of the path.",
             camera=PC(at="6,-2,0", fit="70,46", el=36, az=-90, spin=3),
             actions=[tw("pw.t", 130, "line", "linear"), at("knots", tw("pw.eyes", 1, 1.5)),
                      say("vortices", "about t/2π of them at height t"), "exp:snap"]),
        dict(text="Each time t over two pi passes another whole number, a new vortex is born near the start of the path, and the "
                  "older ones move out along it.",
             camera=PC(at="6,-2,0", fit="70,46", el=36, az="+", spin=3),
             actions=[tw("pw.t", 250, "line", "linear"), say("a vortex is born", "each time t/2π passes a whole number")]),
        dict(text="Between the vortices the path runs straight. Those straight runs are the spokes, where each arrow turns by a "
                  "whole number of full turns from the last. They sit near n equal to t over two pi k: at the hundred and "
                  "fifty-ninth zero, fifty-three, twenty-seven and eighteen.",
             camera=PC(at="6,-4,0", fit="70,46>60,38", el="36>70", az="+", spin=1, span=8),
             actions=[tw("pw.t", 333.645, "{between~fiftythree}"),
                      at("fiftythree", call("path", 53, 0, 0, "spoke 1:  n ≈ 53")), at("twentyseven", call("path", 27, 0, 0, "spoke 2:  n ≈ 27")),
                      at("eighteen", call("path", 18, 0, 0, "spoke 3:  n ≈ 18")),
                      say("spoke k at n ≈ t / 2πk", "where each arrow turns a whole number of turns from the last")]),
        dict(text="Each vortex has an eye, and each eye is a mirror image: the path's own k-th vertex, reflected through zeta's "
                  "phase. The crosses mark those mirror points, and they land inside the rings.",
             camera=PC(at="4,-4,0", fit="52,32", el=78, az="+", spin=1),
             actions=["exp:uncall", at("crosses", tw("pw.mirrors", 1, 1.2)),
                      say("every eye is a mirror", "eye of vortex k = the path's k-th vertex, reflected through ζ's phase")]),
        dict(text="Now let the plane climb on, and at every zero keep each eye where it was. The eyes build a tower; vortex one, in "
                  "cyan, always sits on a circle of radius one.",
             camera=PC(at="0,0,10", fit="60,40>90,70", el="70>26", az="+", spin=6, span=10),
             actions=[tw("pw.mirrors", 0, 1), tw("pw.trails", 1, 0), tw("pw.t", 420, "{climb~end}", "linear"),
                      say("the eyes through the zeros", "kept at every zero:  vortex 1 on a circle of radius 1")]),
        dict(text="Look straight down the axis. The eyes of ten thousand zeros, stacked: the unit circle of vortex one, and rings "
                  "for all the others.",
             camera=PC(at="0,0,0", fit="90,70>56,40", el="26>89.5", az="+", spin=4, span=6),
             actions=[at("ten", tw("pw.alleyes", 1, 2)), at("ten", tw("pw.eyes", 0, 1)), at("ten", tw("pw.trails", .25, 2)),
                      say("all 10 000 zeros, down the axis", "the vortices belong to the counting numbers")]),
        dict(text="So the vortices develop as t climbs. New ones are born at the start, the spokes slide outward, and every vortex "
                  "belongs to a counting number: a reflection of the path's own beginning.",
             camera=PC(at="0,0,0", fit="56,40>64,46", el="89.5>70", az="+", spin=3, span=12),
             actions=[say("how the vortices develop", "one per counting number, each a mirror of the path's start")], pause=1.4),
    ])

# ---------------------------------------------------------------------------------------------------------------
# the zeros inside E8, 3: from a zero's height to the heights of the counting numbers, step by step
# ---------------------------------------------------------------------------------------------------------------

XZ3 = dict(
    id="xz3", title="From a zero to E8's heights", subtitle="the zeros inside E8, part 3:  the turn of n is γ times log n",
    setup=FUNNEL_SETUP,
    lines=[
        dict(text="This is E eight's factor funnel. Every counting number sits at its hour on the wheel of thirty, with a column "
                  "as tall as its logarithm. Seven, at hour seven: one point nine five. Thirty, two times three times five, at "
                  "hour zero: three point four.",
             camera=C(**FUNNEL, az=-100),
             actions=[say("E8's factor funnel", "n at its hour n mod 30, its column as tall as log n"),
                      at("seven#2", call("fun", 7, 0, 0, "log 7 = 1.95")), at("thirty#2", call("fun", 30, 0, 0, "30 = 2·3·5:  log 30 = 3.40")),
                      "exp:snap"]),
        dict(text="Grey it out, and light the powers of two. Two is one piece, log two tall. Four is two pieces, eight is three, "
                  "sixteen is four: each column is its prime factors' logarithms, stacked.",
             camera=C(at="68000,0,-16>68002,-2,-9", fit="124,140>72,44", el="26>22", az="+", spin=1, rise=.035, span=5),
             actions=["exp:uncall", at("grey", "exp:mute:GB factor columns:1.2"), at("two#2", "exp:fwhl:2:logs"), at("four", "exp:fwhl:4"),
                      at("eight", "exp:fwhl:8"), at("sixteen", "exp:fwhl:16:logs"),
                      say("a column is its prime factors, stacked", "log 16 = log 2 + log 2 + log 2 + log 2")]),
        dict(text="Now the path of partial sums at the first zero, t equal to fourteen point one three, on the funnel's floor. "
                  "Arrow one, arrow two, arrow three, and on, each one over root n long, each turned by t times log n.",
             camera=C(at="68002,-2,-9>68006.8,0.2,0", fit="72,44>40,22.5", el="22>84", az="+", az_to=-90, rise=.035, span=4),
             actions=["exp:fwunhl", "exp:mute:GB factor columns:1.5:1.85", at("arrow#1", tw("fw.n", 1, .8)), at("arrow#2", tw("fw.n", 2, .8)), at("arrow#3", tw("fw.n", 3, .8)),
                      at("on#2", tw("fw.n", 15, "{on#2~end}", "linear")),
                      say("the path at the first zero", "arrow n:  1/√n long, turned 14.13 · log n")]),
        dict(text="Its tail closes it on the axis: that is what a zero is. Now lift every arrow to the height of its own number: "
                  "arrow two to log two, arrow three to log three, arrow four to log four, twice as high as two.",
             camera=C(at="68006.8,0.2,0>68002,0,-7", fit="40,22.5>44,40", el="84>22", az="+", spin=1.5, rise=.04, span=6),
             actions=[tw("fw.tail", 1, 1.2), at("lift", tw("fw.tail", 0, 1)), at("lift", tw("fw.axis", 1, 1)), at("lift", tw("fw.path", 0, "{lift~end}")),
                      at("lift", tw("fw.lift", 1, "{lift~end}")), at("two#2", "exp:fwhl:2"), at("three#2", "exp:fwhl:3"), at("four#2", "exp:fwhl:4"),
                      say("lift arrow n to the height log n", "keeping its direction:  −14.13 · log n")]),
        dict(text="Each arrow becomes a hand, pointing out from the axis at the top of its number's column. Through their tips "
                  "runs a coil that turns by gamma for every unit of height: the zero, wound up E eight's funnel.",
             camera=C(at="68000,0,-14", fit="66,74", el=22, az="+", az_to=-90, rise=.04),
             actions=["exp:fwunhl", at("coil", tw("fw.coil", 1, "{coil~end}")),
                      say("the coil:  the hands at every height", "turning 14.13 per unit of height, one turn every 0.44")]),
        dict(text="The coil multiplies by adding heights. Four is two times two, so its hand has turned twice the turn of two. "
                  "Six is two times three: the turn of two plus the turn of three.",
             camera=C(at="68000,0,-14>68004,-1,-9.5", fit="66,74>64,36", el="22>64", az=-90, spin=0, rise=.035, span=5),
             actions=[tw("fw.coil", .32, 2), tw("fw.dim", 1, 2), at("four", "exp:fwhl:4:logs"), at("twice", "exp:fwangles:4"), at("six", "exp:fwunhl"), at("six", "exp:clearchild:FW angles"),
                      at("six+0.1", "exp:fwhl:6:logs"), at("plus", "exp:fwangles:6"),
                      say("multiplying is adding heights", "γ·log 4 = 2 γ·log 2   ·   γ·log 6 = γ·log 2 + γ·log 3")]),
        dict(text="Every zero winds its own coil. Twenty-one point zero two turns each hand further, the higher ones more; "
                  "twenty-five point zero one, further still.",
             camera=C(at="68004,-1,-9.5>68000,0,-14", fit="64,36>66,74", el="64>24", az=-90, spin=0, rise=.04, span=5),
             actions=["exp:fwunhl", "exp:clearchild:FW angles", tw("fw.dim", 0, 1.5), tw("fw.coil", 1, 2.5), at("twentyone", tw("fw.gamma", G2, 2.2)),
                      at("twentyfive", tw("fw.gamma", G3, 2.2)),
                      say("other zeros, other coils", "γ = 21.02,  25.01")]),
        dict(text="At a height that is not a zero, like eighteen, the hands turn just the same, but drop them back onto the floor "
                  "and the path does not close: its tail ends away from the axis.",
             camera=C(at="68000,0,-14>68024.5,-3,0", fit="66,74>51.2,28.8", el="24>84", az=-90, spin=0, rise=.035, span=6),
             actions=[tw("fw.coil", 0, 1.5), at("eighteen", tw("fw.gamma", 18, 2)), at("drop", tw("fw.lift", 0, "{drop~path}")),
                      at("drop", tw("fw.path", 1, 1.5)), at("tail", tw("fw.tail", 1, 1.5)),
                      say("t = 18:  not a zero", "the arrows dropped back into the path:  it ends away from the axis")]),
        dict(text="That is the bridge between a zero and E eight's layout: the zero's height, times the height of n's column, is "
                  "the turn of n's arrow. Each prime factor adds its own turn.",
             camera=C(at="68000,0,-14", fit="66,74>124,140", el="60>26", az="+", spin=3, rise=.04, span=12),
             actions=["exp:mute:GB factor columns:2:1", tw("fw.gamma", G1, 2), tw("fw.tail", 0, 1), tw("fw.path", 0, 2), tw("fw.lift", 1, 3), tw("fw.coil", 1, 4),
                      say("the bridge", "the turn of n at a zero = γ × log n = γ × (the sum of log p over its prime factors)")], pause=1.4),
    ])

# ---------------------------------------------------------------------------------------------------------------
# the zeros inside E8, 4: the coils add up, height by height, into the comb; the comb spins into rings; E8's eight
#    L-functions turn it into eight fans
# ---------------------------------------------------------------------------------------------------------------

XZ4 = dict(
    id="xz4", title="Lamps and spokes", subtitle="the zeros inside E8, part 4:  all the coils together",
    setup=["exp:stage", "exp:backdrop:off", "exp:zfunnel", "exp:fwinit", tw("fw.n", 0, 0), "exp:mute:GB factor columns:0"],
    lines=[
        dict(text="Every zero winds its own coil up the funnel. Here are the first three, fourteen point one three, twenty-one point "
                  "zero two and twenty-five point zero one, over the greyed-out columns.",
             camera=C(at="68000,0,-18", fit="62,92", el=12, az=-96, spin=3, rise=.04),
             actions=[say("every zero, a coil", "γ = 14.13,  21.02,  25.01"), "exp:reveal:JM coils:{fourteen},{twentyone},{twentyfive}", "exp:snap"]),
        dict(text="Take all ten thousand five hundred zeros, and look at one height: the top of seven's column. Each coil points "
                  "somewhere there; on average they lean towards minus one, by minus point one one five, exactly as Landau's "
                  "formula predicts for a prime.",
             camera=C(at="68000,0,-12", fit="74,74", el=40, az=-100, spin=2, rise=.04),
             actions=["exp:show:JM coils", at("sevens", "exp:fwhl:7"), at("each", "exp:reveal:JM disc 7:0,0.8,1.8,3.4"),
                      say("at the height of 7, a prime", "10 500 coils lean to −1:  mean −0.115,  Landau −0.115")]),
        dict(text="At the top of six's column, two times three, the coils point every way, and their mean is zero. The zeros lean "
                  "only at the powers of primes.",
             camera=C(at="68000,0,-11", fit="74,74", el=40, az=-80, spin=2, rise=.04),
             actions=["exp:fwunhl", "exp:hide:JM disc 7", at("sixs", "exp:fwhl:6"), at("coils", "exp:reveal:JM disc 6:0,0.8,1.8,3.4"),
                      say("at the height of 6 = 2·3", "the coils cancel:  mean 0.000")]),
        dict(text="Now do it at every height. A ring climbs the axis, and at each height all the coils add up to one hand, drawn "
                  "out to the side. Where they agree, at a prime power, it shoots out to the top of that prime's column: seven, eight, "
                  "nine. At six, ten and twelve it stays at the axis.",
             camera=C(at="68000,0,-11>68004,0,-10", fit="74,74>56,31.5", el="40>10", az=-90, spin=0, rise=.035, span=4),
             actions=["exp:fwunhl", "exp:hide:JM coils", "exp:hide:JM disc 6",
                      at("climbs", tw("fw.scan", 1.5, 0)), at("climbs", tw("fw.scan", 1.75, "{climbs~seven}")),
                      at("seven", tw("fw.scan", 2.55, "{seven~stays}", "linear")),
                      say("all the coils, height by height", "a tooth of log p at every prime power"),
                      at("seven", call("tooth", 7, 1, -.7, "7", col=GOLD)), at("eight", call("tooth", 8, 1, .05, "8 = 2³", col=GOLD)),
                      at("nine", call("tooth", 9, 1, .8, "9 = 3²", col=GOLD)), at("six", call("tooth", 6, -1, -.7, "6:  dark", col=RED)),
                      at("ten", call("tooth", 10, -1, .1, "10:  dark", col=RED)), at("twelve", call("tooth", 12, -1, .8, "12:  dark", col=RED))]),
        dict(text="Climbing on, it lights every prime power up to four hundred: a comb, the master wave from the first discovery, "
                  "stood up along the logarithm.",
             camera=C(at="68004,0,-10>68008,0,-19", fit="56,31.5>130,73", el=8, az=-90, spin=0, rise=.035, span=6),
             actions=["exp:uncall", at("climbing", tw("fw.scan", 6.02, "{climbing~comb}")), at("comb", tw("fw.scan", 100, 0)),
                      say("ζ's comb", "a tooth at every prime power")]),
        dict(text="But the comb is the same in every direction. Spin it round the axis, and each tooth draws a ring through its "
                  "prime's column. Zeta's zeros know how high a prime sits, not its hour on the wheel.",
             camera=C(at="68008,0,-19>68000,0,-20", fit="130,73>124,118", el="8>30", az=-90, spin=0, span=4, rise=.04),
             actions=[at("spin", "exp:hide:JM comb labels"), at("each", tw("fw.spin", 1, "{each~sits}")),
                      say("spun round the axis:  a ring per prime power", "ζ's zeros fix the height, not the hour")]),
        dict(text="E eight knows more. Its field has eight L-functions, and at the height of seven each one's zeros light the tooth "
                  "of seven, turned by its own character: along one, minus one, i or minus i.",
             camera=C(at="68000,0,-20>68000,0,-12", fit="124,118>34,30", el="30>70", az=-90, spin=0, span=5, rise=.04),
             actions=[at("e", tw("fw.spin", 0, 3)), at("height", tw("fw.scan", -1, 0)), at("height", "exp:hide:JM comb"),
                      at("height", "exp:hide:JM comb labels"),
                      at("height", "exp:fwhl:7"), at("light", "exp:fwchars:7"),
                      say("eight L-functions at the height of 7", "each tooth turned by its character χ(7):  1, −1, i or −i")]),
        dict(text="Turn each one back by its character at an hour, and chain the eight. At every other hour they cancel; only at "
                  "hour seven do all eight line up, and the chain swings onto the spoke of seven.",
             camera=C(at="68000,0,-12", fit="34,30", el=70, az=-90, spin=0, rise=.04),
             actions=[at("turn", "exp:fwturn:7:{turn~end}"), say("turned back by χ(h), and chained", "the eight line up only for the hour of 7")]),
        dict(text="Do this at every height, and the comb splits into eight fans, one for each gold hour: only the primes on spoke h "
                  "survive in fan h.",
             camera=C(at="68000,0,-12>68000,0,-16", fit="34,30>130,130", el="70>46", az=-90, spin=0, span=2.5, rise=.04),
             actions=["exp:fwunhl", "exp:clearchild:FW chars", at("splits", tw("fw.fan", 1, "{splits~survive}")),
                      say("eight L-functions, eight fans", "only the primes on spoke h survive")]),
        dict(text="Forty-nine lights on spoke nineteen, as seven squared. Eleven and forty-one share spoke eleven. Seven thousand "
                  "four hundred zeros sort every prime up to four hundred onto its spoke.",
             camera=C(at="68000,0,-16", fit="130,130>110,110", el="46>62", az="+", spin=2, span=10, rise=.04),
             actions=[at("fortynine", "exp:fwhl:49"), at("fortynine", call("fun", 49, 0, 0, "49 = 7²:  hour 19")),
                      at("eleven", "exp:fwhl:11"), at("eleven", call("fun", 11, 0, 0, "11")), at("fortyone", "exp:fwhl:41"),
                      at("fortyone", call("fun", 41, 0, 0, "41 = 11 + 30")),
                      say("7 420 zeros up to t = 1000", "ζ's give the height, the other seven the hour")]),
        dict(text="So the zeros are not points in the stack. They are frequencies of its height. Added up, they rebuild the prime "
                  "powers, and turned by E eight's characters, they put each prime on its spoke. They reconstruct the primes; "
                  "they do not predict them.",
             camera=C(at="68000,0,-16", fit="110,110>124,140", el="62>30", az="+", spin=4, span=12, rise=.04),
             actions=["exp:uncall", "exp:fwunhl", "exp:show:JM coils",
                      say("the zeros are frequencies of the height", "they rebuild the primes:  they do not predict them")], pause=1.4),
    ])

# ---------------------------------------------------------------------------------------------------------------
# the zeros inside E8, 5: 240 roots and infinitely many counting numbers: the wheel tilts, and E8's shells stack under it
# ---------------------------------------------------------------------------------------------------------------

TOWER = dict(el=22, spin=1, rise=.08)
WHEEL5 = (36254, 36346, -46, 46)                    # E8's wheel with the spiral to radius 40 and its hour labels
XZ5 = dict(
    id="xz5", title="240 roots, infinitely many numbers", subtitle="the zeros inside E8, part 5:  E8's clock and E8's shells",
    setup=["exp:stage", "exp:backdrop:off", "exp:e8wheel:66:150:15:40", "exp:hide:E8 spiral", "exp:hide:E8 impostors", "exp:hide:E8 rings",
           "exp:e8tower:60:attach:16", "exp:hide:E8 tower A", "exp:hide:E8 tower B", "exp:hide:E8 tower C",
           "exp:e8cubesat:7:22,0,96:1.6:3:.4:60", "exp:e8cubesat:6:-62,0,80:1.6:3:.4:60", "exp:hide:E8 cubes 6", "exp:hide:E8 cubes 7",
           "exp:hide:E8 cubes 6 label", "exp:hide:E8 cubes 7 label"],
    lines=[
        dict(text="E eight has two hundred and forty roots, shown here in its Coxeter plane as eight rings of thirty. So how can "
                  "so few points hold infinitely many counting numbers?",
             camera=C(at="36300,0,0", fit="44,50", el=89.5, az=-90, spin=0, rise=.035),
             actions=[say("E8:  240 roots", "8 rings of 30 in the Coxeter plane"), "exp:reveal:E8 rings:{two~thirty}", "exp:snap"]),
        dict(text="They don't have to. The roots set E eight's clock: its Coxeter element turns the plane by a thirtieth of a turn, "
                  "so the wheel has thirty hours, and eight of them, E eight's exponents, are gold.",
             camera=C(at=f"36300,0,0>{BOX(*WHEEL5)['at']}", fit=f"44,50>{BOX(*WHEEL5)['fit']}", el=89.5, az=-90, spin=0, hold=.5, span=3, rise=.035),
             actions=["exp:show:E8 rings", "exp:e8walk:line", say("E8's clock:  30 hours", "the 8 gold hours:  1, 7, 11, 13, 17, 19, 23, 29")]),
        dict(text="Drop every counting number onto the wheel at its hour, n mod thirty, one turn further out each time round. The "
                  "spiral never ends.",
             camera=TOP(WHEEL5),
             actions=[at("drop", "exp:reveal:E8 spiral:{drop~end}"), say("every n at its hour", "n mod 30:  the hours repeat, the radius grows")]),
        dict(text="Grey it out and follow one hour. Seven, thirty-seven, sixty-seven and ninety-seven all land on hour seven, "
                  "each one turn further out: all four are primes. One hundred and twenty-seven, too.",
             camera=C(at=f"{BOX(*WHEEL5)['at']}>36324,2,0", fit=f"{BOX(*WHEEL5)['fit']}>60,40", el=89.5, az=-90, spin=0, rise=.035, span=4),
             actions=[at("grey", "exp:mute:E8 spiral:1.2"), at("seven#1", "exp:e8hl:7:spoke"), at("thirtyseven", "exp:e8hl:37"),
                      at("sixtyseven", "exp:e8hl:67"), at("ninetyseven", "exp:e8hl:97"), at("twentyseven", "exp:e8hl:127"),
                      at("seven#1", call("e8", 7, -.3, 1, "7", frac=.1)), at("thirtyseven", call("e8", 37, -.1, -1, "37 = 7 + 30", frac=.12)),
                      at("sixtyseven", call("e8", 67, .1, 1, "67 = 7 + 2·30", frac=.12)), at("ninetyseven", call("e8", 97, .3, -1, "97 = 7 + 3·30", frac=.12)),
                      at("twentyseven", call("e8", 127, .6, 1, "127 = 7 + 4·30", frac=.12)),
                      say("one hour:  7, 37, 67, 97, 127", "n ≡ 7 (mod 30)")]),
        dict(text="Hour six holds six, thirty-six, sixty-six and ninety-six: every one a multiple of six, off the gold spokes. The "
                  "clock sorts out two, three and five; the rest it cannot see.",
             camera=C(at=f"36324,2,0>{BOX(*WHEEL5)['at']}", fit=f"60,40>{BOX(*WHEEL5)['fit']}", el=89.5, az=-90, spin=0, rise=.035, span=4),
             actions=["exp:uncall", "exp:e8unhl", at("six#1", "exp:e8hl:6:spoke"), at("thirtysix", "exp:e8hl:36"), at("sixtysix", "exp:e8hl:66"),
                      at("ninetysix", "exp:e8hl:96"), at("six#1", call("e8", 6, 0, 0, "6 = 2·3", frac=.08, col=RED)),
                      at("ninetysix", call("e8", 96, 0, 0, "96 = 2<sup>5</sup>·3", frac=.16, col=RED)),
                      say("hour 6:  6, 36, 66, 96", "multiples of 6, off the gold spokes")]),
        dict(text="And E eight itself is not two hundred and forty points. It is an infinite lattice, and the roots are only its first "
                  "shell: the points at distance root two. Tilt the wheel, and the next shells appear beneath it.",
             camera=C(at=f"{BOX(*WHEEL5)['at']}>36300,0,16", fit=f"{BOX(*WHEEL5)['fit']}>150,84.4", el="89.5>34", az=-90, spin=0, span=6, rise=.035),
             actions=["exp:uncall", "exp:e8unhl", at("infinite", "exp:hide:E8 spiral"), at("infinite", "exp:hide:E8 walk"),
                      at("tilt", "exp:reveal:E8 tower A:{tilt~appear}"),
                      say("E8 is a lattice:  shells", "the 240 roots are its first shell, at distance √2")]),
        dict(text="The next shell, at distance two, holds two thousand one hundred and sixty points. Then six thousand seven hundred "
                  "and twenty, then seventeen thousand five hundred and twenty. Every counting number n has a shell of its own, at "
                  "distance root two n.",
             camera=C(at="36300,0,16>36300,0,40", fit="150,84.4>263,148", el="34>38", az=-90, spin=0, span=8, rise=.035),
             actions=[at("seventeen", "exp:reveal:E8 tower B:1.5"), at("every", "exp:reveal:E8 tower C:2"),
                      say("a shell for every counting number", "shell n:  the points at distance √2n:  240, 2 160, 6 720, 17 520, …")]),
        dict(text="And each shell knows its number's divisors: it holds two hundred and forty times the sum of the cubes of n's "
                  "divisors. Seven is prime: one plus three hundred and forty-three, times two forty, makes eighty-two thousand "
                  "five hundred and sixty.",
             camera=C(at="36300,0,40>36302,0,82", fit="263,148>130,73", el="38>30", az=-90, spin=0, span=5, rise=.035),
             actions=["exp:mute:E8 tower A,E8 tower B,E8 tower axis:2:1.6", "exp:reveal:E8 cubes 7:{one},{fortythree}",
                      say("shell n holds 240 σ<sub>3</sub>(n) points", "σ<sub>3</sub>(n):  the sum of the cubes of n's divisors")]),
        dict(text="Six has four divisors, one, two, three and six, so its cubes are one, eight, twenty-seven and two hundred and "
                  "sixteen: sixty thousand four hundred and eighty points, with cubes in between for its factors.",
             camera=C(at="36302,0,82>36284,0,78", fit="130,73>150,84", el=30, az=-90, spin=0, span=4, rise=.035),
             actions=["exp:show:E8 cubes 6 label", "exp:reveal:E8 cubes 6:{one#2},{eight},{twentyseven},{sixteen}",
                      say("6:  1 + 8 + 27 + 216 = 252", "240 × 252 = 60 480")]),
        dict(text="Counted over every shell at once, the points of E eight give its own zeta function: two hundred and forty times "
                  "zeta of s, times zeta of s minus three. A finite symmetry, and infinitely many shells, one for every counting "
                  "number.",
             camera=C(at="36284,0,78>36290,0,40", fit="150,84>260,146", el="30>38", az=-90, spin=0, span=8, rise=.035),
             actions=["exp:unmute:E8 tower A,E8 tower B,E8 tower axis:2",
                      say("Σ 240 σ<sub>3</sub>(n) n<sup>−s</sup> = 240 ζ(s) ζ(s − 3)", "a finite symmetry, a shell for every counting number")],
             pause=1.4),
    ])

SERIES = [XS1, XS2, XS3, XS4, XS5, XZ1, XZ2, XZ3, XZ4, XZ5]
