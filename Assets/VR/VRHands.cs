using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Hands;

/// <summary>
/// One tracked hand, reduced to what the interface needs. Positions are in tracking space (metres, the rig's
/// "Camera Offset"), so distances stay physical no matter how large or small the model has been scaled.
/// </summary>
public class VRHand
{
    public const int JointCount = 26;
    public static readonly int Wrist = XRHandJointID.Wrist.ToIndex();
    public static readonly int Palm = XRHandJointID.Palm.ToIndex();
    public static readonly int ThumbTip = XRHandJointID.ThumbTip.ToIndex();
    public static readonly int IndexMeta = XRHandJointID.IndexMetacarpal.ToIndex();
    public static readonly int IndexProximal = XRHandJointID.IndexProximal.ToIndex();
    public static readonly int IndexIntermediate = XRHandJointID.IndexIntermediate.ToIndex();
    public static readonly int IndexDistal = XRHandJointID.IndexDistal.ToIndex();
    public static readonly int IndexTipId = XRHandJointID.IndexTip.ToIndex();
    public static readonly int MiddleProximal = XRHandJointID.MiddleProximal.ToIndex();
    public static readonly int LittleProximal = XRHandJointID.LittleProximal.ToIndex();

    /// <summary>Joint chains (wrist to tip) for drawing the skeleton.</summary>
    public static readonly int[][] Chains =
    {
        Chain(XRHandJointID.Wrist, XRHandJointID.ThumbMetacarpal, XRHandJointID.ThumbProximal, XRHandJointID.ThumbDistal, XRHandJointID.ThumbTip),
        Chain(XRHandJointID.Wrist, XRHandJointID.IndexMetacarpal, XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip),
        Chain(XRHandJointID.Wrist, XRHandJointID.MiddleMetacarpal, XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip),
        Chain(XRHandJointID.Wrist, XRHandJointID.RingMetacarpal, XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, XRHandJointID.RingTip),
        Chain(XRHandJointID.Wrist, XRHandJointID.LittleMetacarpal, XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip),
    };

    static int[] Chain(params XRHandJointID[] ids)
    {
        int[] result = new int[ids.Length];
        for (int i = 0; i < ids.Length; i++) result[i] = ids[i].ToIndex();
        return result;
    }

    public const float PinchStart = 0.024f;
    public const float PinchEnd = 0.042f;

    public readonly bool IsRight;
    public bool Tracked;
    public readonly Vector3[] Joints = new Vector3[JointCount];

    public Vector3 IndexTip => Joints[IndexTipId];
    public Vector3 ThumbTipPos => Joints[ThumbTip];
    public Vector3 PinchPoint => (Joints[IndexTipId] + Joints[ThumbTip]) * 0.5f;
    public float PinchDistance { get; private set; } = 1f;
    public bool Pinching { get; private set; }
    public bool PinchDown { get; private set; }
    public bool PinchUp { get; private set; }
    public bool IndexExtended { get; private set; }
    public Vector3 PalmCenter { get; private set; }
    /// <summary>Unit vector out of the palm (tracking space).</summary>
    public Vector3 PalmNormal { get; private set; } = Vector3.up;
    /// <summary>Unit vector from the wrist toward the fingers.</summary>
    public Vector3 FingerDirection { get; private set; } = Vector3.forward;
    public Vector3 IndexTipVelocity { get; private set; }
    public float TrackedFor { get; private set; }

    Vector3 lastTip;
    bool hadTip;

    public VRHand(bool isRight)
    {
        IsRight = isRight;
    }

    public void Lost()
    {
        Tracked = false;
        Pinching = false;
        PinchDown = false;
        PinchUp = false;
        hadTip = false;
        TrackedFor = 0f;
    }

