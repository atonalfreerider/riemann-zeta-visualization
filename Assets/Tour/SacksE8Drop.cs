using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Section EF (docs/experiments/README.md) and the module "Dropping onto E8": deciding prime / not prime by a position or an
/// intersection in E8.
///
/// The wheel: E8's Coxeter element c turns its Coxeter plane by 1/30 of a turn (30 = 2·3·5) and satisfies the 30th
/// cyclotomic polynomial, so cᵏ turns E8 the same way as c exactly for the eight k coprime to 30. Dropped onto the wheel at
/// its hour n mod 30, every prime above 5 lands on one of those eight spokes: position alone decides divisibility by 2, 3, 5.
/// The tower: the sphere of norm 2n meets E8 in 240·σ₃(n) points, 240(1 + n³) exactly when n is prime, and each proper divisor
/// d adds 240·d³. For n = pq the excess 240(p³ + q³) gives p and q back (p + q is the root of t³ − 3nt − (p³ + q³)).
/// </summary>
public partial class SacksSpiral
{
    public const float E8DropOffset = 36300f;

    Vector3 e8WheelCenter;
    float e8HourRadius, e8LabelRadius, e8WheelLabel, e8WheelDot, e8Turn;

    /// <summary>The centre of the wheel last drawn (the shells stack under it; the trace chains start from it).</summary>
    public Vector3 E8WheelCenter => e8WheelCenter;
    bool e8WheelReady;

    static readonly Color E8Composite = new(1f, .42f, .4f);
    static readonly Color E8Unit = new(.82f, .86f, 1f);

    /// <summary>The vectors of norm 2n in E8: doubled coordinates all even or all odd, |2v|² = 8n, and 2v summing to 0 mod 4.</summary>
    static List<double[]> E8Shell(int n)
    {
        List<double[]> list = new();
        int[] x = new int[8];
        void Rec(int k, int rem, int parity)
        {
            if (k == 8)
            {
                if (rem != 0) return;
                int s = 0;
                for (int i = 0; i < 8; i++) s += x[i];
                if (((s % 4) + 4) % 4 != 0) return;
                double[] v = new double[8];
                for (int i = 0; i < 8; i++) v[i] = x[i] * .5;
                list.Add(v);
                return;
            }

            int m = (int)System.Math.Sqrt(rem);
            for (int a = -m; a <= m; a++)
            {
                if ((a & 1) != parity) continue;
                int left = rem - a * a;
                if (left < 0 || (parity == 1 && left < 7 - k)) continue;
                x[k] = a;
                Rec(k + 1, left, parity);
            }
        }

        Rec(0, 8 * n, 0);
        Rec(0, 8 * n, 1);
        return list;
    }

    static string Thousands(long v) => v.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

    static List<long> Divisors(long n)
    {
        List<long> d = new();
        for (long i = 1; i <= n; i++) if (n % i == 0) d.Add(i);
        return d;
    }

    /// <summary>The position of hour k on the wheel (clockwise from the top), at radius r.</summary>
    float e8SpiralFrom = 15f, e8SpiralTo = 33f;
    int e8SpiralN = 600;

    /// <summary>Where n sits on the wheel's spiral of numbers (hour n mod 30, radius growing with n).</summary>
    public Vector3 E8SpiralPosition(long n) =>
        E8Hour((int)(n % 30), e8SpiralFrom + (e8SpiralTo - e8SpiralFrom) * (n - 1) / (float)e8SpiralN) + Vector3.back * .3f;

    /// <summary>
    /// Light numbers on the wheel's spiral (the rest muted with exp:mute:E8 spiral): a bright dot at each, its hour's spoke
    /// drawn bright from the centre out, and its name (group "E8 lit"; exp:e8unhl clears it).
    /// </summary>
    public void ExpE8Light(long[] ns, bool spoke)
    {
        Transform g = (ExpRoot().Find("E8 lit") ?? ExpGroup("E8 lit").transform);
        foreach (long n in ns)
        {
            Vector3 at = E8SpiralPosition(n) + Vector3.back * .3f;
            bool prime = ExpPrime(n);
            Color col = prime ? PrimeGold : n % 2 == 0 || n % 3 == 0 || n % 5 == 0 ? new Color(1f, .45f, .4f) : new Color(.75f, .85f, 1f);
            ExpDot(g, $"lit {n}", at, e8WheelDot * 1.7f, col, AttentionIntensity);
            Flare(at, e8WheelDot * 4f, 1.2f, 2);
            if (spoke)
            {
                int h = (int)(n % 30);
                LineRenderer l = ExpLine(g, $"spoke {h}", e8WheelDot * .3f, Color.white, AttentionIntensity,
                    new[] { E8Hour(h, e8SpiralFrom * .92f) + Vector3.back * .25f, E8Hour(h, e8SpiralTo * 1.02f) + Vector3.back * .25f });
                StartCoroutine(SettleRoutine(l, col * .9f, 1f));
            }
        }
    }

