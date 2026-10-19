using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Where the marbles of the counting language sit, in marble diameters (1 = one marble across), about their centre.
/// * Flat: a symmetric spiral. A centre marble (for odd counts) and m arms that wind out together, an m-start spiral, each
///   arm turned τ/m from the last; small counts are rings (a pair, a triangle, a pentagon), 7 is a centre and six round it.
/// * Deep: a spiral sphere pack. Shells round the centre, filled from the outside in so the outside is always a full,
///   round shell: with one arm, each shell a spherical Fibonacci spiral; with m arms, m coils wound from pole to pole.
/// Both list marbles from the centre out, arm by arm: the centre (if any), then ring t holds marble t of every arm, at
/// index t·m + a (plus one for a centre). So when p copies of a disc merge into p arms, copy c's marble t goes to
/// index t·p + c: the copy becomes arm c, its inner marbles inside, its outer ones outside.
/// Layouts are cached: a count, an arm number and a centre always give the same marbles.
/// </summary>
public static class MarbleLayout
{
    /// <summary>Centre to centre of neighbouring marbles along an arm (a marble is 1 across).</summary>
    public const float Gap = 1.1f;
    /// <summary>Between neighbouring arms: a little wider than along them, so a container's arms read as a pinwheel.</summary>
    public const float ArmGap = 1.22f;
    /// <summary>Between the arms (and coils) of an unpacked volume: wide enough that each arm shows as its own lane.</summary>
    public const float VolumeArmGap = 1.5f;
    const float Tol = .03f;

    static readonly float GoldenAngle = Mathf.PI * (3f - Mathf.Sqrt(5f));
    static readonly Dictionary<(int, int, bool, bool), Vector2[]> flatCache = new();
    static readonly Dictionary<(int, int, bool, bool), float[]> angleCache = new();
    static readonly Dictionary<(int, int, bool), Vector3[]> deepCache = new();

    /// <summary>The smallest radius a ring of m marbles fits on.</summary>
    public static float RingRadius(int m) => m <= 1 ? 0f : Gap * .5f / Mathf.Sin(Mathf.PI / m);

    /// <summary>
    /// m for n marbles in m equal arms: six, five, eight, seven or three if they divide n, then nine to thirteen; four
    /// only for long arms (four short bent arms look like a hooked cross); else two, else one.
    /// </summary>
    public static int ArmsFor(long n)
    {
        foreach (int m in new[] { 6, 5, 8, 7, 3, 9, 10, 11, 12, 13 })
            if (n % m == 0) return m;
        if (n % 4 == 0 && n / 4 >= 7) return 4;
        return n % 2 == 0 ? 2 : 1;
    }

    /// <summary>
    /// A container's shape for p marbles (also any lone count): up to six, a ring of p (a pair, a triangle, a square, a
    /// pentagon, a hexagon); an odd count, a centre and m arms of (p − 1) / m; an even one, m arms of p / m.
    /// </summary>
    public static (bool center, int arms) ShapeOf(long count)
    {
        if (count <= 1) return (true, 1);
        if (count <= 6) return (false, (int)count);
        if (count % 2 == 1) return (true, ArmsFor(count - 1));
        return (false, ArmsFor(count));
    }

    // ---- flat ---------------------------------------------------------------------------------------------------

