#!/usr/bin/env python3
"""Render Assets/Resources/Narration/script.json as docs/narration-script.md (one caption per line)."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
script = json.loads((ROOT / "Assets/Resources/Narration/script.json").read_text(encoding="utf-8"))

out = [f"# {script['title']} — narration script", "",
       "Every line below is one caption/narration unit. The tour highlights each word in yellow as it is spoken.",
       "Camera cues and scene actions are listed for reference; see docs/TOUR.md for the vocabulary.", ""]
total_words = 0
for i, chapter in enumerate(script["chapters"], 1):
    out.append(f"## {i}. {chapter['title']}")
    if chapter.get("subtitle"):
        out.append(f"*{chapter['subtitle']}*")
    out.append("")
    for line in chapter["lines"]:
        total_words += len(line["text"].split())
        out.append(f"**{line['id']}** — {line['text']}")
        meta = [f"camera `{line.get('camera', '')}`"]
        if line.get("actions"):
            meta.append("actions `" + " ".join(line["actions"]) + "`")
        if line.get("footnote"):
            meta.append("footnote: " + line["footnote"])
        out.append("  <sub>" + " · ".join(meta) + "</sub>")
        out.append("")
out.append(f"---\n{total_words} words ≈ {total_words / 150:.1f} minutes of speech at 150 wpm, plus pauses and title cards.")
(ROOT / "docs/narration-script.md").write_text("\n".join(out) + "\n", encoding="utf-8")
print("wrote docs/narration-script.md")
