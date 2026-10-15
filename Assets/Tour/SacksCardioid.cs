using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Sections DA–DD (docs/experiments/README.md): exponential sequences on circles, their centroids, the Fermat test as a
/// spiral, and the Sacks spiral wrapped round a cone.
///
/// DA. The doubling cardioid: p points on a circle, the chords k → 2k mod p (their envelope is a cardioid), and the
///     exponential sequence 2^k mod p walking those chords. For p = 163 it visits every nonzero residue; its even steps are
///     the quadratic residues, and their sum s satisfies s² + s + 41 = 0: the centroid of the even steps is a root of Euler's
///     polynomial divided by 81. For every prime p ≡ 3 (mod 4) the same sum is a root of x² + x + (p + 1)/4.
/// DB. The Fermat spiral: n at radius R√n and angle 2π(2^(n − 1) mod n)/n, so n sits at the end of the exponential walk
///     on its own ring. Every prime lands at residue 1, on the lituus r²θ = 2πR²; the composites that land there too are
///     the base-2 pseudoprimes. Composites mp with a small cofactor m land at residue 2^(m − 1) + pj, on rays j/m.
/// DC. The Fermat test in bases 2, 3, 5, 7 stacked as layers: the primes stand in one wall through all of them; a Carmichael
///     number leaves the wall only in the layers of its own prime factors, and 29341 = 13·37·61 never leaves it.
/// DD. The Sacks spiral wrapped twice round a cone of half-angle 30°: the squares side and the pronic side land on one
///     generator, and every k² + Bk + C converges onto it at arc offset −πRΔ/4: one comb ordered by discriminant.
/// </summary>
public partial class SacksSpiral
{
    public const float CardioidOffset = 26500f;
    public const float CentroidGridOffset = 27300f;
    public const float FermatSpiralOffset = 28300f;
    public const float FermatStackOffset = 29300f;
    public const float ConeCombOffset = 30300f;

    static readonly Color ResidueGreen = new(.35f, 1f, .55f);
    static readonly Color NonResidueBlue = new(.45f, .65f, 1f);

    /// <summary>b^e mod m for m below 3·10⁹ (every product fits in a long); 1 % m so that m = 1 gives 0.</summary>
    static long ModPowSmall(long b, long e, long m)
    {
        long r = 1 % m;
        b %= m;
        while (e > 0)
        {
            if ((e & 1) == 1) r = r * b % m;
            b = b * b % m;
            e >>= 1;
        }

        return r;
    }

    static Vector3 OnCircle(Vector3 c, float radius, long a, long n) =>
        c + new Vector3(Mathf.Cos(2f * Mathf.PI * a / n), Mathf.Sin(2f * Mathf.PI * a / n), 0f) * radius;

    // ================================================================================================
    // DA. the doubling cardioid and its centroid
    // ================================================================================================

    /// <summary>
    /// One circle of n points with the chords k → g·k mod n (faint), the walk 1, g, g², … mod n until it returns to 1 (even
    /// steps green, odd steps blue for a prime; one colour for a composite) and the centroids of its even and odd steps
    /// (or of the whole walk). Returns the centroids.
    /// </summary>
    (Vector3 even, Vector3 odd, Vector3 all, int length) ExponentialWalk(Transform g, Vector3 c, long n, long gen, float radius, float dot, bool prime)
    {
        List<(Vector3, Vector3)> chords = new();
        for (long k = 1; k < n; k++)
        {
            long t = k * gen % n;
            if (t != k) chords.Add((OnCircle(c, radius, k, n), OnCircle(c, radius, t, n)));
        }

        Segments(g, $"times {gen} chords mod {n}", chords, radius * .0035f, new Color(.42f, .45f, .6f) * .55f);
        RingAround(g, $"circle {n}", c, Vector3.back, radius, new Color(.6f, .62f, .75f) * .6f, radius * .004f, Factory.NoGlow);
        List<Vector3> walk = new();
        long x = 1;
        do { walk.Add(OnCircle(c, radius, x, n)); x = x * gen % n; } while (x != 1 && walk.Count < n);
        List<(Vector3, Vector3)> evenSeg = new(), oddSeg = new();
        for (int i = 0; i < walk.Count; i++) (i % 2 == 0 ? evenSeg : oddSeg).Add((walk[i], walk[(i + 1) % walk.Count]));
        Color walkEven = prime ? ResidueGreen : new Color(1f, .45f, .4f), walkOdd = prime ? NonResidueBlue : new Color(1f, .45f, .4f);
        Segments(g, $"walk mod {n} (from even steps)", evenSeg, radius * .008f, walkEven * .8f, Factory.SoftGlowIntensity);
        Segments(g, $"walk mod {n} (from odd steps)", oddSeg, radius * .008f, walkOdd * .8f, Factory.SoftGlowIntensity);
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        Vector3 se = Vector3.zero, so = Vector3.zero, sa = Vector3.zero;
        int ne = 0, no = 0;
        for (int i = 0; i < walk.Count; i++)
        {
            pts.Add(walk[i]); cols.Add(i % 2 == 0 ? walkEven : walkOdd); sizes.Add(dot);
            sa += walk[i];
            if (i % 2 == 0) { se += walk[i]; ne++; } else { so += walk[i]; no++; }
        }

        ExpPointCloud(g, $"walk points mod {n}", pts, cols, sizes, Factory.GlowIntensity);
        return (ne > 0 ? se / ne : c, no > 0 ? so / no : c, sa / walk.Count, walk.Count);
    }

