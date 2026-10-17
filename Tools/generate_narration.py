#!/usr/bin/env python3
"""
Generate narration audio + word timings for the tour with ElevenLabs.

  export ELEVENLABS_API_KEY=...          # never passed on the command line
  python3 Tools/generate_narration.py --voice "John V4"            # all lines
  python3 Tools/generate_narration.py --voice "John V4" --only qrh # one chapter
  python3 Tools/generate_narration.py --dry-run                     # list what would be generated

Writes, per line id:
  Assets/Resources/Narration/Audio/<id>.mp3     (Unity imports it as an AudioClip)
  Assets/Resources/Narration/Timing/<id>.json   ({"words":[{"w","s","e"}...], "duration"})

The timing JSON is derived from the ElevenLabs character alignment returned by the
/with-timestamps endpoint, so the caption highlight follows the actual speech.
Existing files are skipped unless --force is given. Every new clip is faded out where its speech ends
(Tools/fade_narration.py): ElevenLabs often ends a clip with a cut-off piece of the next line's first sound.
"""
import argparse
import re
import base64
import json
import os
import sys
import time
from pathlib import Path

import requests

sys.path.insert(0, str(Path(__file__).resolve().parent))
from fade_narration import fade_clip  # noqa: E402  (the exponential fade-out at the end of each clip)

ROOT = Path(__file__).resolve().parent.parent
SCRIPT = ROOT / "Assets/Resources/Narration/script.json"
AUDIO_DIR = ROOT / "Assets/Resources/Narration/Audio"
TIMING_DIR = ROOT / "Assets/Resources/Narration/Timing"
API = "https://api.elevenlabs.io/v1"


def find_voice_id(session, name):
    r = session.get(f"{API}/voices", timeout=30)
    r.raise_for_status()
    voices = r.json().get("voices", [])
    for v in voices:
        if v.get("name", "").strip().lower() == name.strip().lower():
            return v["voice_id"]
    names = ", ".join(sorted(v.get("name", "?") for v in voices))
    sys.exit(f"Voice '{name}' not found. Available voices: {names}")


def words_from_alignment(text, alignment):
    """Collapse ElevenLabs character timings into word timings matching script.json's words."""
    chars = alignment["characters"]
    starts = alignment["character_start_times_seconds"]
    ends = alignment["character_end_times_seconds"]
    words = []
    current = []
    w_start = None
    w_end = None
    for ch, s, e in zip(chars, starts, ends):
        if ch.isspace():
            if current:
                words.append({"w": "".join(current), "s": round(w_start, 3), "e": round(w_end, 3)})
                current, w_start, w_end = [], None, None
            continue
        if w_start is None:
            w_start = s
        w_end = e
        current.append(ch)
    if current:
        words.append({"w": "".join(current), "s": round(w_start, 3), "e": round(w_end, 3)})

    # Re-map onto the script's own tokenisation so captions show exactly the script text.
    script_words = text.split()
    if len(script_words) != len(words):
        # fall back: distribute alignment proportionally over the script's words
        total_start = words[0]["s"] if words else 0.0
        total_end = words[-1]["e"] if words else 1.0
        span = max(total_end - total_start, 0.01)
        chars_total = sum(len(w) for w in script_words) or 1
        t = total_start
        out = []
        for w in script_words:
            d = span * len(w) / chars_total
            out.append({"w": w, "s": round(t, 3), "e": round(t + d, 3)})
            t += d
        return out
    return [{"w": sw, "s": tw["s"], "e": tw["e"]} for sw, tw in zip(script_words, words)]


# How names are said, spelled the way the voice reads them. The captions keep the script's own spelling: every
# replacement is a single word for a single word, so the word timings still line up with the script's words.
PRONOUNCE = [
    (re.compile(r"\bEuler"), "Oiler"),             # Leonhard Euler: "oil-er", never "you-ler"
    (re.compile(r"\b([Cc])osine"), r"\1o-sign"),  # "co-sign"
    (re.compile(r"\bVoronoi"), "Voronoy"),          # Georgy Voronoi: "vo-ro-NOY"
    (re.compile(r"\b([Pp])ronic"), r"\1ro-nick"),  # "PRO-nick"
    (re.compile(r"\bFermat"), "Fair-mah"),         # Pierre de Fermat: "fair-MAH", the t silent
    (re.compile(r"\bi t\b"), "eye tee"),           # ζ(½ + it): the letters, never the word "it"
]


