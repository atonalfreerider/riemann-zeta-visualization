using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Moving pictures for the 144 modules (xtorus, xlegendre, xfinger, xclock): numbers marked with their turn and step; the
/// 12 × 12 board filling as the primes arrive; diagonals swept across it cell by cell; the board rolled into a torus; the
/// residue pinch rising on the saddle until each prime's height is a Legendre symbol; curves stamping their fingerprints
/// mod 144 one value at a time; the 144 clock's hours appearing one by one. They run on the live root (ExperimentLive.cs),
/// so exp:clear stops and removes them, and they draw with the attention bloom (ExperimentAttention.cs): halos on what
/// moves, flares where something lands.
/// </summary>
public partial class SacksSpiral
{
    /// <summary>Where the modules put the 12 × 12 board (and its torus) and the fingerprint board, from the saddle's centre.</summary>
    public const float BoardOffset = 7000f;
    public const float FingerOffset = 8200f;

    static string Base12(long n)
    {
        const string digits = "0123456789ab";
        if (n <= 0) return "0";
        string s = "";
        while (n > 0) { s = digits[(int)(n % 12)] + s; n /= 12; }
        return s;
    }

    static long TurnOf(long n)
    {
        long k = (long)System.Math.Sqrt(n);
        while (k * k > n) k--;
        while ((k + 1) * (k + 1) <= n) k++;
        return k;
    }

    // ================================================================================================
    // turn and step
    // ================================================================================================

    /// <summary>Each number with its turn (the ring it sits on) and its step (the arc from the square that starts the ring), glowing.</summary>
    public void ExpTurnStepMarks(long[] ns, float labelSize)
    {
        Transform g = ExpGroup("V turn and step").transform;
        ExpQuadratic(g, 1, 0, 0, 1f, Count, SquareColor, .08f, false, .1f);
        foreach (long n in ns)
        {
            long k = TurnOf(n), st = n - k * k;
            List<Vector3> arc = new();
            int steps = Mathf.Max(8, (int)st * 8);
            for (int i = 0; i <= steps; i++) arc.Add(ExpNumberAt(k * k + st * i / (double)steps, .3f));
            LineRenderer l = ExpLine(g, "step of " + n, .16f, TorusAllowed, Factory.GlowIntensity, arc.ToArray());
            GlowLine(l, 2f, .6f);
            ExpGlowMark(g, ExpNumberAt(k * k, .35f), 1f, .55f);
            ExpGlowMark(g, ExpNumberAt(n, .35f), 1.6f);
            Vector3 at = ExpNumberAt(n, .5f);
            // labels run to the left of points left of the centre and of points near the right-hand rim, to the right otherwise,
            // so labels of different numbers never cross and none leaves the frame
            float dx = at.x - origin.x;
            bool leftward = dx < 0f || dx > .4f * Extent;
            string turn = k >= 12 ? $"turn {k} → {k % 12}" : $"turn {k}", stepText = st >= 12 ? $"step {st} → {st % 12}" : $"step {st}";
            ExpLabelWide(g, $"{n} = {k}² + {st}\n{turn}, {stepText}", at + new Vector3(leftward ? -2f : 2f, .12f * labelSize + 1.5f, -.5f),
                labelSize, Color.white, leftward ? TextAlignmentOptions.Right : TextAlignmentOptions.Left);
        }
    }

    // ================================================================================================
    // the board fills
    // ================================================================================================

    public void ExpBoardFillLive(Vector3 center, float cell, long nStat, float seconds, float labelSize)
    {
        StartLive(BoardFill(center, cell, nStat, seconds, labelSize));
    }