    /// <summary>Derive pinch, palm frame and pointing state from the raw joints.</summary>
    public void Update(float dt)
    {
        TrackedFor += dt;

        PinchDistance = Vector3.Distance(Joints[IndexTipId], Joints[ThumbTip]);
        bool was = Pinching;
        if (!Pinching && PinchDistance < PinchStart) Pinching = true;
        else if (Pinching && PinchDistance > PinchEnd) Pinching = false;
        PinchDown = Pinching && !was;
        PinchUp = !Pinching && was;

        Vector3 along = Joints[MiddleProximal] - Joints[Wrist];
        Vector3 across = Joints[IndexProximal] - Joints[LittleProximal]; // points toward the thumb side
        FingerDirection = along.sqrMagnitude > 1e-8f ? along.normalized : Vector3.forward;
        Vector3 normal = Vector3.Cross(FingerDirection, across.normalized);
        PalmNormal = (IsRight ? normal : -normal).normalized;
        PalmCenter = (Joints[Wrist] + Joints[MiddleProximal] + Joints[IndexProximal] + Joints[LittleProximal]) * 0.25f;

        float chain = Vector3.Distance(Joints[IndexProximal], Joints[IndexIntermediate])
                      + Vector3.Distance(Joints[IndexIntermediate], Joints[IndexDistal])
                      + Vector3.Distance(Joints[IndexDistal], Joints[IndexTipId]);
        float straight = Vector3.Distance(Joints[IndexProximal], Joints[IndexTipId]);
        IndexExtended = chain > 1e-4f && straight / chain > 0.82f;

        Vector3 tip = Joints[IndexTipId];
        IndexTipVelocity = hadTip && dt > 1e-5f ? (tip - lastTip) / dt : Vector3.zero;
        lastTip = tip;
        hadTip = true;
    }

    /// <summary>
    /// A plausible hand skeleton for the desktop simulation: the index fingertip at <paramref name="tip"/>,
    /// pointing along <paramref name="point"/>, back of the hand toward <paramref name="back"/>.
    /// </summary>
    public void FillSynthetic(Vector3 tip, Vector3 point, Vector3 back, float pinch)
    {
        // Frame for a right hand held palm down, fingers forward: point = +Z, back of hand = +Y, thumb side = -X.
        point = point.normalized;
        float handed = IsRight ? 1f : -1f;
        Vector3 side = handed * Vector3.Cross(point, back).normalized;   // toward the thumb
        back = handed * Vector3.Cross(side, point).normalized;           // out of the back of the hand
        Vector3 origin = tip - point * 0.148f - side * 0.026f;           // wrist, so the index tip lands on `tip`

        // finger layouts: x is the offset toward the pinky, z runs toward the fingertips
        void Finger(int first, float x, float[] z, float curl)
        {
            for (int i = 0; i < z.Length; i++)
            {
                float zz = z[i], yy = 0f;
                if (curl > 0f && i >= 2)
                {
                    float c = curl * (i - 1) * 0.55f;
                    zz = z[1] + (z[i] - z[1]) * Mathf.Cos(c);
                    yy = -(z[i] - z[1]) * Mathf.Sin(c);
                }

                Joints[first + i] = origin + point * zz - side * x + back * yy;
            }
        }

        Joints[Wrist] = origin;
        Joints[Palm] = origin + point * 0.045f;
        // thumb: metacarpal, proximal, distal, tip (indices 1..4)
        Vector3 thumbBase = origin + point * 0.015f + side * 0.022f;
        Vector3 thumbTipRest = origin + point * 0.085f + side * 0.075f - back * 0.005f;
        Vector3 pinchTip = tip + side * 0.008f - back * 0.006f;
        Vector3 thumbTipPos = Vector3.Lerp(thumbTipRest, pinchTip, pinch);
        Joints[1] = thumbBase;
        Joints[2] = Vector3.Lerp(thumbBase, thumbTipPos, 0.35f) + back * 0.004f;
        Joints[3] = Vector3.Lerp(thumbBase, thumbTipPos, 0.7f) + back * 0.003f;
        Joints[4] = thumbTipPos;
        Finger(5, -0.026f, new[] { 0.02f, 0.075f, 0.108f, 0.130f, 0.148f }, 0f);     // index (straight)
        Finger(10, -0.008f, new[] { 0.02f, 0.080f, 0.118f, 0.142f, 0.162f }, 1.15f);  // middle
        Finger(15, 0.012f, new[] { 0.02f, 0.076f, 0.112f, 0.135f, 0.153f }, 1.15f);   // ring
        Finger(20, 0.030f, new[] { 0.02f, 0.068f, 0.098f, 0.117f, 0.132f }, 1.15f);   // little
    }
}

