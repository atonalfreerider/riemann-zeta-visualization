# The guided tour

A narrated, chaptered camera fly-through of the visualization with karaoke-style captions, framed for
16:9 (1920×1080, monitors) or 9:16 (1080×1920, phones and Shorts). The format follows the screen's shape and
can be switched in the side menu.

## Starting screen and side menu

The app opens on the **home view**: the Sacks spiral bent on its saddle, slowly orbiting, with the master wave of
all 10,000 zeros already riding the spiral (computed once in the background, shown when complete) and the zeta trail
growing out of the saddle's centre from t = 0 (up the screen on desktop and phones, from the floor up in VR), faster
and faster, dropping a marker at every zero, up to t = 1000. The picture holds, then grows again. The tour's last
line ends on the same animation.

The master wave (10,000 zeros × 160,000 samples, 60,000 on the Quest) is baked into `Assets/Resources/MasterWave`
by **Zeta ▸ Bake Master Wave** (`unity run . -- -executeMethod MasterWaveBake.Bake`, about 15 s); computing it at
run time takes minutes. Re-bake after changing `zetazeros.txt` or the sampling in `SacksSpiral.FullWaveLogN`.

The outro chapter shows the zeros one at a time instead (`hypothesis:grow`): every zero the growing trail passes sends
its cosine wave out along the spiral at the zero's height, then the wave sinks onto the saddle and joins the master wave.

The saddle view's camera orbits the saddle's centre with the zeta axis up the screen. It opens very close to the
centre, pulls back while the trail grows, then settles into a slow orbit. Drag (or W A S D / arrows) orbits and changes
altitude, the wheel (or Q E) zooms, Shift + drag (or Shift + W A S D) moves the centre; on a phone one finger orbits,
pinch zooms, two fingers move. The chain of arrows is drawn at the trail's tip as it grows. The saddle's last turns
shrink and fade to black so it reads as going on for ever.

A panel tucks into the left edge (the arrow at the middle of the edge, or **M** / **Tab**):
**View** Saddle · XY graph (the original zeta / staircase graph) — **Layers** saddle or flat, master wave, zeta trail,
chain, numbers, the red / blue / green curves, trail height, reset view — **Format** 16:9 / 9:16 —
**Tour** play the whole tour or any chapter — **Record** the whole tour, or one chapter with the ● on its row,
in the chosen format. The panel is a screen overlay, so it never appears in recordings; while recording, the
window shows the frame being recorded, letterboxed.

## Experiments section

Below the chapter list the side menu has an **Experiments** section: directed explainer modules for the thought
experiments on the saddle (findings and pictures in `docs/experiments/README.md`). Each module runs as a single
chapter through the tour machinery (captions, camera cues, actions) from a clean stage and returns to the home view
when it ends. Their script is `Assets/Resources/Narration/experiments.json`, in the same line format as the tour,
narrated with ElevenLabs JohnV4 like the tour (`python Tools/generate_narration.py --script experiments`). All the
modules are narrated. The modules are

