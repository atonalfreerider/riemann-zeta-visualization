"""
The long-form segments (1920 × 1080 and 1080 × 1920): the five discoveries (xs1 … xs5), the zeros inside E8 (xz1 … xz5)
and the twist on the twist (xorbit), each a chapter of Assets/Resources/Narration/experiments.json built by
Tools/video_series.py.

A line is {text, camera, actions, pause}. The camera is an orbit cue as a dict (see CameraRig.OrbitCue); az "+" continues
the previous line's turn from where it stopped. An action "@word|exp:…" runs as the narrator says the word (its k-th
occurrence with "@word#k"), "@3.5|exp:…" 3.5 s into the line.

Callouts (ExperimentCallouts.cs): exp:callKIND:TARGET:DX,DY[,FRAC[,RRGGBB]]:TEXT puts TEXT off in the screen direction
(DX, DY) and grows an arrow from it to the target, which blooms white as it lands. Kinds: num (a number on the surface),
wave (the top of a number's spike, TARGET N or N@prime|divisor), fun (a column top in the zeros funnel), clock and
clocklink (the bridge's clocks), bar and wheel (section IA), vertex (the live chain), fold (section MB), e8 (the wheel's
spiral), zeta (ζ(½ + it) on the curve), path (a vertex of the path of partial sums), fin (the head of a fin on the path, or
in the fins' running sum), sq and sqsq (a fin's tip on the Sacks spiral of twisted squares, and the middle of a full
square), at (a point X,Y,Z from the centre).
"""

GOLD, RED, BLUE, GREEN, WHITE, CYAN = "ffd973", "ff6b5a", "66c4ff", "7dff8f", "ffffff", "59e8d9"


def alt(wide, tall):
    """A value for both frame shapes: 'WIDE^TALL' (the segments record 16:9 and 9:16 from one script)."""
    wide, tall = str(wide), str(tall)
    return wide if wide == tall else f"{wide}^{tall}"


CAM_DEFAULTS = {"span": 12, "hold": 0, "el": 45, "spin": 3, "fov": 55, "follow": 1}


def C(at="0,0,0", fit="50,30", el=89.5, az=-90, spin=0, rise=.05, on="centre", tall=None, **kw):
    """An orbit cue: look at `at` (from the centre of `on`), framing a fit = W,H box; 'A>B' moves over span seconds.
    tall=dict(...) gives the values that differ in a 9:16 frame (each becomes 'WIDE^TALL')."""
    d = dict(on=on, at=at, fit=fit, el=el, az=az, spin=spin, **kw)
    d["raise"] = rise
    if tall:
        for k, v in tall.items():
            key = "raise" if k == "rise" else k
            if key in ("az", "spin", "on"):
                raise ValueError("az, spin and on must be the same in both shapes")
            wide = d.get(key, CAM_DEFAULTS.get(key))
            d[key] = alt(wide, v)
    return d


def call(kind, target, dx, dy, text, frac=None, col=None, tall=None):
    """A callout; tall=(dx, dy[, frac]) points it another way (and as far) in a 9:16 frame."""
    def opts(dx, dy, fr=None):
        fr = frac if fr is None else fr
        o = f"{dx},{dy}"
        if fr is not None or col:
            o += f",{fr if fr is not None else .12}"
        if col:
            o += f",{col}"
        return o
    o = opts(dx, dy) if tall is None else alt(opts(dx, dy), opts(*tall))
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


def BOX(x0, x1, y0, y1, tall=False):
    """Fit and look point that keep the box [x0, x1] × [y0, y1] (world units, from the centre, seen from straight above)
    between the caption board and the captions. 16:9: the safe band is .25 … .82 of the frame's height and .03 … .97 of
    its width, so the box sits at .535 of the height (raise .035). 9:16 (tall): the band is .29 … .80 of the height and
    .04 … .96 of the width, the box at .545 (raise .045)."""
    ch, cw = y1 - y0, x1 - x0
    aspect, band, across = (9 / 16, .51, .92) if tall else (16 / 9, .57, .94)
    hf = max(ch / band, cw / (across * aspect))
    fh = hf / 1.06
    return dict(at=f"{(x0 + x1) / 2:.1f},{(y0 + y1) / 2:.1f},0", fit=f"{fh * aspect:.1f},{fh:.1f}")


RISE = alt(.035, .045)


def TOP(*boxes, el=89.5, tall=None, tall_el=None, **kw):
    """A shot from above framing a box in the safe band; with two boxes it eases from the first to the second. el below
    89.5 tilts the camera a little (the picture leans back, so the depth shows). tall: the boxes for a 9:16 frame (the
    same boxes by default, fitted to the tall band)."""
    b = [BOX(*x) for x in boxes]
    t = [BOX(*x, tall=True) for x in (tall or boxes)]
    at = alt(">".join(x["at"] for x in b), ">".join(x["at"] for x in t))
    fit = alt(">".join(x["fit"] for x in b), ">".join(x["fit"] for x in t))
    return C(at=at, fit=fit, el=alt(el, tall_el if tall_el is not None else el), az=-90, spin=0, rise=RISE, **kw)


def NB(n0, n1, y0=-8, y1=16):
    """A box on the number line from number n0 to n1 (world, from the centre), for a tall frame's closer view."""
    return (round(100000 + arc(n0) - 3, 1), round(100000 + arc(n1) + 3, 1), y0, y1)


def LINE(n, fit="170,96", y=3, tall=None, **kw):
    """The number line seen from straight above, framed round n: the graph is drawn in the plane, up the screen. tall: a
    box (NB) for a 9:16 frame, which shows a shorter stretch of the line, closer."""
    c = C(at=f"{xp(n)},{y},0", fit=fit, el=89.5, az=-90, spin=0, rise=0, **kw)
    if tall:
        tb = BOX(*tall, tall=True)
        c["at"], c["fit"], c["raise"] = alt(c["at"], tb["at"]), alt(c["fit"], tb["fit"]), alt(0, .045)
    return c


COIL = {"on": "nwboxp", "at": "0,0,0", "el": 89.5, "az": -90, "spin": 0, "span": 6, "raise": RISE}   # follows the curling line from above
SPIRAL = dict(at="100000,0,-1", fit="106,59.6", rise=RISE, tall=dict(fit="39.7,70.6"))   # the whole spiral (radius ~18) in the safe band


def curl(word, end, h_to=.6, dot_to=.5):
    """Curl the line into the spiral between `word` and `end`, a doubling of the curl at a time, the graph shrinking with it.
    The camera follows the first 30 numbers as they wind up (nw.frame), then takes in the whole spiral."""
    return [tw("nw.frame", 30, 0), at(word, tw("nw.roll", .004, .8)), at(f"{word}+0.8", tw("nw.roll", 1, f"{{{word}~{end}}}", "geo")),
            at(word, tw("nw.h", h_to, f"{{{word}~{end}}}", "geo")), at(word, tw("nw.dot", dot_to, f"{{{word}~{end}}}", "geo")),
            at(word, tw("nw.labels", 0, 2)), at(end if end != "end" else f"{word}+2.4", tw("nw.frame", 151, 2))]


def uncurl(word, end, h_to=22, dot_to=2):
    """The reverse: the spiral unwinds into the line, the graph growing back."""
    return [at(word, tw("nw.frame", 30, 2.5)), at(word, tw("nw.roll", .004, f"{{{word}~{end}}}", "geo")), at(end, tw("nw.roll", 0, .8)),
            at(word, tw("nw.h", h_to, f"{{{word}~{end}}}", "geo")), at(word, tw("nw.dot", dot_to, f"{{{word}~{end}}}", "geo")),
            at(end, tw("nw.labels", 1, 1.5))]


NW_INIT = ["exp:stage", "exp:backdrop:off", "exp:nwinit", tw("nw.vis", 1, 0), tw("nw.labels", 1, 0), tw("nw.dot", alt(2, 2.4), 0),
           tw("nw.label", alt(14, 20), 0), tw("nw.h", 5, 0), tw("nw.terms.p", 0, 0), tw("nw.terms.d", 1, 0), tw("nw.mix", 0, 0),
           tw("nw.stand", 0, 0), tw("nw.wave", 1, 0), tw("nw.count", 0, 0), tw("nw.counter", 0, 0), tw("nw.split", 0, 0),
           tw("nw.frame", 1000000, 0)]
