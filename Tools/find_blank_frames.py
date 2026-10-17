#!/usr/bin/env python3
"""
Find the stretches of a recorded segment where the picture is empty: twice a second, the share of lit pixels between the
caption board and the captions; a run of a second or more below 0.2 % is reported with the narration line it falls in.

  python3 Tools/find_blank_frames.py Recordings/experiments              # every long-form video in the folder
  python3 Tools/find_blank_frames.py Recordings/experiments/31-xs5-horizontal.mp4

A segment where one picture is hidden before the next is shown, or where a 16:9 shot leaves its subject a sliver in a 9:16
frame, shows up here before anyone has to watch for it.
"""
import json, os, subprocess, sys


def lit_fractions(video, tall):
    w, h = (90, 160) if tall else (160, 90)
    raw = subprocess.run(["ffmpeg", "-loglevel", "error", "-i", video, "-vf", f"fps=2,scale={w}:{h}", "-f", "rawvideo", "-pix_fmt", "gray", "-"],
                         capture_output=True).stdout
    y0, y1 = (int(h * .20), int(h * .70)) if tall else (int(h * .22), int(h * .76))
    out = []
    for f in range(len(raw) // (w * h)):
        fr = raw[f * w * h:(f + 1) * w * h]
        out.append(sum(1 for y in range(y0, y1) for x in range(w) if fr[y * w + x] > 45) / ((y1 - y0) * w))
    return out


def lines_of(video):
    tl = os.path.join(video[:-4], "timeline.json")
    return json.load(open(tl))["audio"] if os.path.exists(tl) else []


def report(video):
    frac = lit_fractions(video, "vertical" in os.path.basename(video))
    lines = lines_of(video)
    runs, start = [], None
    for i, v in enumerate(frac + [1.0]):
        if v < .002 and start is None:
            start = i
        if v >= .002 and start is not None:
            if i - start >= 2:
                runs.append((start / 2, (i - start) / 2))
            start = None
    name = os.path.basename(video)
    if not runs:
        print(f"{name}: no empty stretch")
    for t, d in runs:
        cur = [a for a in lines if a["time"] <= t]
        where = f"{cur[-1]['id']} +{t - cur[-1]['time']:.1f}s" if cur else ""
        print(f"{name}: empty for {d:.1f}s at {t:.1f}s  {where}")


def main():
    target = sys.argv[1] if len(sys.argv) > 1 else "Recordings/experiments"
    if os.path.isdir(target):
        for f in sorted(os.listdir(target)):
            if f.endswith(".mp4") and f[:2].isdigit() and ("-xs" in f or "-xz" in f):
                report(os.path.join(target, f))
    else:
        report(target)


if __name__ == "__main__":
    main()