    /// <summary>
    /// count marbles in an m-start spiral, with a centre marble or not; adjacent arms ArmGap apart, or VolumeArmGap for
    /// a wide (volume) spiral.
    /// </summary>
    public static Vector2[] Flat(int count, int m, bool center, bool wide = false)
    {
        m = Mathf.Max(1, m);
        if (flatCache.TryGetValue((count, m, center, wide), out Vector2[] hit)) return hit;
        List<Vector2> pts = new(count);
        List<float> angles = new(count);
        if (center && count > 0) { pts.Add(Vector2.zero); angles.Add(Mathf.PI / 2f); }
        int q = (count - pts.Count) / m;
        float b = m * (wide ? VolumeArmGap : ArmGap) / (Mathf.PI * 2f); // radius gained per radian: adjacent arms an arm gap apart
        float r0 = Mathf.Max(RingRadius(m), center ? Gap : 0f, m == 1 ? .5f * Gap : 0f);
        Grid2 grid = new(Gap);
        foreach (Vector2 p in pts) grid.Add(p);
        float phi = 0f;
        Vector2 last = Vector2.zero;
        for (int t = 0; t < q; t++)
        {
            float rr;
            Vector2 at;
            while (true)
            {
                // walk out along arm 0 until a gap from its last marble and clear of everything placed
                rr = r0 + b * phi;
                at = new Vector2(rr * Mathf.Cos(Mathf.PI / 2f + phi), rr * Mathf.Sin(Mathf.PI / 2f + phi));
                if (t == 0 || ((at - last).sqrMagnitude >= Gap * Gap && grid.Clear(at, Gap - Tol))) break;
                phi += .004f;
            }

            last = at;
            float a0 = Mathf.PI / 2f + phi;
            for (int a = 0; a < m; a++)
            {
                float ang = a0 + Mathf.PI * 2f * a / m;
                Vector2 p = new(rr * Mathf.Cos(ang), rr * Mathf.Sin(ang));
                pts.Add(p);
                angles.Add(ang);
                grid.Add(p);
            }
        }

        // a count that is not centre + m·q (callers keep to it) still gets every marble: the rest on a ring outside
        int extra = count - pts.Count;
        float rx = (pts.Count > 0 ? Radius(pts) : 0f) + Gap;
        for (int i = 0; i < extra; i++)
        {
            float ang = Mathf.PI / 2f + Mathf.PI * 2f * i / extra;
            pts.Add(new Vector2(rx * Mathf.Cos(ang), rx * Mathf.Sin(ang)));
            angles.Add(ang);
        }

        Vector2[] arr = pts.ToArray();
        flatCache[(count, m, center, wide)] = arr;
        angleCache[(count, m, center, wide)] = angles.ToArray();
        return arr;
    }

    /// <summary>
    /// Each flat marble's angle about the centre, unwrapped along its arm (π/2 + τa/m at the arm's start, growing as the
    /// arm winds out): so a marble can swirl into its place the way the arm winds.
    /// </summary>
    public static float[] FlatAngles(int count, int m, bool center, bool wide = false)
    {
        m = Mathf.Max(1, m);
        if (!angleCache.ContainsKey((count, m, center, wide))) Flat(count, m, center, wide);
        return angleCache[(count, m, center, wide)];
    }

    // ---- deep ---------------------------------------------------------------------------------------------------

    /// <summary>
    /// count marbles as a spiral sphere pack. m = 1: Fibonacci shells (a lone container); m &gt; 1: m coils on every
    /// shell (coil a holds arm a). A centre marble only where the count wants one (m = 1).
    /// </summary>
    public static Vector3[] Deep(int count, int m, bool center = false)
    {
        m = Mathf.Max(1, m);
        if (count % m != 0) m = 1;
        if (deepCache.TryGetValue((count, m, center), out Vector3[] hit)) return hit;
        Vector3[] arr = m == 1 ? Small(count) ?? FibonacciBall(count) : CoilBall(count, m);
        deepCache[(count, m, center)] = arr;
        return arr;
    }

