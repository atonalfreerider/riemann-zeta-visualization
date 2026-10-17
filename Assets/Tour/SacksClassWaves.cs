using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Discovery four, step by step (the long-form segment xs4):
///   ExpClassCountTable  the counts of lattice points at 2, 3 and 6 = 2·3 on the two families, and on the class group's
///       combination (first − second)/2: the families' counts cannot be built from the prime factors (0 × 0 ≠ 4), the
///       combination's can ((−1) × (−1) = 1). That is what an Euler product needs.
///   ExpClassWaves / ExpClassRatioLive  on the critical line each family's completed zeta function is a real wave
///       (ClassWaveData); a cursor sweeps t, and the ratio −Λ₁/Λ₂ is drawn from the two waves' values, breaking into
///       hyperbola-like branches at the second wave's zeros (its poles): the curve of discovery four's chart.
///   ExpClassTerrain  −47's plane of mixtures as a terrain of cells, one mesh per row: lf.build (0 … 1) lays the rows down
///       one after another, lf.h raises each cell by its number of zeros off the line, lf.slice (−3 … 3, off below −9)
///       cuts the terrain at λ₃ = lf.slice and draws that row's profile: a sweep like −23's.
/// </summary>
public partial class SacksSpiral
{
    Vector3 laLatticeOrigin;
    float laLatticeScale = 4.2f;

    /// <summary>For callouts: lattice point (x, y) of family k (exp:calllat:K,X,Y), where the lattices were drawn.</summary>
    public Vector3 LatticePoint(int k, int x, int y)
    {
        k = Mathf.Clamp(k, 0, 2);
        Vector3 c = laLatticeOrigin + new Vector3(-98f + 98f * k, 0f, 0f);
        Vector2 p = FormPoint(Forms23[k], x, y);
        return c + new Vector3(p.x, p.y, -.3f) * laLatticeScale;
    }

    // ---- the counts at 2, 3 and 6 ------------------------------------------------------------------------------------

    /// <summary>A table at o: n, the points on each family's ring of n, and (first − second)/2; then the two checks.</summary>
    public void ExpClassCountTable(Vector3 o, float labelSize)
    {
        Transform g = ExpGroup("LA counts").transform;
        float[] col = { 0f, 34f, 78f, 126f };
        float row = labelSize * .03f;
        Color head = new(.82f, .85f, .95f);
        Transform Part(string name)
        {
            Transform t = new GameObject(name).transform;
            t.SetParent(g, false);
            return t;
        }

        void Cell(Transform parent, string text, int c, float r, Color colr, float size = 1f)
        {
            TextBox l = ExpLabelWide(parent, text, o + new Vector3(col[c], -r * row, 0f), labelSize * .19f * size, colr);
            Outline(l.TextField);
        }

        Transform h = Part("header");
        Cell(h, "n", 0, 0, head);
        Cell(h, "points on\nx² + xy + 6y²", 1, 0, FormColors[0]);
        Cell(h, "points on\n2x² + xy + 3y²", 2, 0, FormColors[1]);
        Cell(h, "(first − second) / 2", 3, 0, PrimeGold);
        ExpLine(h, "rule", .12f, head * .6f, Factory.NoGlow, new[] { o + new Vector3(-12f, -row * .75f, 0f), o + new Vector3(col[3] + 30f, -row * .75f, 0f) });
        (int n, int e, int c, string comb, Color nc)[] rows = { (2, 0, 2, "−1", new Color(1f, .62f, .2f)), (3, 0, 2, "−1", new Color(.78f, .55f, 1f)), (6, 4, 2, "+1", new Color(1f, .4f, .35f)) };
        for (int i = 0; i < rows.Length; i++)
        {
            var (n, e, c, comb, nc) = rows[i];
            Transform r = Part($"row {n}");
            float y = 1.6f + i * 1.15f;
            Cell(r, n == 6 ? "6 = 2·3" : n.ToString(), 0, y, nc, 1.15f);
            Cell(r, e.ToString(), 1, y, FormColors[0], 1.15f);
            Cell(r, c.ToString(), 2, y, FormColors[1], 1.15f);
            Cell(r, comb, 3, y, PrimeGold, 1.15f);
        }

        Transform k0 = Part("check 0");
        TextBox t0 = ExpLabelWide(k0, "the first family:  2 → 0,  3 → 0,  but 6 → 4:   not built from 2 and 3", o + new Vector3(col[1] + 20f, -row * 5.3f, 0f),
            labelSize * .17f, new Color(1f, .5f, .45f));
        Outline(t0.TextField);
        Transform k1 = Part("check 1");
        TextBox t1 = ExpLabelWide(k1, "(first − second)/2:  (−1) × (−1) = +1:   6 is built from 2 and 3", o + new Vector3(col[1] + 20f, -row * 6.5f, 0f),
            labelSize * .17f, PrimeGold);
        Outline(t1.TextField);
        foreach (Transform t in g) t.gameObject.SetActive(false);
    }