ROWS_T = (99940, 100140, -9, 174)             # in a 9:16 frame: the rows 40 apart, the critical line at the left
ROWS = TOP((99946, 100140, -9, 52), tall=[ROWS_T])   # the line to 30 with the formula's rows above it and the critical line at the left
XS1 = dict(
    id="xs1", title="The saddle is a wave", subtitle="discovery 1 of 5:  two master waves on the Sacks spiral",
    setup=["exp:stage", "exp:backdrop:off"],
    lines=[
        # the teaser: where the segment is going
        dict(text="Claude, an AI math research assistant, and I set out to find the geometry hidden in the counting numbers. "
                  "This is where it leads: the Sacks spiral on its saddle, carrying the master wave of zeta's zeros, which "
                  "spikes at every prime, and the curve of zeta rising out of the saddle's centre, touching its axis at every "
                  "zero. Here is how it is built.",
             camera="home_orbit",
             actions=["exp:home:240:line", say("where this leads", "ζ's master wave on the Sacks spiral  ·  ζ's curve rising from the saddle's centre"),
                      "exp:snap"]),
        dict(text="We begin with the plainest picture: the counting numbers on a line, one dot for each. A prime can be divided "
                  "only by one and itself. {2, 3, 5 and 7|Two, three, five and seven} are prime, and so are eleven and thirteen, in gold. {4 = 2 · 2,|Four "
                  "is two times two,} {6 = 2 · 3,|six is two times three,} {9 = 3 · 3:|nine is three times three:} those are not.",
             camera=LINE(8, fit="100,56", y=2, tall=NB(1, 13, -10, 18)),
             actions=NW_INIT + ["exp:snap", say("the counting numbers", "a prime can be divided only by 1 and itself"),
                      at("begin", tw("nw.count", 16, "{begin~prime}", "linear")),
                      at("two", "exp:nwflare:2"), at("two", call("nw", "2", -.3, 1, "2", col=GOLD)),
                      at("three", "exp:nwflare:3"), at("three", call("nw", "3", 0, 1, "3", col=GOLD)),
                      at("five", "exp:nwflare:5"), at("five", call("nw", "5", 0, 1, "5", col=GOLD)),
                      at("seven", "exp:nwflare:7"), at("seven", call("nw", "7", 0, 1, "7", col=GOLD)),
                      at("eleven", "exp:nwflare:11"), at("eleven", call("nw", "11", 0, 1, "11", col=GOLD)),
                      at("thirteen", "exp:nwflare:13"), at("thirteen", call("nw", "13", .3, 1, "13", col=GOLD)),
                      at("four", call("nw", "4", -.3, -1, "4 = 2·2", col=RED)), at("six", call("nw", "6", 0, -1, "6 = 2·3", col=RED)),
                      at("nine", call("nw", "9", .3, -1, "9 = 3·3", col=RED))]),
        # the first zero enters: from the critical line into the formula, then onto the line
        dict(text="Over this line the zeros of zeta draw a wave. The zeros lie on the critical line, real part one half, and "
                  "the first is at height fourteen point one three. That height becomes the frequency of a cosine: {−cos(14.13 · log x)|minus cos "
                  "of fourteen point one three times log x}. Its crests tick on the logarithm: at seven point four, eleven point "
                  "five, eighteen and twenty-eight, each about one and a half times the last.",
             camera=TOP((99960, 100080, -9, 30), (99946, 100140, -9, 52), span=4, tall=[ROWS_T, ROWS_T]),
             actions=["exp:uncall", tw("nw.count", 32, 3), at("zeros#1", f"exp:nwrows:p:3:{alt(13, 40)}:{alt(3.5, 9)}:{alt(30, 64)}"),
                      say("each zero, one wave", "the zero ½ + 14.13i:  its height is the frequency of −cos(14.13 · log x)"),
                      at("first", "exp:nwenter:1:{first~seven}"), at("cos", tw("nw.counter", 1, 1)),
                      at("seven", call("nw", "7.39", 0, 1, "7.4", frac=.06, tall=(-.7, 1, .05))),
                      at("eleven", call("nw", "11.53", 0, 1, "11.5", frac=.06, tall=(0, 1, .05))),
                      at("eighteen", call("nw", "17.98", 0, 1, "18.0", frac=.06, tall=(0, 1, .05))),
                      at("twentyeight", call("nw", "28.05", 0, 1, "28.1", frac=.06, tall=(0, 1, .05)))]),
        dict(text="The second zero, at twenty-one point zero two, adds its own cosine, and the third, at twenty-five point zero "
                  "one, another. Each zero's height is the frequency of one wave, and the waves add up.",
             camera=ROWS,
             actions=["exp:uncall", at("second", "exp:nwenter:2:{second~third}"), at("third", "exp:nwenter:3:{third~waves}"),
                      say("three zeros, three waves", "−cos(14.13 · log x) − cos(21.02 · log x) − cos(25.01 · log x)")]),
        dict(text="Add the waves of more zeros: ten, a hundred, a thousand, ten thousand. They agree at the primes and cancel "
                  "between them, and the sum sharpens into spikes, one at every prime and at every power of a prime.",
             camera=LINE(22, fit="220,124", tall=NB(1, 34, -10, 62)),
             actions=["exp:uncall", "exp:mute:NW rows:1.2:1.9", tw("nw.h", alt(22, 36), 1.5), tw("nw.count", 45, 3),
                      tw("nw.terms.p", 10000, "{add~ten#2}", "geo"), at("agree", "exp:nwrowsclear"),
                      say("adding zeros", "−Σ cos(γ · log x) over the zeros ½ + iγ:  they agree at the primes")]),
        dict(text="{7|Seven} is a prime, and it has a spike. {8 = 2³|Eight is two cubed}, a power of {2|two}, and it has one too; so does {9 = 3².|nine, "
                  "three squared.} But {10 = 2 · 5|ten, two times five,} has no spike, and neither does {12 = 2² · 3.|twelve, two squared times three.}",
             camera=LINE(9.5, fit="64,36", y=3, tall=NB(6, 13, -10, 20)),
             # the callouts wait for the graph to finish shrinking (nw.h), so they land on the spikes as drawn
             actions=[tw("nw.h", alt(7, 9), 1.2), at("seven+1.3", call("nw", "7", -.3, 1, "7:  prime")), at("eight", call("nw", "8", .1, 1, "8 = 2³")),
                      at("nine", call("nw", "9", -.15, 1, "9 = 3²", frac=.2, tall=(.6, 1, .12))),
                      at("ten", call("nw", "10", .5, 1, "10 = 2·5:  no spike", frac=.2, col=RED, tall=(-.25, -1, .12))),
                      at("twelve", call("nw", "12", .6, 1, "12 = 2²·3:  no spike", frac=.3, col=RED, tall=(.35, 1, .27))),
                      say("spikes at the primes and prime powers", "and nowhere else")]),
        dict(text="This is the cosine wave of the home view, drawn the way a mathematician would draw it, along the number line. "
                  "It is the explicit formula of Riemann and von Mangoldt: added up, the zeros spike exactly at the prime powers.",
             camera=LINE(75, fit="720,405", y=0, tall=NB(1, 40, -12, 130)),
             actions=["exp:uncall", tw("nw.h", alt(66, 78), 1.5), tw("nw.count", 150, "{this~end}", "linear"),
                      say("the prime master wave", "10 000 zeros:  the explicit formula, on the number line")]),
        dict(text="Now curl the number line up, one full turn for each step of the square root. Watch the graph curl with it: "
                  "each number keeps its place along the line, and one, four, nine, sixteen, twenty-five, the squares, come to rest "
                  "on a single ray. This is the Sacks spiral.",
             camera=COIL,
             actions=["exp:uncall"] + curl("curl", "ray") +
                     [at("squares", tw("nw.rays", 1, 1.5)),
                      at("ray+0.3", call("nw", "16", 0, 0, "16 = 4²", col=BLUE)), at("ray+0.9", call("nw", "25", 0, 0, "25 = 5²", col=BLUE)),
                      say("curling the line", "one turn for each step of √n:  the Sacks spiral")]),
        # the square root is the critical line's one half
        dict(text="The square root is {n<sup>½</sup>:|n to the power one half:} the same {½|one half} as zeta's critical line. On that line every "
                  "zero's wave swells like the square root of n, the radius of this very spiral, while every term of zeta "
                  "shrinks like one over it. The spiral and the critical line are mirror images through the power {½,|one half,} "
                  "which is why they reflect the same distribution of primes.",
             camera=C(**SPIRAL),
             actions=["exp:uncall", at("root", f"exp:nwradius:100:{alt(24, 13)}:radius √100 = 10"),
                      at("same", say("one half, twice", "the spiral's radius √n = n<sup>½</sup>   ·   ζ's critical line:  Re s = ½")),
                      at("swells", say("one half, twice", "each zero's wave swells like n<sup>½</sup>   ·   each term of ζ shrinks like n<sup>−½</sup>"))]),
        dict(text="Now stand the graph up out of the plane. On the spiral it becomes the curtain of the home view: a spike on every "
                  "prime and every prime power; ninety-seven, a prime, and {64 = 2<sup>6</sup>.|sixty-four, two to the sixth.}",
             camera=C(**(SPIRAL | dict(tall=dict(fit="39.7,70.6>41,73"))), el="89.5>40", az=-90, spin=0, span=7),
             actions=["exp:uncall", "exp:nwradius:0:24:", tw("nw.rays", 0, 1.5), at("stand", tw("nw.stand", 1, "{stand~home}")),
                      at("stand", tw("nw.h", 6, "{stand~home}")),
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
                  "its own. His first wave is a sine of {4π√x|four pi times the square root of x}, so it ticks on the square root, and "
                  "its crests spread apart only slowly: eight point six, eleven point eight, fifteen point five.",
             camera=LINE(15, tall=NB(1, 17, -10, 22)),
             actions=[at("count", tw("nw.mix", 1, "{count~his}")),
                      at("eight", call("nw", "8.63", 0, 0, "8.6", tall=(0, 1, .06))),
                      at("eleven", call("nw", "11.82", 0, 0, "11.8", tall=(0, 1, .12))),
                      at("fifteen", call("nw", "15.5", 0, 0, "15.5", tall=(0, 1, .06))),
                      say("Voronoi's first wave", "sin(4π√x − π/4):  it ticks on √x")]),
        # his next waves enter, each as strong as its number's count of divisors
        dict(text="His wave number m ticks on {√(mx),|the square root of m times x,} and its strength is m's own count of divisors. {2|Two} "
                  "has two divisors, {1 and 2;|one and two;} {3|three} has two; {4|four} has three, {1, 2 and 4.|one, two and four.} Each enters his formula as "
                  "one more wave.",
             camera=ROWS,
             actions=["exp:uncall", tw("nw.h", 5, 1.2), f"exp:nwrows:d:4:{alt(13, 40)}:{alt(3, 8)}:{alt(30, 60)}:2",
                      say("Voronoi's waves", "the wave of m:  d(m) · m<sup>−¼</sup> · sin(4π√(mx) − π/4),  d(m) its divisors"),
                      at("two#2", "exp:nwenter:2:{two#2~three#1}"), at("three#1", "exp:nwenter:3:{three#1~four}"),
                      at("four", "exp:nwenter:4:{four~end}")]),
        dict(text="Add more of his waves, ten, a hundred, four thousand, and they too sharpen into spikes. But now there is a "
                  "spike at every whole number, and each one stands as tall as its number's count of divisors.",
             camera=LINE(20, fit="200,112", tall=NB(1, 30, -10, 60)),
             actions=["exp:uncall", "exp:mute:NW rows:1.2:1.9", tw("nw.h", alt(22, 34), 1.5), tw("nw.count", 45, 3),
                      tw("nw.terms.d", 4000, "{add~divisors}", "geo"), at("spike", "exp:nwrowsclear"),
                      say("Voronoi's divisor wave", "a spike at every n, as tall as its count of divisors")]),
        dict(text="{7,|Seven,} a prime, has two divisors, {1 and 7,|one and seven,} so its spike is short. {12|Twelve} has six: one, two, three, four, "
                  "six and twelve. {16 = 2<sup>4</sup>|Sixteen, two to the fourth,} has five.",
             camera=LINE(11.5, fit="64,36", y=3, tall=NB(6, 17, -10, 26)),
             actions=[tw("nw.h", alt(12, 15), 1.2), at("seven#1+1.3", call("nw", "7", -.5, 1, "7:  2 divisors", col=GOLD)),
                      at("twelve", call("nw", "12", 0, 0, "12:  6 divisors")), at("one#2", "exp:nwdivisors:12:ffd973:{one#2~twelve#2}"),
                      at("sixteen", "exp:uncall"), at("sixteen+0.1", call("nw", "16", .5, 1, f"16 = 2{sup(4)}:  5 divisors")),
                      say("the spikes count divisors", "7:  2   ·   12:  6   ·   16:  5")]),
        dict(text="The count comes from the prime factors: add one to each exponent, and multiply. {12 = 2² · 3,|Twelve is two squared times "
                  "three,} so {3 · 2 = 6.|three times two, six.} {30 = 2 · 3 · 5,|Thirty is two times three times five,} {2 · 2 · 2 = 8.|two times two times two, eight.} And "
                  "{36 = 2² · 3²|thirty-six, two squared times three squared,} has {3 · 3 = 9.|three times three, nine.}",
             camera=LINE(24, fit="170,96", tall=NB(10, 37, -10, 56)),
             actions=["exp:uncall", tw("nw.h", alt(22, 30), 1.5), say("count the divisors from the factors", "n = p<sup>a</sup> q<sup>b</sup> …  has  (a + 1)(b + 1) …  divisors"),
                      at("twelve#1+1.5", call("nw", "12", -.5, 1, "12 = 2²·3:  3·2 = 6", tall=(.2, 1, .08))),
                      at("thirty", call("nw", "30", -.5, 1, "30 = 2·3·5:  2·2·2 = 8", frac=.14, tall=(0, 1, .16))),
                      at("thirtysix", call("nw", "36", .1, 1, "36 = 2²·3²:  3·3 = 9", frac=.16, tall=(-.1, -1, .17)))]),
        dict(text="Take his sum back down to its first wave, and curl the line up into the spiral once more.",
             camera=COIL,
             actions=["exp:uncall", tw("nw.count", 150, 1.5), tw("nw.terms.d", 1, "{take~curl}", "geo")] + curl("curl", "end", h_to=1) +
                     [say("curling Voronoi's first wave", "the same Sacks spiral")]),
        # why the first wave goes up and down on the coiled spiral: the angle is 2π√n, the wave a sine of twice it
        dict(text="On the spiral the angle is two pi times the square root of n, so his first wave, {sin(4π√n),|the sine of four "
                  "pi root n,} is a sine of twice the angle: it rises and falls twice on every turn. Stand it up and look from "
                  "the side: it stands up in two opposite quarters, the brown ones, and hangs down in the other two, the blue "
                  "ones.",
             camera=C(at="100000,0,-1", fit="106,59.6>70,52", el="86>24", az=-105, spin=1, rise=RISE, span=7,
                      tall=dict(fit="39.7,70.6>40,71")),
             actions=["exp:uncall", tw("nw.ribbon", 1, 1.5), at("stand", tw("nw.stand", 1, 2.5)), at("stand", tw("nw.h", 3.5, 2.5)),
                      at("side", "exp:nwquarters:on:34"),
                      say("Voronoi's first wave on the spiral", "the angle θ = 2π√n, so sin(4π√n) = sin 2θ:  up twice and down twice on every turn"),
                      at("stands", say("up in two quarters, down in two", "brown:  the wave above the plane   ·   blue:  below it"))]),
        # where his crests sit: 22.5° before the squares and the pronic numbers
        dict(text="Its crests sit at the same two angles turn after turn, {22.5°|twenty-two and a half degrees} before the ray of the "
                  "squares, just short of sixty-four, and before the ray of the pronic numbers, just short of seventy-two.",
             camera=C(at="100000,0,-1", fit="70,52>106,59.6", el="24>86", az="+", spin=3, rise=RISE, span=4,
                      tall=dict(fit="40,71>39.7,70.6")),
             actions=["exp:nwquarters:off", at("crests", tw("nw.rays", 1, 1.5)), at("twentytwo", "exp:nwangle:on:24"),
                      at("sixtyfour", call("nw", "64", 0, 0, "64 = 8²", col=WHITE)),
                      at("seventytwo", call("nw", "72", 0, 0, "72 = 8·9", col=WHITE)),
                      say("Voronoi's first wave on the spiral", "two crests per turn, 22.5° before the squares and the pronic numbers")]),
        dict(text="Turn it into a zero's wave on the same spiral, and its crests start to drift into arms, because the zeros tick "
                  "on the logarithm. Turn it back: Voronoi's waves tick on the square root, the spiral's own clock, so each one "
                  "stands still.",
             camera=C(**SPIRAL, el=86, az=-90, spin=0),
             actions=["exp:uncall", "exp:nwangle:off", tw("nw.rays", 0, 1.5), tw("nw.h", 1, 1.5), tw("nw.first.p", 100, 0),
                      at("turn", tw("nw.mix", 0, "{turn~drift}")), at("back", tw("nw.mix", 1, "{back~square}")),
                      say("two clocks", "ζ's zeros tick on log n  ·  Voronoi's waves tick on √n")]),
        dict(text="Bend the plane into the saddle that the Sacks spiral is drawn on. It falls twice and rises twice on every turn, "
                  "its valleys along the rays of the squares and the pronic numbers, where primes are scarcest. Voronoi's first "
                  "wave has the same shape, turned over and turned by {22.5°:|twenty-two and a half degrees:} the {−π/4|minus pi over four} in "
                  "his formula moves every crest by a sixteenth of a turn.",
             camera=C(at="100000,0,-3", fit="106,59.6>56,42", el="86>26", az=-90, spin=2, rise=.04, span=8,
                      tall=dict(fit="39.7,70.6>41.7,74.2", rise=.045)),
             actions=[at("bend", tw("nw.bend", 1, "{bend~rays}")), at("bend", tw("nw.h", 2, "{bend~rays}")),
                      at("valleys", tw("nw.rays", 1, 1.5)), at("voronois", "exp:nwangle:on:0"),
                      say("the saddle", "down twice and up twice on every turn:  valleys on the squares and pronic numbers"),
                      at("voronois", say("the saddle and Voronoi's wave", "the same shape, turned over and turned 22.5°:  the phase −π/4 in his formula"))]),
        # both master waves on one saddle: the primes above it, the divisors reflected below
        dict(text="Now build both master waves on this one saddle. The zeros' waves stand above it and spike at the primes; "
                  "Voronoi's waves hang below it, reflected, and every number reaches down as far as its count of divisors.",
             camera=C(at="100000,0,-3", fit="64,50>70,54", el="34>16", az="+", spin=2, rise=.075, span=5,
                      tall=dict(fit="44,78.2>45,80", rise=.045)),
             actions=["exp:nwangle:off", tw("nw.rays", 0, 1.5), tw("nw.ribbon", 0, 1.5), tw("nw.first.p", 1, 0),
                      at("build", tw("nw.terms.p", 10000, "{build~stand}", "geo")), at("build", tw("nw.terms.d", 4000, "{build~stand}", "geo")),
                      at("build", tw("nw.h", 6, 2)), at("stand", tw("nw.split", 1, 2)),
                      say("two master waves on one saddle", "above:  ζ's zeros light the primes   ·   below:  every number by its count of divisors")]),
        dict(text="Voronoi's formula is from nineteen oh four, and the explicit formula for the primes from Riemann and von "
                  "Mangoldt. What Claude and I add is the picture: the saddle under the Sacks spiral is the divisor count's first "
                  "wave, turned a sixteenth of a turn, and both master waves ride the same spiral, the primes above and the "
                  "divisors below.",
             camera=C(at="100000,0,-3", fit="70,54", el="16>22", az="+", spin=2, rise=.075, span=14,
                      tall=dict(fit="45,80", rise=.045)),
             actions=[say("what is new", "the saddle is Voronoi's first wave, turned 22.5°;  both master waves ride one spiral")], pause=1.4),
    ])

# ---------------------------------------------------------------------------------------------------------------
# 2. E8 sets the vortices: E8's clock beside the path of partial sums, and the blocks of the gold path lifted onto it
# ---------------------------------------------------------------------------------------------------------------

E8W = dict(at="36300,0,0", fit="100,100", el=89.5, spin=0)
PW = 104000          # the path's origin (from the centre); 12 world units per unit of the complex plane
CLOCK = alt("104040,-9,0", "104009,-44,0")         # E8's clock: right of the path, or below it in a 9:16 frame
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
BLOCK15_T = (104008.2, 104011.6, -13.8, -11.6)   # 9:16: close on the eight arrows and their labels
WHEEL_BARS = (104024, 104106, -23.5, 6.5)
ALL2 = (103998, 104106, -23.5, 6.5)
# the same in a 9:16 frame: the path above, E8's clock below it, the chart of traces below the clock
WHEEL_BOX_T = (103992, 104026, -61, -27)
PATH_WHEEL_T = (103992, 104026, -61, 3)
PATH_T = (103998, 104021, -21.5, 1.5)          # the gold path at t = 5000 alone
WHEEL_BARS_T = (103980, 104038, -89, -27)       # (the chart wider in a 9:16 frame: 1.6 a bar)
ALL2_T = (103980, 104038, -89, 3)
BARS_AT = alt("104064,-9,0", "103984,-77,0")
TILT = 74            # the clock and the gold path seen a little from the side, so the picture has depth
CLIMB = C(at="104006,-8,-20", fit="161.8,91", el=62, az=-90, spin=0, rise=RISE,
          tall=dict(at="104006,-5.4,-20.2", fit="51.9,92.2"))   # the plane up its t axis, the ground below
XS2 = dict(
    id="xs2", title="E8 sets the vortices", subtitle="discovery 2 of 5:  E8's clock inside zeta's path of partial sums",
    setup=PATH_SETUP + [tw("pw.logh", 1, 0), tw("pw.ground", 0, 0), f"exp:e8wheel:40:150:11:12:{CLOCK}:6", "exp:hide:E8 spiral",
                        "exp:hide:E8 impostors", "exp:hide:E8 rings"],
    lines=[
        dict(text="Discovery two. Claude and I drew zeta's series as a path of partial sums: one arrow for every counting number "
                  "n, one over root n long, each turning from the last by {t · log(n/(n − 1)).|t times the logarithm of n over n minus one.}",
             camera=PC(at="11,-7,0>12,-16,0", fit="44,26>70,42", el=80, span=6, hold=8, tall=dict(at="11,-6,0>11.5,-17,0", fit="28,50>45,80")),
             actions=[say("the path of partial sums", "one arrow n<sup>−½ − it</sup> for every counting number, laid end to end"),
                      at("path", tw("pw.n", 1, 1.2)), at("over", tw("pw.n", 2, 1.4)), at("turning", tw("pw.n", 3, 1.4)),
                      at("logarithm", tw("pw.n", 4, 1.4)), at("minus", tw("pw.n", 8, "{minus~end}", "linear")), "exp:snap"]),
        # the plane climbs its t axis; the ground (t = 0) stays below
        dict(text="Raise t, and the path climbs its axis, winding up as it goes; the grid below is the ground, where t is zero. "
                  "At the first zero, t equal to fourteen point one three, the path's end lands on zero. Higher still, at the "
                  "hundred and fifty-ninth zero, three hundred thirty-three point six, it curls into vortices, with straight "
                  "spokes between them.",
             camera=C(at="104012,-12,0>104006,-8,-20", fit="70,42>161.8,91", el="80>62", az=-90, spin=0, rise=RISE, span=5,
                      tall=dict(at="104011.5,-17,0>104006,-5.4,-20.2", fit="45,80>51.9,92.2")),
             actions=[tw("pw.marks", 0, .8), tw("pw.n", 1000000, 1.5), at("raise", tw("pw.ground", 1, 1.5)),
                      at("raise", tw("pw.rise", 1, "{raise~ground}")), at("raise", tw("pw.axis", 1, 1.5)),
                      at("raise", tw("pw.t", 14.134725, "{raise~lands}", "geo")),
                      at("lands", tw("pw.tail", 1, 1.5)), at("zero#3", call("pathzeta", 0, 0, 0, "ζ = 0", col=GOLD)),
                      at("higher", "exp:uncall"), at("higher", tw("pw.t", 333.645, "{higher~vortices}", "geo")),
                      at("vortices", tw("pw.eyes", 1, 1.5)),
                      say("climbing the t axis", "the first zero, t = 14.13;  the 159th, t = 333.6;  the ground is t = 0")]),
        dict(text="The spokes sit near {n ≈ t/2πk:|n equal to t over two pi k:} fifty-three, twenty-seven, eighteen. Crossing spoke k, the path "
                  "moves by the {k-th|kay-th} term of zeta's reflected series, so every vortex belongs to a counting number.",
             camera=CLIMB,
             actions=[say("spoke k at n ≈ t / 2πk", "t = 333.6:  53,  27,  18, …"),
                      at("fiftythree", call("path", 53, 0, 0, "spoke 1:  n ≈ 53")), at("twentyseven", call("path", 27, 0, 0, "spoke 2:  n ≈ 27")),
                      at("eighteen", call("path", 18, 0, 0, "spoke 3:  n ≈ 18")),
                      at("crossing", say("each spoke is a counting number", "crossing spoke k, the path moves by χ(s) k<sup>s−1</sup>"))]),
        # back down to the ground, where E8's clock lies
        dict(text="Bring the plane back down to the ground, and beside it, E eight's clock: its two hundred and forty roots turn by "
                  "a thirtieth of a circle, and eight of the thirty hours are gold, the ones with no factor two, three or five. Keep "
                  "only the arrows of numbers on those eight hours: the other arrows shrink away, and the path re-forms.",
             camera=C(at=f"104006,-8,-20>{BOX(*PATH_WHEEL)['at']}", fit=f"161.8,91>{BOX(*PATH_WHEEL)['fit']}", el=f"62>{TILT}",
                      az=-90, spin=0, rise=RISE, span=5,
                      tall=dict(at=f"104006,-5.4,-20.2>{BOX(*PATH_WHEEL_T, tall=True)['at']}", fit=f"51.9,92.2>{BOX(*PATH_WHEEL_T, tall=True)['fit']}")),
             actions=["exp:uncall", tw("pw.eyes", 0, 1), at("bring", tw("pw.rise", 0, "{bring~clock}")), at("bring", tw("pw.axis", 0, 1.5)),
                      at("clock", tw("pw.ground", 0, 1.5)), at("clock", "exp:reveal:E8 rings:{clock~eight#2}"),
                      at("eight#2", "exp:e8walk:{eight#2~keep}"), at("keep", tw("pw.gold", 1, "{keep~reforms}")),
                      say("E8's clock and its eight gold hours", "keep only the arrows of n with no factor 2, 3 or 5")]),
        dict(text="Raise t to five thousand. The gold path stretches out: a tangle at the start, then long smooth runs, and at its "
                  "end a coil of tiny loops.",
             camera=TOP(PATH_WHEEL, el=TILT, span=4, tall=[PATH_WHEEL_T, PATH_T]),
             actions=[at("raise", tw("pw.t", 5000, "{raise~stretches}", "geo")),
                      say("the gold path at t = 5000", "only the n on E8's eight gold hours:  25 795 terms")]),
        # these are the arrows of actual counting numbers
        dict(text="Look closely at the long run near n equal to fifteen ninety. These are the arrows of actual counting numbers: "
                  "fifteen seventy-seven, fifteen seventy-nine, fifteen eighty-three and on to sixteen oh three, the eight numbers "
                  "here on E eight's gold hours, one for each hour. All eight arrows point the same way.",
             camera=TOP(PATH_WHEEL, BLOCK15, el=TILT, span=4, tall=[PATH_T, BLOCK15_T]),
             actions=[at("look", "exp:tbblock:15"), at("look", "exp:mute:PW path:1.5"),
                      say("one run, close up", "the arrows of 1577, 1579, 1583, 1589, 1591, 1597, 1601, 1603:  one per gold hour")]),
        # why the blocks sit there
        dict(text="Why here? Thirty numbers on, each arrow has turned by about {30t/n.|thirty t over n.} Near {n = 30t/(2π · 15),|n equal to thirty t over two "
                  "pi times fifteen,} about fifteen ninety, that is fifteen whole turns, so every thirty numbers the pattern "
                  "repeats exactly: the run is block after block of the same eight arrows.",
             camera=TOP(BLOCK15, tall=[BLOCK15_T]),
             actions=[say("the turn over thirty numbers", "t · log((n + 30)/n) ≈ 30t/n = 2π · 15  at  n ≈ 30t / 30π ≈ 1590")]),
        dict(text="Lift one block onto E eight's clock without turning a single arrow. The clock turns instead, until its hour "
                  "zero points the way the arrow of fifteen ninety would. Now each arrow points at fifteen times its own hour, and "
                  "every gold hour times fifteen is hour fifteen: all eight line up, and the block adds to minus eight.",
             camera=TOP(BLOCK15, WHEEL_BOX, el=TILT, span=5, tall=[BLOCK15_T, WHEEL_BOX_T]),
             actions=[at("lift", "exp:mute:E8 rings:1.5:1.25"), at("lift", "exp:tbscale:8"), at("lift", "exp:tboverlay:15:{lift~now}"),
                      say("the clock turns, not the arrows", "hour 0 along the arrow of 1590:  arrow n then points at hour 15·n"),
                      at("now", say("the block on E8's clock", "arrow n points at hour 15·n:  all eight at hour 15, the sum −8"))]),
        dict(text="Near n equal to forty-seven seventy, the clock turns again to meet that block. Each arrow points at five times "
                  "its hour: four land on {hour 5|hour five} and four on {hour 25,|hour twenty-five,} and the block adds to four.",
             camera=TOP(PATH_WHEEL, WHEEL_BOX, el=TILT, span=5, hold="{each}", tall=[PATH_WHEEL_T, WHEEL_BOX_T]),
             actions=["exp:clearchild:TB chain", "exp:clearchild:TB block", at("near", "exp:tbblock:5"),
                      at("near", call("block", 5, 0, 0, "n ≈ 4770")),
                      at("each", "exp:uncall"), at("clock", "exp:tboverlay:5:{clock~four#2}"),
                      say("near n = 4770:  k = 5", "arrow n points at hour 5·n:  hours 5 and 25, the sum 4")]),
        # the prime factors play no part: only the hours do
        dict(text="And at the very end, near n equal to twenty-three thousand eight hundred and eighty, each arrow points at its "
                  "own hour, and the eight nearly cancel: minus one. Their prime factors play no part. Twenty-three thousand eight "
                  "hundred and eighty-one is eleven times thirteen times a hundred and sixty-seven, but on the clock it is simply "
                  "hour one.",
             camera=TOP(PATH_WHEEL, WHEEL_BOX, el=TILT, span=5, hold="{each}", tall=[PATH_WHEEL_T, WHEEL_BOX_T]),
             actions=["exp:uncall", "exp:clearchild:TB chain", "exp:clearchild:TB block", at("near", "exp:tbblock:1"),
                      at("near", call("block", 1, 0, 0, "n ≈ 23 880")),
                      at("each", "exp:uncall"), at("each", "exp:tboverlay:1:{each~nearly}"),
                      say("near n = 23 880:  k = 1", "arrow n points at its own hour:  the sum −1"),
                      at("prime", say("only the hour counts", "23 881 = 11 · 13 · 167:  on the clock, hour 1"))]),
        dict(text="Let k run from one to thirty, and the chain of the eight gold hours dances on the clock. Each time k is a whole "
                  "number it lands on a trace of E eight's Coxeter element: minus one, one, two, four, minus two, minus four, minus "
                  "eight, eight.",
             camera=TOP(WHEEL_BOX, WHEEL_BARS, el=TILT, span=3, tall=[WHEEL_BOX_T, WHEEL_BARS_T]),
             actions=["exp:uncall", "exp:clearchild:TB chain", "exp:clearchild:TB block", "exp:wheelhome:2", "exp:unmute:E8 rings:1.5",
                      f"exp:tbbars:{BARS_AT}:14",
                      at("let", "exp:tbsweep:1:30:{let~end}"),
                      say("tr C<sup>k</sup>:  E8's Coxeter element", "the chain of the eight gold hours, each turned k hours per hour")]),
        dict(text="Measured on the path itself at t equal to five thousand, the charges of the spokes match these traces, to within "
                  "a quarter up to spoke twelve.",
             camera=TOP(WHEEL_BARS, el=TILT, tall=[WHEEL_BARS_T]),
             actions=[at("measured", "exp:tbbarsall:measured"),
                      say("measured on the gold path at t = 5000", "white: the charge of spoke k;  colour: tr C<sup>k</sup>")]),
        dict(text="Added up again, each turned by an hour, the thirty traces give back E eight's sieve: for seven they reach hour "
                  "seven on the rim; for six they close up at the centre.",
             camera=TOP(WHEEL_BARS, WHEEL_BOX, el=TILT, span=3, tall=[WHEEL_BARS_T, WHEEL_BARS_T]),
             actions=[at("seven", "exp:tbsieve:7:{seven~six}"), at("six", "exp:tbsieve:6:3"),
                      say("the sieve as a spectrum", "(1/30) Σ<sub>k</sub> tr C<sup>k</sup> e<sup>−2πikn/30</sup>:  1 on the gold hours, 0 off them")]),
        dict(text="Why it matters: a symmetry of eight dimensions sets the sign and the strength of every vortex of the gold path. "
                  "The identity is Ramanujan's; what Claude and I add is seeing E eight's clock inside zeta's own path.",
             camera=TOP(WHEEL_BOX, ALL2, el=TILT, span=10, tall=[WHEEL_BARS_T, ALL2_T]),
             actions=["exp:unmute:E8 rings,PW path:2",
                      say("what is new", "Ramanujan's sum c<sub>30</sub>(k) = tr C<sup>k</sup>:  E8's clock inside the path of ζ")], pause=1.4),
    ])

# ---------------------------------------------------------------------------------------------------------------
# 3. where the zeros live in E8: the funnel greyed out, one number lit at a time, the coil assembled from the path's arrows
# ---------------------------------------------------------------------------------------------------------------

G1, G2, G3 = 14.134725, 21.022040, 25.010858
FUNNEL = dict(at="68000,0,-16", fit="124,140", el=26, spin=2, tall=dict(at="68000.5,0.5,-18", fit="113.2,201.2"))
# the 9:16 framings of the funnel scenes (from a projection search): floor path, horn, coil, hand of 6, comb, rings, fans
FT = dict(floor="68006.5,0,0", floorfit="25,44.5", horn="68003.1,-2.4,-9", hornfit="33.8,60", coil="68000,0,-18", coilfit="53.3,94.7",
          coilaz="68000,-7.2,-18", coilazfit="45.3,80.6", six="68002.5,-1.8,-10", sixfit="42,74.7", scan="68003.5,0,-10", scanfit="30,53.3",
          comb="68030.5,-17.4,-18", combfit="55,97.8", rings="68000,-32.4,-20.5", ringsfit="104.7,186.1", fans="68000,-17.7,-18",
          fansfit="123.6,219.8", spinfans="68000,0,-18", spinfansfit="129.2,229.6", whole="68000.5,0.5,-18", wholefit="113.2,201.2",
          close="68003,0,-6", closefit="30,53")
FW = dict(at="68000,0,-9", fit="60,52", el=24, rise=.04)          # the coil, close
FUNNEL_SETUP = ["exp:stage", "exp:backdrop:off", "exp:zfunnel", "exp:fwinit", tw("fw.gamma", G1, 0), tw("fw.n", 0, 0), tw("fw.lift", 0, 0),
                tw("fw.dim", 0, 0), tw("fw.stretch", 1, 0),
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
        dict(text="That is too much at once, so grey it out and light one number at a time. Seven is a prime: its arm points to "
                  "hour seven on the floor, and its column is one piece, log seven tall. {6 = 2 · 3,|Six is two times three,} at hour six: a "
                  "piece log three tall, and on top of it one log two tall.",
             camera=C(at="68000,0,-16>68004,0,-6", fit="124,140>36,28", el="26>22", az="+", spin=1, rise=.04, span=5,
                      tall=dict(at=f"{FT['whole']}>{FT['close']}", fit=f"{FT['wholefit']}>{FT['closefit']}")),
             actions=[at("grey", "exp:mute:GB factor columns:1.2"), at("seven#1", "exp:fwhl:7:logs"),
                      at("six", "exp:fwunhl:7"), at("six", "exp:fwhl:6:logs"),
                      say("one number at a time", "7:  prime, one piece log 7   ·   6 = 2·3:  log 3 + log 2")]),
        # the floor path is flat: the camera comes over the top and turns square to it (az −90) by the end of the line
        dict(text="Now bring in the path of partial sums from discovery two, at the first zero, t equal to fourteen point one three, "
                  "and lay it on the funnel's floor, starting at the axis. Its arrows wind round and its tail brings it back to the axis.",
             camera=C(at="68004,0,-6>68006.8,0.2,0", fit="36,28>40,22.5", el="22>84", az="+", az_to=-90, rise=RISE, span=4,
                      tall=dict(at=f"{FT['close']}>{FT['floor']}", fit=f"{FT['closefit']}>{FT['floorfit']}")),
             actions=["exp:fwunhl", "exp:mute:GB factor columns:1.5:1.85", at("bring", tw("fw.n", 15, "{bring~tail}", "linear")),
                      at("tail", tw("fw.tail", 1, 1.5)),
                      say("the path at the first zero, on the floor", "t = 14.13:  its arrows n<sup>−½ − it</sup> close on the axis")]),
        dict(text="Lift each arrow to the height of its own number. Arrow n rises to log n, the top of n's column; its tail moves "
                  "onto the axis, and it keeps both its direction and its length, one over root n. Arrow one stays on the floor, "
                  "arrow two rises to log two, arrow six to log six: stacked, the shrinking arrows narrow like a horn.",
             camera=C(at="68006.8,0.2,0>68002,0,-8", fit="40,22.5>56,40", el="84>22", az="+", spin=1.5, rise=.04, span=6,
                      tall=dict(at=f"{FT['floor']}>{FT['horn']}", fit=f"{FT['floorfit']}>{FT['hornfit']}", rise=.045)),
             actions=[tw("fw.stretch", 0, 0), tw("fw.tail", 0, 1), tw("fw.axis", 1, 1), tw("fw.path", 0, "{lift~end}"),
                      at("lift", tw("fw.lift", 1, "{lift~six#2}")), at("two#2", "exp:fwhl:2"), at("six#2", "exp:fwhl:6"),
                      at("horn", tw("fw.coil", .5, "{horn~end}")),
                      say("lift arrow n to the height log n", "it keeps its direction γ·log n and its length 1/√n:  a horn")]),
        # the stretch by √n: the critical line's ½ cancels the spiral's radius
        dict(text="Now stretch each arrow by the square root of its number: the distance of n from the centre on the Sacks "
                  "spiral, and on this funnel's floor. {1/√n × √n = 1.|One over root n, times root n, is one.} On the critical line the length "
                  "and the radius cancel, every arrow becomes one unit long, and all that is left is its direction.",
             camera=C(at="68002,0,-8>68000,0,-12", fit="56,40>70,52", el=22, az="+", spin=1.5, rise=.04, span=6,
                      tall=dict(at=f"{FT['horn']}>{FT['coil']}", fit=f"{FT['hornfit']}>{FT['coilfit']}", rise=.045)),
             actions=["exp:fwunhl", at("stretch", tw("fw.stretch", 1, "{stretch~one#2}")), at("stretch", "exp:fwhl:2"),
                      say("stretch arrow n by √n", "1/√n × √n = 1:  the critical line's ½ cancels the spiral's radius"),
                      at("one#2", say("every arrow one unit long", "n<sup>−½</sup> · n<sup>½</sup> = 1:  only the direction γ·log n is left"))]),
        dict(text="Stacked, the arrows are the hands of a clock that turns by gamma for every unit of height, and a coil runs "
                  "through their tips, one turn every point four four. That coil is the zero, wound up E eight's funnel.",
             camera=C(at="68000,0,-14", fit="76,74", el=22, az="+", az_to=-90, rise=.04,
                      tall=dict(at=FT['coil'], fit=FT['coilfit'], rise=.045)),
             actions=["exp:fwunhl", tw("fw.coil", 0, 0), at("coil", tw("fw.coil", 1, "{coil~end}")),
                      say("a zero is a coil", "one turn every 2π/γ = 0.44 of height")]),
        # the clock face from above: the hands of 2, 3 and 6 bright, the rest and the coil faded, the turns as arcs
        dict(text="And the coil reads the prime factors. {6 = 2 · 3,|Six is two times three,} so {log 6 = log 2 + log 3,|log six is log two plus log three,} and the "
                  "hand at six has turned the turn of two plus the turn of three.",
             camera=C(at="68000,0,-14>68004,-1,-9.5", fit="76,74>72,40.5", el="22>64", az=-90, spin=0, rise=RISE, span=5,
                      tall=dict(at=f"{FT['coilaz']}>{FT['six']}", fit=f"{FT['coilazfit']}>{FT['sixfit']}")),
             actions=[tw("fw.coil", .32, 2), tw("fw.dim", 1, 2), at("six#1", "exp:fwhl:6:logs"), at("plus#1", "exp:fwangles:6"),
                      say("the hand of 6 = the hand of 2 + the hand of 3", "γ·log 6 = γ·log 2 + γ·log 3:  1.56 + 2.47 = 4.03 turns")]),
        dict(text="Every zero is such a coil. {The next two:|The next two,} twenty-one point zero two and twenty-five point zero one, turn every "
                  "hand further, the higher ones more: tighter coils.",
             camera=C(at="68004,-1,-9.5>68000,0,-14", fit="72,40.5>76,74", el="64>24", az=-90, spin=0, rise=.04, span=5,
                      tall=dict(at=f"{FT['six']}>{FT['coilaz']}", fit=f"{FT['sixfit']}>{FT['coilazfit']}", rise=.045)),
             actions=["exp:fwunhl", "exp:clearchild:FW angles", tw("fw.dim", 0, 1.5), tw("fw.coil", 1, 2.5),
                      at("twentyone", tw("fw.gamma", G2, 2)), at("twentyfive", tw("fw.gamma", G3, 2)),
                      say("the next zeros", "γ = 21.02,  25.01:  tighter coils")]),
        dict(text="Now add the coils of all ten thousand five hundred zeros. A ring climbs the funnel's axis, and at each height "
                  "the coils add up to a single hand. Where they agree, at a prime or a power of a prime, the hand shoots out to the "
                  "top of that prime's column: {7, 8, 9.|seven, eight, nine.} At six and ten they cancel, and the hand stays at the axis.",
             camera=C(at="68000,0,-14>68004,0,-10", fit="76,74>56,31.5", el="24>10", az=-90, spin=0, rise=RISE, span=4,
                      tall=dict(at=f"{FT['coilaz']}>{FT['scan']}", fit=f"{FT['coilazfit']}>{FT['scanfit']}")),
             actions=[tw("fw.coil", 0, 1.5), tw("fw.hands", 0, 1.5), tw("fw.axis", 0, 1.5), "exp:mute:GB factor columns:1.5:1",
                      at("climbs", tw("fw.scan", 1.5, 0)), at("climbs", tw("fw.scan", 1.75, "{climbs~seven}")),
                      at("seven", tw("fw.scan", 2.32, "{seven~cancel}", "linear")),
                      say("adding the coils, height by height", "where they agree, a tooth of log p;  elsewhere they cancel"),
                      at("seven", call("tooth", 7, 1, -.7, "7", col=GOLD)), at("eight", call("tooth", 8, 1, .05, "8 = 2³", col=GOLD)),
                      at("nine", call("tooth", 9, 1, .8, "9 = 3²", col=GOLD)), at("six", call("tooth", 6, -1, -.6, "6:  dark", col=RED)),
                      at("ten#2", call("tooth", 10, -1, .6, "10:  dark", col=RED))]),
        dict(text="Climbing on, the ring lights every prime power up to four hundred, and leaves every composite dark. The comb it "
                  "draws is the master wave of discovery one, stood up along the logarithm.",
             camera=C(at="68004,0,-10>68008,0,-19", fit="56,31.5>130,73", el=8, az=-90, spin=0, rise=RISE, span=6,
                      tall=dict(at=f"{FT['scan']}>{FT['comb']}", fit=f"{FT['scanfit']}>{FT['combfit']}")),
             actions=["exp:uncall", at("climbing", tw("fw.scan", 6.02, "{climbing~dark}")), at("comb", tw("fw.scan", 100, 0)),
                      say("ζ's comb", "a tooth at every prime power:  the master wave, along log n")]),
        dict(text="Zeta's zeros fix a prime's height but not its hour. E eight's field has seven more L-functions; turned back by "
                  "their characters, they split the comb into eight fans, one for each gold hour. {49 = 7² lands on hour 19.|Forty-nine, seven squared, "
                  "lands on hour nineteen.}",
             camera=C(at="68008,0,-19>68000,0,-16", fit="130,73>130,130", el="8>50", az=-90, spin=0, rise=.04, span=8,
                      tall=dict(at=f"{FT['comb']}>{FT['fans']}", fit=f"{FT['combfit']}>{FT['fansfit']}", rise=.045)),
             actions=[at("split", tw("fw.scan", -1, 0)), at("split", "exp:hide:JM comb"), at("split", "exp:hide:JM comb labels"),
                      at("split", tw("fw.fan", 1, "{split~hour}")),
                      at("fortynine", call("fun", 49, 0, 0, "49 = 7²:  hour 19")),
                      say("eight L-functions, eight spokes", "ζ's zeros give the height, the other seven the hour")]),
        dict(text="Why it matters: the zeros are not points in E eight's stack. They are frequencies of its height, and every "
                  "coil reads the prime factors. The explicit formula is classical; what Claude and I add is where each zero sits "
                  "in E eight's funnel.",
             camera=C(at="68000,0,-16", fit="130,130>124,140", el="50>30", az="+", spin=3, rise=.04, span=12,
                      tall=dict(at=FT['spinfans'], fit=f"{FT['spinfansfit']}>{FT['wholefit']}", rise=.045)),
             actions=["exp:uncall", say("what is new", "a zero is a coil up E8's funnel, turning with the logarithm of every number")],
             pause=1.4),
    ])