/// <summary>
/// Hand tracking source for the rig: the OpenXR hand subsystem on a headset, the mouse in the desktop simulation.
/// </summary>
public class VRHands : MonoBehaviour
{
    public VRHand Left { get; } = new(false);
    public VRHand Right { get; } = new(true);
    public bool Available { get; private set; }

    VRRig rig;
    XRHandSubsystem subsystem;
    readonly List<XRHandSubsystem> subsystems = new();

    // ---- desktop simulation / test driving -----------------------------------------------------
    /// <summary>When set, the simulated right hand is placed here (tracking space) instead of following the mouse.</summary>
    public bool ScriptedRight;
    public Vector3 ScriptedRightTip;
    public float ScriptedRightPinch;
    public bool ScriptedLeft;
    public Vector3 ScriptedLeftTip;
    public float ScriptedLeftPinch;
    float simulatedDepth = 0.5f;
    bool simulatedPoke;
    bool simulatedPinch;

    public static VRHands Create(VRRig rig)
    {
        VRHands hands = rig.gameObject.AddComponent<VRHands>();
        hands.rig = rig;
        return hands;
    }

    public VRHand Other(VRHand hand) => hand == Left ? Right : Left;

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (rig.Headset) PollSubsystem(dt);
        else PollSimulation(dt);
    }

    // ---- headset ----------------------------------------------------------------------------------

    void PollSubsystem(float dt)
    {
        if (subsystem == null || !subsystem.running)
        {
            subsystem = null;
            SubsystemManager.GetSubsystems(subsystems);
            foreach (XRHandSubsystem candidate in subsystems)
            {
                if (candidate.running)
                {
                    subsystem = candidate;
                    break;
                }
            }
        }

        Available = subsystem != null;
        if (subsystem == null)
        {
            Left.Lost();
            Right.Lost();
            return;
        }

        Read(subsystem.leftHand, Left, dt);
        Read(subsystem.rightHand, Right, dt);
    }

    static void Read(XRHand source, VRHand target, float dt)
    {
        if (!source.isTracked)
        {
            target.Lost();
            return;
        }

        bool complete = true;
        for (int i = 0; i < VRHand.JointCount; i++)
        {
            XRHandJoint joint = source.GetJoint(XRHandJointIDUtility.FromIndex(i));
            if (joint.TryGetPose(out Pose pose)) target.Joints[i] = pose.position;
            else complete = false;
        }

        if (!complete && target.TrackedFor <= 0f)
        {
            target.Lost();
            return;
        }

        target.Tracked = true;
        target.Update(dt);
    }

    // ---- simulation --------------------------------------------------------------------------------

    void PollSimulation(float dt)
    {
        Available = true;
        PollSimulatedHand(Right, ScriptedRight, ScriptedRightTip, ScriptedRightPinch, dt, true);
        PollSimulatedHand(Left, ScriptedLeft, ScriptedLeftTip, ScriptedLeftPinch, dt, false);
    }

    void PollSimulatedHand(VRHand hand, bool scripted, Vector3 scriptedTip, float scriptedPinch, float dt, bool mouseDriven)
    {
        Vector3 tip;
        float pinch;
        if (scripted)
        {
            tip = scriptedTip;
            pinch = scriptedPinch;
        }
        else if (mouseDriven && Mouse.current != null)
        {
            Mouse mouse = Mouse.current;
            simulatedDepth = Mathf.Clamp(simulatedDepth + mouse.scroll.ReadValue().y * 0.0004f, 0.2f, 1.6f);
            Vector2 position = mouse.position.ReadValue();
            Ray ray = rig.Cam.ScreenPointToRay(position);
            Vector3 local = rig.Offset.InverseTransformPoint(ray.origin + ray.direction * simulatedDepth * rig.Scale);
            simulatedPoke = mouse.leftButton.isPressed && !(Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed);
            simulatedPinch = mouse.leftButton.isPressed && Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
            Vector3 push = rig.Offset.InverseTransformDirection(ray.direction) * (simulatedPoke ? 0.03f : 0f);
            tip = local + push;
            pinch = simulatedPinch ? 1f : 0f;
        }
        else
        {
            hand.Lost();
            return;
        }

        Vector3 head = rig.Cam.transform.localPosition;
        Vector3 pointDirection = (tip - head).normalized;
        hand.FillSynthetic(tip, pointDirection, Vector3.up, pinch);
        hand.Tracked = true;
        hand.Update(dt);
    }
}
