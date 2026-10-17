"""
Captions that read like print while the narrator says the words.

A line of the long-form script may mark a stretch as {display|spoken}: the voice reads `spoken`, the caption shows
`display`, and the caption's karaoke highlight treats the whole stretch as one word (TourScriptLoader.DisplayTiming).
auto_markup() adds such marks for numbers written out in words, so a line can be written as it is said:

  "Voronoi found in nineteen oh four that ..."   →   "Voronoi found in {1904|nineteen oh four} that ..."
  "t equal to fourteen point one three"          →   "t equal to {14.13|fourteen point one three}"
  "E eight's clock"                              →   "{E8's|E eight's} clock"

Rules for numbers: years, decimals, negatives and anything from 10 up become digits; one to nine stay words unless they
stand in a list (joined by commas, "and", "or", "plus", "times", "to") with a number that became digits
("two, three, five, seven, eleven" → "2, 3, 5, 7, 11"). Write a mark by hand wherever the rules get it wrong.
"""
import re

UNITS = {"zero": 0, "one": 1, "two": 2, "three": 3, "four": 4, "five": 5, "six": 6, "seven": 7, "eight": 8, "nine": 9}
TEENS = {"ten": 10, "eleven": 11, "twelve": 12, "thirteen": 13, "fourteen": 14, "fifteen": 15, "sixteen": 16,
         "seventeen": 17, "eighteen": 18, "nineteen": 19}
TENS = {"twenty": 20, "thirty": 30, "forty": 40, "fifty": 50, "sixty": 60, "seventy": 70, "eighty": 80, "ninety": 90}
ORD = {"first": 1, "second": 2, "third": 3, "fourth": 4, "fifth": 5, "sixth": 6, "seventh": 7, "eighth": 8, "ninth": 9,
       "tenth": 10, "eleventh": 11, "twelfth": 12, "thirteenth": 13, "fourteenth": 14, "fifteenth": 15, "sixteenth": 16,
       "seventeenth": 17, "eighteenth": 18, "nineteenth": 19, "twentieth": 20, "thirtieth": 30, "fortieth": 40,
       "fiftieth": 50, "sixtieth": 60, "seventieth": 70, "eightieth": 80, "ninetieth": 90}
SCALES = {"hundred": 100, "thousand": 1000, "million": 1000000}
ORD_SCALES = {"hundredth": 100, "thousandth": 1000, "millionth": 1000000}
JOINERS = {",", "and", "or", "plus", "times", "to"}
GROUP = re.compile(r"\{([^{}|]*)\|([^{}]*)\}")


def spoken_of(text):
    """What the narrator says: every {display|spoken} mark replaced by its spoken side."""
    return GROUP.sub(lambda m: m.group(2), text)


def display_of(text):
    """What the caption shows."""
    return GROUP.sub(lambda m: m.group(1), text)


def _small(word):
    """The value of a one-word number below 100 (hyphenated compounds included), else None."""
    w = word.lower()
    if w in UNITS:
        return UNITS[w]
    if w in TEENS:
        return TEENS[w]
    if w in TENS:
        return TENS[w]
    if "-" in w:
        a, _, b = w.partition("-")
        if a in TENS and b in UNITS and UNITS[b] > 0:
            return TENS[a] + UNITS[b]
    return None


def _small_ord(word):
    w = word.lower()
    if w in ORD:
        return ORD[w]
    if "-" in w:
        a, _, b = w.partition("-")
        if a in TENS and b in ORD and ORD[b] < 10:
            return TENS[a] + ORD[b]
    return None


def _fmt(n):
    if n >= 1000000 and n % 1000000 == 0:
        return f"{n // 1000000} million"
    return f"{n:,}"


