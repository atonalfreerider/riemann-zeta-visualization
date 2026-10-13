using UnityEngine;

/// <summary>
/// Turns the tour's camera cues into where the model sits in front of the user. The 2D tour moves a camera; in VR the
/// camera is the user's head, so instead the cue's view is mapped onto the user: the cue camera becomes the user's eye
/// and the point it looks at is placed <see cref="VRRig.ModelDistance"/> metres in front, which makes the model a
/// tabletop-scale copy of exactly what the narrated 2D video shows.
///
/// Comfort: the mapped camera is smoothed more slowly than in 2D, and the resulting turn and zoom rates are capped
/// (lower in blackout, where there is no room to anchor the eyes).
///
/// The user can zoom, orbit and pan on top of the story (those offsets survive cue changes), or take hold of the model
/// by hand, which hands control to them (Follow = false) until they ask for the story's view again.
/// </summary>
public class VRGuide : MonoBehaviour
{
    public bool Follow = true;
    /// <summary>Extra smoothing of the cue camera, in seconds.</summary>
    public float SmoothTime = 1.6f;
    public bool Comfort = true;

    // user offsets in the cue's own frame
    public float Zoom = 1f;
    public float OrbitYaw;
    public float OrbitPitch;
    public Vector2 Pan;

    public System.Action FollowChanged;

    VRRig rig;
    CameraRig.CueState cue;
    bool hasCue;
    bool snapNext = true;
    Vector3 eye, look, up;
    Vector3 eyeVelocity, lookVelocity, upVelocity;

    /// <summary>The framing used before any narration cue has been asked for: the whole machine at a glance.</summary>
    public static CameraRig.CueState HomeCue => new() { Position = new Vector3(26f, 30f, -92f), LookAt = new Vector3(26f, 28f, 0f), Up = Vector3.up, Fov = 60f };

    public static VRGuide Create(VRRig rig)
    {
        VRGuide g = rig.gameObject.AddComponent<VRGuide>();
        g.rig = rig;
        VRMode.Guide = g;
        g.SetCue(HomeCue, true);
        return g;
    }

    public void SetCue(CameraRig.CueState c, bool snap = false)
    {
        cue = c;
        if (!hasCue || snap) snapNext = true;
        hasCue = true;
    }

    public void ResetOffsets()
    {
        Zoom = 1f;
        OrbitYaw = OrbitPitch = 0f;
        Pan = Vector2.zero;
    }

    public void SetFollow(bool on, bool snap = false)
    {
        if (Follow == on && !snap) return;
        Follow = on;
        if (on)
        {
            rig.CancelAnimation();
            if (snap) snapNext = true;
        }

        FollowChanged?.Invoke();
    }

    /// <summary>The cue camera with the user's zoom, orbit and pan applied.</summary>
    void Biased(out Vector3 eyeOut, out Vector3 lookOut, out Vector3 upOut)
    {
        Vector3 f = cue.LookAt - cue.Position;
        float d = Mathf.Max(f.magnitude, 0.05f);
        Vector3 fwd = f / d;
        Vector3 cueUp = cue.Up.sqrMagnitude > 0f ? cue.Up.normalized : Vector3.up;
        if (Mathf.Abs(Vector3.Dot(fwd, cueUp)) > 0.999f) cueUp = Vector3.Cross(fwd, Vector3.right).normalized;
        Vector3 right = Vector3.Cross(cueUp, fwd).normalized;
        Vector3 camUp = Vector3.Cross(fwd, right);

        Vector3 pan = (right * Pan.x + camUp * Pan.y) * d;
        Vector3 l = cue.LookAt + pan;
        Vector3 p = cue.Position + pan;
        Quaternion q = Quaternion.AngleAxis(OrbitYaw, cueUp) * Quaternion.AngleAxis(OrbitPitch, right);
        eyeOut = l + q * (p - l) * Zoom;
        lookOut = l;
        upOut = q * cueUp;
    }

    void LateUpdate()
    {
        if (!hasCue) return;
        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f) return;

        Biased(out Vector3 targetEye, out Vector3 targetLook, out Vector3 targetUp);
        if (snapNext)
        {
            eye = targetEye;
            look = targetLook;
            up = targetUp;
            eyeVelocity = lookVelocity = upVelocity = Vector3.zero;
        }
        else
        {
            float t = Comfort ? SmoothTime : 0.5f;
            eye = Vector3.SmoothDamp(eye, targetEye, ref eyeVelocity, t, Mathf.Infinity, dt);
            look = Vector3.SmoothDamp(look, targetLook, ref lookVelocity, t, Mathf.Infinity, dt);
            up = Vector3.SmoothDamp(up, targetUp, ref upVelocity, t, Mathf.Infinity, dt);
        }

