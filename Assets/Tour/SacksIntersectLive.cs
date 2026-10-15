using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>What exp:reveal says on the caption board, and where it flares, when an experiment item appears.</summary>
public class RevealNote : MonoBehaviour
{
    public string Text;
    public readonly List<Vector3> FlareAt = new();
}

/// <summary>
/// The moving pictures of the AA–AE modules: a generic reveal (the items of a group appear one by one, their lines drawing
/// in, their notes on the caption board, flares at their primes), the residue torus filling in panel row by panel row, and
/// the prime race drawn from left to right with its lead changes flaring.
/// </summary>
public partial class SacksSpiral
{
    /// <summary>The child "reveal" of a group: the items exp:reveal shows one at a time, in creation order.</summary>
    static Transform RevealItems(Transform g)
    {
        Transform t = g.Find("reveal");
        if (t != null) return t;
        t = new GameObject("reveal").transform;
        t.SetParent(g, false);
        return t;
    }

    Transform FindExpGroup(string name)
    {
        if (experimentsRoot == null) return null;
        Transform found = null;
        foreach (Transform t in experimentsRoot.transform) if (t.name == name) found = t; // the most recent one
        return found;
    }

    /// <summary>Hide the items of a group until a later exp:reveal shows them (exp:hide:GROUP).</summary>
    public void ExpHideItems(string groupName)
    {
        Transform g = FindExpGroup(groupName);
        if (g == null) return;
        Transform items = g.Find("reveal") ?? g;
        foreach (Transform t in items) t.gameObject.SetActive(false);
    }

    /// <summary>Show the items of a group one by one over <paramref name="seconds"/> (exp:reveal:GROUP:SECONDS).</summary>
    public void ExpRevealLive(string groupName, float seconds)
    {
        Transform g = FindExpGroup(groupName);
        if (g == null)
        {
            Debug.LogWarning("exp:reveal: no group " + groupName);
            return;
        }

        Transform items = g.Find("reveal") ?? g;
        List<GameObject> list = new();
        foreach (Transform t in items) list.Add(t.gameObject);
        foreach (GameObject o in list) o.SetActive(false);
        StartLive(RevealLive(list, seconds));
    }

    IEnumerator RevealLive(List<GameObject> items, float seconds)
    {
        float per = seconds * .92f / Mathf.Max(1, items.Count), clock = 0f;
        for (int i = 0; i < items.Count; i++)
        {
            GameObject o = items[i];
            if (o == null) yield break;
            o.SetActive(true);
            float draw = Mathf.Min(per * .85f, 1.2f);
            foreach (LineRenderer l in o.GetComponentsInChildren<LineRenderer>())
                if (l.positionCount > 4 && !l.loop) StartCoroutine(DrawLine(l, draw));
            RevealNote note = o.GetComponent<RevealNote>();
            if (note != null)
            {
                if (!string.IsNullOrEmpty(note.Text)) Board(note.Text);
                foreach (Vector3 p in note.FlareAt) Flare(p, 1.2f, 1.2f, 1);
            }

            float until = (i + 1) * per;
            while (clock < until)
            {
                yield return null;
                clock += Time.deltaTime;
            }
        }

        liveRoutine = null;
    }

    /// <summary>Draw a line in from its first point to its last.</summary>
    IEnumerator DrawLine(LineRenderer l, float seconds)
    {
        int n = l.positionCount;
        Vector3[] pts = new Vector3[n];
        l.GetPositions(pts);
        l.positionCount = 2;
        l.SetPosition(0, pts[0]);
        l.SetPosition(1, pts[0]);
        float t = 0f;
        while (t < seconds)
        {
            if (l == null) yield break;
            int shown = Mathf.Clamp(Mathf.CeilToInt(t / seconds * (n - 1)) + 1, 2, n);
            if (shown != l.positionCount)
            {
                l.positionCount = shown;
                for (int i = 0; i < shown; i++) l.SetPosition(i, pts[i]);
            }

            yield return null;
            t += Time.deltaTime;
        }

        if (l == null) yield break;
        l.positionCount = n;
        l.SetPositions(pts);
    }

    // ---- AC: the residue torus filling in -----------------------------------------------------------------------------------

    MeshFilter residueCells;
    Color[] residueBaseQuads;
    int[] residueP, residueOffsets;
    int residueCount;

    /// <summary>
    /// The cells a curve's values visit, lit panel row by panel row (exp:residuelive:CODE:SECONDS): green (or the curve's colour)
    /// off the crosses, white on them; the board counts the panels whose cross the curve has touched.
    /// </summary>
    public void ExpResidueOverlayLive(string code, float seconds)
    {
        StartLive(ResidueOverlayLive(code, seconds));
    }