    /// <summary>The smallest counts as the polyhedra they pack into: a pair, a triangle, a tetrahedron, a bipyramid, an octahedron.</summary>
    static Vector3[] Small(int n)
    {
        float g = Gap;
        switch (n)
        {
            case 0: return new Vector3[0];
            case 1: return new[] { Vector3.zero };
            case 2: return new[] { new Vector3(0f, g * .5f, 0f), new Vector3(0f, -g * .5f, 0f) };
            case 3:
            {
                Vector2[] f = Flat(3, 3, false);
                return new[] { (Vector3)f[0], (Vector3)f[1], (Vector3)f[2] };
            }
            case 4:
            {
                float s = g / (2f * Mathf.Sqrt(2f));
                return new[] { new Vector3(s, s, s), new Vector3(s, -s, -s), new Vector3(-s, s, -s), new Vector3(-s, -s, s) };
            }
            case 5:
            {
                float r = g / Mathf.Sqrt(3f);
                List<Vector3> v = new() { new Vector3(0f, g * .82f, 0f) };
                for (int i = 0; i < 3; i++) v.Add(new Vector3(r * Mathf.Cos(Mathf.PI * 2f * i / 3f), 0f, r * Mathf.Sin(Mathf.PI * 2f * i / 3f)));
                v.Add(new Vector3(0f, -g * .82f, 0f));
                return v.ToArray();
            }
            case 6:
            {
                float a = g / Mathf.Sqrt(2f);
                return new[] { new Vector3(0f, a, 0f), new Vector3(a, 0f, 0f), new Vector3(0f, 0f, a), new Vector3(-a, 0f, 0f), new Vector3(0f, 0f, -a), new Vector3(0f, -a, 0f) };
            }
        }

        return null;
    }

    static Vector3 Sph(float rho, float th, float ph) => new(rho * Mathf.Sin(th) * Mathf.Cos(ph), rho * Mathf.Cos(th), rho * Mathf.Sin(th) * Mathf.Sin(ph));

    /// <summary>n points on a sphere along the Fibonacci spiral (pole to pole, the golden angle round).</summary>
    static void Fibonacci(List<Vector3> into, float rho, int n, float twist)
    {
        for (int i = 0; i < n; i++)
        {
            float z = 1f - (2f * i + 1f) / n;
            into.Add(Sph(rho, Mathf.Acos(Mathf.Clamp(z, -1f, 1f)), i * GoldenAngle + twist));
        }
    }

    static readonly Dictionary<int, int> fibCap = new();

    /// <summary>The most marbles a Fibonacci shell of this radius holds a gap apart (about 10 (ρ/gap)²).</summary>
    static int FibonacciCapacity(float rho)
    {
        int key = Mathf.RoundToInt(rho * 1000f);
        if (fibCap.TryGetValue(key, out int c)) return c;
        int n = Mathf.Max(1, Mathf.FloorToInt(10.6f * rho * rho / (Gap * Gap)));
        List<Vector3> f = new();
        while (n > 1)
        {
            f.Clear();
            Fibonacci(f, rho, n, 0f);
            if (MinDistanceAtLeast(f, Gap - Tol)) break;
            n--;
        }

        fibCap[key] = n;
        return n;
    }

    static Vector3[] FibonacciBall(int count)
    {
        // the outer radius: the smallest whose full shells (ρ, ρ − gap, …, and a centre) hold count
        List<(float rho, int cap)> shells = new();
        // coarse steps until the shells hold enough, then back one and fine ones
        float start = .55f * Gap;
        while (FibonacciTotal(start + .5f * Gap) < count) start += .5f * Gap;
        for (float rho = start; ; rho += .05f * Gap)
        {
            shells.Clear();
            int total = 0;
            float r = rho;
            for (; r > .45f * Gap; r -= Gap)
            {
                int c = FibonacciCapacity(r);
                shells.Add((r, c));
                total += c;
            }

            if (r > -.05f && (shells.Count == 0 || shells[^1].rho >= Gap - Tol)) { shells.Add((0f, 1)); total += 1; }
            if (total >= count) break;
        }

        // outside in: the outer shells full, what is left on the innermost; listed from the centre out
        int left = count;
        List<(float rho, int n)> take = new();
        foreach ((float rho, int cap) in shells)
        {
            int n = Mathf.Min(cap, left);
            take.Add((rho, n));
            left -= n;
        }

        List<Vector3> pts = new(count);
        for (int s = take.Count - 1; s >= 0; s--)
        {
            (float rho, int n) = take[s];
            if (n <= 0) continue;
            if (rho <= 0f) pts.Add(Vector3.zero);
            else if (n <= 6 && rho < 1.5f * Gap) pts.AddRange(Small(n));
            else Fibonacci(pts, rho, n, rho * 1.7f);
        }

        return pts.ToArray();
    }