    IEnumerator BoardFill(Vector3 center, float cell, long nStat, float seconds, float labelSize)
    {
        Transform g = SectionLiveRoot();
        ExpEnsureSieve(nStat + 1);
        TorusBoardLabels(g, center, cell, labelSize);
        List<(Vector3, Vector3, Vector3, Color)> quads = new();
        for (int r = 0; r < 12; r++)
        for (int c = 0; c < 12; c++)
            quads.Add((BoardCell(center, cell, r, c) + Vector3.forward * .05f, Vector3.right * cell * .46f, Vector3.up * cell * .46f, new Color(.09f, .08f, .1f)));
        Mesh mesh = ExpQuads(g, "filling cells", quads, Factory.SoftGlowIntensity).GetComponent<MeshFilter>().sharedMesh;
        Color[] cols = new Color[144 * 4];
        long[,] cnt = new long[12, 12];
        TextBox counter = ExpLabelWide(g, "", BoardCell(center, cell, 12.6f, 5.5f) + Vector3.down * (.08f * labelSize), labelSize * .7f, Color.white);
        long done = 1, primes = 0, inGold = 0;
        float time = 0f;
        while (true)
        {
            float u = seconds <= 0f ? 1f : Mathf.Clamp01(time / seconds);
            long target = (long)System.Math.Min(nStat, System.Math.Pow(10, 1 + 6 * Mathf.SmoothStep(0f, 1f, u)));
            for (long n = done + 1; n <= target; n++)
            {
                if (!ExpPrime(n)) continue;
                long k = TurnOf(n);
                int r = (int)(k % 12), c = (int)((n - k * k) % 12);
                cnt[r, c]++;
                primes++;
                if (Allowed12(r, c))
                {
                    inGold++;
                    continue;
                }

                // 2 and 3: the only primes off the gold cells
                Vector3 at = BoardCell(center, cell, r, c) + Vector3.back * .4f;
                ExpRing(g, "exception " + n, at, cell * .44f, Color.white, cell * .06f, AttentionIntensity);
                ExpLabelWide(g, n.ToString(), at + Vector3.back * .2f, labelSize * .9f, Color.white);
                Flare(at, cell * .5f, 1.4f, 2);
            }

            done = target;
            long max = 1;
            for (int r = 0; r < 12; r++) for (int c = 0; c < 12; c++) max = System.Math.Max(max, cnt[r, c]);
            for (int r = 0; r < 12; r++)
            for (int c = 0; c < 12; c++)
            {
                bool a = Allowed12(r, c);
                Color col = cnt[r, c] == 0 ? (a ? new Color(.12f, .1f, .08f) : TorusForbidden) : BoardCellColor(a, cnt[r, c] / (float)max);
                int i = (r * 12 + c) * 4;
                cols[i] = cols[i + 1] = cols[i + 2] = cols[i + 3] = col;
            }

            mesh.colors = cols;
            counter.Text = $"n ≤ {done:N0}:   {primes:N0} primes, {inGold:N0} in the 48 gold cells";
            if (u >= 1f) break;
            time += Time.deltaTime;
            yield return null;
        }

        liveRoutine = null;
    }

    // ================================================================================================
    // a diagonal swept across the board, cell by cell
    // ================================================================================================

    public void ExpDiagonalSweepLive(Vector3 center, float cell, int[] deltas, float seconds, float labelSize)
    {
        StartLive(DiagonalSweep(center, cell, deltas, seconds, labelSize));
    }

