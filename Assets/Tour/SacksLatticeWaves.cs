using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Sections P and Q (docs/experiments/README.md).
///
/// P. Two lattice-point problems as waves on the spiral. The divisor problem counts lattice points under the hyperbola
///    uv = x (one per cancellation-curve point); its wobble is Voronoi's sum of cos(4π√(nx) − π/4), two waves per turn of
///    the Sacks spiral, and its first wave is the plain saddle. The Gauss circle problem counts lattice points in the
///    disk a² + b² ≤ x (one per mirror-line point); its wobble is Hardy's sum of r₂(n) n^(−3/4) cos(2π√(nx) − 3π/4), one
///    wave per turn, and its first wave is the saddle twisted half a turn per turn.
/// Q. The prime ideals of Euler's ring Z[(1 + √−163)/2]. The zeros of ζ make a wave that spikes at every prime power;
///    the zeros of the Dirichlet L-function of discriminant −163 (computed for this, see Resources/lzeros163) make one that
///    spikes up at primes that split in the ring and down at inert ones. Their sum spikes at the norms of the ring's prime
///    ideals: split primes, the squares of inert primes, and 163. Only split primes can divide Euler's values k² + k + 41.
/// </summary>
public partial class SacksSpiral
{
    // ================================================================================================
    // P. lattice-point waves
    // ================================================================================================

    static int[] r2Table;

    static void EnsureR2(int nMax)
    {
        if (r2Table != null && r2Table.Length > nMax) return;
        r2Table = new int[nMax + 1];
        int r = Mathf.CeilToInt(Mathf.Sqrt(nMax));
        for (int a = -r; a <= r; a++)
        for (int b = -r; b <= r; b++)
        {
            int s = a * a + b * b;
            if (s > 0 && s <= nMax) r2Table[s]++;
        }
    }

    /// <summary>Lattice points with a² + b² ≤ x.</summary>
    static long LatticeCount(double x)
    {
        long cnt = 0;
        int r = (int)System.Math.Floor(System.Math.Sqrt(x));
        for (int a = -r; a <= r; a++) cnt += 2 * (long)System.Math.Floor(System.Math.Sqrt(x - (double)a * a)) + 1;
        return cnt;
    }

    static double[] divisorPrefix;

    static double DivisorWobble(double x)
    {
        int need = (int)x + 2;
        if (divisorPrefix == null || divisorPrefix.Length <= need)
        {
            divisorPrefix = new double[System.Math.Max(need, 2048) + 1];
            for (int n = 1; n < divisorPrefix.Length; n++) divisorPrefix[n] = divisorPrefix[n - 1] + DivisorCount(n);
        }

        const double euler = 0.5772156649015329;
        return divisorPrefix[(int)x] - x * System.Math.Log(x) - (2 * euler - 1) * x;
    }

    static double CircleWobble(double x) => LatticeCount(x) - System.Math.PI * x;

    /// <summary>One wave of the sum: the n-th term of Voronoi's (divisor) or Hardy's (circle) formula at x, without the x^¼/π factor.</summary>
    static double LatticeTerm(bool circle, int n, double x)
    {
        if (circle)
        {
            int r = r2Table[n];
            return r == 0 ? 0 : r * System.Math.Pow(n, -.75) * System.Math.Cos(2 * System.Math.PI * System.Math.Sqrt(n * x) - .75 * System.Math.PI);
        }

        return DivisorCount(n) * System.Math.Pow(n, -.75) * System.Math.Cos(4 * System.Math.PI * System.Math.Sqrt(n * x) - .25 * System.Math.PI) / System.Math.Sqrt(2);
    }

    static float[] LatticeXs(float xMin, float xMax, int samples)
    {
        float[] xs = new float[samples];
        float s0 = Mathf.Sqrt(xMin), s1 = Mathf.Sqrt(xMax);
        for (int i = 0; i < samples; i++) { float s = Mathf.Lerp(s0, s1, i / (samples - 1f)); xs[i] = s * s; }
        return xs;
    }

    static readonly Color LatticeExactColor = Color.white;
    static readonly Color LatticeSeriesColor = new(.25f, .9f, .85f);
    static readonly Color LatticeFirstColor = new(1f, .6f, .25f);