    /// <summary>
    /// The circle of p points, the cardioid of the doubling chords, the walk 2^k mod p and the centroids of its even and
    /// odd steps, with a magnified panel of the centre (×zoom) at <paramref name="panel"/> showing where they fall.
    /// </summary>
    public void ExpDoublingCardioid(Vector3 o, int p, int gen, float radius, Vector3 panel, float zoom, float labelSize, string poly)
    {
        Transform g = ExpGroup("DA doubling cardioid").transform;
        var (ce, co, _, len) = ExponentialWalk(g, o, p, gen, radius, radius * .012f, true);
        RingAround(g, "residue 0", OnCircle(o, radius, 0, p), Vector3.back, radius * .016f, new Color(.7f, .7f, .75f), radius * .003f, Factory.NoGlow);
        ExpDot(g, "centroid of the even steps", ce + Vector3.back * .3f, radius * .022f, ResidueGreen, AttentionIntensity);
        ExpDot(g, "centroid of the odd steps", co + Vector3.back * .3f, radius * .022f, NonResidueBlue, AttentionIntensity);
        // the magnified centre
        float half = (ce - o).magnitude * 1.5f;
        Vector3 P(Vector3 world) => panel + (world - o) * zoom;
        List<(Vector3, Vector3)> frame = new()
        {
            (o + new Vector3(-half, -half, 0f), o + new Vector3(half, -half, 0f)), (o + new Vector3(half, -half, 0f), o + new Vector3(half, half, 0f)),
            (o + new Vector3(half, half, 0f), o + new Vector3(-half, half, 0f)), (o + new Vector3(-half, half, 0f), o + new Vector3(-half, -half, 0f)),
        };
        List<(Vector3, Vector3)> panelFrame = new();
        foreach (var (a, b) in frame) panelFrame.Add((P(a), P(b)));
        Segments(g, "zoom box", frame, radius * .003f, Color.white * .5f);
        Segments(g, "zoom panel", panelFrame, radius * .004f, Color.white * .6f);
        Segments(g, "zoom leaders", new List<(Vector3, Vector3)> { (frame[0].Item2, panelFrame[0].Item1), (frame[1].Item2, panelFrame[2].Item1) }, radius * .002f, Color.white * .35f);
        Segments(g, "panel axes", new List<(Vector3, Vector3)> { (P(o + Vector3.left * half), P(o + Vector3.right * half)), (P(o + Vector3.down * half), P(o + Vector3.up * half)) },
            radius * .003f, new Color(.6f, .62f, .75f) * .7f);
        ExpDot(g, "panel centroid even", P(ce) + Vector3.back * .3f, radius * .03f, ResidueGreen, AttentionIntensity);
        ExpDot(g, "panel centroid odd", P(co) + Vector3.back * .3f, radius * .03f, NonResidueBlue, AttentionIntensity);
        ExpDot(g, "panel centre", panel + Vector3.back * .3f, radius * .016f, Color.white, Factory.GlowIntensity);
        // the circle |z| = sqrt((p + 1)/4) / ((p − 1)/2) through both centroids
        RingAround(g, "panel ring", panel, Vector3.back, (ce - o).magnitude * zoom, new Color(.8f, .8f, .9f) * .4f, radius * .003f, Factory.NoGlow);
        if (labelSize <= 0f) return;
        float k = (p - 1) / 2f;
        ExpLabelWide(g, $"(−1 + i√{p}) / {p - 1}", P(ce) + new Vector3(radius * .06f, radius * .03f, -.3f), labelSize * .42f, ResidueGreen, TextAlignmentOptions.Left);
        ExpLabelWide(g, $"(−1 − i√{p}) / {p - 1}", P(co) + new Vector3(radius * .06f, -radius * .03f, -.3f), labelSize * .42f, NonResidueBlue, TextAlignmentOptions.Left);
        ExpLabelWide(g, $"times {k:0}: the roots of  {poly} = 0", panel + new Vector3(0f, -half * zoom + radius * .09f, -.3f), labelSize * .42f, Color.white);
        ExpLabelWide(g, $"the centre, magnified {zoom:0}×", panel + new Vector3(0f, half * zoom + radius * .08f, -.3f), labelSize * .38f, new Color(.75f, .78f, .9f));
        ExpLabelWide(g, $"{gen}<sup>k</sup> mod {p}:  {len} steps through every nonzero residue", o + new Vector3(0f, radius * 1.14f, -.3f), labelSize * .42f, Color.white);
        ExpLabelWide(g, $"green: even steps, the squares mod {p}    blue: odd steps", o + new Vector3(0f, -radius * 1.14f, -.3f), labelSize * .36f, new Color(.75f, .78f, .9f));
    }

