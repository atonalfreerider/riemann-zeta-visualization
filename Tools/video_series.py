#!/usr/bin/env python3
"""
Build the long-form horizontal video segments (chapters xs… in Assets/Resources/Narration/experiments.json) from the
spec in Tools/video_series_spec.py, and time their cues to the narration.

An action written "@word|exp:…" runs when the narrator says that word: "@seven|exp:callnum:7:…" becomes
"delay:T:exp:callnum:7:…", with T the start of "seven" in the line's ElevenLabs word timing
(Assets/Resources/Narration/Timing/<id>.json). "@seven#2|…" is its second occurrence, "@3.5|…" is 3.5 s into the line.
Before the line is narrated, T is estimated from the text as the tour estimates it (TourScriptLoader.Estimate), so the
module can be storyboarded first and re-timed after narration by running this again.

  python3 Tools/video_series.py            # write the chapters (replacing earlier versions of the same ids)
  python3 Tools/video_series.py --check    # print the cue times, write nothing
  python3 Tools/video_series.py --doc      # also write the scripts to docs/experiments/long-form-script.md
"""
import argparse, json, re, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SCRIPT = ROOT / "Assets/Resources/Narration/experiments.json"
TIMING = ROOT / "Assets/Resources/Narration/Timing"
sys.path.insert(0, str(Path(__file__).resolve().parent))
from video_series_spec import SERIES  # noqa: E402
from caption_markup import auto_markup, spoken_of, display_of  # noqa: E402

def norm(w):
    return re.sub(r"[^a-z0-9]", "", w.lower())


def word_times(line_id, text):
    """(word, start) for every word of the line: from its timing JSON when it has one, else estimated."""
    t = timing_for(line_id, text)
    if t is not None:
        return [(norm(w["w"]), w["s"]) for w in t["words"]], True
    return [(norm(w), st) for w, st, _ in estimate(text)], False


def timing_for(line_id, text):
    """The line's narration timing, if it was narrated from this very text (a changed line falls back to the estimate)."""
    path = TIMING / f"{line_id}.json"
    if not path.exists():
        return None
    t = json.loads(path.read_text(encoding="utf-8"))
    said = [norm(w["w"]) for w in t["words"]]
    wrote = [norm(w) for w in text.split()]
    return t if [w for w in said if w] == [w for w in wrote if w] else None


def estimate(text):
    """(word, start, end) as the tour estimates an un-narrated line (TourScriptLoader.Estimate), so a storyboard made
    before the narration fires each cue where the caption shows its word."""
    out, t = [], 0.15
    for raw in text.split():
        length = 0.17 + 0.032 * len(raw)
        out.append((raw, t, t + length))
        t += length
        if raw[-1] in ".!?":
            t += 0.5
        elif raw[-1] in ",;:":
            t += 0.22
    return out


def resolve(line_id, text, action):
    """'@word#k|act' → 'delay:T:act'; plain actions pass through."""
    if not action.startswith("@"):
        return action, None
    when, act = action[1:].split("|", 1)
    try:
        return f"delay:{float(when):.2f}:{act}", float(when)
    except ValueError:
        pass
    # 'word#k+0.3': a shift after (or before, with -) the word
    m = re.match(r"^(.*?)([+-]\d+(?:\.\d+)?)?$", when)
    when, shift = m.group(1), float(m.group(2) or 0)
    t = max(0.0, word_time(line_id, text, when) + shift)
    return f"delay:{t:.2f}:{act}", t


def line_seconds(line_id, text, pause):
    """How long the line runs: its narration (timing JSON) or the estimate, plus its pause."""
    t = timing_for(line_id, text)
    if t is not None:
        return float(t.get("duration") or (t["words"][-1]["e"] if t["words"] else 0.0)) + pause
    words = estimate(text)
    return (words[-1][2] + 0.3 if words else 0.0) + pause


