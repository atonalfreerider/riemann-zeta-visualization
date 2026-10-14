using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// The graph view's camera: it always orbits a centre point on the saddle (the origin unless panned), with the zeta axis
/// (the saddle's normal, world −Z) straight up the screen.
///   intro      starts very close to the spiral's centre, slowly pulls back while the trail grows, then settles into a slow orbit
///   mouse      drag: orbit (left / right) and altitude (up / down) · wheel: zoom · Shift + drag or middle drag: move the centre
///   keyboard   A D / ← →: orbit · W S / ↑ ↓: altitude · Q E / − =: zoom · Shift + W A S D: move the centre
///   touch      one finger: orbit and altitude · pinch: zoom · two fingers: move the centre
/// Left alone for a few seconds the orbit drifts on by itself. Not used in VR.
/// </summary>
public class OrbitCamera : MonoBehaviour
{
    const float IntroSeconds = 30f;
    const float IntroStartDistance = 7f;
    static readonly Vector3 Up = Vector3.back; // the zeta axis

    Camera cam;
    SacksSpiral sacks;
    CameraControl freeFly;

    public bool Active { get; private set; }
    Vector3 pivot;
    float yaw, pitch = 28f, distance = 60f, lookLift;
    float introTime = -1f;
    bool userZoomed;
    float idle = 10f;
    float zoomFactor = 1f; // the user's zoom relative to the automatic framing

    public static OrbitCamera Create(Camera cam, SacksSpiral sacks)
    {
        OrbitCamera orbit = cam.gameObject.AddComponent<OrbitCamera>();
        orbit.cam = cam;
        orbit.sacks = sacks;
        orbit.freeFly = cam.GetComponent<CameraControl>();
        return orbit;
    }

    /// <summary>Take the camera. With <paramref name="intro"/> it starts at the spiral's centre and pulls back; without, it carries on from where the camera is.</summary>
    public void Begin(bool intro)
    {
        Active = true;
        pivot = sacks.Center;
        userZoomed = false;
        zoomFactor = 1f;
        idle = 10f;
        if (intro)
        {
            introTime = 0f;
            yaw = 35f;
            pitch = 30f;
            distance = IntroStartDistance;
            lookLift = 0f;
        }
        else
        {
            introTime = -1f;
            // continue from the current camera so taking over from a tour shot does not jump
            Vector3 look = pivot + Up * (sacks.GrowthHeight * .42f);
            Vector3 offset = cam.transform.position - look;
            distance = Mathf.Max(1f, offset.magnitude);
            Vector3 dir = offset / distance;
            pitch = Mathf.Asin(Mathf.Clamp(Vector3.Dot(dir, Up), -1f, 1f)) * Mathf.Rad2Deg;
            Vector3 flat = Vector3.ProjectOnPlane(dir, Up);
            yaw = flat.sqrMagnitude > 1e-6f ? Mathf.Atan2(flat.y, flat.x) * Mathf.Rad2Deg : yaw;
            lookLift = sacks.GrowthHeight * .42f;
            zoomFactor = distance / Mathf.Max(1f, FrameDistance(sacks.GrowthHeight));
        }
    }

    public void End()
    {
        Active = false;
        introTime = -1f;
    }

    /// <summary>Back to the centre and the automatic framing (the menu's Reset view).</summary>
    public void ResetView()
    {
        pivot = sacks.Center;
        userZoomed = false;
        zoomFactor = 1f;
        pitch = 28f;
        idle = 10f;
    }

    /// <summary>Distance that fits the saddle and the trail grown so far, for the current altitude and frame shape.</summary>
    float FrameDistance(float h)
    {
        float aspect = Mathf.Max(.3f, cam.aspect);
        float tan = Mathf.Tan(cam.fieldOfView * .5f * Mathf.Deg2Rad);
        float elevation = Mathf.Clamp(pitch, 5f, 85f) * Mathf.Deg2Rad;
        float radius = sacks.GrowthFrameRadius;
        float dWidth = radius * 1.05f / (tan * Mathf.Min(1f, aspect));
        float dHeight = (radius * .9f + h * Mathf.Cos(elevation) * .6f + 4f) / tan;
        float dNear = 2.6f * radius / Mathf.Max(.3f, Mathf.Cos(elevation)); // the saddle's rim rises ~56 units: keep it off the lens
        return Mathf.Max(dNear, Mathf.Max(dWidth, dHeight));
    }