    /// <summary>
    /// Small multiples: for each (n, g) the circle, the times-g chords and the walk g^k mod n with its centroids. Primes
    /// show the centroid of their even steps (a root of x² + x + (p + 1)/4 divided by (p − 1)/2); composites the centroid of
    /// their whole walk, which closes early.
    /// </summary>
    public void ExpCentroidGrid(Vector3 o, (long n, long g, string title, string note)[] cases, int columns, float spacingX, float spacingY, float radius, float labelSize, string header = null)
    {
        Transform g = ExpGroup("DA centroid grid").transform;
        ExpEnsureSieve(10000);
        int rows = (cases.Length + columns - 1) / columns;
        if (labelSize > 0f && header != null)
        {
            string[] lines = header.Split('|');
            for (int i = 0; i < lines.Length; i++)
                ExpLabelWide(g, lines[i], o + new Vector3(0f, (rows - 1) * .5f * spacingY + radius * 1.42f - i * labelSize * .042f, -.3f), labelSize * .36f, i == 0 ? Color.white : new Color(.8f, .83f, .93f));
        }
        for (int i = 0; i < cases.Length; i++)
        {
            var cs = cases[i];
            Vector3 c = o + new Vector3((i % columns - (columns - 1) * .5f) * spacingX, ((rows - 1) * .5f - i / columns) * spacingY, 0f);
            bool prime = ExpPrime(cs.n);
            var (ce, co, ca, len) = ExponentialWalk(g, c, cs.n, cs.g, radius, radius * .028f, prime);
            if (prime)
            {
                ExpDot(g, $"centroid even {cs.n}", ce + Vector3.back * .3f, radius * .06f, ResidueGreen, AttentionIntensity);
                ExpDot(g, $"centroid odd {cs.n}", co + Vector3.back * .3f, radius * .06f, NonResidueBlue, AttentionIntensity);
            }
            else ExpDot(g, $"centroid {cs.n}", ca + Vector3.back * .3f, radius * .07f, Color.white, AttentionIntensity);
            ExpDot(g, $"centre {cs.n}", c + Vector3.back * .2f, radius * .025f, new Color(.7f, .7f, .78f), Factory.NoGlow);
            if (labelSize <= 0f) continue;
            ExpLabelWide(g, cs.title, c + new Vector3(0f, -radius * 1.24f, -.3f), labelSize * .42f, prime ? PrimeGold : new Color(1f, .5f, .45f));
            ExpLabelWide(g, cs.note, c + new Vector3(0f, -radius * 1.5f, -.3f), labelSize * .34f, new Color(.8f, .83f, .93f));
        }
    }

    // ================================================================================================
    // DB. the Fermat spiral
    // ================================================================================================

    /// <summary>n's point on the Fermat spiral of base a: radius R√n, angle 2π(a^(n − 1) mod n)/n.</summary>
    static Vector3 FermatAt(Vector3 o, long n, long a, float R, out long residue)
    {
        residue = ModPowSmall(a, n - 1, n);
        double th = 2 * System.Math.PI * residue / n, r = R * System.Math.Sqrt(n);
        return o + new Vector3((float)(r * System.Math.Cos(th)), (float)(r * System.Math.Sin(th)), 0f);
    }

