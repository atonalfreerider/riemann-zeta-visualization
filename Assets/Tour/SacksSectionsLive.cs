using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Moving pictures for the section modules (mirror lines, the discriminant comb): the lines k² − m² swinging over the
/// squares ray into k² + m²; Fermat's search walking out along the squares until m² − n is a square; a vertical plane
/// sliding across the comb, reading one quadratic at a time with its prime count. They run on the live root
/// (ExperimentLive.cs), so exp:clear stops and removes them.
/// </summary>
public partial class SacksSpiral
{
    static float Ease(float u) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u));

    GameObject sectionLive;

    /// <summary>
    /// A fresh child of the live root for one section animation: the previous section animation's objects go, the vortex
    /// module's pooled objects on the live root stay. exp:clear removes the whole live root as before.
    /// </summary>
    Transform SectionLiveRoot()
    {
        if (sectionLive != null) Destroy(sectionLive);
        sectionLive = new GameObject("section live");
        sectionLive.transform.SetParent(LiveRoot(), false);
        return sectionLive.transform;
    }

    /// <summary>Route points of n = k² + Bk + C (C real) for k from k0 while n ≤ nMax.</summary>
    List<Vector3> QuadraticPoints(int B, double C, float k0, long nMax, float lift)
    {
        List<Vector3> pts = new();
        double k1 = (-B + System.Math.Sqrt((double)B * B + 4.0 * (nMax - C))) / 2.0;
        int steps = Mathf.Max(2, Mathf.CeilToInt((float)(k1 - k0) * RouteSteps));
        for (int i = 0; i <= steps; i++)
        {
            double k = k0 + (k1 - k0) * i / steps, n = k * k + B * k + C;
            if (n < 1.0) continue;
            pts.Add(ExpCurveAt(n, k, lift));
        }

        return pts;
    }

    // ---- the mirror swing -----------------------------------------------------------------------------------------

    /// <summary>The lines k² − m² (m ≤ mMax) swing across the squares ray into k² + m², from fraction <paramref name="from"/> to <paramref name="to"/> of the way.</summary>
    public void ExpMirrorSwingLive(int mMax, float seconds, long nMax, float labelSize, float from = 0f, float to = 1f)
    {
        StartLive(MirrorSwing(mMax, seconds, nMax, labelSize, from, to));
    }

    IEnumerator MirrorSwing(int mMax, float seconds, long nMax, float labelSize, float from, float to)
    {
        Transform g = SectionLiveRoot();
        LineRenderer axis = Factory.NewLine("squares ray", .1f, SquareColor, Factory.GlowIntensity);
        axis.transform.SetParent(g, false);
        List<Vector3> ray = QuadraticPoints(0, 0, 1f, nMax, .14f);
        axis.positionCount = ray.Count;
        axis.SetPositions(ray.ToArray());
        LineRenderer[] lines = new LineRenderer[mMax];
        TextBox[] labels = new TextBox[mMax];
        for (int m = 1; m <= mMax; m++)
        {
            lines[m - 1] = Factory.NewLine("swing " + m, .08f, CancelCurveColor, Factory.GlowIntensity);
            lines[m - 1].transform.SetParent(g, false);
            labels[m - 1] = TextBoxLabel("", labelSize, CancelCurveColor);
            labels[m - 1].transform.SetParent(g, false);
        }

        float time = 0f;
        while (true)
        {
            float u0 = seconds <= 0f ? 1f : Mathf.Clamp01(time / seconds);
            float u = Mathf.Lerp(from, to, u0);
            for (int m = 1; m <= mMax; m++)
            {
                // the m-th line leaves a moment after the one before it
                float um = Ease((u - .12f * (m - 1)) / (1f - .12f * (mMax - 1)));
                double C = -(double)m * m + 2.0 * m * m * um;
                Color col = Color.Lerp(CancelCurveColor, UncancelColor, um);
                List<Vector3> pts = QuadraticPoints(0, C, Mathf.Sqrt(Mathf.Max(1f, (float)(1 - C))), nMax, .12f);
                lines[m - 1].positionCount = pts.Count;
                lines[m - 1].SetPositions(pts.ToArray());
                lines[m - 1].startColor = lines[m - 1].endColor = col;
                string sign = C < 0 ? "−" : "+";
                labels[m - 1].Text = $"k² {sign} {System.Math.Abs(C):0.0}";
                if (um <= 0f) labels[m - 1].Text = $"k² − {m * m}";
                if (um >= 1f) labels[m - 1].Text = $"k² + {m * m}";
                labels[m - 1].Color = col;
                if (pts.Count > 0) labels[m - 1].transform.position = pts[^1] + new Vector3(1.4f + .25f * labelSize, 0f, -.5f);
            }

            if (u0 >= 1f) break;
            time += Time.deltaTime;
            yield return null;
        }

        liveRoutine = null;
    }

    // ---- Fermat's walk ----------------------------------------------------------------------------------------------

    public void ExpFermatWalkLive(long n, float seconds, float labelSize)
    {
        StartLive(FermatWalk(n, seconds, labelSize));
    }

    IEnumerator FermatWalk(long n, float seconds, float labelSize)
    {
        Transform g = SectionLiveRoot();
        long m0 = (long)System.Math.Ceiling(System.Math.Sqrt(n));
        long mEnd = m0;
        while (true)
        {
            long r = mEnd * mEnd - n, j = (long)System.Math.Round(System.Math.Sqrt(r));
            if (j * j == r) break;
            mEnd++;
        }

        long jEnd = (long)System.Math.Round(System.Math.Sqrt(mEnd * mEnd - n));
        long u = mEnd - jEnd, v = mEnd + jEnd, d = v - u;
        Vector3 pn = ExpNumberAt(n, .3f);
        ExpRing(g, "n", pn, 1.4f, Color.white, .07f, Factory.GlowIntensity);
        ExpDot(g, "n dot", pn, .55f, CompositeDotColor);
        TextBox label = TextBoxLabel("", labelSize, Color.white);
        label.transform.SetParent(g, false);
        label.transform.position = pn + new Vector3(0f, 2.2f + .12f * labelSize, -.8f);
        TextBox step = TextBoxLabel("", labelSize * .9f, SquareColor);
        step.transform.SetParent(g, false);
        LineRenderer marker = Factory.NewLine("step ring", .08f, SquareColor, Factory.GlowIntensity);
        marker.transform.SetParent(g, false);
        int steps = (int)(mEnd - m0) + 1;
        float per = Mathf.Max(.35f, seconds * .7f / steps);
        for (long m = m0; m <= mEnd; m++)
        {
            long r = m * m - n;
            long jr = (long)System.Math.Round(System.Math.Sqrt(r));
            bool square = jr * jr == r;
            Vector3 pm = ExpNumberAt(m * m, .35f);
            SetRing(marker, pm, 1.1f, square ? Factory.Palette.Zero : SquareColor);
            ExpDot(g, "visited " + m, pm, .35f, square ? Factory.Palette.Zero : SquareColor);
            step.Text = square ? $"{m}² − {n} = {r} = {jr}²" : $"{m}² − {n} = {r}";
            step.Color = square ? Factory.Palette.Zero : SquareColor;
            step.transform.position = pm + new Vector3(0f, 1.8f + .1f * labelSize, -.8f);
            label.Text = $"{n}:  step {m - m0 + 1}";
            yield return new WaitForSeconds(per);
        }

        // the cancellation curve the search found, drawn out from its birth to n
        List<Vector3> pts = new();
        float uMax = u + 1.5f;
        int samples = Mathf.CeilToInt((uMax - 1f) * RouteSteps);
        LineRenderer curve = Factory.NewLine("found curve", .1f, CurveColor((int)d), Factory.GlowIntensity);
        curve.transform.SetParent(g, false);
        for (int i = 0; i <= samples; i++)
        {
            double uu = 1.0 + (uMax - 1.0) * i / samples;
            pts.Add(ExpCurveAt(uu * (uu + d), uu, .15f));
            if (i % 6 == 0 || i == samples)
            {
                curve.positionCount = pts.Count;
                curve.SetPositions(pts.ToArray());
                yield return null;
            }
        }

        label.Text = $"{n} = {u} × {v}, on the cancellation curve C<sub>{d}</sub>   ({steps - 1} step{(steps - 1 == 1 ? "" : "s")} out from √{n})";
        label.Color = CurveColor((int)d);
        liveRoutine = null;
    }

    // ---- a plane sliding across the comb ------------------------------------------------------------------------------

    static readonly Dictionary<(int, long), int> combCounts = new();

    /// <summary>Primes among k² + Bk + C for 0 ≤ k ≤ kMax (cached).</summary>
    public static int CombCount(int B, long C, int kMax)
    {
        if (combCounts.TryGetValue((B * 100000 + kMax, C), out int c)) return c;
        ExpEnsureSieve((long)kMax * kMax + (long)B * kMax + System.Math.Abs(C) + 2);
        int cnt = 0;
        for (long k = 0; k <= kMax; k++) if (ExpPrime(k * k + B * k + C)) cnt++;
        combCounts[(B * 100000 + kMax, C)] = cnt;
        return cnt;
    }

    public void ExpCombSlideLive(int B, float cFrom, float cTo, float seconds, int kMax, float labelSize)
    {
        StartLive(CombSlide(B, cFrom, cTo, seconds, kMax, labelSize));
    }

    IEnumerator CombSlide(int B, float cFrom, float cTo, float seconds, int kMax, float labelSize)
    {
        Transform g = SectionLiveRoot();
        float L = Extent + 4f;
        bool squares = B == 0;
        // the plane's rim, its parabola, and the quadratic of the nearest whole C
        LineRenderer rim = Factory.NewLine("sliding plane", .07f, Color.white, Factory.GlowIntensity);
        rim.transform.SetParent(g, false);
        rim.loop = true;
        rim.positionCount = 4;
        LineRenderer section = Factory.NewLine("its parabola", .09f, Color.white, Factory.GlowIntensity);
        section.transform.SetParent(g, false);
        LineRenderer curve = Factory.NewLine("its quadratic", .08f, Color.white, Factory.GlowIntensity);
        curve.transform.SetParent(g, false);
        TextBox label = TextBoxLabel("", labelSize, Color.white);
        label.transform.SetParent(g, false);
        Transform dotsRoot = null;
        long shownC = long.MinValue;
        float time = 0f;
        while (true)
        {
            float u = seconds <= 0f ? 1f : Mathf.Clamp01(time / seconds);
            float cc = Mathf.Lerp(cFrom, cTo, Ease(u));
            float y = squares ? Mathf.PI * R * cc : -Mathf.PI * R * (cc - .25f);
            float sx = squares ? 1f : -1f;
            float zLow = -20f, zHigh = 75f;
            rim.SetPositions(new[]
            {
                origin + new Vector3(0f, y, zLow), origin + new Vector3(sx * L, y, zLow),
                origin + new Vector3(sx * L, y, zHigh), origin + new Vector3(0f, y, zHigh)
            });
            List<Vector3> sec = new();
            for (int i = 0; i <= 200; i++)
            {
                float x = sx * L * i / 200f;
                if (x * x + y * y > L * L) continue;
                sec.Add(ExpAtXY(x, y, .2f));
            }

            section.positionCount = sec.Count;
            section.SetPositions(sec.ToArray());
            long C = (long)System.Math.Round(cc);
            if (C != shownC)
            {
                shownC = C;
                Color col = KindColor(1, B, C);
                rim.startColor = rim.endColor = section.startColor = section.endColor = col;
                List<Vector3> pts = QuadraticPoints(B, C, 0f, Count, .14f);
                curve.positionCount = pts.Count;
                curve.SetPositions(pts.ToArray());
                curve.startColor = curve.endColor = col;
                if (dotsRoot != null) Destroy(dotsRoot.gameObject);
                dotsRoot = new GameObject("values").transform;
                dotsRoot.SetParent(g, false);
                List<Vector3> dp = new(); List<Color> dc = new(); List<float> ds = new();
                for (long k = 0; ; k++)
                {
                    long n = k * k + B * k + C;
                    if (n > Count) break;
                    if (n < 2) continue;
                    bool p = ExpPrime(n);
                    dp.Add(ExpNumberAt(n, .2f)); dc.Add(p ? Factory.Palette.Zero : CompositeDotColor); ds.Add(p ? .4f : .3f);
                }

                if (dp.Count > 0) ExpPointCloud(dotsRoot, "values", dp, dc, ds, Factory.GlowIntensity);
                long D = Discriminant(1, B, C);
                bool factors = IsSquareLong(D);
                int cnt = CombCount(B, C, kMax);
                label.Text = $"{Poly(1, B, C)}     Δ = {D}{(factors ? "  (a square: it factors)" : "")}\nprimes for k ≤ {kMax:N0}:  {cnt:N0}";
                label.Color = col;
            }

            label.transform.position = origin + new Vector3(sx * (L * .55f), y + .16f * labelSize + 1f, 0f) + Vector3.back * 8f;
            if (u >= 1f) break;
            time += Time.deltaTime;
            yield return null;
        }

        liveRoutine = null;
    }
}
