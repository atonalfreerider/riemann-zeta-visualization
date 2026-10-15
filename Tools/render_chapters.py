#!/usr/bin/env python3
"""
Render every chapter of the tour as its own narrated video, vertical (1080x1920) and horizontal (1920x1080).

  python Tools/render_chapters.py --unity "C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe" \
         --project C:/path/to/project --out Recordings/chapters
  python Tools/render_chapters.py ... --chapters hook,zeno --shapes vertical      # a subset
  python Tools/render_chapters.py ... --mux-only                                  # just add narration to existing renders

Unity runs the ChapterRecordingTest render driver in batch mode (close the editor first, or point --project at a copy).
Every chapter is recorded from a freshly loaded scene into <out>/NN-chapter-shape/video.mp4 + timeline.json,
then Tools/mux_recording.py lays the narration clips on at the frames where they started, and the finished file is
written next to the folders as <out>/NN-chapter-shape.mp4.
"""
import argparse
import os
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--unity", help="path to Unity.exe (not needed with --mux-only)")
    parser.add_argument("--project", default=str(ROOT), help="Unity project to open (default: this one)")
    parser.add_argument("--out", default=str(ROOT / "Recordings/chapters"))
    parser.add_argument("--chapters", help="comma-separated chapter ids (default: all)")
    parser.add_argument("--script", default="script", help="script (the tour, default) or experiments (the experiment modules)")
    parser.add_argument("--shapes", default="vertical,horizontal")
    parser.add_argument("--fps", default="30")
    parser.add_argument("--no-captions", action="store_true")
    parser.add_argument("--mux-only", action="store_true")
    parser.add_argument("--log", help="Unity log file")
    args = parser.parse_args()

    out = Path(args.out).resolve()
    out.mkdir(parents=True, exist_ok=True)

    if not args.mux_only:
        if not args.unity:
            sys.exit("--unity is required (path to Unity.exe)")
        env = dict(os.environ, ZETA_OUT=str(out), ZETA_SHAPES=args.shapes, ZETA_FPS=args.fps)
        if args.chapters:
            env["ZETA_CHAPTERS"] = args.chapters
        if args.script != "script":
            env["ZETA_SCRIPT"] = args.script
        if args.no_captions:
            env["ZETA_NO_CAPTIONS"] = "1"
        log = args.log or str(out / "unity.log")
        cmd = [args.unity, "-batchmode", "-projectPath", str(Path(args.project).resolve()), "-runTests", "-testPlatform", "PlayMode",
               "-testFilter", "ChapterRecordingTest", "-logFile", log]
        print("running Unity:", " ".join(cmd), flush=True)
        code = subprocess.run(cmd, env=env).returncode
        print(f"Unity exited with {code} (log: {log})")
        if code not in (0, 2):  # 2 = test failures; the videos that finished are still muxed below
            sys.exit(code)

    wanted = set(args.chapters.split(",")) if args.chapters else None
    shapes = set(args.shapes.split(","))
    made = []
    for folder in sorted(p for p in out.iterdir() if p.is_dir() and (p / "timeline.json").exists()):
        _, chapter, shape = folder.name.split("-", 2)
        if (wanted and chapter not in wanted) or shape not in shapes:
            continue
        final = out / f"{folder.name}.mp4"
        subprocess.run([sys.executable, str(ROOT / "Tools/mux_recording.py"), str(folder), "-o", str(final)], check=True)
        made.append(final)

    print("\nfinished videos:")
    for f in made:
        print(f"  {f.name:34} {f.stat().st_size / 1e6:7.1f} MB")


if __name__ == "__main__":
    main()
