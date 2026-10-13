#!/usr/bin/env python3
"""
Check the rendered narration against the script and derive real word timings for the karaoke captions.

Run it with the Python of the narrator install, which already has torch + transformers and a local Whisper model
in the Hugging Face cache (nothing is downloaded):

  ~/.narrator/env/python.exe Tools/align_narration.py            # all lines in Tools/narration_raw
  ~/.narrator/env/python.exe Tools/align_narration.py --only qrh

For every line it transcribes <raw>/<id>.wav with Whisper (word timestamps), maps the heard words onto the words of
script.json, writes Assets/Resources/Narration/Timing/<id>.json and prints the word error rate. A high rate means the
text-to-speech garbled or skipped something and the line should be rendered again.
"""
import argparse
import difflib
import json
import os
import re
import sys
from pathlib import Path

os.environ.setdefault("HF_HUB_OFFLINE", "1")        # use the cached model only
os.environ.setdefault("TRANSFORMERS_OFFLINE", "1")

ROOT = Path(__file__).resolve().parent.parent
SCRIPT = ROOT / "Assets/Resources/Narration/script.json"
RAW_DIR = ROOT / "Tools/narration_raw"
TIMING_DIR = ROOT / "Assets/Resources/Narration/Timing"
MODEL = "openai/whisper-small.en"


def norm(word):
    return re.sub(r"[^a-z0-9]", "", word.lower().replace("'", ""))


def lines_of(script):
    seen = set()
    for chapter in script["chapters"]:
        for line in chapter["lines"]:
            if line["id"] not in seen:
                seen.add(line["id"])
                yield chapter["id"], line


def word_error_rate(ref, hyp):
    d = list(range(len(hyp) + 1))
    for i, r in enumerate(ref, 1):
        prev, d[0] = d[0], i
        for j, h in enumerate(hyp, 1):
            cur = min(d[j] + 1, d[j - 1] + 1, prev + (r != h))
            prev, d[j] = d[j], cur
    return d[len(hyp)] / max(1, len(ref))


def map_words(script_words, heard):
    """heard: [(word, start, end)] -> one (start, end) per script word (interpolating the unheard ones)."""
    ref = [norm(w) for w in script_words]
    hyp = [norm(h[0]) for h in heard]
    result = [None] * len(script_words)
    for tag, i1, i2, j1, j2 in difflib.SequenceMatcher(None, ref, hyp, autojunk=False).get_opcodes():
        if tag == "equal" or (tag == "replace" and i2 - i1 == j2 - j1):
            for k in range(i2 - i1):
                result[i1 + k] = (heard[j1 + k][1], heard[j1 + k][2])
        elif tag == "replace" and j2 > j1:
            # different counts: spread the heard span over the script words by length
            start, end = heard[j1][1], heard[j2 - 1][2]
            weights = [max(1, len(ref[i])) for i in range(i1, i2)]
            total, t = sum(weights), start
            for k, i in enumerate(range(i1, i2)):
                d = (end - start) * weights[k] / total
                result[i] = (t, t + d)
                t += d
    # interpolate whatever is still missing between its neighbours
    known = [i for i, r in enumerate(result) if r is not None]
    if not known:
        return None
    for i in range(len(result)):
        if result[i] is not None:
            continue
        left = max((k for k in known if k < i), default=None)
        right = min((k for k in known if k > i), default=None)
        if left is None:
            s = result[right][0]
            result[i] = (max(0.0, s - 0.25 * (right - i)), max(0.0, s - 0.25 * (right - i - 1)))
        elif right is None:
            e = result[left][1]
            result[i] = (e + 0.25 * (i - left - 1), e + 0.25 * (i - left))
        else:
            gap = right - left
            a, b = result[left][1], result[right][0]
            step = (b - a) / gap
            result[i] = (a + step * (i - left - 1), a + step * (i - left))
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--only", help="chapter id or line id")
    parser.add_argument("--raw", default=str(RAW_DIR))
    parser.add_argument("--model", default=MODEL)
    parser.add_argument("--threshold", type=float, default=0.2, help="flag lines with a word error rate above this")
    args = parser.parse_args()

    import torch
    from transformers import pipeline

    device = 0 if torch.cuda.is_available() else -1
    asr = pipeline("automatic-speech-recognition", model=args.model, device=device,
                   torch_dtype=torch.float16 if device == 0 else torch.float32)

    script = json.loads(SCRIPT.read_text(encoding="utf-8"))
    TIMING_DIR.mkdir(parents=True, exist_ok=True)
    flagged = []
    rates = []
    for chapter_id, line in lines_of(script):
        if args.only and args.only not in (chapter_id, line["id"]):
            continue
        wav = Path(args.raw) / f"{line['id']}.wav"
        if not wav.exists():
            print(f"{line['id']:10} missing {wav}")
            continue

        out = asr(str(wav), return_timestamps="word", chunk_length_s=30,
                  generate_kwargs={"language": "en"} if "en" not in args.model.split("/")[-1] else {})
        heard = []
        for chunk in out["chunks"]:
            word = chunk["text"].strip()
            start, end = chunk["timestamp"]
            if word and start is not None:
                heard.append((word, float(start), float(end if end is not None else start + 0.2)))

        script_words = line["text"].split()
        wer = word_error_rate([norm(w) for w in script_words], [norm(h[0]) for h in heard])
        rates.append(wer)
        mapped = map_words(script_words, heard)
        if mapped is None:
            print(f"{line['id']:10} nothing recognised")
            flagged.append(line["id"])
            continue

        duration = float(json.loads((TIMING_DIR / f"{line['id']}.json").read_text())["duration"]) if (TIMING_DIR / f"{line['id']}.json").exists() else mapped[-1][1]
        words = [{"w": w, "s": round(max(0.0, s), 3), "e": round(max(e, s), 3)} for w, (s, e) in zip(script_words, mapped)]
        (TIMING_DIR / f"{line['id']}.json").write_text(
            json.dumps({"words": words, "duration": round(duration, 3)}, ensure_ascii=False, indent=1), encoding="utf-8")

        mark = "  <-- CHECK" if wer > args.threshold else ""
        print(f"{line['id']:10} WER {wer:5.2f}  {len(script_words):3} words{mark}", flush=True)
        if wer > args.threshold:
            flagged.append(line["id"])
            print("           script:", line["text"])
            print("           heard :", " ".join(h[0] for h in heard))

    if rates:
        print(f"\nmean WER {sum(rates) / len(rates):.3f} over {len(rates)} lines; flagged: {', '.join(flagged) or 'none'}")


if __name__ == "__main__":
    sys.exit(main())
