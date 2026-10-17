using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scripted camera for the tour. Each cue is a small function of the live scene state
/// (so cues can follow the spiral tip or orbit), evaluated every frame and smoothed.
/// Also enforces a 9:16 portrait viewport (pillarboxed) so previews match the recording.
/// </summary>
public class CameraRig : MonoBehaviour
{
    public struct CueState
    {
        public Vector3 Position;
        public Vector3 LookAt;
        public Vector3 Up;
        public float Fov;
    }

    public const float PortraitAspect = 9f / 16f;

    Camera cam;
    Main main;
    Annotations annotations;
    CameraControl manualControl;

    readonly Dictionary<string, Func<CueState>> cues = new();
    Func<CueState> activeCue;
    string activeCueName = "";
    float cueStartTime;
    float smoothTime = 1f;
    // a scripted orbit cue tightens its smoothing once the cut is done: its own moves are already eased, and a subject
    // that moves (a number line winding up, a plane climbing the t axis) would otherwise leave the camera a second behind
    bool fastFollow;
    Vector3 positionVelocity;
    Vector3 lookVelocity;
    Vector3 currentLook;
    Vector3 currentUp = Vector3.up;
    float fovVelocity;

    public bool Portrait = true;
    public bool Active { get; private set; }
    public string ActiveCue => activeCueName;

    public static CameraRig Create(Camera cam, Main main, Annotations annotations)
    {
        CameraRig rig = cam.gameObject.AddComponent<CameraRig>();
        rig.cam = cam;
        rig.main = main;
        rig.annotations = annotations;
        rig.manualControl = cam.GetComponent<CameraControl>();
        rig.DefineCues();
        return rig;
    }

    float Y(double t) => (float)t * Graph.yScale;
    /// <summary>The aspect the camera renders at (the recording's when offscreen, else the framed viewport's).</summary>
    float Aspect => Mathf.Max(0.3f, cam.aspect);
    /// <summary>Extra distance for cues framed on a 16:9 screen when the frame is tall and narrow.</summary>
    float NarrowWiden => Aspect >= 1f ? 1f : Mathf.Lerp(1.5f, 1f, Mathf.InverseLerp(.5625f, 1f, Aspect));
    float TipY => Y(main.CurrentT);
    Vector3 TipCenter => new(0.8f, TipY, 0.2f);
    float Orbit(float radius, float speed, float phase) => Mathf.Sin(Time.time * speed + phase) * radius;

