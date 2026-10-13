#!/usr/bin/env python3
"""
Cut one GIF per chapter from a tour recording, using the chapter marks in timeline.json.

  python3 Tools/make_chapter_gifs.py Recordings/tour-preview [--seconds 6] [--offset 4] [--width 300] [--fps 10]

Writes docs/gifs/<nn>-<chapter>.gif (palette-optimised). The clip starts `offset` seconds into the chapter
(after the title card) and lasts `seconds`.
"""
import argparse, json, subprocess, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent

def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("recording")
    ap.add_argument("--seconds", type=float, default=6.0)
    ap.add_argument("--offset", type=float, default=4.0)
    ap.add_argument("--width", type=int, default=300)
    ap.add_argument("--fps", type=int, default=10)
    ap.add_argument("--out", default=str(ROOT / "docs/gifs"))
    ap.add_argument("--offsets", default="", help="per-chapter overrides, e.g. hook=12,waves=30 (seconds into the chapter)")
    a = ap.parse_args()
    rec = Path(a.recording)
    tl = json.loads((rec / "timeline.json").read_text())
    fps, video = tl["fps"], rec / tl["video"]
    overrides = dict(kv.split("=") for kv in a.offsets.split(",") if kv)
    out = Path(a.out); out.mkdir(parents=True, exist_ok=True)
    chapters = tl.get("chapters", [])
    if not chapters:
        sys.exit("timeline.json has no chapter marks (record with the current TourDirector)")
    for i, ch in enumerate(chapters):
        start = ch["frame"] / fps + float(overrides.get(ch["id"], a.offset))
        dst = out / f"{i + 1:02d}-{ch['id']}.gif"
        vf = f"fps={a.fps},scale={a.width}:-1:flags=lanczos,split[s0][s1];[s0]palettegen=max_colors=128[p];[s1][p]paletteuse=dither=bayer:bayer_scale=4"
        subprocess.run(["ffmpeg", "-loglevel", "error", "-y", "-ss", f"{start:.2f}", "-t", str(a.seconds), "-i", str(video),
                        "-vf", vf, "-loop", "0", str(dst)], check=True)
        print(f"{dst.name}: {dst.stat().st_size // 1024} KB (from {start:.1f}s)")

if __name__ == "__main__":
    main()
