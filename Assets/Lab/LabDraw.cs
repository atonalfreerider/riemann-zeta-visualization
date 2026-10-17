using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Drawing for the model lab: everything many is one mesh (coloured segments as crossed quads, points as octahedra, both
/// updatable in place for animation), the few long curves are LineRenderers, and labels are billboards. All take world
/// positions and parent their objects under a model's root.
/// </summary>
public static class LabDraw
{
    public static LineRenderer Line(Transform parent, string name, float width, Color color, float glow, IList<Vector3> pts, bool loop = false)
    {
        LineRenderer line = Factory.NewLine(name, width, color, glow);
        line.transform.SetParent(parent, false);
        line.numCornerVertices = 3;
        line.loop = loop;
        line.positionCount = pts.Count;
        for (int i = 0; i < pts.Count; i++) line.SetPosition(i, pts[i]);
        return line;
    }

    public static void SetLine(LineRenderer line, IList<Vector3> pts)
    {
        line.positionCount = pts.Count;
        for (int i = 0; i < pts.Count; i++) line.SetPosition(i, pts[i]);
    }

    public static LineRenderer Ring(Transform parent, string name, Vector3 center, float radius, float width, Color color, float glow, int steps = 96)
    {
        Vector3[] pts = new Vector3[steps];
        for (int i = 0; i < steps; i++)
        {
            float a = i / (float)steps * Mathf.PI * 2f;
            pts[i] = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
        }

        return Line(parent, name, width, color, glow, pts, true);
    }

    public static GameObject Dot(Transform parent, string name, Vector3 pos, float size, Color color, float glow = Factory.GlowIntensity)
    {
        GameObject d = Factory.NewSphere(name, size, color, glow);
        d.transform.SetParent(parent, false);
        d.transform.position = pos;
        return d;
    }

    /// <summary>A billboard label (world height about 0.1 × size); lines are kept whole.</summary>
    public static TextBox Label(Transform parent, string text, Vector3 pos, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        TextBox l = TextBox.Create(text, size);
        l.transform.SetParent(parent, false);
        l.Alignment = align;
        l.Color = color;
        l.TextField.textWrappingMode = TextWrappingModes.NoWrap;
        l.gameObject.AddComponent<Billboard>();
        l.transform.position = pos;
        return l;
    }

    public static Mesh NewMesh(string name, Transform parent, float glow, out MeshRenderer renderer)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        Mesh mesh = new() { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.MarkDynamic();
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = Factory.mainMat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        Factory.SetIntensity(renderer, glow);
        return mesh;
    }
}

/// <summary>Many coloured segments in one mesh (two crossed quads each, readable from any side); rebuilt with <see cref="Set"/>.</summary>
public class SegmentMesh
{
    public readonly Mesh Mesh;
    public readonly MeshRenderer Renderer;
    readonly List<Vector3> verts = new();
    readonly List<Color> cols = new();
    readonly List<int> tris = new();

    public SegmentMesh(Transform parent, string name, float glow = Factory.NoGlow)
    {
        Mesh = LabDraw.NewMesh(name, parent, glow, out Renderer);
    }

    public bool Visible
    {
        get => Renderer.enabled;
        set => Renderer.enabled = value;
    }

    public void Clear() => Mesh.Clear();

    /// <summary>Replace the contents: segment i from a to b, its own colour and width.</summary>
    public void Set(IList<(Vector3 a, Vector3 b, Color c, float w)> segs)
    {
        verts.Clear();
        cols.Clear();
        tris.Clear();
        foreach (var (a, b, c, w) in segs) Add(a, b, c, w);
        Mesh.Clear();
        Mesh.SetVertices(verts);
        Mesh.SetColors(cols);
        Mesh.SetTriangles(tris, 0);
        Mesh.RecalculateBounds();
    }

    void Add(Vector3 a, Vector3 b, Color c, float w)
    {
        Vector3 d = b - a;
        if (d.sqrMagnitude < 1e-12f) return;
        d.Normalize();
        float h = w * .5f;
        Vector3 n1 = Vector3.Cross(d, Mathf.Abs(d.z) > .9f ? Vector3.right : Vector3.forward).normalized * h;
        Vector3 n2 = Vector3.Cross(d, n1).normalized * h;
        for (int q = 0; q < 2; q++)
        {
            Vector3 n = q == 0 ? n1 : n2;
            int v = verts.Count;
            verts.Add(a - n); verts.Add(a + n); verts.Add(b + n); verts.Add(b - n);
            for (int k = 0; k < 4; k++) cols.Add(c);
            tris.Add(v); tris.Add(v + 1); tris.Add(v + 2);
            tris.Add(v); tris.Add(v + 2); tris.Add(v + 3);
        }
    }
}

/// <summary>Many points as small octahedra in one mesh, each with its own colour and size; positions can be moved every frame.</summary>
public class PointMesh
{
    public readonly Mesh Mesh;
    public readonly MeshRenderer Renderer;
    Vector3[] verts = new Vector3[0];
    Color[] cols = new Color[0];
    float[] sizes = new float[0];
    int count;

    static readonly int[][] Faces =
    {
        new[] { 0, 2, 4 }, new[] { 2, 1, 4 }, new[] { 1, 3, 4 }, new[] { 3, 0, 4 },
        new[] { 2, 0, 5 }, new[] { 1, 2, 5 }, new[] { 3, 1, 5 }, new[] { 0, 3, 5 }
    };

    public PointMesh(Transform parent, string name, float glow = Factory.GlowIntensity)
    {
        Mesh = LabDraw.NewMesh(name, parent, glow, out Renderer);
    }

    public bool Visible
    {
        get => Renderer.enabled;
        set => Renderer.enabled = value;
    }

    public int Count => count;

    /// <summary>Rebuild with a new set of points.</summary>
    public void Set(IList<Vector3> pts, IList<Color> colors, IList<float> pointSizes)
    {
        count = pts.Count;
        verts = new Vector3[count * 6];
        cols = new Color[count * 6];
        sizes = new float[count];
        int[] tris = new int[count * 24];
        for (int i = 0; i < count; i++)
        {
            sizes[i] = pointSizes[i];
            for (int k = 0; k < 6; k++) cols[i * 6 + k] = colors[i];
            for (int f = 0; f < 8; f++)
                for (int k = 0; k < 3; k++)
                    tris[i * 24 + f * 3 + k] = i * 6 + Faces[f][k];
        }

        Place(pts);
        Mesh.Clear();
        Mesh.vertices = verts;
        Mesh.colors = cols;
        Mesh.triangles = tris;
        Mesh.RecalculateBounds();
    }

    void Place(IList<Vector3> pts)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 p = pts[i];
            float s = sizes[i];
            int v = i * 6;
            verts[v] = p + new Vector3(s, 0f, 0f); verts[v + 1] = p - new Vector3(s, 0f, 0f);
            verts[v + 2] = p + new Vector3(0f, s, 0f); verts[v + 3] = p - new Vector3(0f, s, 0f);
            verts[v + 4] = p + new Vector3(0f, 0f, s); verts[v + 5] = p - new Vector3(0f, 0f, s);
        }
    }

    /// <summary>Move the points (same count as the last Set).</summary>
    public void Move(IList<Vector3> pts)
    {
        Place(pts);
        Mesh.vertices = verts;
        Mesh.RecalculateBounds();
    }

    public void Recolour(IList<Color> colors, IList<float> pointSizes = null)
    {
        for (int i = 0; i < count; i++)
        {
            for (int k = 0; k < 6; k++) cols[i * 6 + k] = colors[i];
            if (pointSizes != null) sizes[i] = pointSizes[i];
        }

        Mesh.colors = cols;
    }
}