    /// <summary>
    /// Ribbons along the spiral on the current surface, lifted toward the trail: the exact wobble (white), the sum of the first
    /// <paramref name="terms"/> waves (cyan) and the first wave alone (orange). Returns the correlation of the sum with the wobble.
    /// </summary>
    public float ExpLatticeWave(bool circle, int terms, float xMin, float xMax, float amp, bool exact, bool series, bool first, float lift = 1.2f)
    {
        Transform g = ExpGroup(circle ? "P circle wave" : "P divisor wave").transform;
        EnsureR2(terms + 1);
        const int samples = 1500;
        float[] xs = LatticeXs(xMin, xMax, samples);
        double[] e = new double[samples], sr = new double[samples], f1 = new double[samples];
        double maxE = 1e-9;
        for (int i = 0; i < samples; i++)
        {
            double x = xs[i], pre = System.Math.Pow(x, .25) / System.Math.PI, sum = 0;
            e[i] = circle ? CircleWobble(x) : DivisorWobble(x);
            for (int n = 1; n <= terms; n++) sum += LatticeTerm(circle, n, x);
            sr[i] = pre * sum;
            f1[i] = pre * LatticeTerm(circle, 1, x);
            maxE = System.Math.Max(maxE, System.Math.Abs(e[i]));
        }

        float scale = (float)(amp / maxE);
        Vector3[] P(double[] v) { Vector3[] p = new Vector3[samples]; for (int i = 0; i < samples; i++) p[i] = ExpNumberAt(xs[i], lift + (float)v[i] * scale); return p; }
        Vector3[] zero = new Vector3[samples];
        for (int i = 0; i < samples; i++) zero[i] = ExpNumberAt(xs[i], lift);
        ExpLine(g, "zero line", .02f, new Color(.45f, .45f, .55f), Factory.NoGlow, zero);
        if (exact) ExpLine(g, "wobble", .06f, LatticeExactColor, Factory.SoftGlowIntensity, P(e));
        if (series) ExpLine(g, "sum of waves", .06f, LatticeSeriesColor, Factory.GlowIntensity, P(sr));
        if (first) ExpLine(g, "first wave", .07f, LatticeFirstColor, Factory.GlowIntensity, P(f1));
        return (float)Correlation(e, sr);
    }

    /// <summary>Correlation of the wobble with the sum of the first <paramref name="terms"/> waves, for 9 ≤ x ≤ 400.</summary>
    public float ExpLatticeWaveCorrelation(bool circle, int terms)
    {
        EnsureR2(terms + 1);
        float[] xs = LatticeXs(9f, 400f, 1500);
        double[] e = new double[xs.Length], sr = new double[xs.Length];
        for (int i = 0; i < xs.Length; i++)
        {
            double x = xs[i], sum = 0;
            e[i] = circle ? CircleWobble(x) : DivisorWobble(x);
            for (int n = 1; n <= terms; n++) sum += LatticeTerm(circle, n, x);
            sr[i] = System.Math.Pow(x, .25) / System.Math.PI * sum;
        }

        return (float)Correlation(e, sr);
    }

    static double Correlation(double[] a, double[] b)
    {
        double ma = 0, mb = 0;
        for (int i = 0; i < a.Length; i++) { ma += a[i]; mb += b[i]; }
        ma /= a.Length; mb /= b.Length;
        double sab = 0, saa = 0, sbb = 0;
        for (int i = 0; i < a.Length; i++) { sab += (a[i] - ma) * (b[i] - mb); saa += (a[i] - ma) * (a[i] - ma); sbb += (b[i] - mb) * (b[i] - mb); }
        return sab / System.Math.Sqrt(saa * sbb + 1e-300);
    }

    public const float LatticeChartOffset = 3200f;

    /// <summary>
    /// The wobble as a flat chart: √x along the bottom from 3 to 20 (one unit is one turn of the Sacks spiral, marked by a faint
    /// line), the exact wobble in white, the sum of the first <paramref name="terms"/> waves in cyan, the first wave in orange.
    /// </summary>
    public void ExpLatticeChart(bool circle, int terms, Vector3 center, float width, float height, float labelSize, bool exact, bool series, bool first)
    {
        Transform g = ExpGroup(circle ? "P circle chart" : "P divisor chart").transform;
        LatticeChartFrame(g, circle, center, width, height, labelSize);
        EnsureR2(terms + 1);
        const int samples = 1600;
        float[] xs = LatticeXs(9f, 400f, samples);
        double[] e = new double[samples], sr = new double[samples], f1 = new double[samples];
        double maxE = 1e-9;
        for (int i = 0; i < samples; i++)
        {
            double x = xs[i], pre = System.Math.Pow(x, .25) / System.Math.PI, sum = 0;
            e[i] = circle ? CircleWobble(x) : DivisorWobble(x);
            if (series) for (int n = 1; n <= terms; n++) sum += LatticeTerm(circle, n, x);
            sr[i] = pre * sum;
            f1[i] = pre * LatticeTerm(circle, 1, x);
            maxE = System.Math.Max(maxE, System.Math.Abs(e[i]));
        }

        if (exact) ExpLine(g, "wobble", .1f, LatticeExactColor, Factory.SoftGlowIntensity, ChartPoints(xs, e, maxE, center, width, height, .05f));
        if (series) ExpLine(g, "sum", .1f, LatticeSeriesColor, Factory.GlowIntensity, ChartPoints(xs, sr, maxE, center, width, height, .1f));
        if (first) ExpLine(g, "first wave", .12f, LatticeFirstColor, Factory.GlowIntensity, ChartPoints(xs, f1, maxE, center, width, height, .15f));
    }

