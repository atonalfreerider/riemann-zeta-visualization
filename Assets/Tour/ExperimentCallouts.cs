using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Callouts and isolation for the long-form video segments. A callout is a label with an arrow that grows from it to a
/// point and blooms white as it arrives, sized by the camera's distance so it reads the same in a wide shot and a close
/// one. A focus dims every experiment group except the ones named (feature isolation), a pulse blooms a group for a
/// moment, and a draw-on grows a group's lines from their first point to their last.
/// </summary>
public partial class SacksSpiral
{
    readonly Dictionary<Renderer, float> focusDimmed = new();
    readonly Dictionary<TMP_Text, float> focusDimmedText = new();

    Transform CalloutRoot()
    {
        Transform root = ExpRoot();
        Transform g = root.Find("callouts");
        if (g == null)
        {
            g = new GameObject("callouts").transform;
            g.SetParent(root, false);
        }

        return g;
    }

    /// <summary>Where n sits on the current surface (the Sacks spiral as bent), lifted by <paramref name="lift"/>.</summary>
    public Vector3 ExpNumberPosition(double n, float lift = 0f) => ExpNumberAt(n, lift);

    /// <summary>The top of n's column in the zeros module's funnel (exp:zfunnel).</summary>
    public Vector3 ZeroFunnelTop(long n) => FunnelTop(Center + new Vector3(ZeroModuleOffset, 0f, 0f), n, ZmS, ZmH);

    /// <summary>The top of n's column in E8's funnel (hour n mod 30, radius s√n, height hScale·log n towards the viewer).</summary>
    public Vector3 FunnelTop(Vector3 o, long n, float s, float hScale) => FunnelBase(o, n, s) + Vector3.back * (hScale * Mathf.Log(n));

    /// <summary>
    /// A callout at <paramref name="target"/>: the label sits off in the screen direction <paramref name="dir"/> (x right,
    /// y up) at <paramref name="frac"/> of the camera's distance, and an arrow grows from it to the target in
    /// <paramref name="seconds"/>, blooming white as it lands and settling to its colour, while a ring closes in on the
    /// target. The label is outlined in black so it reads over a busy picture.
    /// </summary>
    public void ExpCallout(Vector3 target, Vector2 dir, string text, Color col, float frac = .12f, float seconds = .6f)
    {
        StartCoroutine(CalloutRoutine(target, dir, text, col, frac, seconds));
    }