    IEnumerator ResidueOverlayLive(string code, float seconds)
    {
        if (residueCells == null || residueBaseQuads == null) yield break;
        Mesh mesh = residueCells.sharedMesh;
        int count = residueCount;
        int[] P = residueP;
        Color[] cols = new Color[residueBaseQuads.Length * 4];
        for (int q = 0; q < residueBaseQuads.Length; q++) for (int v = 0; v < 4; v++) cols[4 * q + v] = residueBaseQuads[q];
        Color hit = CurveOverlayColor(code);
        string name = code switch { "euler" => "k² + k + 41", "pronic" => "k(k + 1)", "landau" => "k² + 1", _ => code };
        float per = seconds * .92f / count, clock = 0f;
        int done = 0, touching = 0;
        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < count; j++)
            {
                HashSet<(int, int)> seen = new();
                long period = (long)P[i] * P[j];
                for (long k = 0; k < period; k++) { long v = CurveValue(code, k); seen.Add(((int)(v % P[i]), (int)(v % P[j]))); }
                bool touches = false;
                for (int r = 0; r < P[i]; r++)
                    for (int s2 = 0; s2 < P[j]; s2++)
                    {
                        int q = residueOffsets[i * count + j] + r * P[j] + s2;
                        bool on = seen.Contains((r, s2)) && !(i == j && r != s2), cross = r == 0 || s2 == 0;
                        if (on && cross) touches = true;
                        Color c = on ? (cross ? Color.white : hit) : residueBaseQuads[q] * (cross ? 1f : .45f);
                        for (int v = 0; v < 4; v++) cols[4 * q + v] = c;
                    }

                done++;
                if (touches) touching++;
            }

            mesh.colors = cols;
            Board($"{Tint(hit, name)} in the 144 shadows\n" + Small($"panels so far: {done}      on a cancellation cross: {touching}"));
            float until = (i + 1) * per;
            while (clock < until)
            {
                yield return null;
                clock += Time.deltaTime;
            }
        }

        liveRoutine = null;
    }

    // ---- AE: the race drawn from left to right ------------------------------------------------------------------------------

    /// <summary>
    /// Draw the race chart's curves from left to right over <paramref name="seconds"/> (exp:racelive:SECONDS), the measured race
    /// and its cosine-wave rebuild together; a flare and a note when the front passes a marked lead change.
    /// </summary>
    public void ExpRaceLive(float seconds, double[] markers)
    {
        Transform g = FindExpGroup("AE race");
        if (g == null) return;
        StartLive(RaceLive(g, seconds, markers));
    }

    IEnumerator RaceLive(Transform g, float seconds, double[] markers)
    {
        List<(LineRenderer l, Vector3[] pts)> curves = new();
        foreach (LineRenderer l in g.GetComponentsInChildren<LineRenderer>())
        {
            if (!l.name.StartsWith("measured") && !l.name.StartsWith("cosine waves")) continue;
            Vector3[] pts = new Vector3[l.positionCount];
            l.GetPositions(pts);
            curves.Add((l, pts));
            l.positionCount = 2;
            l.SetPosition(0, pts[0]);
            l.SetPosition(1, pts[0]);
        }

        if (curves.Count == 0) yield break;
        // the curves share their x samples: race12 rows from 10^lx0 to 10^8
        float x0 = curves[0].pts[0].x, x1 = curves[0].pts[^1].x;
        double lx0 = raceLx0, lx1 = 8;
        List<double> pending = markers != null ? new List<double>(markers) : new List<double>();
        float clock = 0f, sweep = seconds * .9f, noteUntil = -1f;
        while (clock < sweep)
        {
            float u = Mathf.SmoothStep(0f, 1f, clock / sweep);
            float front = Mathf.Lerp(x0, x1, u);
            foreach (var (l, pts) in curves)
            {
                if (l == null) yield break;
                int shown = 2;
                while (shown < pts.Length && pts[shown - 1].x <= front) shown++;
                if (shown != l.positionCount)
                {
                    l.positionCount = shown;
                    for (int i = 0; i < shown; i++) l.SetPosition(i, pts[i]);
                }
            }

            double lxFront = lx0 + (front - x0) / (x1 - x0) * (lx1 - lx0);
            for (int m = pending.Count - 1; m >= 0; m--)
            {
                if (System.Math.Log10(pending[m]) > lxFront) continue;
                foreach (var (l, pts) in curves)
                {
                    if (!l.name.StartsWith("measured")) continue;
                    int idx = System.Array.FindIndex(pts, p => p.x >= front - .01f);
                    if (idx >= 0) Flare(pts[idx], 1.6f, 1.4f, 2);
                }

                Board($"x = {pending[m].ToString("N0", CultureInfo.InvariantCulture)}\n" + Small(raceMod4 ? "1 mod 4 takes the lead: the curve touches zero" : "the closest call: class 1 almost leads"));
                pending.RemoveAt(m);
                noteUntil = clock + 2.5f;
            }

            if (clock > noteUntil)
                Board($"x = {System.Math.Pow(10, lxFront).ToString("N0", CultureInfo.InvariantCulture)}\n" + Small(raceMod4 ? "3 mod 4 against 1 mod 4" : "5, 7 and 11 mod 12 against 1"));
            yield return null;
            clock += Time.deltaTime;
        }

        foreach (var (l, pts) in curves)
        {
            if (l == null) continue;
            l.positionCount = pts.Length;
            l.SetPositions(pts);
        }

        liveRoutine = null;
    }
}
