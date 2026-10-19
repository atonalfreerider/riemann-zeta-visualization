using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The games' marble language on the experiments stage (the long-form segment `marbles`): marble fingerprints
/// (Assets/Games/MarbleFingerprint.cs) far out on the stage at MarbleWorldOffset, each in a named slot and kept facing the
/// camera. exp:mwshow:SLOT:N:X,Y[,Z][:SCALE][:nodigits][:beside] puts n's fingerprint in the slot (X, Y in world units from
/// the marble world's origin, SCALE world units per marble), exp:mwhide:SLOT takes it away. Each slot's parameters are
/// eased with exp:tw:
///   mw.SLOT.reveal  0 … 1: the shorthand builds itself (the badge, then each container: its ring, its marbles, its rings)
///   mw.SLOT.phase   0 … Steps: the unpacking (0 the shorthand, 1 the exponents opened, Steps the whole volume)
///   mw.SLOT.depth   0 … 1: flat (symmetric spirals) … 3D (spiral sphere packs)
///   mw.SLOT.fade    0 … 1: the whole fingerprint
/// Callouts point at its parts: exp:callmw:SLOT/badge, SLOT/cup/G (container g of the shorthand), SLOT/ring/G (its
/// outermost exponent ring), SLOT/long/J (container j of the opened string), SLOT/vol and SLOT/voltop (the volume).
/// </summary>
public partial class SacksSpiral
{
    public const float MarbleWorldOffset = 128000f;
    public Vector3 MarbleOrigin => Center + new Vector3(MarbleWorldOffset, 0f, 0f);

    Transform mwRoot;
    readonly Dictionary<string, MarbleFingerprint> mwPrints = new();
    bool mwLooping;

    /// <summary>Open the marble world (an empty group; fingerprints come with exp:mwshow) and start its frame loop.</summary>
    public void ExpMarbleWorld()
    {
        ClearTweens("mw.");
        foreach (MarbleFingerprint f in mwPrints.Values)
            if (f != null) Destroy(f.gameObject);
        mwPrints.Clear();
        mwRoot = ExpGroup("MW marbles").transform;
        if (!mwLooping) StartCoroutine(MarbleWorldLoop());
    }

    /// <summary>n's fingerprint in a slot, at (x, y) from the marble world's origin, scale world units per marble.</summary>
    public void ExpMarbleShow(string slot, long n, Vector3 at, float scale, bool digits, bool beside)
    {
        if (mwRoot == null) ExpMarbleWorld();
        ExpMarbleHide(slot);
        MarbleFingerprint f = MarbleFingerprint.Create(mwRoot, n, scale, digits, false, beside);
        f.transform.position = MarbleOrigin + at;
        f.name = $"MW {slot}: {n}";
        mwPrints[slot] = f;
        Steer(slot, f);
    }

    public void ExpMarbleHide(string slot)
    {
        if (mwPrints.TryGetValue(slot, out MarbleFingerprint f) && f != null) Destroy(f.gameObject);
        mwPrints.Remove(slot);
    }

    IEnumerator MarbleWorldLoop()
    {
        mwLooping = true;
        while (mwRoot != null)
        {
            foreach (KeyValuePair<string, MarbleFingerprint> kv in mwPrints)
                if (kv.Value != null) Steer(kv.Key, kv.Value);
            yield return null;
        }

        mwLooping = false;
    }

    /// <summary>A fingerprint follows its slot's eased parameters and faces the camera (its plane parallel to the screen).</summary>
    void Steer(string slot, MarbleFingerprint f)
    {
        string k = "mw." + slot + ".";
        f.Reveal = Tw(k + "reveal", 1f);
        f.Fade = Tw(k + "fade", 1f);
        f.Drive(Tw(k + "phase", 0f), Tw(k + "depth", 0f));
        Camera cam = Camera.main;
        if (cam != null) f.transform.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);
    }

    /// <summary>For callouts: SLOT/badge, SLOT/cup/G, SLOT/ring/G, SLOT/long/J, SLOT/vol or SLOT/voltop (world space).</summary>
    public Vector3 MarbleWorldPoint(string spec)
    {
        string[] p = spec.Split('/');
        if (!mwPrints.TryGetValue(p[0], out MarbleFingerprint f) || f == null) return MarbleOrigin;
        int i = p.Length > 2 && int.TryParse(p[2], out int v) ? v : 0;
        return (p.Length > 1 ? p[1].ToLowerInvariant() : "badge") switch
        {
            "cup" => f.CupTopWorld(i),
            "ring" => f.RingTopWorld(i),
            "long" => f.LongTopWorld(i),
            "vol" => f.VolumeWorld,
            "voltop" => f.VolumeTopWorld,
            _ => f.BadgeWorld,
        };
    }
}