    IEnumerator DiagonalSweep(Vector3 center, float cell, int[] deltas, float seconds, float labelSize)
    {
        Transform g = ExpGroup("V diagonal sweep").transform; // the lines stay until exp:clear
        GameObject head = Factory.NewSphere("sweep head", cell * .32f, Color.white, AttentionIntensity);
        head.transform.SetParent(LiveRoot(), false);
        float per = seconds / Mathf.Max(1, deltas.Length);
        float step = per * .8f / 12f;
        foreach (int delta in deltas)
        {
            int rows = AllowedRows(delta);
            Color col = DiagonalColor(rows), lineCol = rows == 12 ? Color.white : col;
            float width = rows == 12 ? .9f : rows == 0 ? .3f : .55f;
            float glow = rows == 12 ? Factory.GlowIntensity : Factory.SoftGlowIntensity;
            LineRenderer cur = null;
            List<Vector3> pts = new();
            for (int r = 0; r < 12; r++)
            {
                int c = (r + delta) % 12;
                Vector3 at = BoardCell(center, cell, r, c) + Vector3.back * .2f;
                if (cur == null || c == 0)
                {
                    pts = new List<Vector3> { at };
                    cur = ExpLine(g, $"diagonal {delta}", width, lineCol, glow, new[] { at, at });
                    if (rows == 12) GlowLine(cur, 1.6f, .7f);
                    head.transform.position = at;
                    yield return new WaitForSeconds(step * .4f);
                }
                else
                {
                    Vector3 from = pts[^1];
                    Vector3[] buf = new Vector3[pts.Count + 1];
                    pts.CopyTo(buf);
                    float tt = 0f;
                    while (tt < step)
                    {
                        Vector3 p = Vector3.Lerp(from, at, Mathf.SmoothStep(0f, 1f, tt / step));
                        buf[^1] = p;
                        cur.positionCount = buf.Length;
                        cur.SetPositions(buf);
                        head.transform.position = p;
                        tt += Time.deltaTime;
                        yield return null;
                    }

                    pts.Add(at);
                    cur.positionCount = pts.Count;
                    cur.SetPositions(pts.ToArray());
                }

                if (Allowed12(r, c)) Flare(at + Vector3.back * .2f, cell * .3f, .7f, 1);
                else ExpRing(g, "miss", at + Vector3.back * .1f, cell * .36f, DeadColor, cell * .05f, Factory.GlowIntensity);
            }

            if (labelSize > 0f)
                ExpLabelWide(g, $"c ≡ {delta}: ×{rows / 4f:0.#}", BoardCell(center, cell, 12.2f + (delta % 4 == 1 ? 0f : .8f), (delta + 11) % 12), labelSize * .62f, col);
            yield return new WaitForSeconds(per * .15f);
        }

        if (head != null) Destroy(head);
        liveRoutine = null;
    }

    // ================================================================================================
    // the board rolled into a torus
    // ================================================================================================

    /// <summary>
    /// A point of the board at arc lengths (su along the turn, sv along the step) on the torus of curvatures ku (the long way)
    /// and kv (the short way), relative to the point (φ, ψ) = (0, 0), in the torus' own frame (axis Z, that point on +X).
    /// Zero curvature is the flat board, in the plane of Y (su) and −Z (sv).
    /// </summary>
    static Vector3 RollPoint(double su, double sv, double ku, double kv)
    {
        double phi = ku * su, psi = kv * sv;
        double cphi = System.Math.Cos(phi), sphi = System.Math.Sin(phi), cpsi = System.Math.Cos(psi), spsi = System.Math.Sin(psi);
        double rCosPhiMinus1 = ku < 1e-9 ? -ku * su * su / 2 : (cphi - 1) / ku; // R (cos φ − 1)
        double rSinPhi = ku < 1e-9 ? su : sphi / ku;                         // R sin φ
        double rCosPsiMinus1 = kv < 1e-9 ? -kv * sv * sv / 2 : (cpsi - 1) / kv; // r (cos ψ − 1)
        double rSinPsi = kv < 1e-9 ? sv : spsi / kv;                         // r sin ψ
        double r = kv < 1e-9 ? 0 : 1 / kv;
        double x = rCosPhiMinus1 + cphi * rCosPsiMinus1 + r * (cphi - 1);
        double y = rSinPhi + r * cpsi * sphi;
        return new Vector3((float)x, (float)y, (float)-rSinPsi);
    }

    public void ExpTorusRollLive(Vector3 center, float cell, float majorR, float seconds)
    {
        StartLive(TorusRoll(center, cell, majorR, seconds));
    }

