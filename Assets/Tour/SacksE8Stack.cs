using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Sections EG–EI (docs/experiments/README.md): the Coxeter wheels of E6, E7 and E8 stacked, the spokes' multiplication
/// (why the first impostors are 7·7, 7·11, 7·13), the wheel bent into confocal parabolas, and the twin-prime saddles.
///
/// The wheel of a lattice with Coxeter number h has h hours; its spokes are the units mod h (h = 12, 18, 30 for E6, E7, E8).
/// 12, 18 and 30 are three of the ten numbers whose units are all prime (1, 2, 3, 4, 6, 8, 12, 18, 24, 30), so the first loop
/// of each wheel is clean and the first impostor is the square of the smallest prime not dividing h: 25 on E6 and E7, 49 on E8.
/// Every impostor is a product of spoke primes, and its spoke is the product of their spokes mod h.
/// Twin primes straddle the hours 0 and 6 (E6), 0, 6, 12 (E7) and 0, 12, 18 (E8): the ridges of a two-, three- and warped
/// three-fold saddle; on E8 the spokes 7 and 23 run down the valleys, and their primes can never have a twin.
/// </summary>
public partial class SacksSpiral
{
    public const float WheelRowOffset = 38000f;
    public const float WheelStackOffset = 39000f;
    public const float SpokeTableOffset = 40000f;
    public const float OverlapOffset = 41000f;
    public const float ParabolaOffset = 42000f;
    public const float SaddleOffset = 43000f;

    static readonly Color[] FactorColors =
    {
        new(1f, .32f, .3f), new(1f, .62f, .2f), new(1f, .3f, .75f), new(.72f, .45f, 1f), new(.4f, .62f, 1f), new(.3f, .95f, .9f),
        new(.5f, 1f, .45f), new(.95f, .95f, .5f),
    };

    static readonly int[] SmallPrimes = { 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47 };

    static Color FactorColor(long p)
    {
        int i = System.Array.IndexOf(SmallPrimes, (int)p);
        return i >= 0 ? FactorColors[i % FactorColors.Length] : new Color(.8f, .8f, .85f);
    }

    // ================================================================================================
    // the Coxeter systems E6, E7, E8 (the first 6, 7 or 8 of Bourbaki's simple roots of E8)
    // ================================================================================================

    static readonly double[][] E8Simple =
    {
        new[] { .5, -.5, -.5, -.5, -.5, -.5, -.5, .5 }, new double[] { 1, 1, 0, 0, 0, 0, 0, 0 }, new double[] { -1, 1, 0, 0, 0, 0, 0, 0 },
        new double[] { 0, -1, 1, 0, 0, 0, 0, 0 }, new double[] { 0, 0, -1, 1, 0, 0, 0, 0 }, new double[] { 0, 0, 0, -1, 1, 0, 0, 0 },
        new double[] { 0, 0, 0, 0, -1, 1, 0, 0 }, new double[] { 0, 0, 0, 0, 0, -1, 1, 0 },
    };

    static readonly Dictionary<int, (List<double[]> roots, double[] u1, double[] u2, int h)> coxeterCache = new();

    /// <summary>The roots of E6, E7 or E8 (rank 6, 7, 8), its Coxeter number, and an orthonormal basis of its Coxeter plane.</summary>
    static (List<double[]> roots, double[] u1, double[] u2, int h) CoxeterSystem(int rank)
    {
        if (coxeterCache.TryGetValue(rank, out var cached)) return cached;
        // the roots: close the simple roots under their reflections
        List<double[]> roots = new();
        HashSet<string> seen = new();
        string Key(double[] v) { System.Text.StringBuilder sb = new(); foreach (double x in v) sb.Append(System.Math.Round(x * 2)).Append(','); return sb.ToString(); }
        double[] Reflect(double[] v, double[] a)
        {
            double f = 2 * Dot8(v, a) / Dot8(a, a);
            double[] w = new double[8];
            for (int i = 0; i < 8; i++) w[i] = v[i] - f * a[i];
            return w;
        }

        List<double[]> frontier = new();
        for (int i = 0; i < rank; i++) if (seen.Add(Key(E8Simple[i]))) { roots.Add(E8Simple[i]); frontier.Add(E8Simple[i]); }
        while (frontier.Count > 0)
        {
            List<double[]> next = new();
            foreach (double[] v in frontier)
                for (int i = 0; i < rank; i++)
                {
                    double[] w = Reflect(v, E8Simple[i]);
                    if (seen.Add(Key(w))) { roots.Add(w); next.Add(w); }
                }

            frontier = next;
        }

        // the Coxeter element and its order
        double[,] c = new double[8, 8];
        for (int i = 0; i < 8; i++) c[i, i] = 1;
        for (int s = 0; s < rank; s++)
        {
            double[] a = E8Simple[s];
            double aa = Dot8(a, a);
            double[,] r = new double[8, 8];
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++) r[i, j] = (i == j ? 1 : 0) - 2 * a[i] * a[j] / aa;
            c = Mul8(c, r);
        }

        int h = 1;
        double[,] m = c;
        while (h < 100)
        {
            double err = 0;
            for (int i = 0; i < 8; i++) for (int j = 0; j < 8; j++) err = System.Math.Max(err, System.Math.Abs(m[i, j] - (i == j ? 1 : 0)));
            if (err < 1e-9) break;
            m = Mul8(m, c);
            h++;
        }