    // ---- the two waves and the ratio ---------------------------------------------------------------------------------

    const float CwX0 = -140f, CwW = 190f, CwSy = 12f;
    Vector3 cwOrigin;
    float cwAmp = 2f;

    Vector3 CwP(Vector3 o, double t, double y) => o + new Vector3(CwX0 + (float)(t / 50d) * CwW, (float)y, 0f);

    /// <summary>The two families' real waves on the critical line, over the chart of mixtures' t axis (group "LW waves").</summary>
    public void ExpClassWaves(Vector3 o, float labelSize)
    {
        cwOrigin = o;
        Transform g = ExpGroup("LW waves").transform;
        float[] e = ClassWaveData.E, c = ClassWaveData.C;
        double top = 1e-9;
        for (int i = 0; i < e.Length; i++) top = Math.Max(top, Math.Max(Math.Abs(e[i]), Math.Abs(c[i])));
        cwAmp = (float)(34.0 / Math.Min(top, 12.0));
        Vector3[] pe = new Vector3[e.Length], pc = new Vector3[c.Length];
        for (int i = 0; i < e.Length; i++)
        {
            double t = ClassWaveData.T0 + ClassWaveData.TStep * i;
            pe[i] = CwP(o, t, Mathf.Clamp(e[i] * cwAmp, -40f, 40f)) + Vector3.back * .1f;
            pc[i] = CwP(o, t, Mathf.Clamp(c[i] * cwAmp, -40f, 40f)) + Vector3.back * .12f;
        }

        Color axisCol = new(.6f, .65f, .82f);
        ExpLine(g, "t axis", .14f, axisCol, Factory.NoGlow, new[] { CwP(o, 0, 0), CwP(o, 50.5, 0) });
        ExpLine(g, "first", .32f, FormColors[0], Factory.GlowIntensity, pe);
        ExpLine(g, "second", .32f, FormColors[1], Factory.GlowIntensity, pc);
        TextBox n0 = ExpLabelWide(g, "Λ of x² + xy + 6y²", CwP(o, 0, 0) + new Vector3(-3f, 12f, -.2f), labelSize * .17f, FormColors[0], TextAlignmentOptions.Right);
        TextBox n1 = ExpLabelWide(g, "Λ of 2x² ± xy + 3y²", CwP(o, 0, 0) + new Vector3(-3f, -12f, -.2f), labelSize * .17f, FormColors[1], TextAlignmentOptions.Right);
        TextBox tl = ExpLabelWide(g, "t", CwP(o, 51.5, 0) + Vector3.back * .2f, labelSize * .17f, axisCol, TextAlignmentOptions.Left);
        foreach (TextBox l in new[] { n0, n1, tl }) Outline(l.TextField);
        n0.gameObject.name = "name first";
        n1.gameObject.name = "name second";
        tl.gameObject.name = "t label";
        // their zeros on the axis
        List<Vector3> dots = new(); List<Color> dc = new(); List<float> ds = new();
        foreach (double z in ClassGroupData.OnM23e) { dots.Add(CwP(o, z, 0) + Vector3.back * .3f); dc.Add(FormColors[0]); ds.Add(.9f); }
        foreach (double z in ClassGroupData.OnM23c) { dots.Add(CwP(o, z, 0) + Vector3.back * .32f); dc.Add(FormColors[1]); ds.Add(.9f); }
        ExpPointCloud(g, "zeros", dots, dc, ds, Factory.GlowIntensity);
        foreach (Transform t in g) t.gameObject.SetActive(false);
    }

