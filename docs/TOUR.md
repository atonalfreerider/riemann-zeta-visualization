# The guided tour

A narrated, chaptered camera fly-through of the visualization, laid out for vertical
(9:16, 1080×1920) recording with karaoke-style captions.

## Running it

| Where | How |
|---|---|
| Unity editor | menu **Zeta ▸ Play Tour** (or enter Play mode and press **T**) |
| Editor, one chapter | **Zeta ▸ Play Tour from Chapter…** |
| Editor, record | **Zeta ▸ Play Tour and Record 1080x1920** |
| Player build | `./zeta-visualization -tour`, `-record`, `-chapter qrh`, `-landscape` |

Keys in Play mode: **T** start/stop tour · **N** skip to the next line · **F9** start/stop recording ·
**P** toggle the 9:16 pillarbox · Space / Esc / R / WASD as before.

The tour pillarboxes the camera to 9:16 so the editor preview matches the recording.
**Zeta ▸ Game View – Portrait 1080x1920** adds and selects a matching Game view size.

## Recording

`FrameRecorder` renders the camera into an offscreen 1080×1920 texture at a fixed 30 fps
(`Time.captureFramerate`), so the output is frame-accurate regardless of machine speed, and streams
raw frames to `ffmpeg` (H.264, CRF 18). Without ffmpeg on PATH it writes a PNG sequence instead.

Output lands in `Recordings/<timestamp>/`:

- `video.mp4` (or `frames/`)
- `timeline.json` — which narration clip started on which frame

Narration is muxed afterwards (live audio runs in real time, the capture does not):

```bash
python3 Tools/mux_recording.py Recordings/<timestamp>
```

That writes `Recordings/<timestamp>/tour.mp4`.

## Narration audio (ElevenLabs)

1. Review and edit the captions in `Assets/Resources/Narration/script.json`
   (human-readable copy: `docs/narration-script.md`, regenerate with `python3 Tools/export_captions.py`).
2. Put the API key in the environment, never on the command line:
   ```bash
   export ELEVENLABS_API_KEY=...
   ```
3. Generate (voice name defaults to `John V4`; `--only <chapter-id>` for a subset, `--force` to redo):
   ```bash
   python3 Tools/generate_narration.py --voice "John V4"
   ```
4. Switch to Unity so it imports `Assets/Resources/Narration/Audio/*.mp3`, then play the tour.

Each line gets an `.mp3` and a `Timing/<id>.json` with per-word start/end times derived from the
ElevenLabs character alignment, so the yellow highlight follows the real speech. Until audio exists
the tour estimates timings (~2.7 words/s) so everything can be previewed.

## Script format

`script.json` → `chapters[]` → `lines[]`:

```json
{ "id": "spiral_05", "text": "The fix is Euler and Maclaurin's. …",
  "camera": "spiral_top", "actions": ["show:tail", "flash:tail"], "pause": 0.3,
  "footnote": "optional small print shown above the caption" }
```

**Camera cues** are defined in `Assets/Tour/CameraRig.cs` (one lambda per cue; several follow the
spiral tip or the probe so they track the animation). Tune positions there.

**Actions** (applied when the line starts):

| action | effect |
|---|---|
| `play`, `pause`, `reset` | animation control |
| `speed:X` | t advances X units per second |
| `jump:T` | fast-forward (or restart and forward) to t = T |
| `show:X` / `hide:X` | `tail` `spiral` `components` `strip` `axes` `imaxis` `outaxes` `zcurve` `band78` `band18` `classic` `band1112` `gap` `cosine` `prime` `equations` `zeros` `zetacurve` |
| `flash:zero`, `flash:tail` | pulse the newest zero marker / the orange tail |
| `ghost:on`, `ghost:off`, `ghost:β:γ` | draw the prime staircase as if a zero sat at β + iγ |

## What the scene shows

- **Output space** (the original visualization): x = Re ζ(s), z = Im ζ(s), y = t·0.3. The spiral at
  height t is the chain of arrows n^-s; the orange arrow is the Euler–Maclaurin tail that closes it
  onto ζ(s) exactly (see `Assets/Math/ZetaMath.cs`). Yellow markers on the vertical axis are the zeros.
- **Input panel** (tour only, left of the scene): the critical strip 0 ≤ Re(s) ≤ 1 with the critical
  line at ½, the moving probe s = ½ + it, Hardy's Z(t) wiggle (real, crosses the line at the zeros),
  zero ticks linked to the markers, and the zero-free bands — classical sliver, the 11/12 companion
  result, and the 2026 **7/8** claim with its mirror image below 1/8.
- **Prime graph**: ψ(x) from the explicit formula, red daggers at detected primes, and the
  "cosine chorus" −Σ cos(γ log x) that spikes at prime powers.

## About the 2026 result referenced in chapter 7

On 6 October 2026 OpenAI published ~720 AI-generated manuscripts (`openai/math`). Family 003,
"The Quasi-Riemann Hypothesis: A Zero-Free Half-Plane ℜs > 7/8" (manuscript dated 30 Sept 2026),
claims that ζ and all Dirichlet L-functions (and finite-order Hecke L-functions over ℚ(√−3)) have
no zeros with Re s > 7/8; a companion proves Re s > 11/12 by a different method. The core zeta
statement was formalised in Lean (`riemannZeta s ≠ 0` for `7/8 < s.re`) and re-checked by a third
party with an independent kernel. It is **not** the Riemann Hypothesis (which needs ½), human peer
review is still in progress, and some commentators expect the method to stall near 3/4.
The narration says exactly that.
