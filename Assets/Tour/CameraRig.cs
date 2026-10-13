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
        cues["strip_band"] = () => new CueState { Position = new Vector3(Annotations.PanelCenterX + 0.8f, 8, -12), LookAt = new Vector3(Annotations.PanelCenterX + 0.8f, 8, 0), Up = up, Fov = 60 };
        cues["strip_band_sym"] = () => new CueState { Position = new Vector3(Annotations.PanelCenterX, 8, -14), LookAt = new Vector3(Annotations.PanelCenterX, 8, 0), Up = up, Fov = 60 };
        cues["strip_band_classic"] = () => new CueState { Position = new Vector3(Annotations.PanelCenterX + 1.2f, 16, -22), LookAt = new Vector3(Annotations.PanelCenterX + 1.2f, 18, 0), Up = up, Fov = 60 };
        cues["strip_gap"] = () => new CueState { Position = new Vector3(Annotations.PanelCenterX + 0.75f, 6, -8), LookAt = new Vector3(Annotations.PanelCenterX + 0.75f, 6, 0), Up = up, Fov = 60 };

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
        if (manualControl != null) manualControl.enabled = true;
    }

    void LateUpdate()
    {
        ApplyPortraitViewport();

        if (!Active || activeCue == null) return;

        CueState target = activeCue();
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

        if (!Portrait || !Active)
        {
            // free-roam mode uses the whole window; the 9:16 frame only applies while the tour drives the camera
            cam.rect = new Rect(0, 0, 1, 1);
            return;
        }

        float screenAspect = (float)Screen.width / Screen.height;
        if (screenAspect > PortraitAspect)
        {
            float w = PortraitAspect / screenAspect;
            cam.rect = new Rect((1f - w) * .5f, 0, w, 1);
        }
        else
        {
            float h = screenAspect / PortraitAspect;
            cam.rect = new Rect(0, (1f - h) * .5f, 1, h);
        }
    }
}
