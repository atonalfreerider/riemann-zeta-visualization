#!/usr/bin/env python3
"""
Listen back to the narration: transcribe each clip with ElevenLabs speech-to-text and compare what was heard with what
the script says, word by word, so misread or garbled words show up before a recording.

  export ELEVENLABS_API_KEY=...          # never passed on the command line
  python3 Tools/check_pronunciation.py --script experiments --only xs1      # one chapter (or a line id, or a prefix: x)

Numbers are compared as numbers ("fourteen point one three" and "14.13" agree), case and punctuation are ignored, and
the respellings the narration uses (Tools/generate_narration.py PRONOUNCE: Euler → Oiler) count as the word itself.
Each line prints OK or the differences; the transcripts are cached in Tools/.stt_cache.json so a rerun is free.
"""
import argparse, difflib, json, os, re, sys
from pathlib import Path
import requests

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(Path(__file__).resolve().parent))

API = "https://api.elevenlabs.io/v1/speech-to-text"
CACHE = ROOT / "Tools/.stt_cache.json"


NUMWORDS = {"zero", "oh", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve",
            "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen", "twenty", "thirty", "forty",
            "fifty", "sixty", "seventy", "eighty", "ninety", "hundred", "thousand", "million", "point", "a"}


def collapse(words):
    """Every number phrase ("two hundred and forty", "14.13", "fourteen point one three") becomes one token '#', so a
    number said and a number transcribed in digits agree; the words around them are compared as they are."""
    out, i = [], 0
    while i < len(words):
        w = words[i]
        isnum = w[0].isdigit() or (w in NUMWORDS and w != "a") or (w == "a" and i + 1 < len(words) and words[i + 1] in ("hundred", "thousand", "million"))
        if not isnum:
            out.append(w)
            i += 1
            continue
        j = i + 1
        while j < len(words):
            x = words[j]
            if x[0].isdigit() or (x in NUMWORDS and x != "a"):
                j += 1
            elif x == "and" and j + 1 < len(words) and words[j - 1] in ("hundred", "thousand") and (words[j + 1] in NUMWORDS or words[j + 1][0].isdigit()):
                j += 1
            else:
                break
        out.append("#")
        i = j
    return out


def words_of(text):
    text = re.sub(r"(\d),(\d)", r"\1\2", text).replace("-", " ")
    return [w.lower() for w in re.findall(r"[A-Za-z]+(?:'[a-z]+)?|\d+(?:\.\d+)?", text)]


def transcribe(session, mp3):
    with open(mp3, "rb") as f:
        r = session.post(API, data={"model_id": "scribe_v1", "language_code": "en"}, files={"file": (mp3.name, f, "audio/mpeg")}, timeout=180)
    r.raise_for_status()
    return r.json().get("text", "")


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--script", default="experiments")
    ap.add_argument("--only", default="x", help="chapter id, line id or id prefix")
    ap.add_argument("--refresh", action="store_true", help="transcribe again even if cached")
    args = ap.parse_args()
    key = os.environ.get("ELEVENLABS_API_KEY")
    if not key:
        sys.exit("ELEVENLABS_API_KEY is not set")
    script = json.loads((ROOT / f"Assets/Resources/Narration/{args.script}.json").read_text(encoding="utf-8"))
    cache = json.loads(CACHE.read_text()) if CACHE.exists() else {}
    session = requests.Session()
    session.headers["xi-api-key"] = key
    issues = 0
    for ch in script["chapters"]:
        for ln in ch["lines"]:
            lid = ln["id"]
            if not (lid == args.only or ch["id"] == args.only or lid.startswith(args.only)):
                continue
            mp3 = ROOT / f"Assets/Resources/Narration/Audio/{lid}.mp3"
            if not mp3.exists():
                print(f"{lid}: no clip")
                continue
            stamp = f"{mp3.stat().st_size}:{int(mp3.stat().st_mtime)}"
            if args.refresh or cache.get(lid, {}).get("stamp") != stamp:
                cache[lid] = {"stamp": stamp, "text": transcribe(session, mp3)}
                CACHE.write_text(json.dumps(cache, indent=1, ensure_ascii=False))
            said = collapse(words_of(ln["text"]))
            heard = collapse(words_of(cache[lid]["text"]))
            sm = difflib.SequenceMatcher(a=said, b=heard, autojunk=False)
            diffs = [(op, " ".join(said[a0:a1]), " ".join(heard[b0:b1])) for op, a0, a1, b0, b1 in sm.get_opcodes() if op != "equal"]
            if not diffs:
                print(f"{lid}: OK")
                continue
            issues += 1
            print(f"{lid}: " + "  |  ".join(f"script '{a}' → heard '{b}'" for _, a, b in diffs))
    print(f"{issues} line(s) to look at")


if __name__ == "__main__":
    main()
