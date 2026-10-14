using System.Collections.Generic;
using UnityEngine;

/// <summary>Procedural low-poly meshes shared by the VR layer.</summary>
public static class VRMeshes
{
    static Mesh icosphere;
    static Mesh lowSphere;

    /// <summary>Icosphere of diameter 1 (80 triangles with one subdivision).</summary>
    public static Mesh Icosphere
    {
        get
        {
            if (icosphere == null) icosphere = BuildIcosphere(1, "VR Icosphere");
            return icosphere;
        }
    }

    /// <summary>Replacement for Unity's 768-triangle sphere primitive when thousands of dots are on screen.</summary>
    public static Mesh LowPolySphere
    {
        get
        {
            if (lowSphere == null) lowSphere = BuildIcosphere(1, "VR Low Poly Sphere");
            return lowSphere;
        }
    }

    static Mesh BuildIcosphere(int subdivisions, string name)
    {
        float t = (1f + Mathf.Sqrt(5f)) / 2f;
        List<Vector3> v = new()
        {
            new(-1, t, 0), new(1, t, 0), new(-1, -t, 0), new(1, -t, 0),
            new(0, -1, t), new(0, 1, t), new(0, -1, -t), new(0, 1, -t),
            new(t, 0, -1), new(t, 0, 1), new(-t, 0, -1), new(-t, 0, 1)
        };
        for (int i = 0; i < v.Count; i++) v[i] = v[i].normalized;
        List<int> tri = new()
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
        };
        for (int s = 0; s < subdivisions; s++)
        {
            Dictionary<long, int> cache = new();
            List<int> next = new();
            int Mid(int a, int b)
            {
                long key = ((long)Mathf.Min(a, b) << 32) + Mathf.Max(a, b);
                if (cache.TryGetValue(key, out int m)) return m;
                v.Add(((v[a] + v[b]) * .5f).normalized);
                cache[key] = v.Count - 1;
                return v.Count - 1;
            }

            for (int i = 0; i < tri.Count; i += 3)
            {
                int a = tri[i], b = tri[i + 1], c = tri[i + 2];
                int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }

            tri = next;
        }

        Mesh mesh = new() { name = name };
        Vector3[] verts = new Vector3[v.Count];
        Color[] colors = new Color[v.Count];
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i] = v[i] * .5f;
            colors[i] = Color.white;
        }

        mesh.vertices = verts;
        mesh.colors = colors;
        mesh.triangles = tri.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}

/// <summary>
/// Draws what the user's hands are doing: a glowing fingertip cursor (cyan; gold while touching a button; green while
/// holding the world) and, in blackout, a light-weight skeleton of the whole hand. In passthrough the real hands are
/// visible, so only the cursors are drawn unless the skeleton is switched on.
/// </summary>
public class VRHandVisual : MonoBehaviour
{
    public enum SkeletonMode { Auto, On, Off }
    public SkeletonMode Skeleton = SkeletonMode.Auto;

    class View
    {
        public Transform Root;
        public Transform Index, Thumb;
        public MeshRenderer IndexRenderer, ThumbRenderer;
        public Transform[] Joints = new Transform[VRHand.JointCount];
        public LineRenderer[] Bones = new LineRenderer[5];
    }

    VRRig rig;
    VRHands hands;
    VRPoker poker;
    VRWorldGrab grab;
    Material overlay;
    MaterialPropertyBlock block; // created in Build: Unity forbids it in a field initializer
    readonly Dictionary<VRHand, View> views = new();
    readonly Vector3[] chain = new Vector3[6];
    static readonly int ColorId = Shader.PropertyToID("_Color");

    public static VRHandVisual Create(VRRig rig, VRHands hands, VRPoker poker, VRWorldGrab grab)
    {
        VRHandVisual v = rig.gameObject.AddComponent<VRHandVisual>();
        v.rig = rig;
        v.hands = hands;
        v.poker = poker;
        v.grab = grab;
        v.Build();
        return v;
    }