    /// <summary>What the full shells ρ, ρ − gap, … (and a centre) of a Fibonacci ball hold.</summary>
    static int FibonacciTotal(float rho)
    {
        int total = 0;
        float r = rho;
        for (; r > .45f * Gap; r -= Gap) total += FibonacciCapacity(r);
        return total + (r > -.05f && rho >= Gap - Tol ? 1 : 0);
    }

    static int CoilTotal(float rho, int m)
    {
        int total = 0;
        for (float r = rho; r > .45f * Gap; r -= Gap)
        {
            int n = CoilShell(r, m).Count;
            if (n == 0) break;
            total += n;
        }

        return total;
    }

    static readonly Dictionary<(int, int), List<Vector3[]>> coilCache = new();

    /// <summary>
    /// The rings of a shell wound with m coils pole to pole (adjacent coils VolumeArmGap apart): ring t = marble t of every
    /// coil, a gap apart along each coil.
    /// </summary>
    static List<Vector3[]> CoilShell(float rho, int m)
    {
        int key = Mathf.RoundToInt(rho * 1000f);
        if (coilCache.TryGetValue((key, m), out List<Vector3[]> hit)) return hit;
        float k = m * VolumeArmGap / (Mathf.PI * 2f * rho);  // polar angle gained per radian round the axis
        float th0 = Mathf.Min(Mathf.PI / 2f, Mathf.Max(RingRadius(m), .5f * Gap) / rho);
        List<Vector3[]> rings = new();
        Grid3 grid = new(Gap);
        Vector3[] Ring(float th)
        {
            Vector3[] ring = new Vector3[m];
            for (int c = 0; c < m; c++) ring[c] = Sph(rho, th, (th - th0) / k + Mathf.PI * 2f * c / m);
            return ring;
        }

        for (float th = th0; th <= Mathf.PI - th0 + 1e-6f; th += .002f)
        {
            Vector3[] ring = Ring(th);
            if ((ring[0] - ring[1]).magnitude < Gap - Tol || !grid.Clear(ring[0], Gap - Tol)) continue;
            rings.Add(ring);
            foreach (Vector3 p in ring) grid.Add(p);
        }

        if (rings.Count == 0)
        {
            Vector3[] ring = Ring(Mathf.PI / 2f);
            if ((ring[0] - ring[1]).magnitude >= Gap - Tol) rings.Add(ring);
        }

        coilCache[(key, m)] = rings;
        return rings;
    }

    static Vector3[] CoilBall(int count, int m)
    {
        int per = count / m;
        List<(float rho, List<Vector3[]> rings)> shells = new();
        float start = Mathf.Max(RingRadius(m), .5f * Gap);
        while (CoilTotal(start + .5f * Gap, m) < per) start += .5f * Gap;
        for (float rho = start; ; rho += .05f * Gap)
        {
            shells.Clear();
            int total = 0;
            for (float r = rho; r > .45f * Gap; r -= Gap)
            {
                List<Vector3[]> rings = CoilShell(r, m);
                if (rings.Count == 0) break;
                shells.Add((r, rings));
                total += rings.Count;
            }

            if (total >= per) break;
        }

        int left = per;
        List<(List<Vector3[]> rings, int n)> take = new();
        foreach ((float _, List<Vector3[]> rings) in shells)
        {
            int n = Mathf.Min(rings.Count, left);
            take.Add((rings, n));
            left -= n;
        }

        List<Vector3> pts = new(count);
        for (int s = take.Count - 1; s >= 0; s--)
        {
            (List<Vector3[]> rings, int n) = take[s];
            int cap = rings.Count;
            for (int i = 0; i < n; i++)
            {
                // a partial shell takes rings spread evenly along the coils, so it stays round
                int j = n == 1 ? cap / 2 : Mathf.RoundToInt(i * (cap - 1) / (float)(n - 1));
                pts.AddRange(rings[j]);
            }
        }

        return pts.ToArray();
    }

    // ---- helpers ------------------------------------------------------------------------------------------------

    public static float Radius(IList<Vector2> pts)
    {
        float r = 0f;
        foreach (Vector2 p in pts) r = Mathf.Max(r, p.magnitude);
        return r + .5f;
    }