def spoken(text):
    for pattern, said in PRONOUNCE:
        text = pattern.sub(said, text)
    return text


def synthesize(session, voice_id, text, model_id, previous_text, next_text, speed=1.0, stability=0.35, style=0.45):
    body = {
        "text": spoken(text),
        "model_id": model_id,
        # lower stability and more style give a livelier, less even read; speed < 1 slows it (0.7 to 1.2 accepted)
        "voice_settings": {"stability": stability, "similarity_boost": 0.8, "style": style, "use_speaker_boost": True, "speed": speed},
    }
    if previous_text:
        body["previous_text"] = spoken(previous_text)
    if next_text:
        body["next_text"] = spoken(next_text)
    for attempt in range(4):
        r = session.post(
            f"{API}/text-to-speech/{voice_id}/with-timestamps",
            params={"output_format": "mp3_44100_128"},
            json=body,
            timeout=120,
        )
        if r.status_code == 429:
            time.sleep(5 * (attempt + 1))
            continue
        r.raise_for_status()
        return r.json()
    r.raise_for_status()


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--voice", default="JohnV4", help="ElevenLabs voice name (default: JohnV4)")
    parser.add_argument("--model", default="eleven_multilingual_v2")
    parser.add_argument("--only", help="chapter id or line id to generate")
    parser.add_argument("--script", default="script",
                        help="which script in Assets/Resources/Narration: script (the tour, default) or experiments")
    parser.add_argument("--force", action="store_true", help="regenerate existing files")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--speed", type=float, default=0.95, help="delivery speed, 0.7 to 1.2 (default 0.95: unhurried, not dragging)")
    parser.add_argument("--stability", type=float, default=0.35, help="lower = more expressive (default 0.35)")
    parser.add_argument("--style", type=float, default=0.45, help="style exaggeration, 0 to 1 (default 0.45)")
    args = parser.parse_args()

    script_path = SCRIPT.with_name(f"{args.script}.json")
    script = json.loads(script_path.read_text(encoding="utf-8"))
    lines = []
    for chapter in script["chapters"]:
        for line in chapter["lines"]:
            lines.append((chapter["id"], line))

    selected = [(c, l) for c, l in lines if not args.only or args.only in (c, l["id"])]
    if not selected:
        sys.exit(f"nothing matches --only {args.only}")

    if args.dry_run:
        for c, l in selected:
            print(f"[{c}] {l['id']}: {l['text']}")
        print(f"{len(selected)} lines, {sum(len(l['text']) for _, l in selected)} characters")
        return

    key = os.environ.get("ELEVENLABS_API_KEY")
    if not key:
        sys.exit("Set ELEVENLABS_API_KEY in the environment first (do not put the key on the command line).")

    session = requests.Session()
    session.headers.update({"xi-api-key": key, "Content-Type": "application/json"})
    voice_id = find_voice_id(session, args.voice)
    print(f"voice '{args.voice}' → {voice_id}")

    AUDIO_DIR.mkdir(parents=True, exist_ok=True)
    TIMING_DIR.mkdir(parents=True, exist_ok=True)

    all_texts = [l["text"] for _, l in lines]
    for c, l in selected:
        mp3 = AUDIO_DIR / f"{l['id']}.mp3"
        timing = TIMING_DIR / f"{l['id']}.json"
        if mp3.exists() and timing.exists() and not args.force:
            print(f"skip {l['id']} (exists)")
            continue

        idx = all_texts.index(l["text"])
        previous_text = all_texts[idx - 1] if idx > 0 else None
        next_text = all_texts[idx + 1] if idx + 1 < len(all_texts) else None

        print(f"generating {l['id']} …", end=" ", flush=True)
        result = synthesize(session, voice_id, l["text"], args.model, previous_text, next_text, args.speed, args.stability, args.style)
        mp3.write_bytes(base64.b64decode(result["audio_base64"]))
        alignment = result.get("alignment") or result.get("normalized_alignment")
        words = words_from_alignment(l["text"], alignment)
        duration = words[-1]["e"] if words else 0.0
        timing.write_text(json.dumps({"words": words, "duration": round(duration + 0.25, 3)}, indent=1), encoding="utf-8")
        # a clip often ends with a piece of the next line's first sound (the next line is passed as context): fade it out
        print(f"{duration:.1f}s;", fade_clip(mp3).split(": ", 1)[1])

    print("done — open Unity so it imports the new clips, then play the tour.")


if __name__ == "__main__":
    main()