        if (!Follow || rig.Animating) return;

        VRRig.Placement wanted = rig.PlacementForView(eye, look, up);
        if (snapNext)
        {
            rig.SetPlacement(wanted);
            snapNext = false;
            return;
        }

        VRRig.Placement current = rig.Current;
        float turnRate = Comfort ? (rig.Passthrough ? 50f : 30f) : 400f;
        float zoomRate = Comfort ? (rig.Passthrough ? 0.9f : 0.5f) : 8f;
        Quaternion rotation = Quaternion.RotateTowards(current.Rotation, wanted.Rotation, turnRate * dt);
        float logScale = Mathf.MoveTowards(Mathf.Log(current.Scale), Mathf.Log(wanted.Scale), zoomRate * dt);
        float scale = Mathf.Exp(logScale);
        // keep the cue camera glued to the user's reference point whatever the rate limits did to rotation and scale
        Vector3 position = eye - rotation * (scale * rig.CameraLocal);
        rig.SetPlacement(new VRRig.Placement { Position = position, Rotation = rotation, Scale = scale });
    }

    /// <summary>True when the placement is close enough to the story's view that following has caught up.</summary>
    public bool CaughtUp
    {
        get
        {
            if (!hasCue) return true;
            VRRig.Placement wanted = rig.PlacementForView(eye, look, up);
            VRRig.Placement c = rig.Current;
            return Quaternion.Angle(c.Rotation, wanted.Rotation) < 2f && Mathf.Abs(Mathf.Log(c.Scale / wanted.Scale)) < 0.03f;
        }
    }

    // ---- space controls -------------------------------------------------------------------------------

    /// <summary>
    /// Zoom, turn, tilt or move the model. While following the story these change the offsets on top of the cue;
    /// in manual mode they move the model directly around the point in front of the user.
    /// </summary>
    public void Nudge(NudgeKind kind, float amount = 1f)
    {
        if (Follow)
        {
            switch (kind)
            {
                case NudgeKind.Zoom: Zoom = Mathf.Clamp(Zoom / Mathf.Pow(1.3f, amount), 0.08f, 6f); break;
                case NudgeKind.Yaw: OrbitYaw += 30f * amount; break;
                case NudgeKind.Pitch: OrbitPitch = Mathf.Clamp(OrbitPitch + 20f * amount, -80f, 80f); break;
                case NudgeKind.Raise: Pan.y -= 0.12f * amount; break;
                case NudgeKind.Closer: Zoom = Mathf.Clamp(Zoom / Mathf.Pow(1.18f, amount), 0.08f, 6f); break;
                case NudgeKind.SlideSideways: Pan.x -= 0.12f * amount; break;
            }

            return;
        }

        Vector3 pivot = rig.FocusLocal;
        VRRig.Placement target = rig.Current;
        Vector3 rigUp = rig.transform.up;
        switch (kind)
        {
            case NudgeKind.Zoom: target = rig.AroundLocal(pivot, Quaternion.identity, 1f / Mathf.Pow(1.3f, amount)); break;
            case NudgeKind.Yaw: target = rig.AroundLocal(pivot, Quaternion.AngleAxis(-30f * amount, Vector3.up), 1f); break;
            case NudgeKind.Pitch: target = rig.AroundLocal(pivot, Quaternion.AngleAxis(-20f * amount, rig.Cam.transform.parent.rotation * Vector3.right), 1f); break;
            case NudgeKind.Raise: target.Position -= rigUp * (0.12f * amount * target.Scale); break;
            case NudgeKind.Closer: target.Position += rig.transform.rotation * (rig.YawHome * Vector3.forward) * (0.12f * amount * target.Scale); break;
            case NudgeKind.SlideSideways: target.Position -= rig.transform.rotation * (rig.YawHome * Vector3.right) * (0.12f * amount * target.Scale); break;
        }

        rig.AnimateTo(target, 0.3f);
    }

    public enum NudgeKind { Zoom, Yaw, Pitch, Raise, Closer, SlideSideways }

    /// <summary>Put the model back the way the story frames it (or the home view).</summary>
    public void ResetView()
    {
        ResetOffsets();
        Follow = true;
        snapNext = false;
        rig.CancelAnimation();
        FollowChanged?.Invoke();
    }
}