def _parse(words, i):
    """A number phrase starting at words[i] (bare words, no punctuation): (end, value_text, kind, value) or None.
    kind: 'int', 'dec', 'year', 'ord'."""
    n = len(words)

    def integer(j):
        """Parse a cardinal with scales from j: (end, value, ordinal?) or None."""
        total, current, k, used = 0, 0, j, False
        while k < n:
            w = words[k].lower()
            v = _small(w)
            if v is not None:
                # two plain numbers in a row without a scale between them are two numbers ("fifty-three twenty")
                if used and current % 100 != 0 and current != 0:
                    break
                if used and current == 0 and total == 0:
                    break
                current += v
                used = True
                k += 1
                continue
            if w in SCALES and used:
                if SCALES[w] == 100:
                    current = (current or 1) * 100
                else:
                    total += (current or 1) * SCALES[w]
                    current = 0
                k += 1
                # "two hundred and forty": an "and" right after a scale joins the rest
                if k < n and words[k].lower() == "and" and k + 1 < n and _small(words[k + 1]) is not None:
                    k += 1
                continue
            if w in ORD_SCALES and used:
                if ORD_SCALES[w] == 100:
                    current = (current or 1) * 100
                else:
                    total += (current or 1) * ORD_SCALES[w]
                    current = 0
                return k + 1, total + current, True
            if used and (total + current) >= 100:
                o = _small_ord(w)
                if o is not None:
                    return k + 1, total + current + o, True
            break
        if not used:
            return None
        return k, total + current, False

    w0 = words[i].lower()
    # "the hundredth zero", "the hundred and fifty-ninth zero"
    if w0 in ORD_SCALES:
        return i + 1, f"{ORD_SCALES[w0]}th", "ord", ORD_SCALES[w0]
    if w0 == "hundred" and i + 2 < n and words[i + 1].lower() == "and" and _small_ord(words[i + 2]) is not None:
        v = 100 + _small_ord(words[i + 2])
        return i + 3, f"{v}{_suffix(v)}", "ord", v
    if w0 == "a" and i + 1 < n and words[i + 1].lower() in ("hundred", "thousand", "million"):
        # "a hundred and sixty-seven": read the "a" as "one"
        r = _parse(["one"] + list(words[i + 1:]), 0)
        if r is not None:
            end, txt, kind, value = r
            return i + end, txt, kind, value
    r = integer(i)
    if r is None:
        return None
    end, value, is_ord = r
    if is_ord:
        return end, f"{value}{_suffix(value)}", "ord", value
    # "one sixty-nine", "eight forty-one": a digit followed straight away by a two-digit number is one number
    if 1 <= value <= 9 and end == i + 1 and end < n:
        v2 = _small(words[end])
        if v2 is not None and v2 >= 10 and (end + 1 >= n or words[end + 1].lower() not in SCALES):
            return end + 1, str(value * 100 + v2), "int", value * 100 + v2
    # years and "forty-seven seventy": a two-digit number followed straight away by another, or by "oh" and a digit
    if 10 <= value <= 99 and end < n:
        nxt = words[end].lower()
        if nxt == "oh" and end + 1 < n and words[end + 1].lower() in UNITS:
            return end + 2, str(value * 100 + UNITS[words[end + 1].lower()]), "year", value * 100
        v2 = _small(nxt)
        if v2 is not None and v2 >= 10 and (end + 1 >= n or words[end + 1].lower() not in SCALES):
            return end + 1, str(value * 100 + v2), "year", value * 100 + v2
    # decimals: "fourteen point one three", "point one one five" handled by the caller
    if end + 1 < n and words[end].lower() == "point" and words[end + 1].lower() in UNITS:
        k, digits = end + 1, ""
        while k < n and words[k].lower() in UNITS:
            digits += str(UNITS[words[k].lower()])
            k += 1
        return k, f"{_fmt(value)}.{digits}", "dec", value
    return end, _fmt(value), "int", value


def _suffix(v):
    if 10 <= v % 100 <= 20:
        return "th"
    return {1: "st", 2: "nd", 3: "rd"}.get(v % 10, "th")


TOKEN = re.compile(r"^([(\[\"“]*)(.*?)([.,;:!?)\]\"”]*)$")


def auto_markup(text):
    """Mark the numbers and formulas written out in words (outside any existing marks) so the caption shows print."""
    out, pos = [], 0
    for m in GROUP.finditer(text):
        out.append(_auto_segment(text[pos:m.start()]))
        out.append(m.group(0))
        pos = m.end()
    out.append(_auto_segment(text[pos:]))
    return "".join(out)


