using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Two master waves riding the Sacks spiral on its saddle (the long-form segment on Voronoi), drawn as the home view
/// draws its master wave: a curtain from the spiral up to the wave, rescaled to its own largest value.
///   prime    the home view's own wave, −Σ cos(γ log x) over 10 000 zeros of ζ (the baked sums): it spikes at the primes
///            and prime powers, and its waves tick on log x
///   divisor  the derivative of Voronoi's formula, −√2 x^(−¼) Σ w_m d(m) m^(−¼) sin(4π√(mx) − π/4), times √x so that every
///            spike stands as tall as its number's count of divisors (about 1.1 √M d(n) for M = 4000 waves, correlation
///            0.997 with d(n) for n under 150): it spikes at every whole number, and its waves tick on √x, the spiral's own
///            clock (one turn per unit of √x)
///   term     Voronoi's m-th wave alone, d(m) m^(−¼) sin(4π√(mx) − π/4) (times x^¼ like the sum): 2√m crests per turn, a
///            saddle for m = 1 and a twisted saddle when m is not a square
///   zero     one zero's wave −cos(γ_k log x), and zeros the sum of the first k (not baked): their crests drift from turn
///            to turn, because log x is not the spiral's clock
/// A single wave (term, zero) is drawn as a bright line over a faint curtain.
/// The weights w are a smooth (Gaussian) cutoff, so partial sums do not ring. Beads mark the numbers: gold primes and
/// orange prime powers on the prime wave; on the divisor wave a bead at the top of every number's spike, gold for the
/// primes (two divisors) and white for the rest, sized by its count of divisors.
/// </summary>
public partial class SacksSpiral
{
    static int[] waveDivisors = new int[1];
    static float[] bakedPrimeWave;
    const double PrimeWaveMax = 1601.0;
    static readonly Color DivisorWaveColor = new(1f, .55f, .28f);
    static readonly Color PrimeBead = new(1f, .82f, .3f), PowerBead = new(1f, .55f, .3f);

    static void EnsureDivisorCounts(int n)
    {
        if (waveDivisors.Length > n) return;
        waveDivisors = new int[n + 1];
        for (int a = 1; a <= n; a++)
            for (int b = a; b <= n; b += a) waveDivisors[b]++;
    }

    /// <summary>The home view's baked master wave (10 000 zeros, 160 000 samples, log-spaced over 1.5 … 1601), or null.</summary>
    static float[] BakedPrimeWave()
    {
        if (bakedPrimeWave != null) return bakedPrimeWave;
        TextAsset baked = Resources.Load<TextAsset>(BakedFullWaveName(10000, 160000, (float)PrimeWaveMax));
        if (baked == null) return null;
        byte[] bytes = baked.bytes;
        Resources.UnloadAsset(baked);
        bakedPrimeWave = new float[bytes.Length / 4];
        for (int i = 0; i < bakedPrimeWave.Length; i++) bakedPrimeWave[i] = System.BitConverter.ToSingle(bytes, i * 4);
        return bakedPrimeWave;
    }

    /// <summary>The prime wave at x from the baked sums (nearest sample), or from the first 2000 zeros if there is no bake.</summary>
    static double PrimeWaveAt(double x, double[] zeros)
    {
        float[] b = BakedPrimeWave();
        if (b != null)
        {
            double u = System.Math.Log(x / FieldNMin) / System.Math.Log(PrimeWaveMax / FieldNMin);
            int i = (int)System.Math.Round(u * (b.Length - 1));
            return b[Mathf.Clamp(i, 0, b.Length - 1)];
        }

        double s = 0, lx = System.Math.Log(x);
        for (int k = 0; k < 2000 && k < zeros.Length; k++) s -= System.Math.Cos(zeros[k] * lx);
        return s;
    }

    /// <summary>The weight of term k of `terms` (a Gaussian cutoff that keeps the partial sums smooth).</summary>
    static double WaveWeight(int k, int terms) => System.Math.Exp(-4.0 * (k / (double)terms) * (k / (double)terms));