    IEnumerator TorusRoll(Vector3 center, float cell, float majorR, float seconds)
    {
        Transform g = SectionLiveRoot();
        float W = 12f * cell;
        double kvEnd = 2 * System.Math.PI / W;                 // the short way closes into a circle of circumference W
        double stretchEnd = 2 * System.Math.PI * majorR / W;  // the long way stretches to the circle of radius majorR
        double rEnd = 1 / kvEnd;
        int[,] cnt = TorusPrimeCounts(10000000);
        int max = 1;
        for (int r = 0; r < 12; r++) for (int c = 0; c < 12; c++) max = Mathf.Max(max, cnt[r, c]);
        // cells, each split 3 × 3 so the rolled cells bend smoothly
        const int sub = 3;
        List<(float r0, float c0)> corners = new();
        List<(Vector3, Vector3, Vector3, Color)> quads = new();
        for (int r = 0; r < 12; r++)
        for (int c = 0; c < 12; c++)
        {
            Color col = BoardCellColor(Allowed12(r, c), cnt[r, c] / (float)max);
            for (int i = 0; i < sub; i++)
            for (int j = 0; j < sub; j++)
            {
                float a0 = .04f + .92f * i / sub, b0 = .04f + .92f * j / sub;
                corners.Add((r + a0, c + b0));
                quads.Add((Vector3.zero, Vector3.zero, Vector3.zero, col));
            }
        }

        Mesh mesh = ExpQuads(g, "rolling cells", quads, Factory.SoftGlowIntensity).GetComponent<MeshFilter>().sharedMesh;
        Vector3[] verts = new Vector3[quads.Count * 4];
        float d = .92f / sub;
        // the two gold diagonals, as knots
        List<LineRenderer> knots = new();
        List<List<(float rr, float cc)>> knotRuns = new();
        foreach (int delta in new[] { 5, 11 })
        {
            List<(float, float)> run = new();
            for (int i = 0; i <= 96; i++)
            {
                float rr = 12f * i / 96f, cc = (rr + delta) % 12f;
                if (run.Count > 0 && cc < run[^1].Item2 - 6f)
                {
                    run.Add((rr, cc + 12f)); // finish at the right edge, start again at the left
                    knotRuns.Add(run);
                    run = new List<(float, float)> { (rr, cc) };
                    continue;
                }

                run.Add((rr, cc));
            }

            knotRuns.Add(run);
        }

        foreach (List<(float rr, float cc)> run in knotRuns)
        {
            LineRenderer l = ExpLine(g, "gold knot", .7f, Color.white, Factory.GlowIntensity, new Vector3[run.Count]);
            GlowLine(l, 1.6f, .7f);
            knots.Add(l);
        }

        Quaternion q0 = Quaternion.LookRotation(new Vector3(-1f, 0f, 0f), new Vector3(0f, -1f, 0f));
        float time = 0f;
        while (true)
        {
            float u = seconds <= 0f ? 1f : Mathf.Clamp01(time / seconds);
            float t = Mathf.SmoothStep(0f, 1f, u);
            double a = Mathf.Clamp01(t / .5f), b = Mathf.Clamp01((t - .5f) / .5f);
            double kv = a * kvEnd, stretch = 1 + (stretchEnd - 1) * b, ku = b * kvEnd / stretchEnd;
            Quaternion rot = Quaternion.Slerp(q0, Quaternion.identity, t);
            Vector3 shift = new((float)((majorR + rEnd) * t), 0f, 0f);
            Vector3 P(float rr, float cc) => center + rot * (RollPoint(cell * (rr - 6f) * stretch, cell * (cc - 6f), ku, kv) + shift);
            for (int qi = 0; qi < corners.Count; qi++)
            {
                (float r0, float c0) = corners[qi];
                verts[4 * qi] = P(r0, c0);
                verts[4 * qi + 1] = P(r0, c0 + d);
                verts[4 * qi + 2] = P(r0 + d, c0 + d);
                verts[4 * qi + 3] = P(r0 + d, c0);
            }

            mesh.vertices = verts;
            mesh.RecalculateBounds();
            for (int k = 0; k < knots.Count; k++)
            {
                List<(float rr, float cc)> run = knotRuns[k];
                for (int i = 0; i < run.Count; i++) knots[k].SetPosition(i, P(run[i].rr, run[i].cc) + rot * (Vector3.right * .3f)); // natural +X faces the camera on the flat board
            }

            if (u >= 1f) break;
            time += Time.deltaTime;
            yield return null;
        }

        liveRoutine = null;
    }

    // ================================================================================================
    // the residue pinch rising on the saddle: heights become Legendre symbols
    // ================================================================================================