    public void ExpE8Unlight()
    {
        Transform g = ExpRoot().Find("E8 lit");
        if (g != null) Destroy(g.gameObject);
    }

    Vector3 E8Hour(int k, float r)
    {
        float a = (90f - 12f * k) * Mathf.Deg2Rad;
        return e8WheelCenter + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * r;
    }

    /// <summary>
    /// The wheel: E8's roots in the Coxeter plane (turned so that one root of the outer ring is at the top, hour 0, and
    /// mirrored so the hours run clockwise), the 6720 edges, and the numbers 1 … nMax dropped at their hour n mod 30 on a
    /// spiral between spiralFrom and spiralTo; the composites in <paramref name="impostors"/> ringed. Groups: "E8 rings"
    /// (edges, then the eight rings from the inside out), "E8 spiral" (the spokes, then one loop of 30 at a time) and
    /// "E8 impostors".
    /// </summary>
    public void ExpE8Wheel(Vector3 o, float scale, int nMax, float spiralFrom, float spiralTo, float dot, float labelSize, long[] impostors)
    {
        e8WheelCenter = o; e8WheelDot = dot; e8WheelLabel = labelSize;
        e8SpiralFrom = spiralFrom; e8SpiralTo = spiralTo; e8SpiralN = nMax;
        ExpEnsureSieve(nMax + 2);
        List<double[]> roots = E8Roots();
        (double[] u1, double[] u2) = E8CoxeterPlane();
        Vector2[] p = new Vector2[roots.Count];
        for (int i = 0; i < roots.Count; i++) p[i] = new Vector2((float)Dot8(roots[i], u1), (float)Dot8(roots[i], u2)) * scale;
        List<float> rings = new();
        foreach (Vector2 q in p) if (!rings.Exists(r => Mathf.Abs(r - q.magnitude) < scale * 1e-3f)) rings.Add(q.magnitude);
        rings.Sort();
        // turn the outer ring's first root to the top and mirror (hours clockwise)
        int baseRoot = -1; float best = float.MaxValue;
        for (int i = 0; i < p.Length; i++)
        {
            if (Mathf.Abs(p[i].magnitude - rings[^1]) > scale * 1e-3f) continue;
            float a = Mathf.Repeat(Mathf.Atan2(p[i].y, p[i].x), 2f * Mathf.PI);
            if (a < best) { best = a; baseRoot = i; }
        }

        float turn = Mathf.PI * .5f - Mathf.Atan2(p[baseRoot].y, p[baseRoot].x);
        e8Turn = turn;
        Vector3 W(Vector2 q)
        {
            float c = Mathf.Cos(turn), s = Mathf.Sin(turn);
            Vector2 r = new(q.x * c - q.y * s, q.x * s + q.y * c);
            return o + new Vector3(-r.x, r.y, 0f); // mirrored: c now turns clockwise
        }

        e8HourRadius = rings[^1];
        Transform g = ExpGroup("E8 rings").transform;
        Transform items = RevealItems(g);
        GameObject edgesItem = new("the edges");
        edgesItem.transform.SetParent(items, false);
        List<(Vector3, Vector3)> edges = new();
        for (int i = 0; i < roots.Count; i++)
            for (int j = i + 1; j < roots.Count; j++)
                if (System.Math.Abs(Dot8(roots[i], roots[j]) - 1) < 1e-9) edges.Add((W(p[i]) + Vector3.forward * .2f, W(p[j]) + Vector3.forward * .2f));
        Segments(edgesItem.transform, "edges", edges, dot * .09f, new Color(.45f, .55f, .9f) * .45f);
        edgesItem.AddComponent<RevealNote>().Text = "E8:  240 roots,  6,720 edges\n" + Small("coordinates all whole or all half-whole, even sum");
        for (int ri = 0; ri < rings.Count; ri++)
        {
            GameObject ring = new($"ring {ri + 1}");
            ring.transform.SetParent(items, false);
            List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
            for (int i = 0; i < p.Length; i++)
            {
                if (Mathf.Abs(p[i].magnitude - rings[ri]) > scale * 1e-3f) continue;
                pts.Add(W(p[i]) + Vector3.back * .2f);
                cols.Add(Color.Lerp(new Color(1f, .85f, .35f), new Color(1f, .45f, .6f), ri / 7f));
                sizes.Add(dot);
            }

            ExpPointCloud(ring.transform, "roots", pts, cols, sizes, Factory.GlowIntensity);
            ring.AddComponent<RevealNote>().Text = $"ring {ri + 1} of 8:  30 roots\n" + Small("in the Coxeter plane, where E8 turns by 1/30");
        }

        // the spiral of numbers
        Transform sg = ExpGroup("E8 spiral").transform;
        Transform sitems = RevealItems(sg);
        float R(long n) => spiralFrom + (spiralTo - spiralFrom) * (n - 1) / (float)nMax;
        e8LabelRadius = spiralTo + labelSize * .06f;
        GameObject spokes = new("spokes");
        spokes.transform.SetParent(sitems, false);
        List<(Vector3, Vector3)> spokeSegs = new();
        for (int k = 0; k < 30; k++)
            if (Gcd(k, 30) == 1) spokeSegs.Add((E8Hour(k, spiralFrom * .92f), E8Hour(k, spiralTo * 1.01f)));
        Segments(spokes.transform, "spokes", spokeSegs, dot * .16f, PrimeGold * .35f);
        spokes.AddComponent<RevealNote>().Text = "each number at its hour\n" + Small("n mod 30;  gold spokes: 1, 7, 11, 13, 17, 19, 23, 29");
        int loops = (nMax + 29) / 30;
        for (int l = 0; l < loops; l++)
        {
            GameObject loop = new($"loop {l + 1}");
            loop.transform.SetParent(sitems, false);
            List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
            for (long n = l * 30 + 1; n <= System.Math.Min(nMax, l * 30 + 30); n++)
            {
                bool prime = ExpPrime(n);
                pts.Add(E8Hour((int)(n % 30), R(n)) + Vector3.back * .3f);
                cols.Add(prime ? PrimeGold : Gcd(n, 30) == 1 ? new Color(.62f, .66f, .82f) : new Color(.3f, .3f, .38f));
                sizes.Add(prime ? dot * .9f : dot * .55f);
            }

            ExpPointCloud(loop.transform, "numbers", pts, cols, sizes, Factory.GlowIntensity);
            // the path from number to number, so the winding shows (on into the next loop)
            List<Vector3> path = new();
            for (long n = l * 30 + 1; n <= System.Math.Min(nMax, l * 30 + 31); n++) path.Add(E8Hour((int)(n % 30), R(n)) + Vector3.back * .25f);
            if (path.Count > 1) ExpLine(loop.transform, "path", dot * .1f, new Color(.55f, .6f, .8f) * .45f, Factory.NoGlow, path.ToArray());
            RevealNote note = loop.AddComponent<RevealNote>();
            note.Text = $"1 … {System.Math.Min(nMax, l * 30 + 30)} dropped on the wheel\n" + Small("gold: primes, all above 5 on a gold spoke");
        }

        Transform ig = ExpGroup("E8 impostors").transform;
        Transform iitems = RevealItems(ig);
        List<string> listed = new();
        foreach (long n in impostors)
        {
            GameObject it = new($"impostor {n}");
            it.transform.SetParent(iitems, false);
            Vector3 at = E8Hour((int)(n % 30), R(n)) + Vector3.back * .4f;
            RingAround(it.transform, $"ring {n}", at, Vector3.back, dot * 1.9f, E8Composite, dot * .2f, AttentionIntensity);
            listed.Add($"{n} = {string.Join("·", Factorize(n))}");
            RevealNote note = it.AddComponent<RevealNote>();
            note.Text = "on a gold spoke, but composite\n" + Small(string.Join("     ", listed));
            note.FlareAt.Add(at);
        }

        e8WheelReady = true;
    }