    void DefineCues()
    {
        Vector3 up = Vector3.up;

        // ---- chapter 1: overview --------------------------------------------------------
        cues["overview_orbit"] = () =>
        {
            float a = Time.time * 0.12f;
            Vector3 center = new(0, Mathf.Max(8f, TipY * .6f), 0);
            return new CueState { Position = center + new Vector3(Mathf.Sin(a) * 24f, 7f, -Mathf.Cos(a) * 24f), LookAt = center, Up = up, Fov = 60 };
        };
        cues["follow_tip"] = () => new CueState { Position = TipCenter + new Vector3(3.2f, 2.2f, -5.5f), LookAt = TipCenter, Up = up, Fov = 60 };
        cues["prime_overview"] = () => new CueState { Position = new Vector3(30, 28, -95), LookAt = new Vector3(30, 30, 0), Up = up, Fov = 60 };
        cues["overview_pull"] = () =>
        {
            float a = Time.time * 0.1f;
            Vector3 center = new(4, 12, 0);
            return new CueState { Position = center + new Vector3(Mathf.Sin(a) * 40f, 10f, -Mathf.Cos(a) * 40f), LookAt = center, Up = up, Fov = 60 };
        };

        // ---- chapter 2: spiral -----------------------------------------------------------
        cues["spiral_close"] = () => new CueState { Position = TipCenter + new Vector3(0.2f, 3.2f, -6.5f), LookAt = TipCenter, Up = up, Fov = 55 };
        cues["spiral_top"] = () => new CueState { Position = TipCenter + new Vector3(0, 8.5f, 0.001f), LookAt = TipCenter, Up = Vector3.forward, Fov = 55 };
        cues["spiral_tail_zoom"] = () =>
        {
            Vector3 center = TailMidpoint();
            return new CueState { Position = center + new Vector3(0, 2.6f, 0.001f), LookAt = center, Up = Vector3.forward, Fov = 50 };
        };

        // ---- chapter 3: axes -------------------------------------------------------------
        cues["axes_wide"] = () => new CueState { Position = new Vector3(-4, 10, -30), LookAt = new Vector3(-4, 8, 0), Up = up, Fov = 60 };
        cues["strip_panel"] = () => new CueState { Position = new Vector3(Annotations.PanelCenterX, 7, -15), LookAt = new Vector3(Annotations.PanelCenterX, 7, 0), Up = up, Fov = 60 };
        cues["strip_vertical"] = () => new CueState { Position = new Vector3(Annotations.PanelCenterX, 12, -34), LookAt = new Vector3(Annotations.PanelCenterX, 14, 0), Up = up, Fov = 60 };
        cues["strip_probe"] = () =>
        {
            Vector3 probe = annotations.StripPoint(0.5, main.CurrentT);
            return new CueState { Position = probe + new Vector3(0, 1.5f, -9), LookAt = probe, Up = up, Fov = 55 };
        };
        cues["output_plane"] = () => new CueState { Position = new Vector3(1.2f, 5.5f, -6.5f), LookAt = new Vector3(1.0f, 0.3f, 0.8f), Up = up, Fov = 60 };
        cues["zero_marker"] = () =>
        {
            Vector3 z = main.ZeroMarkerPosition(0);
            return new CueState { Position = z + new Vector3(2.5f, 1.6f, -4.5f), LookAt = z, Up = up, Fov = 55 };
        };

        // ---- chapter 4: zeros ------------------------------------------------------------
        cues["strip_z"] = () =>
        {
            Vector3 probe = annotations.StripPoint(0.5, main.CurrentT);
            return new CueState { Position = probe + new Vector3(0, 0.5f, -11), LookAt = probe + Vector3.up * 0.5f, Up = up, Fov = 55 };
        };
        cues["strip_z_first"] = () =>
        {
            Vector3 z = annotations.StripPoint(0.5, 14.134725);
            return new CueState { Position = z + new Vector3(0, 0.3f, -8), LookAt = z, Up = up, Fov = 55 };
        };
        cues["zero_ladder"] = () =>
        {
            float h = Mathf.Max(7f, TipY * .55f);
            Vector3 center = new(Annotations.PanelCenterX * .5f, h, 0);
            return new CueState { Position = center + new Vector3(0, 0, -Mathf.Max(18f, TipY * 1.1f)), LookAt = center, Up = up, Fov = 60 };
        };
        cues["zero_ladder_up"] = () =>
        {
            Vector3 center = new(Annotations.PanelCenterX * .5f, TipY, 0);
            return new CueState { Position = center + new Vector3(0, -3, -22), LookAt = center, Up = up, Fov = 60 };
        };

        // ---- chapter 5: primes -----------------------------------------------------------
        cues["cosine_wave"] = () => new CueState { Position = new Vector3(24, 8, -70), LookAt = new Vector3(24, 10, 0), Up = up, Fov = 60 };
        cues["cosine_spikes"] = () => new CueState { Position = new Vector3(8, 8, -32), LookAt = new Vector3(8, 9, 0), Up = up, Fov = 60 };
        cues["formula"] = () => new CueState { Position = new Vector3(2.4f, -1.5f, -13), LookAt = new Vector3(2.4f, -1.5f, 0), Up = up, Fov = 60 };
        cues["formula_close"] = () => new CueState { Position = new Vector3(2.8f, -1.6f, -9), LookAt = new Vector3(2.8f, -1.6f, 0), Up = up, Fov = 60 };
        cues["staircase"] = () => new CueState { Position = new Vector3(28, 24, -78), LookAt = new Vector3(28, 26, 0), Up = up, Fov = 60 };
        cues["staircase_wide"] = () => new CueState { Position = new Vector3(48, 44, -150), LookAt = new Vector3(48, 46, 0), Up = up, Fov = 60 };

        // ---- chapter 6: why one half -------------------------------------------------------
        cues["ghost"] = () => new CueState { Position = new Vector3(52, 46, -120), LookAt = new Vector3(52, 48, 0), Up = up, Fov = 60 };

        // ---- chapter 7: 7/8 --------------------------------------------------------------
        // the band labels reach about 5.6 units either side of the band's centre: back off far enough to keep them in a tall frame
        float StripFit(float near) => Mathf.Max(near, 5.6f / (Mathf.Tan(30f * Mathf.Deg2Rad) * Aspect));
        cues["strip_band"] = () => new CueState { Position = new Vector3(Annotations.PanelCenterX + 1.0f, 9, -StripFit(10f)), LookAt = new Vector3(Annotations.PanelCenterX + 1.0f, 9, 0), Up = up, Fov = 60 };
        cues["strip_band_sym"] = () => new CueState { Position = new Vector3(Annotations.PanelCenterX, 9, -11), LookAt = new Vector3(Annotations.PanelCenterX, 9, 0), Up = up, Fov = 60 };
        cues["strip_band_classic"] = () => new CueState { Position = new Vector3(Annotations.PanelCenterX + 1.2f, 16, -22), LookAt = new Vector3(Annotations.PanelCenterX + 1.2f, 18, 0), Up = up, Fov = 60 };
        cues["strip_gap"] = () => new CueState { Position = new Vector3(Annotations.PanelCenterX + 0.75f, 6, -StripFit(8f)), LookAt = new Vector3(Annotations.PanelCenterX + 0.75f, 6, 0), Up = up, Fov = 60 };

        // ---- from-scratch narrative --------------------------------------------------------
        cues["stage"] = () => new CueState { Position = new Vector3(4.5f, 4.2f, -9f), LookAt = new Vector3(1.2f, 2.6f, 1.2f), Up = up, Fov = 60 };
        cues["stage_floor"] = () => new CueState { Position = new Vector3(1.8f, 8f, -2.5f), LookAt = new Vector3(1.8f, 0f, 1.5f), Up = Vector3.forward, Fov = 60 };
        cues["chain_flat"] = () => new CueState { Position = new Vector3(2.8f, 8.5f, -0.4f), LookAt = new Vector3(2.8f, 0f, 0f), Up = Vector3.right, Fov = 60 };
        cues["chain_long"] = () => new CueState { Position = new Vector3(6.5f, 16f, -1f), LookAt = new Vector3(6.5f, 0f, 0f), Up = Vector3.right, Fov = 60 };
        cues["spiral_curl"] = () => new CueState { Position = new Vector3(0.6f, TipY + 7.5f, -1.6f), LookAt = new Vector3(0.6f, TipY, 0.1f), Up = Vector3.forward, Fov = 58 };
        cues["sigma_spiral"] = () => new CueState { Position = new Vector3(0.3f, TipY + 4.2f, -1.0f), LookAt = new Vector3(0.3f, TipY, 0.1f), Up = Vector3.forward, Fov = 58 };
        cues["tip_stack"] = () =>
        {
            // the top-down view of the chain, widened to take in the stacked turns beside (wide) or below (tall) it
            Vector3 stack = annotations.AngleStackCentre;
            Vector3 chain = new(0.2f, TipY, 0.1f);
            Vector3 mid = Vector3.Lerp(chain, stack.sqrMagnitude > 0f ? stack : chain, .5f);
            float d = Aspect >= 1f ? 6.4f : 8.5f;
            return new CueState { Position = mid + new Vector3(0, d, -0.4f), LookAt = mid, Up = Vector3.forward, Fov = 58 };
        };
        cues["tip_center"] = () => new CueState { Position = new Vector3(0.25f, TipY + 4.6f, -1.1f), LookAt = new Vector3(0.2f, TipY, 0.1f), Up = Vector3.forward, Fov = 58 };
        cues["markers_axis"] = () =>
        {
            float h = Mathf.Max(5f, TipY * .55f + 1.5f);
            float d = Mathf.Max(9f, TipY * .95f);
            return new CueState { Position = new Vector3(d * .45f, h + 1.5f, -d), LookAt = new Vector3(0, h, 0), Up = up, Fov = 60 };
        };
        cues["markers_axis_wide"] = () =>
        {
            float h = Mathf.Max(6f, TipY * .5f);
            float d = Mathf.Max(14f, TipY * 1.1f);
            float a = Time.time * 0.15f;
            return new CueState { Position = new Vector3(Mathf.Sin(a) * d, h + 2f, -Mathf.Cos(a) * d), LookAt = new Vector3(0, h, 0), Up = up, Fov = 60 };
        };
        // ---- experiments (the side menu's Experiments section; ExperimentTour.cs) ---------------------
        Vector3 back = Vector3.back;
        Vector3 Sc() => annotations.Sacks.Center;
        Vector3 Panel() => Sc() + new Vector3(SacksSpiral.ExpPanelOffset, 0f, 0f);
        Vector3 Tower() => Sc() + new Vector3(SacksSpiral.ExpTowerOffset, 0f, 0f);
        float TowerH() => Mathf.Max(1f, annotations.Sacks.ExpTowerHeight);
        cues["exp_full"] = () => new CueState { Position = Sc() + back * 205f, LookAt = Sc(), Up = up, Fov = 55 };
        cues["exp_centre"] = () => new CueState { Position = Sc() + back * 100f, LookAt = Sc(), Up = up, Fov = 55 };
        cues["exp_core"] = () => new CueState { Position = Sc() + back * 40f, LookAt = Sc(), Up = up, Fov = 50 };
        cues["exp_oblique"] = () => new CueState { Position = Sc() + new Vector3(-40f, -70f, -45f), LookAt = Sc() + back * 2f, Up = back, Fov = 60 };
        cues["exp_side_y"] = () => new CueState { Position = Sc() + new Vector3(0f, -200f, 15f), LookAt = Sc() + new Vector3(0f, 0f, 15f), Up = back, Fov = 55 };
        cues["exp_side_x"] = () => new CueState { Position = Sc() + new Vector3(-200f, 0f, 15f), LookAt = Sc() + new Vector3(0f, 0f, 15f), Up = back, Fov = 55 };
        cues["exp_levels_oblique"] = () => new CueState { Position = Sc() + new Vector3(-150f, -150f, -90f), LookAt = Sc() + back * 4f, Up = back, Fov = 55 };
        cues["exp_levels_side"] = () => new CueState { Position = Sc() + new Vector3(0f, -210f, -10f), LookAt = Sc() + back * 4f, Up = back, Fov = 55 };
        cues["exp_flat_wide"] = () => new CueState { Position = Sc() + new Vector3(12f, -58f, -215f), LookAt = Sc() + new Vector3(12f, -58f, 0f), Up = up, Fov = 55 };
        cues["exp_flat_close"] = () => new CueState { Position = Sc() + new Vector3(44f, -62f, -100f), LookAt = Sc() + new Vector3(44f, -62f, 0f), Up = up, Fov = 55 };
        cues["exp_flat_first"] = () => new CueState { Position = Sc() + new Vector3(58f, -16f, -60f), LookAt = Sc() + new Vector3(58f, -16f, 0f), Up = up, Fov = 55 };
        cues["exp_panel"] = () => new CueState { Position = Panel() + back * 48f, LookAt = Panel(), Up = up, Fov = 50 };
        cues["exp_tower_oblique"] = () => new CueState { Position = Tower() + new Vector3(-34f, -52f, -TowerH() * .5f), LookAt = Tower() + back * (TowerH() * .5f), Up = back, Fov = 55 };
        cues["exp_tower_side"] = () => new CueState { Position = Tower() + new Vector3(0f, -64f, -TowerH() * .5f), LookAt = Tower() + back * (TowerH() * .5f), Up = back, Fov = 55 };
        cues["exp_tower_top"] = () => new CueState { Position = Tower() + back * (TowerH() + 40f), LookAt = Tower() + back * TowerH(), Up = up, Fov = 45 };
        cues["exp_all_down"] = () =>
        {
            // all ten thousand zeros looked at down the axis (the stack is flat, so an ordinary field of view hardly smears the rings)
            Vector3 f = Tower() + annotations.Sacks.ExpTowerFocus;
            return new CueState { Position = f + back * (TowerH() + 39f), LookAt = f, Up = up, Fov = 30f };
        };

        cues["wave"] = () =>
        {
            float w = annotations.WaveViewMax;
            return new CueState { Position = new Vector3(w * .5f, 0f, -w * 1.8f), LookAt = new Vector3(w * .5f, 0f, 0f), Up = up, Fov = 60 };
        };
        cues["spikes"] = () =>
        {
            float w = annotations.WaveViewMax;
            return new CueState { Position = new Vector3(w * .5f, 2f, -w * 1.5f), LookAt = new Vector3(w * .5f, 2f, 0f), Up = up, Fov = 60 };
        };
        cues["spikes_close"] = () =>
        {
            // close in on x = 1 … 17: the spikes on 2, 3, 5, 7, 11, 13 and the faint ones on 4, 9, 16 are readable
            float d = Mathf.Max(15f, 8.8f / (Mathf.Tan(30f * Mathf.Deg2Rad) * Aspect));
            return new CueState { Position = new Vector3(9f, 1.5f, -d), LookAt = new Vector3(9f, 1.5f, 0f), Up = up, Fov = 60 };
        };
        cues["staircase_close"] = () => new CueState { Position = new Vector3(9f, 7f, -24f), LookAt = new Vector3(9f, 7f, 0f), Up = up, Fov = 60 };
        cues["wobble"] = () => new CueState { Position = new Vector3(30f, 30f, -82f), LookAt = new Vector3(30f, 30f, 0f), Up = up, Fov = 60 };
        cues["primes_grid"] = () => new CueState { Position = new Vector3(-34f, 18.5f, -24f), LookAt = new Vector3(-34f, 18.5f, 0f), Up = up, Fov = 60 };
        cues["lattice"] = () => new CueState { Position = new Vector3(-31f, 15.5f, -27f), LookAt = new Vector3(-31f, 15.5f, 0f), Up = up, Fov = 60 };
        cues["sieve"] = () =>
        {
            // never still: a slow orbit around the spiral's centre, looking down from a little above its plane, with the
            // spiral's normal as screen-up so the rows of dots stack upward; far enough that 1..30 fits either frame shape
            float a = Time.time * 0.07f - 1.9f;
            Vector3 toCamera = new(Mathf.Cos(a), Mathf.Sin(a), 0f);
            // aim a little in front of the centre and low, so the spiral sits above the caption bar
            Vector3 c = annotations.Sacks.Center + Vector3.back * .8f + toCamera * 3.2f;
            const float tilt = 54f * Mathf.Deg2Rad; // from the normal: low enough that "one level up" reads as up
            float d = Mathf.Max(15f, 10.5f / (Mathf.Tan(30f * Mathf.Deg2Rad) * Aspect));
            Vector3 pos = c + toCamera * (d * Mathf.Sin(tilt)) + Vector3.back * (d * Mathf.Cos(tilt));
            return new CueState { Position = pos, LookAt = c, Up = Vector3.back, Fov = 60 };
        };
        cues["route_follow"] = () =>
        {
            // ride along a route on the saddle while its numbers are written: above the surface on the side the ridge
            // rises toward (+Z), set back toward the centre so the route runs away from the camera into the frame
            SacksSpiral s = annotations.Sacks;
            Vector3 f = s.RouteFocus;
            Vector3 radial = f - s.Center;
            radial.z = 0f;
            float r = radial.magnitude;
            Vector3 outward = r > .5f ? radial / r : new Vector3(s.RouteHeading.x, s.RouteHeading.y, 0f).normalized;
            if (outward.sqrMagnitude < .1f) outward = Vector3.right;
            float d = (Aspect >= 1f ? 20f : 30f) + r * .3f;
            Vector3 pos = f + Vector3.forward * (d * .72f) - outward * (d * .6f);
            return new CueState { Position = pos, LookAt = f + outward * 3f, Up = Vector3.forward, Fov = 60 };
        };
        cues["stair_terms"] = () =>
        {
            // the terms view: x = 1 … 30 with the strip of the wave being added above; the graph's foot stays clear of the captions
            float halfHeight = Mathf.Max(22.5f, 16.5f / Aspect);
            float lookY = halfHeight * .55f;
            float d = halfHeight / Mathf.Tan(30f * Mathf.Deg2Rad);
            float sway = Mathf.Sin(Time.time * .2f) * 1.2f;
            return new CueState { Position = new Vector3(15.5f + sway, lookY, -d), LookAt = new Vector3(15.5f, lookY, 0f), Up = up, Fov = 60 };
        };
        cues["stair_wobble"] = () =>
        {
            float halfHeight = Mathf.Max(50f, 31f / Aspect);
            float lookY = halfHeight * .55f;
            float d = halfHeight / Mathf.Tan(30f * Mathf.Deg2Rad);
            float sway = Mathf.Sin(Time.time * .2f) * 2f;
            return new CueState { Position = new Vector3(31f + sway, lookY, -d), LookAt = new Vector3(31f, lookY, 0f), Up = up, Fov = 60 };
        };
        cues["home_orbit"] = () =>
        {
            // the home view: orbit the saddle with its normal as screen-up, so the growing zeta trail rises up the
            // screen; the look point climbs with the trail and the distance fits the saddle to the frame shape
            SacksSpiral s = annotations.Sacks;
            float h = s.GrowthHeight;
            float a = Time.time * 0.045f + 2.2f;
            // frame the part of the saddle the waves ride on (n ≤ 420, radius ~30); the outer spiral runs off the edges
            const float elevation = 34f * Mathf.Deg2Rad;
            float tan = Mathf.Tan(30f * Mathf.Deg2Rad);
            float radius = s.GrowthFrameRadius;
            float dWidth = radius * 1.05f / (tan * Mathf.Min(1f, Aspect));
            float dHeight = (radius * .9f + h * Mathf.Cos(elevation) * .6f + 4f) / tan;
            float dNear = 2.6f * radius / Mathf.Cos(elevation); // the rim rises ~56 units: keep it well away from the lens
            float d = Mathf.Max(dNear, Mathf.Max(dWidth, dHeight));
            Vector3 look = s.Center + Vector3.back * (h * .42f);
            Vector3 horizontal = new(Mathf.Cos(a), Mathf.Sin(a), 0f);
            Vector3 pos = look + horizontal * (d * Mathf.Cos(elevation)) + Vector3.back * (d * Mathf.Sin(elevation));
            return new CueState { Position = pos, LookAt = look, Up = Vector3.back, Fov = 60 };
        };
        cues["sacks_flat"] = () =>
        {
            Vector3 c = annotations.Sacks.Center;
            return new CueState { Position = c + new Vector3(0, -2f, -annotations.Sacks.Extent * 1.75f * NarrowWiden), LookAt = c, Up = up, Fov = 60 };
        };
        cues["sacks_saddle"] = () =>
        {
            // sway around the front of the saddle so its curvature reads in parallax
            Vector3 c = annotations.Sacks.Center;
            float a = Time.time * 0.25f;
            float d = annotations.Sacks.Extent * 2.3f * NarrowWiden;
            // swing well to the side and above so the saddle's rise and fall is unmistakable
            return new CueState { Position = c + new Vector3(Mathf.Sin(a) * d * .85f, .45f * d + Mathf.Cos(a * .7f) * d * .25f, -Mathf.Abs(Mathf.Cos(a)) * d * .55f - d * .25f), LookAt = c, Up = up, Fov = 60 };
        };
        cues["sacks_routes"] = () =>
        {
            // a slow orbit over the saddle from the side its ridge rises toward (+Z), so the squares (red) and the
            // neighbour products (blue) run along a ridge that visibly climbs, and Euler's green curve sweeps between
            // the rim of the ridge rises ~56 units toward the camera, so stay far enough back that it never fills the lens
            Vector3 c = annotations.Sacks.Center + Vector3.forward * 6f;
            float a = Time.time * 0.05f + 3.6f;
            const float elevation = 42f * Mathf.Deg2Rad;
            float d = annotations.Sacks.Extent * (Aspect >= 1f ? 2.7f : 3.6f);
            Vector3 pos = c + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * (d * Mathf.Cos(elevation)) + Vector3.forward * (d * Mathf.Sin(elevation));
            return new CueState { Position = pos, LookAt = c, Up = Vector3.forward, Fov = 60 };
        };
        cues["sacks_3d"] = () =>
        {
            // a slow orbit around the saddle normal, looking slightly down at the surface and the trail rising from it
            Vector3 c = annotations.Sacks.Center + Vector3.back * 6f;
            float a = Time.time * 0.16f;
            float d = annotations.Sacks.Extent * 1.5f;
            return new CueState { Position = annotations.Sacks.Center + new Vector3(Mathf.Sin(a) * d, Mathf.Cos(a) * d, -d * .9f), LookAt = c, Up = Vector3.back, Fov = 60 };
        };
        cues["sacks_wave"] = () =>
        {
            // landscape: the saddle fills the lower frame, the trail and the waves at height above it; slow orbit
            Vector3 c = annotations.Sacks.Center;
            float d = annotations.Sacks.Extent * 1.45f;
            float a = Time.time * 0.12f;
            Vector3 pos = c + new Vector3(Mathf.Sin(a) * d, Mathf.Cos(a) * d, -d * .62f);
            return new CueState { Position = pos, LookAt = c + Vector3.back * 11f, Up = Vector3.back, Fov = 60 };
        };
        cues["sacks_wave_close"] = () =>
        {
            Vector3 c = annotations.Sacks.Center;
            float d = annotations.Sacks.Extent * 0.95f;
            float a = Time.time * 0.1f + 1.2f;
            Vector3 pos = c + new Vector3(Mathf.Sin(a) * d, Mathf.Cos(a) * d, -d * .75f);
            return new CueState { Position = pos, LookAt = c + Vector3.back * 1.5f, Up = Vector3.back, Fov = 55 };
        };
        cues["master_top"] = () =>
        {
            // the clear view: straight down at the centre from close; the wave's normal displacement reads through perspective
            Vector3 c = annotations.Sacks.Center;
            float drift = Mathf.Sin(Time.time * 0.2f) * 0.6f;
            return new CueState { Position = c + Vector3.back * 12f + new Vector3(drift, 0, 0), LookAt = c, Up = Vector3.up, Fov = 50 };
        };
        cues["master_top_pull"] = () =>
        {
            // pull straight up as the wave fills with zeros and the spiral extends
            Vector3 c = annotations.Sacks.Center;
            float u = Mathf.Clamp01((Time.time - cueStartTime) / 22f);
            float d = Mathf.Lerp(12f, 95f, Mathf.SmoothStep(0f, 1f, u));
            annotations.Sacks.FullWaveAmplitude = Mathf.Clamp(d * 0.17f, 1.5f, 14f);
            return new CueState { Position = c + Vector3.back * d, LookAt = c, Up = Vector3.up, Fov = 50 };
        };
        cues["sacks_sheet"] = () =>
        {
            // portrait framing of the fringe sheet standing on the ridge: front view, slight drift
            Vector3 c = annotations.Sacks.Center;
            Vector3 mid = c + new Vector3(1.45f * Mathf.Sqrt(60f) * .5f, 0f, 0f) + Vector3.back * 8.5f;
            float sway = Mathf.Sin(Time.time * 0.25f) * 3f;
            return new CueState { Position = mid + new Vector3(sway, -34f, -2f), LookAt = mid, Up = Vector3.back, Fov = 55 };
        };
        cues["sacks_track"] = () =>
        {
            // tracking shot: ride outward along the spiral just above the master wave, looking ahead along the road
            Vector3 c = annotations.Sacks.Center;
            float u = Mathf.Clamp01((Time.time - cueStartTime) / 18f);
            float n = Mathf.Lerp(3f, 330f, u * u);
            float r = 1.45f * Mathf.Sqrt(n), a = 2f * Mathf.PI * Mathf.Sqrt(n);
            float nAhead = n * 1.25f + 2f;
            float r2 = 1.45f * Mathf.Sqrt(nAhead), a2 = 2f * Mathf.PI * Mathf.Sqrt(nAhead);
            const float k = 1f / 48f;
            Vector3 here = c + new Vector3(r * Mathf.Cos(a), r * Mathf.Sin(a), k * (r * r * Mathf.Cos(2 * a)));
            Vector3 ahead = c + new Vector3(r2 * Mathf.Cos(a2), r2 * Mathf.Sin(a2), k * (r2 * r2 * Mathf.Cos(2 * a2)));
            // ride well above and behind the road so a whole turn of the spiral stays in frame
            Vector3 pos = here + Vector3.back * (11f + r * .35f) - (ahead - here).normalized * (10f + r * .2f);
            return new CueState { Position = pos, LookAt = ahead + Vector3.back * .6f, Up = Vector3.back, Fov = 60 };
        };
        cues["zeno_zoom"] = () => new CueState { Position = annotations.ZenoFocus + new Vector3(0, annotations.ZenoZoom, -annotations.ZenoZoom * .04f), LookAt = annotations.ZenoFocus, Up = Vector3.right, Fov = 60 };
        cues["spiral_angles"] = () => new CueState { Position = new Vector3(-0.4f, TipY + 6.5f, -1.8f), LookAt = new Vector3(-0.4f, TipY, 0.1f), Up = Vector3.forward, Fov = 58 };
        cues["zeno"] = () => new CueState { Position = new Vector3(2.4f, 9.5f, 0.3f), LookAt = new Vector3(2.4f, 0f, 0.55f), Up = Vector3.right, Fov = 60 };
        cues["machine"] = () =>
        {
            // frame the pole with its markers (left), the staircase (right) and the wave chorus (bottom) together,
            // with a gentle lateral sway instead of a full orbit so the flat graphs never go edge-on
            float a = Time.time * 0.15f;
            float height = Mathf.Max(60f, TipY + 10f);
            float d = height * 1.6f;
            Vector3 center = new(height * .42f, height * .47f, 0);
            return new CueState { Position = center + new Vector3(Mathf.Sin(a) * d * .25f, d * .18f, -d), LookAt = center, Up = up, Fov = 60 };
        };
        cues["outro"] = () =>
        {
            float a = Time.time * 0.1f;
            float radius = 28f + Mathf.Clamp01((Time.time - cueStartTime) / 14f) * 30f;
            Vector3 center = new(8, Mathf.Max(10f, TipY * .5f), 0);
            return new CueState { Position = center + new Vector3(Mathf.Sin(a) * radius, radius * .3f, -Mathf.Cos(a) * radius), LookAt = center, Up = up, Fov = 60 };
        };

        // ---- chapter 8: outro --------------------------------------------------------------
        cues["overview_pull_final"] = () =>
        {
            float a = Time.time * 0.1f;
            float radius = 30f + Mathf.Clamp01((Time.time - cueStartTime) / 12f) * 40f;
            Vector3 center = new(6, 14, 0);
            return new CueState { Position = center + new Vector3(Mathf.Sin(a) * radius, radius * .35f, -Mathf.Cos(a) * radius), LookAt = center, Up = up, Fov = 60 };
        };
    }

