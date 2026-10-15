using System.Collections;
using UnityEngine;

/// <summary>
/// White bloom for the experiment modules: whatever is moving or has just happened glows white, so the eye goes to it.
/// A halo is a wider white copy of a line that follows it every frame and shines while its strength is up (the
/// animation sets the strength, or lets it decay); a flare is a white pulse and an expanding ring at a point or a
/// number. Both are drawn far above the bloom threshold, and both live on the experiments' live root, so exp:clear
/// removes them with the animation they belong to.
/// </summary>
public partial class SacksSpiral
{
    /// <summary>HDR intensity of the attention glow: well over the bloom threshold, so it blooms white.</summary>
    public const float AttentionIntensity = 2.6f;

    /// <summary>A white halo that follows <paramref name="source"/>; set its Strength, or its Decay to let it fade.</summary>
    public AttentionHalo Halo(LineRenderer source, float widthScale = 2.6f, float strength = 1f)
    {
        return AttentionHalo.Attach(source, widthScale, strength);
    }

    /// <summary>A white flare at a number on the current surface.</summary>
    public void FlareNumber(double n, float radius = 1.6f, float seconds = 1.6f, int pulses = 2)
    {
        Flare(ExpNumberAt(n, .5f), radius, seconds, pulses);
    }

    /// <summary>A white flare: the point swells and fades while a ring spreads out from it, <paramref name="pulses"/> times.</summary>
    public void Flare(Vector3 at, float radius = 1.6f, float seconds = 1.6f, int pulses = 2)
    {
        StartCoroutine(FlareRoutine(at, radius, seconds, pulses));
    }

    IEnumerator FlareRoutine(Vector3 at, float radius, float seconds, int pulses)
    {
        Transform g = LiveRoot();
        GameObject core = Factory.NewSphere("flare", 1f, Color.white, AttentionIntensity);
        core.transform.SetParent(g, false);
        core.transform.position = at;
        LineRenderer ring = Factory.NewLine("flare ring", .12f, Color.white, AttentionIntensity);
        ring.transform.SetParent(g, false);
        ring.loop = true;
        ring.positionCount = 32;
        Vector3[] pts = new Vector3[32];
        float per = seconds / Mathf.Max(1, pulses);
        for (int p = 0; p < pulses; p++)
        {
            float time = 0f;
            while (time < per)
            {
                if (core == null || ring == null) yield break; // the stage was cleared under us
                float u = time / per;
                // never smaller than ~3% of the camera's distance, so a flare reads in a wide shot as well as a close one
                Camera cam = Camera.main;
                float size = cam != null ? Mathf.Max(radius, Vector3.Distance(cam.transform.position, at) * .028f) : radius;
                float swell = Mathf.Sin(Mathf.PI * Mathf.Min(1f, u * 1.6f));
                core.transform.localScale = Vector3.one * (size * .55f * Mathf.Max(0f, swell));
                float r = size * Mathf.Lerp(.4f, 2.6f, Mathf.SmoothStep(0f, 1f, u));
                for (int i = 0; i < 32; i++)
                {
                    float a = i / 32f * Mathf.PI * 2f;
                    pts[i] = at + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * r;
                }

                ring.SetPositions(pts);
                float fade = 1f - u;
                ring.startColor = ring.endColor = new Color(fade, fade, fade, fade);
                ring.widthMultiplier = Mathf.Lerp(1.4f, .5f, u) * Mathf.Max(.6f, size * .45f);
                time += Time.deltaTime;
                yield return null;
            }
        }

        if (core != null) Destroy(core);
        if (ring != null) Destroy(ring.gameObject);
    }
}

/// <summary>A wider, white, strongly glowing copy of a line that follows it every frame (see SacksSpiral.Halo).</summary>
public class AttentionHalo : MonoBehaviour
{
    /// <summary>0 … 1: how brightly the halo shines.</summary>
    public float Strength = 1f;
    /// <summary>Strength lost per second (0 holds it where the animation puts it).</summary>
    public float Decay;

    LineRenderer source, line;
    float widthScale;
    Vector3[] buffer = new Vector3[0];

    public static AttentionHalo Attach(LineRenderer source, float widthScale, float strength)
    {
        LineRenderer l = Factory.NewLine("attention halo", .1f, Color.white, SacksSpiral.AttentionIntensity);
        l.transform.SetParent(source.transform, false);
        l.numCornerVertices = 2;
        AttentionHalo h = l.gameObject.AddComponent<AttentionHalo>();
        h.source = source;
        h.line = l;
        h.widthScale = widthScale;
        h.Strength = strength;
        h.Sync();
        return h;
    }

    /// <summary>Shine at full strength again, then fade over <paramref name="seconds"/>.</summary>
    public void Pulse(float seconds = 1.2f)
    {
        Strength = 1f;
        Decay = 1f / Mathf.Max(.05f, seconds);
    }

    void LateUpdate() => Sync();

    void Sync()
    {
        if (source == null)
        {
            Destroy(gameObject);
            return;
        }

        if (Decay > 0f) Strength = Mathf.Max(0f, Strength - Decay * Time.deltaTime);
        bool on = Strength > .01f && source.enabled && source.positionCount > 1;
        line.enabled = on;
        if (!on) return;
        int n = source.positionCount;
        if (buffer.Length < n) buffer = new Vector3[n];
        source.GetPositions(buffer);
        line.useWorldSpace = source.useWorldSpace;
        line.loop = source.loop;
        line.positionCount = n;
        line.SetPositions(buffer);
        line.widthCurve = source.widthCurve;
        line.widthMultiplier = source.widthMultiplier * widthScale;
        float s = Mathf.Clamp01(Strength);
        line.startColor = line.endColor = new Color(s, s, s, s);
    }
}