    /// <summary>
    /// A cursor sweeps t from 0 to 50: at each t the ratio −Λ₁/Λ₂ of the two waves is drawn (white), broken where the
    /// second wave crosses zero (a dashed pole line rises there); the waves dim as the curve comes.
    /// </summary>
    public void ExpClassRatioLive(float seconds, float labelSize) => StartLive(ClassRatioRoutine(seconds, labelSize));

    IEnumerator ClassRatioRoutine(float seconds, float labelSize)
    {
        Transform g = LiveRoot();
        Vector3 o = cwOrigin;
        float[] e = ClassWaveData.E, c = ClassWaveData.C;
        const float lo = -3f, hi = 4f;
        LineRenderer cursor = ExpLine(g, "cursor", .22f, Color.white, AttentionIntensity, new[] { CwP(o, 0, lo * CwSy), CwP(o, 0, hi * CwSy) });
        OnTop(cursor, 20);
        GameObject dotE = ExpDot(g, "value first", o, 1.2f, FormColors[0], AttentionIntensity);
        GameObject dotC = ExpDot(g, "value second", o, 1.2f, FormColors[1], AttentionIntensity);
        GameObject dotR = ExpDot(g, "value ratio", o, 1.3f, Color.white, AttentionIntensity);
        TextBox tag = ExpLabelWide(g, "", o, labelSize * .17f, Color.white, TextAlignmentOptions.Left);
        Outline(tag.TextField);
        List<Vector3> run = new();
        LineRenderer cur = null;
        int part = 0;
        Transform waves = ExpRoot().Find("LW waves");
        int last = -1;
        float time = 0f;
        while (true)
        {
            if (cursor == null) yield break;
            float u = Mathf.Clamp01(time / seconds);
            int i1 = Mathf.Min(e.Length - 1, Mathf.FloorToInt(u * (e.Length - 1)));
            for (int i = last + 1; i <= i1; i++)
            {
                double t = ClassWaveData.T0 + ClassWaveData.TStep * i;
                double r = Math.Abs(c[i]) < 1e-9 ? 1e9 : -e[i] / c[i];
                bool pole = i > 0 && c[i] * c[i - 1] < 0f;
                if (pole)
                {
                    // the second wave crossed zero: the ratio jumps through infinity; a dashed line marks the pole
                    double tz = t - ClassWaveData.TStep * c[i] / (c[i] - c[i - 1]);
                    List<(Vector3, Vector3)> dash = new();
                    for (float y = lo; y < hi; y += .5f) dash.Add((CwP(o, tz, y * CwSy) + Vector3.back * .15f, CwP(o, tz, Mathf.Min(hi, y + .25f) * CwSy) + Vector3.back * .15f));
                    Segments(g, $"pole {part}", dash, .12f, new Color(.45f, .8f, 1f, .7f), Factory.SoftGlowIntensity);
                    Flare(CwP(o, tz, 0) + Vector3.back * .4f, 3f, .8f, 1);
                    cur = null;
                    run.Clear();
                }

                bool inside = r >= lo && r <= hi;
                if (!inside)
                {
                    if (run.Count > 0 && cur != null)
                    {
                        run.Add(CwP(o, t, Math.Max(lo, Math.Min(hi, r)) * CwSy) + Vector3.back * .2f);
                        cur.positionCount = run.Count;
                        cur.SetPositions(run.ToArray());
                    }

                    cur = null;
                    run.Clear();
                    continue;
                }

                if (cur == null)
                {
                    cur = ExpLine(g, $"ratio {part++}", .3f, new Color(.95f, .97f, 1f), Factory.GlowIntensity, new[] { o, o });
                    OnTop(cur, 18);
                }

                run.Add(CwP(o, t, r * CwSy) + Vector3.back * .2f);
                cur.positionCount = run.Count;
                cur.SetPositions(run.ToArray());
            }

            last = i1;
            double tc = ClassWaveData.T0 + ClassWaveData.TStep * i1;
            Vector3 a = CwP(o, tc, lo * CwSy) + Vector3.back * .3f, b = CwP(o, tc, hi * CwSy) + Vector3.back * .3f;
            cursor.SetPositions(new[] { a, b });
            dotE.transform.position = CwP(o, tc, Mathf.Clamp(e[i1] * cwAmp, -40f, 40f)) + Vector3.back * .4f;
            dotC.transform.position = CwP(o, tc, Mathf.Clamp(c[i1] * cwAmp, -40f, 40f)) + Vector3.back * .4f;
            double rr = Math.Abs(c[i1]) < 1e-9 ? 1e9 : -e[i1] / c[i1];
            dotR.SetActive(rr >= lo && rr <= hi);
            if (rr >= lo && rr <= hi) dotR.transform.position = CwP(o, tc, rr * CwSy) + Vector3.back * .45f;
            tag.transform.position = CwP(o, tc, hi * CwSy) + new Vector3(1.5f, 3f, -.4f);
            tag.TextField.text = Math.Abs(rr) < 99 ? $"t = {tc:0.0}:  −Λ₁/Λ₂ = {rr:0.00}".Replace("-", "−").Replace("−Λ", "−Λ") : $"t = {tc:0.0}:  Λ₂ = 0";
            // the waves dim as the curve comes
            if (waves != null)
                foreach (LineRenderer l in waves.GetComponentsInChildren<LineRenderer>())
                    if (l.name == "first" || l.name == "second") Factory.SetMute(l, Mathf.Clamp01(u * 4f) * .8f);
            if (u >= 1f) break;
            time += Time.deltaTime;
            yield return null;
        }

        if (cursor != null) Destroy(cursor.gameObject);
        if (dotE != null) Destroy(dotE);
        if (dotC != null) Destroy(dotC);
        if (dotR != null) Destroy(dotR);
        if (tag != null) Destroy(tag.gameObject);
        liveRoutine = null;
    }