    Vector3 TailMidpoint()
    {
        ZetaMath.SpiralDecomposition d = main.Limit.LastDecomposition;
        if (d.PartialSums == null) return TipCenter;
        float y = Y(main.Limit.LastT);
        return (LimitCurve.ToWorld(d.SpiralEnd, y) + LimitCurve.ToWorld(d.Total, y)) * .5f;
    }

    public bool HasCue(string name) => cues.ContainsKey(name);

    /// <summary>Evaluate a named cue right now (used by the VR interface to fly to a view).</summary>
    public bool TryEvaluate(string name, out CueState state)
    {
        if (cues.TryGetValue(name, out Func<CueState> cue))
        {
            state = cue();
            return true;
        }

        state = default;
        return false;
    }

    public void SetCue(string name, float transitionSeconds = 2.5f)
    {
        if (string.IsNullOrEmpty(name)) return;
        if (!cues.TryGetValue(name, out Func<CueState> cue))
        {
            if (name.StartsWith("orbit:"))
            {
                var (at, settle) = OrbitCueAt(name.Substring(6));
                orbitCues[name] = (at, settle);
                cue = () => at(Time.time - cueStartTime);
                cues[name] = cue;
            }
            else
            {
                Debug.LogWarning($"CameraRig: unknown cue '{name}'");
                return;
            }
        }

        if (!Active)
        {
            Activate();
        }

        activeCue = cue;
        activeCueName = name;
        cueStartTime = Time.time;
        smoothTime = Mathf.Max(0.15f, transitionSeconds * 0.4f);
        fastFollow = name.StartsWith("orbit:");
    }

