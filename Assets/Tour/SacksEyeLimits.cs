using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Why the eyes of 10 000 zeros thin out beyond a faint circle (the long-form segment xz2). At a zero, the eye of vortex k
/// is as far from the centre as the path's k-th vertex, |1 + 2^(−s) + … + k^(−s)|. For vortex 2 that is one, plus an
/// arrow 1/√2 long pointing wherever γ·log 2 sends it: the eye stays between 1 − 1/√2 = 0.293 and 1 + 1/√2 = 1.707, and
/// lingers at the two extremes (where the arrow turns along the radius), so both circles glow; beyond 1.707 only vortices
/// 3 and up reach. Measured over the first 10 000 zeros the density of eyes drops 2.7-fold across r = 1.707.
///   ExpEyeLimits(seconds): in the path's plane, the unit arrow and the swinging arrow of 2 (its tip tracing a circle),
///   then the circles r = 1.707 and 0.293 round the centre, labelled (group "PW limits").
/// </summary>
public partial class SacksSpiral
{
    public void ExpEyeLimits(float seconds, float labelSize) => StartCoroutine(EyeLimitsRoutine(seconds, labelSize));

    IEnumerator EyeLimitsRoutine(float seconds, float labelSize)
    {
        if (pw == null) yield break;
        Transform g = ExpGroup("PW limits").transform;
        float h = PathHeight(Tw("pw.t", 1f));
        Vector3 O(float x, float y) => PathOrigin + new Vector3(x, y, 0f) * PwScale + Vector3.back * (h + .3f);
        const float r2 = .70710678f;
        Color c2 = VortexColor(2, 1f), gold = PrimeGold;
        LineRenderer one = ExpLine(g, "arrow 1", .45f, Color.white, AttentionIntensity, new[] { O(0, 0), O(1, 0) });
        OnTop(one, 22);
        TextBox t1 = ExpLabelWide(g, "1", O(.5f, -.12f), labelSize * .09f, Color.white);
        Outline(t1.TextField);
        LineRenderer two = ExpLine(g, "arrow 2", .4f, c2, AttentionIntensity, new[] { O(1, 0), O(1 + r2, 0) });
        OnTop(two, 22);
        TextBox t2 = ExpLabelWide(g, "1/√2", O(1 + r2 * .5f, .12f), labelSize * .08f, c2);
        Outline(t2.TextField);
        LineRenderer trace = ExpLine(g, "trace", .16f, c2 * .8f, Factory.SoftGlowIntensity, new[] { O(1 + r2, 0), O(1 + r2, 0) });
        LineRenderer reach = ExpLine(g, "reach", .22f, new Color(1f, 1f, 1f, .6f), Factory.SoftGlowIntensity, new[] { O(0, 0), O(1 + r2, 0) });
        // the arrow of 2 swings once round: its tip traces a circle about 1, and the eye's distance runs from 0.29 to 1.71
        float swing = seconds * .45f, time = 0f;
        List<Vector3> tr = new();
        while (time < swing)
        {
            if (two == null) yield break;
            float a = Mathf.PI * 2f * time / swing;
            Vector3 tip = O(1 + r2 * Mathf.Cos(a), r2 * Mathf.Sin(a));
            two.SetPositions(new[] { O(1, 0), tip });
            t2.transform.position = O(1 + r2 * .5f * Mathf.Cos(a), r2 * .5f * Mathf.Sin(a) + .12f);
            reach.SetPositions(new[] { O(0, 0), tip });
            tr.Add(tip);
            trace.positionCount = tr.Count;
            trace.SetPositions(tr.ToArray());
            time += Time.deltaTime;
            yield return null;
        }

        // the two extremes, then the circles they sweep round the centre
        // (in a 9:16 frame the outer label goes up and nearer the middle: at −35° it would run off the right edge)
        foreach (var (r, name, ang) in new[] { (1f + r2, "1 + 1/√2 = 1.71", Tall ? 70f : -35f), (1f - r2, "1 − 1/√2 = 0.29", -60f) })
        {
            Vector3[] ring = new Vector3[121];
            for (int i = 0; i <= 120; i++)
            {
                float a = i / 120f * Mathf.PI * 2f;
                ring[i] = O(r * Mathf.Cos(a), r * Mathf.Sin(a));
            }

            LineRenderer l = ExpLine(g, $"circle {name}", .3f, Color.white, AttentionIntensity, new[] { ring[0], ring[0] });
            OnTop(l, 21);
            float dt = 0f, draw = seconds * .18f;
            while (dt < draw)
            {
                if (l == null) yield break;
                int n = Mathf.Clamp(Mathf.CeilToInt(121 * dt / draw), 2, 121);
                l.positionCount = n;
                for (int i = 0; i < n; i++) l.SetPosition(i, ring[i]);
                dt += Time.deltaTime;
                yield return null;
            }

            if (l == null) yield break;
            l.positionCount = 121;
            l.SetPositions(ring);
            StartCoroutine(SettleRoutine(l, gold, .8f));
            float ar = ang * Mathf.Deg2Rad;
            TextBox lab = ExpLabelWide(g, name, O((r + .2f) * Mathf.Cos(ar), (r + .2f) * Mathf.Sin(ar)), labelSize * .09f, gold,
                Mathf.Cos(ar) >= 0f ? TextAlignmentOptions.Left : TextAlignmentOptions.Right);
            Outline(lab.TextField);
        }
    }
}