    void LateUpdate()
    {
        if (!Active || cam == null) return;
        if (freeFly != null && freeFly.enabled) freeFly.enabled = false;
        float dt = Time.unscaledDeltaTime;
        HandleInput(dt);
        dt = Time.deltaTime; // the automatic moves run on game time, so recordings and pauses see them right

        float h = sacks.GrowthHeight;
        float target = FrameDistance(h);
        if (introTime >= 0f)
        {
            // the opening move: from the spiral's centre, pull back while the trail grows, then hand over to the orbit
            introTime += dt;
            float u = Mathf.SmoothStep(0f, 1f, introTime / IntroSeconds);
            distance = Mathf.Lerp(IntroStartDistance, target, u);
            if (introTime >= IntroSeconds || userZoomed) introTime = -1f;
        }
        else
        {
            distance = Mathf.Lerp(distance, target * zoomFactor, 1f - Mathf.Exp(-dt * 1.5f));
        }

        lookLift = Mathf.Lerp(lookLift, h * .42f, 1f - Mathf.Exp(-dt * 1.5f));
        idle += dt;
        float drift = introTime >= 0f ? Mathf.Lerp(.4f, 1f, introTime / IntroSeconds) : Mathf.Clamp01((idle - 4f) / 3f);
        yaw += drift * 2.6f * dt;

        Vector3 look = pivot + Up * lookLift;
        float p = pitch * Mathf.Deg2Rad, y = yaw * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Cos(y), Mathf.Sin(y), 0f) * Mathf.Cos(p) + Up * Mathf.Sin(p);
        cam.transform.position = look + dir * distance;
        cam.transform.rotation = Quaternion.LookRotation(-dir, Up);
        cam.fieldOfView = 60f;
        cam.farClipPlane = Mathf.Max(1000f, distance * 4f);
    }

    bool PointerOverUi()
    {
        EventSystem es = EventSystem.current;
        return es != null && es.IsPointerOverGameObject();
    }

    bool dragging;

    void HandleInput(float dt)
    {
        Keyboard k = Keyboard.current;
        Mouse m = Mouse.current;
        bool shift = k != null && (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed);
        float panScale = distance * .0016f;

        if (m != null)
        {
            bool down = m.leftButton.isPressed || m.rightButton.isPressed || m.middleButton.isPressed;
            if ((m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame || m.middleButton.wasPressedThisFrame) && !PointerOverUi()) dragging = true;
            if (!down) dragging = false;
            Vector2 delta = m.delta.ReadValue();
            if (dragging && delta.sqrMagnitude > 0f)
            {
                idle = 0f;
                if (shift || m.middleButton.isPressed) Pan(-delta.x * panScale, -delta.y * panScale);
                else Orbit(delta.x * .25f, -delta.y * .2f);
            }

            float scroll = m.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > .01f && !PointerOverUi())
            {
                Zoom(Mathf.Pow(1.12f, -Mathf.Sign(scroll) * Mathf.Min(3f, Mathf.Abs(scroll) / 120f + .5f)));
            }
        }

        if (k != null)
        {
            float x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1f : 0f);
            float z = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1f : 0f) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1f : 0f);
            if (x != 0f || z != 0f)
            {
                idle = 0f;
                if (shift) Pan(x * distance * .6f * dt, z * distance * .6f * dt);
                else Orbit(-x * 60f * dt, z * 40f * dt);
            }

            float zoom = (k.qKey.isPressed || k.minusKey.isPressed ? 1f : 0f) - (k.eKey.isPressed || k.equalsKey.isPressed ? 1f : 0f);
            if (zoom != 0f) Zoom(Mathf.Exp(zoom * 1.2f * dt));
        }

        HandleTouch();
    }

    float lastPinch = -1f;
    Vector2 lastMid;

    void HandleTouch()
    {
        Touchscreen ts = Touchscreen.current;
        if (ts == null) return;
        int active = 0;
        Vector2 a = default, b = default, da = default, db = default;
        foreach (var touch in ts.touches)
        {
            if (!touch.isInProgress) continue;
            if (active == 0) { a = touch.position.ReadValue(); da = touch.delta.ReadValue(); }
            else if (active == 1) { b = touch.position.ReadValue(); db = touch.delta.ReadValue(); }
            active++;
        }

        if (active == 1 && !PointerOverUi())
        {
            idle = 0f;
            Orbit(da.x * .2f, -da.y * .15f);
            lastPinch = -1f;
        }
        else if (active >= 2)
        {
            idle = 0f;
            float pinch = Vector2.Distance(a, b);
            Vector2 mid = (a + b) * .5f;
            if (lastPinch > 0f)
            {
                Zoom(lastPinch / Mathf.Max(1f, pinch));
                Vector2 move = mid - lastMid;
                Pan(-move.x * distance * .0016f, -move.y * distance * .0016f);
            }

            lastPinch = pinch;
            lastMid = mid;
        }
        else
        {
            lastPinch = -1f;
        }
    }

    void Orbit(float dYaw, float dPitch)
    {
        yaw -= dYaw;
        pitch = Mathf.Clamp(pitch + dPitch, -10f, 88f);
        if (introTime >= 0f) introTime = -1f; // the user has the camera now
    }

    void Zoom(float factor)
    {
        userZoomed = true;
        if (introTime >= 0f)
        {
            zoomFactor = distance / Mathf.Max(1f, FrameDistance(sacks.GrowthHeight));
            introTime = -1f;
        }

        zoomFactor = Mathf.Clamp(zoomFactor * factor, .02f, 6f);
        distance *= factor;
        idle = 0f;
    }

    /// <summary>Move the centre across the saddle's plane: screen-right and screen-up (projected onto the plane).</summary>
    void Pan(float right, float forward)
    {
        Vector3 r = Vector3.ProjectOnPlane(cam.transform.right, Up).normalized;
        Vector3 f = Vector3.ProjectOnPlane(cam.transform.up + cam.transform.forward, Up).normalized;
        pivot += r * right + f * forward;
        if (introTime >= 0f) introTime = -1f;
    }
}