    /// <summary>
    /// A moving shot written into the script itself, for the experiment modules: "orbit:on=centre;at=x,y,z;d=210>180;
    /// el=60>35;az=-90;spin=3;fov=55;span=12;h=.5". It circles a point on the saddle's normal (−Z) at elevation el
    /// (degrees above the saddle; 90 looks straight down) and distance d, turning at spin degrees per second from az;
    /// "A>B" eases a value from A to B over span seconds (distance, elevation, and the look point's at). on: centre
    /// (the saddle), panel, tower, live (the tower plane the live chain is climbing through) or towerfocus; h raises
    /// the look point by that fraction of the current tower's height. fit=W,H (or W,H>W2,H2) replaces d with the distance that
    /// frames a W × H box (world units, across × up the screen) at the camera's current aspect, so one shot works in 16:9 and
    /// 9:16; raise=F moves the subject up the screen by F of the frame's height (to clear the captions).
    /// </summary>
    // the orbit cues as functions of the cue's age, and the age at which each has finished moving (hold + span)
    readonly Dictionary<string, (Func<float, CueState> at, float settle)> orbitCues = new();

    Func<CueState> OrbitCue(string spec)
    {
        var (at, _) = OrbitCueAt(spec);
        return () => at(Time.time - cueStartTime);
    }