    // ---- −47's plane of mixtures as a terrain ------------------------------------------------------------------------

    static int[,] fiveOff;
    static int fiveCells;

    static int[,] FiveGrid(int cells)
    {
        if (fiveOff != null && fiveCells == cells) return fiveOff;
        const float lo = -3f, hi = 3f;
        float step = (hi - lo) / cells;
        int[,] off = new int[cells, cells];
        for (int i = 0; i < cells; i++)
            for (int j = 0; j < cells; j++)
            {
                float l2 = lo + (i + .5f) * step, l3 = lo + (j + .5f) * step;
                off[i, j] = Mathf.Max(0, FiveTotal(l2, l3) - FiveOnLine(l2, l3));
            }

        fiveOff = off;
        fiveCells = cells;
        return off;
    }

    sealed class TerrainScene
    {
        public Transform Root, Heights, Scan, Slice;
        public GameObject[] Rows, Walls;
        public LineRenderer ScanLine, Profile;
        public MeshRenderer SliceQuad;
        public Vector3 O;
        public float Size;
        public int Cells, Top;
        public string Sig = "";
    }

    TerrainScene terrain;

    /// <summary>The terrain at o (λ₂, λ₃ ∈ [−3, 3] over size × size), hidden until lf.build rises (group "LF terrain").</summary>
    public void ExpClassTerrain(Vector3 o, float size, int cells, float labelSize)
    {
        ClearTweens("lf.");
        int[,] off = FiveGrid(cells);
        TerrainScene ts = new() { O = o, Size = size, Cells = cells, Root = ExpGroup("LF terrain").transform };
        ts.Heights = new GameObject("heights").transform;
        ts.Heights.SetParent(ts.Root, false);
        int top = 0;
        foreach (int v in off) top = Mathf.Max(top, v);
        ts.Top = top;
        const float lo = -3f, hi = 3f;
        float step = (hi - lo) / cells, unit = size / (hi - lo), half = step * unit * .5f;
        Vector3 At(float l2, float l3, float h) => o + new Vector3((l2 - (lo + hi) * .5f) * unit, (l3 - (lo + hi) * .5f) * unit, -h);
        ts.Rows = new GameObject[cells];
        ts.Walls = new GameObject[cells];
        for (int j = 0; j < cells; j++)
        {
            List<(Vector3 c, Vector3 u, Vector3 v, Color col)> quads = new(), walls = new();
            float l3 = lo + (j + .5f) * step;
            for (int i = 0; i < cells; i++)
            {
                float l2 = lo + (i + .5f) * step;
                int h = off[i, j];
                quads.Add((At(l2, l3, h), Vector3.right * half, Vector3.up * half, FiveHeight(h)));
                if (i + 1 < cells && off[i + 1, j] != h)
                {
                    int h2 = off[i + 1, j];
                    walls.Add((At(l2 + step * .5f, l3, (h + h2) * .5f), Vector3.up * half, Vector3.back * (Mathf.Abs(h - h2) * .5f), FiveHeight(Mathf.Max(h, h2)) * .6f));
                }

                if (j + 1 < cells && off[i, j + 1] != h)
                {
                    int h2 = off[i, j + 1];
                    walls.Add((At(l2, l3 + step * .5f, (h + h2) * .5f), Vector3.right * half, Vector3.back * (Mathf.Abs(h - h2) * .5f), FiveHeight(Mathf.Max(h, h2)) * .6f));
                }
            }

            GameObject rowGo = new($"row {j}");
            rowGo.transform.SetParent(ts.Heights, false);
            ExpQuads(rowGo.transform, "cells", quads, Factory.NoGlow);
            // the walls between heights, a mesh of their own: hidden while the plane is flat (they would show as streaks)
            GameObject wallGo = new("walls");
            wallGo.transform.SetParent(rowGo.transform, false);
            if (walls.Count > 0) ExpQuads(wallGo.transform, "walls", walls, Factory.NoGlow);
            rowGo.SetActive(false);
            ts.Rows[j] = rowGo;
            ts.Walls[j] = wallGo;
        }

        // the frame and the axes on the floor: λ₂ across, λ₃ up
        Color axisCol = new(.7f, .74f, .88f);
        List<(Vector3, Vector3)> frame = new()
        {
            (At(lo, lo, 0), At(hi, lo, 0)), (At(hi, lo, 0), At(hi, hi, 0)), (At(hi, hi, 0), At(lo, hi, 0)), (At(lo, hi, 0), At(lo, lo, 0)),
        };
        Transform axes = new GameObject("axes").transform;
        axes.SetParent(ts.Root, false);
        for (int k = -3; k <= 3; k++)
        {
            frame.Add((At(k, lo, 0), At(k, lo - .12f, 0)));
            frame.Add((At(lo, k, 0), At(lo - .12f, k, 0)));
            TextBox a = ExpLabelWide(axes, k.ToString().Replace("-", "−"), At(k, lo - .42f, 0), labelSize * .14f, axisCol);
            TextBox b = ExpLabelWide(axes, k.ToString().Replace("-", "−"), At(lo - .3f, k, 0), labelSize * .14f, axisCol, TextAlignmentOptions.Right);
            Outline(a.TextField); Outline(b.TextField);
        }

        Segments(axes, "frame", frame, .14f, new Color(.55f, .6f, .78f) * .8f, Factory.NoGlow);
        TextBox n2 = ExpLabelWide(axes, "λ<sub>2</sub>", At(hi + .35f, lo - .1f, 0), labelSize * .18f, axisCol, TextAlignmentOptions.Left);
        TextBox n3 = ExpLabelWide(axes, "λ<sub>3</sub>", At(lo - .2f, hi + .35f, 0), labelSize * .18f, axisCol);
        Outline(n2.TextField); Outline(n3.TextField);

        ts.ScanLine = ExpLine(ts.Root, "scan", .5f, Color.white, AttentionIntensity, new[] { o, o });
        OnTop(ts.ScanLine, 20);
        ts.ScanLine.gameObject.SetActive(false);
        ts.Profile = ExpLine(ts.Root, "profile", .45f, Color.white, AttentionIntensity, new[] { o, o });
        OnTop(ts.Profile, 21);
        ts.Profile.gameObject.SetActive(false);
        ts.Slice = new GameObject("slice").transform;
        ts.Slice.SetParent(ts.Root, false);
        ts.SliceQuad = ExpQuads(ts.Slice, "plane", new List<(Vector3, Vector3, Vector3, Color)> { (o, Vector3.right, Vector3.back, new Color(.6f, .8f, 1f, .16f)) }, Factory.NoGlow);
        ts.Slice.gameObject.SetActive(false);
        terrain = ts;
        StartCoroutine(TerrainLoop(ts));
    }