# ---------------------------------------------------------------------------------------------------------------
# 4. what keeps zeros on the line: the lattices become the headers of their zeta functions' strips, the strips fold into
#    the chart of every mixture, and that line of mixtures grows into a plane
# ---------------------------------------------------------------------------------------------------------------

# boxes on the stage, 16:9 (_W) and 9:16 (_T, where the lattices stand in a triangle and the strips and chart are narrower)
LATT_W, LATT_T = (75858, 76142, -64, 56), (75905, 76095, -82, 118)      # the three lattices with their names
STRIPS, STRIPS_T = (75876, 76124, -68, 78), (75920, 76080, -75, 72)     # the four strips, the lattices on their heads
LAT1_W, LAT1_T = (75980, 76020, -13, 13), (75932, 75972, -51, -25)      # close on the second family: the rings of 2 and 3
LAT0_W, LAT0_T = (75884, 75920, -14, 14), (75982, 76018, 48, 76)        # close on the first family: 2, 3 and 6
TABLE_W, TABLE_T = (76020, 76195, -22, 38), (75930, 76080, -22, 52)     # the counts at 2, 3 and 6
WAVES_W, WAVES_T = (75835, 76062, -46, 52), (75885, 76040, -45, 50)     # the two waves and the ratio, on the chart's t axis
CHART_W, CHART_T = (75830, 76120, -48, 52), (75885, 76095, -58, 75)     # the chart of every mixture, with its histogram
PLANE = (76250, 76410, -75, 62)                                         # −47's plane of mixtures, from above
TERRAIN = C(at="76330,-6,-12", fit="220,124", el=38, az=-90, spin=2, rise=RISE, tall=dict(at="76330,-6,-8", fit="186.3,331.2"))
XS4 = dict(
    id="xs4", title="What keeps zeros on the line", subtitle="discovery 4 of 5:  symmetry is not enough",
    setup=["exp:stage", "exp:backdrop:off", "exp:t5lattice3", "exp:t5families:76000,-8,0", "exp:t5mix:76000,0,0", "exp:t5plane:76330,-6,0",
           "exp:t5terrain:76330,-6,0", tw("lf.build", 0, 0), tw("lf.h", 0, 0), tw("lf.slice", -10, 0),
           f"exp:lacounts:{alt('76040,28,0', '75945,40,0')}", "exp:lwaves:76000,0,0",
           "exp:hidechild:LA lattices:ring", "exp:hidechild:LA lattices:lit", "exp:hide:LC zeros", "exp:hide:LE mixtures",
           "exp:hide:LF landscape"],
    lines=[
        dict(text="Discovery four. Discriminant minus twenty-three has three families of quadratic forms, and each family is a "
                  "lattice of points. A number lies on a family when some point of its lattice sits at that number's distance "
                  "squared from the centre.",
             camera=TOP(LATT_W, tall=[LATT_T]),
             actions=[say("three families, three lattices", "x² + xy + 6y²  ·  2x² + xy + 3y²  ·  2x² − xy + 3y²"), "exp:snap",
                      at("lies", "exp:showchild:LA lattices:ring 0 6"), at("lies", "exp:showchild:LA lattices:lit 0 6"),
                      at("distance", call("lat", "0,0,1", 0, 0, "(0, 1):  0 + 0 + 6 = 6", col=RED))]),
        dict(text="Fifty-nine, a prime, lies on the first family only. Thirteen, a prime, lies on the other two. But {6 = 2·3|six, two times "
                  "three,} lies on all three: a composite shows up on more than one family.",
             camera=TOP(LATT_W, tall=[LATT_T]),
             actions=["exp:uncall", at("fiftynine", "exp:showchild:LA lattices:ring 0 59"), at("fiftynine", "exp:showchild:LA lattices:lit 0 59"),
                      at("fiftynine", call("lat", "0,5,2", 1, 1, "59:  4 points", col=WHITE)),
                      at("thirteen", "exp:showchild:LA lattices:ring 1 13"), at("thirteen", "exp:showchild:LA lattices:lit 1 13"),
                      at("thirteen", "exp:showchild:LA lattices:ring 2 13"), at("thirteen", "exp:showchild:LA lattices:lit 2 13"),
                      at("thirteen", call("lat", "1,2,1", 1, 1, "13", col=GREEN)),
                      at("six", "exp:showchild:LA lattices:ring 1 6"), at("six", "exp:showchild:LA lattices:lit 1 6"),
                      at("six", "exp:showchild:LA lattices:ring 2 6"), at("six", "exp:showchild:LA lattices:lit 2 6"),
                      at("six", call("lat", "2,-1,1", 1, -1, "6 = 2·3:  on all three", col=RED))]),
        # count the points: the second family at 2 and 3
        dict(text="Now count the points on each ring. On the second family, two has two points, and three has two points.",
             camera=TOP(LAT1_W, tall=[LAT1_T]),
             actions=["exp:uncall", "exp:hidechild:LA lattices:ring", "exp:hidechild:LA lattices:lit", "exp:hidechild:LA lattices:row",
                      at("two#2", "exp:showchild:LA lattices:ring 1 2"), at("two#2", "exp:showchild:LA lattices:lit 1 2"),
                      at("two#2", call("lat", "1,1,0", 0, 0, "2:  (±1, 0)", col="ff9e33")),
                      at("three", "exp:showchild:LA lattices:ring 1 3"), at("three", "exp:showchild:LA lattices:lit 1 3"),
                      at("three", call("lat", "1,0,1", 0, 0, "3:  (0, ±1)", col="c78cff")),
                      say("count the points on each ring", "2x² + xy + 3y²:  2 has 2 points,  3 has 2 points")]),
        # the first family at 2, 3 and 6
        dict(text="On the first family, two and three have no points at all. But {6 = 2·3|six, two times three,} has four.",
             camera=TOP(LAT0_W, tall=[LAT0_T]),
             actions=["exp:uncall", at("first", "exp:showchild:LA lattices:ring 0 2"), at("first", "exp:showchild:LA lattices:ring 0 3"),
                      at("no", call("lat", "0,1,0", 1, 1, "2, 3:  no points", col=WHITE)),
                      at("six", "exp:showchild:LA lattices:ring 0 6"), at("six", "exp:showchild:LA lattices:lit 0 6"),
                      at("four", call("lat", "0,1,-1", 0, 0, "6:  4 points", col=RED)),
                      say("count the points on each ring", "x² + xy + 6y²:  2 has none,  3 has none,  6 = 2·3 has 4")]),
        # the table: the families' counts are not built from the primes; the class group's combination is
        dict(text="So the first family's counts cannot be built from the prime factors: {0 × 0 ≠ 4.|zero times zero is not four.} But take "
                  "the first family's count minus the second's, and halve it. {2 → −1,|Two gives minus one,} {3 → −1,|three gives minus one,} and "
                  "{6 → +1 = (−1) × (−1).|six gives plus one, which is minus one times minus one.} Building every count from its prime factors is "
                  "exactly what an Euler product does: zeta's own product over the primes.",
             camera=TOP(TABLE_W, tall=[TABLE_T]),
             actions=["exp:uncall", "exp:twgroup:LA lattices/family 2:0,0,0:.01:1",
                      f"exp:twgroup:LA lattices/family 0:0,0,0:{alt(1, .01)}:1", f"exp:twgroup:LA lattices/family 1:0,0,0:{alt(1, .01)}:1",
                      "exp:showchild:LA counts:header",
                      "exp:showchild:LA counts:row 2", "exp:showchild:LA counts:row 3", "exp:showchild:LA counts:row 6",
                      at("zero#1", "exp:showchild:LA counts:check 0"), at("take", "exp:pulse:LA counts:1.5"),
                      at("six", "exp:showchild:LA counts:check 1"),
                      say("counts built from the prime factors", "(first − second)/2:  6 → +1 = (−1) × (−1):  an Euler product")]),
        dict(text="Each family has its own zeta function, counting its lattice points by distance, with Riemann's symmetry. The "
                  "lattices move up to head their strips of zeros. Yet those zeros leave the critical line; one sits at real part "
                  "one point zero zero seven, where no function with an Euler product can vanish.",
             camera=TOP(LATT_W, STRIPS, span=4, tall=[LATT_T, STRIPS_T]),
             actions=["exp:uncall", "exp:hide:LA counts", "exp:twgroup:LA lattices/family 2:0,0,0:1:0",
                      "exp:twgroup:LA lattices/family 0:0,0,0:1:0", "exp:twgroup:LA lattices/family 1:0,0,0:1:0",
                      at("lattices", "exp:hidechild:LA lattices:name"), at("lattices", "exp:hidechild:LA lattices:sub"),
                      at("lattices", f"exp:twgroup:LA lattices/family 0:{alt('2,66,0', '-57,-4,0')}:{alt(.3, .2)}:{{lattices~strips}}"),
                      at("lattices", f"exp:twgroup:LA lattices/family 1:{alt('-45,66,0', '29,96,0')}:{alt(.3, .2)}:{{lattices~strips}}"),
                      at("lattices", f"exp:twgroup:LA lattices/family 2:{alt('-117,66,0', '-67,96,0')}:{alt(.3, .2)}:{{lattices~strips}}"),
                      at("lattices", "exp:show:LC zeros"), at("lattices", "exp:hidechild:LC zeros:tick"),
                      at("lattices", "exp:hidechild:LC zeros:section"), at("lattices", "exp:drawon:LC zeros/line:2"),
                      say("each family's own zeta function", "red: zeros off the critical line Re s = ½"),
                      at("leave", call("at", alt("75916.6,-30.5,-0.4", "75950.5,-30.5,-0.4"), -1, 1, "0.953 + 16.29i", col=RED)),
                      at("one#2", call("at", alt("75982.1,-18.6,-0.4", "75989.5,-18.6,-0.4"), 1, 1, "1.007 + 22.57i", col=RED))]),
        dict(text="The two combinations of the families with Euler products keep every zero on the line: forty-four and forty-two "
                  "zeros, all gold.",
             camera=TOP(STRIPS, tall=[STRIPS_T]),
             actions=["exp:uncall", say("the class group's L-functions", "(Z<sub>1</sub> + 2Z<sub>2</sub>)/2 and (Z<sub>1</sub> − Z<sub>2</sub>)/2:  every zero on the line"),
                      at("fortyfour", call("at", alt("76032,-65.7,-0.4", "76019,-67.5,-0.4"), -1, -1, "44 on the line", col=GOLD)),
                      at("fortytwo", call("at", alt("76096,-65.7,-0.4", "76057,-67.5,-0.4"), 1, -1, "42 on the line", col=GOLD))]),
        # where the chart's curves come from: the two families' waves on the critical line
        dict(text="Now the curves of the chart, built step by step. On the critical line each family's completed zeta function is "
                  "a real wave: gold for the first family, blue for the second. Their zeros are where the waves cross the axis.",
             camera=TOP(WAVES_W, tall=[WAVES_T]),
             actions=["exp:uncall", "exp:mute:LC zeros,LA lattices:2.5:1.6", "exp:showchild:LW waves:t axis",
                      at("curves", "exp:showchild:LW waves:first"), at("curves", "exp:drawon:LW waves/first:{curves~gold}"),
                      at("gold", "exp:showchild:LW waves:name first"), at("blue", "exp:showchild:LW waves:second"),
                      at("blue", "exp:drawon:LW waves/second:2"), at("blue", "exp:showchild:LW waves:name second"),
                      at("zeros", "exp:showchild:LW waves:zeros"), at("zeros", "exp:showchild:LW waves:t label"),
                      at("zeros+0.2", "exp:hide:LC zeros"), at("zeros+0.2", "exp:hide:LA lattices"),
                      say("each family on the critical line", "Λ(½ + it) is real:  gold x² + xy + 6y²,  blue 2x² ± xy + 3y²")]),
        dict(text="Mix them: the first plus lambda times the second. That mixture is zero where the first equals minus lambda "
                  "times the second, that is, where the ratio, minus the first over the second, equals lambda. Sweep along t and "
                  "plot that ratio: wherever the blue wave crosses zero it divides by zero and shoots off to infinity, so the "
                  "curve breaks into these branches.",
             camera=TOP(WAVES_W, tall=[WAVES_T]),
             actions=[at("sweep", "exp:lratio:{sweep~end}"),
                      say("Λ<sub>1</sub> + λΛ<sub>2</sub> = 0  ⟺  λ = −Λ<sub>1</sub>/Λ<sub>2</sub>", "the ratio of the two waves, with a pole at every zero of the blue one")]),
        dict(text="A level line at height lambda crosses this curve exactly at that mixture's zeros on the line. At zero it meets "
                  "the gold dots, the first family's own zeros. {At λ = 2 and λ = −1|At two and at minus one} it meets the two L-functions with Euler "
                  "products, and every one of their zeros is there.",
             camera=TOP(CHART_W, tall=[CHART_T]),
             actions=["exp:show:LE mixtures", "exp:hidechild:LE mixtures:R", "exp:hidechild:LE mixtures:height",
                      "exp:hidechild:LE mixtures:crossings", "exp:mute:LW waves:1.2:1.7",
                      at("zero#1", "exp:showchild:LE mixtures:height 0"), at("zero#1", "exp:showchild:LE mixtures:crossings 0"),
                      at("at#4", "exp:showchild:LE mixtures:height 2"), at("at#4", "exp:showchild:LE mixtures:crossings 2"),
                      at("at#4", "exp:showchild:LE mixtures:height -1"), at("at#4", "exp:showchild:LE mixtures:crossings -1"),
                      say("a level line λ:  the zeros of Λ<sub>1</sub> + λΛ<sub>2</sub> on the line", "λ = 0:  the first family  ·  λ = 2 and −1:  the L-functions")]),
        dict(text="Sweep lambda through every mixture. Each mixture has a set number of zeros up to height fifty; where the level "
                  "line crosses the curve fewer times, the rest have left the line. Only two small windows of mixtures keep every "
                  "zero on it, and both surround the L-functions.",
             camera=TOP(CHART_W, tall=[CHART_T]),
             actions=[at("sweep", "exp:mixsweep:-3:4:{sweep~only}:76000,0,0"),
                      say("no zero off the line:  two small windows", "λ from 1.99 to 2,  and from −1.02 to −0.88"),
                      at("windows", call("at", alt("76072,24,-0.4", "76046,34,-0.4"), 1, 1, "λ = 2", col=GOLD)),
                      at("surround", call("at", alt("76072,-11.4,-0.4", "76046,-16.2,-0.4"), 1, -1, "λ = −1", col=GOLD))]),
        # minus forty-seven: a plane of mixtures, assembled row by row
        dict(text="With five families, for minus forty-seven, there are two weights instead of one, and the mixtures fill a plane. "
                  "Build it row by row: each row is a sweep like the one we just made, every cell coloured by how many zeros leave "
                  "the line, gold where none do.",
             camera=TOP(CHART_W, PLANE, span=4, tall=[CHART_T, PLANE]),
             actions=["exp:uncall", "exp:hide:LW waves", at("five", "exp:clearlive"), at("fill", tw("lf.build", .06, 1)),
                      at("fill", "exp:mute:LE mixtures:1.5:2"), at("build", tw("lf.build", 1, "{build~gold}", "linear")),
                      at("build+1.5", "exp:hide:LE mixtures"),
                      say("five families:  a plane of mixtures", "Λ<sub>1</sub> + λ<sub>2</sub>Λ<sub>2</sub> + λ<sub>3</sub>Λ<sub>3</sub>:  every row a sweep")]),
        # the third dimension: raised by the zeros off the line, sliced
        dict(text="Now raise every mixture by its number of zeros off the line, and the plane becomes a landscape. The gold patches "
                  "are its valleys, at sea level. Slice through it, and each slice is a sweep like {−23's:|minus twenty-three's:} where it "
                  "dips to the floor, a window.",
             camera=C(at="76330,-6,0>76330,-6,-12", fit=f"{BOX(*PLANE)['fit']}>220,124", el="89.5>38", az=-90, spin=2,
                      rise=RISE, span=5, tall=dict(at="76330,-6,0>76330,-6,-8", fit=f"{BOX(*PLANE, tall=True)['fit']}>186.3,331.2")),
             actions=[at("raise", tw("lf.h", 1, "{raise~landscape}")), at("slice", tw("lf.slice", -3, 0)),
                      at("slice", tw("lf.slice", 3, "{slice~end}", "linear")),
                      say("the landscape of mixtures", "height:  the zeros off the line up to t = 50;  gold valleys:  none")]),
        dict(text="Three valleys surround the class group's three L-functions, and their counts, like {−23's,|minus twenty-three's,} are "
                  "built from their prime factors: Euler products. One more valley has no L-function in it.",
             camera=C(**TERRAIN | dict(az="+")),
             actions=[tw("lf.slice", -10, 0),
                      at("three#1", call("at", "76371.3,35.3,-0.4", 1, 1, "ζ(s) L(s, χ<sub>−47</sub>)", col=GOLD)),
                      at("three#1", call("at", "76342.8,-39.4,-0.4", 1, -1, "L<sub>1</sub>", col=GOLD)),
                      at("three#1", call("at", "76296.6,6.8,-0.4", -1, 1, "L<sub>2</sub>", col=GOLD)),
                      at("one", call("at", "76364.1,18.8,-0.4", 1, -1, "no L-function here", col=WHITE)),
                      say("the valleys", "round the three L-functions:  Euler products,  counts built from primes")]),
        dict(text="Why it matters: symmetry alone does not keep zeros on the line. Davenport and Heilbronn proved that in nineteen "
                  "thirty-six. As far as anyone has checked, the functions that keep them there are the ones whose counts are built "
                  "from their prime factors: the Euler products. What Claude and I add is seeing every mixture at once.",
             camera=C(**TERRAIN | dict(az="+", spin=3)),
             actions=["exp:uncall", say("what is new", "symmetry is not enough:  every mixture seen at once")], pause=1.4),
    ])

