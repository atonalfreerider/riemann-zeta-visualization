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
**P** toggle the 9:16 pillarbox · **F** fps readout · Space / Esc / R / WASD as before.

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

## Narration audio

The narration is rendered locally with this machine's Chatterbox install (`narrate`, John's voice):

```bash
python Tools/generate_narration_chatterbox.py              # every line that has no render yet
python Tools/generate_narration_chatterbox.py --only qrh   # one chapter id or one line id
python Tools/generate_narration_chatterbox.py --only outro_05 --force   # render a line again
python Tools/generate_narration_chatterbox.py --no-render  # redo only the alignment and loudness stages
```

For each line of `Assets/Resources/Narration/script.json` it runs `narrate "<text>" --exaggeration 0.25 -o <id>.wav`
(untouched renders go to `Tools/narration_raw`, which is git-ignored), and then

* **aligns** the words with the local Whisper model (`Tools/align_narration.py`, run with the narrator install's Python,
  nothing is downloaded) and writes `Timing/<id>.json` so the yellow karaoke highlight follows the real speech. The same
  pass compares what was spoken with the script and prints a word error rate per line; a high number means the line should
  be rendered again (numbers spoken as words show up as "errors" because Whisper writes digits, that is not a fault);
* **normalises** the loudness (a gentle compressor, gain to -18 LUFS and a peak limiter), because Chatterbox renders come
  out around -28 LUFS. The result is `Assets/Resources/Narration/Audio/<id>.wav`.

After changing a line's text in `script.json`, render that line again (`--only <id> --force`); lines whose text did not
change keep their audio. Until audio exists the tour estimates timings (~2.7 words/s), and without Whisper the generator
falls back to timing the words between the pauses it finds in the audio.

The previous ElevenLabs route (`Tools/generate_narration.py`, an API key, `.mp3` files) still works and
`Tools/mux_recording.py` accepts either format.

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
| `speed:X` | the dial t advances X units per second |
| `jump:T` | fast-forward (or restart and forward) to t = T |
| `terms:N` / `terms:auto` | fix the number of chain arrows (auto grows with t) |
| `build:A:B` | animate the arrow count from A to B across the line |
| `sigma:S` | shrink rate / real part of s (½ is the critical line; nothing else drops markers) |
| `cosine:N` / `cosine:all` | how many collisions the wave chorus sums |
| `cosine:build:A:B` | add collision waves one at a time across the line (each previewed, then merged); B may be `all` |
| `wave:plain[:γ]` / `wave:dial:γ` / `wave:log` / `wave:off` | ordinary cosine; sweep the γ dial across the line; bend with the logarithm; hide |
| `dial:on` / `dial:off` | the on-screen γ dial (needle follows the demo wave) |
| `zeno:build` / `zeno:reset` | grow Zeno's halving chain arrow by arrow |
| `card:<id>` / `card:off` | character card: `zeno` `euler` `riemann` `hardy` `machine` (portraits for zeno/euler/riemann/hardy come from Wikimedia Commons, see `Assets/Resources/Portraits/CREDITS.md`; `machine` uses a glyph medallion) |
| `trail:thin` / `trail:full` | the tip's path: barely visible, or grown to full width and glow |
| `tailghost:on` / `tailghost:off` | faint path of the next 120 arrows the orange tail replaces |
| `primes:on` / `primes:build` / `primes:off` | the 1..30 intro grid; `build` lights the primes one by one and shows factorisations |
| `primes:arrows` / `primes:noarrows` / `primes:highlight` | red arrows over the spikes; highlight 2, 3, 5, 7, 11, 13 in sequence |
| `axes:sprout` | grow the floor axes and the pole outward across the line |
| `angles:on` / `angles:off` | arcs and degree labels at each joint, with the t·log(n/(n−1)) rule |
| `dialstrip:N` / `dialstrip:clear` | add collision N's dial to the shelf at the top (also added automatically by `cosine:build`) |
| `stair:build` | morph the straight line y = x into the staircase |
| `rootlabel:sqrt` / `rootlabel:0.9` / `rootlabel:off` | the shrink rule written beside the spiral |
| `ghost:sweep:A:B[:pingpong]` | sweep the rogue exponent β from A to B while the red staircase and its formula follow |
| `sacks:spiral` / `sacks:saddle` / `sacks:flat` / `sacks:routes` / `sacks:noroutes` / `sacks:show` / `sacks:hide` | the primes grid morphs into the Sacks spiral, bends over a saddle, shows the prime routes |
| `chainfeet:on` / `chainfeet:off` | footprints along the √n chain |
| `rootlabel:chain` | the shrink rule written beside the flat chain |
| `waveview:N` | widen the wave view for N collisions (done automatically by `cosine:build`) |
| `squares:on` / `squares:off` | the muted-squares callout under the number line |
| `primepowers:on` / `primepowers:off` | orange ticks at 4, 8, 9, 16, 25, 27 on the staircase |
| `zeno_zoom` camera cue | follows the newest Zeno footprint and halves its distance each step |
| `sacks:lattice` / `sacks:squares` / `sacks:composites` | build the 2×/3×/5× multiplication lattice; pulse squares / composites on the spiral or saddle |
| `logseq:on` / `logseq:off` | bar panel of the bend sequence log(n/(n−1)) with live degrees |
| `delay:S:<action>` | run an action S seconds into the line (e.g. `delay:3:sacks:sieve:22`) so the narration can catch up |
| `sacks:sieve[:seconds]` | break the grid open into spiral positions, then draw the 2×, 3×, 5× axes through their products, striking each one |
| `landing:on` / `landing:off` | callout at the centre explaining why the chain closed at this t |
| `hypothesis:on` / `hypothesis:off` | the closing guess on the saddle: the √n spiral road plus the rings picked out by the logarithm rule at t = 14.13 and 21.02 |
| `pulse:<element>` | breathe the glow of an element for the line (`spiral` `tail` `zeros` `prime` `cosine` `trail` `strip` `band78` `band18` `gap` `numbers` `zeno` `wave` `dial` `center` `tip` `imaxis` `outaxes` `ghost` `primes` `sacks` `angles` `dialstrip` `primearrows` `chainfeet` `primepowers` `logseq` `floorgrid`) |
| `labels:N` | numeric labels on the first N zero markers |
| `show:X` / `hide:X` | `spiral` `tail` `arrows` `lengths` `tip` `center` `trail` `zeros` `grid` `equations` `cosine` `prime` `numbers` `polelabel` `zeno` `imaxis` `outaxes` `axes` `strip` `zcurve` `band78` `band18` `classic` `band1112` `gap` |
| `flash:zero`, `flash:tail` | pulse the newest zero marker / the orange tail |
| `ghost:all:β` | draw the staircase as if every collision had shrink rate β |
| `ghost:β:γ`, `ghost:off` | a single hypothetical zero at β + iγ |