    static Vector3[] ChartPoints(float[] xs, double[] v, double maxAbs, Vector3 center, float width, float height, float lift)
    {
        Vector3[] p = new Vector3[xs.Length];
        float s0 = Mathf.Sqrt(xs[0]), s1 = Mathf.Sqrt(xs[^1]);
        for (int i = 0; i < xs.Length; i++)
        {
            float sx = (Mathf.Sqrt(xs[i]) - s0) / (s1 - s0) * width - width * .5f;
            p[i] = center + new Vector3(sx, (float)(v[i] / maxAbs) * height * .5f, 0f) + Vector3.back * lift;
        }

        return p;
    }

    void LatticeChartFrame(Transform g, bool circle, Vector3 center, float width, float height, float labelSize)
    {
        Color frame = new(.45f, .45f, .55f), turn = new(.25f, .25f, .33f);
        float hw = width * .5f, hh = height * .5f;
        ExpLine(g, "frame", .05f, frame, Factory.NoGlow, new[] { center + new Vector3(-hw, -hh, 0), center + new Vector3(hw, -hh, 0), center + new Vector3(hw, hh, 0), center + new Vector3(-hw, hh, 0) }, true);
        ExpLine(g, "zero", .04f, frame, Factory.NoGlow, new[] { center + new Vector3(-hw, 0, 0), center + new Vector3(hw, 0, 0) });
        const float s0 = 3f, s1 = 20f;
        for (int t = 3; t <= 20; t++)
        {
            float x = (t - s0) / (s1 - s0) * width - hw;
            ExpLine(g, "turn " + t, .03f, turn, Factory.NoGlow, new[] { center + new Vector3(x, -hh, 0), center + new Vector3(x, hh, 0) });
            if (t % 5 == 0) ExpLabel(g, $"turn {t}", center + new Vector3(x, -hh - .07f * labelSize - .5f, 0), labelSize * .7f, new Color(.7f, .7f, .8f));
        }

        string title = circle
            ? "points in a circle\nHardy's waves: one per turn"
            : "points under uv = x\nVoronoi's waves: two per turn";
        ExpLabel(g, title, center + new Vector3(0, hh + .16f * labelSize + .6f, 0), labelSize * .9f, Color.white);
    }

    /// <summary>Live chart: the cyan sum gains its waves one by one and settles onto the white wobble.</summary>
    public void ExpLatticeChartLive(bool circle, int termsMax, float seconds, Vector3 center, float width, float height, float labelSize)
    {
        StartLive(LatticeChartLive(circle, termsMax, seconds, center, width, height, labelSize));
    }

    IEnumerator LatticeChartLive(bool circle, int termsMax, float seconds, Vector3 center, float width, float height, float labelSize)
    {
        Transform g = SectionLiveRoot();
        LatticeChartFrame(g, circle, center, width, height, labelSize);
        EnsureR2(termsMax + 1);
        const int samples = 1200;
        float[] xs = LatticeXs(9f, 400f, samples);
        double[] e = new double[samples], acc = new double[samples], v = new double[samples];
        double maxE = 1e-9;
        for (int i = 0; i < samples; i++) { e[i] = circle ? CircleWobble(xs[i]) : DivisorWobble(xs[i]); maxE = System.Math.Max(maxE, System.Math.Abs(e[i])); }
        ExpLine(g, "wobble", .1f, LatticeExactColor, Factory.SoftGlowIntensity, ChartPoints(xs, e, maxE, center, width, height, .05f));
        LineRenderer sum = Factory.NewLine("sum", .1f, LatticeSeriesColor, Factory.GlowIntensity);
        sum.transform.SetParent(g, false);
        sum.positionCount = samples;
        TextBox label = TextBoxLabel("", labelSize * .8f, LatticeSeriesColor);
        label.transform.SetParent(g, false);
        label.transform.position = center + new Vector3(0, -height * .5f - .22f * labelSize - 1.2f, 0);
        int done = 0;
        float time = 0f;
        while (true)
        {
            float u = seconds <= 0f ? 1f : Mathf.Clamp01(time / seconds);
            int target = Mathf.Clamp(Mathf.CeilToInt(Mathf.Pow(termsMax, Mathf.SmoothStep(0f, 1f, u))), 1, termsMax);
            if (target != done)
            {
                for (int n = done + 1; n <= target; n++)
                {
                    if (circle && r2Table[n] == 0) continue;
                    for (int i = 0; i < samples; i++) acc[i] += LatticeTerm(circle, n, xs[i]);
                }

                done = target;
                for (int i = 0; i < samples; i++) v[i] = System.Math.Pow(xs[i], .25) / System.Math.PI * acc[i];
                sum.SetPositions(ChartPoints(xs, v, maxE, center, width, height, .1f));
                label.Text = $"{done:N0} wave{(done == 1 ? "" : "s")}:  correlation with the wobble {Correlation(e, v):0.000}";
            }

            if (u >= 1f) break;
            time += Time.deltaTime;
            yield return null;
        }

        liveRoutine = null;
    }