# ---------------------------------------------------------------------------------------------------------------
# 5. Chebyshev's bias is a fold: the race and its explanation in one frame; the race morphs into the fair race, the fold of
#    E8's clock gives way to the triangle of S3, and the race between the families takes the race's place
# ---------------------------------------------------------------------------------------------------------------

# 16:9 boxes, and 9:16 (_T): the race narrower and stacked over the fold, the mod-p circles one above another
RACE_TOP, RACE_TOP_T = (79855, 80145, 8, 64), (79915, 80090, 4, 76)          # the race counted to 10⁷
RACES, RACES_T = (79855, 80145, -62, 64), (79915, 80090, -82, 76)            # counted above, rebuilt from the zeros below
# (in a 9:16 frame the fold, the mod-p circles and the triangle sit right under the counted race, over the rebuilt one,
#  which is faded out by then)
RACE_FOLD, RACE_FOLD_T = (79840, 80335, -78, 64), (79915, 80090, -155, 76)   # the race and E8's clock of squares
FOLD, FOLD_T = (80195, 80325, -72, 56), (79925, 80075, -155, -15)
HOURCLOCK, HOURCLOCK_T = (80222, 80298, -44, 28), (79954, 80046, -119, -51)  # a small clock of 30 hours at the fold's centre
POWERS, POWERS_T = (80222, 80300, -42, 30), (79960, 80040, -120, -48)        # the powers of 7 on the fold's logarithmic clock
MOD5, MOD5_T = (80228, 80292, -36, 34), (79968, 80032, -106, -40)
MODS, MODS_T = (80185, 80372, -40, 38), (79962, 80038, -180, -6)             # 2, 5 and 59 dots
MOD25_T, MOD59_T = (79966, 80034, -100, 1), (79962, 80038, -180, -108)       # 9:16: 2 and 5 (with 2's title), then 59 alone
S3, S3_T = (80215, 80305, -30, 28), (79968, 80032, -107, -47)               # the triangle of roots, where the fold was
FAMILY_T = (79910, 80085, -148, 38)                                          # 9:16: the family race over the fold of S3
XS5 = dict(
    id="xs5", title="Chebyshev's bias is a fold", subtitle="discovery 5 of 5:  why two of E8's spokes run behind",
    setup=["exp:stage", "exp:backdrop:off", "exp:t5race", f"exp:t5fold:{alt('80330,0,0', '80070,-77,0')}",
           f"exp:s3roots:{alt('80260,-4,0', '80000,-81,0')}:0",
           f"exp:t5family:{alt('79914,9,0', '79914,-12,0')}:{alt('80260,-8,0', '80000,-85,0')}:{alt(60, 50)}:{alt(280, 150)}",
           "exp:twgroup:MC fold:0,0,0:.3:0", "exp:mute:MA rebuilt:0:1.9",
           f"exp:modp:2:{alt('80200,-4,0', '80000,-23,0')}:10", f"exp:modp:5:{alt('80260,-4,0', '80000,-73,0')}:{alt(24, 22)}",
           f"exp:modp:59:{alt('80332,-4,0', '80000,-146,0')}:28",
           "exp:hide:MB fair", "exp:hide:MB fold", "exp:hide:S3 identity", "exp:hide:S3 rotation", "exp:hide:S3 reflection",
           "exp:hide:S3 square", "exp:hide:MC fold", "exp:hide:MC race", "exp:hide:MC fair"],
    lines=[
        dict(text="Discovery five. Every prime above five lies on one of E eight's eight gold spokes. Race them up to ten million, "
                  "and two of the spokes, one and nineteen, run behind nearly all the way.",
             camera=TOP(RACE_TOP, tall=[RACE_TOP_T]),
             actions=[say("the prime race on E8's eight spokes", "the primes up to 10<sup>7</sup>, counted and rebuilt from the zeros"),
                      at("behind", call("race", "6.5,-3", .4, -1, "spokes 1 and 19:  about −3", col=RED)),
                      at("behind+0.4", call("race", "6.5,1", .4, 1, "the other six:  about +1", col=GOLD)), "exp:snap"]),
        dict(text="They are ahead of their share only zero point three and one point four percent of the way, and never in the "
                  "lead. Below, six thousand seven hundred and seventy-one zeros of E eight's L-functions rebuild every curve.",
             camera=TOP(RACE_TOP, RACES, span=4, tall=[RACE_TOP_T, RACES_T]),
             actions=["exp:uncall", say("spokes 1 and 19 run behind", "ahead of their share 0.3 % and 1.4 % of the way;  rebuilt from 6 771 zeros"),
                      at("below", "exp:unmute:MA rebuilt:1.2"), at("below", "exp:pulse:MA rebuilt:2.4")]),
        # why one and nineteen: they are the only square hours
        dict(text="Why one and nineteen? Square any gold hour, and see where it lands on E eight's clock. {1² = 1.|One stays at one.} {7² = 49,|Seven "
                  "squared is forty-nine,} hour nineteen. {11² = 121,|Eleven squared, a hundred and twenty-one, is} hour one; {13² = 169,|thirteen squared, "
                  "a hundred and sixty-nine,} hour nineteen. All eight gold hours, squared, land on one or nineteen: the only two "
                  "square hours on the clock.",
             camera=TOP(RACES, HOURCLOCK, span=3, tall=[RACES_T, HOURCLOCK_T]),
             actions=[f"exp:mute:MA counted,MA rebuilt:1.5:{alt(1.8, 2)}", at("square", f"exp:hide:{alt('-', 'MA rebuilt')}"),
                      at("square", "exp:hoursquares:{square~only}"),
                      say("squares on E8's clock", "1² = 1,  7² = 49 → 19,  11² = 121 → 1,  13² = 169 → 19, …:  only hours 1 and 19")]),
        # a prime's powers walk round the clock
        dict(text="Follow one prime's powers round the clock, the radius growing with the logarithm. {7:|Seven sits on} hour seven. {7² = 49:  hour 19.|Seven "
                  "squared, forty-nine, on nineteen.} {7³ = 343:  hour 13.|Seven cubed, three hundred and forty-three, on thirteen.} {7<sup>4</sup> = 2401:|Seven to the fourth, "
                  "two thousand four hundred and one,} back on hour one. Every even power lands on one or nineteen.",
             camera=TOP(HOURCLOCK, POWERS, span=2, tall=[HOURCLOCK_T, POWERS_T]),
             actions=["exp:hide:MB squares", "exp:show:MB fold", "exp:hidechild:MB fold:arcs", "exp:hidechild:MB fold:primes",
                      "exp:hidechild:MB fold:hour label",
                      at("seven#1", "exp:powerwalk:7:4:{seven#1~every}"),
                      say("the powers of 7", "7 → 49 → 343 → 2401:  hours 7, 19, 13, 1")]),
        dict(text="Now every prime at once: an arc from each prime to its square. The zeros count prime powers, not only primes, "
                  "and squaring folds every spoke onto one or nineteen.",
             camera=TOP(POWERS, FOLD, span=3, tall=[POWERS_T, FOLD_T]),
             actions=["exp:hide:MB powers 7", "exp:showchild:MB fold:hour label", at("every", "exp:showchild:MB fold:primes"),
                      at("arc", "exp:showchild:MB fold:arcs"),
                      at("arc", "exp:drawon:MB fold/arcs:{arc~end}"),
                      say("the reason:  a fold", "squaring sends spokes 1, 11, 19, 29 to 1, and 7, 13, 17, 23 to 19")]),
        dict(text="{7² = 49,|Seven squared is forty-nine,} on spoke nineteen. {11² = 121|Eleven squared, one hundred and twenty-one,} lands on {1.|one.} "
                  "{13² = 169,|Thirteen squared, one sixty-nine,} on {19.|nineteen.} {29² = 841,|Twenty-nine squared, eight forty-one,} on {1.|one.}",
             camera=TOP(FOLD, tall=[FOLD_T]),
             actions=[at("seven", call("fold", 49, 1, 1, "7² = 49:  spoke 19", tall=(.3, 1, .12))),
                      at("eleven", call("fold", 121, -1, 1, "11² = 121:  spoke 1", tall=(-.3, 1, .22))),
                      at("thirteen", call("fold", 169, 1, -1, "13² = 169:  spoke 19", tall=(.3, -1, .12))),
                      at("twentynine", call("fold", 841, -1, -1, "29² = 841:  spoke 1", tall=(-.3, -1, .22)))]),
        dict(text="Every square of a prime above five lands on spoke one or nineteen: all four hundred and forty-three of them up "
                  "to ten million. A count of primes leaves out half a prime for each. Put them back, and watch the race even out.",
             camera=TOP(FOLD, RACE_FOLD, span=4, tall=[FOLD_T, RACE_FOLD_T]),
             actions=["exp:uncall", "exp:unmute:MA counted:1", at("put", "exp:racemorph:{put~end}"), at("put", "exp:pulse:MB fold:2.4"),
                      say("put the squares back:  a fair race", "218 squares on spoke 1,  225 on spoke 19,  none elsewhere")]),
        # the second race: count the roots of x³ − x − 1 among the numbers mod p
        dict(text="The same fold turns up in a second race. For minus twenty-three, a prime's family is decided by how many roots "
                  "x cubed minus x minus one has among the numbers modulo that prime. Take five: five numbers, zero to four. Put "
                  "each one in. Only two gives a multiple of five, {8 − 2 − 1:|eight minus two minus one:} one root.",
             camera=TOP(RACE_FOLD, MOD5, span=4, hold=3, tall=[RACE_FOLD_T, MOD5_T]),
             actions=[at("same", "exp:mute:MA counted:1.5"), at("numbers", "exp:hide:MB fold"), at("numbers", "exp:modpshow:5"),
                      at("put", "exp:modptest:5:{put~one#3}"),
                      say("x³ − x − 1 modulo a prime", "5:  0, 1, 2, 3, 4  →  only x = 2 gives 8 − 2 − 1 = 5 ≡ 0")]),
        dict(text="Two has two numbers, zero and one, and neither is a root. Fifty-nine has fifty-nine, and exactly three of them are "
                  "roots: four, thirteen and forty-two.",
             camera=TOP(MODS, tall=[MOD25_T, MOD59_T], span=2, hold=alt(0, "{fiftynine#1-0.3}")),
             actions=[at("two#1", "exp:modptest:2:{two#1~fiftynine#1}"), at("fiftynine#1", "exp:modptest:59:{fiftynine#1~end}"),
                      at("fiftynine#1", f"exp:mute:{alt('-', 'S3 mod 2$,S3 mod 5$')}:1.2:1.8"),
                      say("count the roots", "2:  none   ·   5:  one   ·   59:  three (4, 13, 42)")]),
        # what the counts mean: how the prime moves the three roots
        dict(text="Those counts tell how the prime moves the three roots of x cubed minus x minus one. Three roots fixed: the "
                  "identity, nothing moves. No root fixed: a rotation, all three move round. One root fixed: a reflection, the "
                  "mirror: one root stays and the other two swap.",
             camera=TOP(MODS, S3, span=3, tall=[MOD59_T, S3_T]),
             actions=["exp:hide:S3 mod 2", "exp:hide:S3 mod 5", "exp:hide:S3 mod 59", "exp:show:S3 identity",
                      at("three#2", "exp:s3anim:identity:{three#2~no}"),
                      at("no", "exp:hide:S3 identity"), at("no", "exp:show:S3 rotation"), at("no", "exp:s3anim:rotation:{no~one#2}"),
                      at("one#2", "exp:hide:S3 rotation"), at("one#2", "exp:show:S3 reflection"), at("one#2", "exp:s3anim:reflection:{one#2~end}"),
                      say("how a prime moves the roots", "3 roots: identity  ·  none: rotation  ·  1 root: reflection")]),
        dict(text="And a reflection done twice is the identity: swap, then swap back. So the square of every prime with one root, "
                  "like {25 = 5²,|twenty-five, five squared,} acts like a prime with three roots, and lands in the principal family.",
             camera=TOP(S3, tall=[S3_T]),
             actions=[at("twice", "exp:hide:S3 reflection"), at("twice", "exp:show:S3 square"), at("twice", "exp:s3anim:square:{twice~so}"),
                      say("a reflection, twice:  the identity", "5 has one root;  25 = 5² lands in the principal family x² + xy + 6y²")]),
        dict(text="Half of all primes have one root, so their squares fall in the principal family, which runs behind its share "
                  "and is never in the lead.",
             camera=TOP(S3, RACE_FOLD, span=4, hold=1, tall=[S3_T, FAMILY_T]),
             actions=[at("half", "exp:hide:S3 square"), at("half", "exp:show:MC fold"), at("half", "exp:twgroup:MC fold:0,0,0:1:{half~end}"),
                      at("half", "exp:mute:MA counted,MA rebuilt:1:2"), at("squares+1", "exp:hide:MA counted"),
                      at("squares+1", "exp:hide:MA rebuilt"), at("squares", "exp:show:MC race"), at("squares", "exp:drawon:MC race/class:3"),
                      say("the squares of the reflections", "land in the principal family:  it runs behind")]),
        dict(text="Put those squares back too, and this race evens out as well. Why it matters: one fold explains both races. "
                  "Chebyshev noticed the bias in eighteen fifty-three; what Claude and I add is one picture for E eight's wheel and "
                  "for the families of minus twenty-three.",
             camera=TOP(RACE_FOLD, tall=[FAMILY_T]),
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
             camera=PC(at="11,-6.5,0", fit="44,26", rise=RISE, tall=dict(at="11,-6,0", fit="26.7,47.4")),
             actions=[say("the path of partial sums", "ζ(s) = 1 + 1/2<sup>s</sup> + 1/3<sup>s</sup> + …,  one arrow per term"),
                      at("refresher", tw("pw.n", 1, 1.5)), "exp:snap"]),
        dict(text="Take the height {t = 1.|t equal to one.} The first arrow is one unit long and points to the right. The second is one over "
                  "the square root of two long, and it turns away from the first by {log 2:|the logarithm of two:} {40°.|forty degrees.}",
             camera=PC(at="11,-6.5,0", fit="44,26", rise=RISE, tall=dict(at="11,-6,0", fit="26.7,47.4")),
             actions=[at("second", tw("pw.n", 2, 2.2))]),
        dict(text="The third is one over root three long, and turns a further {log(3/2), 23°.|log of three halves, twenty-three degrees.} Its "
                  "direction is the sum of the turns: {log 3 = 63°|log three, sixty-three degrees} from the first. Every direction is a logarithm.",
             camera=PC(at="11,-6.5,0", fit="44,26", rise=RISE, tall=dict(at="11,-6,0", fit="26.7,47.4")),
             actions=[at("third", tw("pw.n", 3, 2.2)), at("sum", "exp:pulse:PW path:1.6")]),
        dict(text="Four, five, six, seven, eight: shorter and shorter arrows, turning less and less, {17°, 13°, 10°, 9° and 8°,|seventeen degrees, then "
                  "thirteen, ten, nine and eight,} because the logarithm climbs more and more slowly.",
             camera=PC(at="11,-6.5,0>11.5,-17.5,0", fit="44,26>114.7,64.5", span=4, rise=RISE,
                       tall=dict(at="11,-6,0>11.5,-18,0", fit="26.7,47.4>41.6,74")),
             actions=[at("four", tw("pw.n", 4, .7)), at("five", tw("pw.n", 5, .7)), at("six", tw("pw.n", 6, .7)),
                      at("seven", tw("pw.n", 7, .7)), at("eight#1", tw("pw.n", 8, .7))]),
        dict(text="Now raise the height t. Every angle is t times a logarithm, so the path winds up tighter. The arrows go on "
                  "forever, but past a point the rest add up to a simple tail, drawn in orange, and the end of that tail is {ζ(½ + it).|zeta "
                  "of one half plus i t.}",
             camera=PC(at="11.5,-17.5,0>10,-4,0", fit="114.7,64.5>58,36", span=10, tall=dict(at="11.5,-18,0>10,-2,0", fit="41.6,74>28.7,51")),
             actions=[tw("pw.marks", 0, .8), at("raise", tw("pw.n", 1000, 2)), at("raise", tw("pw.t", 10, "{raise~tail}", "geo")),
                      at("tail", tw("pw.tail", 1, 1.6)),
                      say("raising t", "every angle is t × a logarithm;  the tail adds all the arrows beyond")]),
        dict(text="At t equal to fourteen point one three, the end lands exactly on zero. That is the first zero of zeta: all the "
                  "arrows, from one to infinity, cancel out.",
             camera=PC(at="6,0,0", fit="48,30", tall=dict(at="5,0,0", fit="20.5,36.4")),
             actions=[tw("pw.t", 14.134725, "{at~zero#1}"), at("zero#1", call("pathzeta", 0, 0, 0, "ζ(½ + 14.13i) = 0", col=GOLD)),
                      say("the first zero", "t = 14.1347:  ζ(½ + it) = 0")]),
        dict(text="Now let t run on, and stand the plane up on a t axis, so that it climbs as t grows. The end of the path leaves a "
                  "trail, the curve of zeta along the critical line. It touches the axis at every zero: twenty-one point zero "
                  "two, twenty-five point zero one, thirty point four two.",
             camera=PC(at="6,0,0", fit="48,30>96,64", el="89.5>30", az=-90, spin=3, span=8, tall=dict(at="5,0,0>0,0,17.6", fit="20.5,36.4>93.3,165.8")),
             actions=["exp:uncall", at("stand", tw("pw.rise", 1, 3)), at("stand", tw("pw.curve", 1, 2)),
                      at("run", tw("pw.t", 32, "{run~end}", "linear")),
                      say("the curve of ζ on the critical line", "the plane climbs the t axis;  the curve touches it at every zero")]),
        dict(text="Between the zeros, the curve swings out and comes back. Riemann's hypothesis says that every zero of this kind "
                  "lies on this line, {Re s = ½.|real part one half.} Trillions have been checked, and every one does, but no one has proved "
                  "that all of them must.",
             camera=PC(at="6,0,-10", fit="96,64>124,90", el="30>24", az="+", spin=3, span=12, tall=dict(at="0,0,17.6>0,0,27.5", fit="93.3,165.8>100.2,178.1")),
             actions=[tw("pw.t", 50, "{between~end}", "linear"),
                      say("Riemann's hypothesis", "every nontrivial zero of ζ has real part ½")]),
        dict(text="Higher up, the path needs more and more arrows before the tail takes over, and it begins to curl into knots. "
                  "Those knots are its vortices, and the next part follows them up the axis.",
             camera=PC(at="6,-2,0", fit="124,90>84,56", el="24>36", az="+", spin=3, span=10, tall=dict(at="0,0,27.5>0,0,10", fit="100.2,178.1>60,107")),
             actions=[tw("pw.t", 80, "{higher~end}", "linear"), tw("pw.curve", .2, 3), at("knots", tw("pw.eyes", 1, 1.5)),
                      say("the vortices begin", "knots in the path, ringed")], pause=1.4),
    ])