    void Build()
    {
        block = new MaterialPropertyBlock();
        Shader glow = Shader.Find("Zeta/GlowLine");
        overlay = new Material(glow != null ? glow : Shader.Find("Sprites/Default")) { name = "VR Hands", enableInstancing = true };
        overlay.SetFloat("_ZTest", 8f); // always on top
        overlay.SetFloat("_Intensity", 1f);
        overlay.renderQueue = 4000;

        foreach (VRHand hand in new[] { hands.Left, hands.Right })
        {
            View view = new() { Root = new GameObject(hand.IsRight ? "Right Hand" : "Left Hand").transform };
            view.Root.SetParent(rig.Offset, false);
            view.Index = Sphere("Index tip", view.Root, out view.IndexRenderer);
            view.Thumb = Sphere("Thumb tip", view.Root, out view.ThumbRenderer);
            for (int i = 0; i < VRHand.JointCount; i++)
            {
                view.Joints[i] = Sphere("joint " + i, view.Root, out MeshRenderer r);
                SetColor(r, new Color(0.55f, 0.85f, 1f, 0.55f));
            }

            for (int i = 0; i < view.Bones.Length; i++)
            {
                LineRenderer line = new GameObject("bone " + i).AddComponent<LineRenderer>();
                line.transform.SetParent(view.Root, false);
                line.useWorldSpace = false;
                line.material = overlay;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.numCapVertices = 3;
                line.startColor = line.endColor = new Color(0.55f, 0.85f, 1f, 0.5f);
                view.Bones[i] = line;
            }

            views[hand] = view;
        }
    }

    Transform Sphere(string name, Transform parent, out MeshRenderer renderer)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = VRMeshes.Icosphere;
        renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = overlay;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return go.transform;
    }

    void SetColor(MeshRenderer r, Color c)
    {
        block.SetColor(ColorId, c);
        r.SetPropertyBlock(block);
    }

    void LateUpdate()
    {
        bool skeleton = Skeleton == SkeletonMode.On || (Skeleton == SkeletonMode.Auto && !rig.Passthrough);
        float scale = rig.Scale; // lines are measured in world units, so keep them a few millimetres wide in the user's space
        foreach (KeyValuePair<VRHand, View> pair in views)
        {
            VRHand hand = pair.Key;
            View view = pair.Value;
            bool show = hand.Tracked;
            view.Root.gameObject.SetActive(show);
            if (!show) continue;

            int feedback = poker != null ? poker.Feedback(hand) : -1;
            bool holding = grab != null && grab.IsHolding(hand);
            Color cursor = holding ? VRStyle.Good : feedback == 1 ? VRStyle.Accent : feedback == 0 ? new Color(1f, 0.95f, 0.55f) : new Color(0.5f, 0.92f, 1f);
            if (hand.Pinching && !holding) cursor = new Color(0.85f, 0.8f, 1f);
            float tipSize = feedback == 1 ? 0.011f : 0.016f;

            view.Index.localPosition = hand.IndexTip;
            view.Index.localScale = Vector3.one * tipSize;
            SetColor(view.IndexRenderer, new Color(cursor.r, cursor.g, cursor.b, 0.95f));
            view.Thumb.localPosition = hand.ThumbTipPos;
            view.Thumb.localScale = Vector3.one * 0.012f;
            SetColor(view.ThumbRenderer, new Color(cursor.r, cursor.g, cursor.b, 0.8f));

            for (int i = 0; i < VRHand.JointCount; i++)
            {
                bool tip = i == VRHand.IndexTipId || i == VRHand.ThumbTip;
                view.Joints[i].gameObject.SetActive(skeleton && !tip);
                if (!skeleton || tip) continue;
                view.Joints[i].localPosition = hand.Joints[i];
                view.Joints[i].localScale = Vector3.one * 0.0085f;
            }

            for (int c = 0; c < view.Bones.Length; c++)
            {
                LineRenderer line = view.Bones[c];
                line.enabled = skeleton;
                if (!skeleton) continue;
                int[] ids = VRHand.Chains[c];
                line.positionCount = ids.Length;
                for (int i = 0; i < ids.Length; i++) line.SetPosition(i, hand.Joints[ids[i]]);
                line.widthMultiplier = 0.0042f * scale;
            }
        }
    }
}

/// <summary>
/// Pinch to take hold of the model: one hand drags it, two hands drag, scale and turn it together (the line between
/// the pinches stays glued to the same two points of the model). What moves is the rig, so the headset's own
/// tracking is untouched and the model stays put in the world when you let go.
/// </summary>
public class VRWorldGrab : MonoBehaviour
{
    public bool Enabled = true;

    class Hold
    {
        public bool Active;
        public Vector3 Anchor;   // world point of the model that the pinch took hold of
    }

    VRRig rig;
    VRHands hands;
    VRPoker poker;
    readonly Hold left = new(), right = new();
    bool twoHanded;

    public bool Holding => left.Active || right.Active;
    public bool TwoHanded => twoHanded;
    public System.Action Grabbed;

    public static VRWorldGrab Create(VRRig rig, VRHands hands, VRPoker poker)
    {
        VRWorldGrab g = rig.gameObject.AddComponent<VRWorldGrab>();
        g.rig = rig;
        g.hands = hands;
        g.poker = poker;
        return g;
    }

    public bool IsHolding(VRHand hand) => (hand.IsRight ? right : left).Active;