| module | id | what it shows |
|---|---|---|
| Cancellation curves | `xcurves` | the product curves, Euler's line cut by them in two families, the zeta wave along the line |
| Level contours | `xlevels` | the saddle sliced by horizontal planes: hyperbolas, the numbers on them, the trail, the three product families |
| The bend as a probe | `xprobe` | a fixed plane sweeping the product families as the bend goes from flat to twice the tour's |
| Mirrors, not oracles | `xmirror` | the vortex eyes of the chain at a zero, their mirror points, the traces through the zero planes |
| Two counting problems | `xlattice` | the divisor and circle problems' wobbles as charts, their waves added one by one, the saddle twisting from two waves per turn to one |
| Primes of Euler's ring | `xideals` | split and inert primes; needles built from ζ's zeros, then the zeros of L(s, (·/163)) joining |
| Mirror lines | `xmirrorlines` | k² − m² swinging into k² + m²; one pair proves 1601 prime, two pairs factor 221 and 1105; Fermat's walk to 1501 |
| The discriminant comb | `xcomb` | a plane sliding across the comb with its prime count, the prime yield of every plane, Euler's and Landau's lines meeting at 197 and 1601 |
| Pinching the spiral | `xpinch` | the fold and its one comb of every discriminant, the residue arms mod 30 and mod 4 on the saddle, the pleat gathering primes onto k² + k + 11 and k² + k + 17 (leftover composites in white), the residue lattice mod 3, 5, 7 |
| The 12 × 12 torus | `xtorus` | numbers marked with turn and step; the board filling as the primes up to 10⁷ arrive (only 2 and 3 off the 48 gold cells); diagonals swept cell by cell; the board rolled into a torus |
| The saddle computes (3/p) | `xlegendre` | the pinch to 24 arms, then the saddle rising so every prime falls or rises by (3/p); the same for (2/p) on 16 arms, (5/p) on 10, and 144 arms on a 12-fold saddle |
| Base-12 fingerprints | `xfinger` | the last two base-12 digits as a board; the primes filling four columns; the squares, Euler's line, k² + k + 17 and the pronic numbers stamping their cells one value at a time |
| The 144 clock | `xclock` | the 24 half-hours appearing one by one with their product families; the prime-free hours pulsing; 16k² + 11k + 2 beside hour 9; the clock on the saddle |
| Mirrors for prime pairs | `xgoldbach` | Goldbach pairs as chords across the pronic ray and Euler's line, mirror by mirror with a running count; five curves' mirror strength against Hardy and Littlewood's prediction; the chords arriving on the Möbius band of unordered pairs |
| The sieve machine | `xsieve` | Fermat's search through a dial of residue rings (mod 3 to 19) turning one hole per step: 1 501 = 19 × 79 at a = 49, the ring for 19 that never closes, 50 399 = 101 × 499 at a = 300, and the record of every candidate |
| Twisted tori | `xtwist` | a walk along the (5, 7) torus knot of residues with flares at the primes; Euler's line on 12 beads; the spiral winding onto the umbilic torus so turn k lands in position k mod 3; the multiples of 3 of k² + k + 1 and k² + k + 3 on whole stacks; the untwisted torus for contrast |
| Two norm surfaces | `xnorms` | the bowl x² + y² and the saddle x² − y² meeting along the squares; every curve laid on as a slice with its discriminant and prime count; the level sets of 13 and 65 with their factor points and square points |
| Rings through lattices | `xlattices` | the rings of 13, 17, 19 and 23 over the square and triangular lattices (one prime per class mod 12), 1105 and 1729; Euler's lattice drawn ring by ring to 41² |
| A twelve-dimensional torus | `xresidue` | the 144 shadows of the residue torus of 2 … 37 recoloured panel by panel as Euler's line (never on a cross) and the pronic numbers (on every cross) pass through; the 36 shadows close up |
| The modular surface | `xmodular` | the curves' roots in the upper half plane: points, their j-invariants, closed geodesics and the cusp geodesics of the cancellation curves; the fundamental domain close up |
| The lattice race | `xrace` | the race mod 4 rebuilt live from the zeros of Dirichlet's β, with flares at 26 861, 616 841 and 12 306 137; the four lattice classes mod 12 and the near miss at 48 231 785 |
| Dropping onto E8 | `xe8` | E8's roots ring by ring in the Coxeter plane; the Coxeter element walking the 30 hours, the eight hours coprime to 30 lit as they are named; 1 … 600 dropped on the wheel at n mod 30, every prime above 5 on a gold spoke; the tower of spheres of norm 2n with their point counts, primes on the floor 240(1 + n³); one cube per divisor for 6, 7 and 35, and 35 = 5 × 7 recovered from the count |
| E5 to E8, from mirrors | `xebuild` | one mirror and its root; two mirrors at 60° (a third of a turn, six roots) and at 90°; the E-series diagrams growing from E5 (= D5) to E8, and the infinite E9; the roots of E5–E8 on their Coxeter planes (40, 72, 126, 240) with the Coxeter element turning each by one hour; hours = roots ÷ mirrors (8, 12, 18, 30); the E-wheel stack, where 9 and 15 fall off at E6, 25 and 35 at E8, and 49, 77, 91 never |
| The paths back | `xpaths` | E8's wheel lifted into factor columns that grow to the funnel z = log n and are cut by prime; the paths back of 397 (prime), 360 = 2³·3²·5 (drop by drop, the exponents tallied on the board and in the strip below), 256 = 2⁸, 343 = 7³, 210 = 2·3·5·7 and 324 = 2²·3⁴; the six paths unrolled side by side |