    static System.Func<int, int> CharacterFor(string code) => code switch
    {
        "2" => n => n % 2 == 0 ? 0 : n % 8 == 1 || n % 8 == 7 ? 1 : -1,
        "5" => n => n % 5 == 0 ? 0 : n % 5 == 1 || n % 5 == 4 ? 1 : -1,
        _ => n => n % 3 == 0 ? 0 : n % 12 == 1 || n % 12 == 11 ? 1 : n % 2 == 0 ? 0 : -1
    };

    /// <summary>
    /// The pinch to m arms with the primes coloured by a character (cyan +1, magenta −1, white 0). With <paramref name="slide"/>
    /// the numbers slide round their rings to the arms; with <paramref name="rise"/> the bend then grows from 0 to
    /// <paramref name="bendTo"/>, so every prime rises or falls by its character. The arms ride the surface.
    /// </summary>
    public void ExpCharacterLive(int m, string code, float seconds, bool slide, bool rise, float bendTo, float labelSize)
    {
        StartLive(CharacterLive(m, code, seconds, slide, rise, bendTo, labelSize));
    }

    IEnumerator CharacterLive(int m, string code, float seconds, bool slide, bool rise, float bendTo, float labelSize)
    {
        Transform g = SectionLiveRoot();
        System.Func<int, int> chi = CharacterFor(code);
        List<int> ns = new(); List<Color> cols = new(); List<float> sizes = new();
        int[] count = new int[m];
        for (int n = 2; n <= Count; n++)
        {
            bool p = IsPrime(n);
            if (p) count[n % m]++;
            int x = p ? chi(n) : 0;
            ns.Add(n);
            cols.Add(p ? (x > 0 ? ChiPlus : x < 0 ? ChiMinus : Color.white) : new Color(.26f, .26f, .32f));
            sizes.Add(p ? .32f : .1f);
        }

        List<Vector3> zero = new(new Vector3[ns.Count]);
        Mesh mesh = ExpPointCloud(g, "pinching", zero, cols, sizes, Factory.GlowIntensity).GetComponent<MeshFilter>().sharedMesh;
        Vector3[] verts = new Vector3[ns.Count * 6];
        Vector3[] axes = { Vector3.right, Vector3.up, Vector3.forward };
        float rMax = R * Mathf.Sqrt(Count);
        List<(int j, LineRenderer line, AttentionHalo halo, TextBox label)> arms = new();
        for (int j = 0; j < m; j++)
        {
            if (Gcd(j, m) != 1) continue;
            Color c = chi(j) > 0 ? ChiPlus : ChiMinus;
            LineRenderer l = ExpLine(g, "arm " + j, m > 24 ? .05f : .08f, c, Factory.GlowIntensity, new Vector3[40]);
            AttentionHalo h = m <= 24 ? Halo(l, 2f, 0f) : null;
            TextBox lab = labelSize > 0f && m <= 24 ? ExpLabelWide(g, $"{j}: {count[j]}", Vector3.zero, labelSize * .8f, c) : null;
            arms.Add((j, l, h, lab));
        }

        if (rise) bend = 0f;
        float time = 0f;
        while (true)
        {
            float u = seconds <= 0f ? 1f : Mathf.Clamp01(time / seconds);
            float s = !slide ? 1f : rise ? Ease(u / .5f) : Ease(u);
            float rising = !rise ? 0f : slide ? Mathf.Clamp01((u - .5f) / .5f) : u;
            if (rise) bend = bendTo * Ease(rising);
            for (int i = 0; i < ns.Count; i++)
            {
                Vector3 p = ResidueAt(ns[i], m, s, .1f);
                float sz = sizes[i];
                verts[6 * i] = p + axes[0] * sz; verts[6 * i + 1] = p - axes[0] * sz;
                verts[6 * i + 2] = p + axes[1] * sz; verts[6 * i + 3] = p - axes[1] * sz;
                verts[6 * i + 4] = p + axes[2] * sz; verts[6 * i + 5] = p - axes[2] * sz;
            }

            mesh.vertices = verts;
            mesh.RecalculateBounds();
            float shown = slide ? Mathf.Clamp01((s - .7f) / .3f) : 1f; // the arms appear as the numbers arrive on them
            foreach ((int j, LineRenderer line, AttentionHalo halo, TextBox label) in arms)
            {
                float th = 2f * Mathf.PI * j / m;
                for (int i = 0; i < 40; i++) line.SetPosition(i, ExpAt(Mathf.Lerp(R, rMax, i / 39f), th, .05f));
                Color c = chi(j) > 0 ? ChiPlus : ChiMinus;
                line.startColor = line.endColor = new Color(c.r, c.g, c.b, shown);
                line.enabled = shown > .01f;
                if (halo != null) halo.Strength = rise ? .55f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(rising)) : 0f;
                if (label != null)
                {
                    label.gameObject.SetActive(shown > .5f);
                    label.transform.position = ExpAt(rMax + .12f * labelSize + 1.5f, th, 0f);
                }
            }

            if (u >= 1f) break;
            time += Time.deltaTime;
            yield return null;
        }