    void Update()
    {
        if (!Enabled)
        {
            left.Active = right.Active = false;
            return;
        }

        UpdateHold(hands.Left, left);
        UpdateHold(hands.Right, right);

        if (left.Active && right.Active)
        {
            if (!twoHanded)
            {
                twoHanded = true;
                // re-anchor both hands at where they are now so the transition is seamless
                left.Anchor = rig.Offset.TransformPoint(hands.Left.PinchPoint);
                right.Anchor = rig.Offset.TransformPoint(hands.Right.PinchPoint);
            }

            ApplyTwoHanded();
        }
        else if (left.Active || right.Active)
        {
            if (twoHanded)
            {
                twoHanded = false;
                Hold h = left.Active ? left : right;
                VRHand hand = left.Active ? hands.Left : hands.Right;
                h.Anchor = rig.Offset.TransformPoint(hand.PinchPoint);
            }

            ApplyOneHanded(left.Active ? hands.Left : hands.Right, left.Active ? left : right);
        }
        else
        {
            twoHanded = false;
        }
    }

    /// <summary>
    /// A hand hanging at the side is not grabbing. A pinch only takes hold when the hand is raised (above roughly
    /// chest height) and in front of the user; a hold lets go once the hand drops to the side.
    /// </summary>
    bool HandRaised(VRHand hand, float dropAllowance)
    {
        Vector3 head = rig.Cam.transform.localPosition;
        Vector3 p = hand.PinchPoint;
        if (p.y < head.y - (0.45f + dropAllowance)) return false;
        Vector3 forward = Vector3.ProjectOnPlane(rig.Cam.transform.localRotation * Vector3.forward, Vector3.up).normalized;
        Vector3 toHand = Vector3.ProjectOnPlane(p - head, Vector3.up);
        return Vector3.Dot(toHand, forward) > -0.05f;
    }

    void UpdateHold(VRHand hand, Hold hold)
    {
        if (!hand.Tracked)
        {
            hold.Active = false;
            return;
        }

        if (hold.Active)
        {
            if (!hand.Pinching || !HandRaised(hand, 0.2f)) hold.Active = false;
            return;
        }

        if (hand.PinchDown && HandRaised(hand, 0f) && !poker.NearPanel(hand.PinchPoint, 0.07f) && !poker.Hovering(hand))
        {
            hold.Active = true;
            hold.Anchor = rig.Offset.TransformPoint(hand.PinchPoint);
            rig.CancelAnimation();
            Grabbed?.Invoke();
        }
    }

    void ApplyOneHanded(VRHand hand, Hold hold)
    {
        Vector3 current = rig.Offset.TransformPoint(hand.PinchPoint);
        VRRig.Placement p = rig.Current;
        p.Position += hold.Anchor - current;
        rig.SetPlacement(p);
    }

    void ApplyTwoHanded()
    {
        Vector3 h1 = hands.Left.PinchPoint, h2 = hands.Right.PinchPoint;   // tracking space
        Vector3 g1 = left.Anchor, g2 = right.Anchor;                       // world
        Vector3 localSpan = h2 - h1;
        Vector3 worldSpan = g2 - g1;
        if (localSpan.magnitude < 0.03f || worldSpan.magnitude < 1e-5f) return;

        float scale = Mathf.Clamp(worldSpan.magnitude / localSpan.magnitude, 1e-2f, 2e3f);
        Quaternion swing = Quaternion.FromToRotation(localSpan, worldSpan);

        // keep the horizon level: spin about the axis between the hands until the rig's up is as close to world up as it can be
        Vector3 axis = worldSpan.normalized;
        Vector3 up = Vector3.ProjectOnPlane(swing * Vector3.up, axis);
        Vector3 world = Vector3.ProjectOnPlane(Vector3.up, axis);
        Quaternion rotation = swing;
        if (up.magnitude > 0.2f && world.magnitude > 0.2f)
        {
            rotation = Quaternion.AngleAxis(Vector3.SignedAngle(up, world, axis), axis) * swing;
        }

        Vector3 midLocal = (h1 + h2) * .5f;
        Vector3 midWorld = (g1 + g2) * .5f;
        rig.SetPlacement(new VRRig.Placement { Rotation = rotation, Scale = scale, Position = midWorld - rotation * (scale * midLocal) });
    }

    /// <summary>Segment between the two pinch points while holding with both hands (for a guide line).</summary>
    public bool TryGetSpan(out Vector3 a, out Vector3 b)
    {
        a = b = Vector3.zero;
        if (!twoHanded) return false;
        a = hands.Left.PinchPoint;
        b = hands.Right.PinchPoint;
        return true;
    }
}