        // the projector onto the eigenplane of angle 2π/h
        double[,] p = new double[8, 8], ck = new double[8, 8];
        for (int i = 0; i < 8; i++) ck[i, i] = 1;
        for (int k = 0; k < h; k++)
        {
            double w = 2.0 / h * System.Math.Cos(2 * System.Math.PI * k / h);
            for (int i = 0; i < 8; i++) for (int j = 0; j < 8; j++) p[i, j] += w * ck[i, j];
            ck = Mul8(ck, c);
        }

        double[] e = { 1, .3, .7, .2, .9, .4, .6, .8 }, u1 = new double[8], u2 = new double[8];
        for (int i = 0; i < 8; i++) for (int j = 0; j < 8; j++) u1[i] += p[i, j] * e[j];
        double n1 = System.Math.Sqrt(Dot8(u1, u1));
        for (int i = 0; i < 8; i++) u1[i] /= n1;
        for (int i = 0; i < 8; i++) for (int j = 0; j < 8; j++) u2[i] += c[i, j] * u1[j];
        double cth = Dot8(u1, u2);
        for (int i = 0; i < 8; i++) u2[i] -= cth * u1[i];
        double n2 = System.Math.Sqrt(Dot8(u2, u2));
        for (int i = 0; i < 8; i++) u2[i] /= n2;
        var result = (roots, u1, u2, h);
        coxeterCache[rank] = result;
        return result;
    }

    static string LatticeName(int rank) => rank == 6 ? "E6" : rank == 7 ? "E7" : "E8";

    // ================================================================================================
    // the saddle height: ridges at given hours, valleys halfway between
    // ================================================================================================

    /// <summary>
    /// g(θ) for an angle in hours (0 … h): +1 on the ridges, −1 on the valleys halfway between consecutive ridges, a half
    /// cosine in between (so g = cos 2θ for ridges 0, 6 of 12 and cos 3θ for ridges 0, 6, 12 of 18).
    /// </summary>
    static float SaddleShape(float hour, int h, int[] ridges)
    {
        float x = Mathf.Repeat(hour, h);
        for (int i = 0; i < ridges.Length; i++)
        {
            float a = ridges[i], b = i + 1 < ridges.Length ? ridges[i + 1] : ridges[0] + h;
            float xx = x < a ? x + h : x;
            if (xx < a || xx > b) continue;
            float mid = (a + b) * .5f;
            return xx <= mid ? Mathf.Cos(Mathf.PI * (xx - a) / (mid - a)) : -Mathf.Cos(Mathf.PI * (xx - mid) / (b - mid));
        }

        return 0f;
    }

    // ================================================================================================
    // a Coxeter wheel, flat or lifted onto a saddle
    // ================================================================================================

    public class WheelSpec
    {
        public int Rank = 8;
        public float RootScale = 8f;        // the roots' Coxeter-plane projection, scaled
        public int NMax = 600;
        public float From = 15f, To = 33f;  // the spiral of numbers between these radii (linear in n)
        public float Dot = .55f;
        public float LabelSize = 60f;
        public int[] Ridges;                // lift onto the saddle with these ridge hours (null: flat)
        public float Lift = 0f;             // saddle height = −Lift·g·r² (towards the viewer)
        public bool RingImpostors = true;
        public bool TwinLinks;
        public bool Mesh;
        public string Title;
        public long[] Named;                // ring and label these numbers
        public bool HourLabels = true;
        public bool ValleyLabels = true;
        public Vector3 TitleStep = Vector3.down; // screen-down for the title lines (the camera may look at the wheel at a tilt)
        public bool TwinColours;            // spokes that can never hold a twin prime (and their primes) in blue
    }

    static readonly Color NoTwinBlue = new(.45f, .7f, 1f);

    /// <summary>A spoke k of the wheel of h can hold a twin prime when k − 2 or k + 2 is also a unit mod h.</summary>
    static bool TwinSpoke(long k, int h) => Gcd(k, h) == 1 && (Gcd(((k - 2) % h + h) % h, h) == 1 || Gcd((k + 2) % h, h) == 1);

    /// <summary>
    /// One Coxeter wheel: the roots in the Coxeter plane (turned so a root of the outer ring is at the top and mirrored so the
    /// hours run clockwise), the spokes (the units mod h, gold), the numbers 1 … NMax at their hours (primes gold, composites
    /// on a spoke red, the rest dim), the hour labels; lifted onto the saddle when Ridges is set (spokes become parabolas),
    /// with the twin primes linked and a wire mesh of the surface.
    /// </summary>
    public void ExpCoxeterWheel(Vector3 o, WheelSpec w)
    {
        var (roots, u1, u2, h) = CoxeterSystem(w.Rank);
        Transform g = ExpGroup($"{LatticeName(w.Rank)} wheel").transform;
        ExpEnsureSieve(w.NMax + 4);
        Vector2[] p = new Vector2[roots.Count];
        for (int i = 0; i < roots.Count; i++) p[i] = new Vector2((float)Dot8(roots[i], u1), (float)Dot8(roots[i], u2)) * w.RootScale;
        float outer = 0f;
        foreach (Vector2 q in p) outer = Mathf.Max(outer, q.magnitude);
        int baseRoot = -1; float best = float.MaxValue;
        for (int i = 0; i < p.Length; i++)
        {
            if (Mathf.Abs(p[i].magnitude - outer) > w.RootScale * 1e-3f) continue;
            float a = Mathf.Repeat(Mathf.Atan2(p[i].y, p[i].x), 2f * Mathf.PI);
            if (a < best) { best = a; baseRoot = i; }
        }

        float turn = Mathf.PI * .5f - Mathf.Atan2(p[baseRoot].y, p[baseRoot].x);
        float Height(float hour, float r) => w.Ridges == null ? 0f : -w.Lift * SaddleShape(hour, h, w.Ridges) * r * r;
        Vector3 Polar(float hour, float r)
        {
            float a = (90f - 360f / h * hour) * Mathf.Deg2Rad;
            return o + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, Height(hour, r));
        }

        Vector3 RootAt(Vector2 q)
        {
            float cs = Mathf.Cos(turn), sn = Mathf.Sin(turn);
            Vector2 r = new(q.x * cs - q.y * sn, q.x * sn + q.y * cs);
            r.x = -r.x;
            float hour = Mathf.Repeat((90f - Mathf.Atan2(r.y, r.x) * Mathf.Rad2Deg) / (360f / h), h);
            return o + new Vector3(r.x, r.y, Height(hour, r.magnitude));
        }

        // roots and edges
        List<(Vector3, Vector3)> edges = new();
        for (int i = 0; i < roots.Count; i++)
            for (int j = i + 1; j < roots.Count; j++)
                if (System.Math.Abs(Dot8(roots[i], roots[j]) - 1) < 1e-9) edges.Add((RootAt(p[i]) + Vector3.forward * .1f, RootAt(p[j]) + Vector3.forward * .1f));
        Segments(g, "edges", edges, w.Dot * .07f, new Color(.45f, .55f, .9f) * .4f);
        List<Vector3> rp = new(); List<Color> rc = new(); List<float> rs = new();
        foreach (Vector2 q in p) { rp.Add(RootAt(q) + Vector3.back * .1f); rc.Add(Color.Lerp(new Color(1f, .85f, .35f), new Color(1f, .45f, .6f), q.magnitude / outer)); rs.Add(w.Dot * .8f); }
        ExpPointCloud(g, "roots", rp, rc, rs, Factory.GlowIntensity);
        float R(long n) => w.From + (w.To - w.From) * (n - 1) / (float)w.NMax;
        // the saddle's wire mesh and its ridge / valley lines
        if (w.Ridges != null && w.Mesh)
        {
            List<(Vector3, Vector3)> mesh = new();
            int spokesN = h * 2;
            for (int k = 0; k < spokesN; k++)
            {
                float hour = k * .5f;
                for (int s = 0; s < 24; s++)
                {
                    float r0 = w.From * .6f + (w.To * 1.04f - w.From * .6f) * s / 24f, r1 = w.From * .6f + (w.To * 1.04f - w.From * .6f) * (s + 1) / 24f;
                    mesh.Add((Polar(hour, r0), Polar(hour, r1)));
                }
            }

            for (int ring = 0; ring <= 6; ring++)
            {
                float r = w.From * .6f + (w.To * 1.04f - w.From * .6f) * ring / 6f;
                for (int s = 0; s < h * 8; s++) mesh.Add((Polar(s / 8f, r), Polar((s + 1) / 8f, r)));
            }

            Segments(g, "saddle mesh", mesh, w.Dot * .06f, new Color(.4f, .45f, .62f) * .45f);
            int[] valleys = new int[w.Ridges.Length];
            for (int i = 0; i < w.Ridges.Length; i++)
            {
                int a = w.Ridges[i], b = i + 1 < w.Ridges.Length ? w.Ridges[i + 1] : w.Ridges[0] + h;
                valleys[i] = (a + b) / 2 % h;
            }

            void Guide(int hour, Color col, string name)
            {
                List<Vector3> line = new();
                for (int s = 0; s <= 40; s++) line.Add(Polar(hour, w.From * .6f + (w.To * 1.5f - w.From * .6f) * s / 40f) + Vector3.back * .15f);
                ExpLine(g, name, w.Dot * .22f, col, Factory.SoftGlowIntensity, line.ToArray());
            }

            foreach (int r in w.Ridges) Guide(r, new Color(1f, .8f, .4f) * .7f, $"ridge {r}");
            foreach (int v in valleys) Guide(v, new Color(.4f, .55f, 1f) * .7f, $"valley {v}");
            if (w.LabelSize > 0f)
            {
                foreach (int r in w.Ridges) ExpLabelWide(g, $"ridge {r}", Polar(r, w.To * 1.42f), w.LabelSize * .4f, new Color(1f, .82f, .45f));
                if (w.ValleyLabels)
                    foreach (int v in valleys) ExpLabelWide(g, $"valley {v}", Polar(v, w.To * 1.8f), w.LabelSize * .36f, new Color(.5f, .65f, 1f));
            }
        }

        // the spokes (units mod h): straight on the flat wheel, parabolas on the saddle
        for (int k = 1; k < h; k++)
        {
            if (Gcd(k, h) != 1) continue;
            List<Vector3> line = new();
            for (int s = 0; s <= 30; s++) line.Add(Polar(k, w.From * .92f + (w.To * 1.01f - w.From * .92f) * s / 30f) + Vector3.back * .12f);
            Color sc = w.TwinColours && !TwinSpoke(k, h) ? NoTwinBlue * .7f : PrimeGold * .45f;
            ExpLine(g, $"spoke {k}", w.Dot * (w.TwinColours && !TwinSpoke(k, h) ? .3f : .2f), sc, Factory.SoftGlowIntensity, line.ToArray());
        }

        // the numbers
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        List<long> impostors = new();
        for (long n = 1; n <= w.NMax; n++)
        {
            bool prime = ExpPrime(n), unit = Gcd(n, h) == 1;
            if (unit && !prime && n > 1) impostors.Add(n);
            pts.Add(Polar(n % h, R(n)) + Vector3.back * .25f);
            bool blue = w.TwinColours && prime && n > 7 && !TwinSpoke(n % h, h);
            cols.Add(blue ? NoTwinBlue : prime ? PrimeGold : unit && n > 1 ? E8Composite : n == 1 ? Color.white : new Color(.3f, .3f, .38f));
            sizes.Add(prime || (unit && n > 1) ? w.Dot * .9f : w.Dot * .5f);
        }

        ExpPointCloud(g, "numbers", pts, cols, sizes, Factory.GlowIntensity);
        if (w.Named != null)
            foreach (long n in w.Named)
            {
                Vector3 at = Polar(n % h, R(n)) + Vector3.back * .4f;
                RingAround(g, $"named {n}", at, Vector3.back, w.Dot * 2.4f, Color.white, w.Dot * .2f, AttentionIntensity);
                float a = (90f - 360f / h * (n % h)) * Mathf.Deg2Rad;
                Vector3 side = new(-Mathf.Sin(a), Mathf.Cos(a), 0f);
                if (w.LabelSize > 0f)
                    ExpLabelWide(g, $"{n} = {string.Join("·", Factorize(n))}", at + side * (w.LabelSize * .06f), w.LabelSize * .34f, Color.white);
            }
        if (w.RingImpostors)
            foreach (long n in impostors)
                RingAround(g, $"impostor {n}", Polar(n % h, R(n)) + Vector3.back * .3f, Vector3.back, w.Dot * 1.5f, E8Composite * .8f, w.Dot * .14f, Factory.SoftGlowIntensity);
        if (w.TwinLinks)
            for (long n = 5; n + 2 <= w.NMax; n++)
            {
                if (!ExpPrime(n) || !ExpPrime(n + 2)) continue;
                List<Vector3> arc = new();
                for (int s = 0; s <= 12; s++) { float t = s / 12f; arc.Add(Polar((n + 2 * t) % h, R((long)n) + (R(n + 2) - R(n)) * t) + Vector3.back * .45f); }
                ExpLine(g, $"twins {n}", w.Dot * .32f, Color.white * .9f, AttentionIntensity, arc.ToArray());
            }

        if (w.LabelSize <= 0f) return;
        if (w.HourLabels)
            for (int k = 1; k < h; k++)
                if (Gcd(k, h) == 1)
                    ExpLabelWide(g, k.ToString(), Polar(k, w.To + w.LabelSize * .05f) + Vector3.back * .3f, w.LabelSize * .42f, w.TwinColours && !TwinSpoke(k, h) ? NoTwinBlue : PrimeGold);
        if (w.Title != null)
        {
            string[] lines = w.Title.Split('|');
            Vector3 top = o + w.TitleStep.normalized * (w.To + w.LabelSize * .1f);
            for (int i = 0; i < lines.Length; i++)
                ExpLabelWide(g, lines[i], top + w.TitleStep.normalized * (w.LabelSize * .047f * i), w.LabelSize * (i == 0 ? .5f : .34f), i == 0 ? Color.white : new Color(.8f, .83f, .93f));
        }
    }

    // ================================================================================================
    // EG2: the three wheels stacked, with each number's thread through them
    // ================================================================================================

    /// <summary>
    /// E6, E7, E8 wheels stacked along +z (gap apart), each with the numbers 1 … nMax at their hours; a thread through each
    /// number's three positions for the numbers coprime to 6: gold for primes, red for the composites on spokes of all three
    /// (7·7, 7·11, …), blue for the multiples of 5 — on spokes in E6 and E7, off them in E8.
    /// </summary>
    public void ExpWheelStack(Vector3 o, int nMax, float gap, float from, float to, float dot, float labelSize, long[] threads, int[] labelLayers)
    {
        int[] ranks = { 6, 7, 8 };
        ExpEnsureSieve(nMax + 2);
        Transform g = ExpGroup("wheel stack").transform;
        Vector3 Pos(int layer, long n)
        {
            int h = ranks[layer] == 6 ? 12 : ranks[layer] == 7 ? 18 : 30;
            float r = from + (to - from) * (n - 1) / (float)nMax, a = (90f - 360f / h * (n % h)) * Mathf.Deg2Rad;
            return o + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, gap * layer);
        }

        for (int layer = 0; layer < 3; layer++)
        {
            int h = ranks[layer] == 6 ? 12 : ranks[layer] == 7 ? 18 : 30;
            Vector3 c = o + Vector3.forward * (gap * layer);
            RingAround(g, $"rim {h}", c, Vector3.back, to * 1.06f, new Color(.5f, .55f, .7f) * .5f, dot * .12f, Factory.NoGlow);
            List<(Vector3, Vector3)> spokes = new();
            for (int k = 1; k < h; k++)
            {
                if (Gcd(k, h) != 1) continue;
                float a = (90f - 360f / h * k) * Mathf.Deg2Rad;
                Vector3 d = new(Mathf.Cos(a), Mathf.Sin(a), 0f);
                spokes.Add((c + d * from * .85f, c + d * to * 1.02f));
                if (labelSize > 0f) ExpLabelWide(g, k.ToString(), c + d * (to * 1.06f + labelSize * .04f), labelSize * .34f, PrimeGold);
            }

            Segments(g, $"spokes {h}", spokes, dot * .18f, PrimeGold * .4f, Factory.SoftGlowIntensity);
            List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
            for (long n = 1; n <= nMax; n++)
            {
                bool prime = ExpPrime(n), unit = Gcd(n, h) == 1;
                pts.Add(Pos(layer, n) + Vector3.back * .2f);
                cols.Add(prime ? PrimeGold : unit && n > 1 ? E8Composite : new Color(.3f, .3f, .38f));
                sizes.Add(prime || unit ? dot : dot * .55f);
            }

            ExpPointCloud(g, $"numbers {h}", pts, cols, sizes, Factory.GlowIntensity);
            if (labelSize > 0f)
                ExpLabelWide(g, $"{LatticeName(ranks[layer])}:  {h} hours", c + new Vector3(-to * 1.25f, 0f, 0f), labelSize * .5f, Color.white, TextAlignmentOptions.Right);
        }

        foreach (long n in threads)
        {
            bool prime = ExpPrime(n), five = n % 5 == 0;
            Color col = prime ? PrimeGold : five ? new Color(.45f, .65f, 1f) : E8Composite;
            ExpLine(g, $"thread {n}", dot * .32f, col, Factory.GlowIntensity,
                new[] { Pos(0, n) + Vector3.back * .3f, Pos(1, n) + Vector3.back * .3f, Pos(2, n) + Vector3.back * .3f });
            for (int layer = 0; layer < 3; layer++)
                RingAround(g, $"thread {n} ring {layer}", Pos(layer, n) + Vector3.back * .35f, Vector3.back, dot * 2f, col, dot * .2f, AttentionIntensity);
            if (labelSize > 0f)
            {
                int layer = labelLayers[System.Array.IndexOf(threads, n)];
                Vector3 at = Pos(layer, n), outDir = (at - (o + Vector3.forward * (gap * layer))).normalized;
                ExpLabelWide(g, prime ? n.ToString() : $"{n} = {string.Join("·", Factorize(n))}", at + outDir * (dot * 3f + labelSize * .06f) + Vector3.back * .4f, labelSize * .3f, col);
            }
        }
    }

    // ================================================================================================
    // EH1: the spokes' multiplication
    // ================================================================================================

    /// <summary>
    /// The multiplication table of the eight spokes mod 30 (cell colour = the product's spoke), and beside it the eight spokes
    /// on a clock with the arrows of ×k (two 4-cycles for 7), each arrow ending at the product k·s and naming the first
    /// impostor it makes.
    /// </summary>
    public void ExpSpokeMultiplication(Vector3 o, float cell, Vector3 clock, float clockR, int k, float labelSize)
    {
        Transform g = ExpGroup("spoke table").transform;
        int[] U = { 1, 7, 11, 13, 17, 19, 23, 29 };
        Color SpokeColor(int s) => Color.HSVToRGB(System.Array.IndexOf(U, s) / 8f, .55f, .85f);
        List<(Vector3 c, Vector3 u, Vector3 v, Color col)> quads = new();
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                int prod = U[i] * U[j] % 30;
                Vector3 c = o + new Vector3((j - 3.5f) * cell, (3.5f - i) * cell, 0f);
                bool first = U[i] == k && U[j] >= 7;
                quads.Add((c + Vector3.forward * .3f, new Vector3(cell * .46f, 0f, 0f), new Vector3(0f, cell * .46f, 0f), SpokeColor(prod) * (first ? 1f : .55f)));
                if (labelSize > 0f) ExpLabelWide(g, prod.ToString(), c + Vector3.back * .2f, labelSize * .38f, first ? Color.white : new Color(.08f, .08f, .1f));
            }

        ExpQuads(g, "cells", quads, Factory.NoGlow);
        if (labelSize > 0f)
        {
            for (int i = 0; i < 8; i++)
            {
                ExpLabelWide(g, U[i].ToString(), o + new Vector3(-4.5f * cell, (3.5f - i) * cell, 0f), labelSize * .38f, PrimeGold, TextAlignmentOptions.Right);
                ExpLabelWide(g, U[i].ToString(), o + new Vector3((i - 3.5f) * cell, 4.4f * cell, 0f), labelSize * .38f, PrimeGold);
            }

            ExpLabelWide(g, "a × b (mod 30)", o + new Vector3(0f, 5.4f * cell, 0f), labelSize * .45f, Color.white);
            ExpLabelWide(g, $"the bright row:  {k} × each spoke", o + new Vector3(0f, -4.6f * cell, 0f), labelSize * .36f, new Color(.85f, .88f, .95f));
        }

        // the clock of the eight spokes with the arrows of ×k
        Vector3 Spoke(int s, float rr) { float a = (90f - 12f * s) * Mathf.Deg2Rad; return clock + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * rr; }
        RingAround(g, "clock", clock, Vector3.back, clockR, new Color(.5f, .55f, .7f) * .5f, clockR * .006f, Factory.NoGlow);
        List<Vector3> dots = new(); List<Color> dc = new(); List<float> ds = new();
        foreach (int s in U) { dots.Add(Spoke(s, clockR) + Vector3.back * .3f); dc.Add(SpokeColor(s)); ds.Add(clockR * .035f); }
        ExpPointCloud(g, "spokes", dots, dc, ds, Factory.GlowIntensity);
        foreach (int s in U)
        {
            int t = s * k % 30;
            Vector3 a = Spoke(s, clockR * .96f), b = Spoke(t, clockR * .96f);
            Vector3 mid = (a + b) * .5f, toward = (clock - mid);
            Vector3 ctrl = mid + toward * .35f;
            List<Vector3> curve = new();
            for (int q = 0; q <= 24; q++)
            {
                float u = q / 24f;
                curve.Add((1 - u) * (1 - u) * a + 2 * (1 - u) * u * ctrl + u * u * b + Vector3.back * .2f);
            }

            Color col = s % 3 == 1 ? new Color(1f, .5f, .45f) : new Color(.5f, .7f, 1f);
            ExpLine(g, $"x{k} {s}", clockR * .012f, col, Factory.GlowIntensity, curve.ToArray());
            Vector3 dir = (curve[^1] - curve[^3]).normalized, side = Vector3.Cross(dir, Vector3.back).normalized;
            ExpLine(g, $"head {s}", clockR * .012f, col, Factory.GlowIntensity, new[] { curve[^1] - dir * clockR * .07f + side * clockR * .035f, curve[^1], curve[^1] - dir * clockR * .07f - side * clockR * .035f });
        }

        if (labelSize <= 0f) return;
        foreach (int s in U) ExpLabelWide(g, $"{s}", Spoke(s, clockR * 1.13f) + Vector3.back * .2f, labelSize * .4f, PrimeGold);
        ExpLabelWide(g, $"× {k}:  " + Cycles(U, k), clock + new Vector3(0f, clockR * 1.42f, 0f), labelSize * .36f, Color.white);
        int row = 0;
        foreach (int s in new[] { 7, 11, 13, 17, 19, 23, 29, 31 })
        {
            long prod = (long)k * s;
            Color col = (s % 30) % 3 == 1 ? new Color(1f, .55f, .5f) : new Color(.55f, .75f, 1f);
            ExpLabelWide(g, $"{k} · {s} = {prod}   on spoke {prod % 30}", clock + new Vector3(clockR * 1.45f, clockR * (.95f - .27f * row), 0f), labelSize * .32f, col, TextAlignmentOptions.Left);
            row++;
        }
    }

    static string Cycles(int[] U, int k)
    {
        HashSet<int> seen = new();
        List<string> parts = new();
        foreach (int s in U)
        {
            if (seen.Contains(s)) continue;
            List<int> c = new() { s };
            seen.Add(s);
            int x = s * k % 30;
            while (x != s) { c.Add(x); seen.Add(x); x = x * k % 30; }
            parts.Add("(" + string.Join(" → ", c) + ")");
        }

        return string.Join("  ", parts);
    }

    // ================================================================================================
    // EH2: the double overlap
    // ================================================================================================

    /// <summary>
    /// The E8 wheel with every composite on a spoke ringed once per prime factor (with multiplicity), one colour per prime:
    /// 49 = 7·7 two red rings, 77 = 7·11 red and orange, 1001 = 7·11·13 three. The primes carry no ring.
    /// </summary>
    public void ExpOverlapWheel(Vector3 o, int nMax, float from, float to, float dot, float labelSize, long[] named)
    {
        Transform g = ExpGroup("overlap wheel").transform;
        ExpEnsureSieve(nMax + 2);
        Vector3 Pos(long n)
        {
            float r = from + (to - from) * (n - 1) / (float)nMax, a = (90f - 12f * (n % 30)) * Mathf.Deg2Rad;
            return o + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
        }

        List<(Vector3, Vector3)> spokes = new();
        foreach (int k in new[] { 1, 7, 11, 13, 17, 19, 23, 29 })
        {
            float a = (90f - 12f * k) * Mathf.Deg2Rad;
            Vector3 d = new(Mathf.Cos(a), Mathf.Sin(a), 0f);
            spokes.Add((o + d * from * .9f, o + d * to * 1.02f));
            if (labelSize > 0f) ExpLabelWide(g, k.ToString(), o + d * (to * 1.04f + labelSize * .05f), labelSize * .42f, PrimeGold);
        }

        Segments(g, "spokes", spokes, dot * .16f, PrimeGold * .35f, Factory.SoftGlowIntensity);
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        for (long n = 1; n <= nMax; n++)
        {
            if (Gcd(n, 30) != 1) continue;
            bool prime = ExpPrime(n);
            pts.Add(Pos(n) + Vector3.back * .2f); cols.Add(prime ? PrimeGold : n == 1 ? Color.white : new Color(.85f, .85f, .9f)); sizes.Add(dot);
            if (prime || n == 1) continue;
            List<long> f = Factorize(n);
            for (int i = 0; i < f.Count; i++)
                RingAround(g, $"ring {n} {i}", Pos(n) + Vector3.back * .3f, Vector3.back, dot * (1.7f + .75f * i), FactorColor(f[i]), dot * .22f, Factory.GlowIntensity);
        }

        ExpPointCloud(g, "numbers", pts, cols, sizes, Factory.GlowIntensity);
        if (labelSize <= 0f) return;
        int ni = 0;
        foreach (long n in named)
        {
            Vector3 p = Pos(n), outDir = (p - o).normalized, side = Vector3.Cross(Vector3.back, outDir).normalized * (ni++ % 2 == 0 ? 1f : -1f);
            ExpLabelWide(g, $"{n} = {string.Join("·", Factorize(n))}", p + side * (dot * 6f + labelSize * .09f) + Vector3.back * .4f, labelSize * .3f, Color.white);
        }

        // the colour key
        for (int i = 0; i < 6; i++)
        {
            Vector3 at = o + new Vector3(to * 1.45f, to * .6f - i * labelSize * .05f, 0f);
            RingAround(g, $"key {SmallPrimes[i]}", at, Vector3.back, dot * 1.6f, FactorColors[i], dot * .22f, Factory.GlowIntensity);
            ExpLabelWide(g, $"a factor {SmallPrimes[i]}", at + new Vector3(dot * 3f, 0f, 0f), labelSize * .34f, FactorColors[i], TextAlignmentOptions.Left);
        }
    }

    // ================================================================================================
    // EH3: the spokes bent into confocal parabolas
    // ================================================================================================

    /// <summary>
    /// The E8 wheel at the Sacks radius s√n, each composite n = a·b on a spoke (7 ≤ a ≤ b) lifted to the height s(b − a)/2
    /// above it: in each spoke's plane the points of one factor a lie on the parabola z = (r² − s²a²)/(2sa), all with their
    /// focus at the centre. The primes have no such pair and stay on the floor.
    /// </summary>
    public void ExpParabolaWheel(Vector3 o, int nMax, float s, float dot, float labelSize, int[] bright)
    {
        Transform g = ExpGroup("parabola wheel").transform;
        ExpEnsureSieve(nMax + 2);
        int[] U = { 1, 7, 11, 13, 17, 19, 23, 29 };
        Vector3 Dir(long n) { float a = (90f - 12f * (n % 30)) * Mathf.Deg2Rad; return new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f); }
        Vector3 Floor(long n) => o + Dir(n) * (s * Mathf.Sqrt(n));
        float rMax = s * Mathf.Sqrt(nMax);
        List<(Vector3, Vector3)> spokes = new();
        foreach (int k in U)
        {
            Vector3 d = Dir(k);
            spokes.Add((o, o + d * rMax * 1.02f));
            if (labelSize > 0f) ExpLabelWide(g, k.ToString(), o + d * (rMax * 1.04f + labelSize * .04f), labelSize * .42f, PrimeGold);
        }

        Segments(g, "spokes", spokes, dot * .16f, PrimeGold * .4f, Factory.SoftGlowIntensity);
        List<Vector3> fp = new(); List<Color> fc = new(); List<float> fs = new();
        List<Vector3> lp = new(); List<Color> lc = new(); List<float> ls = new();
        List<(Vector3, Vector3)> stems = new();
        for (long n = 1; n <= nMax; n++)
        {
            if (Gcd(n, 30) != 1) continue;
            bool prime = ExpPrime(n);
            fp.Add(Floor(n) + Vector3.back * .15f); fc.Add(prime ? PrimeGold : n == 1 ? Color.white : E8Composite * .7f); fs.Add(prime ? dot : dot * .7f);
            for (long a = 7; a * a <= n; a++)
            {
                if (n % a != 0) continue;
                long b = n / a;
                Vector3 up = Floor(n) + Vector3.back * (s * (b - a) * .5f);
                lp.Add(up); lc.Add(FactorColor(Factorize(a)[0])); ls.Add(dot * 1.1f);
                stems.Add((Floor(n), up));
            }
        }

        ExpPointCloud(g, "floor", fp, fc, fs, Factory.GlowIntensity);
        ExpPointCloud(g, "lifted", lp, lc, ls, AttentionIntensity);
        Segments(g, "stems", stems, dot * .07f, new Color(.6f, .6f, .7f) * .4f);
        // the parabolas, one per factor a and spoke, from the first lifted point to the rim
        foreach (int a in new[] { 7, 11, 13, 17, 19, 23, 29 })
        {
            Color col = FactorColor(a);
            foreach (int h in U)
            {
                // the smallest b ≥ a with a·b ≡ h (mod 30)
                long b0 = -1;
                for (long b = a; b < a + 30; b++) if (a * b % 30 == h) { b0 = b; break; }
                if (b0 < 0 || a * b0 > nMax) continue;
                List<Vector3> arc = new();
                float r0 = s * Mathf.Sqrt(a * b0);
                for (int q = 0; q <= 40; q++)
                {
                    float r = r0 + (rMax - r0) * q / 40f, z = (r * r - s * s * a * a) / (2f * s * a);
                    arc.Add(o + Dir(h) * r + Vector3.back * z);
                }

                bool hi = System.Array.IndexOf(bright, h) >= 0;
                ExpLine(g, $"parabola {a} on {h}", dot * (hi ? .3f : .1f), col * (hi ? 1f : .4f), hi ? Factory.GlowIntensity : Factory.SoftGlowIntensity, arc.ToArray());
                if (hi && labelSize > 0f && a <= 13) ExpLabelWide(g, $"a = {a}", arc[^1] + Dir(h) * (labelSize * .03f), labelSize * .3f, col);
            }
        }
    }

    /// <summary>
    /// The parabola wheel cut through its centre: the spokes on the right half of the clock (1 … 13) folded onto +r, those on
    /// the left (17 … 29) onto −r, r = s√n; each composite n = a·b (7 ≤ a ≤ b) at height s(b − a)/2 on the confocal parabola
    /// of its factor a, z = (r² − s²a²)/(2sa), drawn up to zMax; the primes on the floor.
    /// </summary>
    public void ExpParabolaProfile(Vector3 o, int nMax, float s, float zMax, float dot, float labelSize)
    {
        Transform g = ExpGroup("parabola profile").transform;
        ExpEnsureSieve(nMax + 2);
        float rMax = s * Mathf.Sqrt(nMax);
        float Side(long n) => (n % 30) <= 15 ? 1f : -1f;
        Segments(g, "floor", new List<(Vector3, Vector3)> { (o + Vector3.left * rMax * 1.04f, o + Vector3.right * rMax * 1.04f) }, dot * .16f, PrimeGold * .4f, Factory.SoftGlowIntensity);
        foreach (int a in new[] { 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43 })
        {
            if (a * a > nMax) break;
            List<Vector3> curve = new();
            for (int q = 0; q <= 160; q++)
            {
                float r = -rMax + 2f * rMax * q / 160f, z = (r * r - s * s * a * a) / (2f * s * a);
                if (z < -s * a * .5f || z > zMax) { if (curve.Count > 1) ExpLine(g, $"parabola {a} part", dot * .14f, FactorColor(a) * .55f, Factory.SoftGlowIntensity, curve.ToArray()); curve.Clear(); continue; }
                curve.Add(o + new Vector3(r, z, .2f));
            }

            if (curve.Count > 1) ExpLine(g, $"parabola {a}", dot * .14f, FactorColor(a) * .55f, Factory.SoftGlowIntensity, curve.ToArray());
        }

        if (labelSize > 0f)
        {
            int i = 0;
            foreach (int a in new[] { 7, 11, 13, 17, 19, 23, 29 })
            {
                Vector3 at = o + new Vector3(rMax * 1.08f, zMax * .95f - i * labelSize * .042f, 0f);
                ExpLine(g, $"key {a}", dot * .3f, FactorColor(a), Factory.GlowIntensity, new[] { at, at + new Vector3(labelSize * .05f, 0f, 0f) });
                ExpLabelWide(g, $"a = {a}", at + new Vector3(labelSize * .07f, 0f, 0f), labelSize * .3f, FactorColor(a), TextAlignmentOptions.Left);
                i++;
            }
        }

        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
        for (long n = 2; n <= nMax; n++)
        {
            if (Gcd(n, 30) != 1) continue;
            float r = Side(n) * s * Mathf.Sqrt(n);
            if (ExpPrime(n)) { pts.Add(o + new Vector3(r, 0f, -.2f)); cols.Add(PrimeGold); sizes.Add(dot); continue; }
            for (long a = 7; a * a <= n; a++)
            {
                if (n % a != 0) continue;
                float z = s * (n / a - a) * .5f;
                if (z > zMax) continue;
                pts.Add(o + new Vector3(r, z, -.2f)); cols.Add(FactorColor(Factorize(a)[0])); sizes.Add(dot * 1.1f);
            }
        }

        ExpPointCloud(g, "points", pts, cols, sizes, Factory.GlowIntensity);
        if (labelSize <= 0f) return;
        ExpLabelWide(g, "spokes 17 … 29", o + new Vector3(-rMax * .9f, labelSize * .03f, 0f), labelSize * .3f, PrimeGold);
        ExpLabelWide(g, "spokes 1 … 13", o + new Vector3(rMax * .9f, labelSize * .03f, 0f), labelSize * .3f, PrimeGold);
        ExpDot(g, "focus", o + Vector3.back * .3f, dot * 1.4f, Color.white, AttentionIntensity);
        ExpLabelWide(g, "the centre: the focus", o + new Vector3(0f, -labelSize * .03f, -.3f), labelSize * .27f, Color.white);
        int left = 0, right = 0;
        foreach (long n in new long[] { 49, 77, 143, 169, 91, 121 })
        {
            long a = Factorize(n)[0];
            float side = Side(n);
            Vector3 p = o + new Vector3(side * s * Mathf.Sqrt(n), s * (n / a - a) * .5f, -.3f);
            int row = side < 0 ? left++ : right++;
            Vector3 lab = o + new Vector3(side * rMax * .52f, -labelSize * (.1f + .045f * row), -.3f);
            ExpLine(g, $"leader {n}", dot * .12f, Color.white * .45f, Factory.NoGlow, new[] { p, lab + new Vector3(-side * labelSize * .01f, labelSize * .01f, 0f) });
            ExpLabelWide(g, $"{n} = {a}·{n / a}" + (n / a == a ? ":  a square, on the floor" : ""), lab, labelSize * .27f, Color.white,
                side > 0 ? TextAlignmentOptions.Left : TextAlignmentOptions.Right);
        }
    }

    /// <summary>A plain caption at a world position (several lines split on '|'), for the stills.</summary>
    public void ExpCaption(Vector3 at, Vector3 step, string text, float size, bool rightAligned = false)
    {
        Transform g = ExpGroup("caption").transform;
        string[] lines = text.Split('|');
        for (int i = 0; i < lines.Length; i++)
            ExpLabelWide(g, lines[i], at + step * i, size * (i == 0 ? 1f : .8f), i == 0 ? Color.white : new Color(.82f, .85f, .94f), rightAligned ? TextAlignmentOptions.Right : TextAlignmentOptions.Left);
    }

    /// <summary>The EG–EI wheels from the test (no TextMeshPro type crosses into the test assembly).</summary>
    public void ExpWheelFor(Vector3 o, int rank, int nMax, float from, float to, float rootScale, float dot, float labelSize, string title,
        int[] ridges = null, float lift = 0f, bool twins = false, bool mesh = false, bool ringImpostors = true, long[] named = null,
        bool hourLabels = true, Vector3? titleStep = null, bool valleyLabels = true)
    {
        ExpCoxeterWheel(o, new WheelSpec
        {
            Rank = rank, NMax = nMax, From = from, To = to, RootScale = rootScale, Dot = dot, LabelSize = labelSize, Title = title,
            Ridges = ridges, Lift = lift, TwinLinks = twins, Mesh = mesh, RingImpostors = ringImpostors, Named = named, TwinColours = ridges != null,
            HourLabels = hourLabels, TitleStep = titleStep ?? Vector3.down, ValleyLabels = valleyLabels,
        });
    }
}