## One video per chapter, vertical and horizontal

```bash
python Tools/render_chapters.py --unity "C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe" --project . --out Recordings/chapters
python Tools/render_chapters.py ... --chapters hook,zeno --shapes vertical     # a subset
python Tools/render_chapters.py ... --mux-only                                  # add narration to existing renders
```

`ChapterRecordingTest` records every chapter on its own from a freshly loaded scene, in 1080x1920 and in 1920x1080 (the
captions switch to a wide layout), and `render_chapters.py` lays the narration on at the frames where each line started.
Output: `Recordings/chapters/NN-<chapter>-vertical.mp4` and `NN-<chapter>-horizontal.mp4` (H.264 + AAC, 30 fps).
Close the editor first, or point `--project` at a copy of the project.

## Reviewing without a window (headless)

`StoryboardRenderTest` is a render driver disguised as a PlayMode test, so the Unity test runner
drives play mode in batch mode (plain `-executeMethod` never ticks the player loop). Close the editor first.

Storyboard: two PNGs per narration line, each line compressed to 3 s of animation (~2 minutes total):

```bash
ZETA_OUT=/tmp/storyboard ZETA_MODE=storyboard ZETA_STORYBOARD_SECONDS=3 ~/Unity/Hub/Editor/6000.6.5f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter StoryboardRenderTest -logFile /tmp/storyboard.log
```

Contact sheets for a quick look:

```bash
ffmpeg -framerate 1 -pattern_type glob -i '/tmp/storyboard/*.png' -vf "scale=360:640,tile=3x2" /tmp/sheet_%02d.png
```

Full video (real time, no narration yet; mux audio afterwards with `Tools/mux_recording.py`):

```bash
ZETA_OUT=/tmp/tour-render ZETA_MODE=record ~/Unity/Hub/Editor/6000.6.5f1/Editor/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter StoryboardRenderTest -logFile /tmp/tour.log
```

`ZETA_CHAPTER=<id>` starts from one chapter; `ZETA_END_CHAPTER=<id>` stops after a chapter; `ZETA_NO_CAPTIONS=1` hides the caption layer (clean frames for thumbnails); `ZETA_PROFILE=1` logs avg/max frame time per line (`ZETA_MODE=profile` runs in real time without capture); `ZETA_FLIP=0/1` overrides the frame flip if a platform renders upside down.

## What the scene shows

- **Output space** (the original visualization): x = Re ζ(s), z = Im ζ(s), y = t·0.3. The spiral at
  height t is the chain of arrows n^-s (the tour calls t "the dial" and Re s "the shrink rate");
  the orange arrow is the Euler–Maclaurin tail that closes it onto ζ(s) exactly
  (see `Assets/Math/ZetaMath.cs`). Yellow markers on the vertical pole are the zeros ("collisions").
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

## Performance notes

Measured with the bundled Mono: Meta.Numerics' `RiemannZeta` costs 4–100 µs per animation step, so the spiral
math is not where frames go. What did cost frames during the chain animation was garbage: arrays, lists and
strings allocated every frame (chain joints, trail copies, arrowheads, arcs, footprints, property blocks, label
text), which trigger GC pauses. Those paths now reuse buffers; TextMesh Pro readouts refresh at 10 Hz and only
when their text changes. Press **F** in Play mode: the hint line shows fps, the worst frame time in the last
second, and GC collections per second. If the worst frame still spikes, open the Unity Profiler on that moment;
the next suspects are TMP rebuilds of the caption line and the Billboard labels.

Measured with `ZETA_MODE=profile` (real time, no capture): every line of the first four chapters averages
0.3–0.7 ms of script+engine time per frame (max 5–14 ms), so the chain animation is not CPU-bound in a headless
run. A frame-rate drop seen in the editor during the bend but not after the tail appears therefore points at
something the editor adds in that state, most likely the dozens of TextMesh Pro labels (joint angles, log bars,
readouts) being rebuilt while they change every frame, or the Scene view / Inspector repainting those objects.
Press **F** for the in-game readout and use the Profiler with 'Deep Profile' off on that moment.

## Chapter GIFs

`python3 Tools/make_chapter_gifs.py Recordings/<run>` cuts one GIF per chapter from a recording using the
chapter marks the recorder writes into `timeline.json` (`--seconds`, `--offset`, `--offsets hook=14,turn=58`).
