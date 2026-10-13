using System;
using UnityEngine;

/// <summary>
/// Global switch for the VR presentation. On a Quest (Android) the visualization always runs in VR with hand
/// tracking; on the desktop it can be switched on with <c>-vr</c> or <c>Zeta ▸ VR ▸ Desktop Simulation</c>, which
/// replaces the headset with a free camera and the hands with the mouse, so the whole VR interface can be developed
/// and screenshot-tested without wearing anything.
/// </summary>
public static class VRMode
{
    /// <summary>The VR layer owns the camera, the captions and the interface.</summary>
    public static bool Enabled { get; private set; }

    /// <summary>A real XR session (otherwise the desktop simulation).</summary>
    public static bool Headset { get; private set; }

    public static VRRig Rig { get; private set; }
    public static VRGuide Guide { get; set; }
    public static VRApp App { get; set; }

    /// <summary>Tests set this before loading the scene to get the VR layer without a window or a headset.</summary>
    public static bool RequestSimulation;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Enabled = false;
        Headset = false;
        Rig = null;
        Guide = null;
        App = null;
    }

    /// <summary>True when the visualization should start in VR; <paramref name="headset"/> says whether a real headset is expected.</summary>
    public static bool Wanted(out bool headset)
    {
        headset = false;
        if (RequestSimulation) return true;
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-vr") >= 0) return true;
#if UNITY_EDITOR
        return UnityEditor.EditorPrefs.GetBool("Zeta.VRSimulate", false);
#else
        headset = Application.platform == RuntimePlatform.Android;
        return headset;
#endif
    }

    internal static void Register(VRRig rig, bool headset)
    {
        Enabled = true;
        Headset = headset;
        Rig = rig;
    }
}