    /// <summary>
    /// The Fermat spiral of base a up to N: primes gold, composites dim, the pseudoprimes (composites at residue 1) ringed
    /// red; the lituses r²θ = 2πR²c for the residues c in <paramref name="lituus"/> drawn as thin curves.
    /// </summary>
    public void ExpFermatSpiral(Vector3 o, int N, long a, float R, float dot, float labelSize, long[] lituus, long[] named, float rMin = 0f, float rMax = 1e9f, float labelOffset = 3f,
        float[] lituusLabelX = null, string[] lituusNames = null)
    {
        Transform g = ExpGroup("DB Fermat spiral").transform;
        ExpEnsureSieve(N + 10);
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        List<long> pseudo = new();
        for (long n = 3; n <= N; n++)
        {
            Vector3 p = FermatAt(o, n, a, R, out long res);
            float rr = (p - o).magnitude;
            if (rr < rMin || rr > rMax) continue;
            bool prime = ExpPrime(n);
            if (!prime && res == 1) pseudo.Add(n);
            pts.Add(p);
            cols.Add(prime ? PrimeGold * .95f : res == 0 ? new Color(.8f, .8f, .85f) : Color.Lerp(new Color(.25f, .35f, .6f), new Color(.5f, .7f, 1f), Mathf.Clamp01(1f - Mathf.Log(res + 1) / 9f)));
            sizes.Add(prime ? dot : dot * .7f);
        }

        ExpPointCloud(g, "Fermat spiral", pts, cols, sizes, Factory.SoftGlowIntensity);
        foreach (long c in lituus)
        {
            List<Vector3> curve = new();
            for (int i = 0; i <= 400; i++)
            {
                double r = Mathf.Max(rMin, R * 3f) + (System.Math.Min(rMax, R * System.Math.Sqrt(N)) - System.Math.Max(rMin, R * 3f)) * i / 400.0;
                double th = 2 * System.Math.PI * R * R * c / (r * r);
                if (th > System.Math.PI) continue;
                curve.Add(o + new Vector3((float)(r * System.Math.Cos(th)), (float)(r * System.Math.Sin(th)), -.1f));
            }

            if (curve.Count > 1) ExpLine(g, $"lituus {c}", dot * .25f, (c == 1 ? PrimeGold : new Color(.6f, .75f, 1f)) * .55f, Factory.NoGlow, curve.ToArray());
            int li = System.Array.IndexOf(lituus, c);
            if (labelSize > 0f && lituusLabelX != null && li < lituusLabelX.Length)
            {
                double x = lituusLabelX[li], th = 2 * System.Math.PI * R * R * c / (x * x);
                Vector3 at = o + new Vector3((float)(x * System.Math.Cos(th)), (float)(x * System.Math.Sin(th)), -.3f);
                ExpLabelWide(g, lituusNames[li], at + new Vector3(0f, labelSize * .03f, 0f), labelSize * .3f, c == 1 ? PrimeGold : new Color(.7f, .82f, 1f));
            }
        }

        foreach (long n in pseudo)
        {
            Vector3 p = FermatAt(o, n, a, R, out _);
            RingAround(g, $"pseudoprime {n}", p + Vector3.back * .4f, Vector3.back, dot * 2.6f, new Color(1f, .3f, .28f), dot * .35f, AttentionIntensity);
        }

        if (labelSize <= 0f) return;
        int shown = 0;
        foreach (long n in named)
        {
            Vector3 p = FermatAt(o, n, a, R, out long res);
            string f = string.Join("·", Factorize(n));
            float level = new[] { -1f, -2.1f, -3.2f, -4.3f }[shown % 4];
            Vector3 at = p + new Vector3(0f, level * labelOffset, -.4f);
            ExpLine(g, $"leader {n}", dot * .2f, new Color(1f, .4f, .38f) * .6f, Factory.NoGlow, new[] { p + Vector3.back * .4f, at + new Vector3(0f, labelOffset * .3f, 0f) });
            ExpLabelWide(g, $"{n} = {f}", at, labelSize * .36f, new Color(1f, .45f, .42f));
            shown++;
        }
    }

    // ================================================================================================
    // DC. the Fermat test in four bases, stacked
    // ================================================================================================

