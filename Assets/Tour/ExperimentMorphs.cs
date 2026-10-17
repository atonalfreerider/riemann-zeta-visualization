using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Transforms between charts for the long-form segments, so one chart turns into the next instead of the camera moving
/// between separate charts:
///   ExpGroupMove     move and scale a whole chart (a group) about its centre, smoothly (its lines are switched to local
///                    space first, so they follow)
///   ExpStripsMorph   section LC's four strips of zeros fold into section LE's chart of mixtures: each zero on the critical
///                    line flies from its strip to its place on the chart, at height λ = 0 (the first family), 2 and −1
///                    (the class group's L-functions) or up to the poles (the second family), and the curve λ = −Λ₁/Λ₂
///                    then draws through them; the zeros off the line fade
/// </summary>
public partial class SacksSpiral
{
    readonly Dictionary<Transform, (Vector3 pos, Vector3 scale)> groupHome = new();

    /// <summary>
    /// Move group(s) starting with <paramref name="name"/> by <paramref name="delta"/> and scale them by <paramref name="scale"/>
    /// about their centre (relative to where they were first drawn), in <paramref name="seconds"/>.
    /// </summary>
    public void ExpGroupMove(string name, Vector3 delta, float scale, float seconds)
    {
        // "GROUP/PART" moves one part of a group (a child at the group's origin, its lines in world space until moved)
        string part = null;
        int slash = name.IndexOf('/');
        if (slash > 0)
        {
            part = name.Substring(slash + 1);
            name = name.Substring(0, slash);
        }

        List<Transform> targets = new();
        foreach (Transform grp in ExpRoot())
        {
            if (!GroupMatches(grp.name, new[] { name })) continue;
            if (part == null) targets.Add(grp);
            else if (grp.Find(part) is Transform child) targets.Add(child);
        }

        foreach (Transform g in targets)
        {
            if (!groupHome.ContainsKey(g))
            {
                foreach (LineRenderer l in g.GetComponentsInChildren<LineRenderer>(true)) l.useWorldSpace = false;
                Bounds b = new(g.position, Vector3.zero);
                bool any = false;
                foreach (Renderer r in g.GetComponentsInChildren<Renderer>(true))
                {
                    if (r is LineRenderer lr)
                    {
                        // local-space lines: their bounds from their points (the group is still at identity)
                        for (int i = 0; i < lr.positionCount; i += Mathf.Max(1, lr.positionCount / 16))
                        {
                            Vector3 p = lr.GetPosition(i);
                            if (!any) { b = new Bounds(p, Vector3.zero); any = true; }
                            else b.Encapsulate(p);
                        }

                        continue;
                    }

                    // labels (and their fallback-font sub-meshes, children of the text) are not part of the shape
                    if (r.GetComponentInParent<TMP_Text>() != null) continue;
                    if (!any) { b = r.bounds; any = true; }
                    else b.Encapsulate(r.bounds);
                }

                groupPivot[g] = b.center;
                groupHome[g] = (g.position, g.localScale);
            }

            StartCoroutine(GroupMoveRoutine(g, delta, scale, seconds));
        }
    }

    readonly Dictionary<Transform, Vector3> groupPivot = new();

    IEnumerator GroupMoveRoutine(Transform g, Vector3 delta, float scale, float seconds)
    {
        Vector3 pivot = groupPivot[g];
        Vector3 startPos = g.position, startScale = g.localScale;
        // the target: scale about the pivot, then move
        Vector3 endScale = groupHome[g].scale * scale, endPos = groupHome[g].pos + pivot * (1f - scale) + delta;
        float time = 0f;
        while (true)
        {
            if (g == null) yield break;
            float u = seconds <= 0f ? 1f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / seconds));
            g.position = Vector3.Lerp(startPos, endPos, u);
            g.localScale = Vector3.Lerp(startScale, endScale, u);
            if (u >= 1f) yield break;
            time += Time.deltaTime;
            yield return null;
        }
    }

    // ---- LC's strips fold into LE's chart ----------------------------------------------------------------------

    /// <summary>
    /// The four strips (drawn by ExpClassZeros at <paramref name="stripsAt"/>) fold into the chart of mixtures (drawn by
    /// ExpClassMixtures at <paramref name="chartAt"/>, kept muted until the zeros land).
    /// </summary>
    public void ExpStripsMorph(Vector3 stripsAt, Vector3 chartAt, float seconds)
    {
        StartCoroutine(StripsMorphRoutine(stripsAt, chartAt, seconds));
    }

    IEnumerator StripsMorphRoutine(Vector3 so, Vector3 co, float seconds)
    {
        const float w = 50f, sy = 1.9f, tMax = 50f;
        // where the strips put the zeros on the line, and where the chart puts them
        double[][] on = { ClassGroupData.OnM23e, ClassGroupData.OnM23c, ClassGroupData.OnM23L0, ClassGroupData.OnM23L1 };
        float[] lam = { 0f, 4.25f, 2f, -1f };
        Color[] cols = { FormColors[0], FormColors[1], PrimeGold, PrimeGold };
        List<Vector3> from = new(), to = new();
        List<Color> pc = new();
        List<float> ps = new();
        List<float> delay = new();
        for (int k = 0; k < 4; k++)
        {
            Vector3 c = so + new Vector3((k - 1.5f) * (w + 14f), -tMax * sy * .5f - 6f, 0f);
            foreach (double t in on[k])
            {
                from.Add(c + new Vector3(0f, (float)t * sy, -.3f));
                to.Add(co + new Vector3(-140f + (float)(t / 50d) * 190f, lam[k] * 12f, -.5f));
                pc.Add(cols[k]);
                ps.Add(1f);
                delay.Add(k * .12f + (float)(t / 50d) * .1f);
            }
        }

        Transform g = ExpGroup("LE landed").transform;
        // the zeros leave their strips (the strips' own dots go dark) and fly
        ExpSetChildren("LC zeros", "zeros", false);
        MeshRenderer cloud = null;
        float time = 0f;
        while (true)
        {
            if (g == null) yield break;
            float u = Mathf.Clamp01(time / seconds);
            List<Vector3> pts = new(from.Count);
            for (int i = 0; i < from.Count; i++)
            {
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - delay[i]) / .55f));
                Vector3 mid = Vector3.Lerp(from[i], to[i], a) + Vector3.back * (Mathf.Sin(a * Mathf.PI) * 6f);
                pts.Add(mid);
            }

            if (cloud != null) Destroy(cloud.gameObject);
            cloud = ExpPointCloud(g, "zeros", pts, pc, ps, Factory.GlowIntensity);
            if (u >= 1f) break;
            time += Time.deltaTime;
            yield return null;
        }

        // the second family's zeros are the curve's poles: dashed verticals there
        List<(Vector3, Vector3)> poles = new();
        foreach (double t in ClassGroupData.OnM23c)
        {
            float x = -140f + (float)(t / 50d) * 190f;
            for (float y = -3f; y < 4f; y += .5f) poles.Add((co + new Vector3(x, y * 12f, -.2f), co + new Vector3(x, (y + .25f) * 12f, -.2f)));
        }

        Segments(g, "poles", poles, .1f, FormColors[1] * .6f, Factory.NoGlow);
    }
}
