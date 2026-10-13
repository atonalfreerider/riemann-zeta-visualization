#!/usr/bin/env python3
"""
Render the tour narration locally with the machine's Chatterbox install ("narrate") in John's voice.

  python Tools/generate_narration_chatterbox.py                 # every missing line
  python Tools/generate_narration_chatterbox.py --only qrh      # one chapter id or one line id
  python Tools/generate_narration_chatterbox.py --force         # re-render existing lines
  python Tools/generate_narration_chatterbox.py --no-render     # only re-align and re-normalise the existing renders
  python Tools/generate_narration_chatterbox.py --dry-run

Three stages per line id:

 1. render    narrate "<text>" --exaggeration 0.25 -o Tools/narration_raw/<id>.wav          (untouched Chatterbox output)
 2. align     Tools/align_narration.py (Whisper, run with the narrator install's Python) checks the speech against the
              script and writes Assets/Resources/Narration/Timing/<id>.json with real per-word times for the captions.
              Without Whisper it falls back to timing the words between the pauses ffmpeg finds in the audio.
 3. normalise a gentle compressor, then gain to -18 LUFS and a peak limiter, written to
              Assets/Resources/Narration/Audio/<id>.wav (Chatterbox renders come out around -28 LUFS, far too quiet
              for a voice-over; the raw files are kept so this stage can be repeated).
"""
import argparse
import json
import os
import re
import shutil
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SCRIPT = ROOT / "Assets/Resources/Narration/script.json"
RAW_DIR = ROOT / "Tools/narration_raw"
AUDIO_DIR = ROOT / "Assets/Resources/Narration/Audio"
TIMING_DIR = ROOT / "Assets/Resources/Narration/Timing"

EXAGGERATION = "0.25"
TARGET_LUFS = -18.0
SILENCE_DB = -36      # fallback aligner: below this is silence
SILENCE_MIN = 0.11    # fallback aligner: seconds of silence that count as a pause
NOMINAL_PAUSE = {",": 0.16, ";": 0.24, ":": 0.24, ".": 0.42, "?": 0.42, "!": 0.42}
COMPRESSOR = "acompressor=threshold=-26dB:ratio=2.5:attack=6:release=100:makeup=2dB"


def narrator_home():
    return Path(os.environ.get("NARRATOR_HOME", Path.home() / ".narrator"))


def narrate_command():
    """argv prefix + environment that run `narrate` exactly like the launcher on PATH, without going through
    cmd.exe (whose quoting would mangle the text). Falls back to whatever `narrate` resolves to."""
    home = narrator_home()
    python = home / "env" / ("python.exe" if os.name == "nt" else "bin/python")
    if python.exists():
        env = dict(os.environ, NARRATOR_HOME=str(home), PYTHONPATH=str(home / "app"), PYTHONUTF8="1",
                   PYTHONIOENCODING="utf-8", TOKENIZERS_PARALLELISM="false", HF_HUB_DISABLE_SYMLINKS_WARNING="1")
        return [str(python), "-m", "narrator"], env
    return [shutil.which("narrate")], None


# ---- fallback aligner (no Whisper): words between the pauses ffmpeg finds ----------------------------------

def syllables(word):
    letters = re.sub(r"[^a-z]", "", word.lower())
    if not letters:
        return 1
    n = len(re.findall(r"[aeiouy]+", letters))
    if letters.endswith("e") and not letters.endswith(("le", "ee")) and n > 1:
        n -= 1
    return max(1, n)


def weight(word):
    return syllables(word) + 0.07 * len(word)