# ---------------------------------------------------------------------------------------------------------------
# the zeros inside E8, 2: how the vortices develop as the plane climbs
# ---------------------------------------------------------------------------------------------------------------

VLBOX = C(on="vlbox", at="0,0,0", fit="50,30", el=89.5, az=-90, spin=0, rise=RISE, follow=1)   # follows the vortices' line
XZ2 = dict(
    id="xz2", title="How the vortices develop", subtitle="the zeros inside E8, part 2:  the path of partial sums high up",
    setup=["exp:stage", "exp:backdrop:off", "exp:vortexprep", "exp:pwinit", tw("pw.t", 50, 0), tw("pw.n", 1000, 0), tw("pw.tail", 1, 0),
           tw("pw.rise", 1, 0), tw("pw.curve", 0, 0), tw("pw.readout", 1, 0)],
    lines=[
        dict(text="Climbing higher, the path needs about t over pi arrows before its tail takes over, and it curls into knots: "
                  "vortices. The rings mark their eyes, numbered from the far end of the path.",
             camera=PC(at="6,-2,0", fit="70,46", el=36, az=-90, spin=3, tall=dict(at="5,-1,0", fit="40,71")),
             actions=[tw("pw.t", 130, "line", "linear"), at("knots", tw("pw.eyes", 1, 1.5)),
                      say("vortices", "about t/2π of them at height t"), "exp:snap"]),
        dict(text="Each time t over two pi passes another whole number, a new vortex is born near the start of the path, and the "
                  "older ones move out along it.",
             camera=PC(at="6,-2,0", fit="70,46", el=36, az="+", spin=3, tall=dict(at="5,-1,0", fit="40,71")),
             actions=[tw("pw.t", 250, "line", "linear"), say("a vortex is born", "each time t/2π passes a whole number")]),
        dict(text="Between the vortices the path runs straight. Those straight runs are the spokes, where each arrow turns by a "
                  "whole number of full turns from the last. They sit near {n ≈ t/2πk:|n equal to t over two pi k:} at the hundred and "
                  "fifty-ninth zero, fifty-three, twenty-seven and eighteen.",
             camera=PC(at="6,-4,0", fit="70,46>60,38", el="36>70", az="+", spin=1, span=8, tall=dict(at="5,-1,0>5,-5,0", fit="40,71>44,78")),
             actions=[tw("pw.t", 333.645, "{between~fiftythree}"),
                      at("fiftythree", call("path", 53, 0, 0, "spoke 1:  n ≈ 53")), at("twentyseven", call("path", 27, 0, 0, "spoke 2:  n ≈ 27")),
                      at("eighteen", call("path", 18, 0, 0, "spoke 3:  n ≈ 18")),
                      say("spoke k at n ≈ t / 2πk", "where each arrow turns a whole number of turns from the last")]),
        dict(text="Each vortex has an eye, and each eye is a mirror image: the path's own {k-th|kay-th} vertex, reflected through zeta's "
                  "phase. The crosses mark those mirror points, and they land inside the rings.",
             camera=PC(at="4,-4,0", fit="52,32", el=78, az="+", spin=1, tall=dict(at="4,-5,0", fit="40,71")),
             actions=["exp:uncall", at("crosses", tw("pw.mirrors", 1, 1.2)),
                      say("every eye is a mirror", "eye of vortex k = the path's k-th vertex, reflected through ζ's phase")]),
        # the vortices on the number line: the path unwound, the mirror n ↦ t/2πn, and the same line wound into the Sacks spiral
        dict(text="Now unwind the path onto the number line. Every arrow drops onto its own counting number, in the colour of "
                  "its vortex. Vortex one lands between twenty-seven and fifty-three, vortex two between eighteen and "
                  "twenty-seven, vortex three between thirteen and eighteen. Each stretch is shorter than the last, and below "
                  "seven the vortices are smaller than one arrow: a tangle.",
             camera=VLBOX,
             actions=["exp:vlinit", "exp:mute:PW path:1.2:2", tw("pw.mirrors", 0, 1), tw("pw.eyes", 0, 1), tw("pw.axis", 0, 1),
                      at("unwind", tw("vl.unwind", 1, "{unwind~colour}")), at("colour", tw("vl.marks", 1, 1.5)),
                      say("the vortices on the number line", "the 159th zero:  vortex k on the arrows from t/2π(k + 1) to t/2πk")]),
        dict(text="Now the mirror that puts each eye in place: arrow n goes to {t/2πn,|t over two pi n,} here {53/n.|fifty-three over n.} On the "
                  "mirror's side, vortex one fills the stretch from one to two, vortex two from two to three: lined up on one "
                  "axis, the vortices are evenly spaced, one for every counting number. The two sides meet at {√53 = 7.3.|the square root "
                  "of fifty-three, seven point three.}",
             camera=VLBOX,
             actions=[at("goes", tw("vl.fold", 1, "{goes~lined}")),
                      say("the mirror:  n → t/2πn", "vortex k fills the stretch from k to k + 1:  one vortex for every counting number"),
                      at("meet", say("the fold:  √(t/2π) = 7.3", "the arrows before it make the tangle;  the arrows after it make the vortices"))]),
        dict(text="Slide the mirror's side onto the line, and the colours read the same both ways from seven point three: one "
                  "counting number for each vortex on one side, its long stretch of arrows on the other. Wind the line into the "
                  "Sacks spiral, and the fold becomes a circle: inside it the first seven counting numbers, outside it the "
                  "vortices, bands of the spiral that widen toward the rim.",
             camera=VLBOX,
             actions=[at("slide", tw("vl.merge", 1, 2.5)), at("wind", tw("vl.curl", 1, "{wind~inside}")),
                      say("the vortices on the Sacks spiral", "the fold is a circle:  counting numbers inside, vortices outside, the same colours")]),
        dict(text="Now let the plane climb on, and at every zero keep each eye where it was. The eyes build a tower; vortex one, in "
                  "cyan, always sits on a circle of radius one.",
             camera=PC(at="0,0,10", fit="60,40>90,70", el="70>26", az="+", spin=6, span=10, tall=dict(fit="45,80>70,124")),
             actions=[tw("vl.vis", 0, 1), "exp:unmute:PW path:1.5", tw("pw.eyes", 1, 1), tw("pw.axis", 1, 1.5),
                      tw("pw.mirrors", 0, 1), tw("pw.trails", 1, 0), tw("pw.t", 420, "{climb~end}", "linear"),
                      say("the eyes through the zeros", "kept at every zero:  vortex 1 on a circle of radius 1")]),
        dict(text="Look straight down the axis. The eyes of ten thousand zeros, stacked: the unit circle of vortex one, and rings "
                  "for all the others.",
             camera=PC(at="0,0,0", fit="90,70>56,40", el="26>89.5", az="+", spin=4, span=6, tall=dict(fit="70,124>49.2,87.5")),
             actions=[at("ten", tw("pw.alleyes", 1, 2)), at("ten", tw("pw.eyes", 0, 1)), at("ten", tw("pw.trails", .25, 2)),
                      say("all 10 000 zeros, down the axis", "the vortices belong to the counting numbers")]),
        # the faint circle: vortex two's eye can never be farther than 1 + 1/√2 from the centre
        dict(text="Look closer: the eyes thin out beyond a faint circle at radius one point seven one, and a brighter ring glows at "
                  "zero point two nine. At a zero, vortex two's eye is as far from the centre as the path's second vertex: one, plus "
                  "the arrow of two, {1/√2|one over root two} long.",
             camera=PC(at="0,0,0", fit="64,46>128,72", el=89.5, az=-90, spin=0, span=3, tall=dict(fit="49.2,87.5")),
             actions=[at("thin", "exp:pulse:PW path:1.2"), at("one#3", "exp:eyelimits:14"),
                      say("a faint circle at r = 1.71", "vortex 2's eye:  |1 + 2<sup>−½ − iγ</sup>|,  one plus an arrow 1/√2 long")]),
        dict(text="That arrow can point any way, so the eye stays between {1 − 1/√2|one minus one over root two} and {1 + 1/√2,|one plus one over root "
                  "two,} and it lingers at those two extremes, so both circles glow. Beyond one point seven one only vortex three and "
                  "higher can reach, and the eyes thin out.",
             camera=PC(at="0,0,0", fit="128,72", el=89.5, az=-90, spin=0, tall=dict(fit="49.2,87.5")),
             actions=[say("between 1 − 1/√2 = 0.29 and 1 + 1/√2 = 1.71", "beyond 1.71 only vortices 3 and up:  the eyes thin out 2.7-fold")]),
        dict(text="So the vortices develop as t climbs. New ones are born at the start, the spokes slide outward, and every vortex "
                  "belongs to a counting number: a reflection of the path's own beginning.",
             camera=PC(at="0,0,0", fit="128,72>64,46", el="89.5>70", az=-90, spin=3, span=12, tall=dict(fit="49.2,87.5>44,78")),
             actions=["exp:clearchild:PW limits", say("how the vortices develop", "one per counting number, each a mirror of the path's start")], pause=1.4),
    ])