    /// <summary>Black outline (and a slightly fatter face) on a label, so it reads over bright lines.</summary>
    static void Outline(TMP_Text t, float width = .24f)
    {
        t.fontMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, .18f);
        t.outlineWidth = width;
        t.outlineColor = new Color32(0, 0, 0, 230);
    }

    /// <summary>
    /// How far to move something with these world bounds, at about distance d from the camera, so that it lies within the
    /// safe part of the picture: in from the sides, below the board (top 20 %) and above the captions (bottom 25 %).
    /// </summary>
    static Vector3 KeepInFrame(Camera cam, Bounds b, float d)
    {
        Rect r = ViewRect(cam, b);
        float dx = r.xMin < SafeLeft ? SafeLeft - r.xMin : r.xMax > SafeRight ? SafeRight - r.xMax : 0f;
        float dy = r.yMin < SafeBottom ? SafeBottom - r.yMin : r.yMax > SafeTop ? SafeTop - r.yMax : 0f;
        if (dx == 0f && dy == 0f) return Vector3.zero;
        float h = 2f * d * Mathf.Tan(cam.fieldOfView * .5f * Mathf.Deg2Rad), w = h * cam.aspect;
        return cam.transform.right * (dx * w) + cam.transform.up * (dy * h);
    }

    /// <summary>A readout fixed in a corner of the screen (0 top-left, 1 top-right); empty text hides it.</summary>
    public static void Hud(int corner, string text, Color color)
    {
        CaptionDisplay captions = TourDirector.Instance != null ? TourDirector.Instance.Captions : null;
        if (captions == null) return;
        if (string.IsNullOrEmpty(text)) captions.HideCorner(corner);
        else captions.ShowCorner(corner, text, color);
    }

    /// <summary>Draw a callout's part after the picture's transparent parts, so a curtain or a cloud never covers it.</summary>
    static void OnTop(Renderer r, int order)
    {
        if (r != null) r.sortingOrder = order;
    }

    /// <summary>A ring of <paramref name="radius"/> round <paramref name="c"/>, facing the camera.</summary>
    static Vector3[] FacingRing(Vector3 c, float radius, Vector3 right, Vector3 up, int n = 40)
    {
        Vector3[] pts = new Vector3[n + 1];
        for (int k = 0; k <= n; k++)
        {
            float a = k * 2f * Mathf.PI / n;
            pts[k] = c + (right * Mathf.Cos(a) + up * Mathf.Sin(a)) * radius;
        }

        return pts;
    }

    // the labels of the callouts on screen, so a new one can find a place that does not cover them
    readonly List<TMP_Text> calloutLabels = new();

    /// <summary>The part of the picture (viewport, 0 … 1) that world bounds b cover.</summary>
    static Rect ViewRect(Camera cam, Bounds b)
    {
        float xMin = 1e9f, xMax = -1e9f, yMin = 1e9f, yMax = -1e9f;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = new(i % 2 == 0 ? b.min.x : b.max.x, (i / 2) % 2 == 0 ? b.min.y : b.max.y, i / 4 == 0 ? b.min.z : b.max.z);
            Vector3 v = cam.WorldToViewportPoint(corner);
            xMin = Mathf.Min(xMin, v.x); xMax = Mathf.Max(xMax, v.x);
            yMin = Mathf.Min(yMin, v.y); yMax = Mathf.Max(yMax, v.y);
        }

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    static float OverlapArea(Rect a, Rect b)
    {
        float w = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin), h = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
        return w > 0f && h > 0f ? w * h : 0f;
    }

    // the safe part of the picture: in from the sides, below the board, above the captions
    const float SafeLeft = .03f, SafeRight = .97f, SafeBottom = .25f, SafeTop = .8f;

    static float OutsideArea(Rect r)
    {
        Rect safe = Rect.MinMaxRect(SafeLeft, SafeBottom, SafeRight, SafeTop);
        return r.width * r.height - OverlapArea(r, safe);
    }

    Camera calloutGhost;
    CameraRig calloutRig;

    /// <summary>
    /// A camera standing where the camera rig is heading (not where the camera is while it flies there), so a callout made
    /// during a move is sized and placed for the shot it will be seen in.
    /// </summary>
    Camera CalloutCamera()
    {
        Camera main = Camera.main;
        if (main == null) return null;
        if (calloutRig == null) calloutRig = FindAnyObjectByType<CameraRig>();
        if (calloutRig == null || !calloutRig.TargetPose(out Vector3 pos, out Quaternion rot, out float fov, true)) return main;
        if (calloutGhost == null)
        {
            calloutGhost = new GameObject("callout ghost camera").AddComponent<Camera>();
            calloutGhost.enabled = false;
            calloutGhost.transform.SetParent(transform, false);
        }

        calloutGhost.transform.SetPositionAndRotation(pos, rot);
        calloutGhost.fieldOfView = fov;
        calloutGhost.aspect = main.aspect;
        calloutGhost.nearClipPlane = main.nearClipPlane;
        calloutGhost.farClipPlane = main.farClipPlane;
        return calloutGhost;
    }

    IEnumerator CalloutRoutine(Vector3 target, Vector2 dir, string text, Color col, float frac, float seconds)
    {
        Transform g = CalloutRoot();
        Camera cam = CalloutCamera();
        // its depth in the shot (along the view, so a target to the side of the picture gets the same size), clamped: a
        // target far off the picture, e.g. in a scene not drawn, must not make a label the size of the world
        float d = cam != null ? Mathf.Clamp(Vector3.Dot(target - cam.transform.position, cam.transform.forward), 1f, 2000f) : 100f;
        Vector3 right = cam != null ? cam.transform.right : Vector3.right, up = cam != null ? cam.transform.up : Vector3.up;
        Vector3 back = cam != null ? -cam.transform.forward : Vector3.back;
        // no direction given: away from the middle of the picture
        if (dir.sqrMagnitude < 1e-6f)
        {
            Vector3 v = cam != null ? cam.WorldToViewportPoint(target) : new Vector3(.6f, .6f, 1f);
            dir = new Vector2(v.x - .5f, v.y - .52f);
            if (dir.sqrMagnitude < 1e-4f) dir = new Vector2(1f, 1f);
        }

        dir.Normalize();
        TextBox label = ExpLabelWide(g, text, target, d * .42f * CalloutScale, col);
        TMP_Text t = label.TextField;
        Outline(t);
        OnTop(t.GetComponent<Renderer>(), 31);
        if (cam != null) label.transform.rotation = cam.transform.rotation; // as the billboard will turn it, so its bounds are right
        Vector3 tail = target;

        // place the label beyond the arrow's tail in direction c, aligned away from the arrow
        void Place(Vector2 c)
        {
            Vector3 off = (right * c.x + up * c.y).normalized;
            tail = target + off * (d * frac);
            label.Alignment = c.x > .38f ? TextAlignmentOptions.Left : c.x < -.38f ? TextAlignmentOptions.Right : TextAlignmentOptions.Center;
            label.transform.position = tail + off * (d * .014f) + (Mathf.Abs(c.x) <= .38f ? up * Mathf.Sign(c.y) * d * .018f : Vector3.zero) + back * (d * .01f);
            t.ForceMeshUpdate();
        }

        // the given direction first, then turned by more and more, until the label covers no other callout and stays in the picture
        calloutLabels.RemoveAll(x => x == null);
        Vector2 best = dir;
        if (cam != null)
        {
            float bestScore = float.MaxValue;
            foreach (float turn in new[] { 0f, 25f, -25f, 50f, -50f, 75f, -75f, 105f, -105f, 135f, -135f, 165f, -165f })
            {
                float a = turn * Mathf.Deg2Rad;
                Vector2 c = new(dir.x * Mathf.Cos(a) - dir.y * Mathf.Sin(a), dir.x * Mathf.Sin(a) + dir.y * Mathf.Cos(a));
                Place(c);
                Rect r = ViewRect(cam, t.GetComponent<Renderer>().bounds);
                float score = OutsideArea(r) * 4f;
                foreach (TMP_Text o in calloutLabels) score += OverlapArea(r, ViewRect(cam, o.GetComponent<Renderer>().bounds));
                score += Mathf.Abs(turn) * 1e-6f; // ties go to the direction asked for
                if (score < bestScore)
                {
                    bestScore = score;
                    best = c;
                }

                if (score < 1e-4f) break;
            }
        }

        Place(best);
        // whatever still sticks out of the picture slides back in
        if (cam != null)
        {
            Vector3 shift = KeepInFrame(cam, t.GetComponent<Renderer>().bounds, d);
            label.transform.position += shift;
            tail += shift;
        }

        calloutLabels.Add(t);
        Vector3 head = target + (tail - target).normalized * (d * .02f);
        float width = d * .0034f;
        LineRenderer shaft = Factory.NewLine("callout arrow", width, Color.white, AttentionIntensity);
        shaft.transform.SetParent(g, false);
        shaft.positionCount = 2;
        Vector3 along = (head - tail).normalized, side = Vector3.Cross(along, back).normalized;
        LineRenderer wings = Factory.NewLine("callout head", width, Color.white, AttentionIntensity);
        wings.transform.SetParent(g, false);
        wings.positionCount = 3;
        wings.SetPositions(new[] { head, head, head });
        // the ring round the target: wide and faint at first, closing in and blooming as the arrow lands
        float ringR = d * .014f;
        LineRenderer ring = Factory.NewLine("callout ring", width * .8f, Color.white, AttentionIntensity);
        ring.transform.SetParent(g, false);
        ring.positionCount = 41;
        ring.SetPositions(FacingRing(target + back * (d * .004f), ringR * 3f, right, up));
        OnTop(shaft, 30);
        OnTop(wings, 30);
        OnTop(ring, 30);
        t.alpha = 0f;
        float time = 0f;
        while (time < seconds)
        {
            if (shaft == null) yield break; // the stage was cleared under us
            float u = Mathf.SmoothStep(0f, 1f, time / seconds);
            shaft.SetPositions(new[] { tail, Vector3.Lerp(tail, head, u) });
            ring.SetPositions(FacingRing(target + back * (d * .004f), ringR * Mathf.Lerp(3f, 1f, u), right, up));
            ring.startColor = ring.endColor = new Color(1f, 1f, 1f, u);
            t.alpha = Mathf.Clamp01(time / (seconds * .5f));
            time += Time.deltaTime;
            yield return null;
        }

        if (shaft == null) yield break;
        Vector3 wingA = head - along * (d * .024f) + side * (d * .011f), wingB = head - along * (d * .024f) - side * (d * .011f);
        shaft.SetPositions(new[] { tail, head });
        wings.SetPositions(new[] { wingA, head, wingB });
        ring.SetPositions(FacingRing(target + back * (d * .004f), ringR, right, up));
        t.alpha = 1f;
        // white bloom on arrival, settling to the callout's colour; afterwards the label keeps itself in the picture as the
        // camera moves (the arrow's tail follows it)
        time = 0f;
        const float settle = 1.2f;
        while (true)
        {
            if (shaft == null || label == null) yield break;
            if (time < settle)
            {
                float u = time / settle;
                Color c = Color.Lerp(Color.white, col, u);
                float k = Mathf.Lerp(AttentionIntensity * 1.3f, Factory.GlowIntensity, u);
                foreach (LineRenderer l in new[] { shaft, wings, ring })
                {
                    l.startColor = l.endColor = c;
                    Factory.SetIntensity(l, k);
                }
            }

            cam = CalloutCamera();
            if (cam != null)
            {
                Vector3 shift = KeepInFrame(cam, t.GetComponent<Renderer>().bounds, Vector3.Distance(cam.transform.position, label.transform.position));
                if (shift.sqrMagnitude > 1e-8f)
                {
                    label.transform.position += shift;
                    tail += shift;
                    shaft.SetPosition(0, tail);
                }
            }

            time += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>Show or hide the children of the experiment group(s) <paramref name="group"/> whose names start with <paramref name="prefix"/>.</summary>
    public void ExpSetChildren(string group, string prefix, bool on)
    {
        foreach (Transform g in ExpRoot())
        {
            if (g.name != group) continue;
            foreach (Transform t in g.GetComponentsInChildren<Transform>(true))
                if (t != g && t.name.StartsWith(prefix)) t.gameObject.SetActive(on);
        }
    }

    /// <summary>Remove the experiment groups whose names start with <paramref name="prefix"/>.</summary>
    public void ExpRemoveGroups(string prefix)
    {
        List<GameObject> gone = new();
        foreach (Transform g in ExpRoot()) if (g.name.StartsWith(prefix)) gone.Add(g.gameObject);
        foreach (GameObject g in gone) Destroy(g);
    }

    /// <summary>Remove every callout.</summary>
    public void ExpClearCallouts()
    {
        Transform g = ExpRoot().Find("callouts");
        if (g != null) Destroy(g.gameObject);
    }

    static bool GroupMatches(string name, string[] keep)
    {
        foreach (string k in keep)
            if (k.Length > 0 && (name == k || name.StartsWith(k))) return true;
        return false;
    }

    /// <summary>
    /// Feature isolation: dim every experiment group except the ones named (or starting with a named prefix) to
    /// <paramref name="level"/> of its glow, and their labels to a third. Callouts and captions are never dimmed.
    /// </summary>
    public void ExpFocus(string[] keep, float level = .16f)
    {
        ExpUnfocus();
        foreach (Transform g in ExpRoot())
        {
            if (g.name == "callouts" || g.name == "caption" || GroupMatches(g.name, keep)) continue;
            foreach (Renderer r in g.GetComponentsInChildren<Renderer>(true))
            {
                if (r.GetComponent<TMP_Text>() != null) continue;
                float k = Factory.GetIntensity(r);
                if (k <= 0f) k = 1f;
                focusDimmed[r] = k;
                Factory.SetIntensity(r, k * level);
            }

            foreach (TMP_Text t in g.GetComponentsInChildren<TMP_Text>(true))
            {
                focusDimmedText[t] = t.alpha;
                t.alpha *= .3f;
            }
        }
    }

    /// <summary>Undo a focus.</summary>
    public void ExpUnfocus()
    {
        foreach (var kv in focusDimmed)
            if (kv.Key != null) Factory.SetIntensity(kv.Key, kv.Value);
        foreach (var kv in focusDimmedText)
            if (kv.Key != null) kv.Key.alpha = kv.Value;
        focusDimmed.Clear();
        focusDimmedText.Clear();
    }

    /// <summary>Bloom every renderer of the groups named for <paramref name="seconds"/>, peaking at <paramref name="peak"/> times its glow.</summary>
    public void ExpPulse(string[] groups, float seconds = 1.6f, float peak = 2.2f)
    {
        List<(Renderer r, float k)> rs = new();
        foreach (Transform g in ExpRoot())
        {
            if (!GroupMatches(g.name, groups)) continue;
            foreach (Renderer r in g.GetComponentsInChildren<Renderer>(true))
            {
                if (r.GetComponent<TMP_Text>() != null) continue;
                float k = Factory.GetIntensity(r);
                rs.Add((r, k <= 0f ? 1f : k));
            }
        }

        StartCoroutine(PulseRoutine(rs, seconds, peak));
    }

    IEnumerator PulseRoutine(List<(Renderer r, float k)> rs, float seconds, float peak)
    {
        float time = 0f;
        while (time < seconds)
        {
            float u = Mathf.Sin(Mathf.PI * time / seconds);
            foreach (var (r, k) in rs)
                if (r != null) Factory.SetIntensity(r, k * (1f + (peak - 1f) * u));
            time += Time.deltaTime;
            yield return null;
        }

        foreach (var (r, k) in rs)
            if (r != null) Factory.SetIntensity(r, k);
    }

    /// <summary>
    /// Grow every line of the groups named from its first point to its last in <paramref name="seconds"/>. A name
    /// "GROUP/prefix" takes only the lines whose names start with the prefix. Sequential: one line after another, in
    /// the order they were made, each in an equal share of the time.
    /// </summary>
    public void ExpDrawOn(string[] groups, float seconds, bool sequential = false)
    {
        List<(LineRenderer l, Vector3[] pts)> ls = new();
        foreach (string spec in groups)
        {
            string[] parts = spec.Split('/');
            string prefix = parts.Length > 1 ? parts[1] : "";
            foreach (Transform g in ExpRoot())
            {
                if (!GroupMatches(g.name, new[] { parts[0] })) continue;
                foreach (LineRenderer l in g.GetComponentsInChildren<LineRenderer>(true))
                {
                    if (l.positionCount < 2 || (prefix.Length > 0 && !l.name.StartsWith(prefix))) continue;
                    Vector3[] pts = new Vector3[l.positionCount];
                    l.GetPositions(pts);
                    ls.Add((l, pts));
                    l.positionCount = 2;
                    l.SetPositions(new[] { pts[0], pts[0] });
                }
            }
        }

        StartCoroutine(sequential ? DrawSeqRoutine(ls, seconds) : DrawOnRoutine(ls, seconds));
    }

    IEnumerator DrawSeqRoutine(List<(LineRenderer l, Vector3[] pts)> ls, float seconds)
    {
        float per = seconds / Mathf.Max(1, ls.Count);
        foreach (var item in ls)
            yield return DrawOnRoutine(new List<(LineRenderer l, Vector3[] pts)> { item }, per);
    }

    IEnumerator DrawOnRoutine(List<(LineRenderer l, Vector3[] pts)> ls, float seconds)
    {
        float time = 0f;
        while (time < seconds)
        {
            float u = time / seconds;
            foreach (var (l, pts) in ls)
            {
                if (l == null) continue;
                float f = u * (pts.Length - 1);
                int k = Mathf.Clamp(Mathf.FloorToInt(f), 0, pts.Length - 2);
                Vector3[] part = new Vector3[k + 2];
                System.Array.Copy(pts, part, k + 1);
                part[k + 1] = Vector3.Lerp(pts[k], pts[k + 1], f - k);
                l.positionCount = part.Length;
                l.SetPositions(part);
            }

            time += Time.deltaTime;
            yield return null;
        }

        foreach (var (l, pts) in ls)
        {
            if (l == null) continue;
            l.positionCount = pts.Length;
            l.SetPositions(pts);
        }
    }
}
