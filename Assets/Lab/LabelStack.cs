using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps a group of billboard labels from covering one another on the screen. Each label sits at its anchor, nudged along
/// the camera's right and up by its offset (world units, as ScreenNudge does); in the order they were added, a label that
/// would overlap one already placed slides down the screen until it is clear, so a path's labels read top to bottom in the
/// path's order wherever they bunch up. A label that yields is hidden instead while it would overlap. Runs after the camera
/// and the billboards have moved.
/// </summary>
[DefaultExecutionOrder(1002)]
public class LabelStack : MonoBehaviour
{
    class Entry
    {
        public TextBox Label;
        public Vector3 Anchor;
        public Vector2 Offset;
        public bool Yields;
    }

    readonly List<Entry> entries = new();
    readonly List<Rect> placed = new();
    const float Gap = 3f;                                  // pixels between stacked labels

    public void Add(TextBox label, Vector3 anchor, Vector2 offset, bool yields = false)
    {
        entries.Add(new Entry { Label = label, Anchor = anchor, Offset = offset, Yields = yields });
        Camera cam = Camera.main;
        if (cam != null) label.transform.position = anchor + cam.transform.right * offset.x + cam.transform.up * offset.y;
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        placed.Clear();
        // the labels that hold their places first, in order; then the ones that give way
        foreach (Entry e in entries) if (!e.Yields) Place(e, cam);
        foreach (Entry e in entries) if (e.Yields) Place(e, cam);
    }

    void Place(Entry e, Camera cam)
    {
        if (e.Label == null) return;
        Transform t = cam.transform;
        Vector3 p = e.Anchor + t.right * e.Offset.x + t.up * e.Offset.y;
        e.Label.transform.position = p;
        if (!e.Label.gameObject.activeInHierarchy) return;
        if (!ScreenRect(cam, e.Label, p, out Rect r, out float perUnit))
        {
            e.Label.TextField.enabled = true;
            return;
        }

        if (e.Yields)
        {
            bool clear = !Overlaps(r, out _);
            e.Label.TextField.enabled = clear;
            if (clear) placed.Add(r);
            return;
        }

        float drop = 0f;
        for (int guard = 0; guard < 24 && Overlaps(r, out Rect hit); guard++)
        {
            float d = r.yMax - hit.yMin + Gap;
            r.y -= d;
            drop += d;
        }

        if (drop > 0f) e.Label.transform.position = p - t.up * (drop / perUnit);
        e.Label.TextField.enabled = true;
        placed.Add(r);
    }

    bool Overlaps(Rect r, out Rect hit)
    {
        foreach (Rect q in placed)
            if (r.Overlaps(q))
            {
                hit = q;
                return true;
            }

        hit = default;
        return false;
    }

    /// <summary>The label's text on the screen (pixels), from its text bounds; perUnit is pixels per world unit at its depth.</summary>
    static bool ScreenRect(Camera cam, TextBox label, Vector3 at, out Rect r, out float perUnit)
    {
        r = default;
        perUnit = 0f;
        Bounds b = label.TextField.textBounds;
        if (b.size.x <= 0f)
        {
            label.TextField.ForceMeshUpdate();
            b = label.TextField.textBounds;
            if (b.size.x <= 0f) return false;
        }

        Transform t = cam.transform;
        Vector3 s = label.transform.lossyScale;
        Vector3 centre = at + t.right * (b.center.x * s.x) + t.up * (b.center.y * s.y);
        Vector3 sc = cam.WorldToScreenPoint(centre);
        if (sc.z <= .01f) return false;
        perUnit = cam.orthographic
            ? cam.pixelHeight / (2f * cam.orthographicSize)
            : cam.pixelHeight / (2f * sc.z * Mathf.Tan(cam.fieldOfView * .5f * Mathf.Deg2Rad));
        float w = b.size.x * s.x * perUnit, h = b.size.y * s.y * perUnit;
        r = new Rect(sc.x - w * .5f, sc.y - h * .5f, w, h);
        return true;
    }
}