    (Func<float, CueState> at, float settle) OrbitCueAt(string spec)
    {
        Dictionary<string, string> p = new();
        foreach (string part in spec.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            int eq = part.IndexOf('=');
            if (eq > 0) p[part.Substring(0, eq).Trim().ToLowerInvariant()] = part.Substring(eq + 1).Trim();
        }

        static float Num(string s, float d) => float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : d;
        (float a, float b) Range(string key, float d)
        {
            if (!p.TryGetValue(key, out string s)) return (d, d);
            string[] ab = s.Split('>');
            float a = Num(ab[0], d);
            return (a, ab.Length > 1 ? Num(ab[1], a) : a);
        }

        Vector3 Vec(string s)
        {
            string[] c = s.Split(',');
            return new Vector3(c.Length > 0 ? Num(c[0], 0f) : 0f, c.Length > 1 ? Num(c[1], 0f) : 0f, c.Length > 2 ? Num(c[2], 0f) : 0f);
        }

        (Vector3 a, Vector3 b) At()
        {
            if (!p.TryGetValue("at", out string s)) return (Vector3.zero, Vector3.zero);
            string[] ab = s.Split('>');
            Vector3 a = Vec(ab[0]);
            return (a, ab.Length > 1 ? Vec(ab[1]) : a);
        }

        string on = p.TryGetValue("on", out string o) ? o.ToLowerInvariant() : "centre";
        (float d0, float d1) = Range("d", 200f);
        (float e0, float e1) = Range("el", 45f);
        (Vector3 at0, Vector3 at1) = At();
        float az0 = Num(p.TryGetValue("az", out string az) ? az : "", -90f);
        float spin = Num(p.TryGetValue("spin", out string sp) ? sp : "", 3f);
        float fov = Num(p.TryGetValue("fov", out string fv) ? fv : "", 55f);
        float span = Mathf.Max(.1f, Num(p.TryGetValue("span", out string sn) ? sn : "", 12f));
        float hold = Num(p.TryGetValue("hold", out string hd) ? hd : "", 0f); // seconds to wait before moving from the first value to the second
        float hFrac = Num(p.TryGetValue("h", out string hh) ? hh : "", 0f);
        float raise = Num(p.TryGetValue("raise", out string rs) ? rs : "", 0f);
        Vector2 fit0 = Vector2.zero, fit1 = Vector2.zero;
        if (p.TryGetValue("fit", out string ft))
        {
            string[] ab = ft.Split('>');
            Vector2 Box(string s) { string[] c = s.Split(','); return new Vector2(Num(c[0], 50f), c.Length > 1 ? Num(c[1], 50f) : Num(c[0], 50f)); }
            fit0 = Box(ab[0]);
            fit1 = ab.Length > 1 ? Box(ab[1]) : fit0;
        }

        float settleAge = hold + span;
        return ((float age) =>
        {
            SacksSpiral s = annotations.Sacks;
            Vector3 c = s.Center;
            Vector3 basePoint = on switch
            {
                "panel" => c + new Vector3(SacksSpiral.ExpPanelOffset, 0f, 0f),
                "tower" => c + new Vector3(SacksSpiral.ExpTowerOffset, 0f, 0f),
                "live" => c + new Vector3(SacksSpiral.ExpTowerOffset, 0f, 0f) + Vector3.back * s.ExpLiveHeight,
                "panellive" => c + new Vector3(SacksSpiral.ExpPanelOffset, 0f, 0f) + Vector3.back * s.ExpPanelHeight,
                "towerfocus" => c + new Vector3(SacksSpiral.ExpTowerOffset, 0f, 0f) + s.ExpTowerFocus + Vector3.back * Mathf.Max(1f, s.ExpTowerHeight),
                "pathplane" => s.PathFocus,
                _ => c
            };
            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((age - hold) / span));
            Vector3 look = basePoint + Vector3.Lerp(at0, at1, u) + Vector3.back * (hFrac * Mathf.Max(1f, s.ExpTowerHeight));
            float d = Mathf.Lerp(d0, d1, u);
            float tanV = Mathf.Tan(fov * .5f * Mathf.Deg2Rad);
            float el = Mathf.Clamp(Mathf.Lerp(e0, e1, u), -80f, 89.5f) * Mathf.Deg2Rad;
            float a = (az0 + spin * age) * Mathf.Deg2Rad;
            Vector3 hdir = new(Mathf.Cos(a), Mathf.Sin(a), 0f);
            Vector2 f0 = fit0, f1 = fit1;
            if (on.StartsWith("nwbox"))
            {
                // follow a number-line lane's bounds as it winds up: frame the box as the camera sees it, blended from the
                // cue's own shot (fit, at) towards it by `follow`
                Bounds b = s.NwBounds(on.EndsWith("d") ? "d" : "p");
                Vector3 upV = (Vector3.back * Mathf.Cos(el) - hdir * Mathf.Sin(el)).normalized;
                Vector3 fwd = -(hdir * Mathf.Cos(el) + Vector3.back * Mathf.Sin(el));
                Vector3 rightV = Vector3.Cross(upV, fwd).normalized;
                float w = 0f, hgt = 0f;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = new(i % 2 == 0 ? b.min.x : b.max.x, (i / 2) % 2 == 0 ? b.min.y : b.max.y, i / 4 == 0 ? b.min.z : b.max.z);
                    Vector3 rel = corner - b.center;
                    w = Mathf.Max(w, 2f * Mathf.Abs(Vector3.Dot(rel, rightV)));
                    hgt = Mathf.Max(hgt, 2f * Mathf.Abs(Vector3.Dot(rel, upV)));
                }

                float follow = Num(p.TryGetValue("follow", out string fo) ? fo : "", 1f);
                look = Vector3.Lerp(look, b.center, follow);
                Vector2 own = Vector2.Lerp(fit0, fit1, u);
                // the height goes in the band between the caption board and the captions (.57 of the frame; raise .035)
                Vector2 boxed = new(Mathf.Max(w * 1.12f, 30f), Mathf.Max(hgt / .6f, 17f));
                f0 = f1 = own.x > 0f ? Vector2.Lerp(own, boxed, follow) : boxed;
            }

            if (f0.x > 0f)
            {
                Vector2 box = Vector2.Lerp(f0, f1, u);
                d = Mathf.Max(box.x * .5f / (tanV * Aspect), box.y * .5f / tanV) * 1.06f;
            }

            Vector3 position = look + (hdir * Mathf.Cos(el) + Vector3.back * Mathf.Sin(el)) * d;
            // "up the screen" is the saddle's normal tipped back by the elevation, so a shot can pass overhead smoothly
            Vector3 upDir = (Vector3.back * Mathf.Cos(el) - hdir * Mathf.Sin(el)).normalized;
            if (raise != 0f)
            {
                Vector3 shift = -upDir * (raise * 2f * d * tanV);
                position += shift;
                look += shift;
            }

            return new CueState { Position = position, LookAt = look, Up = upDir, Fov = fov };
        }, settleAge);
    }

    /// <summary>Where the active cue wants the camera now (the camera itself may still be flying there).</summary>
    public bool TargetPose(out Vector3 position, out Quaternion rotation, out float fov, bool settled = false)
    {
        position = cam != null ? cam.transform.position : Vector3.zero;
        rotation = cam != null ? cam.transform.rotation : Quaternion.identity;
        fov = cam != null ? cam.fieldOfView : 55f;
        if (activeCue == null || VRMode.Enabled) return false;
        // settled: where an orbit cue comes to rest (the end of its move), else where it is now
        CueState target = settled && orbitCues.TryGetValue(activeCueName, out var oc)
            ? oc.at(Mathf.Max(Time.time - cueStartTime, oc.settle))
            : activeCue();
        Vector3 forward = target.LookAt - target.Position;
        if (forward.sqrMagnitude < 1e-6f) return false;
        position = target.Position;
        rotation = Quaternion.LookRotation(forward, target.Up);
        fov = target.Fov;
        return true;
    }

    /// <summary>Jump straight to the active cue's current target (no smoothing).</summary>
    public void Snap()
    {
        if (activeCue == null) return;
        CueState target = activeCue();
        if (VRMode.Enabled)
        {
            // the head is the camera; the guide moves the model instead
            if (VRMode.Guide != null) VRMode.Guide.SetCue(target, true);
            return;
        }

        cam.transform.position = target.Position;
        currentLook = target.LookAt;
        Vector3 forward = target.LookAt - target.Position;
        currentUp = target.Up;
        if (forward.sqrMagnitude > 1e-6f) cam.transform.rotation = Quaternion.LookRotation(forward, target.Up);
        cam.fieldOfView = target.Fov;
        positionVelocity = Vector3.zero;
        lookVelocity = Vector3.zero;
        fovVelocity = 0f;
    }

    public void Activate()
    {
        Active = true;
        if (manualControl != null) manualControl.enabled = false;
        currentLook = cam.transform.position + cam.transform.forward * 10f;
        currentUp = cam.transform.up;
        positionVelocity = Vector3.zero;
        lookVelocity = Vector3.zero;
    }

    public void Release()
    {
        Active = false;
        activeCue = null;
        activeCueName = "";
        if (manualControl != null && !VRMode.Enabled) manualControl.enabled = true;
    }

    void LateUpdate()
    {
        if (VRMode.Enabled)
        {
            // never move the head-tracked camera: hand the cue to the guide, which places the model in front of the user
            if (Active && activeCue != null && VRMode.Guide != null) VRMode.Guide.SetCue(activeCue());
            return;
        }

        ApplyPortraitViewport();

        if (!Active || activeCue == null) return;

        CueState target = activeCue();
        // never fully static: a slow figure-eight drift on top of every cue
        float drift = 0.012f * Vector3.Distance(target.Position, target.LookAt);
        target.Position += new Vector3(Mathf.Sin(Time.time * 0.37f), Mathf.Sin(Time.time * 0.23f) * .6f, Mathf.Cos(Time.time * 0.31f)) * drift;
        Transform t = cam.transform;
        float st = fastFollow ? Mathf.Lerp(smoothTime, .2f, Mathf.Clamp01((Time.time - cueStartTime - .4f) / 1.2f)) : smoothTime;
        t.position = Vector3.SmoothDamp(t.position, target.Position, ref positionVelocity, st, Mathf.Infinity, Time.deltaTime);
        currentLook = Vector3.SmoothDamp(currentLook, target.LookAt, ref lookVelocity, st, Mathf.Infinity, Time.deltaTime);
        Vector3 forward = currentLook - t.position;
        if (forward.sqrMagnitude > 1e-6f)
        {
            // the up direction turns smoothly too: a cue with another 'up' (the saddle's normal instead of world up)
            // tilts the picture over the transition instead of snapping its roll
            currentUp = Vector3.Slerp(currentUp, target.Up, 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(.05f, st * .8f))).normalized;
            t.rotation = Quaternion.LookRotation(forward, currentUp);
        }

        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, target.Fov, ref fovVelocity, st, Mathf.Infinity, Time.deltaTime);
    }

    void ApplyPortraitViewport()
    {
        if (cam.targetTexture != null)
        {
            // recording into an offscreen portrait texture: use the whole surface
            cam.rect = new Rect(0, 0, 1, 1);
            return;
        }

        // The tour is framed to its format only when the screen has the other shape (a 9:16 tour on a monitor is
        // pillarboxed, a 16:9 tour on an upright phone is letterboxed); everything else uses the whole window.
        float screenAspect = (float)Screen.width / Screen.height;
        float target = Portrait ? PortraitAspect : 1f / PortraitAspect;
        bool tour = TourDirector.Instance != null && TourDirector.Instance.Running;
        if (!tour || !Active || (screenAspect >= 1f) == (target >= 1f))
        {
            cam.rect = new Rect(0, 0, 1, 1);
            return;
        }

        if (screenAspect > target)
        {
            float w = target / screenAspect;
            cam.rect = new Rect((1f - w) * .5f, 0, w, 1);
        }
        else
        {
            float h = screenAspect / target;
            cam.rect = new Rect(0, (1f - h) * .5f, 1, h);
        }
    }
}