def camera(cam, az_from):
    """A camera dict → an orbit cue. az "+" continues the previous line's spin from where it stopped (az_from)."""
    if isinstance(cam, str):
        return cam
    c = dict(cam)
    if c.get("az") == "+":
        c["az"] = round(az_from, 2)
    order = ["on", "at", "fit", "d", "el", "az", "spin", "raise", "fov", "span", "hold", "h", "follow"]
    parts = []
    for k in order:
        if k in c and c[k] is not None:
            parts.append(f"{k}={c[k]}")
    return "orbit:" + ";".join(parts)


def word_time(line_id, text, spec):
    """The time of a word ('word', 'word#k', either with '+s' or '-s' after it) in the line, a beat early, as resolve() uses."""
    m = re.match(r"^(.*?)([+-]\d+(?:\.\d+)?)?$", spec)
    spec, shift = m.group(1), float(m.group(2) or 0)
    if shift:
        return max(0.0, word_time(line_id, text, spec) + shift)
    word, _, k = spec.partition("#")
    k = int(k) if k else 1
    times, _ = word_times(line_id, text)
    hits = [s for w, s in times if w == norm(word)]
    if len(hits) < k:
        raise SystemExit(f"{line_id}: no occurrence {k} of '{word}' in: {text}")
    return max(0.0, hits[k - 1] - 0.15)


def placeholders(line_id, text, action):
    """'{word#k}' inside an action → that word's time in seconds from the line's start (e.g. the hours of exp:e8walk);
    '{a~b}' → the time from word a to word b (a duration that ends on b); '{a~end}' → from word a to the line's end."""
    def one(m):
        body = m.group(1)
        if "~" in body:
            a, b = body.split("~", 1)
            ta = word_time(line_id, text, a)
            tb = (line_seconds(line_id, text, 0.0) - 0.2) if b == "end" else word_time(line_id, text, b)
            return f"{max(0.3, tb - ta):.2f}"
        return f"{word_time(line_id, text, body):.2f}"
    return re.sub(r"\{([^}]+)\}", one, action)


def build():
    chapters = []
    for ch in SERIES:
        lines = []
        az_end = -90.0
        for i, ln in enumerate(ch["lines"], 1):
            lid = f"{ch['id']}_{i:02d}"
            # the line as written may mark {display|spoken} stretches; numbers said in words are marked automatically
            markup = auto_markup(ln["text"])
            text = spoken_of(markup)
            acts = []
            for a in ln.get("actions", []):
                r, _ = resolve(lid, text, placeholders(lid, text, a))
                acts.append(r)
            pause = ln.get("pause", 0.8)
            cam = ln["camera"]
            if isinstance(cam, dict):
                # az_to: turn at the steady rate that reaches that azimuth as the line ends
                cam = dict(cam)
                az0 = float(az_end if cam.get("az") == "+" else cam.get("az", -90))
                secs = line_seconds(lid, text, pause)
                if "az_to" in cam:
                    cam["spin"] = round((float(cam.pop("az_to")) - az0) / max(1.0, secs), 3)
                cue = camera(cam, az_end)
                az_end = az0 + float(cam.get("spin", 3)) * secs
            else:
                cue = camera(cam, az_end)
            if isinstance(cue, str) and "{" in cue:
                cue = placeholders(lid, text, cue)
            entry = {"id": lid, "text": text, "camera": cue, "actions": acts, "pause": pause}
            if markup != text:
                entry["caption"] = markup
            lines.append(entry)
        chapters.append({"id": ch["id"], "title": ch["title"], "subtitle": ch["subtitle"], "setup": ch.get("setup", []), "lines": lines})
    return chapters


DOC = ROOT / "docs/experiments/long-form-script.md"


def board_of(actions):
    """The caption board a line sets (exp:say:TITLE|SUB), as markdown."""
    for a in actions:
        act = a.split(":", 2)[2] if a.startswith("delay:") else a
        if act.startswith("exp:say:"):
            title, _, sub = act[len("exp:say:"):].partition("|")
            plain = lambda t: re.sub(r"<sup>(.*?)</sup>", r"^\1", re.sub(r"<sub>(.*?)</sub>", r"_\1", t))
            return f"**{plain(title)}**" + (f" — {plain(sub)}" if sub else "")
    return ""