/// <summary>Thousands of thin edges between moving points, as one line-topology mesh (one pixel wide, cheap to move).</summary>
public class LineMesh
{
    public readonly Mesh Mesh;
    public readonly MeshRenderer Renderer;
    (int a, int b)[] pairs = new (int, int)[0];
    Vector3[] verts = new Vector3[0];

    public LineMesh(Transform parent, string name, float glow = Factory.NoGlow)
    {
        Mesh = LabDraw.NewMesh(name, parent, glow, out Renderer);
    }

    public bool Visible
    {
        get => Renderer.enabled;
        set => Renderer.enabled = value;
    }

    public void Set((int a, int b)[] edgePairs, IList<Vector3> positions, Color color)
    {
        pairs = edgePairs;
        verts = new Vector3[pairs.Length * 2];
        Color[] cols = new Color[verts.Length];
        int[] idx = new int[verts.Length];
        for (int i = 0; i < verts.Length; i++)
        {
            cols[i] = color;
            idx[i] = i;
        }

        Fill(positions);
        Mesh.Clear();
        Mesh.vertices = verts;
        Mesh.colors = cols;
        Mesh.SetIndices(idx, MeshTopology.Lines, 0);
        Mesh.RecalculateBounds();
    }

    void Fill(IList<Vector3> positions)
    {
        for (int i = 0; i < pairs.Length; i++)
        {
            verts[2 * i] = positions[pairs[i].a];
            verts[2 * i + 1] = positions[pairs[i].b];
        }
    }

    public void Move(IList<Vector3> positions)
    {
        if (pairs.Length == 0) return;
        Fill(positions);
        Mesh.vertices = verts;
        Mesh.RecalculateBounds();
    }
}