# ---------------------------------------------------------------------------------------------------------------
# the zeros inside E8, 3: from a zero's height to the heights of the counting numbers, step by step
# ---------------------------------------------------------------------------------------------------------------

XZ3 = dict(
    id="xz3", title="From a zero to E8's heights", subtitle="the zeros inside E8, part 3:  the turn of n is γ times log n",
    setup=FUNNEL_SETUP,
    lines=[
        dict(text="This is E eight's factor funnel. Every counting number sits at its hour on the wheel of thirty, with a column "
                  "as tall as its logarithm. {7,|Seven,} at hour seven: {log 7 = 1.95.|one point nine five.} {30 = 2 · 3 · 5,|Thirty, two times three times five,} at "
                  "{hour 0:  log 30 = 3.40.|hour zero: three point four.}",
             camera=C(**FUNNEL, az=-100),
             actions=[say("E8's factor funnel", "n at its hour n mod 30, its column as tall as log n"),
                      at("seven#2", call("fun", 7, 0, 0, "log 7 = 1.95")), at("thirty#2", call("fun", 30, 0, 0, "30 = 2·3·5:  log 30 = 3.40")),
                      "exp:snap"]),
        dict(text="Grey it out, and light the powers of two. Two is one piece, log two tall, on the arm of hour two. {4 is 2 "
                  "pieces, 8 is 3, 16 is 4:|Four is two "
                  "pieces, eight is three, sixteen is four:} each column is its prime factors' logarithms, stacked.",
             camera=C(at="68000,0,-16>68002,-2,-9", fit="124,140>72,44", el="26>22", az="+", spin=1, rise=RISE, span=5,
                      tall=dict(at=f"{FT['whole']}>68002,-2,-9", fit=f"{FT['wholefit']}>36,64")),
             actions=["exp:uncall", at("grey", "exp:mute:GB factor columns:1.2"), at("powers", "exp:fwhl:2:logs"), at("four", "exp:fwhl:4"),
                      at("eight", "exp:fwhl:8"), at("sixteen", "exp:fwhl:16:logs"),
                      say("a column is its prime factors, stacked", "log 16 = log 2 + log 2 + log 2 + log 2")]),
        dict(text="Now the path of partial sums at the first zero, t equal to fourteen point one three, on the funnel's floor. "
                  "Arrow one, arrow two, arrow three, and on, each one over root n long, each turned by {t · log n.|t times log n.}",
             camera=C(at="68002,-2,-9>68006.8,0.2,0", fit="72,44>40,22.5", el="22>84", az="+", az_to=-90, rise=RISE, span=4,
                      tall=dict(at=f"68002,-2,-9>{FT['floor']}", fit=f"36,64>{FT['floorfit']}")),
             actions=["exp:fwunhl", at("floor", "exp:mute:GB factor columns:1.5:1.85"), at("path", tw("fw.n", 1, 1.2)),
                      at("arrow#2", tw("fw.n", 2, .8)), at("arrow#3", tw("fw.n", 3, .8)),
                      at("on#2", tw("fw.n", 15, "{on#2~end}", "linear")),
                      say("the path at the first zero", "arrow n:  1/√n long, turned 14.13 · log n")]),
        dict(text="Its tail closes it on the axis: that is what a zero is. Now lift every arrow to the height of its own number, "
                  "keeping its direction and its length: arrow two to log two, arrow three to log three, arrow four to log four, "
                  "twice as high as two. The arrows shrink as they climb, like a horn.",
             camera=C(at="68006.8,0.2,0>68002,0,-8", fit="40,22.5>56,40", el="84>22", az="+", spin=1.5, rise=.04, span=6,
                      tall=dict(at=f"{FT['floor']}>{FT['horn']}", fit=f"{FT['floorfit']}>{FT['hornfit']}", rise=.045)),
             actions=[tw("fw.stretch", 0, 0), tw("fw.tail", 1, 1.2), at("lift", tw("fw.tail", 0, 1)), at("lift", tw("fw.axis", 1, 1)),
                      at("lift", tw("fw.path", 0, "{lift~end}")), at("lift", tw("fw.lift", 1, "{lift~shrink}")),
                      at("two#2", "exp:fwhl:2"), at("three#2", "exp:fwhl:3"), at("four#2", "exp:fwhl:4"), at("horn", tw("fw.coil", .5, 2)),
                      say("lift arrow n to the height log n", "keeping its direction −14.13 · log n and its length 1/√n")]),
        dict(text="Stretch each arrow by the square root of its number, its distance from the centre on the Sacks spiral and on "
                  "this floor. {1/√n × √n = 1:|One over root n times root n is one:} every arrow becomes one unit long, and only its direction is "
                  "left. Through their tips runs a coil that turns by gamma for every unit of height: the zero, wound up E eight's "
                  "funnel.",
             camera=C(at="68002,0,-8>68000,0,-14", fit="56,40>76,74", el=22, az="+", az_to=-90, rise=.04, span=6,
                      tall=dict(at=f"{FT['horn']}>{FT['coil']}", fit=f"{FT['hornfit']}>{FT['coilfit']}", rise=.045)),
             actions=["exp:fwunhl", at("stretch", tw("fw.stretch", 1, "{stretch~left}")), at("stretch", "exp:fwhl:2"),
                      at("through", "exp:fwunhl"), at("through", tw("fw.coil", 0, 0)), at("through", tw("fw.coil", 1, "{through~end}")),
                      say("stretch arrow n by √n", "1/√n × √n = 1:  every arrow one unit long, only its direction left"),
                      at("through", say("the coil:  the hands at every height", "turning 14.13 per unit of height, one turn every 0.44"))]),
        dict(text="The coil multiplies by adding heights. {4 = 2 · 2,|Four is two times two,} so its hand has turned twice the turn of two. "
                  "{6 = 2 · 3:|Six is two times three:} the turn of two plus the turn of three.",
             camera=C(at="68000,0,-14>68004,-1,-9.5", fit="76,74>72,40.5", el="22>64", az=-90, spin=0, rise=RISE, span=5,
                      tall=dict(at=f"{FT['coilaz']}>{FT['six']}", fit=f"{FT['coilazfit']}>{FT['sixfit']}")),
             actions=[tw("fw.coil", .32, 2), tw("fw.dim", 1, 2), at("four", "exp:fwhl:4:logs"), at("twice", "exp:fwangles:4"), at("six", "exp:fwunhl"), at("six", "exp:clearchild:FW angles"),
                      at("six+0.1", "exp:fwhl:6:logs"), at("plus", "exp:fwangles:6"),
                      say("multiplying is adding heights", "γ·log 4 = 2 γ·log 2   ·   γ·log 6 = γ·log 2 + γ·log 3")]),
        dict(text="Every zero winds its own coil. Twenty-one point zero two turns each hand further, the higher ones more; "
                  "twenty-five point zero one, further still.",
             camera=C(at="68004,-1,-9.5>68000,0,-14", fit="72,40.5>76,74", el="64>24", az=-90, spin=0, rise=.04, span=5,
                      tall=dict(at=f"{FT['six']}>{FT['coilaz']}", fit=f"{FT['sixfit']}>{FT['coilazfit']}", rise=.045)),
             actions=["exp:fwunhl", "exp:clearchild:FW angles", tw("fw.dim", 0, 1.5), tw("fw.coil", 1, 2.5), at("twentyone", tw("fw.gamma", G2, 2.2)),
                      at("twentyfive", tw("fw.gamma", G3, 2.2)),
                      say("other zeros, other coils", "γ = 21.02,  25.01")]),
        dict(text="At a height that is not a zero, like eighteen, the hands turn just the same, but drop them back onto the floor "
                  "and the path does not close: its tail ends away from the axis.",
             camera=C(at="68000,0,-14>68024.5,-3,0", fit="76,74>51.2,28.8", el="24>84", az=-90, spin=0, rise=RISE, span=6,
                      tall=dict(at=f"{FT['coilaz']}>68023.5,-3,0", fit=f"{FT['coilazfit']}>52.3,93")),
             actions=[tw("fw.coil", 0, 1.5), at("eighteen", tw("fw.gamma", 18, 2)), at("drop", tw("fw.lift", 0, "{drop~path}")),
                      at("drop", tw("fw.path", 1, 1.5)), at("tail", tw("fw.tail", 1, 1.5)),
                      say("t = 18:  not a zero", "the arrows dropped back into the path:  it ends away from the axis")]),
        dict(text="That is the bridge between a zero and E eight's layout: the zero's height, times the height of n's column, is "
                  "the turn of n's arrow. Each prime factor adds its own turn.",
             camera=C(at="68000,0,-14", fit="66,74>124,140", el="60>26", az="+", spin=3, rise=.04, span=12,
                      tall=dict(at=FT['coil'], fit=f"{FT['coilfit']}>{FT['wholefit']}", rise=.045)),
             actions=["exp:mute:GB factor columns:2:1", tw("fw.gamma", G1, 2), tw("fw.tail", 0, 1), tw("fw.path", 0, 2), tw("fw.lift", 1, 3), tw("fw.coil", 1, 4),
                      say("the bridge", "the turn of n at a zero = γ × log n = γ × (the sum of log p over its prime factors)")], pause=1.4),
    ])

# ---------------------------------------------------------------------------------------------------------------
# the zeros inside E8, 4: the coils add up, height by height, into the comb; the comb spins into rings; E8's eight
#    L-functions turn it into eight fans
# ---------------------------------------------------------------------------------------------------------------

XZ4 = dict(
    id="xz4", title="Lamps and spokes", subtitle="the zeros inside E8, part 4:  all the coils together",
    setup=["exp:stage", "exp:backdrop:off", "exp:zfunnel", "exp:fwinit", tw("fw.n", 0, 0), tw("fw.dim", 0, 0), tw("fw.stretch", 1, 0),
           "exp:mute:GB factor columns:0"],
    lines=[
        dict(text="Every zero winds its own coil up the funnel. Here are the first {three:|three,} fourteen point one three, twenty-one point "
                  "zero two and twenty-five point zero one, over the greyed-out columns.",
             camera=C(at="68000,0,-18", fit="62,92", el=12, az=-96, spin=3, rise=.04, tall=dict(fit="40.8,72.5", rise=.045)),
             actions=[say("every zero, a coil", "γ = 14.13,  21.02,  25.01"), "exp:reveal:JM coils:{fourteen},{twentyone},{twentyfive}", "exp:snap"]),
        dict(text="Take all ten thousand five hundred zeros, and look at one height: the top of seven's column. Each coil points "
                  "somewhere there; on average they lean towards minus one, by minus point one one five, exactly as Landau's "
                  "formula predicts for a prime.",
             camera=C(at="68000,0,-12", fit="74,74", el=40, az=-100, spin=2, rise=.04, tall=dict(fit="45,80", rise=.045)),
             actions=["exp:show:JM coils", at("sevens", "exp:fwhl:7"), at("each", "exp:reveal:JM disc 7:0,0.8,1.8,3.4"),
                      say("at the height of 7, a prime", "10 500 coils lean to −1:  mean −0.115,  Landau −0.115")]),
        dict(text="At the top of six's column, {6 = 2 · 3,|two times three,} the coils point every way, and their mean is zero. The zeros lean "
                  "only at the powers of primes.",
             camera=C(at="68000,0,-11", fit="74,74", el=40, az=-80, spin=2, rise=.04, tall=dict(fit="45,80", rise=.045)),
             actions=["exp:fwunhl", "exp:hide:JM disc 7", at("sixs", "exp:fwhl:6"), at("coils", "exp:reveal:JM disc 6:0,0.8,1.8,3.4"),
                      say("at the height of 6 = 2·3", "the coils cancel:  mean 0.000")]),
        dict(text="Now do it at every height. A ring climbs the axis, and at each height all the coils add up to one hand, drawn "
                  "out to the side. Where they agree, at a prime power, it shoots out to the top of that prime's column: {7, 8, "
                  "9.|seven, eight, "
                  "nine.} At six, ten and twelve it stays at the axis.",
             camera=C(at="68000,0,-11>68004,0,-10", fit="74,74>56,31.5", el="40>10", az=-90, spin=0, rise=RISE, span=4,
                      tall=dict(at=f"68000,0,-11>{FT['scan']}", fit=f"45,80>{FT['scanfit']}")),
             actions=["exp:fwunhl", "exp:hide:JM coils", "exp:hide:JM disc 6",
                      at("climbs", tw("fw.scan", 1.5, 0)), at("climbs", tw("fw.scan", 1.75, "{climbs~seven}")),
                      at("seven", tw("fw.scan", 2.55, "{seven~stays}", "linear")),
                      say("all the coils, height by height", "a tooth of log p at every prime power"),
                      at("seven", call("tooth", 7, 1, -.7, "7", col=GOLD)), at("eight", call("tooth", 8, 1, .05, "8 = 2³", col=GOLD)),
                      at("nine", call("tooth", 9, 1, .8, "9 = 3²", col=GOLD)), at("six", call("tooth", 6, -1, -.7, "6:  dark", col=RED)),
                      at("ten", call("tooth", 10, -1, .1, "10:  dark", col=RED)), at("twelve", call("tooth", 12, -1, .8, "12:  dark", col=RED))]),
        dict(text="Climbing on, it lights every prime power up to four hundred: a comb, the master wave from the first discovery, "
                  "stood up along the logarithm.",
             camera=C(at="68004,0,-10>68008,0,-19", fit="56,31.5>130,73", el=8, az=-90, spin=0, rise=RISE, span=6,
                      tall=dict(at=f"{FT['scan']}>{FT['comb']}", fit=f"{FT['scanfit']}>{FT['combfit']}")),
             actions=["exp:uncall", at("climbing", tw("fw.scan", 6.02, "{climbing~comb}")), at("comb", tw("fw.scan", 100, 0)),
                      say("ζ's comb", "a tooth at every prime power")]),
        dict(text="But the comb is the same in every direction. Spin it round the axis, slowly, and watch each tooth sweep out a "
                  "ring through its prime's column. Zeta's zeros know how high a prime sits, not its hour on the wheel.",
             camera=C(at="68008,0,-19>68000,0,-18", fit="130,73>227.6,128", el="8>40", az=-90, spin=0, span=3.5, rise=RISE,
                      tall=dict(at=f"{FT['comb']}>{FT['rings']}", fit=f"{FT['combfit']}>{FT['ringsfit']}")),
             actions=[at("spin", "exp:hide:JM comb labels"), at("spin+0.4", tw("fw.spin", 1, "{spin~sits}")),
                      say("spun round the axis:  a ring per prime power", "ζ's zeros fix the height, not the hour")]),
        dict(text="E eight knows more. Its field has eight L-functions, and at the height of seven each one's zeros light the tooth "
                  "of seven, turned by its own character: along {1, −1, i or −i.|one, minus one, i or minus i.}",
             camera=C(at="68000,0,-18>68004,0,-12", fit="227.6,128>76,43", el="40>70", az=-90, spin=0, span=5, rise=.04,
                      tall=dict(at=f"{FT['rings']}>68011,0,-12", fit=f"{FT['ringsfit']}>58,103.1", rise=.045)),
             actions=[at("e", tw("fw.spin", 0, 3)), at("height", tw("fw.scan", -1, 0)), at("height", "exp:hide:JM comb"),
                      at("height", "exp:hide:JM comb labels"),
                      at("height", "exp:fwhl:7"), at("light", "exp:fwchars:7"),
                      say("eight L-functions at the height of 7", "each tooth turned by its character χ(7):  1, −1, i or −i")]),
        dict(text="Turn each one back by its character at an hour, and chain the eight. At every other hour they cancel; only at "
                  "hour seven do all eight line up, and the chain swings onto {the spoke of 7.|the spoke of seven.}",
             camera=C(at="68004,0,-12", fit="76,43", el=70, az=-90, spin=0, rise=.04, tall=dict(at="68011,0,-12", fit="58,103.1", rise=.045)),
             actions=[at("turn", "exp:fwturn:7:{turn~end}"), say("turned back by χ(h), and chained", "the eight line up only for the hour of 7")]),
        dict(text="Do this at every height, and the comb splits into eight fans, one for each gold hour: only the primes on spoke h "
                  "survive in fan h.",
             camera=C(at="68004,0,-12>68000,0,-16", fit="76,43>130,130", el="70>46", az=-90, spin=0, span=2.5, rise=.04,
                      tall=dict(at=f"68011,0,-12>{FT['fans']}", fit=f"58,103.1>{FT['fansfit']}", rise=.045)),
             actions=["exp:fwunhl", "exp:clearchild:FW chars", at("splits", tw("fw.fan", 1, "{splits~survive}")),
                      say("eight L-functions, eight fans", "only the primes on spoke h survive")]),
        dict(text="{49 = 7² lights on spoke 19.|Forty-nine lights on spoke nineteen, as seven squared.} Eleven and forty-one share spoke eleven. Seven thousand "
                  "four hundred zeros sort every prime up to four hundred onto its spoke.",
             camera=C(at="68000,0,-16", fit="130,130>110,110", el="46>62", az="+", spin=2, span=10, rise=.04,
                      tall=dict(at=FT['spinfans'], fit=f"{FT['spinfansfit']}>110,196", rise=.045)),
             actions=[at("fortynine", "exp:fwhl:49"), at("fortynine", call("fun", 49, 0, 0, "49 = 7²:  hour 19")),
                      at("eleven", "exp:fwhl:11"), at("eleven", call("fun", 11, 0, 0, "11")), at("fortyone", "exp:fwhl:41"),
                      at("fortyone", call("fun", 41, 0, 0, "41 = 11 + 30")),
                      say("7 420 zeros up to t = 1000", "ζ's give the height, the other seven the hour")]),
        dict(text="So the zeros are not points in the stack. They are frequencies of its height. Added up, they rebuild the prime "
                  "powers, and turned by E eight's characters, they put each prime on its spoke. They reconstruct the primes; "
                  "they do not predict them.",
             camera=C(at="68000,0,-16", fit="110,110>124,140", el="62>30", az="+", spin=4, span=12, rise=.04,
                      tall=dict(at=FT['spinfans'], fit=f"110,196>{FT['spinfansfit']}", rise=.045)),
             actions=["exp:uncall", "exp:fwunhl", "exp:show:JM coils",
                      say("the zeros are frequencies of the height", "they rebuild the primes:  they do not predict them")], pause=1.4),
    ])

# ---------------------------------------------------------------------------------------------------------------
# the zeros inside E8, 5: 240 roots and infinitely many counting numbers: the wheel tilts, and E8's shells stack under it
# ---------------------------------------------------------------------------------------------------------------