    IEnumerator TerrainLoop(TerrainScene ts)
    {
        while (ts == terrain && ts.Root != null)
        {
            TerrainUpdate(ts);
            yield return null;
        }
    }

    void TerrainUpdate(TerrainScene ts)
    {
        float build = Tw("lf.build", 0f), h = Tw("lf.h", 0f), slice = Tw("lf.slice", -10f);
        string sig = $"{build:F4}|{h:F4}|{slice:F4}";
        if (sig == ts.Sig) return;
        ts.Sig = sig;
        const float lo = -3f, hi = 3f;
        int cells = ts.Cells;
        float step = (hi - lo) / cells, unit = ts.Size / (hi - lo);
        int shown = Mathf.Clamp(Mathf.CeilToInt(build * cells), 0, cells);
        for (int j = 0; j < cells; j++)
            if (ts.Rows[j].activeSelf != j < shown) ts.Rows[j].SetActive(j < shown);
        // the heights: the rows were built one world unit per zero off the line, measured from the floor (z = 0)
        ts.Heights.localScale = new Vector3(1f, 1f, Mathf.Max(.001f, h));
        bool wallsOn = h > .03f;
        foreach (GameObject w in ts.Walls) if (w != null && w.activeSelf != wallsOn) w.SetActive(wallsOn);
        // the scan line along the newest row
        bool scanning = build > 0f && build < 1f;
        ts.ScanLine.gameObject.SetActive(scanning);
        if (scanning)
        {
            float l3 = lo + shown * step;
            Vector3 a = ts.O + new Vector3(-ts.Size * .5f - 2f, (l3 - (lo + hi) * .5f) * unit, -.4f), b = a + Vector3.right * (ts.Size + 4f);
            ts.ScanLine.SetPositions(new[] { a, b });
        }

        // the slice: a translucent wall at λ₃ = slice and the row's profile along the top of the terrain
        bool sliceOn = slice >= lo && slice <= hi && h > .01f;
        ts.Slice.gameObject.SetActive(sliceOn);
        ts.Profile.gameObject.SetActive(sliceOn);
        if (sliceOn)
        {
            int j = Mathf.Clamp(Mathf.FloorToInt((slice - lo) / step), 0, cells - 1);
            float y = (lo + (j + .5f) * step - (lo + hi) * .5f) * unit;
            float wallH = (ts.Top + 2f) * h;
            ts.SliceQuad.transform.localPosition = Vector3.zero;
            Mesh m = ts.SliceQuad.GetComponent<MeshFilter>().sharedMesh;
            Vector3 c = ts.O + new Vector3(0f, y, -wallH * .5f);
            Vector3 u = Vector3.right * (ts.Size * .5f + 2f), v = Vector3.back * (wallH * .5f);
            m.SetVertices(new List<Vector3> { c - u - v, c + u - v, c + u + v, c - u + v });
            m.RecalculateBounds();
            int[,] off = FiveGrid(cells);
            Vector3[] pts = new Vector3[cells * 2];
            for (int i = 0; i < cells; i++)
            {
                float x0 = (lo + i * step - (lo + hi) * .5f) * unit, x1 = x0 + step * unit;
                float z = -off[i, j] * h - .3f;
                pts[2 * i] = ts.O + new Vector3(x0, y, z);
                pts[2 * i + 1] = ts.O + new Vector3(x1, y, z);
            }

            ts.Profile.positionCount = pts.Length;
            ts.Profile.SetPositions(pts);
        }
    }

    /// <summary>For the camera: the terrain's tallest point (top × lf.h), so a shot can frame it as it rises.</summary>
    public float TerrainHeight => terrain == null ? 0f : terrain.Top * Tw("lf.h", 0f);
}