def _auto_segment(text):
    """The word rules on a stretch with no marks in it, then the numbers on what the rules left unmarked."""
    text = re.sub(r"\bE eight('s)?\b", lambda m: "{E8" + (m.group(1) or "") + "|E eight" + (m.group(1) or "") + "}", text)
    text = re.sub(r"(?<![\w])([ntkm]) equal to\b", lambda m: "{" + m.group(1) + " =|" + m.group(1) + " equal to}", text)
    text = re.sub(r"\b(lambda|gamma)\b", lambda m: "{" + {"lambda": "λ", "gamma": "γ"}[m.group(1)] + "|" + m.group(1) + "}", text)
    num = r"((?:twenty|thirty|forty|fifty|sixty|seventy|eighty|ninety)(?:-(?:one|two|three|four|five|six|seven|eight|nine))?" \
          r"|one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve|thirteen|fourteen|fifteen|sixteen|seventeen|eighteen|nineteen)"
    rules = [
        (r"\b" + num + r" squared\b", lambda m: f"{_small(m.group(1))}²"),
        (r"\b" + num + r" cubed\b", lambda m: f"{_small(m.group(1))}³"),
        (r"\blog " + num + r"\b", lambda m: f"log {_small(m.group(1))}"),
        (r"\bone over (?:the square root of |root )(n|" + num[1:-1] + r")\b",
         lambda m: f"1/√{m.group(1) if m.group(1) == 'n' else _small(m.group(1))}"),
        (r"\bx cubed minus x minus one\b", lambda m: "x³ − x − 1"),
        (r"\bx squared plus x y plus six y squared\b", lambda m: "x² + xy + 6y²"),
    ]
    for pattern, shown in rules:
        text = _sub_outside(text, pattern, lambda m, shown=shown: "{" + shown(m) + "|" + m.group(0) + "}")
    out, pos = [], 0
    for m in GROUP.finditer(text):
        out.append(_mark_numbers(text[pos:m.start()]))
        out.append(m.group(0))
        pos = m.end()
    out.append(_mark_numbers(text[pos:]))
    return "".join(out)


def _sub_outside(text, pattern, repl):
    """re.sub on the stretches of text outside {display|spoken} marks."""
    out, pos = [], 0
    for m in GROUP.finditer(text):
        out.append(re.sub(pattern, repl, text[pos:m.start()]))
        out.append(m.group(0))
        pos = m.end()
    out.append(re.sub(pattern, repl, text[pos:]))
    return "".join(out)


def _mark_numbers(seg):
    if not seg.strip():
        return seg
    raw = seg.split(" ")
    # bare words with their punctuation split off
    parts = []
    for r in raw:
        m = TOKEN.match(r)
        parts.append((m.group(1), m.group(2), m.group(3)) if r else ("", "", ""))
    words = [p[1] for p in parts]
    found = []  # (start, end, text, kind, value)
    i = 0
    while i < len(words):
        if not words[i]:
            i += 1
            continue
        neg = words[i].lower() == "minus" and i + 1 < len(words) and not parts[i][2] \
            and not (i > 0 and len(words[i - 1]) == 1 and words[i - 1].isalpha() and not parts[i - 1][2])
        j = i + 1 if neg else i
        if j < len(words) and words[j].lower() == "point" and j + 1 < len(words) and words[j + 1].lower() in UNITS:
            k, digits = j + 1, ""
            while k < len(words) and words[k].lower() in UNITS and not parts[k - 1][2]:
                digits += str(UNITS[words[k].lower()])
                k += 1
            found.append((i, k, ("−" if neg else "") + "0." + digits, "dec", 0))
            i = k
            continue
        # "zero" alone is the noun ("the first zero"); as a number it only starts a decimal ("zero point three")
        zero_noun = words[j].lower() == "zero" and not (j + 1 < len(words) and words[j + 1].lower() == "point" and not parts[j][2])
        r = None if zero_noun else _parse_guarded(words, parts, j)
        if r is not None:
            end, txt, kind, value = r
            if neg:
                txt, kind = "−" + txt, "neg"
            # "one point four percent" → "1.4%"
            if end < len(words) and words[end].lower() == "percent" and not parts[end - 1][2]:
                txt += "%"
                end += 1
            found.append((i, end, txt, kind, value))
            i = end
            continue
        i += 1
    # which become digits: big ones, decimals, years, negatives, and any number naming an hour, a spoke or a value
    def named(f):
        s = f[0]
        if s == 0 or parts[s - 1][2]:
            return False  # "hours, one for each": a comma ends the naming
        before = [w.lower() for w in words[max(0, s - 2):s]]
        return before[-1] in ("hour", "hours", "spoke", "spokes", "floor", "shell") or before[-2:] == ["equal", "to"]
    conv = [f[3] in ("dec", "year", "neg") or (f[3] == "ord" and f[4] >= 11) or (f[3] == "int" and (f[4] >= 10 or named(f))) for f in found]
    changed = True
    while changed:
        changed = False
        for a in range(len(found)):
            if conv[a]:
                continue
            for b in (a - 1, a + 1):
                if 0 <= b < len(found) and conv[b] and found[b][3] != "ord" and found[a][3] != "ord" and _joined(parts, words, found, min(a, b), max(a, b)):
                    conv[a] = changed = True
                    break
    # rebuild the text
    for (s, e, txt, kind, value), c in sorted(zip(found, conv), key=lambda x: -x[0][0]):
        if not c:
            continue
        lead, trail = parts[s][0], parts[e - 1][2]
        spoken = " ".join((parts[k][0] if k > s else "") + parts[k][1] + (parts[k][2] if k < e - 1 else "") for k in range(s, e))
        raw[s:e] = [f"{lead}{{{txt}|{spoken}}}{trail}"]
    return " ".join(raw)