    /// <summary>The gold mark and number of hour k (k coprime to 30) at the rim of the wheel.</summary>
    void E8HourMark(Transform g, int k)
    {
        Vector3 at = E8Hour(k, e8LabelRadius);
        ExpDot(g, $"hour {k}", E8Hour(k, e8HourRadius) + Vector3.back * .5f, e8WheelDot * 1.6f, PrimeGold, AttentionIntensity);
        ExpLine(g, $"hour {k} tick", e8WheelDot * .18f, PrimeGold * .45f, Factory.SoftGlowIntensity,
            new[] { E8Hour(k, e8HourRadius * 1.15f) + Vector3.back * .4f, E8Hour(k, e8LabelRadius - e8WheelLabel * .04f) + Vector3.back * .4f });
        if (e8WheelLabel > 0f) ExpLabelWide(g, k.ToString(), at + Vector3.back * .3f, e8WheelLabel * .5f, PrimeGold);
    }

    /// <summary>All eight hour marks at once (for a still picture).</summary>
    public void ExpE8HourMarks()
    {
        if (!e8WheelReady) return;
        Transform g = ExpGroup("E8 walk").transform;
        for (int k = 1; k < 30; k++) if (Gcd(k, 30) == 1) E8HourMark(g, k);
    }

    /// <summary>
    /// c walks a root of the outer ring once round the 30 hours over <paramref name="seconds"/> (exp:e8walk:SECONDS[:T1,T2,…]);
    /// the eight hours k coprime to 30, where cᵏ turns E8 the same way as c, light gold, each at its time Tᵢ (seconds from the
    /// start, to meet the narration), or as the walker passes them when no times are given.
    /// </summary>
    public void ExpE8WalkLive(float seconds, float[] lightAt = null) => StartLive(E8WalkLive(seconds, lightAt));

