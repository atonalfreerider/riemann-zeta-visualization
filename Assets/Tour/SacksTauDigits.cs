using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The digits of τ and the primes (docs/experiments/README.md, section R*). τ is made of the primes as a whole: Euler's
/// products give τ² = 24 ζ(2) and τ⁴ = 3! · 240 · ζ(4), 240 being E8's roots (RA), and the primes can be read back out of
/// τ's digits, each prime dearer than the last (RB). The helix of one radian per number shows τ's continued fraction as
/// arms, with the primes on the arms coprime to 6, 44 and 710 (RC). The digits themselves, read as E8's hours, carry no
/// trace of the primes (RD). All drawn in the XY plane, to be seen from −z with y up.
/// </summary>
public partial class SacksSpiral
{
    public const float TauOffset = 124000f;

    static readonly Color RGold = new(1f, .82f, .34f), RGrey = new(.55f, .58f, .72f), RRed = new(1f, .38f, .32f), RText = new(.82f, .85f, .94f);

    TextBox RLabel(Transform g, string text, Vector3 at, float size, Color col, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        TextBox l = ExpLabelWide(g, text, at, size, col, align);
        Outline(l.TextField);
        return l;
    }

    /// <summary>An arc of radius r about c from angle a0 to a1 (radians, counterclockwise from +x).</summary>
    static Vector3[] RArc(Vector3 c, float r, double a0, double a1, int perTurn = 360)
    {
        int n = Mathf.Max(2, (int)System.Math.Ceiling(System.Math.Abs(a1 - a0) / (2 * System.Math.PI) * perTurn) + 1);
        Vector3[] pts = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            double a = a0 + (a1 - a0) * i / (n - 1);
            pts[i] = c + new Vector3((float)System.Math.Cos(a), (float)System.Math.Sin(a), 0f) * r;
        }