    /// <summary>Live: the sum of waves gains its terms one by one (fast at first, then by hundreds) and settles onto the wobble.</summary>
    public void ExpLatticeLive(bool circle, int termsMax, float seconds, float xMin, float xMax, float amp, float labelSize)
    {
        StartLive(LatticeLive(circle, termsMax, seconds, xMin, xMax, amp, labelSize));
    }

    IEnumerator LatticeLive(bool circle, int termsMax, float seconds, float xMin, float xMax, float amp, float labelSize)
    {
        Transform g = SectionLiveRoot();
        EnsureR2(termsMax + 1);
        const int samples = 1200;
        float[] xs = LatticeXs(xMin, xMax, samples);
        double[] e = new double[samples], acc = new double[samples];
        double maxE = 1e-9;
        for (int i = 0; i < samples; i++) { e[i] = circle ? CircleWobble(xs[i]) : DivisorWobble(xs[i]); maxE = System.Math.Max(maxE, System.Math.Abs(e[i])); }
        float scale = (float)(amp / maxE);
        const float lift = 1.2f;
        Vector3[] pts = new Vector3[samples];
        for (int i = 0; i < samples; i++) pts[i] = ExpNumberAt(xs[i], lift + (float)e[i] * scale);
        LineRenderer exact = Factory.NewLine("live wobble", .06f, LatticeExactColor, Factory.SoftGlowIntensity);
        exact.transform.SetParent(g, false);
        exact.positionCount = samples;
        exact.SetPositions(pts);
        LineRenderer sum = Factory.NewLine("live sum", .07f, LatticeSeriesColor, Factory.GlowIntensity);
        sum.transform.SetParent(g, false);
        sum.positionCount = samples;
        TextBox label = TextBoxLabel("", labelSize, LatticeSeriesColor);
        label.transform.SetParent(g, false);
        label.transform.position = origin + new Vector3(0f, -Extent - .9f * labelSize, 0f) + Vector3.back * 3f;
        int done = 0;
        float time = 0f;
        while (true)
        {
            float u = seconds <= 0f ? 1f : Mathf.Clamp01(time / seconds);
            int target = Mathf.Clamp(Mathf.CeilToInt(Mathf.Pow(termsMax, Mathf.SmoothStep(0f, 1f, u))), 1, termsMax);
            for (int n = done + 1; n <= target; n++)
            {
                if (circle && r2Table[n] == 0) continue;
                for (int i = 0; i < samples; i++) acc[i] += LatticeTerm(circle, n, xs[i]);
            }

            if (target != done || done == 0)
            {
                done = target;
                double[] v = new double[samples];
                for (int i = 0; i < samples; i++)
                {
                    v[i] = System.Math.Pow(xs[i], .25) / System.Math.PI * acc[i];
                    pts[i] = ExpNumberAt(xs[i], lift + (float)v[i] * scale);
                }

                sum.SetPositions(pts);
                label.Text = $"{done:N0} wave{(done == 1 ? "" : "s")}   correlation with the wobble {Correlation(e, v):0.000}";
            }

            if (u >= 1f) break;
            time += Time.deltaTime;
            yield return null;
        }

        liveRoutine = null;
    }

    /// <summary>
    /// Live: twist the surface from twistFrom·π to twistTo·π per unit of Sacks radius while the numbers ride it (squares red,
    /// pronic numbers blue, primes gold), with its crest and trough lines, and a ribbon of the first wave whose frequency and phase
    /// follow the twist: cos(4π√x − π/4) (the divisor problem's) at no twist, cos(2π√x − 3π/4) (the circle problem's) at 2π.
    /// </summary>
    public void ExpTwistLive(float twistFromPi, float twistToPi, float seconds, bool ribbon, int nMax, float labelSize)
    {
        StartLive(TwistLive(twistFromPi * Mathf.PI, twistToPi * Mathf.PI, seconds, ribbon, nMax, labelSize));
    }

