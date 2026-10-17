using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The tower of E8's shells as a picture of divisibility (the long-form segment xz5): shell n holds 240 σ₃(n) points, one
/// cube d³ for each divisor d of n, so every shell d sends its cube to the shells of its multiples.
///   ExpE8Links(maxN, gap)  an arc beside the tower from floor d to floor n for every proper divisor d of n ≤ maxN, in d's
///       colour, each item "link d-n" of group "E8 links" (lines "arc d-n", "head d-n"), hidden until shown.
/// </summary>
public partial class SacksSpiral
{
    static readonly Color[] LinkColors = { Color.white, new(1f, .62f, .2f), new(.78f, .55f, 1f), new(.35f, .85f, 1f), new(.45f, 1f, .55f), new(1f, .45f, .8f) };

    public void ExpE8Links(int maxN, float gap, float labelSize)
    {
        if (!e8WheelReady) return;
        Transform g = ExpGroup("E8 links").transform;
        Transform items = RevealItems(g);
        float x0 = -e8HourRadius * 1.25f;
        Vector3 Floor(int n) => e8WheelCenter + new Vector3(x0, 0f, gap * (n - 1));
        for (int n = 2; n <= maxN; n++)
        {
            List<long> divs = Divisors(n);
            List<string> parts = new();
            foreach (long dl in divs)
            {
                int d = (int)dl;
                if (d == n) continue;
                parts.Add(d == 1 ? "1" : $"{d}³");
                GameObject it = new($"link {d}-{n}");
                it.transform.SetParent(items, false);
                Vector3 a = Floor(d), b = Floor(n);
                float bow = 4f + 2.6f * (n - d) + 1.5f * d;
                Vector3 ctrl = (a + b) * .5f + Vector3.left * bow;
                Vector3[] pts = new Vector3[25];
                for (int i = 0; i <= 24; i++)
                {
                    float t = i / 24f;
                    pts[i] = (1 - t) * (1 - t) * a + 2 * (1 - t) * t * ctrl + t * t * b;
                }

                Color col = LinkColors[Mathf.Clamp(d - 1, 0, LinkColors.Length - 1)];
                LineRenderer l = ExpLine(it.transform, $"arc {d}-{n}", .32f, col, Factory.GlowIntensity, pts);
                OnTop(l, 17);
                Vector3 tip = pts[24], dir = (pts[24] - pts[21]).normalized, side = Vector3.Cross(dir, Vector3.up).normalized;
                ExpLine(it.transform, $"head {d}-{n}", .32f, col, Factory.GlowIntensity, new[] { tip - dir * 1.6f + side * .9f, tip, tip - dir * 1.6f - side * .9f });
                TextBox t1 = ExpLabelWide(it.transform, d == 1 ? "1³" : $"{d}³ = {d * d * d}", ctrl + Vector3.left * 1.5f, labelSize * .1f, col, TextAlignmentOptions.Right);
                Outline(t1.TextField);
                RevealNote note = it.AddComponent<RevealNote>();
                note.Text = $"{d} divides {n}:  shell {d} sends {d}³ = {d * d * d} to shell {n}";
            }
        }

        foreach (Transform t in items) t.gameObject.SetActive(false);
    }
}