    IEnumerator E8WalkLive(float seconds, float[] lightAt)
    {
        if (!e8WheelReady) yield break;
        Transform g = ExpGroup("E8 walk").transform;
        GameObject walker = ExpDot(g, "walker", E8Hour(0, e8HourRadius) + Vector3.back * .6f, e8WheelDot * 2.2f, Color.white, AttentionIntensity);
        int[] hours = { 1, 7, 11, 13, 17, 19, 23, 29 };
        bool scheduled = lightAt != null && lightAt.Length >= hours.Length;
        List<int> found = new();
        float per = seconds / 30f, clock = 0f;
        int next = 0;
        void Light(int h)
        {
            found.Add(h);
            E8HourMark(g, h);
            Flare(E8Hour(h, e8HourRadius), e8WheelDot * 2.5f, 1.1f, 1);
            Board($"c<sup>{h}</sup> turns E8 like c\n" + Small("so far:  " + string.Join(", ", found)));
        }

        Board("c turns E8 by one hour\n" + Small("1/30 of a circle;  30 = 2 · 3 · 5"));
        while (clock < seconds || (scheduled && next < hours.Length))
        {
            if (walker == null) yield break;
            float hour = Mathf.Min(30f, clock / per);
            float a = (90f - 12f * hour) * Mathf.Deg2Rad;
            walker.transform.position = e8WheelCenter + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * e8HourRadius + Vector3.back * .6f;
            if (scheduled) { while (next < hours.Length && clock >= lightAt[next]) Light(hours[next++]); }
            else { while (next < hours.Length && hour >= hours[next]) Light(hours[next++]); }
            yield return null;
            clock += Time.deltaTime;
        }

        if (!scheduled) while (next < hours.Length) Light(hours[next++]);
        Board("8 of 30 turn E8 like c\n" + Small("1, 7, 11, 13, 17, 19, 23, 29:  no factor shared with 30"));
        liveRoutine = null;
    }

