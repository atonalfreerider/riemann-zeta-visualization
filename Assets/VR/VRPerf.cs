using System.Text;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Frame-time statistics for the headset, written to the Android log (adb logcat -s Unity) at every narration line and
/// every few seconds, so a chapter's cost can be judged from outside the headset.
///
///   [VRPERF] turn_03  frames 421  avg 13.8 ms (72 fps)  worst 29.1 ms  >14ms 3%  gc 0  scale 22.4  passthrough True
/// </summary>
public class VRPerf : MonoBehaviour
{
    public const string Tag = "[VRPERF]";

    string label = "idle";
    int frames;
    int slow;
    float sum;
    float worst;
    int gc0;
    float windowStart;
    FrameTiming[] timings = new FrameTiming[1];
    double gpuSum, cpuSum;
    int gpuSamples;

    public static VRPerf Create(VRRig rig)
    {
        VRPerf p = rig.gameObject.AddComponent<VRPerf>();
        p.windowStart = Time.unscaledTime;
        p.gc0 = System.GC.CollectionCount(0);
        return p;
    }

    /// <summary>The current stretch of play (a narration line, or a view) gets its own statistics.</summary>
    public void Mark(string newLabel)
    {
        Flush();
        label = newLabel;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        frames++;
        sum += dt;
        if (dt > worst) worst = dt;
        if (dt > 0.0145f) slow++;

        FrameTimingManager.CaptureFrameTimings();
        if (FrameTimingManager.GetLatestTimings(1, timings) > 0)
        {
            if (timings[0].gpuFrameTime > 0) { gpuSum += timings[0].gpuFrameTime; gpuSamples++; }
            if (timings[0].cpuFrameTime > 0) cpuSum += timings[0].cpuFrameTime;
        }

        if (Time.unscaledTime - windowStart > 8f) Flush();
    }

    void Flush()
    {
        if (frames < 5)
        {
            Reset();
            return;
        }

        float avg = sum / frames * 1000f;
        StringBuilder sb = new();
        sb.Append(Tag).Append(' ').Append(label.PadRight(10));
        sb.Append(" frames ").Append(frames).Append("  avg ").Append(avg.ToString("0.0")).Append(" ms (").Append((1000f / avg).ToString("0"))
          .Append(" fps)  worst ").Append((worst * 1000f).ToString("0.0")).Append(" ms  >14.5ms ").Append((100f * slow / frames).ToString("0")).Append('%');
        if (gpuSamples > 0) sb.Append("  gpu ").Append((gpuSum / gpuSamples).ToString("0.0")).Append(" ms  cpu ").Append((cpuSum / gpuSamples).ToString("0.0")).Append(" ms");
        sb.Append("  gc ").Append(System.GC.CollectionCount(0) - gc0);
        if (VRMode.Rig != null) sb.Append("  scale ").Append(VRMode.Rig.Scale.ToString("0.0")).Append("  passthrough ").Append(VRMode.Rig.Passthrough);
        sb.Append("  eye ").Append(XRSettings.eyeTextureWidth).Append('x').Append(XRSettings.eyeTextureHeight);
        Debug.Log(sb.ToString());
        Reset();
    }

    void Reset()
    {
        frames = slow = gpuSamples = 0;
        sum = worst = 0f;
        gpuSum = cpuSum = 0;
        gc0 = System.GC.CollectionCount(0);
        windowStart = Time.unscaledTime;
    }
}