Their actions start with `exp:` (`exp:stage`, `exp:bend:B`, `exp:sweep:FROM:TO:SECONDS:H1,H2:families`,
`exp:contours:H1,H2:planes`, `exp:family:squares|quarter|diagonal|all`, `exp:euler:KMAX`, `exp:zetawave`,
`exp:vortexpanel:J`, `exp:tower:A:B[:derotate]`, `exp:towerall[:derotate][:KMAX]`, `exp:chainlive:FROM:TO:SECONDS[:rings][:mirrors]`
(the chain redrawn every frame as the dial turns, its vortex eyes detected live; FROM / TO are dial values, `zN` for zero
N, or `here`), `exp:towergrow:A:B:SECONDS` (the tower grown zero by zero with the chain climbing through the planes),
`exp:snap` (cut to the line's shot instead of flying there), and for the section modules `exp:latticechart[live]`,
`exp:twistlive`, `exp:idealwave`, `exp:mirrorswing`, `exp:eulershow`, `exp:fermatwalk`, `exp:combslide`, `exp:skyline`,
`exp:meet`, the pinches `exp:fold`, `exp:residue`, `exp:pleat`, `exp:residuelattice` and `exp:pinchlive`, the twisted
shapes `exp:goldbachlive`, `exp:goldbachsky`, `exp:mobiusband`, `exp:mobiuspoints`, `exp:mobiuslive`, `exp:sievedial`,
`exp:dialring`, `exp:sieveboard`, `exp:knot`, `exp:knotwalk`, `exp:umbilic`, `exp:umbiliclive`, `exp:umbiliccurve` and
`exp:noboard`, the intersecting geometries `exp:normbase`, `exp:normslice:I,J`, `exp:levelset:N:I`, `exp:ringlattice:N:X`,
`exp:eulerlattice`, `exp:residuetorus:COUNT:CELL[:a;b]`, `exp:residuelive:CODE:SECONDS`,
`exp:modular:UNIT:U:V:CMAX:LABEL:ARCS`, `exp:race:mod4|mod12:W:H:L[:x1,x2]` and `exp:racelive:SECONDS[:x1,x2]`, the generic
`exp:reveal:GROUP:SECONDS` and `exp:hide:GROUP` (a group's items appear one by one, each with its caption-board note and
flares; `exp:reveal:GROUP:T1,T2,…` shows item k at Tk seconds, to land each on the word that names it), the E8 module's `exp:e8wheel`, `exp:e8walk:SECONDS[:T1,…,T8]`, `exp:e8tower`, `exp:e8cubes:N:X:Z:VOXEL:GAP:DOT:LABEL`,
`exp:e8solve:N` and `exp:e8close`, the construction and path modules' `exp:mirrorscene`, `exp:dihedral:M:X:Y`, `exp:dynkin`,
`exp:coxpanels`, `exp:coxwalk:SECONDS`, `exp:estack`, `exp:fcolumns`, `exp:stair:N[:a,b,…]` (n's path back, split into
reveal groups `stair N #k`, with n's column laid on its side in a strip above the captions: a piece lit per drop, the
values at the cuts, the exponent under each run; `exp:strip` hides it) and `exp:descent`, the caption-board line
`exp:say:HEAD|SMALL`, and more,
and `exp:flare:N1,N2[:RADIUS]` (white flares at the numbers a line names, usually with a `delay:`);
see `ExperimentTour.cs`).

**The long-form segments** (horizontal, 1920 × 1080) are written in [`Tools/video_series_spec.py`](../Tools/video_series_spec.py)
and built into `experiments.json` by [`Tools/video_series.py`](../Tools/video_series.py), which times each cue to the word
that names it (`@word|exp:…`, from the narration's word timings, or estimated before it exists). Their scripts are in
[`experiments/long-form-script.md`](experiments/long-form-script.md); they are left off the VR dock.

| Segment | Id | What it shows |
|---|---|---|
| The saddle is a wave | `xs1` | the number line, the primes flaring as they are named; the zeros' wave over it, one zero (crests at 7.4, 11.5, 18, 28) then up to 10 000, spiking at 7, 8 = 2³, 9 = 3² but not at 10 or 12; the line winding into the Sacks spiral (squares on one ray) with its wave; one zero's wave drifting in arms; Voronoi's first wave on its own line (crests at 8.6, 11.8, 15.5), then 4 000 waves, spikes as tall as d(n) (7, 12 with its divisors ringed, 16, 30, 36); wound up, the first wave standing still beside the squares and pronic rays; the plane bending into the saddle; both waves on the saddle |
| E8 sets the vortices | `xs2` | the path of partial sums laid arrow by arrow; t raised to the first zero (the end on 0) and the 159th (vortices, spokes at n ≈ 53, 27, 18); the arrows off E8's gold hours shrinking away; the gold path's spoke charges bar by bar; E8's Coxeter traces as eight-arrow paths (−1, 4, −8); the sieve of 2, 3, 5 on the wheel (7 → 1, 6 → 0) |
| Where the zeros live in E8 | `xs3` | E8's factor funnel, greyed out; 7 and 6 lit with their factors' pieces; the path at the first zero laid on the funnel's floor, its arrows lifted to their numbers' heights and turned into the hands of the coil; the hand of 6 as the hands of 2 and 3; the next zeros' tighter coils; the comb (7, 8, 9 lit; 6, 10 dark); the eight fans (49 on hour 19) |
| What keeps zeros on the line | `xs4` | the three lattices of −23 and the rings of 59, 13, 6; then, side by side and lit in turn, the families' zeros off the line, the L-functions', the sweep over every mixture, the two windows, the plane of −47 and its islands |
| Chebyshev's bias is a fold | `xs5` | the race on E8's spokes; the fold drawn arc by arc (7² → 19, 11² → 1, 13² → 19, 29² → 1); the fair race; the roots of x³ − x − 1 under 59, 2, 5 and a reflection twice; the race between the families, all side by side |
| The path of partial sums | `xz1` | t = 1: arrows 1, 2, 3, … laid with their lengths 1/√n, turns log(n/(n − 1)) and directions log n; t raised to the first zero; the plane standing up on a t axis, ζ(½ + it) leaving its curve through every zero; Riemann's hypothesis |
| How the vortices develop | `xz2` | the climbing path from t = 50 to 333.6 at a fixed scale: vortices born, spokes at n ≈ t/2πk, the eyes' mirrors; the eyes kept at every zero as a tower; 10 000 zeros' eyes down the axis |
| From a zero to E8's heights | `xz3` | the funnel greyed out; 2, 4, 8, 16 lit as stacks of log 2; the path at the first zero on the floor, its arrows lifted into the coil; 4 = 2·2 and 6 = 2·3 as sums of turns; other zeros' coils; t = 18, where the dropped arrows miss |
| Lamps and spokes | `xz4` | three coils; all 10 500 coils at the tops of 7 and 6; the comb (7, 8, 9 lit; 6, 10, 12 dark); the rings; the eight character trees; the eight fans (49 on hour 19; 11 and 41 on hour 11) |
| 240 roots, infinitely many numbers | `xz5` | E8's 240 roots; its clock of 30 hours; the spiral of numbers greyed out with hour 7 (7, 37, 67, 97, 127) and hour 6 lit; the shells of the lattice (240, 2 160, 6 720, 17 520, …); 240σ₃(n) as cubes for 7 and 6; 240 ζ(s) ζ(s − 3) |

Their actions add callouts, `exp:callKIND:TARGET:DX,DY[,FRAC[,RRGGBB]]:TEXT`: a label with an arrow that grows to the
target, blooms white as it lands and rings it (KIND `num`, `wave`, `fun`, `fold`, `e8`, `bar`, `wheel`, `vertex`,
`clock`, `clockend`, `zeta`, `chain`, `at`; DX,DY is the label's direction on the screen, and 0,0 picks one away from the
middle; a label turns away from other callouts and keeps itself in the picture as the camera moves), `exp:uncall`,
`exp:focus:G1,G2[:LEVEL]` and `exp:unfocus` (every other group dimmed), `exp:pulse:G1,G2[:SECONDS[:PEAK]]`,
`exp:drawon:G[/PREFIX]:SECONDS[:seq]`, `exp:hidechild|showchild:GROUP:PREFIX`, the number waves
`exp:nwave:prime|divisor|term|zero|zeros:K:HEIGHT:XMAX[:DX]` and `exp:nwavelive:TERMS:HEIGHT:XMAX:SECONDS[:HOLD]`,
`exp:divisors:N[@KIND]`, `exp:numbers:N`, the zeta plane `exp:zplane`, `exp:zcurve:T1`, `exp:zcurvelive:T0:T1:SECONDS[:chain]`
and `exp:zchain:T:SECONDS`, the clocks `exp:zclocks:N`, `exp:zclockhands:GAMMA[:RRGGBB[:chain]]` and
`exp:zclockslive:GAMMA:FROM:TO:SECONDS[:tail]`, `exp:s3roots`, `exp:mixsweep:LAM0:LAM1:SECONDS`, and `exp:chainlive`'s
`flat` (no t axis, no climbing) and `wide` (framed for 16:9). The continuous scenes are driven by eased parameters,
`exp:tw:KEY:TO:SECONDS[:linear|geo]` (SECONDS may be `line`): the number line `exp:nwinit` (`nw.roll.p|d`, `nw.bend`,
`nw.terms.p|d`, `nw.first.p`, `nw.wave.*`, `nw.count.*`, `nw.h`, …; callouts `exp:callnw:N@p|d`, `exp:nwdivisors`,
`exp:nwflare`), the path of partial sums `exp:pwinit` (`pw.t`, `pw.n`, `pw.marks`, `pw.tail`, `pw.rise`, `pw.curve`,
`pw.eyes`, `pw.mirrors`, `pw.trails`, `pw.alleyes`, `pw.gold`; camera `on=pathplane`; callouts `exp:callpath:K`,
`exp:callpathzeta`) and the coil over the funnel `exp:fwinit` (`fw.gamma`, `fw.n`, `fw.lift`, `fw.coil`, `fw.tail`,
`fw.path`, `fw.hands`, `fw.axis`; `exp:fwhl:N[:logs]`, `exp:fwunhl`, `exp:fwangles:N`, `exp:callhand:N`). `exp:mute` /
`exp:unmute:G1,G2[:SECONDS]` grey groups out and back; `exp:e8hl:N1,N2[:spoke]` lights numbers on E8's wheel; the
stills (`t5…`, `s3roots`, `mixsweep`, `ztrees`, `e8wheel`) take a position so a segment can lay them side by side. A
camera `on=nwboxp|nwboxd` frames a number line by its bounds while it winds up; callouts are sized and placed for the
shot the camera is moving to.

Anything that moves or happens in a module draws the eye with white bloom (`ExperimentAttention.cs`): a moving line
gets a wide white halo that shines while it moves and fades when it settles (the swinging mirror lines, the twisting
squares and pronics, the wave sum as each wave arrives, the plane sliding across the comb and each quadratic it reads,
Fermat's step ring and the curve he finds), events flare white (the square that ends Fermat's search, the numbers the
narration points at), and the growing needles throw white sparks at their tips while zeros join. Their shots are written into the
script as moving orbits, `orbit:on=centre|panel|tower|live|towerfocus;at=x,y,z[>x,y,z];d=FROM>TO;el=FROM>TO;az=A;spin=DEG_PER_S;fov=F;span=S;h=FRAC`
(distance, elevation above the saddle and look point eased over `span` seconds, after waiting `hold=S` seconds, while the camera circles; see
`CameraRig.OrbitCue`). `fit=W,H[>W2,H2]` replaces the distance with the one that frames a W × H box at the current
aspect, so one shot works in 16:9 and 9:16, and `raise=F` lifts the subject by F of the frame's height above the captions, and each module opens already framed on its first shot. To storyboard them headlessly, add `ZETA_SCRIPT=experiments` to the
storyboard command below; to record them as narrated videos, one per module, run
`python Tools/render_chapters.py --unity <Unity.exe> --script experiments --shapes vertical --out <folder>`.

## Running it

| Where | How |
|---|---|
| Unity editor | menu **Zeta ▸ Play Tour** (or enter Play mode and press **T**) |
| Editor, one chapter | **Zeta ▸ Play Tour from Chapter…** |
| Editor, record | **Zeta ▸ Play Tour and Record 1080x1920** |
| Player build | `./zeta-visualization -tour`, `-record`, `-chapter qrh`, `-landscape` / `-portrait` |

Keys in Play mode: **M** side menu · **T** start/stop tour · **N** skip to the next line · **F9** start/stop
recording · **P** switch 16:9 / 9:16 · **F** fps readout.

When the tour's format differs from the screen's shape (a 9:16 tour on a monitor, a 16:9 tour on an upright
phone) the camera is pillarboxed or letterboxed so the preview matches the recording.
**Zeta ▸ Game View – Portrait 1080x1920** adds and selects a matching Game view size.

## Recording

`FrameRecorder` renders the camera into an offscreen 1080×1920 or 1920×1080 texture at a fixed 30 fps
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

The ElevenLabs route (`Tools/generate_narration.py`, voice JohnV4, an API key in `ELEVENLABS_API_KEY`, `.mp3` files) is
the one the current narration uses, and `Tools/mux_recording.py` accepts either format. Its defaults read the lines in a
lively, unhurried way (`--speed 0.95 --stability 0.35 --style 0.45`), and its `PRONOUNCE` table respells names for the
voice only ("Euler" is sent as "Oiler", "cosine" as "co-sign"); the captions keep the script's spelling.

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
| `sacks:sievebegin[:seconds]` | break the grid open onto the spiral; every prime glows, then only the 2 stays lit |
| `sacks:sieveto:N[:seconds]` | make every composite up to N, in order, as p × m (p its smallest prime, nearer the centre; m the number copied, farther out): count m dots, p copies the row up a level at a time, the copies slide up the spiral and only the final dot lands on p × m. A spot the landing jumped over gets an open circle and becomes the next prime. Queued requests run one after another |
| `sacks:sieve[:seconds]` | both of the above in one go, up to 30 |
| `sacks:highlight:squares\|pronic\|euler\|all` | widen one route on the saddle and dim the others |
| `equations:on` / `equations:squares\|pronic\|euler` / `equations:off` | on-screen legend of the saddle's routes (red n = k², blue n = k(k+1), green n = k² + k + 41), optionally highlighting one |
| `hypothesis:grow:TMAX:SECONDS` | the trail grows from the saddle's centre to t = TMAX in SECONDS; each zero's wave runs out along the spiral at its height and sinks into the master wave |
| `stairlab:terms` · `stairlab:const` · `stairlab:trivial` · `stairlab:waves:FROM:TO` | the staircase built from the explicit formula one piece at a time (y = x, − ln 2π, the trivial zeros, then each zero's wave shown alone in a strip and poured in); `TO` may be `all` (300) |
| `stairlab:wobble:ZEROS:BETA` · `stairlab:sweep:FROM:TO[:pingpong]` · `stairlab:beta:B` · `stairlab:off` | the staircase from the first ZEROS zeros on the line (white) against the same zeros moved to Re(s) = β (red), orange bars for the difference |
| `miss:on` / `miss:off` | a line from the pole to the chain's tip with its length, and the closest pass kept on screen |
| `sacks:highlight:…` numbers | highlighting a route writes each of its numbers large at its spot, then shrinks it |
| `home:scene` | the home view's content (saddle + growing trail and waves), without moving the camera (used by the VR Explore view) |
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

To check that cues land on their words, play each line at its narrated length (or its estimate, before it is narrated)
with the camera flying as in the video, and save several frames per line: `ZETA_STORYBOARD_SECONDS=0
ZETA_STORYBOARD_FRAMES=4` writes `NN_<line>_1.png` … `_4.png`, a quarter of the line apart.

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

## Explore mode (after the tour, or from the side menu)

The final picture, free to walk around: the Sacks spiral on the saddle, the zeta trail rising from its centre and
the master wave with every zero in the table (10,000) riding the spiral, computed on a background thread.
Desktop: WASD / QE move, right-drag look, scroll changes speed, **X** flattens the saddle to a plane and back
(saddle is the default); the side menu's **Home** leaves. The wave is displaced along the local surface normal, so it stands on the
saddle in saddle mode and on the plane in flat mode. In VR the dock's Views page has an **Explore** view (you stand
at the saddle's centre with the saddle as the floor and the trail growing from the floor up) and a **Flat** toggle.
Actions: `hypothesis:full[:zeros]` dials every zero into the master wave; `explore:on` enters explore mode when the tour ends.