    IEnumerator TwistLive(float fromR, float toR, float seconds, bool ribbon, int nMax, float labelSize)
    {
        Transform g = SectionLiveRoot();
        ShowBackdrop(false);
        float rMax = R * Mathf.Sqrt(nMax) + 1f;
        // the number field as one mesh whose vertices move
        List<Vector3> v0 = new(); List<Color> cols = new(); List<float> sizes = new(); List<int> ns = new();
        for (int n = 2; n <= nMax; n++)
        {
            int k = Mathf.FloorToInt(Mathf.Sqrt(n));
            bool p = IsPrime(n), sq = k * k == n, pr = k * (k + 1) == n;
            ns.Add(n); v0.Add(Vector3.zero);
            cols.Add(p ? Factory.Palette.Zero : sq ? SquareColor : pr ? PronicColor : DimDotColor);
            sizes.Add(p ? .26f : sq || pr ? .36f : .11f);
        }

        MeshRenderer cloud = ExpPointCloud(g, "twisting numbers", v0, cols, sizes, Factory.SoftGlowIntensity);
        Mesh mesh = cloud.GetComponent<MeshFilter>().sharedMesh;
        Vector3[] verts = new Vector3[ns.Count * 6];
        Vector3[] axes = { Vector3.right, Vector3.up, Vector3.forward };
        List<LineRenderer> rings = new();
        for (float r = 6f; r <= rMax; r += 6f)
        {
            LineRenderer l = Factory.NewLine("ring " + r, .03f, new Color(.32f, .36f, .5f), Factory.NoGlow);
            l.transform.SetParent(g, false);
            l.positionCount = 121;
            rings.Add(l);
        }

        LineRenderer[] crest = new LineRenderer[4];
        for (int i = 0; i < 4; i++)
        {
            crest[i] = Factory.NewLine("crest " + i, .07f, i < 2 ? FallColor : RiseColor, Factory.SoftGlowIntensity);
            crest[i].transform.SetParent(g, false);
            crest[i].positionCount = 300;
        }

        LineRenderer squares = Factory.NewLine("squares", .1f, SquareColor, Factory.SoftGlowIntensity);
        LineRenderer pronics = Factory.NewLine("pronics", .1f, PronicColor, Factory.SoftGlowIntensity);
        squares.transform.SetParent(g, false);
        pronics.transform.SetParent(g, false);
        int kMax = Mathf.FloorToInt(Mathf.Sqrt(nMax));
        LineRenderer wave = null;
        const int samples = 900;
        float[] xs = LatticeXs(2f, nMax, samples);
        if (ribbon)
        {
            wave = Factory.NewLine("first wave", .08f, LatticeFirstColor, Factory.GlowIntensity);
            wave.transform.SetParent(g, false);
            wave.positionCount = samples;
        }

        TextBox label = TextBoxLabel("", labelSize, Color.white);
        label.transform.SetParent(g, false);
        label.transform.position = origin + Vector3.back * (bend * SaddleK * rMax * rMax + .15f * labelSize + 4f);
        float time = 0f;
        Vector3[] buf = new Vector3[300];
        while (true)
        {
            float u = seconds <= 0f ? 1f : Mathf.Clamp01(time / seconds);
            float twistR = Mathf.Lerp(fromR, toR, Mathf.SmoothStep(0f, 1f, u));
            ExpSetSurface(2, twistR);
            for (int i = 0; i < ns.Count; i++)
            {
                Vector3 p = ExpNumberAt(ns[i]);
                float s = sizes[i];
                verts[6 * i] = p + axes[0] * s; verts[6 * i + 1] = p - axes[0] * s;
                verts[6 * i + 2] = p + axes[1] * s; verts[6 * i + 3] = p - axes[1] * s;
                verts[6 * i + 4] = p + axes[2] * s; verts[6 * i + 5] = p - axes[2] * s;
            }

            mesh.vertices = verts;
            mesh.RecalculateBounds();
            int ri = 0;
            for (float r = 6f; r <= rMax; r += 6f, ri++)
            {
                for (int i = 0; i <= 120; i++) rings[ri].SetPosition(i, ExpAt(r, i / 120f * 2f * Mathf.PI));
            }

            for (int c = 0; c < 4; c++)
            {
                int j = c % 2, kind = c / 2; // two crests (cos = 1) then two troughs (cos = −1)
                for (int i = 0; i < 300; i++)
                {
                    float r = rMax * i / 299f;
                    buf[i] = ExpAt(r, (expTwist * r + kind * Mathf.PI + 2f * Mathf.PI * j) / 2f, .1f);
                }

                crest[c].SetPositions(buf);
            }

            squares.positionCount = kMax;
            pronics.positionCount = kMax - 1;
            for (int k = 1; k <= kMax; k++) squares.SetPosition(k - 1, ExpNumberAt(k * k, .2f));
            for (int k = 1; k < kMax; k++) pronics.SetPosition(k - 1, ExpNumberAt(k * (k + 1), .2f));
            if (wave != null)
            {
                float freq = 4f * Mathf.PI - twistR, phase = .25f * Mathf.PI + .25f * twistR;
                float a = 7f / Mathf.Pow(nMax, .25f);
                for (int i = 0; i < samples; i++)
                {
                    float s = Mathf.Sqrt(xs[i]);
                    wave.SetPosition(i, ExpNumberAt(xs[i], 1.5f + a * Mathf.Pow(xs[i], .25f) * Mathf.Cos(freq * s - phase)));
                }
            }

            float turns = (4f * Mathf.PI - twistR) / (2f * Mathf.PI);
            label.Text = $"twist {twistR / Mathf.PI:0.00}π per turn:  {turns:0.00} wave{(Mathf.Abs(turns - 1f) < .005f ? "" : "s")} per turn of the spiral";
            if (u >= 1f) break;
            time += Time.deltaTime;
            yield return null;
        }

        liveRoutine = null;
    }

