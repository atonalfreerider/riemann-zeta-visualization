using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Animated parameters for the continuous scenes of the long-form segments: a scene reads its parameters (how far the
/// number line is rolled up, how far the plane is bent, how many waves are added, …) every frame, and an action eases a
/// parameter to a new value over some seconds, so one picture turns into the next instead of being cleared and redrawn.
/// Also the mute: every renderer of the named groups fades to a faint grey (the GlowLine shader's _Mute), and their labels
/// to a faint grey, so that the one thing being discussed can be lit on top.
/// </summary>
public partial class SacksSpiral
{
    class Tween
    {
        public float From, To, Start, Duration;
        public int Ease; // 0 linear, 1 smooth, 2 geometric (for counts: equal ratios in equal times)
    }

    readonly Dictionary<string, Tween> tweens = new();

    /// <summary>The current value of parameter <paramref name="key"/> (its default if it was never set).</summary>
    // each parameter's default as its scene reads it, so a first tween starts from there (not from its own target)
    readonly Dictionary<string, float> twDefaults = new();

    public float Tw(string key, float dflt = 0f)
    {
        if (!tweens.TryGetValue(key, out Tween t))
        {
            twDefaults[key] = dflt;
            return dflt;
        }

        if (t.Duration <= 0f) return t.To;
        float u = Mathf.Clamp01((Time.time - t.Start) / t.Duration);
        switch (t.Ease)
        {
            case 0: return Mathf.Lerp(t.From, t.To, u);
            case 2:
                if (t.From > 0f && t.To > 0f) return t.From * Mathf.Pow(t.To / t.From, u);
                return Mathf.Lerp(t.From, t.To, u);
            default: return Mathf.Lerp(t.From, t.To, Mathf.SmoothStep(0f, 1f, u));
        }
    }

    /// <summary>True while parameter <paramref name="key"/> is still moving.</summary>
    public bool TwMoving(string key) => tweens.TryGetValue(key, out Tween t) && t.Duration > 0f && Time.time - t.Start < t.Duration;

    /// <summary>Ease parameter <paramref name="key"/> from where it is now to <paramref name="to"/> in <paramref name="seconds"/>.</summary>
    public void TwTo(string key, float to, float seconds, int ease = 1)
    {
        float from = tweens.ContainsKey(key) ? Tw(key, to) : twDefaults.TryGetValue(key, out float d) ? d : to;
        tweens[key] = new Tween { From = from, To = to, Start = Time.time, Duration = Mathf.Max(0f, seconds), Ease = ease };
    }

    /// <summary>Set parameter <paramref name="key"/> at once.</summary>
    public void TwSet(string key, float value) => tweens[key] = new Tween { From = value, To = value, Start = Time.time, Duration = 0f };

    void ClearTweens(string prefix)
    {
        List<string> keys = new();
        foreach (string k in tweens.Keys) if (k.StartsWith(prefix)) keys.Add(k);
        foreach (string k in keys) tweens.Remove(k);
    }

    // ---- the mute ---------------------------------------------------------------------------------------------

    readonly Dictionary<TMP_Text, (Color color, float alpha)> mutedText = new();

    /// <summary>
    /// Fade every renderer of the groups whose names start with one of <paramref name="groups"/> to grey
    /// (<paramref name="on"/>) or back to its colours, over <paramref name="seconds"/>; their labels go faint grey too.
    /// </summary>
    public void ExpMute(string[] groups, bool on, float seconds = .8f, float depth = 1f)
    {
        List<Renderer> rs = new();
        List<TMP_Text> ts = new();
        foreach (Transform g in ExpRoot())
        {
            if (!GroupMatches(g.name, groups)) continue;
            foreach (Renderer r in g.GetComponentsInChildren<Renderer>(true))
                if (r.GetComponent<TMP_Text>() == null) rs.Add(r);
            foreach (TMP_Text t in g.GetComponentsInChildren<TMP_Text>(true)) ts.Add(t);
        }

        StartCoroutine(MuteRoutine(rs, ts, on, seconds, depth));
    }

    IEnumerator MuteRoutine(List<Renderer> rs, List<TMP_Text> ts, bool on, float seconds, float depth)
    {
        float[] from = new float[rs.Count];
        for (int i = 0; i < rs.Count; i++) from[i] = rs[i] != null ? Factory.GetMute(rs[i]) : 0f;
        foreach (TMP_Text t in ts)
            if (t != null && on && !mutedText.ContainsKey(t)) mutedText[t] = (t.color, t.alpha);
        Color[] textFrom = new Color[ts.Count];
        for (int j = 0; j < ts.Count; j++) textFrom[j] = ts[j] != null ? ts[j].color : Color.clear;
        float fade = Mathf.Lerp(1f, .2f, Mathf.Clamp01(depth - 1f));
        float time = 0f;
        while (true)
        {
            float u = seconds <= 0f ? 1f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / seconds));
            for (int i = 0; i < rs.Count; i++)
                if (rs[i] != null) Factory.SetMute(rs[i], Mathf.Lerp(from[i], on ? depth : 0f, u));
            for (int j = 0; j < ts.Count; j++)
            {
                TMP_Text t = ts[j];
                if (t == null || !mutedText.TryGetValue(t, out var orig)) continue;
                Color grey = new(.36f, .38f, .46f, orig.alpha * .25f * fade);
                t.color = Color.Lerp(textFrom[j], on ? grey : orig.color, u);
            }

            if (u >= 1f) break;
            time += Time.deltaTime;
            yield return null;
        }

        if (!on)
            foreach (TMP_Text t in ts)
                if (t != null) mutedText.Remove(t);
    }
}