        return pts;
    }

    static List<int> RPrimes(int n)
    {
        ExpEnsureSieve(n + 1);
        List<int> ps = new();
        for (int i = 2; i <= n; i++) if (ExpPrime(i)) ps.Add(i);
        return ps;
    }

    // ================================================================================================
    // RA. One turn from the primes
    // ================================================================================================

    /// <summary>
    /// Two discs of rings. Each ring is one turn as the primes so far give it: an arc of τ_N radians from 3 o'clock round
    /// counterclockwise, with what the primes still owe drawn red. Left: τ_N = √(24 Π 1/(1 − p⁻²)); right, E8's product:
    /// τ_N = (3! · 240 · Π 1/(1 − p⁻⁴))^¼, with E8's 240 roots (in its Coxeter plane) in the middle. Rings from the inside
    /// out: no prime, then the primes up to 2, 3, 5, 7 and 13.
    /// </summary>
    public void ExpTauTurns(Vector3 o, float R, float labelSize)
    {
        Transform g = ExpGroup("RA turns").transform;
        int[] upTo = { 0, 2, 3, 5, 7, 13 };
        string[] setName = { "no prime", "2", "2, 3", "2, 3, 5", "up to 7", "up to 13" };
        for (int side = 0; side < 2; side++)
        {
            Vector3 c = o + new Vector3(side == 0 ? -1.22f * R : 1.22f * R, 0f, 0f);
            int s = side == 0 ? 2 : 4;
            double k = side == 0 ? 24.0 : 1440.0;
            List<string> table = new();
            for (int i = 0; i < upTo.Length; i++)
            {
                double prod = k;
                for (int p = 2; p <= upTo[i]; p++)
                    if (IsPrimeSmall(p)) prod /= 1 - System.Math.Pow(p, -s);
                double tn = System.Math.Pow(prod, 1.0 / s);
                double gap = 2 * System.Math.PI - tn;
                float r = R * (.56f + .088f * i);
                Color col = Color.Lerp(Color.white, RGold, i / (float)(upTo.Length - 1));
                ExpLine(g, $"turn {side} {i}", R * .028f, col, Factory.SoftGlowIntensity, RArc(c, r, 0, tn, 720));
                if (gap > 1e-6)
                    ExpLine(g, $"gap {side} {i}", R * .014f, RRed, Factory.GlowIntensity, RArc(c, r, tn, 2 * System.Math.PI, 720));
                string deg = Deg(gap * 180 / System.Math.PI);
                table.Add($"{setName[i]}:  {deg}");
                // the gaps that are wide enough get their angle written in them
                if (gap * 180 / System.Math.PI > 20.0)
                {
                    double mid = (tn + 2 * System.Math.PI) / 2;
                    RLabel(g, deg, c + new Vector3((float)System.Math.Cos(mid), (float)System.Math.Sin(mid), 0f) * (r - R * .002f) + Vector3.back * .5f,
                        labelSize * .085f, RRed);
                }
            }

            // the start of every turn, and the full turn they close on
            ExpLine(g, $"start {side}", R * .008f, Color.white * .6f, Factory.NoGlow, new[] { c + Vector3.right * (R * .5f), c + Vector3.right * (R * 1.05f) });
            RLabel(g, side == 0 ? "τ² = 24 · ζ(2)" : "τ<sup>4</sup> = 3! · 240 · ζ(4)", c + Vector3.up * (R * 1.27f), labelSize * .16f, Color.white);
            RLabel(g, side == 0 ? "ζ(2) = Π 1/(1 − p<sup>−2</sup>):  slow" : "240:  the roots of E8;  ζ(4) = Π 1/(1 − p<sup>−4</sup>)",
                c + Vector3.up * (R * 1.12f), labelSize * .1f, RText);
            for (int i = 0; i < table.Count; i++)
                RLabel(g, table[i], c + new Vector3(-R * .98f, -R * (1.22f + .118f * i), 0f), labelSize * .1f,
                    i == 0 ? Color.white : Color.Lerp(Color.white, RGold, i / (float)(table.Count - 1)), TextAlignmentOptions.Left);
            RLabel(g, "short of a full turn, with the primes:", c + new Vector3(-R * .98f, -R * 1.09f, 0f), labelSize * .1f, RText, TextAlignmentOptions.Left);
            if (side == 0)
                RLabel(g, "24", c, labelSize * .3f, Color.white * .85f);
            else
            {
                // E8's 240 roots in its Coxeter plane: 8 rings of 30
                List<double[]> roots = E8Roots();
                (double[] u1, double[] u2) = E8CoxeterPlane();
                float maxR = 0f;
                List<Vector2> pr = new();
                foreach (double[] v in roots)
                {
                    Vector2 q = new((float)Dot8(v, u1), (float)Dot8(v, u2));
                    pr.Add(q);
                    maxR = Mathf.Max(maxR, q.magnitude);
                }

                List<Vector3> pts = new(); List<Color> cols = new(); List<float> sz = new();
                foreach (Vector2 q in pr)
                {
                    pts.Add(c + (Vector3)(q / maxR * (R * .44f)));
                    cols.Add(Color.Lerp(RGold, new Color(1f, .4f, .55f), q.magnitude / maxR));
                    sz.Add(R * .012f);
                }

                ExpPointCloud(g, "E8 roots", pts, cols, sz, Factory.GlowIntensity);
            }
        }
    }

    static bool IsPrimeSmall(int n)
    {
        if (n < 2) return false;
        for (int d = 2; d * d <= n; d++) if (n % d == 0) return false;
        return true;
    }

    static string Deg(double d) => d >= 10 ? $"{d:0.0}°" : d >= 1 ? $"{d:0.00}°" : d >= .01 ? $"{d:0.000}°" : $"{d:0.0000}°";

    // ================================================================================================
    // RB. Reading the primes out of τ
    // ================================================================================================

    /// <summary>
    /// The digits of τ each prime p ≤ 1000 costs to read (TauDigitsData), as bars over the number line on a log scale: gold
    /// where p + 2 is prime too (the dearest), with the running maximum (the digits that read every prime up to p) as a
    /// white staircase and the readouts actually run (50 … 3200 digits) marked on it.
    /// </summary>
    public void ExpTauReadout(Vector3 o, float W, float H, float labelSize)
    {
        Transform g = ExpGroup("RB readout").transform;
        float X(double p) => (float)(-W / 2 + p / 1000.0 * W);
        float Y(double d) => (float)((System.Math.Log10(d) - 1) / 3.0 * H);
        Vector3 P(double p, double d) => o + new Vector3(X(p), Y(d), 0f);
        // the grid: 10, 100, 1000 and 10 000 digits, the axis, and the numbers along it
        List<(Vector3, Vector3)> grid = new();
        foreach (double d in new[] { 10.0, 100.0, 1000.0, 10000.0 })
        {
            grid.Add((P(0, d), P(1000, d)));
            RLabel(g, d >= 1000 ? $"{d:#,0}".Replace(",", " ") : $"{d:0}", P(0, d) + Vector3.left * (W * .012f), labelSize * .08f, RText, TextAlignmentOptions.Right);
        }

        Segments(g, "grid", grid, H * .004f, new Color(.4f, .45f, .62f) * .7f);
        ExpLine(g, "axis", H * .006f, new Color(.6f, .65f, .85f), Factory.NoGlow, new[] { P(0, 10), P(1000, 10) });
        for (int x = 0; x <= 1000; x += 100)
            RLabel(g, x.ToString(), P(x, 10) + Vector3.down * (H * .06f), labelSize * .08f, RText);
        RLabel(g, "digits of τ needed", P(0, 10000) + new Vector3(-W * .012f, H * .07f, 0f), labelSize * .085f, RText, TextAlignmentOptions.Right);
        RLabel(g, "the prime p", P(1000, 10) + new Vector3(0f, -H * .13f, 0f), labelSize * .085f, RText, TextAlignmentOptions.Right);
        // the bars
        int[] ps = TauDigitsData.ReadPrime, ds = TauDigitsData.ReadDigits;
        List<(Vector3, Vector3, Color, float)> bars = new();
        HashSet<int> isP = new(ps);
        for (int i = 0; i < ps.Length; i++)
        {
            bool twin = isP.Contains(ps[i] + 2) || (ps[i] + 2 > 1000 && IsPrimeSmall(ps[i] + 2));
            bars.Add((P(ps[i], 10), P(ps[i], ds[i]), twin ? RGold : new Color(.5f, .75f, 1f), W * .0028f));
        }

        ColouredSegments(g, "bars", bars, Factory.SoftGlowIntensity).sortingOrder = 2;
        // the staircase: the digits that read every prime up to p
        List<Vector3> stair = new();
        int run = 0;
        for (int i = 0; i < ps.Length; i++)
        {
            int nr = Mathf.Max(run, ds[i]);
            if (i > 0) stair.Add(P(ps[i], run));
            stair.Add(P(ps[i], nr));
            run = nr;
        }

        stair.Add(P(1000, run));
        ExpLine(g, "staircase", H * .008f, Color.white, Factory.GlowIntensity, stair.ToArray()).sortingOrder = 3;
        // the readouts actually run: how many primes each read, all of them right
        (int d, int n, int last)[] runs = { (50, 4, 7), (100, 9, 23), (400, 25, 97), (1600, 51, 233), (3200, 80, 409) };
        foreach (var (d, n, last) in runs)
        {
            ExpDot(g, $"run {d}", P(last, d) + Vector3.back * .3f, H * .022f, Color.white, AttentionIntensity);
            RLabel(g, $"{d:#,0}".Replace(",", " "), P(last, d) + new Vector3(-W * .008f, H * .035f, 0f), labelSize * .07f, Color.white, TextAlignmentOptions.Right);
        }


        // the dearest: the lower twins
        foreach (int p in new[] { 881, 419 })
        {
            int i = System.Array.IndexOf(ps, p);
            // 419's label up in the empty corner above the staircase, with a leader down to its bar
            Vector3 at = p == 419 ? P(p, ds[i]) + new Vector3(-W * .02f, H * .2f, 0f) : P(p, ds[i]) + new Vector3(0f, H * .05f, 0f);
            RLabel(g, $"{p} ({p + 2} is prime too):  {ds[i]:#,0} digits".Replace(",", " "), at, labelSize * .075f, RGold,
                p == 419 ? TextAlignmentOptions.Right : TextAlignmentOptions.Center);
            if (p == 419)
                ExpLine(g, "leader 419", H * .004f, RGold * .8f, Factory.NoGlow, new[] { at + new Vector3(W * .004f, -H * .015f, 0f), P(p, ds[i]) + Vector3.up * (H * .01f) });
        }

    }

    // ================================================================================================
    // RC. The helix of one radian per number
    // ================================================================================================

    /// <summary>
    /// Left: n = 1 … nHelix on a helix at n radians (radius hr, rising dz a number): its points fall into six slowly
    /// twisting columns, the classes 6k + r, and the primes (gold) run up two of them. Middle and right, seen from above
    /// with radius √n: n ≤ n44 (44 spiral arms, primes on 20) and the primes up to p710 (710 rays, primes on 280).
    /// </summary>
    public void ExpTauHelix(Vector3 o, float R, int nHelix, int n44, int p710, float labelSize)
    {
        Transform g = ExpGroup("RC helix").transform;
        ExpEnsureSieve(System.Math.Max(n44, p710) + 1);
        // the helix, its axis up the screen and turned a little toward the camera so it reads as one
        Vector3 hc = o + new Vector3(-1.62f * R, -.95f * R, 0f);
        float hr = R * .32f, dz = 1.9f * R / nHelix;
        Quaternion tilt = Quaternion.Euler(-14f, 0f, 0f);
        Vector3 H(int n) => hc + tilt * new Vector3(hr * Mathf.Cos(n), dz * n, hr * Mathf.Sin(n));
        List<Vector3> pts = new(); List<Color> cols = new(); List<float> sz = new();
        List<(Vector3, Vector3, Color, float)> cols6 = new();
        for (int n = 1; n <= nHelix; n++)
        {
            bool p = ExpPrime(n);
            pts.Add(H(n));
            cols.Add(p ? RGold : RGrey * .8f);
            sz.Add(p ? R * .024f : R * .014f);
            if (n + 6 <= nHelix)
            {
                bool lit = n % 6 == 1 || n % 6 == 5;
                cols6.Add((H(n), H(n + 6), lit ? RGold * .55f : RGrey * .3f, R * .005f));
            }
        }

        ColouredSegments(g, "columns", cols6, Factory.NoGlow).sortingOrder = 1;
        ExpPointCloud(g, "helix", pts, cols, sz, Factory.SoftGlowIntensity).sortingOrder = 2;
        RLabel(g, "n at n radians", hc + new Vector3(0f, 2.26f * R, 0f), labelSize * .13f, Color.white);
        RLabel(g, "6 numbers ≈ one turn:  six columns", hc + new Vector3(0f, 2.13f * R, 0f), labelSize * .085f, RText);
        RLabel(g, "the primes on two, 6k ± 1", hc + new Vector3(0f, 2.03f * R, 0f), labelSize * .085f, RText);
        // seen from above, radius √n
        void Disc(Vector3 c, int nMax, bool primesOnly, int q, string title, string sub)
        {
            float k = R * .9f / Mathf.Sqrt(nMax);
            List<Vector3> dp = new(); List<Color> dc = new(); List<float> ds = new();
            for (int n = 1; n <= nMax; n++)
            {
                bool p = ExpPrime(n);
                if (primesOnly && !p) continue;
                dp.Add(c + new Vector3(Mathf.Cos(n), Mathf.Sin(n), 0f) * (Mathf.Sqrt(n) * k));
                dc.Add(p ? RGold : RGrey * .45f);
                ds.Add(nMax <= 100000 ? R * .0075f : R * .0032f);
            }

            ExpPointCloud(g, title, dp, dc, ds, primesOnly ? Factory.NoGlow : Factory.SoftGlowIntensity);
            RLabel(g, title, c + new Vector3(0f, 1.18f * R, 0f), labelSize * .13f, Color.white);
            RLabel(g, sub, c + new Vector3(0f, 1.05f * R, 0f), labelSize * .085f, RText);
        }

        Disc(o + new Vector3(.12f * R, 0f, 0f), n44, true, 44, $"the primes up to {n44:#,0}".Replace(",", " "),
            "44 = 7 turns + 0.0177:  44 arms, the primes on 20");
        Disc(o + new Vector3(2.1f * R, 0f, 0f), p710, true, 710, $"the primes up to {p710:#,0}".Replace(",", " "),
            "710 = 113 turns + 0.00006:  710 arms, the primes on 280");
    }

    // ================================================================================================
    // RD. τ's digits as E8's hours
    // ================================================================================================

    /// <summary>
    /// Left: n ≤ nMax on E8's clock (n at hour n mod 30, radius √n): the primes (gold) keep to the eight hours prime to 30
    /// (and 2, 3, 5). Right: τ's first base-30 digits, the k-th at its own hour and radius √k: they fill all thirty hours
    /// alike, gold where the digit is one of the eight.
    /// </summary>
    public void ExpTauClock(Vector3 o, float R, int nMax, float labelSize)
    {
        Transform g = ExpGroup("RD clock").transform;
        ExpEnsureSieve(nMax + 1);
        string digits = TauDigitsData.Base30;
        int count = Mathf.Min(nMax, digits.Length);
        for (int side = 0; side < 2; side++)
        {
            Vector3 c = o + new Vector3(side == 0 ? -1.15f * R : 1.15f * R, 0f, 0f);
            float k = R / Mathf.Sqrt(count);
            List<Vector3> pts = new(); List<Color> cols = new(); List<float> sz = new();
            for (int n = 1; n <= count; n++)
            {
                int hour = side == 0 ? n % 30 : DigitValue(digits[n - 1]);
                float a = Mathf.PI * 2f * hour / 30f;
                // hour 0 at 12 o'clock, clockwise, as on a clock face
                pts.Add(c + new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f) * (Mathf.Sqrt(n) * k));
                if (side == 0)
                {
                    bool p = ExpPrime(n);
                    cols.Add(p ? RGold : RGrey * .35f);
                    sz.Add(p ? R * .011f : R * .006f);
                }
                else
                {
                    cols.Add(new Color(.55f, .85f, 1f));
                    sz.Add(R * .009f);
                }
            }

            ExpPointCloud(g, side == 0 ? "primes" : "digits", pts, cols, sz, Factory.SoftGlowIntensity);
            for (int h = 0; h < 30; h++)
            {
                float a = Mathf.PI * 2f * h / 30f;
                RLabel(g, h.ToString(), c + new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f) * (R * 1.08f), labelSize * .07f, GoldHour(h) ? RGold : RText * .8f);
            }

            RLabel(g, side == 0 ? $"the numbers up to {count:#,0} at their hours".Replace(",", " ") : $"τ's first {count:#,0} base-30 digits at their hours".Replace(",", " "),
                c + new Vector3(0f, 1.32f * R, 0f), labelSize * .13f, Color.white);
            RLabel(g, side == 0 ? "the primes (gold) keep to E8's eight hours prime to 30" : "every hour alike, gold hours or not",
                c + new Vector3(0f, 1.2f * R, 0f), labelSize * .085f, RText);
        }
    }

    static int DigitValue(char ch) => ch <= '9' ? ch - '0' : ch - 'a' + 10;
}