        liveRoutine = null;
    }

    // ================================================================================================
    // fingerprints mod 144, stamped one value at a time
    // ================================================================================================

    static (System.Func<long, long> f, string title) FingerCurve(string code) => code switch
    {
        "sq" => (k => k * k, "k²"),
        "euler" => (k => k * k + k + 41, "k² + k + 41"),
        "17" => (k => k * k + k + 17, "k² + k + 17"),
        "k2p1" => (k => k * k + 1, "k² + 1"),
        "k2k1" => (k => k * k + k + 1, "k² + k + 1"),
        "pronic" => (k => k * (k + 1), "k(k + 1)"),
        "u4u1" => (k => k * (4 * k + 1), "u(4u + 1)"),
        "primes" => (null, "primes"),
        _ => (null, "the last two base-12 digits")
    };

    /// <summary>The empty fingerprint board: cells, the four prime columns (1, 5, 7, b) marked, digits along both edges.</summary>
    Mesh FingerBoard(Transform g, Vector3 center, float cell, string title, float labelSize)
    {
        List<(Vector3, Vector3, Vector3, Color)> quads = new();
        for (int r = 0; r < 144; r++)
            quads.Add((BoardCell(center, cell, r / 12, r % 12) + Vector3.forward * .05f, Vector3.right * cell * .44f, Vector3.up * cell * .44f, Gcd(r, 6) == 1 ? new Color(.12f, .11f, .08f) : new Color(.06f, .06f, .08f)));
        Mesh mesh = ExpQuads(g, "fingerprint cells", quads, Factory.SoftGlowIntensity).GetComponent<MeshFilter>().sharedMesh;
        foreach (int col in new[] { 1, 5, 7, 11 })
            ExpLine(g, "prime column", .08f, new Color(.6f, .55f, .4f), Factory.NoGlow, new[] { BoardCell(center, cell, -.7f, col) + Vector3.back * .1f, BoardCell(center, cell, 11.7f, col) + Vector3.back * .1f });
        const string digits = "0123456789ab";
        if (labelSize <= 0f) return mesh;
        for (int i = 0; i < 12; i++)
        {
            ExpLabel(g, digits[i].ToString(), BoardCell(center, cell, 12.05f, i), labelSize * .55f, new Color(.7f, .7f, .8f));
            ExpLabel(g, digits[i].ToString(), BoardCell(center, cell, i, -1f), labelSize * .55f, new Color(.7f, .7f, .8f));
        }

        ExpLabelWide(g, title, BoardCell(center, cell, -1.4f, 5.5f) + Vector3.up * (.1f * labelSize), labelSize, Color.white);
        ExpLabelWide(g, "units digit →", BoardCell(center, cell, 12.9f, 5.5f), labelSize * .55f, new Color(.7f, .7f, .8f));
        ExpLabelWide(g, "dozens\ndigit ↓", BoardCell(center, cell, -.9f, -1.3f), labelSize * .5f, new Color(.7f, .7f, .8f));
        return mesh;
    }

    public void ExpFingerprintLive(Vector3 center, float cell, string code, float seconds, float labelSize)
    {
        StartLive(FingerprintLive(center, cell, code, seconds, labelSize));
    }

    IEnumerator FingerprintLive(Vector3 center, float cell, string code, float seconds, float labelSize)
    {
        Transform g = SectionLiveRoot();
        (System.Func<long, long> f, string title) = FingerCurve(code);
        Mesh mesh = FingerBoard(g, center, cell, title, labelSize);
        Color[] cols = mesh.colors;
        int[] hits = new int[144];
        TextBox counter = labelSize > 0f ? ExpLabelWide(g, "", BoardCell(center, cell, 14f, 5.5f) + Vector3.down * (.08f * labelSize), labelSize * .7f, Color.white) : null;
        void Paint(int max)
        {
            for (int r = 0; r < 144; r++)
            {
                if (hits[r] == 0) continue;
                bool unit = Gcd(r, 6) == 1;
                float k = hits[r] / (float)max;
                Color c = unit ? Color.Lerp(new Color(.4f, .3f, .05f), TorusAllowed, .45f + .55f * k) : Color.Lerp(new Color(.25f, .08f, .08f), DeadColor, .4f + .6f * k);
                cols[4 * r] = cols[4 * r + 1] = cols[4 * r + 2] = cols[4 * r + 3] = c;
            }

            mesh.colors = cols;
        }

        if (code == "empty")
        {
            liveRoutine = null;
            yield break;
        }

        if (f == null)
        {
            // the primes, arriving in ever larger batches up to a million
            const long nTop = 1000000;
            ExpEnsureSieve(nTop + 1);
            long done = 4, primes = 0;
            float time = 0f;
            while (true)
            {
                float u = seconds <= 0f ? 1f : Mathf.Clamp01(time / seconds);
                long target = (long)System.Math.Min(nTop, System.Math.Pow(10, 1 + 5 * Mathf.SmoothStep(0f, 1f, u)));
                for (long n = done + 1; n <= target; n++)
                {
                    if (!ExpPrime(n)) continue;
                    int r = (int)(n % 144);
                    if (hits[r] == 0) Flare(BoardCell(center, cell, r / 12, r % 12) + Vector3.back * .3f, cell * .3f, .8f, 1);
                    hits[r]++;
                    primes++;
                }

                done = target;
                int max = 1;
                foreach (int h in hits) max = Mathf.Max(max, h);
                Paint(max);
                if (counter != null) counter.Text = $"primes from 5 to {done:N0}: {primes:N0}";
                if (u >= 1f) break;
                time += Time.deltaTime;
                yield return null;
            }

            liveRoutine = null;
            yield break;
        }

        // a curve: k = 0 … 143, one value at a time
        float per = seconds * .9f / 144f;
        int visited = 0, units = 0;
        for (long k = 0; k < 144; k++)
        {
            long v = f(k);
            int r = (int)(((v % 144) + 144) % 144);
            Vector3 at = BoardCell(center, cell, r / 12, r % 12) + Vector3.back * .3f;
            if (hits[r] == 0)
            {
                visited++;
                if (Gcd(r, 6) == 1) units++;
                Flare(at, cell * .3f, .7f, 1);
            }

            hits[r]++;
            int max = 1;
            foreach (int h in hits) max = Mathf.Max(max, h);
            Paint(max);
            if (counter != null)
            {
                string b12 = Base12(v);
                string head = b12.Length > 2 ? b12[..^2] : "", tail = b12.Length > 2 ? b12[^2..] : b12.PadLeft(2, '0');
                counter.Text = $"k = {k}:   {v:N0} = {head}<color=#FFD24D>{tail}</color> in base 12\n{visited} cells, {units} in the prime columns";
            }

            yield return new WaitForSeconds(per);
        }

        liveRoutine = null;
    }

    /// <summary>All eight fingerprints side by side (two columns of four).</summary>
    public void ExpFingerprintAll(Vector3 at, float labelSize)
    {
        float fx = 16.5f, cellF = 2.2f;
        ExpFingerprint(at + new Vector3(-fx, 66f, 0f), cellF, null, 10000000, "primes", labelSize, true);
        ExpFingerprint(at + new Vector3(fx, 66f, 0f), cellF, k => k * k, 1440, "k²", labelSize);
        ExpFingerprint(at + new Vector3(-fx, 22f, 0f), cellF, k => k * k + k + 41, 1440, "k² + k + 41", labelSize);
        ExpFingerprint(at + new Vector3(fx, 22f, 0f), cellF, k => k * k + k + 17, 1440, "k² + k + 17", labelSize);
        ExpFingerprint(at + new Vector3(-fx, -22f, 0f), cellF, k => k * k + 1, 1440, "k² + 1", labelSize);
        ExpFingerprint(at + new Vector3(fx, -22f, 0f), cellF, k => k * k + k + 1, 1440, "k² + k + 1", labelSize);
        ExpFingerprint(at + new Vector3(-fx, -66f, 0f), cellF, k => k * (k + 1), 1440, "k(k + 1)", labelSize);
        ExpFingerprint(at + new Vector3(fx, -66f, 0f), cellF, k => k * (4 * k + 1), 1440, "u(4u + 1)", labelSize);
    }

    // ================================================================================================
    // the 144 clock, hour by hour
    // ================================================================================================

    public void ExpClockLive(int nStat, float seconds, float labelSize)
    {
        StartLive(ClockLive(nStat, seconds, labelSize));
    }

    IEnumerator ClockLive(int nStat, float seconds, float labelSize)
    {
        // build the whole clock, hidden, then reveal it hour by hour
        ExpClock144(Count, nStat, labelSize);
        Transform g = ExpRoot().Find("Y clock 144");
        if (g == null) { liveRoutine = null; yield break; }
        List<(int hour, List<Transform> parts)> hours = new();
        for (int j = 0; j < 24; j++) hours.Add((j, new List<Transform>()));
        foreach (Transform t in g)
        {
            // rays are named "ray j"; families "u(q²u+d)"; labels are text boxes in hour order after their family
            if (t.name.StartsWith("ray ") && int.TryParse(t.name.Substring(4), out int j)) hours[j].parts.Add(t);
        }

        // families and labels: match by order of creation (ray, family, [halo], label) — collect everything between rays
        int current = -1;
        foreach (Transform t in g)
        {
            if (t.name.StartsWith("ray ") && int.TryParse(t.name.Substring(4), out int j)) { current = j; continue; }
            if (current >= 0) hours[current].parts.Add(t);
        }

        foreach ((int hour, List<Transform> parts) in hours) foreach (Transform p in parts) p.gameObject.SetActive(false);
        float per = seconds * .9f / 24f;
        for (int j = 0; j < 24; j++)
        {
            foreach (Transform p in hours[j].parts)
            {
                p.gameObject.SetActive(true);
                LineRenderer l = p.GetComponent<LineRenderer>();
                if (l != null && l.positionCount > 2) Halo(l, 2f, 1f).Decay = 1.2f;
            }

            yield return new WaitForSeconds(per);
        }

        liveRoutine = null;
    }

    /// <summary>A white pulse on the clock's families: the prime-free hours ("red") or the hours with primes ("orange").</summary>
    public void ExpClockPulse(bool free)
    {
        foreach ((LineRenderer line, bool isFree) in clockFamilies)
        {
            if (line == null || isFree != free) continue;
            AttentionHalo h = Halo(line, 2.2f, 1f);
            h.Decay = .45f;
        }
    }

    /// <summary>A quadratic drawn over everything with a white glow (a curve the line is talking about).</summary>
    public void ExpQuadGlow(int q, int B, long C, Color color)
    {
        Transform g = ExpGroup("glow " + Poly(q, B, C)).transform;
        ExpQuadratic(g, q, B, C, 0f, Count, color, .1f, true, .2f, .34f);
        LineRenderer l = g.Find(Poly(q, B, C))?.GetComponent<LineRenderer>();
        if (l != null) GlowLine(l, 2.2f, .7f);
    }
}
