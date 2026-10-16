#!/usr/bin/env python3
"""
Fade out the end of each narration clip, exponentially, where its speech ends.

ElevenLabs is given the neighbouring lines as context (previous_text / next_text), and a clip often ends with a piece of
the next line: the last word, a short silence or dip, then 20 to 100 ms of the next sound, cut off while it is still
loud. This finds where the speech ends and fades out from there with an exponential curve (−60 dB over FADE seconds,
linear in decibels), leaving silence to the end. The clip keeps its length, so word timings and recorded timelines still
line up.

  python3 Tools/fade_narration.py --script experiments --only xzeros    # one chapter (or one line id)
  python3 Tools/fade_narration.py --script script                        # the tour
  python3 Tools/fade_narration.py --script experiments --check           # report what it would do, change nothing

A processed clip is marked in its timing JSON ("faded") and skipped the next time. generate_narration.py calls
fade_clip() on every clip it writes. The originals stay in git history.
"""
import argparse
import array
import json
import math
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
NARRATION = ROOT / "Assets/Resources/Narration"
AUDIO_DIR = NARRATION / "Audio"
TIMING_DIR = NARRATION / "Timing"
SR = 44100
WIN = 0.01        # envelope window, s
TAIL = 0.25       # how far back from the end a cut-off sound is looked for, s
QUIET = 0.08      # a dip below this share of the clip's peak can end the speech
SILENT = 0.03     # below this share of the peak a window counts as silence
FADE = 0.10       # length of the exponential fade, s
MIN_FADE = 0.04   # every clip ends with at least this fade, so none ends on a click
LOUD_FADE = 0.06  # the fade when the last word is still sounding at the end of the clip


def decode(path):
    raw = subprocess.run(["ffmpeg", "-v", "error", "-i", str(path), "-ac", "1", "-ar", str(SR), "-f", "s16le", "-"],
                         capture_output=True, check=True).stdout
    a = array.array("h")
    a.frombytes(raw)
    return a


def envelope(a):
    w = int(WIN * SR)
    env = []
    for k in range(0, len(a), w):
        seg = a[k:k + w]
        env.append(math.sqrt(sum(v * v for v in seg) / max(1, len(seg))))
    peak = max(env) or 1.0
    return [v / peak for v in env]


def speech_end(a):
    """(time where the fade starts, why). Two kinds of cut-off sound are found at the end of a clip:
    a short piece of sound that runs into the end after a silence (the next line's first sound), and a rise to a loud end
    after a quiet dip. A short sound that dies away before the end is left alone: it is a word's last consonant released
    after its closure (the t of "height", the p of "map")."""
    env = envelope(a)
    n = len(env)
    k = n - 1
    while k > 0 and env[k] < SILENT:
        k -= 1
    last = k                       # the last window with sound
    while k > 0 and env[k] >= SILENT:
        k -= 1
    first = k + 1                  # where that last sound starts
    g = k
    while g > 0 and env[g] < SILENT:
        g -= 1
    gap, piece = (k - g) * WIN, (last - first + 1) * WIN
    if last >= n - 2 and gap >= .05 and piece < .25:
        return (g + 1) * WIN, f"a {1000 * piece:.0f} ms piece of sound at the end, after {1000 * gap:.0f} ms of silence"
    tail = max(1, int(TAIL / WIN))
    m = min(range(max(0, n - tail), n), key=lambda i: (env[i], -i))   # the quietest window of the tail, the last if tied
    end_level = max(env[-2:])
    if m < n - 2 and env[m] < QUIET and end_level > max(2.5 * env[m], .1):
        return m * WIN, f"a rise to {100 * end_level:.0f}% of peak at the end, after a dip to {100 * env[m]:.0f}%"
    if end_level > .1:
        # the last word itself still sounds at the cut: only a short fade, so the click goes and the syllable stays
        return max(0.0, n * WIN - LOUD_FADE), f"ends loud ({100 * end_level:.0f}% of peak)"
    return max(0.0, n * WIN - MIN_FADE), "ends quietly"


def fade_clip(mp3, check=False):
    """Fade one clip in place (an mp3 in AUDIO_DIR). Returns a line describing what was done."""
    mp3 = Path(mp3)
    timing_path = TIMING_DIR / f"{mp3.stem}.json"
    timing = json.loads(timing_path.read_text(encoding="utf-8")) if timing_path.exists() else None
    if timing and timing.get("faded"):
        return f"{mp3.stem}: already faded"
    a = decode(mp3)
    dur = len(a) / SR
    start, why = speech_end(a)
    length = LOUD_FADE if why.startswith("ends loud") else MIN_FADE if why.startswith("ends quietly") else FADE
    if check:
        return f"{mp3.stem}: {dur:.2f} s, fade from {start:.2f} s over {length:.2f} s — {why}"
    k0 = int(start * SR)
    tau = length / math.log(1000.0)                      # −60 dB at the end of the fade
    for k in range(k0, len(a)):
        t = (k - k0) / SR
        a[k] = int(a[k] * math.exp(-t / tau)) if t < length else 0
    with tempfile.TemporaryDirectory() as tmp:
        wav = Path(tmp) / "clip.wav"
        out = Path(tmp) / "clip.mp3"
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-f", "s16le", "-ar", str(SR), "-ac", "1", "-i", "-", str(wav)],
                       input=a.tobytes(), check=True)
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", str(wav), "-c:a", "libmp3lame", "-b:a", "128k", "-ar", str(SR),
                        "-ac", "1", str(out)], check=True)
        mp3.write_bytes(out.read_bytes())
    if timing is not None:
        timing["faded"] = {"from": round(start, 3), "over": round(length, 3)}
        timing_path.write_text(json.dumps(timing, indent=1), encoding="utf-8")
    return f"{mp3.stem}: faded from {start:.2f} s over {length:.2f} s — {why}"


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--script", default="script", help="script (the tour, default) or experiments")
    parser.add_argument("--only", help="chapter id or line id")
    parser.add_argument("--check", action="store_true", help="report only")
    args = parser.parse_args()
    script = json.loads((NARRATION / f"{args.script}.json").read_text(encoding="utf-8"))
    ids = [l["id"] for c in script["chapters"] for l in c["lines"] if not args.only or args.only in (c["id"], l["id"])]
    if not ids:
        sys.exit(f"nothing matches --only {args.only}")
    for i in ids:
        mp3 = AUDIO_DIR / f"{i}.mp3"
        print(fade_clip(mp3, args.check) if mp3.exists() else f"{i}: no clip", flush=True)


if __name__ == "__main__":
    main()
