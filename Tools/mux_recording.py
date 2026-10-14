#!/usr/bin/env python3
"""
Combine a tour recording (video from FrameRecorder) with the narration clips.

  python3 Tools/mux_recording.py Recordings/20261013-101500 [-o final.mp4]

Reads <dir>/timeline.json (written when recording stops), places every narration clip
at its frame offset, and muxes with the video (or encodes the PNG sequence first).
"""
import argparse
import json
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
AUDIO_DIR = ROOT / "Assets/Resources/Narration/Audio"


def audio_file(line_id):
    """The narration clip for a line: wav (Chatterbox, generate_narration_chatterbox.py) or mp3 (ElevenLabs)."""
    for ext in (".wav", ".mp3"):
        path = AUDIO_DIR / f"{line_id}{ext}"
        if path.exists():
            return path
    return None


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("recording", help="recording folder containing timeline.json")
    parser.add_argument("-o", "--output", help="output mp4 (default: <dir>/tour.mp4)")
    args = parser.parse_args()

    if not shutil.which("ffmpeg"):
        sys.exit("ffmpeg not found on PATH")

    rec = Path(args.recording)
    timeline = json.loads((rec / "timeline.json").read_text())
    fps = timeline["fps"]
    output = Path(args.output) if args.output else rec / "tour.mp4"

    video = rec / timeline["video"]
    if "%" in timeline["video"]:
        encoded = rec / "video.mp4"
        subprocess.run([
            "ffmpeg", "-y", "-framerate", str(fps), "-i", str(video),
            "-c:v", "libx264", "-crf", "18", "-pix_fmt", "yuv420p", str(encoded)
        ], check=True)
        video = encoded

    clips = [c for c in timeline["audio"] if audio_file(c["id"])]
    if not clips:
        print("no narration clips found for this recording; copying video only")
        shutil.copy(video, output)
        return

    cmd = ["ffmpeg", "-y", "-i", str(video)]
    filters = []
    for i, clip in enumerate(clips):
        cmd += ["-i", str(audio_file(clip["id"]))]
        delay_ms = int(round(clip["frame"] / fps * 1000))
        filters.append(f"[{i + 1}:a]adelay={delay_ms}|{delay_ms}[a{i}]")
    mix_inputs = "".join(f"[a{i}]" for i in range(len(clips)))
    filters.append(f"{mix_inputs}amix=inputs={len(clips)}:normalize=0:dropout_transition=0,apad[aout]")
    cmd += [
        "-filter_complex", ";".join(filters),
        "-map", "0:v", "-map", "[aout]",
        # the narration is padded with silence and cut at the end of the video: the film runs to its last frame,
        # not to the end of the last spoken line
        "-c:v", "copy", "-c:a", "aac", "-b:a", "192k", "-shortest", str(output)
    ]
    subprocess.run(cmd, check=True)
    print(f"wrote {output}")


if __name__ == "__main__":
    main()