    // ================================================================================================
    // Q. the prime ideals of Euler's ring
    // ================================================================================================

    static double[] lZeros;
    public const float IdealT = 6000f;

    /// <summary>The zeros of L(s, (·/163)) below height 6000 (Resources/lzeros163, computed for this experiment).</summary>
    public static double[] LZeros163
    {
        get
        {
            if (lZeros != null) return lZeros;
            TextAsset t = Resources.Load<TextAsset>("lzeros163");
            List<double> z = new();
            if (t != null)
            {
                foreach (string line in t.text.Split('\n'))
                {
                    string[] parts = line.Trim().Split(' ');
                    if (parts.Length >= 2 && double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double v)) z.Add(v);
                }
            }

            lZeros = z.ToArray();
            return lZeros;
        }
    }

    public static int Chi163(long n)
    {
        long a = ((n % 163) + 163) % 163;
        if (a == 0) return 0;
        return ModPow(a, 81, 163) == 1 ? 1 : -1;
    }

    public enum IdealClass { Other, SplitPrime, InertPrime, InertNorm, Ramified, SplitPower }

    /// <summary>How n sits in Euler's ring: split prime, inert prime, the norm of an inert prime (p^even), 163, or neither.</summary>
    public static IdealClass ClassOf(long n)
    {
        if (n < 2) return IdealClass.Other;
        long p = 0;
        for (long d = 2; d * d <= n; d++) if (n % d == 0) { p = d; break; }
        if (p == 0) return n == 163 ? IdealClass.Ramified : Chi163(n) == 1 ? IdealClass.SplitPrime : IdealClass.InertPrime;
        long m = n; int k = 0;
        while (m % p == 0) { m /= p; k++; }
        if (m != 1) return IdealClass.Other;
        if (p == 163) return IdealClass.Ramified;
        if (Chi163(p) == 1) return IdealClass.SplitPower;
        return k % 2 == 0 ? IdealClass.InertNorm : IdealClass.Other;
    }

    static Color IdealColor(IdealClass c) => c switch
    {
        IdealClass.SplitPrime => Factory.Palette.Zero,
        IdealClass.SplitPower => new Color(1f, .9f, .55f),
        IdealClass.InertPrime => new Color(.35f, .5f, 1f),
        IdealClass.InertNorm => new Color(.3f, .95f, 1f),
        IdealClass.Ramified => Color.white,
        _ => DimDotColor
    };

    /// <summary>Every number ≤ nMax on the current surface, coloured by how it sits in Euler's ring.</summary>
    public void ExpIdealField(int nMax)
    {
        Transform g = ExpGroup("Q ideal field").transform;
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        for (int n = 2; n <= nMax; n++)
        {
            IdealClass c = ClassOf(n);
            pts.Add(ExpNumberAt(n, .05f));
            cols.Add(IdealColor(c));
            sizes.Add(c == IdealClass.Other ? .1f : c == IdealClass.InertPrime ? .26f : .32f);
        }

        ExpPointCloud(g, "ideal field", pts, cols, sizes, Factory.SoftGlowIntensity);
    }

    /// <summary>−Σ (1 − γ/T) cos(γ log n) over the given zeros below T, at every n ≤ nMax (the raw tapered sum).</summary>
    static double[] ZeroWave(double[] zeros, int nMax, int count)
    {
        double[] w = new double[nMax + 1];
        for (int n = 2; n <= nMax; n++)
        {
            double x = System.Math.Log(n), s = 0;
            for (int i = 0; i < count && i < zeros.Length; i++)
            {
                double g = zeros[i];
                if (g >= IdealT) break;
                s -= (1 - g / IdealT) * System.Math.Cos(g * x);
            }

            w[n] = s;
        }

        return w;
    }

    static double idealUnit = -1;

    /// <summary>
    /// The raw sum peaks at a prime power p^k with height about log p / √(p^k) times a constant; multiplying by √n / log n and
    /// dividing by that constant (measured from ζ's wave at the primes up to 200) puts every prime at 1 and every p^k at 1/k.
    /// </summary>
    static double IdealFactor(int n)
    {
        if (idealUnit < 0)
        {
            double[] zs = Main.Instance.zetaZeroValues;
            double sum = 0; int cnt = 0;
            for (int p = 2; p <= 200; p++)
            {
                if (!IsPrime(p)) continue;
                double x = System.Math.Log(p), w = 0;
                for (int i = 0; i < zs.Length && zs[i] < IdealT; i++) w -= (1 - zs[i] / IdealT) * System.Math.Cos(zs[i] * x);
                sum += w * System.Math.Sqrt(p) / x;
                cnt++;
            }

            idealUnit = sum / cnt;
        }

        return System.Math.Sqrt(n) / System.Math.Log(n) / idealUnit;
    }

    MeshRenderer idealNeedles;
    Mesh idealMesh;
    float idealNeedleScale = .02f;

    /// <summary>A needle at every n ≤ nMax standing on the surface toward the trail, height ∝ the wave's value, coloured by class.</summary>
    void DrawIdealNeedles(Transform g, double[] wave, int nMax, float scale)
    {
        int count = nMax - 1;
        if (idealMesh == null || idealNeedles == null)
        {
            idealMesh = new Mesh { name = "ideal needles", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            List<int> tris = new(); List<Color> cols = new();
            for (int i = 0; i < count; i++)
            {
                Color c = IdealColor(ClassOf(i + 2));
                int v = 8 * i;
                for (int k = 0; k < 8; k++) cols.Add(c);
                tris.AddRange(new[] { v, v + 2, v + 1, v + 1, v + 2, v + 3, v, v + 1, v + 2, v + 1, v + 3, v + 2 });
                tris.AddRange(new[] { v + 4, v + 6, v + 5, v + 5, v + 6, v + 7, v + 4, v + 5, v + 6, v + 5, v + 7, v + 6 });
            }

            idealMesh.SetVertices(new Vector3[8 * count]);
            idealMesh.SetColors(cols);
            idealMesh.SetTriangles(tris, 0);
            GameObject go = new("ideal needles");
            go.transform.SetParent(g, false);
            go.AddComponent<MeshFilter>().sharedMesh = idealMesh;
            idealNeedles = go.AddComponent<MeshRenderer>();
            idealNeedles.sharedMaterial = Factory.mainMat;
            idealNeedles.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Factory.SetIntensity(idealNeedles, Factory.GlowIntensity);
        }

        Vector3[] verts = new Vector3[8 * count];
        for (int i = 0; i < count; i++)
        {
            int n = i + 2;
            Vector3 b = ExpNumberAt(n, .05f);
            float h = (float)(wave[n] * IdealFactor(n)) * scale;
            Vector3 t = b + Vector3.back * h;
            float wdt = ClassOf(n) == IdealClass.Other ? .05f : .1f;
            Vector3 s1 = Vector3.right * wdt, s2 = Vector3.up * wdt;
            int v = 8 * i;
            verts[v] = b - s1; verts[v + 1] = b + s1; verts[v + 2] = t - s1; verts[v + 3] = t + s1;
            verts[v + 4] = b - s2; verts[v + 5] = b + s2; verts[v + 6] = t - s2; verts[v + 7] = t + s2;
        }

        idealMesh.vertices = verts;
        idealMesh.RecalculateBounds();
    }

    /// <summary>Static needles: "zeta", "l" or "both" waves at every n ≤ nMax.</summary>
    public void ExpIdealNeedles(int nMax, string which, float scale)
    {
        Transform g = ExpGroup("Q needles " + which).transform;
        idealMesh = null; idealNeedles = null;
        double[] zs = Main.Instance.zetaZeroValues, ls = LZeros163;
        double[] w = new double[nMax + 1];
        if (which != "l") { double[] a = ZeroWave(zs, nMax, zs.Length); for (int n = 2; n <= nMax; n++) w[n] += a[n]; }
        if (which != "zeta") { double[] b = ZeroWave(ls, nMax, ls.Length); for (int n = 2; n <= nMax; n++) w[n] += b[n]; }
        DrawIdealNeedles(g, w, nMax, scale);
    }

    static double[] idealZetaAcc;

    /// <summary>ζ's full tapered sum at every n ≤ nMax (cached), so a line that adds the L-function's zeros can start from it.</summary>
    static double[] IdealZetaSums(int nMax)
    {
        if (idealZetaAcc != null && idealZetaAcc.Length > nMax) return idealZetaAcc;
        idealZetaAcc = ZeroWave(Main.Instance.zetaZeroValues, nMax, int.MaxValue);
        return idealZetaAcc;
    }

    /// <summary>
    /// Live needles. mode "zeta": ζ's zeros are added from none to all below height 6000; "l": starting from ζ's full wave, the
    /// L-function's zeros join (inert primes sink, split primes double); "lonly": the L-function's zeros alone.
    /// </summary>
    public void ExpIdealWaveLive(int nMax, string mode, float seconds, float scale, float labelSize)
    {
        StartLive(IdealWaveLive(nMax, mode, seconds, scale, labelSize));
    }

    IEnumerator IdealWaveLive(int nMax, string mode, float seconds, float scale, float labelSize)
    {
        Transform g = SectionLiveRoot();
        idealMesh = null; idealNeedles = null;
        double[] zs = Main.Instance.zetaZeroValues, ls = LZeros163;
        int zCount = 0; while (zCount < zs.Length && zs[zCount] < IdealT) zCount++;
        double[] acc = new double[nMax + 1], logs = new double[nMax + 1];
        for (int n = 2; n <= nMax; n++) logs[n] = System.Math.Log(n);
        if (mode == "l")
        {
            double[] zAcc = IdealZetaSums(nMax);
            for (int n = 2; n <= nMax; n++) acc[n] = zAcc[n];
        }

        double[] adding = mode == "zeta" ? zs : ls;
        int count = mode == "zeta" ? zCount : ls.Length;
        TextBox label = TextBoxLabel("", labelSize, Color.white);
        label.transform.SetParent(g, false);
        label.transform.position = origin + new Vector3(0f, -R * Mathf.Sqrt(nMax) - .15f * labelSize - 1f, 0f) + Vector3.back * 3f;
        int done = 0;
        float time = 0f;
        while (true)
        {
            float u = seconds <= 0f ? 1f : Mathf.Clamp01(time / seconds);
            int target = Mathf.RoundToInt(count * Mathf.SmoothStep(0f, 1f, u));
            for (int i = done; i < target; i++)
            {
                double w = 1 - adding[i] / IdealT, gm = adding[i];
                for (int n = 2; n <= nMax; n++) acc[n] -= w * System.Math.Cos(gm * logs[n]);
            }

            done = target;
            DrawIdealNeedles(g, acc, nMax, scale);
            label.Text = mode == "zeta" ? $"zeros of ζ: {done:N0}"
                : mode == "l" ? $"zeros of ζ: {zCount:N0}\nzeros of L(s, χ<sub>−163</sub>): {done:N0}"
                : $"zeros of L(s, χ<sub>−163</sub>): {done:N0}";
            if (u >= 1f) break;
            time += Time.deltaTime;
            yield return null;
        }

        liveRoutine = null;
    }

    /// <summary>Euler's line k² + k + 41 up to nMax on the current surface, values dotted.</summary>
    public void ExpEulerRoute(long nMax)
    {
        ExpQuadratic(ExpGroup("Q euler route").transform, 1, 1, 41, 0f, nMax, EulerColor, .09f, true, .3f, .3f);
    }

    /// <summary>Labels at the given numbers, just above the surface.</summary>
    public void ExpNumberLabels(long[] ns, float size, Color color)
    {
        Transform g = ExpGroup("number labels").transform;
        foreach (long n in ns)
        {
            Color c = color == Color.white ? IdealColor(ClassOf(n)) : color;
            ExpLabel(g, n.ToString(), ExpNumberAt(n, .6f) + new Vector3(0f, -.55f * size, 0f), size, c);
        }
    }

    /// <summary>Euler's line and Landau's k² + 1 with the only two numbers they share, 197 and 1601.</summary>
    public string ExpEulerLandauMeet(float labelSize)
    {
        return ExpAtlas(new[] { new AtlasCurve(1, 41, 0), new AtlasCurve(0, 1, 1) }, Count, labelSize, new long[] { 197, 1601 });
    }
}