    /// <summary>
    /// One layer per base, stacked towards the viewer (−z), each the Fermat spiral of that base up to N with primes gold and
    /// composites dim; the primes' wall (segments
    /// through every layer at the primes' positions, which do not depend on the base); the composites at residue 1 in each
    /// layer ringed red; the composites at residue 1 in every layer drawn as white columns; and the columns of
    /// <paramref name="broken"/>, which leave the wall in some layers.
    /// </summary>
    public void ExpFermatStack(Vector3 o, int N, long[] bases, float R, float gap, float dot, float labelSize, long[] broken, float wedgeDeg = 40f)
    {
        Transform g = ExpGroup("DC Fermat stack").transform;
        ExpEnsureSieve(N + 10);
        Vector3 Layer(int i) => o + Vector3.back * (gap * i);
        // each layer is the Fermat spiral mirrored (angle −2πr/n), so its fan sticks out in front of the wall (−y);
        // only the fan within wedgeDeg of the ray is drawn
        Vector3 Shelf(int li, long n, out long res)
        {
            Vector3 p = FermatAt(Layer(li), n, bases[li], R, out res);
            p.y = Layer(li).y - (p.y - Layer(li).y);
            return p;
        }

        float wedge = wedgeDeg * Mathf.Deg2Rad;
        for (int li = 0; li < bases.Length; li++)
        {
            List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
            for (long n = 3; n <= N; n++)
            {
                Vector3 p = Shelf(li, n, out long res);
                if (2 * System.Math.PI * res / n > wedge) continue;
                bool prime = ExpPrime(n);
                if (!prime && res == 1)
                {
                    pts.Add(p); cols.Add(new Color(1f, .3f, .28f)); sizes.Add(dot * 2.2f);
                    continue;
                }

                pts.Add(p); cols.Add(prime ? PrimeGold * .8f : new Color(.13f, .17f, .3f)); sizes.Add(prime ? dot : dot * .5f);
            }

            ExpPointCloud(g, $"layer base {bases[li]}", pts, cols, sizes, Factory.SoftGlowIntensity);
            float rim = R * Mathf.Sqrt(N);
            List<Vector3> arc = new();
            for (int i = 0; i <= 32; i++) arc.Add(Layer(li) + new Vector3(Mathf.Cos(wedge * i / 32f), -Mathf.Sin(wedge * i / 32f), 0f) * rim * 1.02f);
            arc.Add(Layer(li));
            ExpLine(g, $"rim {bases[li]}", dot * .3f, new Color(.5f, .55f, .7f) * .5f, Factory.NoGlow, arc.ToArray());
            if (labelSize > 0f) ExpLabelWide(g, $"base {bases[li]}", Layer(li) + new Vector3(-labelSize * .04f, 0f, 0f), labelSize * .42f, new Color(.8f, .83f, .95f), TextAlignmentOptions.Right);
        }

        // the prime wall: each prime's column through every layer
        List<(Vector3, Vector3)> wall = new();
        int every = 0;
        for (long n = 3; n <= N; n++)
            if (ExpPrime(n) && every++ % 4 == 0)
            {
                Vector3 p = Shelf(0, n, out _);
                wall.Add((p, p + Vector3.back * (gap * (bases.Length - 1))));
            }

        Segments(g, "prime wall", wall, dot * .16f, PrimeGold * .3f);
        // composites at residue 1 in every layer: white columns
        for (long n = 9; n <= N; n += 2)
        {
            if (ExpPrime(n)) continue;
            bool all = true;
            foreach (long b in bases) if (ModPowSmall(b, n - 1, n) != 1) { all = false; break; }
            if (!all) continue;
            Vector3 p = Shelf(0, n, out _);
            ExpLine(g, $"column {n}", dot * .8f, Color.white, AttentionIntensity, new[] { p + Vector3.forward * gap * .3f, p + Vector3.back * (gap * (bases.Length - 1 + .3f)) });
            if (labelSize > 0f)
                ExpLabelWide(g, $"{n} = {string.Join("·", Factorize(n))}:  in every layer", p + Vector3.back * (gap * (bases.Length - 1) + gap * .3f) + Vector3.right * (labelSize * .03f),
                    labelSize * .38f, Color.white, TextAlignmentOptions.Left);
        }

        int bi = 0;
        foreach (long n in broken)
        {
            List<(Vector3, Vector3)> segs = new();
            for (int li = 0; li < bases.Length; li++)
            {
                Vector3 p = Shelf(li, n, out long res);
                if (res == 1) segs.Add((p + Vector3.forward * gap * .12f, p + Vector3.back * gap * .12f));
            }

            Segments(g, $"broken column {n}", segs, dot * 1.2f, new Color(1f, .4f, .38f), AttentionIntensity);
            if (labelSize > 0f)
            {
                List<string> off = new();
                foreach (long b in bases) if (ModPowSmall(b, n - 1, n) != 1) off.Add(b.ToString());
                Vector3 foot = Shelf(0, n, out _), top = foot + Vector3.back * (gap * (bases.Length - 1) + gap * (.45f + .3f * bi));
                ExpLine(g, $"leader {n}", dot * .3f, new Color(1f, .45f, .4f) * .5f, Factory.NoGlow, new[] { foot + Vector3.back * (gap * (bases.Length - 1)), top - Vector3.back * gap * .06f });
                ExpLabelWide(g, $"{n} = {string.Join("·", Factorize(n))}:  off in base {string.Join(", ", off)}", top, labelSize * .34f, new Color(1f, .5f, .45f), TextAlignmentOptions.Left);
            }

            bi++;
        }
    }