def _parse_guarded(words, parts, j):
    """_parse, but a number phrase never runs across punctuation ("fifty-three, twenty-seven")."""
    if j >= len(words) or not words[j]:
        return None
    k = j
    while k < len(words) and not parts[k][2] and parts[k][1]:
        k += 1
    stop = min(len(words), k + 1)
    r = _parse(words[:stop], j)
    return r


def _joined(parts, words, found, a, b):
    """Two number phrases joined only by a comma or a joining word ("seven, eleven", "one and nineteen"); ", and" starts
    a new clause ("ten million, and two of the spokes"), not a list."""
    ea, sb = found[a][1], found[b][0]
    between = [w.lower() for w in words[ea:sb]]
    if any(w not in JOINERS for w in between):
        return False
    if parts[ea - 1][2].startswith(",") and between and between[0] in ("and", "or"):
        return False
    return parts[ea - 1][2].startswith(",") or bool(between)


if __name__ == "__main__":
    tests = [
        "We begin with the plainest picture of them, the number line: one dot for each number, the primes in gold. Two, three, five, seven, eleven, thirteen.",
        "Voronoi found in nineteen oh four that the count of divisors has waves of its own.",
        "The first zero, at height fourteen point one three, adds a single cosine. Its crests tick on the logarithm of n: at seven point four, eleven point five, eighteen and twenty-eight, each about one and a half times the last.",
        "Higher still, at the hundred and fifty-ninth zero, three hundred thirty-three point six, the path curls into vortices.",
        "They are ahead of their share only zero point three and one point four percent of the way. Below, six thousand seven hundred and seventy-one zeros of E eight's L-functions rebuild every curve.",
        "Near n equal to forty-seven seventy, each arrow points at five times its hour: four land on hour five and four on hour twenty-five.",
        "And at the very end, near n equal to twenty-three thousand eight hundred and eighty, each arrow points at its own hour: minus one.",
        "minus one, one, two, four, minus two, minus four, minus eight, eight.",
        "Seventeen degrees, then thirteen, ten, nine and eight, because the logarithm climbs more and more slowly.",
        "Add the waves of more zeros: ten, a hundred, a thousand, ten thousand.",
        "on average they lean towards minus one, by minus point one one five, exactly as Landau's formula predicts",
        "Chebyshev noticed the bias in eighteen fifty-three; Davenport and Heilbronn proved that in nineteen thirty-six.",
        "Seven is prime: one plus three hundred and forty-three, times {240|two forty}, makes eighty-two thousand five hundred and sixty.",
        "Two of the spokes, one and nineteen, run behind nearly all the way. Race them up to ten million.",
        "Let k run from one to thirty, and the chain dances.",
        "Each vortex has an eye: the path's own k-th vertex. E eight has two hundred and forty roots.",
        "eleven times thirteen times a hundred and sixty-seven, but on the clock it is simply hour one. the gold hours, one for each hour.",
        "each turning from the last by t times the logarithm of n over n minus one.",
    ]
    for t in tests:
        m = auto_markup(t)
        print(display_of(m))
        assert spoken_of(m) == spoken_of(t), (spoken_of(m), t)