TOWER = dict(el=22, spin=1, rise=.08)
WHEEL5 = (36254, 36346, -46, 46)                    # E8's wheel with the spiral to radius 40 and its hour labels
CUBES = "".join(f"exp:e8cubesat:{n}:30,0,{16 * (n - 1)}:1.6:3:.4:70|" for n in range(1, 8)).rstrip("|").split("|")
XZ5 = dict(
    id="xz5", title="240 roots, infinitely many numbers", subtitle="the zeros inside E8, part 5:  E8's clock and E8's shells",
    setup=["exp:stage", "exp:backdrop:off", "exp:e8wheel:66:150:15:40", "exp:hide:E8 spiral", "exp:hide:E8 impostors", "exp:hide:E8 rings",
           f"exp:e8tower:{alt(120, 92)}:attach:16", "exp:hide:E8 tower A", "exp:hide:E8 tower B", "exp:hide:E8 tower C", "exp:e8links:7:16"] + CUBES +
          [f"exp:hide:E8 cubes {n}" for n in range(1, 8)] + [f"exp:hide:E8 cubes {n} label" for n in range(1, 8)],
    lines=[
        dict(text="E eight has two hundred and forty roots, shown here in its Coxeter plane as eight rings of thirty. So how can "
                  "so few points hold infinitely many counting numbers?",
             camera=C(at="36300,0,0", fit="44,50", el=89.5, az=-90, spin=0, rise=RISE, tall=dict(fit="30,53")),
             actions=[say("E8:  240 roots", "8 rings of 30 in the Coxeter plane"), "exp:reveal:E8 rings:{two~thirty}", "exp:snap"]),
        dict(text="They don't have to. The roots set E eight's clock: its Coxeter element turns the plane by a thirtieth of a turn, "
                  "so the wheel has thirty hours, and eight of them, E eight's exponents, are gold.",
             camera=C(at=f"36300,0,0>{BOX(*WHEEL5)['at']}", fit=f"44,50>{BOX(*WHEEL5)['fit']}", el=89.5, az=-90, spin=0, hold=.5, span=3, rise=RISE,
                      tall=dict(at=f"36300,0,0>{BOX(*WHEEL5, tall=True)['at']}", fit=f"30,53>{BOX(*WHEEL5, tall=True)['fit']}")),
             actions=["exp:show:E8 rings", "exp:e8walk:line", say("E8's clock:  30 hours", "the 8 gold hours:  1, 7, 11, 13, 17, 19, 23, 29")]),
        dict(text="Drop every counting number onto the wheel at its hour, {n mod 30,|n mod thirty,} one turn further out each time round. The "
                  "spiral never ends.",
             camera=TOP(WHEEL5),
             actions=[at("drop", "exp:reveal:E8 spiral:{drop~end}"), say("every n at its hour", "n mod 30:  the hours repeat, the radius grows")]),
        dict(text="Grey it out and follow one hour, its arm drawn from the centre. Seven, thirty-seven, sixty-seven and ninety-seven "
                  "all land on hour seven, each one turn further out: all four are primes. One hundred and twenty-seven, too.",
             camera=C(at=f"{BOX(*WHEEL5)['at']}>36324,2,0", fit=f"{BOX(*WHEEL5)['fit']}>60,40", el=89.5, az=-90, spin=0, rise=RISE, span=4,
                      tall=dict(at=f"{BOX(*WHEEL5, tall=True)['at']}>36324,2,0", fit=f"{BOX(*WHEEL5, tall=True)['fit']}>34,60")),
             actions=[at("grey", "exp:mute:E8 spiral:1.2"), at("seven#1", "exp:e8hl:7:spoke"), at("thirtyseven", "exp:e8hl:37"),
                      at("sixtyseven", "exp:e8hl:67"), at("ninetyseven", "exp:e8hl:97"), at("twentyseven", "exp:e8hl:127"),
                      at("seven#1", call("e8", 7, -.3, 1, "7", frac=.1)), at("thirtyseven", call("e8", 37, -.1, -1, "37 = 7 + 30", frac=.12)),
                      at("sixtyseven", call("e8", 67, .1, 1, "67 = 7 + 2·30", frac=.12)), at("ninetyseven", call("e8", 97, .3, -1, "97 = 7 + 3·30", frac=.12)),
                      at("twentyseven", call("e8", 127, .6, 1, "127 = 7 + 4·30", frac=.12)),
                      say("one hour:  7, 37, 67, 97, 127", "n ≡ 7 (mod 30)")]),
        dict(text="Hour six holds six, thirty-six, sixty-six and ninety-six: every one a multiple of six, off the gold spokes. The "
                  "clock sorts out two, three and five; the rest it cannot see.",
             camera=C(at=f"36324,2,0>{BOX(*WHEEL5)['at']}", fit=f"60,40>{BOX(*WHEEL5)['fit']}", el=89.5, az=-90, spin=0, rise=RISE, span=4,
                      tall=dict(at=f"36324,2,0>{BOX(*WHEEL5, tall=True)['at']}", fit=f"34,60>{BOX(*WHEEL5, tall=True)['fit']}")),
             actions=["exp:uncall", "exp:e8unhl", at("six#1", "exp:e8hl:6:spoke"), at("thirtysix", "exp:e8hl:36"), at("sixtysix", "exp:e8hl:66"),
                      at("ninetysix", "exp:e8hl:96"), at("six#1", call("e8", 6, 0, 0, "6 = 2·3", frac=.08, col=RED)),
                      at("ninetysix", call("e8", 96, 0, 0, "96 = 2<sup>5</sup>·3", frac=.16, col=RED)),
                      say("hour 6:  6, 36, 66, 96", "multiples of 6, off the gold spokes")]),
        dict(text="And E eight itself is not two hundred and forty points. It is an infinite lattice, and the roots are only its first "
                  "shell: the points at distance {√2.|root two.} Tilt the wheel, and the next shells appear beneath it.",
             camera=C(at=f"{BOX(*WHEEL5)['at']}>36300,0,16", fit=f"{BOX(*WHEEL5)['fit']}>150,84.4", el="89.5>34", az=-90, spin=0, span=6, rise=RISE,
                      tall=dict(at=f"{BOX(*WHEEL5, tall=True)['at']}>36300,-8.8,16", fit=f"{BOX(*WHEEL5, tall=True)['fit']}>52,92.4")),
             actions=["exp:uncall", "exp:e8unhl", at("infinite", "exp:hide:E8 spiral"), at("infinite", "exp:hide:E8 walk"),
                      at("tilt", "exp:reveal:E8 tower A:{tilt~appear}"),
                      say("E8 is a lattice:  shells", "the 240 roots are its first shell, at distance √2")]),
        # the whole tower, turning: one shell for every counting number
        dict(text="Each shell n holds the points at distance {√(2n):|root two n:} two thousand one hundred and sixty for n equal to two, six "
                  "thousand seven hundred and twenty {for 3,|for three,} seventeen thousand five hundred and twenty {for 4,|for four,} and on down to "
                  "{7.|seven.} Every counting number has a shell of its own.",
             camera=C(at="36300,0,16>36296,0,42", fit="150,84.4>291.6,164", el="34>32", az=-90, spin=2.5, span=6, rise=RISE,
                      tall=dict(at="36300,-8.8,16>36300,7.2,48", fit="52,92.4>104.3,185.3")),
             actions=[at("seventeen", "exp:reveal:E8 tower B:1.5"), at("seven#2", "exp:reveal:E8 tower C:2"),
                      say("a shell for every counting number", "shell n:  the points at distance √2n:  240, 2 160, 6 720, 17 520, …")]),
        # the cubes, floor by floor: one cube for each divisor
        dict(text="Each shell's size is two hundred and forty times a sum of cubes, one cube for each divisor of n. {1 has the single cube 1.|One has the single "
                  "cube one.} {2 has 1 and 8.|Two has one and eight.} {3 has 1 and 27.|Three has one and twenty-seven.}",
             camera=C(at="36296,0,42>36284,0,14", fit="291.6,164>131.6,74", el="32>30", az=-90, spin=0, span=4, rise=RISE,
                      tall=dict(at="36300,7.2,48>36311,-18,16", fit="104.3,185.3>66,117.3")),
             actions=[at("one#1", "exp:mute:E8 tower B,E8 tower C:1.5:1.5"), "exp:reveal:E8 cubes 1:{one#2},:own",
                      "exp:reveal:E8 cubes 2:{one#4},{eight}:own", "exp:reveal:E8 cubes 3:{one#5},{twentyseven}:own",
                      say("shell n holds 240 σ<sub>3</sub>(n) points", "σ<sub>3</sub>(n):  one cube d³ for each divisor d of n")]),
        dict(text="{4 has 1, 8 and 64:|Four has one, eight and sixty-four:} its divisors {1, 2 and 4.|one, two and four.} {5,|Five,} a prime, has only one and a hundred and "
                  "twenty-five. {6|Six} has four cubes: one, eight, twenty-seven and two hundred and sixteen. {7,|Seven,} a prime, has one "
                  "and three hundred and forty-three.",
             camera=C(at="36284,0,14>36280,0,66", fit="131.6,74>184.9,104", el=30, az=-90, spin=0, span=5, rise=RISE,
                      tall=dict(at="36311,-18,16>36319.5,-24.9,72", fit="66,117.3>79.8,141.9")),
             actions=["exp:unmute:E8 tower B,E8 tower C:1.5", "exp:mute:E8 tower A:1.5:1.5",
                      "exp:reveal:E8 cubes 4:{one#1},{eight#1},{sixtyfour}:own", "exp:reveal:E8 cubes 5:{one#3},{twentyfive}:own",
                      "exp:reveal:E8 cubes 6:{one#4},{eight#2},{twentyseven},{sixteen}:own",
                      "exp:reveal:E8 cubes 7:{one#5},{fortythree}:own",
                      say("the cubes of the divisors", "4:  1 + 8 + 64   ·   5:  1 + 125   ·   6:  1 + 8 + 27 + 216   ·   7:  1 + 343")]),
        # the links between the shells: divisibility
        dict(text="So the shells are linked by divisibility: each shell sends its cube to the shells of its multiples. {1|One} feeds every "
                  "shell. {2 feeds 4 and 6.|Two feeds four and six.} {3 feeds 6.|Three feeds six.}",
             camera=C(at="36280,0,66>36292,0,42", fit="184.9,104>291.6,164", el="30>32", az=-90, spin=-1.5, span=4, rise=RISE,
                      tall=dict(at="36319.5,-24.9,72>36310.5,5,48", fit="79.8,141.9>106.4,189.2")),
             actions=["exp:unmute:E8 tower A:1.5", at("one", "exp:showchild:E8 links:link 1-"), at("one", "exp:drawon:E8 links/arc 1-:1.5"),
                      at("two", "exp:showchild:E8 links:link 2-"), at("two", "exp:drawon:E8 links/arc 2-:1.2"),
                      at("three", "exp:showchild:E8 links:link 3-"), at("three", "exp:drawon:E8 links/arc 3-:1"),
                      say("linked by divisibility", "shell d sends d³ to every shell n that d divides")]),
        # why this is useful: the extra cubes are the factors
        dict(text="That is why the shells are a useful way to look at numbers. A prime's shell holds the fewest points it can, just {1 "
                  "and p³.|one "
                  "and p cubed.} Every extra cube is a divisor: {6's|six's} extra cubes, eight and twenty-seven, are {2³ and 3³,|two cubed and three "
                  "cubed,} so E eight's geometry has factored {6.|six.}",
             camera=C(at="36292,0,42>36300,0,78", fit="291.6,164>142.2,80", el="32>30", az=-90, spin=0, span=5, rise=RISE,
                      tall=dict(at="36310.5,5,48>36319.5,-24.9,80", fit="106.4,189.2>77.6,138")),
             actions=["exp:mute:E8 tower A,E8 tower B/floor 4,E8 cubes 1,E8 cubes 2,E8 cubes 3,E8 cubes 4,E8 links:1.5:1.6", at("primes", "exp:pulse:E8 cubes 5,E8 cubes 7:1.6"), at("six#1", "exp:pulse:E8 cubes 6:2"),
                      say("a prime's shell:  240 (1 + p³), the fewest", "6:  the extra cubes 8 = 2³ and 27 = 3³ are its factors")]),
        dict(text="Counted over every shell at once, the points of E eight give its own zeta function: {240 ζ(s) ζ(s − 3).|two hundred and forty times "
                  "zeta of s, times zeta of s minus three.} A finite symmetry, and infinitely many shells, one for every counting "
                  "number.",
             camera=C(at="36300,0,78>36304,0,42", fit="142.2,80>291.6,164", el="30>34", az=-90, spin=1.5, span=8, rise=RISE,
                      tall=dict(at="36319.5,-24.9,80>36310.5,5,48", fit="77.6,138>106.4,189.2")),
             actions=["exp:unmute:E8 tower A,E8 tower B/floor 4,E8 cubes 1,E8 cubes 2,E8 cubes 3,E8 cubes 4,E8 links:2",
                      say("Σ 240 σ<sub>3</sub>(n) n<sup>−s</sup> = 240 ζ(s) ζ(s − 3)", "a finite symmetry, a shell for every counting number")],
             pause=1.4),
    ])

# ---------------------------------------------------------------------------------------------------------------
# the twist on the twist (README QA–QC): squares on the links of the chain at zero 300, their fins orbiting the links; the
# fins summed (a road and the octave chain); the Sacks spiral's squares turned by the zeros, summed until they stand on the
# prime powers
# ---------------------------------------------------------------------------------------------------------------

T300 = 541.8474371212013          # zero 300: the chain has t/π + 12 = 184 links; spokes at n = t/τk, knots at n = t/(2k + 1)π
# boxes in the path's plane (world units from its origin, 12 a unit; x right, y up the plane): the whole chain with its
# squares, spokes 1 and 2, the fins' road, and the octave chain left when the road is taken away
OB_CHAIN, OB_SPOKES, OB_ROAD, OB_OCT = (-17, 19, -6, 30), (-17, 3, -5, 10), (-17, 19, -8, 160), (-6, 6, -4.5, 7.5)
TILT_FINS = 56                     # the chain seen a little from the side, so the fins' ups and downs show


def OBOX(*boxes, el=89.5, span=None, hold=None, **kw):
    """A shot of the path's plane framing one box, or easing from the first to the second (both shapes fitted)."""
    w = [BOX(*b) for b in boxes]
    t = [BOX(*b, tall=True) for b in boxes]
    extra = {}
    if span is not None:
        extra["span"] = span
    if hold is not None:
        extra["hold"] = hold
    return PC(at=">".join(x["at"] for x in w), fit=">".join(x["fit"] for x in w), el=el, rise=RISE,
              tall=dict(at=">".join(x["at"] for x in t), fit=">".join(x["fit"] for x in t)), **extra, **kw)


# the Sacks spiral (n ≤ 400, radius 24) at PathOrigin: its middle close (the square of 40), the whole from 66° up (the rings),
# and from 44° up (the needles)
SQ_TOP = dict(at=BOX(-25, 25, -25, 25)["at"], fit=BOX(-25, 25, -25, 25)["fit"], tall=dict(fit=BOX(-25, 25, -25, 25, tall=True)["fit"]))
SQ_RINGS = dict(at="0,0,-1", fit="106,59.6", tall=dict(fit="54,96"))
SQ_NEEDLES = dict(at="0,1,-5", fit="106,59.6", tall=dict(fit="54,96"))


def SQ(a, b=None, el=66, span=None, hold=None):
    """A shot of the Sacks spiral (one framing, or easing from a to b)."""
    b = b or a
    kw = dict(at=a["at"] if a is b else f"{a['at']}>{b['at']}", fit=a["fit"] if a is b else f"{a['fit']}>{b['fit']}", el=el, rise=RISE,
              tall=dict(fit=a["tall"]["fit"] if a is b else f"{a['tall']['fit']}>{b['tall']['fit']}"))
    if span is not None:
        kw["span"] = span
    if hold is not None:
        kw["hold"] = hold
    return PC(**kw)