    /// <summary>
    /// The tower: one floor per sphere of norm 2n (n = 1 … nMax), its points projected to the Coxeter plane and scaled to the
    /// same radius, floor n at depth (n − 1)·gap (+z, down the screen); gold for a prime, red for a composite. The floors are
    /// the items of the groups named in <paramref name="groups"/> (floors split by the ranges in <paramref name="splits"/>).
    /// </summary>
    public void ExpE8Tower(Vector3 o, int nMax, float radius, float gap, float dot, float labelSize, string[] groups, int[] splits, bool attach = false)
    {
        // attached: the shells stack under the wheel last drawn, turned and mirrored as it is, so that floor 1 is the wheel
        if (attach && e8WheelReady)
        {
            o = e8WheelCenter;
            radius = e8HourRadius;
        }

        ExpEnsureSieve(nMax + 2);
        (double[] u1, double[] u2) = E8CoxeterPlane();
        Transform axisGroup = ExpGroup("E8 tower axis").transform;
        ExpLine(axisGroup, "axis", dot * .2f, new Color(.55f, .6f, .8f) * .5f, Factory.NoGlow, new[] { o + Vector3.back * gap * .6f, o + Vector3.forward * (gap * (nMax - .4f)) });
        int gi = 0;
        Transform items = RevealItems(ExpGroup(groups[0]).transform);
        for (int n = 1; n <= nMax; n++)
        {
            if (gi + 1 < groups.Length && n > splits[gi]) { gi++; items = RevealItems(ExpGroup(groups[gi]).transform); }
            List<double[]> shell = E8Shell(n);
            bool prime = ExpPrime(n);
            Color col = n == 1 ? E8Unit : prime ? PrimeGold : E8Composite;
            Vector3 c = o + Vector3.forward * (gap * (n - 1));
            List<Vector2> proj = new(shell.Count);
            float maxR = 0f;
            foreach (double[] v in shell)
            {
                Vector2 q = new((float)Dot8(v, u1), (float)Dot8(v, u2));
                proj.Add(q); maxR = Mathf.Max(maxR, q.magnitude);
            }

            GameObject floor = new($"floor {n}");
            floor.transform.SetParent(items, false);
            List<Vector3> pts = new(shell.Count); List<Color> cols = new(shell.Count); List<float> sizes = new(shell.Count);
            float size = dot * Mathf.Clamp(1.6f / Mathf.Pow(shell.Count / 240f, .33f), .18f, 1f);
            float ct = Mathf.Cos(e8Turn), st = Mathf.Sin(e8Turn);
            foreach (Vector2 q in proj)
            {
                Vector2 s = q / maxR * radius;
                if (attach) s = new Vector2(-(s.x * ct - s.y * st), s.x * st + s.y * ct); // the wheel's turn and mirror
                pts.Add(c + new Vector3(s.x, s.y, 0f));
                cols.Add(col * Mathf.Lerp(1f, .7f, s.magnitude / radius));
                sizes.Add(size);
            }

            // attached, floor 1 is the wheel itself: only its rim and labels are added
            if (!(attach && n == 1)) ExpPointCloud(floor.transform, "points", pts, cols, sizes, Factory.SoftGlowIntensity);
            RingAround(floor.transform, "rim", c, Vector3.back, radius * 1.04f, col * .7f, dot * .18f, Factory.SoftGlowIntensity);
            long count = shell.Count, least = 240L * (1 + (long)n * n * n);
            if (labelSize > 0f)
            {
                ExpLabelWide(floor.transform, $"n = {n}", c + new Vector3(-radius * 1.1f, 0f, 0f), labelSize * .45f, col, TextAlignmentOptions.Right);
                ExpLabelWide(floor.transform, Thousands(count), c + new Vector3(radius * 1.1f, 0f, 0f), labelSize * .38f, col, TextAlignmentOptions.Left);
            }

            List<long> proper = Divisors(n).FindAll(d => d > 1 && d < n);
            string sum = $"240 × (1 + {string.Join(" + ", proper.ConvertAll(d => d + "³"))}{(proper.Count > 0 ? " + " : "")}{n}³)";
            string verdict = n == 1 ? Small("the 240 roots") : prime
                ? Small($"{sum}:  {Tint(PrimeGold, "the fewest a sphere can hold")}")
                : Small($"{sum}:  {Tint(E8Composite, Thousands(count - least) + " more, for " + string.Join(" and ", proper))}");
            RevealNote note = floor.AddComponent<RevealNote>();
            note.Text = $"n = {n}:  {Thousands(count)} points\n{verdict}";
            note.FlareAt.Add(c + new Vector3(-radius * 1.1f, 0f, -.5f));
        }
    }

    /// <summary>
    /// For n = pq: the sphere's extra cubes s = p³ + q³ (its count minus 240(1 + n³), over 240), and p + q as the root
    /// t > √n of t³ − 3nt − s = 0, which gives p and q back (exp:e8solve:N).
    /// </summary>
    public void ExpE8SolveBoard(long n)
    {
        long s = 0;
        foreach (long d in Divisors(n)) if (d > 1 && d < n) s += d * d * d;
        long t = 1;
        while (t * t * t - 3 * n * t - s < 0) t++;
        long disc = t * t - 4 * n, r = (long)System.Math.Round(System.Math.Sqrt(System.Math.Max(0, disc)));
        string tail = r * r == disc ? $"{(t - r) / 2} × {(t + r) / 2}" : "not two primes";
        Board($"{n}:  {Thousands(s)} extra cubes = p³ + q³\n" + Small($"t³ − {3 * n}t − {Thousands(s)} = 0:  t = {t} = p + q,  {n} = {tail}"));
    }