def probe_duration(path):
    out = subprocess.run(
        ["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "default=nw=1:nk=1", str(path)],
        capture_output=True, text=True, check=True).stdout.strip()
    return float(out)


def detect_silences(path, duration):
    proc = subprocess.run(
        ["ffmpeg", "-hide_banner", "-nostats", "-i", str(path),
         "-af", f"silencedetect=noise={SILENCE_DB}dB:d={SILENCE_MIN}", "-f", "null", "-"],
        capture_output=True, text=True)
    starts = [float(x) for x in re.findall(r"silence_start: (-?[\d.]+)", proc.stderr)]
    ends = [float(x) for x in re.findall(r"silence_end: (-?[\d.]+)", proc.stderr)]
    if len(starts) > len(ends):
        ends.append(duration)
    return [(max(0.0, s), min(duration, e)) for s, e in zip(starts, ends)]


def word_timings(text, duration, silences):
    words = text.split()
    n = len(words)
    w = [weight(x) for x in words]
    v0, v1 = 0.0, duration
    inner = []
    for s, e in silences:
        if s <= 0.02:
            v0 = e
        elif e >= duration - 0.02:
            v1 = s
        else:
            inner.append((s, e))
    if v1 - v0 < 0.3:
        v0, v1 = 0.0, duration
    bounds = [(i, NOMINAL_PAUSE[words[i][-1]]) for i in range(n - 1) if words[i][-1] in NOMINAL_PAUSE]
    total_p = sum(p for _, p in bounds)
    u = max(1e-3, (v1 - v0 - total_p) / sum(w))
    pred, cw, cp, k = [], 0.0, 0.0, 0
    for i in range(n):
        cw += w[i]
        if k < len(bounds) and bounds[k][0] == i:
            pred.append(v0 + cw * u + cp + bounds[k][1] / 2)
            cp += bounds[k][1]
            k += 1
    matched, last = {}, -1
    for k in range(len(bounds)):
        best, best_d = None, 0.7
        for j in range(last + 1, len(inner)):
            c = (inner[j][0] + inner[j][1]) / 2
            d = abs(c - pred[k])
            if d < best_d:
                best, best_d = j, d
            elif c > pred[k] + 0.7:
                break
        if best is not None:
            matched[k] = inner[best]
            last = best
    anchors = [(-1, v0, v0)] + [(bounds[k][0], s, e) for k, (s, e) in sorted(matched.items())] + [(n - 1, v1, v1)]
    timings = [None] * n
    for a in range(len(anchors) - 1):
        ia, _, ta = anchors[a]
        ib, tb, _ = anchors[a + 1]
        idx = list(range(ia + 1, ib + 1))
        if not idx:
            continue
        inside = [(bi, p) for (bi, p) in bounds if ia < bi < ib]
        span = max(0.05, tb - ta - sum(p for _, p in inside))
        unit = span / sum(w[i] for i in idx)
        t = ta
        for i in idx:
            timings[i] = {"w": words[i], "s": round(t, 3), "e": round(t + w[i] * unit, 3)}
            t += w[i] * unit
            for bi, p in inside:
                if bi == i:
                    t += p
    return [t for t in timings if t is not None]


# ---- stages -------------------------------------------------------------------------------------------------

def render(line, wav, exaggeration):
    prefix, env = narrate_command()
    cmd = prefix + [line["text"], "--exaggeration", exaggeration, "-o", str(wav)]
    t0 = time.time()
    proc = subprocess.run(cmd, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if proc.returncode != 0 or not wav.exists():
        sys.exit(f"narrate failed for {line['id']}:\n{proc.stdout}\n{proc.stderr}")
    return time.time() - t0


def align_with_whisper(only):
    python = narrator_home() / "env" / ("python.exe" if os.name == "nt" else "bin/python")
    cache = Path.home() / ".cache/huggingface/hub/models--openai--whisper-small.en"
    if not python.exists() or not cache.exists():
        return False
    cmd = [str(python), str(ROOT / "Tools/align_narration.py"), "--raw", str(RAW_DIR)]
    if only:
        cmd += ["--only", only]
    env = dict(os.environ, PYTHONUTF8="1", PYTHONIOENCODING="utf-8", HF_HUB_OFFLINE="1")
    proc = subprocess.run(cmd, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
    for line in proc.stdout.splitlines():
        if re.match(r"^\S+\s+WER|^\s+(script|heard)|^mean WER|nothing recognised", line):
            print(line)
    return proc.returncode == 0


def align_with_pauses(line, wav):
    duration = probe_duration(wav)
    timing = word_timings(line["text"], duration, detect_silences(wav, duration))
    (TIMING_DIR / f"{line['id']}.json").write_text(
        json.dumps({"words": timing, "duration": round(duration, 3)}, ensure_ascii=False, indent=1), encoding="utf-8")


def measure_lufs(path):
    out = subprocess.run(["ffmpeg", "-hide_banner", "-nostats", "-i", str(path), "-af", "ebur128=peak=true", "-f", "null", "-"],
                         capture_output=True, text=True).stderr
    return float(re.findall(r"I:\s+(-?[\d.]+) LUFS", out)[-1])


def normalise(raw, out, scratch):
    tmp = scratch / (raw.stem + "_c.wav")
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", str(raw), "-af", COMPRESSOR, str(tmp)], check=True)
    gain = TARGET_LUFS - measure_lufs(tmp)
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", str(tmp),
                    "-af", f"volume={gain:.2f}dB,alimiter=limit=0.85:attack=2:release=40:level=disabled",
                    "-ar", "24000", "-ac", "1", "-c:a", "pcm_s16le", str(out)], check=True)
    tmp.unlink()
    return gain


def lines_of(script):
    seen = set()
    for chapter in script["chapters"]:
        for line in chapter["lines"]:
            if line["id"] in seen:
                continue
            seen.add(line["id"])
            yield chapter["id"], line


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--only", help="chapter id or line id")
    parser.add_argument("--force", action="store_true", help="re-render lines that already have a raw wav")
    parser.add_argument("--no-render", action="store_true", help="skip rendering; re-align and re-normalise existing raw wavs")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--exaggeration", default=EXAGGERATION)
    args = parser.parse_args()

    for tool in ("narrate", "ffmpeg", "ffprobe"):
        if not shutil.which(tool):
            sys.exit(f"{tool} not found on PATH")
    if args.exaggeration != EXAGGERATION:
        print(f"note: using --exaggeration {args.exaggeration} instead of the project default {EXAGGERATION}")

    script = json.loads(SCRIPT.read_text(encoding="utf-8"))
    todo = [(c, l) for c, l in lines_of(script) if not args.only or args.only in (c, l["id"])]
    if not todo:
        sys.exit(f"nothing matches --only {args.only}")
    for d in (RAW_DIR, AUDIO_DIR, TIMING_DIR):
        d.mkdir(parents=True, exist_ok=True)

    started = time.time()
    rendered = 0
    for n, (chapter_id, line) in enumerate(todo, 1):
        raw = RAW_DIR / f"{line['id']}.wav"
        need = args.force or not raw.exists()
        words = len(line["text"].split())
        print(f"[{n:2}/{len(todo)}] {chapter_id:9} {line['id']:10} {words:3} words  {'render' if need and not args.no_render else 'keep'}", flush=True)
        if args.dry_run:
            continue
        if args.no_render and not raw.exists():
            sys.exit(f"{raw} does not exist; run without --no-render")
        if need and not args.no_render:
            seconds = render(line, raw, args.exaggeration)
            rendered += 1
            print(f"          rendered in {seconds:.1f}s", flush=True)
    if args.dry_run:
        return

    print("aligning words...", flush=True)
    if not align_with_whisper(args.only):
        print("  Whisper not available; timing words between detected pauses instead")
        for _, line in todo:
            align_with_pauses(line, RAW_DIR / f"{line['id']}.wav")

    print("normalising loudness...", flush=True)
    scratch = RAW_DIR
    total = 0.0
    for _, line in todo:
        out = AUDIO_DIR / f"{line['id']}.wav"
        gain = normalise(RAW_DIR / f"{line['id']}.wav", out, scratch)
        total += probe_duration(out)
        print(f"  {line['id']:10} {gain:+5.1f} dB", flush=True)
    print(f"done: {total / 60:.1f} min of audio, {rendered} line(s) rendered, {(time.time() - started) / 60:.1f} min")


if __name__ == "__main__":
    main()