    /// <summary>
    /// Add Voronoi's waves m = from … to (weighted for a sum of `terms`, or unweighted if terms is 0) into s, sampled at
    /// x0 + dx·i: s += w_m d(m) m^(−¼) sin(4π√(mx) − π/4). The display value is then −√2 x^¼ s (see DivisorDisplay).
    /// </summary>
    static void AddDivisorTerms(double[] s, double x0, double dx, int from, int to, int terms)
    {
        EnsureDivisorCounts(to);
        int n = s.Length;
        double[] sx = new double[n];
        for (int i = 0; i < n; i++) sx[i] = System.Math.Sqrt(x0 + dx * i);
        for (int m = from; m <= to; m++)
        {
            double a = (terms > 0 ? WaveWeight(m, terms) : 1.0) * waveDivisors[m] * System.Math.Pow(m, -.25), c = 4 * System.Math.PI * System.Math.Sqrt(m);
            for (int i = 0; i < n; i++) s[i] += a * System.Math.Sin(c * sx[i] - System.Math.PI / 4);
        }
    }

    /// <summary>The derivative of Voronoi's sum times √x: −√2 x^¼ s.</summary>
    static double[] DivisorDisplay(double[] s, double x0, double dx)
    {
        double[] v = new double[s.Length];
        for (int i = 0; i < s.Length; i++) v[i] = -System.Math.Sqrt(2) * System.Math.Pow(x0 + dx * i, .25) * s[i];
        return v;
    }

    /// <summary>A drawn wave, kept for callouts at its spikes.</summary>
    class DrawnWave
    {
        public double X0, Dx;
        public float[] Shown;
        public float Height;
        public Vector3 Offset;
    }

    readonly Dictionary<string, DrawnWave> drawnWaves = new();
    // the live build's first stages: a faint, low curtain under the ribbon, so the shape still has height
    bool curtainUnderRibbon;
    string lastWave = "";

    /// <summary>
    /// The top of the wave `kind` (or the last one drawn, if kind is empty) at n, or the spiral at n if no wave was drawn.
    /// For a spike, the highest point within a quarter of a unit of n.
    /// </summary>
    public Vector3 NumberWaveTop(double n, string kind = "")
    {
        if (kind.Length == 0) kind = lastWave;
        if (!drawnWaves.TryGetValue(kind, out DrawnWave w)) return ExpNumberAt(n, .5f);
        int i0 = Mathf.Clamp((int)System.Math.Round((n - .25 - w.X0) / w.Dx), 0, w.Shown.Length - 1);
        int i1 = Mathf.Clamp((int)System.Math.Round((n + .25 - w.X0) / w.Dx), 0, w.Shown.Length - 1);
        float top = 0f;
        for (int i = i0; i <= i1; i++) top = Mathf.Max(top, w.Shown[i]);
        return ExpNumberAt(n, Mathf.Max(.5f, w.Height * top) + .4f) + w.Offset;
    }

    /// <summary>The offset of the wave `kind` (zero if none).</summary>
    public Vector3 NumberWaveOffset(string kind = "")
    {
        if (kind.Length == 0) kind = lastWave;
        return drawnWaves.TryGetValue(kind, out DrawnWave w) ? w.Offset : Vector3.zero;
    }

    /// <summary>
    /// A number wave over 1.5 ≤ x ≤ xMax: "prime" (the home view's wave), "divisor" (`terms` of Voronoi's waves) or
    /// "term" (Voronoi's wave number `terms` alone), its largest value at <paramref name="height"/>, displaced by
    /// <paramref name="offset"/> (group "NW kind").
    /// </summary>
    public void ExpNumberWave(string kind, int terms, float height, double xMax, Vector3 offset)
    {
        double x0 = FieldNMin, dx = .01;
        int n = (int)((xMax - x0) / dx) + 1;
        double[] v;
        if (kind == "divisor" || kind == "term")
        {
            double[] s = new double[n];
            if (kind == "divisor") AddDivisorTerms(s, x0, dx, 1, terms, terms);
            else AddDivisorTerms(s, x0, dx, terms, terms, 0);
            v = DivisorDisplay(s, x0, dx);
        }
        else if (kind == "zero" || kind == "zeros")
        {
            // one zero's wave −cos(γ_k log x) (k = terms), or the sum of the first `terms` of them
            v = new double[n];
            double[] zeros = Main.Instance.zetaZeroValues;
            int k0 = kind == "zero" ? Mathf.Clamp(terms, 1, zeros.Length) - 1 : 0, k1 = Mathf.Clamp(terms, 1, zeros.Length);
            for (int i = 0; i < n; i++)
            {
                double lx = System.Math.Log(x0 + dx * i), sum = 0;
                for (int k = k0; k < k1; k++) sum -= System.Math.Cos(zeros[k] * lx);
                v[i] = sum;
            }
        }
        else
        {
            v = new double[n];
            double[] zeros = Main.Instance.zetaZeroValues;
            for (int i = 0; i < n; i++) v[i] = PrimeWaveAt(x0 + dx * i, zeros);
        }

        bool single = kind == "term" || kind == "zero";
        DrawNumberWave(ExpGroup($"NW {kind}").transform, kind, v, x0, dx, height, offset, !single && kind != "zeros", single);
    }

