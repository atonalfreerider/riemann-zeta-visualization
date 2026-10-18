#!/usr/bin/env python3
"""A first look at screenshots before a person reviews them, by TypeSafe's System One model Jev (https://docs.typesafe.ai).

For each image Jev answers five narrow questions about the picture itself:
* text_cut_off   (Noul)  is caption or label text clipped by the image border?
* labels_collide (Noul)  do labels overlap each other or bright marks so that words are hard to read?
* mostly_empty   (Noul)  is most of the frame empty background, the drawing small or pushed into a corner?
* shows_intended (Noul)  does the picture show what the one-line description says it should?
* clarity        (Score) how clearly can the intended shapes be made out (a tangle … clear and uncluttered)?
A shot is flagged when a defect is more likely than not, when it does not show what it should, or when clarity is low;
a flag is a request for a human look, not a verdict (the confidence page's three-tier pattern). Code keeps every rule and
threshold; Jev only supplies the judgments.

Run from the repository root, with the key in the environment (never on the command line):
    TYPESAFE_API_KEY=… python3 Tools/jev_check.py SHOT.png "what it should show" [SHOT2.png "…" …]
Images are scaled to at most 1568 px on the longer side (the API's limit) with ffmpeg before upload.
"""
import base64, json, os, subprocess, sys, tempfile, time, urllib.error, urllib.request, uuid

API = "https://api.typesafe.ai/v1/systemone"
CLARITY = [
    "The intended shapes are lost in a tangle of lines or dots; the subject cannot be made out",
    "The subject can be found with effort, but much of it is hidden by clutter or too small to read",
    "The subject is clear, though some areas are busy or crowded",
    "The subject is clear and uncluttered, and every part can be read",
]
QUESTIONS = {
    "text_cut_off": {"type": "noul", "instructions": "In `screenshot`, is any caption or label text cut off by the edge of the image, so that letters or words are missing?",
                     "criteria": {"true": "Some text runs past the border of the image or is clipped at an edge", "false": "Every piece of text lies fully inside the image"}},
    "labels_collide": {"type": "noul", "instructions": "In `screenshot`, do text labels overlap each other, or sit on bright lines or dots, so that some of their words are hard to read?",
                       "criteria": {"true": "At least one label is overlapped by another label or by bright marks, making words hard to read", "false": "Every label can be read without obstruction"}},
    "mostly_empty": {"type": "noul", "instructions": "Is most of `screenshot` empty dark background, with the drawing small or pushed into one part of the frame?"},
    "shows_intended": {"type": "noul", "instructions": "Does `screenshot` show what `intended` describes?"},
    "clarity": {"type": "score", "instructions": "How clearly can the shapes described in `intended` be made out in `screenshot`?", "criteria": CLARITY},
}


def prepare(path):
    """Scale to at most 1568 px on the longer side (and at least 280 on the shorter) as a baseline JPEG."""
    out = os.path.join(tempfile.mkdtemp(), "shot.jpg")
    subprocess.run(["ffmpeg", "-nostdin", "-loglevel", "error", "-y", "-i", path, "-vf",
                    "scale='if(gt(iw,ih),min(1568,iw),-2)':'if(gt(iw,ih),-2,min(1568,ih))'", "-q:v", "3", out], check=True)
    with open(out, "rb") as f:
        data = base64.b64encode(f.read()).decode()
    os.remove(out); os.rmdir(os.path.dirname(out))
    return data


def ask(state, questions, assets=None, tries=5):
    body = {"model": "jev-latest", "state": state, "questions": questions}
    headers = {"Authorization": "Bearer " + os.environ["TYPESAFE_API_KEY"], "Content-Type": "application/json"}
    if assets:
        body["assets"] = assets
        headers["TypeSafe-Danger-Zone-Images"] = "true"
    data = json.dumps(body).encode()
    for k in range(tries):
        try:
            req = urllib.request.Request(API, data=data, headers=headers, method="POST")
            with urllib.request.urlopen(req, timeout=180) as r:
                return json.load(r)
        except urllib.error.HTTPError as e:
            if e.code in (429, 529) and k + 1 < tries:
                time.sleep(2 ** k)
                continue
            raise SystemExit(f"TypeSafe API error {e.code}: {e.read().decode(errors='replace')[:400]}")


def check(path, intended):
    ref = "shot-" + uuid.uuid4().hex
    res = ask({"screenshot": ref, "intended": intended}, QUESTIONS,
              {ref: {"type": "image", "format": "jpeg", "data_base64": prepare(path)}})
    a = res["answers"]
    flags = [name for name in ("text_cut_off", "labels_collide", "mostly_empty") if a[name]["noul"] > .5]
    if a["shows_intended"]["noul"] < .5: flags.append("does_not_show_intended")
    if a["clarity"]["score"] < 1.5: flags.append("unclear")
    return a, flags, res.get("usage", {})


def main():
    args = sys.argv[1:]
    if not args or len(args) % 2:
        raise SystemExit(__doc__)
    report = []
    for path, intended in zip(args[::2], args[1::2]):
        a, flags, usage = check(path, intended)
        row = {"shot": os.path.basename(path), "flags": flags,
               "text_cut_off": round(a["text_cut_off"]["noul"], 3), "labels_collide": round(a["labels_collide"]["noul"], 3),
               "mostly_empty": round(a["mostly_empty"]["noul"], 3), "shows_intended": round(a["shows_intended"]["noul"], 3),
               "clarity": round(a["clarity"]["score"], 2), "clarity_confidence": round(a["clarity"]["confidence"], 2),
               "input_tokens": usage.get("input_tokens")}
        report.append(row)
        print(f"{row['shot']}:  {'FLAGGED ' + ', '.join(flags) if flags else 'ok'}   (cut-off {row['text_cut_off']}, collide {row['labels_collide']},"
              f" empty {row['mostly_empty']}, shows {row['shows_intended']}, clarity {row['clarity']}/3 at {row['clarity_confidence']})", flush=True)
    json.dump(report, open(os.environ.get("JEV_REPORT", os.devnull), "w"), indent=1)


if __name__ == "__main__":
    main()
