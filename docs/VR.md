# Meta Quest 2 (VR, hands only)

The visualization runs on a Quest 2 with **hand tracking only** (no controllers). The real room is the backdrop
(**passthrough**) and the model floats in front of you; one touch turns the room off for a **blackout**. The narrated
tour plays with its captions, and every control is a large icon you push with one finger.

On a Quest 2 the passthrough image is black and white (the colour passthrough cameras are on the Quest 3 / Pro).

## Quick start

1. In the headset: **Settings ▸ Movement tracking ▸ Hand tracking → on**. Turn on developer mode for your account
   and plug the headset in by USB (accept the prompt inside the headset).
2. Build and install (the Unity editor must not have the project open, or build from a copy):

   ```bash
   unity build --target Android --execute-method VRBuildSetup.BuildQuestApk   # or Zeta ▸ VR ▸ Build Quest APK
   python Tools/quest.py install Builds/zeta-vr.apk
   python Tools/quest.py run
   ```

   The app is `com.primitive.zetavisualization` and appears under **Unknown Sources** in the library.
3. Put the headset on. The menu is already in front of you.

`Tools/quest.py run --chapter turn --seconds 90` starts the app straight in a chapter and prints frame-time
statistics from the headset's log (`[VRPERF]` lines, one per narration line).

## Using it

| You want to… | Do this |
|---|---|
| Press a button | Touch it with your index finger and push about 1 cm through its face. It clicks, lights up, and works again when you pull back. |
| Pick a chapter | **Tour** tab: one big button per chapter. Touch the playing one to restart it. |
| Pause, replay, skip | Footer of the menu: **Back / Replay / Pause / Skip / Next**. |
| Move the model | **Pinch** (thumb and index) in the air, away from the menu, and drag. The model stays where you leave it. |
| Scale and turn the model | Pinch with **both hands** and move them apart, together or around each other. |
| Open the menu from anywhere | Turn your **left palm toward your face**: a row of big buttons floats over it (Menu, Pause, Skip, Follow, Room, Dark). |
| Go back to the story's view | **Follow** (pin icon) in the footer or on the palm menu. |
| See the room / go dark | **Room** and **Dark** in the footer or on the palm menu. In blackout a faint floor ring and your hand outlines appear. |
| Look at one visualization | **Views** tab: Chain, Zeros, Strip, Waves, Staircase, Primes, Saddle, Machine. Below them, **Show or hide** toggles for the layers, and a slider for how fast the dial turns. |
| Zoom, turn, tilt, raise, bring closer | **Space** tab. These keep working while the story moves on. |
| Reset | **Space ▸ Reset** (the story's view) or **Re-centre** (use where you stand and look now). |
| Captions, volume, hand outline, gentle motion, model distance | **Settings** tab. |

The menu follows you if you walk or turn away from it for a moment, and it never moves while a finger is near it.

### Comfort

* The tour's camera moves are slowed down and limited to about 50° per second in passthrough and 30° per second in
  blackout, and zoom changes are rate-limited. **Settings ▸ Gentle motion** switches that off.
* When you grab the model yourself the guide stops moving it; **Follow** gives it back.
* The captions drift to your gaze direction only after you have looked 22° away from them.

## Desktop simulation (no headset)

`Zeta ▸ VR ▸ Desktop Simulation (mouse as hands)`, then Play. Move the mouse to move the fingertip, scroll to push it
nearer or farther, hold the left button to poke, **Shift + left button** to pinch, right button + WASD / QE to look and move.
`-vr` on the command line does the same for a standalone build.

`VRPreviewRenderTest` renders the interface to PNGs from scripted hands and asserts on the hover, press, palm menu and
one- and two-handed grab maths:

```bash
ZETA_OUT=/tmp/vr-preview Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter VRPreviewRenderTest
```

## How it works

* **The rig moves, not the model.** Camera, hands, menus and captions live under `XR Rig`; to move, scale or turn the
  model the rig is moved, scaled and turned instead (a rig scaled by 20 makes the model look 20 times smaller; stereo
  separation scales with it). Everything about hands is measured in the user's own metres, whatever the scale.
* **Tour cues become placements.** `VRGuide` takes the 2D tour's camera cue (position, look-at, up) and places the model
  so that a camera at that position would see exactly that picture, with the looked-at point 1 m in front of you
  (Settings ▸ Model distance). Your own zoom / orbit / pan are offsets on top of the cue, so they survive cue changes.
* **Hands** come from the OpenXR hand tracking subsystem (XR Hands). Pinch is thumb-to-index tip distance with
  hysteresis; the palm normal is derived from the joint positions (not the joint rotations) and the left/right
  handedness is handled explicitly.
* **Passthrough** is the Meta OpenXR camera feature: an `ARCameraManager` that is enabled for the room and disabled for
  blackout, with the camera background cleared to alpha 0.
* **Performance.** Single-pass instanced stereo; no post-processing or HDR; 4x MSAA; the Sacks-spiral dots are 80-triangle
  spheres drawn with GPU instancing; the grid's ~1070 tick marks are one mesh. `VRPerf` logs frame times per narration line.

Everything is in `Assets/VR`; `Assets/Editor/VRBuildSetup.cs` configures OpenXR (Meta Quest feature group, hand tracking,
hand interaction profile, passthrough) and the Android player settings (IL2CPP, ARM64, Vulkan, API 29–34).

## Narration and recordings

* `Tools/generate_narration_chatterbox.py` renders all narration with this machine's `narrate` (John's voice,
  `--exaggeration 0.25`), aligns the words with Whisper for the karaoke captions and normalises the loudness.
  See [TOUR.md](TOUR.md).
* `Tools/render_chapters.py` renders every chapter as its own narrated video, vertical (1080×1920) and horizontal
  (1920×1080), from the 2D view.