    // ================================================================================================
    // DD. the Sacks spiral wrapped round a cone
    // ================================================================================================

    /// <summary>
    /// n on the cone of half-angle asin(1/m) about world +y (apex at o): slant distance R√n, azimuth m·2π√n (the plane
    /// wrapped m times round the cone, which is locally an isometry).
    /// </summary>
    static Vector3 ConeAt(Vector3 o, double n, float R, int m)
    {
        double s = R * System.Math.Sqrt(n), phi = m * 2 * System.Math.PI * System.Math.Sqrt(n);
        double rr = s / m, h = s * System.Math.Sqrt(1 - 1.0 / (m * m));
        return o + new Vector3((float)(rr * System.Math.Sin(phi)), (float)h, (float)(-rr * System.Math.Cos(phi)));
    }

    /// <summary>
    /// The Sacks spiral up to N wrapped m times round a cone (primes gold), the wire cone, the generator at azimuth 0
    /// (towards the viewer, −z) and the strands k² + Bk + C of <paramref name="strands"/> (red when Δ is a square,
    /// grey when always even, gold otherwise), each labelled with its discriminant at the rim.
    /// </summary>
    public void ExpConeSpiral(Vector3 o, int N, float R, int m, (int B, int C)[] strands, float dot, float labelSize, int kFrom = 1, bool numbers = true, bool wire = true)
    {
        Transform g = ExpGroup("DD cone spiral").transform;
        ExpEnsureSieve(N + 4000);
        float sMax = R * Mathf.Sqrt(N);
        if (numbers)
        {
            List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
            for (long n = 2; n <= N; n++)
            {
                bool p = ExpPrime(n);
                pts.Add(ConeAt(o, n, R, m)); cols.Add(p ? PrimeGold * .9f : NumberDim); sizes.Add(p ? dot : dot * .55f);
            }

            ExpPointCloud(g, "numbers on the cone", pts, cols, sizes, Factory.SoftGlowIntensity);
        }

        if (wire)
        {
            List<(Vector3, Vector3)> w = new();
            float sinA = 1f / m, cosA = Mathf.Sqrt(1f - sinA * sinA);
            for (int i = 0; i < 24; i++)
            {
                float a = i / 24f * Mathf.PI * 2f;
                w.Add((o, o + new Vector3(sMax * sinA * Mathf.Sin(a), sMax * cosA, -sMax * sinA * Mathf.Cos(a))));
            }

            for (int ring = 1; ring <= 4; ring++)
            {
                float s = sMax * ring / 4f;
                for (int i = 0; i < 96; i++)
                {
                    float a0 = i / 96f * Mathf.PI * 2f, a1 = (i + 1) / 96f * Mathf.PI * 2f;
                    w.Add((o + new Vector3(s * sinA * Mathf.Sin(a0), s * cosA, -s * sinA * Mathf.Cos(a0)), o + new Vector3(s * sinA * Mathf.Sin(a1), s * cosA, -s * sinA * Mathf.Cos(a1))));
                }
            }

            Segments(g, "cone wire", w, dot * .12f, YarnColor * .45f);
            ExpLine(g, "generator", dot * .3f, SquareColor * .7f, Factory.SoftGlowIntensity, new[] { o, o + new Vector3(0f, cosA, -sinA) * sMax });
        }

        foreach (var (B, C) in strands)
        {
            int D = B * B - 4 * C;
            bool square = D >= 0 && IsSquareLong(D), even = B % 2 != 0 && C % 2 == 0; // k(k + B) is even when B is odd
            Color col = square ? DeadColor : even ? new Color(.55f, .55f, .62f) : PrimeGold;
            List<Vector3> path = new(), pts = new(); List<Color> cols = new(); List<float> sizes = new();
            for (long k = kFrom; ; k++)
            {
                long n = k * k + B * k + C;
                if (n > N) break;
                if (n < 2) continue;
                Vector3 p = ConeAt(o, n, R, m);
                path.Add(p); pts.Add(p);
                bool pr = ExpPrime(n);
                cols.Add(pr ? PrimeGold : square ? DeadColor : new Color(.45f, .45f, .52f)); sizes.Add(pr ? dot * 1.5f : dot * 1.2f);
            }

            if (path.Count > 1) ExpLine(g, $"strand Δ = {D}", dot * .22f, col * .6f, Factory.SoftGlowIntensity, path.ToArray());
            ExpPointCloud(g, $"strand Δ = {D} values", pts, cols, sizes, Factory.GlowIntensity);
            if (labelSize > 0f && path.Count > 1 && D == -163)
                ExpLabelWide(g, "Euler's k² + k + 41 (Δ = −163)", path[^1] + new Vector3(0f, labelSize * .05f, 0f), labelSize * .3f, col);
        }

        if (labelSize > 0f)
        {
            long kTop = (long)System.Math.Floor(System.Math.Sqrt(N));
            Vector3 top = ConeAt(o, kTop * kTop, R, m);
            ExpLabelWide(g, "the squares and the pronic numbers:", top + new Vector3(labelSize * .04f, labelSize * .068f, 0f), labelSize * .3f, Color.white, TextAlignmentOptions.Left);
            ExpLabelWide(g, "one generator", top + new Vector3(labelSize * .04f, labelSize * .034f, 0f), labelSize * .3f, Color.white, TextAlignmentOptions.Left);
            ExpLabelWide(g, "every k² + Bk + C beside it at −πRΔ/4", top + new Vector3(labelSize * .04f, 0f, 0f), labelSize * .27f, new Color(.8f, .83f, .93f), TextAlignmentOptions.Left);
        }
    }