def write_doc(chapters):
    """The scripts of all the segments, with each line's board and callouts, for reading and review."""
    out = ["# Long-form segments: scripts", "",
           "The long-form segments, recorded 1920 × 1080 and 1080 × 1920: the five discoveries (xs1 … xs5), the zeros inside "
           "E8 (xz1 … xz5) and the twist on the twist (xorbit). This file is written by `python3 Tools/video_series.py --doc` from "
           "[`Tools/video_series_spec.py`](../../Tools/video_series_spec.py), which also writes the chapters into "
           "[`experiments.json`](../../Assets/Resources/Narration/experiments.json). Narrate a segment with "
           "`Tools/generate_narration.py --script experiments --only xs1`, re-time its cues to the narration with "
           "`Tools/video_series.py`, and record it with `Tools/render_chapters.py --script experiments --chapters xs1 "
           "--shapes horizontal`.", ""]
    for ch in chapters:
        words = sum(len(l["text"].split()) for l in ch["lines"])
        out += [f"## {ch['id']}: {ch['title']}", "", f"*{ch['subtitle']}*  ·  {len(ch['lines'])} lines, {words} words", ""]
        for i, ln in enumerate(ch["lines"], 1):
            b = board_of(ln["actions"])
            calls = []
            for a in ln["actions"]:
                act = a.split(":", 2)[2] if a.startswith("delay:") else a
                if act.startswith("exp:call"):
                    calls.append(act.split(":", 4)[4] if act.count(":") >= 4 else act)
            out.append(f"{i}. {display_of(ln.get('caption', ln['text']))}")
            if b:
                out.append(f"   *Board:* {b}")
            if calls:
                out.append("   *Callouts:* " + "; ".join(re.sub(r"<sup>(.*?)</sup>", r"^\1", c) for c in calls))
        out.append("")
    DOC.write_text("\n".join(out), encoding="utf-8")
    print(f"wrote {DOC.relative_to(ROOT)}")


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--check", action="store_true")
    ap.add_argument("--doc", action="store_true", help="also write docs/experiments/long-form-script.md")
    ap.add_argument("--stale", action="store_true",
                    help="list the lines whose narration was recorded from other words; with --delete, remove those clips")
    ap.add_argument("--delete", action="store_true")
    args = ap.parse_args()
    if args.stale:
        audio = ROOT / "Assets/Resources/Narration/Audio"
        for ch in SERIES:
            for i, ln in enumerate(ch["lines"], 1):
                lid = f"{ch['id']}_{i:02d}"
                has = (TIMING / f"{lid}.json").exists()
                if has and timing_for(lid, spoken_of(ln["text"])) is not None:
                    continue
                print(f"{lid}: {'changed' if has else 'not narrated'}")
                if args.delete and has:
                    for f in [audio / f"{lid}.mp3", audio / f"{lid}.mp3.meta", TIMING / f"{lid}.json", TIMING / f"{lid}.json.meta"]:
                        if f.exists():
                            f.unlink()
        return
    chapters = build()
    if args.doc:
        write_doc(chapters)
    if args.check:
        for ch in chapters:
            print(f"== {ch['id']}: {ch['title']}")
            for ln in ch["lines"]:
                _, real = word_times(ln["id"], ln["text"])
                ts = [a.split(":")[1] for a in ln["actions"] if a.startswith("delay:")]
                print(f"  {ln['id']} ({'narrated' if real else 'estimated'}): {len(ln['text'].split())} words, cues at {', '.join(ts)}")
        return
    raw = SCRIPT.read_text(encoding="utf-8")
    d = json.loads(raw)
    ids = {c["id"] for c in chapters}
    kept = [c for c in d["chapters"] if c["id"] not in ids]
    d["chapters"] = kept + chapters
    SCRIPT.write_text(json.dumps(d, indent=2, ensure_ascii=False) + ("\n" if raw.endswith("\n") else ""), encoding="utf-8")
    print(f"wrote {len(chapters)} chapters ({sum(len(c['lines']) for c in chapters)} lines) into {SCRIPT.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