    public static float Radius(IList<Vector3> pts)
    {
        float r = 0f;
        foreach (Vector3 p in pts) r = Mathf.Max(r, p.magnitude);
        return r + .5f;
    }

    static bool MinDistanceAtLeast(List<Vector3> pts, float d)
    {
        Grid3 g = new(d);
        foreach (Vector3 p in pts)
        {
            if (!g.Clear(p, d)) return false;
            g.Add(p);
        }

        return true;
    }

    /// <summary>A uniform grid for "is anything closer than d" (cells as wide as the gap).</summary>
    sealed class Grid2
    {
        readonly float cell;
        readonly Dictionary<(int, int), List<Vector2>> cells = new();
        public Grid2(float cell) => this.cell = cell;
        (int, int) Key(Vector2 p) => (Mathf.FloorToInt(p.x / cell), Mathf.FloorToInt(p.y / cell));

        public void Add(Vector2 p)
        {
            var k = Key(p);
            if (!cells.TryGetValue(k, out List<Vector2> l)) cells[k] = l = new List<Vector2>();
            l.Add(p);
        }

        public bool Clear(Vector2 p, float d)
        {
            (int x, int y) = Key(p);
            for (int i = -1; i <= 1; i++)
            for (int j = -1; j <= 1; j++)
                if (cells.TryGetValue((x + i, y + j), out List<Vector2> l))
                    foreach (Vector2 q in l)
                        if ((p - q).sqrMagnitude < d * d) return false;
            return true;
        }
    }

    sealed class Grid3
    {
        readonly float cell;
        readonly Dictionary<(int, int, int), List<Vector3>> cells = new();
        public Grid3(float cell) => this.cell = cell;
        (int, int, int) Key(Vector3 p) => (Mathf.FloorToInt(p.x / cell), Mathf.FloorToInt(p.y / cell), Mathf.FloorToInt(p.z / cell));

        public void Add(Vector3 p)
        {
            var k = Key(p);
            if (!cells.TryGetValue(k, out List<Vector3> l)) cells[k] = l = new List<Vector3>();
            l.Add(p);
        }

        public bool Clear(Vector3 p, float d)
        {
            (int x, int y, int z) = Key(p);
            for (int i = -1; i <= 1; i++)
            for (int j = -1; j <= 1; j++)
            for (int k = -1; k <= 1; k++)
                if (cells.TryGetValue((x + i, y + j, z + k), out List<Vector3> l))
                    foreach (Vector3 q in l)
                        if ((p - q).sqrMagnitude < d * d) return false;
            return true;
        }
    }

    // ---- the exponent: the shadow of a k-dimensional cube ---------------------------------------------------------

    /// <summary>
    /// The k-cube's shadow standing on a corner: its k edge directions fanned evenly over the upper half-plane, so the
    /// outline is a regular 2k-gon (a square, a hexagon for the cube, an octagon for the tesseract …). Vertex b (a k-bit
    /// mask) is the sum of the edges in b; z lifts alternate edges toward and away from the viewer, so turned in depth
    /// it is a k-cube seen from its long diagonal (for k = 3 exactly the cube). Edge length L.
    /// </summary>
    public static Vector3 CubeEdge(int k, int j, float L, float depth)
    {
        float a = Mathf.PI / (2f * k) + Mathf.PI * j / k;
        float lift = k == 2 ? 0f : (j % 2 == 0 ? 1f : -1f) * depth * .7f;
        return new Vector3(L * Mathf.Cos(a), L * Mathf.Sin(a), lift * L);
    }

    /// <summary>The 2k-gon round the shadow, as vertex masks: 0, e0, e0 + e1, …, all, …, e(k − 1).</summary>
    public static int[] CubeOutline(int k)
    {
        List<int> path = new() { 0 };
        int b = 0;
        for (int j = 0; j < k; j++) { b |= 1 << j; path.Add(b); }
        for (int j = 0; j < k - 1; j++) { b &= ~(1 << j); path.Add(b); }
        return path.ToArray();
    }
}