    /// <summary>A few lines of legend at a world position (one line per entry, stepping by <paramref name="step"/>).</summary>
    public void ExpWorldLegend(Vector3 at, Vector3 step, string[] lines, float size, Color color, bool rightAligned = false)
    {
        // (no TextMeshPro type in the signature: the test assembly does not reference it)
        Transform g = ExpGroup("legend").transform;
        for (int i = 0; i < lines.Length; i++)
            ExpLabelWide(g, lines[i], at + step * i, size, color, rightAligned ? TextAlignmentOptions.Right : TextAlignmentOptions.Left);
    }

    /// <summary>The factors of k² + Bk + C when its discriminant is a square, e.g. (k − 2)(k + 2).</summary>
    static string SquareFactors(int B, int C)
    {
        int d = (int)System.Math.Round(System.Math.Sqrt(B * B - 4 * C));
        int r1 = (B + d) / 2, r2 = (B - d) / 2; // k² + Bk + C = (k + r1)(k + r2)
        string F(int r) => r == 0 ? "k" : r > 0 ? $"(k + {r})" : $"(k − {-r})";
        return r1 == r2 ? $"{F(r1)}²" : F(r2) + F(r1);
    }

    /// <summary>
    /// The comb on the cone (m = 2) seen face-on: the strip of the cone within <paramref name="halfWidth"/> of the generator
    /// between slant distances s0 and s1, unrolled flat (arc offset across, slant distance up), with every number in it
    /// (primes gold) and the strands k² + Bk + C laid over it, labelled by Δ and coloured by kind.
    /// </summary>
    public void ExpConeComb(Vector3 o, float R, float s0, float s1, float halfWidth, (int B, int C, string name)[] strands, float dot, float labelSize)
    {
        Transform g = ExpGroup("DD cone comb").transform;
        long n0 = (long)System.Math.Floor((s0 / R) * (s0 / R)), n1 = (long)System.Math.Ceiling((s1 / R) * (s1 / R));
        ExpEnsureSieve(n1 + 10);
        // unrolled coordinates: x = arc offset from the generator on the m = 2 cone, y = slant distance
        Vector3 At(double n)
        {
            double s = R * System.Math.Sqrt(n), phi = 4 * System.Math.PI * System.Math.Sqrt(n);
            phi = (phi + System.Math.PI) % (2 * System.Math.PI) - System.Math.PI;
            return o + new Vector3((float)(s - (s0 + s1) / 2), (float)(s / 2 * phi), 0f);
        }

        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        for (long n = n0; n <= n1; n++)
        {
            Vector3 p = At(n);
            if (Mathf.Abs(p.y - o.y) > halfWidth) continue;
            bool pr = ExpPrime(n);
            pts.Add(p); cols.Add(pr ? PrimeGold * .85f : NumberDim * .9f); sizes.Add(pr ? dot : dot * .6f);
        }

        ExpPointCloud(g, "numbers near the generator", pts, cols, sizes, Factory.SoftGlowIntensity);
        foreach (var (B, C, name) in strands)
        {
            int D = B * B - 4 * C;
            bool square = D >= 0 && IsSquareLong(D), even = B % 2 != 0 && C % 2 == 0;
            Color col = square ? DeadColor : even ? new Color(.55f, .55f, .62f) : ResidueGreen;
            List<Vector3> path = new(), sp = new(); List<Color> sc = new(); List<float> ss = new();
            int primes = 0, total = 0;
            for (long k = 1; ; k++)
            {
                long n = k * k + B * k + C;
                if (n > n1) break;
                if (n < n0) continue;
                Vector3 p = At(n) + Vector3.back * .3f;
                if (Mathf.Abs(p.y - o.y) > halfWidth) continue;
                bool pr = ExpPrime(n);
                total++; if (pr) primes++;
                path.Add(p); sp.Add(p); sc.Add(pr ? PrimeGold : square ? DeadColor : new Color(.5f, .5f, .58f)); ss.Add(pr ? dot * 1.6f : dot * 1.2f);
            }

            if (path.Count > 1) ExpLine(g, $"comb strand Δ = {D}", dot * .3f, col * .55f, Factory.SoftGlowIntensity, path.ToArray());
            ExpPointCloud(g, $"comb strand Δ = {D} values", sp, sc, ss, Factory.GlowIntensity);
            if (labelSize > 0f && path.Count > 1)
            {
                // the squares side (Δ ≡ 0 mod 4) labelled on the left, the pronic side (Δ ≡ 1 mod 4) on the right
                bool left = ((D % 4) + 4) % 4 == 0;
                Vector3 end = left ? path[0] : path[^1];
                ExpLabelWide(g, left ? $"{(square ? SquareFactors(B, C) + "   " : "")}{D}" : $"{D}{(square ? "   " + SquareFactors(B, C) : even ? "   even" : "")}",
                    end + new Vector3((left ? -1f : 1f) * labelSize * .03f, 0f, -.3f), labelSize * .3f, col, left ? TextAlignmentOptions.Right : TextAlignmentOptions.Left);
            }
        }

        if (labelSize <= 0f) return;
        Vector3 lh = o + new Vector3(-(s1 - s0) / 2 - labelSize * .03f, halfWidth + labelSize * .1f, -.3f), rh = o + new Vector3((s1 - s0) / 2 + labelSize * .03f, halfWidth + labelSize * .1f, -.3f);
        ExpLabelWide(g, "k² + C", lh, labelSize * .32f, Color.white, TextAlignmentOptions.Right);
        ExpLabelWide(g, "Δ ≡ 0 (mod 4)", lh - new Vector3(0f, labelSize * .035f, 0f), labelSize * .26f, new Color(.75f, .78f, .9f), TextAlignmentOptions.Right);
        ExpLabelWide(g, "k² + k + C", rh, labelSize * .32f, Color.white, TextAlignmentOptions.Left);
        ExpLabelWide(g, "Δ ≡ 1 (mod 4)", rh - new Vector3(0f, labelSize * .035f, 0f), labelSize * .26f, new Color(.75f, .78f, .9f), TextAlignmentOptions.Left);
        ExpLabelWide(g, "gold: primes     red: Δ a square, the strand factors     grey: always even", o + new Vector3(0f, -halfWidth - labelSize * .07f, -.3f), labelSize * .28f, new Color(.75f, .78f, .9f));
    }
}