    /// <summary>
    /// Rescale to the largest value over the upper three quarters of the range, then draw the curtain (columns every ~.1
    /// world units of arc, each spanning the values it covers), the spiral under it, and the beads.
    /// </summary>
    void DrawNumberWave(Transform g, string kind, double[] v, double x0, double dx, float height, Vector3 offset, bool beads, bool smooth = false)
    {
        bool prime = kind == "prime" || kind == "zero" || kind == "zeros";
        double xMax = x0 + dx * (v.Length - 1), top = 1e-9;
        for (int i = 0; i < v.Length; i++)
            if (x0 + dx * i > xMax * .25) top = System.Math.Max(top, System.Math.Abs(v[i]));
        float[] shown = new float[v.Length];
        for (int i = 0; i < v.Length; i++) shown[i] = Mathf.Clamp((float)(v[i] / top), -1.2f, 1.6f);
        // columns: walk the samples and close a column every ~.1 world units of arc
        List<Vector3> verts = new();
        List<Color> cols = new();
        List<int> tris = new();
        Color col = prime ? Factory.Palette.Cosine : DivisorWaveColor;
        // a single smooth wave is a bright line over a faint curtain; a sum of waves is a curtain, as in the home view
        Color foot = new(col.r, col.g, col.b, smooth ? .03f : .12f), head = smooth ? new Color(col.r, col.g, col.b, .3f) : col;
        List<Vector3> crest = new();
        int start = 0;
        Vector3 last = ExpNumberAt(x0, 0f);
        float arc = 0f;
        List<Vector3> spiral = new();
        for (int i = 1; i < v.Length; i++)
        {
            Vector3 p = ExpNumberAt(x0 + dx * i, 0f);
            arc += Vector3.Distance(p, last);
            last = p;
            if (arc < .1f && i < v.Length - 1) continue;
            float lo = 0f, hi = 0f;
            for (int k = start; k <= i; k++)
            {
                lo = Mathf.Min(lo, shown[k]);
                hi = Mathf.Max(hi, shown[k]);
            }

            double xc = x0 + dx * (start + i) * .5;
            Vector3 b = ExpNumberAt(xc, 0f) + offset;
            spiral.Add(b);
            int vi = verts.Count;
            verts.Add(b + Vector3.back * (lo * height));
            verts.Add(b + Vector3.back * (hi * height + .12f));
            cols.Add(foot);
            cols.Add(head);
            crest.Add(b + Vector3.back * (shown[(start + i) / 2] * height + .12f));
            if (vi >= 2)
            {
                tris.AddRange(new[] { vi - 2, vi - 1, vi + 1, vi - 2, vi + 1, vi });
                tris.AddRange(new[] { vi - 2, vi + 1, vi - 1, vi - 2, vi, vi + 1 });
            }

            start = i;
            arc = 0f;
        }

        if (!smooth || curtainUnderRibbon)
        {
            Mesh mesh = new() { name = "curtain", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            mesh.SetColors(cols);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            GameObject go = new("curtain");
            go.transform.SetParent(g, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Factory.mainMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Factory.SetIntensity(mr, .82f);
        }

        if (smooth) WaveRibbon(g, spiral, crest, col, height);
        else ExpLine(g, "spiral", .07f, new Color(.5f, .55f, .75f) * .6f, Factory.NoGlow, spiral.ToArray());
        DrawnWave w = new() { X0 = x0, Dx = dx, Shown = shown, Height = height, Offset = offset };
        drawnWaves[kind] = w;
        lastWave = kind;
        if (!beads) return;
        List<Vector3> pts = new();
        List<Color> pc = new();
        List<float> ps = new();
        EnsureExpSieve((int)xMax + 2);
        EnsureDivisorCounts((int)xMax + 2);
        for (int m = 2; m <= xMax; m++)
        {
            bool isPrime = !expNotPrime[m];
            if (!prime)
            {
                pts.Add(NumberWaveTop(m, kind) + Vector3.back * -.1f);
                pc.Add(isPrime ? PrimeBead : Color.Lerp(new Color(.8f, .82f, .92f), Color.white, Mathf.Clamp01(waveDivisors[m] / 16f)));
                ps.Add(.16f + .022f * waveDivisors[m]);
            }
            else if (isPrime || IsPrimePower(m))
            {
                pts.Add(NumberWaveTop(m, kind) + Vector3.back * -.1f);
                pc.Add(isPrime ? PrimeBead : PowerBead);
                ps.Add(isPrime ? .36f : .3f);
            }
        }

        ExpPointCloud(g, "beads", pts, pc, ps, Factory.GlowIntensity);
    }

    static void EnsureExpSieve(int n) => ExpEnsureSieve(n);

    /// <summary>
    /// A single wave's sign painted on the spiral itself: a ribbon along the spiral, bright in the wave's colour where the
    /// wave is up and deep blue where it is down, so that its crests read from above as a pattern (fixed spokes for
    /// Voronoi's waves, drifting arms for a zero's).
    /// </summary>
    void WaveRibbon(Transform g, List<Vector3> spiral, List<Vector3> crest, Color col, float height)
    {
        int n = spiral.Count;
        if (n < 3) return;
        List<Vector3> verts = new();
        List<Color> cols = new();
        List<int> tris = new();
        Color down = new(.1f, .16f, .5f), mid = new(.07f, .07f, .12f);
        const float half = .5f;
        for (int i = 0; i < n; i++)
        {
            Vector3 along = (spiral[Mathf.Min(n - 1, i + 1)] - spiral[Mathf.Max(0, i - 1)]).normalized;
            Vector3 side = Vector3.Cross(along, Vector3.back).normalized;
            // the wave's value at this column: how far the crest line stands above the spiral, over the full height
            float v = Mathf.Clamp(Vector3.Dot(spiral[i] - crest[i], Vector3.forward) / Mathf.Max(.01f, height), -1f, 1f);
            Color c = v >= 0f ? Color.Lerp(mid, col * 1.15f, v) : Color.Lerp(mid, down, -v);
            verts.Add(spiral[i] + side * half + Vector3.back * .04f);
            verts.Add(spiral[i] - side * half + Vector3.back * .04f);
            cols.Add(c);
            cols.Add(c);
            if (i > 0)
            {
                int a = 2 * i - 2;
                tris.AddRange(new[] { a, a + 1, a + 3, a, a + 3, a + 2, a, a + 3, a + 1, a, a + 2, a + 3 });
            }
        }

        Mesh mesh = new() { name = "wave ribbon", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(verts);
        mesh.SetColors(cols);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        GameObject go = new("wave ribbon");
        go.transform.SetParent(g, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = Factory.mainMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Factory.SetIntensity(mr, 1.1f);
    }

    /// <summary>
    /// The numbers 2 … nMax on the current surface as dots (gold primes, blue squares, grey composites) with the spiral
    /// through them (group "NW numbers"): the Sacks spiral alone, for a surface without a wave.
    /// </summary>
    public void ExpSpiralNumbers(int nMax)
    {
        Transform g = ExpGroup("NW numbers").transform;
        EnsureExpSieve(nMax + 2);
        List<Vector3> line = new();
        for (double x = 1; x <= nMax; x += .02) line.Add(ExpNumberAt(x, .05f));
        ExpLine(g, "spiral", .08f, new Color(.5f, .55f, .75f) * .7f, Factory.NoGlow, line.ToArray());
        List<Vector3> pts = new();
        List<Color> cols = new();
        List<float> sizes = new();
        for (int n = 2; n <= nMax; n++)
        {
            bool prime = !expNotPrime[n], square = Mathf.RoundToInt(Mathf.Sqrt(n)) * Mathf.RoundToInt(Mathf.Sqrt(n)) == n;
            pts.Add(ExpNumberAt(n, .15f));
            cols.Add(prime ? PrimeBead : square ? new Color(.45f, .75f, 1f) : new Color(.5f, .52f, .62f));
            sizes.Add(prime ? .42f : square ? .42f : .26f);
        }

        ExpPointCloud(g, "numbers", pts, cols, sizes, Factory.GlowIntensity);
    }

    /// <summary>
    /// The divisor wave built up live: the first wave alone for <paramref name="hold"/> seconds (the saddle's two crests
    /// per turn), then Voronoi's waves are added, doubling at an even pace, rescaled to their own largest value at every
    /// step; the beads come with the last.
    /// </summary>
    public void ExpNumberWaveLive(int terms, float height, double xMax, float seconds, float hold)
    {
        StartLive(DivisorWaveLive(terms, height, xMax, seconds, hold));
    }

    IEnumerator DivisorWaveLive(int terms, float height, double xMax, float seconds, float hold)
    {
        double x0 = FieldNMin, dx = .01;
        int n = (int)((xMax - x0) / dx) + 1;
        double[] s = new double[n];
        Transform g = LiveRoot();
        GameObject holder = null;
        int added = 0;
        float time = 0f, grow = Mathf.Max(.5f, (seconds - hold) * .9f);
        while (added < terms)
        {
            // the first wave alone for `hold` seconds, then the count doubles at an even pace
            float u = Mathf.Clamp01((time - hold) / grow);
            int want = time < hold ? 1 : Mathf.Clamp(Mathf.RoundToInt(Mathf.Exp(u * Mathf.Log(terms))), 1, terms);
            if (want != added)
            {
                AddDivisorTerms(s, x0, dx, added + 1, want, terms);
                added = want;
                if (holder != null) Destroy(holder);
                holder = new GameObject($"divisor wave {added}");
                holder.transform.SetParent(g, false);
                // a few waves are still a smooth shape: a ribbon over a low curtain; then the curtain at full height
                bool smooth = added <= 12;
                curtainUnderRibbon = smooth;
                DrawNumberWave(holder.transform, "divisor", DivisorDisplay(s, x0, dx), x0, dx, smooth ? height * .4f : height, Vector3.zero, added == terms, smooth);
                curtainUnderRibbon = false;
                Board(added == 1 ? "Voronoi's first wave:  two crests per turn" : $"{added:N0} of Voronoi's waves");
            }

            time += Time.deltaTime;
            yield return null;
            if (holder == null) yield break; // the stage was cleared under us
        }
    }

    /// <summary>
    /// The divisors of n marked on the wave `kind` (or the spiral): a ring and a label at each, popping up one after
    /// another in <paramref name="seconds"/>, smallest first (in the callouts group, so exp:uncall clears them).
    /// </summary>
    public void ExpMarkDivisors(long n, string kind, Color col, float seconds) =>
        ExpMarkDivisorsAt(n, d => d < 2 ? ExpNumberAt(1, .5f) + NumberWaveOffset(kind) : NumberWaveTop(d, kind), col, seconds);

    /// <summary>The divisors of n marked at the points <paramref name="at"/> gives them, one after another.</summary>
    public void ExpMarkDivisorsAt(long n, System.Func<long, Vector3> at, Color col, float seconds)
    {
        StartCoroutine(MarkDivisorsRoutine(n, at, col, seconds));
    }

    IEnumerator MarkDivisorsRoutine(long n, System.Func<long, Vector3> where, Color col, float seconds)
    {
        Transform g = CalloutRoot();
        List<long> ds = new();
        for (long d = 1; d <= n; d++) if (n % d == 0) ds.Add(d);
        Camera cam = CalloutCamera();
        Vector3 right = cam != null ? cam.transform.right : Vector3.right, up = cam != null ? cam.transform.up : Vector3.up;
        Vector3 back = cam != null ? -cam.transform.forward : Vector3.back;
        float per = seconds / ds.Count;
        foreach (long d in ds)
        {
            if (g == null) yield break; // cleared under us
            Vector3 at = where(d);
            float dist = cam != null ? Mathf.Max(1f, Vector3.Distance(cam.transform.position, at)) : 100f;
            LineRenderer ring = Factory.NewLine($"divisor ring {d}", dist * .0028f, Color.white, AttentionIntensity * 1.3f);
            ring.transform.SetParent(g, false);
            ring.positionCount = 41;
            ring.SetPositions(FacingRing(at + back * (dist * .004f), dist * .011f, right, up));
            TextBox label = ExpLabelWide(g, d.ToString(), at + up * (dist * .036f) + back * (dist * .01f), dist * .55f, col);
            Outline(label.TextField);
            OnTop(ring, 30);
            OnTop(label.TextField.GetComponent<Renderer>(), 31);
            StartCoroutine(SettleRoutine(ring, col, .8f));
            float t = 0f;
            while (t < per)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }
    }

    /// <summary>A line blooms white, then settles to its colour.</summary>
    IEnumerator SettleRoutine(LineRenderer l, Color col, float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            if (l == null) yield break;
            float u = t / seconds;
            l.startColor = l.endColor = Color.Lerp(Color.white, col, u);
            Factory.SetIntensity(l, Mathf.Lerp(AttentionIntensity * 1.3f, Factory.GlowIntensity, u));
            t += Time.deltaTime;
            yield return null;
        }
    }
}
