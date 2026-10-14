#!/usr/bin/env python3
"""
Install, launch and observe the VR build on a Quest connected over USB (adb).

  python Tools/quest.py install Builds/zeta-vr.apk
  python Tools/quest.py run                            # launch normally
  python Tools/quest.py run --chapter turn --seconds 90   # start straight in a chapter and collect performance lines
  python Tools/quest.py log --seconds 30               # just watch the app's log
  python Tools/quest.py stop

The app writes lines like
  [VRPERF] turn_03  frames 421  avg 13.8 ms (72 fps)  worst 29.1 ms  >14.5ms 3%  gc 0  scale 22.4  passthrough True
to the Android log; this tool filters and summarises them. Put the headset on (or it will sleep and pause the app).
"""
import argparse
import glob
import os
import re
import shutil
import subprocess
import sys
import time
from pathlib import Path

PACKAGE = "com.metavirtuoso.zetaviewer"


def adb_path():
    """An adb.exe Python can start: on PATH, in ANDROID_HOME, or in the SDK that ships with a Unity editor."""
    found = shutil.which("adb.exe") or shutil.which("adb")
    if found and found.lower().endswith(".exe"):
        return found
    candidates = []
    for root in (os.environ.get("ANDROID_HOME"), os.environ.get("ANDROID_SDK_ROOT")):
        if root:
            candidates.append(os.path.join(root, "platform-tools", "adb.exe"))
    candidates += sorted(glob.glob("C:/Program Files/Unity/Hub/Editor/*/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe"), reverse=True)
    for c in candidates:
        if os.path.exists(c):
            return c
    return found or "adb"


ADB = adb_path()


def adb(*args, check=True, capture=True):
    return subprocess.run([ADB, *args], capture_output=capture, text=True, encoding="utf-8", errors="replace", check=check)


def device():
    out = adb("devices").stdout.splitlines()[1:]
    ready = [line.split()[0] for line in out if line.strip().endswith("device")]
    if not ready:
        sys.exit("no device: connect the Quest by USB and accept the debugging prompt in the headset")
    return ready[0]


def launch_component():
    out = adb("shell", "cmd", "package", "resolve-activity", "--brief", "-c", "android.intent.category.LAUNCHER", PACKAGE).stdout.strip().splitlines()
    return out[-1] if out and "/" in out[-1] else f"{PACKAGE}/com.unity3d.player.UnityPlayerActivity"


def cmd_install(args):
    apk = Path(args.apk)
    if not apk.exists():
        sys.exit(f"{apk} not found")
    print(f"installing {apk.name} ({apk.stat().st_size / 1e6:.1f} MB) on {device()}")
    out = adb("install", "-r", str(apk), check=False)
    print(out.stdout.strip() or out.stderr.strip())
    if "Success" not in out.stdout:
        sys.exit(1)


def cmd_stop(_):
    adb("shell", "am", "force-stop", PACKAGE, check=False)
    print("stopped")


def start(args):
    adb("shell", "am", "force-stop", PACKAGE, check=False)
    adb("logcat", "-c", check=False)
    unity_args = []
    if getattr(args, "tour", False) or getattr(args, "chapter", None):
        unity_args.append("-tour")
    if getattr(args, "chapter", None):
        unity_args += ["-chapter", args.chapter]
    # `adb shell` joins its arguments into one string for the device's shell, so the extras need their own quotes
    command = f"am start -n {launch_component()}"
    if unity_args:
        command += f" -e unity '{' '.join(unity_args)}'"
    out = adb("shell", command)
    print(out.stdout.strip())


def watch(seconds, quiet=False):
    """Wait, then read what the app logged meanwhile (a dump, so a sleeping headset cannot block the tool)."""
    time.sleep(seconds)
    out = adb("logcat", "-d", "-v", "time", "Unity:I", "AndroidRuntime:E", "CRASH:E", "DEBUG:F", "*:S", check=False).stdout
    perf, problems = [], []
    for line in out.splitlines():
        if "[VRPERF]" in line:
            perf.append(line.strip())
            if not quiet:
                print(line.strip().split("[VRPERF]")[-1].strip())
        elif re.search(r"Exception|error CS|FATAL|signal \d|Shader error|failed to|Unable to", line, re.I):
            problems.append(line.strip())
            if not quiet:
                print("!!", line.strip()[:240])
    return perf, problems


def summarise(perf):
    rows = []
    for line in perf:
        m = re.search(r"\[VRPERF\]\s+(\S+)\s+frames (\d+)\s+avg ([\d.]+) ms.*worst ([\d.]+) ms", line)
        if m:
            rows.append((m.group(1), int(m.group(2)), float(m.group(3)), float(m.group(4))))
    if not rows:
        print("no performance lines seen (is the headset on, and did the app start?)")
        return
    frames = sum(r[1] for r in rows)
    avg = sum(r[1] * r[2] for r in rows) / frames
    print(f"\n{len(rows)} windows, {frames} frames: mean {avg:.1f} ms ({1000 / avg:.0f} fps); worst window avg {max(r[2] for r in rows):.1f} ms; worst frame {max(r[3] for r in rows):.0f} ms")


def cmd_run(args):
    device()
    start(args)
    if args.seconds:
        perf, problems = watch(args.seconds)
        summarise(perf)
        if problems:
            print(f"{len(problems)} problem line(s) seen")


def cmd_log(args):
    perf, _ = watch(args.seconds)
    summarise(perf)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="command", required=True)
    p = sub.add_parser("install")
    p.add_argument("apk", nargs="?", default="Builds/zeta-vr.apk")
    p.set_defaults(func=cmd_install)
    p = sub.add_parser("run")
    p.add_argument("--chapter", help="start the narrated tour at this chapter id")
    p.add_argument("--tour", action="store_true", help="start the narrated tour from the first chapter")
    p.add_argument("--seconds", type=int, default=0, help="watch the log for this long")
    p.set_defaults(func=cmd_run)
    p = sub.add_parser("log")
    p.add_argument("--seconds", type=int, default=30)
    p.set_defaults(func=cmd_log)
    p = sub.add_parser("stop")
    p.set_defaults(func=cmd_stop)
    args = parser.parse_args()
    args.func(args)


if __name__ == "__main__":
    main()