XORBIT = dict(
    id="xorbit", title="The twist on the twist", subtitle="squares that orbit the links of zeta's chain",
    setup=["exp:stage", "exp:backdrop:off", "exp:vortexprep", "exp:pwinit", "exp:sqinit", tw("pw.t", T300, 0), tw("pw.n", 0, 0),
           tw("pw.tail", 0, 0), tw("pw.marks", 0, 0), tw("pw.readout", 1, 0), tw("pw.rise", 0, 0), tw("pw.fins", 0, 0),
           tw("pw.alpha", 0, 0), tw("pw.lift", 1, 0)],
    lines=[
        dict(text="Claude, an AI math research assistant, and I gave every link of zeta's chain a square. This is the chain at the "
                  "three hundredth zero, t equal to five hundred forty-one point eight five: a link for every counting number.",
             camera=OBOX(OB_CHAIN),
             actions=["exp:snap", say("the twist on the twist", "a square on every link of zeta's chain"),
                      at("gave", tw("pw.n", 1, .6)), at("gave+0.6", tw("pw.n", 184, "{gave~end}", "geo")),
                      at("this", say("zeta's chain at zero 300", "t = 541.85:  link n is n<sup>−½</sup> long, turned t · log n"))]),
        dict(text="Link n is one over root n long. Stand a fin of the same length at its start, perpendicular to it, and the link "
                  "and its fin are two sides of a square, of area {1/n.|one over n.}",
             camera=OBOX(OB_CHAIN),
             actions=[at("stand", tw("pw.fins", 1, "{stand~two}")),
                      at("square", call("fin", 1, -.6, .5, "link 1 and its fin:  area 1", tall=(-.3, .6))),
                      say("a square on every link", "link n and its fin:  a square of area 1/n")]),
        dict(text="Now the twist on the twist. While each link turns clockwise by {t · log n,|t times log n,} let each fin turn about its "
                  "own link by {α · log n,|alpha times log n,} so that it orbits its joint: gold where it points up out of the plane, cyan "
                  "where it points down.",
             camera=OBOX(OB_CHAIN, el=f"89.5>{TILT_FINS}", span=4),
             actions=["exp:uncall", at("let", tw("pw.alpha", .5, "{let~end}")),
                      say("the twist on the twist", "each fin turns α · log n:  gold up, cyan down")]),
        dict(text="Stop at {α = t/2,|alpha equal to t over two,} half the chain's rate. In spoke k, where each link turns k whole turns "
                  "from the last, its fin turns k half-turns. So in the odd spokes the fins flip from link to link, and in the even "
                  "spokes they line up.",
             camera=OBOX(OB_CHAIN, OB_SPOKES, el=TILT_FINS, span=4),
             actions=[say("α = t/2", "in spoke k a link turns its fin k half-turns"),
                      at("odd", call("path", 86, .4, -1, "spoke 1:  the fins flip", tall=(.3, -1))),
                      at("even", call("path", 43, -.5, 1, "spoke 2:  they line up", tall=(-.2, 1)))]),
        dict(text="At {α = t,|alpha equal to t,} the chain's own rate, each fin tilts by its own link's angle, so the fins line up "
                  "wherever the links do.",
             camera=OBOX(OB_SPOKES, OB_CHAIN, el=TILT_FINS, span=4),
             actions=["exp:uncall", at("at", tw("pw.alpha", 1, "{at~end}")),
                      say("α = t:  the chain's own rate", "each fin tilts by its own link's angle")]),
        dict(text="Now add the fins up, head to tail, seen from above. Half of every fin points the same way, straight up the "
                  "plane, so the sum runs off along a straight road: the chain at height zero, which never turns.",
             camera=OBOX(OB_CHAIN, OB_ROAD, el=f"{TILT_FINS}>89.5", span=7, hold="{add}"),
             actions=[at("add", tw("pw.lift", 0, 4)), at("add", tw("pw.finsum", 1, "{add~road}")),
                      say("the fins, head to tail", "half of each fin points up:  a road, Σ 1/(2√n)")]),
        dict(text="Take the road away, and what is left is half the chain an octave up, at {2t.|two t.} So the twist on the twist is "
                  "a beat: the fins carry two zeta chains, at {t − α|t minus alpha} and {t + α.|t plus alpha.}",
             camera=OBOX(OB_ROAD, OB_OCT, span=4),
             actions=[at("take", tw("pw.road", 1, "{take~left}")), at("take", "exp:mute:PW path:2:1"),
                      say("the road taken away", "left:  half the chain at 2t, an octave up"),
                      at("beat", say("a beat", "the fins in the plane:  (i/2)(S(t − α) + S(t + α))"))]),
        dict(text="This octave chain runs straight where the chain itself runs straight, in its spoke near n equal to eighty-six. "
                  "And it runs straight through the chain's knot near one hundred seventy-two, where the chain curls up tight. The "
                  "twist on the twist turns knots into spokes.",
             camera=OBOX(OB_OCT),
             actions=[at("spoke", call("fin", 86, .5, .9, "n ≈ 86:  spoke 1 of both", tall=(.4, .9))),
                      at("knot", call("fin", 172, -.6, .8, "n ≈ 172:  straight here", tall=(-.5, .8))),
                      at("curls", call("path", 172, .3, -1, "the chain's knot 0", tall=(.3, -1))),
                      say("knots become spokes", "the octave runs straight at spokes and knots")]),
        dict(text="Now the Sacks spiral, where every number n sits at radius {√n.|root n.} The radius is the square root itself, so a "
                  "radius and a fin as long make a square of area n.",
             camera=PC(at=f"{BOX(*OB_OCT)['at']}>{SQ_TOP['at']}", fit=f"{BOX(*OB_OCT)['fit']}>{SQ_TOP['fit']}", span=4, rise=RISE,
                       tall=dict(at=f"{BOX(*OB_OCT, tall=True)['at']}>{SQ_TOP['at']}",
                                 fit=f"{BOX(*OB_OCT, tall=True)['fit']}>{SQ_TOP['tall']['fit']}")),
             actions=["exp:uncall", "exp:mute:PW path,PW fins:1.5:2", tw("pw.readout", 0, 1), at("spiral", tw("sq.dots", 1, 2.5)),
                      at("radius#2", "exp:hide:PW path"), at("radius#2", "exp:hide:PW fins"), at("square", tw("sq.demo", 1, 1.5)),
                      at("square", call("sqsq", 40, .6, .8, "n = 40:  radius √40, area 40", tall=(.3, .9))),
                      say("the Sacks spiral", "n at radius √n:  radius and fin, a square of area n")]),
        dict(text="Give every number its fin, drawn at a quarter of its length, and turn each fin about its radius by the first "
                  "zero's phase, {γ · log n.|gamma times log n.} The fins point down in rings, on the zero's crests, and up between them.",
             camera=SQ(SQ_TOP, SQ_RINGS, el="89.5>45", span=6),
             actions=["exp:uncall", tw("sq.demo", 0, 1.5), at("give", tw("sq.fins", 1, 2)), at("turn", tw("sq.tilt", 1, "{turn~point}")),
                      say("each fin turned by the first zero", "φ = γ<sub>1</sub> log n − τ/4:  the fin's height −√n cos(γ<sub>1</sub> log n)")]),
        dict(text="Now add the fins turned by the second zero, the third, and on up to ten thousand zeros, each sum divided by "
                  "{T/τ.|T over tau.} The fins shrink away, except on the prime powers, where they stand up straight.",
             camera=SQ(SQ_RINGS, SQ_NEEDLES, el="45>42", span=12),
             actions=[at("add", tw("sq.hud", 1, 1)), at("add", tw("sq.k", 13, "{add~end}", "linear")),
                      at("shrink", tw("sq.h", 2.2, "{shrink~end}")),
                      say("summed over the zeros", "K zeros' fins added, ÷ T/τ  (T: the K-th zero)")]),
        dict(text="Landau proved why. Added up to height T, the zeros' waves give {−(T/τ) · log p/√n|minus T over tau, times log p over "
                  "root n,} at every power n of a prime p. The fin's length, {√n,|root n,} cancels the one over root n, so each needle "
                  "stands at log p.",
             camera=SQ(SQ_NEEDLES, el=42),
             actions=[say("Landau", "Σ<sub>γ ≤ T</sub> n<sup>iγ</sup> = −(T/τ) Λ(n)/√n + O(log T)")]),
        dict(text="Three hundred seventy-nine is a prime: its needle reaches five point seven eight, nearly {log 379.|log three seventy-nine.} "
                  "Three hundred sixty-one, nineteen squared, reaches two point eight, nearly log nineteen. And three hundred "
                  "seventy-one, {7 · 53,|seven times fifty-three,} stays at zero.",
             camera=SQ(SQ_NEEDLES, el=42),
             actions=[at("prime", call("sq", 379, .3, 1, "379, a prime:  5.78 ≈ log 379", tall=(.3, 1))),
                      at("sixtyone", "exp:uncall"), at("sixtyone+0.1", call("sq", 361, -.3, 1, "361 = 19²:  2.80 ≈ log 19", tall=(-.3, 1))),
                      at("seventyone", "exp:uncall"), at("seventyone+0.1", call("sq", 371, 0, 1, "371 = 7 · 53:  −0.04", tall=(0, 1))),
                      say("10 000 zeros:  the needles", "379 → 5.78,  361 = 19² → 2.80,  371 = 7 · 53 → 0")]),
        dict(text="So the twist on the twist creates nothing new: it rearranges the chain's vortices into a beat of two chains. And "
                  "on the Sacks spiral, the side of each number's square is exactly the weight that stands the zeros' waves up on "
                  "the primes.",
             camera=SQ(SQ_NEEDLES, el="42>38", span=10),
             actions=["exp:uncall", say("what the twist shows", "two chains in a beat;  √n is Landau's weight")],
             pause=1.4),
    ])

# ---------------------------------------------------------------------------------------------------------------
# the marble language (marbles): the games' fingerprints, without digits, unpacked into their volumes
# ---------------------------------------------------------------------------------------------------------------

MW = 128000           # the marble world's origin (SacksMarbleWorld.cs); a fingerprint is 1 world unit per marble here


def MWB(x0, x1, y0, y1):
    """A box in the marble world (world units from its origin)."""
    return (MW + x0, MW + x1, y0, y1)


# 360's fingerprint at the origin: its shorthand (the badge to the 5), the opened string (six containers), and the opened
# string with the volume above it (the copies' rings reach a little past the volume's own rim)
MW_SHORT, MW_LONG, MW_ALL = MWB(-1.5, 17, -4.2, 4.2), MWB(-1.5, 27, -4.2, 4.2), MWB(-1.5, 27, -4.5, 24.5)
# close on the volume (centre 12.7, 12.45) while it is small: 2, 4, 8; then 24 and 72 with their copies
MW_V1 = MWB(8.2, 17.2, 8, 17)
# one container with its rings (97's, with room above for a callout; 625's), and 625 opened with its volume
MW_ONE, MW_625 = MWB(-1.5, 15, -8.5, 12), MWB(-1.5, 19, -4.5, 22)


def mwshow(slot, n, x=0, y=0, digits=True):
    return f"exp:mwshow:{slot}:{n}:{x},{y},0:1" + ("" if digits else ":nodigits")


MARBLES = dict(
    id="marbles", title="Numbers in marbles", subtitle="the games' counting language, unpacked",
    setup=["exp:stage", "exp:backdrop:off", "exp:mwinit", mwshow("a", 360), tw("mw.a.reveal", 0, 0), tw("mw.a.phase", 0, 0),
           tw("mw.a.depth", 0, 0)],
    lines=[
        dict(text="Claude, an AI math research assistant, and I wrote numbers without digits, in marbles. This is three "
                  "hundred sixty.",
             camera=TOP(MW_SHORT),
             actions=["exp:snap", say("numbers in marbles", "no digits:  a number written as its own count"),
                      at("wrote", tw("mw.a.reveal", 1, "{wrote~end}"))]),
        dict(text="The grey marble says it is a composite. Each gold ring is a container, holding a prime number of marbles in "
                  "a symmetric spiral: two, three and five.",
             camera=TOP(MW_SHORT),
             actions=[at("grey", call("mw", "a/badge", -.2, 1, "grey:  a composite", tall=(.2, 1))),
                      at("two", "exp:uncall"), at("two+0.1", call("mw", "a/cup/0", 0, 1, "2", tall=(0, 1))),
                      at("three", call("mw", "a/cup/1", 0, 1, "3", tall=(0, 1))),
                      at("five", call("mw", "a/cup/2", 0, 1, "5", tall=(0, 1))),
                      say("prime containers", "each gold ring holds a prime number of marbles")]),
        dict(text="The tilted rings are the exponents, one more dimension each. Two wears two of them, so it counts three times: "
                  "two cubed. Three wears one: three squared.",
             camera=TOP(MW_SHORT),
             actions=["exp:uncall", at("two#2", call("mw", "a/ring/0", -.3, 1, "2 · 2 · 2 = 2³", tall=(-.45, 1))),
                      at("three#2", call("mw", "a/ring/1", .6, .9, "3 · 3 = 3²", tall=(.75, .9))),
                      say("exponents as tilted rings", "a container's rings, its own included:  how often its prime counts")]),
        dict(text="Now unpack it. The rings slide out along the string, and each one closes into a copy of its container: two, "
                  "two, two, three, three, five.",
             camera=TOP(MW_SHORT, MW_LONG, span=4),
             actions=["exp:uncall", at("slide", tw("mw.a.phase", 1, "{slide~two}")),
                      say("unpacking 360", "each ring becomes a copy of its container")]),
        dict(text="The first container pours its two marbles into the middle.",
             camera=TOP(MW_LONG, MW_V1, span=1.6, hold="{pours}"),
             actions=[at("pours", tw("mw.a.phase", 2, "{pours~end}")), say("2", "the first container, poured out")]),
        dict(text="The next sends one marble to each of two spots. A copy of the pair blooms at each, and the two copies wind "
                  "together into two spiral arms: four.",
             camera=TOP(MW_V1),
             actions=[at("sends", tw("mw.a.phase", 3, "{sends~four}")), say("2 × 2 = 4", "two copies wind into two arms")]),
        dict(text="Times two again makes eight. Then three copies wind into three arms: twenty-four. And three again: "
                  "seventy-two.",
             camera=TOP(MW_V1, MW_ALL, span=2.2, hold="{twentyfour}"),
             actions=[at("times", tw("mw.a.phase", 4, "{times~eight}")), at("three", tw("mw.a.phase", 5, "{three~twentyfour}")),
                      at("again#2", tw("mw.a.phase", 6, "{again#2~end}")), say("8,  24,  72", "p copies, p arms")]),
        dict(text="Last, the five: five copies of seventy-two wind into five arms. Three hundred sixty marbles, in their own "
                  "colour, grey.",
             camera=TOP(MW_ALL),
             actions=[at("five#2", tw("mw.a.phase", 7, "{five#2~arms}")),
                      say("72 × 5 = 360", "five arms of 72:  the full volume, in the number's own colour")]),
        dict(text="In three dimensions, every group of marbles packs into a spiral sphere, and the rings turn in planes of "
                  "their own.",
             camera=TOP(MW_ALL),
             actions=["exp:uncall", at("three", tw("mw.a.depth", 1, "{three~sphere}")),
                      say("in 3D", "every group of marbles a spiral sphere pack")]),
        dict(text="And it packs back down into its shorthand.",
             camera=TOP(MW_ALL, MW_SHORT, span=2, hold=3.6),
             actions=[at("packs", tw("mw.a.phase", 0, 5)), at("packs", tw("mw.a.depth", 0, 5)),
                      say("packed", "360 = 2³ · 3² · 5")],
             pause=.8),
        dict(text="A prime is one container and nothing more: ninety-seven, a marble in the middle and six arms of sixteen.",
             camera=TOP(MW_SHORT, MW_ONE, span=3),
             actions=[tw("mw.a.fade", 0, 1), at("prime+0.6", mwshow("b", 97)), at("prime+0.6", tw("mw.b.reveal", 0, 0)),
                      at("prime+0.7", tw("mw.b.reveal", 1, "{prime~ninetyseven}")),
                      say("a prime", "97 = 1 + 6 × 16:  one container, no rings")]),
        dict(text="A prime power wears more rings: five to the fourth is a ring of five marbles inside three tilted rings.",
             camera=TOP(MW_ONE),
             actions=["exp:uncall", tw("mw.b.fade", 0, 1), at("power", mwshow("c", 625)), at("power", tw("mw.c.reveal", 0, 0)),
                      at("power+0.1", tw("mw.c.reveal", 1, "{power~fourth}")),
                      at("three", call("mw", "c/ring/0", .4, 1, "four rings:  5⁴", tall=(.3, 1))),
                      say("a prime power", "5⁴:  four rings, one flat and three tilted")]),
        dict(text="It unpacks into six hundred twenty-five marbles: five copies, of five copies, of five copies of five, wound "
                  "into five arms.",
             camera=TOP(MW_ONE, MW_625, span=3),
             actions=["exp:uncall", at("unpacks", tw("mw.c.phase", 5, "{unpacks~end}")),
                      say("5 × 5 × 5 × 5 = 625", "four fives, wound into five arms")]),
        dict(text="No digits needed: every number is written in its own building blocks.",
             camera=TOP(MW_625),
             actions=[at("needed", tw("mw.c.depth", 1, 3)), say("numbers in marbles", "every number, its own building blocks")],
             pause=1.4),
    ])

# ---------------------------------------------------------------------------------------------------------------
# the game tutorials: each chapter opens a game in the model lab and plays it (exp:lab:…, GameModel.StoryCommand); its
# lines' camera "lab" leaves the camera to the game's own views. Recorded 16:9, as the games are played on the desktop.
# ---------------------------------------------------------------------------------------------------------------

LAB = "lab"


def lab(*args):
    return "exp:lab:" + ":".join(str(a) for a in args)


TUT_PRIME = dict(
    id="tutprime", title="The number spiral", subtitle="a game tutorial",
    setup=[lab("enter", "primehunt"), lab("look", 0)],
    lines=[
        dict(text="This is the number spiral: the numbers up to four hundred on the Sacks spiral, each in its colour. Gold for "
                  "the primes, deep red for squares of primes, blue for pronic numbers, and grey for every other composite.",
             camera=LAB, actions=[]),
        dict(text="Touch any number to see what it is made of. Ninety-one looks prime, but its fingerprint says seven times "
                  "thirteen.",
             camera=LAB, actions=[at("ninetyone", lab("pick", 91))]),
        dict(text="Pull the fingerprint apart, and the number rebuilds itself from its primes: thirteen copies of seven, wound "
                  "into thirteen arms.",
             camera=LAB, actions=[at("pull", lab("pull", 3, 3.2))], pause=.6),
        dict(text="Push it back together, and it packs into its shorthand again.",
             camera=LAB, actions=[at("push", lab("pull", 0, 1.6))]),
        dict(text="Step to its neighbours: ninety-two is two squared times twenty-three, and ninety-seven is a prime.",
             camera=LAB, actions=[at("ninetytwo", lab("pick", 92)), at("ninetyseven", lab("pick", 97))]),
        dict(text="Look at one family at a time: the primes, the twin primes, and Euler's k squared plus k plus forty-one.",
             camera=LAB, actions=[at("primes", lab("look", 1)), at("twin", lab("look", 2)), at("euler's", lab("look", 3))], pause=.6),
        dict(text="Or the squares and the pronic numbers, each on a line of its own. Nothing is scored: there are only numbers "
                  "to take apart.",
             camera=LAB, actions=[at("squares", lab("look", 4)), at("nothing", lab("look", 0))], pause=1.2),
    ])

TUT_BLOCKS = dict(
    id="tutblocks", title="Building blocks", subtitle="a game tutorial",
    setup=[lab("enter", "blocks"), lab("autoplay", "off"), lab("splitspeed", 1.05), lab("example", 5)],
    lines=[
        dict(text="This is Building blocks. A number stands as its raw volume: three hundred sixty grey marbles in a symmetric "
                  "spiral. Below it is a tray of prime containers.",
             camera=LAB, actions=[at("tray", lab("point", 7, "prime"))]),
        dict(text="Point at seven. Seven does not divide three hundred sixty: seven equal groups leave three marbles over, "
                  "shining red.",
             camera=LAB, actions=[at("point", lab("prime", 7))]),
        dict(text="Point at two. Two equal groups: one stays, and the other flies to the fingerprint on the left.",
             camera=LAB, actions=[at("point", lab("finish")), at("point+0.1", lab("prime", 2))]),
        dict(text="Keep splitting: by two, then by two again, then by three, by three again, and finally by five.",
             camera=LAB, actions=[at("two", lab("prime", 2)), at("two#2", lab("prime", 2)), at("three", lab("prime", 3)),
                                  at("three#2", lab("prime", 3)), at("five", lab("prime", 5))]),
        dict(text="Nothing is left, so the fingerprint is the whole number: two cubed, three squared, and five. Unpack it, and "
                  "it builds the volume back, beside its string.",
             camera=LAB, actions=[at("unpack", lab("unpack", .7))], pause=6),
        dict(text="When what is left is a prime past the tray, point at the block itself. Two hundred forty-six splits by two, "
                  "then by three, and the forty-one left over is prime.",
             camera=LAB, actions=[at("when", lab("example", 15)), at("splits", lab("prime", 2)), at("three", lab("prime", 3)),
                                  at("prime#2", lab("rest"))], pause=1.2),
    ])

TUT_E8 = dict(
    id="tute8", title="E8's clock", subtitle="a game tutorial",
    setup=[lab("enter", "e8spheres"), lab("shells", "off")],
    lines=[
        dict(text="This is E8's clock. Every number up to six hundred stands at its hour on a clock of thirty hours, as a stack of its prime "
                  "factors: one gold piece, as tall as log p, for each.",
             camera=LAB, actions=[]),
        dict(text="Touch a column, and its path back steps down the staircase. Three hundred sixty drops by two, two, two, three, "
                  "three and five, every landing named by the blocks it has left.",
             camera=LAB, actions=[at("touch", lab("pick", 360, "number"))], pause=.8),
        dict(text="Turn the model to look along the steps: drag it, or pinch it in VR.",
             camera=LAB, actions=[at("turn", lab("view", 10, 22, 240))], pause=1.0),
        dict(text="Three hundred forty-three is seven cubed: three equal drops, each of log seven.",
             camera=LAB, actions=[at("three", lab("home")), at("seven", lab("pick", 343, "number"))], pause=1.2),
        dict(text="Powers shows where the runs of each prime go: two, four, eight and on up the spiral of two to the k, each top "
                  "named with its exponent.",
             camera=LAB, actions=[at("powers", lab("powers", "on"))], pause=.6),
        dict(text="And the shells: E8's lattice points, shell by shell. Touch shell six, and its divisors stand under it as cubes "
                  "of marbles.",
             camera=LAB, actions=[at("shells", lab("powers", "off")), at("shells+0.1", lab("shells", "on")),
                                  at("six", lab("pick", 6, "shell"))], pause=1.2),
    ])

TUT_VORTEX = dict(
    id="tutvortex", title="Zeta's chain", subtitle="a game tutorial",
    setup=[lab("enter", "vortex"), lab("zero", 300, .1)],
    lines=[
        dict(text="This is zeta's chain at its three hundredth zero: a link for every counting number, and its end at zeta of one "
                  "half plus i t.",
             camera=LAB, actions=[]),
        dict(text="Drag up or down, and t moves. The chain winds and unwinds as it goes.",
             camera=LAB, actions=[at("drag", lab("steer", 1.1, 3.2))], pause=.4),
        dict(text="On a touch screen it takes one finger. In VR, pinch the bead on the t rail, and lift it or lower it.",
             camera=LAB, actions=[at("vr", lab("steer", -1.1, 3.2))], pause=.4),
        dict(text="The rail's gold ticks are the zeros. At each one the chain's end lands on zero, and the chain curls into "
                  "vortices.",
             camera=LAB, actions=[at("ticks", lab("zero", 301, 2.4))], pause=.6),
        dict(text="Vortex k belongs to the counting number k. Touch vortex three: its fingerprint, and the mirror line from the "
                  "chain's third vertex to the eye.",
             camera=LAB, actions=[at("touch", lab("pick", 3, "vortex"))], pause=.6),
        dict(text="Then glide on to the next zero, and the next.",
             camera=LAB, actions=[at("glide", lab("jump", 1)), at("next#2", lab("jump", 1))], pause=1.6),
    ])

SERIES = [XS1, XS2, XS3, XS4, XS5, XZ1, XZ2, XZ3, XZ4, XZ5, XORBIT, MARBLES, TUT_PRIME, TUT_BLOCKS, TUT_E8, TUT_VORTEX]
