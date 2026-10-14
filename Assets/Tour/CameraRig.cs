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
    Vector3 positionVelocity;
    Vector3 lookVelocity;
    Vector3 currentLook;
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
        cues["strip_band"] = () => new CueState { Position = new Vector3(Annotations.PanelCenterX + 1.0f, 9, -10), LookAt = new Vector3(Annotations.PanelCenterX + 1.0f, 9, 0), Up = up, Fov = 60 };
        cues["strip_band_sym"] = () => new CueState { Position = new Vector3(Annotations.PanelCenterX, 9, -11), LookAt = new Vector3(Annotations.PanelCenterX, 9, 0), Up = up, Fov = 60 };
        cues["strip_band_classic"] = () => new CueState { Position = new Vector3(Annotations.PanelCenterX + 1.2f, 16, -22), LookAt = new Vector3(Annotations.PanelCenterX + 1.2f, 18, 0), Up = up, Fov = 60 };
        cues["strip_gap"] = () => new CueState { Position = new Vector3(Annotations.PanelCenterX + 0.75f, 6, -8), LookAt = new Vector3(Annotations.PanelCenterX + 0.75f, 6, 0), Up = up, Fov = 60 };

        // ---- from-scratch narrative --------------------------------------------------------
        cues["stage"] = () => new CueState { Position = new Vector3(4.5f, 4.2f, -9f), LookAt = new Vector3(1.2f, 2.6f, 1.2f), Up = up, Fov = 60 };
        cues["stage_floor"] = () => new CueState { Position = new Vector3(1.8f, 8f, -2.5f), LookAt = new Vector3(1.8f, 0f, 1.5f), Up = Vector3.forward, Fov = 60 };
        cues["chain_flat"] = () => new CueState { Position = new Vector3(2.8f, 8.5f, -0.4f), LookAt = new Vector3(2.8f, 0f, 0f), Up = Vector3.right, Fov = 60 };
        cues["chain_long"] = () => new CueState { Position = new Vector3(6.5f, 16f, -1f), LookAt = new Vector3(6.5f, 0f, 0f), Up = Vector3.right, Fov = 60 };
        cues["spiral_curl"] = () => new CueState { Position = new Vector3(0.6f, TipY + 7.5f, -1.6f), LookAt = new Vector3(0.6f, TipY, 0.1f), Up = Vector3.forward, Fov = 58 };
        cues["sigma_spiral"] = () => new CueState { Position = new Vector3(0.3f, TipY + 4.2f, -1.0f), LookAt = new Vector3(0.3f, TipY, 0.1f), Up = Vector3.forward, Fov = 58 };
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
            Debug.LogWarning($"CameraRig: unknown cue '{name}'");
            return;
        }

        if (!Active)
        {
            Activate();
        }

        activeCue = cue;
        activeCueName = name;
        cueStartTime = Time.time;
        smoothTime = Mathf.Max(0.15f, transitionSeconds * 0.4f);
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
        t.position = Vector3.SmoothDamp(t.position, target.Position, ref positionVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);
        currentLook = Vector3.SmoothDamp(currentLook, target.LookAt, ref lookVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);
        Vector3 forward = currentLook - t.position;
        if (forward.sqrMagnitude > 1e-6f)
        {
            t.rotation = Quaternion.LookRotation(forward, target.Up);
        }

        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, target.Fov, ref fovVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);
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