    /// <summary>The closing board of the module (exp:e8close).</summary>
    public static void ExpE8CloseBoard() =>
        Board("the fewest points: a prime\n" + Small("exact, by intersection, but slower than dividing"));

    /// <summary>
    /// The count of the sphere of n as a stack of cubes, one per divisor d (a d × d × d block of dots; above maxDots a wire
    /// cube), side by side along +x with their bases at o, rising up the screen (−z); the cubes of 1 and n white (gold when n
    /// is prime), the cubes of the proper divisors red. Group "E8 cubes {n}", one reveal item per cube.
    /// </summary>
    public void ExpDivisorCubes(Vector3 o, long n, float voxel, float gap, float dot, float labelSize, int maxDots = 400)
    {
        ExpEnsureSieve(n + 2);
        Transform items = RevealItems(ExpGroup($"E8 cubes {n}").transform);
        bool prime = ExpPrime(n);
        float x = 0f;
        long running = 0;
        List<long> divs = Divisors(n);
        List<string> terms = new();
        if (labelSize > 0f)
            ExpLabelWide(ExpGroup($"E8 cubes {n} label").transform, $"n = {n}", o + new Vector3(-voxel * 2.2f, 0f, -voxel * 1.2f), labelSize * .5f,
                prime ? PrimeGold : E8Composite, TextAlignmentOptions.Right);
        int index = 0;
        foreach (long d in divs)
        {
            bool trivial = d == 1 || d == n;
            float stagger = (index++ % 2) * labelSize * .045f;
            Color col = trivial ? (prime ? PrimeGold : E8Unit) : E8Composite;
            GameObject cube = new($"cube {d}");
            cube.transform.SetParent(items, false);
            float side = d * voxel;
            Vector3 corner = o + new Vector3(x, 0f, 0f);
            if (d * d * d <= maxDots)
            {
                List<Vector3> pts = new(); List<Color> cols = new(); List<float> sizes = new();
                for (int i = 0; i < d; i++)
                    for (int j = 0; j < d; j++)
                        for (int k = 0; k < d; k++)
                        {
                            pts.Add(corner + new Vector3((i + .5f) * voxel, (j + .5f - d * .5f) * voxel, -(k + .5f) * voxel));
                            cols.Add(col); sizes.Add(dot);
                        }

                ExpPointCloud(cube.transform, "dots", pts, cols, sizes, Factory.GlowIntensity);
            }

            // the wire box
            Vector3 b = corner + new Vector3(0f, -side * .5f, 0f);
            Vector3[] v =
            {
                b, b + new Vector3(side, 0f, 0f), b + new Vector3(side, side, 0f), b + new Vector3(0f, side, 0f),
                b + new Vector3(0f, 0f, -side), b + new Vector3(side, 0f, -side), b + new Vector3(side, side, -side), b + new Vector3(0f, side, -side),
            };
            List<(Vector3, Vector3)> e = new()
            {
                (v[0], v[1]), (v[1], v[2]), (v[2], v[3]), (v[3], v[0]), (v[4], v[5]), (v[5], v[6]), (v[6], v[7]), (v[7], v[4]),
                (v[0], v[4]), (v[1], v[5]), (v[2], v[6]), (v[3], v[7]),
            };
            Segments(cube.transform, "box", e, dot * (d * d * d <= maxDots ? .12f : .3f), col * .75f, Factory.SoftGlowIntensity);
            if (labelSize > 0f)
                ExpLabelWide(cube.transform, d == 1 ? "1" : $"{d}³ = {Thousands(d * d * d)}", corner + new Vector3(side * .5f, 0f, voxel * 2.4f + labelSize * .035f + stagger),
                    labelSize * .33f, col);
            running += d * d * d;
            terms.Add(d == 1 ? "1" : $"{d}³");
            RevealNote note = cube.AddComponent<RevealNote>();
            note.Text = d == n
                ? $"{n}:  {string.Join(" + ", terms)} = {Thousands(running)}\n" + Small($"× 240 = {Thousands(240 * running)} points on the sphere")
                : $"{n}:  {string.Join(" + ", terms)}\n" + Small(trivial ? "one cube for each divisor" : Tint(E8Composite, $"the divisor {d} adds {Thousands(d * d * d)} cubes of points"));
            note.FlareAt.Add(corner + new Vector3(side * .5f, 0f, -side * .5f));
            x += side + gap;
        }
    }
}
