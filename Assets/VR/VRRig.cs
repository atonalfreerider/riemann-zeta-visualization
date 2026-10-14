using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// The user's place in the scene. Everything the user owns (camera, hands, interface, captions) lives under this
/// transform, and the *visualization stays where it is*: to move, scale or turn the model we move, scale and turn this
/// rig instead (a rig scaled by 20 makes the model look 20 times smaller; stereo separation scales with it).
///
/// Passthrough is the default backdrop (camera background alpha 0 and an enabled ARCameraManager, see the Meta OpenXR
/// docs); "blackout" turns the passthrough layer off and clears to the dark void colour with a faint floor ring.
/// </summary>
public class VRRig : MonoBehaviour
{
    public struct Placement
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float Scale;
    }

    public static readonly Color VoidColor = new(0.012f, 0.012f, 0.02f, 1f);

    /// <summary>Metres between the user's head and the point of the model that the tour is looking at.</summary>
    public float ModelDistance = 1.0f;

    public Camera Cam { get; private set; }
    /// <summary>Hands and head are reported in this space (the XR Origin's Camera Offset).</summary>
    public Transform Offset { get; private set; }
    /// <summary>Panels hang here, in the same metric space as the hands.</summary>
    public Transform Ui { get; private set; }
    /// <summary>Lazily follows the gaze; captions and toasts live under it.</summary>
    public Transform Hud { get; private set; }
    public bool Headset { get; private set; }

    /// <summary>Head position (tracking space) and yaw when the view was last centred.</summary>
    public Vector3 HeadHome { get; private set; } = new(0f, 1.45f, 0f);
    public Quaternion YawHome { get; private set; } = Quaternion.identity;

    public bool Passthrough { get; private set; } = true;
    public float Scale => transform.localScale.x;
    public bool Animating => animation != null;

    ARCameraManager passthroughManager;
    GameObject floorGrid;
    Coroutine animation;
    Vector3 hudDirection = Vector3.forward;
    bool hudMoving = true;
    Vector3 hudVelocity;
    Vector3 hudPosition;

    public const float HudDistance = 1.5f;

    public static VRRig Create(Camera cam, bool headset)
    {
        GameObject root = new("XR Rig");
        root.SetActive(false); // configure before the XR components wake up
        VRRig rig = root.AddComponent<VRRig>();
        rig.Cam = cam;
        rig.Headset = headset;

        GameObject offset = new("Camera Offset");
        offset.transform.SetParent(root.transform, false);
        rig.Offset = offset.transform;

        GameObject ui = new("VR UI");
        ui.transform.SetParent(offset.transform, false);
        rig.Ui = ui.transform;
        GameObject hud = new("VR HUD");
        hud.transform.SetParent(offset.transform, false);
        rig.Hud = hud.transform;

        cam.transform.SetParent(offset.transform, false);
        cam.transform.localPosition = headset ? Vector3.zero : rig.HeadHome;
        cam.transform.localRotation = Quaternion.identity;
        cam.allowHDR = false;
        cam.allowMSAA = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.rect = new Rect(0, 0, 1, 1);
        CameraControl manual = cam.GetComponent<CameraControl>();
        if (manual != null) manual.enabled = false;
        BloomEffect bloom = cam.GetComponent<BloomEffect>();
        if (bloom != null) bloom.enabled = false; // post-processing is too expensive for a mobile headset

        if (headset)
        {
            XROrigin origin = root.AddComponent<XROrigin>();
            origin.Origin = root;
            origin.CameraFloorOffsetObject = offset;
            origin.Camera = cam;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

            TrackedPoseDriver driver = cam.gameObject.AddComponent<TrackedPoseDriver>();
            driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            driver.ignoreTrackingState = true;
            driver.positionInput = new InputActionProperty(new InputAction("Head Position", InputActionType.Value, "<XRHMD>/centerEyePosition", expectedControlType: "Vector3"));
            driver.rotationInput = new InputActionProperty(new InputAction("Head Rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion"));

            GameObject session = new("AR Session");
            session.transform.SetParent(root.transform, false);
            session.AddComponent<ARSession>();
            rig.passthroughManager = cam.gameObject.AddComponent<ARCameraManager>();
        }
        else
        {
            cam.gameObject.AddComponent<VRSimulatedHead>();
        }

        root.SetActive(true);
        VRMode.Register(rig, headset);
        rig.BuildFloorGrid();
        rig.SetPassthrough(true);
        rig.UpdateClipPlanes();
        return rig;
    }

    // ---- view frame ---------------------------------------------------------------------------

    /// <summary>Remember where the user is standing and facing; the model is placed relative to this.</summary>
    public void Recenter()
    {
        Vector3 head = Cam.transform.localPosition;
        if (head.sqrMagnitude < 1e-4f) head = HeadHome; // tracking not up yet
        HeadHome = head;
        float yaw = Cam.transform.localEulerAngles.y;
        YawHome = Quaternion.Euler(0f, yaw, 0f);
        hudMoving = true;
    }

    /// <summary>Rig-local point that holds the model's focus: straight ahead, a little below the eyes.</summary>
    public Vector3 FocusLocal => HeadHome + YawHome * new Vector3(0f, -0.2f, 0f) + YawHome * Vector3.forward * ModelDistance;

    /// <summary>Where the tour camera sits in rig space (the point that maps to the cue camera position).</summary>
    public Vector3 CameraLocal => HeadHome + YawHome * new Vector3(0f, -0.2f, 0f);

    /// <summary>
    /// The placement that makes the model look the way a camera at <paramref name="eye"/> looking at
    /// <paramref name="look"/> would see it, with the looked-at point <see cref="ModelDistance"/> metres away.
    /// </summary>
    public Placement PlacementForView(Vector3 eye, Vector3 look, Vector3 up)
    {
        Vector3 forward = look - eye;
        float distance = Mathf.Max(forward.magnitude, 0.05f);
        forward /= distance;
        if (Mathf.Abs(Vector3.Dot(forward, up.normalized)) > 0.999f) up = Vector3.Cross(forward, Vector3.right);
        Quaternion cue = Quaternion.LookRotation(forward, up);
        float scale = distance / Mathf.Max(ModelDistance, 0.1f);
        Quaternion rotation = cue * Quaternion.Inverse(YawHome);
        return new Placement { Rotation = rotation, Scale = scale, Position = eye - rotation * (scale * CameraLocal) };
    }

    public Placement Current => new() { Position = transform.position, Rotation = transform.rotation, Scale = Scale };

    public void SetPlacement(Placement p)
    {
        transform.SetPositionAndRotation(p.Position, p.Rotation);
        transform.localScale = Vector3.one * Mathf.Clamp(p.Scale, 1e-3f, 1e4f);
        UpdateClipPlanes();
    }

    /// <summary>Glide to a placement (unscaled time, so it also works while the tour is paused).</summary>
    public void AnimateTo(Placement target, float seconds = 0.35f)
    {
        if (animation != null) StopCoroutine(animation);
        animation = StartCoroutine(AnimateRoutine(target, seconds));
    }

    IEnumerator AnimateRoutine(Placement target, float seconds)
    {
        Placement from = Current;
        float logFrom = Mathf.Log(from.Scale), logTo = Mathf.Log(target.Scale);
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / seconds);
            SetPlacement(new Placement
            {
                Position = Vector3.Lerp(from.Position, target.Position, k),
                Rotation = Quaternion.Slerp(from.Rotation, target.Rotation, k),
                Scale = Mathf.Exp(Mathf.Lerp(logFrom, logTo, k))
            });
            yield return null;
        }

        SetPlacement(target);
        animation = null;
    }

    public void CancelAnimation()
    {
        if (animation != null) StopCoroutine(animation);
        animation = null;
    }

    /// <summary>Keep the world point under <paramref name="pivotLocal"/> fixed while applying a similarity around it.</summary>
    public Placement AroundLocal(Vector3 pivotLocal, Quaternion deltaRotation, float scaleFactor)
    {
        Placement c = Current;
        Vector3 pivotWorld = transform.TransformPoint(pivotLocal);
        Quaternion rotation = deltaRotation * c.Rotation;
        float scale = c.Scale * scaleFactor;
        return new Placement { Rotation = rotation, Scale = scale, Position = pivotWorld - rotation * (scale * pivotLocal) };
    }

    void UpdateClipPlanes()
    {
        float s = Scale;
        Cam.nearClipPlane = Mathf.Max(0.001f, 0.05f * s);
        Cam.farClipPlane = Mathf.Max(1000f, 400f * s);
        Factory.SetVrLineMetrics(s, ModelDistance);
        foreach (LineRenderer line in floorLines) line.widthMultiplier = s; // these lines live in the user's space: a few millimetres wide
    }

    // ---- passthrough / blackout ---------------------------------------------------------------

    public void SetPassthrough(bool on)
    {
        Passthrough = on;
        Cam.clearFlags = CameraClearFlags.SolidColor;
        Cam.backgroundColor = on ? new Color(0f, 0f, 0f, 0f) : VoidColor;
        if (passthroughManager != null) passthroughManager.enabled = on;
        if (floorGrid != null) floorGrid.SetActive(!on);
    }

    void BuildFloorGrid()
    {
        floorGrid = new GameObject("Floor ring");
        floorGrid.transform.SetParent(Offset, false);
        Color dim = new(0.28f, 0.34f, 0.5f, 0.55f);
        for (int ring = 1; ring <= 4; ring++)
        {
            LineRenderer line = NewFloorLine("ring", dim);
            line.loop = true;
            const int segments = 72;
            line.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                line.SetPosition(i, new Vector3(Mathf.Cos(a), 0.002f, Mathf.Sin(a)) * ring * 0.5f);
            }
        }

        for (int spoke = 0; spoke < 8; spoke++)
        {
            LineRenderer line = NewFloorLine("spoke", dim);
            float a = spoke * Mathf.PI / 4f;
            line.positionCount = 2;
            line.SetPosition(0, new Vector3(Mathf.Cos(a), 0.002f, Mathf.Sin(a)) * 0.5f);
            line.SetPosition(1, new Vector3(Mathf.Cos(a), 0.002f, Mathf.Sin(a)) * 2f);
        }
    }

    readonly System.Collections.Generic.List<LineRenderer> floorLines = new();

    /// <summary>A thin line in the user's own space (not part of the model): 4 mm wide, whatever the rig scale.</summary>
    LineRenderer NewFloorLine(string name, Color color)
    {
        LineRenderer line = Factory.NewLine(name, 0.004f, color);
        line.transform.SetParent(floorGrid.transform, false);
        line.useWorldSpace = false;
        floorLines.Add(line);
        return line;
    }

    // ---- HUD follow ----------------------------------------------------------------------------

    void LateUpdate()
    {
        UpdateHud(Time.unscaledDeltaTime);
    }

    /// <summary>Lazy follow: the HUD waits until the gaze has moved 22 degrees away, then glides to the new gaze direction.</summary>
    void UpdateHud(float dt)
    {
        Transform head = Cam.transform;
        Vector3 forward = head.parent.InverseTransformDirection(head.forward);
        float pitch = Mathf.Asin(Mathf.Clamp(forward.y, -1f, 1f)) * Mathf.Rad2Deg;
        float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        Vector3 gaze = Quaternion.Euler(Mathf.Clamp(-pitch, -10f, 14f) * 0.5f, yaw, 0f) * Vector3.forward;

        float angle = Vector3.Angle(gaze, hudDirection);
        if (angle > 22f) hudMoving = true;
        if (hudMoving)
        {
            hudDirection = Vector3.Slerp(hudDirection, gaze, 1f - Mathf.Exp(-3.2f * dt)).normalized;
            if (angle < 2f) hudMoving = false;
        }

        Vector3 headPos = head.localPosition;
        Vector3 wanted = headPos + hudDirection * HudDistance;
        hudPosition = hudMoving ? Vector3.SmoothDamp(hudPosition, wanted, ref hudVelocity, 0.25f, Mathf.Infinity, dt) : wanted;
        Hud.localPosition = hudPosition;
        Vector3 look = hudPosition - headPos;
        if (look.sqrMagnitude > 1e-6f) Hud.localRotation = Quaternion.LookRotation(look, Vector3.up);
    }

    public void SnapHud()
    {
        hudDirection = YawHome * Vector3.forward;
        hudPosition = Cam.transform.localPosition + hudDirection * HudDistance;
        hudMoving = false;
    }
}

/// <summary>Desktop stand-in for the headset: right mouse look, WASD / QE to move.</summary>
public class VRSimulatedHead : MonoBehaviour
{
    public float Speed = 1.2f;
    Vector2 look;

    void Update()
    {
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;
        if (mouse != null && mouse.rightButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue() * 0.12f;
            look.x += delta.x;
            look.y = Mathf.Clamp(look.y - delta.y, -80f, 80f);
            transform.localRotation = Quaternion.Euler(look.y, look.x, 0f);
        }

        if (keyboard == null) return;
        Vector3 move = Vector3.zero;
        if (keyboard.wKey.isPressed) move += transform.forward;
        if (keyboard.sKey.isPressed) move -= transform.forward;
        if (keyboard.dKey.isPressed) move += transform.right;
        if (keyboard.aKey.isPressed) move -= transform.right;
        if (keyboard.eKey.isPressed) move += Vector3.up;
        if (keyboard.qKey.isPressed) move -= Vector3.up;
        transform.localPosition += transform.parent.InverseTransformDirection(move) * (Speed * Time.unscaledDeltaTime);
    }

    public void SetLook(float yaw, float pitch)
    {
        look = new Vector2(yaw, pitch);
        transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
