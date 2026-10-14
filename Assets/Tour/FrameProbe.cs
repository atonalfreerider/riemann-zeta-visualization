using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

/// <summary>
/// Wall-clock timing of named pieces of per-frame work, for finding frame drops without the Profiler window (works in
/// batch mode). Off unless <see cref="Enabled"/>; when off, <see cref="Measure"/> costs one branch.
///   using (FrameProbe.Measure("trail upload")) { ... }
/// </summary>
public static class FrameProbe
{
    public static bool Enabled;

    static readonly Dictionary<string, (long ticks, long max, int calls)> totals = new();

    public readonly struct Scope : IDisposable
    {
        readonly string name;
        readonly long start;

        public Scope(string name)
        {
            this.name = name;
            start = Stopwatch.GetTimestamp();
        }

        public void Dispose()
        {
            if (name == null) return;
            long elapsed = Stopwatch.GetTimestamp() - start;
            totals.TryGetValue(name, out (long ticks, long max, int calls) t);
            totals[name] = (t.ticks + elapsed, Math.Max(t.max, elapsed), t.calls + 1);
        }
    }

    public static Scope Measure(string name) => Enabled ? new Scope(name) : default;

    /// <summary>Per-frame averages (and the worst single call) since the last report, then reset.</summary>
    public static string Report(int frames)
    {
        double ms = 1000d / Stopwatch.Frequency;
        StringBuilder sb = new();
        foreach (KeyValuePair<string, (long ticks, long max, int calls)> e in totals.OrderByDescending(e => e.Value.ticks))
        {
            sb.Append($"   {e.Key} {e.Value.ticks * ms / Math.Max(1, frames):0.00} ms/frame (worst {e.Value.max * ms:0.0} ms, {e.Value.calls} calls)\n");
        }

        totals.Clear();
        return sb.ToString();
    }
}
